// Talk on the church steps (LIVES item 2c, 2026-11-12; [Scenes] ChurchSteps, settings version 469; the roadmap's "After the
// service" row). Sunday properly (Core/Sunday.cs, [World] SundayMorning) has the walk to church, the hymn, the sermon and the
// dinner after; what it didn't have was the talk on the steps, where a town of a few hundred took its weekly roll call. Now on a Sunday
// from eleven to two, near the town's church, two of the town in the street talk it over: who wasn't in his pew (a townsman
// whose habits keep him from church, by name: "Didn't see Hollis in his pew. Third Sunday."), the sermon, who sat
// beside whom. And one of them who went up at the revival's altar and has since gone back to the drink or the cards hears
// about it from the other ("Saw you go up at the camp meeting. Haven't seen you since.").
//
// Why it reads true: church attendance in an 1899 town of a few hundred was public and noticed (the pews were often rented or family
// pews, so an empty one had a name on it), and the steps after the service were the week's main social gathering; gossip
// there did the work of social control (the anthropologists' "gossip as norm enforcement", Gluckman 1963). A backslider is
// a story the whole congregation knows the start of.
//
// Cast from two already near each other outside the church (the Director's pair reasons; the game sets Place.NearChurch):
// nobody is walked over, no crowd gathered. It leaves nothing between them but acquaintance. Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ChurchSteps
    {
        public const string Kind = "churchsteps";
        public static bool On;   // [Scenes] ChurchSteps, set by the game
        public const int Strength = 39, Percent = 30, BackslidStrength = 47, BackslidPercent = 60;

        /// Two of a town near its church after the Sunday service: a talk on the steps; with one backslid since the revival,
        /// the other has a word with them about it. Null for no reason.
        public static Reason For(Someone x, Someone y, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || !where.NearChurch || where.Saloon || x == null || y == null || x.Id == y.Id)
                return null;
            if (InnerLife.Weekday != 0 || where.Hour < 11 || where.Hour >= 14)
                return null;
            if (x.P == null || y.P == null || x.P.Home == null || x.P.Home != y.P.Home)
                return null;
            if (y.Backslid && !x.Backslid && Voice.Chance(Lives.Key(x, y) + "|" + gameDay + "|backslid", BackslidPercent))
                return new Reason { Kind = Kind, Strength = BackslidStrength, Matter = "backslid", Why = x.Name + " has a word with " + y.Name + " on the church steps: up at the revival's altar, and back to old ways since" };
            if (!Voice.Chance(Lives.Key(x, y) + "|" + gameDay + "|steps", Percent))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + " and " + y.Name + " talk on the church steps after the service" };
        }

        /// A and B on the steps; c.Absent the man of the town who wasn't in church (null: none known); backslid: B went up at
        /// the revival and has gone back to old ways.
        public static Scene Make(SceneCast c, bool backslid, string seed)
        {
            string a = c != null && c.Names != null && c.Names[0] != null ? Person.FirstOf(c.Names[0]) : null;
            string b = c != null && c.Names != null && c.Names[1] != null ? Person.FirstOf(c.Names[1]) : null;
            string z = c != null && c.Absent != null ? Person.FirstOf(c.Absent) : null;
            var s = new Scene { Kind = Kind, Summary = "two of the town talked it over on the church steps after the service" };
            if (backslid)
            {
                string[] t = Pick(Backslid, seed + "|bs");
                s.Add(0, Fill(t[0], b), "mild", "face").Add(1, t[1], "mild").Add(0, t[2], "mild");
                s.Summary = "on the church steps, one who went up at the revival heard about going back to old ways";
                return s;
            }
            int roll = (Voice.Hash(seed + "|steps") & 0x7fffffff) % 3;
            string[] e = z != null && roll == 0 ? Pick(Absent, seed + "|ab") : roll == 1 || z == null && roll == 0 ? Pick(Sermon, seed + "|se") : Pick(Seats, seed + "|sa");
            s.Add(0, Fill(e[0], z), "mild").Add(1, Fill(e[1], z), "mild").Add(0, Fill(e[2], z), "mild");
            return s;
        }

        private static string Fill(string l, string who) { return who != null ? l.Replace("{z}", who).Replace("{b}", who) : l.Replace("{z}", "somebody").Replace("{b}", "friend"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] set in new[] { Absent, Sermon, Seats, Backslid })
                foreach (string[] t in set)
                    foreach (string l in t)
                        yield return Fill(l, "Hollis");
        }

        private static readonly string[][] Absent = {
            new[] { "Didn't see {z} in his pew this morning.", "Third Sunday running.", "Somebody ought to go and knock on his door." },
            new[] { "{z}'s seat was empty.", "He'll be sleeping off Saturday night.", "Then we'll pray he wakes up in time for the next one." },
            new[] { "Reverend looked straight at {z}'s empty pew when he said the part about the lost sheep.", "Everybody saw him do it.", "Well. Somebody ought to tell {z}." } };
        private static readonly string[][] Sermon = {
            new[] { "Reverend was hard on the drink this morning.", "Not hard enough, by the look of the back rows.", "I near laughed out loud. God forgive me." },
            new[] { "That was a long one today.", "The Lord's got patience. My knees don't.", "Hush. He'll hear you." },
            new[] { "Did you follow all of that about the Pharisees?", "Not a word of it. Fine voice, though.", "Fine voice. I'll give him that." } };
        private static readonly string[][] Seats = {
            new[] { "Did you see who sat beside the new schoolteacher?", "The whole church saw.", "His mother's face. I thought she'd faint." },
            new[] { "Somebody's moved into the Hales' old pew.", "Sat there like they'd paid for it.", "Maybe they did." },
            new[] { "The Dawson girl came in on her brother's arm and left on another's.", "I noticed.", "Everybody noticed. That's why she did it." } };
        private static readonly string[][] Backslid = {
            new[] { "Saw you go up at the camp meeting, {b}. Haven't seen you since.", "I've been meaning to come.", "The door's open. It always was." },
            new[] { "I hear you're back at the cards, {b}.", "Who told you that?", "Nobody needed to. It's in your face, Sunday morning." },
            new[] { "You went down on your knees in that tent, {b}. I was there.", "A man says things in a tent.", "He says them to God, and God remembers." } };

        private static string[] Pick(string[][] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
