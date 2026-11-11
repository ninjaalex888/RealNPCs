// A night's sleep cools it (DEPTH item 19; [Mind] SleepOnIt, on). Overnight, a hostile tie that nothing fed the day before (no scene
// between the two since the start of that day) eases a step: sore to merely cool, cool to nothing; one fed that day (a quarrel, a
// rematch, a word) doesn't. So a feud left alone fades within a day or two of play, and one kept alive by a rematch or a word hardens.
// The kind of the tie and who did what (its Doer, a theft kept as Crime) stay: only the heat goes. Run once at each day's turn
// (Engine's daily tick, midnight by the game's clock). Why it reads true: feeling fades with time and sleep unless rumination or a
// new provocation feeds it. Off in Core so the old harnesses' goldens stand; the game (ApplySettings) and the commit gate's measure set
// it from the settings, on as it comes. Plain C#.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class SleepOnIt
    {
        public static bool On;   // [Mind] SleepOnIt (on as it comes), set by the game; off in Core for the old harnesses

        /// At the turn of the day `now` (a game minute in the new day): every hostile tie last fed before the day just ended began
        /// eases a step toward nothing. How many eased.
        public static int Night(List<SceneTie> ties, long now)
        {
            if (!On || ties == null)
                return 0;
            long dayBegan = (now / 1440 - 1) * 1440;   // the start of the day that has just ended
            int eased = 0;
            foreach (SceneTie t in ties)
            {
                if (t == null || t.Warmth >= 0 || t.At >= dayBegan || t.At > now)
                    continue;   // not hostile, or fed that day (or after: a clock gone back)
                if (t.Sore && t.Count <= 1)
                    continue;   // a big brawl's sore sides keep their week (FOURTEENHOUR item 12; review 2)
                if (t.EasedDay >= now / 1440)
                    continue;   // eased already this day (a reload across midnight: review 2)
                t.EasedDay = now / 1440;
                t.Warmth++;
                eased++;
            }
            return eased;
        }
    }
}
