// Arthur's own fighting words (NIGHT3 item 7; roadmap 34; Lex, 2026-10-31). The game has recorded lines for Arthur that it
// rarely plays (the Cut Dialogue Restoration and Enhancement mod is built on them: his taunts in a fight, at the law, at
// one man or several, near or far). They're the game's own recordings, played by name, so RealNPCs uses them too, with
// nothing taken from that mod: when someone comes at Arthur (the self-defence watch first sees it: a waking man lashing
// out, a joiner, a mugger) and when he catches the pickpocket himself. Never over a line already playing on him (that
// mod's, the game's, ours), and 20 s between. [World] ArthurSpeaks. Log ARTHUR says; Testing row "Arthur's own line".
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private static readonly string[] ArthurAttacked = { "PLAYER_TAUNT_SINGLE_ENEMY_NEG_NEAR", "PLAYER_TAUNT_SINGLE_ENEMY_POS_NEAR", "GENERIC_CURSE_HIGH" };
        private static readonly string[] ArthurCaught = { "PLAYER_TAUNT_SINGLE_ENEMY_POS_NEAR", "PLAYER_TAUNT_SINGLE_ENEMY_POS_FAR", "GENERIC_CURSE_MED" };
        private const int ArthurGapMs = 20000;
        private int _arthurNextAt, _arthurSaid, _arthurBusy, _arthurNone, _arthurTest;
        private string _arthurLast;

        /// A line of Arthur's own for this moment ("attacked", "caught"), at someone (0: nobody); true when one played.
        private bool ArthurSays(string moment, int at, bool test = false)
        {
            if (!test && (!_cfg.ArthurSpeaks || Game.GameTime < _arthurNextAt))
                return false;
            int me = Game.Player.Character.Handle;
            if (Speech.Busy(me))
            {
                _arthurBusy++;
                Log.Detail("ARTHUR: a line was already playing on him (" + moment + "): left alone");
                return false;
            }
            string line, words;
            int take;
            bool played = Speech.PlayFirst(me, moment == "caught" ? ArthurCaught : ArthurAttacked, at, out line, out take, out words);
            _arthurNextAt = Game.GameTime + ArthurGapMs;
            if (played)
            {
                _arthurSaid++;
                _arthurLast = moment + ": " + line;
            }
            else
                _arthurNone++;
            Log.Write("ARTHUR says " + (played ? line : "nothing: his voice had none of the lines for it") + " (" + moment + (test ? ", the Testing row" : "") + ")");
            return played;
        }

        private string ArthurTest(int now)
        {
            string moment = _arthurTest++ % 2 == 0 ? "attacked" : "caught";
            return ArthurSays(moment, 0, true) ? null : "no line played for " + moment + " (the log says why)";
        }

        private string ArthurStatus()
        {
            return (_cfg.ArthurSpeaks ? "on" : "off ([World] ArthurSpeaks)") + "; this session " + _arthurSaid + " said, " + _arthurBusy + " left (a line already playing), " + _arthurNone + " none in his voice" +
                   (_arthurLast != null ? "; last: " + _arthurLast : "");
        }
    }
}
