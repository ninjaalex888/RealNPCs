// Town base ball, staged (FIVEDAY reserve R16; Core/BaseBall.cs, Core/Engine.BaseBall.cs): at the home town's "showground"
// spot (else a spot unseen 20-40 m off), a pitcher and a batter twenty metres apart, two fielders and six of the crowd,
// spawned out of view; beats every six to eight seconds while you're within 60 m: the pitch, a hit or a strike, the crowd's
// shout, the innings' score at the third, sixth and ninth, the result. Reaching the end is attending. The shivaree's rails.
// Behind [World] TownEvents and EventBaseBall.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _ballStaged;

        private void BeginBaseBall(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Spot spot = Spots.Nearest(_spots, e.Town, "showground", me.Position.X, me.Position.Y);
            Vector3 at;
            if (spot != null && new Vector3(spot.X, spot.Y, spot.Z).DistanceTo(me.Position) < 150f && !Natives.Looking(new Vector3(spot.X, spot.Y, spot.Z + 1f), 50f))
                at = new Vector3(spot.X, spot.Y, spot.Z);
            else if (!EncounterSpot(me, 20f, 40f, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 51);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 52);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 53);   // the pitcher
            _eventRun = run;
            _eventsStaged++;
            _ballStaged++;
            Log.Write("EVENT baseball at " + e.Town + ": staging " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void BaseBallStep(EventRun r, int now)
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
                Ped pitcher = Spawner.Spawn(r.FiddlerModel, r.At, 0f);
                Ped batter = Spawner.Spawn(r.ManModel, r.At + new Vector3(0f, 18f, 0f), 180f);
                foreach (Ped p in new[] { pitcher, batter })
                    if (p != null)
                        r.Cast.Add(p);
                Vector3[] spots = { new Vector3(-12f, 6f, 0f), new Vector3(12f, 6f, 0f), new Vector3(-8f, 24f, 0f), new Vector3(-6f, 25f, 0f), new Vector3(-4f, 26f, 0f), new Vector3(6f, 25f, 0f), new Vector3(8f, 24f, 0f), new Vector3(10f, 23f, 0f) };
                for (int i = 0; i < spots.Length; i++)
                {
                    Ped p = Spawner.Spawn(i % 3 == 2 ? r.WomanModel : r.ManModel, r.At + spots[i], 180f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                if (pitcher == null || batter == null || r.Cast.Count < 5)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = pitcher;
                r.Preacher = batter;
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != pitcher)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, pitcher.Handle, 150000, 0f, 0f, 0f);
                }
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(pitcher.Handle, batter.Handle, 150000, 0f, 0f, 0f);
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT baseball at " + e.Town + ": the nines out, " + (r.Cast.Count - 2) + " in the field and the crowd");
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
            if (now < r.NextCall || d > 60f)
                return;
            r.NextCall = now + 6000 + _rng.Next(2000);
            string seed = e.Id + "|" + r.Step;
            int h, a;
            BaseBall.Score(e.Id ?? "", out h, out a);
            Ped crowd = r.Cast[2 + _rng.Next(Math.Max(1, r.Cast.Count - 2))];
            switch (r.Step % 4)
            {
                case 0:
                    SpeechCaption("Base ball", BaseBall.Pitch(seed), r.Fiddler.Handle, 3500, false, false);
                    break;
                case 1:
                    if (Voice.Chance(seed + "|hit", 45))
                        SpeechCaption("Base ball", BaseBall.Hit(seed), r.Preacher.Handle, 3500, false, false);
                    else
                        SpeechCaption("The umpire", BaseBall.Strike(seed), r.Preacher.Handle, 3000);
                    break;
                case 2:
                    SpeechCaption("The crowd", BaseBall.Cheer(e.Town, seed), crowd.Handle, 3000);
                    foreach (Ped p in r.Cast)
                        if (p != r.Fiddler && p != r.Preacher)
                            Speech.SayAny(p.Handle, Speech.Cheer, 0);
                    break;
                default:
                    {
                        int inning = (r.Step / 4 + 1) * 3;
                        if (inning >= 9)
                        {
                            SpeechCaption("Base ball", BaseBall.Result(e.Town, e.Extra, h, a), r.Fiddler.Handle, 5000, false, false);
                            _engine.FinishEvent(e, true, g);
                            _eventsAttended++;
                            foreach (Ped p in r.Cast)
                                WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                            Log.Write("EVENT baseball at " + e.Town + ": over, with you in the crowd");
                            r.ReleaseAt = now + 14000;
                            return;
                        }
                        SpeechCaption("Base ball", BaseBall.Innings(e.Town, e.Extra, h * inning / 9, a * inning / 9, inning), r.Fiddler.Handle, 4000, false, false);
                        break;
                    }
            }
            r.Step++;
        }

        private string BaseBallStatus()
        {
            return !_cfg.EventBaseBall ? "off ([World] EventBaseBall)" : _ballStaged + " games staged this session; the season: Valentine " + _store.Count("baseball:Valentine") + ", Strawberry " + _store.Count("baseball:Strawberry");
        }
    }
}
