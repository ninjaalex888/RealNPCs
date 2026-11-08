// A foreclosure sale, the engine's part (FIVEDAY reserve R3; Core/Foreclosure.cs): whose goods the bank sells today in a town,
// read off the worry calendar (a "bank" worry ending badly today), the same whenever it's asked. Nothing saved.
namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// The resident of this town the bank sells up today, or null.
        public Resident ForeclosureToday(string town, long day)
        {
            if (town == null || Cfg == null || !Cfg.Foreclosures)
                return null;
            foreach (Resident r in Store.Residents)
            {
                if (r == null || r.Town != town || r.Dead || r.Left || r.Fled || r.Held != null || r.PostId != null || r.Cast != null)
                    continue;
                Person p = PersonOfResident(r);
                string kind;
                int stage;
                bool good;
                // the first day of the bad end only (the last stage runs for days; the sale is once)
                if (p != null && InnerLife.WorryNow(p, day, out kind, out stage, out good) && Foreclosure.SoldUp(kind, stage, good) &&
                    (InnerLife.StageFor(r.Id, day - 1) < 3 || InnerLife.ArcFor(r.Id, day - 1) != InnerLife.ArcFor(r.Id, day)) &&
                    Foreclosure.Public(r.Id + "|" + InnerLife.ArcFor(r.Id, day)))   // one in four goes to a public sale; the rest settle quietly
                    return r;
            }
            return null;
        }
    }
}
