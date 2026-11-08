// A reward posted for a neighbour (FIVEDAY reserve R30). A brawl ends in a death (Game/WestworldScript.BrawlDown.cs) and the
// one who struck the last blow lives in that town: the sheriff posts a reward for him, $25 to $75, for a
// week. The town splits on it: the greedy and the cold think of the money, the loyal and the friendly of warning him, the
// rest of keeping out of it; he thinks of the price on his head. In the Around town box once a town a day, the notice. And
// the player, near him, has the J menu: turn him in (the sheriff takes him to the county jail, the reward is paid, and he
// is gone from the town for good), warn him (he runs that night, gone too, and the reward lapses), or leave it. Unclaimed,
// it lapses after the week. Kept in Store.Counts (the day posted and the cents), no new save field. Behind [World]
// NeighbourReward.
//
// Why it reads true: a reward turns a neighbour into a price, and a town finds out what it is by who reaches for it.
// Loyalty and greed are both in the room; which one a person shows is their temper (Personality, Disposition), and the
// player's choice is the same one they're making. Plain C#, no game; all words original. mindtest NeighbourRewardTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class NeighbourReward
    {
        public const string Kind = "reward";
        public const int Days = 7, PromptMs = 10000, ThoughtChance = 35;
        public const float NearMetres = 4f;

        public static string DayKey(string id) { return "reward:" + id; }
        public static string CentsKey(string id) { return "rewardc:" + id; }

        /// The reward, in cents: $25 to $75 in fives, by who and when.
        public static int Cents(string seed) { return (25 + 5 * (int)(Voice.Hash(seed + "|rw") % 11)) * 100; }

        /// Still posted: from the day it went up, for a week.
        public static bool Live(int day, long today) { return day > 0 && today >= day && today - day < Days; }

        /// How a resident of the town leans, by temper: "collect", "warn" or "leave".
        public static string Side(Resident r)
        {
            if (r == null)
                return "leave";
            if (r.Personality == "Greedy" || r.Personality == "Cold")
                return "collect";
            if (r.Personality == "Loyal" || r.Disposition == "Friendly")
                return "warn";
            return "leave";
        }

        private static readonly string[] Notices = {
            "Sheriff's notice at {t}: {d} for {n}, for the death in the fight. Half the town says it was a fair fight.",
            "{d} posted at {t} for {n}, wanted for a death in a brawl. Folk read it and look at each other." };
        private static readonly string[] Collect = {
            "{d} for {n}. That's a season's wages for one walk to the sheriff.",
            "Neighbour or no, {n} killed somebody. And {d} is {d}." };
        private static readonly string[] Warn = {
            "Somebody ought to tell {n} to ride before some fool collects.",
            "{n} hit too hard in a fight. That's no hanging matter. I'll warn {n}." };
        private static readonly string[] Leave = {
            "The notice for {n} is up at the jail. I've read it. I've no wish to read it again.",
            "{n} and that reward. Whatever happens, it isn't mine to make happen." };
        private static readonly string[] Wanted = {
            "{d} on my head. For a fight that fool started.",
            "Every man in this town has read that notice by now. I can see them counting." };
        private static readonly string[] Taken = {
            "The sheriff took {n} up the line to the county jail. You were paid {d}. Some of the town won't look at you.",
            "{n} went off to the county jail in irons, on your word. {d} in your pocket." };
        private static readonly string[] Ran = {
            "{n} left {t} in the night, ahead of the reward. Somebody gave warning.",
            "{n} is gone from {t}, horse and all. The notice is still up, for nobody." };
        private static readonly string[] Thanks = {
            "...I'll not forget it. I'm gone by dark.",
            "You didn't have to tell me. I'll ride tonight." };

        private static string Fill(string l, string name, string town, int cents)
        {
            return l.Replace("{n}", Person.FirstOf(name) ?? "him").Replace("{t}", town ?? "town").Replace("{d}", "$" + cents / 100);
        }

        public static string Notice(string name, string town, int cents, string seed) { return Fill(Voice.Pick(seed + "|rn", Notices), name, town, cents); }
        public static string Thought(string side, string name, int cents, string seed)
        {
            string[] pool = side == "collect" ? Collect : side == "warn" ? Warn : Leave;
            return Fill(Voice.Pick(seed + "|rt", pool), name, null, cents);
        }
        public static string WantedThought(int cents, string seed) { return Fill(Voice.Pick(seed + "|rwt", Wanted), null, null, cents); }
        public static string TakenNotice(string name, int cents, string seed) { return Fill(Voice.Pick(seed + "|rtk", Taken), name, null, cents); }
        public static string RanNotice(string name, string town, string seed) { return Fill(Voice.Pick(seed + "|rr", Ran), name, town, 0); }
        public static string Thank(string seed) { return Voice.Pick(seed + "|rth", Thanks); }

        public static List<InteractOption> Rows(string name, int cents)
        {
            string n = Person.FirstOf(name) ?? "him";
            return new List<InteractOption>
            {
                new InteractOption { Id = "reward:collect", Label = "Turn " + n + " in to the sheriff ($" + cents / 100 + ")." },
                new InteractOption { Id = "reward:warn", Label = "Tell " + n + " to ride tonight." },
                new InteractOption { Id = "reward:leave", Label = "Leave it be." }
            };
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Notices, Collect, Warn, Leave, Wanted, Taken, Ran })
                foreach (string l in p)
                    yield return Fill(l, "Jim Bole", "Valentine", 5000);
            foreach (string l in Thanks) yield return l;
        }
    }
}
