// A town on edge (NIGHT3 item 5; the psychologist's lens: fear spreads beyond those who saw, and fades; a sight met again
// moves people less). Worked out when asked from the happenings already kept (Store.WorldLog: kind, town, game minute),
// so nothing new is saved. A killing makes a whole town afraid for some hours, worst in the first (halving every three
// game hours): the safety need rises for everyone there (more so at night), so people out go home, keep clear and
// watch; a gunfight between gangs, a robbery, a mugging weigh less; a brawl or a rough very little, and a man beaten to
// death in a brawl half a killing. And the same kind of trouble seen again in a town within the week moves an onlooker
// less each time: the third brawl draws a look, not a gasp.
// The hush (the game developer's "drama director": quiet after something big) is apart from the fear since Lex in Saint
// Denis, 2026-10-31 ("I would've expected to see some brawls, or some events but not much is happening": the day's
// deaths and brawls together held the fear near 1 for two days, and the brawls fed the hush that stopped brawls). Only a
// killing hushes a street, only the latest one in that town counts, and only for [Mind] HushHours (2 game hours; Saint
// Denis HushHoursSaintDenis, 1): the street's timing holds back the fights, the shoves, the drunken trouble, the thefts
// and the noon callouts, and leaves longer between scenes. A man beaten to death in a brawl ("a beating") and anything
// from a Testing row never start a hush; brawls, rings, roughs, shoves and standoffs never do. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class TownFear
    {
        public float Level;          // 0..1
        public Happening Worst;      // the happening that weighs most now (for the thought's words), or null
        public float WorstWeight;
        public Happening HushBy;     // the latest killing in the town that may hush it (Hushing), or null
        public long HushLeft;        // game minutes the street stays hushed from it (0: not hushed)
    }

    public static class TownMood
    {
        public const float HalfLifeHours = 3f;     // the fear halves every three game hours (six until 2026-10-31): gone in half a day
        public const long WindowMinutes = 1440 * 2;
        public const long NoveltyMinutes = 1440 * 7;
        public const string Beating = "a beating";   // the outcome of a death by a brawl's fists (WestworldScript.BrawlDown: LedgerNote's how)

        /// How long the latest killing in a town hushes its street, in game hours (0: never hushed); Saint Denis, a city
        /// used to trouble, less.
        public static float HushHours = 2f, CityHushHours = 1f;   // [Mind] HushHours, HushHoursSaintDenis
        public const float HushGap = 1.5f;   // the scenes' gap, stretched while hushed

        /// Whether the town is too shaken for the street's own trouble (a killing within the last HushHours).
        public static bool Hushes(TownFear f)
        {
            return f != null && f.HushBy != null && f.HushLeft > 0;
        }

        /// The scenes a hushed street holds back: blows, shoves, drunken trouble, thefts, a callout.
        public static bool Hushed(string kind)
        {
            switch (kind)
            {
                case "rough": case "brawl": case "drunk": case "pickpocket": case "lift": case "standoff": case "callout":
                    return true;
                default:
                    return false;
            }
        }

        /// A killing: a death, or the player's murder or kill.
        public static bool Killing(string kind)
        {
            return kind == "death" || kind == "deed:murder" || kind == "deed:kill";
        }

        /// A death by fists between others: "a beating" (a brawl's, booked by BrawlDown or the ledger), or bare hands not
        /// the player's (the game's own fistfight).
        public static bool Beaten(Happening h)
        {
            return h != null && h.Kind == "death" && (h.Outcome == Beating || (h.Outcome == "bare hands" && h.DoerId != "player"));
        }

        /// A killing that may hush a street: not a man beaten to death in a brawl, nothing from a Testing row.
        public static bool Hushing(Happening h)
        {
            return h != null && Killing(h.Kind) && !Beaten(h) && !h.Test;
        }

        /// How much a happening of this kind frightens a town (0: not at all).
        public static float Weight(string kind)
        {
            if (string.IsNullOrEmpty(kind))
                return 0f;
            switch (kind)
            {
                case "death":
                case "deed:murder":
                case "deed:kill":
                    return 1f;
                case "scene:standoff":
                    return 0.3f;   // guns drawn in the street (the deaths of a gunfight come as "death")
                case "deed:robbery":
                case "scene:mugging":
                    return 0.55f;
                case "deed:assault":
                case "scene:brawl":
                case "scene:ring":
                    return 0.1f;   // 0.25 until 2026-10-31
                case "scene:rough":
                case "deed:shove":
                    return 0.05f;  // 0.15 until 2026-10-31
                default:
                    return 0f;
            }
        }

        /// How much this happening frightens its town: by its kind, and a man beaten to death in a brawl half a killing.
        public static float Weight(Happening h)
        {
            if (h == null)
                return 0f;
            float w = Weight(h.Kind);
            return Beaten(h) ? w * 0.5f : w;
        }

        /// The town's fear now: each frightening happening there in the last two game days, halving every three hours,
        /// added with diminishing returns (two killings frighten more than one, not twice as much); and the hush: the
        /// latest killing that may hush the town, with the game minutes left of it.
        public static TownFear Fear(IEnumerable<Happening> events, string town, long now)
        {
            var f = new TownFear();
            if (events == null || string.IsNullOrEmpty(town) || now < 0)
                return f;
            float left = 1f;
            foreach (Happening h in events)
            {
                if (h == null || h.Town != town || h.At > now || now - h.At > WindowMinutes)
                    continue;
                if (Hushing(h) && (f.HushBy == null || h.At >= f.HushBy.At))
                    f.HushBy = h;
                float w = Weight(h);
                if (w <= 0f)
                    continue;
                float hours = (now - h.At) / 60f;
                float x = w * (float)Math.Pow(0.5, hours / HalfLifeHours);
                left *= 1f - Math.Min(0.95f, x);
                if (x > f.WorstWeight)
                {
                    f.WorstWeight = x;
                    f.Worst = h;
                }
            }
            f.Level = 1f - left;
            if (f.HushBy != null)
            {
                float hushHours = town == "Saint Denis" ? CityHushHours : HushHours;
                f.HushLeft = Math.Max(0, (long)Math.Round(hushHours * 60f) - (now - f.HushBy.At));
            }
            return f;
        }

        /// How new a sight of this kind is to the town: 1 the first in a week, then 0.65, 0.45, 0.35 at the least.
        /// `except`: the happening being looked at (not counted against itself).
        public static float Novelty(IEnumerable<Happening> events, string town, string kind, long now, Happening except = null)
        {
            if (events == null || string.IsNullOrEmpty(town) || string.IsNullOrEmpty(kind) || now < 0)
                return 1f;
            int seen = 0;
            foreach (Happening h in events)
                if (h != null && h != except && h.Town == town && h.Kind == kind && h.At <= now && now - h.At <= NoveltyMinutes)
                    seen++;
            return seen <= 0 ? 1f : seen == 1 ? 0.65f : seen == 2 ? 0.45f : 0.35f;
        }

        /// The fear in a few words, for the model's request ("on edge since a killing this morning"), or null when calm.
        public static string Words(TownFear f, long now)
        {
            if (f == null || f.Level < 0.25f || f.Worst == null)
                return null;
            string what;
            switch (f.Worst.Kind)
            {
                case "death": what = Beaten(f.Worst) ? "a man beaten to death" : "a killing"; break;
                case "deed:murder": case "deed:kill": what = "a killing"; break;
                case "scene:standoff": what = "guns drawn in the street"; break;
                case "deed:robbery": what = "a robbery"; break;
                case "scene:mugging": what = "a man set upon after dark"; break;
                default: what = "a fight"; break;
            }
            long ago = Math.Max(0, now - f.Worst.At);
            string when = ago < 90 ? "just now" : ago < 360 ? "a few hours back" : ago < 1440 ? "today" : "yesterday";
            return (f.Level >= 0.6f ? "badly frightened" : "on edge") + " since " + what + " " + when;
        }
    }
}
