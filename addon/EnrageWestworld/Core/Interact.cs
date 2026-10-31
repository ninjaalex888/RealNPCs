// The interaction menu (NIGHT3 item 4; roadmap 6; the design in roadmap.md "What the player can do"): locked on to someone
// RealNPCs knows, a "Talk" entry in the game's lock-on menu opens a short list of things Arthur can say, and the answer
// comes from who they are. Four to start: the news (the rumours of his own deeds, else their own worry, in their words),
// a drink stood (a saloon), money given, a warning. Each one is written down as a happening the mind already
// understands (a round and charity read as kindness, a standoff as a challenge), so gratitude, fear, the greetings as he
// passes, the rumours and the memories all follow from what exists; nothing new is saved. Plain C#, no game; all words
// original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class InteractOption
    {
        public string Id;        // news, drink, give, threaten, bye
        public string Label;     // what Arthur says, shown in the list
        public string Why;       // why it can't be chosen now (shown greyed), or null
    }

    public sealed class InteractAnswer
    {
        public string Line;      // what they say back, in their voice
        public string Event;     // the happening it is (round, charity, standoff), or null for none
        public int Cents;        // what it cost Arthur
        public int LikeDelta;    // for enRAGE's own townsfolk: its LikeMore, through its own method
        public string Then;      // what they do after: stay, step back, square up, walk off
        public string Thought;   // what they think of it, over their head
    }

    public static class Interact
    {
        public const int DrinkCents = 10;   // two beers at a nickel, 1899

        /// The list for this person now: saloon (a drink can be stood), cash (cents in Arthur's pocket), money worry (a
        /// gift is five dollars, not one).
        public static List<InteractOption> Options(bool saloon, int cash, bool moneyWorry)
        {
            int gift = moneyWorry ? 500 : 100;
            return new List<InteractOption>
            {
                new InteractOption { Id = "news", Label = "What's the news?" },
                new InteractOption { Id = "drink", Label = "Let me stand you a drink.", Why = !saloon ? "only in a saloon" : cash < DrinkCents ? "not enough money" : null },
                new InteractOption { Id = "give", Label = "Here, take this ($" + gift / 100 + ")", Why = cash < gift ? "not enough money" : null },
                new InteractOption { Id = "threaten", Label = "Watch yourself." },
                new InteractOption { Id = "bye", Label = "Goodbye." },
            };
        }

        /// Their answer: temper (Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter or null), devout (no drink), the rumour
        /// to pass on (Rumours.Version, about Arthur's deeds) or null, their worry's kind (InnerLife) or null, whether they'd
        /// tell him their worry (they like him, or he's asked before), and a seed.
        public static InteractAnswer Answer(string option, string temper, bool woman, bool devout, string rumour, string worry, bool confides, bool moneyWorry, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            switch (option)
            {
                case "news":
                    if (!string.IsNullOrEmpty(rumour))
                    {
                        string r = Grammar.UpperFirst(rumour.Trim().TrimEnd('.'));
                        a.Line = Pick(seed + "|news", r + ", so they say.", "Folks are saying " + rumour.Trim().TrimEnd('.') + ".", "Heard tell " + rumour.Trim().TrimEnd('.') + ". Make of it what you will.");
                        a.Thought = Pick(seed + "|nt", "Asking me what folk say about him. Odd.", "Does he know it's him they're talking of?", "Best tell him straight and be done.");
                    }
                    else if (worry != null && confides)
                    {
                        a.Line = WorryLine(worry, seed);
                        a.Thought = Pick(seed + "|wt", "Didn't mean to say so much.", "Feels better, saying it out loud.", "Why'd I tell a stranger that?");
                    }
                    else
                        a.Line = temper == "Cold" || temper == "Greedy" ? Pick(seed + "|cold", "Nothing I'd tell a stranger.", "News costs, mister.")
                               : Pick(seed + "|none", "Quiet enough. Nothing worth the telling.", "Same as ever, mister.", "Not much happens here that you'd care about.");
                    return a;
                case "drink":
                    if (devout)
                    {
                        a.Line = Pick(seed + "|dry", "Not a drop, thank you kindly.", "I don't take liquor, mister. But thank you.");
                        a.Thought = "Kindly meant, I suppose.";
                        return a;
                    }
                    a.Event = "round";
                    a.Cents = DrinkCents;
                    a.LikeDelta = 8;
                    a.Line = temper == "Cold" ? "If you're buying." : temper == "Hothead" ? "Now you're talking!" : Pick(seed + "|drink", "Well, I won't say no.", "Well, that's neighbourly.", "I'll take a whiskey, then.");
                    a.Thought = Pick(seed + "|dt", "A drink from a stranger. Can't complain.", "Free whiskey's the best kind.", "What's he want for it, I wonder.");
                    return a;
                case "give":
                    int gift = moneyWorry ? 500 : 100;
                    if (temper == "Proud")
                    {
                        a.Line = woman ? "Keep your money, mister. I manage." : "Keep your money. I ain't a beggar.";
                        a.Then = "walk off";
                        a.Thought = "Charity. From him. As if I couldn't manage.";
                        return a;
                    }
                    a.Event = "charity";
                    a.Cents = gift;
                    a.LikeDelta = moneyWorry ? 20 : 10;
                    a.Line = moneyWorry ? Pick(seed + "|mw", "Bless you. That keeps the wolf from the door a while.", "That's... that's the rent. Thank you.")
                           : temper == "Greedy" ? "A dollar? Is that the whole of it?"
                           : Pick(seed + "|give", "Well, I'm obliged to you.", "Well, I'll not refuse it. Obliged.", "What's this for? ...Thank you.");
                    a.Thought = moneyWorry ? "Somebody's looking out for me after all." : temper == "Greedy" ? "A soft touch. I'll remember his face." : "Money for nothing. Folks don't do that.";
                    return a;
                case "threaten":
                    a.Event = "standoff";
                    a.LikeDelta = -15;
                    switch (temper)
                    {
                        case "Coward":
                            a.Line = "Easy, easy! I never meant you any harm.";
                            a.Then = "step back";
                            a.Thought = "Don't look at him. Just don't.";
                            break;
                        case "Hothead":
                            a.Line = woman ? "Is that a threat? I've faced down meaner." : "You threatening me? Go on, then.";
                            a.Then = "square up";
                            a.Thought = "Let him try. Just let him try.";
                            break;
                        case "Proud":
                            a.Line = "I'll remember that, mister.";
                            a.Then = "walk off";
                            a.Thought = "He'll answer for that tone one day.";
                            break;
                        case "Cold":
                            a.Line = Pick(seed + "|thc", "Noted.", "Big man.");
                            a.Thought = "His kind don't make old bones.";
                            break;
                        default:
                            a.Line = Pick(seed + "|th", "Easy now. Easy.", "I didn't mean nothing by it.");
                            a.Then = "step back";
                            a.Thought = "What did I do? What did I do to him?";
                            break;
                    }
                    return a;
                default:
                    a.Line = Pick(seed + "|bye", "Take care, now.", "Go careful, now.", "So long, then.");
                    return a;
            }
        }

        /// Their worry, said aloud the way someone says it to a stranger they've warmed to.
        public static string WorryLine(string worry, string seed)
        {
            switch (worry)
            {
                case "tooth": return "This tooth's been killing me all week.";
                case "debt": return Pick(seed + "|debt", "I owe money I don't have, and the man wants it Friday.", "There's a debt hanging over me. Can't sleep for it.");
                case "fever": return "My little one's had a fever three days now.";
                case "feud": return "My neighbour's moved the fence line again. It'll come to blows.";
                case "letter": return "Waiting on a letter that never comes.";
                case "drink": return "Been drinking more than I ought. You'd think I'd learn.";
                case "secret": return "Got something on my mind I can't tell a soul.";
                case "courting": return "There's someone I can't stop thinking on. Can't find the words to tell them.";
                case "savings": return "Trying to put money by for a ticket west. It isn't easy.";
                case "son": return "My boy went off and I've heard nothing since.";
                case "bank": return "The bank's after the land. I don't know what we'll do.";
                case "law": return "There's a poster I'd rather nobody looked at too close.";
                default: return "Got my troubles, same as anyone.";
            }
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                foreach (string opt in new[] { "news", "drink", "give", "threaten", "bye" })
                    for (int i = 0; i < 6; i++)
                        foreach (bool w in new[] { false, true })
                        {
                            InteractAnswer a = Answer(opt, temper, w, i == 5, i % 2 == 0 ? "a man called Morgan shot a ranch hand" : null, i == 3 ? "debt" : null, i == 3, i == 4, "all" + i);
                            if (a.Line != null) yield return a.Line;
                            if (a.Thought != null) yield return a.Thought;
                        }
            foreach (string k in new[] { "tooth", "debt", "fever", "feud", "letter", "drink", "secret", "courting", "savings", "son", "bank", "law", "other" })
                yield return WorryLine(k, k);
        }
    }
}
