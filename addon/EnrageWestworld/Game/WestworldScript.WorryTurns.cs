// The Testing row "worryturn" (LIVES item 1a; [Mind] WorryTurns): the nearest person the mind knows, in view and with a
// worry this week, thinks their worry's turn, whatever day of it they're on: looking back, then a hope, then the eve, one
// a press. Nothing is kept; the turns themselves come in the ordinary thoughts on their own days (Core/WorryTurns.cs).
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _worryTurnPress;   // which turn the next press shows

        private string WorryTurnTest(out string said)
        {
            said = null;
            if (!_cfg.WorryTurns)
                return "[Mind] WorryTurns is off";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _store == null)
                return "not loaded";
            long day = _store.LastGameMin / 1440;
            string turn = _worryTurnPress % 3 == 0 ? "back" : _worryTurnPress % 3 == 1 ? "hope" : "eve";
            Ped best = null;
            Person bp = null;
            float bestD = 20f;
            foreach (int h in _mindNear)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                float d = p.Position.DistanceTo(me.Position);
                if (d >= bestD || !NodInView(me, p))
                    continue;
                Person who = KnownPerson(p);
                string kind; int stage; bool good;
                if (who == null || !InnerLife.WorryNow(who, day, out kind, out stage, out good))
                    continue;
                best = p;
                bp = who;
                bestD = d;
            }
            if (best == null)
                return "nobody the mind knows within 20 m, in view, with a worry this week";
            string line = InnerLife.WorryTurnLine(bp, day, turn, bp.Id + "|test|" + Game.GameTime);
            if (line == null)
                return bp.Name + "'s worry has no " + turn + " lines";
            _worryTurnPress++;
            _ticker.Push(Person.FirstOf(bp.Name ?? "Someone") + ", thinking", line, "near", best.Handle);
            Log.Write("WORRY turn (test, " + turn + "), " + bp.Name + ": " + line);
            said = turn + " for " + bp.Name + " (" + (int)bestD + " m)";
            return null;
        }
    }
}
