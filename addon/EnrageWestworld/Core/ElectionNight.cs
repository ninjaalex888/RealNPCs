// Election night in the saloon (LIVES item 2b, 2026-11-12; [Scenes] ElectionNight, settings version 468; the roadmap's
// "Election day" row). The town's election (Core/Elections.cs, [World] EventElections) already had its candidates, the
// whiskey bought for votes, the porch speech and the hecklers, and supporters at words; what it didn't have was the year's
// worst brawl. Now on election day, from four in the afternoon, two men in the saloon on opposite sides of the vote (the
// working, farming and rough trades for the people's man, the well-off, the town's men of business and the church-minded for
// the courthouse) may come to blows over it, and the brawl's own machinery does the rest: the openers are the vote's ("Ballot
// box was stuffed."), onlookers who hold a side join their man (Core/BrawlSides.cs reads the topic's sides), and the town
// retells it as a fight over the vote.
//
// Why it reads true: polling was often in or beside a saloon in 1899 and the parties stood the drinks, so election night was a
// fighting night (the historians of the Gilded Age election tell of it in every county), and a fight with a cause picks its
// own sides (Tajfel's minimal groups). Cast from two already in the saloon together: nobody walks over. Off in Core.
namespace EnrageWestworld.Core
{
    public static class ElectionNight
    {
        public const string Topic = "election";
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

        /// Two men in the saloon on election night, on opposite sides of the vote: a brawl over it, or null.
        public static Reason For(Someone x, Someone y, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || !where.Saloon || where.Hour < FromHour || x == null || y == null || x.Id == y.Id || x.Woman || y.Woman)
                return null;
            if (x.P == null || y.P == null || !Today())
                return null;
            int a = QuarrelPolitics.Stance(Topic, QuarrelPolitics.GroupOf(x.P.Trade), false), b = QuarrelPolitics.Stance(Topic, QuarrelPolitics.GroupOf(y.P.Trade), false);
            if (a == 0 || b == 0 || a == b)
                return null;
            if (!Voice.Chance(x.Id + "|" + y.Id + "|" + gameDay + "|election", Percent))
                return null;
            return new Reason { Kind = "brawl", Fallback = "argue", Strength = Strength, Matter = "nothing", Over = "politics:" + Topic, Why = x.Name + " and " + y.Name + " on opposite sides of the vote, election night in the saloon" };
        }
    }
}
