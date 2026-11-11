// The kin who take up a wrong (Core/KinGrudges.cs; SIXDAY item 14; roadmap 20): the engine's side. A wrong of the player's
// written down (NoteEvent) on a living known person with a man among their family makes one grudge, due five to nine days
// on; the game side asks for the one due in the town it's in (the one wronged's town, or the kin's own) and plays the
// walk-up; said or given up, it's done. A grudge waits a week past its day to be found, then lapses. Rewound with the
// rest (Store.Rewind). Nothing here touches the game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private int _kinMade, _kinDone;

        /// The person by id: the roster's, or a resident of ours by their record (a resident not yet met this session isn't
        /// in the roster, and offline none are).
        public Person KinPerson(string id)
        {
            if (id == null)
                return null;
            Person p = Find(id);
            if (p == null && Resident.IsResident(id))
            {
                Resident r = Store.FindResident(id);
                if (r != null)
                    p = PersonOfResident(r);
            }
            return p;
        }

        /// A wrong of the player's on someone the add-on knows: a man of their family will come about it (once for each person).
        /// Someone you saved (a "defend" with you the doer, SavedByYou's): one time in two a kinsman comes to thank you,
        /// by the kin's own walk-up, five to nine days on (SEAMS item 7; [Scenes] KinThanks, with KinGrudges). Once for each person
        /// saved, whether or not one comes; never a test's. Returns whether the happening was a saving (handled here either way).
        private bool KinThanksNote(Happening e)
        {
            if (e == null || e.Kind != "defend" || e.DoerId != "player" || e.TargetId == null || e.TargetId == "player")
                return false;
            if (Cfg == null || !Cfg.KinGrudges || !KinGrudges.ThanksOn || e.Test)
                return true;
            Person p = KinPerson(e.TargetId);
            if (p == null || !p.IsAlive)
                return true;
            PersonState st = Store.Get(p);
            if (st == null || st.Has(KinGrudges.Thanked))
                return true;
            st.Mark(KinGrudges.Thanked);
            if (Voice.Hash(p.Id + "|kinthanks") % 100 >= KinGrudges.ThanksChance)
            {
                Log("KIN THANKS: " + p.Name + "'s family keeps it among themselves (the roll)");
                return true;
            }
            if (st.Family == null)
                st.Family = Family.For(p);
            Relative rel = KinGrudges.Takes(st.Family);
            string town = e.Town ?? p.Home;
            if (rel == null || town == null)
            {
                Log("KIN THANKS: nobody among " + p.Name + "'s kin to come and thank you");
                return true;
            }
            long now = e.At;
            KinGrudge k = new KinGrudge
            {
                Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = town, Wrong = KinGrudges.Thanked, WrongWord = "stood up for " + (p.Woman ? "her" : "him"), At = now,
                DueAt = KinGrudges.Due(now, p.Id + "|t|" + rel.Name), Name = rel.Name, Relation = rel.Relation, Town = rel.Town, Temper = KinGrudges.TemperOf(rel.Name)
            };
            if (Store.KinGrudges == null)
                Store.KinGrudges = new List<KinGrudge>();
            Store.KinGrudges.Add(k);
            _kinMade++;
            Log("KIN THANKS: " + rel.Name + " (" + Person.FirstOf(p.Name) + "'s " + rel.Relation + ", of " + (rel.Town ?? "no town") + ") will come to " + town + " in " + (k.DueAt - now) / 1440 + " days to thank you");
            return true;
        }

        private void KinGrudgeNote(Happening e)
        {
            if (KinThanksNote(e))
                return;   // a saving, not a wrong (SEAMS item 7)
            if (Cfg == null || !Cfg.KinGrudges || e == null || e.Test || e.DoerId != "player" || e.TargetId == null || e.TargetId == "player" || !KinGrudges.Counts(e.Kind))
                return;
            Person p = KinPerson(e.TargetId);
            if (p == null || !p.IsAlive)
                return;
            PersonState st = Store.Get(p);
            if (st == null || st.Has(KinGrudges.Mark))
                return;
            if (st.Family == null)
                st.Family = Family.For(p);
            Relative rel = KinGrudges.Takes(st.Family);
            st.Mark(KinGrudges.Mark);   // once for each person, whether or not a man of theirs takes it up
            if (rel == null)
            {
                Log("KIN GRUDGE: nobody among " + p.Name + "'s kin to take up the " + (e.Kind ?? "wrong"));
                return;
            }
            string town = e.Town ?? p.Home;
            if (town == null)
                return;
            long now = e.At;
            KinGrudge k = new KinGrudge
            {
                Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = town, Wrong = e.Kind, WrongWord = KinGrudges.WrongWord(e.Kind), At = now,
                DueAt = KinGrudges.Due(now, p.Id + "|" + rel.Name), Name = rel.Name, Relation = rel.Relation, Town = rel.Town, Temper = KinGrudges.TemperOf(rel.Name)
            };
            if (Store.KinGrudges == null)
                Store.KinGrudges = new List<KinGrudge>();
            Store.KinGrudges.Add(k);
            _kinMade++;
            Log("KIN GRUDGE: " + rel.Name + " (" + Person.FirstOf(p.Name) + "'s " + rel.Relation + ", of " + (rel.Town ?? "no town") + ", " + (k.Temper ?? "plain") + ") will come to " + town +
                " in " + (k.DueAt - now) / 1440 + " days: you " + k.WrongWord);
        }

        /// A wrong between townsfolk in the last day (a beating, a hand in a pocket: KinGrudges.TownWrong) on a known
        /// living person with a man among their family: two times in five a man of theirs comes about it, five to nine days
        /// on, for the one who did it (FIVEDAY item 32b; with [Scenes] KinGrudges and KinTownWrongs). Once for each person
        /// wronged (TownMark), whether or not anyone comes. From the daily tick. Returns how many were made.
        public int KinTownDaily(long now)
        {
            if (Cfg == null || !Cfg.KinGrudges || !Cfg.KinTownWrongs)
                return 0;
            int made = 0;
            foreach (SceneTie t in Store.Ties.ToList())
            {
                if (!KinGrudges.TownWrong(t.Kind) || t.Doer == null || t.Doer == "player" || now - t.At < 0 || now - t.At > 1440)
                    continue;
                string victimId = t.OtherId(t.Doer);
                Person p = KinPerson(victimId);
                if (p == null || !p.IsAlive)
                    continue;
                PersonState st = Store.Get(p);
                if (st == null || st.Has(KinGrudges.TownMark))
                    continue;
                st.Mark(KinGrudges.TownMark);
                if (!Voice.Chance(victimId + "|kintown|" + t.At, KinGrudges.TownChance))
                    continue;
                if (st.Family == null)
                    st.Family = Family.For(p);
                Relative rel = KinGrudges.Takes(st.Family);
                string town = t.Town ?? p.Home;
                if (rel == null || town == null)
                    continue;
                var k = new KinGrudge
                {
                    Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = town, Wrong = "town:" + t.Kind, WrongWord = t.Kind == "rough" ? "beat him" : "robbed him", At = t.At,
                    DueAt = KinGrudges.Due(t.At, p.Id + "|" + rel.Name), Name = rel.Name, Relation = rel.Relation, Town = rel.Town, Temper = KinGrudges.TemperOf(rel.Name),
                    DoerId = t.Doer, DoerName = t.Doer == t.A ? t.AName : t.BName
                };
                if (Store.KinGrudges == null)
                    Store.KinGrudges = new List<KinGrudge>();
                Store.KinGrudges.Add(k);
                _kinMade++;
                made++;
                Log("KIN GRUDGE: " + rel.Name + " (" + Person.FirstOf(p.Name) + "'s " + rel.Relation + ") will come to " + town + " in " + (k.DueAt - now) / 1440 + " days for " + k.DoerName + ", who " + k.WrongWord);
            }
            return made;
        }

        /// The kin due in this town now (the one wronged's town or their own), not yet done; one past its week lapses. A
        /// townsman's grudge (DoerId) only when doerNear says he's about (else it waits its week); with none, only yours.
        public KinGrudge KinDue(string town, long now, Func<string, bool> doerNear = null)
        {
            if (Cfg == null || !Cfg.KinGrudges || town == null || Store.KinGrudges == null)
                return null;
            foreach (KinGrudge k in Store.KinGrudges)
            {
                if (k == null || k.Done || now < k.DueAt)
                    continue;
                if (now - k.DueAt > KinGrudges.WaitsDays * 1440L)
                {
                    KinDone(k, "never found you", now);
                    continue;
                }
                if (KinGrudges.IsThanks(k) && (Store.DeathOf(k.VictimId) != null || KinWronged(k.VictimId)))
                {
                    KinDone(k, "no thanks now: the one you saved is dead, or you wronged him since", now);   // (review 2 of SEAMS)
                    continue;
                }
                if (k.DoerId != null && (doerNear == null || !doerNear(k.DoerId)))
                    continue;   // a townsman's: only when he's about (FIVEDAY item 32b)
                if (k.VictimTown == town || k.Town == town)
                    return k;
            }
            return null;
        }

        /// Whether a wrong of yours on this person has since been taken up by kin (the wrongs' once-only mark): no thanks after it.
        private bool KinWronged(string id)
        {
            PersonState st = id != null ? Store.Find(id) : null;
            return st != null && st.Has(KinGrudges.Mark);
        }

        public void KinDone(KinGrudge k, string how, long now)
        {
            if (k == null || k.Done)
                return;
            k.Done = true;
            k.How = how;
            k.DoneAt = now;
            _kinDone++;
            Log("KIN GRUDGE: " + k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + "): " + how);
        }

        /// A kin's walk called off by something outside the story (a mission, the add-on stopping): the grudge stands, due again
        /// tomorrow, and his week to find you runs from then (the review of items 15-19).
        public void KinLater(KinGrudge k, string why, long now)
        {
            if (k == null || k.Done)
                return;
            k.DueAt = now + 1440L;
            Log("KIN GRUDGE: " + k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + "): " + why + "; he comes again tomorrow");
        }

        /// A Testing row: a man of this person's kin due here now, as if the player had threatened them a week ago; the mark
        /// isn't set (a test). What was made, or why not ("no ...").
        public string KinTest(string victimId, string town, long now)
        {
            if (Cfg == null || !Cfg.KinGrudges)
                return "no: [Scenes] KinGrudges is off";
            Person p = KinPerson(victimId);
            if (p == null)
                return "no known person near to be the one wronged";
            if (town == null)
                return "no town here";
            PersonState st = Store.Get(p);
            if (st.Family == null)
                st.Family = Family.For(p);
            Relative rel = KinGrudges.Takes(st.Family);
            if (rel == null)
                return "no man among " + p.Name + "'s kin (" + string.Join(", ", st.Family.Select(r => r.Relation)) + ")";
            if (Store.KinGrudges == null)
                Store.KinGrudges = new List<KinGrudge>();
            KinGrudge k = new KinGrudge
            {
                Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = town, Wrong = "threat", WrongWord = KinGrudges.WrongWord("threat"), At = now - 7 * 1440L, DueAt = now,
                Name = rel.Name, Relation = rel.Relation, Town = rel.Town, Temper = KinGrudges.TemperOf(rel.Name)
            };
            Store.KinGrudges.Add(k);
            _kinMade++;
            Log("KIN GRUDGE (a test): " + rel.Name + " (" + Person.FirstOf(p.Name) + "'s " + rel.Relation + ", " + (k.Temper ?? "plain") + ") due in " + town + " now");
            return rel.Name + ", " + Person.FirstOf(p.Name) + "'s " + rel.Relation + " (" + (k.Temper ?? "plain") + "), is due in " + town + " now; the look comes within fifteen seconds";
        }

        /// A Testing row (FIVEDAY item 32b): the victim's kinsman due here now for a townsman who beat them a week ago;
        /// no mark set. What was made, or why not ("no ...").
        public string KinTownTest(string victimId, string doerId, string doerName, string town, long now)
        {
            if (Cfg == null || !Cfg.KinGrudges || !Cfg.KinTownWrongs)
                return "no: [Scenes] KinGrudges or KinTownWrongs is off";
            Person p = KinPerson(victimId);
            if (p == null || doerId == null)
                return "no two named people near (a known resident to be wronged, and another man to have done it)";
            if (town == null)
                return "no town here";
            PersonState st = Store.Get(p);
            if (st.Family == null)
                st.Family = Family.For(p);
            Relative rel = KinGrudges.Takes(st.Family);
            if (rel == null)
                return "no man among " + p.Name + "'s kin (" + string.Join(", ", st.Family.Select(r => r.Relation)) + ")";
            if (Store.KinGrudges == null)
                Store.KinGrudges = new List<KinGrudge>();
            var k = new KinGrudge
            {
                Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = town, Wrong = "town:rough", WrongWord = "beat him", At = now - 7 * 1440L, DueAt = now,
                Name = rel.Name, Relation = rel.Relation, Town = rel.Town, Temper = KinGrudges.TemperOf(rel.Name), DoerId = doerId, DoerName = doerName
            };
            Store.KinGrudges.Add(k);
            _kinMade++;
            Log("KIN GRUDGE (a test): " + rel.Name + " (" + Person.FirstOf(p.Name) + "'s " + rel.Relation + ") due in " + town + " now for " + doerName);
            return rel.Name + ", " + Person.FirstOf(p.Name) + "'s " + rel.Relation + ", is due in " + town + " now for " + doerName + "; he comes within fifteen seconds";
        }

        /// For the F7 dump.
        public string KinStatus(long now)
        {
            if (Cfg == null || !Cfg.KinGrudges)
                return "off";
            List<KinGrudge> all = Store.KinGrudges ?? new List<KinGrudge>();
            int waiting = all.Count(k => k != null && !k.Done && now < k.DueAt), due = all.Count(k => k != null && !k.Done && now >= k.DueAt), done = all.Count(k => k != null && k.Done);
            KinGrudge next = all.Where(k => k != null && !k.Done).OrderBy(k => k.DueAt).FirstOrDefault();
            return waiting + " waiting, " + due + " due, " + done + " done (" + _kinMade + " made, " + _kinDone + " done this session)" +
                   (next != null ? "; next: " + next.Name + " (" + Person.FirstOf(next.VictimName ?? "?") + "'s " + next.Relation + ") in " + next.VictimTown + (now < next.DueAt ? " in " + (next.DueAt - now) / 60 + " h" : ", due") : "");
        }
    }
}
