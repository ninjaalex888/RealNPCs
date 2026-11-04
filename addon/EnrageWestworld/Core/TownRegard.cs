// The town's regard of you, said plainly (Lex, 2026-11-04: "make the town's regard of the player a bit more clear, possibly
// adding in those details such as number of killings, and other thoughts the town has about the player to the Around Town
// box"). Everything here is read off what the add-on keeps already: the standing a town gives you this week (Reputation:
// a killer, a peacemaker, or nothing said: its known killings of yours against your kindnesses), the town's own mood
// (the ledger), how the people you know here regard you (afraid, fond, or holding something against you: the sights'
// regard), and the talk that has reached this town about you (the rumours' own words). Two things come of it: a line under
// the "Around town" box's title while you're in a town, and an item in the box when you come into a town and whenever the
// regard changes. Why it reads true: a reputation is only worth having when the player can see it move. No game here; the
// words original to this add-on. (Core/Regard.cs is the older, other thing: one person's regard; this is the town's.)
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class TownRegard
    {
        public const int PushGapMinutes = 30;   // the item again no sooner than this after the last, unless the town changed

        private static string Killings(int n)
        {
            return n == 0 ? "no killings" : n == 1 ? "one killing" : n + " killings";
        }

        private static string Kindnesses(int n)
        {
            return n == 0 ? "no kindnesses" : n == 1 ? "one kindness" : n + " kindnesses";
        }

        /// The standing in a word: "a killer", "a peacemaker", or "nothing held against you".
        public static string StandingWords(string standing)
        {
            return standing == "killer" ? "a killer" : standing == "peacemaker" ? "a peacemaker" : "nothing held against you";
        }

        /// The line under the box's title while you're in a town: "Valentine: a killer here (39 killings known, no kindnesses)".
        public static string Line(string town, string standing, int kills, int kindnesses)
        {
            if (town == null)
                return null;
            return town + ": " + StandingWords(standing) + (standing != null ? " here" : "") + " (" + Killings(kills) + " known, " + Kindnesses(kindnesses) + ")";
        }

        /// What changes the item: the standing, the counts and the town's mood.
        public static string Key(string town, string standing, int kills, int kindnesses, string mood)
        {
            return (town ?? "?") + "|" + (standing ?? "-") + "|" + kills + "|" + kindnesses + "|" + (mood ?? "-");
        }

        /// The item for the box: the standing with its counts (this week), the town's mood, how the people you know here regard you,
        /// and the talk that has reached the town. known: the people you know within reach; afraid/fond/against among them.
        public static string Item(string town, string standing, int kills, int kindnesses, string mood, int known, int afraid, int fond, int against, string talk)
        {
            string s = town + (standing == "killer" ? " knows you for a killer: " : standing == "peacemaker" ? " knows you for a peacemaker: " : " holds nothing against you: ") +
                       Killings(kills) + " of yours known this week, " + Kindnesses(kindnesses) + ".";
            if (mood != null && mood != "steady")
                s += " The town is " + mood + ".";
            if (known > 0)
            {
                var parts = new List<string>();
                if (afraid > 0) parts.Add(afraid + " afraid of you");
                if (against > 0) parts.Add(against + " holding something against you");
                if (fond > 0) parts.Add(fond + " fond of you");
                s += " Of the " + known + " you know here, " + (parts.Count == 0 ? "none feel much either way" : string.Join(", ", parts.ToArray())) + ".";
            }
            if (!string.IsNullOrEmpty(talk))
                s += " The talk here: \"" + talk.Trim().TrimEnd('.') + ".\"";
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            yield return Line("Valentine", "killer", 39, 0);
            yield return Line("Rhodes", null, 1, 2);
            yield return Line("Strawberry", "peacemaker", 0, 3);
            yield return Item("Valentine", "killer", 39, 0, "fearful", 12, 9, 2, 1, "the man they can't kill gunned down a lawman");
            yield return Item("Rhodes", null, 0, 0, "steady", 0, 0, 0, 0, null);
            yield return Item("Strawberry", "peacemaker", 0, 3, "prospering", 4, 0, 3, 0, null);
        }
    }
}
