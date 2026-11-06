// A bystander's word on a scene they're watching: one person nearby who has a name calls out once, from who they are
// to the two in it (a friend of one sides with them, an enemy of one takes the other side) and their own temper (the
// kind try to calm it, the cold want it gone, the sharp egg it on). A quarrel draws the most, a fight a cheer or a
// cry for help, a courtship a smile or a tut, bad news a murmur; small talk and gossip draw nothing. All lines are
// original to this add-on. The pools grew in FIVEDAY item 3 (4 lines each to 8-16).
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class CrowdLine
    {
        public string Line;
        public string Voice;
    }

    public static class Crowd
    {
        private static readonly string[] SideWithB = { "Don't let {a} talk to you like that, {b}.", "{b}'s in the right of it and everybody knows it.", "Stand your ground, {b}. {a}'s all wind and no weather.", "Half this street heard what {a} said, {b}. Don't you take it.",
            "You tell {a}, {b}. Somebody's waited years to.", "{a} talks big for somebody who owes half the town, {b}.", "Never mind {a}, {b}. Hot air and old whiskey.", "{b}'s got the right of it. Go on, say so.",
            "Don't back down, {b}. Not to the likes of {a}.", "Ask {a} about Tuesday night, {b}.", "Everybody knows what {a} is. Don't let it get turned round, {b}.", "Chin up, {b}. The street's with you." };
        private static readonly string[] SideWithA = { "Tell {b} straight, {a}. Somebody ought to.", "Go on, {a}. You've held your tongue long enough.", "Say your piece, {a}. {b}'s had this coming since spring.", "We're all behind you, {a}. {b} knows it, too.",
            "Don't let {b} crawl out of it, {a}.", "{b}'s been asking for this since harvest, {a}.", "Give it to {b} plain, {a}. Plain is all {b} understands.", "You're right and you know it, {a}.",
            "Make {b} answer you, {a}. No walking off.", "Remember the fence, {a}. Remember the mule.", "Hear that, {b}? {a}'s telling the truth, and it stings.", "Every word, {a}. Every word of it." };
        private static readonly string[] Calm = { "Leave it, the pair of you. Not in the street.", "Come on, {a}. It ain't worth it.", "Leave it be, {b}. There's children watching from the porch.", "Come on, now. Shake hands and go get a drink, the both of you.",
            "Sleep on it, {a}. It'll look smaller in the morning.", "You two were friends at Christmas. Remember that.", "Easy, {b}. Nobody here wants to bury anybody.", "Whatever it is, it'll keep till you've both cooled.",
            "Your family's watching, {a}. Think on it.", "Let it go, {b}. Some fights are bought dear.", "Lower your voices, the both of you. Please.", "No harm done yet. Let's keep it that way." };
        private static readonly string[] Cold = { "Take it somewhere else.", "Somebody fetch the sheriff before it gets out of hand.", "Some of us have work to get to.", "Shout at each other in your own yard, not mine.",
            "Every week, the same two.", "I'm not standing here all day for this.", "Mind the horses. You're spooking them.", "Go and bark at each other down by the creek.",
            "This town has a jail, you know.", "Spare us. Please.", "If there's blood, I'm not sweeping it.", "Lord, the noise of it." };
        private static readonly string[] Sharp = { "Settle it or shift, the pair of you. You're blocking the road.", "Louder, {a}. They can't hear you in Rhodes.", "Is this a quarrel or a quilting bee? Somebody throw a punch.", "Somebody sell tickets. {a} and {b} are putting on a show.",
            "All that jaw and not one swing between you.", "My grandmother quarrels harder than this, {a}.", "Ten cents says {b} backs down first.", "Swing or shift. You're in the road.",
            "Hold my coat, somebody. This is getting good.", "{a}'s face is redder than a schoolmarm's.", "Whoever's losing, buy the next round.", "Lord, the two of you could talk a mule to death." };

        // a fight: {x} the one they favour (a friend, or the other man's enemy), {y} the other
        private static readonly string[] CheerFor = { "Hit him, {x}! Hit him!", "Mind his left, {x}! His left!", "Knock him into the trough, {x}!", "Keep your hands up, {x}!",
            "That's it, {x}! Again!", "Don't let him tie you up, {x}!", "Under the ribs, {x}! Under the ribs!", "Get up, {x}, he's tiring!",
            "Show {y} the floor, {x}!", "Go on, {x}, he's bleeding!", "Watch his feet, {x}! He's set to rush you!", "For the fence, {x}! Give him one for the fence!",
            "Stay on him, {x}!", "Let {y} have it, {x}!", "Steady, {x}. Pick your punch.", "Finish it, {x}!" };
        private static readonly string[] Jeer = { "Swing like you mean it!", "My old aunt swings harder than that!", "This is a fight? Looks like a dance.", "Somebody's going to fall over from tired before they fall from a punch.",
            "Hit him back, you fool!", "Whoever wins buys the house a round!", "Look at them, flapping like hens.", "Ten more seconds and I'll be asleep.",
            "Ha! Right on the nose!", "That'll swell up pretty.", "Get him, get him... oh, you missed him.", "Somebody fetch a bucket for the loser." };
        private static readonly string[] FightFear = { "Somebody stop them!", "Lord, somebody pull them apart!", "Fetch the sheriff! They'll kill each other!", "Stop it! Stop it, there's blood on the boards!",
            "Get the children inside!", "Somebody do something!", "He's going to hurt him bad. Please, somebody!", "Not his head! Not his head!",
            "God have mercy, stop!", "Run for the marshal, quick!", "I can't watch this.", "Somebody grab his arms!" };
        private static readonly string[] FightCold = { "Not again.", "Fools, the pair of them.", "Somebody get the law. I'm not getting in the middle.", "Fighting in the street like dogs.",
            "Whatever it's over, it isn't worth teeth.", "Go on and kill each other, then. Less noise after.", "Same as last Saturday.", "The doctor's going to do well out of this." };

        private static readonly string[] WarmKind = { "About time, the pair of you.", "Well, look at that. Good for you, {a}.", "Somebody tell the preacher to brush his coat.", "Look at {a}, red as a ribbon. Bless the two of them.",
            "Young love. I remember it.", "Treat {b} right, {a}. We're all watching.", "Well, I'll be. {a}'s smiling.", "Somebody fetch a fiddler.",
            "That's the nicest thing I've seen all week.", "{b}'s been waiting on that for months." };
        private static readonly string[] WarmCold = { "In broad daylight. Lord.", "Some of us are trying to eat.", "Take it off the street. There's a church for that.", "My mother would have fetched a bucket of water.",
            "No shame left in this town.", "Mooning in the street like calves.", "I give it till Christmas.", "Somebody's mother ought to hear about this.",
            "There's a time and a place, and this is neither.", "Well, I've lost my appetite." };

        private static readonly string[] SadKind = { "Poor soul.", "God keep you, {a}.", "Hard luck, {a}. We're all sorry for it.", "Let {a} be a while. Nobody needs to stare.",
            "If there's anything, {a}. Anything at all.", "We'll bring something round tonight, {a}.", "The Lord gives and the Lord takes. Hard to bear.", "Lean on us, {a}. That's what neighbours are for.",
            "I lit a candle for them, {a}.", "Come sit a while, {a}. You shouldn't be alone." };

        /// What kind of scene this is to the people watching: "quarrel", "fight", "warm", "sad", or null for one that
        /// draws no word (small talk, gossip, the paper, a song).
        public static string Mood(string kind)
        {
            switch (kind)
            {
                case "argue": case "snap": case "envy": case "debt": case "standoff": case "cheat": case "jealous": return "quarrel";
                case "brawl": case "rough": return "fight";
                case "drunk": return null;     // a man face down on the floor: the barkeep has his say, nobody takes a side
                case "avoid": return null;   // nobody makes a speech of a man crossing the street
                case "flirt": case "sweet": case "propose": case "makeup": case "cheer": return "warm";
                case "comfort": return "sad";
                default: return null;
            }
        }

        /// How often someone watching speaks up, by the scene's mood (percent).
        public static int Chance(string mood)
        {
            return mood == "quarrel" || mood == "fight" ? 60 : mood == "warm" ? 40 : mood == "sad" ? 30 : 0;
        }

        /// The bystander's line, or null if they'd say nothing. warmToA / warmToB: what's between them and A or B
        /// (above 0 friendly, below hostile, 0 nothing); manner: kind, cold or sharp (Exchanges.Manner).
        public static CrowdLine Word(string kind, int warmToA, int warmToB, string manner, string aName, string bName, bool twoMen, string seed)
        {
            string mood = Mood(kind);
            string[] pool = null;
            string voice = "mild";
            switch (mood)
            {
                case "quarrel":
                    // a friend of both tries to calm it, an enemy of both eggs it on; otherwise they take a side
                    if (warmToA > 0 && warmToB > 0) { pool = Calm; voice = "defuse"; }
                    else if (warmToA < 0 && warmToB < 0) { pool = Sharp; voice = "mock"; }
                    else if (warmToB > 0 || warmToA < 0) { pool = SideWithB; voice = "angry"; }
                    else if (warmToA > 0 || warmToB < 0) { pool = SideWithA; voice = "angry"; }
                    else if (manner == "kind") { pool = Calm; voice = "defuse"; }
                    else if (manner == "cold") { pool = Cold; voice = "tut"; }
                    else { pool = Sharp; voice = "mock"; }
                    break;
                case "fight":
                    // the kind cry for it to stop; the cold want it gone; the rest cheer on the one they favour (a friend, or
                    // the other man's enemy; with neither, whoever catches their eye), or jeer the both of them (FIVEDAY item 3:
                    // before, a friend of one could shout for the other). A fight with a woman in it draws no cheering.
                    if (manner == "kind" || (!twoMen && manner != "cold")) { pool = FightFear; voice = "scared"; }
                    else if (!twoMen || (manner == "cold" && warmToA == 0 && warmToB == 0)) { pool = FightCold; voice = "tut"; }
                    else if (warmToA == 0 && warmToB == 0 && manner == "sharp" && Voice.Chance(seed + "jeer", 50)) { pool = Jeer; voice = "mock"; }
                    else
                    {
                        bool forA = warmToA != warmToB ? warmToA > warmToB : (Voice.Hash(seed + "side") & 1) == 0;
                        string x = forA ? aName : bName, y = forA ? bName : aName;
                        return new CrowdLine { Line = Voice.Pick(seed + "crowd", CheerFor).Replace("{x}", x).Replace("{y}", y), Voice = "cheer" };
                    }
                    break;
                case "warm":
                    if (manner == "kind" || warmToA > 0 || warmToB > 0) { pool = WarmKind; voice = "cheer"; }
                    else { pool = WarmCold; voice = "tut"; }
                    break;
                case "sad":
                    if (manner == "kind" || warmToA > 0) { pool = SadKind; voice = "sorry"; }
                    break;
            }
            if (pool == null)
                return null;
            return new CrowdLine { Line = Voice.Pick(seed + "crowd", pool).Replace("{a}", aName).Replace("{b}", bName), Voice = voice };
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return SideWithB.Concat(SideWithA).Concat(Calm).Concat(Cold).Concat(Sharp).Concat(CheerFor).Concat(Jeer).Concat(FightFear).Concat(FightCold).Concat(WarmKind).Concat(WarmCold).Concat(SadKind);
        }
    }
}
