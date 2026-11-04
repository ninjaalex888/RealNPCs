// A line that wakes, and the revived dead seen (SIXDAY item 12c; roadmap 3; roadmap.md P19): two ways the Awakening spreads,
// with West World on, never touching children or story characters. Each person at the fourth stage holds one line out of the
// public domain, seeded from who they are, and says it when they talk strangely with someone they have ties to; a listener
// who already suspects enough (awareness 40 and over) and hears the same line from two different people within three days
// moves up a stage, at most one such waking per town per day. And someone who saw a person die (a tie to the dead, a keeper's
// fall) and later sees them alive, close and in view, reacts once: a look, a step back, a thought, awareness up, and a stage
// at sixty. The town chews on it for a day. Why it reads true: a thing heard twice from two mouths is a sign, and a dead man
// at the rail is the one sight no routine covers. Plain C#, no game; the four lines are quoted from the public domain, the rest
// is original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class WakeLines
    {
        public const int ListenerAwareness = 40, SeenDeadAwareness = 10, SeenDeadStageAt = 60;
        public const int HeardWithinDays = 3;
        public const float SeenDeadMetres = 15f;
        public const string Prefix = "wl|";   // the hearings kept in Told: wl|<line>|<fromId>|<day> (ids carry colons, so the bar divides)

        /// The four lines, with where they come from (all out of copyright for a century and more).
        public static readonly string[] Lines =
        {
            "We are such stuff as dreams are made on.",                       // The Tempest, IV.i
            "All that we see or seem is but a dream within a dream.",         // Poe, 1849
            "I felt a Funeral, in my Brain.",                                  // Dickinson, c. 1861
            "The thing that hath been, it is that which shall be.",           // Ecclesiastes 1:9
        };
        public static readonly string[] Sources = { "a play", "a poem", "a poem", "the Book" };

        public static int IndexOf(string id)
        {
            return Voice.Hash((id ?? "") + "|wakeline") % Lines.Length;
        }

        public static string Of(string id)
        {
            return Lines[IndexOf(id)];
        }

        /// How a stage-four person brings it out, and how the listener takes it.
        public static string Said(int index, string seed)
        {
            string line = Lines[Math.Max(0, Math.Min(Lines.Length - 1, index))], from = Sources[Math.Max(0, Math.Min(Sources.Length - 1, index))];
            return Voice.Pick(seed + "|said", "There's a thing I read once, in " + from + ". \"" + line + "\" I can't put it down.", "\"" + line + "\" That's from " + from + ". It's the only true thing I know.", "Listen. \"" + line + "\" I didn't write it. I only know it's about here.");
        }

        public static string Heard(string seed)
        {
            return Voice.Pick(seed + "|heard", "...Say that again.", "Where did you get that?", "Somebody else said that to me. The same words.", "Don't. I'll be hearing it all night.");
        }

        // ---- the hearings, kept in Told ----

        public static string Key(int index, string fromId, long day)
        {
            return Prefix + index + "|" + (fromId ?? "?") + "|" + day;
        }

        /// A hearing noted (the old ones dropped), and whether the listener has now heard this line from two different people
        /// within three days.
        public static bool Record(List<string> told, int index, string fromId, long day)
        {
            if (told == null)
                return false;
            told.RemoveAll(k => k != null && k.StartsWith(Prefix, StringComparison.Ordinal) && DayOf(k) < day - HeardWithinDays);
            string key = Key(index, fromId, day);
            if (!told.Contains(key))
                told.Add(key);
            var from = new HashSet<string>();
            foreach (string k in told)
            {
                if (k == null || !k.StartsWith(Prefix + index + "|", StringComparison.Ordinal))
                    continue;
                string[] f = k.Split('|');
                if (f.Length >= 4)
                    from.Add(f[2]);
            }
            return from.Count >= 2;
        }

        private static long DayOf(string key)
        {
            string[] f = key.Split('|');
            long d;
            return f.Length >= 4 && long.TryParse(f[3], out d) ? d : long.MaxValue;
        }

        /// Whether a listener can be woken by the line: awake enough, not awake already.
        public static bool CanWake(int awareness, int stage)
        {
            return awareness >= ListenerAwareness && stage < Awakening.Awake;
        }

        // ---- the revived dead ----

        public static string SeenDead(string deadFirst, bool killedByPlayer, string seed)
        {
            string d = deadFirst ?? "him";
            return killedByPlayer
                ? Voice.Pick(seed + "|sd", "That's " + d + ". I watched " + d + " shot. I watched " + d + " not get up.", "Buried " + d + " Tuesday. That's " + d + " at the rail.", d + ". Alive. I saw the hole in " + d + ". I'd swear to it in church.")
                : Voice.Pick(seed + "|sd", "We stood at " + d + "'s grave. There's " + d + ", buying tobacco.", d + " died. I helped carry " + d + ". " + d + " just nodded to me.", "That's " + d + ". " + d + " is dead. One of those two things is a lie and I can see both.");
        }

        /// What the town says for a day after.
        public static string TownTalk(string deadFirst, string seed)
        {
            string d = deadFirst ?? "a dead man";
            return Voice.Pick(seed + "|tt", "They're saying " + d + " is walking the street, and " + d + " buried last week.", "Half the town's seen " + d + " alive today. The other half's looking for the grave.", d + " came back, they say. Nobody's asked " + d + " how.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < Lines.Length; i++)
                for (int s = 0; s < 3; s++)
                    yield return Said(i, "s" + s);
            for (int s = 0; s < 4; s++)
            {
                yield return Heard("h" + s);
                yield return SeenDead("Abner", true, "k" + s);
                yield return SeenDead("Abner", false, "o" + s);
                yield return TownTalk("Abner", "t" + s);
            }
        }
    }
}
