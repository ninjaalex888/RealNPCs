// New faces arrive (the roadmap's idea bank, "Change over time"; [World] NewFaces). Newcomers already step down from the train and become
// residents of the town (SIXDAY item 11b; Engine.ArrivedByTrain): kin of someone here, or a stranger "from" somewhere. Now some of the
// strangers come as somebody: a settler with a claim, a schoolteacher sent for, a new preacher, or a mail-order bride promised to one of
// the town's unattached men (a courting tie between them from the day she steps down). The notice says who they are, and for their
// first two weeks their thoughts are a newcomer's. Why it reads true: towns in 1899 grew by the train; every arrival came with a reason
// and a hope, and the first weeks somewhere new are spent finding out whether the place is what the letters said.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class NewFaces
    {
        public const long FreshDays = 14;
        public const int ThoughtChance = 40;

        /// Who a stranger off the train comes as, or null for a plain newcomer.
        public static string KindOf(string seed, bool woman)
        {
            int roll = System.Math.Abs(Voice.Hash(seed + "|newface") % 100);
            if (woman)
                return roll < 20 ? "bride" : roll < 32 ? "schoolteacher" : roll < 60 ? "settler" : null;
            return roll < 5 ? "schoolteacher" : roll < 11 ? "preacher" : roll < 45 ? "settler" : null;
        }

        public static string TradeOf(string kind)
        {
            switch (kind)
            {
                case "schoolteacher": return "a schoolteacher";
                case "preacher": return "a preacher";
                case "settler": return "a settler";
                default: return null;
            }
        }

        public static string Notice(string kind, string town, string name, string groom, string from)
        {
            string first = Person.FirstOf(name) ?? name;
            switch (kind)
            {
                case "bride": return name + " came in on the train to " + town + " to marry " + (groom ?? "a man she knows only by his letters") + ".";
                case "schoolteacher": return town + " has a schoolteacher at last: " + name + ", off the train" + (from != null ? " from " + from : "") + ".";
                case "preacher": return "A new preacher stepped down in " + town + ": " + name + ". " + first + " means to fill the pews.";
                case "settler": return name + " stepped off the train in " + town + " with a claim to prove up" + (from != null ? ", all the way from " + from : "") + ".";
                default: return null;
            }
        }

        private static readonly Dictionary<string, string[]> Fresh = new Dictionary<string, string[]>
        {
            { "settler", new[] { "Paid every cent for that claim. Haven't seen it yet.", "Nobody here knows my name. That's the beauty of it.", "Land's cheap because it's hard. We'll see." } },
            { "schoolteacher", new[] { "Twelve children and one slate. Lord give me patience.", "The board wrote of a schoolhouse. It's a barn with a bell.", "They look at me as if I've come to take their children." } },
            { "preacher", new[] { "A town this size and the church half empty. Work to do.", "They say the last preacher left in the night. I wonder why.", "I've buried strangers. Here I'll bury neighbours." } },
            { "bride", new[] { "His letters said a house. It's a cabin.", "He's kinder than his letters. That's something.", "Two thousand miles on a promise. Now to see if it holds." } },
        };

        /// A newcomer's own thought in their first two weeks, or null.
        public static string Thought(string kind, long arrivedAt, long now, string seed)
        {
            string[] pool;
            if (kind == null || arrivedAt <= 0 || now < arrivedAt || now - arrivedAt > FreshDays * 1440 || !Fresh.TryGetValue(kind, out pool))
                return null;
            if (!Voice.Chance(seed + "|fresh", ThoughtChance))
                return null;
            return pool[System.Math.Abs(Voice.Hash(seed + "|freshl") % pool.Length)];
        }

        public static IEnumerable<string> AllLines()
        {
            return Fresh.Values.SelectMany(x => x);
        }
    }
}
