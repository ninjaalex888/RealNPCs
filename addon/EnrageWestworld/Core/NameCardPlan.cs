// What the name cards do how often (FIVEDAY2 0g; Lex's log of 2026-11-04: 61 spells over the frame budget, the cards the costliest part
// in 47). A card has to be drawn every frame or it flickers, so the saving is in the work round the drawing that nobody can see.
// A wall check is an expensive synchronous ray, so one a frame, and a far card doesn't need one as often (the card fades in and out,
// which hides the wait). What a card says was worked out for every card in the same pass when the three seconds came round, a spike
// in a crowd, so a few a pass now. The head bone follows the body closely enough between looks, and for longer the farther off.
// The small line under the name can't be read at a distance, and it doubles the text: the nearest few keep it.
// Plain C#, no game.
namespace EnrageWestworld.Core
{
    public static class NameCardPlan
    {
        public const int FillsAPass = 4, NewFillsAPass = 8, RaysAFrame = 1, SecondLines = 8;
        public const int RayNearMs = 250, RayFarMs = 500, BoneNearFrames = 3, BoneFarFrames = 6;
        public const float NearMetres = 12f, BoneNearMetres = 8f, SecondLineMetres = 18f;

        /// Whether this card's words are worked out in this pass: due, and room left in the pass. A new card has more room, so
        /// someone walking up gets a name at once and a crowd still can't ask for all of them together.
        public static bool MayFill(bool due, bool isNew, int fillsThisPass)
        {
            return due && fillsThisPass < (isNew ? NewFillsAPass : FillsAPass);
        }

        public static int RayEveryMs(float metres)
        {
            return metres < NearMetres ? RayNearMs : RayFarMs;
        }

        public static int BoneEveryFrames(float metres)
        {
            return metres < BoneNearMetres ? BoneNearFrames : BoneFarFrames;
        }

        /// The small line under the name: the nearest few, close enough to read.
        public static bool SecondLine(int drawnSoFar, float metres)
        {
            return drawnSoFar < SecondLines && metres <= SecondLineMetres;
        }

        /// Whether a body's existence and readiness are asked this frame: one frame in four, spread by handle.
        public static bool BodyCheckDue(int frame, int handle)
        {
            return ((frame + handle) & 3) == 0;
        }
    }
}
