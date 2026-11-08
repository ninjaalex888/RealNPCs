// Relics: the old outlaws posed for photographs (FIVEDAY reserve R26, 1907). By John's time the outlaw is a curiosity: the
// photographers of Saint Denis and Blackwater sell cabinet cards of the dead bandits (laid out on a door after the posse),
// of the "last of the old outlaws" posed with a borrowed rifle, postcards of a hanging; tourists off the train buy them by
// the dozen. In 1907 the photographer at his counter thinks of the trade (a candidate among his thoughts), and once a town a
// day, near his studio, the Around town box shows the window, where one face is one you'd know. Behind [World] Relics.
//
// Why it reads true: the West was being sold as nostalgia while it was still dying (Buffalo Bill's show, the postcards of
// the dead), and a man who outlived his time finds his past for sale at ten cents a card: the strangest grief there is,
// for a world, not a person. Plain C#, no game; all words original. mindtest RelicsTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Relics
    {
        public const int Year = 1907, Pull = 40;
        public static readonly string[] Towns = { "Saint Denis", "Blackwater" };

        public static bool Now(int year) { return year >= Year; }

        private static readonly string[] Thoughts = {
            "Sold forty cards of the dead Callahan boys this week, laid out on a door. Tourists love a door.",
            "Paid old Ezra a dollar to sit with a Winchester and look hard. He never robbed so much as a henhouse.",
            "A man from Boston wants an outlaw for his parlour wall. Any outlaw. I've a drawer full.",
            "The hanging postcards sell best. God forgive the trade, the hanging postcards sell best.",
            "Eight years ago they were shooting each other in the street. Now they're ten cents a card." };
        private static readonly string[] Windows = {
            "In the photographer's window: cabinet cards of the old outlaws, ten cents each. One of them wears a hat you'd know.",
            "The photographer's window: dead bandits on a door, and a gang of '99 in their Sunday best. One face looks like an old friend.",
            "Postcards of the last of the outlaws in the window. The dead look very peaceful. The living in the pictures look worse." };

        public static string Thought(string seed) { return Voice.Pick(seed + "|rt", Thoughts); }
        public static string Window(string seed) { return Voice.Pick(seed + "|rw", Windows); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Thoughts) yield return l;
            foreach (string l in Windows) yield return l;
        }
    }
}
