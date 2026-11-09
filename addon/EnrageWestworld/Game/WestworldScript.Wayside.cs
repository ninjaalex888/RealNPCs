// Things to come across out on the road, in the game (FIVEDAY2 items 16-20; the table is Core/Wayside*.cs). Rolled like the other encounters, at most one
// out at a time, none in the towns. The cast is put out of your sight 45-70 m off, as the encounters always have been. Come near and the beats play on
// their clock; within 10 m the J menu has whatever can be done; draw on them and they run or come at you, as the kind says; ride on and it ends
// by itself, and they go back to the game. A fight is fists only and nobody dies of it. Testing rows "wayside:<id>"; F7 "TEST wayside:"; log "WAYSIDE".
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
        private sealed class WaysideRun
        {
            public WaysideKind K;
            public string Phase = "spawn";   // spawn, stand, play, fight, after, walk
            public Vector3 At;
            public float Facing;
            public Ped[] Cast;
            public uint[] Models;
            public int StartedAt, NoticedAt, PhaseUntil, Beat, Tries;
            public bool Noticed, Test, Chose, Fought;
            public string Outcome = "you rode on";
            public Blip Mark;
            public TownEvent Ask;
            public string Town;
        }

        private const int WaysideFightMs = 22000, WaysideAfterMs = 12000, WaysideRepeatDays = 2;
        private WaysideRun _wayside;
        private string _waysideForce;
        private int _waysideCount;
        private string _waysideLast;

        private bool IsWaysidePed(int h)
        {
            WaysideRun w = _wayside;
            if (w == null || w.Cast == null)
                return false;
            for (int i = 0; i < w.Cast.Length; i++)
                if (w.Cast[i] != null && w.Cast[i].Handle == h)
                    return true;
            return false;
        }

        private string WaysideNotNow()
        {
            Ped me = Game.Player.Character;
            if (_bridge.TownOnly(me.Position) != null)
                return "you are in a town";
            if (_interiorType != 0)
                return "you're indoors";
            return null;
        }

        private bool WaysideLately(string id)
        {
            int day;
            return _store.Counts.TryGetValue("wayside:" + id, out day) && (int)(_store.LastGameMin / 1440) - day < WaysideRepeatDays && day <= (int)(_store.LastGameMin / 1440);
        }

        private bool StartWayside(int now, bool test)
        {
            Ped me = Game.Player.Character;
            Vector3 p = me.Position;
            bool onRoad = PATHFIND.IS_POINT_ON_ROAD(p.X, p.Y, p.Z, 0);
            WaysideKind k = _waysideForce != null ? Wayside.Of(_waysideForce) : Wayside.Pick(CLOCK.GET_CLOCK_HOURS(), onRoad, WaysideLately, _store.LastGameMin + "|" + now);
            _waysideForce = null;
            if (k == null)
                return false;
            string town = Rumours.NearestTown(p.X, p.Y) ?? _bridge.TownAt(p);
            var w = new WaysideRun { K = k, StartedAt = now, Test = test, Town = town, Cast = new Ped[k.Cast.Length], Models = new uint[k.Cast.Length], Facing = (float)_rng.NextDouble() * 360f };
            for (int i = 0; i < k.Cast.Length; i++)
                w.Models[i] = WaysideModel(k.Cast[i], town);
            _wayside = w;
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);   // a big moment: the quiet starts (Pacing.cs)
            Log.Write("WAYSIDE " + k.Id + " begins" + (test ? " (a test)" : "") + " near " + (town ?? "the road"));
            return true;
        }

        private uint WaysideModel(WaysideRole role, string town)
        {
            return Spawner.ModelFor(role.Pool == "town" ? town : null, role.Woman, _rng.Next());
        }

        private void WaysideStep(int now)
        {
            WaysideRun w = _wayside;
            if (MISC.GET_MISSION_FLAG())
            {
                WaysideEnd("a mission started");
                return;
            }
            if (w.Phase == "spawn")
            {
                WaysideSpawn(w, now);
                return;
            }
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            EventDot("wayside", w.At);
            List<Ped> live = w.Cast.Where(c => c != null && c.Exists() && !c.IsDead).ToList();
            if (live.Count == 0)
            {
                WaysideEnd("none of them is left");
                return;
            }
            float d = live.Min(c => c.Position.DistanceTo(pos));
            if (d > 150f)
            {
                WaysideEnd("you rode on");
                return;
            }
            if (w.Phase == "after")
            {
                if (now >= w.PhaseUntil)
                    WaysideEnd(w.Outcome);
                return;
            }
            if (WaysideThreatened(w, live, me, d, now))
                return;
            if (!w.Noticed)
            {
                bool seen = live.Any(c => ENTITY.IS_ENTITY_ON_SCREEN(c.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, c.Handle, 17));
                if (d < Wayside.NearMetres || (d < 70f && seen))
                {
                    w.Noticed = true;
                    w.NoticedAt = now;
                    w.Phase = "play";
                    Log.Write("WAYSIDE " + w.K.Id + ": you've come upon it, " + (int)d + " m off");
                }
                else if (now - w.StartedAt > (w.Test ? 2 * Wayside.UnseenMs : Wayside.UnseenMs))
                {
                    WaysideEnd("you never came near; nothing came of it");
                    return;
                }
                return;
            }
            WaysidePlay(w, live, now, d);
            if (w.Phase == "fight" && now >= w.PhaseUntil)
                WaysideFightOver(w, live, now);
            if (!w.Chose && d <= Wayside.ChoiceMetres && w.K.Choices != null && _eventMenu == null && (_eventPrompt == null || _eventPrompt == Wayside.Kind || now > _eventPromptUntil))
            {
                if (w.Ask == null)
                    w.Ask = new TownEvent { Kind = Wayside.Kind, Town = w.Town, Extra = w.K.Id };
                PromptSet(Wayside.Kind, w.Ask, 1500);
            }
        }

        private void WaysideSpawn(WaysideRun w, int now)
        {
            if (now - w.StartedAt > 20000 || w.Tries > 8)
            {
                WaysideEnd("nowhere hidden to put them");
                return;
            }
            if (w.Models.Any(m => !Spawner.Ready(m)))
                return;
            Ped me = Game.Player.Character;
            Vector3 at;
            if (!EncounterSpot(me, 45f, 70f, out at) || CampInView(at))
            {
                w.Tries++;
                return;
            }
            w.At = at;
            double rad = w.Facing * Math.PI / 180.0;
            Vector3 along = new Vector3((float)Math.Sin(rad), (float)-Math.Cos(rad), 0f), across = new Vector3(along.Y, -along.X, 0f);
            var spots = new Vector3[w.K.Cast.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 s = at + across * w.K.Cast[i].X + along * w.K.Cast[i].Y, ground;
                spots[i] = Natives.StandSpot(s, at.Z, out ground) ? ground : s;
            }
            for (int i = 0; i < w.Cast.Length; i++)
            {
                WaysideRole role = w.K.Cast[i];
                Vector3 toward = role.Face >= 0 ? spots[role.Face] : at;
                Ped p = Spawner.Spawn(w.Models[i], spots[i], Encounters.Heading(toward.X - spots[i].X, toward.Y - spots[i].Y));
                if (p == null || ENTITY.IS_ENTITY_IN_WATER(p.Handle))
                {
                    if (p != null)
                        Spawner.Release(p, true);
                    continue;
                }
                int h = p.Handle;
                w.Cast[i] = p;
                _encounterPeds.Add(h);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                PED.SET_PED_KEEP_TASK(h, true);
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                if (role.Arm == "revolver")
                    p.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                else if (role.Arm == "rifle")
                    WEAPON.GIVE_WEAPON_TO_PED(h, Natives.Hash("WEAPON_RIFLE_SPRINGFIELD"), 30, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);
                if (role.Act == "dead")
                {
                    ENTITY.SET_ENTITY_HEALTH(h, 0, 0);
                    continue;
                }
                if (role.Act != null && role.Act != "stand")
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(role.Act), -1, true, 0u, -1f, false);
                else
                    TASK.TASK_STAND_STILL(h, -1);
            }
            if (w.Cast.All(c => c == null))
            {
                WaysideEnd("they couldn't be put there");
                return;
            }
            try
            {
                w.Mark = w.Cast.First(c => c != null).AddBlip(BlipType.WhiteDot);
                w.Mark.Label = w.K.Mark;
            }
            catch
            {
            }
            w.Phase = "stand";
            Log.Write("WAYSIDE " + w.K.Id + ": " + w.Cast.Count(c => c != null) + " of them, " + (int)at.DistanceTo(me.Position) + " m away");
        }

        /// Drawn on, shot at or hit: they run, or the armed come at you.
        private bool WaysideThreatened(WaysideRun w, List<Ped> live, Ped me, float d, int now)
        {
            if (w.Fought)
                return false;
            bool threat = live.Any(c => ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(c.Handle, me.Handle, true, false) || PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, c.Handle)) || (PED.IS_PED_SHOOTING(me.Handle) && d < 40f);
            if (!threat)
                return false;
            w.Fought = true;
            int you = me.Handle;
            foreach (Ped c in live)
            {
                int h = c.Handle;
                PED.SET_PED_KEEP_TASK(h, false);
                if (w.K.Threat == "fight" && WEAPON.IS_PED_ARMED(h, 4))
                {
                    MakeHostile(c, now);
                    ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, you, 0, 16));
                }
                else
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    ScenarioExit.SoftUp(h, x => TASK.TASK_SMART_FLEE_PED(x, you, 150f, -1, 0, 3f, 0));
                }
            }
            w.Outcome = "you pulled a gun on them";
            w.Phase = "after";
            w.PhaseUntil = now + 45000;
            Log.Write("WAYSIDE " + w.K.Id + ": you pulled a gun on them; they " + (w.K.Threat == "fight" ? "come at you" : "run"));
            return true;
        }

        /// The next of what's said and done, on the clock from when you came upon it.
        private void WaysidePlay(WaysideRun w, List<Ped> live, int now, float d)
        {
            WaysideBeat[] beats = w.K.Beats ?? new WaysideBeat[0];
            while (w.Beat < beats.Length && now - w.NoticedAt >= beats[w.Beat].Ms)
            {
                WaysideBeat b = beats[w.Beat++];
                if (b.Do != null)
                {
                    WaysideDo(w, b, live, now);
                    continue;
                }
                Ped who = b.Who >= 0 && b.Who < w.Cast.Length ? w.Cast[b.Who] : null;
                if (who == null || !who.Exists() || who.IsDead || b.Lines == null || d > 60f)
                    continue;
                int to = b.To == -2 ? Game.Player.Character.Handle : b.To >= 0 && b.To < w.Cast.Length && w.Cast[b.To] != null ? w.Cast[b.To].Handle : 0;
                Feed.Say(w.K.Cast[b.Who].Name, Wayside.Word(b.Lines, w.K.Id + "|" + w.StartedAt + "|" + w.Beat), Speech.When(who.Handle, b.Tag, to), who.Handle, toYou: b.To == -2);
            }
        }

        private void WaysideDo(WaysideRun w, WaysideBeat b, List<Ped> live, int now)
        {
            if (b.Do == "fight" && w.K.SideA != null && w.K.SideB != null)
            {
                foreach (int i in w.K.SideA)
                    WaysideSwing(w, i, w.K.SideB);
                foreach (int i in w.K.SideB)
                    WaysideSwing(w, i, w.K.SideA);
                w.Phase = "fight";
                w.PhaseUntil = now + WaysideFightMs;
                Log.Write("WAYSIDE " + w.K.Id + ": it comes to blows");
            }
        }

        /// Fists: their guns away, up on their feet, and at the nearest of the other side.
        private void WaysideSwing(WaysideRun w, int i, int[] others)
        {
            Ped p = w.Cast[i];
            Ped foe = others.Select(o => w.Cast[o]).FirstOrDefault(c => c != null && c.Exists() && !c.IsDead);
            if (p == null || !p.Exists() || p.IsDead || foe == null)
                return;
            int h = p.Handle, f = foe.Handle;
            WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);
            ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, f, 0, 16));
        }

        /// The fight is over: one side down on the ground, nobody dead of it.
        private void WaysideFightOver(WaysideRun w, List<Ped> live, int now)
        {
            bool aDown = Voice.Chance(w.K.Id + "|" + w.StartedAt + "|down", 50);
            foreach (int i in aDown ? w.K.SideA : w.K.SideB)
            {
                Ped p = w.Cast[i];
                if (p != null && p.Exists() && !p.IsDead)
                {
                    TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                    PED.SET_PED_TO_RAGDOLL(p.Handle, 7000, 9000, 0, false, false, "");
                }
            }
            foreach (int i in aDown ? w.K.SideB : w.K.SideA)
            {
                Ped p = w.Cast[i];
                if (p != null && p.Exists() && !p.IsDead)
                {
                    TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                    TASK.TASK_STAND_STILL(p.Handle, 5000);
                }
            }
            w.Outcome = "the fight ran its course";
            w.Phase = "after";
            w.PhaseUntil = now + WaysideAfterMs;
            Log.Write("WAYSIDE " + w.K.Id + ": the fight is over, side " + (aDown ? "A" : "B") + " down");
        }

        /// Chosen on the J menu (Trials.cs's EventMenuChoose).
        private void WaysideChoose(TownEvent e, string id)
        {
            WaysideRun w = _wayside;
            if (w == null || e == null || w.K.Id != e.Extra)
                return;
            WaysideChoice c = Wayside.Choice(w.K, id);
            if (c == null)
                return;
            w.Chose = true;
            int now = Game.GameTime;
            if (c.Cents > 0)
                MONEY._MONEY_DECREMENT_CASH_BALANCE(c.Cents);
            Ped by = c.By >= 0 && c.By < w.Cast.Length ? w.Cast[c.By] : null;
            string seed = w.K.Id + "|" + w.StartedAt + "|" + id;
            if (c.Said != null && c.Said.Length > 0 && by != null && by.Exists() && !by.IsDead)
                Feed.Say(w.K.Cast[c.By].Name, Wayside.Word(c.Said, seed), Speech.When(by.Handle, "thanks", Game.Player.Character.Handle), by.Handle, toYou: true);
            w.Outcome = WaysideEffect(w, c, seed, now);
            Log.Write("WAYSIDE " + w.K.Id + ": you chose " + id + ": " + w.Outcome);
        }

        /// What a choice does. Null keeps it going as it was.
        private string WaysideEffect(WaysideRun w, WaysideChoice c, string seed, int now)
        {
            switch (c.Effect)
            {
                case "part":
                    foreach (Ped p in w.Cast)
                        if (p != null && p.Exists() && !p.IsDead)
                        {
                            TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                            TASK.TASK_STAND_STILL(p.Handle, 5000);
                        }
                    w.Phase = "after";
                    w.PhaseUntil = now + WaysideAfterMs;
                    return "you parted them";
                case "bet":
                    if (Wayside.BetWon(seed))
                    {
                        MONEY._MONEY_INCREMENT_CASH_BALANCE(200, 0);
                        Hint("Your man won. Two dollars.", 4000);
                        return "you bet and won";
                    }
                    Hint("Your man went down. The dollar's gone.", 4000);
                    return "you bet and lost";
                case "leave":
                    return "you left them to it";
                default:
                    return "you " + c.Effect;
            }
        }

        private void WaysideEnd(string why)
        {
            EventDotClear("wayside");
            WaysideRun w = _wayside;
            _wayside = null;
            if (w == null)
                return;
            try
            {
                if (w.Mark != null && w.Mark.Exists())
                    w.Mark.Delete();
            }
            catch
            {
            }
            if (_eventPrompt == Wayside.Kind)
                PromptClear();
            if (w.Phase != "spawn")
            {
                if (!w.Test)
                    _store.Counts["wayside:" + w.K.Id] = (int)(_store.LastGameMin / 1440);
                if (w.Noticed)
                    Safe("world events", () => RecordEvent("encounter:wayside", null, w.K.Id, null, w.Town, w.At, 0, 0, 0, why));
            }
            Ped me = Game.Player.Character;
            if (w.Cast != null)
                foreach (Ped p in w.Cast)
                {
                    if (p == null)
                        continue;
                    bool alive = p.Exists() && !p.IsDead;
                    bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle);
                    Spawner.Release(p, alive && (!seen || p.Position.DistanceTo(me.Position) > 120f));
                }
            _waysideCount++;
            _waysideLast = w.K.Id + ": " + why;
            Log.Write("WAYSIDE " + w.K.Id + ": over (" + why + ")");
        }

        /// One of the cast, on the ticker.
        private void WaysideNear(Ped ped, float d)
        {
            WaysideRun w = _wayside;
            if (w == null || w.K.Thoughts == null)
                return;
            int i = Array.FindIndex(w.Cast, c => c != null && c.Handle == ped.Handle);
            if (i < 0 || w.Phase == "spawn")
                return;
            long now = Bridge.Now();
            _near.Add(new NearThought
            {
                Handle = ped.Handle, Key = "way:" + ped.Handle, Name = w.K.Cast[i].Name, Distance = d + 2f,
                Text = () => Wayside.Word(w.K.Thoughts, ped.Handle + "|" + now / 180)
            });
        }

        private string WaysideStatus()
        {
            return (_cfg.Wayside ? _cfg.WaysideChance + "% a roll" : "off ([Encounters] Wayside)") + "; " + _waysideCount + " this session" + (_wayside != null ? "; out: " + _wayside.K.Id + " (" + _wayside.Phase + ")" : "") + (_waysideLast != null ? "; last: " + _waysideLast : "");
        }
    }
}
