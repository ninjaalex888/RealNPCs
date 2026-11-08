// The epilogue remembers (SIXDAY item 23; roadmap 25): Arthur's world carries into John's, eight years older. The add-on
// keeps one save per character (arthur.json, john.json); the first time John's is made with Arthur's beside it, Arthur's
// residents come over with eight years on them (stayed, married, widowed, moved on, or died: seeded by who they are and
// their age), the ties between them come too (never Arthur's own), and those who met Arthur (talked with him, saw him
// more than once, or had a heart for him) are marked as remembering a man called Morgan: a thought when they see John.
// What Arthur did to them is never John's doing. Why it reads true: eight years in a town this size change
// everyone a little and nobody completely, and a face from '99 is remembered by the ones it mattered to. Plain C#, no
// game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Epilogue
    {
        public const int Years = 8;
        public const string Morgan = "morgan", MorganSaid = "morgan:said";   // the marks on a state: remembers him; the thought shown
        public static readonly string[] Fates = { "stayed", "married", "widowed", "moved", "died" };

        /// Their eight years, seeded by who they are: most stayed as they were; the young marry, the older are widowed, some
        /// move on (they may come back by train), and the old die.
        public static string Fate(Resident r, string seed, int year)
        {
            int age = r != null && r.BornYear > 0 ? year - r.BornYear : 40;
            int roll = Voice.Hash((seed ?? "?") + "|fate") % 100;
            int die = age >= 70 ? 45 : age >= 55 ? 20 : 6;
            int moved = r != null && r.Personality == "Drifter" ? 30 : 14;
            int married = age < 45 ? 22 : 6;
            int widowed = age >= 35 ? 12 : 2;
            if (roll < die) return "died";
            roll -= die;
            if (roll < moved) return "moved";
            roll -= moved;
            if (roll < married) return "married";
            roll -= married;
            if (roll < widowed) return "widowed";
            return "stayed";
        }

        /// Arthur's residents into John's store, eight years on; the ties between them; the marks of those who met Arthur. A
        /// summary for the log. now: John's clock at the load (the carried people are met now, so the store keeps them as it keeps
        /// anyone met; the moved left eight years before now, so the trains may bring them back from the start, "years after").
        public static string CarryOver(Store arthur, Store john, long now, int year)
        {
            if (arthur == null || john == null)
                return "nothing to carry";
            int stayed = 0, married = 0, widowed = 0, moved = 0, died = 0, remember = 0;
            var ids = new HashSet<string>();
            foreach (Resident a in arthur.Residents)
            {
                if (a == null || a.Id == null || a.Dead || a.Fled || a.Left || a.Name == null)
                    continue;   // the dead and the gone stay so
                string fate = Fate(a, a.Id, year);
                Resident r = new Resident
                {
                    Id = a.Id, Name = a.Name, Origin = a.Origin, Woman = a.Woman, Model = a.Model, Town = a.Town, Trade = a.Trade, Personality = a.Personality, Disposition = a.Disposition,
                    Sociability = a.Sociability, Devotion = a.Devotion, Smarts = a.Smarts, BornYear = a.BornYear, MetAt = now, TouchedAt = now, Regular = a.Regular, Outfit = a.Outfit,
                    Since1899 = fate
                };
                switch (fate)
                {
                    case "died": r.Dead = true; died++; break;
                    case "moved": r.Left = true; r.Fled = true; r.LeftAt = r.FledAt = now - Years * 365L * 1440L; moved++; break;   // (the review of items 20-27: LeftAt 0 read as "a day after leaving")
                    case "married": married++; break;
                    case "widowed": widowed++; break;
                    default: stayed++; break;
                }
                john.Residents.Add(r);
                ids.Add(r.Id);
                PersonState was;
                if (!r.Dead && arthur.People != null && arthur.People.TryGetValue(a.Id, out was) && was != null && Knowing.Met(was))
                {
                    PersonState st = john.Get(Residents.ToPerson(r));
                    st.Mark(Morgan);
                    remember++;
                }
            }
            int ties = 0;
            foreach (SceneTie t in arthur.Ties)
            {
                if (t == null || t.A == null || t.B == null || !ids.Contains(t.A) || !ids.Contains(t.B))
                    continue;   // only ties between residents who came over; never Arthur's own
                john.Ties.Add(t);
                ties++;
            }
            john.CarriedOver = true;
            return (stayed + married + widowed + moved + died) + " of Arthur's people carried over: " + stayed + " as they were, " + married + " married, " + widowed + " widowed, " + moved + " moved on, " +
                   died + " dead; " + ties + " ties between them; " + remember + " remember a man called Morgan";
        }

        /// What one who met Arthur thinks on seeing John, once.
        public static string MorganThought(string seed)
        {
            return Voice.Pick(seed + "|morgan",
                "Morgan. No. He'd be older, and he'd be dead, most likely. But that way of standing.",
                "There was a fellow rode through in '99. Morgan. This one has his eyes and none of his ease.",
                "I knew a man called Morgan once. Paid for things he needn't have. I still look twice at men on bay horses.",
                "Eight years. You'd think I'd stop seeing Morgan in every stranger with a hat like that.",
                "Not Morgan. The scar's wrong and the years are wrong. Close, though. Close enough to turn my head.");
        }

        // ---- the boy who remembers (FIVEDAY R25; [World] BoyRemembers) ----------------------------------------------------
        // One who was a boy in '99 (eight to fifteen then: sixteen to twenty-three in 1907) and met Arthur remembers him as boys
        // remember a man: larger than he was, and kind where he was kind. On seeing John, his own thought (not the grown-ups'),
        // and if John comes within six metres, he says it aloud, once: the question every boy who outgrew a hero asks a stranger
        // who stands the same way. Why it reads true: an admired adult in childhood becomes a template a boy measures himself
        // against for years (identification, the idealised model), and the memory keeps the man's best and loses the rest.
        public const int BoyFrom = 8, BoyTo = 15;

        /// Whether this person was a boy in 1899 (a man, born 1884 to 1891).
        public static bool WasBoy(Resident r) { return r != null && !r.Woman && r.BornYear > 0 && 1899 - r.BornYear >= BoyFrom && 1899 - r.BornYear <= BoyTo; }

        public static string BoyThought(string seed)
        {
            return Voice.Pick(seed + "|boy",
                "Morgan gave me a dollar once, outside the store, and told me to buy something that wasn't candy. I bought candy.",
                "I used to stand the way Morgan stood. Thumbs in the belt. Pa laughed at me for a year.",
                "That man has Morgan's way of looking at a town. I was eleven. I've been looking for it since.",
                "I wanted to ride with Morgan when I grew up. Then I grew up.",
                "Morgan let me hold his reins once. Bay horse. I can still smell the leather.");
        }

        public static string BoyAsks(string seed)
        {
            return Voice.Pick(seed + "|boya",
                "Mister? You wouldn't have known a man called Morgan? Arthur Morgan. He came through here when I was a boy.",
                "Excuse me. You put me in mind of a man I knew as a boy. Morgan. You wouldn't be kin?",
                "Sorry to stare. There was a man called Morgan, years back. He was good to me. You stand like him.");
        }

        /// What the eight years did to them, in a word for the log and the F7 dump.
        public static string FateWord(string fate)
        {
            switch (fate)
            {
                case "married": return "married since";
                case "widowed": return "widowed since";
                case "moved": return "moved on";
                case "died": return "dead these years";
                default: return "as they were";
            }
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 6; i++)
            {
                yield return MorganThought("s" + i);
                yield return BoyThought("b" + i);
                yield return BoyAsks("a" + i);
            }
            foreach (string f in Fates)
                yield return FateWord(f);
        }
    }
}
