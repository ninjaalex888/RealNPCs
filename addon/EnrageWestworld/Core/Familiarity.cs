// Familiarity (DEPTH item 17; [Mind] Familiarity, on). Two men of the town who keep ending up near each other at the same place (the bar, a
// porch, the hitching rail) three times in a game day, an hour or more apart, become acquainted: a tie at warmth 0, kind "acquainted",
// in what Ties already keeps. From then on what Ties already does with a tie follows: a nod or a word in passing (Passing: acquaintances
// speak most of the time), the town's talk knows them as known to each other, and in a brawl one leans to the other a little (a stake
// of his own, smaller than a friend's: AcquaintedPull). Nothing new saved but the tie itself; the sightings are in memory and go with
// the day. Why it reads true: the mere-exposure effect (Zajonc, 1968), seeing someone again and again is enough to warm to him; and
// propinquity (Festinger, Schachter and Back, 1950), friendships form where paths cross most.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class Familiarity
    {
        public static bool On = true;   // [Mind] Familiarity, from the settings
        public const string Kind = "acquainted";
        public const int Times = 3, ApartMinutes = 60, MostPairs = 2000;
        public const float NearMetres = 6f, AcquaintedPull = 0.06f;

        sealed class Seen
        {
            public long Day, Last;
            public int Count;
        }

        readonly Dictionary<string, Seen> _seen = new Dictionary<string, Seen>();

        /// Two of the town seen near each other at the same place at game minute `now`: true the moment it's the third time today,
        /// an hour or more after the one before (they're acquainted now); a sighting within the hour of the last doesn't count.
        public bool Note(string a, string b, long now)
        {
            if (a == null || b == null || string.Equals(a, b))
                return false;
            string key = string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
            long day = now / 1440;
            Seen s;
            if (!_seen.TryGetValue(key, out s) || s.Day != day)
            {
                if (_seen.Count >= MostPairs)
                    _seen.Clear();   // a new day's worth: the old days' sightings are worth nothing
                _seen[key] = new Seen { Day = day, Last = now, Count = 1 };
                return false;
            }
            if (now - s.Last < ApartMinutes)
                return false;
            s.Last = now;
            s.Count++;
            return s.Count == Times;
        }

        public int Pairs { get { return _seen.Count; } }

        public void Clear() { _seen.Clear(); }
    }
}
