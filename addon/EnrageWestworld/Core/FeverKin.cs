// The fevered's kin kept clear of (DEPTH item 12; the roadmap's "Yellow fever summer", whose half was FIVEDAY R9's fever: the sick keep
// to home, the board of health's notice; with [World] Fever). In a fever town, those who live with the sick (the husband or wife by
// name, a sweetheart, the kin the story names: Engine.FeverHousehold) are kept clear of in the street: a greeting given and not
// returned, a step aside (a Director street scene, kind "feveravoid"), by the passer's nature: the timid most, the hard next, the rest
// now and then; the devout and the kind instead ask after the sick (kind "askafter"). The kin think of being passed by. Why it reads
// true: with no germ theory in the street, fear of contagion fell on whole families: disgust's avoidance reaches whatever touched the
// sick, and the brave and the pious were the ones who still knocked. Plain C#; the game side is the street's (Reasons, Heart, the
// Director) and Game/WestworldScript.FeverKin.cs for the kin's thought.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class FeverKin
    {
        public const string AvoidKind = "feveravoid", AskKind = "askafter";
        public static bool On = true;   // with [World] Fever: the game side sets Someone.FeverSick only with the fever on
        public const int AskStrength = 41, AvoidStrength = 41;

        /// How often a passer keeps clear of the house of the sick in a day, by his nature (percent).
        public static int AvoidPercent(string temper)
        {
            switch (temper)
            {
                case "Coward": return 60;
                case "Cold": case "Greedy": return 35;
                case "Hothead": case "Proud": return 25;
                case "Loyal": return 8;
                default: return 30;
            }
        }

        /// x passes y in the street, y of a sick house (y.FeverSick: the sick one's first name) and x not: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, bool night)
        {
            if (!On || x == null || y == null || x.Id == y.Id || y.FeverSick == null || x.FeverSick != null || saloon || night)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            bool kind = x.Temper == "Loyal" || (x.P != null && x.P.Devotion == "Devout");
            if (kind && Voice.Chance(key + gameDay + "|askafter", 60))
                return new Reason { Kind = AskKind, Strength = AskStrength, Why = x.Name + " asks " + y.Name + " after " + y.FeverSick + ", abed with the fever" };
            if (Voice.Chance(key + gameDay + "|feveravoid", AvoidPercent(x.Temper)))
                return new Reason { Kind = AvoidKind, Strength = AvoidStrength, Why = x.Name + " keeps clear of " + y.Name + ", whose house has the fever" };
            return null;
        }

        private static readonly string[] Greets = { "Morning, {a}.", "{a}. Fine day.", "Afternoon, {a}." };
        private static readonly string[] Asks = { "How is {s}? We're praying for the whole house.", "Is {s} any better? Tell me true.", "If there's anything you need for {s}, you send for me." };
        private static readonly Dictionary<string, string[]> Answers = new Dictionary<string, string[]>
        {
            { "plain", new[] { "No better. But thank you for asking. Not many do.", "The doctor says a few days yet. Thank you.", "You're the first to stop. God bless you." } },
            { "bold", new[] { "Bad. But we'll come through. Thank you.", "Thank you. That means something, this week." } },
            { "fearful", new[] { "I'm frightened for her, if I'm honest.", "Worse last night. I hardly sleep." } },
            { "cold", new[] { "The same. Obliged.", "Holding on." } },
        };
        private static readonly string[] Shunned = { "{a} crossed the street sooner than say good morning. As if we'd brought it on ourselves.", "Not one of them looks at me now. Not since {s} took sick.",
                                                     "They step off the boards when they see me coming. My own neighbours." };

        /// The passer keeps clear: the kin's greeting, and the passer steps away without a word.
        public static Scene Avoid(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed);
            var s = new Scene { Kind = AvoidKind, Doer = 0, Summary = "a passer kept clear of one whose house has the fever" };
            s.Add(1, Pick(Greets, seed + "|g").Replace("{a}", a), "greet", "face")
             .Add(0, null, null, "walkaway")
             .Add(1, null, null, "walkaway");
            return s;
        }

        /// The kind or the devout ask after the sick instead; the kin answer by their nature.
        public static Scene AskAfter(SceneCast c, string seed)
        {
            string sick = c != null && c.FeverSick != null ? c.FeverSick : "the sick";
            string temper = c != null && c.Tempers != null ? c.Tempers[1] : null;
            string[] ans;
            if (!Answers.TryGetValue(Minds.TemperGroup(temper) ?? "", out ans))
                ans = Answers["plain"];
            var s = new Scene { Kind = AskKind, Doer = 0, Summary = "a neighbour asked after one sick with the fever" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Pick(Asks, seed + "|a").Replace("{s}", sick), "mild", "face")
             .Add(1, Pick(ans, seed + "|r"), "thanks", "face")
             .Add(0, null, null, "walkaway")
             .Add(1, null, null, "walkaway");
            return s;
        }

        /// The kin's thought after being passed by.
        public static string ShunnedThought(string passer, string sick, string seed)
        {
            return Pick(Shunned, seed + "|sh").Replace("{a}", passer ?? "He").Replace("{s}", sick ?? "the little one");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Greets) yield return l.Replace("{a}", "Abe");
            foreach (string l in Asks) yield return l.Replace("{s}", "Mary");
            foreach (string[] a in Answers.Values) foreach (string l in a) yield return l;
            foreach (string l in Shunned) yield return l.Replace("{a}", "Abe").Replace("{s}", "Mary");
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
