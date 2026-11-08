// A reward posted for a neighbour, the engine's part (FIVEDAY reserve R30; Core/NeighbourReward.cs): posted when a resident
// beats a man to death in a brawl, read back for a town, ended when the player turns him in or warns him. Kept in
// Store.Counts (the day posted, the cents), no new save field; a lapsed one is pruned when its town is read.
using System;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        public int RewardsPosted { get; private set; }

        /// A resident struck the last blow in a brawl that killed a man: the sheriff posts a reward for him for a week.
        public void PostReward(string id)
        {
            if (Cfg == null || !Cfg.NeighbourReward || id == null || !Resident.IsResident(id))
                return;
            Resident r = Store.FindResident(id);
            if (r == null || r.Dead || r.Left || r.Fled || r.Held != null || r.PostId != null || r.Town == null)   // (PostId: a keeper's counter would keep them; the review)
                return;
            int today = (int)(Store.LastGameMin / 1440), day;
            if (Store.Counts.TryGetValue(NeighbourReward.DayKey(id), out day) && NeighbourReward.Live(day, today))
                return;   // one posted already this week
            int cents = NeighbourReward.Cents(id + "|" + today);
            Store.Counts[NeighbourReward.DayKey(id)] = Math.Max(1, today);
            Store.Counts[NeighbourReward.CentsKey(id)] = cents;
            RewardsPosted++;
            Log("REWARD posted at " + r.Town + ": $" + cents / 100 + " for " + r.Name + ", for a death in a brawl (a week)");
        }

        /// The resident of this town with a reward on him this week, or null. A lapsed one met on the way is pruned.
        public Resident RewardIn(string town)
        {
            if (Cfg == null || !Cfg.NeighbourReward || town == null || Store.Counts.Count == 0)
                return null;
            long today = Store.LastGameMin / 1440;
            foreach (Resident r in Store.Residents)
            {
                int day;
                if (r == null || r.Town != town || !Store.Counts.TryGetValue(NeighbourReward.DayKey(r.Id), out day))
                    continue;
                if (!r.Dead && !r.Left && !r.Fled && NeighbourReward.Live(day, today))
                    return r;
                Store.Counts.Remove(NeighbourReward.DayKey(r.Id));   // (the loop is over Residents, not Counts: safe to remove)
                Store.Counts.Remove(NeighbourReward.CentsKey(r.Id));
                Log("REWARD for " + r.Name + " lapsed");
                return null;   // one pruned a look; the next look goes on
            }
            return null;
        }

        public int RewardCents(string id)
        {
            int c;
            return id != null && Store.Counts.TryGetValue(NeighbourReward.CentsKey(id), out c) ? c : 0;
        }

        /// Turned in (taken to the county jail) or warned (ran): either way gone from the town for good, the reward over.
        public void RewardEnded(Resident r, long now, bool taken)
        {
            if (r == null)
                return;
            Store.Counts.Remove(NeighbourReward.DayKey(r.Id));
            Store.Counts.Remove(NeighbourReward.CentsKey(r.Id));
            GoneFromTown(r, now, taken ? "turned in by you on the reward: to the county jail" : "warned by you of the reward: ran in the night");
        }

        /// Gone from their town for good, not by the train (taken by the law, or run off): Left and Fled, so no cast finds
        /// them again; the log says why (FIVEDAY R30, R31).
        public void GoneFromTown(Resident r, long now, string why)
        {
            if (r == null)
                return;
            if (!r.Left)
            {
                r.Left = true;
                r.LeftAt = now;
            }
            if (!r.Fled)
            {
                r.Fled = true;
                r.FledAt = now;
            }
            Store.Counts["gone:" + r.Id] = 1;   // never back by the train (Engine.ReturnerDue; the review)
            Log("GONE " + r.Name + " from " + r.Town + " for good: " + why);
        }
    }
}
