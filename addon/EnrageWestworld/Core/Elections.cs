// Election day (SIXDAY item 10b; roadmap 10; roadmap.md P22), an event on the town-events framework (Core/TownEvents.cs).
// Election Day, the first Tuesday after the first Monday of November (7 November 1899; 5 November 1907), in every town
// with five living regulars: two grown men of the town stand for mayor or town marshal (never the game's own sheriff or a
// story character), the proud and the loyal first, those with ties to the town before those without. Posters a week ahead:
// a notice, the paper, a clipping in the reader. The voters are the town's regulars, each leaning by temper and by their
// ties to the two men, and by the player's word: an endorsement from a man the town counts a peacemaker helps, from a
// known killer it hurts; whiskey bought for the voters helps and sours the town; a candidate leaned on loses heart. Away,
// the tally comes in the daily tick and the paper carries it; there, a porch speech with a listening crowd, hecklers,
// an old man grumbling about the new secret ballot, a party man pressing tickets, and supporters at words. After, ties
// warm on the winner's side and sour toward him on the loser's. Why it reads true: a small town's election is a contest
// of standing, not platforms, and everyone remembers who stood where. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Elections
    {
        public const int LeastRegulars = 5, PostDaysAhead = 7;
        public const int PollsFrom = 10, PollsTo = 16;         // the hours the speeches and the polls are on
        public const int EndorseSwing = 12, WhiskeySwing = 18, LeanSwing = 20;   // percent of the undecided moved
        public const int WhiskeyCents = 500;
        public const int WhiskeyAffection = -4;                // the town's regulars, for votes bought
        public static readonly string[] Offices = { "mayor", "town marshal" };

        public static string Office(string town)
        {
            return Offices[Voice.Hash(town ?? "") % Offices.Length];
        }

        /// Election Day's day of the month in November: the first Tuesday after the first Monday.
        public static int Day(int year)
        {
            return Calendar.ElectionDay(year);
        }

        /// The election on that day: the polls from ten till four.
        public static TownEvent Make(string town, long electionGameDay, Resident a, Resident b)
        {
            return new TownEvent
            {
                Id = Ids.New(), Kind = "election", Town = town, Title = "Election day", At = electionGameDay * 1440 + PollsFrom * 60, Until = electionGameDay * 1440 + PollsTo * 60,
                AId = a.Id, AName = a.Name, BId = b.Id, BName = b.Name, Extra = Office(town)
            };
        }

        /// The two who stand: grown men of the town, the proud and the loyal first, then those with ties; null when the
        /// town hasn't two such men.
        public static Resident[] Candidates(List<Resident> regulars, List<SceneTie> ties, int nowYear, string seed)
        {
            if (regulars == null)
                return null;
            var ranked = new List<KeyValuePair<int, Resident>>();
            foreach (Resident r in regulars)
            {
                if (r == null || r.Dead || r.Fled || r.Woman || r.Id == null || r.Name == null)
                    continue;
                int age = r.BornYear > 0 ? nowYear - r.BornYear : 40;
                if (age < 25 || age > 70)
                    continue;
                int score = (r.Personality == "Proud" || r.Personality == "Loyal" ? 20 : r.Personality == "Drifter" || r.Personality == "Coward" ? -10 : 0) + TiesOf(ties, r.Id) * 5 + Voice.Hash(seed + "|" + r.Id) % 7;
                ranked.Add(new KeyValuePair<int, Resident>(score, r));
            }
            if (ranked.Count < 2)
                return null;
            ranked.Sort((x, y) => y.Key.CompareTo(x.Key));
            return new[] { ranked[0].Value, ranked[1].Value };
        }

        private static int TiesOf(List<SceneTie> ties, string id)
        {
            int n = 0;
            if (ties != null)
                foreach (SceneTie t in ties)
                    if (t != null && (t.A == id || t.B == id))
                        n++;
            return n;
        }

        private static int Warmth(List<SceneTie> ties, string id, string other)
        {
            if (ties != null)
                foreach (SceneTie t in ties)
                    if (t != null && ((t.A == id && t.B == other) || (t.A == other && t.B == id)))
                        return t.Warmth;
            return 0;
        }

        /// One voter's lean: 1 for A, -1 for B, 0 undecided. By their ties to the two (a warm tie wins, a hostile one sends
        /// them to the other), then by temper (the proud and the hot-headed with a Proud man, the loyal and the timid with a
        /// Loyal one, the greedy with the better-tied), the drifting undecided.
        public static int Lean(Resident voter, Resident a, Resident b, List<SceneTie> ties, string seed)
        {
            if (voter == null || a == null || b == null)
                return 0;
            if (voter.Id == a.Id) return 1;
            if (voter.Id == b.Id) return -1;
            int wa = Warmth(ties, voter.Id, a.Id), wb = Warmth(ties, voter.Id, b.Id);
            if (wa != wb)
                return wa > wb ? 1 : -1;
            string t = voter.Personality ?? "";
            bool aProud = a.Personality == "Proud" || a.Personality == "Hothead", bProud = b.Personality == "Proud" || b.Personality == "Hothead";
            bool aLoyal = a.Personality == "Loyal", bLoyal = b.Personality == "Loyal";
            if ((t == "Proud" || t == "Hothead") && aProud != bProud) return aProud ? 1 : -1;
            if ((t == "Loyal" || t == "Coward") && aLoyal != bLoyal) return aLoyal ? 1 : -1;
            if (t == "Greedy")
            {
                int ta = TiesOf(ties, a.Id), tb = TiesOf(ties, b.Id);
                if (ta != tb) return ta > tb ? 1 : -1;
            }
            if (t == "Drifter")
                return 0;
            int r = Voice.Hash(seed + "|lean|" + voter.Id) % 100;
            return r < 40 ? 1 : r < 80 ? -1 : 0;
        }

        /// The player's standing moves what his word is worth: a peacemaker's endorsement counts, a killer's counts against.
        public static int Swing(string standing)
        {
            return standing == "peacemaker" ? EndorseSwing : standing == "killer" ? -EndorseSwing : EndorseSwing / 2;
        }

        /// The tally: every living regular votes (the undecided by the seed), the player's doings moving a share of the
        /// undecided and the whiskey's drinkers; a tie goes to the better-tied man, then the seed. Returns A's and B's votes.
        public static void Tally(List<Resident> regulars, Resident a, Resident b, List<SceneTie> ties, string endorsed, string standing, string whiskeyFor, string leanedOn, string seed, out int votesA, out int votesB)
        {
            votesA = votesB = 0;
            if (regulars == null || a == null || b == null)
                return;
            foreach (Resident v in regulars)
            {
                if (v == null || v.Dead || v.Fled)
                    continue;
                int lean = Lean(v, a, b, ties, seed);
                int r = Voice.Hash(seed + "|vote|" + v.Id) % 100;
                if (lean == 0)
                {
                    // the undecided: the player's word, the whiskey, the man leaned on, then a coin
                    int toA = 50;
                    if (endorsed != null) toA += endorsed == a.Id ? Swing(standing) : -Swing(standing);
                    if (whiskeyFor != null) toA += whiskeyFor == a.Id ? WhiskeySwing : -WhiskeySwing;
                    if (leanedOn != null) toA += leanedOn == a.Id ? -LeanSwing : LeanSwing;
                    lean = r < toA ? 1 : -1;
                }
                else if (whiskeyFor != null && (v.Personality == "Greedy" || v.Personality == "Drifter") && r < 50)
                    lean = whiskeyFor == a.Id ? 1 : -1;   // a free drink buys the thirsty
                if (lean > 0) votesA++; else votesB++;
            }
            if (votesA == votesB)
            {
                int ta = TiesOf(ties, a.Id), tb = TiesOf(ties, b.Id);
                if (ta != tb) { if (ta > tb) votesA++; else votesB++; }
                else if (Voice.Hash(seed + "|tie") % 2 == 0) votesA++; else votesB++;
            }
        }

        // ---- the words ----

        public static string PostedText(TownEvent e)
        {
            return "Election in " + e.Town + " a week today: " + e.AName + " and " + e.BName + " stand for " + e.Extra + ". The polls open at ten.";
        }

        public static string PaperText(TownEvent e)
        {
            return "The town of " + e.Town + " goes to the polls Tuesday week to choose a " + e.Extra + ". " + e.AName + " and " + e.BName + " have each put his name forward; the ballot will be secret, as the new law requires.";
        }

        public static string ClippingBody(TownEvent e, string aTrade, string bTrade)
        {
            return "ELECTION NOTICE\n\nThe qualified electors of " + e.Town + " are hereby notified that an election will be held on Tuesday next for the office of " + e.Extra.ToUpperInvariant() + ".\n\n" +
                   "Candidates:\n   " + e.AName + (aTrade != null ? ", " + aTrade : "") + "\n   " + e.BName + (bTrade != null ? ", " + bTrade : "") + "\n\n" +
                   "The polls open at ten o'clock in the forenoon and close at four. The ballot is secret. Signed, the town clerk.";
        }

        public static string ResultText(TownEvent e, int votesA, int votesB)
        {
            bool aWon = votesA > votesB;
            string winner = aWon ? e.AName : e.BName, loser = aWon ? e.BName : e.AName;
            int w = Math.Max(votesA, votesB), l = Math.Min(votesA, votesB);
            return winner + " is " + e.Extra + " of " + e.Town + ", by " + w + " votes to " + l + ". " + Person.FirstOf(loser) + " is said to have taken it well, or not.";
        }

        public static string ResultToast(TownEvent e, int votesA, int votesB)
        {
            bool aWon = votesA > votesB;
            return (aWon ? e.AName : e.BName) + " is the new " + e.Extra + " of " + e.Town + ", by " + Math.Max(votesA, votesB) + " votes to " + Math.Min(votesA, votesB) + ".";
        }

        /// The speech from the porch, by the candidate's temper: eight lines.
        public static string[] Speech(string temper, string town, string office, string seed)
        {
            switch (temper)
            {
                case "Proud":
                case "Hothead":
                    return new[]
                    {
                        "Friends. You know me. You know my name and you know my word.",
                        "I've stood for " + town + " when it had nobody else standing for it.",
                        "There's men who'd run this town from a saloon chair. I'd run it from the street, where you can see me.",
                        "A " + office + " ought to look a man in the eye. I've never looked anywhere else.",
                        "They say I'm hard. I say this town's had enough of soft.",
                        "Vote for me and you'll know where you stand. Vote the other way and you'll find out.",
                        "I ask nothing but your vote. And I'll give nothing but my best.",
                        "That's all I have to say. Those who know me know it's enough.",
                    };
                case "Loyal":
                case "Coward":
                    return new[]
                    {
                        "Neighbours. I'll not keep you long.",
                        "I was born in " + town + " and I expect to be buried in it. That's my whole platform.",
                        "A " + office + " ought to serve, not rule. I've served this town thirty years without a title.",
                        "I know your troubles because they're mine. The same mud. The same prices.",
                        "I won't promise what I can't do. I'll promise to be here.",
                        "There's a man running who'd sell this town for a dollar. I'd not sell it for a thousand.",
                        "Whatever you decide, we're still neighbours Wednesday morning.",
                        "Thank you. That's all. Go and vote.",
                    };
                default:
                    return new[]
                    {
                        "Well. Here I am, since you're all here.",
                        "I never wanted the office. I wanted somebody decent in it, and nobody else would stand.",
                        "A " + office + " should keep the street quiet and the books straight. I can do both.",
                        "I've no speech to make. I've a town to keep.",
                        "They'll tell you what I've done wrong. I'll tell you what I'll do right, and you can hold me to it.",
                        "Vote as you see fit. Only vote.",
                        "I'll buy nobody's vote. If you want whiskey, the saloon's that way.",
                        "That's the whole of it. Thank you for listening.",
                    };
            }
        }

        public static readonly string[] Hecklers = { "Hogwash!", "Hear him out!", "Says you!", "That's the truth, that is!", "Get down off that porch!", "Give the man a chance!", "Who paid for the hat?" };
        public static readonly string[] Grumbles = { "Marking a paper in a box, like a thief. In my day you said your man out loud and took what came.", "Secret ballot. Secret. What's a man got to hide about how he votes?", "They'll count it wrong. They always count it wrong." };
        public static readonly string[] Tickets = { "A ticket, friend? The right ticket. There's a drink in it after.", "Vote the straight ticket and I'll see you right at the bar.", "You look like a sensible man. Here, take one of these." };

        public static string Heckle(string seed) { return Hecklers[Voice.Hash(seed) % Hecklers.Length]; }
        public static string Grumble(string seed) { return Grumbles[Voice.Hash(seed) % Grumbles.Length]; }
        public static string Ticket(string seed) { return Tickets[Voice.Hash(seed) % Tickets.Length]; }

        /// The player's rows on a candidate: his word, whiskey for the voters, a word in the other's ear to step aside.
        public static List<InteractOption> Rows(TownEvent e, string candidateId, int cash)
        {
            var list = new List<InteractOption>();
            if (e == null || candidateId == null)
                return list;
            if (e.Endorsed == null)
                list.Add(new InteractOption { Id = "vote:endorse", Label = "You have my word for the vote." });
            if (e.WhiskeyFor == null)
                list.Add(new InteractOption { Id = "vote:whiskey", Label = "Whiskey for your voters ($" + WhiskeyCents / 100 + ")", Why = cash < WhiskeyCents ? "not enough money" : null });
            if (e.LeanedOn == null)
                list.Add(new InteractOption { Id = "vote:lean", Label = "Step aside from this race." });
            return list;
        }

        /// The candidate's answer.
        public static InteractAnswer Answer(string what, string temper, string regard, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            bool hostile = regard == "hostile", proud = temper == "Proud" || temper == "Hothead";
            switch (what)
            {
                case "endorse":
                    if (hostile) { a.Line = "I'd sooner lose than have your word on my side."; a.VoiceHate = 30; return a; }
                    a.Line = proud ? "Your word. Well. A vote's a vote; I'll not ask where it's been." : "That's handsome of you. Tell your friends, if you've any here.";
                    a.LikeDelta = 15;
                    a.Friendly = true;
                    a.Thought = "The stranger's for me. Whether that helps depends what the town thinks of him.";
                    return a;
                case "whiskey":
                    if (hostile) { a.Line = "Keep your whiskey and your money."; a.VoiceHate = 30; return a; }
                    if (temper == "Loyal") { a.Line = "I'll buy nobody's vote, mister. Not with your money, not with mine."; a.Thought = "Buying votes. In my town. I'll remember who offered."; a.LikeDelta = -10; return a; }
                    a.Cents = WhiskeyCents;
                    a.Line = proud ? "A round in my name. Well. It's how it's done, I'm told." : "Five dollars of whiskey buys a lot of friends on a Tuesday. Obliged.";
                    a.LikeDelta = 10;
                    a.Thought = "Whiskey votes. They count the same as the honest kind.";
                    return a;
                case "lean":
                    a.Event = "threat";
                    a.VoiceHate = 40;
                    a.LikeDelta = -30;
                    if (proud) { a.Line = "I'll not be run off by the likes of you. Say that again in the street."; a.Thought = "He wants me out of the race. That decides it: I'm in it to the end."; a.Then = "square up"; }
                    else if (temper == "Coward") { a.Line = "...Step aside. Yes. Maybe I ought. Maybe I will."; a.VoiceFear = 50; a.Thought = "He's warned me off. My wife said this would come. She was right."; }
                    else { a.Line = "You've no say in this town's business, stranger. But I hear you."; a.Thought = "Leaned on. By him. The town will hear of it before dark."; }
                    return a;
                default:
                    a.Line = "...";
                    return a;
            }
        }

        /// What the voters think after: the winner's side, the loser's.
        public static string After(bool won, string winnerFirst, string seed)
        {
            return won ? Voice.Pick(seed, winnerFirst + "'s in. I said he would be. Drinks on the town.", "My man took it. The street looks straighter already.", "Voted for " + winnerFirst + " and he got it. First time I've backed a winner.")
                       : Voice.Pick(seed, winnerFirst + " got it. Lord help us all.", "Lost. We'll see how long " + winnerFirst + " lasts in that chair.", "Four years of " + winnerFirst + ". The town voted wrong and it'll find out.");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string t in new[] { "Proud", "Loyal", null })
                foreach (string s in Speech(t, "Valentine", "mayor", "s"))
                    yield return s;
            foreach (string s in Hecklers) yield return s;
            foreach (string s in Grumbles) yield return s;
            foreach (string s in Tickets) yield return s;
            foreach (string what in new[] { "endorse", "whiskey", "lean" })
                foreach (string t in new[] { null, "Proud", "Loyal", "Coward", "Greedy" })
                    foreach (string r in new[] { null, "hostile" })
                    {
                        InteractAnswer a = Answer(what, t, r, "a");
                        yield return a.Line;
                        if (a.Thought != null) yield return a.Thought;
                    }
            for (int i = 0; i < 3; i++)
            {
                yield return After(true, "Hiram", "w" + i);
                yield return After(false, "Hiram", "l" + i);
            }
        }
    }
}
