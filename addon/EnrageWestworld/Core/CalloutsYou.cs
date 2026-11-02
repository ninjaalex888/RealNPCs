// Called out yourself ([Encounters] CalloutsArthur; Lex, 2026-11-01: "callouts aimed at Arthur"). By day in a town, out of
// doors: one of the named people near with something against you (you struck or robbed them, they saw you kill, word of your
// thieving reached them, enRAGE's own hate, or your dealings soured) walks over and says it to your face. Walk off and
// they call you a coward after you; stay, and they put up their fists (a hothead with a gun may draw instead: [Encounters]
// CalloutsArthurDraw); hold a gun on them and they back off with their hands up. Who, what about and how it ends are
// worked out here; the game side (WestworldScript.CalloutYou.cs) plays it. All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class CalloutsYou
    {
        /// Deeds a grievance can come from, newest first wins; how far back each counts, in game minutes.
        private const long PersonalDays = 30 * 1440, SeenDays = 10 * 1440;

        private static readonly HashSet<string> Killing = new HashSet<string> { "kill", "murder", "lawkill" };
        private static readonly HashSet<string> Stealing = new HashSet<string> { "theft", "robbery", "horsetheft", "looting" };

        /// What they have against you, or null: nothing, or not enough to walk up and say it. In order: done to them
        /// (struck: hurt; robbed), a killing they saw, word of your thieving in their own town, enRAGE's hate, soured dealings.
        public static string MatterOf(string id, string name, string temper, bool drunk, string home, Person p, PersonState st, IList<Deed> deeds, long now)
        {
            bool coward = temper == "Coward" && !drunk;
            bool hot = temper == "Hothead" || temper == "Proud" || drunk;
            bool hostile = p != null && (p.Hate >= 30 || p.Vengeful || p.Murderous);
            if (st != null && deeds != null)
            {
                var mine = new HashSet<string>(st.SawDeeds.Concat(st.KnownDeeds));
                Deed toThem = null, seen = null, heard = null;
                foreach (Deed d in deeds)
                {
                    if (d == null || d.Kind == null || !mine.Contains(d.Id))
                        continue;
                    long age = now - d.At;
                    bool them = d.VictimId == id || (name != null && d.Detail == name);
                    if (them && age <= PersonalDays && (d.Kind == "assault" || Stealing.Contains(d.Kind)) && (toThem == null || d.At > toThem.At))
                        toThem = d;
                    else if (!them && age <= SeenDays && Killing.Contains(d.Kind) && st.SawDeeds.Contains(d.Id) && (seen == null || d.At > seen.At))
                        seen = d;
                    else if (!them && age <= SeenDays && Stealing.Contains(d.Kind) && home != null && d.Town == home && (heard == null || d.At > heard.At))
                        heard = d;
                }
                if (toThem != null && !coward)
                    return toThem.Kind == "assault" ? "hurt" : "robbed";
                if (seen != null && (hot || hostile) && !coward)
                    return "killed";
                if (heard != null && (hot || hostile) && !coward)
                    return "thief";
            }
            if (hostile && !coward)
                return "grudge";
            if (st != null && st.Affection <= -25 && (hot || st.Affection <= -40) && !coward)
                return "words";
            return null;
        }

        /// How it ends if you stand there and take it: "fists", "draw" (a gun out: only one who has a gun, at the given
        /// percent, a hothead or a drunk), or "spit" (said their piece and goes).
        public static string Ending(string temper, bool drunk, string matter, bool armed, int drawPercent, string seed)
        {
            bool hot = temper == "Hothead" || drunk;
            if (armed && hot && drawPercent > 0 && (matter == "killed" || matter == "hurt" || matter == "grudge") && Voice.Chance(seed + "|draw", drawPercent))
                return "draw";
            int fists = temper == "Hothead" || temper == "Proud" ? 80 : drunk ? 75 : temper == "Cold" || temper == "Greedy" ? 45 : 60;
            if (matter == "words")
                fists -= 20;
            return Voice.Chance(seed + "|fists", fists) ? "fists" : "spit";
        }

        private static readonly Dictionary<string, string[]> Hails = new Dictionary<string, string[]>
        {
            { "hurt", new[] { "You! Yes, you. Hold it right there.", "Hey! I've got business with you.", "Don't you walk away from me. Not this time." } },
            { "robbed", new[] { "You there! I know that coat.", "Hey! Hold up, thief!", "Stop right there. I know your face." } },
            { "killed", new[] { "You! Stand where you are.", "Hey, mister! A word. Now.", "I know you. Don't you turn your back." } },
            { "thief", new[] { "You! Stranger! Hold it.", "Hey. You. I've heard about you.", "Hold on there, mister." } },
            { "grudge", new[] { "You! Turn around.", "There he is. Hey!", "Stay put, you." } },
            { "words", new[] { "Hey! You and me aren't done.", "You. Yes, you. Come here.", "Hold up, mister. I've something to say." } },
        };

        private static readonly Dictionary<string, string[]> Faces = new Dictionary<string, string[]>
        {
            { "hurt", new[] { "You put your hands on me. I've thought about nothing else since.", "Laid me out in the street like a sack of meal. Remember?", "Still got the bruise you gave me. Want to see it?", "You hit a man and walk on like it's nothing. Not today.", "Knocked me down in front of folks I know. Now it's my turn." } },
            { "robbed", new[] { "You went through my pockets. Mine. I want it back.", "Took what was mine and rode off laughing. I wasn't laughing.", "That was a month's money you took. A month.", "Thief. Say it isn't so. Go on.", "You robbed me, mister. I don't forget a face." } },
            { "killed", new[] { "I was watching. I watched him fall.", "A man's dead because of you, and you stroll about like a deacon.", "I was there. I saw you shoot. Nobody else will say it. I will.", "Blood on your hands and you buy your coffee like the rest of us.", "Somebody ought to stand up to you. Seems it's me." } },
            { "thief", new[] { "Folks here work for what they have. Then you come along.", "Word is you've been helping yourself in this town. Not anymore.", "We know what you are. The whole street knows.", "You thieve from my neighbours and think we'll smile at you?", "Take your thieving somewhere else, mister." } },
            { "grudge", new[] { "I've been waiting a long while to say this to your face.", "You and me have a score, and I mean to settle it.", "Every night I lie awake on account of you. No more.", "I swore if I saw you again I'd not walk away.", "You've wronged me, mister, and you know it." } },
            { "words", new[] { "You talk to folk like dirt. Talk like that to me now.", "I didn't care for how you spoke to me. Not one bit.", "You think you're better than us? Prove it.", "Been chewing on what you said. It's gone sour.", "Mind your mouth around here, mister. Or I'll mind it for you." } },
        };

        private static readonly string[] FistLines = { "Put 'em up.", "Come on, then. Fists.", "No guns. Just you and me.", "Let's see what you've got.", "I'll knock that look off your face." };
        private static readonly string[] DrawLines = { "Go on. Reach for it.", "Draw, damn you.", "One of us walks away. Draw.", "Enough words. Draw." };
        private static readonly string[] SpitLines = { "There. I've said it. You're not worth the knuckles.", "Remember me, mister. I'll remember you.", "Go on, then. The whole town knows what you are.", "I've said my piece. Keep out of my way.", "That's all. Don't come near me again." };
        private static readonly string[] CowardLines = { "That's right! Walk away, coward!", "Run along! Yellow, the lot of you!", "Coward! Everybody saw it!", "Turned tail! I knew you would!", "Go on, run! Brave man with a gun, aren't you?" };
        private static readonly string[] BackLines = { "Easy! Easy. I'm going.", "All right! All right. Forget I spoke.", "Don't shoot. I'm leaving. See?", "I'll go. Just put that away.", "You'd shoot a man for talking? I'm going." };
        private static readonly string[] Thoughts = { "My heart's going like a drum. Say it anyway.", "If I don't say it now I never will.", "Everybody's watching. Good. Let them watch.", "Hands, stop shaking. Not now.", "He's bigger than I remembered. Too late now." };

        public static string Hail(string matter, string seed) { return Voice.Pick(seed + "|hail", PoolOf(Hails, matter)); }
        public static string Face(string matter, string seed) { return Voice.Pick(seed + "|face", PoolOf(Faces, matter)); }
        public static string Thought(string seed) { return Voice.Pick(seed + "|think", Thoughts); }
        public static string Coward(string seed) { return Voice.Pick(seed + "|coward", CowardLines); }
        public static string Backs(string seed) { return Voice.Pick(seed + "|back", BackLines); }

        public static string Ends(string ending, string seed)
        {
            return Voice.Pick(seed + "|end", ending == "draw" ? DrawLines : ending == "fists" ? FistLines : SpitLines);
        }

        /// For the log and F7: what it was over, in a few words.
        public static string Over(string matter)
        {
            switch (matter)
            {
                case "hurt": return "you struck them";
                case "robbed": return "you robbed them";
                case "killed": return "a killing they saw";
                case "thief": return "your thieving in their town";
                case "grudge": return "an old grudge";
                default: return "how you spoke to them";
            }
        }

        private static string[] PoolOf(Dictionary<string, string[]> d, string matter)
        {
            string[] pool;
            return matter != null && d.TryGetValue(matter, out pool) ? pool : d["grudge"];
        }

        /// Every line, for the originality and test harnesses.
        public static IEnumerable<string> AllLines()
        {
            return Hails.Values.SelectMany(x => x).Concat(Faces.Values.SelectMany(x => x)).Concat(FistLines).Concat(DrawLines).Concat(SpitLines).Concat(CowardLines).Concat(BackLines).Concat(Thoughts);
        }
    }
}
