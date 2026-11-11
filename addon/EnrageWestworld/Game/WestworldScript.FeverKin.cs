// The fevered's kin kept clear of, the game side (DEPTH item 12; Core/FeverKin.cs; with [World] Fever). The street's reason does the
// keeping clear and the asking after (Core/Reasons.cs; Heart sets Someone.FeverSick from Engine.FeverHousehold); here, when a
// "feveravoid" scene has played (OnPlayedOut), the one passed by thinks of it, over their head. Runner: the Director's street scenes
// (seen); the fever (built, not yet seen).
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

        private string FeverKinStatus()
        {
            return _cfg == null || !_cfg.Fever ? "off ([World] Fever)" : "kept clear of " + _feverShunned + (_feverShunnedLast != null ? "; the last: " + _feverShunnedLast : "");
        }
    }
}
