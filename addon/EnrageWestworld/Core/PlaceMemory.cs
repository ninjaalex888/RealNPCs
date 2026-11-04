// Memory that behaves like memory (SIXDAY item 24; roadmap 4): places hold memories, and people know you by more than
// your face. Three small things on what's saved already, all pure:
//   the ground: where you killed someone (the death ledger keeps the spot and whose it was), a person who knew the dead
//     (a warm tie, or word of the death reached them) thinks of it when they're near the spot, once; whether they know
//     it was you follows what the town can know (the ledger's "by player" with the killing seen). Why it reads true: a
//     place is a retrieval cue (context-dependent memory: what was felt somewhere comes back there), and grief is the
//     memory that doesn't blur: a friend's ground pulls for good, an acquaintance's for a month, then the spot is only a
//     spot again.
//   the horse: people who have seen you on your horse know the horse before the face. A rider is told by his mount from
//     farther off than a face can be made out (the horse is the bigger, steadier cue, and a man rides the same one for
//     years): once a day, a known person who sees you ride up on it thinks so.
//   habits: the town learns your routine. The same place at about the same hour on three different days is a habit, and
//     a known person there thinks of it once ("you could set a watch by him"): a stranger becomes a regular by his
//     pattern, not his name; people read each other by what repeats.
// No game here; the words original to this add-on.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    /// A routine of the player's the town has noticed: a place at an hour, on how many different days.
    [DataContract]
    public sealed class Habit
    {
        [DataMember(EmitDefaultValue = false)] public string Town, Place;   // the town; the place kind (saloon, genstore, doctor...)
        [DataMember(EmitDefaultValue = false)] public int Hour;             // the clock hour it was first seen at (the later ones within an hour of it)
        [DataMember(EmitDefaultValue = false)] public int Days;             // different days seen there about then
        [DataMember(EmitDefaultValue = false)] public long FirstDay, LastDay;   // game days (minutes / 1440)
    }

    public static class PlaceMemory
    {
        public const float GroundMetres = 25f;      // the thinker this near the spot
        public const int AcquaintanceDays = 30;     // an acquaintance's ground pulls this long; a friend's for good
        public const int SettleMinutes = 180;       // word of a death needs three game hours to get about (Knowing.cs); before that nobody "knows" it at the spot
        public const int HabitDays = 3;             // different days at a place about the same hour make a habit
        public const int HabitsKept = 40;
        public const float HorseFar = 18f, HorseFarMost = 45f;   // known by the horse from here out, as far as this
        public const float HorseLearnMetres = 15f;  // a known person this near while you're mounted learns the horse

        // ---- the ground ---------------------------------------------------------------------------------------------

        /// The nearest death the player caused within `metres` of a spot, with the dead's own id and a spot recorded; null for none.
        public static DeathEntry NearestGround(List<DeathEntry> deaths, float x, float y, float z, float metres)
        {
            DeathEntry best = null;
            float bestD = metres;
            if (deaths == null)
                return null;
            for (int i = 0; i < deaths.Count; i++)
            {
                DeathEntry e = deaths[i];
                if (e == null || e.Id == null || e.By != "player" || (e.X == 0f && e.Y == 0f && e.Z == 0f))
                    continue;
                float dx = e.X - x, dy = e.Y - y, dz = e.Z - z;
                float d = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
                if (d < bestD)
                {
                    bestD = d;
                    best = e;
                }
            }
            return best;
        }

        /// They knew the dead: a tie between them that wasn't hostile (met, talked, drank together). The man he fought
        /// knew him too, but the spot is nothing to him.
        public static bool Knew(string id, string deadId, List<SceneTie> ties)
        {
            return TieWarmth(id, deadId, ties) >= 0;
        }

        /// A friend of the dead: a warm tie. Their ground pulls for good.
        public static bool Friend(string id, string deadId, List<SceneTie> ties)
        {
            return TieWarmth(id, deadId, ties) > 0;
        }

        /// The warmth of the tie between them, or -99 for none.
        private static int TieWarmth(string id, string deadId, List<SceneTie> ties)
        {
            if (id == null || deadId == null || ties == null || id == deadId)
                return -99;
            for (int i = 0; i < ties.Count; i++)
            {
                SceneTie t = ties[i];
                if (t != null && t.Has(id) && t.Has(deadId))
                    return t.Warmth;
            }
            return -99;
        }

        /// Whether the spot still pulls: a friend's for good (grief doesn't blur), anyone else's for a month; and not before
        /// word of the death could have got about.
        public static bool Pulls(DeathEntry e, bool friend, long now)
        {
            if (e == null || now - e.At < SettleMinutes)
                return false;
            return friend || now - e.At < AcquaintanceDays * 1440L;
        }

        public static string GroundMark(DeathEntry e)
        {
            return "ground:" + e.Id + ":" + e.At;
        }

        /// What one who knew the dead thinks at the spot. knowsYou: the town can say it was the player (the killing was seen).
        public static string GroundLine(string deadFirst, bool knowsYou, bool friend, string seed)
        {
            string d = deadFirst ?? "him";
            if (knowsYou)
                return Voice.Pick(seed + "|gk",
                    "Right here. This is where he put " + d + " in the dirt. I still cross the street for it.",
                    "This is the spot. " + d + " went down right about where that man's standing.",
                    "I don't walk past here without seeing " + d + " on the ground. And him standing over it.",
                    "Here. " + d + " died here, and the one who did it rides through like it's any other road.",
                    "They scrubbed the boards. You can still see where " + d + " lay, if you knew him.",
                    friend ? "I was meant to meet " + d + " that day. Right here. I was late." : "Somebody ought to have said something, that day. Nobody did.");
            return Voice.Pick(seed + "|gu",
                "This is where they found " + d + ". Whoever did it is still walking around.",
                "I can't pass this spot. " + d + ", lying there, and not a soul to say how.",
                "Here's where " + d + " died. Nobody ever said who.",
                "The same stretch of ground. You'd think it would look different, after " + d + ".",
                friend ? "I still come by here. Don't know why. " + d + " isn't here." : "That's where " + d + " was lying. I remember the hat first.",
                "Somebody knows who did for " + d + ". Somebody always knows.");
        }

        // ---- the horse ----------------------------------------------------------------------------------------------

        public static string HorseMark(long day)
        {
            return "horse:" + day;
        }

        /// A known person seeing you ride up on the horse they've seen you on: the horse first, then the man. surname: how
        /// they'd name you, or null when they don't have your name.
        public static string HorseLine(string surname, string seed)
        {
            if (surname != null)
                return Voice.Pick(seed + "|hn",
                    "That's " + surname + "'s horse. Knew the gait before I knew the hat.",
                    "I'd know that horse before the man on it. " + surname + ", then.",
                    "Same horse as last time. So that's " + surname + " coming in.",
                    "The horse I know. " + surname + " I'm still making my mind up about.",
                    "Here comes that horse again, and " + surname + " on it, same as ever.");
            return Voice.Pick(seed + "|hu",
                "I know that horse. The fellow from the other day, then.",
                "Same horse as before. I'd know it anywhere; the rider I only half remember.",
                "That horse again. The man on it was through here not long ago.",
                "I'd know that animal's walk before I'd know his face.",
                "The horse first, the rider second. It's him again.");
        }

        // ---- habits ------------------------------------------------------------------------------------------------

        private static int HoursApart(int a, int b)
        {
            int d = Math.Abs(a - b);
            return Math.Min(d, 24 - d);
        }

        /// The player came into a place: the habit it belongs to (the same town and place within an hour of its hour) counts
        /// the day, or a new one begins. `formed` is true the moment the third different day makes it a habit.
        public static Habit Note(List<Habit> all, string town, string place, int hour, long day, out bool formed)
        {
            formed = false;
            if (all == null || town == null || place == null)
                return null;
            Habit h = Find(all, town, place, hour);
            if (h == null)
            {
                h = new Habit { Town = town, Place = place, Hour = hour, Days = 1, FirstDay = day, LastDay = day };
                all.Add(h);
                while (all.Count > HabitsKept)
                {
                    int oldest = 0;
                    for (int i = 1; i < all.Count; i++)
                        if (all[i].LastDay < all[oldest].LastDay)
                            oldest = i;
                    all.RemoveAt(oldest);
                }
                return h;
            }
            if (h.LastDay != day)
            {
                h.Days++;
                h.LastDay = day;
                formed = h.Days == HabitDays;
            }
            return h;
        }

        private static Habit Find(List<Habit> all, string town, string place, int hour)
        {
            for (int i = 0; i < all.Count; i++)
            {
                Habit h = all[i];
                if (h != null && h.Town == town && h.Place == place && HoursApart(h.Hour, hour) <= 1)
                    return h;
            }
            return null;
        }

        /// The habit the town has of you here, now: one formed (HabitDays) at this place about this hour; else null.
        public static Habit Formed(List<Habit> all, string town, string place, int hour)
        {
            if (all == null || town == null || place == null)
                return null;
            Habit h = Find(all, town, place, hour);
            return h != null && h.Days >= HabitDays ? h : null;
        }

        public static string HabitMark(Habit h)
        {
            return "habit:" + h.Town + "|" + h.Place + "|" + h.Hour;
        }

        public static string PlaceWords(string place)
        {
            switch (place)
            {
                case "saloon": return "the bar";
                case "genstore": return "the store";
                case "gunsmith": return "the gunsmith's";
                case "bank": return "the bank";
                case "sheriff": return "the sheriff's";
                case "doctor": return "the doctor's";
                case "barber": return "the barber's";
                default: return "this place";
            }
        }

        public static string HourWords(int hour)
        {
            if (hour == 12) return "noon";
            if (hour >= 22 || hour < 5) return "this time of night";
            if (hour < 12) return "this time of the morning";
            if (hour < 18) return "this time of the afternoon";
            return "this time of the evening";
        }

        /// What a known person thinks seeing you keep to your hour. surname: how they'd name you, or null.
        public static string HabitLine(string surname, string place, int hour, string seed)
        {
            string you = surname ?? "that one", where = PlaceWords(place), when = HourWords(hour);
            return Voice.Pick(seed + "|hb",
                "Same hour, same door. You could set a watch by " + you + ".",
                you + " again, at " + where + ", at " + when + ". Regular as the stage.",
                "Three days running I've seen " + you + " walk into " + where + " about " + when + ".",
                "A man's habits tell you more than his name. " + you + " keeps " + when + " for " + where + ".",
                "There's " + you + ". Must be " + when + ", then.",
                "I could tell the hour by " + you + " coming through that door.");
        }

        /// For the linetools.
        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return GroundLine("Amos", true, i % 2 == 0, "s" + i);
                yield return GroundLine("Amos", false, i % 2 == 0, "s" + i);
                yield return HorseLine("Morgan", "s" + i);
                yield return HorseLine(null, "s" + i);
                yield return HabitLine("Morgan", "saloon", 20, "s" + i);
                yield return HabitLine(null, "genstore", 9, "s" + i);
            }
        }
    }
}
