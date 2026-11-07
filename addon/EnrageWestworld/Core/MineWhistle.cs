// Three whistles (FIVEDAY item 28). Annesburg's mine whistle blows three times: an accident below. Rarely (a game day's 4%
// roll, never within three weeks of the last), at an hour of the working day, one or two of the town's men die in the mine,
// off screen. They're named and never shown, and their deaths go through the store like any other (friends hear of it,
// they mourn, the wake is kept). If you're in Annesburg when it happens:
// - the three whistles sound (in words);
// - the people near run toward the pithead (the town's "pithead" spot, when recorded);
// - the wives wait for the names (thoughts), the names come up from the shaft (the box);
// - then grief and anger at the company, and the hat goes round for the widows.
// Away, the word reaches you as news. Behind [World] MineWhistle.
//
// Why it reads true: coal mining around 1899 killed men by the hundreds every year, one or two at a time, from falls of
// roof and bad air. The whistle's three blasts called the town to the pithead, and families waited on the names. The
// company's blame and the miners' collection for the widows followed every time. Plain C#, no game; all words original.
// mindtest MineWhistleTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class MineWhistle
    {
        public const string Town = "Annesburg";
        public const int Chance = 4;          // percent a game day
        public const int GapDays = 21;
        public const int FromHour = 7, Hours = 10;

        /// Whether today has an accident, and at what minute of the day; the gap from the last kept by the caller.
        public static bool Today(long day, long lastDay, out int minute)
        {
            minute = (FromHour + (int)((Voice.Hash("mine|hour|" + day) & 0x7fffffff) % Hours)) * 60 + (int)((Voice.Hash("mine|min|" + day) & 0x7fffffff) % 60);
            return day - lastDay >= GapDays && Voice.Chance("mine|" + day, Chance);
        }

        /// Who's killed: one or two of the town's men, a miner first, a regular next; never more than two.
        public static List<Resident> Victims(IEnumerable<Resident> town, string seed)
        {
            var men = town.Where(r => r != null && !r.Dead && !r.Left && !r.Fled && !r.Woman && r.Held == null && r.Name != null)
                          .OrderByDescending(r => (r.Trade != null && r.Trade.ToLowerInvariant().Contains("miner") ? 2 : 0) + (r.Regular ? 1 : 0))
                          .ThenBy(r => Voice.Hash(seed + r.Id)).ToList();
            int n = Voice.Chance(seed + "|two", 35) ? 2 : 1;
            return men.Take(n).ToList();
        }

        public static string Whistle(string seed)
        {
            return Voice.Pick(seed + "|wh", "The mine whistle. Once, twice... three times. Every head in Annesburg turns toward the hill.",
                              "Three long blasts from the pithead. The street stops dead, then everyone is running.",
                              "The whistle at the mine, three times. A woman drops her basket in the road.");
        }

        private static readonly string[] Waiting = { "Not him. Please, not him. He's on the second shift.", "I counted the whistles. I counted them twice.", "They'll bring the names up. Stand still. Wait.", "He kissed me at the door this morning. Like any morning." };
        private static readonly string[] Anger = { "The company knew that roof was bad. They knew.", "Another two for the company's books. Ink, that's all they are to them.", "They'll pay the widows a month's wages and call it Christian.", "My father, my brother, now this. The mine always gets its due." };

        public static string WaitingThought(string seed) { return Voice.Pick(seed + "|wa", Waiting); }
        public static string AngerThought(string seed) { return Voice.Pick(seed + "|an", Anger); }

        public static string Names(List<string> names, string seed)
        {
            string who = names.Count == 1 ? names[0] : names[0] + " and " + names[1];
            return "They brought them up from the shaft at Annesburg: " + who + ". " + Voice.Pick(seed + "|nm", "A fall of roof in the lower gallery.", "Bad air in the old workings.", "The timbers gave on the third level.");
        }

        public static string Collection(string seed)
        {
            return Voice.Pick(seed + "|co", "At the pithead in Annesburg, a hat goes round for the widows. Nobody gives less than they can't spare.",
                              "The miners in Annesburg pass a hat for the families. The company sends a wreath.");
        }

        public static string Away(List<string> names)
        {
            return "Word from Annesburg: three whistles at the mine. " + (names.Count == 1 ? names[0] + " was killed below." : names[0] + " and " + names[1] + " were killed below.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 6; i++) { yield return Whistle("w" + i); yield return Collection("c" + i); }
            foreach (string l in Waiting) yield return l;
            foreach (string l in Anger) yield return l;
            yield return Names(new List<string> { "Abel Grove", "Seth Marsh" }, "n");
            yield return Away(new List<string> { "Abel Grove" });
        }
    }
}
