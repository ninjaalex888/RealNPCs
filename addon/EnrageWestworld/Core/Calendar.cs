// The year as it was (roadmap 23; SIXDAY item 3; Lex's brief: historical context "must be a strictly togglable setting"):
// the real calendar of 1899 and 1907, keyed to the game's own date (GET_CLOCK_YEAR, GET_CLOCK_MONTH 0-based, the day),
// which follows the real weekdays (2 April 1899 was the Sunday Lex's clock read). Holidays on their true dates, movable
// ones computed (Easter 2 April 1899 and 31 March 1907, Mardi Gras 47 days before, Labor Day the first Monday of
// September, Election Day the first Tuesday after the first Monday of November, Thanksgiving the last Thursday); what the
// day before, the day and the day after bring to mind; and the year's work by region (ranch country and the north,
// Lemoyne and the bayou, the high country), two months at a time. All of it stands behind [World] HistoricalAccuracy
// (InnerLife.Historical): off, nothing here is thought or said, and the timeless texture of the days stays as it was.
// Every line is original to this add-on and short enough for the ticker.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Holiday
    {
        public string Key, Name;
        public int Month, Day;    // 1-based
        public string Where;      // null: everywhere; "south": Lemoyne and the bayou (Mardi Gras, All Saints'); "city": Saint Denis and the river parishes (the parades)
    }

    public static class Calendar
    {
        private static readonly string[] South = { "Saint Denis", "Rhodes", "Lagras", "Braithwaite", "Bayou Nwa", "Scarlett Meadows", "Bluewater Marsh", "Caliga", "Lemoyne" };
        private static readonly string[] High = { "Colter", "Wapiti", "Strawberry", "Ambarino", "Grizzlies" };

        /// ranch (ranch country and the north), south (Lemoyne and the bayou), high (the high country).
        public static string Region(string town)
        {
            if (town == null || town.Length == 0)
                return "ranch";
            if (South.Any(t => string.Equals(t, town, StringComparison.OrdinalIgnoreCase)))
                return "south";
            if (High.Any(t => string.Equals(t, town, StringComparison.OrdinalIgnoreCase)))
                return "high";
            return "ranch";
        }

        /// Easter Sunday by the Gregorian computation (2 April 1899, 31 March 1907).
        public static DateTime Easter(int year)
        {
            int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4, f = (b + 8) / 25, g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4, l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
            return new DateTime(year, month, day);
        }

        /// 0 Sunday to 6 Saturday, the clock's own count.
        public static int Weekday(int year, int month, int day)
        {
            return (int)new DateTime(year, month, day).DayOfWeek;
        }

        private static DateTime Nth(int year, int month, DayOfWeek dow, int n)
        {
            DateTime d = new DateTime(year, month, 1);
            int off = ((int)dow - (int)d.DayOfWeek + 7) % 7;
            return d.AddDays(off + 7 * (n - 1));
        }

        private static DateTime Last(int year, int month, DayOfWeek dow)
        {
            DateTime d = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            int off = ((int)d.DayOfWeek - (int)dow + 7) % 7;
            return d.AddDays(-off);
        }

        private static Holiday H(string key, string name, DateTime d, string where = null)
        {
            return new Holiday { Key = key, Name = name, Month = d.Month, Day = d.Day, Where = where };
        }

        private static readonly Dictionary<int, List<Holiday>> _years = new Dictionary<int, List<Holiday>>();

        /// The year's holidays, by their true dates.
        public static List<Holiday> Of(int year)
        {
            List<Holiday> list;
            if (_years.TryGetValue(year, out list))
                return list;
            DateTime easter = Easter(year);
            DateTime firstMonNov = Nth(year, 11, DayOfWeek.Monday, 1);
            list = new List<Holiday>
            {
                H("newyear", "New Year's Day", new DateTime(year, 1, 1)),
                H("mardigras", "Mardi Gras", easter.AddDays(-47), "south"),
                H("ashwednesday", "Ash Wednesday", easter.AddDays(-46), "south"),
                H("washington", "Washington's Birthday", new DateTime(year, 2, 22)),
                H("stpatrick", "Saint Patrick's Day", new DateTime(year, 3, 17), "city"),
                H("goodfriday", "Good Friday", easter.AddDays(-2)),
                H("easter", "Easter Sunday", easter),
                H("mayday", "May Day", new DateTime(year, 5, 1)),
                H("decoration", "Decoration Day", new DateTime(year, 5, 30)),
                H("fourth", "Independence Day", new DateTime(year, 7, 4)),
                H("labor", "Labor Day", Nth(year, 9, DayOfWeek.Monday, 1), "city"),
                H("halloween", "Halloween", new DateTime(year, 10, 31)),
                H("allsaints", "All Saints' Day", new DateTime(year, 11, 1), "south"),
                H("election", "Election Day", firstMonNov.AddDays(1)),
                H("thanksgiving", "Thanksgiving", Last(year, 11, DayOfWeek.Thursday)),
                H("christmaseve", "Christmas Eve", new DateTime(year, 12, 24)),
                H("christmas", "Christmas Day", new DateTime(year, 12, 25)),
                H("newyearseve", "New Year's Eve", new DateTime(year, 12, 31)),
            };
            _years[year] = list;
            return list;
        }

        /// The holiday on this date for this region (null for none): a southern one only in the south, a city one only there too.
        public static Holiday Today(int year, int month, int day, string region)
        {
            if (year < 1800 || month < 1 || month > 12 || day < 1 || day > 31)
                return null;
            foreach (Holiday h in Of(year))
                if (h.Month == month && h.Day == day && (h.Where == null || ((h.Where == "south" || h.Where == "city") && region == "south")))
                    return h;
            return null;
        }

        /// The next holiday on or after this date (within the year, else the next year's first), and the days to it.
        public static Holiday Next(int year, int month, int day, string region, out int daysAway)
        {
            daysAway = -1;
            if (year < 1800 || month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
                return null;
            DateTime d = new DateTime(year, month, day);
            for (int y = year; y <= year + 1; y++)
                foreach (Holiday h in Of(y).Where(x => x.Where == null || region == "south").OrderBy(x => x.Month * 100 + x.Day))
                {
                    DateTime hd = new DateTime(y, h.Month, h.Day);
                    if (hd >= d)
                    {
                        daysAway = (int)(hd - d).TotalDays;
                        return h;
                    }
                }
            return null;
        }

        /// How strongly the day pulls at a thought: a holiday today or beside it, else the season's share, else nothing
        /// (no date known, or the history switched off).
        public static int Pull(int year, int month, int day, string town)
        {
            if (!InnerLife.Historical || year < 1800 || month < 1 || day < 1)
                return 0;
            string region = Region(town);
            if (Today(year, month, day, region) != null)
                return 22;
            try
            {
                DateTime d = new DateTime(year, month, day);
                DateTime t = d.AddDays(1), yd = d.AddDays(-1);
                if (Today(t.Year, t.Month, t.Day, region) != null || Today(yd.Year, yd.Month, yd.Day, region) != null)
                    return 16;
            }
            catch
            {
                return 0;
            }
            return 11;
        }

        /// What the date brings to mind: the holiday today (from six in the morning), the one tomorrow (from noon) or
        /// yesterday's (till noon), else, a third of the time, the season's work in their region; null for none, or
        /// with the history switched off. `source`: "holiday:<key>", "season:<region>".
        public static string Line(Person p, int year, int month, int day, int hour, string seed, List<string> told, out string source)
        {
            source = null;
            if (!InnerLife.Historical || p == null || year < 1800 || month < 1 || month > 12 || day < 1 || day > 31)
                return null;
            string region = Region(p.Home);
            Holiday h = Today(year, month, day, region);
            string[] pool = null;
            if (h != null && hour >= 6)
            {
                pool = Pool(h.Key, "day", year);
                source = "holiday:" + h.Key;
            }
            if (pool == null)
            {
                DateTime d;
                try
                {
                    d = new DateTime(year, month, day);
                }
                catch
                {
                    return null;
                }
                DateTime t = d.AddDays(1), yd = d.AddDays(-1);
                Holiday next = hour >= 12 ? Today(t.Year, t.Month, t.Day, region) : null, prev = hour < 12 ? Today(yd.Year, yd.Month, yd.Day, region) : null;
                if (next != null)
                {
                    pool = Pool(next.Key, "eve", year);
                    source = "holiday:" + next.Key + ":eve";
                }
                else if (prev != null)
                {
                    pool = Pool(prev.Key, "after", year);
                    source = "holiday:" + prev.Key + ":after";
                }
            }
            if (pool == null && Voice.Chance(seed + "|season", 35))
            {
                pool = Season(region, month);
                source = "season:" + region;
            }
            if (pool == null || pool.Length == 0)
            {
                source = null;
                return null;
            }
            return InnerLife.PickLine(pool, p, seed + "|cal", told);
        }

        private static string[] Pool(string key, string when, int year)
        {
            string[] pool;
            if (Pools.TryGetValue(key + "|" + when + "|" + (year >= 1900 ? 1907 : 1899), out pool))
                return pool;
            return Pools.TryGetValue(key + "|" + when, out pool) ? pool : null;
        }

        public static string[] Season(string region, int month)
        {
            string[] pool;
            return month >= 1 && month <= 12 && Seasons.TryGetValue(region + "|" + (month - 1) / 2, out pool) ? pool : null;
        }

        /// The morning's word of the day, once, on the ticker.
        public static string Toast(Holiday h, int year)
        {
            switch (h.Key)
            {
                case "easter": return "Easter Sunday, " + year + ". The churches are full.";
                case "christmas": return "Christmas Day, " + year + ".";
                case "election": return "Election Day. In some towns the saloons shut for the vote.";
                case "fourth": return "The Fourth of July. Speeches, a parade, and guns by dark.";
                case "thanksgiving": return "Thanksgiving Day.";
                case "newyearseve": return year == 1899 ? "The last day of 1899. The century turns at midnight, or next year, depending who you ask." : "New Year's Eve, " + year + ".";
                case "mardigras": return "Mardi Gras. Masks in the streets of Saint Denis.";
                case "decoration": return "Decoration Day. Flowers on the soldiers' graves.";
                default: return h.Name + ".";
            }
        }

        /// The Panic of 1907, as the paper has it from 22 October (roadmap.md P11).
        public static string PanicPaper()
        {
            return "NEW YORK. The Knickerbocker Trust has shut its doors and the crowds are at the banks. Wall Street sits up all night.";
        }

        /// The snake-oil man of 1907 (the Food and Drugs Act of 1906; roadmap.md P11): two beats before "Science, friend".
        public static readonly string[] PitchAct = { "Pure and labelled, friend, as the Food and Drugs Act of nineteen-six demands! Says so right there!", "Then what's in it? The label don't say." };

        // ---- what the day brings to mind ------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "newyear|day", new[] { "New Year's Day. Paid my calls. Three cups of eggnog in and I've called on nobody since.", "First of January. The ledger's turned over. The debts haven't.", "New Year's. Resolved to drink less. Resolved it at the bar." } },
            { "newyear|after", new[] { "Second of January. The year's a day old and looks like the last one.", "New Year's over. Same boots, same mud, new number on the calendar." } },
            { "mardigras|day", new[] { "Mardi Gras. Masks in the street and a trumpet somewhere. Hold onto your purse.", "Fat Tuesday. Everybody's somebody else today. Suits some of them.", "Masks and drums the length of the street. Lent tomorrow. Nobody's thinking of Lent." } },
            { "mardigras|eve", new[] { "Fat Tuesday tomorrow. Found the mask. Found the moths had it first.", "Mardi Gras tomorrow. The city's half drunk already, getting in practice." } },
            { "ashwednesday|day", new[] { "Ash on half the foreheads in town. Didn't go. Feel the smudge anyway.", "Lent. Forty days. The saloon won't notice. I will." } },
            { "washington|day", new[] { "Washington's Birthday. Flags on the bank and the bank shut. The debt isn't.", "Twenty-second of February. Bunting on the store. Cold enough to freeze the flag stiff." } },
            { "stpatrick|day", new[] { "Saint Patrick's. The Irish are marching and the saloons are full by noon.", "Green ribbons on the wharf. Somebody's singing already. Somebody's fighting by dark." } },
            { "goodfriday|day", new[] { "Good Friday. No meat, no music, and the church bell slow.", "Good Friday. Quietest day of the year. Even the saloon talks low." } },
            { "easter|day", new[] { "Easter Sunday. New bonnets, old sins. Church was full for once.", "Easter. He is risen, the preacher says. My back isn't.", "Easter Sunday. Ham on the table, if the pig cooperated." } },
            { "easter|eve", new[] { "Easter tomorrow. Pressed the one good shirt. It's still the one good shirt.", "Easter tomorrow. The whole town's got bonnets on its mind." } },
            { "easter|after", new[] { "Easter Monday. The bonnets went back in the boxes. So did the religion.", "Day after Easter. Ham for breakfast, ham for supper, ham till Thursday." } },
            { "mayday|day", new[] { "First of May. Girls at a maypole and the men talking of the eight-hour day.", "May Day. A picnic on the grass and ants in the pie." } },
            { "decoration|day", new[] { "Decoration Day. Flowers on the soldiers' graves. Old men in blue coats that don't button.", "Thirtieth of May. The veterans marched. Fewer than last year.", "Decoration Day. Flags on the graves. Pa's among them. Didn't go. Should have." } },
            { "fourth|day", new[] { "Fourth of July. Speeches, firecrackers, and a drunk shooting at the sky by noon.", "Independence Day. A flag on every post. Somebody will lose a finger by dark.", "The Fourth. Bunting, a brass band, and the smell of powder all afternoon." } },
            { "fourth|eve", new[] { "Fourth of July tomorrow. The boys have been buying powder. Lord help the dogs.", "The Fourth tomorrow. Bunting going up on the store. Speeches going up in the hotel." } },
            { "fourth|after", new[] { "Fifth of July. Burnt paper in the street. A finger short at the store, I hear.", "Day after the Fourth. The flags are down and the dog's still under the bed." } },
            { "labor|day", new[] { "Labor Day. The unions marching with banners. The bosses watching from the hotel.", "A parade of working men. Clean shirts. Hard faces." } },
            { "halloween|day", new[] { "Halloween. The gate's gone off its hinges again. Boys. Every year.", "All Hallows' Eve. The outhouse was tipped before dark this year. They're getting bold." } },
            { "allsaints|day", new[] { "All Saints'. Whitewashing the tombs. The dead get a cleaner house than I do.", "First of November. Flowers on every tomb in the city. Candles by dark." } },
            { "election|day", new[] { "Election Day. The saloons shut for the vote and the men shut with them.", "Election Day. Party men pressing tickets on anyone who'll take one. Whiskey with it.", "Voting today. A secret ballot, they call it. The whole town knows how I vote." } },
            { "election|eve", new[] { "Election tomorrow. Every fence has a poster and every poster a lie.", "The vote's tomorrow. Both sides have bought me a drink. I'll vote for the whiskey." } },
            { "thanksgiving|day", new[] { "Thanksgiving. A bird on the table, if the bird was slow.", "Thanksgiving Day. Church, then a table, then a sleep. Thankful for the sleep.", "Thanksgiving. Counted my blessings. Short count. Counted again." } },
            { "thanksgiving|eve", new[] { "Thanksgiving tomorrow. The bird's still walking. Not for long.", "Thanksgiving tomorrow. Kin coming. The good plates out. The good temper, too, if I can find it." } },
            { "christmaseve|day", new[] { "Christmas Eve. Carols at the church and a candle in every window.", "Christmas Eve. Wrapped a thing in brown paper. It's the thought, they say." } },
            { "christmas|day", new[] { "Christmas Day. Church, an orange, and a dinner that took three days to make.", "Christmas. Snow would be nice. Mud is what we've got.", "Christmas morning. The children up before the sun. So was the rooster." } },
            { "christmas|after", new[] { "Day after Christmas. The orange is eaten and the quiet is back.", "Boxing Day, the English fellow at the store calls it. We call it Tuesday." } },
            { "newyearseve|day|1899", new[] { "Last night of the century, some say. Others say that's next year. Both of them drunk.", "Nineteen hundred tomorrow. Doesn't feel any different. Yet.", "New Year's Eve. Guns go off at midnight. Keep the dog in." } },
            { "newyearseve|day|1907", new[] { "New Year's Eve. Nineteen eight tomorrow. Where did the years go.", "Last of the year. Guns at midnight, same as ever. Some things don't change." } },
        };

        // ---- the year's work, by region and two months at a time -----------------------------------------------

        private static readonly Dictionary<string, string[]> Seasons = new Dictionary<string, string[]>
        {
            { "ranch|0", new[] { "Feeding stock through the snow. The hay's going faster than the winter.", "Idle hands in town. Cards in the back room and tempers at the bar.", "Ice on the trough every morning. Broke it with the axe. Broke the axe.", "January. Nothing to do but mend harness and wait." } },
            { "ranch|1", new[] { "Calving. Up all night with a heifer that didn't need me.", "Mud to the axle. Spring's here and so's the mud.", "Planting, if the ground thaws. It hasn't.", "A calf dropped in the snow and lived. Tough little thing." } },
            { "ranch|2", new[] { "Roundup. Every hand in the county in the saddle and none of them sober by Saturday.", "Branding fires and bawling calves. The smell of burnt hair for a week.", "Shearing's done. Wool to town, wages to the saloon.", "Spring roundup. Three weeks in the saddle. Can't sit a chair." } },
            { "ranch|3", new[] { "Haying. My back's broke and there's a field left.", "Camp meeting out past the creek. The tents, the singing. The mosquitoes.", "Hay in the barn and a storm coming. Race it.", "August. Grasshoppers in the garden and nothing to be done." } },
            { "ranch|4", new[] { "Fall roundup. Cattle to the railhead and a buyer who can't count.", "County fair. Pies, horses, and a shooting match. Lost at all three.", "Shipping cattle. The cars stink and the money's good.", "First frost. Pumpkins in and the wood half cut." } },
            { "ranch|5", new[] { "Hog killing after the freeze. Sausage for a month. The smell for two.", "Thanksgiving coming and the wood pile short.", "Snow on the ridge. Cabins banked and the stove going day and night.", "Trapping season up the creek. Boots never dry." } },
            { "south|0", new[] { "Grinding's done at last. The mill's quiet and the hands are drunk.", "Mardi Gras coming. Masks in the shop windows.", "January in the bayou. The damp gets in your bones and stays.", "The mill whistle's stopped. First quiet since October." } },
            { "south|1", new[] { "Cotton going in. Rows to the horizon and a mule that won't.", "Corn planted. Now the praying.", "Spring in the bayou. Everything green and everything biting.", "Planting. Bent double from dawn. Straightened at dark." } },
            { "south|2", new[] { "The heat's come. Mosquitoes by the cloud.", "First fever talk of the year. Every cough in church gets a look.", "June in the bayou. Air you could wring out.", "Heat like a wet blanket. The dogs don't move till dark." } },
            { "south|3", new[] { "Yellow fever watch. Ships held in quarantine and the city holding its breath.", "Seed cane going in. Cane knife, cane knife, cane knife.", "August. Nobody moves fast. Nobody moves.", "Fever talk at the market. One cough and the stall empties." } },
            { "south|4", new[] { "Cotton picking. Fingers raw by noon.", "Grinding's begun. The mill runs day and night and so do the men.", "Cane to the mill in wagons. The road's black with it.", "Picking cotton. The sack gets heavier and the sun doesn't move." } },
            { "south|5", new[] { "Grinding at full tilt. Burns on every hand in the parish.", "Cotton sold. Not for enough. Never for enough.", "The mill never stops in December. Nor do the burns.", "Sugar money in town. The saloon knows it." } },
            { "high|0", new[] { "Snowed in. Dug out the door twice today.", "Trappers on their lines. One came down the mountain without his partner.", "The cabin shut tight. Smoke, stew, and the same three faces.", "January in the high country. The cold has a sound." } },
            { "high|1", new[] { "The thaw's begun. The river's up and the logs are coming down.", "Logs on the river. Men on the logs. One in the river.", "Mud and snow, snow and mud. Spring, they call it.", "The pass is open. The first wagon through brought flour and news." } },
            { "high|2", new[] { "High water. The ford's a river and the river's a lake.", "Sheep going up to the summer grass. The herder went with them. Glad to.", "The grass is up on the high meadows. Everything's gone up to it.", "June. Snow on the peaks still, and flowers at my feet." } },
            { "high|3", new[] { "Short summer. Blink and it's gone.", "Prospectors in the hills again. Pans and hope and not much else.", "The one warm month. Make hay of it. I mean that.", "A man came through with a sack of ore. Fool's gold. Fool." } },
            { "high|4", new[] { "First snow on the pass. The road will shut by the month's end.", "Elk season. Every man with a rifle is up the mountain.", "Wood cut and stacked. Not enough. Never enough.", "Snow on the peaks and coming lower every morning." } },
            { "high|5", new[] { "Trapping season. Lines up every creek and ice on every one.", "Cabins shut up for the winter. The town's half empty.", "December. Six hours of light and the stove the whole of them.", "The pass is shut. We're here till spring, whoever we are." } },
        };

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in Pools.Values)
                foreach (string s in pool)
                    yield return s;
            foreach (string[] pool in Seasons.Values)
                foreach (string s in pool)
                    yield return s;
            foreach (Holiday h in Of(1899))
                yield return Toast(h, 1899);
            foreach (Holiday h in Of(1907))
                yield return Toast(h, 1907);
            yield return PanicPaper();
            foreach (string s in PitchAct)
                yield return s;
        }

        public static IEnumerable<string> PoolKeys()
        {
            return Pools.Keys.Concat(Seasons.Keys);
        }
    }
}
