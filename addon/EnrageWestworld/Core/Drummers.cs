// Drummers on the train (FIVEDAY reserve R1). The travelling salesman of 1899, the "drummer" (he drummed up trade), came in
// on the train with a sample case and a firm's name from a far city, put up at the hotel for a night or two, called on every
// storekeeper in town, and left on the next train for the next town. Here: one man in three who steps down off a train is a
// drummer (the rest are newcomers or kin, as before). He's a resident for two game days, never a regular; his trade is his
// line of goods ("a drummer in hardware"); he thinks a drummer's thoughts (InnerLife's "drummer" group); and when you're near
// the store, he walks over to its keeper with his pitch, and the keeper orders, haggles or sends him off by his manner. Then
// he's gone on the train. The leave day lives in Store.Counts ("drummer:<id>"): nothing new in the save. Behind
// [World] Drummers (with TrainTime).
//
// Why it reads true: the drummer was the small town's news from outside, its fashions and its jokes, and its first taste of
// the national market: the storekeeper's order from him decided what the town could buy for a season. He lived on rejection
// (most calls ended without an order) and on charm, which shows in his thoughts. Plain C#, no game; all words original.
// mindtest DrummersTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Drummers
    {
        public const int Chance = 35;              // percent of men stepping down who are drummers
        public const int StayDays = 2;
        public const string TradePrefix = "a drummer in ";
        public const string CountPrefix = "drummer:";
        public const float CallFromMetres = 80f;   // the drummer this near the store's keeper (and the keeper within 60 m of you)
        public const float AtCounter = 2.6f;

        public static readonly string[] Wares = { "hardware", "dry goods", "cigars", "notions", "patent stoves", "barbed wire", "sewing machines", "canned goods" };

        public static bool Comes(string seed) { return Voice.Chance(seed + "|drummer", Chance); }

        public static string Ware(string seed) { return Voice.Pick(seed + "|ware", Wares); }

        public static string Trade(string ware) { return TradePrefix + ware; }

        public static bool Is(string trade) { return trade != null && trade.StartsWith(TradePrefix, System.StringComparison.Ordinal); }

        public static string WareOf(string trade) { return Is(trade) ? trade.Substring(TradePrefix.Length) : null; }

        /// The firm he travels for: a surname, and Sons or Brothers, of a far city.
        public static string Firm(string seed)
        {
            return Residents.Surname(seed + "|firm") + Voice.Pick(seed + "|fs", " & Sons", " Brothers", " & Company") + " of " + Voice.Pick(seed + "|fc", Trains.FarTowns);
        }

        /// The store keeper's answer by his manner (Exchanges.Manner): kind orders, cold haggles, sharp sends him off.
        public static string Outcome(string manner) { return manner == "kind" ? "order" : manner == "cold" ? "haggle" : "no"; }

        private static readonly string[] Pitches = {
            "{f}, sir. You'll want our {w} before the other fellow gets here.",
            "Five minutes, that's all I ask. The finest {w} this side of the Mississippi, and I can prove it.",
            "Sir, your shelves are crying out for {w}. Let me show you what {town} has been missing.",
            "Back again, sir. {f}, {w}. The prices are down since spring." };
        private static readonly string[] Orders = {
            "Leave me your book. Two dozen, and see they come by the end of the month.",
            "All right, you've talked me into it. Half a gross. Don't make me sorry.",
            "Same as last season, and add a little. Folk liked them." };
        private static readonly string[] Haggles = {
            "Your man from Chicago was here Tuesday, and cheaper. Come down a dime and we'll talk.",
            "I'll take a dozen on consignment. Sell them, and I'll pay you. That's my offer.",
            "Too dear. Leave a sample. I'll think on it." };
        private static readonly string[] Nos = {
            "I've a cellar full of the last fellow's. Good day.",
            "Not today, and not tomorrow either. The train's that way.",
            "I don't buy from drummers. Go on, before you scare the customers." };
        private static readonly string[] After = {
            "Order in the book. That's the hotel paid for.", "Half an order. Better than the last three towns.",
            "Turned out on my ear. Smile, tip the hat, next store.", "Sixth no this week. The seventh is always a yes. Always." };
        private static readonly string[] Notices = {
            "A drummer off the train in {town}: {n}, for {f}, in {w}. He's put up at the hotel.",
            "{n}, a drummer in {w} for {f}, came in on the train to {town}. The store will hear his pitch." };

        public static string Pitch(string firm, string ware, string town, string seed)
        {
            return Voice.Pick(seed + "|p", Pitches).Replace("{f}", firm ?? "my firm").Replace("{w}", ware ?? "goods").Replace("{town}", town ?? "this town");
        }

        public static string Answer(string outcome, string seed)
        {
            return Voice.Pick(seed + "|a", outcome == "order" ? Orders : outcome == "haggle" ? Haggles : Nos);
        }

        public static string AfterThought(string outcome, string seed)
        {
            return outcome == "order" ? After[Voice.Hash(seed) % 2] : outcome == "haggle" ? After[1] : After[2 + Voice.Hash(seed) % 2];
        }

        public static string Notice(string name, string firm, string ware, string town, string seed)
        {
            return Voice.Pick(seed + "|n", Notices).Replace("{n}", name ?? "A drummer").Replace("{f}", firm ?? "a city firm").Replace("{w}", ware ?? "goods").Replace("{town}", town ?? "town");
        }

        /// His thoughts (InnerLife's "drummer" trade group).
        public static readonly string[] Thoughts = {
            "Sample case weighs more every town. Or I weigh less.", "Hotel bed, hotel coffee, hotel bugs. Same in every town on the line.",
            "Smile first, prices second, the order third. Never the other way round.", "A storekeeper's no is only his first word.",
            "Wired the firm: two orders, one maybe. They'll want more than that.", "Three towns to go before Saturday, and my boots know it.",
            "Learned a new joke in the last town. It'll do for six more.", "The wife writes care of the firm. I'm a week behind her letters.",
            "Every town thinks its store is the only store. I've seen two hundred.", "Train at seven. Sleep on it if I'm lucky." };

        public static IEnumerable<string> AllLines()
        {
            foreach (string w in Wares)
            {
                yield return Pitch("Hollis & Sons of Saint Louis", w, "Valentine", w);
                yield return Notice("Abel Pratt", "Hollis & Sons of Saint Louis", w, "Valentine", w);
            }
            foreach (string[] p in new[] { Orders, Haggles, Nos, After, Thoughts }) foreach (string l in p) yield return l;
        }
    }
}
