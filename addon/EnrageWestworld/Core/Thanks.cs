// Late, specific thanks (NIGHT3; the research list's "late, specific thanks", the psychologist's reciprocity): someone
// Arthur did a kindness to through the interaction menu (a drink stood, money given, a debt settled: the world log has
// each, with him as the doer and them as the one it was done to) who sees him again in town some hours later, up to a
// week on, comes over and thanks him for that very thing in their own words; the proud, who took his eleven dollars for
// their debt as a loan, pay it back. The cold give a nod and keep it to themselves; the greedy don't come. Once for each
// kindness (a mark on them). Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class ThanksOwed
    {
        public string Kind;      // drink, money, debt
        public long At;          // the game minute he did it
        public string Key;       // the mark that says it's been thanked for
    }

    public static class Thanks
    {
        public const long After = 360;          // six game hours at least: thanks later, not on the spot
        public const long Within = 7 * 1440;    // a week at most
        public const string DebtOutcome = "settled their debt";   // the menu's outcome on the help it records for a debt

        /// What the kindness was, for the words: drink, money, debt; null for anything else.
        public static string KindOf(Happening e)
        {
            if (e == null || e.DoerId != "player")
                return null;
            switch (e.Kind)
            {
                case "round": return "drink";
                case "charity": return "money";
                case "help": return e.Outcome == DebtOutcome ? "debt" : null;
                default: return null;
            }
        }

        /// The kindness this person owes him thanks for, or null: the latest he did them in the world log, at least
        /// After and at most Within ago (a test: any age within the week), not yet thanked (`thanked`: their marks).
        public static ThanksOwed Due(List<Happening> log, string id, long now, Func<string, bool> thanked, bool test = false)
        {
            if (log == null || id == null)
                return null;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e == null || e.TargetId != id || e.At > now || now - e.At >= Within || (!test && now - e.At < After))
                    continue;
                string kind = KindOf(e);
                if (kind == null)
                    continue;
                string key = "thanks:" + e.Kind + "|" + e.At;
                if (thanked != null && thanked(key))
                    continue;
                return new ThanksOwed { Kind = kind, At = e.At, Key = key };
            }
            return null;
        }

        /// Whether anyone at all is owed thanks now (the cheap look before any scan of the street).
        public static bool AnyDue(List<Happening> log, long now, bool test = false)
        {
            if (log == null)
                return false;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e != null && e.TargetId != null && e.At <= now && now - e.At < Within && (test || now - e.At >= After) && KindOf(e) != null)
                    return true;
            }
            return false;
        }

        /// How they take it: "walk" (they come over and say it), "nod" (the Cold: a look and a thought), or null (the
        /// Greedy, and anyone who fears or hates him now).
        public static string Way(string temper, string regard)
        {
            if (regard == "afraid" || regard == "hostile" || temper == "Greedy")
                return null;
            return temper == "Cold" ? "nod" : "walk";
        }

        /// The cents they pay him back: the proud, for the debt he settled (they took it as a loan).
        public static int Repays(string kind, string temper)
        {
            return kind == "debt" && temper == "Proud" ? Interact.SettleCents : 0;
        }

        /// What they say to his face, for this kindness.
        public static string Line(string kind, string temper, bool woman, string seed)
        {
            if (Repays(kind, temper) > 0)
                return woman ? "Here. Eleven dollars. I said I'd see it back to you, and I have."
                             : "Eleven dollars, every cent of it. I told you it was a loan.";
            bool shy = temper == "Coward";
            switch (kind)
            {
                case "drink":
                    return shy ? "That drink you stood me. I never said. It was kind."
                               : Pick(seed + "|tdr", "Never did thank you proper for that drink. I'm thanking you now.", "That whiskey the other day. Kind of you. I didn't say so then.", "You stood me a drink a while back. I haven't forgotten it.");
                case "money":
                    return shy ? "The money you gave me. It went further than you'd think."
                               : Pick(seed + "|tmo", "What you gave me the other day. Bought bread with it, and then some. Thank you.", "Meant to tell you: that money mattered. Folks don't give, mostly.", "That money of yours did more good than you know. I wanted you told.");
                case "debt":
                    return shy ? "You paid what I owed. I sleep now. That's all I came to say."
                               : Pick(seed + "|tde", "You paid my debt and never asked a thing back. I won't forget it.", "Slept easy every night since you settled that for me. I wanted you to know.", "Square with the man I owed, thanks to you. I tell folks so.");
                default:
                    return "Wanted to say thank you. That's all.";
            }
        }

        /// What the Cold think instead, with a nod his way.
        public static string Nod(string kind, string seed)
        {
            switch (kind)
            {
                case "drink": return "Him. Stood me a drink once. I'll stand him one back, some day.";
                case "debt": return Pick(seed + "|tnd", "Him that paid my debt. I owe him. I'll not say it out loud.", "Owe that man more than eleven dollars. Won't tell him so.");
                default: return "Him. Gave me money when he didn't have to. I keep a tally.";
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string kind in new[] { "drink", "money", "debt", "other" })
                foreach (string temper in new[] { null, "Coward", "Proud", "Hothead" })
                    for (int i = 0; i < 6; i++)
                        foreach (bool w in new[] { false, true })
                            yield return Line(kind, temper, w, "all" + i);
            foreach (string kind in new[] { "drink", "money", "debt" })
                for (int i = 0; i < 4; i++)
                    yield return Nod(kind, "all" + i);
        }
    }
}
