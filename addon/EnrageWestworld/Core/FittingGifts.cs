// A gift that fits (LIVES item 4c, 2026-11-12; [Scenes] FittingGifts, settings version 479; the roadmap's "Gifts" row).
// Talk's gift (Core/Fondness.cs) is the same for everyone (tobacco for a man, peaches for a woman), and Talk is frozen. Now,
// in a town's general store, J lets you buy a gift for one of up to three people of the town who are fond of you, chosen to
// fit who they are: a hymn book for the churchgoer, a bottle of rye for the man who keeps the saloon of an evening, a box of
// cigars for the well-off, a pair of work gloves for the farmer, a lace collar or a ribbon, a plug of tobacco. Within two game
// days, a few steps from them, J gives it. They answer by their nature: the warm are touched, the proud take it stiffly,
// the cold ask what it's for; a second gift inside a week warms them less ("You'll spoil me.").
//
// Why it reads true: a gift that fits tells them they were seen, and that, more than its price, is what binds (Mauss's
// "The Gift", 1925: a gift carries the giver; the psychology of gift-giving finds thoughtfulness, not cost, drives
// appreciation, Flynn and Adams, 2009). The diminishing second gift is the hedonic treadmill on a small scale.
// In memory only (the gift in hand; a reload forgets it). Plain C#; the game side (WestworldScript.FittingGifts.cs). Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class GiftFit
    {
        public string Item;   // "a hymn book"
        public int Cents;
    }

    public static class FittingGifts
    {
        public const string BuyKind = "giftbuy", GiveKind = "giftgive";
        public static bool On;   // [Scenes] FittingGifts, set by the game
        public const int LeastAffection = 20, Most = 3, KeepDays = 2, PromptMs = 10000, Liking = 15, LikingAgain = 5;
        public const float GiveMetres = 3.5f;

        /// What fits this person, by who they are (their routine's profile: trade group, devotion, drink, sex).
        public static GiftFit For(Routines.Profile p, bool woman)
        {
            if (p == null)
                return null;
            if (p.Devout || p.Churchgoer && !p.Drinker)
                return new GiftFit { Item = "a hymn book", Cents = 50 };
            if (woman)
                return p.Group == "gentry" || p.Group == "lady" ? new GiftFit { Item = "a lace collar", Cents = 75 } : p.Group == "girl" ? new GiftFit { Item = "a bottle of scent", Cents = 60 } : new GiftFit { Item = "a length of ribbon", Cents = 25 };
            if (p.Drinker)
                return new GiftFit { Item = "a bottle of rye", Cents = 50 };
            if (p.Group == "gentry" || p.Group == "gentleman")
                return new GiftFit { Item = "a box of cigars", Cents = 60 };
            if (p.Group == "farm")
                return new GiftFit { Item = "a pair of work gloves", Cents = 40 };
            return new GiftFit { Item = "a plug of tobacco", Cents = 15 };
        }

        /// How they take it: "warm", "stiff" (the proud) or "cold" (the cold and the greedy).
        public static string Takes(string temper)
        {
            return temper == "Proud" ? "stiff" : temper == "Cold" || temper == "Greedy" ? "cold" : "warm";
        }

        public static string Answer(string takes, bool again, string item, string seed)
        {
            string[] pool = again ? Again : takes == "stiff" ? Stiff : takes == "cold" ? Cold : Warm;
            return Voice.Pick(seed + "|ga", pool).Replace("{g}", Strip(item));
        }

        /// "hymn book" from "a hymn book".
        public static string Strip(string item)
        {
            if (item == null) return "gift";
            return item.StartsWith("a ") ? item.Substring(2) : item.StartsWith("an ") ? item.Substring(3) : item;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Warm, Stiff, Cold, Again })
                foreach (string l in a)
                    yield return l.Replace("{g}", "hymn book");
        }

        private static readonly string[] Warm = {
            "A {g}? For me? Well, I never. Thank you.",
            "Nobody's thought to bring me a thing in years. A {g}. Thank you kindly.",
            "How did you know? A {g}. You've a good eye, stranger." };
        private static readonly string[] Stiff = {
            "I'm not in want of anything. But a {g}. Well. I'll take it, since you went to the trouble.",
            "A {g}. Hm. That's good of you. You needn't have." };
        private static readonly string[] Cold = {
            "A {g}. And what's this in aid of?",
            "Nobody gives a {g} for nothing. But I'll keep it." };
        private static readonly string[] Again = {
            "Another? You'll spoil me.",
            "Twice in a week. Folk will talk.",
            "You're too good to me. Truly." };
    }
}
