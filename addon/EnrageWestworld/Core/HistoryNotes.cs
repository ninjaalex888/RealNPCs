// The history behind the thought (Lex, 2026-11-09: "inner thoughts related to 1899 America. With a historical education influence (this
// can be a toggle within Settings)"; [Ticker] HistoryNotes). The almanac (Core/Almanac.cs) already puts a few hundred real things of 1899 and
// 1907 in people's mouths, each with the paper's own sentence of what happened. When one of those thoughts comes up over someone near you,
// the news box now carries that sentence: what they're thinking of, as the papers had it, with its month and year. Once a story a
// session, one every 90 seconds at most, so it reads as a margin note and never a lecture. The facts are the period's; the wording is
// this add-on's (the almanac's own rule).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class HistoryNotes
    {
        public const int GapMs = 90000;

        private static Dictionary<string, AlmanacEvent> _byLine;
        private static readonly string[] Months = { "", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        /// The almanac story a thought is about, or null when it's not one of the almanac's lines.
        public static AlmanacEvent StoryOf(string thought)
        {
            if (string.IsNullOrEmpty(thought))
                return null;
            if (_byLine == null)
            {
                var d = new Dictionary<string, AlmanacEvent>();
                foreach (AlmanacEvent e in Almanac.All)
                    if (e != null && e.Lines != null && e.Body != null)
                        foreach (string l in e.Lines)
                            if (!string.IsNullOrEmpty(l) && !d.ContainsKey(l))
                                d[l] = e;
                _byLine = d;
            }
            AlmanacEvent ev;
            return _byLine.TryGetValue(thought.Trim(), out ev) ? ev : null;
        }

        /// The note's title: when it was in the papers.
        public static string Title(AlmanacEvent e)
        {
            if (e == null)
                return null;
            return "In the papers, " + (e.Month >= 1 && e.Month <= 12 ? Months[e.Month] + " " : "") + e.Year;
        }

        public static int Stories()
        {
            StoryOf("x");
            return _byLine == null ? 0 : _byLine.Count;
        }
    }
}
