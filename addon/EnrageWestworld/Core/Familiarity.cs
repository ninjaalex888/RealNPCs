// Familiarity (DEPTH item 17; [Mind] Familiarity, on). Two men of the town who keep ending up near each other at the same place (the bar, a
// porch, the hitching rail) three times in a game day, an hour or more apart, become acquainted: a tie at warmth 0, kind "acquainted",
// kept in memory here, never as a tie (review 2: written as ties, 150 a town a day pushed the town's real ties out of the store, whose
// 300 are shared, and took the strangers' reasons from every acquainted pair). What it does: one thinks of the other when it comes
// (a face he knows now), and in a brawl one leans to the other a little (a stake of his own, smaller than a friend's: AcquaintedPull).
// Nothing saved; the sightings go with the day, the acquaintances with the session (MostKnown at most). Why it reads true: the mere-exposure effect (Zajonc, 1968), seeing someone again and again is enough to warm to him; and
// propinquity (Festinger, Schachter and Back, 1950), friendships form where paths cross most.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class Familiarity
    {
        public static bool On = true;   // [Mind] Familiarity, from the settings
        public const string Kind = "acquainted";
        public const int Times = 3, ApartMinutes = 60, MostPairs = 2000, MostKnown = 3000;
        public const float NearMetres = 6f, AcquaintedPull = 0.06f;

        sealed class Seen
        {
            public long Day, Last;
            public int Count;
        }

        readonly Dictionary<string, Seen> _seen = new Dictionary<string, Seen>();
        readonly HashSet<string> _known = new HashSet<string>();

        static string Key(string a, string b) { return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a; }

        /// Whether the two are acquainted (this session).
        public bool Knows(string a, string b) { return a != null && b != null && _known.Contains(Key(a, b)); }

        public int Known { get { return _known.Count; } }

        /// Two of the town seen near each other at the same place at game minute `now`: true the moment it's the third time today,
        /// an hour or more after the one before (they're acquainted now); a sighting within the hour of the last doesn't count.
        public bool Note(string a, string b, long now)
        {
            if (a == null || b == null || string.Equals(a, b))
                return false;
            string key = Key(a, b);
            if (_known.Contains(key))
                return false;   // acquainted already
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
            if (s.Count != Times)
                return false;
            if (_known.Count >= MostKnown)
                _known.Clear();   // a long session's worth: start over rather than grow without end
            _known.Add(key);
            return true;
        }

        public int Pairs { get { return _seen.Count; } }

        public void Clear() { _seen.Clear(); _known.Clear(); }
    }
}
