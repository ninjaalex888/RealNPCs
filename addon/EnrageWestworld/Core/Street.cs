// The street reacts to other people's fights (SIXDAY item 5b; roadmap 17: "the whole street reacts to a fight, a standoff
// or a death between other people, the way it does to yours"). The game has its own crowd: its people notice "shocking
// events" (a brawl, a confrontation, a killing, a body) and look, back off, run or call for the law by their own rules.
// RealNPCs' scenes never raised one, so the street around two men at blows only turned its head (the Onlookers look-at,
// which stays as the fallback). This names the game's event for each moment of a scene, once per scene per moment, and
// the rules that keep it from running away with the town: a guard that holds the events in a town for a while when one
// brings the law into combat, a gun out of someone else's holster or people fleeing by the player, and a look back three
// seconds on at what the street did (the log, for tuning: the natives' trailing arguments are unnamed, so what each try
// does is written down). Plain C#, no game; the natives are the game side's.
using System;

namespace EnrageWestworld.Core
{
    public static class Street
    {
        public const float Metres = 20f;        // the crowd counted within this of the event
        public const int LookBackMs = 3000;     // how long after the event the street is looked at
        public const int GuardMs = 10000;       // the escalation watch after each event
        public const int HoldMinutes = 30;      // game minutes without events in a town after an escalation
        public const int Keep = 5;              // the last events kept for F7
        public const float Duration = 10f;      // what the event is given for its length, in seconds (the first unnamed argument)

        public static readonly string[] Moments = { "blows", "square", "aim", "killed", "body", "knockedout" };

        /// The game's own event for a moment of a scene, by name (the game side hashes it): blows in a saloon are a bar brawl,
        /// elsewhere a melee fight; two men squaring up a confrontation; a gun out a weapon threat; a killing; the body after;
        /// a man knocked out. Null for anything else.
        public static string EventFor(string moment, bool saloon)
        {
            switch (moment)
            {
                case "blows": return saloon ? "EVENT_SHOCKING_BAR_BRAWL_ACTION" : "EVENT_SHOCKING_MELEE_FIGHT";
                case "square": return "EVENT_SHOCKING_SEEN_CONFRONTATION";
                case "aim": return "EVENT_SHOCKING_SEEN_WEAPON_THREAT";
                case "killed": return "EVENT_SHOCKING_SEEN_PED_KILLED";
                case "body": return "EVENT_SHOCKING_DEAD_BODY";
                case "knockedout": return "EVENT_SHOCKING_PED_KNOCKED_OUT";
                default: return null;
            }
        }

        /// Once per scene per moment.
        public static string OnceKey(string sceneSeed, string moment)
        {
            return (sceneSeed ?? "?") + "|" + moment;
        }

        /// Why no event may be raised now, or null: a mission, the law after the player, one of enRAGE's fighters near (its own
        /// fights are its own), the town held after an escalation.
        public static string WhyNot(bool mission, bool wanted, bool enrageNear, bool held)
        {
            if (mission) return "a mission";
            if (wanted) return "the law has your name just now";
            if (enrageNear) return "one of enRAGE's fighters is near";
            if (held) return "the town is held after an escalation";
            return null;
        }

        /// What the guard saw, in words, or null when nothing escalated.
        public static string Escalation(bool lawCombat, bool gunDrawn, bool fleeingNearYou)
        {
            if (lawCombat) return "a lawman in combat";
            if (gunDrawn) return "a gun drawn by someone outside the scene";
            if (fleeingNearYou) return "people fleeing beside you";
            return null;
        }

        /// The look back, for the log: how many stood within Metres, and what they did three seconds on.
        public static string LookBack(int within, int looking, int fleeing, int fighting)
        {
            return within + " within " + (int)Metres + " m; " + (LookBackMs / 1000) + " s on: " + looking + " looking, " + fleeing + " fleeing, " + fighting + " fighting" +
                   (within > 0 && looking + fleeing + fighting == 0 ? " (the event didn't take: the look-at fallback stands)" : "");
        }
    }
}
