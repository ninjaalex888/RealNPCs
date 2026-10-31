// The Awakening, run once a game day: a share of the living is quietly chosen to wake (leaning toward the
// most aware), and each woken person moves through the stages as days pass. See Core/Awakening.cs.
// It covers enRAGE's people and the residents (everyone else, see Residents.cs): a resident rolls the
// share once, when they are first met.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private readonly Dictionary<string, Person> _resPeople = new Dictionary<string, Person>();

        /// How many of the living have begun to wake (any stage), enRAGE's people and residents together.
        public int WakingCount()
        {
            return Store.People.Values.Count(s => s.WakeStage > 0 && !Gone(s.Id));
        }

        /// Dead, or a story character: not part of the Awakening any more.
        private bool Gone(string id)
        {
            Person p;
            if (_byId.TryGetValue(id, out p))
                return !p.IsAlive || p.Lore;
            Resident r = Store.FindResident(id);
            return r != null && r.Dead;
        }

        /// The Person for anyone the add-on keeps a state for: an enRAGE person, a resident, or an enRAGE townsperson.
        private Person PersonForState(PersonState st)
        {
            Person p;
            if (_byId.TryGetValue(st.Id, out p))
                return p;
            Resident r = Store.FindResident(st.Id);
            return r != null ? PersonOfResident(r) : new Person { Id = st.Id, Name = st.Name ?? "someone" };
        }

        /// enRAGE's named townsfolk are known by the add-on as "folk:" ids. The share who start already stirring is
        /// rolled once, the first time they are looked at.
        public Person PersonOfFolk(string key, string name, bool woman, string town, long now)
        {
            Person p = new Person { Id = key, Name = name, Woman = woman, Home = town };
            PersonState st = Store.Get(p);
            if (!st.WakeRolled)
            {
                st.WakeRolled = true;
                if (Cfg.AwakeningShare > 0 && _rng.NextDouble() * 100.0 < Cfg.AwakeningShare)
                    Advance(p, st, now);
            }
            return p;
        }

        /// enRAGE took up one of our residents as its own (a greeting does it): their waking goes with them to the state we
        /// keep for enRAGE's person, so a stage isn't lost (Lex, 2026-10-31: awake, greeted, and the stages were gone).
        /// The later stage wins; the despair roll and the strip go with it. True when anything moved.
        public bool CarryWake(string fromId, Person to)
        {
            PersonState from = Store.Find(fromId);
            if (from == null || from.WakeStage <= 0 || to == null || to.Id == fromId)
                return false;
            PersonState st = Store.Get(to);
            st.WakeRolled = true;
            if (st.WakeStage >= from.WakeStage)
                return false;
            st.WakeStage = from.WakeStage;
            st.WakeStageAt = from.WakeStageAt;
            foreach (string mark in new[] { "despair:rolled2", "wake:strip" })
                if (from.Has(mark))
                    st.Mark(mark);
            return true;
        }

        /// enRAGE took up one of our residents: what they knew of him goes with them too (Lex's test, 2026-10-31: Hugh
        /// Lambert was told the name, was greeted into enRAGE's townsfolk seconds later, and the next Talk offered the name
        /// again; his talks and memories were left behind on the resident's id). Once per pair (a mark on the new state),
        /// whatever the waking. The worry goes too (NIGHT3 For Lex 9a): enRAGE's person lives on with the resident's worry
        /// (Store.WorrySeedOf: the same kind, stage and people, worked out from the resident's id and facts, the resident
        /// kept for it), and a worry Arthur mended (Store.WorrySettled) follows to the new id. Returns what moved, for the
        /// log, or null when nothing did.
        public string CarryPerson(string fromId, Person to, Resident fromRes = null)
        {
            if (fromId == null || to == null || to.Id == null || to.Id == fromId)
                return null;
            bool worry = KeepWorry(fromId, to.Id, fromRes);   // before the mark: a pair carried by an earlier build gets it too
            PersonState from = Store.Find(fromId);
            if (from == null)
                return worry ? "their worry" : null;
            PersonState st = Store.Get(to);
            string mark = "carried:" + fromId;
            if (st.Has(mark))
                return worry ? "their worry" : null;
            st.Mark(mark);
            var moved = new List<string>();
            if (worry)
                moved.Add("their worry");
            if (from.KnowsName)
            {
                if (!st.KnowsName)
                    moved.Add("your name");
                st.KnowsName = true;
                if (from.NameAt > 0 && (st.NameAt == 0 || from.NameAt < st.NameAt))
                    st.NameAt = from.NameAt;
            }
            if (from.Talks > 0)
                moved.Add(from.Talks + (from.Talks == 1 ? " talk" : " talks"));
            st.Talks += from.Talks;
            st.Sightings += from.Sightings;
            st.LastTalkAt = Math.Max(st.LastTalkAt, from.LastTalkAt);
            st.LastSeenAt = Math.Max(st.LastSeenAt, from.LastSeenAt);
            st.LastRemarkAt = Math.Max(st.LastRemarkAt, from.LastRemarkAt);
            st.ContactAt = Math.Max(st.ContactAt, from.ContactAt);
            st.FlirtedAt = Math.Max(st.FlirtedAt, from.FlirtedAt);
            st.Flirts += from.Flirts;
            if (from.FirstSeenAt > 0 && (st.FirstSeenAt == 0 || from.FirstSeenAt < st.FirstSeenAt))
            {
                st.FirstSeenAt = from.FirstSeenAt;
                st.FirstSeenTown = from.FirstSeenTown ?? st.FirstSeenTown;
            }
            if (from.Affection != 0 || from.Heart != 0)
                moved.Add("how they feel about you");
            st.Affection += from.Affection;
            if (Math.Abs(from.Heart) > Math.Abs(st.Heart))
            {
                st.Heart = from.Heart;
                st.HeartAt = from.HeartAt;
            }
            foreach (string t in from.Told)
                if (!t.StartsWith("carried:"))
                    st.Mark(t);
            foreach (string d in from.KnownDeeds)
                if (!st.KnownDeeds.Contains(d))
                    st.KnownDeeds.Add(d);
            foreach (string d in from.SawDeeds)
                if (!st.SawDeeds.Contains(d))
                    st.SawDeeds.Add(d);
            int memories = 0;
            foreach (MindMemory m in from.Mind)
                if (Minds.Note(st.Mind, m, Store.LastGameMin) == m)
                    memories++;   // new to them (the same thing noted twice is one; over the cap the weakest goes)
            if (memories > 0)
                moved.Add(memories + (memories == 1 ? " memory" : " memories"));
            return moved.Count > 0 ? string.Join(", ", moved) : null;
        }

        /// enRAGE's person `toId` lives on with the worry of the resident `fromId` (InnerLife works it out from the
        /// resident's id and facts), the resident kept so their facts last, and a worry Arthur mended for the resident
        /// counts for the new id too. True when this is new.
        public bool KeepWorry(string fromId, string toId, Resident fromRes = null)
        {
            if (fromId == null || toId == null || fromId == toId)
                return false;
            string root = WorryIdOf(fromId);
            if (root == toId)
                return false;   // never a loop
            if (Store.WorrySeedOf == null)
                Store.WorrySeedOf = new Dictionary<string, string>();
            string was;
            bool fresh = !Store.WorrySeedOf.TryGetValue(toId, out was);   // the first take-up decides: their worry never changes again
            if (!fresh)
                return false;
            if (Store.WorrySettled != null && Store.WorrySettled.ContainsKey(toId))
                return false;   // Arthur mended this person's own worry already (a pair carried by an earlier build): theirs stays, so the mend and its thanks still name it (the pre-release review)
            Store.WorrySeedOf[toId] = root;
            _worrySelf.Remove(toId);
            if (fromRes != null && fromRes.Id == fromId && Store.FindResident(fromId) == null)
                Store.KeepResident(fromRes, Store.LastGameMin);   // their trade, age and kin, which the worry is worked out from
            long at;
            if (Store.WorrySettled != null && Store.WorrySettled.TryGetValue(fromId, out at))
            {
                long mine;
                if (!Store.WorrySettled.TryGetValue(toId, out mine) || mine < at)
                    Store.WorrySettled[toId] = at;   // the mend follows to the new id (Thanks and the menu read it by id)
            }
            Log("WORRY kept: " + toId + " lives on with the worry of " + fromId + (root != fromId ? " (from " + root + ")" : "") + (Store.FindResident(root) != null ? " (" + InnerLife.WorryStatus(PersonOfResident(Store.FindResident(root)), Store.LastGameMin / 1440) + ")" : ""));
            return true;
        }

        public int StageOf(Person p)
        {
            return StageOf(p.Id);
        }

        public int StageOf(string id)
        {
            PersonState st = Store.Find(id);
            return st != null ? st.WakeStage : 0;
        }

        // ---- residents ------------------------------------------------------------------------------

        /// The plain Person for a resident (kept so the same object is used every time).
        public Person PersonOfResident(Resident r)
        {
            Person p;
            if (!_resPeople.TryGetValue(r.Id, out p) || p.Name != r.Name)
                _resPeople[r.Id] = p = Residents.ToPerson(r);
            p.IsAlive = !r.Dead;
            return p;
        }

        /// Drops the cached Person for a resident whose facts changed under the same name (the test cast's Fit sets the
        /// age, faith, temper and trade after MeetResident may already have cached them: the night review, 2026-10-28).
        public void ForgetPerson(string id)
        {
            if (id != null)
                _resPeople.Remove(id);
        }

        /// Makes a resident for an ordinary person who has just been asked about. Not saved until something happens
        /// to them, except that the share of residents who start already waking is rolled here, and those are kept.
        public Resident MeetResident(uint model, bool woman, string town, string trade, string seed, long now, bool roll = true)
        {
            Resident r = Residents.Make(Resident.Prefix + (++Store.NextResident), seed, model, woman, town, trade, now, Voice.NowYear);
            if (roll)
                RollAwakening(r, now);
            return r;
        }

        /// The Awakening's share: this resident may be one of those already stirring when met. Once each (MeetResident, or when
        /// one named only for the cards is first met by the mind).
        public void RollAwakening(Resident r, long now)
        {
            if (Cfg.AwakeningShare > 0 && _rng.NextDouble() * 100.0 < Cfg.AwakeningShare)
            {
                Advance(PersonOfResident(r), Store.Get(PersonOfResident(r)), now);
                Store.KeepResident(r, now);
            }
        }

        /// Something happened to this resident (they were talked to, woke a stage...): they are saved from now on.
        public void KeepResident(Resident r, long now)
        {
            Store.KeepResident(r, now);
        }

        // ---- the daily tick -----------------------------------------------------------------------------

        private void AwakeningTick(long now)
        {
            if (!Cfg.WestWorld)
                return;
            long perStage = Math.Max(1, Cfg.AwakeningDays) * 1440L;
            List<Person> living = _byId.Values.Where(p => p.IsAlive && !p.Lore).ToList();
            foreach (Person p in living)
                AdvanceIfDue(p, now, perStage);
            // everyone who has begun to wake, whoever they are (enRAGE people already advanced above)
            foreach (PersonState st in Store.People.Values.Where(s => s.WakeStage >= Awakening.Stirring && s.WakeStage < Awakening.Awake && !Gone(s.Id) && !_byId.ContainsKey(s.Id)).ToList())
                AdvanceIfDue(PersonForState(st), now, perStage);
            if (living.Count == 0 || Cfg.AwakeningShare <= 0)
                return;   // no one new wakes; those already waking carry on
            int target = (int)Math.Ceiling(living.Count * Cfg.AwakeningShare / 100.0);
            int current = living.Count(p => StageOf(p) > 0);
            while (current < target)
            {
                // quietly chosen, leaning toward those who already suspect the most
                Person pick = living.Where(p => StageOf(p) == 0)
                    .OrderByDescending(p => Hosts.Awareness(p, Store.Find(p.Id)) + _rng.Next(25) + WakeLean(p.Id)).FirstOrDefault();   // with MindWakes, what shook them leans it (Engine.Doubt.cs; 0 otherwise)
                if (pick == null)
                    break;
                Advance(pick, Store.Get(pick), now);
                current++;
            }
        }

        private void AdvanceIfDue(Person p, long now, long perStage)
        {
            PersonState st = Store.Find(p.Id);
            if (st != null && st.WakeStage >= Awakening.Stirring && st.WakeStage < Awakening.Awake && now - st.WakeStageAt >= perStage && _rng.Next(100) < WakeChance(p.Id))   // 60 as before; with MindWakes, from 30 to 95 by what shook or steadied them (Engine.Doubt.cs)
                Advance(p, st, now);
        }

        /// Wakes someone a stage further (the first stage if still asleep). Returns the new stage.
        public int Advance(Person p, PersonState st, long now)
        {
            if (st.WakeStage >= Awakening.Awake)
                return st.WakeStage;
            st.WakeStage++;
            st.WakeStageAt = now;
            Log("AWAKENING " + p.Name + (Resident.IsResident(p.Id) ? " (a resident" + (string.IsNullOrEmpty(p.Home) ? "" : " of " + p.Home) + ")" : "") + ": stage " + st.WakeStage + " (" + Awakening.StageName(st.WakeStage) + ")");
            return st.WakeStage;
        }

        /// For testing: wakes the person a stage further right now.
        public int Awaken(Person p, long now)
        {
            return Advance(p, Store.Get(p), now);
        }

        /// The first name of the living one they're closest to: the warmest tie (warmth 2 or more, or a courtship) of the last 60 game
        /// days, the newest of equals; null when there's nobody (despair's last thoughts, DespairWords).
        public string Dearest(string id)
        {
            if (id == null || Store.Ties == null)
                return null;
            long now = Store.LastGameMin;
            SceneTie t = Store.Ties.Where(x => x.Has(id) && (x.Warmth >= 2 || Ties.Sweethearts(x)) && now - x.At >= 0 && now - x.At < 60 * 1440L && !HasDied(x.A == id ? x.B : x.A))
                                   .OrderByDescending(x => x.Warmth).ThenByDescending(x => x.At).FirstOrDefault();
            if (t == null)
                return null;
            string name = Person.FirstOf(t.A == id ? t.BName : t.AName);
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
