// Dreams of real things (the roadmap's idea bank, "Memory that behaves like memory"; [Mind] DiaryDreams). People already write the day's
// hard memory in their diary (Engine.WriteDiary, Voice.Diary). Now a strong one (intensity 60 or more, fear, anger, shame or grief) comes
// back a night or two later as a dream entry, half the time: the same thing, bent the way dreams bend it, the place gone wrong, the
// one who did it without a face, the dead talking. If it was you, the dream has you in it, by name only if they know it. The waking
// dreams of West World are their own (Hosts.Dream); these are anyone's. Why it reads true: a fright isn't over when the day is;
// the mind takes it to bed, and what comes back in the night is the thing itself, out of shape.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Dreams
    {
        public const int LeastIntensity = 60, Chance = 50, SoonestMinutes = 1080, SpreadMinutes = 1800;   // 18 h to 48 h after

        public static bool Dreamt(MemoryItem m)
        {
            return m != null && m.Intensity >= LeastIntensity && (m.Emotion == "Fear" || m.Emotion == "Anger" || m.Emotion == "Shame" || m.Emotion == "Grief");
        }

        /// When it comes back: a night or two on.
        public static long DueAt(long now, string seed)
        {
            return now + SoonestMinutes + System.Math.Abs(Voice.Hash(seed + "|dreamat") % SpreadMinutes);
        }

        private static readonly Dictionary<string, string[]> Of = new Dictionary<string, string[]>
        {
            { "Fear", new[] { "Dreamt of it again. My legs wouldn't move, and the street went on forever.", "In the dream the gun was the size of a church bell. I woke with my hands over my face.", "Dreamt I was back there, only nobody else was. Just the sound." } },
            { "Anger", new[] { "Dreamt I had it out, and this time I won. Woke angrier than I went to sleep.", "In the dream I said every word I should have said. Nobody heard any of it.", "Dreamt of it all night. Ground my teeth till the dawn." } },
            { "Shame", new[] { "Dreamt the whole town was watching. They were. In the dream they all knew.", "In the dream I kept apologising and my mouth made no sound.", "Dreamt of it. Couldn't look at myself in the glass this morning." } },
            { "Grief", new[] { "Dreamt they were at the table like nothing happened. Asked them to pass the bread.", "In the dream they were saying something, and I couldn't make it out. I'd give anything to.", "Dreamt of them last night. Woke and forgot, for a moment. Then remembered." } },
        };

        private static readonly Dictionary<string, string[]> OfYou = new Dictionary<string, string[]>
        {
            { "Fear", new[] { "Dreamt of {you} again. Stood by my bed all night, not a word.", "In the dream {you} had no face, only that hat. I knew who it was all the same." } },
            { "Anger", new[] { "Dreamt of {you}. I had the gun this time. Woke before I used it.", "In the dream {you} laughed. Just laughed, and walked off." } },
            { "Shame", new[] { "Dreamt {you} was telling the whole saloon. Everyone laughing.", "In the dream {you} looked at me the way my father did." } },
            { "Grief", new[] { "Dreamt {you} came to the funeral. Stood at the back. Nobody said a thing.", "In the dream {you} was sorry. I don't know that he is." } },
        };

        /// The dream entry, dated, or null when there's no dream for it.
        public static string Entry(MemoryItem m, PlayerRef you, string date, string seed)
        {
            if (!Dreamt(m))
                return null;
            string[] pool = null;
            if (m.AboutYou && Voice.Chance(seed + "|dyou", 70))
                OfYou.TryGetValue(m.Emotion, out pool);   // when it was you, mostly you in it
            if (pool == null && !Of.TryGetValue(m.Emotion, out pool))
                return null;
            string who = you != null && you.KnownByName ? you.FirstName : "the stranger";
            string line = pool[System.Math.Abs(Voice.Hash(seed + "|dream") % pool.Length)].Replace("{you}", who);
            if (line.StartsWith("the ", System.StringComparison.Ordinal))
                line = "The " + line.Substring(4);
            return date + " - " + line;
        }

        public static IEnumerable<string> AllLines()
        {
            return Of.Values.SelectMany(x => x).Concat(OfYou.Values.SelectMany(x => x));
        }
    }
}
