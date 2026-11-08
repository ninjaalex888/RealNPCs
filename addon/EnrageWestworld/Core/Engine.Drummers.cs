// Drummers on the train, the engine's part (FIVEDAY reserve R1; Core/Drummers.cs). The game side makes one of a train's
// arrivals a drummer (DrummerArrived: his trade, his firm's city, two days' stay booked in Store.Counts, a notice); the daily
// tick sends him off on the train when his days are up (DrummersDaily: Left and Fled, so nobody casts him again; quietly,
// since a drummer leaving is no news). Nothing new in the save.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _drummersCame, _drummersGone;

        /// One stepping down is a drummer: his line of goods and his firm, the stay booked, the town told.
        public void DrummerArrived(Resident r, string town, long now)
        {
            if (r == null || r.Id == null)
                return;
            string ware = Drummers.Ware(r.Id), firm = Drummers.Firm(r.Id);
            r.Trade = Drummers.Trade(ware);
            r.From = firm.Substring(firm.LastIndexOf(" of ", System.StringComparison.Ordinal) + 4);
            r.ByTrain = true;
            r.Regular = false;   // passing through: he never becomes a regular
            Store.Counts[Drummers.CountPrefix + r.Id] = (int)(now / 1440 + Drummers.StayDays);
            _drummersCame++;
            Push(new Toast { Kind = "life", Title = "Off the train in " + ShortTown(town), Weight = 20, Text = Drummers.Notice(r.Name, firm, ware, town, r.Id) });
            Log("DRUMMER " + town + ": " + r.Name + " for " + firm + ", in " + ware + ", for " + Drummers.StayDays + " days");
        }

        /// The daily tick: drummers whose days are up are gone on the train. Returns how many went.
        public int DrummersDaily(long now)
        {
            if (Cfg == null || Store.Counts.Count == 0)
                return 0;
            long day = now / 1440;
            List<string> due = null;
            foreach (KeyValuePair<string, int> kv in Store.Counts)
                if (kv.Key.StartsWith(Drummers.CountPrefix, System.StringComparison.Ordinal) && kv.Value <= day)
                    (due ?? (due = new List<string>())).Add(kv.Key);
            if (due == null)
                return 0;
            foreach (string key in due)
            {
                Store.Counts.Remove(key);
                Resident r = Store.FindResident(key.Substring(Drummers.CountPrefix.Length));
                if (r == null || r.Dead || r.Left)
                    continue;
                r.Left = true;
                r.LeftAt = now;
                r.Fled = true;
                r.FledAt = now;
                _drummersGone++;
                Log("DRUMMER " + (r.Town ?? "?") + ": " + r.Name + " is gone on the train, to the next town on the line");
            }
            return due.Count;
        }

        public string DrummersStatus()
        {
            int here = Store.Counts.Keys.Count(k => k.StartsWith(Drummers.CountPrefix, System.StringComparison.Ordinal));
            return _drummersCame + " came this session, " + _drummersGone + " gone; " + here + " in the towns now";
        }
    }
}
