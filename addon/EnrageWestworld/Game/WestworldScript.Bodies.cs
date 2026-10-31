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
// 2026-10-31, 15:33 (Lex: "picked up, stood there a while, dropped it, walked on"; the log: the place order and the nav
// sequence both ran, status 1 step 0, and he gained not a metre in 16): the tasking matched the undertaker's call for
// call, so the spot was the fault: a random bearing 16-23 m from the body, never checked he could get there (Rockstar
// sends a carrier only to hand-placed coords). Now the spot is Lex's: about [Mind] PickMetres - 3 m from the player
// (12 m at least), on the far side of the body from the player, walkable, not inside, near the body's level, a spot by a
// wall first; a body already that far is carried 3-7 m further. Up to four such spots are found (again as the carry
// settles, from where the player is then); one he stands still on (2.5 s, then every 3 s: under 0.2 m/s and under a
// metre gained) gives the next, by the nav mesh, after two failures straight if the line is clear, and only when every
// spot fails is it set down where he stands ("no reachable spot"). Every check logs his speed and the metres gained.
// 2026-10-31, the review: a body inside PickMetres - 1 goes to the edge (never past PickMetres - 1), one past it 3-7 m
// further; the settle-time spots are found from where it lay (not his shoulder: a porch roof read as ground); a check
// counts as slow only when he is slow too (a detour gains little in a line), the per-spot cap grows with the walk; a
// give-up is a failure, not a clearing; the safe spot tries flags 2 first; the ground fallback no longer asks the safe
// spot a third time, and after three misses in a row the safe spot isn't asked at all.
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
            public List<BodySpot> Spots;   // where it may go, best first (BodyGoal)
            public int SpotIx, Fails;      // the spot he is on; the spots he stood still on
            public bool TriedStraight;     // a straight walk was tried (once two spots failed)
            public bool Straight;          // the order now is the straight walk
            public int LastCheckAt, CarryAt;   // the last progress check; the carry settled
            public float OrderDist;        // metres to the spot when the order went
            public int Slow;               // checks in a row under a metre gained
            public Vector3 LiftedAt;       // where he stood as the carry settled
            public Vector3 BodyStart;      // where it lay (its Z the street's, for the settle-time spots: the review)
            public bool GaveUp;            // set down where he stood (BodyGiveUp): a failure, not a clearing
        }

        private sealed class BodySpot
        {
            public Vector3 At;
            public string How;   // "the safe spot (flags 2)", "the ground", plus "by a wall"
        }

        // the game's own way, from its undertaker script (Lex, 2026-10-31): while he goes to the body and lifts it the body
        // is marked carriable (0xC3ABCFBC7D74AFA5 with 7, as the undertaker calls it), and the carrier keeps reset flag 49;
        // the set-down's flags are 1, not 0
        private const ulong SetCarryingFlag = 0xC3ABCFBC7D74AFA5;   // no known name (the real _SET_ENTITY_CARRYING_FLAG is 0x18FF3110CF47115D); the undertaker's own call
        private const ulong CarryStateHash = 0x61914209C36EFDDB;    // the carriable's carry state (5 CARRIED_BY_HUMAN, 7 CARRIED_BY_MOUNT); by hash, as the undertaker calls it
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

        /// func_43's other form (its flag 16): straight at the spot, in a sequence (radius 0.25, 20 s, no heading).
        private static unsafe void LawCarryStraight(int law, Vector3 goal)
        {
            int seq = 0;
            TASK.OPEN_SEQUENCE_TASK(&seq);
            TASK.TASK_GO_STRAIGHT_TO_COORD(0, goal.X, goal.Y, goal.Z, 1f, 20000, 40000f, 0.25f, 0);
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

        /// The undertaker's func_14: the body's carrier is him and the carry state is 5 (CARRIED_BY_HUMAN) or 7
        /// (CARRIED_BY_MOUNT; the undertaker's own test, kept). Not
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

        private Action _aBodyFrame;

        /// Every frame from Tick04, before the player-control gate (the pre-release review): the clearing's reset flags last
        /// a single frame, so they are set while the controls are off too (a shop's catalogue, the barber, a supper's or a
        /// tryst's dark), when the steps below don't run.
        private void BodyClearFrameStep()
        {
            if (_bodyClear == null)
                return;
            if (_aBodyFrame == null)
                _aBodyFrame = BodyClearFrame;
            Safe("law clears bodies", _aBodyFrame);
        }

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
                int gap = now - _bodyStepAt;
                if (!test && gap > 1000)
                {
                    // no step for over a second: the player's controls were off, and the stages' clocks stood still with them
                    _bodyClear.Since += gap - 250;
                    _bodyClear.StageAt += gap - 250;
                    _bodyClear.CheckAt += gap - 250;
                    _bodyClear.LastCheckAt += gap - 250;
                    _bodyClear.CarryAt += gap - 250;
                }
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
            List<BodySpot> spots;
            string goalWhy;
            if (!BodyGoal(body.Position, me.Position, law.Position, out spots, out goalWhy))
            {
                _bodyRetryAt = now + 20000;
                return "nowhere to take it (" + goalWhy + ")";
            }
            float far = law.Position.DistanceTo(body.Position);
            _bodyClear = new BodyClear { Law = law.Handle, Body = body.Handle, Stage = 0, Since = now, StageAt = now, Goal = spots[0].At, Spots = spots, BodyStart = body.Position, Who = NameToShow(body.Handle) ?? "a body", LastDist = far, CheckAt = now + 3000 };
            _bodyStepAt = now;   // the steps' clock starts here (a gap over a second is the controls off: BodiesTick)
            _bodiesTaken.Add(body.Handle);
            _bodyClear.Owned = LawTake(law.Handle);   // ours while he works: the game took him back before (Lex, 2026-10-31)
            TASK.CLEAR_PED_TASKS(law.Handle, true, false);
            LawNavTo(law.Handle, body.Position, far > 12f ? 2f : 1f);   // by the nav mesh to where it lies, not a go-to on a dead body
            Log.Write("LAW clears: a lawman " + (int)far + " m off goes to " + _bodyClear.Who + " to take it off the street (" + LawTakeWords(_bodyClear.Owned) + "; " + goalWhy + ")" + (test ? " (the Testing row)" : ""));
            RecordRef found = _bridge.RecordAt(body.Handle);
            if (found != null)
                _engine.BodyFound(found.Id, "a lawman carried the body off", Bridge.Now());   // a killing nobody saw: his kin's word sets off now
            return null;
        }

        private static readonly int[] SpotAngles = { 0, 20, -20, 40, -40, 60, -60 };
        // the only values the game's own scripts pass (their RDR2 meanings unverified); 2 first, since in GTA V's enum 2 is
        // NOT_ISOLATED (no cut-off navmesh island: a fenced yard, a roof) and 16 ONLY_NETWORK_SPAWN (the review)
        private static readonly int[] SpotFlags = { 2, 16 };

        /// The ground under (x, y) from 2.5 m above `level` (under a boardwalk's porch roof), never inside. Not
        /// Natives.StandSpot, which asks the safe spot a third time first (the cost review).
        private static unsafe bool BodyGround(Vector3 near, float level, out Vector3 found)
        {
            found = near;
            ulong slot = 0;   // a full 8-byte slot for the float out-parameter
            if (!MISC.GET_GROUND_Z_FOR_3D_COORD(near.X, near.Y, level + 2.5f, (float*)&slot, false))
                return false;
            float g = *(float*)&slot;
            if (g < -200f || INTERIOR.GET_INTERIOR_AT_COORDS(near.X, near.Y, g + 1f) != 0)
                return false;
            found = new Vector3(near.X, near.Y, g);
            return true;
        }

        /// Lex's spot (2026-10-31): about [Mind] PickMetres - 3 m from the player (12 m at least), on the far side of the
        /// body from the player (from the lawman's side when it lies at the player's feet), never past PickMetres - 1; a
        /// body already past that goes 3-7 m further (never back towards the player). Each try on rings round the
        /// player, within 60 degrees of that line, snapped by the game's safe spot (flags 2, then 16; not asked once it has
        /// missed three tries running) or the ground; refused inside, more than 3 m above or below the body, within 4 m of
        /// it or out of the range. Up to six found, four kept, a spot by a wall first. `bodyAt`: where it lies on the
        /// ground (not on his shoulder). `why`: what was found, for the log.
        private bool BodyGoal(Vector3 bodyAt, Vector3 player, Vector3 lawAt, out List<BodySpot> spots, out string why)
        {
            spots = new List<BodySpot>();
            Vector3 dir = new Vector3(bodyAt.X - player.X, bodyAt.Y - player.Y, 0f);
            float lies = dir.Length();
            if (lies < 2f)
                dir = new Vector3(bodyAt.X - lawAt.X, bodyAt.Y - lawAt.Y, 0f);
            if (dir.Length() < 0.5f)
                dir = new Vector3(1f, 0f, 0f);
            dir = dir.Normalized;
            float edge = Math.Max(12f, _cfg.PickMetres - 3f);
            float r, lo, hi;
            if (lies <= Math.Max(edge, _cfg.PickMetres - 1f))
            {
                // inside the edge: to the edge, never past PickMetres - 1 (the review: 20 m off went to 28-31 m before)
                r = edge;
                lo = Math.Max(12f, edge - 4f);
                hi = Math.Max(lo + 2f, Math.Min(edge + 3f, _cfg.PickMetres - 1f));
            }
            else
            {
                // at the edge or past it: a little further, never back towards the player
                r = lies + 5f;
                lo = lies + 3f;
                hi = r + 2f;
            }
            float[] rings = { r, Math.Max(lo, r - 2f), Math.Min(hi, r + 2f) };
            int tried = 0, inside = 0, level = 0, range = 0, safeMiss = 0;
            foreach (int a in SpotAngles)
            {
                double rad = a * Math.PI / 180.0;
                float cos = (float)Math.Cos(rad), sin = (float)Math.Sin(rad);
                Vector3 d = new Vector3(dir.X * cos - dir.Y * sin, dir.X * sin + dir.Y * cos, 0f);
                foreach (float ring in rings)
                {
                    if (spots.Count >= 6)
                        break;
                    Vector3 p = new Vector3(player.X + d.X * ring, player.Y + d.Y * ring, bodyAt.Z);
                    tried++;
                    Vector3 at = p;
                    string how = null;
                    if (safeMiss < 3)
                    {
                        foreach (int fl in SpotFlags)
                            if (Natives.SafeCoord(p, out at, fl))
                            {
                                how = "the safe spot (flags " + fl + ")";
                                break;
                            }
                        safeMiss = how == null ? safeMiss + 1 : 0;
                    }
                    if (how == null && BodyGround(p, bodyAt.Z, out at))
                        how = "the ground";
                    if (how == null)
                        continue;
                    float fromYou = new Vector3(at.X - player.X, at.Y - player.Y, 0f).Length();
                    if (INTERIOR.GET_INTERIOR_AT_COORDS(at.X, at.Y, at.Z + 1f) != 0)
                        inside++;
                    else if (Math.Abs(at.Z - bodyAt.Z) > 3f)
                        level++;
                    else if (fromYou < lo || fromYou > hi || at.DistanceTo(bodyAt) < 4f || spots.Exists(s => s.At.DistanceTo(at) < 3f))
                        range++;
                    else
                        spots.Add(new BodySpot { At = at, How = how + " at " + a + " degrees" });
                }
            }
            if (spots.Count == 0)
            {
                why = "no spot " + (int)lo + "-" + (int)hi + " m from you: " + tried + " tried, " + inside + " inside, " + level + " off the body's level, " + range + " out of range or too near it";
                return false;
            }
            // a spot by a wall first (off the open street): four short rays at waist height
            int walled = 0;
            foreach (BodySpot s in spots)
            {
                Vector3 w = s.At + new Vector3(0f, 0f, 0.8f);
                for (int k = 0; k < 4; k++)
                {
                    double ang = k * Math.PI / 2;
                    if (Natives.Blocked(w, w + new Vector3((float)Math.Cos(ang) * 2.5f, (float)Math.Sin(ang) * 2.5f, 0f), 0))
                    {
                        s.How += ", by a wall";
                        walled++;
                        break;
                    }
                }
            }
            spots = spots.Where(s => s.How.EndsWith(", by a wall")).Concat(spots.Where(s => !s.How.EndsWith(", by a wall"))).Take(4).ToList();
            why = spots.Count + " spot" + (spots.Count == 1 ? "" : "s") + " " + (int)lo + "-" + (int)hi + " m from you (the body lies " + (int)lies + " m off; aiming at " + (int)r + " m), " +
                  walled + " by a wall; the first " + (int)FlatFrom(spots[0].At, player) + " m from you, " + (int)spots[0].At.DistanceTo(bodyAt) + " m from the body, by " + spots[0].How;
            return true;
        }

        private static float FlatFrom(Vector3 a, Vector3 b)
        {
            return new Vector3(a.X - b.X, a.Y - b.Y, 0f).Length();
        }

        private void BodyClearStep(BodyClear c, int now)
        {
            string fail = null;
            if (!ENTITY.DOES_ENTITY_EXIST(c.Law) || ENTITY.IS_ENTITY_DEAD(c.Law)) fail = "no lawman any more";
            else if (!ENTITY.DOES_ENTITY_EXIST(c.Body)) fail = "the body is gone";
            else if (PED.IS_PED_IN_COMBAT(c.Law, 0) || MISC.GET_MISSION_FLAG()) fail = "a fight or a mission";
            else if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0) fail = "you're wanted: he's the law's again";
            else if (c.Stage < BcUnarm && now - c.Since > 120000) fail = "it took too long";   // (the carry has its own clock: below)
            if (fail != null)
            {
                BodyClearEnd(c, fail, false);
                return;
            }
            Vector3 lawAt = ENTITY.GET_ENTITY_COORDS(c.Law, true, true), bodyAt = ENTITY.GET_ENTITY_COORDS(c.Body, true, true);
            if (c.Stage >= BcUnarm && c.Stage < BcSetDown && now - c.CarryAt > 110000 && PED._GET_CARRIER_AS_PED(c.Body) == c.Law)
            {
                BodyGiveUp(c, lawAt, "110 s carrying and not there (" + (int)lawAt.DistanceTo(c.Goal) + " m off the spot)", now);   // set down, not dropped by a clear
                return;
            }
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
                            // the spots found again from where the player stands now (the walk up and the lift take a while)
                            Vector3 you = Game.Player.Character.Position;
                            List<BodySpot> spots;
                            string goalWhy;
                            if (BodyGoal(c.BodyStart, you, lawAt, out spots, out goalWhy))   // where it lay: bodyAt is on his shoulder now
                            {
                                c.Spots = spots;
                                c.Goal = spots[0].At;
                            }
                            else
                                goalWhy = "none found from where you are now (" + goalWhy + "): the ones from the start kept";
                            c.SpotIx = 0;
                            c.CarryAt = now;
                            c.LiftedAt = lawAt;
                            Log.Write("LAW clears: the lawman lifts " + c.Who + " (" + CarryWords(c.Law, c.Body) + ", settled after " + (now - c.StageAt) / 1000 +
                                      " s) and carries it " + (int)lawAt.DistanceTo(c.Goal) + " m, to a spot " + (int)FlatFrom(c.Goal, you) + " m from you (" + goalWhy + ")");
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
                        Log.Detail("LAW clears: hands free after " + (now - c.StageAt) / 1000 + " s; the place order takes it towards spot 1 of " + c.Spots.Count + ", " + (int)lawAt.DistanceTo(c.Goal) +
                                   " m off (flags 0; " + c.Spots[0].How + "; " + CarryWords(c.Law, c.Body) + ")");
                        c.Stage = BcPlaceWalk;
                        BodyOrderAt(c, lawAt, now);
                    }
                    break;
                case BcPlaceWalk:   // func_43 case 4: until the place order ends, or 15 s; then the nav-mesh sequence
                    {
                        float d = lawAt.DistanceTo(c.Goal);
                        if (c.Gone >= 2)
                            BodyPlacedOrDropped(c, bodyAt);   // out of his hands: set down at the spot, else dropped on the way
                        else if ((now - c.StageAt > 500 && TASK.GET_SCRIPT_TASK_STATUS(c.Law, PlaceTask, true) == 8) || now - c.StageAt > 15000)
                        {
                            if (!his)
                            {
                                BodyPlacedOrDropped(c, bodyAt);
                                break;
                            }
                            if (d < 5f && Math.Abs(lawAt.Z - c.Goal.Z) < 2f)
                            {
                                BodySetDown(c, d, lawAt, now, null);
                                break;
                            }
                            float gained = c.OrderDist - d;
                            if (gained < 1f)
                            {
                                BodyNextSpot(c, lawAt, "the place order ended after " + (now - c.StageAt) / 1000 + " s with " + gained.ToString("F1") + " m gained, " + (int)d +
                                                       " m off (speed " + ENTITY.GET_ENTITY_SPEED(c.Law).ToString("F1") + " m/s; " + CarryWords(c.Law, c.Body) + ")", now);
                                break;
                            }
                            Log.Detail("LAW clears: the place order ended after " + (now - c.StageAt) / 1000 + " s, " + (int)d + " m from the spot (" + gained.ToString("F1") +
                                       " m gained), still carrying (" + CarryWords(c.Law, c.Body) + "); he walks the rest by the nav mesh");
                            LawCarryWalk(c.Law, c.Goal);
                            c.Stage = BcNavWalk;
                            c.Straight = false;
                            BodyOrderAt(c, lawAt, now);
                        }
                        else if (now >= c.CheckAt)
                        {
                            string still = BodyProgress(c, d, now);
                            if (still != null)
                                BodyNextSpot(c, lawAt, still, now);   // not waiting the place order out (it ran 5 s for nothing in Lex's log)
                        }
                        break;
                    }
                case BcNavWalk:   // func_43 case 5: there (within 5 m, its f_18), then the undertaker's state 4; else the next spot
                    {
                        float d = lawAt.DistanceTo(c.Goal);
                        if (c.Gone >= 2)
                            BodyDropped(c, bodyAt);
                        else if (d < 5f && Math.Abs(lawAt.Z - c.Goal.Z) < 2f)
                            BodySetDown(c, d, lawAt, now, null);
                        else if (now - c.StageAt > 500 && TASK.GET_SCRIPT_TASK_STATUS(c.Law, SeqTask, true) == 8)
                            BodyNextSpot(c, lawAt, "his walk ended " + (int)d + " m short of the spot (speed " + ENTITY.GET_ENTITY_SPEED(c.Law).ToString("F1") + " m/s, " +
                                                   (c.OrderDist - d).ToString("F1") + " m gained; " + CarryWords(c.Law, c.Body) + ")", now);
                        else if (now - c.StageAt > SpotCap(c))
                            BodyNextSpot(c, lawAt, SpotCap(c) / 1000 + " s on this spot and still " + (int)d + " m off (" + (c.OrderDist - d).ToString("F1") + " m gained; " + LawState(c) + ")", now);
                        else if (now >= c.CheckAt)
                        {
                            string still = BodyProgress(c, d, now);
                            if (still != null)
                                BodyNextSpot(c, lawAt, still, now);
                        }
                        break;
                    }
                case BcSetDown:   // the undertaker's state 5 (func_14 false), here once the body is out of his hands: the carry state leaves 5/7 as he bends
                    if (!his)
                    {
                        float off = bodyAt.DistanceTo(c.Goal);
                        string where = (int)FlatFrom(bodyAt, Game.Player.Character.Position) + " m from you, carried " + (int)FlatFrom(bodyAt, c.LiftedAt) + " m";
                        bool there = !c.GaveUp && off < 5f;   // a give-up is a failure (the review: it counted as a clearing)
                        BodyClearEnd(c, (there ? "set down off the street, " + where
                                               : c.GaveUp ? "set down where he stood, " + (int)off + " m short of the spot, " + where
                                               : "set down short of the spot (" + (int)off + " m), " + where) + " (" + CarryWords(c.Law, c.Body) + ")", there);
                    }
                    else if (now - c.StageAt > 10000)
                    {
                        TASK.CLEAR_PED_TASKS(c.Law, true, false);   // never left holding it
                        BodyClearEnd(c, "dropped where he stood: the set-down didn't finish in 10 s (" + (int)bodyAt.DistanceTo(c.Goal) + " m from the spot; " + CarryWords(c.Law, c.Body) + ")",
                                     !c.GaveUp && bodyAt.DistanceTo(c.Goal) < 4f);
                    }
                    break;
            }
        }

        /// The body out of his hands during the place order: within 4 m of the spot the order set it down (done); farther, he
        /// dropped it on the way (the pre-release review: a drop 17 m short was logged and counted as a clearing).
        private void BodyPlacedOrDropped(BodyClear c, Vector3 bodyAt)
        {
            float off = bodyAt.DistanceTo(c.Goal);
            if (off < 4f)
                BodyClearEnd(c, "set down off the street by the place order (" + CarryWords(c.Law, c.Body) + ")", true);
            else
                BodyDropped(c, bodyAt);
        }

        private void BodyDropped(BodyClear c, Vector3 bodyAt)
        {
            float off = bodyAt.DistanceTo(c.Goal);
            BodyClearEnd(c, "he dropped it on the way, " + (int)off + " m short of the spot (" + LawState(c) + ")", off < 4f);
        }

        /// The watchdog on the way to the body: no metre gained in 3 s and he's sent again (twice at most); after that the
        /// game has him. (The carry has its own: BodyProgress and BodyNextSpot.)
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
            LawNavTo(c.Law, to, speed);
        }

        /// How long one spot may take: 25 s, or the walk at a carrying pace (0.7 m/s) and 8 s (the review: a 20 m carry with a
        /// detour came close to 25 s). The 110 s carry limit stops a runaway.
        private static int SpotCap(BodyClear c)
        {
            return Math.Max(25000, (int)((c.OrderDist / 0.7f + 8f) * 1000f));
        }

        /// A walk order with the body has just gone: its clock and distance, the first check 2.5 s on (a turn or the end of
        /// the lift's animation reads as standing still for a moment).
        private static void BodyOrderAt(BodyClear c, Vector3 lawAt, int now)
        {
            c.StageAt = now;
            c.LastCheckAt = now;
            c.OrderDist = lawAt.DistanceTo(c.Goal);
            c.LastDist = c.OrderDist;
            c.CheckAt = now + 2500;
            c.Slow = 0;
        }

        /// The progress check while he carries it (every 3 s): logged with his speed and the metres gained; null while he
        /// moves, else why he counts as stuck: under 0.2 m/s and under a metre gained, or under 0.5 m/s and no metre gained
        /// two checks running.
        private string BodyProgress(BodyClear c, float d, int now)
        {
            float speed = ENTITY.GET_ENTITY_SPEED(c.Law), gained = c.LastDist - d;
            int secs = Math.Max(1, (now - c.LastCheckAt + 500) / 1000);
            c.LastDist = d;
            c.LastCheckAt = now;
            c.CheckAt = now + 3000;
            c.Slow = gained < 1f && speed < 0.5f ? c.Slow + 1 : 0;   // slow only when he is too: a detour gains little in a line (the review)
            string how = (c.Stage == BcPlaceWalk ? "the place order" : c.Straight ? "straight" : "the nav mesh") + ", spot " + (c.SpotIx + 1) + " of " + c.Spots.Count;
            Log.Detail("LAW clears: carrying " + c.Who + " (" + how + "), " + (int)d + " m to the spot, " + (int)FlatFrom(ENTITY.GET_ENTITY_COORDS(c.Law, true, true), Game.Player.Character.Position) +
                       " m from you; speed " + speed.ToString("F1") + " m/s, " + gained.ToString("F1") + " m gained in " + secs + " s");
            if (speed < 0.2f && gained < 1f)
                return "he stands still with it, " + (int)d + " m off (speed " + speed.ToString("F1") + " m/s, " + gained.ToString("F1") + " m gained in " + secs + " s, " +
                       (c.OrderDist - d).ToString("F1") + " since the order; " + LawState(c) + ")";
            if (c.Slow >= 2)
                return "he gets no nearer with it, " + (int)d + " m off (speed " + speed.ToString("F1") + " m/s, " + gained.ToString("F1") + " m gained in " + secs + " s, " +
                       (c.OrderDist - d).ToString("F1") + " since the order; " + LawState(c) + ")";
            return null;
        }

        /// The spot he is on can't be reached: the next, by the nav mesh (after two failed, straight when the line is
        /// clear); out of spots, one straight try at the first with a clear line; then set down where he stands.
        private void BodyNextSpot(BodyClear c, Vector3 lawAt, string why, int now)
        {
            c.Fails++;
            if (c.Fails > c.Spots.Count)
            {
                BodyGiveUp(c, lawAt, "no reachable spot: all " + c.Spots.Count + " tried" + (c.TriedStraight ? ", one straight" : "") + "; the last: " + why, now);
                return;
            }
            Vector3 up = new Vector3(0f, 0f, 1f);
            Vector3 you = Game.Player.Character.Position;
            c.SpotIx++;
            bool straight;
            if (c.SpotIx < c.Spots.Count)
            {
                straight = c.Fails >= 2 && !Natives.Blocked(lawAt + up, c.Spots[c.SpotIx].At + up, c.Law);
            }
            else
            {
                int clear = c.TriedStraight ? -1 : c.Spots.FindIndex(s => !Natives.Blocked(lawAt + up, s.At + up, c.Law));
                if (clear < 0)
                {
                    BodyGiveUp(c, lawAt, "no reachable spot: " + c.Spots.Count + " tried" + (c.TriedStraight ? " (one straight)" : ", none in a clear line") + "; the last: " + why, now);
                    return;
                }
                c.SpotIx = clear;
                straight = true;
            }
            BodySpot s2 = c.Spots[c.SpotIx];
            c.Goal = s2.At;
            c.Straight = straight;
            if (straight)
            {
                c.TriedStraight = true;
                LawCarryStraight(c.Law, c.Goal);   // a new sequence, no clear: the carry stays
            }
            else
                LawCarryWalk(c.Law, c.Goal);
            Log.Write("LAW clears: " + why + "; trying spot " + (c.SpotIx + 1) + " of " + c.Spots.Count + ", " + (int)lawAt.DistanceTo(c.Goal) + " m off and " +
                      (int)FlatFrom(c.Goal, you) + " m from you, " + (straight ? "straight (the line is clear)" : "by the nav mesh") + " (" + s2.How + ")");
            c.Stage = BcNavWalk;
            BodyOrderAt(c, lawAt, now);
        }

        /// The fallback: no spot he can get to, so the body goes down where he stands, logged with how far he got.
        private void BodyGiveUp(BodyClear c, Vector3 lawAt, string why, int now)
        {
            float moved = FlatFrom(lawAt, c.LiftedAt);
            c.GaveUp = true;
            Log.Write("LAW clears: " + why + "; he sets " + c.Who + " down where he stands, " + (moved < 1f ? "never having moved from where he lifted it" : (int)moved + " m from where he lifted it") +
                      ", " + (int)FlatFrom(lawAt, Game.Player.Character.Position) + " m from you (speed " + ENTITY.GET_ENTITY_SPEED(c.Law).ToString("F1") + " m/s; " + LawState(c) + ")");
            BodySetDown(c, lawAt.DistanceTo(c.Goal), lawAt, now, "where he stands");
        }

        private void BodySetDown(BodyClear c, float d, Vector3 lawAt, int now, string where)
        {
            Vector3 put = where == null && d < 5f ? c.Goal : lawAt;
            TASK.TASK_PLACE_CARRIED_ENTITY_AT_COORD(c.Law, c.Body, put.X, put.Y, put.Z, 1f, 1);   // the game's own undertaker: 1f, flags 1
            Log.Detail("LAW clears: he sets " + c.Who + " down " + (where ?? (d < 5f ? "at the spot" : (int)d + " m short of it")) + " (place order, flags 1; " + CarryWords(c.Law, c.Body) + ")");
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
