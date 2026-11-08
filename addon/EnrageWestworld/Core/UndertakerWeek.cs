// The undertaker's thoughts follow the week's deaths (FIVEDAY reserve R10; the body's carry to his door is Core/Undertaker.cs).
// The man at the undertaker's counter (Posts: the "undertaker" kind, Rhodes and Armadillo) thinks of his town's dead of the
// last seven days, by name and by how they died: the fever, the mine, the rope, a gun, his own hand, or you. A busy week is a
// thought of its own, and when you've been the cause twice or more, he thinks of you as trade. A strong pull among his
// thoughts for the week (the counter's own candidate, Engine.PostThought); in a quiet week, his trade's thoughts as before.
// Behind [Mind] UndertakerKnows.
//
// Why it reads true: the undertaker is the one person in town who handles every death, and so the one who keeps the town's
// real count; his gallows humour is the defence of a man who has to measure his neighbours. Plain C#, no game; all words
// original. mindtest UndertakerWeekTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class UndertakerWeek
    {
        public const string Kind = "undertaker";
        public const int Days = 7, Busy = 3, Pull = 45;

        /// How a death reads at his table: by the cause (How) and the hand (By).
        public static string Cause(DeathEntry d)
        {
            if (d == null)
                return "unknown";
            if (d.How == "the fever") return "fever";
            if (d.How == "the mine") return "mine";
            if (d.How == "the rope") return "rope";
            if (d.By == "player") return "you";
            if (d.By == "self") return "self";
            return d.How != null ? "gun" : "unknown";
        }

        private static readonly Dictionary<string, string[]> ByCause = new Dictionary<string, string[]>
        {
            { "fever", new[] { "{n}. The fever. Closed box, and lime, and the family mustn't watch.", "Sealed {n} in quick. Fever bodies don't wait for the preacher." } },
            { "mine", new[] { "What the mine sent up of {n} goes in a small box. Nobody needs to know how small.", "{n}'s widow wants oak. The company pays for pine." } },
            { "rope", new[] { "{n}'s neck, and the knot's mark. High collar, and nobody will see it.", "Hanged men are heavy. {n} was heavy." } },
            { "gun", new[] { "{n}. Two holes, one in, one out. Wax for the one that shows.", "Dug the lead out of {n} for the sheriff. Sheriff didn't thank me." } },
            { "self", new[] { "{n} did it himself. The family wants it called an accident, and so do I.", "I'll not charge {n}'s widow. Some things a man doesn't charge for." } },
            { "you", new[] { "That stranger sent me {n}. Neat work. He'll send me more.", "{n} on my table, and the man who did it walking the street. Business." } },
            { "unknown", new[] { "{n}. Nobody can say what happened. The body says enough.", "Measured {n} this morning. Six foot and a half inch. A good box." } },
        };
        private static readonly string[] BusyWeek = { "Four boxes this week. The lumberman thinks I'm building a house.", "A busy week. I've planed pine till my arms gave out." };
        private static readonly string[] YouTrade = { "Every time that stranger rides in, I order pine.", "I should give that stranger a cut. He keeps me fed." };

        /// His thought of the week's dead (newest first), or null in a quiet week. dead: deaths in his town within Days.
        public static string Thought(IList<DeathEntry> dead, string seed)
        {
            if (dead == null || dead.Count == 0)
                return null;
            if (dead.Count >= Busy && Voice.Chance(seed + "|busy", 35))
                return Voice.Pick(seed + "|ub", BusyWeek);
            if (dead.Count(d => d.By == "player") >= 2 && Voice.Chance(seed + "|yt", 40))
                return Voice.Pick(seed + "|uy", YouTrade);
            DeathEntry pick = dead[(int)((Voice.Hash(seed + "|ud") & 0x7fffffff) % dead.Count)];
            return Voice.Pick(seed + "|uc", ByCause[Cause(pick)]).Replace("{n}", Person.FirstOf(pick.Name) ?? "the man");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in ByCause.Values) foreach (string l in p) yield return l.Replace("{n}", "Abel");
            foreach (string[] p in new[] { BusyWeek, YouTrade }) foreach (string l in p) yield return l;
        }
    }
}
