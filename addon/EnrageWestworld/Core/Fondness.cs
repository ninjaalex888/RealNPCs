// More ways to win people over (SIXDAY item 1; roadmap.md "Next: more NPC interactions that raise fondness", Lex,
// 2026-11-03, after the greeting's fondness: "for now until we add in more NPC interactions"). Through Talk, in a
// saloon, whiskey for the house ($2, the bar's price in 1899 for a dozen glasses): the room cheers and everyone treated
// thinks better of him, the drunk most of all; a gift (a pouch of tobacco, a tin of peaches: forty cents' worth), which
// the proud take where they'd refuse money, since a gift between equals carries no shame and charity does (the
// looking-glass self: to be given alms is to be seen as a beggar; to be given tobacco is to be seen as a friend);
// condolences to someone grieving a friend, the kindness people most notice and least often get (folk cross the street
// rather than say the dead one's name). And a kindness seen: those who watch him do right by someone else think better
// of him too, though they got nothing (indirect reciprocity: what a man does for a stranger is the surest sign of what
// he'd do for you). Each is written down as a happening the mind already understands (round, charity, comfort), so the
// thanks, the greetings, the trust with a worry and the allies' warmth all follow from what exists. Plain C#, no game;
// all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Fondness
    {
        public const int HouseCents = 200;        // whiskey for the house: two dollars (Lex's roadmap: $1-3; a dozen glasses at fifteen cents)
        public const int HouseLeast = 2;          // others within reach of the bar for it to be a round at all
        public const float HouseMetres = 6f;      // the bar, as the round scene counts it
        public const int HouseMost = 8;           // treated at most; a saloon of forty gets the first eight cheering and the rest the sound of it
        public const int HouseLiking = 8;         // enRAGE's fondness for each treated (a drink stood is 8 too)
        public const int HouseDrunkLiking = 12;   // ... the drunk fondest
        public const int HouseAffection = 3;      // our residents' affection for each treated (as a drink stood to you)
        public const int HouseDrunkAffection = 5;
        public const int GiftCents = 40;          // a pouch of tobacco or a tin of peaches, at the store's price
        public const string GiftOutcome = "a gift";   // the charity happening's outcome for a gift (Thanks reads it back as "gift")
        public const float SeenMetres = 12f;      // a kindness is a quiet thing: it's seen from near, not across a street
        public const int SeenAffection = 2;       // our residents' affection for a kindness seen done to someone else
        public const int CondoleDays = 14;        // a friend's death is fresh enough for condolences for two weeks

        /// A kindness of the player's that those who see it think better of him for: what the menu and the saving
        /// write (round, charity, help, comfort, apology, defend) and his kind deeds (mercy, peace, a thief caught, a man spared).
        public static bool IsKindness(string kind)
        {
            if (kind == null)
                return false;
            switch (kind)
            {
                case "round": case "charity": case "help": case "comfort": case "apology": case "defend":
                    return true;
            }
            return kind.StartsWith("deed:", StringComparison.Ordinal) && Minds.DeedGroup(kind.Substring(5)) == "deed:kindness";
        }

        /// What the witness saw him do, in a word, for the log.
        public static string SeenWord(string kind)
        {
            switch (kind)
            {
                case "round": return "stand a drink";
                case "charity": return "give";
                case "help": return "help";
                case "comfort": return "comfort";
                case "apology": return "apologise";
                case "defend": return "stand up for";
                default: return "do right by";
            }
        }

        // ---- whiskey for the house ----------------------------------------------------------------------

        public static string HouseLabel()
        {
            return "Whiskey for the house, on me ($" + HouseCents / 100 + ")";
        }

        /// The one he says it to answers for the room: by how they stand with him and their temper. Someone who hates
        /// him won't drink his whiskey (nor, they say, will their friends: nothing is poured); the devout don't take a
        /// glass but thank him for the house. The money goes when the answer comes, as the menu does it.
        public static InteractAnswer House(string temper, bool woman, bool devout, string regard, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            bool afraid = regard == "afraid", hostile = regard == "hostile";
            if (hostile)
            {
                a.Line = Pick(seed + "|hh", "I'll not drink your whiskey. Nor will anyone I call a friend.", "Keep your money and your whiskey. We know what you are.", "Buy the house a drink? Buy yourself a conscience.");
                a.VoiceHate = 40;
                a.Thought = "Thinks he can buy this room. Not while I'm in it.";
                return a;
            }
            a.Event = "round";
            a.Cents = HouseCents;
            a.LikeDelta = HouseLiking;
            a.Friendly = !afraid;
            if (afraid)
            {
                a.Line = Pick(seed + "|ha", "Y-yes. The house thanks you, mister. Barkeep!", "That's... that's good of you, mister. Very good. Barkeep, you heard him.", "The house. Yes. Thank you, mister. Thank you.");
                a.VoiceFear = 30;
                a.LikeDelta = 6;
                a.Thought = "Drink it. Smile. Don't let him see your hands shake.";
                return a;
            }
            if (devout)
            {
                a.Line = Pick(seed + "|hd", "Not for me, mister. But the house will thank you, I don't doubt.", "I don't take liquor. I'll not stand between the house and yours, though.");
                a.LikeDelta = 4;
                a.Thought = "Generous, in his way. The Lord sees the giving, not the whiskey.";
                return a;
            }
            switch (temper)
            {
                case "Hothead":
                    a.Line = Pick(seed + "|hhot", "Ha! Now there's a man! Barkeep, you heard him!", "The house! You hear that, boys? Drink the man's whiskey!", "That's the best thing anybody's said in here all week!");
                    a.LikeDelta = 10;
                    a.Thought = "A round for the house. Now there's a fellow I could ride with.";
                    break;
                case "Greedy":
                    a.Line = Pick(seed + "|hgr", "The house, you say? Then I'll have two.", "Whiskey for the house. Mine's a double, if the house is paying.", "Generous. I'll take mine first, before the house drinks you dry.");
                    a.Thought = "Free whiskey. Drink fast, before it runs out.";
                    break;
                case "Cold":
                    a.Line = Pick(seed + "|hco", "Generous. Or foolish. I'll drink it either way.", "If you're buying.", "Buying for a room of strangers. Your coin to waste.");
                    a.LikeDelta = 5;
                    a.Friendly = false;
                    a.Thought = "Buying the room. Everybody wants something. Still. Whiskey's whiskey.";
                    break;
                case "Proud":
                    a.Line = Pick(seed + "|hpr", "A gentleman's gesture. I'll drink to you, sir.", "Handsomely done. The house is obliged to you.", "A round for the house. There's few would. I'll raise a glass.");
                    a.Thought = "Does a thing handsomely, I'll say that for him.";
                    break;
                case "Coward":
                    a.Line = Pick(seed + "|hcw", "That's... that's very handsome, mister. The whole house!", "For everyone? Oh. Oh, that's kind. Barkeep!", "The whole house. Well. I never. Thank you, mister.");
                    a.Thought = "The whole house. Who does that? ...Him, it seems.";
                    break;
                default:
                    a.Line = woman ? Pick(seed + "|hw", "Drinks on the stranger! You heard him, boys!", "Well, that's the finest thing I've heard all day. Barkeep!", "The house! The man's buying, and I'll not let it go to waste.")
                                   : Pick(seed + "|hm", "Drinks on the stranger! You heard him, boys!", "Well, now. That's neighbourly. Barkeep, the man's buying!", "The house! You hear that? Line 'em up!");
                    a.Thought = Pick(seed + "|ht", "A round for the house. Who does that? ...Him, I suppose.", "Whole room's smiling. One man's two dollars did that.", "I'll remember the face that bought the house a drink.");
                    break;
            }
            return a;
        }

        /// What one of the room thinks, treated: the drunk warmest.
        public static string HouseThought(bool drunk, string temper, string seed)
        {
            if (drunk)
                return Pick(seed + "|hdr", "Whiskey on the stranger. Finest man in the territory, whoever he is.", "Free whiskey. I could kiss him. I won't. I might.", "Another on the house. On him, I mean. Bless him. Bless his hat.");
            switch (temper)
            {
                case "Cold": return "Free whiskey. Nobody gives for nothing. Drink it anyway.";
                case "Greedy": return "A round. Wonder if he's good for a second.";
                case "Proud": return "Didn't ask for it. Took it, though. A round's not charity.";
                case "Coward": return "He bought the whole house a drink. A man like that could be dangerous. Or kind.";
                default: return Pick(seed + "|hth", "Didn't pay for that one. The stranger did. Decent of him.", "A round from a stranger. Warm all the way down.", "Whole house drinking his whiskey. Good night, this.");
            }
        }

        // ---- a gift --------------------------------------------------------------------------------------

        /// Tobacco for a man, a tin of peaches for a woman (a respectable woman wasn't handed tobacco in 1899).
        public static string GiftLabel(bool woman)
        {
            return woman ? "I've a tin of peaches here. Take it, I've another." : "Here, I've a pouch of tobacco for you.";
        }

        public static string GiftWord(bool woman)
        {
            return woman ? "peaches" : "tobacco";
        }

        /// Their answer: the proud take a gift where they'd refuse money (no shame in it); the greedy would sooner the
        /// price of it; someone poor, whose money trouble he knows of, is fondest; someone who hates him takes nothing.
        public static InteractAnswer Gift(string temper, bool woman, string regard, bool poor, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            bool afraid = regard == "afraid", hostile = regard == "hostile";
            string thing = GiftWord(woman);
            if (hostile)
            {
                a.Line = Pick(seed + "|gh", "I want nothing of yours.", "Keep it. I'd not take water from you in a desert.", "Put it away. I've not forgotten what you did.");
                a.VoiceHate = 40;
                a.Thought = "Gifts, now. As if a tin could buy what he's done.";
                return a;
            }
            a.Event = "charity";
            a.Cents = GiftCents;
            a.Friendly = !afraid;
            a.LikeDelta = 10;
            if (afraid)
            {
                a.Line = Pick(seed + "|ga", "I... thank you, mister. Thank you.", "For me? Yes. Thank you, mister. That's kind.", "That's... good of you. I'll not forget it.");
                a.VoiceFear = 30;
                a.LikeDelta = 4;
                a.Thought = "Took it. Didn't dare not. What's he want for it?";
                return a;
            }
            if (poor)
            {
                a.Line = Pick(seed + "|gp", "You don't know what this is to me. Thank you.", "I'd not have asked. But I'll not refuse it. Bless you.", "Haven't had " + thing + " in a month. Thank you, mister.");
                a.LikeDelta = 14;
                a.Thought = Pick(seed + "|gpt", "Somebody noticed. Somebody gave, without my asking.", "A gift, when I'd nothing. I'll not forget his face.");
                return a;
            }
            switch (temper)
            {
                case "Proud":
                    a.Line = Pick(seed + "|gpr", "A gift? Well. A gift I can take. Obliged to you.", "Not charity, you say. Then I'll take it, and thank you.", "That's handsome. A man can accept a gift. I accept.");
                    a.Thought = "A gift's not charity. A man can take a gift from a man.";
                    break;
                case "Greedy":
                    a.Line = Pick(seed + "|ggr", thing == "tobacco" ? "Tobacco. I'd sooner the price of it. ...I'll take it, mind." : "Peaches. I'd sooner the price of them. ...I'll take them, mind.", "Is that all? ...Well. It's something.");
                    a.LikeDelta = 4;
                    a.Thought = "Could've been coin. Still. Didn't cost me anything.";
                    break;
                case "Cold":
                    a.Line = Pick(seed + "|gco", "...Thank you.", "What's this for? ...Thank you.", "I'll take it. I'll not ask why.");
                    a.LikeDelta = 6;
                    a.Friendly = false;
                    a.Thought = "A gift. From a stranger. Odd. Not unwelcome.";
                    break;
                case "Coward":
                    a.Line = Pick(seed + "|gcw", "For me? Oh. Oh, that's kind. Thank you.", "I... nobody gives me things. Thank you, mister.", "You're sure? Then... I'm obliged. Truly obliged.");
                    a.Thought = "He gave me something. Just gave it. Maybe he's not so frightening.";
                    break;
                case "Hothead":
                    a.Line = Pick(seed + "|ghot", "Ha! Hand it here, then.", "Now that's a proper thing to hand a man. Obliged!", "I'll take that, and gladly.");
                    a.Thought = "Hands a man " + thing + " without being asked. I like him.";
                    break;
                default:
                    a.Line = woman ? Pick(seed + "|gw", "Oh! That's good of you. Thank you kindly.", "Peaches. Well. I'll think of you when I open them.", "How kind. Thank you, mister.")
                                   : Pick(seed + "|gm", "That's good of you. Thank you kindly.", "Well, now. Obliged to you, mister.", "I'll take that gladly. Obliged.");
                    a.Thought = Pick(seed + "|gt", "A gift from a stranger. Folks don't do that.", "Didn't ask for it. Didn't expect it. Nice, that.", "I'll remember who gave me that.");
                    break;
            }
            return a;
        }

        // ---- condolences ---------------------------------------------------------------------------------

        public static string CondoleLabel(string deadFirst)
        {
            return "I'm sorry about " + (deadFirst ?? "your friend") + ".";
        }

        /// Their answer, grieving a friend: a kindness to the mind (comfort). Someone who knows he's the one who killed
        /// them, or who hates him, throws it back. Someone he killed whose death they don't lay at his door thanks him,
        /// and the player knows what they don't.
        public static InteractAnswer Condole(string temper, bool woman, string regard, bool youKilledAndTheyKnow, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            bool afraid = regard == "afraid", hostile = regard == "hostile";
            if (youKilledAndTheyKnow || hostile)
            {
                a.Line = youKilledAndTheyKnow ? Pick(seed + "|ck", "You. You've the gall to say that to me.", "Sorry? You? Take yourself off before I forget myself.", "Don't you say that name. Not you.")
                                             : Pick(seed + "|ch", "I'll take no comfort from you.", "Keep your sorrow. I've enough of my own.", "You? Sorry? Say it to somebody who'll believe it.");
                a.VoiceHate = 60;
                a.Thought = youKilledAndTheyKnow ? "He stands there and says he's sorry. Sorry." : "His kind are never sorry. Not truly.";
                return a;
            }
            a.Event = "comfort";
            a.LikeDelta = 8;
            a.Friendly = true;
            if (afraid)
            {
                a.Line = Pick(seed + "|ca", "Th-thank you, mister.", "That's... kind, mister. Thank you.", "Yes. Thank you. It's... thank you.");
                a.VoiceFear = 40;
                a.LikeDelta = 2;
                a.Friendly = false;
                a.Thought = "Even him. Even him, sorry. I don't know what to think.";
                return a;
            }
            switch (temper)
            {
                case "Cold":
                    a.Line = Pick(seed + "|cco", "People die. Thank you for saying it.", "It's done. Still. Decent of you to say it.", "Yes. Well. Thank you.");
                    a.LikeDelta = 2;
                    a.Friendly = false;
                    a.Thought = "Said the name. Nobody says the name any more.";
                    break;
                case "Proud":
                    a.Line = Pick(seed + "|cpr", "I'll bear it. But it's kind of you to say so.", "We bear what we must. Kind of you to say it, all the same.", "Kind of you. I'll not make a show of it. But kind.");
                    a.LikeDelta = 6;
                    a.Thought = "Didn't let him see it. He saw it anyway, I think.";
                    break;
                case "Coward":
                    a.Line = Pick(seed + "|ccw", "I can't... I can't speak of it. But thank you.", "Thank you. I... thank you. Forgive me.", "Oh. Oh, that's... thank you, mister.");
                    a.LikeDelta = 6;
                    a.Thought = "Kind words. I near wept in the street. Near.";
                    break;
                case "Hothead":
                    a.Line = Pick(seed + "|chot", "Yeah. Well. Obliged. I'll leave it there.", "...Obliged. Not many have said it.", "That's... Damn it. Thank you.");
                    a.Thought = "Thought I'd be angry if anyone said it. Wasn't. Not with him.";
                    break;
                default:
                    a.Line = woman ? Pick(seed + "|cw", "Thank you. Folk cross the street rather than say it.", "That's kind. Most don't know what to say, so they say nothing.", "Thank you, mister. It helps, hearing the name said.")
                                   : Pick(seed + "|cm", "Thank you. Not many have said it.", "Obliged. It helps, a little, hearing it said.", "Thank you. Folk look at their boots, mostly.");
                    a.Thought = Pick(seed + "|ct", "He said the name. Nobody says the name any more.", "A stranger said he was sorry. The neighbours haven't.", "Kind. Didn't expect kind from him.");
                    break;
            }
            return a;
        }

        // ---- a debt covered in the street ----------------------------------------------------------------

        public const int CoverLiking = 10;        // the creditor, paid: a little fonder (he got his money; it's not his kindness)
        public const int CoverDebtorLiking = 30;  // the debtor: as the menu's settled debt
        public const int CoverPaidAffection = 4, CoverDebtorAffection = 15;

        public static string CoverLabel(int dollars)
        {
            return "I'll cover what he owes you ($" + dollars + ").";
        }

        /// The list for a creditor pressing someone in the street: the one thing to say to him, and never mind.
        public static List<InteractOption> CoverOptions(int cash, int dollars)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "cover", Label = CoverLabel(dollars), Why = cash < dollars * 100 ? "not enough money" : null },
                new InteractOption { Id = "bye", Label = "Never mind." }
            };
        }

        /// The creditor's answer, paid by a stranger: by his temper (he's not refusing money that's owed him, whoever
        /// holds it out; the proud want it known it was owed, the greedy count it twice). Event none: it's the debtor's
        /// kindness, written by the game side; Cents the sum, taken when the answer comes.
        public static InteractAnswer Cover(string temper, int dollars, string seed)
        {
            var a = new InteractAnswer { Then = "stay", Cents = dollars * 100, Friendly = true };
            switch (temper)
            {
                case "Greedy":
                    a.Line = Pick(seed + "|cvg", "Your money's as good as his. Better, likely. We're square, then.", "Count it out, then. ...All there. He's a lucky man to have a friend like you.");
                    a.Thought = "A stranger pays another man's debts. There's more where that came from, I'd wager.";
                    break;
                case "Cold":
                    a.Line = Pick(seed + "|cvc", "If you say so. It's your money to throw away.", "Paid is paid. Who pays makes no odds to me.");
                    a.Friendly = false;
                    a.Thought = "Paid, then. By a fool, but paid.";
                    break;
                case "Proud":
                    a.Line = Pick(seed + "|cvp", "It was owed, mind. Fair and square. But I'll take it, and we're done.", "I'm not a hard man. I only wanted what was mine. Obliged to you.");
                    a.Thought = "Let the street see I was in the right. The money's beside the point. Mostly.";
                    break;
                case "Hothead":
                    a.Line = Pick(seed + "|cvh", "Well, I'll be. Fine. Fine! We're square, him and me.", "Huh. Didn't see that coming. All right. He's paid.");
                    a.Thought = "Had my fist half made. Stranger took the wind out of it.";
                    break;
                default:
                    a.Line = Pick(seed + "|cv", "Then we're square. Obliged, mister.", "Well. That settles it, then. I'll say good day.", "I'll take it, and I'll say this: he's lucky in his friends.");
                    a.Thought = Pick(seed + "|cvt", "Paid, and no hard feelings. Best way.", "A stranger paid it. Never saw the like.");
                    break;
            }
            return a;
        }

        /// The debtor's thanks, said to the stranger as the creditor walks off (a scene beat: Scenes.DebtCovered).
        public static string CoverThanks(string temper, bool woman, string seed)
        {
            switch (temper)
            {
                case "Proud": return woman ? "I'll see every cent of that back to you, mister. Every cent." : "That's a loan, you understand. I'll see it back to you. But... thank you.";
                case "Cold": return "Why would you do a thing like that? ...Thank you.";
                case "Coward": return "Mister, I... I don't know what to say. I thought he'd take the mule. Thank you.";
                case "Greedy": return "Just like that? Well, I won't argue. Thank you kindly.";
                default: return Pick(seed + "|cvth", "Heaven keep you, mister. Heaven keep you.", "I don't know you. You don't know me. And you did that. Thank you.", "I'll not forget this. Not if I live to ninety.");
            }
        }

        // ---- walking someone home ---------------------------------------------------------------------------

        public const float EscortMinMetres = 60f, EscortMaxMetres = 100f;   // their door, as far as the street knows: a spot this far off (Lex's roadmap: 60-100 m)
        public const float EscortLagMetres = 8f;        // further behind than this and they stop and wait
        public const int EscortLagMs = 5000, EscortWaitMs = 20000;   // behind this long and they stop; waited this long and they go on alone
        public const int EscortLiking = 20, EscortAffection = 10, EscortGaveUpAffection = 2;   // seen home; the thought that counted when you didn't finish
        public const string EscortOutcome = "saw them home";   // the help happening's outcome (Thanks reads it back as "escort")

        /// Whether "Let me see you home." is offered: after dark in a town, to a woman, someone in drink or a timid man
        /// (a Coward), who neither hates nor fears him, and isn't sat somewhere.
        public static bool EscortOffered(bool night, bool town, bool woman, bool drunk, string temper, string regard, bool seated)
        {
            if (!night || !town || seated || regard == "hostile" || regard == "afraid")
                return false;
            return woman || drunk || temper == "Coward";
        }

        public static string EscortLabel()
        {
            return "Let me see you home.";
        }

        /// Their answer. A proud man knows the way to his own door; a woman who neither knows his name nor has had a
        /// kindness from him, and isn't fond of him, won't walk out with a stranger half the time (she'd be talked of);
        /// the drunk always, the timid gladly, the cold with a shrug. Yes is Then "escort": the walk is the game side's.
        public static InteractAnswer Escort(string temper, bool woman, bool drunk, string regard, bool knowsName, bool kindLately, string seed)
        {
            var a = new InteractAnswer { Then = "stay", Friendly = true };
            bool fond = regard == "fond";
            if (temper == "Proud" && !woman && !drunk)
            {
                a.Line = Pick(seed + "|epr", "I know the way to my own door, thank you.", "I've walked it forty years, drunk and sober. I'll manage.");
                a.Thought = "Does he take me for an old woman?";
                return a;
            }
            if (woman && !drunk && !fond && !knowsName && !kindLately && Voice.Chance(seed + "|ew", 50))
            {
                a.Line = Pick(seed + "|ewn", "That's kind, but I don't walk out with men I don't know.", "No, thank you. I'd sooner not be talked about.");
                a.Thought = "A stranger, offering to see me home. What they'd say.";
                return a;
            }
            a.Then = "escort";
            if (drunk)
            {
                a.Line = Pick(seed + "|ed", "Would you? Would you, now. Lead on, friend. Lead on.", "Home. Yes. Which way is it? ...You know the way. Good.", "You're a good man. A good, good man. It's this way. Or that way.");
                a.Thought = "Good man. Good man. Where's my hat.";
            }
            else if (temper == "Cold" && !woman)
            {
                a.Line = "...If you like.";
                a.Friendly = false;
                a.Thought = "Company. Even his. The street's no place to be alone tonight.";
            }
            else if (woman)
            {
                a.Line = Pick(seed + "|ewy", "Would you? I'd be glad of it. It's not far.", "Thank you. I don't care for the street after dark.", "That's kind. Yes. It's just along here.");
                a.Thought = "He asked properly. I'll walk with him, and keep my distance.";
            }
            else
            {
                a.Line = Pick(seed + "|em", "I'd not say no. The street's no place to be alone tonight.", "Decent of you, mister. It isn't far.");
                a.Thought = "Safer with company. Even a stranger's.";
            }
            return a;
        }

        /// At the door, to his face.
        public static string EscortThanks(string temper, bool woman, bool drunk, string seed)
        {
            if (drunk)
                return Pick(seed + "|eth", "This is me. Or near enough. You're a good man. I'll remember you. Prob'ly.", "Home! Look at that. Thank you, friend. Thank you.");
            switch (temper)
            {
                case "Cold": return "This is my door. ...Thank you.";
                case "Coward": return "This is me. I don't know what I'd have done, alone. Thank you, mister.";
                case "Proud": return woman ? "This is my door. It was kind of you, and I'll not forget it." : "My door. I'd have managed. But I'm obliged.";
                default: return woman ? Pick(seed + "|etw", "This is me. Thank you, mister. I'll sleep better for it.", "Here's my door. That was kind. Good night, now.")
                                      : Pick(seed + "|etm", "This is me. Obliged to you, mister. Good night.", "Here's my door. Good of you to walk it with me.");
            }
        }

        public static string EscortGaveUp(string seed)
        {
            return Pick(seed + "|egu", "Never mind, mister. I'll go on alone.", "I'll manage the rest myself. Good night.");
        }

        public static string EscortGaveUpThought(string seed)
        {
            return Pick(seed + "|egt", "Offered to see me home, then wandered off. Men.", "Half way and he's gone. Should have known.");
        }

        /// What they think once they're in.
        public static string EscortThought(bool woman, bool drunk, string seed)
        {
            if (drunk)
                return "Walked home by a stranger. Can't recall his face. Recall he was kind.";
            return woman ? Pick(seed + "|ewt", "Saw me to the door and asked nothing. A gentleman, whatever they say.", "Walked me home and kept his distance. I'll think well of him for it.")
                         : Pick(seed + "|emt", "Walked me home like I was somebody. Decent of him.", "Safe in, thanks to him. The street was black as pitch.");
        }

        // ---- carrying a word ----------------------------------------------------------------------------------

        public const long ErrandDays = 2;            // the word lapses after two game days
        public const int ErrandLiking = 12, ErrandAffection = 6, ErrandSentBackAffection = 3;   // each of the two, once it's carried (less for the one who sent it back)
        public const string ErrandOutcome = "carried a word";   // the help happening's outcome, for the sender

        /// Whom they'd send a word to: the latest sore tie of theirs (a quarrel, not a theft) with a named person of the
        /// same town who's alive, or sweethearts fallen out, within the week. Null when nobody.
        public static SceneTie ErrandTie(List<SceneTie> ties, string id, string town, long now, Func<string, bool> alive)
        {
            if (ties == null || id == null)
                return null;
            SceneTie best = null;
            foreach (SceneTie t in ties)
            {
                if (t == null || !t.Has(id) || t.Kind == "lift" || t.Kind == "pickpocket" || t.Kind == "mugging" || t.Kind == "holdup" || t.Kind == "makeup" || now - t.At < 0 || now - t.At >= 7 * 1440L)
                    continue;   // a quarrel mended (by a carried word, or their own making up) isn't a quarrel (the review: a tie at -3 mended to -1 was offered again)
                if (!(t.Warmth < 0 || (t.Courted && t.Warmth <= 0)))
                    continue;
                string other = t.A == id ? t.B : t.A;
                if (other == null || other == "player" || string.IsNullOrEmpty(t.Other(id)) || (town != null && t.Town != null && t.Town != town) || (alive != null && !alive(other)))
                    continue;
                if (best == null || t.At > best.At)
                    best = t;
            }
            return best;
        }

        public static string ErrandLabel(string otherFirst)
        {
            return "Any word you'd have carried to " + otherFirst + "?";
        }

        /// Their answer: a word to carry (Then "errand", the Message in your mouth: "He says he's sorry for what he said."),
        /// or none from the cold, the afraid or the hostile. Pride can't say sorry to a face; a go-between carries it.
        public static InteractAnswer ErrandAsk(string temper, bool woman, string regard, string otherFirst, bool otherWoman, bool sweethearts, string seed)
        {
            var a = new InteractAnswer { Then = "stay", Friendly = true };
            string he = woman ? "She" : "He", him = otherWoman ? "her" : "him";
            if (regard == "hostile")
            {
                a.Line = Pick(seed + "|erh", "Nothing I'd trust to you.", "My business is my own. Keep out of it.");
                a.VoiceHate = 30;
                a.Thought = "Carry my word? He'd carry it to the whole street.";
                return a;
            }
            if (regard == "afraid")
            {
                a.Line = "N-no. Nothing, mister. Nothing at all.";
                a.VoiceFear = 40;
                a.Friendly = false;
                a.Thought = "Say nothing. Give him nothing to carry.";
                return a;
            }
            if (temper == "Cold")
            {
                a.Line = Pick(seed + "|erc", "Nothing I'd trust to a stranger.", "What's between " + him + " and me stays there.");
                a.Friendly = false;
                a.Thought = "A stranger offering to carry my quarrels. No.";
                return a;
            }
            a.Then = "errand";
            if (sweethearts)
            {
                a.Line = woman ? Pick(seed + "|ersw", "Tell him I'm sorry. Tell him I didn't mean a word of it.", "Would you? Tell him... tell him I miss him. There.")
                               : Pick(seed + "|ersm", "Tell her I was a fool, and I'm sorry.", "Tell her I'd take it all back if I could. Every word.");
                a.Message = woman ? he + " says she's sorry, and didn't mean a word of it." : he + " says he was a fool, and he's sorry.";
                a.Thought = "A stranger carrying my heart across town. Better than it sitting here.";
                return a;
            }
            switch (temper)
            {
                case "Proud":
                    a.Line = Pick(seed + "|erp", "Tell " + him + " I was in the wrong. I'll not say it to " + (otherWoman ? "her" : "his") + " face. ...Tell " + him + ".", "You may tell " + him + " the fault was mine. From you, not from me.");
                    a.Message = he + " says the fault was " + (woman ? "hers" : "his") + ".";
                    a.Thought = "Said it to a stranger. Couldn't say it to " + him + ". Well.";
                    break;
                case "Hothead":
                    a.Line = Pick(seed + "|erho", "Tell " + him + " " + (otherWoman ? "she's" : "he's") + " a fool. And that I'll stand " + him + " a drink Saturday.", "Tell " + him + " I'm still sore. And that it's done with. Both.");
                    a.Message = he + " says you're a fool, and " + (woman ? "she'll" : "he'll") + " stand you a drink Saturday.";
                    a.Thought = "Couldn't say it myself. Would've ended in fists.";
                    break;
                case "Coward":
                    a.Line = Pick(seed + "|erco", "Would you? Tell " + him + " I'm sorry. I can't... I can't go myself.", "Oh, would you? Tell " + him + " I never meant it. Please.");
                    a.Message = he + " says " + (woman ? "she's" : "he's") + " sorry, and never meant it.";
                    a.Thought = "He'll carry it. Thank the Lord. I'd never have dared.";
                    break;
                default:
                    a.Line = Pick(seed + "|er", "Tell " + him + " I'm sorry for what I said. There. Said it.", "If you see " + him + ", say I'd sooner be friends. I can't seem to say it myself.");
                    a.Message = Voice.Hash(seed + "|erm") % 2 == 0 ? he + " says " + (woman ? "she's" : "he's") + " sorry for what " + (woman ? "she" : "he") + " said." : he + " says " + (woman ? "she'd" : "he'd") + " sooner be friends.";
                    a.Thought = "A stranger carrying my word. Better than carrying it myself.";
                    break;
            }
            return a;
        }

        public static string DeliverLabel(string fromFirst, string message)
        {
            return fromFirst + " sent word. " + message;
        }

        /// The one it's for answers: the quarrel mended (Then "makeup") or the word sent back; either way a kindness to
        /// them (Event comfort: a word carried in peace), and they think better of you for carrying it.
        public static InteractAnswer Deliver(string temper, bool woman, string fromFirst, bool fromWoman, bool sweethearts, string seed)
        {
            var a = new InteractAnswer { Then = "stay", Event = "comfort", LikeDelta = 10, Friendly = true };
            string him = fromWoman ? "her" : "him", he = fromWoman ? "she" : "he";
            if (sweethearts)
            {
                a.Line = Pick(seed + "|dsw", (fromWoman ? "She" : "He") + " said that? ...Tell " + him + " I'm sorry too.", "Did " + he + "? ...Then tell " + him + " to come and say it. I'll be waiting.");
                a.Then = "makeup";
                a.LikeDelta = 12;
                a.Thought = "Sent word by a stranger. Couldn't come " + (fromWoman ? "herself" : "himself") + ". That's " + fromFirst + " all over. I'll forgive it.";
                return a;
            }
            switch (temper)
            {
                case "Proud":
                    a.Line = Pick(seed + "|dpr", "Then let " + him + " come and say it " + (fromWoman ? "herself" : "himself") + ".", "A word by a stranger. " + (fromWoman ? "She" : "He") + " knows where I live.");
                    a.LikeDelta = 4;
                    a.Thought = "Sorry by proxy. It's something. It's not enough.";
                    break;
                case "Hothead":
                    a.Line = Pick(seed + "|dho", "Ha! Took " + him + " long enough. ...Tell " + him + " it's forgotten.", "Did " + he + " now? Well. Tell " + him + " I'll hold " + him + " to that drink.");
                    a.Then = "makeup";
                    a.Thought = "Forgotten. Mostly. Fair enough.";
                    break;
                case "Cold":
                    a.Line = Pick(seed + "|dco", "...I'll think on it.", "Noted. Thank you for carrying it.");
                    a.Friendly = false;
                    a.LikeDelta = 4;
                    a.Thought = "Words carried by a stranger. We'll see.";
                    break;
                case "Coward":
                    a.Line = Pick(seed + "|dcw", (fromWoman ? "She" : "He") + " said that? Oh. Oh, heaven be praised.", "I feared " + he + " hated me. Tell " + him + " it's all right. Tell " + him + "!");
                    a.Then = "makeup";
                    a.Thought = "It's mended. I could weep. I won't. I might.";
                    break;
                default:
                    a.Line = Pick(seed + "|d", "Well. That's handsome of " + him + ". Tell " + him + " we're square.", "Did " + he + " say so? Then it's done with. Obliged to you for carrying it.");
                    a.Then = "makeup";
                    a.Thought = Pick(seed + "|dt", "Mended, and by a stranger's going between. Odd. Good.", "Square with " + fromFirst + " again. Sleep easier for it.");
                    break;
            }
            return a;
        }

        public static string ErrandLapsedThought(string seed)
        {
            return Pick(seed + "|erl", "Gave the stranger my word to carry. He never did.", "Trusted a stranger with my sorry. Should have gone myself.");
        }

        // ---- tipping the performer -----------------------------------------------------------------------------

        public const int TipCents = 25;
        public const string TipOutcome = "a coin for the music";   // the charity happening's outcome (Thanks reads it back as "tip")

        /// The game's own scenario types that make their user a performer worth a coin: the piano, the guitar (and its
        /// hand-off, pick-up, rest, put-down and wander), the banjo (the same), the fiddle bench. By hash, as the game
        /// names the type someone is using (the review: the mind's own scenario list knows only the piano).
        public static readonly string[] PerformerNames =
        {
            "PROP_HUMAN_PIANO", "WORLD_HUMAN_GUITAR", "WORLD_HUMAN_GUITAR_HANDOFF", "WORLD_HUMAN_GUITAR_PICKUP", "WORLD_HUMAN_GUITAR_RESTING", "WORLD_HUMAN_GUITAR_PUTDOWN", "WORLD_HUMAN_GUITAR_WANDER",
            "PROP_HUMAN_SEAT_CHAIR_BANJO", "PROP_HUMAN_SEAT_CHAIR_BANJO_HANDOFF", "PROP_HUMAN_SEAT_CHAIR_BANJO_RESTING", "WORLD_HUMAN_BANJO_PICKUP", "WORLD_HUMAN_BANJO_PUTDOWN", "WORLD_HUMAN_BANJO_WANDER",
            "PROP_HUMAN_SEAT_BENCH_FIDDLE", "PROP_HUMAN_SEAT_BENCH_FIDDLE_UPBEAT"
        };
        private static Dictionary<uint, string> _performers;

        /// The performer's scenario name for this type hash, or null for anyone else's. No allocation after the first call.
        public static string PerformerScenario(uint hash)
        {
            if (_performers == null)
            {
                var d = new Dictionary<uint, string>();
                foreach (string n in PerformerNames)
                    d[MindScenarios.Joaat(n)] = n;
                _performers = d;
            }
            string s;
            return _performers.TryGetValue(hash, out s) ? s : null;
        }

        public static bool Performer(uint hash)
        {
            return PerformerScenario(hash) != null;
        }

        /// Whether this scenario makes its user a performer worth a coin, by name (the tests; the game asks by hash).
        public static bool Performer(string scenario)
        {
            if (string.IsNullOrEmpty(scenario))
                return false;
            string s = scenario.ToLowerInvariant();
            return s.StartsWith("prop_human_piano") || s.StartsWith("world_human_guitar") || s.StartsWith("prop_human_seat_chair_banjo") || s.StartsWith("world_human_banjo") || s.StartsWith("prop_human_seat_bench_fiddle");
        }

        public static string PerformerName(string scenario)
        {
            string s = (scenario ?? "").ToLowerInvariant();
            return s.Contains("piano") ? "The piano player" : s.Contains("banjo") ? "The banjo player" : s.Contains("fiddle") ? "The fiddler" : "The guitar player";
        }

        public static string TipLabel()
        {
            return "For the music ($0.25)";
        }

        /// The list for a performer: a coin for the music, and nothing else.
        public static List<InteractOption> TipOptions(int cash)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "tip", Label = TipLabel(), Why = cash < TipCents ? "not enough money" : null },
                new InteractOption { Id = "bye", Label = "Never mind." }
            };
        }

        /// Small, but they remember the face.
        public static InteractAnswer Tip(string seed)
        {
            return new InteractAnswer
            {
                Then = "stay", Event = "charity", Cents = TipCents, LikeDelta = 6, Friendly = true,
                Line = Pick(seed + "|tip", "Obliged, mister.", "Much obliged. Any requests?", "Kind of you. I'll play one for you."),
                Thought = Pick(seed + "|tipt", "A coin in the hat. Remember that face. He pays.", "Somebody listening after all.")
            };
        }

        // ---- a fire shared ----------------------------------------------------------------------------------

        public const int CampfireAffection = 10;   // the men at a fire you sat at: fonder of you, and a memory (Engine.CampfireShared)

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            yield return CoverLabel(15);
            yield return EscortLabel();
            foreach (InteractOption o in CoverOptions(1000, 15))
                yield return o.Label;
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                for (int i = 0; i < 4; i++)
                {
                    InteractAnswer es = Escort(temper, i % 2 == 1, i == 2, i == 3 ? "fond" : null, i == 1, false, "es" + i);
                    yield return es.Line;
                    yield return es.Thought;
                    yield return EscortThanks(temper, i % 2 == 1, i == 2, "et" + i);
                    yield return EscortThought(i % 2 == 1, i == 2, "eo" + i);
                }
            yield return EscortGaveUp("x");
            yield return EscortGaveUp("y");
            yield return EscortGaveUpThought("x");
            yield return EscortGaveUpThought("y");
            yield return ErrandLabel("Abner");
            yield return TipLabel();
            foreach (InteractOption o in TipOptions(100))
                yield return o.Label;
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                foreach (string regard in new[] { null, "afraid", "hostile" })
                    for (int i = 0; i < 4; i++)
                    {
                        InteractAnswer er = ErrandAsk(temper, i % 2 == 1, regard, "Abner", i == 2, i == 3, "er" + i);
                        yield return er.Line;
                        yield return er.Thought;
                        if (er.Message != null)
                        {
                            yield return er.Message;
                            yield return DeliverLabel("Hiram", er.Message);
                        }
                        InteractAnswer de = Deliver(temper, i % 2 == 1, "Hiram", i == 2, i == 3, "de" + i);
                        yield return de.Line;
                        yield return de.Thought;
                    }
            for (int i = 0; i < 4; i++)
            {
                yield return ErrandLapsedThought("l" + i);
                InteractAnswer tp = Tip("t" + i);
                yield return tp.Line;
                yield return tp.Thought;
            }
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                for (int i = 0; i < 3; i++)
                {
                    InteractAnswer cv = Cover(temper, 10 + i * 5, "cv" + i);
                    yield return cv.Line;
                    yield return cv.Thought;
                    yield return CoverThanks(temper, i == 1, "th" + i);
                }
            yield return HouseLabel();
            yield return GiftLabel(false);
            yield return GiftLabel(true);
            yield return CondoleLabel("Abner");
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                foreach (string regard in new[] { null, "afraid", "hostile", "fond" })
                    for (int i = 0; i < 4; i++)
                        foreach (bool w in new[] { false, true })
                        {
                            InteractAnswer h = House(temper, w, i == 3, regard, "h" + i);
                            if (h.Line != null) yield return h.Line;
                            if (h.Thought != null) yield return h.Thought;
                            InteractAnswer g = Gift(temper, w, regard, i == 2, "g" + i);
                            if (g.Line != null) yield return g.Line;
                            if (g.Thought != null) yield return g.Thought;
                            InteractAnswer c = Condole(temper, w, regard, i == 1, "c" + i);
                            if (c.Line != null) yield return c.Line;
                            if (c.Thought != null) yield return c.Thought;
                            yield return HouseThought(i == 0, temper, "t" + i);
                        }
        }
    }
}
