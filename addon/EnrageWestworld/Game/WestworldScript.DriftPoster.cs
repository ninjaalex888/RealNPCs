// Posters that drift, in game (FIVEDAY reserve R13; Core/DriftPoster.cs). Every five seconds in a town: the first time each
// day you're in a town that one of your deeds' rumours has reached (Store.TownRumours with this Town; the newest, the
// weightiest deed), the box has the bill outside the sheriff's as it reads there (by the rumour's Fidelity); and now and then
// (a time in four, a minute apart) someone named within 10 m holds you up against it. Nothing is spawned or directed.
// Behind [World] DriftingPosters.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _posterNextLook, _posterNextCompare, _postersShown;
        private readonly Dictionary<string, long> _posterDay = new Dictionary<string, long>();   // town -> the game day its bill was shown

        private void PosterStep(int now)
        {
            if (_cfg == null || !_cfg.DriftingPosters || _store == null || _placeTown == null || now < _posterNextLook)
                return;
            _posterNextLook = now + 5000;
            TownRumour best = null;
            foreach (TownRumour r in _store.TownRumours)
                if (r != null && r.Town == _placeTown && (best == null || r.Fidelity < best.Fidelity))
                    best = r;   // the most bent telling here: that's what's on the bill
            if (best == null)
                return;
            Deed d = _store.Deeds.FirstOrDefault(x => x != null && x.Id == best.DeedId);
            if (d == null)
                return;
            long day = Bridge.Now() / 1440, shown;
            if (!_posterDay.TryGetValue(_placeTown, out shown) || shown != day)
            {
                _posterDay[_placeTown] = day;
                _postersShown++;
                Notice(_placeTown, "Outside the sheriff's: " + DriftPoster.Bill(d.Kind, d.Count, d.Town ?? d.Zone, best.Fidelity, _engine.Player.FullName, best.Id), "news");
                Log.Write("POSTER " + _placeTown + ": the bill for " + d.Kind + " at " + (d.Town ?? d.Zone) + ", the telling at fidelity " + best.Fidelity);
                return;
            }
            if (now < _posterNextCompare || _rng.Next(4) != 0)
                return;
            _posterNextCompare = now + 60000;
            Ped me = Game.Player.Character;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || new Ped(h).Position.DistanceTo(me.Position) > 10f)
                    continue;
                _ticker.Push((w.Name ?? "Someone") + ", thinking", DriftPoster.Comparing(best.Fidelity, w.Id + "|" + day), "near", h);
                break;
            }
        }

        // ---- the photographer's window, 1907 (FIVEDAY R26; Core/Relics.cs) ----
        private int _relicNextLook, _relicsShown;
        private readonly Dictionary<string, long> _relicDay = new Dictionary<string, long>();

        /// Every ten seconds in Saint Denis or Blackwater in 1907: within 25 m of the photographer at his counter, once a town a
        /// day, his window in the Around town box.
        private void RelicStep(int now)
        {
            if (_cfg == null || !_cfg.Relics || _placeTown == null || now < _relicNextLook || !Relics.Now(Voice.NowYear) || System.Array.IndexOf(Relics.Towns, _placeTown) < 0)
                return;
            _relicNextLook = now + 10000;
            long day = Bridge.Now() / 1440, d;
            if (_relicDay.TryGetValue(_placeTown, out d) && d == day)
                return;
            Ped me = Game.Player.Character;
            foreach (Ped p in Peds.All())
            {
                if (p.IsDead || p.Position.DistanceTo(me.Position) > 25f)
                    continue;
                PostKind pk = PostKindOf(p);
                if (pk == null || pk.Kind != MemorialPhoto.Kind)
                    continue;
                _relicDay[_placeTown] = day;
                _relicsShown++;
                Notice(_placeTown, Relics.Window(_placeTown + "|" + day), "news");
                Log.Write("RELICS the photographer's window at " + _placeTown);
                return;
            }
        }

        // ---- the editor's readers (FIVEDAY R14; Core/Editor.cs) ----
        private int _editNextLook, _editThoughts;

        /// Every forty seconds in a town whose paper ran an editorial on you these two days: someone named near who read it
        /// agrees or argues, by temper.
        private void EditorStep(int now)
        {
            if (_cfg == null || !_cfg.EditorSides || _engine == null || _placeTown == null || now < _editNextLook)
                return;
            _editNextLook = now + 40000;
            if (!_engine.EditorialFresh(_placeTown, Bridge.Now()))
                return;
            long day = Bridge.Now() / 1440;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !Voice.Chance(w.Id + "|ed|" + day, 50))
                    continue;
                _ticker.Push((w.Name ?? "Someone") + ", thinking", Editor.Reader(Minds.TemperGroup(w.P.Personality), w.Id + "|" + day), "near", h);
                _editThoughts++;
                break;
            }
        }

        private string PosterStatus()
        {
            if (_cfg == null || !_cfg.DriftingPosters)
                return "off ([World] DriftingPosters)";
            return _postersShown + " bills shown this session; " + _editThoughts + " readers of an editorial on you";
        }
    }
}
