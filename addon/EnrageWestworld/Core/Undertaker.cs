// The dead carried to the undertaker's or the jail, and a mourner following (SIXDAY item 18; roadmap 33; Lex, 2026-10-31:
// "carrying it to the undertaker's or the jail, and someone who knew the dead person following"). The lawman's carry is
// the game side's (WestworldScript.Bodies.cs, the undertaker script's own calls); here is where it goes when a place is
// known within reach (the undertaker's door first, his trade; else the sheriff's door the player last came out of; else
// the short carry off the street as before), and the words of the one who knew the dead and walks behind: their thought
// on the way and at the set-down. Why it reads true: in 1899 a body in the street went to the undertaker or the jail's
// back room, and whoever knew them followed, because somebody had to. Plain C#, no game; all words original.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Undertaker
    {
        public const float ReachMetres = 80f;     // a destination this far from the body is carried to (the carry's cap grows with the walk: WestworldScript.Bodies.cs CarryCap)
        public const float MournerMetres = 25f;   // a friend of the dead this near when the lift settles follows

        /// Where the body goes: the undertaker's when his door is within reach, else the jail door, else null (the short carry).
        /// A distance under 0 means the place isn't known.
        public static string Choose(float undertakerMetres, float jailMetres)
        {
            if (undertakerMetres >= 0f && undertakerMetres <= ReachMetres)
                return "undertaker";
            if (jailMetres >= 0f && jailMetres <= ReachMetres)
                return "jail";
            return null;
        }

        /// The place in words, for the log and the carry's spot.
        public static string Where(string choice)
        {
            return choice == "undertaker" ? "the undertaker's door" : choice == "jail" ? "the jail door" : "off the street";
        }

        /// What the one who knew the dead thinks, walking behind the lawman.
        public static string Following(string deadFirst, bool deadWoman, string seed)
        {
            string d = deadFirst ?? (deadWoman ? "her" : "him");
            string she = deadWoman ? "she" : "he", her = deadWoman ? "her" : "him";
            return Voice.Pick(seed + "|fol",
                "Somebody should walk behind " + d + ". It may as well be me.",
                "Mind " + her + " head, mister. Mind " + her + " head on the step.",
                d + " hated being carried. Said so once, laughing.",
                "I'll see where they put " + her + ". Somebody ought to know where.",
                "Walk slow. " + d + " never hurried a day " + (deadWoman ? "she" : "he") + " lived.",
                "I'm coming, " + d + ". I'm right behind.");
        }

        /// What they think as the body is set down (where: the undertaker's door, the jail door, or null for the street).
        public static string AtRest(string deadFirst, string where, string seed)
        {
            string d = deadFirst ?? "them";
            if (where != null && where.StartsWith("the undertaker", StringComparison.Ordinal))
                return Voice.Pick(seed + "|und",
                    "The undertaker's. Well. " + d + " always said the man had cold hands.",
                    "They'll want paying for the box. I'll find it. " + d + " would've found it for me.",
                    "In there, then. I'll come by in the morning and see " + d + " is done right.");
            if (where != null && where.StartsWith("the jail", StringComparison.Ordinal))
                return Voice.Pick(seed + "|jail",
                    "The jail. Like " + d + " was the one did wrong.",
                    "The sheriff's back room. Cold enough, I suppose. I'll ask about the box tomorrow.",
                    d + " never saw the inside of that place alive. Now this.");
            return Voice.Pick(seed + "|str",
                "Off the street and that's all. Somebody fetch the undertaker for " + d + ".",
                "Just laid down there. I'll stand with " + d + " till somebody comes.",
                "That's no place to leave " + d + ". I'll sit here a while.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 6; i++)
            {
                yield return Following("Abner", false, "f" + i);
                yield return Following("Ruth", true, "g" + i);
                yield return Following(null, false, "h" + i);
                yield return AtRest("Abner", "the undertaker's door", "u" + i);
                yield return AtRest("Abner", "the jail door", "j" + i);
                yield return AtRest("Abner", null, "s" + i);
            }
        }
    }
}
