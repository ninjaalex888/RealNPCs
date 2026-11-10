// Places mourned (THREEHOUR item 5, deepening SIXDAY item 24). Places already hold a memory of yours: someone who knew one you killed
// thinks of it near the spot (Core/PlaceMemory.cs, [Mind] PlacesRemember). That's a thought, and only for your killings. This is the
// rest of the ledger of the dead (Core/Deaths.cs), which keeps where each death near you happened: for three weeks after a death by
// another hand, someone who was close to the dead (a warm tie, or sweethearts: Core/Ties.cs), passing within 15 m of the spot, stops
// there, turns to it and thinks of them; the closest (warmth 2 or more, or sweethearts) kneel a while, in the mourning kneel the
// game's been seen to play. Your killings stay PlaceMemory's, so nobody has two thoughts at one spot. One a day each, for each
// dead; one in the town every 30 s at most. Behind [Mind] PlacesMourned. Why it reads true: grief keeps returning to the place,
// and a street that has seen a death goes on knowing it long after the body is gone.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class PlacesMourned
    {
        public const float NearMetres = 15f;     // passing this near the spot
        public const float LookOutMetres = 45f;  // the spots looked at: those this near you
        public const long WithinDays = 21;
        public const int GapMs = 30000, LookMs = 2000, KneelMs = 9000, KneelWarmth = 2, MostPlaces = 24;

        /// The deaths in this town with a place, by another hand than yours, within the weeks they're remembered there, newest first.
        public static List<DeathEntry> Places(List<DeathEntry> deaths, string town, long now)
        {
            if (deaths == null || town == null)
                return new List<DeathEntry>();
            return deaths.Where(d => d != null && d.Town == town && d.By != "player" && (d.X != 0f || d.Y != 0f) && d.At <= now && now - d.At <= WithinDays * 1440)
                         .OrderByDescending(d => d.At).Take(MostPlaces).ToList();
        }

        /// The warm tie between the living and the dead, or null: friends (warmth above 0) or sweethearts.
        public static SceneTie TieWith(List<SceneTie> ties, string livingId, string deadId)
        {
            if (ties == null || livingId == null || deadId == null || livingId == deadId)
                return null;
            foreach (SceneTie t in ties)
                if (t != null && t.Has(livingId) && t.Has(deadId) && (t.Warmth > 0 || t.Courted))
                    return t;
            return null;
        }

        public static bool Kneels(SceneTie t)
        {
            return t != null && (t.Warmth >= KneelWarmth || t.Courted);
        }

        public static string DayKey(string livingId, string deadId, long now)
        {
            return livingId + "|" + deadId + "|" + now / 1440;
        }

        private static readonly string[] Passing =
        {
            "That's where {d} fell. I still can't walk past it slow.",
            "Right about there. {d} was laughing not an hour before.",
            "{d}. Somebody ought to put a cross up on this spot.",
            "Rain took the stain off that ground. It didn't take the rest.",
            "Went the long way round all week. And here I am anyhow.",
            "Every time I pass, I see {d} lying there."
        };

        private static readonly string[] Dear =
        {
            "{d}. I'm sorry I wasn't here.",
            "I come and talk to you here, {d}. Nowhere else feels right.",
            "Lord, keep {d}. I'll keep the spot.",
            "Down on my knees in the street. Let them look. {d} would've."
        };

        public static string Thought(string deadFirstName, bool kneel, string seed)
        {
            string[] pool = kneel ? Dear : Passing;
            return pool[System.Math.Abs(Voice.Hash(seed) % pool.Length)].Replace("{d}", deadFirstName ?? "him");
        }

        public static IEnumerable<string> AllLines()
        {
            return Passing.Concat(Dear);
        }
    }
}
