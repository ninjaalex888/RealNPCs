// A broken wagon wheel, the probe (SIXDAY item 26; roadmap 32; Core/WagonWheel.cs). The Testing row "wagon" takes an
// ambient wagon off screen with a driver on it (never the player's, never a mission's, never a train) and tries to
// break one wheel, one way a press, round WagonWheel.Ways (the first run, 2026-11-04 09:36, showed the draft-wheel
// native alone does nothing): the draft-wheel native with a heavy force after the can-break flags, the plain
// break-off-wheel native, the wagon taken as a mission entity first, damage at the wheel's own bone, the body health as
// a check that natives take at all. The game's answers are read every two seconds: does the wheel read broken, does the
// wagon stop, does the driver get down or run, do the vehicles behind it stand; a way that reads nothing broken in
// twelve seconds ends there and the next press tries the next way; a break is watched forty seconds, mended
// (SET_VEHICLE_FIXED) and watched twenty more. The report and verdict go to the log, a notice and F7 "TEST wagon:"; the
// wagon is handed back as it was. Nothing is spawned; the scene itself waits on what this says.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private sealed class WagonRun
        {
            public int Vehicle, Driver, StartedAt, BrokeAt, MendedAt, NextRead;
            public Vector3 At;
            public bool Mended, FixedRead, Mission;   // FixedRead: the first reading after the mend has said whether the wheel is whole; Mission: taken as a mission entity (given back at the end)
            public WagonProbe Probe;
        }

        private static readonly string[] WheelBones = { "wheel_lf", "wheel_rf", "wheel_lr", "wheel_rr" };   // the wheels' bones by index, as the vehicle models name them

        private WagonRun _wagon;
        private WagonProbe _wagonLast;
        private int _wagonProbes, _wagonWayIx;
        private Action _aWagon;

        /// The Testing row: the nearest ambient wagon off screen within 150 m, moving or standing, with a driver; the next way tried on
        /// it; null when the probe began.
        private string WagonProbeStart(int now)
        {
            if (_wagon != null)
                return "a probe is going on already (" + (now - _wagon.StartedAt) / 1000 + " s)";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return "no player";
            int mine = PED.GET_VEHICLE_PED_IS_IN(me.Handle, false);
            Vehicle best = null;
            float bestD = 150f;
            string why = "no wagon within 150 m";
            foreach (Vehicle v in World.GetAllVehicles())
            {
                if (v == null || !v.Exists())
                    continue;
                int h = v.Handle;
                if (h == mine || !VEHICLE.IS_DRAFT_VEHICLE(h))
                    continue;
                float d = v.Position.DistanceTo(me.Position);
                if (d >= bestD)
                    continue;
                if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h))
                {
                    why = "the nearest wagon is a mission's";
                    continue;
                }
                if (ENTITY.IS_ENTITY_ON_SCREEN(h))
                {
                    why = "the nearest wagon is in view (the probe works off screen only)";
                    continue;
                }
                int driver = VEHICLE.GET_PED_IN_VEHICLE_SEAT(h, -1);
                if (driver == 0 || !ENTITY.DOES_ENTITY_EXIST(driver) || ENTITY.IS_ENTITY_DEAD(driver))
                {
                    why = "the nearest wagon has no driver";
                    continue;
                }
                best = v;
                bestD = d;
            }
            if (best == null)
                return why;
            int veh = best.Handle;
            string way = WagonWheel.Ways[_wagonWayIx % WagonWheel.Ways.Length];
            _wagonWayIx++;
            WagonProbe probe = new WagonProbe { Model = "model 0x" + ENTITY.GET_ENTITY_MODEL(veh).ToString("X8"), Way = way };
            for (int w = 0; w < WagonWheel.WheelsToTry; w++)
                if (!VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, w))
                {
                    probe.Wheel = w;
                    break;
                }
            if (probe.Wheel < 0)
                return "every wheel of the nearest wagon reads broken already";
            bool mission = false;
            string did;
            switch (way)
            {
                case "draft":
                    WagonBreakFlags(veh);
                    VEHICLE._BREAK_OFF_DRAFT_WHEEL(veh, probe.Wheel, 1000f);
                    did = "the can-break flags, then the draft-wheel native with force 1000";
                    break;
                case "wheel":
                    {
                        WagonBreakFlags(veh);
                        int got = VEHICLE._BREAK_OFF_VEHICLE_WHEEL(veh, probe.Wheel);
                        did = "the can-break flags, then the break-off-wheel native (it returned " + got + ")";
                        break;
                    }
                case "mission":
                    ENTITY.SET_ENTITY_AS_MISSION_ENTITY(veh, true, true);
                    mission = true;
                    WagonBreakFlags(veh);
                    VEHICLE._BREAK_OFF_DRAFT_WHEEL(veh, probe.Wheel, 1000f);
                    did = "taken as a mission entity (" + (ENTITY.IS_ENTITY_A_MISSION_ENTITY(veh) ? "it took" : "it didn't take") + "), the can-break flags, then the draft-wheel native with force 1000";
                    break;
                case "damage":
                    {
                        WagonBreakFlags(veh);
                        string bone;
                        Vector3 off = WagonWheelOffset(veh, probe.Wheel, out bone);
                        VEHICLE.SET_VEHICLE_DAMAGE(veh, off.X, off.Y, off.Z, 1000f, 1.5f, true);
                        did = "the can-break flags, then damage 1000 within 1.5 m of " + bone + " (offset " + off.X.ToString("0.0") + ", " + off.Y.ToString("0.0") + ", " + off.Z.ToString("0.0") + ")";
                        break;
                    }
                default:
                    {
                        float before = VEHICLE.GET_VEHICLE_BODY_HEALTH(veh);
                        VEHICLE.SET_VEHICLE_BODY_HEALTH(veh, 500f);
                        did = "the body health set to 500: it read " + before.ToString("0") + " before and reads " + VEHICLE.GET_VEHICLE_BODY_HEALTH(veh).ToString("0") + " now (the check that natives take on this wagon)";
                        break;
                    }
            }
            probe.Broke = VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, probe.Wheel);
            _wagon = new WagonRun { Vehicle = veh, Driver = VEHICLE.GET_PED_IN_VEHICLE_SEAT(veh, -1), StartedAt = now, BrokeAt = now, NextRead = now + 500, At = best.Position, Probe = probe, Mission = mission };
            _wagonProbes++;
            Log.Write("WAGON probe (way \"" + way + "\"): " + probe.Model + " " + (int)bestD + " m off, off screen, " + (ENTITY.GET_ENTITY_SPEED(veh) > 0.5f ? "moving" : "standing") + ": wheel " + probe.Wheel +
                      ", " + did + " (this frame the game says " + (probe.Broke ? "destroyed" : "still whole") + "); watching up to " + WagonProbe.WatchMs / 1000 + " s");
            return null;
        }

        /// The flags that let the game break this wagon at all, before any way is tried.
        private static void WagonBreakFlags(int veh)
        {
            VEHICLE.SET_VEHICLE_CAN_BREAK(veh, true);
            VEHICLE.SET_VEHICLE_WHEELS_CAN_BREAK(veh, true);
            VEHICLE.SET_VEHICLE_CAN_BE_VISIBLY_DAMAGED(veh, true);
        }

        /// Where the wheel is on the wagon, as an offset from its centre: the wheel's bone if the model names it, else a guess.
        private static Vector3 WagonWheelOffset(int veh, int wheel, out string bone)
        {
            string name = WheelBones[Math.Max(0, Math.Min(WheelBones.Length - 1, wheel))];
            int bi = ENTITY.GET_ENTITY_BONE_INDEX_BY_NAME(veh, name);
            if (bi >= 0)
            {
                bone = "the bone " + name;
                Vector3 world = ENTITY.GET_WORLD_POSITION_OF_ENTITY_BONE(veh, bi);
                return ENTITY.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS(veh, world.X, world.Y, world.Z);
            }
            bone = "a guessed spot (no bone " + name + ")";
            return new Vector3(wheel % 2 == 0 ? -0.9f : 0.9f, wheel < 2 ? 1.4f : -1.4f, 0f);
        }

        /// Tick04: the readings, the early end when nothing broke, the mend at forty seconds, the end at sixty; nothing when no probe
        /// is going. Under its own Safe (the API's raw World.GetAllVehicles has the same silent failure mode as GetAllPeds).
        private void WagonProbeStep(int now)
        {
            if (_wagon == null || now < _wagon.NextRead)
                return;
            if (_aWagon == null)
                _aWagon = () => WagonProbeTick(_now);
            Safe("wagon probe", _aWagon);
        }

        private void WagonProbeTick(int now)
        {
            WagonRun r = _wagon;
            if (r == null)
                return;
            try
            {
                WagonProbeRead(r, now);
            }
            catch (Exception ex)
            {
                Log.Write("WAGON probe failed (" + ex.Message + "); ended");
                r.Probe.EndedWhy = "a fault: " + ex.Message;
                WagonProbeEnd(r);
            }
        }

        private void WagonProbeRead(WagonRun r, int now)
        {
            r.NextRead = now + WagonProbe.ReadEveryMs;
            int veh = r.Vehicle;
            if (!ENTITY.DOES_ENTITY_EXIST(veh))
            {
                r.Probe.EndedWhy = "the wagon is gone";
                WagonProbeEnd(r);
                return;
            }
            Ped me = Game.Player.Character;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(veh, true, true);
            if (me == null || !me.Exists() || at.DistanceTo(me.Position) > 220f)
            {
                r.Probe.EndedWhy = "you went too far from it";
                WagonProbeEnd(r);
                return;
            }
            bool driverIn = r.Driver != 0 && ENTITY.DOES_ENTITY_EXIST(r.Driver) && PED.IS_PED_IN_VEHICLE(r.Driver, veh, false);
            bool driverFleeing = r.Driver != 0 && ENTITY.DOES_ENTITY_EXIST(r.Driver) && (PED.IS_PED_FLEEING(r.Driver) || PED.IS_PED_IN_COMBAT(r.Driver, 0));
            float speed = ENTITY.GET_ENTITY_SPEED(veh);
            bool stopped = VEHICLE.IS_VEHICLE_STOPPED(veh) || VEHICLE._IS_VEHICLE_BROUGHT_TO_HALT(veh);
            int standing = 0;
            foreach (Vehicle v in World.GetAllVehicles())
            {
                if (v == null || !v.Exists() || v.Handle == veh)
                    continue;
                if (v.Position.DistanceTo(at) < 30f && ENTITY.GET_ENTITY_SPEED(v.Handle) < 0.3f)
                    standing++;
            }
            int since = now - (r.Mended ? r.MendedAt : r.BrokeAt);
            // the wheel read here, a frame or more after the native that changed it (the game applies damage on its next update): the
            // break counts once any reading says broken; the mend is judged by the first reading after it (the review of items 20-27)
            bool wheelBroken = VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, r.Probe.Wheel);
            if (!r.Mended)
                r.Probe.Broke |= wheelBroken;
            else if (!r.FixedRead)
            {
                r.FixedRead = true;
                r.Probe.Fixed = !wheelBroken;
            }
            r.Probe.Read(since, r.Mended, speed, stopped, driverIn, driverFleeing, standing, at.DistanceTo(r.At));
            Log.Detail("WAGON probe " + (r.Mended ? "after the mend " : "") + since / 1000 + " s: speed " + speed.ToString("0.0") + ", " + (stopped ? "standing" : "rolling") + ", wheel " +
                       (wheelBroken ? "broken" : "whole") + ", the driver " + (driverIn ? "on the box" : "off it") + (driverFleeing ? " and running" : "") +
                       ", " + standing + " standing within 30 m, " + (int)at.DistanceTo(r.At) + " m from the break");
            if (!r.Mended && !r.Probe.Broke && since >= WagonProbe.NothingMs)
            {
                r.Probe.EndedWhy = "this way did nothing in " + WagonProbe.NothingMs / 1000 + " s";
                WagonProbeEnd(r);
            }
            else if (!r.Mended && since >= WagonProbe.WatchMs)
            {
                VEHICLE.SET_VEHICLE_FIXED(veh);
                r.Mended = true;
                r.MendedAt = now;
                r.Probe.Fixed = !VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, r.Probe.Wheel);
                Log.Write("WAGON probe: mended (SET_VEHICLE_FIXED): the wheel reads " + (r.Probe.Fixed ? "whole" : "still broken") + " this frame (judged again at the next reading); watching " + WagonProbe.AfterMs / 1000 + " s more");
            }
            else if (r.Mended && since >= WagonProbe.AfterMs)
            {
                r.Probe.EndedWhy = "done";
                WagonProbeEnd(r);
            }
        }

        /// The probe over: the wagon left as the game had it (mended if it wasn't yet, the game's own again if it was taken), the
        /// report and the verdict in the log and a notice, and which way the next press tries.
        private unsafe void WagonProbeEnd(WagonRun r)
        {
            _wagon = null;
            try
            {
                if (ENTITY.DOES_ENTITY_EXIST(r.Vehicle))
                {
                    if (!r.Mended)
                        VEHICLE.SET_VEHICLE_FIXED(r.Vehicle);
                    if (r.Mission)
                    {
                        int v = r.Vehicle;
                        ENTITY.SET_VEHICLE_AS_NO_LONGER_NEEDED(&v);   // the game's own again
                    }
                }
            }
            catch
            {
            }
            _wagonLast = r.Probe;
            string next = WagonWheel.NextWay(r.Probe.Way);
            Log.Write("WAGON probe over: " + r.Probe.Report());
            Log.Write("WAGON probe verdict: " + r.Probe.Verdict() + (r.Probe.Broke ? "" : "; the next press tries way \"" + next + "\": " + WagonWheel.WayWords(next)));
            Feed.Notice("RealNPCs", "Wagon probe, way \"" + r.Probe.Way + "\": " + r.Probe.Verdict() + (r.Probe.Broke ? "" : ". Press again for way \"" + next + "\"."));
        }

        private string WagonStatus()
        {
            string next = WagonWheel.Ways[_wagonWayIx % WagonWheel.Ways.Length];
            return _wagonProbes + " probes this session" + (_wagon != null ? "; one going (way \"" + _wagon.Probe.Way + "\", " + (Game.GameTime - _wagon.StartedAt) / 1000 + " s)" : "") +
                   (_wagonLast != null ? "; last: " + _wagonLast.Report() + "; verdict: " + _wagonLast.Verdict() : "; none run yet (the Testing row \"A wagon's wheel off\")") +
                   "; the next press tries way \"" + next + "\": " + WagonWheel.WayWords(next);
        }
    }
}
