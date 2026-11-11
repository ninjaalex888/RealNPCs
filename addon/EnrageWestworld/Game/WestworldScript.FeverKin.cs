// The fevered's kin kept clear of, the game side (DEPTH item 12; Core/FeverKin.cs; with [World] Fever). The street's reason does the
// keeping clear and the asking after (Core/Reasons.cs; Heart sets Someone.FeverSick from Engine.FeverHousehold); here, when a
// "feveravoid" scene has played (OnPlayedOut), the one passed by thinks of it, over their head. Runner: the Director's street scenes
// (seen); the fever (built, not yet seen).
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _feverShunned;
        private string _feverShunnedLast;

        private void FeverKinShunned(Ped a, Ped b)
        {
            if (a == null || b == null || !b.Exists() || b.IsDead)
                return;
            int now = Game.GameTime;
            MindWho wa = a.Exists() ? MindKnown(a.Handle, now) : null, wb = MindKnown(b.Handle, now);
            if (wb == null || wb.Id == null)
                return;
            string sick = _engine.FeverHousehold(wb.Id);
            string thought = FeverKin.ShunnedThought(wa != null ? Person.FirstOf(wa.Name) : null, sick, wb.Id + "|" + _store.LastGameMin / 60);
            _ticker.Push(Person.FirstOf(wb.Name) + ", thinking", thought, "near", b.Handle);
            _feverShunned++;
            _feverShunnedLast = (wa != null ? wa.Name : "someone") + " kept clear of " + wb.Name + (sick != null ? " (" + sick + " has the fever)" : "");
            Log.Write("FEVER kin: " + _feverShunnedLast);
        }

        // ---- the big animals' tales grow (DEPTH item 14): tellings by town and tale, this session, in memory
        private readonly Dictionary<string, Dictionary<string, int>> _folkTold = new Dictionary<string, Dictionary<string, int>>();
        private int _folkTellings;

        private Dictionary<string, int> FolkToldIn(string town)
        {
            Dictionary<string, int> d;
            if (!_folkTold.TryGetValue(town, out d))
                _folkTold[town] = d = new Dictionary<string, int>();
            return d;
        }

        private void FolkTold(string town, string key)
        {
            Dictionary<string, int> d = FolkToldIn(town);
            int n;
            d.TryGetValue(key, out n);
            d[key] = n + 1;
            _folkTellings++;
            Log.Write("FOLK the " + key + " told in " + town + ", the " + (n + 1) + (n == 0 ? "st" : n == 1 ? "nd" : n == 2 ? "rd" : "th") + " time this session" + (n >= 1 ? ": bigger in the telling" : ""));
        }

        private string FolkStatus()
        {
            return _folkTellings + " tellings of the big animals' tales" + (_folkTold.Count > 0 ? ": " + string.Join(", ", _folkTold.SelectMany(t => t.Value.Select(v => v.Key + " " + v.Value + " in " + t.Key))) : "");
        }

        private string FeverKinStatus()
        {
            return _cfg == null || !_cfg.Fever ? "off ([World] Fever)" : "kept clear of " + _feverShunned + (_feverShunnedLast != null ? "; the last: " + _feverShunnedLast : "");
        }
    }
}
