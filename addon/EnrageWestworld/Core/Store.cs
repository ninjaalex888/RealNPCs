// The add-on's save file, one per player character (arthur.json / john.json), next to the ini.
// DataContract JSON: fields missing from an older file come back null, so Fix() fills them in.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Store
    {
        [DataMember(EmitDefaultValue = false)] public int Version = 3;   // 3: people keep memories (PersonState.Mind, 0.6.3)
        [DataMember(EmitDefaultValue = false)] public string Player = "arthur";
        [DataMember(EmitDefaultValue = false)] public long InstalledAt;
        [DataMember(EmitDefaultValue = false)] public long LastGameMin;
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, PersonState> People = new Dictionary<string, PersonState>();
        [DataMember(EmitDefaultValue = false)] public List<Rumor> Rumors = new List<Rumor>();
        [DataMember(EmitDefaultValue = false)] public List<string> Announced = new List<string>();
        [DataMember(EmitDefaultValue = false)] public List<Affair> Affairs = new List<Affair>();
        [DataMember(EmitDefaultValue = false)] public long RomanceAt;

        // 0.4
        [DataMember(EmitDefaultValue = false)] public List<Deed> Deeds = new List<Deed>();
        [DataMember(EmitDefaultValue = false)] public List<TownRumour> TownRumours = new List<TownRumour>();
        [DataMember(EmitDefaultValue = false)] public List<Paper> Papers = new List<Paper>();
        [DataMember(EmitDefaultValue = false)] public List<Fallen> Fallen = new List<Fallen>();
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, Reverie> Reveries = new Dictionary<string, Reverie>();
        [DataMember(EmitDefaultValue = false)] public List<Avenger> Avengers = new List<Avenger>();
        [DataMember(EmitDefaultValue = false)] public List<Wedding> Weddings = new List<Wedding>();
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, long> TownLastAbout = new Dictionary<string, long>();   // town -> the game minute he was last about it (TownVisits is when he last came in) (Core/WhileAway.cs; FIVEDAY item 19)
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, int> Mends = new Dictionary<string, int>();   // "id|wrong" -> kindnesses since that wrong of his (Core/Trust.cs; FIVEDAY item 20)
        [DataMember(EmitDefaultValue = false)] public List<Vouched> Vouches = new List<Vouched>();   // the men you stood surety for at the jail door, and how their week ended (Core/Vouch.cs; FOURTEENHOUR reserve R22)
        [DataMember(EmitDefaultValue = false)] public List<StoryRun> Stories = new List<StoryRun>();   // stories over days (Core/Stories.cs; FOURTEENHOUR reserve R10)
        [DataMember(EmitDefaultValue = false)] public List<TownTale> Tales = new List<TownTale>();   // the town's own tales, grown in the telling (Core/Tales.cs; FOURTEENHOUR item 24)
        [DataMember(EmitDefaultValue = false)] public List<TieHeard> TieHeard = new List<TieHeard>();   // word of his deeds passed along a tie (Core/TieGossip.cs; FIVEDAY item 23)
        [DataMember(EmitDefaultValue = false)] public List<Breakdown> Breakdowns = new List<Breakdown>();   // who broke under the week's load, and how (Core/StressLoad.cs; FIVEDAY item 17)
        [DataMember(EmitDefaultValue = false)] public List<Transfer> Transfers = new List<Transfer>();
        [DataMember(EmitDefaultValue = false)] public List<Debt> Debts = new List<Debt>();
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, int> Counts = new Dictionary<string, int>();
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, long> TownVisits = new Dictionary<string, long>();
        [DataMember(EmitDefaultValue = false)] public int LastStalkDay;
        [DataMember(EmitDefaultValue = false)] public int LastDayStalkDay = -1;
        [DataMember(EmitDefaultValue = false)] public long LastLetterAt;
        [DataMember(EmitDefaultValue = false)] public long LastWorldTickAt;
        [DataMember(EmitDefaultValue = false)] public bool DeadSeeded;
        [DataMember(EmitDefaultValue = false)] public bool SparedSeeded;                 // enRAGE's mercy counts read once, quietly
        [DataMember(EmitDefaultValue = false)] public int DiaryDayKey;
        [DataMember(EmitDefaultValue = false)] public List<Resident> Residents = new List<Resident>();   // ordinary people something has happened to (see Residents.cs)
        [DataMember(EmitDefaultValue = false)] public int NextResident;
        [DataMember(EmitDefaultValue = false)] public List<Post> Posts = new List<Post>();   // the counters with one person each, who pass them on (see Posts.cs)
        [DataMember(EmitDefaultValue = false)] public List<SceneTie> Ties = new List<SceneTie>();      // what street scenes left between two people (see Ties.cs)
        [DataMember(EmitDefaultValue = false)] public List<WorryLink> Links = new List<WorryLink>();   // the real person at the other end of a worry (see Links.cs)
        [DataMember(EmitDefaultValue = false)] public List<DeathEntry> Deaths = new List<DeathEntry>();   // our own record of who has died (see Deaths.cs)
        [DataMember(EmitDefaultValue = false)] public List<LaterAct> Later = new List<LaterAct>();   // diary entries and word of mouth waiting their time (see Knowing.cs)
        [DataMember(EmitDefaultValue = false)] public List<Reply> Replies = new List<Reply>();   // answers in the post, and letters of his own (Core/Replies.cs; SIXDAY item 8)
        [DataMember(EmitDefaultValue = false)] public List<TownEvent> Events = new List<TownEvent>();   // (Resident.Held follows these: FixHeld)   // the town's days: dances and the rest (Core/TownEvents.cs; SIXDAY item 10)
        [DataMember(EmitDefaultValue = false)] public List<Ghost> Ghosts = new List<Ghost>();   // what rewinds dropped, kept a week (Core/Ghosts.cs; SIXDAY item 12b); never rewound
        [DataMember(EmitDefaultValue = false)] public List<KinGrudge> KinGrudges = new List<KinGrudge>();   // the kin who come about a wrong of the player's (Core/KinGrudges.cs; SIXDAY item 14)
        [DataMember(EmitDefaultValue = false)] public List<TrystRecord> Trysts = new List<TrystRecord>();   // the women of the player's nights (Core/Trysts.cs; SIXDAY item 15)
        [DataMember(EmitDefaultValue = false)] public bool CarriedOver;   // John's store took Arthur's world over once (Core/Epilogue.cs; SIXDAY item 23)
        [DataMember(EmitDefaultValue = false)] public List<Habit> Habits = new List<Habit>();   // the player's routines the town has noticed (Core/PlaceMemory.cs; SIXDAY item 24)
        [DataMember(EmitDefaultValue = false)] public int Rewinds;                                 // how many times the clock turned back
        [IgnoreDataMember] public List<Ghost> LastRewound;                                        // the last rewind's ghosts, for the engine's log
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, TownLedger> TownLedgers = new Dictionary<string, TownLedger>();   // each town's ledger, read off the records (Core/TownLedgers.cs; SIXDAY item 11c)
        [DataMember(EmitDefaultValue = false)] public List<Happening> WorldLog = new List<Happening>();   // what happened and who saw it (WorldEvents.cs), capped
        [DataMember(EmitDefaultValue = false)] public List<Plan> Plans = new List<Plan>();   // what people mean to do about each other over the next days (Plans.cs)
        [DataMember(EmitDefaultValue = false)] public List<PlaceSpot> Spots = new List<PlaceSpot>();   // where each town's places are, learned from their keepers (Places.cs)
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, long> WorrySettled;   // id -> the game minute the player settled their worry (InnerLife.SettledAt; roadmap 9); null until one is
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, long> WorryWorsened;   // id -> the game minute the player turned their worry the wrong way (Core/Helps.cs; SIXDAY item 9); null until one
        [DataMember(EmitDefaultValue = false)] public Dictionary<string, string> WorrySeedOf;   // enRAGE's person id -> the resident id whose worry they live on with (Engine.CarryPerson; NIGHT3 For Lex 9a); null until one is taken up
        [DataMember(EmitDefaultValue = false)] public PaceState Pace = new PaceState();                           // when things may happen again (Pacing.cs)
        public List<string> RewoundShares = new List<string>();   // deaths we told enRAGE of that a reload undid; the game side takes them back
        private Dictionary<string, DeathEntry> _deathIndex;

        /// The latest death in our own record for this person, or null.
        public DeathEntry DeathOf(string id)
        {
            if (id == null || Deaths == null)
                return null;
            if (_deathIndex == null)
            {
                _deathIndex = new Dictionary<string, DeathEntry>();
                foreach (DeathEntry d in Deaths)
                    if (d != null && d.Id != null)
                        _deathIndex[d.Id] = d;   // a later one wins
            }
            DeathEntry e;
            return _deathIndex.TryGetValue(id, out e) ? e : null;
        }

        /// Adds a death to our record (DeathLedger.Note: the same death twice is one). True if it was new.
        public bool NoteDeath(DeathEntry e)
        {
            if (Deaths == null)
                Deaths = new List<DeathEntry>();
            bool added = DeathLedger.Note(Deaths, e);
            _deathIndex = null;
            return added;
        }

        public long WasAt;

        /// Someone known by another name now (enRAGE's, for one it knew before we named them): every record of ours that keeps
        /// their name takes the new one, so the card, the thoughts and the bar say one name. Returns how many records changed.
        public int Rename(string id, string name)
        {
            if (id == null || string.IsNullOrEmpty(name))
                return 0;
            int n = 0;
            foreach (Resident r in Residents)
                if (r.Id == id && r.Name != name) { r.Name = name; n++; }
            PersonState st;
            if (People.TryGetValue(id, out st) && st.Name != null && st.Name != name) { st.Name = name; n++; }
            foreach (SceneTie t in Ties)
            {
                if (t.A == id && t.AName != name) { t.AName = name; n++; }
                if (t.B == id && t.BName != name) { t.BName = name; n++; }
            }
            foreach (WorryLink l in Links)
            {
                if (l.Id == id && l.Name != name) { l.Name = name; n++; }
                if (l.OtherId == id && l.OtherName != name) { l.OtherName = name; n++; }
            }
            foreach (Plan p in Plans)
            {
                if (p.HolderId == id && p.HolderName != name) { p.HolderName = name; n++; }
                if (p.TargetId == id && p.TargetName != name) { p.TargetName = name; n++; }
            }
            return n;
        }

        public PersonState Get(Person p)
        {
            PersonState st;
            if (!People.TryGetValue(p.Id, out st))
            {
                st = new PersonState { Id = p.Id, Name = p.Name };
                People[p.Id] = st;
            }
            st.Name = p.Name;
            return st;
        }

        public const int DeedsKnownKept = 50;   // a person's deeds of yours known, and seen, kept: the newest (Trim; HARDEN item 2)
        public const int MaxResidents = 700;   // room for the town regulars (RegularsPerTown in each town) and the rest

        public Resident FindResident(string id)
        {
            return Residents.FirstOrDefault(r => r.Id == id);
        }

        /// A kept resident of this town met before in a body that looked exactly like this one (night 2026-10-29, item 5;
        /// [World] OwnLooks, standing alone): the same model, sex and look, living and still in town, never a regular (they
        /// keep their own outfits: Regulars.cs), the test cast or a keeper at a counter; `taken` says who is in another body
        /// now. Null for nobody, or when two kept residents share the look (a look two people wear is nobody's).
        public Resident ResidentByLook(string look, uint model, bool woman, string town, Func<Resident, bool> taken)
        {
            if (string.IsNullOrEmpty(look) || Residents == null || town == null)
                return null;   // never out in the wilds, where nobody's town would hold them to one place
            Resident found = null;
            foreach (Resident r in Residents)
            {
                if (r.Look != look || r.Model != model || r.Woman != woman || r.Town != town || r.Dead || r.Fled || r.Regular || r.Cast != null || r.PostId != null)
                    continue;
                if (found != null)
                    return null;
                found = r;
            }
            return found != null && taken != null && taken(found) ? null : found;
        }

        /// Saves a resident something has happened to. Over the limit, the oldest untouched ones go first.
        public void KeepResident(Resident r, long now)
        {
            r.TouchedAt = now;
            if (!Residents.Contains(r))
                Residents.Add(r);
            while (Residents.Count > MaxResidents)
            {
                // (one whose letter you carry is kept till it's given: SEAMS item 18, review 5)
                // someone keeping a post, a hermit living at a camp, or one of the test cast is never dropped; a town
                // regular goes only after everyone else, and one the player has met only after every regular he hasn't; a
                // resident enRAGE took up goes last of all, since enRAGE's person lives on with the worry worked out from
                // their facts (WorrySeedOf; the pre-release review: dropped, the worry changed mid-run)
                Resident drop = Residents.Where(x => x != r && !((x.PostId != null || x.HasCamp || x.Cast != null) && !x.Dead) && !LetterCarriedFor(x))
                    .OrderBy(x => WorrySeedOf != null && WorrySeedOf.ContainsValue(x.Id) ? 2 : x.Regular && !x.Dead ? 1 : 0)
                    .ThenBy(x => { PersonState s = Find(x.Id); return x.Regular && s != null && s.Sightings > 0 ? 1 : 0; })
                    .ThenBy(x => { PersonState s = Find(x.Id); return s != null ? s.WakeStage : 0; }).ThenBy(x => x.TouchedAt).FirstOrDefault();
                if (drop == null)
                    break;   // nobody left who may be dropped: kept over the cap rather than throwing here (the recognition plan's critic, point 15)
                Residents.Remove(drop);
                People.Remove(drop.Id);
            }
        }

        /// One whose letter the player carries, not yet given (Core/FindLetter.cs): never pruned while it is.
        private bool LetterCarriedFor(Resident x)
        {
            PersonState s = x != null && !x.Dead ? Find(x.Id) : null;
            return s != null && s.Has(FindLetter.HeldMark) && !s.Has(FindLetter.GivenMark);
        }

        public PersonState Find(string id)
        {
            PersonState st;
            return id != null && People.TryGetValue(id, out st) ? st : null;
        }

        public int Count(string what)
        {
            int n;
            return Counts.TryGetValue(what, out n) ? n : 0;
        }

        public void Bump(string what, int by = 1)
        {
            Counts[what] = Count(what) + by;
        }

        /// An older save was loaded (game time went back): forget what "hasn't happened yet".
        public int Rewind(long now)
        {
            WasAt = LastGameMin;
            long cut = now + 30;
            var kept = new List<Ghost>();   // what the rewind drops, kept as ghosts (Core/Ghosts.cs)
            LastRewound = kept;
            Rewinds++;
            int n = Rumors.RemoveAll(r => r.SentAt > cut);
            n += Affairs.RemoveAll(a => a.Since > cut);
            if (RomanceAt > now)
                RomanceAt = now;
            foreach (PersonState st in People.Values)
            {
                n += st.Heard.RemoveAll(h => h.At > cut);
                n += st.Diary.RemoveAll(d => d.At > cut);
                if (st.Loan != null && st.Loan.At > cut)
                {
                    st.Loan = null;   // lent after the cut: not lent (Core/Verbs.cs)
                    n++;
                }
                n += st.Mind.RemoveAll(m => m.At > cut || m.LearnedAt > cut);   // a thing told after the cut wasn't told (night 2026-10-28)
                foreach (MindMemory m in st.Mind)
                    if (m.LastAt > cut)
                        m.LastAt = cut;   // thought about after the cut: that hasn't happened either (the times stay)
                if (st.NameAt > cut)
                {
                    st.KnowsName = false;
                    st.NameAt = 0;
                    n++;
                }
                foreach (Relative r in st.Family ?? new List<Relative>())
                    n += r.Heard.RemoveAll(h => h.At > cut);
            }
            foreach (Deed d in Deeds)
                if (d != null && d.At > cut)
                    kept.Add(EnrageWestworld.Core.Ghosts.OfDeed(d, now));
            n += Deeds.RemoveAll(d => d.At > cut);
            foreach (Resident r in Residents)
                if (r != null && r.Left && r.LeftAt > cut)
                {
                    r.Left = false;   // the train they took after the cut hasn't left yet (Core/Trains.cs)
                    if (Drummers.Is(r.Trade))
                        Counts[Drummers.CountPrefix + r.Id] = (int)(cut / 1440 + 1);   // a drummer's stay re-booked: he leaves again tomorrow (the R1-R6 review)
                    r.LeftAt = 0;
                    r.Fled = false;
                    r.FledAt = 0;
                    n++;
                }
            foreach (SceneTie t in Ties)
                if (t != null && t.At > cut)
                    kept.Add(EnrageWestworld.Core.Ghosts.OfTie(t, now));
            foreach (WorryLink l in Links)
                if (l != null && l.At > cut)
                    kept.Add(EnrageWestworld.Core.Ghosts.OfLink(l, now));
            n += Ties.RemoveAll(t => t.At > cut);
            foreach (SceneTie t in Ties)
                if (t != null && t.EasedDay > cut / 1440)
                    t.EasedDay = 0;   // (DEPTH item 19; review 2: the night's easing after the cut is not undone, but it can't come twice)
            foreach (SceneTie t in Ties)
                if (t != null && t.PutOffAt > cut)
                {
                    t.PutOffAt = 0;   // a stranger stepped between them after the cut: it hadn't happened yet (DEPTH item 7; review 1)
                    t.PutOffs = 0;
                }
            foreach (string k in Counts.Keys.Where(k => (k.StartsWith("plate|", System.StringComparison.Ordinal) || k.StartsWith("minersmeet|", System.StringComparison.Ordinal)) && Counts[k] > cut / 1440).ToList())
                Counts.Remove(k);   // the Sunday plate came round after the cut (DEPTH item 11; review 1), the miners met after it (reserve R7)
            n += Links.RemoveAll(l => l.At > cut);
            n += TownRumours.RemoveAll(r => r.At > cut);
            n += Papers.RemoveAll(p => p.At > cut);
            if (Events != null)
            {
                n += Events.RemoveAll(e => e.PostedAt > cut || (e.PostedAt == 0 && e.At > cut + 7 * 1440));   // posted after the cut (a trial, a hanging, a show as much as a dance)
                foreach (TownEvent e in Events)
                    if (e.Done && e.Until > cut)
                    {
                        e.Done = false;   // not over yet, as far as the loaded game knows: nor his part in it
                        e.Outcome = null;
                        e.Attended = false;
                        e.Broken = false;
                        e.Part = null;
                        e.Endorsed = null;
                        e.WhiskeyFor = null;
                        e.LeanedOn = null;
                        e.Count = 0;
                        e.Count2 = 0;
                        n++;
                    }
            }
            FixHeld();   // who the law holds follows the trials and hangings left open
            if (Replies != null)
            {
                n += Replies.RemoveAll(r => r.SentAt > cut);
                foreach (Reply r in Replies)
                    if (r.Done && r.ArrivesAt > cut)
                    {
                        r.Done = false;   // not landed yet, as far as the loaded game knows
                        r.Outcome = null;
                        r.MeetAt = 0;
                        r.Met = false;
                        n++;
                    }
            }
            n += Fallen.RemoveAll(f => f.At > cut);
            n += Breakdowns.RemoveAll(b => b.Day * 1440 > cut);
            n += TieHeard.RemoveAll(h => h.At > cut);   // told after the cut: not yet (FIVEDAY item 23)
            foreach (string t in TownLastAbout.Where(kv => kv.Value > cut).Select(kv => kv.Key).ToList())
                TownLastAbout[t] = cut;   // he was last about the town no later than the cut (FIVEDAY item 19)   // a breakdown after the cut hasn't happened (FIVEDAY item 17)
            foreach (string id in Reveries.Where(kv => kv.Value.DiedAt > cut).Select(kv => kv.Key).ToList())
            {
                Reveries.Remove(id);
                n++;
            }
            n += Avengers.RemoveAll(a => a.Stage < 2 && a.DueAt > cut + 10080);
            n += Weddings.RemoveAll(w => !w.Done && w.At > cut + 20160);
            n += Debts.RemoveAll(d => d.Since > cut);
            if (Later != null)
                n += Later.RemoveAll(l => l.At > cut);
            if (WorldLog != null)
                n += WorldLog.RemoveAll(e => e.At > cut);
            if (WorrySettled != null)
                foreach (string id in WorrySettled.Where(kv => kv.Value > cut).Select(kv => kv.Key).ToList())
                {
                    WorrySettled.Remove(id);   // not settled yet, as far as the loaded game knows
                    n++;
                    Happening before = WorldLog != null ? WorldLog.LastOrDefault(e => e != null && e.Kind == "help" && e.DoerId == "player" && e.TargetId == id && e.At <= cut && Thanks.KindOf(e) != null) : null;
                    if (before != null)
                        WorrySettled[id] = before.At;   // an earlier mend for them, which the later one overwrote: back from the log (the review)
                }
            if (WorryWorsened != null)
                foreach (string id in WorryWorsened.Where(kv => kv.Value > cut).Select(kv => kv.Key).ToList())
                {
                    WorryWorsened.Remove(id);
                    n++;
                    Happening before = WorldLog != null ? WorldLog.LastOrDefault(e => e != null && e.Kind == "harm" && e.DoerId == "player" && e.TargetId == id && e.At <= cut) : null;
                    if (before != null)
                        WorryWorsened[id] = before.At;   // an earlier turn for the worse, which the later one overwrote: back from the log (the review)
                }
            Pacing.Rewind(Pace, cut);
            if (Vouches != null)
            {
                n += Vouches.RemoveAll(v => v == null || v.At > cut);   // stood for after the cut: never stood for (reserve R22)
                foreach (Vouched v in Vouches)
                    if (v.Ended != null && v.EndedAt > cut)
                    {
                        v.Ended = v.How = null;   // his week hadn't ended yet, as far as the loaded game knows
                        v.EndedAt = 0;
                        v.Settled = v.Paid = false;
                    }
            }
            if (Stories != null)
                n += Stories.RemoveAll(x => x == null || x.At > cut);   // a story asked after the cut was never asked (reserve R10)
            if (Tales != null)
                n += Tales.RemoveAll(t => t == null || t.At > cut);   // a tale from after the cut never happened (the review of items 22-26)
            if (Deaths != null)
            {
                foreach (DeathEntry d in Deaths.Where(d => d.At > cut).ToList())
                {
                    kept.Add(EnrageWestworld.Core.Ghosts.Killing(d, now));
                    Resident r = Resident.IsResident(d.Id) ? FindResident(d.Id) : null;
                    if (r != null && r.Dead && r.PostId == null && !r.Fled)
                    {
                        r.Dead = false;   // it hadn't happened yet (a post's keeper and a hermit are their own code's)
                        r.DiedAt = 0;
                    }
                    if (d.SharedWithEnrage)
                        (RewoundShares ?? (RewoundShares = new List<string>())).Add(d.Id);
                }
                n += Deaths.RemoveAll(d => d.At > cut);
                _deathIndex = null;
            }
            foreach (Resident r in Residents)
                if (r != null && r.Dead && r.DiedAt > cut && r.PostId == null && !r.Fled)
                {
                    kept.Add(EnrageWestworld.Core.Ghosts.Death(r, now));   // the ghost first: it keeps the hour they died
                    r.Dead = false;   // died after the cut with no ledger entry of ours: alive again all the same (Core/Ghosts.cs)
                    r.DiedAt = 0;
                    n++;
                }
            if (Trysts != null)
            {
                n += Trysts.RemoveAll(t => t != null && t.At > cut);   // a first night undone: never happened
                foreach (TrystRecord t in Trysts)
                    if (t != null && t.LastAt > cut)
                    {
                        t.Count = Math.Max(1, t.Count - 1);   // the last night undone: the one before stands (the nights aren't kept one by one: PaidCents stays as it was, said plainly)
                        t.LastAt = t.At;
                        t.MorningAt = 0;
                        t.ExpectAt = 0;
                        t.Cooled = false;
                        n++;
                    }
            }
            if (Habits != null)
            {
                long cutDay = cut / 1440;
                n += Habits.RemoveAll(h => h != null && h.FirstDay > cutDay);   // a routine begun after the cut: never noticed
                foreach (Habit h in Habits)
                    if (h != null && h.LastDay > cutDay)
                    {
                        h.Days = Math.Max(1, h.Days - 1);   // the day after the cut uncounted (the days aren't kept one by one: one off, said plainly)
                        h.LastDay = cutDay;
                        n++;
                    }
            }
            if (KinGrudges != null)
            {
                foreach (KinGrudge k in KinGrudges)
                    if (k != null && !k.Test && k.At > cut && k.VictimId != null && People != null)   // a test's never set it (the round's review)
                    {
                        PersonState vst;
                        if (People.TryGetValue(k.VictimId, out vst) && vst != null && vst.Told != null)
                            vst.Told.Remove(k.Wrong == "tryst" ? "husband" : k.Wrong == EnrageWestworld.Core.KinGrudges.Thanked ? EnrageWestworld.Core.KinGrudges.Thanked : EnrageWestworld.Core.KinGrudges.Mark);   // (a thanks' own mark: SEAMS item 7)   // the wrong undone: the once-only mark goes with the grudge (a husband's is "husband", Engine.Trysts; the review of items 15-19)
                    }
                n += KinGrudges.RemoveAll(k => k != null && k.At > cut);   // a wrong undone: nobody comes about it
                foreach (KinGrudge k in KinGrudges)
                    if (k != null && k.Done && k.DoneAt > cut)
                    {
                        k.Done = false;   // said after the cut: not said after all
                        k.How = null;
                        k.DoneAt = 0;
                        n++;
                    }
            }
            if (TownLedgers != null)
                foreach (TownLedger tl in TownLedgers.Values)
                    if (tl != null && tl.LastPaperAt > cut)
                        tl.LastPaperAt = 0;   // the month's mention undone with the rest: due again at the next first of the month
            if (Ghosts == null) Ghosts = new List<Ghost>();
            Ghosts.AddRange(kept);
            EnrageWestworld.Core.Ghosts.Trim(Ghosts, now);
            if (LastWorldTickAt > now)
                LastWorldTickAt = now;
            LastGameMin = now;
            return n;
        }

        private static DataContractJsonSerializer Serializer()
        {
            return new DataContractJsonSerializer(typeof(Store), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
        }

        /// Why the save on disk couldn't be read and mustn't be written over (in use by something else at load, or
        /// unreadable and no copy of it could be kept), or null. While it's set the game side saves nothing this
        /// session. Never saved itself.
        [IgnoreDataMember] public string ReadFailed;

        /// The save, read. A file in use by something else (a sync tool, a virus scanner, an editor) is tried again for
        /// a second, then left alone: an empty store that won't be saved, never one that would be written over the real
        /// file. An unreadable file is kept as a dated .bad copy, then the .tmp a save left behind is tried (a crash
        /// between writing it and swapping it in), then a fresh start. No save at all: the .tmp, if one reads.
        public static Store Load(string path, Action<string> log)
        {
            string tmp = path + ".tmp";
            if (!File.Exists(path))
            {
                Store left = TryRead(tmp);
                if (left != null)
                {
                    log?.Invoke("No " + Path.GetFileName(path) + ", but the copy a save left behind reads: recovered from " + Path.GetFileName(tmp));
                    return left;
                }
                return Fresh(null);
            }
            Exception last = null;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                try
                {
                    Store s = Read(path);
                    if (s != null)
                        return s;
                    last = new SerializationException("empty");
                    break;
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    last = ex;
                    System.Threading.Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    last = ex;
                    break;
                }
            }
            if (last is IOException || last is UnauthorizedAccessException)
            {
                log?.Invoke("Couldn't open " + path + " (" + last.Message + "): something else has it open. Nothing is saved this session, so the file stays untouched; close whatever has it and load the game again.");
                return Fresh("the save was in use when it loaded (" + last.Message + ")");
            }
            string bad = path + "." + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bad";
            bool kept = false;
            try
            {
                File.Copy(path, bad, false);
                kept = true;
            }
            catch
            {
            }
            Store fromTmp = TryRead(tmp);
            if (fromTmp != null)
            {
                log?.Invoke("Couldn't read " + path + " (" + last.Message + "); recovered from " + Path.GetFileName(tmp) + (kept ? ", the unreadable file kept as " + Path.GetFileName(bad) : ""));
                return fromTmp;
            }
            log?.Invoke("Couldn't read " + path + " (" + last.Message + ") - starting fresh" + (kept ? ", old file kept as " + Path.GetFileName(bad) : "; no copy of it could be kept, so nothing is saved this session"));
            return Fresh(kept ? null : "the save couldn't be read or copied (" + last.Message + ")");
        }

        private static Store Read(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                Store s = (Store)Serializer().ReadObject(stream);
                if (s != null)
                    s.Fix();
                return s;
            }
        }

        private static Store TryRead(string path)
        {
            try
            {
                return File.Exists(path) ? Read(path) : null;
            }
            catch
            {
                return null;
            }
        }

        private static Store Fresh(string readFailed)
        {
            Store fresh = new Store();
            fresh.Fix();
            fresh.ReadFailed = readFailed;
            return fresh;
        }

        public void Save(string path)
        {
            Trim();
            string tmp = path + ".tmp";
            using (FileStream stream = File.Create(tmp))
                Serializer().WriteObject(stream, this);
            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }

        /// A save in two halves (0.6.3, the stutter): the store turned into JSON, on the thread that changes it...
        public void WriteTo(Stream stream)
        {
            Trim();
            Serializer().WriteObject(stream, this);
        }

        /// Held is derived state (Core/Trials.cs): the open trial or hanging that names a resident, else nothing. A load or a
        /// rewind never strands a man at the jail, nor frees one the law still has.
        private void FixHeld()
        {
            if (Residents == null)
                return;
            foreach (Resident r in Residents)
            {
                if (r == null || r.Id == null)
                    continue;
                string held = null;
                if (Events != null)
                    foreach (TownEvent e in Events)
                        if (e != null && !e.Done && (e.Kind == "trial" || e.Kind == "gallows") && e.AId == r.Id)
                        {
                            held = e.Id;
                            break;
                        }
                r.Held = held;
            }
        }

        /// ...and the bytes put in place, which touches nothing of the store, so it can run on another thread. The same
        /// file as Save: a .tmp first, then swapped in, so a crash part way never leaves half a save.
        public static void WriteFile(string path, byte[] bytes, int count)
        {
            string tmp = path + ".tmp";
            using (FileStream stream = File.Create(tmp))
                stream.Write(bytes, 0, count);
            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }

        /// Keeps the file from growing without end.
        private void Trim()
        {
            Cap(Deeds, 250);
            Cap(TownRumours, 160);
            Cap(Fallen, 150);
            Cap(Debts, 60);
            Cap(Transfers, 30);
            // each person's deeds of yours known and seen, the newest DeedsKnownKept (HARDEN item 2: never trimmed, they kept the
            // ids of deeds long gone from Deeds' 250, and every look that asks whether someone knows a deed walks them; the soak of
            // a copy of Lex's save had the tick climbing over 60 game days). Anyone who knew of you still does: a count read as
            // "knows something of him" stays above nought.
            if (People != null)
                foreach (PersonState st in People.Values)
                {
                    if (st == null)
                        continue;
                    if (st.KnownDeeds != null)
                        Cap(st.KnownDeeds, DeedsKnownKept);
                    if (st.SawDeeds != null)
                        Cap(st.SawDeeds, DeedsKnownKept);
                }
            Weddings.RemoveAll(w => w.Done && LastGameMin - w.At > 43200);
            Avengers.RemoveAll(a => a.Stage == 3 && LastGameMin - a.DueAt > 43200);
            if (Replies != null)
                Replies.RemoveAll(r => r.Done && (r.MeetAt == 0 || r.Met) && LastGameMin - r.ArrivesAt > 43200);
            if (Events != null)
                Events.RemoveAll(e => e.Done && LastGameMin - e.Until > 43200);
            List<Paper> kept = Papers.Where(p => p.Delivered).ToList();
            if (kept.Count > 120)
                Papers.RemoveAll(p => p.Delivered && kept.IndexOf(p) < kept.Count - 120);
            if (Reveries.Count > 200)
                foreach (string id in Reveries.OrderBy(kv => kv.Value.DiedAt).Take(Reveries.Count - 200).Select(kv => kv.Key).ToList())
                    Reveries.Remove(id);
        }

        private static void Cap<T>(List<T> list, int max)
        {
            if (list.Count > max)
                list.RemoveRange(0, list.Count - max);
        }

        private void Fix()
        {
            if (People == null) People = new Dictionary<string, PersonState>();
            if (Rumors == null) Rumors = new List<Rumor>();
            if (Announced == null) Announced = new List<string>();
            if (Affairs == null) Affairs = new List<Affair>();
            if (Deeds == null) Deeds = new List<Deed>();
            if (TownRumours == null) TownRumours = new List<TownRumour>();
            if (Papers == null) Papers = new List<Paper>();
            if (Fallen == null) Fallen = new List<Fallen>();
            if (Reveries == null) Reveries = new Dictionary<string, Reverie>();
            if (Avengers == null) Avengers = new List<Avenger>();
            if (Replies == null) Replies = new List<Reply>();
            if (Events == null) Events = new List<TownEvent>();
            if (Breakdowns == null) Breakdowns = new List<Breakdown>();
            if (TieHeard == null) TieHeard = new List<TieHeard>();
            if (Mends == null) Mends = new Dictionary<string, int>();
            if (Vouches == null) Vouches = new List<Vouched>();   // saves from before reserve R22 (no initializer runs on a read: review 3)
            Vouches.RemoveAll(v => v == null || v.Id == null || v.Test);   // the Testing row's go on load
            if (Stories == null) Stories = new List<StoryRun>();   // saves from before reserve R10 (the lesson of review 3: no initializer runs on a read)
            Stories.RemoveAll(x => x == null || (x.Id != null && x.Id.StartsWith("test-")));
            if (Tales == null) Tales = new List<TownTale>();   // saves from before item 24 load it null (no initializer runs): the add-on switched itself off in the first town (the review of items 22-26)
            Tales.RemoveAll(t => t == null || (t.Id != null && t.Id.StartsWith("test-")));   // the Testing row's tale goes on load
            if (Mends.Count > 500) Mends.Clear();   // (kindnesses since a wrong: losing them only slows a mending)
            if (TownLastAbout == null) TownLastAbout = new Dictionary<string, long>();
            if (TownLedgers == null) TownLedgers = new Dictionary<string, TownLedger>();
            if (KinGrudges == null) KinGrudges = new List<KinGrudge>();
            KinGrudges.RemoveAll(k => k == null || (k.Done && LastGameMin - k.DoneAt > 30 * 1440L));   // the done ones kept a month (the review of item 14)
            if (KinGrudges.Count > 100) KinGrudges.RemoveRange(0, KinGrudges.Count - 100);
            if (Residents != null)
                foreach (Resident r in Residents)
                    if (r != null && !r.ByTrain && r.PostId == null && (r.Kin != null || r.From != null))
                        r.ByTrain = true;   // saves from before ByTrain: kin or a far town with no counter of their own came by train (a post's successor keeps the counter)
            if (Trysts == null) Trysts = new List<TrystRecord>();
            if (Ghosts == null) Ghosts = new List<Ghost>();
            if (Habits == null) Habits = new List<Habit>();
            Habits.RemoveAll(h => h == null || h.Town == null || h.Place == null);
            FixHeld();
            if (Weddings == null) Weddings = new List<Wedding>();
            if (Transfers == null) Transfers = new List<Transfer>();
            if (Debts == null) Debts = new List<Debt>();
            if (Counts == null) Counts = new Dictionary<string, int>();
            if (TownVisits == null) TownVisits = new Dictionary<string, long>();
            if (Residents == null) Residents = new List<Resident>();
            if (Posts == null) Posts = new List<Post>();
            if (Ties == null) Ties = new List<SceneTie>();
            foreach (SceneTie t in Ties)
                if (t != null && t.Crime == null && t.Doer != null && EnrageWestworld.Core.Criminals.IsCrime(t.Kind))
                {
                    t.Crime = t.Kind;   // older saves: the theft read off Kind, as the criminal class did before (the review of items 15-19)
                    t.CrimeAt = t.At;
                    t.CrimeDoer = t.Doer;
                }
            if (Links == null) Links = new List<WorryLink>();
            if (Deaths == null) Deaths = new List<DeathEntry>();
            if (Later == null) Later = new List<LaterAct>();
            if (WorldLog == null) WorldLog = new List<Happening>();
            if (Pace == null) Pace = new PaceState();
            if (Spots == null) Spots = new List<PlaceSpot>();
            if (Plans == null) Plans = new List<Plan>();
            Papers.RemoveAll(p => p != null && p.Id != null && p.Id.StartsWith("test-"));   // the Testing page's test letters go on load
            Pace.Fix();
            WorldLog.RemoveAll(e => e == null);
            foreach (Happening e in WorldLog)
                if (e.Witnesses == null)
                    e.Witnesses = new List<EventWitness>();
            if (RewoundShares == null) RewoundShares = new List<string>();
            foreach (Post p in Posts)
            {
                if (p.Former == null)
                    p.Former = new List<string>();
                if (p.SawFall == null)
                    p.SawFall = new List<string>();
            }
            foreach (Affair a in Affairs)
                if (a.Said == null)
                    a.Said = new List<string>();
            foreach (PersonState st in People.Values)
                st.Fix();
            foreach (Deed d in Deeds)
                if (d.Count <= 0)
                    d.Count = 1;
        }
    }
}
