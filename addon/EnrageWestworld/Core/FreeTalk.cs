// Talk in your own words (roadmap 37; SIXDAY item 4; Lex's brief, pillar 3: "trigger voice capture and AI processing
// natively through the existing in-game Talk prompt"). The first step is typed: a row at the foot of Talk's list opens a
// line to type on, and what Arthur types is said to the person. With the local model on, they answer it in their own
// words from who they are (Language.Talk.cs, Kind "free"); without it, or when it's slow or fails the checks, the written
// answer here stands: what the words seem to be about (the news, their name, how they are, their trouble, the weather,
// the way, a drink, money, God, guns, the law, thanks, sorry, goodbye) answered by their nature and how they stand with
// him, and anything else met with a plain non-answer. Why they answer as they do: a stranger's question is a test of how
// guarded they are. The afraid give nothing, the hostile refuse, the fond answer, the cold shrug. The second step is the
// microphone (SIXDAY item 4b): the voice server's ear (voice_server.py, /listen) writes down what the player says, and
// Heard tidies it into the same line as if typed (the transcriber's notes in brackets dropped, the words it makes out of
// silence refused), so both roads meet in one answer. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Text;

namespace EnrageWestworld.Core
{
    public static class FreeTalk
    {
        public const int MostChars = 90;   // the typed line's length (the subtitle's width)
        public const int MostHeard = 160;  // a spoken line may run longer (a sentence said in six seconds), cut there

        public static string Label()
        {
            return "Say something else...";
        }

        /// The spoken row (the voice server's ear): on a keyboard or a pad.
        public static string SpeakLabel()
        {
            return "Say it out loud...";
        }

        /// What may be typed: letters, digits, a space and the plain punctuation of a sentence.
        public static bool Allowed(char c)
        {
            return char.IsLetterOrDigit(c) || c == ' ' || c == '\'' || c == ',' || c == '.' || c == '?' || c == '!' || c == '-';
        }

        /// The text as said: trimmed, one space between words, the first letter up, a full stop if no mark ends it.
        public static string Tidy(string text, int most = MostChars)
        {
            if (text == null || text.Length == 0)
                return null;
            var b = new StringBuilder();
            bool space = true;
            foreach (char c in text)
            {
                if (!Allowed(c))
                    continue;
                if (c == ' ')
                {
                    if (!space)
                        b.Append(' ');
                    space = true;
                }
                else
                {
                    b.Append(c);
                    space = false;
                }
            }
            string s = b.ToString().Trim();
            if (s.Length == 0)
                return null;
            if (s.Length > most)
                s = s.Substring(0, most).TrimEnd();
            s = s.Substring(0, 1).ToUpperInvariant() + s.Substring(1);
            char last = s[s.Length - 1];
            if (last != '.' && last != '?' && last != '!')
                s += s.IndexOf('?') < 0 && Asks(s.ToLowerInvariant()) ? "?" : ".";
            return s;
        }

        /// What the transcriber wrote down, as said: its notes in brackets dropped ("[BLANK_AUDIO]", "(laughs)"), the stock
        /// words it makes out of silence refused ("you" alone, a watcher's thanks, a subtitler's credit), anything without
        /// a letter in it too, and the rest tidied as a typed line is, to MostHeard. Null when nothing worth answering was heard.
        public static string Heard(string raw)
        {
            if (raw == null || raw.Length == 0)
                return null;
            var b = new StringBuilder(raw.Length);
            int depth = 0;
            foreach (char c in raw)
            {
                if (c == '[' || c == '(' || c == '{') { depth++; continue; }
                if (c == ']' || c == ')' || c == '}') { if (depth > 0) depth--; continue; }
                if (depth == 0)
                    b.Append(c);
            }
            string s = Tidy(b.ToString(), MostHeard);
            if (s == null)
                return null;
            bool letters = false;
            foreach (char c in s)
                if (char.IsLetterOrDigit(c)) { letters = true; break; }
            if (!letters)
                return null;
            string lower = s.ToLowerInvariant().TrimEnd('.', '!', '?', ' ');
            if (lower == "you" || lower == "thanks for watching" || lower == "thank you for watching" || lower.Contains("subtitles by") || lower.Contains("amara.org"))
                return null;
            return s;
        }

        private static bool Asks(string lower)
        {
            return lower.StartsWith("who ") || lower.StartsWith("what ") || lower.StartsWith("where ") || lower.StartsWith("when ") || lower.StartsWith("why ") || lower.StartsWith("how ") ||
                   lower.StartsWith("do you") || lower.StartsWith("are you") || lower.StartsWith("is ") || lower.StartsWith("can you") || lower.StartsWith("have you") || lower.StartsWith("did you");
        }

        /// What he said, for the model: his own words, quoted.
        public static string Said(string text)
        {
            return "He says to you: \"" + text + "\"";
        }

        private static bool Has(string lower, params string[] words)
        {
            foreach (string w in words)
                if (lower.Contains(w))
                    return true;
            return false;
        }

        /// What the words seem to be about: bye, thanks, sorry, news, name, how, worry, weather, way, drink, money, god,
        /// guns, law, question, other.
        public static string Topic(string text)
        {
            string l = (text ?? "").ToLowerInvariant();
            if (Has(l, "goodbye", "good bye", "so long", "farewell", "good day", "be seeing you", "see you"))
                return "bye";
            if (Has(l, "thank", "obliged", "much appreciated"))
                return "thanks";
            if (Has(l, "sorry", "apolog", "forgive", "my fault"))
                return "sorry";
            if (Has(l, "your name", "who are you", "what are you called", "what do they call you", "what's your name"))
                return "name";
            if (Has(l, "how are you", "how's it", "how are things", "how do you do", "how you doing", "how are ya", "keeping well"))
                return "how";
            if (Has(l, "troubl", "worr", "what's wrong", "something wrong", "bother", "on your mind", "what ails"))
                return "worry";
            if (Has(l, "news", "what's happen", "heard anything", "any word", "what's new", "going on", "word about town"))
                return "news";
            if (Has(l, "rain", "weather", " hot", "cold", "snow", "storm", "sun's", "sunny", "the wind", "windy"))
                return "weather";
            if (Has(l, "sheriff", "the law", "deputy", "marshal", "jail", "lawman"))
                return "law";
            if (Has(l, "where", "which way", "the way to", "how far", "road to", "nearest", "direction"))
                return "way";
            if (Has(l, "drink", "whiskey", "beer", "saloon", "thirsty", "bottle"))
                return "drink";
            if (Has(l, "dollar", "money", "loan", "lend", "owe", "pay you", "cash", "cents"))
                return "money";
            if (Has(l, "god", "lord", "church", "pray", "preacher", "heaven", "sin"))
                return "god";
            if (Has(l, "gun", "shoot", "shot", "kill", "dead", "murder", "outlaw", "bounty", "pistol", "rifle"))
                return "guns";
            if (l.EndsWith("?"))
                return "question";
            return "other";
        }

        /// The written answer to his words, by what they're about, their temper and how they stand with him (afraid,
        /// hostile, fond or null); `worry` and `confides` as Talk's news: they tell him their trouble only if they'd
        /// confide; `self` their whole name (the friendly give it), `knowsName` whether they know his.
        public static InteractAnswer Answer(string text, string temper, bool woman, string regard, string worry, bool confides, bool knowsName, string self, string seed, out string topic)
        {
            topic = Topic(text);
            var a = new InteractAnswer { Then = "stay", Friendly = regard != "afraid" && regard != "hostile" && temper != "Cold" };
            bool afraid = regard == "afraid", hostile = regard == "hostile";
            string sir = woman ? "mister" : "mister";
            switch (topic)
            {
                case "bye":
                    a.Line = afraid ? "Yes. Good day, mister. Good day." : hostile ? "And good riddance." : Pick(seed + "|fbye", "Good day, then.", "So long, then.", "Be seeing you.");
                    a.Then = "bye";
                    if (hostile) a.VoiceHate = 20;
                    return a;
                case "thanks":
                    a.Line = hostile ? "Don't thank me. I did nothing for you." : afraid ? "N-no need, mister. No need." : Pick(seed + "|fth", "Don't mention it.", "No trouble.", "You're welcome, I'm sure.");
                    a.Thought = hostile ? "Thanks. From him." : "Mannerly, for a stranger.";
                    return a;
                case "sorry":
                    a.Line = hostile ? Pick(seed + "|fsoh", "Sorry doesn't mend it.", "Keep your sorry.") : afraid ? "It's... it's all right, mister. Truly." : temper == "Proud" ? "I'll take it, this once." : temper == "Cold" ? "If you say so." : Pick(seed + "|fso", "Then it's done with.", "It's forgotten.", "That's handsome of you.");
                    a.Thought = hostile ? "Let him be sorry." : "A man who says sorry. Rare enough.";
                    if (hostile) a.VoiceHate = 30;
                    return a;
                case "name":
                    if (afraid) { a.Line = "It... it don't matter, mister. Nobody you'd know."; a.VoiceFear = 40; a.Thought = "Don't give him a name to carry."; return a; }
                    if (hostile) { a.Line = "You'll get no name from me."; a.VoiceHate = 30; return a; }
                    if (temper == "Cold" || self == null) { a.Line = Pick(seed + "|fnc", "Names are for friends.", "Nobody. Nobody you need."); a.Thought = "Asking names. What's he want with it."; return a; }
                    a.Line = woman ? "Mrs. " + self.Substring(self.LastIndexOf(' ') + 1) + ". And you, " + sir + "?" : self + ". And who's asking?";
                    a.Thought = "Gave him my name. Hope I'll not regret it.";
                    return a;
                case "how":
                    if (afraid) { a.Line = "Fine. Fine, mister. Can't complain."; a.VoiceFear = 30; return a; }
                    if (hostile) { a.Line = "Better before you came along."; a.VoiceHate = 30; return a; }
                    if (worry != null && confides) { a.Line = Interact.WorryLine(worry, seed); a.Confided = true; a.Thought = "Told a stranger my trouble. Well. He asked."; return a; }
                    a.Line = temper == "Cold" ? "Well enough." : temper == "Hothead" ? "Spoiling for something. Don't know what yet." : Pick(seed + "|fhow", "Can't complain. Nobody'd listen.", "Middling. Same as the weather.", "Tolerable, mister. Tolerable.");
                    return a;
                case "worry":
                    if (afraid) { a.Line = "Nothing, mister. Nothing's wrong. Why?"; a.VoiceFear = 40; a.Thought = "What does he know? What's he heard?"; return a; }
                    if (hostile) { a.Line = "My troubles are my own. You've given me enough of them."; a.VoiceHate = 30; return a; }
                    if (worry != null && confides) { a.Line = Interact.WorryLine(worry, seed); a.Confided = true; a.Thought = Pick(seed + "|fwt", "Feels better, saying it.", "Why'd I tell a stranger that?"); return a; }
                    a.Line = worry != null ? Pick(seed + "|fwn", "Got my troubles, same as anyone. I'll not bore you with them.", "Nothing I'd tell a stranger.") : Pick(seed + "|fwx", "Nothing to speak of. Why, do I look it?", "Only the usual. Money, mostly.");
                    a.Thought = "Does it show, then.";
                    return a;
                case "news":
                    if (afraid) { a.Line = "I hear nothing, mister. Nothing at all."; a.VoiceFear = 50; return a; }
                    if (hostile) { a.Line = "Not a word. Not to you."; a.VoiceHate = 30; return a; }
                    if (worry != null && confides) { a.Line = Interact.WorryLine(worry, seed); a.Confided = true; return a; }
                    a.Line = Pick(seed + "|fnw", "Quiet enough. Somebody owes somebody, somebody's sweet on somebody.", "Nothing worth the telling. Ask at the saloon.", "Same as ever, mister.");
                    return a;
                case "weather":
                    a.Line = afraid ? "Yes. The weather. Yes." : hostile ? "Talk to somebody else about the weather." : Pick(seed + "|fwe", "It'll do what it does. Always has.", "Needed rain a week ago. We'll get it when the hay's cut.", "Hot or cold, the work's the same.", "Farmers'll complain either way. I'm one of them.");
                    if (hostile) a.VoiceHate = 20;
                    return a;
                case "way":
                    a.Line = afraid ? "That way, mister. Just... that way." : hostile ? "Find it yourself." : Pick(seed + "|fway", "Follow the road and you'll not miss it. Nothing else out there to miss.", "That way, and keep the river on your right.", "Ask at the station. They've a map, which is more than I have.");
                    if (hostile) a.VoiceHate = 20;
                    return a;
                case "drink":
                    a.Line = hostile ? "I'll not drink with you." : afraid ? "If... if you're buying, mister." : temper == "Cold" ? "If you're buying." : Pick(seed + "|fdr", "Now you're talking.", "I'd not say no, if there's a saloon in it.", "A drink's a drink. Lead on.");
                    a.Thought = hostile ? "Drink with him. Never." : "Free whiskey, maybe. Can't hurt to say yes.";
                    if (hostile) a.VoiceHate = 30;
                    return a;
                case "money":
                    a.Line = hostile ? "I want nothing of yours, and you'll get nothing of mine." : afraid ? "I... I've nothing, mister. Nothing to spare." : temper == "Greedy" ? "Now we're talking. How much?" : temper == "Proud" ? "I don't discuss money in the street." : Pick(seed + "|fmo", "Money? I've four dollars and a button.", "If I had any, I'd not be standing here.", "Never lend and never borrow. Learned that the hard way.");
                    if (hostile) a.VoiceHate = 30;
                    if (afraid) a.VoiceFear = 30;
                    return a;
                case "god":
                    a.Line = hostile ? "Don't you speak of the Lord to me." : Pick(seed + "|fgod", "He sees it all, they say. Busy man.", "I keep the Sabbath, mostly. The rest of the week keeps me.", "Ask the preacher. He's paid to know.", "I pray. Whether anybody's listening is another matter.");
                    if (hostile) a.VoiceHate = 30;
                    return a;
                case "guns":
                    if (afraid) { a.Line = "I... I'm not looking for trouble, mister."; a.VoiceFear = 60; a.Thought = "Why's he talking of guns? Why to me?"; return a; }
                    if (hostile) { a.Line = "That sounded like a threat."; a.VoiceHate = 50; a.Then = "square up"; return a; }
                    a.Line = temper == "Hothead" ? "I can look after myself, if that's what you're asking." : temper == "Coward" ? "I keep clear of all that. Clear as I can." : Pick(seed + "|fgun", "There's too much of that in this country. Always was.", "I've no quarrel with any man. Mean to keep it so.", "Dead men don't pay their debts. Ask the undertaker.");
                    a.Thought = "Guns. Why'd he bring up guns.";
                    return a;
                case "law":
                    a.Line = afraid ? "The sheriff? I... I wouldn't know, mister." : hostile ? "The law? You'd best be asking what the law thinks of you." : Pick(seed + "|flaw", "The sheriff's a decent man. Slow, but decent.", "Law's what the man with the badge says it is, out here.", "Keep on the right side of it and it keeps off you. Mostly.");
                    if (hostile) a.VoiceHate = 30;
                    return a;
                case "question":
                    a.Line = afraid ? "I... I couldn't say, mister." : hostile ? "I'll answer nothing of yours." : temper == "Cold" ? "Couldn't say." : Pick(seed + "|fq", "Can't say I know, mister.", "You'd have to ask somebody who knows. That's not me.", "Depends who you ask. Don't ask me.");
                    if (hostile) a.VoiceHate = 20;
                    if (afraid) a.VoiceFear = 30;
                    return a;
                default:
                    a.Line = afraid ? "If you say so, mister." : hostile ? "Are you finished?" : temper == "Cold" ? "Mm." : temper == "Hothead" ? "Is that right. Well." : Pick(seed + "|fot", "Can't say I follow you, mister.", "If you say so.", "That's as may be.", "Well, now.");
                    a.Thought = afraid ? "Nod. Agree. Let him move on." : hostile ? "Talk, talk. Let him talk to the wall." : "Strange thing to say to a man.";
                    if (hostile) a.VoiceHate = 20;
                    if (afraid) a.VoiceFear = 30;
                    return a;
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            yield return Label();
            yield return SpeakLabel();
            string[] texts = { "Goodbye.", "Thank you kindly.", "I'm sorry about that.", "What's your name?", "How are you faring?", "What's troubling you?", "Any news?", "Think it'll rain?", "Which way to Valentine?", "Fancy a drink?", "Could you lend me a dollar?", "Do you believe in God?", "Ever shot a man?", "Where's the sheriff?", "Is this your town?", "The horse is tired." };
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                foreach (string regard in new[] { null, "afraid", "hostile", "fond" })
                    foreach (string text in texts)
                    {
                        string topic;
                        InteractAnswer a = Answer(text, temper, false, regard, "debt", regard == "fond", false, "Hiram Cobb", "f" + text, out topic);
                        if (a.Line != null) yield return a.Line;
                        if (a.Thought != null) yield return a.Thought;
                        a = Answer(text, temper, true, regard, null, false, true, "Clara Bell", "g" + text, out topic);
                        if (a.Line != null) yield return a.Line;
                        if (a.Thought != null) yield return a.Thought;
                    }
        }
    }
}
