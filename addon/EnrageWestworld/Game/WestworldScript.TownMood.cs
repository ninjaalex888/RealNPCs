// A town on edge, the game side (NIGHT3 item 5; Core/TownMood.cs). The fear of the town the player is in, from the
// happenings already kept, worked out at most every 5 s: it raises everyone's safety need there (MindNeeds), goes into the
// model's request as "The town: ..." (MindRequestOf), and the novelty of a happening scales an onlooker's feeling
// (MindAppraiseOne). The hush (since 2026-10-31) comes from the latest killing alone, for [Mind] HushHours (Saint Denis
// HushHoursSaintDenis); a happening recorded within TestHushMs of a Testing row's fire is marked Test and never hushes.
// With [Mind] MindTownMood and MindLive. F7 TEST town mood:; log TOWN hushed / TOWN hush over.
using System;
using System.Linq;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private TownFear _townFear = new TownFear();
        private int _townFearAt = -100000;
        private string _townFearFor;
        private int _townNoveltyLess;

        private bool TownMoodOn { get { return _cfg.MindLive && _cfg.MindTownMood && _store != null; } }

        private bool _hush;   // the street hushed this drama tick (TownMood.Hushes)
        private int _hushHeld;
        private Happening _hushLogged;   // the killing whose hush was last logged (TOWN hushed), until it's over

        /// A happening recorded this long after a Testing row fired is the test's (its scene may run a few minutes: a
        /// standoff, an execution, a brawl and its fates): it never hushes a street. A fallback: no test flag follows the
        /// people of a test scene to their deaths.
        private const int TestHushMs = 300000;

        private bool FromTestRow()
        {
            return _fires > 0 && _now - _lastFireAt >= 0 && _now - _lastFireAt < TestHushMs;
        }

        private static readonly TownFear CalmTown = new TownFear();

        private TownFear TownFearNow(int now)
        {
            if (!TownMoodOn || _placeTown == null)
                return _townFear = CalmTown;   // one, not one a call (the cost audit: the needs ask about thirty times a second; nobody writes to it)
            if (now - _townFearAt >= 5000 || now < _townFearAt || _townFearFor != _placeTown)
            {
                _townFearAt = now;
                _townFearFor = _placeTown;
                _townFear = TownMood.Fear(_store.WorldLog, _placeTown, _store.LastGameMin);
                HushLogStep(_townFear);
            }
            return _townFear;
        }

        /// Logs a hush as it begins (once a killing) and as it ends.
        private void HushLogStep(TownFear f)
        {
            if (TownMood.Hushes(f))
            {
                if (f.HushBy != _hushLogged)
                {
                    _hushLogged = f.HushBy;
                    Log.Write("TOWN hushed: " + _placeTown + " for " + f.HushLeft + " more game min (no fights, thefts or callouts from the street's timing), since " + HushWhat(f));
                }
            }
            else if (_hushLogged != null)
            {
                Log.Write("TOWN hush over: " + (_hushLogged.Town ?? _placeTown) + " (" + _hushHeld + " scenes held back while hushed this session)");
                _hushLogged = null;
            }
        }

        private string HushWhat(TownFear f)
        {
            return f.HushBy == null ? "nothing" : WorldEvents.Describe(f.HushBy) + ", " + Math.Max(0, _store.LastGameMin - f.HushBy.At) + " game min ago";
        }

        private float TownNovelty(Happening ev, int now)
        {
            if (!TownMoodOn || ev == null)
                return 1f;
            float n = TownMood.Novelty(_store.WorldLog, ev.Town, ev.Kind, ev.At, ev);
            if (n < 1f)
                _townNoveltyLess++;
            return n;
        }

        private string TownMoodStatus()
        {
            if (!TownMoodOn)
                return "off ([Mind] MindTownMood, MindLive)";
            TownFear f = TownFearNow(Environment.TickCount);
            long g = _store.LastGameMin;
            string words = TownMood.Words(f, g);
            float hours = _placeTown == "Saint Denis" ? _cfg.HushHoursSaintDenis : _cfg.HushHours;
            string hush;
            if (_placeTown == null)
                hush = "";
            else if (TownMood.Hushes(f))
                hush = "; the street is hushed for " + f.HushLeft + " more game min (no fights, thefts or callouts from the street's timing), since " + HushWhat(f);
            else if (hours <= 0f)
                hush = "; never hushed here (" + (_placeTown == "Saint Denis" ? "HushHoursSaintDenis" : "HushHours") + " 0)";
            else
                hush = "; not hushed (" + hours.ToString("0.#") + " game h after a killing" + (f.HushBy != null ? "; the latest " + Math.Max(0, g - f.HushBy.At) + " game min ago" : "; none in two days") + ")";
            int quiet = _placeTown == null ? 0 : _store.WorldLog.Count(h => h != null && h.Town == _placeTown && TownMood.Killing(h.Kind) && !TownMood.Hushing(h) && g - h.At <= TownMood.WindowMinutes && h.At <= g);
            return (_placeTown ?? "out of the towns") + ": fear " + f.Level.ToString("0.00") + (words != null ? " (" + words + ")" : " (calm)") +
                   (f.Worst != null ? "; the worst: " + f.Worst.Kind + (TownMood.Beaten(f.Worst) ? " (" + f.Worst.Outcome + ")" : "") + " " + Math.Max(0, g - f.Worst.At) + " game min ago" : "") +
                   "; onlookers moved less by a sight seen again: " + _townNoveltyLess + " this session" + hush +
                   (quiet > 0 ? "; killings that never hush (a beating, a Testing row): " + quiet : "") + (_hushHeld > 0 ? "; scenes held back while hushed: " + _hushHeld : "");
        }
    }
}
