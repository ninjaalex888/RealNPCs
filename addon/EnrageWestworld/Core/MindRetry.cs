// Trying a choice again (NIGHT4 fix 3). The mind chooses for each person near once a minute, and until now acted only when
// the choice changed: someone sat when their choice came, or riding, or in the 1.5 s gap after another act, never acted on
// it at all (352 people in an evening, each refused once). On the minute's recheck of an unchanged choice that never became
// an act, and whose last refusal was one that passes (sat, riding, aimed at, busy, the gap, the cap), it is tried again;
// never for the law, a child or the story's own, never once it has acted (no second stroll or talk). Also which of the
// model's options a seated person may be offered: only those with an act from a seat (WestworldScript.MindActs.cs).
namespace EnrageWestworld.Core
{
    public static class MindRetry
    {
        // the refusals, worded as the game side words them (WestworldScript.MindActs.cs uses these)
        public const string Sat = "sat, leaning or at something already", Riding = "riding, fighting or running", Aimed = "aimed at just now",
                            Busy = "busy with something of RealNPCs'", Gap = "the 1.5 s gap", AtOnce = "10 acting at once", Acting = "acting already",
                            Nodding = "nodding to someone or listening to you", Story = "the story's, a child or the law";

        /// Whether a refusal passes by itself (they get up, get down, stop running, the gap ends).
        public static bool Temporary(string why)
        {
            return why == Sat || why == Riding || why == Aimed || why == Busy || why == Gap || why == AtOnce || why == Acting || why == Nodding;
        }

        /// Whether an unchanged choice is tried again: never once acted; tried when nothing was tried yet (a pick dropped, the
        /// person taken up before it came back) or the last refusal was temporary.
        public static bool Should(bool acted, string lastWhy)
        {
            return !acted && (lastWhy == null || Temporary(lastWhy));
        }

        /// The options a seated person may be offered: a look (watch, keep clear, a friend, someone to talk to), staying at what
        /// they're doing (rest), or a scene reason of theirs. Getting up for anything else is 0.7.12's stage 1.
        public static bool SeatedOption(string id)
        {
            return id == "watch" || id == "keepclear" || id == "greet" || id == "talk" || id == "rest" || (id != null && id.StartsWith("scene:", System.StringComparison.Ordinal));
        }
    }
}
