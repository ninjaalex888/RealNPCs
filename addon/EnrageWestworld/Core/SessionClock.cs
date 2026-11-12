// The clock pass (DEPTH item 16; [World] SessionClock, on). Lex, 2026-11-10: weeks, even three game weeks, are too long for an arc a
// player is meant to see: a game day is 48 real minutes, so two game days is about an hour and a half of play. Every timer longer than
// two game days was sorted: "watch" (an arc you're meant to see: shortened to a session, most to one or two game days), "background"
// (a gap that keeps something rare: kept), "calendar" (Sundays, payday, the week's column: kept) and "memory" (how long someone
// remembers: case by case, kept here). The watch timers read their length through Days(asBuilt, session), so the switch off gives
// every old length back. Off in Core as it stands, so the old harnesses' goldens and tests keep the lengths they were written for;
// the game (ApplySettings) and the commit gate's measure set it from the settings, which have it on. The table below is the one in
// DEPTH.md, and mindtest SessionClockTest checks that the two agree with the code.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class SessionClock
    {
        public static bool On;   // [World] SessionClock (on as it comes), set by the game; off in Core for the old harnesses

        /// A watch timer's length in game days: the session's with the clock on, else as built.
        public static int Days(int asBuilt, int session) { return On ? session : asBuilt; }

        public sealed class Row
        {
            public string Name, Kind, Where;
            public int AsBuilt, Session;   // game days; Session == AsBuilt for the kept ones
        }

        /// Every timer of more than two game days the pass looked at, its kind and its length both ways.
        public static readonly Row[] Table =
        {
            // watch: shortened to a session
            new Row { Name = "a man you vouched for: his week", Kind = "watch", AsBuilt = 7, Session = 2, Where = "Vouch.KeepDays" },
            new Row { Name = "the sheriff won't take a broken word", Kind = "watch", AsBuilt = 30, Session = 2, Where = "Vouch.NoWordDays" },
            new Row { Name = "the town's tales live", Kind = "watch", AsBuilt = 7, Session = 2, Where = "Tales.LastsDays" },
            new Row { Name = "the run-out cardsharp's return, at the most", Kind = "watch", AsBuilt = 14, Session = 2, Where = "Cardsharp.BackMostDays" },
            new Row { Name = "...at the least", Kind = "watch", AsBuilt = 2, Session = 1, Where = "Cardsharp.BackLeastDays" },
            new Row { Name = "the beaten man's plan keeps (a rematch, a shunning, a hand)", Kind = "watch", AsBuilt = 4, Session = 2, Where = "Rematch.Days" },
            new Row { Name = "kin come about a wrong, at the least", Kind = "watch", AsBuilt = 5, Session = 1, Where = "KinGrudges.DueLeastDays" },
            new Row { Name = "...at the most", Kind = "watch", AsBuilt = 9, Session = 2, Where = "KinGrudges.DueMostDays" },
            new Row { Name = "...and look for you", Kind = "watch", AsBuilt = 7, Session = 2, Where = "KinGrudges.WaitsDays" },
            new Row { Name = "the circuit judge comes, at the least", Kind = "watch", AsBuilt = 3, Session = 1, Where = "Trials.JudgeDaysLeast" },
            new Row { Name = "...at the most", Kind = "watch", AsBuilt = 7, Session = 2, Where = "Trials.JudgeDaysMost" },
            new Row { Name = "a loan of yours falls due", Kind = "watch", AsBuilt = 3, Session = 1, Where = "Verbs.LoanDays" },
            new Row { Name = "the claim agent calls on a widow", Kind = "watch", AsBuilt = 3, Session = 1, Where = "ClaimAgent.AfterDays" },
            new Row { Name = "a sweetheart expects you back", Kind = "watch", AsBuilt = 3, Session = 1, Where = "Trysts.ExpectsFromDays" },

            // background: a gap that keeps something rare (kept)
            new Row { Name = "the prize fight's gap in a town (shortened it would triple the town's prize fights: a fight-start rule to decide)", Kind = "background", AsBuilt = 6, Session = 6, Where = "PrizeFight.GapDays" },
            new Row { Name = "a fever's gap", Kind = "background", AsBuilt = 90, Session = 90, Where = "Fever.GapDays" },
            new Row { Name = "the mine whistle's gap", Kind = "background", AsBuilt = 21, Session = 21, Where = "MineWhistle.GapDays" },
            new Row { Name = "the Wild West show's gap", Kind = "background", AsBuilt = 30, Session = 30, Where = "Shows.GapDays" },
            new Row { Name = "the rider asking for a town on no map", Kind = "background", AsBuilt = 14, Session = 14, Where = "NoMapRider.GapDays" },
            new Row { Name = "the Pinkertons' visits", Kind = "background", AsBuilt = 5, Session = 5, Where = "Pinkertons.GapDays" },
            new Row { Name = "fence cutters", Kind = "background", AsBuilt = 6, Session = 6, Where = "FenceCutters.GapDays" },
            new Row { Name = "barn raisings", Kind = "background", AsBuilt = 21, Session = 21, Where = "BarnRaising.GapDays" },
            new Row { Name = "camp meetings", Kind = "background", AsBuilt = 30, Session = 30, Where = "Revival.GapDays" },
            new Row { Name = "the dime novel", Kind = "background", AsBuilt = 60, Session = 60, Where = "DimeNovel.GapDays" },
            // calendar (kept)
            new Row { Name = "the week's column, Sunday, payday, dances, the box social", Kind = "calendar", AsBuilt = 7, Session = 7, Where = "the calendar" },
            new Row { Name = "mourning", Kind = "calendar", AsBuilt = 21, Session = 21, Where = "Mourning.Days" },
            // memory: how long someone remembers (kept)
            new Row { Name = "a wrong remembered (trust)", Kind = "memory", AsBuilt = 60, Session = 60, Where = "Trust.WindowDays" },
            new Row { Name = "the criminal class keeps to it", Kind = "memory", AsBuilt = 30, Session = 30, Where = "Criminals.BentDays" },
            new Row { Name = "the nemesis holds it", Kind = "memory", AsBuilt = 30, Session = 30, Where = "Nemesis.HeldDays" },
            new Row { Name = "condolences fresh", Kind = "memory", AsBuilt = 14, Session = 14, Where = "Fondness.CondoleDays" },
            new Row { Name = "a newcomer fresh off the train", Kind = "memory", AsBuilt = 14, Session = 14, Where = "NewFaces.FreshDays" },
            new Row { Name = "a sweetheart cools if you don't come (kept: a cost, not an arc; review 2)", Kind = "memory", AsBuilt = 14, Session = 14, Where = "Trysts.CoolsAfterDays" },
        };

        public static IEnumerable<Row> Watch() { foreach (Row r in Table) if (r.Kind == "watch") yield return r; }
    }
}
