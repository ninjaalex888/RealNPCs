// People talk to each other from what's on their minds (Lex, 2026-11-03: "have the NPCs use their voicelines, having
// conversations with other NPCs nearby based on their inner thoughts"; [Mind] MindTalks). The same things their thoughts come
// from are what one of them brings up to the other: the worry they're living through (in the words their thoughts use), the
// times (Times.cs: the election, the war, the reservations, the new machines), the stranger (what they remember of the
// player), a plan they're carrying (getting even, paying back, courting), the town's rumour, what they need (a meal, a word,
// a dollar, sleep), or the day. The other answers from their own nature: kindly, coldly or sharply (Exchanges.Manner), and on
// the times from their own side of the question (Stance: the lawful and the well-off for the government, the reckless against
// it; the compassionate and the devout for people, the hard against them; the curious for the new, the orderly against it;
// and nobody speaks against their own people). Then the first has a last word, or not. The scene is built here and played
// by the Director like any other; with the AI model on, the game side has the model word the answer (Language.Talk.cs, the
// Speaker field) and the beat takes it as it plays (Beat.Late). All lines are original to this add-on. Plain C#, no game;
// checked by mindtest (MindTalkTest).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    /// One of the two, as this needs them beyond Someone: the sides of their nature that decide a stance, their people, their need.
    public sealed class MindTalkMind
    {
        public string Origin;                // their people, as their name was chosen (White, Black, Hispanic, Chinese, Native, Irish, French, Italian, German, Scandinavian), or null
        public bool Devout, Lawful, Reckless, Compassionate, Hard, Curious, Orderly, WellOff;
        public string Need;                  // the most pressing need in a word (hungry, lonely, money, rest, safety), or null
        public string StrangerRegard;        // how they take the player: afraid, hostile, warm, or null (they have no view)
    }

    public sealed class MindTalkContext
    {
        public int Year = 1899, Month;       // the game's year and month (the times are by both)
        public int DayOfMonth;               // the day of the month (the almanac's dated news, Core/Almanac.cs; 0 unknown)
        public long Day;                     // the game day (the worry's stage)
        public string Town;
        public List<string> Rumours;         // the town's talk, or null
        public MindTalkMind A, B;            // the one who speaks first, and the other
        public string AMemory;               // what A remembers of the player, in A's own words (a thought), or null
        public string APlan, APlanKind;      // a plan A carries, as A's thought, and its kind (geteven, repay, paydebt, court), or null
        public string Topic;                 // the topic set by the reason (MindTalk.TopicOf: "times" for a talk of the times), or null to choose
        public bool BAtWorst;                // B at the worst of their own trouble (the answer comes sharp)
    }

    public static class MindTalk
    {
        public const string Kind = "mindtalk";
        public const string TimesKind = "timestalk";   // the reason for a talk of the times (Reasons.TimesReason; Run 3, item 2); the scene is a mindtalk
        public const int TimesMoreChance = 60;          // a talk of the times goes a second round (another word on it, another answer) this often

        /// The reasons that play as a talk between two people: what's on a mind, and the times talked over.
        public static bool IsTalkKind(string kind)
        {
            return kind == Kind || kind == TimesKind;
        }

        /// The topic a reason sets ("times" for a talk of the times), or null for the talk to choose.
        public static string TopicOf(string kind)
        {
            return kind == TimesKind ? "times" : null;
        }

        /// Another line of the pool than this one, by the seed (the second round of a talk of the times), or null.
        private static string Other(string[] pool, string seed, string notThis)
        {
            if (pool == null || pool.Length < 2)
                return null;
            for (int i = 0; i < 4; i++)
            {
                string l = Voice.Pick(seed + "|o" + i, pool);
                if (l != notThis)
                    return l;
            }
            int at = Array.IndexOf(pool, notThis);
            return pool[(at + 1) % pool.Length];
        }

        // ---- what one brings up ----------------------------------------------------------------------------------

        private static readonly string[] DayOpeners =
        {
            "Hot one, {b}.", "Quiet today. Too quiet, my pa would've said.", "Mud to the axles on the north road. Lost a boot in it.",
            "Smell that? Somebody's baking. Not my house, worse luck.", "Wind's turning. Rain by supper, I'd say.", "Saw the stage come in. Nobody I knew on it.",
            "Church was full Sunday. Full of hats, anyhow.", "Dust on everything. I washed that shirt yesterday.", "Days are drawing in. I hate the dark coming at five.",
            "Fine morning, {b}. Makes a body almost glad to be up."
        };

        private static readonly Dictionary<string, string[]> NeedOpeners = new Dictionary<string, string[]>
        {
            { "hungry", new[] { "Haven't eaten since yesterday noon. Store won't give credit and the garden's dust.", "You smell dinner anywhere, {b}? I'd follow it like a dog." } },
            { "lonely", new[] { "Days go by and I don't say ten words to a soul. Good to see a face, {b}.", "The house is quiet enough to hear the clock think. I came out for the noise." } },
            { "money", new[] { "Short again this week. Short every week, if I'm honest.", "Paid the store and the rent and there's forty cents between me and Sunday." } },
            { "rest", new[] { "I could sleep standing up. Been on my feet since dark.", "Didn't close my eyes last night. Dogs, then the wind, then the thinking." } },
            { "safety", new[] { "I don't like the look of some of the men riding through lately.", "Keep your door barred nights, {b}. There's something about this week I don't trust." } }
        };

        private static readonly string[] RumourOpeners = { "Heard tell {r}. Make of it what you will.", "They're saying {r}. Whole town's saying it.", "You hear that {r}? I had it from the store." };

        // ---- how the other answers -------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> KindToWorry = new Dictionary<string, string[]>
        {
            { "debt", new[] { "Pay what you can and keep out of his road. Men like that forget slower than they're paid.", "If it's a dollar or two, say so. I've been where you stand." } },
            { "fever", new[] { "Keep the window shut and the broth coming. It breaks, mostly. It did for mine.", "I'll send the boy over with eggs. Don't argue." } },
            { "letter", new[] { "It'll come. The mails are slow, not the people.", "No news is no news, {a}. Don't make it worse than it is." } },
            { "drought", new[] { "Sky owes us. It'll pay, it always pays. Late, like everybody.", "There's water at my place yet. Bring the stock by if it comes to that." } },
            { "son", new[] { "Boys go quiet when they're busy living. He'll write.", "If I hear a word of him on the road, you'll have it the same hour." } },
            { "courting", new[] { "Then ask. Worst they can do is say no, and you've heard no before.", "Wear the clean collar and say it plain. Plain works." } },
            { "feud", new[] { "Let it lie, {a}. Nobody ever won one of those, they just outlived the other.", "Walk the other way when you see him. Costs nothing and keeps you out of a box." } },
            { "bank", new[] { "Banks want the money, not the land. Go in and talk to the man. They'd sooner wait than foreclose.", "Half this town's a payment behind. You're in company." } },
            { "drink", new[] { "Three days dry is three days. Keep counting.", "Come fishing Sunday. Can't drink with a line in your hand. Well, you can." } },
            { "cards", new[] { "Stay off the table one week and see how the money looks.", "The house always wins, {a}. Every man at that table knows it and sits down anyway." } },
            { "savings", new[] { "Somebody near you, then. Sorry to say it, but look near.", "You'll have your ticket. Slower, is all." } },
            { "tooth", new[] { "Clove oil, and the barber if it's not gone by Sunday. Don't let it fester.", "I had one like that. Hurts worse to leave it than to pull it." } },
            { "secret", new[] { "Whatever it is, it's yours. I'm not asking.", "Everybody's got one. Most aren't worth the keeping." } },
            { "law", new[] { "Keep your head down and your mouth shut. Lawmen leave, eventually.", "If it's you he wants, better here among friends than out on the road." } }
        };

        private static readonly Dictionary<string, string[]> StrangerReply = new Dictionary<string, string[]>
        {
            { "warm", new[] { "He did right by me once. I'll say that for him.", "Civil enough to me. Leave the man be.", "I've seen worse ride through. A good deal worse." } },
            { "afraid", new[] { "Keep clear of him. I've seen his kind, and I've seen what's left after.", "Don't look at him too long. That's my advice and it's free.", "He's not staying. They never stay. Pray on that." } },
            { "hostile", new[] { "If he comes near my place he'll get a welcome he won't like.", "Somebody ought to run him out. Somebody with more nerve than me.", "The law should see to him. The law sees to nothing." } },
            { "none", new[] { "Strangers come and strangers go. Don't lose sleep.", "I keep my eyes on my own business, {a}. You might try it.", "Could be anybody. Could be nobody. I've a wagon to load." } }
        };

        private static readonly Dictionary<string, string[]> PlanKind = new Dictionary<string, string[]>
        {
            { "geteven", new[] { "Let it lie, {a}. It never ends where you think it'll end.", "Whatever he did, a grave don't mend it." } },
            { "repay", new[] { "That's decent of you. Not many would think of it.", "Debts of that kind don't need paying. But it's decent." } },
            { "paydebt", new[] { "He'll wait. They always wait, for a price.", "Pay it in pieces and smile while you do. That's all a man can do." } },
            { "court", new[] { "Go on and ask, then. Worst she can do is laugh.", "Clean shirt, plain words. Plain works." } }
        };
        private static readonly string[] PlanCold = { "Your business.", "Hm. Let me know how it comes out." }, PlanSharp = { "Then do it or quit talking of it.", "You've said that before, {a}. Twice." };

        private static readonly string[] RumourKind = { "So I heard. If it's true it'll be in the paper. If it's not, it'll be in the saloon.", "That's the story. Half of what this town says is wind, mind.", "I heard it too. Makes you think." },
                                         RumourCold = { "Talk.", "I don't carry tales, {a}." },
                                         RumourSharp = { "Mind your own affairs, {a}. Talk like that gets a man shot.", "You'd do better to say less and listen more." };

        private static readonly Dictionary<string, string[]> NeedKind = new Dictionary<string, string[]>
        {
            { "hungry", new[] { "Come by at supper. There's always a plate.", "There's bread in my wagon. Take it, I mean it." } },
            { "lonely", new[] { "You know where I live. Door's open, evenings.", "Sunday, after church. We'll walk out a way." } },
            { "money", new[] { "I've two dollars I don't need till Friday. Say the word.", "Everybody's short. But come by Saturday and we'll see." } },
            { "rest", new[] { "Sit, then. The world'll keep an hour.", "Go home and lie down. Nobody'll die of your absence." } },
            { "safety", new[] { "Keep your gun close and your door barred. That's all any of us can do.", "If there's trouble, holler. I'll hear it." } }
        };
        private static readonly string[] NeedCold = { "Hard times all round.", "Everybody's short. Everybody's tired." }, NeedSharp = { "Try working. I find it helps.", "You're not the only one with troubles, {a}." };

        private static readonly string[] DayKind = { "That it is.", "Mud's the price of rain. I'll pay it.", "Good for the corn, bad for the boots.", "Be a long winter. They all are.", "Makes a change. Not much does." },
                                         DayCold = { "Mm-hm.", "If you say so.", "I hadn't noticed." },
                                         DaySharp = { "Did you stop me to talk about the weather?", "Weather's weather, {a}. I've work.", "Is that all? I've a wagon waiting." };

        private static readonly string[] CloseKind = { "Obliged, {b}.", "Knew you'd say so.", "You're a good sort, {b}.", "That's a comfort. Thank you." },
                                         CloseCold = { "Well. I'll let you be.", "Suit yourself.", "Mm. Good day, then." },
                                         CloseSharpHot = { "Didn't ask for your opinion.", "Forget I spoke.", "Same to you, and more of it." },
                                         CloseSharpMild = { "I'll say no more, then.", "I see how it is.", "No. No, you're right. Good day." };

        /// A talk between A (who brings it up) and B, from what's on A's mind: 2 or 3 beats. `topic` and `manner` for the log.
        public static Scene Make(Someone a, Someone b, string aName, string bName, MindTalkContext c, string seed, out string topic, out string manner)
        {
            topic = c.Topic ?? ChooseTopic(a, c, seed);
            manner = Exchanges.Manner(b, c.BAtWorst);
            string town = c.Town;
            Func<string, string> fill = line => line == null ? null : Times.Fill(line.Replace("{a}", aName).Replace("{b}", bName), town);
            string open = null, reply = null, openVoice = "chat", replyVoice = manner == "kind" ? "reply" : manner == "cold" ? "mild" : "replybad";
            string open2 = null, reply2 = null;   // a talk of the times: the second round (another word on it, another answer)
            string kind = Kind;
            TimesTopic times = null;
            AlmanacEvent news = null;
            switch (topic)
            {
                case "worry":
                {
                    string worry;
                    int stage;
                    bool good;
                    InnerLife.WorryNow(a.P, c.Day, out worry, out stage, out good);
                    open = InnerLife.WorrySaid(a.P, c.Day, seed + "ws");
                    if (open != null && Voice.Chance(seed + "wb", 40))
                        open = bName + ". " + open;
                    openVoice = "chatbad";
                    string[] pool;
                    reply = manner == "kind" && worry != null && KindToWorry.TryGetValue(worry, out pool) ? Voice.Pick(seed + "wk", pool) : Exchanges.TroubleReply(manner, seed + "wr");
                    replyVoice = manner == "kind" ? "sorry" : manner == "cold" ? "mild" : "tut";
                    kind = manner == "kind" ? "comfort" : manner == "cold" ? "brushoff" : "snap";
                    break;
                }
                case "times":
                    // the year's dated news first, two times in three while there is any (Core/Almanac.cs); the standing topics else
                    news = Almanac.ForTalk(c.Year, c.Month, c.DayOfMonth, town, seed);
                    if (news != null && Voice.Chance(seed + "an", Math.Min(100, 65 * Almanac.Share / 100)))
                    {
                        open = Voice.Pick(seed + "ao", news.Lines);
                        reply = Almanac.Reply(news, manner, seed);
                        open2 = Other(news.Lines, seed + "ao2", open);
                        string r2 = Almanac.Reply(news, manner, seed + "|2");
                        reply2 = r2 != reply ? r2 : null;
                        break;
                    }
                    news = null;
                    times = Times.Pick(c.Year, c.Month, seed);
                    if (times != null)
                    {
                        string stance = Stance(times, c.B, manner, seed);
                        open = Voice.Pick(seed + "to", times.Openers);
                        reply = Times.Reply(times, stance, seed, town);
                        open2 = Other(times.Openers, seed + "to2", open);
                        string[] pool = stance == "for" ? times.For : stance == "against" ? times.Against : times.Shrug;
                        reply2 = Other(pool, seed + "tr2", Voice.Pick(seed + "reply" + stance, pool));
                    }
                    break;
                case "stranger":
                    open = c.AMemory;
                    reply = Voice.Pick(seed + "sr", StrangerReply[c.B != null && c.B.StrangerRegard != null && StrangerReply.ContainsKey(c.B.StrangerRegard) ? c.B.StrangerRegard : "none"]);
                    break;
                case "plan":
                    open = c.APlan;
                    reply = manner == "kind" ? Voice.Pick(seed + "pk", PlanKind.ContainsKey(c.APlanKind ?? "") ? PlanKind[c.APlanKind] : PlanKind["repay"]) : Voice.Pick(seed + "pc", manner == "cold" ? PlanCold : PlanSharp);
                    break;
                case "rumour":
                {
                    string r = c.Rumours[Voice.Hash(seed + "ru") % c.Rumours.Count].Trim().TrimEnd('.');
                    open = Voice.Pick(seed + "ro", RumourOpeners).Replace("{r}", r);
                    reply = Voice.Pick(seed + "rr", manner == "kind" ? RumourKind : manner == "cold" ? RumourCold : RumourSharp);
                    break;
                }
                case "need":
                {
                    string need = c.A.Need;
                    open = Voice.Pick(seed + "no", NeedOpeners[need]);
                    openVoice = "chatbad";
                    reply = Voice.Pick(seed + "nr", manner == "kind" ? NeedKind[need] : manner == "cold" ? NeedCold : NeedSharp);
                    replyVoice = manner == "kind" ? "sorry" : replyVoice;
                    if (manner == "kind" && (need == "hungry" || need == "money"))
                        kind = "charity";   // a plate or a dollar offered: a kindness between them (Ties.Effect, the appraisal: the asking is A's, the answer B's). Not "help": that kind's note waits for a fall's get-up cue, which a talk never plays (the review)
                    break;
                }
                default:
                    topic = "day";
                    open = Voice.Pick(seed + "do", DayOpeners);
                    reply = Voice.Pick(seed + "dr", manner == "kind" ? DayKind : manner == "cold" ? DayCold : DaySharp);
                    break;
            }
            if (open == null || reply == null)
                return null;
            Scene s = new Scene { Kind = kind, Summary = aName + " and " + bName + ", talking of " + TopicWords(topic, times, news), Doer = kind == "brushoff" ? 1 : -1 };
            s.Add(0, fill(open), openVoice, "face");
            s.Add(1, fill(reply), replyVoice, manner == "cold" && (topic == "worry" || topic == "need") ? "walkaway" : null);
            if (topic == "times" && open2 != null && reply2 != null && Voice.Chance(seed + "more", TimesMoreChance))
            {
                s.Add(0, fill(open2), "chat", null);   // the second round: more of the same news, and the other's word on it again
                s.Add(1, fill(reply2), replyVoice, null);
            }
            if (manner != "cold" || topic == "times")
            {
                if (Voice.Chance(seed + "close", manner == "sharp" ? 70 : 45))
                {
                    bool hot = a.Temper == "Hothead" || a.Temper == "Proud";
                    string close = manner == "kind" ? Voice.Pick(seed + "ck", CloseKind) : manner == "cold" ? Voice.Pick(seed + "cc", CloseCold) : Voice.Pick(seed + "cs", hot ? CloseSharpHot : CloseSharpMild);
                    s.Add(0, fill(close), manner == "kind" ? "relieved" : manner == "sharp" && hot ? "angry" : "bye", manner == "sharp" ? "walkaway" : null);
                }
            }
            return s;
        }

        /// What A brings up, weighed by what's on their mind: the worry first while it lasts, the stranger when they remember
        /// him, the times for the curious, a plan, the town's talk, a need, the day as the last resort.
        public static string ChooseTopic(Someone a, MindTalkContext c, string seed)
        {
            var w = new List<KeyValuePair<string, int>>();
            string worry;
            int stage;
            bool good;
            if (a.P != null && InnerLife.WorryNow(a.P, c.Day, out worry, out stage, out good) && stage <= 1 && InnerLife.WorrySaid(a.P, c.Day, seed + "ws") != null)
                w.Add(new KeyValuePair<string, int>("worry", 40 + 8 * stage));
            if (Times.Of(c.Year, c.Month).Count > 0 || Almanac.Known(c.Year, c.Month, c.DayOfMonth).Count > 0)
                w.Add(new KeyValuePair<string, int>("times", 24 + (c.A != null && c.A.Curious ? 12 : 0) + Almanac.TalkBonus(c.Year, c.Month, c.DayOfMonth)));   // the dated news of the fortnight adds to it (Core/Almanac.cs)
            if (!string.IsNullOrEmpty(c.AMemory))
                w.Add(new KeyValuePair<string, int>("stranger", 30));
            if (!string.IsNullOrEmpty(c.APlan))
                w.Add(new KeyValuePair<string, int>("plan", 24));
            if (c.Rumours != null && c.Rumours.Count > 0)
                w.Add(new KeyValuePair<string, int>("rumour", 18));
            if (c.A != null && c.A.Need != null && NeedOpeners.ContainsKey(c.A.Need))
                w.Add(new KeyValuePair<string, int>("need", 16));
            w.Add(new KeyValuePair<string, int>("day", 12));
            int total = w.Sum(x => x.Value), roll = Voice.Hash(seed + "topic") % Math.Max(1, total);
            foreach (KeyValuePair<string, int> kv in w)
            {
                roll -= kv.Value;
                if (roll < 0)
                    return kv.Key;
            }
            return "day";
        }

        /// B's side of a question of the times: for it, against it, or a shrug. Nobody speaks against their own people.
        public static string Stance(TimesTopic t, MindTalkMind b, string manner, string seed)
        {
            if (b == null)
                return Voice.Pick(seed + "st0", "for", "against", "shrug");
            string o = b.Origin ?? "";
            if (t.Key == "georgia" && o == "Black")
                return "for";
            if ((t.Key == "natives" || t.Key == "natives07") && o == "Native")
                return "for";
            if (t.Key == "immigrants" && (o == "Italian" || o == "German" || o == "Irish" || o == "Scandinavian" || o == "French" || o == "Chinese"))
                return "for";
            int score = 0;
            switch (t.Lean)
            {
                case "politics":
                    score = (b.Lawful ? 1 : 0) + (b.WellOff ? 1 : 0) - (b.Reckless ? 1 : 0) - (b.Hard && !b.Lawful ? 1 : 0);
                    break;
                case "people":
                    score = (b.Compassionate ? 1 : 0) + (b.Devout ? 1 : 0) - (b.Hard ? 1 : 0) - (manner == "sharp" ? 1 : 0);
                    break;
                default:
                    score = (b.Curious ? 1 : 0) - (b.Orderly ? 1 : 0);
                    break;
            }
            if (score > 0)
                return "for";
            if (score < 0)
                return "against";
            int r = Voice.Hash(seed + "st") % 100;
            return r < 40 ? "shrug" : r < 70 ? "for" : "against";
        }

        public static string TopicWords(string topic, TimesTopic times, AlmanacEvent news = null)
        {
            switch (topic)
            {
                case "worry": return "a trouble";
                case "times": return news != null ? Almanac.Words(news) : "the times" + (times != null ? " (" + times.Key + ")" : "");
                case "stranger": return "the stranger";
                case "plan": return "what they mean to do";
                case "rumour": return "the town's talk";
                case "need": return "what they're short of";
                default: return "the day";
            }
        }

        /// Every written line here (not the worries', the times' or the thoughts'), for the tests.
        public static IEnumerable<string> AllLines()
        {
            return DayOpeners.Concat(NeedOpeners.Values.SelectMany(x => x)).Concat(RumourOpeners).Concat(KindToWorry.Values.SelectMany(x => x)).Concat(StrangerReply.Values.SelectMany(x => x))
                .Concat(PlanKind.Values.SelectMany(x => x)).Concat(PlanCold).Concat(PlanSharp).Concat(RumourKind).Concat(RumourCold).Concat(RumourSharp)
                .Concat(NeedKind.Values.SelectMany(x => x)).Concat(NeedCold).Concat(NeedSharp).Concat(DayKind).Concat(DayCold).Concat(DaySharp)
                .Concat(CloseKind).Concat(CloseCold).Concat(CloseSharpHot).Concat(CloseSharpMild);
        }

        /// The worry kinds the kind answers cover, for the tests (every kind Exchanges knows should be here).
        public static IEnumerable<string> WorryKinds()
        {
            return KindToWorry.Keys;
        }
    }
}
