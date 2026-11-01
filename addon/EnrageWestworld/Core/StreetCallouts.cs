// Called out before the street ([Encounters] PublicCallouts; re-staged from the game's beat_town_confrontation, whose
// townsman hails a man across the street and has it out with him in front of whoever is about: Staged.cs). Here it
// is between two of the town's own people with something real between them (a debt, a theft, a beating, a woman,
// hard words), never strangers: one calls the other over and says it out loud, the other answers by his nature (lets
// it go by, owns to it, gives it back, squares up), the first answers that, and those standing near watch, jeer, step
// in or walk off, each by their own. The bodies are the game's own clips for that encounter (script_re@town_confront,
// ten a set: callover, confront, to_crowd, taunt, threat, ignore, positive, negative and the two to the player), with
// the street's standing gestures when a set won't load. No guns, no fists of its own: two who square up are handed to
// the street's quarrel (its ladder keeps the fists as they are). All the words are RealNPCs' own. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class StreetCallouts
    {
        public const string MaleDict = "script_re@town_confront@male@barfight", ColdDict = "script_re@town_confront@male@grays", WomanDict = "script_re@town_confront@female@drunk_duel";
        public static readonly string[] Dicts = { MaleDict, ColdDict, WomanDict };
        public const string CallClip = "callover", ConfrontClip = "confront", ToYouGood = "positive_plyr_player", ToYouBad = "negative_plyr_player";

        // the accuser's answers, the target's, an onlooker's
        public static readonly string[] AccuserIds = { "accept", "to_crowd", "taunt", "threat", "walkoff" };
        public static readonly string[] TargetIds = { "ignore", "positive", "negative", "threat" };
        public static readonly string[] OnlookerIds = { "watch", "jeer", "stepin", "walkoff" };

        /// Whose clips: a woman's set, a cold man's, any other man's.
        public static string DictFor(bool woman, string temper)
        {
            return woman ? WomanDict : temper == "Cold" ? ColdDict : MaleDict;
        }

        /// The clip each choice plays (null: none, they just go).
        public static string ClipOf(string choice)
        {
            switch (choice)
            {
                case "accept": return "positive";
                case "to_crowd": case "taunt": case "threat": case "ignore": case "positive": case "negative": return choice;
                default: return null;
            }
        }

        /// The tone for the street's gestures when the clip can't play, and for the voiced line.
        public static string ToneOf(string choice)
        {
            switch (choice)
            {
                case "accept": case "positive": return "yes";
                case "to_crowd": case "negative": return "angry";
                case "taunt": case "jeer": return "mock";
                case "threat": return "threat";
                case "ignore": return "no";
                case "walkoff": return "storm";
                case "stepin": return "intervene";
                default: return "mild";
            }
        }

        /// What lies between them, in a word, from their tie and his worry: debt, theft, honour, woman, words or feud.
        public static string MatterOf(string tieKind, bool courted, bool debtWorry)
        {
            if (debtWorry || tieKind == "debt")
                return "debt";
            switch (tieKind)
            {
                case "pickpocket": case "lift": return "theft";
                case "rough": case "brawl": case "drunk": case "standoff": return "honour";
                case "jealous": case "cheat": return courted ? "woman" : "theft";
                case "argue": case "snap": case "x:snap": case "scorn": case "brushoff": case "faced": case "passby": return courted ? "woman" : "words";
                default: return courted ? "woman" : "feud";
            }
        }

        /// The one with the grievance calls (the other "did it", else the hotter head); never a coward unless he's owed money.
        public static bool Calls(StagedMind a, StagedMind b, bool aWasWronged, string matter)
        {
            if (a.Temper == "Coward" && matter != "debt")
                return false;
            if (aWasWronged)
                return true;
            return Staged.Heat(a.Temper) >= Staged.Heat(b.Temper);
        }

        /// How the one called out answers. Letting it go by is always possible, and so is at least one other.
        public static Staged.Weights Target(StagedMind m, string matter)
        {
            var w = new Staged.Weights();
            switch (m.Temper)
            {
                case "Hothead": w.Add("ignore", 1).Add("positive", 1).Add("negative", 4).Add("threat", 5); break;
                case "Proud": w.Add("ignore", 2).Add("positive", 1).Add("negative", 5).Add("threat", 2); break;
                case "Cold": w.Add("ignore", 5).Add("positive", 1).Add("negative", 2).Add("threat", 1); break;
                case "Greedy": w.Add("ignore", 3).Add("positive", 1).Add("negative", 3).Add("threat", 1); break;
                case "Coward": w.Add("ignore", 3).Add("positive", 5).Add("negative", 1).Add("threat", 0); break;
                case "Loyal": w.Add("ignore", 2).Add("positive", 4).Add("negative", 2).Add("threat", 1); break;
                default: w.Add("ignore", 3).Add("positive", 3).Add("negative", 2).Add("threat", 1); break;
            }
            if (matter == "debt" && m.Temper != "Greedy")
                w.Add("positive", 2);   // owing is owing
            if (m.Mood == "sore")
                w.Add("negative", 1).Add("threat", 1);
            if (m.Mood == "glad")
                w.Add("positive", 1).Add("threat", -1);
            if (m.Drunk)
                w.Add("threat", 2);
            if (m.Shamed != null)
                w.Add("negative", 2);   // slighted lately and not answered: this one he answers
            if (m.Warmth <= -3)
                w.Add("threat", 1).Add("positive", -1);
            if (m.Woman)
                w.Add("threat", -2).Add("negative", 1);
            w.Clamp();
            if (w.Of("ignore") == 0)
                w.Add("ignore", 1);
            if (w.Possible < 2)
                w.Add("negative", 1);
            return w;
        }

        /// How the one who called answers what he got.
        public static Staged.Weights Accuser(StagedMind m, string answer)
        {
            var w = new Staged.Weights();
            switch (m.Temper)
            {
                case "Hothead": w.Add("accept", 1).Add("to_crowd", 2).Add("taunt", 3).Add("threat", 5).Add("walkoff", 1); break;
                case "Proud": w.Add("accept", 2).Add("to_crowd", 5).Add("taunt", 3).Add("threat", 2).Add("walkoff", 1); break;
                case "Cold": w.Add("accept", 2).Add("to_crowd", 1).Add("taunt", 1).Add("threat", 4).Add("walkoff", 2); break;
                case "Greedy": w.Add("accept", 3).Add("to_crowd", 3).Add("taunt", 1).Add("threat", 3).Add("walkoff", 2); break;
                case "Coward": w.Add("accept", 4).Add("to_crowd", 2).Add("taunt", 1).Add("threat", 0).Add("walkoff", 4); break;
                case "Loyal": w.Add("accept", 4).Add("to_crowd", 3).Add("taunt", 1).Add("threat", 1).Add("walkoff", 3); break;
                default: w.Add("accept", 3).Add("to_crowd", 2).Add("taunt", 2).Add("threat", 1).Add("walkoff", 2); break;
            }
            switch (answer)
            {
                case "positive": w.Add("accept", 4).Add("threat", -2).Add("taunt", -1); break;
                case "ignore": w.Add("accept", -9).Add("to_crowd", 2).Add("taunt", 1); break;
                case "negative": w.Add("accept", -9).Add("threat", 1).Add("walkoff", 1); break;
                case "threat": w.Add("accept", -9).Add("threat", 2).Add("walkoff", 2).Add("to_crowd", -1); break;
            }
            if (m.Mood == "sore")
                w.Add("threat", 1);
            if (m.Mood == "glad")
                w.Add("walkoff", 1);
            if (m.Drunk)
                w.Add("taunt", 2);
            w.Clamp();
            if (w.Of("walkoff") == 0)
                w.Add("walkoff", 1);   // he can always say his piece and go
            return w;
        }

        /// Someone standing near: tieA/tieB their warmth to each of the two (0 for strangers).
        public static Staged.Weights Onlooker(StagedMind m, int tieA, int tieB)
        {
            var w = new Staged.Weights();
            switch (m.Temper)
            {
                case "Hothead": w.Add("watch", 2).Add("jeer", 4).Add("stepin", 2).Add("walkoff", 1); break;
                case "Proud": w.Add("watch", 3).Add("jeer", 2).Add("stepin", 2).Add("walkoff", 1); break;
                case "Cold": w.Add("watch", 3).Add("jeer", 1).Add("stepin", 0).Add("walkoff", 3); break;
                case "Greedy": w.Add("watch", 4).Add("jeer", 2).Add("stepin", 0).Add("walkoff", 2); break;
                case "Coward": w.Add("watch", 3).Add("jeer", 0).Add("stepin", 0).Add("walkoff", 4); break;
                case "Loyal": w.Add("watch", 2).Add("jeer", 0).Add("stepin", 4).Add("walkoff", 1); break;
                default: w.Add("watch", 4).Add("jeer", 1).Add("stepin", 1).Add("walkoff", 2); break;
            }
            int friend = tieA > tieB ? tieA : tieB;
            if (friend >= 1)
                w.Add("stepin", 2 + friend).Add("jeer", -2);
            if (m.Drunk)
                w.Add("jeer", 2);
            if (m.Woman)
                w.Add("jeer", -1);
            return w.Clamp();
        }

        /// The options in words, for the model.
        public static string Words(string role, string id, string a, string b, string matter)
        {
            string m = MatterWords(matter);
            if (role == "target")
                switch (id)
                {
                    case "ignore": return "pays " + a + " no mind and walks on";
                    case "positive": return "owns up to it and offers to make it right";
                    case "negative": return "gives it straight back to " + a;
                    default: return "squares up to " + a + " and dares him on";
                }
            if (role == "accuser")
                switch (id)
                {
                    case "accept": return "takes it, and lets the matter rest";
                    case "to_crowd": return "turns to the folks watching and tells them about " + m;
                    case "taunt": return "mocks " + b + " to his face";
                    case "threat": return "steps in close and warns " + b + " what comes next";
                    default: return "has had his say, and leaves him standing there";
                }
            switch (id)
            {
                case "watch": return "stands and watches how it goes";
                case "jeer": return "jeers at them and eggs them on";
                case "stepin": return "steps between them and tells them to cool off";
                default: return "wants no part of it and walks on";
            }
        }

        private static string MatterWords(string matter)
        {
            switch (matter)
            {
                case "debt": return "the money owed";
                case "theft": return "what was taken";
                case "honour": return "the beating";
                case "woman": return "the woman";
                case "words": return "what was said";
                default: return "the bad blood";
            }
        }

        /// What is happening, for the model ("Right now:").
        public static string Now(string role, string a, string b, string matter)
        {
            string m = MatterWords(matter);
            if (role == "accuser")
                return "you called " + b + " out in the street over " + m + ", with folks looking on";
            if (role == "target")
                return a + " has called you out in the street over " + m + ", in front of everybody";
            return a + " has called " + b + " out in the street over " + m + ", a few steps from you";
        }

        // ---- the words ------------------------------------------------------------------------------------------

        public static string CallOver(string b, string matter, string seed)
        {
            switch (matter)
            {
                case "debt": return Voice.Pick(seed + "sc", b + "! Over here. Let's talk about my money where folks can hear.", "Hold up, " + b + ". You and I have a sum to settle, and it's been long enough.");
                case "theft": return Voice.Pick(seed + "sc", b + "! Don't you walk past me like nothing happened.", "Stop right there, " + b + ". You know full well what went missing.");
                case "honour": return Voice.Pick(seed + "sc", b + ". You and me ain't done, not by half.", "Hey! " + b + "! Look at me when I call you.");
                case "woman": return Voice.Pick(seed + "sc", b + "! I'll have a word about her, and I'll have it now.", "You hear me, " + b + "? Come here. This is about her.");
                case "words": return Voice.Pick(seed + "sc", b + "! Say it again. Go on, say it out here.", "Hold on, " + b + ". You said some things. Come say them again.");
                default: return Voice.Pick(seed + "sc", b + "! I've had about all of you I can stand.", "Hey, " + b + "! Don't pretend you can't hear me.");
            }
        }

        public static string Confront(string b, string matter, string seed)
        {
            switch (matter)
            {
                case "debt": return Voice.Pick(seed + "sf", "Every week it's next week with you. I'm done with next week.", "You shook on it, " + b + ". Your hand's worth nothing, is that it?");
                case "theft": return Voice.Pick(seed + "sf", "Light fingers, this one. Tell them. Tell them what you did.", "I ought to fetch the sheriff. Maybe I still will.");
                case "honour": return Voice.Pick(seed + "sf", "Put me in the dirt and walked off laughing. Laugh now.", "You think folks forgot? I didn't forget.");
                case "woman": return Voice.Pick(seed + "sf", "Stay clear of her. That's the last time I ask nice.", "I saw you two. Don't you lie to me.");
                case "words": return Voice.Pick(seed + "sf", "Brave enough when I ain't in the room. Go on, try it now.", "You've got a mouth on you, " + b + ". Let's see what's behind it.");
                default: return Voice.Pick(seed + "sf", "Years of this. Years. It ends today.", "Everybody here knows what you are, " + b + ".");
            }
        }

        public static string Answer(string choice, string a, string matter, string seed)
        {
            switch (choice)
            {
                case "ignore": return Voice.Pick(seed + "sa", "Not today, " + a + ".", "Go home and sleep it off, " + a + ".", "Take your hollering elsewhere, " + a + ".");
                case "positive":
                    return matter == "debt" ? Voice.Pick(seed + "sa", "All right. All right! You'll have it Friday. Half now, if you want.", "You're right. I owe you. I'll make it good, " + a + ".")
                         : Voice.Pick(seed + "sa", "Maybe I had that coming. I'm sorry, " + a + ".", "Fair enough. I was wrong. There, I said it.");
                case "negative": return Voice.Pick(seed + "sa", "You've a nerve, " + a + ". Look at yourself first.", "Everybody here knows you for a liar, " + a + ".", "That's rich, coming from you.");
                default: return Voice.Pick(seed + "sa", "You want to settle it? Then let's settle it.", "Step up, then. Let's see you mean it.", "Careful, " + a + ". Real careful.");
            }
        }

        public static string Reply(string choice, string b, string seed)
        {
            switch (choice)
            {
                case "accept": return Voice.Pick(seed + "sr", "See that you do. Everybody heard you.", "Fine. That's all I wanted to hear.");
                case "to_crowd": return Voice.Pick(seed + "sr", "You all hear that? Remember his face.", "Look at him, folks. That's the man.", "I want every one of you to know what he is.");
                case "taunt": return Voice.Pick(seed + "sr", "Look at you. Can't even look me in the eye.", "Run along, " + b + ". Like you always do.");
                case "threat": return Voice.Pick(seed + "sr", "Next time it won't be words, " + b + ".", "You'll answer for it. One way or another.");
                default: return Voice.Pick(seed + "sr", "I've said what I came to say.", "Go on and rot, then.", "Not worth my breath.");
            }
        }

        public static string FromOnlooker(string choice, string seed)
        {
            switch (choice)
            {
                case "jeer": return Voice.Pick(seed + "so", "Go on! Don't just stand there!", "Ha! He's got you there!", "Somebody's getting a hiding.");
                case "stepin": return Voice.Pick(seed + "so", "That's enough, the both of you.", "Not out here, boys. Take it somewhere else.", "Easy now. Nobody needs to get hurt.");
                default: return null;
            }
        }

        /// The one who called, to the player walking up: warmer or colder by what he thinks of him.
        public static string ToYou(bool good, string seed)
        {
            return good ? Voice.Pick(seed + "sy", "No, you're right, mister. This ain't the place.", "All right. I'll let it lie. For now.")
                        : Voice.Pick(seed + "sy", "This don't concern you, stranger.", "Keep walking. It ain't your business.");
        }

        /// What goes through their heads (over them).
        public static string Thought(string role, string other, string matter, string seed)
        {
            if (role == "accuser")
                return Voice.Pick(seed + "st", "Let them all see it. Let them see what he is.", "Hands shaking. Keep your voice steady.", "No more swallowing it.");
            if (role == "target")
                return Voice.Pick(seed + "st", "Of all days. Of all streets.", "Of all the places to do this.", "Keep calm. Don't give him the satisfaction.");
            return Voice.Pick(seed + "st", "Always the two of them.", "This'll be all over town by supper.", "Somebody ought to do something.");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (string m in new[] { "debt", "theft", "honour", "woman", "words", "feud" })
                for (int i = 0; i < 8; i++)
                {
                    string s = "s" + i;
                    yield return CallOver("Hiram", m, s);
                    yield return Confront("Hiram", m, s);
                    foreach (string t in TargetIds)
                        yield return Answer(t, "Walt", m, s);
                    yield return Now("accuser", "Walt", "Hiram", m);
                    yield return Now("target", "Walt", "Hiram", m);
                    yield return Now("onlooker", "Walt", "Hiram", m);
                    yield return Thought(i % 3 == 0 ? "accuser" : i % 3 == 1 ? "target" : "onlooker", "Hiram", m, s);
                }
            for (int i = 0; i < 8; i++)
            {
                string s = "s" + i;
                foreach (string c in AccuserIds)
                    yield return Reply(c, "Hiram", s);
                yield return FromOnlooker("jeer", s);
                yield return FromOnlooker("stepin", s);
                yield return ToYou(i % 2 == 0, s);
            }
        }
    }
}
