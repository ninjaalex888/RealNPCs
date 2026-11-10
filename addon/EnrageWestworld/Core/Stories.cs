// A story over days (FOURTEENHOUR reserve R10; TWELVEHOUR item 15; roadmap's "Narratives that grow out of worries"; [World] Stories).
// The worries already ran their arcs in people's thoughts, and the letters, the walk-ups, the trains and the mine whistle were all
// there; nothing made one of them a thing for you to do over days. Now the first of the roadmap's narratives, offered without Talk:
//   The son at the mine. A father whose son went up to the Annesburg mines (his "son" worry, at its worse days or its crisis) writes
//   to you: the letter waits at his town's post office ("They say in Valentine you're a man who gets things done..."). Ride to
//   Annesburg, and the son finds you (a walk-up: a townsman cast for him, the kin's way); J: carry his letter home, or tell
//   him to take the next train. Carried, it's done when you reach his father's town; the train brings him home a day or two on. Either
//   way the father's worry is settled (it ends well, his own lines say so), he thanks you by letter, and the town has the word. The
//   world doesn't wait: ten days and it lapses, and the father remembers who never came back; the mine whistle doesn't wait either
//   (an accident at Annesburg while you haven't found him, and one time in three he's among the names).
// At most two open; one a father. Who cut the fence, the roadmap's second, waits: the fence cutters' night has no runner on screen to
// stand on (Core/FenceCutters.cs is told, not staged). Why it reads true: a small favour that grows is how people let you into their
// lives, and a deadline you didn't set is how the world shows it doesn't wait for you. Plain C#, no game; all words this add-on's.
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class StoryRun
    {
        [DataMember(EmitDefaultValue = false)] public string Id, Kind;                    // Kind: "son"
        [DataMember(EmitDefaultValue = false)] public string AskerId, AskerName, AskerTown;
        [DataMember(EmitDefaultValue = false)] public string SonName, SonId;              // the son as met (a resident cast for him)
        [DataMember(EmitDefaultValue = false)] public int Stage;                          // 0 asked, 1 found, 2 done
        [DataMember(EmitDefaultValue = false)] public string Choice, Outcome;             // carry, train; home, carried, lapsed, whistle
        [DataMember(EmitDefaultValue = false)] public long At, DueAt, HomeAt;             // asked, lapses, the train home
    }

    public static class Stories
    {
        public static bool On = true;   // [World] Stories, from the settings
        public const string Kind = "story";
        public const string SonTown = "Annesburg", SonPlace = "the mines at Annesburg";
        public const int OfferPercent = 12, MostOpen = 2, DueDays = 10, WhistlePercent = 33, PromptMs = 12000;

        public static bool Open(StoryRun s) { return s != null && s.Stage < 2 && s.Outcome == null; }

        /// The father's letter.
        public static string SonLetter(string you, string father, string son, string town, string seed)
        {
            string to = you != null ? "Mr. " + you : "Sir";
            return Voice.Pick(seed + "|sl", to + ",\n\nThey say in " + town + " you're a man who gets things done. My boy " + son + " went up to the mines at Annesburg, and I've had no word from him since the spring. If your road takes you that way, would you look for him, and tell him his father asks after him?\n\n" + father,
                                            to + ",\n\nForgive a stranger writing. My son " + son + " is working the mine at Annesburg, or was. Not a line from him in months. I'm too old for the road. If you pass that way, find him for me.\n\nYours in hope,\n" + father);
        }

        /// The son, walked up to you in Annesburg.
        public static string SonMeets(string father, string fatherTown, string son, string seed)
        {
            string fa = Person.FirstOf(father ?? "my father");
            return Voice.Pick(seed + "|sm", "You've come from " + (fatherTown ?? "home") + "? From my father? ...Lord. I'm " + Person.FirstOf(son) + ". I meant to write. Every Sunday I meant to.",
                                            "Somebody said a stranger was asking after " + Person.FirstOf(son) + ". That's me. Is he well? Is " + fa + " well?");
        }

        public static string SonAnswer(string choice, string seed)
        {
            return choice == "carry" ? Voice.Pick(seed + "|sa1", "Wait. Let me write it now. ...There. Tell him I'm sorry it took a stranger.", "Here. Give him this. It says more than I could.")
                                     : Voice.Pick(seed + "|sa2", "The train. You're right. There's one Thursday. I'll be on it.", "Home. I'll draw my pay and go home.");
        }

        /// The father's thanks by letter, once it's done.
        public static string Thanks(string you, string son, string how, string seed)
        {
            string to = you != null ? "Mr. " + you : "Friend";
            return how == "carried" ? Voice.Pick(seed + "|tc", to + ",\n\nYou brought my boy's words to my door. I've read them till the paper's soft. God keep you.", to + ",\n\n" + Person.FirstOf(son) + "'s letter came by your hand. I'll not forget it.")
                                    : Voice.Pick(seed + "|tt", to + ",\n\n" + Person.FirstOf(son) + " came home on the train Thursday, thin as a rail and grinning. You did that.", to + ",\n\nMy son is home. That's all I can write without the ink running.");
        }

        /// The town's word of how it ended.
        public static string News(string father, string son, string town, string how)
        {
            string fa = Person.FirstOf(father ?? "A father"), so = Person.FirstOf(son ?? "his boy");
            switch (how)
            {
                case "home": return fa + "'s boy " + so + " is home from the Annesburg mine. A stranger found him, they say.";
                case "carried": return "A stranger brought " + fa + " a letter from his boy at the Annesburg mine. First word in months.";
                case "whistle": return "Among the names that came up the Annesburg shaft: " + so + ". His father in " + (town ?? "town") + " had asked a stranger to find him.";
                default: return null;
            }
        }
    }
}
