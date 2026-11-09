// The weather as it really is where you are, and whether a line fits it ([Ticker] WeatherChecked, on; Lex, 2026-11-09, from a player: "Some NPC's comment about sweltering heat when its 7 degrees and drizzling").
// Three things were wrong. The add-on read only the game's "previous" weather, so while one kind of weather was blending into the next, people went on about the old one. Nothing knew the temperature: "sun" was all there was, at noon in Tumbleweed, on a cold clear morning in the high country, and at two in the morning too, since the game's clear night is the same "sunny". And a good many lines that say what it's like out were never tied to the weather at all: the noon thoughts, the season's, the small talk, a hello ("Hot one today").
// So: SkyNow is what's really out, read in one place by the game side (WestworldScript.Weather.cs). Fits says whether a line can be said under it, from what its words claim, and the pickers pass over a line that can't, so the next in its pool comes instead. What the AI model writes is checked the same way and more broadly, since it can put anything.
// [Ticker] WeatherTalk (on): people remark on it more the worse it is, and for a couple of minutes after it turns.
// Why it reads true: a change is what people actually remark on. Nobody says it's raining on the fourth day of rain; everybody says it when the first drops land. Shared weather is the oldest excuse there is for speaking, and talk that agrees with the sky the player can see is what makes the rest of it believable. One line that contradicts the sky undoes a good deal.
// Plain C#, no game: offline-tested (WeatherTruthTest). All lines original to this add-on.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    /// What it's like out where you are, read by the game side every second and a half.
    public sealed class SkyNow
    {
        public string Kind;                  // rain storm snow dust fog sun grey (InnerLife.WeatherOf); null for a kind with nothing to say about it (clouds)
        public uint Type;                    // the game's weather type showing
        public float Temp = float.NaN;       // degrees Celsius here, as the game has it; NaN when it won't say
        public int Hour = 12;
        public string Turned;                // what it's just turned to (SkyWatch), for a couple of minutes; null when it hasn't

        public bool Day { get { return Hour >= 6 && Hour < 20; } }
        public bool TempKnown { get { return !float.IsNaN(Temp); } }
        public bool Wet { get { return Kind == "rain" || Kind == "storm"; } }
    }

    /// Whether it has just turned: a new kind of weather has to hold a few seconds, so a flicker in the blend is no event.
    public sealed class SkyWatch
    {
        public const int HoldMs = 6000, ForMs = 150000;
        public static readonly string[] TurnNames = { "rain", "storm", "snow", "cleared", "fog", "dust" };

        private bool _started;
        private string _settled, _coming;
        private int _comingAt, _turnedAt;

        public string Turned { get; private set; }

        /// Forgets it all (a load, a death, a long jump: the sky there is no news).
        public void Reset()
        {
            _started = false;
            _settled = _coming = null;
            Turned = null;
        }

        /// Each reading. The turn that has just happened, once; null otherwise.
        public string Step(string kind, int nowMs)
        {
            string fresh = null;
            if (!_started)
            {
                _started = true;
                _settled = kind;
            }
            else if (kind == _settled)
                _coming = null;
            else if (kind != _coming)
            {
                _coming = kind;
                _comingAt = nowMs;
            }
            else if (nowMs - _comingAt >= HoldMs || nowMs < _comingAt)
            {
                fresh = TurnOf(_settled, kind);
                _settled = kind;
                _coming = null;
                if (fresh != null)
                {
                    Turned = fresh;
                    _turnedAt = nowMs;
                }
            }
            if (Turned != null && (nowMs - _turnedAt >= ForMs || nowMs < _turnedAt))
                Turned = null;
            return fresh;
        }

        /// The Testing row: as if it had just turned. The real weather isn't touched.
        public void Force(string turned, int nowMs)
        {
            Turned = turned;
            _turnedAt = nowMs;
        }

        internal static string TurnOf(string from, string to)
        {
            bool wasFoul = from == "rain" || from == "storm" || from == "snow";
            if (to == "storm" && from != "storm") return "storm";
            if (to == "rain" && !wasFoul) return "rain";   // a storm easing to rain, or snow to sleet, is no new event
            if (to == "snow" && from != "snow") return "snow";
            if (to == "fog" && !wasFoul && from != "fog") return "fog";
            if (to == "dust" && from != "dust") return "dust";
            if (wasFoul && (to == "sun" || to == "grey" || to == null)) return "cleared";
            return null;
        }
    }

    public static class WeatherTruth
    {
        /// Null: not read yet, or [Ticker] WeatherChecked is off: every line fits and everything is as it was (the offline runs too).
        public static volatile SkyNow Now;

        // from the game's weather names (the same hashes as InnerLife.WeatherOf)
        private const uint Drizzle = 2572975940u, Rain = 1420204096u, Shower = 3878058453u, Sleet = 212278652u, Hail = 1974067816u;
        private const uint Thunder = 3061285535u, Thunderstorm = 2082228755u, Hurricane = 839715153u;
        private const uint Snow = 4021743606u, SnowLight = 603685163u, SnowClearing = 1679686673u, Blizzard = 669657108u, Whiteout = 725623432u, GroundBlizzard = 2137137442u;
        private const uint Sandstorm = 2977915153u, Fog = 3592150529u;

        // where "hot" starts to be fair comment, and "cold", and hard cold, and where it could be lying as snow
        internal const float HotFrom = 25f, ColdUnder = 17f, FrostUnder = 5f, SnowUnder = 2f;

        public const double TurnedPull = 44;   // a thought about it, just after it turns: over nearly everything but a scene that's just played out

        /// Which weather is showing, part way through a change from one to the next. Worse weather counts sooner than better: the first
        /// drops land before the sky has finished changing, and the sky is still grey for a while after the rain stops.
        public static uint Seen(uint prev, uint next, float transition)
        {
            if (next == 0 || next == prev || float.IsNaN(transition) || transition < 0f || transition > 1f)
                return prev;   // no change under way, or a blend that isn't one
            int from = Wetness(InnerLife.WeatherOf(prev)), to = Wetness(InnerLife.WeatherOf(next));
            return transition >= (to > from ? 0.25f : to < from ? 0.75f : 0.5f) ? next : prev;
        }

        private static int Wetness(string kind)
        {
            switch (kind)
            {
                case "storm": return 3;
                case "rain": case "snow": return 2;
                case "dust": case "fog": return 1;
                default: return 0;
            }
        }

        /// The game's reading, or NaN when it isn't believable (nought on the dot is what a read that failed looks like).
        public static float Sane(float celsius)
        {
            return float.IsNaN(celsius) || celsius == 0f || celsius < -60f || celsius > 60f ? float.NaN : celsius;
        }

        /// How bad it is: 0 nobody would remark on it, 3 it stops you in the road.
        public static int Severity(SkyNow s)
        {
            if (s == null)
                return 0;
            int n = 0;
            switch (s.Type)
            {
                case Thunder: case Thunderstorm: case Hurricane: case Hail: case Blizzard: case Whiteout: case GroundBlizzard: case Sandstorm: n = 3; break;
                case Rain: case Shower: case Sleet: case Snow: n = 2; break;
                case Drizzle: case SnowLight: case SnowClearing: case Fog: n = 1; break;
            }
            if (s.TempKnown)
            {
                if (s.Temp >= 38f || s.Temp <= -10f) n = Math.Max(n, 2);
                else if (s.Temp >= 33f || s.Temp <= -3f) n = Math.Max(n, 1);
            }
            return n;
        }

        /// How hard the weather pulls on a thought, by how bad it is (it was 15, and 22 for anything foul).
        public static double Pull(SkyNow s)
        {
            switch (Severity(s))
            {
                case 3: return 36;
                case 2: return 29;
                case 1: return 22;
                default: return 15;
            }
        }

        // ---- what a line claims ------------------------------------------------------------------------------------------------
        // Narrow on purpose: these were written against every line in the add-on and read by eye, so "hot coffee", "face hot" and a
        // hard winter remembered don't count; only the lines that say what it's like out now.

        [Flags]
        private enum Claim { None = 0, Hot = 1, Cold = 2, Frost = 4, SnowLying = 8, Raining = 16, Storm = 32, Sun = 64, Clear = 128, Dry = 256 }

        private static KeyValuePair<Claim, Regex> C(Claim c, string pattern)
        {
            return new KeyValuePair<Claim, Regex>(c, new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));
        }

        private static readonly KeyValuePair<Claim, Regex>[] Says =
        {
            C(Claim.Hot, @"\bhot (?:one|day(?! where)|out\b|enough|wind|spell|weather|afternoon|morning|week)\b|\btoo hot\b|\bbe hotter\b|\bmighty hot\b|\bsay hot\b|\bhot through the boot|\b(?:in|and) (?:this |the )?heat\b|(?<!dust, )\bthe heat(?! went| of| got)\b|\bheat (?:sits|comes|like|makes|and the)\b|\bthis heat\b|\bswelter|\bscorching\b|\bscorcher\b|\bbroiling\b|\bblazing\b|\bbeating down\b|\bsweat in the hatband\b|\bsweat down the spine\b|\bsweat running down my back like\b|\bshade's (?:a|worth)\b|\bshade is\b|\bno shade\b|\bwhatever shade\b|\bshade of the bank\b|\bwarm as (?:tea|a bath)\b|\bbutter sliding\b|\btar weeping\b|\bdog lying flat\b|\boven door\b|\bflies on the sun\b|\bneck's burned\b|\bstand in the drip\b|\bwell rope's hot\b|\bcook in the sun\b"),
            C(Claim.Cold, @"\bcold (?:this early|out here|day\b|night\b|morning|for this time|at this hour|wind)|\bcold's (?:cold|got)\b|\bcold enough to (?:crack|freeze)\b|^cold coming up\b|\bfrom the cold\b|\bthe cold (?:found|has|comes|gets)\b|\bgone chill\b|\bwind cold\b|\bcold like this\b|\bcold tools\b|\bwarm again\b|\bhands so cold\b|\bstraight through this coat\b|\bthe other coat\b|^can't feel my fingers\.$|\btoes gone\b|\bteeth going like\b|\bbreath hangs\b"),
            C(Claim.Frost, @"\bfirst frost\b|\bfrost (?:on|still)\b|\bice on\b|\bbreath freezing\b|\bcold has a sound\b"),
            C(Claim.SnowLying, @"\bsnowed in\b|\bthrough the snow\b|\bdropped in the snow\b|\bsnow over the boot\b|\bsnow and mud\b|\bmud and snow\b"),
            C(Claim.Raining, @"\brain down my collar\b|\brain's found\b|\bthis rain\b|\brain on the roof now\b|\brain on the hat\b|\bhat brim's soaked\b|\bhat's a gutter\b|\bboots soaked through\b|\bsoaked through\. any\b|\bwet to the skin\b|\bwater running down my spine\b|\bwet wool\b|\bout of the wet\b|\bwater in my boots\b|\brain doesn't trouble me\b|\brain's no friend\b|\bwet and cold brings\b"),
            C(Claim.Storm, @"^thunder\b|\bthunder like a train\b|\blightning over the hills\b|\bsky's black as a coal hole\b"),
            C(Claim.Sun, @"\bsun on (?:my|us|the adobe|the back|the lids|the water)\b|\bsun's (?:high|sinking|warmer|so bright|got)\b|\bsun (?:straight|right) overhead\b|\bnoon sun\b|\bsun is out\b|\bsun (?:doesn't move|keeps on)\b|\bsquinting so long\b|\bglare off\b|\bshadows sharp\b|\bgood honest sun\b|\bbacks of my hands\b|\bclear blue since\b|\bgood drying weather\b"),
            C(Claim.Clear, @"\bnot a cloud\b|\bclear sky\b|\bsky's clear\b|\bsky's like brass\b|\bsky white as a plate\b|\bred sky\b"),
            C(Claim.Dry, @"\brain's coming\b|\bsmells (?:like|of) rain\b|\brain by (?:supper|thursday)\b|\bknees (?:say|told me about the) rain\b|\bsmell the rain\b|\bwonder if it'll rain\b|\blooks like it'll rain\b|\bclouds piling up\b|\bgoing to get wet\b|\bstorm coming\b|\bnot a drop of rain\b|\bsky won't give\b|\bneeded rain\b|\bsend rain\b|\brain, lord\b|\bno rain behind\b|\blistening for rain\b|\bif it doesn't rain\b|\bsnow would be nice\b"),
        };

        // for what the model writes, which can say anything: wider, and the odd good line lost is cheap (the rules' line comes instead)
        private static readonly KeyValuePair<Claim, Regex>[] Loose =
        {
            C(Claim.Hot, @"\b(?:heat|swelter\w*|scorch\w*|blazing|sweat(?:ing|y)?|sun beat\w*|hot (?:day|sun|wind|out|today|weather|afternoon|morning|air))\b"),
            C(Claim.Cold, @"\b(?:freezing|frostbite|icy|bitter cold|the cold|this cold)\b"),
            C(Claim.SnowLying, @"\b(?:snow\w*|blizzard|sleet)\b"),
            C(Claim.Raining, @"(?<!as )\b(?:rain(?:ing|s|y)?|drizzl\w*|downpour|puddles?)\b"),
            C(Claim.Storm, @"\b(?:thunder\w*|lightning)\b"),
            C(Claim.Sun, @"\b(?:sun|sunshine|sunlight)\b"),
        };

        private static Claim ClaimsOf(string line, KeyValuePair<Claim, Regex>[] table)
        {
            Claim c = Claim.None;
            foreach (KeyValuePair<Claim, Regex> k in table)
                if (k.Value.IsMatch(line))
                    c |= k.Key;
            return c;
        }

        // The pools' lines come round again and again, the patterns cost about 70 microseconds a line, and a scene can pick dozens in one
        // frame: so each line is read once. Names filled in make new strings all the time, hence the ceiling.
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, Claim> SaysRead = new System.Collections.Concurrent.ConcurrentDictionary<string, Claim>();
        private const int SaysReadMost = 8000;

        private static Claim SaysOf(string line)
        {
            Claim c;
            if (SaysRead.TryGetValue(line, out c))
                return c;
            c = ClaimsOf(line, Says);
            if (SaysRead.Count >= SaysReadMost)
                SaysRead.Clear();
            SaysRead[line] = c;
            return c;
        }

        /// The first thing claimed that isn't so under this sky, or None.
        private static Claim Wrong(Claim c, SkyNow s)
        {
            if (c == Claim.None)
                return Claim.None;
            bool snowing = s.Kind == "snow", known = s.TempKnown, clear = s.Kind == "sun" || s.Kind == null;
            float t = s.Temp;
            if ((c & Claim.Hot) != 0 && (s.Wet || snowing || (known ? t < HotFrom : s.Kind != "sun"))) return Claim.Hot;
            if ((c & Claim.Cold) != 0 && (known ? t >= ColdUnder : s.Kind == "sun" && s.Day)) return Claim.Cold;
            if ((c & Claim.Frost) != 0 && !snowing && !(known && t < FrostUnder)) return Claim.Frost;
            if ((c & Claim.SnowLying) != 0 && !snowing && !(known && t < SnowUnder)) return Claim.SnowLying;
            if ((c & Claim.Raining) != 0 && !s.Wet) return Claim.Raining;
            if ((c & Claim.Storm) != 0 && s.Kind != "storm") return Claim.Storm;
            if ((c & Claim.Sun) != 0 && !(s.Day && clear)) return Claim.Sun;
            if ((c & Claim.Clear) != 0 && !clear) return Claim.Clear;
            if ((c & Claim.Dry) != 0 && (s.Wet || snowing)) return Claim.Dry;
            return Claim.None;
        }

        /// Whether a line can be said under the sky there is now. With nothing read, or the switch off, every line can.
        public static bool Fits(string line)
        {
            return Fits(line, Now);
        }

        public static bool Fits(string line, SkyNow s)
        {
            return s == null || string.IsNullOrEmpty(line) || Wrong(SaysOf(line), s) == Claim.None;
        }

        /// The index to use in a pool: the one given if it fits, else the next that does. If none does, the one given.
        public static int FittingIndex(string[] options, int i)
        {
            SkyNow s = Now;
            if (s == null || options.Length < 2 || Fits(options[i], s))
                return i;
            for (int k = 1; k < options.Length; k++)
                if (Fits(options[(i + k) % options.Length], s))
                    return (i + k) % options.Length;
            return i;
        }

        /// What's wrong with something the model wrote, in a word; null when nothing is. Language.Check turns it down.
        public static string WrongIn(string text)
        {
            SkyNow s = Now;
            if (s == null || string.IsNullOrEmpty(text))
                return null;
            Claim wrong = Wrong(SaysOf(text) | ClaimsOf(text, Loose), s);
            return wrong == Claim.None ? null : wrong.ToString();
        }

        /// The weather in words for the model's request, as it really is; the bare kind with nothing read.
        public static string Words(string kind, SkyNow s)
        {
            if (s == null || kind == null)
                return kind;
            string feel = !s.TempKnown ? null : s.Temp < 0f ? "bitter cold" : s.Temp < 10f ? "cold" : s.Temp < 18f ? "cool" : s.Temp < HotFrom ? "mild" : s.Temp < 33f ? "hot" : "scorching";
            string sky;
            switch (kind)
            {
                case "rain": sky = s.Type == Drizzle ? "a drizzle" : s.Type == Hail ? "hail" : s.Type == Sleet ? "sleet" : "rain"; break;
                case "storm": sky = "a thunderstorm"; break;
                case "snow": sky = s.Type == Blizzard || s.Type == Whiteout || s.Type == GroundBlizzard ? "a blizzard" : "snowing"; break;
                case "dust": sky = "a dust storm"; break;
                case "fog": sky = "foggy"; break;
                case "sun": sky = s.Day ? "clear" : "a clear night"; break;
                case "grey": sky = "overcast"; break;
                default: sky = kind; break;
            }
            return feel != null ? feel + ", " + sky : sky;
        }

        /// In a line, for the log and the Testing row.
        public static string Describe(SkyNow s)
        {
            return s == null ? "nothing read" : (s.Kind ?? "no kind") + ", " + (s.TempKnown ? s.Temp.ToString("0") + " C" : "temperature not known") + ", severity " + Severity(s) + (s.Turned != null ? ", just turned: " + s.Turned : "");
        }

        // ---- the lines -------------------------------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> Turns = new Dictionary<string, string[]>
        {
            { "rain", new[] {
                "There's the first of it. Right down the back of my neck.", "Felt a drop. And another. Here we go.",
                "Washing's still on the line. Course it is.", "Dry a minute ago. Not a minute ago, even.",
                "Spots on the boardwalk, and then no boardwalk left dry.", "Everybody on the street's walking faster all at once.",
                "That's rain. Hat down, collar up.", "Smell of wet dust coming up off the road.",
                "Well, there goes the walk home.", "Came on quick. Should've left when I said I would." } },
            { "storm", new[] {
                "Light's gone yellow. Get the horses in.", "That one was close. That was right over the church.",
                "Came up out of nowhere, that did.", "Wind's swung round. Hold on to your hat.",
                "It was over the hills ten minutes back. Now look.", "Somebody shut that door before it goes.",
                "Folks running for the porches. Can't say I blame them.", "One crack and every horse on the street went sideways.",
                "Every dog in town just went under something.", "Went from blue to black while I tied my boot.",
                "Hear that? That's coming this way.", "There goes somebody's hat, and the man after it." } },
            { "snow", new[] {
                "First flakes. There, on my sleeve.", "Started to snow, and me with no gloves.",
                "Coming down now, and no sign of quitting.", "Sky's gone soft and white. Here it comes.",
                "Flakes the size of two-bit pieces. Means a lot of it.", "Hadn't meant to be out in this. Best get along.",
                "White starting to stick on the rails.", "Snow on my eyelashes. Hello, winter." } },
            { "cleared", new[] {
                "It's stopped. Listen. Just the dripping now.", "Street's steaming already.",
                "All done coming down. Mud's only beginning.", "Light coming through over the hills. About time.",
                "Everything smells washed.", "People coming out on the porches again, looking up.",
                "Let up at last. Shook my hat out.", "There. That's done with. Now the flies." } },
            { "fog", new[] {
                "Fog coming in. Can't see the end of the row now.", "Was clear a minute ago. Now look at it.",
                "White coming up off the low ground.", "Better keep to the road while there's a road to see." } },
            { "dust", new[] {
                "Here it comes, the whole county at once.", "Wind's picking up the road. Cover your mouth.",
                "Get the shutters. Get them now.", "Hat. Where's my hat. Never mind my hat." } },
        };

        private static readonly string[] Drizzling =
        {
            "Hardly rain at all. Just enough to spoil a hat.", "Spitting. Can't make up its mind.",
            "A wet you could call rain, if you wanted to complain.", "Just a fine wet on the face. I've had worse.",
            "Nothing you'd run from. Not yet.", "Rain so light the dust hardly knows it.",
        };

        private static readonly string[] ColdRain =
        {
            "Cold rain. Goes through wool like it was paper.", "So fine you can't see it. Wet through all the same.",
            "This thin cold wet gets in the knees.", "Not enough rain to stop work. Just enough to hate it.",
            "Breath smoking and the rain ticking on my hat.", "Damp and cold both. Stove, please.",
            "The kind of wet you don't notice till your hands quit.", "Thin rain, thin coat, thin patience.",
        };

        private static readonly string[] ColdSun =
        {
            "Bright as a dollar and cold as a stone.", "Sun's out and doing nothing for my ears.",
            "Clear and cold. Breath goes up straight as smoke.", "Blue sky and a bite in it.",
            "Good clean cold. Wakes a body up.", "Looks warmer than it is. Always does.",
            "A sun you look at, and that's all it's good for.", "Collar up in broad daylight.",
        };

        private static readonly string[] ClearNight =
        {
            "Stars right down to the hills tonight.", "Clear night. Nothing between me and all of that.",
            "Moon's up. Could near read by it.", "Stars out thick as spilled salt.",
            "Never counted them. Somebody ought to.", "Sky's so full tonight it looks like it might tip.",
            "You can see the whole road by starlight.", "Whole sky out, and nobody looking up but me.",
        };

        private static readonly string[] Scorching =
        {
            "Air's got weight to it today.", "Nothing moving that's got any sense.",
            "Water's gone before it's down my throat.", "Ground's giving it back up through my boots.",
            "Even the flies have quit.", "Hat's hot on the crown. Head's no better.",
        };

        private static readonly string[] Bitter =
        {
            "The kind of cold that hurts to breathe.", "Spat and it was ice before the ground.",
            "Nose froze shut on the first breath.", "Too cold to snow, my father'd say.",
            "Everything creaks in this. The boards, the leather, me.", "Cold enough to make a man honest.",
        };

        /// The lines for the weather as it really is, or null for the plain table by kind (InnerLife.Now.cs). turned: they're about it having just turned.
        public static string[] PoolFor(SkyNow s, string seed, out bool turned)
        {
            turned = false;
            if (s == null)
                return null;
            string[] own;
            if (s.Turned != null && Turns.TryGetValue(s.Turned, out own))
            {
                turned = true;
                return own;
            }
            bool cold = s.TempKnown && s.Temp < 12f;
            switch (s.Kind)
            {
                case "rain":
                    if (cold) return Voice.Chance(seed + "sky", 60) ? ColdRain : null;
                    return s.Type == Drizzle && Voice.Chance(seed + "sky", 60) ? Drizzling : null;
                case "sun":
                    if (!s.Day) return ClearNight;
                    if (s.TempKnown && s.Temp < 8f) return Voice.Chance(seed + "sky", 60) ? ColdSun : null;
                    return s.TempKnown && s.Temp >= 35f && Voice.Chance(seed + "sky", 40) ? Scorching : null;
                case "snow":
                    return s.TempKnown && s.Temp <= -8f && Voice.Chance(seed + "sky", 50) ? Bitter : null;
            }
            return null;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in Turns.Values)
                foreach (string l in pool)
                    yield return l;
            foreach (string[] pool in new[] { Drizzling, ColdRain, ColdSun, ClearNight, Scorching, Bitter })
                foreach (string l in pool)
                    yield return l;
        }

        /// One pool's lines, for the test: "turn:rain", or a name above.
        public static string[] LinesOf(string name)
        {
            if (name.StartsWith("turn:", StringComparison.Ordinal))
                return Turns[name.Substring(5)];
            switch (name)
            {
                case "drizzling": return Drizzling;
                case "coldrain": return ColdRain;
                case "coldsun": return ColdSun;
                case "clearnight": return ClearNight;
                case "scorching": return Scorching;
                case "bitter": return Bitter;
            }
            return new string[0];
        }
    }
}
