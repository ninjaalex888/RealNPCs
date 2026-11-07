// Sale day at the Valentine stockyards (FIVEDAY item 30). Every Tuesday from ten till half past one, with the town's
// "stockyards" spot recorded and you near it, the named men about the pens gather to the auctioneer. He calls three lots
// (four in the autumn sale, September to November, when the herds come off the range), the buyers bid against each
// other, and each lot is knocked down to the boldest. One lot in five (two in five in the autumn glut) goes for a
// ruinous price: its rancher is ruined, and carries the debt worry for a week (with [Mind] WorriesShown). After, the
// drovers and hands who were there drink their pay from one o'clock (a lean on their choices), and the ruined man
// drinks for another reason. Behind [World] SaleDay.
//
// Why it reads true: a year's work sold in a minute, by a stranger's voice, at a price the seller can't refuse once the
// hammer falls. The auction is a public verdict on a man's whole year, and loss looms larger than the same gain
// (prospect theory): one bad lot outweighs every good one in his mind. The drovers' spree is the other side of it, the
// same windfall accounting as payday: money that came all at once leaves all at once. 1899's beef trust had driven
// prices down for a decade, and a falling market made every sale day a gamble. Plain C#, no game; all words original.
// mindtest SaleDayTest.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class SaleDay
    {
        public const string Town = "Valentine";
        public const int Weekday = 2;                              // Tuesday (0 Sunday)
        public const int FromMin = 10 * 60, ToMin = 13 * 60 + 30;
        public const int MostMen = 8, LeastMen = 4;                // the auctioneer, the rancher and at least two to bid
        public const int DrinkFromHour = 13;                       // the pay is spent from one o'clock
        public const int RuinDays = 7;                             // the debt worry it leaves

        public static bool Now(int weekday, int minuteOfDay) { return weekday == Weekday && minuteOfDay >= FromMin && minuteOfDay < ToMin; }

        /// The autumn sale: the herds off the summer range, more lots, and a glut that sinks the price.
        public static bool Autumn(int month) { return month >= 9 && month <= 11; }

        public static int Lots(int month) { return Autumn(month) ? 4 : 3; }

        /// Whether this sale's last lot goes for a ruinous price: one in five, two in five in the autumn glut.
        public static bool Ruinous(string seed, int month) { return Voice.Chance(seed + "|ruin", Autumn(month) ? 40 : 20); }

        /// A lot's price in dollars a head: a steer fetched thirty to forty-odd in 1899, a ruinous lot half that.
        public static int Price(string seed, int lot, bool ruinous)
        {
            int u = (Voice.Hash(seed + "|price|" + lot) & 0x7fffffff) % 13;
            return ruinous ? 14 + u / 2 : 30 + u;
        }

        public static int Head(string seed, int lot) { return 20 + ((Voice.Hash(seed + "|head|" + lot) & 0x7fffffff) % 7) * 10; }

        /// Who wins a lot, of the bidders (indexes into their list): the boldest, a little luck (-1: no bidders).
        public static int Winner(List<Traits> bidders, string seed, int lot)
        {
            int best = -1;
            float bestS = float.MinValue;
            for (int i = 0, n = bidders.Count; i < n; i++)
            {
                Traits t = bidders[i];
                float s = (t != null ? 0.5f * t.Z[Traits.Assertiveness] + 0.3f * t.Z[Traits.Risk] - 0.2f * t.Z[Traits.Anxiety] : 0f) + ((Voice.Hash(seed + "|bid|" + lot + "|" + i) & 0xff) / 255f - 0.5f) * 1.2f;
                if (s > bestS)
                {
                    bestS = s;
                    best = i;
                }
            }
            return best;
        }

        /// How the sale leans a choice (Choosing's act ids). mood: "paid" (a drover or hand with his pay), "ruined", or null.
        public static float Lean(string mood, int hour, string act)
        {
            if (mood == null || hour < DrinkFromHour)
                return 0f;
            if (mood == "ruined")
                return act == "drink" ? 0.22f : act == "saloon" ? 0.12f : act == "showoff" ? -0.15f : act == "greet" ? -0.08f : act == "work" ? -0.1f : 0f;
            return act == "saloon" ? 0.2f : act == "drink" ? 0.12f : act == "cards" ? 0.1f : act == "work" ? -0.15f : 0f;
        }

        private static readonly string[] Openings = {
            "Lot {l}, gentlemen. {h} head of good grass-fed steers. Who'll start me?",
            "Step up to the rail and look at them. Sound stock, every one. What am I bid?",
            "Here's a fine bunch off the high range. Who opens? Don't be shy with it.",
            "Lot {l}, and a good one. {h} head. Look at the shoulders on them. Who starts?" };
        private static readonly string[] Bids = {
            "{n}.", "I'll go {n}.", "{n}, and that's my limit.", "Here. {n}.", "{n} a head.", "{n}, and no further." };
        private static readonly string[] KnockDown = {
            "Going once. Going twice. Sold, to the gentleman on the rail.",
            "All done? All through? Sold, and thank you, sir.",
            "No more? Then they're yours. Next lot." };
        private static readonly string[] RuinCall = {
            "Is that all I hear for them? That low? Going, then.",
            "Nobody higher? Nobody at all? Then it's sold, and I'm sorry for it.",
            "Market's flooded this week, gentlemen, I know it. Sold low." };
        private static readonly string[] RuinedThought = {
            "That price for my steers. The note at the bank wants more than that.",
            "A whole year of grass and water, gone for half its price.",
            "I'll have to tell her tonight. I don't know how.",
            "They stood there and let it go that low. Every one of them knew." };
        private static readonly string[] PaidThought = {
            "Paid off. First drink's on me, second's on the boss.",
            "Two months in the saddle and it's all in my pocket at once.",
            "Bath, shave, a hot meal, and then we'll see.",
            "The trail's over. Let it stay over till Thursday." };
        private static readonly string[] BuyerThought = {
            "Paid too much for that last lot. Can't let the Chicago men see it.",
            "Good stock at a fair price. The railroad will want them by Friday.",
            "Bought them cheap. Somebody's hurting tonight, and it isn't me." };
        private static readonly string[] Words = {
            "{p} had his herd sold for nothing at the stockyards. Poor man.",
            "Did you hear what {p}'s steers fetched? Half what they cost him.",
            "{p} was drinking alone after the sale. Can't say I blame him." };

        public static string Opening(string seed, int lot, int head) { return Voice.Pick(seed + "|open|" + lot, Openings).Replace("{l}", Word(lot + 1)).Replace("{h}", Up(Word(head))); }
        public static string Bid(string seed, int lot, int i, int dollars) { return Up(Voice.Pick(seed + "|b|" + lot + "|" + i, Bids).Replace("{n}", Word(dollars))); }

        private static readonly string[] Ones = { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        private static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

        /// A number as it's said, 1 to 99 ("thirty-two").
        public static string Word(int n)
        {
            if (n < 1 || n > 99)
                return n.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return n < 20 ? Ones[n] : Tens[n / 10] + (n % 10 != 0 ? "-" + Ones[n % 10] : "");
        }

        private static string Up(string s) { return string.IsNullOrEmpty(s) || s.Length < 2 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1); }
        public static string Sold(string seed, int lot, bool ruinous) { return ruinous ? Voice.Pick(seed + "|rc", RuinCall) : Voice.Pick(seed + "|kd|" + lot, KnockDown); }
        public static string Ruined(string seed) { return Voice.Pick(seed + "|rt", RuinedThought); }
        public static string Paid(string seed) { return Voice.Pick(seed + "|pt", PaidThought); }
        public static string Bought(string seed) { return Voice.Pick(seed + "|bt", BuyerThought); }
        public static string Talk(string rancher, string seed) { return Voice.Pick(seed + "|tk", Words).Replace("{p}", rancher ?? "Somebody"); }

        /// The box's word of the sale: the lots and what they fetched.
        public static string Summary(int lots, int head, int bestPrice, string ruinedName, int ruinPrice)
        {
            string s = "Sale day at the stockyards: " + lots + " lots, " + head + " head, the best at $" + bestPrice + " a head.";
            return ruinedName != null ? s + " " + ruinedName + "'s herd went for $" + ruinPrice + "." : s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Openings) yield return l.Replace("{l}", "four").Replace("{h}", "Seventy");
            foreach (string l in Bids) yield return Up(l.Replace("{n}", "thirty-seven"));
            foreach (string[] p in new[] { KnockDown, RuinCall, RuinedThought, PaidThought, BuyerThought }) foreach (string l in p) yield return l;
            foreach (string l in Words) yield return l.Replace("{p}", "Abel Grange");
        }
    }
}
