// Out of a scenario Rockstar's way (Lex, 2026-11-01, Valentine, NIGHT4 stage 1: "some NPCs nearby do seem frozen"). The
// get-up asked by one bare CLEAR_PED_TASKS(h, true, false), the mind's and the waking's alike, got 3 of 23 up; the other 20,
// standing smokers and bar drinkers as well as the seated, stayed in their scenario or stood frozen in its pose, and were
// left as they were. Rockstar's scripts never do that (the study, script_rel):
//   the soft exit   the scenario's exit flag, then the next task the same tick, no clear between (av_fox_sit.c 1949:
//                   NORMAL_EXIT then TASK_STAND_STILL; barcustomer_interaction.c 863: NORMAL_EXIT then TASK_USE_SCENARIO_POINT)
//   the second ask  a soft clear only while ACTIVE_IN_SCENARIO(p, 0) and not EXITING_SCENARIO(p, 1), never mid-exit
//                   (cv_amb_whisper_sit.c 10468, beat_street_fight.c 4694); the IMMEDIATE flag first (cv_amb_light_smoke func_384)
//   the hard pull   IMMEDIATE_EXIT, CLEAR_PED_TASKS_IMMEDIATELY(p, true, true), DETACH_ENTITY if attached
//                   (cv_amb_whisper_sit.c 4082, cornwall1_outro.c 804, beat_street_fight.c 17060), the next task the same tick
//   "out"           not using the scenario and not exiting it (barcustomer_interaction.c 825)
// And never left frozen: whoever isn't out in time is put back (their own point, their type in place, or a wander), looked at
// 3 s on, and pulled the hard way onto a wander if that didn't take. Every path is logged with the flags seen on the way
// (the second argument of IS_PED_EXITING_SCENARIO and IS_PED_ACTIVE_IN_SCENARIO isn't known: both are logged) and counted
// for F7. The pure decisions are Core/MindScenarios.cs (UpStep, RestorePick, SeatType).
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    /// One person asked out of a scenario: what they were in (to put them back), the path taken, the flags seen.
    internal sealed class ExitAsk
    {
        public int Handle, Point, AskedAt, LastFlags = -1, CheckAt;
        public uint Type;
        public string TypeName, Name, Path, Trace = "", Restored, Who;
        public Vector3 At;
        public bool AskedTwice, Hard;
    }

    internal static class ScenarioExit
    {
        private const int AskStandMs = MindScenarios.UpMostMs + MindScenarios.ExitGraceMs + MindScenarios.StandMs;   // the stand-still that follows the exit outlasts the longest wait
        private static readonly List<ExitAsk> Checks = new List<ExitAsk>();
        private static readonly Dictionary<string, int> Paths = new Dictionary<string, int>();

        private static void Count(string path)
        {
            int n;
            Paths.TryGetValue(path, out n);
            Paths[path] = n + 1;
        }

        /// The soft exit: what they're in kept, the exit flag (directed toward `dest` when there is one), the stand-still the
        /// same tick. No clear. `who` names the feature in the log ("MIND", "WAKE").
        public static ExitAsk Ask(int h, string name, string who, Vector3? dest, int now)
        {
            var e = new ExitAsk { Handle = h, Name = name, Who = who, AskedAt = now };
            e.Point = TASK._GET_SCENARIO_POINT_PED_IS_USING(h, true);   // Rockstar passes true
            e.Type = (uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h);
            e.TypeName = MindScenarios.NameOf(e.Type);
            e.At = ENTITY.GET_ENTITY_COORDS(h, true, true);
            e.Path = "normal";
            if (dest.HasValue)
            {
                bool d = PED.SET_PED_SHOULD_PLAY_DIRECTED_NORMAL_SCENARIO_EXIT(h, dest.Value);
                if (d)
                    e.Path = "directed";
                else
                {
                    e.Path = "normal (directed refused)";
                    PED.SET_PED_SHOULD_PLAY_NORMAL_SCENARIO_EXIT(h);
                }
            }
            else
                PED.SET_PED_SHOULD_PLAY_NORMAL_SCENARIO_EXIT(h);
            TASK.TASK_STAND_STILL(h, AskStandMs);   // the replacement task, the same tick (av_fox_sit.c)
            Count(e.Path.StartsWith("directed") ? "directed" : "normal");
            Flags(e, now);
            return e;
        }

        public static string TypeWords(ExitAsk e)
        {
            return e.TypeName ?? "scenario 0x" + e.Type.ToString("X8");
        }

        /// The flags now, added to the trace when they changed: U using, X exiting (true/false arg), A active (0/1 arg), B in
        /// the base loop, T in a transition. Returns the bits.
        private static int Flags(ExitAsk e, int now)
        {
            int h = e.Handle;
            bool u = PED.IS_PED_USING_ANY_SCENARIO(h), xt = TASK.IS_PED_EXITING_SCENARIO(h, true), xf = TASK.IS_PED_EXITING_SCENARIO(h, false),
                 a0 = TASK.IS_PED_ACTIVE_IN_SCENARIO(h, 0), a1 = TASK.IS_PED_ACTIVE_IN_SCENARIO(h, 1), b = TASK._PED_IS_IN_SCENARIO_BASE(h), t = PED._IS_PED_DOING_SCENARIO_TRANSITION(h);
            int f = (u ? 1 : 0) | (xt ? 2 : 0) | (xf ? 4 : 0) | (a0 ? 8 : 0) | (a1 ? 16 : 0) | (b ? 32 : 0) | (t ? 64 : 0);
            if (f != e.LastFlags)
            {
                e.LastFlags = f;
                if (e.Trace.Length < 600)
                    e.Trace += (e.Trace.Length > 0 ? "; " : "") + ((now - e.AskedAt) / 100 / 10.0).ToString("0.0") + "s U" + (u ? 1 : 0) + " X" + (xt ? 1 : 0) + "/" + (xf ? 1 : 0) +
                               " A" + (a0 ? 1 : 0) + "/" + (a1 ? 1 : 0) + " B" + (b ? 1 : 0) + " T" + (t ? 1 : 0);
            }
            return f;
        }

        /// One poll of a get-up: "up", "restore" (not out in time: Restore them) or null (wait). The second ask is made here,
        /// once, and only Rockstar's guarded way.
        public static string Poll(ExitAsk e, int now)
        {
            int h = e.Handle;
            int f = Flags(e, now);
            bool u = (f & 1) != 0, exiting = (f & 6) != 0, a0 = (f & 8) != 0;
            string step = MindScenarios.UpStep(u, exiting, a0, now - e.AskedAt, e.AskedTwice);
            if (step == "reask")
            {
                e.AskedTwice = true;
                if (TASK.IS_PED_ACTIVE_IN_SCENARIO(h, 0) && !TASK.IS_PED_EXITING_SCENARIO(h, true))
                {
                    PED.SET_PED_SHOULD_PLAY_IMMEDIATE_SCENARIO_EXIT(h);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_STAND_STILL(h, AskStandMs);   // the replacement task, the same tick
                    e.Path += ", immediate at " + (now - e.AskedAt) / 1000 + " s";
                    Count("immediate (second ask)");
                    Log.Detail(e.Who + " up-ask: " + e.Name + " (" + TypeWords(e) + ") asked again, exit=immediate");
                }
                return null;
            }
            return step;
        }

        /// One line for the log when they're up: the path and the flags on the way.
        public static string UpWords(ExitAsk e, int now)
        {
            return "exit " + e.Path + ", up in " + ((now - e.AskedAt) / 100 / 10.0).ToString("0.0") + " s [" + e.Trace + "]";
        }

        /// Not out in time: put back where they are, the same tick, no clear: their own point (still there and free, or still
        /// theirs), else their type in place (not a seat type), else a wander. Events unblocked. Looked at again 3 s on
        /// (Step). Returns the path taken.
        public static string Restore(ExitAsk e, int now)
        {
            int h = e.Handle;
            Flags(e, now);
            int pt = e.Point;
            bool pointOk = false;
            if (pt != 0 && TASK.DOES_SCENARIO_POINT_EXIST(pt))
            {
                int user = TASK._GET_PED_USING_SCENARIO_POINT(pt);
                pointOk = user == 0 || user == h;
            }
            string pick = MindScenarios.RestorePick(pointOk, MindScenarios.SeatType(e.TypeName), e.Type != 0);
            if (pick == "point")
                TASK.TASK_USE_SCENARIO_POINT(h, pt, "", 0, true, false, 0u, false, -1f, false);   // barcustomer_interaction.c 864's args ("" for its null, as every string native here)
            else if (pick == "in place")
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, e.Type, 180000, false, 0u, -1f, false);   // the Director's call; no enter clip: they never left
            else
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            e.Restored = pick;
            e.CheckAt = now + MindScenarios.RestoreCheckMs;
            Count("restored: " + pick);
            Checks.Add(e);
            Log.Write(e.Who + " restore: " + e.Name + " (" + TypeWords(e) + ") not out in " + ((now - e.AskedAt) / 100 / 10.0).ToString("0.0") + " s -> " + pick + " (exit " + e.Path + ") [" + e.Trace + "]");
            return pick;
        }

        /// The hard pull, Rockstar's forced way (cv_amb_whisper_sit.c 4082): the immediate exit flag, the immediate clear,
        /// detached if attached (a chair, a bar). The caller gives the next task the same tick, so no idle pose between.
        public static void Pull(int h)
        {
            PED.SET_PED_SHOULD_PLAY_IMMEDIATE_SCENARIO_EXIT(h);
            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
            if (ENTITY.IS_ENTITY_ATTACHED(h))
                ENTITY.DETACH_ENTITY(h, true, true);
            Count("pulled");
        }

        /// Up from a scenario softly with the next task already given (`next`, the same tick): the exit flag and the task, no
        /// clear; someone in no scenario gets the plain clear first, as before. For the Director and the join-in.
        public static void SoftUp(int h, System.Action<int> next)
        {
            if (PED.IS_PED_USING_ANY_SCENARIO(h))
            {
                PED.SET_PED_SHOULD_PLAY_NORMAL_SCENARIO_EXIT(h);
                Count("normal");
            }
            else
                TASK.CLEAR_PED_TASKS(h, true, false);
            next(h);
        }

        /// A second soft ask, only Rockstar's guarded way (active in the scenario and not exiting it): the immediate flag, the
        /// clear and the next task the same tick. False when they were mid-exit (left to it).
        public static bool Reask(int h, System.Action<int> next)
        {
            if (!TASK.IS_PED_ACTIVE_IN_SCENARIO(h, 0) || TASK.IS_PED_EXITING_SCENARIO(h, true))
                return false;
            PED.SET_PED_SHOULD_PLAY_IMMEDIATE_SCENARIO_EXIT(h);
            TASK.CLEAR_PED_TASKS(h, true, false);
            next(h);
            Count("immediate (second ask)");
            return true;
        }

        /// From DramaTick: each one put back is looked at 3 s on. In a scenario and playing it (its base loop, active, or a
        /// transition), or moved on a wander: done. Else pulled the hard way onto a wander, and looked at once more.
        /// `busy`: another feature has them now (left alone).
        public static void Step(int now, System.Func<int, bool> busy)
        {
            for (int i = Checks.Count - 1; i >= 0; i--)
            {
                ExitAsk e = Checks[i];
                if (now < e.CheckAt && e.CheckAt - now <= 10000)
                    continue;
                Checks.RemoveAt(i);
                int h = e.Handle;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (busy(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h))
                {
                    Log.Detail(e.Who + " restore: " + e.Name + " taken by something else before the look (left be)");
                    continue;
                }
                Flags(e, now);
                bool moved = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(e.At) > 0.5f;
                bool ok;
                if (e.Hard || e.Restored == "wander")
                    ok = moved || PED.IS_PED_USING_ANY_SCENARIO(h);   // on their way, or the game gave them a scenario of its own
                else
                    ok = PED.IS_PED_USING_ANY_SCENARIO(h) && (TASK._PED_IS_IN_SCENARIO_BASE(h) || TASK.IS_PED_ACTIVE_IN_SCENARIO(h, 0) || PED._IS_PED_DOING_SCENARIO_TRANSITION(h));
                if (ok)
                {
                    Count(e.Hard ? "pulled and moving" : "restore held");
                    Log.Write(e.Who + " restore " + (e.Hard ? "hard " : "") + "ok: " + e.Name + " (" + e.Restored + (moved ? ", moved" : "") + ") [" + e.Trace + "]");
                    continue;
                }
                if (e.Hard)
                {
                    Count("pulled, still not moving");
                    Log.Write(e.Who + " restore hard failed: " + e.Name + " still not moving after the pull and the wander [" + e.Trace + "]");
                    continue;
                }
                Pull(h);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // the same tick: no idle pose between
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                e.Hard = true;
                e.CheckAt = now + MindScenarios.RestoreCheckMs;
                Checks.Add(e);
                Log.Write(e.Who + " restore failed: " + e.Name + " (" + e.Restored + ") not playing it 3 s on: pulled the hard way, on a wander [" + e.Trace + "]");
            }
        }

        public static void Clear()
        {
            Checks.Clear();
        }

        /// F7: the paths out of a scenario taken so far.
        public static string Status()
        {
            return Paths.Count == 0 ? "out of a scenario: none yet" : "out of a scenario: " + string.Join(", ", Paths.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value));
        }
    }
}
