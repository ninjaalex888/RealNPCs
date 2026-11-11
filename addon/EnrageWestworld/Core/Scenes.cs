// Little plays between strangers near the player: a flirt, a quarrel, a brawl, a proposal, a jealous
// husband, a cheat at cards, a debt called in, gossip, two people wondering about their world, a eulogy,
// a wedding - and (Scenes.Frontier.cs) standoffs, drunk singing, tall tales, the paper read aloud, horse
// trading, a snake-oil pitch, a posse forming, a widow, small talk and a pickpocket. Each beat is one line
// (and/or an action) for one of the cast; the game layer stages it, and the player can step into it.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Beat
    {
        public int Who;          // 0 = A, 1 = B, 2 = C
        public string Line;      // subtitle, may be null for a pure action
        public string Voice;     // the game's voiced line set (see Speech.Set): greet, angry, mild, scared, cheer, mock, plead...
        public string Act;       // face, approach, shove, fight, endfight, walkaway, flee, kneel, stand, mourn, aim, holster,
                                 // drunk, flask, smoke, run, faceplayer, lookplayer, whittle, stoic, cb:<callback>
        public int Ms;           // extra pause after the beat
        public int To = -1;      // who it's said to or done at (-1: the other of A and B, or A for C)
        public bool Follows;     // Who and To name people, not places: when the Director's endfight swaps the cast by who won, this beat's Who and To swap with them and its line keeps its names (the ladder's beats, but the winner's and loser's own)
        public Func<string> Late;   // the line as it's played, when it may still be on its way (the model wording an answer: MindTalk); null from it, the Line stands

        public Beat(int who, string line, string voice = null, string act = null, int ms = 0)
        {
            Who = who;
            Line = line;
            Voice = voice;
            Act = act;
            Ms = ms;
        }
    }

    public sealed class Scene
    {
        public string Kind;
        public int Cast = 2;
        public bool Fight;
        public string Summary;   // for gossip afterwards: "two men came to blows"
        public bool NoteAtCue;   // its tie and memories wait for the scene's own cue (a cb: act), not its start
        public int Doer = -1;    // which of the cast did the one-sided thing when the scene's own shape says (the creditor who called a debt in, the one who brushed the other off); -1: the tie's rule (Ties.Trouble: A) or nobody (item 17)
        public List<Escalation.Step> Ladder;   // the quarrel ladder's run this scene was built from (Scenes.Ladder.cs; behind [Mind] MindLive); null for the fixed scenes
        public bool NoLadder;                  // a scene with its own words for a fight (the greenhorn's; FOURTEENHOUR item 16): never rebuilt by the quarrel ladder
        public string Over;                    // what it was over in words (Scenes.OverFor: a slight, a bad week, the old business, 1899's quarrels; FOURTEENHOUR items 1, 9), else null
        public string LadderNote;              // what the ladder said that the scene doesn't play (the shot), for the log
        public int Amount;                     // dollars at stake, where the scene has a sum (the debt called in): what the player can cover (Core/Fondness.cs)
        public readonly List<Beat> Beats = new List<Beat>();

        public Scene Add(int who, string line, string voice = null, string act = null, int ms = 0)
        {
            Beats.Add(new Beat(who, line, voice, act, ms));
            return this;
        }
    }

    public sealed class SceneCast
    {
        public bool AWoman, BWoman, CWoman;
        public string Town;
        public bool Saloon;
        public bool Night;
        public bool Payday;          // Saturday afternoon, the week's pay out ([Scenes] Payday; Core/Payday.cs)
        public float Trouble = 1f;   // [Scenes] TroubleWeight: the brawl's and the argument's share of a chance meeting scaled by it
        public bool ArgueCapped = true;   // [Scenes] ChanceFirst: the argument's share scaled by the weight up to 1.5 only, the brawl's by all of it (NIGHT4 fix 7: at 5, argue was half of all chance meetings)
        public bool WomenFight;      // [Scenes] WomenInFights: a brawl with a woman in it, at half the share for each woman (Lex, 2026-10-29)
        public int HostTier;     // how strange the town feels (0-2)
        public List<string> Rumours = new List<string>();   // third-person rumours that can be gossiped
        public bool HasCrowd;                               // a third bystander (index 2) to heckle, cheer or call the law
        public bool OldTimer;                               // A looks old enough to tell tall tales
        public bool Armed0, Armed1;                         // carrying guns: a standoff draws them
        public string Legend;                               // the player's legend as this town tells it, if he has one
        public bool PlayerNear;                             // the player is close enough to be noticed
        public List<string> PaperItems = new List<string>(); // the latest small items from the papers
        public string CrimeRumour;                          // a crime here lately, third person
        public string CrimeDesc;                            // how the witnesses describe whoever did it
        public bool CrimeSeen;                              // somebody saw the culprit's face
        public string DeadName, DeadLabel;                  // a stranger the player killed here, for the widow
        public bool DeadWoman, DeadSeen, DeadWillReturn;
        public HashSet<string> Off = new HashSet<string>(); // kinds switched off in the ini
        public string[] Names = new string[3];              // the names A, B and C really go by (enRAGE's, or a resident's own); null: made up
        public bool[] Knows;                                // whether the others know A, B and C by name (a tie, or the same home town); null: everyone (the tests); a stranger is "mister" or "ma'am" (night 2026-10-28, item 10)
        public int[] Warmth;                                // the tie's warmth between A and B, A and C, B and C (-3..3; 0 none), for the reactions' appraisal (after item 14); null: unknown
        public bool[] Tied;                                 // whether A and B, A and C, B and C have a tie at all

        /// The warmth between two of the cast (0 when unknown), and whether they have a tie.
        public int WarmthBetween(int i, int j, out bool tied)
        {
            tied = false;
            if (Warmth == null || i == j)
                return 0;
            int k = (i == 0 && j == 1 || i == 1 && j == 0) ? 0 : (i == 0 && j == 2 || i == 2 && j == 0) ? 1 : 2;
            tied = Tied != null && k < Tied.Length && Tied[k];
            return k < Warmth.Length ? Warmth[k] : 0;
        }
        public string[] Tempers = new string[3];            // A, B and C's temperaments (Reactions.Listener); null: none known
        public bool[] WellOff;                              // A and B dressed like money (the money argument, Core/MoneyTalk.cs); null: unknown
        public string[] Wars;                               // A's and B's war ("blue", "grey", "cuba", "islands"; Core/OldSoldiers.cs, FOURTEENHOUR item 17); null: unknown
        public bool KinStands;                              // the temperance door: A is her kin, B the drinker, C the churchwoman (item 25)
        public bool SecondChurch;                           // the temperance door: the cast's third is a second woman of the church, not any woman near (the review of items 22-26)
        public bool WifeHome;                               // the temperance door: the cast's third is the drinker's own wife (or his sweetheart), near, who walks him home (DEPTH item 5)
        public string KinId;                                // ...and her kin's id, for the sides
        public int PledgesThisWeek;                         // pledges signed at this town's saloon door this week, for the preacher (item 25)
        public string[] TalkOf;                             // a talk of someone not there: his name, A's view, B's view or null, "w" for a woman (item 23); null: none
        public string Courted;                              // the woman two rivals both court, her first name (Core/Rivals.cs, FOURTEENHOUR item 19); null: none
        public string[] Groups;                             // A's and B's trade group (InnerLife.TradeGroup), for who hears a war story how; null: unknown
        public int MindLead;                                // a chance meeting the minds chose: whose side a cold shoulder or a loan is on (0 A, 1 B; Scenes.Minds.cs)
        public string MindWorry;                            // ... and the asker's money worry (debt, bank, savings, cards)
        public string WakeLine;                             // a stage-four person's line that wakes (Core/WakeLines.cs), said as a talk closes
        public int WakeLineBy = -1;                         // ...by A (0) or B (1); -1 nobody
        public bool Desperate;                              // the holdup's robber at the worst of his money trouble: hardest to talk down (Core/Holdup.cs, FOURTEENHOUR reserve R1)
        public int Friend = -1;                             // the quarrel ladder's friend at hand, the cast's third: 0 a friend of A, 1 of B, -1 nobody (Scenes.Ladder.cs)
    }

    public static partial class Scenes
    {
        private static readonly string[] Men = { "Hank", "Silas", "Amos", "Clem", "Jed", "Earl", "Walt", "Otis", "Lyle", "Abner", "Virgil", "Rufus", "Elmer", "Cyrus", "Ezra", "Wade", "Floyd", "Homer" };
        private static readonly string[] Women = { "Martha", "Ada", "Clara", "Ruth", "Hattie", "Lottie", "Mabel", "Nell", "Pearl", "Ida", "Etta", "Della", "Josie", "Minnie", "Opal", "Cora" };

        public static string FirstName(bool woman, string seed)
        {
            return woman ? Voice.Pick(seed + "fw", Women) : Voice.Pick(seed + "fm", Men);
        }

        /// Whether a word is one of the first names scenes give strangers (the language layer's check).
        public static bool IsWomansName(string w)
        {
            return w != null && Array.IndexOf(Women, w) >= 0;
        }

        internal static bool IsName(string w)
        {
            return Array.IndexOf(Men, w) >= 0 || Array.IndexOf(Women, w) >= 0;
        }

        /// Chooses a scene that fits the cast and the place. Null when nothing fits.
        /// A weight scaled by the trouble weight; at least 1 while the weight isn't 0 (so it never drops out at a low setting).
        private static int Weighted(int w, float weight)
        {
            return weight <= 0f ? 0 : Math.Max(1, (int)Math.Round(w * weight));
        }

        /// The trouble weight an argument's share takes: all of it, or (ArgueCapped) at most 1.5, so a high weight brings blows,
        /// not a street of arguments.
        public static float ArgueWeight(SceneCast c)
        {
            return c.ArgueCapped ? Math.Min(c.Trouble, 1.5f) : c.Trouble;
        }

        /// The kinds that are only talk, with nothing happening (the no-model run, Lex 2026-11-01): passed over with [Scenes] EventfulOnly.
        public static readonly string[] JustTalkKinds = { "smalltalk", "gossip", "song", "talltale", "nighttale", "paper", "pitch", "haggle", "news", "posse", "round", "avoid", "cheer", "glad" };   // not "mindtalk": EventfulOnly is on as built, and the talks from people's minds are wanted (Lex, 2026-11-03; MindTalkTest holds it)

        public static bool JustTalk(string kind)
        {
            return kind != null && Array.IndexOf(JustTalkKinds, kind) >= 0;
        }

        public static Scene Choose(SceneCast c, string seed, bool allowFight)
        {
            List<KeyValuePair<string, int>> w = ChanceWeights(c, allowFight);   // the kinds this cast and place allow (Scenes.Minds.cs, where the minds lean them)
            if (w.Count == 0)
                return null;
            int total = w.Sum(x => x.Value);
            int roll = Voice.Hash(seed + "kind") % Math.Max(1, total);
            string kind = w[w.Count - 1].Key;
            foreach (KeyValuePair<string, int> x in w)
            {
                if (roll < x.Value)
                {
                    kind = x.Key;
                    break;
                }
                roll -= x.Value;
            }
            return Make(kind, c, seed);
        }

        public static Scene Make(string kind, SceneCast c, string seed)
        {
            Scene s = MakeInner(kind, c, seed);
            if (s != null && c != null && c.WakeLine != null && c.WakeLineBy >= 0 && c.WakeLineBy <= 1 && (kind == "hosts" || kind == "smalltalk" || kind == "gossip"))
            {
                // a line that wakes (Core/WakeLines.cs; SIXDAY 12c): said as the talk closes, and taken in
                s.Add(c.WakeLineBy, c.WakeLine, null, null, 500);
                s.Add(1 - c.WakeLineBy, WakeLines.Heard(seed + "|wl"), null, "cb:wakeline", 500);   // the hearing is booked as this plays (the game side's CustomScene), not at the open
            }
            return s;
        }

        private static Scene MakeInner(string kind, SceneCast c, string seed)
        {
            switch (kind)
            {
                case "flirt": return Flirt(c, seed);
                case "argue": return Argue(c, seed);
                case "brawl": return Brawl(c, seed);
                case "rematch": return Rematch.AgainScene(c, seed);   // the beaten man's plan (Core/Rematch.cs): a brawl of its own words
                case "shake": return Rematch.ShakeScene(c, seed);     // his hand offered, taken or refused
                case "temperance": return Temperance.Make(c, seed);   // the pledge put to a drinker (Core/Temperance.cs)
                case "suffrage": return Suffrage.Make(c, seed);   // the vote, put to a man in the street at Saint Denis (Core/Suffrage.cs)
                case "scrip": return ScripTalk.Make(c, seed);   // paid in the company's paper, at Annesburg (Core/ScripTalk.cs)
                case "money": return MoneyTalk.Make(c, seed, Voice.NowYear);   // gold against silver, or the banks in 1907 (Core/MoneyTalk.cs)
                case "wages": return c.AWoman != c.BWoman ? Payday.Wages(c, seed) : null;   // payday: a woman meets a man for his pay (Core/Payday.cs)
                case "propose": return Propose(c, seed);
                case "cheat": return Cheat(c, seed);
                case "debt": return Debt(c, seed);
                case "gossip": return c.Rumours.Count > 0 ? Gossip(c, seed) : Argue(c, seed);
                case "hosts": return HostTalk(c, seed);
                case "jealous": return Jealous(c, seed);
                case "brushoff": return Brushoff(c, seed, c.MindLead);   // the minds' own kinds (Scenes.Minds.cs), never rolled
                case "favour": return AskFavour(c, seed, c.MindLead, c.MindWorry);
                default: return MakeFrontier(kind, c, seed);
            }
        }

        // the man is A unless A is the woman of a mixed pair. The name they really go by, when they have one, so the
        // lines say the same name as the label over them; else one made up from the seed (as the director does)
        internal static string N(SceneCast c, int who, string seed)
        {
            bool woman = who == 0 ? c.AWoman : who == 1 ? c.BWoman : c.CWoman;
            if (c.Knows != null && who < c.Knows.Length && !c.Knows[who])
                return woman ? "ma'am" : "mister";   // strangers weren't on first-name terms in 1899 (item 10)
            string real = c.Names != null && who < c.Names.Length ? c.Names[who] : null;
            if (!string.IsNullOrEmpty(real))
                return real;
            return FirstName(woman, seed + who);
        }

        /// The same person spoken of to a third ("leave the lady be"): the name when known, else by their sex.
        internal static string NThird(SceneCast c, int who, string seed)
        {
            bool woman = who == 0 ? c.AWoman : who == 1 ? c.BWoman : c.CWoman;
            if (c.Knows != null && who < c.Knows.Length && !c.Knows[who])
                return woman ? "the lady" : "the man";
            return N(c, who, seed);
        }

        public static Scene Flirt(SceneCast c, string seed)
        {
            int man = c.AWoman ? 1 : 0, woman = 1 - man;
            string he = N(c, man, seed), she = N(c, woman, seed);
            Scene s = new Scene { Kind = "flirt", Summary = "a couple courting in the street" };
            switch (Voice.Hash(seed) % 5)
            {
                case 0:
                    s.Add(man, "Evening, Miss. You look like trouble.", "greet", "face").Add(woman, "And you look like a man who'd enjoy some.")
                     .Add(man, "Walk with me a spell?").Add(woman, "Maybe. If you behave.", "yes");
                    break;
                case 1:
                    s.Add(woman, "You've been staring, " + he + ".", null, "face").Add(man, "Can't help it. You're the prettiest thing in " + (c.Town ?? "this county") + ".")
                     .Add(woman, "That ain't saying much.").Add(man, "Then let me say it somewhere nicer.").Add(woman, "...Supper. You're paying.", "yes");
                    break;
                case 2:
                    s.Add(man, "I picked these for you, " + she + ".", "greet", "face").Add(woman, "Weeds, " + he + ". You picked me weeds.")
                     .Add(man, "Pretty weeds.").Add(woman, "...They are, at that.");
                    break;
                case 3:
                    s.Add(woman, c.Saloon ? "Buy a lady a drink?" : "Carry these for a lady?", null, "face").Add(man, c.Saloon ? "I'll buy her two, if she laughs at my jokes." : "Carry 'em to the end of the world, ma'am.")
                     .Add(woman, c.Saloon ? "Depends on the jokes." : "Just to the end of the street will do.").Add(man, "A start's a start.", "yes");
                    break;
                default:
                    s.Add(man, "Still mad at me, " + she + "?", null, "face").Add(woman, "Furious.").Add(man, "Furious enough to dance with me Saturday?")
                     .Add(woman, "...Wear a clean shirt this time.", "yes");
                    break;
            }
            return s;
        }

        // The street's everyday quarrels (Argue): who|voice|act|line, a "walkaway:" beat for the one walking off. All our own words.
        private static readonly string[][] Arguments =
        {
            new[] { "0|mild|face|You owe me an apology, {b}.", "1|||I owe you not one thing.", "0|angry||You told the whole street my business!", "1|angry||Then keep your business quieter!", "0|||One of these days...", "1|stare||One of these days what?", "0|||walkaway:" },
            new[] { "0|mild|face|Where's my two dollars, {b}?", "1|||I said Friday.", "0|angry||It IS Friday!", "1|||...Next Friday.", "0|angry||You're a weasel, {b}. A weasel in a hat.", "1|||walkaway:" },
            new[] { "0|angry|face|I saw you with my {spouse}.", "1|||We were talking.", "0|angry||Talk with your hands a lot, do you?", "1|||Ask {spouseobj} yourself, if you're so sure.", "0|angry|walkaway|I will!" },
            new[] { "0|mild|face|Your dog dug up my whole garden.", "1|||Ain't my dog.", "0|||It follows you home every night!", "1|no||Lots of things follow me home. Don't make 'em mine.", "0|angry|walkaway|Insufferable." },
            new[] { "0||face|You said this yesterday.", "1|||I never said it before in my life.", "0|angry||Word for word, {b}! Same words!", "1|mild||...You feeling all right?", "0||walkaway|No. No, I don't think I am." },
            new[] { "0|mild|face|That's my spot.", "1|||Didn't see it marked as yours.", "0|angry||You'll see my boot on you in a minute.", "1|threat||Try it.", "0|mild|walkaway|...Not worth it." },
            new[] { "0|angry|face|You turned the ditch onto your side again, {b}.", "1|||My corn's thirsty too.", "0|angry||Then let it go thirsty on its own side!", "1|stare||Say that at the water meeting.", "0|angry|walkaway|I will, and louder." },
            new[] { "0|mild|face|I'd like my saw back, {b}.", "1|||What saw?", "0|angry||The one in your barn with my mark burned on the handle.", "1|mild||...I meant to bring it Sunday.", "0|angry||Two Sundays have come and gone.", "1|||walkaway:" },
            new[] { "0|angry|face|You moved that piece when I looked away.", "1|||I did no such thing.", "0|||Eleven years I've played you. I know your cheating face.", "1|stare||Then why do you keep sitting down with me?", "0|mild|walkaway|...Nobody else will." },
            new[] { "0|angry|face|Your horse bit mine at the rail, {b}.", "1|||Your horse looked at him wrong.", "0|||A horse can't look wrong at another horse.", "1|no||Ask mine.", "0|angry|walkaway|There's no talking to you." },
            new[] { "0|angry|face|You weighed me two pounds and gave me one.", "1|||The scale says two.", "0|angry||Then your scale lies like its owner.", "1|threat||Take your trade elsewhere.", "0|angry|walkaway|There is no elsewhere! That's the whole trouble!" },
            new[] { "0|angry|face|Did you tell the church I'd been drinking?", "1|||I told one person.", "0|angry||In this town one person is the church!", "1|no||Then don't drink.", "0|angry|walkaway|Of all the..." },
            new[] { "0|angry|face|Your hogs were in my potatoes again.", "1|||Fence is yours to mend.", "0|angry||It stood fine till your hogs leaned on it!", "1|no||Hogs lean. It's their nature.", "0|threat|walkaway|Then it's their nature to end up bacon." },
            new[] { "0|angry|face|Your people put that fool in for sheriff.", "1|||Better a fool than a thief.", "0|angry||He's my cousin!", "1|stare||I know. That's why I said thief.", "0|angry||walkaway:" },
            new[] { "0|mild|face|Your fiddle kept the whole street up, {b}.", "1|||That was music.", "0|||That was a cat in a flour sack.", "1|no||You've no ear, that's your trouble.", "0|angry|walkaway|I've two, and they're both sore." },
            new[] { "0|angry|face|You took my good sheet off the line.", "1|||It's mine.", "0|angry||Mine has the blue thread in the corner!", "1|mild||...So it has.", "0|mild|walkaway|Bring it round tonight, then." },
            new[] { "0|angry|face|My father fought at Shiloh while yours sat home.", "1|||My father had one leg.", "0|||He had it before the war and after.", "1|threat||Leave my father out of it.", "0|mild|walkaway|...Fine. Fine." },
            new[] { "0|mild|face|Where's the almanac I lent you?", "1|||Lent it to my brother.", "0|angry||It wasn't yours to lend!", "1|||He's family. That makes it half his.", "0|angry|walkaway|That makes no sense at all." },
            new[] { "0|angry|face|You spat on my boot, {b}.", "1|||The wind took it.", "0|angry||There's no wind!", "1|no||There was a little.", "0|threat|walkaway|Next time I'll take it as meant." },
            new[] { "0|angry|face|You still owe me two days' hauling.", "1|||You broke my wheel.", "0|angry||That wheel was rotten when you handed it me!", "1|||Then haul with a better one.", "0|angry|walkaway|You'll pay, {b}. One way or the other." },
            new[] { "0|angry|face|I staked that claim in March, {b}.", "1|||Your stake fell over.", "0|angry||You kicked it over!", "1|stare||Prove it.", "0|angry|walkaway|At the land office, I will." },
            new[] { "0|angry|face|Your boy blacked my boy's eye.", "1|||Your boy called him a name first.", "0|||What name?", "1|||I'll not say it in the street.", "0|mild|walkaway|...That does sound like my boy." },
            new[] { "0|mild|face|Rent was Saturday, {b}.", "1|||And I'll have it Saturday.", "0|angry||LAST Saturday!", "1|mild||Was it? Time does get on.", "0|threat|walkaway|So will your trunk, out the window." },
            new[] { "0|angry|face|You've been drawing water from my well.", "1|||It's the town's well.", "0|angry||It's on my land!", "1|no||The water was here before your land was.", "0|angry|walkaway|That isn't how land works!" },
        };

        public static Scene Argue(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "argue", Summary = "a shouting match in the street" };
            // the street's quarrels, picked by the seed (FIVEDAY item 6: six of them became twenty-four; the ride test
            // had the same argument heard again in a third of an hour's scenes). Each beat is who|voice|act|line; a "walkaway:"
            // beat with no words is the one walking off.
            string[] script = Arguments[(Voice.Hash(seed) & 0x7fffffff) % Arguments.Length];
            string spouse = c.AWoman ? "husband" : "wife", spouseObj = c.AWoman ? "him" : "her";   // the speaker's own spouse (the review of Part 2)
            foreach (string beat in script)
            {
                string[] p = beat.Split(new[] { '|' }, 4);
                int who = p[0] == "1" ? 1 : 0;
                string voice = p[1].Length > 0 ? p[1] : null, act = p[2].Length > 0 ? p[2] : null, line = p[3];
                if (line == "walkaway:")
                {
                    s.Add(who, null, voice, "walkaway");
                    continue;
                }
                s.Add(who, line.Replace("{a}", a).Replace("{b}", b).Replace("{spouse}", spouse).Replace("{spouseobj}", spouseObj), voice, act);
            }
            return s;
        }

        // A brawl's openings: who speaks first ("0" A, "1" B), the line, the other's answer, and the first one's last word as
        // the blows start ("" for none). All our own words.
        private static readonly string[][] BrawlOpenings =
        {
            new[] { "0", "Once more, {b}. Say it once more.", "I said your mother was a mule and your father knew it.", "" },
            new[] { "0", "You've been cheating me on the weights all year.", "Prove it.", "I'll prove it on your face!" },
            new[] { "1", "Get out of my way.", "Make me.", "Gladly." },
            new[] { "0", "That was my seat, {b}.", "Didn't see you sitting in it.", "You'll see it on my knuckles!" },
            new[] { "0", "You been talking about me, {b}?", "Only the true parts.", "Then here's something true!" },
            new[] { "1", "Step on my boot again, {a}.", "Or what?", "Or this!" },
            new[] { "0", "That's my money on the table, {b}.", "It was. Now it's mine.", "Like hell it is!" },
            new[] { "0", "Keep clear of my sister, {b}.", "Your sister can choose for herself.", "She's chosen. Now you choose this!" },
            new[] { "1", "You drink like a Methodist, {a}.", "And you fight like one, I'd wager.", "Let's find out!" },
            new[] { "0", "Somebody cut my fence, {b}, and it was you.", "You know nothing.", "I know enough!" },
            new[] { "0", "Laugh at me again, {b}. Go on.", "Ha.", "" },
            new[] { "1", "You owe me two dollars since Easter, {a}.", "And you'll wait till the next one.", "No, I won't!" },
        };

        private static readonly string[] BrawlWon = { "Stay down.", "That's for what you said.", "Next time I won't be so gentle.", "Anybody else?", "Now we understand each other.", "Get up and I'll put you down again.", "Remember that, next time you open your mouth.", "Somebody scrape that off the street." };
        private static readonly string[] BrawlLost = { "Alright! Alright...", "This ain't over.", "Lucky punch.", "My tooth. You took my tooth.", "Leave off, leave off...", "You'll keep.", "I slipped. You saw me slip.", "Help me up, somebody." };

        public static Scene Brawl(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "brawl", Fight = true, Summary = !c.AWoman && !c.BWoman ? "two men came to blows" : c.AWoman && c.BWoman ? "two women came to blows" : "a man and a woman came to blows" };
            // the opening words: who speaks first, then the answer, then the one who swings (A or B) says the last word as
            // the blows start (FIVEDAY item 3: three of these became twelve, so two fights in an hour seldom open alike)
            string[][] open = BrawlOpenings;
            string[] o = open[(Voice.Hash(seed) & 0x7fffffff) % open.Length];
            int first = o[0] == "1" ? 1 : 0;
            s.Add(first, o[1].Replace("{a}", a).Replace("{b}", b), "angry", "face").Add(1 - first, o[2].Replace("{a}", a).Replace("{b}", b), "threat")
             .Add(first, o[3].Length > 0 ? o[3].Replace("{a}", a).Replace("{b}", b) : null, "angry", "fight", 9000);
            s.Add(0, null, null, "endfight").Add(0, Voice.Pick(seed + "w", BrawlWon), "won")
             .Add(1, Voice.Pick(seed + "l", BrawlLost), "mild", "walkaway");
            return s;
        }

        public static Scene Propose(SceneCast c, string seed)
        {
            int man = c.AWoman ? 1 : 0, woman = 1 - man;
            string he = N(c, man, seed), she = N(c, woman, seed);
            Scene s = new Scene { Kind = "propose", Summary = "a man asked a woman to marry him, right there in the street" };
            s.Add(man, she + "... I been carrying this ring around for a month.", null, "face").Add(man, null, null, "kneel", 1500)
             .Add(woman, he + ", get up, people are looking!").Add(man, "Let 'em look. Will you marry me?");
            if (Voice.Chance(seed + "yes", 75))
                s.Add(woman, "Yes. Yes, you fool!", "yes").Add(man, null, null, "stand").Add(man, "She said yes! You hear that? She said yes!", "won");
            else
                s.Add(woman, "Oh, " + he + ". I can't. I'm sorry.", "no", "walkaway").Add(man, null, null, "stand").Add(man, "...Right. Right.", null, "walkaway");
            return s;
        }

        public static Scene Jealous(SceneCast c, string seed)
        {
            // A courts B; C (B's partner) walks in
            string a = N(c, 0, seed), b = N(c, 1, seed), x = N(c, 2, seed);
            Scene s = new Scene { Kind = "jealous", Cast = 3, Summary = "a jealous " + (c.CWoman ? "wife" : "husband") + " caught somebody sweet-talking" };
            s.Add(0, "You've got the loveliest laugh in " + (c.Town ?? "the county") + ", " + b + ".", null, "face").Add(1, "Oh, stop.")
             .Add(2, "Get away from " + (c.BWoman ? "her" : "him") + ".", "angry", "approach").Add(0, "Who's asking?").Add(2, (c.BWoman ? "Her husband" : "His wife") + ".", "threat")
             .Add(1, x + ", it ain't what it looks like!", "scared").Add(2, null, null, "shove").Add(0, "...I was just leaving.", "scared", "walkaway");
            return s;
        }

        public static Scene Cheat(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "cheat", Summary = "a card cheat was called out" };
            s.Add(0, "That's the second ace of spades I've seen tonight, friend.", "mild", "face").Add(1, "Deck's got four aces.").Add(0, "Not up your sleeve, it don't.", "angry")
             .Add(1, "You calling me a cheat?", "threat").Add(0, "I'm calling you a liar, too.", "angry");
            if (!c.AWoman && !c.BWoman && Voice.Chance(seed + "f", 40))
            {
                s.Fight = true;
                s.Add(1, null, "angry", "fight", 8000).Add(0, null, null, "endfight").Add(0, "Keep your winnings. Keep your teeth, too, while you can.", "won", "walkaway");
            }
            else if (c.Saloon && Voice.Chance(seed + "out", 50))
            {
                // thrown out (Tier 4): in a saloon, half the time it's the cheat who goes, told to by the man he cheated
                s.Add(0, Voice.Pick(seed + "o1", "Out. And leave the pot you never won.", "Get out of this saloon. Now. Before the rest of the table hears of it.",
                                    "Walk out that door, " + b + ", or be carried."), "angry")
                 .Add(1, Voice.Pick(seed + "o2", "...Keep it. Crooked table anyhow.", "Fine. I know when I'm not wanted.", "This town's got no sense of humor."), "mild", "walkaway")
                 .Add(1, null, null, "cb:thrownout");   // the boardwalk after: his thought once he's outside (WestworldScript.Life.cs)
                s.Summary = "a card cheat was shown the door";
            }
            else
            {
                s.Add(1, "...Take your money and get out of my sight.", "mild").Add(0, "Gladly.", null, "walkaway");
            }
            return s;
        }

        /// The cheat on the boardwalk after he's been shown the door.
        public static string CheatOutside(string seed)
        {
            return Voice.Pick(seed + "|out", "Crimped the corners too deep. Amateur's work.", "Two aces of spades. Should've counted the deck.",
                              "One more saloon I can't go back to. Getting to be a short list.", "Cold out here. Colder with empty pockets.");
        }

        // The debt called in: the creditor's opening, the debtor's answer, the sum, the debtor's five; then how it ends
        // (who|voice|act|line). All our own words.
        private static readonly string[][] DebtOpenings =
        {
            new[] { "{b}. You know why I'm here.", "I need more time.", "You had time. {amount} dollars, {b}.", "I got five. Take five." },
            new[] { "There you are, {b}. I've looked all over town.", "I was coming to see you.", "Sure you were. {amount} dollars.", "Here's five. It's what I have." },
            new[] { "{b}. The note's come due.", "Harvest's late. Everybody's is.", "The note don't care about harvests. {amount} dollars.", "Five. I can manage five." },
            new[] { "Don't walk off, {b}. We've business.", "Not here. Not in the street.", "Here suits me fine. {amount} dollars.", "Five's all I carry. Here." },
            new[] { "{b}. You know what day this is.", "Payday? Already?", "{amount} dollars' worth of payday.", "Five. That's all there is in the house." },
            new[] { "{b}, I've been patient. That's over.", "Give me one more week.", "I gave you four. {amount} dollars.", "I'll give you five now. Every cent I've got." },
        };

        private static readonly string[][] DebtEndings =
        {
            new[] { "0|threat||Five now. The rest by Sunday. Or I take the mule.", "1|mild||...Sunday.", "0|||walkaway:" },
            new[] { "0|angry||Five dollars don't cover the interest.", "0||shove|", "1|scared||All right! Sunday! On my mother's life!" },
            new[] { "0|mild|walkaway|...Keep it. Buy your kids some shoes. But this is the last time, {b}.", "1|thanks||God bless you. God bless you." },
            new[] { "0|threat||Five's a start. Saturday for the rest, and I'll bring the deputy.", "1|mild||Saturday.", "0|||walkaway:" },
            new[] { "0|angry||I'll take the five, and your saddle with it.", "1|scared||Not the saddle! It was my father's!", "0|angry|walkaway|Then find the money." },
            new[] { "0|mild||Five. And you'll work off the rest at my place, Mondays.", "1|mild||...Mondays. All right.", "0|||walkaway:" },
            new[] { "0|angry||Five, then. But the whole town hears what you are.", "1|scared||Please. Don't do that.", "0|||walkaway:" },
            new[] { "0|mild||Five now, five a week, till it's done. Shake on it.", "1|thanks||Done. Thank you. I mean it.", "0|||walkaway:" },
        };

        public static Scene Debt(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            int amount = 10 + Voice.Hash(seed + "amt") % 8 * 5;
            Scene s = new Scene { Kind = "debt", Doer = 0, Amount = amount, Summary = "a man called in a debt of " + amount + " dollars" };   // A calls it in
            // the calling in (FIVEDAY item 6: one opening became six, three endings eight; the creditor is A, and the debtor
            // always offers five, which every ending answers)
            string[] o = DebtOpenings[(Voice.Hash(seed + "open") & 0x7fffffff) % DebtOpenings.Length];
            Func<string, string> fill = x => x.Replace("{b}", b).Replace("{a}", a).Replace("{amount}", Grammar.UpperFirst(Voice.Number(amount)));
            s.Add(0, fill(o[0]), "mild", "approach").Add(1, fill(o[1]), "scared").Add(0, fill(o[2])).Add(1, fill(o[3]), "scared");
            foreach (string beat in DebtEndings[(Voice.Hash(seed) & 0x7fffffff) % DebtEndings.Length])
            {
                string[] p = beat.Split(new[] { '|' }, 4);
                int who = p[0] == "1" ? 1 : 0;
                string voice = p[1].Length > 0 ? p[1] : null, act = p[2].Length > 0 ? p[2] : null, line = p[3];
                if (line == "walkaway:")
                    s.Add(who, null, voice, "walkaway");
                else
                    s.Add(who, line.Length > 0 ? fill(line) : null, voice, act);
            }
            return s;
        }

        /// The debt scene's end when the stranger covers what's owed, said to the creditor through Talk (Core/Fondness.cs):
        /// the creditor's answer is said by the menu; here the creditor walks off paid, and the debtor turns to the stranger
        /// with their thanks and goes on their way. A's and B's places are the debt scene's (A the creditor).
        public static Scene DebtCovered(SceneCast c, string seed, string thanks)
        {
            Scene s = new Scene { Kind = "debt", Doer = 0, Summary = "a stranger paid a man's debt for him in the street" };
            s.Add(0, null, null, "walkaway");
            s.Add(1, thanks, "thanks", "faceplayer", 800);
            s.Add(1, null, null, "walkaway");
            return s;
        }

        public static Scene Gossip(SceneCast c, string seed)
        {
            string rumour = c.Rumours[Voice.Hash(seed + "r") % c.Rumours.Count];
            List<string> lines = Deeds.Gossip(rumour, c.Town, seed);
            Scene s = new Scene { Kind = "gossip", Summary = "town gossip" };
            for (int i = 0; i < lines.Count; i++)
                s.Add(i % 2, lines[i], i == 0 ? null : (i == 1 ? "yes" : null), i == 0 ? "face" : null);
            return s;
        }

        /// The clock turned back lately (a save loaded from before; Core/Ghosts.cs): the waking open with the morning that happened twice.
        public static bool WentBackLately;

        public static Scene HostTalk(SceneCast c, string seed)
        {
            if (HostNotes.On() && Voice.Chance(seed + "|notes", HostNotes.Share))
                return HostNotes.Make(seed);   // half the time they test the world together (Core/HostNotes.cs; FIVEDAY R28)
            Scene s = new Scene { Kind = "hosts", Summary = "two people talking strangely about the world" };
            bool first = true;
            if (WentBackLately)
            {
                string[] twice = Ghosts.TwiceLines(seed);
                s.Add(0, twice[0], null, "face", 500);
                s.Add(1, twice[1], null, null, 500);
                first = false;
            }
            foreach (KeyValuePair<int, string> l in Hosts.Conversation(seed))
            {
                s.Add(l.Key, l.Value, null, first ? "face" : null, 500);
                first = false;
            }
            return s;
        }

        // ---- ceremonies -------------------------------------------------------------------------------

        public static List<KeyValuePair<string, string>> Vows(Wedding w, string seed)
        {
            string a = Person.FirstOf(w.AName), b = Person.FirstOf(w.BName);
            string partnerA = w.AWoman ? "wife" : "husband", partnerB = w.BWoman ? "wife" : "husband";
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Preacher", "Dearly beloved, we are gathered here in the sight of God to join this man and this woman.".Replace("this man and this woman", w.AWoman == w.BWoman ? "these two souls" : "this man and this woman")),
                new KeyValuePair<string, string>("Preacher", "Do you, " + a + ", take " + b + " to be your lawful wedded " + partnerB + "?"),
                new KeyValuePair<string, string>(w.AName, Voice.Pick(seed + "a", "I do.", "I do. Lord help me, I do.", "I surely do.")),
                new KeyValuePair<string, string>("Preacher", "And do you, " + b + ", take " + a + " to be your lawful wedded " + partnerA + "?"),
                new KeyValuePair<string, string>(w.BName, Voice.Pick(seed + "b", "I do.", "...I do.", "I do, and about time.")),
                new KeyValuePair<string, string>("Preacher", "Then by the power vested in me, I pronounce you joined. You may kiss."),
            };
        }

        public static List<string> Eulogy(string deadName, Person dead, bool killedByPlayer, bool playerHere, string seed)
        {
            string first = Person.FirstOf(deadName ?? "him");
            bool woman = dead != null && dead.Woman;
            string he = woman ? "she" : "he", his = woman ? "her" : "his", him = woman ? "her" : "him";
            List<string> lines = new List<string>();
            lines.Add(Voice.Pick(seed + "1", first + " wasn't a good " + (woman ? "woman" : "man") + ". But " + he + " was ours.",
                "Ashes to ashes. The rest is up to God.", "Rest easy, " + first + ". Nobody can hurt you now."));
            PastItem past = dead != null ? dead.Past.Where(p => p.Public && !string.IsNullOrEmpty(p.Text) && p.Kind != "born" && Grammar.StartsWithPastVerb(p.Text))
                .OrderBy(p => Voice.Hash(seed + p.Text)).FirstOrDefault() : null;
            if (past != null)
            {
                string deed = Grammar.LowerFirst(past.Text.Trim().TrimEnd('.'));
                lines.Add(Voice.Pick(seed + "2", "I'll always remember the time " + he + " " + deed + ". That was " + first + ".",
                    "Folks forget " + he + " was young once. " + Grammar.UpperFirst(he) + " " + deed + ", you know."));
            }
            else
            {
                lines.Add(Voice.Pick(seed + "2", first + " taught me to shoot. Said everybody ought to know how.", "Never once saw " + him + " turn down a friend in need."));
            }
            if (killedByPlayer && playerHere)
                lines.Add(Voice.Pick(seed + "3", "...That's him. Over there. The one who did it.", "Have you no shame, coming here?", "Look who came to watch us bury " + him + "."));
            else if (killedByPlayer)
                lines.Add("Whoever did this... I'll find " + Voice.Pick(seed + "4", "him", "them") + ". On " + first + "'s grave, I swear it.");
            else
                lines.Add(Voice.Pick(seed + "5", "Goodbye, " + first + ".", "Save " + his + " seat for me, Lord. I won't be long."));
            return lines;
        }
    }
}
