// Letters home, the engine's part (TENHOUR R7; Core/LettersHome.cs). From DailyTick: each who left a while ago, with a friend still in the town, may write. Kept in Store.Counts: the friend's key holds the day it came, and a second key who it's from.
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private void LettersHomeDaily(long now)
        {
            if (Cfg == null || !Cfg.LettersHome)
                return;
            long day = now / 1440;
            foreach (Resident r in Store.Residents.Where(x => x.Left && x.LeftAt > 0 && now - x.LeftAt >= LettersHome.FromDays * 1440 && now - x.LeftAt <= LettersHome.ToDays * 1440 && !x.Dead).ToList())
            {
                if (!Voice.Chance(r.Id + "|" + day + "|write", LettersHome.DayPct))
                    continue;
                SceneTie t = Store.Ties.Where(x => x.Has(r.Id) && x.Warmth > 0).OrderByDescending(x => x.Warmth).FirstOrDefault();
                string friend = t != null ? t.OtherId(r.Id) : null;
                Resident f = friend != null ? Store.FindResident(friend) : null;
                if (f == null || f.Dead || f.Left || f.Fled)
                    continue;
                Store.Counts[LettersHome.Key(friend)] = (int)day;
                Store.Counts[LettersHome.FromKey(friend)] = Voice.Hash(r.Id);
                Log("LETTER HOME: " + r.Name + " writes to " + f.Name);
            }
        }

        /// Who has written to this person these last few days, or null.
        public string LetterHomeFrom(string id, long day)
        {
            int at, from;
            if (id == null || !Store.Counts.TryGetValue(LettersHome.Key(id), out at) || day < at || day - at >= LettersHome.OnMindDays || !Store.Counts.TryGetValue(LettersHome.FromKey(id), out from))
                return null;
            Resident r = Store.Residents.FirstOrDefault(x => x.Left && Voice.Hash(x.Id) == from);
            return r != null ? r.Name : null;
        }
    }
}
