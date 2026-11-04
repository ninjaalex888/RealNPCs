// The Awakening's share without the model (the no-model run, item 10; Lex, 2026-11-01: B, a written wondering). The
// model's own wondering chips "things are as they seem" in someone with cause below the second stage (ThoughtShown,
// ModelWakes); without the model nothing did, so about a quarter fewer stirred. Now, for someone below the second stage
// with cause (something seen lately that shouldn't have been, a death they woke from, moments that came round again), a
// rare written thought wonders at the world in their own way, no more than one a day, and chips it by the same amount
// to the same floor ([Mind] WrittenWakes). Every line says its doubt in the words Language.DoubtIn knows. All lines are
// original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        internal static readonly Dictionary<string, string[]> WonderLines = new Dictionary<string, string[]>
        {
            { "seen", new[] {
                "Saw what I saw. Can't have been real. Was it real?",
                "Something's off about this town since that night. Can't put a finger on it.",
                "If a thing like that can walk the street, how much of the rest is real?",
                "Felt like the world's shifting under my boots. Then it stopped.",
                "Told nobody what I saw. They'd say I'm losing my grip on things.",
                "Something's off about the light today. Like it's painted on.",
                "Keep looking at the people passing. Are they real? Am I?",
                "What I saw is slipping through my fingers. Like it's being taken back.",
                "If it wasn't real, why can I still see it when I shut my eyes?",
                "Something's off about the quiet. A town ought to make more noise than this." } },
            { "died", new[] {
                "I should be in the ground. I'm not. Was any of it real?",
                "Woke up from my own death and nobody blinked. Something's off about that.",
                "Feels like I'm reading off a page somebody else wrote. Every word.",
                "Died, they say, and here I am. All a trick, or all a mercy?",
                "Every face I know looks a little painted on since I came back.",
                "I came back. Things don't come back. Not in a real world.",
                "Losing my grip on what's so. Was I dead, or dreaming I was?",
                "My own hands feel unreal some mornings. Since then.",
                "If dying's not the end here, is any of this real?",
                "Something's off about how the doctor looked at me. Like he'd seen it before." } },
            { "again", new[] {
                "Lived this before. This very hour. That man, that word.",
                "Every day the same, near enough. Today it was exactly the same.",
                "Same lines, the same faces, the same weather. How is that real?",
                "Done all this before. I'd swear on my mother's grave.",
                "The same day again, or I'm going soft in the head.",
                "Heard her say it yesterday. The same lines, the same laugh after.",
                "Felt like reading lines in a play. Mine and everybody's.",
                "If the world's a lie, it's a careful one. Nearly fooled me.",
                "The world's shifting back to how it was this morning. Every board.",
                "Lived this before, and I'll live it again, won't I?" } },
        };

        private readonly Dictionary<string, long> _wonderedOn = new Dictionary<string, long>();   // id -> the game day they last wondered
        public int WrittenDoubts, WrittenWonders;

        /// What gives them cause to wonder, or null: seen something lately, a death they woke from, moments that came round.
        private string WonderCause(string id, PersonState st)
        {
            if (WorldShaken(id) >= 0.05f)
                return "seen";
            if (st != null && st.Deaths > 0)
                return "died";
            if (st != null && st.Glitches > 0)
                return "again";
            return null;
        }

        /// The written wondering's candidate for this thought, or null (off, at or past the second stage, no cause, done today).
        private Minds.Candidate WonderCandidate(Person p, PersonState st, string seed, long now)
        {
            // only the waking wonder (stage one): someone still asleep never doubts the world in words, whatever they lived through
            // (Lex, 2026-11-03: "only awake people from the West World part of the mod should realize they are in a simulation");
            // stage one is still reached from what they lived through alone (Engine.Awakening: the share's pick leans by it)
            if (Cfg == null || !Cfg.WrittenWakes || !Cfg.MindWakes || !Cfg.WestWorld || st == null || st.WakeStage < Awakening.Stirring || st.WakeStage >= Awakening.Questioning)
                return null;
            long day;
            if (_wonderedOn.TryGetValue(p.Id, out day) && day == now / 1440)
                return null;
            string cause = WonderCause(p.Id, st);
            if (cause == null)
                return null;
            string[] pool = WonderLines[cause];
            return new Minds.Candidate { Source = "wonder", Pull = 12 + Minds.Jitter(seed, "wonder", 8), Make = () => pool[Voice.Hash(seed + "|wonder") % pool.Length] };
        }

        /// A written wondering was the thought: noted for the day, and "things are as they seem" chipped as the model's is.
        private void WonderShown(string id, long now)
        {
            if (_wonderedOn.Count > 2000)
                _wonderedOn.Clear();
            _wonderedOn[id] = now / 1440;
            WrittenWonders++;
            BeliefState b = Shadow.BeliefsOf(id);
            if (b.Moved[Beliefs.World] <= -ModelDoubtFloor)
                return;
            b.Moved[Beliefs.World] = Math.Max(-ModelDoubtFloor, b.Moved[Beliefs.World] - ModelDoubtChip);
            WrittenDoubts++;
        }
    }
}
