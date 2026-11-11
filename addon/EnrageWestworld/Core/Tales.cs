// The story grows in the telling (FOURTEENHOUR item 24; [Mind] MemoriesBlur). Word of the player's deeds already bent as it travelled
// (Rumours.Version: the count grows, "killed" turns "gunned down", a flourish at low fidelity; TieGossip a hop at a time), but what the
// town did itself was told once, in the news box, and forgotten. Now a big brawl leaves a tale in the town (who was in it, how many,
// how many laid out, where and when, who started it), and each day and each telling bends it for those who weren't there: the count
// of men grows, the hour slides into the night, a knife appears, and the man who started it slides to one the teller holds in
// contempt (item 23). Those who were in it keep it nearest the truth, the more so if they were hurt in it (the painful stay sharp).
// So three days on, the town's brawl had a dozen men in it. It's told in the town's talk (the gossip scenes' rumours) and in the
// thoughts of those near you; after a week it's done. Why it reads true: Bartlett's "War of the Ghosts" (1932): each retelling bends
// a story toward what the teller expects and leaves out what doesn't fit; Allport and Postman (1947): levelling and sharpening, the
// striking detail grows; Loftus: what's said after gets into the memory. Plain C#, no game; all words this add-on's.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class TownTale
    {
        [DataMember(EmitDefaultValue = false)] public string Id, Kind, Town, Starter;   // Kind: "brawl"; Starter: the first name of the man who started it
        [DataMember(EmitDefaultValue = false)] public bool Saloon;
        [DataMember(EmitDefaultValue = false)] public int Count, Down, Hour, Told;      // Told: how many times it's been passed on
        [DataMember(EmitDefaultValue = false)] public long At;                          // game minute
        [DataMember(EmitDefaultValue = false)] public List<string> Were;                // the ids of those who were in it (the eyewitnesses)
        [DataMember(EmitDefaultValue = false)] public List<string> Hurt;                // ...and of those laid out in it
        [DataMember(EmitDefaultValue = false)] public string Held, How;                 // a holdup's (reserve R1): the one held up, and how it ended
        [DataMember(EmitDefaultValue = false)] public List<string> Saw;                 // the onlookers near (not in it): they tell it nearer the truth (the review of items 22-26)
        [DataMember(EmitDefaultValue = false)] public long ToldDay;                     // the game day it was last passed on: it grows a telling a day at most (the review of items 22-26)
    }

    public static class Tales
    {
        public static bool On = true;   // [Mind] MemoriesBlur, from the settings
        public const int Kept = 24, ThoughtGapMs = 90000;
        public static int LastsDays { get { return SessionClock.Days(7, 2); } }   // the session clock (DEPTH item 16): SessionClock.Days(as built, a session's)

        /// A big brawl's tale.
        public static TownTale Brawl(string town, bool saloon, int fighters, int down, string starter, long at, int hour, List<string> were, List<string> hurt)
        {
            return new TownTale { Id = Ids.New(), Kind = "brawl", Town = town, Saloon = saloon, Count = fighters, Down = down, Starter = starter, At = at, Hour = hour, Were = were, Hurt = hurt };
        }

        /// A man's pledge signed at the saloon door (FOURTEENHOUR item 25): told as the town's talk, and whether he held to it.
        public static TownTale Pledge(string town, string man, long at, int hour = 18)
        {
            return new TownTale { Id = Ids.New(), Kind = "pledge", Town = town, Starter = man, At = at, Hour = hour };   // (the hour: a pledge at dusk isn't "last night" (the review of items 22-26))
        }

        /// A holdup by the store (FOURTEENHOUR reserve R1): the robber and the one held up (first names), how it ended ("ran": he ran with it;
        /// "down": the stranger talked him down; "bolted": they ran for help; "shot": they ran and he fired; "drawn": cut short).
        /// Count is the robbers: one, grown into a gang in the telling.
        public static TownTale Holdup(string town, string robber, string held, string how, long at, int hour, List<string> were)
        {
            return new TownTale { Id = Ids.New(), Kind = "holdup", Town = town, Starter = robber, Held = held, How = how, Count = 1, At = at, Hour = hour, Were = were };
        }

        /// The tale as the town's talk has it (the rumour pool: "They say ...", "Word is ..."): one clause, no question, no second
        /// sentence, grown for those who weren't there (the review of items 22-26).
        public static string Rumour(TownTale t, long now)
        {
            if (!Live(t, now))
                return null;
            int days = (int)Math.Max(0, (now - t.At) / 1440);
            bool far = days >= 2 || t.Told >= 3;
            string when = When(t, now, far);
            if (t.Kind == "pledge")
                return (t.Starter ?? "a man") + " signed the pledge at the saloon door " + When(t, now, false);
            if (t.Kind == "holdup")
            {
                int r = CountNow(t, now, false, false);
                return (t.Held ?? "somebody") + " was held up at the store " + when + " by " + (r <= 1 ? "a man with a gun" : r == 2 ? "two men" : "a gang of " + Count(r));
            }
            int n = CountNow(t, now, false, false);
            return "there was a brawl of " + Count(n) + " men at " + (t.Saloon ? "the saloon" : "the street") + " " + when + (t.Starter != null ? ", and " + t.Starter + " started it" : "");
        }

        /// Whether it's still talked of.
        public static bool Live(TownTale t, long now)
        {
            return t != null && now >= t.At && now - t.At < LastsDays * 1440L;
        }

        static readonly string[] Numbers = { "no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen" };
        static string Count(int n) { return n >= 0 && n < Numbers.Length ? Numbers[n] : n.ToString(); }

        /// How many it had in it as told now: the truth for one who was in it (a little more with the days, never for the hurt), else
        /// growing with the days and with each telling, to two and a half times at most.
        public static int CountNow(TownTale t, long now, bool was, bool hurt)
        {
            int days = (int)Math.Max(0, (now - t.At) / 1440);
            if (hurt)
                return t.Count;
            double grow = was ? 1.0 + 0.05 * days : 1.0 + 0.25 * days + 0.10 * t.Told;
            return Math.Max(t.Count, Math.Min(16, (int)Math.Round(t.Count * Math.Min(2.5, grow))));
        }

        /// A holdup as told now (reserve R1): the one held up tells it true; told on, the one man grows into two, then a gang, the sum
        /// grows, the stranger who talked him down becomes a man who faced him down, and the name slides to one the teller scorns.
        static string HoldupTelling(TownTale t, long now, bool was, string blamed, string seed, bool saw = false)
        {
            int days = (int)Math.Max(0, (now - t.At) / 1440);
            string when = When(t, now, false);
            string held = t.Held ?? "somebody";
            if (saw && !was)
                return "I saw " + held + " held up at the store " + when + ". One man, and a gun in his hand.";
            if (was)
                return t.How == "down" ? Voice.Pick(seed + "|hw0", "He had a gun in my face at the store " + when + ". A stranger talked him out of it. I'd know that stranger anywhere.",
                                                     "I was held up at the store " + when + ". If that stranger hadn't spoken up, I'd be poorer, or worse.")
                     : t.How == "bolted" || t.How == "shot" ? Voice.Pick(seed + "|hw1", "I ran from a gun at the store " + when + ". It rings in my ears yet.", "He held me up at the store " + when + " and I ran. Fool thing to do. I'd do it again.")
                     : Voice.Pick(seed + "|hw2", "He held me up at the store " + when + ". I see that gun when I close my eyes.", "I was robbed at the store " + when + ", in broad day. Every cent.");
            int n = CountNow(t, now, false, false);   // the one robber, grown
            string them = n <= 1 ? "a man" : n == 2 ? "two men" : "a gang of " + Count(n);
            string who = !was && blamed != null && days >= 1 ? blamed : t.Starter;
            bool far = days >= 2 || t.Told >= 3;
            string told = Voice.Pick(seed + "|ht", Grammar.UpperFirst(held) + " was held up at the store " + when + " by " + them + ", they say",
                                     "Did you hear? " + Grammar.UpperFirst(them) + " held up " + held + " by the store " + when);
            if (who != null && n <= 2)
                told += ", " + Voice.Pick(seed + "|hn", "and it was " + who, "and they're saying it was " + who);
            if (t.How == "down")
                told += far ? Voice.Pick(seed + "|hd", ". A stranger faced the lot of them down.", ". Some stranger stared them down, gun to gun.") : ". A stranger talked him out of it.";
            else if (t.How == "shot")
                told += ". Shots in the street, in broad day.";
            else if (t.How == "bolted")
                told += far ? ". " + Grammar.UpperFirst(held) + " ran, and bullets flew." : ". " + Grammar.UpperFirst(held) + " ran for it.";
            else
                told += far ? Voice.Pick(seed + "|hs", ". Took a fortune off them.", ". Cleaned them out, every cent and the watch.") : ".";
            return told;
        }

        static string When(TownTale t, long now, bool shifted)
        {
            int days = (int)Math.Max(0, (now - t.At) / 1440);
            int hour = shifted ? Math.Min(23, t.Hour + 3) : t.Hour;   // told on, an evening's brawl was "in the night"
            string part = hour >= 22 || hour < 5 ? "in the night" : hour >= 17 ? "of an evening" : hour >= 12 ? "in the afternoon" : "in the morning";
            return days == 0 ? (part == "in the night" ? "last night" : "today") : days == 1 ? "yesterday" : days < 7 ? Count(days) + " days back" : "last week";
        }

        /// The tale as one tells it now. `was`: they were in it; `hurt`: laid out in it; `blamed`: someone the teller holds in
        /// contempt, whom the starting of it slides to (item 23), else null. Null when it's done.
        public static string Telling(TownTale t, long now, bool was, bool hurt, string blamed, string seed, bool saw = false)
        {
            if (!Live(t, now))
                return null;
            int days = (int)Math.Max(0, (now - t.At) / 1440);
            if (t.Kind == "pledge")
            {
                string man = t.Starter ?? "A man";
                if (days < 3)
                    return Voice.Pick(seed + "|p0", man + " signed the pledge at the saloon door " + When(t, now, false) + ".", "Heard " + man + " signed the pledge? Give it a week.");
                return Voice.Chance(t.Id + "|held", 60)
                    ? Voice.Pick(seed + "|p1", man + " signed the pledge, and he was back at the brass rail by Friday.", man + "'s pledge lasted till payday. They always do.")
                    : Voice.Pick(seed + "|p2", man + " signed the pledge, and he's held to it, they say.", man + " hasn't touched a drop since he signed. Who'd have thought it.");
            }
            if (t.Kind == "holdup")
                return HoldupTelling(t, now, was, blamed, seed, saw);
            int n = CountNow(t, now, was, hurt);
            bool far = !was && (days >= 2 || t.Told >= 3);
            bool knife = far && Voice.Chance(seed + "|knife", 45);
            string where = t.Saloon ? "the saloon" : "the street";
            string when = When(t, now, far);
            string starter = !was && blamed != null && days >= 1 ? blamed : t.Starter;
            if (was)
                return hurt ? Voice.Pick(seed + "|hurt", "My ribs still remember " + where + " " + when + ". " + Grammar.UpperFirst(Count(t.Count)) + " of us at it, no more.",
                                                         "I was laid out in that brawl " + when + ". " + Grammar.UpperFirst(Count(t.Count)) + " men, and I felt every one.")
                            : Voice.Pick(seed + "|was", "I was in that brawl at " + where + " " + when + ". " + Grammar.UpperFirst(Count(n)) + " of us, and " + (t.Down > 0 ? Count(t.Down) : "none") + " laid out.",
                                                        "That fight " + when + ", I was in it. " + Grammar.UpperFirst(Count(n)) + " men, near enough.");
            if (saw)
                return Voice.Pick(seed + "|saw", "I saw that brawl at " + where + " " + When(t, now, false) + ". " + Grammar.UpperFirst(Count(t.Count)) + " men at it, no more.",
                                                 "I watched that fight " + When(t, now, false) + ". " + Grammar.UpperFirst(Count(t.Count)) + " of them, and " + (t.Down > 0 ? Count(t.Down) : "none") + " laid out.");   // an onlooker: nearer the truth (the review of items 22-26)
            string tail = knife ? Voice.Pick(seed + "|k", ", and one pulled a knife", ", and a knife came out", ", and somebody had a blade") : "";
            string told = Voice.Pick(seed + "|t", "That brawl at " + where + " " + when + ": " + Count(n) + " men at it, they say", "Did you hear? " + Grammar.UpperFirst(Count(n)) + " men fighting at " + where + " " + when) + tail + ".";
            if (starter != null)
                told += " " + Voice.Pick(seed + "|s", Grammar.UpperFirst(starter) + " started it.", "It was " + starter + " began it.");   // ("a stranger" too)
            return told;
        }
    }
}
