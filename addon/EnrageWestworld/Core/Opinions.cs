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

        // ---- opinions that act (FOURTEENHOUR item 23; [Mind] OpinionsAct). Until now a view was a thought and nothing more. Now it
        // decides things: fear keeps a man clear of the one he fears (the avoid reason) and out of any brawl he's in; contempt for one
        // fighter pulls an onlooker against him (BrawlSides' pull); respect draws two to walk on together (WalkTogether), contempt and
        // fear never; and what a man says of someone not there is his view of him, in words fit to pass on (the envious pass on the
        // worst), and his friend answers by his own. Why it reads true: an attitude is a readiness to act (Allport, 1935), and what
        // people think of each other shows first in whom they stand beside and what they say behind each other's backs.

        public static bool Act = true;   // [Mind] OpinionsAct, from the settings
        public const float ContemptPull = 0.10f;
        public const int TalkOfPercent = 40, TalkOfStrength = 38, RespectWalkPercent = 15;

        /// A view one holds of someone else, for the talk of him behind his back (the game side fills Someone.Views).
        public sealed class Held
        {
            public string Id, Name, View;
            public bool Woman;
        }

        /// How strongly a view wants passing on: contempt and envy most (the envious pass on the worst), respect least.
        public static int Weight(string view)
        {
            return view == "contempt" ? 4 : view == "envy" ? 3 : view == "fear" ? 2 : view == "respect" ? 1 : 0;
        }

        private static readonly Dictionary<string, string[]> Behind = new Dictionary<string, string[]>
        {
            { "contempt", new[] { "{who}'s at the bottle again, and not even noon.", "You know {who}? Thinks nobody sees what {he} gets up to. Everybody sees.", "{who}? I'd not cross the street to help {him}." } },
            { "envy", new[] { "Seen {who}'s new coat? Some of us work for ours.", "{who} never went hungry a day. It shows.", "What did {who} ever do to have all that?" } },
            { "fear", new[] { "Keep clear of {who}, if you've sense.", "Don't cross {who}. I did, once.", "If {who} comes in, I'm out the back." } },
            { "respect", new[] { "{who}'s a man of {his} word. There's few enough.", "When {who} speaks, I listen.", "{who} has never once done me wrong." } },
        };

        static string Fill(string line, string name, bool woman)
        {
            return line.Replace("{who}", name).Replace("{he}", woman ? "she" : "he").Replace("{him}", woman ? "her" : "him").Replace("{his}", woman ? "her" : "his")
                       .Replace("a man of her word", "a woman of her word");
        }

        /// What he says of someone not there, to a friend: his view, in words fit to pass on.
        public static string TalkOf(string view, string name, bool woman, string seed)
        {
            string[] pool;
            if (view == null || name == null || !Behind.TryGetValue(view, out pool))
                return null;
            return Fill(pool[(Voice.Hash(seed + "|talkof") & 0x7fffffff) % pool.Length], name, woman);
        }

        /// The friend's answer, by his own view of the one talked of: agrees, takes the other part, or doesn't know him.
        public static string AnswerOf(string told, string own, string name, string seed)
        {
            bool warmTold = told == "respect", warmOwn = own == "respect";
            if (own == null)
                return Voice.Pick(seed + "|ans0", "I wouldn't know.", "Is that so?", "Hm. I'll bear it in mind.");
            if (own == told || (!warmTold && !warmOwn))
                return Voice.Pick(seed + "|ans1", "You're not wrong.", "I've said as much myself.", "Ain't that so.");
            return Voice.Pick(seed + "|ans2", "That's not the " + name + " I know.", "You've got " + name + " wrong.", "Funny. " + name + "'s always been square with me.");
        }

        /// A tells B what he thinks of someone (c.TalkOf: his name, A's view, B's view or null, "w" for a woman).
        public static Scene TalkScene(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string z = c.TalkOf != null && c.TalkOf.Length > 1 ? c.TalkOf[0] : null, va = z != null ? c.TalkOf[1] : null;
            string vb = c.TalkOf != null && c.TalkOf.Length > 2 ? c.TalkOf[2] : null;
            bool woman = c.TalkOf != null && c.TalkOf.Length > 3 && c.TalkOf[3] == "w";
            Scene s = new Scene { Kind = "gossip", Summary = z != null ? a + " told " + b + " what they think of " + z : "two who know each other talked" };
            string said = TalkOf(va, z, woman, seed);
            if (said == null)
            {
                s.Add(0, Voice.Pick(seed + "|plain", "Keeping well?", "Weather holding, at least."), "mild", "face").Add(1, Voice.Pick(seed + "|plain2", "Can't complain.", "It is."), "mild");
                return s;
            }
            s.Add(0, said, va == "respect" ? "mild" : va == "fear" ? "scared" : "mock", "face")
             .Add(1, AnswerOf(va, vb, z, seed), vb == null ? "mild" : vb == va ? "yes" : "no");
            return s;
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
