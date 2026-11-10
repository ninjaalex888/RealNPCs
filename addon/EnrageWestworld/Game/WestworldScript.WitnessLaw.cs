// Witnesses fetch the law, the game side (FOURTEENHOUR item 21; Core/WitnessLaw.cs; [Mind] WitnessesFetchLaw). A brawl of three or
// more with no lawman come (BrawlLawComes found none in sight or earshot): once a fight, one onlooker within 25 m, a gentleman or a
// lady first, else the law-abiding by nature, runs at a sprint to the nearest free lawman within 150 m (the chase order, seen); there,
// a word, and the lawman takes it from there at a run (item 5's arrival: BrawlLawDispatch, the cry, the word, the holdouts tied). After
// a face put into a bar or a window ("rough", played to its end), a witness may run for a lawman the same way, who walks over (LawTake's
// borrowing, seen) and has a word with the man who did it if he's still about, or asks the one it was done to; then back to his
// rounds. A single fetch runs at once; a look every half second from the brawl's step, never a frame of its own.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class LawFetch
        {
            public Ped Fetcher, Law, Culprit, Victim;
            public string Name, Kind, Seed, CulpritName, CulpritTemper;   // Kind: "brawl" or "rough"
            public int FightId, Since, LookAt, Phase;                     // 0 running to the lawman; 1 the lawman walking to the culprit (rough)
            public bool Saloon;
            public Vector3 At;
        }

        private LawFetch _lawFetch;
        private Ped _fetchWinner, _fetchLoser;   // the fight a fetcher ran from: who was left standing (OnFightOver)
        private int _fetchFor = -1, _fetchSureUntil, _fetches, _fetchBrought, _fetchWords, _fetchAsked, _fetchLate, _fetchNone;
        private string _fetchLast;

        private static bool Up(Ped p) { return p != null && p.Exists() && !p.IsDead; }

        /// One onlooker near `at` who goes for the law: the well-off first, then by nature (a roll each, nearest first); null for none.
        private Ped FetcherNear(Vector3 at, float metres, int now, string seed, ICollection<int> not, out string name)
        {
            name = null;
            Ped me = Game.Player.Character;
            bool sure = now < _fetchSureUntil;
            var near = Peds.All().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && !not.Contains(p.Handle) && PED.IS_PED_HUMAN(p.Handle) && IsGrownUp(p) &&
                                             !Natives.IsLaw(p) && p.Position.DistanceTo(at) < metres && !_director.Involves(p.Handle) && !NotFree(p.Handle) && !Spawner.IsOurs(p.Handle) &&
                                             !PED.IS_PED_IN_COMBAT(p.Handle, 0) && !PED.IS_PED_ON_MOUNT(p.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) && !PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                                 .OrderBy(p => WellOff(p) ? 0 : 1).ThenBy(p => p.Position.DistanceTo(at)).Take(6).ToList();
            foreach (Ped p in near)
            {
                MindWho w = MindKnown(p.Handle, now);
                if (w == null || w.Id == null || w.Src == null)
                    continue;
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                if (!sure && !WitnessLaw.Fetches(t, WellOff(p), true, seed + "|" + w.Id))
                    continue;
                name = w.Name;
                return p;
            }
            return null;
        }

        /// From BrawlLawComes, when no lawman has come to a brawl of three or more: once a fight, one onlooker may run for him.
        private void BrawlFetchBegin(List<int> a, List<int> b, Vector3 mid, int now)
        {
            if (!WitnessLaw.On || _lawFetch != null || _fetchFor == _director.FightId || a.Count + b.Count < 3 || now - _brawlFightSince < WitnessLaw.FetchAfterMs)
                return;
            _fetchFor = _director.FightId;
            Ped law = BrawlFindLaw(mid, WitnessLaw.LawMostMetres, false, 0);
            string name = null;
            Ped f = law != null ? FetcherNear(mid, WitnessLaw.FetchFromMetres, now, "fetch|" + _director.FightId, new HashSet<int>(a.Concat(b)), out name) : null;
            if (law == null || f == null)
            {
                _fetchNone++;
                Log.Detail("FETCH the law: " + (law == null ? "no lawman free within 150 m of the brawl" : "nobody near the brawl goes for him"));
                return;
            }
            FetchStart(f, name, law, "brawl", null, null, mid, now);
        }

        /// From OnPlayedOut: a face put into a bar or a window, played to its end; a witness may run for a lawman (not one already near).
        private void RoughFetchBegin(Ped doer, Ped victim)
        {
            int now = Game.GameTime;
            if (!WitnessLaw.On || _lawFetch != null || !Up(doer) || !Up(victim))
                return;
            Vector3 at = victim.Position;
            Ped law = BrawlFindLaw(at, WitnessLaw.LawMostMetres, false, 0);
            if (law == null || law.Position.DistanceTo(at) < 15f)
                return;   // none, or one right there already (his own eyes)
            string name;
            Ped f = FetcherNear(at, 20f, now, "rough|" + doer.Handle + "|" + now / 60000, new HashSet<int> { doer.Handle, victim.Handle }, out name);
            if (f == null)
                return;
            FetchStart(f, name, law, "rough", doer, victim, at, now);
        }

        private void FetchStart(Ped f, string name, Ped law, string kind, Ped culprit, Ped victim, Vector3 at, int now)
        {
            MindWho cw = culprit != null ? MindKnown(culprit.Handle, now) : null;
            _lawFetch = new LawFetch
            {
                Fetcher = f, Name = name, Law = law, Kind = kind, Culprit = culprit, Victim = victim, At = at, FightId = _director.FightId, Since = now, LookAt = now + 500,
                Saloon = _placeKind == "saloon", Seed = kind + "|" + f.Handle + "|" + now / 1000, CulpritName = cw != null ? cw.Name : null, CulpritTemper = cw != null && cw.P != null ? cw.P.Personality : null
            };
            _fetches++;
            TASK.CLEAR_PED_TASKS(f.Handle, true, false);
            TASK.TASK_GO_TO_ENTITY(f.Handle, law.Handle, -1, 2.5f, 3f, 0f, 0);   // at a sprint, to him (the chase order)
            if (_cfg.ScenesOnTicker && _cfg.Ticker && f.Position.DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                SpeechCaption(Person.FirstOf(name), WitnessLaw.Off(_lawFetch.Seed), f.Handle, 2500);
            Log.Write("FETCH " + name + " runs for the law (" + (kind == "brawl" ? "a brawl" : "a man beaten") + "): a lawman " + (int)law.Position.DistanceTo(f.Position) + " m off" + (now < _fetchSureUntil ? " (a test)" : ""));
        }

        /// Every half second from the brawl's step: the run to the lawman, the word, and for a beating the lawman's walk over.
        private void LawFetchStep(int now)
        {
            LawFetch f = _lawFetch;
            if (f == null || now < f.LookAt)
                return;
            f.LookAt = now + 500;
            Ped me = Game.Player.Character;
            if (f.Phase == 0)
            {
                bool over = f.Kind == "brawl" && (f.FightId != _director.FightId || _director.FightId == 0 || _brawlLawFor == f.FightId);
                if (over && _brawlLawFor != f.FightId && _director.EndedFightId == f.FightId && Up(_fetchWinner))
                {
                    // the fight's over before he got to the lawman: he tells him all the same, and the lawman has a word with the one left
                    // standing (a brawl's 30 s is over long before a lawman 70 m off could be there: the simulator, item 21)
                    f.Kind = "after";
                    f.Culprit = _fetchWinner;
                    f.Victim = Up(_fetchLoser) ? _fetchLoser : null;
                    f.At = _fetchWinner.Position;
                    MindWho ww = MindKnown(_fetchWinner.Handle, now);
                    f.CulpritName = ww != null ? ww.Name : null;
                    f.CulpritTemper = ww != null && ww.P != null ? ww.P.Personality : null;
                    over = false;
                }
                if (!Up(f.Fetcher) || !Up(f.Law) || PED.IS_PED_IN_COMBAT(f.Law.Handle, me.Handle) || PED.IS_PED_IN_COMBAT(f.Fetcher.Handle, 0) || now - f.Since > WitnessLaw.FetchMostMs || over)
                {
                    if (Up(f.Fetcher) && !PED.IS_PED_IN_COMBAT(f.Fetcher.Handle, 0))
                        WanderLater(f.Fetcher.Handle, now + 1000);
                    if (over)
                        _fetchLate++;
                    _fetchLast = f.Name + " ran for the law" + (over ? ", but the fight was over (or the law there) first" : ", and never found him");
                    Log.Write("FETCH " + _fetchLast);
                    _lawFetch = null;
                    return;
                }
                if (f.Fetcher.Position.DistanceTo(f.Law.Position) > 3.5f)
                {
                    if ((now - f.Since) % 2000 < 500)
                        TASK.TASK_GO_TO_ENTITY(f.Fetcher.Handle, f.Law.Handle, -1, 2.5f, 3f, 0f, 0);   // he may be walking his rounds: the order again
                    return;
                }
                if (_cfg.ScenesOnTicker && _cfg.Ticker && f.Fetcher.Position.DistanceTo(me.Position) <= _cfg.ScenesOnTickerMetres)
                    SpeechCaption(Person.FirstOf(f.Name), WitnessLaw.ToLaw(f.Kind != "rough", f.Saloon, f.Seed), f.Fetcher.Handle, 3000);
                LawSay(f.Law, "The deputy", WitnessLaw.LawReply(f.Seed), "GENERIC_YES");
                if (f.Kind == "brawl")
                {
                    List<int> a, b;
                    if (_director.FightSides(out a, out b) && ENTITY.DOES_ENTITY_EXIST(a[0]))
                    {
                        BrawlLawDispatch(f.Law, a, b, ENTITY.GET_ENTITY_COORDS(a[0], true, true), now, false, f.Name);   // item 5's arrival from here
                        _fetchBrought++;
                        TASK.TASK_GO_TO_ENTITY(f.Fetcher.Handle, a[0], 20000, 8f, 2f, 0f, 0);   // back with him, to watch from a way off
                        WanderLater(f.Fetcher.Handle, now + 20000);
                        _fetchLast = f.Name + " brought the law to the brawl";
                    }
                    _lawFetch = null;
                    return;
                }
                // a beating: the lawman walks over, ours while he does (LawTake), and the fetcher goes back about his day
                f.Phase = 1;
                f.Since = now;
                LawTake(f.Law.Handle);
                TASK.CLEAR_PED_TASKS(f.Law.Handle, true, false);
                Ped to = Up(f.Culprit) && f.Culprit.Position.DistanceTo(f.At) < 40f ? f.Culprit : f.Victim;
                if (Up(to))
                    TASK.TASK_GO_TO_ENTITY(f.Law.Handle, to.Handle, 25000, 2.5f, 2f, 0f, 0);
                WanderLater(f.Fetcher.Handle, now + 3000);
                return;
            }
            // phase 1: the lawman's word with the man who did it (still about, and not fighting you), or he asks the one it was done to
            Ped target = Up(f.Culprit) && f.Culprit.Position.DistanceTo(f.At) < 40f && !PED.IS_PED_IN_COMBAT(f.Culprit.Handle, me.Handle) ? f.Culprit : Up(f.Victim) ? f.Victim : null;
            if (!Up(f.Law) || target == null || now - f.Since > 30000)
            {
                if (Up(f.Law))
                    LawRoundsAgain(f.Law.Handle, now + 1000);
                _lawFetch = null;
                return;
            }
            if (f.Law.Position.DistanceTo(target.Position) > 3f)
            {
                if ((now - f.Since) % 2000 < 500)
                    TASK.TASK_GO_TO_ENTITY(f.Law.Handle, target.Handle, 25000, 2.5f, 2f, 0f, 0);
                return;
            }
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(f.Law.Handle, target.Handle, 4000, 0f, 0f, 0f);
            if (target == f.Culprit)
            {
                LawSay(f.Law, "The deputy", f.Kind == "after" ? WitnessLaw.WordAfter(f.Seed) : WitnessLaw.Word(f.Seed), "SHOUT_STOP");
                SayLater(2200, Person.FirstOf(f.CulpritName ?? "The man"), WitnessLaw.Answer(f.CulpritTemper, f.Seed), f.Culprit.Handle, "mild");
                _fetchWords++;
                _fetchLast = "the law had a word with " + (f.CulpritName ?? "the man") + ", fetched by " + f.Name;
            }
            else
            {
                LawSay(f.Law, "The deputy", WitnessLaw.Asks(f.Seed), "GENERIC_SHOCKED_MED");
                _fetchAsked++;
                _fetchLast = "the law asked after the man who did it, fetched by " + f.Name;
            }
            Log.Write("FETCH " + _fetchLast);
            LawRoundsAgain(f.Law.Handle, now + 6000);
            _lawFetch = null;
        }

        /// From OnFightOver: while someone has run for the law from this fight, who won it (the lawman's word, if he's late).
        private void FetchFightOver(int winner, int loser)
        {
            if (_lawFetch == null || _lawFetch.Kind != "brawl")
                return;
            _fetchWinner = winner != 0 && ENTITY.DOES_ENTITY_EXIST(winner) ? new Ped(winner) : null;
            _fetchLoser = loser != 0 && ENTITY.DOES_ENTITY_EXIST(loser) ? new Ped(loser) : null;
        }

        /// The Testing row: the street's rough scene now, and the first witness runs for a lawman within 150 m.
        private string FetchLawFire(int now, string seed, out string said)
        {
            said = null;
            if (!WitnessLaw.On)
                return "off ([Mind] WitnessesFetchLaw)";
            _fetchSureUntil = now + 60000;
            return FireScene("rough", now, seed, out said);
        }

        /// F7: the fetches this session.
        private string WitnessLawStatus()
        {
            if (!WitnessLaw.On)
                return "off ([Mind] WitnessesFetchLaw)";
            return "ran for the law " + _fetches + " (brought to a brawl " + _fetchBrought + ", too late " + _fetchLate + "; a word had with the culprit " + _fetchWords + ", asked after him " + _fetchAsked +
                   "); brawls with nobody to fetch him " + _fetchNone + (_lawFetch != null ? "; " + _lawFetch.Name + " is on his way" : "") + (_fetchLast != null ? "; last: " + _fetchLast : "");
        }
    }
}
