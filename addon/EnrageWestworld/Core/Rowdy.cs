// Rowdy drunks at night (FOURTEENHOUR reserve R5; [Scenes] RowdyDrunks). A drunk already called a man out in a saloon at night and
// went down before a blow was struck (Scenes.Drunk; [Scenes] DrunkChallenges), and friends whose talk is done already walk on
// together (item 15). What the street never had is the night's own noise: two friends in drink coming down it singing. Now, out of
// doors in a town at night, a man in drink (never a coward) with a friend in drink beside him picks on a man going by who isn't
// drunk and isn't his friend: the two sing a line apiece as they come (the Director's meet, the friend's approach), he picks his
// quarrel, the other answers by his nature, and then it's the friend's: he hauls him home (the Director's homewith, the drunk carried
// home's walk) or, now and then, it comes to blows (the Director's fight between the drunk and the man he picked on, so the brawl's
// own joiners and cascade take it from there: item 7), and the friend takes him home after if he can. A lone drunk out in the street
// is the saloon's scene (he calls the man out and falls on his face). Why it reads true: drink makes a man loud and a friend makes
// him braver, but the same friend is usually the one who walks him home. Plain C#, no game; all words this add-on's, the songs too.
namespace EnrageWestworld.Core
{
    public static class Rowdy
    {
        public static bool On = true;   // [Scenes] RowdyDrunks, from the settings
        public const string Kind = "rowdy";
        public const int Strength = 43, Percent = 45;   // a pair-night's roll, when the drunk and the man going by fit
        public const float FriendMetres = 8f;          // his friend in drink this near him

        /// Whether x, in drink, picks on y in the street at night: never a coward, never at a friend, never at another drunk.
        public static bool Picks(Someone x, Someone y, bool night, bool outdoors, bool friends, long gameDay)
        {
            if (!On || x == null || y == null || !night || !outdoors || friends || !x.Drunk || y.Drunk || x.Woman || y.Woman || x.Temper == "Coward")
                return false;
            return Voice.Chance(x.Id + "|" + y.Id + "|" + gameDay + "|rowdy", Percent);
        }

        /// Whether it comes to blows: the hot and the proud drunk more, a friend who eggs him on (hot or proud) more, a loyal or timid
        /// friend less; a sharp answer more, a kind one less. A percent, 5 to 70 (from 15: at 20 the night street's fights went up 84 to 92 of 240, near a tenth).
        public static int FightPercent(string drunkTemper, string friendTemper, string answer)
        {
            int p = 15;
            if (drunkTemper == "Hothead" || drunkTemper == "Proud") p += 15;
            if (friendTemper == "Hothead" || friendTemper == "Proud") p += 15;
            if (friendTemper == "Loyal" || friendTemper == "Coward" || friendTemper == "Plain") p -= 10;
            if (answer == "sharp") p += 20;
            else if (answer == "kind") p -= 10;
            return p < 5 ? 5 : p > 70 ? 70 : p;
        }

        static readonly string[][] Songs =
        {
            new[] { "Oh, the moon's on the river and the jug's on the shelf...", "...and the girl that I married has gone off by herself!" },
            new[] { "Roll me home, boys, roll me home...", "...the road's too long and the night's too cold!" },
            new[] { "Here's to the mule that kicked my brother...", "...and here's to the one that kicked the other!" },
            new[] { "My pockets are empty, my glass is dry...", "...so pour me another and I'll tell you why!" },
            new[] { "When I was a young man I drank for a reason...", "...and now that I'm older it's just for the season!" },
        };

        /// The night's noise: A the drunk, B the man going by, C his friend in drink. answer: how B takes it
        /// ("kind", "cold", "sharp"); fights: it comes to blows (else the friend hauls him home).
        public static Scene Play(SceneCast c, string seed, string answer, bool fights)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed), f = Scenes.N(c, 2, seed);
            string[] song = Songs[(Voice.Hash(seed + "|song") & 0x7fffffff) % Songs.Length];
            Scene s = new Scene { Kind = Kind, Cast = 3, Summary = "two drunks came down the street singing, and one picked on a man going by" };
            s.Add(0, song[0], "drunk");
            s.Beats.Add(new Beat(2, song[1], "drunk", null) { To = 0 });
            s.Add(0, null, null, "meet");
            s.Add(2, null, null, "approach", 200);
            s.Add(0, Voice.Pick(seed + "|pk", "You. What're you looking at, you long drink of water?", "Hey! You! You think you're better'n us?", "Look at this one. Too good to sing, are you?",
                                "Something funny, mister? Something funny about a song?"), "drunk", "face");
            s.Add(1, answer == "sharp" ? Voice.Pick(seed + "|bs", "Shift yourself, you sot.", "Once more, and you'll wish you hadn't.")
                   : answer == "cold" ? Voice.Pick(seed + "|bc", "Go home.", "Not tonight.")
                   : Voice.Pick(seed + "|bk", "Go home, friend. You can hardly stand.", "Easy, now. Nobody's laughing at you."), answer == "sharp" ? "threat" : "mild");
            if (fights)
            {
                s.Fight = true;   // the brawl's own gates and machinery (the sat, the hushed town, the law's eye; the joiners and cascade)
                s.Beats.Add(new Beat(2, Voice.Pick(seed + "|eg", "Go on, " + a + "! Show him!", a + ", don't! ...Ah, Lord.", "Did you hear what he called you, " + a + "?"), "drunk", null) { To = 0 });
                s.Add(0, Voice.Pick(seed + "|sw", "Right. Right, you've earned it.", "I'll learn you some manners."), "angry", "shove")
                 .Add(0, null, "angry", "fight", 9000)
                 .Add(0, null, null, "endfight");
                s.Beats.Add(new Beat(2, Voice.Pick(seed + "|af", "Come on. Home, before the law comes.", "That's enough, " + a + ". Home."), "mild", "homewith") { To = 0 });
                return s;
            }
            s.Beats.Add(new Beat(2, Voice.Pick(seed + "|hl", "Come on, " + a + ". He ain't worth it. Home.", "Leave him be, " + a + ". My feet hurt. Home.", "Let's go, " + a + ". Your wife'll have us both skinned."), "mild", null) { To = 0 });
            s.Add(0, Voice.Pick(seed + "|gr", "...I'd have had him, " + f + ". I'd have had him.", "Fine. Fine. But he started it.", "You're a good friend, " + f + ". Best there is."), "drunk");
            s.Beats.Add(new Beat(2, null, null, "homewith") { To = 0 });
            return s;
        }

        /// How the man picked on answers, by his nature: the hot and the proud sharp, the cold cold, the rest kind.
        public static string Answer(string temper)
        {
            return temper == "Hothead" || temper == "Proud" ? "sharp" : temper == "Cold" || temper == "Greedy" ? "cold" : "kind";
        }

        public static System.Collections.Generic.IEnumerable<string> AllSongs()
        {
            foreach (string[] s in Songs)
                foreach (string l in s)
                    yield return l;
        }
    }
}
