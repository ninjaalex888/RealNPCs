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
            public int Law, Body, Stage, Since, StageAt, CheckAt, Resent;
            public float LastDist;    // how far he was at the last progress check (the watchdog)
            public bool Owned;        // another script had him: taken lightly (LawTake)
            public Vector3 Goal;
            public string Who;
        }

        // the game's own way, from its undertaker script (Lex, 2026-10-31): every frame of the carry the body is marked
        // carriable by a ped (carrying flag 7) and the carrier keeps reset flag 49; the set-down's flags are 1, not 0
        private const ulong SetCarryingFlag = 0xC3ABCFBC7D74AFA5;   // _SET_ENTITY_CARRYING_FLAG (by hash: wrapper builds name it differently)
        private static readonly uint NavTask = Natives.Hash("SCRIPT_TASK_FOLLOW_NAV_MESH_TO_COORD");

        private static void LawNavTo(int law, Vector3 to, float speed)
        {
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(law, to.X, to.Y, to.Z, speed, -1, 1f, 0, 0f);
        }

        /// Every frame while a clearing runs (the flags are a frame's): the game's undertaker does the same.
        private void BodyClearFrame()
        {
            BodyClear c = _bodyClear;
            if (c == null || !ENTITY.DOES_ENTITY_EXIST(c.Body) || !ENTITY.DOES_ENTITY_EXIST(c.Law))
                return;
            Function.Call(SetCarryingFlag, c.Body, 7, true);
            PED.SET_PED_RESET_FLAG(c.Law, 49, true);
        }

        private string LawState(int law)
        {
            return "a mission entity " + (ENTITY.IS_ENTITY_A_MISSION_ENTITY(law) ? "yes" : "no") + ", ours " + (Spawner.IsBorrowed(law) ? "yes" : "no") +
                   ", in a scenario " + (PED.IS_PED_USING_ANY_SCENARIO(law) ? "yes" : "no") + ", the walk's status " + TASK.GET_SCRIPT_TASK_STATUS(law, NavTask, true);
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
            Ped law = new Ped(c.Law), body = new Ped(c.Body);
            bool carrying = PED.IS_PED_CARRYING_SOMETHING(c.Law) && PED._GET_FIRST_ENTITY_PED_IS_CARRYING(c.Law) == c.Body;
            switch (c.Stage)
            {
                case 0:   // walking over
                    {
                        float d = law.Position.DistanceTo(body.Position);
                        if (d < 1.8f)
                        {
                            TASK.CLEAR_PED_TASKS(c.Law, true, false);
                            TASK.TASK_PICKUP_CARRIABLE_ENTITY(c.Law, c.Body);
                            Log.Detail("LAW clears: he reaches " + c.Who + " after " + (now - c.StageAt) / 1000 + " s and bends to lift it");
                            c.Stage = 1;
                            c.StageAt = now;
                            BodyWatchers(body.Position, c.Law, c.Body);
                        }
                        else if (now - c.StageAt > 45000)
                            BodyClearEnd(c, "he never reached it, " + (int)d + " m off (" + LawState(c.Law) + ")", false);
                        else if (now >= c.CheckAt)
                            BodyWatch(c, d, body.Position, d > 12f ? 2f : 1f, "on his way to it", now);
                        break;
                    }
                case 1:   // lifting
                    if (carrying)
                    {
                        Log.Write("LAW clears: the lawman lifts " + c.Who + " and carries it " + (int)law.Position.DistanceTo(c.Goal) + " m off the street");
                        LawNavTo(c.Law, c.Goal, 1f);   // walking with it, the game's undertaker's way (no clear first: the carry stays)
                        c.Stage = 2;
                        c.StageAt = now;
                        c.Resent = 0;
                        c.LastDist = law.Position.DistanceTo(c.Goal);
                        c.CheckAt = now + 3000;
                    }
                    else if (now - c.StageAt > 8000)
                        BodyClearEnd(c, "the game wouldn't let him lift it (no carry after 8 s; " + LawState(c.Law) + ")", false);
                    break;
                case 2:   // carrying it off
                    {
                        float d = law.Position.DistanceTo(c.Goal);
                        if (!carrying)
                            BodyClearEnd(c, "he dropped it on the way, " + (int)body.Position.DistanceTo(c.Goal) + " m short of the spot (" + LawState(c.Law) + ")", body.Position.DistanceTo(c.Goal) < 4f);
                        else if (d < 2.5f || now - c.StageAt > 40000 || (now >= c.CheckAt && c.Resent >= 2 && c.LastDist - d < 1f))
                            BodySetDown(c, d, now);   // there, or as near as he'll get
                        else if (now >= c.CheckAt)
                            BodyWatch(c, d, c.Goal, 1f, "carrying it", now);
                        break;
                    }
                case 3:   // setting it down
                    if (!carrying)
                        BodyClearEnd(c, body.Position.DistanceTo(c.Goal) < 4f ? "set down off the street" : "set down short of the spot (" + (int)body.Position.DistanceTo(c.Goal) + " m)", true);
                    else if (now - c.StageAt > 6000)
                    {
                        TASK.CLEAR_PED_TASKS(c.Law, true, false);   // never left holding it
                        BodyClearEnd(c, "dropped where he stood: the set-down didn't finish in 6 s (" + (int)body.Position.DistanceTo(c.Goal) + " m from the spot)", body.Position.DistanceTo(c.Goal) < 4f);
                    }
                    break;
            }
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
                if (c.Stage == 0)
                    BodyClearEnd(c, "the game kept him: stopped " + (int)d + " m off after being sent three times (" + LawState(c.Law) + ")", false);
                return;
            }
            c.Resent++;
            Log.Write("LAW clears: he stopped " + doing + ", " + (int)d + " m off (" + LawState(c.Law) + "); sent again");
            LawNavTo(c.Law, to, speed);
        }

        private void BodySetDown(BodyClear c, float d, int now)
        {
            Vector3 at = ENTITY.GET_ENTITY_COORDS(c.Law, true, true);
            Vector3 put = d < 2.5f ? c.Goal : at;
            TASK.TASK_PLACE_CARRIED_ENTITY_AT_COORD(c.Law, c.Body, put.X, put.Y, put.Z, 1f, 1);   // the game's own undertaker: 1f, flags 1
            Log.Detail("LAW clears: he sets " + c.Who + " down " + (d < 2.5f ? "at the spot" : (int)d + " m short of it"));
            c.Stage = 3;
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
