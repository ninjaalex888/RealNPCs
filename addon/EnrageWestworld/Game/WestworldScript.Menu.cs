// The interaction menu, the game side (NIGHT3 item 4; Core/Interact.cs holds the choosing and the words). Locked on to an
// adult RealNPCs knows (a resident, one of enRAGE's townsfolk or records), the game's lock-on menu also offers "Talk" on
// the game's own INPUT_INTERACT_LOCKON_TARGET_INFO control (Q on the keyboard, RB on a pad), with its own icon. [World]
// TalkKey = Q (the default; Lex, 2026-10-31, after a day on T) means exactly that control: a player who rebinds it in the
// game's Settings > Controls (to T, or anything) gets that key and its icon. Any other TalkKey is the direct-read path: no
// game control has T as its keyboard key (femga's Controls list goes from Space to Tab to U), so RealNPCs reads that key
// itself (KeyDown04) while the prompt is up; the prompt still shows the control's icon, reads "Talk (T)" on the keyboard,
// and the control's own press does nothing there; that path alone stands aside on the keyboard next to a station or
// telegraph clerk (T is enRAGE's telegraph key). TalkKey = None: no keyboard prompt, a pad's RB only. Either way Talk stands
// aside while the clerk's Reset prompt (the same control) is up for the same person. Pressing it opens a short list above the ticker: up and
// down (or the D-pad, or the mouse wheel) to choose, Space or Enter (A) to say it, Backspace (B) to leave; the keyboard's keys while the
// keyboard is in use, the pad's buttons while a pad is. Arthur's words show as a subtitle, then their answer in their
// own voice with its subtitle (afraid or hostile when they are), and what it is (a round, charity, a warning) is written
// down as a happening for the mind, so gratitude, fear, the greetings and the memories follow; the money goes when the
// answer comes. The person stops and turns to face him while the list is open (someone on a scenario's walk is asked out
// of it Rockstar's way; one who walks on anyway is asked again; Lex, 2026-11-03: they kept walking and the list closed).
// It closes on a weapon drawn, 3 m walked, the two of them 5 m apart, combat, a mission, a cutscene, a scene taking
// them, or 25 s with nothing chosen. Nobody busy with
// anything else (a scene, a talk, another script, enRAGE's own business) is offered. [World] InteractionMenu. Testing
// row "Talk to the nearest person you know".
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class TalkMenu
        {
            public int Handle;
            public string Id, Name, First, Temper, Worry, Trade, FolkId, Regard, Rumour, Self, AskWorry;   // AskWorry: this run's worry at any stage (asked after even once it's over)
            public long WorryArc;   // Self: their own whole name, when they have one (the friendly men give it back)
            public int WorryStage;  // how far along the worry is (a fever past its crisis can't be mended)
            public bool Woman, Devout, MoneyWorry, Seated, KeepOrders, KnowsName;
            public string Condole;     // the first name of a friend they grieve (condolences can be offered: Core/Fondness.cs), or null
            public bool YouKilledKnown;   // ...and they know he's the one who killed them
            public int CoverDollars;      // a creditor pressing someone in a debt scene: what the stranger can cover (Core/Fondness.cs); 0 otherwise
            public int Debtor;            // ...the one pressed (a handle)
            public bool Drunk;            // in drink (InHisCups)
            public bool EscortOffer;      // "Let me see you home." is on the list (after dark in a town: a woman, the drunk, the timid; or the Testing row)
            public SceneTie ErrandTie;    // a quarrel of theirs a word could mend (Fondness.ErrandTie): the errand's row shows
            public bool ErrandOtherWoman;
            public bool Deliver;          // they're the one a carried word is for: the deliver row shows first
            public bool Performer;        // at the piano, the guitar, the banjo: the list is a coin for the music
            public Person P;           // who they are to the mind, for their worry's words when he asks after it
            public float Reach;
            public List<InteractOption> Options;
            public int Sel, OpenedAt, LastAt, AnswerAt, ThoughtAt, CloseAt, FaceAt, HeldAt;   // HeldAt: when they were last asked to stand and face him
            public Vector3 From;
            public InteractAnswer Pending;
            public string PendingId, PendingLabel;
            public string TalkKey;     // the model's answer asked for (LanguageTalk), or null
            public LanguageRequest TalkRequest;
            public int TalkAskedAt;
            public string Title;
            public string[] Rows;     // the list as drawn, kept until the choice or the options change
            public float Wide;
            public int RowsSel = -1;
            public bool RowsPending;
            public bool Pad, RowsPad;  // a pad in use this frame (the hint under the list names its buttons), and as the rows were made
            public string Foot;        // the hint under the list: which key says it and which leaves
        }

        private const uint TalkControl = 0x31219490;   // INPUT_INTERACT_LOCKON_TARGET_INFO: Q on the keyboard as the game comes (rebindable in its Controls menu), a pad's RB
        private const uint MenuUp = 1662638961u, MenuDown = 97156178u, MenuAccept = 3350541322u, MenuCancel = 359624985u;   // the settings panel's pad buttons
        private const uint InputJump = 0xD9D0E1C0;   // INPUT_JUMP: Space says the chosen line, so Arthur doesn't jump with it
        // the mouse wheel, as the settings panel reads it: the cursor's scroll, then the weapon and menu-scroll controls it is during play
        private static readonly uint[] WheelUps = { 1652558994u, 0x81457A1Au, 0xCC1075A7u, 0xF78D7337u }, WheelDowns = { 2346611779u, 0x9DA42644u, 0xFD0F0C2Cu, 0xD0842EDFu };
        private static readonly uint[] MenuButtons = { MenuUp, MenuDown, MenuAccept, MenuCancel, InputJump,
                                                        1652558994u, 0x81457A1Au, 0xCC1075A7u, 0xF78D7337u, 2346611779u, 0x9DA42644u, 0xFD0F0C2Cu, 0xD0842EDFu };   // the wheel too: it moves the list, not Arthur's weapon

        private static bool WheelMoved(uint[] controls)
        {
            foreach (uint c in controls)
                if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, c))
                    return true;
            return false;
        }
        private const float MenuReach = 4f;
        private static IntPtr _talkWord, _talkWordKey;
        private static string _talkWordKeyFor;
        private TalkMenu _menu;
        private int _talkPrompt, _talkPromptFor, _talkPromptGroup, _talkOpened, _talkSaid, _talkSettled;
        private string _talkPromptWords;   // the prompt as made: the key in "Talk (T)" for a key read directly on the keyboard, else null ("Talk")
        private bool _talkKeyRequested, _talkKeyNoted;   // a direct-read TalkKey pressed while the prompt was up (KeyDown04); the key's log line said
        private string _talkKeyNotedFor;   // the TalkKey that line was for (said again when it changes)
        private readonly HashSet<string> _talkConfided = new HashSet<string>();   // who has told him their worry this session (a money worry he knows of: the gift is five dollars)
        private readonly bool[] _talkKeys = new bool[13];
        private string _talkLast;
        private Action _aTalkPrompt;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void TalkPromptStep()
        {
            if (_aTalkPrompt == null)
                _aTalkPrompt = TalkPromptTick;
            if (_cfg.InteractionMenu && (!_talkKeyNoted || _talkKeyNotedFor != _cfg.TalkKey))
            {
                _talkKeyNoted = true;
                _talkKeyNotedFor = _cfg.TalkKey;
                string k = TalkKeyName();
                Log.Write(TalkOnControl() ? "TALK key: the game's lock-on target-info control (INPUT_INTERACT_LOCKON_TARGET_INFO: Q as the game comes, a pad's RB), its own icon; rebind that control in the game's Settings > Controls for another key"
                          : k == null ? "TALK key: none on the keyboard ([World] TalkKey None); a pad's RB on INPUT_INTERACT_LOCKON_TARGET_INFO only"
                          : "TALK key: " + k + " read directly by RealNPCs (TalkKey isn't Q); the prompt reads \"Talk (" + k + ")\" beside the control's own icon, which a press of that control does nothing for; a pad's RB as ever");
            }
            if (_cfg.InteractionMenu || _talkPrompt != 0 || _menu != null)
                Safe("talk menu", _aTalkPrompt);
            _talkKeyRequested = false;   // a press is for this frame's prompt only
        }

        /// TalkKey as the prompt names it ("T"), or null with none set.
        private string TalkKeyName()
        {
            string k = _cfg != null ? (_cfg.TalkKey ?? "").Trim() : "";
            return k.Length == 0 || k.Equals("None", StringComparison.OrdinalIgnoreCase) ? null : k;
        }

        /// TalkKey = Q (the default): Talk is the game's own control itself, on whatever key the game's Controls menu has it
        /// (Q as it comes), with its icon. Any other key is read directly (KeyDown04).
        private bool TalkOnControl()
        {
            string k = TalkKeyName();
            return k != null && k.Equals("Q", StringComparison.OrdinalIgnoreCase);
        }

        /// A station or telegraph clerk within 3.5 m of Arthur: enRAGE's telegraph opens on T there, so Talk's T stands aside.
        private bool TelegraphClerkNear(Ped me)
        {
            if (_now - _clerkNearAt >= 0 && _now - _clerkNearAt < 750)
                return _clerkNear;   // looked a moment ago: the pool isn't walked every frame
            _clerkNearAt = _now;
            _clerkNear = false;
            Vector3 at = me.Position;
            foreach (Ped p in Peds.All())
                if (p != null && p.Handle != me.Handle && p.Exists() && !p.IsDead && p.Position.DistanceTo(at) < 3.5f && StationClerk(p))
                {
                    _clerkNear = true;
                    break;
                }
            return _clerkNear;
        }
        private int _clerkNearAt = -100000;
        private bool _clerkNear;
        private int _clerkNotedFor;

        private unsafe void TalkPromptTick()
        {
            if (_menu != null)
            {
                TalkPromptEnd();   // the list is open: no prompt under it
                if (PanelOpen || _reader.Open)
                    TalkClose("a panel opened");   // the frame isn't drawn under a panel: nothing held while it's up
                return;
            }
            Ped me = Game.Player.Character;
            int target = 0;
            if (_cfg.InteractionMenu && !PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            string why;
            if (target != 0 && target == _talkWhyFor && _now - _talkWhyAt >= 0 && _now - _talkWhyAt < 250)
                why = _talkWhyLast;   // the same target a quarter second ago: the same answer (the cost audit: ~25 natives and reflection a frame)
            else
            {
                why = target != 0 ? TalkWhyNot(target, me, MenuReach) : "nobody";
                _talkWhyFor = target;
                _talkWhyAt = _now;
                _talkWhyLast = why;
            }
            if (why != null || (_forgetPrompt != 0 && _forgetPromptFor == target))
            {
                TalkPromptEnd();
                return;
            }
            bool keyboard = PAD.IS_USING_KEYBOARD_AND_MOUSE(0);
            string key = TalkKeyName();
            // TalkKey = Q (the default): the control itself, keyboard or pad, whatever key the game's Controls menu gives it
            // and with its own icon; no standing aside by a clerk (that was only for a read T, enRAGE's telegraph key)
            bool onControl = TalkOnControl();
            if (keyboard && !onControl && (key == null || TelegraphClerkNear(me)))
            {
                // the direct-read path on the keyboard: no key set, or a clerk beside him whose T is enRAGE's telegraph
                if (key != null && _clerkNotedFor != target)
                {
                    _clerkNotedFor = target;
                    Log.Write("TALK stands aside: a station or telegraph clerk is within 3.5 m (" + key + ", read directly, is enRAGE's telegraph there)");
                }
                TalkPromptEnd();
                return;
            }
            string words = keyboard && !onControl ? key : null;   // "Talk (T)" only for a key read directly; else "Talk" by the control's icon
            int group = HUD._UI_PROMPT_GET_GROUP_ID_FOR_TARGET_ENTITY(target);
            if (_talkPrompt == 0 || !HUD._UI_PROMPT_IS_VALID(_talkPrompt) || _talkPromptFor != target || _talkPromptGroup != group || _talkPromptWords != words)
            {
                TalkPromptEnd();
                _talkPrompt = NewTalkPrompt(group, words);   // made again when its words change (keyboard or pad, or TalkKey)
                _talkPromptFor = target;
                _talkPromptGroup = group;
                _talkPromptWords = words;
            }
            bool completed = HUD._UI_PROMPT_HAS_STANDARD_MODE_COMPLETED(_talkPrompt, 0);
            if (keyboard && !onControl && completed && !_talkKeyRequested)
            {
                // the direct-read path: the control's own key on the keyboard isn't Talk's key, so the prompt is made afresh
                // and nothing opens
                TalkPromptEnd();
                return;
            }
            bool byKey = keyboard && !onControl && _talkKeyRequested;
            _talkKeyRequested = false;
            if (completed || byKey)
            {
                TalkPromptEnd();
                _talkWhyFor = 0;
                if (TalkWhyNot(target, me, MenuReach) == null)   // asked afresh: the cached answer may be a quarter second old
                    TalkOpen(new Ped(target), byKey ? key + ", read directly" : keyboard ? "the Talk control (Q unless rebound)" : "the Talk prompt (RB)", MenuReach);
            }
        }

        private int _talkWhyFor, _talkWhyAt;
        private string _talkWhyLast;

        /// Why this person can't be talked to through the menu now, or null: within `reach` metres, and not busy with
        /// anything else (MayDirect's rules, less the setting that keeps enRAGE's people out of our scenes: this is
        /// only a word with them).
        private string TalkWhyNot(int target, Ped me, float reach)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target) || !PED.IS_PED_HUMAN(target) || PED.IS_PED_A_PLAYER(target) || ENTITY.IS_ENTITY_DEAD(target))
                return "not someone to talk to";
            Ped p = new Ped(target);
            bool performer = _cfg.FondnessActs && PerformerAt(target);   // at the piano, the guitar, the banjo: a coin for the music, whoever runs them (WestworldScript.Fondness.cs)
            if (!IsGrownUp(p) || (!performer && Natives.StoryModel(p)))
                return "a child, or one of the story's own";
            if (p.Position.DistanceTo(me.Position) > reach)
                return "too far";
            bool record = IsRecord(target);
            int dollars, debtor;
            bool cover = DebtCoverable(target, out dollars, out debtor);   // the creditor of a debt scene: the one word with him (WestworldScript.Fondness.cs)
            if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(target, 0) || PED.IS_PED_FLEEING(target) || PED.IS_PED_RAGDOLL(target) || PED.IS_PED_ON_MOUNT(target) ||
                PED.IS_PED_IN_ANY_VEHICLE(target, false) || (!cover && !performer && (_director.Involves(target) || (NotFree(target) && !LonelyWaiting(target) && !FavourWaiting(target)) || Spawner.IsOurs(target))) ||   // the lonely one waits for exactly this
                (record ? _bridge.EnrageFighter(target) || _bridge.Interrogating : !performer && Natives.OtherScripts(target)))
                return "busy";
            string id, name, temper;
            if ((!LastingId(p, out id, out name, out temper) || id == null) && !performer)
                return "nobody RealNPCs knows";
            return null;
        }

        /// The Talk prompt in this target's group; `key` names the keyboard key in its words ("Talk (T)"), null for a pad's "Talk".
        private static int NewTalkPrompt(int group, string key)
        {
            if (_literalTag == IntPtr.Zero)
                _literalTag = Kept("LITERAL_STRING");
            if (_talkWord == IntPtr.Zero)
                _talkWord = Kept("Talk");
            if (key != null && (_talkWordKey == IntPtr.Zero || _talkWordKeyFor != key))
            {
                _talkWordKey = Kept("Talk (" + key + ")");   // kept for the session, like the rest (a key changed in the ini makes one more)
                _talkWordKeyFor = key;
            }
            IntPtr text = key != null ? _talkWordKey : _talkWord;
            int prompt = HUD._UI_PROMPT_REGISTER_BEGIN();
            HUD._UI_PROMPT_SET_CONTROL_ACTION(prompt, TalkControl);
            ulong word = Function.Call<ulong>(VarText, 10, new InputArgument((ulong)_literalTag.ToInt64()), new InputArgument((ulong)text.ToInt64()));
            Function.Call(SetPromptText, prompt, new InputArgument(word));
            HUD._UI_PROMPT_SET_STANDARD_MODE(prompt, true);
            HUD._UI_PROMPT_SET_GROUP(prompt, group, 0);
            HUD._UI_PROMPT_REGISTER_END(prompt);
            HUD._UI_PROMPT_SET_ENABLED(prompt, true);
            HUD._UI_PROMPT_SET_VISIBLE(prompt, true);
            return prompt;
        }

        private void TalkPromptEnd()
        {
            if (_talkPrompt != 0)
            {
                if (HUD._UI_PROMPT_IS_VALID(_talkPrompt))
                    HUD._UI_PROMPT_DELETE(_talkPrompt);
                _talkPrompt = 0;
            }
            _talkPromptFor = 0;
        }

        /// Opens the list for this person: who they are, how they stand with him, what's on their mind, what's on offer.
        private string TalkOpen(Ped p, string how, float reach)
        {
            string id, name, temper;
            if (p == null || !p.Exists())
                return "nobody RealNPCs knows";
            bool performer = _cfg.FondnessActs && PerformerAt(p.Handle);
            if ((!LastingId(p, out id, out name, out temper) || id == null) && !(performer && PerformerIdentity(p, out id, out name, out temper)))
            {
                if (!performer)
                    return "nobody RealNPCs knows";
                // a performer the game keeps as its own (a unique model): a coin for the music, nothing remembered; the list alone
                int ph = p.Handle, pnow = Game.GameTime;
                string pname = Fondness.PerformerName(PerformerScenario(ph));
                _menu = new TalkMenu { Handle = ph, Id = null, Name = pname, First = pname, Performer = true, Seated = true, Reach = reach, OpenedAt = pnow, LastAt = pnow, From = Game.Player.Character.Position, Woman = !PED.IS_PED_MALE(ph) };
                TalkRefreshOptions();
                TASK.TASK_LOOK_AT_ENTITY(ph, Game.Player.Character.Handle, 8000, 0, 51, 0);
                _talkOpened++;
                Log.Write("TALK menu opened on " + pname + " (a performer the game keeps for itself, at the music; " + how + ")");
                return null;
            }
            int h = p.Handle, now = Game.GameTime;
            Ped me = Game.Player.Character;
            MindWho w = MindKnown(h, now);
            string worry = null, askWorry = null;
            int worryStage = 0;
            if (w != null && w.P != null)
            {
                string kind;
                int stage;
                bool good;
                if (InnerLife.WorryNow(w.P, _store.LastGameMin / 1440, out kind, out stage, out good))
                {
                    askWorry = kind;   // how did it turn out? still worth the asking
                    if (stage <= 2)
                        worry = kind;   // while it lasts: once it's come out one way or the other (stage 3) it's no worry now
                    worryStage = stage;
                }
            }
            if (w != null && w.Traits == null && w.Src != null)
                w.Traits = _engine.Shadow.TraitsOf(w.Src);
            FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(h) : null;
            List<string> pool = _engine.RumourPool(_placeTown, 3);
            string condole = null;
            bool youKilledKnown = false;
            if (_cfg.FondnessActs)
            {
                string deadId;
                condole = _engine.GrievesFor(id, Fondness.CondoleDays, out deadId);   // a friend they grieve: condolences can be offered (Core/Fondness.cs)
                if (condole != null && deadId != null)
                {
                    DeathEntry dead = _store.DeathOf(deadId);
                    string knownId;
                    youKilledKnown = dead != null && dead.By == "player" && (_engine.YourKillGrieved(id, Fondness.CondoleDays, out knownId) != null || _engine.Grudged(id, deadId));   // the killing is laid at his door
                }
            }
            int coverDollars = 0, debtor = 0;
            if (_cfg.FondnessActs)
                DebtCoverable(h, out coverDollars, out debtor);   // a creditor pressing someone: the list is the one word with him
            bool woman = !PED.IS_PED_MALE(h), seated = PED.IS_PED_USING_ANY_SCENARIO(h) && ENTITY.GET_ENTITY_SPEED(h) < 0.4f;
            bool drunk = InHisCups(p, w != null ? w.P : null);
            string regard = SightRegard(p, id);
            bool escortOffer = _cfg.FondnessActs && _escort == null && coverDollars == 0 && !performer && (_escortTest ? !seated : Fondness.EscortOffered(Natives.Night(), _placeTown != null, woman, drunk, temper, regard, seated));
            Errand carrying;
            bool deliver = _cfg.FondnessActs && id != null && ErrandFor(id, out carrying);   // the one a carried word is for (WestworldScript.Errand.cs)
            SceneTie errandTie = _cfg.FondnessActs && !performer && coverDollars == 0 && _errand == null && id != null ? Fondness.ErrandTie(_store.Ties, id, _placeTown, _store.LastGameMin, x => _store.DeathOf(x) == null) : null;
            bool errandOtherWoman = errandTie != null && Residents.IsWomansName(Person.FirstOf(errandTie.Other(id)));
            _menu = new TalkMenu
            {
                Handle = h, Id = id, Name = name ?? TalkName(p), First = name != null ? Person.FirstOf(name) : TalkName(p), Temper = temper, Worry = worry,
                Woman = woman, Devout = w != null && w.Traits != null && w.Traits[Traits.Piety] > 1.2f, MoneyWorry = worry == "debt" || worry == "savings" || worry == "bank",
                Drunk = drunk, EscortOffer = escortOffer, ErrandTie = errandTie, ErrandOtherWoman = errandOtherWoman, Deliver = deliver, Performer = performer,
                Trade = w != null && w.P != null ? w.P.Trade : null, FolkId = ac != null ? ac.Id : null, P = w != null ? w.P : null,
                Seated = seated,   // sat, leaning or stood at their point: they talk from there; a scenario's walker is stopped like anyone else
                Regard = regard, Rumour = pool.Count > 0 ? pool[Voice.Hash(id) % pool.Count] : null,   // which of the town's talk they've heard: theirs, the same each time
                Reach = reach, OpenedAt = now, LastAt = now, From = me.Position, KnowsName = _engine.KnowsYourName(id), Self = name, AskWorry = askWorry, WorryArc = InnerLife.ArcFor(id, _store.LastGameMin / 1440), WorryStage = worryStage,
                Condole = condole, YouKilledKnown = youKilledKnown, CoverDollars = coverDollars, Debtor = debtor
            };
            TalkRefreshOptions();
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);   // the game's own "you're standing too close" and the like wait till goodbye (Lex, 2026-10-31); undone in TalkClose
            PED.SET_PED_KEEP_TASK(h, true);   // the game's own ambient orders don't take them back off our turn-and-stand while the list is open; undone in TalkClose
            TalkFace(_menu, me, true);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 26000, 0, 51, 0);
            _talkOpened++;
            Log.Write("TALK menu opened on " + _menu.Name + (_menu.Temper != null ? " (" + _menu.Temper + ")" : "") + (_menu.Regard != null ? ", " + _menu.Regard + " of you" : "") +
                      (_menu.Worry != null ? ", worried about " + _menu.Worry : "") + (_menu.CoverDollars > 0 ? ", pressing someone for $" + _menu.CoverDollars : "") + " (" + how + ")");
            return null;
        }

        /// They stop, turn to face him and keep facing him (one order for the whole time, given again after each answer): a
        /// turn and then a stand-still replaced the turn at once. The first time, out of a scenario's walk Rockstar's way (the
        /// exit flag and the turn the same tick, Game/ScenarioExit.cs; a bare clear left them walking on: Lex, 2026-11-03).
        private static void TalkFace(TalkMenu t, Ped me, bool first)
        {
            t.HeldAt = Game.GameTime;
            if (t.Seated)
                return;
            int h = t.Handle, you = me.Handle;
            if (first)
                ScenarioExit.SoftUp(h, x => TASK.TASK_TURN_PED_TO_FACE_ENTITY(x, you, 26000, 0f, 0f, 0f));
            else
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, you, 26000, 0f, 0f, 0f);
        }

        private void TalkRefreshOptions()
        {
            int cash = 0;
            try
            {
                cash = MONEY._MONEY_GET_CASH_BALANCE();
            }
            catch
            {
                cash = 0;   // no reading: the money options stay greyed
            }
            if (_menu.CoverDollars > 0 || _menu.Performer)
            {
                _menu.Options = _menu.Performer ? Fondness.TipOptions(cash) : Fondness.CoverOptions(cash, _menu.CoverDollars);   // a creditor mid-scene: cover the debt, or never mind; a performer: a coin for the music
                _menu.Sel = Math.Min(_menu.Sel, _menu.Options.Count - 1);
                _menu.Rows = null;
                return;
            }
            bool told = _talkConfided.Contains(TalkToldKey(_menu));
            string selId = _menu.Options != null && _menu.Sel < _menu.Options.Count ? _menu.Options[_menu.Sel].Id : null;
            Happening wrong = Interact.LastWrong(_store.WorldLog, _menu.Id, _store.LastGameMin);
            int company = _cfg.FondnessActs && _placeKind == "saloon" ? HouseCompany(_menu.Handle) : 0;   // whiskey for the house wants a house (WestworldScript.Fondness.cs)
            _menu.Options = Interact.Options(_placeKind == "saloon", cash, _menu.MoneyWorry && told, told && Interact.Mendable(_menu.Worry, _menu.WorryStage), _menu.KnowsName ? null : YourFullName(),
                                             told && _menu.P != null ? _menu.AskWorry : null, wrong != null, wrong != null && _store.LastGameMin - wrong.At < 60, _menu.Worry,   // a worry money can mend: the debt, the doctor, a dentist, the fare
                                             house: company >= Fondness.HouseLeast, gift: _cfg.FondnessActs, woman: _menu.Woman, condoleFor: _menu.Condole, escort: _menu.EscortOffer,
                                             errandTo: _menu.ErrandTie != null ? Person.FirstOf(_menu.ErrandTie.Other(_menu.Id)) : null,
                                             deliverFrom: _menu.Deliver && _errand != null ? Person.FirstOf(_errand.FromName) : null, deliverMessage: _menu.Deliver && _errand != null ? _errand.Message : null);
            int at = selId != null ? _menu.Options.FindIndex(x => x.Id == selId) : -1;
            _menu.Sel = at >= 0 ? at : 0;   // the same option stays chosen (by index, "Watch yourself." slid under the cursor after an apology)
            _menu.Rows = null;
        }

        /// Every frame while the list is open; a fault closes it rather than failing every frame after.
        private void TalkMenuFrame(int now)
        {
            if (_menu == null)
                return;
            try
            {
                TalkMenuFrameOf(_menu, now);
            }
            catch (Exception ex)
            {
                Log.Write("TALK menu failed and closed: " + ex.Message);
                try
                {
                    TalkClose("it failed");
                }
                catch
                {
                    _menu = null;
                }
            }
        }

        /// The rails, the keys, the answer coming, the list drawn.
        private void TalkMenuFrameOf(TalkMenu t, int now)
        {
            Ped me = Game.Player.Character;
            string close = null;
            if (!ENTITY.DOES_ENTITY_EXIST(t.Handle) || ENTITY.IS_ENTITY_DEAD(t.Handle)) close = "they're gone";
            else if (me.Position.DistanceTo(t.From) > 3f) close = "you walked off";
            else if (ENTITY.GET_ENTITY_COORDS(t.Handle, true, true).DistanceTo(me.Position) > t.Reach + 1f) close = "too far apart";
            else if (PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle) || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PED.IS_PED_IN_COMBAT(t.Handle, 0)) close = "a weapon or a fight";
            else if (MISC.GET_MISSION_FLAG() || (t.CoverDollars == 0 && _director.Involves(t.Handle))) close = "a mission or a scene";
            else if (t.CoverDollars > 0 && t.Pending == null && t.CloseAt == 0 && !DebtStillPressed(t)) close = "the moment passed";   // the creditor's scene ended by itself
            else if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle)) close = "a cutscene or the game's own menu";
            else if (t.Pending == null && now - t.LastAt > 25000) close = "nothing chosen";
            else if (t.CloseAt > 0 && now >= t.CloseAt) close = "done";
            if (close != null)
            {
                TalkClose(close);
                return;
            }
            if (t.CoverDollars > 0 && t.CloseAt == 0)
                _director.HoldLive(1500);   // the scene waits while the word is had
            if (t.Pending != null && now >= t.AnswerAt)
                TalkAnswer(t, now);
            if (_menu != t)
                return;
            if (t.FaceAt > 0 && now >= t.FaceAt)
            {
                t.FaceAt = 0;
                TalkFace(t, me, false);   // stepped back, and turns to him again
            }
            else if (!t.Seated && !t.KeepOrders && t.CloseAt == 0 && now - t.HeldAt > 1500 && ENTITY.GET_ENTITY_SPEED(t.Handle) > 0.6f)
            {
                TalkFace(t, me, true);   // walking on regardless (their scenario or the game's own orders took them back): asked to stop again
                Log.Detail("TALK " + t.Name + " walked on while the list was open: asked to stop and face you again");
            }
            if (t.ThoughtAt > 0 && now >= t.ThoughtAt)
            {
                t.ThoughtAt = 0;
                if (t.Pending == null && _talkLast != null)
                    TalkThink(t);
            }
            // the keys: the pad's buttons kept from the game while the list is open; the keyboard read directly while
            // it's the one in use, the pad's buttons while a pad is (both at once moved the choice twice for one press)
            foreach (uint c in MenuButtons)
                PAD.DISABLE_CONTROL_ACTION(0, c, true);
            int pick = -1;
            bool up, down, accept, cancel;
            bool keyboard = PAD.IS_USING_KEYBOARD_AND_MOUSE(0);
            t.Pad = !keyboard;   // the hint under the list follows what's in use
            if (keyboard)
            {
                bool wheelUp = WheelMoved(WheelUps), wheelDown = !wheelUp && WheelMoved(WheelDowns);   // one notch, one row (Lex, 2026-10-31)
                up = TalkKey(0, 0x26) || wheelUp;
                down = TalkKey(1, 0x28) || wheelDown;
                bool space = TalkKey(4, 0x20);   // Space or Enter says the chosen line (Lex, 2026-10-31: no numbers)
                accept = TalkKey(2, 0x0D) || space;
                cancel = TalkKey(3, 0x08);
            }
            else
            {
                up = PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuUp);
                down = PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuDown);
                accept = PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuAccept);
                cancel = PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuCancel);
            }
            if (t.Pending == null)
            {
                if (cancel)
                {
                    TalkClose("you left it");   // not while an answer is coming: what was said and paid for gets its answer
                    return;
                }
                if (up)
                    t.Sel = (t.Sel + t.Options.Count - 1) % t.Options.Count;
                if (down)
                    t.Sel = (t.Sel + 1) % t.Options.Count;
                if (accept)
                    pick = t.Sel;
                if (pick >= 0 && pick < t.Options.Count)
                    TalkChoose(t, t.Options[pick], now);
            }
            TalkDraw(t);
        }

        /// A key pressed since the last frame (the high bit now, not before).
        private bool TalkKey(int slot, int vk)
        {
            bool down = (GetAsyncKeyState(vk) & 0x8000) != 0;
            bool was = _talkKeys[slot];
            _talkKeys[slot] = down;
            return down && !was;
        }

        private void TalkChoose(TalkMenu t, InteractOption o, int now)
        {
            t.LastAt = now;
            if (o.Why != null)
            {
                Hint(o.Label + ": " + o.Why + ".", 2500);
                return;
            }
            string seed = t.Id + "|" + o.Id + "|" + now / 1000;
            if (o.Id == "bye")
            {
                SpeechCaption(Arthur(), o.Label, Game.Player.Character.Handle, 1600, false);
                TalkSay(t, Interact.Answer("bye", t.Temper, t.Woman, t.Devout, t.Regard, null, null, false, false, seed));
                t.CloseAt = now + 1800;
                return;
            }
            bool confides = Interact.Confides(t.Regard, Interact.KindLately(_store.WorldLog, t.Id, _store.LastGameMin), t.Worry, t.Temper);
            PersonState pst = t.Id != null ? _store.Find(t.Id) : null;   // (null: a performer the game keeps for itself)
            Happening wrong = o.Id == "sorry" ? Interact.LastWrong(_store.WorldLog, t.Id, _store.LastGameMin) : null;
            // asked the same thing again, they don't give the very same words (the no-model run, item 7: the last answer to each person
            // for each option kept in memory; a few other seeds tried; only the words vary, never what the answer does)
            string lastKey = t.Id + "|" + o.Id, last;
            _talkLastLine.TryGetValue(lastKey, out last);
            InteractAnswer a = null;
            for (int tries = 0; tries < 4 && (a == null || (last != null && a.Line == last)); tries++)
            {
                string sd = tries == 0 ? seed : seed + "|again" + tries;
                a = o.Id == "ask" ? Interact.AskedAfter(InnerLife.WorrySaid(t.P, _store.LastGameMin / 1440, sd, pst != null ? pst.Musings : null), t.Temper, sd)   // how it stands today, in their words
                  : o.Id == "sorry" ? Interact.Sorry(t.Temper, t.Regard, sd, wrong != null ? wrong.Kind : null)
                  : o.Id == "house" ? Fondness.House(t.Temper, t.Woman, t.Devout, t.Regard, sd)   // the fondness interactions (Core/Fondness.cs)
                  : o.Id == "gift" ? Fondness.Gift(t.Temper, t.Woman, t.Regard, t.MoneyWorry && _talkConfided.Contains(TalkToldKey(t)), sd)
                  : o.Id == "condole" ? Fondness.Condole(t.Temper, t.Woman, t.Regard, t.YouKilledKnown, sd)
                  : o.Id == "cover" ? Fondness.Cover(t.Temper, t.CoverDollars, sd)
                  : o.Id == "escort" ? Fondness.Escort(t.Temper, t.Woman, t.Drunk, t.Regard, t.KnowsName, Interact.KindLately(_store.WorldLog, t.Id, _store.LastGameMin), sd)
                  : o.Id == "errand" ? Fondness.ErrandAsk(t.Temper, t.Woman, t.Regard, Person.FirstOf(t.ErrandTie.Other(t.Id)), t.ErrandOtherWoman, t.ErrandTie.Courted, sd)
                  : o.Id == "deliver" ? Fondness.Deliver(t.Temper, t.Woman, Person.FirstOf(_errand.FromName), Residents.IsWomansName(Person.FirstOf(_errand.FromName)), _errand.Sweethearts, sd)
                  : o.Id == "tip" ? Fondness.Tip(sd)
                  : Interact.Answer(o.Id, t.Temper, t.Woman, t.Devout, t.Regard, t.Rumour, t.Worry, confides, t.MoneyWorry && _talkConfided.Contains(TalkToldKey(t)), sd, YourSurname(), t.Self);
            }
            if (_talkLastLine.Count > 400)
                _talkLastLine.Clear();
            _talkLastLine[lastKey] = a.Line;
            SpeechCaption(Arthur(), o.Label, Game.Player.Character.Handle, 1600, false);
            t.Pending = a;
            t.PendingId = o.Id;
            t.PendingLabel = o.Label;
            TalkAskModel(t, o, a, now);   // their words from the model, if it answers in time
            t.AnswerAt = now + 1400;
            _talkSaid++;
        }

        /// The answer lands: the money goes now (not when chosen, so nothing is paid for an answer that never came), it's
        /// said, written down for the mind unless he did the same to them within the hour, and they do what it says.
        private void TalkAnswer(TalkMenu t, int now)
        {
            InteractAnswer a = t.Pending;
            if (!TalkModelLine(t, a, now))
            {
                t.AnswerAt = now + 150;   // the model's answer may yet come: a moment more
                return;
            }
            t.Pending = null;
            Ped me = Game.Player.Character;
            int h = t.Handle;
            if (a.Cents > 0)
            {
                int cash;
                try
                {
                    cash = MONEY._MONEY_GET_CASH_BALANCE();
                }
                catch
                {
                    cash = 0;
                }
                if (cash < a.Cents)
                {
                    Hint(t.PendingLabel + ": not enough money.", 2500);
                    TalkRefreshOptions();
                    return;
                }
                MONEY._MONEY_DECREMENT_CASH_BALANCE(a.Cents);
            }
            bool again = a.Event != null && t.Id != null && Interact.Again(_store.WorldLog, t.Id, a.Event, _store.LastGameMin);
            if (again)
            {
                a.Event = null;   // the same thing within the hour: said, not counted again
                a.LikeDelta = 0;
            }
            TalkSay(t, a);
            if (t.PendingId == "tip")
            {
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 2500, 0, 51, 0);   // a look up from the keys, and the thanks in their voice (TalkSay)
                if (t.Id != null)
                    _engine.Tipped(t.Id, t.Name, _store.LastGameMin);
                _tips++;
                _fondLast = "a coin for " + t.Name;
                Log.Write("FOND tip: a coin for the music to " + t.Name + (t.Id == null ? " (a performer the game keeps for itself: nothing remembered)" : ""));
            }
            if (a.Event != null && t.Id != null)
            {
                _kindSeenOff = t.PendingId == "tip";   // a quarter in a hat is no kindness to be seen
                RecordEvent(a.Event, "player", "you", t.Id, t.Name, ENTITY.GET_ENTITY_COORDS(h, true, true), me.Handle, me.Handle, h,
                            a.Settles ? Thanks.OutcomeOf(t.Worry) : a.Event == "charity" ? (t.PendingId == "gift" ? Fondness.GiftOutcome : t.PendingId == "tip" ? Fondness.TipOutcome : "gave $" + a.Cents / 100) : t.PendingId == "house" ? "whiskey for the house" : "the interaction menu");   // what late thanks reads (Thanks.KindOf, CentsOf)
                _kindSeenOff = false;
            }
            if (t.PendingId == "deliver")
                ErrandDelivered(t, a, now);   // the word's home (WestworldScript.Errand.cs)
            if (t.PendingId == "house" && a.Cents > 0)
                HouseRound(t, now);   // the room cheers and everyone treated is fonder (WestworldScript.Fondness.cs)
            if (t.PendingId == "cover" && a.Cents > 0)
                CoverDebt(t, now);    // the debtor's kindness, the creditor paid, the scene's end (WestworldScript.Fondness.cs)
            if (a.LikeDelta != 0 && t.FolkId != null)
                _bridge.LikeMore(t.FolkId, a.LikeDelta);   // enRAGE's own method for its own townsfolk
            if (a.Confided)
                _talkConfided.Add(TalkToldKey(t));
            if (a.LearnsName)
            {
                _engine.LearnName(t.Id, t.Name);   // they know his name from now on (letters, thoughts and greetings say it)
                t.KnowsName = true;
            }
            if (a.Settles)
            {
                FavourSettledFor(t.Id, t.Worry, Interact.FixCents(t.Worry));   // one who came to ask it of him (WestworldScript.Favours.cs)
                _engine.SettleWorry(t.Id);   // the rest of the worry's run reads as ended well (for a debt, the one they owed too)
                if (a.Loan)
                    _engine.MarkThanked(t.Id, Thanks.LoanKey(_store.LastGameMin), t.Name);   // they said it was a loan: they'll bring it back (late thanks)
                t.Worry = null;
                t.MoneyWorry = false;
                _talkSettled++;
            }
            if (!t.Seated)
            {
                Vector3 at = ENTITY.GET_ENTITY_COORDS(h, true, true);
                Vector3 away = at - me.Position;
                away.Z = 0f;
                Vector3 dir = away.Length() < 0.3f ? new Vector3(0f, 1f, 0f) : away.Normalized;
                switch (a.Then)
                {
                    case "step back":
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, at.X + dir.X * 2f, at.Y + dir.Y * 2f, at.Z, 1.0f, 4000, 0.5f, 0, 0f);
                        t.FaceAt = now + 2500;
                        break;
                    case "walk off":
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, at.X + dir.X * 15f, at.Y + dir.Y * 15f, at.Z, 1.3f, 12000, 1f, 0, 0f);
                        t.KeepOrders = true;
                        t.CloseAt = now + 1500;
                        break;
                    case "square up":
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                        t.KeepOrders = true;
                        t.CloseAt = now + 3500;
                        break;
                    case "escort":
                        EscortStart(t, now);   // they set off for home once the list closes (WestworldScript.Escort.cs)
                        t.KeepOrders = true;
                        t.CloseAt = now + 1200;
                        break;
                    case "errand":
                        ErrandStart(t, a, now);   // a word to carry (WestworldScript.Errand.cs); they stay
                        TalkFace(t, me, false);
                        break;
                    case "makeup":
                        TalkFace(t, me, false);
                        break;
                    default:
                        TalkFace(t, me, false);
                        break;
                }
            }
            if (t.Id != null)
                t.Regard = SightRegard(new Ped(h), t.Id);   // a drink or a warning can change how they stand with him
            _talkLast = a.Thought;
            t.ThoughtAt = now + 2600;
            TalkRefreshOptions();
            Log.Write("TALK " + t.Name + ": " + t.PendingId + " -> \"" + a.Line + "\"" + (a.Event != null ? " (" + a.Event + " for the mind)" : again ? " (the same within the hour: not counted again)" : "") +
                      (a.Cents > 0 ? ", $" + (a.Cents / 100.0).ToString("0.00") : "") + (a.Then != "stay" ? ", " + a.Then : "") + (a.Confided ? ", told you their worry" : "") + (a.Settles ? ", their worry mended" : "") + (a.LearnsName ? ", they know your name now" : ""));
        }

        private int TalkModelWaitMs { get { return (int)(_cfg.TalkModelWaitSeconds * 1000f); } }   // from Arthur's words: past this, the written line ([Mind] TalkModelWaitSeconds, 2.8 as built)
        private int _talkModelAsked, _talkModelSaid, _talkModelHeld, _talkModelLate;

        /// With [Mind] LanguageTalk: asks the model to word their answer (its meaning is the written line), through the
        /// picks' channel, ahead of any thought.
        private void TalkAskModel(TalkMenu t, InteractOption o, InteractAnswer a, int now)
        {
            t.TalkKey = null;
            t.TalkRequest = null;
            if (!_cfg.LanguageTalk || !_cfg.Language || !_cfg.MindLive || _engine == null || a == null || string.IsNullOrEmpty(a.Line))
                return;
            if ((o.Id == "drink" || o.Id == "give" || o.Id == "settle" || o.Id == "house" || o.Id == "gift") && a.Cents == 0)
                return;   // a drink or money turned down keeps its written words: the check can't tell a refusal worded as thanks (the review)
            if (o.Id == "condole" && a.Event == null)
                return;   // condolences thrown back: the written words
            if (o.Id == "escort" && a.Then != "escort")
                return;   // the walk declined: the check can't tell a no worded kindly
            if (o.Id == "errand" || o.Id == "deliver" || o.Id == "tip")
                return;   // the errand's words carry a message of their own, and the answer to a carried word is yes or no: the written lines
            if (!string.IsNullOrEmpty(t.Self) && a.Line.Contains(t.Self))
                return;   // their own name given back: the model may not say it (TalkCheck holds it), so the written line (the review)
            MindWho w = MindKnown(t.Handle, now);
            if (w == null || w.P == null)
                return;
            string why;
            LanguageRequest r = MindRequestOf(w, t.Handle, now, out why);
            if (r == null)
                return;
            r.Kind = "talk";
            r.Said = Language.TalkSaid(o.Id, o.Id == "give" && t.MoneyWorry && _talkConfided.Contains(TalkToldKey(t)) ? 500 : 100, t.Worry);
            r.Mean = a.Line;
            r.Near.Clear();       // no names but his own (the model called him by whoever was in sight)
            r.Memories.Clear();
            r.Beliefs.Clear();
            r.Worry = null;       // the meaning carries the worry where it belongs (it crept into every answer)
            r.YouAs = null;       // not how they think of him ("the man I fought" read as knowing him): his name, or a stranger (the review)
            if ((t.KnowsName || o.Id == "name") && YourSurname() != null)
                r.YouAs = "Mr. " + YourSurname();   // they know his name (or he's telling it them now): they may say it
            if (o.Id == "name" && YourFullName() != null)
                r.Said = "He tells you his name: " + YourFullName() + ".";
            string key = "talk|" + t.Id + "|" + now;
            var ask = new LanguageAsk { Key = key, Name = t.Name, PersonId = t.Id, Request = r, Written = a.Line, AskedAtMs = Environment.TickCount };
            if (!_engine.LanguageAnswers.AskPick(ask))
                return;
            if (_langThread == null)
                LanguageStart();
            t.TalkKey = key;
            t.TalkRequest = r;
            t.TalkAskedAt = now;
            _talkModelAsked++;
        }

        /// The model's words in place of the written line when they've come and pass the checks; false while they may
        /// still come (the answer waits a moment), true once it's settled either way.
        private bool TalkModelLine(TalkMenu t, InteractAnswer a, int now)
        {
            if (t.TalkKey == null || a == null)
                return true;
            string answer;
            long ms;
            if (!_engine.LanguageAnswers.TakePick(t.TalkKey, out answer, out ms))
            {
                if (now - t.TalkAskedAt < TalkModelWaitMs)
                    return false;
                _talkModelLate++;
                Log.Detail("TALK model: no answer for " + t.Name + " in " + TalkModelWaitMs + " ms; the written line");
                t.TalkKey = null;
                return true;
            }
            t.TalkKey = null;
            string clean = Language.StripAddress(Language.Tidy(answer), t.KnowsName || t.PendingId == "name" ? YourSurname() : null, t.Name);   // a name it made up for him, out (his own, being told it now, stays)
            bool accepts = a.Event == "round" || a.Event == "charity" || a.Event == "help" || a.Settles;
            string why = clean == null ? "empty" : Language.TalkCheck(t.TalkRequest, clean, accepts, a.Loan);
            if (why == null)
            {
                _talkModelSaid++;
                Log.Write("TALK model for " + t.Name + " in " + ms + " ms: \"" + clean + "\" (written: \"" + a.Line + "\")");
                a.WrittenLine = a.Line;   // the voice is chosen by what it means, not the model's wording (the review)
                a.Line = clean;
            }
            else
            {
                _talkModelHeld++;
                Log.Detail("TALK model for " + t.Name + ": held (" + why + "): \"" + (clean ?? "") + "\"; the written line");
            }
            return true;
        }

        private void TalkThink(TalkMenu t)
        {
            if (!string.IsNullOrEmpty(_talkLast))
                _ticker.Push(t.Name + ", thinking", _talkLast, "near", t.Handle);
            _talkLast = null;
        }

        /// What they say: their own voice for it when the game has one (afraid, hostile or warm as the answer is), the
        /// words as the top subtitle (said to him).
        private readonly Dictionary<string, string> _talkLastLine = new Dictionary<string, string>();   // person|option -> the last answer's words (memory only)

        private void TalkSay(TalkMenu t, InteractAnswer a)
        {
            if (a == null || string.IsNullOrEmpty(a.Line))
                return;
            int h = t.Handle;
            Func<string> voice = Speech.For(h, a.WrittenLine ?? a.Line, Game.Player.Character.Handle, a.VoiceHate, a.VoiceFear, a.Friendly, "chat");
            if (voice != null && !Speech.Busy(h))
                voice();
            SpeechCaption(t.First, a.Line, h, Math.Min(6000, 1800 + a.Line.Length * 55), true);
        }

        /// His whole name for telling ("Arthur Morgan", as the old talk gives it), or null when the engine has none.
        private string YourFullName()
        {
            return _engine != null && _engine.Player != null && !string.IsNullOrEmpty(_engine.Player.FullName) ? _engine.Player.FullName : null;
        }

        /// His surname (what people who know his name call him), or null.
        private string YourSurname()
        {
            return _engine != null && _engine.Player != null && !string.IsNullOrEmpty(_engine.Player.FullName) && !string.IsNullOrEmpty(_engine.Player.Surname) ? _engine.Player.Surname : null;
        }

        /// What they told him is kept by person, run and kind: a new worry (the next run's) hasn't been told (the review).
        private static string TalkToldKey(TalkMenu t)
        {
            return t.Id + "|" + t.WorryArc + "|" + (t.AskWorry ?? t.Worry);
        }

        private string Arthur()
        {
            return _engine != null && _engine.Player != null && !string.IsNullOrEmpty(_engine.Player.FullName) ? _engine.Player.FirstName : "You";
        }

        private void TalkClose(string why)
        {
            TalkMenu t = _menu;
            _menu = null;
            if (t == null)
                return;
            if (t.Id != null)
                _youNodAt[t.Id] = _now;   // just spoke with him: no passing look and greeting straight after goodbye (ToYou's rest)
            if (t.ThoughtAt > 0 && _talkLast != null)
                TalkThink(t);
            _escortTest = false;   // the Testing row's offer was for this list only
            _spaceAgain[t.Handle] = Game.GameTime + 60000;   // stood close to talk: no "a little room" from them straight after
            if (ENTITY.DOES_ENTITY_EXIST(t.Handle) && !ENTITY.IS_ENTITY_DEAD(t.Handle) && !_director.Involves(t.Handle))
            {
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t.Handle, false);   // they react to the world again (TalkOpen blocked it)
                PED.SET_PED_KEEP_TASK(t.Handle, false);
                TASK.TASK_CLEAR_LOOK_AT(t.Handle);
                if (!t.Seated)
                {
                    if (!t.KeepOrders)
                        TASK.CLEAR_PED_TASKS(t.Handle, true, false);   // back to their day (unless they walked off or squared up: those orders stand)
                    WanderLater(t.Handle, Game.GameTime + (t.KeepOrders ? 12000 : 2000));   // and on their way after, not left standing
                }
            }
            Log.Detail("TALK menu closed on " + t.Name + " (" + why + ")");
        }

        /// The list, drawn above the ticker on the left: who, then the options, the chosen one marked, the closed ones
        /// greyed, and under them a small hint of the keys (Lex, 2026-10-31: say that Space chooses). The words and their
        /// width are worked out once per choice, not every frame.
        private void TalkDraw(TalkMenu t)
        {
            float s = 0.3f, lh = Draw.LineHeight(s), x = 0.035f, fs = s * 0.85f, flh = Draw.LineHeight(fs);
            int n = t.Options.Count;
            bool pending = t.Pending != null;
            if (t.Rows == null || t.RowsSel != t.Sel || t.RowsPending != pending || t.RowsPad != t.Pad)
            {
                t.Title = t.Name + (t.Trade != null ? ", " + t.Trade : "");
                t.Wide = Draw.Width(t.Title, s * 1.05f);
                t.Rows = new string[n];
                for (int i = 0; i < n; i++)
                {
                    InteractOption o = t.Options[i];
                    string row = o.Label + (o.Why != null ? " (" + o.Why + ")" : "");
                    t.Wide = Math.Max(t.Wide, Draw.Width(row, s));
                    t.Rows[i] = (i == t.Sel && !pending ? "> " : "  ") + row;
                }
                t.Foot = t.Pad ? "A: say it   B: leave" : "Space: say it   Backspace: leave";
                t.Wide = Math.Max(t.Wide, Draw.Width(t.Foot, fs));
                t.RowsSel = t.Sel;
                t.RowsPending = pending;
                t.RowsPad = t.Pad;
            }
            float y = 0.56f - (n + 1) * lh - flh;   // the hint adds a line at the foot: the box grows upward, its foot stays clear of the ticker
            Draw.Rect(x - 0.008f, y - 0.006f, t.Wide + 0.016f, lh * (n + 1) + flh + 0.016f, 12, 10, 8, 185);
            Draw.Text(t.Title, x, y, s * 1.05f, 236, 214, 160);
            for (int i = 0; i < n; i++)
            {
                bool sel = i == t.Sel && !pending, closed = t.Options[i].Why != null;
                int c = closed ? 130 : sel ? 255 : 210;
                Draw.Text(t.Rows[i], x, y + (i + 1) * lh + 0.004f, s, c, c, closed ? 130 : sel ? 200 : 210);
            }
            Draw.Text(t.Foot, x, y + (n + 1) * lh + 0.006f, fs, pending ? 110 : 165, pending ? 104 : 155, pending ? 92 : 135);   // dimmer while they answer: no key does anything then
        }

        /// The Testing row: the menu on the nearest adult RealNPCs knows, within 6 m, no lock-on needed (the same busy
        /// checks as the prompt, only further).
        private string TalkTest(int now)
        {
            if (_menu != null)
                TalkClose("the Testing row again");
            Ped me = Game.Player.Character;
            foreach (Ped p in Peds.All().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 6f).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                if (TalkWhyNot(p.Handle, me, 6f) != null)
                    continue;
                return TalkOpen(p, "the Testing row", 6f);
            }
            return "nobody RealNPCs knows within 6 m (an adult, not busy with a scene, a talk or a fight)";
        }

        /// The add-on stopping: the prompt out of the lock-on menu, the list closed, the person let go.
        private void TalkShutdown()
        {
            TalkPromptEnd();
            _talkLast = null;   // no thought pushed to a ticker that's going
            if (_menu != null)
                TalkClose("the add-on stopped");
        }

        private string TalkStatus()
        {
            return (_cfg.InteractionMenu ? "on (Talk in the lock-on menu on " + (TalkOnControl() ? "the game's lock-on target-info control: Q unless rebound in its Controls menu, a pad's RB" : TalkKeyName() != null ? TalkKeyName() + ", read directly, or a pad's RB" : "a pad's RB only (TalkKey None)") + ")" : "off ([World] InteractionMenu)") + "; opened " + _talkOpened + " times, " + _talkSaid + " things said" +
                   (_talkConfided.Count > 0 ? ", " + _talkConfided.Count + " told you their worry" : "") + (_talkSettled > 0 ? ", " + _talkSettled + " worries mended" : "") +
                   (_talkModelAsked > 0 ? "; the model worded " + _talkModelSaid + " of " + _talkModelAsked + " answers (" + _talkModelHeld + " held by the checks, " + _talkModelLate + " late)" : "") + (_menu != null ? "; open on " + _menu.Name : "");
        }
    }
}
