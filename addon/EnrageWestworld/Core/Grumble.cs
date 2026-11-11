// Old hands grumble at the newcomer (DEPTH reserve R5; [Scenes] Grumble, on; FOURTEENHOUR item 16's greenhorn, the part it left).
// A regular of the town (not a hothead: he'd say it to the man's face, and the greenhorn's ribbing is that) and a friend of his
// (a warm tie, within 8 m of him: the game side finds him) talk about the greenhorn off the train between themselves, where he
// can hear. He answers by his nature: a dry word back that wins them over a little (a tie a step warmer), a hot one they shrug
// off, or nothing, and he goes (both colder, the old hand's doing). With no friend by, the game side plays the ribbing instead.
// Why it reads true: old-timers measure a newcomer among themselves before they ever speak to him, and the talk is meant to be
// overheard. Men only, as the ribbing. Plain C#; no new game call (the Director's three-man scene, as the limp's tale).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Grumble
    {
        public static bool On = true;   // [Scenes] Grumble, from the settings (the harnesses never mark a greenhorn, so nothing changes there)
        public const string Kind = "grumble";
        public const int Strength = 47, Percent = 25;   // over the ribbing (46): when the old hands are at it, it's this; with no friend by, the ribbing

        /// x, an old hand, grumbles about y, the greenhorn, to a friend of his: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay)
        {
            if (!On || x == null || y == null || x.Id == y.Id || !y.Greenhorn || x.Greenhorn || x.Woman || y.Woman || x.Temper == "Hothead")
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|grumble", Percent))
                return null;
            return new Reason { Kind = Kind, Strength = Strength, Why = x.Name + " has a few words about the greenhorn, " + y.Name + ", for a friend's ear" };
        }

        /// How the greenhorn answers by his temper: "dry" (a word back that wins them a little), "hot" (a sharp one they shrug off), "none".
        public static string Answer(string temper, string seed)
        {
            int r = (Voice.Hash(seed + "|ans") & 0x7fffffff) % 100;
            if (temper == "Hothead" || (temper == "Proud" && r < 70))
                return "hot";
            if (temper == "Coward" || (temper == "Greedy" && r < 50) || (temper == "Cold" && r < 40))
                return "none";
            return r < 65 ? "dry" : "none";
        }

        private static readonly string[] Opens =
        {
            "Another one off the train. Look at the shine on them boots.",
            "See the new man? Hands like a bank clerk's.",
            "There's the fellow come to make his fortune here.",
            "That one bought his hat the day before he left home, you can tell.",
        };
        private static readonly string[] Seconds =
        {
            "He won't last the winter.", "Give him a month. The first norther'll send him back east.",
            "I seen a dozen like him. None of them stayed.", "He'll learn. Or he won't, and he'll go.",
        };
        private static readonly string[] Thirds =
        {
            "Wonder what he thinks he's going to do out here.", "Somebody ought to tell him about the mud in March.",
            "Asked me where a man buys land. Buys it!", "Couldn't saddle a horse if you gave him all day.",
        };
        private static readonly string[] Dry =
        {
            "I can hear you, gentlemen. The boots will scuff. Give them a week.",
            "The winter's welcome to try. I've buried worse than a norther.",
            "You two keep talking. I'm learning the country from you.",
        };
        private static readonly string[] DryBack =
        {
            "Hm. Ears on him, anyhow.", "Well. Maybe he'll do.", "Ha. All right, then. We'll see you in March.",
        };
        private static readonly string[] Hot =
        {
            "If you've something to say, say it to me.", "I've ears, you know. And I don't care for your tone.",
            "Talk about a man behind his back where he can hear, is it? That's the custom here?",
        };
        private static readonly string[] HotBack =
        {
            "Nobody asked your opinion, son.", "Touchy. They're all touchy at first.", "Easy, now. Nobody's said a thing that isn't so.",
        };
        private static readonly string[] Quiet =
        {
            "Didn't say a word, did he.", "There he goes.", "Thought so.",
        };

        /// The scene: A the old hand, B the greenhorn, C (index 2) the old hand's friend. A and C talk between themselves; B answers or goes.
        public static Scene Make(SceneCast c, string seed)
        {
            string how = Answer(c != null && c.Tempers != null ? c.Tempers[1] : null, seed);
            var s = new Scene { Kind = how == "dry" ? "welcomed" : "scorn", Doer = how == "dry" ? -1 : 0 };
            s.Beats.Add(new Beat(0, Pick(Opens, seed + "|o"), "mock") { To = 2 });
            s.Beats.Add(new Beat(2, Pick(Seconds, seed + "|s"), "mild") { To = 0 });
            s.Beats.Add(new Beat(0, Pick(Thirds, seed + "|t"), "mock") { To = 2 });
            switch (how)
            {
                case "dry":
                    s.Add(1, Pick(Dry, seed + "|d"), "mild", "face").Add(0, Pick(DryBack, seed + "|db"), "cheer");
                    s.Summary = "two old hands talked over the greenhorn where he could hear, and he answered them dry enough to win them a little";
                    break;
                case "hot":
                    s.Add(1, Pick(Hot, seed + "|h"), "angry", "face").Add(0, Pick(HotBack, seed + "|hb"), "mock").Add(1, null, null, "walkaway");
                    s.Summary = "two old hands talked over the greenhorn where he could hear; he bristled, and they shrugged it off";
                    break;
                default:
                    s.Add(1, null, null, "walkaway");
                    s.Beats.Add(new Beat(2, Pick(Quiet, seed + "|q"), "mock") { To = 0 });
                    s.Summary = "two old hands talked over the greenhorn where he could hear, and he went off without a word";
                    break;
            }
            s.Cast = 3;
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Opens, Seconds, Thirds, Dry, DryBack, Hot, HotBack, Quiet })
                foreach (string l in a)
                    yield return l;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
