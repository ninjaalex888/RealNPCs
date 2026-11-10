// After a big brawl (FOURTEENHOUR item 12; [Scenes] BrawlAfter): the fight is a minute; who drinks with whom, and who's sore at
// whom, is the week after. What was there already: the beaten see the doctor (the patient's "ribs after a beating"), friends close
// round a man who went down (GroupActs), a death posts the neighbour's reward, a rematch. New here: the law's prisoners at the
// jail door the next morning (the roadmap's "Bail and the cells": his wife waits there, and J pays his fine), the winner stands a
// round, the barkeep's broken chairs (J at the bar pays for them), the men of opposite sides sore at each other a week (so the next
// fight among them grows sooner), and the town's news with the numbers in it. Plain C#, no game; the game side is
// Game/WestworldScript.BrawlAfter.cs. All the words are this add-on's.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class BrawlAfter
    {
        public const string JailKind = "jaildoor";
        public const int FineCents = 200, FromHour = 7, ToHour = 12, DamagePerTwoCents = 100, DamageMostCents = 500, SoreMostPairs = 20;
        public const float DoorMetres = 60f, AskMetres = 6f;

        /// What the broken chairs come to: a dollar for every two who fought, five at most.
        public static int Damage(int fighters)
        {
            return System.Math.Min(DamageMostCents, System.Math.Max(DamagePerTwoCents, fighters / 2 * DamagePerTwoCents));
        }

        /// The town's word on a big brawl: how many were in it and how many were left lying.
        public static string News(int fighters, int down, bool saloon, int hour, string seed)
        {
            string when = hour >= 5 && hour < 12 ? "this morning" : hour >= 12 && hour < 18 ? "this afternoon" : "tonight";
            string where = saloon ? "in the saloon" : "in the street";
            // laid out, not carried off: the knocked out count with the dead, and most of them get up (the review of items 1-13)
            string lying = down <= 0 ? "" : down == 1 ? ", and one laid out" : ", and " + Count(down) + " laid out";
            return Voice.Pick(seed + "|brawlnews", Count(fighters, true) + " men in a brawl " + where + " " + when + lying + ".",
                              "A brawl " + where + " " + when + ": " + Count(fighters) + " at it" + lying + ".");
        }

        private static string Count(int n, bool upper = false)
        {
            string[] w = { "no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" };
            string s = n >= 0 && n < w.Length ? w[n] : n.ToString();
            return upper ? char.ToUpper(s[0]) + s.Substring(1) : s;
        }

        public static List<InteractOption> JailRows(string prisoner, int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "jaildoor:pay", Label = "Pay " + prisoner + "'s fine (" + BrawlVerbs.Money(FineCents) + ").", Why = cashCents < FineCents ? "you haven't the money" : null },
                new InteractOption { Id = "jaildoor:no", Label = "Not your affair." }
            };
        }

        private static readonly string[] Waits = { "He's in there since last night. Two dollars, they say, and I haven't got it.", "Brought him his breakfast. They won't let him out till the fine's paid.",
                                                   "Fighting again. And the fine on top of it." };
        private static readonly string[] Thanks = { "Bless you, mister. He'll pay you back. I'll see that he does.", "You didn't have to do that. I won't forget it.", "Thank you. He's a fool, but he's my fool." };
        private static readonly string[] OutSour = { "came out of the cells at noon, sore-headed and saying nothing", "was let out at noon, his fine unpaid, in no mood to talk" };
        private static readonly string[] Damages = { "Who's paying for those chairs? Not me.", "That's a mirror and two chairs. Somebody owes me." };
        private static readonly string[] Paid = { "That's handsome of you, mister. Nobody else was going to.", "Well, I'll be. A stranger pays for other men's fun." };
        public static string PaidChairsLine(string seed) { return Voice.Pick(seed + "|paid", Paid); }
        public static string WaitsLine(string seed) { return Voice.Pick(seed + "|waits", Waits); }
        public static string ThanksLine(string seed) { return Voice.Pick(seed + "|thanks", Thanks); }
        public static string OutSourLine(string seed) { return Voice.Pick(seed + "|out", OutSour); }
        public static string DamageLine(string seed) { return Voice.Pick(seed + "|damage", Damages); }
        public static IEnumerable<string> AllLines() { foreach (string[] a in new[] { Waits, Thanks, OutSour, Damages, Paid }) foreach (string s in a) yield return s; }
    }
}
