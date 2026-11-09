// Thoughts above the head (0.6.2, [Ticker] ThoughtsOverHeads; Lex: "move the subtitles to above the NPCs head in a
// smaller font to show their thoughts easily"): the ticker's thought of someone in view is drawn over that person, small,
// in a dark box, instead of on the bar at the bottom. The ticker still decides whose turn it is and for how long;
// anything that isn't one person's thought (news, the summary) stays on the bar. Since 2026-10-27 (Lex: thoughts had
// come back to the bar) a thought is never drawn on the bar while this is on: one whose thinker is off screen or
// unknown waits, and after OverHeadWaitMs the ticker moves on to the next.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const float ThoughtAboveHead = 0.62f;   // metres above the head bone: over our bars and enRAGE's name card
        private const float ThoughtWidth = 0.20f;       // share of the screen width one line may take
        private const int ThoughtLines = 4;
        private const int OverHeadWaitMs = 1500;        // a thought whose thinker is out of view waits this long, then the ticker moves on

        private TickerLine _overHeadLaid, _overHeadCounted;
        private List<string> _overHeadParts = new List<string>();
        private float _overHeadScale, _overHeadWidth;
        private int _overHeadShown, _overHeadUnseen, _overHeadUnseenSince = -1;
        private TickerLine _overHeadWaiting;
        private string _overHeadLast;
        private bool _overHeadDrawn;
        private int _overHeadTopHandle, _overHeadTopAt = -1;   // whose thought box was drawn this frame, and its top (screen),
        private float _overHeadTopY;                            // so the Show minds card can sit clear above it
        private Action _aOverHead;

        /// Draws the ticker's line over its thinker's head. False when it can't be (not one person's thought, the
        /// setting off, the person gone or off screen), and the bar shows it instead.
        private bool ThoughtOverHead(TickerLine line, int now)
        {
            if (!_cfg.ThoughtsOverHeads || line == null || line.Kind != "near" || line.Handle == 0 || string.IsNullOrEmpty(line.Text))
                return false;
            int h = line.Handle;
            Vector3 head = Vector3.Zero;
            float x = 0f, y = 0f;
            bool can = ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && ENTITY.IS_ENTITY_ON_SCREEN(h);
            if (can)
            {
                head = PED.GET_PED_BONE_COORDS(h, 21030, 0f, 0f, 0f);
                can = ScreenAt(head + new Vector3(0f, 0f, ThoughtAboveHead), out x, out y);
            }
            if (!can)
            {
                CountOverHead(line, false);
                return false;
            }
            float scale = _cfg.ThoughtsOverHeadScale * Math.Max(0.75f, Feelings.SizeAt(head.DistanceTo(Game.Player.Character.Position)));
            if (line != _overHeadLaid || Math.Abs(scale - _overHeadScale) > 0.004f)
            {
                _overHeadLaid = line;
                _overHeadScale = scale;
                List<string> parts = Draw.Wrap("\"" + Translations.Of(line.Text.Trim()).Trim().Trim('"') + "\"", scale, ThoughtWidth);   // the player's translation, once a line (Run 3, item 8)
                if (parts.Count > ThoughtLines)
                    parts = parts.Take(ThoughtLines - 1).Concat(new[] { Draw.Fit(string.Join(" ", parts.Skip(ThoughtLines - 1)), scale, ThoughtWidth) }).ToList();
                _overHeadParts = parts;
                _overHeadWidth = parts.Count == 0 ? 0f : parts.Max(p => Draw.Width(p, scale));
            }
            if (_overHeadParts.Count == 0)
                return false;
            float lineH = Draw.LineHeight(scale), padX = 0.006f, padY = 0.003f, w = _overHeadWidth;
            float top = y - lineH * _overHeadParts.Count;   // the box sits on the point and grows upward; a wake stage tag goes on top of it (Lex, 2026-10-30)
            int age = Math.Max(0, now - line.ShownAt);
            int a = line.ShownAt > now ? 255 : Math.Min(255, 60 + age * 255 / 400);
            Draw.Rect(x - w / 2f - padX, top - padY, w + padX * 2f, lineH * _overHeadParts.Count + padY * 2f, 12, 10, 8, 150 * a / 255);
            Draw.Rect(x - w / 2f - padX, top - padY, w + padX * 2f, 0.0015f, 222, 182, 96, 200 * a / 255);   // the gold edge the bar has
            for (int i = 0; i < _overHeadParts.Count; i++)
                Draw.Text(_overHeadParts[i], x - Draw.Width(_overHeadParts[i], scale) / 2f, top + i * lineH, scale, 244, 238, 222, a);
            _overHeadTopHandle = h;
            _overHeadTopY = top - padY;
            _overHeadTopAt = now;
            _mainX0 = x - w / 2f - padX;
            _mainX1 = x + w / 2f + padX;
            _mainH = lineH * _overHeadParts.Count + padY * 2f;
            _overHeadWaiting = null;   // in view again: a later wait starts afresh
            CountOverHead(line, true);
            return true;
        }

        // ---- several at once (FIVEDAY2 item 24; Core/TickerBrain.cs says who and when, this draws them). Same box as the main thought, three lines at most, and silent: one
        // person's murmur is a detail, three at once is a noise. A box that would sit on another goes above it, easing there rather than jumping. The frame's text is shared with
        // the names, drawn after this, so a side box needs SideTextRoom pieces left over; short of that it waits, and the names win.
        private const int SideLines = 3, SideTextRoom = 26, MostBoxes = 4;
        private sealed class SideBox
        {
            public TickerLine Laid;
            public List<string> Parts = new List<string>();
            public readonly List<string> Formed = new List<string>(4);   // each line made ready for the game once, not each frame (TENHOUR item 1)
            public readonly List<float> PartWidths = new List<float>(4);
            public float Scale, Width, Shift, WrapScale, WidthWrapped;
            public int UnseenSince = -1;
            // this frame's
            public float X, Top, Height;
            public int Alpha;
        }
        private readonly SideBox[] _sideBoxes = { new SideBox(), new SideBox(), new SideBox() };
        private readonly SideBox[] _sideNow = new SideBox[MostBoxes];
        private readonly float[] _boxX0 = new float[MostBoxes], _boxX1 = new float[MostBoxes], _boxTop = new float[MostBoxes], _boxH = new float[MostBoxes];
        private float _mainX0, _mainX1, _mainH;
        private const float SideRewrapDrift = 0.02f;
        private int _sideShown, _sideMost, _sideFrames, _sidePasses;
        private TickerLine _sideCounted;
        private long _sideTicks, _sidePeak;

        /// New side thoughts this update: no act's or turn's thought of theirs on top of them, and counted for F7.
        private void SideThoughtsCame(int now)
        {
            IList<TickerLine> side = _ticker.Side;
            for (int i = 0; i < side.Count; i++)
                if (side[i].ShownAt == now && side[i] != _sideCounted)
                {
                    _sideCounted = side[i];
                    _sideShown++;
                    NoteTheirThought(side[i].Handle, now, side[i].Text);
                    ThreadThoughtShown(side[i].Key, now);
                }
            ThoughtsMinuteLine(now);
            PairThoughtsStep(now);   // two who think about each other (WestworldScript.PairThoughts.cs)
        }

        // the thought board's numbers for the log, once a minute: what the last line has told already, and when it was
        private int _thoughtsMainShown, _roadThoughtsMade, _thoughtsMinuteAt, _toldMain, _toldSide, _toldAsides, _toldRoad, _toldFollow;

        /// The boxes over heads as a line in the log, so a session can be read afterwards (TENHOUR item 2; asides, road thoughts and follow-ons wrote nothing an ordinary log would show).
        private void ThoughtsMinuteLine(int now)
        {
            if (now - _thoughtsMinuteAt < 60000 && now >= _thoughtsMinuteAt)
                return;
            _thoughtsMinuteAt = now;
            int main = _thoughtsMainShown - _toldMain, beside = _sideShown - _toldSide, asides = _asidesShown - _toldAsides, road = _roadThoughtsMade - _toldRoad, follow = _threadFollows - _toldFollow;
            if (main + beside + asides + road + follow == 0)
                return;
            _toldMain = _thoughtsMainShown;
            _toldSide = _sideShown;
            _toldAsides = _asidesShown;
            _toldRoad = _roadThoughtsMade;
            _toldFollow = _threadFollows;
            double f = Stopwatch.Frequency / 1000.0;
            Log.Write("THOUGHTS minute: " + (main + beside) + " over heads (" + main + " main, " + beside + " beside), at most " + _sideMost + " beside it so far; " + asides + " while someone talked, " + road + " from the saddle, " + follow + " following on" +
                      (_sideFrames > 0 ? "; the side boxes " + (_sideTicks / f / _sideFrames).ToString("0.000") + " ms a frame on average, " + (_sidePeak / f).ToString("0.000") + " ms at worst" : ""));
        }

        /// The box already showing this line, or a free one.
        private SideBox BoxFor(TickerLine line, IList<TickerLine> side)
        {
            SideBox free = null;
            for (int i = 0; i < _sideBoxes.Length; i++)
            {
                SideBox b = _sideBoxes[i];
                if (b.Laid == line)
                    return b;
                if (free == null && (b.Laid == null || !side.Contains(b.Laid)))
                    free = b;
            }
            if (free != null)
            {
                free.Laid = line;
                free.Parts.Clear();
                free.Scale = free.WrapScale = -1f;   // laid out the first time it can be placed
                free.Shift = 0f;
                free.UnseenSince = -1;
            }
            return free;
        }

        /// Every frame the thoughts are drawn: where each side thought goes, clear of the main one and of each other, and then the boxes.
        private void SidesOverHead(int now)
        {
            IList<TickerLine> side = _ticker.Side;
            if (side.Count == 0)
                return;
            long t0 = Stopwatch.GetTimestamp();
            int n = 0, used = 0;
            Vector3 you = Game.Player.Character.Position;
            if (_overHeadTopAt == now && _overHeadDrawn)
            {
                _boxX0[0] = _mainX0;
                _boxX1[0] = _mainX1;
                _boxTop[0] = _overHeadTopY;
                _boxH[0] = _mainH;
                _sideNow[0] = null;
                n = 1;
            }
            for (int i = side.Count - 1; i >= 0 && n < MostBoxes; i--)
            {
                TickerLine line = side[i];
                SideBox box = BoxFor(line, side);
                if (box == null)
                    continue;
                if (!PlaceSideBox(line, box, now, you))
                {
                    if (box.UnseenSince < 0)
                        box.UnseenSince = now;
                    else if (now - box.UnseenSince >= OverHeadWaitMs)
                    {
                        box.UnseenSince = -1;
                        box.Laid = null;
                        _ticker.DropSide(line);   // gone from view: its place is for someone who can be seen
                        _sidePasses++;
                    }
                    continue;
                }
                box.UnseenSince = -1;
                if (Draw.Left - used - box.Parts.Count < SideTextRoom)
                    continue;   // the names come first
                used += box.Parts.Count;
                _boxX0[n] = box.X - box.Width / 2f - 0.006f;
                _boxX1[n] = box.X + box.Width / 2f + 0.006f;
                _boxTop[n] = box.Top;
                _boxH[n] = box.Height;
                _sideNow[n] = box;
                n++;
            }
            ThoughtBoxes.Clear(_boxX0, _boxX1, _boxTop, _boxH, n, 0.004f);
            int drawn = 0;
            for (int i = 0; i < n; i++)
            {
                SideBox box = _sideNow[i];
                if (box == null)
                    continue;
                box.Shift += (_boxTop[i] - box.Top - box.Shift) * 0.2f;   // eases there: the two heads drifting apart don't make it jump
                DrawSideBox(box, box.Top + box.Shift);
                drawn++;
            }
            if (drawn == 0)
                return;
            long spent = Stopwatch.GetTimestamp() - t0;
            _sideTicks += spent;
            _sideFrames++;
            if (spent > _sidePeak)
                _sidePeak = spent;
            int up = drawn + (_overHeadTopAt == now && _overHeadDrawn ? 1 : 0);
            if (up > _sideMost)
                _sideMost = up;
        }

        /// This frame's position for a side thought; false when its thinker can't be seen.
        private bool PlaceSideBox(TickerLine line, SideBox box, int now, Vector3 you)
        {
            int h = line.Handle;
            if (h == 0 || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !ENTITY.IS_ENTITY_ON_SCREEN(h))
                return false;
            Vector3 head = PED.GET_PED_BONE_COORDS(h, 21030, 0f, 0f, 0f);
            float x, y;
            if (!ScreenAt(head + new Vector3(0f, 0f, ThoughtAboveHead), out x, out y))
                return false;
            float scale = _cfg.ThoughtsOverHeadScale * Math.Max(0.75f, Feelings.SizeAt(head.DistanceTo(you)));
            if (line != box.Laid || Math.Abs(scale - box.WrapScale) > SideRewrapDrift)   // the lines are worked out again only when they could really break differently; between times the same ones are drawn a shade bigger or smaller
            {
                box.Laid = line;
                box.WrapScale = scale;
                List<string> parts = Draw.Wrap("\"" + Translations.Of(line.Text.Trim()).Trim().Trim('"') + "\"", scale, ThoughtWidth);
                if (parts.Count > SideLines)
                    parts = parts.Take(SideLines - 1).Concat(new[] { Draw.Fit(string.Join(" ", parts.Skip(SideLines - 1)), scale, ThoughtWidth) }).ToList();
                box.Parts = parts;
                box.Formed.Clear();
                box.PartWidths.Clear();
                box.WidthWrapped = 0f;
                for (int i = 0; i < parts.Count; i++)
                {
                    float w = Draw.Width(parts[i], scale);
                    box.Formed.Add(Draw.Formed(parts[i]));
                    box.PartWidths.Add(w);
                    if (w > box.WidthWrapped)
                        box.WidthWrapped = w;
                }
            }
            box.Scale = scale;
            box.Width = box.WidthWrapped * scale / box.WrapScale;
            if (box.Parts.Count == 0)
                return false;
            float lineH = Draw.LineHeight(scale);
            box.X = x;
            box.Height = lineH * box.Parts.Count + 0.006f;
            box.Top = y - lineH * box.Parts.Count - 0.003f;
            int age = Math.Max(0, now - line.ShownAt), left = line.Until - now;
            int a = Math.Min(255, 60 + age * 255 / 400);
            box.Alpha = left < 500 ? Math.Min(a, Math.Max(0, left) * 255 / 500) : a;   // out as gently as in: these come and go on their own, with nothing after them
            return true;
        }

        private void DrawSideBox(SideBox box, float top)
        {
            float padX = 0.006f, lineH = Draw.LineHeight(box.Scale), w = box.Width, a = box.Alpha;
            Draw.Rect(box.X - w / 2f - padX, top, w + padX * 2f, box.Height, 12, 10, 8, (int)(140 * a / 255));
            Draw.Rect(box.X - w / 2f - padX, top, w + padX * 2f, 0.0015f, 222, 182, 96, (int)(170 * a / 255));
            float wide = box.Scale / box.WrapScale;
            for (int i = 0; i < box.Formed.Count; i++)
                Draw.TextFormed(box.Formed[i], box.X - box.PartWidths[i] * wide / 2f, top + 0.003f + i * lineH, box.Scale, 232, 226, 210, box.Alpha);
        }

        /// F7: how many, and what they cost.
        private string SideStatus()
        {
            if (_cfg.ThoughtsAtOnce <= 1)
                return "singly ([Ticker] ThoughtsAtOnce 1)";
            double f = Stopwatch.Frequency / 1000.0;
            return "up to " + _cfg.ThoughtsAtOnce + " at once: " + _sideShown + " beside the main one, at most " + _sideMost + " up together, " + _sidePasses + " dropped out of view" +
                   (_sideFrames > 0 ? "; drawing them: " + (_sideTicks / f / _sideFrames).ToString("0.000") + " ms a frame on average, " + (_sidePeak / f).ToString("0.000") + " ms at worst" : "");
        }

        /// A thought, while ThoughtsOverHeads is on: it goes over a head or nowhere, never on the bar.
        private bool IsOverHeadKind(TickerLine line)
        {
            return _cfg.ThoughtsOverHeads && line != null && line.Kind == "near";
        }

        /// A thought that couldn't go over its thinker's head this frame: true once it has waited OverHeadWaitMs, and the
        /// ticker should move on to the next.
        private bool OverHeadOnly(TickerLine line, int now)
        {
            if (!IsOverHeadKind(line))
            {
                _overHeadWaiting = null;
                return false;
            }
            if (line != _overHeadWaiting)
            {
                _overHeadWaiting = line;
                _overHeadUnseenSince = now;
                return false;
            }
            if (now - _overHeadUnseenSince < OverHeadWaitMs)
                return false;
            _overHeadWaiting = null;
            return true;
        }

        /// Each line counted once, over a head or not shown (the thinker out of view), for F7; the first over a head is logged.
        private void CountOverHead(TickerLine line, bool overHead)
        {
            if (line == _overHeadCounted)
                return;
            _overHeadCounted = line;
            if (!overHead)
            {
                _overHeadUnseen++;
                return;
            }
            _overHeadShown++;
            _overHeadLast = line.Label;
            if (_overHeadShown == 1)
                Log.Write("THOUGHTS over heads: the first, " + line.Label + ": " + line.Text);
        }

        /// F7: whether thoughts go over heads, and how many did this session.
        private string OverHeadStatus()
        {
            if (!_cfg.ThoughtsOverHeads)
                return "off ([Ticker] ThoughtsOverHeads): on the bar; people within " + (int)_cfg.TickerNearbyMetres + " m";
            return "over heads (size " + _cfg.ThoughtsOverHeadScale.ToString("0.00") + "), people within " + (int)_cfg.TickerNearbyMetres + " m; this session " + _overHeadShown +
                   " over a head, " + _overHeadUnseen + " passed over (the thinker out of view; never on the bar)" + (_overHeadLast != null ? "; last over " + _overHeadLast : "") + "; " + SideStatus() + "; " + AsideStatus() + "; " + ThreadStatus();
        }
    }
}
