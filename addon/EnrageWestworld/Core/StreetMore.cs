// A street with more than fights (FOURTEENHOUR item 14; TWELVEHOUR item 7; [Scenes] StreetMore). Offline, a snap or a brawl was
// the strongest reason near the player in 83% of crowds (ticktest), and a street is mostly business, favours and small embarrassments;
// the fights mean more for being rarer than the rest. Three of the street's own, each with its variants by temper and an aftermath:
// - the horse trade at the hitching post: a man with horses to deal sells one; the buyer comes off fair, or worse and knows it
//   (the sharp dealer, the Greedy or the Cold, stings him more often): a grudge with a cause, the dealer its doer;
// - a tooth pulled in the street: a man whose bad tooth is at its worst, and a steady-handed man near him who offers to have it out
//   with pliers there and then (taken, or refused by the proud and the frightened); whoever saw it winces in their memory;
// - the way asked: a stranger to the town (a drummer, a drifter, someone with no home here) asks a resident the way, who answers by
//   his nature: shown the way, walked there by the kind, sent off short by the cold, or sent wrong for a joke by the greedy.
// Already there and left as they were (checked first): the haggle (a chance meeting), a debt called in (a link's x:debt), an apology
// over a grudge (Grudges), the times talked over. Their strengths sit under the brawls' (47 and up) and over a mild snap (42-45),
// so they take their share mostly from the snaps and the talk, not the fights. Plain C#, no game; all words this add-on's.
using System;

namespace EnrageWestworld.Core
{
    public static class StreetMore
    {
        public static bool On = true;   // [Scenes] StreetMore, from the settings
        public const int HorsePercent = 15, ToothPercent = 45, WayPercent = 25;
        public const int HorseStrength = 45, ToothStrength = 46, WayStrength = 44;

        static bool Deals(string group) { return group == "farm" || group == "trapper" || group == "rough" || group == "card"; }
        static bool Stranger(Someone x) { return x.P != null && (InnerLife.TradeGroup(x.P.Trade) == "drummer" || (x.P.Home == null && x.Temper == "Drifter")); }

        /// The best of the street's own for x and y (by day, out of doors), or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, bool night, bool twoMen)
        {
            if (!On || saloon || night || x == null || y == null || x.Id == y.Id || x.P == null)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            // a tooth at its worst, and a steady man beside him: he offers to have it out
            string kind;
            int stage;
            bool good;
            if (!x.Woman && !y.Woman && y.Temper != "Coward" && y.Temper != "Hothead" && InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) && kind == "tooth" && stage == 2 &&
                Voice.Chance(key + gameDay + "|tooth", ToothPercent))
                return new Reason { Kind = "toothpull", Strength = ToothStrength, BFirst = true, Why = x.Name + "'s tooth is at its worst, and " + y.Name + " has pliers and the nerve" };
            // a horse to sell at the hitching post
            if (twoMen && Deals(InnerLife.TradeGroup(x.P.Trade)) && !y.WellOff && Voice.Chance(key + gameDay + "|horse", HorsePercent))
                return new Reason { Kind = "horsetrade", Strength = HorseStrength, Why = x.Name + " has a horse to sell, and " + y.Name + " is looking" };
            // a stranger to the town asks the way
            if (Stranger(x) && y.P != null && y.P.Home != null && !Stranger(y) && Voice.Chance(key + gameDay + "|way", WayPercent))
                return new Reason { Kind = "wayasked", Strength = WayStrength, Why = x.Name + " is a stranger here and asks " + y.Name + " the way" };
            return null;
        }

        // ---- the horse trade: A sells, B buys; B comes off worse more often against a sharp dealer -------------------

        public static Scene HorseDeal(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string sellerTemper = c.Tempers != null ? c.Tempers[0] : null;
            bool sharp = sellerTemper == "Greedy" || sellerTemper == "Cold";
            bool deal = Voice.Chance(seed + "|deal", 70);
            bool stung = deal && Voice.Chance(seed + "|stung", sharp ? 70 : 30);
            string[][] horses =
            {
                new[] { "That bay's sound as a bell, " + b + ". Four years old, and never kicked a soul.", "Let me see her teeth.", "Go on, look. I've nothing to hide." },
                new[] { "Fifteen hands of Kentucky blood, this one.", "Kentucky by way of the knacker's yard?", "You won't find better between here and Saint Denis." },
                new[] { "A gelding, gentle as a parson. My wife rode him to church.", "And why's she not riding him now?", "She took up with the buggy. Pride, that's all." }
            };
            string[] h = horses[(Voice.Hash(seed + "|horse") & 0x7fffffff) % horses.Length];
            int ask = 40 + (Voice.Hash(seed + "|ask") & 0x7fffffff) % 5 * 5;
            Scene s = new Scene { Kind = stung ? "horsedeal" : "haggle", Doer = stung ? 0 : -1,
                                  Summary = stung ? "a horse sold at the hitching post, and the buyer came off the worse" : deal ? "a horse sold at the hitching post" : "a horse trade that came to nothing" };
            s.Add(0, h[0], "greet", "face").Add(1, h[1], "mock").Add(0, h[2], "mild")
             .Add(1, Grammar.UpperFirst(Voice.Number(ask - 15)) + " dollars.", "no").Add(0, Grammar.UpperFirst(Voice.Number(ask)) + ", and she's worth twice it.", "mild");
            if (!deal)
                s.Add(1, "Then sell her to a fool. Good day.", "storm", "walkaway");
            else if (stung)
                s.Add(1, Grammar.UpperFirst(Voice.Number(ask - 5)) + ". Done.", "yes").Add(0, "Done. You'll not regret it.", "yes")
                 .Add(1, Voice.Pick(seed + "|cough", "...Is she coughing? She's coughing.", "...Hold on. That near fore's lame.", "...Why's she blowing like that?"), "scared")
                 .Add(0, Voice.Pick(seed + "|gone", "Sold as seen, friend. Sold as seen.", "Right as rain when I had her. Afternoon.", "A horse is a horse. You looked at her teeth."), "mild", "walkaway");
            else
                s.Add(1, Grammar.UpperFirst(Voice.Number(ask - 5)) + ", and I'll stand you a drink.", "yes").Add(0, "Done, and done. Treat her kind.", "yes");
            return s;
        }

        // ---- a tooth pulled in the street: A pulls, B suffers it (or won't) ---------------------------------------

        public static Scene ToothPull(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string patient = c.Tempers != null ? c.Tempers[1] : null;
            bool refuses = (patient == "Proud" || patient == "Coward") && Voice.Chance(seed + "|refuse", 60);
            Scene s = new Scene { Kind = refuses ? "smalltalk" : "toothpull", Doer = refuses ? -1 : 0,
                                  Summary = refuses ? "a man with a bad tooth turned down a pair of pliers in the street" : "a man had a tooth pulled in the street" };
            s.Add(0, Voice.Pick(seed + "|offer", "You've been holding that jaw all morning, " + b + ". Let me have it out.", "That tooth again? Hold still. I've pliers in my coat.",
                                "Sit on the step, " + b + ". One pull and it's done with."), "mild", "face")
             .Add(1, Voice.Pick(seed + "|fear", "With those? In the street?", "The dentist's in Saint Denis...", "...It's not that bad. It's bad. Go on."), "scared");
            if (refuses)
                return s.Add(0, "Suit yourself. Suffer, then.", "mild").Add(1, "I'll see the dentist. Or die. One or the other.", "mild", "walkaway");
            s.Add(0, "Open wide. Think of something pleasant.", "mild")
             .Add(1, null, null, "hurt")
             .Add(1, Voice.Pick(seed + "|yell", "Aaagh! God in heaven!", "Mother of-! Is it out? Is it out?", "Hnngh! You near took my jaw with it!"), "angry")
             .Add(0, Voice.Pick(seed + "|out", "There. Look at the roots on that.", "Out, and all in one piece. Rinse it with whiskey.", "Keep it for luck. Or don't."), "mild")
             .Add(1, Voice.Pick(seed + "|thanks", "...That's a drink I'm owing you, " + a + ".", "...Lord in heaven. Obliged.", "Best thing anybody's done for me all week."), "thanks");
            return s;
        }

        // ---- the way asked: A, a stranger, asks; B answers by his nature ---------------------------------------------

        public static Scene WayAsked(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string town = c.Town ?? "town";
            string answers = c.Tempers != null ? c.Tempers[1] : null;
            string how = answers == "Cold" || answers == "Proud" ? "short" : answers == "Greedy" && Voice.Chance(seed + "|wrong", 50) ? "wrong" : answers == "Loyal" || answers == "Drifter" ? "walks" : "shows";
            string[] places = { "the post office", "the hotel", "the doctor's", "the livery", "the land office" };
            string place = places[(Voice.Hash(seed + "|place") & 0x7fffffff) % places.Length];
            Scene s = new Scene { Kind = how == "short" ? "brushoff" : how == "wrong" ? "smalltalk" : "wayshown", Doer = how == "short" || how == "wrong" ? -1 : 1,
                                  Summary = how == "short" ? "a stranger asked the way and got short shrift" : how == "wrong" ? "a stranger was sent the wrong way for a joke" : "a stranger was shown the way" };
            s.Add(0, Voice.Pick(seed + "|ask", "Beg pardon. Which way to " + place + "?", "I'm new to " + town + ". Where would I find " + place + "?", "Pardon me, friend. " + Grammar.UpperFirst(place) + "?"), "greet", "face");
            switch (how)
            {
                case "short": s.Add(1, Voice.Pick(seed + "|short", "Same place it's always been.", "Ask somebody who's paid to know.", "You've a mouth. Ask inside."), "no").Add(0, "...Much obliged, I'm sure.", "mild"); break;
                case "wrong": s.Add(1, "Straight on past the church, then left at the hog pen. Can't miss it.", "mild").Add(0, "Obliged to you.", "thanks"); break;
                case "walks": s.Add(1, "I'm going that way myself. Come on, I'll take you.", "mild").Add(0, "You're a Christian, sir.", "thanks").Add(1, null, null, "walkaway"); break;
                default: s.Add(1, Voice.Pick(seed + "|shows", "Down there, past the saloon, by the blue door.", "Other end of the street. You'll smell the livery first.", "Just there. You're nearly standing on it."), "mild").Add(0, "Obliged to you.", "thanks"); break;
            }
            return s;
        }
    }
}
