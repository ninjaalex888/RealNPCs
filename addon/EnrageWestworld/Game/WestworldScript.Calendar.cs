// The year as it was, the game side (SIXDAY item 3; Core/Calendar.cs has the dates and the words): the date read from the
// game's clock (ReadClock) for the thoughts, and on a holiday's morning one word of the day on the ticker, once a year per
// player; and the almanac's word on the ticker the day something big is in the paper, once each (Core/Almanac.cs). Behind
// [World] HistoricalAccuracy. F7 "TEST calendar:" (today's date, the holiday, the next and how far off) and "TEST almanac:".
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _calendarNextAt, _calendarToasts, _almanacWords;
        private string _calendarLast, _almanacLast;

        /// Tick04, every 30 s: the holiday's word of the day in the morning, once.
        private void CalendarStep(int now)
        {
            if (now < _calendarNextAt)
                return;
            _calendarNextAt = now + 30000;
            if (!_cfg.HistoricalAccuracy || _store == null || _engine == null)
                return;
            Safe("calendar", () =>
            {
                int hourNow = CLOCK.GET_CLOCK_HOURS();
                if (_cfg.Almanac && hourNow >= 6 && hourNow < 22)
                    AlmanacWord(false);
                if (hourNow < 6 || hourNow >= 11)
                    return;
                string region = Calendar.Region(_placeTown ?? _town);
                Holiday h = Calendar.Today(InnerLife.CalendarYear, InnerLife.ClockMonth, InnerLife.ClockDay, region);
                if (h == null)
                    return;
                string key = "holiday:" + h.Key + ":" + InnerLife.CalendarYear;
                if (_store.Count(key) > 0)
                    return;
                _store.Counts[key] = 1;
                string toast = Calendar.Toast(h, InnerLife.ClockYear);
                _ticker.Push("Today", toast, "news");
                _calendarToasts++;
                _calendarLast = h.Name;
                Log.Write("CALENDAR " + h.Name + " (" + InnerLife.ClockDay + "/" + InnerLife.ClockMonth + "/" + InnerLife.CalendarYear + "): " + toast);
            });
        }

        /// The almanac's word on the ticker: the newest big thing in the paper within the last few days, once each, one a
        /// step; `any` (the Testing row) takes the freshest thing whatever its size, said or not.
        private bool AlmanacWord(bool any)
        {
            int year = InnerLife.ClockYear, month = InnerLife.ClockMonth, day = InnerLife.ClockDay;
            AlmanacEvent pick = null;
            foreach (AlmanacEvent e in Almanac.Fresh(year, month, day, any ? Almanac.WarmDays : Almanac.FreshDays))
            {
                if (!any && (e.Big < 2 || _store.Count("almanac:word:" + e.Key) > 0))
                    continue;
                pick = e;
                break;
            }
            if (pick == null && any)
            {
                var known = Almanac.Known(year, month, day);
                if (known.Count > 0)
                    pick = known[Voice.Hash("aw" + _now) % known.Count];
            }
            if (pick == null)
                return false;
            _store.Counts["almanac:word:" + pick.Key] = 1;
            _ticker.Push("In the paper", Almanac.PaperItem(pick), "news");
            _almanacWords++;
            _almanacLast = pick.Key;
            Log.Write("ALMANAC on the ticker: " + pick.Head + " (" + day + "/" + month + "/" + year + ")");
            return true;
        }

        private string AlmanacStatus()
        {
            if (!_cfg.HistoricalAccuracy || !_cfg.Almanac)
                return "off ([World] HistoricalAccuracy, Almanac)";
            int year = InnerLife.ClockYear, month = InnerLife.ClockMonth, day = InnerLife.ClockDay;
            var known = Almanac.Known(year, month, day);
            var fresh = Almanac.Fresh(year, month, day, Almanac.WarmDays);
            AlmanacEvent f = Almanac.Freshest(year, month, day);
            return "on (share " + _cfg.AlmanacShare + "%); " + known.Count + " things known on " + day + "/" + month + "/" + year + ", " + fresh.Count + " of the fortnight" +
                   (f != null ? "; freshest: " + f.Key + " (" + f.Head + "), " + Almanac.Age(f, Almanac.Today(year, month, day)) + " days old" : "; nothing dated this fortnight") +
                   "; pull " + Almanac.Pull(year, month, day) + "; " + _almanacWords + " words on the ticker this session" + (_almanacLast != null ? " (last: " + _almanacLast + ")" : "");
        }

        private string CalendarStatus()
        {
            if (!_cfg.HistoricalAccuracy)
                return "off ([World] HistoricalAccuracy): no dated events, no real calendar, no talk of the times";
            string region = Calendar.Region(_placeTown ?? _town);
            Holiday today = Calendar.Today(InnerLife.CalendarYear, InnerLife.ClockMonth, InnerLife.ClockDay, region);
            int away;
            Holiday next = Calendar.Next(InnerLife.CalendarYear, InnerLife.ClockMonth, InnerLife.ClockDay, region, out away);
            return "on; the clock says " + InnerLife.ClockDay + "/" + InnerLife.ClockMonth + "/" + InnerLife.CalendarYear + " (the era " + InnerLife.ClockYear + ", weekday " + InnerLife.Weekday + ", " + region + ")" +
                   (today != null ? "; today is " + today.Name : "") + (next != null ? "; next: " + next.Name + " in " + away + " day" + (away == 1 ? "" : "s") : "") +
                   "; " + _calendarToasts + " words of the day this session" + (_calendarLast != null ? " (last: " + _calendarLast + ")" : "");
        }
    }
}
