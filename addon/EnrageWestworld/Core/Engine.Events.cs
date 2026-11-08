// Town events in the engine (SIXDAY item 10; Core/TownEvents.cs): the daily tick posts the coming dances (Thursday for
// Saturday, every second week per town), the world tick resolves one that passed with nobody there (the regulars' ties, their
// memories, the notice), and the game side asks for the one open in the town it's in, then reports it attended or broken.
// Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// From DailyTick: the dances posted ahead.
        private void EventsDaily(long now)
        {
            if (!Cfg.TownEvents || Store.Events == null)
                return;
            long today = now / 1440;
            if (Cfg.EventElections)
                ElectionsDaily(now, today);
            if (Cfg.EventShows)
                ShowsDaily(now, today);
            if (Cfg.EventPrizeFights)
                PrizeFightsDaily(now, today);   // a fight out back of the saloon tonight (Core/Engine.PrizeFight.cs; FIVEDAY item 10)
            int weekday = InnerLife.Weekday;
            if (weekday < 0)
                return;   // the clock's weekday unknown: no scheduling
            int toSat = TownEvents.DaysToSaturday(weekday);
            if (!Cfg.EventDances || weekday != TownEvents.DancePostDay)
                return;
            long saturday = today + toSat;
            foreach (string town in TownEvents.DanceTowns)
            {
                if (!TownEvents.DanceWeek(town, saturday))
                    continue;
                if (Store.Events.Any(e => e.Kind == "dance" && e.Town == town && e.At / 1440 == saturday))
                    continue;
                if (!Store.TownVisits.ContainsKey(town))
                    continue;   // a town he has never been to posts nothing he'd hear of
                TownEvent d = TownEvents.Dance(town, saturday);
                d.Posted = true;
                d.PostedAt = now;
                Store.Events.Add(d);
                string when = TownEvents.DaysToSaturday(weekday) == 0 ? "tonight" : "on Saturday";
                Push(new Toast { Kind = "life", Title = "Dance in " + ShortTown(town), Weight = 30, Text = TownEvents.PostedText(d, when) });
                if (Cfg.Newspaper)
                    Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = TownEvents.PaperText(d, when) });
                Log("EVENT dance " + town + ": posted for " + DateText(d.At));
            }
        }

        /// Election Day a week off (the clock's date known): a race in every town with five living regulars, posted.
        private void ElectionsDaily(long now, long today)
        {
            int year = InnerLife.CalendarYear, month = InnerLife.ClockMonth, day = InnerLife.ClockDay, daysTo;
            if (!Elections.PostingOpen(year, month, day, out daysTo))
                return;   // from a week before Election Day to its eve (31 October 1899; a skipped day loses nothing)
            int electionDay = Elections.Day(year);
            long electionGameDay = today + daysTo;
            foreach (string town in Rumours.Towns.Keys)
            {
                if (Store.Events.Any(x => x.Kind == "election" && x.Town == town && !x.Done && x.At / 1440 >= today))
                    continue;   // a race posted already this year
                List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == town).ToList();
                if (regulars.Count < Elections.LeastRegulars)
                    continue;
                Resident[] c = Elections.Candidates(regulars, Store.Ties, Voice.NowYear, town + "|" + year);
                if (c == null)
                    continue;
                TownEvent e = Elections.Make(town, electionGameDay, c[0], c[1]);
                e.Posted = true;
                e.PostedAt = now;
                Store.Events.Add(e);
                Push(new Toast { Kind = "life", Title = "Election in " + ShortTown(town), Weight = 35, Text = Elections.PostedText(e) });
                if (Cfg.Newspaper)
                    Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = Elections.PaperText(e) });
                Store.Papers.Add(new Paper { Id = Ids.New(), Kind = "clipping", Title = "Election notice, " + town, From = "a poster", Body = Elections.ClippingBody(e, c[0].Trade, c[1].Trade), At = now, Delivered = true, Town = town });
                Log("EVENT election " + town + ": posted; " + e.AName + " and " + e.BName + " stand for " + e.Extra + " on " + electionDay + " November " + year);
            }
        }

        /// The polls closed: the tally, the paper, and what the town thinks of the two after.
        private void ElectionClose(TownEvent e, long now, bool attended)
        {
            List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == e.Town).ToList();
            Resident a = Store.FindResident(e.AId), b = Store.FindResident(e.BId);
            if (a == null || b == null)
            {
                e.Outcome = "called off";
                Log("EVENT election " + e.Town + ": called off (a candidate gone)");
                return;
            }
            int killsKnown = 0, kindnesses = 0;
            long since = now - Reputation.StandingDays * 1440L;
            foreach (Deed d in Store.Deeds)
                if (d.Known && d.Town == e.Town && d.At >= since && (d.Kind == "kill" || d.Kind == "murder" || d.Kind == "lawkill"))
                    killsKnown += Math.Max(1, d.Count);
            foreach (Happening h in Store.WorldLog)
                if (h != null && h.DoerId == "player" && h.Town == e.Town && h.At >= since && Fondness.IsKindness(h.Kind))
                    kindnesses++;
            string standing = Reputation.Standing(killsKnown, kindnesses);
            int va, vb;
            Elections.Tally(regulars, a, b, Store.Ties, e.Endorsed, standing, e.WhiskeyFor, e.LeanedOn, e.Id, out va, out vb);
            e.Count = va;
            e.Count2 = vb;
            bool aWon = va > vb;
            Resident winner = aWon ? a : b, loser = aWon ? b : a;
            e.Outcome = winner.Name + " (" + va + "-" + vb + ")";
            // the ties: the winner's voters warm to him, the loser's sour toward him; both think of it
            Dictionary<string, bool> votes = Elections.Votes(regulars, a, b, Store.Ties, e.Endorsed, standing, e.WhiskeyFor, e.LeanedOn, e.Id);   // the votes as cast, not the leans (the review)
            foreach (Resident v in regulars)
            {
                if (v.Id == a.Id || v.Id == b.Id)
                    continue;
                bool forA;
                if (!votes.TryGetValue(v.Id, out forA))
                    continue;
                bool won = forA == aWon;
                Ties.Note(Store.Ties, v.Id, v.Name, winner.Id, winner.Name, e.Town, won ? "cheer" : "envy", now);
                if (Cfg.Minds)
                {
                    PersonState st = MindOf(v.Id, v.Name);
                    if (st != null)
                        Minds.Note(st.Mind, new MindMemory { Kind = won ? "warmth" : "slight", Sub = "election", How = "did", Who = new List<string> { winner.Id }, Names = new List<string> { Person.FirstOf(winner.Name) }, Town = e.Town, At = now, Feel = won ? 2 : -1, Strength = 35, Key = "election:" + e.Id + ":" + v.Id }, now);
                }
            }
            Ties.Note(Store.Ties, loser.Id, loser.Name, winner.Id, winner.Name, e.Town, "envy", now);
            Push(new Toast { Kind = "life", Title = "The election in " + ShortTown(e.Town), Weight = 45, Text = Elections.ResultToast(e, va, vb) });
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = Elections.ResultText(e, va, vb) });
            Log("EVENT election " + e.Town + ": " + a.Name + " " + va + ", " + b.Name + " " + vb + " -> " + winner.Name + (e.Endorsed != null ? " (you endorsed " + (e.Endorsed == a.Id ? a.Name : b.Name) + ")" : "") + (attended || e.Attended ? "; you were there" : ""));
        }

        /// The race this person stands in, posted and not over, if any.
        public TownEvent ElectionFor(string id, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventElections || Store.Events == null || id == null)
                return null;
            foreach (TownEvent e in Store.Events)
                if (e != null && e.Kind == "election" && !e.Done && (e.AId == id || e.BId == id) && now < e.Until)
                    return e;
            return null;
        }

        /// The player's part: his word for a candidate, whiskey for the voters (the town sours), a candidate leaned on.
        public void ElectionWord(TownEvent e, string candidateId, string what, long now)
        {
            if (e == null || candidateId == null)
                return;
            if (what == "endorse")
                e.Endorsed = candidateId;
            else if (what == "whiskey")
            {
                e.WhiskeyFor = candidateId;
                foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && x.Held == null && x.Town == e.Town))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + Elections.WhiskeyAffection, -100, 100);   // votes bought: the town thinks the less of it
                }
            }
            else if (what == "lean")
                e.LeanedOn = candidateId;
            Log("EVENT election " + e.Town + ": you " + what + " " + (candidateId == e.AId ? e.AName : e.BName));
        }

        /// From WorldTick: an event whose hour has passed with nobody there resolves itself.
        private void EventsTick(long now)
        {
            if (Store.Events == null)
                return;
            foreach (TownEvent e in Store.Events)
                if (e != null && !e.Done && now >= e.Until)
                    ResolveAway(e, now);
        }

        private void ResolveAway(TownEvent e, long now)
        {
            e.Done = true;
            e.Outcome = "away";
            if (e.Kind == "dance")
            {
                DanceLeaves(e, now, false);
                Push(new Toast { Kind = "life", Title = "Word from " + ShortTown(e.Town), Weight = 20, Text = TownEvents.OverText(e, 0, false) });
                Log("EVENT " + e.Kind + " " + e.Town + ": away (" + (e.Count) + " pairs)");
            }
            else if (e.Kind == "election")
                ElectionClose(e, now, false);
            else if (e.Kind == "trial")
                TrialVerdict(e, now, false);
            else if (e.Kind == "gallows")
                GallowsDone(e, now, false);
            else if (e.Kind == "show")
                ShowOver(e, now, false);
            else if (e.Kind == PrizeFight.Kind)
                PrizeFightOver(e, now, false);
            else if (e.Kind == Shivaree.Kind)
                ShivareeOver(e, now, false);
            else if (e.Kind == Revival.Kind)
                RevivalOver(e, now, false);
            else if (e.Kind == Baptism.Kind)
                BaptismOver(e, now, false);
            else if (e.Kind == BoxSocial.Kind)
                BoxSocialOver(e, now, false);
            else if (e.Kind == BarnRaising.Kind)
                BarnRaisingOver(e, now, false);
            else if (e.Kind == BaseBall.Kind)
                BaseBallOver(e, now, false);
            else if (IsFestival(e.Kind))
                FestivalOver(e, now, false);
        }

        /// What a dance leaves: the regulars' ties and memories (and, attended, their thought of the player who came).
        private void DanceLeaves(TownEvent e, long now, bool attended)
        {
            List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == e.Town).ToList();
            List<string[]> pairs = TownEvents.DanceAway(regulars, Store.Ties, e.Id);
            e.Count = pairs.Count;
            foreach (string[] p in pairs)
            {
                Ties.Note(Store.Ties, p[0], p[1], p[2], p[3], e.Town, p[4], now);
                if (Cfg.Minds)
                    foreach (int i in new[] { 0, 2 })
                    {
                        PersonState st = MindOf(p[i], p[i + 1]);
                        if (st == null)
                            continue;
                        Minds.Note(st.Mind, new MindMemory
                        {
                            Kind = p[4] == "argue" ? "quarrel" : "warmth", Sub = p[4] == "argue" ? "dancequarrel" : "dance", How = "did", Who = new List<string> { p[i == 0 ? 2 : 0] },
                            Names = new List<string> { Person.FirstOf(p[i == 0 ? 3 : 1]) }, Town = e.Town, At = now, Feel = p[4] == "argue" ? -2 : 2, Strength = 40, Key = "dance:" + e.Id + ":" + p[i]
                        }, now);
                        if (attended)
                            st.Affection = Romance.Clamp(st.Affection + TownEvents.DanceAffection, -100, 100);
                    }
            }
        }

        /// The event open now in this town, for the game to stage (null: none).
        public TownEvent EventNear(string town, long now)
        {
            if (!Cfg.TownEvents || Store.Events == null || town == null)
                return null;
            foreach (TownEvent e in Store.Events)
                if (e != null && e.Town == town && TownEvents.Open(e, now))
                    return e;
            return null;
        }

        /// The event's hour is over with the player there (attended), or he left before the end.
        public void FinishEvent(TownEvent e, bool attended, long now)
        {
            if (e == null || e.Done)
                return;
            e.Done = true;
            e.Attended = attended;
            e.Outcome = attended ? "attended" : "left";
            if (e.Kind == "dance")
            {
                DanceLeaves(e, now, attended);
                Push(new Toast { Kind = "life", Title = e.Title, Weight = 25, Text = TownEvents.OverText(e, e.Count, attended) });
                Log("EVENT " + e.Kind + " " + e.Town + ": " + e.Outcome + " (" + e.Count + " pairs)");
            }
            else if (e.Kind == "election")
                ElectionClose(e, now, attended);
            else if (e.Kind == "trial")
                TrialVerdict(e, now, attended);
            else if (e.Kind == "gallows")
                GallowsDone(e, now, attended);
            else if (e.Kind == "show")
                ShowOver(e, now, attended);
            else if (e.Kind == PrizeFight.Kind)
                PrizeFightOver(e, now, attended);
            else if (e.Kind == Shivaree.Kind)
                ShivareeOver(e, now, attended);
            else if (e.Kind == Revival.Kind)
                RevivalOver(e, now, attended);
            else if (e.Kind == Baptism.Kind)
                BaptismOver(e, now, attended);
            else if (e.Kind == BoxSocial.Kind)
                BoxSocialOver(e, now, attended);
            else if (e.Kind == BarnRaising.Kind)
                BarnRaisingOver(e, now, attended);
            else if (e.Kind == BaseBall.Kind)
                BaseBallOver(e, now, attended);
            else if (IsFestival(e.Kind))
                FestivalOver(e, now, attended);
        }

        /// Shots or a gun pointed broke it up: nothing comes of it, and the town remembers who fired.
        public void BreakEvent(TownEvent e, bool byYou, string why, long now)
        {
            if (e == null || e.Done)
                return;
            if (e.Kind == "gallows" && byYou && (why == null || !why.StartsWith("a gun pointed", StringComparison.Ordinal)))
            {
                PrisonerFreed(e, true, now);   // his shots or his violence: the condemned man got away in the uproar (Engine.Trials.cs); a gun merely pointed sends him inside
                return;
            }
            e.Done = true;
            e.Broken = true;
            e.Outcome = "broken: " + why;
            if (e.Kind == "gallows")
            {
                GallowsFinishedInside(e, now);   // someone else's shots: the crowd scattered and the deputies finished it inside
                Log("EVENT gallows " + e.Town + ": broken (" + why + "), finished inside");
                return;
            }
            if (byYou)
            {
                foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && x.Town == e.Town).Take(6))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection - 10, -100, 100);
                }
                Push(new Toast { Kind = "life", Title = e.Title, Weight = 40, Text = "The " + (e.Kind == "dance" ? "dance" : e.Kind) + " in " + e.Town + " broke up in panic. They know who to blame." });
            }
            Log("EVENT " + e.Kind + " " + e.Town + ": broken (" + why + (byYou ? ", by you" : "") + ")");
            if (e.Kind == "trial")
                TrialVerdict(e, now, false);   // the judge rules once the room is cleared, whoever fired
            else if (e.Kind == "show")
                ShowOver(e, now, true);        // the review has the uproar in it
        }

        /// For F7: what's coming and what passed.
        public string EventsStatus(long now)
        {
            if (!Cfg.TownEvents)
                return "off ([World] TownEvents)";
            if (Store.Events == null || Store.Events.Count == 0)
                return "nothing posted yet (dances are posted on Thursdays for towns you've been to)";
            var parts = new List<string>();
            foreach (TownEvent e in Store.Events.OrderBy(x => x.At))
            {
                if (e == null) continue;
                if (!e.Done)
                    parts.Add(e.Kind + " in " + e.Town + " " + (now >= e.At ? "on now" : "in " + Math.Max(0, e.At - now) / 60 + " h") + (e.Kind == "election" ? " (" + e.AName + " v " + e.BName + " for " + e.Extra + (e.Endorsed != null ? ", your word given" : "") + ")" : e.Kind == "trial" || e.Kind == "gallows" ? " (" + TrialsStatus(e, now) + ")" : e.Kind == "show" ? " (" + ShowsStatus(e) + ")" : ""));
                else if (now - e.Until < 3 * 1440)
                    parts.Add(e.Kind + " in " + e.Town + ": " + e.Outcome);
            }
            return parts.Count == 0 ? "nothing coming" : string.Join("; ", parts);
        }
    }
}
