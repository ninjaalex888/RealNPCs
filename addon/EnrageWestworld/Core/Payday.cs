// Payday Saturday (FIVEDAY item 11; roadmap.md's idea bank: "Saturday afternoon pay fills the saloon and the barber's
// chair. Debts are called in, and wives wait at the saloon door"). In 1899 most hands, labourers and clerks were paid on
// Saturday, many at noon when the week's work stopped. From noon till eight on a Saturday in town, the working people's
// day bends to the money in their pockets: the saloon, the card table, the store and a drink pull harder, the work less
// (Choosing); and a new street scene, the wages: a woman stops a man on his way to the saloon for the week's money before it's gone (Scenes),
// and comes away with all of it, half, a promise or nothing, as the seed falls. Who she is to him (a wife,
// a sister, a landlady) the words leave unsaid; only what the money is for.
//
// Why it reads true: a weekly windfall is spent as a windfall (mental accounting): the pay packet's first hours are its
// loosest, which every saloon keeper, card sharp and creditor of the period knew, as did the families who met the men at
// the gate. Behind [Scenes] Payday. Plain C#, no game; all words original. mindtest PaydayTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Payday
    {
        public const int FromHour = 12, ToHour = 20;   // Saturday, noon to eight

        /// Whether it's payday afternoon (weekday 6 Saturday).
        public static bool Now(int weekday, int hour)
        {
            return weekday == 6 && hour >= FromHour && hour < ToHour;
        }

        /// How payday leans a choice for a working person (Choosing's act ids). Nothing for those who don't draw a wage.
        public static float Lean(int weekday, int hour, bool works, string act)
        {
            if (!works || !Now(weekday, hour))
                return 0f;
            switch (act)
            {
                case "saloon": return 0.12f;
                case "drink": return 0.10f;
                case "cards": return 0.08f;
                case "store": return 0.08f;
                case "work": return -0.25f;   // the week's work stopped at noon
                default: return 0f;
            }
        }

        // the wages: A the one who asks (a woman), B the man with the pay; {a} {b} their names as the scene has them
        private static readonly string[][] Asks =
        {
            new[] { "{b}. The pay, before it's gone through that door.", "Rent's Monday. Coal's Tuesday. The children eat every day." },
            new[] { "Don't you walk past me, {b}. The envelope.", "I've waited by this post since half past twelve." },
            new[] { "{b}, the grocer's bill is three weeks old.", "I'll not ask twice in front of the whole street." },
            new[] { "Give it here, {b}. You can have a dollar of it back.", "One dollar. Not the whole of it, like last week." },
            new[] { "{b}! The shoes for the little one. You promised.", "I told the cobbler Saturday. It's Saturday." },
            new[] { "Hand it over, {b}, and you'll have a hot supper.", "Keep it, and you'll have a cold one, and a colder bed." },
        };

        private static readonly string[] AllOf = { "...Here. All of it.", "Take it. I'd only lose it.", "There. Every cent, see for yourself." };
        private static readonly string[] HalfOf = { "Half. I've earned a drink.", "Half of it. That's fair, and you know it.", "Take the most of it. The rest is mine." };
        private static readonly string[] Later = { "Later. I'll bring it home tonight. I swear it.", "In front of the whole street? Later.", "After one drink. One." };
        private static readonly string[] Refuse = { "It's my money. I sweated for it.", "Go home. I'll be along.", "Not now, I said." };
        private static readonly string[] Thanks = { "That's the rent, then.", "Bless you. Come home early.", "There. That's the week saved." };
        private static readonly string[] Bitter = { "I'll be waiting up. You'll hear about it.", "Every Saturday the same.", "Go on, then. I know where to find you." };
        private static readonly string[] BitterHalf = { "Half. Again.", "Half won't feed us to Friday.", "Every Saturday the same." };

        /// The wages scene: kind "wages" (no tie: it's between two who already know where they stand), A the woman.
        public static Scene Wages(SceneCast c, string seed)
        {
            int asker = c.AWoman ? 0 : 1, man = 1 - asker;
            string a = Scenes.N(c, asker, seed), b = Scenes.N(c, man, seed);
            string[] ask = Asks[(Voice.Hash(seed + "|ask") & 0x7fffffff) % Asks.Length];
            int how = (Voice.Hash(seed + "|how") & 0x7fffffff) % 100;   // all 35, half 30, later 20, no 15
            Scene s = new Scene { Kind = "wages", Summary = "a woman met a man at the saloon door for his pay" };
            s.Add(asker, null, null, "approach", 200)
             .Add(asker, Fill(ask[0], a, b), "mild", "face")
             .Add(asker, Fill(ask[1], a, b), "mild");   // (not angry before he's answered: the Part 3 review)
            if (how < 35)
                s.Add(man, Pick(AllOf, seed + "|all"), "mild").Add(asker, Pick(Thanks, seed + "|th"), "thanks", "walkaway");
            else if (how < 65)
                s.Add(man, Pick(HalfOf, seed + "|half"), "mild").Add(asker, Pick(BitterHalf, seed + "|bi"), "angry", "walkaway");
            else if (how < 85)
                s.Add(man, Pick(Later, seed + "|later"), "mild", "walkaway").Add(asker, Pick(Bitter, seed + "|bl"), "angry");
            else
                s.Add(man, Pick(Refuse, seed + "|no"), "angry", "walkaway").Add(asker, Pick(Bitter, seed + "|bn"), "angry");
            return s;
        }

        private static string Fill(string l, string a, string b)
        {
            return l.Replace("{a}", a).Replace("{b}", b);
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in Asks)
                foreach (string l in p)
                    yield return Fill(l, "Mary", "Amos");
            foreach (string[] pool in new[] { AllOf, HalfOf, Later, Refuse, Thanks, Bitter })
                foreach (string l in pool)
                    yield return l;
        }
    }
}
