// A river baptism (FIVEDAY reserve R5). The Sunday after a camp meeting's altar call (Core/Revival.cs), the man who was
// saved is baptised in the river: from two till half past three, at the town's "riverbank" spot, the preacher and the
// faithful on the bank, a hymn, the preacher's words, and the man goes under and comes up washed; the amens, and his own
// words. Baptised, he's saved three weeks, not two, and doesn't backslide (Revival.StillSaved reads "baptised:<id>" in
// Store.Counts). With no riverbank spot recorded, or you away, it happens all the same and the morning has the word. Booked
// as a town event; staged like the revival; a while there counts as attending. Behind [World] TownEvents and EventBaptisms.
//
// Why it reads true: the river baptism was the public seal on a private turning, done in front of the whole community, and
// a commitment made twice and in public is far harder to walk back (consistency): which is why the churches insisted on it.
// Plain C#, no game; all words original. mindtest BaptismTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Baptism
    {
        public const string Kind = "baptism";
        public const int FromMin = 14 * 60, ToMin = 15 * 60 + 30;
        public const int SavedDays = 21;
        public const string Prefix = "baptised:";

        /// The baptism for a man saved on this day: the next Sunday (weekday 0) after it, in the afternoon. weekdayOfSaved:
        /// 0 Sunday .. 6 Saturday.
        public static TownEvent For(string town, string id, string name, long savedDay, int weekdayOfSaved, string preacher)
        {
            if (town == null || id == null)
                return null;
            int ahead = (7 - weekdayOfSaved) % 7;
            if (ahead == 0)
                ahead = 7;
            long day = savedDay + ahead;
            return new TownEvent
            {
                Id = Ids.New(), Kind = Kind, Town = town, Title = "A baptism at the river", At = day * 1440 + FromMin, Until = day * 1440 + ToMin,
                AId = id, AName = name, Extra = preacher
            };
        }

        private static readonly string[] Hymns = { "[a hymn about the river, the whole bank singing]", "[the old hymn, slow, the women's voices high over the water]" };
        private static readonly string[] Words = {
            "Brethren, we bring this man down to the water as he asked. Let the river take what he leaves in it.",
            "In the name of the Father, and of the Son, and of the Holy Ghost. Hold your breath, brother." };
        private static readonly string[] Under = { "[he goes under, the preacher's hand on his chest]", "[the water closes over him, and the bank goes quiet]" };
        private static readonly string[] Up = { "[up, gasping, the water streaming off him]", "[he comes up blinking, and the bank shouts]" };
        private static readonly string[] Amens = { "Amen!", "Glory be!", "Washed!", "Hallelujah!" };
        private static readonly string[] HisWords = { "Cold as sin, that water. Colder.", "I'm new. I'm a new man, Lord help me keep it.", "Never thought I'd see this day. Never." };
        private static readonly string[] Thoughts = {
            "Under the water it was quiet. First quiet I've had in years.", "Whole county on the bank watching me. Now I've got to mean it.",
            "Dry clothes, and church, and no saloon. That's the bargain." };

        public static string Hymn(string seed) { return Voice.Pick(seed + "|bh", Hymns); }
        public static string PreacherWords(string seed) { return Voice.Pick(seed + "|bw", Words); }
        public static string GoesUnder(string seed) { return Voice.Pick(seed + "|bu", Under); }
        public static string ComesUp(string seed) { return Voice.Pick(seed + "|bc", Up); }
        public static string Amen(string seed) { return Voice.Pick(seed + "|ba", Amens); }
        public static string His(string seed) { return Voice.Pick(seed + "|bm", HisWords); }
        public static string Thought(string seed) { return Voice.Pick(seed + "|bt", Thoughts); }

        public static string Morning(TownEvent e, bool attended)
        {
            string s = (e.AName ?? "A man") + " was baptised in the river yesterday, the faithful on the bank.";
            return attended ? s + " The stranger stood with them." : s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Hymns, Words, Under, Up, Amens, HisWords, Thoughts }) foreach (string l in p) yield return l;
            yield return Morning(new TownEvent { AName = "Abel Grange" }, true);
        }
    }
}
