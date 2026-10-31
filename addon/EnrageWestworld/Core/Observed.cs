// Being watched (NIGHT3; the psychologist's list): people behave better when the law or the church is looking. The
// street's own trouble waits while a lawman is in sight of the one who'd start it: a thief never works under the law's
// nose; a shove, a brawl, a callout or a standoff waits unless temper or drink forgets the law (four times in ten then).
// On a Sunday morning by the church the same, but for the godless. The would-be doer thinks why, so it's seen; once the
// watcher has gone, the reason is still theirs. The rules and the words here; the looking is the game side's
// (WestworldScript.Observed.cs). Plain C#, no game; all words original.
namespace EnrageWestworld.Core
{
    public static class Observed
    {
        public const float LawMetres = 25f;      // a lawman this near, with a clear sight of them
        public const float ChurchMetres = 60f;   // the Sabbath scenes' "by the church"
        public const int RecklessChance = 40;    // a hothead or a drunk who goes ahead anyway

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
        /// temper, drink and devotion. A thief always waits (a crowd by the church is a crowd that would see); a hothead or
        /// a drunk goes ahead four times in ten; the godless don't mind the church.
        public static bool Waits(string kind, bool law, bool church, string temper, bool drunk, string devotion, string seed)
        {
            if (!Holds(kind) || (!law && !church))
                return false;
            if (Thieving(kind))
                return true;
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
