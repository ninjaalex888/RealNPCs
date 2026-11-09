// Mourning and graves visited (FIVEDAY item 26). A friend's death already lived in the mind for three weeks as a memory
// (Minds: "death", Sub "friend") and spoke in thoughts. Now it's mourning:
// - their days go quieter for those three weeks: home, prayer, the church, and not the cards, the saloon or showing off;
// - their own thoughts of the dead come once a day when you're about;
// - a neighbour near them lowers their voice for it (a thought);
// - on a Sunday afternoon, one in mourning walks to the graveyard (the town's "cemetery" spot, else by the church) and
//   kneels at the grave (the game's graveside kneel). Sometimes it's the grave of one you killed. Come near then, and they
//   know you.
// Mourning dress is left out (outfits are held back). Behind [World] Mourning.
//
// Why it reads true: in 1899 mourning was kept by rule and by feeling. The bereaved withdrew from amusements for weeks,
// neighbours hushed around them, and Sunday afternoons were for the graveyard, where families tended the plots. Plain
// C#, no game; all words original. mindtest MourningTest.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Mourning
    {
        public const int Days = 21;
        public const int GraveFrom = 13, GraveTo = 17;   // a Sunday afternoon

        /// The friend they mourn (the latest death within three weeks that word of has reached them): their name and id, or false.
        public static bool MournOf(List<MindMemory> mind, long now, out string deadName, out string deadId)
        {
            deadName = deadId = null;
            if (mind == null)
                return false;
            long best = -1;
            foreach (MindMemory m in mind)
            {
                if (m == null || m.Kind != "death" || m.Sub != "friend")
                    continue;
                long at = Math.Max(m.At, m.DueAt);
                if (at > now || now - at >= Days * 1440L || at <= best)
                    continue;
                best = at;
                deadId = m.Who != null && m.Who.Count > 0 ? m.Who[0] : null;
                deadName = m.Names != null && m.Names.Count > 0 ? m.Names[0] : null;
            }
            return best >= 0;
        }

        /// How mourning leans a choice (Choosing's act ids).
        public static float Lean(bool mourning, string act)
        {
            if (!mourning)
                return 0f;
            return act == "home" ? 0.12f : act == "pray" ? 0.12f : act == "church" ? 0.06f : act == "showoff" ? -0.25f : act == "cards" ? -0.2f : act == "saloon" ? -0.12f : act == "greet" ? -0.08f : 0f;
        }

        public static bool GraveHour(int weekday, int hour) { return weekday == 0 && hour >= GraveFrom && hour < GraveTo; }

        /// All Saints' Day in the south (TENHOUR R8; roadmap.md's idea bank): families at the tombs from morning to evening, the first of November.
        public static bool AllSaints(int month, int day, string town, int hour) { return month == 11 && day == 1 && town != null && Calendar.Region(town) == "south" && hour >= 8 && hour < 18; }

        private static readonly string[] Own =
        {
            "{d}'s chair at the table. Nobody sits in it.", "I keep turning to tell {d} something.", "Three weeks, they say, and it gets easier. They lie.",
            "Folks mean well. I wish they'd stop asking.", "I set out two cups again this morning. Two.", "The house is so quiet without {d}.",
        };
        private static readonly string[] Hush = { "Keep your voice down. {m}'s still grieving {d}.", "Not so loud. {m} lost {d}, remember.", "Mind your tongue near {m}. It's too soon after {d}." };
        private static readonly string[] AtGrave = { "I brought flowers, {d}. Wild ones. You'd laugh.", "The grass is coming in over you already, {d}.", "Sunday again, {d}. I'm here.", "They put your name on straight, {d}. I checked." };
        private static readonly string[] AtGraveYours = { "Here's where he put you, {d}. Here.", "I'll not forget whose doing this is, {d}.", "He walks the street, {d}, and you lie here." };
        private static readonly string[] SeeYou = { "You. You dare come here.", "That's him. At your grave, {d}. God help me.", "Get away from here. You've done enough." };

        private static string Fill(string l, string dead, string mourner) { return l.Replace("{d}", dead ?? "him").Replace("{m}", mourner ?? "them"); }

        public static string OwnThought(string dead, string seed) { return Fill(Voice.Pick(seed + "|mo", Own), dead, null); }
        public static string HushThought(string mourner, string dead, string seed) { return Fill(Voice.Pick(seed + "|hu", Hush), dead, mourner); }
        public static string GraveThought(string dead, bool byYou, string seed) { return Fill(Voice.Pick(seed + "|gr", byYou ? AtGraveYours : AtGrave), dead, null); }
        public static string SeeYouThought(string dead, string seed) { return Fill(Voice.Pick(seed + "|sy", SeeYou), dead, null); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in new[] { Own, Hush, AtGrave, AtGraveYours, SeeYou })
                foreach (string l in p)
                    yield return Fill(l, "Elihu", "Ruth");
        }
    }
}
