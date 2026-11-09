// What goes through someone's head while another person is talking (FIVEDAY2 item 25; [Ticker] Asides). A line said near you shows as a subtitle; the person standing
// nearest the speaker now has a thought of their own about it, over their head, in one of the side boxes (Core/TickerBrain.cs, Aside). The tone is read off the words,
// the answer goes by the listener's temper group (bold, fearful, cold, plain: Minds.TemperGroup). Why it reads true: what's said and what's thought are rarely the
// same, and the distance between them is where a person lives. Being shown both at once is the thing a game almost never gets to do. All lines original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Asides
    {
        public const string Plea = "plea", Threat = "threat", Insult = "insult", Boast = "boast", Grief = "grief", Thanks = "thanks", Money = "money", Question = "question", Plain = "plain";
        public const int GapMs = 6000, PersonGapMs = 25000, Chance = 70;
        public const float EarshotMetres = 6f;

        // in order: the first that matches wins. A list of words rather than anything cleverer: there are only so many ways to beg.
        private static readonly KeyValuePair<string, string[]>[] Marks =
        {
            new KeyValuePair<string, string[]>(Plea, new[] { "please", "i beg", "begging", "mercy", "have pity", "forgive me", "don't hurt", "let me go", "spare" }),
            new KeyValuePair<string, string[]>(Grief, new[] { "buried", "funeral", "grave", "passed on", "he's dead", "she's dead", "they're dead", "widow", "orphan", "mourn", "laid to rest" }),
            new KeyValuePair<string, string[]>(Threat, new[] { "i'll kill", "kill you", "shoot you", "i'll shoot", "hang you", "you'll hang", "you'll pay", "you'll regret", "or else", "last warning", "step back", "draw", "cut you" }),
            new KeyValuePair<string, string[]>(Insult, new[] { "fool", "liar", "coward", "swine", "idiot", "drunk", "thief", "cheat", "bastard", "pig", "dog", "ugly", "stupid", "worthless" }),
            new KeyValuePair<string, string[]>(Money, new[] { "dollar", "owe", "debt", "rent", "price", "cents", "wages", "pay me", "paid", "cost" }),
            new KeyValuePair<string, string[]>(Boast, new[] { "best in", "finest", "fastest", "never lost", "never missed", "nobody can", "i built", "i once", "my own hands" }),
            new KeyValuePair<string, string[]>(Thanks, new[] { "thank", "bless you", "much obliged", "kind of you", "grateful", "good of you" }),
        };

        /// What kind of thing was just said.
        public static string ToneOf(string said)
        {
            if (string.IsNullOrEmpty(said))
                return Plain;
            string s = said.ToLowerInvariant();
            foreach (KeyValuePair<string, string[]> m in Marks)
                foreach (string w in m.Value)
                    if (Starts(s, w))
                        return m.Key;
            return s.TrimEnd().EndsWith("?", StringComparison.Ordinal) ? Question : Plain;
        }

        /// Whether a word or phrase is there, at the start of a word ("owe" isn't in "however", nor "rent" in "parent"); endings are allowed for ("owed", "fools").
        private static bool Starts(string s, string w)
        {
            for (int i = s.IndexOf(w, StringComparison.Ordinal); i >= 0; i = s.IndexOf(w, i + 1, StringComparison.Ordinal))
                if (i == 0 || !char.IsLetter(s[i - 1]))
                    return true;
            return false;
        }

        // by tone, then bold, fearful, cold, plain
        private static readonly Dictionary<string, string[][]> Pools = new Dictionary<string, string[][]>
        {
            { Plea, new[] {
                new[] { "On your feet, for pity's sake.", "I'd sooner take the bullet than ask.", "Stand up straight, at least." },
                new[] { "It'll be me asking, one of these days.", "Don't look. Look and they'll ask you too.", "Let it not be me next." },
                new[] { "Costs nothing to ask.", "Asking won't move it. Nothing moves it.", "Everybody's sorry when it's their turn." },
                new[] { "Poor soul. And not a thing I can do.", "I'd give something, if I had something.", "Lord, that's hard to stand and hear." } } },
            { Threat, new[] {
                new[] { "Do it or hush.", "Say that one to me and see.", "All mouth. I've met the type." },
                new[] { "Eyes down. Eyes down and walk.", "Somebody ought to fetch the law. Somebody.", "I knew I should've stopped home." },
                new[] { "The ones who say it seldom do.", "If it comes to it, I'm over by that wall.", "I wonder which of them is worth more dead." },
                new[] { "This is going to end badly.", "Neither one can back down now.", "There's children about, for the love of God." } } },
            { Insult, new[] {
                new[] { "Say it to me. I'd enjoy that.", "Somebody wants that mouth shut for good.", "I'd have swung by now." },
                new[] { "Oh, you shouldn't have said that.", "I'll be getting along, I think.", "That's how it starts. It always starts so." },
                new[] { "True, mind. That's why it stings.", "Let them tear at each other. Saves me the trouble.", "Worth remembering, who called who what." },
                new[] { "Some things don't get unsaid.", "There was no call for that.", "They'll both be sorry come morning." } } },
            { Boast, new[] {
                new[] { "I've done better drunk and said less.", "I could show that one a thing or two.", "Big talk from clean hands." },
                new[] { "Lord, to be that sure of anything.", "Best not ask how.", "If it's true I'd rather not know." },
                new[] { "Everyone's rich in the telling.", "That story's put on weight since Tuesday.", "Worth a nickel to hear it. Not a dime." },
                new[] { "Here we go again.", "I've heard this one. It was shorter.", "Nod. Just nod. It'll be over soon." } } },
            { Grief, new[] {
                new[] { "Somebody ought to answer for that.", "I'd have burned the place for less.", "Tears won't square it." },
                new[] { "It could be any of us. Any day.", "Don't think about it. Don't think about it.", "I couldn't bear it. I couldn't." },
                new[] { "I wonder who gets the land.", "Comes for everyone. No sense making a hymn of it.", "Sorry for it. Sorry doesn't cost much." },
                new[] { "I don't know what to say. Nobody does.", "I ought to bring something round.", "I remember that feeling. I wish I didn't." } } },
            { Thanks, new[] {
                new[] { "Thank nobody. Stand on your own.", "There's a bill coming for that.", "I'd want more than thanks." },
                new[] { "Careful who you're grateful to.", "That's a debt, and debts get called.", "Glad it's not me that owes." },
                new[] { "Gratitude. That'll be gone by Friday.", "Thanks don't spend.", "I'd like to know what it really cost." },
                new[] { "That's nice to hear. Doesn't happen much.", "Well. There's some decency left.", "I should say that more." } } },
            { Money, new[] {
                new[] { "Owe me that and you'd pay today.", "I'd take it off them and be done.", "A man who counts coin in the street wants robbing." },
                new[] { "Don't let them ask me. I haven't got it.", "We're all one bad month from that.", "Money talk. That's how the trouble comes." },
                new[] { "There's a price under that price.", "Interest on that would be something to see.", "Somebody's getting skinned. Not me, this time." },
                new[] { "Everything's gone up. Everything.", "I wish I had that to argue over.", "It always comes down to that." } } },
            { Question, new[] {
                new[] { "None of your concern, friend.", "Ask me that and see what you get.", "Who wants to know?" },
                new[] { "Don't answer. Whatever it is, don't answer.", "Don't let them ask me anything.", "Why does anybody need to know that?" },
                new[] { "Whoever's asking wants something.", "Good question. Wrong person.", "Careful what you tell that one." },
                new[] { "I wouldn't know, myself.", "Now that I'd like to hear the answer to.", "Lord knows. I don't." } } },
            { Plain, new[] {
                new[] { "Get to the point, why don't you.", "I could be somewhere else, hitting something.", "Talk, talk, talk." },
                new[] { "Keep nodding. They'll move along.", "Did they see me? I don't think they saw me.", "Don't be asked. Don't be asked." },
                new[] { "Nothing worth keeping in any of that.", "Half of that's a lie. Which half is the question.", "Listen long enough and they tell you everything." },
                new[] { "I've lost the thread of this.", "Is it supper yet?", "My feet hurt. That's what I'm thinking." } } },
        };

        // with the speaker's name in it, when the speaker has one
        private static readonly string[] Named =
        {
            "{who} has been at this since Sunday.", "{who} never could leave a thing alone.", "I've known {who} too long to be surprised.",
            "{who} said the same last year, and look.", "Mark that, {who}. Somebody will.", "That's {who} all over.",
        };

        private static int Group(string temper)
        {
            switch (Minds.TemperGroup(temper))
            {
                case "bold": return 0;
                case "fearful": return 1;
                case "cold": return 2;
                default: return 3;
            }
        }

        /// Whether a label is somebody's name, rather than "A man" or "The foreman".
        public static bool IsName(string label)
        {
            return !string.IsNullOrEmpty(label) && char.IsUpper(label[0]) && !label.StartsWith("A ", StringComparison.Ordinal) && !label.StartsWith("An ", StringComparison.Ordinal) && !label.StartsWith("The ", StringComparison.Ordinal) && !label.StartsWith("One ", StringComparison.Ordinal) && !label.StartsWith("Another ", StringComparison.Ordinal) && !label.StartsWith("Someone", StringComparison.Ordinal);
        }

        /// The thought, from what was said and who's listening. `speaker`: the label on what they said.
        public static string Thought(string said, string temper, string speaker, string seed)
        {
            if (IsName(speaker) && Voice.Chance(seed + "|named", 20))
                return Voice.Pick(seed + "|n", Named).Replace("{who}", Person.FirstOf(speaker));
            string[][] byGroup;
            if (!Pools.TryGetValue(ToneOf(said), out byGroup))
                byGroup = Pools[Plain];
            return Voice.Pick(seed + "|a", byGroup[Group(temper)]);
        }

        /// A line said for the Testing row, a different tone each time.
        public static string TestLine(int n)
        {
            string[] lines =
            {
                "Please. I've got nobody else to ask.", "Open your mouth once more and I'll kill you.", "You're a liar and a thief.", "Finest shot in three counties, and I never lost a bet.",
                "They buried her on Tuesday.", "Much obliged to you, truly.", "You owe me nine dollars since August.", "Where were you the night of the fire?", "It looks like it'll rain.",
            };
            return lines[Math.Abs(n) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] t in Pools.Values)
                foreach (string[] g in t)
                    foreach (string l in g)
                        yield return l;
            foreach (string n in Named)
                yield return n.Replace("{who}", "Silas");
        }
    }
}
