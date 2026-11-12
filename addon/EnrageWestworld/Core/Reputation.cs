// Reputation in the street (SIXDAY item 5c; roadmap 17: "a known outlaw walking into town is stared at and stepped
// around, while the doctor or the preacher is greeted"). Who counts: enRAGE's gunmen (its gang and outfit men, read from
// its own roles) and a resident whose trouble is the wanted poster (the worry "law"), for the outlaws; the doctor, a
// preacher, the sheriff and his deputies, and a counter keeper out in the street, for the respected. What the street does,
// by nature: a look that follows him, a step off his line, the old and the timid turning away, the proud and the hot-headed
// staring him down; a hat tipped, a nod or a wave and a word for the respected. One witness mutters it on the ticker. The
// player, lightly: a known killer here, or a known peacemaker, gets a thought from a witness and never a task (the game
// reacts to him already). Why it reads true: a town reads the people who walk through it, and standing shows in small
// motor acts, who yields the boardwalk and who meets whose eye. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Reputation
    {
        public const float NotableMetres = 25f;   // a notable this near the player is noticed
        public const float ReactMetres = 10f;     // by those this near him, who can see him
        public const int GapMs = 20000;           // one reputation reaction near the player per this
        public const int EachMinutes = 60;        // each notable once per game hour
        public const int MostReactors = 3;
        public const int YouGapMs = 60000;        // the player's standing thought at most this often
        public const int YouEachMinutes = 60;     // ...and each witness once per game hour
        public const int StandingDays = 7;        // the deeds and kindnesses that count toward his standing in a town

        public static readonly string[] Kinds = { "outlaw", "doctor", "preacher", "law", "keeper" };

        public static bool IsRespected(string kind)
        {
            return kind == "doctor" || kind == "preacher" || kind == "law" || kind == "keeper";
        }

        /// One passer-by's reaction to an outlaw, by nature: stare (the proud, the hot-headed meet his eye), turnaway (the old,
        /// the timid), stepoff (off his line, then on their way), else follow (a look that follows him); someone in a scenario
        /// only looks (a stare, or a look that follows).
        public static string ToOutlaw(string temper, bool old, bool inScenario, string seed)
        {
            if (temper == "Proud" || temper == "Hothead")
                return "stare";
            if (inScenario)
                return "follow";   // sat, leaning or at their work: a look, never a turn or a step (the review)
            if (old || temper == "Coward")
                return "turnaway";
            if (Voice.Hash(seed + "|step") % 100 < 45)
                return "stepoff";
            return "follow";
        }

        /// The greeting for one of the respected: a hat tip from most men, a nod or a wave from women (the Passing clips).
        public static string ToRespected(bool woman, string seed)
        {
            int r = Voice.Hash(seed + "|gr") % 100;
            return woman ? (r < 50 ? "nod" : "wave") : (r < 65 ? "hattip" : "nod");
        }

        /// Whether the greeting has a word with it, in their own voice (half the time).
        public static bool Speaks(string seed)
        {
            return Voice.Hash(seed + "|sp") % 100 < 50;
        }

        /// The words of the greeting, for the caption: by whom they greet and the hour.
        public static string Greeting(string kind, string surname, int hour, string seed)
        {
            string time = hour < 12 ? "Morning" : hour < 18 ? "Afternoon" : "Evening";
            bool timed = Voice.Hash(seed + "|t") % 100 < 55;
            switch (kind)
            {
                case "doctor": return timed ? time + ", Doc." : "Doctor.";
                case "preacher": return timed ? time + ", Reverend." : "Reverend.";
                case "law": return timed ? time + ", Sheriff." : "Sheriff.";
                default: return surname != null ? (timed ? time + ", Mr. " + surname + "." : "Mr. " + surname + ".") : time + ".";
            }
        }

        /// What a witness mutters on the ticker: the outlaw by the witness's own nature, the respected by who they are.
        public static string Mutter(string kind, string temper, string seed)
        {
            string key = kind == "outlaw" ? (temper == "Coward" ? "outlaw|afraid" : temper == "Proud" || temper == "Hothead" ? "outlaw|proud" : "outlaw") : kind;
            string[] pool;
            if (!Mutters.TryGetValue(key, out pool))
                return null;
            return pool[Voice.Hash(seed + "|m") % pool.Length];
        }

        /// The player's standing in a town from what's known of him here lately: a killer (two killings known, or one with
        /// no kindness beside it), a peacemaker (three kindnesses and no killing), or nothing said.
        // ---- the good deeds a town knows (SEAMS item 19; [Scenes] GoodDeedsStand) --------------------------------------------
        // The standing ("peacemaker" at three kindnesses, none of them killings) counted only the kindnesses in the events log
        // (whiskey bought, money handed over, a rescue); its fights stopped, its thief caught and the outlaw spared, all known,
        // never made you its peacemaker. Now the deeds it knows of count too. A rescue is left out here: its own
        // "defend" event already counts it, and one act counted twice would be a thumb on the scale.
        public static bool GoodDeedsCount = true;   // from the settings
        private static readonly string[] Counted = { "peace", "mercy", "spared", "thiefcaught" };

        /// The good deeds of yours this town knows of since then (a deed's count at least one).
        public static int GoodDeedsKnown(IEnumerable<Deed> deeds, string town, long since)
        {
            if (!GoodDeedsCount || deeds == null || town == null)
                return 0;
            int n = 0;
            foreach (Deed d in deeds)
                if (d != null && d.Known && d.Town == town && d.At >= since && System.Array.IndexOf(Counted, d.Kind) >= 0)
                    n += System.Math.Max(1, d.Count);
            return n;
        }

        public static string Standing(int killsKnown, int kindnesses)
        {
            if (killsKnown >= 2 || (killsKnown == 1 && kindnesses == 0))
                return "killer";
            if (kindnesses >= 3 && killsKnown == 0)
                return "peacemaker";
            return null;
        }

        /// A witness's thought of the player's standing.
        public static string YouThought(string standing, string seed)
        {
            string[] pool;
            if (standing == null || !Mutters.TryGetValue("you|" + standing, out pool))
                return null;
            return pool[Voice.Hash(seed + "|y") % pool.Length];
        }

        public static readonly Dictionary<string, string[]> Mutters = new Dictionary<string, string[]>
        {
            { "outlaw", new[] {
                "Gun on his hip and that look about him. Keep walking.", "That one's no cowhand. Eyes down.",
                "The street went quiet when he came in. It knows.", "Trouble, walking upright. Let it pass.",
                "Don't meet his eye. Don't give him a reason.", "A man like that doesn't buy. He takes.",
                "Somebody's going to get shot, and it'll be whoever's nearest.", "There's a poster with that face on it, or there ought to be." } },
            { "outlaw|afraid", new[] {
                "Lord, let him want nothing here.", "Cross the street. Cross it now.",
                "My hands are shaking and he hasn't so much as looked at me.", "Keep walking. Don't look. Don't look.",
                "Bad men smell fear. Breathe.", "If he stops me I'll play deaf.",
                "Home. Now. By the back way.", "He's the kind the papers write about after." } },
            { "outlaw|proud", new[] {
                "He walks like he owns the street. He doesn't.", "I'll not step aside for the likes of him.",
                "Let him look. I've looked at worse.", "Gun or no gun, he's dirt in a good coat.",
                "A man like that wants you to flinch. I won't.", "He's not the first bad man through here, and he'll not be the last.",
                "Stare all you like, friend. This is my town.", "I've buried men who swaggered like that." } },
            { "doctor", new[] {
                "There goes the doctor. Half this town owes him a life.", "Doc looks tired. He's always tired.",
                "Set my boy's arm. I'd take a bullet for that man.", "The doctor, out walking. Means nobody's dying today.",
                "Morning, Doc. Lord, he's aged this year.", "Tip the hat. He sat up all night with Mother once.",
                "Doc never asks for the money twice. Good man.", "There's the one man here everybody's glad to see." } },
            { "preacher", new[] {
                "The reverend. Straighten up; it's not Sunday, but still.", "Preacher's about. Mind the language.",
                "Good day, Reverend. He'll ask why I wasn't in church.", "There goes the preacher. I owe him a confession and a dollar.",
                "A man of God on a dirt street. Looks about as lost as the rest of us.", "The reverend walks like the sermon's still in him.",
                "Hat off. He buried my father decent.", "Preacher's smile. He means it, which is the unsettling part." } },
            { "law", new[] {
                "The sheriff. Nod, and keep your hands where he can see them.", "There's the law. The street walks straighter for a minute.",
                "Morning, Sheriff. He never says it back.", "The deputy's making his rounds. Means somebody got robbed.",
                "The badge goes by and every man checks his conscience.", "Sheriff looks like he slept in that hat.",
                "The law's about. Good or bad, depending on what you did Saturday.", "Nod to the sheriff. He remembers who doesn't." } },
            { "keeper", new[] {
                "There's the storekeeper, out from behind his counter. He has legs after all.", "I still owe him for the flour. Nod, and walk a little faster.",
                "The barkeep in daylight. Hardly knew him.", "Nod to the man who carries my credit.",
                "The keeper's out walking. Store must be shut for dinner.", "There goes the one man in town who knows what everybody owes.",
                "Tip the hat. He saves me the good cuts.", "He's as much the town as the church is." } },
            { "you|killer", new[] {
                "That's him. The one who should be in the papers, if he isn't.", "A killer, they say. Look at him, buying coffee like a Christian.",
                "He's killed men in this town. Walk wide.", "The street knows what he did. Nobody says it to his face.",
                "That man's hands have done things. Mine won't shake. Much.", "Don't serve him. Don't refuse him either.",
                "Men dead by his hand, and he strolls. That's the West for you.", "Lord, let him be passing through." } },
            { "you|peacemaker", new[] {
                "That's the one who helped the widow. A good man, they say.", "He stepped in for a stranger last week. Rare, that.",
                "There's the fellow who pays for folk. Odd sort. Decent.", "Nod to him. He's done this town more good than the mayor.",
                "The kind one. There's not many.", "He put a man down who needed it, and was gentle after. I'll take that.",
                "They speak well of him at the store. That's the hardest praise there is.", "A stranger who gives more than he takes. Hope he stays." } },
        };

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (KeyValuePair<string, string[]> kv in Mutters)
                foreach (string s in kv.Value)
                    yield return s;
            foreach (string kind in Kinds)
                for (int h = 6; h < 24; h += 6)
                {
                    yield return Greeting(kind, "Cobb", h, "a");
                    yield return Greeting(kind, null, h, "b");
                }
        }
    }
}
