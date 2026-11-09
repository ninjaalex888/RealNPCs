// Things to come across out on the road (FIVEDAY2 items 16-20): scenes the game side stages from a table, so a new one is lines and a cast
// rather than another state machine. Built on what has been seen working: the encounters' way of putting people out of sight and handing them
// back (EncounterSpot, Spawner), the fists and the guns of the road execution and the crying woman's men, the J menu of the town events, the
// game's scenarios. No animation here that no log has shown playing.
//
// Each kind has a cast standing at offsets from a spot you can't see, what's said as you come near (beats, on a clock from the moment you're
// close), what can be done (the J menu), what they do if you draw on them, and thoughts over their heads. Why it reads true: the frontier was
// mostly empty, and a stranger on the road was the one piece of news you got all day. A traveller's trouble asks something of you just by
// being in front of you, and riding past is a choice too. Plain C#, no game; all lines original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class WaysideRole
    {
        public string Name, Pool, Arm, Act;   // Pool: rough, town, soldier. Arm: null, revolver, rifle. Act: a scenario, "stand", or "dead"
        public bool Woman;
        public float X, Y;                    // metres from the spot, along the scene's facing and across it
        public int Face = -1;                 // the one they face (an index into the cast), -1 the middle
    }

    public sealed class WaysideBeat
    {
        public int Ms, Who = -1, To = -1;     // when, who says it, to whom (-1: nobody in particular; -2: you)
        public string Tag, Do;                // Tag: the recorded voice's kind. Do: fight, flee, walk, run, chase, shoot
        public string[] Lines;
    }

    public sealed class WaysideChoice
    {
        public string Id, Label, Effect;      // Effect: part, bet, help, rob, hostile, bless, search, leave
        public int Cents, By = -1;            // the cost, and who answers
        public string[] Said;
    }

    public sealed class WaysideKind
    {
        public string Id, Mark, Test;         // Mark: the dot's label, Test: the Testing row's words (60 characters at most)
        public int From, To, Weight = 10;     // the hours (From > To runs over midnight), how likely against the others
        public bool OnRoad;                   // only on a road
        public WaysideRole[] Cast;
        public WaysideBeat[] Beats;
        public WaysideChoice[] Choices;
        public int[] SideA, SideB;            // for a fight: who against whom
        public string Threat = "flee";        // aimed at or shot: flee, or fight (the armed come at you)
        public float WalkMetres;              // they set off along the scene's facing, one after another, this far
        public string[] Thoughts;             // over their heads, any of them
        public string News;                   // what the nearest town hears when it's over: {town}, {you}
    }

    public static partial class Wayside
    {
        public const string Kind = "wayside";
        public const int NearMetres = 35, ChoiceMetres = 10, UnseenMs = 240000;

        private static string W(string scenario)
        {
            return "WORLD_HUMAN_" + scenario;
        }

        private static WaysideRole R(string name, string pool, string arm, string act, float x, float y, int face = -1, bool woman = false)
        {
            return new WaysideRole { Name = name, Pool = pool, Arm = arm, Act = act, X = x, Y = y, Face = face, Woman = woman };
        }

        private static WaysideBeat Say(int ms, int who, string tag, params string[] lines)
        {
            return new WaysideBeat { Ms = ms, Who = who, Tag = tag, Lines = lines };
        }

        private static WaysideBeat Do(int ms, string what, int who = -1)
        {
            return new WaysideBeat { Ms = ms, Do = what, Who = who };
        }

        private static WaysideChoice C(string id, string label, int cents, string effect, int by, params string[] said)
        {
            return new WaysideChoice { Id = id, Label = label, Cents = cents, Effect = effect, By = by, Said = said };
        }

        /// The kinds there are. Each item's table adds itself here.
        public static IEnumerable<WaysideKind> All
        {
            get { return Fights.Concat(Pursuit).Concat(Trouble).Concat(Camps).Concat(Found).Concat(Burial).Concat(Labour); }
        }

        public static WaysideKind Of(string id)
        {
            return All.FirstOrDefault(k => k.Id == id);
        }

        /// Whether this kind can happen at this hour, on or off a road.
        public static bool Fits(WaysideKind k, int hour, bool onRoad)
        {
            if (k.OnRoad && !onRoad)
                return false;
            return k.From <= k.To ? hour >= k.From && hour < k.To : hour >= k.From || hour < k.To;
        }

        /// What part someone has in a scene (FIVEDAY2 item 26): held, holding them, or come to watch. Null for those in the thick of it, who have the scene's own thoughts.
        public static string PartOf(WaysideKind k, int i)
        {
            WaysideRole r = k.Cast[i];
            if (r.Name == "A prisoner")
                return RoadThoughts.Captive;
            if (r.Arm != null && k.Cast.Any(c => c.Name == "A prisoner"))
                return RoadThoughts.Captor;
            bool fights = (k.SideA != null && Array.IndexOf(k.SideA, i) >= 0) || (k.SideB != null && Array.IndexOf(k.SideB, i) >= 0);
            return !fights && k.SideA != null && r.Act != "dead" ? RoadThoughts.Crowd : null;
        }

        /// A thought for one of the cast: half the time, for those with a part, the part's; the scene's own otherwise.
        public static string ThoughtOf(WaysideKind k, int i, string seed)
        {
            string part = PartOf(k, i);
            if (part != null && Voice.Chance(seed + "|part", 50))
                return RoadThoughts.ForRole(part, seed);
            return Word(k.Thoughts, seed);
        }

        /// One of those that fit, by weight, leaving out any seen lately. Null: none.
        public static WaysideKind Pick(int hour, bool onRoad, Func<string, bool> lately, string seed)
        {
            List<WaysideKind> fit = All.Where(k => Fits(k, hour, onRoad) && !lately(k.Id)).ToList();
            int total = fit.Sum(k => k.Weight);
            if (total <= 0)
                return null;
            int roll = Voice.Hash(seed + "|wayside") % total;
            foreach (WaysideKind k in fit)
            {
                roll -= k.Weight;
                if (roll < 0)
                    return k;
            }
            return fit[fit.Count - 1];
        }

        /// The J menu's rows, the dear ones greyed when you can't pay.
        public static List<InteractOption> Rows(WaysideKind k, int cash)
        {
            var rows = new List<InteractOption>();
            foreach (WaysideChoice c in (k != null ? k.Choices : null) ?? new WaysideChoice[0])
                rows.Add(new InteractOption { Id = c.Id, Label = c.Label + (c.Cents > 0 ? " ($" + (c.Cents / 100) + ")" : ""), Why = c.Cents > cash ? "you haven't the money" : null });
            return rows;
        }

        public static WaysideChoice Choice(WaysideKind k, string id)
        {
            return (k.Choices ?? new WaysideChoice[0]).FirstOrDefault(c => c.Id == id);
        }

        /// A bet comes off a little over half the time.
        public static bool BetWon(string seed)
        {
            return Voice.Chance(seed + "|bet", 55);
        }

        public static string Word(string[] pool, string seed)
        {
            return pool == null || pool.Length == 0 ? null : Voice.Pick(seed, pool);
        }

        // what comes of it (FIVEDAY2 item 22): the nearest town hears of it, and someone you stopped for may write

        /// The town's word of it, the blanks filled.
        public static string Heard(WaysideKind k, string town, string you)
        {
            return k == null || k.News == null ? null : k.News.Replace("{town}", town ?? "town").Replace("{you}", you ?? "a stranger");
        }

        /// Whether someone you helped writes to say so.
        public static bool Writes(string seed)
        {
            return Voice.Chance(seed + "|writes", 60);
        }

        private static readonly string[] ThanksBodies =
        {
            "I never got your name, and I am told you would not have given it. I walk fine now. I wanted it written down somewhere that a person stopped.",
            "You will not remember me. You stopped on the road when two others had gone by. There is a dollar owed to you whenever you pass through.",
            "I have told this to everyone who will listen and most who won't. I only wanted to tell it to you as well."
        };

        public static string ThanksBody(string seed)
        {
            return Voice.Pick(seed + "|thanks", ThanksBodies);
        }

        /// Every line of every kind, for the tests and the translation template.
        public static IEnumerable<string> AllLines()
        {
            foreach (WaysideKind k in All)
            {
                foreach (WaysideBeat b in k.Beats ?? new WaysideBeat[0])
                    foreach (string l in b.Lines ?? new string[0])
                        yield return l;
                foreach (WaysideChoice c in k.Choices ?? new WaysideChoice[0])
                {
                    yield return c.Label;
                    foreach (string l in c.Said ?? new string[0])
                        yield return l;
                }
                foreach (string l in k.Thoughts ?? new string[0])
                    yield return l;
                if (k.News != null)
                    yield return k.News.Replace("{town}", "Valentine").Replace("{you}", "a stranger");
            }
        }
    }
}
