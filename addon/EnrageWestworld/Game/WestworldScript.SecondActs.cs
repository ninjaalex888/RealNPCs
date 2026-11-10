// Second acts, the game side (FOURTEENHOUR reserve R3; Core/SecondActs.cs; [Encounters] SecondActs). Three hooks into runners that
// are already there: the friend calls' walk (WestworldScript.FriendCalls.cs: a man you saved comes over, and the first time after it
// it's thanks for that very deed), the kin grudges' walk-up (an execution you found and never stepped into: one of the dead man's
// menfolk comes to say so; Engine.KinWatched from ExecutionDead), and the executions' plan (captors who got away after you saved
// their man: the news, and the region's next outlaws are the same men; Engine.CaptorsAtLarge from EndExecution). Here: the Testing
// row and F7. Never a frame of its own.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string _secondTestId;   // the Testing row's: the one the friend call may take though they're no friend
        private int _secondThanked;
        private string _secondLast;

        /// The Testing row: the nearest person the mind knows within 20 m, as if you'd saved them from the rope yesterday (a test
        /// memory, taken away again once the line is made), comes over to thank you. Nothing handed over, nothing kept.
        private string SecondActTest(int now)
        {
            if (!SecondActs.On)
                return "off ([Encounters] SecondActs)";
            if (!_cfg.FriendCalls)
                return "it rides on the friend calls: [Scenes] FriendCalls is off";
            if (_friendWalk != null)
                return "a friend is already on the way";
            Ped me = Game.Player.Character;
            MindWho pick = null;
            foreach (Ped p in Peds.All().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 20f).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                MindWho w = MindKnown(p.Handle, now);
                if (w != null && w.Id != null && _engine.CanRemember(w.Id) && MayDirectWhyNot(p) == null && !NotFree(p.Handle))
                {
                    pick = w;
                    break;
                }
            }
            if (pick == null)
                return "nobody the mind knows within 20 m, free";
            PersonState st = _store.Find(pick.Id);
            if (st == null)
                return "nothing kept on " + (pick.Name ?? "them") + " yet";
            if (st.Mind == null)
                st.Mind = new List<MindMemory>();
            long g = _store.LastGameMin;
            var test = new MindMemory { Kind = "warmth", Sub = "defend", Who = new List<string> { "player" }, Names = new List<string> { "the stranger" }, Town = _placeTown, At = g - 1440,
                                        How = "did", Role = "done", Feel = 3, Strength = 30, Key = "saved:" + pick.Id + ":test:rope" };
            st.Mind.Add(test);
            _secondTestId = pick.Id;
            _nextFriendCall = 0;
            string why;
            try
            {
                why = FriendLook(me, now, true);
            }
            finally
            {
                st.Mind.Remove(test);   // the line is made: the test memory goes again
                _secondTestId = null;
            }
            return why;
        }

        /// From the friend's walk, as the saved one says it to your face.
        private void SecondActThanked(string name, string line)
        {
            _secondThanked++;
            _secondLast = name + ": " + line;
        }

        /// F7: the second acts this session, and the engine's.
        private string SecondActsStatus()
        {
            if (!SecondActs.On)
                return "off ([Encounters] SecondActs)";
            return _secondThanked + " of those you saved came back to thank you; " + _engine.SecondActsStatus() + (_secondLast != null ? "; last: " + _secondLast : "");
        }
    }
}
