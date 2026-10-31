// Personal space (NIGHT3; the psychologist's list: "stand too close and people step back, stare, or tell him to"). Someone
// the player stands right up against (under a metre, less in a saloon) for a couple of seconds, both of them standing
// still, minds it the way their feeling for him and their nature say: anyone afraid of him steps back with a glance; the
// timid step back and worry; the plain ask for room and step back; the proud tell him to mind himself and step aside; the
// hot-tempered, and anyone against him, tell him to back off and stand their ground; the cold give him a look and
// nothing more; a friend makes light of it. Played by the street reactions' own player (Sights.Look, WestworldScript.
// Sights.cs SightPlay): the look, a gesture, a recorded line where the feeling has one, with our words as its
// subtitle, the step back, the thought. Plain C#; all words original to this add-on.
namespace EnrageWestworld.Core
{
    public static class Space
    {
        public const float Metres = 0.95f, SaloonMetres = 0.75f;   // a saloon's bar is shoulder to shoulder
        public const int LingerMs = 1800;                          // both still this long before it's minded
        public const int AgainMs = 300000;                         // the same person minds it again after five minutes at the soonest

        /// How they take it: regard (afraid, hostile, fond, or null) first, then their temper.
        public static string Feeling(string temper, string regard)
        {
            if (regard == "afraid")
                return "afraid";
            if (regard == "hostile")
                return temper == "Coward" ? "timid" : "bristle";   // sore at him and timid with it: they back off (anger and fear together withdraw)
            if (regard == "fond")
                return "easy";
            switch (temper)
            {
                case "Hothead": return "bristle";
                case "Proud": return "stiff";
                case "Coward": return "timid";
                case "Cold": case "Greedy": return "cold";
                default: return "uneasy";
            }
        }

        /// The whole reaction, for the street reactions' player.
        public static Sights.Look Look(string temper, string regard, bool woman, string seed, bool indoors = false)
        {
            string feel = Feeling(temper, regard);
            var l = new Sights.Look { State = "close", Feeling = feel };
            int r = Voice.Hash(seed + "|sp") % 100;
            switch (feel)
            {
                case "afraid":
                    l.Gesture = "none";
                    l.LookMs = 700;
                    l.StepsBack = true;
                    if (r < 50)
                    {
                        l.Voice = new[] { "GENERIC_FRIGHTENED_MED" };
                        l.Spoken = Pick(seed, "Mister, I'll get out of your road.", "All right. I'm moving off. I'm moving.");
                    }
                    break;
                case "timid":
                    l.Gesture = "none";
                    l.LookMs = 900;
                    l.StepsBack = true;
                    break;
                case "bristle":
                    l.Gesture = "none";
                    l.LookMs = 2600;   // a level look, and stays put
                    l.Voice = new[] { "WHATS_YOUR_PROBLEM" };
                    l.Spoken = woman ? Pick(seed, "Step back, sir, or I'll call the marshal.", "You'd best find somewhere else to stand, sir.")
                                     : Pick(seed, "Back off. You're standing on my boots.", "Something you want? Then back up and say it.", "Give me room, friend, or give me a reason.");
                    break;
                case "stiff":
                    l.Gesture = "dismiss";
                    l.LookMs = 1800;
                    l.StepsBack = true;
                    if (r < 70)
                    {
                        l.Voice = new[] { "PLAYER_LOOKING_WEIRD" };
                        l.Spoken = woman ? "I'll thank you to keep your distance, sir." : Pick(seed, "I'll thank you to give a man room.", indoors ? "Mind yourself, mister. There's room enough." : "Mind yourself, mister. There's a whole street.");
                    }
                    break;
                case "cold":
                    l.Gesture = r < 40 ? "dismiss" : "none";
                    l.LookMs = 2200;
                    break;
                case "easy":
                    l.Gesture = "none";
                    l.LookMs = 1500;
                    if (r < 60)
                    {
                        l.Voice = new[] { "GREET_GENERAL_FAMILIAR" };
                        l.Spoken = woman ? Pick(seed, "Folks will talk, you standing so close.", "Mind, now. People will talk.") : Pick(seed, "Close enough to share a hat, friend.", "Planning to whisper something, are you?");
                    }
                    break;
                default:   // uneasy
                    l.Gesture = r < 30 ? "headshake" : "none";
                    l.LookMs = 1500;
                    l.StepsBack = true;
                    if (r < 65)
                    {
                        l.Voice = new[] { "PLAYER_LOOKING_WEIRD" };
                        l.Spoken = woman ? Pick(seed, "Sir, a little room, if you please.", "You're standing awful close, mister.")
                                         : Pick(seed, "A little room, friend.", "You mind? You're right on top of me.");
                    }
                    break;
            }
            l.Thought = Thought(feel, woman, seed, temper);
            return l;
        }

        private static string Thought(string feel, bool woman, string seed, string temper)
        {
            if (feel == "cold" && temper == "Greedy")
                return Pick(seed + "|t", "Close as that. Hand on the purse.", "That's how a purse goes missing. Not mine.");   // jostling was how pickpockets worked
            switch (feel)
            {
                case "afraid": return Pick(seed + "|t", "Right up against me. Like he's deciding something.", "Don't look at him. Step away. Slow.");
                case "timid": return Pick(seed + "|t", "Why's he stood so close? What did I do?", "Too close. Just move. Don't make a thing of it.");
                case "bristle": return Pick(seed + "|t", "Crowding me. Let him try something.", "Stands that close to a man, he's asking.");
                case "stiff": return Pick(seed + "|t", "No manners. Not a shred.", "Raised in a barn, that one.");
                case "cold": return Pick(seed + "|t", "Stands close. Wants me to notice. I won't.", "Close as you like, mister. Makes no odds to me.");
                case "easy": return Pick(seed + "|t", "Always did stand too close, that one.", "Friendly sort. Too friendly, some days.");
                default: return woman ? Pick(seed + "|t", "A man doesn't stand that close to a woman he doesn't know.", "Right at my elbow. Strange man.")
                                      : Pick(seed + "|t", "Odd fellow. Stood right on top of me.", "Some folk don't know where they end.");
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed + "|pk") % lines.Length];
        }

        public static System.Collections.Generic.IEnumerable<string> AllLines()
        {
            foreach (string regard in new[] { null, "afraid", "hostile", "fond" })
                foreach (string temper in new[] { null, "Hothead", "Proud", "Coward", "Cold", "Greedy" })
                    for (int i = 0; i < 12; i++)
                        foreach (bool w in new[] { false, true })
                        {
                            Sights.Look l = Look(temper, regard, w, "all" + i, i % 2 == 1);
                            if (l.Spoken != null) yield return l.Spoken;
                            if (l.Thought != null) yield return l.Thought;
                        }
        }
    }
}
