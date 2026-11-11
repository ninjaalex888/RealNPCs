// The collection plate (DEPTH item 11; the roadmap's "The collection plate", whose half was FOURTEENHOUR R20's widow's collection and the
// pulpit's thanks; [Scenes] CollectionPlate). On a Sunday, after the morning service (eleven till one), by a town's church: one man of
// the church (never a crowd) brings the plate round to you. J: a dime, a dollar, or let it pass. What you gave is seen (the world log's
// charity, which the late thanks reads), the man has his word for it, and a stranger's dollar is the preacher's word the next Sunday.
// Once a town a Sunday. Why it reads true: the plate was the one place a town saw what each family could spare, and a stranger who put
// in a dollar was talked of. Plain C#; the game side is the widow's collection's (Game/WestworldScript.WidowCollection.cs).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class CollectionPlate
    {
        public static bool On = true;   // [Scenes] CollectionPlate, from the settings
        public const string Kind = "plate";
        public const int FromHour = 11, ToHour = 13, DimeCents = 10, DollarCents = 100;
        public const float ChurchMetres = 60f;

        public static bool Hour(int weekday, int clock) { return weekday == 0 && clock >= FromHour && clock < ToHour; }

        public static string Asks(string seed)
        {
            return Voice.Pick(seed + "|ask", "For the Lord's work, friend. Whatever you can spare.", "The plate, sir. For the church and the poor of the parish.",
                                             "We take up a little after the service. Would you put something in?");
        }

        public static string Thanks(int cents, string seed)
        {
            return cents >= DollarCents ? Voice.Pick(seed + "|t1", "A whole dollar. The Lord sees it, mister, and so do we.", "That's handsome of you. A stranger, too.")
                 : cents > 0 ? Voice.Pick(seed + "|td", "Every dime counts. Thank you.", "Bless you for it.")
                 : Voice.Pick(seed + "|t0", "Another time, perhaps.", "As you like.");
        }

        /// The next Sunday's sermon, of a stranger's dollar (the town's word, due that morning).
        public static string Sermon(string town, string seed)
        {
            return Voice.Pick(seed + "|sm", "In " + (town ?? "town") + " this Sunday the preacher spoke of a stranger's dollar in the plate: the widow's mite, he said, from a man with more than mites.",
                                            "The sermon in " + (town ?? "town") + " this morning was on the cheerful giver, and the stranger who put a dollar in the plate last Sunday.");
        }

        /// What one of the church thinks, seeing the plate go by you empty.
        public static string Passed(string seed)
        {
            return Voice.Pick(seed + "|ps", "The stranger let the plate go by. Well.", "Not a cent from him. I saw.", "Money for the saloon, I'll wager, and none for the plate.");
        }

        public static List<InteractOption> Rows(int cash)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "plate:10", Label = "Put in a dime", Why = cash < DimeCents ? "you haven't a dime" : null },
                new InteractOption { Id = "plate:100", Label = "Put in a dollar", Why = cash < DollarCents ? "you haven't a dollar" : null },
                new InteractOption { Id = "plate:0", Label = "Let it pass." },
            };
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 12; i++)
            {
                yield return Asks("a" + i);
                yield return Thanks(0, "b" + i);
                yield return Thanks(DimeCents, "c" + i);
                yield return Thanks(DollarCents, "d" + i);
                yield return Sermon("Valentine", "e" + i);
                yield return Passed("f" + i);
            }
        }
    }
}
