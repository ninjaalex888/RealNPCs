// The doctor's call (LIVES item 4a, 2026-11-12; [Scenes] DoctorsCall, settings version 477; the roadmap's "Fetch the doctor"
// row). A fever in a house of the town is a worry with an ending (Core/InnerLife.Worries.cs: the doctor comes by noon and the
// fever breaks, or it doesn't), and Talk could already put two dollars in the father's hand for the doctor; nothing on J did,
// and Talk is frozen. Now at the doctor's (Valentine's or Rhodes'), within a few steps of him, he mentions a child of the
// town bad with fever whose people can't pay his call ("Ruth, of the Hales, is bad with fever. They can't pay the ride out,
// and I can't ride for nothing."), and J lets you pay his two dollars: he rides out, and the worry is settled as the Talk mend
// settles it (the house's thoughts say a stranger paid the doctor, and the fever breaks); a kindness the doctor saw.
//
// Why it reads true: country doctors in 1899 charged by the call and the mile, and a call refused for want of money was
// common enough to be a bitter joke; paying a stranger's doctor is a debt no one in a town of a few hundred forgets. The help is
// unseen by the one helped (the father hears it from the doctor), which is why it lands harder when he thanks you later:
// unasked-for kindness from a stranger draws the strongest gratitude (Tesser, Gatewood and Driver, 1968).
// Plain C#; the game side finds the doctor and the house (WestworldScript.DoctorsCall.cs). Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class DoctorsCall
    {
        public const string Kind = "doctorscall";
        public static bool On;   // [Scenes] DoctorsCall, set by the game
        public const int Cents = 200, PromptMs = 12000, LookMs = 15000;
        public const float DoctorMetres = 6f;

        /// A fever in this house, not yet at its end and not settled by the player: the doctor's call would mend it.
        public static bool Fits(Person p, long gameDay)
        {
            string kind; int stage; bool good;
            return On && p != null && InnerLife.WorryNow(p, gameDay, out kind, out stage, out good) && kind == "fever" && stage >= 1 && stage <= 2 && !InnerLife.IsSettled(p.Id, gameDay);
        }

        /// The sick one's first name, from the worry's own words ("Ruth at home, sick with fever"), or null.
        public static string Sick(Person p, long gameDay)
        {
            string w = InnerLife.WorryWords(p, gameDay);
            if (w == null)
                return null;
            int at = w.IndexOf(" at home", System.StringComparison.Ordinal);
            return at > 0 ? w.Substring(0, at) : null;
        }

        /// The doctor's word: the sick one and their house (the family's name: "the Hales").
        public static string Mention(string sick, string family, string seed)
        {
            return Voice.Pick(seed + "|dm", Mentions).Replace("{s}", sick ?? "the child").Replace("{f}", family ?? "a family in town");
        }

        public static List<InteractOption> Rows(int cashCents)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "doctorscall:pay", Label = "Pay for his call ($2).", Why = cashCents < Cents ? "you haven't the money" : null },
                new InteractOption { Id = "doctorscall:no", Label = "It's none of my affair." }
            };
        }

        public static string Paid(string seed) { return Voice.Pick(seed + "|dp", PaidLines); }
        public static string Thought(string seed) { return Voice.Pick(seed + "|dt", Thoughts); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Mentions) yield return l.Replace("{s}", "Ruth").Replace("{f}", "the Hales");
            foreach (string[] a in new[] { PaidLines, Thoughts }) foreach (string l in a) yield return l;
        }

        private static readonly string[] Mentions = {
            "{s}, of {f}, is bad with fever. They can't pay the ride out, and I can't ride for nothing.",
            "There's a fever with {f}. The child, {s}. I've been asked twice and paid never.",
            "{f} sent word about {s}. Two dollars for the call, and they haven't two dollars." };
        private static readonly string[] PaidLines = {
            "Then I'll ride out directly. They'll want to know who to thank.",
            "Two dollars. I'll get my bag. You've a good heart, stranger.",
            "I'll go now. Fevers don't wait on money, but I've had to." };
        private static readonly string[] Thoughts = {
            "A stranger paid my call. Haven't seen that in twenty years of doctoring.",
            "Paid for a child he's never met. Well. I'd best not waste it." };
    }
}
