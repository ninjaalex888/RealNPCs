// A quarrel played from the ladder (roadmap 7; the night run of 2026-10-28, item 16; Core/Escalation.cs worked out the
// ladder itself the afternoon before, unwired). Behind [Mind] MindLive and [Mind] QuarrelLadder the game side builds an
// argue or a brawl between two people the mind knows through here instead of the fixed scripts: the ladder is run from
// its start rung with the pair's traits and the surroundings as the game reads them, and each step becomes beats the
// Director already knows how to play (face, shove, fight and endfight, aim and holster, approach, walkaway). The words
// open by what it's over; the one who pushes a rung speaks and acts it; the one who brings it down ends it his way (an
// apology, a joke, backing down, walking off, a friend stepping in as the cast's third, a drink in a saloon, the fists'
// own verdict, the law in sight). The shot is NEVER played: a run that reaches it ends as a standoff that breaks off,
// and the scene says so (LadderNote), because nothing of the mind damages anybody yet (For Lex). The scene's Kind is the
// top rung reached (argue for words or a shove, brawl for blows, standoff for a gun drawn), and StartScene notes it by what
// happened in it (a standoff that fought is a brawl, an argue ended by an apology or a drink an apology), so the ties, the
// memories, the plain record and the record fates flow as they do for the fixed scenes. Off, nothing here runs.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A brawl reason starts the ladder here (the blows rung): Reasons already read the tempers and the drink that made it a
        /// brawl, so the words, the shove and the fists are given, and the ladder decides how it ends (a man down, the law,
        /// a friend, an apology) and whether a gun comes out. An argue starts at words (0). At 1 (the shove) the ladder
        /// called the fists too, and a brawl reason came to blows only a third of the time (mindtest, 2026-10-29), a week
        /// after Lex asked for brawls often; the knob is here (For Lex).
        public const int BrawlStartRung = 2;

        /// What a quarrel is over, when the pair's own business doesn't say: a saloon's are drink and honour, the
        /// street's a look or a word over nothing, and money now and then.
        public static string MatterOf(bool saloon, bool drunk, string seed, bool strangers = false)
        {
            int r = Voice.Hash(seed + "|matter") % 100;
            if (strangers)
                return saloon ? (r < 50 ? "drink" : r < 80 ? "honour" : "nothing") : (r < 60 ? "nothing" : "honour");   // strangers owe each other nothing and share no fence (the review)
            if (saloon)   // drink in him makes drink the likelier matter
                return r < (drunk ? 55 : 40) ? "drink" : r < (drunk ? 78 : 70) ? "honour" : r < 90 ? "money" : "nothing";
            return r < 45 ? "nothing" : r < 75 ? "honour" : r < 90 ? "money" : "land";
        }

        /// The matter a worry between two people makes of their quarrel (a debt is money, a feud honour, courting a woman).
        public static string MatterOfWorry(string worryKind)
        {
            switch (worryKind)
            {
                case "debt": case "bank": case "cards": case "savings": return "money";
                case "feud": return "honour";
                case "courting": return "woman";
                case "drought": case "land": return "land";
                case "drink": return "drink";
                default: return null;
            }
        }

        /// The quarrel from the ladder. `startRung`: 0 words (an argue), 2 the blows (a brawl reason). `steps` gets the
        /// run for the log. A woman never goes past the shove and nobody draws what he doesn't carry (Escalation's own
        /// rules); the friend way needs the cast's third (`c.Friend` 0 or 1: whose friend), else it's a walk-off.
        /// shotsPlay (Lex, 2026-10-30: "fights escalate to having the NPCs use guns on each other", deadly, true to 1899;
        /// [Mind] LadderShots): the shot is fired, not thought better of; off (and in the tests' runs), as before.
        public static Scene Quarrel(SceneCast c, string seed, Traits ta, Traits tb, QuarrelContext q, int startRung, out List<Escalation.Step> steps, bool shotsPlay = false)
        {
            if (q == null)
                q = new QuarrelContext();
            if (c.Friend < 0)
                q.FriendOfA = q.FriendOfB = false;   // nobody to step in: the maths mustn't pick the friend way
            else if (c.Friend == 0)
                q.FriendOfB = false;
            else
                q.FriendOfA = false;
            steps = Escalation.RunFrom(startRung, ta, tb, q, seed + "|ladder");
            string top = Escalation.Reached(steps);
            Escalation.Step last = steps[steps.Count - 1];
            bool shot = last.Ended == "shot";
            if (shot)
                top = "draw";   // the shot isn't played: the standoff breaks off
            bool blows = top == "blows" || top == "draw";
            bool drew = top == "draw";
            bool twoMen = !c.AWoman && !c.BWoman;
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene
            {
                Kind = drew ? "standoff" : blows ? "brawl" : "argue",
                Fight = blows,
                Cast = 2,
                Summary = drew ? (twoMen ? "two men squared off" : "two people squared off") + (q.Place == "saloon" ? " in the saloon" : " in the street")
                        : blows ? (twoMen ? "two men came to blows" : "two people came to blows") : "hard words" + (q.Place == "saloon" ? " in the saloon" : " in the street"),
                Ladder = steps
            };
            // fired only where the gun may come out (BuildCast's rule for the cast: armed, no law near, not a saloon)
            bool fires = shot && shotsPlay && (Who(last.By) == 0 ? c.Armed0 : c.Armed1);
            if (fires)
            {
                s.LadderNote = "the ladder said a shot (" + Side(last.By, c.Names[0] ?? a, c.Names[1] ?? b, c) + "): fired";
                s.Summary = "shots fired" + (q.Place == "saloon" ? " in the saloon" : " in the street");
            }
            else if (shot)
                s.LadderNote = "the ladder said a shot (" + Side(last.By, c.Names[0] ?? a, c.Names[1] ?? b, c) + "); not played, the standoff breaks off (the mind damages nobody yet)";
            // the words: what it's over (q.Over first, when the game side named it: a slight, a bad week, the old business)
            string[] open = Opening(q.Matter, q.Over, seed, a, b);
            s.Over = q.Over;
            string said = OverSummary(q.Over);
            if (said != null && s.Summary != null && s.LadderNote == null)
                s.Summary += " " + said;   // the town retells it by what it was over (item 9)
            s.Add(0, open[0], open[3], "face").Add(1, open[1], "no").Add(0, open[2], "angry");
            // the rungs already climbed before the ladder was asked (a brawl reason starts at the blows): A, who opened,
            // did them
            for (int r = 1; r <= startRung && r < Escalation.Rungs.Length; r++)
                Up(s, new Escalation.Step { From = Escalation.Rungs[r - 1], To = Escalation.Rungs[r], By = "a" }, c, seed, a, b, q);   // A: the reason's doer, who opened the words (the review: the aggrieved side belongs to the maths)
            // every beat of the ladder's names its people, not the winner and the loser: the Director's endfight swaps the cast by who
            // won, and a beat that follows its people swaps with them (Beat.Follows); only the fists' own verdict (beaten) is
            // written for the winner at 0 and the loser at 1 (the review)
            var fixedBeats = new List<Beat>();
            foreach (Escalation.Step st in steps)
            {
                if (st.To != null)
                    Up(s, st, c, seed, a, b, q);
                else if (st.Ended != null && !shot)
                {
                    int before = s.Beats.Count;
                    Down(s, st, c, seed, a, b, q, twoMen);
                    if (st.Ended == "beaten")
                        for (int i = before; i < s.Beats.Count; i++)
                            fixedBeats.Add(s.Beats[i]);
                }
            }
            if (fires)
            {
                // the shot: the one who pushed the draw to it fires, and the other fires back if he carries a gun, or runs (the
                // Director's "shoot"); whoever falls is the ledger's, and what follows a death follows
                int w = Who(last.By);
                s.Add(w, Voice.Pick(seed + "|shot|" + w, "Then it's this.", "You were warned.", "Enough talk.", "Should've walked away."), "angry", "shoot");
            }
            else if (shot)
            {
                // the draw played; the shot doesn't: the one who would have fired thinks better of it, and both put up
                int w = Who(last.By);
                s.Add(w, Voice.Pick(seed + "|noshot|" + w, "...Not today. Not for this.", "...No. Not over this.", "Go on. Walk, before I think better of it.",
                                    "...I'd hang for you, and you ain't worth the rope."), "mild", "holster")
                 .Add(1 - w, Voice.Pick(seed + "|noshot|other", "Another day, then.", "...Another day.", "Smart.", "I'll remember this."), "mild", "holster")
                 .Add(w, null, null, "walkaway").Add(1 - w, null, null, "walkaway");
            }
            foreach (Beat bt in s.Beats)
                bt.Follows = !fixedBeats.Contains(bt);
            return s;
        }

        private static int Who(string by)
        {
            return by == "b" || by == "friendB" ? 1 : 0;
        }

        private static string Side(string by, string a, string b, SceneCast c)
        {
            return by == "a" ? a : by == "b" ? b : by == "friendA" ? "a friend of " + a : by == "friendB" ? "a friend of " + b : by ?? "?";
        }

        // ---- what it's over, in so many words (FOURTEENHOUR item 1; Lex's log of 2026-11-09: four brawls in Valentine, every one
        // "over nothing" in the ladder's line, though two of the four had a cause). The matter above is what the ladder's odds read
        // (Escalation: a look or a word over nothing is honour's own matter, and the Lemoyne rule counts it so); it stays exactly as
        // it was. This names the thing itself, for the opening words and the log only: a slight by the place for strangers, the
        // doer's own bad week for a man at the worst of his trouble (never a debt's or a sweetheart's words to a stranger: strangers
        // owe each other nothing), the old business for two who are already foes. Why it reads true: onlookers explain a fight by
        // its cause and retell it by its cause; "over nothing" reads as a machine, "over a spilled drink" as two men.

        /// The street's slights and the saloon's, by place.
        public static readonly string[] StreetSlights = { "shoulder", "way", "look", "horse", "hat" };
        public static readonly string[] SaloonSlights = { "spill", "seat", "laugh", "look" };

        /// A slight by the place, seeded: the street's a shoulder in passing (30%), the way blocked (20), a look held (25), a word
        /// about a man's horse (13) or hat (12); a saloon's a spilled drink (35), a seat taken (20), a laugh (20), a look (25).
        public static string SlightOf(bool saloon, string seed)
        {
            int r = Voice.Hash(seed + "|slight") % 100;
            if (saloon)
                return "slight:" + (r < 35 ? "spill" : r < 55 ? "seat" : r < 75 ? "laugh" : "look");
            return "slight:" + (r < 30 ? "shoulder" : r < 50 ? "way" : r < 75 ? "look" : r < 88 ? "horse" : "hat");
        }

        /// What a quarrel is over in words, once its matter is known (null: the matter's own words say it). `over`: the reason's
        /// own (the snap's worry), kept first. Only where the matter is still "nothing" (after the pair's debt or feud, which win),
        /// or "honour" between strangers (they have no talk about each other to quarrel over: a word taken wrong).
        public static string OverFor(string matter, string over, bool saloon, int warmth, string tieKind, bool strangers, string seed,
                                     string groupA = null, bool piousA = false, string groupB = null, bool piousB = false, bool drunk = false, int year = 1899)
        {
            if (over != null && (matter == null || matter == "nothing"))
                return over;
            // two in drink on opposite sides of one of 1899's quarrels (FOURTEENHOUR item 9): where it would be a look or a word
            // (honour only between strangers: a feud between neighbours is honour too, and is never over silver; the review of items 1-13)
            if (drunk && (matter == null || matter == "nothing" || (matter == "honour" && strangers) || (matter == "drink" && saloon)) && warmth >= 0)   // (a saloon's drink is the setting, not a grievance)
            {
                string pol = QuarrelPolitics.OverFor(groupA, piousA, groupB, piousB, year, seed);
                if (pol != null)
                    return pol;
            }
            if (matter == "nothing" || matter == null)
                return warmth < 0 ? "old:" + (string.IsNullOrEmpty(tieKind) ? "argue" : tieKind) : SlightOf(saloon, seed);
            if (matter == "honour" && strangers)
                return "slight:word";
            return null;
        }

        /// For the log and the model's list: "a shoulder in passing", "a bad week of his own (secret)", "the old business between
        /// them (rough)"; the matter itself when there's nothing finer.
        public static string OverWords(string matter, string over)
        {
            if (string.IsNullOrEmpty(over))
                return matter ?? "nothing";
            int c = over.IndexOf(':');
            string head = c > 0 ? over.Substring(0, c) : over, what = c > 0 ? over.Substring(c + 1) : "";
            switch (head)
            {
                case "snap": return "a bad week of their own (" + what + ")";
                case "politics": return QuarrelPolitics.Words(what);
                case "war": return "the war, blue and grey";   // the old soldiers (FOURTEENHOUR item 17)
                case "class": return "a gentleman's snub";     // the well-off and the working man (item 18)
                case "old": return "the old business between them (" + what + ")";
                case "slight":
                    switch (what)
                    {
                        case "shoulder": return "a shoulder in passing";
                        case "way": return "the way blocked";
                        case "look": return "a look held too long";
                        case "horse": return "a word about a horse";
                        case "hat": return "a word about a hat";
                        case "spill": return "a spilled drink";
                        case "seat": return "a seat taken";
                        case "laugh": return "a laugh at the wrong moment";
                        case "word": return "a word taken wrong";
                    }
                    break;
            }
            return matter ?? "nothing";
        }

        /// What it was over, for the scene's summary (the bar's words and the town's talk after: "two men came to blows over a
        /// spilled drink"); null when there's nothing finer than the matter.
        public static string OverSummary(string over)
        {
            if (string.IsNullOrEmpty(over))
                return null;
            int c = over.IndexOf(':');
            string head = c > 0 ? over.Substring(0, c) : over, what = c > 0 ? over.Substring(c + 1) : "";
            switch (head)
            {
                case "politics": return QuarrelPolitics.Summary(what);
                case "war": return "over the war";
                case "class": return "over a gentleman's snub";
                case "snap": return "over one man's bad week";
                case "old": return "over old business";
                case "slight": return "over " + OverWords(null, over);
            }
            return null;
        }

        /// The opening exchange: A opens, B answers, A presses; the fourth is A's opening voice. `over` (OverFor) first.
        private static string[] Opening(string matter, string over, string seed, string a, string b)
        {
            string[] o = over != null ? OverOpening(over, seed, b) : null;
            return o ?? Opening(matter, seed, a, b);
        }

        /// Every opening an `over` can give (for the tests): each of the four is a line, and the fourth a voice.
        public static IEnumerable<string[]> OverOpenings(string over, string b)
        {
            for (int i = 0; i < 4; i++)
            {
                string[] o = OverOpening(over, "t" + i, b, i);
                if (o != null)
                    yield return o;
            }
        }

        private static string[] OverOpening(string over, string seed, string b, int forced = -1)
        {
            int c = over.IndexOf(':');
            string head = c > 0 ? over.Substring(0, c) : over, what = c > 0 ? over.Substring(c + 1) : "";
            string[][] set = null;
            switch (head)
            {
                case "slight": set = SlightLines(what, b); break;
                case "snap": set = SnapLines(what); break;
                case "old": set = OldLines(what, b); break;
                case "politics": set = QuarrelPolitics.Openers(what); break;
            }
            if (set == null || set.Length == 0)
                return null;
            int i = forced >= 0 ? forced % set.Length : Voice.Hash(seed + "|over|" + over) % set.Length;
            return set[i];
        }

        // A slight by the place: what a stranger does that another man won't swallow. The first two are the openings every
        // quarrel over nothing had before (the look and the shoulder), kept.
        private static string[][] SlightLines(string what, string b)
        {
            switch (what)
            {
                case "shoulder": return new[] {
                    new[] { "Watch where you're walking.", "You walked into me.", "I'd say not.", "angry" },
                    new[] { "You near knocked me down, " + b + ".", "Then stand out of the road.", "Say that again.", "angry" },
                    new[] { "That was my shoulder you just went through.", "It was in my way.", "Your way's about to get shorter.", "threat" } };
                case "look": return new[] {
                    new[] { "You got something to say to me?", "Didn't say a word.", "You looked it.", "mild" },
                    new[] { "You've been staring at me since I came out.", "It's a free country.", "Not for staring, it ain't.", "threat" } };
                case "way": return new[] {
                    new[] { "You're in my road.", "There's room to go round.", "I don't go round.", "angry" },
                    new[] { "Stand aside.", "Ask me nicely.", "I don't ask.", "threat" } };
                case "horse": return new[] {
                    new[] { "That's my horse you're running down.", "That nag? I'd not give a dollar for it.", "Say that again, close up.", "angry" },
                    new[] { "Leave my horse be.", "I was only looking at the brute.", "Look at something else.", "threat" } };
                case "hat": return new[] {
                    new[] { "Something wrong with my hat?", "Nothing a fire wouldn't mend.", "You'll eat those words.", "angry" },
                    new[] { "You laughing at my hat?", "Wasn't laughing.", "Smiling, then. Same thing.", "mild" } };
                case "spill": return new[] {
                    new[] { "You spilled my drink, " + b + ".", "Then buy another.", "You'll buy it.", "angry" },
                    new[] { "That's whiskey all down my coat.", "Whiskey's improved it.", "Has it, now.", "angry" } };
                case "seat": return new[] {
                    new[] { "That's my seat.", "I saw no name on it.", "It does now.", "threat" },
                    new[] { "I was sitting there.", "And now you're standing.", "Not for long.", "angry" } };
                case "laugh": return new[] {
                    new[] { "Something funny?", "Not you. Something else.", "Laugh at me again.", "threat" },
                    new[] { "You think that's funny, " + b + "?", "I think you're funny.", "Let's see you laugh with a split lip.", "angry" } };
                case "word": return new[] {
                    new[] { "What did you call me?", "Nothing you didn't earn.", "Say it again.", "angry" },
                    new[] { "I heard that.", "You were meant to.", "Then we'll settle it.", "threat" },
                    new[] { "Say that again. Louder.", "I said what I said.", "Then come and say it closer.", "angry" } };
            }
            return null;
        }

        // A man at the worst of his own trouble, snapping at whoever is there: his week in the words, never the other's business
        // (the stranger owes him nothing, and these don't say he does). The displaced anger of a bad week (NIGHT3's lens).
        private static string[][] SnapLines(string worry)
        {
            switch (worry)
            {
                case "debt": case "bank": case "cards": case "savings": return new[] {
                    new[] { "Something funny? A man's owing to his eyes, and you find it funny?", "I wasn't laughing at you.", "You were laughing.", "angry" },
                    new[] { "Go on, ask me for money too. Everybody else has.", "I never asked you for a thing.", "Not yet you haven't.", "angry" } };
                case "secret": return new[] {
                    new[] { "What have you heard? Who's been talking to you?", "Talking? About what?", "Don't lie to me.", "threat" },
                    new[] { "Quit looking at me like you know something.", "I never laid eyes on you before.", "Then quit looking.", "angry" } };
                case "law": return new[] {
                    new[] { "You looking at my face for a reason? Seen it on a paper somewhere?", "Never seen you before.", "See that it stays that way.", "threat" } };
                case "drink": return new[] {
                    new[] { "Go on. Say it. Everybody else does.", "Say what?", "That I've been at the bottle again.", "angry" } };
                case "fever": return new[] {
                    new[] { "My child's burning up at home, and you're stood in my road.", "Then go round.", "You go round.", "angry" } };
                case "tooth": return new[] {
                    new[] { "My jaw's on fire, and you're in my way. Shift.", "There's room enough.", "Not for both of us there ain't.", "angry" } };
                case "son": return new[] {
                    new[] { "My boy's gone off and not written, and you want to stand there grinning?", "I wasn't grinning.", "You were.", "angry" } };
                case "letter": return new[] {
                    new[] { "Three weeks I've waited on a letter, and here you are crowding me.", "I'm nowhere near you.", "You're near enough.", "angry" } };
                case "courting": return new[] {
                    new[] { "She turned me down flat, and now you want to laugh about it?", "Who turned you down?", "Don't you play the innocent.", "angry" } };
                case "drought": return new[] {
                    new[] { "Not a drop of rain since spring, and you want to stand in my road?", "It's a wide road.", "Not today it ain't.", "angry" } };
                case "feud": return new[] {
                    new[] { "You're thick with my neighbour, ain't you? I've seen you.", "I don't know your neighbour.", "Then what are you smiling at?", "threat" } };
            }
            return new[] {
                new[] { "What are you gawping at? I've had a week, and you're in it.", "I'm only standing here.", "Then stand somewhere else.", "angry" },
                new[] { "Move. I've trouble enough this week without you.", "Steady, now. I never touched you.", "You're doing it now.", "angry" },
                new[] { "Don't you start with me. Not today.", "I never said a word.", "You were fixing to.", "threat" } };
        }

        // Two who are already foes, over the business between them (the tie's kind: what was done last time).
        private static string[][] OldLines(string kind, string b)
        {
            switch (kind)
            {
                case "rough": return new[] {
                    new[] { "You put my face in that bar, " + b + ". I haven't forgotten.", "You had it coming.", "Then here's what you've got coming.", "threat" } };
                case "pickpocket": case "lift": return new[] {
                    new[] { "You had your hand in my coat, you thief.", "Prove it.", "I'll prove it on your face.", "angry" } };
                case "brawl": case "blows": case "rematch": case "standoff": return new[] {
                    new[] { "Last time was luck, and you know it.", "Luck had nothing to do with it.", "We'll see about that.", "threat" } };
                case "brushoff": case "avoid": case "snub": return new[] {
                    new[] { "You walked past me like I was dirt.", "Maybe you are.", "Say that again.", "angry" } };
            }
            return new[] {
                new[] { "I've not forgotten what you said to me, " + b + ".", "I meant every word.", "Then we've business to settle.", "angry" },
                new[] { "You've a nerve, showing your face to me.", "It's a free street.", "Not for you, it ain't.", "threat" } };
        }

        /// The opening exchange by the matter alone (as before item 1).
        private static string[] Opening(string matter, string seed, string a, string b)
        {
            string k = seed + "|open|" + matter;
            switch (matter)
            {
                case "honour":
                    return Voice.Chance(k, 50)
                        ? new[] { "You called me a liar before the whole room, " + b + ".", "I called it as I saw it.", "Then you saw wrong, and you'll say so.", "angry" }
                        : new[] { "I hear you've been talking about me all over town.", "I say what I like.", "Not about me, you don't.", "threat" };
                case "woman":
                    return Voice.Chance(k, 50)
                        ? new[] { "Keep away from her, " + b + ". You've been told.", "She can talk to who she pleases.", "Not to you.", "threat" }
                        : new[] { "I saw you at her gate last night.", "A man can walk where he likes.", "Not past that gate, he can't.", "angry" };
                case "money":
                    return Voice.Chance(k, 50)
                        ? new[] { "You owe me four dollars, " + b + ", and I've waited long enough.", "You'll have it when I have it.", "I've heard that since Easter.", "mild" }
                        : new[] { "That hog was mine, and you sold it as yours.", "Prove it was.", "Half the county knows that hog.", "angry" };
                case "land":
                    return Voice.Chance(k, 50)
                        ? new[] { "Your fence is ten feet over my line.", "The line's where my father put it.", "Your father couldn't read a survey either.", "mild" }
                        : new[] { "You turned your cattle onto my water again.", "Water runs where it runs.", "And my grass goes with it.", "angry" };
                case "drink":
                    return Voice.Chance(k, 50)
                        ? new[] { "That was my glass, " + b + ".", "Was it. It's empty now.", "Then you'll be filling it.", "angry" }
                        : new[] { "You're leaning on me, " + b + ". Stand up straight or sit down.", "I'll lean where I like.", "Not on me, you won't.", "mild" };
                default:
                    return Voice.Chance(k, 50)
                        ? new[] { "You got something to say to me?", "Didn't say a word.", "You looked it.", "mild" }
                        : new[] { "Watch where you're walking.", "You walked into me.", "I'd say not.", "angry" };
            }
        }

        /// A rung climbed: the pusher speaks and does it.
        private static void Up(Scene s, Escalation.Step st, SceneCast c, string seed, string a, string b, QuarrelContext q)
        {
            int w = Who(st.By);
            string me = w == 0 ? a : b, other = w == 0 ? b : a;
            string k = seed + "|up|" + st.To + "|" + w;
            switch (st.To)
            {
                case "shove":
                    s.Add(w, Voice.Pick(k, "Out of my road!", "I said move!", "Say it again, close up!", "You want it, you'll have it!"), "angry", "shove");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "Put your hands on me once more and I'll break them.", "That's the last time you touch me.", "You've done it now.",
                                             "Easy with those hands, " + me + "."), "angry");
                    break;
                case "blows":
                    s.Add(w, Voice.Pick(k, "That's it. Come here!", "I've heard enough out of you.", "Put up your hands!", "Right. Right, then."), "angry", "fight", 9000);
                    s.Add(0, null, null, "endfight");
                    break;
                case "draw":
                    // the gun comes out only where the cast may draw (BuildCast's rule: both armed, no law near, not a
                    // saloon); otherwise the same words over a hand drifting to the belt (the aim act then only faces)
                    bool gun = w == 0 ? c.Armed0 : c.Armed1;
                    s.Add(w, Voice.Pick(k, "Go on. Reach for it.", "You've got a gun on you. Use it or shut your mouth.", "Say it again with your hand on that iron.",
                                        "I've looked down a barrel before, " + other + ". Yours don't scare me."), gun ? "armed" : "threat", "aim", 300);
                    s.Add(1 - w, Voice.Pick(k + "|answer", "You first.", "Steady, now. Steady.", "They'd stretch your neck for it, " + me + ".", "Think hard before you do."), "stare", "aim", 300);
                    int wait = Voice.Hash(k + "|wait|pair") % 3;   // one roll picks the pair, so the answer answers (the review)
                    s.Add(w, new[] { "Last chance to walk away.", "Nobody has to die today.", "Well?" }[wait], null);
                    s.Add(1 - w, new[] { "I was about to say the same.", "Somebody always does.", "Well what?" }[wait], null);
                    break;
            }
        }

        /// The quarrel brought down at a rung, the way the cooler one (or a friend, or the law in sight) does it.
        private static void Down(Scene s, Escalation.Step st, SceneCast c, string seed, string a, string b, QuarrelContext q, bool twoMen)
        {
            int rung = Array.IndexOf(Escalation.Rungs, st.From);
            bool drawn = rung >= 3;
            string way = st.Ended;
            if (way == "friend" && c.Friend < 0)
                way = "walkoff";   // nobody to step in after all
            int w = way == "friend" ? (st.By == "friendA" ? 0 : 1) : Who(st.By);   // for friend: whose friend; he calms his own man
            string me = w == 0 ? a : b, other = w == 0 ? b : a;
            string own = c.Names != null && w < c.Names.Length && !string.IsNullOrEmpty(c.Names[w]) ? c.Names[w] : me;   // the friend knows his own man by name whatever the other does (the review)
            string k = seed + "|down|" + way + "|" + rung;
            if (drawn && way != "law")
            {
                // guns up before a word of it (the law way holsters as it goes)
                s.Add(0, null, null, "holster").Add(1, null, null, "holster");
            }
            switch (way)
            {
                case "apology":
                    s.Add(w, Voice.Pick(k, "...I spoke out of turn. I'm sorry for it.", "All right. That was wrong of me. I take it back.", "I had no call to say that. Forget it.",
                                        "My temper got the better of me. I'm sorry, " + other + "."), "sorry");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "...Forget it, then.", "See that it stays took back.", "Hm. All right.", "Don't let it happen again."), "mild", "walkaway");
                    break;
                case "joke":
                    s.Add(w, twoMen
                            ? Voice.Pick(k, "Lord, listen to the pair of us. My wife would laugh herself sick.", "If you hit me, do it gentle. I've got a bad back.",
                                         "You're too ugly to hit, " + other + ". I'd only improve you.", "Let's not. I've only got the one good shirt.")
                            : Voice.Pick(k, "Lord, listen to the pair of us. The whole street's enjoying it.", "Let's not. I've only got the one good shirt.",
                                         "I've had this argument with my mother and lost. I know when I'm beat.", "We'll be the talk of the church supper at this rate."), "mild");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "...Ha. Get out of here.", "You're a damned fool, " + me + ".", "Hm. Off with you, then.", "Lucky for you I'm thirsty."), "mild", "walkaway");
                    break;
                case "backdown":
                    s.Add(w, Voice.Pick(k, "...All right. All right, you win.", "I don't want trouble. Leave it there.", "Suit yourself, then.", "...Fine. I was wrong."), "mild");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "That's what I thought.", "Then we understand each other.", "Good. Remember it.", "Go on, then."), "won");
                    s.Add(w, null, null, "walkaway");
                    break;
                case "walkoff":
                    s.Add(w, Voice.Pick(k, "I've no more time for this.", "Talk to the wind. I'm done.", "You ain't worth the trouble.", "Say what you like. I'm going."), "storm", "walkaway");
                    break;
                case "friend":
                    // the friend (the cast's third) comes between them and talks his man down; the hot one goes with him. He
                    // walks to his own man (before 2026-10-29 to A whoever his man was: NIGHT.md For Lex 21)
                    s.Beats.Add(new Beat(2, null, null, "approach") { To = w });
                    Beat word = new Beat(2, Voice.Pick(k, "Easy, " + own + ". Easy. It ain't worth it.", "Leave it, " + own + ". Come on, leave it.", "That's enough, " + own + ". Not here.",
                                                        own + ". Walk with me. Now."), "intervene", "faceone") { To = w };
                    s.Beats.Add(word);
                    s.Add(w, Voice.Pick(k + "|answer", "...All right. I'm coming.", "Keep him off me, then.", "Another time, " + other + ".", "...Let go of my arm. I'm going."), "mild", "walkaway");
                    s.Add(2, null, null, "walkaway");
                    s.Cast = 3;
                    break;
                case "drink":
                    s.Add(w, Voice.Pick(k, "Ah, hell. Let me stand you one and we'll forget it.", "Sit down, " + other + ". I'll buy. Call it settled.",
                                        "This is thirsty work. Have a drink on me and we're square.", "Pour the man a whiskey and I'll pay. There."), "mild");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "...Make it a double.", "Well. I never refused a drink yet.", "Hm. All right. Just the one.", "Now you're talking sense."), "cheer");
                    break;
                case "beaten":
                    // after endfight the Director has the winner at 0, whatever the ladder's By said (the fists decide)
                    s.Add(0, Voice.Pick(k, "Stay down.", "That'll learn you.", "Had enough?", "Mind your tongue next time."), "won");
                    s.Add(1, Voice.Pick(k + "|loser", "...Enough. I've had enough.", "Lucky punch.", "This ain't over.", "...My damned jaw."), "mild", "walkaway");
                    break;
                case "law":
                    s.Add(w, Voice.Pick(k, "The law's watching. Not here.", "Sheriff's looking this way. Leave it.", "There's the sheriff. Leave it, " + other + ".",
                                        "Not under the sheriff's nose."), "mild", drawn ? "holster" : null);
                    if (drawn)
                        s.Add(1 - w, null, null, "holster");
                    s.Add(w, null, null, "walkaway").Add(1 - w, null, null, "walkaway");
                    break;
                default:
                    s.Add(w, null, null, "walkaway");
                    break;
            }
        }

        /// Every line the ladder's scenes can say, for the line tools and the overlap checks.
        public static IEnumerable<string> AllQuarrelLines()
        {
            var cast = new SceneCast { Town = "Valentine" };
            cast.Names[0] = "Abe"; cast.Names[1] = "Hollis"; cast.Names[2] = "Cyrus";
            // a hot man and a cool one, so the runs climb and come down every way there is
            var hot = new Traits(); hot.Z[Traits.Anger] = 2f; hot.Z[Traits.Risk] = 1.5f; hot.Z[Traits.SelfControl] = -1.5f; hot.Z[Traits.Assertiveness] = 1f;
            var cool = new Traits(); cool.Z[Traits.Anger] = -1f; cool.Z[Traits.SelfControl] = 1.5f; cool.Z[Traits.Compassion] = 1f; cool.Z[Traits.Honesty] = 1f; cool.Z[Traits.Sociability] = 1f; cool.Z[Traits.Anxiety] = 1f;
            var seen = new HashSet<string>();
            foreach (string matter in new[] { "honour", "woman", "money", "land", "drink", "nothing" })
                for (int i = 0; i < 160; i++)
                {
                    List<Escalation.Step> steps;
                    bool saloon = i % 2 == 0;
                    var q = new QuarrelContext { Matter = matter, Place = saloon ? "saloon" : "street", Hour = saloon ? 23 : 15, DrunkA = i % 3 == 0, DrunkB = i % 5 == 0, ArmedA = i % 4 != 3, ArmedB = i % 4 != 0,
                                                 LawNear = i % 6 == 0, FriendOfA = true, FriendOfB = true, Witnesses = i % 7, Warmth = (i % 7) - 3, History = i % 4, Aggrieved = i % 3 == 0 ? "b" : null };
                    cast.Friend = (i % 3) - 1;
                    cast.Armed0 = cast.Armed1 = i % 2 == 1;
                    bool swap = i % 8 >= 4;
                    Scene s = Quarrel(cast, "ql" + matter + i, swap ? cool : hot, swap ? hot : cool, q, i % 3 == 2 ? BrawlStartRung : i % 3, out steps);
                    foreach (Beat bt in s.Beats)
                        if (bt.Line != null && seen.Add(bt.Line))
                            yield return bt.Line;
                }
            // and every opening of what it's over, in words (FOURTEENHOUR item 1), with a name and with a stranger's "mister"
            foreach (string over in AllOvers())
                foreach (string b in new[] { "Hollis", "mister" })
                    foreach (string[] o in OverOpenings(over, b))
                        for (int k = 0; k < 3; k++)
                            if (seen.Add(o[k]))
                                yield return o[k];
        }

        /// Every `over` the openings know: each slight, each worry's snap (and a worry with no lines of its own), each old business.
        public static IEnumerable<string> AllOvers()
        {
            foreach (string s in StreetSlights.Concat(SaloonSlights).Distinct())
                yield return "slight:" + s;
            yield return "slight:word";
            foreach (string w in new[] { "debt", "bank", "cards", "savings", "secret", "law", "drink", "fever", "tooth", "son", "letter", "courting", "drought", "feud", "none" })
                yield return "snap:" + w;
            foreach (string k in new[] { "rough", "pickpocket", "lift", "brawl", "blows", "rematch", "standoff", "brushoff", "avoid", "snub", "argue" })
                yield return "old:" + k;
            foreach (string k in QuarrelPolitics.Keys())
                yield return "politics:" + k;
        }
    }
}
