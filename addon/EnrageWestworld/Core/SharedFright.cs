// Shared fright (FIVEDAY item 22; NIGHT3's psychologist list). Two people who both saw the player kill are tied by it: the
// tie between them warms a step (the "fright" kind, SceneTie.FrightAt), for two weeks they keep clear of him together when
// both are about, and when the street's quiet side talk (FIVEDAY item 5) falls to the two of them, three times in four it's
// about that, low-voiced. Behind [Mind] SharedFright.
//
// Why it reads true: Schachter's affiliation under threat. Frightened people seek out others in the same plight, to
// compare what they felt and make sense of it, and the shared ordeal binds them. Plain C#, no game; all words original.
// mindtest SharedFrightTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class SharedFright
    {
        public const string Kind = "fright";
        public const long LastsMinutes = 14 * 1440;   // two weeks of it between them
        public const int TalkChance = 75;             // of their quiet talks, this many in a hundred are about it
        public const int MostWitnesses = 6;           // pairs among the first six who saw it (fifteen at most)

        private static readonly HashSet<string> Killings = new HashSet<string> { "kill", "murder", "lawkill", "namedkill" };
        public static bool IsKilling(string deedKind) { return deedKind != null && Killings.Contains(deedKind); }

        public static bool Fresh(SceneTie t, long now) { return t != null && t.FrightAt > 0 && now - t.FrightAt >= 0 && now - t.FrightAt <= LastsMinutes; }

        private static readonly string[][] Talks =
        {
            new[] { "You were there. You saw it.", "Don't. Not here.", "I can't stop seeing it." },
            new[] { "Have you slept?", "Not a wink. You?", "I keep the lamp lit now." },
            new[] { "He looked right at me after. Did he look at you?", "Couldn't say. I kept my eyes on my boots.", "Good. Don't ever." },
            new[] { "Should we tell somebody?", "Tell them what? They know who.", "Then we keep our heads down." },
            new[] { "I'm glad it was you there with me. I'd not want to carry it alone.", "Nor I.", "Walk me home tonight?" },
            new[] { "The way he fell. I keep hearing it.", "Hush. Somebody's listening.", "Nobody listens to us. That's what saved us." },
            new[] { "My hands still shake. Look.", "Mine too. Hide them in your pockets.", "Like that?" },
            new[] { "If he comes back this way...", "We go inside. Both of us. We don't wait.", "Agreed." },
        };

        /// The quiet talk between two who both saw him kill: A starts, B answers, A ends; words only, standing face to face.
        public static Scene Make(SceneCast c, string seed)
        {
            string[] t = Talks[(Voice.Hash(seed + "|fright") & 0x7fffffff) % Talks.Length];
            var s = new Scene { Kind = Kind, Summary = "two who saw the killing talked of it, low-voiced" };
            s.Add(0, t[0], "mild", "face").Add(1, t[1], "mild").Add(0, t[2], "mild");
            return s;
        }

        /// The choices of one whose companion in it is about: keep clear of him, together.
        public static float Lean(bool fright, string act)
        {
            if (!fright)
                return 0f;
            return act == "keepclear" ? 0.15f : act == "steerclear" ? 0.12f : act == "talk" ? 0.08f : act == "showoff" ? -0.1f : 0f;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] t in Talks)
                foreach (string l in t)
                    yield return l;
        }
    }
}
