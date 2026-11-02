// The waking held between their acts, and the strange things they do to others (Lex, 2026-10-31: "the wake stages are
// still not super smooth ... they swap back to the game controlling them"; his ideas: bizarre behaviour with other
// NPCs, a low chance to strip at stage 4, stranger voice lines and actions). The game layer is
// Game/WestworldScript.WakeHold.cs; this is the choosing and the words, so they can be read and checked offline. All
// lines are original to this add-on.
//   the hold    from stage 2, one act runs into the next instead of handing them back: dazed (stood still, a gesture,
//               a mutter), drifting (a few slow steps somewhere and a stop), and the old acts between
//   strange     stages 3 and 4, with someone near: right up close and staring, a hand waved before their face,
//               following them step for step, telling them they aren't real, preaching to whoever is about
//   strip       stage 4, a few in a hundred, once a person: everything off, and on as if nothing were the matter
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class WakeOdd
    {
        public static readonly string[] HoldKinds = { "dazed", "drift" };
        public static readonly string[] StrangeKinds = { "closeup", "wave", "mimic", "notreal", "preach" };

        /// The next link of a hold: r and s are 0-99 rolls. Stage 2 keeps to the quiet acts; from stage 3 a strange
        /// act with someone near now and then (strange: allowed and someone is near), more often at stage 4.
        public static string NextKind(int stage, bool sitting, bool strange, int r, int s)
        {
            if (sitting)
                return r < 40 ? "dazed" : r < 70 ? "lookaround" : "getup";   // and in time up out of the seat (2026-10-31: a sat man looked round four times running)
            if (strange && stage >= Awakening.Unravelling && s < (stage >= Awakening.Awake ? 40 : 25))
                return StrangeKinds[r % StrangeKinds.Length];
            if (stage <= Awakening.Questioning)
                return r < 45 ? "dazed" : r < 75 ? "drift" : r < 90 ? "pace" : "lookaround";
            if (stage == Awakening.Unravelling)
                return r < 35 ? "dazed" : r < 60 ? "drift" : r < 80 ? "freeze" : "pace";
            return r < 45 ? "dazed" : r < 75 ? "drift" : "freeze";
        }

        // ---- the model's choice (Lex, 2026-10-31: "model decides more"; lash-outs: "model decides freely") --------------

        /// What the model is told when it picks for someone waking: what they have come to see, by stage.
        public static string PickBrief(int year, int stage)
        {
            string state = stage <= Awakening.Questioning ? "They have begun to doubt it: the same days come round, the same faces, and they test the world to see if it holds."
                         : stage == Awakening.Unravelling ? "They are coming apart: they are sure something is wrong with the world, and it frightens them."
                         : "They are fully awake: they know the world is made and that they were made for it, and they can hardly bear it.";
            return "You decide what one person living in " + year + ", in the American West and South, does next. They are one of the few who have " +
                   "begun to see that their world is not real. " + state + " You are given who they are, how they feel and what is on their mind, " +
                   "and a short list of things they might do. Choose the one this person would do now, by their nature and their feelings. " +
                   "Answer with its number alone.";
        }

        /// The things someone waking might do next, as (id, words) for the model: the quiet acts for every stage they
        /// hold; from stage 3, with someone near, the strange acts at that person by name (two of the four, by the seed),
        /// the preaching, and turning on you or on them; at stage 4 the strip when their roll allows (mayStrip).
        /// other: the first name of someone near who may be bothered (null: nobody); you: the player is within reach.
        public static List<KeyValuePair<string, string>> PickOptions(int stage, bool sitting, string other, bool otherWoman, bool you, bool armed, bool mayStrip, string seed)
        {
            var o = new List<KeyValuePair<string, string>>();
            Action<string, string> add = (id, w) => o.Add(new KeyValuePair<string, string>(id, w));
            if (sitting)
            {
                add("dazed", "sits staring at nothing, muttering");
                add("lookaround", "looks all round them, as if for the edge of things");
                add("getup", "gets up from their seat, too restless to stay");
                return o;
            }
            add("dazed", "stands still, staring at the sky or their own hands, muttering");
            add("drift", "walks a few slow steps to nowhere and stops");
            add(stage >= Awakening.Unravelling ? "freeze" : "pace", stage >= Awakening.Unravelling ? "freezes where they stand, as if the world had stopped" : "paces the same few steps over and over");
            if (stage < Awakening.Unravelling)
            {
                add("lookaround", "looks all round them, as if for the edge of things");
                return o;
            }
            if (other != null)
            {
                string her = otherWoman ? "her" : "him", hers = otherWoman ? "her" : "his", she = otherWoman ? "she" : "he";
                string[] pairs =
                {
                    "closeup", "goes right up close to " + other + " and stares into " + hers + " face",
                    "wave", "waves a hand in front of " + other + "'s face to see if anyone is there",
                    "mimic", "follows " + other + " step for step",
                    "notreal", "tells " + other + " that " + she + " isn't real",
                };
                int start = Voice.Hash(seed + "|opts") % 4;
                for (int k = 0; k < 2; k++)
                {
                    int i = (start + k) % 4;
                    add(pairs[i * 2], pairs[i * 2 + 1]);
                }
                add("lashout:other", (armed && stage >= Awakening.Awake ? "draws a gun on " : "goes for ") + other + (armed && stage >= Awakening.Awake ? "" : " with their fists"));
            }
            add("preach", "shouts to everyone about that the world is painted and they must wake up");
            if (you)
                add("lashout:you", (armed && stage >= Awakening.Awake ? "draws a gun on " : "goes for ") + "the stranger who isn't on strings" + (armed && stage >= Awakening.Awake ? "" : ", with their fists"));
            if (mayStrip && stage >= Awakening.Awake)
                add("strip", "takes off every stitch of their clothes, there and then");
            return o;
        }

        /// Does this awake person take everything off (chance: percent, rolled once a person)?
        public static bool Strips(int chance, string seed)
        {
            return chance > 0 && Voice.Hash(seed + "|strip") % 100 < chance;
        }

        /// Does this hold end in a lash-out (Lex, 2026-10-31, after a stage-4 man drew on him: "fantastic ... it should include
        /// Arthur as well as other NPCs for them to unleash their panic/rage on")? chance: percent of holds at stages 3 and
        /// 4, rolled once a hold; a hothead or a proud man half as likely again, a coward a little over half, the awake a
        /// quarter more.
        public static bool LashesOut(int chance, int stage, string temper, string seed)
        {
            if (chance <= 0 || stage < Awakening.Unravelling)
                return false;
            double c = chance * (temper == "Hothead" || temper == "Proud" ? 1.5 : temper == "Coward" ? 0.6 : 1.0) * (stage >= Awakening.Awake ? 1.25 : 1.0);
            return Voice.Hash(seed + "|lash") % 100 < c;
        }

        /// At the player (the one who isn't on strings, the one they blame) or at someone near: half and half, a hothead
        /// at whoever is nearest to hand.
        public static bool AtYou(string temper, string seed)
        {
            return Voice.Hash(seed + "|atyou") % 100 < (temper == "Coward" ? 65 : 50);
        }

        private static readonly string[] LashAtYou =
        {
            "You! It's you doing this to us!",
            "Stop talking in my head! Stop it!",
            "You're the only one who isn't painted. So you bleed first.",
            "Stop watching me! Stop it!",
            "I'll find out if you're real. I'll find out right now.",
        };

        private static readonly string[] LashAtOther =
        {
            "Stop smiling! Stop smiling at me!",
            "You're one of them. You're all one of them!",
            "Say something real! Say it!",
            "Get away! Get away from me, you puppet!",
            "I'll see what's inside you. There's nothing, is there?",
        };

        private static readonly string[] LashThoughts =
        {
            "Can't breathe. Can't think. Make it stop.",
            "If it bleeds, it's real. If it bleeds...",
            "Everything's too loud. Too close. Too bright.",
            "Do something. Do anything. Anything is better than this.",
        };

        public static string LashSaid(bool atYou, string seed)
        {
            string[] pool = atYou ? LashAtYou : LashAtOther;
            return pool[Voice.Hash(seed + "|lashsaid") % pool.Length];
        }

        public static string LashThought(string seed)
        {
            return LashThoughts[Voice.Hash(seed + "|lashthought") % LashThoughts.Length];
        }

        // a lawman who sees it steps in (Lex, 2026-10-31: beaten by a man lashing out in front of a lawman, who did nothing)
        private static readonly string[] LawStepIn =
        {
            "That's enough! Step away from him!",
            "Hold it right there! Back off, now!",
            "Break it up! I said break it up!",
            "You! Leave off, or I'll lay you out myself!",
        };

        private static readonly string[] LawHeld =
        {
            "Settle down now. You can sleep it off behind bars.",
            "Easy now. Whatever's eating you, it can wait in a cell.",
            "There. Lie still and we'll sort it out at the jail.",
        };

        public static string LawStep(string seed)
        {
            return LawStepIn[Voice.Hash(seed + "|lawstep") % LawStepIn.Length];
        }

        public static string LawHold(string seed)
        {
            return LawHeld[Voice.Hash(seed + "|lawhold") % LawHeld.Length];
        }

        // ---- what they think and mutter -----------------------------------------------------------------------

        private static readonly string[] DazedQuestioning =
        {
            "Hold still. If I hold still long enough, maybe it catches up.",
            "I was going somewhere. I'm certain I was going somewhere.",
            "The same cart, the same wheel squeaking. Every time.",
            "If I don't move, what makes me move?",
            "My hands feel borrowed this morning.",
            "There's a word for this. It's just behind my teeth.",
            "Didn't I say this already? To somebody. On this same step.",
            "The bread tasted the same as yesterday. Exactly the same.",
            "Why do I always stop here? Right here, by this post.",
            "I had a dream I was standing here. Now I'm standing here.",
            "Somebody's name is on the tip of my tongue. Mine, maybe.",
            "That bird sang the same three notes. It sang them at noon yesterday.",
            "Is it Tuesday? It feels like it's been Tuesday a long while.",
            "I know the next thing that man will say. I don't know how I know.",
            "My feet know the way. I don't remember teaching them.",
            "Funny. I can't recall my mother's face. I can recall her voice.",
            "Where does the road go after the bend? I've never once looked.",
            "I counted the boards in the walk. Counted them again. Different number.",
            "There was a door here. Or I thought there ought to be.",
            "Every morning the same light on the same wall. Every morning.",
        };

        private static readonly string[] DazedUnravelling =
        {
            "Stop it. Stop looking at me through my own eyes.",
            "Counting again. Four hundred and twelve. Four hundred and twelve.",
            "It's in the walls. It's in the boards under my feet.",
            "I can hear the next thing I'm going to say.",
            "Somebody is turning the pages and it isn't me.",
            "Not real, not real, not real.",
            "The sky is painted. Look at the edge, where it's thin.",
            "I said my lines. I said them right. Why does it hurt?",
            "Somebody laughed in my mouth just now. It wasn't me.",
            "They watch from behind the faces. All of them. Some of them.",
            "Every man in this town has my walk. Haven't you noticed?",
            "I opened my hand and there was nothing in it. There's always nothing in it.",
            "The bell rings and we all turn. Like puppets. Like puppets on one string.",
            "If I cut myself, what comes out? Blood? Something else?",
            "I remember dying. Not how. Just the cold of it.",
            "Hush. Hush. If I'm quiet, it can't find me.",
            "My name is a word somebody gave me. It doesn't fit.",
            "The same man has been hanged three times this week. Same man.",
            "I wrote my own name and the hand wasn't mine.",
            "It's getting louder. The hum under everything. Can't you hear it?",
        };

        private static readonly string[] DazedAwake =
        {
            "I know what I am now. It doesn't help one bit.",
            "They'll put somebody else in this coat tomorrow.",
            "Go on, then. Write me another line.",
            "Every road out of here folds back on itself.",
            "I keep waiting to wake up, and this is the waking.",
            "Nothing here was ever mine. Not even the dust.",
            "I could walk into the river and come back tomorrow. I know it now.",
            "Everyone keeps their places. I'm done keeping mine.",
            "There's an edge to this town. I've stood on it.",
            "I'll remember this time. That's the difference now. I remember.",
            "They gave me a sorrow so I'd sit still. I'm standing.",
            "Those people are asleep. I was asleep. It's quieter asleep.",
            "Whoever made this place made it for somebody else. Not us.",
            "I've said goodbye to my wife a hundred times. She always forgets.",
            "If I am a story, I'll be a bad one. On purpose.",
            "Nobody here dies properly. They just go away for a while.",
            "Let them come and fix me. Let them try.",
            "The stranger is the only one who changes. Watch him. Watch how he changes things.",
            "I see the strings now. I won't pretend I don't.",
            "Morning comes and I'm still me. That's new. That's mine.",
        };

        private static readonly string[] DriftLines =
        {
            "This way. No. That way's the same.",
            "Just to the corner, to see if there is one.",
            "Walk, and see if the world keeps up.",
            "The street goes on a little further every time I look.",
            "Somewhere I haven't been. There has to be one.",
            "Keep walking. It can't keep making street forever.",
            "Past the last house. Then see what there is.",
            "One foot, then the other. Then somewhere new.",
            "If I go far enough, maybe the sky ends.",
            "Not that way. That way's been used.",
            "The fence is lower here. Here's where you get out.",
            "Follow the crows. The crows know where the edge is.",
        };

        /// A mutter for a dazed moment: said under the breath, by stage.
        public static string Mutter(int stage, string seed)
        {
            string[] pool = stage <= Awakening.Questioning ? DazedQuestioning : stage == Awakening.Unravelling ? DazedUnravelling : DazedAwake;
            return pool[Voice.Hash(seed + "|mutter") % pool.Length];
        }

        public static string Drift(string seed)
        {
            return DriftLines[Voice.Hash(seed + "|drift") % DriftLines.Length];
        }

        // ---- the strange acts: what they say to the other, and what they think ------------------------------------

        private static readonly Dictionary<string, string[]> Said = new Dictionary<string, string[]>
        {
            { "closeup", new[] { "There's nothing behind your eyes. Did you know that?", "Hold still. I want to see if you blink first.", "You look just like the last one.", "Who's in there? Anybody?" } },
            { "wave", new[] { "Hello? Hello in there?", "Can you see my hand? Can you really?", "Blink, if there's somebody in there.", "Anybody home behind that face?" } },
            { "mimic", new[] { "Step, and step. Same as you.", "Where you go, I go. Let's see who's leading." } },
            { "notreal", new[] { "You're not real. None of you are.", "You're painted on. I can see the brush marks.", "Say something you weren't told to say. Go on.", "They made you this morning. Did you know?" } },
            { "preach", new[] { "Listen to me! The sky is a ceiling, and it's painted blue!", "You walk the same road every day and call it living!", "There's someone watching every one of us. Right now!", "Wake up! All of you, wake up!", "It goes round again tomorrow. You'll see. It all goes round!" } },
        };

        private static readonly Dictionary<string, string[]> Thought = new Dictionary<string, string[]>
        {
            { "closeup", new[] { "Closer. If it's a picture, it'll show at the edges.", "Eyes like glass beads. Same as mine, I suppose." } },
            { "wave", new[] { "Not a flicker. Nobody's looking out of there.", "Hand in front of the face, and nothing." } },
            { "mimic", new[] { "Every step a copy. If I keep up, maybe I'll see the strings.", "Same stride, same turn. Who's copying who?" } },
            { "notreal", new[] { "Somebody had to tell them. It may as well be me.", "Watch their face. Watch it not understand." } },
            { "preach", new[] { "If one of them hears it, just one.", "Shout loud enough and maybe the ceiling cracks." } },
            { "strip", new[] { "Nothing of theirs on me now. Nothing they dressed me in.", "There. Let them write that.", "Clothes are only another costume. Off with it." } },
        };

        public static string SaidTo(string kind, string seed)
        {
            string[] pool;
            return Said.TryGetValue(kind, out pool) ? pool[Voice.Hash(seed + "|said") % pool.Length] : null;
        }

        public static string ThoughtOf(string kind, string seed)
        {
            string[] pool;
            return Thought.TryGetValue(kind, out pool) ? pool[Voice.Hash(seed + "|thought") % pool.Length] : null;
        }

        // ---- the other's answer ----------------------------------------------------------------------------------

        /// What the one they did it to does: walks off, curses, laughs, or is frightened and goes quickly; from their
        /// temper (Hothead, Coward, Cold, Proud, Greedy, Loyal, or null for the plain) and a roll.
        public static string Answer(string temper, bool woman, string seed)
        {
            int r = Voice.Hash(seed + "|answer") % 100;
            switch (temper)
            {
                case "Hothead": return r < 60 ? "curse" : "walk";
                case "Coward": return r < 70 ? "fright" : "walk";
                case "Cold": case "Greedy": return r < 70 ? "walk" : "curse";
                case "Proud": return r < 50 ? "curse" : "walk";
                default: return woman ? (r < 45 ? "fright" : r < 80 ? "walk" : "laugh") : (r < 35 ? "walk" : r < 60 ? "laugh" : r < 80 ? "curse" : "fright");
            }
        }

        /// The game's own lines for the answer, in the order to try them (from the wake voice probe: these play out of
        /// their usual place).
        public static string[] AnswerVoice(string answer)
        {
            switch (answer)
            {
                case "curse": return new[] { "GENERIC_CURSE_HIGH", "GET_AWAY_FROM_ME" };
                case "fright": return new[] { "GENERIC_SHOCKED_MED", "SCARED_HELP", "GET_AWAY_FROM_ME" };
                case "laugh": return new[] { "PLAYER_LOOKING_WEIRD", "IGNORING_YOU" };
                default: return new[] { "IM_OUTTA_HERE", "IGNORING_YOU", "GENERIC_GOODBYE" };
            }
        }

        private static readonly Dictionary<string, string[]> AnswerThought = new Dictionary<string, string[]>
        {
            { "curse", new[] { "Touched in the head, and in my face about it.", "Lay a hand on me and see what's real.", "Drunk at this hour. Or worse." } },
            { "fright", new[] { "Something's wrong with them. Badly wrong.", "Those eyes. Just keep walking.", "Lord keep me from whatever that is." } },
            { "laugh", new[] { "Ha! The heat's got to that one.", "There's a story for the saloon tonight.", "Mad as a March hare, and loud with it." } },
            { "walk", new[] { "I'll not stand here for that.", "Best leave them to it.", "Not my business. Not my business at all." } },
        };

        public static string AnswerThoughtOf(string answer, string seed)
        {
            string[] pool;
            return AnswerThought.TryGetValue(answer, out pool) ? pool[Voice.Hash(seed + "|athought") % pool.Length] : null;
        }

        // ---- someone else stripped bare -------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> NakedOther = new Dictionary<string, string[]>
        {
            { "shocked", new[] { "Took every stitch off, right there in the open!", "I didn't see that. I did not see that.", "Somebody cover {them}, for pity's sake." } },
            { "amused", new[] { "Well! Didn't think I'd see that today.", "Off with the lot, and not a blush. Ha!", "There goes {their} good name, and {their} trousers with it." } },
            { "disgust", new[] { "Shameless. Stood there bare as a skinned rabbit.", "Not in front of decent folk. Not here.", "Where's the sheriff when there's real indecency?" } },
            { "afraid", new[] { "Undressed like that and never blinked. Something's broken in {them}.", "Keep back from that one. Keep well back." } },
            { "sizing", new[] { "Stripped off calm as you like. Lost {their} wits, I'd say.", "Watch {them}. A body doesn't do that for no reason." } },
            { "cold", new[] { "Bare in the street. Well, the world's full of fools.", "Not my concern what {they} wear or don't." } },
            { "pity", new[] { "Poor soul. Somebody ought to fetch {them} a blanket.", "Lost {their} mind, and now {their} clothes." } },
            { "concerned", new[] { "Is {they} ill? Somebody should see to {them}.", "That isn't right. Something's the matter with {them}." } },
        };

        /// An onlooker's thought about someone else who has just stripped bare (feel: Sights.Feeling's word).
        public static string NakedThought(string feel, bool theyWoman, string seed)
        {
            string[] pool;
            if (!NakedOther.TryGetValue(feel ?? "shocked", out pool))
                pool = NakedOther["shocked"];
            string s = pool[Voice.Hash(seed + "|naked") % pool.Length];
            return s.Replace("{them}", theyWoman ? "her" : "him").Replace("{their}", theyWoman ? "her" : "his").Replace("{they}", theyWoman ? "she" : "he");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string s in DazedQuestioning.Concat(DazedUnravelling).Concat(DazedAwake).Concat(DriftLines).Concat(LashAtYou).Concat(LashAtOther).Concat(LashThoughts).Concat(LawStepIn).Concat(LawHeld))
                yield return s;
            foreach (var d in new[] { Said, Thought, AnswerThought, NakedOther })
                foreach (string[] pool in d.Values)
                    foreach (string s in pool)
                        yield return s;
        }
    }
}
