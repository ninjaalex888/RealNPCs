// Arthur in a brawl, on J (FOURTEENHOUR item 11; [Scenes] BrawlVerbs). A fight is the one moment a street looks to a stranger
// who might settle it. While a fight goes on near him: break it up (his shout, heeded by each fighter's nature and by what they
// think of him: the fond and the afraid heed it more, the hostile less, and those who won't may turn on him), stand with the man
// losing (the one beating him turns on Arthur; that side remembers it kindly, the other doesn't), or take the bet of the onlooker
// waiting for a taker (the wagers', FIVEDAY item 1): a dollar on the other man, settled when the fight is, and a lost bet is
// owed; not paying it is a welsh he holds against you. J only: no Talk row. Plain C#, no game; the game side is
// Game/WestworldScript.BrawlVerbs.cs.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class BrawlVerbs
    {
        public const string Kind = "brawl", PayKind = "brawlpay";
        public const int StakeCents = 100, PromptMs = 7000, PayPromptMs = 30000, AgainMs = 20000;
        public const float NearMetres = 20f;

        /// How much his standing moves a man's heeding of Arthur's shout: the fond and the afraid more, the hostile less.
        public static float Standing(string regard)
        {
            return regard == "afraid" ? 0.20f : regard == "fond" ? 0.15f : regard == "hostile" ? -0.15f : 0f;
        }

        public static List<InteractOption> Rows(string loser, string backer, string backedName, string otherName, int cashCents, string between = null)
        {
            var rows = new List<InteractOption>
            {
                new InteractOption { Id = "brawl:breakup", Label = "\"That's enough! Break it up!\"" },
                new InteractOption { Id = "brawl:stand", Label = "Stand with " + (loser ?? "the man losing") + (between != null ? " (" + between + ")" : "") + "." }
            };
            if (backer != null)
                rows.Add(new InteractOption { Id = "brawl:bet", Label = "Take " + backer + "'s bet: " + Money(StakeCents) + " on " + (otherName ?? "the other man") + " against his " + (backedName ?? "man") + ".",
                                              Why = cashCents < StakeCents ? "you haven't a dollar on you" : null });
            rows.Add(new InteractOption { Id = "brawl:watch", Label = "Leave them to it." });
            return rows;
        }

        public static List<InteractOption> PayRows(string to, int cents, int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "brawlpay:pay", Label = "Pay " + to + " the " + Money(cents) + " you owe.", Why = cashCents < cents ? "you haven't the money" : null },
                new InteractOption { Id = "brawlpay:no", Label = "Walk away from it." }
            };
        }

        public static string Money(int cents) { return cents % 100 == 0 ? "$" + cents / 100 : "$" + (cents / 100.0).ToString("0.00"); }

        private static readonly string[] Shouts = { "That's enough! Break it up!", "Hey! Leave off him!", "All of you, back off!", "That's it, it's over! Go home!" };
        private static readonly string[] TakesYou = { "You're on, mister. A dollar says you're wrong.", "Done. Your dollar and mine.", "A stranger's money spends the same. Done." };
        private static readonly string[] PaysYou = { "Here. Take your dollar and be damned.", "Fair's fair. Your dollar.", "You read that right. Here." };
        private static readonly string[] WantsIt = { "That's a dollar you owe me, mister.", "My man won. Pay up.", "I'll take that dollar now." };
        private static readonly string[] Welshed = { "A welsher. I won't forget it.", "Walk off, then. The whole town'll hear it.", "Not a man of your word, are you." };
        public static string Shout(string seed) { return Pick(Shouts, seed + "|shout"); }
        public static string TakesYourBet(string seed) { return Pick(TakesYou, seed + "|takes"); }
        public static string PaysYourBet(string seed) { return Pick(PaysYou, seed + "|pays"); }
        public static string WantsHisDollar(string seed) { return Pick(WantsIt, seed + "|wants"); }
        public static string WelshedOn(string seed) { return Pick(Welshed, seed + "|welshed"); }

        // ---- a welsher's name (SEAMS item 4; [Scenes] WelshersShunned) ------------------------------------------------------
        // The man you welshed on said the whole town would hear it, and nothing heard it: the next backer at the next fight held
        // out his dollar as if nothing had happened. Now the betting men of a town where you welshed this week won't take your
        // money, and the one who'd have offered says why. The psychology: a broken promise travels fastest among the people it
        // could happen to next (reputation as the gossip of those with a stake in it), and being refused in public is the cost.
        public static bool Shunning = true;    // from the settings

        // ---- the debt in the menu (SEAMS item 13; [Scenes] StandReminds) ----------------------------------------------------------
        // The man losing the fight is one who stood by you this week, or one who owes you: the J row says so ("Stand with Abe (he
        // stood by you)"). The card says it (NameCardsStood/Owes), but a card isn't read in a fistfight; the moment a debt can
        // be repaid is the moment it should be in front of you. Reciprocity is a choice only when you can see it.
        public static bool Reminds = true;     // from the settings

        /// The words in brackets after his name, or null: he stood by you outranks he owes you (yours to repay comes first).
        public static string Between(bool stoodByYou, bool owesYou)
        {
            if (!Reminds)
                return null;
            return stoodByYou ? "he stood by you" : owesYou ? "he owes you" : null;
        }
        public const long ShunDays = 7;        // how long the town's betting men remember a welsh of yours

        /// Whether you've welshed on a bet in this town within the week (the "welshed" happening, you the doer; a test's never),
        /// and not paid it since (SEAMS item 24: a welsh made good is no welsh to the betting men).
        public static bool WelshedHere(List<Happening> log, string town, long now)
        {
            if (!Shunning || log == null || town == null)
                return false;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e != null && e.Kind == "welshed" && e.DoerId == "player" && !e.Test && e.Town == town && e.At <= now && now - e.At < ShunDays * 1440 &&
                    !(RepayOn && WelshSettled(log, e)))
                    return true;
            }
            return false;
        }

        // ---- the welsh made good (SEAMS item 24; [Scenes] WelshRepay) ------------------------------------------------------------
        // A welsh was for ever: the man you owed a dollar held it against you and the town's betting men refused you, and nothing
        // you did could square it. Now, meeting him again within the month (J near him), you can pay him what you owe: his liking
        // back up, his memory of a bet paid (the wagers' own "wagerpaid"), and the betting men take your money again. The
        // psychology: a debt repaid late restores trust, if not all of it (a man remembers he had to wait).
        public static bool RepayOn = true;     // from the settings
        public const string RepayKind = "welshpay", Repaid = "welshpaid";
        public const long RepayDays = 30;
        public const long RepayAfterMinutes = 60;   // a game hour at least before you can make it good

        /// Whether you've paid him since then (a "welshpaid" with you the doer, him the one paid).
        public static bool RepaidSince(List<Happening> log, string id, long since)
        {
            if (log == null || id == null)
                return false;
            foreach (Happening e in log)
                if (e != null && e.Kind == Repaid && e.DoerId == "player" && e.TargetId == id && e.At >= since)
                    return true;
            return false;
        }

        /// The welsh of yours on him not yet paid, within the month: its happening, or null.
        public static Happening UnpaidWelsh(List<Happening> log, string id, long now)
        {
            if (!RepayOn || log == null || id == null)
                return null;
            foreach (Happening e in log)   // the oldest unpaid first: each payment squares one welsh (review 7)
                if (e != null && e.Kind == "welshed" && e.DoerId == "player" && e.TargetId == id && !e.Test && e.At <= now && now - e.At < RepayDays * 1440 && !WelshSettled(log, e))
                    return e;
            return null;
        }

        /// Whether this welsh has been paid: payments to him are matched to his welshes in order, one each, a payment only to a
        /// welsh before it (review 7: one dollar used to clear every welsh before it).
        public static bool WelshSettled(List<Happening> log, Happening welsh)
        {
            if (log == null || welsh == null || welsh.TargetId == null)
                return false;
            var owed = new List<Happening>();
            foreach (Happening e in log)
            {
                if (e == null || e.DoerId != "player" || e.TargetId != welsh.TargetId)
                    continue;
                if (e.Kind == "welshed" && !e.Test)
                    owed.Add(e);
                else if (e.Kind == Repaid)
                {
                    int i = owed.FindIndex(w => w.At <= e.At);
                    if (i >= 0)
                    {
                        if (owed[i] == welsh)
                            return true;
                        owed.RemoveAt(i);
                    }
                }
            }
            return false;
        }

        public static List<InteractOption> RepayRows(string to, int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "welshpay:pay", Label = "Pay " + (to ?? "them") + " the " + Money(StakeCents) + " you owe from the bet.", Why = cashCents < StakeCents ? "you haven't a dollar on you" : null },
                new InteractOption { Id = "welshpay:no", Label = "Not now." },
            };
        }

        private static readonly string[] RepaidWords = { "Took you long enough. But I'll take it.", "Well. Late, but you paid. I'll say so.", "Huh. Didn't think I'd see that dollar." };
        public static string RepaidLine(string seed) { return Pick(RepaidWords, seed + "|repaid"); }

        private static readonly string[] Refuses = { "Not with you, mister. Word's gone round about your bets.", "I'd sooner bet with the horse. You don't pay.",
                                                     "Keep your dollar. Heard what happened last time." };
        public static string WontBetWithYou(string seed) { return Pick(Refuses, seed + "|refuse"); }
        public static IEnumerable<string> AllLines() { foreach (string[] a in new[] { Shouts, TakesYou, PaysYou, WantsIt, Welshed, Refuses, RepaidWords }) foreach (string s in a) yield return s; }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
