// Grudges that travel, the last piece (SIXDAY item 14; roadmap 20): "someone wronged tells their kin, and a brother takes it
// up: the trouble comes back around a week later, from someone the wrongdoer never met." A wrong of the player's on a
// living person the add-on knows (a threat, a beating, a theft, a trouble turned the wrong way on purpose: the same wrongs
// a friend takes up and an apology mends) reaches one of the men of their family (Core/Family.cs: a brother, a father, a
// son, a cousin, an uncle), who comes to the town five to nine days on, walks up to him and says it to his face, by his
// own nature: the hot threaten, the proud wanted a look, the cold say little, the plain only tell him. Once for each
// person wronged; nothing violent from our side, as the grudge rule has it. The man is a resident from then on (he came
// to town and stays a while), so he may be met again and knows the face. Why it reads true: in 1899 a wrong done to a man
// was his family's business, and the one who came about it was often someone you'd never seen. Plain C#, no game; all
// words original to this add-on.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class KinGrudge
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string VictimId, VictimName, VictimTown;
        [DataMember(EmitDefaultValue = false)] public string Wrong;       // the happening's kind (threat, faced, harm, deed:<kind>)
        [DataMember(EmitDefaultValue = false)] public string WrongWord;   // ...in the kin's words ("robbed him")
        [DataMember(EmitDefaultValue = false)] public long At, DueAt;    // the wrong, and the day they come
        [DataMember(EmitDefaultValue = false)] public string Name, Relation, Town, Temper;   // the kin (Core/Family.cs's relative), their town, their seeded temper
        [DataMember(EmitDefaultValue = false)] public string CastId;     // the resident they became when a body was cast (one body with the name)
        [DataMember(EmitDefaultValue = false)] public bool Done;
        [DataMember(EmitDefaultValue = false)] public long DoneAt;
        [DataMember(EmitDefaultValue = false)] public string How;        // said to your face, never reached you, never found you
    }

    public static class KinGrudges
    {
        /// The men of a family who take a wrong up (Family.PickRelation's men; a nephew isn't among them).
        public static readonly string[] Takers = { "brother", "father", "son", "cousin", "uncle" };
        public const int DueLeastDays = 5, DueMostDays = 9;   // "a week later"
        public const int WaitsDays = 7;                        // after the due day, a week to find you in one of the two towns
        public const string Mark = "kin";                      // once for each person wronged (PersonState.Mark)

        /// Whether this wrong of the player's is one a family takes up: never a killing (the avengers ride for those), never
        /// a wrong on the dead.
        public static bool Counts(string kind)
        {
            if (kind == null)
                return false;
            if (kind == "threat" || kind == "faced" || kind == "harm")
                return true;
            if (!kind.StartsWith("deed:", StringComparison.Ordinal))
                return false;
            string deed = kind.Substring(5);
            if (deed == "murder" || deed == "kill" || deed == "lawkill" || deed == "namedkill" || deed == "graverobbing")
                return false;
            string g = Minds.DeedGroup(deed);
            return g == "deed:violence" || g == "deed:theft";
        }

        /// The first man among their kin, or null (the women write letters; they don't come to the street over it).
        public static Relative Takes(IList<Relative> family)
        {
            if (family == null)
                return null;
            foreach (Relative r in family)
                if (r != null && !r.Woman && r.Name != null && Array.IndexOf(Takers, r.Relation) >= 0)
                    return r;
            return null;
        }

        public static long Due(long at, string seed)
        {
            return at + (DueLeastDays + Voice.Hash(seed + "|kindue") % (DueMostDays - DueLeastDays + 1)) * 1440L;
        }

        /// The kin's temper, seeded by their name: hot, proud, cold, or plain (null).
        public static string TemperOf(string name)
        {
            switch (Voice.Hash((name ?? "?") + "|kintemper") % 4)
            {
                case 0: return "Hothead";
                case 1: return "Proud";
                case 2: return "Cold";
                default: return null;
            }
        }

        /// The wrong in the kin's words ("You ... the other week").
        public static string WrongWord(string kind)
        {
            switch (kind)
            {
                case "threat": return "threatened him in the street";
                case "faced": return "faced him down";
                case "harm": return "turned his trouble the wrong way, and on purpose";
                case "deed:assault": return "put him in the dirt";
                case "deed:robbery": case "deed:theft": case "deed:looting": return "robbed him";
                case "deed:horsetheft": return "took his horse";
                default: return "wronged him";
            }
        }

        /// What they say, walked up to his face: who they are, the wrong, and a close by their nature. `you`: his surname
        /// when the one wronged knew it (the kin had it from them), else nothing.
        public static string Opening(KinGrudge k, string you, string seed)
        {
            string first = Person.FirstOf(k.VictimName ?? "my kin");
            string open = you != null ? Voice.Pick(seed + "|o", "You'd be " + you + ".", "Mr. " + you + ". I've been hunting the town for you.", "They said you'd be " + you + ". You are.")
                                      : Voice.Pick(seed + "|o", "You're the one.", "I've had you pointed out to me.", "It's you. I was told the look of you.");
            string who = "I'm " + first + "'s " + (k.Relation ?? "kin") + ".";
            string wrong = "You " + (k.WrongWord ?? "wronged him") + " the other week.";
            string close;
            switch (k.Temper)
            {
                case "Hothead":
                    close = Voice.Pick(seed + "|c", "Do it again and it's me you'll answer to, not him.", "Next time you'll have me to deal with, and I don't scare.", "Lay a hand on him again and I'll come back with more than words.");
                    break;
                case "Proud":
                    close = Voice.Pick(seed + "|c", "I wanted a look at the man who'd do that to him. Now I've had it.", "I came a long way to see your face. It's about what I expected.", "He's my blood. I'd have you remember that's who you were dealing with.");
                    break;
                case "Cold":
                    close = Voice.Pick(seed + "|c", "That's all I came to say. I'll know your face now.", "I don't make threats. I make a note. This is the note.", "I'll not waste more breath on it. We've met now.");
                    break;
                default:
                    close = Voice.Pick(seed + "|c", "He wouldn't have told you himself. So I'm telling you.", "He's not a man to speak up for himself. I am.", "I said I'd say it to your face, and that's done.");
                    break;
            }
            return open + " " + who + " " + wrong + " " + close;
        }

        /// The game's voiced line set that goes with it: the hot threaten, the rest are angry.
        public static string VoiceFor(string temper)
        {
            return temper == "Hothead" ? "threat" : "angry";
        }

        /// What they think after, over their head.
        public static string Thought(KinGrudge k, string seed)
        {
            string first = Person.FirstOf(k.VictimName ?? "him");
            return Voice.Pick(seed + "|t", "Said it. Said it to his face. Now I can go home to " + first + ".", "That's the man. Smaller than I'd pictured. They always are.", "He looked at me like I was weather. Well. He heard it.", "Came a long way for one sentence. It was the right sentence.");
        }

        /// The notice to the player after the words.
        public static string Notice(KinGrudge k, string town)
        {
            return k.Name + ", " + Person.FirstOf(k.VictimName ?? "someone") + "'s " + (k.Relation ?? "kin") + ", found you in " + (town ?? "town") + " on his account.";
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string temper in new[] { "Hothead", "Proud", "Cold", null })
                for (int i = 0; i < 3; i++)
                {
                    var k = new KinGrudge { VictimName = "Amos Teale", Relation = "brother", WrongWord = WrongWord(i == 0 ? "threat" : i == 1 ? "deed:assault" : "deed:robbery"), Temper = temper, Name = "Hiram Teale" };
                    yield return Opening(k, "Morgan", "a" + i);
                    yield return Opening(k, null, "b" + i);
                    yield return Thought(k, "t" + i);
                    yield return Notice(k, "Valentine");
                }
            foreach (string kind in new[] { "threat", "faced", "harm", "deed:assault", "deed:robbery", "deed:horsetheft", "deed:other" })
                yield return "You " + WrongWord(kind) + " the other week.";
        }
    }
}
