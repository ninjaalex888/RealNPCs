// The fight at the dance's game side (LIVES item 2d; [Scenes] DanceFight): whether a dance is on in this town now (the
// engine's open event; looked up once in ten seconds, not each pair).
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _danceLookAt = -100000;
        private bool _danceOn;
        private string _danceTown;

        private bool DanceOnHere(long gameNow)
        {
            int t = Game.GameTime;
            if (t - _danceLookAt < 10000 && _danceTown == _placeTown)
                return _danceOn;
            _danceLookAt = t;
            _danceTown = _placeTown;
            TownEvent e = _cfg.EventDances && _placeTown != null ? _engine.EventNear(_placeTown, gameNow) : null;
            _danceOn = e != null && e.Kind == "dance" && gameNow >= e.At && gameNow < e.Until;
            return _danceOn;
        }
    }
}
