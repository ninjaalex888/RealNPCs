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

    /// The name cards' own frame budget (HARDEN item 1; [Display] NameCardsBudgetUs, 400, above the worst ten seconds of that log; 0 = off, exactly as before). The cards were
    /// the costliest part of a frame in Lex's 11-09 log (0.2 to 0.34 ms of the add-on's 1 ms), and SEAMS put five more words on them,
    /// unplayed. What a frame of cards costs is measured whole, every frame, and a slow mean (about a third of a second) drives a
    /// ladder that sheds work in a fixed order, the invisible first, the way the control ring draws itself in:
    ///   1. stretch: fewer cards' words worked out a pass, the wall ray and the head bone looked at half as often (nothing you see);
    ///   2. the small line under the name goes (the name stays);
    ///   3. the farthest cards fade out, the nearest NearestKept keep theirs.
    /// It never skips or freezes a card that's up for a frame: a different card missing each frame is flicker, and a stale screen
    /// point lags when the camera turns. Up a step after RiseAfterMs over the budget; down a step after the ease wait under
    /// EaseShare of it. A step down that rises again within QuickRiseMs doubles the next wait (to MostEaseMs), so it doesn't see-saw
    /// between a line shown and hidden; a calm minute at the bottom puts the wait back. A frame far over (BackstopTimes the budget: a
    /// hitch, not the mean) lets the rest of that frame's cards wait, the only cut inside a frame, for a gross overrun. Plain C#.
    public sealed class CardBudget
    {
        public const int MostLevel = 3, RiseAfterMs = 2000, EaseAfterMs = 6000, MostEaseMs = 48000, QuickRiseMs = 30000, CalmMs = 60000;
        public const int NearestKept = 8, BackstopTimes = 8;
        public const double EaseShare = 0.65, MeanWeight = 0.05;   // a frame's weight in the mean: about twenty frames

        public int BudgetUs;
        public int Level { get; private set; }
        public double MeanUs { get; private set; }
        public int Rises { get; private set; }   // steps up this session (F7, and the log's line)
        public int EaseWaitMs { get { return _easeWaitMs; } }
        private int _overMs, _underMs, _easeWaitMs = EaseAfterMs, _sinceEaseMs = int.MaxValue / 2, _calmMs, _bandMs;
        public const int BandEaseTimes = 3;
        /// How long a card keeps being shed, or kept, before it may change (the review of item 1: two people swapping eighth and
        /// ninth place as they walk pulsed in and out every quarter second).
        public const int ShedHoldMs = 1000;

        public CardBudget(int budgetUs) { BudgetUs = budgetUs; }

        public bool On { get { return BudgetUs > 0; } }

        /// One frame of cards: what it cost (microseconds) and how long the frame was. True when the level changed.
        public bool Step(double frameUs, int dtMs)
        {
            int was = Level;
            if (!On)
            {
                Level = 0;
                MeanUs = 0;
                _overMs = _underMs = 0;
                return was != 0;
            }
            dtMs = System.Math.Max(0, System.Math.Min(250, dtMs));
            MeanUs = MeanUs <= 0 ? frameUs : MeanUs + (frameUs - MeanUs) * MeanWeight;
            if (_sinceEaseMs < int.MaxValue / 2)
                _sinceEaseMs += dtMs;
            if (MeanUs > BudgetUs)
            {
                _underMs = 0;
                _calmMs = 0;
                _overMs += dtMs;
                if (_overMs >= RiseAfterMs && Level < MostLevel)
                {
                    Level++;
                    Rises++;
                    _overMs = 0;
                    if (_sinceEaseMs < QuickRiseMs)
                        _easeWaitMs = System.Math.Min(MostEaseMs, _easeWaitMs * 2);   // it eased and came straight back: wait longer next time
                    _sinceEaseMs = int.MaxValue / 2;
                }
            }
            else if (MeanUs < BudgetUs * EaseShare)
            {
                _overMs = 0;
                _underMs += dtMs;
                if (Level == 0)
                {
                    _calmMs += dtMs;
                    if (_calmMs >= CalmMs)
                        _easeWaitMs = EaseAfterMs;   // a calm minute at the bottom: the wait back to its first length
                }
                if (_underMs >= _easeWaitMs && Level > 0)
                {
                    Level--;
                    _underMs = 0;
                    _sinceEaseMs = 0;
                }
            }
            else
            {
                // between the ease share and the budget: neither climbing nor easing, except that a long spell here eases a step
                // too, at a third of the pace (the review of item 1: a street whose floor sat in this band kept the line under
                // names off for good); a rise from that is the see-saw the doubled wait already slows
                _overMs = 0;
                _underMs = 0;
                _bandMs += dtMs;
                if (_bandMs >= _easeWaitMs * BandEaseTimes && Level > 0)
                {
                    Level--;
                    _bandMs = 0;
                    _sinceEaseMs = 0;
                }
                return Level != was;
            }
            _bandMs = 0;
            return Level != was;
        }

        /// Words worked out a pass at this level (a new card keeps its room: someone walking up still gets a name at once).
        public int FillsAPass(bool isNew)
        {
            if (isNew)
                return NameCardPlan.NewFillsAPass;
            return Level >= 1 ? System.Math.Max(1, NameCardPlan.FillsAPass / 2) : NameCardPlan.FillsAPass;
        }

        /// Whether this card's words are worked out in this pass at this level (NameCardPlan.MayFill's rule, the room per level).
        public bool MayFill(bool due, bool isNew, int fillsThisPass)
        {
            return due && fillsThisPass < FillsAPass(isNew);
        }

        /// How much longer the wall ray and the head bone wait at this level (twice from step 1).
        public int Stretch { get { return Level >= 1 ? 2 : 1; } }

        public bool SecondLines { get { return Level < 2; } }

        /// How many of the nearest keep their card at this level.
        public int Most(int most) { return Level >= 3 ? System.Math.Min(most, NearestKept) : most; }

        /// The backstop: this frame's cards so far cost so much that the rest wait a frame. Off with the budget.
        public bool Backstop(double usSoFar) { return On && usSoFar > (double)BudgetUs * BackstopTimes; }

        /// A pass's words cost this much already: the rest wait for the next pass (never a new card's first words).
        public bool FillsSpent(double usSoFar) { return On && usSoFar > BudgetUs * 2.0; }

        public static string Says(int level)
        {
            switch (level)
            {
                case 0: return "as normal";
                case 1: return "stretched (fewer cards' words a pass, the wall and head looks half as often)";
                case 2: return "names alone (the small line under them off)";
                default: return "the nearest " + NearestKept + " only (the farther fade out)";
            }
        }
    }
}
