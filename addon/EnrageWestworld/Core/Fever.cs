// Fever in town (FIVEDAY reserve R9). Rarely (a town in a hundred and fifty a day, never within ninety days of the last), a
// fever breaks out in one of the towns: one of its people falls sick, and each day it passes along the warm ties (friends,
// sweethearts, the neighbours who talk: Ties.Warmth > 0) to someone in the same town, one tie in twenty-five a day. The sick
// keep to home for about a week (a strong lean on their choices, the home proper at any hour), think of it, and get better,
// or one in twelve dies of it (mourned like anyone: the wake, the grave). With three down at once the board of health posts
// a quarantine notice (the box), and the town's talk turns to it. It's over when nobody is sick. Who's sick and who's had it
// live in Store.Counts ("sick:<id>" the day they fell ill, "had:<id>" the day they got up; "fever:<town>" the outbreak's
// day): nothing new in the save. Behind [World] Fever (and FeverDeaths for the dying).
//
// Why it reads true: 1899's towns had no germ theory in the street, only the board of health's yellow flag; diphtheria,
// typhoid and scarlet fever went through a town along the very ties that held it together: the visits, the church, the
// shared pew. A disease that spreads by kindness is the cruellest kind, and every friend's call becomes a gamble. Plain C#,
// no game; all words original. mindtest FeverTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Fever
    {
        public const int StartChancePerMil = 7;        // a town's day, in a thousand (about one in a hundred and fifty)
        public const int GapDays = 90;
        public const int SpreadPercent = 4;           // a warm tie a day (a fifth to a third of a town of sixty, as 1899's outbreaks ran)
        public const int SickDaysLeast = 6, SickDaysMost = 9;
        public const int DiesPercent = 8;             // of the sick, when the fever turns
        public const int QuarantineAt = 3;
        public const string SickPrefix = "sick:", HadPrefix = "had:", TownPrefix = "fever:", PostedPrefix = "quarantine:";
        public static readonly string[] Towns = { "Valentine", "Strawberry", "Rhodes", "Blackwater", "Annesburg", "Armadillo", "Saint Denis", "Van Horn" };

        public static bool Starts(string town, long day, long lastDay)
        {
            return day - lastDay >= GapDays && (Voice.Hash("fever|" + town + "|" + day) & 0x7fffffff) % 1000 < StartChancePerMil;
        }

        public static bool Catches(string from, string to, long day) { return Voice.Chance("catch|" + from + "|" + to + "|" + day, SpreadPercent); }

        public static int SickDays(string id) { return SickDaysLeast + (int)((Voice.Hash(id + "|sickdays") & 0x7fffffff) % (SickDaysMost - SickDaysLeast + 1)); }

        public static bool Dies(string id, long sickDay) { return Voice.Chance(id + "|dies|" + sickDay, DiesPercent); }

        /// The fever's name for an outbreak (the period's three).
        public static string Name(string town, long day) { return Voice.Pick("fevername|" + town + "|" + day, "the diphtheria", "a typhoid fever", "the scarlet fever"); }

        /// How being sick leans a choice (Choosing's act ids).
        public static float Lean(bool sick, string act)
        {
            if (!sick)
                return 0f;
            return act == "home" ? 0.6f : act == "work" ? -0.5f : act == "saloon" ? -0.5f : act == "drink" ? -0.4f : act == "cards" ? -0.4f : act == "church" ? -0.3f : act == "store" ? -0.2f : act == "greet" ? -0.3f : 0f;
        }

        private static readonly string[] SickThoughts = {
            "Hot and cold at once. The room won't keep still.", "Throat like a fist. Can't swallow the broth she brings.",
            "Doctor came and went and said nothing. That's the worst kind of saying.", "If I'd not gone to the Hollises' Sunday. If.",
            "Three days in this bed. The ceiling has a crack shaped like a river." };
        private static readonly string[] TownThoughts = {
            "Yellow flag on the Dray place now. That's two on our street.", "Keep the children in. Keep them in and away from the well.",
            "They say it came off the train. They say a lot of things.", "Went to sit with her and now I'm frightened of my own cough." };
        private static readonly string[] Better = { "Up and walking. Weak as a kitten, but walking.", "Ate an egg today. Kept it down. Lord be thanked." };

        public static string SickThought(string seed) { return Voice.Pick(seed + "|fs", SickThoughts); }
        public static string TownThought(string seed) { return Voice.Pick(seed + "|ft", TownThoughts); }
        public static string BetterThought(string seed) { return Voice.Pick(seed + "|fb", Better); }

        public static string Quarantine(string town, string fever, int sick)
        {
            return "QUARANTINE. The board of health in " + (town ?? "town") + ": " + fever + ", " + sick + " houses. Keep away from the sick, boil your water.";
        }

        public static string Over(string town, string fever, int dead)
        {
            return dead > 0 ? (town ?? "Town") + " is over " + fever + ". It took " + dead + (dead == 1 ? " life." : " lives.") : (town ?? "Town") + " is over " + fever + ", and nobody died of it.";
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { SickThoughts, TownThoughts, Better }) foreach (string l in p) yield return l;
            yield return Quarantine("Saint Denis", "a typhoid fever", 4);
            yield return Over("Strawberry", "the scarlet fever", 2);
            yield return Over("Strawberry", "the diphtheria", 0);
        }
    }
}
