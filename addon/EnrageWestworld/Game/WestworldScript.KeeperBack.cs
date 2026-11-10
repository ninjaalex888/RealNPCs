// The revived dead are seen, the game side (THREEHOUR reserve R3; Core/KeeperBack.cs). HolderAt finds a holder shot down here and
// brought back by the game (Engine.PostBack); this keeps who saw the fall, and the holder's body, for ten minutes. Every 2 s: one of
// them the mind has near you, within 20 m of the holder, free and ours to direct, turns and stares, with a thought. Once each.
// Behind [Mind] KeeperBack. Log "KEEPER BACK".
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class KeeperBackWatch
        {
            public int Holder, Until;
            public string Name;
            public bool Woman;
            public List<string> Saw;
        }

        private readonly List<KeeperBackWatch> _keeperBack = new List<KeeperBackWatch>();
        private int _keeperBackNextLook, _keeperBackStares;

        /// From HolderAt: a holder found back at the counter, and who saw them fall.
        private void KeeperBackNote(int holder, Resident r, List<string> saw)
        {
            if (_cfg == null || !_cfg.KeepersBackSeen || r == null || saw == null || saw.Count == 0 || _keeperBack.Count >= 4)
                return;
            _keeperBack.Add(new KeeperBackWatch { Holder = holder, Until = Game.GameTime + KeeperBack.WatchMs, Name = r.Name, Woman = r.Woman, Saw = new List<string>(saw) });
        }

        private void KeeperBackStep(int now)
        {
            if (_keeperBack.Count == 0 || now < _keeperBackNextLook)
                return;
            _keeperBackNextLook = now + KeeperBack.LookMs;
            Safe("keepers back", () => KeeperBackLook(now));
        }

        private void KeeperBackLook(int now)
        {
            Ped me = Game.Player.Character;
            for (int i = _keeperBack.Count - 1; i >= 0; i--)
            {
                KeeperBackWatch w = _keeperBack[i];
                if (now > w.Until || now < w.Until - KeeperBack.WatchMs || w.Saw.Count == 0 || !ENTITY.DOES_ENTITY_EXIST(w.Holder) || ENTITY.IS_ENTITY_DEAD(w.Holder))
                {
                    _keeperBack.RemoveAt(i);
                    continue;
                }
                Ped holder = new Ped(w.Holder);
                for (int k = w.Saw.Count - 1; k >= 0; k--)
                {
                    int h = MindHandleById(w.Saw[k]);
                    if (h == 0 || h == w.Holder || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    Ped p = new Ped(h);
                    if (p.Position.DistanceTo(holder.Position) > KeeperBack.NearHolderMetres || p.Position.DistanceTo(me.Position) > KeeperBack.NearYouMetres ||
                        NotFree(h) || MayDirectWhyNot(p) != null || _director.Involves(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_ON_MOUNT(h))
                        continue;
                    w.Saw.RemoveAt(k);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, w.Holder, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, w.Holder, KeeperBack.StareMs, 0, 51, 0);
                    MindWho mw;
                    string who = _mindWho.TryGetValue(h, out mw) && mw.Name != null ? Person.FirstOf(mw.Name) : "Someone";
                    _ticker.Push(who + ", thinking", KeeperBack.Thought(Person.FirstOf(w.Name), w.Woman, w.Holder + "|" + h), "near", h);
                    _keeperBackStares++;
                    Log.Write("KEEPER BACK " + who + " stares at " + w.Name + ", back at the counter after they saw " + (w.Woman ? "her" : "him") + " fall");
                    return;   // one a look
                }
            }
        }
    }
}
