// The interaction menu (NIGHT3 item 4; roadmap 6; the design in roadmap.md "What the player can do"): locked on to someone
// RealNPCs knows, a "Talk" entry in the game's lock-on menu opens a short list of things Arthur can say, and the answer
// comes from who they are and how they already stand with him. Four to start: the news (their own worry, if they'd
// trust him with it, else the rumours of his own deeds, in their words), a drink stood (a saloon), money given, a
// warning. Each one is written down as a happening the mind already understands (a round and charity read as kindness,
// a warning as a challenge to the one warned), so gratitude, fear, the greetings as he passes, the rumours and the
// memories all follow from what exists. A debt they've told him of he can settle (roadmap 9: the one thing saved, the
// day he did, so the rest of its run reads as ended well). The same thing done to the same person again within the
// game hour is said but not counted (Again). Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class InteractOption
    {
        public string Id;        // news, name, ask, drink, give, settle, sorry, threaten, bye
        public string Label;     // what Arthur says, shown in the list
        public string Why;       // why it can't be chosen now (shown greyed), or null
    }

    public sealed class InteractAnswer
    {
        public string Line;      // what they say back, in their voice
        public string Event;     // the happening it is (round, charity, help, comfort, apology, threat, faced), or null for none
        public int Cents;        // what it cost Arthur
        public int LikeDelta;    // for enRAGE's own townsfolk: its LikeMore, through its own method
        public string Then;      // what they do after: stay, step back, square up, walk off
        public string Thought;   // what they think of it, over their head
        public bool Confided;    // they told him their worry: he knows of it now
        public int VoiceHate, VoiceFear;   // how it's said, for the game's own lines (Speech.For)
        public bool Friendly;
        public bool Settles;     // he paid their debt: the worry is over (Engine.SettleWorry)
        public bool LearnsName;  // he told them his name: they know it from now on (Engine.LearnName)
        public bool Loan;        // the debt he settled they took as a loan, and said so: they'll bring it back (Thanks)
    }

    public static class Interact
    {
        public const int DrinkCents = 10;   // two beers at a nickel, 1899
        public const int AgainMinutes = 60; // the same thing to the same person within the game hour: said, not counted again
        public const int KindMinutes = 1440; // a kindness from him within the game day: enough to trust him with a worry
        public const int SettleCents = 1100; // the debt's eleven dollars (the worry's own lines name the sum)
        public const long SorryMinutes = 3 * 1440; // a wrong of his within three game days can be apologised for

        /// The list for this person now: saloon (a drink can be stood), cash (cents in Arthur's pocket), money worry (one
        /// he knows of: a gift is five dollars, not one), a debt he knows of and hasn't settled (he can pay it off), and
        /// his whole name when they don't know it yet (he can tell them), the worry they've told him of (he can ask after
        /// it: AskLabel), and a wrong of his lately they're owed an apology for.
        public static List<InteractOption> Options(bool saloon, int cash, bool moneyWorry, bool debtKnown = false, string tellName = null, string askAbout = null, bool sorry = false)
        {
            int gift = moneyWorry ? 500 : 100;
            var list = new List<InteractOption> { new InteractOption { Id = "news", Label = "What's the news?" } };
            if (!string.IsNullOrEmpty(tellName))
                list.Add(new InteractOption { Id = "name", Label = "The name's " + tellName + "." });
            string ask = AskLabel(askAbout);
            if (ask != null)
                list.Add(new InteractOption { Id = "ask", Label = ask });
            list.Add(new InteractOption { Id = "drink", Label = "Let me stand you a drink.", Why = !saloon ? "only in a saloon" : cash < DrinkCents ? "not enough money" : null });
            list.Add(new InteractOption { Id = "give", Label = "Here, take this ($" + gift / 100 + ")", Why = cash < gift ? "not enough money" : null });
            if (debtKnown)
                list.Add(new InteractOption { Id = "settle", Label = "Let me settle that debt ($" + SettleCents / 100 + ")", Why = cash < SettleCents ? "not enough money" : null });
            if (sorry)
                list.Add(new InteractOption { Id = "sorry", Label = "I'm sorry about before." });
            list.Add(new InteractOption { Id = "threaten", Label = "Watch yourself." });
            list.Add(new InteractOption { Id = "bye", Label = "Goodbye." });
            return list;
        }

        /// How he asks after a worry they told him of, or null where it can't be asked after aloud (a secret, a poster).
        public static string AskLabel(string worry)
        {
            switch (worry)
            {
                case "debt": return "How's that debt of yours?";
                case "fever": return "How's the little one?";
                case "letter": return "Any word in the post?";
                case "drought": return "Any rain out your way?";
                case "son": return "Heard from your boy?";
                case "courting": return "Spoken up yet, to your sweetheart?";
                case "feud": return "That neighbour of yours behaving?";
                case "bank": return "How are things with the bank?";
                case "drink": return "Keeping off the bottle?";
                case "cards": return "Still at the cards?";
                case "savings": return "Much put by for that ticket?";
                case "tooth": return "How's the tooth?";
                default: return null;
            }
        }

        /// Their answer when he asks after their worry: how it stands today in their own words (InnerLife.WorrySaid), and
        /// being asked is a kindness (a comfort, for the mind), the more to someone who thinks nobody listens.
        public static InteractAnswer AskedAfter(string worryNow, string temper, string seed)
        {
            var a = new InteractAnswer { Then = "stay", Event = "comfort", LikeDelta = 4, Friendly = temper != "Cold" };
            a.Line = !string.IsNullOrEmpty(worryNow) ? worryNow : Pick(seed + "|ak", "Same as ever. But thank you for asking.", "No better, no worse. Kind of you to ask.");
            a.Thought = temper == "Cold" ? "Why should he care? ...Still. He asked." : Pick(seed + "|akt", "Nobody asks. He asked.", "Remembered, did he. Fancy that.", "Didn't think he'd been listening.");
            return a;
        }

        /// Their answer to his apology for a wrong of his lately (a warning, a slight, a blow): by how they stand with him
        /// and their temper; written down as an apology (a kindness to the mind), less from someone who hates him.
        public static InteractAnswer Sorry(string temper, string regard, string seed)
        {
            var a = new InteractAnswer { Then = "stay", Event = "apology", LikeDelta = 6, Friendly = true };
            if (regard == "afraid")
            {
                a.Line = "It's forgotten, mister. All forgotten.";
                a.Friendly = false;
                a.VoiceFear = 30;
                a.Thought = "Says he's sorry. I'll still not turn my back on him.";
                return a;
            }
            if (regard == "hostile")
            {
                a.Line = "Sorry doesn't mend it.";
                a.Friendly = false;
                a.LikeDelta = 2;
                a.VoiceHate = 40;
                a.Thought = "Let him be sorry. I'll still remember.";
                return a;
            }
            switch (temper)
            {
                case "Proud":
                    a.Line = "An apology. Well. I'll take it, this once.";
                    a.Thought = "At least he knows he was wrong.";
                    break;
                case "Hothead":
                    a.Line = "Sorry, is it? ...Fine. We're square.";
                    a.Thought = "Wanted to hit him. Glad I didn't.";
                    break;
                case "Cold":
                    a.Line = "Saying it costs you nothing, mister.";
                    a.Friendly = false;
                    a.LikeDelta = 2;
                    a.Thought = "Means nothing. Still, he said it.";
                    break;
                case "Coward":
                    a.Line = "Oh! It's... it's all right. Really it is.";
                    a.Thought = "He's sorry? He's sorry. Thank the Lord.";
                    break;
                default:
                    a.Line = Pick(seed + "|so", "Well. That's handsome of you. We'll say no more of it.", "I've had worse said to me. But I'm obliged.", "Kind of you to say so. It's forgotten.");
                    a.Thought = Pick(seed + "|sot", "Didn't think he had it in him.", "A man who says sorry. Rare as rain.", "Well. Maybe he's not so bad.");
                    break;
            }
            return a;
        }

        /// The latest wrong he did them within SorryMinutes (a warning, a slight, a blow, a theft: Thanks.Wrong) that he
        /// hasn't apologised for since, or null.
        public static Happening LastWrong(List<Happening> log, string id, long now)
        {
            if (log == null || id == null)
                return null;
            long sorryAt = -1;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e == null || e.TargetId != id || e.DoerId != "player" || e.At > now || now - e.At >= SorryMinutes)
                    continue;
                if (e.Kind == "apology")
                    sorryAt = Math.Max(sorryAt, e.At);
                else if (Thanks.Wrong(e) && e.At > sorryAt)
                {
                    bool since = false;   // an apology after this wrong, further on in the log
                    for (int j = i + 1; j < log.Count; j++)
                        if (log[j] != null && log[j].Kind == "apology" && log[j].DoerId == "player" && log[j].TargetId == id && log[j].At >= e.At)
                            since = true;
                    if (!since)
                        return e;
                }
            }
            return null;
        }

        /// Whether they'd tell him their own worry: only someone fond of him, or someone he's lately been kind to (a drink,
        /// money, a good deed done them); a poster or a secret, or a Cold one's worry, only to someone they're fond of.
        /// Never when they fear or hate him. Asking twice isn't trust.
        public static bool Confides(string regard, bool kindLately, string worry, string temper)
        {
            if (worry == null || regard == "afraid" || regard == "hostile")
                return false;
            bool fond = regard == "fond";
            if ((worry == "law" || worry == "secret" || temper == "Cold") && !fond)
                return false;
            return fond || kindLately;
        }

        /// Their answer: temper (Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter or null), devout (no drink), regard
        /// (how they stand with him already: afraid, hostile, fond or null), the rumour to pass on (Rumours.Version, about
        /// Arthur's deeds) or null, their worry's kind (InnerLife, while it lasts) or null, whether they'd tell him it
        /// (Confides), a money worry he knows of (the gift is five dollars), and a seed; for his name, his surname (you) and
        /// their whole name (self), which the friendly men give back.
        public static InteractAnswer Answer(string option, string temper, bool woman, bool devout, string regard, string rumour, string worry, bool confides, bool moneyWorry, string seed, string you = null, string self = null)
        {
            var a = new InteractAnswer { Then = "stay" };
            bool afraid = regard == "afraid", hostile = regard == "hostile";
            switch (option)
            {
                case "news":
                    if (afraid)
                    {
                        a.Line = Pick(seed + "|naf", "Me? I hear nothing, mister. Never have.", "Nothing. Nothing at all, I swear it.");
                        a.VoiceFear = 60;
                        a.Thought = "Tell him nothing. Just get him gone.";
                    }
                    else if (hostile)
                    {
                        a.Line = "Not a word, not for the likes of you.";
                        a.VoiceHate = 40;
                    }
                    else if (worry != null && confides)
                    {
                        a.Line = WorryLine(worry, seed);
                        a.Confided = true;
                        a.Thought = Pick(seed + "|wt", "Didn't mean to say so much.", "Feels better, saying it out loud.", "Why'd I tell a stranger that?");
                    }
                    else if (!string.IsNullOrEmpty(rumour))
                    {
                        string r = rumour.Trim().TrimEnd('.');
                        a.Line = Pick(seed + "|news", Grammar.UpperFirst(r) + ", so they say.", "Folks are saying " + r + ".", "Heard tell " + r + ". Make of it what you will.");
                        a.Thought = Pick(seed + "|nt", "Asking me what folk say about him. Odd.", "Does he know it's him they're talking of?", "Best tell him straight and be done.");
                    }
                    else
                        a.Line = temper == "Cold" || temper == "Greedy" ? Pick(seed + "|cold", "Nothing I'd tell a stranger.", "News costs, mister.")
                               : Pick(seed + "|none", "Quiet enough. Nothing worth the telling.", "Same as ever, mister.", "Not much happens here that you'd care about.");
                    return a;
                case "drink":
                    if (hostile)
                    {
                        a.Line = "I'll buy my own, thank you.";
                        a.VoiceHate = 30;
                        return a;
                    }
                    if (devout)
                    {
                        a.Line = Pick(seed + "|dry", "Not a drop, thank you kindly.", "I don't take liquor, mister. But thank you.");
                        a.Thought = "Kindly meant, I suppose.";
                        return a;
                    }
                    a.Event = "round";
                    a.Cents = DrinkCents;
                    a.LikeDelta = 8;
                    a.Friendly = !afraid;
                    a.Line = afraid ? "Sure... sure. Much obliged, mister."
                           : temper == "Cold" ? "If you're buying." : temper == "Hothead" ? "Now you're talking!" : Pick(seed + "|drink", "Well, I won't say no.", "Well, that's neighbourly.", "I'll take a whiskey, then.");
                    a.Thought = afraid ? "Drink it and smile. Don't cross him." : Pick(seed + "|dt", "A drink from a stranger. Can't complain.", "Free whiskey's the best kind.", "What's he want for it, I wonder.");
                    return a;
                case "give":
                    if (hostile)
                    {
                        a.Line = "Keep it. I want nothing that's been yours.";
                        a.VoiceHate = 40;
                        return a;
                    }
                    if (temper == "Proud" && !afraid)
                    {
                        a.Line = woman ? "Keep your money, mister. I manage." : "Keep your money. I ain't a beggar.";
                        a.Then = "walk off";
                        a.Event = "faced";   // money held out to the proud lands as a slight (the review)
                        a.VoiceHate = 20;
                        a.Thought = "Charity. From him. As if I couldn't manage.";
                        return a;
                    }
                    a.Event = "charity";
                    a.Cents = moneyWorry ? 500 : 100;
                    a.LikeDelta = moneyWorry ? 20 : 10;
                    a.Friendly = !afraid;
                    a.Line = afraid ? "That's... good of you, mister. Very good."
                           : moneyWorry ? Pick(seed + "|mw", "Bless you. That keeps the wolf from the door a while.", "That's... that's the rent. Thank you.")
                           : temper == "Greedy" ? "A dollar? Is that the whole of it?"
                           : Pick(seed + "|give", "Well, I'm obliged to you.", "Well, I'll not refuse it. Obliged.", "What's this for? ...Thank you.");
                    a.Thought = afraid ? "What does he want for it? What will he want?" : moneyWorry ? "Somebody's looking out for me after all." : temper == "Greedy" ? "A soft touch. I'll remember his face." : "Money for nothing. Folks don't do that.";
                    return a;
                case "name":
                    return Introduced(temper, regard, woman, you ?? "Smith", woman ? null : self, seed);
                case "settle":
                    if (hostile)
                    {
                        a.Line = "I'd sooner owe the devil than owe you.";
                        a.VoiceHate = 40;
                        return a;
                    }
                    a.Event = "help";
                    a.Cents = SettleCents;
                    a.LikeDelta = 30;
                    a.Settles = true;
                    a.Friendly = !afraid;
                    switch (afraid ? "afraid" : temper)
                    {
                        case "afraid":
                            a.Line = "You... you'd do that? I won't forget it, mister.";
                            a.Thought = "Paid it, just like that. What will he want for it?";
                            break;
                        case "Proud":
                            a.Line = woman ? "Then it's a loan, and I'll see every cent of it back to you." : "Then it's a loan. Every cent back, you have my word on it.";
                            a.Loan = true;
                            a.Thought = "Beholden to a stranger. I'll make it right if it takes a year.";
                            break;
                        case "Greedy":
                            a.Line = "Eleven dollars, just like that? Well, I won't argue with you.";
                            a.Thought = "Wonder what else he'd pay for, a man like that.";
                            break;
                        case "Cold":
                            a.Line = "Why would you do a thing like that?";
                            a.Thought = "Nobody pays another man's debts for nothing. Still. It's paid.";
                            break;
                        default:
                            a.Line = Pick(seed + "|set", "You'd do that? For me? Lord love you, mister.", "Eleven dollars... I can pay him. I can pay him today!", "I hardly know what to say. I'm in your debt now, not his.");
                            a.Thought = Pick(seed + "|sett", "Square today. I can breathe again.", "A man I hardly know did that. Wait till I tell them at home.", "Can't think why he'd do it. Won't ask, either.");
                            break;
                    }
                    return a;
                case "threaten":
                    a.Event = "threat";   // a warning, not a gunfight: a challenge to the one warned, quiet to the street (the review)
                    a.LikeDelta = -15;
                    if (afraid)
                    {
                        a.Line = "Steady, mister. I never meant to cross you.";
                        a.Then = "step back";
                        a.VoiceFear = 70;
                        a.Thought = "He'll do it. I know he'll do it.";
                        return a;
                    }
                    switch (temper)
                    {
                        case "Coward":
                            a.Line = "Easy, easy! I never meant you any harm.";
                            a.Then = "step back";
                            a.VoiceFear = 60;
                            a.Thought = "Don't look at him. Just don't.";
                            break;
                        case "Hothead":
                            a.Line = woman ? "Is that a threat? I've faced down meaner." : "You threatening me? Go on, then.";
                            a.Then = "square up";
                            a.VoiceHate = 50;
                            a.Thought = "Let him try. Just let him try.";
                            break;
                        case "Proud":
                            a.Line = "I'll remember that, mister.";
                            a.Then = "walk off";
                            a.VoiceHate = 40;
                            a.Thought = "He'll answer for that tone one day.";
                            break;
                        case "Cold":
                            a.Line = Pick(seed + "|thc", "Noted.", "Big man.");
                            a.VoiceHate = 20;
                            a.Thought = "His kind don't make old bones.";
                            break;
                        default:
                            a.Line = Pick(seed + "|th", "Easy now. Easy.", "I didn't mean nothing by it.");
                            a.Then = "step back";
                            a.VoiceFear = 40;
                            a.Thought = "What did I do? What did I do to him?";
                            break;
                    }
                    return a;
                default:
                    a.Line = afraid ? "Yes, mister. Goodbye." : hostile ? "Good riddance." : Pick(seed + "|bye", "Take care, now.", "Go careful, now.", "So long, then.");
                    if (hostile)
                        a.VoiceHate = 20;
                    return a;
            }
        }

        /// He tells them his name: they know it from now on, whatever they make of him; the friendly give theirs back.
        private static InteractAnswer Introduced(string temper, string regard, bool woman, string you, string self, string seed)
        {
            // `you`: his surname. In 1899 a stranger told "Arthur Morgan" says "Mr. Morgan", a man among men "Morgan"; a
            // woman doesn't give a strange man her first name, and the friendly man gives his whole one back (the review)
            string mister = "Mr. " + you;
            var a = new InteractAnswer { Then = "stay", LearnsName = true };
            if (regard == "afraid")
            {
                a.Line = mister + ". Yes. I'll... I'll remember it.";
                a.VoiceFear = 40;
                a.Thought = "His name. Now I'll hear it in my sleep.";
                return a;
            }
            if (regard == "hostile")
            {
                a.Line = "I know well enough who you are.";
                a.VoiceHate = 30;
                a.Thought = "As if I'd forget it.";
                return a;
            }
            a.Friendly = true;
            switch (temper)
            {
                case "Cold":
                    a.Line = (woman ? mister : you) + ".";
                    a.Friendly = false;
                    a.Thought = "A name. Means nothing to me. Yet.";
                    break;
                case "Proud":
                    a.Line = woman ? mister + ". I shall remember it." : self != null ? self + ", and pleased to make your acquaintance, " + mister + "." : "Then I'll know you when I see you, " + mister + ".";
                    a.Thought = "Has manners, at least. More than most.";
                    break;
                case "Greedy":
                    a.Line = woman ? mister + ". And what is it you want?" : you + ". A man with a name and a full purse, I hope.";
                    a.Thought = "Names come cheap. What's he carrying?";
                    break;
                case "Hothead":
                    a.Line = woman ? mister + ", is it? Well." : you + ", is it? Well. Now we know each other.";
                    a.Thought = "Wants me to know his name. Fine. I know it.";
                    break;
                case "Coward":
                    a.Line = "P-pleased to know you, " + mister + ".";
                    a.Thought = "Why'd he tell me? What's he want me to do with it?";
                    break;
                default:
                    a.Line = woman ? Pick(seed + "|nm", "Pleased to know you, " + mister + ".", mister + ". I'll remember it.")
                                   : Pick(seed + "|nm", self != null ? self + ". Pleased to know you, " + you + "." : "Pleased to know you, " + mister + ".", you + ", is it? Good to put a name to a face.", "I'll remember that, " + mister + ".");
                    a.Thought = Pick(seed + "|nmt", you + ". Decent enough, for a man with that many guns.", you + ". Now I'll know who folk mean.", you + ". Plain name. Hard face.");
                    break;
            }
            return a;
        }

        /// Whether he did this same thing to this same person within the game hour (the world log has it): said, not
        /// counted again, so a drink stood twice over doesn't buy twice the liking (habituation; the review).
        public static bool Again(List<Happening> log, string id, string kind, long now)
        {
            if (log == null || id == null || kind == null)
                return false;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e != null && e.Kind == kind && e.DoerId == "player" && e.TargetId == id && e.At <= now && now - e.At < AgainMinutes)
                    return true;
            }
            return false;
        }

        /// Whether he's done them a kindness within the game day (a drink, money, a good deed the log has them the one it
        /// was done to): enough for some to trust him with a worry (Confides).
        public static bool KindLately(List<Happening> log, string id, long now)
        {
            if (log == null || id == null)
                return false;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e == null || e.DoerId != "player" || e.TargetId != id || e.At > now || now - e.At >= KindMinutes)
                    continue;
                if (e.Kind == "round" || e.Kind == "charity" || e.Kind == "help" || (e.Kind != null && e.Kind.StartsWith("deed:", StringComparison.Ordinal) && Minds.DeedGroup(e.Kind.Substring(5)) == "deed:kindness"))
                    return true;
            }
            return false;
        }

        /// Their worry, said aloud the way someone says it to a stranger they've warmed to.
        public static string WorryLine(string worry, string seed)
        {
            switch (worry)
            {
                case "tooth": return "This tooth's been killing me all week.";
                case "debt": return Pick(seed + "|debt", "I owe eleven dollars I don't have, and the man wants it Saturday.", "There's a debt hanging over me. Can't sleep for it.");
                case "drought": return "Five weeks and not a drop of rain. The corn's standing there dying.";
                case "cards": return "Lost more at cards than I can cover, and the rent's due.";
                case "fever": return "My little one's had a fever three days now.";
                case "feud": return "My neighbour's moved the fence line again. It'll come to blows.";
                case "letter": return "Waiting on a letter that never comes.";
                case "drink": return "Been drinking more than I ought. You'd think I'd learn.";
                case "secret": return "Got something on my mind I can't tell a soul.";
                case "courting": return "There's someone I can't stop thinking on. Can't find the words to tell them.";
                case "savings": return "Trying to put money by for a ticket west. It isn't easy.";
                case "son": return "My boy went off and I've heard nothing since.";
                case "bank": return "The bank's after the land. I don't know what we'll do.";
                case "law": return "There's a poster I'd rather nobody looked at too close.";
                default: return "Got my troubles, same as anyone.";
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                foreach (string regard in new[] { null, "afraid", "hostile", "fond" })
                    foreach (string opt in new[] { "news", "name", "drink", "give", "settle", "threaten", "bye" })
                        for (int i = 0; i < 6; i++)
                            foreach (bool w in new[] { false, true })
                            {
                                InteractAnswer a = Answer(opt, temper, w, i == 5, regard, i % 2 == 0 ? "a man called Morgan shot a ranch hand" : null, i == 3 ? "debt" : null, i == 3, i == 4, "all" + i, "Morgan", i % 2 == 0 ? "Hiram Cobb" : null);
                                if (a.Line != null) yield return a.Line;
                                if (a.Thought != null) yield return a.Thought;
                            }
            foreach (string k in new[] { "tooth", "debt", "fever", "feud", "letter", "drink", "secret", "courting", "savings", "son", "bank", "law", "drought", "cards", "other" })
            {
                yield return WorryLine(k, k);
                if (AskLabel(k) != null)
                    yield return AskLabel(k);
            }
            foreach (string t in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                for (int i = 0; i < 6; i++)
                {
                    InteractAnswer ak = AskedAfter(i % 2 == 0 ? null : "Counted the jar.", t, "ak" + i);
                    yield return ak.Line;
                    yield return ak.Thought;
                    foreach (string r in new[] { null, "afraid", "hostile", "fond" })
                    {
                        InteractAnswer so = Sorry(t, r, "so" + i);
                        yield return so.Line;
                        yield return so.Thought;
                    }
                }
            yield return "I'm sorry about before.";
        }
    }
}
