// Peak and end (FIVEDAY item 18; NIGHT3's psychologist list). Until now every feeling someone had about the player kept
// its full strength for its time (MindShadow.Feel), so ten small warm moments in a long talk outweighed the shove that
// ended it. Now a meeting with him (feelings about him no more than a quarter hour apart) is remembered by its strongest
// moment and its last: when a new feeling about him comes, the meeting's others, neither the peak nor the end, fade to a
// quarter of their strength, once. A friendly talk ended by a shove is remembered as the shove; a bad meeting put right at
// the end is remembered for both. Behind [Mind] PeakEnd.
//
// Why it reads true: Kahneman's peak-end rule. The memory of an experience is set by its most intense moment and how it
// ended, and its length counts for next to nothing ("duration neglect"). Plain C#, no game. mindtest PeakEndTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class PeakEnd
    {
        public static bool On = true;          // [Mind] PeakEnd, set by the game side (WestworldScript.Tuning.cs)
        public const long GapMinutes = 15;     // feelings about him this close together are one meeting
        public const float Fade = 0.25f;       // what's left of a moment that was neither the peak nor the end

        /// +1 for a warm feeling about him, -1 for a sore one, 0 for one that isn't about him.
        public static int Valence(FeltAt f)
        {
            string k = f.Kind >= 0 && f.Kind < Feeling.Kinds.Length ? Feeling.Kinds[f.Kind] : null;
            switch (k)
            {
                case "gratitude": case "relief": case "joy":
                    return f.AboutId == "player" ? 1 : 0;
                case "anger": case "contempt": case "envy": case "fear":
                    return f.AboutId == "player" ? -1 : 0;
                case "shame":
                    return f.DoerId == "player" ? -1 : 0;
                default:
                    return 0;
            }
        }

        /// After a feeling about him was added (the last of the list): the meeting's moments other than its peak and its end
        /// fade, once. Returns how many faded.
        public static int Apply(List<FeltAt> list, long now)
        {
            if (!On || list == null || list.Count < 3)
                return 0;
            int last = list.Count - 1;
            if (Valence(list[last]) == 0)
                return 0;
            // the meeting: feelings about him walking back from the newest, each within GapMinutes of the one after it
            var idx = new List<int> { last };
            long edge = list[last].At;
            for (int i = last - 1; i >= 0; i--)
            {
                if (Valence(list[i]) == 0)
                    continue;
                if (edge - list[i].At > GapMinutes || list[i].At > list[last].At)
                    break;
                idx.Add(i);
                edge = list[i].At;
            }
            if (idx.Count < 3)
                return 0;
            int peak = last;
            foreach (int i in idx)
                if (list[i].Intensity > list[peak].Intensity)
                    peak = i;
            int faded = 0;
            foreach (int i in idx)
            {
                if (i == peak || i == last || list[i].Faded)
                    continue;
                FeltAt f = list[i];
                f.Intensity *= Fade;
                f.Faded = true;
                list[i] = f;
                faded++;
            }
            return faded;
        }

        /// What the meeting leaves, warm minus sore, for the log and the test: the sum of what's left of its feelings.
        public static float Net(List<FeltAt> list)
        {
            float n = 0f;
            if (list != null)
                foreach (FeltAt f in list)
                    n += Valence(f) * f.Intensity;
            return n;
        }
    }
}
