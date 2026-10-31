// Onlookers who join a fistfight going on near the player, on a side, from their minds (Core/JoinIn.cs): a friend of
// someone getting beaten, someone with bad blood against a fighter, a drunk hothead in a saloon. A single joiner each look, every few
// seconds, so a fight spreads the way it would: the friend of the man just set upon may be next (the NPC-life run, Lex
// 2026-10-30: "it can break out between any 2+ NPCs if their inner thoughts lead to it"). Behind [Mind] MindLive and
// MindJoinFights.
using System;
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
        private const int JoinEveryMs = 2500;
        private const float JoinMetres = 18f;   // 12 until 2026-10-31 (Lex, in busy Saint Denis: more of the street should join)
        private const int JoinMeetMost = 6;     // strangers given a resident's identity on the spot in one look, the nearest first (the cost)
        private int _joinNextAt, _joinTotal, _joinMet;
        private bool _joinFailed;
        private string _joinLast;

        // [Mind] MindBrawls (Core/Brawl.cs): the cap, the waves, and the sat and the leaning who get up first
        private bool BrawlsOn { get { return _cfg.MindLive && _cfg.MindJoinFights && _cfg.MindBrawls; } }
        private int MostJoinersNow { get { return BrawlsOn ? Brawl.MostJoiners(_cfg.BrawlMost) : JoinIn.MostJoiners; } }
        private sealed class JoinPending { public int Handle, Side, Target, At, Until; public string Name, Why; }
        private readonly List<JoinPending> _joinPending = new List<JoinPending>();
        private int _brawlBiggest;

        private void JoinStep(int now)
        {
            _director.FightMostMs = BrawlsOn ? 45000 : 20000;
            _director.DownsOn = BrawlsOn;   // a man beaten down in a brawl: killed or knocked out, and it goes on (WestworldScript.Brawl.cs)
            if (_director.FighterDown == null)
                _director.FighterDown = OnFighterDown;
            if (!_cfg.MindLive || !_cfg.MindJoinFights || _joinFailed || now < _joinNextAt)
                return;
            _joinNextAt = now + JoinEveryMs;
            List<int> a, b;
            if (!_director.FightSides(out a, out b))
            {
                _joinPending.Clear();
                return;
            }
            if (a.Count + b.Count > _brawlBiggest)
                _brawlBiggest = a.Count + b.Count;
            JoinPendingStep(now);
            if (_director.JoinerCount + _joinPending.Count >= MostJoinersNow)
                return;
            try
            {
                JoinLook(a, b, now);
            }
            catch (Exception ex)
            {
                _joinFailed = true;
                Log.Write("BRAWL joining failed once, off for this session: " + ex.Message);
            }
        }

        private void JoinLook(List<int> a, List<int> b, int now)
        {
            Ped me = Game.Player.Character;
            if (!ENTITY.DOES_ENTITY_EXIST(a[0]))
                return;
            Vector3 mid = ENTITY.GET_ENTITY_COORDS(a[0], true, true);
            var sides = new[] { a.Select(FighterOf).ToList(), b.Select(FighterOf).ToList() };
            var looking = new List<Ped>();
            int witnesses = 0;
            bool law = false;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(mid) > JoinMetres + 18f)
                    continue;   // the distance first
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(h))
                    continue;
                if (Natives.IsLaw(p))
                {
                    if (p.Position.DistanceTo(mid) < 30f)
                        law = true;
                    continue;
                }
                if (p.Position.DistanceTo(mid) > JoinMetres || _director.Involves(h))
                    continue;
                witnesses++;
                if (_director.IsOut(h) || _brawlOut.ContainsKey(h) || _brawlFatal.Contains(h))
                    continue;   // knocked out in it, or killed: not getting up to join
                if (Spawner.IsOurs(h) || Natives.StoryModel(p) || !IsGrownUp(p) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || _wakeActs.Any(w => w.Handle == h) ||
                    (PED.IS_PED_USING_ANY_SCENARIO(h) && !(BrawlsOn && a.Count + b.Count >= 3)) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || MayDirectWhyNot(p) != null ||
                    JoinIsPending(h))
                    continue;   // the sat and the leaning stay put (pulled off a bar they went into a T pose); in a brawl they get up first (JoinPending)
                looking.Add(p);
            }
            if (looking.Count == 0)
                return;
            string fight = (_director.FightId != 0 ? "f" + _director.FightId : a[0] + "|" + b[0]) + "|" + (a.Count + b.Count);
            bool saloon = _placeKind == "saloon", sure = BrawlsOn && now < _brawlTestUntil;   // the Testing row: the waves without the bar or the roll
            int most = MostJoinersNow, room = most - _director.JoinerCount - _joinPending.Count;
            int wave = BrawlsOn ? Brawl.Wave(a.Count + b.Count, saloon, room, witnesses) : Math.Min(1, room);
            var takes = new List<KeyValuePair<JoinTake, KeyValuePair<Ped, MindWho>>>();
            int met = 0;
            if (BrawlsOn && !OwnLooksOn)
                looking = looking.OrderBy(p => p.Position.DistanceTo(mid)).ToList();   // the nearest strangers are the ones given a name
            foreach (Ped p in looking)
            {
                MindWho w = MindKnown(p.Handle, now);
                // with brawls on, anyone near may join (Lex, 2026-10-31: in Saint Denis only people the mind had met could):
                // a stranger is given a resident's identity on the spot, as the street scenes do, a few a look; never with
                // OwnLooks on (its reading of clothes is the kind that froze the game)
                if (BrawlsOn && !OwnLooksOn && (w == null || w.Id == null) && met < JoinMeetMost && ResidentEligible(p))
                {
                    met++;
                    if (ResidentOf(p) != null)
                    {
                        _mindWho.Remove(p.Handle);   // the cache had them as nobody a moment ago
                        w = MindKnown(p.Handle, now);
                        _joinMet++;
                    }
                }
                if (w == null || w.Id == null || w.Src == null)
                    continue;   // the mind decides who joins: nobody it doesn't know
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                JoinTake take = JoinIn.Weigh(w.Id, t, !PED.IS_PED_MALE(p.Handle), InHisCups(p, w.P), sides, LadderWarmth, witnesses, law, saloon,
                                             _cfg.WomenInFights, _director.JoinerCount + _joinPending.Count, fight + "|" + w.Id, most, BrawlsOn, sure);
                if (take.Side >= 0)
                    takes.Add(new KeyValuePair<JoinTake, KeyValuePair<Ped, MindWho>>(take, new KeyValuePair<Ped, MindWho>(p, w)));
                else if (take.Why != null && take.Score > 0.2f)
                    Log.Detail("BRAWL " + w.Name + " stays out: " + take.Why);
            }
            int placed = 0, joinedNow = _director.JoinerCount + _joinPending.Count;
            foreach (var kv in takes.OrderByDescending(x => x.Key.Score))
            {
                if (placed >= wave)
                    break;
                JoinTake best = kv.Key;
                Ped bestPed = kv.Value.Key;
                MindWho bw = kv.Value.Value;
                string bestName = bw.Name;
                if (BrawlsOn && placed > 0)
                {
                    // weighed again against the sides as this wave has left them, so a wave doesn't all pile onto one man
                    best = JoinIn.Weigh(bw.Id, bw.Traits, !PED.IS_PED_MALE(bestPed.Handle), InHisCups(bestPed, bw.P), sides, LadderWarmth, witnesses, law, saloon,
                                        _cfg.WomenInFights, joinedNow, fight + "|" + bw.Id, most, BrawlsOn, sure);
                    if (best.Side < 0)
                        continue;
                }
                placed++;
                joinedNow++;
                List<int> theirs = best.Side == 0 ? b : a;
                if (BrawlsOn)
                {
                    JoinIn.Join(sides, best, new Fighter { Id = bw.Id, Woman = !PED.IS_PED_MALE(bestPed.Handle) });
                    (best.Side == 0 ? a : b).Add(bestPed.Handle);   // the handles kept beside the sides, index for index
                }
                int target = theirs[Math.Max(0, Math.Min(theirs.Count - 1, best.Target))];
                string with = NameOfFighter(best.Side == 0 ? a[0] : b[0]), at = NameOfFighter(target);
                string why = best.Why + "; " + witnesses + " watching" + (saloon ? ", a saloon" : "") + (sure ? ", a test" : "") + "; " + (a.Count + b.Count) + " fighting";
                if (PED.IS_PED_USING_ANY_SCENARIO(bestPed.Handle))
                {
                    TASK.CLEAR_PED_TASKS(bestPed.Handle, true, false);   // up off the bar the game's way first; in once they're on their feet
                    _joinPending.Add(new JoinPending { Handle = bestPed.Handle, Side = best.Side, Target = target, At = now + 1500, Until = now + 8000, Name = bestName, Why = with + "'s side and goes for " + at + " (" + why + ")" });
                    Log.Detail("BRAWL " + bestName + " gets up to join in, for " + with);
                    continue;
                }
                JoinAdd(bestPed.Handle, best.Side, target, bestName, with + "'s side and goes for " + at + " (" + why + ")", with, at);
            }
        }

        private bool JoinIsPending(int h)
        {
            foreach (JoinPending j in _joinPending)
                if (j.Handle == h)
                    return true;
            return false;
        }

        private void JoinAdd(int h, int side, int target, string name, string how, string with, string at)
        {
            if (!_director.AddFighter(h, side, target))
                return;
            _joinTotal++;
            _joinLast = name + " for " + with + ", at " + at;
            Log.Write("BRAWL joins: " + name + " takes " + how);
            Safe("join tie", () => NoteSceneTie("brawl", new List<Ped> { new Ped(h), new Ped(target) }));
        }

        /// Those getting up to join: in once they're off the bar or the chair, or dropped after 8 s still in it.
        private void JoinPendingStep(int now)
        {
            for (int i = _joinPending.Count - 1; i >= 0; i--)
            {
                JoinPending j = _joinPending[i];
                if (!ENTITY.DOES_ENTITY_EXIST(j.Handle) || ENTITY.IS_ENTITY_DEAD(j.Handle) || now > j.Until)
                {
                    _joinPending.RemoveAt(i);
                    continue;
                }
                if (now < j.At || PED.IS_PED_USING_ANY_SCENARIO(j.Handle))
                    continue;
                _joinPending.RemoveAt(i);
                if (ENTITY.DOES_ENTITY_EXIST(j.Target) && !ENTITY.IS_ENTITY_DEAD(j.Target))
                    JoinAdd(j.Handle, j.Side, j.Target, j.Name, j.Why, NameOfFighter(j.Target), NameOfFighter(j.Target));
            }
        }

        private Fighter FighterOf(int h)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, Game.GameTime) : null;
            return new Fighter { Id = w != null ? w.Id : null, Woman = ENTITY.DOES_ENTITY_EXIST(h) && !PED.IS_PED_MALE(h), On = _director.AttackersOn(h) };
        }

        private string NameOfFighter(int h)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, Game.GameTime) : null;
            return w != null && w.Name != null ? w.Name : "a stranger";
        }

        /// F7: who joined fights this session.
        private string JoinStatus()
        {
            if (!_cfg.MindLive || !_cfg.MindJoinFights)
                return "off ([Mind] MindJoinFights, with MindLive)";
            return _joinFailed ? "FAILED once this session (see the log)" : _joinTotal + " joined a fight this session" + (BrawlsOn ? ", the biggest " + _brawlBiggest + " fighting (cap " + _cfg.BrawlMost + "), " + _joinMet + " onlookers given a name to weigh" : "") + (_joinLast != null ? " (last: " + _joinLast + ")" : "");
        }
    }
}
