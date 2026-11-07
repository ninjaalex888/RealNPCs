// The notice board (FIVEDAY item 16). The town's days were each posted on their own as they came: a toast for a dance, a
// clipping for an election or the show's bill. Now they're gathered on one board: what's coming in the next seven days, by
// town and by day, as a page in the reader (made each time it opens, never kept, like "Who's who"), and a short item in
// the "Around town" box when you come into a town that has something posted. On the board: what the town has posted
// (dances, elections, the circuit judge, a hanging, the show, a prize fight), the weddings that have been announced,
// enRAGE's funerals, and the town's standing days: Saturday's wages and Sunday's service. Shivarees aren't on it: they
// come as a surprise; nor a prize fight, kept from the marshal (the Part 3 review). Behind [World] NoticeBoard.
//
// Why it reads true: in 1899 a town's board stood by the post office or the general store, with bills for the dance, the
// sale and the show, notice of a funeral, the court's sitting. People planned the week by it. Plain C#, no game; all
// words original. mindtest NoticeBoardTest.
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnrageWestworld.Core
{
    public sealed class Notice
    {
        public long At;         // game minute it starts
        public string Town, Kind, Text;
    }

    public static class NoticeBoard
    {
        public const int Days = 7;

        private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

        /// What's coming in the next week, from the store: the posted events (not done) and the announced weddings. extra:
        /// the game side's (enRAGE's funerals). weekday: today's (0 Sunday), or -1 when it isn't known (no standing days then).
        public static List<Notice> Gather(Store s, long now, int weekday, IEnumerable<string> towns, IEnumerable<Notice> extra)
        {
            var list = new List<Notice>();
            long until = now + Days * 1440L;
            if (s != null)
            {
                foreach (TownEvent e in s.Events)
                {
                    if (e == null || !e.Posted || e.Done || e.Kind == Shivaree.Kind || e.Kind == PrizeFight.Kind || e.Until < now || e.At > until)
                        continue;
                    list.Add(new Notice { At = e.At, Town = e.Town, Kind = e.Kind, Text = EventText(e) });
                }
                foreach (Wedding w in s.Weddings)
                {
                    if (w == null || w.Done || !w.Announced || w.At < now - 60 || w.At > until)
                        continue;
                    list.Add(new Notice { At = w.At, Town = w.Town, Kind = "wedding", Text = "the wedding of " + w.AName + " and " + w.BName });
                }
            }
            if (extra != null)
                list.AddRange(extra.Where(n => n != null && n.At >= now - 60 && n.At <= until));
            if (weekday >= 0 && towns != null)
            {
                long today = now / 1440;
                foreach (string town in towns.Distinct())
                    for (int d = 0; d < Days; d++)
                    {
                        int wd = (weekday + d) % 7;
                        long day = today + d;
                        if (wd == 6 && (d > 0 || now % 1440 < Payday.ToHour * 60))
                            list.Add(new Notice { At = day * 1440 + Payday.FromHour * 60, Town = town, Kind = "payday", Text = "wages paid out" });
                        if (wd == 0 && (d > 0 || now % 1440 < 11 * 60))
                            list.Add(new Notice { At = day * 1440 + 10 * 60, Town = town, Kind = "service", Text = "Sunday service" });
                    }
            }
            return list.OrderBy(n => n.At).ThenBy(n => n.Town).ToList();
        }

        /// One posted event's line on the board.
        public static string EventText(TownEvent e)
        {
            switch (e.Kind)
            {
                case "dance": return "the dance";
                case "election": return "election day" + (e.Extra != null ? ", for " + e.Extra : "") + (e.AName != null && e.BName != null ? ": " + e.AName + " against " + e.BName : "");
                case "trial": return "the circuit judge sits" + (e.AName != null ? ": " + e.AName + " to answer for " + (e.Extra ?? "a killing") : "");
                case "gallows": return "a hanging" + (e.AName != null ? ": " + e.AName : "");
                case "show": return "the Wild West show" + (e.AName != null ? ", " + e.AName : "");
                default: return (e.Title ?? e.Kind ?? "something").ToLowerInvariant();
            }
        }

        /// The hour as the board gives it ("at 7 in the evening", "at noon").
        public static string Hour(long at)
        {
            int h = (int)(at / 60 % 24), m = (int)(at % 60);
            if (h == 12 && m == 0)
                return "at noon";
            string part = h < 12 ? "in the morning" : h < 17 ? "in the afternoon" : "in the evening";
            int h12 = h % 12 == 0 ? 12 : h % 12;
            return "at " + h12 + (m > 0 ? ":" + m.ToString("00") : "") + " " + part;
        }

        /// "Today", "Tomorrow", or the day's name.
        public static string DayOf(long at, long now, int weekday)
        {
            long d = at / 1440 - now / 1440;
            if (d <= 0) return "Today";
            if (d == 1) return "Tomorrow";
            return weekday >= 0 ? DayNames[(int)((weekday + d) % 7)] : "In " + d + " days";
        }

        /// The reader's page: this town's first (when you're in one), then the others, each by day.
        public static string Body(List<Notice> notices, string here, long now, int weekday)
        {
            var b = new StringBuilder();
            b.Append("What's posted for the coming week, as the boards by the post offices and the stores have it.");
            if (notices == null || notices.Count == 0)
            {
                b.Append("\n\nNothing posted anywhere yet.");
                return b.ToString();
            }
            var towns = notices.Select(n => n.Town).Where(t => t != null).Distinct().OrderBy(t => t == here ? 0 : 1).ThenBy(t => t).ToList();
            foreach (string town in towns)
            {
                var mine = notices.Where(n => n.Town == town).ToList();
                bool onlyStanding = mine.All(n => n.Kind == "payday" || n.Kind == "service");
                if (onlyStanding && town != here)
                    continue;   // a town with nothing but its Saturday and Sunday: left off, unless you're in it
                b.Append("\n\n").Append(town.ToUpperInvariant()).Append(town == here ? " (here)" : "");
                foreach (var day in mine.GroupBy(n => n.At / 1440))
                {
                    b.Append("\n").Append(DayOf(day.First().At, now, weekday)).Append(": ");
                    b.Append(string.Join("; ", day.Select(n => n.Text + " " + Hour(n.At))));
                    b.Append(".");
                }
            }
            return b.ToString();
        }

        /// The "Around town" item for a town as you come into it, or null when it has nothing but its standing days.
        public static string Box(List<Notice> notices, string town, long now, int weekday)
        {
            var mine = (notices ?? new List<Notice>()).Where(n => n.Town == town && n.Kind != "payday" && n.Kind != "service").Take(4).ToList();
            if (mine.Count == 0)
                return null;
            return "Posted in " + town + ": " + string.Join("; ", mine.Select(n => DayOf(n.At, now, weekday).ToLowerInvariant() + ", " + n.Text + " " + Hour(n.At))) + ". The rest is in the reader.";
        }
    }
}
