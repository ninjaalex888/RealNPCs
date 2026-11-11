// 0.4 plumbing for the script: settings (and reloading them while the game runs), where notices go, the
// bottom ticker and the papers reader drawn every frame, and the order the new features run in.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly TickerBrain _ticker = new TickerBrain();
        private readonly List<NearThought> _near = new List<NearThought>();
        private Func<string, bool> _inViewId;

        /// The handle of someone in view near you (RefreshNear's list, once a second), by their id; 0 if not.
        private int NearHandleOf(string id)
        {
            if (id == null)
                return 0;
            for (int i = 0; i < _near.Count; i++)
                if (_near[i].Key == id && _near[i].Handle != 0)
                    return _near[i].Handle;
            return 0;
        }

        /// Game minutes between a new thought for a counter keeper or a resident: 45, about the 90 real seconds the
        /// ticker waits before asking again (it was 600, so the same thought came back for 20 minutes).
        private const long ThoughtEvery = 45;
        private long EveryNow { get { return _ticker.Several > 1 ? 30 : ThoughtEvery; } }   // game minutes (FIVEDAY2 item 24: thirty, a minute of real time, when several can be up)

        /// Closer than this, a person is right beside you: every other ticker turn goes to them (TickerBrain).
        private const float BesideMetres = 3.5f;
        private readonly Dictionary<string, int> _thoughtSoundAt = new Dictionary<string, int>();
        private readonly Reader _reader = new Reader();
        private readonly Director _director = new Director();
        private readonly Queue<Keys> _readerKeys = new Queue<Keys>();
        private KeySpec _readerKey;
        private bool _readerRequested;
        private KeySpec _tickerNextKey;
        private KeySpec _spotKey;   // the spot table's key (WestworldScript.Spots.cs)
        private bool _tickerNextRequested;
        private KeySpec _talkHotkey;   // [World] TalkKey: Talk while its prompt is up (WestworldScript.Menu.cs)
        private string _iniPath;
        private DateTime _iniStamp;
        private int _nextNear, _nextIni, _nextUi, _controlOffAt = -1;
        private bool _uiOpen;
        private string _hint;          // a prompt shown above the ticker ("[N] take the diary")
        private string _hintRowsFor;   // the hint its rows were wrapped for
        private List<string> _hintRows;
        private float[] _hintRowW;
        private float _hintW, _hintRowsScale;
        private int _hintUntil;
        private int _now, _nextTickerUpdate;
        private TickerLine _tickerLine;
        private readonly Dictionary<string, int> _featureErrors = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _featureErrorSince = new Dictionary<string, int>();   // when the count began (it starts again after two quiet minutes)
        private readonly HashSet<string> _featureOff = new HashSet<string>();
        private Action _aFrame, _aReader, _aEvents, _aRequests, _aIni, _aPlaces, _aHosts, _aStalker, _aDrama, _aCourtThoughts, _aCeremony, _aCast, _aPickups, _aMission,
            _aPlead, _aTheft, _aArrival, _aLater, _aVoices, _aWake, _aWakeActs;
        private bool _missionPaused;

        /// Runs one feature; if it keeps throwing, only that feature is switched off (not the whole add-on).
        private void Safe(string feature, Action a)
        {
            if (_featureOff.Contains(feature))
                return;
            // timed for the frame-time meter (WestworldScript.Perf.cs): its own time, nested features subtracted
            bool timed = _cfg == null || _cfg.PerfMeter;
            PerfSlot slot = timed ? PerfSlotOf(feature) : null;
            int depth = 0;
            long start = timed ? PerfBegin(out depth) : 0;
            int wd = WatchEnter(feature);   // the hang watch: where the game thread is, for a thread that looks on
            try
            {
                a();
            }
            catch (Exception ex)
            {
                // switched off only when it fails over and over: 15 errors with no two quiet minutes between them (players,
                // 2026-10-30: Alt+R stopped working a while into the game; the count used to run all session, so a rare
                // error every so often added up to the settings panel switched off for good)
                int n, since, t = Environment.TickCount;
                _featureErrors.TryGetValue(feature, out n);
                if (_featureErrorSince.TryGetValue(feature, out since) && (t - since > 120000 || t < since))
                    n = 0;
                _featureErrorSince[feature] = t;
                _featureErrors[feature] = ++n;
                int total;
                _featureErrors.TryGetValue(feature + "|all", out total);
                _featureErrors[feature + "|all"] = ++total;
                if (total == 1 || n == 1 && total <= 5)
                    Log.Write("ERROR in " + feature + ": " + ex);
                if (n >= 15)
                {
                    _featureOff.Add(feature);
                    Log.Write(feature + " kept failing and is switched off until the game restarts (details above). Everything else carries on." +
                              (feature == "settings panel" ? " The settings key brings it back." : ""));
                }
            }
            finally
            {
                WatchLeave(wd);
                if (timed)
                    PerfEnd(slot, start, depth);
            }
        }

        // ---- the hang watch (Lex, 2026-10-31: the game froze twice in Saint Denis; Windows called both a hang and the log
        // said nothing) ----------------------------------------------------------------------------------------------
        // Safe notes which feature the game thread is in (nested ones too); a background thread looks every second, and if
        // the same feature has held the game thread over 2 s, the log says which, and again at 8 s and 30 s. Nothing but
        // ints and strings cross between the threads; the log has its own lock.
        private static readonly string[] _watchAt = new string[16];
        private static volatile int _watchDepth, _watchSince, _watchSeq;
        private static System.Threading.Thread _watcher;
        private static volatile bool _watchStop;

        /// The watch ended with the script, before Script Hook unloads it: a thread left running can hold up the reload (Lex,
        /// 2026-10-31: Insert froze the game after "Aborted script").
        private static void WatchStop()
        {
            _watchStop = true;
            System.Threading.Thread t = _watcher;
            if (t != null)
                try { t.Join(2000); } catch { }
            _watcher = null;
        }

        // the native call a looks read is in, and which piece of how many (the recognition plan, Stage 0c): the watch's line
        // then says "inside testing > looks probe (guids, piece 3 of 11)"; set by Looks.Read, cleared on every enter and leave
        private static volatile string _watchNative;
        private static volatile int _watchPiece, _watchPieces;

        internal static void WatchNative(string native, int piece, int pieces)
        {
            _watchNative = native;
            _watchPiece = piece;
            _watchPieces = pieces;
            _watchSince = Environment.TickCount;   // each call timed on its own: a hang is one call that never returns
            _watchSeq++;
        }

        internal static int WatchEnter(string feature)
        {
            _watchNative = null;
            int d = _watchDepth;
            if (d < _watchAt.Length)
                _watchAt[d] = feature;
            _watchDepth = d + 1;
            _watchSince = Environment.TickCount;
            _watchSeq++;
            if (_watcher == null && !_watchStop)
            {
                _watcher = new System.Threading.Thread(WatchLoop) { IsBackground = true, Name = "RealNPCs hang watch", Priority = System.Threading.ThreadPriority.BelowNormal };
                _watcher.Start();
            }
            return d;
        }

        internal static void WatchLeave(int d)
        {
            _watchNative = null;
            _watchDepth = d;
            _watchSince = Environment.TickCount;
            _watchSeq++;
        }

        private static void WatchLoop()
        {
            int seenSeq = -1, told = 0;
            while (!_watchStop)
            {
                System.Threading.Thread.Sleep(250);
                if (_watchStop)
                    break;
                try
                {
                    int seq = _watchSeq, depth = _watchDepth, held = Environment.TickCount - _watchSince;
                    if (seq != seenSeq || depth == 0)
                    {
                        seenSeq = seq;
                        told = 0;
                        continue;   // moving, or between features (a pause or a load stops the ticks too; that's no hang)
                    }
                    int next = told == 0 ? 2000 : told == 1 ? 8000 : told == 2 ? 30000 : int.MaxValue;
                    if (held < next)
                        continue;
                    told++;
                    var path = new List<string>();
                    for (int i = 0; i < depth && i < _watchAt.Length; i++)
                        path.Add(_watchAt[i]);
                    string native = _watchNative;
                    int piece = _watchPiece, pieces = _watchPieces;
                    string call = native == null ? "" : " (" + native + (pieces > 0 ? ", piece " + piece + " of " + pieces : "") + ")";
                    Log.Write("HANG watch: the game thread has been inside " + string.Join(" > ", path) + call + " for " + held / 1000 + " s (RealNPCs is the one holding it)");
                }
                catch
                {
                    // the watch must never be what brings the game down
                }
            }
        }

        private void Setup04()
        {
            _aFrame = () => FrameImpl(_now);
            _aReader = ReaderInputImpl;
            _aEvents = () => PollWorldEventsImpl(_now);
            _aRequests = WorldRequestsImpl;
            _aIni = () => IniTick(_now);
            _aPlaces = () => Places(_now);
            _aHosts = () => HostsTick(_now);
            _aStalker = () => StalkerTick(_now);
            _aVoices = () => VoicesTick(_now);
            _aWake = () => WakeTick(_now);
            _aWakeActs = () => WakeActsTick(_now);
            _aDrama = () => DramaTick(_now);
            _aCourtThoughts = () => CourtThoughtsDue(_now);
            _aCeremony = () => Ceremonies(_now);
            _aCast = () => CastTick(_now);
            _aPickups = () => Pickups(_now);
            _aMission = () => MissionPause(_now);
            _aPlead = () => PleadTick(_now);
            _aTheft = () => TheftTick(_now);
            _aArrival = () => ArrivalTick(_now);
            _aLater = () => LaterTick(_now);
            _iniPath = Path.Combine(_dir, "RealNPCs.ini");
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            ApplySettings();
            LoadVoices();
            Feed.Thinking = (who, text, ped) => _ticker.Push(who + ", thinking", text, "near", ped);
            Feed.Spoke = ListenersThink;   // WestworldScript.Asides.cs
            // Nothing here may use _engine, _store or _bridge: they're made when a save loads (LoadStore), after this constructor,
            // and a use here stops the add-on loading at all. The thread hooks are set in LoadStore, with the engine.
            Log.Write("0.4 features: ticker " + (_cfg.Ticker ? "on" : "off") + ", news to " + _cfg.NewsRoute + ", reader key " + _readerKey +
                      ", reveries " + (_cfg.Reveries ? _cfg.ReturnChance + "%" : "off") + ", stalkers " + (_cfg.Stalkers ? _cfg.StalkerChance + "% every " + _cfg.StalkerEveryMinutes + " min" : "off") + ".");
            Log.Write("West World " + (_cfg.WestWorld ? "on (awakening " + _cfg.AwakeningShare + "%, breakdown " + _cfg.AwakeningBreakdown + "%, despair " + _cfg.AwakeningDespair + "%)" : "off") +
                      ", Stalkers " + (_cfg.Stalkers ? "on (" + _cfg.StalkerChance + "% every " + _cfg.StalkerEveryMinutes + " min, key " + _stalkerKey + ", when you stand still: " + _cfg.StalkerWhenStill + (_cfg.StalkerBlack ? ", in black: " + _cfg.StalkerBlackModel + " preset " + _cfg.StalkerBlackOutfit : ", black off") + ")" : "off") + ".");
            Log.Write("0.5 features: scene reactions " + (_cfg.SceneReactions ? "on" : "off") + ", standoffs " + (_cfg.Standoffs ? "on" : "off") + ", songs " + (_cfg.Songs ? "on" : "off") +
                      ", tall tales " + (_cfg.TallTales ? "on" : "off") + ", pickpockets " + (_cfg.Pickpockets ? "on" : "off") + ", pleading " + (_cfg.Pleading ? "on" : "off") +
                      ", arrivals " + (_cfg.ArrivalTalk ? "on" : "off") + ", mourners " + (_cfg.Mourners ? "on" : "off") + ", posses " + (_cfg.PosseTalk ? "on" : "off") + ".");
        }

        private void ApplySettings()
        {
            Log.Debug = _cfg.DebugLog;
            InnerLife.Historical = _cfg.HistoricalAccuracy;   // [World] HistoricalAccuracy: the times, the year, the calendar (Core/Calendar.cs)
            Folklore.On = _cfg.NightTales;   // [Scenes] NightTales: the old tales, in the street's scenes and by a campfire (Core/Folklore.cs)
            Almanac.On = _cfg.Almanac;   // [World] Almanac: the year's dated news (Core/Almanac.cs)
            Almanac.Share = _cfg.AlmanacShare;
            Payday.NightOn = _cfg.PaydayNight;   // [Scenes] PaydayNight (FOURTEENHOUR item 13)
            StreetMore.On = _cfg.StreetMore;     // [Scenes] StreetMore (FOURTEENHOUR item 14)
            LiftNerve.Apply(_cfg.LiftsWary);     // [Scenes] LiftsWary: the lift rarer, and choosier (Core/LiftNerve.cs; a player's report, 2026-11-10)
            Interact.StoodByOwes = _cfg.StoodByOwes;   // [Encounters] StoodByOwes: the man you stood with in a fight owes you (DEEPEN item 4)
            Interact.FineOwes = _cfg.FineOwes;         // [Encounters] FineOwes: the man whose fine you paid owes you (SEAMS item 1)
            Interact.SuretyKeptOwes = _cfg.SuretyKeptOwes;   // [Encounters] SuretyKeptOwes: a week kept on your word owes you (SEAMS item 10)
            KinGrudges.ThanksOn = _cfg.KinThanks;           // [Scenes] KinThanks: a kinsman of someone you saved comes to thank you (SEAMS item 7)
            Appraisal.GratitudeFelt = _cfg.GratitudeFelt;   // [Encounters] GratitudeFelt: the mind feels a stand, a fine, a surety as a kindness (SEAMS item 11)
            Helps.WalkHomeMendsOn = _cfg.WalkHomeMends;     // [Scenes] WalkHomeMends: seen home from the drink mends a drinking worry (SEAMS item 15)
            FriendCalls.YoursOn = _cfg.FriendsKnowYourThief;   // [Scenes] FriendsKnowYourThief: the warning names the one who robbed you (SEAMS item 14)
            BrawlVerbs.Reminds = _cfg.StandReminds;         // [Scenes] StandReminds: the J row says he stood by you, or owes you (SEAMS item 13)
            BrawlVerbs.Shunning = _cfg.WelshersShunned;     // [Scenes] WelshersShunned: no bets for a welsher this week (SEAMS item 4)
            SecondActs.StoodThanks = _cfg.StoodRemembered;   // [Encounters] StoodRemembered: the stand's memory brings its late thanks (SEAMS item 2)
            FriendCalls.ThiefWarnOn = _cfg.FriendsWarnThief;   // [Scenes] FriendsWarnThief: a friend warns you of a known pickpocket near (DEEPEN item 3)
            FriendChase.On = _cfg.FriendsChase;  // [Scenes] FriendsChase: a friend runs down the man who robbed you (Core/FriendChase.cs; DEEPEN item 2)
            WalkTogether.On = _cfg.WalkTogether; // [Mind] WalkTogether (FOURTEENHOUR item 15)
            Greenhorn.On = _cfg.Greenhorn;       // [Scenes] Greenhorn (FOURTEENHOUR item 16)
            Snub.On = _cfg.Snubs;                // [Scenes] Snubs (FOURTEENHOUR item 18)
            StepBetween.On = _cfg.StepBetween;   // [Scenes] StepBetween (DEPTH item 7)
            Brands.On = _cfg.Brands;             // [Scenes] Brands (DEPTH item 8)
            CollectionPlate.On = _cfg.CollectionPlate;   // [Scenes] CollectionPlate (DEPTH item 11)
            RevivedSeen.On = _cfg.RevivedSeen;           // [World] RevivedSeen (DEPTH item 15)
            Core.SessionClock.On = _cfg.SessionClock;     // [World] SessionClock (DEPTH item 16)
            Core.Familiarity.On = _cfg.Familiarity;       // [Mind] Familiarity (DEPTH item 17)
            Core.FeudArcs.On = _cfg.FeudArcs;             // [Mind] FeudArcs (DEPTH item 18)
            Core.SleepOnIt.On = _cfg.SleepOnIt;           // [Mind] SleepOnIt (DEPTH item 19)
            Mourning.GraveNightOn = _cfg.GraveNight;      // [World] GraveNight (DEPTH reserve R1)
            Core.NoteMeet.On = _cfg.NoteMeet;             // [Scenes] NoteMeet (DEPTH reserve R2)
            Slander.On = _cfg.SpreadRumour;               // [Scenes] SpreadRumour (DEPTH reserve R3)
            Core.CenturyTurns.On = _cfg.CenturyTurns;    // [World] CenturyTurns (DEPTH reserve R6)
            Core.MinersMeet.On = _cfg.MinersMeet;        // [Scenes] MinersMeet (DEPTH reserve R7)
            Core.Grumble.On = _cfg.Grumble;              // [Scenes] Grumble (DEPTH reserve R5)
            YourReward.On = _cfg.PostReward;              // [Scenes] PostReward (DEPTH reserve R4)
            Temperance.DoorOn = _cfg.TemperanceDoor && _cfg.Temperance;   // [Scenes] TemperanceDoor (FOURTEENHOUR item 25)
            RainShelter.On = _cfg.RainShelter;   // [Scenes] RainShelter (FOURTEENHOUR item 26)
            Holdup.On = _cfg.StoreHoldups;   // [Scenes] StoreHoldups (FOURTEENHOUR reserve R1)
            Holdup.Shots = _cfg.HoldupShots;
            Holdup.Percent = _cfg.HoldupChance;
            SecondActs.On = _cfg.SecondActs;   // [Encounters] SecondActs (FOURTEENHOUR reserve R3)
            DoctorsDoor.On = _cfg.DoctorsDoor;   // [Scenes] DoctorsDoor (FOURTEENHOUR reserve R4)
            Rowdy.On = _cfg.RowdyDrunks;   // [Scenes] RowdyDrunks (FOURTEENHOUR reserve R5)
            Cardsharp.On = _cfg.CardsharpRunOut;   // [Scenes] CardsharpRunOut (FOURTEENHOUR reserve R6)
            BestFist.On = _cfg.BestFist;   // [Scenes] BestFist (FOURTEENHOUR reserve R7)
            GameEncounters.On = _cfg.GameEncounters;   // [World] GameEncounters (FOURTEENHOUR reserve R8)
            Riders.On = _cfg.Riders;   // [Encounters] Riders (FOURTEENHOUR reserve R9)
            BarRound.WakeOn = _cfg.StandTheWake;   // [Scenes] StandTheWake (FOURTEENHOUR reserve R19)
            AlleyVoice.On = _cfg.AlleyVoice;   // [Encounters] AlleyVoice (FOURTEENHOUR reserve R15)
            Elopers.On = _cfg.Elopers;   // [Encounters] Elopers (FOURTEENHOUR reserve R18)
            WidowCollection.On = _cfg.WidowCollection;   // [Scenes] WidowCollection (FOURTEENHOUR reserve R20)
            HorseWatch.On = _cfg.HorseWatch;   // [Scenes] HorseWatch (FOURTEENHOUR reserve R21)
            Vouch.On = _cfg.Vouch;   // [Scenes] Vouch (FOURTEENHOUR reserve R22)
            NoMapRider.On = _cfg.NoMapRider && _cfg.Riders;   // [Encounters] NoMapRider (FOURTEENHOUR reserve R11)
            Stories.On = _cfg.Stories && _cfg.Letters;   // [World] Stories, offered by letter (FOURTEENHOUR reserve R10)
            Rivals.On = _cfg.Rivals;             // [Scenes] Rivals (FOURTEENHOUR item 19)
            PurseSnatch.On = _cfg.PurseSnatch;   // [Scenes] PurseSnatch (FOURTEENHOUR item 20)
            WitnessLaw.On = _cfg.WitnessesFetchLaw;   // [Mind] WitnessesFetchLaw (FOURTEENHOUR item 21)
            Census.On = _cfg.ControlCensus;      // [Mind] ControlCensus (FOURTEENHOUR item 22)
            Opinions.Act = _cfg.OpinionsAct && _cfg.MindLive;   // [Mind] OpinionsAct (FOURTEENHOUR item 23)
            Tales.On = _cfg.MemoriesBlur;        // [Mind] MemoriesBlur (FOURTEENHOUR item 24)
            OldSoldiers.On = _cfg.OldSoldiers && _cfg.HistoricalAccuracy;   // [Scenes] OldSoldiers: named history, so only with the real history on (item 17)
            AlmanacSides.On = _cfg.AlmanacSides;   // [World] AlmanacSides: their side on the year's quarrels (Core/AlmanacSides.cs)
            QuarrelPolitics.On = _cfg.QuarrelPolitics && _cfg.HistoricalAccuracy;   // [Mind] QuarrelPolitics: men in drink fight over 1899's quarrels (FOURTEENHOUR item 9); named history, so only with [World] HistoricalAccuracy (the review of items 1-13)
            LoopVoice.On = _cfg.WestWorld && _cfg.LoopVoice;   // the centre's thoughts only with the voice on (Core/LoopVoice.cs)
            Natives.Brisk = _cfg.ErrandPace;
            Feed.SpeechMs = Math.Max(3, _cfg.SubtitleSeconds) * 1000;
            Feed.Fallback = (title, text) => NewsPush(title, text, "news");   // when the game won't take top-left updates
            Feed.SubtitlesOn = _cfg.Subtitles != "off";
            Feed.SubtitleMode = _cfg.Subtitles;
            _ticker.SlotMs = _cfg.TickerSeconds * 1000;
            _ticker.GapMs = _cfg.TickerGapSeconds * 1000;
            _ticker.Several = _cfg.ThoughtsOverHeads ? _cfg.ThoughtsAtOnce : 1;   // beside the main line only over heads: the bar has room for one (FIVEDAY2 item 24)
            _ticker.ThoughtCacheMs = _ticker.Several > 1 ? 60000 : 90000;         // with room for more, each person moves on sooner
            JoinIn.BrawlBoost = _cfg.BrawlJoinBoost / 100f;   // onlookers readier to join (Lex, 2026-11-01)
            JoinIn.BraveryPull = Bravery.JoinPull(_cfg.Bravery);   // [World] Bravery: only how readily onlookers join a fight now (Lex, 2026-11-04)
            ActThoughts.PersonGapMs = _cfg.ActThoughtEachSeconds * 1000;   // the act thoughts' limits (Lex, 2026-11-01: livelier)
            ActThoughts.TheirsGapMs = _cfg.ActThoughtGapSeconds * 1000;
            _testKey = KeySpec.Parse(_cfg.TestKey, Keys.F7);
            _flirtKey = KeySpec.Parse(_cfg.FlirtKey, Keys.None);
            _readerKey = KeySpec.Parse(_cfg.ReaderKey, Keys.N);
            _stalkerKey = KeySpec.Parse(_cfg.StalkerKey, Keys.OemCloseBrackets);
            _execKey = KeySpec.Parse(_cfg.ExecutionKey, Keys.OemOpenBrackets);
            _cryingKey = KeySpec.Parse(_cfg.CryingWomanKey, Keys.None);   // the Testing page's keys: none unless the player sets one
            _hermitKey = KeySpec.Parse(_cfg.HermitKey, Keys.None);
            _gatherKey = KeySpec.Parse(_cfg.GatheringKey, Keys.None);
            _wakeKey = KeySpec.Parse(_cfg.WakeKey, Keys.Y);
            _settingsKey = KeySpec.Parse(_cfg.SettingsKey, Keys.F9);
            _settingsKey2 = KeySpec.Parse(_cfg.SettingsKey2, Keys.None);   // None (or nothing): no second key
            _tickerNextKey = KeySpec.Parse(_cfg.TickerNextKey, Keys.T);
            _spotKey = KeySpec.Parse(_cfg.SpotKey, Keys.K);   // Shift with it cycles the kind (MatchesLoose lets the Shift through)
            _eventKey = KeySpec.Parse(_cfg.EventKey, Keys.J);   // the trial's, the gallows' and the show's menus (WestworldScript.Trials.cs)
            _talkHotkey = KeySpec.Parse(_cfg.TalkKey, Keys.Q);   // read only when TalkKey isn't Q: Q is the game's own control (WestworldScript.Menu.cs)
            _reader.KeyHook = LetterKey;   // the pad's keys over the reader reach the letters' menus too (WestworldScript.Letters.cs)
            Speech.ShoutsCarry = _cfg.ShoutsCarry;
            Observed.BoldThiefChance = _cfg.BoldThieves;   // a thief who tries it with the law looking on ([Scenes] BoldThieves)
            ApplyTunedNumbers();
            VramApply();       // the VRAM gauge shown or not ([Display] VramGauge, with the AI model; WestworldScript.Vram.cs)
            BuildTestKeys();   // the Testing page's keys ([Testing], WestworldScript.Testing.cs)
            BuildPadKeys();    // and a controller's buttons ([Controller], WestworldScript.Pad.cs)
        }

        private void IniTick(int now)
        {
            if (!_cfg.ReloadIni || now < _nextIni)
                return;
            _nextIni = now + 3000;
            DateTime stamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            if (stamp == _iniStamp || stamp == DateTime.MinValue)
                return;
            _iniStamp = stamp;
            EnrageWestworld.Core.Settings fresh = EnrageWestworld.Core.Settings.Load(_iniPath, Log.Write);
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            if (fresh.Language != _cfg.Language)
                AiChosen(fresh, "in RealNPCs.ini");   // changed by hand: the player's choice from now on
            _cfg.CopyFrom(fresh.Gated());
            ApplySettings();
            Log.Write("Settings reloaded from the ini.");
            News("RealNPCs", "settings reloaded.", "news");
        }

        private void Shutdown04()
        {
            PanelTimeRestore("the add-on stopped");
            PassingAbort();
            TestCastClear("the add-on stopped");
            WanderLaterClear();
            PosseProbeAbort();   // the mounted probe's men let go (FIVEDAY R32; the review)
            SightsReset("shutdown");
            try
            {
                FlirtPromptEnd();
                ForgetPromptEnd();
                TalkShutdown();      // the Talk prompt out of the lock-on menu, the interaction menu closed (WestworldScript.Menu.cs)
                _director.AbortAll("stopping");
                EndEncounters("the add-on stopped");
                BelowAbort("the add-on stopped");
                WakeActsAbort();
                PatientAbort("the add-on stopped");
                GrudgesAbort();      // someone walking up to have it out with you stops (their walk order cleared)
                ThanksAbort();       // and someone walking over to thank you
                FriendAbort();       // and a friend coming over (Run 3, item 7)
                LonelyAbort();       // and someone lonely hoping for a word
                EscortAbort();       // and someone you were seeing home
                MeetAbort();         // and a letter's writer waiting to meet you (WestworldScript.Letters.cs)
                EventsAbort("the add-on stopped");   // and the dance floor (WestworldScript.TownEvents.cs)
                ErrandAbort();       // and a word you were carrying (memory only)
                FavourAbort();       // and someone coming to ask a favour, or to warn you (NIGHT4 item 6)
                ChanceWaitAbort("the add-on stopped");   // and a chance meeting waiting on the model's pick
                WarnAbort();
                KinAbort();
                LanguageStop();      // the language layer's worker thread
                VoiceStop();         // and the voice server's (WestworldScript.Voiced.cs)
                SoberVoices(true);   // the drink goes out of every voice we put it in
                WakeVoiceEnd(_now, "the add-on stopped");
                AnimTestEnd("the add-on stopped");
                GunshotTestEnd("the add-on stopped");
                TrystAbort();
                if (_rough != null)
                    RoughEnd(_rough, "stopped: the add-on stopped", _now);
                if (_stalker != null)
                    _stalker.Finish(true);
                Spawner.CleanupAll();
                ClearRegularBlips();
                // the ticker's text is not freed here: text sent to the game this frame is drawn after the script has
                // stopped, and freeing it under the game is the likeliest cause of a crash seen on an Insert reload
                // (0.6.1). It's a few kilobytes, kept until the game closes.
            }
            catch (Exception ex)
            {
                Log.Write("Cleanup: " + ex.Message);
            }
        }

        // ---- keys ---------------------------------------------------------------------------------

        private Keys _lastHotkey;
        private int _lastHotkeyAt;
        private bool _hotkeyLetGo;

        /// Windows repeats KeyDown while a key is held. The add-on's own keys act once per press; a quick second
        /// press counts once the key has been let go (T tapped through a long ticker line).
        private bool HeldKeyRepeat(KeyEventArgs e)
        {
            if (_cfg == null || !(_testKey.Matches(e) || _flirtKey.Matches(e) || _readerKey.Matches(e) || IsTestingKey(e) || SettingsKeyHit(e) || _tickerNextKey.Matches(e) || _spotKey.MatchesLoose(e) || (_eventKey.Matches(e) && _eventPrompt != null) || (!TalkOnControl() && _talkHotkey.Matches(e))))
                return false;
            int t = Environment.TickCount;
            bool repeat = e.KeyCode == _lastHotkey && !_hotkeyLetGo && t - _lastHotkeyAt >= 0 && t - _lastHotkeyAt < 600;
            _lastHotkey = e.KeyCode;
            _lastHotkeyAt = t;
            _hotkeyLetGo = false;
            return repeat;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _lastHotkey)
                _hotkeyLetGo = true;
        }

        private bool KeyDown04(KeyEventArgs e)
        {
            if (_menu != null && (_menu.Typing || _menu.Listening))
                return true;   // a line being typed in Talk, or the voice server's ear open: no hotkey fires (WestworldScript.Menu.cs reads the keys itself)
            if (PanelOpen)
            {
                _panelKeys.Enqueue(e);   // while the settings are open, every key is theirs
                return true;
            }
            if (SettingsKeyHit(e))
            {
                if (_featureOff.Remove("settings panel"))
                {
                    _featureErrors.Remove("settings panel");
                    Log.Write("SETTINGS key: the panel had switched itself off after errors; it's back on (any error comes to the log)");
                }
                _settingsRequested = true;
                return true;
            }
            if (_reader.Open)
            {
                if (_readerKey.Matches(e))
                    _readerKeys.Enqueue(Keys.Escape);
                else
                    _readerKeys.Enqueue(e.KeyCode);
                return true;
            }
            if (_readerKey.Matches(e))
            {
                _readerRequested = true;
                return true;
            }
            if (EventKey(e.KeyCode))   // the trial's and the gallows' menus: J while a prompt shows, the menu's keys while one is up (WestworldScript.Trials.cs)
                return true;
            // the Testing page's keys (the stalker, waking someone, the encounters, the talk and rough tests...): each asks
            // for its fire, which the tick does (WestworldScript.Testing.cs)
            if (TestKeyHit(e))
                return true;
            // a Talk key read directly (TalkKey other than Q, e.g. T) while its prompt is up for someone locked on to: the press
            // is Talk's, not the ticker's; with no
            // prompt up it falls through, so T still turns the ticker. TalkKey Q is the game's control, never read here
            if (!TalkOnControl() && _talkHotkey.Matches(e) && _cfg.InteractionMenu && _talkPrompt != 0 && _menu == null)
            {
                _talkKeyRequested = true;
                return true;
            }
            if (_tickerNextKey.Matches(e) && _cfg.Ticker)
            {
                _tickerNextRequested = true;
                return true;
            }
            if (_spotKey.MatchesLoose(e))   // the spot table: the place under the kind, or with Shift the next kind (WestworldScript.Spots.cs)
            {
                _spotRequested = e.Shift ? 2 : 1;
                return true;
            }
            return false;
        }

        private void ReaderInput()
        {
            Safe("reader", _aReader);
        }

        private void PollWorldEvents(int now)
        {
            _now = now;
            if (_engine != null)
                Safe("events", _aEvents);
        }

        private void WorldRequests()
        {
            Safe("world requests", _aRequests);
        }

        private void ReaderInputImpl()
        {
            while (_readerKeys.Count > 0)
            {
                Keys k = _readerKeys.Dequeue();
                if (LetterKey(k))   // the answers and the letters of his own, over the reader (WestworldScript.Letters.cs)
                    continue;
                _reader.Key(k);
            }
            if (!_readerRequested)
                return;
            _readerRequested = false;
            if (_uiOpen)
                return;
            if (TakePickup())
                return;
            List<Paper> papers = _store != null ? _store.Papers.Where(p => p.Delivered).OrderByDescending(p => p.At).ToList() : new List<Paper>();
            int unread = papers.FindIndex(p => !p.Read);
            Paper overheard = OverheardPaper();   // what went on near you this last day (WestworldScript.Overheard.cs)
            if (overheard != null)
                papers.Add(overheard);
            Paper board = NoticeBoardPaper();   // what's posted for the week, before "Who's who" (WestworldScript.NoticeBoard.cs; FIVEDAY item 16)
            if (board != null)
                papers.Add(board);
            if (_cfg != null && _cfg.WhosWho && _store != null)
            {
                // the people you've met, last among the papers, made now and never kept (Core/WhosWho.cs); for everyone, its
                // "Lately" (the mind's feelings) only with MindLive (Lex, 2026-10-30)
                string body = null;
                try
                {
                    body = WhosWho.Body(_store, Bridge.Now(), id => _cfg.MindLive && _engine != null && _engine.Shadow != null ? _engine.Shadow.Felt(id) : null);
                }
                catch (Exception ex)
                {
                    Log.Write("WHOS WHO failed: " + ex.Message);
                }
                if (body != null && papers.Count == 0)
                    body += "\n\nYou carry no letters, diaries or clippings yet. Letters wait at post offices in town. Diaries can be found on the dead, and in their camps.";   // the reader's own hint, which this page would hide
                if (body != null)
                    papers.Add(new Paper { Kind = "note", Title = "Who's who", From = "from memory", Body = body, Delivered = true, Read = true });
            }
            _reader.Show(papers, unread >= 0 ? unread : 0);
            Log.Detail("READER pages: " + papers.Count + " papers" + (overheard != null ? ", Overheard " + _overheardLines + " lines" : ""));
        }

        // ---- where notices go ---------------------------------------------------------------------

        private void Route(Toast t)
        {
            string label, text;
            string kind = t.Kind;
            switch (t.Kind)
            {
                case "thought":
                    // over the thinker's head when they're in view (Lex, 2026-10-27); every one of these is about you (what
                    // they remember of you, heard of you, your name, their heart, a sighting, kin's letters), so one far off
                    // goes to the "Around town" box as before (Lex: far-off thoughts there only when they're about you)
                    int h = NearHandleOf(t.PersonId);
                    if (h != 0)
                    {
                        _ticker.Push(t.Title + ", thinking", t.Text, "near", h);
                        return;
                    }
                    label = t.Title;
                    text = t.Text;
                    break;
                case "diary":
                    label = t.Title;
                    text = t.Text;
                    break;
                case "world":
                    // gang and faction news: the game's own top-left updates (by default), the "Around town" box, or the ticker
                    string war = _cfg.WarRoute;
                    if (war == "feed")
                        Feed.Notice(t.Title, t.Text);
                    else if (war == "corner")
                        NewsPush(t.Title, t.Text, "news");
                    else
                        _ticker.Push(t.Title, t.Text, "news");
                    Log.Write("WORLD NEWS (" + war + "): " + t.Title + ": " + t.Text);
                    return;
                case "life":
                    label = t.Title == NameOf(t.PersonId) ? null : t.Title;
                    text = t.Text;
                    kind = "news";
                    break;
                default:
                    label = t.Title;
                    text = t.Text;
                    kind = "news";
                    break;
            }
            Notice(label, text, kind, t.Kind == "thought" ? (PersonWoman(t.PersonId) ? "herself" : "himself") : null);
        }

        /// One notice, to wherever the player wants them. self: "himself"/"herself" for a private thought.
        private void Notice(string label, string text, string kind, string self = null)
        {
            if (string.IsNullOrEmpty(text))
                return;
            string route = _cfg.NewsRoute;   // corner, ticker, gamefeed or off (Settings.NewsRoute): never white text
            if (route == "off")
                return;   // NewsTo = off: no notices anywhere (the R7-R22 review: "off" fell through to the game feed's toasts)
            if (route == "corner")
            {
                NewsPush(label, text, kind);   // the "Around town" box (WestworldScript.News.cs)
                return;
            }
            if (route == "ticker")
            {
                _ticker.Push(label, text, kind);
                return;
            }
            // a toast at the top left of the screen (Toaster)
            if (kind == "thought")
                Feed.Notice(label + ", to " + (self ?? "himself"), "\"" + text + "\"");
            else if (kind == "diary")
                Feed.Notice(label, "\"" + text + "\"");
            else
                Feed.Notice(label, text);
        }

        private void News(string label, string text, string kind = "news")
        {
            Notice(label, text, kind);
        }

        private void Hint(string text, int ms)
        {
            text = Translations.Of(text);   // the player's own translation, when there is one (Run 3, item 8)
            _hint = text;
            _hintUntil = Game.GameTime + ms;
            if (!_cfg.Ticker)
                Feed.Notice(null, text);
        }

        // ---- every frame ---------------------------------------------------------------------------

        private void Frame(int now)
        {
            _now = now;
            Draw.Clean = _cfg != null && _cfg.CleanScreen;   // the clean screen, one check in Draw (Draw.Hidden)
            Draw.Part = Draw.PartVram;
            VramStep();   // the VRAM gauge, top left: one piece of the frame's text, before the rest (WestworldScript.Vram.cs)
            Draw.Part = Draw.PartOther;   // each step says who it is, so a frame's text can be told apart (Draw.Spent)
            Safe("ticker", _aFrame);
            ThinStreetsStep(now);   // a thinned town's ambient density, this frame (WestworldScript.Ledgers.cs; [World] ThinStreets)
            Draw.Part = Draw.PartFeelings;
            FeelingsStep(now);
            if (_cfg.NameCardsAlways)
            {
                Draw.Part = Draw.PartSubtitles;
                SubtitleDraw(now);
                Draw.Part = Draw.PartNameCards;
                NameCardsStep(now);   // always on: before the Testing overlays, so a crowd's cards aren't the ones the frame's text budget drops (Lex, 2026-10-30)
            }
            Draw.Part = Draw.PartMinds;
            ShowMindsStep(now);   // Show minds, the cards over people (the UI page; WestworldScript.ShowMinds.cs)
            Draw.Part = Draw.PartWake;
            WakeTagsStep(now);    // and its wake stages over heads (WestworldScript.WakeTags.cs)
            Draw.Part = Draw.PartNews;
            NewsStep(now);
            if (!_cfg.NameCardsAlways)
            {
                Draw.Part = Draw.PartSubtitles;
                SubtitleDraw(now);
                Draw.Part = Draw.PartNameCards;
                NameCardsStep(now);   // names over heads, last: the least to lose if the frame's text budget runs out (WestworldScript.NameCards.cs)
            }
            Draw.Part = Draw.PartOther;
        }

        // a line being said near you (a scene, the doctor, a hello), as a subtitle in the game's style for its own time, never
        // on the bar, which the thoughts used to share (Lex, 2026-10-30: "There shouldn't be inner thoughts showing at the
        // bottom of the screen"; a mourner's line read as one)
        private void SpeechCaption(string label, string text, int handle, int ms, bool toYou = false, bool voice = true)
        {
            if (string.IsNullOrEmpty(text))
                return;
            VoiceJob said = voice ? VoiceSaid(handle, label, text, toYou) : null;   // in their own voice too, with VoicedDialogue and the voice server (WestworldScript.Voiced.cs); off screen as well
            if (said != null)
            {
                // the subtitle waits for the voice (a second or two to make) and shows as it starts; shown anyway when the voice
                // isn't coming or takes too long (Lex, 2026-11-03: the voice came behind the words)
                said.CapLabel = label;
                said.CapMs = ms;
                said.CapToYou = toYou;
                said.CapAskedAt = Environment.TickCount;
                said.CapText = text;
                return;
            }
            CaptionNow(label, text, handle, ms, toYou);
        }

        /// The subtitle itself, now: to the player the top box, else the one under it, and only for someone on screen.
        private void CaptionNow(string label, string text, int handle, int ms, bool toYou)
        {
            if (!toYou && handle != 0 && ENTITY.DOES_ENTITY_EXIST(handle) && !ENTITY.IS_ENTITY_ON_SCREEN(handle))
            {
                Log.Detail("CAPTION " + label + ": " + text + " (no subtitle: off screen)");   // between others, only who you can see (Lex, 2026-10-31: hard to tell where it came from)
                return;
            }
            float metres = !toYou && handle != 0 && ENTITY.DOES_ENTITY_EXIST(handle) ? ENTITY.GET_ENTITY_COORDS(handle, true, true).DistanceTo(Game.Player.Character.Position) : -1f;
            bool shown = toYou ? Feed.ToYouCaption(label, text, Math.Max(1500, ms)) : Feed.Overheard(label, text, Math.Max(1500, ms), handle, metres);   // to the player: the top box; else the one under it (a newer caption from someone else, farther off, waits its turn)
            Log.Detail("CAPTION " + (toYou ? "(to you) " : "") + label + ": " + text + (shown ? "" : " (no subtitle: [Output] Subtitles off, or another speaker's caption still new)"));
            if (!toYou && handle != 0)
                ListenersThink(handle, label, text, ms);   // and whoever is listening thinks something about it (WestworldScript.Asides.cs)
        }

        private void FrameImpl(int now)
        {
            bool askNext = _tickerNextRequested;   // the ticker key: only counts while the ticker is showing
            _tickerNextRequested = false;
            if (PanelOpen)
                return;   // the settings panel draws itself; no ticker under it
            if (_reader.Open)
            {
                _reader.Render();
                LetterMenuDraw();   // the letters' hint or menu over the paper (WestworldScript.Letters.cs)
                return;
            }
            EventMenuDraw();   // the trial's and the gallows' prompt or menu (WestworldScript.Trials.cs)
            Draw.Part = Draw.PartTalk;
            TalkMenuFrame(now);   // the interaction menu, ticker or no ticker (WestworldScript.Menu.cs)
            Draw.Part = Draw.PartOther;
            if (_engine == null || !_cfg.Ticker)
                return;
            if (!TickerVisible(now))
                return;
            if (now >= _nextNear)
            {
                _nextNear = now + 1000;
                RefreshNear();
            }
            if (askNext && _cfg.TickerShowsScene && _sceneBarCount >= 2)
            {
                // the bar shows the scenes nearby: the key steps to the next of them, and holds it a while (Lex, 2026-10-30)
                _scenePick = _sceneBarIdx + 1;
                _scenePickUntil = now + 12000;
                askNext = false;
            }
            if (askNext && !Ticker.NextPart(_tickerLine, _cfg, now))
            {
                _ticker.Skip();
                _nextTickerUpdate = 0;
            }
            if (now >= _nextTickerUpdate || _tickerLine == null)
            {
                _nextTickerUpdate = now + 200;
                TickerLine was = _tickerLine;
                _tickerLine = _ticker.Update(now, _near, _cfg.NewsRoute == "corner" || _cfg.NewsRoute == "off" ? null : (Func<string>)(() => _engine.Summary()));   // the news has its own box then
                if (_tickerLine != was)
                {
                    Safe("thought sound", () => ThoughtSound(_tickerLine, now));
                    Safe("voiced thought", () => VoiceThought(_tickerLine, now));   // murmured in their own voice (WestworldScript.Voiced.cs), before Aloud, which then stays quiet for it
                }
                Safe("aloud", () => AloudAsk(_tickerLine, now));   // said aloud in their own voice, if it fits (WestworldScript.Aloud.cs)
                if (_tickerLine != was && _tickerLine != null && _tickerLine.Kind == "near" && _tickerLine.Handle != 0)
                {
                    _thoughtsMainShown++;
                    NoteTheirThought(_tickerLine.Handle, now, _tickerLine.Text);   // a thought over someone came up: no act's or turn's thought of theirs on top of it (WestworldScript.ActThoughts.cs)
                }
                if (_tickerLine != was && _tickerLine != null && _tickerLine.Kind == "near" && _tickerLine.Key != null)
                    ThreadThoughtShown(_tickerLine.Key, now);
                SideThoughtsCame(now);   // and the same for those beside it (WestworldScript.OverHead.cs)
            }
            TickerLine line = _tickerLine;
            // a thought goes over its thinker's head (WestworldScript.OverHead.cs), under its own guard, and with
            // ThoughtsOverHeads on never on the bar (Lex, 2026-10-27): one whose thinker is off screen waits, and after
            // a moment the ticker moves on; anything else (the news, the summary) on the bar
            _overHeadDrawn = false;
            if (_aOverHead == null)
                _aOverHead = () =>
                {
                    bool despair = DespairOverHead(_now);   // despair's words first, while they last (WestworldScript.Despair.cs), and on their own
                    _overHeadDrawn = despair || ThoughtOverHead(_tickerLine, _now);
                    if (!despair)
                        SidesOverHead(_now);   // the others thinking at the same time (FIVEDAY2 item 24)
                };
            Draw.Part = Draw.PartThought;
            Safe("thoughts over heads", _aOverHead);
            Draw.Part = Draw.PartBar;
            TickerLine scene = _cfg.TickerShowsScene ? SceneOnBar() : null;   // the street scene near you takes the bar while it plays
            if (!_overHeadDrawn && OverHeadOnly(line, now))
            {
                _ticker.Skip();
                _nextTickerUpdate = 0;
            }
            if (scene != null)
                Ticker.Render(scene, _cfg, now);
            else if (!_overHeadDrawn && !IsOverHeadKind(line))
            {
                Ticker.Render(line, _cfg, now);
                if (_cfg.ThoughtMarker)
                    DrawThinkingMark(line);
            }
            Draw.Part = Draw.PartHint;
            if (_hint != null && now < _hintUntil)
            {
                float s = _cfg.TickerScale * 0.9f;
                // wrapped to the screen, the lines stacked upward from where the one line was (a Testing row's words run long)
                if (_hint != _hintRowsFor || s != _hintRowsScale)
                {
                    _hintRows = Draw.Wrap(_hint, s, 0.9f);   // once a hint, not every frame (the cost audit: a Testing row's long words, ~100 allocations a frame)
                    if (_hintRows.Count > 2)   // two lines at most: a test's long words took the frame's text from the names (Lex, 2026-10-31); the whole of it is in the log
                        _hintRows = new List<string> { _hintRows[0], Draw.Fit(string.Join(" ", _hintRows.Skip(1)), s, 0.9f) };
                    _hintRowW = new float[_hintRows.Count];
                    _hintW = 0f;
                    for (int i = 0; i < _hintRows.Count; i++)
                    {
                        _hintRowW[i] = Draw.Width(_hintRows[i], s);
                        _hintW = Math.Max(_hintW, _hintRowW[i]);
                    }
                    _hintRowsFor = _hint;
                    _hintRowsScale = s;
                }
                List<string> rows = _hintRows;
                float lh = Draw.LineHeight(s);
                float y = _cfg.TickerY - Draw.LineHeight(_cfg.TickerScale) * 1.4f - (line != null && line.Text != null && line.Text.Length > 90 ? Draw.LineHeight(_cfg.TickerScale) : 0f) - (rows.Count - 1) * lh;
                float w = _hintW;
                Draw.Rect(0.5f - w / 2f - 0.006f, y - 0.003f, w + 0.012f, lh * rows.Count + 0.006f, 12, 10, 8, 170);
                for (int i = 0; i < rows.Count; i++)
                    Draw.Text(rows[i], 0.5f - _hintRowW[i] / 2f, y + i * lh, s, 236, 214, 160);
            }
        }

        private TickerLine _sceneBar;
        private const float SceneBarMetres = 40f;

        /// "Nearby: two men came to blows (Gideon and Asa)" while a street scene plays within 40 m (Lex, 2026-10-30: the
        /// bar shows the nearest scene playing out near you, a switch on the UI page); null when none.
        private TickerLine SceneOnBar()
        {
            // every scene playing within 40 m, nearest first, as many as the bar's width holds; the rest "(+2 more)" (Lex, 2026-10-30:
            // "show all scenes happening nearby, try to account for if the text will go off the screen")
            // (no LINQ: the 40 m filter and the nearest-first order go into reused lists, an insertion sort so equal distances
            // keep the Director's order as OrderBy's stable sort did; the frame-cost audit's follow-up, NIGHT4 item 6)
            Vector3 me = Game.Player.Character.Position;
            List<KeyValuePair<string, Vector3>> all = _director.TickerLines();
            List<KeyValuePair<string, Vector3>> near = _sceneNear;
            near.Clear();
            _sceneNearM.Clear();
            for (int i = 0; i < all.Count; i++)
            {
                float m = all[i].Value.DistanceTo(me);
                if (m > SceneBarMetres)
                    continue;
                int at = near.Count;
                while (at > 0 && _sceneNearM[at - 1] > m)
                    at--;
                near.Insert(at, all[i]);
                _sceneNearM.Insert(at, m);
            }
            _sceneBarCount = near.Count;
            _sceneBarIdx = -1;
            if (near.Count == 0)
            {
                _sceneBar = null;
                return null;
            }
            float room = _cfg.TickerWidth - Draw.Width("Nearby", _cfg.TickerScale) - 0.02f;
            List<string> parts = _sceneParts;
            parts.Clear();
            bool more = false;   // a "(+N more)" went in: the bar can't hold them all
            for (int i = 0; i < near.Count; i++)
            {
                string one = near[i].Key + " - " + Whereabouts(me, near[i].Value);
                if (parts.Count > 0)
                {
                    int left = near.Count - i - 1;
                    string tryText = string.Join("  \u00b7  ", parts) + "  \u00b7  " + one + (left > 0 ? "  (+" + left + " more)" : "");
                    if (Draw.Width(tryText, _cfg.TickerScale) > room)
                    {
                        parts.Add("(+" + (near.Count - i) + " more)");
                        more = true;
                        break;
                    }
                }
                parts.Add(one);
            }
            string text = near[0].Key;   // the line is made anew only when the nearest scene changes
            string shown0 = string.Join("  \u00b7  ", parts);
            if (parts.Count < near.Count || more || (Game.GameTime < _scenePickUntil && near.Count >= 2))
            {
                // more than the bar holds: each in turn, four seconds apiece, so none is hidden (Lex, 2026-10-30)
                int idx = (int)((Game.GameTime / 4000) % near.Count);
                if (Game.GameTime < _scenePickUntil)
                    idx = _scenePick % near.Count;   // the one picked with the ticker key
                _sceneBarIdx = idx;
                shown0 = near[idx].Key + " - " + Whereabouts(me, near[idx].Value) + "  (" + (idx + 1) + " of " + near.Count + ")";
            }
            // how far and which way, to the nearest 5 m (Lex, 2026-10-30: "such as 10m South"); the line is made anew only
            // when the scene or that changes
            string shown = shown0;
            if (_sceneBar == null || !ReferenceEquals(_sceneBarOf, text))
            {
                _sceneBarOf = text;
                _sceneBar = new TickerLine { Label = "Nearby", Text = shown, Kind = "news", ShownAt = Game.GameTime };
            }
            else if (_sceneBar.Text != shown)
                _sceneBar.Text = shown;
            return _sceneBar;
        }

        private string _sceneBarOf;
        private readonly List<KeyValuePair<string, Vector3>> _sceneNear = new List<KeyValuePair<string, Vector3>>();   // SceneOnBar's, refilled each call
        private readonly List<float> _sceneNearM = new List<float>();
        private readonly List<string> _sceneParts = new List<string>();

        private int _namedForCards;

        /// Without enRAGE, everyone within the cards' reach is one of ours, with a name (two new a second, nearest first; once
        /// named they stay so): enRAGE names its townsfolk when it runs, and alone the add-on does it for everyone it can.
        private void NameEveryone(Ped me)
        {
            if (_bridge == null || _bridge.Present || _engine == null)
                return;
            Vector3 pos = me.Position;
            float reach = Math.Max(_cfg.NameCardsMetres, 12f);
            var fresh = new List<KeyValuePair<Ped, float>>();
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Handle == me.Handle || _resBind.ContainsKey(p.Handle))
                    continue;
                float d = p.Position.DistanceTo(pos);
                if (d <= reach && ResidentEligible(p))
                    fresh.Add(new KeyValuePair<Ped, float>(p, d));
            }
            foreach (KeyValuePair<Ped, float> kv in fresh.OrderBy(x => x.Value).Take(2))
                if (ResidentOf(kv.Key, true) != null)
                    _namedForCards++;
        }
        private int _sceneBarCount, _sceneBarIdx = -1, _scenePick, _scenePickUntil;

        private static readonly string[] Points = { "north", "northeast", "east", "southeast", "south", "southwest", "west", "northwest" };

        /// "10 m south": from here to there, to the nearest 5 m, on the game's map (north is +Y, east +X).
        private static string Whereabouts(Vector3 from, Vector3 to)
        {
            float dx = to.X - from.X, dy = to.Y - from.Y;
            int m = Math.Max(5, (int)Math.Round(Math.Sqrt(dx * dx + dy * dy) / 5.0) * 5);
            double deg = (Math.Atan2(dx, dy) * 180.0 / Math.PI + 360.0) % 360.0;
            return m + " m " + Points[(int)Math.Round(deg / 45.0) % 8];
        }

        private int _tickerVisFrame = -1;
        private bool _tickerVis;

        /// Whether what we draw may show this frame. Asked by the ticker, the news, the feelings, the subtitles and the VRAM
        /// gauge, five or six times a frame, four natives each: worked out once a frame now (THREEHOUR item 1).
        private bool TickerVisible(int now)
        {
            int frame = Game.FrameCount;
            if (frame == _tickerVisFrame)
                return _tickerVis;
            _tickerVisFrame = frame;
            PerfSub sub = SubStart();   // its own line on the frame meter: the first feature to ask each frame paid for it (FOURTEENHOUR item 2)
            _tickerVis = TickerVisibleNow(now);
            SubEnd(ref _psHud, "the screen check (HUD, menus)", sub);
            return _tickerVis;
        }

        private PerfSlot _psHud, _psHudMenus;

        private bool TickerVisibleNow(int now)
        {
            if (HUD.IS_PAUSE_MENU_ACTIVE() || HUD.IS_HUD_HIDDEN() || CAM.IS_SCREEN_FADING_OUT())
                return false;
            if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
            {
                if (_controlOffAt < 0)
                    _controlOffAt = now;
                if (now - _controlOffAt > 1500)
                    return false;
            }
            else
            {
                _controlOffAt = -1;
            }
            if (_bridge != null && now >= _nextUi)
            {
                _nextUi = now + 250;
                PerfSub sub = SubStart();   // enRAGE's menus, asked through the bridge every 250 ms: its own line (item 2)
                _uiOpen = _bridge.UiOpen();
                SubEnd(ref _psHudMenus, "the screen check: enRAGE's menus", sub);
            }
            return !_uiOpen;
        }

        /// The day of the week and the year, for what people think about (InnerLife). In the epilogue the clock may
        /// still say 1899, so John means 1907 either way.
        private static void ReadClock()
        {
            InnerLife.Weekday = CLOCK.GET_CLOCK_DAY_OF_WEEK();
            int year = CLOCK.GET_CLOCK_YEAR();
            InnerLife.ClockYear = IsJohn() && year < 1900 ? 1907 : year;
            InnerLife.CalendarYear = year;   // the clock's own year: the dates and the weekdays follow it (Core/Calendar.cs)
            try
            {
                DateTime d = GameDate.FromMinutes(Bridge.Now());
                InnerLife.ClockMonth = d.Month;   // the month, for what was in the papers by now (Times.cs)
                InnerLife.ClockDay = d.Day;       // the day, for the real calendar
            }
            catch
            {
                InnerLife.ClockMonth = 0;
                InnerLife.ClockDay = 0;
            }
        }

        /// What the residents near you are living through, for the F7 dump.
        private string WorriesStatus()
        {
            Ped me = Game.Player.Character;
            long day = _store.LastGameMin > 0 ? _store.LastGameMin / 1440 : -1;
            List<string> parts = _resBind.Where(kv => ENTITY.DOES_ENTITY_EXIST(kv.Key) && !kv.Value.R.Dead && new Ped(kv.Key).Position.DistanceTo(me.Position) < 40f)
                .Take(6).Select(kv => kv.Value.R.Name + ": " + InnerLife.WorryStatus(_engine.PersonOfResident(kv.Value.R), day)).ToList();
            return "weekday " + InnerLife.Weekday + ", year " + InnerLife.ClockYear + "; " + (parts.Count == 0 ? "nobody with a name near" : string.Join("; ", parts));
        }

        /// Who the ticker counts as near you, nearest first, and what each would think right now, for the F7 dump.
        /// The number is the ticker's ranking distance (counter keepers +2 m, enRAGE's townsfolk +3, passers-by +10).
        private string NearStatus()
        {
            if (_near.Count == 0)
                return "nobody (the ticker shows the news)";
            return string.Join("; ", _near.OrderBy(n => n.Distance).Take(8).Select(n =>
            {
                string t;
                try
                {
                    t = n.Text != null ? n.Text() : null;
                }
                catch (Exception ex)
                {
                    t = "FAILED: " + ex.GetType().Name + ": " + ex.Message;
                }
                return n.Name + " (" + (int)n.Distance + "): " + (string.IsNullOrEmpty(t) ? "(nothing)" : t);
            }));
        }

        private void RefreshNear()
        {
            _near.Clear();
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            Safe("names for all", () => NameEveryone(me));
            Vector3 pos = me.Position;
            float r = _cfg.TickerNearbyMetres;
            long now = Bridge.Now();
            ReadClock();
            List<KeyValuePair<Ped, float>> strangers = new List<KeyValuePair<Ped, float>>();
            List<KeyValuePair<Ped, float>> passers = new List<KeyValuePair<Ped, float>>();   // those passing, on the road (FIVEDAY2 item 26)
            bool road = _cfg.RoadThoughts && _placeTown == null && _interiorType == 0;
            float wide = road ? Math.Max(r, _cfg.RoadThoughtsMetres) : r;
            int nowMs = Game.GameTime;
            MomentSweepStart();   // the street around you, for everyone's moment (WestworldScript.Moments.cs)
            foreach (Ped ped in Peds.All())
            {
                if (ped == null)
                    continue;
                int h = ped.Handle;
                if (h != me.Handle)
                {
                    float md = ped.Position.DistanceTo(pos);
                    if (md <= 40f && PED.IS_PED_HUMAN(h))
                        MomentSweep(h, md, ENTITY.IS_ENTITY_DEAD(h), nowMs);
                }
                if (IsHermit(h))
                {
                    float hd = ped.Position.DistanceTo(pos);
                    if (hd <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        HermitNear(ped, hd);
                    continue;
                }
                if (IsGatherer(h))
                {
                    float gd = ped.Position.DistanceTo(pos);
                    if (gd <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        GatherNear(ped, gd);
                    continue;
                }
                if (IsWaysidePed(h))
                {
                    float wd = ped.Position.DistanceTo(pos);
                    if (wd <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        WaysideNear(ped, wd);
                    continue;
                }
                if (IsExecutionPed(h))
                {
                    float ed = ped.Position.DistanceTo(pos);
                    if (ed <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        ExecutionNear(ped, ed);
                    continue;
                }
                if (h == me.Handle || Spawner.IsOurs(h))
                    continue;
                float d = ped.Position.DistanceTo(pos);
                if (d > wide || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                if (d > r)
                {
                    // past the usual reach: only a stranger on a horse or a wagon, seen coming along the road
                    if ((PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false)) && ENTITY.IS_ENTITY_ON_SCREEN(h) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17) && ResidentEligible(ped))
                        passers.Add(new KeyValuePair<Ped, float>(ped, d));
                    continue;
                }
                // only people you can see think on the ticker, so it's always clear whose thought it is
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(h) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17);
                // a loud moment (a fight, a flight, a gun on them: WestworldScript.Moments.cs) gets the first turn and a line every few seconds
                bool urgent = seen && MomentLoud(ped, me, d);
                RecordRef rec = _bridge.RecordAt(h);
                if (rec != null)
                {
                    if (!rec.IsAlive || !seen)
                        continue;
                    Person p = PersonOf(rec);
                    PersonState ps = _store.Find(p.Id);
                    _near.Add(new NearThought { Key = p.Id, Name = p.Name, Handle = h, Distance = d, Beside = d < BesideMetres, Urgent = urgent,
                                                Text = () => ThoughtNow(ped, me, d, p, ps, p.Id, null, lk => _engine.TickerThought(p, now, lk)) });
                    continue;
                }
                // right beside you: a new thought every 15 game minutes (30 s), to match the ticker's quicker turn
                long every = d < BesideMetres ? 15 : EveryNow;
                FolkRef a = _bridge.AcquaintanceAt(h);
                if (a != null && !string.IsNullOrEmpty(a.Name))
                {
                    if (!seen)
                        continue;
                    Folk f = Snapshot.Of(a);
                    Person fp = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, now);
                    PersonState st = _store.Get(fp);
                    float fd = d;
                    _near.Add(new NearThought
                    {
                        Key = f.Key,
                        Handle = h,
                        Name = f.Name,
                        Distance = d + 3f,
                        Beside = d < BesideMetres,
                        Urgent = urgent,
                        Text = () => ThoughtNow(ped, me, fd, fp, st, f.Key, Look(me, fd), lk => _cfg.WestWorld && st.WakeStage > 0 ? Hosts.Thought(fp, st, _engine.Player, f.Key + now / 180)
                            : _engine.FolkThought(f, fp, st, f.Key + "|" + now / every, CLOCK.GET_CLOCK_HOURS(), lk))
                    });
                    continue;
                }
                // the person at a counter: one person for good, who knows you
                PostKind pk = _cfg.TownPosts ? Posts.Of(Natives.ModelName(ped)) : null;
                if (pk != null)
                {
                    Resident holder = HolderAt(ped, pk);
                    if (holder != null)
                    {
                        NotePostVisit(holder, d);   // a visit counts whether or not you're looking at them
                        float near = d;
                        if (seen)
                            _near.Add(new NearThought
                            {
                                Key = holder.Id,
                                Handle = h,
                                Name = holder.Name + ", " + pk.Role,
                                Distance = d + 2f,
                                Beside = d < BesideMetres,
                                Urgent = urgent,
                                Text = () =>
                                {
                                    PersonState hs = _store.Find(holder.Id);
                                    Person hp = _engine.PersonOfResident(holder);
                                    return ThoughtNow(ped, me, near, hp, hs, holder.Id, Look(me, near), lk => _cfg.WestWorld && hs != null && hs.WakeStage > 0 ? Hosts.Thought(hp, hs, _engine.Player, holder.Id + "|" + now / 180)
                                        : _engine.PostThought(holder, near < 10f, CLOCK.GET_CLOCK_HOURS(), holder.Id + "|" + now / every, lk));
                                }
                            });
                    }
                    continue;
                }
                // someone fighting or fleeing is a stranger with a loud moment (a thought of it), not passed over as before
                if (seen && d < 12f && (urgent || (!PED.IS_PED_IN_COMBAT(h, 0) && !PED.IS_PED_FLEEING(h))) && ResidentEligible(ped))
                    strangers.Add(new KeyValuePair<Ped, float>(ped, d));
                else if (seen && road && d >= 12f && !urgent && !PED.IS_PED_IN_COMBAT(h, 0) && !PED.IS_PED_FLEEING(h) && ResidentEligible(ped))
                    passers.Add(new KeyValuePair<Ped, float>(ped, d));
            }
            MomentSweepEnd(me, nowMs);
            // the three nearest ordinary people get turns too (behind anyone with a name, unless right beside you)
            bool named = _near.Count > 0;
            foreach (KeyValuePair<Ped, float> s in strangers.OrderBy(x => x.Value).Take(3))
            {
                Ped stranger = s.Key;
                float sd = s.Value;
                bool woman = !PED.IS_PED_MALE(stranger.Handle);
                bool loud = MomentLoud(stranger, me, sd);
                Resident res = ResidentOf(stranger);
                if (res != null)
                {
                    // an ordinary person, but with a name, a trade and a town: and a mind that may be waking. A town
                    // regular counts each time you meet them, and may know you again.
                    Person rp = _engine.PersonOfResident(res);
                    int met = res.Regular ? NoteRegularMet(res, stranger.Handle, rp) : 0;
                    long every = sd < BesideMetres ? 15 : EveryNow;
                    _near.Add(new NearThought
                    {
                        Key = res.Id,
                        Name = ShownName(stranger.Handle, res.Name),
                        Handle = stranger.Handle,
                        Distance = sd + 10f,
                        Beside = sd < BesideMetres,
                        Urgent = loud,
                        Text = () =>
                        {
                            PersonState rst = _store.Find(res.Id);
                            InnerLife.PlayerLook look = Look(me, sd);
                            look.Seen = met;
                            return ThoughtNow(stranger, me, sd, rp, rst, res.Id, look, lk => _cfg.WestWorld && rst != null && rst.WakeStage > 0 ? Hosts.Thought(rp, rst, _engine.Player, res.Id + "|" + now / 180)
                                : _engine.ResidentThought(rp, CLOCK.GET_CLOCK_HOURS(), res.Id + "|" + now / every, lk));
                        }
                    });
                }
                else if (loud || (!named && _near.Count == 0))
                {
                    // someone who can't be a resident: only when nobody else is near, as before, or in a loud moment of their own
                    string key = "stranger:" + stranger.Handle;
                    string town = _placeTown;
                    _near.Add(new NearThought
                    {
                        Key = key, Name = Grammar.UpperFirst(Director.Kind(stranger, woman)), Handle = stranger.Handle, Distance = sd + 10f, Urgent = loud,
                        Text = () => ThoughtNow(stranger, me, sd, null, null, key, null, lk =>
                        {
                            string t = _engine.StrangerThought(key + now / 600, town, woman);
                            if (t != null || lk.Now == null)
                                return t;
                            string src;
                            return Moments.Ambient(null, lk.Now, key + now / 600, null, out src);   // the quiet side of their moment rather than nothing
                        })
                    });
                }
            }
            foreach (KeyValuePair<Ped, float> s in passers.OrderBy(x => x.Value).Take(2))
                RoadNear(s.Key, s.Value, me);
            NoteNearby();
        }

        /// Someone you're passing on the road: a thought, and nothing kept. No name and no record: you'll never see them again, and that's the point of them.
        private void RoadNear(Ped ped, float d, Ped me)
        {
            int h = ped.Handle;
            string key = "road:" + h;
            long now = Bridge.Now();
            _near.Add(new NearThought
            {
                Key = key,
                Name = Grammar.UpperFirst(Director.Kind(ped, !PED.IS_PED_MALE(h))),
                Handle = h,
                Distance = d + 6f,
                Text = () => ThoughtNow(ped, me, d, null, null, key, null, lk => { _roadThoughtsMade++; return RoadThoughts.Passing(lk.Now, Voice.Hash(key) & 3, key + "|" + now / 30); })
            });
        }

        private int _nextNearby;

        /// Every few seconds: the named people in view, as the others see them (InnerLife.Nearby), so their thoughts can
        /// turn to each other: envy of the well-dressed, pity for the drunk, a wary eye on the hothead (InnerLife.Now.cs).
        private void NoteNearby()
        {
            int t = Game.GameTime;
            if (t < _nextNearby)
                return;
            _nextNearby = t + 5000;
            var list = new List<InnerLife.Nearby>();
            foreach (NearThought n in _near.Where(x => x.Handle != 0 && x.Key != null && !x.Key.StartsWith("stranger:") && !x.Key.StartsWith("road:")).Take(6))
            {
                Ped p = new Ped(n.Handle);
                if (!p.Exists())
                    continue;
                Someone who = SomeoneOf(p);
                if (who == null)
                    continue;
                // what they're known for this week: putting a man's face in the bar, or being caught at a pocket
                long g = Bridge.Now();
                string known = _store.Ties.Any(x => x.Did(who.Id) && x.Kind == "rough" && g - x.At >= 0 && g - x.At < 7 * 1440L) ? "bully"
                             : (_cfg.CriminalClass ? Criminals.Known(_store.Ties, who.Id, g)   // the criminal class (item 16): the month's thefts the town saw (a pickpocket caught, a mugging; never a lift nobody noticed: the review of items 15-19)
                                                   : _store.Ties.Any(x => x.Did(who.Id) && x.Kind == "pickpocket" && g - x.At >= 0 && g - x.At < 7 * 1440L)) ? "thief" : null;
                list.Add(new InnerLife.Nearby { Id = who.Id, Name = Person.FirstOf(who.Name), Temper = who.Temper, Woman = who.Woman, WellOff = who.WellOff, Drunk = who.Drunk, Known = known });
            }
            _engine.Nearby = list;
            _engine.NearbyAt = _store.LastGameMin;   // the same clock ThoughtDay reads (Bridge.Now() runs ahead of it)
        }

        /// What someone this far off can see of the player, for their thought about him.
        private static InnerLife.PlayerLook Look(Ped me, float dist)
        {
            return new InnerLife.PlayerLook { Near = dist < 10f, Armed = WEAPON.IS_PED_ARMED(me.Handle, 4), Mounted = PED.IS_PED_ON_MOUNT(me.Handle) };
        }

        /// A person's thought just came up on the ticker: if they're within a few steps, a small wordless sound (a breath,
        /// a "hm"), at most once every three minutes each. The words stay on the ticker; nothing is said aloud.
        private void ThoughtSound(TickerLine line, int now)
        {
            if (!_cfg.ThoughtSounds || line == null || line.Kind != "near" || line.Key == null)
                return;
            NearThought n = _near.FirstOrDefault(x => x.Key == line.Key);
            if (n == null || n.Handle == 0 || !ENTITY.DOES_ENTITY_EXIST(n.Handle) || ENTITY.IS_ENTITY_DEAD(n.Handle) || Speech.Busy(n.Handle))
                return;
            if (new Ped(n.Handle).Position.DistanceTo(Game.Player.Character.Position) > 8f)
                return;
            int at;
            if (_thoughtSoundAt.TryGetValue(line.Key, out at) && now - at < 180000 && now >= at)
                return;
            if (_thoughtSoundAt.Count > 200)
                foreach (string k in _thoughtSoundAt.Where(kv => now - kv.Value > 180000 || now < kv.Value).Select(kv => kv.Key).ToList())
                    _thoughtSoundAt.Remove(k);
            _thoughtSoundAt[line.Key] = now;
            Speech.SayAny(n.Handle, Speech.Hum);
        }

        /// A story mission started: give back everyone the add-on borrowed or made, and keep out of the way until
        /// it's over. The ticker, deeds and diaries carry on.
        private void MissionPause(int now)
        {
            if (_missionPaused)
                return;
            _missionPaused = true;
            _director.AbortAll("a mission started");
            EndEncounters("a mission started");
            BelowAbort("a mission started");
            PatientAbort("a mission started");
            WakeActsAbort();
            AnimTestEnd("a mission started");
            GunshotTestEnd("a mission started");
            TrystAbort();
            PassingAbort();
            TestCastClear("a mission started");
            WanderLaterClear();
            SightsAbort();
            GrudgesAbort();
            ThanksAbort();
            FriendAbort();
            LonelyAbort();
            EscortAbort();
            MeetAbort();
            EventsAbort("a mission began");
            FavourAbort();
            ChanceWaitAbort("a mission started");
            WarnAbort();
            KinAbort();
            SoberVoices(true);
            if (_rough != null)
                RoughEnd(_rough, "stopped: a mission started", now);
            _glitch = null;
            if (_stalker != null)
            {
                Fallen f = _stalker.Revenant;
                _stalker.Finish(false);
                if (f != null && !f.Done)
                    f.ReturnAt = Bridge.Now() + 240;
                RevenantReset();
                _stalker = null;
                _stalkerRecord = false;
                _stalkerPendingUntil = 0;
                _stalkerCooldown = now + 120000;
            }
            if (_wedding != null)
                EndWeddingRun("a mission started");
            _plea = null;
            if (_theft != null)
                ChaseLetGo(_theft, now);   // a chaser let go with the theft (the DEEPEN review)
            _theft = null;
            _arrival = null;
            _later.Clear();
            Log.Write("A mission started: scenes, stalkers and ceremonies wait until it's over.");
        }

        // ---- the order things run in ------------------------------------------------------------------

        private void Tick04(int now)
        {
            _now = now;
            Log.Tick(now);   // what's waiting to be written, a couple of times a second
            Natives.YieldToDying = Speech.YieldToDying = _cfg != null && _cfg.YieldToDying;   // the rule for the dying, read each tick ([World] YieldToDying; item 27)
            Safe("settings reload", _aIni);
            Safe("places", _aPlaces);
            TranslationStep(now);   // the player's translation.txt read, and read again when it changes (WestworldScript.Translation.cs; Run 3, item 8)
            WorldQuietStep(now);   // whether a body's looks may be read now (WestworldScript.WorldQuiet.cs; Run 3, item 5), before a test row reads them
            TestingStep(now);   // a test asked for by its key or the panel (refused in a mission, with why)
            ProofTourStep(now);   // the proof tour's next row, when one is walking (WestworldScript.ProofTour.cs; DEPTH item 2): before the mission's return, so it sees the mission and pauses
            if (MISC.GET_MISSION_FLAG())
            {
                Safe("mission pause", _aMission);
                StoryHold(now);   // the storyteller holds every wait as built in a mission (WestworldScript.Story.cs)
                return;
            }
            if (_missionPaused)
            {
                _missionPaused = false;
                Log.Detail("The mission is over: scenes, stalkers and ceremonies carry on.");
            }
            Safe("lines", _aLater);
            LedgerStep(now);   // deaths are noted whether or not the player has control (not in story missions)
            BodyClearFrameStep();   // a lawman's carry: its reset flags every frame, the controls off too (WestworldScript.Bodies.cs)
            AimLetGoStep();   // whoever you aim at is let go of whatever of ours held them (WestworldScript.AimLetGo.cs)
            DeedEyesStep(now);   // who saw a killing is still alive to tell it (WestworldScript.Watch.cs)
            RecordFatesStep(now);   // a record's death RealNPCs caused, written into its record (WestworldScript.Directing.cs)
            TrystWatchdog(now);   // never left dark (outside the trysts' own guard)
            TrystStep(now);       // runs with the player's controls off while it's dark
            SupperInTheDark();    // so does the supper invitation's dark, and its watchdog (WestworldScript.Supper.cs)
            HookupsStep(now);     // CasualHookups' scene may take the controls too (WestworldScript.Hookups.cs)
            if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return;
            bool own = _cfg == null || !_cfg.LookScheduler;   // the background looks: each on its own timer every frame (off), or the scheduler's (WestworldScript.Looks.cs; DEPTH item 4)
            StoryStep(now);   // the storyteller: the street's, the walk-ups' and the encounters' waits by how long it's been quiet near you (WestworldScript.Story.cs)
            QuietStep(now);
            PostsStep(now);
            RegularsStep(now);
            EncountersStep(now);
            TalkStep(now);
            SubtitleStep(now);
            if (own) TerritoryStep(now);
            FlirtPromptStep();
            TalkPromptStep();     // Talk in the lock-on menu, and the interaction menu (WestworldScript.Menu.cs)
            BodiesStep(_now);     // a lawman carries a body off the street (WestworldScript.Bodies.cs)
            ForgetPromptStep();   // Reset RealNPCs Memory in a station clerk's list (WestworldScript.Forget.cs)
            RoughStep(now);
            Safe("pleading", _aPlead);
            Safe("pickpockets", _aTheft);
            Safe("arrivals", _aArrival);
            Safe("hosts", _aHosts);
            Safe("stalker", _aStalker);
            Safe("voices", _aVoices);
            Safe("awakening", _aWake);
            Safe("wakeacts", _aWakeActs);
            WakeVoiceStep();   // the waking say their own lines oddly (WestworldScript.WakeVoice.cs)
            AnimTestStep();    // the Testing page's animation rows, while one runs (WestworldScript.AnimTest.cs)
            GunshotTestStep(); // and its gunshot row (WestworldScript.Gunshot.cs)
            Safe("scenes", _aDrama);
            Safe("court thoughts", _aCourtThoughts);
            PassingStep(now);   // people who know each other acknowledge it as they pass (WestworldScript.Passing.cs)
            if (own) TaleStep(now);      // the town's tales in the thoughts of those near you (WestworldScript.Tales.cs; FOURTEENHOUR item 24)
            FistLook(now);      // the town's best fist thinks of it near you, once a day (WestworldScript.BestFist.cs; reserve R7)
            CensusStep(now);    // who holds the people near you, a CONTROL line a minute (WestworldScript.Census.cs; FOURTEENHOUR item 22)
            HandBackStep(now);  // what we set on people to make them fight, put back once they're done (WestworldScript.HandBack.cs; FOURTEENHOUR item 3)
            SightsStep(now);    // people notice how you look (WestworldScript.Sights.cs)
            StreetStep(now);    // the street reacts to other people's fights, and to who walks among them (WestworldScript.Street.cs)
            LettersStep(now);   // a letter's writer waiting to meet you (WestworldScript.Letters.cs)
            SpotsStep(now);   // the spot key's request (WestworldScript.Spots.cs)
            TrainsStep(now);   // a train at the platform: who gets off and who gets on (WestworldScript.Trains.cs)
            LoopStep(now);   // the waking hear their habit as a voice (WestworldScript.Loop.cs)
            GhostsStep(now);   // the reload noticed: a deja vu for the undone (WestworldScript.Ghosts.cs)
            TownEventsStep(now);   // the town's day: a dance staged when you're there (WestworldScript.TownEvents.cs)
            SpaceStep(now);     // and mind you standing too close (WestworldScript.Space.cs)
            GrudgesStep(now);   // someone grieving one you killed (WestworldScript.Grudges.cs)
            KinGrudgesStep(now); // the kin who come about a wrong of yours (WestworldScript.KinGrudges.cs)
            if (own) CriminalsStep(now);  // the criminal class: a thief gone straight thinks of it (WestworldScript.Criminals.cs)
            if (own) EpilogueStep(now);   // the epilogue: one who remembers a man called Morgan (WestworldScript.Epilogue.cs)
            if (own) PlaceMemoryStep(now); // places hold memories; known by your horse and your habits (WestworldScript.PlaceMemory.cs)
            WagonProbeStep(now);  // the wagon wheel probe's readings, while one is going (WestworldScript.Wagon.cs; item 26)
            if (own) NoticeBoardStep(now); // into a town: what it has posted, in the box (WestworldScript.NoticeBoard.cs; FIVEDAY item 16)
            if (own) RegardStep(now);      // the town's regard of you in the "Around town" box (WestworldScript.Regard.cs)
            Loads.Sweep(now);     // a body asked for and never used goes back to the game (Game/Loads.cs)
            PerformStep(now);     // whoever is playing a row of the catalogue (WestworldScript.Perform.cs)
            ComposedStep(now);    // and whoever is at two or three in order (WestworldScript.Composed.cs)
            ThanksStep(now);    // someone you helped comes over to thank you (WestworldScript.Thanks.cs)
            if (own) FenceStep(now);         // what you saw of the fence cutters, and the town's word (WestworldScript.FenceCutters.cs; FIVEDAY item 31)
            if (own) PosseStep(now);         // the town whose posse is out thinks of the road (WestworldScript.Posse.cs; FIVEDAY R32)
            PosseProbeStep(now);    // the Testing row's mounted probe, when one runs
            WantedStep(now);        // a resident on a wanted poster, recognised (WestworldScript.Wanted.cs; FIVEDAY R31)
            RewardStep(now);        // a reward posted for a neighbour (WestworldScript.Reward.cs; FIVEDAY R30)
            if (own) HoboStep(now);          // the chalk on the water tower by the station (WestworldScript.DriftPoster.cs; FIVEDAY R29)
            if (own) RelicStep(now);         // 1907: the photographer's window of old outlaws (WestworldScript.DriftPoster.cs; FIVEDAY R26)
            if (own) EditorStep(now);        // the readers of an editorial on you (WestworldScript.DriftPoster.cs; FIVEDAY R14)
            if (own) PosterStep(now);        // the bill outside the sheriff's, as the rumour has it (WestworldScript.DriftPoster.cs; FIVEDAY R13)
            if (own) NovelStep(now);         // people near you who've read the dime novel (WestworldScript.DimeNovel.cs; FIVEDAY R12)
            if (own) DryStep(now);           // 1907: a town dry by local option (WestworldScript.Fever.cs; FIVEDAY R27)
            if (own) LettersHomeStep(now);   // a letter from one who left (WestworldScript.Roundup.cs; TENHOUR R7)
            if (own) RoundupStep(now);       // the hands paid off after the roundup (WestworldScript.Roundup.cs; TENHOUR R1)
            if (own) TownRivalStep(now);     // what this town thinks of its rival (WestworldScript.Roundup.cs; TENHOUR R4)
            if (own) ColdSnapStep(now);      // the high country's cold spells (WestworldScript.ColdSnap.cs; TENHOUR R3)
            if (own) GrindStep(now);         // the grinding season's thoughts (WestworldScript.Fever.cs; FIVEDAY R23)
            if (own) FeverStep(now);         // fever in town: the thoughts near you (WestworldScript.Fever.cs; FIVEDAY R9)
            BalladStep(now);        // a murder ballad from the next fire (WestworldScript.MurderBallad.cs; FIVEDAY R20)
            QuiltStep(now);         // the quilting bee overheard by the church (WestworldScript.QuiltingBee.cs; FIVEDAY R8)
            ForeclosureStep(now);   // the sheriff's sale when the bank takes a place (WestworldScript.Foreclosure.cs; FIVEDAY R3)
            if (own) KeeperBackStep(now);    // a keeper shot down, seen back (WestworldScript.KeeperBack.cs, THREEHOUR R3)
            if (own) PlacesMournedStep(now);   // where someone died, those close to them stop (WestworldScript.PlacesMourned.cs, THREEHOUR item 5)
            WalkHomeStep(now);      // J after dark: see someone home (WestworldScript.WalkHome.cs, THREEHOUR item 4)
            WalkTogetherStep(now);  // two friends walking on together (WestworldScript.WalkTogether.cs; FOURTEENHOUR item 15)
            if (own) PaydayNightStep(now);   // Saturday night: the town's word that the hands are paid (WestworldScript.BrawlAfter.cs; FOURTEENHOUR item 13)
            JailDoorStep(now);      // the morning after a brawl the law broke up: his wife at the jail door (WestworldScript.BrawlAfter.cs; FOURTEENHOUR item 12)
            VouchStep(now);         // a man you vouched for: his week, and its end told in his town (WestworldScript.Vouch.cs; FOURTEENHOUR reserve R22)
            if (own) MinersMeetStep(now);   // the miners' meeting at Annesburg (WestworldScript.MinersMeet.cs; DEPTH reserve R7)
            if (own) YourRewardStep(now);   // J at the jail door: a reward for a thief (WestworldScript.YourReward.cs; DEPTH reserve R4)
            if (own) SlanderStep(now);   // J in a saloon: a word in a talker's ear (WestworldScript.Slander.cs; DEPTH reserve R3)
            if (own) NoteMeetStep(now);   // two a word went between meet (WestworldScript.Errand.cs; DEPTH reserve R2)
            if (own) FamiliarityStep(now);   // two seen about the same place get acquainted (WestworldScript.Familiarity.cs; DEPTH item 17)
            if (own) SendWordStep(now);   // a word carried for a dime (WestworldScript.SendWord.cs; DEPTH item 10)
            if (own) BrandsStep(now);   // J to tell a ranch man who really ran his brands (WestworldScript.Brands.cs; DEPTH item 8)
            if (own) StepBetweenStep(now);   // J to step between two about to quarrel (WestworldScript.StepBetween.cs; DEPTH item 7; the scheduler's look with LookScheduler on)
            BrawlVerbsStep(now);    // J in a fight near you: break it up, stand with the man losing, the bet (WestworldScript.BrawlVerbs.cs; FOURTEENHOUR item 11)
            BarRoundStep(now);      // J at a saloon's bar: the house a round (WestworldScript.BarRound.cs, THREEHOUR item 3)
            SlateStep(now);         // credit at the store's counter (WestworldScript.Slate.cs; FIVEDAY R2)
            DrummerStep(now);       // a drummer calls on the store's keeper (WestworldScript.Drummers.cs; FIVEDAY R1)
            SaleDayStep(now);       // Tuesday's sale at the Valentine stockyards (WestworldScript.SaleDay.cs; FIVEDAY item 30)
            ShapeUpStep(now);       // the dawn shape-up on the Saint Denis waterfront (WestworldScript.ShapeUp.cs; FIVEDAY item 29)
            MineStep(now);          // three whistles at Annesburg, staged when you're there (WestworldScript.MineWhistle.cs; FIVEDAY item 28)
            if (own) WakeNightStep(now);     // the night before a burial: they sit up with the dead (WestworldScript.WakeNight.cs; FIVEDAY item 27)
            MourningStep(now);      // mourning: quieter thoughts, hushed neighbours, the grave on a Sunday afternoon (WestworldScript.Mourning.cs; FIVEDAY item 26)
            if (own) SundayStep(now);        // a Sunday by the church: the hymn, the sermon talked over, dinner on the grounds (WestworldScript.Sunday.cs; FIVEDAY item 25)
            if (own) MarksStep(now);         // the marks of a beating of yours, worn for days (WestworldScript.Marks.cs; FIVEDAY item 24)
            if (own) SurvivorsStep(now);     // a friend of one you beat thinks of what it did to them (WestworldScript.Survivors.cs; FIVEDAY item 21)
            PinkertonsStep(now);    // a Pinkerton man asks the town after you (WestworldScript.Pinkertons.cs; FIVEDAY item 15)
            FriendCallsStep(now);   // a friend comes over unasked: a warning, news, a gift, a favour (WestworldScript.FriendCalls.cs; Run 3, item 7)
            LonelyStep(now);    // someone lonely who knows you comes over (WestworldScript.Lonely.cs)
            FavourStep(now);    // someone in trouble comes to ask a favour (WestworldScript.Favours.cs)
            ChanceWaitStep(now);   // a chance meeting waiting on the model's pick (WestworldScript.ChanceMinds.cs)
            WarnStep(now);      // someone who saw what you did, or knows who's after you (WestworldScript.Warnings.cs)
            EscortStep(now);    // someone you're seeing home (WestworldScript.Escort.cs)
            ErrandStep(now);    // a word you're carrying (WestworldScript.Errand.cs)
            BelowStep(now);     // the voice below the saloon floor, late at night (WestworldScript.Below.cs)
            if (own) CalendarStep(now);  // a holiday's word of the day, in the morning (WestworldScript.Calendar.cs)
            MindShadowStep(now);   // the mind in shadow mode: traits, needs, feelings and mood, reported only (WestworldScript.Mind.cs)
            LanguageStep(now);     // the language layer's worker and the engine's hand (WestworldScript.Language.cs; its own try/catch)
            Safe("voice", () => VoiceStep(now));   // voiced thoughts and dialogue: the worker and where each speaker is (WestworldScript.Voiced.cs)
            TestCastStep(now);     // the Testing page's test cast, while they're about (WestworldScript.TestCast.cs)
            if (!own)
                LooksStep(now);    // the background looks above, at most two a frame, the most overdue first (WestworldScript.Looks.cs; DEPTH item 4)
            if (_drinkVoice != 0 || _drunkVoices.Count > 0)
                Safe("vices", _aDrinkVoice ?? (_aDrinkVoice = () => { DrinkVoiceStep(_now); SoberVoices(false); }));   // a drinker's voice, and sober again at 4 (WestworldScript.Vices.cs)
            Safe("ceremonies", _aCeremony);
            Safe("kin", _aCast);
            Safe("pickups", _aPickups);
        }
    }
}
