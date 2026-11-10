// A voice from an alley (FOURTEENHOUR reserve R15; TWELVEHOUR reserve R4; [Encounters] AlleyVoice). Saint Denis after dark: a man calls
// for help from somewhere you can't see, a back street off yours. Go to him and he is what his nature made the night: most times real
// (beaten and robbed, sat on the stones: help him up, and he remembers it, or leave him), sometimes bait (the honest-with-nobody kind:
// as you reach him two men step out behind you with their guns: pay, and they go; refuse, and it's their fight with you, the game's
// from there; the man on the stones is up and away with them). The staged encounters' way: his choice is drawn from what his temper
// and need weigh. Why it reads true: a city's dark is full of cries, and a man who goes to every one of them is either a saint or a
// mark; Saint Denis in 1899 was both kinds of city. Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class AlleyVoice
    {
        public static bool On = true;   // [Encounters] AlleyVoice, from the settings
        public const string Kind = "alley";
        public const string Town = "Saint Denis";
        public const int Percent = 25, FromHour = 21, ToHour = 4, CallGapMs = 6500, MostCalls = 6, PromptMs = 9000;
        public const float NearMetres = 5f, TrapMetres = 7f;

        public static bool Hour(int clock) { return clock >= FromHour || clock < ToHour; }

        /// Real or bait, by the nature of the man on the stones: the honest-with-nobody and the greedy are bait more often; seeded.
        public static bool Trap(string temper, bool needsMoney, string seed)
        {
            int pct = temper == "Greedy" || temper == "Cold" ? 45 : temper == "Drifter" ? 35 : temper == "Loyal" || temper == "Coward" ? 10 : 20;
            if (needsMoney)
                pct += 15;
            return Voice.Chance(seed + "|trap", pct);
        }

        public static string Call(int n, string seed)
        {
            return Voice.Pick(seed + "|call" + n, "Help! Somebody, help me!", "Please! Over here! I'm hurt!", "Is anybody there? For the love of God!", "Help... please...");
        }

        /// The real one, when you reach him.
        public static string Hurt(string seed)
        {
            return Voice.Pick(seed + "|hurt", "Two of them. Took my wages and my boots. Thank God you came.", "They jumped me from the doorway. I can't get up. Help me up?", "I thought nobody'd come. Nobody comes, down here.");
        }

        public static string Thanks(string seed)
        {
            return Voice.Pick(seed + "|th", "Bless you, mister. I'll not forget a face that stopped.", "I'm alright. I'm alright. Thank you.", "You're the only one came. The only one.");
        }

        /// The bait's men, as they step out behind you.
        public static string Demand(int cents, string seed)
        {
            return Voice.Pick(seed + "|dm", "Evening. Easy, now. Your purse, and nobody gets hurt.", "That's far enough, friend. " + (cents / 100) + " dollars, and you walk away.", "Kind of you to come. Now the money.");
        }

        public static string Paid(string seed)
        {
            return Voice.Pick(seed + "|pd", "Obliged. Stay out of the dark, friend.", "Smart man. Don't come down here again.");
        }

        public static string Bait(string seed)
        {
            return Voice.Pick(seed + "|bt", "Sorry, friend. Nothing personal.", "You should've kept walking.");
        }

        /// What they take from you, if you pay: five to twenty dollars, never more than you carry.
        public static int Cents(int carried, string seed)
        {
            int want = (5 + (Voice.Hash(seed + "|cents") & 0x7fffffff) % 16) * 100;
            return carried < want ? carried : want;
        }

        public static System.Collections.Generic.List<InteractOption> Rows(bool trap, int cents)
        {
            if (trap)
                return new System.Collections.Generic.List<InteractOption>
                {
                    new InteractOption { Id = "alley:pay", Label = "Hand it over ($" + (cents / 100) + "." + (cents % 100).ToString("00") + ")" },
                    new InteractOption { Id = "alley:refuse", Label = "\"Not tonight, boys.\"" },
                };
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "alley:help", Label = "Help him up" },
                new InteractOption { Id = "alley:leave", Label = "Leave him" },
            };
        }
    }
}
