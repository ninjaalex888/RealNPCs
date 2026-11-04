// Town events, staged (SIXDAY item 10; Core/TownEvents.cs, Core/Engine.Events.cs). The dance: when the player is in a town
// with a dance on, it's staged in the street outside the saloon he last came out of there (else a spot behind the camera):
// a fiddler on a bench, three women dancing (WORLD_HUMAN_DANCING, the game's one dance), a few watching, the caller's
// shouts as captions, the people near turning to look. The wedding's rails: never in missions, nothing spawned in view,
// shots or a gun pointed break it up and the town remembers who fired; an hour there counts as having been to it, and the
// regulars think the better of him for it. [World] TownEvents, EventDances. Log EVENT; F7 "TEST events:".
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
        private sealed class EventRun
        {
            public TownEvent E;
            public Vector3 At;
            public List<Ped> Cast = new List<Ped>();
            public Ped Fiddler;
            public int Since, NextCall, Looked, SpawnedAt, ReleaseAt;
            public uint FiddlerModel, WomanModel, ManModel;
            public bool Ready, FiddlerSeated;
            public string Speaker, SpeakerId;      // election day: the man on the porch (the run's first place is his)
            public int SpeakerOutfit = -1, Step;
            public string[] Lines;
            public bool Grumbled, Ticketed, Argued;
            public uint AccusedModel;             // the trial and the hanging: the accused in his own body
            public Ped Accused, Preacher;
            public readonly List<Ped> Deputies = new List<Ped>();
            public int Witnesses;
            public bool Asked;                    // the judge asked the stranger by the door
            public string Told;                   // what he told the court (guilty, innocent, none)
            public readonly Dictionary<int, int> Health = new Dictionary<int, int>();
        }

        private EventRun _eventRun;
        private readonly Dictionary<string, Vector3> _saloonDoor = new Dictionary<string, Vector3>();   // where he last came out of a saloon, by town
        private int _eventsNext, _eventsStaged, _eventsAttended, _eventsBroken;
        private Action _aTownEvents;

        /// The saloon door, remembered as he leaves one (WestworldScript.Watch.cs, where the place changes).
        private void NoteSaloonDoor(string wasKind, Vector3 at)
        {
            if (wasKind == "saloon" && _placeTown != null)
                _saloonDoor[_placeTown] = at;
            if (wasKind == "sheriff" && _placeTown != null)
                _jailDoor[_placeTown] = at;   // the hanging is staged at the jail he came out of (WestworldScript.Trials.cs)
        }

        private void TownEventsStep(int now)
        {
            if (_aTownEvents == null)
                _aTownEvents = () => TownEventsTick(_now);
            Safe("town events", _aTownEvents);
        }

        private void TownEventsTick(int now)
        {
            if (_cfg == null || !_cfg.TownEvents || _engine == null || _store == null)
                return;
            FreedTick(now);
            if (_eventRun != null)
            {
                EventStep(now);
                return;
            }
            if (now < _eventsNext)
                return;
            _eventsNext = now + 2000;
            if (_placeTown == null || MISC.GET_MISSION_FLAG() || _wedding != null)
                return;
            JailTick(now);   // the breakout's prompt at night, a hogtied man brought in (WestworldScript.Trials.cs)
            TownEvent e = _engine.EventNear(_placeTown, Bridge.Now());
            if (e == null)
                return;
            if (e.Kind == "dance" && _cfg.EventDances)
                BeginDance(e, now);
            else if (e.Kind == "election" && _cfg.EventElections)
                BeginElection(e, now);
            else if (e.Kind == "trial" && _cfg.EventTrials)
                BeginTrial(e, now);
            else if (e.Kind == "gallows" && _cfg.EventTrials)
                BeginGallows(e, now);
        }

        /// The dance staged: outside the saloon he came out of here, else behind the camera, 20-35 m off.
        private void BeginDance(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at;
            if (_saloonDoor.TryGetValue(e.Town, out at) && at.DistanceTo(me.Position) < 120f)
            {
                Vector3 spot;
                if (Natives.StandSpot(at + new Vector3(5f, 3f, 0f), at.Z, out spot))
                    at = spot;
            }
            else if (!EncounterSpot(me, 20f, 35f, out at))
                return;   // nowhere unseen just now: again in two seconds
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 6000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 1);
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 2);
            _eventRun = run;
            _eventsStaged++;
            Log.Write("EVENT dance " + e.Town + ": staging " + (int)at.DistanceTo(me.Position) + " m off (" + (_saloonDoor.ContainsKey(e.Town) ? "outside the saloon" : "in the street") + ")");
        }

        /// Election day staged: a porch speech at the town's heart, the candidate whose hour it is (the first before one, the second after)
        /// before a listening crowd; hecklers, an old man's grumble, a party man's tickets, and supporters at words.
        private void BeginElection(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 centre;
            float radius;
            if (!_bridge.TownCore(e.Town, out centre, out radius))
                return;
            if (centre.DistanceTo(me.Position) > TownEvents.NearMetres)
                return;
            Vector3 at;
            if (!EncounterSpot(me, 18f, 32f, out at))
                return;   // nowhere unseen just now
            long g = Bridge.Now();
            bool second = g >= e.At + (Elections.PollsTo - Elections.PollsFrom) * 30;   // the second man speaks the second half
            string id = second ? e.BId : e.AId, name = second ? e.BName : e.AName;
            Resident who = _store.FindResident(id);
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000, Speaker = name, SpeakerId = id };
            int seed = (e.Id ?? "").GetHashCode();
            run.FiddlerModel = who != null && who.Model != 0 ? who.Model : Spawner.ModelFor(e.Town, false, seed);   // the speaker's own build, when he has been seen
            run.SpeakerOutfit = who != null && (who.Regular || who.Outfit > 0) ? who.Outfit : -1;
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 1);
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 2);
            run.Lines = Elections.Speech(who != null ? who.Personality : null, e.Town, e.Extra ?? "mayor", e.Id);
            _eventRun = run;
            _eventsStaged++;
            Log.Write("EVENT election " + e.Town + ": staging " + name + "'s speech " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void EventStep(int now)
        {
            EventRun r = _eventRun;
            if (r.E.Kind == "election")
            {
                ElectionStep(r, now);
                return;
            }
            if (r.E.Kind == "trial")
            {
                TrialStep(r, now);
                return;
            }
            if (r.E.Kind == "gallows")
            {
                GallowsStep(r, now);
                return;
            }
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            if (!r.Ready)
            {
                if (!(Spawner.Ready(r.FiddlerModel) & Spawner.Ready(r.WomanModel) & Spawner.Ready(r.ManModel)))
                {
                    if (now - r.Since > 15000)
                    {
                        Log.Write("EVENT dance " + r.E.Town + ": the game never streamed the bodies in; resolved as if away");
                        _engine.FinishEvent(r.E, false, g);
                        _eventRun = null;
                    }
                    return;
                }
                if (!Spawner.SafeToSpawn())
                    return;
                Vector3 p0 = r.At;
                r.Fiddler = Spawner.Spawn(r.FiddlerModel, p0, 180f);
                if (r.Fiddler != null)
                    r.Cast.Add(r.Fiddler);
                float[][] spots = { new[] { -2.2f, 2.5f }, new[] { 0f, 3.2f }, new[] { 2.2f, 2.5f }, new[] { -3.5f, 0.5f }, new[] { 3.5f, 0.5f }, new[] { 0f, 5.5f } };
                for (int i = 0; i < spots.Length; i++)
                {
                    bool dancer = i < 3;
                    Ped p = Spawner.Spawn(dancer ? r.WomanModel : (i == 5 ? r.WomanModel : r.ManModel), p0 + new Vector3(spots[i][0], spots[i][1], 0f), 180f);
                    if (p == null)
                        continue;
                    r.Cast.Add(p);
                    if (dancer)
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(p.Handle, Natives.Hash("WORLD_HUMAN_DANCING"), -1, true, 0u, -1f, false);
                    else
                    {
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(p.Handle, Natives.Hash("WORLD_HUMAN_STARE_STOIC"), -1, true, 0u, -1f, false);
                        TASK.TASK_LOOK_AT_ENTITY(p.Handle, r.Cast[Math.Min(1, r.Cast.Count - 1)].Handle, 60000, 0, 51, 0);
                    }
                }
                if (r.Fiddler != null)
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(r.Fiddler.Handle, Natives.Hash("PROP_HUMAN_SEAT_BENCH_FIDDLE_UPBEAT"), -1, true, 0u, -1f, false);   // a prop scenario: it may not take without a bench (checked 2 s on)
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT dance " + r.E.Town + ": " + r.Cast.Count + " on the floor");
                return;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
                    _eventRun = null;
                }
                return;
            }
            if (r.Fiddler != null && !r.FiddlerSeated && now - r.SpawnedAt > 2500)
            {
                r.FiddlerSeated = true;
                if (!PED.IS_PED_USING_ANY_SCENARIO(r.Fiddler.Handle))
                {
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(r.Fiddler.Handle, Natives.Hash("WORLD_HUMAN_STARE_STOIC"), -1, true, 0u, -1f, false);   // the bench scenario wants a prop the street lacks: he looks on
                    Log.Write("EVENT dance: the fiddler's bench scenario needs a prop the street lacks; he looks on instead");
                }
            }
            bool byYou = false;
            string broke = now - r.SpawnedAt < 1500 ? null : EventBroken(r, out byYou);
            if (broke != null)
            {
                BreakEventRun(r, byYou, broke, now, g);
                return;
            }
            float d = me.Position.DistanceTo(r.At);
            if (d > 120f)
            {
                bool stayed = (g - Math.Max(r.E.At, g - (now - r.Since) / 1000 / 60)) >= 0 && (now - r.Since) >= TownEvents.DanceStayMinutes * 1000 / 2;   // a real minute's worth at the game's pace
                _engine.FinishEvent(r.E, stayed, g);
                if (stayed) _eventsAttended++;
                Log.Write("EVENT dance " + r.E.Town + ": you rode off" + (stayed ? " after staying a while" : " before it got going"));
                r.ReleaseAt = now + 1;
                return;
            }
            if (g >= r.E.Until)
            {
                _engine.FinishEvent(r.E, true, g);
                _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 2000 + _rng.Next(6000));
                Log.Write("EVENT dance " + r.E.Town + ": over, past eleven; you were there");
                r.ReleaseAt = now + 9000;
                return;
            }
            if (now >= r.NextCall && d < 40f)
            {
                r.NextCall = now + 12000 + _rng.Next(8000);
                Ped caller = r.Fiddler ?? (r.Cast.Count > 0 ? r.Cast[r.Cast.Count - 1] : null);
                if (caller != null && caller.Exists())
                {
                    SpeechCaption("The caller", TownEvents.Call(r.E.Id + "|" + now / 1000), caller.Handle, 3500, false, false);
                    Speech.SayAny(caller.Handle, Speech.Cheer, 0);
                }
            }
            if (now - r.Looked > 15000 && d < 40f)
            {
                r.Looked = now;
                int n = 0;
                foreach (Ped p in Peds.All())
                {
                    if (n >= 6) break;
                    int h = p.Handle;
                    if (h == me.Handle || r.Cast.Any(c => c.Handle == h) || !Natives.Bystander(p, me.Handle, IsRecord(h)) || p.Position.DistanceTo(r.At) > 15f)
                        continue;
                    TASK.TASK_LOOK_AT_ENTITY(h, r.Cast[Math.Min(1, r.Cast.Count - 1)].Handle, 8000, 0, 51, 0);
                    n++;
                }
            }
        }

        private void ElectionStep(EventRun r, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            if (!r.Ready)
            {
                if (!(Spawner.Ready(r.FiddlerModel) & Spawner.Ready(r.WomanModel) & Spawner.Ready(r.ManModel)))
                {
                    if (now - r.Since > 15000)
                    {
                        Log.Write("EVENT election " + r.E.Town + ": the game never streamed the bodies in; the polls run on without the speech");
                        _eventRun = null;
                        _eventsNext = now + 120000;
                    }
                    return;
                }
                if (!Spawner.SafeToSpawn())
                    return;
                Vector3 p0 = r.At;
                r.Fiddler = Spawner.Spawn(r.FiddlerModel, p0, 180f);   // the speaker (the run's first place)
                if (r.Fiddler == null)
                {
                    _eventRun = null;
                    _eventsNext = now + 120000;
                    return;
                }
                if (r.SpeakerOutfit >= 0)
                {
                    PED._EQUIP_META_PED_OUTFIT_PRESET(r.Fiddler.Handle, r.SpeakerOutfit, false);
                    PED._UPDATE_PED_VARIATION(r.Fiddler.Handle, false, true, true, true, false);
                }
                r.Cast.Add(r.Fiddler);
                if (r.SpeakerId != null)
                {
                    Resident who = _store.FindResident(r.SpeakerId);
                    if (who != null)
                        _resBind[r.Fiddler.Handle] = new ResBind { R = who, Model = r.FiddlerModel };   // himself: his name card, and Talk knows him
                }
                float[][] spots = { new[] { -2.5f, 3f }, new[] { 0f, 3.6f }, new[] { 2.5f, 3f }, new[] { -1.2f, 5.5f }, new[] { 1.5f, 5.8f } };
                for (int i = 0; i < spots.Length; i++)
                {
                    Ped p = Spawner.Spawn(i == 1 ? r.WomanModel : r.ManModel, p0 + new Vector3(spots[i][0], spots[i][1], 0f), 0f);
                    if (p == null)
                        continue;
                    r.Cast.Add(p);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, r.Fiddler.Handle, 120000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(p.Handle, r.Fiddler.Handle, 120000, 0, 51, 0);
                }
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT election " + r.E.Town + ": " + r.Speaker + " on the porch, " + (r.Cast.Count - 1) + " listening");
                return;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
                    _eventRun = null;
                    _eventsNext = now + 180000;   // the next speech, the other man's, three minutes on at the soonest
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
            if (d > 120f || g >= r.E.Until)
            {
                bool stayed = now - r.Since >= 90000;
                if (g >= r.E.Until)
                    _engine.FinishEvent(r.E, stayed, g);   // the polls closed with him there: the tally
                else if (stayed)
                    r.E.Attended = true;
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                Log.Write("EVENT election " + r.E.Town + ": " + (g >= r.E.Until ? "the polls closed" : "you rode off") + (stayed ? ", you stayed a while" : ""));
                r.ReleaseAt = now + 8000;
                return;
            }
            if (now < r.NextCall || d > 45f)
                return;
            r.NextCall = now + 7000 + _rng.Next(3000);
            if (r.Step < r.Lines.Length)
            {
                SpeechCaption(Person.FirstOf(r.Speaker), r.Lines[r.Step], r.Fiddler.Handle, 6000, false, false);
                if (r.Step % 3 == 2 && r.Cast.Count > 2)
                {
                    Ped h = r.Cast[1 + _rng.Next(r.Cast.Count - 1)];
                    SpeechCaption("A voice in the crowd", Elections.Heckle(r.E.Id + "|" + r.Step), h.Handle, 2500, false, false);
                    Speech.SayAny(h.Handle, _rng.Next(2) == 0 ? Speech.Cheer : Speech.Mock, r.Fiddler.Handle);
                }
                r.Step++;
                return;
            }
            if (!r.Grumbled && r.Cast.Count > 1)
            {
                r.Grumbled = true;
                SpeechCaption("An old man", Elections.Grumble(r.E.Id), r.Cast[r.Cast.Count - 1].Handle, 5000, false, false);
                return;
            }
            if (!r.Ticketed && d < 12f && r.Cast.Count > 2)
            {
                r.Ticketed = true;
                Ped party = r.Cast[2];
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(party.Handle, me.Handle, 3000, 0f, 0f, 0f);
                SpeechCaption("A party man", Elections.Ticket(r.E.Id), party.Handle, 4500, true, false);
                return;
            }
            if (!r.Argued)
            {
                r.Argued = true;
                string said;
                string why = FireScene("argue", now, r.E.Id + "|argue", out said);   // supporters at words, by the test's own path: the nearest free named pair
                Log.Detail("EVENT election: supporters' argue " + (why == null ? "started" : "not started (" + why + ")"));
                return;
            }
            // the speech done: the crowd thins
            foreach (Ped p in r.Cast)
                if (p != r.Fiddler)
                    WanderLater(p.Handle, now + _rng.Next(4000));
            WanderLater(r.Fiddler.Handle, now + 6000);
            if (now - r.Since >= 90000)
            {
                r.E.Attended = true;
                _eventsAttended++;
            }
            Log.Write("EVENT election " + r.E.Town + ": " + r.Speaker + "'s speech done");
            r.ReleaseAt = now + 8000;
        }

        private string EventBroken(EventRun r, out bool byYou)
        {
            byYou = false;
            Ped me = Game.Player.Character;
            foreach (Ped p in r.Cast)
            {
                if (!p.Exists())
                    return "someone at the dance vanished";
                int h = p.Handle;
                if (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h))
                {
                    byYou = true;
                    return "a gun pointed at the dancers";
                }
                int start;
                if (!r.Health.TryGetValue(h, out start))
                    start = ENTITY.GET_ENTITY_HEALTH(h);
                if (p.IsDead || PED.IS_PED_FLEEING(h) || PED.IS_PED_IN_COMBAT(h, 0) || ENTITY.GET_ENTITY_HEALTH(h) < start - 10)
                {
                    byYou = ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(h, me.Handle, true, true) || PED.IS_PED_IN_COMBAT(h, me.Handle) || (p.IsDead && PED.GET_PED_SOURCE_OF_DEATH(h) == me.Handle);
                    return p.IsDead ? "someone at the dance is dead" : "violence at the dance";
                }
            }
            if (PED.IS_PED_SHOOTING(me.Handle) && me.Position.DistanceTo(r.At) < 70f)
            {
                byYou = true;
                return "shots fired by the dance";
            }
            if (MISC.GET_MISSION_FLAG())
                return "a mission began";
            return null;
        }

        private void BreakEventRun(EventRun r, bool byYou, string why, int now, long g)
        {
            Ped me = Game.Player.Character;
            bool screamed = false;
            // a hanging broken by someone else's shots: the sworn men hustle the condemned man inside and finish it there
            bool inside = r.E.Kind == "gallows" && !byYou;
            foreach (Ped p in r.Cast)
            {
                if (!p.Exists() || p.IsDead)
                    continue;
                if (inside && (p == r.Accused || r.Deputies.Contains(p)))
                {
                    Vector3 back = r.At + new Vector3(0f, -12f, 0f);
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, back.X, back.Y, back.Z, 2f, -1, 1f, 0, 0f);
                    continue;
                }
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                if (why == "a mission began")
                    continue;
                if (Bravery.Holds(_cfg.Bravery, "dance|" + p.Handle))
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, me.Handle, 4000, 0f, 0f, 0f);
                    WanderLater(p.Handle, Game.GameTime + 5000);
                    continue;
                }
                BraveRelease(p.Handle);
                TASK.TASK_SMART_FLEE_PED(p.Handle, me.Handle, 120f, -1, 0, 3f, 0);
                if (!screamed)
                    screamed = Speech.SayAny(p.Handle, Speech.Scared, me.Handle);
            }
            _engine.BreakEvent(r.E, byYou, why, g);
            _eventsBroken++;
            r.ReleaseAt = now + 8000;
        }

        /// The add-on stopping, or a wipe: the floor cleared, the peds given back.
        private void EventsAbort(string why)
        {
            EventRun r = _eventRun;
            _eventRun = null;
            _eventMenu = null;
            PromptClear();
            if (r == null)
                return;
            foreach (Ped p in r.Cast)
                Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
            Log.Write("EVENT " + r.E.Kind + " " + r.E.Town + ": staging ended (" + why + ")");
        }

        private string TownEventsStatus()
        {
            return _engine.EventsStatus(Bridge.Now()) + "; this session: " + _eventsStaged + " staged, " + _eventsAttended + " attended, " + _eventsBroken + " broken" +
                   (_eventRun != null ? "; on now: " + _eventRun.E.Kind + " in " + _eventRun.E.Town + " (" + _eventRun.Cast.Count + " on the floor)" : "") +
                   (_saloonDoor.Count > 0 ? "; saloon doors known in " + string.Join(", ", _saloonDoor.Keys) : "") + "; " + TrialsRunStatus();
        }
    }
}
