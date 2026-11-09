// A revenant the model can ask, in the game (FIVEDAY2 item 11; Core/RevenantMind.cs; [Stalkers] RevenantsMinds). Every so often while someone you killed is within reach, what they do
// next is put to the model, and to the rules if the model is off, slow or answers nonsense. Only the stalker's own moves are offered, so whatever comes back is something he already knows how to do.
using System;
using System.Collections.Generic;
using System.Linq;
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
        private bool _revForceAttack, _revTest;   // the Testing row's: go for you as soon as they can, and nothing of it kept

        /// A revenant's visit is over, one way or another: what's kept for the visit goes with it, so the next one starts clean.
        private void RevenantReset()
        {
            _revForceAttack = false;
            _revTest = false;
            _revFor = null;
            _revKey = null;
        }

        /// You died with one of them after you: they've had what they came for.
        private void RevenantHadYou(int now)
        {
            Ped me = Game.Player.Character;
            if (_stalker == null || !_stalker.Hunted || _stalker.Done || me == null || !me.Exists() || !me.IsDead)
                return;
            _stalker.HadYou();
            EndStalker(now);
        }

        /// Word of him ahead of him (item 13): when someone you killed is due back within half a game day and you're in a town, it's said there once.
        private void RevenantWord(long gameMin)
        {
            if (!_cfg.RevenantsHunt || _placeTown == null || _store.Fallen == null)
                return;
            foreach (Fallen f in _store.Fallen)
            {
                if (f.Done || f.ReturnAt <= 0 || gameMin < f.ReturnAt - 720 || gameMin >= f.ReturnAt || _store.Counts.ContainsKey("rev:word:" + f.Id))
                    continue;
                _store.Counts["rev:word:" + f.Id] = 1;
                Notice(_placeTown, RevenantMind.AskingAfter(f, _placeTown, f.Id), "news");
                Log.Write("REVENANT word of " + (f.Name ?? f.Id) + " reaches " + _placeTown + " ahead of them");
                return;
            }
        }

        /// The Testing row: someone you killed, or someone like them, turns up behind you and goes for you as soon as they can.
        private string RevenantTest(int now, out string said)
        {
            said = null;
            if (_stalker != null)
                return "a stalker is out already";
            Fallen f = _store.Fallen != null ? _store.Fallen.LastOrDefault(x => !x.Done && x.Model != 0) : null;
            if (f == null)
                f = new Fallen { Id = "test:revenant", Model = Spawner.ModelFor(null, false, _rng.Next()), Label = "a ranch hand", Town = _placeTown ?? "Valentine", Weapon = "a revolver", Held = "you robbed me" };
            _revForceAttack = true;
            _revTest = true;
            BeginStalker(f, f.Model, f.Woman, f.Name ?? "A familiar man", now, true);
            said = "someone you killed is behind you; stand still a moment, and they'll come";
            return null;
        }

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
            _revMenu = RevenantMind.Menu(metres, _revAsks, _cfg.RevenantsHunt);
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
            string what = picked ?? RevenantMind.Rules(_revMenu, metres, _revAsks, f.Id + "|" + now / 20000, _cfg.RevenantsHunt ? RevenantMind.AttackChance(_cfg.RevenantHuntChance, f) : 0);
            if (_revForceAttack && _revMenu.Contains(RevenantMind.Attack))
                what = RevenantMind.Attack;
            if (byModel)
                _revPicked++;
            else
                _revRules++;
            _revLast = (f.Name ?? "a revenant") + ": " + RevenantMind.WordsOf(what) + (byModel ? " (the model)" : " (the rules)");
            Log.Write("REVENANT " + _revLast);
            _stalker.Decide(what, me, now);
            RevenantSpeaks(f, what, byModel ? thought : null, now);
        }

        /// His own words as he does it: the model's, held to the check every line of the model's is held to, else the written ones.
        private void RevenantSpeaks(Fallen f, string what, string fromModel, int now)
        {
            string seed = f.Id + "|" + now / 20000;
            if (_stalker != null && what == RevenantMind.Attack)
            {
                StalkerSay(_stalker.Label, RevenantMind.Attacks(f, seed), true);
                return;
            }
            if (_stalker == null || !RevenantMind.Speaks(seed))
                return;
            string line = fromModel != null && what != RevenantMind.Follow && _revRequest != null && Language.Check(_revRequest, fromModel) == null ? fromModel : RevenantMind.Says(what, f, seed);
            if (line != null)
                StalkerSay(_stalker.Label, line, true);
        }

        private string RevenantMindStatus()
        {
            return (_cfg.RevenantsMinds ? "on" : "off ([Stalkers] RevenantsMinds)") + "; " + _revPicked + " chosen by the model, " + _revRules + " by the rules this session" + (_revLast != null ? "; last: " + _revLast : "");
        }
    }
}
