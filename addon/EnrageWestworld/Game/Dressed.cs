// Bodies dressed lately (Run 3, item 5: the recognition plan's Stage 0b). Every place RealNPCs puts an outfit on a body
// and has the game redraw it (for the regulars, the posts, the stalkers, the test cast, the executions, the allies, the
// campfire, the hogtied, the letters, the town events, the trains, the trials, the returners and the waking's stripping)
// notes it here, and no read of a body's looks touches it for LooksAfterDressMs: the game is still putting the new
// clothes together, and a read mid-way is the kind of call that hung the game thread in Saint Denis (2026-10-31). The game
// thread only; real time, so a pause doesn't stretch it.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.InGame
{
    internal static class Dressed
    {
        public const int LooksAfterDressMs = 10000;
        private static readonly Dictionary<int, int> At = new Dictionary<int, int>();

        /// This body was dressed just now.
        public static void Note(int handle)
        {
            if (handle == 0)
                return;
            if (At.Count > 600)
                Prune();
            At[handle] = Environment.TickCount;
        }

        /// Milliseconds since this body was dressed (int.MaxValue: never, as far as we know).
        public static int SinceMs(int handle)
        {
            int at;
            return At.TryGetValue(handle, out at) ? Environment.TickCount - at : int.MaxValue;
        }

        /// Whether a read may touch it yet.
        public static bool Settled(int handle)
        {
            return SinceMs(handle) >= LooksAfterDressMs;
        }

        public static void Clear()
        {
            At.Clear();
        }

        private static void Prune()
        {
            int now = Environment.TickCount;
            foreach (int k in At.Where(kv => now - kv.Value > 60000).Select(kv => kv.Key).ToList())
                At.Remove(k);
            if (At.Count > 600)
                At.Clear();
        }
    }
}
