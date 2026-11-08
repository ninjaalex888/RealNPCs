// A camp meeting in the engine (FIVEDAY reserve R4; Core/Revival.cs): the summer day's roll books three nights in a country
// town (posted: the notice board has them); each night over leaves its word in the morning, and the regulars a little warmer
// to the stranger who came; the third night's altar call saves a townsman whose worry is the drink or the cards
// (Store.Counts "saved:<id>" = the day), which Choosing reads for two weeks (SavedNow). Nothing new in the save.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _revivalsBooked, _revivalsSaved;

        private void RevivalDaily(long now)
        {
            if (Cfg == null || !Cfg.TownEvents || !Cfg.EventRevivals || Store.Events == null)
                return;
            long day = now / 1440;
            foreach (string town in Revival.Towns)
                if (Revival.Books(town, day, InnerLife.ClockMonth, Store.Count("revival:" + town) == 0 ? -999 : Store.Count("revival:" + town)))
                {
                    BookRevival(town, day, now);
                    break;   // one town a day at most
                }
        }

        private void BookRevival(string town, long day, long now)
        {
            if (Store.Events.Any(x => x != null && x.Kind == Revival.Kind && x.Town == town && !x.Done))
                return;
            string preacher = Revival.Preacher(town + "|" + day);
            foreach (TownEvent e in Revival.For(town, day, preacher))
            {
                e.Posted = true;
                e.PostedAt = now;
                Store.Events.Add(e);
            }
            Store.Counts["revival:" + town] = (int)day;
            _revivalsBooked++;
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 25, Text = Revival.Posted(town, preacher) });
            Log("EVENT revival " + town + ": three nights from tomorrow, " + preacher);
        }

        /// The Testing row: the first night here two game minutes from now, or the third (the altar call) when `third`.
        public string RevivalTest(string town, long now, bool third)
        {
            if (!Cfg.TownEvents || !Cfg.EventRevivals)
                return "off ([World] TownEvents, EventRevivals)";
            if (town == null)
                return "no town here";
            TownEvent e = Revival.For(town, now / 1440, Revival.Preacher(town + "|test"))[third ? 2 : 0];
            e.At = now + 2;
            e.Until = now + 60;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            return "A camp meeting's " + (third ? "third night (the altar call)" : "first night") + " in two minutes here.";
        }

        private void RevivalOver(TownEvent e, long now, bool attended)
        {
            e.Outcome = attended ? "attended" : "away";
            string savedName = e.Count >= Revival.Nights ? RevivalSinner(e, now / 1440) : null;
            if (savedName != null)
            {
                Store.Counts[Revival.SavedPrefix + e.AId] = (int)(now / 1440);
                _revivalsSaved++;
                BookBaptism(e, now);   // the next Sunday, at the river (Engine.Baptism.cs; FIVEDAY R5)
            }
            if (attended)
                foreach (Resident r in Store.Residents.Where(r => r.Regular && !r.Dead && r.Town == e.Town).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).Take(6))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + Revival.Affection, -100, 100);
                }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = Revival.Morning(e, savedName, attended), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT revival " + e.Town + ": night " + e.Count + " " + e.Outcome + (savedName != null ? "; " + savedName + " is saved" : ""));
        }

        /// The third night's sinner: a townsman whose worry today is the drink or the cards (kept on the event, so
        /// the staging can name him at the altar call and the end saves the same man). Null if the town has none.
        public string RevivalSinner(TownEvent e, long day)
        {
            if (e == null)
                return null;
            if (e.AId != null)
                return e.AName;
            foreach (Resident r in Store.Residents.Where(r => r.Town == e.Town && !r.Woman && !r.Dead && !r.Left && !r.Fled && r.Held == null).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)))
            {
                Person p = PersonOfResident(r);
                string kind;
                int stage;
                bool good;
                if (p == null || !InnerLife.WorryNow(p, day, out kind, out stage, out good) || !Revival.Sinner(kind))
                    continue;
                e.AId = r.Id;
                e.AName = r.Name;
                return r.Name;
            }
            return null;
        }

        /// Whether this person is still saved today (Choosing's lean), and whether they've backslid (for their thought).
        public bool SavedNow(string id, long day, out bool backslid)
        {
            backslid = false;
            if (id == null || Cfg == null || !Cfg.EventRevivals)
                return false;
            int at;
            if (!Store.Counts.TryGetValue(Revival.SavedPrefix + id, out at))
                return false;
            bool saved = BaptisedWithin(id, day) || Revival.StillSaved(id, at, day);   // baptised: three weeks, no backsliding (FIVEDAY R5)
            backslid = !saved && day - at < Revival.SavedDays;
            return saved;
        }

        public string RevivalStatus()
        {
            return (Cfg == null || !Cfg.EventRevivals ? "off ([World] EventRevivals)" : _revivalsBooked + " booked this session, " + _revivalsSaved + " saved") +
                   "; " + (Store.Events ?? new System.Collections.Generic.List<TownEvent>()).Count(x => x != null && x.Kind == Revival.Kind && !x.Done) + " nights to come";
        }
    }
}
