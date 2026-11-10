// Lawmen we send on a job (a body off the street, a fight broken up) are taken over properly (Lex, 2026-10-31: the game
// took them back before they got there: 14 of 15 bodies "never reached it", and the one lifted was set down 3 s later).
// Ours while they work, as the Spawner takes its own (Borrow: a mission entity), or, one another script already owns,
// directed lightly (BorrowLight: never "no longer needed" after, so the game never clears a ped another script made);
// their task kept; handed back with Spawner.Release, after a delay when there is a rope to finish. Every take says
// which it was in the log, so a lawman the game still takes back can be told apart.
using System.Collections.Generic;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly List<KeyValuePair<int, int>> _lawGiveBack = new List<KeyValuePair<int, int>>();   // handle, when

        /// Takes him over for a job; true when another script already had him (taken lightly).
        private bool LawTake(int h)
        {
            bool owned = Natives.OtherScripts(h) && !Spawner.IsBorrowed(h);   // a mission entity of someone else's (not the test cast)
            Ped p = new Ped(h);
            if (!Spawner.IsBorrowed(h))
            {
                if (owned)
                    Spawner.BorrowLight(p);
                else
                    Spawner.Borrow(p);
            }
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            for (int i = _lawGiveBack.Count - 1; i >= 0; i--)
                if (_lawGiveBack[i].Key == h)
                    _lawGiveBack.RemoveAt(i);   // taken again before an earlier hand-back came due
            return owned;
        }

        private static string LawTakeWords(bool owned)
        {
            return owned ? "another script already had him: directed lightly" : "ours while he works";
        }

        /// Hands him back to the game at `at` (now, or after the rope); `keepTasks`: his own fight (with the player) stands.
        private void LawGiveBack(int h, int at, bool keepTasks = false)
        {
            if (!Spawner.IsBorrowed(h))
                return;
            if (keepTasks)
                Spawner.LeaveTasks(h);
            for (int i = _lawGiveBack.Count - 1; i >= 0; i--)
                if (_lawGiveBack[i].Key == h)
                    _lawGiveBack.RemoveAt(i);
            _lawGiveBack.Add(new KeyValuePair<int, int>(h, at));
        }

        /// A brawl's lawman done: handed back at `at` (after the rope); one fighting the player keeps that fight.
        private void LawRoundsAgain(int h, int at)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                return;
            bool atYou = PED.IS_PED_IN_COMBAT(h, Game.Player.Character.Handle);
            if (Spawner.IsBorrowed(h))
            {
                LawGiveBack(h, atYou ? Game.GameTime : at, atYou);
                return;
            }
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            if (!atYou)
                WanderLater(h, at);
        }

        private void LawGiveBackStep(int now)
        {
            for (int i = _lawGiveBack.Count - 1; i >= 0; i--)
            {
                if (now < _lawGiveBack[i].Value)
                    continue;
                int h = _lawGiveBack[i].Key;
                _lawGiveBack.RemoveAt(i);
                if (Spawner.IsBorrowed(h) && !(_bodyClear != null && _bodyClear.Law == h) && !_brawlLaw.Exists(x => x.Law == h) && !(_domestic != null && _domestic.Law != null && _domestic.Law.Handle == h))
                {
                    if (ENTITY.DOES_ENTITY_EXIST(h) && PED.IS_PED_IN_COMBAT(h, Game.Player.Character.Handle))
                        Spawner.LeaveTasks(h);   // he's turned on you since the hand-back was set: his fight stands (the review of items 22-26)
                    Spawner.Release(new Ped(h), false);   // back to his rounds (GiveBack: stood up, unblocked, tasks cleared, wandering)
                }
            }
        }
    }
}
