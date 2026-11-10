// The newcomer tested (FOURTEENHOUR item 16; [Scenes] Greenhorn). A greenhorn off the train (NewFaces: a settler, a preacher, a
// drummer, a stranger come to stay; his first two weeks) is ribbed by a regular in the street about his boots, his hat, his city
// ways. He answers by his nature: laughs it off and gives as good (welcomed: a tie, the regular stands him a drink in his words),
// squares up (a fistfight, and the street's own rules from there: the stand-up may step in, joiners may come, the winner is talked
// of), or takes it and goes off red in the face (scorned). After, he belongs here a little more, with a tie to show for it either way, and
// it's told round town after (the scene's summary). Why it reads true: initiation (Aronson and Mills, 1959): the harder the way in, the
// more a man belongs once he's in; a town tests a newcomer before it takes him, and the test is half of the welcome. Men only:
// the town's teasing of a new schoolteacher or a bride was another thing. Plain C#, no game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class Greenhorn
    {
        public static bool On = true;   // [Scenes] Greenhorn, from the settings
        public const int Strength = 46, HotPercent = 35, OtherPercent = 15;

        /// x, a regular, ribs y, a greenhorn: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, bool night)
        {
            if (!On || x == null || y == null || x.Id == y.Id || !y.Greenhorn || x.Greenhorn || x.Woman || y.Woman || x.Temper == "Coward" || x.Temper == "Loyal")
                return null;
            bool hot = x.Temper == "Hothead" || x.Temper == "Proud" || x.Temper == "Greedy";
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|greenhorn", hot ? HotPercent : OtherPercent))
                return null;
            return new Reason { Kind = "greenhorn", Strength = Strength, Why = x.Name + " has a word for the greenhorn, " + y.Name };
        }

        /// The ribbing: A, the regular; B, the newcomer, answers by his temper (c.Tempers[1]).
        public static Scene Rib(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string t = c.Tempers != null ? c.Tempers[1] : null;
            string how = t == "Hothead" || (t == "Proud" && Voice.Chance(seed + "|proud", 60)) ? "fights" : t == "Coward" || (t == "Greedy" && Voice.Chance(seed + "|meek", 40)) ? "takes" : "laughs";
            string[][] ribs =
            {
                new[] { "Look at the boots on him. Fresh off the train, are you?", "Fresh enough. And yours look like they walked here from Kansas." },
                new[] { "Where'd you get that hat, mister? Out of a catalogue?", "Same place you got your manners, I expect." },
                new[] { "City man. Bet he can't tell a mule from a mare.", "The mule's the one that looks like you, isn't it?" },
                new[] { "You're the new fellow. Lost already, I'll bet.", "Not lost. Just looking for somebody worth talking to." }
            };
            string[] r = ribs[(Voice.Hash(seed + "|rib") & 0x7fffffff) % ribs.Length];
            Scene s;
            switch (how)
            {
                case "laughs":
                    s = new Scene { Kind = "welcomed", Summary = "a regular ribbed the greenhorn, and the greenhorn gave as good" };
                    s.Add(0, r[0], "mock", "face").Add(1, r[1], "mock")
                     .Add(0, Voice.Pick(seed + "|ha", "Ha! He'll do. Buy you a drink sometime, greenhorn.", "Ha! All right, all right. Welcome to town.", "Well, he's got a tongue on him. He'll fit in."), "cheer")
                     .Add(1, Voice.Pick(seed + "|ok", "I'll hold you to that drink.", "Obliged. I think.", "Glad to be here. Mostly."), "yes");
                    break;
                case "takes":
                    s = new Scene { Kind = "scorn", Doer = 0, Summary = "a regular made sport of the greenhorn in the street" };
                    s.Add(0, r[0], "mock", "face").Add(1, Voice.Pick(seed + "|meek", "Now, now. I mean no offence.", "I'll just be going.", "Excuse me, I have an errand."), "scared")
                     .Add(0, Voice.Pick(seed + "|scorn", "Then run along home to your mother.", "Off he goes. Mind the horse apples, city boy.", "Didn't think so."), "mock")
                     .Add(1, null, null, "walkaway");
                    break;
                default:
                    s = new Scene { Kind = "brawl", Fight = true, NoLadder = true, Summary = "the greenhorn wouldn't take a regular's ribbing, and it came to blows" };
                    s.Add(0, r[0], "mock", "face").Add(1, Voice.Pick(seed + "|hot", "Say that again. Slowly.", "You want to try that a second time?", "I didn't come two thousand miles to be laughed at."), "angry")
                     .Add(0, Voice.Pick(seed + "|push", "Oh, he's touchy. Go on home, greenhorn.", "Touchy, ain't he.", "What'll you do about it?"), "mock", "shove")
                     .Add(1, Voice.Pick(seed + "|swing", "This.", "Right, then.", "You'll see."), "angry", "fight", 9000)
                     .Add(0, null, null, "endfight");
                    break;
            }
            s.Cast = 2;
            return s;
        }
    }
}
