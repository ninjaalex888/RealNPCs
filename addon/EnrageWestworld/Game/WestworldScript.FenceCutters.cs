// Fence cutters in game (FIVEDAY item 31; Core/FenceCutters.cs, Engine.FenceCutters.cs). Every ten seconds: at night
// (ten to five) in one of the open-range towns, it notes that you were near that town that night (keyed by the morning's
// day). When the engine finds a cut (LastFence, new since the last look): if you were near that town that night, the
// box tells you what you saw riding off, the cattleman's hands or drifters; if you're in the town now, the box has the
// homesteader's word and whom he blames. The thoughts of the two, and the town's talk of them, come through the
// "fencecut" tie (Ties.Thought, Ties.TownThought). Session only. Behind [World] FenceCutters.
using System.Collections.Generic;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, long> _fenceNight = new Dictionary<string, long>();   // town -> the morning's day after a night you were near it
        private long _fenceSeenAt = -1;
        private int _fenceNextLook, _fenceSaw;

        private void FenceStep(int now)
        {
            if (_cfg == null || !_cfg.FenceCutters || _engine == null || now < _fenceNextLook)
                return;
            _fenceNextLook = now + 10000;
            Safe("fence cutters", () =>
            {
                long g = Bridge.Now();
                int hour = (int)(g % 1440 / 60);   // the game's hour, from the same minute the engine keeps
                if (_placeTown != null && FenceCutters.Night(hour) && System.Array.IndexOf(FenceCutters.Towns, _placeTown) >= 0)
                    _fenceNight[_placeTown] = g / 1440 + (hour >= FenceCutters.NightFrom ? 1 : 0);   // the morning it's found
                FenceCut f = _engine.LastFence;
                if (f == null || f.At == _fenceSeenAt)
                    return;
                _fenceSeenAt = f.At;
                long night;
                if (_fenceNight.TryGetValue(f.Town, out night) && night == f.At / 1440)
                {
                    NewsPush(f.Town, FenceCutters.SawLine(f, "saw|" + f.At), "news");   // you know what he doesn't
                    _fenceSaw++;
                    Log.Write("FENCE you were near " + f.Town + " that night: you saw " + (f.Drifters ? "drifters" : "the " + Residents.SurnameOf(f.CName) + " hands"));
                }
                if (_placeTown == f.Town)
                    NewsPush(f.Town, FenceCutters.FoundLine(f, "found|" + f.At), "news");
            });
        }
    }
}
