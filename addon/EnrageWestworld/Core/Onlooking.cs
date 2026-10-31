// What an onlooker's own feeling moves them to do about trouble between others, acted out (roadmap 39, part 2; night
// 2026-10-29, item 3; with [Mind] MindLive and [Mind] MindOnlookers). The shadow already takes every recorded happening
// person by person (Appraisal, Display); here the urge of someone who only watched it becomes one small thing the game is
// proven able to play: the timid and anyone shown afraid walk off from it, a fright at a gun or the uncanny roots them to
// the spot, contempt shakes its head at it (and the pious or orderly, disapproving, shake their heads and walk off: men
// left a brawl too, out of propriety, the review of item 3), fear shown as bluster bristles, and a hard interest turns the
// whole body to watch. In a saloon, where a fight was nothing new, a mild fright stays to watch. A feeling kept off the face plays nothing (the thought says it), but
// for fear that wants away: masked, it slips off quietly (the mind doc's timid clerk: "looks away, steps back"). Those
// who are sitting or leaning, in a scenario, only look, as they did before. At most MostAScene act on one happening, the
// strongest feelings first, so a street isn't a chorus. Pure; plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Onlooking
    {
        /// The acts, in the words the log uses.
        public static readonly string[] Acts = { "leave", "hurry", "slip", "freeze", "dismiss", "disapprove", "bristle", "watch" };

        /// The happenings onlookers act on: trouble between people (never one meant not to be seen: a pickpocket, a lift,
        /// someone helped up), and what the player did.
        public static bool ActsOn(string kind)
        {
            if (kind == null)
                return false;
            if (kind.StartsWith("deed:"))
                return true;
            switch (kind)
            {
                case "scene:rough": case "scene:brawl": case "scene:argue": case "scene:x:quarrel": case "scene:snap": case "scene:x:snap": case "scene:standoff": case "scene:drunk": case "scene:scorn":
                    return true;
                default:
                    return false;
            }
        }

        /// At most this many onlookers act on one happening (the run's rule: two a scene).
        public const int MostAScene = 2;
        public const int MostRunning = 4;   // a fright that spreads (Spread): up to this many run from one happening
        public static int Running = MostRunning;   // [Mind] OnlookersRunning (set from the settings)

        /// How strong a stranger's fright must be to send them off from someone else's trouble (a milder one watches it
        /// warily): between felt (0.12) and moderate (0.35).
        public const float LeaveBar = 0.25f;

        /// What this onlooker does about it, or null for nothing beyond the look they already gave. `role`: the
        /// appraisal's (onlooker or friend; the two in it are the scene's, never this); `sitting`: in a scenario; `place`:
        /// the happening's (saloon...); `t`: their nature (the pious and the orderly disapprove); `bravery`: [World] Bravery
        /// (0 as built, the bars as they were; higher, a fright must be worse to send them off, and the rest stay to watch).
        public static string ActFor(Feeling f, string shows, string role, bool sitting, string place = null, Traits t = null, int bravery = 0)
        {
            bool saloon = place == "saloon", strong = f != null && f.Intensity >= Appraisal.Strong;
            float hurryBar = Bravery.HurryBar(bravery), leaveBar = Bravery.LeaveBar(bravery);
            if (f == null || f.Kind == "none" || sitting)
                return null;
            if (role != "onlooker" && role != "friend")
                return null;
            if (shows == "masked")
                return f.Urge == "avoid" && (saloon ? strong : f.Intensity >= Appraisal.Moderate) ? "slip" : null;   // fear kept off the face still wants away: a look away and a quiet walk off (a man before other men; the mind doc's timid clerk)
            switch (f.Urge)
            {
                case "avoid":
                    if (shows == "turned")
                        return "bristle";   // fear shown as bluster stands and glares instead
                    if (role == "onlooker" && f.Intensity < leaveBar)
                        return f.Kind == "fear" ? "watch" : null;   // a stranger's mild fright doesn't send them off: they watch it warily (the advisor, after StreetVolumeTest: two in three fights had someone walk off)
                    if (f.Intensity >= hurryBar && f.Kind == "fear" && role == "onlooker")
                        return "hurry";
                    if (bravery > 0 && strong && f.Kind == "fear" && role == "onlooker")
                        return "watch";   // badly frightened, and brave: stands back and watches it (Lex, 2026-10-31)   // real danger, badly frightened: they hurry off, not stroll (NIGHT3; a brisk walk away, the flee still unproven)
                    return saloon && !strong ? "watch" : "leave";   // a saloon's fights were nothing new: a mild fright there stays to watch
                case "freeze":
                    return "freeze";
                case "dismiss":
                    return t != null && (t.Z[Traits.Piety] > 0.8f || t.Z[Traits.Order] > 0.8f) ? "disapprove" : "dismiss";   // the pious and the orderly won't stand and watch it
                case "watch":
                case "intervene":   // moved to step in, and someone else did (or nobody could): watches it hard
                    if (role == "onlooker" && t != null && (t.Z[Traits.Piety] > 0.8f || t.Z[Traits.Order] > 0.8f) && f.Kind != "fear" && f.Intensity >= Appraisal.Moderate)
                        return "disapprove";   // the pious and the orderly don't stand about watching a brawl: a shake of the head, and off
                    return f.Intensity >= Appraisal.Moderate || f.Kind == "fear" ? "watch" : null;   // a mild interest is the look they already gave
                default:
                    return null;
            }
        }

        /// Of these (each an id, the feeling, its act), the ones that act: the strongest feelings first, at most `most`,
        /// one act a person; the same people give the same answer (the id breaks a tie).
        public static List<int> Pick(IList<string> ids, IList<Feeling> felt, IList<string> acts, int most = MostAScene)
        {
            var order = new List<int>();
            for (int i = 0; i < ids.Count; i++)
                if (acts[i] != null && felt[i] != null)
                    order.Add(i);
            order.Sort((x, y) =>
            {
                int c = felt[y].Intensity.CompareTo(felt[x].Intensity);
                return c != 0 ? c : string.CompareOrdinal(ids[x], ids[y]);
            });
            var seen = new HashSet<string>();
            var picked = new List<int>();
            foreach (int i in order)
            {
                if (picked.Count >= most)
                    break;
                if (seen.Add(ids[i]))
                    picked.Add(i);
            }
            return picked;
        }

        /// Whether someone rooted by fright then runs (the review: a killing is always armed, so strong fear froze and nobody
        /// hurried): an onlooker's strong fear, shown, after the freeze's moment.
        public static bool FreezeThenRun(Feeling f, string role, int bravery = 0)
        {
            return f != null && f.Kind == "fear" && f.Intensity >= Bravery.HurryBar(bravery) && role == "onlooker" && f.Shows != "masked";
        }

        /// Emotional contagion (NIGHT3, the psychologist's list): one of the picked hurries off, and the others who were
        /// frightened at all, and showed it, go with them, the most frightened first, up to MostRunning in all; their acts
        /// become "hurry" (`acts` is changed). Returns the picked with the ones who caught it after. No hurry, no change.
        public static List<int> Spread(IList<string> ids, IList<Feeling> felt, IList<string> acts, List<int> picked, int most = MostRunning, IList<string> roles = null)
        {
            bool runs = false;
            foreach (int i in picked)
                runs |= acts[i] == "hurry";
            if (!runs)
                return picked;
            var seen = new HashSet<string>();
            foreach (int i in picked)
                seen.Add(ids[i]);
            var catching = new List<int>();
            for (int i = 0; i < ids.Count; i++)
                if (!seen.Contains(ids[i]) && felt[i] != null && felt[i].Kind == "fear" && felt[i].Intensity >= Appraisal.Felt && (acts[i] == "leave" || acts[i] == "watch" || acts[i] == "freeze") &&
                    (roles == null || i >= roles.Count || roles[i] != "friend"))
                    catching.Add(i);   // masked fear (slip), a bristle and a disapproval keep their own ways; a friend of one in it goes toward them, not away
            catching.Sort((x, y) =>
            {
                int c = felt[y].Intensity.CompareTo(felt[x].Intensity);
                return c != 0 ? c : string.CompareOrdinal(ids[x], ids[y]);
            });
            var all = new List<int>(picked);
            foreach (int i in catching)
            {
                if (all.Count >= most)
                    break;
                if (!seen.Add(ids[i]))
                    continue;
                acts[i] = "hurry";
                all.Add(i);
            }
            return all;
        }

        /// How long an act holds them, in milliseconds (the game side's own clock; nobody is held past it).
        public static int HoldMs(string act)
        {
            switch (act)
            {
                case "freeze": return 4000;
                case "watch": return 6000;
                case "leave": case "slip": case "disapprove": case "hurry": return 9000;
                default: return 3000;
            }
        }
    }
}
