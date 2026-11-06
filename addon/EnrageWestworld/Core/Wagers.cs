// Wagers on a fight (FIVEDAY item 1; the 11-05 plan Lex asked for: "onlookers bet among themselves on a fight. After it,
// money changes hands between them ... the loser's backer grumbles, or refuses to pay, which can start a new quarrel").
// Who among the people watching a fistfight puts money on it, on whom, how much, and what the one who backed the loser
// does when it's over: pays, pays with a sour word, says he'll owe it, or won't pay at all. Words only: nobody is moved
// or tasked to bet (the ring that gathered people round a fight is off since settings 182, Lex 2026-11-01: onlookers
// froze), and Arthur is never part of it.
//
// Why it reads true (the psychologist's lens): a stake turns a watcher into a partisan, so the man who backed the loser
// feels the loss as his own, and he blames the fight before his judgement (a crooked fight, a lucky punch). Who welshes
// follows the person: the hot-tempered, the drunk and the low in honesty, against the self-possessed and the honest; a
// friend of the winner's backer pays up, because the friendship costs more than the money. In 1899 a fight in the street
// drew side bets as surely as a horse race did, and a welshed bet was a matter of honour. (And the game developer's: a
// refusal leaves a hostile tie between the two backers, which the street's own reasons pick up next time they meet, so one
// fight can seed the next quarrel between two men who never threw a punch in it.)
//
// The thoughts after it are memories (Minds.Lines.cs: wagerpaid, wagerowed, welsh). Plain C#, no game; seeded, so the same people at the same fight bet the same way. All words original to this add-on.
// The game side is Game/WestworldScript.Wagers.cs.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Wagers
    {
        /// The most bets made at one fight (each between two onlookers, one for each side), so a big crowd doesn't turn
        /// into a ticker of wagers.
        public const int MostBets = 3;

        /// How far apart two onlookers can stand and still strike a bet (they call it across to each other).
        public const float PairMetres = 9f;

        /// Whether this onlooker bets at all, and on which of the two (0 or 1; -1: no bet). A friend of one of the two backs
        /// him, nearly always; someone with bad blood against one backs the other, more often than not; anyone else bets by
        /// their nature (risk, drink and sociability for, self-control, anxiety and piety against), and the Devout never do.
        /// warmthA and warmthB: their tie to each fighter (-3..3, 0 for none).
        public static int Backs(Traits t, int warmthA, int warmthB, bool drunk, string devotion, string seed, out string why)
        {
            why = null;
            if (t == null)
            {
                why = "nobody the mind knows";
                return -1;
            }
            if (devotion == "Devout")
            {
                why = "devout: no gambling";
                return -1;
            }
            // loyalty first: a friend's money is on his friend (the closer tie, if they know both)
            int friend = warmthA >= 2 && warmthA >= warmthB ? 0 : warmthB >= 2 ? 1 : -1;
            if (friend >= 0)
            {
                why = "a friend of his";
                return Voice.Chance(seed + "|friend", 85) ? friend : -1;
            }
            int foe = warmthA <= -1 && warmthA < warmthB ? 1 : warmthB <= -1 ? 0 : -1;   // against the one they've bad blood with
            float c = 0.22f + 0.10f * t.Z[Traits.Risk] + 0.05f * t.Z[Traits.Sociability] - 0.06f * t.Z[Traits.SelfControl] - 0.05f * t.Z[Traits.Anxiety] - 0.05f * t.Z[Traits.Piety];
            if (drunk)
                c += 0.15f;
            if (devotion == "Churchgoer")
                c -= 0.08f;
            if (foe >= 0)
                c += 0.25f;
            c = Math.Max(0.03f, Math.Min(0.80f, c));
            int pct = (int)Math.Round(c * 100);
            if (!Voice.Chance(seed + "|bets", pct))
            {
                why = "keeps his money (" + pct + "%)";
                return -1;
            }
            why = (foe >= 0 ? "bad blood with the other" : drunk ? "drink in him" : "a sporting nature") + " (" + pct + "%)";
            return foe >= 0 ? foe : (Voice.Hash(seed + "|side") & 1);
        }

        /// The stake in cents: two bits, four bits, a dollar, two, five. The reckless and the drunk go higher.
        public static int Stake(Traits t, bool drunk, string seed)
        {
            int[] steps = { 25, 50, 100, 200, 500 };
            float lift = (t != null ? 0.6f * t.Z[Traits.Risk] - 0.3f * t.Z[Traits.SelfControl] : 0f) + (drunk ? 0.8f : 0f);
            int i = (int)((Voice.Hash(seed + "|stake") & 0x7fffffff) % 3) + (int)Math.Round(Math.Max(0f, Math.Min(2f, lift)));
            return steps[Math.Max(0, Math.Min(steps.Length - 1, i))];
        }

        /// The ways a lost bet ends.
        public const string Pays = "pays", Grumbles = "grumbles", Owes = "owes", Welshes = "welshes";

        /// What the one who backed the loser does: pays, pays with a sour word, says he'll owe it, or won't pay. The
        /// hot-tempered, the drunk and the dishonest welsh; the honest and the self-possessed pay; a friend of the winner's
        /// backer pays (or owes), never welshes on him; a lawman near makes a welsher think twice; a stake past what a man
        /// carries (two dollars and up) is owed oftener. warmth: the tie between the two backers. pct: the welsh chance.
        public static string Settle(Traits t, bool drunk, int warmth, bool lawNear, int cents, string seed, out int pct)
        {
            float w = 0.12f;
            if (t != null)
                w += 0.08f * t.Z[Traits.Anger] - 0.09f * t.Z[Traits.Honesty] - 0.05f * t.Z[Traits.SelfControl] + 0.03f * t.Z[Traits.Assertiveness];
            if (drunk)
                w += 0.10f;
            if (lawNear)
                w -= 0.06f;
            if (warmth >= 2)
                w = 0f;   // not on a friend
            else if (warmth < 0)
                w += 0.10f;   // and gladly on a man he already dislikes
            w = Math.Max(0f, Math.Min(0.60f, w));
            pct = (int)Math.Round(w * 100);
            if (pct > 0 && Voice.Chance(seed + "|welsh", pct))
                return Welshes;
            int owe = cents >= 200 ? 35 : 10;
            if (Voice.Chance(seed + "|owe", owe))
                return Owes;
            float sour = 0.35f + (t != null ? 0.10f * t.Z[Traits.Anger] - 0.08f * t.Z[Traits.Compassion] : 0f);
            return Voice.Chance(seed + "|sour", (int)Math.Round(Math.Max(0.05f, Math.Min(0.9f, sour)) * 100)) ? Grumbles : Pays;
        }

        /// The stake in words: "two bits", "four bits", "a dollar", "two dollars", "five dollars".
        public static string Money(int cents)
        {
            switch (cents)
            {
                case 25: return "two bits";
                case 50: return "four bits";
                case 100: return "a dollar";
                case 200: return "two dollars";
                case 500: return "five dollars";
                default: return cents >= 100 ? (cents / 100) + " dollars" : cents + " cents";
            }
        }

        /// The scene kind a settled bet leaves between the two backers (Ties.Effect, Minds.GroupOf): paid, owed, welshed.
        public static string TieKind(string how)
        {
            return how == Welshes ? "welsh" : how == Owes ? "wagerowed" : "wagerpaid";
        }

        // ---- the words (all our own) ---------------------------------------------------------------------------------
        // {stake} the money in words, {him} the fighter backed (a first name or "the big fellow"), {other} the other fighter,
        // {who} the other backer's first name or "friend".

        private static readonly string[] Offer =
        {
            "{stake} says {him} puts {other} in the dirt.",
            "I'll lay {stake} on {him}. Who'll take it?",
            "{stake} on {him}, and I'll give odds.",
            "Who wants {stake} against {him}? Anybody?",
            "My money's on {him}. {stake}, right here.",
            "{stake} on {him}. Look at the reach on him.",
            "Put me down for {stake} on {him}.",
            "{stake} {him} is the one on his feet when they're done.",
            "I've seen {other} fight. {stake} on {him}.",
            "{stake} on {him}, and you can hold the money, {who}.",
        };

        private static readonly string[] Take =
        {
            "Done. {stake} on {him}, and you're a fool.",
            "I'll take that. {him}'s got the heart for it.",
            "You're on, {who}. {stake}, and no crying after.",
            "Taken. {him} will make a liar of you.",
            "Shake on it. {stake} on {him}.",
            "{stake}? Easiest money I'll make all week.",
            "Done and done. {him} hits like a mule.",
            "I'll cover it. Don't go running off, {who}.",
        };

        private static readonly string[] Crow =
        {
            "Pay up, {who}! Told you so!",
            "That's {stake} of yours in my pocket, {who}.",
            "Never doubted him. Not for a second.",
            "Hand it over, {who}. A bet's a bet.",
            "What did I say? What did I say!",
            "{stake}, if you please, {who}. I'll drink to your health.",
            "Easiest money I ever made.",
            "You'll learn, {who}. Back the one with the hands.",
        };

        private static readonly string[] Paying =
        {
            "Here. Take it.",
            "Fair's fair. There's your money.",
            "Lucky punch, but there it is.",
            "Take it and choke on it. No, I don't mean that. Take it.",
            "There's {stake}. I'll win it back Saturday.",
            "Count it if you like. It's all there.",
        };

        private static readonly string[] Sour =
        {
            "He folded like a wet newspaper. Here's your money.",
            "That was no fair fight, and you know it. Here.",
            "Take it. And stop grinning, {who}.",
            "My last {stake}, and he drops like that.",
            "I'd have done better throwing it in the river.",
            "Here. Don't spend it all at once, you vulture.",
        };

        private static readonly string[] Owing =
        {
            "I'm a little short, {who}. Saturday, on my word.",
            "You'll have it. Give a man till payday.",
            "Haven't got it on me. You know I'm good for it.",
            "Put it on my slate, {who}. You'll get your {stake}.",
            "Not tonight. End of the week. I swear it.",
        };

        private static readonly string[] Welsh =
        {
            "That fight was fixed. Not a cent from me, not tonight.",
            "I'm not paying for a crooked fight, {who}.",
            "Bet? What bet? I don't recall a bet.",
            "He went down before it got started. All bets are off.",
            "You want your {stake}? Come and take it.",
            "Nobody shook on nothing. Go home, {who}.",
        };

        private static readonly string[] Cheated =
        {
            "You low dog! A bet's a bet, {who}!",
            "Welshing in front of the whole street? I'll remember this.",
            "Keep it, then. Everyone here saw what you are.",
            "I'll have that {stake} out of your hide, {who}.",
            "So that's your word. Worth nothing.",
            "Fine. But don't you ever ask me for a thing again.",
        };

        private static readonly string[] Off =
        {
            "Law broke it up. All bets off, then.",
            "No winner, no money. Keep your {stake}.",
            "Well, that settles nothing. Bets are off.",
        };

        public static string OfferLine(string seed) { return Pick(Offer, seed + "|offer"); }
        public static string TakeLine(string seed) { return Pick(Take, seed + "|take"); }
        public static string CrowLine(string seed) { return Pick(Crow, seed + "|crow"); }
        public static string OffLine(string seed) { return Pick(Off, seed + "|off"); }
        public static string CheatedLine(string seed) { return Pick(Cheated, seed + "|cheated"); }

        /// The loser's backer's words for how it ended.
        public static string LoserLine(string how, string seed)
        {
            switch (how)
            {
                case Welshes: return Pick(Welsh, seed + "|welsh");
                case Owes: return Pick(Owing, seed + "|owing");
                case Grumbles: return Pick(Sour, seed + "|sour");
                default: return Pick(Paying, seed + "|paying");
            }
        }

        /// Fills a line: the stake, the fighter backed and the other, the other backer.
        public static string Fill(string line, int cents, string him, string other, string who)
        {
            if (line == null)
                return null;
            string stake = Money(cents);
            string s = line.Replace("{stake}", stake).Replace("{him}", him ?? "the big fellow").Replace("{other}", other ?? "the other one").Replace("{who}", who ?? "friend");
            return s.Length > 0 ? char.ToUpperInvariant(s[0]) + s.Substring(1) : s;
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        /// Every line, for the offline checks (filled with sample names).
        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in new[] { Offer, Take, Crow, Paying, Sour, Owing, Welsh, Cheated, Off })
                foreach (string l in pool)
                    foreach (int c in new[] { 25, 100, 500 })
                        yield return Fill(l, c, "Hayes", "Calloway", "Amos");
        }
    }
}
