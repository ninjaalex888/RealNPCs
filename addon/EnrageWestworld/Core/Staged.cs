// Encounters re-staged from the game's own (NIGHT4 item 4; Lex: "pull from any existing NPC encounters like ... the
// execution ... that our MindsEye model could easily apply their inner thoughts to"). Rockstar's random encounters
// (the beat_* scripts) are played again with the people RealNPCs keeps, and at each turn every person in one chooses
// what they do from a short list of what that encounter can show: their temper, mood and drink, what lies between
// them and the other, weigh each option (the mind's own choice is a draw on those weights, stable by the seed), and
// with [Mind] LanguagePicks the model may pick among the same options instead. An option weighted 0 is never offered,
// so whatever the model picks is one the person could do. The tables live with each encounter (StreetCallouts.cs,
// Domestic.cs, Hogtied.cs); this is what they share. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// Who is choosing, as an encounter's tables read them.
    public sealed class StagedMind
    {
        public string Temper;     // Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter, or null
        public string Mood;       // "sore", "glad" or null (InnerLife.MoodOf)
        public bool Woman, Drunk;
        public string Shamed;     // slighted, had words or beaten lately and not answered it, else null
        public int Warmth;        // to the other in it, -3..3 (the scenes' tie)
        public int ToYou;         // to the player, -3..3 (their regard: below 0 they think ill of him)
        public string Worry;      // the worry they're living through today (debt, feud, drink, ...), or null
    }

    public static class Staged
    {
        /// The weights an encounter's table gives, in its own order (the order breaks ties, so it stays the same).
        public sealed class Weights
        {
            public readonly List<string> Ids = new List<string>();
            public readonly List<int> W = new List<int>();

            public Weights Add(string id, int w)
            {
                int i = Ids.IndexOf(id);
                if (i >= 0)
                    W[i] += w;
                else
                {
                    Ids.Add(id);
                    W.Add(w);
                }
                return this;
            }

            public int Of(string id)
            {
                int i = Ids.IndexOf(id);
                return i < 0 ? 0 : W[i];
            }

            /// Never below zero, never above nine.
            public Weights Clamp()
            {
                for (int i = 0; i < W.Count; i++)
                    W[i] = W[i] < 0 ? 0 : W[i] > 9 ? 9 : W[i];
                return this;
            }

            public int Possible
            {
                get
                {
                    int n = 0;
                    foreach (int w in W)
                        if (w > 0)
                            n++;
                    return n;
                }
            }
        }

        /// The mind's own choice: a draw on the weights by the seed, so the same person in the same moment does the same.
        public static string Choose(Weights w, string seed)
        {
            int sum = 0;
            for (int i = 0; i < w.W.Count; i++)
                sum += w.W[i] > 0 ? w.W[i] : 0;
            if (sum <= 0)
                return w.Ids.Count > 0 ? w.Ids[0] : null;
            int roll = Voice.Hash(seed + "|staged") % sum;
            for (int i = 0; i < w.W.Count; i++)
            {
                if (w.W[i] <= 0)
                    continue;
                if (roll < w.W[i])
                    return w.Ids[i];
                roll -= w.W[i];
            }
            return w.Ids[w.Ids.Count - 1];
        }

        /// What is put to the model: the possible options, heaviest first, at most `most` (Language.MostOptions), the mind's
        /// own always among them.
        public static List<string> Offer(Weights w, string mind, int most)
        {
            var order = new List<int>();
            for (int i = 0; i < w.W.Count; i++)
                if (w.W[i] > 0)
                    order.Add(i);
            order.Sort((x, y) => w.W[y] != w.W[x] ? w.W[y].CompareTo(w.W[x]) : x.CompareTo(y));
            var ids = new List<string>();
            foreach (int i in order)
                if (ids.Count < most)
                    ids.Add(w.Ids[i]);
            if (mind != null && w.Of(mind) > 0 && !ids.Contains(mind))
            {
                if (ids.Count >= most)
                    ids.RemoveAt(ids.Count - 1);
                ids.Add(mind);
            }
            return ids;
        }

        /// The model's answer read against what was offered: the id, or null for the mind's own.
        public static string Picked(string answer, List<string> ids, out string thought)
        {
            int i = Language.ParseThinkPick(answer, ids.Count, out thought);
            return i >= 0 && i < ids.Count ? ids[i] : null;
        }

        /// The temper's heat, 0 (a coward) to 4 (a hothead): for who speaks first and who backs down.
        public static int Heat(string temper)
        {
            switch (temper)
            {
                case "Hothead": return 4;
                case "Proud": return 3;
                case "Cold": case "Greedy": return 2;
                case "Coward": return 0;
                default: return 1;
            }
        }
    }
}
