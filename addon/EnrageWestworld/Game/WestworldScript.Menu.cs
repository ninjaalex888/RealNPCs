// The interaction menu, the game side (NIGHT3 item 4; Core/Interact.cs holds the choosing and the words). Locked on to an
// adult RealNPCs knows (a resident, one of enRAGE's townsfolk or records), the game's lock-on menu also offers "Talk" on Q
// (a pad's RB: INPUT_INTERACT_LOCKON_TARGET_INFO, free in the people's lock-on menu; the clerk's Reset prompt uses it
// only in a station clerk's list, and Talk stands aside there). Pressing it opens a short list above the ticker: up and
// down (or the D-pad, or 1 to 5) to choose, Enter (A) to say it, Backspace (B) to leave. Arthur's words show as a
// subtitle, then their answer in their own voice with its subtitle, and what it is (a round, charity, a standoff) is
// written down as a happening for the mind, so gratitude, fear, the greetings and the memories follow. The person turns
// to him and waits while the list is open. It closes on a weapon drawn, 3 m walked, combat, a mission, a scene taking
// them, or 25 s with nothing chosen. [World] InteractionMenu. Testing row "Talk to the nearest person you know".
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
            public string Id, Name, First, Temper, Worry, Trade, FolkId;
            public bool Woman, Devout, MoneyWorry, Seated;
            public List<InteractOption> Options;
            public int Sel, OpenedAt, LastAt, AnswerAt, ThoughtAt, CloseAt;
            public Vector3 From;
            public InteractAnswer Pending;
            public string PendingId;
        }

        private const uint TalkControl = 0x31219490;   // INPUT_INTERACT_LOCKON_TARGET_INFO: Q, or a pad's RB (as the clerk's Reset prompt)
        private const uint MenuUp = 1662638961u, MenuDown = 97156178u, MenuAccept = 3350541322u, MenuCancel = 359624985u;   // the settings panel's pad buttons
        private static IntPtr _talkWord;
        private TalkMenu _menu;
        private int _talkPrompt, _talkPromptFor, _talkPromptGroup, _talkOpened, _talkSaid;
        private readonly HashSet<string> _talkAsked = new HashSet<string>();   // who has been asked the news this session (the second time, they confide)
        private readonly bool[] _talkKeys = new bool[8];
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
                return;
            }
            Ped me = Game.Player.Character;
            int target = 0;
            if (_cfg.InteractionMenu && !PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            string why = target != 0 ? TalkWhyNot(target, me) : "nobody";
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
                TalkOpen(new Ped(target), "the Talk prompt");
            }
        }

        /// Why this person can't be talked to through the menu now, or null.
        private string TalkWhyNot(int target, Ped me)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target) || !PED.IS_PED_HUMAN(target) || PED.IS_PED_A_PLAYER(target) || ENTITY.IS_ENTITY_DEAD(target))
                return "not someone to talk to";
            Ped p = new Ped(target);
            if (!IsGrownUp(p) || Natives.StoryModel(p))
                return "a child, or one of the story's own";
            if (p.Position.DistanceTo(me.Position) > 4f)
                return "too far";
            if (PED.IS_PED_IN_COMBAT(target, 0) || MISC.GET_MISSION_FLAG() || _director.Involves(target) || PED.IS_PED_ON_MOUNT(target))
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

        /// Opens the list for this person: who they are, what's on their mind, what's on offer.
        private string TalkOpen(Ped p, string how)
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
                if (InnerLife.WorryNow(w.P, _store.LastGameMin / 1440, out kind, out stage, out good))
                    worry = kind;
            }
            if (w != null && w.Traits == null && w.Src != null)
                w.Traits = _engine.Shadow.TraitsOf(w.Src);
            FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(h) : null;
            _menu = new TalkMenu
            {
                Handle = h, Id = id, Name = name ?? TalkName(p), First = name != null ? Person.FirstOf(name) : TalkName(p), Temper = temper, Worry = worry,
                Woman = !PED.IS_PED_MALE(h), Devout = w != null && w.Traits != null && w.Traits[Traits.Piety] > 1.2f, MoneyWorry = worry == "debt" || worry == "savings" || worry == "bank",
                Trade = w != null && w.P != null ? w.P.Trade : null, FolkId = ac != null ? ac.Id : null, Seated = PED.IS_PED_USING_ANY_SCENARIO(h),
                OpenedAt = now, LastAt = now, From = me.Position
            };
            TalkRefreshOptions();
            if (!_menu.Seated)
            {
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 1500, 0f, 0f, 0f);
                TASK.TASK_STAND_STILL(h, 26000);
            }
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 26000, 0, 51, 0);
            _talkOpened++;
            Log.Write("TALK menu opened on " + _menu.Name + (_menu.Temper != null ? " (" + _menu.Temper + ")" : "") + (_menu.Worry != null ? ", worried about " + _menu.Worry : "") + " (" + how + ")");
            return null;
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
            _menu.Options = Interact.Options(_placeKind == "saloon", cash, _menu.MoneyWorry);
            if (_menu.Sel >= _menu.Options.Count)
                _menu.Sel = 0;
        }

        /// Every frame while the list is open: the rails, the keys, the answer coming, the list drawn.
        private void TalkMenuFrame(int now)
        {
            TalkMenu t = _menu;
            if (t == null)
                return;
            Ped me = Game.Player.Character;
            string close = null;
            if (!ENTITY.DOES_ENTITY_EXIST(t.Handle) || ENTITY.IS_ENTITY_DEAD(t.Handle)) close = "they're gone";
            else if (me.Position.DistanceTo(t.From) > 3f) close = "you walked off";
            else if (PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle) || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PED.IS_PED_IN_COMBAT(t.Handle, 0)) close = "a weapon or a fight";
            else if (MISC.GET_MISSION_FLAG() || _director.Involves(t.Handle)) close = "a mission or a scene";
            else if (now - t.LastAt > 25000) close = "nothing chosen";
            else if (t.CloseAt > 0 && now >= t.CloseAt) close = "done";
            if (close != null)
            {
                TalkClose(close);
                return;
            }
            if (t.Pending != null && now >= t.AnswerAt)
                TalkAnswer(t, now);
            if (t.ThoughtAt > 0 && now >= t.ThoughtAt)
            {
                t.ThoughtAt = 0;
                if (t.Pending == null && _talkLast != null)
                    TalkThink(t);
            }
            // the keys: the pad's buttons kept from the game while the list is open, the keyboard read directly
            foreach (uint c in new[] { MenuUp, MenuDown, MenuAccept, MenuCancel })
                PAD.DISABLE_CONTROL_ACTION(0, c, true);
            int pick = -1;
            bool up = TalkKey(0, 0x26) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuUp);
            bool down = TalkKey(1, 0x28) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuDown);
            bool accept = TalkKey(2, 0x0D) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuAccept);
            bool cancel = TalkKey(3, 0x08) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, MenuCancel);
            for (int i = 0; i < Math.Min(4, t.Options.Count); i++)
                if (TalkKey(4 + i, 0x31 + i))
                    pick = i;
            if (cancel)
            {
                TalkClose("you left it");
                return;
            }
            if (t.Pending == null)
            {
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
            if (o.Id == "bye")
            {
                SpeechCaption(Arthur(), o.Label, Game.Player.Character.Handle, 1600, false);
                InteractAnswer bye = Interact.Answer("bye", t.Temper, t.Woman, t.Devout, null, null, false, false, t.Id + "|bye|" + now / 1000);
                TalkSay(t, bye.Line);
                t.CloseAt = now + 1800;
                return;
            }
            string rumour = null;
            if (o.Id == "news")
            {
                List<string> pool = _engine.RumourPool(_placeTown, 1);
                rumour = pool.Count > 0 ? pool[0] : null;
            }
            bool confides = _talkAsked.Contains(t.Id) || SightRegard(new Ped(t.Handle), t.Id) == "fond";
            if (o.Id == "news")
                _talkAsked.Add(t.Id);
            InteractAnswer a = Interact.Answer(o.Id, t.Temper, t.Woman, t.Devout, rumour, t.Worry, confides, t.MoneyWorry, t.Id + "|" + o.Id + "|" + now / 1000);
            if (a.Cents > 0)
            {
                int cash = MONEY._MONEY_GET_CASH_BALANCE();
                if (cash < a.Cents)
                {
                    Hint(o.Label + ": not enough money.", 2500);
                    return;
                }
                MONEY._MONEY_DECREMENT_CASH_BALANCE(a.Cents);
            }
            SpeechCaption(Arthur(), o.Label, Game.Player.Character.Handle, 1600, false);
            t.Pending = a;
            t.PendingId = o.Id;
            t.AnswerAt = now + 1400;
            _talkSaid++;
        }

        private void TalkAnswer(TalkMenu t, int now)
        {
            InteractAnswer a = t.Pending;
            t.Pending = null;
            Ped me = Game.Player.Character;
            int h = t.Handle;
            TalkSay(t, a.Line);
            if (a.Event != null)
                RecordEvent(a.Event, "player", "you", t.Id, t.Name, ENTITY.GET_ENTITY_COORDS(h, true, true), me.Handle, me.Handle, h, "the interaction menu");
            if (a.LikeDelta != 0 && t.FolkId != null)
                _bridge.LikeMore(t.FolkId, a.LikeDelta);   // enRAGE's own method for its own townsfolk
            if (!t.Seated)
            {
                Vector3 away = ENTITY.GET_ENTITY_COORDS(h, true, true) - me.Position;
                away.Z = 0f;
                Vector3 dir = away.Length() < 0.3f ? new Vector3(0f, 1f, 0f) : away.Normalized;
                Vector3 at = ENTITY.GET_ENTITY_COORDS(h, true, true);
                switch (a.Then)
                {
                    case "step back":
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, at.X + dir.X * 3f, at.Y + dir.Y * 3f, at.Z, 1.0f, 4000, 0.5f, 0, 0f);
                        break;
                    case "walk off":
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, at.X + dir.X * 15f, at.Y + dir.Y * 15f, at.Z, 1.3f, 12000, 1f, 0, 0f);
                        t.CloseAt = now + 1500;
                        break;
                    case "square up":
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                        t.CloseAt = now + 3500;
                        break;
                }
            }
            _talkLast = a.Thought;
            t.ThoughtAt = now + 2600;
            TalkRefreshOptions();
            Log.Write("TALK " + t.Name + ": " + t.PendingId + " -> \"" + a.Line + "\"" + (a.Event != null ? " (" + a.Event + " for the mind)" : "") + (a.Cents > 0 ? ", $" + (a.Cents / 100.0).ToString("0.00") : "") + (a.Then != "stay" ? ", " + a.Then : ""));
        }

        private void TalkThink(TalkMenu t)
        {
            if (!string.IsNullOrEmpty(_talkLast))
                _ticker.Push(t.Name + ", thinking", _talkLast, "near", t.Handle);
            _talkLast = null;
        }

        /// What they say: their own voice for it when the game has one, the words as the top subtitle (said to him).
        private void TalkSay(TalkMenu t, string line)
        {
            if (string.IsNullOrEmpty(line))
                return;
            int h = t.Handle;
            Func<string> voice = Speech.For(h, line, Game.Player.Character.Handle, 0, 0, false, "chat");
            if (voice != null && !Speech.Busy(h))
                voice();
            SpeechCaption(t.First, line, h, Math.Min(6000, 1800 + line.Length * 55), true);
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
            if (ENTITY.DOES_ENTITY_EXIST(t.Handle) && !ENTITY.IS_ENTITY_DEAD(t.Handle) && !t.Seated && !_director.Involves(t.Handle) && t.CloseAt == 0)
                TASK.CLEAR_PED_TASKS(t.Handle, true, false);   // back to their day (unless they walked off or squared up: those orders stand)
            Log.Detail("TALK menu closed on " + t.Name + " (" + why + ")");
        }

        /// The list, drawn above the ticker on the left: who, then the options, the chosen one marked, the closed ones greyed.
        private void TalkDraw(TalkMenu t)
        {
            float s = 0.3f, lh = Draw.LineHeight(s), x = 0.035f;
            int n = t.Options.Count;
            float y = 0.56f - (n + 1) * lh;
            string title = t.Name + (t.Trade != null ? ", " + t.Trade : "");
            float wide = Draw.Width(title, s * 1.05f);
            for (int i = 0; i < n; i++)
                wide = Math.Max(wide, Draw.Width((i + 1) + ". " + t.Options[i].Label + (t.Options[i].Why != null ? " (" + t.Options[i].Why + ")" : ""), s));
            Draw.Rect(x - 0.008f, y - 0.006f, wide + 0.016f, lh * (n + 1) + 0.016f, 12, 10, 8, 185);
            Draw.Text(title, x, y, s * 1.05f, 236, 214, 160);
            for (int i = 0; i < n; i++)
            {
                InteractOption o = t.Options[i];
                bool sel = i == t.Sel && t.Pending == null;
                string row = (sel ? "> " : "  ") + (i + 1) + ". " + o.Label + (o.Why != null ? " (" + o.Why + ")" : "");
                int c = o.Why != null ? 130 : sel ? 255 : 210;
                Draw.Text(row, x, y + (i + 1) * lh + 0.004f, s, c, c, o.Why != null ? 130 : sel ? 200 : 210);
            }
        }

        /// The Testing row: the menu on the nearest adult RealNPCs knows, within 6 m, no lock-on needed.
        private string TalkTest(int now)
        {
            if (_menu != null)
                TalkClose("the Testing row again");
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 6f).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                string why = TalkWhyNot(p.Handle, me);
                if (why == "too far")
                    why = null;   // the row reaches 6 m
                if (why != null)
                    continue;
                return TalkOpen(p, "the Testing row");
            }
            return "nobody RealNPCs knows within 6 m (an adult, not in a scene or a fight)";
        }

        private string TalkStatus()
        {
            return (_cfg.InteractionMenu ? "on (Talk on Q, a pad's RB, in the lock-on menu)" : "off ([World] InteractionMenu)") + "; opened " + _talkOpened + " times, " + _talkSaid + " things said" +
                   (_menu != null ? "; open on " + _menu.Name : "");
        }
    }
}
