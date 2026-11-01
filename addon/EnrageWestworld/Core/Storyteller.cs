// The storyteller (NIGHT4 item 2, 2026-10-31; Lex: "sounds like a great addition"): a tension meter around the player, the
// game designer's "AI director". It never starts anything itself: it reads what happened near him lately (the notable
// kinds only, never the everyday exchanges) and where he is and what he's doing, and hands the game a set of multipliers
// for the timers and thresholds that already decide when the street's scenes, the walk-ups and the encounters look again.
// Quiet too long: shorter gaps, the chance meeting first more often, the main scene nearer; busy, or just after something
// big (a short breather, 30-60 s, never a ban on any kind): a little longer. Off: every multiplier 1, as before.
// Real milliseconds throughout (Game.GameTime); Happening.At is a game minute, so the game stamps each one itself.
// No allocation once built: a fixed ring of the last 32 notable things, the multipliers a struct, the words for them
// built only when the band changes.
using System;
using System.Text;

namespace EnrageWestworld.Core
{
    public enum StoryMode { Off, Calm, Lively, Wild }

    /// Even: as the mode has it. Quiet: nothing notable near him for a while. Busy: a lot lately, or the breather after
    /// something big. Held: a mission, wanted, a fight, the panel, indoors (not a saloon): every multiplier 1, the meter still.
    public enum StoryBand { Even, Quiet, Busy, Held }

    /// Where the player is and what he's doing, read by the game every couple of seconds.
    public struct StoryState
    {
        public bool Town, Road, Interior, Saloon, RidingFast, Combat, Wanted, Mission, Panel;

        public string HeldWhy()
        {
            if (Mission) return "a mission";
            if (Panel) return "the settings panel";
            if (Wanted) return "you're wanted";
            if (Combat) return "you're fighting";
            if (Interior && !Saloon) return "indoors";
            return null;
        }

        public string Where()
        {
            return Saloon ? "a saloon" : Interior ? "indoors" : Town ? "town" : Road ? "the road" : "the wilds";
        }
    }

    /// What the game multiplies its timers by. Gaps and rests: below 1 sooner. ChanceFirstBelow is the strength under which
    /// the chance meeting goes first (50 as built); PerMetre multiplies the main scene's points off a metre (3 or 2).
    public readonly struct StoryMults
    {
        public readonly float Drama, Side, Chance, PerMetre, EncounterEvery, EncounterChance, WalkLook, PaceChance, PaceRest, PersonRest;
        public readonly int ChanceFirstBelow;
        public readonly string Sped;

        public StoryMults(float drama, float side, float chance, int chanceFirstBelow, float perMetre, float encounterEvery, float encounterChance,
                          float walkLook, float paceChance, float paceRest, float personRest, string sped = null)
        {
            Drama = drama;
            Side = side;
            Chance = chance;
            ChanceFirstBelow = chanceFirstBelow;
            PerMetre = perMetre;
            EncounterEvery = encounterEvery;
            EncounterChance = encounterChance;
            WalkLook = walkLook;
            PaceChance = paceChance;
            PaceRest = paceRest;
            PersonRest = personRest;
            Sped = sped ?? Describe(drama, side, chance, chanceFirstBelow, perMetre, encounterEvery, encounterChance, walkLook, paceChance, paceRest, personRest);
        }

        public static readonly StoryMults Neutral = new StoryMults(1f, 1f, 1f, 50, 1f, 1f, 1f, 1f, 1f, 1f, 1f, "nothing (as built)");

        /// Riding fast: nobody walks up to a galloping man, so the walk-ups' looks, odds and rests are as built; the street's own still apply.
        public StoryMults Riding()
        {
            return new StoryMults(Drama, Side, Chance, ChanceFirstBelow, PerMetre, EncounterEvery, EncounterChance, 1f, 1f, 1f, PersonRest);
        }

        public bool Same(StoryMults o)
        {
            return Drama == o.Drama && Side == o.Side && Chance == o.Chance && ChanceFirstBelow == o.ChanceFirstBelow && PerMetre == o.PerMetre &&
                   EncounterEvery == o.EncounterEvery && EncounterChance == o.EncounterChance && WalkLook == o.WalkLook && PaceChance == o.PaceChance &&
                   PaceRest == o.PaceRest && PersonRest == o.PersonRest;
        }

        private static string Describe(float drama, float side, float chance, int below, float perMetre, float encEvery, float encChance,
                                       float walkLook, float paceChance, float paceRest, float personRest)
        {
            var b = new StringBuilder();
            Part(b, "scene gaps", drama);
            Part(b, "quiet talks' gaps", side);
            Part(b, "chance meetings' gap", chance);
            if (below != 50)
                b.Append(b.Length > 0 ? ", " : "").Append("chance meeting first under ").Append(below);
            Part(b, "points off a metre", perMetre);
            Part(b, "encounter rolls' gap", encEvery);
            Part(b, "encounter odds", encChance);
            Part(b, "walk-up looks", walkLook);
            Part(b, "walk-up odds", paceChance);
            Part(b, "walk-up rests", paceRest);
            Part(b, "a person's rest", personRest);
            return b.Length == 0 ? "nothing (as built)" : b.ToString();
        }

        private static void Part(StringBuilder b, string what, float x)
        {
            if (Math.Abs(x - 1f) < 0.005f)
                return;
            b.Append(b.Length > 0 ? ", " : "").Append(what).Append(" x").Append(x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    public sealed class StoryMeter
    {
        public const int RingSize = 32;
        public const int UpdateMs = 2000;            // the game updates it this often
        public const float ReachMetres = 30f;        // a notable thing counts within this of the player
        public const float EncounterReach = 60f;     // an encounter is staged out of sight and comes to him: counted farther
        public const float DecaySeconds = 180f;      // a thing's weight falls by e every three real minutes
        public const float Saturate = 6f;            // tension = 1 - exp(-sum / Saturate): busy (0.7) takes about three fights or thefts in two minutes
        public const float BusyAt = 0.7f, BusyLeave = 0.5f;
        public const int BreatherMinMs = 30000, BreatherMaxMs = 60000;   // after a weight-3 thing (never the old hush: it only lengthens gaps)
        public const int MaxStepMs = 10000;          // a long pause between updates (a load, the pause menu) counts as this

        private readonly int[] _at = new int[RingSize];
        private readonly byte[] _weight = new byte[RingSize];
        private readonly float[] _metres = new float[RingSize];
        private readonly string[] _kind = new string[RingSize];
        private int _head, _count, _lastUpdate = int.MinValue, _lastNotable, _breatherUntil, _bornAt;
        private bool _riding;

        public StoryMode Mode { get; private set; }
        public StoryBand Band { get; private set; }
        public StoryBand Was { get; private set; }
        public StoryMults Mults { get; private set; }
        public float Tension { get; private set; }
        public string HeldWhy { get; private set; }
        public string Where { get; private set; }
        public int Noted { get; private set; }        // notable things counted since the start
        public int Ignored { get; private set; }      // notable kinds too far off to count
        public int BandChanges { get; private set; }
        public int Retracted { get; private set; }    // counted at a scene's start, then taken back: its two never met
        public bool On { get { return Mode != StoryMode.Off; } }

        public StoryMeter(StoryMode mode, int now)
        {
            Mode = mode;
            Band = StoryBand.Even;
            Was = StoryBand.Even;
            _lastNotable = now;
            _bornAt = now;
            Where = "not yet read";
            Mults = Table(mode, StoryBand.Even, false);
        }

        public static StoryMode ParseMode(string s)
        {
            switch ((s ?? "").Trim().ToLowerInvariant())
            {
                case "off": return StoryMode.Off;
                case "calm": return StoryMode.Calm;
                case "wild": return StoryMode.Wild;
                default: return StoryMode.Lively;
            }
        }

        public static string ModeName(StoryMode m)
        {
            return m == StoryMode.Off ? "off" : m == StoryMode.Calm ? "calm" : m == StoryMode.Wild ? "wild" : "lively";
        }

        /// Real seconds of nothing notable near him before the street is called quiet.
        public static int QuietAfterSeconds(StoryMode m)
        {
            return m == StoryMode.Calm ? 240 : m == StoryMode.Wild ? 60 : 120;
        }

        /// A new mode (the ini or the panel): true when it changed. The band starts even, the ring is kept.
        public bool SetMode(StoryMode m, int now)
        {
            if (m == Mode)
                return false;
            Mode = m;
            Was = Band;
            Band = m == StoryMode.Off ? StoryBand.Even : Band;
            Mults = Table(m, Band, _riding);
            return true;
        }

        /// How much a happening is worth to the meter: 3 a fight, a theft, a standoff, a death, an encounter, the player's
        /// violence; 2 a scene worth walking over for or someone coming to him; 1 a quarrel or a small kindness; 0 anything
        /// else (the snap and comfort exchanges, small talk, gossip: counting them would read the everyday chatter as excitement).
        /// `kind` is a Happening's (scene:x, deed:x, death, encounter:x) or a walk-up's (you:x) or "gang".
        public static int Weight(string kind, bool playerDid)
        {
            if (string.IsNullOrEmpty(kind))
                return 0;
            if (kind == "death" || kind == "gang")
                return 3;
            if (kind.StartsWith("encounter:", StringComparison.Ordinal))
                return 3;
            if (kind.StartsWith("deed:", StringComparison.Ordinal))
            {
                if (!playerDid)
                    return 0;
                string g = Minds.DeedGroup(kind.Substring(5));
                return g == "deed:violence" ? 3 : g == "deed:theft" ? 2 : 0;
            }
            if (kind.StartsWith("you:", StringComparison.Ordinal))
            {
                switch (kind.Substring(4))
                {
                    case "lift": return 3;
                    case "grudge": case "thanks": case "lonely": case "favour": case "warn": case "kin": case "stalker": case "brawl": return 2;
                    default: return 0;
                }
            }
            if (!kind.StartsWith("scene:", StringComparison.Ordinal))
                return 0;
            switch (kind.Substring(6))
            {
                case "brawl": case "rough": case "standoff": case "pickpocket": case "lift": case "drunk": case "cheat": case "posse":
                case "callout": case "jealous": case "mugging": case "shootout":
                    return 3;
                case "mourn": case "propose": case "grudge": case "haggle": case "song": case "talltale": case "lostchild": case "preacher":
                case "marry": case "beg": case "condole": case "avenger": case "defend":
                    return 2;
                case "argue": case "fall": case "help": case "round": case "standup": case "apology": case "patient": case "tend": case "charity":
                    return 1;
                default:
                    return 0;   // x:* and how they were dressed (snap, comfort, brushoff, cheer, envy, makeup, sweet), smalltalk, gossip ...
            }
        }

        /// Something happened `metres` from the player. Only a notable kind (weight > 0) within reach counts.
        public void Noticed(string kind, int weight, int now, float metres)
        {
            if (weight <= 0)
                return;
            float reach = kind != null && kind.StartsWith("encounter:", StringComparison.Ordinal) ? EncounterReach : ReachMetres;
            if (metres > reach)
            {
                Ignored++;
                return;
            }
            _at[_head] = now;
            _weight[_head] = (byte)Math.Min(weight, 3);
            _metres[_head] = metres;
            _kind[_head] = kind;
            _head = (_head + 1) % RingSize;
            if (_count < RingSize)
                _count++;
            _lastNotable = now;
            Noted++;
            if (weight >= 3)
                _breatherUntil = Math.Max(_breatherUntil, now + BreatherMs(metres));
        }

        /// The breather after a weight-3 thing: 30 s, up to 60 for one close by (it only lengthens the gaps; nothing is held back).
        private static int BreatherMs(float metres)
        {
            return BreatherMinMs + (int)((BreatherMaxMs - BreatherMinMs) * Math.Max(0f, 1f - metres / ReachMetres));
        }

        /// A scene counted at its start whose two never met (the walk-over gave up, so nothing reached the screen): the newest
        /// entry of that kind noted at or after `since` is taken back, and the quiet clock and the breather go back to what the
        /// rest of the ring says. True when one was found.
        public bool Retract(string kind, int since)
        {
            for (int k = 0; k < _count; k++)
            {
                int i = (_head - 1 - k + RingSize) % RingSize;
                if (_weight[i] == 0 || _kind[i] != kind || _at[i] < since)
                    continue;
                _weight[i] = 0;
                Retracted++;
                int last = _bornAt, breather = 0;
                for (int j = 0; j < _count; j++)
                {
                    if (_weight[j] == 0)
                        continue;
                    last = Math.Max(last, _at[j]);
                    if (_weight[j] >= 3)
                        breather = Math.Max(breather, _at[j] + BreatherMs(_metres[j]));
                }
                _lastNotable = last;
                _breatherUntil = breather;
                return true;
            }
            return false;
        }

        /// Every UpdateMs: the band and the multipliers. True when the band changed (the game logs it and pulls the street's
        /// deadlines in when it went quiet); the multipliers can change without it (riding fast), never the other way.
        public bool Update(int now, StoryState s)
        {
            int dt = _lastUpdate == int.MinValue ? 0 : Math.Max(0, Math.Min(MaxStepMs, now - _lastUpdate));
            _lastUpdate = now;
            Where = s.Where();
            StoryBand band;
            HeldWhy = Mode == StoryMode.Off ? null : s.HeldWhy();
            if (Mode == StoryMode.Off)
                band = StoryBand.Even;
            else if (HeldWhy != null)
            {
                // the meter stands still: nothing ages and the quiet doesn't grow while he's busy with his own
                for (int i = 0; i < _count; i++)
                    _at[i] += dt;
                _lastNotable += dt;
                _bornAt += dt;
                if (_breatherUntil > now - dt)
                    _breatherUntil += dt;
                band = StoryBand.Held;
            }
            else
            {
                Tension = TensionAt(now);
                if (now < _breatherUntil || Tension >= BusyAt || (Band == StoryBand.Busy && Tension >= BusyLeave))
                    band = StoryBand.Busy;
                else if (now - _lastNotable >= QuietAfterSeconds(Mode) * 1000)
                    band = StoryBand.Quiet;
                else
                    band = StoryBand.Even;
            }
            bool riding = s.RidingFast;
            bool changed = band != Band;
            if (changed || riding != _riding)
            {
                if (changed)
                {
                    Was = Band;
                    BandChanges++;
                }
                Band = band;
                _riding = riding;
                StoryMults m = Table(Mode, band, riding);
                if (!m.Same(Mults))
                    Mults = m;
            }
            return changed;
        }

        public float TensionAt(int now)
        {
            float sum = 0f;
            for (int i = 0; i < _count; i++)
            {
                float age = Math.Max(0, now - _at[i]) / 1000f;
                sum += _weight[i] * (float)Math.Exp(-age / DecaySeconds);
            }
            return 1f - (float)Math.Exp(-sum / Saturate);
        }

        /// Real seconds since the last notable thing near him (frozen while held).
        public int QuietSeconds(int now)
        {
            return Math.Max(0, now - _lastNotable) / 1000;
        }

        public int BreatherSeconds(int now)
        {
            return Math.Max(0, _breatherUntil - now) / 1000;
        }

        /// The last notable thing, for F7: kind, metres, seconds ago; null when none yet.
        public string Last(int now)
        {
            for (int k = 0; k < _count; k++)
            {
                int i = (_head - 1 - k + RingSize) % RingSize;
                if (_weight[i] > 0)
                    return _kind[i] + " " + Math.Round(_metres[i]) + " m off, " + Math.Max(0, now - _at[i]) / 1000 + " s ago";
            }
            return null;
        }

        public static string BandName(StoryBand b)
        {
            return b == StoryBand.Quiet ? "quiet" : b == StoryBand.Busy ? "busy" : b == StoryBand.Held ? "held" : "even";
        }

        // ---- the tables -------------------------------------------------------------------------------------------
        // Each within the study's safe ranges: scene, chance and side gaps x0.45-1.25 (the 8 s retry and the 20 s floor stay),
        // chance first 40-65, points a metre x1-1.33 (2-4 after), encounter rolls' gap x0.5-1.25 and odds x0.75-1.6 (60%
        // cap), walk-up looks x0.5-1.25, their odds x0.6-1.5 and kind rests x0.5-1.25, a person's rest x0.6-1.25 (the 60 s
        // floor stays). The audit: unhushed Saint Denis starts enough scenes, so the even band leaves the scene gap alone in
        // Lively and Wild; the quiet band is where the gaps shorten. Calm is a quieter street than as built.

        private static readonly StoryMults CalmEven = new StoryMults(1.25f, 1.2f, 1.25f, 45, 1f, 1.2f, 0.85f, 1.2f, 0.8f, 1.2f, 1.1f);
        private static readonly StoryMults CalmQuiet = new StoryMults(0.9f, 0.95f, 0.9f, 50, 1f, 1f, 1f, 0.9f, 1.1f, 0.9f, 1f);
        private static readonly StoryMults CalmBusy = new StoryMults(1.25f, 1.25f, 1.25f, 50, 1f, 1.25f, 0.75f, 1.25f, 0.75f, 1.25f, 1.2f);
        private static readonly StoryMults LivelyEven = new StoryMults(1f, 1f, 1f, 50, 1.1f, 1f, 1f, 0.9f, 1.1f, 0.9f, 1f);
        private static readonly StoryMults LivelyQuiet = new StoryMults(0.65f, 0.8f, 0.75f, 58, 1.2f, 0.7f, 1.3f, 0.65f, 1.3f, 0.75f, 0.8f);
        private static readonly StoryMults LivelyBusy = new StoryMults(1.15f, 1.1f, 1.15f, 50, 1f, 1.15f, 0.9f, 1.15f, 0.9f, 1.1f, 1.1f);
        // Wild's chance meeting first only a little above 50 (the review: at 60/65 it took the main slot from the minds' own
        // reasons, which run 40-55); its liveliness comes from the gaps, the walk-ups and the scene nearer. Busy lengthens the
        // main and side gaps in every mode.
        private static readonly StoryMults WildEven = new StoryMults(1f, 0.9f, 0.75f, 52, 1.2f, 0.85f, 1.2f, 0.75f, 1.25f, 0.75f, 0.85f);
        private static readonly StoryMults WildQuiet = new StoryMults(0.45f, 0.65f, 0.5f, 55, 1.33f, 0.5f, 1.6f, 0.5f, 1.5f, 0.5f, 0.6f);
        private static readonly StoryMults WildBusy = new StoryMults(1.1f, 1f, 0.85f, 50, 1.1f, 1f, 1.1f, 0.85f, 1.1f, 0.85f, 0.9f);

        private static readonly StoryMults CalmEvenR = CalmEven.Riding(), CalmQuietR = CalmQuiet.Riding(), CalmBusyR = CalmBusy.Riding();
        private static readonly StoryMults LivelyEvenR = LivelyEven.Riding(), LivelyQuietR = LivelyQuiet.Riding(), LivelyBusyR = LivelyBusy.Riding();
        private static readonly StoryMults WildEvenR = WildEven.Riding(), WildQuietR = WildQuiet.Riding(), WildBusyR = WildBusy.Riding();

        public static StoryMults Table(StoryMode mode, StoryBand band, bool riding)
        {
            if (mode == StoryMode.Off || band == StoryBand.Held)
                return StoryMults.Neutral;
            switch (mode)
            {
                case StoryMode.Calm:
                    return band == StoryBand.Quiet ? (riding ? CalmQuietR : CalmQuiet) : band == StoryBand.Busy ? (riding ? CalmBusyR : CalmBusy) : (riding ? CalmEvenR : CalmEven);
                case StoryMode.Wild:
                    return band == StoryBand.Quiet ? (riding ? WildQuietR : WildQuiet) : band == StoryBand.Busy ? (riding ? WildBusyR : WildBusy) : (riding ? WildEvenR : WildEven);
                default:
                    return band == StoryBand.Quiet ? (riding ? LivelyQuietR : LivelyQuiet) : band == StoryBand.Busy ? (riding ? LivelyBusyR : LivelyBusy) : (riding ? LivelyEvenR : LivelyEven);
            }
        }
    }
}
