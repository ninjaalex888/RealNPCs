// A brawl that grows, and the law that stops it (Lex, 2026-10-31, in game after the bully test: "the law will get involved
// with stopping the brawls, as well as having them cascade into whole town brawls if it leads that way"). Lex's answers:
// a lawman who sees it shouts to break it up, those who stop get off with a word, whoever keeps swinging gets the law's
// fists and the rope (guns only if someone draws); up to about a dozen at once, likelier in a saloon or with drink, a
// wave at a time pulled in by friends and grudges; Arthur may be swung at if he's near; and rarely, by the mind, a losing
// man draws, by the quarrel ladder's own heat for the gun. The crowd rule of the mind doc still holds (most watch, few
// join); what grows a brawl is the brawl itself: the more who are in it, the less it takes to join. Seeded, so the same
// people in the same fight answer the same way. Plain C#, no game; the game layer is WestworldScript.Brawl.cs and
// WestworldScript.JoinIn.cs.
using System;

namespace EnrageWestworld.Core
{
    public static class Brawl
    {
        /// The most who may fight at once, the two who began it among them ([Mind] BrawlMost: 3 to 20).
        public const int MostAtAll = 20, LeastAtAll = 3;

        /// How many may join besides the two who began it.
        public static int MostJoiners(int most)
        {
            return Math.Max(1, Math.Min(MostAtAll, Math.Max(LeastAtAll, most)) - 2);
        }

        /// How many may come in at one look (every 2.5 s): one while it's a fight between a few, more once it's a brawl;
        /// a saloon's drink brings one more. Never past the room left.
        public static int Wave(int fighting, bool saloon, int roomLeft)
        {
            int w = fighting < 4 ? 1 : fighting < 7 ? 2 : 3;
            if (saloon && fighting >= 3)
                w++;
            return Math.Max(0, Math.Min(Math.Min(w, 4), roomLeft));
        }

        /// The pull of the brawl itself, added to a would-be joiner's score: nothing while it's two or three, then a little
        /// more for each in it, more in a saloon, up to a cap (a dozen swinging is hard to stand and watch).
        public static float Momentum(int fighting, bool saloon)
        {
            if (fighting < 4)
                return 0f;
            float m = (saloon ? 0.035f : 0.012f) * (fighting - 3);   // a saloon's riot pulls; on the street most still watch
            return Math.Min(saloon ? 0.20f : 0.07f, m);
        }

        /// How much the law at hand holds a would-be joiner back: on the street most stay out with a badge in sight, in a
        /// saloon's riot fewer do. A factor on the chance (the old rule, nobody at all, was 0).
        public static float LawHolds(bool saloon, int fighting)
        {
            float f = saloon ? 0.35f : 0.15f;
            if (fighting >= 7)
                f += 0.10f;   // past a point a riot doesn't see the badge
            return f;
        }

        /// Whether one of those fighting stops when the lawman shouts: the law-abiding and the self-possessed do, the angry,
        /// the drunk and the reckless less; one who's winning is slower to stop than one who's losing. Seeded.
        public static bool Heeds(Traits t, bool drunk, bool winning, string seed, out int chance)
        {
            float c = 0.55f;
            if (t != null)
                c += 0.14f * t.Z[Traits.Deference] + 0.10f * t.Z[Traits.SelfControl] - 0.12f * t.Z[Traits.Anger] - 0.06f * t.Z[Traits.Risk] - 0.04f * t.Z[Traits.Assertiveness];
            if (drunk)
                c -= 0.20f;
            if (winning)
                c -= 0.08f;
            c = Math.Max(0.10f, Math.Min(0.95f, c));
            chance = (int)Math.Round(c * 100);
            return Voice.Chance(seed + "|heeds", chance);
        }

        /// Whether someone in a big brawl, or watching it, goes for Arthur standing near: anyone he's hit in it most of all,
        /// someone against him, a drunk hothead; never someone afraid of him. Nothing while it's still a fight of a few.
        public static bool AtYou(Traits t, bool drunk, bool hostile, bool afraid, bool hitByYou, int fighting, bool saloon, string seed, out string why)
        {
            why = null;
            if (afraid || t == null || fighting < 4)
            {
                why = afraid ? "afraid of you" : fighting < 4 ? "a fight of a few" : "nobody the mind knows";
                return false;
            }
            float heat = 0.10f * t.Z[Traits.Anger] + 0.06f * t.Z[Traits.Assertiveness] + 0.05f * t.Z[Traits.Risk] - 0.08f * t.Z[Traits.SelfControl] + (drunk ? 0.12f : 0f);
            float c = hitByYou ? 0.55f + heat : hostile ? 0.15f + heat : drunk && heat > 0.15f ? 0.04f + heat * 0.5f : 0f;
            if (c > 0f)
                c += Momentum(fighting, saloon) * 0.5f;   // the brawl adds to a reason, never makes one
            c = Math.Max(0f, Math.Min(0.85f, c));
            int pct = (int)Math.Round(c * 100);
            why = (hitByYou ? "you hit them" : hostile ? "against you" : drunk ? "drunk and hot" : "nothing against you") + " (" + pct + "%)";
            return pct > 0 && Voice.Chance(seed + "|atyou", pct);
        }

        /// Rarely, a man losing a brawl reaches for his gun: only an armed man, only one drunk, hot-tempered, or proud with
        /// bad blood against the one beating him; the chance is the quarrel ladder's own for the blows to the draw (Escalation,
        /// with the law and the town's gun rule in it), a quarter of it, and then its own for the draw to the shot. "none", "draw" or
        /// "shoot".
        public static string Draws(Traits t, QuarrelContext c, bool losing, int warmth, string seed, out int drawPct)
        {
            drawPct = 0;
            if (t == null || c == null || !losing || !c.ArmedA || c.WomanA)
                return "none";
            bool hot = t.Z[Traits.Anger] > 0.8f, proudFoe = t.Z[Traits.Assertiveness] > 0.5f && warmth < 0;
            if (!c.DrunkA && !hot && !proudFoe)
                return "none";
            drawPct = (int)Math.Round(Escalation.Heat(t, true, c, 2) * 25f);   // a quarter of the ladder's own odds: a brawl is fists, the gun the rare end of it
            if (drawPct <= 0 || !Voice.Chance(seed + "|draw", drawPct))
                return "none";
            int shootPct = (int)Math.Round(Escalation.Heat(t, true, c, 3) * 100f);
            return shootPct > 0 && Voice.Chance(seed + "|shoot", shootPct) ? "shoot" : "draw";
        }

        // ---- the words (all our own) ---------------------------------------------------------------------------------

        private static readonly string[] BreakUp =
        {
            "That's enough! Break it up, the lot of you!",
            "Hands down! Every man of you, hands down, now!",
            "Swing once more and you sleep behind bars!",
            "Law here! Step apart or I'll do the parting myself!",
            "I said enough! Back off him, all of you!",
            "Not in my town. Break it up!",
        };

        private static readonly string[] LetOff =
        {
            "Go home and sleep it off.",
            "Walk away. I see you again today, you're mine.",
            "Count yourself lucky. Off with you.",
            "Wash the blood off and stay off my street.",
        };

        private static readonly string[] Taken =
        {
            "You had your chance. Now you've got the rope.",
            "Down you go. The judge can hear your side.",
            "I told you twice. That's the jail for you.",
            "Lie still. You've earned yourself a cell.",
        };

        private static readonly string[] Stops =
        {
            "All right, all right.",
            "He started it, Marshal.",
            "Enough. I've had enough.",
            "Easy. Nobody's hurt.",
            "Fine. Fine.",
        };

        private static readonly string[] AtYouLines =
        {
            "And you! Don't think I didn't see you!",
            "You want some too, mister?",
            "Come here, you!",
            "That was my jaw you hit!",
        };

        public static string LawBreaksUp(string seed) { return BreakUp[Voice.Hash(seed + "|breakup") % BreakUp.Length]; }
        public static string LawLetsOff(string seed) { return LetOff[Voice.Hash(seed + "|letoff") % LetOff.Length]; }
        public static string LawTakes(string seed) { return Taken[Voice.Hash(seed + "|taken") % Taken.Length]; }
        public static string Stopping(string seed) { return Stops[Voice.Hash(seed + "|stops") % Stops.Length]; }
        public static string ComesAtYou(string seed) { return AtYouLines[Voice.Hash(seed + "|atyou") % AtYouLines.Length]; }
    }
}
