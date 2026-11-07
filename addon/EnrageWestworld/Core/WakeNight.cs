// The wake (FIVEDAY item 27): the night before a burial, the neighbours sit up with the dead. Interiors are out of reach, so
// it plays outside and in talk. For a death in the town (one of its people, the night of the day they died, or the next
// night when they died after six; or one of enRAGE's funerals due tomorrow), from eight to two:
// - the Around town box tells you once a night that they're sitting up with the dead (lamps lit, food brought, comings
//   and goings at the door);
// - the street's quiet talks are of the dead, half the time;
// - in West World, one of the waking near you swears the dead one moved.
// Behind [World] Wakes. (Named WakeNight so as not to be mistaken for the Awakening's waking.)
//
// Why it reads true: in 1899 the dead were laid out at home and watched through the night before the burial. Neighbours
// came with food and stayed to talk of the dead and their own, and stories of a corpse that sighed or stirred were part
// of the night. Plain C#, no game; all words original. mindtest WakeNightTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class WakeFor
    {
        public string Id, Name, Town;
    }

    public static class WakeNight
    {
        public const int FromHour = 20, ToHour = 2;
        public const int TalkChance = 50;

        public static bool Hour(int hour) { return hour >= FromHour || hour < ToHour; }

        /// The game day whose night the wake is: the day they died, or the next for a death after six in the evening.
        public static long NightOf(long diedAt)
        {
            long day = diedAt / 1440;
            return diedAt % 1440 >= 18 * 60 ? day + 1 : day;
        }

        /// The night now belongs to (after midnight, still the evening's).
        public static long Tonight(long now)
        {
            return now % 1440 < ToHour * 60 ? now / 1440 - 1 : now / 1440;
        }

        /// The town's people being sat up with tonight (its residents dead; the game side adds enRAGE's funerals due tomorrow).
        public static List<WakeFor> Due(Store s, string town, long now)
        {
            var list = new List<WakeFor>();
            if (s == null || town == null)
                return list;
            long night = Tonight(now);
            foreach (Resident r in s.Residents)
                if (r != null && r.Town == town && r.Dead && r.DiedAt > 0 && r.Name != null && NightOf(r.DiedAt) == night)
                    list.Add(new WakeFor { Id = r.Id, Name = r.Name, Town = town });
            return list;
        }

        private static readonly string[][] Talks =
        {
            new[] { "Are you going over to sit with {d}?", "After supper. I've a pie to bring.", "I'll walk with you. Nobody should go alone." },
            new[] { "They've {d} laid out in the front room.", "I saw. They've stopped the clock.", "Ma did that for Pa. I'd forgotten." },
            new[] { "Remember {d} at the harvest dance?", "Lord, yes. Fell into the punch.", "That's how I'll keep {d}. Laughing." },
            new[] { "Who's sitting up with {d} till morning?", "The family, and whoever can keep awake.", "I'll take the hours after midnight." },
            new[] { "{d} owed me two dollars.", "Hush. Not tonight.", "I wasn't asking. Just remembering." },
            new[] { "The lamps are lit over there all night.", "For {d}. They'll not let it go dark.", "I'll bring more oil in the morning." },
        };

        /// Two neighbours, low-voiced, on the one they sit up with: three beats, words only.
        public static Scene Talk(SceneCast c, string deadFirst, string seed)
        {
            string[] t = Talks[(Voice.Hash(seed + "|wake") & 0x7fffffff) % Talks.Length];
            var s = new Scene { Kind = "smalltalk", Summary = "two of the town talked of " + deadFirst + ", the night they sat up with the dead" };
            s.Add(0, t[0].Replace("{d}", deadFirst), "mild", "face").Add(1, t[1].Replace("{d}", deadFirst), "mild").Add(0, t[2].Replace("{d}", deadFirst), "mild");
            return s;
        }

        public static string Box(string town, string dead, string seed)
        {
            return Voice.Pick(seed + "|box", "In " + town + " tonight they sit up with " + dead + ". Lamps in every window of the house, neighbours at the door with covered dishes.",
                              "They're keeping the wake for " + dead + " in " + town + " tonight. The door stands open; folks go in quiet and come out quieter.",
                              dead + " is laid out at home in " + town + " for the night, and the neighbours are taking turns to sit with the body.");
        }

        private static readonly string[] Odd =
        {
            "I swear {d}'s hand moved. I swear it on my mother.", "{d} sighed. Dead men don't sigh. Do they?", "When the lamp guttered, {d}'s eyes were open. They closed them again. I saw.",
            "Nobody else heard {d} breathe. I did.",
        };

        /// West World: one of the waking at the wake swears the dead one moved.
        public static string OddThought(string deadFirst, string seed) { return Voice.Pick(seed + "|odd", Odd).Replace("{d}", deadFirst ?? "him"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] t in Talks) foreach (string l in t) yield return l.Replace("{d}", "Elihu");
            foreach (string l in Odd) yield return l.Replace("{d}", "Elihu");
            for (int i = 0; i < 6; i++) yield return Box("Valentine", "Elihu Starling", "b" + i);
        }
    }
}
