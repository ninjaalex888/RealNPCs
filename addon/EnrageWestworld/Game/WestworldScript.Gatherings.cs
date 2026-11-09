// The gathering, in the game (the hours, the ring, the creep and the lines are in Core/Encounters.cs). After midnight
// in the wilds (once a night), and with [Encounters] GatheringHours any at any other hour too, in the open or just
// outside a town's edge (Lex, 2026-10-31: "make them happen more often and at any time of day"), at least
// GatheringGapHours apart, on a roll, six to ten of the awake stand in a ring 45-70 m off, out of sight, facing in: residents
// who are unravelling or awake, in the looks they were met in, made up with strangers. Within 30 m they all turn
// their heads to you. While none of them is on your screen they step closer, never nearer than a few metres; the
// moment one is seen, all of them stop dead. Step into the middle and they speak at once. Draw on one, shoot, or hit
// one and they scatter. Half of them (GatheringHostile) come at you, bare-handed, once you are within 10 m; killing one counts as a killing unless they came at you (Watch.cs).
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
        private sealed class Gatherer
        {
            public Ped Ped;
            public Resident R;      // null for a stranger
            public uint Model;
            public bool Woman;
            public float X, Y;      // place in the ring, from the centre
            public string Act;      // what they do in the ring (GatherActs; Lex, 2026-11-01: "a wide range of eerie animations")
            public Blip Dot;        // the red enemy dot while a hostile ring comes at you
            public bool Lead;       // the one in the middle: who leads it, or who they're trying to wake (FIVEDAY2 item 14)
            public int GoAt;        // a procession: when this one sets off (0: gone already)
        }

        private sealed class Gathering
        {
            public string Phase = "spawn";   // spawn, stand, watched, scatter
            public Vector3 Centre;
            public readonly List<Gatherer> Ring = new List<Gatherer>();
            public int StartedAt, PhaseAt, NextCreep, NextMurmur;
            public int ResumeAt, NextHum;   // back to their acts after a stop; the singers' next low sound
            public bool Moving, Spoke, Test;
            public bool Hostile;   // rolled at the start ([Encounters] GatheringHostile): they turn on you when you come within 10 m
            public bool Day;   // outside midnight to 03:30 (GatheringHours any): in the open or at a town's edge
            public bool Vanishes;   // rolled at the start (a third, never a hostile one): look away once you've seen them and they're gone (Lex, 2026-11-02: eerier)
            public bool WasSeen;
            public int UnseenSince;
            // the kinds of ring (Core/Encounters.cs; FIVEDAY2 item 14)
            public string Kind = Encounters.RingPlain;
            public Vector3 From;              // a procession: where it starts; Centre is where it's going
            public int NextWord, AnswerAt, Words;
            public bool Woken, KnownSaid;
            // the circle answers you (FIVEDAY2 item 10; Core/GroupActs.cs)
            public string AskKey;
            public List<string> AskMenu;
            public LanguageRequest AskRequest;
            public int AskDeadline;
            public bool Answered;
        }

        /// The hours a gathering belongs to (Lex, 2026-11-02: rarer, eerier): from 21:00 to 05:00, or any hour in fog, rain or
        /// a storm. Outside them a gathering rolls at a quarter of its chance, and only with nobody else in sight.
        private bool GatherDark()
        {
            int hour = CLOCK.GET_CLOCK_HOURS(), minute = CLOCK.GET_CLOCK_MINUTES();
            if (Encounters.GatherHours(hour, minute) || hour >= 21 || hour < 5)
                return true;
            string w = InnerLife.WeatherOf(SkySeen());
            return w == "fog" || w == "rain" || w == "storm";
        }

        /// By day: who else is about to see it (anyone alive within 80 m and on your screen), or null.
        private string GatherOthersInSight()
        {
            Ped me = Game.Player.Character;
            Vector3 at = me.Position;
            int n = 0;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                if (p.Position.DistanceTo(at) > 80f || !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle))
                    continue;
                n++;
            }
            return n > 0 ? n + (n == 1 ? " person" : " people") + " in sight within 80 m" : null;
        }

        private Gathering _gathering;
        private string _gatherForce;   // the Testing page's choice of ring

        /// Someone you've met: a word with them, or your name told.
        private bool GatherMet(Resident r)
        {
            PersonState st = r != null ? _store.Find(r.Id) : null;
            return st != null && (st.KnowsName || st.Talks > 0);
        }

        private bool IsGatherer(int handle)
        {
            return _gathering != null && _gathering.Ring.Any(m => m.Ped != null && m.Ped.Handle == handle);
        }

        /// Why a gathering can't happen here and now; null when it can.
        private string GatherNotNow()
        {
            int hour = CLOCK.GET_CLOCK_HOURS(), minute = CLOCK.GET_CLOCK_MINUTES();
            bool night = Encounters.GatherHours(hour, minute);
            if (night)
            {
                // the night's, as it always was: out in the wilds, once a night
                string town = _bridge.TownOnly(Game.Player.Character.Position);   // TownAt gives the region out in the wilds
                if (town != null)
                    return "you're in " + town + " (at night only out in the wilds)";
                if (DoneToday("gather"))
                    return "there was one already tonight";
            }
            else if (_cfg.GatheringHours == "night")
                return "it's " + hour.ToString("00") + ":" + minute.ToString("00") + " (from midnight to 03:30; [Encounters] GatheringHours is night)";
            else if (_interiorType != 0)
                return "you're indoors";   // by day: in the open, or at a town's edge (StartGathering keeps the ring outside the town)
            int since = GatherHoursSince();
            if (since >= 0 && since < _cfg.GatheringGapHours)
                return "the last one was " + since + " game hours ago (at least " + _cfg.GatheringGapHours + ", [Encounters] GatheringGapHours)";
            return null;
        }

        /// Game hours since the last gathering stood, or -1 when there hasn't been one.
        private int GatherHoursSince()
        {
            int at;
            if (!_store.Counts.TryGetValue("enc:gatherAt", out at))
                return -1;
            int hour = (int)(Bridge.Now() / 60);
            if (hour <= 0)
                return -1;   // no game clock yet: the stamp kept for when there is
            if (at > hour)
            {
                _store.Counts.Remove("enc:gatherAt");   // later than now: an earlier save was loaded, so that gathering hasn't happened yet
                return -1;
            }
            return hour - at;
        }

        private bool StartGathering(int now, bool test = false)
        {
            Ped me = Game.Player.Character;
            bool day = !Encounters.GatherHours(CLOCK.GET_CLOCK_HOURS(), CLOCK.GET_CLOCK_MINUTES());
            bool inTown = day && _bridge.TownOnly(me.Position) != null;
            Vector3 centre = Vector3.Zero;
            bool found = false;
            for (int tries = 0; tries < (inTown ? 4 : 1) && !found; tries++)
            {
                // by day in town: the ring stands just outside its edge, never in the street
                found = EncounterSpot(me, 45f, 70f, out centre) && (!inTown || _bridge.TownOnly(centre) == null);
            }
            if (!found)
            {
                Log.Write("ENCOUNTER gathering: " + (inTown ? "nowhere out of sight outside the town for one (by day in town, only at its edge)" : "nowhere out of sight for one"));
                return false;
            }
            int n = Encounters.GatherSize(_rng.Next(100));
            Gathering g = new Gathering { Centre = centre, StartedAt = now, Test = test, Day = day };
            g.Hostile = _cfg.GatheringHostile > 0 && _rng.Next(100) < _cfg.GatheringHostile;   // Lex, 2026-11-01: half of them hostile, only within 10 m
            g.Vanishes = !g.Hostile && _rng.Next(3) == 0;
            g.Kind = _gatherForce ?? Encounters.GatherKind(_rng.Next(100), GatherDark());
            _gatherForce = null;
            if (g.Kind != Encounters.RingPlain)
                g.Vanishes = false;   // something is going on in it: it stays
            if (g.Kind == Encounters.RingProcession)
            {
                // it starts somewhere else and walks to the ring's own ground; no second place far enough off, and it's a plain ring
                Vector3 start;
                if (EncounterSpot(me, 45f, 70f, out start) && start.DistanceTo(centre) >= 25f)
                    g.From = start;
                else
                    g.Kind = Encounters.RingPlain;
            }
            // the awake first, as they looked when they were met; strangers make up the rest
            List<Resident> awake = _store.Residents.Where(r => !r.Dead && r.PostId == null && r.Cast == null && (_hermit == null || _hermit.R != r) && _engine.StageOf(r.Id) >= Awakening.Unravelling)
                .OrderByDescending(r => GatherMet(r) ? 1 : 0).ThenBy(r => _rng.Next()).Take(n).ToList();   // someone you've met first: a face you know in the ring is the point of it
            bool middle = g.Kind == Encounters.RingLeader || g.Kind == Encounters.RingSleeper;
            for (int i = 0; i < n; i++)
            {
                Resident r = i < awake.Count ? awake[i] : null;
                bool woman = r != null ? r.Woman : _rng.Next(100) < 35;
                float dx, dy;
                bool lead = middle && i == 0;
                if (lead)
                    dx = dy = 0f;   // in the middle
                else
                    Encounters.CirclePoint(middle ? i - 1 : i, middle ? n - 1 : n, Encounters.CircleRadius, out dx, out dy);
                g.Ring.Add(new Gatherer { R = r, Woman = woman, Model = r != null ? r.Model : Spawner.ModelFor(null, woman, _rng.Next()), X = dx, Y = dy, Lead = lead });
            }
            _gathering = g;
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);   // a big moment: the quiet starts (Pacing.cs)
            Log.Write("ENCOUNTER gathering (" + g.Kind + ")" + (day ? " by day" + (inTown ? ", at the town's edge" : ", in the open") : "") + ": " + n + " in a ring " + (int)centre.DistanceTo(me.Position) + " m away, " + awake.Count + " of them people who woke" +
                      (awake.Count > 0 ? " (" + string.Join(", ", awake.Select(r => r.Name)) + ")" : ""));
            return true;
        }

        // What each of the ring does (Lex, 2026-11-01: "a wide range of eerie animations they could be doing in a ring instead such
        // as kneeling, singing, dancing"): the game's own looping scenarios the add-on already plays elsewhere, in place, facing
        // in; "sing" stands stock-still and now and then makes a low wordless sound. Mixed round the ring by its seed.
        private static readonly string[] GatherActs = { "WORLD_HUMAN_GRAVE_MOURNING_KNEEL", "WORLD_HUMAN_DANCING", "sing", "WORLD_HUMAN_STARE_STOIC", "WORLD_HUMAN_SIT_GROUND",
                                                        "WORLD_HUMAN_GRAVE_MOURNING", "WORLD_HUMAN_SLEEP_GROUND_ARM", "WORLD_HUMAN_CROUCH_INSPECT", "WORLD_HUMAN_GUARD_LANTERN_NERVOUS" };

        private static void GatherAct(Gatherer m)
        {
            if (m == null || m.Ped == null || !m.Ped.Exists() || m.Ped.IsDead)
                return;
            int h = m.Ped.Handle;
            if (m.Act == null || m.Act == "sing")
                TASK.TASK_STAND_STILL(h, -1);
            else
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(m.Act), -1, true, 0u, -1f, false);
        }

        private void GatherSpawn(int now)
        {
            Gathering g = _gathering;
            if (now - g.StartedAt > 25000)
            {
                EndGathering("they couldn't be put there");
                return;
            }
            if (now - g.StartedAt > 10000)
                foreach (Gatherer m in g.Ring)
                    if (m.R != null && m.Model == m.R.Model && !Spawner.Ready(m.Model))
                        m.Model = Spawner.ModelFor(null, m.Woman, _rng.Next());   // their own looks won't load: someone like them
            bool ready = true;
            foreach (Gatherer m in g.Ring)
                ready &= Spawner.Ready(m.Model);
            bool walking = g.Kind == Encounters.RingProcession;
            if (!ready || CampInView(walking ? g.From : g.Centre))
                return;   // never seen arriving
            Vector3 along = walking ? (g.Centre - g.From).Normalized : Vector3.Zero;
            int inLine = 0;
            foreach (Gatherer m in g.Ring)
            {
                float x = walking ? g.From.X - along.X * inLine * 2.2f : g.Centre.X + m.X, y = walking ? g.From.Y - along.Y * inLine * 2.2f : g.Centre.Y + m.Y;
                Vector3 at = new Vector3(x, y, Natives.GroundZ(x, y, walking ? g.From.Z : g.Centre.Z));
                float heading = walking ? Encounters.Heading(along.X, along.Y) : Encounters.Heading(-m.X, -m.Y);   // along the way, or facing in
                int place = inLine++;
                Ped p = Spawner.Spawn(m.Model, at, heading);
                if (p == null)
                    continue;
                int h = p.Handle;
                if (ENTITY.IS_ENTITY_IN_WATER(h))
                {
                    Spawner.Release(p, true);
                    continue;
                }
                m.Ped = p;
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                PED.SET_PED_KEEP_TASK(h, true);
                ENTITY.SET_ENTITY_HEADING(h, heading);
                m.Act = GatherActFor(g, m);
                if (walking)
                {
                    m.GoAt = now + place * 1500;   // they string out as they set off
                    GatherTorch(h);
                }
                else
                    GatherAct(m);
            }
            g.Ring.RemoveAll(m => m.Ped == null);
            Log.Write("ENCOUNTER gathering acts: " + string.Join(", ", g.Ring.Select(m => m.Act)));
            if (g.Ring.Count < 3)
            {
                EndGathering("too few of them could stand there");
                return;
            }
            g.Phase = walking ? "walk" : "stand";
            g.PhaseAt = now;
            Log.Write("ENCOUNTER gathering: " + (walking ? "setting off in a line" : "standing") + ", " + g.Ring.Count + " of them, " + (int)g.Centre.DistanceTo(Game.Player.Character.Position) + " m away");
        }

        private void GatherStep(int now)
        {
            Gathering g = _gathering;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndGathering(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died");
                return;
            }
            if (g.Phase == "spawn")
            {
                GatherSpawn(now);
                return;
            }
            EventDot("gathering", g.Centre);
            foreach (Gatherer m in g.Ring)
                if (m.R != null && !m.R.Dead && m.Ped.Exists() && m.Ped.IsDead)
                {
                    LedgerNote(m.Ped.Handle, m.R.Id, m.R.Name, "resident", null, null);   // marks them dead too (Engine.NoteDied)
                    m.R.Dead = true;
                    m.R.DiedAt = Bridge.Now();
                    _engine.KeepResident(m.R, Bridge.Now());
                }
            List<Gatherer> live = g.Ring.Where(m => m.Ped.Exists() && !m.Ped.IsDead).ToList();
            if (live.Count == 0)
            {
                EndGathering("none of them is left");
                return;
            }
            Vector3 pos = me.Position;
            float toCentre = g.Centre.DistanceTo(pos);
            float nearest = live.Min(m => m.Ped.Position.DistanceTo(pos));
            if (Math.Min(toCentre, nearest) > 150f)
            {
                EndGathering("you rode away");
                return;
            }
            bool seen = live.Any(m => ENTITY.IS_ENTITY_ON_SCREEN(m.Ped.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, m.Ped.Handle, 17));
            if (g.Phase == "walk")
            {
                GatherWalk(g, live, me, now, nearest);
                return;
            }
            if (g.Phase == "scatter")
            {
                if ((!seen && nearest > 40f) || now - g.PhaseAt > 45000)
                    EndGathering("they scattered");
                return;
            }
            if (g.Phase == "hostile")
            {
                foreach (Gatherer m in live)
                    _cameAtYouSeen[m.Ped.Handle] = now;   // fighting them off is self-defence while they come at you
                if (nearest > 60f || now - g.PhaseAt > 120000)
                    EndGathering(nearest > 60f ? "you got away from them" : "they gave up on you");
                return;
            }
            if (g.Hostile && nearest < 10f)
            {
                // one of the hostile circles: within 10 m every one of them comes at you, bare hands, at once
                g.Phase = "hostile";
                g.PhaseAt = now;
                foreach (Gatherer m in live)
                {
                    int h = m.Ped.Handle;
                    MakeHostile(m.Ped, now);   // one group against you, together, as the crying woman's men (WestworldScript.Encounters.cs)
                    try
                    {
                        if (MAP.GET_BLIP_FROM_ENTITY(h) == 0)
                            m.Dot = new Blip(MAP.BLIP_ADD_FOR_ENTITY(Natives.Hash("BLIP_STYLE_ENEMY"), h));   // a red dot on each (Lex, 2026-11-01)
                    }
                    catch { }
                    int you = me.Handle;
                    ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, you, 0, 16));   // up out of their act the game's way and at you the same tick (a bare clear left the kneeling kneeling: Lex, 2026-11-01)
                    _cameAtYouSeen[h] = now;
                }
                Feed.Say("All of them, at once", Encounters.Pick(Encounters.GatherHostile, "hostile|" + NightIndex()), toYou: true);
                Log.Write("ENCOUNTER gathering: a hostile one; within 10 m all " + live.Count + " come at you");
                return;
            }
            if (now - g.StartedAt > 10 * 60000 && !seen)
            {
                EndGathering("the night moved on");
                return;
            }
            // a ring you've seen, looked away from for a moment, and looked back at: nobody there (a third of them)
            if (seen)
            {
                g.WasSeen = true;
                g.UnseenSince = 0;
            }
            else if (g.WasSeen && g.Vanishes && !g.Spoke && nearest > 12f)
            {
                if (g.UnseenSince == 0)
                    g.UnseenSince = now;
                else if (now - g.UnseenSince > 2500)
                {
                    EndGathering("gone when you looked back");   // the living vanish only where nobody sees it (EndGathering)
                    return;
                }
            }
            // draw on one, hurt one, or fire near them, and the ring breaks
            int aimedAt;
            bool threat = g.Ring.Any(m => m.Ped.Exists() && (m.Ped.IsDead || ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(m.Ped.Handle, me.Handle, true, false) ||
                                                              PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, m.Ped.Handle) ||
                                                              (_aimedAt.TryGetValue(m.Ped.Handle, out aimedAt) && now - aimedAt < 1500)));
            if (threat || (PED.IS_PED_SHOOTING(me.Handle) && nearest < 40f))
            {
                Scatter(g, live, me, now);
                return;
            }
            if (g.AskMenu != null && !g.Answered && CircleAnswerStep(g, live, me, now))
                return;
            if (g.Phase == "stand")
            {
                if (toCentre >= 30f)
                    return;
                g.Phase = "watched";   // every head turns to you; the bodies stay facing in
                g.PhaseAt = now;
                g.NextCreep = now + 3000;
                g.NextMurmur = now + 5000;
                foreach (Gatherer m in live)
                    TASK.TASK_LOOK_AT_ENTITY(m.Ped.Handle, me.Handle, -1, 0, 51, 0);
                Log.Write("ENCOUNTER gathering: they turn their heads to you");
            }
            if (!g.Spoke && toCentre < 1.6f)
            {
                // in the middle of them: all of them at once
                g.Spoke = true;
                g.Moving = false;
                foreach (Gatherer m in live)
                {
                    int h = m.Ped.Handle;
                    int you = me.Handle;
                    ScenarioExit.SoftUp(h, x => TASK.TASK_TURN_PED_TO_FACE_ENTITY(x, you, 3000, 0f, 0f, 0f));   // up out of their act, Rockstar's way
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                    Speech.SayAny(h, Speech.Hum, me.Handle);   // a wordless sound from every one of them at once
                }
                Feed.Say("All of them, at once", Encounters.Pick(Encounters.GatherTogether, "gather|" + NightIndex()), toYou: true);
                Log.Write("ENCOUNTER gathering: you stood in the middle; they spoke at once");
                g.NextCreep = now + 5000;
                CircleAsk(g, live, now);   // and then the ring decides what to do about you (FIVEDAY2 item 10)
                return;
            }
            if (seen)
            {
                if (g.Moving)
                {
                    // caught moving: every one of them stops dead, mid-step
                    g.Moving = false;
                    foreach (Gatherer m in live)
                    {
                        int h = m.Ped.Handle;
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, false);
                        TASK.TASK_STAND_STILL(h, -1);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                    }
                    g.ResumeAt = now + 4000;   // a moment stock-still, then each back to their act
                }
                if (!g.Moving && g.ResumeAt != 0 && now >= g.ResumeAt)
                {
                    g.ResumeAt = 0;
                    foreach (Gatherer m in live)
                        GatherAct(m);
                }
            }
            else if (now >= g.NextCreep && nearest < 45f)
            {
                g.NextCreep = now + 1200;
                foreach (Gatherer m in live)
                {
                    Vector3 mp = m.Ped.Position;
                    float step = Encounters.CreepStep(mp.DistanceTo(pos));
                    if (step <= 0f)
                        continue;
                    Vector3 to = mp + (pos - mp).Normalized * step;
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Ped.Handle, to.X, to.Y, to.Z, Natives.Brisk, -1, 0.3f, 0, 0f);
                    g.Moving = true;
                }
            }
            if (now >= g.NextHum && nearest < 40f && !g.Moving)
            {
                // the singers: a low wordless sound now and then, one of them at a time
                g.NextHum = now + 6000 + _rng.Next(5000);
                List<Gatherer> singers = live.Where(x => x.Act == "sing").ToList();
                if (singers.Count > 0)
                    Speech.SayAny(singers[_rng.Next(singers.Count)].Ped.Handle, Speech.Hum, 0);
            }
            GatherKindStep(g, live, me, now, nearest, toCentre);
            if (g.Kind == Encounters.RingPlain && now >= g.NextMurmur && nearest < 25f)
            {
                g.NextMurmur = now + 10000 + _rng.Next(6000);
                Gatherer m = live[_rng.Next(live.Count)];
                string who = m.R != null && m.Ped.Position.DistanceTo(pos) < 10f ? m.R.Name + ", low" : "One of them, low";
                Feed.Say(who, Encounters.Pick(Encounters.GatherMurmurs, "murmur|" + now / 10000), Speech.When(m.Ped.Handle, "hum"));
            }
        }

        /// What this one does in this kind of ring. The plain ring: all the same, whichever it is (Lex, 2026-11-01). With one in the
        /// middle the rest kneel; the one who leads stands, the one they're waking lies down.
        private static string GatherActFor(Gathering g, Gatherer m)
        {
            if (g.Kind == Encounters.RingLeader)
                return m.Lead ? "sing" : ExecKneel;
            if (g.Kind == Encounters.RingSleeper)
                return m.Lead ? "WORLD_HUMAN_SLEEP_GROUND_ARM" : ExecKneel;
            if (g.Kind == Encounters.RingProcession)
                return ExecKneel;   // once they've come to it
            return GatherActs[Voice.Hash(g.StartedAt + "|act") % GatherActs.Length];
        }

        /// A torch in the hand for the walk, after dark. The game gives its own night people torches; on these it isn't proven, and without one they simply walk.
        private void GatherTorch(int h)
        {
            if (!GatherDark())
                return;
            try
            {
                uint torch = Natives.Hash("WEAPON_MELEE_TORCH");
                WEAPON.GIVE_WEAPON_TO_PED(h, torch, 1, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);
                WEAPON.SET_CURRENT_PED_WEAPON(h, torch, true, 0, false, false);
            }
            catch (Exception ex)
            {
                Log.Detail("ENCOUNTER gathering: no torch: " + ex.Message);
            }
        }

        /// A procession on its way: each sets off in turn for their own place in the ring, and when they're there it is a ring.
        private void GatherWalk(Gathering g, List<Gatherer> live, Ped me, int now, float nearest)
        {
            if (live.Any(m => ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(m.Ped.Handle, me.Handle, true, false) || PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, m.Ped.Handle)) || (PED.IS_PED_SHOOTING(me.Handle) && nearest < 40f))
            {
                Scatter(g, live, me, now);
                return;
            }
            bool there = true;
            foreach (Gatherer m in live)
            {
                float x = g.Centre.X + m.X, y = g.Centre.Y + m.Y;
                if (m.GoAt != 0)
                {
                    there = false;
                    if (now >= m.GoAt)
                    {
                        m.GoAt = 0;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Ped.Handle, x, y, Natives.GroundZ(x, y, g.Centre.Z), 1f, -1, 0.8f, 0, 0f);
                    }
                    continue;
                }
                Vector3 p = m.Ped.Position;
                if (Math.Abs(p.X - x) > 2.2f || Math.Abs(p.Y - y) > 2.2f)
                    there = false;
            }
            if (now >= g.NextMurmur && nearest < 25f)
            {
                g.NextMurmur = now + 10000 + _rng.Next(6000);
                Gatherer m = live[_rng.Next(live.Count)];
                Feed.Say("One of them, low", Encounters.Pick(Encounters.GatherMurmurs, "murmur|" + now / 10000), Speech.When(m.Ped.Handle, "hum"));
            }
            if (there || now - g.PhaseAt > 120000)
            {
                foreach (Gatherer m in live)
                {
                    float hx = g.Centre.X - m.Ped.Position.X, hy = g.Centre.Y - m.Ped.Position.Y;
                    ENTITY.SET_ENTITY_HEADING(m.Ped.Handle, Encounters.Heading(hx, hy));
                    GatherAct(m);
                }
                g.Phase = "stand";
                g.PhaseAt = now;
                Log.Write("ENCOUNTER gathering: the line has come to the ground, and it's a ring");
            }
        }

        /// What goes on in a ring with something in the middle, and the face you know in it.
        private void GatherKindStep(Gathering g, List<Gatherer> live, Ped me, int now, float nearest, float toCentre)
        {
            if (!g.KnownSaid && nearest < 18f)
            {
                Gatherer known = live.FirstOrDefault(x => x.R != null && GatherMet(x.R) && x.Ped.Position.DistanceTo(me.Position) < 16f);
                if (known != null)
                {
                    g.KnownSaid = true;
                    Feed.Say(known.R.Name, Encounters.GatherKnowsYou(_engine.KnowsYourName(known.R.Id) ? _engine.Player.FirstName : null, known.R.Id + "|" + g.StartedAt), Speech.When(known.Ped.Handle, "hum"), known.Ped.Handle, toYou: true);
                    Log.Write("ENCOUNTER gathering: " + known.R.Name + ", someone you've met, is in the ring and says so");
                }
            }
            Gatherer lead = live.FirstOrDefault(x => x.Lead);
            if (lead == null)
                return;
            string who = lead.R != null ? lead.R.Name : "The one in the middle";
            if (g.Kind == Encounters.RingLeader && nearest < 35f)
            {
                if (now >= g.NextWord)
                {
                    g.NextWord = now + 9000 + _rng.Next(4000);
                    g.AnswerAt = now + 3200;
                    Feed.Say(who, Encounters.Pick(Encounters.GatherSermon, "sermon|" + g.StartedAt + "|" + g.Words++), Speech.When(lead.Ped.Handle, "hum"), lead.Ped.Handle);
                }
                if (g.AnswerAt != 0 && now >= g.AnswerAt)
                {
                    g.AnswerAt = 0;
                    foreach (Gatherer m in live)
                        if (!m.Lead)
                            Speech.SayAny(m.Ped.Handle, Speech.Hum, 0);
                    Feed.Say("All of them, low", Encounters.Pick(Encounters.GatherResponse, "answer|" + g.StartedAt + "|" + g.Words));
                }
            }
            if (g.Kind == Encounters.RingSleeper && !g.Woken)
            {
                if (now >= g.NextWord && nearest < 30f)
                {
                    g.NextWord = now + 8000 + _rng.Next(3000);
                    Feed.Say("One of them, low", Encounters.Pick(Encounters.GatherCoax, "coax|" + g.StartedAt + "|" + g.Words++), Speech.When(live[_rng.Next(live.Count)].Ped.Handle, "hum"));
                }
                if (toCentre < 6f || (g.Phase == "watched" && now - g.PhaseAt > 18000))
                {
                    g.Woken = true;
                    int you = me.Handle;
                    ScenarioExit.SoftUp(lead.Ped.Handle, x => TASK.TASK_TURN_PED_TO_FACE_ENTITY(x, you, 4000, 0f, 0f, 0f));
                    Feed.Say(who, Encounters.Pick(Encounters.GatherWakes, "woke|" + g.StartedAt), Speech.When(lead.Ped.Handle, "hum"), lead.Ped.Handle);
                    if (lead.R != null)
                        _engine.Awaken(_engine.PersonOfResident(lead.R), Bridge.Now());   // what they came for: one more awake
                    Log.Write("ENCOUNTER gathering: the one in the middle woke" + (lead.R != null ? " (" + lead.R.Name + ", a stage further)" : ""));
                }
            }
        }

        // ---- the circle answers you (FIVEDAY2 item 10; Core/GroupActs.cs)
        private int _circleModel, _circleRules;
        private string _circleLast;

        /// You've stood in the middle: what the ring does next, asked of the model if it's on; the rules' choice otherwise, when the moment comes.
        private void CircleAsk(Gathering g, List<Gatherer> live, int now)
        {
            if (!_cfg.CircleAnswers)
                return;
            g.AskMenu = GroupActs.RingMenu(g.Kind == Encounters.RingSleeper && !g.Woken && live.Any(x => x.Lead));
            g.AskDeadline = now + GroupActs.RingWaitMs;
            if (!PicksOn)
                return;
            LanguageRequest r = GroupActs.RingRequest(Voice.NowYear, CLOCK.GET_CLOCK_HOURS(), live.Count, _placeTown, g.Kind, g.AskMenu);
            string key = "circle|" + g.StartedAt;
            if (_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = "the circle", PersonId = "circle", Request = r, AskedAtMs = Environment.TickCount }))
            {
                g.AskKey = key;
                g.AskRequest = r;
            }
        }

        /// The answer, when it's in or the wait's up. True on the frame the ring acts.
        private bool CircleAnswerStep(Gathering g, List<Gatherer> live, Ped me, int now)
        {
            string answer = null;
            long ms = 0;
            bool came = g.AskKey != null && _engine.LanguageAnswers.TakePick(g.AskKey, out answer, out ms);
            if (!came && now < g.AskDeadline && now >= g.AskDeadline - GroupActs.RingWaitMs)
                return false;
            g.Answered = true;
            string thought = null, seed = "circle|" + g.StartedAt;
            int idx = came ? Language.ParseThinkPick(answer, g.AskMenu.Count, out thought) : -1;
            string what = idx >= 0 ? g.AskMenu[idx] : GroupActs.RingRules(g.AskMenu, seed);
            if (idx >= 0)
                _circleModel++;
            else
                _circleRules++;
            _circleLast = GroupActs.RingWords(what) + (idx >= 0 ? " (the model)" : " (the rules)");
            Log.Write("ENCOUNTER gathering: the ring answers you: " + _circleLast);
            CircleDo(g, live, me, what, thought, seed, now);
            return true;
        }

        private void CircleDo(Gathering g, List<Gatherer> live, Ped me, string what, string thought, string seed, int now)
        {
            int you = me.Handle;
            Vector3 at = me.Position;
            g.ResumeAt = 0;
            switch (what)
            {
                case GroupActs.RingClose:
                    for (int i = 0; i < live.Count; i++)
                    {
                        double a = 2 * Math.PI * i / live.Count;
                        Vector3 to = at + new Vector3((float)Math.Cos(a) * 1.9f, (float)Math.Sin(a) * 1.9f, 0f);
                        int h = live[i].Ped.Handle;
                        ScenarioExit.SoftUp(h, x => TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(x, to.X, to.Y, to.Z, Natives.Brisk, -1, 0.3f, 0, 0f));
                        TASK.TASK_LOOK_AT_ENTITY(h, you, -1, 0, 51, 0);
                    }
                    g.Moving = false;   // their own walk in, not the creeping that stops when you look
                    g.NextCreep = now + 60000;
                    break;
                case GroupActs.RingForward:
                {
                    Gatherer m = live.FirstOrDefault(x => x.Lead) ?? live.OrderBy(x => x.Ped.Position.DistanceTo(at)).First();
                    int h = m.Ped.Handle;
                    Vector3 to = m.Ped.Position + (at - m.Ped.Position).Normalized * 1.4f;
                    ScenarioExit.SoftUp(h, x => TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(x, to.X, to.Y, to.Z, 1f, -1, 0.4f, 0, 0f));
                    TASK.TASK_LOOK_AT_ENTITY(h, you, -1, 0, 51, 0);
                    string line = thought != null && g.AskRequest != null && Language.Check(g.AskRequest, thought) == null ? thought : GroupActs.ForwardLine(seed);
                    Feed.Say(m.R != null ? m.R.Name : "One of them", line, Speech.When(h, "hum", you), h, toYou: true);
                    break;
                }
                case GroupActs.RingKneel:
                    foreach (Gatherer m in live)
                    {
                        int h = m.Ped.Handle;
                        ScenarioExit.SoftUp(h, x => TASK.TASK_START_SCENARIO_IN_PLACE_HASH(x, Natives.Hash(ExecKneel), -1, true, 0u, -1f, false));
                        TASK.TASK_LOOK_AT_ENTITY(h, you, -1, 0, 51, 0);
                    }
                    g.NextCreep = now + 60000;   // they've stopped coming: they've knelt
                    break;
                case GroupActs.RingWake:
                {
                    Gatherer lead = live.FirstOrDefault(x => x.Lead);
                    if (lead == null)
                        break;
                    g.Woken = true;
                    ScenarioExit.SoftUp(lead.Ped.Handle, x => TASK.TASK_TURN_PED_TO_FACE_ENTITY(x, you, 4000, 0f, 0f, 0f));
                    Feed.Say(lead.R != null ? lead.R.Name : "The one in the middle", Encounters.Pick(Encounters.GatherWakes, "woke|" + g.StartedAt), Speech.When(lead.Ped.Handle, "hum"), lead.Ped.Handle);
                    if (lead.R != null)
                        _engine.Awaken(_engine.PersonOfResident(lead.R), Bridge.Now());
                    break;
                }
            }
        }

        private string CircleStatus()
        {
            return (_cfg.CircleAnswers ? "on" : "off ([Encounters] CircleAnswers)") + ": " + _circleModel + " by the model, " + _circleRules + " by the rules this session" + (_circleLast != null ? "; last: " + _circleLast : "");
        }

        private void Scatter(Gathering g, List<Gatherer> live, Ped me, int now)
        {
            g.Phase = "scatter";
            g.PhaseAt = now;
            g.Moving = false;
            foreach (Gatherer m in live)
            {
                int h = m.Ped.Handle;
                int you = me.Handle;
                bool brave = Bravery.Holds(_cfg.Bravery, "scatter|" + h);
                // up out of their act Rockstar's way (a bare clear left the kneeling kneeling), the run the same tick
                ScenarioExit.SoftUp(h, x => { if (brave) TASK.TASK_SMART_FLEE_PED(x, you, 40f, 20000, 0, 1.3f, 0); else TASK.TASK_SMART_FLEE_PED(x, you, 200f, -1, 0, 3f, 0); });
            }
            if (live.Count > 0)
                Feed.Say("The ring breaks", Encounters.Pick(Encounters.GatherScatter, "scatter|" + now / 1000), Speech.When(live[0].Ped.Handle, "scared", me.Handle), toYou: true);
            Log.Write("ENCOUNTER gathering: they scatter");
        }

        private void EndGathering(string why)
        {
            if (_gathering != null)
                foreach (Gatherer m in _gathering.Ring)
                    if (m.Dot != null)
                    {
                        try { if (m.Dot.Exists()) m.Dot.Delete(); } catch { }
                        m.Dot = null;
                    }
            EventDotClear("gathering");
            Gathering g = _gathering;
            _gathering = null;
            if (g == null)
                return;
            if (g.Phase != "spawn")
                Safe("world events", () => RecordEvent("encounter:gathering", null, "the awake", "player", "you", g.Centre, 0, 0, 0, why));
            Ped me = Game.Player.Character;
            foreach (Gatherer m in g.Ring)
            {
                if (m.Ped == null)
                    continue;
                // the living vanish only where nobody sees it; the dead stay where they fell
                bool alive = m.Ped.Exists() && !m.Ped.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(m.Ped.Handle);
                Spawner.Release(m.Ped, alive && (!seen || m.Ped.Position.DistanceTo(me.Position) > 120f));
            }
            if (g.Phase != "spawn" && !g.Test)
            {
                if (!g.Day)
                    MarkDone("gather");   // nowhere to put them doesn't use up the night, nor does a test; one by day doesn't use up the night either
                _store.Counts["enc:gatherAt"] = (int)(Bridge.Now() / 60);   // the next at least GatheringGapHours on
            }
            Log.Write("ENCOUNTER gathering: over (" + why + ")");
        }

        /// One of the ring on the ticker (the add-on's own are skipped there otherwise).
        private void GatherNear(Ped ped, float d)
        {
            Gatherer m = _gathering.Ring.FirstOrDefault(x => x.Ped != null && x.Ped.Handle == ped.Handle);
            if (m == null)
                return;
            long now = Bridge.Now();
            Resident r = m.R;
            PersonState st = r != null ? _store.Find(r.Id) : null;
            _near.Add(new NearThought
            {
                Handle = ped.Handle,
                Key = r != null ? r.Id : "ring:" + ped.Handle,
                Name = r != null ? r.Name : m.Lead ? "The one in the middle" : "One of the ring",
                Distance = d + 2f,
                Text = () => m.Lead && _gathering != null && _gathering.Kind != Encounters.RingPlain ? Encounters.Pick(_gathering.Kind == Encounters.RingLeader ? Encounters.GatherLeadThoughts : Encounters.GatherSleeperThoughts, ped.Handle + "|" + now / 180) : st != null ? Hosts.Thought(_engine.PersonOfResident(r), st, _engine.Player, r.Id + "|" + now / 180)
                                        : Encounters.Pick(_gathering.Day ? Encounters.GatherDayThoughts : Encounters.GatherThoughts, ped.Handle + "|" + now / 180)
            });
        }

        private string GatheringsStatus()
        {
            if (!_cfg.Gatherings)
                return "off";
            int awake = _store.Residents.Count(r => !r.Dead && r.PostId == null && _engine.StageOf(r.Id) >= Awakening.Unravelling);
            int since = GatherHoursSince();
            return _cfg.GatheringChance + "% per roll (" + (_cfg.GatheringHours == "night" ? "00:00 - 03:30, the wilds" : "any hour: 00:00 - 03:30 in the wilds, by day in the open or at a town's edge") +
                   ", " + _cfg.GatheringGapHours + " game hours apart), " + awake + " unravelling or awake to stand in one" +
                   (_gathering != null ? ", out: " + _gathering.Ring.Count + " (" + _gathering.Kind + ", " + _gathering.Phase + (_gathering.Day ? ", by day" : "") + ")" : "") + (DoneToday("gather") ? ", one already tonight" : "") +
                   (since >= 0 ? ", the last " + since + " game hours ago" : "");
        }
    }
}
