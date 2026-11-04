// The engine's side of the calendar (SIXDAY item 3; Core/Calendar.cs): each game day, what the date itself brings:
// the Panic of 1907 in the paper from 22 October (roadmap.md P11). Behind [World] HistoricalAccuracy (InnerLife.Historical).
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// From DailyTick: the dated things. The paper carries the Panic once, from 22 October 1907 (the clock's own year
        /// 1907, or John's 1907 with the clock on 1899: the month and day are the clock's either way).
        private void CalendarDaily(long now)
        {
            if (!InnerLife.Historical || Store == null)
                return;
            int month = InnerLife.ClockMonth, day = InnerLife.ClockDay;
            if (InnerLife.ClockYear >= 1907 && ((month == 10 && day >= 22) || month == 11) && Store.Count("paper:panic1907") == 0)
            {
                Store.Counts["paper:panic1907"] = (int)(now / 1440);
                Requests.Add(new WorldRequest { Kind = "paper", Text = Calendar.PanicPaper(), Town = CurrentTown ?? "Saint Denis" });
                Log("CALENDAR the Panic of 1907 is in the paper (" + day + "/" + month + ")");
            }
        }
    }
}
