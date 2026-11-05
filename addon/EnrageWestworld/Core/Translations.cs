// A translation file of the player's own (Run 3, item 8; a player's ask, relayed by Lex 2026-11-04: "an external language
// file system (like English<TAB>translation per line) so it can be translated?"). translation.txt in the RealNPCs folder:
// one entry a line, the English, a TAB, the translation; lines starting with # are notes; a later entry for the same
// English wins over an earlier one. The English may keep the {slots} RealNPCs' lines are written with ({who}, {town},
// {a}, {b}, {o}, {you}...): such an entry matches the line once filled in, and the translation's own {slots} get what
// filled them (a name stays a name). A line with no entry of its own is tried sentence by sentence; anything not found
// shows in English, as before. Only what shows on screen is translated (the bar, the thoughts over heads, the subtitles,
// the hints): the cloned voices still say the English, the game's own recorded words stay the game's, the AI model's
// own words are as it wrote them, and the settings panel and the ini stay in English. The Testing row "Write the
// translation template" writes every line RealNPCs has, one a line with a TAB after it, to translation-template.txt.
// Lookups happen once a line, when it is first laid out (never each frame), and are kept (Of's cache).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Translations
    {
        /// Whether lookups translate at all: [Display] Translate on and a file read with at least one entry.
        public static bool On;

        private sealed class Template
        {
            public Regex Match;
            public string To;
            public string[] Slots;   // the slot name of each group, in order (s0, s1...)
        }

        private static readonly object Gate = new object();
        private static Dictionary<string, string> _exact = new Dictionary<string, string>();
        private static Dictionary<string, List<Template>> _byHead = new Dictionary<string, List<Template>>();
        private static List<Template> _slotFirst = new List<Template>();
        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();
        private const int CacheMost = 4000;
        /// The {slots} in an English line (small letters, then letters, digits or underscores, in braces): where each begins, and
        /// its name.
        private static List<KeyValuePair<int, string>> Slots(string s)
        {
            var found = new List<KeyValuePair<int, string>>();
            for (int i = 0; i + 2 < s.Length; i++)
            {
                if (s[i] != '{' || !char.IsLower(s[i + 1]))
                    continue;
                int j = i + 1;
                while (j < s.Length && (char.IsLower(s[j]) || char.IsDigit(s[j]) || s[j] == '_'))
                    j++;
                if (j < s.Length && s[j] == '}')
                {
                    found.Add(new KeyValuePair<int, string>(i, s.Substring(i + 1, j - i - 1)));
                    i = j;
                }
            }
            return found;
        }
        private static readonly char[] SentenceEnds = { '.', '?', '!' };

        public static int Entries { get { return _exact.Count + _byHead.Values.Sum(l => l.Count) + _slotFirst.Count; } }
        public static int Templates { get { return _byHead.Values.Sum(l => l.Count) + _slotFirst.Count; } }
        public static int Hits, Misses;

        /// Reads the entries (the file's lines, as read). How many were taken; `skipped` the lines that weren't entries (no TAB,
        /// an empty side). A later entry for the same English replaces the earlier.
        public static int Load(IEnumerable<string> lines, out int skipped)
        {
            skipped = 0;
            var exact = new Dictionary<string, string>();
            var templates = new Dictionary<string, string>();
            foreach (string raw in lines ?? Enumerable.Empty<string>())
            {
                string line = (raw ?? "").TrimEnd('\r', '\n').TrimStart('﻿');
                if (line.Trim().Length == 0 || line.TrimStart().StartsWith("#"))
                    continue;
                int tab = line.IndexOf('\t');
                if (tab <= 0)
                {
                    skipped++;   // no TAB, or nothing before it: not an entry
                    continue;
                }
                string en = line.Substring(0, tab).Trim(), to = line.Substring(tab + 1).Trim();
                if (en.Length == 0)
                {
                    skipped++;
                    continue;
                }
                if (to.Length == 0)
                    continue;   // left blank (the template's own lines): stays English
                if (Slots(en).Count > 0)
                    templates[en] = to;
                else
                    exact[en] = to;
            }
            var byHead = new Dictionary<string, List<Template>>();
            var slotFirst = new List<Template>();
            foreach (KeyValuePair<string, string> kv in templates)
            {
                Template t = Compile(kv.Key, kv.Value);
                if (t == null)
                {
                    skipped++;
                    continue;
                }
                string head = HeadOf(kv.Key);
                if (head == null)
                    slotFirst.Add(t);
                else
                {
                    List<Template> list;
                    if (!byHead.TryGetValue(head, out list))
                        byHead[head] = list = new List<Template>();
                    list.Add(t);
                }
            }
            lock (Gate)
            {
                _exact = exact;
                _byHead = byHead;
                _slotFirst = slotFirst;
                Cache.Clear();
                Hits = Misses = 0;
            }
            return exact.Count + byHead.Values.Sum(l => l.Count) + slotFirst.Count;
        }

        /// Forgets every entry (the file gone, or the switch off).
        public static void Clear()
        {
            int skipped;
            Load(null, out skipped);
            On = false;
        }

        /// The text as shown: its translation, or the text itself when there's none (or the translation is off).
        public static string Of(string text)
        {
            if (!On || string.IsNullOrEmpty(text))
                return text;
            lock (Gate)
            {
                string done;
                if (Cache.TryGetValue(text, out done))
                    return done;
                done = Whole(text);
                if (done == null)
                {
                    // sentence by sentence: a line made of two thoughts may have one of them in the file
                    List<string> parts = Sentences(text);
                    if (parts.Count > 1)
                    {
                        bool any = false;
                        var sb = new StringBuilder();
                        foreach (string p in parts)
                        {
                            string t = Whole(p.Trim());
                            any |= t != null;
                            if (sb.Length > 0)
                                sb.Append(' ');
                            sb.Append(t ?? p.Trim());
                        }
                        if (any)
                            done = sb.ToString();
                    }
                }
                if (done == null)
                    Misses++;
                else
                    Hits++;
                if (Cache.Count >= CacheMost)
                    Cache.Clear();
                Cache[text] = done ?? text;
                return done ?? text;
            }
        }

        /// One whole line or sentence: an exact entry, else a template whose English matches it filled in; null for none.
        private static string Whole(string text)
        {
            string to;
            if (_exact.TryGetValue(text, out to))
                return to;
            string head = HeadOf(text);
            List<Template> list;
            if (head != null && _byHead.TryGetValue(head, out list))
                foreach (Template t in list)
                {
                    string r = Fill(t, text);
                    if (r != null)
                        return r;
                }
            foreach (Template t in _slotFirst)
            {
                string r = Fill(t, text);
                if (r != null)
                    return r;
            }
            return null;
        }

        private static string Fill(Template t, string text)
        {
            Match m = t.Match.Match(text);
            if (!m.Success)
                return null;
            string r = t.To;
            for (int i = 0; i < t.Slots.Length; i++)
                r = r.Replace("{" + t.Slots[i] + "}", m.Groups["s" + i].Value);
            return r;
        }

        /// The English as a pattern: its words as they are, each {slot} a group of anything (the first time a slot is used; a
        /// slot used again must read the same).
        private static Template Compile(string en, string to)
        {
            try
            {
                var slots = new List<string>();
                var sb = new StringBuilder("^");
                int at = 0;
                foreach (KeyValuePair<int, string> m in Slots(en))
                {
                    sb.Append(Regex.Escape(en.Substring(at, m.Key - at)));
                    string name = m.Value;
                    int known = slots.IndexOf(name);
                    if (known >= 0)
                        sb.Append(@"\k<s" + known + ">");
                    else
                    {
                        sb.Append("(?<s" + slots.Count + ">.+?)");
                        slots.Add(name);
                    }
                    at = m.Key + name.Length + 2;
                }
                sb.Append(Regex.Escape(en.Substring(at))).Append("$");
                return new Template { Match = new Regex(sb.ToString(), RegexOptions.CultureInvariant), To = to, Slots = slots.ToArray() };
            }
            catch
            {
                return null;
            }
        }

        /// The first word, lower case, of a line or of a template's English; null when a template begins with a slot.
        private static string HeadOf(string s)
        {
            s = (s ?? "").TrimStart();
            if (s.StartsWith("{"))
                return null;
            int end = 0;
            while (end < s.Length && (char.IsLetterOrDigit(s[end]) || s[end] == '\''))
                end++;
            return end == 0 ? "" : s.Substring(0, end).ToLowerInvariant();
        }

        private static List<string> Sentences(string text)
        {
            var parts = new List<string>();
            int start = 0;
            for (int i = 0; i < text.Length; i++)
                if (Array.IndexOf(SentenceEnds, text[i]) >= 0 && (i + 1 == text.Length || text[i + 1] == ' '))
                {
                    parts.Add(text.Substring(start, i + 1 - start));
                    start = i + 1;
                }
            if (start < text.Length && text.Substring(start).Trim().Length > 0)
                parts.Add(text.Substring(start));
            return parts;
        }

        // ---- the template file ---------------------------------------------------------------------------------------

        /// Every line RealNPCs has, as written (with its {slots}), once each, sorted: the left side of a translation file.
        /// Read from Core's own compiled code (every string written into it: the word lists, the lines picked in place, the
        /// scenes' beats), keeping what reads as words (Wordy); the game side's notices and the log's own lines are left out.
        public static List<string> AllLines()
        {
            var seen = new HashSet<string>();
            Module module = typeof(Translations).Module;
            foreach (Type t in typeof(Translations).Assembly.GetTypes())
            {
                if (t.Namespace != typeof(Translations).Namespace || t == typeof(Translations))
                    continue;
                const BindingFlags all = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (MethodBase m in t.GetMethods(all).Cast<MethodBase>().Concat(t.GetConstructors(all)))
                {
                    byte[] il;
                    try
                    {
                        MethodBody body = m.GetMethodBody();
                        il = body != null ? body.GetILAsByteArray() : null;
                    }
                    catch
                    {
                        il = null;
                    }
                    if (il == null)
                        continue;
                    for (int i = 0; i + 4 < il.Length; i++)
                    {
                        if (il[i] != 0x72 || il[i + 4] != 0x70)
                            continue;   // ldstr, and a user-string token (its top byte 0x70)
                        try
                        {
                            seen.Add(module.ResolveString(BitConverter.ToInt32(il, i + 1)));
                            i += 4;
                        }
                        catch
                        {
                            // an operand's byte that looked like ldstr: carry on
                        }
                    }
                }
            }
            var lines = seen.Where(Wordy).ToList();
            lines.Sort(StringComparer.Ordinal);
            return lines;
        }

        /// Words a player reads: a space, small letters, eight characters or more, not a file path, a native's name or a key.
        public static bool Wordy(string s)
        {
            if (s == null || s.Length < 8 || s.IndexOf(' ') < 0 || s.IndexOf('\t') >= 0 || s.IndexOf('\n') >= 0)
                return false;
            if (s.Contains("\\") || s.Contains("://") || s.StartsWith("WORLD_HUMAN") || s.StartsWith("PROP_") || s.Contains("_0x"))
                return false;
            int sp = s.IndexOf(' ');
            if (sp >= 3 && s.Substring(0, sp).All(c => char.IsUpper(c) || c == ':' || c == '_'))
                return false;   // a log line's tag ("WORRY settled...", "SCENE ...")
            if (char.IsWhiteSpace(s[0]) || s.StartsWith(";") || s.StartsWith("Settings:") || s.Contains(" = "))
                return false;   // a piece of a longer string, the ini's own notes, the settings' log lines
            return s.Any(char.IsLower) && s.Count(char.IsLetter) * 2 >= s.Length;
        }

        /// The template file's text: a note at the top, then each line followed by a TAB and nothing.
        public static string TemplateText(IEnumerable<string> lines)
        {
            var sb = new StringBuilder();
            sb.Append("# RealNPCs: every line it can show, one a line. Write the translation after the TAB, save the file as translation.txt\r\n");
            sb.Append("# in the same folder (UTF-8), and RealNPCs shows yours. Keep the {slots} as they are: {who} is a person, {town} a town, and\r\n");
            sb.Append("# so on; put them where your language wants them. Lines left blank after the TAB stay English. Lines starting with # are notes.\r\n");
            foreach (string l in lines)
                sb.Append(l).Append("\t\r\n");
            return sb.ToString();
        }
    }
}
