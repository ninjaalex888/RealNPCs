// Fence cutters, the engine's part (FIVEDAY item 31; Core/FenceCutters.cs). The daily tick rolls the day (the 12% and
// the six days' gap) and books the morning minute; the world tick, at that minute, picks a town of the open range with
// two men to stand for it, the homesteader and the one he blames (his feud neighbour first: the worry links), notes the
// "fencecut" tie between them (two steps colder: the feud climbs) and keeps the cut as LastFence for the game side, which
// tells you what you saw if you were near that night. The counts live in Store.Counts (fence:due, fence:lastday), as the
// mine's do: nothing new in the save. Behind [World] FenceCutters.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        public FenceCut LastFence { get; private set; }
        private int _fencesSession;

        private void FenceDaily(long now)
        {
            if (Cfg == null || !Cfg.FenceCutters)
                return;
            long day = now / 1440;
            int minute;
            if (Store.Count("fence:due") > now)
                return;   // one booked already
            if (FenceCutters.Today(day, Store.Count("fence:lastday") == 0 ? -99 : Store.Count("fence:lastday"), out minute))
            {
                Store.Counts["fence:due"] = (int)(day * 1440 + minute);
                Log("FENCE a fence will be found cut this morning at " + minute / 60 + ":" + (minute % 60).ToString("00"));
            }
        }

        /// From the world tick: the booked minute come.
        private void FenceTick(long now)
        {
            int due = Store.Count("fence:due");
            if (Cfg == null || !Cfg.FenceCutters || due <= 0 || now < due)
                return;
            Store.Counts["fence:due"] = 0;
            FenceCutNow(now, null, false);
        }

        /// The cut found (the Testing row's too, in the town given): who, whom he blames, the tie, the truth.
        public string FenceCutNow(long now, string inTown, bool test)
        {
            // the feud neighbours, from the worry links (the worrier -> the one at the other end)
            var feud = new Dictionary<string, string>();
            foreach (WorryLink l in Store.Links)
                if (l.Kind == "feud" && !l.Ended && l.Id != null && l.OtherId != null && !feud.ContainsKey(l.Id))
                    feud[l.Id] = l.OtherId;
            string seed = "fence|" + now;
            IEnumerable<string> towns = inTown != null ? (IEnumerable<string>)new[] { inTown } : FenceCutters.Towns.OrderBy(t => Voice.Hash(seed + "|" + t));
            foreach (string town in towns)
            {
                Resident h, c;
                if (!FenceCutters.Pair(Store.Residents.Where(r => r.Town == town).ToList(), feud, seed, out h, out c))
                    continue;
                var f = new FenceCut { At = now, Town = town, HId = h.Id, HName = h.Name, CId = c.Id, CName = c.Name, Drifters = FenceCutters.WereDrifters(seed) };
                SceneTie t = Ties.Note(Store.Ties, h.Id, h.Name, c.Id, c.Name, town, "fencecut", now, h.Id);
                // cold enough, and he means to get even with the man he blames (the ties' own get-even plans run from
                // the wronged to the doer, and here the Doer is the one who blames, so the plan is made here; with MindPlans)
                string plan = Cfg.MindPlans && t != null && t.Warmth <= -2 ? Plans.Add(Store.Plans, h.Id, h.Name, "geteven", c.Id, c.Name, town, 4, now) : null;
                if (plan != null)
                    Log("PLAN " + plan + " (the cut wire)");
                Store.Counts["fence:lastday"] = (int)(now / 1440);
                LastFence = f;
                _fencesSession++;
                Log("FENCE " + h.Name + "'s wire cut in the night at " + town + "; he blames " + c.Name + (f.Drifters ? " (it was drifters)" : " (it was his hands)") + (test ? " (a test)" : ""));
                return null;
            }
            return inTown != null ? "too few men of " + inTown + " known to RealNPCs (two)" : "no town of the open range with two men known to RealNPCs";
        }

        public string FenceStatus()
        {
            if (Cfg == null || !Cfg.FenceCutters)
                return "off ([World] FenceCutters)";
            int due = Store.Count("fence:due");
            return _fencesSession + " fences cut this session; the last day " + Store.Count("fence:lastday") + (due > 0 ? "; one booked at minute " + due : "") +
                   (LastFence != null ? "; the last: " + LastFence.HName + " blames " + LastFence.CName + " at " + LastFence.Town + (LastFence.Drifters ? " (drifters)" : "") : "");
        }
    }
}
