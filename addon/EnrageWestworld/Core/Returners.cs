// Returning people, written onto a body (Run 3, item 5: the recognition plan's Stage 1, daytime\recognition-plan.md, with
// its critic's corrections; Lex, 2026-11-04: "Yes start recognition plan"). The game keeps no identity for its passers-by:
// a body met again is a new ped, its clothes rolled anew from the model's ten to forty outfits. So a resident who matters
// (woken, a worry, met) comes back by being CAST onto an unseen body of their own build and dressed in an outfit of their
// own, the same each time: identity written onto the body, never read off it, so no clothes-reading native is called (the
// reads that froze the game in Saint Denis on 2026-10-31 stay held back). No native says which outfit the game rolled, so
// their first body keeps the game's clothes; every later one wears theirs, and from then on they look the same each time.
// Outfits are shared out with the town regulars' (no two people of one build in one town wear the same) and a town takes
// [World] ReturnersPerTown of them at most: enRAGE may take a dressed body for one of its own people, so the fewer the
// better until Lex has seen it. Plain C#; checked by mindtest (ReturnersTest).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Returners
    {
        public const int ChanceAsBuilt = 35, PerTownAsBuilt = 12;   // [World] ReturnersChance, ReturnersPerTown

        /// Everyone the store already dresses marked as having an outfit (0 is an outfit too, so HasOutfit says it; the critic's
        /// point 7): the regulars, the test cast and anyone with an outfit set above 0 (a campfire stranger's -1 means the town
        /// had no room). Not the posts' holders: a post dresses its holder in the post's own outfit (Post.Outfit), which
        /// NewOutfit counts through alsoWorn. Run once at each load. How many were marked.
        public static int MarkOutfits(IEnumerable<Resident> all)
        {
            int n = 0;
            foreach (Resident r in all)
            {
                if (r == null || r.HasOutfit)
                    continue;
                if (r.Regular || r.Cast != null || r.Outfit > 0)
                {
                    r.HasOutfit = true;
                    n++;
                }
            }
            return n;
        }

        /// Whether this resident is one who may return: kept, alive, here, not a regular, the test cast or a post's holder
        /// (they have ways back of their own), not gone to the wilds or away on the train, not held by the law.
        public static bool MayReturn(Resident r, string town)
        {
            return r != null && !r.Regular && r.Cast == null && r.PostId == null && !r.Dead && !r.Fled && !r.Left && r.Held == null && r.Town == town;
        }

        /// Who comes back in this body: a resident who may return, of its build and sex, not someone else right now (busy), with
        /// something to come back for (pull above 0: woken, a worry, met); the strongest pull, ties to the one met longest ago.
        /// Null when nobody.
        public static Resident Back(IEnumerable<Resident> all, string town, uint model, bool woman, Func<Resident, bool> busy, Func<Resident, int> pull)
        {
            Resident best = null;
            int bestPull = 0;
            foreach (Resident r in all)
            {
                if (!MayReturn(r, town) || r.Model != model || r.Woman != woman || busy(r))
                    continue;
                int p = pull(r);
                if (p <= 0)
                    continue;
                if (best == null || p > bestPull || (p == bestPull && r.TouchedAt < best.TouchedAt))
                {
                    best = r;
                    bestPull = p;
                }
            }
            return best;
        }

        /// The outfit a returner without one is given: one that nobody of this build in this town wears (the regulars', the
        /// other returners' and alsoWorn: the posts' of this build there), the town not yet holding perTown returners with
        /// outfits; -1 when none is left (they wait for one: two people in one outfit would be one person to the eye).
        public static int NewOutfit(IEnumerable<Resident> all, string town, uint model, int outfits, int perTown, string seed, IEnumerable<int> alsoWorn = null)
        {
            List<Resident> here = all.Where(r => r != null && !r.Dead && r.Town == town).ToList();
            if (here.Count(r => r.HasOutfit && !r.Regular && r.Cast == null && r.PostId == null) >= perTown)
                return -1;
            HashSet<int> worn = new HashSet<int>(here.Where(r => r.Model == model && (r.Regular || r.HasOutfit)).Select(r => r.Outfit));
            if (alsoWorn != null)
                worn.UnionWith(alsoWorn);
            if (outfits <= 0)
                return -1;
            int start = Voice.Hash(seed) % outfits;
            for (int i = 0; i < outfits; i++)
            {
                int o = (start + i) % outfits;
                if (!worn.Contains(o))
                    return o;
            }
            return -1;
        }

        /// How strongly someone is drawn back: three a stage of waking, two for having met you, one for a worry running now.
        public static int Pull(int wakeStage, bool met, bool worry)
        {
            return 3 * Math.Max(0, wakeStage) + (met ? 2 : 0) + (worry ? 1 : 0);
        }
    }
}
