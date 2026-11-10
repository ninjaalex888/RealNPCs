// An execution on the road (2026-10-26, Lex: more NPC-on-NPC scenes like the game's own, where two men were about to
// execute Zeke Townsend, but with the people RealNPCs keeps). One of a nearby town's regulars kneels at gunpoint out on
// a road, for a reason from his own life: the worry he's living through (InnerLife.WorryNow) names it, and the same
// name and sum his thoughts use are the ones the men say (InnerLife.WorryFill, the eleven dollars), so what he thinks,
// what they say and what happens agree. Who holds the guns (Lex: all of these): for a debt, the creditor himself when
// he's a real person of the town, with a hired man, else two hired men he sent; for a feud, the other man and his
// brother; for talking to the law, the region's outlaws; with no worry that fits, outlaws who caught him seeing what
// he shouldn't. Ride on and they go through with it. All the words are RealNPCs' own.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Executions
    {
        public sealed class Plan
        {
            public string Reason;     // debt, feud, informer, witness
            public string Boss;       // the creditor, the other family's name, or the gang
            public string BossId;     // a real person behind it (a regular), when there is one
            public string Captors;    // boss (him and a hired man), hired, kin, outlaws
            public string Gang;       // the region's outlaws, for informer and witness
            public string GangKey;    // odriscolls, raiders, murfrees: the models (the game side)
            public bool Again;        // the same men you let get away from a rescue here lately: they know you (FOURTEENHOUR reserve R3)
        }

        /// The plan for him: worry is his worry kind today (or null), who its other person as his thoughts name them,
        /// whoId that person's id when they're real, region the town or region it happens in.
        public static Plan For(string worry, string who, string whoId, string region, string seed)
        {
            string key = GangOf(region);
            string gang = key == "raiders" ? "the Lemoyne Raiders" : key == "murfrees" ? "the Murfree Brood" : "the O'Driscolls";
            switch (worry)
            {
                case "debt":
                    return new Plan { Reason = "debt", Boss = who ?? Residents.Surname(seed + "b"), BossId = whoId, Captors = whoId != null ? "boss" : "hired" };
                case "feud":
                    return new Plan { Reason = "feud", Boss = who ?? Residents.Surname(seed + "b"), BossId = whoId, Captors = "kin" };
                case "law":
                case "secret":
                    return new Plan { Reason = "informer", Boss = gang, Captors = "outlaws", Gang = gang, GangKey = key };
                default:
                    return new Plan { Reason = "witness", Boss = gang, Captors = "outlaws", Gang = gang, GangKey = key };
            }
        }

        private static readonly string[] Lemoyne = { "Saint Denis", "Rhodes", "Lagras", "Braithwaite", "Bayou Nwa", "Scarlett Meadows", "Bluewater Marsh", "Lemoyne", "Caliga" };
        private static readonly string[] Roanoke = { "Annesburg", "Van Horn", "Roanoke", "Butcher Creek", "Brandywine" };

        /// Whose outlaws ride where: the Lemoyne Raiders in Lemoyne, the Murfree Brood in Roanoke, the O'Driscolls elsewhere.
        public static string GangOf(string region)
        {
            string r = region ?? "";
            foreach (string s in Lemoyne)
                if (r.Contains(s))
                    return "raiders";
            foreach (string s in Roanoke)
                if (r.Contains(s))
                    return "murfrees";
            return "odriscolls";
        }

        /// The game's models for each, the ones enRAGE spawns for those outfits.
        public static string[] GangModels(string key)
        {
            switch (key)
            {
                case "raiders": return new[] { "g_m_y_uniexconfeds_01", "g_m_y_uniexconfeds_02", "g_m_o_uniexconfeds_01" };
                case "murfrees": return new[] { "g_m_m_unimountainmen_01" };
                default: return new[] { "g_m_m_uniduster_01", "g_m_m_uniduster_02", "g_m_m_uniduster_03" };
            }
        }

        /// What the men call the one they're holding to the player: "between him and Mr. Yates".
        public static string BossWord(Plan p)
        {
            return p.Reason == "debt" ? "Mr. " + p.Boss : p.Reason == "feud" ? "the " + p.Boss + " family" : p.Gang;
        }

        // the men to him, overheard
        public static string Threat(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "et", "{boss} said you'd beg. He said you'd promise. Go on, then.", "Eleven dollars. A man dying for eleven dollars.",
                        "Mr. {boss} gave you all spring. Spring's over.", "You don't pay, you don't stand. That's how it works."), p);
                case "feud":
                    return Fill(Voice.Pick(seed + "et", "You moved that fence post one time too many.", "Should've let the dog be. Should've let all of it be.",
                        "My family's put up with you twenty years. No more.", "Say you're sorry. Go on. It won't help."), p);
                case "committee":
                    return Voice.Pick(seed + "et", "Forty men signed that paper. You want to argue with forty?", "The posse came home with nothing. We won't.",
                        "Somebody did it, and you were seen on that road.", "The sheriff had his turn. We're what comes after.");
                case "informer":
                    return Fill(Voice.Pick(seed + "et", "Somebody talked to the law. Somebody sweating, just like you.", "{gang} don't forget a loose mouth.",
                        "You told the sheriff where we sleep. Now you'll sleep."), p);
                default:
                    return Fill(Voice.Pick(seed + "et", "You saw what you saw. That's all there is to it.", "Wrong road, wrong hour, friend.",
                        "Nothing personal. You just got eyes."), p);
            }
        }

        // him to them, overheard
        public static string Plead(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "ep", "I'll have it Saturday! Saturday, I promise you!", "Tell {boss} I'll work it off. Any work at all. Please.",
                        "It's eleven dollars! It's only eleven dollars!"), p);
                case "feud":
                    return Voice.Pick(seed + "ep", "It was never about the fence! Please!", "I'll sell the place and leave! I'll leave tonight!", "Think of my children, for God's sake.");
                case "committee":
                    return Voice.Pick(seed + "ep", "I was at the mill! Ask anyone, I was at the mill!", "You've known me eleven years. All of you.", "Shoot me and whoever did it goes on walking about.");
                case "informer":
                    return Voice.Pick(seed + "ep", "I never said a word! Not one word!", "They had me in a cell! What was I to do?", "There's a wife waiting on me. Please. A wife and two little ones.");
                default:
                    return Voice.Pick(seed + "ep", "I never saw a thing! My eyes were shut the whole time!", "I'll go east. You'll never hear of me again.", "Please. I'm nobody. I'm nobody at all.");
            }
        }

        // one of the men to the player, as he comes near: said to him, so it carries the reason to him
        public static string WarnYou(Plan p, string seed)
        {
            if (p.Again)
                return SecondActs.Again(seed);   // (reserve R3)
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "ew", "Ride on, mister. This is between him and {bossword}.", "Keep moving. This is debt business, and it's paid today."), p);
                case "feud":
                    return Voice.Pick(seed + "ew", "Family matter, stranger. Keep riding.", "This ain't yours. Turn around.");
                case "committee":
                    return Voice.Pick(seed + "ew", "Town business, stranger. We're seeing to it ourselves.", "The law had its turn. Ride on.");
                default:
                    return Fill(Voice.Pick(seed + "ew", "{gangadj} business. You didn't see us.", "Ride on, or kneel down next to him."), p);
            }
        }

        // the last warning to the player, a few seconds before
        public static string LastChance(Plan p, string seed)
        {
            if (p.Reason == "debt")
                return Fill(Voice.Pick(seed + "elc", "Last chance, mister. {bossword} paid for this, not you.", "I'm counting to three. Be gone by two."), p);
            return Voice.Pick(seed + "elc", "Last chance, mister. Ride off while you still can.", "I'm counting to three. Be gone by two.", "You still here? Then you're watching.");
        }

        // him to the player
        public static string CallsToYou(string seed)
        {
            return Voice.Pick(seed + "ec", "Mister! Mister, they're going to kill me!", "Please, sir! Help me!", "For the love of God, don't ride on!");
        }

        public static string LastWords(string seed)
        {
            return Voice.Pick(seed + "el", "Wait. Wait, please...", "Tell my wife I...", "Lord. I'm not ready.");
        }

        // the men to each other, after
        public static string Done(Plan p, string seed)
        {
            if (p.Captors == "boss" || p.Captors == "kin")
                return Voice.Pick(seed + "ed", "Not a word of this. Not to anyone.", "It's done. Let's go home.");
            return Voice.Pick(seed + "ed", "That's done.", "Leave him for the crows.", "Let's go before somebody comes.");
        }

        // him to the player, saved
        public static string Thanks(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "eth", "I'll pay {boss}. Every cent. Thank you, mister. Thank you.", "Eleven dollars. I nearly died for eleven dollars. Thank you."), p);
                case "feud":
                    return Voice.Pick(seed + "eth", "They'd have done it. They really would have done it. Thank you.", "You came out of nowhere. Lord, you came out of nowhere.");
                case "committee":
                    return Voice.Pick(seed + "eth", "My own neighbours. My own neighbours. Thank you.", "I sold that man seed for nine years, and he held the gun. Thank you, mister.");
                case "informer":
                    return Voice.Pick(seed + "eth", "They'll come again. But not today. Thank you.", "I owe you my life, mister. I don't know how to pay that.");
                default:
                    return Voice.Pick(seed + "eth", "I'm going home, and I'm never taking this road again.", "I'll light a candle for you. Every Sunday I live.");
            }
        }

        // what he thinks, kneeling (over his head)
        public static string During(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "edu", "Eleven dollars. Not like this. Not over eleven dollars.", "{boss} sent them. {boss}, who shook my hand at church."), p);
                case "feud":
                    return Voice.Pick(seed + "edu", "The children. Who'll see to the children.", "Twenty years of that fence. It comes to this.");
                case "committee":
                    return Voice.Pick(seed + "edu", "Sat beside him in church on Sunday. He passed me the hymnal.", "They need it to be someone. It's only that it's me.");
                case "informer":
                    return Voice.Pick(seed + "edu", "Should've kept my mouth shut. Should've kept my mouth shut.", "The sheriff swore nobody would know.");
                default:
                    return Voice.Pick(seed + "edu", "Don't look at their faces. Don't look.", "I only took the short way home.");
            }
        }

        // what he thinks for a few days after he was saved
        public static string After(Plan p, string seed)
        {
            if (p != null && p.Reason == "hogtied")   // cut loose by the road (Hogtied.cs), not knelt at gunpoint
                return Voice.Pick(seed + "eh", "Rope marks on both wrists still.", "Trussed in the dirt like a calf. Then a stranger came by.", "Every rider on the road, I look twice now.");
            if (p != null && p.Reason == "debt" && Voice.Chance(seed + "ead", 35))
                return Fill(Voice.Pick(seed + "eab", "Paid {boss} today. Every cent. Couldn't look him in the eye.", "Every time I pass {boss}'s door my knees go."), p);
            return Voice.Pick(seed + "ea", "Still feel the barrel at the back of my head.", "Knelt in the dirt and begged. Can't stop seeing it.",
                "A stranger rode up and I'm alive. Just like that.", "Slept with the lamp lit again. Every sound's a horse.");
        }

        /// What the town hears once he's found, for the "Around town" box.
        public static string Found(Plan p, string name, string town, string seed)
        {
            string where = town != null ? " on the " + town + " road" : " by the road";
            switch (p.Reason)
            {
                case "debt": return name + " was found shot dead" + where + ". Folks say it was over a debt to " + p.Boss + ".";
                case "feud": return name + " was found shot" + where + ". Nobody says the " + p.Boss + " name out loud.";
                case "committee": return name + " was found shot" + where + ". The men of the town aren't saying who went out that night.";
                case "informer": return name + " was found shot" + where + ". They say he'd talked to the law.";
                default: return name + " was found shot dead" + where + ". Nobody knows why.";
            }
        }

        private static string Fill(string s, Plan p)
        {
            string adj = p.GangKey == "raiders" ? "Raider" : p.GangKey == "murfrees" ? "Murfree" : "O'Driscoll";
            return s.Replace("{bossword}", BossWord(p)).Replace("{boss}", p.Boss ?? "").Replace("{gangadj}", adj).Replace("{gang}", Grammar.UpperFirst(p.Gang ?? "the gang"));
        }

        // ---- what can turn it (FIVEDAY2 item 15; built on the road executions Lex has seen). Why it reads true: killing a man who is
        // begging is hard to do, and most who hold the gun have never done it. One who balks, or someone arriving with the money, is
        // the thing a witness hopes for; a few who came to watch make the worst of it, because nobody ever thinks it's theirs to stop.

        public const string TwistNone = "none", TwistBalk = "balk", TwistReprieve = "reprieve";

        /// What may turn this one: a captor who won't (with two or more to disagree), or kin arriving with what's owed (a debt or a feud can be settled; a gang can't).
        public static string Twist(Plan p, int men, string seed)
        {
            int roll = Voice.Hash(seed + "|twist") % 100;
            if (men >= 2 && roll < 22)
                return TwistBalk;
            if (p != null && (p.Reason == "debt" || p.Reason == "feud" || p.Reason == "committee") && roll < 44)
                return TwistReprieve;
            return TwistNone;
        }

        /// Whether a few have come to watch.
        public static bool Crowd(string seed)
        {
            return Voice.Chance(seed + "|crowd", 35);
        }

        /// Whether the other man, hearing the first refuse, lets it go too.
        public static bool Relents(string seed)
        {
            return Voice.Chance(seed + "|relents", 45);
        }

        /// Whether kin come for the body before the crows do.
        public static bool KinCome(string seed)
        {
            return Voice.Chance(seed + "|kin", 45);
        }

        public static string Balks(string seed, Plan p = null)
        {
            if (p != null && p.Reason == "committee")
                return Voice.Pick(seed + "|balk", "We said we'd scare it out of him. We said scare.", "I can't. I've had supper at his table.", "Not me. Let a judge have him.");
            return Voice.Pick(seed + "|balk", "I took money to scare him. Nobody said this.", "Not me. I've a boy near his age.", "You do it. I'll be on the road.", "No. Look at him. No.");
        }

        public static string BalkAnswered(string seed, Plan p = null)
        {
            if (p != null && p.Reason == "committee")
                return Voice.Pick(seed + "|balka", "Then go home, and say nothing to your wife.", "There's always one. Go on, then.");
            return Voice.Pick(seed + "|balka", "Then ride, and be quick about it.", "Go on, then. I've got a strong stomach for two.", "You'll want your share all the same, I suppose.");
        }

        public static string RelentsLine(string seed)
        {
            return Voice.Pick(seed + "|relent", "Hell with it. Get up. Get up and run.", "You got lucky. Don't let me see you.", "Get out of here before I think better of it.");
        }

        public static string ReprieveCry(Plan p, string seed)
        {
            if (p != null && p.Reason == "committee")
                return Voice.Pick(seed + "|cry", "Stop! It wasn't him! They've got the man at the depot!", "Hold on! He was with me that night, I'll swear it on the Book!");
            return p != null && p.Reason == "feud"
                ? Voice.Pick(seed + "|cry", "Enough! It's finished! Father says it's finished!", "Hold your fire! The land's yours! It's yours!")
                : Voice.Pick(seed + "|cry", "Stop! Stop, I've got it! It's all here!", "Wait! Wait, I sold the mare! I've got the money!");
        }

        public static string ReprieveAnswer(Plan p, string seed)
        {
            if (p != null && p.Reason == "committee")
                return Voice.Pick(seed + "|cryans", "Then we were never here. Nobody was ever here.", "Get up. Get up, and don't you look at me.");
            return p != null && p.Reason == "feud"
                ? Voice.Pick(seed + "|cryans", "Took you long enough. Get him out of here.", "Twenty years, and this is how it ends. Go on.")
                : Voice.Pick(seed + "|cryans", "Count it. ...It's all here. Get up.", "Your kin bought your hide. Don't make them buy it twice.");
        }

        public static string Muttered(string seed)
        {
            return Voice.Pick(seed + "|mutter", "Somebody ought to stop it.", "Not me. It's not for me to say.", "What did he do? Does anybody know?", "Don't look away. They'll remember who looked away.", "I only came to see if it was true.");
        }

        public static string KinAtBody(string seed)
        {
            return Voice.Pick(seed + "|kinb", "Get up. Get up, you fool, get up.", "Who did this? Which way did they go?", "I told you to pay it. I told you.", "Lord. Who's going to tell your mother.");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            for (int t = 0; t < 12; t++)
            {
                string q = "t" + t;
                Plan debt = For("debt", "Yates", null, "Valentine", q), feud = For("feud", "Yates", null, "Valentine", q);
                Plan comm = new Plan { Reason = "committee", Boss = "Yates", Captors = "boss" };
                yield return Balks(q, comm);
                yield return BalkAnswered(q, comm);
                yield return ReprieveCry(comm, q);
                yield return ReprieveAnswer(comm, q);
                yield return Threat(comm, q);
                yield return Plead(comm, q);
                yield return WarnYou(comm, q);
                yield return Thanks(comm, q);
                yield return During(comm, q);
                yield return Found(comm, "Milo Hubbard", "Valentine", q);
                yield return Balks(q);
                yield return BalkAnswered(q);
                yield return RelentsLine(q);
                yield return ReprieveCry(debt, q);
                yield return ReprieveCry(feud, q);
                yield return ReprieveAnswer(debt, q);
                yield return ReprieveAnswer(feud, q);
                yield return Muttered(q);
                yield return KinAtBody(q);
            }
            foreach (string reason in new[] { "debt", "feud", "law", null })
                for (int i = 0; i < 16; i++)
                {
                    Plan p = For(reason, "Yates", reason == "debt" ? "res:y" : null, i % 2 == 0 ? "Rhodes" : "Valentine", "s" + i);
                    string s = "s" + i;
                    yield return Threat(p, s);
                    yield return Plead(p, s);
                    yield return WarnYou(p, s);
                    yield return Done(p, s);
                    yield return Thanks(p, s);
                    yield return During(p, s);
                    yield return After(p, s);
                    yield return Found(p, "Milo Hubbard", "Valentine", s);
                }
            for (int i = 0; i < 12; i++)
            {
                yield return CallsToYou("s" + i);
                yield return LastChance(For(i % 2 == 0 ? "debt" : null, "Yates", null, "Valentine", "s" + i), "s" + i);
                yield return LastWords("s" + i);
            }
        }
    }
}
