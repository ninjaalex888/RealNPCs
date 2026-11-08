// Hobo signs by the tracks (FIVEDAY reserve R29). The tramps riding the freights (the almanac's "tramp army", 1899) left
// chalk marks for the next man off the train: which back door gives a meal, which has a dog, where the law is hard. Here
// the marks are true: they are drawn from the tempers of the town's own residents. Once a town a day, near the station
// agent at his window, the Around town box shows the chalk on the water tower: two of the town's living residents, each
// with the mark a tramp would leave for their door. Behind [World] HoboSigns.
//
// Why it reads true: the poor read the respectable more closely than the respectable read themselves; a tramp's chalk is a
// census of character taken at the back door, and it's seldom wrong. The player who knows these people finds the marks
// uncannily fair. Plain C#, no game; all words original. mindtest HoboSignsTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class HoboSigns
    {
        public const float NearMetres = 30f;

        /// The mark a tramp would chalk for this resident's door, by their temper (Personality, Disposition, Devotion).
        public static string Mark(Resident r)
        {
            if (r == null)
                return null;
            string who = r.Woman ? "a kind woman" : "a kind heart";
            if (r.Personality == "Hothead")
                return "teeth (a bad temper)";
            if (r.Personality == "Greedy")
                return "a crossed circle (nothing here)";
            if (r.Personality == "Cold")
                return "a gun (will shoot)";
            if (r.Devotion == "Devout")
                return "a cross (talk religion, get fed)";
            if (r.Personality == "Coward")
                return "an open circle (an easy mark)";
            if (r.Disposition == "Friendly" || r.Personality == "Loyal")
                return "a cat (" + who + ")";
            if (r.Disposition == "Surly")
                return "a crossed circle (nothing here)";   // a surly door with nothing else to say of it
            return "a square (work for a meal)";
        }

        /// The chalk on the water tower: up to two of the town's living residents, each with their mark, or null if none.
        public static string Tower(IEnumerable<Resident> all, string town, string seed)
        {
            if (all == null || town == null)
                return null;
            List<Resident> here = all.Where(r => r != null && r.Town == town && !r.Dead && !r.Left && !r.Fled && r.Name != null).ToList();
            if (here.Count == 0)
                return null;
            here = here.OrderBy(r => Voice.Hash(seed + "|" + r.Id)).Take(2).ToList();
            var parts = here.Select(r => "by " + Person.FirstOf(r.Name) + "'s door, " + Mark(r)).ToList();
            return "Tramps' chalk on the water tower at " + town + ": " + string.Join("; ", parts) + ".";
        }

        public static IEnumerable<string> AllMarks()
        {
            yield return Mark(new Resident { Personality = "Hothead" });
            yield return Mark(new Resident { Personality = "Greedy" });
            yield return Mark(new Resident { Personality = "Cold" });
            yield return Mark(new Resident { Devotion = "Devout" });
            yield return Mark(new Resident { Personality = "Coward" });
            yield return Mark(new Resident { Disposition = "Friendly", Woman = true });
            yield return Mark(new Resident { Disposition = "Friendly" });
            yield return Mark(new Resident());
        }
    }
}
