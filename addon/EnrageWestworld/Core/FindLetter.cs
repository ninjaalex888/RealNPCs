// Find the letter (SEAMS item 18; roadmap "Find the letter"; [Scenes] FindTheLetter). The resident waiting on a letter that never
// comes (the "letter" worry: "Every time the stage comes in I find a reason to be by the post office") lives it out to its end, and
// Talk's mend is only a dollar for a telegraph. In 1899 the mail came by rail and a letter misdirected sat at the wrong depot until
// someone asked. Now: at a station agent's window in a town (J), when one you've met elsewhere is waiting on a letter and it's held
// here (seeded: one depot that isn't his own town), the agent hands it to you to carry; near him (J) you give it over, he reads
// it aloud, and his worry ends well. Words, a J menu and a walk across the map you make yourself: no prop, no Talk row.
//
// The psychology: a thing carried by hand from far off is a kindness with a journey in it, and the receiver's relief is the
// strongest reward a stranger can see on another's face (the peak of a worry's end is what's remembered).
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class FindLetter
    {
        public const string Kind = "findletter", GiveKind = "giveletter";
        public static bool On = true;   // [Scenes] FindTheLetter, from the settings
        public const int LookMs = 1000, PromptMs = 8000, AgainMs = 30000, ScanMs = 10000;
        public const float NearMetres = 4f;
        public const string HeldMark = "letterheld", GivenMark = "lettergiven";   // PersonState marks on the one waiting (kept in the save)
        public const string ArcMark = "letterarc:";   // + the worry's arc the letter was taken in: given in another run, it settles nothing (review 4)

        /// The arc a carried letter belongs to, from his marks; -1 for none.
        public static long ArcOf(IList<string> told)
        {
            if (told != null)
                foreach (string t in told)
                {
                    long a;
                    if (t != null && t.StartsWith(ArcMark, System.StringComparison.Ordinal) && long.TryParse(t.Substring(ArcMark.Length), out a))
                        return a;
                }
            return -1;
        }

        /// The towns with a station agent (Core/Posts.cs, kind "station").
        public static readonly string[] Towns = { "Rhodes", "Saint Denis", "Blackwater", "Strawberry", "Armadillo", "Tumbleweed", "Van Horn" };

        /// Where his letter sits, this worry's run (seeded by his id and the arc): one depot that isn't his own town.
        public static string HeldAt(string id, string home, long arc)
        {
            string[] t = Towns.Where(x => x != home).ToArray();
            return t[(Voice.Hash((id ?? "?") + "|letterheld|" + arc) & 0x7fffffff) % t.Length];
        }

        /// Whether his letter worry is one the letter can still end: gone worse, or at its crisis.
        public static bool Waiting(string worry, int stage)
        {
            return On && worry == "letter" && stage >= 1 && stage <= 2;
        }

        public static List<InteractOption> TakeRows(string name, string town)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "findletter:take", Label = "Take the letter for " + (name ?? "them") + (town != null ? " of " + town : "") + ", to carry it there." },
                new InteractOption { Id = "findletter:no", Label = "Leave it be." },
            };
        }

        public static List<InteractOption> GiveRows(string name, string kin)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "giveletter:give", Label = "Give " + (name ?? "them") + " the letter" + (kin != null ? " from " + kin : "") + "." },
                new InteractOption { Id = "giveletter:no", Label = "Not now." },
            };
        }

        /// The agent, handing it over.
        public static string AgentLine(string name, string town, string seed)
        {
            string first = Person.FirstOf(name ?? "someone");
            return Voice.Pick(seed + "|ag", "There's one here for " + first + " of " + (town ?? "elsewhere") + ". Came on the wrong train, weeks back. You know the name?",
                                            "Letter for a " + first + ", " + (town ?? "not from here") + ". Been in the pigeonhole since spring. You'll see it gets there?",
                                            "Mail for " + first + " out of " + (town ?? "another town") + ". Misdirected. I'd be obliged if you'd carry it.");
        }

        /// His words as he reads it; kin: "my sister Ruth", "Mother".
        public static string Reads(string kin, string seed)
        {
            string k = kin ?? "home";
            string K = Grammar.UpperFirst(k);
            return Voice.Pick(seed + "|rd", "That's " + k + "'s hand. I'd know it anywhere. Where did you... never mind. Thank you.",
                                            K + ". Alive and well, and cross I never wrote back. I wrote, Lord, I wrote.",
                                            "From " + k + "! Sat at the wrong depot all this while? I'd near given up on it.");
        }

        /// What he thinks after, over his head.
        public static string After(string seed)
        {
            return Voice.Pick(seed + "|af", "Read it three times. Reading it again tonight.", "A stranger carried it halfway across the territory. For me.", "All that worrying. All of it over a mail train.");
        }

        public static string Notice(string name, string town)
        {
            return "You carry a letter for " + (name ?? "someone") + (town != null ? " of " + town : "") + ". Find them and give it over.";
        }

        public const string Outcome = "brought the letter they waited on";

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 6; i++)
            {
                yield return AgentLine("Amos Teale", i % 2 == 0 ? "Valentine" : null, "a" + i);
                yield return Reads(i % 3 == 0 ? "Mother" : "my sister Ruth", "r" + i);
                yield return After("f" + i);
            }
            yield return Notice("Amos Teale", "Valentine");
        }
    }
}
