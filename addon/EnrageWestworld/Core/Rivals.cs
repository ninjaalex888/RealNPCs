// Rivals for one woman (FOURTEENHOUR item 19; [Scenes] Rivals). Two men courting the same woman meet: words, and by their natures
// blows (their friends may join: a brawl's own sides, item 6), one walking away from it ("She'll choose. Not you, not me."), or a
// cold "may the best man win". Then she chooses, by her nature and by how it went: the proud and the grasping take the winner of a
// fight, the kind and the steady the one who walked away from it (or the steadier man), and the cold, now and then, neither; the
// one she didn't choose takes it as grief or as a grudge, by his; and the town hears who won the fight and who won her, which aren't
// always the same man. Whom a man is courting: his courting worry's real person (Core/Links.cs, not yet ended), else a sweetheart
// he's warm with (Core/Ties.cs), else one of enRAGE's affairs of his (Core/Affair.cs; the engine already settles two of its suitors
// by a fight off-screen, RivalFight). Already there and left alone: the jealous husband or wife walking in on a sweet-talker
// (Scenes.Jealous), the box social's bidding between rivals (Core/BoxSocial.cs), the courting worry's own ending. Why it reads true:
// rivalry is the oldest plot there is, and it's best when the fight decides nothing. Plain C#, no game; the game side fills
// Someone.Courting and SceneCast.Courted, and settles her choice when the scene is over (Game/WestworldScript.Rivals.cs).
using System;

namespace EnrageWestworld.Core
{
    public static class Rivals
    {
        public static bool On = true;   // [Scenes] Rivals, from the settings
        public const int Percent = 70, Strength = 52, SaloonStrength = 54;

        /// The woman he's courting, and her name: his courting worry's (a link to a real person, not ended), else his most recent
        /// warm sweetheart, else one of enRAGE's affairs of his; null for none. `woman` says whether an id is a woman's.
        public static string CourtingOf(Store st, string id, Func<string, bool> woman, out string name)
        {
            name = null;
            if (st == null || id == null || id == "player")
                return null;
            foreach (WorryLink l in st.Links)
                if (l.Kind == "courting" && !l.Ended && Links.IsOf(l, id) && l.OtherId != null && l.OtherId != "player" && woman(l.OtherId))
                {
                    name = l.OtherName;
                    return l.OtherId;
                }
            SceneTie best = null;
            foreach (SceneTie t in st.Ties)
                if (t.Has(id) && Ties.Sweethearts(t) && t.Warmth > 0 && t.OtherId(id) != "player" && (best == null || t.At > best.At) && woman(t.OtherId(id)))
                    best = t;
            if (best != null)
            {
                name = best.Other(id);
                return best.OtherId(id);
            }
            foreach (Affair a in st.Affairs)
                if (a.Active && a.AId == id && a.BWoman && a.BId != null)
                {
                    name = a.BName;
                    return a.BId;
                }
            return null;
        }

        static bool Hot(string t) { return t == "Hothead" || t == "Proud"; }

        /// x and y, two men courting the same woman: a reason, or null (whatever they are to each other: Reasons.Between asks first).
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon)
        {
            if (!On || x == null || y == null || x.Id == y.Id || x.Woman || y.Woman || x.Courting == null || x.Courting != y.Courting || x.Courting == x.Id || x.Courting == y.Id)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|rivals", Percent))
                return null;
            string named = x.CourtingName ?? y.CourtingName;
            string her = named != null ? Person.FirstOf(named) : "the same woman";
            return new Reason { Kind = "rivals", Strength = saloon ? SaloonStrength : Strength, BFirst = Hot(y.Temper) && !Hot(x.Temper), Matter = "woman", Over = "rival",
                                Why = x.Name + " and " + y.Name + " are both courting " + her };
        }

        /// A and B, both courting her (c.Courted: her first name). It ends by their natures: blows, one walking away, or the best man.
        public static Scene Play(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed), her = c.Courted ?? "her";
            string ta = c.Tempers != null ? c.Tempers[0] : null, tb = c.Tempers != null ? c.Tempers[1] : null;
            string open = Voice.Pick(seed + "|open", "You've been calling on " + her + ", " + b + ".", "I hear you walked " + her + " home from church.", "Seen you at " + her + "'s gate again, " + b + ".");
            string back = Voice.Pick(seed + "|back", "And if I have?", "That's her business, not yours.", "She didn't seem to mind it.");
            string warn = Voice.Pick(seed + "|warn", "Keep away from her.", "She's spoken for.", "I was courting her before you ever got off the train.");
            string claim = Voice.Pick(seed + "|claim", "Not by you, she isn't.", "She'll choose for herself.", "Then let her tell me so.");
            bool hotA = Hot(ta), hotB = Hot(tb);
            bool blows = (hotA && (hotB || tb == "Cold")) || (hotB && ta == "Cold") || (hotA && Voice.Chance(seed + "|blows", 35)) || (hotB && Voice.Chance(seed + "|blowsb", 35));
            if (ta == "Coward" || tb == "Coward")
                blows = false;
            Scene s;
            if (blows)
            {
                s = new Scene { Kind = "brawl", Fight = true, NoLadder = true, Over = "rival", Summary = "two men courting the same woman came to blows over her" };
                s.Add(0, open, "mild", "face").Add(1, back, "mock").Add(0, warn, "angry").Add(1, claim, "angry", "shove")
                 .Add(0, Voice.Pick(seed + "|swing", "We'll settle it now.", "Then I'll tell you so myself.", "Put your hands up."), "angry", "fight", 9000)
                 .Add(0, null, null, "endfight");
            }
            else if (!hotA && !hotB && Voice.Chance(seed + "|best", 45))
            {
                s = new Scene { Kind = "argue", NoLadder = true, Over = "rival", Summary = "two men courting the same woman had words, and left it to her" };
                s.Add(0, open, "mild", "face").Add(1, back, "mild").Add(0, warn, "angry").Add(1, claim, "mild")
                 .Add(0, Voice.Pick(seed + "|best0", "May the best man win, then.", "We'll see who she picks.", "Fair enough. Let her pick."), "mild")
                 .Add(1, Voice.Pick(seed + "|best1", "He will.", "We'll see.", "She's no fool. She'll pick right."), "mock")
                 .Add(1, null, null, "walkaway");
            }
            else
            {
                // the cooler one walks away from it: he won't fight over her like dogs over a bone
                int walker = hotA ? 1 : hotB ? 0 : 1;
                s = new Scene { Kind = "argue", NoLadder = true, Over = "rival", Summary = "two men courting the same woman had words, and one walked away from a fight" };
                s.Add(0, open, "mild", "face").Add(1, back, "mock").Add(0, warn, "angry").Add(1, claim, "angry")
                 .Add(walker, Voice.Pick(seed + "|walk", "I'll not fight over her like a dog over a bone. She'll choose.", "No. I won't brawl over her. She deserves better than that.", "Hit me if you like. It won't make her yours."), "mild")
                 .Add(1 - walker, Voice.Pick(seed + "|coward", "Go on, then. Walk.", "That's what I thought.", "She'll not want a man who won't fight for her."), "mock")
                 .Add(walker, null, null, "walkaway");
            }
            s.Cast = 2;
            return s;
        }

        static int Steady(string t) { return t == "Loyal" ? 3 : t == "Plain" ? 2 : t == "Cold" || t == "Drifter" || t == "Greedy" || t == "Coward" ? 1 : 0; }

        /// Whom she chooses after it: 0 (A), 1 (B) or -1 (neither), by her nature and how it went: the proud and the grasping take
        /// the winner of a fight; the kind and the steady the one who walked away from it, or the steadier; the cold, now and then,
        /// neither. `winner`, `walkedAway`: 0, 1 or -1.
        public static int Chooses(string her, string ta, string tb, int winner, int walkedAway, string seed)
        {
            int neither = her == "Cold" || her == "Proud" ? 25 : 12;
            if ((Voice.Hash(seed + "|choose") & 0x7fffffff) % 100 < neither)
                return -1;
            bool likesWinner = her == "Proud" || her == "Greedy" || her == "Hothead";
            if (winner >= 0 && likesWinner && Voice.Chance(seed + "|win", 70))
                return winner;
            if (walkedAway >= 0 && !likesWinner && Voice.Chance(seed + "|walked", 60))
                return walkedAway;
            int sa = Steady(ta), sb = Steady(tb);
            if (sa != sb && Voice.Chance(seed + "|steady", 70))
                return sa > sb ? 0 : 1;
            return Voice.Chance(seed + "|coin", 50) ? 0 : 1;
        }

        /// The town's word on it: who won the fight and who won her, which aren't always the same man. `chosen` null: neither.
        public static string News(string her, string chosen, string other, bool fought, bool chosenWon, bool chosenWalked, bool otherWalked, string seed)
        {
            if (chosen == null)
                return Voice.Pick(seed + "|n0", her + " will have neither of them, she says. Not after the way they carried on.", "Neither one, " + her + " says. Two grown men, at it in the street like boys.");
            if (fought && !chosenWon)
                return Voice.Pick(seed + "|n1", other + " won the fight over " + her + ", and " + chosen + " won " + her + ". There's a lesson in that.", her + " has chosen " + chosen + ". It was " + other + " who won the fight.");
            if (fought)
                return Voice.Pick(seed + "|n2", chosen + " fought " + other + " over " + her + ", and won her too.", her + " is walking out with " + chosen + " now. " + other + " is nursing a black eye over it.");
            if (chosenWalked)
                return Voice.Pick(seed + "|n3", "It's " + chosen + " for " + her + ", they say. The one who wouldn't fight for her.", chosen + " walked away from a fight over " + her + ", and won her anyway.");
            if (otherWalked)
                return Voice.Pick(seed + "|n4", "It's " + chosen + " for " + her + ". " + other + " walked away from a fight and lost her anyway.", other + " wouldn't fight for " + her + ", and " + chosen + " has her now.");
            return Voice.Pick(seed + "|n5", her + " has chosen " + chosen + ". " + other + " is taking it hard.", "It's " + chosen + " for " + her + ". The best man, she says.");
        }

        /// What the one she didn't choose says, by his nature: a grudge from the hot and the cold, grief from the rest.
        public static string LoserLine(string temper, string seed)
        {
            return Hot(temper) || temper == "Cold"
                ? Voice.Pick(seed + "|grudge", "This isn't finished.", "He'll keep. I've a long memory.", "Let him have her. For now.")
                : Voice.Pick(seed + "|grief", "Well. That's that, then.", "I should have asked her sooner.", "I hope he's good to her.");
        }
    }
}
