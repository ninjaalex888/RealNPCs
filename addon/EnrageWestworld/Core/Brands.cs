// Doctored brands (DEPTH item 8; the roadmap's "Doctored brands", whose half so far was the road table's rustlers at a branding
// fire, FIVEDAY2). A ranch man of the town, stock gone off his range with the brands run over, blames his neighbour to his face in
// the street by day (a Director street scene, kind "blamed": the accusation, the denial, his last word, the street hearing it), and
// a hostile "blamed" tie lies between them (Effect -2, the accuser's doing), which the quarrels and fights the ties drive can take
// further. If you came on the rustlers at their fire in the last two game days, J while it plays: tell him who really did it, and
// the rest of the scene is his apology and the neighbour's thanks (a "cleared" tie, +2, so they stand where they did before).
// Why it reads true: suspicion falls on the nearest plausible man (the availability heuristic), and on the range that was the man
// over the fence; and the only one who could clear him is a stranger nobody thought to ask. Plain C#; the game side is
// Game/WestworldScript.Brands.cs. Behind [Scenes] Brands.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Brands
    {
        public const string Kind = "blamed", TruthKind = "brandtruth";
        // off in Core so the old harnesses' goldens stand; the game (ApplySettings) and the commit gate's measure set it from
        // [Scenes] Brands, which is on as it comes
        public static bool On;
        public const int Percent = 2, Strength = 44, SawWithinMinutes = 2 * 1440;
        public const float NearMetres = 14f;
        public static readonly string[] Towns = { "Valentine", "Strawberry", "Rhodes", "Emerald Ranch", "Blackwater", "Armadillo", "Tumbleweed" };

        public static bool Ranch(string trade)
        {
            return trade != null && (trade.Contains("ranch") || trade.Contains("cow") || trade.Contains("drover") || trade.Contains("cattle") || trade.Contains("wrangler"));
        }

        /// x, a ranch man of a ranch town, and y, another man of his town, in the street by day: a roll a pair a day, else null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, bool night)
        {
            if (!On || saloon || night || x == null || y == null || x.Woman || y.Woman || x.P == null || y.P == null || x.Id == y.Id)
                return null;
            if (!Ranch(x.P.Trade) || x.P.Home == null || x.P.Home != y.P.Home || System.Array.IndexOf(Towns, x.P.Home) < 0)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|brands", Ranch(y.P.Trade) ? Percent * 2 : Percent))
                return null;   // the man over the fence is the likelier to be blamed: a ranch neighbour twice as often
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + " is missing stock, and blames " + y.Name + " for the brands run over" };
        }

        private static readonly string[] Accuse =
        {
            "Six head gone off my north range, {b}, and the brands run over. I know whose iron did that.",
            "My steers are wearing a brand I never put on them, {b}. Want to tell me how?",
            "I found my calves in your pen with my mark burned crooked, {b}. Explain it.",
        };
        private static readonly Dictionary<string, string[]> Denies = new Dictionary<string, string[]>
        {
            { "bold", new[] { "Say that again and you'll need a doctor.", "You calling me a thief? In the street?", "I never touched your beef, and I'll not stand here and hear it." } },
            { "fearful", new[] { "It wasn't me, I swear it on my mother.", "I'd never. You know I'd never.", "Brands? I've no iron but my own." } },
            { "cold", new[] { "Prove it.", "Bring the sheriff, then. I'll wait.", "Think what you like." } },
            { "plain", new[] { "I've been your neighbour eleven years. You know me better than that.", "I lost two head myself this month.", "Look somewhere else. I'm no rustler." } },
        };
        private static readonly string[] Last = { "I am riding for the sheriff.", "You've been warned, {b}.", "I'll be watching your pens." };
        private static readonly string[] YouTell =
        {
            "It wasn't him. I saw men at a branding fire out past the ridge, two nights back.",
            "You've the wrong man. There's a crew running irons in the hills. I came on their fire.",
        };
        private static readonly string[] Sorry = { "I spoke out of turn, {b}. I am sorry for it.", "Rustlers. And here I was. I'm sorry, {b}.", "I was a fool. Your hand, {b}?" };
        private static readonly string[] Thanks = { "Obliged to you, mister. I'd have had that hanging on me.", "Thank you. Nobody else was going to speak for me.", "I'll not forget that, stranger." };

        public static Scene Make(SceneCast c, string seed)
        {
            string b = Scenes.N(c, 1, seed);
            string temper = c != null && c.Tempers != null ? c.Tempers[1] : null;
            string[] deny;
            if (!Denies.TryGetValue(Minds.TemperGroup(temper) ?? "", out deny))
                deny = Denies["plain"];
            var s = new Scene { Kind = Kind, Doer = 0, Summary = "a ranch man blamed his neighbour in the street for stock gone with the brands run over" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Pick(Accuse, seed + "|ac").Replace("{b}", b), "angry", "face")
             .Add(1, Pick(deny, seed + "|dn"), Minds.TemperGroup(temper) == "bold" ? "angry" : "mild", "face")
             .Add(0, Pick(Last, seed + "|ls").Replace("{b}", b), "angry", "walkaway")
             .Add(1, null, null, "walkaway");
            return s;
        }

        /// You tell them what you saw: his apology, the neighbour's thanks to you, and they go.
        public static Scene Truth(SceneCast c, string seed)
        {
            string b = Scenes.N(c, 1, seed);
            var s = new Scene { Kind = TruthKind, Summary = "a stranger told a ranch man the rustlers were someone else, and he made it right with his neighbour" };
            s.Add(0, Pick(Sorry, seed + "|so").Replace("{b}", b), "mild", "face")
             .Add(1, Pick(Thanks, seed + "|th"), "thanks", "faceplayer")
             .Add(0, null, null, "walkaway")
             .Add(1, null, null, "walkaway");
            return s;
        }

        public static string YourWord(string seed) { return Pick(YouTell, seed + "|you"); }

        public static List<InteractOption> Rows(string accuser)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "brands:tell", Label = "Tell " + (accuser ?? "him") + " about the branding fire you saw." },
                new InteractOption { Id = "brands:no", Label = "Say nothing." },
            };
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Accuse, Last, Sorry })
                foreach (string l in a)
                    yield return l.Replace("{b}", "Tully");
            foreach (string[] a in new[] { YouTell, Thanks })
                foreach (string l in a)
                    yield return l;
            foreach (string[] a in Denies.Values)
                foreach (string l in a)
                    yield return l;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
