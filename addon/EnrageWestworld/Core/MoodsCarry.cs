// Moods that carry (SIXDAY item 14; roadmap 18), the two pieces that were missing: the weather wears people down, and a
// feeling shown in a crowd is caught by those near it. The weather: rain, snow, a storm, fog and dust push the mood of
// everyone out in it a little toward sore and flat (a storm keys people up instead), a quarter of it under a roof (the
// gloom comes in; the wet doesn't); the unsteady feel it more. Why it reads true: weather moves mood in every study that
// has looked (small, real, and larger for those who mind it), and 1899 had no way out of the rain. The catching: when a
// happening is appraised for those present, anyone who felt little of it themselves catches the strongest feeling shown by
// another present, at a share set by who they are (the agreeable and the open catch more, the steady less: the
// emotional-contagion account, mimicry and feedback), never a feeling kept off the face, and never more than half of what
// the source felt. A caught feeling is one of the ordinary kinds (fear, anger, grief...), so it moves the mood, the
// thoughts and the street's leans as any other does, marked "caught" so a thought can say so. Plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public static class MoodsCarry
    {
        public const float Indoors = 0.25f;                       // the share of the weather's push felt under a roof
        public const float CatchMost = 0.5f;                      // never more than this share of the source's feeling
        public const float CatchLeast = 0.1f, CatchBase = 0.3f;   // the share at rest, and its floor

        /// The weather's push on the mood (pleasure, arousal, dominance) for someone out in it; a quarter of it indoors;
        /// nothing for fair weather the add-on doesn't name, or none known.
        public static PadMood Weather(string weather, bool indoors)
        {
            float p, a, d = 0f;
            switch (weather)
            {
                case "rain": p = -0.10f; a = -0.06f; d = -0.04f; break;
                case "storm": p = -0.10f; a = 0.08f; d = -0.06f; break;
                case "snow": p = -0.14f; a = -0.10f; d = -0.05f; break;
                case "fog": p = -0.05f; a = -0.04f; break;
                case "dust": p = -0.06f; a = 0.02f; break;
                case "grey": p = -0.03f; a = -0.03f; break;
                case "sun": p = 0.04f; a = 0.02f; d = 0.02f; break;
                default: return new PadMood(0f, 0f, 0f);
            }
            float k = indoors ? Indoors : 1f;
            return new PadMood(p * k, a * k, d * k);
        }

        /// How much the weather moves this person: the unsteady feel it half again as much, the steady half as much.
        public static float WeatherWeight(Traits t)
        {
            if (t == null)
                return 1f;
            return Clamp(1f + 0.5f * (t.Domain(4) / 3f), 0.5f, 1.5f);   // neuroticism, about -1..1
        }

        /// The mood with the weather on it; unchanged when there is no push (the offline runs, a roof in fair weather).
        public static PadMood Worn(PadMood mood, PadMood push, Traits t)
        {
            if (push.P == 0f && push.A == 0f && push.D == 0f)
                return mood;
            float w = WeatherWeight(t);
            return new PadMood(Clamp(mood.P + push.P * w, -1f, 1f), Clamp(mood.A + push.A * w, -1f, 1f), Clamp(mood.D + push.D * w, -1f, 1f));
        }

        /// How much of another's shown feeling this person catches: 0.3 at rest, more for the agreeable, the open and the
        /// unsteady, less for the rest; 0.1 to 0.5.
        public static float Susceptibility(Traits t)
        {
            if (t == null)
                return CatchBase;
            float o = t.Domain(0) / 3f, agree = t.Domain(3) / 3f, n = t.Domain(4) / 3f;
            return Clamp(CatchBase + 0.12f * agree + 0.06f * o + 0.08f * n, CatchLeast, CatchMost);
        }

        /// Whether a feeling can be caught at all: on the face (shown or softened, or not yet judged), at least moderate.
        public static bool Catchable(Feeling f)
        {
            return f != null && f.Kind != null && f.Kind != "none" && f.Intensity >= Appraisal.Moderate && (f.Shows == null || f.Shows == "shown" || f.Shows == "softened");
        }

        /// The feeling caught off a source's face by someone who felt less of the same happening themselves (own: their own
        /// intensity from it), or null when there's nothing to catch. The kind and whom it's about are the source's; Why
        /// "caught"; DoerId the source, whose face it was read off; shown softened (a caught feeling is a smaller thing).
        public static Feeling Catch(Feeling source, string sourceId, float own, Traits t)
        {
            if (!Catchable(source))
                return null;
            float got = source.Intensity * Susceptibility(t);
            if (got < Appraisal.Felt || got <= own)
                return null;
            return new Feeling { Kind = source.Kind, Intensity = got, About = source.About, AboutId = source.AboutId, DoerId = sourceId, Why = "caught", Urge = null, Shows = "softened" };
        }

        private static float Clamp(float v, float lo, float hi)
        {
            return v < lo ? lo : v > hi ? hi : v;
        }
    }
}
