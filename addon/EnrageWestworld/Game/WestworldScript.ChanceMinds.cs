// The chance meeting chosen by the two people's minds ([Mind] MindChanceMeetings, with MindLive; Core/Scenes.Minds.cs;
// NIGHT4 item 5's plan E). Who the two are to the mind (MindKnown: never makes anyone) leans what the place allows; the
// mind's own choice is a draw on those weights, and with [Mind] LanguagePicks the model may pick among its three
// likeliest within 2.5 s while the two are held (late, empty or unreadable: the mind's own). The second full scene takes
// the mind's own at once (no wait). Neither known: the roll, as before. Off: the roll exactly as it was.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class ChanceWait
        {
            public Ped A, B, Crowd;
            public SceneCast Cast;
            public string Seed, Why;
            public StagedAsk Ask;
            public ChanceLean Lean;
            public bool Saloon;
            public int Since;
        }

        private ChanceWait _chanceWait;
        private Action _aChanceWait;
        private readonly Dictionary<string, int> _chanceByMind = new Dictionary<string, int>(), _chanceByModel = new Dictionary<string, int>(), _chanceByRoll = new Dictionary<string, int>();
        private int _chanceMindAsked, _chanceWaitDropped, _chanceNoMind;
        private string _chanceMindLast, _chanceWaitWhyDropped;

        private bool ChanceMindsOn { get { return _cfg.MindLive && _cfg.MindChanceMeetings && _store != null && _engine != null; } }

        /// One of the two as the lean reads them, from who the mind knows them as.
        private ChanceMind ChanceMindOf(MindWho w, Ped p, long g, long day)
        {
            if (w.Traits == null && w.Src != null)
                w.Traits = _engine.Shadow.TraitsOf(w.Src);   // two people, once a meeting (as the ToYou nods fill them)
            var m = new ChanceMind { Name = Person.FirstOf(w.Name), Temper = w.Src != null ? w.Src.Temper : w.P != null ? w.P.Personality : null };
            if (w.Traits != null)
            {
                m.Mood = Passing.DayMood(_engine.Shadow.MoodOf(w.Id, w.Traits, g), Mood.Baseline(w.Traits));
                m.Sociable = w.Traits[Traits.Sociability];
                m.Pious = w.Traits[Traits.Piety];
            }
            string kind;
            int stage;
            bool good;
            if (w.P != null && InnerLife.WorryNow(w.P, day, out kind, out stage, out good) && stage <= 2)
            {
                m.Worry = kind;
                m.WorryStage = stage;
            }
            m.Drunk = InHisCups(p, w.P);
            return m;
        }

        /// The kind the two minds choose, or null for the roll. `by`: mind, roll, or "wait" when the model was asked (the two are
        /// held and ChanceWaitStep plays it once the pick is settled).
        private string ChanceMindKind(Ped a, Ped b, Ped crowd, SceneCast cast, string seed, bool saloon, bool asSecond, int now, out string by)
        {
            by = "roll";
            MindWho wa = MindKnown(a.Handle, now), wb = MindKnown(b.Handle, now);
            bool ka = wa != null && wa.Id != null, kb = wb != null && wb.Id != null;
            if (!ka && !kb)
            {
                _chanceNoMind++;
                return null;
            }
            long g = _store.LastGameMin, day = g / 1440;
            ChanceMind ma = ka ? ChanceMindOf(wa, a, g, day) : null, mb = kb ? ChanceMindOf(wb, b, g, day) : null;
            ChanceLean lean = Scenes.MindLean(Scenes.ChanceWeights(cast, true), cast, ma, mb);
            if (lean == null || lean.W.Possible == 0)
                return null;
            string mseed = seed + "|minds";
            string otherB = kb && wb.Name != null ? Person.FirstOf(wb.Name) : null;
            if (!asSecond && ka && PicksOn && lean.W.Possible >= 2)
            {
                // A is asked (the one who starts the scene), among the three likeliest
                int leadFav = lean.LeadFavour, leadOff = lean.LeadBrushoff;
                StagedAsk ask = StagedChoose(a.Handle, "chance", lean.W, mseed, id => Scenes.MindWords(id, otherB, id == "favour" ? leadFav : id == "brushoff" ? leadOff : 0),
                                             Scenes.MindNow(otherB, saloon, _placeTown), otherB, now, 3);
                if (ask.Asked && !ask.Done)
                {
                    _chanceMindAsked++;
                    _staged.Add(a.Handle);   // held from every other feature for the wait (NotFree), nothing more: the Director sets its own on start
                    _staged.Add(b.Handle);
                    _chanceWait = new ChanceWait { A = a, B = b, Crowd = crowd, Cast = cast, Seed = seed, Ask = ask, Lean = lean, Saloon = saloon, Since = now, Why = Scenes.MindWhy(lean) };
                    by = "wait";
                    return null;
                }
                by = "mind";
                return ChanceKindSet(cast, lean, ask.Picked);
            }
            by = "mind";
            return ChanceKindSet(cast, lean, Staged.Choose(lean.W, mseed));
        }

        private string ChanceKindSet(SceneCast cast, ChanceLean lean, string kind)
        {
            cast.MindLead = lean.LeadOf(kind);
            cast.MindWorry = lean.FavourWorry;
            _chanceMindLast = kind + " (" + Scenes.MindWhy(lean) + ")";
            return kind;
        }

        /// Tick04: the chance meeting waiting on the model's pick, settled and played (or let go if the street moved on).
        private void ChanceWaitStep(int now)
        {
            if (_chanceWait == null)
                return;
            if (_aChanceWait == null)
                _aChanceWait = () => ChanceWaitPlay(_now);
            Safe("chance minds", _aChanceWait);
        }

        private void ChanceWaitPlay(int now)
        {
            ChanceWait w = _chanceWait;
            if (w == null)
                return;
            if (!ChanceMindsOn)
            {
                ChanceWaitAbort("[Mind] MindChanceMeetings off");
                return;
            }
            if (!StagedSettle(w.Ask, "chance", w.Ask.Name, now) && now - w.Since < PickWaitMs + 2000)
                return;
            _chanceWait = null;
            _staged.Remove(w.A.Handle);
            _staged.Remove(w.B.Handle);
            Ped me = Game.Player.Character;
            string whyNot = me == null || !me.Exists() || me.IsDead ? "you're down"
                          : _director.Busy ? "another scene started meanwhile"
                          : ChanceGone(w.A) ?? ChanceGone(w.B) ?? (w.Crowd != null ? ChanceGone(w.Crowd) : null)
                          ?? (w.A.Position.DistanceTo(me.Position) > StreetMetres + 8f ? "you've walked on" : null)
                          ?? (MISC.GET_MISSION_FLAG() ? "a mission" : null);
            if (whyNot != null)
            {
                _chanceWaitDropped++;
                _chanceWaitWhyDropped = whyNot;
                Log.Detail("CHANCE let go after the model's pick: " + whyNot);
                return;
            }
            string by = w.Ask.ByModel ? "model" : "mind";
            string kind = ChanceKindSet(w.Cast, w.Lean, w.Ask.Picked);
            var people = new List<Ped> { w.A, w.B };
            Scene s = ChanceKindScene(kind, w.Cast, w.Seed, w.A, w.B, w.Crowd, people, ref by);
            ChancePlay(s, people, w.A, w.B, w.Cast, w.Seed, me, w.Saloon, now, false, by);
        }

        private string ChanceGone(Ped p)
        {
            if (p == null || !ENTITY.DOES_ENTITY_EXIST(p.Handle) || ENTITY.IS_ENTITY_DEAD(p.Handle))
                return "one of them is gone";
            int h = p.Handle;
            if (_director.Involves(h) || NotFree(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || AimedLately(h, _now))
                return "one of them is taken up";
            return MayDirectWhyNot(p);
        }

        /// The wait let go (a wipe, a mission, the switch off).
        private void ChanceWaitAbort(string why)
        {
            ChanceWait w = _chanceWait;
            if (w == null)
                return;
            _chanceWait = null;
            if (w.A != null)
                _staged.Remove(w.A.Handle);
            if (w.B != null)
                _staged.Remove(w.B.Handle);
            _chanceWaitDropped++;
            _chanceWaitWhyDropped = why;
        }

        /// A chance meeting played: counted by who chose its kind, and the log's line.
        private void ChanceNoteBy(string by, string kind, Ped a, Ped b)
        {
            Dictionary<string, int> d = by == "model" ? _chanceByModel : by == "mind" ? _chanceByMind : _chanceByRoll;
            int n;
            d.TryGetValue(kind, out n);
            d[kind] = n + 1;
            if (by != "roll" || ChanceMindsOn)
                Log.Write("CHANCE " + ChanceName(a) + " & " + ChanceName(b) + ": " + kind + " (" + by + (by != "roll" && _chanceMindLast != null ? "; " + _chanceMindLast : "") + ")");
        }

        private string ChanceName(Ped p)
        {
            MindWho w;
            return p != null && _mindWho.TryGetValue(p.Handle, out w) && w.Name != null ? w.Name : "someone";
        }

        /// F7: the chance meetings by who chose them.
        private string ChanceMindsStatus()
        {
            Func<Dictionary<string, int>, string> list = d =>
            {
                int all = d.Values.Sum();
                return all + (all == 0 ? "" : " (" + string.Join(", ", d.OrderByDescending(x => x.Value).Select(x => x.Key + " " + x.Value)) + ")");
            };
            return "[Mind] MindChanceMeetings " + (_cfg.MindChanceMeetings ? "on" : "off") + (_cfg.MindLive ? "" : " (MindLive off: the roll)") +
                   "; chosen by the mind " + list(_chanceByMind) + ", by the model " + list(_chanceByModel) + ", by the roll " + list(_chanceByRoll) +
                   "; the model asked " + _chanceMindAsked + ", waits let go " + _chanceWaitDropped + (_chanceWaitWhyDropped != null ? " (last: " + _chanceWaitWhyDropped + ")" : "") +
                   ", nobody the mind knows " + _chanceNoMind + (_chanceWait != null ? "; one waiting on the model now" : "") +
                   (_chanceMindLast != null ? "; last the minds chose: " + _chanceMindLast : "");
        }
    }
}
