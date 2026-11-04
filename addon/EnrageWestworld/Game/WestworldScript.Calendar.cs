// The year as it was, the game side (SIXDAY item 3; Core/Calendar.cs has the dates and the words): the date read from the
// game's clock (ReadClock) for the thoughts, and on a holiday's morning one word of the day on the ticker, once a year per
// player. Behind [World] HistoricalAccuracy. F7 "TEST calendar:" (today's date, the holiday, the next and how far off).
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _calendarNextAt, _calendarToasts;
        private string _calendarLast;

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
