// Step between them (DEPTH item 7; the roadmap's "Settle the feud", whose only half so far was Talk's "a word with your neighbour",
// and Talk is frozen, so this is J). Two men with a quarrel between them (a hostile tie) meet near Arthur and it's about to start:
// J, "Step between them". Each heeds him by his nature and by what he thinks of him (Brawl.Heeds, BrawlVerbs.Standing, as the
// break-it-up shout does): both heed, and it's put off (their quarrel's next turn waits six game hours: SceneTie.PutOffAt); one won't,
// and he shoves past Arthur and the scene goes on as it was (the Director's, and any fight with Arthur the game's). Stepped between a
// second time for the same pair, both heeding, they make it up before him (a "makeup" tie: +2). Why it reads true: a third man
// standing between two gives both a way to back down without losing face (Goffman's face-work); the second time, the backing down is
// itself a habit, and the quarrel looks smaller than the two men standing in it. Plain C#; the game side is
// Game/WestworldScript.StepBetween.cs.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class StepBetween
    {
        public const string Kind = "stepbetween";
        public static bool On = true;   // [Scenes] StepBetween, from the settings
        public const float NearMetres = 10f;
        public const int PromptMs = 6000, PutOffMinutes = 360;

        /// The scenes it can be offered in: a quarrel between the two, before any blow (a standoff's guns are not a thing to step into).
        public static bool Quarrel(string kind)
        {
            return kind == "argue" || kind == "x:feud" || kind == "x:quarrel" || kind == "quarrel" || kind == "snap" || kind == "x:snap" || kind == "brawl" || kind == "drunk";
        }

        /// Whether the tie's quarrel is put off just now (the reasons pass over the pair while it is).
        public static bool PutOff(SceneTie t, long now)
        {
            return t != null && t.PutOffAt > 0 && now - t.PutOffAt >= 0 && now - t.PutOffAt < PutOffMinutes;
        }

        public static List<InteractOption> Rows(string a, string b)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "stepbetween:step", Label = "Step between " + (a ?? "them") + (b != null ? " and " + b : "") + "." },
                new InteractOption { Id = "stepbetween:no", Label = "Leave them to it." },
            };
        }

        private static readonly string[] Yours = { "Easy, the both of you.", "Whatever this is, it'll keep till morning.", "Nobody's getting hurt today. Step back.", "Gentlemen. Not here." };
        private static readonly Dictionary<string, string[]> BacksOff = new Dictionary<string, string[]>
        {
            { "bold", new[] { "Another day, then. Count on it.", "You're lucky he's here.", "This isn't finished." } },
            { "fearful", new[] { "I never wanted it to come to this.", "Fine by me. Fine.", "Fine by me. I've somewhere to be." } },
            { "cold", new[] { "Suit yourself.", "It'll keep.", "Not worth the bother." } },
            { "plain", new[] { "Maybe he's right.", "All right. Some other time.", "I've said my piece." } },
        };
        private static readonly string[] Past = { "Out of my way, mister.", "This ain't your business.", "Move, or you're next.", "Stand aside. This is between us." };
        private static readonly string[] MakesUp = { "Ah, hell. We've been fools about this.", "Second time a stranger's had to stop us. Shame on us both.", "Your hand, then. Let it be done.", "I'll buy the first one, if you'll drink it." };
        private static readonly string[] Takes = { "Done. And I'm sorry for what I said.", "Let it be done. My hand.", "I'll drink it. Thank you.", "Aye. We will say no more of it." };

        public static string YourWord(string seed) { return Pick(Yours, seed + "|you"); }
        public static string ShovesPast(string seed) { return Pick(Past, seed + "|past"); }

        /// Both heeded: each backs off by his nature and goes his way.
        public static Scene PutOffScene(string temperA, string temperB, string seed)
        {
            var s = new Scene { Kind = Kind, Summary = "a stranger stepped between two men about to quarrel, and they let it be" };
            s.Add(0, Pick(GroupPool(temperA), seed + "|a"), "mild", "walkaway");
            s.Add(1, Pick(GroupPool(temperB), seed + "|b"), "mild", "walkaway");
            return s;
        }

        /// Stepped between the second time and both heeding: they make it up then and there.
        public static Scene MadeUpScene(string seed)
        {
            var s = new Scene { Kind = "makeup", Summary = "two men who kept quarrelling made it up when a stranger stepped between them a second time" };
            s.Add(0, Pick(MakesUp, seed + "|mk"), "thanks", "face");
            s.Add(1, Pick(Takes, seed + "|tk"), "thanks", "face");
            s.Add(0, null, null, "walkaway");
            s.Add(1, null, null, "walkaway");
            return s;
        }

        private static string[] GroupPool(string temper)
        {
            string[] pool;
            return BacksOff.TryGetValue(Minds.TemperGroup(temper) ?? "", out pool) ? pool : BacksOff["plain"];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Yours, Past, MakesUp, Takes })
                foreach (string l in a)
                    yield return l;
            foreach (string[] a in BacksOff.Values)
                foreach (string l in a)
                    yield return l;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
