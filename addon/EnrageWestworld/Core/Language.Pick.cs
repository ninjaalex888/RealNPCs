// The model picks what someone does (Lex, 2026-10-30: "let it pick between a few safe options and go from there"; the
// first step toward the model deciding). The mind still scores every action that's proper here and now; the best few
// of those, and a plan's own step when the one it's about is near, are put to the model with who the person is and how
// they feel, and it answers with a number. Every option is one the mind would allow, so whatever it picks is safe; a
// late, empty or unreadable answer leaves the mind's own choice. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static partial class Language
    {
        public const int MostOptions = 4;
        private static readonly Regex FirstNumber = new Regex(@"\d+", RegexOptions.CultureInvariant);

        /// What the model is told it's doing when it picks.
        public static string PickBrief(LanguageRequest r)
        {
            int year = r != null && r.Year > 0 ? r.Year : 1899;
            if (r != null && r.WakeStage >= Awakening.Questioning)
                return WakeOdd.PickBrief(year, r.WakeStage);   // the waking (Lex, 2026-10-31: the model decides, lash-outs too)
            return "You decide what one person living in " + year + ", in the American West and South, does next. You are given who they are, where, " +
                   "how they feel and what is on their mind, and a short list of things they might do. Choose the one this person would do, " +
                   "by their nature and their feelings, not the one that's best for them. Answer with its number alone.";
        }

        /// The request in words, with the choices numbered.
        public static string PickPrompt(LanguageRequest r)
        {
            var b = new StringBuilder(Describe(r));
            b.Append("What they might do:\n");
            for (int i = 0; i < r.Options.Count; i++)
                b.Append(i + 1).Append(". ").Append(r.Options[i]).Append('\n');
            b.Append("Which do they do? The number alone.");
            return b.ToString();
        }

        /// The body for /api/generate: steadier than a thought, and a few tokens are enough for a number.
        public static string PickBody(string model, LanguageRequest r)
        {
            if (r != null && r.WithThought)
                return ThinkPickBody(model, r);   // the thought and the choice in one answer
            var b = new StringBuilder(1024);
            b.Append("{\"model\":").Append(JsonString(model)).Append(",\"system\":").Append(JsonString(PickBrief(r))).Append(",\"prompt\":").Append(JsonString(PickPrompt(r)));
            b.Append(",\"stream\":false,\"keep_alive\":\"30m\",\"options\":{\"num_ctx\":1024,\"temperature\":").Append(0.5.ToString("0.00", CultureInfo.InvariantCulture))
             .Append(",\"num_predict\":4,\"stop\":[\"\\n\"]}}");
            return b.ToString();
        }

        /// The option the answer names (0-based), or -1 when it names none of them: the first number in its first few
        /// characters, within the list.
        public static int ParsePick(string answer, int count)
        {
            if (string.IsNullOrWhiteSpace(answer) || count <= 0)
                return -1;
            string a = answer.Trim();
            Match m = FirstNumber.Match(a.Length > 12 ? a.Substring(0, 12) : a);
            int n;
            if (!m.Success || !int.TryParse(m.Value, NumberStyles.None, CultureInfo.InvariantCulture, out n))
                return -1;
            return n >= 1 && n <= count ? n - 1 : -1;
        }

        // ---- the thought and the choice in one answer (Lex, 2026-10-31: "having their inner thoughts reflect the model's
        // inner thoughts for that NPC better") ----------------------------------------------------------------------
        // Asked as two lines, "Thought: ..." then "Choice: N", so the thought over their head is the one the choice came
        // from. The thought is held to the same rules as any other (Check); the choice is read as before.

        /// The pick's brief, with the thought's rules after it.
        public static string ThinkPickBrief(LanguageRequest r)
        {
            return PickBrief(r).Replace(" Answer with its number alone.", "") +
                   " First write the private thought in their head as they decide: short fragments, the way people think, not speak, " + MaxWords + " words at most, " +
                   "plain words of the time, no modern words, no poetry, never a name you were not given, never a mention of video games. Then the number of what they do. " +
                   "Answer in exactly two lines, nothing else:\nThought: the thought\nChoice: the number";
        }

        public static string ThinkPickPrompt(LanguageRequest r)
        {
            return PickPrompt(r).Replace("Which do they do? The number alone.", "What do they think, and which do they do? Two lines: Thought, then Choice.");
        }

        public static string ThinkPickBody(string model, LanguageRequest r)
        {
            var b = new StringBuilder(1024);
            b.Append("{\"model\":").Append(JsonString(model)).Append(",\"system\":").Append(JsonString(ThinkPickBrief(r))).Append(",\"prompt\":").Append(JsonString(ThinkPickPrompt(r)));
            b.Append(",\"stream\":false,\"keep_alive\":\"30m\",\"options\":{\"num_ctx\":1024,\"temperature\":").Append(0.7.ToString("0.00", CultureInfo.InvariantCulture))
             .Append(",\"num_predict\":72,\"stop\":[\"\\n\\n\\n\"]}}");   // a blank line comes between the two: stopping at one lost the choice (sampled 2026-10-31)
            return b.ToString();
        }

        /// The choice (0-based, or -1) and the thought (tidied, unchecked; null when there is none) from a two-line answer.
        /// A bare number, as the old answers were, still reads as the choice.
        public static int ParseThinkPick(string answer, int count, out string thought)
        {
            thought = null;
            if (string.IsNullOrWhiteSpace(answer))
                return -1;
            int choice = -1;
            foreach (string raw in answer.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim();
                if (line.StartsWith("Thought:", StringComparison.OrdinalIgnoreCase))
                    thought = Tidy(line.Substring(8).Trim());
                else if (line.StartsWith("Choice:", StringComparison.OrdinalIgnoreCase))
                    choice = ParsePick(line.Substring(7), count);
                else if (choice < 0 && thought != null && line.Length > 0 && line.Length <= 4 && char.IsDigit(line[0]))
                    choice = ParsePick(line, count);   // a bare number on the line after the thought (seen in sampling)
            }
            if (thought != null && thought.Length > 1 && char.IsLower(thought[0]))
                thought = char.ToUpperInvariant(thought[0]) + thought.Substring(1);
            if (choice < 0 && thought == null)
                choice = ParsePick(answer, count);
            return choice;
        }
    }

    public static partial class Choosing
    {
        /// The best few proper actions, best first, with what each does for them (Score's own why): the options put to the
        /// model. Empty when a feeling decides (its urge goes ahead of any choosing).
        public static List<KeyValuePair<string, string>> Ranked(NeedState n, Traits t, ChoiceContext c, string seed, int most)
        {
            var scored = new List<KeyValuePair<float, KeyValuePair<string, string>>>();
            if (c.Urge != null && c.Urge != "none" && c.FeelingLeft >= Interrupts)
                return new List<KeyValuePair<string, string>>();
            foreach (Doing d in All)
            {
                string why;
                float s = Score(d, n, t, c, seed, out why);
                if (!float.IsNaN(s))
                    scored.Add(new KeyValuePair<float, KeyValuePair<string, string>>(s, new KeyValuePair<string, string>(d.Id, why ?? "their nature")));
            }
            scored.Sort((x, y) => y.Key.CompareTo(x.Key));
            var top = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < scored.Count && top.Count < most; i++)
                top.Add(scored[i].Value);
            return top;
        }
    }
}
