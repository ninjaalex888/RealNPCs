// The model knows their week and their people (LIVES item 3a, 2026-11-12; [Mind] ModelKnowsLife, settings version 474). The
// language model was told a person's feeling, mood, needs, the mind's concerns, two memories, who is in sight by first name
// and their thoughts lately (Core/Language.cs Describe), but not the two things the written thoughts lean on most: the worry
// they're living through this week (Whitlow's eleven dollars, the child with the fever), with where it stands, and who their
// people are (a friend, a sweetheart, the neighbour they're at odds with, the man they fear, the woman who saw the same
// killing). So the model wrote "someone in sight" thoughts about names it knew nothing of, and worries that weren't theirs.
// Now the request carries "Their week:" (InnerLife.WorryWords: what it is by name, its stage, today's turn) and "Their
// people:" (up to three of their ties of the last week, by first name, what's between them, and "in sight" for those near).
//
// Why it matters: a thought is believable when it's continuous with a life the player can learn about (the written mind's
// whole design), and the model writes far better from specifics than from moods ("Owe Whitlow eleven. Saturday." against
// "Money worries."). The names given are added to those the check allows, so a thought may name them. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ModelLife
    {
        /// [Mind] ModelKnowsLife: set by the game from the settings; off here so the language goldens stand.
        public static bool On;

        public const int Most = 3, Days = 7;

        /// What's between this person and the other in the tie, in a few words, or null for nothing worth saying.
        public static string Between(SceneTie t, string id, string temper, long now)
        {
            if (t == null || id == null)
                return null;
            if (SharedFright.Fresh(t, now))
                return "who saw the same killing";
            string view = Opinions.View(t, id, temper, false, false, false);
            if (view == "fear") return "whom they fear";
            if (view == "contempt") return "whom they look down on";
            if (view == "envy") return "whom they envy";
            if (Ties.Romantic(t.Kind) && t.Warmth > 0) return "their sweetheart";
            if (t.Warmth <= -1) return "at odds with them";
            if (view == "respect") return "whom they look up to";
            if (Ties.Friends(t)) return "a friend";
            if (t.Warmth > 0) return "on good terms";
            return null;
        }

        /// Fills r.Week and r.People for this person (nothing with the switch off).
        public static void Fill(LanguageRequest r, Person p, List<SceneTie> ties, ICollection<string> nearFirstNames, long now)
        {
            if (!On || r == null || p == null || p.Id == null)
                return;
            r.Week = InnerLife.WorryWords(p, now / 1440);
            if (ties == null)
                return;
            List<SceneTie> mine = Ties.Of(ties, p.Id, now, Days);
            foreach (SceneTie t in mine)
            {
                if (r.People.Count >= Most)
                    break;
                string other = t.OtherId(p.Id);
                if (other == null || other == "player")
                    continue;
                string first = Person.FirstOf(t.A == p.Id ? t.BName : t.AName);
                string what = Between(t, p.Id, p.Personality, now);
                if (first == null || what == null)
                    continue;
                bool seen = nearFirstNames != null && nearFirstNames.Contains(first);
                r.People.Add(first + ", " + what + (seen ? ", in sight" : ""));
            }
        }
    }
}
