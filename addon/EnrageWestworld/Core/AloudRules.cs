// Thoughts said aloud by rule (the no-model run, item 2; Lex, 2026-11-01). Without the model, or without a voice list for
// that person, what they say aloud as a thought shows comes from what they feel most now (MindShadow.Strongest) and where
// they are: the game's own recorded remarks, played by name so the game picks a take the voice has (Speech.SayAny). Only
// feelings with an outward sound are ever voiced; grief, shame, guilt, longing, pride, envy and pity stay inside, and so
// does anything the face hid (masked or turned). Coarse words only with CrudeTalk. Plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public static class AloudRules
    {
        /// How much of a feeling must be left in it to be voiced at all, and for the louder remark.
        public const float Voiced = 0.25f, Loud = 0.5f;

        /// The remarks to try, in order (the first the voice has is said), or null: kept in. kind: Feeling.Kinds index, or -1
        /// for no feeling to speak of; left: Mood.Lasting; aboutNear: the one it's about is close by (a reproach needs
        /// someone to hear it).
        public static string[] Say(int kind, float left, bool masked, bool aboutNear, bool crude, bool drunk, bool wetOutdoors)
        {
            string k = kind >= 0 && kind < Feeling.Kinds.Length ? Feeling.Kinds[kind] : null;
            if (k != null && left >= Voiced && !masked)
            {
                bool loud = left >= Loud;
                switch (k)
                {
                    case "anger":
                        if (!crude)
                            return new[] { "VOCAL_FX_DISAPPROVE" };
                        return loud ? new[] { "GENERIC_CURSE_HIGH", "GENERIC_CURSE_MED" } : new[] { "GENERIC_CURSE_MED", "VOCAL_FX_DISAPPROVE" };
                    case "contempt":
                        return aboutNear && loud ? new[] { "SHAME_ON_YOU", "VOCAL_FX_DISAPPROVE" } : new[] { "VOCAL_FX_DISAPPROVE" };
                    case "fear":
                        return new[] { "GENERIC_FRIGHTENED_MED" };
                    case "relief":
                        return new[] { "RELIEVED_REACT" };
                    case "gratitude":
                        return new[] { "GENERIC_THANKS" };
                    case "joy":
                        return new[] { "GOING_WELL" };
                }
                return null;   // grief, shame, guilt, pride, envy, pity: always kept in
            }
            if (k != null && left >= Voiced)
                return null;   // a strong feeling they're hiding: no idle talk over it either
            if (drunk)
                return new[] { "DRUNK" };
            if (wetOutdoors)
                return new[] { "CHAT_BAD_WEATHER" };
            return null;
        }

        /// Every line name Say can give (for the offline check against the voice list's counts).
        public static readonly string[] Names = { "VOCAL_FX_DISAPPROVE", "GENERIC_CURSE_HIGH", "GENERIC_CURSE_MED", "SHAME_ON_YOU", "GENERIC_FRIGHTENED_MED", "RELIEVED_REACT", "GENERIC_THANKS", "GOING_WELL", "DRUNK", "CHAT_BAD_WEATHER" };
    }
}
