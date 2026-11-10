// The old soldiers (FOURTEENHOUR item 17; [Scenes] OldSoldiers, only with [World] HistoricalAccuracy: it's named history). Thirty-
// four years after Appomattox, most of the old men in a town of 1899 had been in the war on one side or the other, and the West was
// full of both: Union men homesteading the plains, Confederates gone west after the surrender. Two strangers of an age find out
// which side the other was on, in the saloon most (in drink), and:
// - of opposite sides, they argue it: Shiloh, the third day at Gettysburg, the march to the sea, the prisons, the pension one gets and
//   the other doesn't, who really won, and Cuba last summer. It ends by their natures: a toast to the dead, a cold parting, or blows
//   (more often in Lemoyne, where an insult must be answered: Core/Honour.cs). In a brawl over it, the onlookers who fought lean to
//   their own side, and in Lemoyne the young men to the grey (BrawlSides.QuarrelTie, "war");
// - of the same side, they remember it: their states, where they were, and a toast to the boys;
// - and a young man home from the war with Spain (Cuba, '98) or the islands (the volunteer regiments came home from Manila from the
//   end of August 1899) tells it in the saloon: an old soldier hears him out (one war to another), a working man or a farmer who holds
//   the islands a trust's war won't (QuarrelPolitics' islands sides), and anyone else asks what it was like.
// Who served: old-timers, and men of 52 and over (born by 1847: 18 by the war's last spring), seeded by their id and their home's
// region (Lemoyne mostly grey, the cattle country of New Austin grey and blue, the north and west mostly blue); a young man home
// from '98 one in twenty-five, a soldier two in five. Already there and left alone: the old men's arguments about the old days
// (Core/Scenes.OldDays.cs: winters and prices, no war), the times' talk of the Philippines (Core/Times.cs) and the 1899 quarrels
// over the islands (Core/QuarrelPolitics.cs). Why it reads true: the reconciliation of the 1890s was real and thin (the blue and the
// grey shaking hands at reunions, an old Confederate general in a blue coat in Cuba, and every Decoration Day the old quarrel), and
// an old wound is the quickest way two old men are young again. The facts named are sure ones; the words are this add-on's. Plain
// C#, no game; the game side fills Someone.War and SceneCast.Wars (Game/WestworldScript.OldSoldiers.cs).
using System;

namespace EnrageWestworld.Core
{
    public static class OldSoldiers
    {
        public static bool On = true;   // [Scenes] OldSoldiers and [World] HistoricalAccuracy, from the settings
        public const int WarPercent = 55, WarStreetPercent = 25, ComradePercent = 35, ComradeStreetPercent = 15, StoryPercent = 30, StoryStreetPercent = 8;
        // the war argued is a quarrel: in a saloon it sits among the fights (over two hot-blooded men's 50 and most snaps, under drink and temper's 56),
        // in the street with the street's own business; the remembering and the story over the times' 41 and gossip's 40
        public const int WarStrength = 54, WarStreetStrength = 45, ComradeStrength = 42, StoryStrength = 43, StoryStreetStrength = 40;
        public const int OldFromAge = 52, YoungVetPercent = 4, SoldierVetPercent = 40;

        static readonly string[] NewAustin = { "Armadillo", "Tumbleweed", "MacFarlane", "Thieves", "Benedict", "Plainview", "Rathskeller", "Gaptooth", "Cholla", "Rio Bravo", "New Austin" };

        public static bool Old(string war) { return war == "blue" || war == "grey"; }
        public static bool Young(string war) { return war == "cuba" || war == "islands"; }

        /// The war someone was in: "blue" or "grey" for a man old enough (an old-timer's build, or 52 and over), "cuba" or
        /// "islands" for a young man home from the war with Spain or the Philippines, else null. Seeded by id, so a man's war
        /// never changes; his home's region weighs the sides. `month` 1-12 (the islands' volunteers came home from September 1899).
        public static string WarOf(string id, string home, bool woman, bool oldBuild, int age, bool young, bool soldier, int year, int month)
        {
            if (!On || id == null || woman)
                return null;
            if (oldBuild || age >= OldFromAge)
            {
                int grey, blue;
                Shares(home, out grey, out blue);
                int roll = (Voice.Hash(id + "|war") & 0x7fffffff) % 100;
                return roll < grey ? "grey" : roll < grey + blue ? "blue" : null;
            }
            if (!young && !soldier)
                return null;
            if (!Voice.Chance(id + "|war98", soldier ? SoldierVetPercent : YoungVetPercent))
                return null;
            bool islandsHome = year > 1899 || month >= 9;
            return islandsHome && Voice.Chance(id + "|islands", 45) ? "islands" : "cuba";
        }

        /// Of a hundred old men from this home, how many wore the grey and how many the blue (the rest: too old, exempt, or came
        /// over after the war).
        public static void Shares(string home, out int grey, out int blue)
        {
            if (Calendar.Region(home) == "south")
            {
                grey = 66;
                blue = 8;
            }
            else if (home != null && Array.Exists(NewAustin, t => home.IndexOf(t, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                grey = 42;
                blue = 24;
            }
            else
            {
                grey = 22;
                blue = 44;
            }
        }

        static string Coat(string war) { return war == "grey" ? "grey" : "blue"; }

        /// x and y, two strangers of an age or a young man home from the war and whoever is by him: a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool saloon, bool night)
        {
            if (!On || x == null || y == null || x.Id == y.Id || x.Woman || y.Woman || x.War == null)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (Old(x.War) && Old(y.War))
            {
                bool against = x.War != y.War;
                int pct = against ? (saloon ? WarPercent : WarStreetPercent) : (saloon ? ComradePercent : ComradeStreetPercent);
                if (!Voice.Chance(key + gameDay + "|oldsoldiers", pct))
                    return null;
                return new Reason
                {
                    Kind = "oldsoldiers", Strength = against ? (saloon ? WarStrength : WarStreetStrength) : ComradeStrength,
                    Why = against ? x.Name + " wore the " + Coat(x.War) + ", and " + y.Name + " the " + Coat(y.War) : x.Name + " and " + y.Name + " both wore the " + Coat(x.War)
                };
            }
            if (Young(x.War) && !Young(y.War) && Voice.Chance(key + gameDay + "|warstory", saloon ? StoryPercent : StoryStreetPercent))
                return new Reason { Kind = "warstory", Strength = saloon ? StoryStrength : StoryStreetStrength, Why = x.Name + " is home from " + (x.War == "cuba" ? "Cuba" : "the islands") + ", and tells " + y.Name + " of it" };
            return null;
        }

        static string YearsOn()
        {
            int n = Math.Max(1, Voice.NowYear - 1865);
            string[] tens = { "", "ten", "twenty", "thirty", "forty", "fifty" };
            string[] ones = { "", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
            if (n < 10 || n >= 60)
                return n.ToString();
            return n % 10 == 0 ? tens[n / 10] : tens[n / 10] + "-" + ones[n % 10];
        }

        // ---- two old soldiers: of opposite sides, the argument; of the same, the remembering ------------------------------

        // each topic told from the blue side first and from the grey side first: { opener, the other's answer, the opener again }
        static readonly string[][] BlueFirst =
        {
            new[] { "Shiloh. Nineteen years old, and you boys came out of those trees while we were frying our breakfast.", "We had you backed against the river by dark. One more hour of daylight.", "And in the morning Grant had his fresh men, and you were walking back to Corinth." },
            new[] { "I went to the sea with Uncle Billy. Georgia ate well that winter. We did, anyway.", "My mother was in Georgia that winter. She ate what you left her.", "War's war. You'd no business starting it." },
            new[] { "Andersonville. Nine months. I weighed ninety pounds the day they let me out.", "Elmira. One winter of it, and half my mess buried in New York. Don't you tell me about prisons.", "I won't, then. If you won't tell me." },
            new[] { "Joe Wheeler in a blue coat, leading our boys in Cuba. Lived to see that.", "Your boys and ours in the same ditch under that hill. First war since '65 I didn't mind reading about.", "Took a war with Spain to do it." },
        };
        static readonly string[][] GreyFirst =
        {
            new[] { "Shiloh. We caught you boys frying your breakfast and ran you to the river.", "And Grant was still there in the morning, and you were walking back to Corinth.", "One more hour of daylight on the Sunday, and you'd have been swimming." },
            new[] { "Third day at Gettysburg, I went up that field with Pickett's men. A mile of open ground.", "I was at the stone wall on the ridge. I watched you come.", "Then you know what it was. I walk that field most nights still." },
            new[] { "Washington sends you a pension. What does it send me? A limp, and the tax to pay yours.", "You'd have one too if you'd picked the right side.", "I picked my state's side. Same as you did." },
            new[] { "You never whipped us. You starved us out, and there's a difference.", "Not at Appomattox there wasn't. Your General Lee seemed to know it.", "Don't you say his name in that voice." },
        };

        /// Two old soldiers. c.Wars[0] and c.Wars[1] are A's and B's ("blue" or "grey"); unknown (a test), they're set opposite.
        public static Scene War(SceneCast c, string seed)
        {
            string wa = c.Wars != null && c.Wars.Length > 0 ? c.Wars[0] : null, wb = c.Wars != null && c.Wars.Length > 1 ? c.Wars[1] : null;
            if (!Old(wa))
                wa = Old(wb) ? (wb == "blue" ? "grey" : "blue") : Voice.Chance(seed + "|side", 50) ? "blue" : "grey";
            if (!Old(wb))
                wb = wa == "blue" ? "grey" : "blue";
            return wa == wb ? Comrades(c, wa, seed) : Argue(c, wa, seed);
        }

        static bool Hot(string t) { return t == "Hothead" || t == "Proud"; }

        static Scene Argue(SceneCast c, string wa, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string ta = c.Tempers != null ? c.Tempers[0] : null, tb = c.Tempers != null ? c.Tempers[1] : null;
            string[][] pool = wa == "blue" ? BlueFirst : GreyFirst;
            string[] t = pool[(Voice.Hash(seed + "|war") & 0x7fffffff) % pool.Length];
            string them = wa == "blue" ? "Reb" : "Yank";
            // how it ends, by their natures: both hot, blows; one hot, blows half the time (more in Lemoyne, where it must be answered);
            // a cold one, a cold parting; else a toast to the dead. A coward never comes to blows.
            bool honour = Honour.Holds(c.Town);
            int blowsPct = ta == "Coward" || tb == "Coward" ? 0 : Hot(ta) && Hot(tb) ? 85 : Hot(ta) || Hot(tb) ? (honour ? 65 : 45) : honour ? 12 : 4;
            string end = Voice.Chance(seed + "|blows", blowsPct) ? "blows" : ta == "Cold" || tb == "Cold" || ta == "Greedy" ? "cold" : "toast";
            Scene s;
            switch (end)
            {
                case "blows":
                    s = new Scene { Kind = "brawl", Fight = true, NoLadder = true, Over = "war", Summary = c.Saloon ? "two old soldiers fought the war over again in the saloon" : "two old soldiers fought the war over again in the street" };
                    s.Add(0, t[0], "mild", "face").Add(1, t[1], "angry").Add(0, t[2], "mock")
                     .Add(1, Voice.Pick(seed + "|say", "Say that again, " + (wa == "blue" ? "Yank" : "Reb") + ". Slow, so I hear it.", "You'll take that back, old man.", "Men better than you are in the ground over less."), "angry", "shove")
                     .Add(0, Voice.Pick(seed + "|swing", "I'll say it slower, " + them + ".", "Come on, then. Like old times.", "I've waited thirty years for this."), "angry", "fight", 9000)
                     .Add(0, null, null, "endfight");
                    break;
                case "cold":
                    s = new Scene { Kind = "argue", NoLadder = true, Over = "war", Summary = "two old soldiers of either side had words over the war" };
                    s.Add(0, t[0], "mild", "face").Add(1, t[1], "angry").Add(0, t[2], "mock")
                     .Add(1, Voice.Pick(seed + "|cold", "We'll not agree, you and me.", "There's no talking to you people. There never was.", "Keep your war. I'm done with it."), "angry")
                     .Add(0, Voice.Pick(seed + "|cold2", YearsOn() + " years, and no, we won't.", "Suits me.", "Then we're done."), "mild")
                     .Add(1, null, null, "walkaway");
                    break;
                default:
                    s = new Scene { Kind = "oldtoast", Summary = "two old soldiers of either side argued the war, then drank to the dead" };
                    s.Add(0, t[0], "mild", "face").Add(1, t[1], "angry").Add(0, t[2], "mild")
                     .Add(1, Voice.Pick(seed + "|quiet", "Well. " + Grammar.UpperFirst(YearsOn()) + " years.", "Listen to us. Two old fools.", "We were boys, the both of us."), "sad")
                     .Add(0, Voice.Pick(seed + "|toast", "To the boys that never came back.", "To the ones still out there in the fields.", "Here's to the dead, then. Yours and mine."), "sad", "flask")
                     .Add(1, Voice.Pick(seed + "|toast2", "Both sides of them.", "Yours and ours. All of them.", "To the dead."), "sad", "flask");
                    break;
            }
            s.Cast = 2;
            return s;
        }

        // the states their regiments were raised in, and where they were (true of that state's regiments)
        static readonly string[][] Blue =
        {
            new[] { "Iowa", "With Grant at Vicksburg." }, new[] { "Wisconsin", "The Iron Brigade. What was left of it." }, new[] { "Ohio", "Sherman's army. Atlanta and the sea." },
            new[] { "Illinois", "Shiloh, the first day." }, new[] { "Kansas", "Border work, mostly. You don't want to hear it." }, new[] { "Michigan", "Cavalry. Custer's brigade." },
        };
        static readonly string[][] Grey =
        {
            new[] { "Louisiana", "The Tigers, in Virginia." }, new[] { "Texas", "Hood's brigade." }, new[] { "Tennessee", "Bragg, then Johnston, then Hood." },
            new[] { "Mississippi", "Vicksburg. Inside it, eating mule." }, new[] { "Georgia", "Atlanta was home, and we couldn't hold it." }, new[] { "Alabama", "With Lee, to the end." },
        };

        static Scene Comrades(SceneCast c, string side, string seed)
        {
            string[][] pool = side == "grey" ? Grey : Blue;
            int i = (Voice.Hash(seed + "|ca") & 0x7fffffff) % pool.Length, j = (Voice.Hash(seed + "|cb") & 0x7fffffff) % pool.Length;
            string[] ma = pool[i], mb = pool[j];
            Scene s = new Scene { Kind = "comrades", Summary = "two old soldiers of the same side remembered the war together" };
            s.Add(0, Voice.Pick(seed + "|ask", "You were in it. I can always tell. Which outfit?", "You've the look of a man who wore the " + side + ". Who were you with?", "You were in the war, I'd say. Where?"), "mild", "face")
             .Add(1, ma == mb ? mb[0] + ", same as you, I'd guess. " + mb[1] : mb[0] + ". " + mb[1], "mild")
             .Add(0, ma == mb ? "Well, I'll be. " + ma[0] + " it is." : ma[0] + " for me. " + ma[1], "cheer")
             .Add(1, Voice.Pick(seed + "|still", "Still hear it some nights.", "Not many of us left now.", "My boy asks about it. I tell him about the food."), "sad")
             .Add(0, Voice.Pick(seed + "|toast", "To the boys, then.", "To the old outfits. What's left of them.", "Here's to the ones who didn't come home."), "sad", "flask")
             .Add(1, Voice.Pick(seed + "|toast2", "To the boys.", "What's left of us.", "Amen to that."), "sad", "flask");
            s.Cast = 2;
            return s;
        }

        // ---- a young man home from the war with Spain or the islands tells it ---------------------------------------------

        /// A, home from Cuba or the islands (c.Wars[0]; unknown, the islands); B hears him out by who he is: an old soldier
        /// (c.Wars[1] "blue" or "grey"), a working man or a farmer against the islands' war (c.Groups[1]), or anyone else.
        public static Scene Story(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string war = c.Wars != null && c.Wars.Length > 0 && Young(c.Wars[0]) ? c.Wars[0] : "islands";
            string heard = c.Wars != null && c.Wars.Length > 1 ? c.Wars[1] : null;
            string group = c.Groups != null && c.Groups.Length > 1 ? c.Groups[1] : null;
            string ta = c.Tempers != null ? c.Tempers[0] : null;
            string tell = war == "cuba"
                ? Voice.Pick(seed + "|tell", "We went up that hill at San Juan on the first of July. Hottest day God ever made, and the Spanish had Mausers.",
                             "Tampa, then the boat, then the fever. The Spanish were the least of it.", "Six weeks in Cuba. Buried more boys of the fever than of the Spanish.")
                : Voice.Pick(seed + "|tell", "Manila. Rain for a month at a time, and men you never saw in the grass.",
                             "We crossed a river out there under fire, on a rope. I can't swim.", "A year in the islands. I don't know what we won.");
            bool anti = war == "islands" && !Old(heard) && QuarrelPolitics.Stance("islands", group, false) == 1 && Voice.Chance(seed + "|anti", 60);
            Scene s;
            if (Old(heard))
            {
                s = new Scene { Kind = "warheard", Summary = "an old soldier heard a young one out about his war" };
                s.Add(0, tell, "mild", "face")
                 .Add(1, Voice.Pick(seed + "|old", "I had mine at " + (heard == "grey" ? "Chickamauga" : "Shiloh") + ". You did all right, son.", "It doesn't leave you, does it?", "Every war's the same war, once you're in it."), "sad")
                 .Add(0, Voice.Pick(seed + "|sir", "No, sir. It doesn't.", "Thank you, sir.", "My father said the same."), "sad")
                 .Add(1, Voice.Pick(seed + "|drink", "Come and sit. I'll stand you one.", "Your money's no good tonight. Not to me.", "Have a glass with me. Old men like company."), "mild", "flask");
            }
            else if (anti)
            {
                bool blows = Hot(ta) && Voice.Chance(seed + "|antiblows", 55);
                s = blows ? new Scene { Kind = "brawl", Fight = true, NoLadder = true, Over = "politics:islands", Summary = "a man home from the islands came to blows with one who called it a trust's war" }
                          : new Scene { Kind = "argue", NoLadder = true, Over = "politics:islands", Summary = "a man home from the islands had words with one who called it a trust's war" };
                s.Add(0, tell, "mild", "face")
                 .Add(1, Voice.Pick(seed + "|trust", "And for what? Coaling stations and sugar men. You were had, son.", "We'd no business out there. None.", "A war for the trusts, and you boys paid for it."), "angry")
                 .Add(0, Voice.Pick(seed + "|man", "I was with the man next to me. That's all it ever was.", "You weren't there.", "Say that to the ones we buried."), "angry");
                if (blows)
                    s.Add(1, Voice.Pick(seed + "|again", "I'll say it again. You were had.", "Somebody ought to tell you.", "Then you were a fool for it."), "mock", "shove")
                     .Add(0, Voice.Pick(seed + "|swing", "Say it once more.", "That does it.", "You've said enough."), "angry", "fight", 9000)
                     .Add(0, null, null, "endfight");
                else
                    s.Add(1, Voice.Pick(seed + "|so", "Maybe so. It's still a bad war.", "I'll not drink to it.", "Suit yourself."), "mild")
                     .Add(0, null, null, "walkaway");
            }
            else
            {
                s = new Scene { Kind = "warheard", Summary = "a young man told of his war, and was heard out" };
                s.Add(0, tell, "mild", "face")
                 .Add(1, Voice.Pick(seed + "|what", "What was it like? Truly?", "I read about it in the paper. It's different hearing it.", "You were there? Well, I'll be."), "mild")
                 .Add(0, Voice.Pick(seed + "|like", "Loud. Then quiet, which was worse.", "Hot, and a long way from home.", "Like nothing I'd want my brother to see."), "sad")
                 .Add(1, Voice.Pick(seed + "|glad", "Well. Glad you're home.", "I'd like to hear the rest sometime.", "Let me buy you one."), "mild");
            }
            s.Cast = 2;
            return s;
        }
    }
}
