// What a brawl is over, when it's 1899's own quarrels (FOURTEENHOUR item 9; [Mind] QuarrelPolitics). The almanac's sides
// (Core/AlmanacSides.cs) give a farmer, a working man, the well-off and a soldier their own thought on the year's arguments;
// here, two men in drink who hold opposite sides of one of them may come to blows over it, and say so: free silver against gold,
// the trusts, the islands, the dry vote. Onlookers who hold a side lean to their man (a little: item 6's pull, smaller), and the
// town retells it by what it was over. Only where the matter would otherwise be a look or a word (nothing, or strangers' honour):
// a debt, a feud, a woman, land or drink between them speak for themselves. Why it reads true: in 1899 an argument over silver or
// the war was a fighting matter in a saloon (the campaign of 1896 split families and the 1900 one was coming), election days were
// a year's worst brawls, and a fight with a cause picks its own sides. The facts named are sure ones; the words are this
// add-on's. Plain C#, no game.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class QuarrelPolitics
    {
        public static bool On = true;   // [Mind] QuarrelPolitics, set by the game side
        public const int Percent = 85;  // of two in drink on opposite sides: tuned for about one saloon brawl in six (mindtest QuarrelPoliticsTest)

        sealed class Topic
        {
            public string Key, Words, Summary;
            public string[] SideA, SideB;   // trade groups on each side ("pious" for the church-minded)
            public int From = 1890, To = 1910;
            public string[][] Openers;
        }

        static readonly Topic[] Topics =
        {
            new Topic { Key = "silver", Words = "free silver against gold", Summary = "over silver", SideA = new[] { "farm", "working", "rough", "trapper" }, SideB = new[] { "gentry", "town", "card" }, To = 1900,
                Openers = new[] {
                    new[] { "Free silver, I said. Gold's a banker's metal.", "Gold's honest money, and you know it.", "Say that to a man with a mortgage.", "angry" },
                    new[] { "Bryan had it right about silver, and every farmer knows it.", "Bryan's a windbag from Nebraska.", "Say that again, you gold bug.", "threat" } } },
            new Topic { Key = "trusts", Words = "the trusts", Summary = "over the trusts", SideA = new[] { "farm", "working", "rough" }, SideB = new[] { "gentry", "town" },
                Openers = new[] {
                    new[] { "The trusts are bleeding this country white.", "The trusts built every mill you ever sweated in.", "Then they can pay a man a living.", "angry" },
                    new[] { "You talk like a trust man.", "I talk like a man who reads the paper.", "Read this, then.", "threat" } } },
            new Topic { Key = "islands", Words = "the Philippines", Summary = "over the Philippines", SideA = new[] { "working", "farm" }, SideB = new[] { "gentry", "town", "soldier" }, To = 1902,
                Openers = new[] {
                    new[] { "We've no business in the Philippines.", "The flag's there now. You'd haul it down?", "I'd bring our boys home.", "angry" },
                    new[] { "Our boys dying in the islands so a sugar trust can grow fat.", "My brother's out there. Mind your mouth.", "I'll say it louder.", "threat" } } },
            new Topic { Key = "dry", Words = "the dry vote", Summary = "over the dry vote", SideA = new[] { "working", "rough", "card" }, SideB = new[] { "pious" },
                Openers = new[] {
                    new[] { "Vote this town dry and I'll drink in the next one.", "Drink's ruined half the families on this street.", "Not mine, it hasn't.", "angry" },
                    new[] { "Temperance man, are you? In a saloon?", "I came to fetch my brother home.", "Then fetch him and get out.", "threat" } } },
        };

        static Topic Of(string key) { return Topics.FirstOrDefault(t => t.Key == key); }

        /// Which side of a quarrel someone is on: 1 or 2, else 0 (no side). "pious" sides with the church-minded.
        public static int Stance(string key, string group, bool pious)
        {
            Topic t = Of(key);
            if (t == null)
                return 0;
            if (t.SideB.Contains("pious") && pious)
                return 2;
            if (group != null && t.SideA.Contains(group))
                return 1;
            if (group != null && t.SideB.Contains(group))
                return 2;
            return 0;
        }

        /// "politics:<key>" when two in drink hold opposite sides of one of the year's quarrels (seeded, Percent of the time),
        /// the first such in the topics' order for the seed; null otherwise.
        public static string OverFor(string groupA, bool piousA, string groupB, bool piousB, int year, string seed)
        {
            if (!On)
                return null;
            int start = (Voice.Hash(seed + "|politics") & 0x7fffffff) % Topics.Length;
            for (int i = 0; i < Topics.Length; i++)
            {
                Topic t = Topics[(start + i) % Topics.Length];
                if (year < t.From || year > t.To)
                    continue;
                int a = Stance(t.Key, groupA, piousA), b = Stance(t.Key, groupB, piousB);
                if (a != 0 && b != 0 && a != b)
                    return Voice.Chance(seed + "|politics|" + t.Key, Percent) ? "politics:" + t.Key : null;
            }
            return null;
        }

        public static string Words(string key) { Topic t = Of(key); return t != null ? t.Words : key; }
        public static string Summary(string key) { Topic t = Of(key); return t != null ? t.Summary : "over politics"; }
        public static string[][] Openers(string key) { Topic t = Of(key); return t != null ? t.Openers : null; }
        public static IEnumerable<string> Keys() { return Topics.Select(t => t.Key); }
    }
}
