// Hosts compare notes (FIVEDAY reserve R28, West World). Two waking people (the "hosts" reason: one at stage 2 or more, the
// other stirring) used to trade a strange remark and part. With [West World] HostNotes, half the time they test the world
// together instead: one counted the faces on the street and got the same number two days running, one walked to the town's
// edge and found himself walking back in, or they tell each other their dreams and find the same room in both. And it
// takes: for three days after, each of them wakes faster (the daily roll to wake a stage further, 60%, is 72%).
//
// Why it reads true: doubt alone is easy to talk yourself out of; doubt that someone else confirms isn't. Two people who
// agree on a strange thing stop calling it a dream (social proof, and the relief and fear of not being mad alone). The
// tests are what a sane person would try: count, walk, compare. Plain C#, no game; all words original. mindtest
// HostNotesTest.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class HostNotes
    {
        public const string Summary = "two waking people comparing notes on the world";
        public const int Share = 50, Lift = 12, Days = 3;

        /// Whether it's on (West World and HostNotes), set by Engine so the scene builder reads the live settings.
        public static Func<bool> On = () => false;

        private static readonly string[][] Tests =
        {
            new[] { "0:I counted the faces on the street this morning. Forty-one.", "1:So?",
                    "0:Forty-one yesterday. Same coats. Same order past the bank.", "1:...I'll count tomorrow. If it comes to forty-one, we talk." },
            new[] { "0:I walked east till the town stopped. Wanted to see past the last fence.", "1:And what's past it?",
                    "0:More road. Then I was walking west, into town. I never turned round.", "1:Show me tonight. Both of us, so nobody can say it was a dream." },
            new[] { "0:Tell me your dream first. Don't let me say mine.", "1:A white room. A man writing in a book with my name on it.",
                    "0:...Mine had a lamp that hummed. And the same man.", "1:Then it's no dream. Two people don't dream the same room." },
            new[] { "0:Ask me something about my mother. Anything.", "1:Where was she born?",
                    "0:I don't know. I've known all my life and I don't know. Now you.", "1:...I don't know either. Keep your voice down." }
        };

        /// The scene: one of the tests, in the hosts' kind (so it is treated as their strange talk everywhere else).
        public static Scene Make(string seed)
        {
            string[] t = Tests[Voice.Hash(seed + "|hn") % Tests.Length];
            var s = new Scene { Kind = "hosts", Summary = Summary };
            for (int i = 0; i < t.Length; i++)
                s.Add(t[i][0] - '0', t[i].Substring(2), null, i == 0 ? "face" : null, 500);
            return s;
        }

        /// The wake roll's lift for someone who compared notes on `day` (game days), today being `today`; 0 otherwise.
        public static int LiftFor(int day, long today)
        {
            return day > 0 && today - day >= 0 && today - day < Days ? Lift : 0;
        }

        public static string Key(string id) { return "hostnotes:" + id; }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] t in Tests)
                foreach (string l in t)
                    yield return l.Substring(2);
        }
    }
}
