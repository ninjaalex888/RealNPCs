// What a road scene does when you walk up on it (TENHOUR item 11; built on the circle's answer, FIVEDAY2 item 10, and the everyday picks). The scene plays out by its beats whether you're there or not, but the people in it
// would notice a stranger: so once you're close, one of them decides what to do about you, from a short list that fits the kind of scene. The model's word if it's on and quick enough, the rules' if not.
// Why it reads true: being noticed is what separates walking through a world from watching one. The men at the branding fire should mind that you've come over the ridge.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        public const string AnswerCarry = "carry", AnswerWarn = "warn", AnswerCall = "call", AnswerAsk = "ask", AnswerTurn = "turn";
        public const int AnswerWaitMs = 3000, AnswerAfterMs = 1500;
        public const float AnswerMetres = 14f;

        private static readonly Dictionary<string, string> AnswerWordsOf = new Dictionary<string, string>
        {
            { AnswerCarry, "they carry on and pay the stranger no mind" },
            { AnswerWarn, "one of them warns the stranger off" },
            { AnswerCall, "one of them calls the stranger over" },
            { AnswerAsk, "one of them begs the stranger for help" },
            { AnswerTurn, "the armed ones turn on the stranger" },
        };

        public static string AnswerWords(string id)
        {
            string w;
            return AnswerWordsOf.TryGetValue(id, out w) ? w : id;
        }

        private static readonly string[] TurnKinds = { "rustlers", "moonshine" };   // those with something to hide: a soldier or a sharpshooter has no reason to shoot a passer-by
        private static readonly string[] DisputeKinds = { "teamsters", "hunters", "cards", "sheepherder", "surveyors", "strike" };   // two sides who might want a third opinion

        /// Whether this is a scene that might turn on someone who walks up on it.
        public static bool CanTurn(WaysideKind k) { return k != null && System.Array.IndexOf(TurnKinds, k.Id) >= 0; }

        public const int TurnOfferedPct = 30;   // sampled against llama3.2:3b, 30 answers at a firing party: offered every time, the model turned on the stranger 12 times. The numbers it can't see, it can't pick

        /// What they might do about a stranger walking up, by the kind of scene it is. Only the dead have nothing to say. Turning on you is on the list only some of the time, by the seed.
        public static List<string> AnswerMenu(WaysideKind k, string seed)
        {
            var m = new List<string>();
            if (k == null || k.Cast == null || k.Id == KnownId || k.Cast.All(c => c.Act == "dead"))
                return m;   // the dead have nothing to say, and someone you know has their own
            bool armed = k.Cast.Any(c => c.Arm != null);
            m.Add(AnswerCarry);
            if (armed)
                m.Add(AnswerWarn);
            if (System.Array.IndexOf(DisputeKinds, k.Id) >= 0)
                m.Add(AnswerCall);
            if (k.NewsIfHelped)
                m.Add(AnswerAsk);   // the hurt, the snake-bitten and the stranded
            if (armed && CanTurn(k) && Voice.Chance(seed + "|turnoffer", TurnOfferedPct))
                m.Add(AnswerTurn);
            return m;
        }

        /// Who speaks for them: the first with a gun, or the first alive.
        public static int AnswerSpeaker(WaysideKind k, System.Func<int, bool> alive)
        {
            for (int i = 0; i < k.Cast.Length; i++)
                if (k.Cast[i].Arm != null && alive(i))
                    return i;
            for (int i = 0; i < k.Cast.Length; i++)
                if (alive(i))
                    return i;
            return -1;
        }

        public static string AnswerBrief(int year)
        {
            return "You decide what people away from the towns in " + year + ", in the American West and South, do when a stranger walks up on them in the middle of something. You are told who they are, what they are doing and a short list of things they might do. " +
                   "Choose the one they would do: most people mind their own business, and those with something to hide want the stranger gone. Answer with its number alone.";
        }

        /// The words a scene's own label has for what's going on, without the row's "In the wilds:".
        public static string SceneWords(WaysideKind k)
        {
            if (k == null || k.Test == null)
                return "something going on";
            int c = k.Test.IndexOf(": ", System.StringComparison.Ordinal);
            return c >= 0 ? k.Test.Substring(c + 2) : k.Test;
        }

        public static LanguageRequest AnswerRequest(WaysideKind k, int speaker, int year, int hour, List<string> menu)
        {
            var r = new LanguageRequest
            {
                Kind = "pick", RoadScene = true, WithThought = true, Name = k.Cast[speaker].Name, Woman = k.Cast[speaker].Woman, Trade = "one of " + k.Cast.Length + " people out here", Town = null, Hour = hour, Year = year,
                Place = k.OnRoad ? "on a road" : "on open ground", Mood = "on edge", Now = "a stranger has walked up on them; what's going on: " + SceneWords(k)
            };
            r.Options = menu.ConvertAll(AnswerWords);
            return r;
        }

        /// The rules' choice from the same list.
        public static string AnswerRules(List<string> menu, string seed)
        {
            int roll = Voice.Hash(seed + "|answer") % 100, then = Voice.Hash(seed + "|answer2") % 100;
            if (menu.Contains(AnswerAsk) && roll < 75)
                return AnswerAsk;
            if (menu.Contains(AnswerTurn) && roll < 8)
                return AnswerTurn;
            if (menu.Contains(AnswerWarn) && then < 55)
                return AnswerWarn;
            return then < 80 || !menu.Contains(AnswerCall) ? AnswerCarry : AnswerCall;
        }

        private static readonly string[] AnswerWarnLines = { "That's far enough.", "Keep going, friend. Nothing here for you.", "You lost, mister? Then get found somewhere else.", "Mind your own affairs, friend.", "Whatever you think you saw, you didn't." };
        private static readonly string[] AnswerCallLines = { "You there. Come and look at this.", "Hey! You want to settle something for us?", "Stranger! You've got an honest face. Come here.", "You. Who do you say's in the right?" };
        private static readonly string[] AnswerAskLines = { "Mister! Over here! For the love of God!", "Please. Please, you've got to help me.", "Don't go by. Don't go by me.", "You're the first soul I've seen all day." };
        private static readonly string[] AnswerTurnLines = { "He's seen too much.", "Should've kept going, friend.", "Get him. Don't let him get away.", "That's one too many who knows." };

        /// What they say to you, or null when they say nothing.
        public static string AnswerLine(string what, string seed)
        {
            string[] pool = what == AnswerWarn ? AnswerWarnLines : what == AnswerCall ? AnswerCallLines : what == AnswerAsk ? AnswerAskLines : what == AnswerTurn ? AnswerTurnLines : null;
            return pool != null ? Voice.Pick(seed + "|answerl", pool) : null;
        }

        public static IEnumerable<string> AnswerLines()
        {
            foreach (string[] p in new[] { AnswerWarnLines, AnswerCallLines, AnswerAskLines, AnswerTurnLines })
                foreach (string l in p)
                    yield return l;
        }
    }
}
