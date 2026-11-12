// The Testing row "blur" (LIVES item 1c; [Mind] MemoryBlur): the nearest person the mind knows, in view, with a memory of
// their own past its half-life, thinks of it as it comes now: its details doubted, or too sharp. Nothing is kept; in
// ordinary play it's four in ten of the times such a memory is their thought (Engine.Candidates).
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string BlurTest(out string said)
        {
            said = null;
            if (!_cfg.MemoryBlur || !_cfg.MindLive)
                return "[Mind] MemoryBlur (and MindLive) must be on";
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _store == null)
                return "not loaded";
            long now = _store.LastGameMin;
            foreach (int h in _mindNear)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) >= 20f || !NodInView(me, p))
                    continue;
                Person who = KnownPerson(p);
                PersonState st = who != null && who.Id != null ? _store.Find(who.Id) : null;
                if (st == null || st.Mind == null)
                    continue;
                foreach (MindMemory m in st.Mind.OrderByDescending(x => x.Strength))
                {
                    string how = null;
                    for (int i = 0; i < 12 && how == null; i++)
                        how = MemoryBlur.How(m, now, "test|" + i);   // the roll aside: is it one that blurs or comes sharp at all
                    if (how == null)
                        continue;
                    string line = MemoryBlur.Line(m, how, who.Id + "|test|" + Game.GameTime, null);
                    _ticker.Push(Person.FirstOf(who.Name ?? "Someone") + ", thinking", line, "near", h);
                    Log.Write("MEMORY " + how + " (test, " + m.Kind + ", " + (now - m.At) / 60 + " h old), " + who.Name + ": " + line);
                    said = how + ": " + who.Name + "'s " + m.Kind;
                    return null;
                }
            }
            return "nobody the mind knows within 20 m, in view, with a memory of their own past its half-life (not told, not of you)";
        }
    }
}
