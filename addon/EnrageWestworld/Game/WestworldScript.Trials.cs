// The circuit judge and gallows day, staged (SIXDAY item 10c; Core/Trials.cs, Core/Engine.Trials.cs). The trial sits outside
// the saloon he last came out of in the town (else in the street behind him): the judge, the accused in his own body between
// two deputies with carbines, a few of the town looking on; the charge, the plea by temper, a witness, the judge's question
// to the stranger by the door (J: testify, true or false), the verdict read in three lines, the crowd's word. Gallows day at
// the jail door he last came out of (else the street): the sheriff, two sworn men at the stockade line, the preacher, the
// condemned man standing inside, a crowd outside; the sentence read, his last words, a hymn, a silence, and the drop never
// shown (he walks out of sight and the captions carry it). Before the hour, J swears the player in as a special deputy; at
// night inside the jail with no lawman near, J breaks the prisoner out; a hogtied man of ours carried into a sheriff's office
// is brought in. The wedding's rails: never in missions, nothing spawned in view, shots break it up. Keyboard only for J.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, Vector3> _jailDoor = new Dictionary<string, Vector3>();   // where he last came out of a sheriff's office, by town
        private LetterMenu _eventMenu;                 // the trial's and the gallows' small menus (the letters' class and box)
        private string _eventPrompt;                   // testify, swear, breakout: J opens the menu while it shows
        private TownEvent _eventPromptFor;
        private int _eventPromptUntil;
        private string _eventPromptText;            // the prompt's line, made once when it's set (no string work each frame)
        private readonly HashSet<string> _broughtIn = new HashSet<string>();   // resident id | game day: once a day each
        private Ped _freedPed;
        private int _freedRelease;
        private int _trialsStaged, _gallowsStaged, _testified, _sworn, _breakouts, _broughtInCount;
        private static readonly uint Carbine = Natives.Hash("WEAPON_REPEATER_CARBINE");
        private KeySpec _eventKey;                     // [Keys] EventKey, J as it comes (the game's own J is the journal: change it in the ini if that bites)
        private const uint DeputyGiveReason = 0x2CD419DC;   // the game's default reason for giving a weapon (WakeActs.cs)
        private const int PlayerPad = 0;

        private void PromptSet(string kind, TownEvent e, int ms)
        {
            _eventPrompt = kind;
            _eventPromptFor = e;
            _eventPromptUntil = Game.GameTime + ms;
            _eventPromptText = KeySpec.Name(_eventKey.Key) + (kind == Wayside.Kind ? ": what you do" : kind == "testify" ? ": speak to the court" : kind == "swear" ? ": be sworn in" : kind == "show" ? ": the show" : kind == PrizeFight.Kind ? ": the fight" : kind == Slate.Kind ? ": pay their slate" : kind == BarRound.Kind ? ": buy the house a round" : kind == WalkHome.Kind ? ": offer to see them home" : kind == Foreclosure.Kind ? ": buy it back for them" : kind == BoxSocial.Kind ? ": bid for the box" : kind == BrawlVerbs.Kind ? ": the fight" : kind == PurseSnatch.Kind ? ": the thief you caught" : kind == BrawlAfter.JailKind ? ": his fine" : kind == BrawlVerbs.PayKind ? ": pay the bet you lost" : kind == NeighbourReward.Kind ? ": the reward" : kind == WantedWhich.Kind ? ": the poster" : ": the cell");
        }

        private void PromptClear()
        {
            _eventPrompt = null;
            _eventPromptFor = null;
        }

        // ---- the trial ----

        private void BeginTrial(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at;
            bool saloon = false;
            if (_saloonDoor.TryGetValue(e.Town, out at) && at.DistanceTo(me.Position) < 120f && !Natives.Looking(at + new Vector3(0f, 0f, 1f), 50f))
            {
                Vector3 spot;
                if (Natives.StandSpot(at + new Vector3(6f, 4f, 0f), at.Z, out spot))
                    at = spot;
                saloon = true;
            }
            else if (!EncounterSpot(me, 18f, 30f, out at))
                return;   // nowhere unseen just now (the door in his sight counts as nowhere): again in two seconds
            Resident who = _store.FindResident(e.AId);
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000, Speaker = e.AName, SpeakerId = e.AId };
            int seed = (e.Id ?? "").GetHashCode();
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 3);   // the judge (the run's first place)
            run.AccusedModel = who != null && who.Model != 0 ? who.Model : Spawner.ModelFor(e.Town, false, seed + 4);
            run.SpeakerOutfit = who != null && (who.Regular || who.Outfit > 0) ? who.Outfit : -1;
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 1);
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 2);
            run.Witnesses = _engine.WitnessesOf(e.BId).Count;
            _eventRun = run;
            _eventsStaged++;
            _trialsStaged++;
            Log.Write("EVENT trial " + e.Town + ": staging " + e.AName + "'s trial " + (int)at.DistanceTo(me.Position) + " m off (" + (saloon ? "at the saloon" : "in the street") + "; " + run.Witnesses + " saw the killing)");
        }

        private bool EventCastReady(EventRun r)
        {
            return Spawner.Ready(r.FiddlerModel) & Spawner.Ready(r.WomanModel) & Spawner.Ready(r.ManModel) & (r.AccusedModel == 0 || Spawner.Ready(r.AccusedModel));
        }

        /// The accused, in his own body, his name card on him and Talk knowing him.
        private Ped SpawnAccused(EventRun r, Vector3 at, float heading)
        {
            UnbindOthers(r.SpeakerId);   // one body with his name (the review)
            Ped p = Spawner.Spawn(r.AccusedModel, at, heading);
            if (p == null)
                return null;
            if (r.SpeakerOutfit >= 0)
            {
                PED._EQUIP_META_PED_OUTFIT_PRESET(p.Handle, r.SpeakerOutfit, false);
                PED._UPDATE_PED_VARIATION(p.Handle, false, true, true, true, false);
                Dressed.Note(p.Handle);   // not read for a while (Game/Dressed.cs)
            }
            r.Cast.Add(p);
            r.Accused = p;
            Resident who = r.SpeakerId != null ? _store.FindResident(r.SpeakerId) : null;
            if (who != null)
                _resBind[p.Handle] = new ResBind { R = who, Model = r.AccusedModel };
            return p;
        }

        /// The resident's other body, if one is walking the town, goes nameless: one body carries the name (the review).
        private void UnbindOthers(string id)
        {
            if (id == null)
                return;
            List<int> gone = null;
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
                if (kv.Value.R != null && kv.Value.R.Id == id)
                    (gone ?? (gone = new List<int>())).Add(kv.Key);
            if (gone != null)
                foreach (int h in gone)
                    _resBind.Remove(h);
        }

        private Ped SpawnDeputy(EventRun r, Vector3 at, float heading)
        {
            Ped p = Spawner.Spawn(r.ManModel, at, heading);
            if (p == null)
                return null;
            r.Cast.Add(p);
            r.Deputies.Add(p);
            WEAPON.GIVE_WEAPON_TO_PED(p.Handle, Carbine, 20, true, false, 0, false, 0.5f, 1f, DeputyGiveReason, true, 0f, false);
            return p;
        }

        private void EventRunLost(EventRun r, int now, string why)
        {
            Log.Write("EVENT " + r.E.Kind + " " + r.E.Town + ": " + why + "; resolved as if away");
            _engine.FinishEvent(r.E, false, Bridge.Now());
            _eventRun = null;
            _eventsNext = now + 60000;
            PromptClear();
        }

        private void TrialStep(EventRun r, int now)
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
                Vector3 p0 = r.At;
                r.Fiddler = Spawner.Spawn(r.FiddlerModel, p0, 180f);   // the judge, facing the accused
                if (r.Fiddler == null)
                {
                    EventRunLost(r, now, "the judge never appeared");
                    return;
                }
                r.Cast.Add(r.Fiddler);
                Ped accused = SpawnAccused(r, p0 + new Vector3(0f, -3f, 0f), 0f);
                SpawnDeputy(r, p0 + new Vector3(-1.7f, -3.3f, 0f), 0f);
                SpawnDeputy(r, p0 + new Vector3(1.7f, -3.3f, 0f), 0f);
                float[][] spots = { new[] { -3f, -6.5f }, new[] { -1f, -7f }, new[] { 1.3f, -7.2f }, new[] { 3f, -6.5f } };
                for (int i = 0; i < spots.Length; i++)
                {
                    Ped p = Spawner.Spawn(i == 1 ? r.WomanModel : r.ManModel, p0 + new Vector3(spots[i][0], spots[i][1], 0f), 0f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != r.Fiddler)
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, r.Fiddler.Handle, 180000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(p.Handle, r.Fiddler.Handle, 180000, 0, 51, 0);
                    }
                }
                if (accused != null)
                    TASK.TASK_LOOK_AT_ENTITY(r.Fiddler.Handle, accused.Handle, 180000, 0, 51, 0);
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT trial " + r.E.Town + ": the court sits; " + r.E.AName + (accused != null ? " in the dock" : " not brought in") + ", " + r.Deputies.Count + " deputies, " + (r.Cast.Count - 1 - r.Deputies.Count - (accused != null ? 1 : 0)) + " looking on");
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
            if (_eventPrompt == "testify" && now > _eventPromptUntil)
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
            if (d > 120f || (g >= r.E.Until && r.Step < 6))
            {
                PromptClear();
                bool stayed = now - r.Since >= 90000;
                _engine.FinishEvent(r.E, stayed, g);   // the verdict, with his word if he gave it
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                Log.Write("EVENT trial " + r.E.Town + ": " + (d > 120f ? "you rode off" : "the court rose") + " before the verdict was read" + (stayed ? ", you stayed a while" : "") + " (" + r.E.Outcome + ")");
                r.ReleaseAt = now + 8000;
                return;
            }
            if (now < r.NextCall || d > 45f)
                return;
            r.NextCall = now + 8000 + _rng.Next(3000);
            string first = Person.FirstOf(r.E.AName);
            switch (r.Step)
            {
                case 0:
                    SpeechCaption("The judge", Trials.JudgeOpens(r.E)[0], r.Fiddler.Handle, 5000, false, false);
                    break;
                case 1:
                    SpeechCaption("The judge", Trials.JudgeOpens(r.E)[1], r.Fiddler.Handle, 6500, false, false);
                    break;
                case 2:
                    {
                        Resident who = _store.FindResident(r.E.AId);
                        SpeechCaption(first, Trials.Plea(who != null ? who.Personality : null, Person.FirstOf(r.E.BName), r.E.Id + "|plea"), (r.Accused ?? r.Fiddler).Handle, 6500, false, false);
                        if (r.Accused != null)
                            Speech.SayAny(r.Accused.Handle, who != null && who.Personality == "Hothead" ? Speech.Angry : Speech.Plead, r.Fiddler.Handle);
                    }
                    break;
                case 3:
                    if (r.Witnesses > 0 && r.Cast.Count > 4)
                        SpeechCaption("A witness", Trials.WitnessLine(Person.FirstOf(r.E.BName), r.E.Id + "|w"), r.Cast[r.Cast.Count - 2].Handle, 6000, false, false);
                    break;
                case 4:
                    if (d < 15f && r.E.Part == null)
                    {
                        SpeechCaption("The judge", Trials.JudgeAsksYou(r.E.Id + "|ask"), r.Fiddler.Handle, 6500, true, false);
                        TASK.TASK_LOOK_AT_ENTITY(r.Fiddler.Handle, me.Handle, 30000, 0, 51, 0);
                        PromptSet("testify", r.E, 40000);
                        r.NextCall = now + 40000;   // the court waits on him
                        r.Asked = true;
                    }
                    break;
                case 5:
                    if (r.Told != null)
                        SpeechCaption("The judge", Trials.JudgeOnTestimony(r.Told, r.E.Id + "|on"), r.Fiddler.Handle, 5000, true, false);
                    else if (r.Asked)
                        SpeechCaption("The judge", "The stranger has nothing for us. Very well.", r.Fiddler.Handle, 4000, false, false);
                    PromptClear();
                    break;
                case 6:
                    _engine.FinishEvent(r.E, true, g);   // the verdict weighed now, with his word if he gave it
                    _eventsAttended++;
                    r.Lines = Trials.JudgeRules(r.E, r.E.Outcome == "guilty" ? "guilty" : "not guilty");
                    SpeechCaption("The judge", r.Lines[0], r.Fiddler.Handle, 5000, false, false);
                    if (r.Accused != null)
                        TASK.TASK_LOOK_AT_ENTITY(r.Fiddler.Handle, r.Accused.Handle, 60000, 0, 51, 0);
                    break;
                case 7:
                    SpeechCaption("The judge", r.Lines[1], r.Fiddler.Handle, 6500, false, false);
                    break;
                case 8:
                    SpeechCaption("The judge", r.Lines[2], r.Fiddler.Handle, 8000, false, false);
                    break;
                default:
                    {
                        bool guilty = r.E.Outcome == "guilty";
                        Ped voice = r.Cast.Count > 4 ? r.Cast[r.Cast.Count - 1] : r.Fiddler;
                        SpeechCaption("A voice in the room", Trials.CrowdAtVerdict(guilty ? "guilty" : "not guilty", first, r.E.Id + "|crowd"), voice.Handle, 4500, false, false);
                        Speech.SayAny(voice.Handle, guilty ? Speech.Shame : Speech.Cheer, r.Accused != null ? r.Accused.Handle : 0);
                        if (r.Accused != null)
                            Speech.SayAny(r.Accused.Handle, guilty ? Speech.Plead : Speech.Relieved, 0);
                        // the court rises: the deputies take a guilty man back toward the jail, a free man walks out on his own
                        Vector3 jail = default(Vector3);
                        bool toJail = guilty && _jailDoor.TryGetValue(r.E.Town, out jail) && jail.DistanceTo(r.At) < 200f;
                        foreach (Ped p in r.Cast)
                        {
                            if (p == r.Fiddler) { WanderLater(p.Handle, now + 10000); continue; }
                            if (toJail && (p == r.Accused || r.Deputies.Contains(p)))
                            {
                                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, jail.X, jail.Y, jail.Z, 1f, -1, 1f, 0, 0f);
                                continue;
                            }
                            WanderLater(p.Handle, now + 2000 + _rng.Next(6000));
                        }
                        Log.Write("EVENT trial " + r.E.Town + ": the verdict read, " + (guilty ? "guilty; " + first + (toJail ? " taken back to the jail" : " led off") : "not guilty; " + first + " walks out") + "; you were there");
                        r.ReleaseAt = now + 14000;
                    }
                    break;
            }
            r.Step++;
        }

        // ---- gallows day ----

        private void BeginGallows(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at;
            bool jail = false;
            if (_jailDoor.TryGetValue(e.Town, out at) && at.DistanceTo(me.Position) < 150f && !Natives.Looking(at + new Vector3(0f, 0f, 1f), 50f))
            {
                Vector3 spot;
                if (Natives.StandSpot(at + new Vector3(0f, 5f, 0f), at.Z, out spot))
                    at = spot;
                if (at.DistanceTo(me.Position) < 8f)
                    return;   // he's standing on the ground: wait for him to step off it
                jail = true;
            }
            else if (!EncounterSpot(me, 25f, 40f, out at))
                return;
            Resident who = _store.FindResident(e.AId);
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 6000, Speaker = e.AName, SpeakerId = e.AId };
            int seed = (e.Id ?? "").GetHashCode();
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 5);   // the sheriff (the run's first place)
            run.AccusedModel = who != null && who.Model != 0 ? who.Model : Spawner.ModelFor(e.Town, false, seed + 4);
            run.SpeakerOutfit = who != null && (who.Regular || who.Outfit > 0) ? who.Outfit : -1;
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 1);
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 2);
            _eventRun = run;
            _eventsStaged++;
            _gallowsStaged++;
            Log.Write("EVENT gallows " + e.Town + ": staging " + e.AName + "'s hanging " + (int)at.DistanceTo(me.Position) + " m off (" + (jail ? "at the jail" : "in the street") + (e.Part == "deputy" ? "; you are sworn in" : "") + ")");
        }

        private void GallowsStep(EventRun r, int now)
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
                Vector3 p0 = r.At;   // the stockade line: the crowd's side is +y, the inside -y
                r.Fiddler = Spawner.Spawn(r.FiddlerModel, p0 + new Vector3(0f, -1f, 0f), 0f);   // the sheriff
                if (r.Fiddler == null)
                {
                    EventRunLost(r, now, "the sheriff never appeared");
                    return;
                }
                r.Cast.Add(r.Fiddler);
                SpawnDeputy(r, p0 + new Vector3(-1.8f, -0.4f, 0f), 0f);
                SpawnDeputy(r, p0 + new Vector3(1.8f, -0.4f, 0f), 0f);
                Ped condemned = SpawnAccused(r, p0 + new Vector3(0f, -3.5f, 0f), 0f);
                Ped preacher = Spawner.Spawn(r.ManModel, p0 + new Vector3(2.6f, -2.4f, 0f), 0f);
                if (preacher != null)
                {
                    r.Cast.Add(preacher);
                    r.Preacher = preacher;
                }
                float[][] spots = { new[] { -4f, 6.5f }, new[] { -2f, 7.5f }, new[] { 0f, 8f }, new[] { 2f, 7.5f }, new[] { 4f, 6.5f }, new[] { -1f, 9.5f } };
                for (int i = 0; i < spots.Length; i++)
                {
                    Ped p = Spawner.Spawn(i == 1 || i == 4 ? r.WomanModel : r.ManModel, p0 + new Vector3(spots[i][0], spots[i][1], 0f), 180f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p == r.Fiddler || r.Deputies.Contains(p))
                        continue;
                    Ped look = p == condemned || p == preacher ? r.Fiddler : (condemned ?? r.Fiddler);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, look.Handle, 240000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(p.Handle, look.Handle, 240000, 0, 51, 0);
                }
                if (condemned != null)
                    TASK.TASK_LOOK_AT_ENTITY(r.Fiddler.Handle, condemned.Handle, 240000, 0, 51, 0);
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT gallows " + r.E.Town + ": the ground kept; " + r.E.AName + (condemned != null ? " inside" : " not brought out") + ", " + r.Deputies.Count + " sworn men, " + (r.Cast.Count - 2 - r.Deputies.Count - (condemned != null ? 1 : 0)) + " outside");
                return;
            }
            // the condemned man, gone inside out of sight: handed back the moment nobody can see him (never in view: a dead man
            // can't walk back into the street, so the run waits for him to be out of sight, 40 s at most)
            if (r.Accused != null && r.Step >= 8 && r.Accused.Exists() && !ENTITY.IS_ENTITY_ON_SCREEN(r.Accused.Handle))
            {
                r.Cast.Remove(r.Accused);
                Spawner.Release(r.Accused, true);
                r.Accused = null;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    if (r.Accused != null && r.Step >= 8 && r.Accused.Exists())
                    {
                        if (now < r.ReleaseAt + 40000)
                            return;
                        Log.Write("EVENT gallows " + r.E.Town + ": the condemned man never got out of sight (a wall in the way?); taken away in view");
                        r.Cast.Remove(r.Accused);
                        Spawner.Release(r.Accused, true);
                        r.Accused = null;
                    }
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
                    _eventRun = null;
                    _eventsNext = now + 60000;
                }
                return;
            }
            if (_eventPrompt == "swear" && (now > _eventPromptUntil || r.E.Part == "deputy" || r.Step > 0))
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
                _engine.FinishEvent(r.E, stayed, g);   // the sentence carried out without him watching
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                Log.Write("EVENT gallows " + r.E.Town + ": " + (d > 120f ? "you rode off" : "the hour passed") + " before the end" + (stayed ? ", you stayed a while" : ""));
                r.ReleaseAt = now + 8000;
                return;
            }
            bool sworn = r.E.Part == "deputy";
            if (r.Step == 0 && !sworn && d < 12f && _eventPrompt == null && now - r.SpawnedAt < 90000)
                PromptSet("swear", r.E, 20000);   // the sheriff's men take oaths till the sentence is read
            if (now < r.NextCall || d > 50f)
                return;
            r.NextCall = now + 9000 + _rng.Next(3000);
            string first = Person.FirstOf(r.E.AName);
            Ped crowd = r.Cast.Count > 0 ? r.Cast[r.Cast.Count - 1] : r.Fiddler;
            switch (r.Step)
            {
                case 0:
                    if (d < 10f && r.Deputies.Count > 0)
                    {
                        Ped dep = r.Deputies[0];
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(dep.Handle, me.Handle, 4000, 0f, 0f, 0f);
                        SpeechCaption("A sworn man", Trials.DeputyToYou(sworn, r.E.Id + "|dep"), dep.Handle, 5000, true, false);
                    }
                    else
                        r.NextCall = now + 3000;
                    break;
                case 1:
                    SpeechCaption("The sheriff", Trials.SheriffReads(r.E)[0], r.Fiddler.Handle, 6000, false, false);
                    break;
                case 2:
                    SpeechCaption("The sheriff", Trials.SheriffReads(r.E)[1], r.Fiddler.Handle, 4000, false, false);
                    break;
                case 3:
                    {
                        Resident who = _store.FindResident(r.E.AId);
                        if (r.Accused != null && sworn)
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Accused.Handle, me.Handle, 8000, 0f, 0f, 0f);
                        SpeechCaption(r.Accused != null ? first : "From inside the stockade", Trials.LastWords(who != null ? who.Personality : null, sworn, r.E.Id + "|last"), (r.Accused ?? r.Fiddler).Handle, 6500, sworn, false);
                        if (r.Accused != null)
                            Speech.SayAny(r.Accused.Handle, who != null && who.Personality == "Coward" ? Speech.Plead : Speech.Mild, 0);
                    }
                    break;
                case 4:
                    SpeechCaption("The preacher", Trials.PreacherLine(r.E.Id + "|pr"), (r.Preacher ?? r.Fiddler).Handle, 5500, false, false);
                    break;
                case 5:
                    SpeechCaption("The crowd", Trials.Line(Trials.Hymn, r.E.Id + "|hymn"), crowd.Handle, 7000, false, false);
                    r.NextCall = now + 14000;
                    break;
                case 6:
                    SpeechCaption("The crowd", Trials.Line(Trials.Silence, r.E.Id + "|sil"), crowd.Handle, 6000, false, false);
                    break;
                case 7:
                    // he walks inside, out of everyone's sight; the trap is never shown
                    if (r.Accused != null && r.Accused.Exists())
                    {
                        Vector3 inside = r.At + new Vector3(0f, -12f, 0f);
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(r.Accused.Handle, inside.X, inside.Y, inside.Z, 1f, -1, 1f, 0, 0f);
                        foreach (Ped p in r.Cast)
                            if (p != r.Accused && !r.Deputies.Contains(p))
                                TASK.TASK_LOOK_AT_ENTITY(p.Handle, r.Accused.Handle, 20000, 0, 51, 0);
                    }
                    r.NextCall = now + 12000;
                    break;
                case 8:
                    SpeechCaption("Inside the stockade", Trials.Line(Trials.Drop, r.E.Id + "|drop"), r.Fiddler.Handle, 7000, false, false);
                    _engine.FinishEvent(r.E, true, g);   // the sentence carried out, the town remembering
                    _eventsAttended++;
                    if (r.Accused != null && r.Accused.Exists() && ENTITY.IS_ENTITY_ON_SCREEN(r.Accused.Handle))
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(r.Accused.Handle, r.At.X, r.At.Y - 30f, r.At.Z, 1.5f, -1, 1f, 0, 0f);   // still in sight: further, until he isn't
                    break;
                default:
                    SpeechCaption("The crowd", Trials.Line(Trials.After, r.E.Id + "|after"), crowd.Handle, 6000, false, false);
                    foreach (Ped p in r.Cast)
                        if (p != r.Accused)
                            WanderLater(p.Handle, now + (p == r.Fiddler || r.Deputies.Contains(p) ? 15000 : 2000 + _rng.Next(8000)));
                    Log.Write("EVENT gallows " + r.E.Town + ": over; you were there" + (sworn ? ", sworn in" : ""));
                    r.ReleaseAt = now + 20000;
                    break;
            }
            r.Step++;
        }

        // ---- the jail: a breakout at night, a man brought in hogtied ----

        private void JailTick(int now)
        {
            if (_placeKind != "sheriff" || _placeTown == null)
                return;
            Ped me = Game.Player.Character;
            long g = Bridge.Now();
            if (_cfg.EventTrials)
            {
                TownEvent held = _engine.HeldAt(_placeTown);
                int clockHour = CLOCK.GET_CLOCK_HOURS();
                if (held != null && Trials.QuietHour(clockHour) && BrawlFindLaw(me.Position, 15f, false, 0) == null)
                    PromptSet("breakout", held, 3500);
            }
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || p.IsDead || p.Position.DistanceTo(me.Position) > 8f || !PED.IS_PED_HOGTIED(h))
                    continue;
                string id = null, name = null;
                ResBind rb;
                Known k;
                if (_resBind.TryGetValue(h, out rb) && rb.R != null) { id = rb.R.Id; name = rb.R.Name; }
                else if (_known.TryGetValue(h, out k) && Resident.IsResident(k.Id)) { id = k.Id; name = k.Name; }
                if (id == null || !_broughtIn.Add(id + "|" + g / 1440))
                    continue;
                string what = _engine.BroughtIn(id, _placeTown, g);
                if (what == null)
                    continue;
                _broughtInCount++;
                Hint("The deputy takes " + (name ?? "him") + " off your hands: " + what + ".", 6000);
            }
        }

        private void BreakoutDo(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            long g = Bridge.Now();
            _engine.PrisonerFreed(e, false, g);
            _breakouts++;
            Resident who = _store.FindResident(e.AId);
            Vector3 at;
            if (who != null && who.Model != 0 && Spawner.Ready(who.Model) && EncounterSpot(me, 4f, 12f, out at) && Spawner.SafeToSpawn())
            {
                Ped p = Spawner.Spawn(who.Model, at, 0f);
                if (p != null)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    TASK.TASK_SMART_FLEE_PED(p.Handle, me.Handle, 300f, -1, 0, 3f, 0);
                    _freedPed = p;
                    _freedRelease = now + 25000;
                }
            }
            Hint("You get " + Person.FirstOf(e.AName) + " out the back way. He's gone before anyone stirs.", 5000);
        }

        private void FreedTick(int now)
        {
            if (_freedPed == null || now < _freedRelease)
                return;
            Spawner.Release(_freedPed, _freedPed.Exists() && !ENTITY.IS_ENTITY_ON_SCREEN(_freedPed.Handle));
            _freedPed = null;
        }

        // ---- J, and the small menu ----

        /// A key for the trial's and the gallows' menus (KeyDown04): the menu's while one is up, J to open one while a prompt shows.
        private bool EventKey(Keys k)
        {
            LetterMenu m = _eventMenu;
            if (m != null)
            {
                switch (k)
                {
                    case Keys.Up: m.Sel = (m.Sel + m.Rows.Count - 1) % m.Rows.Count; break;
                    case Keys.Down: m.Sel = (m.Sel + 1) % m.Rows.Count; break;
                    case Keys.Enter: case Keys.Space: EventMenuChoose(m); break;
                    case Keys.Escape: case Keys.Back: _eventMenu = null; break;
                }
                return true;
            }
            if (k == Keys.None || k != _eventKey.Key || _eventPrompt == null || Game.GameTime > _eventPromptUntil || _eventPromptFor == null)
                return false;
            var menu = new LetterMenu { Mode = _eventPrompt };
            switch (_eventPrompt)
            {
                case Wayside.Kind: menu.Rows = Wayside.Rows(_wayside != null ? _wayside.K : Wayside.Of(_eventPromptFor.Extra), MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "On the road:"; break;   // (FIVEDAY2 items 16-20)
                case "testify": menu.Rows = Trials.TestifyRows(); menu.Title = "To the court:"; break;
                case "swear": menu.Rows = Trials.SwearRows(); menu.Title = "The sheriff's man:"; break;
                case "show": menu.Rows = Shows.Rows(_eventPromptFor); menu.Title = "At the show:"; break;
                case PrizeFight.Kind: menu.Rows = PrizeFight.Rows(_eventPromptFor); menu.Title = "Out back, the fight:"; break;
                case BoxSocial.Kind: menu.Rows = BoxSocial.Rows(_boxHigh, MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "The box social:"; break;   // (FIVEDAY R6)
                case Foreclosure.Kind: menu.Rows = Foreclosure.Rows(_eventPromptFor.Extra, _eventPromptFor.Count, MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "The sheriff's sale:"; break;   // (FIVEDAY R3)
                case WalkHome.Kind: menu.Rows = WalkHome.Rows(_eventPromptFor.AName); menu.Title = Person.FirstOf(_eventPromptFor.AName) + ", after dark:"; break;   // (THREEHOUR item 4)
                case BarRound.Kind: int barAt; int.TryParse(_eventPromptFor.Extra, out barAt); menu.Rows = BarRound.Rows(_eventPromptFor.Count, MONEY._MONEY_GET_CASH_BALANCE(), barAt, SaloonDamageToday(), _barRoundDays.Contains(BarRound.DayKey(_placeTown, _store.LastGameMin))); menu.Title = "At the bar:"; break;   // (THREEHOUR item 3)
                case Slate.Kind: menu.Rows = Slate.Rows(_eventPromptFor.Count, MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "At the counter:"; break;   // (FIVEDAY R2)
                case WantedWhich.Kind: menu.Rows = WantedWhich.Rows(_eventPromptFor.AName, _eventPromptFor.AId, MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "You know " + Person.FirstOf(_eventPromptFor.AName) + "'s face:"; break;   // (FIVEDAY R31)
                case BrawlVerbs.Kind: menu.Rows = BrawlVerbsRows(); menu.Title = "The fight here:"; break;   // (FOURTEENHOUR item 11)
                case BrawlAfter.JailKind: menu.Rows = BrawlAfter.JailRows(Person.FirstOf(_eventPromptFor.AName), MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "At the jail door:"; break;   // (FOURTEENHOUR item 12)
                case BrawlVerbs.PayKind: menu.Rows = BrawlVerbs.PayRows(Person.FirstOf(_eventPromptFor.AName), _eventPromptFor.Count, MONEY._MONEY_GET_CASH_BALANCE()); menu.Title = "The bet you lost:"; break;
                case PurseSnatch.Kind: menu.Rows = PurseSnatch.Rows(Person.FirstOf(_eventPromptFor.AName), _theftAsk != null && NearLawFor(_theftAsk.Thief, PurseSnatch.LawNearMetres) != null); menu.Title = "The thief:"; break;   // (FOURTEENHOUR item 20)
                case NeighbourReward.Kind: menu.Rows = NeighbourReward.Rows(_eventPromptFor.AName, _eventPromptFor.Count); menu.Title = "The reward on " + Person.FirstOf(_eventPromptFor.AName) + ":"; break;   // (FIVEDAY R30)
                default: menu.Rows = Trials.BreakoutRows(_eventPromptFor.AName); menu.Title = Person.FirstOf(_eventPromptFor.AName) + " is in the cell:"; break;
            }
            _eventMenu = menu;
            return true;
        }

        private void EventMenuChoose(LetterMenu m)
        {
            if (m.Rows == null || m.Sel < 0 || m.Sel >= m.Rows.Count)
                return;
            InteractOption row = m.Rows[m.Sel];
            if (row.Why != null)
            {
                Hint(row.Label + ": " + row.Why + ".", 2500);   // a greyed row does nothing (the review)
                return;
            }
            TownEvent e = _eventPromptFor;
            EventRun r = _eventRun;
            int now = Game.GameTime;
            _eventMenu = null;
            PromptClear();
            if (e == null)
                return;
            if (m.Mode == Wayside.Kind)
            {
                WaysideChoose(e, row.Id);   // WestworldScript.Wayside.cs
                return;
            }
            if (m.Mode == BrawlAfter.JailKind)
            {
                JailDoorChoose(row.Id);   // WestworldScript.BrawlAfter.cs (FOURTEENHOUR item 12)
                return;
            }
            if (m.Mode == BarRound.Kind && row.Id == "barround:damage")
            {
                SaloonDamagePay();   // the broken chairs (FOURTEENHOUR item 12)
                return;
            }
            if (m.Mode == BrawlVerbs.Kind || m.Mode == BrawlVerbs.PayKind)
            {
                BrawlVerbChoose(row.Id);   // WestworldScript.BrawlVerbs.cs (FOURTEENHOUR item 11)
                return;
            }
            if (m.Mode == PurseSnatch.Kind)
            {
                ThiefChoose(row.Id);   // WestworldScript.PurseSnatch.cs (FOURTEENHOUR item 20)
                return;
            }
            if (m.Mode == "testify")
            {
                string what = row.Id.Substring("testify:".Length);
                _engine.Testify(e, what);
                _testified++;
                if (r != null && r.E == e)
                {
                    r.Told = what;
                    r.NextCall = now + 2500;
                }
                return;
            }
            if (m.Mode == "swear")
            {
                if (row.Id != "swear:yes")
                    return;
                _engine.SwornIn(e);
                _sworn++;
                if (r != null && r.E == e && r.Deputies.Count > 0)
                {
                    Ped dep = r.Deputies[0];
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(dep.Handle, Game.Player.Character.Handle, 5000, 0f, 0f, 0f);
                    SpeechCaption("A sworn man", Trials.DeputySwears(e.Id + "|oath"), dep.Handle, 7000, true, false);
                    r.NextCall = now + 9000;
                }
                return;
            }
            if (m.Mode == "breakout" && row.Id == "breakout:yes")
                BreakoutDo(e, now);
            if (m.Mode == "show" && row.Id != "show:watch")
                ShowChoice(r, e, row.Id.Substring("show:".Length), now);   // WestworldScript.Shows.cs
            if (m.Mode == BoxSocial.Kind && row.Id == "box:bid")
            {
                BoxSocialBid(e);   // WestworldScript.BoxSocial.cs (FIVEDAY R6)
                return;
            }
            if (m.Mode == Foreclosure.Kind && row.Id == "foreclosure:buy")
            {
                ForeclosureBuy(e);   // WestworldScript.Foreclosure.cs (FIVEDAY R3)
                return;
            }
            if (m.Mode == WantedWhich.Kind)
            {
                WantedChoice(e, row.Id.Substring("wanted:".Length));   // WestworldScript.Wanted.cs (FIVEDAY R31)
                return;
            }
            if (m.Mode == NeighbourReward.Kind)
            {
                RewardChoice(e, row.Id.Substring("reward:".Length));   // WestworldScript.Reward.cs (FIVEDAY R30)
                return;
            }
            if (m.Mode == WalkHome.Kind && row.Id == "walkhome:offer")
            {
                WalkHomeOffer(e);   // WestworldScript.WalkHome.cs (THREEHOUR item 4)
                return;
            }
            if (m.Mode == BarRound.Kind && row.Id == "barround:buy")
            {
                BarRoundBuy(e);   // WestworldScript.BarRound.cs (THREEHOUR item 3)
                return;
            }
            if (m.Mode == Slate.Kind && row.Id == "slate:pay")
            {
                SlatePay(e);   // WestworldScript.Slate.cs (FIVEDAY R2)
                return;
            }
            if (m.Mode == PrizeFight.Kind && row.Id != "fight:watch")
                PrizeFightChoice(r, e, row.Id.Substring("fight:".Length), now);   // WestworldScript.PrizeFight.cs
        }

        /// Each frame with the reader closed: the menu's box, or the prompt's line.
        private void EventMenuDraw()
        {
            LetterMenu m = _eventMenu;
            if (m != null)
            {
                PAD.DISABLE_ALL_CONTROL_ACTIONS(PlayerPad);   // the game's own keys held off while the menu is up, as the reader does
                MenuBoxDraw(m);
                return;
            }
            if (_eventPrompt == null || _eventPromptText == null || Game.GameTime > _eventPromptUntil)
                return;
            string text = _eventPromptText;
            float s = 0.3f;
            Draw.Text(text, 0.5f - Draw.Width(text, s) / 2f, 0.85f, s, 236, 214, 160);
        }

        private string TrialsRunStatus()
        {
            return _trialsStaged + " trials and " + _gallowsStaged + " hangings staged, " + _testified + " testified, " + _sworn + " sworn in, " + _breakouts + " breakouts, " + _broughtInCount + " brought in" +
                   (_jailDoor.Count > 0 ? "; jail doors known in " + string.Join(", ", _jailDoor.Keys) : "") + (_eventPrompt != null ? "; prompt: " + _eventPrompt : "");
        }
    }
}
