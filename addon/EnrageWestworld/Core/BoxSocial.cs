// A box social (FIVEDAY reserve R6). On the first Saturday of the month, from three till five, a church in one of the
// settled towns raises money the old way: the young women each pack a supper in a box trimmed with ribbon and paper, the
// boxes are auctioned unopened, and whoever buys a box eats it with the woman who made it. Everyone knows whose box is whose
// (that's the point), so two young men who want the same girl bid each other up, and the town watches. The winner and she
// come away courting (a "flirt" tie), the loser envies the winner (an "envy" tie), and the church has its money. While the
// box is bid, J lets you bid against them: buy it, and you eat with her (she thinks the more of you), and the suitor you
// outbid is sore at you. Posted on the Thursday; staged like the shivaree. Behind [World] TownEvents and EventBoxSocials.
//
// Why it reads true: the box social was courtship conducted in public, with money as the signal and the town as the
// audience: costly signalling at its plainest. The bidding war is status competition between rivals in front of the woman
// and everyone she knows. Plain C#, no game; all words original. mindtest BoxSocialTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class BoxSocial
    {
        public const string Kind = "boxsocial";
        public const int FromMin = 15 * 60, ToMin = 17 * 60;
        public const int PostWeekday = 4;          // Thursday, for the Saturday
        public const int YouOverCents = 50;        // you bid fifty cents over the high bid
        public const int Affection = 6;            // what she feels for the stranger who bought her box
        public const int Sore = 4;                 // what the suitor you outbid holds against you
        public const int PromptMs = 12000;
        public static readonly string[] Towns = { "Valentine", "Strawberry", "Rhodes", "Blackwater", "Annesburg", "Armadillo" };

        /// Whether Thursday posts a box social for this Saturday: the first Saturday of the month (day of month 1-7 then).
        public static bool PostsToday(int weekday, int dayOfMonth) { return weekday == PostWeekday && dayOfMonth >= 1 && dayOfMonth + 2 <= 7; }

        /// The box's maker and the two who'll bid for it, of a town's regulars: a young woman; two young men (the one she
        /// likes the better first, by an existing warm tie, else by the seed). False if the town hasn't the three.
        public static bool Cast(List<Resident> regulars, IEnumerable<SceneTie> ties, int year, string seed, out Resident girl, out Resident winner, out Resident loser)
        {
            girl = winner = loser = null;
            System.Func<Resident, int> age = r => r.BornYear > 0 ? year - r.BornYear : 30;
            var women = regulars.Where(r => r.Woman && !r.Dead && age(r) >= 16 && age(r) <= 30).OrderBy(r => Voice.Hash(seed + r.Id)).ToList();
            var men = regulars.Where(r => !r.Woman && !r.Dead && age(r) >= 17 && age(r) <= 35).OrderBy(r => Voice.Hash(seed + r.Id)).ToList();
            if (women.Count == 0 || men.Count < 2)
                return false;
            girl = women[0];
            Resident g = girl;
            List<SceneTie> hers = ties != null ? ties.Where(t => t.Has(g.Id) && t.Warmth > 0).ToList() : new List<SceneTie>();
            winner = men.FirstOrDefault(m => hers.Any(t => t.Has(m.Id))) ?? men[0];
            Resident w = winner;
            loser = men.First(m => m != w);
            return true;
        }

        /// The bids, in cents, as the two go at it: four calls, the last the winner's.
        public static int[] Bids(string seed)
        {
            int start = 25 + (int)((Voice.Hash(seed + "|bs") & 0x7fffffff) % 4) * 25;
            return new[] { start, start + 50, start + 100, start + 175 + (int)((Voice.Hash(seed + "|be") & 0x7fffffff) % 5) * 25 };
        }

        private static readonly string[] Opens = {
            "Now here's a box with a blue ribbon on it, and I'll not say whose. Who'll start me?",
            "Paper roses on this one, gentlemen. Somebody sat up late over it. What am I bid?",
            "A heavy box, this. Fried chicken, if I'm any judge. Who opens?" };
        private static readonly string[] RivalBids = { "Fifty cents more.", "I'll raise him.", "Higher. Whatever he says, higher.", "You'll not have it." };
        private static readonly string[] Solds = { "Sold! Go and find your supper partner, son.", "Sold, and the church thanks you. She's waiting." };
        private static readonly string[] YouSold = { "Sold to the stranger! Well. That'll be talked about.", "The stranger has it! Go on, mister, she won't bite." };
        private static readonly string[] HerLines = { "It's only chicken and a bit of pie. I hope you like pie.", "I made the biscuits myself. Don't tell anyone they're hard." };
        private static readonly string[] HerToYou = { "You bought my box? I don't even know your name.", "Well. Sit down, then. Mind the ribbon." };
        private static readonly string[] LoserThoughts = { "Two dollars and a quarter. I had two dollars.", "He'll eat her pie and laugh at me after.", "Next month I'll have saved more. Next month." };
        private static readonly string[] Thoughts = { "Everybody watching me bid. Everybody.", "She smiled at the last bid. At me." };

        public static string Open(string seed) { return Voice.Pick(seed + "|bo", Opens); }
        public static string RivalBid(string seed, int cents) { return Voice.Pick(seed + "|rb", RivalBids) + " " + Slate.Dollars(cents) + "."; }
        public static string Sold(bool toYou, string seed) { return Voice.Pick(seed + "|bd", toYou ? YouSold : Solds); }
        public static string Her(bool toYou, string seed) { return Voice.Pick(seed + "|bh", toYou ? HerToYou : HerLines); }
        public static string LoserThought(string seed) { return Voice.Pick(seed + "|bl", LoserThoughts); }
        public static string WinnerThought(string seed) { return Voice.Pick(seed + "|bw", Thoughts); }

        public static string Posted(string town) { return "Box social at the church in " + (town ?? "town") + ", Saturday at three. Ladies, a box; gentlemen, your money."; }

        public static string Morning(TownEvent e, bool youBought)
        {
            if (youBought)
                return "The stranger bought " + Person.FirstOf(e.AName) + "'s box at the social, outbidding two young men.";
            return (e.BName ?? "A young man") + " bought " + Person.FirstOf(e.AName) + "'s box at the social, after a bidding war.";
        }

        public static List<InteractOption> Rows(int highCents, int cashCents)
        {
            int bid = highCents + YouOverCents;
            return new List<InteractOption>
            {
                new InteractOption { Id = "box:bid", Label = "Bid " + Slate.Dollars(bid) + " for the box.", Why = cashCents < bid ? "you haven't the money" : null },
                new InteractOption { Id = "box:no", Label = "Leave it to the young men." }
            };
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Opens, Solds, YouSold, HerLines, HerToYou, LoserThoughts, Thoughts }) foreach (string l in p) yield return l;
            foreach (string l in RivalBids) yield return l + " $1.75.";
            yield return Posted("Strawberry");
            yield return Morning(new TownEvent { AName = "Cora Hollis", BName = "Abel Grange" }, false);
            yield return Morning(new TownEvent { AName = "Cora Hollis" }, true);
        }
    }
}
