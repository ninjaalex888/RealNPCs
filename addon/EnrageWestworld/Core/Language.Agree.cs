// The agreement guard (LIVES item 3b, 2026-11-12; [Mind] ModelAgrees, settings version 475). The mind decides what someone
// feels; the model only words it (the language layer's first rule, Core/Language.cs). But a small local model now and then
// writes a cheerful line for a person the mind has frightened or grieving ("Fine morning. Coffee's hot." for a widow of a
// day), or a bitter one for someone in joy, and the line goes over their head as if it were what they feel. Now the check
// holds an answer whose plain words go against a strong or fair feeling it was given: delight for the frightened, the angry,
// the ashamed and the grieving; hate and despair for the joyful, the grateful and the relieved. The written line stands in
// (as for any held answer), and the hold is counted ("against the feeling").
//
// Why: the player reads a thought as the person's truth (it says what is really felt, even when the face hides it: the
// brief), so one that contradicts what they just lived through breaks the person, not just the line. It reads words, not
// meaning: a narrow list, so a masked feeling's brave front ("I'm fine. I'm fine.") isn't held (those words aren't in it).
// Off in Core so the language goldens stand. Plain C#.
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static partial class Language
    {
        /// [Mind] ModelAgrees: set by the game from the settings.
        public static bool AgreeOn;

        public const string AgainstFeeling = "against the feeling";

        private static readonly Regex Cheerful = new Regex(@"\b(glad|happy|happiest|delighted|wonderful|lovely|cheerful|merry|joyful|grand day|fine day|fine morning|best day|can't stop smiling|whistl(e|ed|ing)|laugh(ed|ing)? out loud|light as a feather|on top of the world)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Bitter = new Regex(@"\b(hate|hateful|despair|wretched|miserable|wish I were dead|wish I was dead|curse (him|her|them|it)|damn (him|her|them|it)|no hope|hopeless)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static bool Dark(string f) { return f == "fear" || f == "anger" || f == "shame" || f == "guilt" || f == "grief" || f == "contempt"; }
        private static bool Bright(string f) { return f == "joy" || f == "gratitude" || f == "relief" || f == "pride"; }

        /// The words that go against the feeling given, or null: only for a feeling of some strength, not a mild one.
        public static string AgainstFeelingIn(LanguageRequest r, string answer)
        {
            if (!AgreeOn || r == null || string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(r.Feeling) || r.Strength == "mild")
                return null;
            if (r.Kind == "talk" || r.Kind == "free")
                return null;   // Talk is frozen: its answers keep their own check (the halfway review)
            if (r.Shows == "masked" && Dark(r.Feeling))
                return null;   // a brave face may say "glad" aloud; the thought is checked by its words alone, so leave a masked feeling be
            Match m = Dark(r.Feeling) ? Cheerful.Match(answer) : Bright(r.Feeling) ? Bitter.Match(answer) : Match.Empty;
            return m.Success ? m.Value.ToLowerInvariant() + " for " + r.Feeling : null;
        }
    }
}
