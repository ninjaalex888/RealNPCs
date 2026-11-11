// Feuds in a day (DEPTH item 18; [Mind] FeudArcs, on). A slight in the morning (a shove, a snub, a debt called) is words by the afternoon,
// and by evening either blows or a making-up in front of witnesses. The stages are the feud's own (Reasons: the tie's Stage, a quarrel
// picking up again every HostileRest game minutes, blows at the second stage for the hot), which already run in hours; what this adds
// is the repair attempt where blows would come: the one who did it (the tie's Doer), if his nature allows (the kind and the loyal, a
// plain man now and then; never the proud or a hothead while others watch), offers his hand where the street can see. The other takes
// it or not by his own nature (the kind and the trusting take it; the proud and the hot refuse it). Taken: a making-up before the
// street (an "apology" tie, a step warmer, and the onlookers see two men shake hands); refused: blows on the spot. Why it reads true:
// repair attempts decide whether a quarrel ends or grows (Gottman), and an audience makes backing down costly and making up public
// (face). Plain C#; the Director plays it.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class FeudArcs
    {
        public static bool On;   // [Mind] FeudArcs (on as it comes), set by the game; off in Core for the old harnesses
        public const string Kind = "handoffer";
        public const int Strength = 76;

        /// Whether the one who did it offers his hand where blows would come (a seeded roll by his nature): the kind and the loyal
        /// most, a plain man now and then, the cold and the grasping rarely, never the proud or a hothead in front of others.
        public static bool Offers(string temper, bool watched, string seed)
        {
            if (watched && (temper == "Proud" || temper == "Hothead"))
                return false;
            int pct = temper == "Loyal" ? 70 : temper == "Coward" ? 55 : temper == "Cold" || temper == "Greedy" ? 15 : temper == "Proud" || temper == "Hothead" ? 10 : 40;
            return Voice.Chance(seed + "|offers", pct);
        }

        /// Whether the other takes it: the kind and the trusting take it, the proud and the hot are slow to.
        public static bool Takes(string temper, string seed)
        {
            int pct = temper == "Loyal" ? 85 : temper == "Coward" ? 75 : temper == "Proud" ? 30 : temper == "Hothead" ? 25 : temper == "Cold" || temper == "Greedy" ? 45 : 60;
            return Voice.Chance(seed + "|takes", pct);
        }

        private static readonly string[] Offer = { "{b}. I was wrong this morning, and I'll say so in front of all these. My hand.", "Let's not do this, {b}. I'm sorry for what I did. Shake?",
                                                   "I'll own it, {b}. It was my fault. Here's my hand on it." };
        private static readonly string[] Take = { "...All right. Your hand, then.", "I'll take it. And we'll say no more.", "That's handsome of you. Done." };
        private static readonly string[] Refuse = { "Keep your hand. I'll have satisfaction.", "Sorry won't cover it.", "Put that hand away. I want none of it." };

        /// A the one who did it, B the one it was done to. Taken: kind "apology" (both go); refused: blows ("brawl", Fight).
        public static Scene Make(SceneCast c, string bTemper, string seed, out bool taken)
        {
            string b = Scenes.N(c, 1, seed);
            taken = Takes(bTemper, seed);
            var s = new Scene { Kind = taken ? "apology" : "brawl", Doer = 0, Fight = !taken, NoLadder = true,
                                Summary = taken ? "the man who began a quarrel that morning offered his hand before the street, and it was taken" : "the man who began a quarrel offered his hand before the street, and it was refused: blows" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Pick(Offer, seed + "|of").Replace("{b}", b), "sorry", "face");
            if (taken)
                s.Add(1, Pick(Take, seed + "|tk"), "mild", "face").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
            else
                s.Add(1, Pick(Refuse, seed + "|rf"), "angry", "shove").Add(1, null, "angry", "fight", 9000).Add(1, null, null, "endfight");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Offer) yield return l.Replace("{b}", "Abe");
            foreach (string[] a in new[] { Take, Refuse }) foreach (string l in a) yield return l;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
