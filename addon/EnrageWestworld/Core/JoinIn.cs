// Who joins a fistfight already going, and on whose side (the NPC-life run, Lex 2026-10-30: a brawl "can break out between
// any 2+ NPCs if their inner thoughts lead to it"; and before that, after the bully test: crowds joining, taking sides,
// chains of reactions). The mind doc's crowd rule holds: most watch, few join, fewer the bigger the crowd; the ones who do
// are a fighter's friends, the ones who hate a fighter, and the hot-headed and the drunk, a saloon's most of all. Nobody
// joins with the law at hand. A man's hand against a woman at a tenth of his rate, as the quarrel ladder has it (Lex's
// word, 2026-10-30: 1899); a woman joins only with WomenInFights, at half. Each joiner is a fighter like the first two,
// so a friend of the man he goes for may join in turn: that is how a fight spreads. Seeded, so the same people in the same
// fight answer the same way. Plain C#, no game; the game layer (WestworldScript.JoinIn.cs) asks it every few seconds.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One onlooker's answer: the side they take (0: with the first fighter's side, against the other; 1 the reverse),
    /// whom they go for, and why, for the log.
    public sealed class JoinTake
    {
        public int Side = -1;
        public int Target = -1;     // index into that side's opponents
        public float Score, Bar;
        public string Why;
    }

    /// Someone already in the fight, as the onlooker sees them.
    public sealed class Fighter
    {
        public string Id;          // null: nobody the mind knows
        public bool Woman;
        public int On;             // how many are going for him now (with brawls on: the sides balance, the blows spread)
    }

    public static class JoinIn
    {
        /// At most this many join one fight (two who started it and these: a saloon brawl, not a riot).
        public const int MostJoiners = 3;

        /// With brawls on, added to every onlooker's pull to join ([Mind] BrawlJoinBoost / 100, set by the game side; 0 here, as
        /// the offline tests weigh it). Lex, 2026-11-01, Saint Denis: two men fighting never grew into a brawl, every onlooker
        /// short of the bar (a stranger needs the crowd's bar and a quarter more) and the brawl's own pull only from four.
        public static float BrawlBoost;
        /// Added on top in a fight's first 10 s ([Mind] BrawlOpeningBoost / 100, set by the game side; 0 here).
        public static float OpeningBoost;
        /// [World] Bravery's pull on every onlooker (Bravery.JoinPull, set by the game side; 0 here and at 85): with brawls on or off.
        public static float BraveryPull;

        /// Whether this onlooker joins, and on whose side. sides[0] and sides[1]: who is fighting on each side; warmth(x, y):
        /// the tie between two ids (-3..3, 0 none); witnesses: how many watch; saloon: a saloon's fights draw the drunk and
        /// the hot; joined: how many have joined already.
        public static JoinTake Weigh(string id, Traits t, bool woman, bool drunk, IList<Fighter>[] sides, Func<string, string, int> warmth,
                                     int witnesses, bool lawNear, bool saloon, bool womenFight, int joined, string seed,
                                     int mostJoiners = MostJoiners, bool brawls = false, bool sure = false, float lawHold = -1f,
                                     Func<string, string, string, SideTie> sideTie = null)
        {
            // sideTie(onlooker, fighter, the one he's fighting): what pulls the onlooker to a fighter by who they are (kin, his own
            // trade, the town's own; Core/BrawlSides.cs, FOURTEENHOUR item 6), else null
            // lawHold: the law's hold worked out by sight and distance (Brawl.LawHoldsSeen; FOURTEENHOUR item 5), else -1 for
            // the old rule, Brawl.LawHolds for any lawman within 30 m
            var no = new JoinTake();
            if (t == null || sides == null || sides.Length != 2 || joined >= mostJoiners)
                return no;
            // with [Mind] MindBrawls (Core/Brawl.cs): the law at hand holds most back, not all, and the brawl's own size pulls
            int fighting = sides[0].Count + sides[1].Count;
            float momentum = brawls ? Brawl.Momentum(fighting, saloon) : 0f, lawFactor = 1f;
            if (lawNear)
            {
                if (!brawls)
                {
                    no.Why = "the law at hand";
                    return no;
                }
                lawFactor = lawHold >= 0f ? lawHold : Brawl.LawHolds(saloon, fighting);
            }
            if (woman && !womenFight)
            {
                no.Why = "a woman, and WomenInFights is off";
                return no;
            }
            float Z(int i) => t.Z[i];
            // the heat they bring to any fight: temper, the push to act, the taste for risk, drink; held back by self-control
            float heat = 0.10f * Z(Traits.Anger) + 0.07f * Z(Traits.Assertiveness) + 0.05f * Z(Traits.Risk) - 0.08f * Z(Traits.SelfControl) + (drunk ? 0.12f : 0f) + (saloon ? 0.04f : 0f);
            JoinTake best = no;
            for (int s = 0; s < 2; s++)
            {
                IList<Fighter> mine = sides[s], theirs = sides[1 - s];
                if (theirs.Count == 0)
                    continue;
                int friend = 0, foe = 0, target = 0;   // the one they hate most over there, else the one who started on that side
                foreach (Fighter f in mine)
                    if (f.Id != null && id != null)
                        friend = Math.Max(friend, warmth(id, f.Id));
                for (int i = 0; i < theirs.Count; i++)
                {
                    int w = theirs[i].Id != null && id != null ? warmth(id, theirs[i].Id) : 0;
                    if (w < foe)
                    {
                        foe = w;
                        target = i;
                    }
                }
                if (brawls)
                    target = Spread(theirs, target, foe, woman, foe <= -2 || (Z(Traits.Anger) > 0.8f && Z(Traits.Compassion) < -0.5f));
                // with nobody hated over there, a man goes for a man before a woman when he can
                else if (foe == 0 && !woman && theirs[target].Woman)
                    for (int i = 0; i < theirs.Count; i++)
                        if (!theirs[i].Woman)
                        {
                            target = i;
                            break;
                        }
                float tie = friend > 0 ? 0.22f + 0.08f * friend : 0f;
                if (foe < 0)
                    tie = Math.Max(tie, 0.18f + 0.08f * -foe);
                // his own kind on this side (FOURTEENHOUR item 6): about half a friend's pull, the strongest of them
                float kin = 0f;
                string kinWhy = null;
                if (sideTie != null && id != null && theirs.Count > 0)
                    foreach (Fighter f in mine)
                    {
                        if (f.Id == null)
                            continue;
                        SideTie k = sideTie(id, f.Id, theirs[0].Id);
                        if (k != null && k.Pull > kin)
                        {
                            kin = k.Pull;
                            kinWhy = k.Why;
                        }
                    }
                float score = tie + kin + heat + momentum + BraveryPull;
                if (brawls)
                    score += BrawlBoost + OpeningBoost;
                // with brawls on, someone with no stake in it sides with the side that's short (Lex, 2026-10-31: in the
                // biggest brawl every joiner went for one man): a tenth for each man the other side has more
                if (brawls && tie <= 0f && kin <= 0f)
                    score += Math.Max(-0.2f, Math.Min(0.2f, 0.10f * (theirs.Count - mine.Count)));
                // a stranger to both sides must clear the crowd's bar and more; a friend or a foe is moved by their own; one of his
                // own, halfway between (item 6)
                float bar = tie > 0f ? 0.25f : kin > 0f ? 0.25f + Appraisal.CrowdBar(witnesses) * 0.5f : Appraisal.CrowdBar(witnesses) + 0.25f;
                if (score <= bar && !sure)   // sure: the Testing row's waves, no bar and no roll (the sex rules still hold)
                {
                    if (best.Side < 0 && score > best.Score)
                        best = new JoinTake { Score = score, Bar = bar, Why = Words(friend, foe, heat, drunk) + (kinWhy != null ? ", " + kinWhy : "") + (momentum > 0f ? ", the brawl's pull" : "") + ": not enough to join" };
                    continue;
                }
                float chance = Math.Max(0f, Math.Min(0.8f, (score - bar) * 2.5f));
                if (woman)
                    chance *= 0.5f;   // half as often as a man, as for a quarrel (WomenInFights)
                if (!woman && theirs[target].Woman)
                    chance *= 0.1f;   // a man's hand against a woman, at a tenth (1899; Lex, 2026-10-30)
                chance *= lawFactor;
                bool goes = sure ? !(!woman && theirs[target].Woman) : Voice.Chance(seed + "|join|" + s, (int)Math.Round(chance * 100));
                string why = Words(friend, foe, heat, drunk) + (kinWhy != null ? ", " + kinWhy : "") + (momentum > 0f ? ", the brawl's pull" : "") + (lawFactor < 1f ? ", the law at hand" : "");
                var take = new JoinTake { Side = goes ? s : -1, Target = target, Score = score, Bar = bar, Why = why + " (" + (int)Math.Round(chance * 100) + "%)" };
                if (goes && (best.Side < 0 || score > best.Score))
                    best = take;
                else if (best.Side < 0 && score > best.Score)
                    best = take;
            }
            return best;
        }

        /// Whom a joiner goes for in a brawl: the man he hates, unless two are on him already (three for a man who gangs up:
        /// a grudge of -2 or worse, or a hothead with no pity); else whoever has the fewest on him, a man before a woman for a
        /// man joining. The first such over there on a tie, so the same fight answers the same way.
        public static int Spread(IList<Fighter> theirs, int hated, int foe, bool woman, bool gangsUp)
        {
            int most = gangsUp ? 3 : 2;
            if (foe < 0 && hated >= 0 && hated < theirs.Count && theirs[hated].On < most)
                return hated;
            int best = -1;
            for (int i = 0; i < theirs.Count; i++)
            {
                if (best < 0)
                {
                    best = i;
                    continue;
                }
                Fighter f = theirs[i], b = theirs[best];
                bool fWrong = !woman && f.Woman, bWrong = !woman && b.Woman;   // a man's hand against a woman last
                if (fWrong != bWrong ? !fWrong : f.On < b.On)
                    best = i;
            }
            return Math.Max(0, best);
        }

        /// The joiner is in: on his side, and one more on the man he went for (the next to weigh sees the new counts).
        public static void Join(IList<Fighter>[] sides, JoinTake take, Fighter joiner)
        {
            if (sides == null || take == null || take.Side < 0 || take.Side > 1)
                return;
            IList<Fighter> theirs = sides[1 - take.Side];
            if (take.Target >= 0 && take.Target < theirs.Count)
                theirs[take.Target].On++;
            joiner.On = 0;
            sides[take.Side].Add(joiner);
        }

        private static string Words(int friend, int foe, float heat, bool drunk)
        {
            var w = new List<string>();
            if (friend > 0)
                w.Add("a friend on that side");
            if (foe < 0)
                w.Add("bad blood with the other");
            if (heat >= 0.15f)
                w.Add("hot-headed");
            if (drunk)
                w.Add("drunk");
            return w.Count > 0 ? string.Join(", ", w) : "no stake in it";
        }
    }
}
