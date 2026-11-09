// This week in town, the engine's part (TENHOUR R5; Core/WeekColumn.cs). From DailyTick, on a Sunday: the column for the town you're nearest, from the people ledger, the deeds and what was seen in its streets.
using System;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private void WeekColumnDaily(long now)
        {
            if (Cfg == null || !Cfg.WeekColumn || InnerLife.Weekday != 0)
                return;
            string town = CurrentTown ?? Rumours.NearestTown(PlayerX, PlayerY);
            if (town == null)
                return;
            string key = "weekcolumn:" + town + ":" + now / 1440;
            if (Store.Counts.ContainsKey(key))
                return;
            Store.Counts[key] = 1;
            string text = WeekColumnNow(town, now, key);
            if (text == null)
                return;
            Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = text });
            Log("THE WEEK IN " + town + ": " + text);
        }

        /// The column for this town as of now, from what the store has of the last seven days.
        public string WeekColumnNow(string town, long now, string seed)
        {
            long since = now - WeekColumn.Days * 1440;
            var dead = (Store.Deaths ?? new System.Collections.Generic.List<DeathEntry>()).Where(d => d.Town == town && d.At >= since && d.At <= now && d.Name != null).Select(d => d.Name).Distinct().ToList();
            var married = Store.Weddings.Where(w => w.Done && w.Town == town && w.At >= since && w.At <= now && w.AName != null && w.BName != null).Select(w => w.AName + " and " + w.BName).Distinct().ToList();
            int fights = (Store.WorldLog ?? new System.Collections.Generic.List<Happening>()).Count(e => e.Town == town && e.At >= since && e.At <= now && e.Kind != null && (e.Kind == "scene:brawl" || e.Kind == "scene:rough" || e.Kind == "scene:standoff"));
            return WeekColumn.Text(town, dead, married, fights, seed);
        }
    }
}
