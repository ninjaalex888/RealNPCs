// Composed acts (FIVEDAY2 items 8 and 9; [Mind] ComposedActs). Everything someone does is played one row at a time; this strings two or three of the game's own clips and moves into an
// order that can follow one another: a look, then a shrug, then walking off. Why it reads true: people's displays have a grammar. Attention first (a look, a turn), then what they feel
// (a gesture, a word, a whole-body clip), then what it comes to (they go, they kneel, they stand there). Out of order the same pieces read as a puppet. Only rows the game has been seen to
// play are used (Catalogue: Proven), by the feeling and urge the person has now. The rules pick an order without the model; with it (item 9) the model is given the moves as a short numbered
// menu in plain words, never clip names, answers with numbers in order, and the order is checked: a bad one falls back to the rules. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnrageWestworld.Core
{
    public static class Composed
    {
        public const int LeastSteps = 2, MostSteps = 3, MostMenu = 6, PersonGapMs = 90000, AtOnce = 3, Chance = 60, AskWaitMs = 2500;
        private const int Opens = 0, Shows = 1, Ends = 2;

        /// Whether Perform can play it, and there are words for it (the model is only ever offered what has words).
        public static bool Playable(Expression e)
        {
            return e != null && e.Proven && e.Words != null && (e.HasClip || e.Channel == "scenario" || e.Channel == "voice" || (e.Channel == "move" && Slot(e) >= 0));
        }

        /// Where in the sentence a row goes: a look or a turn opens, what's felt is shown, a kneel or a walk-away ends it. -1: none of them.
        private static int Slot(Expression e)
        {
            switch (e.Channel)
            {
                case "move":
                    return e.Name == Catalogue.TurnMove || e.Name == Catalogue.LookMove ? Opens : e.Name == Catalogue.StillMove || e.Name == Catalogue.WanderMove ? Ends : -1;
                case "scenario":
                    return Ends;
                default:
                    return Shows;
            }
        }

        /// Whether one row can come straight after another: looks open, nothing comes after an ending, and no two of one kind in a row (two words at once, two big movements).
        public static bool CanFollow(Expression a, Expression b)
        {
            if (a == null || b == null || a.Name == b.Name)
                return false;
            int sa = Slot(a), sb = Slot(b);
            if (sa < 0 || sb < 0 || sb == Opens || sa == Ends || sb < sa)
                return false;
            return a.Channel != b.Channel;
        }

        /// Whether a whole sequence holds together.
        public static bool Valid(IList<Expression> steps)
        {
            if (steps == null || steps.Count < LeastSteps || steps.Count > MostSteps || steps.Any(s => !Playable(s)))
                return false;
            for (int i = 1; i < steps.Count; i++)
                if (!CanFollow(steps[i - 1], steps[i]))
                    return false;
            return steps.Any(s => Slot(s) == Shows || s.Channel == "scenario");   // a look and a walk-away isn't a feeling shown
        }

        /// What this person could do with this feeling: a few of each part of the sentence. Empty when there's nothing to compose from.
        public static List<Expression> Menu(string feeling, string urge, string strength, bool woman, bool standing, string seed)
        {
            List<Expression> fits = Catalogue.For(feeling, urge, strength, woman, standing).Where(Playable).OrderBy(e => Voice.Hash(seed + "|" + e.Name)).ToList();
            var menu = new List<Expression>();
            foreach (int slot in new[] { Opens, Shows, Ends })
                menu.AddRange(fits.Where(e => Slot(e) == slot).Take(slot == Shows ? 3 : slot == Opens ? 1 : 2));   // one way of looking: sampled against the real model, two looks at the top of the list were the commonest wrong answer
            return menu.Count > MostMenu ? menu.Take(MostMenu).ToList() : menu;
        }

        /// The rules' sequence from a menu, or null if there's no sentence to be made of it: opens if it can, shows always, ends about half the time.
        public static List<Expression> Rules(IList<Expression> menu, string seed)
        {
            if (menu == null)
                return null;
            Expression show = menu.FirstOrDefault(e => Slot(e) == Shows);
            if (show == null)
                return null;
            var steps = new List<Expression>();
            Expression open = menu.FirstOrDefault(e => Slot(e) == Opens);
            if (open != null)
                steps.Add(open);
            steps.Add(show);
            Expression end = menu.FirstOrDefault(e => Slot(e) == Ends);
            if (end != null && (steps.Count < LeastSteps || Voice.Chance(seed + "|ends", 50)))
                steps.Add(end);
            if (steps.Count < LeastSteps)
            {
                Expression more = menu.FirstOrDefault(e => Slot(e) == Shows && e.Channel != show.Channel);
                if (more != null)
                    steps.Add(more);
            }
            return Valid(steps) ? steps : null;
        }

        // ---- the model (FIVEDAY2 item 9) --------------------------------------------------------------------------------------

        public static string Brief(int year)
        {
            return "You decide how one person living in " + (year > 0 ? year : 1899) + ", in the American West and South, shows what they feel. You are given who they are, how they feel and what is " +
                   "on their mind, and a short numbered list of small things their body can do. Choose two or three, in the order they would do them. A look or a turn comes first, if there is one. " +
                   "Anything that ends it (walking away, kneeling, standing stock still) comes last. Answer with the numbers alone, separated by commas, like this: 2, 5";
        }

        public static string Prompt(LanguageRequest r)
        {
            var b = new StringBuilder(Language.Describe(r));
            b.Append("What their body might do:\n");
            for (int i = 0; i < r.Options.Count; i++)
                b.Append(i + 1).Append(". ").Append(r.Options[i]).Append('\n');
            b.Append("In what order? The numbers alone.");
            return b.ToString();
        }

        /// The numbers on the menu the answer names, in order, 0-based; null when there aren't at least two different ones, or one comes twice.
        public static List<int> Parse(string answer, int count)
        {
            if (string.IsNullOrWhiteSpace(answer) || count <= 0)
                return null;
            string a = answer.Trim();
            int line = a.IndexOf('\n');
            if (line > 0)
                a = a.Substring(0, line);
            if (a.Length > 24)
                a = a.Substring(0, 24);
            var got = new List<int>();
            int n = -1;
            foreach (char c in a + " ")
            {
                if (char.IsDigit(c))
                    n = (n < 0 ? 0 : n * 10) + (c - '0');
                else if (n >= 0)
                {
                    if (n >= 1 && n <= count)
                    {
                        if (got.Contains(n - 1))
                            return null;
                        got.Add(n - 1);
                    }
                    n = -1;   // a number off the list is passed over: sampled against the real model, it was the commonest unreadable answer
                }
            }
            return got.Count >= LeastSteps ? got : null;
        }

        /// The model's order, as far as it can follow: moves that can't come next are passed over, up to three in all, and what's left must still hold together. Null, and the rules decide.
        public static List<Expression> FromAnswer(string answer, IList<Expression> menu)
        {
            List<int> idx = Parse(answer, menu.Count);
            if (idx == null)
                return null;
            var steps = new List<Expression>();
            foreach (int i in idx)
                if (steps.Count < MostSteps && (steps.Count == 0 || CanFollow(steps[steps.Count - 1], menu[i])))
                    steps.Add(menu[i]);
            return Valid(steps) ? steps : null;
        }
    }
}
