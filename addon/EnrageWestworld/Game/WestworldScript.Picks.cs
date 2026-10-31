// The model picks what someone near does (Lex, 2026-10-30: "let it pick between a few safe options and go from there";
// Core/Language.Pick.cs holds the prompt and the reading of the answer). When the mind's choice changes for someone
// close by, it isn't acted on at once: the best three of the proper actions (and a plan's own step, when the one it's
// about is near) go to the model with who they are and how they feel, and what it picks is done. It waits 2.5 s at most;
// late, empty or unreadable, the mind's own choice is done, as before. Only with the server answering, only the nearest
// few, never a choice a strong feeling made. Behind [Mind] Language, LanguagePicks and MindLive.
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
        private sealed class PendingPick
        {
            public MindWho W;
            public int Handle, Deadline, AskedAt;
            public string Key, Mind;
            public List<string> Ids, Words;
            public Plan Plan;
            public LanguageRequest Req;   // for the thought's check (it came with the choice)
            public List<PairReason> Scenes;   // the person's own reasons offered too (ids "scene:N"; WestworldScript.MindScenes.cs)
            public bool Test;                 // the Testing row asked it
        }

        private const float PickNear = 15f;   // within this, a person's every new choice may be asked; past it ([Mind] PickMetres), half of them
        private const int PickWaitMs = 2500, PicksLoud = 40;   // how many wait at once and how far out: [Mind] PicksAtOnce, PickMetres (NIGHT3 item 2)
        private readonly List<PendingPick> _pending = new List<PendingPick>();
        private int _picksAsked, _picksTaken, _picksSame, _picksLate, _picksUnread, _picksPlan, _picksLogged;
        private string _pickLast;

        private bool PicksOn { get { return _cfg.MindLive && _cfg.Language && _cfg.LanguagePicks && _langReady && _engine != null; } }

        /// From MindChooseFor, when someone near has a new choice: the model is asked to pick, and true (it's acted on when the
        /// answer comes, or the wait is up); false to act on the mind's choice now.
        private bool PickAsk(MindWho w, int h, int now, ChoiceContext c)
        {
            if (!PicksOn || w == null || w.Id == null || w.Choice == null || _pending.Count >= _cfg.PicksAtOnce || w.Choice.StartsWith("urge:", StringComparison.Ordinal))
                return false;
            if (w.ChoiceWhy != null && w.ChoiceWhy.StartsWith("a feeling", StringComparison.Ordinal))
                return false;   // a feeling strongly felt decides, not the model
            if (_pending.Any(x => x.Handle == h) || IsMindActing(h))
                return false;
            Ped p = new Ped(h);
            float pd = p.Exists() ? p.Position.DistanceTo(Game.Player.Character.Position) : 999f;
            if (!p.Exists() || pd > _cfg.PickMetres || MindActWhyNot(p, h) != null)
                return false;
            if (pd > PickNear && (Voice.Hash(w.Id + "|" + now / 30000) & 1) == 1)
                return false;   // the outer ring: half its choices go to the model, the rest are the mind's own (NIGHT3 item 2)
            List<KeyValuePair<string, string>> ranked = Choosing.Ranked(w.Needs, w.Traits, c, Choosing.SeedFor(w.Id, _store.LastGameMin), 3);
            var ids = new List<string>();
            var words = new List<string>();
            foreach (KeyValuePair<string, string> kv in ranked)
            {
                ids.Add(kv.Key);
                words.Add(Choosing.WordsOf(kv.Key));
            }
            if (!ids.Contains(w.Choice))
            {
                ids.Insert(0, w.Choice);   // the mind's own is always among them
                words.Insert(0, Choosing.WordsOf(w.Choice));
            }
            int th;
            float td;
            Plan plan = PlanSeekable(w.Id, h, now, out th, out td);
            if (plan != null && ids.Count < Language.MostOptions + 1)
            {
                ids.Add("plan:seek");
                words.Add("goes to find " + Person.FirstOf(plan.TargetName ?? "them") + ", " + (int)td + " m off, meaning to " + Plans.Words(plan));
            }
            while (ids.Count > Language.MostOptions)
            {
                int drop = ids.Count - 2;   // keep the mind's own and the plan's step: the lowest of the rest goes
                if (ids[drop] == w.Choice)
                    drop--;
                ids.RemoveAt(drop);
                words.RemoveAt(drop);
            }
            List<PairReason> scenes = _cfg.MindStartsScenes ? MindScenesInto(w, now, ids, words) : null;   // their own reasons with someone near, in their words (step 2)
            if (ids.Count < 2)
                return false;
            string why;
            LanguageRequest r = MindRequestOf(w, h, now, out why);
            if (r == null)
                return false;
            r.Kind = "pick";
            r.Options = words;
            r.WithThought = true;   // the thought and the choice in one answer, so they agree (Lex, 2026-10-31)
            string key = "pick|" + w.Id + "|" + Environment.TickCount + "|" + _picksAsked;
            if (!_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = w.Name, PersonId = w.Id, Request = r, AskedAtMs = Environment.TickCount }))
                return false;
            _picksAsked++;
            _pending.Add(new PendingPick { W = w, Handle = h, Key = key, Mind = w.Choice, Ids = ids, Words = words, Plan = ids.Contains("plan:seek") ? plan : null, AskedAt = now, Deadline = now + PickWaitMs, Req = r, Scenes = scenes });
            return true;
        }

        /// Each tick: the picks that came back, or whose wait is up, acted on.
        private void PicksStep(int now)
        {
            ModelMinute(now);
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingPick pp = _pending[i];
                string answer = null;
                long ms = 0;
                bool came = _engine != null && _engine.LanguageAnswers.TakePick(pp.Key, out answer, out ms);
                if (!came && now < pp.Deadline && now >= pp.AskedAt)
                    continue;
                _pending.RemoveAt(i);
                if (!came)
                {
                    answer = null;
                    ms = now - pp.AskedAt;
                }
                try
                {
                    PickResolve(pp, came, answer, ms, now);
                }
                catch (Exception ex)
                {
                    Log.Write("MIND pick failed for " + pp.W.Name + ": " + ex.Message);
                }
            }
        }

        private void PickResolve(PendingPick pp, bool came, string answer, long ms, int now)
        {
            MindWho w = pp.W;
            int h = pp.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || w.Choice != pp.Mind)
                return;   // gone, or the mind has moved on since
            string thought = null;
            int idx = came ? Language.ParseThinkPick(answer, pp.Ids.Count, out thought) : -1;
            string offered = string.Join(" | ", pp.Words.Select((x, k) => (k + 1) + " " + x));
            string outcome;
            if (!came)
            {
                _picksLate++;
                outcome = "no answer in " + PickWaitMs / 1000.0 + " s: the mind's own";
            }
            else if (idx < 0)
            {
                _picksUnread++;
                outcome = "\"" + (answer ?? "").Trim() + "\" names none of them: the mind's own";
            }
            else
            {
                _picksTaken++;
                ModelMs(ms);
                outcome = (idx + 1) + " (" + pp.Words[idx] + ")" + (pp.Ids[idx] == pp.Mind ? ", the mind's own too" : ", not the mind's (" + Choosing.WordsOf(pp.Mind) + ")");
            }
            (_picksLogged++ < PicksLoud ? (Action<string>)Log.Write : Log.Detail)("MIND pick: " + w.Name + " offered " + offered + " -> " + outcome + ", " + ms + " ms");
            if (idx >= 0)
                PickThought(w.Name, h, thought, pp.Req);   // the thought it chose by, over their head as they go
            string picked = idx >= 0 ? pp.Ids[idx] : pp.Mind;
            _pickLast = w.Name + ": " + (idx >= 0 ? pp.Words[idx] : Choosing.WordsOf(pp.Mind) + " (the mind's)");
            if (picked != null && picked.StartsWith("scene:", StringComparison.Ordinal))
            {
                // a reason the person has with someone near: that scene, played the way the Director plays it (WestworldScript.MindScenes.cs)
                int si;
                if (pp.Scenes != null && int.TryParse(picked.Substring(6), out si) && si < pp.Scenes.Count &&
                    MindStartScene(pp.Scenes[si], w, pp.Words[idx], pp.Test ? "the Testing row, the model's pick" : "the model's pick", now))
                    return;
                MindActOn(w, h, now);   // the street couldn't take it: the mind's own
                return;
            }
            if (picked == pp.Mind)
            {
                if (idx >= 0)
                    _picksSame++;
                MindActOn(w, h, now);
                return;
            }
            if (picked == "plan:seek")
            {
                int t;
                float d;
                Plan p = PlanSeekable(w.Id, h, now, out t, out d);
                Ped hp = new Ped(h);
                if (p != null && p == pp.Plan && MindActWhyNot(hp, h) == null && !IsMindActing(h))
                {
                    _picksPlan++;
                    PlanSeekNow(p, h, t, d, now, "the model's pick");
                    return;
                }
                MindActOn(w, h, now);   // the one it's about has gone: the mind's own
                return;
            }
            // the model's pick in place of the mind's: the choice, what they did lately and why
            if (w.Recent != null && w.Recent.Id[0] == pp.Mind)
                w.Recent.Id[0] = picked;
            w.Choice = picked;
            w.ChoiceWhy = "the model's pick, over " + Choosing.WordsOf(pp.Mind);
            w.ChoiceTold = false;
            MindActOn(w, h, now);
        }

        private int _pickThoughtsShown, _pickThoughtsHeld;

        /// The thought that came with a pick, shown over them when it passes the same check as any other thought.
        private void PickThought(string name, int h, string thought, LanguageRequest r)
        {
            if (string.IsNullOrEmpty(thought))
                return;
            string why = Language.Check(r, thought);
            if (why != null)
            {
                _pickThoughtsHeld++;
                Log.Detail("MIND pick thought held for " + name + " (" + why + "): " + thought);
                return;
            }
            _pickThoughtsShown++;
            _ticker.Push(name + ", thinking", thought, "near", h);
            (_picksLogged < PicksLoud ? (Action<string>)Log.Write : Log.Detail)("MIND pick thought: " + name + ": \"" + thought + "\"");
        }

        // ---- the model's numbers (NIGHT3 item 2, so the reach can be raised by what the PC keeps up with) ----------------
        // The last 128 pick answers' times (the everyday picks and the waking's), the asks a minute, how many wait, the late
        // and unreadable shares. F7 TEST model:, and a MODEL line in the log once a minute while picks are asked.
        private readonly int[] _modelMs = new int[128];
        private int _modelMsN, _modelMinuteAt, _modelMinAsked, _modelMinLate, _modelMinUnread, _modelMinTaken;

        private void ModelMs(long ms)
        {
            _modelMs[_modelMsN % _modelMs.Length] = (int)Math.Max(0, Math.Min(ms, 60000));
            _modelMsN++;
        }

        private string ModelTimes()
        {
            int n = Math.Min(_modelMsN, _modelMs.Length);
            if (n == 0)
                return "no answers yet";
            var a = new int[n];
            Array.Copy(_modelMs, a, n);
            Array.Sort(a);
            return "answers in " + a[n / 2] + " ms typically, " + a[Math.Min(n - 1, (int)(n * 0.9))] + " ms for 9 in 10, " + a[n - 1] + " ms the slowest (the last " + n + ")";
        }

        private void ModelMinute(int now)
        {
            if (now < _modelMinuteAt)
                return;
            int asked = _picksAsked + _wakePicksAsked, taken = _picksTaken + _wakePicksTaken, late = _picksLate + _wakePicksLate, unread = _picksUnread + _wakePicksUnread;
            if (_modelMinuteAt > 0 && asked > _modelMinAsked)
                Log.Write("MODEL minute: " + (asked - _modelMinAsked) + " picks asked, " + (taken - _modelMinTaken) + " picked, " + (late - _modelMinLate) + " late, " + (unread - _modelMinUnread) + " unreadable; " +
                          _pending.Count + " waiting now (at most " + _cfg.PicksAtOnce + ", within " + _cfg.PickMetres + " m); " + ModelTimes());
            _modelMinuteAt = now + 60000;
            _modelMinAsked = asked;
            _modelMinTaken = taken;
            _modelMinLate = late;
            _modelMinUnread = unread;
        }

        private string ModelStatus()
        {
            if (!PicksOn)
                return "the model's picks are off or the server isn't ready";
            int asked = _picksAsked + _wakePicksAsked, late = _picksLate + _wakePicksLate, unread = _picksUnread + _wakePicksUnread;
            string store = _engine != null ? "; the queue: " + (_engine.LanguageAnswers.PicksAsked - _engine.LanguageAnswers.PicksAnswered - _engine.LanguageAnswers.PicksFailed) + " not yet answered, " +
                           _engine.LanguageAnswers.PicksDropped + " turned away (full)" : "";
            return asked + " picks asked this session (" + _picksAsked + " everyday, " + _wakePicksAsked + " waking), " + (asked > 0 ? (100 * late / asked) + "% late, " + (100 * unread / asked) + "% unreadable" : "none yet") +
                   "; " + ModelTimes() + "; reach " + _cfg.PickMetres + " m (past " + (int)PickNear + " m half as often), " + _pending.Count + " of " + _cfg.PicksAtOnce + " waiting" + store;
        }

        private string PicksStatus()
        {
            if (!_cfg.LanguagePicks)
                return "off ([Mind] LanguagePicks)";
            if (!(_cfg.MindLive && _cfg.Language))
                return "waiting on [Mind] Language and MindLive";
            string store = _engine != null ? ", store: " + _engine.LanguageAnswers.PicksAsked + " queued, " + _engine.LanguageAnswers.PicksAnswered + " answered, " + _engine.LanguageAnswers.PicksFailed + " failed, " +
                           _engine.LanguageAnswers.PicksDropped + " dropped" : "";
            return (_langReady ? "on" : "on, the server not ready") + ": " + _picksAsked + " asked this session, " + _picksTaken + " picked (" + _picksSame + " the mind's own, " + _picksPlan + " a plan's step), " +
                   _picksLate + " late, " + _picksUnread + " unreadable, " + _pending.Count + " waiting; thoughts with them " + _pickThoughtsShown + " shown, " + _pickThoughtsHeld + " held by the check" + store + (_pickLast != null ? "; last: " + _pickLast : "") + "; scenes from their own reasons: " + MindScenesStatus();
        }
    }
}
