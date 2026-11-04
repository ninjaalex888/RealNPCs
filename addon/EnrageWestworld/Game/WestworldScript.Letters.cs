// Letters answered and written (SIXDAY item 8; roadmap 8; Core/Replies.cs, Core/Engine.Letters.cs). Over the reader: R on a
// letter opens the answers (kindly, coldly, a threat, money, asking to meet); L in a post town opens the people he has met, then
// what to write (a warning, an apology, a love note, a threat, money). The money goes as it's sent; the engine carries the rest
// (the post's days, the writer's change of heart, the letter back). The meet: when a writer waits (ten till four on the day, in
// their town), a free unseen body of their sex there becomes them (bound as a resident: their name card, a grey mark on the map
// with their name); come within 3 m and they turn and say their two lines, and the engine is told. Keyboard only for the two
// menus (the pad closes the reader). [World] LetterReplies, WriteLetters. Log REPLY, WRITE, MEET; F7 "TEST letters:".
using System;
using System.Collections.Generic;
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
        private sealed class LetterMenu
        {
            public string Mode;                  // reply, write:who, write:what
            public Paper Paper;                  // the letter answered
            public List<InteractOption> Rows;
            public List<PersonState> People;     // for write:who
            public PersonState To;               // chosen
            public int Sel, LinesSel = -1;
            public string Title;
            public string[] Lines;               // the rows as drawn, remade only when the choice moves
            public float Wide;
        }

        private sealed class MeetRun
        {
            public Reply Reply;
            public int Handle, Since, NearSince, SecondAt;
            public Blip Dot;
            public Ped Body;                     // kept, not made each frame
            public bool Fresh;                   // a body bound for the meet alone: unbound when it ends
            public string Name, Second;
        }

        private LetterMenu _letterMenu;
        private MeetRun _meet;
        private int _lettersNext, _lettersSent, _lettersWritten, _meetsCast, _meetsMet;
        private Action _aLetters;

        private bool InPostTown()
        {
            return _placeTown != null && PostTowns.Contains(_placeTown);
        }

        /// A key while the reader is open: the menus' while one is up, R or L to open one; false when the reader's own.
        private bool LetterKey(Keys k)
        {
            if (_cfg == null || _engine == null || _store == null)
                return false;
            LetterMenu m = _letterMenu;
            if (m == null)
            {
                if (k == Keys.R && _cfg.LetterReplies)
                {
                    Paper p = _reader.Current;
                    if (p != null && Replies.Answerable(p) && !Replies.Answered(_store.Replies, p.Id))
                    {
                        if (p.FromId != null && _engine.HasDied(p.FromId))
                            Hint("No answering the dead.", 2500);
                        else
                            LetterMenuOpen("reply", p);
                        return true;
                    }
                }
                if (k == Keys.L && _cfg.WriteLetters)
                {
                    if (!InPostTown())
                        Hint("Letters go from a post office: write in a town.", 3000);
                    else
                        LetterMenuOpen("write:who", null);
                    return true;
                }
                return false;
            }
            switch (k)
            {
                case Keys.Up: case Keys.W:
                    m.Sel = (m.Sel + m.Rows.Count - 1) % m.Rows.Count;
                    return true;
                case Keys.Down: case Keys.S:
                    m.Sel = (m.Sel + 1) % m.Rows.Count;
                    return true;
                case Keys.Enter: case Keys.Space:
                    LetterChoose(m);
                    return true;
                case Keys.Escape: case Keys.Back:
                    _letterMenu = null;
                    return true;
                default:
                    return true;   // every other key is the menu's while it's up
            }
        }

        private int CashNow()
        {
            try
            {
                return MONEY._MONEY_GET_CASH_BALANCE();
            }
            catch
            {
                return 0;
            }
        }

        private void LetterMenuOpen(string mode, Paper p)
        {
            var m = new LetterMenu { Mode = mode, Paper = p };
            int cash = CashNow();
            if (mode == "reply")
            {
                m.Rows = Replies.Options(cash);
                m.Title = "Answer " + (p.From ?? "them") + ":";
            }
            else if (mode == "write:who")
            {
                m.People = _engine.WriteTo(_store.LastGameMin);
                if (m.People.Count == 0)
                {
                    Hint("You've met nobody you could write to.", 3000);
                    return;
                }
                m.Rows = m.People.Select(st => new InteractOption { Id = st.Id, Label = st.Name }).ToList();
                m.Title = "Write to:";
            }
            else
            {
                m.Rows = Replies.OwnOptions(cash);
                m.Title = "To " + (m.To != null ? m.To.Name : "them") + ":";
            }
            _letterMenu = m;
        }

        private void LetterChoose(LetterMenu m)
        {
            if (m.Rows == null || m.Sel < 0 || m.Sel >= m.Rows.Count)
                return;
            InteractOption row = m.Rows[m.Sel];
            if (row.Why != null)
            {
                Hint(row.Label + ": " + row.Why + ".", 2500);
                return;
            }
            long gameNow = _store.LastGameMin;
            if (m.Mode == "reply")
            {
                string tone = row.Id == "money5" || row.Id == "money20" ? "money" : row.Id;
                int amount = row.Id == "money5" ? Replies.Money5 : row.Id == "money20" ? Replies.Money20 : 0;
                if (amount > 0 && CashNow() < amount)
                {
                    Hint("Not enough money.", 2500);
                    return;
                }
                Person who = m.Paper.FromId != null ? _engine.Find(m.Paper.FromId) : null;
                string toTown = who != null ? who.Home : Replies.TownOf(m.Paper);
                Reply r = _engine.SendReply(m.Paper, tone, amount, _placeTown ?? "the road", MetresBetween(_placeTown, toTown), gameNow);
                if (r == null)
                {
                    Hint("That letter can't be answered.", 2500);
                    _letterMenu = null;
                    return;
                }
                if (amount > 0)
                    MONEY._MONEY_DECREMENT_CASH_BALANCE(amount);
                _lettersSent++;
                Hint("Your answer to " + (m.Paper.From ?? "them") + " is in the post.", 4000);
                _letterMenu = null;
                return;
            }
            if (m.Mode == "write:who")
            {
                m.To = m.People[m.Sel];
                var next = new LetterMenu { Mode = "write:what", To = m.To, Rows = Replies.OwnOptions(CashNow()), Title = "To " + m.To.Name + ":" };
                _letterMenu = next;
                return;
            }
            // write:what
            string kind = row.Id;
            int cents = kind == "money" ? Replies.OwnMoney : 0;
            if (cents > 0 && CashNow() < cents)
            {
                Hint("Not enough money.", 2500);
                return;
            }
            Person to = _engine.Find(m.To.Id);
            string town = to != null && to.Home != null ? to.Home : m.To.FirstSeenTown;
            Reply w = _engine.WriteLetter(m.To.Id, m.To.Name, to != null && to.Woman, town, kind, cents, _placeTown, MetresBetween(_placeTown, town), gameNow);
            if (w == null)
            {
                Hint("That letter won't go.", 2500);
                _letterMenu = null;
                return;
            }
            if (cents > 0)
                MONEY._MONEY_DECREMENT_CASH_BALANCE(cents);
            _lettersWritten++;
            Hint("Your letter to " + m.To.Name + " is in the post.", 4000);
            _letterMenu = null;
        }

        /// The post's distance between two towns, from the towns' table: 0 for the same, far for one unknown.
        private static float MetresBetween(string a, string b)
        {
            if (a == null || b == null)
                return 9999f;
            if (a == b)
                return 0f;
            float[] pa, pb;
            if (!Rumours.Towns.TryGetValue(a, out pa) || !Rumours.Towns.TryGetValue(b, out pb))
                return 9999f;
            float dx = pa[0] - pb[0], dy = pa[1] - pb[1];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// Drawn after the reader each frame: the hint under the paper, or the menu over it.
        private void LetterMenuDraw()
        {
            if (_cfg == null || !_reader.Open)
                return;
            LetterMenu m = _letterMenu;
            if (m == null)
            {
                Paper p = _reader.Current;
                bool canReply = _cfg.LetterReplies && p != null && _store != null && Replies.Answerable(p) && !Replies.Answered(_store.Replies, p.Id);
                bool canWrite = _cfg.WriteLetters && InPostTown();
                bool pad = !PAD.IS_USING_KEYBOARD_AND_MOUSE(0);
                string ans = pad ? "X: answer" : "R: answer", wr = pad ? "Y: write" : "L: write";
                _reader.Extra = canReply && canWrite ? ans + "     " + wr : canReply ? ans : canWrite ? wr : null;
                return;
            }
            MenuBoxDraw(m);
        }

        /// A small keyboard menu in the middle of the screen: the letters', the trial's, the gallows' (WestworldScript.Trials.cs).
        private void MenuBoxDraw(LetterMenu m)
        {
            float s = 0.32f, lh = Draw.LineHeight(s), x = 0.42f, fs = s * 0.85f, flh = Draw.LineHeight(fs);
            if (m.Lines == null || m.LinesSel != m.Sel)
            {
                m.Lines = new string[m.Rows.Count];
                m.Wide = Draw.Width(m.Title, s * 1.05f);
                for (int i = 0; i < m.Rows.Count; i++)
                {
                    InteractOption o = m.Rows[i];
                    string row = o.Label + (o.Why != null ? " (" + o.Why + ")" : "");
                    m.Lines[i] = (i == m.Sel ? "> " : "  ") + row;
                    m.Wide = Math.Max(m.Wide, Draw.Width(m.Lines[i], s));
                }
                m.Wide = Math.Max(m.Wide, Draw.Width(MenuFoot(), fs));
                m.LinesSel = m.Sel;
            }
            int n = m.Rows.Count;
            float y = 0.5f - (n + 1) * lh / 2f;
            Draw.Rect(x - 0.012f, y - 0.008f, m.Wide + 0.024f, lh * (n + 1) + flh + 0.02f, 24, 18, 12, 230);
            Draw.Text(m.Title, x, y, s * 1.05f, 236, 214, 160);
            for (int i = 0; i < n; i++)
            {
                bool sel = i == m.Sel, closed = m.Rows[i].Why != null;
                int c = closed ? 130 : sel ? 255 : 210;
                Draw.Text(m.Lines[i], x, y + (i + 1) * lh + 0.004f, s, c, c, closed ? 130 : sel ? 200 : 210);
            }
            Draw.Text(MenuFoot(), x, y + (n + 1) * lh + 0.006f, fs, 165, 155, 135);
        }

        private static string MenuFoot()
        {
            return PAD.IS_USING_KEYBOARD_AND_MOUSE(0) ? "Enter: choose   Backspace: back" : "A: choose   B: back";
        }

        // ---- the meet ----

        private void LettersStep(int now)
        {
            if (_aLetters == null)
                _aLetters = () => LettersTick(_now);
            Safe("letters", _aLetters);
        }

        private void LettersTick(int now)
        {
            if (_letterMenu != null && !_reader.Open)
                _letterMenu = null;   // the reader closed under the menu (a pad's B): nothing left over for the next paper
            if (_cfg == null || !_cfg.LetterReplies || _engine == null || _store == null)
                return;
            if (_meet != null)
            {
                MeetRunStep(now);
                return;
            }
            if (now < _lettersNext)
                return;
            _lettersNext = now + 5000;
            if (_placeTown == null || MISC.GET_MISSION_FLAG())
                return;
            Reply r = _engine.MeetDue(_placeTown, _store.LastGameMin);
            if (r != null)
                MeetCast(r, now);
        }

        /// The writer, waiting: a free unseen grown body of their sex, 30-120 m off, becomes them.
        private void MeetCast(Reply r, int now)
        {
            Ped me = Game.Player.Character;
            // one of ours already walking the town in a body of their own: that body, never a second one with the same name
            Resident known = r.ToId != null && Resident.IsResident(r.ToId) ? _store.FindResident(r.ToId) : (r.CastId != null ? _store.FindResident(r.CastId) : null);
            if (known != null)
            {
                int bound = 0;
                foreach (KeyValuePair<int, ResBind> kv in _resBind)
                    if (kv.Value.R == known && ENTITY.DOES_ENTITY_EXIST(kv.Key) && !ENTITY.IS_ENTITY_DEAD(kv.Key))
                    {
                        bound = kv.Key;
                        break;
                    }
                if (bound != 0)
                {
                    Ped body = new Ped(bound);
                    float bd = body.Position.DistanceTo(me.Position);
                    if (bd < 150f)
                        MeetBind(r, body, known, false, now, bd);
                    return;   // theirs is the body that waits; farther off, again in five seconds
                }
            }
            Ped best = null;
            float bestD = 120f;
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || !IsGrownUp(p) || PED.IS_PED_MALE(h) == r.ToWoman)
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d < 30f || d >= bestD || ENTITY.IS_ENTITY_ON_SCREEN(h) || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17))
                    continue;
                if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || ResidentWhy(p) != null || PostKindOf(p) != null || Natives.StoryModel(p) ||
                    _resBind.ContainsKey(h) || _director.Involves(h) || !MayDirect(p) || (_bridge != null && _bridge.Looked(h)))
                    continue;
                best = p;
                bestD = d;
            }
            if (best == null)
                return;   // nobody free and unseen just now: again in five seconds
            int bh = best.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(bh);
            long gameNow = _store.LastGameMin;
            Resident res = known;
            if (res == null)
            {
                res = _engine.MeetResident(model, r.ToWoman, r.Town, "come to meet you", bh + "|meet|" + Game.GameTime, gameNow, false);
                res.Name = r.ToName;
                _engine.ForgetPerson(res.Id);
                r.CastId = res.Id;   // the same one next time, never another with the name
            }
            else if ((res.Regular || res.Outfit > 0) && res.Model == model)
            {
                PED._EQUIP_META_PED_OUTFIT_PRESET(bh, res.Outfit, false);   // themselves, as the town knows them (a preset of their own build only)
                PED._UPDATE_PED_VARIATION(bh, false, true, true, true, false);
            }
            _engine.KeepResident(res, gameNow);
            _resBind[bh] = new ResBind { R = res, Model = model };
            MeetBind(r, best, res, true, now, bestD);
        }

        /// The writer's body, waiting where it stands, the grey mark on it.
        private void MeetBind(Reply r, Ped body, Resident res, bool fresh, int now, float d)
        {
            int bh = body.Handle;
            PED.SET_PED_KEEP_TASK(bh, true);
            TASK.TASK_STAND_STILL(bh, 300000);   // they wait where they stand
            Blip dot = null;
            try
            {
                dot = body.AddBlip(BlipType.CompanionGray);
                dot.Label = r.ToName;
            }
            catch { }
            _meet = new MeetRun { Reply = r, Handle = bh, Body = body, Fresh = fresh && res != null && res.Id == r.CastId, Since = now, Dot = dot, Name = r.ToName };
            _meetsCast++;
            Log.Write("MEET " + r.ToName + " waits in " + _placeTown + ", " + (int)d + " m off (" + (fresh ? "a body cast" : "their own body") + "; the grey mark on the map says where; " + r.Letter + ", " + r.Tone + ")");
            Hint(r.ToName + " is waiting somewhere in " + _placeTown + "; the grey mark on the map says where.", 7000);
        }

        private void MeetRunStep(int now)
        {
            MeetRun m = _meet;
            int h = m.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
            {
                MeetEnd(false, "the body went", false);   // cast again while the hour lasts
                return;
            }
            if (!Replies.MeetOpen(m.Reply, _store.LastGameMin))
            {
                MeetEnd(false, "the hour passed", true);
                return;
            }
            Ped me = Game.Player.Character;
            if (m.SecondAt > 0)
            {
                if (now >= m.SecondAt)
                {
                    SpeechCaption(Person.FirstOf(m.Name), m.Second, h, 4500, true);
                    MeetEnd(true, "met", true);
                }
                return;
            }
            float d = (m.Body ?? (m.Body = new Ped(h))).Position.DistanceTo(me.Position);
            if (d > 3f)
            {
                m.NearSince = 0;
                return;
            }
            if (m.NearSince == 0)
                m.NearSince = now;
            else if (now - m.NearSince > 1200)
                MeetTalk(m, me, now);
        }

        private void MeetTalk(MeetRun m, Ped me, int now)
        {
            int h = m.Handle;
            string kin = null;
            if (m.Reply.Letter == "grief")
            {
                Avenger a = _store.Avengers.FirstOrDefault(x => x.Name == m.Reply.ToName);
                if (a != null)
                    kin = Engine.KinWord(a.Relation, a.DeadWoman);   // bare: the lines say "my" themselves
            }
            string[] lines = Replies.MeetLines(m.Reply.Letter, kin, m.Reply.Id);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 4000, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 9000, 0, 51, 0);
            Speech.SayTag(h, "greet", me.Handle);
            SpeechCaption(Person.FirstOf(m.Name), lines[0], h, 4500, true);
            m.Second = lines[1];
            m.SecondAt = now + 4500;
            Log.Write("MEET " + m.Name + " speaks to you: " + lines[0]);
        }

        private void MeetEnd(bool met, string why, bool over)
        {
            MeetRun m = _meet;
            _meet = null;
            if (m == null)
                return;
            try
            {
                if (m.Dot != null)
                    m.Dot.Delete();
            }
            catch { }
            if (ENTITY.DOES_ENTITY_EXIST(m.Handle) && !ENTITY.IS_ENTITY_DEAD(m.Handle))
            {
                PED.SET_PED_KEEP_TASK(m.Handle, false);
                WanderLater(m.Handle, Game.GameTime + (met ? 7000 : 500));
            }
            if (m.Fresh)
                _resBind.Remove(m.Handle);   // a body borrowed for the meet alone goes back to being nobody
            if (over)
                _engine.MeetOver(m.Reply, met, _store.LastGameMin);
            if (met)
                _meetsMet++;
            Log.Write("MEET " + m.Name + ": " + why);
        }

        /// The add-on stopping, or a wipe: the waiting writer let go, the mark off the map.
        private void MeetAbort()
        {
            if (_meet != null)
                MeetEnd(false, "the add-on stopped", false);
        }

        private string LettersStatus()
        {
            if (_cfg == null || !_cfg.LetterReplies)
                return "off ([World] LetterReplies)";
            return _engine.RepliesStatus(_store.LastGameMin) + "; this session: " + _lettersSent + " answers sent, " + _lettersWritten + " letters written (" + (_cfg.WriteLetters ? "on" : "off") + "), " +
                   _meetsCast + " meets cast, " + _meetsMet + " met" + (_meet != null ? "; waiting now: " + _meet.Name : "");
        }
    }
}
