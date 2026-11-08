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
            if (Cfg == null || !Cfg.PosseRides)
                return;
            foreach (string key in Store.Counts.Keys.Where(k => k.StartsWith(PosseRides.Prefix, StringComparison.Ordinal)).ToList())
            {
                int rode = Store.Counts[key];
                if (now - rode >= PosseRides.OutMinutes)
                    PosseBack(key.Substring(PosseRides.Prefix.Length), rode, now);
            }
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
        }

        /// Gets a posse up in this town after a deed (its id seeds who rides): the men, the box, the log. Returns how many
        /// ride, or 0 with too few men (or one out already). The Testing row "posse" calls it with a made-up deed.
        public int PosseForm(string town, string deedId, string kind, long now)
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
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 50, Text = PosseRides.FormedLine(town, PosseRides.What(kind), men.Select(m => Person.FirstOf(m.Name)).ToList(), deedId) });
            Log("POSSE rides out of " + town + " after " + kind + " (" + deedId + "): " + string.Join(", ", men.Select(m => m.Name)));
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
            List<string> manKeys = Store.Counts.Keys.Where(k => k.StartsWith(PosseRides.ManPrefix, StringComparison.Ordinal) && Store.Counts[k] == rode).ToList();
            List<Resident> men = manKeys.Select(k => Store.FindResident(k.Substring(PosseRides.ManPrefix.Length))).Where(r => r != null && r.Town == town).ToList();
            string outcome = PosseRides.Outcome(seed), deadName = null;
            if (outcome == "dead")
            {
                Resident v = men.Where(r => !r.Dead && !r.Left && !r.Fled).OrderBy(r => Voice.Hash(seed + "|" + r.Id)).FirstOrDefault();
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
        }
    }
}
