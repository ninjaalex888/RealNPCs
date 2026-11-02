// Hogtied by the road ([Encounters] Hogtied; Core/Hogtied.cs, re-staged from the game's beat_kidnap_victim). Out of the
// towns from six in the morning to eight at night, once a day: one of the nearest town's regulars, a man or a woman,
// saved with their look, is put out of sight 45-70 m off, tied hand and foot (the game's own hogtie; if it doesn't take
// in 2 s, the execution's kneel, bound by words), two men standing 8-12 m off: bounty hunters for a known thief, a
// creditor's men over a debt, else the region's outlaws. A mark on the map. The captive calls, bargains, curses or
// keeps quiet, by their nature (the first call put to the model with LanguagePicks); the men's leader, as you come
// within 25 m, warns you off, names a price (greet him to pay it), packs up and goes, or (with [Encounters] HogtiedMenDraw,
// off by default; outlaws, and creditors' men at a push; never bounty hunters) draws. Shoot at them, hold a gun on one, or come within 6 m of outlaws and they
// fight: from then they are fair game (outlaws are from the start, as the execution's men). Cut the captive loose with
// the game's own prompt (or, kneeling, stand by them 2 s), and they thank you and go home or run; ride on and the men
// carry them off. Never seen in four minutes: it ends quietly. Turning on Arthur once freed is not built ([Encounters]
// HogtiedTurnsOnYou does not exist yet: For Lex).
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
        private sealed class HogRun
        {
            public string Phase = "spawn";   // spawn, held, fight, freed, leaving
            public Resident R;
            public Ped Captive;
            public readonly List<Ped> Guards = new List<Ped>();
            public readonly List<uint> MenModels = new List<uint>();
            public string Captors, Town, Region, Seed, Name, Lead, Freed;
            public int Price, StartedAt, CheckAt, NextCall, PhaseUntil, NearSince, AimSince, Calls, Tries;
            public bool Tied, Noticed, Offered, Paid, Thought, Test, FairGame, ByYou, Fought;
            public StagedAsk CallAsk, LeadAsk, FreedAsk;
            public StagedMind MC;
            public Blip Mark;
        }

        private const string HogMan = "The one in charge";   // the leader, as the subtitle names him (a stranger)
        private HogRun _hog;
        private int _hogStarted, _hogTied, _hogKneel, _hogFreed, _hogPaid, _hogFought, _hogCarried, _hogModel, _hogMind;
        private readonly Dictionary<string, int> _hogChoices = new Dictionary<string, int>();
        private string _hogLast;

        private string HogtiedNotNow(bool test = false)
        {
            string town = _bridge.TownOnly(Game.Player.Character.Position);
            if (town != null)
                return "you're in " + town + " (only away from the towns)";
            if (test)
                return null;
            int hr = CLOCK.GET_CLOCK_HOURS();
            if (hr < 6 || hr >= 20)
                return "it's " + hr.ToString("00") + ":00 (from 06:00 to 20:00)";
            if (DoneToday("hogtied"))
                return "there was one already today";
            return null;
        }

        /// A regular of that town to tie up: a man or a woman, saved with their look, bound to nobody in the world now.
        private Resident HogtiedCaptive(string town)
        {
            HashSet<Resident> bound = new HashSet<Resident>(_resBind.Values.Select(b => b.R));
            Func<Resident, bool> fits = x => x.Regular && !x.Dead && !x.Fled && x.PostId == null && x.Model != 0 && !bound.Contains(x) && (_exec == null || _exec.R != x);
            List<Resident> pool = _store.Residents.Where(x => fits(x) && x.Town == town).ToList();
            if (pool.Count == 0)
                pool = _store.Residents.Where(fits).ToList();
            if (pool.Count == 0)
                return null;
            string seed = Bridge.Now() / 1440 + "|hog";
            return pool.OrderBy(x => Voice.Hash(x.Id + seed)).First();
        }

        private bool StartHogtied(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string not = HogtiedNotNow(test);
            if (not != null)
            {
                if (test)
                    Hint("Not now: " + not + ".", 5000);
                return false;
            }
            string region = _bridge.TownAt(me.Position);
            string town = Rumours.NearestTown(me.Position.X, me.Position.Y) ?? region;
            Resident r = HogtiedCaptive(town);
            if (r == null)
            {
                Log.Write("ENCOUNTER hogtied: nobody to tie up (no town regulars saved yet)");
                if (test)
                    Hint("Nobody to put there yet: walk round a town first, so it has regulars.", 6000);
                return false;
            }
            Person p = _engine.PersonOfResident(r);
            string kind;
            int stage;
            bool good;
            string worry = InnerLife.WorryNow(p, _store.LastGameMin / 1440, out kind, out stage, out good) && !good ? kind : null;
            bool thief = _store.Ties.Any(t => t.Doer == r.Id && (t.Kind == "pickpocket" || t.Kind == "lift" || t.Kind == "mugging"));
            var h = new HogRun { R = r, Name = r.Name, Town = town ?? r.Town, Region = region, StartedAt = now, Test = test, Seed = r.Id + "|hog|" + Bridge.Now() / 1440 };
            h.Captors = Hogtied.CaptorsFor(thief, worry);
            h.Price = Hogtied.Price(h.Captors, h.Seed);
            h.MC = new StagedMind { Temper = r.Personality, Woman = r.Woman, Worry = worry };
            if (h.Captors == "kidnappers")
            {
                string[] models = Executions.GangModels(Executions.GangOf(region ?? town));
                for (int i = 0; i < 2; i++)
                    h.MenModels.Add(Natives.Hash(models[_rng.Next(models.Length)]));
            }
            else
                for (int i = 0; i < 2; i++)
                    h.MenModels.Add(Spawner.ModelFor(null, false, _rng.Next()));
            h.Lead = Voice.Pick(h.Seed + "lead", "Hothead", "Greedy", "Cold", "Coward", "Proud", "Loyal", null);   // the leader's temper (a stranger: his nature, seeded)
            _hog = h;
            StagedLoad(new[] { Hogtied.StruggleDict }, now);   // the thanks clip and the gestures, asked for now so they are in by the time they are freed
            _hogStarted++;
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);
            Log.Write("ENCOUNTER hogtied: begins" + (test ? " (test)" : "") + ": " + r.Name + " (" + r.Trade + " of " + r.Town + "), held by " +
                      (h.Captors == "bounty" ? "bounty hunters (a known thief)" : h.Captors == "creditor" ? "a creditor's men (" + worry + ")" : "outlaws") + ", their price $" + h.Price);
            return true;
        }

        private void HogtiedStep(int now)
        {
            HogRun h = _hog;
            Ped me = Game.Player.Character;
            if (h.Captive != null && h.Captive.Exists())
                EventDot("hogtied", h.Captive.Position);
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndHogtied(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died");
                return;
            }
            if (h.Phase == "spawn")
            {
                HogtiedSpawn(h, now);
                return;
            }
            if (h.Captive == null || !h.Captive.Exists())
            {
                EndHogtied("they vanished");
                return;
            }
            int ch = h.Captive.Handle;
            List<Ped> men = _hogMen;   // one list, refilled each step (the frame-cost audit, NIGHT4 item 6); no callee keeps it
            men.Clear();
            for (int i = 0; i < h.Guards.Count; i++)
            {
                Ped m = h.Guards[i];
                if (m != null && m.Exists() && !m.IsDead)
                    men.Add(m);
            }
            if (h.FairGame)
                foreach (Ped m in men)
                    _cameAtYouSeen[m.Handle] = now;
            if (h.Captive.IsDead && h.Phase != "leaving")
            {
                bool byYou = PED.GET_PED_SOURCE_OF_DEATH(ch) == me.Handle;
                if (h.Mark != null && h.Mark.Exists())
                    h.Mark.Delete();
                Log.Write("ENCOUNTER hogtied: " + h.Name + " died " + (byYou ? "by your hand" : "in it"));
                h.Phase = "leaving";
                h.PhaseUntil = now + 45000;
                h.Freed = "died";
                return;
            }
            float d = h.Captive.Position.DistanceTo(me.Position);
            if (h.CheckAt != 0 && now >= h.CheckAt)
            {
                h.CheckAt = 0;
                h.Tied = PED.IS_PED_HOGTIED(ch);
                if (h.Tied)
                    _hogTied++;
                else
                {
                    _hogKneel++;
                    TASK.CLEAR_PED_TASKS_IMMEDIATELY(ch, true, true);
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(ch, Natives.Hash(ExecKneel), -1, true, 0u, -1f, false);
                }
                Log.Write("ENCOUNTER hogtied: " + h.Name + (h.Tied ? " is hogtied (the game's own, TASK_KNOCKED_OUT_AND_HOGTIED: seen to take; its escape timer reads " + TASK._GET_HOGTIE_ESCAPE_TIMER(ch).ToString("0") + ")" : " didn't stay hogtied (IS_PED_HOGTIED false at 2 s): kneeling, bound by words"));
            }
            switch (h.Phase)
            {
                case "held":
                case "fight":
                    HogHeld(h, men, d, now);
                    break;
                case "freed":
                    HogFreedStep(h, men, now);
                    break;
                case "leaving":
                    if (now >= h.PhaseUntil || (!HogAnyWithin(men, me.Position, 150f) && d > 90f))
                        EndHogtied(h.Freed ?? "over");
                    break;
            }
        }

        private void HogtiedSpawn(HogRun h, int now)
        {
            if (now - h.StartedAt > 20000 || h.Tries > 8)
            {
                EndHogtied("nowhere hidden to put them");
                return;
            }
            if (!Spawner.Ready(h.R.Model) || h.MenModels.Any(m => !Spawner.Ready(m)))
                return;
            Ped me = Game.Player.Character;
            Vector3 at;
            if (!EncounterSpot(me, 45f, 70f, out at))
            {
                h.Tries++;
                return;
            }
            Ped c = Spawner.Spawn(h.R.Model, at, (float)_rng.NextDouble() * 360f);
            if (c == null || ENTITY.IS_ENTITY_IN_WATER(c.Handle))
            {
                if (c != null)
                    Spawner.Release(c, true);
                h.Tries++;
                return;
            }
            h.Captive = c;
            int ch = c.Handle;
            PED._EQUIP_META_PED_OUTFIT_PRESET(ch, h.R.Outfit, false);   // themselves, as the town knows them
            PED._UPDATE_PED_VARIATION(ch, false, true, true, true, false);
            _resBind[ch] = new ResBind { R = h.R, Model = h.R.Model };
            _encounterPeds.Add(ch);
            StagedHold(c);
            TASK.TASK_KNOCKED_OUT_AND_HOGTIED(ch, 0f, 0);   // the game's own hogtie (unproven here: checked at 2 s)
            TASK._SET_HOGTIE_ESCAPE_TIMER(ch, 600f);   // long enough for you to ride up before they work free by themselves (the game's escape; unproven: the log reads it back)
            h.CheckAt = now + 2000;
            for (int i = 0; i < h.MenModels.Count; i++)
            {
                float ang = (float)(_rng.NextDouble() * Math.PI * 2);
                float dist = 8f + (float)_rng.NextDouble() * 4f;
                Vector3 spot = at + new Vector3((float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist, 0f);
                Vector3 ground;
                if (Natives.StandSpot(spot, at.Z, out ground))
                    spot = ground;
                Ped m = Spawner.Spawn(h.MenModels[i], spot, 0f);
                if (m == null)
                    continue;
                int mh = m.Handle;
                m.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                StagedHold(m);
                _encounterPeds.Add(mh);
                if (i == 0)
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(mh, Director.SmokeHash, -1, true, 0u, -1f, false);
                else
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(mh, ch, -1, 0f, 0f, 0f);
                h.Guards.Add(m);
            }
            if (h.Guards.Count == 0)
            {
                EndHogtied("the men couldn't be put there");
                return;
            }
            if (h.Captors == "kidnappers")
                h.FairGame = true;   // outlaws holding someone tied: dealing with them is self-defence the whole time, as the execution's men
            try
            {
                h.Mark = c.AddBlip(BlipType.WhiteDot);
                h.Mark.Label = "Someone tied up";
            }
            catch
            {
            }
            h.Phase = "held";
            h.NextCall = now + 3000;
            Log.Write("ENCOUNTER hogtied: " + h.Name + " tied up " + (int)at.DistanceTo(me.Position) + " m away, " + h.Guards.Count + " men by them");
        }

        private readonly List<Ped> _hogMen = new List<Ped>();

        /// One of the men is within `metres` of `at` (a loop: no closure a step).
        private static bool HogAnyWithin(List<Ped> men, Vector3 at, float metres)
        {
            foreach (Ped man in men)
                if (man.Position.DistanceTo(at) < metres)
                    return true;
            return false;
        }

        private unsafe void HogHeld(HogRun h, List<Ped> men, float d, int now)
        {
            Ped me = Game.Player.Character;
            int ch = h.Captive.Handle;
            Vector3 meAt = me.Position;
            if (h.Phase == "held")
            {
                ulong slot = 0;
                int aimed = PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot) ? (int)slot : 0;
                bool aiming = false, hurt = false;
                foreach (Ped man in men)   // a List's own enumerator, not Any: no closures a step (the frame-cost audit)
                    if (aimed != 0 && man.Handle == aimed)
                        aiming = true;
                h.AimSince = aiming ? (h.AimSince == 0 ? now : h.AimSince) : 0;
                if (!h.Fought)
                {
                    hurt = men.Count < h.Guards.Count;   // once: a fight over stays over (the damage marks and the dead stay true)
                    for (int k = 0; k < men.Count && !hurt; k++)
                        hurt = ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(men[k].Handle, me.Handle, true, false);
                }
                bool rushed = h.Captors == "kidnappers" && HogAnyWithin(men, meAt, 6f);
                bool drawnOn = aiming && now - h.AimSince > 1200 && h.Captors != "bounty";
                if (men.Count > 0 && (hurt || rushed || drawnOn))
                {
                    HogFight(h, men, now, hurt ? "you fired on them" : rushed ? "you came right at them" : "you drew and kept it on them");
                    return;
                }
            }
            else if (!HogAnyWithin(men, h.Captive.Position, 70f) || now >= h.PhaseUntil)
            {
                Log.Write("ENCOUNTER hogtied: the fight is over; " + h.Name + " is still tied");
                h.Phase = "held";   // nobody left to stop you: cut them loose
            }
            if (!h.Noticed)
            {
                if (d < 40f || (d < 75f && ENTITY.IS_ENTITY_ON_SCREEN(ch) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, ch, 17)))   // the sight test only until they're found, and only in range (the frame-cost audit)
                {
                    h.Noticed = true;
                    StoryEncounterStarted("hogtied", d, h.Test);   // counted once you find them (its end's event isn't counted again)
                    _engine.ExecutionSeen(h.R, Bridge.Now());
                    Log.Write("ENCOUNTER hogtied: you found them, " + (int)d + " m off");
                }
                else if (now - h.StartedAt > (h.Test ? 480000 : 240000))
                {
                    EndHogtied("you never came near; nothing came of it");
                    return;
                }
            }
            // cut loose: the game's own prompt on a hogtied body (the hogtie lifts with you beside them), or by them 2 s kneeling
            bool loose = h.Tied ? !PED.IS_PED_HOGTIED(ch) && d < 3f && h.CheckAt == 0 : d < 2f && h.CheckAt == 0;
            if (!h.Tied && loose)
            {
                h.NearSince = h.NearSince == 0 ? now : h.NearSince;
                loose = now - h.NearSince > 2000;
            }
            else if (!h.Tied)
                h.NearSince = 0;
            if (loose)
            {
                HogRelease(h, men, now);
                return;
            }
            if (h.Tied && !PED.IS_PED_HOGTIED(ch) && h.CheckAt == 0 && d >= 3f)
            {
                // the hogtie wore off with nobody by them (the game's escape timer): they're up and off
                h.ByYou = false;
                HogRelease(h, men, now);
                return;
            }
            if (!h.Noticed)
                return;
            bool menBy = HogAnyWithin(men, h.Captive.Position, 70f);   // one still by them to carry them off
            if (h.Phase == "held" && d > 110f && menBy)
            {
                // you rode on: they carry them off
                _hogCarried++;
                h.Freed = h.Captors == "bounty" ? "delivered up for the bounty" : "carried off";
                Log.Write("ENCOUNTER hogtied: you rode on; " + h.Name + " is " + h.Freed);
                StoryEncounterEnded("hogtied", h.StartedAt, true, h.Test, () => Safe("world events", () => RecordEvent("encounter:hogtied", null, h.Captors == "bounty" ? "bounty hunters" : "two men", h.R.Id, h.Name, h.Captive.Position, ch, ch, 0, h.Freed)));
                EndHogtied(h.Freed, true);
                return;
            }
            if (h.Phase == "held" && d > 110f && !menBy)
            {
                // the men dead or run off and you rode on without cutting them loose: left tied (the game's escape timer, or the
                // clear-up, has them after)
                Log.Write("ENCOUNTER hogtied: you rode on; " + h.Name + " is left tied, the men dead or gone");
                EndHogtied("left tied, the men dead or gone");
                return;
            }
            if (!h.Thought && d < 30f && ENTITY.IS_ENTITY_ON_SCREEN(ch) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, ch, 17))   // (the sight test only until thought)
            {
                h.Thought = true;
                _ticker.Push(h.Name + ", thinking", Hogtied.Thought(h.Captors, h.Seed), "near", ch);
            }
            // their calls, by their own nature (the first asked of the model)
            if (h.CallAsk != null && StagedSettle(h.CallAsk, "hogtied", h.Name, now))
            {
                if (h.CallAsk.Written)
                    h.Thought = true;   // the call's own thought showed (ActThoughts): the first-sight thought stands aside
                HogSay(h, h.CallAsk.Picked, h.CallAsk, now);
                h.CallAsk = null;
            }
            if (now >= h.NextCall && d < 75f && h.CallAsk == null)
            {
                h.NextCall = now + 7000 + _rng.Next(3000);
                Staged.Weights w = Hogtied.Captive(h.MC, h.Captors, d < 30f);
                if (h.Calls == 0)
                    h.CallAsk = StagedChoose(ch, "captive", w, h.Seed + "c0", id => Hogtied.Words("captive", id, Person.FirstOf(h.Name)), Hogtied.Now("captive", Person.FirstOf(h.Name), h.Captors), null, now, 0, h.Name);
                else
                    HogSay(h, Staged.Choose(w, h.Seed + "c" + h.Calls), null, now);
                h.Calls++;
            }
            // the men's leader, as you come near
            if (h.Phase == "held" && men.Count > 0 && d < 25f && h.LeadAsk == null && h.Offered == false)
            {
                Ped lead = men[0];
                Staged.Weights w = Hogtied.Captor(new StagedMind { Temper = h.Lead }, h.Captors, _cfg.HogtiedMenDraw);   // off: never unprovoked
                string mind = Staged.Choose(w, h.Seed + "lead");
                h.LeadAsk = new StagedAsk { Handle = lead.Handle, Mind = mind, Picked = mind, Done = true, Ids = Staged.Offer(w, mind, Language.MostOptions) };   // a stranger: his seeded nature, never the model's
                h.Offered = true;
                HogLead(h, men, mind, now);
            }
        }

        private void HogSay(HogRun h, string pick, StagedAsk a, int now)
        {
            int ch = h.Captive.Handle;
            Ped me = Game.Player.Character;
            HogCount("captive " + pick, a);
            if (a != null)
                Log.Write("ENCOUNTER hogtied: " + h.Name + " chose " + pick + " (" + By(a) + ")");
            else if (TurnThoughtFor(ch, h.Name, h.R != null ? h.R.Id : null, "captive", pick, null, now))
                h.Thought = true;   // a later call, their own nature's draw: its reason over them (ActThoughts), and no first-sight thought on top
            string line = Hogtied.FromCaptive(pick, h.Captors, h.Seed + h.Calls);
            if (line == null)
                return;   // quiet, in fear
            Speech.SayAny(ch, pick == "curse" ? Speech.Angry : pick == "call" ? Speech.Scared : Speech.Plead, 0);
            Feed.Say(h.Name, line, null, ch, toYou: pick == "call");
        }

        private void HogLead(HogRun h, List<Ped> men, string pick, int now)
        {
            Ped me = Game.Player.Character;
            Ped lead = men[0];
            int lh = lead.Handle;
            HogCount("captor " + pick, null);
            Log.Write("ENCOUNTER hogtied: the men's leader chose " + pick + " (mind; " + h.Captors + ")");
            TurnThoughtFor(lh, HogMan, null, "captor", pick, null, now);   // his reason, over his head (ActThoughts; a stranger: the frame guard only)
            TASK.CLEAR_PED_TASKS(lh, false, false);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(lh, me.Handle, 2000, 0f, 0f, 0f);
            switch (pick)
            {
                case "warn":
                    Feed.Say(HogMan, Hogtied.Warn(h.Captors, h.Seed), Speech.When(lh, "warned", me.Handle), lh, toYou: true);
                    break;
                case "bribe":
                    Feed.Say(HogMan, Hogtied.Bribe(h.Captors, h.Price, h.Seed), Speech.When(lh, "threat", me.Handle), lh, toYou: true);
                    Hint("Greet him to pay $" + h.Price + " and they're yours to cut loose.", 7000);
                    break;
                case "leave":
                    Feed.Say(HogMan, Hogtied.Leave(h.Seed), Speech.When(lh, "storm", me.Handle), lh, toYou: true);
                    HogMenGo(h, men);
                    break;
                default:
                    Feed.Say(HogMan, Hogtied.Fight(h.Seed), Speech.When(lh, "final", me.Handle), lh, toYou: true);
                    HogFight(h, men, now, "their leader drew");
                    break;
            }
        }

        /// The player greeted someone: the leader, with a price named, is paid (if the cash is there).
        private bool HogtiedGreeted(int handle)
        {
            HogRun h = _hog;
            if (h == null || h.Phase != "held" || h.Paid || h.LeadAsk == null || h.LeadAsk.Picked != "bribe" || !h.Guards.Any(m => m != null && m.Handle == handle))
                return false;
            Ped me = Game.Player.Character;
            int cash = MONEY._MONEY_GET_CASH_BALANCE();
            if (cash < h.Price * 100)
            {
                Feed.Say(HogMan, "That ain't $" + h.Price + ". Come back when it is.", Speech.When(handle, "no", me.Handle), handle, toYou: true);
                return true;
            }
            if (!h.Test)
                MONEY._MONEY_DECREMENT_CASH_BALANCE(h.Price * 100);
            h.Paid = true;
            _hogPaid++;
            Feed.Say(HogMan, Hogtied.Paid(h.Seed), Speech.When(handle, "yes", me.Handle), handle, toYou: true);
            Hint("Paid $" + h.Price + (h.Test ? " (a test: nothing taken)" : "") + ". Cut them loose.", 6000);
            Log.Write("ENCOUNTER hogtied: you paid $" + h.Price + (h.Test ? " (a test: nothing taken)" : "") + "; the men go");
            HogMenGo(h, h.Guards.Where(m => m != null && m.Exists() && !m.IsDead).ToList());
            return true;
        }

        private void HogMenGo(HogRun h, List<Ped> men)
        {
            Ped me = Game.Player.Character;
            foreach (Ped m in men)
            {
                StagedLetGo(m, false);
                TASK.CLEAR_PED_TASKS(m.Handle, false, false);
                Vector3 away = m.Position + (m.Position - me.Position).Normalized * 120f;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Handle, away.X, away.Y, away.Z, Natives.Brisk, -1, 1f, 0, 0f);
            }
        }

        private void HogFight(HogRun h, List<Ped> men, int now, string why)
        {
            Ped me = Game.Player.Character;
            foreach (Ped m in men)
            {
                _staged.Remove(m.Handle);
                MakeHostile(m, now);
                TASK.TASK_COMBAT_PED(m.Handle, me.Handle, 0, 16);
            }
            h.FairGame = true;   // they fight: fair game from now
            h.Fought = true;
            h.Phase = "fight";
            h.PhaseUntil = now + 180000;
            _hogFought++;
            Log.Write("ENCOUNTER hogtied: " + why + "; " + men.Count + " men fight you");
        }

        private void HogRelease(HogRun h, List<Ped> men, int now)
        {
            Ped me = Game.Player.Character;
            int ch = h.Captive.Handle;
            h.ByYou = h.ByYou || h.Captive.Position.DistanceTo(me.Position) < 3.5f;
            _hogFreed++;
            if (h.Mark != null && h.Mark.Exists())
                h.Mark.Delete();
            if (!h.Tied)
            {
                TASK.CLEAR_PED_TASKS(ch, false, false);   // up from the kneel
                Spawner.StandUp(ch);
            }
            Log.Write("ENCOUNTER hogtied: " + h.Name + " is loose" + (h.ByYou ? " (you cut them free)" : " (they worked free)"));
            // the men still there and unpaid: outlaws and creditors' men turn on you; bounty hunters curse it and go
            if (!h.Paid && h.Phase == "held" && men.Count > 0 && men.Any(m => m.Position.DistanceTo(me.Position) < 40f))
            {
                if (h.Captors == "bounty")
                {
                    Feed.Say(HogMan, "That was our claim. You'll answer for that someday.", Speech.When(men[0].Handle, "storm", me.Handle), men[0].Handle, toYou: true);
                    HogMenGo(h, men);
                }
                else
                    HogFight(h, men, now, "you cut " + Person.FirstOf(h.Name) + " loose under their noses");
            }
            h.FreedAsk = StagedChoose(ch, "freed", Hogtied.Freed(h.MC, h.Captors), h.Seed + "f", id => Hogtied.Words("freed", id, Person.FirstOf(h.Name)), Hogtied.Now("freed", Person.FirstOf(h.Name), h.Captors), null, now, 0, h.Name);
            h.Phase = "freed";
            h.PhaseUntil = now + 2500;
        }

        private void HogFreedStep(HogRun h, List<Ped> men, int now)
        {
            if (now < h.PhaseUntil || !StagedSettle(h.FreedAsk, "hogtied freed", h.Name, now))
                return;
            Ped me = Game.Player.Character;
            int ch = h.Captive.Handle;
            string pick = h.FreedAsk.Picked;
            HogCount("freed " + pick, h.FreedAsk);
            Log.Write("ENCOUNTER hogtied: " + h.Name + ", freed, chose " + pick + " (" + By(h.FreedAsk) + ")");
            _staged.Remove(ch);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(ch, false);
            PED.SET_PED_KEEP_TASK(ch, false);
            if (PED.IS_PED_HOGTIED(ch))
            {
                h.PhaseUntil = now + 1500;   // still on the ground: wait for the rope to be off
                h.FreedAsk.Done = true;
                return;
            }
            if (pick == "thank" && h.ByYou)
            {
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(ch, me.Handle, 2500, 0f, 0f, 0f);
                TASK.TASK_LOOK_AT_ENTITY(ch, me.Handle, 8000, 0, 51, 0);
                StagedClip(ch, Hogtied.StruggleDict, Hogtied.ThanksClip, "thanks", h.R.Woman, h.Seed, now);
                Feed.Say(h.Name, Hogtied.Thanks(h.Captors, h.Seed), Speech.When(ch, "thanks", me.Handle), ch, toYou: true);
            }
            else
            {
                Feed.Say(h.Name, Hogtied.Run(h.Seed), Speech.When(ch, "scared", me.Handle), ch, toYou: true);
                BraveRelease(ch);
                TASK.TASK_SMART_FLEE_PED(ch, men.Count > 0 ? men[0].Handle : me.Handle, 120f, -1, 0, 3f, 0);
            }
            if (h.ByYou)
            {
                Vector3 p = h.Captive.Position;
                var plan = new Executions.Plan { Reason = "hogtied", Boss = h.Captors == "bounty" ? "the bounty hunters" : h.Captors == "creditor" ? "the men sent over a debt" : "the outlaws" };
                _engine.Rescued(h.R, plan, h.Region ?? h.Town, ZoneAt(p), p.X, p.Y, p.Z, Bridge.Now());
            }
            h.Freed = h.ByYou ? "freed by you, " + (pick == "thank" ? "thanked you" : "ran") : "worked free";
            StoryEncounterEnded("hogtied", h.StartedAt, true, h.Test, () => Safe("world events", () => RecordEvent("encounter:hogtied", null, h.Captors == "bounty" ? "bounty hunters" : "two men", h.R.Id, h.Name, h.Captive.Position, ch, ch, 0, "saved; " + h.Freed)));
            if (pick == "thank")
                TASK.TASK_WANDER_STANDARD(ch, 10f, 10);
            h.Phase = "leaving";
            h.PhaseUntil = now + 40000;
        }

        private void HogCount(string what, StagedAsk a)
        {
            int n;
            _hogChoices[what] = _hogChoices.TryGetValue(what, out n) ? n + 1 : 1;
            if (a != null && a.ByModel)
                _hogModel++;
            else
                _hogMind++;
        }

        private void EndHogtied(string why, bool recorded = false)
        {
            EventDotClear("hogtied");
            HogRun h = _hog;
            _hog = null;
            if (h == null)
                return;
            Ped me = Game.Player.Character;
            try
            {
                if (h.Mark != null && h.Mark.Exists())
                    h.Mark.Delete();
            }
            catch
            {
            }
            foreach (Ped p in new[] { h.Captive }.Concat(h.Guards))
            {
                if (p == null)
                    continue;
                _staged.Remove(p.Handle);
                if (p != h.Captive && !h.FairGame || p.Exists() && !p.IsDead && !ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(p.Handle, me.Handle, true, false))
                    _cameAtYouSeen.Remove(p.Handle);   // a handle the game may give someone else; one you fought stays judged as he started it
                bool alive = p.Exists() && !p.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle);
                bool enrageKnows = alive && _bridge != null && _bridge.Looked(p.Handle);
                Spawner.Release(p, alive && !enrageKnows && (!seen || p.Position.DistanceTo(me.Position) > 120f));
            }
            if (h.Phase != "spawn" && !h.Test)
                MarkDone("hogtied");
            _hogLast = h.Name + ": " + why + (h.Test ? " (a test)" : "");
            Log.Write("ENCOUNTER hogtied: over (" + why + ")");
        }

        private string HogtiedStatus()
        {
            if (!_cfg.Hogtied)
                return "off ([Encounters] Hogtied)";
            return _cfg.HogtiedChance + "% (06:00-20:00, away from the towns)" + (DoneToday("hogtied") ? ", done today" : "") + "; " + _hogStarted + " started (hogtied " + _hogTied + ", kneeling instead " + _hogKneel +
                   "), freed " + _hogFreed + ", paid " + _hogPaid + ", fought " + _hogFought + ", carried off " + _hogCarried + "; choices " +
                   (_hogChoices.Count == 0 ? "none yet" : string.Join(", ", _hogChoices.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) + " (the model's " + _hogModel + ", the mind's " + _hogMind + ")" +
                   (_hog != null ? "; going on: " + _hog.Name + " (" + _hog.Phase + ", " + _hog.Captors + ")" : "") + (_hogLast != null ? "; last: " + _hogLast : "");
        }
    }
}
