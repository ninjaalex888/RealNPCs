// Group acts (SIXDAY item 21; roadmap 39): acts for several people at once that the mind, and the model when it's on,
// can choose. Two here: friends closing round a beaten friend (after a street fight, the warmest friend near may fetch
// the next and the two close round the man who went down, see him up and walk him off: a scene of three, offered to the
// first friend's mind beside tending him alone or leaving it), and a family walking off together (an onlooker whose
// mind says leave takes their spouse or sweetheart with them, side by side, with a word: WestworldScript.Onlookers.cs).
// A crowd gathering was already the Director's (the brawl's crowd, the body's watchers). Why it reads true: people act
// in their ties before they act alone, and the first thing a frightened man does is find his own. Plain C#, no game;
// all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class GroupActs
    {
        public static readonly string[] Picks = { "tend", "close", "leave" };

        /// The first friend's table after a fight, with another friend near: tend him alone, fetch the other and close round
        /// him, or leave it. The loyal and the proud close round; the timid leave; a sore mood leaves sooner.
        public static Staged.Weights CloseRoundWeights(StagedMind m, int friendsNear)
        {
            string t = m != null ? m.Temper : null;
            int tend = 3, close = friendsNear >= 2 ? 4 : 1, leave = 1;
            switch (t)
            {
                case "Loyal": close += 2; leave = 0; break;
                case "Proud": close += 2; break;
                case "Hothead": close += 1; break;
                case "Coward": leave += 3; tend -= 1; close -= 1; break;
                case "Cold": leave += 1; close -= 1; break;
                case "Drifter": leave += 1; break;
            }
            if (m != null && m.Mood == "sore") leave += 1;
            if (m != null && m.Warmth >= 2) { close += 1; leave = Math.Max(0, leave - 1); }
            var w = new Staged.Weights();
            w.Add("tend", Math.Max(1, tend)).Add("close", Math.Max(0, close)).Add("leave", Math.Max(0, leave));
            return w.Clamp();
        }

        /// The option as the model is offered it.
        public static string Words(string id, string otherFirst)
        {
            switch (id)
            {
                case "close": return "fetch " + (otherFirst ?? "the other") + " and close round him";
                case "leave": return "let it be; he would only hate the fuss";
                default: return "go to him yourself and see to him";
            }
        }

        public static string Now(string loserFirst)
        {
            return (loserFirst ?? "A friend of yours") + " is down in the street after a fight, and you saw it.";
        }

        /// Two friends close round the man who went down: A the first friend, B the one down, C the second friend. C comes over,
        /// A and C each say their piece, B answers by his temper, is got up, and A walks him off past C, who goes his own way.
        public static Scene CloseRound(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string bt = c.Tempers != null && c.Tempers.Length > 1 ? c.Tempers[1] : null;
            Scene s = new Scene { Kind = "comfort", Summary = "two friends closed round a beaten friend" };
            s.Beats.Add(new Beat(2, null, null, "approach", 200) { To = 1 });
            s.Add(0, Voice.Pick(seed + "|ca", "Easy, " + b + ". Easy. We've got you.", "Hold still, " + b + ". Let me see your head.", b + "? " + b + ". Look at me. There."), "mild", "face");
            s.Beats.Add(new Beat(2, Voice.Pick(seed + "|cc", "Who did this? ...Never mind. Later.", "Lord, look at him. Get his other arm.", "Is he... " + b + ", can you hear us?"), "angry") { To = 1 });
            string answer = bt == "Hothead" || bt == "Proud" ? Voice.Pick(seed + "|cb", "Get off me. I'm fine. I'm fine.", "Took him a while, didn't it. Took him a while.")
                          : bt == "Coward" ? Voice.Pick(seed + "|cb", "Don't leave me. Don't let him come back.", "...I didn't do anything. I didn't.")
                          : Voice.Pick(seed + "|cb", "...I'll live. Help me up.", "Thank God it's you two.");
            s.Add(1, answer, bt == "Coward" ? "scared" : "mild");
            s.Add(1, null, null, "getup", 600);
            s.Add(0, Voice.Pick(seed + "|cw", "Come on. We'll see you home.", "Lean on me. " + a + "'s got you.", "That's it. One foot, then the other."), "mild");
            s.Beats.Add(new Beat(0, null, null, "leavewith") { To = 1 });
            s.Beats.Add(new Beat(1, null, null, "leavewith") { To = 1 });   // and the man himself, beside A (both name B, so the Director's spot is the same point); without this beat only A walked (the review of items 20-27)
            s.Add(2, null, null, "walkaway", 800);
            return s;
        }

        /// The onlooker who leaves takes their own with them: the word to the spouse or sweetheart.
        public static string TakeAway(string partnerFirst, bool partnerWoman, string seed)
        {
            string p = partnerFirst ?? (partnerWoman ? "my dear" : "love");
            return Voice.Pick(seed + "|ta", "Come away, " + p + ". It's none of ours.", "Don't look, " + p + ". Walk.", p + ". Home. Now.", "Take my arm, " + p + ". We're going.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 6; i++)
            {
                yield return TakeAway("Martha", true, "t" + i);
                yield return TakeAway("Amos", false, "u" + i);
                yield return TakeAway(null, true, "v" + i);
            }
            foreach (string id in Picks)
                yield return Words(id, "Clem");
            yield return Now("Abner");
        }
    }
}
