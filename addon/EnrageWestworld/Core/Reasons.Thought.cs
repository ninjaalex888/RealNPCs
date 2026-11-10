// The reason, shown (NIGHT3; the game developer's "legible reasons" and the research list's "show the reason"): when the
// street's timing starts a scene for a reason between two people, the one whose reason it is thinks why, a moment before
// they go over, so a player can follow the chain (a quarrel over money, a friend to comfort, the good news to tell).
// Someone they know they think of by name; a stranger as "that fellow" or "the lady", and only for the reasons a stranger
// can be in (a snap, drink, a coin asked for, a word passed, a flirt); history between them only with someone they know.
// Scenes a mind starts from the model's pick have the model's own thought; these are for the rest. Plain C#; all words
// original to this add-on.
namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        /// What the one whose reason it is thinks, a moment before: the reason's kind and matter, the other's first name
        /// (`known`: they know them by name, else they're a stranger to them), whether the other is a woman.
        public static string Thought(Reason r, string other, string seed, bool known = true, bool otherWoman = false)
        {
            if (r == null || string.IsNullOrEmpty(r.Kind))
                return null;
            if (!known)
                return StrangerThought(r, otherWoman, seed);
            if (string.IsNullOrEmpty(other))
                return null;   // no name for someone they know: no thought (the name is what makes it legible)
            string o = other, him = otherWoman ? "her" : "him", he = otherWoman ? "she" : "he";
            string k = r.Kind;
            if (k.StartsWith("court:", System.StringComparison.Ordinal))
                return ReasonPick(seed, o + ". Steady now. Say something worth hearing.", "Go on. Talk to " + o + ". What's the worst of it?");
            switch (k)
            {
                case "argue":
                    switch (r.Matter)
                    {
                        case "money": return ReasonPick(seed, o + " still owes me. Today I say so.", "That money. " + o + " hears about it now.");
                        case "drink": return ReasonPick(seed, o + " and the bottle. I've had a bellyful of it.", "Drunk again, " + o + ". Somebody has to say it.");
                        case "honour": return ReasonPick(seed, o + " spoke ill of me. Not twice.", "What " + o + " said about me. I'll not swallow it.");
                        case "woman": return ReasonPick(seed, o + " has been sniffing round where he's no business.", "Saw how " + o + " looked at her. Enough.");
                        case "land": return ReasonPick(seed, o + " and that fence line. It ends today.", "My land, not " + o + "'s. Time " + he + " heard it plain.");
                        default: return ReasonPick(seed, "I've a bone to pick with " + o + ", and it's today.", o + ". Can't let it lie any longer.");
                    }
                case "brawl": return ReasonPick(seed, o + " has had this coming a long while.", "Words won't do it with " + o + ". Not any more.");
                case "rough": return ReasonPick(seed, o + " needs a lesson taught.", "Look at " + o + ". Asking for it.");
                case "tiff": return ReasonPick(seed, o + "'s been short with me all week. I'll say something.", o + " and that tone. Not today.");
                case "mend": return ReasonPick(seed, "Been too long, me and " + o + ". Time I put it right.", "Swallow it. Go and make it up with " + o + ".");
                case "condole": return ReasonPick(seed, o + " lost someone. Somebody ought to say a word.", "Poor " + o + ". I'll go and sit with " + him + " a minute.");
                case "tend": return ReasonPick(seed, o + "'s hurt. Somebody has to see to it.", "Look at " + o + ". Needs a hand.");
                case "beg": return ReasonPick(seed, "Maybe " + o + " can spare a coin. Can't hurt to ask.", "Swallow your pride. Ask " + o + ".");
                case "drunk": return ReasonPick(seed, "Whiskey says " + o + " looked at me funny.", o + ". Think I'll have a word. A loud one.");
                case "avoid": return ReasonPick(seed, "Don't let " + o + " see me. Not today.", "There's " + o + ". Other side of the street, then.");
                case "hosts": return ReasonPick(seed, "Does " + o + " feel it too? That none of this is right?", o + " looks like I feel. I have to ask.");
                case "gossip": return ReasonPick(seed, "Wait till " + o + " hears this.", o + " will want to know. Everybody will.");
                case "smalltalk": return ReasonPick(seed, "There's " + o + ". Might as well pass the time.", "A word with " + o + ". Where's the harm.");
                case "debt": case "x:debt": return ReasonPick(seed, o + " and that money. One way or another, today.", "Owed is owed. I'm settling it with " + o + " today.");
                case "x:feud": return ReasonPick(seed, "This business with " + o + " ends today.", o + ". No more creeping round each other.");
                case "x:snap": return ReasonPick(seed, "Rotten week. And here's " + o + ", in the way of it.", "Everything's gone wrong. " + o + " can hear about it.");
                case "x:goodnews": return ReasonPick(seed, "Got to tell " + o + ". Can't keep it in.", o + "! Wait till " + he + " hears.");
                case "x:badnews": return ReasonPick(seed, "My news is bad, and " + o + " should have it first.", "Can't carry it alone. I'll tell " + o + ".");
                case "x:quarrel": return ReasonPick(seed, o + " wronged me. I'll not let it lie.", "Been chewing on it all day. " + o + " hears it now.");
                case "x:sweet": return ReasonPick(seed, "Just a word with " + o + ". Just a kind one.", "There's " + o + ". My heart's going like a drum.");
                case "horsetrade": return ReasonPick(seed, o + "'s looking for a horse. I've just the one.", "Sell " + o + " the bay. Don't mention the cough.");   // (FOURTEENHOUR item 14)
                case "toothpull": return ReasonPick(seed, o + "'s tooth again. I've pliers and a strong arm.", "Somebody ought to put " + o + " out of his misery.");
                case "talkof": return ReasonPick(seed, "Wait till " + o + " hears what I've got to say.", o + "'ll want to know about that one.");   // (item 23)
                case "rivals": return ReasonPick(seed, "There's " + o + ". Sniffing round my girl again.", o + " thinks he's got a chance with her. We'll see about that.");   // (item 19)
                case "snub": return ReasonPick(seed, "I'll ask the gentleman. Can't hurt to ask.", o + " in his fine coat. Wonder if he'd give a working man the time of day.");   // (item 18)
                case "oldsoldiers": return ReasonPick(seed, o + " has the look of an old soldier. I wonder which side.", "Old men and old wars. " + o + " was in it, I'd bet.");   // (FOURTEENHOUR item 17)
                case "warstory": return ReasonPick(seed, "Home a month, and nobody's asked me what it was like.", "Somebody ought to hear what it was like out there.");
                case "greenhorn": return ReasonPick(seed, "Here's the new man, " + o + ". Let's see what he's made of.", o + " and those boots. Somebody has to say something.");   // (FOURTEENHOUR item 16)
                case "wayasked": return ReasonPick(seed, "Don't know a soul here. " + o + " looks like he'd know.", "Ask " + o + ". Can't wander this town all day.");
                case "flirt": return ReasonPick(seed, o + " smiled at me once. Maybe twice.", "Go on. Say something to " + o + ".");
                case "propose": return ReasonPick(seed, "Today I ask " + o + ". Today, or never.", "The ring's in my pocket. " + o + " is right there.");
                default: return null;
            }
        }

        /// A stranger, by the reasons a stranger can be in; null for the rest (a quarrel's history, a friend's grief, a
        /// sweetheart: those are between people who know each other).
        private static string StrangerThought(Reason r, bool woman, string seed)
        {
            string one = woman ? "the lady there" : "that fellow";
            if (r.Kind.StartsWith("court:", System.StringComparison.Ordinal))
                return woman ? ReasonPick(seed + "s", "Pretty. Go on, say something.", "A word with her. What's the worst she can say?")
                             : ReasonPick(seed + "s", "Handsome, that one. Go on, say something.", "A word with him. What's the worst he can say?");
            switch (r.Kind)
            {
                case "x:snap": return ReasonPick(seed + "s", "Rotten day. And here's " + one + ", in the way of it.", "One more thing today and I'll bite somebody's head off.");
                case "drunk": return ReasonPick(seed + "s", "Whiskey says " + one + " looked at me funny.", "Somebody in here wants a lesson. " + Grammar.UpperFirst(one) + " will do.");
                case "beg": return ReasonPick(seed + "s", "Maybe " + one + " can spare a coin. Can't hurt to ask.", "Swallow your pride. Ask.");
                case "rough": case "brawl": case "argue": return ReasonPick(seed + "s", "Look at " + one + ". Asking for it.", Grammar.UpperFirst(one) + "'s been eyeing me.");
                case "x:goodnews": return ReasonPick(seed + "s", "Got to tell somebody. Anybody.", "Good news won't keep. " + Grammar.UpperFirst(one) + " will do.");
                case "x:badnews": return ReasonPick(seed + "s", "Got to tell somebody, or I'll burst.", "Can't carry it alone. Even a stranger's ear.");
                case "gossip": return ReasonPick(seed + "s", "Wait till " + one + " hears this.", "Somebody ought to know. Might as well be " + one + ".");
                case "hosts": return ReasonPick(seed + "s", Grammar.UpperFirst(one) + " looks like I feel. I have to ask.", "Does anyone else feel it? That none of this is right?");
                case "smalltalk": return ReasonPick(seed + "s", "Might as well pass the time with somebody.", "A word with " + one + ". Where's the harm.");
                default: return null;
            }
        }

        /// Whether the one whose choice this reason is plays second in its scene: a condolence (the first is the grieving
        /// one; the friend goes to them), keeping clear (the first did the wrong; the other keeps clear of him), a debt
        /// still owed (the one owed calls it in; once it's paid, the debtor goes to pay it), and a proposal where the first
        /// is a woman (the man kneels; two women, the second). `first`: the scene's first; `gameDay` for the debt's stage.
        public static bool ChosenBySecond(Reason r, Someone first, long gameDay)
        {
            if (r == null)
                return false;
            switch (r.Kind)
            {
                case "condole": case "avoid": return true;
                case "x:debt": return first != null && first.Id != null && InnerLife.StageFor(first.Id, gameDay) < 3;
                case "propose": return first != null && first.Woman;
                default: return false;
            }
        }

        private static string ReasonPick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed + "|rt") % lines.Length];
        }

        /// Every reason thought, for the tests.
        public static System.Collections.Generic.IEnumerable<string> AllThoughts()
        {
            string[] kinds = { "argue", "brawl", "rough", "tiff", "mend", "condole", "tend", "beg", "drunk", "avoid", "hosts", "gossip", "smalltalk", "debt", "x:debt", "x:feud", "x:snap",
                               "x:goodnews", "x:badnews", "x:quarrel", "x:sweet", "flirt", "propose", "court:walk" };
            foreach (string k in kinds)
                foreach (string m in new[] { null, "money", "drink", "honour", "woman", "land" })
                    for (int i = 0; i < 6; i++)
                        foreach (bool known in new[] { true, false })
                            foreach (bool w in new[] { false, true })
                            {
                                string t = Thought(new Reason { Kind = k, Matter = m }, "Hiram", "all" + i, known, w);
                                if (t != null)
                                    yield return t;
                            }
        }
    }
}
