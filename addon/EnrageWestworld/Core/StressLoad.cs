// Stress that builds, to a breaking point (FIVEDAY item 17; NIGHT3's psychologist list). Until now the strain was of one
// day: two troubles at once and someone snapped at whoever wouldn't hit back (Reasons.Strains, StrainChance). Now a week's
// load adds up, person by person, from what the mind holds: a friend's death heard of, a death seen, a beating or a
// quarrel taken, a theft from them, and the days their worry was deepening or at its worst. Past their breaking point
// (lower for the anxious, higher for the steady) a person breaks once, in the daily tick, in a way that fits them, and
// the habit stays a week:
// - drinks the nights away (the saloon and the bottle lean their days);
// - prays (the church);
// - sobs at the grave (a grief among the load; home and the church);
// - turns on their own at home (a husband's temper, the domestic dispute, leans to him; and he snaps at whoever's near);
// - takes the train (the platform's leavers, the next day).
// Each has its own thoughts. Three weeks before the same person breaks again. Behind [Mind] StressBuilds.
//
// Why it reads true: cumulative life-event load (Holmes and Rahe's scale: a death near you weighs most, then injury and
// trouble with the law, then quarrels); a breaking point that differs by temperament; and coping styles (the drinker's
// avoidance, the believer's religious coping, the grieving one's rumination, displaced anger at home, flight). Plain C#,
// no game; all words original. mindtest StressLoadTest.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Breakdown
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Way;     // drink, pray, grave, temper, train
        [DataMember(EmitDefaultValue = false)] public string Why;     // the load's parts, for the log and F7
        [DataMember(EmitDefaultValue = false)] public long Day;       // the game day it came
        [DataMember(EmitDefaultValue = false)] public int Load;
    }

    public static class StressLoad
    {
        public const int WeekDays = 7, HabitDays = 7, AgainDays = 21;
        public const string Drink = "drink", Pray = "pray", Grave = "grave", Temper = "temper", Train = "train";

        /// The week's load and what it's made of: deaths (a friend's 40, one seen 25), a beating taken 20 (at most two), a
        /// theft from them 15, a quarrel or a slight taken 8 (at most three), and a worry: each of the last seven days it was
        /// deepening 3, at its worst 6, gone wrong 10. worryStage(day) gives the worry's stage that day (-1 none, 4 gone wrong).
        public static int Load(List<MindMemory> mind, long now, Func<long, int> worryStage, out List<string> parts)
        {
            parts = new List<string>();
            int load = 0;
            long from = now - WeekDays * 1440L;
            if (mind != null)
            {
                int friendDeaths = 0, seen = 0, blows = 0, thefts = 0, words = 0;
                foreach (MindMemory m in mind)
                {
                    if (m == null)
                        continue;
                    long at = Math.Max(m.At, m.DueAt);
                    if (at < from || at > now)
                        continue;
                    if (m.Kind == "death")
                    {
                        if (m.Sub == "friend") friendDeaths++;
                        else if (m.Sub == "seen") seen++;
                    }
                    else if (m.Role == "done" && m.Kind == "blows") blows++;
                    else if (m.Role == "done" && m.Kind == "theft") thefts++;
                    else if (m.Role == "done" && (m.Kind == "quarrel" || m.Kind == "slight")) words++;
                }
                if (friendDeaths > 0) { load += 40 * Math.Min(2, friendDeaths); parts.Add(friendDeaths > 1 ? "two deaths of friends" : "a friend's death"); }
                if (seen > 0) { load += 25; parts.Add("a death seen"); }
                if (blows > 0) { load += 20 * Math.Min(2, blows); parts.Add(blows > 1 ? "beaten twice" : "a beating"); }
                if (thefts > 0) { load += 15; parts.Add("robbed"); }
                if (words > 0) { load += 8 * Math.Min(3, words); parts.Add(words > 1 ? words + " quarrels" : "a quarrel"); }
            }
            if (worryStage != null)
            {
                int w = 0;
                for (int d = 0; d < WeekDays; d++)
                {
                    int s = worryStage(now / 1440 - d);
                    w += s == 1 ? 3 : s == 2 ? 6 : s == 4 ? 10 : 0;
                }
                if (w > 0) { load += w; parts.Add("a worry (" + w + ")"); }
            }
            return load;
        }

        /// Where they break: 70 as built, lower for the anxious and the angry, higher for the self-controlled and the
        /// orderly (40 to 110).
        public static int BreakingPoint(Traits t)
        {
            float p = 70f;
            if (t != null)
                p += -12f * t.Z[Traits.Anxiety] - 4f * t.Z[Traits.Anger] + 8f * t.Z[Traits.SelfControl] + 4f * t.Z[Traits.Order];
            return (int)Math.Round(Math.Max(40f, Math.Min(110f, p)));
        }

        /// How they break. grieving: a friend's death in the load; married: a man with a wife at home (a temper turns there);
        /// canLeave: free to take the train (not keeping a post, not held).
        public static string Way(Traits t, Person p, bool grieving, bool married, bool canLeave, string seed)
        {
            var w = new Dictionary<string, float>();
            float piety = t != null ? t.Z[Traits.Piety] : 0f, self = t != null ? t.Z[Traits.SelfControl] : 0f, anger = t != null ? t.Z[Traits.Anger] : 0f,
                  risk = t != null ? t.Z[Traits.Risk] : 0f, soc = t != null ? t.Z[Traits.Sociability] : 0f;
            string dev = p != null ? p.Devotion : null;
            w[Drink] = Math.Max(0.05f, 1f - 0.6f * self + 0.3f * risk + (dev == "Godless" || dev == "Lapsed" ? 0.4f : 0f) - (p != null && p.Woman ? 0.5f : 0f));
            w[Pray] = Math.Max(0.02f, 0.4f + 0.6f * piety + (dev == "Devout" ? 1f : dev == "Churchgoer" ? 0.5f : dev == "Godless" ? -1f : 0f));
            if (grieving)
                w[Grave] = 1.2f + 0.3f * (p != null && p.Woman ? 1f : 0f);
            if (married && p != null && !p.Woman)
                w[Temper] = Math.Max(0f, 0.3f + 0.6f * anger - 0.4f * self);
            if (canLeave)
                w[Train] = Math.Max(0f, 0.2f + 0.25f * risk - 0.2f * soc - (p != null && p.Children > 0 ? 0.2f : 0f));
            float sum = w.Values.Sum();
            double roll = (Voice.Hash(seed + "|way") & 0x7fffffff) / (double)int.MaxValue * sum;
            foreach (var kv in w.OrderBy(k => k.Key, StringComparer.Ordinal))
            {
                roll -= kv.Value;
                if (roll <= 0)
                    return kv.Key;
            }
            return Drink;
        }

        /// The week's habit after: how it leans their choices (Choosing's act ids).
        public static float Lean(string way, string act)
        {
            switch (way)
            {
                case Drink: return act == "drink" ? 0.3f : act == "saloon" ? 0.2f : act == "cards" ? 0.05f : act == "work" ? -0.15f : act == "church" ? -0.1f : 0f;
                case Pray: return act == "pray" ? 0.3f : act == "church" ? 0.25f : act == "saloon" ? -0.15f : act == "cards" ? -0.15f : 0f;
                case Grave: return act == "pray" ? 0.15f : act == "church" ? 0.15f : act == "home" ? 0.1f : act == "talk" ? -0.15f : act == "greet" ? -0.15f : act == "showoff" ? -0.2f : 0f;
                case Temper: return act == "drink" ? 0.1f : act == "keepclear" ? -0.05f : act == "greet" ? -0.2f : act == "talk" ? -0.1f : 0f;
                default: return 0f;
            }
        }

        private static readonly Dictionary<string, string[]> BreakLines = new Dictionary<string, string[]>
        {
            { Drink, new[] { "One more, and then one more, until none of it is there.", "I'll drink till the week's gone. All of it.", "Nobody asks a man at the bar what's wrong. That's the bar's kindness." } },
            { Pray, new[] { "I can't carry it. Lord, You'll have to.", "On my knees is the only place left to go.", "I've nothing left but prayer, and I'm not sure of that." } },
            { Grave, new[] { "I keep walking out to the grave. I don't know what I'm waiting for.", "I talk to the dead more than the living now.", "The stone doesn't answer. I go back anyway." } },
            { Temper, new[] { "Everything at home sets my teeth on edge.", "She asked a simple thing and I shouted the house down. I'm not proud of it.", "It all comes out at the wrong people. Always the ones at home." } },
            { Train, new[] { "There's a train tomorrow. I'll be on it. No more of this town.", "I can't stay where all of it happened. I'm going.", "Pack a bag. Don't tell anybody. Just go." } },
        };

        private static readonly Dictionary<string, string[]> ActLines = new Dictionary<string, string[]>
        {
            { Drink + "|drink", new[] { "This one's for the week. The next one too.", "It helps. Not much. Enough for tonight.", "Pour it. Don't ask." } },
            { Drink + "|saloon", new[] { "Back to the bar. Where else is there.", "The saloon's the only place that doesn't look at me.", "Barkeep knows what I want before I say it now." } },
            { Pray + "|pray", new[] { "Give me the strength for one more day. Just one.", "I don't know the words for this, Lord. Hear me anyway.", "Take it from me. Please." } },
            { Pray + "|church", new[] { "The church is quiet. I need quiet.", "I'll sit in the back pew a while.", "If anywhere has room for this, it's here." } },
            { Grave + "|pray", new[] { "I pray for them. I pray for me too, God help me.", "Rest easy. I can't, but you rest easy.", "Every night I ask the same thing and get the same quiet." } },
            { Grave + "|church", new[] { "The graveyard's just past the church. I'll go after.", "I'll light a candle. Then go and sit with them.", "Their name was read out here. I can still hear it." } },
            { Grave + "|home", new[] { "Home, and the empty chair.", "Their coat's still by the door. I can't move it.", "The lamp stays dark tonight. Nobody to light it for." } },
            { Temper + "|drink", new[] { "Drink first. Then home. Then God knows.", "A drink to steady me. It never does.", "If I go home now I'll say something. One more." } },
        };

        public static string BreakThought(string way, string seed)
        {
            string[] pool;
            return way != null && BreakLines.TryGetValue(way, out pool) ? Voice.Pick(seed + "|break", pool) : null;
        }

        /// The thought when the act is the habit's own, else null.
        public static string ActThought(string way, string act, string seed)
        {
            string[] pool;
            return way != null && ActLines.TryGetValue(way + "|" + act, out pool) ? Voice.Pick(seed + "|act", pool) : null;
        }

        /// The train's why on the platform (Trains.LeavesWhy's kind).
        public const string TrainWhy = "couldn't bear the town any longer";

        /// Whether a breakdown's habit is still with them on this day.
        public static bool Lasting(Breakdown b, long day)
        {
            return b != null && day >= b.Day && day < b.Day + HabitDays;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in BreakLines.Values) foreach (string l in p) yield return l;
            foreach (string[] p in ActLines.Values) foreach (string l in p) yield return l;
        }
    }
}
