// Pinkertons asking (FIVEDAY item 15; roadmap.md's idea bank: "a man in a bowler asks regulars about the player. Those who
// like him lie for him; those who fear him talk; a friend tells him which"). Once in a while, in a town where a violent
// deed of the player's is known from the past week, a man in a city suit turns up and works the street: he walks up to two
// or three of the town's people the mind knows and asks after "a man of your description". Each answers by what they feel
// for the player: a friend lies for him ("Never seen anyone like that"), the afraid tell what they know, the rest shrug or
// send him on. Then he goes, and the town knows he was asking; a friend of the player's coming over later warns him of it
// (FriendCalls' warning, the "pinkerton" kind) and says who talked. He is spawned for it, and released after.
// Behind [Encounters] Pinkertons.
//
// Why it reads true: the Pinkerton National Detective Agency was the railroads' and the banks' hunter of outlaws by 1899,
// its men asking questions in towns under false names; what a town told them depended on whom it liked and whom it feared,
// and outlaws lived or hanged by those answers. The psychologist's lens: loyalty and reciprocity against fear, and the
// outsider who has no claim on anyone. Plain C#, no game; all words original. mindtest PinkertonsTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Pinkertons
    {
        public const int Chance = 25;          // percent a qualifying town gets a visit on a given game day
        public const int GapDays = 5;          // at most one visit a town in five days
        public const int KnownDays = 7;        // a deed of his known in town within a week brings them
        public const int MostAsked = 3;

        /// How someone answers: "lie" (they like him), "talk" (they fear him, or they're law-minded), "shrug".
        public static string Answer(bool friend, bool afraid, Traits t, string seed)
        {
            if (friend && !afraid)
                return "lie";
            if (friend && afraid)
                return Voice.Chance(seed + "|torn", 50) ? "lie" : "shrug";
            if (afraid)
                return "talk";
            float lawful = t != null ? t.Z[Traits.Deference] + 0.5f * t.Z[Traits.Honesty] : 0f;
            return lawful > 0.8f ? "talk" : "shrug";
        }

        private static readonly string[] Ask =
        {
            "Afternoon. I'm looking for a man. Tall, dark coat, rides well. Seen anyone like that?",
            "A moment of your time, friend. A man passed through here lately. Big fellow, quiet. You'd remember him.",
            "I'm with an agency out of Chicago. There's a man I'd like a word with. You'd know the one.",
            "Pardon me. A stranger did some business here this week. Which way did he ride?",
        };

        private static readonly string[] Lie =
        {
            "Can't say I've seen anybody like that.",
            "Tall man? No. Short fellow came through, though. Went south.",
            "We don't get strangers here. Try the next town.",
            "My eyes aren't what they were, mister.",
        };

        private static readonly string[] Talk =
        {
            "He was here. Rode in from the north. Don't tell him I said so.",
            "You mean the one who... yes. He was at the saloon. I'll say no more.",
            "He's been through more than once. Ask at the livery.",
            "I saw him. God help me, I saw what he did.",
        };

        private static readonly string[] Shrug =
        {
            "Lot of men come through. I don't look close.",
            "Ask the sheriff. That's his business.",
            "Couldn't tell you.",
            "Not my affair, mister.",
        };

        private static readonly string[] Leaves = { "Much obliged.", "I'll be at the hotel, if your memory improves.", "Thank you for your time." };

        public static string AskLine(string seed) { return Pick(Ask, seed + "|ask"); }
        public static string LeaveLine(string seed) { return Pick(Leaves, seed + "|leave"); }

        public static string AnswerLine(string answer, string seed)
        {
            return Pick(answer == "lie" ? Lie : answer == "talk" ? Talk : Shrug, seed + "|ans");
        }

        /// The warning a friend brings after (FriendCalls' warning kind "pinkerton"): {you}, {who} one who talked or null.
        public static string Warning(string you, string talked, string seed)
        {
            string w = talked != null
                ? Voice.Pick(seed + "|wn", "{you}, a Pinkerton man was in town asking after you. {who} told him plenty.", "There was a detective here lately, {you}, asking about you. {who} talked. I didn't.")
                : Voice.Pick(seed + "|ww", "{you}. A man from the Pinkerton agency was asking about you. Nobody told him a thing, as far as I know.", "Watch yourself, {you}. A city man in a bowler hat was asking after a man of your description.");
            return w.Replace("{you}", you ?? "friend").Replace("{who}", talked ?? "somebody");
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Ask, Lie, Talk, Shrug, Leaves })
                foreach (string l in p)
                    yield return l;
            for (int i = 0; i < 6; i++)
            {
                yield return Warning("Morgan", "Abner", "w" + i);
                yield return Warning(null, null, "x" + i);
            }
        }
    }
}
