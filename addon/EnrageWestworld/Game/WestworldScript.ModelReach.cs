// The model's reach while it's idle, game side (LIVES item 3c; [Mind] ModelIdleReach; Core/ModelReach.cs): from the language
// step (once a second), when the model is idle (nothing waiting or in flight) and ready, every 8 s the known person within
// 20 m asked longest ago is asked ahead. Counted for F7 and the log.
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
            if (_engine == null || _cfg == null || !_cfg.ModelIdleReach || !_cfg.Language || !_cfg.MindLive)
                return;
            if (!ModelReach.Due(true, _langReady, _engine.LanguageAnswers.Idle, _reachLastMs, now))
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
            if (_reachAskedAt.Count > 200)
                _reachAskedAt.Clear();   // (first: the one just asked stays marked; the track 3 review)
            _reachAskedAt[best.Id] = now;
            string why;
            LanguageRequest r = MindRequestOf(best, bestH, now, out why);
            if (r != null && LanguageAsk(best, r, r.AvoidLine, false))
                _reachAsks++;
        }
    }
}
