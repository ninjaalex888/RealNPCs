// Thieves choose their marks (2026-11-11; a player on Nexus, 2026-11-10: "pickpocketed pretty much every time I visited" a
// town; [Scenes] LiftsWary). The lift (WestworldScript.Lift.cs: a man bumps into you and takes some of your cash) was paced at
// two game hours between anywhere and a town every four, so with a look a minute at 25% nearly every visit to a town had one.
// Two changes, both behind the one switch:
//   - rarer: 24 game hours between anywhere, 48 in the same town (a game hour is two real minutes, so about one an hour
//     of play; the storyteller may halve the 24 when it wants more going on, Pacing.KindRestNow, never the town's 48). "A town once a day" alone did little: a player riding between three towns is back in each a game day
//     later anyway (the test's model: 112 lifts a hundred visits as built, 95 with that).
//   - choosier: a pickpocket weighs the mark before the bump. The psychology is plain risk: a thief goes for the soft-looking
//     and the distracted, and a man the town knows has killed is a bad risk however full his pockets. So in a town where your
//     killings are known (Engine.StandingOf, the same count the Around town box shows), fewer of them try it.
// Lex's 10-30 rule stands: a man who has picked your pocket stays a pickpocket and is the first sent again.
// Off in Core so the harness goldens stand; ApplySettings sets it from [Scenes] LiftsWary.

namespace EnrageWestworld.Core
{
    public static class LiftNerve
    {
        public static bool On;

        /// The lift's pacing: as built (2026-10-29), and the rarer one this switch brings.
        public static readonly Pacing.Rule AsBuilt = new Pacing.Rule("lift", false, 25, 120, 240, 0);
        public static readonly Pacing.Rule Rarer = new Pacing.Rule("lift", false, 25, 1440, 2880, 0);

        /// Puts the switch's pacing in the table (ApplySettings, and the test).
        public static void Apply(bool on)
        {
            On = on;
            Pacing.Rules["lift"] = on ? Rarer : AsBuilt;
        }

        /// Of the thieves who would try it, the share who still do (percent), given the killings of yours this town knows.
        public static int TriesPercent(int killsKnown)
        {
            return TriesPercent(killsKnown, 0);
        }

        /// The same, and the thieves you've run down in this town lately (the "thiefcaught" deed, a week): a man known to run
        /// pickpockets down is a bad mark too, and it's the thieves' own talk that spreads it (DEEPEN item 5).
        public static int TriesPercent(int killsKnown, int caughtHere)
        {
            if (!On)
                return 100;
            int p = killsKnown <= 0 ? 100 : killsKnown == 1 ? 60 : killsKnown <= 3 ? 35 : 15;
            if (caughtHere >= 2)
                p = p * 35 / 100;
            else if (caughtHere == 1)
                p = p * 60 / 100;
            return p;
        }

        public const long CaughtDays = 7;   // a catch of yours is remembered by the town's thieves this many game days
    }
}
