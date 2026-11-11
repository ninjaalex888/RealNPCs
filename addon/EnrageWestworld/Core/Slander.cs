// Spread a rumour (DEPTH reserve R3; [Scenes] SpreadRumour, on). In a saloon, J at a man who likes to talk (the mind's sociable man
// near you at the bar): a word in his ear about a townsman you know by name. The town's tales carry it (Core/Tales.cs, kind
// "slander") and it bends as they all do: said once, then "for years", then run out of another town for it; the man it's about,
// when he's near you and has heard it, thinks it, and a stranger's name comes into it. Why it reads true: a rumour needs only a
// willing mouth and a name, and it grows by what each teller adds; Allport and Postman's sharpening, aimed this time. Plain C#; the
// game side is Game/WestworldScript.Slander.cs.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Slander
    {
        public static bool On = true;   // [Scenes] SpreadRumour, from the settings
        public const string Kind = "slander", PromptKind = "slander";
        public const float TalkerMetres = 4f;
        public static readonly string[] Charges = { "cards", "drink", "thief", "wife" };

        /// The charge said of him, by who he is and the day (the same man the same day, the same charge).
        public static string ChargeFor(string targetId, long day) { return Charges[(Voice.Hash(targetId + "|" + day + "|charge") & 0x7fffffff) % Charges.Length]; }

        static string Words(string charge)
        {
            switch (charge)
            {
                case "cards": return "cheats at cards";
                case "drink": return "drinks his wages away";
                case "thief": return "has light fingers";
                default: return "is no husband to his wife";
            }
        }

        public static List<InteractOption> Rows(List<KeyValuePair<string, string>> targets, long day)
        {
            var rows = new List<InteractOption>();
            foreach (KeyValuePair<string, string> t in targets)
                rows.Add(new InteractOption { Id = "slander:" + t.Key, Label = "Tell him " + Person.FirstOf(t.Value) + " " + Words(ChargeFor(t.Key, day)) + "." });
            rows.Add(new InteractOption { Id = "slander:no", Label = "Say nothing." });
            return rows;
        }

        /// The talker's answer when you've said it.
        public static string Takes(string target, string seed)
        {
            return Voice.Pick(seed + "|tk", "Is that so? Well, well. " + target + ".", "I always thought there was something about " + target + ".", "You don't say. Wait till the boys hear.");
        }

        /// The tale as one tells it now (Tales.Telling hands slander here): bigger with the days and the tellings.
        public static string Telling(TownTale t, int days, string seed)
        {
            string who = t.Starter ?? "a man", charge = t.How ?? "cards";
            bool far = days >= 1 || t.Told >= 2;
            if (!far)
                return Voice.Pick(seed + "|s0", "They say " + who + " " + Words(charge) + ".", "Heard " + who + " " + Words(charge) + ". From a man who'd know.");
            if (days < 2 && t.Told < 4)
                return Voice.Pick(seed + "|s1", who + "'s " + (charge == "cards" ? "been cheating at cards for years" : charge == "drink" ? "drunk his wages away for years" : charge == "thief" ? "been light-fingered for years" : "been no husband for years") + ", they say.",
                                              "Everybody knows about " + who + " now. " + Grammar.UpperFirst(Words(charge)) + ", and has for years.");
            return Voice.Pick(seed + "|s2", who + " was run out of Rhodes for it, I heard. " + Grammar.UpperFirst(Words(charge)) + ".", "They say " + who + " " + Words(charge) + ", and worse. Run out of two towns.");
        }

        /// What he thinks, having heard it said of him.
        public static string HeardOfIt(string charge, string seed)
        {
            string mine = charge == "cards" ? "cheat at cards" : charge == "drink" ? "drink my wages away" : charge == "thief" ? "have light fingers" : "am no husband to my wife";
            return Voice.Pick(seed + "|hd", "Somebody's telling the town I " + mine + ". A stranger started it, I'd wager.",
                                            "Whispers. Behind my back. I know what they're saying, and it's a lie.", "Who's been talking about me? I'll find out who.");
        }

        public static IEnumerable<string> AllLines()
        {
            var t = new TownTale { Starter = "Abe", How = "cards" };
            foreach (string c in Charges)
            {
                t.How = c;
                for (int d = 0; d < 3; d++) for (int i = 0; i < 4; i++) yield return Telling(t, d, "x" + i);
                for (int i = 0; i < 4; i++) yield return HeardOfIt(c, "h" + i);
            }
            for (int i = 0; i < 4; i++) yield return Takes("Abe", "t" + i);
        }
    }
}
