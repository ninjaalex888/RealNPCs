// The engine's side of the frontier scenes and the player's encounters: what a town knows about him when he
// rides in, the crime a posse would form over, the stranger he shot whose widow is in town, the latest from
// the papers, and his legend as the old-timers tell it.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// What a local says when the player rides in, or null (most arrivals pass without a word).
        public ArrivalTalk Arrival(string town, bool wanted, bool firstVisit, int hostTier, long now, string seed)
        {
            if (town == null)
                return null;
            // the deed this town knows best: done here lately, or heard of here
            Deed top = null;
            int topWeight = 0;
            foreach (Deed d in Store.Deeds.Where(x => x.Known && x.Weight >= 10 && now - x.At < 14 * 1440 && x.Kind != "visit" && x.Kind != "hunt" && !Deeds.IsShop(x.Kind)))
            {
                int w = 0;
                if (d.Town == town)
                {
                    w = d.Weight + 10;
                }
                else
                {
                    TownRumour r = Store.TownRumours.FirstOrDefault(x => x.DeedId == d.Id && x.Town == town);
                    if (r != null)
                        w = d.Weight * Math.Max(20, r.Fidelity) / 100;
                }
                if (w > topWeight)
                {
                    topWeight = w;
                    top = d;
                }
            }
            int notoriety = Store.Deeds.Count(x => x.Known && x.Weight >= 25);
            return Pleas.Arrival(town, top, wanted, firstVisit, notoriety, hostTier, seed);
        }

        /// A crime here in the last day that men would get up a posse over.
        public Deed RecentCrime(string town, long now)
        {
            if (town == null)
                return null;
            return Store.Deeds.Where(d => d.Town == town && d.TalkAt == 0 && now - d.At < 1440 && now >= d.At && d.Weight >= 45 && (Deeds.IsViolent(d.Kind) || Pleas.IsThieving(d.Kind)))
                .OrderByDescending(d => d.Weight).FirstOrDefault();
        }

        /// How the witnesses describe whoever did it (null if nobody saw).
        public string CrimeDesc(Deed d)
        {
            if (d == null || d.Witnesses <= 0)
                return null;
            return Player.Scarred ? Voice.Pick(d.Id + "desc", "it was a scarred man on a dark horse", "it was a lean man with a scarred face")
                                  : Voice.Pick(d.Id + "desc", "it was a big man in a worn hat", "it was a tall man who gave no name", "it was a rough-looking stranger");
        }

        /// A stranger the player killed in this town in the last week, not yet mourned on screen; none on a game day the town has
        /// mourned someone already (NIGHT4 fix 6: one widow grieved two lawmen forty seconds apart, the same line both times).
        public Fallen MournFor(string town, long now)
        {
            if (town == null || MournedToday(Store.Fallen, town, now))
                return null;
            return Store.Fallen.Where(f => f.Town == town && !f.Mourned && f.Returned == 0 && now - f.At < 7 * 1440 && now - f.At > 120)
                .OrderByDescending(f => f.At).FirstOrDefault();
        }

        /// Whether someone was grieved on screen in this town on this game day.
        public static bool MournedToday(IEnumerable<Fallen> fallen, string town, long now)
        {
            foreach (Fallen f in fallen)
                if (f.Town == town && f.Mourned && f.MournedAt > 0 && f.MournedAt / 1440 == now / 1440)
                    return true;
            return false;
        }

        /// Marks the dead as grieved on screen now.
        public void NoteMourned(Fallen f, long now)
        {
            f.Mourned = true;
            f.MournedAt = now;
        }

        /// The latest small items from the papers, for somebody to read aloud.
        public List<string> PaperItems(long now, int n)
        {
            List<string> items = new List<string>();
            foreach (Deed d in Store.Deeds.Where(x => x.Printed && now - x.At < 5 * 1440).OrderByDescending(x => x.At).Take(n * 2))
            {
                string p = Deeds.Paper(d, Player, false, d.Id);
                if (p != null && !items.Contains(p))
                    items.Add(p);
            }
            // the almanac's dated news of the fortnight beside the town's own (Core/Almanac.cs): one of them keeps a place when there is any
            List<string> dated = Almanac.PaperItems(InnerLife.ClockYear, InnerLife.ClockMonth, InnerLife.ClockDay, n);
            if (dated.Count > 0)
                items = items.Take(Math.Max(0, n - 1)).Concat(dated).ToList();
            return items.Take(n).ToList();
        }

        /// The player's legend, bent out of shape the way old-timers tell it, or null if he hasn't earned one.
        public string Legend(long now, string seed)
        {
            Deed d = Store.Deeds.Where(x => x.Known && x.Weight >= 50 && (Deeds.IsViolent(x.Kind) || x.Kind == "bankrobbery" || x.Kind == "trainrobbery" || x.Kind == "stagecoach"))
                .OrderByDescending(x => x.Weight).ThenByDescending(x => x.At).FirstOrDefault();
            if (d == null)
                return null;
            TownRumour r = new TownRumour { Id = "legend" + d.Id + (now / 1440), Fidelity = 15 + Voice.Hash(seed + d.Id) % 30, Origin = d.Town, Town = "elsewhere" };
            return Rumours.Version(r, d, Player, false);
        }
    }
}
