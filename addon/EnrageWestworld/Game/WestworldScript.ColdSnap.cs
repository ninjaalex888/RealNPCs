// A cold snap, in the game (TENHOUR R3; Core/ColdSnap.cs): every thirty seconds during one, in the high country's towns, someone near you thinks of it.
using EnrageWestworld.Core;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _coldNextLook;

        private void ColdSnapStep(int now)
        {
            if (_cfg == null || !_cfg.ColdSnaps || _placeTown == null || (now < _coldNextLook && _coldNextLook - now <= 30000))
                return;
            _coldNextLook = now + 30000;
            if (!ColdSnap.In(_placeTown) || !ColdSnap.Now(InnerLife.ClockYear, InnerLife.ClockMonth, InnerLife.ClockDay))
                return;
            long day = Bridge.Now() / 1440;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !SeasonFirst("cold|" + w.Id + "|" + day))
                    continue;
                _ticker.Push((w.Name ?? "Someone") + ", thinking", ColdSnap.Thought(w.Id + "|" + day), "near", h);
                break;
            }
        }
    }
}
