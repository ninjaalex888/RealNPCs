// Mail day's game side (LIVES item 2f; [Scenes] MailDay): a train came in at this town's platform in the last ten minutes
// (WestworldScript.Trains.cs: _trainArrivedAt, _trainPlatform) and the player is within 60 m of it.
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int MailInMs = 10 * 60 * 1000;

        private bool MailInHere()
        {
            if (_trainArrivedAt <= 0 || _trainPlatform == null || _trainPlatform.Town != _placeTown || Game.GameTime - _trainArrivedAt > MailInMs)
                return false;
            Ped me = Game.Player.Character;
            return me != null && me.Exists() && me.Position.DistanceTo(new Vector3(_trainPlatform.X, _trainPlatform.Y, _trainPlatform.Z)) <= 60f;
        }
    }
}
