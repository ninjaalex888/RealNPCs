// A camp meeting, staged (FIVEDAY reserve R4; Core/Revival.cs, Core/Engine.Revival.cs): each of the three nights, at the
// town's "showground" spot (else its "churchsteps", else a spot unseen 20-40 m off), a preacher and six of the faithful in a
// ring of three metres, spawned out of view; beats every seven to nine seconds while you're within 50 m: a hymn, the night's
// preaching (the call to repent, hellfire, the altar call), the amens, the preaching again, a hymn; on the third night the
// saved man's name and his words at the bench. Reaching the end there counts as attending. The shivaree's rails: nothing
// spawned in view, never in missions, shots or a gun drawn break it up. Behind [World] TownEvents and EventRevivals.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _revivalsStaged;

        private void BeginRevival(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Spot spot = Spots.Nearest(_spots, e.Town, "showground", me.Position.X, me.Position.Y) ?? Spots.Nearest(_spots, e.Town, "churchsteps", me.Position.X, me.Position.Y);
            Vector3 at;
            if (spot != null && new Vector3(spot.X, spot.Y, spot.Z).DistanceTo(me.Position) < 150f && !Natives.Looking(new Vector3(spot.X, spot.Y, spot.Z + 1f), 50f))
                at = new Vector3(spot.X, spot.Y, spot.Z);
            else if (!EncounterSpot(me, 20f, 40f, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 11);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 12);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 13);   // the preacher
            _eventRun = run;
            _eventsStaged++;
            _revivalsStaged++;
            Log.Write("EVENT revival " + e.Town + ": night " + e.Count + " staging " + (int)at.DistanceTo(me.Position) + " m off" + (spot != null ? " at the " + spot.Kind : ""));
        }

        private void RevivalStep(EventRun r, int now)
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
                if (preacher != null)
                    r.Cast.Add(preacher);
                for (int i = 0; i < 6; i++)
                {
                    double ang = i * Math.PI * 2 / 6;
                    Ped p = Spawner.Spawn(i % 2 == 1 ? r.WomanModel : r.ManModel, r.At + new Vector3((float)Math.Cos(ang) * 3f, (float)Math.Sin(ang) * 3f, 0f), (float)(ang * 180 / Math.PI) + 180f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                if (preacher == null || r.Cast.Count < 4)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = preacher;
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != r.Fiddler)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, r.Fiddler.Handle, 120000, 0f, 0f, 0f);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT revival " + e.Town + ": " + (r.Cast.Count - 1) + " of the faithful round " + (e.Extra ?? "the preacher"));
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
            Ped any = r.Cast[1 + _rng.Next(r.Cast.Count - 1)];
            string preacherName = e.Extra ?? "The preacher", seed = e.Id + "|" + r.Step;
            switch (r.Step)
            {
                case 0: case 4:
                    SpeechCaption("The camp meeting", Revival.Hymn(seed), r.Fiddler.Handle, 4000, false, false);
                    break;
                case 1: case 3:
                    SpeechCaption(preacherName, Revival.Sermon(e.Count, seed), r.Fiddler.Handle, 5500);
                    break;
                case 2:
                    SpeechCaption("One of the faithful", Revival.Amen(seed), any.Handle, 2500);
                    foreach (Ped p in r.Cast)
                        if (p != r.Fiddler)
                            Speech.SayAny(p.Handle, Speech.Cheer, 0);
                    break;
                case 5:
                    if (e.Count >= Revival.Nights)
                    {
                        string saved = _engine.RevivalSinner(e, g / 1440);
                        if (saved != null)
                        {
                            Hint(saved + " goes up to the mourners' bench.", 5000);
                            SpeechCaption(Person.FirstOf(saved) ?? "A sinner", Revival.SavedLine(seed), any.Handle, 4500);
                            break;
                        }
                    }
                    SpeechCaption("One of the faithful", Revival.Amen(seed + "|b"), any.Handle, 2500);
                    break;
                default:
                    _engine.FinishEvent(e, true, g);   // each beat waits on you within 50 m: reaching the end is having been there
                    _eventsAttended++;
                    foreach (Ped p in r.Cast)
                        WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                    Log.Write("EVENT revival " + e.Town + ": night " + e.Count + " over, with you in the tent");
                    r.ReleaseAt = now + 14000;
                    return;
            }
            r.Step++;
        }

        private string RevivalGameStatus()
        {
            return _engine.RevivalStatus() + "; " + _revivalsStaged + " nights staged this session";
        }
    }
}
