// Carry the note, and they meet (DEPTH reserve R2; SIXDAY item 1's errand carried the word between two who fell out, and the one it
// went to took it (a makeup tie) or sent it back; [Scenes] NoteMeet, on). Now the next time the two are near each other in the next two
// game days, they meet in the street: taken, a word about it and a hand; sent back, a cold word and they part. Why it reads true: a
// go-between lets pride save its face, but the quarrel isn't over until the two have stood in front of each other, and the meeting is
// where it's settled or isn't. Plain C#; the game side is Game/WestworldScript.Errand.cs (NoteMeetStep).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class NoteMeet
    {
        public static bool On = true;   // [Scenes] NoteMeet, from the settings
        public const string Kind = "notemeet";
        public const int KeepMinutes = 2 * 1440;
        public const float NearMetres = 12f;

        private static readonly string[] SentWarm = { "I meant every word I sent you, {b}.", "{b}. I'm glad you took it. I couldn't say it to your face then.", "So. You got my word." };
        private static readonly string[] TakesWarm = { "I know you did. Here's my hand.", "Took a stranger to carry it. We'll say no more about it.", "I got it. We're square, you and me." };
        private static readonly string[] SentCold = { "You got my word, {b}?", "I sent you word, {b}. You know I did.", "{b}. About what I sent." };
        private static readonly string[] TakesCold = { "I got it. It changed nothing.", "A stranger's mouth doesn't make it right.", "Words are cheap, sent by somebody else." };

        /// A the one who sent the word, B the one it went to. mended: B took it.
        public static Scene Make(SceneCast c, bool mended, string seed)
        {
            string b = Scenes.N(c, 1, seed);
            var s = new Scene { Kind = mended ? "makeup" : "brushoff", Doer = mended ? 0 : 1,
                                Summary = mended ? "two who fell out met after a word was carried between them, and shook hands" : "two who fell out met after a word was carried between them, and parted cold" };
            s.Beats.Add(new Beat(0, null, null, "approach", 200) { To = 1 });
            s.Beats.Add(new Beat(0, Pick(mended ? SentWarm : SentCold, seed + "|s").Replace("{b}", b), "mild", "face") { To = 1 });
            s.Beats.Add(new Beat(1, Pick(mended ? TakesWarm : TakesCold, seed + "|t"), mended ? "thanks" : "storm", "face") { To = 0 });
            s.Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { SentWarm, SentCold }) foreach (string l in a) yield return l.Replace("{b}", "Abe");
            foreach (string[] a in new[] { TakesWarm, TakesCold }) foreach (string l in a) yield return l;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
