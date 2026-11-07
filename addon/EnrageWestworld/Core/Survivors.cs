// Survivors come back changed (FIVEDAY item 21; NIGHT3's psychologist list). Someone you beat or robbed and left alive
// already could come and call you out over it (CalloutsArthur: "hurt", "robbed"), the same way for all but the cowards.
// Now what it did to them decides how they come back, once for each wrong:
// - "geteven": angry and with friends, they come with up to two of them, who stand with them (and fight with them);
// - "standing": the bold have more standing for having faced you: their thoughts and a friend's say so;
// - "broken": the anxious and the timid are broken by it. They never come at you, keep clear of you and stay off the
//   street, and their thoughts say why;
// - otherwise they let it lie, and nothing changes from before.
// Behind [Mind] SurvivorsChanged.
//
// Why it reads true: after harm, some grow (post-traumatic growth: standing, purpose, a closer circle), some are diminished
// (learned helplessness: the timid who expect it again), and the angry with friends recruit them (a grievance shared is
// a grievance armed). Plain C#, no game; all words original. mindtest SurvivorsTest.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Survivors
    {
        public const string GetEven = "geteven", Standing = "standing", Broken = "broken";
        public const int FriendsMost = 2;
        public const long AfterMinutes = 1440;   // a day on before it shows: the shock first

        /// The way a wrong left them, by who they are (the same for one wrong, whenever asked); null: they let it lie.
        public static string Way(Traits t, string temper, string seed)
        {
            float anx = t != null ? t.Z[Traits.Anxiety] : 0f, ass = t != null ? t.Z[Traits.Assertiveness] : 0f,
                  ang = t != null ? t.Z[Traits.Anger] : 0f, soc = t != null ? t.Z[Traits.Sociability] : 0f;
            var w = new List<KeyValuePair<string, float>>
            {
                new KeyValuePair<string, float>(Broken, Math.Max(0.02f, 0.3f + 0.45f * anx - 0.3f * ass + (temper == "Coward" ? 0.6f : 0f))),
                new KeyValuePair<string, float>(GetEven, Math.Max(0.02f, 0.25f + 0.4f * ang + 0.15f * ass + (temper == "Hothead" ? 0.4f : 0f) - (temper == "Coward" ? 0.25f : 0f))),
                new KeyValuePair<string, float>(Standing, Math.Max(0.02f, 0.25f + 0.35f * ass + 0.15f * soc + (temper == "Proud" ? 0.4f : 0f) - (temper == "Coward" ? 0.25f : 0f))),
                new KeyValuePair<string, float>("", 0.45f),
            };
            float sum = w.Sum(k => k.Value);
            double roll = (Voice.Hash(seed + "|survivor") & 0x7fffffff) / (double)int.MaxValue * sum;
            foreach (var kv in w)
            {
                roll -= kv.Value;
                if (roll <= 0)
                    return kv.Key.Length == 0 ? null : kv.Key;
            }
            return null;
        }

        /// How it leans their day while you're about (Choosing's act ids).
        public static float Lean(string way, string act)
        {
            switch (way)
            {
                case Broken: return act == "keepclear" ? 0.25f : act == "steerclear" ? 0.2f : act == "home" ? 0.12f : act == "showoff" ? -0.25f : act == "greet" ? -0.2f : act == "stroll" ? -0.1f : 0f;
                case Standing: return act == "showoff" ? 0.15f : act == "talk" ? 0.06f : act == "keepclear" ? -0.1f : 0f;
                case GetEven: return act == "talk" ? 0.06f : act == "watch" ? 0.08f : 0f;
                default: return 0f;
            }
        }

        private static readonly Dictionary<string, string[]> Thoughts = new Dictionary<string, string[]>
        {
            { Broken, new[] { "Every tall man in a hat is him, these days.", "I go round by the back now. Quicker anyhow.", "I keep thinking of the ground coming up. I don't sleep.", "Don't look at him. Don't give him a reason.", "I was different before. I don't know where that went." } },
            { Standing, new[] { "He knocked me down. I got up. Folks saw that part.", "They buy me drinks now. Funny what a beating gets you.", "I looked him in the eye after. I'll not forget that, and neither will he.", "Took the worst he had. Still standing.", "Folks nod to me different now." } },
            { GetEven, new[] { "Next time it won't be just me.", "I've told the boys. They know his face.", "He'll have three of us to deal with. See how he likes it.", "Let him come round here again. We're ready." } },
        };

        private static readonly Dictionary<string, string[]> FriendThoughts = new Dictionary<string, string[]>
        {
            { Standing, new[] { "{n} stood up to him. Takes nerve.", "Whatever folks say, {n} didn't run.", "I'd stand with {n} now. Anybody would." } },
            { Broken, new[] { "{n} hasn't been right since. Barely speaks.", "{n} jumps at every door. It's hard to watch.", "Somebody ought to do something about what was done to {n}." } },
            { GetEven, new[] { "If {n} goes after him, I'm going too.", "{n} asked me to stand by. I said I would.", "{n}'s been talking. I know what about." } },
        };

        private static readonly string[] Backing = { "Not alone this time, mister.", "You'll deal with all of us.", "We're with {n}. Make your choice.", "Go on. Try it now." };

        public static string Thought(string way, string seed)
        {
            string[] pool;
            return way != null && Thoughts.TryGetValue(way, out pool) ? Voice.Pick(seed + "|th", pool) : null;
        }

        public static string FriendThought(string way, string name, string seed)
        {
            string[] pool;
            return way != null && FriendThoughts.TryGetValue(way, out pool) ? Voice.Pick(seed + "|fr", pool).Replace("{n}", name ?? "him") : null;
        }

        public static string BackingLine(string name, string seed) { return Voice.Pick(seed + "|back", Backing).Replace("{n}", name ?? "him"); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] p in Thoughts.Values) foreach (string l in p) yield return l;
            foreach (string[] p in FriendThoughts.Values) foreach (string l in p) yield return l.Replace("{n}", "Hank");
            foreach (string l in Backing) yield return l.Replace("{n}", "Hank");
        }
    }
}
