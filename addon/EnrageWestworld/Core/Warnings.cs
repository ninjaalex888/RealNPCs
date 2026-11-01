// Witnessed, or warned (NIGHT4 item 5; Lex: "more NPC interactions with the main character"): someone near Arthur in
// town acts on what they know of him, never on their own needs. Three facts, each real: they saw his violence today and
// aren't afraid of him (w1: answer him to his face in words, a hard stare, or let it be); someone near holds a grudge
// against him and the one who knows it thinks well of him (w2: warn him, a look toward the man and a short word, or let
// it be); a deed of his in this town is known, the law is near, and the one who knows thinks well of him (w3: the same).
// Words, looks and turning away only: nobody lays a hand on him. The tables and the words here; the walk is the game
// side's (WestworldScript.Warnings.cs). Plain C#, no game; all words original.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Warnings
    {
        public const string Saw = "saw", Grudge = "grudge", Law = "law";
        public const float Cowed = 0.3f;   // fear of him at least this (the mood's fading) and nobody has it out with him
        public const float GrudgeAnger = 0.45f;   // w2: anger at him still this strong (the mood's fading), not a bump's lingering

        /// Whether they'd warn him at all (w2, w3): never someone afraid of him or against him; someone fond of him, grateful
        /// to him, or a loyal soul who has met him.
        public static bool MayWarn(string regard, bool grateful, bool met, string temper)
        {
            if (regard == "afraid" || regard == "hostile")
                return false;
            return regard == "fond" || regard == "smitten" || grateful || (met && temper == "Loyal");
        }

        /// The mind's table for the fact, in a fixed order. w1: haveitout (only the hot and the proud, not cowed), stare,
        /// letbe. w2/w3: warn, hint, letbe. Every temper can let it be and do one thing more. `knowsDanger` false (w2, no
        /// tie between the warner and the grudge-holder): no warning by name, only the hint.
        public static Staged.Weights Weights(string fact, StagedMind m, bool cowed, bool knowsDanger = true)
        {
            string t = m != null ? m.Temper : null;
            bool sore = m != null && m.Mood == "sore";
            var w = new Staged.Weights();
            if (fact == Saw)
            {
                int out_ = (t == "Hothead" || t == "Proud") && !cowed ? 4 + (sore ? 1 : 0) + (m != null && m.Drunk ? 1 : 0) : 0;
                int stare = 3 + (t == "Cold" ? 2 : 0) + (sore ? 1 : 0);
                int let = 2 + (t == "Coward" ? 3 : 0) + (cowed ? 2 : 0) + (t == "Greedy" || t == "Drifter" ? 1 : 0);
                w.Add("haveitout", out_).Add("stare", stare).Add("letbe", let);
            }
            else
            {
                bool fond = m != null && m.ToYou > 0;
                int warn = 3 + (t == "Loyal" ? 2 : 0) + (fond ? 2 : 0) - (t == "Coward" ? 2 : 0);
                int hint = 2 + (t == "Coward" ? 2 : 0) + (t == "Cold" || t == "Proud" ? 1 : 0);
                int let = 1 + (t == "Cold" ? 2 : 0) + (t == "Greedy" ? 2 : 0);
                w.Add("warn", fact == Grudge && !knowsDanger ? 0 : warn < 1 ? 1 : warn).Add("hint", hint).Add("letbe", let);
            }
            return w.Clamp();
        }

        /// The mind's options over every fact (the tests): w1's, then w2's and w3's.
        public static readonly string[] Ids = { "haveitout", "stare", "letbe", "warn", "hint" };

        /// What goes through their head as their own mind settles it, as they notice him (the no-model run, item 1;
        /// ActThoughts.TurnPool): the reason for the choice, beside the choice's words; a stare's or a letting be's stands in for
        /// Thought's at the same moment, a warning's comes before the walk (Thought's after it).
        internal static readonly Dictionary<string, string[]> Turns = new Dictionary<string, string[]>
        {
            { "warn|haveitout", new[] { "What he did, and walking about like nothing. Not past me.", "Saw what he did. Not keeping quiet about it.", "He'll hear what decent folk think of it." } },
            { "warn|stare", new[] { "Let him see I saw.", "Hold his eye. Make him feel it.", "No words. He'll know what I mean." } },
            { "warn|letbe", new[] { "Not my quarrel. Let it pass.", "Keep out of it. Better that way.", "Least said, soonest mended." } },
            { "warn|warn", new[] { "He ought to know what's coming. I'd want telling.", "Tell him quiet, before it's too late.", "He stood by me once. Not letting him walk into it blind." } },
            { "warn|hint", new[] { "A look'll do. No need to be seen talking.", "Catch his eye. He's sharp enough.", "Point him to it, careful. Not my neck." } },
        };

        /// The option as the model is offered it.
        public static string Words(string id, string fact, string danger)
        {
            switch (id)
            {
                case "haveitout": return "walk up and tell him to his face what you think of what he did";
                case "stare": return "stare him down from where you stand, and say nothing";
                case "warn": return fact == Law ? "walk over and tell him quietly the law is asking after him" : "walk over and tell him quietly " + (danger ?? "someone near") + " means him harm";
                case "hint": return fact == Law ? "catch his eye and look toward the lawman, with a word" : "catch his eye and look toward " + (danger ?? "the one glaring at him") + ", with a word";
                default: return "let it be; it's none of your business";
            }
        }

        /// The "Right now:" line for the model, the fact in the mind's own words.
        public static string Now(string fact, string you, string danger, string town)
        {
            string him = you != null ? "Mr " + you : "the stranger Arthur";
            switch (fact)
            {
                case Saw: return "You saw " + him + " do violence in " + (town ?? "town") + " today; he has stopped near you.";
                case Law: return "The law in " + (town ?? "town") + " is asking about something " + him + " did; a lawman is close, and " + him + " has stopped near you.";
                default: return (danger != null ? "You know " + danger : "You've seen someone near here who") + " means " + him + " harm; " + him + " has stopped near you.";
            }
        }

        /// What they say to his face. `danger`: the grudge-holder's name (w2); `place`: where the deed was (w3); `you`:
        /// his surname when they know it.
        public static string Line(string pick, string fact, string danger, string place, string you, string seed)
        {
            string m = you != null ? "Mr. " + you : "mister";
            string M = you != null ? "Mr. " + you : "Mister";
            switch (pick)
            {
                case "haveitout":
                    return Pick(seed, "That, back there, " + m + ". There was no call for it.",
                                M + ". I saw it. Don't think the whole street didn't.",
                                "In the open street, " + m + ". I won't pretend I didn't see it.");
                case "warn":
                    if (fact == Law)
                        return Pick(seed, "A word, " + m + ". The deputy's asking about that business" + (place != null ? " " + place : "") + ". I'd keep moving.",
                                    "The law's been asking after the man who did that" + (place != null ? " " + place : "") + ", " + m + ". Figured you'd want to hear it.");
                    return Pick(seed, (danger ?? "Somebody") + " was asking where you'd got to, " + m + ". I'd not stand about.",
                                "Watch yourself, " + m + ". " + (danger ?? "Somebody near here") + " has it in for you, and isn't far off.");
                case "hint":
                    return fact == Law ? Pick(seed, "Law's about, " + m + ". That's all.", "I'd mind that badge, " + m + ".")
                                       : Pick(seed, "Mind who's at your back, " + m + ".", "Wouldn't linger here, " + m + ". That's all I'll say.");
                default:
                    return null;
            }
        }

        /// What they think: after a stare, after letting it be, after having said it.
        public static string Thought(string pick, string fact, string seed)
        {
            switch (pick)
            {
                case "stare": return Pick(seed + "|s", "I saw what he did. I'll know his face again.", "Look all you like, mister. I saw it.", "Somebody has to look a man like that in the eye.");
                case "haveitout": return Pick(seed + "|o", "Said my piece. Let him do what he likes with it.", "Somebody had to tell him.", "My hands are shaking. Still. I said it.");
                case "warn": case "hint": return Pick(seed + "|w", "There. He's been told.", "Least I could do for him.", "Hope he heeds it.");
                default: return fact == Saw ? Pick(seed + "|l", "Not my business. Not my business.", "Keep your head down and your mouth shut.")
                                            : Pick(seed + "|k", "Let him find out for himself.", "It's no affair of mine.");
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed + "|wn") % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string fact in new[] { Saw, Grudge, Law })
                foreach (string pick in new[] { "haveitout", "warn", "hint", "stare", "letbe" })
                    foreach (string you in new[] { null, "Morgan" })
                        for (int i = 0; i < 6; i++)
                        {
                            string l = Line(pick, fact, i % 2 == 0 ? "Hiram Cobb" : null, i % 3 == 0 ? "by the saloon" : null, you, "all" + i);
                            if (l != null) yield return l;
                            yield return Thought(pick, fact, "all" + i);
                        }
        }
    }
}
