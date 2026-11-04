// Town regulars: the ordinary people who live in a town and are met there again and again. When the game puts an
// ordinary person in the street out of sight, the game side asks here who they are: a regular of the same build who
// isn't anywhere else right now comes back (the one met longest ago), or, while the town has room, someone new
// becomes a regular with an outfit no other regular of that build in the town wears, so each keeps a look of their
// own. Regulars are saved like any resident (their worries follow from their Id, so they go on between visits).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Regulars
    {
        /// At most this many regulars of one build in one town (each needs an outfit of their own).
        public const int PerModel = 4;

        /// A regular of this town and build who is free to be met: not dead, not gone to the wilds, not someone else
        /// right now (busy). The one met longest ago, so they take turns. Null if none.
        public static Resident Back(IEnumerable<Resident> all, string town, uint model, bool woman, Func<Resident, bool> busy)
        {
            return all.Where(r => r.Regular && !r.Dead && !r.Fled && r.Held == null && r.Town == town && r.Model == model && r.Woman == woman && !busy(r))
                      .OrderBy(r => r.TouchedAt).FirstOrDefault();
        }

        /// The same, by routine (Routines.cs; SIXDAY 5a): of the free regulars of this town and build, the one whose habits
        /// put them at this spot now (the best fit; ties to the one met longest ago). `fit` is the best fit found (-1: none).
        public static Resident BackFit(IEnumerable<Resident> all, string town, uint model, bool woman, Func<Resident, bool> busy, Func<Resident, int> fit, out int bestFit)
        {
            Resident best = null;
            bestFit = -1;
            foreach (Resident r in all)
            {
                if (!r.Regular || r.Dead || r.Fled || r.Held != null || r.Town != town || r.Model != model || r.Woman != woman || busy(r))
                    continue;
                int f = fit(r);
                if (best == null || f > bestFit || (f == bestFit && r.TouchedAt < best.TouchedAt))
                {
                    best = r;
                    bestFit = f;
                }
            }
            return best;
        }

        /// The outfit a new regular of this build in this town would wear: one that no other regular of the build here
        /// wears. -1 when the town has its RegularsPerTown, the build has its share, or there are no outfits left.
        public static int NewOutfit(IEnumerable<Resident> all, string town, uint model, int outfits, int perTown, string seed)
        {
            List<Resident> here = all.Where(r => r.Regular && !r.Dead && r.Town == town).ToList();
            if (here.Count >= perTown)
                return -1;
            HashSet<int> worn = new HashSet<int>(here.Where(r => r.Model == model).Select(r => r.Outfit));
            if (worn.Count >= PerModel || outfits <= worn.Count)
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

        /// How many regulars each town has, for the log: "Valentine 14, Rhodes 9".
        public static string Count(IEnumerable<Resident> all)
        {
            List<string> parts = all.Where(r => r.Regular && !r.Dead && r.Town != null).GroupBy(r => r.Town).OrderByDescending(g => g.Count())
                                    .Select(g => g.Key + " " + g.Count()).ToList();
            return parts.Count == 0 ? "none yet" : string.Join(", ", parts);
        }
    }
}
