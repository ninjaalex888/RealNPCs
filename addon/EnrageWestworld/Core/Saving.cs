// When RealNPCs saves its memory of people (0.6.3). A save is a slow frame: Lex's 1.6 MB arthur.json took about 22 ms a
// save offline and 15-24 ms in game, once a minute (2026-10-27); 2.6 MB and 22-23 ms by 2026-11-04. So a save waits for a
// moment when nobody can see a slow frame (the game paused, the screen faded out, a load, the player down), and only when
// none comes for InPlayMs does it save in play: every ten minutes since 2026-11-04 (Lex: "Yes we can save less often, we
// could even do every 10 minutes"; players felt the once-a-minute hitch). A crash in play can lose up to ten minutes of
// what people remember; a fade, a load or a fast travel still saves within 20 s of the last. The game side
// (Game/WestworldScript.Saving.cs) then writes the file on another thread.
namespace EnrageWestworld.Core
{
    public static class Saving
    {
        public const int QuietGapMs = 20000;   // saves while nobody can see them, at most this often
        public const int InPlayMs = 600000;    // in play, a save waits this long for a quiet moment, then happens anyway (180 s at first; 60 s once the pause menu turned out to stop our ticks, 2026-10-27; ten minutes since 2026-11-04, Lex's answer on the hitch)

        /// Why a save is due now ("quiet" or "in play"), or null. quiet: nobody can see a slow frame now; sinceMs: since the
        /// last save or load; played: ticks in play since then (none: nothing can have changed); writing: the last save is
        /// still being written out.
        public static string WhyNow(bool quiet, int sinceMs, int played, bool writing)
        {
            if (writing || played <= 0 || sinceMs < 0)
                return null;
            if (quiet)
                return sinceMs >= QuietGapMs ? "quiet" : null;
            return sinceMs >= InPlayMs ? "in play" : null;
        }
    }
}
