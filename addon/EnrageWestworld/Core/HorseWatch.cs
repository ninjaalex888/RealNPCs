// Hire a man to watch your horse (FOURTEENHOUR reserve R21; TWELVEHOUR reserve R10; [Scenes] HorseWatch). Down off your horse in a town
// by day, a townsman who's down on his luck (money short, or a money worry on him) comes over: a dime and he'll watch her. J:
// "Here's a dime" or "No, thank you". Hired, he stands by your horse while you're about the town; come back to her and he says so, and
// he's a little fonder of the stranger who gave him work. And a horse thief who goes for a horse near him meets a man who shouts: the
// thief drops it and runs (the horse thief's own scene, Game/WestworldScript.HorseThief.cs, which never takes yours). Why it reads true:
// a dime to a man with nothing is a day's dignity, and a hitching rail with a man watching it is a rail thieves pass by. Plain C#,
// no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class HorseWatch
    {
        public static bool On = true;   // [Scenes] HorseWatch, from the settings
        public const string Kind = "horsewatch";
        public const int Cents = 10, FromHour = 7, ToHour = 20, PromptMs = 9000, WatchMs = 1800000, LookMs = 4000;
        public const float OfferMetres = 15f, ThiefMetres = 20f;

        public static bool Hour(int clock) { return clock >= FromHour && clock < ToHour; }

        /// Down on his luck: money short, or a money worry on him (debt, savings, cards, the bank).
        public static bool DownOnLuck(float moneyNeed, string worry)
        {
            return moneyNeed >= 0.5f || worry == "debt" || worry == "savings" || worry == "cards" || worry == "bank";
        }

        public static string Offer(string seed)
        {
            return Voice.Pick(seed + "|of", "Watch your horse, mister? A dime, and nobody lays a finger on her.", "Fine animal. I'll mind her for you. A dime?",
                                            "I'll stand by her while you're about your business. Ten cents, is all.");
        }

        public static string Hired(string seed) { return Voice.Pick(seed + "|hd", "Obliged. She's safe with me.", "Thank you, sir. Take your time."); }
        public static string Declined(string seed) { return Voice.Pick(seed + "|dc", "Suit yourself.", "Well. Mind her yourself, then."); }
        public static string Back(string seed) { return Voice.Pick(seed + "|bk", "There she is. Not a soul came near.", "All yours, mister. Quiet as a church.", "She's been good as gold."); }
        public static string Shout(string seed) { return Voice.Pick(seed + "|sh", "Hey! You! That's not your horse!", "Get away from there! I see you!", "Thief! Leave that horse be!"); }

        public static System.Collections.Generic.List<InteractOption> Rows(int cash)
        {
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "horsewatch:yes", Label = "\"Here's a dime.\"", Why = cash < Cents ? "you haven't a dime" : null },
                new InteractOption { Id = "horsewatch:no", Label = "\"No, thank you.\"" },
            };
        }
    }
}
