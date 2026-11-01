// Hogtied by the road ([Encounters] Hogtied; re-staged from the game's beat_kidnap_victim, a woman tied up off the road
// with her captor's horse grazing near, and beat_hostage_rescue's plea: Staged.cs). Out of the towns by day, one of a
// nearby town's regulars, a man or a woman, lies tied up with two men standing over them, and who the men are comes
// from the captive's own life: bounty hunters if they're a known thief (a lawful claim: not to be shot unless they
// fight), a creditor's men over a debt, else kidnappers (the region's outlaws). The captive calls for help, bargains,
// curses them or keeps quiet in fear, by their nature; the men, when the player comes near, warn him off, offer to sell
// the captive for a price, pack up and go, or (outlaws only, or anyone once fired on) fight. Cut them free, pay, fight
// or ride on. Freed, the captive thanks you and goes home, or runs. All the words are RealNPCs' own. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Hogtied
    {
        public static readonly string[] CaptiveIds = { "call", "bargain", "curse", "quiet" };
        public static readonly string[] CaptorIds = { "warn", "bribe", "leave", "fight" };
        public static readonly string[] FreedIds = { "thank", "run" };
        public const string StruggleDict = "script_re@kidnap_victim", StruggleA = "a_upper_body_victim", StruggleB = "b_upper_body_01_victim", ThanksClip = "thank_you_a_victim";

        /// Who holds them: bounty (a known thief), creditor (a debt), kidnappers (anyone else).
        public static string CaptorsFor(bool knownThief, string worry)
        {
            return knownThief ? "bounty" : worry == "debt" ? "creditor" : "kidnappers";
        }

        /// What the men ask to let them go, in dollars: a bounty's worth, a debt and a little over, a ransom.
        public static int Price(string captors, string seed)
        {
            int r = Voice.Hash(seed + "|price") % 100;
            return captors == "bounty" ? 15 + r % 26 : captors == "creditor" ? 8 + r % 18 : 25 + r % 36;
        }

        /// The one tied up, each time they call out.
        public static Staged.Weights Captive(StagedMind m, string captors, bool youNear)
        {
            var w = new Staged.Weights().Add("call", 3).Add("bargain", 2).Add("curse", 1).Add("quiet", 2);
            switch (m.Temper)
            {
                case "Hothead": w.Add("curse", 4).Add("quiet", -1); break;
                case "Proud": w.Add("curse", 2).Add("bargain", -1); break;
                case "Greedy": w.Add("bargain", 4); break;
                case "Coward": w.Add("quiet", 3).Add("call", 1).Add("curse", -1); break;
                case "Cold": w.Add("quiet", 2).Add("bargain", 1).Add("call", -1); break;
                case "Loyal": w.Add("call", 2); break;
            }
            if (youNear)
                w.Add("call", 3).Add("quiet", -1);   // someone at last
            if (captors == "creditor")
                w.Add("bargain", 2);   // a debt can be talked about
            if (captors == "bounty")
                w.Add("curse", 1).Add("bargain", 1);
            if (m.Woman)
                w.Add("curse", -1);
            w.Clamp();
            if (w.Of("call") == 0)
                w.Add("call", 1);
            return w;
        }

        /// The men's leader, as the player comes near. Bounty hunters never start a fight (a lawful claim); with mayDraw
        /// false ([Encounters] HogtiedMenDraw off) nobody does, and the men fight only once the player steps in.
        public static Staged.Weights Captor(StagedMind m, string captors, bool mayDraw = true)
        {
            var w = new Staged.Weights().Add("warn", 4).Add("bribe", 2).Add("leave", 1).Add("fight", 1);
            switch (m.Temper)
            {
                case "Hothead": w.Add("fight", 3).Add("leave", -1); break;
                case "Greedy": w.Add("bribe", 4); break;
                case "Cold": w.Add("warn", 2).Add("fight", 1); break;
                case "Coward": w.Add("leave", 3).Add("fight", -1); break;
                case "Proud": w.Add("warn", 2).Add("leave", -1); break;
                case "Loyal": w.Add("warn", 1); break;
            }
            switch (captors)
            {
                case "bounty": w.Add("bribe", 1).Add("fight", -9); break;     // they'll sell a claim, they won't kill for it
                case "creditor": w.Add("bribe", 3).Add("fight", -1); break;   // the money is what they came for
                default: w.Add("fight", 1); break;
            }
            w.Clamp();
            if (!mayDraw)
                w.Add("fight", -w.Of("fight"));
            if (w.Of("warn") == 0)
                w.Add("warn", 1);
            return w;
        }

        /// Freed: what they do then.
        public static Staged.Weights Freed(StagedMind m, string captors)
        {
            var w = new Staged.Weights().Add("thank", 5).Add("run", 1);
            if (captors == "bounty")
                w.Add("run", 4).Add("thank", -2);   // a thief cut loose from a bounty runs for it (the game's "guilty" getting up)
            if (m.Temper == "Coward")
                w.Add("run", 2);
            if (m.Temper == "Loyal" || m.ToYou > 0)
                w.Add("thank", 2);
            w.Clamp();
            if (w.Of("thank") == 0)
                w.Add("thank", 1);
            return w;
        }

        /// What goes through their head as their own mind (or the men's leader's nature) settles the turn (the no-model run,
        /// item 1; ActThoughts.TurnPool): the reason for the choice, beside the choice's words.
        internal static readonly Dictionary<string, string[]> Turns = new Dictionary<string, string[]>
        {
            { "captive|call", new[] { "Somebody must hear. Louder.", "A rider. Shout. Shout now.", "Throat's raw. Keep calling." } },
            { "captive|bargain", new[] { "Everybody has a price. Find theirs.", "Offer them something. Anything.", "Money talks. Promise it, worry later." } },
            { "captive|curse", new[] { "Curse them. Curse every one of them.", "Get loose and they'll pay for this.", "Let them hear what I think of them." } },
            { "captive|quiet", new[] { "Quiet, or they'll hurt me.", "Not a sound. Don't give them a reason.", "Breathe slow. Wait." } },
            { "captor|warn", new[] { "He'll ride on if he knows what's good for him.", "Warn the stranger off before it gets messy.", "Make him think twice." } },
            { "captor|bribe", new[] { "Money's money. Let him pay for the fool.", "A few dollars, and the trouble's his.", "Name a price. See how badly he wants it." } },
            { "captor|leave", new[] { "Not worth a bullet. Not for this.", "Too much trouble. Pack it in.", "Let him have this one. Not worth the hide." } },
            { "captor|fight", new[] { "He won't take what's ours.", "Draw first. Ask after.", "One stranger. Two of us. Easy." } },
            { "freed|thank", new[] { "Free. Thank God, free.", "Hands free. Blood coming back into them.", "Alive. Still alive." } },
            { "freed|run", new[] { "Run. Don't stop till the road bends.", "Away before they come back.", "Free, and not waiting to learn why." } },
        };

        public static string Words(string role, string id, string captive)
        {
            if (role == "captive")
                switch (id)
                {
                    case "call": return "cries out for help to anyone who'll hear";
                    case "bargain": return "tries to buy their way out";
                    case "curse": return "curses the men holding them";
                    default: return "keeps quiet, too scared to make a sound";
                }
            if (role == "captor")
                switch (id)
                {
                    case "warn": return "warns the stranger to ride on";
                    case "bribe": return "offers to let " + captive + " go, for a price";
                    case "leave": return "decides it's not worth the trouble and packs up";
                    default: return "reaches for his gun to see the stranger off";
                }
            return id == "thank" ? "thanks the stranger who freed them" : "runs before anyone can tie them up again";
        }

        public static string Now(string role, string captive, string captors)
        {
            string who = captors == "bounty" ? "bounty hunters" : captors == "creditor" ? "men sent over a debt" : "kidnappers";
            if (role == "captive")
                return "you lie tied hand and foot by the road, held by " + who;
            if (role == "captor")
                return "you and another hold " + captive + " tied up off the road, and a stranger is riding up";
            return "you have just been cut loose by a stranger, the men who held you close by";
        }

        // ---- the words ------------------------------------------------------------------------------------------

        public static string FromCaptive(string choice, string captors, string seed)
        {
            switch (choice)
            {
                case "call": return Voice.Pick(seed + "hc", "Help! Somebody, help me!", "Over here! Please, I'm tied!", "Mister! Mister, cut me loose!");
                case "bargain":
                    return captors == "creditor" ? Voice.Pick(seed + "hc", "I'll pay it. Double, by Friday. Just let me up.", "Tell him I've got it put by. I swear I have.")
                         : Voice.Pick(seed + "hc", "I've money at home. Let me go and it's yours.", "Whatever they're paying you, I'll better it.");
                case "curse": return Voice.Pick(seed + "hc", "Rot, the pair of you!", "You'll hang for this. Both of you.", "Cut this rope and try me, you cowards.");
                default: return null;
            }
        }

        public static string Warn(string captors, string seed)
        {
            switch (captors)
            {
                case "bounty": return Voice.Pick(seed + "hw", "Lawful business, friend. There's paper on this one.", "Bounty. Ride on, it's ours.");
                case "creditor": return Voice.Pick(seed + "hw", "We're here about money owed. Move along.", "This one owes. Keep moving.");
                default: return Voice.Pick(seed + "hw", "You didn't see a thing. Turn that horse.", "Keep riding, unless you want a rope of your own.");
            }
        }

        public static string Bribe(string captors, int price, string seed)
        {
            string p = "$" + price;
            switch (captors)
            {
                case "bounty": return Voice.Pick(seed + "hb", "Paper says " + p + ". Pay it and you can have the wretch.", p + " and the claim's yours. Greet me if you mean it.");
                case "creditor": return Voice.Pick(seed + "hb", "Owes " + p + ". You pay it, we cut the rope.", "Settle " + p + " for them and we're gone.");
                default: return Voice.Pick(seed + "hb", "Ransom's " + p + ". Cash, now.", p + ", and they walk home with you.");
            }
        }

        public static string Leave(string seed)
        {
            return Voice.Pick(seed + "hl", "Not worth a fight. Leave them.", "Come on. Let somebody else feed them.");
        }

        public static string Fight(string seed)
        {
            return Voice.Pick(seed + "hf", "You were told.", "Should've kept riding.");
        }

        public static string Paid(string seed)
        {
            return Voice.Pick(seed + "hp", "Done, then. They're your trouble now.", "They're yours. Good luck to you.");
        }

        public static string Thanks(string captors, string seed)
        {
            return captors == "creditor" ? Voice.Pick(seed + "ht", "I'll pay it back. You and him both. Thank you.", "My wrists. Lord. Thank you, mister.")
                                         : Voice.Pick(seed + "ht", "I thought I'd die out here. Thank you.", "God bless you. I'm going home.", "I'll not forget your face, mister.");
        }

        public static string Run(string seed)
        {
            return Voice.Pick(seed + "hr", "Not going back!", "So long, mister!");
        }

        public static string Thought(string captors, string seed)
        {
            return captors == "bounty" ? Voice.Pick(seed + "hk", "Should never have taken that purse.", "Jail. Or worse. Think.")
                 : captors == "creditor" ? Voice.Pick(seed + "hk", "Over a few dollars. Tied like a hog.", "He sent them. He really sent them.")
                 : Voice.Pick(seed + "hk", "Rope's cutting in. Can't feel my hands.", "Where are they taking me.", "Somebody has to come by.");
        }

        /// What the town hears.
        public static string Heard(string name, string captors, bool freed, string town)
        {
            string where = town != null ? " on the " + town + " road" : " off the trail";
            if (freed)
                return name + " was found tied up" + where + " and cut loose by a stranger.";
            return captors == "bounty" ? name + " was taken in" + where + " by bounty hunters." : name + " was carried off" + where + ", tied across a saddle.";
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (string c in new[] { "bounty", "creditor", "kidnappers" })
                for (int i = 0; i < 8; i++)
                {
                    string s = "s" + i;
                    foreach (string id in CaptiveIds)
                    {
                        string l = FromCaptive(id, c, s);
                        if (l != null)
                            yield return l;
                    }
                    yield return Warn(c, s);
                    yield return Bribe(c, Price(c, s), s);
                    yield return Thanks(c, s);
                    yield return Thought(c, s);
                    yield return Now("captive", "Ada", c);
                    yield return Now("captor", "Ada", c);
                    yield return Heard("Ada Pell", c, i % 2 == 0, i % 3 == 0 ? null : "Valentine");
                    yield return Leave(s);
                    yield return Fight(s);
                    yield return Paid(s);
                    yield return Run(s);
                }
        }
    }
}
