// The criminal class (SIXDAY item 16; roadmap 28): people who take to crime keep to it. What the add-on already writes
// down (the ties: a pocket picked, a lift in passing, a mugging, with the doer named) is read here as a class: anyone with
// a theft to their name in the last thirty days is of the bent, likelier to go for a pocket again whatever their nature
// or their money (the street's thief reason takes them first), and known for it the whole month (the "thief" over their
// head, the mark's wariness) rather than the week; thirty clean days after the last and they've gone straight, which
// they think about once. The drunk rolled is the same lift on a man in his cups, with its own words (Scenes.Rolled).
// Why it reads true: the last offence is the best predictor of the next, and a small town's memory of a thief ran a
// season, not a week. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Criminals
    {
        public const int BentDays = 30, ReformDays = 30;
        public const int KeepsChance = 70;              // the bent's chance of going for a pocket on a day (money trouble 30, light fingers 25)
        public const string KeepsToIt = "keeps to it";  // Reasons.Thief's word for it
        public static readonly string[] Kinds = { "pickpocket", "lift", "mugging", "holdup" };

        public static bool IsCrime(string tieKind)
        {
            return tieKind != null && Array.IndexOf(Kinds, tieKind) >= 0;
        }

        /// Their thefts within the month, and the last one's time (0 for none).
        public static int Crimes(List<SceneTie> ties, string id, long now, out long lastAt)
        {
            lastAt = 0;
            int n = 0;
            if (ties == null || id == null)
                return 0;
            for (int i = 0; i < ties.Count; i++)
            {
                SceneTie t = ties[i];
                if (t == null || t.Crime == null || t.CrimeDoer != id || t.CrimeAt > now || now - t.CrimeAt >= BentDays * 1440L)
                    continue;   // the theft kept on the tie (Crime, CrimeAt, CrimeDoer), not its latest Kind: small talk since doesn't wipe it (the review of items 15-19)
                n++;
                if (t.CrimeAt > lastAt)
                    lastAt = t.CrimeAt;
            }
            return n;
        }

        /// Of the bent: a theft to their name this month.
        public static bool Bent(List<SceneTie> ties, string id, long now)
        {
            long last;
            return Crimes(ties, id, now, out last) > 0;
        }

        /// Known as a thief: a theft this month the town saw: caught at a pocket, or a mugging (the one robbed saw his face). A lift
        /// nobody saw (Scenes.Lift: the thief walked off before anyone noticed) makes him bent, never known (the review of items 15-19).
        public static bool Known(List<SceneTie> ties, string id, long now)
        {
            if (ties == null || id == null)
                return false;
            for (int i = 0; i < ties.Count; i++)
            {
                SceneTie t = ties[i];
                if (t != null && t.Crime != null && t.Crime != "lift" && t.CrimeDoer == id && t.CrimeAt <= now && now - t.CrimeAt < BentDays * 1440L)
                    return true;
            }
            return false;
        }

        /// Gone straight: a theft to their name once, none in ReformDays; lastAt the last one (the once-only mark's key).
        public static bool Reformed(List<SceneTie> ties, string id, long now, out long lastAt)
        {
            lastAt = 0;
            if (ties == null || id == null)
                return false;
            for (int i = 0; i < ties.Count; i++)
            {
                SceneTie t = ties[i];
                if (t != null && t.Crime != null && t.CrimeDoer == id && t.CrimeAt <= now && t.CrimeAt > lastAt)
                    lastAt = t.CrimeAt;
            }
            return lastAt > 0 && now - lastAt >= ReformDays * 1440L;
        }

        private static readonly string[] Reform =
        {
            "A month since my hand was in a coat. Feels like a year. Feels like something.",
            "Honest work since the spring. The pay's worse and I sleep like a child.",
            "Walked past a fat pocketbook today and kept walking. Didn't know I had it in me.",
            "They still call me thief behind their hands. Let them. My hands are clean a month.",
            "Thirty days straight. The trick is to stop counting, they say. I'm counting.",
            "No more pockets. I said it to the mirror and the mirror didn't laugh, so maybe.",
        };

        /// What a thief gone straight thinks, once.
        public static string ReformThought(string seed)
        {
            return Voice.Pick(seed + "|rf", Reform);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Reform)
                yield return l;
        }
    }
}
