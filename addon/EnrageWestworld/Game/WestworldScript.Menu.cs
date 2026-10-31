// The interaction menu, the game side (NIGHT3 item 4; Core/Interact.cs holds the choosing and the words). Locked on to an
// adult RealNPCs knows (a resident, one of enRAGE's townsfolk or records), the game's lock-on menu also offers "Talk" on Q
// (a pad's RB: INPUT_INTERACT_LOCKON_TARGET_INFO, free in the people's lock-on menu; the clerk's Reset prompt uses it
// only in a station clerk's list, and Talk stands aside there). Pressing it opens a short list above the ticker: up and
// down (or the D-pad, or 1 to 6) to choose, Enter (A) to say it, Backspace (B) to leave; the keyboard's keys while the
// keyboard is in use, the pad's buttons while a pad is. Arthur's words show as a subtitle, then their answer in their
// own voice with its subtitle (afraid or hostile when they are), and what it is (a round, charity, a warning) is written
// down as a happening for the mind, so gratitude, fear, the greetings and the memories follow; the money goes when the
// answer comes. The person turns to face him while the list is open. It closes on a weapon drawn, 3 m walked, the two
// of them 5 m apart, combat, a mission, a cutscene, a scene taking them, or 25 s with nothing chosen. Nobody busy with
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
            public string Id, Name, First, Temper, Worry, Trade, FolkId, Regard, Rumour;
            public bool Woman, Devout, MoneyWorry, Seated, KeepOrders;
            public float Reach;
            public List<InteractOption> Options;
            public int Sel, OpenedAt, LastAt, AnswerAt, ThoughtAt, CloseAt, FaceAt;
            public Vector3 From;
            public InteractAnswer Pending;
            public string PendingId, PendingLabel;
            public string Title;
            public string[] Rows;     // the list as drawn, kept until the choice or the options change
            public float Wide;
            public int RowsSel = -1;
            public bool RowsPending;
        }

        private const uint TalkControl = 0x31219490;   // INPUT_INTERACT_LOCKON_TARGET_INFO: Q, or a pad's RB (as the clerk's Reset prompt)
        private const uint MenuUp = 1662638961u, MenuDown = 97156178u, MenuAccept = 3350541322u, MenuCancel = 359624985u;   // the settings panel's pad buttons
        private static readonly uint[] MenuButtons = { MenuUp, MenuDown, MenuAccept, MenuCancel };
        private const float MenuReach = 4f;
        private static IntPtr _talkWord;
        private TalkMenu _menu;
        private int _talkPrompt, _talkPromptFor, _talkPromptGroup, _talkOpened, _talkSaid, _talkSettled;
        private readonly HashSet<string> _talkConfided = new HashSet<string>();   // who has told him their worry this session (a money worry he knows of: the gift is five dollars)
        private readonly bool[] _talkKeys = new bool[10];
        private string _talkLast;
        private Action _aTalkPrompt;

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private void TalkPromptStep()
        {
            if (_aTalkPrompt == null)
                _aTalkPrompt = TalkPromptTick;
            if (_cfg.InteractionMenu || _talkPrompt != 0 || _menu != null)
                Safe("talk menu", _aTalkPrompt);
        }

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
            string why = target != 0 ? TalkWhyNot(target, me, MenuReach) : "nobody";
            if (why != null || (_forgetPrompt != 0 && _forgetPromptFor == target))
            {
                TalkPromptEnd();
                return;
            }
            int group = HUD._UI_PROMPT_GET_GROUP_ID_FOR_TARGET_ENTITY(target);
            if (_talkPrompt == 0 || !HUD._UI_PROMPT_IS_VALID(_talkPrompt) || _talkPromptFor != target || _talkPromptGroup != group)
            {
                TalkPromptEnd();
                _talkPrompt = NewTalkPrompt(group);
                _talkPromptFor = target;
                _talkPromptGroup = group;
            }
            if (HUD._UI_PROMPT_HAS_STANDARD_MODE_COMPLETED(_talkPrompt, 0))
            {
                TalkPromptEnd();
                TalkOpen(new Ped(target), "the Talk prompt", MenuReach);
            }
        }

        /// Why this person can't be talked to through the menu now, or null: within `reach` metres, and not busy with
        /// anything else (MayDirect's rules, less the setting that keeps enRAGE's people out of our scenes: this is
        /// only a word with them).
        private string TalkWhyNot(int target, Ped me, float reach)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target) || !PED.IS_PED_HUMAN(target) || PED.IS_PED_A_PLAYER(target) || ENTITY.IS_ENTITY_DEAD(target))
                return "not someone to talk to";
            Ped p = new Ped(target);
            if (!IsGrownUp(p) || Natives.StoryModel(p))
                return "a child, or one of the story's own";
            if (p.Position.DistanceTo(me.Position) > reach)
                return "too far";
            bool record = IsRecord(target);
            if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(target, 0) || PED.IS_PED_FLEEING(target) || PED.IS_PED_RAGDOLL(target) || PED.IS_PED_ON_MOUNT(target) ||
                PED.IS_PED_IN_ANY_VEHICLE(target, false) || _director.Involves(target) || NotFree(target) || Spawner.IsOurs(target) ||
                (record ? _bridge.EnrageFighter(target) || _bridge.Interrogating : Natives.OtherScripts(target)))
                return "busy";
            string id, name, temper;
            if (!LastingId(p, out id, out name, out temper) || id == null)
                return "nobody RealNPCs knows";
            return null;
        }

        private static int NewTalkPrompt(int group)
        {
            if (_literalTag == IntPtr.Zero)
                _literalTag = Kept("LITERAL_STRING");
            if (_talkWord == IntPtr.Zero)
                _talkWord = Kept("Talk");
            int prompt = HUD._UI_PROMPT_REGISTER_BEGIN();
            HUD._UI_PROMPT_SET_CONTROL_ACTION(prompt, TalkControl);
            ulong word = Function.Call<ulong>(VarText, 10, new InputArgument((ulong)_literalTag.ToInt64()), new InputArgument((ulong)_talkWord.ToInt64()));
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
            if (p == null || !p.Exists() || !LastingId(p, out id, out name, out temper) || id == null)
                return "nobody RealNPCs knows";
            int h = p.Handle, now = Game.GameTime;
            Ped me = Game.Player.Character;
            MindWho w = MindKnown(h, now);
            string worry = null;
            if (w != null && w.P != null)
            {
                string kind;
                int stage;
                bool good;
                if (InnerLife.WorryNow(w.P, _store.LastGameMin / 1440, out kind, out stage, out good) && stage <= 2)
                    worry = kind;   // while it lasts: once it's come out one way or the other (stage 3) it's no worry now
            }
            if (w != null && w.Traits == null && w.Src != null)
                w.Traits = _engine.Shadow.TraitsOf(w.Src);
            FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(h) : null;
            List<string> pool = _engine.RumourPool(_placeTown, 3);
            _menu = new TalkMenu
            {
                Handle = h, Id = id, Name = name ?? TalkName(p), First = name != null ? Person.FirstOf(name) : TalkName(p), Temper = temper, Worry = worry,
                Woman = !PED.IS_PED_MALE(h), Devout = w != null && w.Traits != null && w.Traits[Traits.Piety] > 1.2f, MoneyWorry = worry == "debt" || worry == "savings" || worry == "bank",
                Trade = w != null && w.P != null ? w.P.Trade : null, FolkId = ac != null ? ac.Id : null, Seated = PED.IS_PED_USING_ANY_SCENARIO(h),
                Regard = SightRegard(p, id), Rumour = pool.Count > 0 ? pool[Voice.Hash(id) % pool.Count] : null,   // which of the town's talk they've heard: theirs, the same each time
                Reach = reach, OpenedAt = now, LastAt = now, From = me.Position
            };
            TalkRefreshOptions();
            TalkFace(_menu, me, true);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 26000, 0, 51, 0);
            _talkOpened++;
            Log.Write("TALK menu opened on " + _menu.Name + (_menu.Temper != null ? " (" + _menu.Temper + ")" : "") + (_menu.Regard != null ? ", " + _menu.Regard + " of you" : "") +
                      (_menu.Worry != null ? ", worried about " + _menu.Worry : "") + " (" + how + ")");
            return null;
        }

        /// They turn to face him and keep facing him (one order for the whole time, given again after each answer): a
        /// turn and then a stand-still replaced the turn at once.
        private static void TalkFace(TalkMenu t, Ped me, bool first)
        {
            if (t.Seated)
                return;
            if (first)
                TASK.CLEAR_PED_TASKS(t.Handle, true, false);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(t.Handle, me.Handle, 26000, 0f, 0f, 0f);
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
            bool told = _talkConfided.Contains(_menu.Id);
            _menu.Options = Interact.Options(_placeKind == "saloon", cash, _menu.MoneyWorry && told, _menu.Worry == "debt" && told);
            if (_menu.Sel >= _menu.Options.Count)
                _menu.Sel = 0;
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
            else if (MISC.GET_MISSION_FLAG() || _director.Involves(t.Handle)) close = "a mission or a scene";
            else if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle)) close = "a cutscene or the game's own menu";
            else if (t.Pending == null && now - t.LastAt > 25000) close = "nothing chosen";
            else if (t.CloseAt > 0 && now >= t.CloseAt) close = "done";
            if (close != null)
            {
                TalkClose(close);
                return;
            }
            if (t.Pending != null && now >= t.AnswerAt)
                TalkAnswer(t, now);
            if (_menu != t)
                return;
            if (t.FaceAt > 0 && now >= t.FaceAt)
            {
                t.FaceAt = 0;
                TalkFace(t, me, false);   // stepped back, and turns to him again
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
            if (PAD.IS_USING_KEYBOARD_AND_MOUSE(0))
            {
                up = TalkKey(0, 0x26);
                down = TalkKey(1, 0x28);
                accept = TalkKey(2, 0x0D);
                cancel = TalkKey(3, 0x08);
                for (int i = 0; i < 6; i++)
                    if (TalkKey(4 + i, 0x31 + i) && i < t.Options.Count)
                        pick = i;
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
            InteractAnswer a = Interact.Answer(o.Id, t.Temper, t.Woman, t.Devout, t.Regard, t.Rumour, t.Worry, confides, t.MoneyWorry && _talkConfided.Contains(t.Id), seed);
            SpeechCaption(Arthur(), o.Label, Game.Player.Character.Handle, 1600, false);
            t.Pending = a;
            t.PendingId = o.Id;
            t.PendingLabel = o.Label;
            t.AnswerAt = now + 1400;
            _talkSaid++;
        }

        /// The answer lands: the money goes now (not when chosen, so nothing is paid for an answer that never came), it's
        /// said, written down for the mind unless he did the same to them within the hour, and they do what it says.
        private void TalkAnswer(TalkMenu t, int now)
        {
            InteractAnswer a = t.Pending;
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
            bool again = a.Event != null && Interact.Again(_store.WorldLog, t.Id, a.Event, _store.LastGameMin);
            if (again)
            {
                a.Event = null;   // the same thing within the hour: said, not counted again
                a.LikeDelta = 0;
            }
            TalkSay(t, a);
            if (a.Event != null)
                RecordEvent(a.Event, "player", "you", t.Id, t.Name, ENTITY.GET_ENTITY_COORDS(h, true, true), me.Handle, me.Handle, h, "the interaction menu");
            if (a.LikeDelta != 0 && t.FolkId != null)
                _bridge.LikeMore(t.FolkId, a.LikeDelta);   // enRAGE's own method for its own townsfolk
            if (a.Confided)
                _talkConfided.Add(t.Id);
            if (a.Settles)
            {
                _engine.SettleWorry(t.Id);   // the rest of the debt's run reads as ended well, for them and the one they owed
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
                    default:
                        TalkFace(t, me, false);
                        break;
                }
            }
            t.Regard = SightRegard(new Ped(h), t.Id);   // a drink or a warning can change how they stand with him
            _talkLast = a.Thought;
            t.ThoughtAt = now + 2600;
            TalkRefreshOptions();
            Log.Write("TALK " + t.Name + ": " + t.PendingId + " -> \"" + a.Line + "\"" + (a.Event != null ? " (" + a.Event + " for the mind)" : again ? " (the same within the hour: not counted again)" : "") +
                      (a.Cents > 0 ? ", $" + (a.Cents / 100.0).ToString("0.00") : "") + (a.Then != "stay" ? ", " + a.Then : "") + (a.Confided ? ", told you their worry" : "") + (a.Settles ? ", their debt settled" : ""));
        }

        private void TalkThink(TalkMenu t)
        {
            if (!string.IsNullOrEmpty(_talkLast))
                _ticker.Push(t.Name + ", thinking", _talkLast, "near", t.Handle);
            _talkLast = null;
        }

        /// What they say: their own voice for it when the game has one (afraid, hostile or warm as the answer is), the
        /// words as the top subtitle (said to him).
        private void TalkSay(TalkMenu t, InteractAnswer a)
        {
            if (a == null || string.IsNullOrEmpty(a.Line))
                return;
            int h = t.Handle;
            Func<string> voice = Speech.For(h, a.Line, Game.Player.Character.Handle, a.VoiceHate, a.VoiceFear, a.Friendly, "chat");
            if (voice != null && !Speech.Busy(h))
                voice();
            SpeechCaption(t.First, a.Line, h, Math.Min(6000, 1800 + a.Line.Length * 55), true);
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
            if (t.ThoughtAt > 0 && _talkLast != null)
                TalkThink(t);
            if (ENTITY.DOES_ENTITY_EXIST(t.Handle) && !ENTITY.IS_ENTITY_DEAD(t.Handle) && !_director.Involves(t.Handle))
            {
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
        /// greyed. The words and their width are worked out once per choice, not every frame.
        private void TalkDraw(TalkMenu t)
        {
            float s = 0.3f, lh = Draw.LineHeight(s), x = 0.035f;
            int n = t.Options.Count;
            bool pending = t.Pending != null;
            if (t.Rows == null || t.RowsSel != t.Sel || t.RowsPending != pending)
            {
                t.Title = t.Name + (t.Trade != null ? ", " + t.Trade : "");
                t.Wide = Draw.Width(t.Title, s * 1.05f);
                t.Rows = new string[n];
                for (int i = 0; i < n; i++)
                {
                    InteractOption o = t.Options[i];
                    string row = (i + 1) + ". " + o.Label + (o.Why != null ? " (" + o.Why + ")" : "");
                    t.Wide = Math.Max(t.Wide, Draw.Width(row, s));
                    t.Rows[i] = (i == t.Sel && !pending ? "> " : "  ") + row;
                }
                t.RowsSel = t.Sel;
                t.RowsPending = pending;
            }
            float y = 0.56f - (n + 1) * lh;
            Draw.Rect(x - 0.008f, y - 0.006f, t.Wide + 0.016f, lh * (n + 1) + 0.016f, 12, 10, 8, 185);
            Draw.Text(t.Title, x, y, s * 1.05f, 236, 214, 160);
            for (int i = 0; i < n; i++)
            {
                bool sel = i == t.Sel && !pending, closed = t.Options[i].Why != null;
                int c = closed ? 130 : sel ? 255 : 210;
                Draw.Text(t.Rows[i], x, y + (i + 1) * lh + 0.004f, s, c, c, closed ? 130 : sel ? 200 : 210);
            }
        }

        /// The Testing row: the menu on the nearest adult RealNPCs knows, within 6 m, no lock-on needed (the same busy
        /// checks as the prompt, only further).
        private string TalkTest(int now)
        {
            if (_menu != null)
                TalkClose("the Testing row again");
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 6f).OrderBy(x => x.Position.DistanceTo(me.Position)))
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
            return (_cfg.InteractionMenu ? "on (Talk on Q, a pad's RB, in the lock-on menu)" : "off ([World] InteractionMenu)") + "; opened " + _talkOpened + " times, " + _talkSaid + " things said" +
                   (_talkConfided.Count > 0 ? ", " + _talkConfided.Count + " told you their worry" : "") + (_talkSettled > 0 ? ", " + _talkSettled + " debts settled" : "") + (_menu != null ? "; open on " + _menu.Name : "");
        }
    }
}
