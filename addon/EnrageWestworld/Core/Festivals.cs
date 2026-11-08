// The calendar's festivals as events (FIVEDAY reserve R17 the county fair; R18 the Fourth of July and R19 Mardi Gras add their
// kinds here). A festival is a town event on its date: booked the day before (posted, so the notice board and the box have
// it), staged like the shivaree (a crowd spawned out of view at the town's spot, its beats one by one), and settled when it's
// over: its prize-winners named from the town's own people (a "cheer" tie from a neighbour; an "envy" from a rival), the
// regulars warmer to the stranger who came, the morning's word. Each kind its own switch. Plain C#, no game; all words
// original. mindtest FestivalsTest.
//
// The county fair (R17, [World] EventFair): Valentine, the third Saturday of September, eleven till two at the stockyards:
// the bunting and the cornet, the livestock judged (a man's prize beast), the pie table (a woman's blue ribbon, and a rival's
// sour face), the greasy pig. Why it reads true: the fair was the county's yearly show of itself, and a ribbon was status
// that a whole county saw; the rivalry over it is as old as the fair (social comparison at its most public).
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class FestivalKind
    {
        public string Kind, Title, Spot;
        public string[] Towns;
        public int FromMin, ToMin, Affection = 3;
        public Func<int, int, int, bool> On;        // (year, month, day): the festival's date
        public string[] Beats;                       // the staged lines in order ({m} a man's prize, {w} a woman's)
        public string ManPrize, WomanPrize;         // what the named winners win, or null
        public string Posted, Morning;              // {t} the town, {m} {w} the winners
    }

    public static class Festivals
    {
        public static readonly List<FestivalKind> All = new List<FestivalKind>
        {
            new FestivalKind
            {
                Kind = "fair", Title = "The county fair", Spot = "stockyards", Towns = new[] { "Valentine" }, FromMin = 11 * 60, ToMin = 14 * 60,
                On = (y, m, d) => m == 9 && d >= 15 && d <= 21 && Calendar.Weekday(y, m, d) == 6,   // the third Saturday of September
                Beats = new[] {
                    "[bunting on every rail, a brass cornet, the smell of roast corn and cattle]",
                    "Ladies and gentlemen, the judging! First prize for the finest beast in the county goes to... {m}!",
                    "[the crowd round the pens, hats in the air]",
                    "And the blue ribbon for pies, against a powerful field, goes to {w}!",
                    "[a greased pig let loose, and every boy in the county after it]",
                    "That's the fair for another year. Mind the cattle on your way home!" },
                ManPrize = "first prize for his steer", WomanPrize = "the blue ribbon for pies",
                Posted = "The county fair at {t} on Saturday: the judging, the pies, the greasy pig. All welcome.",
                Morning = "At the county fair: {m} took first prize for his steer, and {w} the blue ribbon for pies. Some say the judges were her cousins."
            },
        };

        public static FestivalKind Of(string kind)
        {
            foreach (FestivalKind f in All)
                if (f.Kind == kind)
                    return f;
            return null;
        }

        /// Whether a festival kind falls on this date.
        public static bool On(FestivalKind f, int year, int month, int day) { return f != null && f.On != null && year > 1800 && month >= 1 && f.On(year, month, day); }

        /// Tomorrow's date (for booking the day before).
        public static void Tomorrow(int year, int month, int day, out int y, out int m, out int d)
        {
            DateTime t = new DateTime(year, month, Math.Min(day, DateTime.DaysInMonth(year, month))).AddDays(1);
            y = t.Year; m = t.Month; d = t.Day;
        }

        public static string Fill(string line, string town, string man, string woman)
        {
            return (line ?? "").Replace("{t}", town ?? "town").Replace("{m}", man ?? "a county man").Replace("{w}", woman ?? "a county woman");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (FestivalKind f in All)
            {
                foreach (string b in f.Beats) yield return Fill(b, f.Towns[0], "Abel Grange", "Cora Hollis");
                yield return Fill(f.Posted, f.Towns[0], "Abel Grange", "Cora Hollis");
                yield return Fill(f.Morning, f.Towns[0], "Abel", "Cora");
            }
        }
    }
}
