// Wanted, but which one? (FIVEDAY reserve R31). A resident whose trouble is the wanted poster (the worry "law" at stage 2
// or before: InnerLife.WorryNow, the same test the street's reputation uses) has lived quietly under another name. Come
// within four metres of one and you know the face: once each, the box says so (the poster at the sheriff's office, the
// sum, the old crime far away), and from then on near them J offers the choice the town never had to make: turn them in
// for the bounty (paid; they go up the line in irons, gone for good), warn them (their thanks, and they owe you), see
// them onto the next train (a ticket bought, a new name, gone by train), or leave it. Kept in Store.Counts ("wanted:<id>":
// 1 recognised, 2 settled), no new save field. Behind [World] WantedWhich.
//
// Why it reads true: a fugitive who has made an honest life is the hardest case in ethics, the law against the person
// they've become; who you are shows in what you do with knowing. And being recognised is the fear they have lived with
// every day: the moment it comes is quieter than they imagined. Plain C#, no game; all words original. mindtest
// WantedWhichTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class WantedWhich
    {
        public const string Kind = "wanted";
        public const int PromptMs = 10000, TicketCents = 1500, Seen = 1, Settled = 2;
        public const float NearMetres = 4f;

        public static string Key(string id) { return "wanted:" + id; }

        /// Whether this worry is the poster's, still live (stage 2 or before, as Game/WestworldScript.Street.cs reads it).
        public static bool OnThePoster(string worry, int stage) { return worry == "law" && stage <= 2; }

        /// The bounty, in cents: $50 to $150 in tens, by who.
        public static int Cents(string id) { return (50 + 10 * (int)(Voice.Hash(id + "|wb") % 11)) * 100; }

        private static readonly string[] Crimes = {
            "a mail coach in the Dakotas", "the Pecos bank, eight years back", "a killing at a dance in Abilene",
            "horses run off from a Wyoming ranch", "a train stopped near Tucson", "a jailbreak in Fort Smith" };

        public static string Crime(string id) { return Voice.Pick(id + "|wc", Crimes); }

        private static readonly string[] Knows = {
            "You know that face from the poster at the sheriff's: {n}, ${d}, for {c}.",
            "{n}. Same eyes as the poster by the jail door: ${d}, for {c}." };
        private static readonly string[] Taken = {
            "{n} went up the line in irons, on your word. ${d} for it. The town says they'd never have guessed.",
            "The sheriff took {n} at the door. ${d} in your hand. {n} didn't fight it." };
        private static readonly string[] Warned = {
            "...How long have you known? No. Don't tell me. Thank you.",
            "I've waited eight years for someone to say that. I owe you." };
        private static readonly string[] Train = {
            "A ticket, and a name to go with it. I won't forget your face either.",
            "The next train. Yes. I've had a bag packed for years." };
        private static readonly string[] Thought = {
            "The stranger looked at me like he'd seen a poster. God help me, he has.",
            "Somebody knows. I can feel it at the back of my neck." };

        private static string Fill(string l, string name, string id)
        {
            return l.Replace("{n}", Person.FirstOf(name) ?? "them").Replace("{d}", (Cents(id ?? "x") / 100).ToString()).Replace("{c}", Crime(id ?? "x"));
        }

        public static string KnowsLine(string name, string id) { return Fill(Voice.Pick(id + "|wk", Knows), name, id); }
        public static string TakenLine(string name, string id) { return Fill(Voice.Pick(id + "|wt", Taken), name, id); }
        public static string WarnedLine(string id) { return Voice.Pick(id + "|ww", Warned); }
        public static string TrainLine(string id) { return Voice.Pick(id + "|wtr", Train); }
        public static string ThoughtLine(string id) { return Voice.Pick(id + "|wth", Thought); }

        public static List<InteractOption> Rows(string name, string id, int cashCents)
        {
            string n = Person.FirstOf(name) ?? "them";
            return new List<InteractOption>
            {
                new InteractOption { Id = "wanted:turnin", Label = "Turn " + n + " in for the bounty ($" + Cents(id) / 100 + ")." },
                new InteractOption { Id = "wanted:warn", Label = "Tell " + n + " the poster's up." },
                new InteractOption { Id = "wanted:train", Label = "Buy " + n + " a ticket on the next train ($" + TicketCents / 100 + ").", Why = cashCents < TicketCents ? "you haven't the money" : null },
                new InteractOption { Id = "wanted:leave", Label = "Leave it be." }
            };
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Knows, Taken })
                foreach (string l in p)
                    yield return Fill(l, "Martha Ellis", "r1");
            foreach (string[] p in new[] { Warned, Train, Thought })
                foreach (string l in p)
                    yield return l;
        }
    }
}
