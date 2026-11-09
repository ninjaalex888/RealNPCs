// An execution on the road (Core/Executions.cs; [Encounters] Executions). Out of the towns, a roll once a day: one of
// the nearest town's regulars (a man, saved with his model and outfit, not walking about as himself just now) is
// spawned out of sight 45-70 m off, kneeling, two men behind him with guns on him: the creditor himself and a hired
// man, two hired men, the man he's feuding with and his brother, or the region's outlaws, as his worry says. A mark on
// the map. Come near and he calls out to you, they warn you off (said to you, so the reason reaches you), and his
// thought shows over his head. Shoot or rush them and they fight you (self-defence, as with the crying woman's men);
// kill or drive them off and he's saved (Engine.Rescued: the town hears, he thinks of it for days). Wait, or ride off,
// and they shoot him (the people ledger notes it by their hand; Engine.Executed: his kin, and the town once he's found).
// Never seen him at all in four minutes: it ends quietly and nobody dies. The test: the settings panel's Encounters
// page, "Stage an execution nearby". Not yet seen in game.
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
        private sealed class Execution
        {
            public string Phase = "spawn";   // spawn, held, fight, last, firing, shot, saved, leaving
            public Resident R, BossR;
            public Executions.Plan Plan;
            public Ped Captive;
            public readonly List<Ped> Men = new List<Ped>();
            public readonly List<uint> MenModels = new List<uint>();
            public Blip Mark;
            public string Town, Region;
            public int StartedAt, NoticedAt, ShootAt, NextLine, PhaseUntil, Tries, AimSince, Lines;
            public bool Noticed, Warned, Called, Thought, Test, LastChance;
            // what can turn it (FIVEDAY2 item 15; Core/Executions.cs)
            public int SpotRetryAt;
            public string Twist = Executions.TwistNone;
            public bool TwistDone, RunnerSent, KinDue, KinSaid, LetGo;
            public Ped Runner, Kin;
            public uint RunnerModel, KinModel;
            public int KinAt, NextMutter;
            public readonly List<Ped> Crowd = new List<Ped>();
            public readonly List<uint> CrowdModels = new List<uint>();
        }

        private const string ExecKneel = "WORLD_HUMAN_GRAVE_MOURNING_KNEEL";
        // 25 s after he's seen (12 once warned): 40 s of the same threats read as a loop (Lex, 22:02)
        private const int ExecWaitMs = 25000, ExecAfterWarnMs = 12000, ExecUnseenMs = 240000;
        private Execution _exec;
        private bool _execRequested;
        private string _execForce;   // the Testing page's choice: balk, reprieve or crowd
        private KeySpec _execKey, _cryingKey, _hermitKey, _gatherKey;
        private int _execCount;
        private string _execLast;

        private bool IsExecutionPed(int h)
        {
            Execution e = _exec;
            return IsStaged(h) || e != null && ((e.Captive != null && e.Captive.Handle == h) || e.Men.Any(m => m != null && m.Handle == h));
        }

        /// Why there can't be one here and now; null when there can.
        private string ExecutionNotNow()
        {
            string town = _bridge.TownOnly(Game.Player.Character.Position);
            if (town != null)
                return "you're in " + town + " (only away from the towns)";
            int hour = CLOCK.GET_CLOCK_HOURS();
            if (hour < 5 || hour >= 22)
                return "it's " + hour.ToString("00") + ":00 (from 05:00 to 22:00)";
            if (DoneToday("execution"))
                return "there was one already today";
            return null;
        }

        /// The road's chance of an execution on this roll: a certainty when men of the nearest town mean to act about now, so a committee is seen if you're near it (TENHOUR item 4).
        private int ExecutionChanceNow()
        {
            Vector3 p = Game.Player.Character.Position;
            return _engine != null && _engine.CommitteeDue(Rumours.NearestTown(p.X, p.Y), Bridge.Now()) ? 100 : _cfg.ExecutionChance;
        }

        /// A regular of that town to put on his knees: a man, saved with his look, not bound to anyone in the world
        /// now; those whose worry gives a reason first. Null if there's nobody.
        private Resident ExecutionCaptive(string town)
        {
            HashSet<Resident> bound = new HashSet<Resident>(_resBind.Values.Select(b => b.R));
            Func<Resident, bool> fits = x => x.Regular && !x.Dead && !x.Fled && x.PostId == null && !x.Woman && x.Model != 0 && !bound.Contains(x);
            List<Resident> pool = _store.Residents.Where(x => fits(x) && x.Town == town).ToList();
            if (pool.Count == 0)
                pool = _store.Residents.Where(fits).ToList();
            if (pool.Count == 0)
                return null;
            string seed = Bridge.Now() / 1440 + "|exec";
            return pool.OrderByDescending(x => _engine.ExecutionPlan(x, town, x.Id + seed).Reason != "witness" ? 1 : 0).ThenBy(x => Voice.Hash(x.Id + seed)).First();
        }

        private void StartExecution(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string region = _bridge.TownAt(me.Position);
            string town = Rumours.NearestTown(me.Position.X, me.Position.Y) ?? region;
            Resident r = ExecutionCaptive(town);
            if (r == null)
            {
                Log.Write("EXECUTION: nobody to put there (no town regulars saved yet)");
                _execForce = null;
                if (test)
                    Hint("Nobody to put there yet: walk round a town first, so it has regulars.", 6000);
                return;
            }
            string seed = r.Id + "|" + Bridge.Now() / 1440;
            Executions.Plan plan = _engine.CommitteePlan(r, town, Bridge.Now(), seed, _execForce == "committee") ?? _engine.ExecutionPlan(r, region ?? town, seed);
            Execution e = new Execution { R = r, Plan = plan, Town = town ?? r.Town, Region = region, StartedAt = now, Test = test };   // Town: nearest where it happens, for the news
            if (plan.BossId != null)
            {
                HashSet<Resident> bound = new HashSet<Resident>(_resBind.Values.Select(b => b.R));
                Resident boss = _store.Residents.FirstOrDefault(x => x.Id == plan.BossId);
                if (boss != null && boss.Regular && !boss.Dead && !boss.Fled && boss.PostId == null && !boss.Woman && boss.Model != 0 && !bound.Contains(boss) && boss != r)
                    e.BossR = boss;   // the man himself, as the town knows him
            }
            if (plan.Captors == "boss" && e.BossR == null)
                plan.Captors = "hired";   // he isn't someone we can put there: he sent men
            if (plan.Captors == "outlaws")
            {
                string[] models = Executions.GangModels(plan.GangKey);
                int n = 2 + (_rng.Next(100) < 30 ? 1 : 0);
                for (int i = 0; i < n; i++)
                    e.MenModels.Add(Natives.Hash(models[_rng.Next(models.Length)]));
            }
            else
            {
                if (e.BossR != null)
                    e.MenModels.Add(e.BossR.Model);
                while (e.MenModels.Count < (plan.Reason == "committee" ? 3 : 2))
                    e.MenModels.Add(Spawner.ModelFor(null, false, _rng.Next()));
            }
            e.Twist = _execForce == "balk" ? Executions.TwistBalk : _execForce == "reprieve" ? Executions.TwistReprieve : Executions.Twist(plan, e.MenModels.Count, seed);
            if (e.Twist == Executions.TwistBalk && e.MenModels.Count < 2)
                e.Twist = Executions.TwistNone;
            if (_execForce == "crowd" || Executions.Crowd(seed))
                for (int i = 2 + _rng.Next(3); i > 0; i--)
                    e.CrowdModels.Add(Spawner.ModelFor(town, _rng.Next(100) < 40, _rng.Next()));
            _execForce = null;
            if (e.Twist == Executions.TwistReprieve)
                e.RunnerModel = Spawner.ModelFor(town, _rng.Next(100) < 30, _rng.Next());
            e.KinDue = Executions.KinCome(seed);
            if (e.KinDue)
                e.KinModel = Spawner.ModelFor(town, _rng.Next(100) < 60, _rng.Next());
            _exec = e;
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);   // a big moment: the quiet starts (Pacing.cs)
            Log.Write("EXECUTION begins" + (test ? " (test)" : "") + ": " + r.Name + " (" + r.Trade + " of " + r.Town + "), " + plan.Reason + " (" + plan.Boss + "), held by " +
                      (plan.Reason == "committee" ? "men of " + e.Town + (e.BossR != null ? ", " + e.BossR.Name + " leading" : "") : e.BossR != null ? e.BossR.Name + " and a hired man" : plan.Captors == "outlaws" ? e.MenModels.Count + " of " + plan.Gang : plan.Captors == "kin" ? "the " + plan.Boss + " men" : "two men " + plan.Boss + " hired"));
        }

        private void ExecutionStep(int now)
        {
            Execution e = _exec;
            if (e.Phase == "spawn")
            {
                ExecutionSpawn(e, now);
                return;
            }
            if (e.Captive != null && e.Captive.Exists())
                EventDot("execution", e.Captive.Position);
            Ped me = Game.Player.Character;
            if (!e.LetGo)
                foreach (Ped m in e.Men)
                    if (m != null && m.Exists() && !m.IsDead)
                        _cameAtYouSeen[m.Handle] = now;   // men holding a man at gunpoint: shooting them is self-defence, while they are
            bool captiveThere = e.Captive != null && e.Captive.Exists();
            bool captiveAlive = captiveThere && !e.Captive.IsDead;
            List<Ped> men = e.Men.Where(m => m != null && m.Exists() && !m.IsDead).ToList();
            float d = e.Captive != null && e.Captive.Exists() ? e.Captive.Position.DistanceTo(me.Position) : 999f;
            if (!captiveThere && e.Phase != "shot" && e.Phase != "leaving")
            {
                EndExecution("he vanished before anyone saw him die");   // not seen to die: nothing comes of it
                return;
            }
            if (!captiveAlive && (e.Phase == "held" || e.Phase == "fight" || e.Phase == "last"))   // ("firing": his death is the shot's)
            {
                ExecutionCaptiveDied(e, now);
                return;
            }
            if (!captiveAlive && e.Phase == "saved")
            {
                Log.Write("EXECUTION " + e.R.Name + ": died after he was saved (the ledger has how)");
                e.Phase = "leaving";
                e.PhaseUntil = now + 30000;
                return;
            }
            switch (e.Phase)
            {
                case "held":
                    ExecutionHeld(e, men, d, now);
                    break;
                case "fight":
                    // saved once none of them is left standing near him, or three minutes on whatever became of them
                    if (men.All(m => m.Position.DistanceTo(e.Captive.Position) > 70f) || now >= e.PhaseUntil)
                        ExecutionSaved(e, now);
                    break;
                case "last":
                    if (now >= e.PhaseUntil)
                        ExecutionShoot(e, men, now);
                    break;
                case "firing":
                    // the gunman shoots him for real (animation, flash, sound); the old bullet only if he still stands
                    if (e.Captive.IsDead)
                        ExecutionDead(e, now);
                    else if (now >= e.PhaseUntil)
                    {
                        ExecutionBullet(e, men);
                        ExecutionDead(e, now);
                    }
                    break;
                case "shot":
                    if (now >= e.PhaseUntil)
                        ExecutionAfter(e, men, now);
                    break;
                case "saved":
                    if (now >= e.PhaseUntil)
                    {
                        if (e.Captive.Exists() && !e.Captive.IsDead)
                            TASK.TASK_WANDER_STANDARD(e.Captive.Handle, 10f, 10);
                        e.Phase = "leaving";
                        e.PhaseUntil = now + 30000;
                    }
                    break;
                case "leaving":
                    if (ExecutionKinStep(e, now))
                        break;
                    if (now >= e.PhaseUntil || men.All(m => m.Position.DistanceTo(me.Position) > 150f) && (e.Captive == null || !e.Captive.Exists() || e.Captive.IsDead || e.Captive.Position.DistanceTo(me.Position) > 80f))
                        EndExecution("over");
                    break;
            }
        }

        private void ExecutionSpawn(Execution e, int now)
        {
            if (now - e.StartedAt > 20000 || e.Tries > 8)
            {
                EndExecution("nowhere hidden to put them");
                return;
            }
            if (!Spawner.Ready(e.R.Model) || e.MenModels.Any(m => !Spawner.Ready(m)) || e.CrowdModels.Any(m => !Spawner.Ready(m)) || (e.RunnerModel != 0 && !Spawner.Ready(e.RunnerModel)))
                return;
            Ped me = Game.Player.Character;
            Vector3 at;
            if (!EncounterSpot(me, 45f, 70f, out at))
            {
                e.Tries++;
                return;
            }
            float heading = (float)_rng.NextDouble() * 360f;
            Ped c = Spawner.Spawn(e.R.Model, at, heading);
            if (c == null || ENTITY.IS_ENTITY_IN_WATER(c.Handle))
            {
                if (c != null)
                    Spawner.Release(c, true);
                e.Tries++;
                return;
            }
            e.Captive = c;
            int h = c.Handle;
            PED._EQUIP_META_PED_OUTFIT_PRESET(h, e.R.Outfit, false);   // himself, as the town knows him
            PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
            Dressed.Note(h);   // not read for a while (Game/Dressed.cs)
            _resBind[h] = new ResBind { R = e.R, Model = e.R.Model };
            _encounterPeds.Add(h);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(ExecKneel), -1, true, 0u, -1f, false);
            double rad = heading * Math.PI / 180.0;
            Vector3 back = new Vector3((float)Math.Sin(rad), (float)-Math.Cos(rad), 0f);   // behind him
            Vector3 side = new Vector3(back.Y, -back.X, 0f);
            for (int i = 0; i < e.MenModels.Count; i++)
            {
                Vector3 spot = at + back * 2.6f + side * ((i - (e.MenModels.Count - 1) / 2f) * 1.6f);
                Vector3 ground;
                if (Natives.StandSpot(spot, at.Z, out ground))
                    spot = ground;
                Ped m = Spawner.Spawn(e.MenModels[i], spot, heading);
                if (m == null)
                    continue;
                int mh = m.Handle;
                if (i == 0 && e.BossR != null)
                {
                    PED._EQUIP_META_PED_OUTFIT_PRESET(mh, e.BossR.Outfit, false);
                    PED._UPDATE_PED_VARIATION(mh, false, true, true, true, false);
                    Dressed.Note(mh);   // not read for a while (Game/Dressed.cs)
                    _resBind[mh] = new ResBind { R = e.BossR, Model = e.BossR.Model };
                }
                m.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(mh, true);
                PED.SET_PED_KEEP_TASK(mh, true);
                TASK.TASK_AIM_GUN_AT_ENTITY(mh, h, -1, false, 0);
                _encounterPeds.Add(mh);
                _cameAtYouSeen[mh] = now;
                e.Men.Add(m);
            }
            if (e.Men.Count == 0)
            {
                EndExecution("the men couldn't be put there");
                return;
            }
            ExecutionCrowdSpawn(e, at, now);
            try
            {
                e.Mark = c.AddBlip(BlipType.WhiteDot);
                e.Mark.Label = "A man in trouble";
            }
            catch
            {
            }
            e.Phase = "held";
            e.NextLine = now + 2500;
            Log.Write("EXECUTION " + e.R.Name + ": on his knees " + (int)at.DistanceTo(me.Position) + " m away, " + e.Men.Count + " men behind him");
        }

        private unsafe void ExecutionHeld(Execution e, List<Ped> men, float d, int now)
        {
            Ped me = Game.Player.Character;
            int ch = e.Captive.Handle;
            bool seen = ENTITY.IS_ENTITY_ON_SCREEN(ch) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, ch, 17);
            // he steps in: a shot at them, a gun on one of them a moment, or right up to them
            ulong slot = 0;   // out-parameters take 8 bytes
            int aimed = PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot) ? (int)slot : 0;
            bool aiming = aimed != 0 && men.Any(m => m.Handle == aimed);
            e.AimSince = aiming ? (e.AimSince == 0 ? now : e.AimSince) : 0;
            if (men.Count < e.Men.Count || men.Any(m => ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(m.Handle, me.Handle, true, false)) ||
                (aiming && now - e.AimSince > 1200) || men.Any(m => m.Position.DistanceTo(me.Position) < 6f))
            {
                ExecutionCrowdRuns(e, me);
                foreach (Ped m in men)
                {
                    MakeHostile(m, now);
                    TASK.TASK_COMBAT_PED(m.Handle, me.Handle, 0, 16);
                }
                e.Phase = "fight";
                e.PhaseUntil = now + 180000;
                Log.Write("EXECUTION " + e.R.Name + ": you stepped in; " + men.Count + " men fight you");
                return;
            }
            if (!e.Noticed)
            {
                if (d < 40f || (d < 75f && seen))
                {
                    e.Noticed = true;
                    e.NoticedAt = now;
                    e.ShootAt = now + ExecWaitMs;
                    _engine.ExecutionSeen(e.R, Bridge.Now());
                    Log.Write("EXECUTION " + e.R.Name + ": you found them, " + (int)d + " m off");
                }
                else if (now - e.StartedAt > (e.Test ? 2 * ExecUnseenMs : ExecUnseenMs))
                {
                    EndExecution("you never came near; nothing came of it");
                    return;
                }
            }
            if (e.Noticed)
            {
                if (!e.Called && d < 60f)
                {
                    e.Called = true;
                    Feed.Say(e.R.Name, Executions.CallsToYou(e.R.Id + now), Speech.When(ch, "scared", me.Handle), ch, toYou: true);
                }
                if (!e.Warned && d < 28f && men.Count > 0)
                {
                    e.Warned = true;
                    e.ShootAt = Math.Min(e.ShootAt, now + ExecAfterWarnMs);
                    Ped m = men[0];
                    Feed.Say(ExecManName(e, m), Executions.WarnYou(e.Plan, e.R.Id + now), Speech.When(m.Handle, "threat", me.Handle), m.Handle, toYou: true);
                }
                if (!e.Thought && d < 30f && seen)
                {
                    e.Thought = true;
                    _ticker.Push(e.R.Name + ", thinking", Executions.During(e.Plan, e.R.Id), "near", ch);
                }
                if (d > 110f)
                    e.ShootAt = Math.Min(e.ShootAt, now);   // he rode on
            }
            if (now >= e.NextLine && men.Count > 0)
            {
                // overheard: the men and him, by turns
                e.NextLine = now + 6000 + _rng.Next(2500);
                string seed = e.R.Id + "|" + e.Lines;
                if (e.Lines++ % 2 == 0)
                {
                    Ped m = men[e.Lines / 2 % men.Count];
                    Feed.Say(ExecManName(e, m), Executions.Threat(e.Plan, seed), Speech.When(m.Handle, "threat", ch), m.Handle);
                }
                else
                    Feed.Say(e.R.Name, Executions.Plead(e.Plan, seed), Speech.When(ch, "plead", men[0].Handle), ch);
            }
            ExecutionMutters(e, now, d);
            if (e.Noticed && !e.TwistDone && ExecutionTwists(e, men, now))
                return;
            if (e.Noticed && !e.LastChance && men.Count > 0 && e.ShootAt - now <= 6000 && d < 90f)
            {
                // the last warning, said to him: it counts down rather than going round again
                e.LastChance = true;
                Ped m = men[0];
                Feed.Say(ExecManName(e, m), Executions.LastChance(e.Plan, e.R.Id), Speech.When(m.Handle, "final", me.Handle), m.Handle, toYou: true);
            }
            if (e.Noticed && now >= e.ShootAt)
            {
                Feed.Say(e.R.Name, Executions.LastWords(e.R.Id), Speech.When(ch, "plead", men.Count > 0 ? men[0].Handle : me.Handle), ch);
                // he tries to get up: out of the kneel and free to fall when he's hit
                PED.SET_PED_KEEP_TASK(ch, false);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(ch, false);
                TASK.CLEAR_PED_TASKS(ch, false, false);
                e.Phase = "last";
                e.PhaseUntil = now + 1800;
            }
        }

        private static string ExecManName(Execution e, Ped m)
        {
            return e.BossR != null && e.Men.Count > 0 && e.Men[0] == m ? e.BossR.Name : "A gunman";
        }

        /// The gunman fires on him: a fight with him alone, so the game plays the shot.
        private void ExecutionShoot(Execution e, List<Ped> men, int now)
        {
            Ped shooter = men.FirstOrDefault();
            if (shooter == null)
            {
                ExecutionBullet(e, men);
                ExecutionDead(e, now);
                return;
            }
            ExecutionCrowdRuns(e, Game.Player.Character);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(shooter.Handle, false);
            TASK.CLEAR_PED_TASKS(shooter.Handle, false, false);
            TASK.TASK_COMBAT_PED(shooter.Handle, e.Captive.Handle, 0, 16);
            e.Phase = "firing";
            e.PhaseUntil = now + 3000;
        }

        /// He still stands three seconds on: the old way, a bullet from the gunman's hand, and down.
        private void ExecutionBullet(Execution e, List<Ped> men)
        {
            int ch = e.Captive.Handle;
            Ped shooter = men.FirstOrDefault();
            if (ENTITY.IS_ENTITY_DEAD(ch))
                return;
            Vector3 head = e.Captive.Position + new Vector3(0f, 0f, 0.55f);
            if (shooter != null)
            {
                Vector3 from = shooter.Position + new Vector3(0f, 0f, 1.3f);
                GunTrigger(shooter.Handle, head, Natives.Hash("WEAPON_REVOLVER_CATTLEMAN"));   // the shot heard: a bullet made in the air is silent (WestworldScript.Gunshot.cs)
                MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(from.X, from.Y, from.Z, head.X, head.Y, head.Z, 500, true, Natives.Hash("WEAPON_REVOLVER_CATTLEMAN"), shooter.Handle, true, false, 1000f, false);
            }
            if (!ENTITY.IS_ENTITY_DEAD(ch))
            {
                ENTITY.SET_ENTITY_HEALTH(ch, 0, shooter != null ? shooter.Handle : ch);
                PED.SET_PED_TO_RAGDOLL(ch, 3000, 3000, 0, false, false, "");
            }
            Log.Write("EXECUTION " + e.R.Name + ": the gunman's shot didn't take, so the old way");
        }

        private void ExecutionDead(Execution e, int now)
        {
            int ch = e.Captive.Handle;
            LedgerNote(ch, e.R.Id, e.R.Name, "resident", "a revolver", "another");
            _engine.Executed(e.R, e.Plan, e.Town, Bridge.Now());
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            if (e.KinDue)
                e.KinAt = now + 18000 + _rng.Next(8000);
            e.Phase = "shot";
            e.PhaseUntil = now + 2500;
            _execCount++;
            _execLast = e.R.Name + ": shot (" + e.Plan.Reason + ")";
            Log.Write("EXECUTION " + e.R.Name + ": they shot him");
        }

        private void ExecutionAfter(Execution e, List<Ped> men, int now)
        {
            Ped me = Game.Player.Character;
            if (men.Count > 0)
                Feed.Say(ExecManName(e, men[0]), Executions.Done(e.Plan, e.R.Id), Speech.When(men[0].Handle, "storm", me.Handle), men[0].Handle);
            foreach (Ped m in men)
            {
                TASK.CLEAR_PED_TASKS(m.Handle, false, false);
                Vector3 away = m.Position + (m.Position - me.Position).Normalized * 150f;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Handle, away.X, away.Y, away.Z, Natives.Brisk, -1, 1f, 0, 0f);
            }
            e.Phase = "leaving";
            e.PhaseUntil = now + 90000;
        }

        private void ExecutionSaved(Execution e, int now)
        {
            Ped me = Game.Player.Character;
            int ch = e.Captive.Handle;
            TASK.CLEAR_PED_TASKS(ch, false, false);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(ch, false);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(ch, me.Handle, 3000, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(ch, me.Handle, 8000, 0, 51, 0);
            Feed.Say(e.R.Name, Executions.Thanks(e.Plan, e.R.Id), Speech.When(ch, "thanks", me.Handle), ch, toYou: true);
            Vector3 p = e.Captive.Position;
            _engine.Rescued(e.R, e.Plan, e.Region ?? e.Town, ZoneAt(p), p.X, p.Y, p.Z, Bridge.Now());
            SavedByYou(ch, e.R.Id, e.R.Name, "saved from the rope");   // theirs for good: fondness to the top, a friend who owes you (WestworldScript.Saved.cs; Lex, 2026-11-03)
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            e.Phase = "saved";
            e.PhaseUntil = now + 8000;
            _execCount++;
            _execLast = e.R.Name + ": saved (" + e.Plan.Reason + ")";
            Log.Write("EXECUTION " + e.R.Name + ": you saved him");
        }

        /// He died before they shot him on purpose: in the fight, by their hand or the player's (the player's is a
        /// killing like any other, the deeds see to that).
        private void ExecutionCaptiveDied(Execution e, int now)
        {
            int ch = e.Captive != null ? e.Captive.Handle : 0;
            bool byYou = ch != 0 && ENTITY.DOES_ENTITY_EXIST(ch) && PED.GET_PED_SOURCE_OF_DEATH(ch) == Game.Player.Character.Handle;
            if (!byYou)
                _engine.Executed(e.R, e.Plan, e.Town, Bridge.Now());
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            Log.Write("EXECUTION " + e.R.Name + ": died " + (byYou ? "by your hand" : "in it") + " (" + e.Phase + ")");
            _execLast = e.R.Name + ": died " + (byYou ? "by your hand" : "in it");
            e.Phase = "leaving";
            e.PhaseUntil = now + 60000;
        }

        /// A few who've come to see, standing off, looking at him.
        private void ExecutionCrowdSpawn(Execution e, Vector3 at, int now)
        {
            int ch = e.Captive.Handle;
            foreach (uint model in e.CrowdModels)
            {
                double a = _rng.NextDouble() * Math.PI * 2;
                float d = 9f + (float)_rng.NextDouble() * 5f;
                Vector3 spot = at + new Vector3((float)Math.Cos(a) * d, (float)Math.Sin(a) * d, 0f), ground;
                if (Natives.StandSpot(spot, at.Z, out ground))
                    spot = ground;
                Ped p = Spawner.Spawn(model, spot, Encounters.Heading(at.X - spot.X, at.Y - spot.Y));
                if (p == null)
                    continue;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                PED.SET_PED_KEEP_TASK(p.Handle, true);
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(p.Handle, Natives.Hash("WORLD_HUMAN_" + "STARE_STOIC"), -1, true, 0u, -1f, false);
                TASK.TASK_LOOK_AT_ENTITY(p.Handle, ch, -1, 0, 51, 0);
                _encounterPeds.Add(p.Handle);
                e.Crowd.Add(p);
            }
            e.NextMutter = now + 7000;
            if (e.Crowd.Count > 0)
                Log.Write("EXECUTION " + e.R.Name + ": " + e.Crowd.Count + " have come to watch");
        }

        /// One of them, low, while you're near enough to hear.
        private void ExecutionMutters(Execution e, int now, float d)
        {
            if (e.Crowd.Count == 0 || now < e.NextMutter || d > 35f)
                return;
            e.NextMutter = now + 9000 + _rng.Next(5000);
            Ped p = e.Crowd[_rng.Next(e.Crowd.Count)];
            if (p.Exists() && !p.IsDead)
                Feed.Say("Someone watching", Executions.Muttered(e.R.Id + "|" + now / 9000), Speech.When(p.Handle, "scared"), p.Handle);
        }

        /// Shots, or a fight: those who came to see run.
        private void ExecutionCrowdRuns(Execution e, Ped me)
        {
            if (e.Crowd.Count == 0 || e.TwistDone && e.Crowd.All(p => p == null || !p.Exists()))
                return;
            int you = me.Handle;
            foreach (Ped p in e.Crowd)
            {
                if (p == null || !p.Exists() || p.IsDead)
                    continue;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                PED.SET_PED_KEEP_TASK(p.Handle, false);
                ScenarioExit.SoftUp(p.Handle, x => TASK.TASK_SMART_FLEE_PED(x, you, 120f, -1, 0, 3f, 0));
            }
        }

        /// What can turn it, once you've found them: true when it's over for the men and they've gone.
        private bool ExecutionTwists(Execution e, List<Ped> men, int now)
        {
            if (e.Twist == Executions.TwistBalk && men.Count >= 2 && now >= e.ShootAt - 3500)
            {
                e.TwistDone = true;
                string seed = e.R.Id + "|balk";
                Ped second = men[1], first = men[0];
                PED.SET_PED_KEEP_TASK(second.Handle, false);
                TASK.CLEAR_PED_TASKS(second.Handle, false, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(second.Handle, first.Handle, 2500, 0f, 0f, 0f);
                Feed.Say(ExecManName(e, second), Executions.Balks(seed, e.Plan), Speech.When(second.Handle, "storm", first.Handle), second.Handle);
                Log.Write("EXECUTION " + e.R.Name + ": one of the men won't do it");
                if (Executions.Relents(seed))
                {
                    Feed.Say(ExecManName(e, first), Executions.RelentsLine(seed), Speech.When(first.Handle, "storm", e.Captive.Handle), first.Handle);
                    ExecutionLetGo(e, men, now, "the men wouldn't go through with it");
                    return true;
                }
                Feed.Say(ExecManName(e, first), Executions.BalkAnswered(seed, e.Plan), Speech.When(first.Handle, "threat", second.Handle), first.Handle);
                e.Men.Remove(second);   // he's out of it: not one of them, to you or to anyone
                e.Crowd.Add(second);
                _cameAtYouSeen.Remove(second.Handle);
                Vector3 away = second.Position + (second.Position - e.Captive.Position).Normalized * 120f;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(second.Handle, away.X, away.Y, away.Z, Natives.Brisk, -1, 1f, 0, 0f);
                e.ShootAt = Math.Max(e.ShootAt, now + 3000);
                return false;
            }
            if (e.Twist != Executions.TwistReprieve)
                return false;
            if (!e.RunnerSent)
            {
                if (now < e.SpotRetryAt && e.SpotRetryAt - now <= 1000)
                    return false;
                Vector3 at;
                if (!EncounterSpot(Game.Player.Character, 40f, 60f, out at))
                {
                    e.SpotRetryAt = now + 1000;   // nowhere hidden just now: not looked for again this second (TENHOUR item 1)
                    return false;
                }
                e.RunnerSent = true;
                e.Runner = Spawner.Spawn(e.RunnerModel, at, 0f);
                if (e.Runner == null)
                {
                    e.TwistDone = true;
                    return false;
                }
                _encounterPeds.Add(e.Runner.Handle);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(e.Runner.Handle, true);
                TASK.TASK_GO_TO_ENTITY(e.Runner.Handle, e.Captive.Handle, 40000, 1.5f, 3f, 0f, 0);
                e.ShootAt = Math.Max(e.ShootAt, now + 20000);   // the men have seen someone coming
                Log.Write("EXECUTION " + e.R.Name + ": someone is running up the road");
                return false;
            }
            if (e.Runner == null || !e.Runner.Exists() || e.Runner.IsDead)
            {
                e.TwistDone = true;
                return false;
            }
            if (e.Runner.Position.DistanceTo(e.Captive.Position) > 5f && now < e.ShootAt - 1500)
                return false;
            e.TwistDone = true;
            string s = e.R.Id + "|reprieve";
            Feed.Say("Someone running up", Executions.ReprieveCry(e.Plan, s), Speech.When(e.Runner.Handle, "plead", men[0].Handle), e.Runner.Handle);
            Feed.Say(ExecManName(e, men[0]), Executions.ReprieveAnswer(e.Plan, s), Speech.When(men[0].Handle, "storm", e.Runner.Handle), men[0].Handle);
            ExecutionLetGo(e, men, now, "someone came with what was owed");
            return true;
        }

        /// It's off: the men go, he gets up, and nothing of it is yours.
        private void ExecutionLetGo(Execution e, List<Ped> men, int now, string why)
        {
            Ped me = Game.Player.Character;
            foreach (Ped m in men)
            {
                PED.SET_PED_KEEP_TASK(m.Handle, false);
                TASK.CLEAR_PED_TASKS(m.Handle, false, false);
                Vector3 away = m.Position + (m.Position - me.Position).Normalized * 150f;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Handle, away.X, away.Y, away.Z, Natives.Brisk, -1, 1f, 0, 0f);
            }
            int ch = e.Captive.Handle;
            PED.SET_PED_KEEP_TASK(ch, false);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(ch, false);
            ScenarioExit.SoftUp(ch, x => TASK.TASK_WANDER_STANDARD(x, 10f, 10));
            _engine.Spared(e.R, e.Plan, e.Town, Bridge.Now());
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            e.KinDue = false;
            e.LetGo = true;   // nobody's holding anyone now: shooting them walking away isn't self-defence
            foreach (Ped m in e.Men)
                if (m != null)
                    _cameAtYouSeen.Remove(m.Handle);
            e.Phase = "leaving";
            e.PhaseUntil = now + 45000;
            _execCount++;
            _execLast = e.R.Name + ": let go (" + why + ")";
            Log.Write("EXECUTION " + e.R.Name + ": let go (" + why + ")");
        }

        /// After the men have gone, kin may come for him, walk up, and kneel by him. True while that's going on.
        private bool ExecutionKinStep(Execution e, int now)
        {
            if (!e.KinDue || e.KinAt == 0 || e.Captive == null || !e.Captive.Exists() || !e.Captive.IsDead)
                return false;
            if (now < e.KinAt)
                return true;
            if (e.Captive.Position.DistanceTo(Game.Player.Character.Position) > 90f)
            {
                e.KinDue = false;   // you've ridden on: nobody comes for you to see
                return false;
            }
            int body = e.Captive.Handle;
            if (e.Kin == null)
            {
                Vector3 at = default(Vector3);
                bool looked = now >= e.SpotRetryAt || e.SpotRetryAt - now > 1000;
                if (looked && Spawner.Ready(e.KinModel) && !EncounterSpot(Game.Player.Character, 40f, 55f, out at))
                {
                    looked = false;
                    e.SpotRetryAt = now + 1000;
                }
                if (!looked || !Spawner.Ready(e.KinModel))
                {
                    if (now - e.KinAt > 15000)
                    {
                        e.KinDue = false;
                        return false;
                    }
                    return true;
                }
                e.Kin = Spawner.Spawn(e.KinModel, at, 0f);
                if (e.Kin == null)
                {
                    e.KinDue = false;
                    return false;
                }
                _encounterPeds.Add(e.Kin.Handle);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(e.Kin.Handle, true);
                TASK.TASK_GO_TO_ENTITY(e.Kin.Handle, body, 60000, 1.6f, 1.6f, 0f, 0);
                e.PhaseUntil = now + 90000;
                Log.Write("EXECUTION " + e.R.Name + ": kin have come for him");
                return true;
            }
            if (!e.Kin.Exists() || e.Kin.IsDead)
            {
                e.KinDue = false;
                return false;
            }
            if (!e.KinSaid && e.Kin.Position.DistanceTo(e.Captive.Position) < 2.6f)
            {
                e.KinSaid = true;
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(e.Kin.Handle, Natives.Hash(ExecKneel), 30000, true, 0u, -1f, false);
                Feed.Say("Someone who knew him", Executions.KinAtBody(e.R.Id), Speech.When(e.Kin.Handle, "scared"), e.Kin.Handle);
                e.KinAt = now + 28000;
                e.KinDue = true;
                return true;
            }
            if (e.KinSaid && now >= e.KinAt)
            {
                e.KinDue = false;
                return false;
            }
            return now < e.PhaseUntil;
        }

        private void EndExecution(string why)
        {
            EventDotClear("execution");
            Execution e = _exec;
            _exec = null;
            if (e == null)
                return;
            if (e.Captive != null && e.Captive.Exists() && e.Phase != "spawn")
            {
                // the outcome says how it ended for him, so the mind can take it as a killing seen or, for him, a rescue (night 2026-10-28)
                string outcome = e.Captive.IsDead ? "he died; " + why : e.LetGo ? "let go; " + why : e.Phase == "saved" || e.Phase == "leaving" ? "saved; " + why : why;   // let go: nothing of that was yours
                Safe("world events", () => RecordEvent("encounter:execution", e.BossR != null ? e.BossR.Id : null, e.Plan != null ? e.Plan.Boss ?? "two men" : "two men", e.R.Id, e.R.Name,
                                                       e.Captive.Position, e.Captive.Handle, e.Captive.Handle, 0, outcome));
            }
            Ped me = Game.Player.Character;
            try
            {
                if (e.Mark != null && e.Mark.Exists())
                    e.Mark.Delete();
            }
            catch
            {
            }
            foreach (Ped p in new[] { e.Captive, e.Runner, e.Kin }.Concat(e.Men).Concat(e.Crowd))
            {
                if (p == null)
                    continue;
                // the living vanish only where nobody sees it, and never one enRAGE has looked at; the dead stay where they fell
                bool alive = p.Exists() && !p.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle);
                bool enrageKnows = alive && _bridge != null && _bridge.Looked(p.Handle);
                Spawner.Release(p, alive && !enrageKnows && (!seen || p.Position.DistanceTo(me.Position) > 120f));
            }
            if (e.Phase != "spawn" && !e.Test)
                MarkDone("execution");   // nowhere to put them doesn't use up the day, nor does a test
            Log.Write("EXECUTION " + e.R.Name + ": over (" + why + ")");
        }

        /// On the ticker: his thought over his head while he kneels (the men have none).
        private void ExecutionNear(Ped ped, float d)
        {
            Execution e = _exec;
            if (_cfg.RoadThoughts && e != null && e.Phase == "held" && e.Men.Exists(x => x != null && x.Handle == ped.Handle))
            {
                long at = Bridge.Now();   // those holding him have their own thoughts on it (FIVEDAY2 item 26)
                string key = "exec:" + ped.Handle;
                _near.Add(new NearThought { Handle = ped.Handle, Key = key, Name = "One of them", Distance = d + 1f, Text = () => RoadThoughts.ForRole(RoadThoughts.Captor, key + "|" + at / 3) });
                return;
            }
            if (e == null || e.Captive == null || ped.Handle != e.Captive.Handle || e.Phase != "held" && e.Phase != "fight")
                return;
            Resident r = e.R;
            long now = Bridge.Now();
            _near.Add(new NearThought
            {
                Handle = ped.Handle,
                Key = r.Id,
                Name = r.Name + ", on his knees",
                Distance = d,
                Text = () => Executions.During(e.Plan, r.Id + "|" + now / 3)
            });
        }

        private string ExecutionsStatus()
        {
            if (!_cfg.Executions)
                return "off";
            return _cfg.ExecutionChance + "% (05:00-22:00, away from the towns)" + (DoneToday("execution") ? ", done today" : "") +
                   (_exec != null ? "; going on: " + _exec.R.Name + " (" + _exec.Phase + ", " + _exec.Plan.Reason + ")" : "") + "; this session " + _execCount + (_execLast != null ? ", last " + _execLast : "");
        }
    }
}
