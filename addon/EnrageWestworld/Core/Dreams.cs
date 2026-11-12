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
            string[] more;
            if (Wider && pool != null && !ReferenceEquals(pool, OfYouPool(m.Emotion)) && More.TryGetValue(m.Emotion, out more))
                pool = Join(pool, more);   // the wider pools (LIVES reserve R1; [Features] DreamsWider)
            string who = you != null && you.KnownByName ? you.FirstName : "the stranger";
            string line = pool[System.Math.Abs(Voice.Hash(seed + "|dream") % pool.Length)].Replace("{you}", who);
            if (line.StartsWith("the ", System.StringComparison.Ordinal))
                line = "The " + line.Substring(4);
            return date + " - " + line;
        }

        // ---- the wider pools (LIVES reserve R1, 2026-11-12; [Features] DreamsWider, settings version 483) -----------------------
        // Three dreams a feeling made a diary of a hard month read the same; a dream of the same fright comes back changed each
        // time (dreams recombine the day's residue with older fears: the "day residue" of the dream research since Freud, and the
        // continuity studies). Four more a feeling (a diary's entries already written stay as they were written).
        public static bool Wider;   // set by the game; off here so the goldens stand

        private static readonly Dictionary<string, string[]> More = new Dictionary<string, string[]>
        {
            { "Fear", new[] { "Dreamt the house had no doors. I could hear it coming round the outside, looking for one.", "In the dream it happened again, only slower, and I knew every second of it before it came.", "Dreamt I called for help and my own mother crossed the street to be away from me.", "Woke sure someone was standing in the room. Lit the lamp. Nobody. Didn't sleep after." } },
            { "Anger", new[] { "Dreamt I said it to his face in front of the whole town, and they all cheered. Woke ashamed of how good it felt.", "In the dream I was hitting the wall, and the wall was laughing.", "Dreamt I had him by the collar and he turned into my father. Didn't know what to do with my hands.", "Woke with my jaw aching. Must have been grinding at it all night." } },
            { "Shame", new[] { "Dreamt I was at church in my nightshirt and the preacher read my name out.", "In the dream the newspaper had it on the front page, with a drawing. A good likeness.", "Dreamt I went to say I was sorry and the door kept moving down the street.", "Dreamt everyone I'd ever known was at the table and nobody would pass me the salt." } },
            { "Grief", new[] { "Dreamt they were calling me from the yard. Ran out barefoot. Only the wind.", "In the dream I'd only been away a while, and they'd waited, and the kettle was on.", "Dreamt I could smell their pipe. Woke and the room smelled of nothing at all.", "In the dream I knew they were gone, and they knew, and we sat there anyway, not saying." } },
        };

        private static string[] OfYouPool(string emotion)
        {
            string[] p;
            return OfYou.TryGetValue(emotion, out p) ? p : null;
        }

        private static string[] Join(string[] a, string[] b)
        {
            var all = new string[a.Length + b.Length];
            a.CopyTo(all, 0);
            b.CopyTo(all, a.Length);
            return all;
        }

        public static IEnumerable<string> AllLines()
        {
            return Of.Values.SelectMany(x => x).Concat(OfYou.Values.SelectMany(x => x)).Concat(More.Values.SelectMany(x => x));
        }
    }
}
