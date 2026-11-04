// The engine's side of the calendar (SIXDAY item 3; Core/Calendar.cs): each game day, what the date itself brings:
// the Panic of 1907 in the paper from 22 October (roadmap.md P11); the almanac's week in the reader and its biggest things as
// the town's own paper items (Core/Almanac.cs). Behind [World] HistoricalAccuracy (InnerLife.Historical).
using System;
using System.Collections.Generic;
using System.Linq;

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
            AlmanacDaily(now, month, day);
        }

        /// The almanac's week in the reader every seven days (a clipping of the dated things of the week, the last four kept),
        /// and the town's own paper item the day something the whole country talks of breaks, once each (Core/Almanac.cs).
        private void AlmanacDaily(long now, int month, int day)
        {
            if (!Almanac.On || !Cfg.Newspaper || month < 1 || day < 1)
                return;
            int today = (int)(now / 1440), last = Store.Count("almanac:week");
            if (last == 0 || today - last >= 7)
            {
                if (AlmanacPaper(now, month, day, 7, "wk" + today))
                    Store.Counts["almanac:week"] = today;
            }
            foreach (AlmanacEvent e in Almanac.Fresh(InnerLife.ClockYear, month, day, Almanac.FreshDays))
            {
                if (e.Big < 3 || Store.Count("almanac:item:" + e.Key) > 0 || (e.Key == Almanac.CalendarCovers && Store.Count("paper:panic1907") > 0))
                    continue;
                Store.Counts["almanac:item:" + e.Key] = today;
                Requests.Add(new WorldRequest { Kind = "paper", Text = Almanac.PaperItem(e), Town = CurrentTown ?? "Saint Denis" });
                Log("ALMANAC in the paper: " + e.Head + " (" + day + "/" + month + ")");
            }
        }

        /// The paper of the last `days` into the reader as a clipping; false when nothing dated fell in them.
        private bool AlmanacPaper(long now, int month, int day, int days, string seed)
        {
            string title;
            string body = Almanac.WeekPaper(InnerLife.ClockYear, month, day, CurrentTown, seed, days, out title);
            if (body == null)
                return false;
            List<Paper> old = Store.Papers.Where(x => x != null && x.Kind == "clipping" && x.From == Almanac.PaperFrom).OrderBy(x => x.At).ToList();
            for (int i = 0; i + 3 < old.Count; i++)
                Store.Papers.Remove(old[i]);   // the last three stay with the new one
            Store.Papers.Add(new Paper { Id = Ids.New(), Kind = "clipping", Title = title, From = Almanac.PaperFrom, Body = body, At = now, Delivered = true, Town = CurrentTown });
            Log("ALMANAC the paper in the reader: " + title);
            return true;
        }

        /// The Testing row: the fortnight's paper into the reader now, and what is known today, for the log.
        public string AlmanacTest(long now)
        {
            if (!InnerLife.Historical || !Almanac.On)
                return "off ([World] HistoricalAccuracy, Almanac)";
            int year = InnerLife.ClockYear, month = InnerLife.ClockMonth, day = InnerLife.ClockDay;
            List<AlmanacEvent> known = Almanac.Known(year, month, day);
            if (month < 1 || day < 1)
                return "no date from the clock (the everyday talk only: " + known.Count + " things)";
            bool paper = AlmanacPaper(now, month, day, 14, "test" + now);
            AlmanacEvent f = Almanac.Freshest(year, month, day);
            return (f != null ? "freshest: " + f.Key + " (" + f.Head + "), " + Almanac.Age(f, Almanac.Today(year, month, day)) + " days old" : "nothing dated this fortnight") +
                   "; " + known.Count + " things known on " + day + "/" + month + "/" + year + "; " + (paper ? "the fortnight's paper is in the reader" : "nothing dated for the paper");
        }
    }
}
