// The town's best fist, the game side (FOURTEENHOUR reserve R7; Core/BestFist.cs, Core/Engine.PrizeFight.cs; [Scenes] BestFist). A
// fight's winner (the Director's FightOver: a street or saloon fight, a brawl's last side standing) is counted toward a name in the
// town (Engine.FistWin; the prize fight's winner is counted in the engine). Here too: the holder, near you and in view, has
// one thought of it a day (a look every 5 s). Never a frame of its own.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _fistLookAt, _fistThoughts, _fistWins;
        private readonly HashSet<string> _fistThought = new HashSet<string>();   // "id|day": one a day

        /// A Director fight's winner: one more toward the town's name.
        private void FistWinNote(int winner)
        {
            if (!BestFist.On || winner == 0 || _placeTown == null || _engine == null || _store == null || !ENTITY.DOES_ENTITY_EXIST(winner))
                return;
            MindWho w = MindKnown(winner, Game.GameTime);
            if (w == null || w.Id == null)
                return;
            _fistWins++;
            Safe("best fist", () => _engine.FistWin(_placeTown, w.Id, w.Name, _store.LastGameMin));
        }

        /// The holder near you, in view: his thought of it, one a day.
        private void FistLook(int now)
        {
            if (!BestFist.On || now < _fistLookAt || _store == null || _placeTown == null)
                return;
            _fistLookAt = now + 5000;
            long day = _store.LastGameMin / 1440;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !_store.Counts.ContainsKey(BestFist.HolderKey(_placeTown, w.Id)))
                    continue;
                string key = w.Id + "|" + day;
                if (_fistThought.Contains(key) || !ENTITY.DOES_ENTITY_EXIST(h) || !ENTITY.IS_ENTITY_ON_SCREEN(h))
                    continue;
                if (!_ticker.Aside(Person.FirstOf(w.Name ?? "He") + ", thinking", BestFist.Thought(_placeTown, key), "fist|" + w.Id, h, 8000, now))
                    continue;
                _fistThought.Add(key);
                if (_fistThought.Count > 200)
                    _fistThought.Clear();
                _fistThoughts++;
                return;
            }
        }

        /// F7: the best fists this session.
        private string BestFistStatus()
        {
            if (!BestFist.On)
                return "off ([Scenes] BestFist)";
            return _fistWins + " fights won counted this session, " + _fistThoughts + " thoughts of the name; " + (_engine != null ? _engine.BestFistStatus() : "");
        }
    }
}
