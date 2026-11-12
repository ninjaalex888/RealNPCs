// Known by the horse, the engine's half (LIVES item 6a; Core/HorseTalk.cs): the tale a town tells of the player whose horse
// is the one he rides in on now, looked up once a game minute a town.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private string _horseTaleTown;
        private long _horseTaleAt = -1;
        private Deed _horseTale;

        /// The deed whose tale this town tells (its rumour of the last week) and whose horse is the one the player is riding
        /// now; null when he's on foot, on another horse, or the town tells no such tale.
        public Deed HorseTaleIn(string town, long now)
        {
            if (!HorseTalk.On || town == null || !PlayerMounted || PlayerHorse == null || Store.TownRumours == null)
                return null;
            if (_horseTaleTown == town && now / 1 == _horseTaleAt)
                return _horseTale;
            _horseTaleTown = town;
            _horseTaleAt = now;
            _horseTale = null;
            TownRumour newest = null;
            foreach (TownRumour r in Store.TownRumours)
                if (r != null && r.Town == town && r.DeedId != null && now - r.At >= 0 && now - r.At < 7 * 1440L && (newest == null || r.At > newest.At))
                    newest = r;
            if (newest == null)
                return null;
            for (int i = Store.Deeds.Count - 1; i >= 0; i--)
            {
                Deed d = Store.Deeds[i];
                if (d != null && d.Id == newest.DeedId)
                {
                    _horseTale = d.Horse != null && d.Horse == PlayerHorse && !d.Unseen ? d : null;
                    break;
                }
            }
            return _horseTale;
        }
    }
}
