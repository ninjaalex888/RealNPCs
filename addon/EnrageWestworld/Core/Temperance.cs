// The temperance ladies (FIVEDAY item 12; roadmap.md's idea bank: "women pray and sing outside the saloon. Drinkers jeer
// or slink off; one husband is marched home; the barkeep sours"). A street scene between two of the town's own people out
// of doors by day: a temperance woman meets a drinker before the saloon doors and asks him to sign. He jeers and
// goes in anyway, slinks off ashamed, swears off for the day, or is taken home on her arm, by the seed, the pious more
// likely to start it (Scenes.Minds: a pious mind leans to it, and so does a man whose worry is the drink, the one she'd
// pick). Whoever saw it thinks of it (InnerLife.Now's BySeen "temperance"). Behind [Scenes] Temperance. No hymn's words:
// her lines are her own, the period's cause in our words.
//
// Why it reads true: the Woman's Christian Temperance Union was the largest women's organisation in the country by 1899,
// and its members met drinkers at the saloon door with prayer, pledge cards and song; reactance (the jeer, the drink taken
// out of spite) and shame (the slink) were both what they got, and now and then a man who signed. Plain C#, no game; all
// words original. mindtest TemperanceTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Temperance
    {
        private static readonly string[] Plea =
        {
            "{b}. Before you go through those doors, think of the ones at home.",
            "Sign the pledge, {b}. One line, and your Saturdays are your own again.",
            "There's no friend for you in that saloon, {b}. Only a till.",
            "We'll pray for you, {b}, whether you want it or not.",
            "Every glass in there is bought with bread money, {b}.",
            "Turn round, {b}. It's not too late in the day to turn round.",
            "The Lord made you for better than a brass rail, {b}.",
            "Walk home with me, {b}. Just today. See how it feels.",
        };

        private static readonly string[] Jeer = { "Pray louder, ma'am. They can't hear you in Kansas.", "I'll drink one for your soul, then.", "Save it for Sunday, sister.", "Bread money's mine to spend." };
        private static readonly string[] Slink = { "...I was only passing.", "Another day, ma'am. Another day.", "Spare me the look, ma'am." };
        private static readonly string[] Swear = { "You're right. Not today. Not today.", "Give me the card. Quick, before I think.", "One day dry. I'll try one day." };
        private static readonly string[] Home = { "...All right. Home, then.", "You'll not let go of my arm, will you?", "Lead on. I'm too tired to argue." };
        private static readonly string[] HerAfter = { "God keep you.", "One saved is one more than yesterday.", "Bless you. Saturday's the hardest. Remember that." };   // after he swears off
        private static readonly string[] HerRebuffed = { "We'll be here again Saturday.", "I'll pray for him all the same.", "The Lord is patient. So am I." };   // after a jeer or a slink

        /// The scene: kind "temperance", A the woman (whichever side she's on), B the man. Outcome by the seed: jeer 35,
        /// slink 30, swear off 20, home on her arm 15.
        // ---- at the saloon door (FOURTEENHOUR item 25; [Scenes] TemperanceDoor). Before, the pledge was put only when a chance
        // meeting happened to pick it. Now, at dusk, a churchwoman (devout or a churchgoer, never a working girl) by the saloon
        // door puts it to a man going in who's in drink or has the drink on his mind; a second woman of the church beside her (one
        // already near: a cast at the door, never a crowd brought to watch, Lex's rule) says her word too; a hothead's insult to
        // them brings her kin if he's near, and blows with sides (the pious lean to him, the drinking men to the drinker); a signed
        // pledge is the town's talk (Core/Tales.cs: "back at the brass rail by Friday"), and the preacher's word on Sunday.

        public static bool DoorOn = true;   // [Scenes] TemperanceDoor, from the settings
        public const int DoorPercent = 15, DoorStrength = 43, DoorFrom = 16, DoorTo = 21;

        /// x, a churchwoman, and y, a man in drink or with the drink on his mind, by the saloon door at dusk: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, int hour, bool drinkWorry)
        {
            if (!DoorOn || saloon || hour < DoorFrom || hour >= DoorTo || x == null || y == null || !x.Woman || y.Woman || x.P == null || x.Id == y.Id)
                return null;
            if ((x.P.Devotion != "Devout" && x.P.Devotion != "Churchgoer") || InnerLife.TradeGroup(x.P.Trade) == "girl" || (!y.Drunk && !drinkWorry))
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|door", DoorPercent))
                return null;
            return new Reason { Kind = "temperance", Strength = DoorStrength, Why = x.Name + " puts the pledge to " + y.Name + " by the saloon's doors" };
        }

        private static readonly string[] Second = { "We'll sing for you, {b}. We sing for all of them.", "Every Saturday till you sign, {b}. We're patient.", "Your mother would want it, {b}.", "Think of the little ones, {b}." };
        private static readonly string[] Insult = { "Out of my road, you old crow.", "Go home and scrub something, the pair of you.", "I'll drink twice tonight, just for you, sister.", "Take your hymns and choke on them." };
        private static readonly string[] KinRises = { "That's my kin you're talking to.", "Say that again. To me, this time.", "You'll take that back, or I'll take it out of you." };
        private static readonly string[] KinLast = { "My mouth, my business.", "Make me, then.", "Who asked you?" };

        /// The pledge put to him with her kin near and a hothead's insult (c.KinStands): A her kin, B the drinker, C the churchwoman;
        /// blows, over the pledge ("temperance:" + A's id when the game side knows it, for the sides).
        public static Scene WithKin(SceneCast c, string seed)
        {
            string b = Scenes.N(c, 1, seed);
            Scene s = new Scene { Kind = "brawl", Fight = true, NoLadder = true, Over = "temperance" + (c.KinId != null ? ":" + c.KinId : ""), Summary = "a drinker insulted the temperance ladies at the saloon door, and her kin answered it" };
            s.Beats.Add(new Beat(2, Pick(Plea, seed + "|plea").Replace("{b}", b), "mild", "face") { To = 1 });
            s.Add(1, Pick(Insult, seed + "|ins"), "mock")
             .Add(0, null, null, "meet")   // he walks over to him; the next word waits till he's there
             .Add(0, Pick(KinRises, seed + "|kin"), "angry", "face")
             .Add(1, Pick(KinLast, seed + "|last"), "angry", "shove")
             .Add(0, null, "angry", "fight", 9000)
             .Add(0, null, null, "endfight");
            s.Cast = 3;
            return s;
        }

        // ---- the husband marched home (DEPTH item 5; with [Scenes] TemperanceDoor). The pledge put at the door, and his own wife
        // near (already in the street: the cast's third, never fetched): she takes his arm and walks him home past the men he
        // drinks with, and he goes, grumbling or meek by his nature. The barkeep watching his door empty thinks less of the church
        // women for it, and says so to the next man at the bar (the game side: a soured tie, a caption and a thought; the barkeep is
        // a counter keeper and is never given an order). Why it reads true: the wives were the temperance movement's front line,
        // and a husband walked home past his friends was the shame it counted on.

        private static readonly string[] WifeTakes =
        {
            "{b}. Your supper's been cold an hour. Come home.", "Not tonight, {b}. Take my arm.", "I'll not stand here while you shame us both. Home.",
            "The children asked where their father was, {b}. Come along.", "That's the rent money in your pocket, {b}. Home. Now.",
        };
        private static readonly Dictionary<string, string[]> HeGoes = new Dictionary<string, string[]>
        {
            { "fearful", new[] { "Yes, dear. Yes, I'm coming.", "All right. All right, don't take on so.", "I was only going to look in." } },
            { "bold", new[] { "A man can't have one glass in peace.", "In front of the whole street, woman?", "Fine. Fine! I'm coming, ain't I?", "I'll walk on my own, thank you.", "Let go my arm. I said I'm coming." } },
            { "cold", new[] { "As you like.", "Very well.", "No need for a scene." } },
            { "plain", new[] { "You're right. You always are.", "I'm sorry, love. Let's go home.", "Lead on, then." } },
        };
        private static readonly string[] ChurchAfter = { "Go with God, the both of you.", "There's one home safe tonight.", "See he's in church on Sunday, ma'am.", "Bless you for it, ma'am." };

        /// The pledge put, and his wife there (c.WifeHome: A or B the churchwoman by sex, the other the drinker, C his wife): she walks
        /// him home. Kind "marchedhome"; the Director's walk home (homewith) takes them both off together.
        public static Scene MarchedHome(SceneCast c, string seed)
        {
            int her = c.AWoman ? 0 : 1, him = 1 - her;
            string b = c.Names != null && c.Names[him] != null ? Person.FirstOf(c.Names[him]) : Scenes.N(c, him, seed);   // his wife knows his name whoever else doesn't
            string group = Minds.TemperGroup(c.Tempers != null ? c.Tempers[him] : null);
            string[] goes;
            if (!HeGoes.TryGetValue(group ?? "", out goes))
                goes = HeGoes["fearful"];
            Scene s = new Scene { Kind = "marchedhome", Summary = "a man's wife walked him home from the saloon door, past the men he drinks with" };
            s.Add(her, null, null, "approach", 200)
             .Add(her, Pick(Plea, seed + "|plea").Replace("{b}", Scenes.N(c, him, seed)), "mild", "face");
            s.Beats.Add(new Beat(2, Pick(WifeTakes, seed + "|wife").Replace("{b}", b), "angry", "approach") { To = him });
            s.Add(him, Pick(goes, seed + "|goes"), group == "bold" ? "angry" : "mild", "face");
            s.Beats.Add(new Beat(2, null, null, "homewith") { To = him });   // she and he, side by side, away from the door (the walk-home runner)
            s.Add(her, Pick(ChurchAfter, seed + "|after"), "thanks");
            s.Cast = 3;
            s.Doer = 2;   // his wife's doing (the memories' roles)
            return s;
        }

        // the barkeep, as his door empties (the game side: a caption from him to the next man at his bar, and a thought; never an order)
        private static readonly string[] BarkeepSays =
        {
            "That's another one walked off my door on his wife's arm.", "Those church women will have me pouring lemonade.",
            "A man's money walks out that door on his wife's arm.", "If they had their way I'd be selling sarsaparilla.",
        };
        private static readonly string[] BarkeepThinks =
        {
            "Hymns at the door. Empty glasses at the bar.", "Every woman of that church costs me a dollar a night.", "{who} again. She'll have the whole town dry.",
        };

        public static string BarkeepLine(string seed)
        {
            return Pick(BarkeepSays, seed + "|bks");
        }

        public static string BarkeepThought(string churchwomanFirst, string seed)
        {
            return Pick(BarkeepThinks, seed + "|bkt").Replace("{who}", churchwomanFirst ?? "Her");
        }

        public static Scene Make(SceneCast c, string seed)
        {
            if (c != null && c.KinStands)
                return WithKin(c, seed);   // her kin near, a hothead's insult (item 25)
            if (c != null && c.WifeHome && c.AWoman != c.BWoman && c.CWoman)
                return MarchedHome(c, seed);   // his own wife near: she walks him home (DEPTH item 5)
            if (c == null || c.AWoman == c.BWoman)
                return null;
            int her = c.AWoman ? 0 : 1, him = 1 - her;
            string b = Scenes.N(c, him, seed);
            int how = (Voice.Hash(seed + "|how") & 0x7fffffff) % 100;
            Scene s = new Scene { Kind = "temperance", Summary = "a temperance woman put the pledge to a man outside the saloon" };
            s.Add(her, null, null, "approach", 200)
             .Add(her, Pick(Plea, seed + "|plea").Replace("{b}", b), "mild", "face");
            bool second = c.HasCrowd && c.CWoman && c.SecondChurch;   // a second woman of the church beside her (item 25), she and nobody else near
            if (second)
            {
                s.Beats.Add(new Beat(2, Pick(Second, seed + "|second").Replace("{b}", b), "mild", "face") { To = him });
                s.Cast = 3;   // cast with her: without it the Director gave her line to the drinker (the review of items 22-26)
            }
            if (how < 35)
                s.Add(him, Pick(Jeer, seed + "|j"), "mock", "walkaway").Add(her, Pick(HerRebuffed, seed + "|a"), "mild");
            else if (how < 65)
                s.Add(him, Pick(Slink, seed + "|s"), "mild", "walkaway").Add(her, Pick(HerRebuffed, seed + "|a"), "mild");
            else if (how < 85)
            {
                s.Doer = her;   // she put it, he signed: the memories' roles (pledge is one-sided (the review of items 22-26))
                s.Kind = "pledge";   // he signed: the town's talk tomorrow, and the preacher's on Sunday (item 25)
                s.Summary = "a man signed the pledge at the saloon door";
                s.Add(him, Pick(Swear, seed + "|w"), "thanks").Add(her, Pick(HerAfter, seed + "|a"), "thanks", "walkaway").Add(him, null, null, "walkaway");
            }
            else
                s.Add(him, Pick(Home, seed + "|h"), "mild").Add(her, null, null, "walkaway").Add(him, null, null, "walkaway");
            return s;
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Plea)
                yield return l.Replace("{b}", "Amos");
            foreach (string l in Second)
                yield return l.Replace("{b}", "Amos");
            foreach (string[] pool in new[] { Jeer, Slink, Swear, Home, HerAfter, HerRebuffed, Insult, KinRises, KinLast })
                foreach (string l in pool)
                    yield return l;
            foreach (string l in WifeTakes)
                yield return l.Replace("{b}", "Amos");   // DEPTH item 5
            foreach (string[] pool in HeGoes.Values)
                foreach (string l in pool)
                    yield return l;
            foreach (string[] pool in new[] { ChurchAfter, BarkeepSays })
                foreach (string l in pool)
                    yield return l;
            foreach (string l in BarkeepThinks)
                yield return l.Replace("{who}", "Martha");
        }
    }
}
