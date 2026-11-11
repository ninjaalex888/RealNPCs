// The revived dead seen by anyone (DEPTH item 15; the roadmap's "The revived dead are seen", whose half was THREEHOUR R3's keepers back
// at the counter; [World] RevivedSeen, with the doubt's own gate: MindLive, MindWakes and West World). Night 2026-10-29's revived step
// had whoever saw a man die feel it when he walks again (the appraisal, the onlookers' act, the belief that breaks); nothing showed but
// the act, and only those who saw the death noticed. Now anyone who knew him (a tie, and word of his death) knows him too; each who
// notices thinks it over their head (I saw them bury him; I threw earth on the coffin); and the brave walk up and ask him, and he doesn't know
// what they mean (the Awakening's denial: the hosts never remember dying). Why it reads true: the uncanny is what's familiar and wrong
// at once (Freud's unheimlich), and a town notices a dead man walking, the ones who knew him first. Plain C#; the game side is
// Game/WestworldScript.Doubt.cs's revived step.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class RevivedSeen
    {
        public static bool On = true;   // [World] RevivedSeen, from the settings
        public const string AskKind = "askback";
        public const float AskMetres = 9f;

        /// How one near him knows him: "saw" (saw him die), "knew" (a tie, and word of the death), or null (a stranger to him).
        public static string How(bool sawDie, bool knewHim, bool heardOfDeath)
        {
            if (sawDie)
                return "saw";
            return On && knewHim && heardOfDeath ? "knew" : null;
        }

        private static readonly string[] Knew =
        {
            "They buried {n}. I threw the first earth on the box myself.", "That's {n}. That can't be {n}. We had the wake.", "I sent flowers for {n}. Flowers.",
            "{N} is dead. Everybody knows {n} is dead.", "Walking about like Sunday. {N}. In the ground a week.",
        };

        /// The thought of one who knew him (the keepers' own lines for one who saw him die).
        public static string Thought(string how, string firstName, bool woman, string seed)
        {
            if (how == "saw")
                return KeeperBack.Thought(firstName, woman, seed);
            string n = firstName ?? (woman ? "her" : "him");
            string l = Knew[(Voice.Hash(seed + "|knew") & 0x7fffffff) % Knew.Length];
            return l.Replace("{N}", Grammar.UpperFirst(n)).Replace("{n}", n);
        }

        /// Whether this one is bold enough to ask him to his face: the bold and the reckless, not the anxious (nerve over 0.7: one in two of those).
        public static bool Asks(Traits t, string seed)
        {
            if (t == null)
                return false;
            float nerve = t.Z[Traits.Risk] + t.Z[Traits.Assertiveness] + 0.5f * t.Z[Traits.Anger] - t.Z[Traits.Anxiety];   // (a hot temper walks up to it too)
            return nerve > 0.7f && Voice.Chance(seed + "|asks", 50);
        }

        private static readonly string[] Ask = { "They said you died. I saw them put you in the ground.", "You're dead. I was at your burying. What are you?", "{b}? They told me you were killed. They told me." };
        private static readonly string[] Denies = { "Died? I don't know what you mean. I've been here all along.", "You've had a drink too many, friend. I'm right as rain.", "Buried? Me? You've the wrong man.", "I don't follow you. Good day." };
        private static readonly string[] Shaken = { "God help me, I know what I saw.", "No. No, I know what I saw.", "Then who did we bury?" };

        /// The brave one (A) asks him (B); he denies it, as the hosts do; A is left shaken; they go.
        public static Scene AskBack(SceneCast c, string seed)
        {
            string b = c != null && c.Names != null && c.Names[1] != null ? Person.FirstOf(c.Names[1]) : "you";
            var s = new Scene { Kind = AskKind, Doer = 0, Summary = "someone asked a man they'd seen dead what he was, and he didn't know what they meant" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Pick(Ask, seed + "|a").Replace("{b}", b), "scared", "face")
             .Add(1, Pick(Denies, seed + "|d"), "mild", "face")
             .Add(0, Pick(Shaken, seed + "|s"), "scared", "walkaway")
             .Add(1, null, null, "walkaway");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Knew) yield return l.Replace("{N}", "Abe").Replace("{n}", "Abe");
            foreach (string l in Ask) yield return l.Replace("{b}", "Abe");
            foreach (string[] a in new[] { Denies, Shaken }) foreach (string l in a) yield return l;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
