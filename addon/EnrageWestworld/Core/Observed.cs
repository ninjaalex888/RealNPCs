// Being watched (NIGHT3; the psychologist's list): people behave better when the law or the church is looking. The
// street's own trouble waits while a lawman is in sight of the one who'd start it: a thief mostly won't work under the
// law's nose, but a bold one tries now and then (BoldThiefChance, more for a hothead, a greedy one or a drunk); a shove, a brawl, a callout or a standoff waits unless temper or drink forgets the law (four times in ten then).
// On a Sunday morning by the church the same, but for the godless. The would-be doer thinks why, so it's seen; once the
// watcher has gone, the reason is still theirs. The rules and the words here; the looking is the game side's
// (WestworldScript.Observed.cs). Plain C#, no game; all words original.
namespace EnrageWestworld.Core
{
    public static class Observed
    {
        public static float LawMetres = 25f;     // ([Scenes] WatchedLawMetres) a lawman this near, with a clear sight of them
        public static float ChurchMetres = 60f;  // ([Scenes] WatchedChurchMetres) the Sabbath scenes' "by the church"
        public static int RecklessChance = 40;   // ([Scenes] WatchedRecklessChance) a hothead or a drunk who goes ahead anyway

        /// A thief who tries it with the law looking on, in percent ([Scenes] BoldThieves; Lex, 2026-10-31: "less realistic,
        /// more exciting"); a hothead, a greedy one or a drunk half as likely again. Set from the settings.
        public static int BoldThiefChance = 25;

        /// The percent for this thief: BoldThiefChance, half as much again for the hothead, the greedy and the drunk.
        public static int BoldChance(string temper, bool drunk)
        {
            int c = System.Math.Max(0, System.Math.Min(100, BoldThiefChance));
            return temper == "Hothead" || temper == "Greedy" || drunk ? System.Math.Min(100, c * 3 / 2) : c;
        }

        /// Whether a thief with the law looking on goes ahead anyway (one roll for the seed: the game side seeds it by the day).
        public static bool BoldGoes(string temper, bool drunk, string seed)
        {
            return Voice.Chance(seed + "|bold", BoldChance(temper, drunk));
        }

        /// The kinds that wait for the watcher to go: the thefts, a shove, the fights and the guns.
        public static bool Holds(string kind)
        {
            return kind == "pickpocket" || kind == "lift" || kind == "rough" || kind == "brawl" || kind == "standoff" || kind == "callout";
        }

        public static bool Thieving(string kind)
        {
            return kind == "pickpocket" || kind == "lift";
        }

        /// Whether `kind` waits: `law` when the law has them in view, `church` a Sunday morning by the church; the doer's
        /// temper, drink and devotion. A thief waits but for a bold one under the law's eye (BoldGoes), and always by the church
        /// (a crowd by the church is a crowd that would see); a hothead or
        /// a drunk goes ahead four times in ten; the godless don't mind the church.
        public static bool Waits(string kind, bool law, bool church, string temper, bool drunk, string devotion, string seed)
        {
            if (!Holds(kind) || (!law && !church))
                return false;
            if (Thieving(kind))
                return church || !(law && BoldGoes(temper, drunk, seed));   // by the church a thief always waits, the law looking on or not
            bool reckless = temper == "Hothead" || drunk;
            if (law)
                return !(reckless && Voice.Chance(seed + "|law", RecklessChance));
            return devotion != "Godless" && !(reckless && Voice.Chance(seed + "|church", RecklessChance));
        }

        /// What they think as they hold off.
        public static string Thought(string kind, bool law, string seed)
        {
            if (law)
                return Thieving(kind) ? Voice.Pick(seed + "|wt", "Lawman's eye on the street. Later, then.", "Under a lawman's nose? I'm not that green.", "Lawman right there. Hands in your own pockets.")
                                      : Voice.Pick(seed + "|wl", "It'll keep. The law's standing right there.", "Law's watching. Another day, then.", "I'd settle it now, if the law weren't looking on.");
            return Thieving(kind) ? Voice.Pick(seed + "|wc", "Church folk on every step in their Sunday best. Too many eyes.", "Church folk everywhere. Leave it be.")
                                  : Voice.Pick(seed + "|ws", "On the Lord's day? It'll wait.", "Sunday. It can wait till tomorrow.", "The whole town's in its Sunday best. Some other time.");
        }
    }
}
