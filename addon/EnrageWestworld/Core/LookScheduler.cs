// The frame pass (DEPTH item 4, 2026-11-10; [Mind] LookScheduler, on). The main tick's background looks (the fever's thoughts,
// the posters, the calendar, the town's rivals and two dozen more) each kept a timer of its own and ran on the frame it came due,
// so on an unlucky frame several ran at once: every one of them comes due on the first frame after a load, and those whose
// intervals share a multiple (5, 10, 20, 30 and 60 s) meet again every minute. The 11-09 log had a peak over 4 ms about once a
// minute. Here one scheduler owns them: each look registers its interval once, and a frame runs at most MostAFrame (2) of those
// due, the most overdue first. A look is late by a few frames at most and still runs at its own interval; nothing allocates
// after registration. The looks keep their own timers too (a look called early returns at once), so the scheduler can only
// space them out, never run one more often than it ran before.
//
// Off ([Mind] LookScheduler false), the tick calls each look every frame as before and this class is never stepped.
using System;

namespace EnrageWestworld.Core
{
    public sealed class LookScheduler
    {
        public const int MostAFrame = 2;

        sealed class Look
        {
            public string Name;
            public int Every, Due, Last = int.MinValue, Runs;
            public Action<int> Run;
        }

        Look[] _looks = new Look[48];
        int _count;
        bool _started;

        // what F7 and the minute line say
        public int Registered { get { return _count; } }
        public int WorstFrame { get; private set; }      // the most looks one frame ran (never above MostAFrame)
        public int LatestMs { get; private set; }        // the furthest past its due time a look has run
        public long Runs { get; private set; }
        public int RanLastFrame { get; private set; }

        /// Registers a look: its name (for F7), its interval in ms and what runs it. Called once each, before the first Step.
        public void Add(string name, int everyMs, Action<int> run)
        {
            if (run == null || everyMs <= 0)
                return;
            if (_count == _looks.Length)
                Array.Resize(ref _looks, _looks.Length * 2);
            _looks[_count++] = new Look { Name = name, Every = everyMs, Run = run };
        }

        /// One frame: at most MostAFrame looks that are due, the most overdue first. Returns how many ran.
        /// The first frame spreads their first turns over each one's own interval, so they never all start together.
        public int Step(int now)
        {
            if (!_started)
            {
                _started = true;
                for (int i = 0; i < _count; i++)
                    _looks[i].Due = now + (int)((uint)(i * 2654435761u) % (uint)Math.Max(1, Math.Min(_looks[i].Every, 5000)));   // the first turn within five seconds, spread
            }
            int ran = 0;
            for (int k = 0; k < MostAFrame; k++)
            {
                int best = -1, bestLate = int.MinValue;
                for (int i = 0; i < _count; i++)
                {
                    Look l = _looks[i];
                    int late = now - l.Due;
                    if (late >= 0 && l.Last != now && late > bestLate)
                    {
                        best = i;
                        bestLate = late;
                    }
                }
                if (best < 0)
                    break;
                Look b = _looks[best];
                b.Last = now;
                b.Due = now + b.Every;   // from when it ran, as the looks' own timers count
                b.Runs++;
                Runs++;
                if (bestLate > LatestMs)
                    LatestMs = bestLate;
                ran++;
                b.Run(now);
            }
            RanLastFrame = ran;
            if (ran > WorstFrame)
                WorstFrame = ran;
            return ran;
        }

        /// After a load or a long pause: every look's next turn spread again from now (none all at once on the frame after).
        public void Restart()
        {
            _started = false;
        }

        public string Status()
        {
            return _count + " looks under the scheduler, at most " + MostAFrame + " a frame; the most one frame ran " + WorstFrame + ", the latest one ran " + LatestMs + " ms past its turn; " + Runs + " runs";
        }

        // ---- offline: how many looks the old way would have run on one frame -------------------------------------------

        /// The old way's worst frame over `ms` at `frameMs` a frame: every look called each frame, each on its own timer
        /// (all due on the first frame, as after a load), and how many ran their body on the worst one.
        public static int OldWorst(int[] everyMs, int ms, int frameMs)
        {
            var next = new int[everyMs.Length];
            int worst = 0;
            for (int t = 0; t <= ms; t += frameMs)
            {
                int n = 0;
                for (int i = 0; i < everyMs.Length; i++)
                    if (t >= next[i])
                    {
                        next[i] = t + everyMs[i];
                        n++;
                    }
                worst = Math.Max(worst, n);
            }
            return worst;
        }

        /// The old way's worst frame after the first ten seconds (the meeting of shared multiples, not the load).
        public static int OldWorstSettled(int[] everyMs, int ms, int frameMs)
        {
            var next = new int[everyMs.Length];
            int worst = 0;
            for (int t = 0; t <= ms; t += frameMs)
            {
                int n = 0;
                for (int i = 0; i < everyMs.Length; i++)
                    if (t >= next[i])
                    {
                        next[i] = t + everyMs[i];
                        n++;
                    }
                if (t > 10000)
                    worst = Math.Max(worst, n);
            }
            return worst;
        }
    }
}
