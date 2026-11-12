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
        [DataMember(EmitDefaultValue = false)] public bool Test;          // a Testing row's (the kin thanks row, SEAMS item 23): it leaves no debt
        [DataMember(EmitDefaultValue = false)] public long DoneAt;
        [DataMember(EmitDefaultValue = false)] public string How;        // said to your face, never reached you, never found you
        [DataMember(EmitDefaultValue = false)] public string DoerId, DoerName;   // a townsman who did the wrong (FIVEDAY item 32b; [Scenes] KinTownWrongs): the kin come for him, not you; null for yours
    }

    public static class KinGrudges
    {
        /// The men of a family who take a wrong up (Family.PickRelation's men; a nephew isn't among them).
        public static readonly string[] Takers = { "brother", "father", "son", "cousin", "uncle" };
        public static int DueLeastDays { get { return SessionClock.Days(5, 1); } }   // "a week later" (as built)   // the session clock (DEPTH item 16): SessionClock.Days(as built, a session's)
        public static int DueMostDays { get { return SessionClock.Days(9, 2); } }
        public static int WaitsDays { get { return SessionClock.Days(7, 2); } }   // after the due day, a week (as built) to find you in one of the two towns   // the session clock (DEPTH item 16): SessionClock.Days(as built, a session's)
        public const string Mark = "kin";                      // once for each person wronged (PersonState.Mark)
        public const string TownMark = "kintown";              // once for each person wronged by a townsman (FIVEDAY item 32b)
        public const int TownChance = 40;                      // percent: a family hears and sends a man (friends near take most of these up)

        /// The wrongs between townsfolk a family takes up (FIVEDAY item 32b): a beating, a hand in the pocket. Grudges.Wrongs
        /// has the words for both.
        public static bool TownWrong(string tieKind) { return tieKind == "rough" || tieKind == "pickpocket"; }

        /// The kin's first words to the townsman, before the friends' confrontation lines (who he is, from where).
        public static string TownIntro(KinGrudge k, string seed)
        {
            string first = Person.FirstOf(k.VictimName ?? "my kin"), rel = k.Relation ?? "kin";
            return k.Town != null && Voice.Chance(seed + "|ti", 50)
                ? Voice.Pick(seed + "|tf", "I'm " + first + "'s " + rel + ". Rode in from " + k.Town + " for this.", "Name's " + Person.FirstOf(k.Name) + ". " + first + "'s " + rel + ", from " + k.Town + ".")
                : Voice.Pick(seed + "|tn", "I'm " + first + "'s " + rel + ". You'll have heard of me by now.", "Take a good look. I'm " + first + "'s " + rel + ", and I rode a long way.");
        }

        /// The notice after (you saw it, or were near).
        public static string TownNotice(KinGrudge k, string town)
        {
            return k.Name + ", " + Person.FirstOf(k.VictimName ?? "someone") + "'s " + (k.Relation ?? "kin") + ", came to " + (town ?? "town") + " and had it out with " + (k.DoerName ?? "the man") + ".";
        }

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

        // ---- the kin of one you saved (SEAMS item 7; [Scenes] KinThanks) -------------------------------------------------------
        // The kin's walk-up was only ever for a wrong: shoot a man's brother's friend, rob his cousin, and a kinsman comes
        // to say it to your face. Save that cousin and nobody of his came. Now, one time in two, a kinsman of someone you
        // saved comes the same way, a few days on, to thank you for him. Gratitude carried by kin is the oldest kind of standing a
        // stranger gets in a town: he is now owed by a family, not a man.
        public const string Thanked = "kinthanks";   // the KinGrudge's Wrong for a thanks; also the once-a-person mark
        public static bool ThanksOn = true;          // from the settings
        public const int ThanksChance = 50;          // percent: a family hears and sends a man to thank you

        /// Whether this one comes to thank you, not about a wrong.
        public static bool IsThanks(KinGrudge k) { return k != null && k.Wrong == Thanked; }

        /// What he says, walked up to your face: who he is, what you did for his kin, and a close by his nature.
        public static string ThanksOpening(KinGrudge k, string you, string seed)
        {
            string first = Person.FirstOf(k.VictimName ?? "my kin");
            string open = you != null ? Voice.Pick(seed + "|to", "You'd be " + you + ". I hoped I'd find you.", "Mr. " + you + "? I've been looking for you since Tuesday.")
                                      : Voice.Pick(seed + "|to", "You're the one. I was told the look of you.", "Hold up, mister. I think you're who I'm after.");
            string who = "I'm " + first + "'s " + (k.Relation ?? "kin") + ".";
            string did = Voice.Pick(seed + "|td", "You got " + first + " out of a bad spot the other week.", "You stood up for " + first + " when nobody else would.");
            string close;
            switch (k.Temper)
            {
                case "Hothead": close = Voice.Pick(seed + "|tc", "Anybody gives you trouble here, you send for me.", "You ever need a hand in a fight, you've got mine."); break;
                case "Proud": close = Voice.Pick(seed + "|tc", "My family pays what it owes. I'll not forget it.", "We're not people who take a thing like that and say nothing."); break;
                case "Cold": close = Voice.Pick(seed + "|tc", "I'm not one for speeches. I came to shake your hand.", "That's all. I wanted you to know we know."); break;
                default: close = Voice.Pick(seed + "|tc", "The whole family's grateful. I came to say it for them.", "Our door's open to you, any time you're by."); break;
            }
            return open + " " + who + " " + did + " " + close;
        }

        /// The voiced set for the walk-up: thanks for a thanks, else the wrong's (the hot threaten, the rest are angry).
        public static string VoiceOf(KinGrudge k)
        {
            return IsThanks(k) ? "thanks" : VoiceFor(k != null ? k.Temper : null);
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
                case "tryst": return "took my wife off somewhere quiet";   // a husband who found out (Core/Engine.Trysts.cs; item 15)
                case SecondActs.Watched: return "sat your horse and watched them shoot him";   // an execution you found and never stepped into (FOURTEENHOUR reserve R3)
                default: return "wronged him";
            }
        }

        /// What they say, walked up to his face: who they are, the wrong, and a close by their nature. `you`: his surname
        /// when the one wronged knew it (the kin had it from them), else nothing.
        public static string Opening(KinGrudge k, string you, string seed)
        {
            if (IsThanks(k))
                return ThanksOpening(k, you, seed);
            string first = Person.FirstOf(k.VictimName ?? "my kin");
            string open = you != null ? Voice.Pick(seed + "|o", "You'd be " + you + ".", "Mr. " + you + ". I've been hunting the town for you.", "They said you'd be " + you + ". You are.")
                                      : Voice.Pick(seed + "|o", "You're the one.", "I've had you pointed out to me.", "It's you. I was told the look of you.");
            string who = "I'm " + first + "'s " + (k.Relation ?? "kin") + ".";
            string wrong = "You " + (k.WrongWord ?? "wronged him") + " the other week.";
            string close;
            if (k.Wrong == SecondActs.Watched)
                return open + " " + who + " " + wrong + " " + SecondActs.WatchedClose(k.Temper, seed);   // he's dead: no 'do it again' (reserve R3)
            switch (k.Temper)
            {
                case "Hothead":
                    close = Voice.Pick(seed + "|c", "Do it again and it's me you'll answer to.", "Next time you'll have me to deal with, and I don't scare.", "Try it again and I'll come back with more than words.");
                    break;
                case "Proud":
                    close = Voice.Pick(seed + "|c", "I wanted a look at the man who'd do that to my own. Now I've had it.", "I came a long way to see your face. It's about what I expected.", "That's my own you were dealing with. I'd have you remember it.");
                    break;
                case "Cold":
                    close = Voice.Pick(seed + "|c", "That's all I came to say. I'll know your face now.", "I don't make threats. I make a note. This is the note.", "I'll not waste more breath on it. We've met now.");
                    break;
                default:
                    close = Voice.Pick(seed + "|c", "They'd not have told you themselves. So I'm telling you.", "Some don't speak up for themselves. I do.", "I said I'd say it to your face, and that's done.");
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
            if (k.Wrong == SecondActs.Watched)
                return SecondActs.WatchedThought(first, seed);   // (reserve R3)
            if (IsThanks(k))
                return Voice.Pick(seed + "|tt", "Said it. " + first + " will want to hear how he took it.", "Quiet sort. Shook my hand like he meant it.", "Didn't know what to say back. Men like that never do.");
            return Voice.Pick(seed + "|t", "Said it. Said it to his face. Now I can go home to " + first + ".", "That's the man. Smaller than I'd pictured. They always are.", "He looked at me like I was weather. Well. He heard it.", "Came a long way for one sentence. It was the right sentence.");
        }

        /// The notice to the player after the words.
        public static string Notice(KinGrudge k, string town)
        {
            if (IsThanks(k))
                return k.Name + ", " + Person.FirstOf(k.VictimName ?? "someone") + "'s " + (k.Relation ?? "kin") + ", found you in " + (town ?? "town") + " to thank you for what you did for " + (k.WrongWord != null && k.WrongWord.EndsWith(" her") ? "her" : "him") + ".";
            return k.Name + ", " + Person.FirstOf(k.VictimName ?? "someone") + "'s " + (k.Relation ?? "kin") + ", found you in " + (town ?? "town") + " on his account.";
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 8; i++)
            {
                var tk = new KinGrudge { VictimName = "Amos Teale", Relation = i % 2 == 0 ? "brother" : "cousin", Name = "Hiram Teale", Town = i < 4 ? "Strawberry" : null, DoerName = "Seth Cornwallis" };
                yield return TownIntro(tk, "k" + i);
                yield return TownNotice(tk, "Valentine");
            }
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
            for (int i = 0; i < 4; i++)
                yield return Opening(new KinGrudge { VictimName = "Edith Teale", Relation = "husband", WrongWord = WrongWord("tryst"), Temper = i == 0 ? "Hothead" : i == 1 ? "Proud" : i == 2 ? "Cold" : null, Name = "Amos Teale" }, "Morgan", "h" + i);
            foreach (string temper in new[] { "Hothead", "Proud", "Cold", null })
                for (int i = 0; i < 4; i++)
                {
                    var tk = new KinGrudge { VictimName = "Amos Teale", Relation = "brother", Wrong = Thanked, Temper = temper, Name = "Hiram Teale" };   // the thanks (SEAMS item 7)
                    yield return Opening(tk, i % 2 == 0 ? "Morgan" : null, "th" + i);
                    yield return Thought(tk, "tt" + i);
                    yield return Notice(tk, "Valentine");
                }
        }
    }
}
