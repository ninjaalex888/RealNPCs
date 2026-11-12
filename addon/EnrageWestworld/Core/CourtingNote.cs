// Carry the note (LIVES item 4b, 2026-11-12; [Scenes] CourtingNote, settings version 478; the roadmap's "Carry the note" row).
// A courting worry with a real woman at the other end (Core/Links.cs: a courting link to a resident of his town) used to be
// mended only in Talk ("I'll put in a word for you."), and Talk is frozen. Now, a few steps from a man whose courting is getting
// worse or at its worst, he lets on he has a note in his pocket for her and no nerve to give it ("Wrote it four times. Can't
// hand it over. Not to her face."), and J lets you carry it. For two game days it's yours: near her (within a few steps), J
// gives it. She reads it there, and answers as his worry would end: yes (a warm tie between them, and his worry settled: it
// comes good), not yet ("Tell him he may walk me home from church. No further."), or no, kindly. Both remember you carried it.
//
// Why it reads true: courting in 1899 ran through go-betweens (a sister, a friend, a note slipped at church), because a refusal
// face to face cost a man his standing; the go-between takes the risk, and is thanked by both whichever way it goes. The answer
// is the worry's own (InnerLife's EndsWell for the run), so what the player did and what the man thinks agree.
// One note at a time, in memory only (a reload forgets it). Plain C#; the game side holds the note
// (WestworldScript.CourtingNote.cs). Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class CourtingNote
    {
        public const string Kind = "courtnote", GiveKind = "courtnote:give";
        public static bool On;   // [Scenes] CourtingNote, set by the game
        public const int PromptMs = 10000, LookMs = 10000, KeepDays = 2;
        public const float Metres = 5f;

        /// Her answer: "yes" when his worry ends well, else "notyet" (two in three) or "no", by the seed.
        public static string Answer(bool endsWell, string seed)
        {
            return endsWell ? "yes" : Voice.Chance(seed + "|ny", 66) ? "notyet" : "no";
        }

        public static string Asks(string her, string seed) { return Voice.Pick(seed + "|ca", AsksLines).Replace("{w}", her); }
        public static string Reads(string answer, string him, string seed)
        {
            string[] pool = answer == "yes" ? Yes : answer == "notyet" ? NotYet : No;
            return Voice.Pick(seed + "|cr", pool).Replace("{m}", him);
        }

        public static List<InteractOption> TakeRows(string her)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "courtnote:take", Label = "Carry his note to " + her + "." },
                new InteractOption { Id = "courtnote:no", Label = "Give it her yourself." }
            };
        }

        public static List<InteractOption> GiveRows(string him)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "courtnote:give", Label = "Give her " + him + "'s note." },
                new InteractOption { Id = "courtnote:keep", Label = "Not now." }
            };
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in AsksLines) yield return l.Replace("{w}", "Clara");
            foreach (string[] a in new[] { Yes, NotYet, No }) foreach (string l in a) yield return l.Replace("{m}", "Will");
        }

        private static readonly string[] AsksLines = {
            "Wrote {w} a note four times. Can't hand it over. Not to her face.",
            "I've a letter here for {w}. If somebody else gave it her, I'd not have to watch her read it.",
            "Every time I get near {w} the note stays in my pocket. You'd not carry it, would you?" };
        private static readonly string[] Yes = {
            "From {m}? Tell him yes. Tell him I wondered what was taking him so long.",
            "Oh. Oh, my. Tell {m} he may call on Sunday. And that he spells like a mule.",
            "Tell {m} yes, and that he might have asked me himself." };
        private static readonly string[] NotYet = {
            "Tell {m} he may walk me home from church. No further. Not yet.",
            "It's a sweet note. Tell him I'll think on it.",
            "Tell {m} I read it twice. He'll know what that means, or he won't." };
        private static readonly string[] No = {
            "Tell {m} it's kind of him. Tell him gently. I'm promised elsewhere.",
            "I can't. Tell him I'm sorry, and that it's no fault of his.",
            "Give it back to him. No. Wait. Tell him I'm flattered, and that's all." };
    }
}
