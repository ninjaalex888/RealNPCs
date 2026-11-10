// The town's record of the rain, the engine's part (FOURTEENHOUR item 26; Core/RainShelter.cs). Each game minute the player is in a
// town, a minute of its sky; and when it's raining (or storming), a minute of rain, in the save's counts, a key a town a day. Read by
// the drought worry (InnerLife.RainDaysIn) and by the reasons (wet now, or within the hour after). A month's keys are kept.
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private long _skyNotedAt = -1, _skyPrunedDay = -1;

        /// The game minute it last rained where the player was (this session), or -1.
        public long LastWetAt = -1;

        /// From the game side's weather look: one minute of this town's sky, and of its rain when wet, once a game minute.
        public void NoteSky(string town, bool wet, long gameMin)
        {
            if (town == null || gameMin < 0 || gameMin == _skyNotedAt)
                return;
            _skyNotedAt = gameMin;
            long day = gameMin / 1440;
            Store.Counts[RainShelter.SkyKey(town, day)] = Store.Count(RainShelter.SkyKey(town, day)) + 1;
            if (wet)
            {
                Store.Counts[RainShelter.RainKey(town, day)] = Store.Count(RainShelter.RainKey(town, day)) + 1;
                LastWetAt = gameMin;
            }
            if (day != _skyPrunedDay)
            {
                _skyPrunedDay = day;
                foreach (string k in Store.Counts.Keys.Where(x => (x.StartsWith("sky|") || x.StartsWith("rain|")) && OldSky(x, day)).ToList())
                    Store.Counts.Remove(k);   // a month's record is enough
            }
        }

        private static bool OldSky(string key, long today)
        {
            int bar = key.LastIndexOf('|');
            long day;
            return bar > 0 && long.TryParse(key.Substring(bar + 1), out day) && today - day > 30;
        }

        /// Days with rain (20 minutes or more) in this town between two game days, inclusive; -1 when nobody saw its sky then.
        public int RainDaysIn(string town, long fromDay, long toDay)
        {
            if (town == null || toDay < fromDay)
                return -1;
            bool seen = false;
            int wet = 0;
            for (long d = fromDay; d <= toDay; d++)
            {
                if (Store.Count(RainShelter.SkyKey(town, d)) > 0)
                    seen = true;
                if (Store.Count(RainShelter.RainKey(town, d)) >= RainShelter.RainDayMinutes)
                    wet++;
            }
            return seen ? wet : -1;
        }

        /// Wet now or within the hour after (the shelter's talk).
        public bool WetLately(long gameMin)
        {
            return LastWetAt >= 0 && gameMin >= LastWetAt && gameMin - LastWetAt <= RainShelter.AfterRainMinutes;
        }
    }
}
