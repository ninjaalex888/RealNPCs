// A husband's temper ([Encounters] DomesticDisputes; re-staged from the game's beat_domestic_dispute, where a man berates
// his wife outside their door in Rhodes or Annesburg and she sobs, cowers or runs: Staged.cs). Here it is one of the
// town's own men and his wife or sweetheart, over what is really on his mind (money, drink, another man, the life he
// thought he'd have). Each turn he chooses: rant at her, raise his hand, storm off, or ease off when he sees he's
// watched; she chooses: give it back, plead, sob, cower or walk off; a neighbour watches, steps in, fetches the law or
// walks on. His hand is rare (a man's hand against a woman is a tenth of the rate, a third for a hot, hard man in his
// cups, as in the quarrel ladder), once at most, and never more than a blow that knocks her down: once he has struck
// her he is fair game to the player. The bodies are the game's own clips for his rant (script_re@domestic_dispute),
// the proven sob, the hands up, the walk away. All the words are RealNPCs' own. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Domestic
    {
        public const string RhodesDict = "script_re@domestic_dispute@rhodes", AnnesburgDict = "script_re@domestic_dispute@annesburg";
        public static readonly string[] Dicts = { RhodesDict, AnnesburgDict };
        // his rant, in turn: the clips are body language (the game times them to its own lines, which we don't use)
        public static readonly string[] RantClips = { RhodesDict + "|head_in_clouds_husband", RhodesDict + "|live_on_moon_husband", AnnesburgDict + "|you_married_husband",
                                                      RhodesDict + "|grass_greener_husband", AnnesburgDict + "|shoulda_husband", RhodesDict + "|thats_rhodes_husband" };
        public static readonly string[] HusbandIds = { "rant", "raise", "storm", "ease" };
        public static readonly string[] WifeIds = { "argue", "plead", "sob", "cower", "leave" };
        public static readonly string[] NeighbourIds = { "watch", "stepin", "law", "walkon" };

        /// What's eating him, from his worry: money, drink, rival or life.
        public static string MatterOf(string worry, bool drunk)
        {
            switch (worry)
            {
                case "debt": case "bank": case "cards": case "savings": case "drought": return "money";
                case "drink": return "drink";
                case "courting": case "secret": return "rival";
                default: return drunk ? "drink" : "life";
            }
        }

        /// The hand's chance this turn, percent (the quarrel ladder's rule for a man against a woman).
        public static int HandChance(StagedMind m)
        {
            bool hard = m.Temper == "Hothead" || m.Temper == "Cold";
            return hard && m.Drunk ? 33 : 10;
        }

        /// His turn. watched: the player close and in view, or the law in sight; struck: he has already (never twice).
        public static Staged.Weights Husband(StagedMind m, string matter, int turn, bool watched, bool struck, string handSeed)
        {
            var w = new Staged.Weights();
            switch (m.Temper)
            {
                case "Hothead": w.Add("rant", 5).Add("raise", 3).Add("storm", 2).Add("ease", 1); break;
                case "Proud": w.Add("rant", 5).Add("raise", 1).Add("storm", 3).Add("ease", 1); break;
                case "Cold": w.Add("rant", 3).Add("raise", 2).Add("storm", 3).Add("ease", 2); break;
                case "Greedy": w.Add("rant", 4).Add("raise", 1).Add("storm", 2).Add("ease", 2); break;
                case "Coward": w.Add("rant", 3).Add("raise", 0).Add("storm", 3).Add("ease", 3); break;
                case "Loyal": w.Add("rant", 2).Add("raise", 0).Add("storm", 2).Add("ease", 5); break;
                default: w.Add("rant", 4).Add("raise", 1).Add("storm", 2).Add("ease", 2); break;
            }
            if (matter == "money")
                w.Add("rant", 2);
            if (m.Drunk)
                w.Add("raise", 2).Add("rant", 1).Add("ease", -1);
            if (m.Mood == "sore")
                w.Add("raise", 1);
            if (m.Mood == "glad")
                w.Add("ease", 2);
            if (watched)
                w.Add("ease", 3).Add("raise", -3);
            if (turn >= 1)
                w.Add("storm", 2).Add("ease", 1);
            if (turn >= 2)
                w.Add("rant", -2).Add("storm", 2);
            w.Clamp();
            if (struck || !Voice.Chance(handSeed + "|hand", HandChance(m)) || (m.Temper == "Coward" || m.Temper == "Loyal") && !m.Drunk)
                w.W[w.Ids.IndexOf("raise")] = 0;   // the hand is rare, and once at most
            if (w.Of("storm") == 0)
                w.Add("storm", 1);
            if (w.Possible < 2)
                w.Add("ease", 1);
            return w;
        }

        /// Her turn, after what he did.
        public static Staged.Weights Wife(StagedMind m, string his, bool struck)
        {
            var w = new Staged.Weights().Add("argue", 3).Add("plead", 2).Add("sob", 2).Add("cower", 1).Add("leave", 2);
            switch (m.Temper)
            {
                case "Hothead": case "Proud": w.Add("argue", 3).Add("cower", -1); break;
                case "Coward": w.Add("cower", 3).Add("plead", 2).Add("argue", -2); break;
                case "Loyal": w.Add("plead", 2).Add("leave", -1); break;
                case "Cold": w.Add("leave", 3).Add("sob", -1); break;
                case "Greedy": w.Add("argue", 1); break;
            }
            if (his == "raise" || struck)
                w.Add("cower", 3).Add("leave", 4).Add("sob", 2).Add("argue", -3);
            if (his == "storm")
                w.Add("sob", 2).Add("argue", 1).Add("cower", -2);
            if (his == "ease")
                w.Add("plead", 1).Add("cower", -1);
            if (m.Mood == "sore")
                w.Add("argue", 1);
            w.Clamp();
            if (w.Of("leave") == 0)
                w.Add("leave", 1);   // she can always walk away
            return w;
        }

        /// Someone named near: tieWife their warmth to her, lawNear a lawman within reach to fetch.
        public static Staged.Weights Neighbour(StagedMind m, int tieWife, bool struck, bool lawNear)
        {
            var w = new Staged.Weights().Add("watch", 4).Add("stepin", 1).Add("law", lawNear ? 1 : 0).Add("walkon", 2);
            switch (m.Temper)
            {
                case "Loyal": w.Add("stepin", 3); break;
                case "Hothead": w.Add("stepin", 2); break;
                case "Coward": w.Add("walkon", 2).Add("stepin", -1).Add("law", lawNear ? 1 : 0); break;
                case "Cold": case "Greedy": w.Add("walkon", 2).Add("stepin", -1); break;
            }
            if (tieWife >= 1)
                w.Add("stepin", 2 + tieWife);
            if (struck)
                w.Add("stepin", 2).Add("law", lawNear ? 3 : 0);
            return w.Clamp();
        }

        public static string Words(string role, string id, string him, string her)
        {
            if (role == "husband")
                switch (id)
                {
                    case "rant": return "goes on at " + her + ", louder";
                    case "raise": return "loses his temper and strikes her";
                    case "storm": return "throws up his hands and storms off";
                    default: return "sees folks looking, and lets it drop";
                }
            if (role == "wife")
                switch (id)
                {
                    case "argue": return "gives as good as she gets";
                    case "plead": return "begs " + him + " to calm down";
                    case "sob": return "breaks down crying";
                    case "cower": return "shrinks back from him";
                    default: return "turns her back on him and walks away";
                }
            switch (id)
            {
                case "watch": return "stays where they are and watches";
                case "stepin": return "goes over and tells " + him + " to leave her be";
                case "law": return "goes to fetch the law";
                default: return "goes on about their business";
            }
        }

        public static string Now(string role, string him, string her, string matter)
        {
            string m = matter == "money" ? "money" : matter == "drink" ? "his drinking" : matter == "rival" ? "another man" : "the life they have";
            if (role == "husband")
                return "you are shouting at " + her + " outside your door over " + m + ", the street looking on";
            if (role == "wife")
                return him + " is shouting at you outside your door over " + m;
            return him + " is shouting at " + her + " outside their door, a few steps from you";
        }

        // ---- the words ------------------------------------------------------------------------------------------

        public static string Rant(string matter, string her, string seed)
        {
            switch (matter)
            {
                case "money": return Voice.Pick(seed + "dr", "Where does it go, " + her + "? I bring it home and it's gone by Thursday!", "You think money grows out there? I break my back for it!", "Another bolt of cloth? With what we owe?");
                case "drink": return Voice.Pick(seed + "dr", "Don't you count my glasses, woman. Don't you dare.", "A man can't have one drink without a sermon at his own door!", "I'll drink what I like. I earned it.");
                case "rival": return Voice.Pick(seed + "dr", "I saw how he looked at you. I saw how you looked back.", "Don't lie to me. The whole street knows his name.", "Smiling at him in the store like a girl. I saw it.");
                default: return Voice.Pick(seed + "dr", "This ain't the life I was promised. You ain't the woman I was promised.", "Head in the clouds, always. Somebody has to live down here.", "Same supper, same faces, same look on you.");
            }
        }

        public static string Raise(string seed)
        {
            return Voice.Pick(seed + "dh", "That's enough out of you!", "Shut your mouth!", "I said enough!");
        }

        public static string Storm(string seed)
        {
            return Voice.Pick(seed + "ds", "I'm going to the saloon. Don't wait up.", "To hell with this house.", "I can't stand the sight of it.");
        }

        public static string Ease(string seed)
        {
            return Voice.Pick(seed + "de", "Get inside. We'll talk inside.", "Folks are looking. Let it be.", "Fine. Fine. Not out here.");
        }

        public static string Her(string choice, string him, string seed)
        {
            switch (choice)
            {
                case "argue": return Voice.Pick(seed + "dw", "Don't you shout at me in the street, " + him + "!", "Me? Look at yourself before you look at me!", "You married me with your eyes open.");
                case "plead": return Voice.Pick(seed + "dw", "Please, " + him + ". Not out here. Please.", "Hush now. Folks can hear.", "I'm sorry. Whatever it is, I'm sorry.");
                case "sob": return Voice.Pick(seed + "dw", "Why do you have to be like this?", "I can't. I can't do this anymore.");
                case "cower": return Voice.Pick(seed + "dw", "Don't. Don't you touch me.", "Stay back. Please stay back.");
                default: return Voice.Pick(seed + "dw", "I'm going to my sister's.", "Shout at the walls, then.", "I won't stand here and listen to it.");
            }
        }

        public static string FromNeighbour(string choice, string him, string seed)
        {
            switch (choice)
            {
                case "stepin": return Voice.Pick(seed + "dn", "That's enough, " + him + ". Leave her be.", "Easy now. Go cool your head.", "Hey! Not on this street you don't.");
                case "law": return Voice.Pick(seed + "dn", "I'm getting the sheriff.", "Somebody fetch a deputy!");
                default: return null;
            }
        }

        public static string LawWord(string him, string seed)
        {
            return Voice.Pick(seed + "dl", "Go home, " + him + ". Sleep it off, or sleep in a cell.", "That's enough. Go on, walk it off before I make you.");
        }

        /// To the player who walks up: backing off, or rounding on him with words (never a gun).
        public static string ToYou(bool backs, string seed)
        {
            return backs ? Voice.Pick(seed + "dy", "This is between me and my wife, mister. We're done.", "All right. All right. It's over.")
                         : Voice.Pick(seed + "dy", "Mind your own, stranger. She's my wife.", "You want some of this? Go on, walk away.");
        }

        public static string Thanks(bool struck, string seed)
        {
            return struck ? Voice.Pick(seed + "dt", "Thank you. He's not always like this. He's not.", "Bless you, mister. I didn't know what he'd do next.")
                          : Voice.Pick(seed + "dt", "Thank you. I'm sorry you had to see that.", "I'm obliged. He'll be calmer by supper.");
        }

        public static string Thought(string role, string matter, string seed)
        {
            if (role == "husband")
                return Voice.Pick(seed + "dk", "Why won't she just listen.", "Everybody's staring. Let them.", "Didn't mean to start. Can't stop now.");
            return Voice.Pick(seed + "dk", "Not again. Not out here.", "The whole street hearing it.", "Eyes on the ground. Wait for the storm to blow over.");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 8; i++)
            {
                string s = "s" + i;
                foreach (string m in new[] { "money", "drink", "rival", "life" })
                {
                    yield return Rant(m, "Ada", s);
                    yield return Now("husband", "Walt", "Ada", m);
                    yield return Now("wife", "Walt", "Ada", m);
                    yield return Now("neighbour", "Walt", "Ada", m);
                    yield return Thought(i % 2 == 0 ? "husband" : "wife", m, s);
                }
                yield return Raise(s);
                yield return Storm(s);
                yield return Ease(s);
                foreach (string c in WifeIds)
                    yield return Her(c, "Walt", s);
                yield return FromNeighbour("stepin", "Walt", s);
                yield return FromNeighbour("law", "Walt", s);
                yield return LawWord("Walt", s);
                yield return ToYou(i % 2 == 0, s);
                yield return Thanks(i % 2 == 0, s);
            }
        }
    }
}
