// Lonely regulars grow attached (NIGHT3; the research list's "lonely regulars"): someone the mind finds lonely (their
// company need pressing) who knows Arthur (his name, or a kindness from him lately) comes over when they see him in
// town, hoping for a word, and waits a moment facing him. If he stops to talk (opens Talk with them) they're glad of it;
// if he walks off, it stings, and they think so (nothing is held against him: he may not have noticed). The words and
// the choosing here; the walk is the game side's (WestworldScript.Lonely.cs). Plain C#, no game; all words original.
namespace EnrageWestworld.Core
{
    public static class Lonely
    {
        public const float CompanyBar = 0.65f;   // the company need at least this (Needs: 0..1, pressing past 0.5)
        public const int WaitMs = 15000;         // how long they stand hoping for a word once they've spoken
        public const float SoreLeft = 0.1f;      // a feeling about him still this strong keeps them away
        public const long StungRest = 3 * 1440;  // walked past once: three game days before they try again (the review)

        /// Whether something he did still sits with them (the mind's feelings, any age): anger, contempt, fear or shame he
        /// caused, or grief for someone he killed. Nobody comes over glad to the man they're sore at, or mourning by (the
        /// review: the grieving are the loneliest, and a friend of a man he shot came up hoping for a word).
        public static bool Sore(System.Collections.Generic.List<FeltAt> felt, long now)
        {
            if (felt == null)
                return false;
            foreach (FeltAt f in felt)
            {
                string k = f.Kind >= 0 && f.Kind < Feeling.Kinds.Length ? Feeling.Kinds[f.Kind] : null;
                bool his = k == "grief" ? f.DoerId == "player" : (k == "anger" || k == "contempt" || k == "fear" || k == "shame") && Passing.AboutB(f, "player");
                if (his && Mood.Weight(f, now) >= SoreLeft)   // the mood's slower fading: hours for a slight, days for grief
                    return true;
            }
            return false;
        }

        /// Whether they'd come over: lonely enough, knowing him (his name, or a kindness from him lately), and not
        /// afraid of him or against him; nor the cold, who don't seek company out.
        public static bool Comes(float company, bool knowsName, bool kindLately, string regard, string temper)
        {
            if (company < CompanyBar || (!knowsName && !kindLately))
                return false;
            if (regard == "afraid" || regard == "hostile" || temper == "Cold")
                return false;
            return true;
        }

        /// What they say as they reach him. `you`: his surname when they know his name, else null.
        public static string Hello(string temper, bool woman, string you, string seed)
        {
            if (temper == "Coward")
                return "Oh... hello again. Nice, to see you about, is all.";
            if (temper == "Proud")   // never owns to being lonely (the review)
                return woman ? (you != null ? "Mr. " + you + ". " : "Mister. ") + "I happened to be passing. You've a minute, I hope?"
                             : (you != null ? "Mr. " + you + ". " : "Mister. ") + "A word, if you've a minute. Nothing pressing.";
            if (you != null)
                return woman ? Pick(seed, "Mr. " + you + ". I hoped I'd see you. Might you spare a minute?", "Good day, Mr. " + you + ". It's good to see a face I know.")
                             : Pick(seed, "Mr. " + you + "! Hoped I'd run into you. Got a minute for a fellow?", "There he is. How are you keeping, " + you + "?", you + "! Hold up a minute. Haven't had a soul to talk to all day.");
            return woman ? Pick(seed, "Oh, it's you, mister. Might you spare a minute?", "Mister, good day. It's good to see a face I know.")
                         : Pick(seed, "Mister! It's you again. Got a minute?", "Hold up, mister. Haven't had a soul to talk to all day.");
        }

        /// What they think after: glad if he stopped to talk, stung if he walked off.
        public static string After(bool stopped, string seed)
        {
            return stopped ? Pick(seed + "|g", "He stopped. He actually stopped to talk.", "Somebody to talk to. Lord, I needed that.", "Nice, to be spoken to like I matter.")
                           : Pick(seed + "|h", "Didn't stop. Folks never do.", "Too busy for the likes of me. Well.", "Shouldn't have bothered him.");
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed + "|lo") % lines.Length];
        }

        public static System.Collections.Generic.IEnumerable<string> AllLines()
        {
            foreach (string t in new[] { null, "Coward", "Proud", "Hothead" })
                foreach (bool w in new[] { false, true })
                    foreach (string you in new[] { null, "Morgan" })
                        for (int i = 0; i < 6; i++)
                            yield return Hello(t, w, you, "all" + i);
            for (int i = 0; i < 6; i++)
            {
                yield return After(true, "all" + i);
                yield return After(false, "all" + i);
            }
        }
    }
}
