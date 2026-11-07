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
                Resident[] two = PrizeFight.Pick(regulars, Voice.NowYear, seed);
                if (two == null)
                    continue;
                TownEvent e = PrizeFight.Make(town, today, two[0], two[1], seed);
                e.Posted = true;
                e.PostedAt = now;
                Store.Events.Add(e);
                Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = PrizeFight.WordText(e, seed), DueAt = today * 1440 + PrizeFight.WordHour * 60 });
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
                    Ties.Note(Store.Ties, wonId, wonName, lostId, lostName, e.Town, "makeup", now);   // a fair fight, done: respect, not a grudge
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
            }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = PrizeFight.Morning(e, w, raided), DueAt = (now / 1440 + 1) * 1440 + 7 * 60 });
            Log("EVENT prizefight " + e.Town + ": " + e.Outcome + (paid > 0 ? "; your dollar paid two" : part.Contains("bet:") ? "; your dollar lost" : ""));
            return paid;
        }
    }
}
