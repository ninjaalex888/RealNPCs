// The shape-up at the Saint Denis wharf (FIVEDAY item 29). At dawn (five till half past six), the men looking for a day's
// work crowd the wharf (the town's "wharf" spot) and the foreman picks his gang. About half are picked, the steadier
// first. The picked go to it, and the passed-over drift to the saloon for the day (a lean on their choices). The hottest of
// the passed-over may take it out on the last man picked: hard words at once, and a rematch plan between them (the plans'
// fight, with every brawl gate). Once a day, when you're near the wharf. Behind [World] ShapeUp.
//
// Why it reads true: the shape-up was how New Orleans and every port hired by the day in 1899. A foreman chose from a
// crowd at the gate, the work went to the strong and the favoured, and being passed over with the whole crowd watching was a
// daily shame. Scarcity breeds rivalry, and those left over turned on each other, not on the foreman. Plain C#, no game;
// all words original. mindtest ShapeUpTest.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class ShapeUp
    {
        public const string Town = "Saint Denis";
        public const int FromMin = 5 * 60, ToMin = 6 * 60 + 30;
        public const int MostMen = 8, LeastMen = 3;

        public static bool Hour(int minuteOfDay) { return minuteOfDay >= FromMin && minuteOfDay < ToMin; }

        /// The picked, of those waiting (in their order): about half, the steadier and the deferent first, a little luck.
        public static List<int> Pick(List<Traits> men, string seed)
        {
            int n = men.Count, k = Math.Max(1, n / 2);
            return Enumerable.Range(0, n)
                .OrderByDescending(i => (men[i] != null ? 0.5f * men[i].Z[Traits.Order] + 0.3f * men[i].Z[Traits.Deference] + 0.2f * men[i].Z[Traits.SelfControl] : 0f) + ((Voice.Hash(seed + "|" + i) & 0xff) / 255f - 0.5f))
                .Take(k).ToList();
        }

        /// Which of the passed-over takes it out on the last man picked (-1: none): the angriest, if angry enough.
        public static int Bitter(List<Traits> men, List<int> picked, string seed)
        {
            int best = -1;
            float bestA = 0.6f;
            for (int i = 0, all = men.Count; i < all; i++)
            {
                if (picked.Contains(i) || men[i] == null)
                    continue;
                float a = men[i].Z[Traits.Anger] + 0.5f * men[i].Z[Traits.Assertiveness] - 0.4f * men[i].Z[Traits.SelfControl];
                if (a > bestA)
                {
                    bestA = a;
                    best = i;
                }
            }
            return best >= 0 && Voice.Chance(seed + "|bitter", 60) ? best : -1;
        }

        /// The day passed over leans them (Choosing's act ids).
        public static float Lean(bool passedOver, string act)
        {
            if (!passedOver)
                return 0f;
            return act == "saloon" ? 0.2f : act == "drink" ? 0.15f : act == "cards" ? 0.08f : act == "work" ? -0.25f : act == "home" ? -0.05f : 0f;
        }

        private static readonly string[] Calls = { "Twelve men for the cotton boat. You. You. And you, the big one.", "I want the ones who came sober. You, you, you.", "Same as yesterday, and you, and you. The rest of you, go home.", "Hold up your hands if you can lift a bale. Not you. You." };
        private static readonly string[] Picked = { "Work. Thank God. The rent's paid this week.", "A day's wage. I'll take it and not look back at them.", "Picked. Don't smile. They're watching." };
        private static readonly string[] Passed = { "Passed over again. Third day running.", "He looked right at me and took the man behind.", "I'll go stand at the next gate. Or the saloon. Same thing.", "My children ate yesterday. Today I don't know." };
        private static readonly string[] Words = { "That was my place, {p}. He was pointing at me.", "You crawled to him, {p}. I saw you.", "Enjoy your wage, {p}. You'll earn it twice before I'm done." };
        private static readonly string[] Answers = { "Take it up with the foreman.", "He pointed where he pointed.", "Go home and sleep it off." };

        public static string Call(string seed) { return Voice.Pick(seed + "|call", Calls); }
        public static string PickedThought(string seed) { return Voice.Pick(seed + "|pk", Picked); }
        public static string PassedThought(string seed) { return Voice.Pick(seed + "|ps", Passed); }
        public static string BitterWords(string pickedFirst, string seed) { return Voice.Pick(seed + "|bw", Words).Replace("{p}", pickedFirst ?? "you"); }
        public static string Answer(string seed) { return Voice.Pick(seed + "|ba", Answers); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Calls, Picked, Passed, Answers }) foreach (string l in p) yield return l;
            foreach (string l in Words) yield return l.Replace("{p}", "Abel");
        }
    }
}
