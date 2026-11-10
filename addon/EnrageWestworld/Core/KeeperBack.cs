// The revived dead are seen (THREEHOUR reserve R3). A post holder shot down at the counter is brought back bandaged by the game; RealNPCs
// already knows who saw the fall (Post.SawFall) and, in West World, wakes them a step when the holder is back (Engine.PostBack).
// Nothing showed it. Now, for ten minutes after the holder is found back, one of those who saw the fall, near you and within 20 m of
// the holder, turns and stares at them with a thought over their head. Once each. Behind [Mind] KeepersBackSeen. Why it reads true:
// the mind can't hold a death it saw and a living face together, and the stare is the gap between them.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class KeeperBack
    {
        public const int WatchMs = 600000, LookMs = 2000, StareMs = 6000;
        public const float NearHolderMetres = 20f, NearYouMetres = 35f;

        private static readonly string[] Lines =
        {
            "I saw {n} shot. I saw it. And there {he} stands.",
            "That's not right. That's not right at all. {N} went down.",
            "Bandaged. Bandaged, they say. I watched {him} fall.",
            "{N}'s back at the counter like nothing. Nobody else finds that queer?",
            "I'd swear on the Book I saw {n} die.",
            "Don't look at {him}. Don't let {him} see you looking."
        };

        /// The thought, the name and the pronouns filled in.
        public static string Thought(string firstName, bool woman, string seed)
        {
            string l = Lines[System.Math.Abs(Voice.Hash(seed + "|rev") % Lines.Length)];
            string n = firstName ?? (woman ? "she" : "he");
            return l.Replace("{N}", firstName ?? (woman ? "She" : "He")).Replace("{n}", n).Replace("{he}", woman ? "she" : "he").Replace("{him}", woman ? "her" : "him");
        }

        public static IEnumerable<string> AllLines()
        {
            return Lines;
        }
    }
}
