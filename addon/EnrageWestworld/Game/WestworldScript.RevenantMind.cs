// A revenant the model can ask, in the game (FIVEDAY2 item 11; Core/RevenantMind.cs; [Stalkers] RevenantsMinds). Every so often while someone you killed is within reach, what they do
// next is put to the model, and to the rules if the model is off, slow or answers nonsense. Only the stalker's own moves are offered, so whatever comes back is something he already knows how to do.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string _revKey;
        private int _revNextAt, _revDeadline, _revAsks, _revPicked, _revRules;
        private List<string> _revMenu;
        private LanguageRequest _revRequest;
        private Fallen _revFor;
        private string _revLast;

        private void RevenantThink(int now)
        {
            Fallen f = _stalker != null ? _stalker.Revenant : null;
            if (f == null || !_cfg.RevenantsMinds)
                return;
            if (f != _revFor)
            {
                _revFor = f;
                _revKey = null;
                _revAsks = 0;
                _revNextAt = now + 8000;
            }
            Ped me = Game.Player.Character;
            if (_revKey != null)
            {
                string answer = null;
                long ms = 0;
                bool came = _engine != null && _engine.LanguageAnswers.TakePick(_revKey, out answer, out ms);
                if (!came && now < _revDeadline && now >= _revDeadline - RevenantMind.AskWaitMs - 1000)
                    return;
                _revKey = null;
                string thought = null;
                int idx = came ? Language.ParseThinkPick(answer, _revMenu.Count, out thought) : -1;
                RevenantDo(f, me, idx >= 0 ? _revMenu[idx] : null, idx >= 0, thought, now);
                return;
            }
            if (now < _revNextAt || _revAsks >= RevenantMind.MostAsks || !_stalker.MayDecide || _stalker.Handle == 0)
                return;
            float metres = _stalker.MetresFrom(me);
            if (metres > RevenantMind.ReachMetres)
                return;
            _revNextAt = now + RevenantMind.ThinkEveryMs;
            _revAsks++;
            _revMenu = RevenantMind.Menu(metres, _revAsks);
            if (PicksOn)
            {
                _revRequest = RevenantMind.Request(f, _placeTown, CLOCK.GET_CLOCK_HOURS(), Voice.NowYear, metres, _revMenu);
                string key = "rev|" + f.Id + "|" + now;
                if (_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = f.Name ?? "a revenant", PersonId = f.Id, Request = _revRequest, AskedAtMs = Environment.TickCount }))
                {
                    _revKey = key;
                    _revDeadline = now + RevenantMind.AskWaitMs;
                    return;
                }
            }
            RevenantDo(f, me, null, false, null, now);
        }

        /// The choice made: the model's if there is one, else the rules'.
        private void RevenantDo(Fallen f, Ped me, string picked, bool byModel, string thought, int now)
        {
            if (_stalker == null || !_stalker.MayDecide)
                return;
            float metres = _stalker.MetresFrom(me);
            string what = picked ?? RevenantMind.Rules(_revMenu, metres, _revAsks, f.Id + "|" + now / 20000);
            if (byModel)
                _revPicked++;
            else
                _revRules++;
            _revLast = (f.Name ?? "a revenant") + ": " + RevenantMind.WordsOf(what) + (byModel ? " (the model)" : " (the rules)");
            Log.Write("REVENANT " + _revLast);
            _stalker.Decide(what, me, now);
        }

        private string RevenantMindStatus()
        {
            return (_cfg.RevenantsMinds ? "on" : "off ([Stalkers] RevenantsMinds)") + "; " + _revPicked + " chosen by the model, " + _revRules + " by the rules this session" + (_revLast != null ? "; last: " + _revLast : "");
        }
    }
}
