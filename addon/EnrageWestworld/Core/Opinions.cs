// Opinions of each other (the roadmap's idea bank, "The social web"; [Ticker] Opinions). A tie between two people has a warmth and a
// history (Core/Ties.cs): who did what to whom, the last scene between them, how often they've met. What it hadn't was a view: the
// standing thing one person thinks of another. Read off what's already known, no new state: fear of one who hurt you; contempt for one
// you're sore at who drinks, robbed someone, or stands below you; envy of one who's better off, if you're sore or cool at them and
// of a grasping or proud temper; respect for one you're warm to who stands above you, met you fairly with fists, or has been about a
// long while. A view is one-sided: the man who's feared may not know it. It shows first where two who know each other stand
// in view (Core/PairThoughts.cs): seven times in ten a view has the thought. Why it reads true: people carry a verdict on each other
// that outlives the quarrel that made it, and it's rarely the same verdict in both directions.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Opinions
    {
        public const int ShownChance = 70;

        /// This one's view of the other in the tie, or null.
        public static string View(SceneTie t, string id, string temper, bool iGentry, bool otherGentry, bool otherDrunk)
        {
            if (t == null || id == null || !t.Has(id))
                return null;
            string other = t.OtherId(id);
            string group = Minds.TemperGroup(temper);
            bool hurtMe = t.Doer != null && t.Doer == other;   // the tie keeps who did it to whom (a beating, a theft, a drunk who went for them)
            bool robbedSomeone = t.CrimeDoer != null && t.CrimeDoer == other;
            if (hurtMe && (group == "fearful" || t.Warmth <= -2))
                return "fear";
            if (t.Warmth <= -1 && (otherDrunk || robbedSomeone || (iGentry && !otherGentry)))
                return "contempt";
            if (otherGentry && !iGentry && t.Warmth <= 0 && (temper == "Greedy" || temper == "Proud" || group == "cold"))
                return "envy";
            if (t.Warmth >= 1 && (otherGentry || t.Kind == "bout" || t.Count >= 5))
                return "respect";
            return null;
        }

        private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            { "respect", new[] { "{who} built something here. Hard not to admire it.", "When {who} speaks, I listen. Most don't earn that.", "{who} has never once done me wrong.", "There's a man worth his word. {who}." } },
            { "contempt", new[] { "Look at {who}. Not even noon.", "{who} thinks nobody sees. Everybody sees.", "I wouldn't spit on {who} if {who} was on fire.", "Some folk are born low. {who} worked at it." } },
            { "envy", new[] { "New coat on {who}. Some of us work for ours.", "{who} never went hungry a day. You can tell.", "What did {who} ever do to get all that?", "Some are born with a silver spoon. {who} was born with the drawer." } },
            { "fear", new[] { "Don't catch {who}'s eye. Not after last time.", "Keep your head down. {who}'s here.", "My hands still remember {who}.", "If {who} comes this way, I'm going." } },
        };

        public static string Line(string view, string otherFirstName, string seed)
        {
            string[] pool;
            if (view == null || otherFirstName == null || !Lines.TryGetValue(view, out pool))
                return null;
            return pool[System.Math.Abs(Voice.Hash(seed + "|op") % pool.Length)].Replace("{who}", otherFirstName);
        }

        public static IEnumerable<string> Views()
        {
            return Lines.Keys;
        }

        public static IEnumerable<string> AllLines()
        {
            return Lines.Values.SelectMany(x => x).Select(l => l.Replace("{who}", "Silas"));
        }
    }
}
