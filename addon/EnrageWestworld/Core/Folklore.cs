// Night folklore, as tales people tell (Run 3, item 3; Lex, 2026-11-04, answering the run's question 10: "this is good with
// people telling night folklore"). Roadmap 16's P20 had the bell on the grave and the tommyknockers happen to the player;
// Lex chose the other way: the old tales told at night, in the saloon and the street after dark (Scenes.NightTale) and by a
// campfire in the wilds (Campfire.Tale), with the listener answering by their nature: the coward shivers, the hard scoff, the
// rest lean in to hear how it goes. The tales are the period's own folklore, told in this add-on's own words: the bell on the grave
// (the safety coffins of the century), the Cornish miners' tommyknockers, the feu follet of the Louisiana bayous, the Bell
// Witch of Tennessee, the phantom stage, the Lost Dutchman's mine (Jacob Waltz, dead in 1891), Tailypo from the hills, the
// fiddler at the crossroads, La Llorona of the river country, the brakeman's lantern on the Carolina line, John Henry and the
// steam drill, the hoop snake, and the white pacing stallion of the plains. Why they fit: a frontier night was long and
// dark, and a tale told round a fire or a bar was the evening's entertainment and the way a place passed on what it feared;
// the teller is enjoyed for the shiver, and the listener's answer says who they are. Nothing gory; the fright is in the
// telling. Plain C#; checked by mindtest (FolkloreTest). [Scenes] NightTales (on).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class FolkTale
    {
        public string Key;
        public string[] Lines;   // the teller's, in order: three or four, each said on its own
        public bool Eerie;       // a ghost or a haunting (the listener may shiver), else a tall one or a legend
        public string[][] Grows; // the big animals' tales (DEPTH item 14): each line's later tellings, bigger each time (Lines is the first)
    }

    public static class Folklore
    {
        /// [Scenes] NightTales: the tales are told (set by the game; on in the harnesses).
        public static bool On = true;

        private static FolkTale F(string key, bool eerie, params string[] lines)
        {
            return new FolkTale { Key = key, Eerie = eerie, Lines = lines };
        }

        public static readonly FolkTale[] All =
        {
            F("bell", true,
              "Back home they buried folk with a string to the hand and a bell on a post. In case they woke.",
              "Old Mrs. Tuttle went in the ground in a July heat. Three nights on, the bell rang at midnight.",
              "The sexton dug her up by lantern light. She was lying just as they'd laid her.",
              "But the string was pulled tight round her finger. Nobody's slept easy in that county since."),
            F("knockers", true,
              "In the mines up north the Cornishmen leave a crust of their pasty for the knockers.",
              "Little men in the walls, no bigger than a boot. They knock to warn you when the roof's going.",
              "My cousin laughed at them once and ate his whole dinner. Next shift the timbers groaned.",
              "He got out with the knocking at his back. Leaves them half his bread every day now."),
            F("fifolet", true,
              "Down in the bayou the Cajuns call it feu follet. A little light over the water at night.",
              "It bobs along ahead of you like a lantern on a pole, and it keeps just out of reach.",
              "Follow it and it leads you into the deep water. The old women say it's a soul that can't rest.",
              "Stick a needle in a fence post: it has to pass through the eye before it can lead you off. So they tell it."),
            F("bellwitch", true,
              "There was a farm in Tennessee, the Bell place, where a voice talked to the family four years.",
              "It sang hymns and cursed and pulled the children's hair, and nobody ever laid eyes on it.",
              "They say General Jackson rode out to see for himself. His wagon wheels stuck fast on a dry road.",
              "Jackson said he'd sooner fight the British again than spend another night on that farm."),
            F("stage", true,
              "On the old stage road east of here there's a coach still running that went off the bridge in '68.",
              "Late at night you hear the team and the driver's whip, coming on hard.",
              "Step off the road to let it by, and nothing comes round the bend. Nothing at all.",
              "Folks who've heard it don't ride that road after dark. I don't, and I'm not ashamed to say it."),
            F("dutchman", false,
              "There was a Dutchman in Arizona with a gold mine nobody else could find.",
              "When he lay dying he told a woman the way to it. She hunted it one summer, then sold maps of it.",
              "Men still go up into those mountains with his directions in their heads. None has found it.",
              "Some say the mountain doesn't want the gold found. I'd leave it lie."),
            F("tailypo", true,
              "An old man in the hills cut the tail off a varmint that came in his cabin, and ate it for supper.",
              "That night he heard scratching at the door, and a voice: tailypo, tailypo, all I want's my tailypo.",
              "His dogs went after it and never came back. The scratching came closer every night.",
              "There's nothing left of that cabin now but the chimney, and a voice in the wind, asking."),
            F("fiddler", true,
              "There was a fiddler in the hollows who met a stranger at the crossroads one moonless night.",
              "Next morning he could play like no man living. Reels so fast the dancers couldn't stop.",
              "Played every wedding for twenty years and never got old. Then one night he didn't come home.",
              "They found his fiddle at the crossroads with every string broke."),
            F("llorona", true,
              "Down by the river the Mexican women tell of La Llorona, a woman in white weeping for her children.",
              "She walks the water at night calling for them. If you hear her near, she's far. If far, she's near.",
              "A vaquero I rode with heard her once on the Rio Bravo. Wouldn't water his horse there again."),
            F("lantern", true,
              "In Carolina there's a light that walks the railroad at night, swinging, like a lantern.",
              "Years back a brakeman ran down the track swinging his lantern to stop the next train. The wreck caught him.",
              "Folks go out to watch for it. It comes down the track toward you and goes out a few yards off."),
            F("johnhenry", false,
              "John Henry was a steel-driving man, and they brought a steam drill to the tunnel to replace him.",
              "He said a man ain't nothing but a man, and he'd beat that drill or die with the hammer in his hand.",
              "Drove fourteen feet to the drill's nine. Then he laid down his hammer and he died.",
              "They still sing it on the railroad gangs. A man against a machine, and the man won."),
            F("hoopsnake", false,
              "A hoop snake takes its tail in its mouth and rolls after you like a wagon wheel.",
              "One came after my uncle downhill. He jumped behind a tree and the snake struck the trunk.",
              "Next morning that tree was swole up and dead, leaves and all. He made a fence post of it."),
            F("stallion", false,
              "Out on the plains there's a white stallion nobody's ever caught. A pacer, never breaks his stride.",
              "Men have chased him with fresh horses three days running. He just paces off over the rise.",
              "Some say he's a ghost. My pa said he was a horse with more sense than the men chasing him.")
        };

        /// One tale, by the seed (null when the tales are off).
        // ---- the big animals' tales grow (DEPTH item 14; the roadmap's "Tales of the big animals", told and never grown till now): each
        // time the hoop snake or the white stallion is told in a town this session it bends a step (a longer snake, a cousin who saw
        // it, the whole orchard; a week's chase, then a month's, and a dozen riders), two steps at most; a teller who swears he saw it
        // himself keeps nearest his first telling and says so. Why it reads true: Bartlett, again: each telling bends toward what the
        // teller expects, and the eyewitness, whose story it is, has least need to improve on it.
        static readonly Dictionary<string, string[][]> GrowsOf = new Dictionary<string, string[][]>
        {
            { "hoopsnake", new[] {
                new[] { "A hoop snake near eight foot round takes its tail in its mouth and rolls after you like a wagon wheel.", "A hoop snake big as a stage wheel, twelve foot if it's an inch, rolls after you downhill like it's on rails." },
                new[] { "One came after my cousin downhill, and my cousin swears to it. He jumped behind a tree and the snake struck the trunk.", "Three men saw one go after my cousin, all sober. He got behind a tree, and the snake struck it so hard the bark flew." },
                new[] { "By morning that tree was dead, and the two beside it. He made a fence of them.", "By morning the whole orchard was dead, root and branch. They had to burn it." } } },
            { "stallion", new[] {
                new[] { "Out on the plains there's a white stallion nobody's ever caught. Paces faster than a running horse can gallop.", "Out on the plains there's a white stallion that's outpaced the railroad. Never breaks his stride, not once." },
                new[] { "Men have chased him with fresh horses a week running. He just paces off over the rise.", "A dozen riders chased him a month, changing horses at every ranch. He paced off over the rise every evening." },
                new[] { "Some say he's a ghost. My pa saw him once and wouldn't speak of it.", "Some say he's a ghost. A man in Tumbleweed shot at him point blank and the bullet went clean through." } } },
        };

        /// The lines as told now: the tale's first telling, or (for the big animals') bent a step for each time it's been told in this
        /// town before, two at most; a teller who saw it himself keeps the first and says so. times below 0: the tale as written.
        public static string[] Telling(FolkTale tale, int times, bool saw)
        {
            string[][] grows;
            if (tale == null || times < 0 || !GrowsOf.TryGetValue(tale.Key, out grows))
                return tale != null ? tale.Lines : null;
            if (saw)
                return tale.Lines.Concat(new[] { "I saw it myself, and that's the plain truth of it. No more, no less." }).ToArray();
            int stage = Math.Min(2, times);
            if (stage == 0)
                return tale.Lines;
            var lines = new string[tale.Lines.Length];
            for (int i = 0; i < lines.Length; i++)
                lines[i] = i < grows.Length && stage - 1 < grows[i].Length ? grows[i][stage - 1] : tale.Lines[i];
            return lines;
        }

        /// Whether this teller swears he saw it himself (one in five, by who he is and the tale).
        public static bool SawIt(string teller, string key) { return teller != null && Voice.Chance(teller + "|saw|" + key, 20); }

        public static bool Grows(string key) { return key != null && GrowsOf.ContainsKey(key); }

        public static FolkTale Pick(string seed)
        {
            return !On ? null : All[Voice.Hash(seed + "|folk") % All.Length];
        }

        // ---- what the listener says between the lines, by their nature --------------------------------------------

        private static readonly string[] Shiver = { "Hush, now. Not at this hour.", "Why'd you tell that one at night?", "I've the long walk home after this.",
                                                    "Don't. I've a cellar to go down in the morning." };
        private static readonly string[] Scoff = { "Ghost stories, at our age.", "And I'm the governor of New York.", "Somebody's been at the whiskey.",
                                                   "Every county's got one of those." };
        private static readonly string[] ScoffTall = { "Tall talk, and taller every time it's told.", "And I'm the governor of New York.", "Somebody's been at the whiskey.",
                                                       "Every county's got one of those." };   // at a legend or a tall one, not a ghost (the review)
        private static readonly string[] Lean = { "And then what?", "Go on.", "My grandmother told that one, near enough.", "Is that a fact?" };
        private static readonly string[] CloseShiver = { "Well. I'm for bed, and I'll leave a light burning.", "I'll not sleep tonight, and that's your doing.",
                                                         "Next time tell one about a mule." };
        private static readonly string[] CloseScoff = { "A fine tale. I'll thank you not to tell it twice.", "You ought to write for the papers.", "Huh. Pour me another." };
        private static readonly string[] CloseLean = { "That's a good one. I'll tell it myself.", "Well. That'll keep me up.", "Huh. Who told you that?" };

        /// How the listener takes it: "shiver" (a coward, at an eerie tale), "scoff" (the hard and the hot), else "lean".
        public static string Taking(string temper, bool eerie, string seed)
        {
            if (temper == "Coward" && eerie)
                return "shiver";
            if (temper == "Cold" || temper == "Hothead" || temper == "Greedy")
                return "scoff";
            if (eerie && Voice.Chance(seed + "|sh", 25))
                return "shiver";
            return "lean";
        }

        /// The listener's word between two of the teller's lines: a ghost scoffed at as a ghost, a tall one as tall talk.
        public static string Between(string taking, bool eerie, string seed)
        {
            return Voice.Pick(seed + "|bt", taking == "shiver" ? Shiver : taking == "scoff" ? (eerie ? Scoff : ScoffTall) : Lean);
        }

        public static string Between(string taking, string seed)
        {
            return Between(taking, true, seed);
        }

        /// The listener's last word.
        public static string Close(string taking, string seed)
        {
            return Voice.Pick(seed + "|cl", taking == "shiver" ? CloseShiver : taking == "scoff" ? CloseScoff : CloseLean);
        }

        /// What the others at a campfire say between a folk tale's lines (Campfire.Answer for a tale of these).
        public static string FireAnswer(int line, string seed)
        {
            switch (line)
            {
                case 0: return Voice.Pick(seed + "|fa0", "Here he goes. Not that one, not at night.", "Quiet, now. Let the man tell it.", "My mother told that one.");
                case 1: return Voice.Pick(seed + "|fa1", "Hush.", "Go on.", "Throw another stick on.");
                default: return Voice.Pick(seed + "|fa2", "Well.", "Keep the fire high tonight.", "Huh. Pour the man another.");
            }
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return All.SelectMany(t => t.Lines).Concat(Shiver).Concat(Scoff).Concat(ScoffTall).Concat(Lean).Concat(CloseShiver).Concat(CloseScoff).Concat(CloseLean)
                      .Concat(new[] { "Here he goes. Not that one, not at night.", "Quiet, now. Let the man tell it.", "My mother told that one.", "Hush.", "Go on.", "Throw another stick on.", "Well.",
                                      "Keep the fire high tonight.", "Huh. Pour the man another." }.Distinct());
        }
    }
}
