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
        public Func<int, int, int, bool> On;        // (year, month, day): the festival's date (null for one booked by a death)
        public bool OnDeath;                         // booked the day after a named death in its town ({d} the dead; R21)
        public bool Couple;                          // its man and woman come away courting (a "flirt" between them), not cheered and envied (R22)
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
            // The Fourth of July (R18, [World] EventFourth): every town of size, six till half past nine at the bandstand (else
            // the main street): the band and the bunting, the Declaration read out, a speech too long, firecrackers under the
            // horses, the shooting match (a townsman's win), the fireworks at dark. Why it reads true: thirty-four years after
            // the war the Fourth was the one day the whole country agreed to be one town, a ritual of belonging (and of
            // gunpowder) that even the ones it left out turned up for.
            new FestivalKind
            {
                Kind = "fourth", Title = "Independence Day", Spot = "bandstand", Towns = new[] { "Valentine", "Saint Denis", "Rhodes", "Blackwater", "Strawberry", "Armadillo" },
                FromMin = 18 * 60, ToMin = 21 * 60 + 30, Affection = 2,
                On = (y, m, d) => m == 7 && d == 4,
                Beats = new[] {
                    "[a brass band, bunting on every porch, the flag up the pole]",
                    "When in the course of human events... [the Declaration, read out by the mayor, all of it]",
                    "[firecrackers under the horses, and a dog that won't come out from the porch]",
                    "Friends, this great nation, a hundred and twenty-three years free... [the speech goes on]",
                    "The shooting match to {m}! Five bottles, five shots, not a one left standing!",
                    "[the fireworks at dark: rockets, and the whole street saying ahh]" },
                ManPrize = "the shooting match",
                Posted = "The Fourth of July at {t}: the band at six, the reading, the shooting match, fireworks at dark.",
                Morning = "The Fourth at {t}: {m} won the shooting match, the mayor spoke an hour, and a rocket set fire to the livery roof."
            },
            // Mardi Gras in Saint Denis (R19, [World] EventMardiGras): Fat Tuesday (Easter less forty-seven days), seven till
            // eleven on the main street: masks and torches, the krewe's float, the king of the carnival crowned (a townsman), the
            // king cake cut and the baby found (a townswoman, next year's queen), the street dancing till the bells, and the
            // morning after Ash Wednesday's quiet. Why it reads true: the carnival is licensed disorder, one night when rank
            // and rule are turned over by permission, which is why a strict town needs it and why the morning after is so quiet.
            new FestivalKind
            {
                Kind = "mardigras", Title = "Mardi Gras", Spot = "mainstreet", Towns = new[] { "Saint Denis" }, FromMin = 19 * 60, ToMin = 23 * 60, Affection = 2,
                On = (y, m, d) => { System.DateTime t = Calendar.Easter(y).AddDays(-47); return t.Month == m && t.Day == d; },
                Beats = new[] {
                    "[masks and torches, a cornet band, the whole street in costume]",
                    "[the krewe's float rolls by, paper dragons and a man dressed as the Devil throwing beads]",
                    "Hail the King of the Carnival! Long live King {m}!",
                    "[the king cake cut on a trestle, every slice watched]",
                    "The baby! {w} has the baby! Next year's queen!",
                    "[dancing in the street till the church bells ring for Lent]" },
                ManPrize = "King of the Carnival", WomanPrize = "the king cake's baby",
                Posted = "Mardi Gras tomorrow night in Saint Denis: the krewe's parade at seven, masks and the king cake after.",
                Morning = "Ash Wednesday, and Saint Denis is very quiet. {m} was King of the Carnival; {w} found the baby in the cake."
            },
            // A brass band funeral in Saint Denis (R21, [World] EventBrassFuneral): the day after a named death in the city, three
            // till five, from the cemetery spot: the band's dirge going out, the hymn at the grave, and coming back the second
            // line, the band hot and the street dancing behind it. Why it reads true: the city's benevolent societies buried their
            // members with a band, mourning on the way out and rejoicing on the way back, grief turned into a whole street's
            // celebration of a life (and a society's promise that nobody is buried alone).
            new FestivalKind
            {
                Kind = "brassfuneral", Title = "A funeral with a band", Spot = "cemetery", Towns = new[] { "Saint Denis" }, FromMin = 15 * 60, ToMin = 17 * 60, Affection = 2,
                OnDeath = true,
                Beats = new[] {
                    "[a brass band, slow, the dirge going out, and the society's men in black sashes behind {d}]",
                    "[the hymn at the graveside, the trombone carrying the tune, the women's voices under it]",
                    "Dust to dust. Go on home now, {d}. Go on home.",
                    "[the snare cracks, the band strikes up hot, and the second line is dancing in the street]",
                    "[parasols and handkerchiefs in the air, the whole street behind the band, and nobody sad]" },
                Posted = "The society buries {d} tomorrow at three, with the band. All who knew him are asked to walk.",
                Morning = "They buried {d} with the band yesterday, and the second line went on till dark."
            },
            // A fais do-do in Lagras (R22, [World] EventFaisDoDo): every Saturday night, eight till half past eleven, the bayou's
            // dance: a fiddle and an accordion, waltzes and two-steps on the boards, the gumbo pot, the babies put to sleep in the
            // back room ("fais do-do", go to sleep, is what the mothers sang), and a young couple of the village dancing every
            // dance together, who come away courting. Why it reads true: the weekly dance was the bayou's whole social world, the
            // place courtship was done in public, under every aunt's eye (the chaperone as institution), and for that, safely.
            new FestivalKind
            {
                Kind = "faisdodo", Title = "A fais do-do", Spot = "mainstreet", Towns = new[] { "Lagras" }, FromMin = 20 * 60, ToMin = 23 * 60 + 30, Affection = 2,
                On = (y, m, d) => Calendar.Weekday(y, m, d) == 6,
                Beats = new[] {
                    "[a fiddle and an accordion strike up, and the boards start to thump]",
                    "[a waltz, slow, the old couples first, then everybody]",
                    "[the babies carried to the back room and sung to sleep: fais do-do, fais do-do]",
                    "Look at {m} and {w}. Every dance together. Somebody tell her mother.",
                    "[a two-step, fast, the gumbo pot passed round, somebody's uncle on the spoons]",
                    "[the last waltz, and the lamps turned down]" },
                ManPrize = "every dance with her", WomanPrize = "every dance with him", Couple = true,
                Posted = "Fais do-do in Lagras Saturday night: the fiddle, the accordion, gumbo for all.",
                Morning = "At the fais do-do in Lagras, {m} and {w} danced every dance together. The aunts are talking."
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

        public static string Fill(string line, string town, string man, string woman, string dead = null)
        {
            return (line ?? "").Replace("{t}", town ?? "town").Replace("{m}", man ?? "a county man").Replace("{w}", woman ?? "a county woman").Replace("{d}", dead ?? "our brother");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (FestivalKind f in All)
            {
                foreach (string b in f.Beats) yield return Fill(b, f.Towns[0], "Abel Grange", "Cora Hollis", "Abel Grange");
                yield return Fill(f.Posted, f.Towns[0], "Abel Grange", "Cora Hollis", "Abel Grange");
                yield return Fill(f.Morning, f.Towns[0], "Abel", "Cora", "Abel Grange");
            }
        }
    }
}
