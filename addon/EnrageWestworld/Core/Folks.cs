// Private thoughts of townsfolk enRAGE has met (acquaintances), for the ticker.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Folks
    {
        public static string Thought(Folk f, PersonState st, PlayerRef player, string seed, bool romance)
        {
            PlayerRef you = new PlayerRef { FullName = player.FullName, Scarred = player.Scarred, KnownByName = st.KnowsName };
            Person shell = new Person { Id = f.Key, Name = f.Name, Woman = f.Woman };
            string who = Voice.YouPrivate(shell, you, false);
            // what they think of him (Regard): the same judgement their answer to his flirting comes from
            string feeling = Regard.OfFolk(f, st, romance);
            if (feeling == "witnessed")
                return Voice.ToPrivate(Townsfolk.Witness(f, f.Witnessed.Last(), seed), shell, you);
            // enRAGE knows its townsfolk by their look and passes "met" between bodies that share one, so a stranger in another
            // man's clothes is "met" the moment he's seen (Lex's players, 2026-10-28: "Him. I've spoken to them before" on a first
            // meeting); only a talk of our own says spoken, the rest only that the face seems known
            if (feeling == "met")
                feeling = st.Talks > 0 ? "met_talked" : "met_seen";
            string[] pool;
            if (feeling == null || !ByFeeling.TryGetValue(feeling, out pool))
                return null;
            string line = Voice.Pick(seed, pool).Replace("{who}", who).Replace("{him}", st.KnowsName && player.Surname != null ? player.Surname : "him");
            return Grammar.UpperFirst(line);
        }

        /// The lines by how they feel about him (pools since Run 3, item 6, where they were two to four each).
        private static readonly Dictionary<string, string[]> ByFeeling = new Dictionary<string, string[]>
        {
            { "smitten", new[] { "There's {who} again. My heart's going like a rabbit.", "Don't look at him. Don't look at him. ...Looked.", "I hope he stops to talk. I hope he doesn't. I don't know what I hope.",
                "{who} smiled at me once by the trough. I've found reasons to pass that trough ever since.",
                "I've rehearsed a whole talk with {who}. In it I'm clever. In life I say good day.",
                "My hands don't know where to go when {who} is near. Pockets? No. Hair? No.",
                "Half the town's seen how I look at {who}. The other half's blind.",
                "If he looks over, I'll look away. If he doesn't look, I'll be miserable a week.",
                "Been to the store twice today for nothing at all. In case {who} came by.",
                "{who}, across the street. Stand still. People stand still every day. How do they do it?",
                "I'd know {who} from the far end of the street by the shoulders alone.",
                "Somebody say something to {who} so I can stand near and listen.",
                "If {who} asked me to the dance I'd say no out of fright and cry about it after." } },
            { "scorned", new[] { "Of all the people to walk past.", "I hope he trips.",
                "Chin up. Walk past slow. Let {who} see what he threw away.",
                "I spent a whole week thinking kindly of {who}. I'd like that week back.",
                "Look at him, pleased with himself. Somebody ought to tell {who} he isn't much.",
                "If he wishes me good day I'll look through him like a pane of glass.",
                "Not one more thought about {who}. Not one. ...That was one.",
                "Let {who} make a fool of some other soul. I'm done being the one.",
                "Smile at somebody else. Anybody. Make sure {who} sees it.",
                "His loss. I've said it nine times today. It's starting to sound true.",
                "I'd cross the street, but I was here first. Let him do the crossing.",
                "He had pretty words once. I'd like to hand them all back, every one." } },
            { "grateful", new[] { "{who} did me a kindness once. I ain't forgot.", "There's {who}. Decent, for all the talk about him.",
                "Whatever they say about {who}, he helped me when nobody else in this town would.",
                "I owe {who}. I don't like owing, but I don't mind owing him.",
                "Said a prayer for {who} on Sunday. Don't suppose he'd want one. He got one anyway.",
                "If anybody runs {who} down in my hearing, they'll have me to answer to.",
                "There's {who}. I'd stand him a supper if he ever sat still long enough.",
                "Some day I'll do him a good turn back. I keep looking for the day.",
                "{who} helped me and never once made me feel small for needing it.",
                "Folk talk hard of {who}. They didn't see what I saw him do for me.",
                "I've told my people about {who}. They pray for him now. I didn't ask them to.",
                "When the day comes that {who} needs a hand, I hope it's mine he finds." } },
            { "hostile", new[] { "Him again. Can't stand the sight of {him}.", "One day somebody'll give him what he's owed.",
                "Look at {him}, walking about like the street belongs to him. It doesn't.",
                "If I had more nerve I'd tell {him} to his face what I think of him.",
                "There'll come a day a judge sits {him} down. I mean to be in the crowd.",
                "I see {him} and my hands go to fists before I've told them to.",
                "Him. Smiling at folk. As if a smile pays for one thing he's done.",
                "I keep a list in my head, and every morning I write {him} first.",
                "Nobody in this town would weep over {him}. I know I wouldn't.",
                "Can't abide that man, nor the way folk step aside for him.",
                "Same coat, same swagger. I hate {him} down to the buttons.",
                "If he nods at me I'll turn my back. Let him look at that a while." } },
            { "afraid", new[] { "Keep walking. Keep walking.", "Please don't stop here. Please don't stop here.",
                "Eyes down. He doesn't know me. He doesn't need to know me.",
                "Don't let him see your hands shake. Fold them. There.",
                "If {who} comes into the store, I'll go out the back. I know the back.",
                "Pray God he's only riding through. Pray he keeps riding.",
                "I'll wait till {who} has gone round the corner. Then I'll breathe.",
                "He looked at me. Did he look at me? He looked at me.",
                "Busy. Look busy. Men like him leave busy people alone.",
                "Somebody stand between me and {who}. Anybody. The post will do.",
                "I've checked the bar on the door twice since he rode in. Three times.",
                "The last time {who} came through, I didn't sleep for a week." } },
            { "fond", new[] { "Good to see {who} back in town.", "Always tips his hat, that one.",
                "There's {who}. Town feels better with him in it, somehow.",
                "I like {who}. Can't say why, and I've given up trying.",
                "{who} again. I'll ask after his horse. He likes that.",
                "If {who} stops by, there's coffee on. There's always coffee on for him.",
                "Whatever he's done elsewhere, he's never been anything but civil to me.",
                "{who} laughs like a man who doesn't get to often. I like hearing it.",
                "The children like {who}. Children generally know.",
                "Hope {who} stays a while this time. The town could use him.",
                "There goes {who}. Wave, then. He'll wave back. He always does.",
                "Saved a slice of pie in case {who} came through. Don't tell anybody." } },
            { "met_talked", new[] { "I've talked to {who} before. Can't decide what I make of him.", "Him. We've spoken.", "Passed a word with {who} once. Civil enough.", "Spoke to {who} once. He didn't say much.",
                "We talked a minute once, {who} and me. About the weather, of all things.",
                "Spoke with {who} at the store once. He asked me where the good water was.",
                "{who}. Talked with him once. Listened more than he talked, which is rare.",
                "I've spoken to {who}. Pleasant enough. Eyes never stopped moving the whole time.",
                "Had a word with {who} once by the rail. Can't recall a thing that was said.",
                "We've spoken, {who} and I. He asked my name and seemed to keep it.",
                "I talked to him a while, once. Knew more about horses than I'd have guessed.",
                "I've talked with {who} before. Wouldn't say I know him. Wouldn't say I don't.",
                "He spoke to me once like I was somebody. I remember that about {who}.",
                "Swapped a word with {who} at the post office. He thanked me for nothing much." } },
            { "met_seen", new[] { "Seen {who} before, or someone the spit of him.", "That face again. Or one like it.", "Him. Seen him about, I think.", "I've seen that one in town before. Couldn't say where.",
                "Where have I seen {who}? Church? The station? It'll come to me when I stop trying.",
                "I know that face. Seen it on a poster, or in a pew. Hope it's a pew.",
                "I'm near sure I've seen him before. Never had a name to put to the face.",
                "That face. I've seen it before, across a street like this one.",
                "Somewhere I've seen {who} before. Can't place it, and it nags at me.",
                "His face is one I've seen before. Couldn't tell you where for a dollar.",
                "I've seen that man before. Or his brother. Or his ghost.",
                "Seen him on the road once, I think. Riding the other way, fast.",
                "Familiar face. Seen it before, and not anywhere good, I'd guess.",
                "Know the jaw on him. Seen it in town before, under a different hat." } },
        };

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return ByFeeling.Values.SelectMany(x => x);
        }

        /// Whether a thought, filled in, is one of this feeling's lines ("met_talked", "met_seen" for met), for the tests.
        public static bool FromPool(string feeling, string thought)
        {
            string[] pool;
            if (thought == null || feeling == null || !ByFeeling.TryGetValue(feeling, out pool))
                return false;
            foreach (string l in pool)
            {
                string pat = "^" + System.Text.RegularExpressions.Regex.Escape(l).Replace("\\{who}", ".+?").Replace("\\{him}", ".+?") + "$";
                if (System.Text.RegularExpressions.Regex.IsMatch(thought, pat, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    return true;
            }
            return false;
        }

        /// Whether a thought is one of these lines at all (any feeling), for the tests.
        public static bool IsFolksLine(string thought)
        {
            return ByFeeling.Keys.Any(k => FromPool(k, thought));
        }
    }
}
