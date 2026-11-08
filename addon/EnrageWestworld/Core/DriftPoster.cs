// Posters that drift from the rumours (FIVEDAY reserve R13). The bill outside the sheriff's in a town your deed's rumour has
// reached describes you as that town has heard it: close to the truth where the telling's still faithful (TownRumour's
// Fidelity 80 and up), one thing wrong further out (the coat, the horse, the count), and wild once it's been passed along
// enough (seven foot, a scar, twice the dead, a name that isn't yours). Coming into such a town, once a day, the box has the
// bill as it reads there; and someone near you now and then holds you up against it. Behind [World] DriftingPosters.
//
// Why it reads true: rumour is a game of telephone (Allport and Postman's serial reproduction): each telling levels the
// detail and sharpens the drama, and the description a town has of a man is the story it has heard, not the man. Plain C#,
// no game; all words original. mindtest DriftPosterTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class DriftPoster
    {
        public const int Faithful = 80, Bent = 50;

        private static readonly Dictionary<string, string> Crimes = new Dictionary<string, string>
        {
            { "kill", "for the killing" }, { "murder", "for murder" }, { "lawkill", "for the murder of a lawman" }, { "robbery", "for robbery" },
            { "theft", "for theft" }, { "assault", "for assault" }, { "horsetheft", "for horse stealing" }, { "looting", "for robbing the dead" },
        };
        private static readonly string[] Wrongs = { "a gray coat", "a white horse", "a red beard", "a limp in the left leg" };
        private static readonly string[] Wilds = { "seven foot tall", "a scar from ear to chin", "eyes like a dead man's", "two guns and a knife in each boot" };
        private static readonly string[] FalseNames = { "Black Jack", "the Lemoyne Ghost", "Dutch Morgan", "the Tall Man" };

        /// The bill as it reads in a town: the crime, the place, and the description by how bent the telling is.
        public static string Bill(string kind, int count, string origin, int fidelity, string fullName, string seed)
        {
            string crime = kind != null && Crimes.ContainsKey(kind) ? Crimes[kind] : "for crimes";
            string at = origin ?? "the county";
            if (fidelity >= Faithful)
                return "WANTED " + crime + " at " + at + ": a big man, dark coat, wide hat. Answers to " + (fullName ?? "no name known") + ".";
            if (fidelity >= Bent)
                return "WANTED " + crime + " at " + at + ": a big man in " + Voice.Pick(seed + "|wr", Wrongs) + ". " + (count > 1 ? count + 1 + " dead" : "Armed") + ". Approach with care.";
            int dead = count > 0 ? count * 2 + 1 : 3;
            return "WANTED " + crime + " at " + at + ": " + Voice.Pick(seed + "|wi", Wilds) + ", " + dead + " dead by his hand. Calls himself " + Voice.Pick(seed + "|wn", FalseNames) + ".";
        }

        private static readonly string[] Compare = {
            "Poster says seven foot. He's not seven foot.", "That's the man off the bill. Doesn't look like a man who's killed nine.",
            "The bill's got him on a white horse. That's no white horse.", "Looks just like the poster. God help us, he looks just like it." };

        /// Someone near holding you up against the bill: the further the telling has bent, the less you match it.
        public static string Comparing(int fidelity, string seed)
        {
            return fidelity >= Faithful ? Compare[3] : fidelity >= Bent ? Compare[2] : Voice.Pick(seed + "|cm", new[] { Compare[0], Compare[1] });
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Compare) yield return l;
            foreach (int f in new[] { 90, 60, 30 })
                foreach (string k in Crimes.Keys)
                    yield return Bill(k, 2, "Valentine", f, "Arthur Morgan", k + f);
        }
    }
}
