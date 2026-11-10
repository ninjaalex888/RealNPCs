// Riders: one runner, two kinds (FOURTEENHOUR reserve R9; TWELVEHOUR item 6; [Encounters] Riders). The road is where you spend your
// time, and no road scene had anyone mounted. One rider runner, on the one proven ride (the allies': a horse made under the rider
// unseen, the game's go-to-entity from the saddle given again about once a second, Core/StalkerRide.cs's watchdog: 3 m in 5 s or he
// gets down and walks), and an on-foot twin for each (no horse to be had: the same person walks up, the traveller's way).
//   Riding along (6a): a resident of the town you're riding toward comes up beside you, keeps near a minute or two and talks by
//     their nature and their worry (the mind's lines), then turns off; J "Ride on together" (he rides on ahead to town and knows
//     you there) or "Good day".
//   A deputy asking after a man (6b): while a posse is out from a town near (Core/PosseRides.cs), a lawman rides up and asks if
//     you've seen him; J: point him the right way, the wrong way, or say nothing, and the posse's end follows your word (a man
//     brought back, the trail gone cold, or as it would have gone).
// They take the traveller's turn on the road (half the time on a road, a rider instead), so the road's pace doesn't rise.
// Why it reads true: on an empty road a stranger who matches your pace is either company or a question, and people tell a passing
// stranger what they'd never tell a neighbour. Nobody gets hurt in either. Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class Riders
    {
        public static bool On = true;   // [Encounters] Riders, from the settings
        public const string Kind = "rider";
        public const int SharePercent = 50, AlongMs = 100000, TalkGapMs = 16000, PromptMs = 9000, DeputyPercent = 60;
        public const float NearMetres = 7f, StopMetres = 4f;

        /// His greeting as he comes up beside you, by his nature.
        public static string Greeting(string temper, string town, string seed)
        {
            string to = town ?? "town";
            switch (temper)
            {
                case "Hothead": case "Proud": return Voice.Pick(seed + "|g", "You're riding my road, friend. Might as well ride it together.", "Headed to " + to + "? So am I. Don't fall behind.");
                case "Cold": return Voice.Pick(seed + "|g", "Going to " + to + ".", "Same road. I'll not talk your ear off.");
                case "Coward": case "Loyal": return Voice.Pick(seed + "|g", "Mind if I ride along a while? Road's lonely.", "Safer two together, they say. Headed to " + to + "?");
                default: return Voice.Pick(seed + "|g", "Afternoon. Headed to " + to + "?", "Good riding weather. Mind the company?", "You look like you know this road better than me.");
            }
        }

        /// What he says riding along when there's no worry to tell: a few words by his nature.
        public static string Talk(string temper, string seed)
        {
            switch (temper)
            {
                case "Hothead": return Voice.Pick(seed + "|t", "Man at the feed store tried to cheat me. Didn't try twice.", "I'd sooner fight than haggle. Saves time.");
                case "Proud": return Voice.Pick(seed + "|t", "My father built half of that town. Not that they remember.", "Best horse in the county, this. Don't let anyone tell you different.");
                case "Greedy": return Voice.Pick(seed + "|t", "There's money in this country if a man keeps his eyes open.", "Price of feed's a crime. A crime.");
                case "Coward": return Voice.Pick(seed + "|t", "Heard there's men on this road who'd shoot you for your boots.", "I don't ride after dark anymore. Not since last spring.");
                case "Cold": return Voice.Pick(seed + "|t", "...", "Weather's turning.");
                default: return Voice.Pick(seed + "|t", "Nothing out here but the wind. Suits me.", "Wife says I talk to the horse more than to her. Horse listens.", "They say the railroad's coming through. They always say that.");
            }
        }

        /// His last word as he turns off, or as he rides on ahead (together: you said ride on with him).
        public static string Parting(bool together, string town, string seed)
        {
            return together ? Voice.Pick(seed + "|pt", "I'll ride on ahead. Look me up in " + (town ?? "town") + ".", "See you in " + (town ?? "town") + ", then. Drinks are on me.")
                            : Voice.Pick(seed + "|pn", "This is my turning. Obliged, and safe road.", "Well. Obliged for the company.", "Ride careful.");
        }

        /// The deputy's question.
        public static string DeputyAsks(string town, string seed)
        {
            return Voice.Pick(seed + "|da", "Afternoon. Posse's out of " + (town ?? "town") + " after a man. You seen anybody riding hard?",
                                            "Hold up there. We're after a man from " + (town ?? "town") + ". Pass anybody on this road?");
        }

        public static string DeputyAnswer(string how, string seed)
        {
            return how == "right" ? Voice.Pick(seed + "|ar", "Obliged. We'll have him by dark.", "That's our man. Thank you kindly.")
                 : how == "wrong" ? Voice.Pick(seed + "|aw", "That way? Obliged. We'll ride hard.", "The hills. Figures. Thank you.")
                 : Voice.Pick(seed + "|an", "Not much for talking, are you. Good day.", "Suit yourself.");
        }

        /// The J rows: riding along, or the deputy's.
        public static System.Collections.Generic.List<InteractOption> Rows(bool deputy, string name)
        {
            if (deputy)
                return new System.Collections.Generic.List<InteractOption>
                {
                    new InteractOption { Id = "rider:right", Label = "\"He went that way.\" (the right way)" },
                    new InteractOption { Id = "rider:wrong", Label = "\"Saw a man head for the hills.\" (the wrong way)" },
                    new InteractOption { Id = "rider:none", Label = "Say nothing" },
                };
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "rider:together", Label = "\"Ride on together.\"" },
                new InteractOption { Id = "rider:goodday", Label = "\"Good day.\"" },
            };
        }

        /// The posse's end by your word: "right" brings a man back, "wrong" leaves the trail cold, else as it would have gone (a
        /// posse man's death, rolled, stays: your word doesn't stop a bullet from the rocks).
        public static string PosseOutcome(string rolled, int word)
        {
            if (rolled == "dead")
                return rolled;
            return word == 1 ? "brought" : word == 2 ? "lost" : rolled;
        }
    }
}
