// A cold snap (TENHOUR R3; roadmap.md's idea bank; built on the calendar's seasons and the fever's deaths off screen). Twice a winter, five days of it, in the high country: people huddle by the stove, the store runs out of coal oil, and now and then an old man doesn't wake.
// Why it reads true: cold is the one thing nobody in 1899 could buy their way clear of, and it comes for the old in their own beds. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ColdSnap
    {
        public const int Days = 5, DeathPct = 35;
        public static readonly string[] Towns = { "Colter", "Wapiti", "Strawberry" };

        public static bool In(string town) { return town != null && System.Array.IndexOf(Towns, town) >= 0; }

        // the day of the winter, from the first of December: -1 outside it
        private static int WinterDay(int month, int day)
        {
            return month == 12 ? day - 1 : month == 1 ? 30 + day : month == 2 ? 61 + day : -1;
        }

        private static int WinterOf(int year, int month) { return month == 12 ? year : year - 1; }

        // how many days into this spell it is, or -1: two spells a winter, on days fixed by the winter
        private static int SpellDay(int year, int month, int day)
        {
            int d = WinterDay(month, day);
            if (d < 0)
                return -1;
            int w = WinterOf(year, month);
            int first = (int)(Voice.Hash("snap|" + w + "|a") % 38), second = 45 + (int)(Voice.Hash("snap|" + w + "|b") % 38);
            int intoFirst = d - first, intoSecond = d - second;
            return intoFirst >= 0 && intoFirst < Days ? intoFirst : intoSecond >= 0 && intoSecond < Days ? intoSecond : -1;
        }

        public static bool Now(int year, int month, int day) { return SpellDay(year, month, day) >= 0; }

        public static bool FirstDay(int year, int month, int day) { return SpellDay(year, month, day) == 0; }

        private static readonly string[] Thoughts = { "Can't feel my feet. Haven't since Tuesday.", "Burned the fence last night. It's that or the chairs.", "Nobody's got coal oil. Nobody. I asked at the store and he laughed.", "Sit close to the stove and don't think about the woodpile.", "My water froze in the jug, and the jug was by the bed." };
        private static readonly string[] Comes = { "It's gone bitter at {t}. Stoves going day and night, and the store's out of coal oil.", "The cold came down on {t} in the night. Nobody's going further than the woodpile." };
        private static readonly string[] Takes = { "Old {n} at {t} didn't wake. The stove had gone out.", "They found old {n} of {t} in his chair, the fire dead and the door still barred." };

        public static string Thought(string seed) { return Voice.Pick(seed + "|cold", Thoughts); }
        public static string Line(string town, string seed) { return Voice.Pick(seed + "|comes", Comes).Replace("{t}", town); }
        public static string DeathLine(string town, string name, string seed) { return Voice.Pick(seed + "|takes", Takes).Replace("{t}", town).Replace("{n}", Person.FirstOf(name) ?? "One"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Thoughts) yield return l;
            foreach (string l in Comes) yield return l.Replace("{t}", "Colter");
            foreach (string l in Takes) yield return l.Replace("{t}", "Colter").Replace("{n}", "Abe");
        }
    }
}
