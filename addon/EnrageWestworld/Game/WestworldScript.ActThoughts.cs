// A thought with what their own mind chose to do, and with each encounter turn their own mind settles (the no-model run,
// item 1; roadmap 41; Lex, 2026-10-31: what they think and what they do agree; [Mind] ActThoughts). The lines are Core's
// (Core/ActThoughts.cs, each encounter's Turns), given by the Engine through the rules written thoughts keep (SaidToday,
// the frame guard: Engine.ActThoughts.cs); here only who and when: someone near and in view (NodInView, within
// PickMetres), on a real start of the act (a get-up's once they're up: WestworldScript.MindScenarios.cs S1Send; never a
// retry, never "at it already"), never when the AI model's own thought came with the choice (MindWho.ModelThought,
// StagedAsk.ModelThought), a person's act thought at most once in 3 minutes and never within 45 s of another thought of
// theirs (one up or waiting on the ticker, or one that came up lately: NoteTheirThought), one act thought every 8 s and one
// turn's every 3 s near you, and nothing while the ticker has more than two waiting. The waking (West World, any stage)
// think the waking's own thoughts, never these. Works without the model; in memory only.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<int, int> _theirThoughtAt = new Dictionary<int, int>();     // handle -> when a thought over them last came up on the ticker, or was pushed here
        private readonly Dictionary<int, string> _theirThoughtText = new Dictionary<int, string>();   // handle -> that thought's words: the same line again isn't "another" (Lex, 2026-11-01)
        private readonly Dictionary<string, int> _actThoughtAt = new Dictionary<string, int>();  // person id -> their last act thought
        private readonly Dictionary<string, int> _actThoughtHeld = new Dictionary<string, int>();
        private int _actThoughtNextAt = ActThoughts.Never, _turnThoughtNextAt = ActThoughts.Never;
        private int _actThoughtsShown, _turnThoughtsShown;
        private string _actThoughtLast, _turnThoughtLast;
        private bool _actThoughtGameFailed;

        /// A thought over someone came up on the ticker (anyone's, any feature's), or was pushed here: when, by handle.
        private void NoteTheirThought(int h, int now, string text = null)
        {
            if (h == 0)
                return;
            string was;
            if (text != null && _theirThoughtText.TryGetValue(h, out was) && was == text && _theirThoughtAt.ContainsKey(h))
                return;   // the same line of theirs again (the ticker came back round to it): not a new thought, so the gap doesn't start over
            if (text != null)
            {
                if (_theirThoughtText.Count > 300)
                    _theirThoughtText.Clear();
                _theirThoughtText[h] = text;
                if (_cfg != null && _cfg.HistoryNotes)
                    HistoryNoteFor(text, now);   // a thought of 1899's news: the paper's own sentence beside it (Core/HistoryNotes.cs)
            }
            if (_theirThoughtAt.Count > 300)
                PruneTimes(_theirThoughtAt, now, ActThoughts.TheirsGapMs);
            _theirThoughtAt[h] = now;
        }

        private int _historyNoteAt = -100000, _historyNotes;
        private readonly HashSet<string> _historyNoted = new HashSet<string>();   // the stories noted this session

        /// The almanac story behind a thought just shown, in the news box: once a story a session, one every 90 s at most.
        private void HistoryNoteFor(string text, int now)
        {
            if (now - _historyNoteAt < HistoryNotes.GapMs && now >= _historyNoteAt)
                return;
            AlmanacEvent e = HistoryNotes.StoryOf(text);
            if (e == null || !_historyNoted.Add(e.Key))
                return;
            _historyNoteAt = now;
            _historyNotes++;
            News(HistoryNotes.Title(e), e.Body, "news");
            Log.Write("HISTORY note: " + e.Key + " (" + e.Year + ")");
        }

        private int TheirThoughtAt(int h)
        {
            int at;
            return _theirThoughtAt.TryGetValue(h, out at) ? at : ActThoughts.Never;
        }

        /// Drops the times older than `ms` (or from before a load); everything, if that leaves too many.
        private static void PruneTimes<TKey>(Dictionary<TKey, int> times, int now, int ms)
        {
            var old = new List<TKey>();
            foreach (KeyValuePair<TKey, int> kv in times)
                if (now - kv.Value > ms || now < kv.Value)
                    old.Add(kv.Key);
            foreach (TKey k in old)
                times.Remove(k);
            if (times.Count > 300)
                times.Clear();
        }

        private void ActThoughtHeld(string why)
        {
            int n;
            _actThoughtHeld.TryGetValue(why, out n);
            _actThoughtHeld[why] = n + 1;
        }

        /// With West World on, someone at any stage of waking thinks the waking's own thoughts (the main loop's Hosts.Thought).
        private bool ActThoughtAwake(string id)
        {
            if (!_cfg.WestWorld || id == null || _store == null)
                return false;
            PersonState st = _store.Find(id);
            return st != null && st.WakeStage > 0;
        }

        /// The one an act is about by first name, when the two know each other (a tie between them, or the same home town),
        /// else null (the lines that name nobody are taken).
        private string ActWhoOf(MindWho w, int other)
        {
            MindWho o;
            if (other == 0 || !_mindWho.TryGetValue(other, out o) || o.Id == null || o.Name == null)
                return null;
            bool knows = w.P != null && o.P != null && w.P.Home != null && w.P.Home == o.P.Home;
            if (!knows)
                foreach (SceneTie t in _store.Ties)
                    if (t.Has(w.Id) && t.Has(o.Id))
                    {
                        knows = true;
                        break;
                    }
            return knows ? Person.FirstOf(o.Name) : null;
        }

        private int _worryActThoughts, _stressThoughts;
        private readonly HashSet<string> _stressToldBreak = new HashSet<string>();   // id|day: the breakdown's own thought said once (this session)

        /// Broken by the week's load (Core/StressLoad.cs; FIVEDAY item 17): the breakdown's own thought first, once, then when the
        /// act is the habit's, its thought seven times in ten; null otherwise.
        private string StressActThought(MindWho w, string act)
        {
            if (w == null || w.Id == null)
                return null;
            long day = _store.LastGameMin / 1440;
            string way = _engine.BrokenWay(w.Id, day);
            if (way == null)
                return null;
            string seed = w.Id + "|" + act + "|" + _store.LastGameMin / 60;
            string line = null;
            if (_stressToldBreak.Add(w.Id + "|" + way))   // once a breakdown this session (the Part 4 review: it came back each day of the week)
                line = StressLoad.BreakThought(way, seed);
            else if (StressLoad.Lean(way, act) > 0f && Voice.Chance(seed + "|stressact", 70))
                line = StressLoad.ActThought(way, act, seed);
            if (line != null)
                _stressThoughts++;
            return line;
        }

        /// When the act is one their worry leans to, its own thought (Core/WorryActs.cs; FIVEDAY item 7), seven times in ten, the
        /// worry's {who} filled in; null otherwise (the act's own thought, as before).
        private string WorryActThought(MindWho w, string act)
        {
            if (!_cfg.WorriesShown || w == null || w.P == null)
                return null;
            long day = _store.LastGameMin / 1440;
            string wk;
            int ws;
            bool wg;
            if (!InnerLife.WorryNow(w.P, day, out wk, out ws, out wg) || ws >= 3 || WorryActs.Lean(wk, ws, act) <= 0f)
                return null;
            string seed = w.Id + "|" + act + "|" + _store.LastGameMin / 60;
            if (!Voice.Chance(seed + "|worryact", 70))
                return null;
            string line = WorryActs.Thought(wk, act, seed);
            if (line == null)
                return null;
            if (line.Contains("{who}"))
            {
                string who = InnerLife.WorryFill(w.P, day, "{who}");
                if (string.IsNullOrEmpty(who) || who.Contains("{"))
                    return null;
                line = line.Replace("{who}", Person.FirstOf(who) ?? who);
            }
            _worryActThoughts++;
            return line;
        }

        /// An act their own mind chose has begun (MindActOn; a get-up's from S1Send once they're up): the thought that goes with
        /// it, over their head, when the caps let it. `why`: the choice's why (the need it serves); `retry`, `model`: the choice
        /// was tried again a minute on, or the model's own thought came with it.
        private void ActThoughtFor(MindWho w, int h, MindAct a, string why0, int now, bool retry, bool model)
        {
            if (!_cfg.ActThoughts || _engine == null || _store == null || w == null || w.Id == null || w.P == null || a == null)
                return;
            try
            {
                Ped me = Game.Player.Character;
                Ped p = new Ped(h);
                if (!p.Exists() || p.IsDead)
                    return;
                int last;
                if (!_actThoughtAt.TryGetValue(w.Id, out last))
                    last = ActThoughts.Never;
                string why = ActThoughts.WhyNot(retry, model, NodInView(me, p), p.Position.DistanceTo(me.Position), _cfg.PickMetres, now, last, TheirThoughtAt(h), _ticker.Waiting(h), _actThoughtNextAt, _ticker.FreshCount);
                if (why == null && ActThoughtAwake(w.Id))
                    why = "awake (the waking's own thoughts)";
                if (why != null)
                {
                    ActThoughtHeld(why);
                    return;
                }
                string who = ActThoughts.SlotOf(a.Kind) == "{who}" ? ActWhoOf(w, a.About) : null;
                string line = SurvivorActThought(w, a.Kind) ?? StressActThought(w, a.Kind) ?? WorryActThought(w, a.Kind) ?? _engine.ActThought(w.P, a.Kind, why0 ?? w.ChoiceWhy, who, w.Id + "|" + a.Kind + "|" + _store.LastGameMin / 60);   // a worry's own thought first (FIVEDAY item 7)
                if (line == null)
                {
                    ActThoughtHeld("nothing left for them today");
                    return;
                }
                _ticker.Push(w.Name + ", thinking", line, "near", h);
                NoteTheirThought(h, now);
                if (_actThoughtAt.Count > 300)
                    PruneTimes(_actThoughtAt, now, ActThoughts.PersonGapMs);
                _actThoughtAt[w.Id] = now;
                _actThoughtNextAt = now + ActThoughts.ActGapMs;
                _actThoughtsShown++;
                _actThoughtLast = w.Name + " (" + a.Kind + "): " + line;
                Log.Detail("MIND act thought: " + w.Name + " (" + Choosing.WordsOf(a.Kind) + ", " + (why0 ?? w.ChoiceWhy) + "): \"" + line + "\"");
            }
            catch (Exception ex)
            {
                if (!_actThoughtGameFailed)
                    Log.Write("MIND act thought failed, so none for now: " + ex.Message);
                _actThoughtGameFailed = true;
            }
        }

        /// An encounter's turn their own mind settled (StagedThink; the draws made outside StagedChoose: the domestic's
        /// neighbour, the hogtied men's leader and the captive's later calls): its thought over their head when the caps let
        /// it. `id` null for someone the add-on keeps nothing of (a stranger); `other` the other in it, for a line that names
        /// them. True when it showed.
        private bool TurnThoughtFor(int h, string name, string id, string role, string option, string other, int now, bool besideReason = false)
        {
            if (!_cfg.ActThoughts || _engine == null || name == null || ActThoughts.TurnPool(role, option) == null)
                return false;
            try
            {
                Ped me = Game.Player.Character;
                Ped p = new Ped(h);
                if (!p.Exists() || p.IsDead)
                    return false;
                // beside the encounter's own reason (the callout, the domestic: Lex, 2026-11-01, "both"): it follows that thought on the ticker
                string why = ActThoughts.TurnWhyNot(false, NodInView(me, p), p.Position.DistanceTo(me.Position), _cfg.PickMetres, now, besideReason ? ActThoughts.Never : TheirThoughtAt(h), !besideReason && _ticker.Waiting(h), _turnThoughtNextAt, besideReason ? 0 : _ticker.FreshCount);
                if (why == null && ActThoughtAwake(id))
                    why = "awake (the waking's own thoughts)";
                if (why != null)
                {
                    ActThoughtHeld("a turn: " + why);
                    return false;
                }
                string line = _engine.TurnThought(id, role, option, other, (id ?? "h" + h) + "|" + role + "|" + option + "|" + now / 60000);
                if (line == null)
                {
                    ActThoughtHeld("a turn: nothing left for them today");
                    return false;
                }
                _ticker.Push(name + ", thinking", line, "near", h);
                NoteTheirThought(h, now);
                _turnThoughtNextAt = now + ActThoughts.TurnGapMs;
                _turnThoughtsShown++;
                _turnThoughtLast = name + " (" + role + ", " + option + "): " + line;
                Log.Detail("ENCOUNTER thought: " + name + " (" + role + ", " + option + "): \"" + line + "\"");
                return true;
            }
            catch (Exception ex)
            {
                if (!_actThoughtGameFailed)
                    Log.Write("ENCOUNTER turn thought failed, so none for now: " + ex.Message);
                _actThoughtGameFailed = true;
                return false;
            }
        }

        /// F7: how many acts and turns came with a thought this session, the last of each, and why the others didn't.
        private string ActThoughtsStatus()
        {
            if (!_cfg.ActThoughts)
                return "off ([Mind] ActThoughts)";
            return _actThoughtsShown + " acts with a thought this session" + (_actThoughtLast != null ? " (last: " + _actThoughtLast + ")" : "") + ", " + _turnThoughtsShown + " encounter turns" +
                   (_turnThoughtLast != null ? " (last: " + _turnThoughtLast + ")" : "") + "; not shown: " +
                   (_actThoughtHeld.Count == 0 ? "none" : string.Join(", ", _actThoughtHeld.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   (_engine != null ? "; lines given " + (_engine.ActThoughtsGiven + _engine.TurnThoughtsGiven) + ", asks with nothing left " + (_engine.ActThoughtsNone + _engine.TurnThoughtsNone) : "") +
                   " (written lines, no AI model needed; with the model answering, its own thought comes first)";
        }
    }
}
