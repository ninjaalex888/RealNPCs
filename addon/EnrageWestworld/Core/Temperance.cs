// The temperance ladies (FIVEDAY item 12; roadmap.md's idea bank: "women pray and sing outside the saloon. Drinkers jeer
// or slink off; one husband is marched home; the barkeep sours"). A street scene between two of the town's own people out
// of doors by day: a temperance woman meets a drinker before the saloon doors and asks him to sign. He jeers and
// goes in anyway, slinks off ashamed, swears off for the day, or is taken home on her arm, by the seed, the pious more
// likely to start it (Scenes.Minds: a pious mind leans to it, and so does a man whose worry is the drink, the one she'd
// pick). Whoever saw it thinks of it (InnerLife.Now's BySeen "temperance"). Behind [Scenes] Temperance. No hymn's words:
// her lines are her own, the period's cause in our words.
//
// Why it reads true: the Woman's Christian Temperance Union was the largest women's organisation in the country by 1899,
// and its members met drinkers at the saloon door with prayer, pledge cards and song; reactance (the jeer, the drink taken
// out of spite) and shame (the slink) were both what they got, and now and then a man who signed. Plain C#, no game; all
// words original. mindtest TemperanceTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Temperance
    {
        private static readonly string[] Plea =
        {
            "{b}. Before you go through those doors, think of the ones at home.",
            "Sign the pledge, {b}. One line, and your Saturdays are your own again.",
            "There's no friend for you in that saloon, {b}. Only a till.",
            "We'll pray for you, {b}, whether you want it or not.",
            "Every glass in there is bought with bread money, {b}.",
            "Turn round, {b}. It's not too late in the day to turn round.",
            "The Lord made you for better than a brass rail, {b}.",
            "Walk home with me, {b}. Just today. See how it feels.",
        };

        private static readonly string[] Jeer = { "Pray louder, ma'am. They can't hear you in Kansas.", "I'll drink one for your soul, then.", "Save it for Sunday, sister.", "Bread money's mine to spend." };
        private static readonly string[] Slink = { "...I was only passing.", "Another day, ma'am. Another day.", "Spare me the look, ma'am." };
        private static readonly string[] Swear = { "You're right. Not today. Not today.", "Give me the card. Quick, before I think.", "One day dry. I'll try one day." };
        private static readonly string[] Home = { "...All right. Home, then.", "You'll not let go of my arm, will you?", "Lead on. I'm too tired to argue." };
        private static readonly string[] HerAfter = { "God keep you.", "One saved is one more than yesterday.", "Bless you. Saturday's the hardest. Remember that." };   // after he swears off
        private static readonly string[] HerRebuffed = { "We'll be here again Saturday.", "I'll pray for him all the same.", "The Lord is patient. So am I." };   // after a jeer or a slink

        /// The scene: kind "temperance", A the woman (whichever side she's on), B the man. Outcome by the seed: jeer 35,
        /// slink 30, swear off 20, home on her arm 15.
        public static Scene Make(SceneCast c, string seed)
        {
            if (c == null || c.AWoman == c.BWoman)
                return null;
            int her = c.AWoman ? 0 : 1, him = 1 - her;
            string b = Scenes.N(c, him, seed);
            int how = (Voice.Hash(seed + "|how") & 0x7fffffff) % 100;
            Scene s = new Scene { Kind = "temperance", Summary = "a temperance woman put the pledge to a man outside the saloon" };
            s.Add(her, null, null, "approach", 200)
             .Add(her, Pick(Plea, seed + "|plea").Replace("{b}", b), "mild", "face");
            if (how < 35)
                s.Add(him, Pick(Jeer, seed + "|j"), "mock", "walkaway").Add(her, Pick(HerRebuffed, seed + "|a"), "mild");
            else if (how < 65)
                s.Add(him, Pick(Slink, seed + "|s"), "mild", "walkaway").Add(her, Pick(HerRebuffed, seed + "|a"), "mild");
            else if (how < 85)
                s.Add(him, Pick(Swear, seed + "|w"), "thanks").Add(her, Pick(HerAfter, seed + "|a"), "thanks", "walkaway").Add(him, null, null, "walkaway");
            else
                s.Add(him, Pick(Home, seed + "|h"), "mild").Add(her, null, null, "walkaway").Add(him, null, null, "walkaway");
            return s;
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Plea)
                yield return l.Replace("{b}", "Amos");
            foreach (string[] pool in new[] { Jeer, Slink, Swear, Home, HerAfter, HerRebuffed })
                foreach (string l in pool)
                    yield return l;
        }
    }
}
