// Train time (SIXDAY item 11b; roadmap 11; roadmap.md P10): the rules and the words for a train standing at a town's platform.
// Who gets off: a newcomer from a far city, or kin of someone in the town, made a resident on a body the game already put by
// the platform. Who gets on: a resident whose worry has just ended in leaving (the ticket saved for and bought; the house gone
// to the bank, one time in two; a son gone and the father after him, one in three), said goodbye to a friend and gone for
// good, seen or not. The station's keeper calls the train in his own voice; the people on the platform turn to look. Why it
// reads true: in 1899 the train was how anyone arrived or left, and a town's talk was half about who had. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Trains
    {
        public const float PlatformWatchMetres = 120f;   // the player this near a platform: the watch runs
        public const float TrainNearMetres = 90f;        // a train this near the platform counts as at it (the engine may stand past it)
        public const float LookMetres = 25f, StepDownMetres = 30f;
        public const float StandingSpeed = 0.5f, MovingSpeed = 1.5f;   // m/s
        public const int StandingMs = 5000;
        public const int MostLooking = 4;
        public const int MostTracks = 40;                // track indices probed
        public static readonly string[] FarTowns = { "Saint Louis", "Kansas City", "Chicago", "Philadelphia", "New Orleans", "Denver", "Cincinnati", "Baltimore", "Pittsburgh" };

        /// Whether a worry's end sends someone away on the train.
        public static bool Leaves(string kind, bool good, string seed)
        {
            switch (kind)
            {
                case "savings": return good;                                          // the ticket bought
                case "bank": return !good && Voice.Hash(seed + "|bank") % 2 == 0;      // the house gone: a room over the store, or the train
                case "son": return !good && Voice.Hash(seed + "|son") % 3 == 0;        // the boy gone: some go after him
                default: return false;
            }
        }

        public static string LeavesWhy(string kind)
        {
            switch (kind)
            {
                case "savings": return "the ticket west";
                case "bank": return "the house gone to the bank";
                case "son": return "gone to look for the son";
                default: return "gone";
            }
        }

        /// The station keeper's call as the train stands.
        public static string ArrivalLine(string role, string seed)
        {
            bool freight = role != null && role.Contains("freight");
            return freight
                ? Voice.Pick(seed, "Freight's in. Mind the platform, folks, they'll be unloading.", "That's the freight, and late. Stand clear of the doors.")
                : Voice.Pick(seed, "That's her, right on her time. Mind the step, folks.", "All off that's getting off! Mind the gap at the end car.", "Train's in. Five minutes, no more, so say your goodbyes quick.", "There she is. Baggage to the end of the platform, please.");
        }

        /// A leaver's goodbye to a friend on the platform, by temper.
        public static string Goodbye(string temper, string toFirst, string seed)
        {
            string to = toFirst ?? "friend";
            switch (temper)
            {
                case "Proud": return Voice.Pick(seed, "Well, " + to + ". Don't write; I'll not be reading.", "No speeches, " + to + ". I'm going, that's all.");
                case "Coward": return Voice.Pick(seed, to + ", if anyone asks... say I went east. Say anything.", "I'd stay if I could, " + to + ". I can't. You know I can't.");
                case "Hothead": return Voice.Pick(seed, "This town can go to hell, " + to + ". Not you. The town.", "Tell them I said what I said, " + to + ". All of it.");
                case "Loyal": return Voice.Pick(seed, "I'll write, " + to + ". Every week. See you do too.", "You were the best of this place, " + to + ". I mean that.");
                case "Greedy": return Voice.Pick(seed, "There's money where I'm going, " + to + ". Come find me when you've sense.", "Sell the rest of my things, " + to + ". Keep a dollar for your trouble.");
                default: return Voice.Pick(seed, "Well, " + to + ". That's that, then.", "Look after yourself, " + to + ". I'll think of you.", "Don't let them forget me, " + to + ". Not right away.");
            }
        }

        public static string Farewell(string seed)
        {
            return Voice.Pick(seed, "Go on, before I say something foolish.", "Write when you're settled. Write before that.", "The town'll be smaller. Go on.", "I'll keep your stool warm. Somebody will.");
        }

        /// A newcomer's kin to someone in the town, by the two sexes.
        public static string KinRelation(bool womanNew, bool womanOf, string seed)
        {
            int r = Voice.Hash(seed + "|kin") % 100;
            if (womanNew)
                return r < 45 ? "sister" : r < 70 ? "cousin" : r < 85 ? "niece" : womanOf ? "daughter" : "daughter";
            return r < 45 ? "brother" : r < 70 ? "cousin" : r < 85 ? "nephew" : "son";
        }

        public static string NewcomerFrom(string seed)
        {
            return FarTowns[Voice.Hash(seed + "|from") % FarTowns.Length];
        }

        public static string ArrivedNotice(string town, string name, string kin, string kinOf, string from)
        {
            return kin != null && kinOf != null
                ? name + " stepped off the train in " + town + ": " + Person.FirstOf(kinOf) + "'s " + kin + ", come to stay."
                : name + " stepped off the train in " + town + ", from " + (from ?? "the east") + ", with one bag and no plans anyone knows of.";
        }

        public const int ReturnChance = 33, ReturnAfterDays = 7;   // a neighbour who left comes back on a later train (SIXDAY item 22; roadmap 36)

        /// The notice when someone who left comes back off a train.
        public static string BackNotice(string town, string name, long daysGone)
        {
            string gone = daysGone > 365 ? Voice.Number((int)Math.Min(40, daysGone / 365)) + " years" : daysGone <= 1 ? "a day" : Voice.Number((int)Math.Min(90, daysGone)) + " days";   // (years: the epilogue's moved, back in 1907)
            return name + " stepped off the train in " + town + ", " + gone + " after leaving it. The west didn't take.";
        }

        /// What a friend thinks, seeing them back: the double take.
        public static string BackThought(string first, string seed)
        {
            string f = first ?? "them";
            return Voice.Pick(seed + "|back", "That's " + f + ". I stood on that platform and watched " + f + " go.", f + "? Thought the west had " + f + " for good.",
                "Well, look. " + f + ", back with the same bag. I'll not ask.", f + " came back. They always say they won't, and they always do.");
        }

        public static string LeftNotice(string town, string name, string why, bool seen)
        {
            return seen
                ? name + " took the train out of " + town + " (" + why + "). You saw them go."
                : name + " took the train out of " + town + " (" + why + "). The town says so.";
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 4; i++)
            {
                yield return ArrivalLine("the station agent", "a" + i);
                yield return ArrivalLine("the freight agent", "f" + i);
                yield return Farewell("w" + i);
            }
            foreach (string t in new[] { "Proud", "Coward", "Hothead", "Loyal", "Greedy", null })
                for (int i = 0; i < 2; i++)
                    yield return Goodbye(t, "Amos", "g" + i);
            yield return ArrivedNotice("Valentine", "Hiram Teale", "brother", "Amos Teale", null);
            yield return ArrivedNotice("Valentine", "Hiram Teale", null, null, "Saint Louis");
            yield return LeftNotice("Valentine", "Hiram Teale", LeavesWhy("savings"), true);
            yield return LeftNotice("Valentine", "Hiram Teale", LeavesWhy("bank"), false);
            yield return LeftNotice("Valentine", "Hiram Teale", LeavesWhy("son"), false);
            yield return BackNotice("Valentine", "Hiram Teale", 1);
            yield return BackNotice("Valentine", "Hiram Teale", 12);
            for (int i = 0; i < 4; i++)
                yield return BackThought("Hiram", "b" + i);
        }
    }
}
