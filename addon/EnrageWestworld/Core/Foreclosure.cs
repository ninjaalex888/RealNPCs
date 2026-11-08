// The bank's note: a foreclosure sale in the street (FIVEDAY reserve R3). When a resident's worry over the bank ends badly
// (stage three, not good: InnerLife's "bank" worry), the bank sells them up the same day: from ten till two, at the town's
// "bankdoor" spot (else its "mainstreet"), the sheriff's man stands on a crate and calls their things one by one, the plough,
// the milch cow, the parlour organ, and the neighbours stand round. Some bid low; two sales in five the neighbours won't bid
// against the family at all, and the bank's own man takes each lot at the reserve. The family watches if they're near. You
// may buy one lot back for them (J, a few dollars): a kindness the whole street sees. Half of these families leave on the
// train after (Trains.Leaves, already). Once a day a town, while you're near. Behind [World] Foreclosures.
//
// Why it reads true: the 1890s were the decade of the farm mortgage and the falling price, the decade the Farmers'
// Alliance was born of. A sheriff's sale was public, and neighbours knew they could be next: shame for the family, fear and
// solidarity for the rest. A refusal to bid was a protest a whole town could make without a word. Plain C#, no game; all
// words original. mindtest ForeclosureTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Foreclosure
    {
        public const string Kind = "foreclosure";
        public const int FromMin = 10 * 60, ToMin = 14 * 60;
        public const int Lots = 3, MostMen = 7, LeastMen = 3;
        public const int RefuseChance = 40;      // percent of sales the neighbours won't bid against the family
        public const int PromptMs = 12000;

        public static bool Hour(int minuteOfDay) { return minuteOfDay >= FromMin && minuteOfDay < ToMin; }

        /// Whether this worry, today, is the bank selling them up.
        public static bool SoldUp(string worry, int stage, bool good) { return worry == "bank" && stage >= 3 && !good; }

        public const int PublicChance = 25;      // percent of the bank's bad ends that come to a sheriff's sale in the street (the rest: a room over the store, a deal with the bank)

        public static bool Public(string seed) { return Voice.Chance(seed + "|public", PublicChance); }

        public static bool NeighboursRefuse(string seed) { return Voice.Chance(seed + "|refuse", RefuseChance); }

        public static readonly string[] Things = { "the plough", "the milch cow", "the parlour organ", "the bedstead", "the spring wagon", "the cook stove", "the mantel clock", "the team of mules" };

        /// The lot's thing and its price in cents (a low one: a forced sale fetches a third of the worth).
        public static string Thing(string seed, int lot) { return Voice.Pick(seed + "|thing|" + lot, Things); }
        public static int PriceCents(string seed, int lot) { return (2 + (int)((Voice.Hash(seed + "|fp|" + lot) & 0x7fffffff) % 7)) * 100; }

        private static readonly string[] Openings = {
            "The court and the bank have spoken: the goods of {f}. Cash only, gentlemen.",
            "Sheriff's sale. The property of {f}, sold to satisfy the note. Who'll start me?",
            "All right, step up. {f}'s goods, by order. I don't like it any more than you do." };
        private static readonly string[] Calls = {
            "{t}. Sound as a dollar. What am I bid?", "Here's {t}. Somebody open, now.", "{t}, and a good one. Who'll give me a dollar?" };
        private static readonly string[] Bids = { "A dollar.", "Two.", "Two and a half, and God forgive me.", "Three dollars." };
        private static readonly string[] Silences = {
            "Nobody? Nobody at all? Then the bank takes it at the reserve.", "Not a bid in the street. All right. The bank's man has it.",
            "I'll not stand here all day. No bids, the bank keeps it." };
        private static readonly string[] Sold = { "Sold. Next lot.", "Sold, and that's that. Next." };
        private static readonly string[] FamilyThoughts = {
            "That was my mother's organ. She played it Sundays.", "Every neighbour I have standing there with his hat off.",
            "Thirty years on that land. The sale took the morning.", "Don't cry. Not in the street. The children are watching." };
        private static readonly string[] NeighbourThoughts = {
            "Next year it could be my name on the bill.", "I'll not bid. Let the bank buy its own plunder.",
            "Got the mules for two dollars. Hate myself for it.", "Same bank holds my note. Same bank." };
        private static readonly string[] BoughtBack = {
            "There. That one goes back to the family.", "Wrap it up and give it back to them. Paid." };
        private static readonly string[] FamilyThanks = {
            "You bought it back? For us? Mister, I don't know you.", "It's only the clock, and it's everything. Thank you." };

        public static string Opening(string family, string seed) { return Voice.Pick(seed + "|fo", Openings).Replace("{f}", family ?? "the family"); }
        public static string Call(string thing, string seed) { return Up(Voice.Pick(seed + "|fc", Calls).Replace("{t}", thing ?? "this")); }
        public static string Bid(string seed) { return Voice.Pick(seed + "|fb", Bids); }
        public static string Silence(string seed) { return Voice.Pick(seed + "|fs", Silences); }
        public static string SoldLine(string seed) { return Voice.Pick(seed + "|fd", Sold); }
        public static string FamilyThought(string seed) { return Voice.Pick(seed + "|ft", FamilyThoughts); }
        public static string NeighbourThought(string seed) { return Voice.Pick(seed + "|fn", NeighbourThoughts); }
        public static string BoughtBackLine(string seed) { return Voice.Pick(seed + "|fbb", BoughtBack); }
        public static string Thanks(string seed) { return Voice.Pick(seed + "|fth", FamilyThanks); }

        public static string Summary(string family, string town, bool refused)
        {
            return refused ? "The bank sold up " + (family ?? "a family") + " in " + (town ?? "town") + " today. Not one neighbour would bid."
                           : "The bank sold up " + (family ?? "a family") + " in " + (town ?? "town") + " today, for next to nothing.";
        }

        public static List<InteractOption> Rows(string thing, int cents, int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "foreclosure:buy", Label = "Buy " + (thing ?? "it") + " back for them (" + Slate.Dollars(cents) + ").", Why = cashCents < cents ? "you haven't the money" : null },
                new InteractOption { Id = "foreclosure:no", Label = "Let it go." }
            };
        }

        private static string Up(string s) { return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Openings) yield return l.Replace("{f}", "Abel Grange");
            foreach (string t in Things) foreach (string l in Calls) yield return Up(l.Replace("{t}", t));
            foreach (string[] p in new[] { Bids, Silences, Sold, FamilyThoughts, NeighbourThoughts, BoughtBack, FamilyThanks }) foreach (string l in p) yield return l;
            yield return Summary("Abel Grange", "Strawberry", true);
            yield return Summary("Abel Grange", "Strawberry", false);
        }
    }
}
