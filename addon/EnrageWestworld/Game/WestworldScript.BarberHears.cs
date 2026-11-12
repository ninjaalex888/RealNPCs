// The Testing row "barber" (LIVES item 1e; [Mind] BarberHears): the barber, within 20 m and in view, thinks of what his
// chair heard now (a customer dead this week, two of his town at odds or courting, the stranger's deeds as told). Nothing
// kept; in ordinary play it's one of his thoughts (Engine.PostThought).
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string BarberTest(out string said)
        {
            said = null;
            if (!_cfg.BarberHears)
                return "[Mind] BarberHears is off";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _store == null)
                return "not loaded";
            foreach (int h in _mindNear)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) >= 20f || !NodInView(me, p))
                    continue;
                Person who = KnownPerson(p);
                Resident r = who != null && who.Id != null ? _store.FindResident(who.Id) : null;
                if (r == null || r.PostId == null)
                    continue;
                string line = _engine.BarberThoughtFor(r, r.Id + "|test|" + Game.GameTime);
                if (line == null)
                    return r.Name + " keeps a post, but the chair heard nothing yet (no quarrel or courtship of his town these two days, no rumour of you, no death this week), or he isn't the barber";
                _ticker.Push(Person.FirstOf(r.Name ?? "The barber") + ", thinking", line, "near", h);
                Log.Write("BARBER thought (test), " + r.Name + ": " + line);
                said = r.Name;
                return null;
            }
            return "no barber within 20 m in view (Valentine's barber shop)";
        }
    }
}
