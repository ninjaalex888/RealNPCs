// The doctor's door at night, the game side (FOURTEENHOUR reserve R4; Core/DoctorsDoor.cs; [Scenes] DoctorsDoor). From DramaTick when
// the street is free, a look every 30 s after dark (nine till five) in a town whose doctor is at his post within 90 m of you (the
// patient's IsDoctor): one of the mind's men 15-80 m from the doctor whose child's fever is at its worst (or whose wife's time has
// come, now and then) runs to him (the patient's walk order, at a run), pounds and calls out; the doctor looks at him and answers from
// his post (the patient's head-only look and its say: the box, their voices, the ticker); the man turns for home. J then: "I'll walk
// with you" starts the walk home's escort (seen as the J walk home's, built on Talk's escort) at his brisk pace, and at his door a
// voice calls from inside (a caption) and he thanks you; or he runs home alone. Next morning the town has it (Engine.WaysideHeard's
// news): the fever worry's own ending, and whether a stranger walked with him. Once a town a night. Never a frame of its own: the
// run is stepped from the scenes' helpers, beside the patient's.
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
        private sealed class DoorRun
        {
            public Ped Man, Doctor;
            public Person P;
            public string Id, Name, DoctorName, Why, Child, Seed, Town, Phase = "run";   // run, answer, turn, escort, home
            public bool Girl, Walked, Test;
            public int StartedAt, PhaseAt, HomeCount, StepAt;   // HomeCount: the escorts seen home before yours (did yours reach his door)
            public long Day;
        }

        private DoorRun _doorRun;
        private int _doorLookAt, _doorRuns, _doorWalked, _doorReached;
        private string _doorLast;

        /// From DramaTick when the street is free: tonight's run to the doctor, if one fits. why: what stood in the way (the test).
        private bool DoctorsDoorScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!DoctorsDoor.On || !_cfg.Drama)
            {
                why = "off ([Scenes] DoctorsDoor, or [World] Drama)";
                return false;
            }
            if (_doorRun != null || _patient != null || _escort != null)
            {
                why = "one is going already, or someone is being seen home";
                return false;
            }
            if (!test && now - _doorLookAt < DoctorsDoor.LookMs && now >= _doorLookAt)
                return false;
            _doorLookAt = now;
            if (_placeTown == null || _interiorType != 0)
            {
                why = "it wants a town street, out of doors";
                return false;
            }
            if (!test && !DoctorsDoor.Hour(CLOCK.GET_CLOCK_HOURS()))
            {
                why = "after dark only (nine till five)";
                return false;
            }
            long g = _store.LastGameMin;
            string key = "doctor|" + _placeTown + "|" + WalkHome.NightKey(g);
            if (!test && _nightDone.Contains(key))
                return false;
            Ped doctor = Peds.All().Where(p => p != null && p.Exists() && !ENTITY.IS_ENTITY_DEAD(p.Handle) && p.Position.DistanceTo(me.Position) <= 90f && IsDoctor(p))
                                   .OrderBy(p => p.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (doctor == null)
            {
                why = "no doctor at his post within 90 m of you tonight";
                return false;
            }
            long day = g / 1440;
            int year = Voice.NowYear > 0 ? Voice.NowYear : 1899;
            Candidate pick = null;
            string runWhy = null;
            foreach (Candidate c in ReasonCandidates(me, now, test).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)))
            {
                if (c.Who.Woman || c.Who.P == null || c.Who.Name == null || IsDoctor(c.Ped) || NotFree(c.Ped.Handle) || PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || MayDirectWhyNot(c.Ped) != null)
                    continue;
                float d = c.Ped.Position.DistanceTo(doctor.Position);
                if (d < DoctorsDoor.LeastMetres || d > DoctorsDoor.FromMetres)
                    continue;
                string wk; int stage; bool good;
                if (!InnerLife.WorryNow(c.Who.P, day, out wk, out stage, out good))
                    wk = null;
                PersonState st = _store.Find(c.Who.Id);
                List<Relative> fam = st != null && st.Family != null ? st.Family : Family.For(c.Who.P);
                bool married = fam != null && fam.Any(r => r != null && r.Relation == "wife");
                string w = DoctorsDoor.Why(wk, stage, married, c.Who.P.AgeIn(year), c.Who.Id, day);
                if (w == null && !test)
                    continue;
                pick = c;
                runWhy = w ?? "fever";
                break;
            }
            if (pick == null)
            {
                why = "nobody 15-80 m from the doctor with a child's fever at its worst tonight";
                return false;
            }
            string seed = pick.Who.Id + "|door|" + day;
            if (!test)
            {
                _nightDone.Add(key);   // the night's roll, whatever it says
                if (!Voice.Chance(seed, DoctorsDoor.Percent))
                {
                    Log.Detail("DOCTORSDOOR " + pick.Who.Name + "'s night: he sits up with them and doesn't run for the doctor");
                    return false;
                }
            }
            bool girl = DoctorsDoor.ChildGirl(pick.Who.Id);
            _doorRun = new DoorRun
            {
                Man = pick.Ped, Doctor = doctor, P = pick.Who.P, Id = pick.Who.Id, Name = pick.Who.Name, DoctorName = DoctorLabel(doctor), Why = runWhy, Girl = girl,
                Child = runWhy == "fever" ? Residents.FirstName(girl, pick.Who.Id + "|child") : null,   // the child the fever worry names (InnerLife's own naming)
                Seed = seed, Town = _placeTown, Day = day, StartedAt = now, PhaseAt = now, Test = test
            };
            _doorRuns++;
            NoteInScene(new[] { pick.Ped }, "patient", now);   // busy for the other features while he runs (the patient's)
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(pick.Ped.Handle, true);
            TASK.CLEAR_PED_TASKS(pick.Ped.Handle, true, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(pick.Ped.Handle, doctor.Position.X, doctor.Position.Y, doctor.Position.Z, 3f, 60000, 1.8f, 0, 0f);   // at a run
            Log.Write("DOCTORSDOOR " + pick.Who.Name + " runs for the doctor (" + (runWhy == "birth" ? "his wife's time" : "the child's fever") + "), " +
                      (int)pick.Ped.Position.DistanceTo(doctor.Position) + " m from him" + (test ? " (a test)" : ""));
            return true;
        }

        /// From the scenes' helpers, beside the patient's: the run, the knock, the answer, the turn for home.
        private void DoorRunStep(int now)
        {
            DoorRun r = _doorRun;
            if (r == null || now < r.StepAt)
                return;
            r.StepAt = now + 250;   // a look a quarter second
            Ped me = Game.Player.Character;
            bool manOk = r.Man != null && r.Man.Exists() && !r.Man.IsDead && !PED.IS_PED_IN_COMBAT(r.Man.Handle, 0) && !_director.Involves(r.Man.Handle) && MayDirectWhyNot(r.Man) == null;
            if (!manOk || MISC.GET_MISSION_FLAG())
            {
                DoorRunEnd("he's taken up with something else", false, r.Phase != "run" && r.Phase != "answer");
                return;
            }
            int h = r.Man.Handle;
            switch (r.Phase)
            {
                case "run":
                    {
                        bool doctorOk = r.Doctor != null && r.Doctor.Exists() && !r.Doctor.IsDead;
                        if (!doctorOk || now - r.PhaseAt > 60000)
                        {
                            DoorRunEnd(doctorOk ? "he never reached the doctor" : "the doctor's gone", false, false);
                            return;
                        }
                        if (r.Man.Position.DistanceTo(r.Doctor.Position) < 2.8f)
                        {
                            TASK.CLEAR_PED_TASKS(h, true, false);
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, r.Doctor.Handle, 3000, 0f, 0f, 0f);
                            TASK.TASK_LOOK_AT_ENTITY(r.Doctor.Handle, h, 9000, 0, 51, 0);   // the doctor looks: head only, he keeps his post
                            PatientSay(Person.FirstOf(r.Name), DoctorsDoor.Knock(r.Why, r.Child, r.Girl, r.Seed), r.Man, r.Doctor);
                            _doorReached++;
                            r.Phase = "answer";
                            r.PhaseAt = now;
                        }
                        return;
                    }
                case "answer":
                    if (now - r.PhaseAt < 2600)
                        return;
                    if (r.Doctor != null && r.Doctor.Exists() && !r.Doctor.IsDead)
                        PatientSay(r.DoctorName, DoctorsDoor.Answer(r.Why, r.Seed), r.Doctor, r.Man);
                    r.Phase = "turn";
                    r.PhaseAt = now;
                    if (r.Man.Position.DistanceTo(me.Position) < 25f && _eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                        PromptSet(DoctorsDoor.Kind, new TownEvent { Kind = DoctorsDoor.Kind, Town = _placeTown, AName = r.Name }, DoctorsDoor.PromptMs);   // J as he turns
                    return;
                case "turn":
                    if (_eventMenu != null && _eventMenu.Mode == DoctorsDoor.Kind)
                    {
                        r.PhaseAt = Math.Max(r.PhaseAt, now - 1500);   // the menu open: he waits a moment on your word
                        return;
                    }
                    if (now - r.PhaseAt < DoctorsDoor.PromptMs && _eventPrompt == DoctorsDoor.Kind && Game.GameTime <= _eventPromptUntil)
                        return;   // a man in a hurry waits a moment on your word, no more
                    if (now - r.PhaseAt < 1200)
                        return;
                    // no word from you (or the row that sends him on): home at a run, alone
                    if (_eventPrompt == DoctorsDoor.Kind)
                        PromptClear();
                    Vector3 home;
                    if (EscortSpot(r.Man.Position, out home))
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, home.X, home.Y, home.Z, 3f, 60000, 1.5f, 0, 0f);
                    _ticker.Push(Person.FirstOf(r.Name) + ", thinking", DoctorsDoor.Thought(false, r.Seed), "near", h);
                    DoorRunEnd("he ran home alone", false, true, 25000);
                    return;
                case "escort":
                    if (_escort == null)
                        DoorRunEnd(_escortsHome > r.HomeCount ? "you saw him to his door" : "he went on alone (the walk was given up)", _escortsHome > r.HomeCount, true);   // the escort's own end
                    return;
            }
        }

        /// He's running for the doctor (not yet seen home: the escort counts him then): busy for the other features.
        private bool DoorRunning(int h)
        {
            DoorRun r = _doorRun;
            return r != null && r.Man != null && r.Man.Handle == h && r.Phase != "escort";
        }

        /// J's row as he turns: "I'll walk with you" starts the walk home's escort, at his pace.
        private void DoctorsDoorChoose(string id)
        {
            DoorRun r = _doorRun;
            if (r == null || r.Phase != "turn")
                return;
            if (id != "doctorsdoor:walk")
            {
                r.PhaseAt = Game.GameTime - 1300;   // let him go: off he runs, next look
                return;
            }
            if (r.Man == null || !r.Man.Exists() || r.Man.Position.DistanceTo(Game.Player.Character.Position) > 25f)
                return;
            string temper = r.P != null ? r.P.Personality : null;
            r.HomeCount = _escortsHome;
            if (!EscortStartFor(r.Man.Handle, r.Id, r.Name, temper, false, false, Game.GameTime))
            {
                Hint("He can't wait for you: he runs on.", 2500);
                r.PhaseAt = Game.GameTime - 1300;
                return;
            }
            _escort.Pace = Natives.Brisk;   // a brisk walk beside a man in a hurry
            _escort.DoorRun = true;
            r.Walked = true;
            r.Phase = "escort";
            _doorWalked++;
            Log.Write("DOCTORSDOOR you walk " + r.Name + " home");
        }

        /// The escort at his door, for a man who ran for the doctor: his own words, the voice inside, his thought (EscortArrive).
        private bool DoorRunAtDoor(int h, string seed)
        {
            DoorRun r = _doorRun;
            if (r == null || r.Man == null || r.Man.Handle != h)
                return false;
            Feed.Say(Person.FirstOf(r.Name), DoctorsDoor.AtHisDoor(seed), Speech.When(h, "thanks", Game.Player.Character.Handle), h, toYou: true);
            SpeechCaption("A voice from inside", DoctorsDoor.Inside(r.Why, seed), h, 4500, false, false);
            _ticker.Push(Person.FirstOf(r.Name) + ", thinking", DoctorsDoor.Thought(true, seed), "near", h);
            return true;
        }

        private void DoorRunEnd(string why, bool walked, bool reached, int wanderAfterMs = 0)
        {
            DoorRun r = _doorRun;
            _doorRun = null;
            if (r == null)
                return;
            if (_eventPrompt == DoctorsDoor.Kind)
                PromptClear();
            if (r.Man != null && r.Man.Exists() && !r.Man.IsDead && r.Phase != "escort")
            {
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Man.Handle, false);
                if (wanderAfterMs > 0)
                    WanderLater(r.Man.Handle, Game.GameTime + wanderAfterMs);   // home, then on his way (the street has no doors of his)
                else if (!_director.Involves(r.Man.Handle) && !PED.IS_PED_IN_COMBAT(r.Man.Handle, 0))
                    TASK.TASK_WANDER_STANDARD(r.Man.Handle, 10f, 10);
            }
            if (reached && !r.Test && r.Town != null)
            {
                // next morning the town has it: the fever worry's own ending, and whether a stranger walked with him
                bool well = DoctorsDoor.EndsWell(r.P, r.Why, r.Day, r.Seed);
                Safe("doctor's door", () => _engine.WaysideHeard(DoctorsDoor.News(r.Name, r.Child, r.Girl, r.Why, well, walked, r.Seed), r.Town, r.Seed + "|news", _store.LastGameMin));
            }
            _doorLast = r.Name + ": " + why;
            Log.Write("DOCTORSDOOR " + r.Name + ": " + why);
        }

        /// The Testing row: the nearest named man 15-80 m from the doctor runs to him now (any hour; a child's fever).
        private string DoctorsDoorFire(int now, out string said)
        {
            said = null;
            string why;
            if (!DoctorsDoorScene(Game.Player.Character, now, true, out why))
                return why ?? "nothing fit";
            said = _doorRun != null ? _doorRun.Name + " runs for the doctor; J as he turns for home" : null;
            return null;
        }

        /// F7: the runs to the doctor this session.
        private string DoctorsDoorStatus()
        {
            if (!DoctorsDoor.On)
                return "off ([Scenes] DoctorsDoor)";
            return _doorRuns + " ran for the doctor (reached him " + _doorReached + ", you walked with " + _doorWalked + ")" + (_doorLast != null ? "; last: " + _doorLast : "");
        }
    }
}
