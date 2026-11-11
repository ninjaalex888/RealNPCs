// Three more verbs for Talk (SIXDAY item 6; roadmap 6: "ask about someone they know, lend money, introduce two people").
// Ask about: "What do you know of <X>?" for someone they've had dealings with (a tie: a quarrel, a kindness, sweethearts,
// an acquaintance), answered by the tie's warmth and kind and their own nature, with a warning when the town's log knows X
// for a killer, a thief or a brawler; the afraid and the hostile carry no tales. Lend: ten dollars to someone known, taken
// by most (the proud won't be lent to, the hostile want nothing of his), owed for three game days, then "About that ten
// dollars...": paid back by those who can, put off by those who can't, and now and then denied outright by the greedy and
// the drifting. Introduce: "Have you met <X>?" for someone standing near whom they don't know: a word each, a hat tipped,
// and from then on they're acquainted (a tie: they nod in passing, and the street's reasons can pair them). Why they answer
// as they do: gossip is a currency the warm spend freely and the afraid hoard; a loan is a bond the proud read as charity
// and the greedy as a windfall; an introduction is a small gift of standing, and people remember who gave it. Plain C#, no
// game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Verbs
    {
        public const int LendCents = 1000;
        public static int LoanDays { get { return SessionClock.Days(3, 1); } }   // game days before the loan is due   // the session clock (DEPTH item 16): SessionClock.Days(as built, a session's)
        public const int AskDays = 14;              // a tie this fresh can be asked about
        public const int KnownForDays = 7;          // what the log knows X for, this fresh
        public const int MostAbout = 2;             // rows of "What do you know of..."
        public const int LentAffection = 4, RepaidAffection = 2, PutOffAffection = -1, RefusedAffection = -6, IntroducedAffection = 3;
        public const int LentLiking = 8, PaidLiking = 2, RefusedLiking = -10, IntroduceLiking = 5, EarlyLiking = -3;
        public const string LentOutcome = "lent $10";

        public static string AskAboutLabel(string first)
        {
            return "What do you know of " + first + "?";
        }

        public static string LendLabel()
        {
            return "Could you use a loan? ($" + LendCents / 100 + ")";
        }

        public static string CollectLabel()
        {
            return "About that ten dollars I lent you...";
        }

        public static string IntroduceLabel(string first)
        {
            return "Have you met " + first + "?";
        }

        public static string OtherId(SceneTie t, string id)
        {
            return t.A == id ? t.B : t.A;
        }

        /// The ties worth asking about: the freshest within AskDays, the other alive and named, never a lift (the mark never
        /// knew who); at most MostAbout.
        public static List<SceneTie> AskAboutOf(List<SceneTie> all, string id, long now, Func<string, bool> alive)
        {
            var list = new List<SceneTie>();
            if (all == null || id == null)
                return list;
            foreach (SceneTie t in all)
            {
                if (t == null || !t.Has(id) || t.Kind == "lift" || t.At > now || now - t.At > AskDays * 1440L)
                    continue;
                string other = OtherId(t, id), name = t.Other(id);
                if (other == null || string.IsNullOrEmpty(name) || (alive != null && !alive(other)))
                    continue;
                list.Add(t);
            }
            list.Sort((x, y) => y.At.CompareTo(x.At));
            if (list.Count > MostAbout)
                list.RemoveRange(MostAbout, list.Count - MostAbout);
            return list;
        }

        /// What the town's log knows X for lately, by X's own doing: killer, thief, brawler, or null.
        public static string KnownFor(List<Happening> log, string xId, long now)
        {
            if (log == null || xId == null)
                return null;
            string worst = null;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e == null || e.DoerId != xId || e.At > now || now - e.At > KnownForDays * 1440L || e.Kind == null)
                    continue;
                string k = e.Kind;
                if (k == "death" || k.Contains("kill") || k.Contains("murder"))
                    return "killer";
                if (k.Contains("pickpocket") || k.Contains("lift") || k.Contains("mugging") || k.Contains("holdup") || k.Contains("theft") || k.Contains("robbery"))
                    worst = "thief";
                else if (worst == null && (k.Contains("brawl") || k.Contains("rough") || k.Contains("assault")))
                    worst = "brawler";
            }
            return worst;
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        /// Their answer about X: the afraid and the hostile carry no tales; sweethearts go shy; a warm tie praises by what passed
        /// between them, a hostile one grieves, an acquaintance states a fact; a warning when X is known for something.
        public static InteractAnswer AskAbout(SceneTie t, string id, string temper, string regard, string knownFor, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            string first = Person.FirstOf(t.Other(id));
            bool woman = Ties.OtherIsWoman(t, id);
            if (regard == "hostile")
            {
                a.Line = Pick(seed + "|h", "Ask {him} yourself. I don't talk to you.", "I'd not give you the time of day, let alone that.", "What I know of " + first + " is mine. Walk on.");
                a.VoiceHate = 30;
                return Fill(a, woman);
            }
            if (regard == "afraid")
            {
                a.Line = Pick(seed + "|a", "I... I don't carry tales, mister. Please.", first + "? I hardly know {him}. Honest.", "Don't ask me about folk. I keep to myself.");
                a.VoiceFear = 40;
                return Fill(a, woman);
            }
            if (Ties.Sweethearts(t))
            {
                a.Line = Pick(seed + "|s", first + "? ... Why do you ask?", "What's " + first + " to you, mister?", "I'd sooner not say what I know of " + first + ". Not to a stranger.");
                a.Thought = Pick(seed + "|st", "He asked about " + first + ". My face went hot.", "Why would he ask after " + first + "? What's he heard?");
                return Fill(a, woman);
            }
            bool cold = temper == "Cold", hot = temper == "Hothead";
            string k = t.Kind ?? "";
            if (t.Warmth > 0)
            {
                if (cold)
                    a.Line = Pick(seed + "|wc", first + " is sound. That's all I'll say.", "Straight dealer, " + first + ". Rare enough.", "Nothing against " + first + ". Nothing much for, either. Sound.");
                else if (k == "help" || k == "carried")
                    a.Line = Pick(seed + "|wh", first + " picked me up out of the street once. I don't forget that.", "When I was down, " + first + " was the one who stopped. Says all you need.");
                else if (k == "round")
                    a.Line = Pick(seed + "|wr", first + " stood me a drink when I was dry. A good sort.", "Bought me a whiskey once and asked nothing. That's " + first + ".");
                else if (k == "defend")
                    a.Line = Pick(seed + "|wd", first + " stood up for me when it counted. I'd do the same.", "Stepped in for me, " + first + " did. Not many would.");
                else if (k == "makeup")
                    a.Line = Pick(seed + "|wm", first + " and I had words once. Mended since. Good people, under it.", "We fell out, " + first + " and me. We're square now. I think the better of {him} for it.");
                else
                    a.Line = Pick(seed + "|w", first + "? A good sort. You could do worse than know {him}.", "Known " + first + " a while. Never a wrong word between us.", first + " is all right. Pays {his} way, keeps {his} word.");
            }
            else if (t.Warmth < 0)
            {
                bool theyDid = t.Doer != null && t.Doer != id;
                if (k == "brawl" || k == "drunk")
                    a.Line = Pick(seed + "|hb", first + " and I came to blows. {He} swung first, whatever {he} says.", "Don't say that name to me. We've fought, and we'll fight again.", "I've a bruise with " + first + "'s name on it. Ask {him} about it.");
                else if (k == "debt")
                    a.Line = Pick(seed + "|hd", first + " owes me money. Or I owe {him}. Depends who you ask.", "There's money between " + first + " and me, and no good feeling.");
                else if (k == "cheat")
                    a.Line = Pick(seed + "|hc", first + " cheats at cards. Watch your hand.", "Don't sit at a table with " + first + ". Not unless you like losing.");
                else if (k == "standoff")
                    a.Line = Pick(seed + "|hs", "Guns came out between " + first + " and me once. Both still breathing, more's the pity.", first + "? Keep {him} where you can see {his} hands.");
                else if (k == "rough" || k == "pickpocket" || k == "mugging" || k == "holdup")
                    a.Line = theyDid ? Pick(seed + "|hr", first + " had {his} hand in my coat once. Keep yours buttoned.", first + " put my face into a bar. I'll not forget it.")
                                     : Pick(seed + "|hr2", "We had a disagreement, " + first + " and me. It ended my way.", first + " and I don't speak. {He} knows why.");
                else
                    a.Line = hot ? Pick(seed + "|hh", "Don't say that name to me.", first + "? I'd cross the street. And I'd spit.") : Pick(seed + "|hx", "We had words, " + first + " and me. I've not forgotten them.", first + " and I don't get on. Leave it there.");
                a.VoiceHate = 15;
            }
            else
                a.Line = Pick(seed + "|k", "Know {him} to nod to. Keeps {himself} to {himself}.", first + "? Works around here. Pays {his} way, far as I know.", "We talk now and then, " + first + " and me. Nothing to tell.", first + ". Seen {him} about. Can't say more than that.");
            if (knownFor != null && t.Warmth <= 0)
                a.Line += knownFor == "killer" ? " Mind, there's blood on {his} hands, and not long dried." : knownFor == "thief" ? " Keep a hand on your purse around {him}." : " {He}'s handy with {his} fists, and quick to use them.";
            else if (knownFor == "killer")
                a.Line += " Though I hear {he}'s killed a man lately. I'd not have believed it.";
            return Fill(a, woman);
        }

        private static InteractAnswer Fill(InteractAnswer a, bool woman)
        {
            a.Line = Pronouns(a.Line, woman);
            if (a.Thought != null)
                a.Thought = Pronouns(a.Thought, woman);
            return a;
        }

        /// {he} {him} {his} {himself} {He} of the one spoken of.
        public static string Pronouns(string line, bool woman)
        {
            if (line == null)
                return null;
            return line.Replace("{himself}", woman ? "herself" : "himself").Replace("{He}", woman ? "She" : "He").Replace("{he}", woman ? "she" : "he")
                       .Replace("{him}", woman ? "her" : "him").Replace("{his}", woman ? "her" : "his");
        }

        /// A loan of ten dollars offered: taken by most (it's owed from here), refused by the proud (charity with a longer face),
        /// wanted not at all by the hostile; the afraid take it as if ordered to; the greedy and the drifting take it gladly
        /// and mean to forget.
        public static InteractAnswer Lend(string temper, bool woman, string regard, bool moneyWorry, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            if (regard == "hostile")
            {
                a.Line = Pick(seed + "|lh", "Keep your money and your company.", "I'd owe you nothing, mister. Not even that.");
                a.VoiceHate = 40;
                return a;
            }
            if (temper == "Proud" && regard != "afraid")
            {
                a.Line = woman ? Pick(seed + "|lpw", "I'll not be beholden to a stranger, mister.", "Thank you, no. I manage.") : Pick(seed + "|lpm", "A loan's charity with a longer face. Keep it.", "I don't borrow. Never have.");
                a.Thought = "Lend. As if I'd come begging.";
                return a;
            }
            a.Event = "loan";
            a.Cents = LendCents;
            a.LikeDelta = LentLiking;
            a.Friendly = true;
            if (regard == "afraid")
            {
                a.Line = "...If you say so, mister. I'll have it back to you. I swear it.";
                a.VoiceFear = 25;
                a.Thought = "His money. Now I'm tied to him. Pay it quick and be done.";
            }
            else if (temper == "Greedy" || temper == "Drifter")
            {
                a.Line = Pick(seed + "|lg", "Ten dollars? I'll not say no. You'll have it back... soon enough.", "Well now. That's handsome of you. Soon as I'm flush.");
                a.Thought = Pick(seed + "|lgt", "Ten dollars, and he barely knows me. Easy money, if he forgets.", "Soon as I'm flush, I said. Flush is a long way off.");
            }
            else if (moneyWorry)
            {
                a.Line = Pick(seed + "|lw", "I'll not pretend I don't need it. Three days, and it's back in your hand, every cent.", "You don't know what this is to me. Three days. You have my word.");
                a.Thought = "Ten dollars. Room to breathe. I'll pay it. I will.";
            }
            else
            {
                a.Line = Pick(seed + "|l", "Could use it, truth be told. Three days, and you'll have it back.", "That's good of you. I'll not forget it, nor the paying back.", "Ten dollars. All right. Three days, and I'll find you.");
                a.Thought = "A stranger's ten dollars in my pocket. Pay it back first thing.";
            }
            return a;
        }

        [System.Runtime.Serialization.DataContract]
        public sealed class Loan
        {
            public Happening Lent;                                                         // the log's record, when it still has it
            [System.Runtime.Serialization.DataMember(EmitDefaultValue = false)] public long At;      // lent then (saved on the person: the log is capped)
            [System.Runtime.Serialization.DataMember(EmitDefaultValue = false)] public long DueAt;
            [System.Runtime.Serialization.DataMember(EmitDefaultValue = false)] public int Cents;
        }

        /// The loan to them still out, if any: the latest "loan" by the player with no "repaid" or "refused" since; a "putoff"
        /// since moves its due day.
        public static Loan LoanOut(List<Happening> log, string id, long now)
        {
            if (log == null || id == null)
                return null;
            long putOff = -1;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e == null || e.At > now)
                    continue;
                if (e.DoerId == id && e.TargetId == "player")
                {
                    if (e.Kind == "repaid" || e.Kind == "refused")
                        return null;
                    if (e.Kind == "putoff" && putOff < 0)
                        putOff = e.At;
                }
                else if (e.DoerId == "player" && e.TargetId == id && e.Kind == "loan")
                    return new Loan { Lent = e, At = e.At, DueAt = Math.Max(e.At, putOff) + LoanDays * 1440L, Cents = LoanCents(e) };
            }
            return null;
        }

        /// The cents a loan happening was for, from its outcome ("lent $10"); LendCents when it doesn't say.
        public static int LoanCents(Happening e)
        {
            string o = e != null ? e.Outcome : null;
            int i = o != null ? o.IndexOf('$') : -1;
            if (i < 0)
                return LendCents;
            int n = 0, j = i + 1;
            while (j < o.Length && char.IsDigit(o[j]))
                n = n * 10 + (o[j++] - '0');
            return n > 0 ? n * 100 : LendCents;
        }

        public static bool LoanDue(Loan l, long now)
        {
            return l != null && now >= l.DueAt;
        }

        /// Asked after the loan. `Message` carries the outcome: "early" (not three days yet: a little sore), "paid" (due and
        /// able: the money back, Cents below zero), "putoff" (due and short: another three days), "refused" (the greedy and
        /// the drifting, half the time: they deny it, and walk off).
        public static InteractAnswer Collect(string temper, bool woman, string regard, bool due, bool canPay, int cents, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            if (!due)
            {
                a.Message = "early";
                a.LikeDelta = EarlyLiking;
                a.Line = Pick(seed + "|ce", "It's not been three days, mister.", "You'll have it when I said. Not before.", "Give a body time. I said three days.");
                a.Thought = "Already asking after it. As if I'd run.";
                return a;
            }
            bool denies = (temper == "Greedy" || temper == "Drifter") && regard != "afraid" && Voice.Hash(seed + "|deny") % 100 < 50;
            if (denies)
            {
                a.Message = "refused";
                a.LikeDelta = RefusedLiking;
                a.Line = Pick(seed + "|cr", "What ten dollars? I don't recall any loan.", "Loan? You gave me that. I heard you say it.", "Wrong man, mister. Not a cent owing from me.");
                a.VoiceHate = 20;
                a.Thought = "He'll not see that money again. Careless, to lend it.";
                a.Then = "walk off";
                return a;
            }
            if (!canPay && regard != "afraid")
            {
                a.Message = "putoff";
                a.Line = Pick(seed + "|cp", "Haven't got it. Give me till Sunday, I swear.", "It's coming. I've had a bad week. Three days more.", "I know, I know. I've not forgotten. Soon.");
                a.Thought = "His ten dollars. Where am I to find it?";
                return a;
            }
            a.Message = "paid";
            a.Cents = -cents;
            a.Friendly = true;
            a.LikeDelta = PaidLiking;
            a.Line = regard == "afraid" ? "Here. All of it. We're square now, aren't we?" : temper == "Cold" ? "Here. Count it if you like." :
                     Pick(seed + "|c", "Here it is, every cent. I'm obliged to you.", "Here's your ten, on the day I named. Obliged to you for trusting me with it.", "Had it ready. Paying it back felt as good as getting it.");
            a.Thought = "Square with him. A weight off.";
            return a;
        }

        /// Introduced to someone near: taken by most, with a word for the newcomer; the hostile choose their own acquaintance;
        /// the cold allow it coolly. `Then` "introduced" when it takes.
        public static InteractAnswer Introduce(string temper, bool woman, string otherFirst, bool otherWoman, string regard, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            if (regard == "hostile")
            {
                a.Line = Pick(seed + "|ih", "I choose my own acquaintance, mister.", "I'll meet who I please. Not on your say-so.");
                a.VoiceHate = 30;
                return a;
            }
            a.Then = "introduced";
            a.LikeDelta = IntroduceLiking;
            a.Friendly = true;
            if (regard == "afraid")
                a.Line = "...Pleased, I'm sure, " + otherFirst + ".";
            else if (temper == "Cold")
                a.Line = Pick(seed + "|ic", "I know the face. " + otherFirst + ".", "We've nodded. " + otherFirst + ", then.");
            else if (temper == "Proud")
                a.Line = Pick(seed + "|ip", "Can't say I have. " + otherFirst + ". A pleasure, I'm sure.", otherFirst + ". I've heard the name.");
            else
                a.Line = Pick(seed + "|i", "Can't say I have. " + otherFirst + ", is it? Pleased to know you.", "Glad to. " + otherFirst + ". Any friend of this one's.", "Well now. " + otherFirst + ". We'll have to share a bottle some evening.");
            a.Thought = Pick(seed + "|it", "He made me known to " + otherFirst + ". A face I'd seen about. A name now.", "Introduced, like gentlefolk. " + otherFirst + ". I'll remember.");
            return a;
        }

        /// The newcomer's word back, in the caption.
        public static string Greets(string temper, bool woman, string toFirst, string seed)
        {
            if (temper == "Cold")
                return Pick(seed + "|gc", toFirst + ".", "Likewise.");
            if (woman)
                return Pick(seed + "|gw", "Charmed, I'm sure.", "Pleased to know you, " + toFirst + ".", "How do you do.");
            return Pick(seed + "|g", "Likewise.", "Pleased to know you, " + toFirst + ".", "How do.", toFirst + ". A pleasure.");
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            yield return AskAboutLabel("Cobb");
            yield return LendLabel();
            yield return CollectLabel();
            yield return IntroduceLabel("Cobb");
            string[] kinds = { "help", "round", "defend", "makeup", "flirt", "brawl", "debt", "cheat", "standoff", "rough", "argue", "smalltalk" };
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy", "Drifter" })
                foreach (string regard in new[] { null, "afraid", "hostile", "fond" })
                {
                    foreach (string kind in kinds)
                        foreach (int warmth in new[] { -2, 0, 2 })
                            foreach (string known in new[] { null, "killer", "thief", "brawler" })
                            {
                                var t = new SceneTie { A = "a", B = "b", AName = "Hiram Cobb", BName = "Clara Bell", Kind = kind, Warmth = warmth, Doer = kind == "rough" ? "b" : null, Courted = kind == "flirt" };
                                InteractAnswer x = AskAbout(t, "a", temper, regard, known, "s" + kind + warmth + known);
                                yield return x.Line;
                                if (x.Thought != null) yield return x.Thought;
                            }
                    foreach (bool worry in new[] { false, true })
                    {
                        InteractAnswer l = Lend(temper, false, regard, worry, "l" + temper + regard + worry);
                        yield return l.Line;
                        if (l.Thought != null) yield return l.Thought;
                    }
                    for (int s = 0; s < 4; s++)
                        foreach (bool due in new[] { false, true })
                            foreach (bool can in new[] { false, true })
                            {
                                InteractAnswer c = Collect(temper, false, regard, due, can, LendCents, "c" + temper + regard + s);
                                yield return c.Line;
                                if (c.Thought != null) yield return c.Thought;
                            }
                    InteractAnswer n = Introduce(temper, false, "Clara", true, regard, "i" + temper + regard);
                    yield return n.Line;
                    if (n.Thought != null) yield return n.Thought;
                    yield return Greets(temper, false, "Hiram", "g" + temper);
                    yield return Greets(temper, true, "Hiram", "h" + temper);
                }
        }
    }
}
