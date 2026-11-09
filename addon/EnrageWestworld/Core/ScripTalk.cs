// Scrip at Annesburg (TENHOUR R2; roadmap.md's idea bank, "Company town"; built on the street's chance meetings). Two men in the company's town by day, and one says what everyone's thinking about being paid in the company's own paper. The other agrees, hushes him, or lets him know that it'll be heard of.
// The meeting at the pithead and the man who'd organise it are already on the road (Core/Wayside.Labour.cs, the union organiser in Wayside.Range.cs). Why it reads true: fear in a company town isn't of the mine, it's of who's standing next to you. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ScripTalk
    {
        public const string Town = "Annesburg";

        private static readonly string[] Grumble =
        {
            "Paper with the company's name on it, {b}, and the store takes it at ninety cents.",
            "I dug eleven tons last week, {b}, and I owe them money.",
            "They weigh it on their scale, pay in their paper, sell at their store. What's left for a man?",
            "My boy's nine, {b}, and he's picking slate for scrip.",
        };
        private static readonly string[] Agree = { "Aye. And the rent comes out before you ever see it.", "Ninety cents if you're lucky. It was eighty-five at Christmas.", "Don't I know it." };
        private static readonly string[] Hush = { "Keep your voice down, {a}. For the love of God.", "Not here. Not with him standing there.", "I didn't hear that, and neither did you." };
        private static readonly string[] Warn = { "The foreman likes to know who's unhappy, {a}.", "You want your name on a list? Go on talking.", "There's men been let go for less, {a}. With a family in the house." };
        private static readonly string[] AfterWarn = { "Is that a threat, or are you just afraid?", "I see. So it's like that.", "...Forget I spoke." };

        /// The scene, kind "scrip": by the seed the other agrees 40, hushes him 40, warns him 20. Between two men, by day, in the company's town.
        public static Scene Make(SceneCast c, string seed)
        {
            if (c == null || c.AWoman || c.BWoman)
                return null;
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            int how = (Voice.Hash(seed + "|scrip") & 0x7fffffff) % 100;
            Scene s = new Scene { Kind = "scrip", Summary = "two men had words about being paid in scrip" };
            s.Add(0, Voice.Pick(seed + "|g", Grumble).Replace("{b}", b), "chatbad", "face");
            if (how < 40)
                s.Add(1, Voice.Pick(seed + "|a", Agree), "tut");
            else if (how < 80)
                s.Add(1, Voice.Pick(seed + "|h", Hush).Replace("{a}", a), "mild");
            else
                s.Add(1, Voice.Pick(seed + "|w", Warn).Replace("{a}", a), "mild").Add(0, Voice.Pick(seed + "|w2", AfterWarn), "angry", "walkaway");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Grumble, Agree, Hush, Warn, AfterWarn })
                foreach (string l in p)
                    yield return l.Replace("{a}", "Amos").Replace("{b}", "Amos");
        }
    }
}
