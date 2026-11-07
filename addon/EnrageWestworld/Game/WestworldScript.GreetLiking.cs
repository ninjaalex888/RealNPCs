// Greeting someone makes them fonder of you (Lex, 2026-11-03: "significantly boost the fondness meter for NPCs to really
// enjoy when the player greets them"; then "the friend/ally status from 4 days to upon 2nd greet, even if it's the same
// day", then, two hellos having filled only half the bar, "each greet gives 25% fondness boost for now until we add in more
// NPC interactions", then "3 greets will get to 100%"). [Encounters] GreetLiking (34) of enRAGE's fondness (0-100: the bar
// under people with FeelingBars, what AlliesLiking reads, so the people you're civil to are the ones who come to your aid)
// on every greeting of someone (three hellos fill the bar and pass AlliesLiking 90: a friend who'll stand with you), nothing from someone afraid of you or
// angry with you (fear or anger at 40 or more: a greeting from the man they fear is no comfort). More ways to grow the
// fondness are the roadmap's next item. enRAGE's own liking for a greeting stays as it is; this is on top,
// through its own method (LikeMore: an audited write). Noted at the greeting itself (PollGreets in WestworldScript.cs:
// the game's own event, whatever else is going on), counted three and a half seconds later when enRAGE has taken the
// person up; someone it hasn't yet is tried once more four seconds on. Log "GREET" (detail). F7 "TEST greet liking:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, long[]> _greetLikedOn = new Dictionary<string, long[]>();   // enRAGE id -> { the game day, greetings counted that day }
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
                long day = Bridge.Now() / 1440;
                long[] on;
                if (!_greetLikedOn.TryGetValue(ac.Id, out on) || on[0] != day)
                {
                    if (_greetLikedOn.Count > 500)
                        _greetLikedOn.Clear();
                    _greetLikedOn[ac.Id] = on = new[] { day, 0L };
                }
                on[1]++;   // hellos today, for the log
                int delta = _cfg.GreetLiking;   // every greeting in full (Lex: "each greet gives 25% fondness boost for now")
                string wrongNote = null;
                if (_cfg.TrustSlow)
                    delta = TrustedDelta(ped, delta, out wrongNote);   // someone you wronged: less, until time and kindness bring them back (FIVEDAY item 20)
                _bridge.LikeMore(ac.Id, delta);
                if (on[1] <= 1)
                    _greetLiked++;
                else
                    _greetLikedAgain++;
                string name = FolkShown(ac) ?? ac.Id;
                _greetLikeLast = name + " +" + delta + (on[1] > 1 ? " (hello " + on[1] + " today)" : "");
                Log.Detail("GREET " + name + " likes you " + delta + " more for the greeting" + (on[1] > 1 ? " (hello " + on[1] + " today)" : "") + (like > 0f ? " (was " + (int)like + ")" : "") + (wrongNote != null ? " (" + wrongNote + ")" : ""));
            }
            catch (Exception ex)
            {
                Log.Detail("GREET the fondness couldn't be raised: " + ex.Message);
            }
        }

        private int _trustSlowed, _trustUndone;

        /// A kindness's worth to someone you wronged (Core/Trust.cs): the full delta with nothing between you; else scaled by the
        /// days since and the kindnesses since, and the kindness counted toward the mending.
        private int TrustedDelta(int ped, int delta, out string note)
        {
            note = null;
            string id, name;
            if (!LastingId(new Ped(ped), out id, out name) || id == null)
                return delta;
            long g = Bridge.Now();
            Deed wrong = Trust.LastWrong(_store.Deeds, id, g, name);
            if (wrong == null)
                return delta;
            MindWho w = MindKnown(ped, Game.GameTime);
            Traits t = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
            string key = Trust.MendKey(id, wrong);
            int since;
            _store.Mends.TryGetValue(key, out since);
            float f = Trust.Factor(t, g - wrong.At, since);
            _store.Mends[key] = since + 1;
            int d = (int)Math.Round(delta * f);
            if (d < delta)
            {
                _trustSlowed++;
                note = "your " + wrong.Kind + " " + ((g - wrong.At) / 1440) + " days ago: " + (int)(f * 100) + "% of it, kindness " + (since + 1) + " since, forgiving in " + Trust.ForgiveDays(t) + " days";
            }
            return d;
        }

        /// A wrong of his just done to someone enRAGE knows: one wrong undoes three kindnesses on the bar, at once (FIVEDAY item 20).
        private void TrustWronged(Deed d, int victim)
        {
            if (_cfg == null || !_cfg.TrustSlow || _cfg.GreetLiking <= 0 || d == null || victim == 0 || !Trust.IsWrong(d.Kind))
                return;
            try
            {
                FolkRef ac = _bridge.AcquaintanceAt(victim);
                if (ac == null || ac.Id == null)
                    return;
                _bridge.LikeMore(ac.Id, -Trust.Undoes * _cfg.GreetLiking);
                _trustUndone++;
                Log.Write("TRUST " + (FolkShown(ac) ?? ac.Id) + ": your " + d.Kind + " undoes " + Trust.Undoes + " greetings' worth (-" + Trust.Undoes * _cfg.GreetLiking + ")");
            }
            catch (Exception ex)
            {
                Log.Detail("TRUST the bar couldn't be lowered: " + ex.Message);
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
                : "+" + _cfg.GreetLiking + " on every greeting (a friend at " + _cfg.AlliesLiking + "); " + _greetLiked + " first greetings of a day counted, " + _greetLikedAgain + " repeats, " +
                  _greetLikeHeld + " held (afraid or angry)" + (_greetLikeLast != null ? "; last: " + _greetLikeLast : "") +
                  (_cfg.TrustSlow ? "; trust: " + _trustSlowed + " counted for less (a wrong of yours), " + _trustUndone + " wrongs undid three greetings" : "; trust off ([Mind] TrustSlow)");
        }
    }
}
