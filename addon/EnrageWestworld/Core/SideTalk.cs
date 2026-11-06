// Who can strike up a quiet talk beside the street's scene (FIVEDAY item 5; SIXDAY For Lex 62: "the quiet talks of
// ScenesAtOnce never started in your 3.9 hours"). The quiet talks (Game/WestworldScript.Life.cs SideScene) pair two
// people within 13 m of the player; until settings 259 the two had to stand within 3.4 m of each other and be all but
// still (under 0.8 m/s), which in a town's street, where most people are walking somewhere, almost never happens: Lex's
// log of 2026-11-04 has 89 main scenes and not one talk beside them. Now two who are within calling distance (5.5 m, the
// near edge of what Hall called public distance: close enough to hail a neighbour and step together) and at most
// strolling (1.3 m/s, an unhurried walk) may stop and talk; the talk's first beat turns them to each other, which stops a
// stroller where he is, the way two people who know each other do when they meet in the street.
//
// Why it reads true: most of a town's talk is two people meeting by chance and stopping, not two who happened to be
// standing together already (Festinger's propinquity: who passes whom is who talks). Plain C#, no game; the offline
// measure is mindtest SideTalkTest.
using System;

namespace EnrageWestworld.Core
{
    public static class SideTalk
    {
        /// The farthest apart two may be ([Scenes] SideTalkMetres, 5.5; 3.4 until settings 259).
        public const float Metres = 5.5f;

        /// The fastest either may be moving: a stroll, not a hurry (1.3 m/s; 0.8 until settings 259).
        public const float StrollSpeed = 1.3f;

        /// After a quiet talk starts, the next may start this much later at the soonest (25 s): with the pairs found far more
        /// often now, a busy street would otherwise strike up a new one every look (5 s).
        public const int GapMs = 25000;

        /// Whether this one is unhurried enough to stop and talk.
        public static bool Unhurried(float speed)
        {
            return speed < StrollSpeed;
        }

        /// The settings' distance, kept between a handshake and a shout (2.5 to 8 m).
        public static float Clamp(float metres)
        {
            return Math.Max(2.5f, Math.Min(8f, metres));
        }
    }
}
