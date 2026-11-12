// "Between you and them", a page of the reader (SEAMS item 19; [Display] BetweenYouPage). The debts the SEAMS round made visible on
// the name cards ("Owes you", "Stood by you", "Robbed you") only show while that person is in front of you; the letter you carry for
// someone (Find the letter) only in a notice when you took it. This page puts them in one place: who owes you and for what, how
// long that holds; who stood by you this week; who got away with your money; whose letter is in your coat. Made when the reader
// opens, never kept (as Overheard and Who's who). The psychology: reciprocity runs on remembered debts, and a player can only
// choose to repay, collect or forgive a debt he can recall.
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnrageWestworld.Core
{
    public static class BetweenYou
    {
        public static bool On = true;   // [Display] BetweenYouPage, from the settings

        /// What the debt is for, in a few words, by the happening's kind.
        public static string ForWhat(string kind)
        {
            switch (kind)
            {
                case "defend": return "you saved them";
                case "stoodby": return "you stood with them in a fight";
                case "paidfine": return "you paid their fine";
                case Interact.SuretyKept: return "they kept the peace a week on your word";
                default: return "a good turn of yours";
            }
        }

        /// The page's body, or null when there's nothing between you and anyone. `people`: the saved states (names, marks).
        public static string Body(List<Happening> log, IDictionary<string, PersonState> people, long now)
        {
            if (!On)
                return null;
            var owes = new Dictionary<string, Happening>();
            var stood = new Dictionary<string, Happening>();
            if (log != null)
                foreach (Happening e in log)
                {
                    if (e == null || e.At > now)
                        continue;
                    if (Interact.Owing(e) && now - e.At < Interact.SavedMinutes)
                        owes[e.TargetId] = e;   // the latest for each
                    else if (e.Kind == "defend" && e.TargetId == "player" && e.DoerId != null && e.DoerId != "player" && !e.Test && now - e.At < Interact.StoodByYouMinutes)
                        stood[e.DoerId] = e;
                }
            var robbed = new List<string>();
            var letters = new List<string>();
            if (people != null)
                foreach (KeyValuePair<string, PersonState> kv in people)
                {
                    PersonState st = kv.Value;
                    if (st == null || st.Name == null)
                        continue;
                    if (st.Has("robbedyou"))
                        robbed.Add(st.Name);
                    if (st.Has(FindLetter.HeldMark) && !st.Has(FindLetter.GivenMark))
                        letters.Add(st.Name);
                }
            if (owes.Count == 0 && stood.Count == 0 && robbed.Count == 0 && letters.Count == 0)
                return null;
            var b = new StringBuilder();
            b.Append("What's owed, either way, and what you carry:\n");
            if (owes.Count > 0)
            {
                b.Append("\nThey owe you\n");
                foreach (Happening e in owes.Values.OrderByDescending(x => x.At))
                    b.Append("- ").Append(e.TargetName ?? "someone").Append(": ").Append(ForWhat(e.Kind)).Append(" (").Append(Days(Interact.SavedMinutes - (now - e.At))).Append(" more)\n");
            }
            if (stood.Count > 0)
            {
                b.Append("\nThey stood by you\n");
                foreach (Happening e in stood.Values.OrderByDescending(x => x.At))
                    b.Append("- ").Append(e.DoerName ?? "someone").Append(": fought on your side, ").Append(Ago(now - e.At)).Append('\n');
            }
            if (robbed.Count > 0)
            {
                b.Append("\nThey robbed you\n");
                foreach (string n in robbed.OrderBy(x => x))
                    b.Append("- ").Append(n).Append(": got away with your money\n");
            }
            if (letters.Count > 0)
            {
                b.Append("\nYou carry\n");
                foreach (string n in letters.OrderBy(x => x))
                    b.Append("- a letter for ").Append(n).Append('\n');
            }
            return b.ToString().TrimEnd('\n');
        }

        private static string Days(long minutes)
        {
            long d = System.Math.Max(1, (minutes + 1439) / 1440);
            return d == 1 ? "a day" : d + " days";
        }

        private static string Ago(long minutes)
        {
            long d = minutes / 1440;
            return d <= 0 ? "today" : d == 1 ? "yesterday" : d + " days ago";
        }
    }
}
