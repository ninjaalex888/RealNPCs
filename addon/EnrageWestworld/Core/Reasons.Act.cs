// A reason between two people, as something one of them might choose to do (NIGHT3 item 1; Lex, 2026-10-31: "NPCs can
// start acting upon their inner thoughts with our current NPC interactions"). The model is asked what someone near does;
// the reasons the street already has for them and another (Reasons.Between: a quarrel to pick up, a friend to comfort, a
// debt to call in, someone to court) join the list in the words of the one who would start it. Only the one whose reason
// it is (ChosenBySecond: the reason's first, except a condolence, keeping clear, a debt still owed and a proposal with a
// woman first) is offered it: the other's part is to
// answer. Plain C#, no game.
namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        /// What the one who starts this reason's scene would be doing, in their words (lowercase, for the model's list);
        /// `other`: the other's first name. Null for a reason that isn't anyone's choice to make.
        public static string ActWords(Reason r, string other)
        {
            if (r == null || string.IsNullOrEmpty(r.Kind))
                return null;
            string o = string.IsNullOrEmpty(other) ? "them" : other;
            string over = r.Over != null && r.Over.StartsWith("snap:", System.StringComparison.Ordinal) ? " out of a bad week" : MatterWords(r.Matter);   // the snap's own (FOURTEENHOUR item 1)
            string k = r.Kind;
            if (k.StartsWith("court:", System.StringComparison.Ordinal))
                return "goes to sweet-talk " + o;
            switch (k)
            {
                case "argue": return "goes to have words with " + o + over;
                case "brawl": return "goes to settle things with " + o + " with their fists" + over;
                case "rough": return "goes to rough " + o + " up" + over;
                case "tiff": return "picks a small quarrel with " + o;
                case "mend": return "goes to make it up with " + o;
                case "condole": return "goes to offer " + o + " sympathy";
                case "tend": return "goes to see to " + o + ", who has just been hurt";
                case "beg": return "asks " + o + " for a coin";
                case "drunk": return "goes looking for trouble with " + o + ", drink in them";
                case "avoid": return "keeps out of " + o + "'s way";
                case "hosts": return "asks " + o + " whether the world seems wrong to them too";
                case "gossip": return "goes to pass on the talk to " + o;
                case "smalltalk": return "goes over for a word with " + o;
                case "x:debt": return "goes to see " + o + " about the money between them";
                case "x:feud": return "goes to settle the feud with " + o;
                case "x:snap": return "takes a bad week out on " + o;
                case "x:goodnews": return "goes to tell " + o + " the good news";
                case "x:badnews": return "goes to tell " + o + " the bad news";
                case "x:quarrel": return "goes to " + o + " with a grievance";
                case "x:sweet": return "goes to have a tender word with " + o;
                case "horsetrade": return "offers " + o + " a horse";   // the street's own (FOURTEENHOUR item 14)
                case "toothpull": return "offers to pull " + o + "'s bad tooth";
                case "wayasked": return "asks " + o + " the way";
                case "greenhorn": return "has some sport with " + o + ", the greenhorn";
                case "oldsoldiers": return "gets to talking with " + o + " about the war";   // (FOURTEENHOUR item 17)
                case "warstory": return "tells " + o + " of his war";
                case "snub": return "has a word for " + o + ", the gentleman";   // (FOURTEENHOUR item 18)
                case "rivals": return "squares up to " + o + " over the woman they both court";   // (item 19)
                case "talkof": return "tells " + o + " what they think of someone";   // (item 23)
                case "shelter": return "passes the time with " + o + " till the rain stops";   // (item 26)
                case "rowdy": return "comes down the street singing, and picks on " + o;   // (reserve R5)   // (FOURTEENHOUR item 16)
                default:
                    return k.StartsWith("x:", System.StringComparison.Ordinal) ? "goes to see " + o + " about what's between them" : "goes over to " + o;
            }
        }

        private static string MatterWords(string matter)
        {
            switch (matter)
            {
                case "money": return " over money";
                case "drink": return " over drink";
                case "honour": return " over a slight";
                case "woman": return " over a woman";
                case "land": return " over land";
                default: return "";
            }
        }
    }
}
