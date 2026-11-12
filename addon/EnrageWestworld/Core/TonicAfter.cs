// The tonic and the morning after (LIVES item 2g, 2026-11-12; [Scenes] TonicAfter, settings version 473; the roadmap's "The
// tonic and the sceptic" row). The street's medicine show (Scenes.Pitch, kind "pitch") already had its sceptic, who smells
// turpentine and, half the time, buys a bottle anyway, "for my horse". Now the man who bought is remembered (in memory, by
// id, for three game days), and in the days after, meeting anyone by day, he tells how it went: sick as a dog since Sunday
// ("You were right about the turpentine."), or the horse that won't look at him the same, and the other has his laugh.
//
// Why it reads true: the patent medicine trade of the 1890s sold mostly alcohol, opium and bitters, and every town had its
// story of the bottle that did something nobody wanted; a sceptic who buys anyway is human (the foot-in-the-door of a good
// pitch), and the story he tells after is how he squares it with himself: a joke at his own expense keeps his standing.
// Cast from two already near each other; nothing in the save. Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class TonicAfter
    {
        public const string Kind = "tonicafter";
        public static bool On;   // [Scenes] TonicAfter, set by the game
        public const int Strength = 38, Percent = 45, Days = 3;

        /// Who bought a bottle off the medicine show, and when (game minutes); in memory only.
        private static readonly Dictionary<string, long> Bought = new Dictionary<string, long>();

        /// The pitch played and its sceptic bought ("buy" on its last line): kept for the days after.
        public static void Note(string buyerId, long now)
        {
            if (!On || buyerId == null)
                return;
            Bought[buyerId] = now;
            if (Bought.Count > 64)
            {
                var old = new List<string>();
                foreach (var kv in Bought)
                    if (now - kv.Value > Days * 1440L)
                        old.Add(kv.Key);
                foreach (string k in old)
                    Bought.Remove(k);
            }
        }

        public static void Clear() { Bought.Clear(); }

        /// Whether this one bought a bottle in the last three game days, not the same day.
        public static bool Took(string id, long now)
        {
            long at;
            return id != null && Bought.TryGetValue(id, out at) && now - at >= 600 && now - at <= Days * 1440L;
        }

        /// x bought, y is anyone: the telling, by day, or null.
        public static Reason For(Someone x, Someone y, long gameDay, long now, Reasons.Place where)
        {
            if (!On || where == null || where.Night || x == null || y == null || x.Id == y.Id || !Took(x.Id, now))
                return null;
            if (!Voice.Chance(x.Id + "|" + y.Id + "|" + gameDay + "|tonic", Percent))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + " tells " + y.Name + " what the medicine show's bottle did" };
        }

        /// A bought, B hears. One telling in three is the horse's.
        public static Scene Make(SceneCast c, string seed)
        {
            bool horse = Voice.Chance(seed + "|horse", 33);
            string[] t = Pick(horse ? Horse : Sick, seed + "|ta");
            var s = new Scene { Kind = Kind, Summary = horse ? "a man told how the medicine show's tonic took his horse" : "a man told how the medicine show's tonic took him" };
            s.Add(0, t[0], "mild", "face").Add(1, t[1], "mock").Add(0, t[2], "mild");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] set in new[] { Sick, Horse })
                foreach (string[] t in set)
                    foreach (string l in t)
                        yield return l;
        }

        private static readonly string[][] Sick = {
            new[] { "Took a spoon of that tonic off the medicine show. Sick as a dog since.", "Turpentine, I heard somebody say.", "Somebody was right." },
            new[] { "That elixir. Two swallows and I saw my grandfather. He's been dead ten years.", "Did he say anything?", "Told me to go easy on it." },
            new[] { "Want to buy a bottle of miracle tonic? Nearly full.", "Did it cure you?", "Cured me of buying tonic." } };
        private static readonly string[][] Horse = {
            new[] { "Gave the horse a dose of that tonic, like I said I would.", "And?", "He won't look at me the same. Can't say I blame him." },
            new[] { "Horse drank the whole bottle off the medicine show.", "How is he?", "Faster than he's ever been. In the wrong direction." } };

        private static string[] Pick(string[][] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
