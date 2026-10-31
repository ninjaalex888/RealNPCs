// A town on edge (NIGHT3 item 5; the psychologist's lens: fear spreads beyond those who saw, and fades; a sight met again
// moves people less). Worked out when asked from the happenings already kept (Store.WorldLog: kind, town, game minute),
// so nothing new is saved. A killing makes a whole town afraid for about a day, worst in the first hours: the safety
// need rises for everyone there (more so at night), so people out go home, keep clear and watch; a gunfight between
// gangs, a robbery, a mugging weigh less; a brawl or a rough little. And the same kind of trouble seen again in a town
// within the week moves an onlooker less each time: the third brawl draws a look, not a gasp. And while the fear is
// high (Hushes: about the first five or six hours after a killing) the street is hushed (the game developer's "drama
// director": quiet after something big): the street's timing holds back the fights, the shoves, the drunken trouble,
// the thefts and the noon callouts, and leaves longer between scenes. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class TownFear
    {
        public float Level;          // 0..1
        public Happening Worst;      // the happening that weighs most now (for the thought's words), or null
        public float WorstWeight;
    }

    public static class TownMood
    {
        public const float HalfLifeHours = 6f;     // the fear halves every six game hours: about a day before it's gone
        public const long WindowMinutes = 1440 * 2;
        public const long NoveltyMinutes = 1440 * 7;

        /// The fear at which the street is hushed (a killing within about six hours; two robberies close together); a city
        /// as big as Saint Denis takes more (one killing there doesn't quiet every street: the review).
        public static float HushLevel = 0.5f, CityHushLevel = 0.8f;   // [Mind] HushFear, HushFearSaintDenis
        public const float HushGap = 1.5f;   // the scenes' gap, stretched while hushed

        /// Whether the town is too shaken for the street's own trouble.
        public static bool Hushes(TownFear f, string town = null)
        {
            return f != null && f.Level >= (town == "Saint Denis" ? CityHushLevel : HushLevel);
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
                    return 0.25f;
                case "scene:rough":
                case "deed:shove":
                    return 0.15f;
                default:
                    return 0f;
            }
        }

        /// The town's fear now: each frightening happening there in the last two game days, halving every six hours,
        /// added with diminishing returns (two killings frighten more than one, not twice as much).
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
                float w = Weight(h.Kind);
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
                case "death": case "deed:murder": case "deed:kill": what = "a killing"; break;
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
