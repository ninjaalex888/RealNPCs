// A river baptism, staged (FIVEDAY reserve R5; Core/Baptism.cs, Core/Engine.Baptism.cs): the Sunday after a camp meeting's
// altar call, at the town's "riverbank" spot (within 150 m of you and out of view; with none recorded, nothing is staged and
// it happens away), the preacher, the man being baptised (a body of the town's, spawned out of view, with his name), and five
// of the faithful on the bank, spawned; beats every seven to nine seconds while you're within 50 m: a hymn, the preacher's
// words, he goes under, he comes up, the amens, his words, his thought. Reaching the end there is attending. The revival's
// rails. Behind [World] TownEvents and EventBaptisms.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _baptismsStaged;

        private void BeginBaptism(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Spot spot = Spots.Nearest(_spots, e.Town, "riverbank", me.Position.X, me.Position.Y);
            if (spot == null)
                return;   // no riverbank recorded: it happens away (the engine finishes it as the hour passes)
            var at = new Vector3(spot.X, spot.Y, spot.Z);
            if (at.DistanceTo(me.Position) > 150f || Natives.Looking(at + new Vector3(0f, 0f, 1f), 50f))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 21);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 22);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 23);   // the preacher
            _eventRun = run;
            _eventsStaged++;
            _baptismsStaged++;
            Log.Write("EVENT baptism " + e.Town + ": staging for " + e.AName + " at the riverbank, " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void BaptismStep(EventRun r, int now)
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
                Ped preacher = Spawner.Spawn(r.FiddlerModel, r.At, 0f);
                Ped man = Spawner.Spawn(r.ManModel, r.At + new Vector3(1.2f, 0f, 0f), 270f);
                if (preacher != null) r.Cast.Add(preacher);
                if (man != null) r.Cast.Add(man);
                for (int i = 0; i < 5; i++)
                {
                    double ang = Math.PI * (0.6 + i * 0.2);
                    Ped p = Spawner.Spawn(i % 2 == 0 ? r.WomanModel : r.ManModel, r.At + new Vector3((float)Math.Cos(ang) * 4f, (float)Math.Sin(ang) * 4f, 0f), (float)(ang * 180 / Math.PI) + 180f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                if (preacher == null || man == null || r.Cast.Count < 4)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = preacher;
                r.Preacher = man;   // (the run's second role: here, the man being baptised)
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != r.Fiddler && p != man)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, man.Handle, 120000, 0f, 0f, 0f);
                }
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(man.Handle, preacher.Handle, 120000, 0f, 0f, 0f);
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT baptism " + e.Town + ": " + (r.Cast.Count - 2) + " on the bank for " + e.AName);
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
            string seed = e.Id + "|" + r.Step, preacherName = e.Extra ?? "The preacher", hisName = Person.FirstOf(e.AName) ?? "The man";
            Ped any = r.Cast[2 + _rng.Next(Math.Max(1, r.Cast.Count - 2))];
            switch (r.Step)
            {
                case 0:
                    SpeechCaption("The river", Baptism.Hymn(seed), r.Fiddler.Handle, 4500, false, false);
                    break;
                case 1:
                    SpeechCaption(preacherName, Baptism.PreacherWords(seed), r.Fiddler.Handle, 5500);
                    break;
                case 2:
                    SpeechCaption("The river", Baptism.GoesUnder(seed), r.Preacher.Handle, 3500, false, false);
                    break;
                case 3:
                    SpeechCaption("The river", Baptism.ComesUp(seed), r.Preacher.Handle, 3500, false, false);
                    SpeechCaption("One of the faithful", Baptism.Amen(seed), any.Handle, 2500);
                    foreach (Ped p in r.Cast)
                        if (p != r.Fiddler && p != r.Preacher)
                            Speech.SayAny(p.Handle, Speech.Cheer, 0);
                    break;
                case 4:
                    SpeechCaption(hisName, Baptism.His(seed), r.Preacher.Handle, 4500);
                    _ticker.Push((e.AName ?? "The man") + ", thinking", Baptism.Thought(seed), "near", r.Preacher.Handle);
                    break;
                default:
                    _engine.FinishEvent(e, true, g);
                    _eventsAttended++;
                    foreach (Ped p in r.Cast)
                        WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                    Log.Write("EVENT baptism " + e.Town + ": over, with you on the bank");
                    r.ReleaseAt = now + 14000;
                    return;
            }
            r.Step++;
        }

        private string BaptismStatus()
        {
            return !_cfg.EventBaptisms ? "off ([World] EventBaptisms)" : _baptismsStaged + " staged this session";
        }
    }
}
