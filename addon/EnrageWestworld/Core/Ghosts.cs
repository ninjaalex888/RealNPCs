// The reload noticed (SIXDAY item 12b; roadmap 3; roadmap.md P18). When the player loads an older save, Store.Rewind drops what
// happened after the cut; before it does, the dropped things are kept here as ghosts (a killing, a deed, a tie, a link, a
// death) for a week, a hundred at most, never rewound themselves. With West World on, the people who lived through the lost
// hours feel it: a victim of a rewound killing who is alive again gets one sharp deja vu the first time the player comes within
// twelve metres; both sides of a rewound tie think of a quarrel or a kiss that didn't happen; the waking mention the morning
// that happened twice when they talk together; and the ticker says how many times the clock turned back. Why it reads true: a
// world that can be rewound is the uncanniest fact about this one, and only the waking are in a position to notice. Plain C#,
// no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Ghost
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Kind;        // killing, death, deed:<kind>, tie:<kind>, link:<kind>
        [DataMember(EmitDefaultValue = false)] public string Who, WhoName;      // the one it happened to (the victim, the worrier, a tie's first)
        [DataMember(EmitDefaultValue = false)] public string Other, OtherName;  // the other in a tie or a link; the player for a deed
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public long At;            // game minute it happened, in the lost hours
        [DataMember(EmitDefaultValue = false)] public long RewoundAt;     // game minute the clock turned back to
        [DataMember(EmitDefaultValue = false)] public bool ByPlayer;      // a killing of his
        [DataMember(EmitDefaultValue = false)] public bool Felt;          // the deja vu had; a tie's memories given
        [DataMember(EmitDefaultValue = false)] public List<string> SeenBy;   // who has seen the revived man and reacted (Core/WakeLines.cs): once each
    }

    public static class Ghosts
    {
        public const int KeepDays = 7, Most = 100;
        public const float DejaVuMetres = 12f;
        public const int DejaVuAwareness = 10;
        public const int TalkDays = 2;                 // the waking talk of the morning that happened twice this long

        public static Ghost Killing(DeathEntry d, long rewoundAt)
        {
            return d == null ? null : new Ghost { Id = Ids.New(), Kind = "killing", Who = d.Id, WhoName = d.Name, Town = d.Town, At = d.At, RewoundAt = rewoundAt, ByPlayer = d.By == "player", Other = d.By == "player" ? "player" : d.ById };
        }

        public static Ghost Death(Resident r, long rewoundAt)
        {
            return r == null ? null : new Ghost { Id = Ids.New(), Kind = "death", Who = r.Id, WhoName = r.Name, Town = r.Town, At = r.DiedAt, RewoundAt = rewoundAt };
        }

        public static Ghost OfDeed(Deed d, long rewoundAt)
        {
            return d == null ? null : new Ghost { Id = Ids.New(), Kind = "deed:" + d.Kind, Who = "player", WhoName = "you", OtherName = d.Detail, Town = d.Town, At = d.At, RewoundAt = rewoundAt, ByPlayer = true };
        }

        public static Ghost OfTie(SceneTie t, long rewoundAt)
        {
            return t == null ? null : new Ghost { Id = Ids.New(), Kind = "tie:" + t.Kind, Who = t.A, WhoName = t.AName, Other = t.B, OtherName = t.BName, Town = t.Town, At = t.At, RewoundAt = rewoundAt };
        }

        public static Ghost OfLink(WorryLink l, long rewoundAt)
        {
            return l == null ? null : new Ghost { Id = Ids.New(), Kind = "link:" + l.Kind, Who = l.Id, WhoName = l.Name, Other = l.OtherId, OtherName = l.OtherName, Town = l.Town, At = l.At, RewoundAt = rewoundAt };
        }

        /// A week kept, a hundred at most, the oldest dropped first.
        public static int Trim(List<Ghost> ghosts, long now)
        {
            if (ghosts == null)
                return 0;
            int n = ghosts.RemoveAll(g => g == null || now - g.RewoundAt > KeepDays * 1440L);
            if (ghosts.Count > Most)
            {
                ghosts.Sort((a, b) => a.RewoundAt.CompareTo(b.RewoundAt));
                int extra = ghosts.Count - Most;
                ghosts.RemoveRange(0, extra);
                n += extra;
            }
            return n;
        }

        /// Whether the clock turned back lately: the waking talk of it for two days.
        public static bool WentBackLately(List<Ghost> ghosts, long now)
        {
            return ghosts != null && ghosts.Any(g => g != null && now - g.RewoundAt >= 0 && now - g.RewoundAt < TalkDays * 1440L);
        }

        /// The first unfelt killing ghost of this person (alive again): one deja vu, once.
        public static Ghost DejaVuFor(List<Ghost> ghosts, string id)
        {
            if (ghosts == null || id == null)
                return null;
            return ghosts.FirstOrDefault(g => g != null && !g.Felt && (g.Kind == "killing" || g.Kind == "death") && g.Who == id);
        }

        /// The rewound ties not yet thought about.
        public static List<Ghost> TiesUnfelt(List<Ghost> ghosts)
        {
            return ghosts == null ? new List<Ghost>() : ghosts.Where(g => g != null && !g.Felt && g.Kind != null && g.Kind.StartsWith("tie:", StringComparison.Ordinal)).ToList();
        }

        /// For the log: "4 (a killing, 2 deeds, a tie)".
        public static string Summary(List<Ghost> dropped)
        {
            if (dropped == null || dropped.Count == 0)
                return "0";
            var counts = new Dictionary<string, int>();
            foreach (Ghost g in dropped)
            {
                string k = g.Kind == null ? "thing" : g.Kind == "killing" ? "killing" : g.Kind == "death" ? "death" : g.Kind.StartsWith("deed:", StringComparison.Ordinal) ? "deed" : g.Kind.StartsWith("tie:", StringComparison.Ordinal) ? "tie" : "link";
                int c;
                counts[k] = counts.TryGetValue(k, out c) ? c + 1 : 1;
            }
            return dropped.Count + " (" + string.Join(", ", counts.Select(kv => (kv.Value == 1 ? "a " : kv.Value + " ") + kv.Key + (kv.Value == 1 ? "" : "s"))) + ")";
        }

        public static string WentBack(int times)
        {
            return times <= 0 ? "the clock has never turned back" : times == 1 ? "the clock turned back once" : "the clock turned back " + Voice.Number(times) + " times";
        }

        /// The deja vu: a victim alive again, the player within twelve metres.
        public static string DejaVu(bool byPlayer, string seed)
        {
            return byPlayer
                ? Voice.Pick(seed + "|dv", "I've been shot here. I've never been shot.", "That man. I've died in front of that man. I'm standing up.", "There's a hole in me that isn't there. Right here. Under the rib.", "He killed me. I remember the ground. I remember nothing of the kind.")
                : Voice.Pick(seed + "|dv", "I died here. On this spot. Yesterday, or never.", "I was buried. I was. I can feel the lid.", "The last thing I remember is the last thing. Then this. Then this again.");
        }

        /// The waking, together: the morning that happened twice.
        public static readonly string[][] Twice =
        {
            new[] { "Did yesterday happen twice for you? The morning did, for me.", "Twice. The same man at the same door, the same words. I said nothing the second time either." },
            new[] { "I woke and it was the morning I'd already had.", "I know. I poured the same coffee. It was wet the same way." },
            new[] { "Something was taken back. A whole afternoon.", "Not taken. Put back. Like a book on a shelf, and us in it." },
        };

        public static string[] TwiceLines(string seed)
        {
            return Twice[Voice.Hash(seed + "|twice") % Twice.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 4; i++)
            {
                yield return DejaVu(true, "p" + i);
                yield return DejaVu(false, "o" + i);
            }
            foreach (string[] pair in Twice)
                foreach (string s in pair)
                    yield return s;
            yield return WentBack(0); yield return WentBack(1); yield return WentBack(3);
        }
    }
}
