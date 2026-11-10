// The control census (FOURTEENHOUR item 22; TWELVEHOUR item 3; [Mind] ControlCensus). Lex asked on 2026-11-10 who drives whom: how much of
// a street is the game's, enRAGE's and ours, and how many people we could take and don't. Every share the log had counted only the
// people the mind had named, so there was no denominator. Now every few seconds each human within 30 m, named or not, goes into one
// box by what already decides it (the natives the add-on calls anyway), first match:
// the story's (a story model, or a mission's own), the game's own encounters (an re_ model), ours (and which feature: a scene, the
// ring, an encounter's cast, a wake act, talking with you, a nod or a walk-up, walking together, spawned or borrowed, other),
// enRAGE's in a role (a fight or a job of its own), another script's (any other mission entity), then the game's own business
// (fighting, fleeing, down, riding, in a vehicle, in the water, in a scenario, and which kind of scenario) and, last, free for us and
// not used (walking or standing). Apart from the box, whether the mind has named them. Once a minute a CONTROL line averages it
// within 25 m. Plain C#, no game: the game side fills the facts (Game/WestworldScript.Census.cs).
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnrageWestworld.Core
{
    public static class Census
    {
        public static bool On = true;   // [Mind] ControlCensus, from the settings
        public const float LookMetres = 30f, LineMetres = 25f;
        public const int PassMs = 3000, MinuteMs = 60000;

        /// What the game side knows of one person at a look.
        public sealed class Facts
        {
            public bool Story, GameEncounter, OtherScript, Record, RecordInRole, Fighting, Fleeing, Down, Riding, Vehicle, Water, Scenario, Moving, Named;
            public string Ours;       // the feature of ours holding them, else null
            public string Scenery;    // the kind of scenario they're in (sitting, leaning, at work...), else null
        }

        public static readonly string[] Boxes =
        {
            "the story", "the game's encounters", "ours", "enRAGE's, in a role", "another script's",   // who drives them
            "fighting", "fleeing", "down", "riding", "in a vehicle", "in the water",                     // the game's own business
            "in a scenario", "free, walking", "free, standing"                                            // and the rest
        };

        /// The one box, first match.
        public static string Box(Facts f)
        {
            if (f.Story) return "the story";
            if (f.GameEncounter) return "the game's encounters";
            if (f.Ours != null) return "ours";
            if (f.Record && f.RecordInRole) return "enRAGE's, in a role";
            if (f.OtherScript && !f.Record) return "another script's";
            if (f.Fighting) return "fighting";
            if (f.Fleeing) return "fleeing";
            if (f.Down) return "down";
            if (f.Riding) return "riding";
            if (f.Vehicle) return "in a vehicle";
            if (f.Water) return "in the water";
            if (f.Scenario) return "in a scenario";
            return f.Moving ? "free, walking" : "free, standing";
        }

        public static bool Free(string box) { return box == "free, walking" || box == "free, standing"; }

        /// A minute's looks, added up.
        public sealed class Tally
        {
            public int Looks, People, Unnamed, Records;
            public readonly Dictionary<string, int> ByBox = new Dictionary<string, int>(), ByOurs = new Dictionary<string, int>(), ByScenery = new Dictionary<string, int>();

            public void Add(Facts f)
            {
                string box = Box(f);
                People++;
                ByBox[box] = (ByBox.TryGetValue(box, out int n) ? n : 0) + 1;
                if (f.Ours != null)
                    ByOurs[f.Ours] = (ByOurs.TryGetValue(f.Ours, out int o) ? o : 0) + 1;
                if (box == "in a scenario" && f.Scenery != null)
                    ByScenery[f.Scenery] = (ByScenery.TryGetValue(f.Scenery, out int s) ? s : 0) + 1;
                if (!f.Named)
                    Unnamed++;
                if (f.Record)
                    Records++;
            }

            public void Clear()
            {
                Looks = People = Unnamed = Records = 0;
                ByBox.Clear();
                ByOurs.Clear();
                ByScenery.Clear();
            }
        }

        static string Pct(int n, int of) { return of <= 0 ? "0%" : (100 * n / of) + "%"; }

        /// The minute's line: people a look within 25 m, the share in each box, ours and the scenarios by kind, the free a look.
        public static string Line(Tally t)
        {
            if (t.Looks == 0)
                return "no looks this minute";
            var b = new StringBuilder();
            b.Append((t.People / (double)t.Looks).ToString("0.0")).Append(" people within ").Append((int)LineMetres).Append(" m a look (").Append(t.Looks).Append(" looks; unnamed ")
             .Append(Pct(t.Unnamed, t.People)).Append(", enRAGE's records ").Append(Pct(t.Records, t.People)).Append("): ");
            b.Append(string.Join(", ", Boxes.Where(x => t.ByBox.ContainsKey(x)).Select(x =>
            {
                string s = x + " " + Pct(t.ByBox[x], t.People);
                if (x == "ours" && t.ByOurs.Count > 0)
                    s += " (" + string.Join(", ", t.ByOurs.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + Pct(kv.Value, t.People))) + ")";
                if (x == "in a scenario" && t.ByScenery.Count > 0)
                    s += " (" + string.Join(", ", t.ByScenery.OrderByDescending(kv => kv.Value).Take(4).Select(kv => kv.Key + " " + Pct(kv.Value, t.People))) + ")";
                return s;
            })));
            int free = t.ByBox.Where(kv => Free(kv.Key)).Sum(kv => kv.Value);
            b.Append("; free and not ours ").Append((free / (double)t.Looks).ToString("0.0")).Append(" a look");
            return b.ToString();
        }
    }
}
