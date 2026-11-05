// The world-quiet gate (Run 3, item 5: the recognition plan's Stage 0a). No body's looks are read while the world is changing
// under the game: a story mission, the controls taken, the player dead or being arrested, the screen faded or fading, a load
// or the game paused; nor for LooksQuietMs after it settles, nor for LooksAfterLoadMs after a load, a death or a long jump.
// Those three are noticed in one place, SightsReset (WestworldScript.Sights.cs), which arms this gate through QuietFor; with
// the sights switched off this file watches for the long jump itself, so one watcher runs at a time and the two can never
// disagree (the plan's critic, point 10). enRAGE waits about five seconds the same way before it reads anyone. Only the
// Testing row "Read the looks of the people near you" reads looks while enRAGE runs (and [World] OwnLooks standing alone,
// held back): both wait on this. The game's clock (Game.GameTime), as the rest of the tick; a handful of cheap questions to
// the game five times a second.
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int LooksQuietMs = 5000, LooksAfterLoadMs = 10000, WorldQuietEveryMs = 200;
        private int _worldQuietUntil;          // Game.GameTime before which no look is read
        private string _worldQuietWhy;         // why the world is quiet now; null when it isn't
        private string _worldQuietLast;        // what last made it quiet, for the probe row's refusal
        private int _worldQuietSince, _nextWorldQuiet;
        private Vector3 _worldQuietWas;        // where you were at the last look (only while the sights are off)
        private bool _worldQuietHasWas;

        /// Five times a second: whether the world is quiet now; while it is, and LooksQuietMs after, no look is read. Only the
        /// changes are logged, and only with the detailed log on.
        private void WorldQuietStep(int now)
        {
            if (now - _nextWorldQuiet < 0)
                return;
            _nextWorldQuiet = now + WorldQuietEveryMs;
            string why;
            try
            {
                why = WorldQuietNow();
            }
            catch
            {
                why = "the game wouldn't say";   // nothing read while we can't tell
            }
            if (why != null)
            {
                if (_worldQuietWhy == null)
                {
                    _worldQuietSince = now;
                    Log.Detail("LOOKS quiet: " + why + " (no looks read until " + LooksQuietMs / 1000 + " s after)");
                }
                _worldQuietWhy = why;
                _worldQuietLast = why;
                QuietUntil(now + LooksQuietMs);
            }
            else if (_worldQuietWhy != null)
            {
                Log.Detail("LOOKS quiet over (" + (now - _worldQuietSince) / 1000 + " s of " + _worldQuietWhy + ")");
                _worldQuietWhy = null;
            }
            if (_cfg == null || _cfg.SightReactions)
            {
                _worldQuietHasWas = false;   // the sights watch for the long jump (SightSample) and arm the gate through SightsReset
                return;
            }
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            Vector3 at = me.Position;
            if (_worldQuietHasWas && _worldQuietWas.DistanceTo(at) > 150f)
                QuietFor(LooksAfterLoadMs, "a long way from where you were (fast travel, a respawn, a load)");
            _worldQuietWas = at;
            _worldQuietHasWas = true;
        }

        /// What makes the world quiet this moment, or null: the conditions Spawner.SafeToSpawn waits out, and the controls, a
        /// death, an arrest and a pause besides.
        private static string WorldQuietNow()
        {
            if (Game.IsLoading)
                return "a load";
            if (Game.IsPaused)
                return "the game paused";
            if (MISC.GET_MISSION_FLAG())
                return "a story mission";
            if (CAM.IS_SCREEN_FADED_OUT() || CAM.IS_SCREEN_FADING_OUT())
                return "the screen faded";
            int you = Game.Player.Handle;
            if (PLAYER.IS_PLAYER_DEAD(you))
                return "you're dead";
            if (PLAYER.IS_PLAYER_BEING_ARRESTED(you, true))
                return "the law is taking you in";
            if (!PLAYER.IS_PLAYER_CONTROL_ON(you))
                return "your controls are taken";
            return null;
        }

        /// No look read for `ms` from now: a load, a death or a long jump (SightsReset), or the long jump noticed here.
        private void QuietFor(int ms, string why)
        {
            _worldQuietLast = why;
            QuietUntil(Game.GameTime + ms);
            Log.Detail("LOOKS quiet: " + why + " (no looks read for " + ms / 1000 + " s after)");
        }

        private void QuietUntil(int until)
        {
            if (until - _worldQuietUntil > 0)
                _worldQuietUntil = until;
        }

        /// Whether a body's looks may be read now: the world isn't quiet and has been settled long enough.
        private bool LooksReadable(int now)
        {
            return _worldQuietWhy == null && now - _worldQuietUntil >= 0;
        }

        /// Why no look can be read now ("quiet: 7 s left after a load"), or null when one can.
        private string LooksRefusal(int now)
        {
            if (_worldQuietWhy != null)
                return "quiet: " + _worldQuietWhy;
            int left = _worldQuietUntil - now;
            if (left > 0)
                return "quiet: " + (left + 999) / 1000 + " s left after " + (_worldQuietLast ?? "the world settled");
            return null;
        }
    }
}
