// Send word with him (DEPTH item 10; the roadmap's "Hire a regular", whose half was FOURTEENHOUR R21's man who watches your horse;
// with [Scenes] HorseWatch). The man down on his luck who comes over to watch your horse will carry a word instead, for the same dime:
// J, "Take a word to <a friend of yours in town>". He goes (out of sight), the friend has it within the hour (the warmth of a talk,
// and their thought of it when next you're near), and he comes back to say it's done and the friend's thanks with it. Why it reads
// true: before the telephone, a boy or a loafer with a message was how a town talked to itself, and a dime for a walk was honest work.
// Plain C#; the game side is Game/WestworldScript.SendWord.cs.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class SendWord
    {
        public const string RowId = "horsewatch:word";
        public const int FromMinutes = 20, ToMinutes = 50, BackMs = 90000;

        public sealed class Friend
        {
            public string Id, Name;
            public int Warmth;
        }

        /// The friend to send word to: a resident of this town the player has a friend's footing with (warmth 2 or more,
        /// Allies.Warmth's rule), by the seed, never notId; null for none.
        public static Friend Pick(IEnumerable<Friend> town, string notId = null, string seed = null)
        {
            if (town == null)
                return null;
            // one of the friends, by the day's seed (not always the warmest: a word carried warms that one, who'd be picked for good;
            // review 1), never the man who'd carry it
            return town.Where(f => f != null && f.Id != null && f.Warmth >= 2 && f.Id != notId).OrderBy(f => Voice.Hash((seed ?? "") + "|" + f.Id)).ThenBy(f => f.Name).FirstOrDefault();
        }

        /// How long it takes him, in game minutes (20 to 50, by the seed).
        public static int Minutes(string seed) { return FromMinutes + (Voice.Hash(seed + "|min") & 0x7fffffff) % (ToMinutes - FromMinutes + 1); }

        public static InteractOption Row(string friend, int cash)
        {
            return new InteractOption { Id = RowId, Label = "\"Take a word to " + friend + " for me instead.\"", Why = cash < HorseWatch.Cents ? "you haven't a dime" : null };
        }

        private static readonly string[] Goes = { "To {f}? I know the place. I'll go now.", "{f}. Right you are. Back directly.", "I'll find {f}. Won't take me long." };
        private static readonly string[] Back = { "Told {f}. {f} said to thank you kindly.", "Your word's with {f}. Glad to have it, too.", "Found {f}, gave the word. Pleased as anything." };
        private static readonly string[] Thinks = { "A word from the stranger, carried by {m}. Good of him to think of me.", "{m} brought me the stranger's word. Kind of him.", "Sent word by {m}, like an old friend would." };

        public static string GoesLine(string friend, string seed) { return Pick(Goes, seed + "|go").Replace("{f}", friend); }
        public static string BackLine(string friend, string seed) { return Pick(Back, seed + "|bk").Replace("{f}", friend); }
        public static string Thought(string messenger, string seed) { return Pick(Thinks, seed + "|th").Replace("{m}", messenger ?? "a man"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Goes.Concat(Back)) yield return l.Replace("{f}", "Martha");
            foreach (string l in Thinks) yield return l.Replace("{m}", "Abe");
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
