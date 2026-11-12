// The news over the fence's game side (LIVES item 2e; [Scenes] NewsOverFence): the town's freshest tie between two of its
// people (Ties.TalkOf, the last two days), looked up once in ten seconds, and the pair of names for the scene.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _newsLookAt = -100000;
        private string _newsTown;
        private List<SceneTie> _newsTies;

        private List<SceneTie> TownNewsHere(long gameNow)
        {
            int t = Game.GameTime;
            if (t - _newsLookAt < 10000 && _newsTown == _placeTown)
                return _newsTies;
            _newsLookAt = t;
            _newsTown = _placeTown;
            List<SceneTie> ties = _placeTown != null ? Ties.TalkOf(_store.Ties, _placeTown, null, gameNow) : null;
            _newsTies = ties != null && ties.Count > 0 ? ties : null;
            return _newsTies;
        }

        /// The freshest news of two others (neither of the two talking, both alive): their first names and its kind; null: none.
        private string[] TownNewsFor(Someone a, Someone b, long gameNow)
        {
            List<SceneTie> ties = TownNewsHere(gameNow);
            if (ties == null)
                return null;
            string ai = a != null ? a.Id : null, bi = b != null ? b.Id : null;
            SceneTie t = ties.FirstOrDefault(x => !x.Has(ai) && !x.Has(bi) && x.AName != null && x.BName != null && !_engine.HasDied(x.A) && !_engine.HasDied(x.B));
            if (t == null)
                return null;
            bool bDid = t.Doer != null && t.Doer == t.B;   // who did the good turn comes first (the review)
            return new[] { Person.FirstOf(bDid ? t.BName : t.AName), Person.FirstOf(bDid ? t.AName : t.BName), NewsOverFence.NewsKind(t) };
        }
    }
}
