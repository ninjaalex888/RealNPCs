// The criminal class ([Scenes] CriminalClass; Core/Criminals.cs, Core/Engine.Criminals.cs; SIXDAY item 16). The class
// itself lives in the street's reasons (a thief this month keeps to it: Reasons.Thief, Someone.Bent), the "thief" over
// a head for the month (Core04's Nearby) and the drunk rolled (Scenes.Rolled in WestworldScript.Reasons.cs). Here: every
// thirty seconds the known people near you in view are looked at for one gone straight, who thinks of it once over
// their head. F7 "TEST criminals:".
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private const int CriminalsLookMs = 30000;
        private int _nextCriminals, _reformThoughts;
        private Action _aCriminals;

        /// Tick04: a look every thirty seconds, under its own Safe.
        private void CriminalsStep(int now)
        {
            if (_cfg == null || !_cfg.CriminalClass || _store == null || _engine == null || now < _nextCriminals)
                return;
            _nextCriminals = now + CriminalsLookMs;
            if (_aCriminals == null)
                _aCriminals = () => CriminalsLook(_now);
            Safe("criminals", _aCriminals);
        }

        private void CriminalsLook(int now)
        {
            if (_mindNear.Count == 0 || MISC.GET_MISSION_FLAG())
                return;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            long g = Bridge.Now();
            for (int i = 0; i < _mindNear.Count; i++)
            {
                int h = _mindNear[i];
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > 15f || !NodInView(me, p))
                    continue;
                string thought = _engine.CriminalReformed(w.Id, g);
                if (thought == null)
                    continue;
                _ticker.Push(w.Name + ", thinking", thought, "near", h);
                _reformThoughts++;
                Log.Write("CRIMINAL " + w.Name + " thinks: " + thought);
            }
        }

        private string CriminalsStatus()
        {
            if (_engine == null || _store == null)
                return "not loaded";
            return _engine.CriminalsStatus(Bridge.Now()) + "; " + _reformThoughts + " reform thoughts this session";
        }
    }
}
