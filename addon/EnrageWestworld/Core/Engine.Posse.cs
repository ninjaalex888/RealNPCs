// The posse rides, the engine's part (FIVEDAY reserve R32; Core/PosseRides.cs). From DailyTick: a posse out eighteen hours
// or more comes back (nothing, a stranger, or one of them dead: NoteDied, mourned like anyone); then a town with a bad
// crime of the last day and no posse out gets one up, once a deed. Store.Counts only: "posse:<town>" the minute it rode,
// "possekind:<town>" 1 for a killing, "posseman:<id>" each man, "possedeed:<id>" the deed (kept, so it's once).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        public int PossesOut { get { return Store.Counts.Keys.Count(k => k.StartsWith(PosseRides.Prefix, StringComparison.Ordinal)); } }
        public int PossesRode { get; private set; }

        /// Whether a posse from this town is out now.
        public bool PosseOut(string town) { return town != null && Store.Counts.ContainsKey(PosseRides.TownKey(town)); }

        private void PosseDaily(long now)
        {
            if (Cfg == null)
                return;
            if (!Cfg.PosseRides)
            {
                // switched off: any posse out is dropped quietly, so nothing waits in the save for the switch (the review)
                foreach (string k in Store.Counts.Keys.Where(k => k.StartsWith(PosseRides.Prefix, StringComparison.Ordinal) || k.StartsWith(PosseRides.ManPrefix, StringComparison.Ordinal) || k.StartsWith("possekind:", StringComparison.Ordinal) || k.StartsWith("committee:", StringComparison.Ordinal)).ToList())
                    Store.Counts.Remove(k);
                return;
            }
            foreach (string key in Store.Counts.Keys.Where(k => k.StartsWith(PosseRides.Prefix, StringComparison.Ordinal)).ToList())
            {
                int rode = Store.Counts[key];
                if (now - rode >= PosseRides.OutMinutes)
                    PosseBack(key.Substring(PosseRides.Prefix.Length), rode, now);
            }
            CommitteeDaily(now);
            foreach (string town in Store.Deeds.Where(d => d.Town != null && now >= d.At && now - d.At < 1440).Select(d => d.Town).Distinct().ToList())
            {
                if (PosseOut(town))
                    continue;
                Deed d = RecentCrime(town, now);
                if (d == null || d.Id == null || Store.Counts.ContainsKey(PosseRides.DeedKey(d.Id)))
                    continue;
                Store.Counts[PosseRides.DeedKey(d.Id)] = 1;
                PosseForm(town, d.Id, d.Kind, now);
            }
            if (!Cfg.PosseFromTheRoad)
                return;
            // a killing out of town, near enough for word to get there (TENHOUR item 3): the worst first, once a deed, one posse a town at a time
            foreach (Deed d in Store.Deeds.Where(x => x.Town == null && now >= x.At && now - x.At < 1440 && x.Id != null).OrderByDescending(x => x.Weight).ToList())
            {
                string town = PosseRides.RoadTown(d, now);
                if (town == null || PosseOut(town) || Store.Counts.ContainsKey(PosseRides.DeedKey(d.Id)))
                    continue;
                Store.Counts[PosseRides.DeedKey(d.Id)] = 1;
                PosseForm(town, d.Id, d.Kind, now, true);
            }
        }

        /// Gets a posse up in this town after a deed (its id seeds who rides): the men, the box, the log. Returns how many
        /// ride, or 0 with too few men (or one out already). The Testing row "posse" calls it with a made-up deed.
        public int PosseForm(string town, string deedId, string kind, long now, bool road = false)
        {
            if (PosseOut(town))
                return 0;
            List<Resident> men = PosseRides.Pick(Store.Residents, town, deedId);
            if (men.Count < PosseRides.Min)
            {
                Log("POSSE at " + town + ": not enough men to ride after " + kind + " (" + men.Count + ")");
                return 0;
            }
            Store.Counts[PosseRides.TownKey(town)] = (int)now;
            Store.Counts["possekind:" + town] = Deeds.IsViolent(kind) ? 1 : 0;
            foreach (Resident m in men)
                Store.Counts[PosseRides.ManKey(m.Id)] = (int)now;
            PossesRode++;
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 50, Text = road ? PosseRides.RoadFormedLine(town, men.Select(m => Person.FirstOf(m.Name)).ToList(), deedId) : PosseRides.FormedLine(town, PosseRides.What(kind), men.Select(m => Person.FirstOf(m.Name)).ToList(), deedId) });
            Log("POSSE rides out of " + town + " after " + kind + (road ? " on the road" : "") + " (" + deedId + "): " + string.Join(", ", men.Select(m => m.Name)));
            return men.Count;
        }

        /// The Testing row's "back now": the posse out of this town comes back at once (its outcome rolled as ever).
        public bool PosseBackNow(string town, long now)
        {
            int rode;
            if (town == null || !Store.Counts.TryGetValue(PosseRides.TownKey(town), out rode))
                return false;
            PosseBack(town, rode, now);
            return true;
        }

        private void PosseBack(string town, int rode, long now)
        {
            string seed = town + "|" + rode, what = Store.Count("possekind:" + town) == 1 ? "the killing" : "the robbery";
            // this town's riders only: two towns' posses can ride out in the same tick, with the same minute (the review)
            List<string> manKeys = Store.Counts.Keys.Where(k => k.StartsWith(PosseRides.ManPrefix, StringComparison.Ordinal) && Store.Counts[k] == rode)
                                       .Where(k => { Resident x = Store.FindResident(k.Substring(PosseRides.ManPrefix.Length)); return x == null || x.Town == town; }).ToList();
            List<Resident> men = manKeys.Select(k => Store.FindResident(k.Substring(PosseRides.ManPrefix.Length))).Where(r => r != null && r.Town == town).ToList();
            string outcome = Riders.PosseOutcome(PosseRides.Outcome(seed), Store.Count("possepoint:" + town)), deadName = null;   // your word to the deputy on the road (FOURTEENHOUR reserve R9)
            Store.Counts.Remove("possepoint:" + town);
            if (outcome == "dead")
            {
                Resident v = men.Where(r => !r.Dead && !r.Left && !r.Fled && r.Cast == null)   // never one walking in the world now (the review)
                             .OrderBy(r => Voice.Hash(seed + "|" + r.Id)).FirstOrDefault();
                if (v == null)
                    outcome = "lost";
                else
                {
                    NoteDied(new DeathEntry { Id = v.Id, Name = v.Name, Kind = "resident", At = now, Town = town, How = "shot riding with the posse", By = "unknown" });
                    deadName = v.Name;
                }
            }
            Store.Counts.Remove(PosseRides.TownKey(town));
            Store.Counts.Remove("possekind:" + town);
            foreach (string k in manKeys)
                Store.Counts.Remove(k);
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = outcome == "dead" ? 60 : 45, Text = PosseRides.BackLine(outcome, town, what, deadName, seed) });
            Log("POSSE back at " + town + ": " + outcome + (deadName != null ? " (" + deadName + " killed)" : ""));
            if (Cfg.VigilanceCommittees && outcome == "lost" && what == "the killing" && PosseRides.Committee(seed))
            {
                Store.Counts[PosseRides.CommitteeKey(town)] = (int)now + PosseRides.CommitteeDelay(seed);
                Log("VIGILANCE at " + town + ": the posse came home with nothing, and some of the men mean to settle it themselves");
            }
        }

        // ---- a vigilance committee (TENHOUR item 4; Core/PosseRides.cs): staged as an execution if you're near when it's due, settled off screen if you never come
        private void CommitteeDaily(long now)
        {
            foreach (string key in Store.Counts.Keys.Where(k => k.StartsWith("committee:", StringComparison.Ordinal)).ToList())
            {
                int due = Store.Counts[key];
                if (!Cfg.VigilanceCommittees)
                {
                    Store.Counts.Remove(key);   // switched off: nothing waits in the save for the switch
                    continue;
                }
                if (now - due < PosseRides.CommitteeKeepDays * 1440)
                    continue;
                Store.Counts.Remove(key);
                string town = key.Substring("committee:".Length), seed = town + "|" + due;
                Resident v = PosseRides.CommitteeHangs(seed) ? CommitteeVictim(town, seed, null) : null;
                if (v != null)
                    NoteDied(new DeathEntry { Id = v.Id, Name = v.Name, Kind = "resident", At = now, Town = town, How = "hanged by his neighbours", By = "unknown" });
                Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = v != null ? 60 : 40, Text = PosseRides.CommitteeLine(v != null, town, v != null ? v.Name : null, seed) });
                Log("VIGILANCE at " + town + ": " + (v != null ? v.Name + " hanged, off screen" : "they went home again"));
            }
        }

        /// Whom they would turn on: a man of that town, never their leader, never a keeper, and never someone bound to a body at present.
        private Resident CommitteeVictim(string town, string seed, string notId)
        {
            return Store.Residents.Where(r => r.Town == town && !r.Woman && !r.Dead && !r.Left && !r.Fled && r.Held == null && r.PostId == null && r.Cast == null && r.Name != null && r.Id != notId)
                .OrderBy(r => Voice.Hash(seed + "|cv|" + r.Id)).FirstOrDefault();
        }

        /// Whether men of this town mean to act about now, for the road's roll.
        public bool CommitteeDue(string town, long now)
        {
            int due;
            return Cfg.VigilanceCommittees && Cfg.PosseRides && town != null && Store.Counts.TryGetValue(PosseRides.CommitteeKey(town), out due) && now >= due;
        }

        /// The plan for one held by his own neighbours, using up what's due; null if nothing is (`force`: the Testing row). The man who leads it comes from those who'd have ridden with the posse.
        public Executions.Plan CommitteePlan(Resident r, string town, long now, string seed, bool force)
        {
            if (r == null || (!force && !CommitteeDue(town, now)))
                return null;
            Store.Counts.Remove(PosseRides.CommitteeKey(town));
            Resident lead = PosseRides.Pick(Store.Residents, town, seed).FirstOrDefault(m => m.Id != r.Id);
            return new Executions.Plan { Reason = "committee", Boss = lead != null ? Residents.SurnameOf(lead.Name) : Residents.Surname(seed + "b"), BossId = lead != null ? lead.Id : null, Captors = "boss" };
        }
    }
}
