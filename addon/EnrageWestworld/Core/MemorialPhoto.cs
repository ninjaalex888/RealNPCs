// Memorial photographs (FIVEDAY reserve R11). In 1899 a family that had never sat for a likeness often had one made after a
// death: the dead posed among the living, eyes painted on the plate, a hand on a Bible, the child in her mother's arms as
// if asleep. The photographer at his counter (Posts' "photographer" kind: Saint Denis and Blackwater) thinks of the sittings
// of the last ten days, his town's dead by name (a strong candidate among his thoughts); and a mourner in those towns
// thinks, now and then, of the photograph that came (in place of their mourning thought, a time in three). Behind [Mind]
// MemorialPhotos.
//
// Why it reads true: the memorial photograph was often the only image a poor family ever had of its dead, and keeping the
// dead present (continuing bonds) is how grief settles rather than ends; the photographer, like the undertaker, sees the
// town's losses up close and has to make them look peaceful. Plain C#, no game; all words original. mindtest
// MemorialPhotoTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class MemorialPhoto
    {
        public const string Kind = "photographer";
        public const int Days = 10, Pull = 42, MournerPercent = 33;
        public static readonly string[] Towns = { "Saint Denis", "Blackwater" };

        public static bool HasPhotographer(string town) { return town != null && Towns.Contains(town); }

        private static readonly string[] Sittings = {
            "Sat {n} up among the family for the plate. A brace behind the back. You'd never know.",
            "Painted eyes on {n}'s portrait, so the mother could bear to look at it.",
            "{n}'s widow wanted his hand on the Bible. Took three tries to get the fingers right.",
            "Long exposure. The living blurred and {n} sat perfectly still. They always do.",
            "Printed {n}'s likeness twice. One for the mantel, one for the coffin." };
        private static readonly string[] Busy = { "Three sittings of the dead this week. The living are scarce in my studio.", "More plates of the dead than the living this month. It's the season for it." };
        private static readonly string[] Mourners = {
            "The photograph came. {n} looks asleep. Only asleep.", "Put {n}'s likeness on the mantel. Can't look at it. Can't put it away.",
            "First likeness we ever had of {n}, and it's this one.", "Showed the photograph to the neighbours. They said {n} looked peaceful. Liars, kind ones." };

        /// The photographer's thought of the dead he's photographed lately (newest first), or null in a quiet spell.
        public static string Thought(IList<DeathEntry> dead, string seed)
        {
            if (dead == null || dead.Count == 0)
                return null;
            if (dead.Count >= 3 && Voice.Chance(seed + "|mb", 35))
                return Voice.Pick(seed + "|mbp", Busy);
            DeathEntry pick = dead[(int)((Voice.Hash(seed + "|mp") & 0x7fffffff) % dead.Count)];
            return Voice.Pick(seed + "|ms", Sittings).Replace("{n}", Person.FirstOf(pick.Name) ?? "the dead man");
        }

        /// A mourner's thought of the photograph (a time in three, in a town with a photographer), or null.
        public static string MournerThought(string town, string dead, string seed)
        {
            if (!HasPhotographer(town) || dead == null || !Voice.Chance(seed + "|mm", MournerPercent))
                return null;
            return Voice.Pick(seed + "|mmp", Mourners).Replace("{n}", dead);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Sittings.Concat(Mourners)) yield return l.Replace("{n}", "Abel");
            foreach (string l in Busy) yield return l;
        }
    }
}
