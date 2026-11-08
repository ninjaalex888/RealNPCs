// Grinding season in the cane (FIVEDAY reserve R23). From the middle of October till the twentieth of December the sugar
// mills of Lemoyne run day and night, and the cane must be cut and ground before the frost: in Rhodes, Saint Denis and Lagras
// the working men (InnerLife's working and farm trade groups) work into the night (the work proper till midnight, a strong
// lean to it; less saloon, less home), think the season's thoughts (the knives, the rollers, the smoke, the sleep they're not
// getting), and the season ends with the grinding ball at Rhodes (a festival kind: Core/Festivals.cs, a couple who come away
// courting). Behind [World] GrindingSeason.
//
// Why it reads true: the grinding was the hardest and the most dangerous labour of the Southern year, the whole parish at
// the mill's pace for two months; exhaustion narrows a mind to the next shift (the work crowding everything out), and the
// ball at the end is the release that made it bearable. Plain C#, no game; all words original. mindtest GrindingTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Grinding
    {
        public static readonly string[] Towns = { "Rhodes", "Saint Denis", "Lagras" };

        /// The season: 15 October to 20 December.
        public static bool Now(int month, int day) { return month == 10 && day >= 15 || month == 11 || month == 12 && day <= 20; }

        public static bool In(string town) { return town != null && Towns.Contains(town); }

        /// Whether this person works the grinding: a working or farm trade in the cane towns, in the season.
        public static bool Works(string town, string tradeGroup, int month, int day) { return In(town) && (tradeGroup == "working" || tradeGroup == "farm") && Now(month, day); }

        /// How the grinding leans a choice (Choosing's act ids).
        public static float Lean(bool grinding, int hour, string act)
        {
            if (!grinding)
                return 0f;
            return act == "work" ? (hour >= 18 ? 0.45f : 0.25f) : act == "saloon" ? -0.2f : act == "home" ? (hour >= 18 && hour < 24 ? -0.25f : 0f) : act == "stroll" ? -0.15f : 0f;
        }

        private static readonly string[] Thoughts = {
            "Cut, strip, top, throw. Cut, strip, top, throw. I do it in my sleep now. What sleep there is.",
            "The mill whistle at four. The mill whistle at ten. The mill never sleeps, so nobody does.",
            "Boudreaux lost his arm to the rollers Tuesday. They had the mill running again in an hour.",
            "Smoke from the sugar house over everything. My shirt tastes of molasses.",
            "Six more weeks before the frost. Six more weeks of this, then the ball.",
            "The cane knife's dull and so am I." };

        public static string Thought(string seed) { return Voice.Pick(seed + "|gt", Thoughts); }

        public static IEnumerable<string> AllLines() { return Thoughts; }
    }
}
