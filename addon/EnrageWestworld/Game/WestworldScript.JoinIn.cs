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
        // [Mind] BrawlJoinMetres (30): 12 until 2026-10-31, then 18 fixed (Lex, in busy Saint Denis: more of the street should join; later
        // that day "brawls reach further": a lawman 41 m off arrived to find it over)
        private float JoinMetres { get { return _cfg.BrawlJoinMetres; } }
        private const float JoinYouMetres = 18f;   // the brawl that comes at you (WestworldScript.JoinYou.cs) keeps its 18 m: those beside you, not the street
        private const float JoinWalkFrom = 10f, JoinArriveMetres = 6f;   // a joiner farther than this runs over first, and is in it once this close
        private const int JoinMeetMost = 6;     // strangers given a resident's identity on the spot in one look, the nearest first (the cost)
        private int _joinNextAt, _joinTotal, _joinMet;
        private bool _joinFailed;
        private string _joinLast;

        // [Mind] MindBrawls (Core/Brawl.cs): the cap, the waves, and the sat and the leaning who get up first
        private bool BrawlsOn { get { return _cfg.MindLive && _cfg.MindJoinFights && _cfg.MindBrawls; } }
        private int MostJoinersNow { get { return BrawlsOn ? Brawl.MostJoiners(_cfg.BrawlMost) : JoinIn.MostJoiners; } }
        private sealed class JoinPending { public int Handle, Side, Target, At, Until, Since; public bool Walk, Pulled; public string Name, Why, With; }   // Walk: running over from afar (events held till he is in it)
        private readonly List<JoinPending> _joinPending = new List<JoinPending>();
        private readonly Dictionary<int, int> _joinSideOf = new Dictionary<int, int>();   // this fight's picks: whose side each took, kept for the fight
        private int _brawlBiggest, _joinPendingAt, _joinSidesFor, _joinCameOver, _joinGaveUp;

        private int _joinFightSince;
        private const int BrawlOpeningMs = 10000;

        private void JoinStep(int now)
        {
            int least = _cfg.BrawlLeastSeconds * 1000;
            _director.BrawlLeastMs = least;   // [Mind] BrawlLeastSeconds: a brawl lasts at least this long from its first blows
            _director.FightMostMs = BrawlsOn ? Math.Max(45000, least + 15000) : 20000;
            _director.BraveryNow = _cfg.Bravery;   // the scene's own frights hold their ground by [World] Bravery
            _director.DownsOn = BrawlsOn;   // a man beaten down in a brawl: killed or knocked out, and it goes on (WestworldScript.Brawl.cs)
            if (_director.FleeOrdered == null)
                _director.FleeOrdered = BraveRelease;
            if (_director.FighterDown == null)
                _director.FighterDown = OnFighterDown;
            List<int> a, b;
            if (_joinPending.Count > 0 && now - _joinPendingAt >= 0)
            {
                // those on their way, every half second: in once they're close (or up off the bar), and counted for their
                // side meanwhile, so the fight doesn't end before they get there
                _joinPendingAt = now + 500;
                if (!_director.FightSides(out a, out b))
                    JoinPendingDrop("the fight is over");
                else
                    JoinPendingStep(now);
            }
            if (!_cfg.MindLive || !_cfg.MindJoinFights || _joinFailed || now < _joinNextAt)
                return;
            _joinNextAt = now + JoinEveryMs;
            if (!_director.FightSides(out a, out b))
            {
                JoinPendingDrop("the fight is over");
                return;
            }
            if (_joinSidesFor != _director.FightId)
            {
                _joinSidesFor = _director.FightId;
                _joinSideOf.Clear();   // a new fight: nobody's side is taken yet
                _joinFightSince = now;
            }
            // the first seconds of a fight draw people in more readily, so two at it can grow before one goes down (Lex,
            // 2026-11-01: someone may go down early, but bystanders more likely to join) ([Mind] BrawlOpeningBoost)
            JoinIn.OpeningBoost = now - _joinFightSince < BrawlOpeningMs && now >= _joinFightSince ? _cfg.BrawlOpeningBoost / 100f : 0f;
            if (a.Count + b.Count > _brawlBiggest)
                _brawlBiggest = a.Count + b.Count;
            if (!_director.FightSides(out a, out b))
                return;   // the sides again: those who got up to join are on them now, so the balance sees them (the review, 2026-10-31)
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
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Position.DistanceTo(mid) > JoinMetres + 12f)
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
                    JoinIsPending(h) || AimedLately(h, now) || IsStaged(h) || MindGettingUp(h) || WalkingUp(h) || InTalk(h))   // (an encounter's, a get-up, a walk-up, a talk: NIGHT4 item 6; not all of NotFree, whose onlookers are the joiners)
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
                int took;
                if (_joinSideOf.TryGetValue(bestPed.Handle, out took) && took != best.Side)
                {
                    Log.Detail("BRAWL " + bestName + " stays with the side he first took");   // picked before for the other side: nobody switches sides mid-fight
                    continue;
                }
                placed++;
                joinedNow++;
                _joinSideOf[bestPed.Handle] = best.Side;
                List<int> theirs = best.Side == 0 ? b : a;
                if (BrawlsOn)
                {
                    JoinIn.Join(sides, best, new Fighter { Id = bw.Id, Woman = !PED.IS_PED_MALE(bestPed.Handle) });
                    (best.Side == 0 ? a : b).Add(bestPed.Handle);   // the handles kept beside the sides, index for index
                }
                int target = theirs[Math.Max(0, Math.Min(theirs.Count - 1, best.Target))];
                string with = NameOfFighter(best.Side == 0 ? a[0] : b[0]), at = NameOfFighter(target);
                string why = best.Why + "; " + witnesses + " watching" + (saloon ? ", a saloon" : "") + (sure ? ", a test" : "") + "; " + (a.Count + b.Count) + " fighting";
                bool sat = PED.IS_PED_USING_ANY_SCENARIO(bestPed.Handle);
                float off = bestPed.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(target, true, true));
                if (sat || off > JoinWalkFrom)
                {
                    // up off the bar the game's way first, or a run over from afar: counted for his side on the way (the
                    // Director's Coming), in once he's close (Lex, 2026-10-31: nine got up to join, and it was over first)
                    var j = new JoinPending { Handle = bestPed.Handle, Side = best.Side, Target = target, Since = now, At = now + (sat ? 1500 : 0), Until = now + JoinWaitMs(off, sat),
                                              Name = bestName, With = with, Why = with + "'s side and goes for " + at + " (" + why + ")" };
                    _joinPending.Add(j);
                    if (sat)
                        ScenarioExit.SoftUp(bestPed.Handle, x => TASK.TASK_STAND_STILL(x, 6000));   // the exit flag and the next task the same tick, no bare clear (Game/ScenarioExit.cs)
                    else
                        JoinRunOver(j);
                    Log.Write("BRAWL " + bestName + (sat ? " gets up to join in" : " runs over to join in, " + off.ToString("0") + " m off") + ", for " + with);
                    continue;
                }
                JoinAdd(bestPed.Handle, best.Side, target, bestName, with + "'s side and goes for " + at + " (" + why + ")", with, at);
            }
            JoinComingCount();
        }

        /// How long one on his way may take: about 1.6 s a metre and 4 s more, 30 s at most (10 s at least for one getting up).
        private static int JoinWaitMs(float metres, bool sat)
        {
            return Math.Max(sat ? 10000 : 6000, Math.Min(30000, 4000 + (int)(metres * 1600f)));
        }

        /// He runs to the man he's going for; the fight's own events don't send him off on the way.
        private static void JoinRunOver(JoinPending j)
        {
            j.Walk = true;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(j.Handle, true);
            TASK.TASK_GO_TO_ENTITY(j.Handle, j.Target, 30000, 3f, JoinArriveMetres - 2f, 0f, 0);
        }

        /// The fight's sides learn who is on the way to each.
        private void JoinComingCount()
        {
            int c0 = 0, c1 = 0;
            foreach (JoinPending j in _joinPending)
                if (j.Side == 0)
                    c0++;
                else
                    c1++;
            _director.SetComing(c0, c1);
        }

        /// Everyone on the way let go to the game (the fight is over, or the add-on stopping).
        private void JoinPendingDrop(string why)
        {
            if (_joinPending.Count > 0)
            {
                foreach (JoinPending j in _joinPending)
                    JoinLetWalkerGo(j);
                Log.Detail("BRAWL " + _joinPending.Count + " on their way to join let go: " + why);
                _joinGaveUp += _joinPending.Count;
            }
            _joinPending.Clear();
            _director.SetComing(0, 0);
        }

        private static void JoinLetWalkerGo(JoinPending j)
        {
            if (!j.Walk || !ENTITY.DOES_ENTITY_EXIST(j.Handle) || ENTITY.IS_ENTITY_DEAD(j.Handle))
                return;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(j.Handle, false);
            TASK.CLEAR_PED_TASKS(j.Handle, true, false);
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

        /// Those on their way to join, every half second. One getting up: still on the bar or the chair 6 s on, taken off it
        /// at once (as the Director does for the two who began it); once up, in if he's close, else he runs over. One running
        /// over: in once he's within 6 m of the man he's going for (another of that side if that one is down). Each is
        /// dropped when his time is up (about 1.6 s a metre).
        private void JoinPendingStep(int now)
        {
            List<int> a, b;
            _director.FightSides(out a, out b);
            for (int i = _joinPending.Count - 1; i >= 0; i--)
            {
                JoinPending j = _joinPending[i];
                bool gone = !ENTITY.DOES_ENTITY_EXIST(j.Handle) || ENTITY.IS_ENTITY_DEAD(j.Handle);
                if (gone || now > j.Until || PED.IS_PED_IN_COMBAT(j.Handle, Game.Player.Character.Handle) || PED.IS_PED_FLEEING(j.Handle) || _director.Involves(j.Handle))
                {
                    if (!gone && !_director.Involves(j.Handle))
                        JoinLetWalkerGo(j);
                    _joinPending.RemoveAt(i);
                    if (!gone && now > j.Until)
                    {
                        _joinGaveUp++;
                        Log.Detail("BRAWL " + j.Name + " never got there (" + (now - j.Since) / 1000 + " s on the way)");
                    }
                    continue;
                }
                if (_director.IsOut(j.Target) || !ENTITY.DOES_ENTITY_EXIST(j.Target) || ENTITY.IS_ENTITY_DEAD(j.Target))
                {
                    List<int> theirs = j.Side == 0 ? b : a;
                    int t = theirs != null ? theirs.FirstOrDefault(x => x != j.Target && !_director.IsOut(x) && ENTITY.DOES_ENTITY_EXIST(x) && !ENTITY.IS_ENTITY_DEAD(x)) : 0;
                    if (t == 0)
                        continue;   // nobody over there to go for just now: he keeps coming, and AddFighter finds one
                    j.Target = t;
                    if (j.Walk)
                        JoinRunOver(j);
                }
                if (now < j.At)
                    continue;
                if (!j.Walk && PED.IS_PED_USING_ANY_SCENARIO(j.Handle))
                {
                    if (!j.Pulled && now - j.Since > 6000)
                    {
                        j.Pulled = true;
                        string how = ScenarioExit.Pull(j.Handle, x => JoinRunOver(j));   // the run the same tick: no idle pose between; the immediate clear only off camera (no T pose)
                        j.Until = Math.Max(j.Until, now + JoinWaitMs(ENTITY.GET_ENTITY_COORDS(j.Handle, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(j.Target, true, true)), false));   // the run's own time, as the path after a get-up gives it
                        Log.Write("BRAWL " + j.Name + " was still sat or leaning " + (now - j.Since) / 1000 + " s on: pulled out of it to join in (" + how + ")");
                    }
                    continue;
                }
                float off = ENTITY.GET_ENTITY_COORDS(j.Handle, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(j.Target, true, true));
                if (off > JoinArriveMetres)
                {
                    if (!j.Walk)
                    {
                        JoinRunOver(j);   // up off the bar, and the fight is across the street: he runs to it
                        j.Until = Math.Max(j.Until, now + JoinWaitMs(off, false));
                    }
                    continue;
                }
                _joinPending.RemoveAt(i);
                if (j.Walk)
                    _joinCameOver++;
                JoinAdd(j.Handle, j.Side, j.Target, j.Name, j.Why, j.With ?? NameOfFighter(j.Target), NameOfFighter(j.Target));
                if (!_director.Involves(j.Handle))
                    JoinLetWalkerGo(j);   // the fight wouldn't take him after all
            }
            JoinComingCount();
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
            return _joinFailed ? "FAILED once this session (see the log)" : _joinTotal + " joined a fight this session" + (BrawlsOn ? ", the biggest " + _brawlBiggest + " fighting (cap " + _cfg.BrawlMost + "), " + _joinMet + " onlookers given a name to weigh, a brawl lasts " + _cfg.BrawlLeastSeconds + " s at least" : "") + "; joining from within " + JoinMetres.ToString("0") + " m, " + _joinCameOver + " ran over from afar, " + _joinGaveUp + " never got there, " + _joinPending.Count + " on the way now; the ring " + _ring.Count + " now" + (_joinLast != null ? " (last: " + _joinLast + ")" : "");
        }
    }
}
