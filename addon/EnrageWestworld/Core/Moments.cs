// What is happening to a person this very moment, and what they think of it (Lex, 2026-11-03: a man fighting Arthur thought
// "Can't complain"; "greatly improve this inner thought functionality... aware of their surroundings"). The game side reads
// the moment off each person as their turn on the ticker comes (Game/WestworldScript.Moments.cs: in combat with you or with
// someone else, fists or guns, fleeing, knocked down, hurt, a gun of yours pointed at them; shots, a body or a fight close
// by; and the quieter side: riding, driving, drinking, eating, smoking, at their work, sat or leaning, walking by day or in
// the dark, running, standing about, drunk, a gun in hand; how you look beside them). The loud moments (Urgent) are thought
// before anything else, awake or asleep, written even with the AI model on (its answer would come after the fight), with a
// seed that turns over every few seconds so the line moves with the fight. The quiet ones join the mind's candidates
// (Engine.Minds.Candidates, "moment:<what>") above the timeless fillers and below a worry or a fresh memory, or take a turn
// in the old roll without the minds (InnerLife.Thought). The model is told the moment in a few words (Words: "Right now:"
// in Language.Describe). Pools by the moment and by temper group (Minds.TemperGroup: bold, fearful, cold, plain), each line
// short enough for the ticker. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    /// A person's moment, as the game side read it (null fields and false flags where nothing is known).
    public sealed class Moment
    {
        public string Doing;    // fighting-you, fighting, fleeing, down, hurt, aimed-at; riding, driving, swimming, drinking, eating, smoking, working, resting, walking, running, standing
        public bool Melee;      // the fight is fists (Doing fighting-you or fighting); else guns
        public bool Armed;      // a gun in their hand
        public bool Drunk, Hurt, Night, Rain, Indoors, Woman;
        public string Place;    // the kind of place: saloon, church, store, hotel... (null: the street)
        public int FightNear;   // others in combat close by (not this person)
        public bool BodyNear;   // a dead person close by
        public bool ShotsNear;  // gunfire close by in the last few seconds
        public bool YouNear, YouArmed, YouAiming, YouMounted, YouWanted;
        public string YouLook;  // how you look beside them: naked, bloodied, undressed, drunk, filthy, soaked; null ordinary
    }

    public static class Moments
    {
        public const int UrgentSeedMs = 8000;   // the loud moments' seed turns over this often (real time): the line moves with the fight

        // ---- what is loud -------------------------------------------------------------------------------------

        /// The loud moments: thought before anything else.
        public static bool Urgent(Moment m)
        {
            return Loudest(m) != null;
        }

        /// The loudest thing about the moment, by name (the pool's key), or null when nothing is loud. Knocked down and hurt
        /// are loud only while the trouble is still on (shots or a fight near): health doesn't come back, and a man who took
        /// a bullet last week isn't "Lord, it hurts" every turn for the rest of his life (the advisor); after, a quiet thought.
        public static string Loudest(Moment m)
        {
            if (m == null)
                return null;
            bool troubleOn = m.ShotsNear || m.FightNear > 0;
            switch (m.Doing)
            {
                case "fighting-you": return m.Melee ? "fightyou:fists" : "fightyou:guns";
                case "fleeing": return "fleeing";
                case "fighting": return m.Melee ? "fight:fists" : "fight:guns";
                case "aimed-at": return "aimed";
                case "down":
                    if (troubleOn)
                        return "down";
                    break;
                case "hurt":
                    if (troubleOn)
                        return "hurt";
                    break;
            }
            if (m.YouAiming)
                return "aimed";
            if (m.ShotsNear)
                return "shots";
            if (m.FightNear > 0)
                return "watching";
            if (m.BodyNear)
                return "body";
            if (m.YouWanted && m.YouNear && m.YouArmed)
                return "wanted";
            return null;
        }

        /// A loud moment's thought (null when nothing is loud): the loudest thing, by the person's temper group; someone
        /// hurt in a fight now and then thinks of the hurt; the awake (stage two and up) now and then think of the fight the
        /// way the awake do. Not kept in the person's told list: the fight is the fight, and the seed turns over by itself.
        public static string Thought(Person p, Moment m, string seed, out string source, int wakeStage = 0)
        {
            source = null;
            string what = Loudest(m);
            if (what == null)
                return null;
            int g = GroupOf(p);
            string[] pool;
            if (what.StartsWith("fightyou:") && m.Hurt && Voice.Chance(seed + "fh", 35))
            {
                pool = FightHurt;
                what = "fight:hurt";
            }
            else if (wakeStage >= Awakening.Questioning && (what.StartsWith("fightyou:") || what.StartsWith("fight:")) && Voice.Chance(seed + "aw", 50))
            {
                pool = AwakeFight;
                what = "fight:awake";
            }
            else
                pool = PoolOf(what, g);
            if (pool == null || pool.Length == 0)
                return null;
            source = "moment:" + what;
            return Fill(pool[Voice.Hash(seed + "|" + what) % pool.Length], p);
        }

        // ---- what is quiet ------------------------------------------------------------------------------------

        /// The quiet side of the moment worth a thought, by name, or null: how you look beside them first (a bloodied man at
        /// your elbow is a thought), then drink or a gun in hand, then what they're at.
        public static string Quiet(Moment m)
        {
            if (m == null || Urgent(m))
                return null;
            if (m.Doing == "hurt" || m.Doing == "down")
                return m.Doing;   // the trouble over: still hurt, still on the ground, a thought of it among the rest
            if (m.YouNear && m.YouLook != null && Sights.States.Contains(m.YouLook))
                return "youlook";
            if (m.Drunk)
                return "drunk";
            if (m.Armed && m.Doing != "riding")
                return "armed";
            switch (m.Doing)
            {
                case "riding": case "driving": case "swimming": case "drinking": case "eating": case "smoking": case "working": case "resting": case "running":
                    return m.Doing;
                case "walking":
                    return m.Night && !m.Indoors ? "walking:night" : "walking";
                case "standing":
                    return "standing";
            }
            return null;
        }

        /// How hard a quiet moment pulls among the mind's candidates (Engine.Minds.Candidates): above the timeless fillers
        /// (14) and the day (16), below a worry or a fresh memory (45 and up).
        public static double Pull(string quiet)
        {
            switch (quiet)
            {
                case "hurt": case "down": return 40;
                case "youlook": return 38;
                case "drunk": return 36;
                case "armed": return 34;
                case "riding": case "driving": case "drinking": case "smoking": case "swimming": return 30;
                case "eating": case "working": case "running": case "walking:night": return 28;
                case "resting": return 25;
                default: return 22;   // walking, standing
            }
        }

        /// A quiet moment's thought (null when there is none): the person's told list keeps the same line from coming
        /// back soon, as the inner life's pools do.
        public static string Ambient(Person p, Moment m, string seed, List<string> told, out string source)
        {
            source = null;
            string what = Quiet(m);
            if (what == null)
                return null;
            string line;
            if (what == "youlook")
            {
                Sights.Look look = Sights.For(m.YouLook, p != null ? p.Personality : null, null, seed, p != null && p.Woman);
                line = look != null ? look.Thought : null;
                if (line == null)
                    return null;
            }
            else
            {
                string[] pool = PoolOf(what, GroupOf(p));
                if (pool == null || pool.Length == 0)
                    return null;
                line = Pick(pool, seed + "|" + what, told);
            }
            source = "moment:" + what;
            return Fill(line, p);
        }

        /// The moment in a few words for the AI model ("Right now: ..."), or null when there's nothing worth saying.
        public static string Words(Moment m)
        {
            if (m == null)
                return null;
            var w = new List<string>();
            switch (m.Doing)
            {
                case "fighting-you": w.Add(m.Melee ? "trading punches with the stranger" : "in a gunfight with the stranger, who is shooting at them"); break;
                case "fighting": w.Add(m.Melee ? "in a fistfight with someone" : "in a gunfight"); break;
                case "fleeing": w.Add("running for their life"); break;
                case "down": w.Add("knocked to the ground"); break;
                case "hurt": w.Add("hurt and bleeding"); break;
                case "aimed-at": w.Add("the stranger is pointing a gun at them"); break;
                case "riding": w.Add("on horseback"); break;
                case "driving": w.Add("driving a wagon"); break;
                case "swimming": w.Add("in the water"); break;
                case "drinking": w.Add("having a drink"); break;
                case "eating": w.Add("eating"); break;
                case "smoking": w.Add("having a smoke"); break;
                case "working": w.Add("at their work"); break;
                case "resting": w.Add("sat or leaning, resting"); break;
                case "running": w.Add("running"); break;
                case "walking": w.Add(m.Night && !m.Indoors ? "walking in the dark" : "walking"); break;
            }
            if (m.Doing != "aimed-at" && m.YouAiming)
                w.Add("the stranger is pointing a gun at them");
            if (m.Doing != "fighting-you" && m.Doing != "fighting" && m.Hurt)
                w.Add("hurt");
            if (m.ShotsNear)
                w.Add("gunfire close by");
            if (m.FightNear > 0 && m.Doing != "fighting" && m.Doing != "fighting-you")
                w.Add(m.FightNear > 1 ? "people fighting close by" : "a fight close by");
            if (m.BodyNear)
                w.Add("a dead body lies near");
            if (m.YouWanted && m.YouNear)
                w.Add(m.YouArmed ? "the stranger beside them is wanted by the law and has a gun out" : "the stranger beside them is wanted by the law");
            else if (m.YouNear && m.YouArmed && !m.YouAiming)
                w.Add("the stranger beside them has a gun in hand");
            if (m.YouNear && m.YouLook != null)
                w.Add("the stranger beside them looks " + m.YouLook);
            if (m.Drunk)
                w.Add("drunk");
            if (m.Armed && m.Doing != "fighting" && m.Doing != "fighting-you")
                w.Add("a gun in their own hand");
            return w.Count > 0 ? string.Join(", ", w) : null;
        }

        // ---- the pools ------------------------------------------------------------------------------------------

        private static int GroupOf(Person p)
        {
            switch (Minds.TemperGroup(p != null ? p.Personality : null))
            {
                case "bold": return 0;
                case "fearful": return 1;
                case "cold": return 2;
                default: return 3;
            }
        }

        private static string[] PoolOf(string what, int g)
        {
            string[][] byGroup;
            if (ByGroup.TryGetValue(what, out byGroup))
                return byGroup[Math.Max(0, Math.Min(3, g))];
            string[] one;
            return Shared.TryGetValue(what, out one) ? one : null;
        }

        private static string Fill(string line, Person p)
        {
            return line.Replace("{town}", p == null || string.IsNullOrEmpty(p.Home) ? "this town" : p.Home);
        }

        private static string Pick(string[] pool, string seed, List<string> told)
        {
            int start = Voice.Hash(seed) % pool.Length;
            for (int i = 0; i < pool.Length; i++)
            {
                string raw = pool[(start + i) % pool.Length];
                string key = "mo" + Voice.Hash(raw);
                if (told != null && told.Contains(key))
                    continue;
                if (told != null)
                {
                    told.Add(key);
                    if (told.Count > 40)
                        told.RemoveAt(0);
                }
                return raw;
            }
            return pool[start];
        }

        /// Every line (for the harness: length, period words).
        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] pools in ByGroup.Values)
                foreach (string[] pool in pools)
                    foreach (string l in pool)
                        yield return l;
            foreach (string[] pool in Shared.Values)
                foreach (string l in pool)
                    yield return l;
            foreach (string l in FightHurt)
                yield return l;
            foreach (string l in AwakeFight)
                yield return l;
        }

        public static IEnumerable<string> Kinds { get { return ByGroup.Keys.Concat(Shared.Keys); } }

        // hurt in a fight with you (any temper)
        private static readonly string[] FightHurt =
        {
            "Hit me again and I'll stop feeling it. Nearly there.", "Blood in my eye. Can't see his left coming.",
            "Something's broke in me. Keep swinging anyway.", "Every breath's a knife now. Make the next one count.",
            "He's winning. Doesn't mean he's won.", "Legs going. Don't let them. Not yet.",
            "I can taste it. Salt and copper. Never liked the taste.", "One more. I've got one more in me. Maybe two."
        };

        // the awake (stage two and up) in a fight: the fight real enough, the rest in doubt
        private static readonly string[] AwakeFight =
        {
            "Hit him. Hit him. If none of it's real, the pain's a good liar.", "I've done this before. This exact fight. I know what he does next.",
            "Fists feel real. Blood feels real. Then why don't I.", "Let him knock me down. I'll only get up again. I always get up again.",
            "Somebody's watching this. Not the street. Somebody above the street.", "Every blow lands where it landed last time. Every single one.",
            "Fight, because that's what the man I'm meant to be would do.", "If I die here I'll wake somewhere. I think. I hope."
        };

        // by temper group: 0 bold (Hothead, Proud), 1 fearful (Coward), 2 cold (Cold, Greedy), 3 plain (the rest)
        private static readonly Dictionary<string, string[][]> ByGroup = new Dictionary<string, string[][]>
        {
            { "fightyou:fists", new[] {
                new[] { "Come on then. Come on. I've been hit by better than you.", "Keep your chin up like that and I'll put it through your hat.",
                        "He's got weight on me. Weight's slow. Slow is how a man gets hurt.", "Swing again. I'll be waiting under it.",
                        "Best thing that's happened to me all week.", "Hands up, feet moving, like Pa showed me behind the barn.",
                        "Oh, he can take a punch. Let's see how many.", "Not backing off. Not from this one. Not from anybody.",
                        "Hit me in the mouth. Nobody hits me in the mouth.", "He's grinning. I'll fix the grin first." },
                new[] { "Why me. Why today. Why did I open my mouth.", "Keep my hands up. Keep them up. Don't go down.",
                        "If I fall he'll kick me. They always kick you when you're down.", "Somebody stop this. Somebody. Anybody.",
                        "My ear's ringing. Which hand hit me. I can't see his hands.", "Say sorry. Say anything. Make it stop.",
                        "God, let him tire before I do.", "I'm going to be sick. Not now. Not in front of him.",
                        "I don't fight. I don't know how to fight. Why am I fighting.", "He's enjoying it. Oh God, he's enjoying it." },
                new[] { "He leads with the right every time. Noted.", "Fists are a waste of a morning. This will cost him.",
                        "No anger in it. Just the work of taking him apart.", "He'll tire. They all tire. Then we'll see.",
                        "A tooth loose. He'll pay for the tooth.", "Blood on my cuff. That's a shirt ruined. That's on him too.",
                        "Let him swing himself empty. Patience is a kind of blade.", "I remember faces. I'll remember this one.",
                        "Hit where it counts and nowhere else. Economy.", "He wants me angry. He'll get something colder." },
                new[] { "Never thought I'd be in a street fight at my age.", "Lord, he hits hard. Keep standing. Just keep standing.",
                        "My wife'll see the bruises. What do I tell her.", "Haven't thrown a punch since I was sixteen. Still know how, it seems.",
                        "Don't fall. Whatever you do, don't fall in the mud.", "There's people watching. Make it count, then.",
                        "Is this about the look I gave him? That's all it was. A look.", "Breathe. Watch his hands. Breathe.",
                        "Knuckles split. Didn't know a hand could hurt from hitting.", "How does this end. Somebody tell me how this ends." } } },
            { "fightyou:guns", new[] {
                new[] { "He's shooting at me. Fine. I've got lead of my own.", "Reload, cover, reload. The old rhythm. Never forgot it.",
                        "If this is my day, I'll take him into the dirt with me.", "They'll talk about this in the saloon tonight. See that I'm there to hear it.",
                        "Breathe out. Squeeze. Don't yank it like a greenhorn.", "Hat's gone. He'll pay for the hat first.",
                        "Missed. Missed. Not a third time.", "He's good. I'm better. One of us is wrong." },
                new[] { "Shooting. He's shooting. Oh God, oh God, oh God.", "Where's the law. Where is anybody.",
                        "My hands won't stop shaking. Can't aim a thing.", "Run and he shoots my back. Stay and he shoots my front.",
                        "I never wanted a gun. Who put this gun in my hand.", "Let his jam. Let his jam. Let mine fire.",
                        "Don't look at the barrel. Look at the barrel. Which.", "I'm going to die in the street like a dog." },
                new[] { "Count his shots. Five. One left, or he's reloading.", "He's standing in the open like a fool. Good.",
                        "Aim at the middle. The middle doesn't move much.", "A dead man can't carry a grudge. That's the sum of it.",
                        "No hurry. Panic is what gets men killed, and I don't panic.", "He flinches when he fires. Watch the flinch.",
                        "Cover, then the shot. The order matters.", "Whatever this costs him, it'll cost him all of it." },
                new[] { "Never shot at a man before. God forgive me, I'm shooting at a man.", "Keep the post between us. Keep it there.",
                        "What did I do to him? I don't even know what I did.", "Mama, if you're watching, close your eyes.",
                        "The powder smell. I'll never get it out of this coat.", "Somebody tell my brother it wasn't my fault.",
                        "Can't stop shaking. Can't stop firing either.", "Please let the law come. Please let anybody come." } } },
            { "down", new[] {
                new[] { "Get up. Get up, damn you, get up.", "Only down. Not out. Not out yet.", "Ground's cold. Everything's cold. Get up.",
                        "Let him think it's over. Then show him.", "Mud in my mouth. I'll give him some back.", "Knocked down. Never knocked out. There's a difference." },
                new[] { "Stay down. Stay down and maybe he'll leave.", "Can't feel my legs. Can I feel my legs. Yes. Barely.",
                        "Don't look at me. Please don't look at me.", "Play dead. Animals play dead. It works for animals.",
                        "Everything's spinning. The sky's on the wrong side.", "If I don't move, I'm not here. If I'm not here, he's done." },
                new[] { "Down is not done. Count to five, then up.", "My own fault for not seeing it coming. Won't happen twice.",
                        "Let them laugh. Laughing men don't watch their backs.", "The ground. Fine. From here I can see every one of them.",
                        "A knee's gone sideways. That will mend. The score won't.", "Note who laughed. Note it well." },
                new[] { "Oof. What hit me. Something hit me.", "Lie here a minute. One minute. Then I'll see.",
                        "Everybody saw that. The whole town saw that.", "Is that my blood or the mud. Can't tell. Does it matter.",
                        "My hat. Where's my hat. A man can't get up without his hat.", "The boards smell of beer and feet. Get up. Get up." } } },
            { "fleeing", new[] {
                new[] { "Running isn't losing. Running is choosing the ground.", "I'll be back for this. With friends. With a gun.",
                        "Hate this. Hate every step of it.", "Live to fight tomorrow. That's not cowardice, that's arithmetic.",
                        "Fast, fast, don't look back. Looking back is how you trip.", "Nobody saw me run. Nobody who'll live to tell it." },
                new[] { "Run. Run. Don't stop. Don't ever stop.", "Which way is safe. There is no safe. Just run.",
                        "Please don't follow me. Please don't follow me.", "Lungs on fire. Keep going. Keep going.",
                        "Hide. Find a door. Any door. A hole in the ground.", "Don't trip. Don't trip. God, don't let me trip." },
                new[] { "A withdrawal. That's what they'd call it in the army.", "I'll remember who ran with me and who didn't.",
                        "Run now, settle later. The ledger's open.", "Panic makes a crowd. I'm not a crowd.",
                        "Fast and quiet. The fast and loud get shot.", "Round the corner, slow down, walk. A running man draws eyes." },
                new[] { "Why is this happening. Why today.", "Get home. Just get home. Lock the door.",
                        "Somebody shouted. I ran. Didn't even see what from.", "My side's splitting. Haven't run like this since I was a boy.",
                        "If I make the corner I'm all right. The corner. Just the corner.", "Keep the children inside. Keep everybody inside." } } },
            { "fight:fists", new[] {
                new[] { "Ha! He's slower than he looks.", "That one's for what you said. The next one's for free.",
                        "Felt that in my knuckles. Worth it.", "Come at me again. Go on. I dare you.", "Nobody talks to me like that. Nobody.",
                        "The whole street's watching. Good. Let them learn." },
                new[] { "I'm fighting. Why am I fighting. How did this start.", "Let me land one and then we can stop.",
                        "He's going to kill me. Over this. Over nothing.", "Don't let him see I'm scared. He can smell it.",
                        "They're laughing. Let them. Let me live.", "My glasses. If he breaks my glasses I can't work." },
                new[] { "He wanted this. He asked for it in plain words.", "Short and ugly. The only kind of fight worth having.",
                        "He'll remember who he picked on. That's the point.", "No wasted swings. Every one's a message.",
                        "He's bleeding. He'll stop soon. They always stop.", "Let him land one. Then he'll think he's winning." },
                new[] { "My mother would die if she saw this.", "Never been in a fight that made sense. This one's no different.",
                        "Just end it. Somebody end it.", "I don't even dislike the man. How did we get here.",
                        "He hit first. That counts for something. Doesn't it.", "Somebody's going to lose teeth. Please not me." } } },
            { "fight:guns", new[] {
                new[] { "Shooting back. Shooting back like a man.", "They picked the wrong street.", "Steady now. Steady. Make it count.",
                        "Smoke and noise and my blood up. I was made for this." },
                new[] { "Oh God, there's shooting and I'm in it.", "Hide. Fire. Hide. Don't think.", "I'm going to die. I'm going to die here.",
                        "Shoot at the noise. Just shoot at the noise." },
                new[] { "Noise and smoke. Underneath it, the same old arithmetic.", "He's wasting lead. Let him.", "Take the shot you have, not the one you want.",
                        "Whoever's paying for this, it's cheap at the price." },
                new[] { "Lord keep me. Lord keep me through this.", "I just wanted to get home. Now this.", "Somebody's going to be buried tomorrow. Not me, please.",
                        "Hands steady. Hands steady. They're not steady." } } },
            { "aimed", new[] {
                new[] { "He's pointing that thing at me. Pointing's not shooting.", "Go on and shoot, or put it away, but don't stand there.",
                        "Staring down a barrel. Not the first time. Won't blink.", "I know that look. He's deciding. Let me help him decide wrong.",
                        "If he pulls it, the saloon will talk about me for a week.", "A gun on me and my hands empty. Give me a minute to change that." },
                new[] { "Don't shoot. Don't shoot. Please. I've got nothing.", "Hands. Where do I put my hands. Up. Up is right.",
                        "He's looking right at me down that barrel. Right at me.", "I'll give him whatever he wants. Everything. Just say it.",
                        "Don't move. Rabbits freeze. Freeze.", "My legs want to run. My legs are going to get me killed." },
                new[] { "A gun on me. He wants something. Everybody wants something.", "Steady hand. Not his first time. Mine neither.",
                        "If he meant to shoot he'd have done it. So he wants to talk.", "I'll remember the gun. I'll remember the hand that held it.",
                        "Let him think he's in charge. For now.", "Nothing in my face. Give him nothing to read." },
                new[] { "A gun. He's got a gun on me. Why me.", "I've got children. Does he know I've got children.",
                        "Say something. Say nothing. Which gets me killed.", "The whole street's watching and nobody's moving.",
                        "Breathe. It's a mistake. Please let it be a mistake.", "Lord, I've been good. Mostly good. Does mostly count." } } },
            { "hurt", new[] {
                new[] { "Bleeding. Fine. I've bled before.", "Walk it off. Don't let them see you limp.",
                        "Whoever did this, I'll find. First the doctor, then him.", "A scratch. A deep one. But a scratch.",
                        "Stay upright. A man standing is a man nobody counts out.", "Hurts like hell. Hell and I are old friends." },
                new[] { "There's so much blood. Is that all mine.", "Doctor. Where's the doctor. I need the doctor.",
                        "Don't faint. Don't faint in the street.", "It hurts to breathe. Something's broken in there.",
                        "Somebody help me. Why isn't anybody helping me.", "I'm going to die of this. People die of less." },
                new[] { "Pain's just the body talking. Let it talk.", "Stitches, whiskey, a day in bed. Then the reckoning.",
                        "Blood on the boards. That'll stain. Not my problem.", "I know who did this. He's a dead man and doesn't know it.",
                        "One hand on the wound, one on the pistol. That's the whole plan.", "Doctors cost. Revenge is free." },
                new[] { "Lord, it hurts. It hurts bad.", "The wife'll have a fit. Then she'll fix me up. She always does.",
                        "Press on it. Press on it hard, they say.", "Can't work like this. What do I tell the boss.",
                        "A man can bleed to death on a street full of people. Funny world.", "Is it bad. Don't look. It's bad." } } },
            { "shots", new[] {
                new[] { "Shooting. Where. Who. Let me see.", "Somebody's having a better afternoon than me.",
                        "Hand on the pistol. In case the party spreads.", "Gunfire. Somebody's been owed that a while, I'd wager.",
                        "Don't duck. Ducking's for people who get shot.", "Count the shots. Two guns. No, three. Interesting." },
                new[] { "Shots. Get down. Get down. Get inside.", "Is it coming this way. Is it. Where is it.",
                        "Mama said this town would kill me. Mama was right.", "Under the table. Now. Don't care who's looking.",
                        "Count them. Three. Four. Make it stop.", "Not again. Not again. I can't do this again." },
                new[] { "Someone's settling an account. Not mine. Keep walking.", "Shots from the north. Away from the bank, then. Good.",
                        "The street empties. Good time to be unseen.", "Gunfire's just weather here. You learn to read it.",
                        "Somebody's dying. It isn't me. Carry on.", "Whoever's shooting is bad at it. Too many shots." },
                new[] { "Gunshots. Lord. Again.", "Get the children off the street. Somebody get the children.",
                        "Who's shooting. Is it the law. Is it worse than the law.", "Every time I think this town's getting civil.",
                        "Stay by the wall. Walls don't stop much, but they're something.", "Somebody's mother is about to get bad news." } } },
            { "body", new[] {
                new[] { "Dead man in the street. Somebody's had a bad morning.", "Don't know him. Don't care to. Step round.",
                        "Whoever did that was quick. Or he was slow.", "A body on the boards and nobody doing a thing. This town.",
                        "Could be any of us. Not me, though. Not today.", "Somebody's going to want to know who did that. Not me." },
                new[] { "There's a dead man. There's a dead man right there.", "Don't look. Don't look at his face. Too late.",
                        "Who killed him. Are they still here. Are they looking at me.", "I can't stop shaking. I didn't even know him.",
                        "Get away. Get away from it. Get away from this street.", "His eyes are open. Why are his eyes open." },
                new[] { "Dead. Good boots. Somebody'll have them by dark.", "One less. Whoever he was, one less.",
                        "A body's just a body once the man's gone out of it.", "Killed clean. Somebody knew their work.",
                        "Nothing to see. Nothing I haven't seen.", "Dead men tell no tales and pay no debts. Pity." },
                new[] { "God rest him. Whoever he was. God rest him.", "Somebody should cover his face. Somebody. Me, maybe.",
                        "A man dies in the street and the sun keeps on. Doesn't seem right.", "His mother doesn't know yet. Somewhere a mother doesn't know.",
                        "I'll say a prayer. It's all I've got to give him.", "I saw him alive an hour ago. An hour." } } },
            { "watching", new[] {
                new[] { "Hit him! No, the other one. Oh, that's poor work.", "Two men scrapping. Nobody fights properly anymore.",
                        "If that comes this way I'll show them both how it's done.", "The big one's winning. The small one's got more heart.",
                        "A fight. Finally, something worth watching in {town}.", "My money's on the ugly one. Ugly men have practice." },
                new[] { "A fight. Get back. Get back from it.", "Someone's going to get hurt. Someone always does.",
                        "Don't let them see me watching. Don't let them pull me in.", "There's blood. Oh, there's blood already.",
                        "Where's the law when you need it. Never here.", "Walk on. Walk on like I haven't seen it." },
                new[] { "Let them wear each other down. Saves everybody trouble.", "That one leads with his chin. Somebody'll teach him.",
                        "Men fighting over nothing. Men mostly do.", "Not my quarrel. Not my blood. Not my concern.",
                        "One of them will lose, and then he'll owe somebody. Interesting.", "Whoever wins, the saloon sells more tonight." },
                new[] { "Somebody stop them. Somebody. Not me.", "A fight in the street. In daylight. What's become of {town}.",
                        "That one's got children at home, I'm sure of it.", "They'll be sorry tomorrow. Tomorrow's the fun of it.",
                        "I'd step in if I were younger. If I were braver. I'm neither.", "Grown men. Grown men, rolling in the dirt." } } },
            { "wanted", new[] {
                new[] { "The law's after this one. And he's stood next to me. Fine company.", "A wanted man with a gun. Not afraid of him. Not stood close, either.",
                        "If the law comes shooting, I'm shooting back at whoever's nearest. He knows it too.", "He's got a price on him. Wonder what he'd pay me to forget it." },
                new[] { "That's him. The one they're after. Don't look. Don't breathe.", "He's got a gun and the law's coming and I'm right here.",
                        "Walk away. Slow. Like nothing. Slow.", "If they shoot at him they'll hit me. They always hit the wrong man." },
                new[] { "A wanted man. Worth money to somebody. Not to me, today.", "The law's coming for him. I'll be across the street when it does.",
                        "A price on him and a gun in his hand. Simple sum.", "Men like him are weather. You get indoors." },
                new[] { "Lord, that's a wanted man. Right there. Right by me.", "The law'll come and they won't ask who's who.",
                        "Keep walking. Nod. Keep walking. Don't say a word.", "He looks like anybody. That's the trouble with wanted men." } } },
            { "drinking", new[] {
                new[] { "Another. And one for the piano man if he plays something I like.", "Whiskey's the only honest thing in {town}.",
                        "Three in and I could fight the whole room. Won't. Could.", "Drink like a man or go home to your mother." },
                new[] { "Just one. One more and then I'll go.", "If I drink enough I won't mind the walk home in the dark.",
                        "Nobody's looking at me. Good. Nobody's looking.", "The corner seat. Back to the wall. Then I can drink." },
                new[] { "Watered whiskey. Everybody knows. Everybody pays.", "Men talk when they drink. I drink and listen.",
                        "The barkeep's counting what I owe. So am I.", "Drunk men pay for things sober men keep. Interesting." },
                new[] { "First of the day. Lord, that's good.", "Too many of these and I start telling the truth.",
                        "A drink and a quiet corner. A man can ask for less.", "To the ones who aren't here. Fewer every year." } } },
            { "smoking", new[] {
                new[] { "A smoke and a long look at the street. Let them come.", "Tobacco's the only thing I don't share.", "Smoke out slow. Let them wonder what I'm thinking." },
                new[] { "A smoke steadies the hands. Mostly.", "Last one in the pouch. Make it last.", "Nobody bothers a man having a smoke. I hope." },
                new[] { "Smoke and watch. Everybody tells you who they are if you watch.", "A pipe's cheaper than a friend and asks less.", "The smoke goes where it likes. Wish I could say the same." },
                new[] { "Nothing like it, a smoke at the end of a day.", "Doctor says it'll kill me. So will everything else.", "Watching the smoke go. Wish I could go with it." } } },
            { "resting", new[] {
                new[] { "A seat, a view, and anybody who wants trouble knows where I am.", "Feet up. Nobody's getting me off this bench without a fight.", "Sit where they can see you. Let them look." },
                new[] { "Nobody bothers a man sitting still. Mostly.", "Sit here where the wall's at my back. Safer.", "If I don't move, nobody notices me. That's the plan." },
                new[] { "Sitting's free. Everything else in {town} costs.", "From here I see the bank, the saloon, and who goes in each.", "Rest the body. The mind keeps working. Always." },
                new[] { "Give the feet a rest. They've earned it.", "Sun on my face. Five minutes. Then back to it.", "Watch the street go by. Better than a newspaper.",
                        "Lean here long enough and the whole town walks past you.", "A bench and nothing to do. Haven't had that in years." } } },
            { "armed", new[] {
                new[] { "Iron in hand. Whatever's coming, it can come.", "Gun out. Feels right. Feels like the old days." },
                new[] { "I've got the gun out. Why have I got the gun out. Put it away.", "A gun in my hand and no idea what to do with it." },
                new[] { "A gun's a tool. Tools come out when there's work.", "Pistol out, eyes open. The arithmetic of a quiet life." },
                new[] { "Pistol's heavy. Heavier every year.", "Lord, I hope I don't have to use this." } } },
            { "drunk", new[] {
                new[] { "The street's tilting. Or I am. One of us.", "One more and I'll sing. God help them.", "Drunk and happy and ready for anybody." },
                new[] { "Home's that way. Or that way. Both ways, maybe.", "Said something to somebody. Hope it was kind.", "Don't fall. Falling's when they rob you." },
                new[] { "Drunk's just honest with the brakes off.", "Keep the wallet in the inside pocket. Even drunk, I know that much.", "Everybody's a friend when you're drunk. That's the danger." },
                new[] { "Said I'd stop at two. Said that last week too.", "The ground keeps moving. Hold still, ground.", "She'll smell it on me. She always does." } } }
        };

        // the same for everyone
        private static readonly Dictionary<string, string[]> Shared = new Dictionary<string, string[]>
        {
            { "riding", new[] {
                "Horse knows the way better than I do. Let her have her head.", "Saddle sore. Three more miles and I won't feel my legs.",
                "Smell the rain coming. The horse smells it too.", "She's limping a little on the left. The smith tomorrow.",
                "Nothing like a road with nobody on it.", "Every rider I pass could be anybody. Nod and ride on.",
                "Dust in my teeth. Dust in everything.", "Wish I'd brought the coat. Wish I'd brought a lot of things.",
                "Easy, girl. Easy. It's only the wind.", "Ride till dark, sleep, ride. A life, of a kind." } },
            { "driving", new[] {
                "This wheel's going to go. I can hear it thinking about it.", "The team's tired. So am I. Nearly there.",
                "A wagon's slow but it carries. Can't say that for a horse.", "Ruts deep as graves on this road.",
                "Mind the ditch. Mind the ditch. Mind the ditch.", "Every bump goes straight up my spine." } },
            { "swimming", new[] { "Cold. Lord, the water's cold.", "Boots full of river. Should've thought of that.", "Keep the head up. Keep kicking. Nearly there." } },
            { "eating", new[] {
                "Stew's thin. Stew's always thin.", "Best meal I've had in a week, and that's not saying much.",
                "Chew slow. Nobody's taking it off the plate.", "Bread's hard. Teeth are harder. So far.",
                "Could eat three of these. Could pay for one.", "Hot food. A roof. For a minute, nothing's wrong." } },
            { "working", new[] {
                "Sweep it today, mud tomorrow. That's the job.", "Hands know the work. Head's somewhere else.",
                "Two hours till I'm paid. Two hours and a half.", "A clean floor's a small thing. It's my small thing.",
                "Work's the only thing that doesn't talk back.", "Nail it straight the first time. Pa's voice in my head still.",
                "The boss watches. Let him. The work's good.", "Back's going. Hands are going. Work's still here." } },
            { "walking", new[] {
                "Where was I going. Oh. The store. Right.", "Mud again. Clean boots are a wasted morning here.",
                "Keep moving. A moving man looks busy, and busy men are left alone.", "Same street, same faces, same walk. Not complaining.",
                "Nod to that one. Don't nod to that one. Nod.", "One foot, then the other. The whole secret of it." } },
            { "walking:night", new[] {
                "Dark early. Lamp's out again on the corner.", "Footsteps behind me. My own, echoing. Only mine. Probably.",
                "Home, a fire, a door with a bar across it.", "Every shadow's a man till it isn't.",
                "Should've left before dark. Said that last time too.", "Quiet. Too quiet, or just quiet. Can't tell anymore." } },
            { "running", new[] {
                "Late. Late again. She'll have my hide.", "Run, and the whole town wonders what's chasing you.",
                "Lungs burning. Should've taken the horse.", "Nearly there. Nearly there. Why do I always say nearly." } },
            { "standing", new[] {
                "Waiting. Always waiting on somebody in {town}.", "Stand here long enough and somebody will want something.",
                "Nothing to do and all day to do it in.", "Watching the sky. It's doing more than I am.",
                "Stood here so long my feet have opinions.", "Somebody's late. Somebody's always late." } }
        };
    }
}
