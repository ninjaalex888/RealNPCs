// A couple running off (FOURTEENHOUR reserve R18; TWELVEHOUR reserve R7; [Encounters] Elopers). Out by a town by day, a young woman and
// a young man come hurrying to you on foot: her family won't have him, and her father's men are a minute behind. J: hide them (they slip
// behind you out of the way and keep down), send them on (they run for it, the way they were going), or give them up (they wait,
// knowing). Then the pursuers come, her father and her brother, asking: point them the wrong way, or the right one (or there they
// are, if you gave them up). Sent wrong, or hidden and the men gone by, the pair get away; pointed right, the men run them down and
// she's walked home between them while he runs. The town has it next day, with your part in it. Why it reads true: an elopement is
// the oldest story a small town tells about itself, and the stranger who was asked which way they went is in every version.
// Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class Elopers
    {
        public static bool On = true;   // [Encounters] Elopers, from the settings
        public const string Kind = "elope";
        public const int Percent = 8, FromHour = 9, ToHour = 21, BehindMs = 50000, PromptMs = 10000;

        public static bool Hour(int clock) { return clock >= FromHour && clock < ToHour; }

        public static string Plea(string seed)
        {
            return Voice.Pick(seed + "|pl", "Please, mister. Her father's men are right behind us. He'll kill Tom if he catches him.", "Mister, help us. Her people won't have me. They're coming.",
                                            "Please. We're to be married, whatever my father says. Don't tell them you saw us.");
        }

        public static string Hidden(string seed) { return Voice.Pick(seed + "|hd", "God bless you. Not a sound, Tom.", "Down. Down, and don't breathe."); }
        public static string SentOn(string seed) { return Voice.Pick(seed + "|so", "Thank you! Come on, run!", "The station, then. Hurry!"); }
        public static string GivenUp(string seed) { return Voice.Pick(seed + "|gu", "You'd hand us to them? ...I see.", "Then we're done. Lord help us."); }

        public static string PursuerAsks(string seed)
        {
            return Voice.Pick(seed + "|pa", "You there! A girl in a blue dress and a no-account with her. Which way?", "My daughter. Run off with a drifter. You see them come by here?",
                                            "Seen a young couple, in a hurry? Speak up, man.");
        }

        public static string PursuerWrong(string seed) { return Voice.Pick(seed + "|pw", "That way? Come on, son!", "Obliged. They won't get far."); }
        public static string PursuerRight(string seed) { return Voice.Pick(seed + "|pr", "There! I see them! Get him!", "That's her. Come here, girl!"); }
        public static string Caught(string seed) { return Voice.Pick(seed + "|ca", "No! Papa, no! I won't go back!", "Tom! Run, Tom!"); }

        /// The town's talk next day: she got away, or was brought home, and your part.
        public static string News(string town, bool away, string how, string seed)
        {
            string t = town ?? "town";
            if (away)
                return how == "hid" ? Voice.Pick(seed + "|na1", "A girl of " + t + " ran off with her sweetheart, and her father's men went right past where they hid. A stranger, they say, kept them close.", "The talk of " + t + ": a girl eloped, and a stranger hid the pair while her father rode by.")
                                    : Voice.Pick(seed + "|na2", "A girl of " + t + " ran off with a young man, and a stranger sent her father's men the wrong way. Married by now, like as not.", "Her father's men were pointed down the wrong road, and the girl from " + t + " is gone with her sweetheart.");
            return Voice.Pick(seed + "|nb", "A girl of " + t + " tried to run off with a young man. Her father's men brought her home; a stranger showed them which way she went.", "The girl who ran off from " + t + " is home again, between her father and her brother. The young man ran.");
        }

        public static System.Collections.Generic.List<InteractOption> FirstRows()
        {
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "elope:hide", Label = "\"Behind me. Keep down.\" (hide them)" },
                new InteractOption { Id = "elope:send", Label = "\"Go. Run for it.\" (send them on)" },
                new InteractOption { Id = "elope:give", Label = "\"Wait here.\" (give them up)" },
            };
        }

        public static System.Collections.Generic.List<InteractOption> PursuerRows(bool gaveUp)
        {
            if (gaveUp)
                return new System.Collections.Generic.List<InteractOption> { new InteractOption { Id = "elope:there", Label = "\"They're here.\"" } };
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "elope:wrong", Label = "\"Saw them head for the river.\" (the wrong way)" },
                new InteractOption { Id = "elope:right", Label = "Show them the way they went" },
            };
        }
    }
}
