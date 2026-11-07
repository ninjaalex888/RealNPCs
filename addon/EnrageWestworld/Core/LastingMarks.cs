// Lasting marks (FIVEDAY item 24). The man you beat carries it for days. A knocking down (a shove) shows a day, a beating
// four. While it lasts:
// - he thinks of it, and of you, when you're about;
// - a friend of his near you names you for it ("That's the one did Hank's face").
// With [Mind] LastingMarksLook (off as built, until Lex has seen the probe), the body shows it too: one of the game's
// injured locomotion archetypes, and a damage pack for the bruising. Neither gait name nor
// pack name is proven in RDR2, so the Testing row "marks" tries each candidate on the nearest named person, in turn, and
// says which: Lex picks the ones that read. The names it tried and settled on go in [Mind] LastingMarksGait and
// LastingMarksPack. Behind [Mind] LastingMarks.
//
// Why it reads true: in 1899 a beating's marks were worn in public for a week (no doctor for a black eye), and a town
// read them: who'd been hurt, and by whom, was common knowledge. Plain C#, no game; all words original.
// mindtest LastingMarksTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class LastingMarks
    {
        public static readonly string[] Gaits = { "injured_general", "injured_left_leg", "injured_right_leg", "injured_torso", "injured_left_arm", "injured_right_arm" };
        public static readonly string[] Packs = { "SCR_Torture", "BigHitByVehicle", "SCR_Dumpster", "Fall", "Skin_Melee_0", "TD_PISTOL_FRONT" };

        /// How many game days a wrong's marks show: a beating four, a knocking down one; null for none.
        public static int Days(string deedKind)
        {
            return deedKind == "assault" ? 4 : deedKind == "shove" ? 1 : 0;
        }

        /// Whether the marks still show: the deed's kind, and the minutes since it.
        public static bool Shows(string deedKind, long minutesSince)
        {
            int d = Days(deedKind);
            return d > 0 && minutesSince >= 0 && minutesSince < d * 1440L;
        }

        private static readonly string[] Own =
        {
            "Every step, I feel where he put his boot.", "Can't chew on that side yet. Soup again.", "Mirror says I'll mend. Mirror's a liar.",
            "Folks look at my face and then away. They know who.", "I'll heal. I'll not forget.", "Ribs. Every breath, the ribs.",
            "Wore my hat low today. Didn't help.", "There he is. Don't look. Don't let him see you look.",
        };

        private static readonly string[] FriendNames =
        {
            "That's the one did {n}'s face. Look at him.", "{you}. He's the one put {n} on the ground.", "Him. {n} still can't walk right because of him.",
            "{n}'s limping, and there goes the man who did it.", "Look at {you}, strolling like nothing happened. {n} can barely stand.",
        };
        private static readonly string[] FriendNamesShove = { "{you}. He's the one put {n} on the ground.", "There's the man who knocked {n} down in the street.", "{n} is still sore about it. And there he goes." };   // a knocking down: no limp to speak of

        public static string OwnThought(string seed) { return Voice.Pick(seed + "|mk", Own); }

        /// A friend near you naming you for it: {n} the one hurt, {you} what they call you ("that stranger" when they don't know your name).
        public static string FriendThought(string hurt, string you, string seed, bool beating = true)
        {
            return Voice.Pick(seed + "|mf", beating ? FriendNames : FriendNamesShove).Replace("{n}", hurt ?? "my friend").Replace("{you}", you ?? "That stranger");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Own) yield return l;
            foreach (string l in FriendNames) yield return l.Replace("{n}", "Hank").Replace("{you}", "Morgan");
            foreach (string l in FriendNames) yield return l.Replace("{n}", "Hank").Replace("{you}", "That stranger");
            foreach (string l in FriendNamesShove) yield return l.Replace("{n}", "Hank").Replace("{you}", "That stranger");
        }
    }
}
