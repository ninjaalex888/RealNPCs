// A town on edge, the game side (NIGHT3 item 5; Core/TownMood.cs). The fear of the town the player is in, from the
// happenings already kept, worked out at most every 5 s: it raises everyone's safety need there (MindNeeds), goes into the
// model's request as "The town: ..." (MindRequestOf), and the novelty of a happening scales an onlooker's feeling
// (MindAppraiseOne). The hush (since 2026-10-31) comes from the latest killing alone, for [Mind] HushHours (Saint Denis
// HushHoursSaintDenis); a Testing row's own trouble is marked Test and never hushes (FromTestRow: the test's own people,
// or within TestHushMs of a fire unless it was the player's doing). HushedNow is asked afresh by whoever needs it.
// With [Mind] MindTownMood and MindLive. F7 TEST town mood:; log TOWN hushed / TOWN hush over.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

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

        /// A happening recorded this long after a Testing row fired (five real minutes, about two and a half game hours: its
        /// scene may run a few minutes, a standoff, an execution, a brawl and its fates) is the test's unless it was the
        /// player's own doing; and a test scene's cast are the test's people for as long.
        private const int TestHushMs = 300000;

        private readonly Dictionary<int, int> _testPeds = new Dictionary<int, int>();   // a test scene's cast, by handle: the test's people until then (game ms)

        /// Whether what is being recorded is a Testing row's own (TownMood.FromTest): done to or by the test's people
        /// (`focus`, `a`, `b`: the body of a death, the victim of the player's deed), or anything else within TestHushMs of
        /// a fire that wasn't the player's. The player's own killing after a test still hushes (the review, 2026-10-31).
        private bool FromTestRow(string doerId, int focus, int a, int b)
        {
            bool sinceFire = _fires > 0 && _now - _lastFireAt >= 0 && _now - _lastFireAt < TestHushMs;
            return TownMood.FromTest(TestPerson(focus) || TestPerson(a) || TestPerson(b), sinceFire, doerId);
        }

        /// One of the test's own people: the test cast (still bound to their part after they died: TestCastDrop lets the
        /// body go from the cast as the ledger books it), or one of a test scene's cast for TestHushMs after it began.
        private bool TestPerson(int h)
        {
            if (h == 0 || h == Game.Player.Character.Handle)
                return false;
            if (Spawner.IsCast(h))
                return true;
            ResBind rb;
            if (_resBind.TryGetValue(h, out rb) && rb.R != null && rb.R.Cast != null && ENTITY.DOES_ENTITY_EXIST(h) && ENTITY.GET_ENTITY_MODEL(h) == rb.Model)
                return true;
            int until;
            return _testPeds.TryGetValue(h, out until) && _now < until;
        }

        /// A Testing row's scene began with these people: what happens to them for TestHushMs is the test's.
        private void MarkTestPeople(List<Ped> people, int now)
        {
            if (_testPeds.Count > 64)
                foreach (int h in _testPeds.Where(kv => now >= kv.Value).Select(kv => kv.Key).ToList())
                    _testPeds.Remove(h);
            int me = Game.Player.Character.Handle;
            foreach (Ped p in people)
                if (p != null && p.Handle != me)
                    _testPeds[p.Handle] = now + TestHushMs;
        }

        /// Whether the street of the town the player is in is hushed now (the latest killing within HushHours), asked
        /// afresh (TownFearNow is cached 5 s): the pickpocket's own tick read the drama tick's flag, which an encounter or
        /// the gap after a scene leaves standing for a minute or more (the review, 2026-10-31).
        private bool HushedNow(int now)
        {
            return TownMoodOn && _placeTown != null && TownMood.Hushes(TownFearNow(now));
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
