// A purse snatched (FOURTEENHOUR item 20; TWELVEHOUR item 9; [Scenes] PurseSnatch). Already there, and the footing for this: the
// pickpocket caught at it ("pickpocket": the mark feels it, the thief runs, she cries "Thief!" and asks you to stop him), the thief a
// red dot and fair game, a lawman within 70 m giving chase and tackling him (seen), and your own catch (lasso, hogtie, tackle) with
// her thanks and a good name. New here: the crowd is no longer only onlookers. At the cry, one man near her may give chase, the one
// whose nature makes it his (the bold, the reckless and the hot, the kind, a friend of hers most; the anxious hardly ever): he runs the thief
// down and brings him down (the lawman's own tackle), hands her purse back, and the town hears of it; or the thief is too quick for
// him. And when you catch the thief yourself, J says what's done with him: make him give it back (a hot one may fight you for it:
// his fight is the game's), hand him to the law (a lawman near comes and ties him), or let him run. Why it reads true: a snatch turns
// a crowd from strangers into a group with one task for ten seconds (diffusion of responsibility, broken), and the one who runs
// after is always the one whose nature made it his. Plain C#, no game; all words this add-on's.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class PurseSnatch
    {
        public static bool On = true;   // [Scenes] PurseSnatch, from the settings
        public const string Kind = "thiefheld";
        public const float ChaseFromMetres = 20f, LawNearMetres = 60f;
        public const int ChaseMostMs = 20000, AskMs = 15000;

        /// Whether this man gives chase at her cry: the bold and the reckless, the kind, a friend of hers; never the anxious much.
        public static bool Chases(Traits t, bool friend, bool markWoman, string seed)
        {
            return Voice.Chance(seed, ChasePercent(t, friend, markWoman));
        }

        public static int ChasePercent(Traits t, bool friend, bool markWoman)
        {
            float c = 0.07f + (friend ? 0.25f : 0f) + (markWoman ? 0.08f : 0f);
            if (t != null)
                c += 0.18f * t.Z[Traits.Assertiveness] + 0.12f * t.Z[Traits.Risk] + 0.10f * t.Z[Traits.Compassion] + 0.10f * t.Z[Traits.Anger] - 0.15f * t.Z[Traits.Anxiety];   // (the hot run after him too)
            return (int)Math.Round(Math.Max(0.03f, Math.Min(0.70f, c)) * 100);
        }

        public static string Shout(string seed) { return Voice.Pick(seed, "Stop, thief!", "Hey! Stop that man!", "You there! Stop!"); }
        public static string Got(string seed) { return Voice.Pick(seed, "Got you, you devil.", "Not so fast, friend.", "Stay down."); }
        public static string Away(string seed) { return Voice.Pick(seed, "He's away. Too quick for me.", "Lost him in the crowd.", "Gone. Fast as a jackrabbit."); }

        /// The man who ran him down, to her (or him), handing it back.
        public static string Handed(bool woman, string seed)
        {
            return woman ? Voice.Pick(seed, "Here's your purse, ma'am. All there, I think.", "Your purse, ma'am. He didn't get far.", "This yours, ma'am? Thought so.")
                         : Voice.Pick(seed, "Your wallet, friend. He didn't get far.", "Here. Count it, mind.");
        }

        public static string Thanks(bool woman, string seed)
        {
            return Voice.Pick(seed, "Oh! You got it back! Oh, sir!", "Bless you. I thought it was gone for good.", woman ? "I don't know how to thank you. Truly." : "I owe you a drink, friend. Two.");
        }

        public static string News(string chaser, string mark, bool woman, string seed)
        {
            return Voice.Pick(seed, chaser + " ran down a thief in the street and gave " + mark + " back " + (woman ? "her purse." : "his money."),
                              "A thief was run down in the street by " + chaser + ", of all people. " + mark + " has " + (woman ? "her purse" : "his money") + " back.");
        }

        /// What you can do with the thief you caught; `lawNear` a lawman free within 60 m.
        public static List<InteractOption> Rows(string thief, bool lawNear)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "thief:back", Label = "Make " + (thief ?? "him") + " give it back." },
                new InteractOption { Id = "thief:law", Label = "Hand him to the law.", Why = lawNear ? null : "no lawman near" },
                new InteractOption { Id = "thief:go", Label = "Let him run." }
            };
        }

        /// Whether he gives it back when told: a hothead or a proud thief may fight you for it instead.
        public static bool GivesBack(string temper, string seed)
        {
            return !Voice.Chance(seed + "|fight", temper == "Hothead" ? 35 : temper == "Proud" ? 20 : 5);
        }

        public static string GiveLine(bool gives, string seed)
        {
            return gives ? Voice.Pick(seed + "|give", "All right, all right. Here.", "Here. Have it, damn you.", "Easy! It's here. It's all here.")
                         : Voice.Pick(seed + "|nogive", "Like hell I will.", "You'll have to take it off me.", "Come and get it, then.");
        }

        public static string LetGoLine(bool woman, string seed)
        {
            return Voice.Pick(seed, "You turned him loose? With my " + (woman ? "purse" : "money") + "?", "You had him in your hands, and you turned him loose!", "Well. Some help you are.");
        }

        public static string ToLawLine(string seed) { return Voice.Pick(seed, "Deputy! This one's yours.", "Here's a thief for you, deputy.", "Lawman! Take him off my hands."); }
    }
}
