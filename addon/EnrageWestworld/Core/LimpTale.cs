// How he got the limp (DEPTH item 13; the roadmap's "Limps and scars", whose half was FIVEDAY item 24's lasting marks: the man you beat
// carries it for days and a friend names you for it; with [Mind] LastingMarks). The man you beat, still marked, tells a stranger in the
// saloon how he came by it, and the telling grows with the days, as FOURTEENHOUR item 24's tales do: a lucky blow, then two of them
// from behind, then three and a knife, and never once his fault. A friend of his beside him (the Director's third, already near) sets
// it straight, or backs him, by his nature: the loyal back him, the cold and the plain-spoken don't. Why it reads true: self-serving
// memory (the beaten remember a fairer fight than they had, and a bigger one), and Bartlett's bending toward what the teller needs
// the story to be. Plain C#; the game side sets Someone.MarkedDays (Heart) and the friend (PlayPairReason).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class LimpTale
    {
        public const string Kind = "limptale";
        public static bool On = true;   // with [Mind] LastingMarks: the game side sets MarkedDays only with it on
        public const int Strength = 40, Percent = 35;

        /// x, still marked from a beating of yours, and y, a stranger to him (no tie), in the saloon: a reason, or null.
        public static Reason For(Someone x, Someone y, SceneTie tie, long gameDay, bool saloon)
        {
            if (!On || !saloon || x == null || y == null || x.MarkedDays < 0 || x.Woman || tie != null || x.Id == y.Id)
                return null;
            if (!Voice.Chance(x.Id + "|" + y.Id + "|" + gameDay + "|limp", Percent))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + ", still marked, tells " + y.Name + " how he came by it" };
        }

        // the telling, by the days since (0: a lucky blow; 1: two of them; 2+: three and a knife)
        private static readonly string[][] Tells =
        {
            new[] { "A stranger got a lucky one in, is all. Caught me looking the other way.", "Some drifter swung on me when I wasn't set. Wouldn't have happened twice." },
            new[] { "Two of them came at me from behind the livery. I gave as good as I got.", "There were two of them, and I'd have had the both if the second hadn't gone low." },
            new[] { "Three of them, and one had a knife. I'm lucky to be standing.", "Took three men to put me down, and one with a blade. Ask anybody." },
        };
        private static readonly string[] NotMe = { "I never threw the first. Never.", "Minding my own business, I was.", "Wasn't my doing, any of it." };
        private static readonly string[] Listens = { "You don't say.", "That so? Hard luck.", "Three men. Well, well.", "Is that a fact." };
        private static readonly string[] Straight = { "It was one man, {a}. And you swung first.", "One stranger, {a}, and he never broke a sweat.", "That's not how I saw it, {a}, and I was there." };
        private static readonly string[] Backs = { "That's how it was. I saw it.", "Every word true. I was there.", "Three of them. I'd not have stood it." };
        private static readonly string[] Caught = { "Shut your mouth, {c}.", "Whose side are you on?", "You'd better keep that to yourself." };

        /// Whether his friend sets it straight: the loyal back him (one in seven sets it straight), the cold and the drifter set it
        /// straight two times in three, the rest about half.
        public static bool SetsStraight(string friendTemper, string seed)
        {
            int pct = friendTemper == "Loyal" ? 15 : friendTemper == "Cold" || friendTemper == "Drifter" ? 65 : friendTemper == "Proud" || friendTemper == "Hothead" ? 35 : 50;
            return Voice.Chance(seed + "|straight", pct);
        }

        /// The scene: A the marked man, B the stranger, C (with Cast 3) his friend. days: since the beating.
        public static Scene Make(SceneCast c, int days, string seed)
        {
            string a = c != null && c.Names != null && c.Names[0] != null ? Person.FirstOf(c.Names[0]) : "Hank";
            string cName = c != null && c.Names != null && c.Names[2] != null ? Person.FirstOf(c.Names[2]) : "Ed";
            int stage = days <= 0 ? 0 : days == 1 ? 1 : 2;
            var s = new Scene { Kind = Kind, Doer = 0, Summary = "a man still marked from a beating told a stranger how he came by it, bigger than it was" };
            s.Add(0, Pick(Tells[stage], seed + "|t"), "mild", "face")
             .Add(0, Pick(NotMe, seed + "|n"), "mild")
             .Add(1, Pick(Listens, seed + "|l"), "mild");
            bool friend = c != null && c.FriendThere;
            if (friend)
            {
                bool straight = SetsStraight(c.Tempers != null ? c.Tempers[2] : null, seed);
                s.Beats.Add(new Beat(2, Pick(straight ? Straight : Backs, seed + "|f").Replace("{a}", a), straight ? "mild" : "thanks") { To = 0 });
                if (straight)
                    s.Add(0, Pick(Caught, seed + "|c").Replace("{c}", cName), "angry", "face");
                s.Cast = 3;
                s.Summary += straight ? "; his friend set it straight" : "; his friend backed him";
            }
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in Tells) foreach (string l in a) yield return l;
            foreach (string[] a in new[] { NotMe, Listens, Backs }) foreach (string l in a) yield return l;
            foreach (string l in Straight) yield return l.Replace("{a}", "Hank");
            foreach (string l in Caught) yield return l.Replace("{c}", "Ed");
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
