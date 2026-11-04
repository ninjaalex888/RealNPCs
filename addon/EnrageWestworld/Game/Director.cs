// Stages a Scene with real people standing near the player: turns them to each other, shows each line as a
// subtitle, plays a matching voiced line from the game in the speaker's own voice, shoves, fists, draws,
// kneels, drinks, walks off. Stops the moment anything goes wrong (someone dies, gets drawn into a fight with
// the player, the player draws on them, or the player leaves). The player can step in: Intrude().
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed class Director
    {
        private sealed class Live
        {
            public Scene Scene;
            public Ped[] Cast;
            public string[] Names;
            public string BarText;               // "Nearby: ..." for the ticker, made once (TickerLine)
            public bool[] Introduced;
            public bool[] Women;
            public string[] Kinds;
            public int Index;
            public int NextAt;
            public int StartedAt;
            public bool Fighting;
            public int FightUntil;               // until blows start (FightStartMs), then the fight's own length from then
            public bool FightOn;                 // they're at it
            public int FightA, FightB, FightAskedAt, FightOrderedAt, FightMs;
            public Dictionary<int, bool[]> FightAttrs;   // each fighter's own combat attributes, put back at the end
            public readonly Dictionary<int, int> StartHp = new Dictionary<int, int>();   // each fighter's health when he came into it, by handle (a joiner may lead a side later)
            public int FightId;                  // this fight's own number, the same however its leaders change (seeds, "this fight has had its lawman")
            public bool WasBig;                  // two or more joined it once: a brawl, which a man going down doesn't end
            public bool SideGone;                // everyone on one side is down or out: the fight ends
            public int FightOnAt;                // when the blows began (FightOn): a brawl lasts BrawlLeastMs from then
            public int WaitSide = -1, WaitUntil;  // a side with nobody left standing waits this long for one of its own on the way (Coming)
            public readonly HashSet<int> Out = new HashSet<int>();   // down (knocked out or killed) or gone from the fight (a gun drawn): no longer in it, and never a reason to stop the scene
            public string Seed;
            public Action Done;
            public SceneCast Info;
            public bool Intruded;
            public bool[] Aimed = new bool[3];   // guns out in a standoff: put away if the scene is cut short
            public int MeetUntil;                // "meet": B is walking over; nothing is said until they're close, or give up then
            public bool Slamming;                // "slam": the rough move is playing (WestworldScript.Roughs.cs); nothing else until it's done
            public bool WasFighting;             // a "fight" was started: at the end both are let go of it, however it ended
            public bool Blows;                   // and blows were traded (FightOn came): only then is there an aftermath (FightOver)
            public int CarryUntil, CarryBy, CarryWho;   // "carry": the game's own carry asked for, waited on until then
            public int Shooter, ShotAt, ShootUntil;   // "shoot": who fired at whom, and until when the gunplay is let run
            public int Shover, Shoved, ShoveUntil;    // "shove": who shoved whom, and until when stopping him is no crime
            public int MeetWalker = 1;           // who walks over in "meet" (the second, or the first for trouble)
            public int MeetGoAt;                 // the walker was sitting or leaning: he sets off once he's up (0: on his way)
            public bool Downed;                  // "passout" played: he's lying there until "cometo", or the end gets him up
            public bool MadeDrunk;               // A was given a drunk voice for the scene
            public bool HappenedSent;            // the scene's defining moment has been reported (Happened)
            public int ReactAt;                  // when the one spoken to reacts to the line (0: no reaction due)
            public int ReactBy, ReactTo;         // who reacts, and to whom
            public string ReactTone;             // the tone of the line they react to
            public int Sat = -1;                 // "sitdown": who is sitting on the ground after a fall, until "getup" (or the end gets them up)
            public int ApproachUntil;            // "approach" by the third: the next beat waits until they're at the one they walk to, or goes on then
            public int ApproachTo;               // whom the third walks to
            public int ApproachGoAt;             // the third was sitting or leaning: they set off once they're up (0: on their way)
            public int ApproachCheckAt;          // when the walk was last seen getting nearer, or last sent again
            public float ApproachBest;           // the nearest they've been
            public bool Side;                    // a quiet talk beside the scene (Start with side): voices and near subtitles only
            public bool Ended;                   // End has begun: the joiners are let go for good
            public List<int[]> Joiners = new List<int[]>();   // those who joined the fight: { handle, side (0 with A, 1 with B), whom they went for } (Core/JoinIn.cs)
        }

        /// What one of the cast does about a line said to them, or about the slam ("hurt"), for the game layer to play.
        public sealed class Reaction
        {
            public Ped By, To;
            public string Tone, Temper, ByName, ToName, Seed;
            public bool Woman;
            public bool Known;      // the two have a tie (the speaker is no stranger to the one reacting)
            public int Warm;        // the tie's warmth, -3..3 (0 none)
        }

        internal static readonly uint SmokeHash = Natives.Hash("WORLD_HUMAN_SMOKE");   // a smoke in place (the scenes' "smoke" step, the mind's showoff)
        internal static readonly uint Crouch = Natives.Hash("WORLD_HUMAN_CROUCH_INSPECT");   // the crouch over something on the ground (also the night visitor's)
        internal static readonly uint GraveMourning = Natives.Hash("WORLD_HUMAN_GRAVE_MOURNING");   // standing over a grave (the mourn act; the lawman's mourner in WestworldScript.Bodies.cs)
        private Live _live;
        // a second full scene at once ([Scenes] MainScenesAtOnce 2; NIGHT4 item 3), at least SecondApart from the first's people and
        // never the same people; only what SecondOk allows (words, the walk-over, someone walking up), so the one-owner machinery
        // (the fight, the ring and the joiners, the rough move, Happened, the pickpocket, the fall) only ever sees _live. Never
        // promoted: when _live ends first, the second plays out as the second
        private Live _second;
        public const float SecondApart = SceneSlots.Apart;
        public int SecondStarted, SecondRefusedApart;
        // quiet talks playing at the same time as the scene, elsewhere on the street (the NPC-life run, Lex 2026-10-30: "several
        // at once"): words only, never blows, a walk-over or a callback, so the scene's one-owner parts (the rough move, the
        // fight, Happened, the pickpocket's cast) stay the scene's
        private readonly List<Live> _side = new List<Live>();
        private readonly List<Live> _sideTick = new List<Live>();   // Tick's snapshot of _side, reused
        private int _player;
        private Action<string, string, bool, bool, int> _say;

        /// Called for actions the director doesn't know ("cb:reveal"), with the part after "cb:".
        public Action<string> Custom;
        /// The same, with the scene it played in: its bodies and its cast (the review of items 11-12: a word that must know
        /// who said it, in a side or a second scene as much as the main one).
        public Action<string, Ped[], SceneCast> CustomScene;
        /// The saloon's door near the player, for the drunk taking it outside ("outside"; WestworldScript.Saloon.cs); null: none known.
        public Func<Vector3?> OutsideSpot;

        /// "slam": the game layer plays the rough move, attacker then victim; false if it can't (then it's a shove).
        public Func<Ped, Ped, bool> Slam;
        /// While the rough move is still playing.
        public Func<bool> SlamGoing;
        /// The scene ended while the move played: stop it.
        public Action SlamStop;
        /// How the move came out once it's over: "rough" (it played), "shove" (it couldn't, so a shove), or null (nothing).
        public Func<string> SlamResult;
        /// The scene's defining moment really happened, once per scene: "rough" (the move began), "shove" (it couldn't,
        /// so a shove), "drunk" (he went down), "lift" (the thief walked off with it). Ties for these are noted then.
        public Action<string> Happened;
        /// The one spoken to reacts (WestworldScript.Reactions.cs); null: nobody reacts.
        public Action<Reaction> React;
        /// A moment of the scene the street would notice (SIXDAY 5b, WestworldScript.Street.cs): "blows" as the fists fly, "square"
        /// as two men square up unarmed, "aim" as a gun comes out; (the moment, who it's on, the scene's kind, its seed).
        public Action<string, int, string, string> Shock;
        /// A fight between the scene's two is over, blows having been traded: the one left standing (the better off), then the
        /// other (WestworldScript.Aftermath.cs).
        public Action<int, int> FightOver;
        /// A fight scene stopped (someone in combat with the player, or aimed at) while its two were at it: (A, B, when the
        /// blows began), so the law can still hear it if they go on (WestworldScript.LooseFight.cs).
        public Action<int, int, int> FightLeftLoose;
        /// "meet" gave up: the two never got close enough, so nothing happened between them.
        public Action<Ped, Ped, bool> NeverMet;   // (the two, and whether it was the second scene)
        /// Asked as the third sets off to walk over (the one who steps in, a friend, a spouse): why they mayn't be moved
        /// now, or null. They were free when the scene was built; a story mission, enRAGE or a fight may have taken them
        /// up since (night 2026-10-29: the mind moves people, checked at the roll and again at the moment).
        public Func<Ped, string> ThirdFree;

        // the drunk who calls a man out (Scenes.Drunk): the game's own drunk-dueler moves, one ped at a time
        public const string DrunkDict = "script_re@drunk_dueler";

        public bool Busy { get { return _live != null; } }
        /// The quiet talks playing beside the scene now.
        public int SideCount { get { return _side.Count; } }
        /// Anything playing, the scene or a quiet talk (DramaTick ticks while there is).
        public bool AnyPlaying { get { return _live != null || _second != null || _side.Count > 0; } }
        /// A second full scene is playing ([Scenes] MainScenesAtOnce).
        public bool SecondBusy { get { return _second != null; } }
        /// The full scenes playing now (not the quiet talks): 0, 1 or 2.
        public int MainCount { get { return (_live != null ? 1 : 0) + (_second != null ? 1 : 0); } }
        /// The second scene's kind, or null.
        public string SecondKind { get { return _second != null ? _second.Scene.Kind : null; } }
        public string LastSummary { get; private set; }
        public string CurrentKind { get { return _live != null ? _live.Scene.Kind : null; } }

        /// Is this ped one of the cast of the scene running now?
        /// The one who started a street fight, while the two are at it, and the man he is at (0 when none): the bully the
        /// player may step in on (WestworldScript.Watch.cs; Lex, 2026-10-30).
        public int FightStarter(out int against)
        {
            against = 0;
            Live l = _live;
            if (l == null || !l.Fighting || !l.FightOn || l.FightA == 0)
                return 0;
            against = l.FightB;
            return l.FightA;
        }

        /// The street scene playing now, for the bar at the bottom: what it is and who is in it, and where; null when none.
        public string TickerLine(out Vector3 at)
        {
            at = default(Vector3);
            Live l = _live;
            if (l == null && _side.Count > 0)
            {
                Vector3 me = Game.Player.Character.Position;
                l = _side.Where(x => x.Cast.Length > 0 && x.Cast[0] != null && x.Cast[0].Exists()).OrderBy(x => x.Cast[0].Position.DistanceTo(me)).FirstOrDefault();
            }
            if (l == null || l.Scene == null || string.IsNullOrEmpty(l.Scene.Summary) || l.Cast == null || l.Cast.Length == 0 || l.Cast[0] == null || !l.Cast[0].Exists())
                return null;
            at = l.Cast[0].Position;
            if (l.BarText == null)
            {
                string who = l.Names != null ? string.Join(" and ", l.Names.Take(2).Where(n => !string.IsNullOrEmpty(n))) : "";
                l.BarText = Grammar.UpperFirst(l.Scene.Summary) + (who.Length > 0 ? " (" + who + ")" : "");
            }
            return l.BarText;
        }

        /// Every scene playing now, the scene and the quiet talks, as ("Two men came to blows (Gideon and Asa)", where).
        /// One list, refilled on each call: read it before the next (the frame-cost audit, NIGHT4 item 6: a new list and a Concat
        /// every frame before).
        public List<KeyValuePair<string, Vector3>> TickerLines()
        {
            _tickerAll.Clear();
            TickerAdd(_live);
            TickerAdd(_second);
            for (int i = 0; i < _side.Count; i++)
                TickerAdd(_side[i]);
            return _tickerAll;
        }

        private readonly List<KeyValuePair<string, Vector3>> _tickerAll = new List<KeyValuePair<string, Vector3>>();

        private void TickerAdd(Live l)
        {
            if (l == null || l.Scene == null || string.IsNullOrEmpty(l.Scene.Summary) || l.Cast == null || l.Cast.Length == 0 || l.Cast[0] == null || !l.Cast[0].Exists())
                return;
            if (l.BarText == null)
            {
                string who = l.Names != null ? string.Join(" and ", l.Names.Take(2).Where(n => !string.IsNullOrEmpty(n))) : "";
                l.BarText = Grammar.UpperFirst(l.Scene.Summary) + (who.Length > 0 ? " (" + who + ")" : "");
            }
            _tickerAll.Add(new KeyValuePair<string, Vector3>(l.BarText, l.Cast[0].Position));
        }

        /// Arthur aims at this one (WestworldScript.AimLetGo.cs): let go of him at once, so the game's own answer to a gun
        /// takes over. One of a scene's cast ends that scene; one in the fight who isn't of the cast (a joiner, or a joiner
        /// leading a side) is dropped from it and his events let go. What held him ("a street scene (kind)"), or null.
        public string LetGo(int h)
        {
            if (h == 0)
                return null;
            foreach (Live l in new[] { _live, _second }.Where(x => x != null).Concat(_side).ToArray())
            {
                foreach (Ped p in l.Cast)
                    if (p != null && p.Handle == h)
                    {
                        string kind = l.Scene.Kind;
                        Stop(l, "someone in it was aimed at");
                        return "a Director scene (" + kind + ")";
                    }
                if (l.Fighting && SideOf(l, h) >= 0 && !l.Out.Contains(h))
                {
                    string kind = l.Scene.Kind;
                    if (l.FightOn)
                        DropFromFight(l, h);
                    else
                        l.Joiners.RemoveAll(j => j[0] == h);
                    if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
                    {
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                        TASK.CLEAR_PED_TASKS(h, true, false);
                    }
                    return "a Director scene (" + kind + ", in the fight)";
                }
            }
            return null;
        }

        public bool Involves(int handle)
        {
            if (handle == 0)
                return false;
            Live l = _live;
            if (l != null)
            {
                if (l.Fighting && (handle == l.FightA || handle == l.FightB))
                    return true;   // a joiner leading a side since its first man went down is in neither list below
                foreach (Ped p in l.Cast)   // loops, not Any: no closure (the busy test asks this every frame for the mind's acts)
                    if (p != null && p.Handle == handle)
                        return true;
                foreach (int[] j in l.Joiners)
                    if (j[0] == handle)
                        return true;
            }
            if (_second != null)
                foreach (Ped p in _second.Cast)
                    if (p != null && p.Handle == handle)
                        return true;
            foreach (Live x in _side)
                foreach (Ped p in x.Cast)
                    if (p != null && p.Handle == handle)
                        return true;
            return false;
        }

        private readonly Dictionary<int, int> _leaving = new Dictionary<int, int>();   // handle -> until: walking off together ("leavewith")

        /// Still walking off together after a scene's "leavewith" (the take-away, night 2026-10-29): busy for everything
        /// else for 30 s, though the scene itself has ended (the collision audit: an onlooker's wander or a wake act cut it).
        public bool Leaving(int handle)
        {
            int until;
            if (handle == 0 || !_leaving.TryGetValue(handle, out until))
                return false;
            int now = Game.GameTime;
            if (now < until && until - now <= LeaveHoldMs)
                return true;
            _leaving.Remove(handle);
            return false;
        }

        private const int LeaveHoldMs = 30000;

        public Ped CastAt(int i)
        {
            Live l = _live;
            return l != null && i >= 0 && i < l.Cast.Length ? l.Cast[i] : null;
        }

        /// The full scene playing, its cast's description and its seed (null with none): for the Talk menu's word with the
        /// creditor of a debt scene (WestworldScript.Fondness.cs).
        public Scene CurrentScene { get { return _live != null ? _live.Scene : null; } }
        public SceneCast CurrentInfo { get { return _live != null ? _live.Info : null; } }
        public string CurrentSeed { get { return _live != null ? _live.Seed : null; } }
        public bool CurrentIntruded { get { return _live != null && _live.Intruded; } }
        public int CurrentIndex { get { return _live != null ? _live.Index : 0; } }   // the next beat's index: how far the scene has played

        /// The full scene waits: its next beat no sooner than `ms` from now (the Talk menu open on one of its people).
        public void HoldLive(int ms)
        {
            Live l = _live;
            if (l != null)
                l.NextAt = Math.Max(l.NextAt, Game.GameTime + ms);
        }

        /// The rest of the full scene replaced by `s` (the player's step-in rewrites it the same way): the beats given
        /// play from here, then the scene ends as any other. False with no scene, or one already rewritten or fighting.
        public bool Rewrite(Scene s, string why)
        {
            Live l = _live;
            if (l == null || s == null || s.Beats.Count == 0 || l.Intruded || l.Fighting)
                return false;
            Scene r = new Scene { Kind = l.Scene.Kind, Cast = l.Scene.Cast, Fight = l.Scene.Fight, Summary = s.Summary ?? l.Scene.Summary, Amount = l.Scene.Amount };
            r.Beats.AddRange(s.Beats.Where(b => b.Who < l.Cast.Length));
            l.Scene = r;
            l.Index = 0;
            l.Intruded = true;
            l.NextAt = Game.GameTime + 300;
            Log.Write("SCENE " + r.Kind + ": " + why);
            return true;
        }

        /// How far `at` is from the nearest of either full scene's people (float.MaxValue with none playing): the encounters keep
        /// clear of the full scenes (NIGHT4 item 6, the review across features). No allocation.
        public float FullSceneMetres(Vector3 at)
        {
            float best = float.MaxValue;
            for (int k = 0; k < 2; k++)
            {
                Live l = k == 0 ? _live : _second;
                if (l == null || l.Cast == null)
                    continue;
                for (int i = 0; i < l.Cast.Length; i++)
                    if (l.Cast[i] != null && l.Cast[i].Exists())
                        best = Math.Min(best, l.Cast[i].Position.DistanceTo(at));
            }
            return best;
        }

        public string NameAt(int i)
        {
            Live l = _live;
            return l != null && i >= 0 && i < l.Names.Length ? l.Names[i] : null;
        }

        public static bool OldModel(Ped p)
        {
            string m = Natives.ModelName(p);
            return m.Contains("_m_o_") || m.Contains("_f_o_");
        }

        public static string Kind(Ped p, bool woman)
        {
            string m = Natives.ModelName(p);
            if (m.Contains("_m_o_")) return "an old-timer";
            if (m.Contains("_f_o_")) return "an old woman";
            if (m.Contains("upperclass") || m.Contains("fancy") || m.Contains("highsociety")) return woman ? "a lady" : "a gentleman";
            if (m.Contains("prostitute") || m.Contains("whore")) return "a working girl";
            if (m.Contains("laborer") || m.Contains("worker") || m.Contains("foreman")) return woman ? "a working woman" : "a labourer";
            if (m.Contains("farm") || m.Contains("rancher")) return woman ? "a farm wife" : "a farmhand";
            if (m.Contains("miner")) return "a miner";
            if (m.Contains("dock") || m.Contains("boatcrew")) return "a dockhand";
            if (m.Contains("cardgame") || m.Contains("gambl")) return "a card player";
            if (m.Contains("slums") || m.Contains("rough") || m.Contains("criminal") || m.Contains("thug") || m.Contains("hillbilly")) return woman ? "a hard-looking woman" : "a roughneck";
            if (m.Contains("survivalist") || m.Contains("hunter")) return "a trapper";
            if (m.Contains("townfolk")) return woman ? "a townswoman" : "a townsman";
            return woman ? "a woman" : "a man";
        }

        /// Starts a scene with these people (2 or 3). names: acquaintance names when enRAGE knows them, else null.
        /// side: a quiet talk beside the scene (Quiet), at most maxSide at once.
        /// second: a second full scene beside the first ([Scenes] MainScenesAtOnce), only when the first plays, SecondOk allows
        /// it and its people are SecondApart from the first's. Any full scene keeps SecondApart from the other's people.
        public bool Start(Scene s, List<Ped> cast, string[] knownNames, string seed, Action done, SceneCast info = null, bool side = false, int maxSide = 1, bool second = false)
        {
            if (s == null || cast.Count < s.Cast || (side ? _side.Count >= maxSide || !Quiet(s) : second ? _live == null || _second != null || !SecondOk(s) : _live != null))
                return false;
            if (cast.Take(s.Cast).Any(p => p != null && Involves(p.Handle)))
                return false;
            Live other = side ? null : second ? _live : _second;
            if (other != null && !SceneSlots.FarEnough(XY(cast.Take(s.Cast)), XY(other.Cast)))
            {
                SecondRefusedApart++;
                return false;   // two full scenes keep 20 m apart, whichever began first
            }
            Live l = new Live
            {
                Scene = s, Cast = cast.Take(s.Cast).ToArray(), Names = new string[s.Cast], Introduced = new bool[s.Cast], Women = new bool[s.Cast],
                Kinds = new string[s.Cast], NextAt = Game.GameTime + 600, StartedAt = Game.GameTime, Seed = seed, Done = done, Info = info ?? new SceneCast()
            };
            for (int i = 0; i < l.Cast.Length; i++)
            {
                int h = l.Cast[i].Handle;
                l.Women[i] = !PED.IS_PED_MALE(h);
                l.Kinds[i] = Kind(l.Cast[i], l.Women[i]);
                l.Names[i] = knownNames != null && i < knownNames.Length && knownNames[i] != null ? knownNames[i] : Scenes.FirstName(l.Women[i], seed + i);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            }
            l.Side = side;
            if (side)
                _side.Add(l);
            else if (second)
            {
                _second = l;
                SecondStarted++;
            }
            else
                _live = l;
            foreach (string d in Gestures.Dicts)
                STREAMING.REQUEST_ANIM_DICT(d);   // body language for the lines (Gestures), loaded by the time they're said
            if (s.Beats.Any(x => x.Act == "challenge" || x.Act == "passout" || x.Act == "cometo"))
            {
                STREAMING.REQUEST_ANIM_DICT(DrunkDict);
                AUDIO.SET_PED_IS_DRUNK(l.Cast[0].Handle, true);   // he sounds it, too
                l.MadeDrunk = true;
            }
            Log.Write("SCENE " + (side ? "(beside, " + _side.Count + " now) " : second ? "(second) " : "") + s.Kind + ": " + string.Join(" & ", l.Names));
            return true;
        }

        // what a quiet talk may do: words, turning to each other, a smoke or a flask, walking off; nothing that needs the
        // scene's one-owner machinery (blows, the rough move, a walk-over, a drunk on the floor, a callback)
        private static readonly HashSet<string> QuietActs = new HashSet<string> { "face", "faceone", "lookplayer", "smoke", "flask", "whittle", "stoic", "stand", "walkaway", "cb:wakeline" };   // cb:wakeline: a word to the game side only (the hearing booked), no task

        // what a second full scene may do besides a quiet talk's: the walk-over and someone walking up (their state is the scene's own)
        private static readonly HashSet<string> SecondActs = new HashSet<string> { "face", "faceone", "lookplayer", "smoke", "flask", "whittle", "stoic", "stand", "walkaway", "meet", "approach", "cb:wakeline" };
        private static readonly HashSet<string> SecondNot = new HashSet<string> { "pickpocket", "lift", "rough", "drunk", "help", "passby", "hosts", "carry" };

        private static float[] XY(IEnumerable<Ped> ps)
        {
            var xy = new List<float>();
            foreach (Ped p in ps)
                if (p != null && p.Exists())
                {
                    Vector3 v = p.Position;
                    xy.Add(v.X);
                    xy.Add(v.Y);
                }
            return xy.ToArray();
        }

        /// Can this scene play as a second full scene: two or three people, no fight, no quarrel ladder, nothing noted at a cue,
        /// not a theft, a rough move, a drunk, a fall or a host, and only the quiet acts, the walk-over and someone walking up.
        public static bool SecondOk(Scene s)
        {
            return s != null && (s.Cast == 2 || s.Cast == 3) && !s.Fight && s.Ladder == null && !s.NoteAtCue && !SecondNot.Contains(s.Kind) && s.Beats.All(b => b.Act == null || SecondActs.Contains(b.Act));
        }

        /// Stops the second scene (a test making room).
        public void AbortSecond(string why)
        {
            if (_second != null)
                Stop(_second, why);
        }

        /// Can this scene play as a quiet talk beside another: two people, no fight, no quarrel ladder, only quiet acts.
        public static bool Quiet(Scene s)
        {
            return s != null && s.Cast == 2 && !s.Fight && s.Ladder == null && !s.NoteAtCue && s.Beats.All(b => b.Act == null || QuietActs.Contains(b.Act));
        }

        private static string Temper(Live l, int i)
        {
            return l.Info != null && l.Info.Tempers != null && i < l.Info.Tempers.Length ? l.Info.Tempers[i] : null;
        }

        /// A townsfolk gesture that fits the line's tone (Core/Gestures.cs), on the upper body while they say it,
        /// about half the time; never for someone sitting or leaning in a scenario, or before the set has loaded.
        private static void Gesture(int h, string tone, bool woman, string seed)
        {
            if (!Voice.Chance(seed + "gesture", 55) || PED.IS_PED_USING_ANY_SCENARIO(h))
                return;
            string clip;
            string dict = Gestures.For(tone, woman, seed, out clip);
            if (!STREAMING.HAS_ANIM_DICT_LOADED(dict))
                return;
            TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
        }

        /// The two swapped places (B won the fight the script gave to A): the lines still to come swap their names too,
        /// so nobody calls the winner by his own name. A beat that follows its people (`Beat.Follows`, the ladder's)
        /// swaps its Who and To instead and keeps its line: the man the ladder had speaking still speaks it, by name.
        private static void SwapNamesInLines(Live l, string x, string y)
        {
            Regex both = string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || x == y ? null : new Regex(@"\b(" + Regex.Escape(x) + "|" + Regex.Escape(y) + @")\b");
            for (int j = l.Index; j < l.Scene.Beats.Count; j++)
            {
                Beat b = l.Scene.Beats[j];
                if (b.Follows)
                {
                    if (b.Who < 2)
                        b.Who = 1 - b.Who;
                    if (b.To == 0 || b.To == 1)
                        b.To = 1 - b.To;
                }
                else if (b.Line != null && both != null)
                    b.Line = both.Replace(b.Line, m => m.Value == x ? y : x);
            }
        }

        /// Stops the scene (not the quiet talks beside it; AbortAll stops those too).
        public void Abort(string why)
        {
            if (_live != null)
                Stop(_live, why);
        }

        /// Stops everything playing: the scene and the quiet talks (the script stopping, a mission, a wipe).
        public void AbortAll(string why)
        {
            Abort(why);
            AbortSecond(why);
            foreach (Live x in _side.ToArray())
                Stop(x, why);
        }

        private void Stop(Live l, string why)
        {
            Log.Write("SCENE " + (l.Side ? "(beside) " : ReferenceEquals(l, _second) ? "(second) " : "") + "stopped after " + l.Index + " of " + l.Scene.Beats.Count + " beats: " + why);
            End(l);
            LetGoCutShort(l);
        }

        /// A scene cut off before its end (a reload, a mission, the player gone) leaves its people holding its last task: stood
        /// still facing someone, a gesture held (Lex, 2026-11-01: after an Insert reload "quite a few NPCs near me are in a
        /// frozen state", gone once he walked away). Each one free of a scenario, not down and not the player is sent on his
        /// way: their tasks cleared and a wander the same tick. Someone in a scenario is left in it (a bare clear froze them).
        private void LetGoCutShort(Live l)
        {
            for (int i = 0; i < l.Cast.Length; i++)
            {
                Ped p = l.Cast[i];
                if (p == null || !p.Exists() || p.Handle == _player || ENTITY.IS_ENTITY_DEAD(p.Handle) || l.Out.Contains(p.Handle) ||
                    PED.IS_PED_USING_ANY_SCENARIO(p.Handle) || PED.IS_PED_RAGDOLL(p.Handle))
                    continue;
                TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                TASK.TASK_WANDER_STANDARD(p.Handle, 10f, 10);
            }
        }

        private void End(Live l)
        {
            if (ReferenceEquals(l, _live))
                _live = null;
            else if (ReferenceEquals(l, _second))
                _second = null;
            else
                _side.Remove(l);
            if (l.Slamming)
                SlamStop?.Invoke();
            Ped a0 = l.Cast.Length > 0 ? l.Cast[0] : null;
            if (a0 != null && a0.Exists() && !ENTITY.IS_ENTITY_DEAD(a0.Handle))
            {
                // a drunk still lying where he fell gets up as the scene ends, whatever ended it, and sobers in his voice
                if (l.Downed && STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict) && ENTITY.IS_ENTITY_PLAYING_ANIM(a0.Handle, DrunkDict, "pass_out_fwd_v1_drunk", 3))
                    TASK.TASK_PLAY_ANIM(a0.Handle, DrunkDict, "pass_out_fwd_v1_exit_drunk", 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                if (l.MadeDrunk)
                    AUDIO.SET_PED_IS_DRUNK(a0.Handle, false);
            }
            // someone still sitting where they fell gets up as the scene ends, whatever ended it, and goes on their way
            if (l.Sat >= 0 && l.Sat < l.Cast.Length && l.Cast[l.Sat] != null && l.Cast[l.Sat].Exists() && !ENTITY.IS_ENTITY_DEAD(l.Cast[l.Sat].Handle))
                ScenarioExit.SoftUp(l.Cast[l.Sat].Handle, x => TASK.TASK_WANDER_STANDARD(x, 10f, 10));   // the exit flag and the wander the same tick (Game/ScenarioExit.cs)
            if ((l.Scene.Kind == "help" || l.Scene.Kind == "passby") && l.Cast.Length > 1 && l.Cast[1] != null && l.Cast[1].Exists() && !ENTITY.IS_ENTITY_DEAD(l.Cast[1].Handle) &&
                !PED.IS_PED_USING_ANY_SCENARIO(l.Cast[1].Handle))
                TASK.TASK_WANDER_STANDARD(l.Cast[1].Handle, 10f, 10);
            // a third still walking over when it ended (the player left, someone in it was hurt) is let go on their way
            if (l.ApproachUntil > 0 && l.Cast.Length > 2 && l.Cast[2] != null && l.Cast[2].Exists() && !ENTITY.IS_ENTITY_DEAD(l.Cast[2].Handle))
                TASK.TASK_WANDER_STANDARD(l.Cast[2].Handle, 10f, 10);
            if (l.Fighting && l.FightOn)
                SnapEnd(l, Game.GameTime);   // cut short with fists still flying: the aftermath watch sees the blows after
            l.Ended = true;
            LetJoinersGo(l);
            if (l.Shooter != 0 && ENTITY.DOES_ENTITY_EXIST(l.Shooter) && !ENTITY.IS_ENTITY_DEAD(l.Shooter))
            {
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Shooter, false);
                TASK.CLEAR_PED_TASKS(l.Shooter, true, false);
                FleeOrdered?.Invoke(l.Shooter);   // the run is a run, not cowering ([World] Bravery)
                TASK.TASK_SMART_FLEE_PED(l.Shooter, l.ShotAt != 0 && ENTITY.DOES_ENTITY_EXIST(l.ShotAt) ? l.ShotAt : _player, 150f, -1, 0, 3f, 0);   // he runs from what he's done
            }
            if (l.FightAttrs != null)
                foreach (KeyValuePair<int, bool[]> fa in l.FightAttrs)
                    if (ENTITY.DOES_ENTITY_EXIST(fa.Key))
                        for (int a = 0; a < FightAttrIds.Length; a++)
                            PED.SET_PED_COMBAT_ATTRIBUTES(fa.Key, FightAttrIds[a], fa.Value[a]);   // their own again
            for (int i = 0; i < l.Cast.Length; i++)
            {
                Ped p = l.Cast[i];
                if (p != null && p.Exists())
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                    if (l.Out.Contains(p.Handle))
                        continue;   // knocked out or killed in the brawl, or gone for his gun: what he's doing is the game layer's (a clear would stand a knocked-out man up)
                    if (((l.Fighting || l.WasFighting) && i < 2) || (i < l.Aimed.Length && l.Aimed[i]))
                        TASK.CLEAR_PED_TASKS(p.Handle, true, false);   // the two who fought (never the barkeep or a bystander)
                    if (i < l.Aimed.Length && l.Aimed[i])
                        WEAPON.SET_CURRENT_PED_WEAPON(p.Handle, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                }
            }
            if (l.Blows && !l.Side && l.Cast.Length >= 2 && l.Cast[0] != null && l.Cast[1] != null && l.Cast[0].Exists() && l.Cast[1].Exists() && FightOver != null)
            {
                // the two leading the sides at the end (a joiner may lead one once its first man went down): the one standing
                // wins; with both standing, the better off; with neither, no aftermath
                int fa = l.FightA != 0 ? l.FightA : l.Cast[0].Handle, fb = l.FightB != 0 ? l.FightB : l.Cast[1].Handle;
                bool aUp = ENTITY.DOES_ENTITY_EXIST(fa) && !ENTITY.IS_ENTITY_DEAD(fa) && !l.Out.Contains(fa);
                bool bUp = ENTITY.DOES_ENTITY_EXIST(fb) && !ENTITY.IS_ENTITY_DEAD(fb) && !l.Out.Contains(fb);
                bool first = aUp && bUp ? HealthOf(l, fa) >= HealthOf(l, fb) : aUp;
                try
                {
                    if (aUp || bUp)
                        FightOver(first ? fa : fb, first ? fb : fa);
                }
                catch
                {
                }
            }
            LastSummary = l.Scene.Summary;
            try
            {
                l.Done?.Invoke();
            }
            catch
            {
            }
        }

        private string Label(Live l, int who)
        {
            if (l.Introduced[who])
                return l.Names[who];
            l.Introduced[who] = true;
            return l.Names[who] + ", " + l.Kinds[who];
        }

        /// The player greeted (friendly) or antagonized one of the cast. Rewrites the rest of the scene around
        /// it. Returns the outcome ("peace", "egged", "sided") or null when it didn't apply.
        public string Intrude(int handle, bool friendly, out string kind)
        {
            kind = null;
            Live l = _live;
            if (l == null)
                return null;
            int who = Array.FindIndex(l.Cast, p => p != null && p.Handle == handle);
            if (who < 0 || l.Scene.Kind == "pickpocket" || l.Intruded)
                return null;
            kind = l.Scene.Kind;
            string outcome;
            Scene add = Scenes.Intrude(l.Scene.Kind, who, friendly, l.Info, l.Fighting, l.Seed + "|in", out outcome);
            if (add == null || add.Beats.Count == 0)
                return null;
            Scene s = new Scene { Kind = l.Scene.Kind, Cast = l.Scene.Cast, Fight = l.Scene.Fight, Summary = l.Scene.Summary };
            s.Beats.AddRange(add.Beats.Where(b => b.Who < l.Cast.Length));
            l.Scene = s;
            l.Index = 0;
            l.Intruded = true;
            l.NextAt = Game.GameTime + 300;
            if (l.Fighting && outcome != "egged")
            {
                l.Fighting = false;   // the first beat ends the fight
                l.FightUntil = 0;
                if (!s.Beats.Any(b => b.Act == "endfight"))
                {
                    // a scene the stranger's word has no fight-ending beat for (the trouble scenes): stop it here
                    for (int i = 0; i < Math.Min(2, l.Cast.Length); i++)
                        if (l.Cast[i] != null && l.Cast[i].Exists())
                        {
                            TASK.CLEAR_PED_TASKS(l.Cast[i].Handle, true, false);
                            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Cast[i].Handle, true);
                        }
                }
            }
            else if (l.Fighting && _say != null && s.Beats.Count > 0 && s.Beats[0].Line != null)
            {
                // egged on mid-fight: the line can't wait for the fight to finish
                Beat b0 = s.Beats[0];
                _say(Label(l, b0.Who), b0.Line, false, l.Women[b0.Who], l.Cast[Math.Min(b0.Who, l.Cast.Length - 1)].Handle);
                Speech.SayTag(l.Cast[b0.Who].Handle, b0.Voice, _player);
                s.Beats.RemoveAt(0);
                l.FightUntil = Math.Max(l.FightUntil, Game.GameTime + 4000);
            }
            Log.Write("SCENE " + l.Scene.Kind + ": you " + (friendly ? "stepped in" : "stirred it up") + (outcome != null ? " (" + outcome + ")" : ""));
            return outcome;
        }

        /// sideSay: where a quiet talk's lines go (the subtitle near you, never the queue the scene's lines take).
        public void Tick(Ped player, Action<string, string, bool, bool, int> say, Action<string, string, bool, bool, int> sideSay = null, Action<string, string, bool, bool, int> secondSay = null)
        {
            _player = player.Handle;
            _say = say;
            if (_live != null)
                TickOne(_live, player, say);
            if (_second != null)
                TickOne(_second, player, secondSay ?? say);   // its voices as any scene's; its words as a subtitle near you, never the first's queue
            if (_side.Count > 0)
            {
                // a snapshot, as ToArray was (a talk ended by another's tick still gets its own, in the same order), into one reused
                // list instead of a new array every frame (the frame-cost audit's follow-up, NIGHT4 item 6); Tick is never re-entered
                _sideTick.Clear();
                for (int i = 0; i < _side.Count; i++)
                    _sideTick.Add(_side[i]);
                for (int i = 0; i < _sideTick.Count; i++)
                    TickOne(_sideTick[i], player, sideSay ?? ((a, b, c, d2, e) => { }));
                _sideTick.Clear();
            }
        }

        private void TickOne(Live l, Ped player, Action<string, string, bool, bool, int> say)
        {
            int now = Game.GameTime;
            foreach (Ped p in l.Cast)
            {
                if (p != null && l.Out.Count > 0 && l.Out.Contains(p.Handle))
                    continue;   // down or out of the fight already: a brawl goes on without him (Lex, 2026-10-31)
                bool gone = p == null || !p.Exists() || p.IsDead;
                if (gone && p != null && p.Exists() && DownsOn && l.Fighting && l.FightOn && (l.WasBig || l.Joiners.Count > 0) && (p.Handle == l.FightA || p.Handle == l.FightB))
                {
                    FightDown(l, p.Handle, true);   // killed in a brawl before the down check saw him: dropped from the sides, and it goes on
                    continue;
                }
                bool atYou = !gone && PED.IS_PED_IN_COMBAT(p.Handle, player.Handle);
                bool aimed = !gone && !atYou && PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, p.Handle);
                if (gone || atYou || aimed)
                {
                    // who and why (Lex, 2026-11-02: a fight stopped here and the law never came; the line didn't say which of it)
                    int idx = Array.IndexOf(l.Cast, p);
                    string whoStop = idx >= 0 && l.Names != null && idx < l.Names.Length && !string.IsNullOrEmpty(l.Names[idx]) ? l.Names[idx] : "someone";
                    string what = gone ? (p == null || !p.Exists() ? " gone" : " dead") : atYou ? " in combat with you" : ", you aimed at them";
                    Stop(l, l.ShootUntil > 0 ? "shot down" : "someone in the scene is gone or threatened (" + whoStop + what + ")");
                    // a fistfight between the two of them that goes on without the scene: the law still hears it
                    if (l.Fighting && l.FightOn && FightLeftLoose != null && l.FightA != 0 && l.FightB != 0)
                        FightLeftLoose(l.FightA, l.FightB, l.FightOnAt);
                    return;
                }
            }
            // a brawl, while it lasts, runs past the scene's 90 s to its least time and a minute more, and holds within 80 m
            // (Lex, 2026-10-31: brawls reach further and last longer)
            // a brawl here is any fight with fists flying and a joiner in it or on the way (the review, 2026-10-31: one joiner
            // and two coming were cut at 90 s from the quarrel's first words), and its cap covers the fight's own furthest
            // end (FightOnAt + BrawlLeastMs + FightMostMs for those coming), so the fight's rules end it, not this
            bool brawl = l.Fighting && l.FightOn && (l.WasBig || l.Joiners.Count > 0 || ComingFor(0) + ComingFor(1) > 0);
            int mostMs = l.Scene.Ladder != null ? 150000 : 90000;   // a ladder quarrel that climbs the whole way runs past 90 s (the review)
            if (brawl)
                mostMs = Math.Max(mostMs, l.FightOnAt - l.StartedAt + BrawlLeastMs + Math.Max(60000, FightMostMs + 15000));
            Vector3 youAt = player.Position;
            float nearest = float.MaxValue;
            for (int i = 0; i < l.Cast.Length; i++)   // a loop, not Min: no closure a frame (the frame-cost audit)
                if (l.Cast[i] != null)
                    nearest = Math.Min(nearest, l.Cast[i].Position.DistanceTo(youAt));
            bool far = nearest > (brawl ? 80f : 50f);
            if (far || now - l.StartedAt > mostMs)
            {
                Stop(l, far ? "the player left" : "it ran out of time (" + mostMs / 1000 + " s)");
                return;
            }
            if (l.Fighting)
            {
                KeepFists(l.FightA);
                KeepFists(l.FightB);
                if (!l.FightOn)
                {
                    // at it: in melee, or a blow landed (the combat flag alone came the moment they were told, with no blows)
                    if (PED.IS_PED_IN_MELEE_COMBAT(l.FightA) || PED.IS_PED_IN_MELEE_COMBAT(l.FightB) || Health(l, 0) < 0.97f || Health(l, 1) < 0.97f)
                    {
                        l.FightOn = true;
                        l.Blows = true;
                        l.FightOnAt = now;
                        l.FightUntil = now + l.FightMs;
                        Log.Write("SCENE fight: at it " + ((now - l.FightAskedAt) / 1000.0).ToString("0.0") + " s after the word");
                        try { Shock?.Invoke("blows", l.FightA, l.Scene.Kind, l.Seed); } catch (Exception ex) { Log.Write("STREET: the blows weren't raised: " + ex.Message); }   // the street reacts (SIXDAY 5b)
                    }
                    else if (now >= l.FightUntil)
                    {
                        Stop(l, "they never came to blows in " + FightStartMs / 1000 + " s (" +
                              (PED.IS_PED_USING_ANY_SCENARIO(l.FightA) || PED.IS_PED_USING_ANY_SCENARIO(l.FightB) ? "still sat or leaning"
                               : PED.IS_PED_IN_COMBAT(l.FightA, l.FightB) || PED.IS_PED_IN_COMBAT(l.FightB, l.FightA) ? "in combat, never in melee" : "ordered, never in combat") + ")");
                        return;
                    }
                    else
                    {
                        if (now - l.FightOrderedAt > 2000)
                            FightOrder(l);
                        return;
                    }
                }
                foreach (int[] j in l.Joiners)
                    if (ENTITY.DOES_ENTITY_EXIST(j[0]) && !ENTITY.IS_ENTITY_DEAD(j[0]))
                        KeepFists(j[0]);
                if (DownsOn && !l.SideGone)
                    FightDownStep(l);   // a man beaten down: killed or knocked out (the game layer rolls it), and the brawl goes on
                if (l.WaitSide >= 0 && !l.SideGone && (now >= l.WaitUntil || ComingFor(l.WaitSide) == 0))
                {
                    Log.Write("SCENE fight: nobody of the fallen side came in time" + (now >= l.WaitUntil ? " (waited " + WaitMs / 1000 + " s)" : ""));
                    l.SideGone = true;   // the one on his way gave up or was taken off: the side is gone after all
                    l.FightUntil = 0;
                }
                int coming = ComingFor(0) + ComingFor(1);
                if (l.WasBig && !l.SideGone)
                    l.FightUntil = Math.Max(l.FightUntil, l.FightOnAt + BrawlLeastMs);   // a brawl lasts its least time ([Mind] BrawlLeastSeconds)
                if (coming > 0 && !l.SideGone)
                    l.FightUntil = Math.Max(l.FightUntil, Math.Min(now + 6000, l.FightOnAt + BrawlLeastMs + FightMostMs));   // those on their way to it keep it going a while
                bool hurt = (HealthOf(l, l.FightA) < 0.6f || HealthOf(l, l.FightB) < 0.6f) && !(FightMostMs > 20000 && (l.Joiners.Count >= 2 || l.WasBig || coming > 0));   // a brawl goes on past the first two's bruises ([Mind] MindBrawls), to its time
                if (now < l.FightUntil && !hurt && !l.SideGone)
                    return;
                l.Fighting = false;
                SnapEnd(l, now);   // who was still at it, for the blows the game lands after (the aftermath watch)
                LetJoinersGo(l);
                if (l.Out.Count > 0)
                {
                    foreach (Ped p in l.Cast)
                        if (p != null && l.Out.Contains(p.Handle))
                        {
                            Stop(l, "the fight is over, with " + l.Out.Count + " down or out of it");   // the rest of the scene's words would be a downed man's
                            return;
                        }
                }
            }
            if (l.CarryUntil > 0)
            {
                Ped by = new Ped(l.CarryBy);
                if (by.Exists() && by.IsCarryingSomething)
                {
                    l.CarryUntil = 0;
                    Vector3 dir = _player != 0 ? by.Position - new Ped(_player).Position : new Vector3(1f, 0f, 0f);
                    dir.Z = 0f;
                    dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                    Vector3 spot = by.Position + dir * 30f;
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(l.CarryBy, spot.X, spot.Y, spot.Z, 1f, -1, 1f, 0, 0f);
                    _leaving[l.CarryBy] = now + LeaveHoldMs;
                    _leaving[l.CarryWho] = now + LeaveHoldMs;
                    Log.Write("SCENE   " + l.Names[1] + " carries " + l.Names[0] + " home over the shoulder");
                    Stop(l, "carried off home");
                    return;
                }
                if (now < l.CarryUntil)
                    return;
                // the game wouldn't carry him: brought round, and walked home on the friend's arm
                l.CarryUntil = 0;
                Log.Write("SCENE   the carry didn't take (" + (PED.IS_PED_RAGDOLL(l.CarryWho) ? "limp" : "not limp") + "): " + l.Names[0] + " is brought round and walked home");
                TASK.CLEAR_PED_TASKS(l.CarryBy, true, false);
                l.Scene.Beats.Add(new Beat(0, null, null, "getup"));
                l.Scene.Beats.Add(new Beat(1, null, null, "homewith"));
                l.NextAt = now + 1500;
                return;
            }
            if (l.ShootUntil > 0)
            {
                if (now < l.ShootUntil)
                    return;
                Stop(l, "the shooting is over");
                return;
            }
            if (l.Slamming)
            {
                if (SlamGoing != null && SlamGoing())
                    return;
                l.Slamming = false;
                string result = SlamResult != null ? SlamResult() : null;
                if (result != null)
                    Report(l, result);   // only now is it known whether the face went into the bar
            }
            if (l.ReactAt > 0 && now >= l.ReactAt)
            {
                // the one spoken to reacts, unless they're about to speak or act themselves (theirs would cut it short)
                l.ReactAt = 0;
                int by = l.ReactBy;
                Beat next = l.Index < l.Scene.Beats.Count ? l.Scene.Beats[l.Index] : null;
                bool theirTurn = next != null && next.Who == by && (next.Line != null || next.Act != null) && l.NextAt - now < 1000;
                if (React != null && !theirTurn && !l.Fighting && l.MeetUntil == 0 && !(l.Downed && by == 0))
                {
                    bool tied = false;
                    int warm = l.Info != null ? l.Info.WarmthBetween(by, l.ReactTo, out tied) : 0;
                    if (l.Info == null)
                        tied = false;
                    React(new Reaction { By = l.Cast[by], To = l.Cast[l.ReactTo], Tone = l.ReactTone, Temper = Temper(l, by), Woman = l.Women[by],
                                         ByName = l.Names[by], ToName = l.Names[l.ReactTo], Seed = l.Seed + l.Index, Known = tied, Warm = warm });
                }
            }
            if (now < l.NextAt)
                return;
            if (l.MeetUntil > 0)
            {
                // the walker was sitting or leaning: he's up now, and sets off
                if (l.MeetGoAt > 0 && now >= l.MeetGoAt)
                {
                    l.MeetGoAt = 0;
                    Ped w = l.Cast[l.MeetWalker], o = l.Cast[1 - l.MeetWalker];
                    TASK.TASK_GO_TO_ENTITY(w.Handle, o.Handle, 20000, Natives.Brisk, 1.3f, 0f, 0);
                }
                // B is still walking over: the first word waits until they're face to face, or near enough across a bar or a
                // table when the one waiting sits or leans there (3.6 m: they often stopped short at the counter)
                Ped waiting = l.Cast[1 - l.MeetWalker];
                if (l.Cast[0].Position.DistanceTo(l.Cast[1].Position) <= (PED.IS_PED_USING_ANY_SCENARIO(waiting.Handle) && l.Sat < 0 ? 3.6f : 2.4f))   // someone down on the ground is helped from close by
                {
                    l.MeetUntil = 0;
                    Act(l, "face", 0, 1, 0);
                }
                else if (now > l.MeetUntil)
                {
                    Ped walker = l.Cast[l.MeetWalker], waiter = l.Cast[1 - l.MeetWalker];   // why, for the log: how far off, and whether a scenario held them
                    if (NeverMet != null)
                        NeverMet(l.Cast[0], l.Cast[1], ReferenceEquals(l, _second));
                    Stop(l, "they never got close enough to talk (still " + waiter.Position.DistanceTo(walker.Position).ToString("0.0") + " m apart" +
                          (PED.IS_PED_USING_ANY_SCENARIO(walker.Handle) ? ", a scenario held the walker" : "") +
                          (PED.IS_PED_USING_ANY_SCENARIO(waiter.Handle) ? ", a scenario held the other" : "") + ")");
                    return;
                }
                else
                    return;
            }
            if (l.ApproachUntil > 0 && l.Cast.Length > 2)
            {
                // the third is still walking over: the next beat waits until they're there, or the time is up
                Ped w = l.Cast[2], o = l.Cast[l.ApproachTo];
                if (l.ApproachGoAt > 0)
                {
                    if (now < l.ApproachGoAt)
                        return;
                    l.ApproachGoAt = 0;
                    TASK.TASK_GO_TO_ENTITY(w.Handle, o.Handle, 20000, Natives.Brisk, 1.3f, 0f, 0);
                    l.ApproachCheckAt = now;
                    return;
                }
                float d = w.Position.DistanceTo(o.Position);
                if (d <= ApproachNear)
                    l.ApproachUntil = 0;
                else if (now > l.ApproachUntil)
                {
                    l.ApproachUntil = 0;
                    Log.Write("SCENE   " + l.Names[2] + " never got there (still " + d.ToString("0.0") + " m off" +
                              (PED.IS_PED_USING_ANY_SCENARIO(w.Handle) ? ", a scenario held them" : "") + "): goes on from there");
                }
                else
                {
                    if (d < l.ApproachBest - 0.3f)
                    {
                        l.ApproachBest = d;
                        l.ApproachCheckAt = now;
                    }
                    else if (now - l.ApproachCheckAt > 3000)
                    {
                        // no nearer in 3 s (knocked off the walk, or it never took): the order again, as the kin walk-up does
                        TASK.TASK_GO_TO_ENTITY(w.Handle, o.Handle, 20000, Natives.Brisk, 1.3f, 0f, 0);
                        l.ApproachCheckAt = now;
                    }
                    return;
                }
            }
            if (l.Index >= l.Scene.Beats.Count)
            {
                Log.Write("SCENE " + (l.Side ? "(beside) " : ReferenceEquals(l, _second) ? "(second) " : "") + "over: " + l.Scene.Kind + " (" + string.Join(" & ", l.Names) + "), played to the end");
                End(l);
                return;
            }
            Beat b = l.Scene.Beats[l.Index++];
            int who = Math.Min(b.Who, l.Cast.Length - 1);
            if (b.Act == "approach" && who == 2 && l.Cast.Length > 2 && ThirdFree != null)
            {
                string taken = null;
                try
                {
                    taken = ThirdFree(l.Cast[2]);
                }
                catch (Exception ex)
                {
                    taken = "the check failed: " + ex.Message;
                }
                if (taken != null)
                {
                    Stop(l, l.Names[2] + " can't come over now (" + taken + ")");
                    return;
                }
            }
            int other = b.To >= 0 && b.To < l.Cast.Length && b.To != who ? b.To : who == 0 ? 1 : 0;   // said to the other, or to whom the beat names
            Ped me = l.Cast[who], them = l.Cast[other];
            int wait = 900;
            if (b.Act != null)
                wait = Math.Max(wait, Act(l, b.Act, who, other, b.Ms));
            if (b.Voice != null)
                Speech.SayTag(me.Handle, b.Voice, them.Handle);
            if (b.Late != null)
            {
                // a line that may have been on its way as the scene began (the model wording an answer, MindTalk): taken as it plays
                string late = null;
                try { late = b.Late(); } catch (Exception ex) { Log.Detail("SCENE a late line failed (" + ex.Message + "): the written one"); }
                if (!string.IsNullOrEmpty(late))
                    b.Line = late;
                b.Late = null;
            }
            if (b.Line != null)
            {
                Log.Write("SCENE   " + l.Names[who] + " (" + (int)me.Position.DistanceTo(new Ped(_player).Position) + " m from you, " +
                          (ENTITY.IS_ENTITY_ON_SCREEN(me.Handle) ? "on screen" : "off screen") + "): " + b.Line);
                if ((b.Act == null || b.Act == "face") && !l.Fighting)
                    Gesture(me.Handle, b.Voice, l.Women[who], l.Seed + l.Index);
                say(Label(l, who), b.Line, l.Scene.Kind == "hosts", l.Women[who], me.Handle);
                wait = Math.Max(wait, 1600 + b.Line.Length * 55);
                if (b.Act != "hurt" && b.Voice != null)
                {
                    l.ReactAt = now + 700;   // the one it's said to takes it, a moment in
                    l.ReactBy = other;
                    l.ReactTo = who;
                    l.ReactTone = b.Voice;
                }
            }
            l.NextAt = now + wait + (b.Act == "fight" ? 0 : b.Ms);
        }

        private int Act(Live l, string act, int who, int other, int ms)
        {
            Ped me = l.Cast[who], them = l.Cast[other];
            int h = me.Handle, t = them.Handle;
            switch (act)
            {
                case "face":
                    foreach (Ped p in l.Cast)
                    {
                        Ped target = p == me ? them : me;
                        // someone sitting at a table or leaning on the bar just looks over
                        if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, target.Handle, 4000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(p.Handle, target.Handle, 20000, 0, 51, 0);
                    }
                    return 1400;
                case "approach":
                    if (who == 2 && l.Cast.Length > 2)
                    {
                        // the third walks over to the pair (the one who steps in, a friend, a spouse) to the one the beat names
                        // (else A), and the next beat waits until they're there (Tick), as "meet" does. Before 2026-10-29 the
                        // walk had 2.6 s and the next beat's face turned them where they stood: the one who stepped in said it
                        // from 9 m off (Lex)
                        l.ApproachTo = other;
                        l.ApproachBest = me.Position.DistanceTo(them.Position);
                        l.ApproachCheckAt = Game.GameTime;
                        l.ApproachUntil = Game.GameTime + Math.Min(ApproachMaxMs, 4000 + (int)(l.ApproachBest * 800f));
                        if (PED.IS_PED_USING_ANY_SCENARIO(h))
                        {
                            // a walk order alone never moves someone sitting or leaning (seen 2026-10-25): let go of it first
                            TASK.CLEAR_PED_TASKS(h, true, false);
                            l.ApproachGoAt = Game.GameTime + 1800;
                            l.ApproachUntil += 1800;
                        }
                        else
                            TASK.TASK_GO_TO_ENTITY(h, t, 20000, Natives.Brisk, 1.3f, 0f, 0);
                        return 300;
                    }
                    TASK.TASK_GO_TO_ENTITY(h, t, -1, Natives.Brisk, 1.3f, 0f, 0);
                    return 2600;
                case "meet":
                    // walks over to the other, who waits; the next line waits until they're close (Tick). Someone sitting or
                    // leaning (a scenario) is let go of it first and sets off once he's up: a walk order alone never
                    // moves him (seen 2026-10-25)
                    l.MeetWalker = who == 0 ? 0 : 1;
                    TASK.TASK_LOOK_AT_ENTITY(t, h, 20000, 0, 51, 0);
                    if (PED.IS_PED_USING_ANY_SCENARIO(h))
                    {
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        l.MeetGoAt = Game.GameTime + 1800;
                        l.MeetUntil = Game.GameTime + 16800;
                    }
                    else
                    {
                        TASK.TASK_GO_TO_ENTITY(h, t, 20000, Natives.Brisk, 1.3f, 0f, 0);
                        l.MeetUntil = Game.GameTime + 15000;
                    }
                    return 300;
                case "shove":
                    Ped victim = who == 2 ? l.Cast[0] : them;
                    PED.SET_PED_TO_RAGDOLL(victim.Handle, 900, 1600, 0, false, false, "");
                    l.Shover = h;
                    l.Shoved = victim.Handle;
                    l.ShoveUntil = Game.GameTime + 4000;   // a man who shoves another: fair game a moment (the review: the bully turning on the one who stepped in)
                    Speech.SayAny(victim.Handle, Speech.Scared, h);
                    return 1800;
                case "hurt":
                    // his face has just gone into the bar: he reels as he cries out (the game layer picks the clip)
                    if (React != null)
                        React(new Reaction { By = me, To = them, Tone = Reactions.HurtTone, Temper = Temper(l, who), Woman = l.Women[who], ByName = l.Names[who],
                                             ToName = l.Names[other], Seed = l.Seed + l.Index });
                    return 0;
                case "slam":
                    // the rough move into a table, a bar or a wall; with nothing it can be played on, a hard shove
                    if (Slam != null && Slam(me, them))
                    {
                        l.Slamming = true;   // reported once it's over (SlamResult)
                        return 300;
                    }
                    PED.SET_PED_TO_RAGDOLL(t, 1500, 2500, 0, false, false, "");
                    Speech.SayAny(h, Speech.Angry, t);
                    Report(l, "shove");
                    return 2200;
                case "challenge":
                    // the drunk lurches up off the bar if he was slumped on it, turns on the other and squares up,
                    // swaying (or, without the move, just looks him up and down)
                    if (PED.IS_PED_USING_ANY_SCENARIO(h))
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, false, true);
                    Vector3 from = me.Position, to = them.Position;
                    ENTITY.SET_ENTITY_HEADING(h, (float)(Math.Atan2(-(to.X - from.X), to.Y - from.Y) * 180.0 / Math.PI));
                    TASK.TASK_LOOK_AT_ENTITY(h, t, 20000, 0, 51, 0);
                    if (STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict))
                    {
                        TASK.TASK_PLAY_ANIM(h, DrunkDict, "main_challenge_drunk", 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                        return Math.Max(1500, (int)(ENTITY.GET_ANIM_DURATION(DrunkDict, "main_challenge_drunk") * 1000) - 500);
                    }
                    return 1200;
                case "passout":
                    // down he goes, and stays down (the last frame held) until he comes to
                    l.Downed = true;
                    Report(l, "drunk");
                    if (STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict))
                    {
                        TASK.TASK_PLAY_ANIM(h, DrunkDict, "pass_out_fwd_v1_drunk", 4f, -4f, -1, 2, 0f, false, 0, false, "", false);
                        return Math.Max(1500, (int)(ENTITY.GET_ANIM_DURATION(DrunkDict, "pass_out_fwd_v1_drunk") * 1000));
                    }
                    PED.SET_PED_TO_RAGDOLL(h, 6000, 9000, 0, false, false, "");
                    return 2500;
                case "cometo":
                    if (STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict) && ENTITY.IS_ENTITY_PLAYING_ANIM(h, DrunkDict, "pass_out_fwd_v1_drunk", 3))
                    {
                        TASK.TASK_PLAY_ANIM(h, DrunkDict, "pass_out_fwd_v1_exit_drunk", 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                        return Math.Max(1500, (int)(ENTITY.GET_ANIM_DURATION(DrunkDict, "pass_out_fwd_v1_exit_drunk") * 1000));
                    }
                    return 600;
                case "fight":
                    l.WasFighting = true;
                    // up off the bar or a seat Rockstar's way: the exit flag and a stand-still the same tick, never a bare clear
                    // (it froze people in the pose, 2026-11-01; Game/ScenarioExit.cs); the fight is ordered once they're up
                    ScenarioExit.SoftUp(h, x => TASK.TASK_STAND_STILL(x, 2000));
                    ScenarioExit.SoftUp(t, x => TASK.TASK_STAND_STILL(x, 2000));
                    uint fists = Natives.Hash("WEAPON_UNARMED");
                    foreach (Ped p in l.Cast.Take(2))
                        WEAPON.SET_CURRENT_PED_WEAPON(p.Handle, fists, true, 0, false, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t, false);
                    l.StartHp.Clear();
                    l.StartHp[l.Cast[0].Handle] = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(l.Cast[0].Handle));
                    l.StartHp[l.Cast[1].Handle] = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(l.Cast[1].Handle));
                    l.StartHp[h] = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(h));
                    l.StartHp[t] = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(t));
                    l.FightId = ++_fightSeq;
                    l.WasBig = false;
                    l.SideGone = false;
                    l.WaitSide = -1;
                    l.FightOnAt = 0;
                    l.Out.Clear();
                    l.Fighting = true;
                    // the fight's own seconds start when they're at it (Tick): two men leaning or sat, ordered to fight
                    // the moment they were told to stop, got up and never swung, and the beat's 9 s ran out on the
                    // words alone (Lex, 2026-10-28)
                    l.FightOn = false;
                    l.FightA = h;
                    l.FightB = t;
                    l.FightAskedAt = Game.GameTime;
                    l.FightOrderedAt = 0;
                    l.FightMs = Math.Max(5000, ms);
                    l.FightUntil = Game.GameTime + FightStartMs;
                    // made to fight with their fists, as enRAGE makes its people (Lex, 2026-10-28: ordered to fight, two
                    // townsmen stood off in "combat" and never swung): fight unarmed (5), always fight (46), never flee
                    // the fight (58); their own put back when the scene ends
                    l.FightAttrs = new Dictionary<int, bool[]>();
                    foreach (int p in new[] { h, t })
                    {
                        l.FightAttrs[p] = FightAttrIds.Select(id => PED._GET_PED_COMBAT_ATTRIBUTE(p, id)).ToArray();
                        foreach (int id in FightAttrIds)
                            if (FightAttrMine(id))
                                PED.SET_PED_COMBAT_ATTRIBUTES(p, id, true);
                    }
                    FightOrder(l);
                    return 1000;
                case "endfight":
                    Ped a = l.Cast[0], b = l.Cast[1];
                    TASK.CLEAR_PED_TASKS(a.Handle, true, false);
                    TASK.CLEAR_PED_TASKS(b.Handle, true, false);
                    float ha = Health(l, 0), hb = Health(l, 1);
                    if (hb > ha)
                    {
                        // the script says A wins; the fists said otherwise
                        l.Cast[0] = b;
                        l.Cast[1] = a;   // the start health is kept by handle, so it follows them
                        string n = l.Names[0]; l.Names[0] = l.Names[1]; l.Names[1] = n;
                        SwapNamesInLines(l, l.Names[0], l.Names[1]);
                        string k = l.Kinds[0]; l.Kinds[0] = l.Kinds[1]; l.Kinds[1] = k;
                        bool i = l.Introduced[0]; l.Introduced[0] = l.Introduced[1]; l.Introduced[1] = i;
                    }
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Cast[0].Handle, true);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Cast[1].Handle, true);
                    return 1500;
                case "walkaway":
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    if (l.Scene.Kind == "lift" && who == 0)
                        Report(l, "lift");   // the thief walks off with it
                    return 600;
                case "leavewith":
                    {
                        // two leave together (the stand-up's take-away, night 2026-10-29): both walk to one spot 25 m past the third,
                        // away from the one the beat names, the other a metre to the side; the walk order is the one the encounters'
                        // and the campfire's walkers use (seen working), so they go side by side instead of wandering apart
                        Ped lead = l.Cast.Length > 2 && l.Cast[2] != null && l.Cast[2].Exists() ? l.Cast[2] : me;
                        Vector3 dir = lead.Position.DistanceTo2D(them.Position) >= 0.3f ? lead.Position - them.Position : me.Position - them.Position;
                        dir.Z = 0f;
                        dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                        Vector3 spot = lead.Position + dir * 25f;
                        if (me != lead)
                            spot += new Vector3(-dir.Y, dir.X, 0f) * 1.2f;
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, spot.X, spot.Y, spot.Z, 1f, -1, 1f, 0, 0f);
                        if (_leaving.Count > 50)
                            _leaving.Clear();   // only ever two at a time; a guard against a session that never checks
                        _leaving[h] = Game.GameTime + LeaveHoldMs;
                        return 600;
                    }
                case "outside":
                    {
                        // the drunk taking it outside (SIXDAY item 17): both walk to the saloon's door the game side remembers, side by side,
                        // and the fight beat after finds them in the street; no door known: they square up where they stand
                        Vector3? door = OutsideSpot != null ? OutsideSpot() : null;
                        if (door == null)
                            return 600;
                        Vector3 spot = door.Value, dir = spot - me.Position;
                        dir.Z = 0f;
                        dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                        Vector3 side = new Vector3(-dir.Y, dir.X, 0f) * 0.9f;
                        foreach (Ped p in new[] { me, them })
                        {
                            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                            Vector3 goTo = p == me ? spot + side : spot - side;
                            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, goTo.X, goTo.Y, goTo.Z, 1f, -1, 1f, 0, 0f);
                            _leaving[p.Handle] = Game.GameTime + LeaveHoldMs;
                        }
                        return 600;
                    }
                case "carry":
                    // the game's own carry, over the shoulder (its pickup of a carriable body): he goes limp first, as the knocked-out
                    // do, since the game carries only the limp (Lex, 2026-10-30: "the in game pickup person animation"); Tick waits
                    // 5 s to see it took, else he's brought round and the two walk home
                    TASK.CLEAR_PED_TASKS(t, true, false);
                    PED.SET_PED_TO_RAGDOLL(t, 15000, 20000, 0, false, false, "");
                    TASK.TASK_PICKUP_CARRIABLE_ENTITY(h, t);
                    l.CarryBy = h;
                    l.CarryWho = t;
                    l.CarryUntil = Game.GameTime + 5000;
                    l.Downed = false;
                    return 600;
                case "homewith":
                    {
                        // two go home together (the drunk carried home): side by side to a spot 30 m off, away from the player
                        Vector3 mid = (me.Position + them.Position) * 0.5f;
                        Vector3 dir = _player != 0 ? mid - new Ped(_player).Position : me.Position - them.Position;
                        dir.Z = 0f;
                        dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                        Vector3 spot = mid + dir * 30f, side = new Vector3(-dir.Y, dir.X, 0f) * 0.9f;
                        foreach (Ped p in new[] { me, them })
                        {
                            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                            Vector3 goTo = p == me ? spot + side : spot - side;
                            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, goTo.X, goTo.Y, goTo.Z, 1f, -1, 1f, 0, 0f);
                            _leaving[p.Handle] = Game.GameTime + LeaveHoldMs;
                        }
                        l.Downed = false;   // he's up and going: the end doesn't get him up again
                        return 600;
                    }
                case "flee":
                    FleeOrdered?.Invoke(h);
                    if (Bravery.Holds(BraveryNow, l.Seed + "|flee|" + who))
                        TASK.TASK_SMART_FLEE_PED(h, t, 10f, 6000, 0, 1f, 0);   // brave ([World] Bravery): backs off a few steps, at a walk
                    else
                        TASK.TASK_SMART_FLEE_PED(h, t, 40f, -1, 0, 2f, 0);
                    return 600;
                case "kneel":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Crouch, 9000, true, 0u, -1f, false);
                    return 1800;
                case "fall":
                    // a trip: down on the ground for a moment (the old sit down hard instead: their scene leaves this out);
                    // short, so they're done tumbling when they sit up
                    PED.SET_PED_TO_RAGDOLL(h, 900, 1300, 0, false, false, "");
                    return 2300;
                case "faceone":
                    // only this one turns to the one the beat names (the rest carry on: one walking off stays walking)
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, t, 6000, 0, 51, 0);
                    return 900;
                case "sitdown":
                    // stays down, sitting where they fell, until they get up or someone helps them
                    if (!PED.IS_PED_RAGDOLL(h))
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_SIT_GROUND"), 30000, true, 0u, -1f, false);
                    l.Sat = who;
                    return 1200;
                case "getup":
                    if (l.Sat == who)
                        l.Sat = -1;
                    ScenarioExit.SoftUp(h, x => TASK.TASK_STAND_STILL(x, 2200));   // up off the ground Rockstar's way: the exit flag and the next task, no bare clear
                    if (l.Scene.Kind == "help" || l.Scene.Kind == "passby")
                        Report(l, l.Scene.Kind);   // helped up, or left to get up alone: it happened
                    return 2200;
                case "stand":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 3000, 0f, 0f, 0f);
                    return 1200;
                default:
                    if (act.StartsWith("cb:"))
                    {
                        try
                        {
                            Custom?.Invoke(act.Substring(3));
                            CustomScene?.Invoke(act.Substring(3), l.Cast, l.Info);
                        }
                        catch (Exception ex)
                        {
                            Log.Write("SCENE action " + act + " failed: " + ex.Message);
                        }
                    }
                    return 0;
                case "mourn":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, GraveMourning, 20000, true, 0u, -1f, false);
                    return 1200;
                case "shoot":
                    {
                        // the shot (the ladder's top, LadderShots): his own sidearm, at the other; the other fires back if he carries
                        // one, or runs. The gunplay runs until one is down, or 15 s (Tick); then the shooter runs
                        uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                        if (gun == 0 || gun == Natives.Hash("WEAPON_UNARMED"))
                            return 600;
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t, false);
                        WEAPON.SET_CURRENT_PED_WEAPON(h, gun, true, 0, false, false);
                        PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, false);   // not "fight unarmed": the gun
                        TASK.TASK_COMBAT_PED(h, t, 0, 16);
                        uint theirs = WEAPON.GET_BEST_PED_WEAPON(t, false, false);
                        bool armed = theirs != 0 && theirs != Natives.Hash("WEAPON_UNARMED");
                        FleeOrdered?.Invoke(t);   // he may run: the run is a run ([World] Bravery)
                        if (armed)
                        {
                            WEAPON.SET_CURRENT_PED_WEAPON(t, theirs, true, 0, false, false);
                            PED.SET_PED_COMBAT_ATTRIBUTES(t, 5, false);
                            TASK.TASK_COMBAT_PED(t, h, 0, 16);
                        }
                        else if (Bravery.Holds(BraveryNow, l.Seed + "|drawn"))
                            TASK.TASK_HANDS_UP(t, 8000, h, -1, false);   // brave ([World] Bravery): unarmed, he stands his ground with his hands up
                        else
                            TASK.TASK_SMART_FLEE_PED(t, h, 80f, -1, 0, 3f, 0);
                        l.Shooter = h;
                        l.ShotAt = t;
                        l.ShootUntil = Game.GameTime + 15000;
                        for (int i = 0; i < l.Aimed.Length; i++)
                            l.Aimed[i] = false;
                        Log.Write("SCENE shots: " + l.Names[who] + " fires on " + l.Names[other] + (armed ? ", who fires back" : ", who runs"));
                        return 1000;
                    }
                case "aim":
                    // a standoff: guns come out only when the cast is armed and no lawman is close (the game layer checks)
                    if ((who == 0 && l.Info.Armed0) || (who == 1 && l.Info.Armed1))
                    {
                        // after a fight the fists are still the weapon in hand (the fight act set them, KeepFists held
                        // them): the sidearm back in the hand first, else the aim would point nothing (the ladder's
                        // review; the fixed standoff never aims after a fight)
                        uint best = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                        if (best != 0 && best != Natives.Hash("WEAPON_UNARMED"))
                            WEAPON.SET_CURRENT_PED_WEAPON(h, best, true, 0, false, false);
                        TASK.TASK_AIM_GUN_AT_ENTITY(h, t, 25000, false, 0);
                        l.Aimed[who] = true;
                        try { Shock?.Invoke("aim", h, l.Scene.Kind, l.Seed); } catch (Exception ex) { Log.Write("STREET: the gun out wasn't raised: " + ex.Message); }   // the street reacts (SIXDAY 5b)
                        return 900;
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, t, 20000, 0, 51, 0);
                    try { Shock?.Invoke("square", h, l.Scene.Kind, l.Seed); } catch (Exception ex) { Log.Write("STREET: the squaring up wasn't raised: " + ex.Message); }
                    return 700;
                case "holster":
                    if (who < l.Aimed.Length)
                        l.Aimed[who] = false;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 2000, 0f, 0f, 0f);
                    return 700;
                case "drunk":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_DRINKING_DRUNK"), 12000, true, 0u, -1f, false);
                    return 900;
                case "flask":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_DRINK_FLASK"), 8000, true, 0u, -1f, false);
                    return 900;
                case "smoke":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, SmokeHash, 15000, true, 0u, -1f, false);
                    return 900;
                case "whittle":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_SIT_GROUND_WHITTLE"), 20000, true, 0u, -1f, false);
                    return 1600;
                case "stoic":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_STARE_STOIC"), 10000, true, 0u, -1f, false);
                    return 900;
                case "run":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    FleeOrdered?.Invoke(h);
                    TASK.TASK_SMART_FLEE_PED(h, _player != 0 ? _player : t, 180f, -1, 0, 3f, 0);
                    return 300;
                case "faceplayer":
                    if (_player != 0 && !(l.Downed && who == 0))   // a drunk lying where he fell only looks
                    {
                        if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, _player, 3000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, _player, 6000, 0, 51, 0);
                    }
                    return 600;
                case "lookplayer":
                    if (_player != 0)
                        TASK.TASK_LOOK_AT_ENTITY(h, _player, 6000, 0, 51, 0);
                    return 500;
            }
        }

        /// The scene's defining moment, once (Happened).
        private void Report(Live l, string what)
        {
            if (l.HappenedSent)
                return;
            l.HappenedSent = true;
            try
            {
                Happened?.Invoke(what);
            }
            catch (Exception ex)
            {
                Log.Write("SCENE report " + what + " failed: " + ex.Message);
            }
        }

        /// Health now, as a share of what it was when the fight started.
        private static float Health(Live l, int i)
        {
            return HealthOf(l, l.Cast[i].Handle);
        }

        /// A fighter's health now, as a share of what it was when he came into the fight (by handle; no allocation).
        private static float HealthOf(Live l, int h)
        {
            int start;
            if (!l.StartHp.TryGetValue(h, out start))
                start = Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(h, false));
            return ENTITY.GET_ENTITY_HEALTH(h) / (float)Math.Max(1, start);
        }

        /// The third's walk over: there within 2.4 m (as "meet"), and given at most 12 s (4 s and 0.8 s a metre).
        private const float ApproachNear = 2.4f;
        private const int ApproachMaxMs = 12000;

        /// A brawl stays a brawl: if a fighter reaches for a gun or a knife, it goes back in the holster.
        private const int FightStartMs = 16000;   // 12 s until 2026-10-30: a man getting up off the bar is given 8 s before he's pulled off it
        // 5 and 46 together make them fight with fists (femga and Halen84 name them CA_ALWAYS_FIGHT and
        // CA_CAN_FIGHT_ARMED_PEDS_WHEN_NOT_ARMED), 58 CA_DISABLE_FLEE_FROM_COMBAT; since 2026-10-31 ([World] Bravery's half for
        // fights) 77 CA_DISABLE_RESPONDED_TO_THREAT_BROADCAST, as the game's own street fight sets on its fighters
        // (beat_street_fight.c), so the fight doesn't send the crowd running, and 78 CA_DISABLE_ALL_RANDOMS_FLEE (named in both
        // lists, no script seen setting it); every one of them put back when the scene ends
        private static readonly int[] FightAttrIds = { 5, 46, 58, 77, 78 };

        /// Whether the fight sets this one of FightAttrIds: 77 and 78 are Bravery's half, so at [World] Bravery 0 a fight
        /// leaves them as the game has them (the review, 2026-10-31); all five are read and put back either way.
        private bool FightAttrMine(int id)
        {
            return (id != 77 && id != 78) || BraveryNow > 0;
        }

        /// The order to fight, once both are on their feet: a man sat or leaning is told to stop (the clear lets him get
        /// up) and, still in it after 3 s, taken out of it at once. Given again every 2 s until they're at it.
        /// The fight going on now, as two sides: who started it on each, and who has joined each (Core/JoinIn.cs); false when
        /// no fists are flying.
        public bool FightSides(out List<int> a, out List<int> b)
        {
            a = b = null;
            Live l = _live;
            if (l == null || !l.Fighting || !l.FightOn || l.FightA == 0 || l.FightB == 0 || l.SideGone)
                return false;
            a = new List<int> { l.FightA };
            b = new List<int> { l.FightB };
            foreach (int[] j in l.Joiners)
                (j[1] == 0 ? a : b).Add(j[0]);
            return true;
        }

        public int JoinerCount { get { return _live != null ? _live.Joiners.Count : 0; } }

        /// Who is laying into someone in the scene now besides its starter (FightStarter), and at whom: those who joined the
        /// fight while fists fly, and a man firing on another while the gunplay lasts (Lex, 2026-10-30: the joiners fair game
        /// as the bully is). Each { attacker, target }.
        public List<int[]> Attackers()
        {
            var all = new List<int[]>();
            Live l = _live;
            if (l == null)
                return all;
            if (l.Fighting && l.FightOn)
                foreach (int[] j in l.Joiners)
                    if (ENTITY.DOES_ENTITY_EXIST(j[0]) && !ENTITY.IS_ENTITY_DEAD(j[0]))
                        all.Add(new[] { j[0], j.Length > 2 ? j[2] : 0 });
            if (l.Scene.Kind == "mugging" && l.Cast.Length > 1 && l.Cast[0] != null && l.Cast[0].Exists() && !l.Cast[0].IsDead)
                all.Add(new[] { l.Cast[0].Handle, l.Cast[1] != null ? l.Cast[1].Handle : 0 });   // a man robbing another: stopping him is no crime
            if (l.ShootUntil > 0 && l.Shooter != 0 && ENTITY.DOES_ENTITY_EXIST(l.Shooter) && !ENTITY.IS_ENTITY_DEAD(l.Shooter))
                all.Add(new[] { l.Shooter, l.ShotAt });
            if (l.ShoveUntil > Game.GameTime && l.Shover != 0 && ENTITY.DOES_ENTITY_EXIST(l.Shover) && !ENTITY.IS_ENTITY_DEAD(l.Shover))
                all.Add(new[] { l.Shover, l.Shoved });
            return all;
        }

        /// Someone joins the fight on a side and goes for target, with their fists, as the first two fight; the fight runs
        /// a little longer for it. Their own ways are put back when the fight ends.
        public bool AddFighter(int h, int side, int target)
        {
            Live l = _live;
            if (l == null || !l.Fighting || !l.FightOn || l.SideGone || h == 0 || target == 0 || Involves(h) || l.Out.Contains(h) || !ENTITY.DOES_ENTITY_EXIST(h) || !ENTITY.DOES_ENTITY_EXIST(target))
                return false;
            if (l.Out.Contains(target) || ENTITY.IS_ENTITY_DEAD(target))
            {
                target = LeastAttacked(l, 1 - side, PED.IS_PED_MALE(h));   // the man he got up for is down already: the least beset over there (a man before a woman for a man)
                if (target == 0)
                    return false;
            }
            bool leads = l.WaitSide == side && (side == 0 ? l.FightA : l.FightB) != 0 && l.Out.Contains(side == 0 ? l.FightA : l.FightB);
            if (leads)
            {
                // his side's last man went down as he came: he leads it now, against the other side's leader
                if (side == 0)
                    l.FightA = h;
                else
                    l.FightB = h;
                l.WaitSide = -1;
                target = side == 0 ? l.FightB : l.FightA;
                Log.Write("SCENE fight: one of the fallen side's own comes and leads it now");
            }
            else
                l.Joiners.Add(new[] { h, side, target });
            l.StartHp[h] = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(h));
            if (l.Joiners.Count >= 2)
                l.WasBig = true;
            if (l.FightAttrs == null)
                l.FightAttrs = new Dictionary<int, bool[]>();
            if (!l.FightAttrs.ContainsKey(h))
                l.FightAttrs[h] = FightAttrIds.Select(id => PED._GET_PED_COMBAT_ATTRIBUTE(h, id)).ToArray();
            foreach (int id in FightAttrIds)
                if (FightAttrMine(id))
                    PED.SET_PED_COMBAT_ATTRIBUTES(h, id, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
            TASK.TASK_COMBAT_PED(h, target, 0, 16);
            if (leads)
            {
                if (ENTITY.DOES_ENTITY_EXIST(target) && !ENTITY.IS_ENTITY_DEAD(target))
                    TASK.TASK_COMBAT_PED(target, h, 0, 16);   // the other side's leader turns on him
                foreach (int[] j in l.Joiners)
                    if (j[1] != side && j.Length > 2 && l.Out.Contains(j[2]) && ENTITY.DOES_ENTITY_EXIST(j[0]) && !ENTITY.IS_ENTITY_DEAD(j[0]))
                    {
                        j[2] = h;   // those left with a downed man for a target go for the one who came
                        TASK.TASK_COMBAT_PED(j[0], h, 0, 16);
                    }
            }
            l.FightUntil = Math.Min(Game.GameTime + FightMostMs, Math.Max(l.FightUntil, Game.GameTime + 6000));   // a fight with more in it lasts a little longer
            return true;
        }

        /// How long a fight may be kept going by those joining it, from the last who joined (20 s; [Mind] MindBrawls makes it
        /// longer, so a brawl can grow a wave at a time).
        public int FightMostMs = 20000;

        /// [Mind] BrawlLeastSeconds, set by the game layer every look: a brawl (WasBig) lasts at least this long from its
        /// first blows, unless a side is all down.
        public int BrawlLeastMs = 45000;

        /// How long a side with nobody left standing waits for one of its own on the way.
        private const int WaitMs = 10000;

        private readonly int[] _coming = new int[2];

        /// Those on their way to join, by side (the game layer, every half second): they count for their side, so the fight
        /// doesn't end while they come.
        public void SetComing(int sideA, int sideB)
        {
            _coming[0] = Math.Max(0, sideA);
            _coming[1] = Math.Max(0, sideB);
        }

        private int ComingFor(int side)
        {
            return side == 0 || side == 1 ? _coming[side] : 0;
        }

        /// The fight just over, as it stood: { handle, side, target } for each still in it, the leaders first, and when.
        public readonly List<int[]> EndedFighters = new List<int[]>();
        public int EndedAt, EndedFightId;

        private void SnapEnd(Live l, int now)
        {
            EndedFighters.Clear();
            EndedAt = now;
            EndedFightId = l.FightId;
            if (!(l.WasBig || l.Joiners.Count > 0 || ComingFor(0) + ComingFor(1) > 0))
                return;   // a plain two-man fistfight nobody joined: never a brawl, so no fate after it either (FightDownStep spares the two in it too; the review, 2026-10-31)
            if (l.FightA != 0 && !l.Out.Contains(l.FightA))
                EndedFighters.Add(new[] { l.FightA, 0, l.FightB });
            if (l.FightB != 0 && !l.Out.Contains(l.FightB))
                EndedFighters.Add(new[] { l.FightB, 1, l.FightA });
            foreach (int[] j in l.Joiners)
                if (!l.Out.Contains(j[0]))
                    EndedFighters.Add(new[] { j[0], j[1], j.Length > 2 ? j[2] : 0 });
        }

        /// One of the fight just over goes down after it (the game's own melee carried on): the sides as they stood at the
        /// end, him among them, put where FighterDown reads them (DownSideA/B), and he is told down once.
        public void DownAfter(int h, int by, bool dead)
        {
            DownSideA.Clear();
            DownSideB.Clear();
            int on = 0;
            foreach (int[] f in EndedFighters)
            {
                (f[1] == 0 ? DownSideA : DownSideB).Add(f[0]);
                if (f[0] != h && f[2] == h)
                    on++;
            }
            for (int i = EndedFighters.Count - 1; i >= 0; i--)
                if (EndedFighters[i][0] == h)
                    EndedFighters.RemoveAt(i);
            // one still in a scene that's playing its after-fight words: out of it, and the scene ends (its words would be a
            // downed man's; the review, 2026-10-31: the beats went on tasking a man the fate had just put down)
            foreach (Live l in _live != null ? new[] { _live }.Concat(_side).ToArray() : _side.ToArray())
                if (!l.Ended && l.Cast.Any(p => p != null && p.Handle == h))
                {
                    l.Out.Add(h);
                    Stop(l, "a man down after the fight");
                }
            try
            {
                FighterDown?.Invoke(h, by, Math.Max(1, on), dead);
            }
            catch (Exception ex)
            {
                Log.Write("SCENE fight: a man down after it, the fate failed: " + ex.Message);
            }
        }

        /// Everyone swinging in the fight now: the two who began it and those who joined (0 when no fists fly).
        public int FightingCount
        {
            get
            {
                Live l = _live;
                return l != null && l.Fighting && l.FightOn ? 2 + l.Joiners.Count : 0;
            }
        }

        /// Whom this fighter is going for: the other of the two for those who began it, a joiner's own target; 0 for none.
        public int TargetOf(int h)
        {
            Live l = _live;
            return l != null && l.Fighting ? TargetOf(l, h) : 0;
        }

        private static int TargetOf(Live l, int h)
        {
            if (h == l.FightA)
                return l.FightB;
            if (h == l.FightB)
                return l.FightA;
            foreach (int[] j in l.Joiners)
                if (j[0] == h)
                    return j.Length > 2 ? j[2] : 0;
            return 0;
        }

        /// A joiner turns on someone else (the player, in a brawl that comes at him): his target in the list follows, so
        /// Attackers says whom he's at.
        public bool Retarget(int h, int target)
        {
            Live l = _live;
            if (l == null || !l.Fighting || !l.FightOn || target == 0 || !ENTITY.DOES_ENTITY_EXIST(h))
                return false;
            foreach (int[] j in l.Joiners)
                if (j[0] == h && j.Length > 2)
                {
                    j[2] = target;
                    TASK.TASK_COMBAT_PED(h, target, 0, 16);
                    return true;
                }
            return false;
        }

        /// The fight is over: those who joined stop, and their own combat ways come back (End puts back the attributes).
        /// A joiner who came to lead a side once its first man went down is let go the same way: he is in neither the
        /// joiners nor the first two of the cast (the review, 2026-10-31: he fought on after the scene, off our hold).
        private static void LetJoinersGo(Live l)
        {
            foreach (int[] j in l.Joiners)
                LetFighterGo(l, j[0]);
            for (int k = 0; k < 2; k++)
            {
                int h = k == 0 ? l.FightA : l.FightB;
                if (h == 0 || l.Out.Contains(h))
                    continue;   // down, or gone for his gun: the game layer has him
                bool first = false;
                for (int i = 0; i < l.Cast.Length && i < 2; i++)
                    first |= l.Cast[i] != null && l.Cast[i].Handle == h;
                if (!first)
                    LetFighterGo(l, h);
            }
        }

        private static void LetFighterGo(Live l, int h)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                return;
            if (PED.IS_PED_IN_COMBAT(h, 0))
                TASK.CLEAR_PED_TASKS(h, true, false);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, !l.Ended);   // held until the scene ends, as endfight holds the two; let go then
        }

        private static void FightOrder(Live l)
        {
            int now = Game.GameTime;
            bool up = true;
            foreach (int p in new[] { l.FightA, l.FightB })
            {
                if (!PED.IS_PED_USING_ANY_SCENARIO(p))
                    continue;
                // still leaning or sat: asked again Rockstar's guarded way (never mid-exit); pulled out only past 8 s, Rockstar's
                // forced way (the immediate exit flag, the clear, detached), with the fight given the same tick: no idle pose
                // between (Lex, 2026-10-30: a man pulled off the bar went into a T pose; the flag and the detach were missing)
                if (now - l.FightAskedAt > 8000)
                {
                    int foe = p == l.FightA ? l.FightB : l.FightA;
                    string how = ScenarioExit.Pull(p, x => TASK.TASK_COMBAT_PED(x, foe, 0, 16));   // the immediate clear only off camera (Game/ScenarioExit.cs)
                    Log.Write("SCENE fight: " + (p == l.FightA ? l.Names[0] : l.Names[1]) + " was still sat or leaning " + (now - l.FightAskedAt) / 1000 + " s after the word: pulled out of it (" + how + ")");
                    continue;
                }
                up = false;
                if (now - l.FightAskedAt > 4000)
                    ScenarioExit.Reask(p, x => TASK.TASK_STAND_STILL(x, 4000));
            }
            if (!up)
            {
                l.FightOrderedAt = now;   // looked at again in 2 s (Tick), not every frame: each ask starts the getting up over
                return;
            }
            TASK.TASK_COMBAT_PED(l.FightA, l.FightB, 0, 16);
            TASK.TASK_COMBAT_PED(l.FightB, l.FightA, 0, 16);
            l.FightOrderedAt = now;
        }

        private static void KeepFists(Ped p)
        {
            if (p != null)
                KeepFists(p.Handle);
        }

        private static readonly uint Fists = Natives.Hash("WEAPON_UNARMED");

        private static unsafe void KeepFists(int h)
        {
            if (h == 0 || !ENTITY.DOES_ENTITY_EXIST(h))
                return;
            ulong current = 0;
            if (WEAPON.GET_CURRENT_PED_WEAPON(h, (uint*)&current, true, 0, false) && (uint)current != Fists)
                WEAPON.SET_CURRENT_PED_WEAPON(h, Fists, true, 0, false, false);
        }

        // ---- a man down in a brawl (Lex, 2026-10-31: "a death does not end a big brawl"; fists fatal or a knockout) ----

        private static int _fightSeq;

        /// [World] Bravery, set by the game layer every look: the scene's own frights (a man backing off, the unarmed man a
        /// gun is drawn on) hold their ground this percent of the time (0: they run, as before).
        public int BraveryNow;

        /// With [Mind] MindBrawls (set by the game layer every look): a fighter beaten down is dropped from the sides and the
        /// fight goes on; FighterDown is told so it can roll his fate.
        public bool DownsOn;

        /// A fighter is down: (who, the one on him (0 none), how many were on him, already dead or incapacitated when seen).
        /// Called once per man per fight, from the frame tick.
        public Action<int, int, int, bool> FighterDown;

        /// Someone in a scene is about to be sent running (the game layer gives him back his own flight first: [World] Bravery).
        public Action<int> FleeOrdered;

        /// The two sides as they stood the moment the last man went down, him included (read in FighterDown).
        public readonly List<int> DownSideA = new List<int>(), DownSideB = new List<int>();

        /// Under this share of his start health a fighter is down: the fate is rolled before the game's own blow kills him.
        private const float DownShare = 0.45f;   // 0.30 until 2026-10-31: one blow took a man from over it to dead before the check saw him

        /// The fight going on now's own number (0: none).
        public int FightId { get { Live l = _live; return l != null && l.Fighting ? l.FightId : 0; } }

        /// Down or gone from the fight going on now.
        public bool IsOut(int h)
        {
            Live l = _live;
            return l != null && l.Out.Count > 0 && l.Out.Contains(h);
        }

        /// How many in the fight are going for this man now (the two leaders count for each other).
        public int AttackersOn(int h)
        {
            Live l = _live;
            return l != null && l.Fighting ? AttackersOn(l, h) : 0;
        }

        private static int AttackersOn(Live l, int h)
        {
            int n = 0;
            if (h == 0)
                return 0;
            if (h == l.FightB && l.FightA != 0 && !l.Out.Contains(l.FightA))
                n++;
            if (h == l.FightA && l.FightB != 0 && !l.Out.Contains(l.FightB))
                n++;
            for (int i = 0; i < l.Joiners.Count; i++)
                if (l.Joiners[i].Length > 2 && l.Joiners[i][2] == h)
                    n++;
            return n;
        }

        /// One of those going for this man (the leader of the other side first), or 0.
        private static int AttackerOf(Live l, int h)
        {
            if (h == l.FightB && l.FightA != 0 && !l.Out.Contains(l.FightA))
                return l.FightA;
            if (h == l.FightA && l.FightB != 0 && !l.Out.Contains(l.FightB))
                return l.FightB;
            for (int i = 0; i < l.Joiners.Count; i++)
                if (l.Joiners[i].Length > 2 && l.Joiners[i][2] == h)
                    return l.Joiners[i][0];
            return 0;
        }

        /// Which side a fighter is on (0 with the first, 1 with the second), or -1.
        private static int SideOf(Live l, int h)
        {
            if (h == l.FightA)
                return 0;
            if (h == l.FightB)
                return 1;
            for (int i = 0; i < l.Joiners.Count; i++)
                if (l.Joiners[i][0] == h)
                    return l.Joiners[i][1];
            return -1;
        }

        /// The man on that side with the fewest on him, still up (0: none). For a man's hand (byMan), a man before a woman
        /// whenever a man is still up over there, as JoinIn.Spread has it (the review, 2026-10-31: the turn after a down
        /// sent men onto a woman with no regard for the 1899 rule).
        private static int LeastAttacked(Live l, int side, bool byMan)
        {
            int best = 0, bestOn = int.MaxValue, bestMan = 0, bestManOn = int.MaxValue;
            for (int i = -1; i < l.Joiners.Count; i++)
            {
                int h = i < 0 ? (side == 0 ? l.FightA : l.FightB) : l.Joiners[i][1] == side ? l.Joiners[i][0] : 0;
                if (h == 0 || l.Out.Contains(h) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                int on = AttackersOn(l, h);
                if (on < bestOn)
                {
                    best = h;
                    bestOn = on;
                }
                if (byMan && on < bestManOn && PED.IS_PED_MALE(h))
                {
                    bestMan = h;
                    bestManOn = on;
                }
            }
            return bestMan != 0 ? bestMan : best;
        }

        /// Every frame of a fight: the first man found beaten down (or killed) is dropped. Loops over handles only.
        private void FightDownStep(Live l)
        {
            for (int k = -2; k < l.Joiners.Count; k++)
            {
                int h = k == -2 ? l.FightA : k == -1 ? l.FightB : l.Joiners[k][0];
                if (h == 0 || (l.Out.Count > 0 && l.Out.Contains(h)) || !ENTITY.DOES_ENTITY_EXIST(h))
                    continue;
                if (k < 0 && !l.WasBig && l.Joiners.Count == 0 && ComingFor(0) + ComingFor(1) == 0)
                    continue;   // the first two alone, nobody joined or coming: the 60% rule ends it before this
                // only the dead count as killed by the game's blow; a man the game has incapacitated is a down like any
                // other and his fate is rolled (the review, 2026-10-31: he was killed whatever BrawlFistsFatal said)
                bool dead = ENTITY.IS_ENTITY_DEAD(h);
                if (dead || PED.IS_PED_INCAPACITATED(h) || HealthOf(l, h) < DownShare)
                {
                    FightDown(l, h, dead);
                    return;   // one a frame: the lists have changed
                }
            }
        }

        /// He's down: out of the sides, a joiner of his side leads it if he led it, whoever was on him turns to the least
        /// beset man left over there, and the game layer rolls his fate. A side with nobody left ends the fight.
        private void FightDown(Live l, int h, bool dead)
        {
            int by = AttackerOf(l, h), on = AttackersOn(l, h);
            if (by == 0)
            {
                // nobody listed on him (a joiner beaten by the man he went for, who fights back by the game's own ways): that
                // man, else the least beset of the other side, so the blow and the death are someone's, never his own hand
                int t = TargetOf(l, h), side = SideOf(l, h);
                by = t != 0 && !l.Out.Contains(t) && ENTITY.DOES_ENTITY_EXIST(t) && !ENTITY.IS_ENTITY_DEAD(t) ? t
                   : side >= 0 ? LeastAttacked(l, 1 - side, true) : 0;
            }
            // the sides as they stood when he went down, for his fate's fair fight and friend in it (after the drop a side may be
            // gone, and FightSides says nothing)
            DownSideA.Clear();
            DownSideB.Clear();
            if (l.FightA != 0 && (h == l.FightA || !l.Out.Contains(l.FightA)))
                DownSideA.Add(l.FightA);
            if (l.FightB != 0 && (h == l.FightB || !l.Out.Contains(l.FightB)))
                DownSideB.Add(l.FightB);
            foreach (int[] j in l.Joiners)
                if (j[0] == h || !l.Out.Contains(j[0]))
                    (j[1] == 0 ? DownSideA : DownSideB).Add(j[0]);
            DropFromFight(l, h);
            try
            {
                FighterDown?.Invoke(h, by, on, dead);
            }
            catch (Exception ex)
            {
                Log.Write("SCENE fight: a man down, the fate failed: " + ex.Message);
            }
        }

        /// Out of the fight without a fate (he drew a gun: the game layer has him now). True while the fight goes on.
        public bool LeaveFight(int h)
        {
            Live l = _live;
            if (l == null || !l.Fighting || !l.FightOn || l.Out.Contains(h) || SideOf(l, h) < 0)
                return false;
            DropFromFight(l, h);
            return !l.SideGone;
        }

        private void DropFromFight(Live l, int h)
        {
            int side = SideOf(l, h);
            l.Out.Add(h);
            bool lead = h == l.FightA || h == l.FightB;
            if (lead)
            {
                // a man of his side leads it before a woman, so the other side's leader isn't sent onto her first (1899)
                int idx = -1;
                for (int i = 0; i < l.Joiners.Count; i++)
                {
                    int[] j = l.Joiners[i];
                    if (j[1] == side && !l.Out.Contains(j[0]) && ENTITY.DOES_ENTITY_EXIST(j[0]) && !ENTITY.IS_ENTITY_DEAD(j[0]))
                    {
                        if (idx < 0)
                            idx = i;
                        if (PED.IS_PED_MALE(j[0]))
                        {
                            idx = i;
                            break;
                        }
                    }
                }
                if (idx < 0 && ComingFor(side) > 0 && l.WaitSide < 0)
                {
                    // nobody left standing on his side, but one of theirs is on the way: the side waits for him (10 s at most),
                    // and he leads it when he comes (AddFighter)
                    l.WaitSide = side;
                    l.WaitUntil = Game.GameTime + WaitMs;
                    Log.Write("SCENE fight: a man down, the last of his side standing; " + ComingFor(side) + " of theirs on the way, so it waits for them");
                    return;
                }
                if (idx < 0)
                {
                    l.SideGone = true;   // nobody left on his side: the fight is over (TickOne ends it next frame)
                    l.FightUntil = 0;
                    return;
                }
                int next = l.Joiners[idx][0];
                l.Joiners.RemoveAt(idx);
                if (h == l.FightA)
                    l.FightA = next;
                else
                    l.FightB = next;
                Log.Write("SCENE fight: a man down; another of his side leads it now (" + (2 + l.Joiners.Count) + " still fighting)");
            }
            else
                for (int i = l.Joiners.Count - 1; i >= 0; i--)
                    if (l.Joiners[i][0] == h)
                        l.Joiners.RemoveAt(i);
            // whoever was on him turns to the least beset man left on his side
            if (lead && ENTITY.DOES_ENTITY_EXIST(l.FightA) && ENTITY.DOES_ENTITY_EXIST(l.FightB))
            {
                TASK.TASK_COMBAT_PED(l.FightA, l.FightB, 0, 16);
                TASK.TASK_COMBAT_PED(l.FightB, l.FightA, 0, 16);
            }
            for (int i = 0; i < l.Joiners.Count; i++)
            {
                int[] j = l.Joiners[i];
                if (j.Length < 3 || j[2] != h)
                    continue;
                int t = LeastAttacked(l, 1 - j[1], ENTITY.DOES_ENTITY_EXIST(j[0]) && PED.IS_PED_MALE(j[0]));
                if (t == 0)
                    continue;
                j[2] = t;
                if (ENTITY.DOES_ENTITY_EXIST(j[0]) && !ENTITY.IS_ENTITY_DEAD(j[0]))
                    TASK.TASK_COMBAT_PED(j[0], t, 0, 16);
            }
        }
    }
}
