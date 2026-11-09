// Decides what the bottom ticker says. People near the player come first (their private thought about him);
// a brand-new notice gets one turn in between; with nobody near it rotates a summary and the latest news, or, with
// no summary given (news shown elsewhere), says nothing. GapMs puts a short blank between one line and the next.
// Pure logic: the game layer draws Current and feeds in who is near.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class TickerLine
    {
        public string Label;   // "Elihu Starling", "News", "RealNPCs"
        public string Text;
        public string Kind;    // near, news, summary
        public string Key;     // person key for near lines
        public int Handle;     // the thinker's ped for near lines, 0 if unknown (the game side marks them)
        public int ShownAt;    // game ms
        public int Until;      // a side thought's end, game ms (FIVEDAY2 item 24)
        public bool Fresh;     // a notice shown for the first time (drawn brighter)
    }

    public sealed class NearThought
    {
        public string Key;
        public string Name;
        public float Distance;
        public int Handle;          // the game's handle for the person, 0 if none (for a sound as the thought shows)
        public Func<string> Text;   // computed lazily, at most every ThoughtCacheMs per person
        public bool Beside;         // right next to the player: every other turn, and a new thought every BesideCacheMs
        public bool Urgent;         // in a loud moment (a fight, a flight, a gun on them: Core/Moments.cs): the first turn, and a new thought every UrgentCacheMs
    }

    public sealed class TickerBrain
    {
        public int SlotMs = 12000;
        public int ThoughtCacheMs = 90000;
        public int BesideCacheMs = 30000;
        public int UrgentCacheMs = 6000;
        public int HistorySize = 14;
        public int GapMs;             // a blank this long between one line and the next (0: none)

        // ---- several at once (FIVEDAY2 item 24; [Ticker] ThoughtsAtOnce): the main line goes on exactly as it was, and beside it up to Several - 1 more people's
        // thoughts over their own heads. Why: a street is a lot of people thinking at once, and one box every fifteen seconds reads as one person being
        // switched on at a time. They arrive in turn (StaggerMs), so the eye goes to each; nearest first, the loud moments before anyone.
        public int Several = 1;       // how many may be up at once, the main line included (1: only the main line)
        public int StaggerMs = 2500;  // at most one new side thought this often
        public int SideMs = 9000;     // how long one stays, before its reading time
        public int SideTries = 4;     // how many people are asked for a thought in one go, when the nearest have nothing new

        private readonly List<TickerLine> _fresh = new List<TickerLine>();
        private readonly List<TickerLine> _history = new List<TickerLine>();
        private readonly Dictionary<string, KeyValuePair<int, string>> _thoughts = new Dictionary<string, KeyValuePair<int, string>>();
        private int _nextAt;
        private bool _skipped;
        private int _rotation;        // index into history while nobody is near
        private int _nearTurn;        // which near person is next
        private bool _summaryNext = true;
        private string _lastNearKey;
        private TickerLine _prev;     // the last line shown, through any blank after it
        private TickerLine _live;     // a line said right now (a street scene within earshot): shown at once, for its own time, never in the history
        private readonly List<TickerLine> _side = new List<TickerLine>();
        private int _sideNextAt, _sideAskAt;
        private int _liveMs;

        public TickerLine Current { get; private set; }

        public int FreshCount { get { return _fresh.Count; } }

        /// The side thoughts up now (FIVEDAY2 item 24), oldest first. The game draws them over their thinkers' heads.
        public IList<TickerLine> Side { get { return _side; } }

        /// Whether a thought over this person is up now or waits its turn (pushed, not shown yet): read only, a loop over the
        /// few waiting (the no-model run, item 1: never a second thought of theirs on top of one).
        public bool Waiting(int handle)
        {
            if (handle == 0)
                return false;
            if (Current != null && Current.Handle == handle && Current.Kind == "near")
                return true;
            for (int i = 0; i < _fresh.Count; i++)
                if (_fresh[i].Handle == handle)
                    return true;
            for (int i = 0; i < _side.Count; i++)
                if (_side[i].Handle == handle)
                    return true;
            return false;
        }

        /// A new notice: it gets a turn soon and then joins the rotation.
        public void Push(string label, string text, string kind = "news", int handle = 0)
        {
            if (string.IsNullOrEmpty(text))
                return;
            text = text.Trim();
            if (_fresh.Any(x => x.Text == text) || (Current != null && Current.Text == text))
                return;
            _fresh.Add(new TickerLine { Label = label, Text = text, Kind = kind, Fresh = true, Handle = handle });
            if (_fresh.Count > 6)
                _fresh.RemoveAt(0);
        }

        /// A line being said now, within earshot (a street scene's beat, 2026-10-29, Lex: "the ticker showing the nearest scene playing out"):
        /// shown at once in place of whatever is up, for `forMs`, and not kept in the history (it was speech, not news).
        public void Say(string label, string text, int handle, int forMs)
        {
            if (string.IsNullOrEmpty(text))
                return;
            _live = new TickerLine { Label = label, Text = text.Trim(), Kind = "scene", Handle = handle };
            _liveMs = Math.Max(1500, forMs);
        }

        /// The player asked for the next line now (TickerNextKey). From someone's thought it moves on to the news,
        /// so asking again near the same person doesn't bring back the same thought.
        public void Skip()
        {
            _nextAt = 0;
            _skipped = true;
        }

        public void Clear()
        {
            _fresh.Clear();
            _history.Clear();
            _thoughts.Clear();
            _side.Clear();
            Current = null;
            _prev = null;
            _nextAt = 0;
        }

        /// Call a few times a second. near: people close to the player, any order.
        public TickerLine Update(int now, IList<NearThought> near, Func<string> summary)
        {
            TickerLine main = UpdateMain(now, near, summary);
            if (Several > 1)
                UpdateSide(now, near);
            else if (_side.Count > 0)
                _side.Clear();
            return main;
        }

        /// A side thought that couldn't be shown (its thinker went out of view): its place is free for someone who can be seen.
        public void DropSide(TickerLine line)
        {
            _side.Remove(line);
        }

        /// A private thought while someone else is talking (FIVEDAY2 item 25): up at once beside whatever's showing, in place of the oldest if there's no room. It's what they think as it's said, so it can't wait its turn.
        public bool Aside(string label, string text, string key, int handle, int forMs, int now)
        {
            if (Several < 2 || string.IsNullOrEmpty(text) || handle == 0 || IsUp(key, handle))
                return false;
            if (_side.Count >= Several - 1)
                _side.RemoveAt(0);
            _side.Add(new TickerLine { Label = label, Text = text.Trim(), Kind = "near", Key = key, Handle = handle, ShownAt = now, Until = now + Math.Max(2500, forMs) });
            _sideNextAt = Math.Max(_sideNextAt, now + StaggerMs);
            return true;
        }

        /// The next new side thought may come at once, and everyone may think again (the Testing row).
        public void SidesNow()
        {
            _sideNextAt = _sideAskAt = 0;
            _shown.Clear();
        }

        /// Whether this person's thought is already up, on the main line or beside it.
        private bool IsUp(string key, int handle)
        {
            if (Current != null && Current.Kind == "near" && ((key != null && Current.Key == key) || (handle != 0 && Current.Handle == handle)))
                return true;
            for (int i = 0; i < _side.Count; i++)
                if ((key != null && _side[i].Key == key) || (handle != 0 && _side[i].Handle == handle))
                    return true;
            return false;
        }

        /// Ends the side thoughts that are done, or whose person the main line has just come to; then, room and time allowing, brings up one more.
        private void UpdateSide(int now, IList<NearThought> near)
        {
            for (int i = _side.Count - 1; i >= 0; i--)
            {
                TickerLine s = _side[i];
                bool mainHasThem = Current != null && Current != s && Current.Kind == "near" && ((s.Key != null && Current.Key == s.Key) || (s.Handle != 0 && Current.Handle == s.Handle));
                if (now >= s.Until || now < s.ShownAt || mainHasThem)
                    _side.RemoveAt(i);
            }
            if (_side.Count >= Several - 1 || (now < _sideNextAt && _sideNextAt - now <= StaggerMs))
                return;
            int ms = SideMs;
            TickerLine next = SideFresh();
            if (next == null && near != null && (now >= _sideAskAt || _sideAskAt - now > 1000))
            {
                _sideAskAt = now + 500;   // not asked again for half a second (with an empty bar this runs every frame)
                // the loud moments first, then nearest; a few asked, since someone's thought may be the one they had a moment ago
                foreach (NearThought n in near.Where(x => x != null && x.Key != null && x.Handle != 0 && !IsUp(x.Key, x.Handle)).OrderByDescending(x => x.Urgent).ThenBy(x => x.Distance).Take(SideTries))
                {
                    next = NearLine(n, now);
                    if (next == null)
                        continue;
                    if (n.Urgent)
                        ms = Math.Min(ms, UrgentCacheMs);   // a fight's line moves with the fight
                    break;
                }
            }
            if (next == null)
                return;
            next.ShownAt = now;
            next.Until = now + ms + Math.Min(4000, next.Text.Length * 25);
            _side.Add(next);
            _sideNextAt = now + StaggerMs;
        }

        /// A thought pushed for someone (an event's, a turn's) gets a free place at once rather than waiting its turn on the main line.
        private TickerLine SideFresh()
        {
            for (int i = 0; i < _fresh.Count; i++)
            {
                TickerLine t = _fresh[i];
                if (t.Kind != "near" || t.Handle == 0 || IsUp(t.Key, t.Handle))
                    continue;
                _fresh.RemoveAt(i);
                t.Fresh = false;
                return t;
            }
            return null;
        }

        private TickerLine UpdateMain(int now, IList<NearThought> near, Func<string> summary)
        {
            if (_live != null)
            {
                TickerLine said = _live;
                _live = null;
                said.ShownAt = now;
                Current = _prev = said;
                _nextAt = now + _liveMs;
                return Current;
            }
            bool skip = _skipped;
            _skipped = false;
            List<NearThought> close = (near ?? new List<NearThought>()).Where(n => n != null && n.Key != null)
                .OrderBy(n => n.Distance).ToList();
            string nearest = close.Count > 0 ? close[0].Key : null;

            // someone new walked up: switch to them straight away (after a short minimum), with a little
            // hysteresis so two people at the same distance don't flicker
            bool newcomer = false;
            if (!skip && nearest != null && nearest != _lastNearKey && (Current == null || now - Current.ShownAt >= 3000))
            {
                NearThought shown = close.FirstOrDefault(c => c.Key == _lastNearKey);
                newcomer = shown == null || close[0].Distance + 2f < shown.Distance;
            }
            if (nearest == null)
                _lastNearKey = null;
            if (now < _nextAt && !newcomer && !(skip && Current == null))
                return Current;   // the line still showing, or the blank after it
            if (Current != null && GapMs > 0 && !newcomer && !skip)
            {
                Current = null;   // a short blank before the next line, so they don't run together
                _nextAt = now + GapMs;
                return null;
            }
            if (newcomer)
            {
                _lastNearKey = nearest;
                _nearTurn = 0;
            }

            TickerLine next = null;
            // the ticker key on someone's thought moves on to the news, when the news is on the ticker at all
            bool toNews = skip && _prev != null && _prev.Kind == "near" && (_fresh.Count > 0 || _history.Count > 0 || summary != null);
            if (close.Count > 0 && !toNews)
            {
                if (!newcomer && _fresh.Count > 0 && (_prev == null || _prev.Kind == "near"))
                    next = TakeFresh();
                // someone in a loud moment (fighting, fleeing, a gun on them) comes first, with a new line every few seconds; two
                // of them take turns (the one just shown waits for the other)
                NearThought urgent = close.FirstOrDefault(n => n.Urgent && !(_prev != null && _prev.Kind == "near" && _prev.Key == n.Key)) ?? close.FirstOrDefault(n => n.Urgent);
                if (next == null && urgent != null && !(_prev != null && _prev.Kind == "near" && _prev.Key == urgent.Key && now - _prev.ShownAt < UrgentCacheMs))
                    next = NearLine(urgent, now);
                // the person right beside the player gets every other turn
                NearThought beside = close.FirstOrDefault(n => n.Beside);
                if (next == null && beside != null && !(_prev != null && _prev.Kind == "near" && _prev.Key == beside.Key))
                    next = NearLine(beside, now);
                for (int i = 0; next == null && i < close.Count; i++)
                {
                    NearThought who = close[(_nearTurn + i) % close.Count];
                    next = NearLine(who, now);
                    if (next != null)
                        _nearTurn = _nearTurn + i + 1;
                }
            }
            if (next == null)
            {
                if (_fresh.Count > 0)
                {
                    next = TakeFresh();
                }
                else if (_summaryNext || _history.Count == 0)
                {
                    string s = summary != null ? summary() : null;
                    next = string.IsNullOrEmpty(s) ? null : new TickerLine { Label = "RealNPCs", Text = s, Kind = "summary" };
                    _summaryNext = false;
                }
                else
                {
                    TickerLine h = _history[_history.Count - 1 - (_rotation % _history.Count)];
                    _rotation++;
                    if (_rotation % Math.Max(1, Math.Min(_history.Count, 4)) == 0)
                        _summaryNext = true;
                    next = new TickerLine { Label = h.Label, Text = h.Text, Kind = h.Kind };
                }
            }

            if (next != null && next != Current)
            {
                next.ShownAt = now;
                Current = _prev = next;
                _nextAt = now + (next.Kind == "summary" ? Math.Max(4000, SlotMs * 2 / 3) : SlotMs) + Math.Min(6000, next.Text.Length * 25);
            }
            else
            {
                if (next == null)
                    Current = null;   // nothing to say: the bar goes away rather than keep an old line up
                _nextAt = now + 2000;
            }
            return Current;
        }

        private TickerLine TakeFresh()
        {
            TickerLine t = _fresh[0];
            _fresh.RemoveAt(0);
            _history.Add(new TickerLine { Label = t.Label, Text = t.Text, Kind = t.Kind });
            if (_history.Count > HistorySize)
                _history.RemoveAt(0);
            _rotation = 0;
            return t;
        }

        private readonly Dictionary<string, KeyValuePair<int, string>> _shown = new Dictionary<string, KeyValuePair<int, string>>();

        private TickerLine NearLine(NearThought n, int now)
        {
            TickerLine line = NearLineRaw(n, now);
            if (line == null)
                return null;
            // the same thought again so soon: give the slot to the news instead
            KeyValuePair<int, string> last;
            if (_shown.TryGetValue(n.Key, out last) && last.Value == line.Text && now - last.Key < CacheMs(n))
                return null;
            _shown[n.Key] = new KeyValuePair<int, string>(now, line.Text);
            if (_shown.Count > 200)
                _shown.Remove(_shown.OrderBy(kv => kv.Value.Key).First().Key);
            return line;
        }

        /// How long one person's thought is kept before a new one is asked for: a few seconds in a loud moment, half a
        /// minute right beside the player, a minute and a half otherwise.
        private int CacheMs(NearThought n)
        {
            return n.Urgent ? UrgentCacheMs : n.Beside ? BesideCacheMs : ThoughtCacheMs;
        }

        private readonly HashSet<string> _urgentCached = new HashSet<string>();   // whose cached thought was a loud moment's: dropped when the moment is over, not kept 90 s

        private TickerLine NearLineRaw(NearThought n, int now)
        {
            KeyValuePair<int, string> cached;
            string text = null;
            bool over = !n.Urgent && _urgentCached.Remove(n.Key);   // the fight's line doesn't outlive the fight
            if (!over && _thoughts.TryGetValue(n.Key, out cached) && now - cached.Key < CacheMs(n) && now >= cached.Key)
            {
                text = cached.Value;
            }
            else
            {
                if (n.Urgent)
                    _urgentCached.Add(n.Key);
                try
                {
                    text = n.Text != null ? n.Text() : null;
                }
                catch
                {
                    text = null;
                }
                _thoughts[n.Key] = new KeyValuePair<int, string>(now, text);
                if (_thoughts.Count > 200)
                    _thoughts.Remove(_thoughts.OrderBy(kv => kv.Value.Key).First().Key);
            }
            if (string.IsNullOrEmpty(text))
                return null;
            return new TickerLine { Label = n.Name, Text = text, Kind = "near", Key = n.Key, Handle = n.Handle };
        }

        /// Forget a cached thought (something just happened to this person).
        public void Refresh(string key)
        {
            if (key == null)
                return;
            _thoughts.Remove(key);
            for (int i = _side.Count - 1; i >= 0; i--)
                if (_side[i].Key == key)
                    _side.RemoveAt(i);
        }
    }

    /// Keeps thought boxes off each other (FIVEDAY2 item 24). Plain arithmetic, so it can be tested without the game.
    public static class ThoughtBoxes
    {
        /// Boxes in order of importance, in screen fractions (y grows downward): each moves up clear of those before it, the first never moves. Arrays, so nothing is made per frame.
        public static int Clear(float[] x0, float[] x1, float[] top, float[] h, int n, float gap)
        {
            int moved = 0;
            for (int i = 1; i < n; i++)
            {
                bool shifted = false;
                for (int tries = 0; tries <= n; tries++)
                {
                    int hit = -1;
                    for (int j = 0; j < i && hit < 0; j++)
                        if (x0[i] < x1[j] && x1[i] > x0[j] && top[i] < top[j] + h[j] + gap && top[i] + h[i] + gap > top[j])
                            hit = j;
                    if (hit < 0)
                        break;
                    top[i] = top[hit] - gap - h[i];
                    shifted = true;
                }
                if (shifted)
                    moved++;
            }
            return moved;
        }
    }
}
