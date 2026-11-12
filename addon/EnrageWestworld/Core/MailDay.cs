// Mail day at the depot (LIVES item 2f, 2026-11-12; [Scenes] MailDay, settings version 472; the roadmap's "Mail day" row).
// The train already stands at the platform with its keeper's call, its newcomers and leavers (Core/Trains.cs, [World]
// TrainTime); the mail it brought didn't exist. Now for a while after a train has come in, near the town's platform, one of
// the town opens a letter and reads it out to whoever is by: good news called out ("Letter from my sister Ruth! A boy, born
// in August."), bad news folded away and taken home ("I'll read the rest at home."). For someone whose worry this week is a
// letter that never came, at its end, it's THAT letter, and it reads as their worry ends: well or not.
//
// Why it reads true: in 1899 the mail came by train and the depot was where a town learned of births and deaths far off;
// opening a letter in public was common, and how a person reacts to news in front of others is the moment a town sees who
// they are (the good shared at once, the bad carried home: Goffman's front and back stage). Cast from two already near
// each other by the platform: nobody is walked over. It leaves nothing between them but a word. Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MailDay
    {
        public const string Kind = "mailday";
        public static bool On;   // [Scenes] MailDay, set by the game
        public const int Strength = 40, Percent = 20, GoodChance = 60;

        /// One of the town by the platform after a train came in (Place.MailIn), and whoever is by: a letter opened, or null.
        public static Reason For(Someone x, Someone y, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || !where.MailIn || where.Saloon || x == null || y == null || x.Id == y.Id || x.P == null)
                return null;
            if (where.Town != null && x.P.Home != where.Town)
                return null;   // a visitor's mail comes to his own town (the review)
            long readAt;
            if (Read.TryGetValue(x.Id, out readAt) && gameDay - readAt <= 2)
                return null;   // one letter, read once, not to everyone by in turn
            if (!Voice.Chance(x.Id + "|" + gameDay + "|mail", 80))
                return null;   // rolled before the worry is read (the halfway review: WorryNow per pair is the cost)
            string kind; int stage; bool good;
            bool awaited = InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) && kind == "letter" && stage == 3;
            if (!awaited && !Voice.Chance(x.Id + "|" + gameDay + "|mail2", Percent * 100 / 80))
                return null;   // 80% x 25% = the 20% of anyone else
            return new Reason { Kind = Kind, Strength = Strength + (awaited ? 8 : 0), Matter = awaited ? (good ? "awaited:good" : "awaited:bad") : null, Why = x.Name + " opens a letter off the train" + (awaited ? ", the one waited on" : "") };
        }

        private static readonly Dictionary<string, long> Read = new Dictionary<string, long>();   // who has read a letter, by the game day (memory only)

        /// The scene played: this one's letter is read (by the day).
        public static void NoteRead(string id, long gameDay)
        {
            if (id == null)
                return;
            Read[id] = gameDay;
            if (Read.Count > 128)
                Read.Clear();
        }

        public static void Clear() { Read.Clear(); }

        /// Whether the letter's news is good: the awaited one as the worry ends; any other by the seed.
        public static bool Good(string matter, string seed)
        {
            if (matter == "awaited:good") return true;
            if (matter == "awaited:bad") return false;
            return Voice.Chance(seed + "|mailgood", GoodChance);
        }

        /// A opens it (kin: "my sister Ruth", "Mother"; null: someone back home), B hears.
        public static Scene Make(SceneCast c, string kin, string matter, string seed)
        {
            bool good = Good(matter, seed), awaited = matter != null && matter.StartsWith("awaited");
            string k = kin ?? "my brother";
            string[] t = Pick(good ? (awaited ? AwaitedGood : Good_) : (awaited ? AwaitedBad : Bad), seed + "|ml");
            var s = new Scene { Kind = Kind, Summary = good ? "a letter off the train, good news read out by the platform" : "a letter off the train, bad news taken home" };
            s.Add(0, Fill(t[0], k), good ? "cheer" : "mild", "face").Add(1, Fill(t[1], k), "mild").Add(0, Fill(t[2], k), "mild");
            return s;
        }

        private static string Fill(string l, string kin)
        {
            l = l.Replace("{kin}", kin);
            return l.Length > 0 && char.IsLower(l[0]) ? char.ToUpper(l[0]) + l.Substring(1) : l;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] set in new[] { Good_, Bad, AwaitedGood, AwaitedBad })
                foreach (string[] t in set)
                    foreach (string l in t)
                        yield return Fill(l, "my sister Ruth");
        }

        private static readonly string[][] Good_ = {
            new[] { "Letter from {kin}! A boy, born in August.", "Well, I never. Give them my best.", "I'll write back tonight, first thing." },
            new[] { "{kin} writes the crop came in fine this year.", "That's a mercy.", "First good year since the flood." },
            new[] { "Ten dollars in it, from {kin}. For the winter.", "There's family for you.", "I'll not tell a soul how much." } };
        private static readonly string[][] Bad = {
            new[] { "Letter from {kin}.", "Bad news?", "I'll read the rest at home." },
            new[] { "Uncle's passed. Last spring, and nobody wrote till now.", "That's hard news, that.", "Spring. And I've written him all summer." },
            new[] { "{kin} says the farm's to be sold.", "Can they not hold on?", "Seems not." } };
        private static readonly string[][] AwaitedGood = {
            new[] { "It came. It came! {kin}'s alive and well.", "There now. Didn't I say it would?", "You did. I didn't believe you." },
            new[] { "Two months I've waited on this. {kin}, in the hand I know.", "Read it, then! Don't just stand there.", "I can't. I keep reading the first line." } };
        private static readonly string[][] AwaitedBad = {
            new[] { "It's come. It isn't in {kin}'s hand. Somebody else wrote it.", "Oh, no.", "Don't. I need to get home." },
            new[] { "Two months for this.", "What does it say?", "Nothing a body wants to read on a platform." } };

        private static string[] Pick(string[][] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
