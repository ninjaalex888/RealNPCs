// The rider who asks the way to a town on no map (FOURTEENHOUR reserve R11; TWELVEHOUR item 16; [Encounters] NoMapRider). Night on
// a road: a rider (the riders' runner, reserve R9; on foot when you are) comes up and asks the way to a town nobody has heard of,
// thanks you whatever you say, and goes on into the dark. A day or two later the nearest town has the news of a man found dead on that
// road, a stranger nobody knew, and the town he asked for on no map; and those near you who hear it think of it. Rare: on the
// traveller's turn at night (when the traveller doesn't walk), one in ten, and never within two weeks of the last. Why it reads true:
// the unsettling thing is the ordinary request with one wrong detail, and the news afterwards that makes it a story you'll repeat.
// Plain C#, no game; all words this add-on's, the towns too.
namespace EnrageWestworld.Core
{
    public static class NoMapRider
    {
        public static bool On = true;   // [Encounters] NoMapRider, from the settings
        public const int Percent = 10, GapDays = 14, FromHour = 21, ToHour = 4;
        public const string LastKey = "nomap:lastday";

        static readonly string[] Places = { "Calder's Rest", "Hollow Pines", "Gilead Bend", "Saint Agnes Crossing", "Larkin's Ferry", "Mercy Flats", "Absalom's Well", "Little Sorrow Station" };

        public static bool Hour(int clock) { return clock >= FromHour || clock < ToHour; }

        public static string Place(string seed) { return Places[(Voice.Hash(seed + "|place") & 0x7fffffff) % Places.Length]; }

        public static bool Due(long today, int lastDay, string seed)
        {
            return On && (lastDay <= 0 || today - lastDay >= GapDays) && Voice.Chance(seed + "|nomap", Percent);
        }

        public static string Asks(string place, string seed)
        {
            return Voice.Pick(seed + "|ask", "Evening. Is this the road to " + place + "? I've been riding since sundown.", "Beg pardon. How far to " + place + "? They told me it was this way.",
                                             "You'll know it: " + place + ". There's a church with a white steeple. Which way?");
        }

        public static string Thanks(string answer, string seed)
        {
            return answer == "never" ? Voice.Pick(seed + "|tn", "No? Well. It's out here somewhere. Good night to you.", "Strange. Everyone says that. Good night.")
                 : answer == "back" ? Voice.Pick(seed + "|tb", "Turn back? No. They're expecting me. Good night.", "I can't turn back now. Good night, friend.")
                 : Voice.Pick(seed + "|to", "Down the road. Obliged. They're expecting me.", "Thank you kindly. Not far now, then.");
        }

        /// The nearest town's news a day or two on.
        public static string News(string place, string town, string seed)
        {
            return Voice.Pick(seed + "|nw", "A rider was found dead a few miles from " + (town ?? "town") + ". Nobody knew him. In his pocket, a letter addressed to " + place + ", and there's no " + place + " on any map.",
                                            "They found a stranger dead on the road at first light, his horse standing over him. Folk say he'd been asking the way to " + place + ". There's no such place.");
        }

        /// The thought of one who hears it.
        public static string Thought(string place, string seed)
        {
            return Voice.Pick(seed + "|th", place + ". I've lived here all my life. There's no " + place + ".", "Asking the way to a town that isn't there. And then dead on the road. Lord.",
                                            "Somebody must have seen him that night. Somebody must have told him the way.");
        }

        public static System.Collections.Generic.List<InteractOption> Rows(string place)
        {
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "nomap:never", Label = "\"Never heard of it.\"" },
                new InteractOption { Id = "nomap:on", Label = "\"Down the road, I'd guess.\"" },
                new InteractOption { Id = "nomap:back", Label = "\"Turn back, friend. Ride with me to town.\"" },
            };
        }
    }
}
