// A quilting bee overheard through the window (FIVEDAY reserve R8). On Tuesday and Thursday afternoons the women of the
// town sit at the frame in the church hall, and the window is open. Stand still on foot by the church steps for a few
// seconds and you hear them for half a minute: the needles and the kettle, then the town's own business, by name, as they
// see it: who quarrelled with whom, who's courting, who was robbed, who came to blows (the store's ties of the last three
// days, as Ties.TownThought tells them), the women's own talk between, and, when the town has an opinion of you, a word
// about you that they don't know you can hear. Once a town a day. Interiors are out of reach, so it's heard, not seen: the
// ticker, as "Through the window". Behind [Scenes] QuiltingBee.
//
// Why it reads true: the quilting bee was where the women of a small town held the town's social knowledge, and gossip is
// how a community polices its norms (reputation, who can be trusted). Overhearing it is the plainest way to learn what a
// town really thinks, including of you: the speakers are candid because they don't know they're heard. Plain C#, no game;
// all words original. mindtest QuiltingBeeTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class QuiltingBee
    {
        public const int FromMin = 13 * 60, ToMin = 16 * 60 + 30;
        public const float NearMetres = 25f;
        public const int StillMs = 6000, LineMs = 7000, MostTies = 2;

        public static bool Now(int weekday, int minuteOfDay) { return (weekday == 2 || weekday == 4) && minuteOfDay >= FromMin && minuteOfDay < ToMin; }

        private static readonly string[] Openings = { "[through the open window: needles, a kettle, five women talking at once]", "[the church hall window is open: the quilting frame, and the town being taken apart]" };
        private static readonly string[] Between = {
            "Pass the blue thread, would you. No, the other blue.", "That's a crooked seam, Martha, and you know it is.",
            "I'll say no more. Only that I saw it with my own eyes.", "Oh, hush with your eyebrows. I only repeat what I'm told.",
            "This square's for the Hollis baby. Lord knows the child will need the warmth.", "More tea? It's stewed, but it's hot." };
        private static readonly string[] OfYouFeared = {
            "That stranger's in town again. I keep my door barred when he is.", "They say he's killed men. Hush. Somebody's at the window.",
            "My husband says not to look at him. So I don't. Mostly." };
        private static readonly string[] OfYouKind = {
            "The stranger paid Ezra's slate, I heard. Well. There's good in some.", "He sat with the Pikes when their boy was sick. Say what you like.",
            "A kind man, for all he looks like trouble." };
        private static readonly string[] OfYouNothing = {
            "Who is that man by the church? Does anybody know him?", "A stranger, standing about. Tell your daughters to stay in." };
        private static readonly string[] Fillers = {
            "Lord, the price of flour this month.", "If it rains Sunday the picnic's off, and I've baked for it.", "Nine stitches to the inch, girls. Nine." };

        public static string Opening(string seed) { return Voice.Pick(seed + "|qo", Openings); }
        public static string Talk(string seed) { return Voice.Pick(seed + "|qb", Between); }
        public static string Filler(string seed) { return Voice.Pick(seed + "|qf", Fillers); }

        /// A word about you, by what the town knows: killings known (feared), kindnesses (warm), or neither (a stranger).
        public static string OfYou(int killsKnown, int kindnesses, string seed)
        {
            return Voice.Pick(seed + "|qy", killsKnown > kindnesses ? OfYouFeared : kindnesses > 0 ? OfYouKind : OfYouNothing);
        }

        /// The half minute's lines, in order: the opening, the gossip (each a tie's town thought), the women's talk between,
        /// the word of you. ties: TownThought lines already made (may be empty).
        public static List<string> Lines(IList<string> ties, int killsKnown, int kindnesses, string seed)
        {
            var l = new List<string> { Opening(seed) };
            for (int i = 0; i < ties.Count && i < MostTies; i++)
            {
                l.Add(ties[i]);
                l.Add(Talk(seed + i));
            }
            if (ties.Count == 0)
            {
                l.Add(Filler(seed));
                l.Add(Talk(seed + "f"));
            }
            l.Add(OfYou(killsKnown, kindnesses, seed));
            return l;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Openings, Between, OfYouFeared, OfYouKind, OfYouNothing, Fillers }) foreach (string l in p) yield return l;
        }
    }
}
