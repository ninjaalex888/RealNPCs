// The Wild West show, staged (SIXDAY item 10d; Core/Shows.cs, Core/Engine.Shows.cs): on the ground just outside the town's
// edge on the player's side (else behind the camera), a barker, two riders in the ring (on foot: the blanks and the horses
// are in the captions), a crowd of eight. The barker's opening, the bill, the act told in four captions from the most bent
// version of the player's own legend, the closing with "the real one". J while the act runs: heckle (the barker answers, the
// crowd jeers, the town's regulars think the less of him), stand up as the real one (a deed: the crowd stares, some leave),
// rob the takings (the money, a robbery deed, the show breaks up), or just watch. The wedding's rails as the dance's.
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
        private int _showsStaged, _showHeckled, _showRevealed, _showRobbed;

        /// The show ground: 25 m past the town's edge on his side of it, unseen; else a spot behind the camera.
        private bool ShowGround(TownEvent e, Ped me, out Vector3 at)
        {
            Vector3 centre;
            float radius;
            if (_bridge.TownCore(e.Town, out centre, out radius))
            {
                float dx = me.Position.X - centre.X, dy = me.Position.Y - centre.Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 1f) { dx = 1f; dy = 0f; len = 1f; }
                float reach = radius + 25f;
                Vector3 edge = new Vector3(centre.X + dx / len * reach, centre.Y + dy / len * reach, me.Position.Z);
                Vector3 spot;
                if (Natives.StandSpot(edge, me.Position.Z, out spot) && spot.DistanceTo(me.Position) > 20f && spot.DistanceTo(me.Position) < 120f && !Natives.Looking(spot + new Vector3(0f, 0f, 1f), 50f))
                {
                    at = spot;
                    return true;
                }
            }
            return EncounterSpot(me, 30f, 50f, out at);
        }

        private void BeginShow(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 centre;
            float radius;
            if (!_bridge.TownCore(e.Town, out centre, out radius) || centre.DistanceTo(me.Position) > TownEvents.NearMetres)
                return;
            Vector3 at;
            if (!ShowGround(e, me, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 6000, Speaker = e.AName };
            int seed = (e.Id ?? "").GetHashCode();
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 6);   // the barker (the run's first place)
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 1);
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 2);
            run.Lines = Shows.Act(_engine.ShowVersion(e), e.Id);
            _eventRun = run;
            _eventsStaged++;
            _showsStaged++;
            Log.Write("EVENT show " + e.Town + ": staging " + e.AName + " " + (int)at.DistanceTo(me.Position) + " m off; the bill: " + e.Extra);
        }

        private void ShowStep(EventRun r, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
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
                Vector3 p0 = r.At;   // the barker's stand: the ring behind him (-y), the crowd before him (+y)
                r.Fiddler = Spawner.Spawn(r.FiddlerModel, p0, 0f);
                if (r.Fiddler == null)
                {
                    EventRunLost(r, now, "the barker never appeared");
                    return;
                }
                r.Cast.Add(r.Fiddler);
                Ped a = Spawner.Spawn(r.ManModel, p0 + new Vector3(-4f, -5f, 0f), 0f), b = Spawner.Spawn(r.ManModel, p0 + new Vector3(4f, -5f, 0f), 0f);
                if (a != null) { r.Cast.Add(a); r.Performers.Add(a); }
                if (b != null) { r.Cast.Add(b); r.Performers.Add(b); }
                if (a != null && b != null)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(a.Handle, b.Handle, 240000, 0f, 0f, 0f);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(b.Handle, a.Handle, 240000, 0f, 0f, 0f);
                }
                float[][] spots = { new[] { -5f, 6f }, new[] { -3f, 7f }, new[] { -1f, 7.5f }, new[] { 1f, 7.5f }, new[] { 3f, 7f }, new[] { 5f, 6f }, new[] { -2f, 9.5f }, new[] { 2f, 9.5f } };
                for (int i = 0; i < spots.Length; i++)
                {
                    Ped p = Spawner.Spawn(i % 3 == 1 ? r.WomanModel : r.ManModel, p0 + new Vector3(spots[i][0], spots[i][1], 0f), 180f);
                    if (p == null)
                        continue;
                    r.Cast.Add(p);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, r.Fiddler.Handle, 240000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(p.Handle, r.Fiddler.Handle, 240000, 0, 51, 0);
                }
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT show " + r.E.Town + ": the ground set; " + r.Performers.Count + " in the ring, " + (r.Cast.Count - 1 - r.Performers.Count) + " watching");
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
            if (_eventPrompt == "show" && now > _eventPromptUntil)
                PromptClear();
            bool byYou = false;
            string broke = now - r.SpawnedAt < 1500 ? null : EventBroken(r, out byYou);
            if (broke != null)
            {
                PromptClear();
                BreakEventRun(r, byYou, broke, now, g);
                return;
            }
            float d = me.Position.DistanceTo(r.At);
            if (d > 120f || (g >= r.E.Until && r.Step < 8))
            {
                PromptClear();
                bool stayed = now - r.Since >= 90000;
                _engine.FinishEvent(r.E, stayed, g);
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                Log.Write("EVENT show " + r.E.Town + ": " + (d > 120f ? "you rode off" : "the hour passed") + (stayed ? " after staying a while" : ""));
                r.ReleaseAt = now + 8000;
                return;
            }
            if (r.Step >= 2 && r.Step <= 7 && d < 25f && _eventPrompt == null)
                PromptSet("show", r.E, 12000);
            if (now < r.NextCall || d > 50f)
                return;
            r.NextCall = now + 9000 + _rng.Next(3000);
            Ped crowd = r.Cast.Count > 0 ? r.Cast[r.Cast.Count - 1] : r.Fiddler;
            switch (r.Step)
            {
                case 0:
                    SpeechCaption("The barker", Shows.Opening(r.E)[0], r.Fiddler.Handle, 6500, false, false);
                    Speech.SayAny(r.Fiddler.Handle, Speech.Cheer, 0);
                    break;
                case 1:
                    SpeechCaption("The barker", Shows.Opening(r.E)[1], r.Fiddler.Handle, 6500, false, false);
                    break;
                case 2:
                    SpeechCaption("The barker", Shows.Opening(r.E)[2], r.Fiddler.Handle, 6000, false, false);
                    break;
                case 3: case 4: case 5: case 6:
                    {
                        int i = r.Step - 3;
                        SpeechCaption("The ring", r.Lines[i], (r.Performers.Count > 0 ? r.Performers[i % r.Performers.Count] : r.Fiddler).Handle, 7000, false, false);
                        if (i == 1 && r.Performers.Count > 1)
                            Speech.SayAny(r.Performers[0].Handle, Speech.Threat, r.Performers[1].Handle);
                        if (i == 2 && r.Performers.Count > 1)
                            Speech.SayAny(r.Performers[1].Handle, Speech.Plead, r.Performers[0].Handle);
                        if (i == 3)
                            for (int k = 0; k < 3 && k < r.Cast.Count; k++)
                                Speech.SayAny(r.Cast[r.Cast.Count - 1 - k].Handle, Speech.Cheer, r.Fiddler.Handle);
                    }
                    break;
                case 7:
                    SpeechCaption("The barker", Shows.Closing(r.E.Id + "|close"), r.Fiddler.Handle, 6500, false, false);
                    PromptClear();
                    break;
                default:
                    _engine.FinishEvent(r.E, true, g);
                    _eventsAttended++;
                    SpeechCaption("Someone in the crowd", Shows.CrowdAfter((r.E.Part ?? "").Contains("revealed"), r.E.Id + "|after"), crowd.Handle, 5000, false, false);
                    foreach (Ped p in r.Cast)
                        WanderLater(p.Handle, now + (p == r.Fiddler || r.Performers.Contains(p) ? 12000 : 2000 + _rng.Next(8000)));
                    Log.Write("EVENT show " + r.E.Town + ": over; you were there (" + r.E.Outcome + ")");
                    r.ReleaseAt = now + 16000;
                    break;
            }
            r.Step++;
        }

        /// What he chose at the show (the J menu, WestworldScript.Trials.cs).
        private void ShowChoice(EventRun r, TownEvent e, string what, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            bool staged = r != null && r.E == e && r.Ready && r.Fiddler != null && r.Fiddler.Exists();
            switch (what)
            {
                case "heckle":
                    _engine.ShowHeckled(e, g);
                    _showHeckled++;
                    if (staged)
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Fiddler.Handle, me.Handle, 6000, 0f, 0f, 0f);
                        SpeechCaption("The barker", Shows.HeckleRetort(e.Id + "|retort"), r.Fiddler.Handle, 7000, true, false);
                        for (int k = 0; k < 3 && k < r.Cast.Count; k++)
                        {
                            Ped p = r.Cast[r.Cast.Count - 1 - k];
                            TASK.TASK_LOOK_AT_ENTITY(p.Handle, me.Handle, 6000, 0, 51, 0);
                            Speech.SayAny(p.Handle, Speech.Mock, me.Handle);
                        }
                        r.NextCall = now + 8000;
                    }
                    break;
                case "reveal":
                    _engine.ShowRevealed(e, g);
                    _showRevealed++;
                    if (staged)
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Fiddler.Handle, me.Handle, 8000, 0f, 0f, 0f);
                        SpeechCaption("The barker", Shows.RevealLine(e.Id + "|reveal"), r.Fiddler.Handle, 8000, true, false);
                        int n = 0;
                        foreach (Ped p in r.Cast)
                        {
                            if (p == r.Fiddler || r.Performers.Contains(p))
                                continue;
                            n++;
                            if (n % 4 == 0)
                                WanderLater(p.Handle, now + 1000 + _rng.Next(3000));   // some leave
                            else
                            {
                                TASK.TASK_LOOK_AT_ENTITY(p.Handle, me.Handle, 25000, 0, 51, 0);
                                if (n % 2 == 0)
                                    Speech.SayAny(p.Handle, Speech.Stare, me.Handle);
                            }
                        }
                        r.NextCall = now + 9000;
                    }
                    break;
                case "rob":
                    {
                        int cents = _engine.ShowRobbed(e, g);
                        if (cents > 0)
                            MONEY._MONEY_INCREMENT_CASH_BALANCE(cents, 0u);
                        _showRobbed++;
                        Hint("You take the day's takings: $" + cents / 100 + ".", 4000);
                        if (staged)
                        {
                            SpeechCaption("The barker", Shows.RobbedShout(e.Id + "|rob"), r.Fiddler.Handle, 5000, true, false);
                            Speech.SayAny(r.Fiddler.Handle, Speech.Thief, me.Handle);
                            BreakEventRun(r, true, "the takings robbed", now, g);
                        }
                        else
                            _engine.BreakEvent(e, true, "the takings robbed", g);
                    }
                    break;
            }
        }

        private string ShowsRunStatus()
        {
            return _showsStaged + " shows staged, " + _showHeckled + " heckled, " + _showRevealed + " revealed, " + _showRobbed + " robbed";
        }
    }
}
