// Walking someone home (SIXDAY item 1; Core/Fondness.cs has the offer, the answers and the words). After dark in a town,
// Talk on a woman, someone drunk or a timid man offers "Let me see you home."; accepted, they set off for a spot 60-100 m
// away (their door, as far as the street knows) at a walk, and you keep up: fall more than 8 m behind for five seconds and
// they stop and wait, looking back; wait twenty seconds and they go on alone, stung. At the door they turn and thank you in
// their own voice, and it's a kindness the mind understands (a "help" happening: trust, the late thanks, the allies'
// warmth) and a memory ("{you} walked me home, the night I was afraid"). Why it works on them: the street after dark was
// a real fear in 1899, and company against it is protection; protection is the oldest ground of attachment. One escort at
// a time; nothing else of RealNPCs takes them while it lasts. Testing row "escort". F7 in "TEST fondness:"; log "FOND escort".
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
        private sealed class EscortRun
        {
            public Ped Ped;
            public string Id, Name, Temper, Phase = "start";   // start, walk, wait
            public bool Woman, Drunk, Test;
            public Vector3 Home;
            public int StartedAt, StepAt, LagSince, WaitSince, OrderedAt;
            public float Pace = 1.0f;   // a walk; a man in a hurry for the doctor goes brisk (FOURTEENHOUR reserve R4)
            public bool DoorRun;        // he ran for the doctor: his own words at his door, and a voice inside (WestworldScript.DoctorsDoor.cs)
        }

        private EscortRun _escort;
        private bool _escortTest;   // the Testing row: the offer shows whatever the hour and the place
        private int _escortsStarted, _escortsHome, _escortsGaveUp;
        private string _escortLast;

        /// Their door, as far as the street knows: a spot 60-100 m off that someone can stand on, out of doors.
        private bool EscortSpot(Vector3 from, out Vector3 at)
        {
            for (int tries = 0; tries < 12; tries++)
            {
                double ang = _rng.NextDouble() * Math.PI * 2;
                float d = Fondness.EscortMinMetres + (float)_rng.NextDouble() * (Fondness.EscortMaxMetres - Fondness.EscortMinMetres);
                Vector3 c = from + new Vector3((float)Math.Cos(ang) * d, (float)Math.Sin(ang) * d, 0f);
                Vector3 safe;
                if (!Natives.StandSpot(c, from.Z, out safe))
                    continue;
                if (safe.DistanceTo(from) < Fondness.EscortMinMetres * 0.6f)
                    continue;   // snapped back beside them: no walk at all
                at = safe;
                return true;
            }
            at = Vector3.Zero;
            return false;
        }

        /// They said yes (TalkAnswer): the walk begins once the list has closed (the first step orders it). False when
        /// there's no spot to walk to (the list goes on as after any answer).
        private bool EscortStart(TalkMenu t, int now)
        {
            return t != null && EscortStartFor(t.Handle, t.Id, t.Name, t.Temper, t.Woman, t.Drunk, now);
        }

        /// The walk for whoever said yes: through Talk, or to J (THREEHOUR item 4, WestworldScript.WalkHome.cs).
        private bool EscortStartFor(int handle, string id, string name, string temper, bool woman, bool drunk, int now)
        {
            if (_escort != null || !ENTITY.DOES_ENTITY_EXIST(handle))
                return false;
            Ped p = new Ped(handle);
            Vector3 home;
            if (!EscortSpot(p.Position, out home))
            {
                Log.Write("FOND escort: no way home found for " + name + " (no spot 60-100 m off to walk to)");
                return false;
            }
            _escort = new EscortRun { Ped = p, Id = id, Name = name, Temper = temper, Woman = woman, Drunk = drunk, Home = home, StartedAt = now, StepAt = now + 1500, Test = _escortTest };
            _escortsStarted++;
            Log.Write("FOND escort: you're seeing " + name + " home (" + (int)home.DistanceTo(p.Position) + " m off)" + (_escortTest ? " (a test)" : ""));
            return true;
        }

        private void EscortWalk(EscortRun e, int now)
        {
            int h = e.Ped.Handle;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, e.Home.X, e.Home.Y, e.Home.Z, e.Pace, 120000, 1.5f, 0, 0f);
            e.Phase = "walk";
            e.OrderedAt = now;
            e.LagSince = 0;
        }

        /// Tick04, every quarter second while someone is being seen home.
        private void EscortStep(int now)
        {
            EscortRun e = _escort;
            if (e == null)
                return;
            if (_featureOff.Contains("escort"))
            {
                EscortAbort();   // Safe switched the escort off after errors: nobody stays held or busy for it (the review)
                return;
            }
            if (now < e.StepAt)
                return;
            e.StepAt = now + 250;
            Safe("escort", () => EscortStepImpl(e, _now));
        }

        private void EscortStepImpl(EscortRun e, int now)
        {
            if (!ReferenceEquals(e, _escort))
                return;
            Ped me = Game.Player.Character;
            if (e.Ped == null || !e.Ped.Exists() || e.Ped.IsDead || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(e.Ped.Handle, 0) || PED.IS_PED_FLEEING(e.Ped.Handle) ||
                PED.IS_PED_RAGDOLL(e.Ped.Handle) || _director.Involves(e.Ped.Handle))
            {
                EscortEnd("something came up: " + (e.Ped != null && e.Ped.Exists() ? (PED.IS_PED_IN_COMBAT(e.Ped.Handle, 0) || PED.IS_PED_FLEEING(e.Ped.Handle) ? "they're fighting or fleeing" : "a mission or a scene took them") : "they're gone"), 0);
                return;
            }
            int h = e.Ped.Handle;
            if (PED.IS_PED_IN_COMBAT(me.Handle, 0) || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0)
            {
                EscortGiveUp(e, me, now, PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) ? "you rode off" : "trouble came");
                return;
            }
            if (e.Phase == "start")
            {
                EscortWalk(e, now);
                return;
            }
            Vector3 at = e.Ped.Position;
            float lag = me.Position.DistanceTo(at);
            if (e.Phase == "walk")
            {
                if (at.DistanceTo(e.Home) < 2.5f)
                {
                    EscortArrive(e, me, now);
                    return;
                }
                if (now - e.StartedAt > 240000)
                {
                    EscortGiveUp(e, me, now, "it took too long (four minutes)");
                    return;
                }
                if (lag > Fondness.EscortLagMetres)
                {
                    if (e.LagSince == 0)
                        e.LagSince = now;
                    else if (now - e.LagSince > Fondness.EscortLagMs)
                    {
                        TASK.TASK_STAND_STILL(h, Fondness.EscortWaitMs);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, Fondness.EscortWaitMs, 0, 51, 0);
                        e.Phase = "wait";
                        e.WaitSince = now;
                        Log.Detail("FOND escort: " + e.Name + " stops and waits for you (" + (int)lag + " m behind)");
                    }
                }
                else
                    e.LagSince = 0;
                if (ENTITY.GET_ENTITY_SPEED(h) < 0.2f && now - e.OrderedAt > 6000)
                    EscortWalk(e, now);   // stopped (an event, a door, the game's own orders): on their way again
            }
            else if (e.Phase == "wait")
            {
                if (lag <= Fondness.EscortLagMetres * 0.6f)
                    EscortWalk(e, now);   // caught up: on they go
                else if (now - e.WaitSince > Fondness.EscortWaitMs)
                    EscortGiveUp(e, me, now, "you fell behind");
            }
        }

        /// At the door: they turn, thank you in their own voice, and it's a kindness done them (the mind, the late thanks,
        /// enRAGE's fondness, a memory); then they go on in (a wander: the street has no doors of theirs).
        private void EscortArrive(EscortRun e, Ped me, int now)
        {
            int h = e.Ped.Handle;
            long g = _store.LastGameMin;
            string seed = e.Id + "|escort|" + now / 1000;
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 4000, 0f, 0f, 0f);
            bool door = e.DoorRun && DoorRunAtDoor(h, seed);   // a man who ran for the doctor: his own words at his door, a voice inside (reserve R4)
            if (!door)
            {
                string line = Fondness.EscortThanks(e.Temper, e.Woman, e.Drunk, seed);
                Feed.Say(Person.FirstOf(e.Name), line, Speech.For(h, line, me.Handle, 0, 0, true, "thanks"), h, toYou: true);
            }
            if (!e.Test && e.Id != null)
            {
                RecordEvent("help", "player", "you", e.Id, e.Name, e.Ped.Position, me.Handle, me.Handle, h, Fondness.EscortOutcome);   // what late thanks reads (Thanks.KindOf: escort)
                FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(h) : null;
                if (ac != null)
                    _bridge.LikeMore(ac.Id, Fondness.EscortLiking);   // enRAGE's own method for its own townsfolk
                _engine.Escorted(e.Id, e.Name, g, true);
                if (Helps.WalkHomeMendsOn)
                {
                    // seen home while the drink is his worry: mended, as the coffee is (SEAMS item 15). The worry itself is the
                    // test, not e.Drunk: J's walk home never knows a man's in his cups (review 4: InHisCups without his person)
                    MindWho ww = MindKnown(h, now);
                    Person wp = ww != null && ww.P != null ? ww.P : _engine.KinPerson(e.Id);
                    string wk;
                    int stage;
                    bool good;
                    if (wp != null && InnerLife.WorryNow(wp, g / 1440, out wk, out stage, out good) && Helps.WalkHomeMends(true, wk, stage))
                    {
                        _engine.SettleWorry(e.Id);
                        Log.Write("FOND escort: " + e.Name + " seen home from the drink: his drinking worry mended (the rest of its run ends well)");
                    }
                }
            }
            if (!door)
                _ticker.Push(Person.FirstOf(e.Name) + ", thinking", Fondness.EscortThought(e.Woman, e.Drunk, seed), "near", h);
            _escortsHome++;
            _escortLast = e.Name + " seen home";
            _fondLast = e.Name + " seen home";
            Log.Write("FOND escort: " + e.Name + " is home; they thank you" + (e.Test ? " (a test: nothing written)" : ""));
            EscortEnd(null, 5000);
        }

        /// You fell behind, rode off, or trouble came: they go on alone, stung, and think the less of it (a share of the
        /// affection for the thought that counted).
        private void EscortGiveUp(EscortRun e, Ped me, int now, string why)
        {
            int h = e.Ped.Handle;
            string seed = e.Id + "|escortgu|" + now / 1000;
            if (me.Position.DistanceTo(e.Ped.Position) < 15f)
            {
                string line = Fondness.EscortGaveUp(seed);
                Feed.Say(Person.FirstOf(e.Name), line, Speech.For(h, line, me.Handle, 0, 0, false, "chat"), h, toYou: true);
            }
            if (!e.Test && e.Id != null)
                _engine.Escorted(e.Id, e.Name, _store.LastGameMin, false);
            if (NodInView(me, e.Ped) || me.Position.DistanceTo(e.Ped.Position) < 15f)
                _ticker.Push(Person.FirstOf(e.Name) + ", thinking", Fondness.EscortGaveUpThought(seed), "near", h);
            _escortsGaveUp++;
            _escortLast = e.Name + ": " + why;
            Log.Write("FOND escort: " + e.Name + " goes on alone (" + why + ")");
            EscortEnd(null, 0);
        }

        /// Let go: the hold off them, and on their way.
        private void EscortEnd(string why, int wanderAfterMs)
        {
            EscortRun e = _escort;
            _escort = null;
            if (e == null)
                return;
            if (e.Ped != null && e.Ped.Exists() && !e.Ped.IsDead)
            {
                int h = e.Ped.Handle;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                PED.SET_PED_KEEP_TASK(h, false);
                TASK.TASK_CLEAR_LOOK_AT(h);
                if (!_director.Involves(h) && !PED.IS_PED_IN_COMBAT(h, 0))
                {
                    if (wanderAfterMs > 0)
                        WanderLater(h, Game.GameTime + wanderAfterMs);   // after the thanks, in they go (a wander: the street knows no doors)
                    else
                        TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                }
            }
            if (why != null)
            {
                _escortLast = e.Name + ": " + why;
                Log.Write("FOND escort: " + e.Name + " let go (" + why + ")");
            }
        }

        /// Someone being seen home: nothing else of RealNPCs takes them (IsAcknowledging).
        private bool EscortHas(int h)
        {
            return _escort != null && _escort.Ped != null && _escort.Ped.Handle == h;
        }

        private void EscortAbort()
        {
            if (_escort != null)
                EscortEnd("stopped", 0);
        }

        /// The Testing row: Talk on the nearest adult RealNPCs knows within 6 m, with the offer to see them home shown whatever
        /// the hour and the place (the walk itself is as real as any).
        private string EscortTest(int now)
        {
            if (!_cfg.FondnessActs)
                return "[Encounters] FondnessActs is off";
            if (_escort != null)
                return "you're already seeing " + _escort.Name + " home";
            if (_menu != null)
                TalkClose("the Testing row again");   // (its close would clear the flag set next: the review)
            _escortTest = true;
            string r = TalkTest(now);   // WestworldScript.Menu.cs: the list on the nearest known person
            if (r != null)
                _escortTest = false;
            return r;
        }

        private string EscortStatus()
        {
            return _escortsStarted + " walks home begun, " + _escortsHome + " seen to the door, " + _escortsGaveUp + " gone on alone" +
                   (_escort != null ? "; now: " + _escort.Name + " (" + _escort.Phase + ", " + (int)_escort.Ped.Position.DistanceTo(_escort.Home) + " m to go)" : "") + (_escortLast != null ? "; last: " + _escortLast : "");
        }
    }
}
