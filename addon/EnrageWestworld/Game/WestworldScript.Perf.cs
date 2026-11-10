// The frame-time meter (overnight 2026-10-27, Tier 1.1): every Safe() feature is timed with the high-resolution clock,
// its own time only (a feature inside another, such as the reactions inside the scenes, is subtracted from its
// parent), and once a frame each one's time goes into a rolling average and a 10-second peak. The whole of OnTick's
// Step is "total"; what runs outside any Safe() is "step (other)". The budget for the whole add-on: 1 ms a frame on
// average, 4 ms at peak. Nothing here allocates per call or per frame (a slot is made once, the first time a feature
// runs); the sorting happens only for the F7 dump and the once-a-minute warnings.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class PerfSlot
        {
            public string Name;
            public long Frame;          // this frame's own time, in Stopwatch ticks
            public int FrameCalls;
            public double Avg, Calls;   // ms and calls per frame, rolling (about 2 s)
            public double Peak, PeakWin; // ms: the worst frame in the last full 10 s window, and in this one
        }

        private const double BudgetAvgMs = 1.0, BudgetPeakMs = 4.0;
        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;
        private readonly Dictionary<string, PerfSlot> _perf = new Dictionary<string, PerfSlot>();
        private readonly List<PerfSlot> _perfList = new List<PerfSlot>();
        private readonly long[] _perfChild = new long[24];   // nested features' time, by depth
        private int _perfDepth;
        private long _perfTop;                                 // this frame's time inside top-level Safe() calls
        private readonly PerfSlot _perfTotal = new PerfSlot { Name = "total" };
        private readonly PerfSlot _perfOther = new PerfSlot { Name = "step (other)" };
        private int _perfWindowAt, _perfOverSince = -1, _perfOverLogged = -60000, _perfPeakLogged = -60000, _perfFrames;
        private bool _perfUnseen;   // this frame nobody could see (paused, faded out, loading: Step's !InWorld): no peak warning for it, since the saves wait for these

        private PerfSlot PerfSlotOf(string feature)
        {
            PerfSlot s;
            if (!_perf.TryGetValue(feature, out s))
            {
                s = new PerfSlot { Name = feature };
                _perf[feature] = s;
                _perfList.Add(s);
            }
            return s;
        }

        /// Called by Safe() around a feature: returns the start time and the depth it ran at.
        private long PerfBegin(out int depth)
        {
            depth = _perfDepth++;
            if (depth < _perfChild.Length)
                _perfChild[depth] = 0;
            return Stopwatch.GetTimestamp();
        }

        private void PerfEnd(PerfSlot slot, long start, int depth)
        {
            long elapsed = Stopwatch.GetTimestamp() - start;
            _perfDepth = depth;
            long children = depth < _perfChild.Length ? _perfChild[depth] : 0;
            slot.Frame += Math.Max(0, elapsed - children);
            slot.FrameCalls++;
            if (depth == 0)
                _perfTop += elapsed;
            else if (depth - 1 < _perfChild.Length)
                _perfChild[depth - 1] += elapsed;
        }

        /// The parts of Step that run outside any Safe get slots of their own, "<name> (step)", so a slow frame there is
        /// named, not "step (other)" (2026-10-27: a 15 ms frame every few seconds was left there once the save had its
        /// own). Their errors still go to OnTick's catch, as before; a throw leaves the depth to PerfFrame's reset.
        private long StepBegin(out int depth)
        {
            if (_cfg != null && !_cfg.PerfMeter)
            {
                depth = -1;
                return 0;
            }
            return PerfBegin(out depth);
        }

        // ---- sub-slots inside a feature (FOURTEENHOUR item 2; TWELVEHOUR's correction: after THREEHOUR item 1 the add-on still
        // averaged 1.09-1.38 ms a frame, and "scenes" and "name cards" were too big to say which part): a part of a Safe()
        // feature timed on its own, its own time only, and taken off its parent's the way a nested Safe() is, so the parent's
        // line is what's left. A struct, so nothing is allocated; the slot is found once and kept in the caller's field.
        private struct PerfSub
        {
            public long At, Kids;
        }

        private PerfSub SubStart()
        {
            if (_cfg != null && !_cfg.PerfMeter)
                return default(PerfSub);
            int d = _perfDepth - 1;
            return new PerfSub { At = Stopwatch.GetTimestamp(), Kids = d >= 0 && d < _perfChild.Length ? _perfChild[d] : 0 };
        }

        /// The part since `s` into `slot` (made the first time, named `name`): its time less any Safe() run inside it.
        private void SubEnd(ref PerfSlot slot, string name, PerfSub s)
        {
            if (s.At == 0)
                return;
            int d = _perfDepth - 1;
            long kids = d >= 0 && d < _perfChild.Length ? _perfChild[d] - s.Kids : 0;
            SubTicks(ref slot, name, Stopwatch.GetTimestamp() - s.At - kids);
        }

        /// Ticks gathered by the caller (a part spread through a loop: the name cards' placing, walls and drawing), added once.
        private void SubTicks(ref PerfSlot slot, string name, long ticks)
        {
            if (_cfg != null && !_cfg.PerfMeter)
                return;
            if (slot == null)
                slot = PerfSlotOf(name);
            long own = Math.Max(0, ticks);
            slot.Frame += own;
            slot.FrameCalls++;
            int d = _perfDepth - 1;
            if (d >= 0 && d < _perfChild.Length)
                _perfChild[d] += own;   // the parent's own time is what's left
            else if (d < 0)
                _perfTop += own;   // outside any feature: not counted again in "step (other)"
        }

        private void StepEnd(string slot, long start, int depth)
        {
            if (depth >= 0)
                PerfEnd(PerfSlotOf(slot), start, depth);
        }

        /// Once a frame, from OnTick with the whole Step's time: the averages, the peaks, and the warnings.
        private void PerfFrame(long stepTicks)
        {
            bool unseen = _perfUnseen;
            _perfUnseen = false;
            if (_cfg != null && !_cfg.PerfMeter)
                return;
            _perfDepth = 0;   // a feature that threw past its finally can't leave the depth wrong for good
            int now = Environment.TickCount;
            if (_perfWindowAt == 0)
                _perfWindowAt = now;
            bool window = now - _perfWindowAt >= 10000;
            if (window)
                _perfWindowAt = now;
            _perfFrames++;
            double totalMs = stepTicks * TicksToMs;
            Roll(_perfTotal, totalMs, 1, window);
            double otherMs = Math.Max(0, stepTicks - _perfTop) * TicksToMs;
            Roll(_perfOther, otherMs, 1, window);
            _perfTop = 0;
            PerfSlot worst = null, second = null, third = null;
            double worstMs = 0, secondMs = 0, thirdMs = 0;   // kept apart: each slot's Frame is zeroed below, so comparing with worst.Frame named the last to run
            for (int i = 0; i < _perfList.Count; i++)
            {
                PerfSlot s = _perfList[i];
                double ms = s.Frame * TicksToMs;
                // the frame's three biggest parts, for the peak line (FOURTEENHOUR item 2: with the sub-slots, one name said too little)
                if (worst == null || ms > worstMs)
                {
                    third = second; thirdMs = secondMs;
                    second = worst; secondMs = worstMs;
                    worst = s;
                    worstMs = ms;
                }
                else if (second == null || ms > secondMs)
                {
                    third = second; thirdMs = secondMs;
                    second = s; secondMs = ms;
                }
                else if (third == null || ms > thirdMs)
                {
                    third = s; thirdMs = ms;
                }
                int calls = s.FrameCalls;
                s.Frame = 0;
                s.FrameCalls = 0;
                Roll(s, ms, calls, window);
            }
            if (_perfFrames < 120)
                return;   // the first two seconds after loading (and after the save loads: LoadStore) settle first
            // over budget on average for 10 s: once a minute at most, with what costs most
            if (_perfTotal.Avg > BudgetAvgMs)
            {
                if (_perfOverSince < 0)
                    _perfOverSince = now;
                else if (now - _perfOverSince >= 10000 && now - _perfOverLogged >= 60000)
                {
                    _perfOverLogged = now;
                    Log.Write("PERF: over budget: the add-on has averaged " + _perfTotal.Avg.ToString("0.00") + " ms a frame for 10 s (budget " + BudgetAvgMs.ToString("0.0") +
                              " ms; peak " + _perfTotal.Peak.ToString("0.00") + " ms). Costliest: " + PerfTop(5));
                }
            }
            else
                _perfOverSince = -1;
            if (totalMs > BudgetPeakMs && !unseen && now - _perfPeakLogged >= 60000)
            {
                _perfPeakLogged = now;
                Log.Write("PERF: peak: one frame took " + totalMs.ToString("0.00") + " ms (budget " + BudgetPeakMs.ToString("0.0") + " ms)" +
                          (worst != null && worstMs >= otherMs ? "; the most of it: " + worst.Name : "; the most of it: step (other), outside any feature") +
                          (worst != null ? " (" + worst.Name + " " + worstMs.ToString("0.00") + " ms" + (second != null && secondMs > 0.05 ? ", " + second.Name + " " + secondMs.ToString("0.00") + " ms" : "") +
                                           (third != null && thirdMs > 0.05 ? ", " + third.Name + " " + thirdMs.ToString("0.00") + " ms" : "") + (otherMs > 0.05 ? ", outside any feature " + otherMs.ToString("0.00") + " ms" : "") + ")" : ""));
            }
        }

        private static void Roll(PerfSlot s, double ms, int calls, bool window)
        {
            s.Avg = s.Avg * 0.99 + ms * 0.01;   // about two seconds at 60 frames
            s.Calls = s.Calls * 0.99 + calls * 0.01;
            if (ms > s.PeakWin)
                s.PeakWin = ms;
            if (window)
            {
                s.Peak = s.PeakWin;
                s.PeakWin = 0;
            }
            else if (s.PeakWin > s.Peak)
                s.Peak = s.PeakWin;
        }

        private string PerfTop(int n)
        {
            return string.Join(", ", _perfList.OrderByDescending(s => s.Avg).Take(n).Select(s => s.Name + " " + s.Avg.ToString("0.000") + " ms"));
        }

        /// For the F7 dump: the whole add-on's cost, and the ten costliest features.
        private string PerfStatus()
        {
            if (_cfg != null && !_cfg.PerfMeter)
                return "off ([Output] PerfMeter)";
            return "total " + _perfTotal.Avg.ToString("0.000") + " ms a frame (peak " + _perfTotal.Peak.ToString("0.00") + " ms in the last 10 s; budget " + BudgetAvgMs.ToString("0.0") + " / " +
                   BudgetPeakMs.ToString("0.0") + "), outside any feature " + _perfOther.Avg.ToString("0.000") + " ms; costliest: " +
                   string.Join("; ", _perfList.OrderByDescending(s => s.Avg).Take(10).Select(s => s.Name + " " + s.Avg.ToString("0.000") + " ms (peak " + s.Peak.ToString("0.00") + ", " +
                                                                                                s.Calls.ToString("0.#") + " calls)"));
        }
    }
}
