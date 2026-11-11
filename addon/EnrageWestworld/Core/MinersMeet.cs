// The miners' meeting (DEPTH reserve R7; [Scenes] MinersMeet, on; roadmap "Company town"). At Annesburg after the shift on a
// weekday (six to nine), when four or more of the town's working men are already standing within 10 m of one another and two
// other men stand off 8 to 30 m (the company's men, by the look of it), the boldest of them speaks up about the scrip and the
// weights; one backs him, one doubts; he asks for hands, each answers by his nature, and it's carried or it isn't. The company's
// men watch and think it. Nobody is walked anywhere or given an order: they say it where they stand. Once a game week. Told in
// the town's Around town box. Why it reads true: before the mine workers' union came to a coal town, the men met where they
// were, after the shift, and knew they were watched; the vote was the dangerous part. Plain C#; the game side is
// Game/WestworldScript.MinersMeet.cs.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class MinersMeet
    {
        public static bool On = true;   // [Scenes] MinersMeet, from the settings
        public const string Town = "Annesburg";
        public const int FromHour = 18, ToHour = 21, Least = 4, Most = 8, CompanyLeast = 2;
        public const float RingMetres = 10f, CompanyNear = 8f, CompanyFar = 30f, HearMetres = 25f;

        /// After the shift on a weekday (weekday 0 Sunday), in the company's town.
        public static bool Hour(string town, int hour, int weekday) { return On && town == Town && weekday > 0 && hour >= FromHour && hour < ToHour; }

        /// Who speaks up: the hot-tempered and the proud first, then by the seed.
        public static int Speaker(IList<string> tempers, string seed)
        {
            int best = 0, bestKey = int.MinValue;
            for (int i = 0; i < tempers.Count; i++)
            {
                string t = tempers[i];
                int key = (t == "Hothead" || t == "Proud" ? 2000 : t == "Loyal" ? 1000 : t == "Coward" ? -1000 : 0) + (Voice.Hash(seed + "|sp" + i) & 0x3ff);
                if (key > bestKey) { bestKey = key; best = i; }
            }
            return best;
        }

        /// One man's hand: up (true) or not, by his temper. Seeded.
        public static bool Aye(string temper, string seed)
        {
            int pct = temper == "Hothead" || temper == "Proud" || temper == "Loyal" ? 85 : temper == "Coward" ? 20 : temper == "Greedy" ? 40 : 60;
            return Voice.Chance(seed + "|aye", pct);
        }

        /// What they vote on, by the seed: to send for the union's man, or to go to the superintendent together about the weights.
        public static string Motion(string seed) { return (Voice.Hash(seed + "|mo") & 1) == 0 ? "union" : "weights"; }

        private static readonly string[] Opens =
        {
            "Listen. They short us on the scale every week, and they pay us in paper. I say we stop taking it.",
            "Eleven tons and a debt at the store. That's what a man gets here. We have to do something, boys.",
            "My cousin is in the Illinois fields. They struck in ninety-seven, and they won. Why not here?",
        };
        private static readonly string[] Backs = { "He's right. My weights came up two hundred short on Friday.", "I'm with him. I'm tired of being robbed with a smile.", "Somebody had to say it." };
        private static readonly string[] Doubts = { "And who feeds my children when they let us all go?", "Keep your voice down. Look who's standing over there.", "They'll have our names by morning." };
        private static readonly Dictionary<string, string> Asks = new Dictionary<string, string>
        {
            { "union", "Hands up, then. Who's for sending to the union for a man?" },
            { "weights", "Hands up. Who'll go with me to the superintendent about the weights?" },
        };
        private static readonly string[] Ayes = { "Aye.", "I'm for it.", "Count me.", "Aye, God help me." };
        private static readonly string[] Nays = { "Not me.", "I can't. I've a family.", "No.", "I'll wait and see." };
        private static readonly string[] Company =
        {
            "Names. The superintendent will want names, and I have four of them.",
            "There it is. Write it down before you forget a face.",
            "Talk is cheap. Tomorrow we'll see which of them shows up for work.",
        };

        public static string Ask(string motion) { return Asks[motion]; }
        public static string Open(string seed) { return Pick(Opens, seed + "|o"); }
        public static string Back(string seed) { return Pick(Backs, seed + "|b"); }
        public static string Doubt(string seed) { return Pick(Doubts, seed + "|d"); }
        public static string Answer(bool aye, string seed) { return Pick(aye ? Ayes : Nays, seed + "|a"); }
        public static string CompanyThinks(string seed) { return Pick(Company, seed + "|c"); }
        public static bool Carried(int ayes, int of) { return ayes * 2 > of; }

        /// The speaker's word on the count.
        public static string Tally(int ayes, int of)
        {
            string n = Count(ayes);
            n = char.ToUpperInvariant(n[0]) + n.Substring(1);   // it opens the sentence
            return Carried(ayes, of) ? n + " for it, " + Count(of - ayes) + " against. Then that's carried, and we stand together."
                                     : n + " for it, " + Count(of - ayes) + " against. Then we wait. And God help us.";
        }

        /// The town's news of it.
        public static string News(string motion, int ayes, int of)
        {
            string what = motion == "union" ? "to send to the union for an organiser" : "to go together to the superintendent about the weights";
            return Carried(ayes, of) ? "Miners stood together after the shift and voted " + Count(ayes) + " to " + Count(of - ayes) + " " + what + ". The company's men were watching."
                                     : "Miners talked after the shift of standing together, and " + Count(of - ayes) + " of " + Count(of) + " wouldn't put a hand up. The company's men were watching.";
        }

        private static readonly string[] Numbers = { "none", "one", "two", "three", "four", "five", "six", "seven", "eight" };
        private static string Count(int n) { return n >= 0 && n < Numbers.Length ? Numbers[n] : n.ToString(); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Opens, Backs, Doubts, Ayes, Nays, Company }) foreach (string l in a) yield return l;
            foreach (string l in Asks.Values) yield return l;
            for (int of = Least; of <= Most; of++) for (int ayes = 0; ayes <= of; ayes++) { yield return Tally(ayes, of); yield return News("union", ayes, of); yield return News("weights", ayes, of); }
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
