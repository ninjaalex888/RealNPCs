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

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] t in After_.Values)
                foreach (string[] g in t)
                    foreach (string l in g)
                        yield return l;
        }

        public static IEnumerable<string> Families()
        {
            return After_.Keys;
        }
    }
}
