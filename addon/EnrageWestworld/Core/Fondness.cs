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

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
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
