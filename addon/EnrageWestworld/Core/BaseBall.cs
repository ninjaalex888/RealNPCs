// Town base ball: Valentine against Strawberry (FIVEDAY reserve R16). Every other Saturday from April to September, at two,
// the two towns' nines play, at home by turns (Valentine on even weeks), posted on the Thursday. Staged like the revival: a
// pitcher and batter, the fielders and a crowd at the town's "showground" spot (else near you); the pitch, the swing, the
// hit or the strike, the crowd, the innings' score, the result. The home crowd glad when they win; the losers' men sore at
// the winners' (an "envy" tie between two regulars of the towns, each game); a game there warms the home regulars to you a
// little. The season's tally lives in Store.Counts ("baseball:Valentine", "baseball:Strawberry"): nothing new saved. Behind
// [World] TownEvents and EventBaseBall.
//
// Why it reads true: town base ball was the summer's great entertainment by 1899, and a neighbouring town the best rival
// there is: in-group pride and out-group rivalry cost nothing to join and bind a town together (social identity). Plain C#,
// no game; all words original. mindtest BaseBallTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class BaseBall
    {
        public const string Kind = "baseball";
        public const string A = "Valentine", B = "Strawberry";
        public const int FromMin = 14 * 60, ToMin = 16 * 60 + 30, PostWeekday = 4, Affection = 2;

        public static bool Season(int month) { return month >= 4 && month <= 9; }

        /// Whether this Thursday posts a game for Saturday, and where: every other week (by the week's parity), home by turns.
        public static bool PostsToday(int weekday, int month, long day, out string home)
        {
            long week = day / 7;
            home = week % 4 < 2 ? A : B;
            return weekday == PostWeekday && Season(month) && week % 2 == 0;
        }

        /// The final score, home first (seeded by the game).
        public static void Score(string seed, out int home, out int away)
        {
            home = 2 + (int)((Voice.Hash(seed + "|h") & 0x7fffffff) % 12);
            away = 2 + (int)((Voice.Hash(seed + "|a") & 0x7fffffff) % 12);
            if (home == away)
                home++;   // no draws: they play on till somebody wins
        }

        private static readonly string[] Pitches = { "[the pitcher winds up, the whole field holding its breath]", "[a fast one, low, and the catcher's mitt cracks]" };
        private static readonly string[] Hits = { "[a crack of the bat, and the ball sails over the fielders' heads]", "[a grounder, and the runner sprinting for first]" };
        private static readonly string[] Strikes = { "Strike three! Sit down, son!", "Swing and a miss! He's out!" };
        private static readonly string[] Cheers = { "Run! Run, you fool, run!", "Throw it home! Home!", "That's how {t} plays ball!", "Kill the umpire!" };

        public static string Pitch(string seed) { return Voice.Pick(seed + "|bp", Pitches); }
        public static string Hit(string seed) { return Voice.Pick(seed + "|bh", Hits); }
        public static string Strike(string seed) { return Voice.Pick(seed + "|bs", Strikes); }
        public static string Cheer(string town, string seed) { return Voice.Pick(seed + "|bc", Cheers).Replace("{t}", town ?? "this town"); }
        public static string Innings(string home, string away, int h, int a, int inning) { return "End of the " + Ordinal(inning) + ": " + home + " " + h + ", " + away + " " + a + "."; }

        private static string Ordinal(int n) { return n == 1 ? "first" : n == 2 ? "second" : n == 3 ? "third" : n == 4 ? "fourth" : n == 5 ? "fifth" : n == 6 ? "sixth" : n == 7 ? "seventh" : n == 8 ? "eighth" : "ninth"; }

        public static string Result(string home, string away, int h, int a)
        {
            string winner = h > a ? home : away, loser = h > a ? away : home;
            return winner + " beat " + loser + " " + System.Math.Max(h, a) + " to " + System.Math.Min(h, a) + " at base ball on Saturday.";
        }

        public static string Posted(string home) { return "Base ball at " + home + ", Saturday at two: Valentine against Strawberry. Ten cents, ladies free."; }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Pitches, Hits, Strikes }) foreach (string l in p) yield return l;
            foreach (string l in Cheers) yield return l.Replace("{t}", "Strawberry");
            yield return Innings("Valentine", "Strawberry", 7, 11, 9);
            yield return Result("Valentine", "Strawberry", 7, 11);
            yield return Posted("Strawberry");
        }
    }
}
