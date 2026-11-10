// Witnesses fetch the law (FOURTEENHOUR item 21; TWELVEHOUR item 10; [Mind] WitnessesFetchLaw). Before this, the law came to a
// brawl only if a lawman saw it (about 35 m) or heard it (60 m, after 4 s), and only the domestic dispute's neighbour ever went to
// fetch one (Core/Domestic.cs). Now, in a brawl of three or more with no lawman come, one onlooker runs for the law: a gentleman or
// a lady first (the well-off never join a brawl, item 6: they send for the law), else the law-abiding by nature (order, honesty,
// deference; not the reckless; the frightened less, a fearful man still runs from it), to the nearest free lawman within 150 m; the
// lawman comes at a run, item 5's arrival (he takes it from there: the cry, the word, the holdouts tied). And after a face put into
// a bar or a window in the street ("rough"), a witness may fetch a lawman who has a word with the man who did it, if he's still
// about, or asks the one it was done to which way he went. Why it reads true: in a crowd everyone waits for someone else
// (diffusion of responsibility, Darley and Latane, 1968); the spell breaks when one person's nature makes it their duty, and in
// 1899 that was the respectable sort, whose duty it was to fetch the marshal, not to join a fight. Plain C#, no game; all words
// this add-on's.
using System;

namespace EnrageWestworld.Core
{
    public static class WitnessLaw
    {
        public static bool On = true;   // [Mind] WitnessesFetchLaw, from the settings
        public const float FetchFromMetres = 25f, LawMostMetres = 150f;
        public const int FetchAfterMs = 6000, FetchMostMs = 30000, GentlePercent = 60;

        /// How likely this onlooker runs for the law: the well-off mostly; else the orderly, the honest and the deferent, not the
        /// reckless; fear holds a man back a little (he may still run from it to the law).
        public static int Percent(Traits t, bool wellOff, bool afraid)
        {
            if (wellOff)
                return GentlePercent;
            float c = 0.06f;
            if (t != null)
                c += 0.14f * t.Z[Traits.Order] + 0.10f * t.Z[Traits.Honesty] + 0.08f * t.Z[Traits.Deference] - 0.10f * t.Z[Traits.Risk] - (afraid ? 0.06f : 0f) * Math.Max(0f, t.Z[Traits.Anxiety]);
            return (int)Math.Round(Math.Max(0.02f, Math.Min(0.55f, c)) * 100);
        }

        public static bool Fetches(Traits t, bool wellOff, bool afraid, string seed)
        {
            return Voice.Chance(seed, Percent(t, wellOff, afraid));
        }

        /// What the one who goes cries as he sets off.
        public static string Off(string seed)
        {
            return Voice.Pick(seed, "Somebody fetch the law!", "I'll get the marshal!", "Stop it, all of you! I'm going for the sheriff!");
        }

        /// To the lawman, when he reaches him: a brawl, or a man beaten.
        public static string ToLaw(bool brawl, bool saloon, string seed)
        {
            return brawl ? Voice.Pick(seed, "Sheriff! There's a brawl " + (saloon ? "in the saloon" : "in the street") + ". They'll kill each other.", "Deputy, come quick! Half the town's fighting.", "Marshal! A fight, a big one. This way!")
                         : Voice.Pick(seed, "Deputy! A man just put another's face into the bar.", "Sheriff, there's been a man beaten, back there.", "Come quick, marshal. A man's been knocked senseless.");
        }

        public static string LawReply(string seed)
        {
            return Voice.Pick(seed, "Show me.", "Where? All right. I'm coming.", "Again? Right, then.");
        }

        /// The lawman's word to the man who did it, after a beating.
        public static string Word(string seed)
        {
            return Voice.Pick(seed, "You. A word. I hear you've been laying hands on folk.", "That's enough of that in my town. You hear me?", "Do that again and you'll sleep in my cells.");
        }

        /// The lawman's word to the winner of a brawl he was fetched to too late.
        public static string WordAfter(string seed)
        {
            return Voice.Pick(seed, "Heard there was a fight. Was it you started it?", "You. I'm told you were in that brawl.", "A fight in my town, and you the last one on his feet. Explain.");
        }

        /// His answer, by his nature: the hot and the proud sullen, the rest meek.
        public static string Answer(string temper, string seed)
        {
            return temper == "Hothead" || temper == "Proud" || temper == "Cold"
                ? Voice.Pick(seed, "He had it coming, Sheriff.", "Mind your own business.", "I'll do as I please.")
                : Voice.Pick(seed, "Yes, sir. It won't happen again.", "Understood, Sheriff.", "It was a mistake. I'm going.");
        }

        /// The culprit gone: the lawman asks the one it was done to.
        public static string Asks(string seed)
        {
            return Voice.Pick(seed, "Which way did he go?", "Did you know him? What did he look like?", "Gone, is he. I'll keep an eye out.");
        }
    }
}
