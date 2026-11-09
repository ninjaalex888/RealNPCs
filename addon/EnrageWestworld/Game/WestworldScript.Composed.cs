// Composed acts, in the game (FIVEDAY2 item 8; Core/Composed.cs; [Mind] ComposedActs). Someone the control ring holds, between acts, with a feeling strong enough to show, plays two or three
// rows of the catalogue one after another: a step runner over Perform, counted as one of the mind's acts so the ring waits for it, and let go of if anything takes them.
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
        private sealed class ComposedRun
        {
            public int Handle, Target, NextAt, Step, CheckAt;
            public List<Expression> Steps;
            public MindAct Act;
            public string Name;
        }

        private const int ComposedCheckMs = 200;
        private Action _aComposed;
        private readonly List<ComposedRun> _composed = new List<ComposedRun>();
        private int _composedPlayed, _composedSteps, _composedFailed;
        private string _composedLast;

        /// The ring's moment for it: true when a composed act has begun (or is waiting on the model) instead of the next ordinary one.
        private bool RingComposed(int h, MindWho w, Held hd, Ped me, int now)
        {
            if (!RingOn || !_cfg.ComposedActs || w.Id == null || now - hd.ComposedAt < Composed.PersonGapMs || _composed.Count >= Composed.AtOnce || IsPerforming(h))
                return false;
            FeltAt best;
            float left;
            if (!_engine.Shadow.Strongest(w.Id, _store.LastGameMin, out best, out left) || best.Kind < 0 || best.Kind >= Feeling.Kinds.Length)
                return false;
            string strength = Appraisal.StrengthOf(Math.Min(1f, left));
            if (strength != "moderate" && strength != "strong")
                return false;
            string seed = w.Id + "|" + _store.LastGameMin / 30;
            hd.ComposedAt = now;   // tried: not again for a while, whichever way it goes
            if (!Voice.Chance(seed + "|composed", Composed.Chance))
                return false;
            List<Expression> menu = Composed.Menu(Feeling.Kinds[best.Kind], best.Urge, strength, w.P.Woman, true, seed);
            int target = best.AboutId == "player" || best.About == "you" ? me.Handle : ComposedOther(h, w, best);
            if (target == 0)
                menu = menu.Where(e => e.Channel != "move" || e.Name == Catalogue.StillMove || e.Name == Catalogue.WanderMove).ToList();
            if (menu.Count < Composed.LeastSteps)
                return false;
            if (ComposedAsk(h, w, hd, menu, target, now, Feeling.Kinds[best.Kind]))
                return true;
            return ComposedStart(h, w, Composed.Rules(menu, seed), target, now, "the rules");
        }

        /// Whom else it's about, if they're near.
        private int ComposedOther(int h, MindWho w, FeltAt best)
        {
            if (best.AboutId == null)
                return 0;
            foreach (int oh in _mindNear)
            {
                MindWho o;
                if (oh != h && _mindWho.TryGetValue(oh, out o) && o.Id == best.AboutId && ENTITY.DOES_ENTITY_EXIST(oh))
                    return oh;
            }
            return 0;
        }

        private bool ComposedStart(int h, MindWho w, List<Expression> steps, int target, int now, string by)
        {
            if (!Composed.Valid(steps) || IsMindActing(h) || _mindActs.Count >= MindActsAtOnceNow)
                return false;
            var a = new MindAct { Handle = h, Kind = "composed", Name = w.Name, Started = now, Until = now + 20000 };
            _mindActs.Add(a);
            _composed.Add(new ComposedRun { Handle = h, Target = target, NextAt = now, Steps = steps, Act = a, Name = w.Name });
            _composedPlayed++;
            _composedLast = w.Name + ": " + string.Join(", then ", steps.Select(s => s.Words)) + " (" + by + ")";
            Log.Write("HELD composed: " + _composedLast);
            return true;
        }

        /// Each frame: the next step of each, once the last has played out.
        private void ComposedTick(int now)
        {
            ComposedAskStep(now);
            for (int i = _composed.Count - 1; i >= 0; i--)
            {
                ComposedRun r = _composed[i];
                int h = r.Handle;
                bool due = now >= r.NextAt && !IsPerforming(h);
                if (due || now >= r.CheckAt || r.CheckAt - now > ComposedCheckMs)   // whether it's still theirs, before a step and a few times a second between them, not every frame (TENHOUR item 1)
                {
                    r.CheckAt = now + ComposedCheckMs;
                    if (!_mindActs.Contains(r.Act) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !CanDirect(new Ped(h)))
                    {
                        PerformCancel(h);
                        r.Act.Until = now;
                        _composed.RemoveAt(i);   // the act's been dropped, or a fight, a flight or a fall has them: no more orders
                        continue;
                    }
                }
                if (!due)
                    continue;
                if (r.Step >= r.Steps.Count)
                {
                    r.Act.Until = now;   // done: the ring's next act, or their own way
                    _composed.RemoveAt(i);
                    continue;
                }
                Expression row = r.Steps[r.Step++];
                int ms;
                string why = Perform(h, row, r.Target, out ms);
                if (why != null)
                {
                    _composedFailed++;
                    r.NextAt = now + 300;   // the next step anyway: a sentence with a word missing still reads
                    continue;
                }
                _composedSteps++;
                r.NextAt = now + ms + 250;
                r.Act.Until = Math.Max(r.Act.Until, r.NextAt + 4000);
            }
        }

        private void ComposedStep(int now)
        {
            if (_composed.Count > 0 || _composedAsks.Count > 0)
                Safe("composed acts", _aComposed ?? (_aComposed = () => ComposedTick(_now)));
        }

        /// The Testing row: the nearest person the ring holds shows whatever's on them, or, with nothing on them, plain contempt.
        private string ComposedTest(int now, out string said)
        {
            said = null;
            if (!RingOn || !_cfg.ComposedActs)
                return "[Mind] ComposedActs or ControlRing is off";
            Ped me = Game.Player.Character;
            int h = 0;
            float bd = 30f;
            foreach (int x in _held.Keys)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(x) || IsMindActing(x) || PED.IS_PED_USING_ANY_SCENARIO(x))
                    continue;
                float d = new Ped(x).Position.DistanceTo(me.Position);
                if (d < bd)
                {
                    bd = d;
                    h = x;
                }
            }
            MindWho w;
            if (h == 0 || !_mindWho.TryGetValue(h, out w) || w.P == null)
                return "nobody the ring holds is free near you";
            FeltAt best;
            float left;
            bool felt = _engine.Shadow.Strongest(w.Id, _store.LastGameMin, out best, out left) && best.Kind >= 0 && best.Kind < Feeling.Kinds.Length;
            string feeling = felt ? Feeling.Kinds[best.Kind] : "contempt", urge = felt ? best.Urge : "dismiss";
            List<Expression> menu = Composed.Menu(feeling, urge, "strong", w.P.Woman, true, w.Id + "|" + now);
            List<Expression> steps = Composed.Rules(menu, w.Id + "|" + now);
            if (steps == null)
                return "nothing to compose for " + feeling + " yet (" + menu.Count + " moves known)";
            if (!ComposedStart(h, w, steps, me.Handle, now, "the Testing row"))
                return "the ring or the street has no room for it just now";
            said = w.Name + ": " + string.Join(", then ", steps.Select(s => s.Words));
            return null;
        }

        private string ComposedStatus()
        {
            return (_cfg.ComposedActs ? "on" : "off ([Mind] ComposedActs)") + ": " + _composedPlayed + " begun this session, " + _composedSteps + " steps played, " + _composedFailed + " steps failed soft" + ComposedAskStatus() + (_composedLast != null ? "; last " + _composedLast : "");
        }
    }
}
