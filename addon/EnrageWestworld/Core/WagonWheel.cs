// A broken wagon wheel (SIXDAY item 26; roadmap 32), the probe first. Roadmap 32 asks what has to be worked out in game
// before the scene is built: how a wagon's wheel is broken and mended by the game's own vehicle damage, and whether a
// stopped wagon holds the road without jamming the traffic behind it. The game side (WestworldScript.Wagon.cs) runs a
// Testing row that breaks a wheel on an ambient wagon off screen, watches forty seconds, mends it, and watches twenty
// more; this file is the readings' bookkeeping and the report, pure, so the shape of the probe can be read and checked
// offline, and the scene's words for when the probe has answered (the driver down beside the wheel calling a passer-by,
// his thanks, his asking the next man along to send word to town). All words original to this add-on.
// The first run (Lex, 2026-11-04 09:36, a wagon moving 10 m off): the draft-wheel native alone did nothing in sixty
// seconds (the wheel read whole throughout, the wagon rolled on at 2.5 m/s). So the row now tries one way a press, round
// the list (Ways): the draft-wheel native with a heavy force after the can-break flags, the plain break-off-wheel native,
// the wagon taken as a mission entity first, damage applied at the wheel's own bone, and the body health set as a check
// that natives take on this wagon at all; a way that reads nothing broken in twelve seconds ends there.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// What the probe read, second by second, and what it found; the game side fills it, Report says it.
    public sealed class WagonProbe
    {
        public const int WatchMs = 40000, AfterMs = 20000, ReadEveryMs = 2000;   // the broken wheel watched 40 s, then mended and watched 20 s
        public const int NothingMs = 12000;   // nothing read broken this long after the call: this way did nothing, the probe ends
        public string Way;                  // which way was tried (WagonWheel.Ways)
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
        public string EndedWhy;             // how the probe ended (done, the wagon gone, the player too far, nothing in 12 s)

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
            string way = Way != null ? "way \"" + Way + "\": " : "";
            if (!Broke)
                return way + "wheel " + Wheel + " didn't break (nothing read broken in " + NothingMs / 1000 + " s); " + Reads + " readings" + (EndedWhy != null ? " (" + EndedWhy + ")" : "");
            return way + "wheel " + Wheel + " broke" +
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
            if (Wheel < 0)
                return "no whole wheel to break: nothing learned";
            if (!Broke)
                return "this way can't break the wheel";
            if (StoppedAfterMs < 0)
                return "the wheel breaks but the wagon rolls on: the scene needs the driver halted on purpose";
            if (JamMost >= 2)
                return "the wheel breaks and the wagon stops, but it jams the road: the scene must be off the road, or short";
            if (!Fixed)
                return "the wheel breaks and the wagon stops, but the mend doesn't take: the wagon would have to be let go rather than sent on";
            return "the scene can be built as the roadmap has it: a wheel off, a halt, a mend, the wagon on its way";
        }
    }

    public static class WagonWheel
    {
        public const int WheelsToTry = 4;
        // the scene ([Encounters] WagonWheel), built on the probe's answer of 2026-11-04 10:38 (the plain break-off-wheel native)
        public const float NearMetres = 40f, FarMetres = 150f;   // the wagon ahead, out of view, this far off
        public const float CallMetres = 25f;      // the driver calls to you from here
        public const float HelpMetres = 3f;       // on foot this near the wagon, still...
        public const int HelpStillMs = 3000;      // ...this long, and you've stopped to help
        public const int LiftMs = 6000;           // the lift and the wheel going on
        public const float PassMetres = 40f;      // called to and this far gone: you rode on
        public const int WordMinutes = 30;        // reach a town within this and the word is sent
        public const float GoneMetres = 220f;     // this far from the wagon before you've dealt with it: over
        public const int DriverDownMs = 25000;    // the driver still on the box this long after the stop: asked down
        public const int MostMs = 600000;         // ten minutes and it's over whatever happened

        /// By day: wagons break down between six and eight.
        public static bool Hour(int clock)
        {
            return clock >= 6 && clock < 20;
        }

        /// Whether the wagon is ahead of you: the way you face against the way to it (the flat dot of the two, over a fifth).
        public static bool Ahead(float fx, float fy, float dx, float dy)
        {
            double fl = Math.Sqrt(fx * fx + fy * fy), dl = Math.Sqrt(dx * dx + dy * dy);
            if (fl < 0.01 || dl < 0.01)
                return false;
            return (fx * dx + fy * dy) / (fl * dl) > 0.2;
        }

        /// What he says as you put your shoulder to it.
        public static string LiftWords(string seed)
        {
            return Voice.Pick(seed + "|wl", "Now. Up. Up, and hold her there.", "That's it. Hold her. I've got the wheel.", "Together, on three. One. Two. Up.");
        }

        /// The notice when you've told a town about the wagon out on the road.
        public static string WordSent(string town, string seed)
        {
            string t = town ?? "town";
            return Voice.Pick(seed + "|ws", "You told them in " + t + " about the wagon down on the road. The smith is saddling up.",
                "Word of the wagon reached " + t + " with you. Somebody will ride out to it.");
        }

        /// The ways the row tries, one a press, round the list.
        public static readonly string[] Ways = { "draft", "wheel", "mission", "damage", "health" };

        public static string NextWay(string way)
        {
            int i = Array.IndexOf(Ways, way);
            return Ways[(i + 1 + Ways.Length) % Ways.Length];
        }

        public static string WayWords(string way)
        {
            switch (way)
            {
                case "draft": return "the draft-wheel native with a heavy force, after the can-break flags";
                case "wheel": return "the plain break-off-wheel native, its return logged";
                case "mission": return "the wagon taken as a mission entity first, then the draft-wheel native";
                case "damage": return "damage applied at the wheel's own bone";
                case "health": return "the body health set, as a check that natives take on this wagon at all";
                default: return null;
            }
        }

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

        // ---- someone else pulls up (FIVEDAY item 32d; README 32; [Encounters] WagonOthersStop) ------------------------------
        // A passer-by on the road stops for the wagon when you haven't: twenty-five seconds after the driver is down beside it
        // and you've not put your shoulder to it, or once you've ridden on. Six wagons in ten get one; the rest wait on you
        // or the smith.
        //
        // Why it reads true: on an empty road the bystander effect runs backwards. With nobody else to leave it to, the one
        // who passes feels the whole of the duty, and most stop. One who rode past first makes the next more likely to stop
        // and to say so, because a norm broken in sight is a norm made plain.
        public const int OtherAfterMs = 25000;     // after the driver is down and you've not helped
        public const int OtherChance = 60;         // percent of wagons someone else stops for
        public const float OtherFrom = 25f, OtherTo = 90f;   // how far off the passer-by is when he turns aside
        public const float OtherThere = 4.5f;      // at the wagon
        public const int OtherWalkMs = 45000;      // he gives it up if he's not there in this long

        public static bool OtherStops(string seed) { return Voice.Chance(seed + "|other", OtherChance); }

        /// The passer-by's offer as he comes up; with you having ridden past, he says so.
        public static string OtherOffer(bool youRodePast, string seed)
        {
            return youRodePast
                ? Voice.Pick(seed + "|op", "Saw a fellow ride right by you. Well, I'm not him. Where do you want me?", "That rider never even slowed. Here, let me get under it.")
                : Voice.Pick(seed + "|oo", "Wheel off? I'll take the weight. Say when.", "Hold up, I'm coming. Seen enough wagons in the ditch to know how this goes.", "Need another back? I've got one, and it's not doing anything.");
        }

        /// The driver's answer to the one who stopped.
        public static string DriverToOther(string seed)
        {
            return Voice.Pick(seed + "|od", "Bless you. Take the corner there, and lift when I say.", "Thank God. Get your shoulder under the bed, there.", "You're a Christian. Right, on three.");
        }

        /// The thought of one who stopped, after.
        public static string OtherAfter(bool youRodePast, string seed)
        {
            return youRodePast
                ? Voice.Pick(seed + "|oa", "Some men ride by. I was raised not to.", "Whoever that rider was, I hope his own wheel holds.")
                : Voice.Pick(seed + "|ob", "Late now, but the man's on his way. That's worth an hour.", "Wheel's on. Hands are filthy. I feel better than I did this morning.");
        }

        /// The end's word when a passer-by set it (for the log and F7).
        public static string OtherSetIt(string otherName) { return (otherName ?? "a passer-by") + " stopped and set it with him"; }

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
                yield return LiftWords("s" + i);
                yield return WordSent("Valentine", "s" + i);
                yield return OtherOffer(i % 2 == 0, "s" + i);
                yield return DriverToOther("s" + i);
                yield return OtherAfter(i % 2 == 0, "s" + i);
            }
        }
    }
}
