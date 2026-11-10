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
using System.Collections.Generic;
using System.Linq;

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
        /// a saloon's drink brings one more, and so does a busy street (ten or more watching; Lex, 2026-10-31, in Saint
        /// Denis: "I am near a lot of NPCs ... expected those NPCs to join the brawl"). Never past the room left.
        public static int Wave(int fighting, bool saloon, int roomLeft, int witnesses = 0)
        {
            int w = fighting < 4 ? 1 : fighting < 7 ? 2 : 3;
            if (saloon && fighting >= 3)
                w++;
            if (witnesses >= BusyStreet && fighting >= 3)
                w++;
            return Math.Max(0, Math.Min(Math.Min(w, 5), roomLeft));
        }

        /// So many watching makes a busy street: a wave more.
        public const int BusyStreet = 10;

        /// The pull of the brawl itself, added to a would-be joiner's score: nothing while it's two or three, then a little
        /// more for each in it, more in a saloon, up to a cap (a dozen swinging is hard to stand and watch).
        /// `fromThree`: [Mind] BrawlCascade's own (FOURTEENHOUR item 7 asked for the pull from three; the review of items 1-13 found
        /// it still began at four): the same steps, one sooner.
        public static float Momentum(int fighting, bool saloon, bool fromThree = false)
        {
            int from = fromThree ? 3 : 4;
            if (fighting < from)
                return 0f;
            float m = (saloon ? 0.035f : 0.02f) * (fighting - from + 1);   // a saloon's riot pulls; a street crowd too, less (0.012 to a cap of 0.07 until 2026-10-31)
            return Math.Min(saloon ? 0.20f : 0.15f, m);
        }

        /// How much the law at hand holds a would-be joiner back: on the street most stay out with a badge in sight, in a
        /// saloon's riot fewer do. A factor on the chance (the old rule, nobody at all, was 0).
        public static float LawHolds(bool saloon, int fighting)
        {
            float f = saloon ? 0.35f : 0.30f;   // the street 0.15 until 2026-10-31 (Lex: a crowded street should grow one)
            if (fighting >= 7)
                f += 0.10f;   // past a point a riot doesn't see the badge
            return f;
        }

        // ---- the badge across the street (FOURTEENHOUR item 5; [Mind] BrawlLawSight). Lex's log of 2026-11-09: every man who
        // stayed out of a Valentine fight was held by "the law at hand", any lawman within 30 m, seen or not. People are deterred
        // by a sanction they believe is certain and near (the deterrence literature's oldest finding: certainty and immediacy
        // count for more than severity); a badge they can't see doesn't stop a punch, and one 25 m off is a man walking over.

        /// The hold by sight and distance: in sight within 12 m, as LawHolds; in sight farther (to 30 m), half the hold
        /// (0.30 becomes 0.65); out of sight (behind a wall, indoors, at his desk), a little (0.85).
        public static float LawHoldsSeen(bool saloon, int fighting, float metres, bool sees)
        {
            float near = LawHolds(saloon, fighting);
            if (!sees)
                return Math.Max(near, OutOfSightHold);
            if (metres <= NearMetres)
                return near;
            return near + (1f - near) * 0.5f;
        }

        public const float OutOfSightHold = 0.85f, NearMetres = 12f;

        // He walks over, then runs (FOURTEENHOUR item 5, built at the review of items 1-13): from beyond LawRunsWithin at a walk, for
        // LawWalksMostMs at most, then at a run. A lawman within LawOnTopMetres when he set off stops a fight at once, fair or not.
        public const float LawRunsWithin = 20f, LawOnTopMetres = 8f;
        public const int LawWalksMostMs = 6000;

        /// Some lawmen let two men settle it with their fists a while, as many marshals did: this one (seeded by the town and
        /// the man), and for how long. He steps in at once when it turns two on one, a man goes down, or a gun shows.
        public static bool LetsItRun(string town, string lawSeed, out int seconds)
        {
            int h = Voice.Hash((town ?? "") + "|" + lawSeed + "|letrun");
            seconds = 12 + (h & 0x7fffffff) % 14;   // 12-25 s
            return Voice.Chance((town ?? "") + "|" + lawSeed + "|letsrun", LetsRunPercent);
        }

        public const int LetsRunPercent = 40;

        /// One deputy alone against this many or more holds off and sends for help before he gives the word.
        public const int WaitsForHelpAt = 6;

        // ---- the cascade (FOURTEENHOUR item 7; [Mind] BrawlCascade). Granovetter's threshold model (1978): what a crowd does
        // depends on the spread of its thresholds, not its average mood. Each onlooker with no stake in a fight has a threshold,
        // how many must be at it before he'd join: low for the hot, the reckless and the drunk, high for the self-possessed, the
        // law-abiding and the anxious, higher with the law near. Below it he only watches; past it the join's own weighing
        // (the score, the bar, the roll) decides as before. So a fight with nobody quick to join stays two men, and one with a
        // hothead or two in the crowd tips: each who joins brings the next man's threshold nearer. Friends, foes and his own kind
        // aren't held by it: they have their own reasons. Measured in the simulator (item 4): most fights stay small and a few
        // grow large, where before nearly every lawless fight grew to a brawl of six or more.

        /// How many must be fighting before this onlooker, with no stake in it, would join (1 to 12). Seeded, so the same man
        /// at the same fight has the same threshold.
        public static int Threshold(Traits t, bool drunk, bool saloon, bool lawNear, string seed, bool paydayNight = false)
        {
            float x = saloon ? CascadeSaloonBase : CascadeStreetBase;
            if (paydayNight && saloon)
                x -= Payday.NightThreshold;   // paid, in drink, among their own: payday night's saloon (FOURTEENHOUR item 13)
            if (t != null)
                x += -1.1f * t.Z[Traits.Anger] - 0.6f * t.Z[Traits.Risk] - 0.4f * t.Z[Traits.Assertiveness] + 0.7f * t.Z[Traits.SelfControl] + 0.5f * t.Z[Traits.Deference] + 0.4f * t.Z[Traits.Anxiety];
            if (drunk)
                x -= 1.5f;
            if (lawNear)
                x += 1.0f;
            x += ((Voice.Hash(seed + "|threshold") & 0x7fffffff) % 200) / 100f - 1f;   // a man's own day: -1 to +1
            return Math.Max(1, Math.Min(12, (int)Math.Round(x)));
        }

        // ---- the stray blow and the friend who pulls him out (FOURTEENHOUR item 8; [Mind] BrawlStrays). The stray punch is the
        // oldest way a two-man fight becomes a room's; the friend dragging his man out by the collar is the oldest way it ends.

        /// Someone standing within arm's length of the fighting two looks running takes a stray blow this often.
        public const int StrayPercent = 35;

        /// What a man does with a stray blow: "swing" (he's in it, against the one who hit him: the hot, the reckless, the
        /// drunk), "back" (he backs off: the timid), or "cry" (a woman cries out and leaves, unless WomenInFights has her swing).
        public static string StrayAnswer(Traits t, bool drunk, bool woman, bool womenFight, string seed)
        {
            float hot = t == null ? 0f : 0.6f * t.Z[Traits.Anger] + 0.3f * t.Z[Traits.Risk] + 0.2f * t.Z[Traits.Assertiveness] - 0.3f * t.Z[Traits.SelfControl] - 0.3f * t.Z[Traits.Anxiety];
            if (drunk)
                hot += 0.5f;
            int roll = (Voice.Hash(seed + "|stray") & 0x7fffffff) % 100;
            if (woman)
                return womenFight && hot > 0.6f && roll < 40 ? "swing" : "cry";
            int swing = Math.Max(10, Math.Min(90, 50 + (int)(hot * 40)));
            return roll < swing ? "swing" : "back";
        }

        /// The word over a stray blow's head (the review of items 1-13: the blow was never seen): a woman's cry, a man's protest as
        /// he backs off, or the man who swings back.
        public static string StrayCry(bool woman, string answer, string seed)
        {
            if (woman)
                return Voice.Pick(seed + "|cry", "Oh! You brute!", "Ow! Mind me!", "Watch your fists, you animal!");
            if (answer == "swing")
                return Voice.Pick(seed + "|cry", "Who swung that?", "Right. That does it.", "That one'll cost you!");
            return Voice.Pick(seed + "|cry", "Hey! I ain't in this!", "Watch your elbows!", "Not me, you fool!");
        }

        /// Whether a friend near a man losing badly (under 60% of his health as the fight began) drags him out of it: the warmer the friend and the
        /// kinder, the likelier; the anxious less (it means walking into the fight). Seeded.
        public static bool PullsOut(Traits friend, int warmth, string seed)
        {
            float c = 0.40f + 0.10f * Math.Max(0, warmth - 1);
            if (friend != null)
                c += 0.15f * friend.Z[Traits.Compassion] + 0.08f * friend.Z[Traits.Assertiveness] - 0.10f * friend.Z[Traits.Anxiety];
            c = Math.Max(0.10f, Math.Min(0.90f, c));
            return Voice.Chance(seed + "|pullout", (int)Math.Round(c * 100));
        }

        /// Whether the one beating him goes after the two as they leave: the hot and the drunk do, the rest let him go.
        public static bool FollowsThem(Traits t, bool drunk)
        {
            return t != null && (t.Z[Traits.Anger] > 0.8f || (drunk && t.Z[Traits.Anger] > 0.3f));
        }

        // against his health as the fight began (Director.FightHealth), and above the Director's 45% down (FightDownStep), or the
        // down comes first and nobody is ever pulled out (0.35 of max health until the review of items 1-13)
        public const float PullOutHealth = 0.6f;

        // ---- the big brawl's shape (FOURTEENHOUR item 10; [Mind] BrawlShape). A big fight climbs, sprawls, and dies of tiredness or
        // the law; the shape is what makes it a story. Six or more at it: the barkeep's shout from behind his bar (keepers keep their
        // posts: words and a look), the timid and the women near slip out of it, and a gun drawn in it scatters the room. From ten
        // seconds before its least time is up, the beaten-looking quit at each look, so it thins and the last of them finish it.

        public const int BigAt = 6;
        public const int BurnOutPercent = 35;
        public const float BurnOutHealth = 0.75f;   // of his health as the fight began (0.5 of max until the review of items 1-13: only 45-50% was caught)
        public const int BurnOutLeadMs = 10000;   // the thinning starts this long before the brawl's least time is up

        /// Whether one watching a big brawl slips out of the room: the timid (the anxious, the meek), and women.
        public static bool SlipsOut(Traits t, bool woman, string seed)
        {
            float c = woman ? 0.55f : 0.10f;
            if (t != null)
                c += 0.20f * t.Z[Traits.Anxiety] - 0.12f * t.Z[Traits.Risk] - 0.08f * t.Z[Traits.Anger];
            return Voice.Chance(seed + "|slipsout", (int)Math.Round(Math.Max(0.02f, Math.Min(0.85f, c)) * 100));
        }

        private static readonly string[] KeeperShouts = { "Not in here! Take it outside!", "That's my mirror, damn you!", "Who's paying for those chairs?", "Somebody fetch the marshal!",
                                                          "I'll have every one of you barred!", "Break it up or I'll break it up for you!" };
        public static string KeeperShout(string seed) { return KeeperShouts[Voice.Hash(seed + "|keeper") % KeeperShouts.Length]; }
        public static IEnumerable<string> ShapeLines() { return KeeperShouts; }

        public static float CascadeStreetBase = 4.0f, CascadeSaloonBase = 6.6f;   // tuned in the simulator (item 7): bigger than before where the law is (Valentine with a lawman in sight, four or more 14% to 28%), the lawless street about as it was, a saloon at night a riot 65% of the time, not 97%   // (static, not const: the simulator tunes them)

        /// "Law!" cried as he comes (within 20 m, three or more at it): only the law-abiding (deference over half a step) who'd
        /// heed his word slip away before he's there; the rest swing on.
        public static bool SlipsAwayAtTheCry(Traits t, bool drunk, string seed)
        {
            if (t == null || t.Z[Traits.Deference] <= 0.5f)
                return false;
            int chance;
            return Heeds(t, drunk, false, seed + "|cry", out chance);
        }

        /// Whether one of those fighting stops when the lawman shouts: the law-abiding and the self-possessed do, the angry,
        /// the drunk and the reckless less; one who's winning is slower to stop than one who's losing. Seeded. Two lawmen at
        /// it together are heeded more (item 5: the sheriff come with another).
        public static bool Heeds(Traits t, bool drunk, bool winning, string seed, out int chance, int lawmen = 1, float standing = 0f)
        {
            float c = 0.55f;
            if (t != null)
                c += 0.14f * t.Z[Traits.Deference] + 0.10f * t.Z[Traits.SelfControl] - 0.12f * t.Z[Traits.Anger] - 0.06f * t.Z[Traits.Risk] - 0.04f * t.Z[Traits.Assertiveness];
            if (drunk)
                c -= 0.20f;
            if (winning)
                c -= 0.08f;
            if (lawmen >= 2)
                c += 0.12f;
            c += standing;   // Arthur's own shout: what this man thinks of him (BrawlVerbs.Standing; FOURTEENHOUR item 11)
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
        /// with the law and the town's gun rule in it), share percent of it ([Mind] BrawlDrawShare, 40; a quarter until
        /// 2026-10-31, Lex: "tune it to escalate to guns slightly"), and then its own for the draw to the shot. Two or more on
        /// him is a reason too. "none", "draw" or "shoot".
        public static string Draws(Traits t, QuarrelContext c, bool losing, int warmth, string seed, out int drawPct, int share = DrawShare, int onHim = 1)
        {
            drawPct = 0;
            if (t == null || c == null || !losing || !c.ArmedA || c.WomanA)
                return "none";
            bool hot = t.Z[Traits.Anger] > 0.8f, proudFoe = t.Z[Traits.Assertiveness] > 0.5f && warmth < 0, setUpon = onHim >= 2;
            if (!c.DrunkA && !hot && !proudFoe && !setUpon)
                return "none";
            drawPct = (int)Math.Round(Escalation.Heat(t, true, c, 2) * Math.Max(0, Math.Min(100, share)));   // a share of the ladder's own odds: a brawl is fists, the gun the rare end of it
            if (drawPct <= 0 || !Voice.Chance(seed + "|draw", drawPct))
                return "none";
            int shootPct = (int)Math.Round(Escalation.Heat(t, true, c, 3) * 100f);
            return shootPct > 0 && Voice.Chance(seed + "|shoot", shootPct) ? "shoot" : "draw";
        }

        /// The share of the quarrel ladder's draw odds a man losing a brawl has, as built ([Mind] BrawlDrawShare).
        public const int DrawShare = 40;

        // ---- fists: killed or knocked out (Lex, 2026-10-31: "Ideally the fistfights should eventually be fatal or knockout
        // depending on the NPC innermind. For now we can have fist fights go maybe 50% fatal, 50% knockout or sort of random
        // generation that makes sense for exciting gameplay from a game designer perspective") ----

        /// The fatal share of a beating in a brawl as built ([Mind] BrawlFistsFatal).
        public const int FistsFatal = 50;

        /// Whether a man beaten down in a brawl is killed or only knocked out. fatalPct ([Mind] BrawlFistsFatal) is the
        /// plain case: one man on him, nothing else. Worse: more on him (+12 each past the first, at most +25), a drunk or a
        /// hothead beating him (+10 each), bad blood between them (+8; +15 for a grudge of -2 or worse), an old man down
        /// (+15). Better: a lawman close (-20), a friend of his in it (-15), a fair fight, one on one with the sides even
        /// (-10). Kept within 5-95 (0 and 100 mean what they say). Seeded by who is in it: "fatal" or "knockout".
        public static string Fate(Traits attacker, bool attackerDrunk, int attackersOn, int warmth, bool victimOld, bool lawNear, bool friendNear, bool fair,
                                  int fatalPct, string seed, out int pct, out string why)
        {
            string w = null;
            int p = Math.Max(0, Math.Min(100, fatalPct));
            if (p > 0 && p < 100)
            {
                if (attackersOn > 1)
                {
                    p += Math.Min(25, 12 * (attackersOn - 1));
                    w = Also(w, attackersOn + " on him");
                }
                if (attackerDrunk)
                {
                    p += 10;
                    w = Also(w, "a drunk at him");
                }
                if (attacker != null && attacker.Z[Traits.Anger] > 0.8f)
                {
                    p += 10;
                    w = Also(w, "a hothead at him");
                }
                if (warmth <= -2)
                {
                    p += 15;
                    w = Also(w, "a grudge");
                }
                else if (warmth < 0)
                {
                    p += 8;
                    w = Also(w, "bad blood");
                }
                if (victimOld)
                {
                    p += 15;
                    w = Also(w, "an old man");
                }
                if (lawNear)
                {
                    p -= 20;
                    w = Also(w, "the law close");
                }
                if (friendNear)
                {
                    p -= 15;
                    w = Also(w, "a friend of his in it");
                }
                if (fair)
                {
                    p -= 10;
                    w = Also(w, "a fair fight");
                }
                p = Math.Max(5, Math.Min(95, p));
            }
            pct = p;
            why = (w ?? "a plain beating") + " (" + pct + "% fatal)";
            return Voice.Chance(seed + "|fate", pct) ? "fatal" : "knockout";
        }

        private static string Also(string w, string more) { return w == null ? more : w + ", " + more; }

        /// How long a man knocked out in a brawl lies there before he comes to: 20-40 s, seeded.
        public static int KnockoutMs(string seed)
        {
            return 20000 + (Voice.Hash(seed + "|out") & 0x7fffffff) % 20001;
        }

        // ---- the words (all our own) ---------------------------------------------------------------------------------

        // (the pools grew in FIVEDAY item 3: four to six lines each made a long brawl say the same thing twice)
        private static readonly string[] BreakUp =
        {
            "That's enough! Break it up, the lot of you!",
            "Hands down! Every man of you, hands down, now!",
            "Swing once more and you sleep behind bars!",
            "Law here! Step apart or I'll do the parting myself!",
            "I said enough! Back off him, all of you!",
            "Not in my town. Break it up!",
            "Apart! The next fist I see goes in irons!",
            "You hear me? Stand off, or I start cracking heads!",
            "This street's had its fill of you. Separate!",
            "Drop your hands, or I'll drop you!",
            "Every man still swinging spends the night with me!",
            "Hold it! Hold it, I said!",
            "In the name of the county, stand apart!",
            "Lay off him! Now, damn you!",
            "That's the last punch thrown today. Step back!",
            "Whoever's got the most blood on him, I'm asking him first!",
        };

        private static readonly string[] LetOff =
        {
            "Go home and sleep it off.",
            "Walk away. I see you again today, you're mine.",
            "Count yourself lucky. Off with you.",
            "Wash the blood off and stay off my street.",
            "Go on. And explain that torn shirt at home.",
            "I'll let it go this once. Once.",
            "Go cool your head in the trough.",
            "Get to the doctor's and keep out of trouble.",
            "You're not worth a night's lamp oil. Move along.",
            "Shake hands or don't. Just do it somewhere else.",
            "Next time it's a fine and a night inside.",
            "Off you go. I've got your name now.",
        };

        private static readonly string[] Taken =
        {
            "You had your chance. Now you've got the rope.",
            "Down you go. The judge can hear your side.",
            "I told you twice. That's the jail for you.",
            "Lie still. You've earned yourself a cell.",
            "Wrists. Now. We're walking to the jail.",
            "Fight me too, would you? Wrong man.",
            "That's enough out of you for one day.",
            "A night on the boards will cool you.",
            "You can explain it to the circuit judge.",
            "Stop struggling. You're only making it worse.",
            "There. Trussed like a Christmas goose.",
            "I warned you. Folks heard me warn you.",
        };

        private static readonly string[] Stops =
        {
            "All right, all right.",
            "He started it, Marshal.",
            "Enough. I've had enough.",
            "Easy. Nobody's hurt.",
            "Fine. Fine.",
            "Enough. I've no more fight in me.",
            "Ask anybody, he swung first.",
            "We were only settling something, sir.",
            "No harm meant, Marshal.",
            "I'm stepping back. See? Stepping back.",
            "It's over. It's over.",
            "Just a disagreement, is all.",
            "He'll keep. I can wait.",
            "My hands are down. Look.",
        };

        private static readonly string[] AtYouLines =
        {
            "And you! Don't think I didn't see you!",
            "You want some too, mister?",
            "Come here, you!",
            "That was my jaw you hit!",
            "Who asked you in, stranger?",
            "That one costs you, stranger!",
            "Think you're clever? Come on, then!",
            "Do that twice and see where it gets you!",
            "Stranger thinks he's a fighting man!",
            "You're next, big fellow!",
            "Keep out of it! Or don't, and get some!",
            "Mister, you just bought yourself a fight!",
        };

        public static string LawBreaksUp(string seed) { return BreakUp[Voice.Hash(seed + "|breakup") % BreakUp.Length]; }
        public static string LawLetsOff(string seed) { return LetOff[Voice.Hash(seed + "|letoff") % LetOff.Length]; }
        public static string LawTakes(string seed) { return Taken[Voice.Hash(seed + "|taken") % Taken.Length]; }
        public static string Stopping(string seed) { return Stops[Voice.Hash(seed + "|stops") % Stops.Length]; }
        public static string ComesAtYou(string seed) { return AtYouLines[Voice.Hash(seed + "|atyou") % AtYouLines.Length]; }

        // the badge across the street's words (FOURTEENHOUR item 5): the cry as he comes, the deputy who sends for help, the one
        // who lets a fair fight run
        private static readonly string[] Cries = { "Law! Law's coming!", "Here comes the deputy!", "Badge! Break off, he's coming!", "Marshal's on his way, boys!" };
        private static readonly string[] SendsHelp = { "Fetch the sheriff! Run!", "I need another man here! Go!", "Somebody get the office! Now!", "One of you, run for the sheriff!" };
        private static readonly string[] LetsRunLines = { "Let 'em have it out. Fair's fair.", "Two men, fair and square. I'll wait.", "Go on, then. Get it out of your systems.", "Fists, and nothing else. I'm watching." };
        public static string Cry(string seed) { return Cries[Voice.Hash(seed + "|cry") % Cries.Length]; }
        public static string SendsForHelp(string seed) { return SendsHelp[Voice.Hash(seed + "|help") % SendsHelp.Length]; }
        public static string LetsRunLine(string seed) { return LetsRunLines[Voice.Hash(seed + "|letsrun") % LetsRunLines.Length]; }
        public static IEnumerable<string> LawLines() { return Cries.Concat(SendsHelp).Concat(LetsRunLines); }
    }
}
