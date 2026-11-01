// A favour asked (NIGHT4 item 5; Lex: "more NPC interactions with the main character"): someone at the worst of a worry
// that money can mend (a debt, a tooth, the fare they're saving for) comes up to Arthur in town and, by their own nature,
// asks him outright for the sum, tells him the trouble and leaves the asking unsaid, asks only for a dollar, or loses
// their nerve and bids him good day. Once they've told him, Talk lists the fix (Interact.Options), so a kindness, and
// the thanks, the nods and the lonely's warmth that follow from one, need no history first. Who comes, the table and the
// words here; the walk is the game side's (WestworldScript.Favours.cs). Plain C#, no game; all words original.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Favours
    {
        public const int WaitMs = 15000;       // how long they stand hoping for an answer once they've said it
        public const float NeedPressed = 0.5f; // a money need this pressing brings a stranger over without knowing him
        public const float NeedCoin = 0.6f;    // ... and this, when they're poor, may make it a dollar instead

        /// The worry that brings them: at its worst today (stage 2) and one a sum of his can mend (never the fever at its
        /// crisis: Interact.Mendable).
        public static bool Asks(string worry, int stage)
        {
            return worry != null && stage == 2 && Interact.Mendable(worry, stage) && (worry == "debt" || worry == "tooth" || worry == "savings");
        }

        /// The money need as Comes reads it: a worry at its worst that a sum can mend presses at least NeedPressed (the
        /// needs model gives a stage-2 debt about 0.44, a tooth nothing, so a stranger would never be asked; the review).
        public static float NeedOf(float need, string worry, int stage)
        {
            return Asks(worry, stage) && need < NeedPressed ? NeedPressed : need;
        }

        /// Whether they'd come to him at all: never afraid of him or against him; the proud and the cold only to someone
        /// they think well of or who was kind to them lately; anyone else if they know his name, had a kindness of him, or
        /// the money presses hard enough to ask a stranger.
        public static bool Comes(string temper, string regard, bool knowsName, bool kindLately, float moneyNeed)
        {
            if (regard == "afraid" || regard == "hostile")
                return false;
            bool fond = regard == "fond" || regard == "smitten";
            if (temper == "Proud" || temper == "Cold")
                return fond || kindLately;
            return fond || knowsName || kindLately || moneyNeed >= NeedPressed;
        }

        /// The mind's table, in a fixed order: ask (say it plainly, naming the sum), hint (tell the trouble, the sum unsaid),
        /// holdback (walk over, think better of it, bid him good day: always possible), coin (a dollar instead; only when
        /// poor and pressed).
        public static Staged.Weights Weights(StagedMind m, string regard, float need, bool poor)
        {
            string t = m != null ? m.Temper : null;
            bool fond = regard == "fond" || regard == "smitten" || (m != null && m.ToYou > 0);
            var w = new Staged.Weights();
            int ask = 3, hint = 3, hold = 1;
            if (t == "Greedy" || t == "Drifter") ask += 2;
            if (fond) ask += 2;
            if (need >= 0.75f) ask += 2;
            if (t == "Proud") { hint += 3; ask -= 2; if (!fond) hold += 3; }
            if (t == "Loyal") hint += 2;
            if (t == "Cold") { hint += 2; ask -= 1; }
            if (t == "Coward") { hold += 3; ask -= 1; }
            if (m != null && m.Mood == "sore") hold += 1;
            w.Add("ask", ask < 1 ? 1 : ask).Add("hint", hint).Add("holdback", hold);
            if (poor && need >= NeedCoin)
                w.Add("coin", t == "Proud" ? 1 : t == "Drifter" ? 5 : 3);
            return w.Clamp();
        }

        /// The option as the model is offered it (the pick's words, the person's own point of view).
        public static string Words(string id, string worry)
        {
            switch (id)
            {
                case "ask": return "ask him outright for the " + Sum(worry) + " to " + Mend(worry);
                case "hint": return "tell him your trouble and leave the asking unsaid";
                case "coin": return "ask him only for a dollar, to tide you over";
                default: return "lose your nerve and just bid him good day";
            }
        }

        /// The model's "Right now:" line: the stranger or Mr Morgan, the town, and the trouble at its worst.
        public static string Now(string you, string town, string worry)
        {
            return "You have walked up to " + (you != null ? "Mr " + you : "the stranger Arthur") + " in " + (town ?? "town") + "; your " + WorryWord(worry) + " is at its worst today.";
        }

        /// What they say to his face. `who`: the name of the one they owe, when the debt has a real one; `you`: his surname
        /// when they know it. Never any other name.
        public static string Line(string pick, string worry, string who, string you, bool woman, string seed)
        {
            string m = you != null ? "Mr. " + you : "Mister";
            string owed = who != null ? " to " + who : "";
            switch (pick)
            {
                case "ask":
                    switch (worry)
                    {
                        case "tooth":
                            return Pick(seed, m + ", forgive me. This tooth wants a proper dentist, three dollars, and I haven't got it. Could you spare it?",
                                        m + ". I hate to ask. Three dollars for a dentist, and I'd stop walking the floor at night.");
                        case "savings":
                            return Pick(seed, m + ", I'm five dollars short of a ticket out of here. I'd not ask a soul, only I'm asking you.",
                                        "Five dollars, " + Low(m) + ". That's all stands between me and the train west.");
                        default:
                            return Pick(seed, m + ", I'll not dress it up. I owe eleven dollars" + owed + " and my time's up. Could you see your way to it?",
                                        m + ". Eleven dollars would clear me" + (who != null ? " with " + who : "") + ". I've nowhere else to turn, and that's the truth.");
                    }
                case "hint":
                    switch (worry)
                    {
                        case "tooth":
                            return Pick(seed, m + ". This tooth's had me up three nights running. The barber would only make it worse.",
                                        "Can't eat, can't sleep, " + Low(m) + ". It's this tooth. I'd see a proper man for it, if I could.");
                        case "savings":
                            return Pick(seed, m + ". Been putting by for a ticket west the best part of a year. Never seem to get there.",
                                        "Ever wanted to be shut of a place, " + Low(m) + "? I've been saving to go. It comes slow.");
                        default:
                            return Pick(seed, m + ". There's a debt" + owed + " come due, and I don't know which way to turn.",
                                        "They want what I owe" + (who != null ? ", " + who + " does," : "") + " and I haven't got it, " + Low(m) + ". Thought I'd say it to somebody.");
                    }
                case "coin":
                    return Pick(seed, m + ", could you spare a dollar? Things are bad just now.",
                                "A dollar, " + Low(m) + ", if you've one to spare. I wouldn't ask, otherwise.");
                default:
                    return Pick(seed, "I was going to... no. Never mind me, " + Low(m) + ". Good day.",
                                "Ah. Beg pardon, " + Low(m) + ". Took you for a man I know. Go on, then.");
            }
        }

        /// What they think after: heard out (he stopped, or greeted them), walked past, or their own nerve failing.
        public static string After(string how, string seed)
        {
            switch (how)
            {
                case "heard":
                    return Pick(seed + "|h", "He stopped. He actually listened.", "Didn't laugh me off, at least.", "Maybe there's some good in folk yet.");
                case "holdback":
                    return Pick(seed + "|b", "Couldn't do it. Couldn't make myself ask.", "What was I thinking, going up to him.", "Pride or nerve, one of them failed me.");
                default:
                    return Pick(seed + "|w", "Shouldn't have asked a stranger.", "Begging in the street. What've I come to.", "Well. He's got his own troubles, I expect.");
            }
        }

        private static string Low(string m)
        {
            return m == "Mister" ? "mister" : m;
        }

        private static string Sum(string worry)
        {
            return worry == "tooth" ? "$3" : worry == "savings" ? "$5" : "$11";
        }

        private static string Mend(string worry)
        {
            return worry == "tooth" ? "see a proper dentist" : worry == "savings" ? "make up your fare" : "clear the debt";
        }

        private static string WorryWord(string worry)
        {
            return worry == "tooth" ? "bad tooth" : worry == "savings" ? "saving for a ticket" : "debt";
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed + "|fv") % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string pick in new[] { "ask", "hint", "coin", "holdback" })
                foreach (string worry in new[] { "debt", "tooth", "savings" })
                    foreach (string who in new[] { null, "Hiram Cobb" })
                        foreach (string you in new[] { null, "Morgan" })
                            for (int i = 0; i < 6; i++)
                                yield return Line(pick, worry, who, you, i % 2 == 0, "all" + i);
            foreach (string how in new[] { "heard", "holdback", "walked" })
                for (int i = 0; i < 6; i++)
                    yield return After(how, "all" + i);
        }
    }
}
