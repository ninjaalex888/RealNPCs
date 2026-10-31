// Late, specific thanks (NIGHT3; the research list's "late, specific thanks", the psychologist's reciprocity): someone
// Arthur did a kindness to through the interaction menu (a drink stood, money given, a debt settled: the world log has
// each, with him as the doer and them as the one it was done to, and the save keeps the day he settled a debt) who sees
// him again in town some hours or days later may come over and thank him for that very thing in their own words. Not
// everyone: a drink stood is answered with a drink some day, not a speech, so few come for one; most come for five
// dollars, nearly all for a debt (a roll once per kindness, by its key: a miss is never mentioned). The biggest kindness
// is the one they thank him for, and that thanks covers the rest. A wrong he did them since (a warning, a slight, a blow,
// a theft) cancels what came before it. The proud who took his eleven dollars as a loan pay it back, three days on at the
// soonest. The cold, anyone sitting, and a woman after dark or indoors (crossing a saloon to a strange man, in 1899, said
// something else) give a nod and keep it to themselves; the greedy don't come. Plain C#, no game; all words original.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class ThanksOwed
    {
        public string Kind;      // drink, money, debt
        public long At;          // the game minute he did it
        public string Key;       // the mark that says it's been thanked for
        public int Cents;        // the money given (100 or 500), else 0
        public bool Loan;        // a debt they took as a loan: they bring the money back (Thanks.Repays)
        public List<string> Keys = new List<string>();   // every kindness this thanks covers (all marked at once)
    }

    public static class Thanks
    {
        public static long After = 360;  // [Scenes] ThanksAfterHours: six game hours at least: thanks later, not on the spot
        public static long Within = 7 * 1440;   // [Scenes] ThanksWithinDays: a week at most (the world log, kept to its last 400, may forget a drink sooner)
        public static long RepayAfter = 3 * 1440;   // [Scenes] ThanksLoanDays: a loan of eleven dollars, from a man with four and a button, takes days to raise
        public static int DrinkChance = 15, DollarChance = 35, FiveChance = 60, MendedChance = 90;   // [Scenes] ThanksDrinkChance, ThanksDollarChance, ThanksFiveChance, ThanksMendedChance: how often each kindness is thanked later
        public const string DebtOutcome = "settled their debt";   // the menu's outcome on the help it records for a debt
        public const string DoctorOutcome = "paid the doctor for them", ToothOutcome = "paid their dentist", FareOutcome = "made up their fare";

        /// The help's outcome, for the worry he paid to mend (the world log keeps it; KindOf reads it back).
        public static string OutcomeOf(string worry)
        {
            return worry == "fever" ? DoctorOutcome : worry == "tooth" ? ToothOutcome : worry == "savings" ? FareOutcome : DebtOutcome;
        }

        /// The thanks for a worry he paid to mend: debt, doctor, tooth, ticket.
        public static string KindOfFix(string worry)
        {
            return worry == "fever" ? "doctor" : worry == "tooth" ? "tooth" : worry == "savings" ? "ticket" : "debt";
        }

        /// A worry mended: thanked like a settled debt (nine in ten come; first of all he's owed).
        private static bool Mended(string kind)
        {
            return kind == "debt" || kind == "doctor" || kind == "tooth" || kind == "ticket";
        }

        /// What the kindness was, for the words: drink, money, debt (or the doctor, a tooth, a ticket); null for anything else.
        public static string KindOf(Happening e)
        {
            if (e == null || e.DoerId != "player")
                return null;
            switch (e.Kind)
            {
                case "round": return "drink";
                case "charity": return "money";
                case "help": return e.Outcome == DebtOutcome ? "debt" : e.Outcome == DoctorOutcome ? "doctor" : e.Outcome == ToothOutcome ? "tooth" : e.Outcome == FareOutcome ? "ticket" : null;
                default: return null;
            }
        }

        /// A wrong from him that cancels the kindnesses before it: a warning, a slight, a blow or a theft.
        public static bool Wrong(Happening e)
        {
            if (e == null || e.DoerId != "player" || e.Kind == null)
                return false;
            if (e.Kind == "threat" || e.Kind == "faced")
                return true;
            if (!e.Kind.StartsWith("deed:", StringComparison.Ordinal))
                return false;
            string g = Minds.DeedGroup(e.Kind.Substring(5));
            return g == "deed:violence" || g == "deed:theft";
        }

        /// The cents a charity happening was for (the menu writes "gave $5"), else 0.
        public static int CentsOf(Happening e)
        {
            return e != null && e.Kind == "charity" ? (e.Outcome == "gave $5" ? 500 : 100) : 0;
        }

        /// Whether they come at all for this kindness: once per kindness, by its key (drink 15%, a dollar 35%, five 60%,
        /// a debt 90%). A test always comes.
        public static bool Comes(string kind, int cents, string key, bool test)
        {
            if (test)
                return true;
            int chance = Mended(kind) ? MendedChance : kind == "money" ? (cents >= 500 ? FiveChance : DollarChance) : DrinkChance;
            return Voice.Hash(key + "|comes") % 100 < chance;
        }

        private static int Rank(string kind)
        {
            return Mended(kind) ? 3 : kind == "money" ? 2 : 1;
        }

        /// The kindness this person would thank him for now, or null: the biggest he did them (a debt, then money, then a
        /// drink; the latest of a kind) at least After and at most Within ago, not cancelled by a wrong since, not thanked
        /// for yet (`thanked`: their marks), and one they'd come for (Comes). `settledAt`: the game minute the save says he
        /// settled their debt, or -1 (the log may have let it go). `loanAt`: whether the debt settled at that minute was
        /// taken as a loan (a mark the menu left): a loan always comes, with the money, and not before RepayAfter, and
        /// nothing else thanked meanwhile covers it. A test skips the waits and the roll, not the week.
        public static ThanksOwed Due(List<Happening> log, string id, long now, Func<string, bool> thanked, bool test = false, long settledAt = -1, Func<long, bool> loanAt = null, string settledKind = "debt")
        {
            if (id == null)
                return null;
            long wrongAt = -1;
            var owed = new List<ThanksOwed>();
            if (log != null)
                for (int i = log.Count - 1; i >= 0; i--)
                {
                    Happening e = log[i];
                    if (e == null || e.TargetId != id || e.At > now || now - e.At >= Within)
                        continue;
                    if (Wrong(e))
                    {
                        wrongAt = Math.Max(wrongAt, e.At);
                        continue;
                    }
                    string kind = KindOf(e);
                    if (kind != null)
                        owed.Add(new ThanksOwed { Kind = kind, At = e.At, Key = "thanks:" + e.Kind + "|" + e.At, Cents = CentsOf(e) });
                }
            if (settledKind != null && settledAt >= 0 && settledAt <= now && now - settledAt < Within && !owed.Exists(o => o.Key == "thanks:help|" + settledAt))   // (null: the game couldn't tell which worry: no words rather than the wrong ones)
                owed.Add(new ThanksOwed { Kind = settledKind, At = settledAt, Key = "thanks:help|" + settledAt });   // the worry it mended
            ThanksOwed best = null;
            var keys = new List<string>();
            foreach (ThanksOwed o in owed)
            {
                if (o.At <= wrongAt || (thanked != null && thanked(o.Key)))
                    continue;   // cancelled by what he did since, or thanked for already
                o.Loan = o.Kind == "debt" && loanAt != null && loanAt(o.At);
                bool fresh = !test && now - o.At < (o.Loan ? RepayAfter : After);
                if (o.Loan && fresh)
                    continue;   // they'll come when they have the money: not covered by anything thanked meanwhile
                keys.Add(o.Key);
                if (fresh || (!o.Loan && !Comes(o.Kind, o.Cents, o.Key, test)))
                    continue;
                if (best == null || Rank(o.Kind) > Rank(best.Kind) || (Rank(o.Kind) == Rank(best.Kind) && o.At > best.At))
                    best = o;
            }
            if (best == null)
                return null;
            best.Keys = keys;   // the thanks covers every kindness still owed (the beer before the debt isn't thanked for again tomorrow)
            return best;
        }

        /// Whether anyone at all may be owed thanks now (the cheap look before any scan of the street): a kindness in the
        /// window in the log, or a debt settled in it.
        public static bool AnyDue(List<Happening> log, long now, bool test = false, IEnumerable<long> settled = null)
        {
            if (settled != null)
                foreach (long at in settled)
                    if (at <= now && now - at < Within && (test || now - at >= After))
                        return true;
            if (log == null)
                return false;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e != null && e.TargetId != null && e.At <= now && now - e.At < Within && (test || now - e.At >= After) && KindOf(e) != null)
                    return true;
            }
            return false;
        }

        /// How they take it: "walk" (they come over and say it), "nod" (a look and a thought: the cold, anyone sitting,
        /// and a woman after dark or indoors), or null (the greedy, and anyone who fears or hates him now).
        public static string Way(string temper, string regard, bool woman, bool nightOrIndoors, bool seated)
        {
            if (regard == "afraid" || regard == "hostile" || temper == "Greedy")
                return null;
            return temper == "Cold" || seated || (woman && nightOrIndoors) ? "nod" : "walk";
        }

        /// The cents they pay him back: a debt they said was a loan (the proud, for the debt he settled), three days on at
        /// the soonest (Due holds it till then); else 0.
        public static int Repays(ThanksOwed o, long now)
        {
            return o != null && o.Kind == "debt" && o.Loan && now - o.At >= RepayAfter ? Interact.SettleCents : 0;
        }

        /// The mark the menu leaves when a debt was settled as a loan (the proud said so), and Due's loanAt reads.
        public static string LoanKey(long at)
        {
            return "loan:" + at;
        }

        /// What they say to his face, for this kindness. `sameDay`: it was today (no "the other day", no "every night since").
        public static string Line(string kind, string temper, bool woman, string seed, bool sameDay, int repays = 0)
        {
            if (repays > 0)
                return woman ? "Here. Eleven dollars. I said I'd see it back to you, and I have."
                             : "Eleven dollars, every cent of it. I told you it was a loan.";
            bool shy = temper == "Coward";
            switch (kind)
            {
                case "drink":
                    return shy ? "That drink you stood me. It was kind."
                               : sameDay ? Pick(seed + "|tdr", "Next one's on me, mister. Don't let me forget it.", "I owe you a drink. I'll not forget.")
                                         : Pick(seed + "|tdr", "That whiskey the other day. Next one's mine.", "Still owe you a drink, mister. I keep a tally.");
                case "money":
                    return shy ? "The money you gave me. It went further than you'd think."
                               : sameDay ? Pick(seed + "|tmo", "Meant to tell you: that money mattered. Folks don't give, mostly.", "That money of yours is bread on the table tonight. Thank you.")
                                         : Pick(seed + "|tmo", "What you gave me the other day. Bought bread with it, and then some. Thank you.", "That money of yours did more good than you know. I wanted you told.");
                case "debt":
                    return shy ? "You paid what I owed. That's all I came to say. Thank you."
                               : sameDay ? Pick(seed + "|tde", "You paid my debt and never asked a thing back. I won't forget it.", "Square with the man I owed, thanks to you. I'll tell folks so.")
                                         : Pick(seed + "|tde", "Slept easy every night since you settled that for me. I wanted you to know.", "You paid my debt and never asked a thing back. I haven't forgotten it.");
                case "doctor":
                    return shy ? "The doctor you paid for. They're mending at home. Thank you."
                               : sameDay ? Pick(seed + "|tdo", "The doctor's been out to us, thanks to you. I wanted you told.", "Two dollars, and the doctor came. I'll not forget who paid him.")
                                         : Pick(seed + "|tdo", "We're through the worst since the doctor came. You paid him. I haven't forgotten.", "That doctor you paid for. Best two dollars anybody ever spent on us.");
                case "tooth":
                    return shy ? "The dentist. I can eat again. Thank you."
                               : sameDay ? Pick(seed + "|tto", "Tooth's out and I'm still standing. I've you to thank for that.", "No more clove oil, thanks to you, mister.")
                                         : Pick(seed + "|tto", "Eating on both sides again, mister. That was your three dollars.", "Not a twinge since you paid the dentist. Wanted you told.");
                case "ticket":
                    return shy ? "The money for the fare. I'm going, thanks to you."
                               : sameDay ? Pick(seed + "|tti", "Got the ticket in my pocket. Wanted you to see it.", "Fare's paid, mister. Now I only want the nerve.")
                                         : Pick(seed + "|tti", "I'll be leaving one of these mornings. Wouldn't be, but for you.", "When I go, I'll think of you as the train pulls out.");
                default:
                    return "Wanted to say thank you. That's all.";
            }
        }

        /// What they think instead, with a nod his way.
        public static string Nod(string kind, string seed)
        {
            switch (kind)
            {
                case "drink": return "Him. Stood me a drink once. I'll stand him one back, some day.";
                case "debt": return Pick(seed + "|tnd", "Him that paid my debt. I owe him. I'll not say it out loud.", "Owe that man more than eleven dollars. Won't tell him so.");
                case "doctor": return "Him that paid the doctor. I owe him more than I can say.";
                case "tooth": return "Him. Paid the dentist for me. Can't look at him without grinning.";
                case "ticket": return "Him that made up my fare. I'll write to him, if I ever learn his name.";
                default: return "Him. Gave me money when he didn't have to. I keep a tally.";
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string kind in new[] { "drink", "money", "debt", "doctor", "tooth", "ticket", "other" })
                foreach (string temper in new[] { null, "Coward", "Proud", "Hothead" })
                    for (int i = 0; i < 6; i++)
                        foreach (bool w in new[] { false, true })
                            foreach (bool same in new[] { false, true })
                                yield return Line(kind, temper, w, "all" + i, same, kind == "debt" && temper == "Proud" && i == 0 ? Interact.SettleCents : 0);
            foreach (string kind in new[] { "drink", "money", "debt", "doctor", "tooth", "ticket" })
                for (int i = 0; i < 4; i++)
                    yield return Nod(kind, "all" + i);
        }
    }
}
