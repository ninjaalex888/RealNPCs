// The shivaree, staged (FIVEDAY item 13; Core/Shivaree.cs, Core/Engine.Shivaree.cs): the night after a wedding in a town, by
// the saloon he last came out of (else a spot unseen 18-35 m off), five of the couple's friends with their noise (a caption,
// their cheering and mocking voices), their calls for the groom; at the fifth beat a man walks in from out of view, the groom,
// says his piece and stands them a round; their cheer, and they head off. A minute there counts as having been to it. The
// dance's rails: nothing spawned in view, never in missions, shots or a gun drawn break it up. Behind [World] TownEvents and
// EventShivarees.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _shivareesStaged;

        private void BeginShivaree(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at;
            if (_saloonDoor.TryGetValue(e.Town, out at) && at.DistanceTo(me.Position) < 120f && !Natives.Looking(at + new Vector3(0f, 0f, 1f), 50f))
            {
                Vector3 spot;
                if (Natives.StandSpot(at + new Vector3(-4f, 4f, 0f), at.Z, out spot))
                    at = spot;
            }
            else if (!EncounterSpot(me, 18f, 35f, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 4000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 7);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 8);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 9);   // the groom
            _eventRun = run;
            _eventsStaged++;
            _shivareesStaged++;
            Log.Write("EVENT shivaree " + e.Town + ": staging for " + e.AName + " and " + e.BName + " " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void ShivareeStep(EventRun r, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            TownEvent e = r.E;
            if (!r.Ready)
            {
                if (!EventCastReady(r))
                {
                    if (now - r.Since > 15000)
                        EventRunLost(r, now, "the game never streamed the bodies in");
                    return;
                }
                if (!Spawner.SafeToSpawn())
                    return;
                for (int i = 0; i < 5; i++)
                {
                    double ang = i * Math.PI * 2 / 5;
                    Ped p = Spawner.Spawn(i % 3 == 2 ? r.WomanModel : r.ManModel, r.At + new Vector3((float)Math.Cos(ang) * 2.2f, (float)Math.Sin(ang) * 2.2f, 0f), (float)(ang * 180 / Math.PI));
                    if (p == null)
                        continue;
                    r.Cast.Add(p);
                }
                if (r.Cast.Count < 3)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = r.Cast[0];
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != r.Fiddler)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, r.Fiddler.Handle, 60000, 0f, 0f, 0f);   // round the one leading the din
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT shivaree " + e.Town + ": " + r.Cast.Count + " friends gathered with their pans");
                return;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
                    _eventRun = null;
                    _eventsNext = now + 60000;
                }
                return;
            }
            bool byYou = false;
            string broke = now - r.SpawnedAt < 1500 ? null : EventBroken(r, out byYou);
            if (broke != null)
            {
                BreakEventRun(r, byYou, broke, now, g);
                return;
            }
            float d = me.Position.DistanceTo(r.At);
            if (d > 120f || g >= e.Until)
            {
                bool stayed = now - r.Since >= 60000;
                _engine.FinishEvent(e, stayed, g);
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                r.ReleaseAt = now + 10000;
                return;
            }
            if (now < r.NextCall || d > 50f)
                return;
            r.NextCall = now + 6500 + _rng.Next(2000);
            Ped any = r.Cast[_rng.Next(r.Cast.Count)];
            switch (r.Step)
            {
                case 0: case 2:
                    SpeechCaption("The shivaree", Shivaree.DinLine(e.Id + "|" + r.Step), r.Fiddler.Handle, 3500, false, false);
                    foreach (Ped p in r.Cast)
                        Speech.SayAny(p.Handle, _rng.Next(2) == 0 ? Speech.Cheer : Speech.Mock, 0);
                    break;
                case 1: case 3:
                    SpeechCaption("A friend of the couple", Shivaree.CallLine(e, e.Id + "|" + r.Step), any.Handle, 4500, false, false);
                    Speech.SayAny(any.Handle, Speech.Cheer, 0);
                    break;
                case 4:
                    {
                        Vector3 from = r.At + new Vector3(0f, 9f, 0f);
                        Vector3 spot;
                        Ped groom = Natives.StandSpot(from, r.At.Z, out spot) && !Natives.Looking(spot + new Vector3(0f, 0f, 1f), 40f) ? Spawner.Spawn(r.FiddlerModel, spot, 180f) : null;
                        if (groom != null)
                        {
                            r.Cast.Add(groom);
                            r.Health[groom.Handle] = ENTITY.GET_ENTITY_HEALTH(groom.Handle);
                            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(groom.Handle, true);
                            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(groom.Handle, r.At.X, r.At.Y + 2.5f, r.At.Z, 1.3f, 20000, 0.8f, 0, 0f);   // the ring's walk (WestworldScript.Ring.cs)
                            r.Preacher = groom;
                        }
                        break;
                    }
                case 5:
                    SpeechCaption(Person.FirstOf(e.AName) ?? "The groom", Shivaree.GroomLine(e.Id), (r.Preacher ?? r.Fiddler).Handle, 5000, false, false);
                    break;
                case 6:
                    SpeechCaption("The shivaree", Shivaree.AfterLine(e.Id), any.Handle, 4000, false, false);
                    foreach (Ped p in r.Cast)
                        Speech.SayAny(p.Handle, Speech.Cheer, 0);
                    break;
                default:
                    bool there = true;   // each beat waits on him within 50 m, so reaching the end is having stood through it (the Part 3 review: the minute's test missed nineteen in twenty)
                    _engine.FinishEvent(e, there, g);
                    if (there) _eventsAttended++;
                    foreach (Ped p in r.Cast)
                        WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                    Log.Write("EVENT shivaree " + e.Town + ": over; you were there");
                    r.ReleaseAt = now + 14000;
                    return;
            }
            r.Step++;
        }

        private string ShivareeStatus()
        {
            return !_cfg.EventShivarees ? "off ([World] EventShivarees)" : _shivareesStaged + " staged this session";
        }
    }
}
