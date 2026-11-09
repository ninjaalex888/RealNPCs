// Roundup season (TENHOUR R1; roadmap.md's idea bank; built on the payday pull and the calendar's own words for the season). In the first fortnight after each roundup (July and November), the cattle towns fill with hands who have two months' pay, and one comes back hurt.
// Why it reads true: seasonal work makes a whole class of men rich for a fortnight, and everyone in town knows the date. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Roundup
    {
        public const int HurtDays = 14;
        public static readonly string[] Towns = { "Valentine", "Emerald Ranch", "Blackwater", "Armadillo" };

        public static bool In(string town) { return town != null && System.Array.IndexOf(Towns, town) >= 0; }

        /// The fortnight after a roundup, when the pay is out.
        public static bool Paid(int month, int day) { return (month == 7 || month == 11) && day >= 1 && day <= 14; }

        public static bool FirstDay(int month, int day) { return (month == 7 || month == 11) && day == 1; }

        public static string HurtKey(string id) { return "rounduphurt:" + id; }

        /// The pay in their pockets, for the hour's choice: the saloon, the cards and the store pull, the work less.
        public static float Lean(string act)
        {
            switch (act)
            {
                case "saloon": return 0.2f;
                case "drink": return 0.12f;
                case "cards": return 0.1f;
                case "store": return 0.06f;
                case "work": return -0.15f;
                default: return 0f;
            }
        }

        private static readonly string[] PaidThoughts = { "Two months' pay and a bath. In that order, or the other.", "Every saloon from here to the river knows what day it is.", "Spent half of it and I've only been here an hour.", "I'll keep some back this time. I say that every time." };
        private static readonly string[] HurtThoughts = { "Horse came over on me at the branding. Still can't lift this arm.", "Ribs. Every breath's an argument.", "Two months' pay, and half of it goes to the doctor." };
        private static readonly string[] Home = { "The roundup's in at {t}. The hands are paid, and the saloon knows it.", "Hands are in at {t} with two months' wages and no sense." };
        private static readonly string[] HomeHurt = { "The roundup's in at {t}, and {n} came back with a broken collarbone.", "{n} rode into {t} with the rest of the hands, one arm strapped. The horse came off better." };

        public static string PaidThought(string seed) { return Voice.Pick(seed + "|rp", PaidThoughts); }
        public static string HurtThought(string seed) { return Voice.Pick(seed + "|rh", HurtThoughts); }

        public static string InLine(string town, string hurt, string seed)
        {
            return Voice.Pick(seed + "|ri", hurt != null ? HomeHurt : Home).Replace("{t}", town).Replace("{n}", Person.FirstOf(hurt) ?? "One");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { PaidThoughts, HurtThoughts, Home, HomeHurt })
                foreach (string l in p)
                    yield return l.Replace("{t}", "Valentine").Replace("{n}", "Abe");
        }
    }
}
