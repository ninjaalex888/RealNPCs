// After a fistfight, the beaten man remembers (FIVEDAY item 2; the 11-05 plan: "the loser remembers. Days later, when the
// two meet, there's a rematch, a sullen crossing of the street, or a handshake, depending on their natures"). When the
// Director says a fight between two of the street's people is over, the one left on the ground takes a plan toward the one
// left standing (Core/Plans.cs carries it, kept in the save): "rematch" (he wants it again), "shun" (he keeps out of the
// winner's way) or "shake" (he offers his hand), or none at all (he lets it lie). The plan waits half a game day, then,
// when the two are next near each other and near the player, the street's reasons lean to its scene: a rematch is a
// brawl with its own opening words, the shunning is the street's "avoid" (the winner says his piece, the loser walks
// off), and the handshake is a scene of its own here (taken, or refused by a man with bad blood left from before).
//
// Why it reads true (the psychologist's lens): a public defeat is a loss of standing, and people repair it by the means
// their nature gives them. The hot and the proud need the score evened in front of the same street (honour, more so in
// Lemoyne); the anxious avoid the man who hurt them, which is how fear is learned; the agreeable repair the tie instead,
// because a feud costs more than a black eye. Most let it lie: memory fades for the steady. (And the game developer's:
// a fight leaves a thread the player may see picked up days later, by people who were only passers-by to him.)
//
// Plain C#, no game; seeded. All words original to this add-on. The game side is in WestworldScript.Plans.cs (the
// plan made from FightOver, the half day's rest) and the scenes through the Director as any street scene.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Rematch
    {
        /// The ways the beaten man carries it: the plan kinds (Plans.cs).
        public const string Again = "rematch", Shun = "shun", Shake = "shake";

        /// Game minutes after the fight before the plan acts: half a day, so it's "days later", not the same street.
        public const long RestMinutes = 720;

        /// How long the plan keeps (days): past that he's let it go.
        public const int Days = 4;

        /// How the beaten man takes it, by his nature: a rematch for the hot and the proud (never a woman against a man, and
        /// only one who fights: the brawl's own rule, WomenInFights, is the game side's); shunning for the anxious and the
        /// meek; a handshake for the kind and the trusting, more so when they were on good terms before; else he lets it
        /// lie (null). warmthBefore: their tie before the fight (-3..3). Seeded by the two and the day.
        public static string Way(Traits t, bool woman, int warmthBefore, bool beatenBadly, string seed, out string why)
        {
            why = null;
            if (t == null)
            {
                why = "nobody the mind knows";
                return null;
            }
            float again = 0.10f + 0.10f * t.Z[Traits.Anger] + 0.08f * t.Z[Traits.Assertiveness] + 0.05f * t.Z[Traits.Risk] - 0.06f * t.Z[Traits.SelfControl] - 0.05f * t.Z[Traits.Anxiety];
            float shun = 0.10f + 0.10f * t.Z[Traits.Anxiety] - 0.07f * t.Z[Traits.Assertiveness] - 0.04f * t.Z[Traits.Risk];
            float shake = 0.10f + 0.08f * t.Z[Traits.Compassion] + 0.06f * t.Z[Traits.Trust] - 0.05f * t.Z[Traits.Anger];
            if (beatenBadly)
            {
                shun += 0.08f;   // knocked cold: fear weighs more than pride
                again -= 0.04f;
            }
            if (warmthBefore >= 1)
            {
                shake += 0.15f;   // old friends mend it
                again -= 0.05f;
            }
            else if (warmthBefore <= -2)
            {
                again += 0.10f;   // it was coming before the fight; it isn't over now
                shake -= 0.08f;
            }
            if (woman)
                again *= 0.4f;    // most women of 1899 settled it otherwise; the fight itself already was the rare case
            again = Math.Max(0.02f, Math.Min(0.6f, again));
            shun = Math.Max(0.02f, Math.Min(0.5f, shun));
            shake = Math.Max(0.02f, Math.Min(0.5f, shake));
            int roll = (Voice.Hash(seed + "|way") & 0x7fffffff) % 1000;
            int ia = (int)(again * 1000), isn = (int)(shun * 1000), ish = (int)(shake * 1000);
            why = "rematch " + ia / 10 + "%, shun " + isn / 10 + "%, shake " + ish / 10 + "%";
            if (roll < ia)
                return Again;
            if (roll < ia + isn)
                return Shun;
            if (roll < ia + isn + ish)
                return Shake;
            return null;
        }

        // ---- the scenes (A is the one who lost the first fight, B the one who won it) ---------------------------------

        private static readonly string[][] Openers =
        {
            new[] { "You and me, {b}. Round two.", "Got lucky the other day. Let's see you do it twice." },
            new[] { "Been looking for you, {b}.", "Didn't get enough last time?" },
            new[] { "Everybody saw you put me down, {b}. Everybody's going to see this.", "Go home, {a}. Once was enough." },
            new[] { "My jaw still aches, {b}. Yours is about to.", "You really want this again?" },
            new[] { "I don't lose to the same man twice.", "You lost to me once. Twice comes easier." },
            new[] { "I've had a few days to think on it, {b}.", "Think harder. You'll lose the same way." },
        };

        private static readonly string[] AgainWon = { "Now we're square.", "That's how it should've gone the first time.", "Tell them about that one, {b}." };
        private static readonly string[] AgainLost = { "Twice. God help me, twice.", "Leave it, {a}. Just leave it.", "All right. You're the better man. Today." };

        /// A rematch: a brawl with its own words. Kind "brawl", so the tie, the memories and the aftermath are the brawl's.
        public static Scene AgainScene(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string[] pair = Openers[(Voice.Hash(seed + "|open") & 0x7fffffff) % Openers.Length];
            Scene s = new Scene { Kind = "brawl", Fight = true, Summary = "a rematch: two who'd fought before came to blows again" };
            s.Add(0, Fill(pair[0], a, b), "angry", "face").Add(1, Fill(pair[1], a, b), "threat")
             .Add(0, null, "angry", "fight", 9000)
             .Add(0, null, null, "endfight")
             .Add(0, Fill(Pick(AgainWon, seed + "|aw"), a, b), "won")
             .Add(1, Fill(Pick(AgainLost, seed + "|al"), a, b), "mild", "walkaway");
            return s;
        }

        private static readonly string[] Offer =
        {
            "{b}. About the other day. No hard feelings?",
            "Hold up, {b}. I was in the wrong. Here's my hand.",
            "I've a sore head and a lesson learned, {b}. Shake on it?",
            "We were both fools, {b}. Me the bigger one. Friends?",
            "Wanted to say it plain, {b}: it's done with, far as I'm concerned.",
            "You hit hard, {b}. I'd sooner have you on my side. Shake?",
        };

        private static readonly string[] Taken =
        {
            "Done. You've a hard head, {a}, I'll say that.",
            "No hard feelings. Buy me a drink and we'll call it square.",
            "Fair enough. I'd not want to do that again either.",
            "Took guts to come over here. Shake.",
            "It's done with, {a}. Forgotten by Sunday.",
        };

        private static readonly string[] Refused =
        {
            "Keep your hand. I've not forgotten what came before.",
            "You think a handshake mends it? Walk on, {a}.",
            "Some other day, maybe. Not today.",
            "I'll shake when you've paid what you owe me.",
        };

        private static readonly string[] AfterTaken = { "Glad that's done.", "There. Better.", "Should've done that sooner." };
        private static readonly string[] AfterRefused = { "Suit yourself, then.", "Tried. Can't say I didn't.", "Your choice, {b}. I offered." };

        /// A handshake offered by the one who lost (A): taken, so the tie mends ("makeup"), or refused by a man with bad
        /// blood left between them from before the fight, about half the time ("brushoff").
        public static Scene ShakeScene(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            bool tied;
            int warmth = c.WarmthBetween(0, 1, out tied);
            bool taken = warmth > -2 || Voice.Chance(seed + "|takes", 50);
            Scene s = new Scene { Kind = taken ? "makeup" : "brushoff", Doer = taken ? -1 : 1, Summary = taken ? "two who'd fought shook hands in the street" : "a handshake after a fight was refused" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Fill(Pick(Offer, seed + "|of"), a, b), "mild", "face")
             .Add(1, Fill(Pick(taken ? Taken : Refused, seed + "|an"), a, b), taken ? "mild" : "cold")
             .Add(0, Fill(Pick(taken ? AfterTaken : AfterRefused, seed + "|af"), a, b), taken ? "thanks" : "mild", "walkaway");
            return s;
        }

        // ---- the plan's own words (Plans.Words, Plans.Thought) -------------------------------------------------------

        public static string Words(string kind, string t)
        {
            return kind == Again ? "have it out again with " + t : kind == Shun ? "keep clear of " + t : "shake " + t + "'s hand";
        }

        public static string Thought(string kind, string t, string seed)
        {
            switch (kind)
            {
                case Again:
                    return Voice.Pick(seed, t + " got the first one. Not the second.", "Keep seeing " + t + " standing over me. Next time it's the other way.",
                                      "Whole street watched " + t + " put me down. They'll watch the rest.", "Jaw's healing. Pride isn't.");
                case Shun:
                    return Voice.Pick(seed, "If " + t + " is about, I'll take the back way.", "Not looking for " + t + ". Not looking for anybody.",
                                      "Ribs still hurt when I breathe. " + t + " can have the street.", "Saw " + t + " once since. Turned right around.");
                default:
                    return Voice.Pick(seed, "Ought to go and make it right with " + t + ".", "Fighting " + t + " was a fool's business. I'll say so.",
                                      "A town's too small to keep a quarrel with " + t + ".", "Next time I see " + t + ", I'll hold out my hand.");
            }
        }

        private static string Fill(string line, string a, string b)
        {
            return line.Replace("{a}", a ?? "friend").Replace("{b}", b ?? "friend");
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pair in Openers)
                foreach (string l in pair)
                    yield return Fill(l, "Amos", "Hiram");
            foreach (string[] pool in new[] { AgainWon, AgainLost, Offer, Taken, Refused, AfterTaken, AfterRefused })
                foreach (string l in pool)
                    yield return Fill(l, "Amos", "Hiram");
            foreach (string k in new[] { Again, Shun, Shake })
                for (int i = 0; i < 12; i++)
                    yield return Thought(k, "Hiram", k + i);
        }
    }
}
