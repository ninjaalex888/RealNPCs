// The model composes (FIVEDAY2 item 9; Core/Composed.cs). Offered a person's moves as a short numbered menu in plain words, it answers with two or three numbers in order; the order is
// checked, and anything that doesn't hold (a walk-away before the look, the same move twice, words where numbers should be) falls back to the rules. Numbers only, never a clip's name: the
// model can't ask for anything that isn't on the list. Under PicksAtOnce's limit, by way of the same queue as the everyday picks.
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
        private sealed class ComposedPending
        {
            public int Handle, Target, Deadline, AskedAt;
            public MindWho W;
            public List<Expression> Menu;
            public string Key, Seed;
        }

        private readonly List<ComposedPending> _composedAsks = new List<ComposedPending>();
        private int _composedAsked, _composedByModel, _composedBad, _composedLate;

        /// Put to the model when it's on and has room: true means the act will start when the answer comes, or the wait is up.
        private bool ComposedAsk(int h, MindWho w, Held hd, List<Expression> menu, int target, int now, string feeling)
        {
            if (!_cfg.ComposedModel || !PicksOn || _pending.Count + _composedAsks.Count >= _cfg.PicksAtOnce)
                return false;
            string why;
            LanguageRequest r = MindRequestOf(w, h, now, out why);
            if (r == null)
                return false;
            r.Kind = "pick";
            r.Compose = true;
            r.WithThought = false;
            r.Options = menu.Select(e => e.Words).ToList();
            if (string.IsNullOrEmpty(r.Feeling))
                r.Feeling = feeling;
            string key = "compose|" + w.Id + "|" + now;
            if (!_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = w.Name, PersonId = w.Id, Request = r, AskedAtMs = Environment.TickCount }))
                return false;
            _composedAsks.Add(new ComposedPending { Handle = h, Target = target, Deadline = now + Composed.AskWaitMs, AskedAt = now, W = w, Menu = menu, Key = key, Seed = w.Id + "|" + _store.LastGameMin / 30 });
            _composedAsked++;
            hd.NextAt = now + Composed.AskWaitMs + 500;
            return true;
        }

        private void ComposedAskStep(int now)
        {
            for (int i = _composedAsks.Count - 1; i >= 0; i--)
            {
                ComposedPending p = _composedAsks[i];
                string answer = null;
                long ms = 0;
                bool came = _engine != null && _engine.LanguageAnswers.TakePick(p.Key, out answer, out ms);
                if (!came && now < p.Deadline && now >= p.AskedAt)
                    continue;
                _composedAsks.RemoveAt(i);
                if (!_held.ContainsKey(p.Handle) || MindActWhyNot(new Ped(p.Handle), p.Handle) != null)
                    continue;   // let go, drawn on, sat down or taken up during the wait
                List<Expression> steps = came ? Composed.FromAnswer(answer, p.Menu) : null;
                string by;
                if (steps != null)
                {
                    _composedByModel++;
                    ModelMs(ms);
                    by = "the model";
                }
                else
                {
                    if (came)
                        _composedBad++;
                    else
                        _composedLate++;
                    by = came ? "the rules, the model's order wouldn't do: \"" + (answer ?? "").Trim() + "\"" : "the rules, the model was late";
                    steps = Composed.Rules(p.Menu, p.Seed);
                }
                ComposedStart(p.Handle, p.W, steps, p.Target, now, by);
            }
        }

        private string ComposedAskStatus()
        {
            return _composedAsked > 0 ? "; the model asked " + _composedAsked + " times: " + _composedByModel + " orders used, " + _composedBad + " wouldn't do, " + _composedLate + " late" : "";
        }
    }
}
