// The control ring's rules (FIVEDAY2 item 1; Lex's plan of 2026-10-31, "a control ring with leases", grown out of the held ring).
// The people near you are kept at what their minds choose rather than left to fall back on the game between one act and the next.
// Why it reads true: behaviour only looks like intention if it carries on while you watch; someone who goes slack the moment
// their errand ends reads as a puppet, and the eye goes straight to it.
//
// This file is what doesn't need the game: how far the ring reaches by how you're moving, and the guard that draws it in when it
// costs too much. You on foot get the whole ring. At a walk in the saddle you still see faces, so it narrows; at a trot only those
// beside the road matter; at a gallop or on a train nobody can be watched for long enough to be worth holding, and the ring is
// empty. A gait has to hold for a moment before the ring follows it, so a horse changing step doesn't flicker people in and out.
// The guard answers Lex's "we can test frame cost afterwards": every step's cost is measured whatever else happens, and a ring
// running over the budget (1 ms average, 4 ms peak) shrinks itself until it fits, then eases back. Plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public enum RingGait { Foot, SaddleWalk, Trot, Fast }

    /// How far the ring reaches and how many it holds just now. In 0: nobody.
    public struct RingReach
    {
        public float In, Out;
        public int Most;
    }

    public static class Ring
    {
        public const float DefaultMetres = 22f, MostMetres = 25f, LeastMetres = 6f;   // 25: the mind's near ring, past which it knows nobody by name
        public const int DefaultMost = 12, MostHeld = 20, LeastHeld = 2;
        public const float WalkBelow = 2.6f, TrotBelow = 6.0f;   // metres a second under the saddle: a horse walks at about 2, trots at about 5, canters from about 8

        /// How you're moving. Any vehicle counts as fast: a seat on a wagon or a train takes you past people, and you can't stop to look.
        public static RingGait GaitOf(bool mounted, bool inVehicle, float speed)
        {
            if (inVehicle)
                return RingGait.Fast;
            if (!mounted)
                return RingGait.Foot;
            return speed < WalkBelow ? RingGait.SaddleWalk : speed < TrotBelow ? RingGait.Trot : RingGait.Fast;
        }

        /// The ring for this gait from the player's settings, drawn in by the guard's scale (1 at rest, down to RingGuard.LeastScale).
        public static RingReach ReachFor(RingGait gait, float metres, int most, float scale)
        {
            metres = Math.Max(LeastMetres, Math.Min(MostMetres, metres));
            most = Math.Max(LeastHeld, Math.Min(MostHeld, most));
            float share = gait == RingGait.Foot ? 1f : gait == RingGait.SaddleWalk ? 0.6f : gait == RingGait.Trot ? 0.3f : 0f;
            if (share <= 0f)
                return new RingReach();
            float inM = metres * share * scale;
            int n = (int)Math.Round(most * share * scale);
            return new RingReach { In = inM, Out = inM + (gait == RingGait.Foot ? 6f : 4f), Most = Math.Max(1, n) };
        }
    }

    /// A gait has to hold this long before the ring follows it.
    public sealed class RingGaitWatch
    {
        public const int HoldMs = 1500;
        private RingGait _settled = RingGait.Foot, _seen = RingGait.Foot;
        private int _seenAt;

        public RingGait Settled { get { return _settled; } }

        public RingGait Update(RingGait now, int ms)
        {
            if (now != _seen)
            {
                _seen = now;
                _seenAt = ms;
            }
            if (_seen != _settled && (ms - _seenAt >= HoldMs || ms < _seenAt))
                _settled = _seen;
            return _settled;
        }
    }

    /// What the ring costs, and the check on it. Spent is told each step's microseconds; the average of the last Window steps, and
    /// repeated peaks, draw the ring in by a fifth (not below LeastScale), at most once every ShrinkGapMs; a long quiet spell at
    /// under half the budget lets it out again. The numbers go to F7 and the log, which is how the frame cost gets tested.
    public sealed class RingGuard
    {
        public const int Window = 64, AverageBudget = 1000, PeakBudget = 4000, PeaksToShrink = 2;   // microseconds a step: the run's 1 ms average, 4 ms peak
        public const int ShrinkGapMs = 5000, EaseAfterMs = 30000;
        public const float LeastScale = 0.4f;

        private readonly int[] _micros = new int[Window];
        private int _n, _peaks, _lastShrink = int.MinValue / 2, _quietSince = int.MinValue / 2;
        private long _total;

        public float Scale = 1f;
        public int StepsSeen { get; private set; }
        public int PeakMicros { get; private set; }
        public int Shrinks { get; private set; }
        public int Eases { get; private set; }

        public int AverageMicros
        {
            get { return _n == 0 ? 0 : (int)(_total / _n); }
        }

        public void Spent(long micros, int now)
        {
            int m = (int)Math.Max(0, Math.Min(micros, int.MaxValue));
            int slot = StepsSeen % Window;
            if (_n == Window)
                _total -= _micros[slot];
            else
                _n++;
            _micros[slot] = m;
            _total += m;
            StepsSeen++;
            if (m > PeakMicros)
                PeakMicros = m;
            if (m > PeakBudget)
                _peaks++;
            int avg = AverageMicros;
            bool over = (_n >= Window / 4 && avg > AverageBudget) || _peaks >= PeaksToShrink;
            if (over)
            {
                _quietSince = now;
                if (Scale > LeastScale && (now - _lastShrink >= ShrinkGapMs || now < _lastShrink))
                {
                    Scale = Math.Max(LeastScale, Scale * 0.8f);
                    _lastShrink = now;
                    _peaks = 0;
                    _n = 0;       // judged afresh at the new size: the old steps' cost would go on shrinking a ring that already fits
                    _total = 0;
                    Shrinks++;
                }
                return;
            }
            if (avg > AverageBudget / 2)
            {
                _quietSince = now;
                return;
            }
            if (now < _quietSince)
                _quietSince = now;   // the game clock went back
            if (Scale < 1f && now - _quietSince >= EaseAfterMs)
            {
                Scale = Math.Min(1f, Scale * 1.1f);
                _quietSince = now;
                Eases++;
            }
        }
    }
}
