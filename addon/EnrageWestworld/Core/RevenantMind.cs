// A revenant the model can ask (FIVEDAY2 items 11-13; Lex, 2026-10-31: "once within the model's reach of the player the model takes over their choices, intent most
// likely to kill him"). A returned dead man has no Person, only his record: who killed him, how, where, and what he held against you first. That's all he's
// got, and it's enough. Every few seconds while he's near, what he does next is put to the model as a short numbered list, the stalker's own moves; a slow or
// empty answer, or no model at all, and the rules choose from the same list. Why it reads true: grievance outlives the wound. What comes back is the unfinished
// business, and it only wants one thing. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class RevenantMind
    {
        public const string Follow = "follow", Stare = "stare", Circle = "circle", Rush = "rush", Leave = "leave";
        public const int ThinkEveryMs = 22000, AskWaitMs = 3500, MostAsks = 7;
        public const float ReachMetres = 60f;

        private static readonly Dictionary<string, string> Words = new Dictionary<string, string>
        {
            { Follow, "keeps after him, out of sight" },
            { Stare, "stands where he can be seen, and stares" },
            { Circle, "comes in close and walks round him" },
            { Rush, "comes straight at him, and stops a few steps short" },
            { Leave, "turns and goes, finished with him for now" },
        };

        public static string WordsOf(string id)
        {
            string w;
            return Words.TryGetValue(id, out w) ? w : id;
        }

        /// What he might do now. Far off, keeping after him is all there is to do; close in, all of it. Once he's stayed a while, he may leave.
        public static List<string> Menu(float metres, int asks)
        {
            var m = new List<string> { Follow };
            if (metres < 45f)
                m.Add(Stare);
            if (metres < 30f)
            {
                m.Add(Circle);
                m.Add(Rush);
            }
            if (asks >= 3)
                m.Add(Leave);
            return m;
        }

        /// What the model is told it's doing.
        public static string Brief(int year)
        {
            return "You decide what one of the dead does next. It is " + year + ", in the American West and South. This person was killed, came back, and has found the one who did it. " +
                   "You are given how they died and what they held against the one who killed them, and a short list of what they might do. Choose the one this person would do. Answer with its number alone.";
        }

        /// The request: how he died, and what's left of him.
        public static LanguageRequest Request(Fallen f, string nearestTown, int hour, int year, float metres, List<string> menu)
        {
            string who = f.Name ?? f.Label ?? (f.Woman ? "a woman" : "a man");
            string place = !string.IsNullOrEmpty(f.Town) ? f.Town : !string.IsNullOrEmpty(f.Zone) ? f.Zone : "the road";
            var r = new LanguageRequest
            {
                Kind = "pick", Revenant = true, WithThought = true, Name = who, Woman = f.Woman, Trade = f.Label, Town = nearestTown, Hour = hour, Year = year,
                Feeling = "anger", Strength = "strong", About = "you", YouAs = "the one who killed you", Shows = "shown", Mood = "cold, and in no hurry",
                Now = "you died, and came back, and you have found the one who did it, " + (int)metres + " metres off"
            };
            r.Memories.Add("killed in " + place + (string.IsNullOrEmpty(f.Weapon) ? "" : " with " + f.Weapon) + (f.Headshot ? ", with a bullet through the skull" : "") + (f.Begged ? ", begging at the end" : ""));
            if (f.Held != null)
                r.Memories.Add("before that: " + f.Held);
            r.Options = menu.Select(WordsOf).ToList();
            return r;
        }

        /// The rules' choice from the same list, by how near he is and how many times he's been asked.
        public static string Rules(List<string> menu, float metres, int asks, string seed)
        {
            int roll = Voice.Hash(seed + "|rev") % 100;
            if (menu.Contains(Leave) && roll < 12 + 6 * Math.Max(0, asks - 3))
                return Leave;
            if (menu.Contains(Circle))
                return roll < 34 ? Circle : roll < 58 ? Rush : roll < 80 ? Stare : Follow;
            return menu.Contains(Stare) && roll < 55 ? Stare : Follow;
        }
    }
}
