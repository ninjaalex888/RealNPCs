// Fever in town, in game (FIVEDAY reserve R9; Core/Fever.cs, Core/Engine.Fever.cs). Every twenty seconds in a town with a
// fever going through it: one of the named people near thinks of it, a sick one of his or her sickness (they're keeping to
// home: ChoiceContext.Sick), anyone else of the town's fear. The quarantine notice and the end's word come from the engine's
// toasts. Nothing is spawned or directed. Behind [World] Fever.
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _feverNextLook, _feverThoughts;

        private void FeverStep(int now)
        {
            if (_cfg == null || !_cfg.Fever || _engine == null || _placeTown == null || now < _feverNextLook)
                return;
            _feverNextLook = now + 20000;
            if (!_engine.FeverIn(_placeTown))
                return;
            Safe("fever", () =>
            {
                long day = Bridge.Now() / 1440;
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    bool sick = _engine.SickNow(w.Id);
                    _ticker.Push((w.Name ?? "Someone") + ", thinking", sick ? Fever.SickThought(w.Id + "|" + day) : Fever.TownThought(w.Id + "|" + day), "near", h);
                    _feverThoughts++;
                    break;
                }
            });
        }

        private string FeverGameStatus()
        {
            return _engine.FeverStatus() + "; " + _feverThoughts + " fever thoughts this session";
        }
    }
}
