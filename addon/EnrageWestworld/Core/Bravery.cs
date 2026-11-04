// How brave the town is ([World] Bravery, 0-100; Lex, 2026-10-31: "it would be nice if we tuned down the amount of fleeing the
// NPCs do, we should make all NPCs much, much more brave, even though it may not be super realistic for the time of 1899,
// it's close and it's also a game so it will be more exciting for the player"). Since 2026-11-04 (Lex: "Can we have it so
// bravery only effects their chance to join a brawl - and not the flee or cower flag?") it never touches the game's own
// flight: the config flag the game layer used to set on the brave (PCF 44, cower instead of flee) froze people where they
// stood, twice in Lex's sessions, and BraveStand, meant to stand them up, never fired. What it does now: how readily an
// onlooker joins a fistfight (JoinPull, into JoinIn.BraveryPull: 85 as tuned on 2026-11-01, higher more, lower fewer),
// and the mod's own fear-driven running (Onlooking's hurry and contagion, the scattered gatherings, the flight from a
// drawn gun), which brave ones answer by standing back and watching (no flag: the mod simply doesn't send them). Brave,
// LeftAlone and Decide are kept for the tests. Seeded per person, so the same man answers the same way. Plain C#.
using System;

namespace EnrageWestworld.Core
{
    public static class Bravery
    {
        /// As built: most hold their ground, the odd one in seven still runs.
        public const int AsBuilt = 85;

        /// Bravery as a share, 0..1.
        public static float Share(int bravery)
        {
            return Math.Max(0, Math.Min(100, bravery)) / 100f;
        }

        /// Whether this body is one of the brave (by its handle and model: a handle the game gives someone else is asked again).
        public static bool Brave(int bravery, int handle, uint model)
        {
            if (bravery <= 0)
                return false;
            if (bravery >= 100)
                return true;
            return (Voice.Hash("brave|" + handle + "|" + model) & 0x7fffffff) % 100 < bravery;
        }

        /// Whether someone the mod would send running holds his ground instead (seeded by who and what).
        public static bool Holds(int bravery, string seed)
        {
            return bravery > 0 && Voice.Chance(seed + "|holds", Math.Max(0, Math.Min(100, bravery)));
        }

        /// Who this ped is to the sweep: why the game's own ways are left alone for him (null: he's one the sweep may make
        /// braver). Never a child, a story model, a lawman, another script's (a mission's, a shop's) person, one of
        /// enRAGE's people, one of the mod's own (spawned, borrowed, the test cast), the dead, or someone Arthur has a gun
        /// or the lock-on on (hold-ups and robbery run on the game's own answer).
        public static string LeftAlone(bool child, bool grownUp, bool story, bool law, bool otherScripts, bool record, bool ours, bool dead, bool aimedAt)
        {
            if (dead) return "dead";
            if (child || !grownUp) return "not a grown-up";
            if (story) return "a story character";
            if (law) return "a lawman";
            if (otherScripts) return "another script's";
            if (record) return "one of enRAGE's people";
            if (ours) return "one of the mod's own";
            if (aimedAt) return "you have him in your sights";
            return null;
        }

        /// The decision for one ped: "brave" (the flag set), "as the game has it" (a coward, or bravery 0), or why he's left
        /// alone.
        public static string Decide(int bravery, int handle, uint model, bool child, bool grownUp, bool story, bool law, bool otherScripts, bool record, bool ours, bool dead, bool aimedAt)
        {
            string why = LeftAlone(child, grownUp, story, law, otherScripts, record, ours, dead, aimedAt);
            if (why != null)
                return why;
            return Brave(bravery, handle, model) ? "brave" : "as the game has it";
        }

        // ---- joining a fight (Core/JoinIn.cs) ----

        /// What bravery adds to an onlooker's pull to join a fistfight (JoinIn.BraveryPull, set by the game side): nothing at 85
        /// as built (the joining as tuned on 2026-11-01), up to a fifth more pull at 100, down to a quarter less at 0 (then only
        /// a fighter's friend or foe steps in).
        public static float JoinPull(int bravery)
        {
            int b = Math.Max(0, Math.Min(100, bravery));
            return b >= AsBuilt ? 0.20f * (b - AsBuilt) / (100 - AsBuilt) : -0.25f * (AsBuilt - b) / AsBuilt;
        }

        // ---- the mod's own running, scaled (Core/Onlooking.cs) ----

        /// How strong a fright must be before an onlooker hurries off (Appraisal.Strong as built; up toward the top with
        /// bravery, so at 85 only near the worst fright runs).
        public static float HurryBar(int bravery)
        {
            return Appraisal.Strong + (1f - Appraisal.Strong) * 0.9f * Share(bravery);
        }

        /// How strong a stranger's fright must be to walk off from someone else's trouble (Onlooking.LeaveBar as built).
        public static float LeaveBar(int bravery)
        {
            return Onlooking.LeaveBar + 0.4f * Share(bravery);
        }

        /// How many run from one happening when one does (Onlooking.MostRunning as built, one at 85).
        public static int MostRunning(int bravery)
        {
            return bravery <= 0 ? Onlooking.Running : Math.Max(1, (int)Math.Round(Onlooking.Running * (1f - Share(bravery))));
        }
    }
}
