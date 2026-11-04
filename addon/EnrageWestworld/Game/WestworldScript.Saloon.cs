// The saloon's own two (SIXDAY item 17; roadmap 7): the barkeep stepping in ([Scenes] BarkeepSteps: Scenes.BarkeepSteps
// from AddBarkeep in WestworldScript.Reasons.cs) and the drunk really taking it outside ([Scenes] DrunkOutside:
// Scenes.DrunkOutside, the Director's "outside" act walking the two to the saloon's door the add-on remembers from the
// last time you came out of it, the fists in the road). Here: the door for the Director, the counts, F7 "TEST saloon:".
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private int _barkeepSteps, _barkeepHeeded, _drunksOutside;

        /// The saloon's door of this town, as remembered from the last time you came out of it (TownEvents' _saloonDoor), when
        /// it's within `metres` of you; else null (the drunk squares up where he stands).
        private Vector3? SaloonDoorNear(float metres)
        {
            Vector3 at;
            if (_placeTown == null || !_saloonDoor.TryGetValue(_placeTown, out at))
                return null;
            Ped me = Game.Player.Character;
            return me != null && me.Exists() && at.DistanceTo(me.Position) < metres ? at : (Vector3?)null;
        }

        /// The drunk's challenge taken outside: the scene, counted and logged.
        private Scene DrunkOutsideScene(SceneCast cast, string seed)
        {
            _drunksOutside++;
            Log.Write("DRUNK outside: the challenge taken to the street (the saloon's door " + (SaloonDoorNear(40f) != null ? (int)SaloonDoorNear(40f).Value.DistanceTo(Game.Player.Character.Position) + " m off" : "unknown") + ")");
            return Scenes.DrunkOutside(cast, seed);
        }

        private string SaloonStatus()
        {
            if (_cfg == null)
                return "not loaded";
            return "barkeep steps in: " + (_cfg.BarkeepSteps ? "on" : "off") + " (" + _barkeepSteps + " this session, " + _barkeepHeeded + " heeded); the drunk outside: " + (_cfg.DrunkOutside ? "on" : "off") +
                   " (" + _drunksOutside + " this session); the saloon door here: " + (SaloonDoorNear(40f) != null ? "known, within 40 m" : _placeTown != null && _saloonDoor.ContainsKey(_placeTown) ? "known, farther off" : "not yet (leave a saloon by its door once)");
        }
    }
}
