// Allies who come to your aid (a player's request, 2026-11-03, the roadmap's front item; Lex: "more interactions and NPC
// events that can add to the fondness"). When you're attacked within sight of people who think well of you, some of them
// come: who, from how they stand with you (enRAGE's fondness, a kindness of yours they remember, talks and a name known, a
// trouble you mended) and from their own nature (the brave and the hot first, the loyal for a friend, cowards never, the
// drunk readier), fewer the more are already with you, and only the armed against a gun. What they shout as they come and
// what they say to you after. Plain C#, no game; the game side (WestworldScript.Allies.cs) finds the fight and the people.
// All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class AllyTake
    {
        public bool Comes;
        public float Score, Bar;
        public string Why;
    }

    public static class Allies
    {
        /// How warm someone is to you, from everything the add-on keeps: 0 nothing, 1 a little, 2 a friend, 3 someone who owes
        /// you. enRAGE's fondness (0-100) counts from `likingFrom` up, a kindness of yours lately or a trouble you mended counts
        /// as a friend's, talks and your name known a little; fear or a grudge, nothing at all. Someone you saved (a bully
        /// pulled off them, the rope, the hogtied cut loose: Interact.SavedLately) owes you whatever else they feel (Lex, 2026-11-03).
        public static int Warmth(float like, int likingFrom, bool kindLately, bool mended, int talks, bool knowsName, bool afraidOrHostile, bool saved = false)
        {
            if (saved)
                return 3;
            if (afraidOrHostile)
                return 0;
            int w = 0;
            if (likingFrom > 0 && like >= likingFrom)
                w = like >= Math.Min(100, likingFrom + 30) ? 3 : 2;   // the bar full: someone who owes you (the line is 90 since settings 202)
            if (mended)
                w = Math.Max(w, 3);
            else if (kindLately)
                w = Math.Max(w, 2);
            if (talks >= 2 || (talks >= 1 && knowsName))
                w = Math.Max(w, 1);
            return w;
        }

        /// Whether this person comes to your aid: someone who owes you (warmth 3: the bar at AlliesLiking or more, someone you
        /// saved, a trouble you mended) always (Lex, 2026-11-03: a friend "would now protect the main character if fought by the
        /// law or anyone else"); a friend (warmth 2) most times, someone who merely thinks well of you (1) with the nerve for it.
        /// The bar: their own caution (anxiety, the risk they'll take, assertiveness), a gun against them unless they carry one,
        /// the first to come readiest and each after less so; a coward never; the drunk and the hot readier; a woman only
        /// against fists.
        public static AllyTake Weigh(Traits t, string temper, bool woman, bool drunk, int warmth, bool foeArmed, bool armed, int foes, int helping, string seed)
        {
            var a = new AllyTake();
            if (warmth <= 0)
            {
                a.Why = "no fondness for you";
                return a;
            }
            if (warmth >= 3)
            {
                a.Comes = true;
                a.Score = 2f;
                a.Why = "owes you: comes whatever the odds" + (foeArmed ? ", guns out" : "") + (helping > 0 ? ", " + helping + " with you already" : "");
                return a;
            }
            if (t == null)
            {
                a.Why = "no mind to weigh";
                return a;
            }
            if (temper == "Coward")
            {
                a.Why = "a coward";
                return a;
            }
            if (foeArmed && !armed)
            {
                a.Why = "a gun against them, and none of their own";
                return a;
            }
            if (woman && foeArmed)
            {
                a.Why = "a woman, and guns drawn";
                return a;
            }
            float nerve = 0.6f + 0.25f * t[Traits.Risk] + 0.2f * t[Traits.Assertiveness] - 0.3f * t[Traits.Anxiety] + 0.15f * t[Traits.Compassion];
            if (temper == "Hothead")
                nerve += 0.3f;
            if (temper == "Loyal")
                nerve += 0.25f;
            if (drunk)
                nerve += 0.2f;
            float pull = warmth >= 3 ? 1.15f : warmth == 2 ? 1f : 0.6f;   // an ordinary friend with ordinary nerve comes most times; someone who merely thinks well of you seldom
            a.Score = nerve * pull;
            a.Bar = 0.5f + 0.12f * helping + (foeArmed ? 0.15f : 0f) + (foes >= 3 ? 0.1f : 0f);
            // a roll a person and a fight, so the same friend doesn't always come and the same shirker always shirk
            float roll = (Voice.Hash(seed + "ally") % 100) / 100f * 0.3f - 0.15f;
            a.Comes = a.Score + roll >= a.Bar;
            a.Why = (warmth >= 3 ? "owes you" : warmth == 2 ? "a friend of yours" : "thinks well of you") + ", " + Describe(nerve) +
                    (drunk ? ", in drink" : "") + (foeArmed ? ", guns out" : "") + (helping > 0 ? ", " + helping + " with you already" : "");
            return a;
        }

        private static string Describe(float nerve)
        {
            return nerve >= 0.9f ? "nerve to spare" : nerve >= 0.6f ? "nerve enough" : nerve >= 0.4f ? "a little nerve" : "not much nerve";
        }

        // ---- what they say -------------------------------------------------------------------------------------

        private static readonly string[] Coming =
        {
            "Hold on! I'm coming!", "Not on my street you don't!", "Leave him be, you dogs!", "Hang on, I've got your back!",
            "Here! Over here! Come try me!", "You'll have to go through me first!", "Hey! That man's a friend of mine!", "Keep your head down, I'm coming!"
        };
        private static readonly string[] ComingArmed =
        {
            "Hold on, I've a gun!", "Drop it! Drop it or I'll shoot!", "Get down! I've got them!", "Hang on, friend, I'm here!", "They want a fight, they've got one!"
        };
        private static readonly string[] AfterWon =
        {
            "You all right? Lord, that was close.", "Nobody does that to a friend of mine. Nobody.", "You'd have done the same for me. You did, near enough.",
            "That's the last of them. Let's get a drink, if you're not shot.", "Couldn't stand there and watch, could I?", "Reckon we're even now. Or near it.",
            "My hands won't stop. Don't tell anybody.", "Didn't think. Just came. Glad I did."
        };
        private static readonly string[] AfterRan =
        {
            "They won't be back. Not while I'm about.", "Ran, the cowards. Good.", "That's them gone. You hurt?"
        };
        private static readonly string[] AfterHurt =
        {
            "I'm all right. It's nothing. Where'd they go?", "Caught one. I'll live. You?", "Worth it. Don't look at me like that, it was worth it."
        };
        private static readonly string[] RoadComing =
        {
            "Heard the shooting from the road! Hold on!", "Thought that was you! I'm coming!", "Hold them off, I'm here!"
        };

        public static string Shout(bool armed, bool road, string seed)
        {
            return Voice.Pick(seed + "shout", road ? RoadComing : armed ? ComingArmed : Coming);
        }

        // ---- the friend from the road rides in (SIXDAY item 2; [Encounters] AlliesRide) ----------------------------------

        /// Horses a townsman might own, by the game's own model names (a saddle horse, nothing a gang would ride).
        public static readonly string[] HorseModels = { "A_C_Horse_KentuckySaddle_Grey", "A_C_Horse_Morgan_Bay", "A_C_Horse_Tennesseewalker_Chestnut", "A_C_Horse_AmericanStandardbred_Black", "A_C_Horse_KentuckySaddle_Black" };
        public const float RideMinMetres = 45f, RideMaxMetres = 70f;   // made further off than a man on foot (30-45), so the ride shows
        public const float DismountMetres = 14f;   // down off the horse this near you, and in on foot
        public const int RideNoProgressMs = 8000;  // no nearer in this long (the horse won't take the order): down and run, as before
        public const int RideMostMs = 40000;       // and however it goes, off the horse by then

        public static string HorseModel(string seed)
        {
            return HorseModels[Voice.Hash(seed + "|horse") % HorseModels.Length];
        }

        // ---- an ally who fell (SIXDAY item 2; [Encounters] AlliesGrieved) ----------------------------------------------

        /// What those who knew the fallen think the less of you by: enRAGE's fondness (the setting for a friend, a third of
        /// it for someone merely known) and our own affection.
        public static int FellLiking(bool friend, int setting)
        {
            return friend ? setting : setting / 3;
        }

        public static int FellAffection(bool friend)
        {
            return friend ? 10 : 4;
        }

        /// The town's word of it, that evening.
        public static string FellNews(string deadFirst, string yourName)
        {
            string you = yourName != null ? Person.FirstOf(yourName) ?? yourName : "the stranger";
            return deadFirst + " is dead: stood with " + you + " when the guns came out, and took a bullet meant for " + (yourName != null ? "him" : "the stranger") + ".";
        }

        /// To you, as they fall.
        public static string FellCaption(string deadName)
        {
            return (Person.FirstOf(deadName) ?? "Your friend") + " fell standing with you.";
        }

        public static string After(bool foesRan, bool hurt, string seed)
        {
            return Voice.Pick(seed + "after", hurt ? AfterHurt : foesRan ? AfterRan : AfterWon);
        }

        /// What they keep of it: the memory's words come from the mind's "defend" lines (Minds.Lines.cs); this is the log's.
        public static string MemoryWords(string yourName)
        {
            return "stood with " + (yourName ?? "the stranger") + " when he was attacked";
        }

        public static IEnumerable<string> AllLines()
        {
            return Coming.Concat(ComingArmed).Concat(AfterWon).Concat(AfterRan).Concat(AfterHurt).Concat(RoadComing)
                         .Concat(new[] { FellNews("Abner", "Arthur Morgan"), FellNews("Abner", null), FellCaption("Abner Tuck") });
        }
    }
}
