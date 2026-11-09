// Roundup season, the engine's part (TENHOUR R1; Core/Roundup.cs). From DailyTick: on the first day after a roundup the town's box says the hands are in, and one of them has come back hurt.
using System;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private void RoundupDaily(long now)
        {
            if (Cfg == null || !Cfg.RoundupSeason || !Roundup.FirstDay(InnerLife.ClockMonth, InnerLife.ClockDay))
                return;
            long day = now / 1440;
            foreach (string town in Roundup.Towns)
            {
                string key = "roundup:" + town + ":" + InnerLife.ClockYear + ":" + InnerLife.ClockMonth;
                if (Store.Counts.ContainsKey(key))
                    continue;
                Store.Counts[key] = (int)day;
                Resident hurt = Store.Residents.Where(r => r.Town == town && !r.Woman && !r.Dead && !r.Left && !r.Fled && r.PostId == null && r.Cast == null && r.Name != null && InnerLife.TradeGroup(r.Trade) == "farm")
                    .OrderBy(r => Voice.Hash(key + "|" + r.Id)).FirstOrDefault();
                if (hurt != null)
                    Store.Counts[Roundup.HurtKey(hurt.Id)] = (int)day;
                Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 35, Text = Roundup.InLine(town, hurt != null ? hurt.Name : null, key) });
                Log("ROUNDUP in at " + town + (hurt != null ? ": " + hurt.Name + " hurt" : ""));
            }
        }

        /// Whether this one is hurt from the roundup, for the two weeks it takes.
        public bool RoundupHurt(string id, long day)
        {
            int at;
            return id != null && Store.Counts.TryGetValue(Roundup.HurtKey(id), out at) && day >= at && day - at < Roundup.HurtDays;
        }
    }
}
