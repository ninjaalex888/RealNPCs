// The reason, shown (NIGHT3; the game developer's "legible reasons" and the research list's "show the reason"): when the
// street's timing starts a scene for a reason between two people, the one who starts it thinks why, a moment before they
// go over, so a player can follow the chain (a quarrel over money, a friend to comfort, the good news to tell). Scenes a
// mind starts from the model's pick have the model's own thought; these are for the rest. Plain C#; all words original.
namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        /// What the one starting it thinks, a moment before: the reason's kind and matter, the other's first name.
        public static string Thought(Reason r, string other, string seed)
        {
            if (r == null || string.IsNullOrEmpty(r.Kind) || string.IsNullOrEmpty(other))
                return null;   // no name for the other: no thought (the name is what makes it legible)
            string o = other;
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
                        case "land": return ReasonPick(seed, o + " and that fence line. It ends today.", "My land, not " + o + "'s. Time he heard it plain.");
                        default: return ReasonPick(seed, "I've a bone to pick with " + o + ", and it's today.", o + ". Can't let it lie any longer.");
                    }
                case "brawl": return ReasonPick(seed, o + " has had this coming a long while.", "Words won't do it with " + o + ". Not any more.");
                case "rough": return ReasonPick(seed, o + " needs a lesson taught.", "Look at " + o + ". Asking for it.");
                case "tiff": return ReasonPick(seed, o + "'s been short with me all week. I'll say something.", "Something's eating " + o + ". I'll find out what.");
                case "mend": return ReasonPick(seed, "Been too long, me and " + o + ". Time I put it right.", "Swallow it. Go and make it up with " + o + ".");
                case "condole": return ReasonPick(seed, o + " lost someone. Somebody ought to say a word.", "Poor " + o + ". I'll go and sit with them a minute.");
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
                case "x:goodnews": return ReasonPick(seed, "Got to tell " + o + ". Can't keep it in.", o + "! Wait till they hear.");
                case "x:badnews": return ReasonPick(seed, "Somebody has to tell " + o + ". Might as well be me.", "How do you tell " + o + " a thing like that?");
                case "x:quarrel": return ReasonPick(seed, o + " wronged me. I'll not let it lie.", "Been chewing on it all day. " + o + " hears it now.");
                case "x:sweet": return ReasonPick(seed, "Just a word with " + o + ". Just a kind one.", "There's " + o + ". My heart's going like a drum.");
                case "flirt": return ReasonPick(seed, o + " smiled at me once. Maybe twice.", "Go on. Say something to " + o + ".");
                case "propose": return ReasonPick(seed, "Today I ask " + o + ". Today, or never.", "The ring's in my pocket. " + o + " is right there.");
                default: return null;
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
                    {
                        string t = Thought(new Reason { Kind = k, Matter = m }, "Hiram", "all" + i);
                        if (t != null)
                            yield return t;
                    }
        }
    }
}
