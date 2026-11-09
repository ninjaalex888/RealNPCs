// Two who think about each other (TENHOUR item 14; [Ticker] PairThoughts; built on the ties the street's scenes leave and the side boxes over heads). Two people in view who have some history: a box each, at the same moment, each about the other.
// Why it reads true: what two people privately make of each other rarely matches. Each is sure of what the other thinks and is usually wrong, and the player is the one person in the street who gets to see both sides of it at once. All lines original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class PairThoughts
    {
        public const int GapMs = 45000, LookMs = 5000, ForMs = 7000, Chance = 70;
        public const float Metres = 9f;

        // by the temper group: bold, fearful, cold, plain
        private static readonly string[][] Warm =
        {
            new[] { "{who}'s all right. Not that I'd say so.", "I'd stand up for {who}. Wouldn't tell {who} that.", "Not many I'd drink with. {who}, yes." },
            new[] { "Thank the Lord for {who}.", "{who} always knows what to say. I never do.", "If {who} wasn't about I don't know who I'd talk to." },
            new[] { "{who} will be useful one of these days.", "Fond of {who}, in a way. In a way.", "{who} trusts me. I've done nothing to spoil it yet." },
            new[] { "Good old {who}.", "{who} looks tired. I should ask.", "I've known {who} too long to be polite about it." },
        };
        private static readonly string[][] DidIt =   // this one wronged the other
        {
            new[] { "Go on, {who}. Say something.", "Still looking at me, {who}?", "I'd do it again, {who}." },
            new[] { "Don't look up. {who} hasn't forgotten.", "I never should have. I know that, {who}.", "If {who} comes over I'll say I'm sorry. I will." },
            new[] { "{who} knows better now.", "No harm done that {who} can prove.", "Let {who} stew." },
            new[] { "I ought to say something to {who}. I won't.", "Not my finest hour, that, with {who}.", "Wish {who} would stop looking so tired." },
        };
        private static readonly string[][] Wronged =   // the other wronged this one
        {
            new[] { "One of these days, {who}.", "I've not forgotten, {who}.", "Look me in the eye, {who}. Just once." },
            new[] { "Keep walking. {who} hasn't seen me.", "Why does {who} have to be here.", "I'd cross the street from {who}, if it was a wider street." },
            new[] { "I can wait, {who}. I'm good at it.", "Everybody owes somebody. {who} owes me.", "I know where {who} sleeps." },
            new[] { "{who} never once said sorry.", "Same as ever, {who}. Same as ever.", "I'd have let it go, if {who} had only asked." },
        };
        private static readonly string[][] Cool =
        {
            new[] { "{who} again.", "What's {who} looking at?", "I could take {who}, if it came to it." },
            new[] { "Does {who} know me? I think {who} knows me.", "I never know what to say to {who}.", "Don't let {who} come over." },
            new[] { "What does {who} do all day?", "I wonder what {who} wants.", "{who} has a way of being around." },
            new[] { "I never did know what to make of {who}.", "{who}'s got thin.", "Must ask {who} about that, some time." },
        };
        private static readonly string[] Sweethearts = { "Don't look. Don't look. {who} looked.", "Does {who} think about it? I can't tell.", "Lord, not here, {who}. Not with people looking." };

        private static int Group(string temper)
        {
            switch (Minds.TemperGroup(temper))
            {
                case "bold": return 0;
                case "fearful": return 1;
                case "cold": return 2;
                default: return 3;
            }
        }

        /// What this one thinks of the other, from the tie between them and their own temper.
        public static string Line(SceneTie t, string id, string temper, string seed)
        {
            if (t == null || id == null || !t.Has(id))
                return null;
            string other = Person.FirstOf(t.Other(id));
            if (string.IsNullOrEmpty(other))
                return null;
            string[] pool;
            if (t.Courted && t.Warmth >= 0)
                pool = Sweethearts;
            else if (t.Warmth < 0 && t.Doer != null)
                pool = (t.Did(id) ? DidIt : Wronged)[Group(temper)];
            else if (t.Warmth < 0)
                pool = Wronged[Group(temper)];
            else if (t.Warmth > 0)
                pool = Warm[Group(temper)];
            else
                pool = Cool[Group(temper)];
            return Voice.Pick(seed + "|pair", pool).Replace("{who}", other);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] g in new[] { Warm, DidIt, Wronged, Cool })
                foreach (string[] p in g)
                    foreach (string l in p)
                        yield return l.Replace("{who}", "Silas");
            foreach (string l in Sweethearts)
                yield return l.Replace("{who}", "Silas");
        }
    }
}
