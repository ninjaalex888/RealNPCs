// The game's own encounters, noticed (FOURTEENHOUR reserve R8; TWELVEHOUR item 4; [World] GameEncounters). The game's random events
// (a snakebite, a coach robbery, a prison wagon, a horse race: about sixty kinds over 119 models of their own, every one named re_...)
// are the game's, and enRAGE leaves them alone by that prefix; the control census (item 22) already counts them. Now RealNPCs gives
// them a place in its world without touching them: read by the model's name, the kind is looked up in a table of our own short
// phrases (never the game's lines), and when one plays out near you, a person the mind knows who saw it keeps it in mind and thinks
// of it, and the nearest town has it in its talk the next day, with what you did (stopped, or rode on). Some kinds are left out on
// purpose (the camp's own visitors, the lewd, the game's story people, the ones too grim to make talk of); the table says why.
// Why it reads true: the game's own encounters are the most memorable things on its roads, and until now nobody in RealNPCs' world
// had ever heard of them. Plain C#, no game; all words this add-on's.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class GameEncounters
    {
        public static bool On = true;   // [World] GameEncounters, from the settings
        public const float SeenMetres = 30f;
        public const int WatchMs = 60000, StayedMs = 40000;   // watched a minute; you near it this long is "stopped"

        /// The kind in a re_ model's name ("re_coachrobbery_males_01" is "coachrobbery"), or null for another model.
        public static string KindOf(string model)
        {
            if (model == null || !model.StartsWith("re_", System.StringComparison.Ordinal))
                return null;
            string k = model.Substring(3);
            foreach (string tail in new[] { "_males_01", "_females_01", "_teens_01", "_dogs_01", "_01" })
                if (k.EndsWith(tail, System.StringComparison.Ordinal))
                    return k.Substring(0, k.Length - tail.Length);
            return k;
        }

        /// The kind in our own words, for the talk and a witness's thought: a short phrase without its place. Null: left out.
        public static readonly Dictionary<string, string> Phrases = new Dictionary<string, string>
        {
            { "snakebite", "a man bitten by a snake" }, { "coachrobbery", "a coach held up" }, { "prisonwagon", "a barred wagon of prisoners" },
            { "horserace", "a horse race" }, { "townrobbery", "a store robbed in broad day" }, { "street_fight", "a fight in the street" },
            { "executions", "men about to shoot a man on his knees" }, { "publichanging", "a hanging" }, { "duelwinner", "a duel, one man up and one down" },
            { "duelboaster", "a man bragging he'd outdraw anyone" }, { "drunkdueler", "a drunk calling men out to draw" }, { "lostfriend", "a fellow hunting for his missing partner" },
            { "lostman", "a man lost out there" }, { "lostdrunk", "someone lost in drink, a long way from home" }, { "injuredrider", "a rider hurt and his horse gone" },
            { "strandedrider", "a rider stranded" }, { "animalattack", "someone set on by a wild animal" }, { "animalmauling", "a fellow torn up by a beast" },
            { "beartrap", "a fellow whose leg the iron jaws had hold of" }, { "fleeingfamily", "a family running from something" }, { "kidnappedvictim", "a woman carried off against her will" },
            { "hostagerescue", "men holding a hostage" }, { "trappedwoman", "a woman trapped and calling for help" }, { "crashedwagon", "a wagon wrecked" },
            { "wagonthreat", "a wagon set on by men" }, { "footrobbery", "a man robbed on foot" }, { "darkalleyambush", "a man set on in an alley" },
            { "darkalleystabbing", "a stabbing in an alley" }, { "slumambush", "an ambush in the slums" }, { "parlorambush", "an ambush in a parlor house" },
            { "pickpocket", "a pickpocket at work" }, { "policechase", "the police after a man" }, { "ontherun", "a fugitive running from the law" },
            { "goldpanner", "a prospector working a creek for color" }, { "treasurehunter", "a man after buried treasure" }, { "rowdydrunks", "drunks rowdy on the road" },
            { "townburial", "a burial" }, { "townwidow", "a widow in black, grieving" }, { "domesticdispute", "a man and wife at it, out in the open" },
            { "townconfrontation", "a row in the street" }, { "wealthycouple", "a well-to-do couple in trouble" }, { "disabledbeggar", "a lame man begging" },
            { "fundraiser", "a woman collecting for a cause" }, { "corpsecart", "a cart of the dead" }, { "deadbodies", "bodies by the road" },
            { "burningbodies", "bodies burning" }, { "washedashore", "a man washed up by the water" }, { "frozentodeath", "someone frozen to death" },
            { "drownmurder", "a drowning that was no accident" }, { "poisoned", "a man poisoned" }, { "moonshinecamp", "a moonshiners' camp" },
            { "drunkcamp", "a camp of drunks" }, { "checkpoint", "men stopping everyone on the road" }, { "trainholdup", "a train held up" },
            { "boatattack", "a boat attacked" }, { "savagefight", "hill folk fighting" }, { "savagewarning", "hill folk warning travellers off" },
            { "savageaftermath", "what the hill folk left behind" }, { "savagewagon", "a wagon set on by hill folk" }, { "laramiegangrustling", "rustlers driving off cattle" },
            { "lostdog", "a lost dog" }, { "skippingstones", "boys skipping stones on the water" }, { "sharpshooter", "a trick shooter showing off" },
            { "showoff", "a man showing off" }, { "taunting", "men jeering at a stranger" }, { "stalkinghunter", "a hunter stalking somebody" },
            { "friendlyoutdoorsman", "a friendly trapper" }, { "ratinfestation", "a house overrun with rats" }, { "rally", "a political rally" },
            { "rallysetup", "a political rally being set up" }, { "rallydispute", "a row at a political rally" }, { "wildman", "a wild man of the woods" },
            { "torturingcaptive", "men torturing a captive" }, { "outlawlooter", "an outlaw looting the dead" }, { "loneprisoner", "a prisoner left alone" },
            { "inbredkidnap", "a man carried off by hill folk" }, { "murdercamp", "a camp where men were killed" }, { "darkalleybum", "a bum in an alley" },
            { "escort", "a woman asking to be seen home safe" },
        };

        /// Left out on purpose, and why: the player's camp's own visitors, the lewd, the game's story people, the too grim to talk of.
        public static readonly Dictionary<string, string> LeftOut = new Dictionary<string, string>
        {
            { "playercampstrangers", "the player's camp's own visitors: not the town's to talk of" },
            { "approach", "a stranger's approach to the player: his business" }, { "voice", "a voice only: nothing seen" },
            { "consequence", "the game's own reckoning with the player: his business" }, { "fussarchase", "the game's story people" },
            { "pisspot", "lewd" }, { "peepingtom", "lewd" }, { "nakedswimmer", "lewd" }, { "deadjohn", "lewd" },
            { "murdersuicide", "too grim for the town's talk" },
        };

        public static string PhraseOf(string kind)
        {
            string p;
            return kind != null && Phrases.TryGetValue(kind, out p) ? p : null;
        }

        /// A witness's thought, with the phrase.
        public static string Witness(string phrase, string seed)
        {
            return Voice.Pick(seed + "|wt", "Did you see that? " + Grammar.UpperFirst(phrase) + ", right there.", Grammar.UpperFirst(phrase) + ", out in the open. What's this country coming to.",
                                            "I'll be telling this one for a week. " + Grammar.UpperFirst(phrase) + ".");
        }

        /// The nearest town's talk the next day, with what you did: stayed by it, or rode on.
        public static string News(string phrase, string town, bool stayed, string seed)
        {
            string where = town != null ? " near " + town : " on some lonely road";
            string you = stayed ? Voice.Pick(seed + "|ys", ", and a stranger stopped to see to it", ", and a stranger stood by till it was done") : Voice.Pick(seed + "|yr", ", and a stranger rode straight past", "");
            return Voice.Pick(seed + "|nw", "Word is there was " + phrase + where + " yesterday" + you + ".", "Folks are talking of " + phrase + where + you + ".");
        }
    }
}
