// The house a round, at the bar (THREEHOUR item 3). Whiskey for the house came first through Talk (SIXDAY item 1; Core/Fondness.cs,
// Game/WestworldScript.Fondness.cs HouseRound): the room cheers and each named person treated is fonder of you. Talk is frozen,
// so this is the same round from the event key (J), as the slate and the sheriff's sale take your money: stand at a saloon's bar
// near the barkeep with a few others about and J offers it. The price is the room's: a dime a head, a dollar at least, four at
// most. One a day in each saloon: a second round the same day is only a drink. The barkeep says his word; the rest is the
// round as it was (the cheers, the fondness, the "round" each treated keeps, which the town's talk and the late thanks read).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class BarRound
    {
        public const string Kind = "barround";
        public const int PromptMs = 12000, LookMs = 1000, AgainMs = 20000;
        public const float BarMetres = 4f;   // you this near the barkeep
        public const int LeastCompany = 2;   // others at the bar: two make a house

        /// What a round costs with this many at the bar, in cents.
        public static int Cents(int company)
        {
            int c = company * 10;
            return c < 100 ? 100 : c > 400 ? 400 : c;
        }

        public static string Dollars(int cents)
        {
            return "$" + (cents / 100) + "." + (cents % 100).ToString("00");
        }

        /// Whether this saloon has had your round today already ("town|day").
        public static string DayKey(string town, long gameMinute)
        {
            return (town ?? "?") + "|" + gameMinute / 1440;
        }

        public static List<InteractOption> Rows(int cents, int cashCents, int company)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "barround:buy", Label = "Buy the house a round (" + Dollars(cents) + ", " + company + " at the bar).", Why = cashCents < cents ? "you haven't the money" : null },
                new InteractOption { Id = "barround:no", Label = "Just the one for me." }
            };
        }

        private static readonly string[] Keeper =
        {
            "Drinks on the gentleman! Step up, step up.",
            "You heard him. Whiskey for the house.",
            "There's a man who knows how to walk into a saloon.",
            "Line 'em up. The stranger's paying.",
            "On you, then. They'll remember it, I'd wager.",
            "Glasses out, boys. This one's bought."
        };

        public static string KeeperLine(string seed)
        {
            return Keeper[System.Math.Abs(Voice.Hash(seed) % Keeper.Length)];
        }

        public static IEnumerable<string> AllLines()
        {
            return Keeper;
        }
    }
}
