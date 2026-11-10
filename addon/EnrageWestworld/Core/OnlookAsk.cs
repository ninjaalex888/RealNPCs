// The model's word for an onlooker (THREEHOUR reserve R1). What someone who only watched trouble does about it has been the
// rules' (Core/Onlooking.cs ActFor: the timid walk off, contempt shakes its head, a hard interest watches...), and how a crowd takes
// a fight as a whole is the model's already (FIVEDAY2 R10, GroupActs.CrowdRequest). This puts the strongest onlooker's own act to
// the model too, in the two and a half seconds the onlookers wait anyway ("the trouble shows first", OnlookDelayMs): a short list
// of what the game is proven able to play (the rules' own act first, then the others that suit), the model picks, and if it's late
// or unreadable the rules' act stands. Only for a fight or a quarrel, and only the first in line. With a thought, held to the
// language check like any other. Behind [Mind] OnlookerPicks (with the model's picks on).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class OnlookAsk
    {
        public const int WaitMs = 1500;   // after the onlookers' own wait: the answer this late at most

        /// Asked for these: a fight or a quarrel between others (never what the player did: that's the rules', and the witnesses').
        public static bool AskedFor(string kind)
        {
            return kind == "scene:brawl" || kind == "scene:rough" || kind == "scene:argue" || kind == "scene:x:quarrel" || kind == "scene:standoff";
        }

        /// The list: the rules' act first, then the others that are always fit to play, four at most, no two alike.
        public static List<string> Menu(string rulesAct, bool saloon)
        {
            var m = new List<string>();
            if (rulesAct != null && System.Array.IndexOf(Onlooking.Acts, rulesAct) >= 0)
                m.Add(rulesAct);
            foreach (string a in saloon ? new[] { "watch", "dismiss", "leave" } : new[] { "watch", "leave", "disapprove" })
                if (!m.Contains(a) && m.Count < Language.MostOptions)
                    m.Add(a);
            return m;
        }

        public static string Words(string act)
        {
            switch (act)
            {
                case "leave": return "walk away from it";
                case "hurry": return "get away from it, quick";
                case "slip": return "slip off quietly before anyone notices";
                case "freeze": return "stand rooted to the spot";
                case "dismiss": return "shake their head at it and look away";
                case "disapprove": return "make their disapproval plain and leave";
                case "bristle": return "square up, ready if it comes their way";
                case "watch": return "stop and watch it";
                default: return act;
            }
        }

        public static string WhatWords(string kind)
        {
            switch (kind)
            {
                case "scene:brawl": return "two men are fighting with their fists";
                case "scene:rough": return "one man is roughing up another";
                case "scene:standoff": return "two men are squared off, hands near their guns";
                default: return "two people are quarrelling, loud";
            }
        }

        public static LanguageRequest Request(string name, bool woman, string trade, string town, string place, int hour, int year, string mood, string kind, List<string> menu)
        {
            var r = new LanguageRequest
            {
                Kind = "pick", WithThought = true, Name = name, Woman = woman, Trade = trade, Town = town, Place = place, Hour = hour, Year = year,
                Mood = mood ?? "unsettled", Now = "close by, " + WhatWords(kind) + "; they're only watching"
            };
            r.Options = menu.ConvertAll(Words);
            return r;
        }
    }
}
