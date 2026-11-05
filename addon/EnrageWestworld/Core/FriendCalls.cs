// Friends come to you (Run 3, item 7; Lex's /goal: "Friendly NPCs without Talk: friends come to you (warnings, news, small
// gifts) and favours by trade"). A friend (the friend dot's rule: enRAGE's fondness at AlliesLiking or more, a kindness of
// yours remembered, a trouble you mended, someone you saved) who sees you stopped in their town walks over now and then,
// unasked, with one of four things:
//   a warning: someone in town mourns one you killed or is still hot with anger at you, or the law has been asking after a
//              deed of yours here (named only if the friend would know who);
//   the news:  the almanac's freshest story, told as theirs;
//   a gift:    a few cents, a handful of cartridges for the gun you carry, or something to eat (by their means and trade);
//   a favour of their trade: the doctor looks you over (your health back), the gunsmith tops up your rounds, the barkeep
//              stands you a drink (your stamina), the store, the butcher, the hotel or a farm something for the road.
// Words, a walk and a small hand-over only: no Talk row, verb or answer (Talk is frozen, 2026-11-04). The game side is
// Game/WestworldScript.FriendCalls.cs ([Scenes] FriendCalls, FriendGifts). Plain C#; mindtest FriendCallsTest.
//
// Why it reads as friendship (the behavioural psychologist's lens, Lex's brief): people who like us seek us out unprompted,
// with information (a warning, the news) and small gifts; it is the reciprocity of the kindness you did them coming back
// in their own coin, and the unasked part is what makes it feel real rather than a menu's answer. A favour of their trade
// is the oldest way a town shows you're one of its own: the doctor won't take your money, the barkeep pours one free.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class FriendCalls
    {
        public const string Warn = "warn", News = "news", Gift = "gift", Trade = "trade";
        public const int LookMs = 50000;        // a look for a friend every 50 s (x the storyteller's) in a town
        public const float Metres = 25f;        // a friend in view within this comes over
        public const int RoundsGiven = 12;      // cartridges in a gift or the gunsmith's favour
        public const int CoreGiven = 40;        // points of a core (of 100) a meal or a drink gives back

        /// What a friend can do for you by their trade, or null: "patch" (a doctor), "rounds" (a gunsmith, a trapper, a
        /// lawman, a soldier), "drink" (a barkeep), "meal" (a store, a butcher, a hotel, a farm). `trade` is their trade's
        /// words ("the gunsmith", "a farmhand"); `group` its group (InnerLife.TradeGroup).
        public static string Favour(string trade, string group)
        {
            string t = (trade ?? "").ToLowerInvariant();
            if (t.Contains("doctor"))
                return "patch";
            if (t.Contains("gunsmith") || group == "trapper" || group == "law" || group == "soldier")
                return "rounds";
            if (t.Contains("barkeep") || t.Contains("bartender") || t.Contains("saloon keeper"))
                return "drink";
            if (t.Contains("store") || t.Contains("butcher") || t.Contains("hotel") || t.Contains("cook") || group == "farm")
                return "meal";
            return null;
        }

        /// Which of the four they come with: weighed by what there is (a warning first when there is one), seeded so the same
        /// friend on the same day comes with the same thing. Null when they have nothing at all (no gifts allowed, nothing else).
        public static string Which(bool warning, bool news, string favour, bool gifts, string seed)
        {
            var kinds = new List<KeyValuePair<string, int>>();
            if (warning)
                kinds.Add(new KeyValuePair<string, int>(Warn, 45));
            if (news)
                kinds.Add(new KeyValuePair<string, int>(News, 30));
            if (favour != null && gifts)
                kinds.Add(new KeyValuePair<string, int>(Trade, 30));
            if (gifts)
                kinds.Add(new KeyValuePair<string, int>(Gift, 20));
            int total = kinds.Sum(k => k.Value);
            if (total == 0)
                return null;
            int roll = Voice.Hash(seed + "|friendcall") % total;
            foreach (KeyValuePair<string, int> k in kinds)
            {
                if (roll < k.Value)
                    return k.Key;
                roll -= k.Value;
            }
            return kinds[0].Key;
        }

        /// The gift itself: "coin" (cents by their means), "rounds" (those who carry a gun), "meal" (those who'd have bread to
        /// hand). Seeded.
        public static string GiftOf(string group, bool woman, string seed)
        {
            bool guns = group == "trapper" || group == "rough" || group == "law" || group == "soldier" || (!woman && group == "farm");
            int r = Voice.Hash(seed + "|gift") % 100;
            if (guns && r < 40)
                return "rounds";
            if ((group == "farm" || group == "town" || group == "old" || woman) && r < 75)
                return "meal";
            return "coin";
        }

        /// The cents a coin gift is, by their means: the gentry a dollar, the rest less.
        public static int CentsOf(string group, string seed)
        {
            int[] by = group == "gentry" ? new[] { 100, 100, 150 } : group == "card" ? new[] { 50, 100 } : group == "working" || group == "girl" || group == "plain" ? new[] { 25, 25, 50 } : new[] { 25, 50, 50 };
            return by[Voice.Hash(seed + "|cents") % by.Length];
        }

        // ---- the words: {you} how they call him, {who} the danger, {news} the news in their words, {town} -------------------------

        private static readonly string[] WarnNamed =
        {
            "Watch yourself, {you}. {who} has been asking where you put up.",
            "A word, {you}. {who} hasn't forgiven you. I'd not turn my back in this town if I were you.",
            "Keep clear of {who} a while. There's been drinking, and your name said loud.",
            "{you}. Don't take it wrong, but {who} has a grievance and a gun. Mind them both.",
            "Thought you ought to hear it from a friend. {who} is still sore with you, sore enough to do something about it.",
            "I'd walk the long way round tonight, {you}. {who} was in the street asking after you.",
        };
        private static readonly string[] WarnNameless =
        {
            "Somebody in {town} has your name in his mouth, {you}, and not kindly. I'd keep my eyes open.",
            "A word, {you}. There's a man in town grieving, and he blames you. I won't say who.",
            "Watch your back here a day or two. There's bad feeling about you, and I've heard it more than once.",
            "Don't stand with your back to the door tonight, {you}. There's talk.",
            "I'd not linger in {town} after dark, friend. There's somebody here who wants a word with you, and not with words.",
        };
        private static readonly string[] WarnLaw =
        {
            "There was a lawman at the store asking after a man of your look, {you}. I said I'd never seen you.",
            "The marshal's been about, asking questions. Thought you ought to know before you heard it from him.",
            "Keep your hat low a day or two, {you}. The law's taken an interest in what happened here.",
            "A deputy asked me straight out if I knew you. I said I knew a good many people. Mind how you go.",
        };
        private static readonly string[] NewsOpen =
        {
            "{you}! Have you heard? {news}",
            "Thought you'd want to know. {news}",
            "There's news, {you}. {news}",
            "Did you see the paper? {news}",
            "Here's a thing, {you}. {news}",
        };
        private static readonly string[] GiftCoin =
        {
            "Here, {you}. Don't argue. You were good to me when it counted.",
            "Take it. It ain't much, but it's honest, and it's yours.",
            "For your trouble, back when. I don't forget a kindness.",
            "Put that in your pocket and don't make a speech about it.",
            "I came into a little. Some of it is yours by rights.",
        };
        private static readonly string[] GiftRounds =
        {
            "Found these in the bottom of a drawer. They'll do more good in your belt than mine.",
            "Here, a handful of cartridges. A man in your line runs short.",
            "You looked light on rounds. Take these. Don't thank me.",
            "Had a box I'll never shoot. Better you have them.",
        };
        private static readonly string[] GiftMeal =
        {
            "Baked this morning. Take a piece for the road. You look like you've been living on coffee.",
            "Here's bread and a bit of ham. Eat it, don't save it.",
            "Brought you something warm. I knew you'd be passing.",
            "My wife said if I saw you I was to make you eat. So eat.",
        };
        private static readonly string[] TradePatch =
        {
            "Hold still, {you}. That wants looking at before it festers. No charge.",
            "Sit down a minute. I'll see to that. Don't reach for your purse.",
            "You're walking like a man with something cracked. Let me look. Friends don't pay.",
            "Here, let me. I'd sooner patch you now than dig a bullet out later.",
        };
        private static readonly string[] TradeRounds =
        {
            "Your belt's light, {you}. A box of rounds, on the house this once.",
            "Had a box going spare at the shop. They fit what you carry.",
            "Don't you go out there half loaded. Here. My compliments.",
            "Saw you checking your gun. Take these. Pay me in better weather.",
        };
        private static readonly string[] TradeDrink =
        {
            "Next one's on me, {you}. Sit a minute.",
            "Here. A short one, on the house. You look like you've earned it.",
            "Put your money away. This one's from a friend.",
            "This one costs you nothing. Keep it quiet at the bar, mind.",
        };
        private static readonly string[] TradeMeal =
        {
            "Take this for the road, {you}. Don't argue, I've more than I can sell.",
            "Here. Something to eat that ain't jerky. Friends' prices, which is nothing.",
            "Saved you a bite. I had a feeling you'd be through.",
            "Wrapped you some of the good stuff. No charge. Eat it hot.",
        };

        /// What they say as they come up. kind: Warn ("named", "nameless", "law" in detail), News (the news line in detail),
        /// Gift (the gift: "coin", "rounds", "meal"), Trade (the favour: "patch", "rounds", "drink", "meal").
        public static string Line(string kind, string detail, string you, string danger, string news, string town, string seed)
        {
            string[] pool;
            switch (kind)
            {
                case Warn:
                    pool = detail == "law" ? WarnLaw : danger != null ? WarnNamed : WarnNameless;
                    break;
                case News:
                    pool = NewsOpen;
                    break;
                case Gift:
                    pool = detail == "rounds" ? GiftRounds : detail == "meal" ? GiftMeal : GiftCoin;
                    break;
                case Trade:
                    pool = detail == "patch" ? TradePatch : detail == "rounds" ? TradeRounds : detail == "drink" ? TradeDrink : TradeMeal;
                    break;
                default:
                    return null;
            }
            string line = Voice.Pick(seed + "|fc|" + kind + "|" + detail, pool);
            line = line.Replace("{you}", you ?? "friend").Replace("{who}", danger ?? "somebody").Replace("{news}", news ?? "").Replace("{town}", town ?? "town");
            return Grammar.UpperFirst(line.Trim());
        }

        /// The friend's own thought as they go back to their day.
        public static string After(string kind, string seed)
        {
            return Voice.Pick(seed + "|fca|" + kind,
                kind == Warn ? new[] { "Said my piece. What he does with it is his business.", "Hope I wasn't too late with that.", "If anything happens to him now, it won't be for want of telling." }
              : kind == News ? new[] { "He didn't know. Glad I was the one to tell him.", "Good to have somebody to tell things to.", "He took it well. Better than I did." }
              : new[] { "Felt good, that. Ought to do it more.", "He'd have done the same. More, likely.", "Owed him that and more. A start, anyway." });
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return WarnNamed.Concat(WarnNameless).Concat(WarnLaw).Concat(NewsOpen).Concat(GiftCoin).Concat(GiftRounds).Concat(GiftMeal)
                            .Concat(TradePatch).Concat(TradeRounds).Concat(TradeDrink).Concat(TradeMeal);
        }
    }
}
