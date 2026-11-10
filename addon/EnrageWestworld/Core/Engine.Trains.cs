// Train time in the engine (SIXDAY item 11b; Core/Trains.cs): who is leaving today (a worry just ended in leaving), marked gone
// for good when they go (seen on the platform, or at the next day's tick, whether or not anyone saw); who arrived (a newcomer
// or kin, made a resident by the game side, noted here). Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private readonly Dictionary<string, string> _leaving = new Dictionary<string, string>();   // resident id -> why, today (not saved: found again from the worry)
        private long _leavingDay = -1;
        private int _trainArrivals, _trainDepartures;

        /// Why this resident leaves on this day, or null: a worry at its last stage that ends in leaving. Read off the worry
        /// calendar (the same by id and day whenever it's asked), so a reload or a day boundary loses nobody.
        private string LeavesOn(Resident r, long day)
        {
            if (r == null || r.Id == null || r.Dead || r.Fled || r.Left || r.Held != null || r.PostId != null || r.Town == null || day < 0)
                return null;   // (a counter keeper keeps his counter: the post's own records are not ours to send west; the review of item 14)
            if (LeavesBroken(r, day))
                return StressLoad.TrainWhy;   // broken by the week's load, they take the train the day after (FIVEDAY item 17)
            Person p = PersonOfResident(r);
            if (p == null)
                return null;
            string kind;
            int stage;
            bool good;
            if (!InnerLife.WorryNow(p, day, out kind, out stage, out good) || stage < 3)
                return null;
            long arc = InnerLife.ArcFor(r.Id, day);
            return Trains.Leaves(kind, good, r.Id + "|" + arc) ? Trains.LeavesWhy(kind) : null;
        }

        /// Today's leavers, found once a day (the daily tick, or the platform's first ask of the day).
        private void FindLeavers(long today)
        {
            if (_leavingDay == today)
                return;
            _leaving.Clear();
            _leavingDay = today;
            foreach (Resident r in Store.Residents)
            {
                string why = LeavesOn(r, today);
                if (why != null)
                    _leaving[r.Id] = why;
            }
            if (_leaving.Count > 0)
                Log("TRAIN leaving today: " + string.Join(", ", _leaving.Select(kv => (Store.FindResident(kv.Key)?.Name ?? kv.Key) + " (" + kv.Value + ")")));
        }

        /// From DailyTick: yesterday's leavers who weren't seen off are gone all the same (found again from the calendar);
        /// today's are found.
        private void DeparturesDaily(long now)
        {
            if (!Cfg.TrainTime)
                return;
            long today = now / 1440;
            foreach (Resident r in Store.Residents.ToList())
            {
                string why = LeavesOn(r, today - 1);
                if (why != null)
                    Departed(r, now, false, why);
            }
            FindLeavers(today);
        }

        /// Today's leavers from this town, for the platform (the game side).
        public List<Resident> DeparturesDue(string town, long now)
        {
            var list = new List<Resident>();
            if (!Cfg.TrainTime || town == null)
                return list;
            FindLeavers(now / 1440);
            foreach (KeyValuePair<string, string> kv in _leaving)
            {
                Resident r = Store.FindResident(kv.Key);
                if (r != null && !r.Left && !r.Dead && r.Town == town)
                    list.Add(r);
            }
            return list;
        }

        public string LeavingWhy(string id)
        {
            string why;
            return id != null && _leaving.TryGetValue(id, out why) ? why : null;
        }

        private int _trainReturns;

        /// A neighbour who left this town on the train, a week ago or more, due back on this one (one in three, by the day); null
        /// for none (SIXDAY item 22; roadmap 36).
        public Resident ReturnerDue(string town, long now)
        {
            if (Cfg == null || !Cfg.ReturningNeighbours || town == null)
                return null;
            foreach (Resident r in Store.Residents)
                if (r != null && r.Left && !r.Dead && r.Town == town && r.Name != null && !Drummers.Is(r.Trade) && !Store.Counts.ContainsKey("gone:" + r.Id) && now - r.LeftAt >= Trains.ReturnAfterDays * 1440L && Voice.Chance(r.Id + "|back|" + now / 1440, Trains.ReturnChance))
                    return r;
            return null;
        }

        /// Back off the train: back among the town's people (Left and Fled off), a notice, the log, the ledger's arrival.
        public void ReturnedByTrain(Resident r, string town, long now)
        {
            if (r == null)
                return;
            long gone = r.LeftAt > 0 ? (now - r.LeftAt) / 1440 : 0;
            r.Left = false;
            r.LeftAt = 0;
            r.Fled = false;
            r.FledAt = 0;
            r.ByTrain = true;   // the ledger's arrival: with MetAt now, the town's index counts them as come on this train (the departure's -2 stood while they were away; the review of items 20-27)
            r.MetAt = now;
            PersonState st = Store.Get(PersonOfResident(r));
            st.Mark("back:" + now);
            _trainReturns++;
            Push(new Toast { Kind = "life", Title = "Off the train in " + ShortTown(town), Weight = 30, Text = Trains.BackNotice(town, r.Name, gone) });
            Log("TRAIN " + town + ": " + r.Name + " is back, " + gone + " days after leaving");
        }

        /// Gone for good on the train: never cast again (Fled as well, which every cast checks), a notice, the log.
        public void Departed(Resident r, long now, bool seen, string why)
        {
            if (r == null || r.Left)
                return;
            r.Left = true;
            r.LeftAt = now;
            r.Fled = true;
            r.FledAt = now;
            _leaving.Remove(r.Id);
            _trainDepartures++;
            Push(new Toast { Kind = "life", Title = "The train from " + ShortTown(r.Town), Weight = 30, Text = Trains.LeftNotice(r.Town, r.Name, why ?? "gone", seen) });
            Log("TRAIN " + r.Town + ": departed " + r.Name + " (" + (why ?? "gone") + (seen ? "; you saw them go" : "; away") + ")");
        }

        /// Someone stepped down and became a resident of the town (made by the game side): kin of someone here, or a newcomer.
        public void ArrivedByTrain(Resident r, string town, Resident kinOf, string relation, long now)
        {
            if (r == null)
                return;
            if (kinOf != null && relation != null)
            {
                r.Kin = relation;
                r.KinOf = kinOf.Name;
            }
            else
                r.From = Trains.NewcomerFrom(r.Id);
            string groom = kinOf == null && Cfg.NewFaces ? NewFaceArrives(r, town, now) : null;   // who the stranger comes as (Core/NewFaces.cs)
            r.ByTrain = true;   // the ledger counts these alone (a post's successor has Kin too; the review of items 11-12)
            _trainArrivals++;
            Push(new Toast { Kind = "life", Title = "Off the train in " + ShortTown(town), Weight = 25, Text = NewFaces.Notice(r.NewAs, ShortTown(town), r.Name, groom, r.From) ?? Trains.ArrivedNotice(town, r.Name, r.Kin, r.KinOf, r.From) });
            Log("TRAIN " + town + ": arrival " + r.Name + (r.Kin != null ? " (" + r.KinOf + "'s " + r.Kin + ")" : " (from " + r.From + ")"));
        }

        /// A stranger off the train comes as somebody: their trade, and for a bride, the man she's promised to (a courting tie). The groom's name, or null.
        private string NewFaceArrives(Resident r, string town, long now)
        {
            string kind = NewFaces.KindOf(r.Id, r.Woman);
            if (kind == null)
                return null;
            Resident groom = null;
            if (kind == "bride")
            {
                groom = Store.Residents.Where(x => x != null && x.Town == town && !x.Woman && x.Regular && !x.Dead && !x.Fled && !x.Left && x.Id != r.Id && x.Name != null && !Store.Ties.Any(t => t != null && t.Courted && t.Has(x.Id)))
                                       .OrderBy(x => Voice.Hash(r.Id + "|groom|" + x.Id)).FirstOrDefault();
                if (groom == null)
                    kind = "settler";   // nobody here free to have sent for her: she comes to settle
            }
            r.NewAs = kind;
            r.NewAt = now;
            if (NewFaces.TradeOf(kind) != null)
                r.Trade = NewFaces.TradeOf(kind);
            if (groom != null)
            {
                SceneTie t = Ties.Note(Store.Ties, groom.Id, groom.Name, r.Id, r.Name, town, "flirt", now);
                if (t != null)
                {
                    t.Courted = true;
                    t.Warmth = Math.Max(t.Warmth, 1);
                }
            }
            Log("TRAIN " + town + ": " + r.Name + " comes as " + kind + (groom != null ? ", to marry " + groom.Name : ""));
            return groom != null ? groom.Name : null;
        }

        public string TrainsEngineStatus()
        {
            return _trainArrivals + " arrivals, " + _trainDepartures + " departures noted" + (_leaving.Count > 0 ? "; leaving today: " + string.Join(", ", _leaving.Keys.Select(k => Store.FindResident(k)?.Name ?? k)) : "");
        }
    }
}
