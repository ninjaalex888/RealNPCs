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
            "Fine morning, {b}. Makes a body almost glad to be up.",
            "Price of flour's gone up again, {b}. Five cents on the sack.",
            "Livery's got a new colt. All knees and nerve, that one.",
            "Somebody's dog howled half of last night. Not mine, I'll swear to it.",
            "Clouds piling up over the hills, {b}. Somebody's going to get wet.",
            "Blacksmith's been hammering all day. I can feel it in my teeth.",
            "Roads are busy this week. Half the county's going somewhere.",
            "Somebody's rooster crows at all hours. Doesn't know dawn from midnight.",
            "Store's out of coffee till the wagon comes. I'm drinking chicory like a pauper.",
            "River's up, {b}. Saw a whole tree go past, roots and all.",
            "Moon was bright enough to read by last night, {b}."
        };

        private static readonly Dictionary<string, string[]> NeedOpeners = new Dictionary<string, string[]>
        {
            { "hungry", new[] { "Haven't eaten since yesterday noon. Store won't give credit and the garden's dust.", "You smell dinner anywhere, {b}? I'd follow it like a dog.",
                "Had coffee and a heel of bread at dawn. That's the whole of it today, {b}.",
                "My belt's on the last hole and it's still loose.",
                "I'd sell my hat for a plate of beans, {b}. I'd sell it twice." } },
            { "lonely", new[] { "Days go by and I don't say ten words to a soul. Good to see a face, {b}.", "The house is quiet enough to hear the clock think. I came out for the noise.",
                "Talked to the cat all through supper last night. The cat had nothing to add.",
                "Nobody's knocked at my door since the preacher, and he only came for the collection.",
                "Stop a minute, {b}. Just a minute. I've had nobody to talk to all week." } },
            { "money", new[] { "Short again this week. Short every week, if I'm honest.", "Paid the store and the rent and there's forty cents between me and Sunday.",
                "Sold my good coat for two dollars. It was worth five.",
                "The store's got my name in its book, and the page is near full.",
                "Counted what I've got three times over. It doesn't grow for counting." } },
            { "rest", new[] { "I could sleep standing up. Been on my feet since dark.", "Didn't close my eyes last night. Dogs, then the wind, then the thinking.",
                "Worked a double day and walked home half asleep.",
                "My eyes feel like they've been rolled in sand.",
                "Baby next door cried the whole night. I know every note of it now." } },
            { "safety", new[] { "I don't like the look of some of the men riding through lately.", "Keep your door barred nights, {b}. There's something about this week I don't trust.",
                "Somebody tried my back door last night. Found it barred, thank God.",
                "There's a rider been watching the road from the ridge two days now, {b}. Just watching.",
                "I've started sleeping with the shotgun by the bed. Never did that before." } }
        };

        private static readonly string[] RumourOpeners = { "Heard tell {r}. Make of it what you will.", "They're saying {r}. Whole town's saying it.", "You hear that {r}? I had it from the store.",
            "Word at the barber's is that {r}. Can't vouch for it.",
            "Somebody told me {r}. I only repeat it. I don't swear to it.",
            "My cousin swears {r}. My cousin swears to a lot of things.",
            "The preacher's wife told me {r}. She's not one for idle talk." };

        // ---- how the other answers -------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> KindToWorry = new Dictionary<string, string[]>
        {
            { "debt", new[] { "Pay what you can and keep out of his road. Men like that forget slower than they're paid.", "If it's a dollar or two, say so. I've been where you stand.",
                "Go to him before he comes to you. A man takes it kinder when you knock first.",
                "Pay him a little every week, even fifty cents. It shows good faith.",
                "Eleven dollars isn't a hanging matter. We'll find it between us if we have to." } },
            { "fever", new[] { "Keep the window shut and the broth coming. It breaks, mostly. It did for mine.", "I'll send the boy over with eggs. Don't argue.",
                "Willow bark tea and cold cloths. My grandmother swore by both.",
                "Let me sit up with the little one tonight. You sleep. You'll be no use otherwise.",
                "Children burn hot and mend fast. I've seen it a dozen times." } },
            { "letter", new[] { "It'll come. The mails are slow, not the people.", "No news is no news, {a}. Don't make it worse than it is.",
                "Write again, {a}, and send it by the other stage line. Letters go astray.",
                "The postmaster at the other end may be a drunk. That's all it is, I'd bet.",
                "I'll ask the stage driver to watch for it, {a}. He knows everybody's mail." } },
            { "drought", new[] { "Sky owes us. It'll pay, it always pays. Late, like everybody.", "There's water at my place yet. Bring the stock by if it comes to that.",
                "Plant the low field next year. It holds water when the rest won't.",
                "My uncle witched a well once with a peach fork. Want me to ask him over?",
                "Every dry year ends. I've seen four of them end, and I'll see this one end." } },
            { "son", new[] { "Boys go quiet when they're busy living. He'll write.", "If I hear a word of him on the road, you'll have it the same hour.",
                "He was raised right. Boys raised right come home again.",
                "Write to the company he works for. They keep the names of their men.",
                "Men out working don't write. Mine didn't for a year, and he came home fat." } },
            { "courting", new[] { "Then ask. Worst they can do is say no, and you've heard no before.", "Wear the clean collar and say it plain. Plain works.",
                "Bring flowers. Not store ones. Wild ones. It shows you walked for them.",
                "Ask after church. Everybody's in a forgiving mood after a sermon.",
                "If they look at you twice in one day, that's your answer. Ask." } },
            { "feud", new[] { "Let it lie, {a}. Nobody ever won one of those, they just outlived the other.", "Walk the other way when you see him. Costs nothing and keeps you out of a box.",
                "Get the surveyor out, {a}. Let a stranger with a chain settle the fence.",
                "Take him a pie, {a}. Hard to keep a grudge going with your mouth full.",
                "Ask the preacher to stand between you. Neither of you will cuss with him standing there." } },
            { "bank", new[] { "Banks want the money, not the land. Go in and talk to the man. They'd sooner wait than foreclose.", "Half this town's a payment behind. You're in company.",
                "Sell what you can spare before they sell what you can't.",
                "My brother-in-law clerks at the bank. I'll find out how hard they mean to push.",
                "Pay the interest if you can't pay the note. It buys you a season." } },
            { "drink", new[] { "Three days dry is three days. Keep counting.", "Come fishing Sunday. Can't drink with a line in your hand. Well, you can.",
                "When it's bad, come to my porch. I'll keep you talking till it passes.",
                "Drink coffee by the pot. If your hands are going to shake, let it be from that.",
                "My father quit with nothing but stubbornness. You've more of that than he had." } },
            { "cards", new[] { "Stay off the table one week and see how the money looks.", "The house always wins, {a}. Every man at that table knows it and sits down anyway.",
                "Give me your stake money to hold, {a}. You can have it back Sunday.",
                "Talk to your landlady before her nephew talks to you. She's softer than she looks.",
                "Every sharp in the county knows your face by now, {a}. They see you coming and smile." } },
            { "savings", new[] { "Somebody near you, then. Sorry to say it, but look near.", "You'll have your ticket. Slower, is all.",
                "Put it in the bank where nobody can get at it. Nobody but the bank, anyhow.",
                "Sew it into your coat lining. That's what my mother did, coming over.",
                "A little at a time still gets there. I'll put in a quarter myself." } },
            { "tooth", new[] { "Clove oil, and the barber if it's not gone by Sunday. Don't let it fester.", "I had one like that. Hurts worse to leave it than to pull it.",
                "Whiskey on a rag, held to it. Not swallowed. Held.",
                "There's a proper dentist in the city. Dear, but he won't break it like the barber.",
                "Salt water, warm as you can stand it. Rinse till your eyes water." } },
            { "secret", new[] { "Whatever it is, it's yours. I'm not asking.", "Everybody's got one. Most aren't worth the keeping.",
                "I won't pry. But whatever it is, you're carrying it in your face.",
                "Put right what you can and let the rest lie. That's all any of us does.",
                "Nobody in this town is what they were born. You're in good company." } },
            { "law", new[] { "Keep your head down and your mouth shut. Lawmen leave, eventually.", "If it's you he wants, better here among friends than out on the road.",
                "Change your hat and your habits. Lawmen look for both.",
                "There's a back door to my barn, and it isn't locked nights. That's all I'll say.",
                "Whatever it was, it was a long way back. I'll say so to anybody who asks." } }
        };

        private static readonly Dictionary<string, string[]> StrangerReply = new Dictionary<string, string[]>
        {
            { "warm", new[] { "He did right by me once. I'll say that for him.", "Civil enough to me. Leave the man be.", "I've seen worse ride through. A good deal worse.",
                "He held the door for me at the store. Small thing. Not many do it.",
                "Rough to look at. Gentle with horses. I judge a man by his horses.",
                "He paid what he owed at the store before he rode out. More than some here can say.",
                "Whatever he is, he's been square with me. That's all I go by." } },
            { "afraid", new[] { "Keep clear of him. I've seen his kind, and I've seen what's left after.", "Don't look at him too long. That's my advice and it's free.", "He's not staying. They never stay. Pray on that.",
                "Hush. Don't say it so loud. He might be at the next table.",
                "I bar the door when he's in town. I'm not ashamed to say it.",
                "I saw his eyes once. Close. I've no wish to again.",
                "Men like that don't come through by accident. Something brought him. I'd rather not learn what." } },
            { "hostile", new[] { "If he comes near my place he'll get a welcome he won't like.", "Somebody ought to run him out. Somebody with more nerve than me.", "The law should see to him. The law sees to nothing.",
                "I'd not give him water from my well if his horse was dying.",
                "Men like him are why decent folk bar their doors.",
                "I hope he rides on and keeps riding till the sea stops him.",
                "He's trouble in a good hat, that's all he is." } },
            { "none", new[] { "Strangers come and strangers go. Don't lose sleep.", "I keep my eyes on my own business, {a}. You might try it.", "Could be anybody. Could be nobody. I've a wagon to load.",
                "Haven't noticed him, {a}. I don't go looking at strangers.",
                "One more face on the street. The street's full of them.",
                "Every month brings a new stranger, {a}, and every month you fret over him.",
                "If he's not buying or selling, he's none of my concern." } }
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
                        break;   // a story of the fortnight: one round (its lines each tell the news afresh: the review)
                    }
                    news = null;
                    times = Times.Pick(c.Year, c.Month, seed);
                    if (times != null)
                    {
                        string stance = Stance(times, c.B, manner, seed);
                        string[] pool = stance == "for" ? times.For : stance == "against" ? times.Against : times.Shrug;
                        open = Voice.Pick(seed + "to", times.Openers);
                        string said = Voice.Pick(seed + "reply" + stance, pool);   // picked once and kept: with SaidLately a second pick of the same seed gives another line (the review)
                        reply = Times.Fill(said, town);
                        if (times.More != null && times.More.Length > 0 && Voice.Chance(seed + "more", TimesMoreChance))
                        {
                            open2 = Voice.Pick(seed + "tm2", times.More);   // more on the same subject (the topic's own follow-ups)
                            string r2 = Other(pool, seed + "tr2", said);
                            reply2 = r2 != null && r2 != said ? Times.Fill(r2, town) : null;
                        }
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
            if (topic == "times" && open2 != null && reply2 != null)   // the chance rolled where the round was picked
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
