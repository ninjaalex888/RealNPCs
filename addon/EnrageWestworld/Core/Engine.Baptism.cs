// A river baptism in the engine (FIVEDAY reserve R5; Core/Baptism.cs): booked from the camp meeting's altar call for the
// next Sunday afternoon; over, it seals the man's saving (Store.Counts "baptised:<id>" = the day: SavedNow gives three weeks
// and no backsliding), warms the regulars to the stranger who stood on the bank, and leaves the morning's word. Nothing new
// in the save.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _baptisms;

        /// From RevivalOver: the saved man's baptism the next Sunday.
        private void BookBaptism(TownEvent revival, long now)
        {
            if (Cfg == null || !Cfg.EventBaptisms || Store.Events == null || revival == null || revival.AId == null)
                return;
            TownEvent e = Baptism.For(revival.Town, revival.AId, revival.AName, now / 1440, InnerLife.Weekday < 0 ? 0 : InnerLife.Weekday, revival.Extra);
            if (e == null || Store.Events.Any(x => x != null && x.Kind == Baptism.Kind && x.AId == e.AId && !x.Done))
                return;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            Log("EVENT baptism " + e.Town + ": " + e.AName + " at the river on Sunday afternoon");
        }

        public string BaptismTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventBaptisms)
                return "off ([World] TownEvents, EventBaptisms)";
            if (town == null)
                return "no town here";
            Resident man = Store.Residents.FirstOrDefault(r => !r.Dead && !r.Woman && !r.Left && r.Town == town);
            if (man == null)
                return "no man of " + town + " known to RealNPCs to be baptised";
            TownEvent e = Baptism.For(town, man.Id, man.Name, now / 1440, 6, Revival.Preacher(town + "|test"));
            e.At = now + 2;
            e.Until = now + 60;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            return "A baptism at the river in two minutes here, for " + man.Name + " (the riverbank spot, recorded with the spots tool).";
        }

        private void BaptismOver(TownEvent e, long now, bool attended)
        {
            e.Outcome = attended ? "attended" : "away";
            if (e.AId != null)
                Store.Counts[Baptism.Prefix + e.AId] = (int)(now / 1440);
            _baptisms++;
            if (attended)
                foreach (Resident r in Store.Residents.Where(r => r.Regular && !r.Dead && r.Town == e.Town).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).Take(5))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + Revival.Affection, -100, 100);
                }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = Baptism.Morning(e, attended), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT baptism " + e.Town + ": " + e.AName + " baptised (" + e.Outcome + ")");
        }

        /// Baptised lately: three weeks saved, no backsliding (SavedNow).
        private bool BaptisedWithin(string id, long day)
        {
            int at;
            return id != null && Store.Counts.TryGetValue(Baptism.Prefix + id, out at) && day - at >= 0 && day - at < Baptism.SavedDays;
        }
    }
}
