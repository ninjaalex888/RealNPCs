// Grudges between towns (TENHOUR R4; roadmap.md's idea bank; built on the rumours that travel between towns). Valentine thinks little of Strawberry: a tale that crosses from one town to its rival arrives more bent than the miles would make it, and people near you say what they think of the place.
// Why it reads true: people are surest about towns they have never slept in, and a rival's misfortune is the only news that always gets repeated. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class TownRivals
    {
        public const int ExtraLoss = 18, ThoughtPct = 20;

        private static readonly string[][] Pairs =
        {
            new[] { "Valentine", "Strawberry" }, new[] { "Rhodes", "Saint Denis" }, new[] { "Annesburg", "Van Horn" }, new[] { "Armadillo", "Tumbleweed" },
        };

        /// The town this one thinks little of, or null.
        public static string RivalOf(string town)
        {
            foreach (string[] p in Pairs)
            {
                if (p[0] == town)
                    return p[1];
                if (p[1] == town)
                    return p[0];
            }
            return null;
        }

        /// How much more a tale is bent crossing between these two towns.
        public static int Loss(string from, string to)
        {
            return from != null && RivalOf(from) == to ? ExtraLoss : 0;
        }

        private static readonly string[] Thoughts =
        {
            "{r}. Wouldn't trust a man from {r} with a bucket.", "They'd believe anything over in {r}.", "My cousin went to {r}. We don't talk about it.",
            "Nothing good ever came out of {r} but the road.", "They've a saloon in {r}, I'm told. And a mule that reads.", "Heard it from a {r} man, so halve it and halve it again.",
        };

        public static string Thought(string town, string seed)
        {
            string r = RivalOf(town);
            return r == null ? null : Voice.Pick(seed + "|rival", Thoughts).Replace("{r}", r);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Thoughts)
                yield return l.Replace("{r}", "Strawberry");
        }
    }
}
