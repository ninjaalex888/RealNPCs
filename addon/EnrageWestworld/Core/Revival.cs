// A camp meeting: three nights of revival (FIVEDAY reserve R4). In the summer (June to August) a travelling preacher pitches
// his tent outside one of the country towns for three nights, half past seven till ten: the hymns, the preaching (the call
// to repent the first night, hellfire the second), and on the third the altar call, when the sinners come forward to the
// mourners' bench. A townsman whose worry is the drink or the cards goes up and is saved: for two weeks his days
// lean away from the saloon and the card table and toward church, and one in three backslides at the week. Booked as three
// town events (posted, so the notice board has them); staged like the shivaree, a ring of the faithful round the preacher,
// spawned out of view; a night there counts as attending, and the regulars warm a little to the stranger who came. The
// saved man's mark lives in Store.Counts ("saved:<id>", the day): nothing new in the save. Behind [World] TownEvents and
// EventRevivals.
//
// Why it reads true: the Second Great Awakening's camp meetings still ran in 1899 across the rural South and West, a summer
// social event as much as a religious one. Public commitment under a crowd's eye is one of the strongest levers on
// behaviour there is, and it fades as the crowd goes home: which is why the backsliding was as famous as the saving.
// Plain C#, no game; all words original. mindtest RevivalTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Revival
    {
        public const string Kind = "revival";
        public const int FromMin = 19 * 60 + 30, ToMin = 22 * 60;
        public const int Nights = 3, DayChance = 2, GapDays = 30;   // a summer day's roll per town, at least a month between
        public const int SavedDays = 14, BackslideDay = 7, BackslidePercent = 33;
        public const int Affection = 2;
        public const string SavedPrefix = "saved:";
        public static readonly string[] Towns = { "Valentine", "Strawberry", "Rhodes", "Blackwater", "Annesburg", "Armadillo", "Emerald Ranch" };

        public static bool Summer(int month) { return month >= 6 && month <= 8; }

        /// Whether this town books a camp meeting today (the first night tomorrow).
        public static bool Books(string town, long day, int month, long lastDay)
        {
            return Summer(month) && day - lastDay >= GapDays && Voice.Chance("revival|" + town + "|" + day, DayChance);
        }

        /// The three nights' events, from tomorrow.
        public static List<TownEvent> For(string town, long day, string preacher)
        {
            var list = new List<TownEvent>();
            for (int n = 1; n <= Nights; n++)
                list.Add(new TownEvent
                {
                    Id = Ids.New(), Kind = Kind, Town = town, Title = "A camp meeting, night " + n + " of " + Nights, At = (day + n) * 1440 + FromMin, Until = (day + n) * 1440 + ToMin,
                    Count = n, Extra = preacher
                });
            return list;
        }

        /// Who goes forward on the third night: a townsman whose worry is the drink or the cards (worry: their worry
        /// key today, or null).
        public static bool Sinner(string worry) { return worry == "drink" || worry == "cards"; }

        /// Still saved, this many days after: two weeks, but a third backslide at the week.
        public static bool StillSaved(string id, long savedDay, long day)
        {
            long since = day - savedDay;
            if (since < 0 || since >= SavedDays)
                return false;
            return since < BackslideDay || !Voice.Chance(id + "|backslide|" + savedDay, BackslidePercent);
        }

        /// How being saved leans a choice (Choosing's act ids).
        public static float Lean(bool saved, string act)
        {
            if (!saved)
                return 0f;
            return act == "saloon" ? -0.3f : act == "drink" ? -0.3f : act == "cards" ? -0.35f : act == "church" ? 0.2f : act == "pray" ? 0.15f : act == "work" ? 0.05f : 0f;
        }

        public static string Preacher(string seed) { return "Brother " + Residents.Surname(seed + "|preacher") + " of " + Voice.Pick(seed + "|pt", "Missouri", "Tennessee", "Kentucky", "Arkansas"); }

        private static readonly string[] Hymns = { "[a hymn, ragged and loud: the whole tent singing]", "[the faithful singing, a tambourine, someone wailing the high part]", "[a slow hymn, then hands clapping, faster]" };
        private static readonly string[] NightOne = {
            "Every one of you has a stone in his heart tonight. I've come to roll it away.",
            "The Lord sees the whiskey glass and the card table. He sees, and He waits.",
            "Repent! Not tomorrow, for tomorrow is the Devil's favourite word." };
        private static readonly string[] NightTwo = {
            "There's a fire that never goes out, brethren, and it's not the one in this lantern.",
            "The sinner sleeps sound tonight. He'll not sleep sound in the pit.",
            "I've seen men die cursing, and I've seen men die singing. Choose." };
        private static readonly string[] NightThree = {
            "Who will come? Who will come up to the bench tonight? There's room for every one of you.",
            "Come forward. Come forward and be washed. Don't let your neighbour stop you.",
            "The bench is waiting. Your mother's prayers are waiting. Come." };
        private static readonly string[] Amens = { "Amen!", "Glory!", "Preach it, Brother!", "Hallelujah!" };
        private static readonly string[] SavedLines = { "I'm done with it. Done. Lord help me, I'm done.", "Pray for me. Pray for me, all of you." };
        private static readonly string[] SavedThoughts = {
            "Swore off in front of the whole county. Can't take it back now.", "Felt it lift off me. Whatever it was, it lifted.",
            "Passed the saloon twice today. Kept walking. Twice." };
        private static readonly string[] BackThoughts = {
            "One drink. One. The Lord knows a man's thirst.", "Saved for a week. That's longer than last summer." };

        public static string Hymn(string seed) { return Voice.Pick(seed + "|rh", Hymns); }
        public static string Sermon(int night, string seed) { return Voice.Pick(seed + "|rs", night <= 1 ? NightOne : night == 2 ? NightTwo : NightThree); }
        public static string Amen(string seed) { return Voice.Pick(seed + "|ra", Amens); }
        public static string SavedLine(string seed) { return Voice.Pick(seed + "|rv", SavedLines); }
        public static string SavedThought(bool backslid, string seed) { return Voice.Pick(seed + "|rt", backslid ? BackThoughts : SavedThoughts); }

        public static string Posted(string town, string preacher)
        {
            return "A camp meeting outside " + (town ?? "town") + ": three nights from tomorrow, half past seven, " + (preacher ?? "a preacher from Missouri") + " preaching.";
        }

        public static string Morning(TownEvent e, string savedName, bool attended)
        {
            string s = e.Count >= Nights ? (savedName != null ? savedName + " went up to the mourners' bench at the camp meeting last night, in front of the whole county." : "The camp meeting ended last night. Nobody went up to the bench.")
                                         : "The camp meeting was loud last night. They heard the singing in town.";
            return attended ? s + " The stranger was in the tent." : s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { NightOne, NightTwo, NightThree, Amens, SavedLines, SavedThoughts, BackThoughts }) foreach (string l in p) yield return l;
            yield return Posted("Strawberry", "Brother Hale of Missouri");
            yield return Morning(new TownEvent { Count = 3 }, "Abel Grange", true);
            yield return Morning(new TownEvent { Count = 1 }, null, false);
        }
    }
}
