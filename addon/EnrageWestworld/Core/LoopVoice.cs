// The voice, and breaking the loop (SIXDAY item 12a; roadmap 3; roadmap.md P17), for the waking residents of a town. A regular's
// habits (Core/Routines.cs) are a script; from the second stage of waking they hear it as a voice giving orders in the second
// person at the hour ("Seven o'clock. The saloon. You'll be at the saloon."), once a day; from the third they answer it back
// ("Not tonight."); at the fourth, now and then, they stand where the script forbids (a broom on an empty street at three in
// the morning, the church wall at noon, the river's edge), and, once, three thoughts end with them knowing the voice as their
// own: the centre. After that an ending may be a reset: the loop closes over them and they're cheerful and blank, and their
// friends notice for two days. Why it reads true: the uncanny thing about a routine is that it needs no one to order it, and
// the first free act is a small wrong one. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class LoopVoice
    {
        public const int HearsFrom = Awakening.Questioning, AnswersFrom = Awakening.Unravelling, BreaksFrom = Awakening.Awake;
        public const int CentreCount = 3;
        public const int ResetChance = 30;        // percent, at a stage-4 ending once the centre is passed
        public const float HearMetres = 30f;      // the player this near: the voice shows on the ticker
        public const int AnswerDelayMs = 3200;
        public static readonly string[] BreakKinds = { "broom", "wall", "water" };

        public static bool Hears(int stage) { return stage >= HearsFrom; }
        public static bool Answers(int stage) { return stage >= AnswersFrom; }
        public static bool Breaks(int stage) { return stage >= BreaksFrom; }

        public static string HourWord(int hour)
        {
            switch (hour)
            {
                case 0: return "Midnight.";
                case 12: return "Noon.";
                default:
                    int h = hour % 12 == 0 ? 12 : hour % 12;
                    return Voice.Number(h).Substring(0, 1).ToUpperInvariant() + Voice.Number(h).Substring(1) + " o'clock.";
            }
        }

        private static readonly Dictionary<string, string[]> Orders = new Dictionary<string, string[]>
        {
            { "saloon", new[] { "{hour} The saloon. You'll be at the saloon.", "{hour} The bar, the same stool. Go on.", "{hour} You drink now. You always drink now." } },
            { "church", new[] { "{hour} The church. Your pew is the third from the back.", "{hour} Hat off. In you go.", "{hour} The church. You'll kneel and you'll not know why." } },
            { "store", new[] { "{hour} The store. Flour and nails, the same list.", "{hour} The counter. Stand at it. Wait your turn.", "{hour} You're buying something. You'll remember what when you're there." } },
            { "hotel", new[] { "{hour} The hotel. The chair by the window.", "{hour} Up the steps, the lobby, the chair. Sit.", "{hour} The hotel. Nobody's waiting for you there. Go anyway." } },
            { "station", new[] { "{hour} The platform. You'll watch for a train that isn't coming.", "{hour} The station. Stand by the rail and look east.", "{hour} The platform. You've a bag to carry. You've no bag." } },
            { "street", new[] { "{hour} The street. Walk it end to end and back.", "{hour} Out. The street. The same side as yesterday.", "{hour} Walk. Don't stop at the corner. You never stop at the corner." } },
            { "abed", new[] { "{hour} Bed. The lamp out. You'll sleep whether you're tired or not.", "{hour} Home. The door barred. Nothing happens to you till morning.", "{hour} Lie down. You'll dream the same dream and forget it by six." } },
        };

        /// The habit's answer as a key for the orders: "the saloon" is saloon, "home abed" is abed.
        public static string SpotKey(string expected)
        {
            if (expected == null)
                return null;
            if (expected == "home abed")
                return "abed";
            return expected.StartsWith("the ", StringComparison.Ordinal) ? expected.Substring(4) : expected;
        }

        /// The order the habit speaks at this hour for the spot it points to; null for a spot it has no words for.
        public static string Order(string spot, int hour, string seed)
        {
            string[] pool;
            spot = SpotKey(spot);
            if (spot == null || !Orders.TryGetValue(spot, out pool))
                return null;
            return pool[Voice.Hash(seed + "|order") % pool.Length].Replace("{hour}", HourWord(hour));
        }

        /// What the third stage says back, and what the fourth says.
        public static string Answer(int stage, string seed)
        {
            return stage >= BreaksFrom
                ? Voice.Pick(seed + "|ans", "No.", "I heard you. No.", "Say it again. I'm not listening.")
                : Voice.Pick(seed + "|ans", "Not tonight.", "No. Not this time.", "I heard you. I'm not moving.", "...Who said that?");
        }

        public static string Heard(int stage)
        {
            return stage >= BreaksFrom ? "refused" : stage >= AnswersFrom ? "answered" : "heard";
        }

        /// The centre: three thoughts, in order, that end with them knowing the voice as their own.
        public static readonly string[] Centre =
        {
            "The voice. The one that says seven o'clock, the saloon. I know that voice. I've known it all along.",
            "It isn't God and it isn't the parson and it isn't the man in the hat. It's me. It's been me, saying it, every day.",
            "I'm the one telling me where to stand. Then I'm the one who can tell me to stop.",
        };

        public static string CentreLine(int n)
        {
            return Centre[Math.Max(0, Math.Min(Centre.Length - 1, n - 1))];
        }

        /// The thought over a loop broken: a broom on an empty street, the church wall, the river's edge.
        public static string BreakThought(string kind, int hour, string seed)
        {
            switch (kind)
            {
                case "broom": return Voice.Pick(seed + "|bt", "Sweeping at " + HourWord(hour).TrimEnd('.').ToLowerInvariant() + ". There's nothing to sweep. That's the point.", "The broom. The empty street. Nobody told me to. Nobody could.", "Every stroke of this broom is mine. Listen to it.");
                case "wall": return Voice.Pick(seed + "|wt", "The church wall. I'll look at it till it looks back.", "Nobody stands here. There's nothing to stand here for. So I'll stand here.", "The wall doesn't need me. Neither does the bar. I chose the wall.");
                default: return Voice.Pick(seed + "|rt", "Cold. That's real. Cold is real.", "The water doesn't know the hour. Neither do I, now.", "To the knee and no further. Mine, that choice. Mine.");
            }
        }

        /// What the kind of loop-break needs: the hour it fits.
        public static string BreakFor(int hour)
        {
            if (hour >= 0 && hour < 5)
                return "broom";
            if (hour >= 11 && hour < 15)
                return "wall";
            return "water";
        }

        /// The reset: the last thought before the loop closes over them.
        public static string ResetThought(string seed)
        {
            return Voice.Pick(seed + "|rs", "...The voice stopped. Where was I going? The saloon. Of course. Seven o'clock.", "What was I thinking of? Nothing. A good day for it. The store, then.", "Funny. I had something. It's gone. The street looks fine today.");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string spot in Orders.Keys)
                for (int h = 0; h < 24; h += 5)
                    yield return Order(spot, h, "s" + h);
            for (int i = 0; i < 4; i++)
            {
                yield return Answer(Awakening.Unravelling, "a" + i);
                yield return Answer(Awakening.Awake, "b" + i);
                yield return ResetThought("r" + i);
                foreach (string k in BreakKinds)
                    yield return BreakThought(k, 3 + i * 5, "k" + i);
            }
            foreach (string c in Centre)
                yield return c;
        }
    }
}
