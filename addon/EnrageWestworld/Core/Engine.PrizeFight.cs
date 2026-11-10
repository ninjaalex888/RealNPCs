// The barn prize fight in the engine (FIVEDAY item 10; Core/PrizeFight.cs): the daily tick books one now and then in a town he
// has been to (two of its regulars of a fighting temper, tonight, out back of the saloon), the word goes round from three
// in the afternoon (a toast, never the paper), and when it's over, there or away: the winner by their natures (or the law
// breaking it up, if the player tipped off the sheriff), both men's memories of it, the winner's standing in the town's talk,
// a respect between the two (a fair fight leaves no grudge: the tie warms a step), his bet paid, and the morning's word.
// Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private void PrizeFightsDaily(long now, long today)
        {
            foreach (string town in PrizeFight.Towns)
            {
                if (!Store.TownVisits.ContainsKey(town))
                    continue;   // a town he has never been to: no word he'd hear
                if (Store.Events.Any(x => x != null && x.Kind == PrizeFight.Kind && x.Town == town && Math.Abs(x.At / 1440 - today) < PrizeFight.GapDays))
                    continue;
                if (Store.Events.Any(x => x != null && x.Town == town && !x.Done && x.At / 1440 == today))
                    continue;   // the town has something on tonight already
                string seed = "fight|" + town + "|" + today;
                if (!Voice.Chance(seed, PrizeFight.Chance))
                    continue;
                List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == town).ToList();
                // the town's best fist, if it has one: men from other towns come to try him (FOURTEENHOUR reserve R7)
                Resident holder = BestFist.On ? FistHolder(town, regulars) : null;
                Resident challenger = holder != null && Voice.Chance(seed + "|challenge", BestFist.ChallengePercent) ? ChallengerFor(town, seed) : null;
                Resident[] two = challenger != null ? new[] { holder, challenger } : PrizeFight.Pick(regulars, Voice.NowYear, seed);
                if (two == null)
                    continue;
                TownEvent e = PrizeFight.Make(town, today, two[0], two[1], seed);
                if (challenger != null)
                {
                    e.Extra = "challenge:" + challenger.Town;   // a man come over to try the town's best fist
                    e.Count *= 2;   // twice the purse for it
                    _fistChallenges++;
                }
                e.Posted = true;
                e.PostedAt = now;
                Store.Events.Add(e);
                Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = challenger != null ? BestFist.ChallengeWord(e.AName, e.BName, challenger.Town, town, e.Count, seed) : PrizeFight.WordText(e, seed),
                                     DueAt = today * 1440 + PrizeFight.WordHour * 60 });
                Log("EVENT prizefight " + town + ": " + e.AName + " against " + e.BName + " tonight, a $" + e.Count + " purse (the word goes round at " + PrizeFight.WordHour + ":00)");
            }
        }

        /// The Testing row: a fight booked here two game minutes from now, for an hour, between two of the town's regulars.
        public string PrizeFightTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventPrizeFights)
                return "off ([World] TownEvents, EventPrizeFights)";
            if (town == null)
                return "no town here";
            List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == town).ToList();
            Resident[] two = PrizeFight.Pick(regulars, Voice.NowYear, "test|" + now);
            if (two == null)
                return "no two regulars of a fighting temper in " + town + " (men, 18-48, hot, proud, a drifter, greedy or surly)";
            TownEvent e = PrizeFight.Make(town, now / 1440, two[0], two[1], "test|" + now);
            e.At = now + 2;
            e.Until = now + 62;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            Log("EVENT prizefight " + town + ": the test: " + e.AName + " against " + e.BName + " in two minutes");
            return "A prize fight in two minutes here: " + e.AName + " against " + e.BName + ".";
        }

        /// The player's bet ("bet:A" or "bet:B") or tip-off ("tipped"), kept on the event for its end.
        public void PrizeFightPart(TownEvent e, string part)
        {
            if (e == null || part == null)
                return;
            e.Part = string.IsNullOrEmpty(e.Part) ? part : e.Part + "|" + part;
            Log("EVENT prizefight " + e.Town + ": you " + (part == "tipped" ? "tipped off the sheriff" : "put a dollar on " + (part == "bet:A" ? e.AName : e.BName)));
        }

        private int _fistNamed, _fistChallenges, _fistLost;

        /// The town's best fist among its regulars, or null (FOURTEENHOUR reserve R7).
        private Resident FistHolder(string town, List<Resident> regulars)
        {
            foreach (Resident r in regulars)
                if (r != null && Store.Counts.ContainsKey(BestFist.HolderKey(town, r.Id)))
                    return r;
            return null;
        }

        /// A man of a fighting temper from another of the towns, come to try him.
        private Resident ChallengerFor(string town, string seed)
        {
            return Store.Residents.Where(r => r != null && r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town != null && r.Town != town && PrizeFight.Towns.Contains(r.Town) && PrizeFight.Fighter(r, Voice.NowYear))
                                  .OrderBy(r => Voice.Hash(seed + "|ch|" + r.Id)).FirstOrDefault();
        }

        /// A fight won in a town (the street's, the saloon's, the prize fight's): counted; the third gives him the name, when the town
        /// has nobody holding it. True when he got it now.
        public bool FistWin(string town, string id, string name, long now)
        {
            if (!BestFist.On || town == null || id == null || Store == null)
                return false;
            string wk = BestFist.WinsKey(town, id);
            int wins = Store.Count(wk) + 1;
            Store.Counts[wk] = wins;
            if (wins < BestFist.NameAfter || Store.Counts.ContainsKey(BestFist.HolderKey(town, id)))
                return false;
            string prefix = BestFist.HolderPrefix(town);
            if (Store.Counts.Keys.Any(k => k.StartsWith(prefix, StringComparison.Ordinal)))
                return false;   // the town has its best fist already: he'll have to beat him for it
            Store.Counts[BestFist.HolderKey(town, id)] = (int)(now / 1440);
            _fistNamed++;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = BestFist.Named(name, town, wins, id + "|" + now / 1440), DueAt = Knowing.TalkDue(now, false, id + "|fist") });
            Log("BESTFIST " + name + " is the best fist in " + town + " now (" + wins + " fights won there)");
            return true;
        }

        /// The Testing row: the first regular of a fighting temper here named the best fist, and a challenge booked in two minutes.
        public string BestFistTest(string town, long now)
        {
            if (!BestFist.On)
                return "off ([Scenes] BestFist)";
            if (!Cfg.TownEvents || !Cfg.EventPrizeFights)
                return "off ([World] TownEvents, EventPrizeFights)";
            if (town == null)
                return "no town here";
            List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == town && PrizeFight.Fighter(r, Voice.NowYear)).ToList();
            Resident holder = FistHolder(town, regulars) ?? regulars.FirstOrDefault();
            Resident challenger = ChallengerFor(town, "test|" + now);
            if (holder == null || challenger == null)
                return holder == null ? "no regular of a fighting temper in " + town : "no man of a fighting temper in another town to come and try him";
            Store.Counts[BestFist.HolderKey(town, holder.Id)] = (int)(now / 1440);
            TownEvent e = PrizeFight.Make(town, now / 1440, holder, challenger, "test|" + now);
            e.Extra = "challenge:" + challenger.Town;
            e.Count *= 2;
            e.At = now + 2;
            e.Until = now + 62;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            Log("BESTFIST the test: " + holder.Name + " named the best fist in " + town + "; " + challenger.Name + " of " + challenger.Town + " comes to try him in two minutes");
            return null;
        }

        /// F7's engine part.
        public string BestFistStatus()
        {
            return _fistNamed + " named a town's best fist, " + _fistChallenges + " challenges booked, " + _fistLost + " lost the name";
        }

        /// Who won it (0 A, 1 B), worked out once and kept (Count2: 1 + the winner), so the staging and the end agree.
        public int PrizeFightWinner(TownEvent e)
        {
            if (e.Count2 > 0)
                return e.Count2 - 1;
            Person pa = e.AId != null ? Find(e.AId) : null, pb = e.BId != null ? Find(e.BId) : null;
            Traits ta = pa != null ? Shadow.TraitsOf(TraitSource.From(pa, "resident")) : null, tb = pb != null ? Shadow.TraitsOf(TraitSource.From(pb, "resident")) : null;
            int pct;
            int w = PrizeFight.Winner(ta, tb, (e.Id ?? "") + "|winner", out pct);
            e.Count2 = w + 1;
            return w;
        }

        /// The fight is over. raided: the law broke it up (his tip-off). Returns the cents his bet pays back (0: none or lost).
        private int PrizeFightOver(TownEvent e, long now, bool attended)
        {
            string part = e.Part ?? "";
            bool raided = part.Contains("tipped");
            int w = PrizeFightWinner(e);
            e.Outcome = (attended ? "attended" : "away") + (raided ? ", raided" : ", " + (w == 0 ? e.AName : e.BName) + " won");
            int paid = 0;
            if (!raided)
            {
                string wonId = w == 0 ? e.AId : e.BId, wonName = w == 0 ? e.AName : e.BName, lostId = w == 0 ? e.BId : e.AId, lostName = w == 0 ? e.BName : e.AName;
                if (wonId != null && lostId != null)
                {
                    Ties.Note(Store.Ties, wonId, wonName, lostId, lostName, e.Town, "bout", now);   // a fair fight, done: respect a step, not a grudge (the Part 3 review: "makeup" made friends at once and said sorry)
                    if (Cfg.Minds)
                        foreach (bool won in new[] { true, false })
                        {
                            string id = won ? wonId : lostId, name = won ? wonName : lostName, other = won ? lostName : wonName;
                            PersonState st = MindOf(id, name);
                            if (st == null)
                                continue;
                            Minds.Note(st.Mind, new MindMemory
                            {
                                Kind = "blows", Sub = "prizefight", How = "did", Who = new List<string> { won ? lostId : wonId }, Names = new List<string> { Person.FirstOf(other) },
                                Town = e.Town, At = now, Feel = won ? 2 : -1, Strength = 55, Key = "prizefight:" + e.Id + ":" + id
                            }, now);
                        }
                }
                if (part.Contains("bet:A") && w == 0 || part.Contains("bet:B") && w == 1)
                    paid = PrizeFight.StakeCents * 2;
                if (BestFist.On && wonId != null)
                    FistWin(e.Town, wonId, wonName, now);   // a win toward the town's name (FOURTEENHOUR reserve R7)
            }
            string from = e.Extra != null && e.Extra.StartsWith("challenge:", StringComparison.Ordinal) ? e.Extra.Substring(10) : null;
            if (from != null && !raided && w == 1 && e.AId != null && Store.Counts.Remove(BestFist.HolderKey(e.Town, e.AId)))
                _fistLost++;   // the man from the other town took the name with the purse
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = from != null && !raided ? BestFist.ChallengeMorning(e.AName, e.BName, from, e.Town, w == 0, e.Id ?? e.Town) : PrizeFight.Morning(e, w, raided),
                                 DueAt = (now / 1440 + 1) * 1440 + 7 * 60 });
            Log("EVENT prizefight " + e.Town + ": " + e.Outcome + (paid > 0 ? "; your dollar paid two" : part.Contains("bet:") ? "; your dollar lost" : ""));
            return paid;
        }
    }
}
