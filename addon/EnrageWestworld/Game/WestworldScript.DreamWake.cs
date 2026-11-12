// The Testing row "dreamwake" (LIVES item 1d; [Mind] DreamsRemembered): the nearest person the mind knows, in view, thinks
// of last night's dream as on the morning after: their own if they dreamt one, else as if of a fright (nothing kept).
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _dreamTestPress;

        private string DreamWakeTest(out string said)
        {
            said = null;
            if (!_cfg.DreamsRemembered)
                return "[Mind] DreamsRemembered is off";
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
                if (who == null || who.Id == null)
                    continue;
                DreamMark dm;
                bool own = _engine.DreamMarks.TryGetValue(who.Id, out dm);
                if (!own)
                {
                    string[] emos = { "Fear", "Grief", "Anger", "Shame" };
                    dm = new DreamMark { Emotion = emos[_dreamTestPress % 4], Nights = 1 + _dreamTestPress / 4 % 3, AboutYou = _dreamTestPress % 2 == 1 };
                }
                _dreamTestPress++;
                string line = DreamWake.Line(dm, null, who.Id + "|test|" + Game.GameTime, null);
                if (line == null)
                    return "no lines for " + dm.Emotion;
                _ticker.Push(Person.FirstOf(who.Name ?? "Someone") + ", thinking", line, "near", h);
                Log.Write("DREAM morning (test, " + (own ? "their own" : "as if") + ", " + dm.Emotion + ", night " + dm.Nights + "), " + who.Name + ": " + line);
                said = (own ? "their own dream: " : "as if: ") + dm.Emotion + ", night " + dm.Nights + ", " + who.Name;
                return null;
            }
            return "nobody the mind knows within 20 m, in view";
        }
    }
}
