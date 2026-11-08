// Going dry (FIVEDAY reserve R27, 1907). By John's time the temperance women (Core/Temperance.cs, item 12) have won the
// local-option vote in Rhodes and Strawberry: the towns are dry by law. The game's saloons can't be shut, so they pour
// under the counter as "blind tigers" (pay to see the tiger, the drink is free): the law-abiding and the devout keep out
// (a lean away from the saloon and a drink, by deference to the law and piety), the rest still go and talk about it; the
// thoughts of a dry town (the vote, the women who won it, the jug behind the feed store); in the box the first time you
// come into a dry town each day, the notice on the saloon door. Behind [World] GoingDry.
//
// Why it reads true: a law against a habit doesn't end the habit, it sorts the people (the conscientious obey, the rest
// learn a password), and a dry town's talk is never about anything else. Plain C#, no game; all words original. mindtest
// GoingDryTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class GoingDry
    {
        public const int Year = 1907;
        public static readonly string[] Towns = { "Rhodes", "Strawberry" };

        public static bool Dry(string town, int year) { return year >= Year && town != null && Towns.Contains(town); }

        /// How a dry town leans a choice (Choosing's act ids): the law-abiding and the pious keep from the blind tiger.
        public static float Lean(bool dry, float deference, float piety, string act)
        {
            if (!dry || act != "saloon" && act != "drink")
                return 0f;
            float law = 0.15f + 0.12f * deference + 0.1f * piety;
            return -System.Math.Max(0.05f, law);
        }

        private static readonly string[] Thoughts = {
            "Dry by law since the vote. The saloon still pours, they just call it lemonade.",
            "Knock twice at the feed store's rear door and a jug comes out. Everybody knows. Nobody says.",
            "The temperance women won it fair, I'll give them that. Every widow in the county voted with her feet.",
            "Fined five dollars for a bottle in my own coat. Five dollars. The judge drinks, I'd bet my farm.",
            "Since the town went dry the men are home by eight. My wife says it's a miracle. I say it's dull." };
        private static readonly string[] Doors = {
            "On the saloon door at {t}: CLOSED BY ORDER under local option. The piano is playing inside.",
            "{t} is dry by law. The saloon calls itself a reading room now. Nobody in it is reading." };

        public static string Thought(string seed) { return Voice.Pick(seed + "|dt", Thoughts); }
        public static string Door(string town, string seed) { return Voice.Pick(seed + "|dd", Doors).Replace("{t}", town ?? "town"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Thoughts) yield return l;
            foreach (string l in Doors) yield return l.Replace("{t}", "Strawberry");
        }
    }
}
