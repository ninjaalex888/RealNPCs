// Thoughts that follow on (FIVEDAY2 item 27; [Ticker] ThoughtsFollowOn). Someone's loud moment (a fight, a run for it, a gun in their face, a body) used to end and leave no mark: the next
// thought was about the stew. Now, for a few minutes after, their next thoughts or two pick up from it, by what it was and their temper. With the AI model on it's told too what they were
// just through, and one of their asks in three is told their last thought and to carry on from it. Why it reads true: the mind doesn't change the subject when the danger stops. Rumination
// is what comes next, and it takes the shape of the person: the proud replay it with a better ending, the frightened can't put it down. All lines original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class ThreadNote
    {
        public string Family;
        public int At, Follows;
    }

    public static class ThoughtThreads
    {
        public const int KeepMs = 300000, MostFollows = 2, Chance = 75, CarryOnMs = 180000, CarryOnChance = 33;

        /// What kind of thing a loud moment was (Moments' source, "moment:fightyou:fists"); null for one nothing follows from.
        public static string FamilyOf(string source)
        {
            string s = source != null && source.StartsWith("moment:", StringComparison.Ordinal) ? source.Substring(7) : source;
            switch (s)
            {
                case "fightyou:fists": case "fight:fists": case "fight:hurt": case "fight:awake": case "fightyou:fists:woman": case "fight:fists:woman": case "down": case "hurt": return "fought";
                case "fightyou:guns": case "fight:guns": return "guns";
                case "fleeing": return "fled";
                case "aimed": return "aimed";
                case "shots": return "shots";
                case "watching": return "watched";
                case "body": return "body";
                case "spooked": return "spooked";
                default: return null;
            }
        }

        // by family, then bold, fearful, cold, plain
        private static readonly Dictionary<string, string[][]> After_ = new Dictionary<string, string[][]>
        {
            { "fought", new[] {
                new[] { "Should've hit twice.", "I've had worse at a wedding.", "Next time I'll finish it." },
                new[] { "My hands won't stop shaking.", "I thought that was it. I really did.", "Never again. I'll say I was sick." },
                new[] { "Cost me a tooth. Somebody pays for the tooth.", "Learned a few things there.", "I'll remember which hand it was." },
                new[] { "My jaw's going to feel that tomorrow.", "What started it? What on earth started it?", "I'm too old for this." } } },
            { "guns", new[] {
                new[] { "Still breathing. Let them count that.", "Reload first. Shake later.", "That's what I think of that." },
                new[] { "My ears are ringing. Is that blood?", "Why am I alive? Why me and not him?", "I've wet myself. Lord. Nobody look." },
                new[] { "Note where the shots came from.", "Dead men's pockets don't stay full.", "Not a scratch. Remember that for the bill." },
                new[] { "I never knew it'd be that loud.", "I think I'm all right. Am I all right?", "Somebody's going to have to tell his mother." } } },
            { "fled", new[] {
                new[] { "I wasn't running. I was going somewhere fast.", "I'll go back. In a minute.", "Nobody saw that. Nobody better say so." },
                new[] { "Stopped running. Legs haven't heard.", "Is it still back there? Don't look.", "I'll never go that way again." },
                new[] { "Alive, and ran well. That's the whole of it.", "Let the brave ones bury each other.", "A good pair of boots. Worth every cent." },
                new[] { "Left my hat. Not going back for it.", "My heart's going like a mill.", "I don't even know what I ran from." } } },
            { "aimed", new[] {
                new[] { "Pointed and didn't use it. That tells me something.", "Point one at me again and see.", "I didn't blink. Did I blink?" },
                new[] { "I can still see the barrel of it.", "So small, that hole. So small.", "Why didn't I say something? What would I say?" },
                new[] { "Now I know what kind of man that is.", "The law'll want a description. Or someone will.", "Worth knowing how quick that hand is." },
                new[] { "I've got family. I kept thinking that.", "What did I do? I didn't do anything.", "I'd like to sit down now." } } },
            { "shots", new[] {
                new[] { "Somebody's having a worse day than me.", "I should have gone to look.", "Sounded like a Cattleman. Sounded like two." },
                new[] { "It's stopped. Has it stopped?", "That was close. That was too close.", "Don't go out. Don't go out yet." },
                new[] { "Somebody'll be selling those boots by noon.", "Count's not in yet.", "Bad for trade, or good for it. Depends." },
                new[] { "This place isn't what it was.", "And I'd only just got the washing out.", "Somebody's mother heard that too." } } },
            { "watched", new[] {
                new[] { "Dropped the left. I'd have had him.", "That was no fight. I'll show them a fight.", "Three more and I'd have stepped in." },
                new[] { "I shouldn't have watched. I always watch.", "That sound. That sound a nose makes.", "What if it had come my way?" },
                new[] { "Two dollars I could have made on that.", "Nobody's the better for it.", "Worth knowing who stepped in and who didn't." },
                new[] { "Never saw so much blood from one nose.", "And they'll drink together by Friday.", "Somebody ought to see to that one's eye." } } },
            { "body", new[] {
                new[] { "Seen plenty. They all look the same.", "Somebody'll answer for that one.", "Better than I'd expect, for a dead man." },
                new[] { "I keep seeing it. Eyes shut and I keep seeing it.", "It was looking at me. I know it wasn't.", "Don't touch anything. Don't touch anything." },
                new[] { "Good boots, those were.", "Who gains by it? Somebody gains.", "One less at the table." },
                new[] { "That was somebody's son.", "I'll say a word for that one tonight.", "Not a thing you get used to." } } },
            { "spooked", new[] {
                new[] { "Stupid animal. Stupid, stupid animal.", "I stayed on. Let the record show.", "She'll get no sugar for that." },
                new[] { "I nearly died on a horse. On a Tuesday.", "My hands. Look at my hands.", "I'll walk from here. I'll walk." },
                new[] { "Sell her. First fool with a dollar.", "Something spooked her. What spooked her?", "That's a bad horse, or a bad road." },
                new[] { "She's never done that before.", "There now. There now, girl. Both of us.", "Wonder what she saw." } } },
        };

        // what the AI model is told they've just been through
        private static readonly Dictionary<string, string> Told = new Dictionary<string, string>
        {
            { "fought", "the fight they were just in" }, { "guns", "the shooting they were just in" }, { "fled", "having just run for their life" },
            { "aimed", "the gun that was pointed at them a moment ago" }, { "shots", "the shots they heard close by a moment ago" }, { "watched", "the fight they just watched" },
            { "body", "the dead body they just saw" }, { "spooked", "their horse bolting under them a moment ago" },
        };

        /// Whether this note is still on their mind.
        public static bool Open(ThreadNote n, int now)
        {
            return n != null && n.Follows < MostFollows && now >= n.At && now - n.At < KeepMs;
        }

        /// The thought that follows on. `nth`: the first or second since it happened, so the two aren't the same line.
        public static string After(string family, string temper, int nth, string seed)
        {
            string[][] byGroup;
            if (family == null || !After_.TryGetValue(family, out byGroup))
                return null;
            string[] pool = byGroup[GroupOf(temper)];
            return pool[(Voice.Hash(seed + "|after") + Math.Max(0, nth)) % pool.Length];
        }

        /// What the model is told, in a few words, for the time it's still on their mind.
        public static string Words(string family)
        {
            string w;
            return family != null && Told.TryGetValue(family, out w) ? w : null;
        }

        /// Whether this ask carries on from their last thought: only one that's still fresh, and not every time, or they'd never think of anything else.
        public static bool CarriesOn(int msSince, string seed)
        {
            return msSince >= 0 && msSince < CarryOnMs && Voice.Chance(seed + "|carry", CarryOnChance);
        }

        private static int GroupOf(string temper)
        {
            switch (Minds.TemperGroup(temper))
            {
                case "bold": return 0;
                case "fearful": return 1;
                case "cold": return 2;
                default: return 3;
            }
        }

        // the morning after (THREEHOUR R2; [Ticker] ThreadsNextDay): a resident's loud moment is kept in the save, by its day; their first
        // quiet thought the next game day may pick it up again, once that day. Why it reads true: a fright or a fight comes back in the
        // morning, smaller, with sleep between, and it's what people walk around with that makes a town feel lived in.
        public const int NextDayChance = 50;

        private static readonly Dictionary<string, string[]> NextDay_ = new Dictionary<string, string[]>
        {
            { "fought", new[] { "My knuckles told me about yesterday before I woke.", "Still can't say what yesterday's fight was even about.", "Folk look at me sideways today. On account of yesterday." } },
            { "guns", new[] { "Slept in my boots after yesterday's shooting.", "Every door that bangs today, I'm back in it.", "Bullets yesterday. Flour and coffee today. Strange world." } },
            { "fled", new[] { "Ran like a jackrabbit yesterday. Walked slow today.", "Hardly slept. Kept running in my sleep.", "Somebody saw me run off yesterday. I'd bet on it." } },
            { "aimed", new[] { "A man held a gun on me yesterday. Can't eat a bite today.", "That barrel was in my dreams all night.", "I keep saying what I should've said to him yesterday." } },
            { "shots", new[] { "The shooting yesterday. Nobody'll say who.", "Kept the children in today. Not after yesterday.", "Shots yesterday, and the town's quiet as a church today." } },
            { "watched", new[] { "Still chewing on that fight I saw yesterday.", "Yesterday's two fighters passed in the street. Not a word.", "I'd have done better than either of them yesterday." } },
            { "body", new[] { "That body yesterday. I saw it again all night.", "Wonder if they've buried the one I saw.", "Can't walk that way since yesterday." } },
            { "spooked", new[] { "My horse and I aren't speaking. Not since yesterday.", "Still sore from that horse yesterday.", "Went over every inch of the tack this morning." } },
        };

        /// The morning-after thought for what it was, or null.
        public static string NextDay(string family, string seed)
        {
            string[] pool;
            return family != null && NextDay_.TryGetValue(family, out pool) ? pool[Math.Abs(Voice.Hash(seed + "|nd") % pool.Length)] : null;
        }

        /// The day and what it was, packed in one number for the save's counts (day * 16 + the family's place).
        public static int Pack(long day, string family)
        {
            int i = new List<string>(After_.Keys).IndexOf(family);
            return i < 0 ? -1 : (int)(day * 16 + i);
        }

        public static bool Unpack(int packed, out long day, out string family)
        {
            day = 0;
            family = null;
            if (packed < 0)
                return false;
            var keys = new List<string>(After_.Keys);
            int i = packed % 16;
            if (i >= keys.Count)
                return false;
            day = packed / 16;
            family = keys[i];
            return true;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] t in After_.Values)
                foreach (string[] g in t)
                    foreach (string l in g)
                        yield return l;
            foreach (string[] p in NextDay_.Values)
                foreach (string l in p)
                    yield return l;
        }

        public static IEnumerable<string> Families()
        {
            return After_.Keys;
        }
    }
}
