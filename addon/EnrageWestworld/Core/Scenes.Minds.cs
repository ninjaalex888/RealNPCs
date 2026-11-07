// The chance meeting chosen by the two people's minds (NIGHT4 item 5's plan E, built after item 6; [Mind] MindChanceMeetings,
// with MindLive). The street's roll (Choose) weighs only the place, the pair's sexes and age; here the same kinds that roll
// would allow are leaned by who the two are: what lies between them (friends pass the time and sing, foes come to words,
// square up or turn their backs), their day (the sore snap, the glad talk and, in a saloon, sing), their worry (money
// sends them to a trade or to asking the other for a loan, a feud to an argument), drink (tall tales and songs) and their
// nature (the hothead, the proud, the coward, the sociable, the pious). A lean only, on what the place already allows: a
// kind the roll would never offer here stays at 0, so the two new kinds (a cold shoulder between people already on bad
// terms, a loan asked by one with a money worry) are the only additions. The mind's own choice is a draw on these weights;
// with [Mind] LanguagePicks the model may pick among its three heaviest (the game side, StagedChoose). Plain C#, no game;
// every line original.
using System;
using System.Collections.Generic;


namespace EnrageWestworld.Core
{
    /// One of the two, as the chance meeting's lean reads them.
    public sealed class ChanceMind
    {
        public string Name;          // first name, for the log
        public string Temper;        // Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter, or null
        public string Mood;          // "sore", "glad" or null (Passing.DayMood)
        public string Worry;         // the worry they're living through today, while it lasts (stage 0-2), else null
        public int WorryStage;       // 0 begun, 1 worse, 2 at its worst
        public bool Drunk;
        public float Sociable, Pious;   // trait z-scores (Traits.Sociability, Traits.Piety; 0 the town's middle)
    }

    /// The minds' weights for a chance meeting, whose mind leads the kinds that have a side, and why, for the log.
    public sealed class ChanceLean
    {
        public Staged.Weights W;
        public int LeadFavour, LeadBrushoff;   // who asks for the loan, who turns away: 0 A, 1 B
        public string FavourWorry;             // the asker's worry (debt, bank, savings, cards)
        public string Why;

        public int LeadOf(string kind)
        {
            return kind == "favour" ? LeadFavour : kind == "brushoff" ? LeadBrushoff : 0;
        }
    }

    public static partial class Scenes
    {
        /// A lean moves a kind's weight by this many percent at most each way: never below a tenth, never above three times.
        public const int MindLeanLeast = -90, MindLeanMost = 200;
        /// No weight above this (a brawl at TroubleWeight 5 is 100 before the lean).
        public const int MindWeightMost = 300;
        /// A trait this far from the middle is the person's nature (Traits.WordsAt: about one in six each way).
        public const float MindTraitAt = 1.0f;

        /// The kinds the roll offers this cast, in the roll's order, with the ini's switched-off kinds taken out.
        public static List<KeyValuePair<string, int>> ChanceWeights(SceneCast c, bool allowFight)
        {
            List<KeyValuePair<string, int>> w = new List<KeyValuePair<string, int>>();
            bool mixed = c.AWoman != c.BWoman;
            bool twoMen = !c.AWoman && !c.BWoman;
            if (mixed)
                w.Add(new KeyValuePair<string, int>("flirt", c.Saloon ? 14 : 18));
            w.Add(new KeyValuePair<string, int>("argue", Weighted(16, ArgueWeight(c))));
            if (twoMen && allowFight)
                w.Add(new KeyValuePair<string, int>("brawl", Weighted(c.Saloon ? 20 : 10, c.Trouble)));   // twice the old 10 and 5 (Lex, 2026-10-28: brawls often)
            else if (c.WomenFight && allowFight)
                w.Add(new KeyValuePair<string, int>("brawl", Weighted((c.Saloon ? 20 : 10) * (c.AWoman ? Reasons.WomanFightPercent : 100) * (c.BWoman ? Reasons.WomanFightPercent : 100) / 10000, c.Trouble)));   // a woman in it: half the share for each (Lex, 2026-10-29)
            if (mixed && !c.Night)
                w.Add(new KeyValuePair<string, int>("propose", 3));
            if (c.Saloon && twoMen)
                w.Add(new KeyValuePair<string, int>("cheat", 10));
            w.Add(new KeyValuePair<string, int>("debt", 7));
            if (mixed && !c.Saloon && !c.Night && c.Town != null)
                w.Add(new KeyValuePair<string, int>("temperance", 4));   // (in a town: "those doors" are a saloon's; the Part 3 review)   // a temperance woman puts the pledge to a man (Core/Temperance.cs; FIVEDAY item 12)
            if (c.Payday && mixed && !c.Saloon)
                w.Add(new KeyValuePair<string, int>("wages", 14));
            if (c.Rumours.Count > 0)
                w.Add(new KeyValuePair<string, int>("gossip", c.Saloon ? 30 : 18));
            w.Add(new KeyValuePair<string, int>("hosts", c.HostTier >= 2 ? 16 : c.HostTier == 1 ? 9 : c.Night ? 6 : 3));
            FrontierWeights(c, w, twoMen, allowFight);
            w.RemoveAll(x => c.Off.Contains(x.Key));
            return w;
        }

        private static bool MoneyWorry(string worry)
        {
            return worry == "debt" || worry == "bank" || worry == "savings" || worry == "cards";
        }

        private static void Lean(Dictionary<string, int> d, string kind, int pct)
        {
            int v;
            d.TryGetValue(kind, out v);
            d[kind] = v + pct;
        }

        /// One mind's leanings, as percent moves on each kind.
        private static void LeanOf(ChanceMind m, SceneCast c, Dictionary<string, int> d, List<string> why)
        {
            if (m == null)
                return;
            var mine = new List<string>();
            if (m.Mood == "sore")
            {
                Lean(d, "argue", 60); Lean(d, "standoff", 40); Lean(d, "brawl", 30); Lean(d, "cheat", 20);
                Lean(d, "smalltalk", -40); Lean(d, "song", -40); Lean(d, "flirt", -40); Lean(d, "propose", -50); Lean(d, "gossip", -20); Lean(d, "talltale", -20);
                mine.Add("sore");
            }
            else if (m.Mood == "glad")
            {
                Lean(d, "smalltalk", 50); Lean(d, "song", c.Saloon ? 90 : 50); Lean(d, "gossip", 30); Lean(d, "flirt", 30); Lean(d, "talltale", 20);
                Lean(d, "argue", -40); Lean(d, "brawl", -40); Lean(d, "standoff", -40); Lean(d, "cheat", -20);
                mine.Add("glad");
            }
            if (m.Drunk)
            {
                Lean(d, "talltale", 60); Lean(d, "song", 80); Lean(d, "brawl", 30); Lean(d, "cheat", 20); Lean(d, "paper", -50); Lean(d, "haggle", -40);
                mine.Add("in drink");
            }
            if (m.Worry != null)
            {
                int press = m.WorryStage < 0 ? 0 : m.WorryStage > 2 ? 2 : m.WorryStage;
                if (MoneyWorry(m.Worry))
                {
                    Lean(d, "haggle", 60 + 30 * press);
                    if (m.Worry == "cards") Lean(d, "cheat", 40);
                    mine.Add(m.Worry + " worry");
                }
                else if (m.Worry == "feud")
                {
                    Lean(d, "argue", 60 + 20 * press); Lean(d, "standoff", 30); Lean(d, "smalltalk", -20);
                    mine.Add("a feud");
                }
                else if (m.Worry == "drink")
                {
                    Lean(d, "song", 30); Lean(d, "talltale", 20);
                    Lean(d, "temperance", 60);   // the drinker she'd stop (the scene makes the man the drinker) (FIVEDAY item 12; the Part 3 review: it sat in the money branch and never fired)
                    mine.Add("the drink");
                }
                else if (m.Worry == "courting")
                {
                    Lean(d, "flirt", 40); Lean(d, "propose", 30);
                    mine.Add("courting");
                }
                else if (m.Worry == "fever" || m.Worry == "son" || m.Worry == "letter")
                {
                    Lean(d, "smalltalk", 30); Lean(d, "song", -30); Lean(d, "talltale", -20);
                    mine.Add("a " + m.Worry + " worry");
                }
            }
            switch (m.Temper)
            {
                case "Hothead": Lean(d, "argue", 50); Lean(d, "brawl", 50); Lean(d, "standoff", 50); Lean(d, "smalltalk", -20); break;
                case "Proud": Lean(d, "standoff", 40); Lean(d, "argue", 30); Lean(d, "haggle", 20); break;
                case "Coward": Lean(d, "brawl", -60); Lean(d, "standoff", -60); Lean(d, "argue", -30); Lean(d, "smalltalk", 20); break;
                case "Greedy": Lean(d, "haggle", 50); Lean(d, "cheat", 40); Lean(d, "pickpocket", 30); Lean(d, "pitch", 30); break;
                case "Loyal": Lean(d, "smalltalk", 20); Lean(d, "gossip", 20); break;
                case "Cold": Lean(d, "smalltalk", -30); Lean(d, "gossip", -30); Lean(d, "flirt", -30); break;
                case "Drifter": Lean(d, "talltale", 30); Lean(d, "nighttale", 30); Lean(d, "song", 20); break;
            }
            if (m.Temper != null)
                mine.Add(m.Temper.ToLowerInvariant());
            if (m.Sociable >= MindTraitAt)
            {
                Lean(d, "smalltalk", 40); Lean(d, "gossip", 40); Lean(d, "song", 30); Lean(d, "talltale", 20);
                mine.Add("sociable");
            }
            else if (m.Sociable <= -MindTraitAt)
            {
                Lean(d, "smalltalk", -30); Lean(d, "gossip", -30); Lean(d, "song", -30);
                mine.Add("solitary");
            }
            if (m.Pious >= MindTraitAt)
            {
                Lean(d, "cheat", -60); Lean(d, "pickpocket", -60); Lean(d, "brawl", -40); Lean(d, "flirt", -30); Lean(d, "paper", 30); Lean(d, "smalltalk", 20); Lean(d, "song", 20);
                Lean(d, "temperance", 80);   // the pious take up the cause
                mine.Add("pious");
            }
            why.Add((m.Name ?? "one") + (mine.Count > 0 ? " " + string.Join(", ", mine) : " even"));
        }

        /// The two minds' weights on what the roll would offer this cast (`baseW`, ChanceWeights), plus a cold shoulder between
        /// two already on bad terms and a loan asked by one with a money worry of someone not against them. Null when neither
        /// mind is known (the roll then, as before).
        public static ChanceLean MindLean(List<KeyValuePair<string, int>> baseW, SceneCast c, ChanceMind a, ChanceMind b)
        {
            if (baseW == null || (a == null && b == null))
                return null;
            var d = new Dictionary<string, int>();
            var why = new List<string>();
            LeanOf(a, c, d, why);
            LeanOf(b, c, d, why);
            bool tied;
            int warmth = c.WarmthBetween(0, 1, out tied);
            if (tied && warmth >= 1)
            {
                int k = warmth >= 2 ? 2 : 1;
                Lean(d, "smalltalk", 30 * k); Lean(d, "gossip", 30 * k); Lean(d, "song", 20 * k); Lean(d, "talltale", 20 * k);
                Lean(d, "argue", -30 * k); Lean(d, "brawl", -30 * k); Lean(d, "standoff", -35 * k); Lean(d, "cheat", -25 * k); Lean(d, "pickpocket", -40 * k); Lean(d, "haggle", -10 * k);
                why.Add(warmth >= 2 ? "friends" : "on good terms");
            }
            else if (tied && warmth <= -1)
            {
                int k = warmth <= -2 ? 2 : 1;
                Lean(d, "argue", 40 * k); Lean(d, "brawl", 25 * k); Lean(d, "standoff", 30 * k);
                Lean(d, "smalltalk", -35 * k); Lean(d, "song", -30 * k); Lean(d, "gossip", -25 * k); Lean(d, "flirt", -35 * k); Lean(d, "propose", -45 * k);
                why.Add(warmth <= -2 ? "foes" : "on bad terms");
            }
            var lean = new ChanceLean { W = new Staged.Weights() };
            foreach (KeyValuePair<string, int> x in baseW)
            {
                if (x.Value <= 0)
                {
                    lean.W.Add(x.Key, 0);
                    continue;
                }
                int pct;
                d.TryGetValue(x.Key, out pct);
                pct = Math.Max(MindLeanLeast, Math.Min(MindLeanMost, pct));
                int v = (int)Math.Round(x.Value * (100 + pct) / 100.0);
                lean.W.Add(x.Key, Math.Max(1, Math.Min(MindWeightMost, v)));
            }
            // a cold shoulder: only between two with a tie already gone bad; the sorer or colder one turns away
            if (tied && warmth < 0 && !c.Off.Contains("brushoff"))
            {
                int sa = Cold(a), sb = Cold(b);
                lean.LeadBrushoff = sb >= sa ? 1 : 0;
                lean.W.Add("brushoff", Math.Min(MindWeightMost, 6 * -warmth + 3 * Math.Max(sa, sb)));
            }
            // a loan asked: one with a money worry, of someone not against them
            if (!(tied && warmth < 0) && !c.Off.Contains("favour"))
            {
                int pa = Pressed(a), pb = Pressed(b);
                if (pa > 0 || pb > 0)
                {
                    lean.LeadFavour = pb > pa ? 1 : 0;
                    ChanceMind asker = lean.LeadFavour == 0 ? a : b;
                    lean.FavourWorry = asker.Worry;
                    lean.W.Add("favour", Math.Min(MindWeightMost, 4 * Math.Max(pa, pb) + (tied && warmth >= 1 ? 4 : 0) + (asker.Temper == "Proud" ? -3 : 0)));
                }
            }
            lean.Why = string.Join("; ", why);
            return lean;
        }

        // how ready to turn away: sore 2, cold or proud 1 each
        private static int Cold(ChanceMind m)
        {
            return m == null ? 0 : (m.Mood == "sore" ? 2 : 0) + (m.Temper == "Cold" || m.Temper == "Proud" ? 1 : 0) + (m.Temper == "Hothead" ? 1 : 0);
        }

        // how hard money presses: 1-3 by the worry's stage, 0 with no money worry
        private static int Pressed(ChanceMind m)
        {
            return m != null && MoneyWorry(m.Worry) && m.WorryStage >= 0 && m.WorryStage <= 2 ? m.WorryStage + 1 : 0;
        }

        /// The kind as the model is offered it, from A's own point of view (`lead`: whose side a cold shoulder or a loan is on).
        public static string MindWords(string kind, string other, int lead)
        {
            string o = other ?? "them";
            switch (kind)
            {
                case "flirt": return "flirt a little with " + o;
                case "argue": return "have sharp words with " + o;
                case "brawl": return "come to blows with " + o;
                case "propose": return "ask " + o + " to marry you";
                case "cheat": return "call " + o + " out for cheating at cards";
                case "temperance": return "ask " + o + " to sign the pledge";
                case "wages": return "ask " + o + " for the week's pay";
                case "gossip": return "trade the town's news with " + o;
                case "hosts": return "wonder aloud with " + o + " about this town";
                case "smalltalk": return "pass the time of day with " + o;
                case "standoff": return "square up to " + o + " in the street";
                case "song": return "strike up a song with " + o;
                case "talltale": return "spin " + o + " a tall tale";
                case "nighttale": return "tell " + o + " one of the old tales";
                case "paper": return "read " + o + " something out of the paper";
                case "haggle": return "try to sell " + o + " something";
                case "pitch": return "sell " + o + " a bottle of tonic";
                case "pickpocket": return "get a hand into " + o + "'s coat while you talk";
                case "brushoff": return lead == 0 ? "turn your back on " + o : "say good day and have " + o + " turn their back on you";
                case "favour": return lead == 0 ? "ask " + o + " for a loan" : "hear " + o + " out over a loan";
                default: return kind + " with " + o;
            }
        }

        /// The model's "Right now:" line for the chance meeting.
        public static string MindNow(string other, bool saloon, string town)
        {
            return "You have come face to face with " + (other ?? "someone you know by sight") + (saloon ? " in the saloon" : town != null ? " in " + town : " in the street") + ".";
        }

        /// What the two minds leaned the meeting by, for the log: "Clem sore, hothead; Ada glad; foes".
        public static string MindWhy(ChanceLean l)
        {
            return l == null || string.IsNullOrEmpty(l.Why) ? "even" : l.Why;
        }

        // ---- the cold shoulder ------------------------------------------------------------------------

        /// One greets the other, who turns their back (a tie already gone bad; `lead` turns away). Kind "brushoff", so the tie,
        /// the appraisal and the gossip read it as they already do.
        public static Scene Brushoff(SceneCast c, string seed, int lead)
        {
            int l = lead == 1 ? 1 : 0, o = 1 - l;
            string nl = N(c, l, seed);
            Scene s = new Scene { Kind = "brushoff", Doer = l, Summary = "one turned a cold shoulder on the other" };
            string hello = c.Night ? "Evening" : "Afternoon";
            s.Add(o, Voice.Pick(seed + "bo", hello + ", " + nl + ".", "Well. " + Grammar.UpperFirst(nl) + ".", "Thought I might run into you, " + nl + "."), "greet", "face");
            s.Add(l, Voice.Pick(seed + "bl", "We've said all we'll say, you and me.", "Not today.", "Can't say I'd hoped to see you.", "Keep walking."), "no", "walkaway");
            s.Add(o, Voice.Pick(seed + "bt", "Suit yourself, then.", "Same as ever, I see.", "Fine. Be that way."), "mild");
            s.Beats[1].To = o;
            s.Beats[0].To = l;
            s.Beats[2].To = l;
            return s;
        }

        // ---- a loan asked -----------------------------------------------------------------------------

        /// One with a money worry asks the other to tide them over; the other's nature and what lies between them answer. Kind
        /// "charity" when given (the tie warms), "scorn" when refused (it cools). The tie's doer is the asker, as with Beg and
        /// Ties.Trouble: the appraisal turns it round to the answer, and Grudges.Wrongdoer to the one who refused.
        public static Scene AskFavour(SceneCast c, string seed, int lead, string worry)
        {
            int k = lead == 1 ? 1 : 0, g = 1 - k;
            string ng = N(c, g, seed);
            bool tied;
            int warmth = c.WarmthBetween(0, 1, out tied);
            string temper = c.Tempers != null && g < c.Tempers.Length ? c.Tempers[g] : null;
            int yes = 45 + (warmth >= 1 ? 25 : 0) + (temper == "Loyal" ? 25 : 0) - (temper == "Cold" || temper == "Greedy" ? 25 : 0);
            bool given = Voice.Chance(seed + "fy", Math.Max(5, Math.Min(95, yes)));
            Scene s = new Scene { Kind = given ? "charity" : "scorn", Doer = k, Summary = given ? "one tided the other over with a loan" : "one asked the other for a loan and was turned down" };
            string ask;
            switch (worry)
            {
                case "bank":
                    ask = Voice.Pick(seed + "fa", "The bank's note comes due Friday, " + ng + ", and I'm short. Could you see me through to the month's end?",
                                                  "I'd not ask, " + ng + ", only the bank won't wait. A few dollars would hold them off.");
                    break;
                case "savings":
                    ask = Voice.Pick(seed + "fa", "I'm a few dollars short of my fare, " + ng + ". I'd send it back out of my first wages.",
                                                  "Could you stand me a little toward the ticket, " + ng + "? I'm that close to gone.");
                    break;
                case "cards":
                    ask = Voice.Pick(seed + "fa", "I had a bad run at the table, " + ng + ". Just enough to square it, and I'm done with cards.",
                                                  "I owe a man from last night's game, " + ng + ". He's not the patient kind.");
                    break;
                default:
                    ask = Voice.Pick(seed + "fa", "I hate to ask it of you, " + ng + ". I'm behind on what I owe and they're pressing.",
                                                  "Could you spare a few dollars, " + ng + "? Only till I'm square. You have my word.");
                    break;
            }
            s.Add(k, ask, "plead", "face");
            if (given)
                s.Add(g, Voice.Pick(seed + "fg", "Here. Pay me when you can, and not before.", "I've not much, but you're welcome to some of it.", "Put that face away. Here."), "yes")
                 .Add(k, Voice.Pick(seed + "fk", "I'll remember this kindness.", "Bless you. I mean it.", "You're a better friend than I've earned."), "thanks");
            else
                s.Add(g, temper == "Cold" || temper == "Proud" || temper == "Greedy"
                         ? Voice.Pick(seed + "fn", "A body ought to keep out of debt, then.", "Lending's how friends turn strangers.")
                         : Voice.Pick(seed + "fn", "I'm stretched thin as it is.", "You know I would if I could. I can't."), "no")
                 .Add(k, Voice.Pick(seed + "fk", "Right. Forget I asked.", "No. I suppose not.", "Well. I had to try."), "mild");
            s.Beats[0].To = g;
            for (int i = 1; i < s.Beats.Count; i++)
                s.Beats[i].To = s.Beats[i].Who == k ? g : k;
            return s;
        }

        /// Every line the two new kinds can say, for the line checks.
        public static IEnumerable<string> MindLines()
        {
            var c = new SceneCast { Names = new[] { "Hiram", "Cobb", null } };
            foreach (bool night in new[] { false, true })
                for (int i = 0; i < 40; i++)
                {
                    c.Night = night;
                    foreach (Beat b in Brushoff(c, "b" + i, i % 2).Beats)
                        if (b.Line != null)
                            yield return b.Line;
                    foreach (string w in new[] { "debt", "bank", "savings", "cards" })
                        foreach (Beat b in AskFavour(c, "f" + i + w, i % 2, w).Beats)
                            if (b.Line != null)
                                yield return b.Line;
                }
        }
    }
}
