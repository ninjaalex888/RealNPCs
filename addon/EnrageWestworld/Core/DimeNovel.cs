// Dime novels about you (FIVEDAY reserve R12). When your most-told deed (Shows.MostTold: a legendary one, told in two towns or
// more) has travelled far enough, some New York hack writes it up: a dime novel turns up in the store windows, its title
// built from the deed and wildly exaggerated ("ARTHUR MORGAN, THE TERROR OF VALENTINE; or, Forty Men in the Snow"). Once a
// deed, at most one every sixty days, on a Monday. For thirty days after, the people near you in town think of having read
// it: the bold admire it, the fearful believe every word, the cold scoff at it, the rest are tickled; a few think the real
// man looks smaller. Behind [World] DimeNovels.
//
// Why it reads true: Buffalo Bill and Deadwood Dick were dime-novel heroes long before 1899, and real outlaws read about
// themselves in them. A legend outruns the man: the town reacts to the story, not the person (the halo of a reputation), and
// meeting the original is always a let-down. Plain C#, no game; all words original. mindtest DimeNovelTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class DimeNovel
    {
        public const int GapDays = 60, ReadDays = 30, LeastTowns = 2;
        public const string Prefix = "novel:", LastKey = "novel:last";

        private static readonly Dictionary<string, string[]> Epithets = new Dictionary<string, string[]>
        {
            { "kill", new[] { "THE TERROR OF", "THE DEATH-DEALER OF", "THE SCOURGE OF" } },
            { "murder", new[] { "THE BUTCHER OF", "THE FIEND OF" } },
            { "lawkill", new[] { "THE LAWMAN'S BANE AT", "THE BADGE-BREAKER OF" } },
            { "robbery", new[] { "THE ROAD AGENT OF", "THE GENTLEMAN BANDIT OF" } },
            { "theft", new[] { "THE PHANTOM THIEF OF", "THE SHADOW OF" } },
            { "assault", new[] { "THE IRON FIST OF", "THE BRUISER OF" } },
        };
        private static readonly string[] Subtitles = {
            "Forty Men in the Snow", "The Midnight Ride", "Six Guns and a Silver Heart", "The Vengeance Trail", "The Bloody Saloon",
            "A Tale of the Wild Frontier", "The Outlaw's Oath", "Death at High Noon" };

        /// The novel's title for this deed (the same every time for the same deed).
        public static string Title(string fullName, Deed d)
        {
            string kind = d != null && d.Kind != null && Epithets.ContainsKey(d.Kind) ? d.Kind : "kill";
            string seed = d != null ? d.Id ?? "x" : "x";
            string place = d != null ? (d.Town ?? d.Zone ?? "THE WEST") : "THE WEST";
            return (fullName ?? "THE STRANGER").ToUpperInvariant() + ", " + Voice.Pick(seed + "|ep", Epithets[kind]) + " " + place.ToUpperInvariant() + "; or, " + Voice.Pick(seed + "|sub", Subtitles);
        }

        /// Whether this Monday publishes one: a deed told in enough towns, not written up before, and the gap since the last.
        public static bool Publishes(int weekday, int towns, bool writtenBefore, long day, long lastDay)
        {
            return weekday == 1 && towns >= LeastTowns && !writtenBefore && day - lastDay >= GapDays;
        }

        public static bool Reading(long day, long publishedDay) { return publishedDay > 0 && day - publishedDay >= 0 && day - publishedDay < ReadDays; }

        private static readonly string[] Bold = { "Read the whole of the {n} novel twice. Forty men! I'd settle for four.", "My boy plays at being {n} now. Shoots the hens with a stick." };
        private static readonly string[] Fearful = { "That book says {n} ate a man's heart in Lemoyne. I believe it. I've seen his eyes.", "Hid the {n} novel from the children. Burned it, after." };
        private static readonly string[] Cold = { "Ten cents for lies about {n}. The printer's the only outlaw in it.", "Some fool in New York has never been west of the Hudson. Read his {n} book." };
        private static readonly string[] Plain = { "Bought the {n} novel at the store. Can't put it down. Can't believe a word.", "Saw {n} in the street. Smaller than the woodcut on the cover." };

        /// A reader's thought, by temper group (Minds.TemperGroup: bold, fearful, cold, plain); {n} the surname.
        public static string ReaderThought(string temperGroup, string surname, string seed)
        {
            string[] pool = temperGroup == "bold" ? Bold : temperGroup == "fearful" ? Fearful : temperGroup == "cold" ? Cold : Plain;
            return Voice.Pick(seed + "|dr", pool).Replace("{n}", surname ?? "the stranger");
        }

        public static string Notice(string title, string town) { return "In the window at " + (town ?? "the store") + ", ten cents: " + title + "."; }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Bold, Fearful, Cold, Plain }) foreach (string l in p) yield return l.Replace("{n}", "Morgan");
        }
    }
}
