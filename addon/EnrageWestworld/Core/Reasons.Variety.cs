// The street's reasons take turns (NIGHT4 fix 5; [Scenes] ReasonVariety, on). A quarter of the town is at the worst of a worry
// on any day, and "x:snap" (45-55) beat every other reason (40-44): 56 of 97 reasons played in game, 146 of 240 offline. Three
// changes, all here so the game side and the offline sim agree: a worrier says their worry once a street visit (x:snap,
// x:badnews and x:goodnews rest WorrierRestMinutes for that person, whoever they'd say it to; the pair rest alone let one
// man snap at three people in three minutes), each scene of the same kind among the street's last four takes SameKindPenalty
// off a reason's strength, and x:snap starts at 42 (Reasons.Own), so gossip 40, a lift 43, a drunk 44 and courting win turns.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        public const long WorrierRestMinutes = 180;   // game minutes (about six real minutes): one say of their worry a visit
        public const int SameKindPenalty = 6, StreetMemory = 4;

        /// A kind that is one person's worry said aloud: it rests for that person, whoever is with them.
        public static bool WorrierRests(string kind)
        {
            return kind == "x:snap" || kind == "x:badnews" || kind == "x:goodnews";
        }

        /// What the street's last scenes take off a reason of this kind: SameKindPenalty for each of the same kind among the
        /// last StreetMemory (newest last in `last`).
        public static int StreetPenalty(string kind, IList<string> last)
        {
            if (kind == null || last == null)
                return 0;
            int n = 0;
            for (int i = last.Count - 1, seen = 0; i >= 0 && seen < StreetMemory; i--, seen++)
                if (last[i] == kind)
                    n++;
            return n * SameKindPenalty;
        }

        /// The street's memory of what it played, newest last, kept to StreetMemory.
        public static void NoteStreet(List<string> last, string kind)
        {
            if (last == null || kind == null)
                return;
            last.Add(kind);
            while (last.Count > StreetMemory)
                last.RemoveAt(0);
        }

        /// A person's rest between scenes (real ms) under a trouble weight (NIGHT4 fix 6): a weight over 1 shortens it, by half
        /// at most and never under a minute. The weight scales the odds of blows, not how soon the same person is cast again
        /// (at 5 the two-minute rest had become 24 s, and one man was in seven scenes in three minutes).
        public const int RestFloorMs = 60000;

        public static int RestFor(int ms, float troubleWeight)
        {
            if (troubleWeight <= 1f)
                return ms;
            return System.Math.Max(System.Math.Min(ms, RestFloorMs), (int)(ms / System.Math.Min(troubleWeight, 2f)));
        }

        /// The kind of the snap family (a worry said aloud, which Exchanges dresses as a snap, a brush-off or a word of comfort).
        public static bool SnapFamily(string kind)
        {
            return kind == "x:snap";
        }
    }
}
