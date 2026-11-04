// The almanac (the second six-day run, item 1; Lex, 2026-11-04: "I haven't noticed much historical accuracy voicelines,
// inner thoughts or subtitles, if we can add in hundreds of real world events from the time from the wild west but also
// just American people would be talking about in 1899"). Times.cs has the year's forty standing topics, but they reach
// people only through the talks between two of them; the era line sits at the bottom of the thought pile. This is the
// dated side: a few hundred real events of 1899 (and of 1907 for the epilogue), each known from the day it was in the
// papers and talked of while it was news, with a headline and a paper sentence, and what one person thinks or says of
// it in their own words; and beside the dated ones the year's everyday talk (prices, the catalogue, the wheel, ragtime,
// the phonograph, the patent medicines, where the old names of the West got to), known all year.
//
// Where it reaches the player: an "almanac" candidate among the thoughts (Engine.Minds), weighed by the one thing it
// would think of (fresh big news pulls hard, the everyday talk beside the day's own lines); Talk's "What's the news?"
// and the typed news question (Interact, FreeTalk); the talks between two people (MindTalk's "times"); somebody reading
// the paper aloud on the street (Engine.PaperItems); a clipping of the week's paper in the reader and the town paper's
// own item the day something the whole country talks of breaks (Engine.Calendar); a word on the ticker (the game side).
// Behind [World] HistoricalAccuracy and [World] Almanac; AlmanacShare scales how much gets through.
//
// The psychology: news is social currency. People bring up what everyone else is talking about because it is common
// ground with anyone (the mere-exposure and agenda-setting effects: what the paper prints is what the town thinks
// about), and they talk of it most while it is fresh, then let it go. Region decides what feels close (a robbery in
// Wyoming is "ours" to a ranch hand, a hurricane at Hatteras to a Saint Denis dock man), so the pick doubles a thing of
// their own country. A person has only a few things to say about any one story and doesn't repeat them (the told list).
//
// The dates are the events' own, or the day the news could be in a Western paper; a day of 0 is "that month" (known from
// its first); a month of 0 is the whole year. The wording is all original to this add-on; the facts are the period's.
// Every thought line is one whole line of 110 characters at most that names what it is about (the harnesses' rule).
// Checked offline by mindtest (AlmanacTest).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    /// One thing of the year: when it was in the papers, where it mattered most, how big it was, and the words.
    public sealed class AlmanacEvent
    {
        public string Key;
        public int Year;         // 1899 or 1907
        public int Month, Day;   // Day 0: known from the month's first; Month 0: the whole year (the everyday talk)
        public string Where;     // null the nation; "west" the ranch country; "high" the mines and the mountains; "south" the deep south; "world" abroad
        public int Big;          // 1 a small item, 2 talked of, 3 the whole country talking
        public string Kind;      // war, politics, crime, disaster, wonder, sport, money, death, world, west, life, law
        public string Head;      // the paper's headline
        public string Body;      // the paper's sentence (read aloud, on the ticker, the town paper's item)
        public string[] Lines;   // what one person thinks or says of it, in their own words (each 110 characters at most)

        /// The day it was known from, as a date; null for the everyday talk.
        public DateTime? From
        {
            get
            {
                if (Month < 1)
                    return null;
                try { return new DateTime(Year, Month, Math.Max(1, Day)); }
                catch { return null; }
            }
        }

        /// How long it stays news after its day.
        public int KnownDays
        {
            get { return Big >= 3 ? 90 : Big == 2 ? 60 : 30; }
        }
    }

    public static partial class Almanac
    {
        public static bool On = true;      // [World] Almanac (with HistoricalAccuracy)
        public static int Share = 100;     // [World] AlmanacShare: percent, scales the pull of the thoughts and the share of the news answers
        public const int FreshDays = 3, WarmDays = 14;   // fresh: everybody has it; warm: still the talk
        public const int EverydayPull = 12;              // the everyday talk and stale news: a little above the era's old lines (10), under the fillers (14) and the moment's own thoughts (15 to 22)
        public const int EverydayChance = 50;            // percent: how often the everyday talk and stale news come to mind at all (the fortnight's news always does)
        public const string PaperFrom = "the week's paper";   // the reader's byline on the almanac's clippings
        public const string CalendarCovers = "knickerbocker";  // the Panic of 1907 the calendar already prints (Engine.Calendar): no second town item

        private static AlmanacEvent[] _all;
        private static List<AlmanacEvent> _known;
        private static int _knownKey = -2;

        /// Every event, the data files' lists joined once.
        public static AlmanacEvent[] All
        {
            get { return _all ?? (_all = Events1899A.Concat(Events1899B).Concat(Everyday1899).Concat(Events1907).Concat(Everyday1907).ToArray()); }
        }

        private static AlmanacEvent E(string key, int year, int month, int day, string where, int big, string kind, string head, string body, params string[] lines)
        {
            return new AlmanacEvent { Key = key, Year = year, Month = month, Day = day, Where = where, Big = big, Kind = kind, Head = head, Body = body, Lines = lines };
        }

        private static bool Live
        {
            get { return On && InnerLife.Historical && Share > 0; }
        }

        /// The era's year of events: 1907 from 1905 on (the epilogue), else 1899.
        public static int EraOf(int year)
        {
            return year >= 1905 ? 1907 : 1899;
        }

        /// Today as a date in the era's own count, or null when the clock gave no month or day.
        public static DateTime? Today(int year, int month, int day)
        {
            if (month < 1 || day < 1)
                return null;
            try { return new DateTime(year, month, day); }
            catch { return null; }
        }

        /// Days since the event was in the papers: 0 the day itself; negative, not yet; int.MaxValue for the everyday talk.
        public static int Age(AlmanacEvent e, DateTime? today)
        {
            DateTime? from = e.From;
            if (from == null)
                return int.MaxValue;
            if (today == null)
                return -1;
            return (int)(today.Value - from.Value).TotalDays;
        }

        /// What is known today: the dated things of the era's year that have happened and are still news, and the
        /// everyday talk of the year; nothing with the switch off. Without a date, only the everyday talk. The list is
        /// kept for the date (every thought's pick and every talk asks again) and only read, never changed.
        public static List<AlmanacEvent> Known(int year, int month, int day)
        {
            int key = Live ? year * 10000 + month * 100 + day : -1;
            if (_known != null && key == _knownKey)
                return _known;
            var list = new List<AlmanacEvent>();
            _known = list;
            _knownKey = key;
            if (!Live)
                return list;
            int era = EraOf(year);
            DateTime? today = Today(year, month, day);
            foreach (AlmanacEvent e in All)
            {
                if (e.Year != era)
                    continue;
                if (e.Month < 1)
                {
                    list.Add(e);
                    continue;
                }
                int age = Age(e, today);
                if (age >= 0 && age <= e.KnownDays)
                    list.Add(e);
            }
            return list;
        }

        /// The dated things in the papers within `days` of today, the biggest and newest first; none without a date.
        public static List<AlmanacEvent> Fresh(int year, int month, int day, int days)
        {
            DateTime? today = Today(year, month, day);
            if (today == null)
                return new List<AlmanacEvent>();
            return Known(year, month, day).Where(e => e.Month > 0 && Age(e, today) <= days).OrderByDescending(e => e.Big).ThenByDescending(e => e.From).ToList();
        }

        /// The one everybody has today: the biggest of the fresh, else of the warm; null when nothing is new.
        public static AlmanacEvent Freshest(int year, int month, int day)
        {
            List<AlmanacEvent> f = Fresh(year, month, day, FreshDays);
            if (f.Count == 0)
                f = Fresh(year, month, day, WarmDays);
            return f.Count == 0 ? null : f[0];
        }

        /// Something the whole country is talking of, broken within the last few days; null for none.
        public static AlmanacEvent Breaking(int year, int month, int day)
        {
            if (!Live)
                return null;
            foreach (AlmanacEvent e in Fresh(year, month, day, FreshDays))
                if (e.Big >= 3)
                    return e;
            return null;
        }

        /// What one known thing pulls on a thought today: fresh big news pulls like the player close by (30), a fresh small
        /// item or the week's big news like the day's own mood (20 to 25), the fortnight's news a little above the day's
        /// lines, the stale and the everyday beside them (EverydayPull); scaled by the share.
        public static int PullOf(AlmanacEvent e, int year, int month, int day)
        {
            if (e == null || !Live)
                return 0;
            int pull = EverydayPull;
            if (e.Month > 0)
            {
                int age = Age(e, Today(year, month, day));
                if (age >= 0 && age <= FreshDays)
                    pull = 15 + 5 * e.Big;
                else if (age >= 0 && age <= WarmDays)
                    pull = 15 + 2 * e.Big;
            }
            return pull * Share / 100;
        }

        /// Whether the thing comes to mind at all this time: the fortnight's news always; the everyday talk and stale news
        /// EverydayChance of the time, by the seed and the share. A big pool of lines nobody else has used lately would
        /// otherwise win every time the town's shared lines are spent (the variety pass), whatever its pull: this keeps the
        /// year's background a background (ticktest's by-source share is the dial).
        public static bool Comes(AlmanacEvent e, int year, int month, int day, string seed)
        {
            if (e == null || !Live)
                return false;
            if (e.Month > 0)
            {
                int age = Age(e, Today(year, month, day));
                if (age >= 0 && age <= WarmDays)
                    return true;
            }
            return Voice.Chance(seed + "|alcome", Math.Min(100, EverydayChance * Share / 100));
        }

        /// The strongest pull of anything known today (the F7 line and the tests); 0 with nothing known.
        public static int Pull(int year, int month, int day)
        {
            int best = 0;
            foreach (AlmanacEvent e in Known(year, month, day))
            {
                int pull = PullOf(e, year, month, day);
                if (pull > best)
                    best = pull;
            }
            return best;
        }

        /// What the dated news adds to the talks' "times" topic: five a point of size for the freshest thing of the fortnight.
        public static int TalkBonus(int year, int month, int day)
        {
            AlmanacEvent f = Live ? Freshest(year, month, day) : null;
            return f == null ? 0 : 5 * f.Big * Share / 100;
        }

        private static string RegionOf(string where)
        {
            return where == "west" ? "ranch" : where;
        }

        /// The weight of one known thing for someone of this region today: the fresh and the big first, their own
        /// country's doubled, the everyday talk one each so the week's news outweighs it while there is any.
        private static int Weight(AlmanacEvent e, DateTime? today, string region)
        {
            int w = e.Big;
            if (e.Month > 0)
            {
                int age = Age(e, today);
                w *= age <= FreshDays ? 12 : age <= WarmDays ? 6 : 3;
            }
            if (e.Where != null && e.Where != "world" && RegionOf(e.Where) == region)
                w *= 2;
            return w;
        }

        /// One known thing for this person today, weighed by freshness, size and region, by the seed; null for none.
        public static AlmanacEvent PickFor(int year, int month, int day, string region, string seed)
        {
            List<AlmanacEvent> known = Known(year, month, day);
            if (known.Count == 0)
                return null;
            DateTime? today = Today(year, month, day);
            int total = 0;
            foreach (AlmanacEvent e in known)
                total += Weight(e, today, region);
            int roll = Voice.Hash(seed + "alm") % Math.Max(1, total);
            foreach (AlmanacEvent e in known)
            {
                roll -= Weight(e, today, region);
                if (roll < 0)
                    return e;
            }
            return known[known.Count - 1];
        }

        /// The thing this person would think of today: a weighed pick, passing over a thing whose every line they've
        /// thought lately (a person has only so much to say about one story); a few tries, else null.
        public static AlmanacEvent ThoughtEvent(Person p, int year, int month, int day, string seed, List<string> told)
        {
            if (p == null || !Live)
                return null;
            string region = Calendar.Region(p.Home);
            for (int i = 0; i < 6; i++)
            {
                AlmanacEvent e = PickFor(year, month, day, region, seed + "|t" + i);
                if (e == null)
                    return null;
                if (!InnerLife.AllTold(e.Lines, told))
                    return e;
            }
            return null;
        }

        /// One of the thing's lines for this person, not thought lately (marked as thought as it's given).
        public static string ThoughtOf(AlmanacEvent e, Person p, string seed, List<string> told)
        {
            return e == null || p == null ? null : InnerLife.PickLine(e.Lines, p, seed + "|al" + e.Key, told);
        }

        /// What this person thinks of the times today, and `source` names it ("almanac:wilcox"); null for nothing.
        public static string Thought(Person p, int year, int month, int day, string seed, List<string> told, out string source)
        {
            source = null;
            AlmanacEvent e = ThoughtEvent(p, year, month, day, seed, told);
            string line = ThoughtOf(e, p, seed, told);
            if (line != null)
                source = "almanac:" + e.Key;
            return line;
        }

        /// What someone tells a stranger who asks for the news: the one everybody has if something big is fresh, else
        /// one known thing by the seed; null when nothing is known.
        public static string Said(int year, int month, int day, string town, string seed)
        {
            AlmanacEvent e = ForTalk(year, month, day, town, seed);
            return e == null ? null : Voice.Pick(seed + "|as", e.Lines);
        }

        /// The thing someone brings up: a big one still fresh seven times in ten, else a weighed pick for their region.
        public static AlmanacEvent ForTalk(int year, int month, int day, string town, string seed)
        {
            if (!Live)
                return null;
            AlmanacEvent big = Freshest(year, month, day);
            if (big != null && big.Big >= 2 && Voice.Chance(seed + "|big", 70))
                return big;
            return PickFor(year, month, day, Calendar.Region(town), seed);
        }

        /// Whether the news answer takes the almanac this time: the share's chance, and nine in ten when something the
        /// whole country talks of broke in the last few days.
        public static bool Takes(int year, int month, int day, string seed, int basePercent)
        {
            if (!Live)
                return false;
            int chance = basePercent * Share / 100;
            if (Breaking(year, month, day) != null)
                chance = Math.Max(chance, 90 * Share / 100);
            return Voice.Chance(seed + "|takes", Math.Min(100, chance));
        }

        /// The paper's own sentence, as somebody would read it aloud, the ticker shows it, the town paper prints it.
        public static string PaperItem(AlmanacEvent e)
        {
            return e == null ? null : e.Body;
        }

        /// The items for the street's reading aloud: the dated things of the last two weeks, the biggest first.
        public static List<string> PaperItems(int year, int month, int day, int n)
        {
            return Fresh(year, month, day, WarmDays).Take(n).Select(PaperItem).ToList();
        }

        /// The week's paper for the reader: the dated things of the last `days`, biggest first, up to five, each under its
        /// headline, and one of the everyday talk after them as a small item; null when there is nothing dated to print.
        public static string WeekPaper(int year, int month, int day, string town, string seed, int days, out string title)
        {
            title = null;
            if (!Live)
                return null;
            List<AlmanacEvent> week = Fresh(year, month, day, days);
            if (week.Count == 0)
                return null;
            DateTime? today = Today(year, month, day);
            title = "The paper, " + (today != null ? TownLedgers.Months[today.Value.Month - 1] + " " + today.Value.Day : "this week") + (town != null ? ", " + town : "");
            var b = new System.Text.StringBuilder();
            foreach (AlmanacEvent e in week.Take(5))
            {
                if (b.Length > 0)
                    b.Append("\n\n");
                b.Append(e.Head).Append("\n").Append(e.Body);
            }
            List<AlmanacEvent> small = Known(year, month, day).Where(e => e.Month < 1 && e.Head != null).ToList();
            if (small.Count > 0)
            {
                AlmanacEvent e = small[Voice.Hash(seed + "|small") % small.Count];
                b.Append("\n\n").Append(e.Head).Append("\n").Append(e.Body);
            }
            return b.ToString();
        }

        // ---- the other's answer in a talk, by the kind of thing and their manner ---------------------------------

        private static readonly Dictionary<string, string[]> ReplyKind = new Dictionary<string, string[]>
        {
            { "war", new[] { "Boys from here are in it, you know. I think of their mothers every time the paper comes.", "I pray it's over quick. Wars are quick in the papers and slow in the ground.", "My father fought in the last one. He never would speak of it. That tells you enough." } },
            { "politics", new[] { "Whoever wins, somebody'll still want the rent on Monday.", "I read it the same way you do. For once the paper and I agree.", "Big men in big rooms. It's a long way from here, but it reaches us all the same." } },
            { "crime", new[] { "Lord. Lock your door tonight, and the barn too.", "They'll hang for it, or they'll vanish. It's always one or the other.", "That could have been our train. I ride that line twice a year." } },
            { "disaster", new[] { "God help them. Did the paper give the count?", "Makes you grateful for a dull week, doesn't it.", "I'll put something in the plate Sunday. It's all a body can do from here." } },
            { "wonder", new[] { "Well, I never. What'll they think of next?", "I'd like to see that with my own eyes before I'm done.", "Half of it's true and the other half is Hearst. But the half that's true is something." } },
            { "sport", new[] { "I'd have paid money to see it. Two dollars, even.", "My cousin had a dollar on the other fellow. Hasn't spoken since.", "Go on. Tell me the rounds. I want every one." } },
            { "money", new[] { "Prices go one way and wages go the other. Always have.", "That's rich men's weather. It'll rain on us whichever way it blows.", "I'll believe it when it's in my hand and not in a column of figures." } },
            { "death", new[] { "God rest him. There'll not be another like him.", "I read it this morning. Sat down a minute after.", "A big name gone. Makes the year feel like it's turning under you." } },
            { "world", new[] { "Far off, that. Far off and it comes in the paper all the same.", "Other folks' troubles. We've enough of our own, but I'm sorry for them.", "I can't find the place on a map and I'm not sure the paper can either." } },
            { "west", new[] { "This country's changing shape under our feet.", "Heard it from a drover before the paper had it. The road's faster than the wire some weeks.", "That's our own news for once, not something out of New York." } },
            { "life", new[] { "That's the truth. My wife says the same every Saturday.", "Everything's new and nothing's cheaper. That's the whole of it.", "I saw one myself. Didn't know whether to laugh or run." } },
            { "law", new[] { "About time, I'd say. Or about time somebody tried.", "A law's a law till it meets a rich man. Then it's a suggestion.", "They'll pass it, and then we'll see who it's for." } }
        };
        private static readonly string[] ReplyCold = { "I don't read the papers.", "So I heard. And?", "That's a long way from here.", "Mm. The paper says a lot of things." },
                                         ReplySharp = { "Is that what you stopped me for? The paper?", "I can read, {a}. I don't need it read to me.", "The world's troubles are the world's. I've my own.", "You and that paper. Give it a rest." };

        /// The other's answer: warm by the kind of thing, cold or sharp whatever it was.
        public static string Reply(AlmanacEvent e, string manner, string seed)
        {
            if (manner == "cold")
                return Voice.Pick(seed + "|rc", ReplyCold);
            if (manner == "sharp")
                return Voice.Pick(seed + "|rs", ReplySharp);
            string[] pool;
            return Voice.Pick(seed + "|rk", ReplyKind.TryGetValue(e != null && e.Kind != null ? e.Kind : "world", out pool) ? pool : ReplyKind["world"]);
        }

        /// What the talk was of, for the log.
        public static string Words(AlmanacEvent e)
        {
            return e == null ? null : "the news (almanac:" + e.Key + ")";
        }

        /// The talks' answers, for the tests.
        public static IEnumerable<string> ReplyLines()
        {
            return ReplyKind.Values.SelectMany(x => x).Concat(ReplyCold).Concat(ReplySharp);
        }

        /// Every written line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (AlmanacEvent e in All)
            {
                yield return e.Head;
                yield return e.Body;
                foreach (string l in e.Lines)
                    yield return l;
            }
            foreach (string l in ReplyLines())
                yield return l;
        }
    }
}
