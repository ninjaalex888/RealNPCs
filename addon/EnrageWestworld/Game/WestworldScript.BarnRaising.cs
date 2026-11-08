// A barn raising, staged (FIVEDAY reserve R7; Core/BarnRaising.cs, Core/Engine.BarnRaising.cs): at the town's "corral" spot
// (else a spot unseen 20-40 m off), the foreman, six men (two of them the pair at odds, if the town has them, by name) and two
// women with the dinner, spawned out of view; beats every seven to nine seconds while you're within 50 m: the call, a bent
// lifted, the pair bickering on their beam, another bent, the pair after (mended or not, as the end will have it), the
// dinner, the family's thanks. Reaching the end is attending. The shivaree's rails. Behind [World] TownEvents and
// EventBarnRaisings.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _barnsStaged;

        private void BeginBarnRaising(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Spot spot = Spots.Nearest(_spots, e.Town, "corral", me.Position.X, me.Position.Y);
            Vector3 at;
            if (spot != null && new Vector3(spot.X, spot.Y, spot.Z).DistanceTo(me.Position) < 150f && !Natives.Looking(new Vector3(spot.X, spot.Y, spot.Z + 1f), 50f))
                at = new Vector3(spot.X, spot.Y, spot.Z);
            else if (!EncounterSpot(me, 20f, 40f, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 41);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 42);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 43);   // the foreman
            _eventRun = run;
            _eventsStaged++;
            _barnsStaged++;
            Log.Write("EVENT barnraising " + e.Town + ": staging for " + e.AName + " " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void BarnRaisingStep(EventRun r, int now)
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
                Ped foreman = Spawner.Spawn(r.FiddlerModel, r.At, 0f);
                if (foreman != null)
                    r.Cast.Add(foreman);
                for (int i = 0; i < 8; i++)
                {
                    Vector3 to = r.At + new Vector3(-6f + (i % 4) * 4f, i < 4 ? 4f : 7f, 0f);
                    Ped p = Spawner.Spawn(i >= 6 ? r.WomanModel : r.ManModel, to, 180f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                if (foreman == null || r.Cast.Count < 5)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = foreman;
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != foreman)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, foreman.Handle, 120000, 0f, 0f, 0f);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT barnraising " + e.Town + ": " + (r.Cast.Count - 1) + " neighbours at " + e.AName + "'s");
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
            r.NextCall = now + 7000 + _rng.Next(2000);
            string seed = e.Id + "|" + r.Step, aId, aName, bId, bName;
            bool mends, pair = Engine.BarnPair(e, out aId, out aName, out bId, out bName, out mends);
            Ped pa = r.Cast.Count > 1 ? r.Cast[1] : r.Fiddler, pb = r.Cast.Count > 2 ? r.Cast[2] : r.Fiddler;
            switch (r.Step)
            {
                case 0:
                    SpeechCaption("The foreman", BarnRaising.Call(seed), r.Fiddler.Handle, 4000);
                    break;
                case 1: case 3:
                    SpeechCaption("The barn raising", BarnRaising.Lift(seed), r.Fiddler.Handle, 4000, false, false);
                    foreach (Ped p in r.Cast)
                        if (p != r.Fiddler)
                            Speech.SayAny(p.Handle, Speech.Cheer, 0);
                    break;
                case 2:
                    if (pair)
                    {
                        SpeechCaption(Person.FirstOf(aName) ?? "A neighbour", BarnRaising.BickerLine(Person.FirstOf(bName), seed), pa.Handle, 4000);
                        break;
                    }
                    SpeechCaption("The foreman", BarnRaising.Call(seed + "b"), r.Fiddler.Handle, 3500);
                    break;
                case 4:
                    if (pair)
                        SpeechCaption(Person.FirstOf(bName) ?? "A neighbour", BarnRaising.After(mends, Person.FirstOf(aName), seed), pb.Handle, 4000);
                    break;
                case 5:
                    SpeechCaption("The barn raising", BarnRaising.DinnerLine(seed), r.Fiddler.Handle, 4500, false, false);
                    break;
                case 6:
                    SpeechCaption(Person.FirstOf(e.AName) ?? "The farmer", BarnRaising.FamilyLine(seed), r.Cast[r.Cast.Count - 1].Handle, 4500);
                    break;
                default:
                    _engine.FinishEvent(e, true, g);
                    _eventsAttended++;
                    foreach (Ped p in r.Cast)
                        WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                    Log.Write("EVENT barnraising " + e.Town + ": the barn's up, with you among them");
                    r.ReleaseAt = now + 14000;
                    return;
            }
            r.Step++;
        }

        private string BarnRaisingStatus()
        {
            return !_cfg.EventBarnRaisings ? "off ([World] EventBarnRaisings)" : _barnsStaged + " staged this session";
        }
    }
}
