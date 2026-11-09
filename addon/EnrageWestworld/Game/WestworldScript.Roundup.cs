// Roundup season, in the game (TENHOUR R1; Core/Roundup.cs): every thirty seconds in a cattle town in the fortnight after a roundup, a farmhand near you thinks of the money, or of what the horse did to him.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _roundupNextLook, _roundupThoughts;
        private readonly HashSet<string> _seasonDone = new HashSet<string>();

        /// True the first time this key is seen: a person thinks of a season's thing once a day, not every half a minute for as long as they're the nearest.
        private bool SeasonFirst(string key)
        {
            if (_seasonDone.Count > 400)
                _seasonDone.Clear();
            return _seasonDone.Add(key);
        }

        private void RoundupStep(int now)
        {
            if (_cfg == null || !_cfg.RoundupSeason || _placeTown == null || (now < _roundupNextLook && _roundupNextLook - now <= 30000))
                return;
            _roundupNextLook = now + 30000;
            if (!Roundup.In(_placeTown) || !Roundup.Paid(InnerLife.ClockMonth, InnerLife.ClockDay))
                return;
            long day = Bridge.Now() / 1440;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                string tg = w.TradeGroup ?? (w.TradeGroup = InnerLife.TradeGroup(w.P.Trade));
                if (tg != "farm" || !SeasonFirst("roundup|" + w.Id + "|" + day))
                    continue;
                string seed = w.Id + "|" + day;
                _ticker.Push((w.Name ?? "Someone") + ", thinking", _engine.RoundupHurt(w.Id, day) ? Roundup.HurtThought(seed) : Roundup.PaidThought(seed), "near", h);
                _roundupThoughts++;
                break;
            }
        }
    }
}
