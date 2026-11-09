// Letters home (TENHOUR R7; roadmap.md's idea bank, "Letters between neighbours"; built on those who leave on the train and the ties they leave behind). Someone who left writes, some weeks on, to a friend still in the town, and the friend has it on their mind for a few days.
// Carrying mail to a homestead would need a Talk row, and Talk is frozen. Why it reads true: for most people in 1899, those who went west became a handwriting. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class LettersHome
    {
        public const int FromDays = 14, ToDays = 150, DayPct = 4, OnMindDays = 4;

        public static string Key(string friendId) { return "letterhome:" + friendId; }
        public static string FromKey(string friendId) { return "letterhomefrom:" + friendId; }

        private static readonly string[] Thoughts =
        {
            "Had a letter from {who}. Says the land's good and the neighbours aren't.", "{who} writes. Doing well, it says. {who} always did say that.",
            "A letter from {who}, and three pages about the weather.", "{who} wants to know who's died. I'll have to sit down to write that one.",
            "Nothing from {who} since Easter, then four pages at once.", "{who} says come out. I read it twice, and put it in the drawer.",
        };

        public static string Thought(string who, string seed) { return Voice.Pick(seed + "|lh", Thoughts).Replace("{who}", Person.FirstOf(who) ?? "my cousin"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Thoughts)
                yield return l.Replace("{who}", "Silas");
        }
    }
}
