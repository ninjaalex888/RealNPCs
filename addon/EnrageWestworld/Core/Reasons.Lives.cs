// The lives round's street scenes (LIVES track 2, 2026-11-12): reasons between two people already near each other that come
// whatever they are to each other (a tie or none), asked in Reasons.Between after the rivals and before the tie's own. Each
// is chance-gated and behind its own switch, so the old reasons still win almost every pair-day; and since a lives reason
// returned here pre-empts the tie's own (a feud, a mend, a condolence), they keep out of the way (the track 2 review):
// - nothing over sweethearts' business (a courting tie, or one cooled since they last met);
// - the friendly ones (the school, the steps, the news, the mail, the tonic) never over a hostile tie, a fresh grief or a
//   hurt just done to either: those are the tie's to play;
// - a lives kind played lately between the two (Place.PlayedLately, the game's PairPlayedLately) passes to the next, and
//   at the end to the tie's own reasons, so a pair that just had its school scene isn't left with nothing for three hours.
// Rolls are on the pair's order-free key (Lives.Key), once a pair-day, so asking both orders doesn't double the odds.
// Both orders are asked: the one whose part comes first is A (BFirst when it's b). Plain C#.
namespace EnrageWestworld.Core
{
    public static class Lives
    {
        /// The pair's key whichever comes first.
        public static string Key(Someone x, Someone y)
        {
            return string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
        }
    }

    public static partial class Reasons
    {
        private static Reason LivesBetween(Someone a, Someone b, SceneTie tie, long gameDay, long now, Place where)
        {
            if (tie != null && (Ties.Sweethearts(tie) || Ties.Cooled(tie)))
                return null;   // their own business comes first
            bool friendly = (tie == null || tie.Warmth >= 0) && a.Grief == null && b.Grief == null && a.Hurt == null && b.Hurt == null;
            Reason r = null;
            if (friendly)
                r = Lately(a, b, where, Either(a, b, (x, y) => SchoolFather.For(x, y, tie, gameDay, where)));   // the schoolteacher and the father (LIVES item 2a)
            if (r == null && MayFight(a, b, where, "election" + gameDay))
                r = Lately(a, b, where, Either(a, b, (x, y) => ElectionNight.For(x, y, gameDay, where)));   // election night in the saloon (LIVES item 2b)
            if (r == null && friendly)
                r = Lately(a, b, where, b.Backslid && !a.Backslid ? Either(a, b, (x, y) => ChurchSteps.For(x, y, gameDay, where)) : Either(b, a, (x, y) => ChurchSteps.For(x, y, gameDay, where), true));   // talk on the church steps, the backslider as B (LIVES item 2c)
            if (r == null && MayFight(a, b, where, "dance" + gameDay))
                r = Lately(a, b, where, Either(a, b, (x, y) => DanceFight.For(x, y, tie, gameDay, where)));   // the fight at the dance (LIVES item 2d)
            if (r == null && friendly)
                r = Lately(a, b, where, NewsOverFence.For(a, b, gameDay, where));   // the town's news over the fence (LIVES item 2e)
            if (r == null && friendly)
                r = Lately(a, b, where, Either(a, b, (x, y) => MailDay.For(x, y, gameDay, where)));   // a letter off the train (LIVES item 2f)
            if (r == null && friendly)
                r = Lately(a, b, where, Either(a, b, (x, y) => TonicAfter.For(x, y, gameDay, now, where)));   // the medicine show's bottle, the days after (LIVES item 2g)
            return r;
        }

        /// The reason, unless the two had that kind lately (then null, and the next is asked).
        private static Reason Lately(Someone a, Someone b, Place where, Reason r)
        {
            return r != null && where.PlayedLately != null && where.PlayedLately(a.Id, b.Id, r.Kind) ? null : r;
        }

        /// f(a, b), else f(b, a) with BFirst. `swapped`: the pair was given as (b, a), so the first order is BFirst.
        private static Reason Either(Someone a, Someone b, System.Func<Someone, Someone, Reason> f, bool swapped = false)
        {
            Reason r = f(a, b);
            if (r != null)
            {
                if (swapped)
                    r.BFirst = true;
                return r;
            }
            r = f(b, a);
            if (r != null && !swapped)
                r.BFirst = true;
            return r;
        }
    }
}
