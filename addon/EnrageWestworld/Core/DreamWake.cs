// The dream carried into the morning (LIVES item 1d, 2026-11-12; [Mind] DreamsRemembered, settings version 464). A hard
// day already comes back a night or two later as a dream in the diary (Core/Dreams.cs; [Features] DiaryDreams). But a
// diary is read only if the player opens it, and a dream doesn't stay on the page: it hangs over the next morning. Now the
// morning after a dream (five to two in the afternoon of the game day it was dreamt, or the day after for one dreamt before
// midnight), the dreamer's own thoughts can go back to it ("Can't shake last night's dream. Kept to the near side of the
// street all morning."), the more if it's the same one again ("Same dream, third night.").
//
// The psychology: dream recall is best on waking and fades within hours unless rehearsed (the "dream amnesia" curve), and a
// dream of a fright colours the mood of the next day (dream affect carries over: Schredl's continuity studies); a dream that
// recurs is the mind still working on something unresolved, and people notice and count it.
//
// State in memory only (Engine.DreamMarks, by id; a reload forgets last night's dream, which is how mornings are): no save
// shape. Off in Core so the goldens stand. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class DreamMark
    {
        public long At;            // the game minute it was dreamt
        public string Emotion;     // Fear, Anger, Shame, Grief
        public bool AboutYou;
        public string Key;         // the memory's key, to count the same dream again
        public int Nights = 1;     // the same dream, how many nights running (or near it)
    }

    public static class DreamWake
    {
        /// [Mind] DreamsRemembered: set by the game from the settings.
        public static bool On;

        public const double Pull = 31;   // a morning's: above an old tie (30), below a fresh memory or the player close again

        /// Whether this mark is on their mind at the game minute `now`: the waking hours (5 to 14) of the morning after.
        public static bool Morning(DreamMark d, long now)
        {
            if (!On || d == null || now < d.At)
                return false;
            long hour = now % 1440 / 60;
            if (hour < 5 || hour >= 14)
                return false;
            long morning = d.At % 1440 / 60 < 5 ? d.At / 1440 : d.At / 1440 + 1;   // dreamt before five: the morning of that day; later: the next day's
            return now / 1440 == morning;
        }

        /// Notes a dream: the same memory dreamt again within three days counts a night more.
        public static DreamMark Note(Dictionary<string, DreamMark> marks, string id, MemoryItem m, string key, long now)
        {
            if (!On || marks == null || id == null || m == null)
                return null;
            DreamMark was;
            int nights = marks.TryGetValue(id, out was) && was.Key == key && now - was.At <= 3 * 1440 ? was.Nights + 1 : 1;
            var d = new DreamMark { At = now, Emotion = m.Emotion, AboutYou = m.AboutYou, Key = key, Nights = nights };
            marks[id] = d;
            return d;
        }

        /// The thought, the stranger's name or "the stranger" for {you}; null with no lines.
        public static string Line(DreamMark d, string you, string seed, List<string> told)
        {
            if (d == null || d.Emotion == null)
                return null;
            string[] pool;
            if (d.Nights >= 2 && Voice.Chance(seed + "|again", 60))
                pool = Again;
            else if (d.AboutYou && Voice.Chance(seed + "|dyou", 60) && OfYou.TryGetValue(d.Emotion, out pool))
            { }
            else if (!Of.TryGetValue(d.Emotion, out pool))
                return null;
            return Fill(InnerLife.PickTold(pool, seed + "|dwake", told) ?? pool[0], you ?? "the stranger", d.Nights);   // the next one not thought lately
        }

        private static readonly string[] Nth = { "", "", "second", "third", "fourth", "fifth" };

        private static string Fill(string l, string who, int nights)
        {
            l = l.Replace("{you}", who).Replace("{nth}", Nth[System.Math.Min(nights, Nth.Length - 1)]);
            return l.StartsWith("the ", System.StringComparison.Ordinal) ? "The " + l.Substring(4) : l;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Again)
                yield return l;
            foreach (var kv in Of)
                foreach (string l in kv.Value)
                    yield return l;
            foreach (var kv in OfYou)
                foreach (string l in kv.Value)
                    yield return l;
        }

        private static readonly string[] Again =
        {
            "Same dream, {nth} night. I know every turn of it now and still can't wake.",
            "That dream again, the {nth} time. Something in me isn't done with it.",
            "Dreamt it the {nth} night running. Might sit up tonight and spite it.",
        };

        private static readonly Dictionary<string, string[]> Of = new Dictionary<string, string[]>
        {
            { "Fear", new[] {
                "Can't shake last night's dream. Kept to the near side of the street all morning.",
                "Woke with my heart going like a horse at the gallop. Only a dream. Only.",
                "Coffee's gone cold in my hand twice. Still back in that dream.",
                "Every noise this morning sounds like the one in the dream." } },
            { "Anger", new[] {
                "Woke up spoiling for a fight. The dream did that.",
                "Had it out with them all night in my sleep. Tired as if I had.",
                "Snapped at the first soul I saw this morning. Wasn't them I was angry at." } },
            { "Shame", new[] {
                "Couldn't meet anybody's eye this morning. A dream, and still.",
                "Dreamed they all knew. Walked to work feeling like they did.",
                "Washed my face three times. The dream won't wash off." } },
            { "Grief", new[] {
                "Woke and for a moment it hadn't happened. Then it had, all over again.",
                "They were in my dream, plain as life. Morning was a cruel thing after.",
                "Set two cups out this morning without thinking. The dream did that.",
                "Heard their voice all night. The quiet this morning is very loud." } },
        };

        private static readonly Dictionary<string, string[]> OfYou = new Dictionary<string, string[]>
        {
            { "Fear", new[] { "Dreamt of {you} again. Saw that hat in every doorway on the way here.", "{you} was in my dream last night, standing at the foot of the bed. I've not been easy since." } },
            { "Anger", new[] { "Dreamt I faced {you} down. Woke, and the anger came along with me.", "{you} again, all night long. I'll not give {you} the morning too." } },
            { "Shame", new[] { "In the dream {you} told everybody. Can't put it out of my head that it could happen.", "Dreamt of {you} looking at me. That look's followed me all morning." } },
            { "Grief", new[] { "Dreamt {you} came to the door, hat in hand, saying sorry. Woke wet-eyed.", "{you} was in the dream, at the graveside. I don't know what to make of that." } },
        };
    }
}
