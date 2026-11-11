// The proof tour (DEPTH item 2, 2026-11-10; [Testing] ProofTour, ProofTourRisky, ProofTourGapSeconds; the Testing row
// "prooftour"). One row walks the Testing page's rows that can run where you stand, each in its turn, so a single session in
// game proves dozens of systems instead of one: most of what has been built has never been seen in a log (13 of 81 systems as
// of 0.7.24), and each row has had to be pressed by hand.
//
// What it does: the rows in the panel's order (its groups, then each group's rows as Core/TestActions.cs lists them), each
// fired through the row's own start path (WestworldScript.Testing.cs TestFireFor) about every ProofTourGapSeconds; the next
// waits while anything RealNPCs fired is still under way (a scene, an encounter); every row logs `PROOF <row>: ran`,
// `skipped (<the row's own why>)` or `failed (<the error>)`, and the end a `PROOF summary`. It pauses for combat, a mission,
// a menu or the saddle, and the row again resumes it (pressed while it runs, it stops).
//
// Safe by default (DEPTH's rule): a row that takes real money, can get Arthur shot or hurt, calls a native nobody has seen
// work, or changes the world for good (someone dies, a fever starts) carries a verdict here and is passed over unless
// [Testing] ProofTourRisky is on. A few rows never go on the tour at all (the tour itself, replay, toggles, the long voice
// recordings). Every row on the page has a verdict: a new row with none fails mindtest's ProofTourTest, so nobody can add
// a row and forget to say whether the tour may run it.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class ProofTour
    {
        public const string Id = "prooftour";

        // ---- the verdicts ----------------------------------------------------------------------------------------------
        // Never on the tour: they'd fire the tour itself, the last test again, flip a display for the session, write a file
        // nobody asked for, take an hour, or bring people over (the test cast walks over to a spot: Lex, 2026-11-01).
        static readonly HashSet<string> Never = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Id, "replay", "showminds", "wakestages", "cast", "cast:go", "voicerec:all", "translation:template", "font", "subtitle",
        };

        // Risky: passed over unless [Testing] ProofTourRisky. The word says why (the log's skipped line carries it).
        static readonly Dictionary<string, string> Risky = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // real money
            { "vouch", "takes real money" }, { "horsewatch", "takes real money" }, { "collection", "takes real money" }, { "plate", "takes real money" },
            { "lift", "takes real money" }, { "alley", "takes real money and can get you shot" },
            // can get Arthur shot or hurt
            { "holdup", "can get you shot" }, { "allies", "an armed man comes at you" }, { "revenant", "comes to kill you" },
            { "callyou", "can come to blows with you" }, { "stalker", "can come at you" }, { "stalkerhorse", "can come at you" },
            { "plea", "needs your gun drawn on someone" }, { "wakestrange", "can lash out at you" }, { "elope", "her father's men come armed" },
            { "gunshot", "shots fired near you; the law may come" }, { "brawltown", "a brawl may come at you" },
            { "scene:standoff", "guns drawn near you" }, { "visitor", "can frighten people into running at you" },
            // calls a native, or a clip, nobody has seen work (the probes)
            { "owners", "an unproven native (the owners probe)" }, { "posse:probe", "a probe of unproven natives" },
            { "wagon", "a probe of unproven natives" }, { "events:probe", "a probe" }, { "looks:probe", "a probe of clothes reads" },
            { "clips", "plays unproven clips for minutes" }, { "despairanims", "plays clips for minutes" },
            { "stageanims", "plays clips for minutes" }, { "wakevoices", "plays every line for minutes" },
            { "stalkeroutfit", "stands men up before you" },
            // changes the world for good: somebody dies, something spreads or is kept
            { "despairnow", "someone dies for real" }, { "execution", "someone may die" }, { "execution:balk", "someone may die" },
            { "execution:reprieve", "someone may die" }, { "execution:crowd", "someone may die" }, { "execution:committee", "someone may die" },
            { "mine", "two men die" }, { "fever", "a fever spreads for days" }, { "trial", "a man may hang" },
            { "novel", "a dime novel is published" }, { "foreclosure", "a family loses its place" },
            { "hogtied", "a captive held for a price" }, { "prizefight", "the town's men at fists" }, { "bestfist", "the town's men at fists" },
            // needs something running outside the game (nothing to prove without it)
            { "voice", "needs the voice server" }, { "voicerec", "needs the voice server" }, { "listen", "needs the voice server and a microphone" },
            { "mind:ask", "needs the local model running" },
        };

        /// The row's verdict: null (the tour runs it), "never: <why>", or why it's risky.
        public static string Verdict(string id)
        {
            if (id == null)
                return "never: no row";
            if (Never.Contains(id))
                return "never: not a row to walk";
            string why;
            if (Risky.TryGetValue(id, out why))
                return why;
            if (id.StartsWith("wayside:", StringComparison.OrdinalIgnoreCase))
                return WaysideRisk(id.Substring("wayside:".Length));
            return null;
        }

        /// A road scene is risky when it can turn on you: its armed come at you when aimed at, or a beat shoots, draws or
        /// fights (the table says, so a new kind is judged by what it does, not by a list kept by hand).
        static string WaysideRisk(string kind)
        {
            WaysideKind k = Wayside.All.FirstOrDefault(x => string.Equals(x.Id, kind, StringComparison.OrdinalIgnoreCase));
            if (k == null)
                return null;
            if (k.Threat == "fight")
                return "a road scene whose armed come at you";
            if (k.Beats != null && k.Beats.Any(b => b.Do == "shoot" || b.Do == "draw" || b.Do == "fight"))
                return "a road scene with a fight or a shot in it";
            return null;
        }

        /// Every id the verdict tables name (for the offline check that each row has one and no table names a row that's gone).
        public static IEnumerable<string> Named { get { return Never.Concat(Risky.Keys); } }

        /// The panel's order: its groups, then each group's rows in the catalogue's order; the "never" rows left out.
        public static List<TestActions.Entry> Order(IEnumerable<TestActions.Entry> all)
        {
            var list = all.ToList();
            var order = new List<TestActions.Entry>();
            foreach (string g in TestActions.Groups)
                foreach (TestActions.Entry e in list)
                    if (e.Group == g && !(Verdict(e.Id) ?? "").StartsWith("never", StringComparison.Ordinal))
                        order.Add(e);
            return order;
        }

        // ---- the walk ---------------------------------------------------------------------------------------------------

        /// One tour. The game side asks `Next` once a tick with why it can't go on (null: it can) and whether anything we fired
        /// is still under way; it fires what Next hands it and says what came of it in `Done`. Nothing here touches the game.
        public sealed class Walk
        {
            public readonly List<TestActions.Entry> Rows;
            public readonly bool RiskyToo;
            public readonly int GapMs, BusyMostMs;
            public int At, Ran, Skipped, Failed, Passed;   // Passed: risky rows passed over
            public string PausedFor;                       // why it's paused (combat, a mission...), else null
            public bool Over, Stopped;
            public int LastAt = int.MinValue / 2, BusySince = -1;
            public string Waiting;                         // the row in hand, fired and not yet reported
            public readonly List<string> Lines = new List<string>();   // what the log gets, in order (the game side writes them)

            public Walk(IEnumerable<TestActions.Entry> all, bool riskyToo, int gapMs, int busyMostMs = 180000)
            {
                Rows = Order(all);
                RiskyToo = riskyToo;
                GapMs = Math.Max(1000, gapMs);
                BusyMostMs = busyMostMs;
            }

            /// The row to fire now, or null. `stop`: why the world won't have it now (it pauses, logged once); `busy`: something
            /// we fired is still under way (the next waits; past BusyMostMs the tour pauses, so it never fires over it).
            public TestActions.Entry Next(int now, string stop, bool busy)
            {
                if (Over || Waiting != null)
                    return null;
                if (stop != null)
                {
                    if (PausedFor == null)
                    {
                        PausedFor = stop;
                        Lines.Add("PROOF paused: " + stop + " (row " + (At + 1) + " of " + Rows.Count + "; the row again resumes)");
                    }
                    return null;
                }
                if (PausedFor != null)
                    return null;   // only the row again resumes it (Resume): a pause never lifts itself mid-scene
                if (busy)
                {
                    if (BusySince < 0)
                        BusySince = now;
                    else if (now - BusySince > BusyMostMs)
                    {
                        PausedFor = "something is still under way after " + BusyMostMs / 1000 + " s";
                        Lines.Add("PROOF paused: " + PausedFor + " (row " + (At + 1) + " of " + Rows.Count + "; the row again resumes)");
                        BusySince = -1;
                    }
                    return null;
                }
                BusySince = -1;
                if (now - LastAt < GapMs)
                    return null;
                while (At < Rows.Count)
                {
                    TestActions.Entry e = Rows[At];
                    string why = Verdict(e.Id);
                    if (why != null && !RiskyToo)
                    {
                        Passed++;
                        Lines.Add("PROOF " + e.Id + ": skipped (risky: " + why + "; [Testing] ProofTourRisky)");
                        At++;
                        continue;
                    }
                    Waiting = e.Id;
                    LastAt = now;
                    return e;
                }
                Finish(false);
                return null;
            }

            /// What came of the row Next handed out: null ran, else the row's own why ("failed: ..." for an error).
            public void Done(string id, string refused)
            {
                if (Waiting == null || !string.Equals(Waiting, id, StringComparison.OrdinalIgnoreCase))
                    return;
                Waiting = null;
                At++;
                if (refused == null) { Ran++; Lines.Add("PROOF " + id + ": ran"); }
                else if (refused.StartsWith("failed: ", StringComparison.Ordinal)) { Failed++; Lines.Add("PROOF " + id + ": failed (" + refused.Substring(8) + ")"); }
                else { Skipped++; Lines.Add("PROOF " + id + ": skipped (" + refused + ")"); }
                if (At >= Rows.Count)
                    Finish(false);
            }

            public void Resume(int now)
            {
                if (Over || PausedFor == null)
                    return;
                Lines.Add("PROOF resumed at row " + (At + 1) + " of " + Rows.Count + " (paused for " + PausedFor + ")");
                PausedFor = null;
                BusySince = -1;
                LastAt = now - GapMs;   // the next row at once
            }

            public void Finish(bool stopped)
            {
                if (Over)
                    return;
                Over = true;
                Stopped = stopped;
                Lines.Add("PROOF summary: " + (stopped ? "stopped at row " + (At + 1) + " of " + Rows.Count : "all " + Rows.Count + " rows walked") +
                          "; ran " + Ran + ", skipped " + Skipped + " (the rows' own reasons), failed " + Failed + ", risky passed over " + Passed);
            }

            /// The lines not yet written, taken (the game side writes each once).
            public List<string> Take()
            {
                var t = new List<string>(Lines);
                Lines.Clear();
                return t;
            }
        }
    }
}
