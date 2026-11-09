// This week in town (TENHOUR R5; roadmap.md's idea bank; built on the paper items the town's events already send and the people ledger). On a Sunday, for the town you're nearest: a short column from what really happened there that week: who died, who married, how many fights.
// Why it reads true: a small-town paper is mostly a list of names, and finding one you know in it, or one you put there, is what makes it read like news. All lines original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class WeekColumn
    {
        public const int Days = 7;

        private static readonly string[] Fights = { "The street saw {n} fights, which is about usual.", "{n} fights, and the sheriff says he counted.", "There were {n} fights, none of them about anything." };
        private static readonly string[] OneFight = { "One fight in the street, and that a short one.", "One fight, and it wasn't much." };
        private static readonly string[] Quiet = { "Otherwise a quiet week.", "Nothing else of note.", "The weather has been remarked on." };

        /// The column, or null when there's nothing to put in it. `dead` and `married` are names; `fights` a count.
        public static string Text(string town, IList<string> dead, IList<string> married, int fights, string seed)
        {
            if (town == null || ((dead == null || dead.Count == 0) && (married == null || married.Count == 0) && fights == 0))
                return null;
            var b = new List<string>();
            if (dead != null && dead.Count > 0)
                b.Add("Buried: " + List(dead) + ".");
            if (married != null && married.Count > 0)
                b.Add("Married: " + List(married) + ".");
            if (fights == 1)
                b.Add(Voice.Pick(seed + "|one", OneFight));
            else if (fights > 1)
                b.Add(Voice.Pick(seed + "|fights", Fights).Replace("{n}", fights.ToString()));
            else
                b.Add(Voice.Pick(seed + "|quiet", Quiet));
            return "THE WEEK IN " + town.ToUpperInvariant() + ". " + string.Join(" ", b);
        }

        private static string List(IList<string> names)
        {
            var some = names.Take(4).ToList();
            string l = some.Count == 1 ? some[0] : string.Join(", ", some.Take(some.Count - 1)) + " and " + some[some.Count - 1];
            return names.Count > 4 ? l + ", and others" : l;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Fights, OneFight, Quiet })
                foreach (string l in p)
                    yield return l.Replace("{n}", "3");
        }
    }
}
