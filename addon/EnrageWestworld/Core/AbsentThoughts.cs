// Thinking of someone who isn't there (LIVES item 1b, 2026-11-12; [Mind] AbsentThoughts, settings version 462). Two things
// already existed only face to face: a view of another (Core/Opinions.cs: fear, contempt, envy, respect) showed in a thought
// only when the two stood in view of each other (Core/PairThoughts.cs), and the tie of two who both saw the player kill
// (Core/SharedFright.cs) showed only when they met and talked of it. People don't stop thinking of each other when they part:
// the one you fear is on your mind most when you can't see where he is, and a shared horror is carried alone between the
// times it's spoken of. Now a person's own thoughts can go to someone of their ties from the last four days who isn't in
// sight: the fellow witness ("Ruth saw it too. I wonder does she sleep."), or the one they hold a view of, in words for an
// absent person ("Haven't seen Hollis all day. I'll take that.").
//
// The psychology: rumination is mostly about absent others (Nolen-Hoeksema), and a feared person's absence keeps the threat
// open rather than closing it (uncertainty sustains vigilance); shared trauma ties survivors, who think of each other as
// the only ones who know (the "co-witness" bond of survivor studies). Envy and contempt are social comparisons and they run
// on in private (Festinger, 1954): the coat is thought of at home, not only when it walks past.
//
// No new state: it reads the person's ties of the last four days (InnerLife.Context.Ties, already worked out for each
// thought), who's in sight (Context.Nearby) and who has died (Context.IsDead). Off in Core so the goldens stand. Plain C#.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class AbsentThoughts
    {
        /// [Mind] AbsentThoughts: set by the game from the settings.
        public static bool On;

        public const double FrightPull = 33, ViewPull = 23;   // fright: above an old tie's thought (30), below the player close again (34); a view: beside the town's talk

        /// Which absent tie this person might think of now: the first fresh fellow witness, else the first tie they hold a view
        /// of, among their ties of the last four days whose other end is out of sight and alive. Null: none. `kind` is "fright"
        /// or the view; `who` the other's first name.
        public static SceneTie Pick(string id, string temper, InnerLife.Context day, System.Func<string, Resident> resident, out string kind, out string who)
        {
            kind = null;
            who = null;
            if (!On || id == null || day == null || day.Ties == null || day.Ties.Count == 0)
                return null;
            SceneTie viewTie = null;
            string view = null;
            foreach (SceneTie t in day.Ties)
            {
                string other = t.OtherId(id);
                if (other == null || other == "player" || day.IsDead(other) || InSight(day, other))
                    continue;
                if (SharedFright.Fresh(t, day.Now))
                {
                    kind = "fright";
                    who = Person.FirstOf(t.A == id ? t.BName : t.AName);
                    return who != null ? t : null;
                }
                if (viewTie == null)
                {
                    Resident mine = resident != null ? resident(id) : null, theirs = resident != null ? resident(other) : null;
                    bool iGentry = mine != null && InnerLife.TradeGroup(mine.Trade) == "gentry", otherGentry = theirs != null && InnerLife.TradeGroup(theirs.Trade) == "gentry";
                    string v = Opinions.View(t, id, temper, iGentry, otherGentry, false);   // not drunk: not seen
                    if (v != null)
                    {
                        viewTie = t;
                        view = v;
                    }
                }
            }
            if (viewTie == null)
                return null;
            kind = view;
            who = Person.FirstOf(viewTie.A == id ? viewTie.BName : viewTie.AName);
            return who != null ? viewTie : null;
        }

        private static bool InSight(InnerLife.Context day, string id)
        {
            if (day.Nearby != null)
                foreach (InnerLife.Nearby n in day.Nearby)
                    if (n != null && n.Id == id)
                        return true;
            return false;
        }

        public static double PullOf(string kind)
        {
            return kind == "fright" ? FrightPull : ViewPull;
        }

        /// The thought, the other's first name filled in; null for no lines.
        public static string Line(string kind, string who, string seed, List<string> told)
        {
            string[] pool;
            if (kind == null || who == null || !Lines.TryGetValue(kind, out pool))
                return null;
            int start = (Voice.Hash(seed + "|absent") & 0x7fffffff) % pool.Length;
            for (int i = 0; i < pool.Length; i++)
            {
                string l = pool[(start + i) % pool.Length].Replace("{who}", who);
                if (told == null || !told.Contains(l))
                    return l;
            }
            return pool[start].Replace("{who}", who);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in Lines)
                foreach (string l in kv.Value)
                    yield return l;
        }

        private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            { "fright", new[] {
                "{who} saw it too. I wonder does {who} sleep.",
                "Only {who} and me know how it was. Nobody else was close enough.",
                "Should go and sit with {who} a while. Neither of us need say a word.",
                "Keep thinking {who} will tell somebody. Then I think, tell them what.",
                "If I'd been alone I'd think I dreamed it. {who} was there. It happened.",
                "{who} looked as white as I felt. Like two ghosts at the edge of it.",
                "Wonder if {who} keeps a lamp lit too.",
                "Saw {who} cross the street at the sound of a horse. I'd have done the same." } },
            { "fear", new[] {
                "Haven't seen {who} all day. I'll take that.",
                "Wonder where {who} is now. Rather know than wonder.",
                "Took the back way home in case {who} was on the street.",
                "Every door that opens, I think it's {who}.",
                "{who} won't forget me. That's the trouble with being noticed." } },
            { "contempt", new[] {
                "{who} will be at the bottle by now. Regular as a church bell.",
                "Said a word about {who} at supper. Got a laugh. Didn't feel like one.",
                "Every town has one like {who}. Ours is the genuine article.",
                "Thinking of {who} puts me off my supper.",
                "{who} will come to a bad end. I won't send flowers." } },
            { "envy", new[] {
                "{who} is sitting down to beef tonight, I'd wager. Beans for me.",
                "Thought of {who}'s house on the walk home. Mine looked smaller after.",
                "What I'd do with half of what {who} has. More than {who} does with all of it.",
                "{who} has never once had to count the jar.",
                "Saw my own boots and thought of {who}'s. Shouldn't have." } },
            { "respect", new[] {
                "{who} would know what to do about this. {who} generally does.",
                "Thought of what {who} said the other day. Still turning it over.",
                "Wish I'd half the steadiness {who} has.",
                "If I ever get in a fix, I'd go to {who} first.",
                "{who} keeps a good name in this town. Earned every bit of it." } },
        };
    }
}
