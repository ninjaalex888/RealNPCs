// The town ledgers in the engine (SIXDAY item 11c; Core/TownLedgers.cs): every town's ledger read off the records at the daily
// tick (and on first use after a load), the change logged, the monthly mention in the paper, the regulars' cap and the town's
// mood for the thoughts. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private long _ledgersReadAt = -1;

        /// Every town's ledger, read off the records; a change of index or mood logged `TOWN name: index, mood (why)`.
        public void LedgersRead(long now, bool log)
        {
            if (!Cfg.TownLedger || Store.TownLedgers == null)
                return;
            foreach (string town in Rumours.Towns.Keys)
            {
                TownLedger keep;
                Store.TownLedgers.TryGetValue(town, out keep);
                int wasIndex = keep != null ? keep.Index : TownLedgers.Start;
                string wasMood = keep != null ? keep.Mood : "steady";
                TownLedger l = TownLedgers.Read(town, now, Store.Deaths, Store.Deeds, Store.Residents, Store.Events, keep);
                if (keep == null && l.Index == TownLedgers.Start && l.Mood == "steady")
                    continue;   // a town with no records keeps none
                Store.TownLedgers[town] = l;
                if (log && (l.Index != wasIndex || l.Mood != wasMood))
                    Log("TOWN " + town + ": " + l.Index + ", " + l.Mood + (l.Why != null ? " (" + l.Why + ")" : "") + " (was " + wasIndex + ", " + wasMood + ")");
            }
            _ledgersReadAt = now;
        }

        /// From DailyTick: the ledgers read, and the monthly mention for towns he has been to (the first of the month when the
        /// clock's date is known, else every thirty days).
        private void LedgersDaily(long now)
        {
            if (!Cfg.TownLedger)
                return;
            LedgersRead(now, true);
            int month = InnerLife.ClockMonth, day = InnerLife.ClockDay;
            foreach (TownLedger l in Store.TownLedgers.Values.ToList())
            {
                if (l == null || l.Town == null || !Store.TownVisits.ContainsKey(l.Town))
                    continue;
                bool firstOfMonth = day == 1, clockUnknown = day <= 0;
                // the first of the month (twenty days at least since the last), every thirty days with the clock's day unknown, and a
                // month that was never played on its first caught up at forty-five days (the review of items 11-12)
                bool due = l.LastPaperAt == 0 ? (firstOfMonth || clockUnknown)
                         : clockUnknown ? now - l.LastPaperAt >= 30 * 1440L
                         : (firstOfMonth && now - l.LastPaperAt >= 20 * 1440L) || now - l.LastPaperAt >= 45 * 1440L;
                if (!due)
                    continue;
                l.LastPaperAt = now;
                if (Cfg.Newspaper)
                    Requests.Add(new WorldRequest { Kind = "paper", Town = l.Town, Text = TownLedgers.Paper(l, month) });
                Log("TOWN " + l.Town + ": the month's mention in the paper (" + l.Index + ", " + l.Mood + ")");
            }
        }

        public TownLedger LedgerOf(string town)
        {
            if (!Cfg.TownLedger || town == null || Store.TownLedgers == null)
                return null;
            if (_ledgersReadAt < 0)
                LedgersRead(Store.LastGameMin, false);   // the first ask after a load
            TownLedger l;
            return Store.TownLedgers.TryGetValue(town, out l) ? l : null;
        }

        /// The town's month, for its people's thoughts: grieving, fearful, prospering; null when steady or unknown.
        public string TownMoodOf(string town)
        {
            TownLedger l = LedgerOf(town);
            return l != null && l.Mood != null && l.Mood != "steady" ? l.Mood : null;
        }

        /// The regulars this town keeps: the setting scaled by its index, never below min(10, the setting).
        public int TownCap(string town)
        {
            TownLedger l = LedgerOf(town);
            return l != null ? TownLedgers.Cap(l.Index, Cfg.RegularsPerTown) : Cfg.RegularsPerTown;
        }

        /// For F7: every town with a ledger.
        public string LedgersStatus()
        {
            if (!Cfg.TownLedger)
                return "off ([World] TownLedger)";
            if (_ledgersReadAt < 0)
                LedgersRead(Store.LastGameMin, false);
            if (Store.TownLedgers == null || Store.TownLedgers.Count == 0)
                return "every town at 50, steady (nothing on any record yet)";
            return string.Join("; ", Store.TownLedgers.Values.Where(l => l != null).OrderBy(l => l.Town).Select(l =>
                l.Town + " " + l.Index + " " + l.Mood + " (cap " + TownLedgers.Cap(l.Index, Cfg.RegularsPerTown) +
                (l.DeathsByPlayer > 0 ? ", " + l.DeathsByPlayer + " killed by you" : "") + (l.DeathsOther > 0 ? ", " + l.DeathsOther + " other deaths" : "") +
                (l.Hangings > 0 ? ", " + l.Hangings + " hanged" : "") + (l.Departures > 0 ? ", " + l.Departures + " left" : "") + (l.Arrivals > 0 ? ", " + l.Arrivals + " came" : "") +
                (l.GoodDeeds > 0 ? ", " + l.GoodDeeds + " good turns" : "") + (l.Why != null ? "; last: " + l.Why : "") + ")"));
        }
    }
}
