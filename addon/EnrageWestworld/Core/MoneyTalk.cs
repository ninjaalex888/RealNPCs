// The money argument (TENHOUR item 15; roadmap.md's idea bank, "Free silver in the saloon"; built on the street's arguments and the ties they leave). Gold against silver, the banks, the trusts and the railroads: in 1899 the
// question that still decided who sat with whom. Which side a man's on goes by how he's fixed; two on one side agree, two on opposite sides fall out and the tie between them cools. 1907 has the bank panic instead. The lines are all original to this add-on.
// Why it reads true: an argument about money is hardly ever about money. Each man is defending the sum of his own luck, and that's why it can part friends.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MoneyTalk
    {
        public const string Silver = "silver", Gold = "gold";

        /// Which side a man's on: the well-off for sound money, everyone who owes anything for silver, with some of each going the other way.
        public static string SideOf(bool wellOff, string temper, string seed)
        {
            if (wellOff)
                return Voice.Chance(seed + "|money", 85) ? Gold : Silver;
            return Voice.Chance(seed + "|money", temper == "Greedy" ? 40 : 70) ? Silver : Gold;
        }

        private static readonly string[] SilverOpen = { "Sixteen to one, {b}. Give a working man money he can lay hands on.", "Gold's a rich man's metal, {b}, and they've got all of it.", "The railroad sets the rate and the bank sets the note, {b}, and I set the table with nothing.", "You show me one trust that ever lowered a price, {b}." };
        private static readonly string[] GoldOpen = { "Silver's a swindle, {b}. You'd be paid in dollars worth fifty cents.", "A dollar ought to be a dollar, {b}. That's all I ask of anybody.", "Mr. Bryan never met a payroll in his life.", "The mills are running, {b}. Leave the money alone." };
        private static readonly string[] SilverAnswer = { "Easy said with a full dinner pail, {a}.", "Who's got the gold, {a}? Not me. Not anybody I drink with.", "Try paying off a mortgage in your sound money." };
        private static readonly string[] GoldAnswer = { "You'd pay your debts in tin if they let you, {a}.", "And what's your silver dollar buy in a year, {a}? Half.", "That's the speech. I've read the speech." };
        private static readonly string[] PanicAngry = { "Banks in New York shut their doors, {b}. Whose money was that?", "One man decides which banks live, {b}. One man, in a library.", "It's all sound, {b}, right up until the doors are chained." };
        private static readonly string[] PanicCalm = { "Somebody had to step in, {b}. Better him than nobody.", "It's a bad autumn, {b}, that's all. They come and they go.", "Keep your head and your deposit, {b}." };
        private static readonly string[] PanicAngryAnswer = { "Keep your deposit, then. Mine's in a coffee tin.", "Tell that to them standing in the line, {a}." };
        private static readonly string[] PanicCalmAnswer = { "You'd have them all run at once, {a}.", "Coffee tins don't pay interest, {a}." };
        private static readonly string[] Agree = { "Hear, hear. That's all I've been saying.", "You'll get no quarrel from me, {a}.", "They won't hear it from us. But it's so." };
        private static readonly string[] Parting = { "I'll sit somewhere else, then.", "Talk to me when you've got a mortgage.", "We'll see who's right when the note comes due." };

        /// The scene: "argue" when they fall out, and the tie cools as for any argument; "smalltalk" when they agree. Only between two men: nobody asked the women, which was rather the trouble.
        public static Scene Make(SceneCast c, string seed, int year)
        {
            if (c == null || c.AWoman || c.BWoman)
                return null;
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string sa = SideOf(WellOff(c, 0), c.Tempers != null ? c.Tempers[0] : null, seed + "a"), sb = SideOf(WellOff(c, 1), c.Tempers != null ? c.Tempers[1] : null, seed + "b");
            bool panic = year >= 1907, same = sa == sb;
            string[] open = panic ? (sa == Silver ? PanicAngry : PanicCalm) : (sa == Silver ? SilverOpen : GoldOpen);
            string[] answer = same ? Agree : panic ? (sb == Silver ? PanicAngryAnswer : PanicCalmAnswer) : (sb == Silver ? SilverAnswer : GoldAnswer);
            Scene s = new Scene { Kind = same ? "smalltalk" : "argue", Summary = same ? a + " and " + b + ", agreeing about the money question" : a + " and " + b + " fell out over the money question" };
            s.Add(0, Voice.Pick(seed + "|mo", open).Replace("{b}", b), same ? "chat" : "mild", "face");
            s.Add(1, Voice.Pick(seed + "|ma", answer).Replace("{a}", a), same ? "chat" : "angry");
            if (!same)
                s.Add(0, Voice.Pick(seed + "|mp", Parting), "storm", "walkaway");
            return s;
        }

        private static bool WellOff(SceneCast c, int i)
        {
            return c.WellOff != null && i < c.WellOff.Length && c.WellOff[i];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in new[] { SilverOpen, GoldOpen, SilverAnswer, GoldAnswer, PanicAngry, PanicCalm, PanicAngryAnswer, PanicCalmAnswer, Agree, Parting })
                foreach (string l in pool)
                    yield return l.Replace("{a}", "Amos").Replace("{b}", "Amos");
        }
    }
}
