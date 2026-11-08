// A barn raising (FIVEDAY reserve R7). From March to October, now and then a farm family in one of the country towns has
// its barn raised by the neighbours on a Saturday morning, posted the week before: the frame laid out on the ground, forty
// hands on the pike poles, the bents pushed up one by one, the women's dinner after. And if two men of the town are at odds
// (a hostile tie), the foreman puts them on the same beam: they bicker, and then the beam has to go up. Six times in ten the
// work mends it (a "makeup" tie and a memory); the rest, they fall out again ("argue"). The neighbours who came are warmer to
// the family (a "help" tie each), and the stranger who was there is thought the better of by the regulars. Staged like the
// shivaree, at the town's "corral" spot or near you. Behind [World] TownEvents and EventBarnRaisings.
//
// Why it reads true: a barn was more than one family could raise, so the whole district did it, and owed and repaid the
// labour for a generation. And a goal neither side can reach alone is the oldest cure for a feud there is (Sherif's
// superordinate goals): it isn't certain, but it's better odds than talking. Plain C#, no game; all words original.
// mindtest BarnRaisingTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class BarnRaising
    {
        public const string Kind = "barnraising";
        public const int FromMin = 9 * 60, ToMin = 12 * 60;
        public const int DayChance = 2, GapDays = 21;
        public const int MendPercent = 60, Affection = 3, Helpers = 4;
        public static readonly string[] Towns = { "Valentine", "Strawberry", "Rhodes", "Blackwater", "Emerald Ranch", "Annesburg" };

        public static bool Season(int month) { return month >= 3 && month <= 10; }

        public static bool Books(string town, long day, int month, long lastDay)
        {
            return Season(month) && day - lastDay >= GapDays && Voice.Chance("barn|" + town + "|" + day, DayChance);
        }

        /// Days from this weekday (0 Sunday) to the next Saturday, at least two (the word must get round).
        public static int DaysToSaturday(int weekday)
        {
            int d = (6 - (weekday < 0 ? 0 : weekday) + 7) % 7;
            return d < 2 ? d + 7 : d;
        }

        /// The family: a farmer of the town, by the seed. Null if the town has none.
        public static Resident Family(IEnumerable<Resident> town, string seed)
        {
            return town.Where(r => !r.Dead && !r.Left && !r.Fled && r.Held == null && r.Trade != null && r.Trade.Contains("farm")).OrderBy(r => Voice.Hash(seed + r.Id)).FirstOrDefault();
        }

        /// Two men of the town at odds (a hostile tie, both men of the town), for the same beam; false if none.
        public static bool Feud(IEnumerable<SceneTie> ties, ICollection<string> menOfTown, out SceneTie feud)
        {
            feud = ties.Where(t => t.Warmth < 0 && menOfTown.Contains(t.A) && menOfTown.Contains(t.B)).OrderBy(t => t.Warmth).FirstOrDefault();
            return feud != null;
        }

        public static bool Mends(string seed) { return Voice.Chance(seed + "|mend", MendPercent); }

        private static readonly string[] Calls = { "All hands to the first bent! Pike poles ready!", "Get your backs under it, boys. On my word.", "Here she comes. Steady, steady, and up!" };
        private static readonly string[] Lifts = { "[the frame rises, forty hands on the pike poles]", "[a bent goes up and is pinned, and a cheer]", "[timber creaking, men grunting, the beam swinging true]" };
        private static readonly string[] Bicker = { "Keep your end up, {o}. If you can manage that.", "I'd sooner lift with a mule than with you, {o}.", "Of all the beams in this county, they put me on yours." };
        private static readonly string[] Mend = { "...That's it. Pin it. Good work, {o}.", "You pull your weight, {o}. I'll give you that." };
        private static readonly string[] FallOut = { "Your end was low again, {o}. Always is.", "Barn's up, {o}. That's all we had to say to each other." };
        private static readonly string[] Dinner = { "[the women set out dinner on trestles: ham, beans, pies by the dozen]", "[tin plates, cold tea, and the whole district sitting in the shade of the new barn]" };
        private static readonly string[] FamilyThanks = { "God bless every one of you. We'd never have done it alone.", "There's pie for all, and our thanks for the rest of our lives." };

        public static string Call(string seed) { return Voice.Pick(seed + "|bc", Calls); }
        public static string Lift(string seed) { return Voice.Pick(seed + "|bl", Lifts); }
        public static string BickerLine(string other, string seed) { return Voice.Pick(seed + "|bb", Bicker).Replace("{o}", other ?? "neighbour"); }
        public static string After(bool mended, string other, string seed) { return Voice.Pick(seed + "|ba", mended ? Mend : FallOut).Replace("{o}", other ?? "neighbour"); }
        public static string DinnerLine(string seed) { return Voice.Pick(seed + "|bd", Dinner); }
        public static string FamilyLine(string seed) { return Voice.Pick(seed + "|bf", FamilyThanks); }

        public static string Posted(string town, string family) { return "A barn raising for " + (family ?? "a family") + " near " + (town ?? "town") + ", Saturday at nine. Bring your hands."; }

        public static string Morning(TownEvent e, string pair, bool mended, bool attended)
        {
            string s = Person.FirstOf(e.AName) + "'s barn went up Saturday.";
            if (pair != null)
                s += mended ? " " + pair + " shared a beam and shook hands." : " " + pair + " shared a beam. It didn't help.";
            return attended ? s + " The stranger helped." : s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Calls, Lifts, Dinner, FamilyThanks }) foreach (string l in p) yield return l;
            foreach (string[] p in new[] { Bicker, Mend, FallOut }) foreach (string l in p) yield return l.Replace("{o}", "Seth");
            yield return Posted("Strawberry", "Abel Grange");
            yield return Morning(new TownEvent { AName = "Abel Grange" }, "Seth and Will", true, true);
            yield return Morning(new TownEvent { AName = "Abel Grange" }, "Seth and Will", false, false);
        }
    }
}
