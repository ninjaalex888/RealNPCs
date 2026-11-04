// The revived dead seen, in the game (SIXDAY item 12c; Core/WakeLines.cs, Core/Engine.WakeLines.cs): a resident alive again after
// a save loaded from before, in view and within 15 m of someone bound who knew them for dead (a tie), gets looked at, stepped
// back from, and thought about, once by each; the line that wakes is said inside the scenes (WestworldScript.Life.cs builds it
// into the cast, WestworldScript.Events.cs notes it said). F7 "TEST wakelines:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _revivedNext, _revivedReactions;

        /// Every two seconds (from GhostsTick): a revived resident in view, and the bound people near who knew them for dead.
        private void RevivedSeenStep(int now)
        {
            if (_cfg == null || !_cfg.WestWorld || !_cfg.WakeLines || now < _revivedNext || _resBind.Count < 2)
                return;
            _revivedNext = now + 2000;
            Ped me = Game.Player.Character;
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                Resident dead = kv.Value.R;
                if (dead == null || dead.Dead)
                    continue;
                Ghost g = _engine.RevivedGhost(dead.Id);
                if (g == null)
                    continue;
                int dh = kv.Key;
                if (!ENTITY.DOES_ENTITY_EXIST(dh) || ENTITY.IS_ENTITY_DEAD(dh) || !ENTITY.IS_ENTITY_ON_SCREEN(dh))
                    continue;
                Vector3 dpos = new Ped(dh).Position;
                if (dpos.DistanceTo(me.Position) > 40f)
                    continue;
                foreach (KeyValuePair<int, ResBind> other in _resBind)
                {
                    Resident seer = other.Value.R;
                    int sh = other.Key;
                    if (seer == null || seer.Dead || sh == dh || !_engine.WouldKnowDead(g, seer.Id) || !ENTITY.DOES_ENTITY_EXIST(sh) || ENTITY.IS_ENTITY_DEAD(sh))
                        continue;
                    Ped sp = new Ped(sh);
                    Vector3 spos = sp.Position;
                    if (spos.DistanceTo(dpos) > WakeLines.SeenDeadMetres)
                        continue;
                    // a look, a step back, a thought
                    TASK.TASK_LOOK_AT_ENTITY(sh, dh, 7000, 0, 51, 0);
                    Vector3 away = spos - dpos;
                    float len = (float)Math.Sqrt(away.X * away.X + away.Y * away.Y);
                    if (len > 0.1f && !PED.IS_PED_USING_ANY_SCENARIO(sh))
                    {
                        Vector3 back = new Vector3(spos.X + away.X / len * 1.5f, spos.Y + away.Y / len * 1.5f, spos.Z);
                        TASK.TASK_GO_STRAIGHT_TO_COORD(sh, back.X, back.Y, back.Z, 1f, 2500, HeadingFrom(back, dpos), 0.3f, 0);
                    }
                    _ticker.Push(Person.FirstOf(seer.Name) + ", thinking", WakeLines.SeenDead(Person.FirstOf(dead.Name), g.ByPlayer, seer.Id + "|" + g.Id), "near", sh);
                    Speech.SayAny(sh, Speech.Stare, dh);
                    _engine.SeenDead(g, seer.Id, seer.Name, Bridge.Now());
                    _revivedReactions++;
                    return;   // one reaction a tick
                }
            }
        }

        /// The game's heading (degrees, north nought, counter-clockwise) from one point to another.
        private static float HeadingFrom(Vector3 from, Vector3 to)
        {
            float h = (float)(Math.Atan2(-(to.X - from.X), to.Y - from.Y) * 180.0 / Math.PI);
            return h < 0f ? h + 360f : h;
        }

        private string WakeLinesStatus()
        {
            if (_cfg == null || !_cfg.WestWorld)
                return "West World is off";
            if (!_cfg.WakeLines)
                return "off ([WestWorld] WakeLines)";
            return _engine.WakeLinesStatus() + "; this session: " + _revivedReactions + " reactions to the revived";
        }
    }
}
