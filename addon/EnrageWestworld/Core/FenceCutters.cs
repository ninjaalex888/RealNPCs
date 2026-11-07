// Fence cutters (FIVEDAY item 31). Now and then, in the night, someone cuts a homesteader's wire in one of the open-range
// towns. In the morning he finds it and blames the cattleman: the man of the town with a ranch trade (his feud neighbour
// first, if he has one). That's a "fencecut" tie between them, two steps colder (Ties.Effect), so their feud climbs into
// the quarrels, the get-even plans and the fights the ties already drive. He's right about two times in three; the third,
// it was drifters cutting through for water. If you were near the town that night, you saw the riders, and the box says
// which it was, so you know what he doesn't. Behind [World] FenceCutters.
//
// Why it reads true: the fence-cutting wars of the 1880s (Texas, 1883, and every range where wire met free grass) were
// fought at night by men nobody saw, so every homesteader blamed the cattleman he already had trouble with. That's the
// fundamental attribution error, and confirmation bias on top: a wrong with no face takes the face of an old grudge. You,
// knowing better, are the only one who could say otherwise, and nobody asked you. Plain C#, no game; all words original.
// mindtest FenceCuttersTest.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class FenceCut
    {
        public long At;                       // the game minute it was found
        public string Town;
        public string HId, HName;             // the homesteader whose wire was cut
        public string CId, CName;             // the one he blames
        public bool Drifters;                 // the truth: drifters, not the cattleman's hands
    }

    public static class FenceCutters
    {
        public static readonly string[] Towns = { "Valentine", "Strawberry", "Rhodes", "Emerald Ranch", "Blackwater", "Armadillo", "Tumbleweed" };
        public const int Chance = 12;          // percent a day
        public const int GapDays = 6;          // never sooner than this after the last
        public const int DriftersPercent = 35;
        public const int NightFrom = 22, NightTo = 5;   // the hours the wire is cut (you near the town then: you saw them)

        /// Whether a cut is found today, and at what minute (six to half past seven: he rides the line at first light).
        public static bool Today(long day, long lastDay, out int minute)
        {
            minute = 6 * 60 + (Voice.Hash("fence|min|" + day) & 0x7fffffff) % 90;
            return day - lastDay >= GapDays && Voice.Chance("fence|day|" + day, Chance);
        }

        public static bool Night(int hour) { return hour >= NightFrom || hour < NightTo; }

        private static bool Farmer(Resident r) { return r.Trade != null && (r.Trade.Contains("farm") || r.Trade.Contains("homestead") || r.Trade.Contains("settler")); }

        private static bool Cattle(Resident r) { return r.Trade != null && (r.Trade.Contains("ranch") || r.Trade.Contains("cow") || r.Trade.Contains("drover") || r.Trade.Contains("cattle")); }

        private static bool Fit(Resident r) { return r != null && !r.Woman && !r.Dead && !r.Left && !r.Fled && r.Held == null && r.Id != null; }   // (not a man gone by train: the Part 5 review)

        /// Whose wire, and whom he blames, of a town's people: a farmer first, and the man he's already at odds with in a
        /// feud (feudWith: the worrier's id to his neighbour's id), else a ranch man, else any other man. False if the
        /// town hasn't two men to stand for it.
        public static bool Pair(List<Resident> town, Dictionary<string, string> feudWith, string seed, out Resident homesteader, out Resident blamed)
        {
            homesteader = blamed = null;
            var men = town.Where(Fit).OrderBy(r => Voice.Hash(seed + "|" + r.Id)).ToList();
            if (men.Count < 2)
                return false;
            homesteader = men.FirstOrDefault(Farmer) ?? men.FirstOrDefault(r => !Cattle(r)) ?? men[0];
            Resident h = homesteader;
            string foe;
            if (feudWith != null && feudWith.TryGetValue(h.Id, out foe))
                blamed = men.FirstOrDefault(r => r.Id == foe);
            if (blamed == null)
                blamed = men.FirstOrDefault(r => r != h && Cattle(r)) ?? men.FirstOrDefault(r => r != h);
            return blamed != null;
        }

        public static bool WereDrifters(string seed) { return Voice.Chance(seed + "|drift", DriftersPercent); }

        private static readonly string[] Found = {
            "{h} rode his line at first light and found the wire cut, post to post. He says it was {c}'s men.",
            "Forty rods of {h}'s wire lying in the grass this morning, cut clean. {h} has {c}'s name in his mouth.",
            "Somebody cut {h}'s fence in the night and his cows are on the road. He's telling the town it was {c}." };
        private static readonly string[] SawRanch = {
            "Last night by {town}: riders at a fence, no lamps, the wire singing as it parted. They turned in at the {s} place.",
            "Outside {town} last night: three men at a fence with nippers, quiet as church. Then off to the {s} spread." };
        private static readonly string[] SawDrifters = {
            "Last night by {town}: riders cut a fence to water their horses. Strangers, ragged, riding on. Nobody's hands.",
            "Outside {town} last night: two drifters snipped a wire to reach the creek. Not ranch men. Just passing." };
        private static readonly string[] Accuser = {
            "{other}'s men cut my wire in the night. I don't need to see a thing to know it.",
            "My fence down and my cows out on the road. Let {other} stand there and deny it.",
            "{other} wants the grass back. Thinks wire cutters will do it. We'll see." };
        private static readonly string[] Accused = {
            "{other} says my boys cut his wire. My boys were in their beds, and I'll swear to it.",
            "Now {other}'s telling the whole town I cut his fence. I've never touched his fence.",
            "If I wanted {other}'s wire down, he'd have seen me do it. I don't skulk.",
            "{other} looks at me in the street like I'm a thief. Over wire. Over a few rods of wire." };
        private static readonly string[] Town = {
            "{a} found his wire cut and swears it was {b}'s hands. {b} swears otherwise. Somebody's lying.",
            "There'll be guns over that fence of {a}'s before long. He's sure it was {b}.",
            "Cut wire at {a}'s place. He's said {b}'s name to everyone who'll stand still for it.",
            "Wire's new out here, and so is cutting it. {a} and {b} won't share a pew now." };

        public static string FoundLine(FenceCut f, string seed) { return Voice.Pick(seed + "|fd", Found).Replace("{h}", Person.FirstOf(f.HName) ?? "a homesteader").Replace("{c}", Residents.SurnameOf(f.CName) ?? "the cattleman"); }

        /// What you saw, if you were near that night.
        public static string SawLine(FenceCut f, string seed)
        {
            return Voice.Pick(seed + "|saw", f.Drifters ? SawDrifters : SawRanch).Replace("{town}", f.Town ?? "town").Replace("{s}", Residents.SurnameOf(f.CName) ?? "ranch");
        }

        public static string AccuserThought(string other, string seed) { return Voice.Pick(seed + "|ac", Accuser).Replace("{other}", other ?? "him"); }
        public static string AccusedThought(string other, string seed) { return Voice.Pick(seed + "|ad", Accused).Replace("{other}", other ?? "him"); }
        public static string TownLine(string a, string b, string seed) { return Voice.Pick(seed + "|tw", Town).Replace("{a}", a).Replace("{b}", b); }

        public static IEnumerable<string> AllLines()
        {
            var f = new FenceCut { Town = "Emerald Ranch", HName = "Abel Grange", CName = "Seth Cornwallis" };
            foreach (string l in Found) yield return l.Replace("{h}", "Abel").Replace("{c}", "Cornwallis");
            foreach (string l in SawRanch.Concat(SawDrifters)) yield return l.Replace("{town}", f.Town).Replace("{s}", "Cornwallis");
            foreach (string l in Accuser.Concat(Accused)) yield return l.Replace("{other}", "Seth");
            foreach (string l in Town) yield return l.Replace("{a}", "Abel").Replace("{b}", "Seth");
        }
    }
}
