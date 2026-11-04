// The town ledger in the game (SIXDAY item 11c; Core/TownLedgers.cs, Core/Engine.Ledgers.cs): the regulars' cap a town keeps
// comes from its ledger, and, with [World] ThinStreets on (off as it comes), a town whose index is under 30 has its ambient
// people thinned each frame (never below 0.6, that town only, never in missions), the count within 60 m logged once a minute
// so the game's answer to it can be read. F7 "TEST towns:".
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private Action _aThin;
        private int _thinNext;
        private string _thinTown;
        private float _thinMult = 1f;

        /// The regulars this town keeps now (Core/TownLedgers.cs: the setting scaled by the town's index).
        private int RegularsCap(string town)
        {
            return _engine != null ? _engine.TownCap(town) : _cfg.RegularsPerTown;
        }

        private void ThinStreetsStep(int now)
        {
            if (_aThin == null)
                _aThin = () => ThinStreetsFrame(_now);
            Safe("thin streets", _aThin);
        }

        /// Every frame: the multiplier for a thinned town, this frame only (the game forgets it each frame).
        private void ThinStreetsFrame(int now)
        {
            if (_cfg == null || !_cfg.ThinStreets || !_cfg.TownLedger || _engine == null || _placeTown == null)
                return;
            if (MISC.GET_MISSION_FLAG())
                return;
            TownLedger l = _engine.LedgerOf(_placeTown);
            if (l == null || l.Index >= TownLedgers.ThinBelow)
            {
                if (_thinTown == _placeTown)
                    _thinTown = null;
                return;
            }
            float mult = TownLedgers.Density(l.Index);
            PED._SET_AMBIENT_HUMAN_DENSITY_MULTIPLIER_THIS_FRAME(mult);
            if (now < _thinNext)
                return;
            _thinNext = now + 60000;
            _thinTown = _placeTown;
            _thinMult = mult;
            Log.Write("THIN STREETS " + _placeTown + ": multiplier " + mult.ToString("0.00") + " (index " + l.Index + ")");   // no scan of the peds from the frame (the review of items 11-12)
        }

        private string TownsStatus()
        {
            return _engine.LedgersStatus() + (_cfg.ThinStreets ? (_thinTown != null ? "; streets thinned in " + _thinTown + " (x" + _thinMult.ToString("0.00") + ")" : "; thin streets on, nowhere thinned now") : "; thin streets off");
        }
    }
}
