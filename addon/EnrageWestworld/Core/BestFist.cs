// The town's best fist (FOURTEENHOUR reserve R7; [Scenes] BestFist). Fights already had winners (the Director's fights, a brawl's last
// man standing, the prize fight out back of the saloon: Core/PrizeFight.cs), and a win already counted for a night (the round, the
// news, the wagers), never for a name. Now a man who wins three fights in a town (the street's, the saloon's, the prize fight's)
// gets one: the town's talk calls him its best fist, and he thinks of it when you're near, once a day. Then men from other towns come
// to try him: the town's next prize fight is his, against a man of a fighting temper from another of the nine towns, for twice the
// purse; lose it and the name goes with the purse. Why it reads true: a frontier town ranked its men by what they'd done in front of
// it, and a reputation is an invitation; the man from the next town over who rides in to test it is as old as reputations.
// Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class BestFist
    {
        public static bool On = true;   // [Scenes] BestFist, from the settings
        public const int NameAfter = 3, ChallengePercent = 60;   // three wins in a town; the town's prize fight is a challenge this often once he has the name

        public static string WinsKey(string town, string id) { return "fistwins|" + town + "|" + id; }
        public static string HolderKey(string town, string id) { return "bestfist|" + town + "|" + id; }
        public static string HolderPrefix(string town) { return "bestfist|" + town + "|"; }

        /// What the town's talk says when he gets the name.
        public static string Named(string name, string town, int wins, string seed)
        {
            string first = Person.FirstOf(name ?? "A man");
            return Voice.Pick(seed + "|nm", "Folks in " + (town ?? "town") + " are calling " + first + " the best fist in town. " + Grammar.UpperFirst(Count(wins)) + " fights, and not one of them laid him out.",
                                            first + " has put " + Count(wins) + " men on the boards in " + (town ?? "town") + ". Nobody's keen to make it " + Count(wins + 1) + ".");
        }

        /// His thought, near you, once a day while he holds it.
        public static string Thought(string town, string seed)
        {
            return Voice.Pick(seed + "|th", "They step aside for me in " + (town ?? "this town") + " now. Took three fights.", "Best fist in town. Somebody'll come to try it. Somebody always does.",
                                            "Men buy me drinks I didn't ask for. That's what a name gets you.");
        }

        /// The word that goes round for a challenge (the prize fight's toast: {a} the holder, {b} the man come to try him).
        public static string ChallengeWord(string a, string b, string from, string town, int purse, string seed)
        {
            string fa = Person.FirstOf(a ?? "their man"), fb = Person.FirstOf(b ?? "a stranger");
            return Voice.Pick(seed + "|cw", "Word in " + town + ": " + fb + " has come over from " + from + " to try " + fa + ", the best fist in town. Out back of the saloon tonight, after nine. A $" + purse + " purse.",
                                            "A man called " + fb + " rode in from " + from + " asking for " + fa + " by name. They settle it behind the saloon tonight, $" + purse + " in the hat.");
        }

        /// The morning after a challenge: the holder kept it, or the man from the other town took it home.
        public static string ChallengeMorning(string a, string b, string from, string town, bool holderWon, string seed)
        {
            string fa = Person.FirstOf(a ?? "their man"), fb = Person.FirstOf(b ?? "the stranger");
            return holderWon ? Voice.Pick(seed + "|cm1", fb + " came all the way from " + from + " to try " + fa + ", and went home on a borrowed horse.", fa + " is still the best fist in " + town + ". The man from " + from + " found that out.")
                             : Voice.Pick(seed + "|cm0", "The man from " + from + " beat " + fa + " out back of the saloon. " + town + " will be a while living that down.", fa + " lost the name to a stranger from " + from + ". Took the purse too.");
        }

        static readonly string[] Numbers = { "no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten" };
        static string Count(int n) { return n >= 0 && n < Numbers.Length ? Numbers[n] : n.ToString(); }
    }
}
