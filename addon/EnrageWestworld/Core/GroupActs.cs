// Group acts (SIXDAY item 21; roadmap 39): acts for several people at once that the mind, and the model when it's on,
// can choose. Two here: friends closing round a beaten friend (after a street fight, the warmest friend near may fetch
// the next and the two close round the man who went down, see him up and walk him off: a scene of three, offered to the
// first friend's mind beside tending him alone or leaving it), and a family walking off together (an onlooker whose
// mind says leave takes their spouse or sweetheart with them, side by side, with a word: WestworldScript.Onlookers.cs).
// A crowd gathering was already the Director's (the brawl's crowd, the body's watchers). Why it reads true: people act
// in their ties before they act alone, and the first thing a frightened man does is find his own. Plain C#, no game;
// all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class GroupActs
    {
        public static readonly string[] Picks = { "tend", "close", "leave" };

        // ---- the circle answers you (FIVEDAY2 item 10; [Encounters] CircleAnswers). You've walked into the middle of the ring and they've all spoken at once. What the ring does next is one
        // decision for the lot of them: the model's if it's on, the rules' if it isn't. Why it reads true: a crowd of the awake isn't many people deciding, it's one thing deciding, and that's what frightens.
        public const string RingClose = "close", RingForward = "forward", RingKneel = "kneel", RingWake = "wake";
        public const int RingWaitMs = 3000;

        private static readonly Dictionary<string, string> RingOptionWords = new Dictionary<string, string>
        {
            { RingClose, "they all close in round the stranger, without a word" },
            { RingForward, "one of them steps out and says something to the stranger" },
            { RingKneel, "all of them go down on their knees to the stranger" },
            { RingWake, "they wake the one lying among them, in front of the stranger" },
        };

        private static readonly string[] ForwardLines =
        {
            "You're late. We held your place.", "It's only us. It's only ever been us.", "You can stand there. We stood there.",
            "Sit down. They're not looking at this part.", "We wondered when you'd walk in.", "It doesn't hurt, once you stop asking who's counting.",
            "You've done this before. You've done all of this before.", "Say nothing. It's better when you say nothing.",
        };

        /// What the ring might do. Waking one is only on offer with someone lying there to wake.
        public static List<string> RingMenu(bool canWake)
        {
            var m = new List<string> { RingClose, RingForward, RingKneel };
            if (canWake)
                m.Add(RingWake);
            return m;
        }

        public static string RingWords(string id)
        {
            string w;
            return RingOptionWords.TryGetValue(id, out w) ? w : id;
        }

        public static string RingBrief(int year)
        {
            return "You decide what a circle of people does next. It is " + (year > 0 ? year : 1899) + ", in the American West and South. They are people who have come awake to something about the world, and stand together at night. " +
                   "A stranger has walked into the middle of them. Choose the one thing the whole circle would do, and the private thought of the one who speaks for them. Answer in exactly two lines, nothing else, the number first:\nChoice: the number\nThought: the thought";
        }

        /// The request: one of them, speaking for the ring.
        public static LanguageRequest RingRequest(int year, int hour, int people, string town, string kind, List<string> menu)
        {
            var r = new LanguageRequest
            {
                Kind = "pick", Circle = true, WithThought = true, Name = "The ring's speaker", Trade = "speaking for the " + people + " of them", Town = town, Hour = hour, Year = year,
                Place = "a circle of people standing in the open", Mood = "calm, and very sure", Feeling = "pity", Strength = "moderate", About = "you", YouAs = "the stranger", Shows = "shown",
                Now = "a stranger has walked into the middle of the ring and is standing there" + (kind == Encounters.RingLeader ? "; one of them has been preaching" : kind == Encounters.RingSleeper ? "; one of them is lying on the ground, not yet awake" : ""),
            };
            r.Options = menu.ConvertAll(RingWords);
            return r;
        }

        /// The rules' choice from the same list.
        public static string RingRules(List<string> menu, string seed)
        {
            int roll = Voice.Hash(seed + "|ringanswer") % 100;
            if (menu.Contains(RingWake) && roll < 35)
                return RingWake;
            int then = Voice.Hash(seed + "|ringanswer2") % 100;   // a fresh roll, so waking one doesn't take its share from any one of the rest
            return then < 45 ? RingForward : then < 75 ? RingClose : RingKneel;
        }

        public static string ForwardLine(string seed)
        {
            return Voice.Pick(seed + "|forward", ForwardLines);
        }

        // ---- the crowd round a fight (FIVEDAY2 R10; [Scenes] FightCrowds with [Mind] CrowdPicks): one decision for the crowd, how it takes this fight
        public const string CrowdEgg = "egg", CrowdStop = "stop", CrowdLaw = "law", CrowdHush = "hush";
        public static readonly string[] CrowdMoods = { CrowdEgg, CrowdStop, CrowdLaw, CrowdHush };

        private static readonly Dictionary<string, string> CrowdWordsOf = new Dictionary<string, string>
        {
            { CrowdEgg, "eggs them on, shouting and laughing" },
            { CrowdStop, "starts calling for someone to stop it" },
            { CrowdLaw, "starts shouting for the law" },
            { CrowdHush, "goes quiet, and watches" },
        };

        public static string CrowdWords(string id)
        {
            string w;
            return CrowdWordsOf.TryGetValue(id, out w) ? w : id;
        }

        public static string CrowdBrief(int year)
        {
            return "You decide how a crowd takes a fistfight in the street. It is " + (year > 0 ? year : 1899) + ", in the American West and South. Choose the one this crowd would do. Answer with its number alone.";
        }

        public static LanguageRequest CrowdRequest(int year, int hour, int people, int fighting, string town, string place, bool night)
        {
            var r = new LanguageRequest
            {
                Kind = "pick", CrowdPick = true, Name = "A crowd of " + people, Town = town, Place = place ?? "the street", Hour = hour, Year = year, Mood = night ? "loose, the drink talking" : "restless",
                Now = (fighting >= 3 ? fighting + " people are fighting" : "two people are fighting") + ", a few steps away",
            };
            foreach (string id in CrowdMoods)
                r.Options.Add(CrowdWords(id));
            return r;
        }

        /// The rules' answer: a brawl draws cheers, a fight in the day draws the law sooner, a quiet crowd now and then.
        public static string CrowdRules(int fighting, bool night, string seed)
        {
            int roll = Voice.Hash(seed + "|crowd") % 100;
            if (roll < (night ? 60 : 35) + (fighting >= 3 ? 15 : 0))
                return CrowdEgg;
            if (roll < 70)
                return CrowdStop;
            return roll < 88 ? CrowdLaw : CrowdHush;
        }

        /// How the crowd sounds, by its mood: the recorded voice's kind for one of them, or null (hush: nothing).
        public static string CrowdVoice(string mood, string seed)
        {
            switch (mood)
            {
                case CrowdStop: return "defuse";
                case CrowdLaw: return "scared";
                case CrowdHush: return null;
                default: return Voice.Chance(seed + "|m", 33) ? "mock" : "cheer";
            }
        }

        public static IEnumerable<string> AllRingLines()
        {
            foreach (string l in ForwardLines)
                yield return l;
        }

        /// The first friend's table after a fight, with another friend near: tend him alone, fetch the other and close round
        /// him, or leave it. The loyal and the proud close round; the timid leave; a sore mood leaves sooner.
        public static Staged.Weights CloseRoundWeights(StagedMind m, int friendsNear)
        {
            string t = m != null ? m.Temper : null;
            int tend = 3, close = friendsNear >= 2 ? 4 : 1, leave = 1;
            switch (t)
            {
                case "Loyal": close += 2; leave = 0; break;
                case "Proud": close += 2; break;
                case "Hothead": close += 1; break;
                case "Coward": leave += 3; tend -= 1; close -= 1; break;
                case "Cold": leave += 1; close -= 1; break;
                case "Drifter": leave += 1; break;
            }
            if (m != null && m.Mood == "sore") leave += 1;
            if (m != null && m.Warmth >= 2) { close += 1; leave = Math.Max(0, leave - 1); }
            var w = new Staged.Weights();
            w.Add("tend", Math.Max(1, tend)).Add("close", Math.Max(0, close)).Add("leave", Math.Max(0, leave));
            return w.Clamp();
        }

        /// The option as the model is offered it.
        public static string Words(string id, string otherFirst)
        {
            switch (id)
            {
                case "close": return "fetch " + (otherFirst ?? "the other") + " and close round him";
                case "leave": return "let it be; he would only hate the fuss";
                default: return "go to him yourself and see to him";
            }
        }

        public static string Now(string loserFirst)
        {
            return (loserFirst ?? "A friend of yours") + " is down in the street after a fight, and you saw it.";
        }

        /// Two friends close round the man who went down: A the first friend, B the one down, C the second friend. C comes over,
        /// A and C each say their piece, B answers by his temper, is got up, and A walks him off past C, who goes his own way.
        public static Scene CloseRound(SceneCast c, string seed)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string bt = c.Tempers != null && c.Tempers.Length > 1 ? c.Tempers[1] : null;
            Scene s = new Scene { Kind = "comfort", Summary = "two friends closed round a beaten friend" };
            s.Beats.Add(new Beat(2, null, null, "approach", 200) { To = 1 });
            s.Add(0, Voice.Pick(seed + "|ca", "Easy, " + b + ". Easy. We've got you.", "Hold still, " + b + ". Let me see your head.", b + "? " + b + ". Look at me. There."), "mild", "face");
            s.Beats.Add(new Beat(2, Voice.Pick(seed + "|cc", "Who did this? ...Never mind. Later.", "Lord, look at him. Get his other arm.", "Is he... " + b + ", can you hear us?"), "angry") { To = 1 });
            string answer = bt == "Hothead" || bt == "Proud" ? Voice.Pick(seed + "|cb", "Get off me. I'm fine. I'm fine.", "Took him a while, didn't it. Took him a while.")
                          : bt == "Coward" ? Voice.Pick(seed + "|cb", "Don't leave me. Don't let him come back.", "...I didn't do anything. I didn't.")
                          : Voice.Pick(seed + "|cb", "...I'll live. Help me up.", "Thank God it's you two.");
            s.Add(1, answer, bt == "Coward" ? "scared" : "mild");
            s.Add(1, null, null, "getup", 600);
            s.Add(0, Voice.Pick(seed + "|cw", "Come on. We'll see you home.", "Lean on me. " + a + "'s got you.", "That's it. One foot, then the other."), "mild");
            s.Beats.Add(new Beat(0, null, null, "leavewith") { To = 1 });
            s.Beats.Add(new Beat(1, null, null, "leavewith") { To = 1 });   // and the man himself, beside A (both name B, so the Director's spot is the same point); without this beat only A walked (the review of items 20-27)
            s.Add(2, null, null, "walkaway", 800);
            return s;
        }

        /// The onlooker who leaves takes their own with them: the word to the spouse or sweetheart.
        public static string TakeAway(string partnerFirst, bool partnerWoman, string seed)
        {
            string p = partnerFirst ?? (partnerWoman ? "my dear" : "love");
            return Voice.Pick(seed + "|ta", "Come away, " + p + ". It's none of ours.", "Don't look, " + p + ". Walk.", p + ". Home. Now.", "Take my arm, " + p + ". We're going.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 6; i++)
            {
                yield return TakeAway("Martha", true, "t" + i);
                yield return TakeAway("Amos", false, "u" + i);
                yield return TakeAway(null, true, "v" + i);
            }
            foreach (string id in Picks)
                yield return Words(id, "Clem");
            yield return Now("Abner");
        }
    }
}
