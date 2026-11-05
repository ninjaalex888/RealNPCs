// A body's look as a fingerprint (night 2026-10-29, item 5; roadmap 34, phase A's second half: identity by look of our own,
// standing alone). The model and every piece the game has dressed the body in, with its tints, hashed: the same body met
// again reads the same, so the same person can be known in it (Core: Store.ResidentByLook). Adapted from enRAGE's
// Fingerprint (NemesisMod, its author permits reuse; the "full" print it keys its own prints by): the same natives, 8-byte
// out slots, the pieces sorted and hashed by FNV-1a with a separator between them. Null when the body isn't ready to be
// read (not yet rendered) or wears nothing readable, as enRAGE leaves an empty print unused.
using System;
using System.Collections.Generic;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Looks
    {
        public const int MostPieces = 64;

        /// The look of this body, "MODEL-HASH" in hex, or null.
        // held back with the clothes read in sights (2026-10-31): the metaped piece calls hung the game thread in Saint Denis
        public static readonly bool HeldBack = true;

        public static unsafe string Of(Ped p)
        {
            if (HeldBack)
                return null;
            if (p == null || !p.Exists() || p.IsDead)
                return null;
            int h = p.Handle;
            if (!PED.IS_PED_READY_TO_RENDER(h))
                return null;
            int n = PED._GET_NUM_COMPONENTS_IN_PED(h);
            ulong* guids = stackalloc ulong[4];
            ulong* tint = stackalloc ulong[4];
            var parts = new List<string>();
            for (int i = 0; i < n && i < MostPieces; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    guids[j] = 0;
                    tint[j] = 0;
                }
                if (!PED.GET_META_PED_ASSET_GUIDS(h, i, (uint*)guids, (uint*)(guids + 1), (uint*)(guids + 2), (uint*)(guids + 3)))
                    continue;
                PED.GET_META_PED_ASSET_TINT(h, i, (uint*)tint, (int*)(tint + 1), (int*)(tint + 2), (int*)(tint + 3));
                uint category = 0;
                try
                {
                    category = (uint)PED._0x9B90842304C938A7((ulong)h, (ulong)i, 0);   // the piece's category
                }
                catch
                {
                }
                uint drawable = (uint)guids[0], albedo = (uint)guids[1];
                if (drawable == 0 && albedo == 0)
                    continue;
                parts.Add(category.ToString("X8") + ":" + drawable.ToString("X8") + "/" + albedo.ToString("X8") + " " + ((uint)tint[0]).ToString("X8") + "." + (int)tint[1] + "." + (int)tint[2] + "." + (int)tint[3]);
            }
            if (parts.Count == 0)
                return null;
            parts.Sort(StringComparer.Ordinal);
            return ((uint)ENTITY.GET_ENTITY_MODEL(h)).ToString("X8") + "-" + Fnv(parts).ToString("X8");
        }

        // ---- the probe row's read (Run 3, item 5: the recognition plan's Stage 2) ----------------------------------------
        // The Testing row "Read the looks of the people near you" is the only caller; Of above stays held back. It asks one
        // tier more of the game at each depth ([World] LooksReadDepth): 1 whether the body is drawn and how many pieces it
        // wears; 2 each piece's drawable and albedo; 3 its palette and tints; 4 its kind (the category native, which read 0 on
        // Arthur for every piece, kept last: enRAGE's print has it, so only at 4 can the two be the same). Before each call the
        // hang watch is told which call and which piece, and lastread.txt says the same, so a freeze leaves the call that held
        // it. The pieces go into the form Of uses, so at depth 4 Full can be set beside enRAGE's own print of the body.
        private const string AskDrawn = "drawn", AskCount = "count", AskAssets = "guids", AskTints = "tint", AskKind = "category";

        public static unsafe LookRead Read(Ped p, int depth)
        {
            var got = new LookRead { Depth = Math.Max(1, Math.Min(4, depth)) };
            bool there = p != null && p.Exists();
            int h = there ? p.Handle : 0;
            if (!there || ENTITY.IS_ENTITY_DEAD(h))
            {
                got.Why = there ? "dead or knocked out" : "gone";
                return got;
            }
            got.Handle = h;
            got.Model = (uint)ENTITY.GET_ENTITY_MODEL(h);
            if (!Dressed.Settled(h))
            {
                got.Why = "dressed " + Dressed.SinceMs(h) / 1000 + " s ago";
                return got;
            }
            string body = "body " + h + " of model " + got.Model.ToString("X8");
            var clock = System.Diagnostics.Stopwatch.StartNew();
            int w = WestworldScript.WatchEnter("looks probe");
            try
            {
                WestworldScript.WatchNative(AskDrawn, 0, 0);
                ReadMarks.Set(body + ", is it drawn");
                if (!PED.IS_PED_READY_TO_RENDER(h))
                {
                    got.Why = "not drawn yet";
                    return got;
                }
                WestworldScript.WatchNative(AskCount, 0, 0);
                ReadMarks.Set(body + ", how many pieces");
                got.Raw = PED._GET_NUM_COMPONENTS_IN_PED(h);
                if (got.Raw <= 0)
                {
                    got.Why = "no pieces";
                    return got;
                }
                if (got.Depth < 2)
                    return got;
                int upTo = Math.Min(got.Raw, MostPieces);
                ulong* asset = stackalloc ulong[4];   // a whole word a slot: the game writes 8 bytes for each
                ulong* shade = stackalloc ulong[4];
                var withTints = new List<string>(upTo);
                var plain = new List<string>(upTo);
                for (int i = 0; i < upTo; i++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        asset[k] = 0;
                        shade[k] = 0;
                    }
                    string at = body + ", piece " + (i + 1) + " of " + upTo + ", ";
                    WestworldScript.WatchNative(AskAssets, i + 1, upTo);
                    ReadMarks.Set(at + AskAssets);
                    if (!PED.GET_META_PED_ASSET_GUIDS(h, i, (uint*)asset, (uint*)(asset + 1), (uint*)(asset + 2), (uint*)(asset + 3)))
                        continue;
                    got.Answered++;
                    uint drawable = (uint)asset[0], albedo = (uint)asset[1];
                    if (drawable == 0 && albedo == 0)
                        continue;   // an empty slot
                    string tints = "";
                    if (got.Depth >= 3)
                    {
                        WestworldScript.WatchNative(AskTints, i + 1, upTo);
                        ReadMarks.Set(at + AskTints);
                        PED.GET_META_PED_ASSET_TINT(h, i, (uint*)shade, (int*)(shade + 1), (int*)(shade + 2), (int*)(shade + 3));
                        tints = " " + ((uint)shade[0]).ToString("X8") + "." + (int)shade[1] + "." + (int)shade[2] + "." + (int)shade[3];
                    }
                    uint kind = 0;
                    if (got.Depth >= 4)
                    {
                        WestworldScript.WatchNative(AskKind, i + 1, upTo);
                        ReadMarks.Set(at + AskKind);
                        try
                        {
                            kind = (uint)PED._0x9B90842304C938A7((ulong)h, (ulong)i, 0);
                        }
                        catch
                        {
                            kind = 0;
                        }
                        if (kind != 0)
                            got.Kinds++;
                    }
                    string piece = kind.ToString("X8") + ":" + drawable.ToString("X8") + "/" + albedo.ToString("X8");
                    plain.Add(piece);
                    withTints.Add(piece + tints);
                }
                got.Kept = plain.Count;
                if (got.Kept == 0)
                {
                    got.Why = "nothing readable (" + got.Answered + " of " + upTo + " pieces answered)";
                    return got;
                }
                withTints.Sort(StringComparer.Ordinal);
                plain.Sort(StringComparer.Ordinal);
                string model = got.Model.ToString("X8") + "-";
                got.Full = model + Fnv(withTints).ToString("X8");
                got.Bare = model + Fnv(plain).ToString("X8");
                if (got.Answered < upTo - 1)
                    got.Refused = "half read, " + got.Answered + " of " + upTo + " pieces answered";   // part of a body's print is someone else's: all but one at most (the plan)
                return got;
            }
            finally
            {
                WestworldScript.WatchLeave(w);
                ReadMarks.Done();
                got.Ms = clock.Elapsed.TotalMilliseconds;
            }
        }

        private static uint Fnv(List<string> parts)
        {
            uint x = 2166136261u;
            foreach (string part in parts)
            {
                foreach (char c in part)
                {
                    x ^= c;
                    x *= 16777619u;
                }
                x ^= '|';
                x *= 16777619u;
            }
            return x;
        }
    }

    /// One read of a body's looks by the probe row (Looks.Read): what was asked, what answered, how long it took.
    internal sealed class LookRead
    {
        public int Handle, Depth;
        public uint Model;
        public int Raw;        // the pieces the body says it wears
        public int Answered;   // those whose read came back (depth 2 and up)
        public int Kept;       // with something in them
        public int Kinds;      // with a kind other than 0 (depth 4)
        public double Ms;
        public string Full;    // "MODEL-HASH" over the pieces with their tints (enRAGE's own form at depth 4)
        public string Bare;    // the same without the tints
        public string Why;     // nothing read, and why ("not drawn yet", "dressed 3 s ago", "no pieces")
        public string Refused; // read, but not to be trusted ("half read, 5 of 11 pieces answered")
    }
}
