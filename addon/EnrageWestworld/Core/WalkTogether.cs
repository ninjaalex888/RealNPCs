// Friends walk together (FOURTEENHOUR item 15; [Mind] WalkTogether). A tie you can see from across the street is how a town reads
// as people who know each other: two friends who've had their word go on side by side, to the saloon after the day's work, homeward
// at dusk, or (a courting couple, a Sunday afternoon) for a stroll, the 1899 way of walking out; a thought on the way, a nod as they
// part at a door. Already there and left alone: a family or a couple leaving together after a fright (GroupActs), the take-away
// from a bully (the stand-up's leavewith), a friend getting a drunk home on his arm ("carried"), Arthur's own walk home. The ring held
// 0-2 of its 12 in Lex's log: there's room for two such pairs a town at once. Plain C#, no game; the game side is
// Game/WestworldScript.WalkTogether.cs.
namespace EnrageWestworld.Core
{
    public static class WalkTogether
    {
        public static bool On = true;   // [Mind] WalkTogether, from the settings
        public const int MostPairs = 2, FriendPercent = 55, CouplePercent = 70, LeastWarmth = 2;
        public const int WalkMs = 45000;
        public const float StrollMetres = 40f, HomeMetres = 35f, ArriveMetres = 4f;

        /// The talks after which two friends may go on together (the street's quiet kinds, the warm ones).
        public static bool TalkKind(string kind)
        {
            switch (kind)
            {
                case "smalltalk": case "mindtalk": case "gossip": case "timestalk": case "hosts": case "comfort": case "cheer": case "apology": case "sweet":
                    return true;
                default:
                    return false;
            }
        }

        /// Where they'd go: "saloon" (two men, the working day done, a saloon in town), "stroll" (a courting couple on a Sunday
        /// afternoon, or any fine afternoon now and then), "home" (dusk); null for nowhere just now.
        public static string Where(int hour, int weekday, bool bothMen, bool couple, bool saloonInTown, string seed)
        {
            if (couple && hour >= 12 && hour < 18 && (weekday == 0 || Voice.Chance(seed + "|stroll", 30)))
                return "stroll";
            if (bothMen && saloonInTown && hour >= 17 && hour < 23)
                return "saloon";
            if (hour >= 18 && hour < 21)
                return "home";
            return null;
        }

        /// Whether these two go on together: friends (warmth 2 or more) a little over half the time, a couple more often. Seeded.
        /// `respect`: one of them looks up to the other (Opinions, FOURTEENHOUR item 23): a little likelier.
        public static bool Goes(int warmth, bool couple, string seed, bool respect = false)
        {
            if (!On || (warmth < LeastWarmth && !couple))
                return false;
            return Voice.Chance(seed + "|walk", (couple ? CouplePercent : FriendPercent) + (respect ? Opinions.RespectWalkPercent : 0));
        }

        /// The one in front's thought as they set off.
        public static string Thought(string where, string other, string seed)
        {
            string o = other ?? "him";
            switch (where)
            {
                case "saloon": return Voice.Pick(seed + "|ts", o + " and me, a glass when the day's done. Best hour there is.", "Walk down with " + o + ". He'll buy the first. He always says he will.", "Day's done. " + o + "'s good company for the walk.");
                case "stroll": return Voice.Pick(seed + "|tc", "Walking out with " + o + ". The whole street's looking. Let it look.", o + " on my arm, and the afternoon ours.", "Slow, now. Make the walk last.");
                default: return Voice.Pick(seed + "|th", "Walk home with " + o + ". Saves talking to myself.", o + "'s going my way. Good.", "Evening coming on. Nice to have " + o + " beside me.");
            }
        }

        /// A word as they part.
        public static string Parting(string where, string seed)
        {
            switch (where)
            {
                case "saloon": return Voice.Pick(seed + "|ps", "After you.", "First one's mine.", "In we go.");
                case "stroll": return Voice.Pick(seed + "|pc", "Same time Sunday?", "I'll see you to church, then.", "That was a good walk.");
                default: return Voice.Pick(seed + "|ph", "Night, then.", "See you tomorrow.", "Watch your step in the dark.");
            }
        }
    }
}
