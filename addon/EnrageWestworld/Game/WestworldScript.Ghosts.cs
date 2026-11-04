// The reload noticed, in the game (SIXDAY item 12b; Core/Ghosts.cs, Core/Engine.Ghosts.cs): a victim of a rewound killing who
// walks the town again gets one sharp deja vu the first time the player comes within twelve metres (a thought on the ticker and
// an uneasy voice line), and the ticker says how many times the clock turned back. F7 "TEST ghosts:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _ghostsNext, _rewindsTold = -1;
        private Action _aGhosts;

        private void GhostsStep(int now)
        {
            if (_aGhosts == null)
                _aGhosts = () => GhostsTick(_now);
            Safe("ghosts", _aGhosts);
        }

        private void GhostsTick(int now)
        {
            if (_cfg == null || !_cfg.WestWorld || !_cfg.Ghosts || _engine == null || _store == null)
                return;
            if (now < _ghostsNext)
                return;
            _ghostsNext = now + 2000;
            if (_rewindsTold < 0)
                _rewindsTold = _store.Rewinds;   // the count at the start: only a change this session is news
            else if (_store.Rewinds != _rewindsTold)
            {
                _rewindsTold = _store.Rewinds;
                _ticker.Push("The world", Ghosts.WentBack(_store.Rewinds) + ".", "summary");
            }
            if (MISC.GET_MISSION_FLAG() || _resBind.Count == 0)
                return;
            Ped me = Game.Player.Character;
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                Resident r = kv.Value.R;
                if (r == null || r.Dead)
                    continue;
                Ghost g = _engine.DejaVuFor(r.Id);
                if (g == null)
                    continue;
                int h = kv.Key;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > Ghosts.DejaVuMetres)
                    continue;
                string first = Person.FirstOf(r.Name);
                _ticker.Push(first + ", thinking", Ghosts.DejaVu(g.ByPlayer, r.Id + "|" + g.Id), "near", h);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 6000, 0, 51, 0);
                Speech.SayAny(h, Speech.Stare, me.Handle);   // uneasy, in their own voice
                _engine.DejaVuFelt(g, r.Name, Bridge.Now());
                break;   // one a tick
            }
        }

        private string GhostsStatus()
        {
            if (_cfg == null || !_cfg.WestWorld)
                return "West World is off";
            return (_cfg.Ghosts ? "" : "off ([WestWorld] Ghosts; the list is kept all the same): ") + _engine.GhostsStatus(Bridge.Now());
        }
    }
}
