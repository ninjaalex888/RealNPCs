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
                w = like >= likingFrom + 30 ? 3 : 2;
            if (mended)
                w = Math.Max(w, 3);
            else if (kindLately)
                w = Math.Max(w, 2);
            if (talks >= 2 || (talks >= 1 && knowsName))
                w = Math.Max(w, 1);
            return w;
        }

        /// Whether this person comes to your aid: warmth 2 or more (a friend), or 1 with the nerve for it. The bar: their own
        /// caution (anxiety, the risk they'll take, assertiveness), a gun against them unless they carry one, the first to come
        /// readiest and each after less so; a coward never; the drunk and the hot readier; a woman only against fists.
        public static AllyTake Weigh(Traits t, string temper, bool woman, bool drunk, int warmth, bool foeArmed, bool armed, int foes, int helping, string seed)
        {
            var a = new AllyTake();
            if (t == null || warmth <= 0)
            {
                a.Why = warmth <= 0 ? "no fondness for you" : "no mind to weigh";
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
            return Coming.Concat(ComingArmed).Concat(AfterWon).Concat(AfterRan).Concat(AfterHurt).Concat(RoadComing);
        }
    }
}
