// A cold snap, the engine's part (TENHOUR R3; Core/ColdSnap.cs). From DailyTick: on the first day of a snap the town hears, and an old man may not wake: a death like any other, mourned and buried.
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private void ColdSnapDaily(long now)
        {
            if (Cfg == null || !Cfg.ColdSnaps || !ColdSnap.FirstDay(InnerLife.ClockYear, InnerLife.ClockMonth, InnerLife.ClockDay))
                return;
            foreach (string town in ColdSnap.Towns)
            {
                string key = "coldsnap:" + town + ":" + now / 1440;
                if (Store.Counts.ContainsKey(key))
                    continue;
                Store.Counts[key] = 1;
                Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 40, Text = ColdSnap.Line(town, key) });
                Log("COLD SNAP at " + town);
                if (!Voice.Chance(key + "|takes", ColdSnap.DeathPct))
                    continue;
                Resident old = Store.Residents.Where(r => r.Town == town && !r.Woman && !r.Dead && !r.Left && !r.Fled && r.Held == null && r.PostId == null && r.Cast == null && r.Name != null && InnerLife.TradeGroup(r.Trade) == "old")
                    .OrderBy(r => Voice.Hash(key + "|" + r.Id)).FirstOrDefault();
                if (old == null)
                    continue;
                NoteDied(new DeathEntry { Id = old.Id, Name = old.Name, Kind = "resident", At = now, Town = town, How = "the cold", By = "unknown" });
                Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 55, Text = ColdSnap.DeathLine(town, old.Name, key) });
                Log("COLD SNAP at " + town + ": " + old.Name + " didn't wake");
            }
        }
    }
}
