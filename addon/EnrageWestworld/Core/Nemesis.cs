// Nemesis memory past death (SIXDAY item 22; roadmap 35): a man the player kills keeps what he held against him before
// the killing. The worst wrong of the player's on him in the month before (the world log: a threat, a facing-down, a
// trouble turned the wrong way, a theft, a beating) is written onto the Fallen as `Held`, in his own first person, and
// the revenant who comes back says so (Hosts.RevenantHeld). Why it reads true: the dead who return in every story come
// back with the grievance, not the wound. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Nemesis
    {
        public const int HeldDays = 30;

        /// The wrong in the dead man's own words ("you robbed me"), or null for a kind that isn't one.
        public static string HeldWord(string kind)
        {
            switch (kind)
            {
                case "threat": return "you threatened me";
                case "faced": return "you faced me down in front of everybody";
                case "harm": return "you turned my trouble the wrong way, and on purpose";
                case "deed:assault": return "you beat me in the street";
                case "deed:robbery": case "deed:theft": case "deed:looting": return "you robbed me";
                case "deed:horsetheft": return "you took my horse";
                default:
                    if (kind != null && kind.StartsWith("deed:", StringComparison.Ordinal) && Minds.DeedGroup(kind.Substring(5)) == "deed:violence")
                        return "you did me violence";
                    return null;
            }
        }

        /// What this one held against the player before he died: the latest wrong of the player's on them within HeldDays
        /// (a killing itself isn't one), in their own words; null for none.
        public static string Held(List<Happening> log, string victimId, long now)
        {
            if (log == null || victimId == null)
                return null;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                Happening e = log[i];
                if (e == null || e.TargetId != victimId || e.DoerId != "player" || e.At > now || now - e.At > HeldDays * 1440L)
                    continue;
                if (e.Kind != null && (e.Kind.Contains("murder") || e.Kind.Contains("kill")))
                    continue;
                string w = HeldWord(e.Kind);
                if (w != null)
                    return w;
            }
            return null;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string k in new[] { "threat", "faced", "harm", "deed:assault", "deed:robbery", "deed:horsetheft", "deed:bankrobbery" })
            {
                string w = HeldWord(k);
                if (w != null)
                    yield return w;
            }
        }
    }
}
