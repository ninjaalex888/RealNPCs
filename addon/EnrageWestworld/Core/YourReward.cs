// Post a reward (DEPTH reserve R4; [Scenes] PostReward, on). Someone you know in this town was robbed in the last few days (a lift, a
// pickpocket's hand, a mugging: the tie keeps the thief as CrimeDoer): at the jail door, J, put five dollars with the sheriff for whoever
// brings the thief in. Within the day (a session's clock), the town settles it: a regular brings him in and the money is his (the one
// robbed thinks better of you), or a man claims it falsely, naming someone who was elsewhere, and the sheriff sends him off and your
// money comes back; or nobody does, and it comes back after two game days. Told in the Around town box. Why it reads true: a reward
// is a question the town answers about itself, honestly or not. Plain C#; the game side is Game/WestworldScript.YourReward.cs.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class YourReward
    {
        public static bool On = true;   // [Scenes] PostReward, from the settings
        public const string Kind = "postreward";
        public const int Cents = 500, WithinDays = 3, LapseMinutes = 2 * 1440;
        public const float DoorMetres = 6f;

        /// How it ends: "brought" (a regular brings him in, 55%), "false" (a false claim, 25%), else "none" (it lapses). Seeded.
        public static string Outcome(string seed)
        {
            int r = (Voice.Hash(seed + "|out") & 0x7fffffff) % 100;
            return r < 55 ? "brought" : r < 80 ? "false" : "none";
        }

        /// When it's settled, in game minutes after it's posted (an hour to ten: inside the session's two days).
        public static int After(string seed) { return 60 + (Voice.Hash(seed + "|after") & 0x7fffffff) % 540; }

        public static List<InteractOption> Rows(string victim, int cash)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "postreward:yes", Label = "Put " + BrawlVerbs.Money(Cents) + " with the sheriff for whoever robbed " + victim + ".", Why = cash < Cents ? "you haven't five dollars" : null },
                new InteractOption { Id = "postreward:no", Label = "Not your affair." },
            };
        }

        public static string Posted(string victim, string town) { return "A stranger has put five dollars with the sheriff in " + town + " for whoever robbed " + victim + "."; }

        public static string Brought(string by, string thief, string victim, string seed)
        {
            return Voice.Pick(seed + "|br", by + " brought " + thief + " in for robbing " + victim + ", and took the stranger's five dollars for it.",
                                            "The stranger's reward is paid: " + by + " walked " + thief + " into the jail for what he took off " + victim + ".");
        }

        public static string FalseClaim(string by, string named, string seed)
        {
            return Voice.Pick(seed + "|fc", by + " tried to claim the stranger's five dollars, naming " + named + ", who was in another town that night. The sheriff sent him off.",
                                            "A false claim on the stranger's reward: " + by + " named " + named + ", and three men swore " + named + " was elsewhere.");
        }

        public static string Lapsed(string victim) { return "Nobody claimed the stranger's reward for whoever robbed " + victim + ". The sheriff has handed it back."; }

        public static string VictimThinks(string seed)
        {
            return Voice.Pick(seed + "|vt", "A stranger put up money for what was took from me. Somebody cares, then.", "Five dollars of a stranger's, on my account. I'll not forget it.");
        }

        public static IEnumerable<string> AllLines()
        {
            yield return Posted("Abe", "Valentine");
            for (int i = 0; i < 4; i++) { yield return Brought("Ed", "Sam", "Abe", "b" + i); yield return FalseClaim("Ed", "Sam", "f" + i); yield return VictimThinks("v" + i); }
            yield return Lapsed("Abe");
        }
    }
}
