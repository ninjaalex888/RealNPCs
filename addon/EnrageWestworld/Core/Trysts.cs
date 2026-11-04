// A night together, off screen (0.6.3, Lex): after a woman says yes to the player's flirting, flirting with her again
// within two minutes takes them somewhere quieter. The screen goes dark and an hour passes; nothing is shown. These are
// the words said before and what she thinks after, by who she is: one of the saloon's women, a single woman, or one
// married or spoken for. Widened (SIXDAY item 15; roadmap 26): where they go (the saloon's own stairs, her home, the
// hotel's back stairs, or a quiet place past the last lamp), a price at the saloon (two dollars as it comes, the house's
// rule), the morning after (her thought when he's still about the next day), a sweetheart who expects him back (a single
// woman after two nights: a word to his face if he's gone a few days, and she cools if he's gone a fortnight), and a
// husband who finds out (a married woman's man hears of it one time in three, two in three when the saloon saw, and comes
// to town as the kin do: Core/KinGrudges.cs). Why it reads true: a night in 1899 had a geography and a price, and a woman
// who'd been seen had a husband to reckon with. All lines original to this add-on.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    /// One woman's nights with the player, kept (Store.Trysts): the first and the last, how many, what was paid, and what
    /// the days after have said.
    [DataContract]
    public sealed class TrystRecord
    {
        [DataMember(EmitDefaultValue = false)] public string WomanId, Name, Town, Kind;
        [DataMember(EmitDefaultValue = false)] public long At, LastAt;
        [DataMember(EmitDefaultValue = false)] public int Count, PaidCents;
        [DataMember(EmitDefaultValue = false)] public long MorningAt, ExpectAt;   // the morning after's thought said; the sweetheart's word said (game minutes; 0 not yet)
        [DataMember(EmitDefaultValue = false)] public bool Cooled;                // a sweetheart who gave up waiting
    }

    public static class Trysts
    {
        public const int OfferMs = 120000;   // how long after her yes the second flirt still counts
        public const int Keep = 60;          // records kept (the oldest goes)
        public const int ExpectsFromDays = 3, CoolsAfterDays = 14, CoolsBy = 20;   // a sweetheart's patience, and what her cooling costs him
        public static readonly string[] HotelTowns = { "Valentine", "Rhodes", "Saint Denis", "Blackwater", "Strawberry", "Tumbleweed", "Annesburg", "Armadillo" };

        /// "saloon", "taken" or "single".
        public static string Kind(bool saloonGirl, bool spoken)
        {
            return saloonGirl ? "saloon" : spoken ? "taken" : "single";
        }

        /// Whether her yes to his flirting is a yes to the night too, in percent (Lex, 2026-10-26: greet her, then flirt,
        /// then ask; flirting cold does worse). greeted: 1 he greeted her first, -1 he didn't, 0 not known.
        public static int NightChance(string kind, int greeted)
        {
            switch (kind)
            {
                case "saloon": return greeted < 0 ? 80 : 100;
                case "taken": return greeted > 0 ? 70 : greeted < 0 ? 40 : 55;
                default: return greeted > 0 ? 85 : greeted < 0 ? 55 : 70;
            }
        }

        /// The day's roll for the night, the same all day for her.
        public static bool NightYes(string id, long day, int chance)
        {
            return (uint)Voice.Hash((id ?? "") + "|night|" + day) % 100u < chance;
        }

        /// Her no to the night, when she liked the flirting well enough: a cold approach hears about it.
        public static string NotTonight(string kind, int greeted, string seed)
        {
            if (greeted < 0)
                return Voice.Pick(seed + "tn", "You didn't so much as say good evening. Not tonight.", "Manners first, mister. Then we'll see.",
                    "A woman likes to be greeted before she's asked a thing like that.");
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "tn", "Not tonight. I'm spoken for till the piano stops.", "Another night, cowboy. Tonight I'm tired.");
                case "taken": return Voice.Pick(seed + "tn", "I can't. Lord knows I'd like to, but I can't.", "Not tonight. Not with him home.");
                default: return Voice.Pick(seed + "tn", "Slow down. I said I liked you, not that I'd follow you anywhere.", "Another time, maybe. Not tonight.");
            }
        }

        /// What the player says, asking.
        public static string Ask(string seed)
        {
            return Voice.Pick(seed + "ta", "Somewhere quieter, maybe?", "Walk with me a while. Away from all these eyes.", "Care to keep me company a spell?");
        }

        /// Her yes, by who she is.
        public static string Yes(string kind, string seed)
        {
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "ty", "Upstairs, then. Mind the third step, it creaks.", "Come on. I know a room with a lock on the door.");
                case "taken": return Voice.Pick(seed + "ty", "Not here. Somewhere nobody knows my face.", "Just this once. And you never saw me.");
                default: return Voice.Pick(seed + "ty", "Lead the way, before I think better of it.", "I was starting to think you'd never ask.");
            }
        }

        // ---- where they go (item 15) ----

        public static bool HasHotel(string town)
        {
            return town != null && Array.IndexOf(HotelTowns, town) >= 0;
        }

        /// Where the night is: the saloon's women upstairs when they're in a saloon (else the hotel, else a quiet place), a
        /// single woman's own home in her own town three times in five (else the hotel, else a quiet place), a married one
        /// the hotel's back stairs (never her home), and a quiet place past the last lamp where there's no hotel.
        public static string Where(string kind, bool inSaloon, bool herTown, bool hotel, string seed)
        {
            switch (kind)
            {
                case "saloon": return inSaloon ? "upstairs" : hotel ? "hotel" : "quiet";
                case "taken": return hotel ? "hotel" : "quiet";
                default: return herTown && Voice.Chance(seed + "|home", 60) ? "home" : hotel ? "hotel" : "quiet";
            }
        }

        /// Her yes with the place in it (null or upstairs: the old lines, which say the stairs).
        public static string Yes(string kind, string where, string seed)
        {
            switch (where)
            {
                case "home": return Voice.Pick(seed + "th", "My place is along the street. Quietly, now; the neighbours have ears.", "Come home with me. The lamp's low and the door has a bolt.");
                case "hotel": return kind == "taken" ? Voice.Pick(seed + "tt", "The hotel, by the back stairs. And you never saw me.", "The hotel. Separate stairs, and not a word to anyone.")
                                                     : Voice.Pick(seed + "th", "The hotel keeps a back room, and the clerk sleeps like the dead.", "The hotel, then. I'll go up first. Count to fifty.");
                case "quiet": return kind == "saloon" ? Voice.Pick(seed + "tq", "Not here, then. There's a wagon back of the livery nobody minds.", "Out past the last lamp. Bring the bottle.")
                                                      : Voice.Pick(seed + "tq", "Out past the last house. Nobody walks there after dark.", "Walk with me to the water. Nobody goes there at night.");
                default: return Yes(kind, seed);
            }
        }

        // ---- the price at the saloon ----

        private static string Dollars(int d)
        {
            return d == 1 ? "one dollar" : Voice.Number(d) + " dollars";
        }

        private static string FirstUp(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// The saloon's price, said before her yes.
        public static string PriceLine(int dollars, string seed)
        {
            return Voice.Pick(seed + "tp", FirstUp(Dollars(dollars)) + ", cowboy. Nothing's free in this house.", "It's " + Dollars(dollars) + " up front. The house's rule, not mine.");
        }

        /// When he hasn't got it.
        public static string CantPay(int dollars, string seed)
        {
            return Voice.Pick(seed + "tc", "Not for free, honey. Come back when you've been paid.", "That's " + Dollars(dollars) + " and you haven't got it. Buy me a drink and we'll call it a visit.");
        }

        // ---- the days after ----

        /// What she thinks the next day when he's still about (count: how many nights, this one included).
        public static string Morning(string kind, int count, string seed)
        {
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "tm", "Him again. He paid and he was gentle. Most manage one of the two.", "Morning. The room's the same price, if he's wondering.");
                case "taken": return Voice.Pick(seed + "tm", "If he so much as nods at me in the street I'll die on the spot.", "Supper was on the table by six. He doesn't know. He can't.");
                default: return count >= 2 ? Voice.Pick(seed + "tm", "He's still in town. Good. I think good.", "Twice now. Mother would say that makes it something. Mother's not here.")
                                           : Voice.Pick(seed + "tm", "He stayed in town. That's either very good or very bad.", "I keep looking at the door. Stop it.");
            }
        }

        /// A sweetheart's word to his face when he's been gone a few days (after two nights).
        public static string Expects(string seed)
        {
            return Voice.Pick(seed + "te", "You said you'd come by. That was days ago, mister.", "There you are. I'd stopped looking. Nearly.", "Two visits and then nothing. I'm not a hotel room, you know.");
        }

        /// What she thinks once a fortnight has gone by without him.
        public static string Cooled(string seed)
        {
            return Voice.Pick(seed + "tk", "He never came back. They don't. I knew that and I did it anyway.", "Two weeks. Well. I've been a fool before and lived.", "If he rides in now I'll nod like a stranger. I've practised.");
        }

        /// The husband's chance of hearing, in percent: one in three, two in three when the saloon saw.
        public static int HusbandChance(bool seen)
        {
            return seen ? 60 : 30;
        }

        /// What she thinks after, on the ticker over her head.
        public static string After(string kind, string seed)
        {
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "tf", "He never said a word about his troubles. They all have troubles.", "Gentle, for a man with that many scars.");
                case "taken": return Voice.Pick(seed + "tf", "Supper on the table by six. Nobody will ever know. Nobody.", "What have I done. Lord, what have I done.");
                default: return Voice.Pick(seed + "tf", "I'll be smiling at nothing all day.", "Mother would faint. Mother isn't here.");
            }
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (string k in new[] { "saloon", "taken", "single" })
                for (int i = 0; i < 12; i++)
                {
                    yield return Yes(k, "s" + i);
                    yield return After(k, "s" + i);
                    yield return NotTonight(k, 1, "s" + i);
                    yield return NotTonight(k, -1, "s" + i);
                    foreach (string w in new[] { "home", "hotel", "quiet" })
                        yield return Yes(k, w, "s" + i);
                    yield return Morning(k, 1, "s" + i);
                    yield return Morning(k, 2, "s" + i);
                }
            for (int i = 0; i < 12; i++)
            {
                yield return Ask("s" + i);
                yield return PriceLine(1, "s" + i);
                yield return PriceLine(2, "s" + i);
                yield return PriceLine(5, "s" + i);
                yield return CantPay(2, "s" + i);
                yield return Expects("s" + i);
                yield return Cooled("s" + i);
            }
        }
    }
}
