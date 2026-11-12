// The town's news passed over the fence (LIVES item 2e, 2026-11-12; [Scenes] NewsOverFence, settings version 471; the
// roadmap's "Talk at the pump" row). The street's gossip scenes passed on the stranger's deeds (Rumours); the quilting bee
// overheard the town's talk once a week by the church. What two women of a town didn't do was trade the town's own news when
// they met in the street: who quarrelled, who's walking out with whom, who made it up. Now, by day away from the saloon, two
// women of the same town who meet may pass on the freshest thing between two others of their town (the town's ties of the
// last two days, by name: "Did you hear Hollis and Abe Cobb were at it again outside the store?"), the hearer's answer by its
// kind, and the word is out: what's said at the pump is in every house by supper.
//
// Why it reads true: in an 1899 town without a paper of its own most news moved by word of mouth, and the women's daily rounds
// (the pump, the store, the clothesline) were its exchange (the historians of the frontier household, and Dunbar's view of
// gossip as the grooming that holds a group together: knowing who's at odds with whom is how a town keeps its peace).
//
// Cast from two already in the street near each other; it leaves them acquainted, the news itself is the town's ties already
// kept. Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class NewsOverFence
    {
        public const string Kind = "fencenews";
        public static bool On;   // [Scenes] NewsOverFence, set by the game
        public const int Strength = 37, Percent = 35;

        /// Two women of a town by day, with news of their town to pass (the game sets Place.TownNews): a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || !where.TownNews || where.Saloon || where.Night || x == null || y == null || x.Id == y.Id || !x.Woman || !y.Woman)
                return null;
            if (x.P == null || y.P == null || x.P.Home == null || x.P.Home != y.P.Home)
                return null;
            if (!Voice.Chance(Lives.Key(x, y) + "|" + gameDay + "|fence", Percent))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + " has news of the town for " + y.Name };
        }

        /// A tells B of two others of the town (news: their first names and the tie's kind and warmth); null news: the scene
        /// can't be made.
        public static Scene Make(SceneCast c, string seed)
        {
            if (c == null || c.News == null || c.News.Length < 3)
                return null;
            string p = c.News[0], q = c.News[1], kind = c.News[2];
            string[][] set = kind == "hostile" ? AtOdds : kind == "court" ? Courting : kind == "makeup" ? MadeUp : Kindness;
            string[] t = set[(Voice.Hash(seed + "|fn") & 0x7fffffff) % set.Length];
            var s = new Scene { Kind = Kind, Summary = "two women of the town passed on its news: " + p + " and " + q };
            s.Add(0, Fill(t[0], p, q), "mild", "face").Add(1, Fill(t[1], p, q), "mild").Add(0, Fill(t[2], p, q), "mild");
            return s;
        }

        /// The news kind of a tie: hostile, court, makeup or kind.
        public static string NewsKind(SceneTie t)
        {
            if (t == null)
                return null;
            return t.Warmth < 0 ? "hostile" : Ties.Romantic(t.Kind) ? "court" : t.Kind == "makeup" ? "makeup" : "kind";
        }

        private static string Fill(string l, string p, string q) { return l.Replace("{p}", p).Replace("{q}", q); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] set in new[] { AtOdds, Courting, MadeUp, Kindness })
                foreach (string[] t in set)
                    foreach (string l in t)
                        yield return Fill(l, "Hollis", "Abe");
        }

        private static readonly string[][] AtOdds = {
            new[] { "Did you hear {p} and {q} were at it again?", "Again? In front of the whole street?", "Outside the store, for anybody to see." },
            new[] { "{p} won't so much as look at {q} since Tuesday.", "Over what?", "Nobody knows. That's the worst of it." },
            new[] { "There's bad blood between {p} and {q}, and it's getting worse.", "Somebody ought to sit them down.", "Not me. I've my own to see to." } };
        private static readonly string[][] Courting = {
            new[] { "{p} was walking out with {q} on Sunday.", "Never!", "Bold as you like, past the church." },
            new[] { "I saw {q} smile at {p} by the hitching rail.", "A smile's not a ring.", "It's the start of one." },
            new[] { "{p} and {q}. Mark my words.", "By harvest?", "Before." } };
        private static readonly string[][] MadeUp = {
            new[] { "{p} and {q} have made it up.", "After all that?", "Shook hands in the street like it never happened." },
            new[] { "You'll never guess who's friends again. {p} and {q}.", "I'll believe it when I see it.", "You'll see it. They were laughing." } };
        private static readonly string[][] Kindness = {
            new[] { "{p} did {q} a good turn this week.", "Did they? I'd not have thought it.", "People surprise you." },
            new[] { "{p} sat with {q} a whole evening.", "Those two? Well.", "There's more to {p} than you'd think." } };
    }
}
