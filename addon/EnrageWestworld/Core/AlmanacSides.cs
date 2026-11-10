// Sides on the year's quarrels (Lex, 2026-11-09: more depth to 1899's inner thoughts; [World] AlmanacSides). The almanac gave
// everyone who thought of a story the same two lines. But 1899 split on its news: the war in the Philippines (expansion and trade, or
// an empire no republic should keep), the trusts, gold against silver, the dry towns, votes for women, the Boers. Here, for the stories
// the country argued over, what a farmer, a working man, the well-off, a soldier and a woman were apt to think, six times in ten when
// the thinker has a side on it (their trade, or for the vote and the drink, being a woman). The rest of the time, the story's own
// lines. The sides are the period's; the wording is this add-on's. The history notes (Core/HistoryNotes.cs) know these lines too.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class AlmanacSides
    {
        public const int Chance = 60;
        public static bool On = true;   // [World] AlmanacSides, from the settings (WestworldScript.ApplySettings)

        private static readonly Dictionary<string, Dictionary<string, string[]>> Sides = new Dictionary<string, Dictionary<string, string[]>>
        {
            { "manilafight", new Dictionary<string, string[]> {
                { "gentry", new[] { "The Philippines will pay for themselves in trade. Manila's the door to China, the bankers say." } },
                { "working", new[] { "Our boys dying in the Philippines so a sugar trust can have its islands. Not my war." } },
                { "farm", new[] { "My cousin's boy is in the Philippines. Seven thousand miles to fight men who never heard of us." } },
                { "soldier", new[] { "The Philippines. Jungle, fever and men you can't see. Spain was the easy part." } },
                { "woman", new[] { "Mothers in every town read the Philippines lists. Mine's not on it. This week." } } } },
            { "thetrusts", new Dictionary<string, string[]> {
                { "farm", new[] { "The trusts set the price of my twine and the price of my wheat. I lose at both ends." } },
                { "working", new[] { "The trusts cut wages and call it good business. A man can't strike against all of it." } },
                { "gentry", new[] { "Folk cuss the trusts, but the trusts built the mills. Somebody has to make the country run." } } } },
            { "goldsilver", new Dictionary<string, string[]> {
                { "farm", new[] { "Free silver would let a farmer pay his debts. Gold's a banker's metal and a farmer's noose." } },
                { "gentry", new[] { "Gold behind every dollar. Bryan's free silver would have ruined honest savers." } },
                { "working", new[] { "Gold or silver, my wages come in whichever is cheaper that week." } } } },
            { "womenvote", new Dictionary<string, string[]> {
                { "woman", new[] { "Wyoming's women have voted since '69 and the sky hasn't fallen. Why not here?" } },
                { "gentry", new[] { "Votes for women. Next they'll sit on juries. Wyoming can keep its notions." } } } },
            { "drytowns", new Dictionary<string, string[]> {
                { "woman", new[] { "Every dry town is a home where Saturday's wages come home. Let them all go dry." } },
                { "working", new[] { "Dry towns. Twelve hours at the mill and a man can't buy himself one glass after." } } } },
            { "antiimperialists", new Dictionary<string, string[]> {
                { "gentry", new[] { "Carnegie against the islands, and him richer than the islands. The anti-imperialists are odd company." } },
                { "working", new[] { "The anti-imperialists are right for once. No empire ever fed a working man." } } } },
            { "boerwar", new Dictionary<string, string[]> {
                { "farm", new[] { "The Boers are farmers fighting the whole British Empire. I know whose side I'm on." } },
                { "gentry", new[] { "The British will have the Transvaal. Gold mines don't stay long with farmers." } } } },
        };

        /// The side this person has on the year's quarrels, for this story: being a woman, for the stories women argued as women; else their trade.
        public static string SideOf(string key, Person p)
        {
            Dictionary<string, string[]> sides;
            if (!On || p == null || key == null || !Sides.TryGetValue(key, out sides))
                return null;
            if (p.Woman && sides.ContainsKey("woman"))
                return "woman";
            string g = InnerLife.TradeGroup(p.Trade);
            return sides.ContainsKey(g) ? g : null;
        }

        /// Their side's lines on this story, or null.
        public static string[] For(string key, Person p)
        {
            string side = SideOf(key, p);
            return side == null ? null : Sides[key][side];
        }

        public static IEnumerable<KeyValuePair<string, string>> LinesByKey()
        {
            foreach (var s in Sides)
                foreach (var side in s.Value)
                    foreach (string l in side.Value)
                        yield return new KeyValuePair<string, string>(s.Key, l);
        }

        public static IEnumerable<string> Keys()
        {
            return Sides.Keys;
        }

        public static IEnumerable<string> AllLines()
        {
            return LinesByKey().Select(kv => kv.Value);
        }
    }
}
