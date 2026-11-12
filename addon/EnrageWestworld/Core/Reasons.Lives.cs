// The lives round's street scenes (LIVES track 2, 2026-11-12): reasons between two people already near each other that come
// whatever they are to each other (a tie or none), asked in Reasons.Between after the rivals and before the tie's own. Each
// is chance-gated and behind its own switch, so the old reasons still win almost every pair-day. Both orders are asked: the
// one whose part comes first is A (BFirst when it's b). Plain C#.
namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        private static Reason LivesBetween(Someone a, Someone b, SceneTie tie, long gameDay, Place where)
        {
            Reason r = Either(a, b, (x, y) => SchoolFather.For(x, y, tie, gameDay, where));   // the schoolteacher and the father (LIVES item 2a)
            if (r == null)
                r = ElectionNight.For(a, b, gameDay, where);   // election night in the saloon (LIVES item 2b)
            return r;
        }

        private static Reason Either(Someone a, Someone b, System.Func<Someone, Someone, Reason> f)
        {
            Reason r = f(a, b);
            if (r != null)
                return r;
            r = f(b, a);
            if (r != null)
                r.BFirst = true;
            return r;
        }
    }
}
