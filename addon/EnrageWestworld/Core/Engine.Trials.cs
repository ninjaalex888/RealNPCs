// The circuit judge and gallows day in the engine (SIXDAY item 10c; Core/Trials.cs): the arrest when a resident's hand is on
// a death (NoteDied), the trial posted, the verdict weighed when the trial's hours end (there or away), the hanging posted and
// carried out, the breakout and the rescue, the player's testimony and his oath as a special deputy, and what each leaves in
// the town's ties, minds and paper. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// From NoteDied: a resident of ours killed this one (the ledger names him) and the law takes him.
        private void TrialsOnDeath(DeathEntry e)
        {
            if (!Cfg.TownEvents || !Cfg.EventTrials || Store.Events == null || e == null)
                return;
            if (e.By != "another" || !Resident.IsResident(e.ById))
                return;
            Resident accused = Store.FindResident(e.ById);
            string cant = Trials.CantTry(accused, e.Id, Store.Events);
            if (cant != null)
            {
                Log("EVENT trial: " + (accused != null ? accused.Name : e.ById) + " not taken for " + e.Name + "'s death (" + cant + ")");
                return;
            }
            string town = e.Town != null && Rumours.Towns.ContainsKey(e.Town) ? e.Town : accused.Town;
            if (town == null)
                return;
            Arrest(accused, e.Id, e.Name, town, e.At);
        }

        /// The law has him: the trial posted, the notice, the paper.
        private TownEvent Arrest(Resident accused, string deadId, string deadName, string town, long now)
        {
            TownEvent t = Trials.Trial(town, accused, deadId, deadName, now, accused.Id + "|" + deadId);
            t.Posted = true;
            t.PostedAt = now;
            Store.Events.Add(t);
            accused.Held = t.Id;
            Push(new Toast { Kind = "life", Title = "The law in " + ShortTown(town), Weight = 45, Text = Trials.HeldToast(t) });
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = Trials.HeldPaper(t) });
            Log("EVENT trial " + town + ": " + accused.Name + " held for " + t.Extra + "; the judge in " + (t.At / 1440 - now / 1440) + " days");
            return t;
        }

        /// A hogtied man of ours brought into a sheriff's office: a killing on his record means the judge; a theft, a fine in the
        /// paper; else nothing the law can hold him for. Returns what became of him, for the log and the hint.
        public string BroughtIn(string id, string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventTrials || !Resident.IsResident(id) || town == null)
                return null;
            Resident r = Store.FindResident(id);
            if (r == null || r.Dead)
                return null;
            Happening kill = Store.WorldLog.LastOrDefault(h => h != null && h.Kind == "death" && h.DoerId == id && h.TargetId != null);
            if (kill != null && Trials.CantTry(r, kill.TargetId, Store.Events) == null)
            {
                Arrest(r, kill.TargetId, kill.TargetName ?? "a man", town, now);
                return "held for the death of " + (kill.TargetName ?? "a man");
            }
            bool thief = Store.Ties.Any(t => t != null && t.Crime != null && t.CrimeDoer == id);   // the theft kept on the tie, whatever came between them since (the review of items 15-19)
            if (thief)
            {
                if (Cfg.Newspaper)
                    Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = Trials.FinedPaper(r.Name, town) });
                PersonState st = MindOf(r.Id, r.Name);
                if (st != null)
                {
                    st.Affection = Romance.Clamp(st.Affection - 20, -100, 100);
                    Minds.Note(st.Mind, new MindMemory { Kind = "slight", Sub = "broughtin", How = "did", Who = new List<string> { "player" }, Names = new List<string> { "you" }, Town = town, At = now, Feel = -2, Strength = 50, Key = "broughtin:" + r.Id + ":" + now / 1440 }, now);
                }
                Log("EVENT law " + town + ": " + r.Name + " brought in by you: fined ten dollars (a known thief)");
                return "fined ten dollars, a known thief";
            }
            Log("EVENT law " + town + ": " + r.Name + " brought in by you: nothing to hold him on");
            return "nothing to hold him on";
        }

        /// The player's standing in a town this week: its known kills against his kindnesses (Reputation.Standing).
        private string StandingIn(string town, long now)
        {
            int killsKnown, kindnesses;
            StandingCounts(town, now, out killsKnown, out kindnesses);
            return Reputation.Standing(killsKnown, kindnesses);
        }

        /// The same with its counts, for the "Around town" box's regard line (WestworldScript.Regard.cs; Lex, 2026-11-04).
        public string StandingOf(string town, long now, out int killsKnown, out int kindnesses)
        {
            StandingCounts(town, now, out killsKnown, out kindnesses);
            return Reputation.Standing(killsKnown, kindnesses);
        }

        private void StandingCounts(string town, long now, out int killsKnown, out int kindnesses)
        {
            killsKnown = kindnesses = 0;
            long since = now - Reputation.StandingDays * 1440L;
            foreach (Deed d in Store.Deeds)
                if (d.Known && d.Town == town && d.At >= since && (d.Kind == "kill" || d.Kind == "murder" || d.Kind == "lawkill"))
                    killsKnown += Math.Max(1, d.Count);
            foreach (Happening h in Store.WorldLog)
                if (h != null && h.DoerId == "player" && h.Town == town && h.At >= since && Fondness.IsKindness(h.Kind))
                    kindnesses++;
        }

        /// Who saw the killing: the death's witnesses with lasting ids (the WorldLog's "death" record).
        public List<string> WitnessesOf(string deadId)
        {
            var ids = new List<string>();
            if (deadId == null)
                return ids;
            foreach (Happening h in Store.WorldLog)
                if (h != null && h.Kind == "death" && h.TargetId == deadId && h.Witnesses != null)
                    foreach (EventWitness w in h.Witnesses)
                        if (w != null && w.Id != null && w.Id != "player" && w.How == "saw" && !ids.Contains(w.Id))
                            ids.Add(w.Id);
            return ids;
        }

        /// The trial's hours over (there or away): the verdict weighed, and what follows.
        private void TrialVerdict(TownEvent e, long now, bool attended)
        {
            Resident accused = Store.FindResident(e.AId);
            if (accused == null || accused.Dead || accused.Fled)
            {
                e.Outcome = "called off";
                if (accused != null) accused.Held = null;
                string why = accused == null ? "the prisoner is nobody we know" : accused.Dead ? "the prisoner died in his cell" : "the prisoner is gone";
                Push(new Toast { Kind = "life", Title = e.Title, Weight = 20, Text = Trials.CalledOff(e, why) });
                Log("EVENT trial " + e.Town + ": called off (" + why + ")");
                return;
            }
            List<string> witnesses = WitnessesOf(e.BId);
            List<string> deadFriends = Trials.FriendsOf(e.BId, Store.Ties), accusedFriends = Trials.FriendsOf(e.AId, Store.Ties);
            string testimony = e.Part != null && e.Part.StartsWith("testify:", StringComparison.Ordinal) ? e.Part.Substring(8) : null;
            int killsKnown, kindnesses;
            StandingCounts(e.Town, now, out killsKnown, out kindnesses);
            string standing = Reputation.Standing(killsKnown, kindnesses);
            int score = Trials.Score(witnesses.Count, deadFriends.Count, accusedFriends.Count, accused.Personality, testimony, standing, killsKnown);
            string verdict = Trials.Verdict(score, e.Id);
            e.Count = witnesses.Count;
            e.Count2 = score;
            e.Outcome = verdict;
            Push(new Toast { Kind = "life", Title = e.Title, Weight = 50, Text = Trials.VerdictToast(e, verdict) });
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = Trials.VerdictPaper(e, verdict, witnesses.Count) });
            // the player's word, remembered: a lie the witnesses know for one, and the sides he took
            if (testimony == "innocent" && witnesses.Count >= 2)
                foreach (string w in witnesses)
                    Remember(w, "slight", "perjury", "did", e.Town, now, -2, 50, "perjury:" + e.Id + ":" + w);
            if (testimony == "guilty")
            {
                foreach (string f in accusedFriends) Remember(f, "slight", "spokeagainst", "did", e.Town, now, -1, 40, "witness:" + e.Id + ":" + f);
                foreach (string f in deadFriends) Remember(f, "warmth", "spokefor", "did", e.Town, now, 2, 40, "witness:" + e.Id + ":" + f);
            }
            if (verdict == "guilty")
            {
                TownEvent g = Trials.Gallows(e, now);
                g.Posted = true;
                g.PostedAt = now;
                Store.Events.Add(g);
                accused.Held = g.Id;
                Push(new Toast { Kind = "life", Title = "A hanging in " + ShortTown(e.Town), Weight = 45, Text = Trials.GallowsToast(g) });
                if (Cfg.Newspaper)
                    Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = Trials.GallowsPaper(g) });
            }
            else
            {
                accused.Held = null;
                // the town splits: the dead man's friends sour toward him, and a week's talk on both sides
                foreach (string f in deadFriends)
                {
                    Resident fr = Store.FindResident(f);
                    Ties.Note(Store.Ties, f, fr != null ? fr.Name : f, accused.Id, accused.Name, e.Town, "envy", now);
                    Remember(f, "slight", "acquittal", "heard", e.Town, now, -2, 45, "acquittal:" + e.Id + ":" + f, accused.Id, accused.Name);
                }
                foreach (string f in accusedFriends)
                    Remember(f, "warmth", "walkedfree", "heard", e.Town, now, 1, 40, "acquittal:" + e.Id + ":" + f, accused.Id, accused.Name);
            }
            Log("EVENT trial " + e.Town + ": " + accused.Name + " " + verdict + " (score " + score + ": " + witnesses.Count + " saw, " + deadFriends.Count + " for the dead, " + accusedFriends.Count + " for him, " + (accused.Personality ?? "plain") +
                (testimony != null ? ", your word: " + testimony + (standing == "killer" ? " (a killer's word, half)" : "") : "") + (killsKnown >= 2 ? ", a frightened town" : "") + ")" + (attended ? "; you were there" : ""));
        }

        /// A note in one townsperson's mind, when minds are on.
        private void Remember(string id, string kind, string sub, string how, string town, long now, int feel, int strength, string key, string whoId = "player", string whoName = "you")
        {
            if (!Cfg.Minds || id == null)
                return;
            Resident r = Store.FindResident(id);
            PersonState st = MindOf(id, r != null ? r.Name : null);
            if (st == null)
                return;
            Minds.Note(st.Mind, new MindMemory { Kind = kind, Sub = sub, How = how, Who = new List<string> { whoId }, Names = new List<string> { whoId == "player" ? "you" : Person.FirstOf(whoName) }, Town = town, At = now, Feel = feel, Strength = strength, Key = key }, now);
        }

        /// The hour of the hanging over: the condemned man dies, the town remembers, the paper has it.
        private void GallowsDone(TownEvent e, long now, bool attended)
        {
            Resident condemned = Store.FindResident(e.AId);
            if (condemned == null || condemned.Dead || condemned.Fled)
            {
                e.Outcome = "called off";
                if (condemned != null) condemned.Held = null;
                string why = condemned == null ? "the prisoner is nobody we know" : condemned.Dead ? "the prisoner died in his cell" : "the prisoner is gone";
                Push(new Toast { Kind = "life", Title = e.Title, Weight = 20, Text = Trials.CalledOff(e, why) });
                Log("EVENT gallows " + e.Town + ": called off (" + why + ")");
                return;
            }
            bool sworn = e.Part == "deputy";
            condemned.Held = null;
            NoteDied(new DeathEntry { Id = condemned.Id, Name = condemned.Name, Kind = "resident", At = now, Town = e.Town, How = "the rope", By = "another" });
            e.Outcome = "hanged" + (sworn ? ", you stood inside" : attended ? ", you were there" : "");
            // the town: the regulars who went stand in the crowd (most of them), the rest hear of it; his friends think the less of the sworn man
            List<string> friends = Trials.FriendsOf(condemned.Id, Store.Ties);
            int went = 0;
            foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && !x.Fled && x.Town == e.Town))
            {
                bool there = Voice.Hash(e.Id + "|crowd|" + r.Id) % 100 < 60;
                if (there) went++;
                Remember(r.Id, "slight", "hanging", there ? "saw" : "heard", e.Town, now, there ? -2 : -1, there ? 55 : 35, "hanging:" + e.Id + ":" + r.Id, condemned.Id, condemned.Name);
                if (sworn && friends.Contains(r.Id))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + Trials.SwornSoured, -100, 100);
                }
            }
            e.Count = went;
            Push(new Toast { Kind = "life", Title = e.Title, Weight = 40, Text = Trials.HangedToast(e) });
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = Trials.HangedPaper(e, went >= 3 || attended) });
            Log("EVENT gallows " + e.Town + ": " + condemned.Name + " hanged; " + went + " of the town stood outside" + (sworn ? "; you stood inside with the deputies (" + friends.Count + " of his friends think the less of you)" : attended ? "; you were there" : ""));
        }

        /// The trial or hanging that holds this man, open or posted.
        public TownEvent HoldingEvent(string id)
        {
            if (Store.Events == null || id == null)
                return null;
            foreach (TownEvent e in Store.Events)
                if (e != null && !e.Done && (e.Kind == "trial" || e.Kind == "gallows") && e.AId == id)
                    return e;
            return null;
        }

        /// The man held at this town's jail, if any (for the breakout): the event holding him.
        public TownEvent HeldAt(string town)
        {
            if (!Cfg.TownEvents || !Cfg.EventTrials || Store.Events == null || town == null)
                return null;
            foreach (TownEvent e in Store.Events)
                if (e != null && !e.Done && (e.Kind == "trial" || e.Kind == "gallows") && e.Town == town)
                {
                    Resident r = Store.FindResident(e.AId);
                    if (r != null && r.Held == e.Id && !r.Dead && !r.Fled)
                        return e;
                }
            return null;
        }

        /// The player's word at the trial: guilty, innocent or none.
        public void Testify(TownEvent e, string what)
        {
            if (e == null || e.Kind != "trial" || e.Done)
                return;
            e.Part = "testify:" + what;
            Log("EVENT trial " + e.Town + ": you testified: " + what);
        }

        /// Sworn in as a special deputy for the hanging.
        public void SwornIn(TownEvent e)
        {
            if (e == null || e.Kind != "gallows" || e.Done)
                return;
            e.Part = "deputy";
            Log("EVENT gallows " + e.Town + ": you were sworn in as a special deputy");
        }

        /// The prisoner got out of the jail by the player's hand (the night before), or away from the gallows in the uproar:
        /// the event ends, he is gone from the town, a deed as heavy as a robbery, and the two sides of the town remember.
        public void PrisonerFreed(TownEvent e, bool atGallows, long now)
        {
            if (e == null || e.Done)
                return;
            Resident r = Store.FindResident(e.AId);
            e.Done = true;
            e.Broken = atGallows;
            e.Outcome = atGallows ? "stopped by you" : "broken out by you";
            if (r != null)
            {
                r.Held = null;
                r.Fled = true;
                r.FledAt = now;
            }
            if (Cfg.Deeds)
                OnDeed(new Deed { Id = Ids.New(), Kind = "jailbreak", Detail = e.AName, Town = e.Town, At = now, Weight = Trials.JailbreakWeight, Witnesses = atGallows ? 6 : 0 }, new List<string>(), now);
            foreach (string f in Trials.FriendsOf(e.BId, Store.Ties))
            {
                Resident fr = Store.FindResident(f);
                PersonState st = fr != null ? MindOf(f, fr.Name) : null;
                if (st != null)
                    st.Affection = Romance.Clamp(st.Affection + Trials.DeadFriendsSoured, -100, 100);
                Remember(f, "slight", atGallows ? "gallowsbroken" : "jailbreak", "heard", e.Town, now, -3, 60, "freed:" + e.Id + ":" + f);
            }
            foreach (string f in Trials.FriendsOf(e.AId, Store.Ties))
            {
                Resident fr = Store.FindResident(f);
                PersonState st = fr != null ? MindOf(f, fr.Name) : null;
                if (st != null)
                    st.Affection = Romance.Clamp(st.Affection + Trials.AccusedFriendsWarmed, -100, 100);
                Remember(f, "warmth", atGallows ? "friendsaved" : "friendfreed", "heard", e.Town, now, 2, 55, "freed:" + e.Id + ":" + f);
            }
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = atGallows ? Trials.StoppedPaper(e) : Trials.BreakoutPaper(e) });
            Push(new Toast { Kind = "life", Title = e.Title, Weight = 45, Text = e.AName + (atGallows ? " got away in the uproar. The town knows whose shots those were." : " is out of the jail and gone. The sheriff will want a word with somebody.") });
            Log("EVENT " + e.Kind + " " + e.Town + ": " + e.AName + (atGallows ? " got away from the gallows" : " broken out of the jail") + " by you");
        }

        /// Shots by someone else at the hanging: the crowd scatters and the deputies finish it inside.
        private void GallowsFinishedInside(TownEvent e, long now)
        {
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = Trials.FinishedInsidePaper(e) });
            GallowsDone(e, now, false);
            e.Outcome = "hanged inside after shots";   // "hanged...": the ledger counts it as the hanging it was (the review of items 11-12)
        }

        /// A Testing row: no trial open in this town, a living regular is taken for the death of a made-up man, the judge due
        /// tomorrow at ten; a trial posted, it begins in two minutes; a hanging posted, it begins in two minutes. What was done.
        public string TrialTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventTrials)
                return "off ([World] TownEvents, EventTrials)";
            if (town == null)
                return "no town here";
            TownEvent open = Store.Events.FirstOrDefault(x => x != null && !x.Done && (x.Kind == "trial" || x.Kind == "gallows") && x.Town == town);
            if (open != null)
            {
                if (now >= open.At - 30 && now < open.Until)
                    return open.Kind + " of " + open.AName + " is on now";
                long length = open.Until - open.At;
                open.At = now + 2;
                open.Until = open.At + length;
                Log("EVENT " + open.Kind + " " + town + ": moved up by the Testing row, begins in two minutes");
                return open.Kind + " of " + open.AName + " begins in two minutes";
            }
            Resident accused = Store.Residents.FirstOrDefault(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && !r.Woman && r.Town == town);
            if (accused == null)
                return "no living man among " + town + "'s regulars to try";
            var dead = new Resident { Id = Resident.Prefix + "test-" + Ids.New(), Name = Residents.NameFor(false, town + "|dead|" + now), Town = town, Dead = true, MetAt = now, TouchedAt = now, Trade = "a drover", Personality = "Cold" };
            Store.Residents.Add(dead);
            TownEvent t = Arrest(accused, dead.Id, dead.Name, town, now);
            t.At = (now / 1440 + 1) * 1440 + Trials.TrialFrom * 60;
            t.Until = (now / 1440 + 1) * 1440 + Trials.TrialTo * 60;
            return accused.Name + " held for the death of " + dead.Name + "; the judge tomorrow at ten (the row again: it begins in two minutes)";
        }

        private string TrialsStatus(TownEvent e, long now)
        {
            string who = e.AName + " for " + e.Extra;
            if (e.Kind == "trial")
                return who + (e.Part != null && e.Part.StartsWith("testify:", StringComparison.Ordinal) ? ", your word " + e.Part.Substring(8) : "");
            return who + (e.Part == "deputy" ? ", you sworn in" : "");
        }
    }
}
