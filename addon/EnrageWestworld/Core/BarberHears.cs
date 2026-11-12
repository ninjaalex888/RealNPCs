// The barber hears it all (LIVES item 1e, 2026-11-12; [Mind] BarberHears, settings version 465). The undertaker counts the
// week's dead (Core/UndertakerWeek.cs) and the telegraph clerk knows the wire first (Core/Telegraph.cs); the barber had only
// a few lines of his own trade. In 1899 the barber's chair was the town's exchange: every man in it talked, and the barber
// heard every version. Now his thoughts come from what the town really holds: a quarrel or a courtship between two of his
// town these two days, by name ("Three men in the chair, three tellings of Hollis and Cobb."), the stranger's deeds as the
// town tells them, bent ("Heard about the stranger four times today. He grows a foot each telling."), and a townsman
// of his who died this week, whose face he shaved ("Shaved Elwood Saturday last. Last thing he said to me was about rain.").
//
// The psychology: a hub in a gossip network hears more versions than anyone, and so is the one most aware that stories
// change in the telling (Allport and Postman's "Psychology of Rumor", 1947: levelling, sharpening, assimilation); and the
// small intimacy of a trade that touches faces makes a customer's death personal in a way the town's talk isn't.
//
// No new state: it reads the town's ties of two days (Ties.TalkOf), its rumours of the player, and the deaths (Store.Deaths).
// Engine.PostThought offers it as one of his thoughts. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class BarberHears
    {
        public const string Kind = "barber";
        public const int Pull = 40, DeathDays = 7;

        /// Off in Core so the harness goldens stand; the game's settings ([Mind] BarberHears) turn it on.
        public static bool On;

        /// His thought, or null: a death of his town this week first (a customer gone is the sharpest), else, by the seed, the
        /// town's talk of two of its people or the stranger's deeds as told here.
        public static string Thought(SceneTie talk, string rumourText, int rumourFidelity, string deadName, string seed)
        {
            if (!On)
                return null;
            if (deadName != null && Voice.Chance(seed + "|bd", 60))
                return Voice.Pick(seed + "|bdl", Dead).Replace("{n}", Person.FirstOf(deadName) ?? deadName);
            bool tieFirst = talk != null && (rumourText == null || Voice.Chance(seed + "|bt", 55));
            if (tieFirst)
            {
                string a = Person.FirstOf(talk.AName), b = Person.FirstOf(talk.BName);
                if (a == null || b == null)
                    return null;
                string[] pool = talk.Warmth < 0 ? Quarrel : Ties.Romantic(talk.Kind) ? Courting : Kind_;
                return Voice.Pick(seed + "|btl", pool).Replace("{a}", a).Replace("{b}", b);
            }
            if (rumourText != null)
                return Voice.Pick(seed + "|brl", rumourFidelity < 70 ? Bent : Told);
            return null;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in new[] { Dead, Quarrel, Courting, Kind_, Bent, Told })
                foreach (string l in pool)
                    yield return l;
        }

        private static readonly string[] Dead = {
            "Shaved {n} Saturday last. Last thing he said to me was about rain.",
            "{n}'s mug is still on the shelf with his name on it. Can't bring myself to move it.",
            "Had {n} in the chair a hundred times. Never once did he sit still. Now look." };
        private static readonly string[] Quarrel = {
            "Three men in the chair, three tellings of {a} and {b}. None of them agree but on the shouting.",
            "{a} and {b} again. I keep the razor steady and my opinions in the drawer.",
            "Everybody in that chair today had a side in {a} and {b}'s trouble. I had a full till." };
        private static readonly string[] Courting = {
            "{a} walking out with {b}, so the chair says. Half the town knew before {b} did.",
            "Trimmed a young man's hair for a Sunday. Said it wasn't for {b}. It was for {b}.",
            "Every lather this week, somebody's got a word on {a} and {b}." };
        private static readonly string[] Kind_ = {
            "Heard {a} did {b} a good turn. Doesn't travel half as fast as a bad one.",
            "Somebody said {a} and {b} have made it up. Took the shine off the week's talk." };
        private static readonly string[] Bent = {
            "Heard about the stranger four times today. He grows a foot each telling.",
            "By noon the stranger had shot six men. By supper, nine. I shave and say nothing.",
            "A man in the chair swore he saw it himself. The man before swore the same, and they don't match." };
        private static readonly string[] Told = {
            "Nobody in the chair talks of anything but the stranger. I could recite it.",
            "Every man who sits down wants to tell me what the stranger did. I nod. Pays the same.",
            "The stranger's the talk of the chair. Kept my eyes on the razor when he walked past the window." };
    }
}
