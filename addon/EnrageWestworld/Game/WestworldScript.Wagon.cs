// A broken wagon wheel, the probe (SIXDAY item 26; roadmap 32; Core/WagonWheel.cs). The Testing row "wagon" takes an
// ambient wagon off screen with a driver on it (never the player's, never a mission's, never a train), breaks one wheel
// with the game's own draft-wheel native, reads the game's answers every two seconds for forty (does the wheel read
// broken, does the wagon stop, does the driver get down or run, do the vehicles behind it stand), mends it
// (SET_VEHICLE_FIXED) and reads twenty seconds more (does the wheel read whole, does it move on), then says what it
// found in the log and F7 "TEST wagon:" and hands the wagon back as it was. Nothing is spawned; the scene itself waits on
// what this says (roadmap 32: "first to work out in game").
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
            public bool Mended, FixedRead;   // FixedRead: the first reading after the mend has said whether the wheel is whole
            public WagonProbe Probe;
        }

        private WagonRun _wagon;
        private WagonProbe _wagonLast;
        private int _wagonProbes;
        private Action _aWagon;

        /// The Testing row: the nearest ambient wagon off screen within 150 m, moving or standing, with a driver; null when it began.
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
            WagonProbe probe = new WagonProbe { Model = "model 0x" + ENTITY.GET_ENTITY_MODEL(veh).ToString("X8") };
            for (int w = 0; w < WagonWheel.WheelsToTry; w++)
                if (!VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, w))
                {
                    probe.Wheel = w;
                    break;
                }
            if (probe.Wheel < 0)
                return "every wheel of the nearest wagon reads broken already";
            VEHICLE.SET_VEHICLE_WHEELS_CAN_BREAK(veh, true);
            VEHICLE._BREAK_OFF_DRAFT_WHEEL(veh, probe.Wheel, 1f);
            probe.Broke = VEHICLE._IS_VEHICLE_WHEEL_DESTROYED(veh, probe.Wheel);
            _wagon = new WagonRun { Vehicle = veh, Driver = VEHICLE.GET_PED_IN_VEHICLE_SEAT(veh, -1), StartedAt = now, BrokeAt = now, NextRead = now + 500, At = best.Position, Probe = probe };
            _wagonProbes++;
            Log.Write("WAGON probe: " + probe.Model + " " + (int)bestD + " m off, off screen, " + (ENTITY.GET_ENTITY_SPEED(veh) > 0.5f ? "moving" : "standing") + ": wheel " + probe.Wheel +
                      " broken off (" + (probe.Broke ? "the game says destroyed" : "the game says still whole") + "); watching " + WagonProbe.WatchMs / 1000 + " s");
            return null;
        }

        /// Tick04: the readings, the mend at forty seconds, the end at sixty; nothing when no probe is going. Under its own Safe (the
        /// review of items 20-27: the API's raw World.GetAllVehicles has the same silent failure mode as GetAllPeds).
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
            if (!r.Mended && since >= WagonProbe.WatchMs)
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

        /// The probe over: the wagon left as the game had it (mended if it wasn't yet), the report in the log.
        private void WagonProbeEnd(WagonRun r)
        {
            _wagon = null;
            try
            {
                if (!r.Mended && ENTITY.DOES_ENTITY_EXIST(r.Vehicle))
                    VEHICLE.SET_VEHICLE_FIXED(r.Vehicle);
            }
            catch
            {
            }
            _wagonLast = r.Probe;
            Log.Write("WAGON probe over: " + r.Probe.Report());
            Log.Write("WAGON probe verdict: " + r.Probe.Verdict());
            Feed.Notice("RealNPCs", "Wagon probe: " + r.Probe.Verdict());
        }

        private string WagonStatus()
        {
            return _wagonProbes + " probes this session" + (_wagon != null ? "; one going (" + (Game.GameTime - _wagon.StartedAt) / 1000 + " s)" : "") +
                   (_wagonLast != null ? "; last: " + _wagonLast.Report() + "; verdict: " + _wagonLast.Verdict() : "; none run yet (the Testing row \"A wagon's wheel off\")");
        }
    }
}
