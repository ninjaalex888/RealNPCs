// A broken wagon wheel, the scene ([Encounters] WagonWheel; roadmap 32; the words and the rules are Core/WagonWheel.cs; built
// on the probe's answer, Lex 2026-11-04 10:38: the plain break-off-wheel native breaks a wheel, the wagon stops within
// eight seconds of its own accord, the driver gets down of his own accord, nothing jams behind, the mend takes, and the
// wagon moves on after). In the encounter framework: on a road out of the towns, by day, an ambient wagon with a driver
// ahead of you (40-150 m, out of view, moving) loses a wheel. You come on it stopped, the driver beside it; within 25 m
// he calls to you (subtitled, his voice). Stop on foot within 3 m of the wagon for three seconds and you put your
// shoulder to it: six seconds, the wheel is on, his thanks, his liking for you up, and he climbs back on the box and
// drives on. Ride past instead and once you're 40 m gone he calls after you to send word to the nearest town; reach any
// town within thirty game minutes and the word is sent: a notice, and the smith goes out (the wagon is mended and let
// go). Nothing is spawned: the wagon and its driver are the game's, taken off screen and handed back. Once a day. The
// Testing row "A wagon loses a wheel ahead of you" (kind "wagonscene"); F7 "TEST wagon:" carries the scene's line.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private sealed class WagonSceneRun
        {
            public int Vehicle, Driver, Wheel, Passengers;
            public string Phase = "broken";   // broken (the wagon slowing, the driver getting down), down, lifting, mended, passed, done
            public int StartedAt, PhaseAt, StillSince, CallAt, StandAt;
            public long WordAskedAt;
            public Vector3 At;
            public string DriverName, DriverId, Town, Seed;
            public bool Called, Test, Helped, Word, Ours;   // Ours: the driver's events are ours (blocked) until the end
            public int Other, OtherAt, DownAt;              // a passer-by who stops (FIVEDAY item 32d): his handle, when he turned aside; when the driver got down
            public bool OtherTried, OtherSet, RodePast;      // looked for one already; he set it; you rode on first
            public string OtherName;
        }

        private WagonSceneRun _wagonScene;
        private int _wagonScenes, _wagonHelped, _wagonWords, _wagonPassed, _wagonOthers;
        private string _wagonSceneLast;

        private string WagonSceneNotNow(bool test = false)
        {
            Ped me = Game.Player.Character;
            if (_bridge.TownOnly(me.Position) != null)
                return "you're in a town (it happens out on the roads)";
            if (_wagon != null)
                return "the wheel probe is going";
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!test && !WagonWheel.Hour(clock))
                return "it's " + clock.ToString("00") + ":00 (wagons break down by day, 06:00 to 20:00)";
            if (!test && DoneToday("wagonscene"))
                return "one broke down already today";
            if (!test && !PATHFIND.IS_POINT_ON_ROAD(me.Position.X, me.Position.Y, me.Position.Z, 0))
                return "you're not on a road";
            return null;
        }

        /// The start: the nearest ambient wagon with a driver ahead of you, out of view, 40-150 m off and moving, loses a wheel.
        /// False (and why in the log) when there's no such wagon.
        private bool StartWagonScene(int now, bool test)
        {
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return false;
            int mine = PED.GET_VEHICLE_PED_IS_IN(me.Handle, false);
            Vector3 fwd = ENTITY.GET_ENTITY_FORWARD_VECTOR(me.Handle);
            Vehicle best = null;
            float bestD = WagonWheel.FarMetres;
            string why = "no wagon fit for it (a driver on it, ahead, unseen, within " + (int)WagonWheel.FarMetres + " m)";
            foreach (Vehicle v in World.GetAllVehicles())
            {
                if (v == null || !v.Exists())
                    continue;
                int h = v.Handle;
                if (h == mine || !VEHICLE.IS_DRAFT_VEHICLE(h) || ENTITY.IS_ENTITY_A_MISSION_ENTITY(h))
                    continue;
                Vector3 at = v.Position;
                float d = at.DistanceTo(me.Position);
                if (d >= bestD || (!test && d < WagonWheel.NearMetres))
                    continue;
                if (ENTITY.IS_ENTITY_ON_SCREEN(h))
                {
                    why = "the wagon near is in view";
                    continue;
                }
                if (!test && !WagonWheel.Ahead(fwd.X, fwd.Y, at.X - me.Position.X, at.Y - me.Position.Y))
                {
                    why = "the wagon near is behind you";
                    continue;
                }
                if (ENTITY.GET_ENTITY_SPEED(h) < 0.5f)
                {
                    why = "the wagon near is standing";
                    continue;
                }
                int driver = VEHICLE.GET_PED_IN_VEHICLE_SEAT(h, -1);
                if (driver == 0 || !ENTITY.DOES_ENTITY_EXIST(driver) || ENTITY.IS_ENTITY_DEAD(driver) || Natives.StoryModel(new Ped(driver)) || Natives.OtherScripts(driver))
                {
                    why = "the wagon near has no driver of the game's own";
                    continue;
                }
                best = v;
                bestD = d;
            }
            if (best == null)
            {
                Log.Write("WAGON scene: not now: " + why);
                return false;
            }
            int veh = best.Handle, drv = VEHICLE.GET_PED_IN_VEHICLE_SEAT(veh, -1);
            int wheel = -1;
            for (int w = 0; w < WagonWheel.WheelsToTry; w++)
                if (!VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, w))
                {
                    wheel = w;
                    break;
                }
            if (wheel < 0)
            {
                Log.Write("WAGON scene: not now: every wheel of the wagon near reads broken already");
                return false;
            }
            WagonBreakFlags(veh);
            VEHICLE._BREAK_OFF_VEHICLE_WHEEL(veh, wheel);   // the way the probe found (10:38:45): the wheel off, the wagon stops of itself, the driver gets down
            string id, name;
            LastingId(new Ped(drv), out id, out name);
            _wagonScene = new WagonSceneRun
            {
                Vehicle = veh, Driver = drv, Wheel = wheel, Passengers = VEHICLE.GET_VEHICLE_NUMBER_OF_PASSENGERS(veh), StartedAt = now, PhaseAt = now, At = best.Position,
                DriverName = name, DriverId = id, Town = ZoneAt(best.Position), Test = test, Seed = "wg" + now
            };
            _wagonScenes++;
            if (!test)
                MarkDone("wagonscene");
            Log.Write("WAGON scene: a wagon " + (int)bestD + " m ahead, out of view, loses a wheel (wheel " + wheel + ", " + (ENTITY.IS_ENTITY_DEAD(drv) ? "no" : "a") + " driver" + (name != null ? ", " + name : "") +
                      (_wagonScene.Passengers > 0 ? ", " + _wagonScene.Passengers + " on the bench" : "") + ")" + (test ? " (the Testing row)" : ""));
            return true;
        }

        /// The encounters' tick: the wagon stopping and the driver getting down, his call, your help or your passing, the mend and the
        /// drive on; the ends.
        private void WagonSceneStep(int now)
        {
            WagonSceneRun r = _wagonScene;
            if (r == null)
                return;
            Ped me = Game.Player.Character;
            string end = null;
            if (!ENTITY.DOES_ENTITY_EXIST(r.Vehicle)) end = "the wagon is gone";
            else if (!ENTITY.DOES_ENTITY_EXIST(r.Driver) || ENTITY.IS_ENTITY_DEAD(r.Driver)) end = "the driver is gone or dead";
            else if (MISC.GET_MISSION_FLAG()) end = "a mission began";
            else if (me == null || !me.Exists() || me.IsDead) end = "you're gone";
            else if (now - r.StartedAt > WagonWheel.MostMs) end = "its time ran out";
            else if (r.Phase != "passed" && me.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(r.Vehicle, true, true)) > WagonWheel.GoneMetres) end = "you went too far off";
            if (end != null)
            {
                WagonSceneEnd(r, end, r.Phase == "mended");
                return;
            }
            Vector3 at = ENTITY.GET_ENTITY_COORDS(r.Vehicle, true, true);
            float d = me.Position.DistanceTo(at);
            bool driverIn = PED.IS_PED_IN_VEHICLE(r.Driver, r.Vehicle, false);
            if ((r.Phase == "down" || r.Phase == "passed") && _cfg.WagonOthersStop && WagonOtherStep(r, me, at, now))
                return;   // someone else set it (FIVEDAY item 32d)
            switch (r.Phase)
            {
                case "broken":
                    {
                        bool stopped = ENTITY.GET_ENTITY_SPEED(r.Vehicle) < 0.2f;
                        if (stopped && driverIn && now - r.PhaseAt > WagonWheel.DriverDownMs)
                        {
                            TASK.TASK_LEAVE_VEHICLE(r.Driver, r.Vehicle, 0, 0);   // the game didn't get him down: asked
                            r.PhaseAt = now;
                            Log.Detail("WAGON scene: the driver was still on the box " + WagonWheel.DriverDownMs / 1000 + " s after the stop: asked down");
                        }
                        if (stopped && !driverIn)
                        {
                            r.Phase = "down";
                            r.PhaseAt = now;
                            r.DownAt = now;
                            r.At = at;
                            WagonDriverStand(r, now);
                            Log.Write("WAGON scene: the wagon stands and the driver is down beside it, " + (int)d + " m from you");
                        }
                        break;
                    }
                case "down":
                    {
                        if (now - r.StandAt > 25000)
                            WagonDriverStand(r, now);   // kept beside his wagon
                        if (!r.Called && d <= WagonWheel.CallMetres && NodInView(me, new Ped(r.Driver)))
                        {
                            r.Called = true;
                            r.CallAt = now;
                            Hint("Stand by the wagon on foot for three seconds to set the wheel with him, or ride on.", 9000);   // the bar's hint, where the Nearby line shows (Lex, 2026-11-04)
                            TASK.TASK_LOOK_AT_ENTITY(r.Driver, me.Handle, 6000, 0, 51, 0);
                            string call = WagonWheel.DriverCall(r.Passengers > 0, r.Seed);
                            SpeechCaption(WagonDriverLabel(r), call, r.Driver, 5000, true, true);
                            Log.Write("WAGON scene: the driver calls to you (" + (int)d + " m): " + call);
                        }
                        // your help: on foot, within 3 m of the wagon, still for three seconds
                        bool close = d <= WagonWheel.HelpMetres && !PED.IS_PED_ON_MOUNT(me.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) && ENTITY.GET_ENTITY_SPEED(me.Handle) < 0.4f;
                        if (close)
                        {
                            if (r.StillSince == 0)
                                r.StillSince = now;
                            else if (now - r.StillSince >= WagonWheel.HelpStillMs)
                            {
                                r.Phase = "lifting";
                                r.PhaseAt = now;
                                if (r.Other != 0 && !r.OtherSet)
                                    WagonOtherRelease(r, "you got there first");
                                TASK.CLEAR_PED_TASKS(r.Driver, true, false);
                                TASK.TASK_TURN_PED_TO_FACE_COORD(r.Driver, at.X, at.Y, at.Z, 3000);
                                string lift = WagonWheel.LiftWords(r.Seed);
                                SpeechCaption(WagonDriverLabel(r), lift, r.Driver, 3500, true, true);
                                Log.Write("WAGON scene: you put your shoulder to it: " + lift);
                            }
                        }
                        else
                            r.StillSince = 0;
                        // riding past: called to and now 40 m gone: he calls after you to send word
                        if (r.Called && !close && d >= WagonWheel.PassMetres && now - r.CallAt > 4000)
                        {
                            r.Phase = "passed";
                            r.PhaseAt = now;
                            r.RodePast = true;
                            r.WordAskedAt = Bridge.Now();
                            string word = WagonWheel.DriverSendWord(r.Town, r.Seed);
                            SpeechCaption(WagonDriverLabel(r), word, r.Driver, 5000, true, true);
                            _wagonPassed++;
                            Hint("He asks you to send word: reach any town within half an hour.", 8000);
                            Log.Write("WAGON scene: you rode on; he calls after you: " + word);
                        }
                        break;
                    }
                case "lifting":
                    if (now - r.PhaseAt >= WagonWheel.LiftMs)
                    {
                        VEHICLE.SET_VEHICLE_FIXED(r.Vehicle);
                        r.Helped = true;
                        r.Phase = "mended";
                        r.PhaseAt = now;
                        _wagonHelped++;
                        string thanks = WagonWheel.DriverThanks(r.Seed);
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Driver, me.Handle, 3000, 0f, 0f, 0f);
                        SpeechCaption(WagonDriverLabel(r), thanks, r.Driver, 5000, true, true);
                        if (!r.Test && r.DriverId != null && r.DriverId.StartsWith("folk:"))
                            _bridge.LikeMore(r.DriverId.Substring(5), 6);   // enRAGE's own liking for you (an audited write)
                        Log.Write("WAGON scene: the wheel is on (" + (VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(r.Vehicle, r.Wheel) ? "the game still says broken" : "whole") + "); his thanks: " + thanks);
                    }
                    break;
                case "mended":
                    if (!driverIn && now - r.PhaseAt > 4000 && now - r.PhaseAt < 4400)
                    {
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Driver, false);
                        r.Ours = false;
                        TASK.CLEAR_PED_TASKS(r.Driver, true, false);
                        TASK.TASK_ENTER_VEHICLE(r.Driver, r.Vehicle, 20000, -1, 1f, 1, 0);   // back on the box; the game's own driving takes it from there (the probe saw it roll on)
                        Log.Detail("WAGON scene: the driver climbs back on the box");
                    }
                    if (now - r.PhaseAt > 30000)
                        WagonSceneEnd(r, driverIn ? "he drives on" : "he never got back on the box; the wagon is the game's again", true);
                    break;
                case "passed":
                    {
                        long g = Bridge.Now();
                        if (_placeTown != null)
                        {
                            // word sent: you reached a town in time; the smith goes out (the wagon mended and let go)
                            r.Word = true;
                            _wagonWords++;
                            string sent = WagonWheel.WordSent(_placeTown, r.Seed);
                            Feed.Notice("Word of the wagon", sent);
                            if (ENTITY.DOES_ENTITY_EXIST(r.Vehicle))
                                VEHICLE.SET_VEHICLE_FIXED(r.Vehicle);
                            WagonSceneEnd(r, "word sent in " + _placeTown, true);
                        }
                        else if (g - r.WordAskedAt > WagonWheel.WordMinutes)
                            WagonSceneEnd(r, "no word sent within " + WagonWheel.WordMinutes + " game minutes", false);
                        else if (d > WagonWheel.GoneMetres * 3f)
                            WagonSceneEnd(r, "you went far off without sending word", false);
                        break;
                    }
            }
        }

        /// Someone else pulls up (FIVEDAY item 32d; [Encounters] WagonOthersStop). Twenty-five seconds after the driver is down
        /// with no help from you, or once you've ridden on: six wagons in ten, the nearest free grown man 25-90 m from the wagon,
        /// not in a vehicle and nobody's (no mission, story or scene of ours), turns aside to it (the go-to-entity, on foot or
        /// mounted). At the wagon: down off his horse if he's on one, his offer (he says so if you rode past), the driver's
        /// answer, the lift and the mend, the driver's thanks, the helper's thought; then the mended phase takes the driver back
        /// to the box. True when he set it (the step is done this tick). You putting your shoulder to it first sends him on.
        private bool WagonOtherStep(WagonSceneRun r, Ped me, Vector3 at, int now)
        {
            if (r.Other == 0)
            {
                if (r.OtherTried || now - r.DownAt < WagonWheel.OtherAfterMs && !r.RodePast)
                    return false;
                r.OtherTried = true;
                if (!r.Test && !WagonWheel.OtherStops(r.Seed))
                {
                    Log.Detail("WAGON scene: nobody else on the road stops for him");
                    return false;
                }
                float best = WagonWheel.OtherTo;
                Ped pick = null;
                foreach (Ped p in Peds.All())
                {
                    int h = p.Handle;
                    if (h == me.Handle || h == r.Driver || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || !IsGrownUp(p) || !PED.IS_PED_MALE(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false))
                        continue;
                    float d = p.Position.DistanceTo(at);
                    if (d < WagonWheel.OtherFrom || d >= best || ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) || Natives.StoryModel(p) || _director.Involves(h) || _staged.Contains(h) || !MayDirect(p) || NotFree(h))
                        continue;
                    pick = p;
                    best = d;
                }
                if (pick == null)
                {
                    Log.Detail("WAGON scene: nobody free on the road to stop for him");
                    return false;
                }
                r.Other = pick.Handle;
                r.OtherAt = now;
                MindWho ow;
                r.OtherName = _mindWho.TryGetValue(r.Other, out ow) ? ow.Name : null;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Other, true);
                TASK.CLEAR_PED_TASKS(r.Other, true, false);
                TASK.TASK_GO_TO_ENTITY(r.Other, r.Vehicle, WagonWheel.OtherWalkMs, WagonWheel.OtherThere - 1f, 1.6f, 0f, 0);
                Log.Write("WAGON scene: " + (r.OtherName ?? "a passer-by") + " " + (int)best + " m off turns aside to the wagon" + (PED.IS_PED_ON_MOUNT(r.Other) ? " on horseback" : "") + (r.RodePast ? " (you rode on)" : ""));
                return false;
            }
            if (!ENTITY.DOES_ENTITY_EXIST(r.Other) || ENTITY.IS_ENTITY_DEAD(r.Other) || now - r.OtherAt > WagonWheel.OtherWalkMs + 15000)
            {
                WagonOtherRelease(r, "he never got there");
                return false;
            }
            Vector3 oa = ENTITY.GET_ENTITY_COORDS(r.Other, true, true);
            if (r.OtherSet || oa.DistanceTo(at) > WagonWheel.OtherThere + 1.5f)
                return false;
            if (PED.IS_PED_ON_MOUNT(r.Other))
            {
                if (now - r.OtherAt > 4000)
                {
                    Function.Call(0x48E92D3DDE23C23A, r.Other, 0, 0, 0, 0, 0);   // down off his horse (the stalker's dismount, by hash)
                    r.OtherAt = now - WagonWheel.OtherWalkMs + 8000;   // a few seconds more to get down
                }
                return false;
            }
            // there, on foot: the offer, the answer, the lift (six seconds: the mended phase does the rest)
            r.OtherSet = true;
            string label = r.OtherName != null ? Person.FirstOf(r.OtherName) : "A passer-by";
            TASK.TASK_TURN_PED_TO_FACE_COORD(r.Other, at.X, at.Y, at.Z, 3000);
            SpeechCaption(label, WagonWheel.OtherOffer(r.RodePast, r.Seed), r.Other, 4000);
            SpeechCaption(WagonDriverLabel(r), WagonWheel.DriverToOther(r.Seed), r.Driver, 3500, false, true);
            VEHICLE.SET_VEHICLE_FIXED(r.Vehicle);
            SpeechCaption(WagonDriverLabel(r), WagonWheel.DriverThanks(r.Seed + "|o"), r.Driver, 4500, false, true);
            _ticker.Push(label + ", thinking", WagonWheel.OtherAfter(r.RodePast, r.Seed), "near", r.Other);
            _wagonOthers++;
            Log.Write("WAGON scene: " + WagonWheel.OtherSetIt(r.OtherName));
            r.Phase = "mended";
            r.PhaseAt = now;
            WagonOtherRelease(r, null);
            return true;
        }

        /// Lets the passer-by go again (his events and tasks his own once more); `why` in the log if he never helped.
        private void WagonOtherRelease(WagonSceneRun r, string why)
        {
            int h = r.Other;
            if (h == 0)
                return;
            if (!r.OtherSet)
                r.Other = 0;
            try
            {
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    if (!r.OtherSet)
                        TASK.CLEAR_PED_TASKS(h, true, false);
                    else
                        TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // on his way again
                }
            }
            catch
            {
            }
            if (why != null)
                Log.Detail("WAGON scene: the passer-by gave it up (" + why + ")");
        }

        /// The driver kept beside his wagon while it stands (the game's own would wander): his events ours, a stand-still.
        private void WagonDriverStand(WagonSceneRun r, int now)
        {
            r.StandAt = now;
            if (!r.Ours)
            {
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Driver, true);
                r.Ours = true;
            }
            Vector3 at = ENTITY.GET_ENTITY_COORDS(r.Driver, true, true);
            if (at.DistanceTo(r.At) > 6f)
                TASK.TASK_GO_STRAIGHT_TO_COORD(r.Driver, r.At.X, r.At.Y, r.At.Z, 1f, 8000, ENTITY.GET_ENTITY_HEADING(r.Driver), 0.5f, 0);
            else
                TASK.TASK_STAND_STILL(r.Driver, 30000);
        }

        private string WagonDriverLabel(WagonSceneRun r)
        {
            return r.DriverName != null ? Person.FirstOf(r.DriverName) : "The driver";
        }

        /// The end: the driver's events the game's again (and his tasks cleared if he never remounted), the wagon mended when the
        /// scene is done with it, the line for F7 and the log.
        private void WagonSceneEnd(WagonSceneRun r, string why, bool mended)
        {
            _wagonScene = null;
            if (r.Other != 0 && !r.OtherSet)
                WagonOtherRelease(r, why);
            try
            {
                if (ENTITY.DOES_ENTITY_EXIST(r.Driver) && !ENTITY.IS_ENTITY_DEAD(r.Driver))
                {
                    if (r.Ours)
                    {
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Driver, false);
                        TASK.CLEAR_PED_TASKS(r.Driver, true, false);
                    }
                }
                if (mended && ENTITY.DOES_ENTITY_EXIST(r.Vehicle))
                    VEHICLE.SET_VEHICLE_FIXED(r.Vehicle);
                // set by a passer-by while you were far off: the scene ends at once, so the driver is sent back to the box here (FIVEDAY item 32d)
                if (r.OtherSet && ENTITY.DOES_ENTITY_EXIST(r.Vehicle) && ENTITY.DOES_ENTITY_EXIST(r.Driver) && !ENTITY.IS_ENTITY_DEAD(r.Driver) && !PED.IS_PED_IN_VEHICLE(r.Driver, r.Vehicle, false))
                    TASK.TASK_ENTER_VEHICLE(r.Driver, r.Vehicle, 20000, -1, 1f, 1, 0);
            }
            catch
            {
            }
            _wagonSceneLast = (r.Helped ? "you set the wheel" : r.OtherSet ? WagonWheel.OtherSetIt(r.OtherName) : r.Word ? "you sent word" : r.Called ? "you rode on" : "nobody came") + " (" + why + ")";
            Log.Write("WAGON scene over: " + _wagonSceneLast + " after " + (Game.GameTime - r.StartedAt) / 1000 + " s");
        }

        private string WagonSceneStatus()
        {
            return (_cfg.WagonWheel ? "on (" + _cfg.WagonWheelChance + "% a roll)" : "off ([Encounters] WagonWheel)") + "; this session " + _wagonScenes + " wagons lost a wheel, " + _wagonHelped + " set by you, " +
                   _wagonWords + " word sent, " + _wagonPassed + " ridden past, " + _wagonOthers + " set by a passer-by" + (_wagonScene != null ? "; one standing now (" + _wagonScene.Phase + ")" : "") + (_wagonSceneLast != null ? "; last: " + _wagonSceneLast : "");
        }
    }
}
