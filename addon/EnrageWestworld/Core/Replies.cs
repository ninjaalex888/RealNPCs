// Answering letters, and writing your own (SIXDAY item 8; roadmap 8; roadmap.md P14 and P16). A letter with a living writer
// can be answered once, from the reader: kindly, coldly, with a threat, with money, or asking to meet. The answer travels one
// to three game days by the post, and at the writer's end it changes what they do next: a grieving relative who was riding
// west may stand down, or ride sooner; a sweetheart warms or cools; a frightened host feels believed or mocked; the thankful
// stay grateful; and most write a short letter back, delivered like any other. Asked to meet, some wait at the station of
// their town on a set day, from ten till four. From any post town you can also write first, to anyone you've met: a warning,
// an apology, a love note, a threat or money, carried and taken the same way. Why it reads true: a letter is the one act of
// the player's that lands when he isn't there to see it, so the world answers in its own time and in writing, and the proud
// and the frightened answer the same words differently. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    /// An answer in the post (or a letter of the player's own), until it lands and after.
    [DataContract]
    public sealed class Reply
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string PaperId;    // the letter being answered; null when he wrote first
        [DataMember(EmitDefaultValue = false)] public string ToId;       // the writer's lasting id (null for a grieving relative, who is no person of the store's)
        [DataMember(EmitDefaultValue = false)] public string ToName;
        [DataMember(EmitDefaultValue = false)] public bool ToWoman;
        [DataMember(EmitDefaultValue = false)] public string Letter;     // the letter's kind: grief, thanks, love, scorned, confession, invitation, tip, plain; "own" for his
        [DataMember(EmitDefaultValue = false)] public string Tone;       // kind, cold, threat, money, meet; for his own: warning, apology, love, threat, money
        [DataMember(EmitDefaultValue = false)] public int Amount;        // cents sent
        [DataMember(EmitDefaultValue = false)] public string From;       // the post town it went from
        [DataMember(EmitDefaultValue = false)] public string Town;       // the writer's town, where a meet would be
        [DataMember(EmitDefaultValue = false)] public long SentAt, ArrivesAt;
        [DataMember(EmitDefaultValue = false)] public bool Done, Own;
        [DataMember(EmitDefaultValue = false)] public string Outcome;    // how it was taken, in a word (Effect.Note)
        [DataMember(EmitDefaultValue = false)] public long MeetAt;       // the game minute the writer starts waiting (10:00 of the day), 0 none
        [DataMember(EmitDefaultValue = false)] public bool Met;          // the meeting happened, or its day passed
        [DataMember(EmitDefaultValue = false)] public bool Effected;     // its effects at the writer's end applied once (a rewind re-lands the letter, never the effects)
        [DataMember(EmitDefaultValue = false)] public string CastId;     // the resident made to stand for a writer who is no person of ours, so a second cast finds the same one
    }

    public static class Replies
    {
        public static readonly string[] Tones = { "kind", "cold", "threat", "money", "meet" };
        public static readonly string[] OwnKinds = { "warning", "apology", "love", "threat", "money" };
        public static readonly string[] LetterKinds = { "grief", "thanks", "love", "scorned", "confession", "invitation", "tip", "plain" };
        public const int Money5 = 500, Money20 = 2000, OwnMoney = 500;
        public const int MeetFrom = 10, MeetTo = 16;       // the hours they wait
        public const int MeetDaysOn = 2;                   // the meet is this many days after the answer lands
        public const int MeetWaitDays = 14;                // ...and the writer comes to the station every day for this long (the letter may be read late)
        public const float NearMetres = 2500f;             // towns this near are a two-day post; farther, three
        public const int MostWriteTo = 8;                  // the people offered when he writes first

        /// What an answer leaves at the writer's end.
        public sealed class Effect
        {
            public bool StandDown, RideSooner, Meet, Silence;   // Silence: no letter back
            public int Affection, Heart, Awareness, Liking;
            public string Note;
        }

        /// The letter's kind: its own mark, else read off the title (letters from before the mark).
        public static string KindOf(Paper p)
        {
            if (p == null || p.Kind != "letter")
                return null;
            if (!string.IsNullOrEmpty(p.Letter))
                return p.Letter;
            string t = p.Title ?? "";
            if (t.IndexOf("wedding invitation", StringComparison.OrdinalIgnoreCase) >= 0) return "invitation";
            if (t.IndexOf("unsigned", StringComparison.OrdinalIgnoreCase) >= 0) return "confession";
            if (t.StartsWith("A letter from ", StringComparison.Ordinal) && t.IndexOf(',') > 0) return "grief";   // "A letter from <kin>, <town>"
            return "plain";
        }

        /// The writer's town, from the grief letter's title ("A letter from <kin>, <town>"), else null.
        public static string TownOf(Paper p)
        {
            if (p == null || KindOf(p) != "grief")
                return null;   // only a grief letter's title names the writer's town
            string t = p.Title;
            int i = t != null ? t.IndexOf(", ", StringComparison.Ordinal) : -1;
            return i > 0 && i + 2 < t.Length ? t.Substring(i + 2).Trim() : null;
        }

        /// Whether a letter can be answered: a letter with a writer (a person, or a grieving relative by name); no answer twice,
        /// and none of his own.
        public static bool Answerable(Paper p)
        {
            if (p == null || p.Kind != "letter" || !p.Delivered)
                return false;
            string k = KindOf(p);
            if (k == null || k == "answer" || k == "own")
                return false;
            return p.FromId != null || k == "grief";
        }

        /// Already answered.
        public static bool Answered(List<Reply> all, string paperId)
        {
            if (all == null || paperId == null)
                return false;
            foreach (Reply r in all)
                if (r != null && r.PaperId == paperId)
                    return true;
            return false;
        }

        /// The rows offered for a letter: by tone, the money rows greyed under the sum.
        public static List<InteractOption> Options(int cash)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "kind", Label = "Answer kindly" },
                new InteractOption { Id = "cold", Label = "Answer coldly" },
                new InteractOption { Id = "threat", Label = "Answer with a threat" },
                new InteractOption { Id = "money5", Label = "Send money ($5)", Why = cash < Money5 ? "not enough money" : null },
                new InteractOption { Id = "money20", Label = "Send money ($20)", Why = cash < Money20 ? "not enough money" : null },
                new InteractOption { Id = "meet", Label = "Ask to meet" },
            };
        }

        /// The rows offered when he writes first.
        public static List<InteractOption> OwnOptions(int cash)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "warning", Label = "A warning" },
                new InteractOption { Id = "apology", Label = "An apology" },
                new InteractOption { Id = "love", Label = "A love note" },
                new InteractOption { Id = "threat", Label = "A threat" },
                new InteractOption { Id = "money", Label = "Money ($" + OwnMoney / 100 + ")", Why = cash < OwnMoney ? "not enough money" : null },
            };
        }

        /// Days in the post between two towns: the same town a day, a near one two, a far one three.
        public static int Days(float metres)
        {
            return metres < 1f ? 1 : metres < NearMetres ? 2 : 3;
        }

        /// The game minute the writer starts waiting: ten o'clock, MeetDaysOn days after the answer lands.
        public static long MeetAt(long landed)
        {
            return (landed / 1440 + MeetDaysOn) * 1440 + MeetFrom * 60;
        }

        /// Whether the writer is waiting now: from the meet's day for a fortnight, in the hours.
        public static bool MeetOpen(Reply r, long now)
        {
            if (r == null || r.MeetAt <= 0 || r.Met || now < r.MeetAt || now >= r.MeetAt + MeetWaitDays * 1440L)
                return false;
            int hour = (int)(now % 1440 / 60);
            return hour >= MeetFrom && hour < MeetTo;
        }

        /// The fortnight gone with no meeting.
        public static bool MeetPast(Reply r, long now)
        {
            return r != null && r.MeetAt > 0 && !r.Met && now >= r.MeetAt + MeetWaitDays * 1440L;
        }

        /// The day's name for the letter: the weekday MeetDaysOn days on, or "the day after tomorrow".
        public static string DayWord(int weekdayNow, int daysOn)
        {
            if (weekdayNow < 0 || weekdayNow > 6)
                return daysOn == 2 ? "the day after tomorrow" : "in " + daysOn + " days";
            string[] names = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
            return names[(weekdayNow + daysOn) % 7];
        }

        private static int Roll(string seed)
        {
            return Voice.Hash(seed) % 100;
        }

        /// What an answer does at the writer's end, by the letter's kind and the tone.
        public static Effect Outcome(string letter, string tone, int amount, string seed)
        {
            var e = new Effect { Note = "taken" };
            int r = Roll(seed + "|" + letter + "|" + tone);
            switch (letter)
            {
                case "grief":
                    if (tone == "kind") { e.StandDown = r < 55; e.Note = e.StandDown ? "stays home" : "unmoved"; }
                    else if (tone == "cold") { e.Note = "unmoved"; }
                    else if (tone == "threat") { e.RideSooner = r < 65; e.StandDown = !e.RideSooner; e.Note = e.RideSooner ? "rides sooner" : "frightened off"; }
                    else if (tone == "money") { e.StandDown = r < (amount >= Money20 ? 80 : 50); e.Note = e.StandDown ? "stays home" : "burns the money"; }
                    else if (tone == "meet") { e.StandDown = r < 50; e.Meet = e.StandDown; e.Note = e.Meet ? "will meet" : "won't meet"; }
                    break;
                case "thanks":
                    e.Affection = tone == "kind" ? 10 : tone == "cold" ? -5 : tone == "threat" ? -25 : tone == "money" ? (amount >= Money20 ? 25 : 15) : 10;
                    e.Liking = e.Affection;
                    e.Meet = tone == "meet";
                    e.Note = tone == "threat" ? "hurt" : tone == "cold" ? "cooled" : tone == "meet" ? "will meet" : "glad";
                    break;
                case "love":
                    if (tone == "kind") { e.Heart = 1; e.Affection = 12; e.Note = "warmed"; }
                    else if (tone == "cold") { e.Heart = -1; e.Affection = -10; e.Note = "cooled"; }
                    else if (tone == "threat") { e.Heart = -2; e.Affection = -40; e.Note = "scorned"; }
                    else if (tone == "money") { e.Affection = 5; e.Note = "money, not words"; }
                    else { e.Heart = 1; e.Affection = 8; e.Meet = true; e.Note = "will meet"; }
                    e.Liking = e.Affection;
                    break;
                case "scorned":
                    if (tone == "kind") { e.Affection = 8; e.Note = "softened"; }
                    else if (tone == "cold") { e.Affection = -5; e.Note = "done with him"; }
                    else if (tone == "threat") { e.Affection = -30; e.Note = "sunk lower"; }
                    else if (tone == "money") { e.Affection = -10; e.Note = "insulted"; }
                    else { e.Meet = r < 40; e.Affection = e.Meet ? 5 : 0; e.Note = e.Meet ? "will meet" : "won't meet"; }
                    e.Liking = e.Affection;
                    break;
                case "confession":
                    if (tone == "kind") { e.Awareness = 15; e.Affection = 10; e.Note = "believed"; }
                    else if (tone == "cold") { e.Awareness = -10; e.Affection = -5; e.Note = "mocked"; }
                    else if (tone == "threat") { e.Awareness = 5; e.Affection = -20; e.Note = "part of it"; }
                    else if (tone == "money") { e.Affection = 5; e.Note = "a kindness"; }
                    else { e.Meet = true; e.Awareness = 10; e.Note = "will meet"; }
                    e.Liking = e.Affection;
                    break;
                case "invitation":
                    e.Affection = tone == "kind" ? 8 : tone == "cold" ? -8 : tone == "threat" ? -30 : tone == "money" ? 15 : 5;
                    e.Liking = e.Affection;
                    e.Note = tone == "kind" ? "expected" : tone == "cold" ? "disappointed" : tone == "threat" ? "better you don't come" : tone == "money" ? "a gift" : "at the church";
                    break;
                case "tip":
                    e.Affection = tone == "kind" ? 5 : tone == "cold" ? 0 : tone == "threat" ? -15 : tone == "money" ? 10 : 5;
                    e.Liking = e.Affection;
                    e.Meet = tone == "meet";
                    e.Note = tone == "threat" ? "warned off" : tone == "meet" ? "will meet" : "square";
                    break;
                default:
                    e.Affection = tone == "kind" ? 5 : tone == "cold" ? -3 : tone == "threat" ? -15 : tone == "money" ? 8 : 3;
                    e.Liking = e.Affection;
                    e.Meet = tone == "meet";
                    e.Note = tone == "meet" ? "will meet" : "taken";
                    break;
            }
            return e;
        }

        /// What a letter of his own does when it lands: a warning (they keep clear), an apology (forgiven, which is harder
        /// than forgotten), a love note (the romance's rules: the courted warm, the rest half scorned), a threat, money.
        public static Effect OwnOutcome(string kind, bool courted, bool grudge, string seed)
        {
            var e = new Effect { Note = "taken" };
            int r = Roll(seed + "|own|" + kind);
            switch (kind)
            {
                case "warning": e.Affection = -8; e.Liking = -8; e.Silence = true; e.Note = "warned"; break;
                case "apology": e.Affection = grudge ? 12 : 6; e.Liking = e.Affection; e.Note = grudge ? "forgiven" : "handsome of him"; break;
                case "love":
                    if (courted) { e.Heart = 1; e.Affection = 10; e.Note = "warmed"; }
                    else if (r < 50) { e.Heart = -1; e.Affection = -10; e.Note = "scorned"; }
                    else { e.Affection = 5; e.Note = "a nerve, and liked it"; }
                    e.Liking = e.Affection;
                    break;
                case "threat": e.Affection = -30; e.Liking = -30; e.Silence = true; e.Note = "threatened"; break;
                case "money": e.Affection = 10; e.Liking = 12; e.Note = "obliged"; break;
            }
            return e;
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        /// The letter back, or null when they don't write (a warning or a threat of his own gets none). `dayWord` names the
        /// meet's day; `kin` the dead's relation to a grieving writer ("brother").
        public static Paper Answer(Reply r, Effect e, PlayerRef you, bool knowsName, string dayWord, string kin, long now)
        {
            if (r == null || e == null || e.Silence)
                return null;
            string first = Person.FirstOf(r.ToName ?? "");
            string salute = knowsName && you != null ? "Mr. " + you.Surname + "," : r.Letter == "grief" ? "You," : r.Letter == "love" ? "Dear stranger," : "Sir,";
            string seed = (r.Id ?? "") + "|ans";
            string body = r.Own ? OwnBody(r.Tone, e, first, seed) : Body(r.Letter, r.Tone, e, r.Town, dayWord, kin, seed);
            if (body == null)
                return null;
            return new Paper { Id = Ids.New(), Kind = "letter", Letter = "answer", Title = "A letter from " + (r.ToName ?? "someone"), From = r.ToName, FromId = r.ToId, Body = salute + "\n\n" + body + "\n\n" + first, At = now };
        }

        private static string MeetLine(string town, string dayWord)
        {
            return "The station in " + (town ?? "town") + ", from " + dayWord + ", ten o'clock till four. I'll come every day for a fortnight, in case the post is slow.";
        }

        public static string Body(string letter, string tone, Effect e, string town, string dayWord, string kin, string seed)
        {
            string k = kin ?? "kin";
            if (tone == "meet" && !e.Meet && letter != "grief" && letter != "scorned")
                return Pick(seed, "Meet? I can't come so far, and you'd not come to me. Let the letters do what they can.", "There'll be no meeting. Not from here. Write, if you must; I'll answer.");   // too far for a station (the writer's town is nowhere he rides)
            switch (letter)
            {
                case "grief":
                    if (tone == "kind")
                        return e.StandDown ? Pick(seed, "I read your letter more times than I'll say. I'm not coming west. Not for want of hate, but because my mother needs me more than she needs you dead. Don't write again.",
                                                      "There's a man in that letter who knows what he did. More than I looked for. I'm staying put. Forgiveness is another matter.")
                                           : Pick(seed, "Kind words. My " + k + " had kind words too. I'm still coming.", "A soft letter from a hard man. It changes nothing. I'm coming.");
                    if (tone == "cold")
                        return Pick(seed, "Your letter was short. Mine is shorter. I'm coming.", "Cold. Good. It makes what I have to do easier.");
                    if (tone == "threat")
                        return e.RideSooner ? Pick(seed, "You threaten me? Good. Now I know the kind of man you are, and I'll lose no sleep over what I do. I've moved my train up.", "Threats, from the man who killed my " + k + ". I'm coming sooner, not later.")
                                            : Pick(seed, "I'll not pretend your letter didn't frighten me. It did. I have children. God forgive me, I'm staying home.", "You've made your point. I've a family that needs me alive more than it needs you dead. I'll stay.");
                    if (tone == "money")
                        return e.StandDown ? Pick(seed, "Your money came. I wanted to send it back. I couldn't afford to. That's the shame of it. I'll not come.", "The money will see us through the winter. I hate that it's yours. I'm staying home.")
                                           : Pick(seed, "Your money went in the stove. A " + k + " isn't for sale.", "Money. For my " + k + ". I put it in the stove and watched it go.");
                    if (tone == "meet")
                        return e.Meet ? "You want to meet. All right. " + MeetLine(town, dayWord) + " Come alone. I'll have no gun. I want to see your face, that's all."
                                      : Pick(seed, "Meet you? So you can finish the family? No.", "I'll meet you when I choose, not when you ask.");
                    return null;
                case "thanks":
                    if (tone == "kind") return Pick(seed, "Your letter is pinned by the stove. We read it at supper. You're spoken of here as a good man, whatever else you are.", "We didn't expect an answer, and a kind one at that. Come by if you're ever this way.");
                    if (tone == "cold") return "I thought you'd want to know we were grateful. Seems not. I'll not trouble you again.";
                    if (tone == "threat") return "I don't understand your letter. We thanked you. Whatever we did to deserve that, we're sorry for it.";
                    if (tone == "money") return "We didn't write for money and I nearly sent it back. The children's shoes decided it. Thank you twice over.";
                    return "Meet? Gladly. " + MeetLine(town, dayWord) + " I'll bring the boy; he wants to see you.";
                case "love":
                    if (tone == "kind") return Pick(seed, "Your letter. I've read it till the fold wore through. Come back soon. Come back at all.", "I carried your letter to the well and back. Twice. Come back before the water's all drawn.");
                    if (tone == "cold") return "So that's how it is. No more letters from me, then. May the road treat you better than you treat people.";
                    if (tone == "threat") return "I don't know what I did to earn that. Don't write to me. Don't come to " + (town ?? "town") + ".";
                    if (tone == "money") return "Money. You sent money. I'd have rather had one line in your hand. Still, I kept it.";
                    return "Yes. " + MeetLine(town, dayWord) + " I'll wear the blue. Don't be late; I'll think you've changed your mind.";
                case "scorned":
                    if (tone == "kind") return "A kind letter, late. I don't know what to do with it. Maybe nothing. Maybe I'll keep it.";
                    if (tone == "cold") return "Good. Now we understand each other.";
                    if (tone == "threat") return "Threatening me by post. You've sunk lower than I'd have guessed, and I guessed low.";
                    if (tone == "money") return "Money. As if that was the thing I wanted. I gave it to the church. They'll pray for you; I won't.";
                    return e.Meet ? "If it must be said in person: " + MeetLine(town, dayWord) + " I'll give you five minutes." : "No.";
                case "confession":
                    if (tone == "kind") return "You believe me. Do you know what that is, after months of thinking I'd lost my mind? I slept last night. The first time in weeks.";
                    if (tone == "cold") return "Nonsense, you say. Maybe. Then why do the days feel like the same day? Never mind. I'll not write again.";
                    if (tone == "threat") return "You threaten me for telling you the truth. Then you're part of it too. I should have known.";
                    if (tone == "money") return "You sent money. I didn't ask for money. I asked for an answer. I'll take it as a kindness and go on wondering.";
                    return "Yes. Come. " + MeetLine(town, dayWord) + " Say nothing to anyone. They listen.";
                case "invitation":
                    if (tone == "kind") return "We're glad. There's a place at the table with your name on it. Bring an appetite and nothing else.";
                    if (tone == "cold") return "We'd hoped. Never mind. The cake will go round.";
                    if (tone == "threat") return "We don't understand your letter. We asked you to a wedding. Perhaps better you don't come.";
                    if (tone == "money") return "A gift! Far too much, and we'll keep every cent. The first dance is yours if you want it.";
                    return "We'll see you at the church, then. That's meeting enough.";
                case "tip":
                    if (tone == "kind") return "Glad it was some use. We're square now. Don't write again; letters get read.";
                    if (tone == "cold") return "I did you a good turn and that's your answer. Noted.";
                    if (tone == "threat") return "Threaten me and I'll tell the next man what I told you, and more besides.";
                    if (tone == "money") return "I didn't want paying. I'll take it, mind. Call it a retainer.";
                    return "Quietly, then. " + MeetLine(town, dayWord) + " Don't look like you're meeting anyone.";
                default:
                    if (tone == "kind") return "Kind of you to write back. Not many do.";
                    if (tone == "cold") return "All right.";
                    if (tone == "threat") return "I'll keep clear of you, then.";
                    if (tone == "money") return "Your money came, and welcome.";
                    return MeetLine(town, dayWord);
            }
        }

        private static string OwnBody(string kind, Effect e, string first, string seed)
        {
            switch (kind)
            {
                case "apology": return e.Note == "forgiven" ? "Your apology came. I'll not say all is forgotten, but it's forgiven, which is the harder thing." : "I don't recall what you're sorry for, but it's handsome of you to say so.";
                case "love":
                    return e.Note == "warmed" ? Pick(seed, "Twice through before I knew the hand was yours. Say it to me in person next time.", "A letter! You could have just come by. But I'll keep it.")
                         : e.Note == "scorned" ? Pick(seed, "I hardly know you. Don't write such things again.", "A love note, from you. I showed it to my mother. She laughed. So did I, after.")
                         : "You've a nerve. I liked it. Don't make a habit of it.";
                case "money": return "Your money came. I didn't ask for it and I'll not pretend I didn't need it. Obliged.";
                default: return null;
            }
        }

        /// What they say when met, two lines, by the letter and how the answer was taken.
        public static string[] MeetLines(string letter, string kin, string seed)
        {
            string k = kin ?? "kin";
            switch (letter)
            {
                case "grief": return new[] { "I came to look you in the eye. That's all I came for.", "I've seen it now. My " + k + "'s dead and you're alive, and I'll have to live with both." };
                case "thanks": return new[] { "There you are. I wanted to shake the hand.", "Safe roads to you, whatever you are." };
                case "love": return new[] { "You came. I didn't think you would.", "Don't say anything. Just stand there a minute." };
                case "scorned": return new[] { "Go on, then. Out with it.", "...That's all? Then we're done." };
                case "confession": return new[] { "Tell me I'm not mad. Please. Tell me you see it too.", "Thank you. Whatever happens now, thank you." };
                case "tip": return new[] { "Keep your voice down. Walk with me.", "That's all I know. We never met." };
                default: return new[] { "Didn't think you'd come.", "Well. That's that, then." };
            }
        }

        /// A line the town hears when a letter of his is shown around (a love note or a threat).
        public static string ShownAround(string kind, string toName, string yourName)
        {
            if (kind == "love")
                return toName + " has been showing a love note from " + yourName + " to anyone who'll look.";
            if (kind == "threat")
                return toName + " had a letter from " + yourName + " that read like a threat, and showed it round.";
            return null;
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (string letter in LetterKinds)
                foreach (string tone in Tones)
                    for (int s = 0; s < 3; s++)
                    {
                        Effect e = Outcome(letter, tone, s == 0 ? Money5 : Money20, "s" + s);
                        string body = Body(letter, tone, e, "Valentine", "Thursday", "brother", "b" + s);
                        if (body != null) yield return body;
                    }
            foreach (string kind in OwnKinds)
                foreach (bool courted in new[] { false, true })
                    foreach (bool grudge in new[] { false, true })
                        for (int s = 0; s < 3; s++)
                        {
                            Effect e = OwnOutcome(kind, courted, grudge, "o" + s);
                            string body = OwnBody(kind, e, "Clara", "ob" + s);
                            if (body != null) yield return body;
                        }
            foreach (string letter in LetterKinds)
                foreach (string line in MeetLines(letter, "brother", "m"))
                    yield return line;
            yield return ShownAround("love", "Clara Bell", "Arthur Morgan");
            yield return ShownAround("threat", "Hiram Cobb", "Arthur Morgan");
            foreach (InteractOption o in Options(5000)) yield return o.Label;
            foreach (InteractOption o in OwnOptions(5000)) yield return o.Label;
        }
    }
}
