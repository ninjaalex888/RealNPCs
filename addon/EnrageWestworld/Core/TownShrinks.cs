// A town that shrinks says so (LIVES item 5b, 2026-11-12; [World] TownShrinks, settings version 481; the roadmap's "Towns that
// shrink" row). The town ledger (Core/TownLedgers.cs) already moved an index with every death, departure and arrival, and
// fewer regulars walked a town whose index fell; its mood (grieving, fearful) had thoughts. What a shrinking town didn't have
// was the shrinking itself in anyone's mouth. Now when a town's index is low (35 or under: the killings and the trains out
// have outrun the trains in), its people think of the empty houses, the shop that shut, the neighbours who left and whether
// they'll be next ("Hendry's shuttered the bakery. Says there's no trade left to bake for."), and the paper's monthly
// mention adds that a shop on the main street has put up its shutters for good.
//
// Why it reads true: the West of 1899 was full of towns that died (the railroad passed them by, the mine played out, the
// violence drove families off), and their people watched it happen one shuttered window at a time. Leaving is contagious:
// each family gone makes the next likelier (the tipping of a neighbourhood: Schelling, 1971), so a town's talk of who left
// is also talk of who's next. Read only: it reads the ledger's index. Off in Core so the goldens stand.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class TownShrinks
    {
        public static bool On;   // [World] TownShrinks, set by the game
        public const int Threshold = 35, Pull = 21;

        public static bool Shrinking(int index) { return On && index >= 0 && index <= Threshold; }

        public static string Line(string seed, List<string> told) { return InnerLife.PickTold(Lines, seed + "|shrink", told) ?? Lines[0]; }

        public const string PaperLine = " A shop on the main street has put up its shutters for good.";

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Lines) yield return l;
            yield return PaperLine.Trim();
        }

        private static readonly string[] Lines = {
            "The baker's shuttered up. Says there's no trade left to bake for.",
            "Three houses empty on our street now. You can hear the wind go through them.",
            "Another family took the train Tuesday. Didn't even sell the furniture.",
            "Every family that goes makes it easier for the next. I keep thinking about it.",
            "Fewer at church every Sunday. The preacher's started counting out loud.",
            "Store's out of half of what it used to carry. Not worth his while to order it.",
            "This town was something, when I came. Now look.",
            "If one more family goes, I'll go too. I said that last month.",
            "Grass coming up through the boardwalk by the old feed store.",
            "Used to know every face at the post office. Half of them are gone now.",
            "Wrote my brother out in Oregon. Asked what land costs there. Just asking." };
    }
}
