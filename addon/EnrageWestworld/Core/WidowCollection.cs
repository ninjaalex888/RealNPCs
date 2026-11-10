// A collection for the widow (FOURTEENHOUR reserve R20; TWELVEHOUR reserve R9; the game's fundraiser theme; [Scenes] WidowCollection).
// After a death in a town (one of its people, in the last three days), a townswoman goes round with a collection for his widow
// (or his family) and walks up to you: J "Give a dollar", "Give five", or "Not today". What you give is known: she remembers it (the
// world log's charity, as a gift: the late thanks may come of it), the town has it, and on the Sunday after, the preacher thanks a
// generous stranger from the pulpit. Once for each death. Why it reads true: in 1899 the hat that went round after a death was the
// only insurance most families had, and the town always knew who put what in it. Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class WidowCollection
    {
        public static bool On = true;   // [Scenes] WidowCollection, from the settings
        public const string Kind = "collection";
        public const int DaysAfter = 3, FromHour = 9, ToHour = 18, PromptMs = 10000, LookMs = 45000;

        public static string Asks(string dead, string widow, string seed)
        {
            string d = Person.FirstOf(dead ?? "him"), w = widow != null ? Person.FirstOf(widow) : null;
            return Voice.Pick(seed + "|ask", "Would you give something for " + (w != null ? w + ", " + d + "'s widow" : d + "'s family") + "? Every little helps.",
                                             "We're taking up a collection for " + (w ?? d + "'s people") + ". Whatever you can spare, mister.",
                                             "Sir, " + d + " left " + (w != null ? w : "a family") + " with nothing. Would you put something in?");
        }

        public static string Thanks(int cents, string seed)
        {
            return cents >= 500 ? Voice.Pick(seed + "|t5", "Five dollars! Lord bless you. That's rent for a month.", "That's more than the whole street gave. Thank you.")
                 : cents > 0 ? Voice.Pick(seed + "|t1", "Thank you, mister. It all adds up.", "Bless you. She'll hear of it.")
                 : Voice.Pick(seed + "|t0", "Well. I'll not keep you.", "I see.");
        }

        /// The Sunday after, from the pulpit (the town's word).
        public static string Pulpit(string dead, string widow, string town, int cents, string seed)
        {
            string w = widow != null ? Person.FirstOf(widow) : Person.FirstOf(dead ?? "the dead") + "'s family";
            return Voice.Pick(seed + "|pp", "From the pulpit in " + (town ?? "town") + " this Sunday, the preacher thanked a generous stranger who gave " + (cents >= 500 ? "five dollars" : "for") + " " + w + ".",
                                            "The preacher in " + (town ?? "town") + " spoke this morning of a stranger's kindness to " + w + ". \"The Lord loves a cheerful giver.\"");
        }

        public static System.Collections.Generic.List<InteractOption> Rows(int cash)
        {
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "collection:1", Label = "Give a dollar", Why = cash < 100 ? "you haven't a dollar" : null },
                new InteractOption { Id = "collection:5", Label = "Give five dollars", Why = cash < 500 ? "you haven't five dollars" : null },
                new InteractOption { Id = "collection:0", Label = "\"Not today.\"" },
            };
        }

        /// Days to the coming Sunday (0 Sunday, the weekday as InnerLife has it); 7 when today is Sunday (the next one).
        public static int DaysToSunday(int weekday)
        {
            if (weekday < 0)
                return 3;
            int d = (7 - weekday) % 7;
            return d == 0 ? 7 : d;
        }
    }
}
