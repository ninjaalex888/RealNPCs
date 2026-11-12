// The model's reach while it's idle, game side (LIVES item 3c; [Mind] ModelIdleReach; Core/ModelReach.cs): from the language
// step (once a second), when the queue is empty and the model ready, the nearest known person within 20 m not asked in the
// last minute is asked ahead. Counted for F7 and the log.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _reachLastMs = int.MinValue / 2, _reachAsks;
        private readonly Dictionary<string, int> _reachAskedAt = new Dictionary<string, int>();

        private void ModelReachStep(int now)
        {
            if (!ModelReach.Due(_cfg.ModelIdleReach && _cfg.Language && _cfg.MindLive, _langReady, _engine.LanguageAnswers.Waiting, _reachLastMs, now))
                return;
            _reachLastMs = now;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            MindWho best = null;
            int bestH = 0, bestAt = int.MaxValue;
            float bestD = float.MaxValue;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                int at;
                at = _reachAskedAt.TryGetValue(w.Id, out at) ? at : int.MinValue;
                if (!ModelReach.Again(at, now))
                    continue;
                float d = new Ped(h).Position.DistanceTo(me.Position);
                if (d > ModelReach.Metres)
                    continue;
                if (ModelReach.Before(at, d, bestAt, bestD))   // the one asked longest ago (never first), then the nearest
                {
                    bestAt = at;
                    bestD = d;
                    best = w;
                    bestH = h;
                }
            }
            if (best == null)
                return;
            _reachAskedAt[best.Id] = now;
            if (_reachAskedAt.Count > 200)
                _reachAskedAt.Clear();
            string why;
            LanguageRequest r = MindRequestOf(best, bestH, now, out why);
            if (r != null && LanguageAsk(best, r, r.AvoidLine, false))
                _reachAsks++;
        }
    }
}
