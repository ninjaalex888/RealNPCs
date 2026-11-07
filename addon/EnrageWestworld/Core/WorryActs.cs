// Worries you can see (FIVEDAY item 7; the 11-05 plan: "worries are 42% of the thoughts over heads, but nothing shows them.
// People would act them out in small ways"). Each ordinary person carries one worry through the week (InnerLife.Worries),
// and until now it lived only in the words over their head. Here it leans what they choose to do (Choosing: a debtor keeps to
// his work and away from the cards, a parent with a feverish child goes home and prays, a toothache rests and reaches for the
// whiskey, a farmer in a dry spell keeps an eye on the sky), most at the worry's worst and not at all once it's over; and when
// they act on it, the thought over their head says why ("Home. The little one was hotter this morning than last night."). Every act is one
// the minds already play with the game's own scenarios (MindScenarios), so nothing new is asked of the engine; the worry is
// shown by what they do and when. Behind [Mind] WorriesShown.
//
// Why it reads true (the psychologist's lens): a worry narrows attention and bends the day around itself (rumination and
// preoccupation): people go where it can be answered (the post office, the church, home), avoid what would make it worse
// (the card table for a man in debt), and seek relief (a drink for the pain). Seen from the street, a worry is a pattern of
// small choices, which is what the player can notice. Plain C#, no game; all words original. mindtest WorryActsTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class WorryActs
    {
        /// How a worry leans a choice (Choosing's act ids), at full strength; Lean scales it by the worry's stage.
        public static float Base(string worry, string act)
        {
            switch (worry)
            {
                case "debt": return act == "work" ? 0.15f : act == "cards" ? -0.2f : act == "saloon" ? -0.12f : act == "store" ? -0.05f : 0f;
                case "bank": return act == "work" ? 0.15f : act == "pray" ? 0.08f : act == "cards" ? -0.15f : act == "saloon" ? -0.08f : 0f;
                case "savings": return act == "work" ? 0.12f : act == "store" ? -0.1f : act == "saloon" ? -0.08f : 0f;
                case "son": return act == "pray" ? 0.15f : act == "church" ? 0.12f : act == "watch" ? 0.06f : act == "showoff" ? -0.1f : 0f;
                case "fever": return act == "home" ? 0.2f : act == "pray" ? 0.12f : act == "saloon" ? -0.15f : act == "stroll" ? -0.12f : act == "cards" ? -0.15f : 0f;
                case "letter": return act == "store" ? 0.15f : act == "stroll" ? 0.05f : 0f;
                case "drought": return act == "watch" ? 0.12f : act == "pray" ? 0.1f : act == "work" ? 0.05f : 0f;
                case "tooth": return act == "rest" ? 0.12f : act == "drink" ? 0.1f : act == "eat" ? -0.15f : act == "talk" ? -0.1f : 0f;
                case "drink": return act == "drink" ? 0.15f : act == "saloon" ? 0.1f : act == "church" ? -0.08f : 0f;
                case "cards": return act == "cards" ? 0.15f : act == "saloon" ? 0.06f : 0f;
                case "courting": return act == "store" ? 0.1f : act == "stroll" ? 0.1f : act == "showoff" ? 0.06f : 0f;
                case "feud": return act == "watch" ? 0.1f : act == "steerclear" ? 0.06f : act == "greet" ? -0.05f : 0f;
                case "law": return act == "watch" ? 0.12f : act == "showoff" ? -0.15f : act == "church" ? -0.05f : 0f;
                case "secret": return act == "watch" ? 0.06f : act == "talk" ? -0.1f : 0f;
                default: return 0f;
            }
        }

        /// The lean by the worry's stage: 0 the start (40%), 1 deepening (70%), 2 at its worst (all), 3 over (none: it's
        /// settled, well or badly, and the day goes back to being theirs).
        public static float Lean(string worry, int stage, string act)
        {
            if (worry == null || stage >= 3)
                return 0f;
            float k = stage <= 0 ? 0.4f : stage == 1 ? 0.7f : 1f;
            return Base(worry, act) * k;
        }

        // ---- the thought when the act is the worry's ("worry|act"); {who} the one at the other end of it, when known ----
        private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            { "debt|work", new[] { "Every hour worked is a dollar off what I owe {who}.", "Head down. Work. Don't think about the note.", "Nobody takes the mule from a man who's working." } },
            { "bank|work", new[] { "The bank's note won't wait on the weather. Neither will I.", "If I work till dark every day, maybe.", "Another hour. The bank don't care how tired I am." } },
            { "bank|pray", new[] { "Lord, I've never asked for money. I'm asking now.", "Keep the roof over us, Lord. That's all.", "The bank has the paper. You have the rest of it, Lord." } },
            { "savings|work", new[] { "A little more in the tin each week. That's the way out.", "Eleven dollars to go. Then the ticket.", "Not a cent wasted. Not one." } },
            { "son|pray", new[] { "Bring my boy home, Lord, wherever he's sleeping tonight.", "Watch over my boy. He thinks he can't be hurt.", "Lord, I'll do anything. Keep him safe." } },
            { "son|church", new[] { "The pastor says leave it with God. I'll leave it with God and worry anyway.", "Lit a candle for my boy. The good kind.", "I sit in the same pew. Like it helps." } },
            { "son|watch", new[] { "Every rider on the road, I look to see if it's him.", "If a letter comes, I'll be first at the counter.", "Watching the road again. Can't help it." } },
            { "fever|home", new[] { "Home. The little one was hotter this morning than last night.", "Can't stay out. The little one needs me.", "Got the quinine. Now home, quick." } },
            { "fever|pray", new[] { "Take the fever, Lord. Take it out of the child and put it in me.", "Doctor's done what he can. Now You.", "So small, Lord. Too small for this." } },
            { "letter|store", new[] { "Maybe today. The mail came in on the noon train.", "Ask at the counter again. Don't care if they laugh.", "Nothing yesterday. Today, maybe." } },
            { "letter|stroll", new[] { "Walk past the post office. Just to see.", "If I don't look, it'll come. That's how it goes.", "Weeks now. Not a line." } },
            { "drought|watch", new[] { "Not a cloud. Not one. The sky's like brass.", "Smells like rain off west. Smelled like it yesterday too.", "Watching the sky won't bring it. Can't stop, though." } },
            { "drought|pray", new[] { "Rain, Lord. Just a week of rain.", "The well's down to mud. Please.", "Send rain on the just and the unjust. I'm not particular." } },
            { "drought|work", new[] { "Hauling water by the bucket again. Back's breaking.", "The corn won't last the week like this.", "Work the ground while it's still worth working." } },
            { "tooth|rest", new[] { "Can't think for the tooth. Sit. Just sit.", "The whole side of my face is a drum.", "If I hold still, maybe it'll quiet down." } },
            { "tooth|drink", new[] { "Whiskey on the tooth. Only thing that touches it.", "One for the pain. One for the patience.", "Swish it round. Swallow. Again." } },
            { "drink|drink", new[] { "Just the one. I keep saying just the one.", "My hand's steadier with a glass in it.", "Said I'd stop Sunday. It isn't Sunday." } },
            { "drink|saloon", new[] { "Only going in to get warm.", "My feet know the way by themselves now.", "Somebody in there owes me a drink. Probably." } },
            { "cards|cards", new[] { "Win back last week's and I'm square. That's all I want.", "Luck turns. It has to turn.", "One more hand. Then home." } },
            { "cards|saloon", new[] { "There'll be a game. There's always a game.", "I'll only watch. Won't sit in.", "Feel lucky tonight. Feel it in my hands." } },
            { "courting|store", new[] { "Something for {who}. Not too dear, not too plain.", "Peppermints, or a cake of soap? What would {who} like?", "Should I buy it? It's foolish. I'll buy it." } },
            { "courting|stroll", new[] { "If I walk past {who}'s at noon, maybe.", "Clean collar. Walk slow. Don't look too hopeful.", "Just taking the air. Past {who}'s window." } },
            { "courting|showoff", new[] { "If {who} is watching, I'd best stand straight.", "Let {who} see me at my best for once.", "Hat brushed, boots blacked. Now be seen." } },
            { "feud|watch", new[] { "If {who} comes up this street, I want to see it coming.", "Keep the wall at my back. {who} has a temper.", "Every door that opens, I look." } },
            { "feud|steerclear", new[] { "Not today, {who}. I won't give you the satisfaction.", "Cross over. Let {who} have that side.", "Not worth the trouble. Not in the street." } },
            { "law|watch", new[] { "That deputy looked at me twice. Twice.", "If they've the poster up at the station, I'm done.", "Keep to the edge of things. Keep quiet." } },
            { "secret|watch", new[] { "Does anybody know? They'd look different at me if they knew.", "Keep your face still. Nobody knows.", "Every whisper, I think it's about me." } },
        };

        /// The thought that goes with an act begun for a worry, or null when the act isn't the worry's or there's no line.
        public static string Thought(string worry, string act, string seed)
        {
            string[] pool;
            if (worry == null || act == null || !Lines.TryGetValue(worry + "|" + act, out pool))
                return null;
            return pool[(Voice.Hash(seed + "|wa") & 0x7fffffff) % pool.Length];
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in Lines)
                foreach (string l in kv.Value)
                    yield return l.Replace("{who}", "Ezra");
        }

        /// The pairs that have words (for the tests: every one leans that act up).
        public static IEnumerable<string> Pairs()
        {
            return Lines.Keys;
        }
    }
}
