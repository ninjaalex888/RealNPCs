// Lawmen clear the dead from the street (NIGHT3 item 6; roadmap 33; Lex, 2026-10-31). A body left lying in a town (dead
// 20 s or more, nobody carrying it, no fight or pursuit near, not the story's or another script's): the nearest free
// lawman on foot within 50 m walks over, lifts it with the game's own carry, takes it about 20 m off to a walkable spot
// away from the player, sets it down and goes back to his rounds. Up to three people near turn to watch him do it. One
// at a time, each body once, a minute between. Every step is logged (LAW clears:) since an NPC lifting a body is new:
// the log says where it stopped if the game won't let him. [World] LawClearsBodies. Testing row "A lawman clears a body".
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
            public int Law, Body, Stage, Since, StageAt;
            public Vector3 Goal;
            public string Who;
        }

        private BodyClear _bodyClear;
        private readonly Dictionary<int, int> _bodyFirstSeen = new Dictionary<int, int>();   // body handle -> when first seen dead
        private readonly HashSet<int> _bodiesTaken = new HashSet<int>();
        private int _bodyNextLook, _bodyNextStart, _bodyClears, _bodyFails;
        private string _bodyLast;
        private Action _aBodies;

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
                BodyClearStep(_bodyClear, now);
                return test ? "a lawman is already clearing a body" : null;
            }
            if (!test && (now < _bodyNextLook || now < _bodyNextStart))
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
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || !p.IsDead || !PED.IS_PED_HUMAN(p.Handle) || p.Position.DistanceTo(me.Position) > 40f)
                    continue;
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
            if (World.GetAllPeds().Any(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_IN_COMBAT(x.Handle, 0) && x.Position.DistanceTo(body.Position) < 35f))
                return "a fight near the body";
            Ped law = World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && Natives.IsLaw(x) && !PED.IS_PED_ON_MOUNT(x.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(x.Handle, false) &&
                                                    !PED.IS_PED_IN_COMBAT(x.Handle, 0) && !_director.Involves(x.Handle) && !NotFree(x.Handle) && x.Position.DistanceTo(body.Position) < (test ? 80f : 50f))
                                        .OrderBy(x => x.Position.DistanceTo(body.Position)).FirstOrDefault();
            if (law == null)
                return "no lawman on foot within " + (test ? 80 : 50) + " m of the body";
            Vector3 goal;
            if (!BodyGoal(body.Position, me.Position, out goal))
                return "nowhere off the street to take it";
            _bodyClear = new BodyClear { Law = law.Handle, Body = body.Handle, Stage = 0, Since = now, StageAt = now, Goal = goal, Who = NameToShow(body.Handle) ?? "a body" };
            _bodiesTaken.Add(body.Handle);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(law.Handle, true);
            TASK.CLEAR_PED_TASKS(law.Handle, true, false);
            TASK.TASK_GO_TO_ENTITY(law.Handle, body.Handle, 20000, 1.2f, 1.0f, 0f, 0);
            Log.Write("LAW clears: a lawman " + (int)law.Position.DistanceTo(body.Position) + " m off goes to " + _bodyClear.Who + " to take it off the street" + (test ? " (the Testing row)" : ""));
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
            else if (now - c.Since > 90000) fail = "it took too long";
            if (fail != null)
            {
                BodyClearEnd(c, fail, false);
                return;
            }
            Ped law = new Ped(c.Law), body = new Ped(c.Body);
            switch (c.Stage)
            {
                case 0:   // walking over
                    if (law.Position.DistanceTo(body.Position) < 1.8f)
                    {
                        TASK.CLEAR_PED_TASKS(c.Law, true, false);
                        TASK.TASK_PICKUP_CARRIABLE_ENTITY(c.Law, c.Body);
                        c.Stage = 1;
                        c.StageAt = now;
                        BodyWatchers(body.Position, c.Law, c.Body);
                    }
                    else if (now - c.StageAt > 25000)
                        BodyClearEnd(c, "he never reached it", false);
                    break;
                case 1:   // lifting
                    if (PED.IS_PED_CARRYING_SOMETHING(c.Law) && PED._GET_FIRST_ENTITY_PED_IS_CARRYING(c.Law) == c.Body)
                    {
                        Log.Write("LAW clears: the lawman lifts " + c.Who);
                        TASK.TASK_PLACE_CARRIED_ENTITY_AT_COORD(c.Law, c.Body, c.Goal.X, c.Goal.Y, c.Goal.Z, 1.0f, 0);
                        c.Stage = 2;
                        c.StageAt = now;
                    }
                    else if (now - c.StageAt > 8000)
                        BodyClearEnd(c, "the game wouldn't let him lift it (no carry after 8 s)", false);
                    break;
                case 2:   // carrying it off and setting it down
                    bool carrying = PED.IS_PED_CARRYING_SOMETHING(c.Law);
                    if (!carrying && now - c.StageAt > 2500)
                        BodyClearEnd(c, body.Position.DistanceTo(c.Goal) < 4f ? "set down off the street" : "set down short of the spot (" + (int)body.Position.DistanceTo(c.Goal) + " m)", true);
                    else if (now - c.StageAt > 40000)
                        BodyClearEnd(c, "still carrying after 40 s", false);
                    break;
            }
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
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(c.Law, false);
                WanderLater(c.Law, Game.GameTime + 4000);
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
