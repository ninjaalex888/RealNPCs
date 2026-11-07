// Stress that builds, the engine's part (FIVEDAY item 17; Core/StressLoad.cs): once a day, each of the towns' people the
// store holds a mind for is weighed: the week's load against their breaking point. Those past it break, once in three
// weeks, in their own way (kept in Store.Breakdowns, a rewind drops what came after the cut). The game side reads the habit
// (BrokenWay) for the choices, the thoughts, the domestic dispute's pick and the strain's snap; the train's leavers read it
// here (LeavesOn). Behind [Mind] StressBuilds.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _breaksSession;

        /// The worry's stage on a day, for the load: -1 none, 0-2 its stage, 3 settled well, 4 gone wrong.
        private static int WorryStageOn(Person p, long day)
        {
            string kind;
            int stage;
            bool good;
            if (p == null || !InnerLife.WorryNow(p, day, out kind, out stage, out good))
                return -1;
            return stage == 3 && !good ? 4 : stage;
        }

        /// The load of one person now, and their breaking point (for the daily weighing and F7).
        public int StressOf(Resident r, long now, out int point, out List<string> parts)
        {
            point = 0;
            parts = new List<string>();
            PersonState st = r != null ? Store.Find(r.Id) : null;
            if (st == null)
                return 0;
            Person p = PersonOfResident(r);
            int load = StressLoad.Load(st.Mind, now, d => WorryStageOn(p, d), out parts);
            point = StressLoad.BreakingPoint(Shadow.TraitsOf(TraitSource.From(p, "resident")));
            return load;
        }

        /// From DailyTick, before the train's leavers are found.
        private void StressDaily(long now)
        {
            if (!Cfg.StressBuilds)
                return;
            long day = now / 1440;
            foreach (Resident r in Store.Residents)
            {
                if (r == null || r.Id == null || r.Dead || r.Left || r.Fled || r.Town == null || r.Name == null)
                    continue;
                if (Store.Breakdowns.Any(b => b.Id == r.Id && day - b.Day < StressLoad.AgainDays))
                    continue;
                int point;
                List<string> parts;
                int load = StressOf(r, now, out point, out parts);
                if (load < point)
                    continue;
                Person p = PersonOfResident(r);
                bool grieving = parts.Any(x => x.Contains("friend"));   // a friend's death among the load
                bool married = p.Family == "married" || !string.IsNullOrEmpty(p.SpouseName);
                bool canLeave = Cfg.TrainTime && r.PostId == null && r.Held == null;
                string way = StressLoad.Way(Shadow.TraitsOf(TraitSource.From(p, "resident")), p, grieving, married, canLeave, r.Id + "|" + day);
                Store.Breakdowns.Add(new Breakdown { Id = r.Id, Way = way, Why = string.Join(", ", parts), Day = day, Load = load });
                _breaksSession++;
                Log("STRESS " + r.Name + " (" + r.Town + ") breaks: " + way + " (load " + load + " past " + point + ": " + string.Join(", ", parts) + ")");
            }
            if (Store.Breakdowns.Count > 300)
                Store.Breakdowns.RemoveRange(0, Store.Breakdowns.Count - 300);
        }

        /// The habit a breakdown left them with, while it lasts (a week), else null.
        public string BrokenWay(string id, long day)
        {
            if (Cfg == null || !Cfg.StressBuilds || id == null)
                return null;
            for (int i = Store.Breakdowns.Count - 1; i >= 0; i--)
            {
                Breakdown b = Store.Breakdowns[i];
                if (b.Id == id)
                    return StressLoad.Lasting(b, day) ? b.Way : null;
            }
            return null;
        }

        /// Whether this resident leaves on the train on this day for a breakdown (the day after it).
        private bool LeavesBroken(Resident r, long day)
        {
            if (Cfg == null || !Cfg.StressBuilds)
                return false;
            return Store.Breakdowns.Any(b => b.Id == r.Id && b.Way == StressLoad.Train && b.Day + 1 == day);
        }

        public string StressStatus(long now)
        {
            if (Cfg == null || !Cfg.StressBuilds)
                return "off ([Mind] StressBuilds)";
            long day = now / 1440;
            var lasting = Store.Breakdowns.Where(b => StressLoad.Lasting(b, day)).ToList();
            return _breaksSession + " broke this session; " + lasting.Count + " living with it now" +
                   (lasting.Count > 0 ? " (" + string.Join(", ", lasting.Take(6).Select(b => (Store.FindResident(b.Id)?.Name ?? b.Id) + ": " + b.Way)) + ")" : "") +
                   "; " + Store.Breakdowns.Count + " kept";
        }
    }
}
