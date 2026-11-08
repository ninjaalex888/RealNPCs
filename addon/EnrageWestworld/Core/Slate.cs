// The slate at the store (FIVEDAY reserve R2). In 1899 most of a town lived on credit at the general store: flour, coffee,
// tobacco and cloth went "on the slate" (the storekeeper's book) and were paid down on payday, or at harvest, or never. Here:
// now and then, when you're at the store, a townsman or townswoman goes up to the counter and asks to put it on the slate.
// What they owe is their own this week (more with a debt, the cards or the bank on their mind; nothing for the well-off),
// and the keeper answers by what's owed and his own manner: he chalks it up, asks for something on account first, or says
// the slate's full and it's cash from now on. When he won't, J for ten seconds lets you pay what they owe ($ from your own
// pocket): a kindness they and anyone watching remember. On Saturday afternoons (payday) they come to pay it down instead.
// Session-only for who's been paid off (nothing new in the save). Behind [Scenes] StoreSlate.
//
// Why it reads true: credit is a relationship before it's money. The book is where a small town keeps score of who is
// trusted, and to be refused at the counter with neighbours in the store is a public shame (status loss weighs as much as
// the goods). Paying another man's slate is old-fashioned charity at its most visible, and puts him in your debt in a way
// he won't forget, for better and worse. Plain C#, no game; all words original. mindtest SlateTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Slate
    {
        public const string Kind = "slate";
        public const int ChalkUnderCents = 600, FullOverCents = 1200;
        public const int PromptMs = 10000;
        public const float CounterMetres = 2.4f, KeeperNearYou = 25f, CustomerFromKeeper = 18f;
        public const int LookMs = 20000, RestMs = 90000, Chance = 35;

        /// What this person owes the store this week, in cents: their own, by the week; the worries that eat money owe more;
        /// the well-off owe nothing.
        public static int OwedCents(string id, long gameDay, string worry, bool wellOff)
        {
            if (wellOff || id == null)
                return 0;
            int baseCents = 100 + (int)((Voice.Hash(id + "|slate|" + gameDay / 7) & 0x7fffffff) % 9) * 100;   // $1 to $9
            bool pressed = worry == "debt" || worry == "cards" || worry == "bank" || worry == "drink";
            return pressed ? baseCents + 600 : baseCents;
        }

        /// The keeper's answer: "chalk" (put on the slate), "account" (something paid first), or "full" (cash only now).
        public static string Answer(int owedCents, string manner)
        {
            if (owedCents >= FullOverCents || manner == "sharp" && owedCents >= 400)
                return "full";
            if (owedCents < ChalkUnderCents || manner == "kind")
                return "chalk";
            return "account";
        }

        public static string Dollars(int cents) { return "$" + (cents / 100) + (cents % 100 != 0 ? "." + (cents % 100).ToString("00") : ""); }

        private static readonly string[] Asks = {
            "Flour and a pound of coffee, if you'd put it on the slate.", "Put it on the book, would you? Payday's Saturday.",
            "Same as always, and on the slate, if it's all the same to you.", "Could you chalk this up? Just till the end of the week." };
        private static readonly string[] Chalks = {
            "On the slate it goes. Mind you square it Saturday.", "There. It's in the book.", "All right. Don't make a habit of it." };
        private static readonly string[] Accounts = {
            "You're at {d} already. Something on account first, and then we'll see.", "Pay me a little of the {d}, and I'll chalk the rest.",
            "I've a family too. Put something down on the {d} you owe." };
        private static readonly string[] Fulls = {
            "Your slate's full. {d}. It's cash from now on.", "No more credit. Not till the {d} is paid.",
            "I can't, not with {d} on the book. Cash, or put it back." };
        private static readonly string[] Refused = {
            "The whole store heard him. Every soul at that counter.", "I'll have to tell her there's no coffee this week.",
            "Twenty years I've bought at that counter.", "Walk out slow. Don't let them see your face." };
        private static readonly string[] PaysDown = {
            "Here's two dollars on the slate. The rest next week.", "Payday. Take what I owe, and give me a twist of tobacco.",
            "Wipe my name off, I'm square. Feels good to say." };
        private static readonly string[] PaidOffThanks = {
            "You paid it? All of it? I've no words for that.", "I'll pay you back. I will. Every cent, and soon.",
            "Nobody's ever done that for me. God keep you, mister." };
        private static readonly string[] KeeperOnPaid = { "Well. That's the book square. Go on, take your coffee.", "Paid in full. Here's your flour." };
        private static readonly string[] PaidOffThought = {
            "A stranger paid my slate. I'll not forget the face.", "Square at the store, and owing a man I don't know.",
            "Owing him now instead of the store. It sits easier, somehow." };

        public static string Ask(string seed) { return Voice.Pick(seed + "|sa", Asks); }

        public static string AnswerLine(string answer, int owedCents, string seed)
        {
            string[] pool = answer == "chalk" ? Chalks : answer == "account" ? Accounts : Fulls;
            return Voice.Pick(seed + "|sw", pool).Replace("{d}", Dollars(owedCents));
        }

        public static string RefusedThought(string seed) { return Voice.Pick(seed + "|sr", Refused); }
        public static string PayDown(string seed) { return Voice.Pick(seed + "|sp", PaysDown); }
        public static string Thanks(string seed) { return Voice.Pick(seed + "|st", PaidOffThanks); }
        public static string KeeperPaid(string seed) { return Voice.Pick(seed + "|sk", KeeperOnPaid); }
        public static string PaidThought(string seed) { return Voice.Pick(seed + "|so", PaidOffThought); }

        /// The J menu at the counter.
        public static List<InteractOption> Rows(int owedCents, int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "slate:pay", Label = "Pay what they owe (" + Dollars(owedCents) + ").", Why = cashCents < owedCents ? "you haven't the money" : null },
                new InteractOption { Id = "slate:no", Label = "It's none of my affair." }
            };
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Asks, Chalks, Refused, PaysDown, PaidOffThanks, KeeperOnPaid, PaidOffThought }) foreach (string l in p) yield return l;
            foreach (string l in Accounts) yield return l.Replace("{d}", "$7");
            foreach (string l in Fulls) yield return l.Replace("{d}", "$12");
        }
    }
}
