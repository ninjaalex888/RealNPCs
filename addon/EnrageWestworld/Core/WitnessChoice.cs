// Witnesses who choose (FIVEDAY item 14; roadmap.md's idea bank: "after a crime, threaten or pay a named witness. The
// frightened tell a different story; the paid say nothing"; NIGHT3's: "every reaction has a source; witnesses choose").
// Until now everyone who saw the player do violence told it, an hour on, if they lived (Knowing.cs). Now each witness the
// mind knows decides: tell it, or keep quiet (out of fear of him, or loyalty to him), by who they are. A witness who keeps
// quiet doesn't count as one who'll tell; if nobody who saw it will, the town's talk never learns it was him (as when they
// all died). And within the hour, a gun pointed at a witness leans on them: the frightened swear they saw nothing, the
// proud and the law-abiding look him in the eye and tell it sooner. Behind [World] WitnessesChoose.
//
// Why it reads true (the psychologist's lens): a bystander's report weighs its cost against the duty: fear of reprisal
// silences, loyalty silences, a tie to the victim and respect for the law speak up; intimidation works on the anxious and
// backfires on the proud (reactance). In 1899 whoever saw a killing in a small town was marked, and knew it.
// Plain C#, no game; all words original. mindtest WitnessChoiceTest.
using System;

namespace EnrageWestworld.Core
{
    public static class WitnessChoice
    {
        /// Whether a witness keeps quiet about what they saw him do, and why ("afraid", "loyal", or null: they'll tell).
        /// afraid: they fear him; fond: they're fond of him (a friend by the bar's rule); victimFriend: the dead or hurt was
        /// one they cared for. pct: how likely the silence is.
        public static string Keeps(Traits t, bool afraid, bool fond, bool victimFriend, string seed, out int pct)
        {
            float q = 0.08f;
            if (t != null)
                q += 0.06f * t.Z[Traits.Anxiety] - 0.07f * t.Z[Traits.Deference] - 0.05f * t.Z[Traits.Honesty] - 0.03f * t.Z[Traits.Assertiveness];
            if (afraid)
                q += 0.25f;
            if (fond)
                q += 0.30f;
            if (victimFriend)
                q -= 0.30f;   // grief wants him answering for it
            q = Math.Max(0f, Math.Min(0.85f, q));
            pct = (int)Math.Round(q * 100);
            if (pct <= 0 || !Voice.Chance(seed + "|keeps", pct))
                return null;
            return fond && (!afraid || Voice.Chance(seed + "|why", 50)) ? "loyal" : "afraid";
        }

        /// A gun on a witness, within the hour: true they swear silence; false they defy him (and tell it sooner). pct: the
        /// chance they give in.
        public static bool GivesIn(Traits t, string seed, out int pct)
        {
            float g = 0.55f;
            if (t != null)
                g += 0.15f * t.Z[Traits.Anxiety] - 0.12f * t.Z[Traits.Assertiveness] - 0.08f * t.Z[Traits.Deference] - 0.06f * t.Z[Traits.Risk];
            g = Math.Max(0.1f, Math.Min(0.95f, g));
            pct = (int)Math.Round(g * 100);
            return Voice.Chance(seed + "|givesin", pct);
        }

        private static readonly string[] Silent =
        {
            "I didn't see a thing, mister. Not a thing.",
            "I was looking the other way. I always am.",
            "Nothing. I saw nothing. Please.",
            "My eyes are bad. Everybody knows it.",
            "I've a family. I never saw your face.",
            "Whatever happened, I was at home. Ask anybody.",
        };

        private static readonly string[] Defiant =
        {
            "Shoot me, then. Everyone'll know why.",
            "You can't put a bullet in the whole town.",
            "I saw what I saw. Put that away.",
            "Go on. The marshal will hear it from my widow if not from me.",
        };

        public static string SilentLine(string seed) { return Pick(Silent, seed + "|s"); }
        public static string DefiantLine(string seed) { return Pick(Defiant, seed + "|d"); }

        /// The thought of one who keeps quiet, as they go about their day.
        public static string QuietThought(string why, string seed)
        {
            return why == "loyal"
                ? Voice.Pick(seed + "|ql", "I saw it. I'll not be the one to tell. He's been decent to me.", "Some things a body keeps. That's one.", "They can ask. I've a short memory, for a friend.")
                : Voice.Pick(seed + "|qa", "I saw his face. He saw mine. I'll say nothing.", "Keep your head down. Keep your mouth shut. Live.", "If I tell it, I'm next. So I don't.");
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static System.Collections.Generic.IEnumerable<string> AllLines()
        {
            foreach (string l in Silent) yield return l;
            foreach (string l in Defiant) yield return l;
            for (int i = 0; i < 9; i++)
            {
                yield return QuietThought("loyal", "l" + i);
                yield return QuietThought("afraid", "a" + i);
            }
        }
    }
}
