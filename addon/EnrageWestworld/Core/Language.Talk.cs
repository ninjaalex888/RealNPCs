// Talk answers in the model's words (NIGHT3; Lex: the model words more of what people say). Kind "talk": someone answers
// what Arthur said to them in the interaction menu. What the answer means is fixed by the menu (Interact's written line,
// which stays the fallback: Mean); the model says it in this person's own words, from who they are and how they feel.
// The prompt is kept narrow, after a sample with the live model: what he said in plain terms (Said: "He offers to pay the
// eleven dollars you owe"), the meaning as a hard rule, no names but his own when they know it (the model called him by
// whoever was in sight), and their worry only when he asked after it (it crept into every answer). Checked as a thought
// is (Language.Check), and asked through the picks' channel (taken by key on the game thread, ahead of any thought).
// Plain C#, no game.
using System.Globalization;
using System.Text;

namespace EnrageWestworld.Core
{
    public static partial class Language
    {
        /// What he said, in plain terms for the model (the menu's own words can be read two ways: "settle that debt").
        public static string TalkSaid(string option, int cents = 0)
        {
            switch (option)
            {
                case "news": return "He asks you what the news is.";
                case "name": return "He tells you his name.";
                case "ask": return "He asks after the trouble you told him of.";
                case "drink": return "He offers to buy you a drink.";
                case "give": return "He holds out " + (cents >= 500 ? "five dollars" : "a dollar") + " for you to take, as a gift.";
                case "settle": return "He offers to pay the eleven dollars you owe, so you can be clear of the debt.";
                case "sorry": return "He says he's sorry for how he treated you before.";
                case "threaten": return "He tells you to watch yourself, and means it as a threat.";
                default: return "He says good day.";
            }
        }

        /// The brief for a spoken answer: one or two short sentences, said aloud to a stranger, in the words of 1899.
        public static string TalkBrief(LanguageRequest r)
        {
            int year = r != null && r.Year > 0 ? r.Year : 1899;
            return "You write what one person says aloud, in " + year + ", in the American West and South, answering a stranger who has just spoken to them. " +
                   "One or two short sentences, " + MaxWords + " words at most, the way people of the time talked: plain words, no modern words or phrases, no poetry. " +
                   "Say exactly what you are told the answer means, no more and nothing else: if it accepts, accept; if it refuses, refuse; bring up nothing else. " +
                   "Use no names at all unless you are told his: not his, not your own, not your family's; call him mister, or nothing. Never mention video games, being in a story, or being written. " +
                   "Answer with the spoken words alone: no quotation marks, no name before them, nothing else.";
        }

        /// The ask: who they are and how they feel (only that: the rest of a thought's request pulled the answer away from
        /// its meaning), what he said, what their answer means.
        public static string TalkPrompt(LanguageRequest r)
        {
            // no name of their own in it: given one, the model took it for his (the live sample: "Mr. Cobb", to the stranger)
            var b = new StringBuilder();
            b.Append("You are ").Append(r.Woman ? "a woman" : "a man");
            if (r.Age > 0) b.Append(" of ").Append(r.Age);
            if (!string.IsNullOrEmpty(r.Trade)) b.Append(", ").Append(r.Trade);
            b.Append(".\n");
            if (!string.IsNullOrEmpty(r.TraitWords)) b.Append("Your nature: ").Append(r.TraitWords).Append(".\n");
            if (!string.IsNullOrEmpty(r.Mood)) b.Append("Your mood: ").Append(r.Mood).Append(".\n");
            b.Append("The man speaking to you ").Append(string.IsNullOrEmpty(r.YouAs) ? "is a stranger: you don't know his name" : "is " + r.YouAs + "; you know his name").Append(".\n");
            if (!string.IsNullOrEmpty(r.Worry)) b.Append("Your trouble: ").Append(r.Worry).Append(".\n");
            b.Append(r.Said ?? "He says good day.").Append("\n");
            b.Append("Your answer means: ").Append(r.Mean ?? "a word back").Append("\n");
            b.Append("Say it to him, in your own words.");
            return b.ToString();
        }

        private static readonly System.Text.RegularExpressions.Regex Titled = new System.Text.RegularExpressions.Regex(@"\b(?:Mr|Mrs|Mister|Miss|Ma'am)\.?\s+([A-Z][a-z']+)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        private static readonly string[] Refusing = { "but i", "can't accept", "cannot accept", "won't take", "keep your", "no, thank", "no thank", "don't need", "decline", "have to pass", "ain't takin", "rather not", "not today",
                                                      "don't feel right", "ain't sure i can", "can't take", "couldn't take", "can't let you" };

        private static readonly System.Text.RegularExpressions.Regex AddressEnd = new System.Text.RegularExpressions.Regex(@",\s*(?:(?:Mr|Mrs|Mister|Miss)\.?\s+)?([A-Z][a-z']+)(?=\s*[.!?]\s*$)", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        private static readonly System.Text.RegularExpressions.Regex AddressMid = new System.Text.RegularExpressions.Regex(@",\s*(?:Mr|Mrs|Mister|Miss)\.?\s+([A-Z][a-z']+)(?=\s*[,.!?])", System.Text.RegularExpressions.RegexOptions.CultureInvariant);
        private static readonly System.Text.RegularExpressions.Regex AddressStart = new System.Text.RegularExpressions.Regex(@"^((?:Mr|Mrs|Mister|Miss)\.?\s+)?([A-Z][a-z']+),\s+", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        /// Takes out a form of address the model made up for him ("..., Mr. Jenkins." becomes "..."; "Abe, ..." loses
        /// "Abe, " when Abe is the speaker's own name, `own`), keeping his own name when they know it (`keep`) and the
        /// words anyone may say ("Lord"; "mister" is lowercase and untouched). At the start only a titled name or their
        /// own is taken ("Obliged, ..." stays). The model kept naming him whatever it was told; this is the sure way.
        public static string StripAddress(string line, string keep, string own = null)
        {
            if (string.IsNullOrEmpty(line))
                return line;
            System.Text.RegularExpressions.MatchEvaluator drop = m => m.Groups[1].Value == keep || Common.Contains(m.Groups[1].Value) && m.Groups[1].Value != "Marshal" && m.Groups[1].Value != "Sheriff" && m.Groups[1].Value != "Deputy" ? m.Value : "";
            string s = AddressEnd.Replace(line, drop);
            s = AddressMid.Replace(s, drop);
            string before = s;
            s = AddressStart.Replace(s, m =>
            {
                string name = m.Groups[2].Value;
                bool titled = m.Groups[1].Success, theirs = own != null && System.Array.IndexOf(own.Split(' '), name) >= 0;
                return name == keep || (!titled && !theirs) || Common.Contains(name) ? m.Value : "";
            });
            if (s != before && s.Length > 0)
                s = Grammar.UpperFirst(s);
            return s.Trim();
        }

        /// Whether a spoken answer may be said; why not otherwise. The thought's checks first; then what the live sample
        /// showed: never a name for him but the one they know (the model made one up, or used the speaker's own), never
        /// their own name to him, and an answer that accepts (a drink, money, a debt paid) with no refusal in it.
        public static string TalkCheck(LanguageRequest r, string answer, bool accepts)
        {
            string why = Check(r, answer);
            if (why != null)
                return why;
            string known = r != null && !string.IsNullOrEmpty(r.YouAs) ? r.YouAs.Replace("Mr. ", "") : null;
            foreach (System.Text.RegularExpressions.Match m in Titled.Matches(answer))
                if (known == null || m.Groups[1].Value != known)
                    return "a name for him he didn't give (" + m.Value + ")";
            if (r != null && !string.IsNullOrEmpty(r.Name))
                foreach (string part in r.Name.Split(' '))
                    if (part.Length > 1 && System.Text.RegularExpressions.Regex.IsMatch(answer, @"\b" + System.Text.RegularExpressions.Regex.Escape(part) + @"\b"))
                        return "their own name in it (" + part + ")";
            if (accepts)
            {
                string low = answer.ToLowerInvariant();
                foreach (string w in Refusing)
                    if (low.Contains(w))
                        return "refuses what the answer accepts (" + w + ")";
            }
            return null;
        }

        /// The request body for a spoken answer (one line; cooler than a thought, to keep to its meaning).
        public static string TalkBody(string model, LanguageRequest r)
        {
            var b = new StringBuilder(1024);
            b.Append("{\"model\":").Append(JsonString(model)).Append(",\"system\":").Append(JsonString(TalkBrief(r))).Append(",\"prompt\":").Append(JsonString(TalkPrompt(r)));
            b.Append(",\"stream\":false,\"keep_alive\":\"30m\",\"options\":{\"num_ctx\":1024,\"temperature\":").Append(0.6.ToString("0.00", CultureInfo.InvariantCulture))
             .Append(",\"num_predict\":48,\"stop\":[\"\\n\"]}}");
            return b.ToString();
        }
    }
}
