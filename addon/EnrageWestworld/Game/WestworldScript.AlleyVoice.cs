// A voice from an alley, the game side (FOURTEENHOUR reserve R15; Core/AlleyVoice.cs; [Encounters] AlleyVoice). In Saint Denis after
// dark, out of doors, once a night at most (a look a minute, a quarter of them), a man is made unseen 20-40 m off (EncounterSpot),
// sat on the stones, and calls for help every few seconds (a caption from the dark and his own recorded voice). Reach him (5 m): real,
// and J is help him up (he gets up Rockstar's way, thanks you and goes) or leave him; bait, and two men are made behind you out of view
// with their revolvers on you: J pay (the money, and they go) or refuse (their fight with you, the game's from there: we let go).
// Ignore him and after six calls the voice stops. Everyone made is given back once it's over (unseen ones vanish).
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
        private sealed class AlleyRun
        {
            public Ped Man;
            public readonly List<Ped> Agents = new List<Ped>();
            public bool Trap, Test, Chose, Fight;
            public string Seed, Phase = "spawn";   // spawn, call, near, demand, after
            public uint Model, AgentModel;
            public int Started, NextCall, Calls, PhaseAt, Cents, Tries, StepAt;
        }

        private AlleyRun _alley;
        private int _alleyLookAt, _alleys, _alleyTraps, _alleyHelped, _alleyPaid, _alleyFought;
        private string _alleyLast;

        /// From the scenes' helpers: a look a minute, and the run.
        private void AlleyStep(int now)
        {
            if (_alley != null)
            {
                AlleyRunStep(_alley, now);
                return;
            }
            if (!AlleyVoice.On || now < _alleyLookAt || _store == null || _placeTown != AlleyVoice.Town || _interiorType != 0)
                return;
            _alleyLookAt = now + 60000;
            if (MISC.GET_MISSION_FLAG() || !AlleyVoice.Hour(CLOCK.GET_CLOCK_HOURS()) || PED.IS_PED_ON_MOUNT(Game.Player.Character.Handle))
                return;
            string key = "alley|" + WalkHome.NightKey(_store.LastGameMin);
            if (_nightDone.Contains(key))
                return;
            string seed = "alley|" + _store.LastGameMin / 60;
            if (!Voice.Chance(seed, AlleyVoice.Percent))
                return;
            _nightDone.Add(key);
            AlleyStart(now, false, seed);
        }

        private void AlleyStart(int now, bool test, string seed)
        {
            string temper = Voice.Pick(seed + "|t", "Plain", "Greedy", "Cold", "Drifter", "Loyal", "Coward", "Hothead");
            bool trap = AlleyVoice.Trap(temper, Voice.Chance(seed + "|need", 40), seed);
            if (test)
                trap = _alleys % 2 == 1;   // the Testing row: real, then bait, by turns
            _alley = new AlleyRun { Trap = trap, Test = test, Seed = seed, Started = now, PhaseAt = now, Model = Spawner.ModelFor(AlleyVoice.Town, false, Voice.Hash(seed)), AgentModel = Spawner.ModelFor(AlleyVoice.Town, false, Voice.Hash(seed + "|ag")) };
            Spawner.Ready(_alley.Model);
            Spawner.Ready(_alley.AgentModel);
            _alleys++;
            Log.Write("ALLEY a voice in the dark, Saint Denis (" + (trap ? "bait" : "real") + ")" + (test ? " (a test)" : ""));
        }

        private void AlleyRunStep(AlleyRun r, int now)
        {
            if (now < r.StepAt)
                return;
            r.StepAt = now + 250;   // a look a quarter second: nothing made a frame
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                AlleyEnd("a mission, or you");
                return;
            }
            if (r.Phase == "spawn")
            {
                if (now - r.Started > 20000 || r.Tries > 10)
                {
                    AlleyEnd("nowhere to put him");
                    return;
                }
                if (!Spawner.Ready(r.Model) || (r.Trap && !Spawner.Ready(r.AgentModel)))
                    return;
                Vector3 at;
                if (!EncounterSpot(me, 20f, 40f, out at))
                {
                    r.Tries++;
                    return;
                }
                Ped m = Spawner.Spawn(r.Model, at, (float)(Voice.Hash(r.Seed) % 360));
                if (m == null)
                {
                    r.Tries++;
                    return;
                }
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(m.Handle, true);
                PED.SET_PED_KEEP_TASK(m.Handle, true);
                WEAPON.REMOVE_ALL_PED_WEAPONS(m.Handle, true, true);
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(m.Handle, Natives.Hash("WORLD_HUMAN_SIT_GROUND"), 240000, true, 0u, -1f, false);   // sat on the stones
                _encounterPeds.Add(m.Handle);
                r.Man = m;
                r.Phase = "call";
                r.NextCall = now + 1500;
                return;
            }
            if (r.Man == null || !r.Man.Exists() || r.Man.IsDead)
            {
                AlleyEnd(r.Man != null && r.Man.Exists() ? "he died" : "he was gone");
                return;
            }
            int h = r.Man.Handle;
            float d = r.Man.Position.DistanceTo(me.Position);
            if (!r.Fight && AlleyWentForYou(r, me))
            {
                if (r.Trap && r.Agents.Count > 0)
                    AlleyFight(r, me, "a blow from you first");   // their fight with you from here, the game's (one order, as when you draw)
                else
                    AlleyEnd("struck by you: handed back");   // let go at once (the rule)
                return;
            }
            switch (r.Phase)
            {
                case "call":
                    if (d <= AlleyVoice.NearMetres)
                    {
                        AlleyReach(r, me, now);
                        return;
                    }
                    if (now >= r.NextCall)
                    {
                        if (r.Calls >= AlleyVoice.MostCalls || d > 70f)
                        {
                            AlleyEnd(r.Calls >= AlleyVoice.MostCalls ? "nobody came: the voice stopped" : "you went on");
                            return;
                        }
                        r.Calls++;
                        r.NextCall = now + AlleyVoice.CallGapMs;
                        SpeechCaption("A voice from the dark", AlleyVoice.Call(r.Calls, r.Seed), h, 3500, false, false);
                        Speech.SayTag(h, "scared", me.Handle);   // his own recorded voice
                    }
                    return;
                case "near":
                case "demand":
                    if (r.Chose || now - r.PhaseAt > AlleyVoice.PromptMs + 3000 && !(_eventMenu != null && _eventMenu.Mode == AlleyVoice.Kind))
                    {
                        if (!r.Chose)
                            AlleyChoose(r.Trap ? "alley:refuse" : "alley:leave");   // no word from you: as if you'd said it
                        r.Phase = "after";
                        r.PhaseAt = now;
                    }
                    if (r.Trap && PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle) && !r.Fight)
                        AlleyFight(r, me, "you drew");   // drawn on: their fight, the game's
                    return;
                case "after":
                    if (now - r.PhaseAt > 15000 || d > 90f || (r.Fight && r.Agents.All(a => !a.Exists() || a.IsDead || !PED.IS_PED_IN_COMBAT(a.Handle, me.Handle))))
                        AlleyEnd(_alleyLast ?? "over");
                    return;
            }
        }

        /// Arthur struck one of them before it was their fight (a blow or a shot, or the man on the stones brawling with him): the
        /// reserve's review found the run would still order them about. Their aim at you is our order, not a fight, so for the two
        /// behind you only a hit from you counts.
        private static bool AlleyWentForYou(AlleyRun r, Ped me)
        {
            if (r.Man != null && r.Man.Exists() && !r.Man.IsDead && (PED.IS_PED_IN_COMBAT(r.Man.Handle, me.Handle) || ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(r.Man.Handle, me.Handle, true, true)))
                return true;
            foreach (Ped a in r.Agents)
                if (a != null && a.Exists() && !a.IsDead && ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(a.Handle, me.Handle, true, true))
                    return true;
            return false;
        }

        private void AlleyReach(AlleyRun r, Ped me, int now)
        {
            int h = r.Man.Handle;
            r.PhaseAt = now;
            if (!r.Trap)
            {
                r.Phase = "near";
                SpeechCaption("The man on the stones", AlleyVoice.Hurt(r.Seed), h, 4500, true, true);
                if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                    PromptSet(AlleyVoice.Kind, new TownEvent { Kind = AlleyVoice.Kind, Town = _placeTown, Extra = "real" }, AlleyVoice.PromptMs);
                return;
            }
            // bait: two men behind you with their revolvers on you
            r.Phase = "demand";
            r.Cents = AlleyVoice.Cents(MONEY._MONEY_GET_CASH_BALANCE(), r.Seed);
            Vector3 back = me.Position - ENTITY.GET_ENTITY_FORWARD_VECTOR(me.Handle) * 9f;
            for (int i = 0; i < 2; i++)
            {
                Vector3 spot;
                Vector3 side = new Vector3((i == 0 ? 1.6f : -1.6f), (i == 0 ? 0.6f : -0.6f), 0f);
                if (!Natives.StandSpot(back + side, me.Position.Z, out spot))
                    spot = back + side;
                Ped a = Spawner.Spawn(r.AgentModel, spot, ENTITY.GET_ENTITY_HEADING(me.Handle));
                if (a == null)
                    continue;
                a.GiveWeapon(eWeapon.RevolverCattleman, 30, eWeaponAttachPoint.PistolR, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(a.Handle, true);
                TASK.TASK_AIM_GUN_AT_ENTITY(a.Handle, me.Handle, 25000, false, 0);
                _encounterPeds.Add(a.Handle);
                r.Agents.Add(a);
            }
            if (r.Agents.Count == 0)
            {
                r.Trap = false;   // nobody could be made behind you: he's only a man on the stones after all
                AlleyReach(r, me, now);
                return;
            }
            _alleyTraps++;
            SpeechCaption("A man behind you", AlleyVoice.Demand(r.Cents, r.Seed), r.Agents[0].Handle, 4500, true, true);
            Speech.SayTag(r.Agents[0].Handle, "threat", me.Handle);
            ScenarioExit.SoftUp(r.Man.Handle, x => TASK.TASK_TURN_PED_TO_FACE_ENTITY(x, me.Handle, 4000, 0f, 0f, 0f));   // the bait gets up
            SpeechCaption("The man on the stones", AlleyVoice.Bait(r.Seed), r.Man.Handle, 3500, true, true);
            if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                PromptSet(AlleyVoice.Kind, new TownEvent { Kind = AlleyVoice.Kind, Town = _placeTown, Extra = "trap", Count = r.Cents }, AlleyVoice.PromptMs);
        }

        /// J's row (or no word, as its default).
        private void AlleyChoose(string id)
        {
            AlleyRun r = _alley;
            if (r == null || r.Chose || (r.Phase != "near" && r.Phase != "demand"))
                return;
            r.Chose = true;
            if (_eventPrompt == AlleyVoice.Kind)
                PromptClear();
            Ped me = Game.Player.Character;
            if (!r.Trap)
            {
                if (id == "alley:help" && r.Man.Exists())
                {
                    ScenarioExit.SoftUp(r.Man.Handle, x => TASK.TASK_TURN_PED_TO_FACE_ENTITY(x, me.Handle, 3000, 0f, 0f, 0f));   // up off the stones, Rockstar's way
                    SpeechCaption("The man on the stones", AlleyVoice.Thanks(r.Seed), r.Man.Handle, 4000, true, true);
                    WanderLater(r.Man.Handle, Game.GameTime + 6000);
                    _alleyHelped++;
                    _alleyLast = "a real one: you helped him up";
                }
                else
                    _alleyLast = "a real one: you left him";
                return;
            }
            if (id == "alley:pay" && r.Cents > 0 && MONEY._MONEY_GET_CASH_BALANCE() >= r.Cents)
            {
                MONEY._MONEY_DECREMENT_CASH_BALANCE(r.Cents);
                if (r.Agents.Count > 0 && r.Agents[0].Exists())
                    SpeechCaption("A man behind you", AlleyVoice.Paid(r.Seed), r.Agents[0].Handle, 3500, true, true);
                foreach (Ped a in r.Agents.Concat(new[] { r.Man }))
                    if (a != null && a.Exists() && !a.IsDead)
                    {
                        TASK.CLEAR_PED_TASKS(a.Handle, true, false);
                        TASK.TASK_SMART_FLEE_PED(a.Handle, me.Handle, 120f, -1, 0, 2f, 0);   // off into the dark with it
                    }
                _alleyPaid++;
                _alleyLast = "bait: you paid $" + (r.Cents / 100);
                return;
            }
            AlleyFight(r, me, "you refused");
        }

        /// Refused or drawn on: their fight with you. One order, then they're the game's: we let go at once.
        private void AlleyFight(AlleyRun r, Ped me, string why)
        {
            r.Fight = true;
            r.Chose = true;
            if (r.Phase != "after")
            {
                r.Phase = "after";
                r.PhaseAt = Game.GameTime;
            }
            foreach (Ped a in r.Agents)
                if (a.Exists() && !a.IsDead)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(a.Handle, false);
                    TASK.TASK_COMBAT_PED(a.Handle, me.Handle, 0, 16);
                    Spawner.LeaveTasks(a.Handle);   // their fight stands when they're given back
                }
            if (r.Man != null && r.Man.Exists() && !r.Man.IsDead)
            {
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Man.Handle, false);
                TASK.TASK_SMART_FLEE_PED(r.Man.Handle, me.Handle, 120f, -1, 0, 3f, 0);   // the bait runs
            }
            _alleyFought++;
            _alleyLast = "bait: " + why + ", and it was their fight with you";
            Log.Write("ALLEY " + _alleyLast);
        }

        private void AlleyEnd(string why)
        {
            AlleyRun r = _alley;
            _alley = null;
            if (r == null)
                return;
            if (_eventPrompt == AlleyVoice.Kind)
                PromptClear();
            Ped me = Game.Player.Character;
            foreach (Ped p in r.Agents.Concat(new[] { r.Man }))
            {
                if (p == null || !p.Exists())
                    continue;
                _encounterPeds.Remove(p.Handle);
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && p.Position.DistanceTo(me.Position) < 120f;
                Spawner.Release(p, !p.IsDead && !seen && !PED.IS_PED_IN_COMBAT(p.Handle, me.Handle));   // the unseen vanish; one fighting you stays the game's
            }
            Log.Write("ALLEY over: " + why);
        }

        /// The Testing row: a voice now, in Saint Denis after dark or not (real, then bait, by turns).
        private string AlleyFire(int now, out string said)
        {
            said = null;
            if (!AlleyVoice.On)
                return "off ([Encounters] AlleyVoice)";
            if (_alley != null)
                return "a voice is calling already";
            if (_placeTown != AlleyVoice.Town)
                return "in Saint Denis only";
            AlleyStart(now, true, "alley|test|" + now);
            said = "a voice calls from the dark (" + (_alley.Trap ? "bait" : "real") + ")";
            return null;
        }

        /// The voice from the dark, or the men behind him: busy for the other features (IsAcknowledging; a loop, asked every frame).
        private bool InAlley(int h)
        {
            AlleyRun r = _alley;
            if (r == null)
                return false;
            if (r.Man != null && r.Man.Handle == h)
                return true;
            for (int i = 0; i < r.Agents.Count; i++)
                if (r.Agents[i] != null && r.Agents[i].Handle == h)
                    return true;
            return false;
        }

        /// F7.
        private string AlleyStatus()
        {
            if (!AlleyVoice.On)
                return "off ([Encounters] AlleyVoice)";
            return _alleys + " voices in the dark (" + _alleyTraps + " bait: paid " + _alleyPaid + ", fought " + _alleyFought + "; helped up " + _alleyHelped + ")" + (_alleyLast != null ? "; last: " + _alleyLast : "");
        }
    }
}
