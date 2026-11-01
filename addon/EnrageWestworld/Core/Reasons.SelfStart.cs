// People start a scene from their own reasons, without the model (the no-model run, item 3; Lex, 2026-11-01: on top of
// the street's own scenes). Until now a person's reason to go to someone (call in a debt, comfort a friend, have words)
// was played only when the model picked it; without the model nobody chose it for themselves. The pull of a reason: its
// strength, the person's temper (the assertive and the angry for a quarrel or a debt, the soft-hearted for comfort, the
// sociable for news and talk) and an urge of a lasting feeling that fits it (confront, repay, help, amends). Past the
// floor they may go, more likely the stronger the pull, seeded so the same person in the same minute answers the same.
// Plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        /// A pull at or over this may start the scene; at 1.0 it nearly always does (the chance tops out at SelfStartMost).
        public const float SelfStartFloor = 0.55f, SelfStartMost = 0.7f;

        /// How strongly this person's own reason pulls them to start it now (0 for a kind they'd never start themselves).
        public static float SelfStartPull(Reason why, Traits t, string urge)
        {
            if (why == null || string.IsNullOrEmpty(why.Kind) || why.Kind == "avoid" || why.Kind == "hosts")
                return 0f;
            float Z(int i) => t != null ? t.Z[i] : 0f;
            float pull = why.Strength / 100f, lean = 0f;
            string k = why.Kind;
            bool hard = k == "argue" || k == "brawl" || k == "rough" || k == "tiff" || k == "drunk" || k == "x:debt" || k == "x:feud" || k == "x:quarrel" || k == "x:snap" || k == "debt";
            bool soft = k == "condole" || k == "tend" || k == "mend" || k == "x:sweet";
            bool talk = k == "smalltalk" || k == "gossip" || k == "x:goodnews" || k == "x:badnews" || k == "beg";
            if (hard)
                lean = 0.06f * Z(Traits.Assertiveness) + 0.06f * Z(Traits.Anger) - 0.04f * Z(Traits.SelfControl);
            else if (soft)
                lean = 0.08f * Z(Traits.Compassion) + 0.03f * Z(Traits.Sociability);
            else if (talk)
                lean = 0.08f * Z(Traits.Sociability) + 0.02f * Z(Traits.Curiosity);
            pull += Math.Max(-0.15f, Math.Min(0.15f, lean));
            if (urge != null && ((hard && (urge == "confront" || urge == "repay")) || (soft && (urge == "help" || urge == "comfort" || urge == "amends")) || (talk && urge == "share")))
                pull += 0.15f;
            return pull;
        }

        /// Whether they go: past the floor, with a chance that grows with the pull; seeded.
        public static bool SelfStarts(float pull, string seed)
        {
            if (pull < SelfStartFloor)
                return false;
            float chance = Math.Min(SelfStartMost, 0.25f + (pull - SelfStartFloor) * 1.2f);
            return Voice.Chance(seed + "|selfstart", (int)Math.Round(chance * 100));
        }
    }
}
