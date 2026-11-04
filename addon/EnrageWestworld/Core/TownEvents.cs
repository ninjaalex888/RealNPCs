// Town events on one framework (SIXDAY item 10; roadmap 10; roadmap.md P3, then P22-P24): things a town does on a day,
// posted ahead, that happen whether the player is there or not. Away, the daily tick resolves them in the store (ties warm
// or sour, memories, the paper); present, the game side stages them at a spot with a cast and lines, and shots break them up.
// The first kind is the Saturday dance: every second Saturday in a town with a saloon, seven till eleven, posted on Thursday;
// two to four of the town's regulars come away warmer toward each other, sweethearts fall out once in a while, and anyone
// who was there thinks of it for days. The weddings keep their own code (Core/World.cs, Engine.World.cs) and are listed beside
// these for F7. Why it reads true: a town's week has a shape with a Saturday night in it, and what people remember of a
// dance is who they danced with. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class TownEvent
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Kind;      // dance, election, trial, gallows, show
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Title;
        [DataMember(EmitDefaultValue = false)] public long At, Until;   // game minutes: the start, the end
        [DataMember(EmitDefaultValue = false)] public bool Posted, Done, Attended, Broken;
        [DataMember(EmitDefaultValue = false)] public string Outcome;   // away, attended, broken (why)
        [DataMember(EmitDefaultValue = false)] public string AId, AName, BId, BName;   // the kind's own people (candidates, the accused...)
        [DataMember(EmitDefaultValue = false)] public string Extra;     // the kind's own word (the office, the verdict, the show's bill)
        [DataMember(EmitDefaultValue = false)] public int Count;        // the kind's own number (votes for A, the crowd)
        [DataMember(EmitDefaultValue = false)] public int Count2;       // (votes for B)
        [DataMember(EmitDefaultValue = false)] public string Endorsed, WhiskeyFor, LeanedOn;   // the player's part in an election, by candidate id (Core/Elections.cs)
    }

    public static class TownEvents
    {
        public const int DanceFrom = 19, DanceTo = 23;         // the hours
        public const int DanceEveryWeeks = 2;                  // every second Saturday, by the town's own parity
        public const int DancePostDay = 4;                     // posted on Thursday (0 Sunday)
        public const int DanceStayMinutes = 60;                // present for this long counts as having been to it (game minutes)
        public const int DanceMost = 4, DanceLeast = 2;        // regulars who come away warmer
        public const int DanceAffection = 6;                   // what a regular feels for the player who came
        public const float NearMetres = 150f;                  // the player this near the town's heart is present
        public static readonly string[] DanceTowns = { "Valentine", "Rhodes", "Blackwater", "Strawberry", "Saint Denis", "Armadillo", "Van Horn", "Tumbleweed", "Annesburg" };

        /// Days from this weekday (0 Sunday) to the coming Saturday: 0 on a Saturday; -1 when the weekday isn't known.
        public static int DaysToSaturday(int weekday)
        {
            return weekday < 0 || weekday > 6 ? -1 : (6 - weekday + 7) % 7;
        }

        /// Whether this town's dance falls on the week of this game day (every second week, the town's own parity).
        public static bool DanceWeek(string town, long gameDay)
        {
            return ((gameDay / 7) + Voice.Hash(town ?? "") % DanceEveryWeeks) % DanceEveryWeeks == 0;
        }

        /// The dance on the Saturday that is `saturdayDay` (a game day): seven till eleven.
        public static TownEvent Dance(string town, long saturdayDay)
        {
            return new TownEvent
            {
                Id = Ids.New(), Kind = "dance", Town = town, Title = "The Saturday dance", At = saturdayDay * 1440 + DanceFrom * 60, Until = saturdayDay * 1440 + DanceTo * 60
            };
        }

        /// Whether the event is on now (a little before the start, for the staging).
        public static bool Open(TownEvent e, long now)
        {
            return e != null && !e.Done && now >= e.At - 30 && now < e.Until;
        }

        /// The notice posted ahead, and the paper's item.
        public static string PostedText(TownEvent e, string when)
        {
            switch (e.Kind)
            {
                case "dance": return "A dance at the saloon in " + e.Town + ", " + when + " from seven. A fiddler is promised. All welcome.";
                default: return e.Title + " in " + e.Town + ", " + when + ".";
            }
        }

        public static string PaperText(TownEvent e, string when)
        {
            switch (e.Kind)
            {
                case "dance": return "There is to be a dance at the saloon " + when + ", from seven in the evening. Ladies admitted free; gentlemen are asked to leave their arms at the bar.";
                default: return e.Title + ", " + when + ".";
            }
        }

        /// What a dance leaves when nobody was there to see it: two to four of the town's regulars come away warmer toward
        /// each other (pairs, a "smalltalk" tie each, or "flirt" between a man and a woman now and then), and one pair of
        /// sweethearts already has words over it (an "argue") one dance in three. The pairs are (a, b, kind).
        public static List<string[]> DanceAway(List<Resident> regulars, List<SceneTie> ties, string seed)
        {
            var pairs = new List<string[]>();
            if (regulars == null || regulars.Count < 2)
                return pairs;
            var pool = new List<Resident>();
            foreach (Resident r in regulars)
                if (r != null && !r.Dead && !r.Fled && r.Id != null && r.Name != null)
                    pool.Add(r);
            if (pool.Count < 2)
                return pairs;
            int n = Math.Min(pool.Count / 2, DanceLeast + Voice.Hash(seed + "|n") % (DanceMost - DanceLeast + 1));
            // a seeded shuffle, so the same dance pairs the same people
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Voice.Hash(seed + "|s" + i) % (i + 1);
                Resident t = pool[i]; pool[i] = pool[j]; pool[j] = t;
            }
            var used = new HashSet<string>();
            for (int i = 0; i + 1 < pool.Count && pairs.Count < n; i += 2)
            {
                Resident a = pool[i], b = pool[i + 1];
                if (used.Contains(a.Id) || used.Contains(b.Id))
                    continue;
                used.Add(a.Id);
                used.Add(b.Id);
                bool mixed = a.Woman != b.Woman;
                string kind = mixed && Voice.Hash(seed + "|k" + i) % 100 < 35 ? "flirt" : "smalltalk";
                pairs.Add(new[] { a.Id, a.Name, b.Id, b.Name, kind });
            }
            // a jealous pair: sweethearts who were there have words one dance in three
            if (ties != null && Voice.Hash(seed + "|j") % 3 == 0)
                foreach (SceneTie t in ties)
                    if (t != null && Ties.Sweethearts(t) && t.A != null && t.B != null && used.Contains(t.A) && !used.Contains(t.A + "|q"))
                    {
                        pairs.Add(new[] { t.A, t.AName, t.B, t.BName, "argue" });
                        used.Add(t.A + "|q");
                        break;
                    }
            return pairs;
        }

        /// The caller's shouts at the dance, for the captions.
        public static readonly string[] Calls =
        {
            "All join hands and circle left!", "Swing your partner, don't be shy!", "Ladies to the centre, gents around!",
            "Promenade, now, promenade!", "Bow to your partner, bow to the fiddler!", "One more round before the fiddler's dry!",
            "Grand right and left, and mind the lamp!", "Do-si-do, and home you go!",
        };

        public static string Call(string seed)
        {
            return Calls[Voice.Hash(seed) % Calls.Length];
        }

        /// A word in the log and the notice when it's over.
        public static string OverText(TownEvent e, int pairs, bool attended)
        {
            if (e.Kind == "dance")
                return attended ? "The dance in " + e.Town + " wound down past eleven. You were there." : "There was a dance in " + e.Town + " last night, they say.";
            return e.Title + " in " + e.Town + " is over.";
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (string c in Calls)
                yield return c;
            var e = Dance("Valentine", 10);
            yield return PostedText(e, "on Saturday");
            yield return PaperText(e, "on Saturday");
            yield return OverText(e, 2, true);
            yield return OverText(e, 2, false);
        }
    }
}
