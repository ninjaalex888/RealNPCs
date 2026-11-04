// Greeting someone makes them fonder of you (Lex, 2026-11-03: "significantly boost the fondness meter for NPCs to really
// enjoy when the player greets them"). [Encounters] GreetLiking (12) of enRAGE's fondness (0-100: the bar under people
// with FeelingBars, what AlliesLiking reads, so the people you're civil to are the ones who come to your aid) on the first
// greeting of someone in a game day, a third of it on later greetings of the same person that day (a string of hellos
// doesn't buy a friend by noon), nothing from someone afraid of you or angry with you (fear or anger at 40 or more: a
// greeting from the man they fear is no comfort). enRAGE's own liking for a greeting stays as it is; this is on top,
// through its own method (LikeMore: an audited write). Noted at the greeting itself (PollGreets in WestworldScript.cs:
// the game's own event, whatever else is going on), counted three and a half seconds later when enRAGE has taken the
// person up; someone it hasn't yet is tried once more four seconds on. Log "GREET" (detail). F7 "TEST greet liking:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, long> _greetLikedOn = new Dictionary<string, long>();   // enRAGE id -> the game day of the last greeting that counted in full
        private readonly List<int[]> _greetLikeLater = new List<int[]>();   // { ped, due at, tries }
        private int _greetLiked, _greetLikedAgain, _greetLikeHeld;
        private string _greetLikeLast;

        /// A greeting just happened: counted in a few seconds, when enRAGE has the person.
        private void GreetFondnessSoon(int ped, int now)
        {
            if (_cfg == null || _cfg.GreetLiking <= 0 || ped == 0)
                return;
            if (_greetLikeLater.Count > 30)
                _greetLikeLater.RemoveAt(0);
            _greetLikeLater.Add(new[] { ped, now + 3500, 0 });
        }

        private void GreetFondness(int ped, int now, int tries)
        {
            if (_cfg == null || _cfg.GreetLiking <= 0 || ped == 0)
                return;
            try
            {
                FolkRef ac = _bridge.AcquaintanceAt(ped);
                if (ac == null || ac.Id == null)
                {
                    if (tries == 0)
                        _greetLikeLater.Add(new[] { ped, now + 4000, 1 });   // enRAGE may take them up a moment later
                    return;
                }
                float fear, like, anger;
                if (_bridge.FeelingsOf(ped, out fear, out like, out anger) && (fear >= 40f || anger >= 40f))
                {
                    _greetLikeHeld++;
                    return;
                }
                long day = Bridge.Now() / 1440, was;
                bool first = !_greetLikedOn.TryGetValue(ac.Id, out was) || was != day;
                int delta = first ? _cfg.GreetLiking : Math.Max(1, _cfg.GreetLiking / 3);
                if (_greetLikedOn.Count > 500)
                    _greetLikedOn.Clear();
                if (first)
                    _greetLikedOn[ac.Id] = day;
                _bridge.LikeMore(ac.Id, delta);
                if (first)
                    _greetLiked++;
                else
                    _greetLikedAgain++;
                string name = ac.Name ?? ac.Id;
                _greetLikeLast = name + " +" + delta + (first ? "" : " (again today)");
                Log.Detail("GREET " + name + " likes you " + delta + " more for the greeting" + (first ? "" : " (again today)") + (like > 0f ? " (was " + (int)like + ")" : ""));
            }
            catch (Exception ex)
            {
                Log.Detail("GREET the fondness couldn't be raised: " + ex.Message);
            }
        }

        /// The greetings due: counted now; someone enRAGE hadn't taken up is tried once more, four seconds on.
        private void GreetFondnessLater(int now)
        {
            for (int i = _greetLikeLater.Count - 1; i >= 0; i--)
            {
                int[] g = _greetLikeLater[i];
                if (now < g[1])
                    continue;
                _greetLikeLater.RemoveAt(i);
                if (ENTITY.DOES_ENTITY_EXIST(g[0]) && !ENTITY.IS_ENTITY_DEAD(g[0]))
                    GreetFondness(g[0], now, g[2]);
            }
        }

        private string GreetLikingStatus()
        {
            return _cfg.GreetLiking <= 0 ? "off ([Encounters] GreetLiking)"
                : "+" + _cfg.GreetLiking + " a first greeting in a day, +" + Math.Max(1, _cfg.GreetLiking / 3) + " again that day; " + _greetLiked + " first greetings counted, " + _greetLikedAgain + " repeats, " +
                  _greetLikeHeld + " held (afraid or angry)" + (_greetLikeLast != null ? "; last: " + _greetLikeLast : "");
        }
    }
}
