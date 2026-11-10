// The held ring (Lex, 2026-11-02: "tighten the radius around the player we control NPCs but widen the control duration"):
// anyone the mind knows within [Mind] HeldRingMetres (12) of you, on their feet and free, is held. When one of their acts
// ends the next begins at once instead of the game taking them back: their mind's present choice, else the choice it
// ranked next, else the nearest of the game's own spots near them (a bench, a rail, a wall), else a short stroll about
// where they stand. Never idle between acts, and never a new order over a seat or a spot they're at already (that is
// what they chose; the ordinary minute's choice moves them on). Let go past HeldRingMetres + 13 m (25 for 12), after
// HeldRingMinutes, or the moment anything else has a claim: a gun on them or near, a fight, a flee, a scene, Talk, a
// wake act, enRAGE driving its own, the law after you, a mission. A two-minute rest after each let-go. Behind
// [Mind] HeldRing (off as it comes, while it's tried) with MindLive and MindActs. The waking's own hold
// (WestworldScript.WakeHold.cs) is the model this follows; a waking act always comes first.
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
        private sealed class Held
        {
            public string Name;
            public int Since, NextAt, Acts, Fillers;
            public int AskedAt = int.MinValue / 2;   // the last time the model was asked about them (RingPlans.AskGapMs)
            public int StoodAt = int.MinValue / 2;   // the last standing look (a watch, a show): one each HeldStandMs at most
            public string Last;
            public int ComposedAt = int.MinValue / 2;   // the last composed act (FIVEDAY2 item 8)
            public bool Seated;   // taken in sat or leaning (FIVEDAY2 item 3): gets up, in time, for what their mind wants
        }

        // a watch or a show is a stand and a look: chained back to back it stopped and turned them every few seconds, which
        // read as freezing (Lex, 2026-11-02, Valentine). One in this long, else the next choice, a spot or a stroll
        private const int HeldStandMs = 30000;

        private static bool StandingKind(string kind)
        {
            return kind == "watch" || kind == "showoff";
        }

        private readonly Dictionary<int, Held> _held = new Dictionary<int, Held>();
        private readonly Dictionary<int, int> _heldRest = new Dictionary<int, int>();   // let go: not held again before this
        private const int HeldRestMs = 120000, HeldMost = 8, HeldStepMs = 500;
        private int _heldStepAt, _heldStarts, _heldChained, _heldFills, _heldEnds;
        private readonly Dictionary<string, int> _heldEndWhy = new Dictionary<string, int>();
        private string _heldLast;

        // ---- the control ring (FIVEDAY2 item 1; Core/Ring.cs): [Mind] ControlRing takes the held ring wider, by how you're moving, and
        // measures what every step costs. HeldRing stays as it was for anyone with ControlRing off.
        private bool RingOn { get { return _cfg != null && _cfg.ControlRing; } }
        private readonly RingGuard _ringGuard = new RingGuard();
        private readonly RingGaitWatch _ringGait = new RingGaitWatch();
        private RingReach _ringReach;
        private int _ringActsThisMinute, _ringActsLastMinute, _ringMinuteAt, _ringDrawnIn, _ringReorders, _ringReorderChecks;

        private bool HeldOn { get { return _cfg != null && (_cfg.HeldRing || _cfg.ControlRing) && _cfg.MindLive && _cfg.MindActs; } }
        private float HeldInMetres { get { return RingOn ? _ringReach.In : Math.Max(4f, Math.Min(20f, _cfg.HeldRingMetres)); } }
        private float HeldOutMetres { get { return RingOn ? _ringReach.Out : HeldInMetres + 13f; } }
        private int HeldMostNow { get { return RingOn ? _ringReach.Most : HeldMost; } }

        /// How many acts may run at once: the street's ten, or room for everyone the ring holds.
        private int MindActsAtOnceNow { get { return RingOn ? Math.Max(MindActsAtOnce, _ringReach.Most + 4) : MindActsAtOnce; } }

        /// The ring's reach for how you're moving now, drawn in by the guard.
        private void RingUpdate(Ped me, int now)
        {
            int h = me.Handle;
            bool mounted = PED.IS_PED_ON_MOUNT(h), vehicle = PED.IS_PED_IN_ANY_VEHICLE(h, false);
            float speed = mounted ? ENTITY.GET_ENTITY_SPEED(PED.GET_MOUNT(h)) : ENTITY.GET_ENTITY_SPEED(h);
            _ringReach = Ring.ReachFor(_ringGait.Update(Ring.GaitOf(mounted, vehicle, speed), now), _cfg.ControlRingMetres, _cfg.ControlRingMost, _ringGuard.Scale);
            if (now - _ringMinuteAt >= 60000 || now < _ringMinuteAt)
            {
                _ringMinuteAt = now;
                RingMinuteLine();
                _ringActsLastMinute = _ringActsThisMinute;
                _ringActsThisMinute = 0;
            }
        }

        // what the last minute's line has told already, so each says that minute's own numbers
        private int _ringToldStarts, _ringToldEnds, _ringToldReorders, _ringToldRaised, _ringToldErrand, _ringToldAsked, _ringToldComposed;

        /// Once a minute while the ring has anything to say: F7's numbers in the log, so a session can be read afterwards (TENHOUR item 2; nobody is looking at F7 while they play, and the frame-cost test needs these).
        private void RingMinuteLine()
        {
            int began = _heldStarts - _ringToldStarts, ended = _heldEnds - _ringToldEnds, told = _ringReorders - _ringToldReorders, up = _ringRaised - _ringToldRaised,
                errand = _ringPlanSteps - _ringToldErrand, asked = _ringAsked - _ringToldAsked, composed = _composedPlayed - _ringToldComposed;
            if (_held.Count == 0 && _ringActsThisMinute == 0 && began == 0 && ended == 0)
                return;
            _ringToldStarts = _heldStarts;
            _ringToldEnds = _heldEnds;
            _ringToldReorders = _ringReorders;
            _ringToldRaised = _ringRaised;
            _ringToldErrand = _ringPlanSteps;
            _ringToldAsked = _ringAsked;
            _ringToldComposed = _composedPlayed;
            Log.Write("RING minute: " + _held.Count + " held now (" + _ringGait.Settled + ", " + (int)_ringReach.In + " m, at most " + _ringReach.Most + "); this minute " + began + " taken in, " + ended + " let go, " +
                      _ringActsThisMinute + " acts chained (" + errand + " of an errand, " + composed + " composed, " + up + " got up), " + told + " told again, " + asked + " asked the model; a step " +
                      _ringGuard.AverageMicros + " us on average, peak " + _ringGuard.PeakMicros + " us, the guard at " + (int)(_ringGuard.Scale * 100f) + "%" + (_ringGuard.Shrinks > 0 ? " (drawn in " + _ringGuard.Shrinks + " times)" : ""));
        }

        /// The ring has drawn in below how many it holds (you got on a horse, or it cost too much): the farthest are let go, with no
        /// rest, since nothing went wrong with them.
        private void RingDrawIn(Vector3 at, int now)
        {
            while (_held.Count > Math.Max(0, HeldMostNow))
            {
                int far = 0;
                float d = -1f;
                foreach (int h in _held.Keys)
                {
                    float x = ENTITY.DOES_ENTITY_EXIST(h) ? ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(at) : float.MaxValue;
                    if (x > d)
                    {
                        d = x;
                        far = h;
                    }
                }
                HeldLetGo(far, "the ring drew in", now, false);
                _ringDrawnIn++;
            }
        }

        private bool IsHeld(int h)
        {
            return _held.ContainsKey(h);
        }

        /// Twice a second from the mind's step: let go who should be, take in who's come near, and keep the held going.
        private void HeldStep(int now)
        {
            if (!HeldOn)
            {
                if (_held.Count > 0)
                    foreach (int h in _held.Keys.ToList())
                        HeldLetGo(h, "the held ring is off", now);
                return;
            }
            if (now < _heldStepAt && now >= _heldStepAt - 5000)
                return;
            _heldStepAt = now + HeldStepMs;
            long t0 = Stopwatch.GetTimestamp();
            HeldStepWork(now);
            _ringGuard.Spent(Micros(t0), now);   // every step measured, whatever it did: the gauge in F7 and the log, and the guard's check
        }

        private void HeldStepWork(int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at = me.Position;
            if (RingOn)
                RingUpdate(me, now);
            string all = HeldAllWhy(me);
            _ringShooting = _held.Count > 0 && all == null && PED.IS_ANY_PED_SHOOTING_IN_AREA(at.X - 70f, at.Y - 70f, at.Z - 50f, at.X + 70f, at.Y + 70f, at.Z + 50f, false, false);   // one look round, so each held person needn't ask for their own patch of it
            _heldNow.Clear();
            _heldNow.AddRange(_held.Keys);
            for (int i = 0; i < _heldNow.Count; i++)
            {
                int h = _heldNow[i];
                string why = all ?? HeldWhyNot(h, at, now);
                if (why != null)
                    HeldLetGo(h, why, now, all == null);   // a let-go of everyone (you mounted, fired) leaves no rest: back in once it's over
            }
            if (all != null)
                return;
            if (RingOn)
                RingDrawIn(at, now);
            // who comes in: the named people the mind has near, nearest first
            if (_held.Count < HeldMostNow)
            {
                RingCandidates(me, at, now);
                for (int c = 0; c < _ringCandidates; c++)
                {
                    if (_held.Count >= HeldMostNow)
                        break;
                    int h = _ringCandidate[c];
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    int rest;
                    if (_heldRest.TryGetValue(h, out rest) && now < rest && now >= rest - HeldRestMs - 1000)
                        continue;
                    Ped p = new Ped(h);
                    bool seated = PED.IS_PED_USING_ANY_SCENARIO(h);
                    if (!seated)
                        _ringSeatedSince.Remove(h);
                    else if (!RingSeatedReady(h, w, now))
                        continue;   // sat or at a spot: theirs, for a decent while; the ring's own (FIVEDAY2 item 3) after that, or the minute's choice gets them up and on their feet they're taken in (a bench full of the held was a ring of nobody chained)
                    if (MindChosenQuietly(h) || MindActWhyNot(p, h, seated) != null || !CanDirect(p))
                    {
                        if (_ringRefused.Count > 200)
                            _ringRefused.Clear();
                        _ringRefused[h] = now + RingRefusedMs;   // not asked again for a moment: a refusal costs dozens of game calls, and whatever it was won't have changed in half a second
                        continue;
                    }
                    _held[h] = new Held { Name = w.Name, Since = now, NextAt = now, Seated = seated };
                    if (!seated)
                        RingAskModel(h, w, _held[h], now);   // the model's word on where they're off to next (FIVEDAY2 item 5)
                    _heldStarts++;
                    Log.Write("HELD: " + w.Name + " held, " + (int)p.Position.DistanceTo(at) + " m from you (" + (w.Choice != null ? Choosing.WordsOf(w.Choice) : "no choice yet") + ")");
                }
            }
            // keep each going: the next act the moment one ends
            _heldNow.Clear();
            _heldNow.AddRange(_held.Keys);
            for (int i = 0; i < _heldNow.Count; i++)
            {
                int h = _heldNow[i];
                Held hd;
                if (!_held.TryGetValue(h, out hd))
                    continue;
                if (now < hd.NextAt || IsMindActing(h) || InWakeAct(h))
                    continue;
                if (PED.IS_PED_USING_ANY_SCENARIO(h))
                {
                    MindWho sw;
                    if (hd.Seated && _mindWho.TryGetValue(h, out sw) && sw.P != null && RingSeatedReady(h, sw, now))
                    {
                        RingRaise(h, sw, hd, now);
                        continue;
                    }
                    hd.NextAt = now + 3000;   // at a seat or a spot: theirs, untouched (the minute's choice may get them up)
                    continue;
                }
                hd.Seated = false;
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.P == null)
                {
                    HeldLetGo(h, "the mind lost them", now);
                    continue;
                }
                HeldNext(h, w, hd, me, now);
            }
        }

        // one step's working lists, kept so a step allocates nothing (TENHOUR item 1), and who was refused a moment ago
        private const int RingRefusedMs = 2000, RingMostCandidates = 48;
        private readonly List<int> _heldNow = new List<int>(24);
        private readonly int[] _ringCandidate = new int[RingMostCandidates];
        private readonly float[] _ringCandidateNear = new float[RingMostCandidates];
        private int _ringCandidates;
        private bool _ringShooting;
        private readonly Dictionary<int, int> _ringRefused = new Dictionary<int, int>();

        /// Who might come in this step, nearest first, each counted once: their distance, less a little if they're along the way you're going (FIVEDAY2 R1). How you're moving is read once for the step, not once a person.
        private void RingCandidates(Ped me, Vector3 at, int now)
        {
            _ringCandidates = 0;
            bool ahead = RingOn && _cfg.ControlRingAhead;
            float fx = 0f, fy = 0f, speed = 0f, room = HeldOutMetres - HeldInMetres - 1f, reach = HeldInMetres;
            if (ahead)
            {
                Vector3 f = me.ForwardVector;
                fx = f.X;
                fy = f.Y;
                speed = ENTITY.GET_ENTITY_SPEED(PED.IS_PED_ON_MOUNT(me.Handle) ? PED.GET_MOUNT(me.Handle) : me.Handle);
            }
            for (int i = 0; i < _mindNear.Count; i++)
            {
                int h = _mindNear[i], until;
                if (_held.ContainsKey(h) || (_ringRefused.TryGetValue(h, out until) && now < until && until - now <= RingRefusedMs))
                    continue;
                Vector3 p = ENTITY.GET_ENTITY_COORDS(h, true, true);
                float d = p.DistanceTo(at);
                if (ahead && d >= 0.5f)
                    d = Ring.Nearness(d, ((p.X - at.X) * fx + (p.Y - at.Y) * fy) / d, speed, room);
                if (d > reach)
                    continue;
                int s = Math.Min(_ringCandidates, RingMostCandidates - 1);
                while (s > 0 && _ringCandidateNear[s - 1] > d)
                {
                    _ringCandidate[s] = _ringCandidate[s - 1];
                    _ringCandidateNear[s] = _ringCandidateNear[s - 1];
                    s--;
                }
                _ringCandidate[s] = h;
                _ringCandidateNear[s] = d;
                if (_ringCandidates < RingMostCandidates)
                    _ringCandidates++;
            }
        }

        /// What lets go everyone at once, or null.
        private string HeldAllWhy(Ped me)
        {
            if (MISC.GET_MISSION_FLAG() || _missionPaused) return "a mission";
            if (me.IsDead) return "you died";
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0) return "you're wanted";
            if (PED.IS_PED_SHOOTING(me.Handle)) return "you fired";
            if (RingOn)
            {
                if (_ringReach.Most <= 0) return "you're moving too fast to be watched";
            }
            else if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false)) return "you're riding";
            return null;
        }

        /// Why this one is let go now, or null to keep them.
        private string HeldWhyNot(int h, Vector3 at, int now)
        {
            Held hd = _held[h];
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h)) return "gone";
            if (now - hd.Since > Math.Max(1, Math.Min(30, _cfg.HeldRingMinutes)) * 60000) return "held long enough";
            Ped p = new Ped(h);
            Vector3 pos = p.Position;
            if (pos.DistanceTo(at) > HeldOutMetres) return "you went off";
            if (InWakeAct(h)) return "a waking act of theirs";
            if (!_mindNear.Contains(h)) return "out of the mind's ring";
            if (IsMindActing(h))
            {
                // their own act under way: only what takes them from it (the act's own step drops it for the rest)
                if (TakenUp(h)) return "taken up by something else";
                if (AimedLately(h, now)) return "aimed at";
                if (!CanDirect(p)) return "the game has them (" + CantDirectWhy(p) + ")";   // (split: FOURTEENHOUR item 22)
                return null;
            }
            if (_ringShooting && PED.IS_ANY_PED_SHOOTING_IN_AREA(pos.X - 40f, pos.Y - 40f, pos.Z - 15f, pos.X + 40f, pos.Y + 40f, pos.Z + 15f, false, false)) return "gunfire near";
            if (!CanDirect(p)) return "the game has them (" + CantDirectWhy(p) + ")";
            return MindActWhyNot(p, h, true);
        }

        private void HeldLetGo(int h, string why, int now, bool rest = true)
        {
            Held hd;
            if (!_held.TryGetValue(h, out hd))
                return;
            _held.Remove(h);
            if (rest)
                _heldRest[h] = now + HeldRestMs;
            if (_heldRest.Count > 200)
                _heldRest.Clear();
            _heldEnds++;
            int n;
            _heldEndWhy.TryGetValue(why, out n);
            _heldEndWhy[why] = n + 1;
            Log.Write("HELD: " + hd.Name + " let go after " + (now - hd.Since) / 1000 + " s, " + hd.Acts + " act" + (hd.Acts == 1 ? "" : "s") + (hd.Fillers > 0 ? " (" + hd.Fillers + " of them a spot or a stroll)" : "") + " (" + why + ")");
        }

        // ---- plans per person (FIVEDAY2 item 4; Core/Ring.cs, RingPlans): an errand kept by who they are, not by the body in front of you
        private readonly Dictionary<string, RingPlan> _ringPlans = new Dictionary<string, RingPlan>();
        private int _ringPlansMade, _ringPlanSteps;

        /// Their errand: the one they were on if it's still fresh, else a new one. Null with the switch off or a strong feeling in charge.
        private RingPlan RingPlanFor(MindWho w, int now)
        {
            if (!RingOn || !_cfg.ControlRingPlans || w.Id == null || (w.ChoiceWhy != null && w.ChoiceWhy.StartsWith("a feeling", StringComparison.Ordinal)))
                return null;
            RingPlan p;
            if (_ringPlans.TryGetValue(w.Id, out p))
            {
                if (RingPlans.Keeps(p, now))
                    return p;
                if (p != null && p.Done && now >= p.MadeAt && now - p.MadeAt < RingPlans.KeepMs)
                    return null;   // done with it: their own way until it's old
            }
            if (_ringPlans.Count > 200)
                foreach (string k in _ringPlans.Where(kv => !RingPlans.Keeps(kv.Value, now)).Select(kv => kv.Key).ToList())
                    _ringPlans.Remove(k);
            string tg = w.TradeGroup ?? (w.TradeGroup = InnerLife.TradeGroup(w.P.Trade));
            p = RingPlans.Make(CLOCK.GET_CLOCK_HOURS(), _placeTown != null, tg != "gentry" && !(w.P.Woman && tg == "town"), w.Id + "|" + _store.LastGameMin / 180, now);
            _ringPlans[w.Id] = p;
            _ringPlansMade++;
            Log.Detail("HELD: " + w.Name + " means to " + string.Join(", then ", p.Steps.Select(Choosing.WordsOf)));
            return p;
        }

        private int _ringAsked;

        /// As someone comes into the ring: their errand's next step goes to the model with the rest of what they might do, the everyday pick's own way (PickAsk, under PicksAtOnce, so the model's never asked more than it can answer), and whichever it picks is done. Late or unreadable, the step stands.
        private void RingAskModel(int h, MindWho w, Held hd, int now)
        {
            if (!_cfg.ControlRingPicks || !PicksOn)
                return;
            RingPlan plan = RingPlanFor(w, now);
            if (plan == null || plan.Done)
                return;
            string was = w.Choice, wasNext = w.ChoiceNext;
            w.Choice = plan.NextStep;
            if (w.Choice == was || !PickAsk(w, h, now, MindContextFor(w, h, false, _store.LastGameMin)))
            {
                w.Choice = was;
                w.ChoiceNext = wasNext;
                return;
            }
            plan.Next++;
            _ringAsked++;
            hd.AskedAt = now;
            hd.NextAt = now + PickWaitMs + 500;   // the answer's act comes through PicksStep
        }

        /// The model's word on what they're about to do (TENHOUR item 12: its answers take a third of a second and it was asked a few times a minute). True when it's asked: the act comes through PicksStep, the step itself if the answer's late or unreadable. Not more than once a person in RingPlans.AskGapMs. `evenIfSame`: a seated person's choice is the one standing already, and the ask is what gets them offered something else.
        private bool RingAskStep(int h, MindWho w, Held hd, string step, int now, bool evenIfSame = false)
        {
            if (!_cfg.ControlRingPicks || !PicksOn || step == null || (now - hd.AskedAt < RingPlans.AskGapMs && now >= hd.AskedAt))
                return false;
            string was = w.Choice, wasNext = w.ChoiceNext;
            if (step == was && !evenIfSame)
                return false;
            hd.AskedAt = now;
            w.Choice = step;
            if (!PickAsk(w, h, now, MindContextFor(w, h, false, _store.LastGameMin)))
            {
                w.Choice = was;
                w.ChoiceNext = wasNext;
                return false;
            }
            _ringAsked++;
            hd.NextAt = now + PickWaitMs + 500;
            return true;
        }

        /// The next act for one held between acts: the next step of their errand if they have one, the mind's choice, the one it ranked next, a spot, a stroll. Never nothing.
        private void HeldNext(int h, MindWho w, Held hd, Ped me, int now)
        {
            hd.NextAt = now + 1500;
            if (RingComposed(h, w, hd, me, now))
            {
                hd.Acts++;
                _heldChained++;
                _ringActsThisMinute++;
                return;   // a feeling shown in two or three moves (WestworldScript.Composed.cs)
            }
            RingPlan plan = RingPlanFor(w, now);
            while (plan != null && !plan.Done)
            {
                string step = plan.NextStep;
                plan.Next++;   // done or not to be done, it's passed: a plan never stalls on what can't happen now
                if (RingAskStep(h, w, hd, step, now))
                    return;
                string got = HeldTry(w, hd, h, now, step);
                if (got == null)
                {
                    hd.Acts++;
                    _heldChained++;
                    _ringPlanSteps++;
                    _ringActsThisMinute++;
                    hd.Last = _heldLast = _mindActLast;
                    Log.Detail("HELD: " + hd.Name + " goes on with the errand: " + _mindActLast);
                    return;
                }
                if (MindRetry.Temporary(got))
                {
                    plan.Next--;   // not now, rather than not at all: the step waits
                    if (got == MindRetry.Aimed || got == MindRetry.Riding || got == MindRetry.Busy)
                        return;
                    break;   // the mind's own choice meanwhile
                }
                if (got == MindRetry.Story)
                    return;
            }
            string lost = HeldTry(w, hd, h, now, w.MindChoice ?? w.Choice);
            if (lost != null && w.ChoiceNext != null && w.ChoiceNext != (w.MindChoice ?? w.Choice))
                lost = HeldTry(w, hd, h, now, w.ChoiceNext);
            if (lost == null)
            {
                hd.Acts++;
                _heldChained++;
                _ringActsThisMinute++;
                hd.Last = _mindActLast;
                _heldLast = _mindActLast;
                Log.Detail("HELD: " + hd.Name + " goes on: " + _mindActLast);
                return;
            }
            if (lost == MindRetry.Aimed || lost == MindRetry.Story || lost == MindRetry.Riding || lost == MindRetry.Busy)
                return;   // HeldWhyNot lets them go on the next step
            if (PED.IS_PED_USING_ANY_SCENARIO(h))
            {
                hd.NextAt = now + 12000;   // sat, and nothing to get up for yet: no spot or stroll over their seat
                return;
            }
            HeldFill(h, w, hd, now, lost);
        }

        /// One choice tried as an act now, the mind's own way (MindActOn), past the street's 1.5 s gap: a held one going on
        /// isn't a new start. Null when it began.
        private string HeldTry(MindWho w, Held hd, int h, int now, string kind)
        {
            if (kind == null || kind == "stand" || Choosing.CarriesOn(kind))
                return LostStand;
            bool standing = StandingKind(kind);
            if (standing && now - hd.StoodAt < HeldStandMs)
                return "a standing look lately";
            string was = w.Choice;
            int gap = _mindActNextAt;
            w.Choice = kind;
            _mindActNextAt = 0;
            string lost = MindActOn(w, h, now);
            if (lost != null)
            {
                w.Choice = was;
                if (_mindActNextAt == 0)
                    _mindActNextAt = gap;
            }
            else if (standing)
                hd.StoodAt = now;
            return lost;
        }

        // the ring's seated: when each was first seen sat near you, and how many have got up for it
        private readonly Dictionary<int, int> _ringSeatedSince = new Dictionary<int, int>();
        private int _ringRaised, _ringStayed;

        /// Whether someone sat near you has been there a decent while, and their mind wants something else that gets them up. Their dwell is theirs: a bench doesn't rise as one.
        private bool RingSeatedReady(int h, MindWho w, int now)
        {
            if (!RingOn || !_cfg.ControlRingSeated || !_cfg.MindActsScenarios)
                return false;
            int since;
            if (!_ringSeatedSince.TryGetValue(h, out since) || now < since)
            {
                if (_ringSeatedSince.Count > 200)
                    _ringSeatedSince.Clear();
                _ringSeatedSince[h] = now;
                return false;
            }
            if (now - since < Ring.SeatedDwellMs(Voice.Hash(w.Id)))
                return false;
            string kind = w.MindChoice ?? w.Choice;
            if (kind == null || !MindScenarios.GetsUpFor(kind) || MindAlreadyAt(kind, h) != null)
            {
                _ringStayed++;
                return false;   // it's what they want, or nothing that gets them up: they stay
            }
            return true;
        }

        /// Up the game's way and off to what they wanted (MindGetUp, under its own limit of a few at a time near you). If it can't be done yet they stay and it's tried again in a while.
        private void RingRaise(int h, MindWho w, Held hd, int now)
        {
            hd.NextAt = now + 6000;
            if (w.ChoiceWhy != null && w.ChoiceWhy.StartsWith("the model's pick", StringComparison.Ordinal))
            {
                hd.NextAt = now + RingPlans.AskGapMs;
                return;   // the model has had its say about them: they stay as it left them until their own mind moves on
            }
            if (RingAskStep(h, w, hd, w.MindChoice ?? w.Choice, now, true))
                return;
            string lost = HeldTry(w, hd, h, now, w.MindChoice ?? w.Choice);
            if (lost != null || !MindGettingUp(h))
                return;
            hd.Acts++;
            hd.Seated = false;
            _heldChained++;
            _ringActsThisMinute++;
            _ringRaised++;
            _ringSeatedSince.Remove(h);
            hd.Last = _heldLast = _mindActLast;
            Log.Detail("HELD: " + hd.Name + " gets up: " + _mindActLast);
        }

        /// An act under way for someone the ring holds, every few seconds: stopped short of where they were sent, they're told again (never a scenario, never over a fight, a flee or a fall).
        private void RingKeepAt(MindAct a, int h, int now)
        {
            if (a.Spot || a.Arrived || a.Seated || a.Then != 0 || (now < a.CheckAt && a.CheckAt - now <= Ring.CheckWalkMs))
                return;
            Vector3 pos = ENTITY.GET_ENTITY_COORDS(h, true, true);
            bool first = a.CheckAt == 0;
            float moved = first ? 99f : pos.DistanceTo(a.CheckedFrom);
            a.CheckedFrom = pos;
            a.CheckAt = now + Ring.CheckWalkMs;
            _ringReorderChecks++;
            if (first || !Ring.Stalled(moved, a.Reorders) || PED.IS_PED_USING_ANY_SCENARIO(h) || !CanDirect(new Ped(h)) || _director.Involves(h))
                return;
            if (a.To != default(Vector3))
            {
                Vector3 to = a.To;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, to.X, to.Y, to.Z, 1f, -1, a.Arrive != null ? 2.5f : 1f, 0, 0f);
            }
            else if (a.Toward != 0 && ENTITY.DOES_ENTITY_EXIST(a.Toward))
                TASK.TASK_GO_TO_ENTITY(h, a.Toward, 20000, 1f, a.Joining ? 1.3f : 1.4f, 0f, 0);
            else if (a.Kind == "held")
                TASK.TASK_WANDER_IN_AREA(h, pos.X, pos.Y, pos.Z, 6f, 2f, 5f, 0);
            else
                return;   // a look, a stand: nothing that goes anywhere
            a.Reorders++;
            _ringReorders++;
            Log.Detail("HELD: " + a.Name + " had stopped short, told again (" + a.Reorders + " of " + Ring.MostReorders + ")");
        }

        /// Nothing the mind chose could be done: the nearest of the game's own spots within 10 m for a while, else a stroll
        /// about where they stand. Counted as one of the mind's acts, so the step lets them go when it's over.
        private void HeldFill(int h, MindWho w, Held hd, int now, string lost)
        {
            Vector3 pos = ENTITY.GET_ENTITY_COORDS(h, true, true);
            var a = new MindAct { Handle = h, Kind = "held", Name = w.Name, Started = now };
            string did;
            if (TASK.DOES_SCENARIO_EXIST_IN_AREA(pos, 10f, true, 0, false) && _rng.Next(100) < 60)
            {
                int ms = 40000 + _rng.Next(50000);
                TASK._TASK_USE_NEAREST_SCENARIO_TO_COORD(h, pos, 10f, ms, false, true, false, false);
                a.Until = now + ms;
                a.Spot = true;
                a.LookAt = now + 6000;
                did = "takes the nearest spot";
            }
            else
            {
                int ms = 20000 + _rng.Next(25000);
                TASK.TASK_WANDER_IN_AREA(h, pos.X, pos.Y, pos.Z, 6f, 2f, 5f, 0);
                a.Until = now + ms;
                did = "strolls about";
            }
            _mindActs.Add(a);
            hd.Acts++;
            hd.Fillers++;
            _heldFills++;
            _ringActsThisMinute++;
            hd.Last = did;
            _heldLast = w.Name + " " + did;
            Log.Detail("HELD: " + w.Name + " " + did + " (their choice: " + lost + ")");
        }

        /// The Testing row: a white dot on the map for a minute on everyone the ring holds, to see who it has.
        private string RingMark(int now, out string said)
        {
            said = null;
            if (!HeldOn)
                return "off ([Mind] ControlRing, with MindLive and MindActs)";
            int until = now + 60000, n = 0;
            foreach (KeyValuePair<int, Held> kv in _held)
            {
                if (!ENTITY.DOES_ENTITY_EXIST(kv.Key))
                    continue;
                Blip b = new Ped(kv.Key).AddBlip(BlipType.WhiteDot);
                b.Label = (kv.Value.Name ?? "someone") + " (held)";
                _regBlips.Add(new KeyValuePair<Blip, int>(b, until));   // taken off with the regulars' marks
                n++;
            }
            said = n + " held by the ring: a white dot on each for a minute";
            Log.Write("TEST ring: " + HeldWords(Game.Player.Character.Position));
            return null;
        }

        /// The minute's line and F7's: how many in the ring are held, and how it's gone.
        private string HeldWords(Vector3 at)
        {
            if (!HeldOn)
                return "control ring off ([Mind] ControlRing)";
            int inRing = 0, held = 0, acting = 0;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(at) > HeldInMetres)
                    continue;
                inRing++;
                if (_held.ContainsKey(h))
                {
                    held++;
                    if (IsMindActing(h) || PED.IS_PED_USING_ANY_SCENARIO(h))
                        acting++;
                }
            }
            string gauge = RingOn ? "; ring " + _ringGait.Settled + " " + (int)_ringReach.In + " m, at most " + _ringReach.Most + "; " + _ringActsLastMinute + " decisions last minute; a step " + _ringGuard.AverageMicros + " us on average, peak " + _ringGuard.PeakMicros + " us (budget " + RingGuard.AverageBudget + " / " + RingGuard.PeakBudget + "); guard " + _ringGuard.Scale.ToString("0.00") + ", drawn in " + _ringGuard.Shrinks + " times, eased " + _ringGuard.Eases + "; " + _ringDrawnIn + " let go as it drew in; " + _ringReorders + " told again of " + _ringReorderChecks + " checks; " + _ringRaised + " got up for what they wanted" + (_ringStayed > 0 ? ", " + _ringStayed + " looks at someone content where they sat" : "") + "; " + _ringPlansMade + " errands made, " + _ringPlanSteps + " steps done, " + _ringAsked + " put to the model" : "";
            return (RingOn ? "control ring: " : "held ring: ") + held + " of " + inRing + " named within " + (int)HeldInMetres + " m held" + (inRing > 0 ? " (" + (100 * held / inRing) + "%)" : "") + ", " + acting + " of them at an act or a spot; " +
                   _heldStarts + " held this session, " + _heldChained + " acts chained, " + _heldFills + " spots or strolls, " + _heldEnds + " let go" +
                   (_heldEndWhy.Count > 0 ? " (" + string.Join(", ", _heldEndWhy.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)) + ")" : "") + (_heldLast != null ? "; last: " + _heldLast : "") + gauge;
        }
    }
}
