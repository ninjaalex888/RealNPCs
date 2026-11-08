// The editor takes a side (FIVEDAY reserve R14). Every town's paper has its editor and his line: law and order (the outlaw a
// menace, the sheriff too slow), the people's (the banks and the railroad the real thieves, the outlaw half a hero), or
// business (whatever's bad for trade). When one of your deeds is printed in a town's paper (Deeds.Paper), the editor adds his
// word to it, in his slant; and for two days after, someone near you in that town who's read it agrees or doesn't, by their
// temper (the bold and the cold argue with a paper; the fearful and the plain believe it). Behind [World] EditorSides.
//
// Why it reads true: 1899's town papers were partisan by trade; an editor's framing decided whether a killing was a crime or
// a rough justice (framing effects), and readers who already leaned his way read it as fact while the rest read it as the
// editor (hostile media effect). Plain C#, no game; all words original. mindtest EditorTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Editor
    {
        public const int ReadDays = 2;
        public const string Prefix = "editorial:";

        private static readonly Dictionary<string, string> Stances = new Dictionary<string, string>
        {
            { "Saint Denis", "law" }, { "Rhodes", "law" }, { "Valentine", "people" }, { "Strawberry", "people" }, { "Annesburg", "people" },
            { "Blackwater", "business" }, { "Armadillo", "business" }, { "Van Horn", "people" }, { "Tumbleweed", "law" }, { "Emerald Ranch", "business" }
        };

        /// The town's paper's line: law, people, or business.
        public static string Stance(string town)
        {
            string s;
            return town != null && Stances.TryGetValue(town, out s) ? s : Voice.Pick("stance|" + town, "law", "people", "business");
        }

        private static readonly Dictionary<string, string[]> Words = new Dictionary<string, string[]>
        {
            { "law", new[] { "THE EDITOR: How long will decent people endure this? The sheriff must act, or be replaced.", "THE EDITOR: There is no romance in a bullet. Hang the man, and quickly." } },
            { "people", new[] { "THE EDITOR: We shed no tears for those who grow fat on the poor. Ask who the real thieves are.", "THE EDITOR: The banks call him an outlaw. The farmers call him something else." } },
            { "business", new[] { "THE EDITOR: Every such outrage costs this town a merchant and a railroad depot.", "THE EDITOR: Capital will not settle where lead flies. Order, gentlemen, is good business." } },
        };
        private static readonly string[] Agree = { "The paper's right for once. Somebody ought to say it.", "Read the editor this morning. Said what I've been thinking." };
        private static readonly string[] Disagree = { "That editor's never been west of his own press. Wind and ink.", "The paper says what the man who buys the ink wants said." };

        public static string Editorial(string town, string seed) { return Voice.Pick(seed + "|ed", Words[Stance(town)]); }

        /// Someone who read it: the bold and the cold argue with a paper, the fearful and the plain take it as read.
        public static string Reader(string temperGroup, string seed)
        {
            bool argues = temperGroup == "bold" || temperGroup == "cold";
            return Voice.Pick(seed + "|er", argues ? Disagree : Agree);
        }

        public static bool Reading(long day, long printedDay) { return printedDay > 0 && day - printedDay >= 0 && day - printedDay < ReadDays; }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in Words.Values) foreach (string l in p) yield return l;
            foreach (string l in Agree) yield return l;
            foreach (string l in Disagree) yield return l;
        }
    }
}
