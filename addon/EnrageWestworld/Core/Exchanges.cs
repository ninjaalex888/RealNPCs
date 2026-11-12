// Short exchanges between two people, written for the reason they have something to do with each other (Reasons.cs):
// someone at the worst of their trouble snaps when spoken to, someone whose trouble ended tells it (good or bad), a
// quarrel picks up again and is sometimes made up, sweethearts greet. Whoever's reason it is speaks to their own
// trouble, with its people named (the same names as their thoughts: InnerLife.WorryFill); the other one answers kindly,
// coldly or sharply, from their own temper and whether they're at the worst of their own trouble. How it went is the
// scene's kind, which is what it leaves between them (Ties.Effect). All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Exchanges
    {
        // ---- the one at the worst of it, spoken to ------------------------------------------------------

        private static readonly string[] Hello =
        {
            "Morning, {a}.", "{a}. Hot one today.", "You look done in, {a}.", "{a}! Haven't seen you about."
        };

        private static string[] _helloPool;
        private static string[] HelloPool { get { return _helloPool ?? (_helloPool = Hello.Concat(MoreHello).ToArray()); } }   // with Exchanges.More.cs's (item 6e)

        private static readonly Dictionary<string, string> Snap = new Dictionary<string, string>
        {
            { "debt", "Not now. I owe {who} more than I've got, and the day's coming." },
            { "fever", "Not now. {who}'s burning up, and the doctor wants two dollars to ride out." },
            { "letter", "Leave me be. Still nothing from {kin}. Not a word." },
            { "drought", "Don't say hot to me. Well's near dry and the sky won't give." },
            { "son", "Not today. No word from {who}. Not one, since {place}." },
            { "courting", "Leave off. I've a thing to ask somebody and no nerve to ask it." },
            { "feud", "If {who} walks by while you're talking, I won't answer for myself." },
            { "bank", "There's a bank notice nailed to my door. So no. It isn't a fine morning." },
            { "drink", "Third day dry. Talk to me in a week." },
            { "cards", "Unless you're lending money, keep walking. Rent's due." },
            { "savings", "Somebody's been at my savings. And no, I won't say who I suspect." },
            { "tooth", "Can't talk. Tooth. Like a nail driven in my jaw." },
            { "secret", "Why? Who's been asking about me?" },
            { "law", "That rider at the livery. Who's he been talking to? You see him?" }
        };

        // ---- news, told -----------------------------------------------------------------------------------

        private static readonly Dictionary<string, string> GoodNews = new Dictionary<string, string>
        {
            { "debt", "Paid {who} off this morning. Every cent. I could dance." },
            { "fever", "Fever broke! {who} sat up and asked for eggs." },
            { "letter", "Letter came from {who}. Alive, well, and wordy as ever." },
            { "drought", "Hit sweet water at last. Six feet deeper, but I hit it." },
            { "son", "{who}'s home! Ate like three men and slept like a fourth." },
            { "courting", "{who} said yes. Don't tell anybody. Tell everybody." },
            { "feud", "Shook hands with {who}. Would you credit it?" },
            { "bank", "Bank gave me another year. A whole year." },
            { "drink", "Thirty days dry. Coffee's on me, if you'll have coffee." },
            { "cards", "Rent's paid! Quit the table ahead, for once in my life." },
            { "savings", "Bought my ticket. {place}. I'm going." },
            { "tooth", "Barber had the tooth out. Look at that gap. Handsome, ain't it?" },
            { "secret", "Turns out nobody knew. Nobody. Slept like a baby." },
            { "law", "That Pinkerton's gone on the morning train. Good riddance." }
        };

        private static readonly Dictionary<string, string> BadNews = new Dictionary<string, string>
        {
            { "debt", "{who} took the mule. For the debt. Walked it right off my land." },
            { "fever", "We buried {who}. That's all there is to say about it." },
            { "letter", "Letter came. Black edge. {who} passed in the winter." },
            { "drought", "Sold the cow. Nothing else left to sell." },
            { "son", "{who} came home missing a leg. Smiles anyway. I can't." },
            { "courting", "{who}'s promised to somebody else. Heard it at the store." },
            { "feud", "Sheriff came about the fence. {who} lied to his face, and won." },
            { "bank", "Bank has the house. Strangers in my kitchen." },
            { "drink", "I'm back at it. Don't. I know." },
            { "cards", "Lost my room. Cards. Don't you say one word." },
            { "savings", "Ticket money's gone. Every cent of it." },
            { "tooth", "Barber broke it. Took him three goes. Look at my jaw." },
            { "secret", "They know. All of them. You do too, don't you?" },
            { "law", "My old name's on a poster by the church." }
        };

        // ---- the other one's answer -------------------------------------------------------------------------

        private static readonly string[] KindToTrouble = { "Lord. I'm sorry, {a}. If there's anything at all.", "I'll say a prayer for it. For whatever that's worth.", "I didn't know, {a}. There's coffee on my stove whenever you want it.", "No offence taken. You've a weight on you, anybody can see it.",
            "All right, {a}. I'll not keep you. But I'll look in on you tomorrow.",
            "Bite away, {a}. I've had days like yours. I'll still be here after.",
            "I'll leave you be, {a}. Send word if you want company. Or soup.",
            "That's all right, {a}. A hard week makes for hard words.",
            "Easy, {a}. Nobody's against you here. Least of all me.",
            "Go on, then. But if it gets heavier, you knock on my door. Any hour." };
        private static readonly string[] ColdToTrouble = { "Everybody's got troubles.", "Well. Can't help you there.", "None of my affair. Good day.", "Then I'll leave you to it.",
            "Suit yourself. I've my own to carry.",
            "Then I won't trouble you with good manners again.",
            "Hm. I'll ask somebody else how they're keeping, then.",
            "Can't see what it has to do with me.",
            "Well. I only stopped to be civil.",
            "So be it. There's work waiting on me." };
        private static readonly string[] SharpToTrouble = { "No call to bite my head off.", "Keep your troubles to yourself, then.", "I only said good morning. Choke on it, then.", "Try that tone on somebody who'll stand for it.",
            "Your trouble's no excuse to bark at me.",
            "Next time I'll cross the street and spare us both.",
            "I asked kindly. You'll not get kindly twice.",
            "We've all got troubles. Some of us manage manners as well.",
            "Bark at your dog, not at me.",
            "Then sit in it by yourself. See who stops next time." };
        private static readonly string[] SnapBack = { "Then don't ask.", "Off with you, then.", "Mind your own, {b}.", "Good. Then we're done talking.",
            "Suits me, {b}. Keep walking.",
            "Fine. Go and be civil at somebody else.",
            "Walk on, then, {b}. I never called you over.",
            "Go on, {b}. Your road's that way.",
            "Then go, {b}, and take your manners with you.",
            "Fine. I'll do my suffering in private." };

        private static readonly string[] KindToGood = { "That's the best thing I've heard all week, {a}.", "Ha! Good. Good for you. You've earned it.", "Well now! That calls for a drink, {a}, and I'm buying.", "I knew it'd come right, {a}. I said so at supper.",
            "Oh, {a}! I'm glad. I'm that glad I could shake your arm off.",
            "Now there's news worth stopping in the street for.",
            "Bless you, {a}. Wait till I tell them at home.",
            "About time something went your way, {a}. Long past time.",
            "I'm right glad for you. You've had a hard road to it.",
            "Good! There's justice after all, now and again." };
        private static readonly string[] ColdToGood = { "Huh. Lucky.", "Mm. Well. That's something.", "Good for you, I suppose.", "So long as you're pleased with it.",
            "Hm. Fancy that.",
            "That's your affair. Glad it suits you.",
            "Fine, then. I'll hear the rest some other time.",
            "Hm. Things turn, I suppose.",
            "Glad to hear it. Now, I've errands.",
            "Right. Good. Well, I'd best get on." };
        private static readonly string[] SharpToGood = { "Some folks get all the luck.", "Well. Ain't that grand.", "You'll be insufferable for a month, then.", "Spare me the dancing, {a}. Some of us had a plain day.",
            "And I suppose you'll be wanting me to cheer.",
            "Lucky for you. Shame it wasn't somebody who'd earned it.",
            "Mind the whole town doesn't hear you crowing, {a}.",
            "Good for you. Some of us are still waiting on ours.",
            "It won't last the month, {a}. Nothing good does.",
            "Must be pleasant, having things fall your way, {a}." };

        private static readonly string[] KindToBad = { "I'm so sorry, {a}. Truly.", "Come by tonight. You shouldn't sit alone with that.", "Oh, {a}. Here, sit down a minute. Sit.", "That's a hard blow. Anything you need, you've only to say it.",
            "Oh, {a}. I don't know what to say. I'm here, though.",
            "You'll take supper with us tonight, {a}. I won't hear no.",
            "I'll be by in the morning with what I can spare. And I'll stay a while.",
            "That's cruel hard, {a}. You didn't deserve that.",
            "Lean on me a minute, {a}. The street can look if it likes.",
            "I'll tell the preacher. You'll not carry this by yourself." };
        private static readonly string[] ColdToBad = { "That's hard. That's how it goes.", "Well. Life's like that.", "It happens to everybody sooner or later.", "Hm. A pity. Can't be helped.",
            "Bad luck. Nothing to be done about it now.",
            "Well. You'll manage. People do.",
            "Sorry to hear it. I'll not keep you.",
            "Hard. But you're not the first.",
            "Hm. Somebody's always got it worse.",
            "I've buried worse. You'll live." };
        private static readonly string[] SharpToBad = { "Told you how it'd go.", "Can't say I'm surprised.", "And what did you expect, {a}?", "You brought that on yourself, and you know it.",
            "I warned you, {a}. Nobody ever listens to a warning.",
            "Well, don't stand there looking at me like I did it.",
            "Should've thought of that sooner, {a}.",
            "And now you'll want pity. Go and ask the preacher for it.",
            "Half the town could have told you. Did you ask any of them?",
            "Comfort? From me, {a}? I've none spare for this." };

        // ---- a quarrel, again ---------------------------------------------------------------------------------

        private static readonly string[] QuarrelAgain = { "Still sore, {b}? I am.", "You've a nerve, standing there like nothing happened.", "Thought about what I said, {b}?", "Don't you cross the street on my account, {b}. Stand there and hear me.",
            "You. I've not forgotten, {b}. Not one word of it.",
            "Got anything to say to me, {b}? Anything at all?",
            "I've waited a week to see your face, {b}. Here it is.",
            "Still telling folk your side of it, {b}? I hear things.",
            "You owe me an apology, {b}, and I mean to stand here till I get it.",
            "Here's {b}. Come to say the rest of it?" };
        private static readonly string[] MakeUp = { "I was wrong, {a}. There. I said it.", "Let it go, {a}. It ain't worth what it's costing us.", "I've been sore a week over nothing, {a}. Let's be done with it.", "My hand's out, {a}. Take it or don't, but it's out.",
            "We were both fools, {a}. Me the bigger one, maybe.",
            "I miss our talks, {a}. I miss them more than I'm sore.",
            "This town's too small to keep a quarrel in, {a}. Shake?",
            "I said things I'd take back if I could. I'm taking them back, {a}.",
            "Come by for coffee Sunday, {a}. We'll talk about anything but this.",
            "I'll not let my temper cost me a friend, {a}. Not this time." };
        private static readonly string[] Accept = { "...Well. All right, then.", "Huh. All right. Buy me a drink and we'll call it square.", "...Fine. Fine. I was sick of it, too.", "Ah, give it here, {b}. Life's too short.",
            "...I've missed you, {b}. Don't tell anybody I said so.",
            "Well. It took you long enough. All right.",
            "You stubborn old mule. All right. Come here.",
            "I'll shake. I'd sooner be your friend than right.",
            "...That's handsome of you, {b}. I'll say the same.",
            "Done, then. I'll not raise it again if you won't." };
        private static readonly string[] Refuse = { "Wrong don't begin to cover it.", "Sorry don't mend a thing.", "Keep your hand, {b}. I know where it's been.", "A week late with that, and a dollar short.",
            "You'll not talk your way round me this time, {b}.",
            "You've always had words, {b}. I'll want more than words.",
            "You're sorry now, {b}. You weren't sorry then.",
            "I've heard you sorry before, {b}. It never lasted to Sunday.",
            "No. Some things stay said.",
            "I'm not in a forgiving way today, {b}. Ask me some other year." };
        private static readonly string[] Stand = { "Step aside, {a}, or I'll put you aside.", "One more word, {a}. Just the one.", "Say it again, {a}, and say it closer.", "You want this settled, {a}? Here's as good a place as any.",
            "You'll get no sorry out of me, {a}. Not one.",
            "Keep on at me, {a}. I'll not stand here and take it twice.",
            "I'm right here, {a}. I'm not hard to find.",
            "Raise your voice to me again, {a}. Go on.",
            "Mind how you talk to me in the street, {a}. I've a long memory.",
            "You've said your piece, {a}. Now I'll say mine with my fists." };
        private static readonly string[] Dare = { "Not here. Not today.", "Another time, {b}. Count on it.", "I'll pick the day, {b}. Not you.", "Walk on, {b}. I've not forgotten and I won't.",
            "Not with the whole street watching. But soon, {b}.",
            "You'll keep, {b}. I'm a patient sort.",
            "There'll be a better day for it, {b}. A quieter one.",
            "Enjoy your walk home, {b}. Look behind you now and then.",
            "Not worth the fine, {b}. Not today.",
            "The sheriff's watching, {b}. He won't always be." };

        // ---- sweethearts ----------------------------------------------------------------------------------------

        private static readonly string[] SweetHello = { "There you are, {b}.", "Afternoon, {b}. You look well.", "I was hoping I'd pass you.", "Morning, {b}. I saved you the first of the apples.",
            "Well. There's the best thing I'll see all day.",
            "I brought you something, {b}. Don't open it till you're home.",
            "You've done something with your hair, {b}. Don't change it back.",
            "Walk a little way with me, {b}? Only to the corner.",
            "I thought about you at breakfast, {b}. And after.",
            "I've been saving up things to tell you, {b}. Nothing important. All of it." };
        private static readonly string[] SweetBack = { "Were you, now?", "I took the long way round. Don't ask why.", "You'll make me blush in the street.", "Hush, {a}. Half the town's at the window.",
            "And here I'd thought you'd forgotten me, {a}.",
            "Mind, {a}. The storekeeper's wife sees everything.",
            "Hush now. Walk with me, then tell me again, softer.",
            "I put on my good boots in case I saw you.",
            "Go on with you, {a}. Say it again, though.",
            "I'd hoped it would be you coming up the street." };

        // ---- the worrier and the real person at the other end (Links.cs): by the worry's stage, steered by its end ----

        // a debt: B (the one owed) calls it in, A (the debtor) answers by stage; the same eleven dollars as their thoughts
        private static readonly string[] CallIn =
        {
            "No hurry, {a}, but I'll want that eleven dollars by and by.",
            "Eleven dollars, {a}. I've been patient.",
            "Saturday, {a}. Eleven dollars Saturday, or I take it out in mule."
        };
        private static readonly string[] Owe =
        {
            "You'll have it. Soon as I'm paid.",
            "I need more time. A week. Just a week.",
            "Saturday. I swear it on my mother's grave."
        };
        // the one owed answers, by stage, from their own manner
        /// A line for this stage of a debt from a pool kept in stage order (0, 1, 2, 0, 1, 2...; Run 3, item 6 added two more of
        /// each), chosen by the seed among that stage's own.
        private static string StageLine(string[] pool, int st, string seed)
        {
            int n = System.Math.Max(1, (pool.Length - st + 2) / 3);
            return pool[System.Math.Min(pool.Length - 1, st + 3 * (Voice.Hash(seed + "|owed" + st) % n))];
        }

        private static readonly string[] OwedKind = { "No rush. Just don't forget it.", "...A week, then.", "Saturday it is. I'll come by the house for it.",
            "Whenever it suits. I know you're good for it.",
            "A week. I'll not ask before then.",
            "Saturday, then. Don't fret yourself sick over it.",
            "Pay me when you're able. I'll not hound you.",
            "Take the week. Take two if you need them.",
            "Saturday. And if it's short, bring what you have." };
        private static readonly string[] OwedCold = { "Mind you don't forget it.", "One week. I'll be counting.", "I'll hold you to it.",
            "I'll note it down. Soon, you said.",
            "A week. Not a day more.",
            "Saturday. I'll be on your step at noon.",
            "Mind it is soon.",
            "One week. After that I come asking.",
            "Then Saturday it is. Have it counted." };
        private static readonly string[] OwedSharp = { "Soon ain't a day of the week.", "You said that last week.", "Don't swear. Pay.",
            "You've been about to be paid since I've known you.",
            "A week. And where's the money to come from in a week?",
            "Leave your mother out of it. Bring the money.",
            "Soon. Everybody's paid soon. Nobody's paid now.",
            "You've had your week, and the one before it.",
            "Swear on whatever you like. Saturday, or the mule." };
        private static readonly string[] Paid = { "Here. Eleven dollars, {b}. Count it.", "...Square, then. Square and glad of it." };
        // paid in the street (Tier 4): counted out where folks can see, the one who paid asks it said aloud, and it is
        private static readonly string[] PaidCount = { "...nine, ten, eleven. Eleven it is.", "Eight... nine... ten... eleven. Well, I'll be.", "Ten... and eleven. Every cent, and no bent ones.", "...ten, eleven. Eleven dollars, counted in the street." };
        private static readonly string[] PaidAsk = { "Say it so they can hear, {b}.", "Out loud, {b}. Let the street hear it.", "Now say it, {b}. Say we're square.", "Tell them, {b}. I want it heard." };
        private static readonly string[] PaidWitness = { "You all heard it. {a} paid me square, every cent.", "Mark it, folks. {a} owes me nothing. Not one cent.", "{a} and me are square. Anybody says different, send them to me.", "Paid in full, and in the street. {a}'s word is good." };
        private static readonly string[] PaidLast = { "Lighter than I've felt in a month.", "Never again. Not a dollar, not from anybody.", "There. Now I can look a man in the eye.", "Eleven dollars, and I'd have sworn it weighed a hundred." };
        private static readonly string[] Mule = { "Working my mule hard, {b}? Sleeping well?", "I didn't want it this way, {a}. You know I didn't.", "Owed is owed, {a}." };

        // a feud: A (the one worrying) at the neighbour, by stage; mended or not at the end
        private static readonly string[] Fence =
        {
            "{b}. That post's a foot onto my side again.", "It's where it's always been.",
            "Your dog was in my hens again, {b}.", "Prove it was my dog.",
            "Next time I see that post moved, I'm burning it.", "Burn it and see what follows."
        };
        private static readonly string[] Shake = { "I'll shake on it if you will, {b}. Fence and all.", "Aye. I will. It's been a long, stupid summer." };
        private static readonly string[] Done = { "There's no more talking between us, {b}.", "Suits me." };

        // cards: A (the gambler) asks B for a loan, by stage; B lends, refuses cold, or snaps (FIVEDAY item 32a)
        private static readonly string[] Borrow =
        {
            "{b}, a word. I'm a little short till Saturday. Ten dollars would see me right.",
            "{b}, I know I asked before. The cards turned on me. Five more and I'll win it back.",
            "{b}, they'll have the roof off my head. I need it tonight. I don't beg, and I'm begging."
        };
        private static readonly string[] Lend = { "Here. Saturday, mind.", "This is the last of it, {a}. I mean that.", "Take it. And stay away from that table." };
        private static readonly string[] LendCold = { "I don't lend at cards, {a}. Not to anyone.", "I'm no bank, {a}.", "You'll have to find it somewhere else." };
        private static readonly string[] LendSharp = { "Lend you money for faro? Go home, {a}.", "You owe half the street already.", "Beg somebody who doesn't know you." };
        private static readonly string[] Repaid = { "Every dollar, {b}, and my thanks. I'm done with the cards.", "Glad to see it, {a}. Gladder than the money." };
        private static readonly string[] Stiffed = { "Saturday came and went, {a}. Three of them.", "I'll have it. I just need one good night." };

        // courting: A courts B; B warms to it only if it will end well
        private static readonly string[] Court =
        {
            "Morning, {b}. Fine morning. Isn't it a fine morning?",
            "I could carry that for you, {b}. If you'd like.",
            "{b}, there's a thing I've been meaning to ask..."
        };
        private static readonly string[] CourtWarm = { "It is now.", "I'd like that.", "Then ask it, before you burst." };
        private static readonly string[] CourtCool = { "It's a morning.", "I can manage, thank you.", "Best not, {a}. Best not." };
        private static readonly string[] Yes = { "I'm asking, {b}. Will you walk out with me? Proper?", "Yes. Yes, you fool. Yes." };
        private static readonly string[] No = { "So it's no, then.", "It's no. I'm sorry, {a}. Truly." };

        /// Whether a reason's exchange is a linked one (the worrier and the real person at the other end: MakeLinked), read off
        /// Links.Linkable so a new linked kind can't be missed. The game's routing listed debt, feud and courting by hand and
        /// left out cards when FIVEDAY item 32a made it linked: the gambler's loan scene was chosen as a reason and then never
        /// played (Make has no "x:cards"; HARDEN item 3, a loose end since SEAMS). Behind [Scenes] GamblersBorrow, as the lender is.
        public static bool IsLinked(string kind)
        {
            return kind != null && kind.StartsWith("x:") && Links.Linkable(kind.Substring(2));
        }

        /// A meeting between a worrier (A) and the real person at the other end of their worry (B): "x:debt", "x:feud",
        /// "x:courting". The stage and how it will end come from the worrier's own run (the same as their thoughts).
        public static Scene MakeLinked(string kind, Someone a, Someone b, string aName, string bName, long day, string seed)
        {
            string worry = kind.Substring(2);
            long arc = InnerLife.ArcFor(a.Id, day);
            int stage = InnerLife.StageFor(a.Id, day);
            bool good = InnerLife.EndsWellFor(a.Id, worry, arc);
            System.Func<string, string> fill = line => line.Replace("{a}", aName).Replace("{b}", bName);
            Scene s = new Scene();
            int st = System.Math.Min(stage, 2);
            switch (worry)
            {
                case "debt":
                    if (stage < 3)
                    {
                        string manner = Manner(b, false);
                        s.Add(1, fill(CallIn[st]), st == 0 ? "familiar" : "angry", "face").Add(0, fill(Owe[st]), st == 0 ? "mild" : "plead");
                        string[] back = manner == "kind" ? OwedKind : manner == "cold" ? OwedCold : OwedSharp;
                        s.Add(1, fill(StageLine(back, st, seed)), manner == "sharp" ? "angry" : "mild", "walkaway");
                        s.Kind = "debt";
                        s.Doer = 1;   // B calls it in (the worrier A owes)
                    }
                    else if (good)
                    {
                        // paid where the street can see it: counted out, said aloud at the debtor's asking (a settled tie; word gets round)
                        s.Add(0, fill(Paid[0]), "relieved", "face").Add(1, fill(Voice.Pick(seed + "pc", PaidCount)), "mild")
                         .Add(0, fill(Voice.Pick(seed + "pa", PaidAsk)), "mild").Add(1, fill(Voice.Pick(seed + "pw", PaidWitness)), "cheer")
                         .Add(0, fill(Voice.Pick(seed + "pl", PaidLast)), "relieved");
                        s.Kind = "settled";
                    }
                    else
                    {
                        s.Add(0, fill(Mule[0]), "angry", "face").Add(1, fill(Manner(b, false) == "kind" ? Mule[1] : Mule[2]), "mild", "walkaway");
                        s.Kind = "argue";
                    }
                    break;
                case "feud":
                    if (stage < 3)
                    {
                        s.Add(0, fill(Fence[st * 2]), st == 2 ? "threat" : "angry", "face").Add(1, fill(Fence[st * 2 + 1]), st == 2 ? "threat" : "tut", st == 2 ? "walkaway" : null);
                        s.Kind = "argue";
                    }
                    else
                    {
                        string[] end = good ? Shake : Done;
                        s.Add(0, fill(end[0]), good ? "defuse" : "angry", "face").Add(1, fill(end[1]), good ? "relieved" : "mild", good ? null : "walkaway");
                        s.Kind = good ? "makeup" : "argue";
                    }
                    break;
                case "courting":
                    if (stage < 3)
                    {
                        s.Add(0, fill(Court[st]), "flatter", "face").Add(1, fill((good ? CourtWarm : CourtCool)[st]), good ? "cheer" : "mild", good ? null : "walkaway");
                        s.Kind = good ? "sweet" : "brushoff";
                        s.Doer = good ? -1 : 1;   // B cools
                    }
                    else
                    {
                        string[] end = good ? Yes : No;
                        s.Add(0, fill(end[0]), good ? "flatter" : "shame", "face").Add(1, fill(end[1]), good ? "cheer" : "sorry");
                        s.Kind = good ? "propose" : "brushoff";
                        s.Doer = good ? -1 : 1;   // B says no
                    }
                    break;
                case "cards":   // the gambler asks his lender (FIVEDAY item 32a): lent, refused cold, or snapped at; then paid back or not
                    if (stage < 3)
                    {
                        string manner = Manner(b, false);
                        s.Add(0, fill(Borrow[st]), st == 2 ? "plead" : "mild", "face");
                        if (manner == "kind")
                        {
                            s.Add(1, fill(Lend[st]), "mild");
                            s.Kind = "help";
                            s.MindKind = "cardsloan";   // remembered as the loan it was, not a fall helped up from (SEAMS review 2)
                            s.Doer = 1;   // B helps A out
                        }
                        else
                        {
                            s.Add(1, fill((manner == "cold" ? LendCold : LendSharp)[st]), manner == "cold" ? "mild" : "angry", "walkaway");
                            s.Kind = manner == "cold" ? "brushoff" : "snap";
                            if (manner == "cold")
                                s.Doer = 1;   // B turns A down: the lender's brush-off, not the gambler's (the review of HARDEN item 3; as courting's)
                        }
                    }
                    else if (good)
                    {
                        s.Add(0, fill(Repaid[0]), "relieved", "face").Add(1, fill(Repaid[1]), "cheer");
                        s.Kind = "settled";
                        s.Doer = 0;   // A paid B what was owed
                    }
                    else
                    {
                        s.Add(1, fill(Stiffed[0]), "angry", "face").Add(0, fill(Stiffed[1]), "plead", "walkaway");
                        s.Kind = "debt";
                        s.Doer = 1;   // B calls it in
                    }
                    break;
                default:
                    return null;
            }
            s.Summary = aName + " and " + bName + ": " + worry;
            return s;
        }

        /// How the other one answers: kind, cold or sharp, from their temper and whether they're at the worst of
        /// their own trouble. The same person answers the same way (it's who they are).
        public static string Manner(Someone b, bool bAtWorst)
        {
            if (bAtWorst || b.Temper == "Hothead" || b.Temper == "Proud")
                return "sharp";
            if (b.Temper == "Cold" || b.Temper == "Greedy" || b.Temper == "Drifter")
                return "cold";
            if (b.Temper == "Loyal" || b.Temper == "Coward")
                return "kind";
            return Voice.Chance(b.Id + "|manner", 55) ? "kind" : "cold";
        }

        /// The answer to someone's trouble in this manner (kind, cold, sharp), for a talk built elsewhere (MindTalk).
        public static string TroubleReply(string manner, string seed)
        {
            return Voice.Pick(seed + "tr", manner == "kind" ? KindToTrouble : manner == "cold" ? ColdToTrouble : SharpToTrouble);
        }

        /// The exchange for this reason ("x:snap", "x:goodnews", "x:badnews", "x:quarrel", "x:sweet"): A is the one whose
        /// reason it is, B the other. aName and bName are what they go by in the lines. Null if it can't be made.
        public static Scene Make(string kind, Someone a, Someone b, string aName, string bName, long day, bool bAtWorst, SceneTie tie, string seed)
        {
            string worry;
            int stage;
            bool good;
            InnerLife.WorryNow(a.P, day, out worry, out stage, out good);
            string manner = Manner(b, bAtWorst);
            System.Func<string, string> fill = line => InnerLife.WorryFill(a.P, day, line).Replace("{a}", aName).Replace("{b}", bName);
            Scene s = new Scene();
            switch (kind)
            {
                case "x:snap":
                    // their worry's own words at its worst; else (a shame, a pile of troubles, a worry settled) a plain snap (the review)
                    string snapLine = worry != null && stage == 2 && Snap.ContainsKey(worry) ? WorryLine(Snap, MoreSnap, worry, seed + "sn")
                                    : Voice.Pick(seed + "ps", "Not now. It's one thing on top of another.", "Leave me be. I've had a day of it.", "Can't a body get a minute's peace?");
                    if (!WeatherTruth.Fits(snapLine))
                        snapLine = "Not now. The well's low and I've no patience for it.";   // "Don't say hot to me" wants a hot day (Core/WeatherTruth.cs)
                    s.Add(1, fill(Voice.Pick(seed + "h", HelloPool)), "familiar", "face").Add(0, fill(snapLine), "replybad");
                    Answer(s, manner, fill, seed, KindToTrouble, ColdToTrouble, SharpToTrouble);
                    if (manner == "sharp")
                        s.Add(0, fill(Voice.Pick(seed + "sb", SnapBack)), "angry", "walkaway");
                    s.Kind = manner == "kind" ? "comfort" : manner == "cold" ? "brushoff" : "snap";
                    s.Doer = manner == "cold" ? 1 : -1;   // B brushes A off
                    s.Summary = aName + " snapped at " + bName;
                    return s;
                case "x:goodnews":
                case "x:badnews":
                    bool glad = kind == "x:goodnews";
                    Dictionary<string, string> news = glad ? GoodNews : BadNews;
                    if (worry == null || !news.ContainsKey(worry))
                        return null;
                    string newsLine = glad && worry == "tooth" && a.P != null && InnerLife.IsSettled(a.P.Id, day) ? "A real dentist pulled it, in Saint Denis. Look at that gap. Handsome, ain't it?" : WorryLine(news, glad ? MoreGood : MoreBad, worry, seed + "nw");   // he paid for no barber
                    s.Add(0, fill(newsLine), glad ? "cheer" : "chatbad", "face");
                    if (glad)
                        Answer(s, manner, fill, seed, KindToGood, ColdToGood, SharpToGood);
                    else
                        Answer(s, manner, fill, seed, KindToBad, ColdToBad, SharpToBad);
                    s.Kind = manner == "kind" ? (glad ? "cheer" : "comfort") : manner == "cold" ? "brushoff" : (glad ? "envy" : "snap");
                    s.Doer = manner == "cold" ? 1 : -1;   // B brushes A off
                    s.Summary = aName + " told " + bName + " the news";
                    return s;
                case "x:quarrel":
                    s.Add(0, fill(Voice.Pick(seed + "q", QuarrelAgain)), "angry", "face");
                    bool sorry = manner == "kind" || (manner == "cold" && tie != null && tie.Stage >= 2 && Voice.Chance(seed + "mk", 50));
                    if (sorry)
                    {
                        s.Add(1, fill(Voice.Pick(seed + "m", MakeUp)), "defuse");
                        bool takes = a.Temper != "Hothead" && a.Temper != "Proud" || Voice.Chance(seed + "ac", 30);
                        s.Add(0, fill(Voice.Pick(seed + "a", takes ? Accept : Refuse)), takes ? "relieved" : "angry", takes ? null : "walkaway");
                        s.Kind = takes ? "makeup" : "argue";
                    }
                    else
                    {
                        s.Add(1, fill(Voice.Pick(seed + "st", Stand)), "threat").Add(0, fill(Voice.Pick(seed + "d", Dare)), "angry", "walkaway");
                        s.Kind = "argue";
                    }
                    s.Summary = aName + " and " + bName + " at it again";
                    return s;
                case "x:sweet":
                    s.Add(0, fill(Voice.Pick(seed + "sw", SweetHello)), "flatter", "face").Add(1, fill(Voice.Pick(seed + "sbk", SweetBack)), "cheer");
                    s.Kind = "sweet";
                    s.Summary = aName + " and " + bName + ", sweet on each other";
                    return s;
            }
            return null;
        }

        private static void Answer(Scene s, string manner, System.Func<string, string> fill, string seed, string[] kind, string[] cold, string[] sharp)
        {
            string[] pool = manner == "kind" ? kind : manner == "cold" ? cold : sharp;
            s.Add(1, fill(Voice.Pick(seed + "ans", pool)), manner == "kind" ? "sorry" : manner == "cold" ? "mild" : "tut", manner == "cold" ? "walkaway" : null);
        }

        /// Every line, with the worry slots as they come, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return MoreLines().Concat(Hello).Concat(Snap.Values).Concat(GoodNews.Values).Concat(BadNews.Values).Concat(KindToTrouble).Concat(ColdToTrouble).Concat(SharpToTrouble)
                .Concat(SnapBack).Concat(KindToGood).Concat(ColdToGood).Concat(SharpToGood).Concat(KindToBad).Concat(ColdToBad).Concat(SharpToBad)
                .Concat(QuarrelAgain).Concat(MakeUp).Concat(Accept).Concat(Refuse).Concat(Stand).Concat(Dare).Concat(SweetHello).Concat(SweetBack)
                .Concat(CallIn).Concat(Owe).Concat(OwedKind).Concat(OwedCold).Concat(OwedSharp).Concat(Paid).Concat(PaidCount).Concat(PaidAsk).Concat(PaidWitness).Concat(PaidLast).Concat(Mule).Concat(Fence).Concat(Shake).Concat(Done).Concat(Borrow).Concat(Lend).Concat(LendCold).Concat(LendSharp).Concat(Repaid).Concat(Stiffed).Concat(Court).Concat(CourtWarm).Concat(CourtCool).Concat(Yes).Concat(No);
        }

        /// The worry kinds every reason needs lines for, for the tests.
        public static IEnumerable<string> WorryKinds()
        {
            return Snap.Keys;
        }
    }
}
