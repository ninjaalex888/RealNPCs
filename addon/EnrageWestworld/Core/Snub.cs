// The well-off and the working man (FOURTEENHOUR item 18; [Scenes] Snubs). The game's own "wealthy couple" (a gentleman and a lady
// walking out in Saint Denis or Rhodes) and the working men about them, as a theme. A working man (a hand, a labourer, a miner, the
// rough sort) asks a gentleman something in the street, or brushes past him, and is brushed off; he answers by his nature (takes it,
// sneers, or answers back); the gentleman's pride (who he is, and the marshal he can call); and when a lady is beside him, her word ends it (they
// walk on together) or makes it worse, by her nature. With no lady to take him away, a hot working man and a proud gentleman can come
// to blows, and the working men near lean to their own (BrawlSides.QuarrelTie, "class"); the well-off near never join a brawl, they
// send for the law (item 6). The snub is the gentleman's doing (a tie colder, the working man's memory of it). Already there and left
// alone: a beggar asking the well-off for a coin (the "beg" reason: charity or scorn), the money argument between the fixed and the
// owing (Core/MoneyTalk.cs), the opinions' contempt and envy (Core/Opinions.cs), the strain's brush-off between any two
// (Core/Exchanges.cs, seen twice in Lex's log). Why it reads true: contempt from above and envy from below were the period's two
// sharpest feelings (the trusts, the bank failures of '93, a gentleman's word against a labourer's at law), and a snub in the street is
// where they meet; a man in front of his wife is twice as proud, and a woman's word is often what ends men's quarrels. Plain C#, no
// game; all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class Snub
    {
        public static bool On = true;   // [Scenes] Snubs, from the settings
        public const int Strength = 44, HaughtyPercent = 14, OtherPercent = 5;

        static bool WorkingClass(string g) { return g == "working" || g == "farm" || g == "rough" || g == "trapper"; }
        static bool Haughty(string t) { return t == "Proud" || t == "Cold" || t == "Greedy"; }

        /// x, a working man, and y, a gentleman, in the street by day: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, bool night)
        {
            if (!On || saloon || night || x == null || y == null || x.Id == y.Id || x.Woman || y.Woman || x.WellOff || !y.WellOff || x.P == null || !WorkingClass(InnerLife.TradeGroup(x.P.Trade)))
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|snub", Haughty(y.Temper) ? HaughtyPercent : OtherPercent))
                return null;
            return new Reason { Kind = "snub", Strength = Strength, Why = x.Name + ", a working man, comes up against " + y.Name + ", a gentleman" };
        }

        static bool Hot(string t) { return t == "Hothead" || t == "Proud"; }

        /// A, the working man; B, the gentleman; C, a lady beside him when c.HasCrowd and c.CWoman (the game side casts her).
        public static Scene Play(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            bool lady = c.HasCrowd && c.CWoman;
            string ta = c.Tempers != null ? c.Tempers[0] : null, tb = c.Tempers != null ? c.Tempers[1] : null, tc = c.Tempers != null ? c.Tempers[2] : null;
            string ask = Voice.Pick(seed + "|ask", "Pardon me, sir. Have you the time?", "Beg pardon, mister. Any work going at your place?", "Excuse me, sir. Which way to the bank?", "Sorry, sir. Didn't see you there.");
            string off = Voice.Pick(seed + "|off", "Out of my way.", "Go and ask someone else.", "Don't put your hand on me, man.", "Do I know you?");
            // A answers by his nature: answers back (the hot), sneers (the cold and the grasping), or takes it
            string answer = Hot(ta) ? "back" : ta == "Cold" || ta == "Greedy" || ta == "Drifter" ? "sneer" : "takes";
            Scene s = new Scene { Kind = "snub", Doer = 1, Summary = lady ? "a gentleman and his lady snubbed a working man in the street" : "a gentleman snubbed a working man in the street" };
            s.Add(0, ask, "mild", "face").Add(1, off, "mock");
            if (answer == "takes")
            {
                s.Add(0, Voice.Pick(seed + "|meek", "Yes, sir. Sorry, sir.", "No offence meant.", "I'll not trouble you, sir."), "scared");
                Leave(s, lady, seed);
                s.Cast = lady ? 3 : 2;
                return s;
            }
            s.Add(0, answer == "back"
                    ? Voice.Pick(seed + "|back", "My money's as good as yours, mister.", "I asked you civil.", "Fine coat. Men like me paid for it.")
                    : Voice.Pick(seed + "|sneer", "Course you don't. Too busy counting.", "Mind your boots, then. They cost more than I make.", "Lord forbid you'd speak to the help."), "angry");
            s.Add(1, Voice.Pick(seed + "|pride", lady ? "Not in front of my wife, you lout." : "Have you any notion who I am?", "Mind your tongue, or I'll have the marshal on you.", "I owe your sort no answer."), "angry");
            if (lady)
            {
                // her word, by her nature: the kind and the steady end it; the proud and the cold make it worse; either way they go on
                bool ends = tc != "Proud" && tc != "Cold" && (tc == "Loyal" || tc == "Plain" || Voice.Chance(seed + "|ends", 60));
                if (ends)
                {
                    s.Beats.Add(new Beat(2, Voice.Pick(seed + "|lady", "Come away, " + b + ". People are looking.", "Leave it, dear. He's not worth your trouble.", b + ". Please. Not here."), "mild", "face") { To = 1 });
                    s.Add(1, Voice.Pick(seed + "|yes", "Quite right, my dear.", "As you like.", "Yes. Come along."), "mild");
                }
                else
                {
                    s.Beats.Add(new Beat(2, Voice.Pick(seed + "|ladyworse", "Really, " + b + ". These people.", "Must we stand here while it talks?", "I told you we should have taken the carriage."), "mock", "face") { To = 1 });
                    s.Add(0, Voice.Pick(seed + "|maam", "Ma'am.", "And a fine afternoon to you, ma'am.", "Lovely manners. Both of you."), "mock");
                }
                Leave(s, true, seed);
                s.Cast = 3;
                return s;
            }
            // no lady to take him away: a hot man answering back to a proud or hot gentleman can come to blows
            if (answer == "back" && (Hot(tb) || tb == "Cold") && tb != "Coward" && Voice.Chance(seed + "|blows", Hot(tb) ? 60 : 30))
            {
                s = new Scene { Kind = "brawl", Fight = true, NoLadder = true, Over = "class", Summary = "a working man and a gentleman came to blows in the street" };
                s.Add(0, ask, "mild", "face").Add(1, off, "mock")
                 .Add(0, Voice.Pick(seed + "|back", "My money's as good as yours, mister.", "I asked you civil.", "Fine coat. Men like me paid for it."), "angry")
                 .Add(1, Voice.Pick(seed + "|push", "Stand aside, I said.", "Hands off, you brute.", "Know your station, man."), "angry", "shove")
                 .Add(0, Voice.Pick(seed + "|swing", "Learn this.", "Right, then, sir.", "My place is right here."), "angry", "fight", 9000)
                 .Add(0, null, null, "endfight");
                s.Cast = 2;
                return s;
            }
            s.Add(0, Voice.Pick(seed + "|mutter", "Swell.", "Go on, then. Run along home.", "One day, mister. One day."), "mock");
            Leave(s, false, seed);
            s.Cast = 2;
            return s;
        }

        /// The gentleman goes on his way: with his lady beside him (the Director's leavewith: both to one spot, away from A), or alone.
        static void Leave(Scene s, bool lady, string seed)
        {
            if (lady)
            {
                s.Beats.Add(new Beat(2, null, null, "leavewith") { To = 0 });
                s.Beats.Add(new Beat(1, null, null, "leavewith") { To = 0 });
            }
            else
                s.Add(1, null, null, "walkaway");
        }
    }
}
