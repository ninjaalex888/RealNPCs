// Arthur in a brawl, on J (FOURTEENHOUR item 11; [Scenes] BrawlVerbs). A fight is the one moment a street looks to a stranger
// who might settle it. While a fight goes on near him: break it up (his shout, heeded by each fighter's nature and by what they
// think of him: the fond and the afraid heed it more, the hostile less, and those who won't may turn on him), stand with the man
// losing (the one beating him turns on Arthur; that side remembers it kindly, the other doesn't), or take the bet of the onlooker
// waiting for a taker (the wagers', FIVEDAY item 1): a dollar on the other man, settled when the fight is, and a lost bet is
// owed; not paying it is a welsh he holds against you. J only: no Talk row. Plain C#, no game; the game side is
// Game/WestworldScript.BrawlVerbs.cs.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class BrawlVerbs
    {
        public const string Kind = "brawl", PayKind = "brawlpay";
        public const int StakeCents = 100, PromptMs = 7000, PayPromptMs = 30000, AgainMs = 20000;
        public const float NearMetres = 20f;

        /// How much his standing moves a man's heeding of Arthur's shout: the fond and the afraid more, the hostile less.
        public static float Standing(string regard)
        {
            return regard == "afraid" ? 0.20f : regard == "fond" ? 0.15f : regard == "hostile" ? -0.15f : 0f;
        }

        public static List<InteractOption> Rows(string loser, string backer, string backedName, string otherName, int cashCents)
        {
            var rows = new List<InteractOption>
            {
                new InteractOption { Id = "brawl:breakup", Label = "\"That's enough! Break it up!\"" },
                new InteractOption { Id = "brawl:stand", Label = "Stand with " + (loser ?? "the man losing") + "." }
            };
            if (backer != null)
                rows.Add(new InteractOption { Id = "brawl:bet", Label = "Take " + backer + "'s bet: " + Money(StakeCents) + " on " + (otherName ?? "the other man") + " against his " + (backedName ?? "man") + ".",
                                              Why = cashCents < StakeCents ? "you haven't a dollar on you" : null });
            rows.Add(new InteractOption { Id = "brawl:watch", Label = "Leave them to it." });
            return rows;
        }

        public static List<InteractOption> PayRows(string to, int cents, int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "brawlpay:pay", Label = "Pay " + to + " the " + Money(cents) + " you owe.", Why = cashCents < cents ? "you haven't the money" : null },
                new InteractOption { Id = "brawlpay:no", Label = "Walk away from it." }
            };
        }

        public static string Money(int cents) { return cents % 100 == 0 ? "$" + cents / 100 : "$" + (cents / 100.0).ToString("0.00"); }

        private static readonly string[] Shouts = { "That's enough! Break it up!", "Hey! Leave off him!", "All of you, back off!", "That's it, it's over! Go home!" };
        private static readonly string[] TakesYou = { "You're on, mister. A dollar says you're wrong.", "Done. Your dollar and mine.", "A stranger's money spends the same. Done." };
        private static readonly string[] PaysYou = { "Here. Take your dollar and be damned.", "Fair's fair. Your dollar.", "You read that right. Here." };
        private static readonly string[] WantsIt = { "That's a dollar you owe me, mister.", "My man won. Pay up.", "I'll take that dollar now." };
        private static readonly string[] Welshed = { "A welsher. I won't forget it.", "Walk off, then. The whole town'll hear it.", "Not a man of your word, are you." };
        public static string Shout(string seed) { return Pick(Shouts, seed + "|shout"); }
        public static string TakesYourBet(string seed) { return Pick(TakesYou, seed + "|takes"); }
        public static string PaysYourBet(string seed) { return Pick(PaysYou, seed + "|pays"); }
        public static string WantsHisDollar(string seed) { return Pick(WantsIt, seed + "|wants"); }
        public static string WelshedOn(string seed) { return Pick(Welshed, seed + "|welshed"); }
        public static IEnumerable<string> AllLines() { foreach (string[] a in new[] { Shouts, TakesYou, PaysYou, WantsIt, Welshed }) foreach (string s in a) yield return s; }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
