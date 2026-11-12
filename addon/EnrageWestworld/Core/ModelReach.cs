// The model's reach while it's idle (LIVES item 3c, 2026-11-12; [Mind] ModelIdleReach, settings version 476). The model
// was asked ahead only when a feeling was just kept for someone near you (WestworldScript.Language.cs LanguageAskAhead);
// everyone else's thought was the written line, its model words asked at that moment and shown only the next time the same
// situation came round. Measured 2026-10-31: the model busy about 5% of the time, 18 people worded of 52 the mind read. Now,
// while the model's queue is empty and it has answered lately, every few seconds the nearest person the mind knows within
// reach who hasn't been asked in the last minute is asked ahead too, so more of the street's thoughts are the model's own
// when they come. Nothing is shown early, and the queue's own rules hold (a key already kept or waiting isn't asked again).
//
// The pacing (`Due`) is here, game-free and tested; the game side walks the near people (WestworldScript.ModelReach.cs).
namespace EnrageWestworld.Core
{
    public static class ModelReach
    {
        public const int EveryMs = 4000, AgainMs = 60000;
        public const float Metres = 20f;

        /// Whether an idle ask may go now: the model ready, nothing waiting, the last idle ask EveryMs ago.
        public static bool Due(bool on, bool ready, int waiting, int lastIdleAskMs, int nowMs)
        {
            return on && ready && waiting == 0 && nowMs - lastIdleAskMs >= EveryMs;
        }

        /// Whether a candidate comes before the best so far: asked longest ago (never asked first), then the nearer. So the
        /// whole street is reached in turn, not the same few nearest every minute (the test caught it).
        public static bool Before(int askedAt, float metres, int bestAskedAt, float bestMetres)
        {
            return askedAt < bestAskedAt || askedAt == bestAskedAt && metres < bestMetres;
        }

        /// Whether this person may be asked again (not in the last minute).
        public static bool Again(int lastAskedMs, int nowMs)
        {
            return lastAskedMs == int.MinValue || nowMs - lastAskedMs >= AgainMs;
        }
    }
}
