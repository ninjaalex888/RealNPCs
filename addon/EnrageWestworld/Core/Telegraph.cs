// The telegraph clerk knows first (FIVEDAY reserve R15). The station agent works the key, so the news of other towns comes
// through his hands before it reaches anyone's mouth: the deaths of the last two days elsewhere, by name and town, and your
// own deeds in other towns whose word hasn't reached his town yet (no TownRumour of it here). He thinks of what he knows that
// the town doesn't, and of you when you walk in with it on the wire. A strong candidate among his thoughts while it's fresh
// (Engine.PostThought). Behind [Mind] TelegraphKnows.
//
// Why it reads true: information asymmetry is a small power, and the telegraph clerk of 1899 held it daily; his trade was
// discretion, and his temptation was to break it. Knowing the stranger's deed before the town does, and watching him walk
// in, is the clerk's whole drama. Plain C#, no game; all words original. mindtest TelegraphTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Telegraph
    {
        public const string Kind = "station";
        public const int Days = 2, Pull = 44;

        private static readonly string[] Deaths = {
            "Wire from {t}: {n} dead. Nobody here knows yet. I'm not paid to tell them.",
            "Took a death notice off the key for {t}. {n}. Wrote it out twice to get it right.",
            "{n}, of {t}. Gone. The wire doesn't say how. Wires never do." };
        private static readonly string[] YourDeeds = {
            "Wire from {t} about a stranger: {d}. And here he stands at my window.",
            "The {t} sheriff's wire came in an hour ago. {d}. He's buying a ticket like any man.",
            "I know what he did at {t}. The town will know by tomorrow. Tonight it's just me." };

        public static string DeathLine(string name, string town, string seed)
        {
            return Voice.Pick(seed + "|td", Deaths).Replace("{n}", Person.FirstOf(name) ?? "a man").Replace("{t}", town ?? "up the line");
        }

        public static string DeedLine(string what, string town, string seed)
        {
            return Voice.Pick(seed + "|tw", YourDeeds).Replace("{d}", what ?? "trouble").Replace("{t}", town ?? "up the line");
        }

        /// A deed in a few wire words (the clerk's shorthand).
        public static string Wire(string kind)
        {
            switch (kind)
            {
                case "kill": case "murder": return "SHOT DEAD STOP";
                case "lawkill": return "LAWMAN KILLED STOP";
                case "robbery": case "theft": return "ROBBERY STOP";
                case "assault": return "BEATING STOP";
                case "horsetheft": return "HORSE STOLEN STOP";
                default: return "TROUBLE STOP";
            }
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Deaths) yield return l.Replace("{n}", "Abel").Replace("{t}", "Emerald Ranch");
            foreach (string l in YourDeeds) yield return l.Replace("{d}", "LAWMAN KILLED STOP").Replace("{t}", "Emerald Ranch");
        }
    }
}
