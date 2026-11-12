// Election night in the saloon (LIVES item 2b, 2026-11-12; [Scenes] ElectionNight, settings version 468; the roadmap's
// "Election day" row). The town's election (Core/Elections.cs, [World] EventElections) already had its candidates, the
// whiskey bought for votes, the porch speech and the hecklers, and supporters at words; what it didn't have was the year's
// worst brawl. Now on election day by the real calendar (with HistoricalAccuracy), from four in the afternoon, two men in the
// saloon on opposite sides of the vote (the working, farming and rough trades for the people's man; the well-off and the
// town's men of business for the courthouse) may come to blows over it. The scene is its own (as the old soldiers' is): the
// vote's openers ("Ballot box was stuffed."), Over "politics:election" so the onlookers who hold a side join their man
// (Core/BrawlSides.cs reads the topic's sides) and the town retells it as a fight over the vote; a coward has words instead.
//
// Why it reads true: polling was often in or beside a saloon in 1899 and the parties stood the drinks, so election night was a
// fighting night, and a fight with a cause picks its own sides (Tajfel's minimal groups). Cast from two already in the saloon
// together: nobody walks over. TroubleWeight scales it like every other fight. Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ElectionNight
    {
        public const string Topic = "election", Kind = "electionfight";
        public static bool On;   // [Scenes] ElectionNight, set by the game
        public const int Strength = 58, Percent = 30, FromHour = 16;

        /// Whether the real calendar says it's election day today (InnerLife's clock; the history on).
        public static bool Today()
        {
            if (!InnerLife.Historical || InnerLife.ClockMonth != 11 || InnerLife.ClockDay <= 0)
                return false;
            Holiday h = Calendar.Today(InnerLife.CalendarYear > 0 ? InnerLife.CalendarYear : InnerLife.ClockYear, InnerLife.ClockMonth, InnerLife.ClockDay, null);
            return h != null && h.Key == "election";
        }

        /// Two men in the saloon on election night, on opposite sides of the vote: their fight over it, or null.
        public static Reason For(Someone x, Someone y, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || !where.Saloon || where.Hour < FromHour || x == null || y == null || x.Id == y.Id || x.Woman || y.Woman)
                return null;
            if (x.P == null || y.P == null || !Today())
                return null;
            int a = QuarrelPolitics.Stance(Topic, QuarrelPolitics.GroupOf(x.P.Trade), false), b = QuarrelPolitics.Stance(Topic, QuarrelPolitics.GroupOf(y.P.Trade), false);
            if (a == 0 || b == 0 || a == b)
                return null;
            if (!Voice.Chance(Lives.Key(x, y) + "|" + gameDay + "|election", Reasons.Weighted(Percent, where.Trouble)))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Fallback = "argue", Matter = "nothing", Over = "politics:" + Topic, Why = x.Name + " and " + y.Name + " on opposite sides of the vote, election night in the saloon" };
        }

        /// A opens on the vote; blows, or words when either is a coward.
        public static Scene Make(SceneCast c, string seed)
        {
            return CauseFight.Make(c, QuarrelPolitics.Openers(Topic), "politics:" + Topic, "two men came to blows over the vote on election night", "two men had words over the vote on election night", seed);
        }
    }

    /// A fight over a cause, the scene's own (the election, the dance): A opens with the cause's opener, B answers, A presses,
    /// a last word each and the blows; a coward of either has words and walks off. Over is the cause, for the sides and the
    /// telling after.
    public static class CauseFight
    {
        private static readonly string[] Last = { "Say that again.", "You'll take that back.", "Outside, then.", "Any time you like." };
        private static readonly string[] Swing = { "I'll not say it twice.", "Come on, then.", "You asked for this." };
        private static readonly string[] Off = { "You're not worth it.", "Suit yourself, then.", "We're done talking, you and me." };

        public static Scene Make(SceneCast c, string[][] openers, string over, string fightSummary, string wordsSummary, string seed)
        {
            string[] o = openers != null && openers.Length > 0 ? openers[(Voice.Hash(seed + "|cause") & 0x7fffffff) % openers.Length] : new[] { "You and me have business.", "Do we, now?", "We do.", "angry" };
            string ta = c != null && c.Tempers != null ? c.Tempers[0] : null, tb = c != null && c.Tempers != null ? c.Tempers[1] : null;
            bool coward = ta == "Coward" || tb == "Coward";
            Scene s;
            if (coward)
            {
                s = new Scene { Kind = "argue", NoLadder = true, Over = over, Summary = wordsSummary };
                s.Add(0, o[0], "angry", "face").Add(1, o[1], "angry").Add(0, o[2], o.Length > 3 ? o[3] : "angry")
                 .Add(1, Voice.Pick(seed + "|off", Off), "mild").Add(1, null, null, "walkaway");
                return s;
            }
            s = new Scene { Kind = "brawl", Fight = true, NoLadder = true, Over = over, Summary = fightSummary };
            s.Add(0, o[0], "angry", "face").Add(1, o[1], "angry").Add(0, o[2], o.Length > 3 ? o[3] : "angry")
             .Add(1, Voice.Pick(seed + "|last", Last), "angry")
             .Add(0, Voice.Pick(seed + "|swing", Swing), "angry", "fight", 9000)
             .Add(0, null, null, "endfight");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Last, Swing, Off })
                foreach (string l in a)
                    yield return l;
        }
    }
}
