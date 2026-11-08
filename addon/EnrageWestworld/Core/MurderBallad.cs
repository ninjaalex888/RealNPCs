// Murder ballads at a fire (FIVEDAY reserve R20). Sit at a campfire out of the towns at night, and once a night a voice from
// the dark (another fire, a drover's camp across the creek) sings a murder ballad: a real killing, set to the old tunes,
// with its names and its place; your own most-told killing first, if one has been told of, else a death the add-on knows
// (Store.Deaths: who, where, by whose hand). Four verses on the ticker, as "From the next fire". Behind [Scenes] MurderBallads.
//
// Why it reads true: the murder ballad (Omie Wise, Pretty Polly, Stagger Lee) was how the back country kept its news and
// its morals: a real killing, sung until the facts wore off and the warning stayed. Hearing your own deed sung as a ballad,
// by someone who doesn't know you're listening, is a reputation's strangest mirror. Plain C#, no game; all words original
// (the tunes' shape is traditional; every line here is new). mindtest MurderBalladTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MurderBallad
    {
        public const int CheckMs = 15000, VerseMs = 8000;

        private static readonly string[] Openings = {
            "Come all you good people and hear of {k}, at {p}, where it all went wrong.",
            "Down by {p} where the cottonwoods grow, {k} lies sleeping six feet below." };
        private static readonly string[] YouVerses = {
            "A stranger rode in on a cold grey morn, with a gun on his hip and his hat pulled down.",
            "He spoke not a word as he drew and he fired, and {v} fell down in the dust of the town.",
            "They say he rode on and he never looked back, and his horse was as dark as the night.",
            "So mothers, keep your children close by, when a stranger rides into your sight." };
        private static readonly string[] OtherVerses = {
            "{v} was a good man, or so it was said, with a wife and a cabin and fields of his own.",
            "But {b} came calling one night by the creek, and {v} never came walking home.",
            "They buried {v} on a hill by the pines, and the preacher said what preachers say.",
            "And {b}'s still out on the roads tonight, so keep your doors barred till the day." };

        /// The opening and four verses for your killing (victim, place), or null.
        public static List<string> OfYours(string victim, string place, string seed)
        {
            if (place == null)
                return null;
            string v = victim ?? "a man";
            var l = new List<string> { Voice.Pick(seed + "|mo", Openings).Replace("{k}", "the killing of " + v).Replace("{p}", place) };
            foreach (string verse in YouVerses)
                l.Add(verse.Replace("{v}", v));
            return l;
        }

        /// The opening and four verses for a death the add-on knows (the dead, the place, the hand: null for unknown).
        public static List<string> OfDeath(string dead, string place, string by, string seed)
        {
            if (dead == null || place == null)
                return null;
            string b = by ?? "a man with no name";
            var l = new List<string> { Voice.Pick(seed + "|mo", Openings).Replace("{k}", "poor " + dead).Replace("{p}", place) };
            foreach (string v in OtherVerses)
                l.Add(v.Replace("{v}", dead).Replace("{b}", b));
            return l;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in OfYours("Abel Grange", "Valentine", "a")) yield return l;
            foreach (string l in OfDeath("Abel Grange", "Strawberry", "Seth Dray", "b")) yield return l;
        }
    }
}
