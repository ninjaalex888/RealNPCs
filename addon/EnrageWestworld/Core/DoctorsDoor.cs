// The doctor's door at night (FOURTEENHOUR reserve R4; TWELVEHOUR item 13; [Scenes] DoctorsDoor). By day someone already walks to the
// doctor for a complaint of their own, a child's fever among them (Core/Patient.cs, the patient's walk), and the fever worry runs its
// arc in a parent's thoughts (InnerLife "fever": its crisis line is "Doctor came. Looked a long time. Said tonight will tell"). Now,
// after dark in a town whose doctor is at his post near you, a man whose child's fever is at its worst runs up the street to the
// doctor (or, now and then, a married man whose wife's time has come). He pounds and calls; the doctor keeps his post: his answer
// comes from where he stands, words and a look (he'll get his bag and be along). The man turns for home at a run. J as he turns:
// "I'll walk with you" (the walk home's escort, at his pace: you go with him to his door, a voice inside calls out, a thought from
// him; the escort's own thanks and kindness) or let him go. The doctor's visit is off screen. Next morning the town has it: the
// child pulled through, or didn't (the fever worry's own ending, so his thoughts after agree), and whether a stranger walked him
// home. Once a town a night. Why it reads true: a man running at night is a signal everyone reads at once, and the stranger who falls
// in beside him is remembered longer than the doctor's fee. Plain C#, no game; all words this add-on's.
using System;

namespace EnrageWestworld.Core
{
    public static class DoctorsDoor
    {
        public static bool On = true;   // [Scenes] DoctorsDoor, from the settings
        public const string Kind = "doctorsdoor";
        public const int PromptMs = 4500, BirthPerMil = 4, LookMs = 30000, Percent = 35;   // PromptMs: a man in a hurry waits that long on your word
        public const float FromMetres = 80f, LeastMetres = 15f;   // he starts this far from the doctor at most, and at least

        /// After dark: nine at night till five.
        public static bool Hour(int clock)
        {
            return clock >= 21 || clock < 5;
        }

        /// What sends him running tonight: "fever" (his child's fever worry at its crisis, the worry's own "tonight will tell"), "birth"
        /// (a married man of an age, now and then), else null.
        public static string Why(string worryKind, int stage, bool married, int age, string id, long day)
        {
            if (worryKind == "fever" && stage == 2)
                return "fever";
            if (married && age >= 20 && age <= 45 && (Voice.Hash(id + "|birth|" + day) & 0x7fffffff) % 1000 < BirthPerMil)
                return "birth";
            return null;
        }

        /// The child's sex, as the fever worry has it (InnerLife's "{who}": a daughter when the hash is even).
        public static bool ChildGirl(string id)
        {
            return Voice.Hash(id + "|cw") % 2 == 0;
        }

        /// His cry at the doctor's.
        public static string Knock(string why, string child, bool girl, string seed)
        {
            if (why == "birth")
                return Voice.Pick(seed + "|kb", "Doctor! Doctor, it's my wife, her time's come! Please!", "Doctor! It's the baby, it's coming, and it's wrong, something's wrong!");
            string c = child ?? (girl ? "my girl" : "my boy");
            return Voice.Pick(seed + "|kf", "Doctor! Doctor, it's " + c + ", " + (girl ? "she" : "he") + "'s burning up!", "Doctor, open up! " + Grammar.UpperFirst(c) + " won't wake properly!",
                                            "Doctor! Please, it's " + c + ". The fever's worse. Much worse.");
        }

        /// The doctor's answer from his post.
        public static string Answer(string why, string seed)
        {
            if (why == "birth")
                return Voice.Pick(seed + "|ab", "Go on home and boil water. I'll get my bag and be right behind you.", "Keep her warm and keep her calm. I'm coming.");
            return Voice.Pick(seed + "|af", "Go on home and keep cool cloths on the child. I'll get my bag and be right behind you.",
                                            "Run home and open the windows. I'll be there before you've caught your breath.", "Go. I'm coming. Go!");
        }

        /// At his door, with you beside him (the escort's arrival).
        public static string AtHisDoor(string seed)
        {
            return Voice.Pick(seed + "|ad", "This is us. Thank you for coming with me. I'll sit with them till he comes.", "Here. Thank you. I couldn't have stood the walk alone.",
                                            "You didn't have to come. I'll not forget it.");
        }

        /// A voice from inside as the door opens (a caption: nobody comes out).
        public static string Inside(string why, string seed)
        {
            if (why == "birth")
                return Voice.Pick(seed + "|ib", "Is that him? Is the doctor coming?", "Hurry. Oh, hurry.");
            return Voice.Pick(seed + "|if", "Is the doctor coming? Tell me he's coming.", "Is that you? The fever's no better.", "Hurry, love. She keeps asking for you.");
        }

        /// His thought once he's in.
        public static string Thought(bool walked, string seed)
        {
            return walked ? Voice.Pick(seed + "|tw", "A stranger walked me home. In the dark, for nothing.", "Didn't even ask his name. I should have asked his name.")
                          : Voice.Pick(seed + "|tn", "Ran the whole way. Don't remember the street.", "Please. Just get here.");
        }

        /// How it ends, from the fever worry's own arc (InnerLife): the first day it's settled, its good or bad; none within ten
        /// days (or a birth): good, mostly.
        public static bool EndsWell(Person p, string why, long day, string seed)
        {
            if (why == "fever" && p != null)
                for (long d = day; d <= day + 10; d++)
                {
                    string k; int stage; bool good;
                    if (InnerLife.WorryNow(p, d, out k, out stage, out good) && k == "fever" && stage == 3)
                        return good;
                }
            return (Voice.Hash(seed + "|end") & 0x7fffffff) % 12 != 0;
        }

        /// What the town has next morning.
        public static string News(string man, string child, bool girl, string why, bool well, bool walked, string seed)
        {
            string first = Person.FirstOf(man ?? "A man");
            string tail = walked ? " A stranger walked him home through the dark, they say." : "";
            if (why == "birth")
                return (well ? Voice.Pick(seed + "|nb", first + "'s wife was brought to bed in the night, of a " + (girl ? "girl" : "boy") + ". Mother and child doing well.",
                                                         "The doctor was up all night at " + first + "'s. A fine " + (girl ? "girl" : "boy") + " by morning.")
                             : first + "'s wife came through it, but the baby didn't. The doctor stayed till light.") + tail;
            string c = child ?? (girl ? "his girl" : "his boy");
            return (well ? Voice.Pick(seed + "|nw", first + "'s " + (child != null ? "little " + child : (girl ? "girl" : "boy")) + " came through the fever in the night. The doctor sat up till dawn.",
                                                     "The fever broke at " + first + "'s before morning. Doctor says the child will mend.")
                         : Voice.Pick(seed + "|nl", "The doctor got to " + first + "'s too late. " + Grammar.UpperFirst(c) + " didn't see the morning.",
                                                    first + " lost " + c + " to the fever in the night. The doctor did what could be done.")) + tail;
        }

        /// The J menu's rows as he turns for home.
        public static System.Collections.Generic.List<InteractOption> Rows(string man)
        {
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "doctorsdoor:walk", Label = "\"I'll walk with you.\"" },
                new InteractOption { Id = "doctorsdoor:let", Label = "Let " + (man ?? "him") + " go" },
            };
        }
    }
}
