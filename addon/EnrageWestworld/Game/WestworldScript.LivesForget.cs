// The lives round's memory-only marks, forgotten when a save is loaded (the track 4 review): a gift in your pocket or a note
// carried belong to the save they were bought or taken in (a load gives the money back, so the gift would be free), and the
// doctor's "once a day" and the asked lists are by game day, which a load turns back. And a free prompt: none up, or one that
// has run out (PromptSet never clears an expired one; BarRound and WalkHome gate the same way).
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private void LivesForget()
        {
            _gift = null;
            _giftOffered = false;
            _giftLastTo.Clear();
            _giftStoreVisit = null;
            _courtNote = null;
            _courtGiveOffered = false;
            _courtAsked.Clear();
            _doctorsCallDays.Clear();
            _doctorsCallDoctor = null;
        }

        private bool PromptFree()
        {
            return _eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil);
        }
    }
}
