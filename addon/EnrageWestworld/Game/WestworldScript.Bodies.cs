// Lawmen clear the dead from the street (NIGHT3 item 6; roadmap 33; Lex, 2026-10-31). A body left lying in a town (dead
// 20 s or more, nobody carrying it, no fight or pursuit near, not the story's or another script's): the nearest free
// lawman on foot within 50 m walks over, lifts it with the game's own carry, takes it about 20 m off to a walkable spot
// away from the player, sets it down and goes back to his rounds. Up to three people near turn to watch him do it. One
// at a time, each body once, a minute between. Every step is logged (LAW clears:) since an NPC lifting a body is new:
// the log says where it stopped if the game won't let him. [World] LawClearsBodies. Testing row "A lawman clears a body".
// 2026-10-31 (Lex: he lifted it, the body stayed and the game took the officer back): he is taken over for the job
// (LawTake: ours, or lightly if another script has him), sent by the nav mesh with a watchdog, and the carry is done the
// game's own undertaker's way (the carriable flag on the body and reset flag 49 on him every frame, a walk with it, the
// set-down's flags 1).
// 2026-10-31 later (Lex's log: "lifts Jedediah Archer and carries it 17 m off", then 1 s on "he dropped it on the way"):
// the walk order went in mid-lift and cancelled it. Now step for step the undertaker's (script_rel/undertaker.c, func_13
// states 2-5, func_14, func_43): within 15 m one sequence walks up and lifts; the carry counts only once the body's
// carrier is him and its carry state is 5 or 7; then his tasks are cleared, he goes unarmed, the game's place order takes
// it towards the spot (flags 0), a nav-mesh sequence walks the rest, the place order with flags 1 sets it down, and it is
// done when the carry ends. Every step logs the carry state number.
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
        private sealed class BodyClear
        {
            public int Law, Body, Stage, Since, StageAt, CheckAt, Resent, Gone;   // Gone: steps in a row the body wasn't his
            public bool Reached;      // the lift sequence brought him to it (logged once)
            public float LastDist;    // how far he was at the last progress check (the watchdog)
            public bool Owned;        // another script had him: taken lightly (LawTake)
            public Vector3 Goal;
            public string Who;
        }

        // the game's own way, from its undertaker script (Lex, 2026-10-31): every frame of the carry the body is marked
        // carriable by a ped (carrying flag 7) and the carrier keeps reset flag 49; the set-down's flags are 1, not 0
        private const ulong SetCarryingFlag = 0xC3ABCFBC7D74AFA5;   // _SET_ENTITY_CARRYING_FLAG (by hash: wrapper builds name it differently)
        private const ulong CarryStateHash = 0x61914209C36EFDDB;    // the carriable's carry state (5 or 7: carried, settled); by hash, as the undertaker calls it
        private static readonly uint NavTask = Natives.Hash("SCRIPT_TASK_FOLLOW_NAV_MESH_TO_COORD");
        private static readonly uint SeqTask = Natives.Hash("SCRIPT_TASK_PERFORM_SEQUENCE");
        private static readonly uint PlaceTask = Natives.Hash("SCRIPT_TASK_PLACE_CARRIED_ENTITY_AT_COORD");
        private static readonly uint Unarmed = Natives.Hash("WEAPON_UNARMED");

        // the stages (the undertaker's func_13 state in brackets)
        private const int BcWalk = 0, BcLift = 1, BcUnarm = 2, BcPlaceWalk = 3, BcNavWalk = 4, BcSetDown = 5;

        private static void LawNavTo(int law, Vector3 to, float speed)
        {
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(law, to.X, to.Y, to.Z, speed, -1, 1f, 0, 0f);
        }

        /// The undertaker's state 2: one sequence walks up to the body and lifts it (no order between them to cancel the lift).
        private static unsafe void LawLiftSequence(int law, int body)
        {
            int seq = 0;
            TASK.OPEN_SEQUENCE_TASK(&seq);
            TASK.TASK_GO_TO_ENTITY(0, body, 20000, 0.5f, 1f, 2f, 0);
            TASK.TASK_PICKUP_CARRIABLE_ENTITY(0, body);
            TASK.CLOSE_SEQUENCE_TASK(seq);
            TASK.TASK_PERFORM_SEQUENCE(law, seq);
            TASK.CLEAR_SEQUENCE_TASK(&seq);
        }

        /// func_43 case 4 as func_46 sets it up (flag 128: no timeout; stopping range 0.25; nav flags 1; heading 40000, none).
        private static unsafe void LawCarryWalk(int law, Vector3 goal)
        {
            int seq = 0;
            TASK.OPEN_SEQUENCE_TASK(&seq);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(0, goal.X, goal.Y, goal.Z, 1f, -1, 0.25f, 1, 40000f);
            TASK.CLOSE_SEQUENCE_TASK(seq);
            TASK.TASK_PERFORM_SEQUENCE(law, seq);
            TASK.CLEAR_SEQUENCE_TASK(&seq);
        }

        private static unsafe bool LawUnarmed(int law)
        {
            uint a = 0, b = 0;
            WEAPON.GET_CURRENT_PED_WEAPON(law, &a, true, 0, false);
            WEAPON.GET_CURRENT_PED_WEAPON(law, &b, true, 1, false);
            return a == Unarmed && b == Unarmed;
        }

        private static int CarryState(int body)
        {
            return Function.Call<int>(CarryStateHash, body);
        }

        /// The undertaker's func_14: the body's carrier is him and the carry has settled (state 5 or 7). Not
        /// IS_PED_CARRYING_SOMETHING, which is true mid-lift (an order then cancelled the lift: Lex's log, 2026-10-31).
        private static bool CarrySettled(int law, int body)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(body) || PED._GET_CARRIER_AS_PED(body) != law)
                return false;
            int s = CarryState(body);
            return s == 5 || s == 7;
        }

        /// Every frame while a clearing runs (reset flags are a frame's): the undertaker's own, stage by stage. The body
        /// carriable by a ped (carrying flag 7) while he goes to it and lifts it (its states 2-3); reset flag 49 on him
        /// throughout (states 1-5); reset flag 179 while its walk helper runs (func_43).
        private void BodyClearFrame()
        {
            BodyClear c = _bodyClear;
            if (c == null || !ENTITY.DOES_ENTITY_EXIST(c.Body) || !ENTITY.DOES_ENTITY_EXIST(c.Law))
                return;
            if (c.Stage <= BcLift)
                Function.Call(SetCarryingFlag, c.Body, 7, true);
            PED.SET_PED_RESET_FLAG(c.Law, 49, true);
            if (c.Stage >= BcUnarm && c.Stage <= BcNavWalk)
                PED.SET_PED_RESET_FLAG(c.Law, 179, true);
        }

        /// "carry state 7, carried by him" (or nobody, or someone else).
        private static string CarryWords(int law, int body)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(body))
                return "the body is gone";
            int by = PED._GET_CARRIER_AS_PED(body);
            return "carry state " + CarryState(body) + ", carried by " + (by == law ? "him" : by == 0 ? "nobody" : "someone else");
        }

        private string LawState(BodyClear c)
        {
            int law = c.Law;
            uint task = c.Stage == BcWalk ? NavTask : c.Stage == BcPlaceWalk || c.Stage == BcSetDown ? PlaceTask : SeqTask;
            string order = c.Stage == BcWalk ? "the walk's" : c.Stage == BcPlaceWalk || c.Stage == BcSetDown ? "the place order's" : "the sequence's";
            return "a mission entity " + (ENTITY.IS_ENTITY_A_MISSION_ENTITY(law) ? "yes" : "no") + ", ours " + (Spawner.IsBorrowed(law) ? "yes" : "no") +
                   ", in a scenario " + (PED.IS_PED_USING_ANY_SCENARIO(law) ? "yes" : "no") + ", " + order + " status " + TASK.GET_SCRIPT_TASK_STATUS(law, task, true) +
                   (task == SeqTask ? " (step " + TASK.GET_SEQUENCE_PROGRESS(law) + ")" : "") + ", " + CarryWords(law, c.Body);
        }

        private BodyClear _bodyClear;
        private readonly Dictionary<int, int> _bodyFirstSeen = new Dictionary<int, int>();   // body handle -> when first seen dead
        private readonly HashSet<int> _bodiesTaken = new HashSet<int>();
        private int _bodyNextLook, _bodyNextStart, _bodyClears, _bodyFails;
        private string _bodyLast;
        private Action _aBodies;

        private int _bodyStepAt, _bodyRetryAt;

        private void BodiesStep(int now)
        {
            if (_aBodies == null)
                _aBodies = () => BodiesTick(_now, false);
            if (_cfg.LawClearsBodies || _bodyClear != null)
                Safe("law clears bodies", _aBodies);
        }

        /// The clearing under way stepped; else, now and then, a body that wants clearing looked for.
        private string BodiesTick(int now, bool test)
        {
            if (_bodyClear != null)
            {
                BodyClearFrame();
                if (now - _bodyStepAt >= 250 || now < _bodyStepAt)
                {
                    _bodyStepAt = now;   // a quarter second is plenty for a walk and a carry: its stages only read state (the cost audit)
                    BodyClearStep(_bodyClear, now);
                }
                return test ? "a lawman is already clearing a body" : null;
            }
            if (!test && (now < _bodyNextLook || now < _bodyNextStart || now < _bodyRetryAt))
                return null;
            _bodyNextLook = now + 3000;
            if (MISC.GET_MISSION_FLAG() || _missionPaused)
                return "a mission";
            if (!test && _placeTown == null)
                return "out of the towns";
            Ped me = Game.Player.Character;
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0))
                return "you're wanted or fighting";
            Ped body = null;
            Vector3 mePos = me.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(mePos) > 40f || !p.Exists() || !p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;   // (the distance first: the cost audit)
                int h = p.Handle;
                int first;
                if (!_bodyFirstSeen.TryGetValue(h, out first))
                {
                    _bodyFirstSeen[h] = now;
                    first = now;
                }
                if (_bodiesTaken.Contains(h) || (!test && now - first < 20000) || Natives.StoryModel(p) || Natives.OtherScripts(h) || _director.Involves(h))
                    continue;
                int carried = PED.IS_PED_CARRYING_SOMETHING(me.Handle) ? PED._GET_FIRST_ENTITY_PED_IS_CARRYING(me.Handle) : 0;
                if (carried == h || ENTITY.IS_ENTITY_IN_WATER(h))
                    continue;
                if (body == null || p.Position.DistanceTo(me.Position) < body.Position.DistanceTo(me.Position))
                    body = p;
            }
            if (_bodyFirstSeen.Count > 200)
                foreach (int k in _bodyFirstSeen.Keys.Where(k => !ENTITY.DOES_ENTITY_EXIST(k)).ToList())
                    _bodyFirstSeen.Remove(k);
            if (body == null)
                return "no body lying in the street within 40 m" + (test ? "" : " for 20 s or more");
            Vector3 bodyAt = body.Position;
            float lawMetres = test ? 80f : 50f;
            if (World.GetAllPeds().Any(x => x != null && x.Position.DistanceTo(bodyAt) < 35f && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_IN_COMBAT(x.Handle, 0)))
            {
                _bodyRetryAt = now + 20000;   // a fight near it: look again in a while, not every 3 s (the cost audit)
                return "a fight near the body";
            }
            Ped law = World.GetAllPeds().Where(x => x != null && x.Position.DistanceTo(bodyAt) < lawMetres && x.Exists() && !x.IsDead && Natives.IsLaw(x) && !PED.IS_PED_ON_MOUNT(x.Handle) &&
                                                    !PED.IS_PED_IN_ANY_VEHICLE(x.Handle, false) && !PED.IS_PED_IN_COMBAT(x.Handle, 0) && !_director.Involves(x.Handle) && !NotFree(x.Handle) &&
                                                    !Spawner.IsBorrowed(x.Handle) && !_brawlLaw.Exists(b => b.Law == x.Handle))   // not one already on another job of ours
                                        .OrderBy(x => x.Position.DistanceTo(bodyAt)).FirstOrDefault();
            if (law == null)
            {
                _bodyRetryAt = now + 20000;   // no lawman near: look again in a while
                return "no lawman on foot within " + (test ? 80 : 50) + " m of the body";
            }
            Vector3 goal;
            if (!BodyGoal(body.Position, me.Position, out goal))
                return "nowhere off the street to take it";
            float far = law.Position.DistanceTo(body.Position);
            _bodyClear = new BodyClear { Law = law.Handle, Body = body.Handle, Stage = 0, Since = now, StageAt = now, Goal = goal, Who = NameToShow(body.Handle) ?? "a body", LastDist = far, CheckAt = now + 3000 };
            _bodiesTaken.Add(body.Handle);
            _bodyClear.Owned = LawTake(law.Handle);   // ours while he works: the game took him back before (Lex, 2026-10-31)
            TASK.CLEAR_PED_TASKS(law.Handle, true, false);
            LawNavTo(law.Handle, body.Position, far > 12f ? 2f : 1f);   // by the nav mesh to where it lies, not a go-to on a dead body
            Log.Write("LAW clears: a lawman " + (int)far + " m off goes to " + _bodyClear.Who + " to take it off the street (" + LawTakeWords(_bodyClear.Owned) + ")" + (test ? " (the Testing row)" : ""));
            RecordRef found = _bridge.RecordAt(body.Handle);
            if (found != null)
                _engine.BodyFound(found.Id, "a lawman carried the body off", Bridge.Now());   // a killing nobody saw: his kin's word sets off now
            return null;
        }

        /// Somewhere about 20 m off, walkable, further from the player than the body is.
        private bool BodyGoal(Vector3 from, Vector3 player, out Vector3 goal)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 g;
                if (FarPoint(from, 16f + i, out g) && g.DistanceTo(player) > from.DistanceTo(player))
                {
                    goal = g;
                    return true;
                }
            }
            return FarPoint(from, 18f, out goal);
        }

        private void BodyClearStep(BodyClear c, int now)
        {
            string fail = null;
            if (!ENTITY.DOES_ENTITY_EXIST(c.Law) || ENTITY.IS_ENTITY_DEAD(c.Law)) fail = "no lawman any more";
            else if (!ENTITY.DOES_ENTITY_EXIST(c.Body)) fail = "the body is gone";
            else if (PED.IS_PED_IN_COMBAT(c.Law, 0) || MISC.GET_MISSION_FLAG()) fail = "a fight or a mission";
            else if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0) fail = "you're wanted: he's the law's again";
            else if (now - c.Since > 120000) fail = "it took too long";
            if (fail != null)
            {
                BodyClearEnd(c, fail, false);
                return;
            }
            Vector3 lawAt = ENTITY.GET_ENTITY_COORDS(c.Law, true, true), bodyAt = ENTITY.GET_ENTITY_COORDS(c.Body, true, true);
            bool his = PED._GET_CARRIER_AS_PED(c.Body) == c.Law;
            c.Gone = his || c.Stage <= BcLift ? 0 : c.Gone + 1;   // two steps in a row (half a second) before it counts as dropped
            switch (c.Stage)
            {
                case BcWalk:   // walking over by the nav mesh (the undertaker's state 1: its walk stops 15 m short)
                    {
                        float d = lawAt.DistanceTo(bodyAt);
                        if (d < 15f)
                        {
                            if (ENTITY.IS_ENTITY_ATTACHED_TO_ANY_PED(c.Body))
                            {
                                BodyClearEnd(c, "someone else has hold of it (" + CarryWords(c.Law, c.Body) + ")", false);
                                break;
                            }
                            TASK.CLEAR_PED_TASKS(c.Law, true, false);
                            LawLiftSequence(c.Law, c.Body);
                            Log.Detail("LAW clears: he is " + (int)d + " m from " + c.Who + " after " + (now - c.StageAt) / 1000 + " s: one order to walk up and lift it (the undertaker's sequence; " + CarryWords(c.Law, c.Body) + ")");
                            c.Stage = BcLift;
                            c.StageAt = now;
                            c.Reached = false;
                        }
                        else if (now - c.StageAt > 45000)
                            BodyClearEnd(c, "he never reached it, " + (int)d + " m off (" + LawState(c) + ")", false);
                        else if (now >= c.CheckAt)
                            BodyWatch(c, d, bodyAt, d > 12f ? 2f : 1f, "on his way to it", now);
                        break;
                    }
                case BcLift:   // the sequence walks up and lifts (its state 3: until func_14)
                    {
                        float d = lawAt.DistanceTo(bodyAt);
                        if (!c.Reached && (d < 2f || his))
                        {
                            c.Reached = true;
                            Log.Detail("LAW clears: he reaches " + c.Who + " after " + (now - c.StageAt) / 1000 + " s and bends to lift it (" + CarryWords(c.Law, c.Body) + ")");
                            BodyWatchers(bodyAt, c.Law, c.Body);
                        }
                        if (CarrySettled(c.Law, c.Body))
                        {
                            Log.Write("LAW clears: the lawman lifts " + c.Who + " (" + CarryWords(c.Law, c.Body) + ", settled after " + (now - c.StageAt) / 1000 +
                                      " s) and carries it " + (int)lawAt.DistanceTo(c.Goal) + " m off the street");
                            TASK.CLEAR_PED_TASKS(c.Law, true, false);   // the undertaker's state 3 to 4, then its walk helper (func_43 cases 0-2)
                            TASK.CLEAR_PED_SECONDARY_TASK(c.Law);
                            WEAPON.SET_CURRENT_PED_WEAPON(c.Law, Unarmed, false, 0, false, false);
                            WEAPON.SET_CURRENT_PED_WEAPON(c.Law, Unarmed, false, 1, false, false);
                            TASK.TASK_SWAP_WEAPON(c.Law, 1, 0, 0, 0);
                            c.Stage = BcUnarm;
                            c.StageAt = now;
                        }
                        else if (now - c.StageAt > 1500 && TASK.GET_SCRIPT_TASK_STATUS(c.Law, SeqTask, true) == 8 && !his)
                            BodyClearEnd(c, "the game wouldn't let him lift it: the walk-up-and-lift sequence ended with no carry (" + LawState(c) + ")", false);
                        else if (now - c.StageAt > 30000)
                            BodyClearEnd(c, "the game wouldn't let him lift it (no settled carry after 30 s; " + LawState(c) + ")", false);
                        break;
                    }
                case BcUnarm:   // func_43 case 3: unarmed, or 4 s, then the game's place order towards the spot (flags 0)
                    if (c.Gone >= 2)
                        BodyDropped(c, bodyAt);
                    else if (LawUnarmed(c.Law) || now - c.StageAt > 4000)
                    {
                        TASK.TASK_PLACE_CARRIED_ENTITY_AT_COORD(c.Law, c.Body, c.Goal.X, c.Goal.Y, c.Goal.Z, 1f, 0);
                        Log.Detail("LAW clears: hands free after " + (now - c.StageAt) / 1000 + " s; the place order takes it towards the spot, " + (int)lawAt.DistanceTo(c.Goal) +
                                   " m off (flags 0; " + CarryWords(c.Law, c.Body) + ")");
                        c.Stage = BcPlaceWalk;
                        c.StageAt = now;
                    }
                    break;
                case BcPlaceWalk:   // func_43 case 4: until the place order ends, or 15 s; then the nav-mesh sequence
                    if ((now - c.StageAt > 500 && TASK.GET_SCRIPT_TASK_STATUS(c.Law, PlaceTask, true) == 8) || now - c.StageAt > 15000)
                    {
                        float d = lawAt.DistanceTo(c.Goal);
                        if (!his)
                        {
                            float off = bodyAt.DistanceTo(c.Goal);   // the place order set it down itself
                            BodyClearEnd(c, (off < 4f ? "set down off the street" : "set down short of the spot (" + (int)off + " m)") + " by the place order (" + CarryWords(c.Law, c.Body) + ")", true);
                            break;
                        }
                        Log.Detail("LAW clears: the place order ended after " + (now - c.StageAt) / 1000 + " s, " + (int)d + " m from the spot, still carrying (" +
                                   CarryWords(c.Law, c.Body) + "); he walks the rest by the nav mesh");
                        LawCarryWalk(c.Law, c.Goal);
                        c.Stage = BcNavWalk;
                        c.StageAt = now;
                        c.Resent = 0;
                        c.LastDist = d;
                        c.CheckAt = now + 3000;
                    }
                    break;
                case BcNavWalk:   // func_43 case 5: there (within 5 m) or the sequence over, then the undertaker's state 4
                    {
                        float d = lawAt.DistanceTo(c.Goal);
                        if (c.Gone >= 2)
                            BodyDropped(c, bodyAt);
                        else if ((d < 5f && Math.Abs(lawAt.Z - c.Goal.Z) < 2f) || (now - c.StageAt > 500 && TASK.GET_SCRIPT_TASK_STATUS(c.Law, SeqTask, true) == 8) ||
                                 now - c.StageAt > 40000 || (now >= c.CheckAt && c.Resent >= 2 && c.LastDist - d < 1f))
                            BodySetDown(c, d, lawAt, now);   // there, or as near as he'll get
                        else if (now >= c.CheckAt)
                            BodyWatch(c, d, c.Goal, 1f, "carrying it", now);
                        break;
                    }
                case BcSetDown:   // the undertaker's state 5 (func_14 false), here once the body is out of his hands: the carry state leaves 5/7 as he bends
                    if (!his)
                    {
                        float off = bodyAt.DistanceTo(c.Goal);
                        BodyClearEnd(c, (off < 4f ? "set down off the street" : "set down short of the spot (" + (int)off + " m)") + " (" + CarryWords(c.Law, c.Body) + ")", true);
                    }
                    else if (now - c.StageAt > 10000)
                    {
                        TASK.CLEAR_PED_TASKS(c.Law, true, false);   // never left holding it
                        BodyClearEnd(c, "dropped where he stood: the set-down didn't finish in 10 s (" + (int)bodyAt.DistanceTo(c.Goal) + " m from the spot; " + CarryWords(c.Law, c.Body) + ")",
                                     bodyAt.DistanceTo(c.Goal) < 4f);
                    }
                    break;
            }
        }

        private void BodyDropped(BodyClear c, Vector3 bodyAt)
        {
            float off = bodyAt.DistanceTo(c.Goal);
            BodyClearEnd(c, "he dropped it on the way, " + (int)off + " m short of the spot (" + LawState(c) + ")", off < 4f);
        }

        /// The watchdog: no metre gained in 3 s and he's sent again (twice at most); after that, on the way to the body, the
        /// game has him (with the carry, the set-down follows wherever he is).
        private void BodyWatch(BodyClear c, float d, Vector3 to, float speed, string doing, int now)
        {
            c.CheckAt = now + 3000;
            if (c.LastDist - d >= 1f)
            {
                c.LastDist = d;
                return;
            }
            c.LastDist = d;
            if (c.Resent >= 2)
            {
                if (c.Stage == BcWalk)
                    BodyClearEnd(c, "the game kept him: stopped " + (int)d + " m off after being sent three times (" + LawState(c) + ")", false);
                return;
            }
            c.Resent++;
            Log.Write("LAW clears: he stopped " + doing + ", " + (int)d + " m off (" + LawState(c) + "); sent again");
            if (c.Stage == BcNavWalk)
                LawCarryWalk(c.Law, to);   // the same sequence again (no clear: the carry stays)
            else
                LawNavTo(c.Law, to, speed);
        }

        private void BodySetDown(BodyClear c, float d, Vector3 lawAt, int now)
        {
            Vector3 put = d < 5f ? c.Goal : lawAt;
            TASK.TASK_PLACE_CARRIED_ENTITY_AT_COORD(c.Law, c.Body, put.X, put.Y, put.Z, 1f, 1);   // the game's own undertaker: 1f, flags 1
            Log.Detail("LAW clears: he sets " + c.Who + " down " + (d < 5f ? "at the spot" : (int)d + " m short of it") + " (place order, flags 1; " + CarryWords(c.Law, c.Body) + ")");
            c.Stage = BcSetDown;
            c.StageAt = now;
        }

        /// Up to three people near turn to watch the lawman at his work.
        private void BodyWatchers(Vector3 at, int law, int body)
        {
            Ped me = Game.Player.Character;
            int n = 0;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != law && x.Handle != body && x.Handle != me.Handle && x.Position.DistanceTo(at) < 18f)
                                                .OrderBy(x => x.Position.DistanceTo(at)))
            {
                if (!PED.IS_PED_HUMAN(p.Handle) || NotFree(p.Handle) || MayDirectWhyNot(p) != null)
                    continue;
                TASK.TASK_LOOK_AT_ENTITY(p.Handle, law, 6000, 0, 51, 0);
                if (++n >= 3)
                    break;
            }
        }

        private void BodyClearEnd(BodyClear c, string why, bool done)
        {
            _bodyClear = null;
            _bodyNextStart = Game.GameTime + 60000;
            if (ENTITY.DOES_ENTITY_EXIST(c.Law) && !ENTITY.IS_ENTITY_DEAD(c.Law))
            {
                if (PED.IS_PED_CARRYING_SOMETHING(c.Law))
                    TASK.CLEAR_PED_TASKS(c.Law, true, false);   // never left holding it
                if (Spawner.IsBorrowed(c.Law))
                    LawGiveBack(c.Law, Game.GameTime + 1500);   // back to the game and his rounds (Spawner.Release: unblocked, wandering)
                else
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(c.Law, false);
                    WanderLater(c.Law, Game.GameTime + 4000);
                }
            }
            if (done)
                _bodyClears++;
            else
                _bodyFails++;
            _bodyLast = c.Who + ": " + why;
            Log.Write("LAW clears: " + c.Who + " - " + why + " (" + (Game.GameTime - c.Since) / 1000 + " s)");
        }

        private string BodiesStatus()
        {
            return (_cfg.LawClearsBodies ? "on" : "off ([World] LawClearsBodies)") + "; this session " + _bodyClears + " cleared, " + _bodyFails + " not" +
                   (_bodyClear != null ? "; one under way (" + _bodyClear.Who + ", stage " + _bodyClear.Stage + ")" : "") + (_bodyLast != null ? "; last: " + _bodyLast : "");
        }
    }
}
