// Fever in town, the engine's part (FIVEDAY reserve R9; Core/Fever.cs). The daily tick: an outbreak may start in a town (an
// index case among its residents); the sick pass it along their warm ties to residents of the same town; the sick whose days
// are up get better ("had:") or, with FeverDeaths, one in twelve dies (NoteDied: mourned, waked, buried like anyone); three
// sick at once posts the quarantine; none sick ends it with the word of what it cost. All in Store.Counts: nothing new saved.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _feverSick, _feverDead;

        private void FeverDaily(long now)
        {
            if (Cfg == null || !Cfg.Fever)
                return;
            long day = now / 1440;
            // the sick: better, or worse
            List<string> sick = Store.Counts.Keys.Where(k => k.StartsWith(Fever.SickPrefix, System.StringComparison.Ordinal)).ToList();
            foreach (string key in sick)
            {
                string id = key.Substring(Fever.SickPrefix.Length);
                int fell = Store.Counts[key];
                if (day - fell < Fever.SickDays(id))
                    continue;
                Store.Counts.Remove(key);
                Resident r = Store.FindResident(id);
                if (r != null && !r.Dead && Cfg.FeverDeaths && Fever.Dies(id, fell))
                {
                    NoteDied(new DeathEntry { Id = r.Id, Name = r.Name, Kind = "resident", At = now, Town = r.Town, How = "the fever", By = "unknown" });
                    Store.Counts["feverdead:" + r.Town] = Store.Count("feverdead:" + r.Town) + 1;
                    _feverDead++;
                    Log("FEVER " + r.Name + " died of it at " + r.Town);
                }
                else
                {
                    Store.Counts[Fever.HadPrefix + id] = (int)day;
                    if (r != null)
                        Log("FEVER " + r.Name + " is up again at " + r.Town);
                }
            }
            // the spread, along warm ties, to residents of the same town
            var nowSick = Store.Counts.Keys.Where(k => k.StartsWith(Fever.SickPrefix, System.StringComparison.Ordinal)).Select(k => k.Substring(Fever.SickPrefix.Length)).ToList();
            foreach (string s in nowSick)
            {
                Resident from = Store.FindResident(s);
                if (from == null || from.Dead || from.Left || from.Fled)
                    continue;   // (a dead or gone carrier spreads nothing: the review)
                foreach (SceneTie t in Store.Ties)
                {
                    if (t.Warmth <= 0 || !t.Has(s))
                        continue;
                    string other = t.OtherId(s);
                    Resident to = Store.FindResident(other);
                    if (to == null || to.Dead || to.Left || to.Town != from.Town || Store.Counts.ContainsKey(Fever.SickPrefix + other) || Store.Counts.ContainsKey(Fever.HadPrefix + other))
                        continue;
                    if (Fever.Catches(s, other, day))
                        FallSick(to, day, "from " + from.Name);
                }
            }
            // the towns: a new outbreak, the quarantine, the end
            foreach (string town in Fever.Towns)
            {
                bool running = Store.Count(Fever.TownPrefix + town) > 0;
                int count = Store.Residents.Count(r => r.Town == town && Store.Counts.ContainsKey(Fever.SickPrefix + r.Id));
                if (running && count == 0)
                {
                    int dead = Store.Count("feverdead:" + town);
                    Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 40, Text = Fever.Over(town, Fever.Name(town, Store.Count(Fever.TownPrefix + town)), dead) });
                    Store.Counts.Remove(Fever.TownPrefix + town);
                    Store.Counts.Remove(Fever.PostedPrefix + town);
                    Store.Counts.Remove("feverdead:" + town);
                    Store.Counts["feverlast:" + town] = (int)day;
                    Log("FEVER " + town + ": over (" + dead + " dead)");
                    continue;
                }
                if (running && count >= Fever.QuarantineAt && Store.Count(Fever.PostedPrefix + town) == 0)
                {
                    Store.Counts[Fever.PostedPrefix + town] = (int)day;
                    Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 60, Text = Fever.Quarantine(town, Fever.Name(town, Store.Count(Fever.TownPrefix + town)), count) });
                    Log("FEVER " + town + ": quarantine posted (" + count + " sick)");
                }
                if (!running && Fever.Starts(town, day, Store.Count("feverlast:" + town) == 0 ? -999 : Store.Count("feverlast:" + town)))
                    FeverStart(town, day, null);
            }
            // forget who had it a year ago
            foreach (string k in Store.Counts.Keys.Where(k => k.StartsWith(Fever.HadPrefix, System.StringComparison.Ordinal) && day - Store.Counts[k] > 365).ToList())
                Store.Counts.Remove(k);
        }

        /// The claim agent and the widow (FIVEDAY R24; Core/ClaimAgent.cs): from the daily tick, the deaths three days old that
        /// bring an agent (the mine's, a killing in Saint Denis): the visit in the box, and the next day the widow's end. Once a
        /// death (Store.Counts "claim:<id>": the day of the visit). Returns how many visits.
        public int ClaimAgentsDaily(long now)
        {
            if (Cfg == null || !Cfg.ClaimAgents)
                return 0;
            long day = now / 1440;
            int visits = 0;
            foreach (DeathEntry d in Store.Deaths)
            {
                string kind = ClaimAgent.Kind(d);
                if (kind == null || d.Id == null || Store.Count(ClaimAgent.Prefix + "told:" + d.Id) > 0)
                    continue;
                Resident dr = Store.FindResident(d.Id);
                if (dr != null && dr.Woman)
                    continue;   // the lines are a widow's (the review)
                int visited = Store.Count(ClaimAgent.Prefix + d.Id);
                long since = day - d.At / 1440;
                if (visited == 0 && since >= ClaimAgent.AfterDays && since < ClaimAgent.AfterDays + 7)
                {
                    Store.Counts[ClaimAgent.Prefix + d.Id] = (int)day;
                    Push(new Toast { Kind = "life", Title = ShortTown(d.Town), Weight = 30, Text = ClaimAgent.Visit(kind, d.Name, d.Town) });
                    Log("CLAIM " + kind + " agent to " + d.Name + "'s widow at " + d.Town);
                    visits++;
                }
                else if (visited > 0 && day - visited >= 1)
                {
                    Store.Counts.Remove(ClaimAgent.Prefix + d.Id);
                    Store.Counts[ClaimAgent.Prefix + "told:" + d.Id] = 1;   // a day or more on, once: a skipped midnight never loses it (the review)
                    string choice = ClaimAgent.Widow(d.Id);
                    Push(new Toast { Kind = "life", Title = ShortTown(d.Town), Weight = 30, Text = ClaimAgent.End(choice, d.Name) });
                    Log("CLAIM " + d.Name + "'s widow: " + choice);
                }
            }
            return visits;
        }

        private void FallSick(Resident r, long day, string how)
        {
            Store.Counts[Fever.SickPrefix + r.Id] = (int)day;
            _feverSick++;
            Log("FEVER " + r.Name + " falls sick at " + r.Town + " (" + how + ")");
        }

        /// An outbreak in this town: the index case (the given resident, or a townsperson picked by the day).
        private string FeverStart(string town, long day, string indexId)
        {
            Resident first = indexId != null ? Store.FindResident(indexId) :
                Store.Residents.Where(r => r.Town == town && !r.Dead && !r.Left && !r.Fled && r.Held == null && !Store.Counts.ContainsKey(Fever.HadPrefix + r.Id)).OrderBy(r => Voice.Hash(r.Id + "|fever|" + day)).FirstOrDefault();
            if (first == null)
                return null;
            Store.Counts[Fever.TownPrefix + town] = (int)System.Math.Max(1, day);
            FallSick(first, day, "the first");
            Log("FEVER " + town + ": " + Fever.Name(town, day) + ", " + first.Name + " the first");
            return first.Name;
        }

        public string FeverTest(string town, string indexId, long now)
        {
            if (Cfg == null || !Cfg.Fever)
                return "off ([World] Fever)";
            if (town == null || indexId == null || Store.FindResident(indexId) == null)
                return "no resident near to fall sick first";
            if (!Fever.Towns.Contains(town))
                return "no: the fever comes only to " + string.Join(", ", Fever.Towns) + ", not " + town;   // (it would never end there: the review)
            if (Store.Count(Fever.TownPrefix + town) > 0)
                return "no: a fever is going through " + town + " already";
            string who = FeverStart(town, now / 1440, indexId);
            return who + " has the fever in " + town + "; it goes along the ties each day (the daily tick)";
        }

        /// Whether this person is sick now (Choosing's lean, their thoughts).
        public bool SickNow(string id) { return id != null && Cfg != null && Cfg.Fever && Store.Counts.ContainsKey(Fever.SickPrefix + id); }

        /// Whether a fever is going through this town now.
        public bool FeverIn(string town) { return town != null && Cfg != null && Cfg.Fever && Store.Count(Fever.TownPrefix + town) > 0; }

        public string FeverStatus()
        {
            if (Cfg == null || !Cfg.Fever)
                return "off ([World] Fever)";
            int sick = Store.Counts.Keys.Count(k => k.StartsWith(Fever.SickPrefix, System.StringComparison.Ordinal));
            return _feverSick + " fell sick this session, " + _feverDead + " died; " + sick + " sick now; towns: " + string.Join(", ", Fever.Towns.Where(t => Store.Count(Fever.TownPrefix + t) > 0));
        }
    }
}
