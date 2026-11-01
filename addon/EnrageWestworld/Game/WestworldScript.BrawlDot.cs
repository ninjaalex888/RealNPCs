// The one who threw the first punch wears the game's small red enemy dot on the map while the fight lasts, and dealing with
// him meanwhile is self-defence (Watch.cs's came-at-you list) (Lex, 2026-11-01, Saint Denis: "turn the one who starts the
// fight into a red dot"). [Mind] BrawlStarterDot. A man who already has a dot (enRAGE's, a regular's) keeps his own: only
// ours is ever added or taken away. Every 1.5 s from BrawlStep, never per frame.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private Blip _brawlDot;
        private int _brawlDotOn;

        private void BrawlDotStep(List<int> a, int now)
        {
            if (!_cfg.BrawlStarterDot || a == null || a.Count == 0)
            {
                BrawlDotClear();
                return;
            }
            int h = a[0];
            if (_brawlDot != null && _brawlDotOn != h)
                BrawlDotClear();
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
            {
                BrawlDotClear();
                return;
            }
            _cameAtYouSeen[h] = now;   // he started it: stepping in against him is self-defence while it lasts
            if (_brawlDot != null || MAP.GET_BLIP_FROM_ENTITY(h) != 0)
                return;
            try
            {
                _brawlDot = new Blip(MAP.BLIP_ADD_FOR_ENTITY(Natives.Hash("BLIP_STYLE_ENEMY"), h));
                _brawlDot.Label = "Started the fight";
                _brawlDotOn = h;
                Log.Write("BRAWL a red dot on the one who started it (fair game while it lasts)");
            }
            catch (Exception ex)
            {
                _brawlDot = null;
                Log.Write("BRAWL the red dot failed: " + ex.Message);
            }
        }

        private void BrawlDotClear()
        {
            if (_brawlDot == null)
                return;
            try
            {
                if (_brawlDot.Exists())
                    _brawlDot.Delete();
            }
            catch { }
            _brawlDot = null;
            _brawlDotOn = 0;
        }
    }
}
