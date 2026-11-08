// The festivals, staged (FIVEDAY reserve R17-R19; Core/Festivals.cs, Core/Engine.Festivals.cs): at the kind's spot in the
// town (else a spot unseen 20-40 m off), nine of the crowd spawned out of view in a loose ring, the first of them the one who
// calls it; the kind's beats in order every seven to nine seconds while you're within 60 m, the crowd's cheers between, the
// winners named. Reaching the end is attending. The shivaree's rails. Behind each kind's own switch.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _festStaged;

        private void BeginFestival(TownEvent e, int now)
        {
            FestivalKind f = Festivals.Of(e.Kind);
            if (f == null)
                return;
            Ped me = Game.Player.Character;
            Spot spot = Spots.Nearest(_spots, e.Town, f.Spot, me.Position.X, me.Position.Y);
            Vector3 at;
            if (spot != null && new Vector3(spot.X, spot.Y, spot.Z).DistanceTo(me.Position) < 150f && !Natives.Looking(new Vector3(spot.X, spot.Y, spot.Z + 1f), 50f))
                at = new Vector3(spot.X, spot.Y, spot.Z);
            else if (!EncounterSpot(me, 20f, 40f, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 61);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 62);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 63);
            _eventRun = run;
            _eventsStaged++;
            _festStaged++;
            Log.Write("EVENT " + e.Kind + " " + e.Town + ": staging " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void FestivalStep(EventRun r, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            TownEvent e = r.E;
            FestivalKind f = Festivals.Of(e.Kind);
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
                Ped caller = Spawner.Spawn(r.FiddlerModel, r.At, 0f);
                if (caller != null)
                    r.Cast.Add(caller);
                for (int i = 0; i < 9; i++)
                {
                    double ang = i * Math.PI * 2 / 9;
                    Ped p = Spawner.Spawn(i % 2 == 1 ? r.WomanModel : r.ManModel, r.At + new Vector3((float)Math.Cos(ang) * 5f, (float)Math.Sin(ang) * 5f, 0f), (float)(ang * 180 / Math.PI) + 180f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                if (caller == null || r.Cast.Count < 5)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = caller;
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != caller)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, caller.Handle, 150000, 0f, 0f, 0f);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT " + e.Kind + " " + e.Town + ": the crowd gathered");
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
            if (d > 120f || g >= e.Until || f == null)
            {
                bool stayed = now - r.Since >= 60000;
                _engine.FinishEvent(e, stayed, g);
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                r.ReleaseAt = now + 10000;
                return;
            }
            if (now < r.NextCall || d > 60f)
                return;
            r.NextCall = now + 7000 + _rng.Next(2000);
            if (r.Step >= f.Beats.Length)
            {
                _engine.FinishEvent(e, true, g);
                _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                Log.Write("EVENT " + e.Kind + " " + e.Town + ": over, with you in the crowd");
                r.ReleaseAt = now + 14000;
                return;
            }
            string line = Festivals.Fill(f.Beats[r.Step], e.Town, e.AName, e.BName);
            bool stage = line.StartsWith("[", StringComparison.Ordinal);
            SpeechCaption(stage ? f.Title : "The caller", line, r.Fiddler.Handle, 5000, false, !stage);
            if (stage)
                foreach (Ped p in r.Cast)
                    if (p != r.Fiddler)
                        Speech.SayAny(p.Handle, Speech.Cheer, 0);
            r.Step++;
        }

        private string FestivalStatus()
        {
            return _festStaged + " festivals staged this session";
        }
    }
}
