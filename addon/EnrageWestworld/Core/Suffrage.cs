// The vote (TENHOUR item 16; roadmap.md's idea bank, "Suffrage meeting"; built on the temperance ladies' street scene). In Saint Denis, by day: a woman on her way to a meeting says what she thinks to a man she knows, and he takes it his own way.
// By 1899 Wyoming, Colorado, Utah and Idaho had given women the vote, and nothing had fallen down. Why it reads true: an argument about somebody's rights goes differently when the person is standing in front of you and knows your mother. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Suffrage
    {
        public const string Town = "Saint Denis";

        private static readonly string[] Case =
        {
            "Wyoming's had the vote a generation, {b}, and the sky's still up.",
            "There's a meeting at the hall on Thursday, {b}. I mean to be at it.",
            "I pay the same taxes you do, {b}, and I've the same sense.",
            "Colorado. Utah. Idaho. Wyoming. It'll come here too, {b}.",
            "I keep the books for a business I can't vote on, {b}.",
            "You've got a vote, {b}. What do you do with it, may I ask?",
        };

        private static readonly string[] Forbid = { "Go home. That hall's no place for you.", "You'll get yourself talked about.", "I'd thank you to keep that to yourself." };
        private static readonly string[] Laugh = { "And who'd get the supper on?", "A vote! What'll you want next, a cigar?", "Bless you, you've been reading again." };
        private static readonly string[] Shaken = { "...My mother used to say the same.", "I never thought of it that way.", "Thursday, you said. Well. I'm not saying anything." };
        private static readonly string[] Agrees = { "Go on, then. You'd do it better than half of them.", "I'll tell you what, you've got my vote. Such as it is.", "High time. Don't tell them I said so." };
        private static readonly string[] AnswerToForbid = { "We'll see about that.", "You always did know best.", "Talked about. Yes. That's rather the idea." };
        private static readonly string[] AnswerToLaugh = { "The same woman who always has.", "I'll tell your wife you asked.", "Laugh. They laughed in Wyoming." };
        private static readonly string[] AnswerKind = { "There. That didn't hurt.", "I'll hold you to that.", "Tell your friends. Quietly, if you must." };

        /// The scene, kind "suffrage": A or B the woman, whichever she is; by the seed he forbids it 35, laughs 30, is shaken 20, agrees 15. Only between a man and a woman.
        public static Scene Make(SceneCast c, string seed)
        {
            if (c == null || c.AWoman == c.BWoman)
                return null;
            int her = c.AWoman ? 0 : 1, him = 1 - her;
            string b = Scenes.N(c, him, seed);
            int how = (Voice.Hash(seed + "|vote") & 0x7fffffff) % 100;
            Scene s = new Scene { Kind = "suffrage", Summary = "a woman said what she thought about the vote, to a man in the street" };
            s.Add(her, null, null, "approach", 200).Add(her, Voice.Pick(seed + "|case", Case).Replace("{b}", b), "mild", "face");
            if (how < 35)
                s.Add(him, Voice.Pick(seed + "|f", Forbid), "angry").Add(her, Voice.Pick(seed + "|af", AnswerToForbid), "mild", "walkaway");
            else if (how < 65)
                s.Add(him, Voice.Pick(seed + "|l", Laugh), "mock").Add(her, Voice.Pick(seed + "|al", AnswerToLaugh), "mild", "walkaway");
            else if (how < 85)
                s.Add(him, Voice.Pick(seed + "|s", Shaken), "mild").Add(her, Voice.Pick(seed + "|ak", AnswerKind), "thanks", "walkaway");
            else
                s.Add(him, Voice.Pick(seed + "|g", Agrees), "thanks").Add(her, Voice.Pick(seed + "|ak", AnswerKind), "thanks", "walkaway");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Case)
                yield return l.Replace("{b}", "Amos");
            foreach (string[] pool in new[] { Forbid, Laugh, Shaken, Agrees, AnswerToForbid, AnswerToLaugh, AnswerKind })
                foreach (string l in pool)
                    yield return l;
        }
    }
}
