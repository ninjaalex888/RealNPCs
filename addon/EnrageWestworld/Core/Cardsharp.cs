// The cardsharp run out of town (FOURTEENHOUR reserve R6; [Scenes] CardsharpRunOut). A card cheat was already called at a table, and
// half the time in a saloon the man he cheated told him to walk out that door, and he went, and thought about it on the boardwalk
// (Scenes.Cheat; the thrown-out cue). Now, when there are men at that table to do it, the table sees him off properly: up to two of
// them (the man he cheated first, then those at his elbow, getting up from their seats Rockstar's way) see him through the door and
// down the street out past the last houses, one word at the door and one at the last houses, then turn back; he walks on. It leaves bad blood
// (a tie that won't soon mend, with each of them), and another day, two to five on, he's back in that town, sore, and thinks of it
// when you're near. Why it reads true: a town policed its own tables long before it had a sheriff to do it, and the shame of the walk
// was the sentence; the man who comes back anyway has decided something about that town. Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class Cardsharp
    {
        public static bool On = true;   // [Scenes] CardsharpRunOut, from the settings
        public const string Kind = "runout";
        public const int RunPercent = 60, MostMen = 2, WalkMs = 70000;
        public static int BackLeastDays { get { return SessionClock.Days(2, 1); } }   // the session clock (DEPTH item 16): SessionClock.Days(as built, a session's)
        public static int BackMostDays { get { return SessionClock.Days(14, 2); } }
        public const float TableMetres = 5f, EdgePast = 15f, MostWalk = 140f;   // the table's men within this of the man he cheated; the spot to walk him to, past the town's core

        /// Whether the table runs him out (men to do it, and the roll); else he's only shown the door, as before.
        public static bool RunsOut(int menAtTable, string seed)
        {
            return On && menAtTable > 0 && Voice.Chance(seed + "|runout", RunPercent);
        }

        public static string AtDoor(string seed)
        {
            return Voice.Pick(seed + "|door", "Walk. We'll see you out past the last houses.", "Keep moving. We're going your way.", "No, friend, not the back door. The long way, with company.");
        }

        public static string CheatGoes(string seed)
        {
            return Voice.Pick(seed + "|goes", "I can find my own way, gentlemen.", "An escort. I'm honored.", "Alright, alright. No need to crowd a man.");
        }

        public static string AtEdge(string seed)
        {
            return Voice.Pick(seed + "|edge", "This is as far as we go. You keep on.", "There's the road. Use it.", "Show your face at our table again and you'll leave lighter.");
        }

        public static string Retort(string seed)
        {
            return Voice.Pick(seed + "|ret", "Your mothers would be proud.", "Fine town. Fine people. Rot, the lot of you.", "I'll remember every face.");
        }

        /// His thought, back in that town another day.
        public static string Sore(string town, string seed)
        {
            return Voice.Pick(seed + "|sore", "Walked out of " + (town ?? "this town") + " like a stray dog. Well, the dog's back.", "Two days on the road over a crimped card. Back now, and quieter.",
                                             "They'll remember my face. Good. I remember theirs.");
        }

        /// What the town has of it, for the talk (a clause).
        public static string News(string cheat, string town)
        {
            return "the card table at the saloon walked " + (cheat ?? "a sharper") + " out past the last houses of " + (town ?? "town");
        }
    }
}
