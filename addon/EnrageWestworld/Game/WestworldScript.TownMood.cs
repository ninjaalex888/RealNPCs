// A town on edge, the game side (NIGHT3 item 5; Core/TownMood.cs). The fear of the town the player is in, from the
// happenings already kept, worked out at most every 5 s: it raises everyone's safety need there (MindNeeds), goes into the
// model's request as "The town: ..." (MindRequestOf), and the novelty of a happening scales an onlooker's feeling
// (MindAppraiseOne). With [Mind] MindTownMood and MindLive. F7 TEST town mood:.
using System;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private TownFear _townFear = new TownFear();
        private int _townFearAt = -100000;
        private string _townFearFor;
        private int _townNoveltyLess;

        private bool TownMoodOn { get { return _cfg.MindLive && _cfg.MindTownMood && _store != null; } }

        private TownFear TownFearNow(int now)
        {
            if (!TownMoodOn || _placeTown == null)
                return _townFear = new TownFear();
            if (now - _townFearAt >= 5000 || now < _townFearAt || _townFearFor != _placeTown)
            {
                _townFearAt = now;
                _townFearFor = _placeTown;
                _townFear = TownMood.Fear(_store.WorldLog, _placeTown, _store.LastGameMin);
            }
            return _townFear;
        }

        private float TownNovelty(Happening ev, int now)
        {
            if (!TownMoodOn || ev == null)
                return 1f;
            float n = TownMood.Novelty(_store.WorldLog, ev.Town, ev.Kind, ev.At, ev);
            if (n < 1f)
                _townNoveltyLess++;
            return n;
        }

        private string TownMoodStatus()
        {
            if (!TownMoodOn)
                return "off ([Mind] MindTownMood, MindLive)";
            TownFear f = TownFearNow(Environment.TickCount);
            string words = TownMood.Words(f, _store.LastGameMin);
            return (_placeTown ?? "out of the towns") + ": fear " + f.Level.ToString("0.00") + (words != null ? " (" + words + ")" : " (calm)") +
                   (f.Worst != null ? "; the worst: " + f.Worst.Kind + " " + Math.Max(0, _store.LastGameMin - f.Worst.At) + " game min ago" : "") +
                   "; onlookers moved less by a sight seen again: " + _townNoveltyLess + " this session";
        }
    }
}
