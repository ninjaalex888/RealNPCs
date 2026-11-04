// A broken wagon wheel (SIXDAY item 26; roadmap 32), the probe first. Roadmap 32 asks what has to be worked out in game
// before the scene is built: how a wagon's wheel is broken and mended by the game's own vehicle damage, and whether a
// stopped wagon holds the road without jamming the traffic behind it. The game side (WestworldScript.Wagon.cs) runs a
// Testing row that breaks a wheel on an ambient wagon off screen, watches forty seconds, mends it, and watches twenty
// more; this file is the readings' bookkeeping and the report, pure, so the shape of the probe can be read and checked
// offline, and the scene's words for when the probe has answered (the driver down beside the wheel calling a passer-by,
// his thanks, his asking the next man along to send word to town). All words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// What the probe read, second by second, and what it found; the game side fills it, Report says it.
    public sealed class WagonProbe
    {
        public const int WatchMs = 40000, AfterMs = 20000, ReadEveryMs = 2000;   // the broken wheel watched 40 s, then mended and watched 20 s
        public int Wheel = -1;              // the wheel broken (0-3), -1 for none found whole
        public bool Broke;                  // the game says the wheel is destroyed after the call
        public int StoppedAfterMs = -1;     // when the wagon first stood still after the break (-1: never)
        public bool DriverDown;             // the driver got down off the box
        public bool DriverFled;             // ...or ran (fleeing or in combat)
        public int JamMost;                 // the most vehicles seen standing within 30 m behind it at once
        public bool Fixed;                  // the wheel reads whole again after SET_VEHICLE_FIXED
        public int MovedOnAfterMs = -1;     // when it moved again after the mend (-1: never)
        public float FarthestM;             // how far it got from where the wheel broke
        public int Reads;                   // readings taken
        public string Model;                // the wagon's model name
        public string EndedWhy;             // how the probe ended (done, the wagon gone, the player too far)

        /// A reading of the game's answers, taken every ReadEveryMs: speed, standing, the driver's state, the jam behind.
        public void Read(int sinceMs, bool afterMend, float speed, bool stopped, bool driverIn, bool driverFleeing, int standingBehind, float fromStart)
        {
            Reads++;
            if (fromStart > FarthestM)
                FarthestM = fromStart;
            if (standingBehind > JamMost)
                JamMost = standingBehind;
            if (!afterMend)
            {
                if (stopped && StoppedAfterMs < 0)
                    StoppedAfterMs = sinceMs;
                if (!driverIn)
                    DriverDown = true;
                if (driverFleeing)
                    DriverFled = true;
            }
            else if (speed > 0.5f && !stopped && MovedOnAfterMs < 0)
                MovedOnAfterMs = sinceMs;
        }

        /// One line for the log and F7, in words Lex can act on.
        public string Report()
        {
            if (Wheel < 0)
                return "no whole wheel to break";
            return "wheel " + Wheel + (Broke ? " broke" : " didn't break (the call did nothing)") +
                   "; " + (StoppedAfterMs < 0 ? "never stood still in " + WatchMs / 1000 + " s" : "stood still after " + StoppedAfterMs / 1000 + " s") +
                   "; the driver " + (DriverFled ? "ran" : DriverDown ? "got down" : "stayed on the box") +
                   "; " + (JamMost == 0 ? "nothing stood behind it" : JamMost + " behind it at a standstill at most") +
                   "; mended: " + (Fixed ? "the wheel reads whole" : "the wheel still reads broken") +
                   "; " + (MovedOnAfterMs < 0 ? "didn't move on in " + AfterMs / 1000 + " s" : "moved on " + MovedOnAfterMs / 1000 + " s after the mend") +
                   "; farthest " + (int)FarthestM + " m from the break; " + Reads + " readings" + (EndedWhy != null ? " (" + EndedWhy + ")" : "");
        }

        /// What the readings say the scene can be built on, for the roadmap's question.
        public string Verdict()
        {
            if (Wheel < 0 || !Broke)
                return "the wheel can't be broken this way: another native, or a prop wheel beside a stopped wagon";
            if (StoppedAfterMs < 0)
                return "the wagon rolls on with a wheel off: the scene needs the driver halted on purpose";
            if (JamMost >= 2)
                return "a stopped wagon jams the road: the scene must be off the road, or short";
            if (!Fixed)
                return "the mend doesn't take: the wagon would have to be let go rather than sent on";
            return "the scene can be built as the roadmap has it: a wheel off, a halt, a mend, the wagon on its way";
        }
    }

    public static class WagonWheel
    {
        public const int WheelsToTry = 4;

        /// What the driver calls to a passer-by from beside the wheel (the scene to come; roadmap 32).
        public static string DriverCall(bool family, string seed)
        {
            return Voice.Pick(seed + "|wc",
                "You there! Give a man a hand with this? The wheel's off and I can't lift her alone.",
                "Mister! A shoulder under this and we'll have it back on. Two minutes of your day.",
                family ? "Please. My wife and the little one are up there and the light's going. Help me set this wheel." : "Hold up a moment. This wheel. I'll pay you for the lifting.",
                family ? "Don't ride past. Not with the children on the bench. Just help me lift." : "Here's a thing: a wheel off and not a soul on the road for an hour. You'll help a man?",
                "The pin sheared. If you can hold her up I can get the wheel on. Please.");
        }

        /// His thanks when it's on.
        public static string DriverThanks(string seed)
        {
            return Voice.Pick(seed + "|wt",
                "There. She'll hold to town. I'm obliged to you, friend.",
                "You've kept me out of that ditch till morning. Here, for your trouble.",
                "That's got it. Mind how you go; I'll not forget the face that stopped.",
                "On and tight. I owe you a drink the next time we're in the same town.");
        }

        /// What he asks of one who won't stop to lift: send word to town.
        public static string DriverSendWord(string town, string seed)
        {
            string t = town ?? "the next town";
            return Voice.Pick(seed + "|ww",
                "If you won't stop, at least tell them at " + t + " there's a wagon down out here.",
                "Then send the smith out from " + t + ", would you? Tell him it's the back wheel.",
                "Fine. Ride on. But tell " + t + " I'm out here, before dark if you can.");
        }

        /// The thought of one who rode past.
        public static string PassedBy(string seed)
        {
            return Voice.Pick(seed + "|wp",
                "A wheel off and a man alone with it. I kept riding. I'll think about that tonight.",
                "Somebody will stop for him. Somebody always does. Not me, today.",
                "That could be me tomorrow, out there in the dust with a wheel off.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return DriverCall(i % 2 == 0, "s" + i);
                yield return DriverThanks("s" + i);
                yield return DriverSendWord("Valentine", "s" + i);
                yield return PassedBy("s" + i);
            }
        }
    }
}
