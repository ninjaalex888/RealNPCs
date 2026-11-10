// A revenant the model can ask (FIVEDAY2 items 11-13; Lex, 2026-10-31: "once within the model's reach of the player the model takes over their choices, intent most
// likely to kill him"). A returned dead man has no Person, only his record: who killed him, how, where, and what he held against you first. That's all he's
// got, and it's enough. Every few seconds while he's near, what he does next is put to the model as a short numbered list, the stalker's own moves; a slow or
// empty answer, or no model at all, and the rules choose from the same list. Why it reads true: grievance outlives the wound. What comes back is the unfinished
// business, and it only wants one thing. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class RevenantMind
    {
        public const string Follow = "follow", Stare = "stare", Circle = "circle", Rush = "rush", Leave = "leave", Attack = "attack";
        public const int ThinkEveryMs = 22000, AskWaitMs = 3500, MostAsks = 7;
        public const float ReachMetres = 60f, AttackMetres = 25f;

        private static readonly Dictionary<string, string> Words = new Dictionary<string, string>
        {
            { Follow, "keeps after him, out of sight" },
            { Stare, "stands where he can be seen, and stares" },
            { Circle, "comes in close and walks round him" },
            { Rush, "comes straight at him, and stops a few steps short" },
            { Leave, "turns and goes, finished with him for now" },
            { Attack, "goes for him, to kill him" },
        };

        public static string WordsOf(string id)
        {
            string w;
            return Words.TryGetValue(id, out w) ? w : id;
        }

        /// What he might do now. Far off, keeping after him is all there is to do; close in, all of it. Once he's stayed a while, he may leave.
        public static List<string> Menu(float metres, int asks, bool hunt = false)
        {
            var m = new List<string> { Follow };
            if (hunt && metres < AttackMetres && asks >= 2)
                m.Add(Attack);
            if (metres < 45f)
                m.Add(Stare);
            if (metres < 30f)
            {
                m.Add(Circle);
                m.Add(Rush);
            }
            if (asks >= 3)
                m.Add(Leave);
            return m;
        }

        /// What the model is told it's doing.
        public static string Brief(int year)
        {
            return "You decide what one of the dead does next. It is " + year + ", in the American West and South. This person was killed, came back, and has found the one who did it. " +
                   "You are given how they died and what they held against the one who killed them, and a short list of what they might do. Choose the one this person would do. Answer with its number alone.";
        }

        /// The request: how he died, and what's left of him.
        public static LanguageRequest Request(Fallen f, string nearestTown, int hour, int year, float metres, List<string> menu)
        {
            string who = f.Name ?? f.Label ?? (f.Woman ? "a woman" : "a man");
            string place = !string.IsNullOrEmpty(f.Town) ? f.Town : !string.IsNullOrEmpty(f.Zone) ? f.Zone : "the road";
            var r = new LanguageRequest
            {
                Kind = "pick", Revenant = true, WithThought = true, Name = who, Woman = f.Woman, Trade = f.Label, Town = nearestTown, Hour = hour, Year = year,
                Feeling = "anger", Strength = "strong", About = "you", YouAs = "the one who killed you", Shows = "shown", Mood = "cold, and in no hurry",
                Now = "you died, and came back, and you have found the one who did it, " + (int)metres + " metres off"
            };
            r.Memories.Add("killed in " + place + (string.IsNullOrEmpty(f.Weapon) ? "" : " with " + f.Weapon) + (f.Headshot ? ", with a bullet through the skull" : "") + (f.Begged ? ", begging at the end" : ""));
            if (f.Held != null)
                r.Memories.Add("before that: " + f.Held);
            r.Options = menu.Select(WordsOf).ToList();
            return r;
        }

        // ---- his own words (FIVEDAY2 item 12): the model's when it gave some and they pass, these when it didn't. All original to this add-on.

        private static readonly Dictionary<string, string[]> Said = new Dictionary<string, string[]>
        {
            { Stare, new[] { "Go on. Have a good look.", "You didn't look this long the first time.", "I'm still here. I expect you'd noticed." } },
            { Circle, new[] { "You walk different than I remember.", "Round and round. I've had practice.", "Stand still. I did." } },
            { Rush, new[] { "There. That's how close you were.", "You can smell it, can't you? The ground.", "I wanted to see if you'd flinch." } },
            { Leave, new[] { "Not today. I've got time. I've got nothing but.", "Sleep well, then. See if you can.", "I'll know where to find you." } },
        };

        /// What he says as he does it, from what he holds, or null: nothing to say.
        public static string Says(string what, Fallen f, string seed)
        {
            if (what == Follow)
                return null;
            if (f != null && f.Held != null && Voice.Chance(seed + "|held", 30))
                return Voice.Pick(seed + "|heldw", Grammar.UpperFirst(f.Held) + ". You'll have forgotten.", "It was never the bullet. " + Grammar.UpperFirst(f.Held) + ".");
            string[] pool;
            return Said.TryGetValue(what, out pool) ? Voice.Pick(seed + "|said", pool) : null;
        }

        /// How likely he is to go for you, from the setting and what he has to hold against you (FIVEDAY2 item 13): someone with a grievance from before, someone who begged, someone back more than once.
        public static int AttackChance(int setting, Fallen f)
        {
            if (f == null || setting <= 0)
                return setting;
            int c = setting + (f.Held != null ? 20 : 0) + (f.Begged ? 15 : 0) + (f.Headshot ? 5 : 0) + 10 * Math.Min(2, f.Returned) + Vengeance.AttackShift(f.Anger);   // and their temper (Core/Vengeance.cs; 0 when not known)
            return Math.Max(0, Math.Min(100, c));
        }

        /// As he comes.
        public static string Attacks(Fallen f, string seed)
        {
            string name = f != null && f.Name != null ? f.Name : "me";
            return Voice.Pick(seed + "|att", "Now you can do it twice.", "This time I've got the first shot.", "Say my name. Go on. Say it. It's " + name + ".", "I've had a long walk to do this.");
        }

        /// Word of him ahead of him: someone's been asking.
        public static string AskingAfter(Fallen f, string town, string seed)
        {
            return Voice.Pick(seed + "|ask", "Somebody at " + town + " has been wanting to know where you are. Those who saw him don't care to describe him.", "Somebody at " + town + " swears they saw a dead man buy a ticket.", "A man was asking the way to you at " + town + " and left no name.");
        }

        /// He got what he came for, or you put him down again.
        public static string Over(Fallen f, bool gotYou, string seed)
        {
            string who = f != null && f.Name != null ? f.Name : "Someone you killed";
            return gotYou ? who + " has had what they came back for." : Voice.Pick(seed + "|over", who + " is down again.", who + " is dead, again. It may take, this time.");
        }

        /// Whether, put down again, he stays down.
        public static bool StaysDown(string seed)
        {
            return Voice.Chance(seed + "|down", 60);
        }

        /// Whether he says anything this time: he isn't talkative.
        public static bool Speaks(string seed)
        {
            return Voice.Chance(seed + "|speaks", 65);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in Said.Values)
                foreach (string s in pool)
                    yield return s;
            yield return "You robbed me. You'll have forgotten.";
            yield return "It was never the bullet. You robbed me.";
        }

        /// The rules' choice from the same list, by how near he is and how many times he's been asked.
        public static string Rules(List<string> menu, float metres, int asks, string seed, int attackChance = 0)
        {
            int roll = Voice.Hash(seed + "|rev") % 100;
            if (menu.Contains(Attack) && Voice.Chance(seed + "|attack", attackChance))
                return Attack;
            if (menu.Contains(Leave) && roll < 12 + 6 * Math.Max(0, asks - 3))
                return Leave;
            if (menu.Contains(Circle))
                return roll < 34 ? Circle : roll < 58 ? Rush : roll < 80 ? Stare : Follow;
            return menu.Contains(Stare) && roll < 55 ? Stare : Follow;
        }
    }
}
