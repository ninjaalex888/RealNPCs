// The century turns (DEPTH reserve R6; [World] CenturyTurns, on, with HistoricalAccuracy). On New Year's Eve of 1899 and of 1900,
// from five in the afternoon, any two who stop together may argue the old question: does the twentieth century begin tonight, or
// a year from tonight? (The almanacs and the learned said 1901, since there was no year nought; the crowds and most newspapers
// held their celebrations on 1 January 1900; the argument ran in the papers all through 1899.) In 1900 the one who said "next
// year" gets his due. A talk, not a quarrel: it leaves nothing between them. At midnight the ticker has the turn of the year in
// words. The gunfire the plan named isn't played: firing in a town at midnight is unseen (For Lex). Plain C#; the game side sets
// Place.CenturyEve (null in the harnesses, so nothing changes there) and the midnight word (WestworldScript.Calendar.cs).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class CenturyTurns
    {
        public static bool On = true;   // [World] CenturyTurns, from the settings
        public const string Kind = "century";
        public const int Strength = 45, Percent = 60, FromHour = 17;
        public static int Argued;       // this session, for F7

        /// The eve's year when the clock says it's New Year's Eve of 1899 or 1900 from five in the afternoon (the epilogue's 1907 never), else 0.
        public static int EveOf(int calendarYear, int eraYear, int month, int day, int hour, bool historical)
        {
            if (!On || !historical || eraYear >= 1907 || month != 12 || day != 31 || hour < FromHour)
                return 0;
            return calendarYear == 1899 || calendarYear == 1900 ? calendarYear : 0;
        }

        /// Two who stop together on the eve may argue it: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, int eve)
        {
            if (!On || eve == 0 || x == null || y == null || x.Id == y.Id)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|century", Percent))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + " and " + y.Name + " argue whether the century turns tonight" };
        }

        private static readonly string[][] Eve99 =
        {
            new[] { "Midnight tonight, and it's the twentieth century. Think of that.", "It is not. There was never a year nought. The century turns a year from tonight." },
            new[] { "Last night of the eighteen hundreds. I mean to be drunk for it.", "Then you'll be drunk a year early. Nineteen hundred is the last year of this century, not the first of the next." },
            new[] { "Nineteen hundred tomorrow. A new century, and about time.", "The almanac says nineteen-one. Count it on your fingers: one to a hundred is a hundred years." },
        };
        private static readonly string[] Press99 =
        {
            "Fingers. The papers say tonight, and the whole country's going to ring it in.",
            "You can count all you like. When those nines go to noughts, it's a new century.",
            "Go and argue with the fellow who painted the banner over the street.",
        };
        private static readonly string[] End99 =
        {
            "Have it your way. I'll drink to it tonight and again next year, and be right one of the times.",
            "Then we'll both drink, and argue it again next December.",
            "Suit yourself. A drink's a drink, whatever century it's in.",
        };
        private static readonly string[] Hold99 =
        {
            "I'll see you here a year from tonight, and you'll owe me an apology.",
            "You'll find out you were wrong in about twelve months.",
        };
        private static readonly string[][] Eve00 =
        {
            new[] { "Tonight's the night, then. The real one, this time.", "You said that last year." },
            new[] { "Twentieth century at midnight. Properly, this time.", "I told you a year ago. Nobody listened." },
        };
        private static readonly string[] Owed00 =
        {
            "I said it then and I'll say it now: you were right, and you're insufferable about it.",
            "All right. You were right. I'll stand the drink.",
            "Don't. I've heard it from the preacher already.",
        };

        /// The argument: A says tonight (or, in 1900, owns that it's tonight now), B says otherwise.
        public static Scene Make(SceneCast c, int eve, string seed)
        {
            Argued++;
            var s = new Scene { Kind = Kind, Cast = 2 };
            string bt = c != null && c.Tempers != null ? c.Tempers[1] : null;
            if (eve == 1900)
            {
                string[] o = Eve00[(Voice.Hash(seed + "|o") & 0x7fffffff) % Eve00.Length];
                s.Add(0, o[0], "cheer", "face").Add(1, o[1], "mock").Add(0, Pick(Owed00, seed + "|w"), "mild");
                s.Summary = "two had the century over again on New Year's Eve, and the one who'd said a year ago it turned tonight was owed his due";
                return s;
            }
            string[] p = Eve99[(Voice.Hash(seed + "|p") & 0x7fffffff) % Eve99.Length];
            s.Add(0, p[0], "cheer", "face").Add(1, p[1], "mild").Add(0, Pick(Press99, seed + "|r"), "mock");
            bool stubborn = bt == "Proud" || bt == "Hothead" || bt == "Cold";
            s.Add(1, Pick(stubborn ? Hold99 : End99, seed + "|e"), stubborn ? "mock" : "mild");
            s.Summary = "two argued on New Year's Eve whether the century turned at midnight or a year on";
            return s;
        }

        /// The midnight word on the ticker, the first minutes of the year (1900 or 1901), once each.
        public static string Midnight(int newYear)
        {
            return newYear == 1900 ? "Midnight. Nineteen hundred, or the old century's last year, depending who you ask."
                 : newYear == 1901 ? "Midnight. The twentieth century, by everybody's count this time."
                 : null;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] g in new[] { Eve99, Eve00 }) foreach (string[] a in g) foreach (string l in a) yield return l;
            foreach (string[] a in new[] { Press99, End99, Hold99, Owed00 }) foreach (string l in a) yield return l;
            yield return Midnight(1900);
            yield return Midnight(1901);
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
