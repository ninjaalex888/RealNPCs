// The church steps' game side (LIVES item 2c; [Scenes] ChurchSteps): whether the player is out of doors near the town's
// church (the pair reasons' Place.NearChurch), and the man of the town who wasn't in his pew, by name, for the talk.
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const float ChurchStepsMetres = 60f;

        /// Out of doors within 60 m of this town's church (the Churches table, else the town's middle is no church: false).
        private bool NearChurchSteps()
        {
            Vector3 at;
            if (_placeTown == null || _interiorType != 0 || !Churches.TryGetValue(_placeTown, out at))
                return false;
            Ped me = Game.Player.Character;
            return me != null && me.Exists() && me.Position.DistanceTo(at) <= ChurchStepsMetres;
        }

        /// A living townsman, a regular whose habits keep him from church (Routines: no churchgoer), not one of the two
        /// talking: the same man all day (seeded by the day). Null: none.
        private string AbsentFromChurch(Someone a, Someone b)
        {
            if (_placeTown == null || _store == null)
                return null;
            long day = _store.LastGameMin / 1440;
            var men = _store.Residents.Where(r => r != null && r.Regular && !r.Woman && r.Town == _placeTown && r.Id != (a != null ? a.Id : null) && r.Id != (b != null ? b.Id : null)
                                                  && !Routines.ProfileOf(r, Voice.NowYear).Churchgoer && !_engine.HasDied(r.Id)).ToList();
            if (men.Count == 0)
                return null;
            return men[(Voice.Hash(_placeTown + "|pew|" + day) & 0x7fffffff) % men.Count].Name;
        }
    }
}
