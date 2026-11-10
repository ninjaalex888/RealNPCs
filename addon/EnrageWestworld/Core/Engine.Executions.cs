// An execution on the road (Executions.cs): the plan from the captive's own life, and what follows a rescue or a death.
// The death itself is the people ledger's (the game side binds him, so it's noted with how and by whose hand).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// Why he's on his knees, from the worry he's living through today and who is at the other end of it.
        public Executions.Plan ExecutionPlan(Resident r, string region, string seed)
        {
            Person p = PersonOfResident(r);
            long day = Store.LastGameMin / 1440;
            string kind;
            int stage;
            bool good;
            string worry = InnerLife.WorryNow(p, day, out kind, out stage, out good) ? kind : null;
            string who = worry == "debt" || worry == "feud" ? InnerLife.WorryFill(p, day, "{who}") : null;
            WorryLink link = worry != null ? Store.Links.LastOrDefault(l => l.Id == r.Id && l.Kind == worry) : null;
            Executions.Plan plan = Executions.For(worry, who, link != null ? link.OtherId : null, region, seed);
            int was;
            if (SecondActs.On && plan.Gang != null && region != null && Store.Counts.TryGetValue(AtLargeKey(region), out was) && day - was <= SecondActs.AgainDays)
            {
                plan.Again = true;   // the outlaws you let get away here lately: the same men, and they know you (FOURTEENHOUR reserve R3)
                Store.Counts.Remove(AtLargeKey(region));
                _secondAgain++;
            }
            return plan;
        }

        private int _secondWatched, _secondAtLarge, _secondAgain;

        private static string AtLargeKey(string region) { return "atlarge|" + region; }

        /// You saved their man and some of them rode off alive (FOURTEENHOUR reserve R3): the region hears of them two to four days
        /// on, and, outlaws, the next of their executions here within two weeks is the same men.
        public void CaptorsAtLarge(Executions.Plan plan, string region, long now)
        {
            if (!SecondActs.On || plan == null || region == null)
                return;
            if (plan.Gang != null)
                Store.Counts[AtLargeKey(region)] = (int)(now / 1440);
            string seed = region + "|" + now;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(region), At = now, Text = SecondActs.AtLargeNews(plan.Gang ?? (plan.Boss != null ? plan.Boss + "'s men" : null), region, seed),
                                 DueAt = SecondActs.AtLargeNewsDue(now, seed) });
            _secondAtLarge++;
            Log("SECOND ACT the men got away from a rescue near " + region + " (" + (plan.Gang ?? plan.Boss ?? "two men") + "): the news in a few days" + (plan.Gang != null ? ", and they'll know you next time" : ""));
        }

        /// They shot him with you there, and you never stepped in (FOURTEENHOUR reserve R3): a man of his family comes to his town five
        /// to nine days on and says it to your face (the kin grudges' walk-up; once for each man).
        public void KinWatched(Resident r, string town, long now)
        {
            if (Cfg == null || !Cfg.KinGrudges || !SecondActs.On || r == null)
                return;
            Person p = PersonOfResident(r);
            PersonState st = Store.Get(p);
            if (st == null || st.Has(SecondActs.WatchedMark))
                return;
            if (st.Family == null)
                st.Family = Family.For(p);
            Relative rel = KinGrudges.Takes(st.Family);
            st.Mark(SecondActs.WatchedMark);
            if (rel == null)
            {
                Log("SECOND ACT nobody among " + p.Name + "'s kin to come about it");
                return;
            }
            string where = p.Home ?? town;
            if (where == null)
                return;
            KinGrudge k = new KinGrudge
            {
                Id = Ids.New(), VictimId = p.Id, VictimName = p.Name, VictimTown = where, Wrong = SecondActs.Watched, WrongWord = KinGrudges.WrongWord(SecondActs.Watched), At = now,
                DueAt = KinGrudges.Due(now, p.Id + "|w|" + rel.Name), Name = rel.Name, Relation = rel.Relation, Town = rel.Town, Temper = KinGrudges.TemperOf(rel.Name)
            };
            if (Store.KinGrudges == null)
                Store.KinGrudges = new List<KinGrudge>();
            Store.KinGrudges.Add(k);
            _kinMade++;
            _secondWatched++;
            Log("SECOND ACT " + rel.Name + " (" + Person.FirstOf(p.Name) + "'s " + rel.Relation + ") will come to " + where + " in " + (k.DueAt - now) / 1440 + " days: you watched them shoot him");
        }

        /// F7's engine part: the kin coming for a man you watched die, the men at large, and those met again.
        public string SecondActsStatus()
        {
            return _secondWatched + " kin coming for a man you watched die, " + _secondAtLarge + " times the men got away, " + _secondAgain + " times met again";
        }

        /// He met the player over it: they've met, whatever happens next (the corner speaks only of people he's met).
        public void ExecutionSeen(Resident r, long now)
        {
            PersonState st = Store.Get(PersonOfResident(r));
            st.Sightings = Math.Max(st.Sightings, 2);
            KeepResident(r, now);
        }

        /// Saved: fonder of him for good, thinking of it for days, and the town hears of it.
        public void Rescued(Resident r, Executions.Plan plan, string town, string zone, float x, float y, float z, long now)
        {
            Person p = PersonOfResident(r);
            PersonState st = Store.Get(p);
            st.Talks++;
            st.Sightings = Math.Max(st.Sightings, 2);
            st.Affection = Romance.Clamp(st.Affection + 30, -100, 100);
            st.Mark("rescued:" + now / 1440);
            st.Mark("rescue reason:" + plan.Reason + ":" + plan.Boss);
            KeepResident(r, now);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "rescued", Sub = "rescue", How = "did", Names = plan.Boss != null ? new List<string> { plan.Boss } : null, Town = town,
                    At = Store.LastGameMin, Feel = 3, Strength = 80, Key = "rescued:" + Store.LastGameMin / 1440
                }, Store.LastGameMin);   // on his mind for days, fading (Minds.cs)
            OnDeed(Deeds.Make("rescue", p.Name, town, zone, now, x, y, z, 1), null, now);
            Log("EXECUTION " + p.Name + ": saved by you (" + plan.Reason + ", " + plan.Boss + ")");
        }

        /// What the nearest town hears of something that happened out on the road (Core/Wayside.cs), a while after.
        public void WaysideHeard(string text, string town, string key, long now)
        {
            if (string.IsNullOrEmpty(text) || town == null)
                return;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = text, DueAt = Knowing.TalkDue(now, false, key) });
        }

        /// Let go at the last moment, by a captor who wouldn't or kin who paid: nothing of yours in it, and the town has it by evening.
        public void Spared(Resident r, Executions.Plan plan, string town, long now)
        {
            Person p = PersonOfResident(r);
            KeepResident(r, now);
            Later(new LaterAct
            {
                Kind = "news", PersonId = p.Id, Title = "Word around " + ShortTown(town ?? r.Town), At = now,
                Text = p.Name + " came home on foot with no hat and won't say where he has been.", DueAt = Knowing.TalkDue(now, false, p.Id + "spared")
            });
            Log("EXECUTION " + p.Name + ": let go at the last moment (" + plan.Reason + ")");
        }

        /// The player gave someone a hand up after a fall: fonder of him, and they remember it (a kindness done to them).
        /// Only for those who can remember (a lasting id); the game side keeps an unsaved resident first.
        public void HelpedUp(Person p, bool nobodyElse, long now)
        {
            if (p == null || !CanRemember(p.Id))
                return;
            PersonState st = Store.Get(p);
            st.Talks++;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + (nobodyElse ? 10 : 6), -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "helpup", How = "did", Who = new List<string> { "player" }, Town = CurrentTown, At = Store.LastGameMin,
                    Feel = 2, Strength = nobodyElse ? 60 : 50, Key = "helpup:" + Store.LastGameMin / 1440
                }, Store.LastGameMin);
            Log("KIND " + p.Name + ": you gave them a hand up" + (nobodyElse ? " (nobody else stopped)" : ""));
        }

        /// The night visitor was out in this town: next morning (07:00-09:00) the town speaks of a body nobody can account
        /// for (a corner notice, no names).
        public void VisitorSeen(string town, long now)
        {
            VisitorSeen(town, now, true);
        }

        /// `witnessed`: someone besides the player saw it (one ran from it); else the town has only what was left.
        public void VisitorSeen(string town, long now, bool witnessed)
        {
            long morning = (now / 1440 + (now % 1440 >= 7 * 60 ? 1 : 0)) * 1440 + 7 * 60 + Voice.Hash(town + now) % 120;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = Visitor.Morning(town, town + now, witnessed), DueAt = morning });
            Log("VISITOR the town will talk of it in the morning (" + (morning - now) + " game min)");
        }

        /// Someone stood the player a drink with the rest of the room, and he took it: a little fonder of him.
        public void StoodYouADrink(Person p)
        {
            if (p == null || !CanRemember(p.Id))
                return;
            PersonState st = Store.Get(p);
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + 3, -100, 100);
        }

        /// Shot: his kin hear of it, and once he's found, the town (a notice for the corner, 6 to 18 game hours on).
        public void Executed(Resident r, Executions.Plan plan, string town, long now)
        {
            Person p = PersonOfResident(r);
            if (Cfg.WordOfMouth)
                QueueDeathNews(p, now, byYou: false);   // the men did it: no avenger, no grief letter for him
            Later(new LaterAct
            {
                Kind = "news", PersonId = p.Id, Title = "Word around " + ShortTown(town ?? r.Town), At = now,
                Text = Executions.Found(plan, p.Name, town ?? r.Town, p.Id), DueAt = Knowing.TalkDue(now, false, p.Id + "found")
            });
            Log("EXECUTION " + p.Name + ": shot (" + plan.Reason + ", " + plan.Boss + "); the town hears once he's found");
        }

        /// A thought from the days after he was saved, or null (3 game days).
        private string AfterRescue(PersonState st, string seed)
        {
            if (st == null)
                return null;
            long day = Store.LastGameMin / 1440;
            bool lately = false;
            for (long d = day; d > day - 3 && !lately; d--)
                lately = st.Has("rescued:" + d);
            if (!lately)
                return null;
            Executions.Plan plan = null;
            string mark = st.Told != null ? st.Told.LastOrDefault(t => t.StartsWith("rescue reason:")) : null;
            if (mark != null)
            {
                string[] parts = mark.Split(':');
                plan = new Executions.Plan { Reason = parts.Length > 1 ? parts[1] : null, Boss = parts.Length > 2 ? parts[2] : null };
            }
            return Executions.After(plan, seed);
        }
    }
}
