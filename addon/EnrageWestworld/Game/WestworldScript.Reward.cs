// A reward posted for a neighbour in game (FIVEDAY reserve R30; Core/NeighbourReward.cs, Core/Engine.Rewards.cs). Posted
// from the brawl's death (WestworldScript.BrawlDown.cs). Every three seconds in a town with one: the notice in the box once
// a town a day; near the wanted man (4 m) the J prompt (turn him in, warn him, leave it); every thirty seconds, a named
// person near thinks of it by their side of the split (or he of the price on his head). Behind [World] NeighbourReward.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _rewardNextLook, _rewardNextThought, _rewardThoughts, _rewardsTaken, _rewardsWarned, _rewardAskWho;
        private TownEvent _rewardAsk;
        private readonly Dictionary<string, long> _rewardNoticeDay = new Dictionary<string, long>();

        private void RewardStep(int now)
        {
            if (_cfg == null || !_cfg.NeighbourReward || _placeTown == null || _engine == null || _store == null || now < _rewardNextLook)
                return;
            _rewardNextLook = now + 3000;
            Resident w = _engine.RewardIn(_placeTown);
            if (w == null)
                return;
            int cents = _engine.RewardCents(w.Id);
            long day = Bridge.Now() / 1440, d;
            if (!_rewardNoticeDay.TryGetValue(_placeTown, out d) || d != day)
            {
                _rewardNoticeDay[_placeTown] = day;
                Notice(_placeTown, NeighbourReward.Notice(w.Name, _placeTown, cents, w.Id + "|" + day), "news");
                return;
            }
            Ped me = Game.Player.Character;
            bool thinkNow = now >= _rewardNextThought;
            if (thinkNow)
                _rewardNextThought = now + 30000;
            bool thought = false;
            foreach (int h in _mindNear)
            {
                MindWho mw;
                if (!_mindWho.TryGetValue(h, out mw) || mw.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (mw.Id == w.Id && new Ped(h).Position.DistanceTo(me.Position) <= NeighbourReward.NearMetres && _eventMenu == null
                    && (_eventPrompt == null || _eventPrompt == NeighbourReward.Kind || now > _eventPromptUntil))
                {
                    if (_rewardAsk == null || _rewardAsk.AId != w.Id)
                        _rewardAsk = new TownEvent { Kind = NeighbourReward.Kind, Town = _placeTown, AId = w.Id, AName = w.Name, Count = cents };
                    _rewardAskWho = h;
                    PromptSet(NeighbourReward.Kind, _rewardAsk, NeighbourReward.PromptMs);
                }
                if (!thinkNow || thought || !Voice.Chance(mw.Id + "|rw|" + day, NeighbourReward.ThoughtChance))
                    continue;
                string line = mw.Id == w.Id ? NeighbourReward.WantedThought(cents, mw.Id + "|" + day)
                            : NeighbourReward.Thought(NeighbourReward.Side(_store.FindResident(mw.Id)), w.Name, cents, mw.Id + "|" + day);
                _ticker.Push((mw.Name ?? "Someone") + ", thinking", line, "near", h);
                _rewardThoughts++;
                thought = true;
            }
        }

        /// The J menu's choice: turn him in (paid, to the county jail), warn him (he runs), or leave it.
        private void RewardChoice(TownEvent e, string what)
        {
            if (e == null || e.Kind != NeighbourReward.Kind || what == "leave")
                return;
            Resident r = _store.FindResident(e.AId);
            if (r == null || r.Left || r.Fled || r.Dead)
                return;
            int now = Game.GameTime, h = _rewardAskWho;
            bool there = h != 0 && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h);
            if (what == "collect")
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(e.Count, 0);
                _engine.RewardEnded(r, Bridge.Now(), true);
                _rewardsTaken++;
                Notice(e.Town, NeighbourReward.TakenNotice(r.Name, e.Count, r.Id), "news");
            }
            else if (what == "warn")
            {
                if (there)
                    SpeechCaption(Person.FirstOf(r.Name), NeighbourReward.Thank(r.Id), h, 6000, true, false);
                _engine.RewardEnded(r, Bridge.Now(), false);
                _rewardsWarned++;
                Notice(e.Town, NeighbourReward.RanNotice(r.Name, e.Town, r.Id), "news");
            }
            if (there)
                WanderLater(h, now + 4000);   // he goes his way; not cast again (Left, Fled)
            _rewardAsk = null;
        }

        private string RewardStatus()
        {
            return "reward: " + (_cfg != null && _cfg.NeighbourReward ? "on" : "off") + ", posted " + (_engine != null ? _engine.RewardsPosted : 0) +
                   " this session, turned in " + _rewardsTaken + ", warned " + _rewardsWarned + ", thoughts " + _rewardThoughts;
        }
    }
}
