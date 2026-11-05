// The Testing row "Read the looks of the people near you" (Run 3, item 5: the recognition plan's Stage 2), the only read of
// anyone's clothes RealNPCs makes while the clothes reads stay held back (Looks.HeldBack, ClothesReadHeldBack, StripHeldBack).
// One press reads up to six people within 40 m, nearest first, each once a session at a depth, at [World] LooksReadDepth:
// one tier of natives at a time (Looks.Read), so a tier that hangs is known before the next is tried. The plan's T1-T4: a
// tier passes after about ten loads and ten fast travels in Saint Denis with no HANG line and no read over 2 ms. The row
// waits on the world-quiet gate (WestworldScript.WorldQuiet.cs) and refuses with why; it never reads a body dressed in the
// last 10 s, the player, a child, a story or unique character, the dead or knocked out, a mission's people, our own spawned
// or a post's person. With enRAGE running each read is set beside enRAGE's own print of the body ("same" only means
// something at depth 4, where the two are made the same way), and each press says how many of enRAGE's prints are of people
// RealNPCs has bound: whether knowing people by look could ever come from enRAGE's reads (the plan's critic, point 11).
using System;
using System.Collections.Generic;
using System.Linq;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int LooksProbeMost = 6;
        private const float LooksProbeMetres = 40f;
        private const double LooksSlowMs = 2.0;
        private readonly HashSet<long> _lookProbed = new HashSet<long>();   // handle and depth read this session (once each)
        private int _looksProbePresses, _looksProbeRead, _looksProbeSame, _looksProbeDiffers, _looksProbeHalf;
        private double _looksProbeSlowest;
        private string _looksProbeLast;

        private static long ProbeKey(int handle, int depth)
        {
            return ((long)handle << 3) | (long)(depth & 7);
        }

        /// The row itself: null when it read (the log has each body), else why it didn't.
        private string LooksProbeTest(int now, out string said)
        {
            said = null;
            int depth = _cfg.LooksReadDepth;
            string quiet = LooksRefusal(now);
            if (quiet != null)
            {
                Log.Write("LOOKS probe refused: " + quiet);
                return quiet;
            }
            ReadMarks.Open(_dir);
            _looksProbePresses++;
            Ped me = Game.Player.Character;
            var skipped = new Dictionary<string, int>();
            var near = new List<Ped>();
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || !PED.IS_PED_HUMAN(p.Handle) || p.Position.DistanceTo(me.Position) > LooksProbeMetres)
                    continue;
                string why = LooksProbeSkip(p, depth);
                if (why != null)
                    ProbeTally(skipped, why);
                else
                    near.Add(p);
            }
            int read = 0;
            double slowest = 0;
            foreach (Ped p in near.OrderBy(x => x.Position.DistanceTo(me.Position)).Take(LooksProbeMost))
            {
                int h = p.Handle;
                LookRead got = Looks.Read(p, depth);
                _lookProbed.Add(ProbeKey(h, depth));
                if (got.Why != null)
                {
                    ProbeTally(skipped, got.Why);
                    continue;
                }
                read++;
                _looksProbeRead++;
                if (got.Ms > slowest)
                    slowest = got.Ms;
                if (got.Refused != null)
                    _looksProbeHalf++;
                string vs = "";
                if (_bridge.Present)
                {
                    string theirs = _bridge.PrintAt(h);
                    if (theirs == null)
                        vs = "; enRAGE: none";
                    else if (got.Full == null)
                        vs = "; enRAGE: has one, ours not made at this depth";
                    else if (got.Full == theirs)
                    {
                        vs = "; enRAGE: same";
                        _looksProbeSame++;
                    }
                    else
                    {
                        vs = "; enRAGE: differs (" + theirs + ")" + (depth < 4 ? ", as it must below depth 4" : "");
                        _looksProbeDiffers++;
                    }
                }
                string model = Natives.ModelName(p);
                Log.Write("LOOKS probe d" + depth + " " + model + " h=" + h + " " + got.Kept + "/" + got.Raw + " pieces" +
                          (depth >= 2 ? " (" + got.Answered + " answered" + (depth >= 4 ? ", " + got.Kinds + " with a kind" : "") + ")" : "") + " " + got.Ms.ToString("0.00") + " ms" +
                          (got.Full != null ? " -> " + got.Full + " (bare " + got.Bare + ")" : "") + (got.Refused != null ? "; not taken: " + got.Refused : "") + vs);
                if (got.Ms > LooksSlowMs)
                    Log.Write("LOOKS slow: " + got.Ms.ToString("0.0") + " ms on " + model + " (" + got.Raw + " pieces)");
            }
            if (slowest > _looksProbeSlowest)
                _looksProbeSlowest = slowest;
            int skips = skipped.Values.Sum();
            string skipList = skips == 0 ? "" : " (" + string.Join(", ", skipped.OrderByDescending(kv => kv.Value).Select(kv => kv.Value + " " + kv.Key)) + ")";
            string prints = "";
            if (_bridge.Present)
            {
                int printed;
                int ours = _bridge.PrintedAmong(x => _resBind.ContainsKey(x), out printed);
                prints = "; enRAGE holds prints of " + printed + " bodies, " + ours + " of them people RealNPCs has bound";
            }
            Log.Write("LOOKS probe: " + read + " read at depth " + depth + ", " + skips + " skipped" + skipList + ", slowest " + slowest.ToString("0.00") + " ms" + prints);
            _looksProbeLast = read + " read at depth " + depth + ", slowest " + slowest.ToString("0.00") + " ms";
            if (read == 0)
                return skips == 0 ? "no one near enough to read (40 m)" : "nobody read" + skipList;
            said = "Looks probe, depth " + depth + ": " + read + " read, the slowest " + slowest.ToString("0.0") + " ms (each in the log)";
            return null;
        }

        private static void ProbeTally(Dictionary<string, int> tally, string why)
        {
            int n;
            tally.TryGetValue(why, out n);
            tally[why] = n + 1;
        }

        /// Why the probe leaves this person alone, or null.
        private string LooksProbeSkip(Ped p, int depth)
        {
            int h = p.Handle;
            if (_lookProbed.Contains(ProbeKey(h, depth)))
                return "read before at this depth";
            if (ENTITY.IS_ENTITY_DEAD(h))
                return "dead or knocked out";
            if (PED.IS_PED_A_PLAYER(h))
                return "a player";
            if (Natives.IsChild(p))
                return "a child";
            if (Natives.StoryModel(p))
                return "a story or unique character";
            if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h))
                return "a mission's (or a scene's)";
            if (Spawner.IsOurs(h))
                return "one RealNPCs spawned";
            if (PostKindOf(p) != null)
                return "a post's person";
            if (!Dressed.Settled(h))
                return "dressed in the last 10 s";
            return null;
        }

        /// F7: the depth, what the row has read this session, the slowest read, how its reads stood beside enRAGE's, and
        /// whether a look may be read now.
        private string LooksProbeStatus()
        {
            string gate = _cfg != null ? LooksRefusal(_now) : null;
            return "depth " + (_cfg != null ? _cfg.LooksReadDepth : 1) + " ([World] LooksReadDepth); " + _looksProbePresses + " presses, " + _looksProbeRead + " bodies read this session" +
                   (_looksProbeRead > 0 ? ", the slowest " + _looksProbeSlowest.ToString("0.00") + " ms" : "") +
                   (_bridge != null && _bridge.Present ? ", beside enRAGE's print " + _looksProbeSame + " same, " + _looksProbeDiffers + " differing" : "") +
                   (_looksProbeHalf > 0 ? ", " + _looksProbeHalf + " half read (not taken)" : "") + (_looksProbeLast != null ? "; last press: " + _looksProbeLast : "") +
                   "; " + (gate ?? "the world is settled: looks may be read now");
        }
    }
}
