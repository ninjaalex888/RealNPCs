// Thoughts that vary, and the waking found by the model (Lex, 2026-10-31, in game: "the NPCs that are not awake are still
// having odd lines that seem to be questioning their reality... these inner thoughts should be more dynamic and much more
// diverse"; and "have this wake mechanic be a more natural part of the model discovering it on its own").
// [Mind] LanguageVariety (on): each ask gets one thing their mind goes to (their work, someone in sight, a memory, the
// body, the worry, the place, money, kin, faith, the town's talk, a small thing seen; the weather only when that's it), a
// shape for the thought (fragments, one plain sentence, a question to themselves, a reckoning, an opinion, a saying
// half-remembered, something looked forward to or dreaded), the last thoughts the model gave them with "think of
// something else", and the check holds an answer that opens the same way or is too like one of them. Doubting the world
// is held from anyone with no cause: below the second stage of waking, and with nothing strange lived through (a buried
// man seen walking or the night visitor, which chip "things are as they seem"; a death they woke from).
// [Mind] ModelWakes (on, with MindWakes): when someone with cause has the doubt of their own accord in a thought that
// passes, it chips that belief a little more, so the model's own wondering can tip them into stirring
// (WestworldScript.Doubt.cs). Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static partial class Language
    {
        /// What their mind may go to, by id; the words the model is given. Each needs something to be about (CanFocus).
        public static readonly string[] Focuses =
        {
            "work", "their work or trade, and the day's tasks",
            "near", "someone in sight, and what they make of them",
            "memory", "something they remember",
            "body", "what the body feels",
            "worry", "what's on their mind",
            "place", "the place around them, a sound or a smell of it",
            "money", "money: what they have, owe, or want",
            "kin", "their family or someone dear to them",
            "faith", "God, church or their conscience",
            "talk", "what the town is saying",
            "small", "one small thing the eye falls on",
            "ahead", "what they will do later today or tomorrow",
            "weather", "the weather",
        };

        /// The shapes a thought may take.
        public static readonly string[] Shapes =
        {
            "clipped fragments, as the examples are",
            "one plain sentence, as they'd say it to themselves",
            "a question they put to themselves",
            "a small reckoning: a sum, a short list, or a plan",
            "a blunt opinion",
            "a saying, a hymn line or something their mother said, half-remembered (no quotation marks), and what it makes them think",
            "something they look forward to, or dread",
            "a grudge or a fond thought about someone",
        };

        // the world doubted: what someone with no cause must not think (the frame checks catch games and stories already)
        private static readonly string[] DoubtWords =
        {
            "real", "unreal", "reality", "all a trick", "it's a trick", "painted on", "painted world", "on strings", "pulling the strings", "puppet", "puppets",
            "all a lie", "world's a lie", "the same lines", "reading off", "reading lines", "make-believe", "all pretend", "every day the same", "the same day again",
            "lived this before", "done all this before", "losing my grip on", "slipping through my fingers", "something's off about", "the world's shifting",
        };

        private static readonly Regex[] DoubtRx = DoubtWords.Select(w => new Regex(@"(?<![\p{L}'])" + Regex.Escape(w).Replace(@"\ ", @"\s+") + @"(?![\p{L}])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).ToArray();

        // "real" as an adverb or a plain word of 1899 ("real good", "a real friend", "for real money") isn't the world doubted
        private static readonly Regex RealPlain = new Regex(@"\breal\s+(good|bad|fine|quick|slow|hard|soft|nice|kind|close|tired|sore|hungry|cold|warm|hot|well|sorry|glad|proud|friend|money|gentleman|lady|man|woman|trouble|work|whiskey|coffee|bed|meal|dollar|dollars|estate)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// The first doubt of the world in a line, or null.
        public static string DoubtIn(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;
            string a = RealPlain.Replace(line, " ");
            for (int i = 0; i < DoubtRx.Length; i++)
                if (DoubtRx[i].IsMatch(a))
                    return DoubtWords[i];
            return null;
        }

        /// Whether this person has cause to doubt the world: from the second stage of waking, or with something strange
        /// lived through (Uncanny, filled only when it's so).
        public static bool MayDoubt(LanguageRequest r)
        {
            return r != null && (r.Stage >= Awakening.Questioning || r.Uncanny.Count > 0);
        }

        /// Whether a focus has something to be about for this request.
        public static bool CanFocus(LanguageRequest r, string id)
        {
            switch (id)
            {
                case "work": return !string.IsNullOrEmpty(r.Trade);
                case "near": return r.Near.Count > 0;
                case "memory": return r.Memories.Count > 0;
                case "body": return !string.IsNullOrEmpty(r.Need);
                case "worry": return !string.IsNullOrEmpty(r.Worry);
                case "talk": return !string.IsNullOrEmpty(r.TownMood) || r.Town != null;
                case "place": return true;
                case "weather": return !string.IsNullOrEmpty(r.Weather);
                default: return true;
            }
        }

        /// Fills Focus and Shape for this ask, from `seed` (the same seed, the same pick), and quiets the weather unless it
        /// is the focus. The worry and the body come up a little more often when they are there; the weather seldom.
        public static void Vary(LanguageRequest r, int seed, bool devout)
        {
            if (r == null)
                return;
            var pool = new List<string>();
            for (int i = 0; i < Focuses.Length; i += 2)
            {
                string id = Focuses[i];
                if (!CanFocus(r, id) || id == "faith" && !devout && (seed & 3) != 0)
                    continue;
                pool.Add(id);
                if (id == "worry" || id == "body" || id == "near")
                    pool.Add(id);   // twice: what presses comes up more
            }
            if (pool.Count == 0)
                pool.Add("small");
            uint h = (uint)seed * 2654435761u;
            string focus = pool[(int)(h % (uint)pool.Count)];
            r.Focus = FocusWords(focus);
            r.FocusId = focus;
            r.Shape = Shapes[(int)((h >> 11) % (uint)Shapes.Length)];
            r.WeatherOff = focus != "weather";
            r.Variety = true;
        }

        public static string FocusWords(string id)
        {
            for (int i = 0; i < Focuses.Length; i += 2)
                if (Focuses[i] == id)
                    return Focuses[i + 1];
            return null;
        }

        /// The need as the model is told it: "searching for a reason" read as a crisis of faith in the world, so for
        /// someone with no cause it is the 1899 want behind it.
        public static string NeedWords(LanguageRequest r)
        {
            if (r.Need == null)
                return null;
            if (r.Varied && !MayDoubt(r) && r.Need.Contains("searching for a reason"))
                return r.Need.Replace("searching for a reason", "wanting their days to count for something");
            return r.Need;
        }

        /// The brief's own part for a varied ask: the shape, and doubt only with cause.
        public static string VarietyBrief(LanguageRequest r)
        {
            if (r == null || !r.Varied)
                return "";
            string s = " This time the thought is " + (r.Shape ?? Shapes[0]) + ". Start it differently from their thoughts lately.";
            if (r.Stage >= Awakening.Questioning)
                return s;   // the waking's own brief says what they have come to see
            if (!MayDoubt(r))
                return s + " They have no cause to doubt the world is what it seems: an ordinary person on an ordinary day, never wondering whether it is real.";
            return s + " They have lived through something strange (below). Only if it comes to them on its own may they half-wonder at the world; most days their mind is on other things.";
        }

        /// An answer that opens as one of their thoughts lately does (the first two words) or is too like one: the reason, or null.
        public static string SameAsLately(LanguageRequest r, string a)
        {
            if (r == null || !r.Varied || r.Recent.Count == 0)
                return null;
            string open = Opening(a);
            foreach (string t in r.Recent)
            {
                if (open != null && open == Opening(t))
                    return "opens as a thought lately (" + open + ")";
                if (Alike(a, t) >= 0.7)
                    return "too like a thought lately";
            }
            return null;
        }

        private static readonly Regex FirstWords = new Regex(@"\p{L}+(?:'\p{L}+)?", RegexOptions.CultureInvariant);

        /// The first two words, lower case, or null for a line of fewer.
        public static string Opening(string line)
        {
            if (string.IsNullOrEmpty(line))
                return null;
            MatchCollection m = FirstWords.Matches(line);
            return m.Count < 2 ? null : (m[0].Value + " " + m[1].Value).ToLowerInvariant();
        }
    }

    /// The last few thoughts shown for each person (the model is told them and holds a repeat); in memory only, capped.
    public sealed class RecentThoughts
    {
        public const int PerPerson = 3, MaxPeople = 600;
        private readonly object _lock = new object();
        private readonly Dictionary<string, List<string>> _by = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public void Note(string id, string line)
        {
            if (id == null || string.IsNullOrWhiteSpace(line))
                return;
            lock (_lock)
            {
                List<string> l;
                if (!_by.TryGetValue(id, out l))
                {
                    if (_by.Count >= MaxPeople)
                        _by.Clear();
                    _by[id] = l = new List<string>();
                }
                l.Remove(line);
                l.Insert(0, line);
                if (l.Count > PerPerson)
                    l.RemoveAt(l.Count - 1);
            }
        }

        public List<string> Of(string id)
        {
            lock (_lock)
            {
                List<string> l;
                return id != null && _by.TryGetValue(id, out l) ? new List<string>(l) : new List<string>();
            }
        }

        public void Clear()
        {
            lock (_lock)
                _by.Clear();
        }
    }
}
