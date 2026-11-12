// The Testing row "absent" (LIVES item 1b; [Mind] AbsentThoughts): the nearest person the mind knows, in view, thinks of
// someone of their ties who isn't in sight (a fellow witness of a killing, or one they fear, scorn, envy or look up to),
// now, if they have one. Nothing is kept; in ordinary play it's one of their candidate thoughts (Engine.Candidates).
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string AbsentTest(out string said)
        {
            said = null;
            if (!_cfg.AbsentThoughts)
                return "[Mind] AbsentThoughts is off";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _store == null)
                return "not loaded";
            int tried = 0;
            Ped best = null;
            Person bp = null;
            string bk = null, bw = null;
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
                if (who == null || who.Id == null)
                    continue;
                tried++;
                string kind, other;
                InnerLife.Context day = _engine.ThoughtDay(who.Home, who.Id);
                if (AbsentThoughts.Pick(who.Id, who.Personality, day, _store.FindResident, out kind, out other) == null)
                    continue;
                best = p;
                bp = who;
                bk = kind;
                bw = other;
                bestD = d;
            }
            if (best == null)
                return tried == 0 ? "nobody the mind knows within 20 m, in view" : "none of the " + tried + " near has a tie of the last four days out of sight (a shared fright, or a view)";
            string line = AbsentThoughts.Line(bk, bw, bp.Id + "|test|" + Game.GameTime, null);
            _ticker.Push(Person.FirstOf(bp.Name ?? "Someone") + ", thinking", line, "near", best.Handle);
            Log.Write("ABSENT thought (test, " + bk + " of " + bw + "), " + bp.Name + ": " + line);
            said = bk + ": " + bp.Name + " of " + bw;
            return null;
        }
    }
}
