// Stage 1 of 0.7.12 (NIGHT4 queue item 3; Lex, 2026-10-31: "people near Arthur visibly act on their own minds"): the
// mind's everyday choices acted through the game's own scenarios (Core/MindScenarios.cs has the pure parts). Someone sat
// or leaning whose choice gets them up is asked up Rockstar's way, as the waking's (Game/ScenarioExit.cs: events blocked as
// its Take, the scenario's exit flag and a stand-still the same tick, never a bare clear; asked once more at 4 s only if they
// never began the exit; at 9 s put back in their scenario, never left frozen; once up, stood still 3 s and the next order
// 2.5 s on), then goes on to it: a bench, a chair, the steps, a wall, a post, a rail, the bar, a table, a broom, found by type
// (FIND_SCENARIO_OF_TYPE_HASH, Rockstar's ambient_load pattern: find by type, check it exists and is free, use it), or the
// saloon, the store, the church, home, a stroll, someone to talk to (two already talking first). Standing people's rests,
// meals, drinks and work go by type too, where before the nearest point of any kind was taken (work became a smoke). One
// start every 4-6 s near you, three at once, never two within 8 m; someone not out in time is put back (counted, and
// looked at 3 s on); each let go to the game's own ambient life when done. Behind [Mind] MindActsScenarios (with MindLive, MindActs).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _s1NextAt, _s1Starts, _s1GotUp, _s1Walked, _s1TookUp, _s1Joined, _s1Watched, _s1UpMsSum;
        private readonly Dictionary<string, int> _s1TookKind = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _s1Lost = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _s1TypeFound = new Dictionary<string, int>(), _s1TypeNone = new Dictionary<string, int>();
        private string _s1Last;
        private const string S1Kept = "not out of their scenario in time: put back", S1NoPoint = "no fitting point near (typed)", S1Never = "never reached the point", S1Nothing = "up, but nothing to go to",
                             S1Taken = "the game took them (a fight, a flee, a fall) or enRAGE";
        // points they never reached, skipped by the next searches for a while (the review: no navmesh route, the same point each time)
        private const int S1BadPoints = 8, S1BadPointMs = 300000;
        private readonly int[] _s1BadPoint = new int[S1BadPoints], _s1BadUntil = new int[S1BadPoints];
        private int _s1BadNext;

        private void S1RememberBad(int p, int now)
        {
            if (p == 0)
                return;
            _s1BadPoint[_s1BadNext] = p;
            _s1BadUntil[_s1BadNext] = now + S1BadPointMs;
            _s1BadNext = (_s1BadNext + 1) % S1BadPoints;
        }

        private bool S1IsBad(int p, int now)
        {
            for (int i = 0; i < S1BadPoints; i++)
                if (_s1BadPoint[i] == p && now < _s1BadUntil[i] && _s1BadUntil[i] - now <= S1BadPointMs)
                    return true;
            return false;
        }

        // ScenarioExit.Step's "another feature has them now": the look at someone put back after a get-up is left alone (made once)
        private Func<int, bool> _exitBusy;

        private bool ExitBusy(int h)
        {
            return _director.Involves(h) || InTalk(h) || IsStaged(h) || IsMindActing(h) || !CanDirect(new Ped(h)) || S1GameHasThem(h);   // nobody else's, enRAGE's included (MayDirect's rule)
        }

        private void S1Lose(string why)
        {
            int n;
            _s1Lost.TryGetValue(why, out n);
            _s1Lost[why] = n + 1;
        }

        /// Stage-1 acts getting up or on their way now (to a point, the bar, home, a talk: every act begun as a stage-1 start, until
        /// it ends or is there; the review), and the nearest of their starts to `at`.
        private int S1Active(Vector3 at, out float nearest)
        {
            int n = 0;
            nearest = 999f;
            for (int i = 0; i < _mindActs.Count; i++)
            {
                MindAct a = _mindActs[i];
                if (!a.S1Begun || a.TookUp || a.Arrived)
                    continue;
                n++;
                float d = a.StartAt.DistanceTo(at);
                if (d < nearest)
                    nearest = d;
            }
            return n;
        }

        /// The rate limit (guard 2), or null: go.
        private string S1Refuse(MindWho w, Vector3 at, int now)
        {
            float nearest;
            int active = S1Active(at, out nearest);
            return MindScenarios.Refuse(now, _s1NextAt, active, nearest, w.Stage1RestUntil);
        }

        private void S1Started(MindWho w, int now)
        {
            _s1Starts++;
            _s1NextAt = MindScenarios.NextAt(now, (uint)Voice.Hash(w.Id + "|s1|" + now / 1000));
        }

        /// A point of the plan's types near `at`, free, not you, not one another act is heading to: its handle, where and its
        /// type, or 0. At most ten types asked, once a start (every 4 s or more).
        // a type found taken or missing in a 10 m cell isn't asked there again for 25 s, and a sweep that found nothing holds the
        // next one, anyone's, for 1 s (the frame-cost audit, NIGHT4 item 6: a full street ran a ten-query sweep for every chooser
        // and every retry; only a start moved the 4-6 s gap)
        private readonly Dictionary<long, int> _s1Miss = new Dictionary<long, int>();
        private int _s1SearchNextAt;
        private const int S1MissMs = 25000, S1SearchGapMs = 1000;

        private static long S1MissKey(uint hash, Vector3 at)
        {
            long cx = (long)Math.Floor(at.X / 10f) & 0xFFFF, cy = (long)Math.Floor(at.Y / 10f) & 0xFFFF;
            return ((long)hash << 32) | (cx << 16) | cy;
        }

        /// No sweep failed in the last second (GameTime going back, a reload, never holds it).
        private bool S1MaySearch(int now)
        {
            return now >= _s1SearchNextAt || _s1SearchNextAt - now > S1SearchGapMs;
        }

        private int S1FindPoint(Vector3 at, MindScenarios.Plan pl, out Vector3 pAt, out string name)
        {
            pAt = default(Vector3);
            name = null;
            int wd = WatchEnter("mind scenarios: find a point");
            try
            {
                Vector3 me = Game.Player.Character.Position;
                int now = Game.GameTime;
                int asked = 0;
                if (_s1Miss.Count > 256)
                    _s1Miss.Clear();
                foreach (string t in pl.Types)
                {
                    if (asked >= 10)
                        break;
                    uint hash = Natives.Hash(t);
                    long cell = S1MissKey(hash, at);
                    int missUntil;
                    if (_s1Miss.TryGetValue(cell, out missUntil) && now < missUntil && now > missUntil - S1MissMs - 1000)
                        continue;   // none of this type free here a few seconds ago: not asked again yet (the frame-cost audit)
                    asked++;
                    int p = TASK.FIND_SCENARIO_OF_TYPE_HASH(at, hash, pl.Radius, 1UL, false);   // Rockstar's own args (ambient_load.c)
                    bool ok = p != 0 && TASK.DOES_SCENARIO_POINT_EXIST(p) && TASK._GET_PED_USING_SCENARIO_POINT(p) == 0;
                    Vector3 c = ok ? TASK._GET_SCENARIO_POINT_COORDS(p, true) : default(Vector3);
                    if (ok && (c.DistanceTo(at) > pl.Radius + 1f || c.DistanceTo(me) < MindScenarios.PointFromYou))
                        ok = false;
                    if (ok && S1IsBad(p, now))
                        ok = false;   // never reached a while ago: another type, or nothing
                    if (ok)
                        for (int i = 0; i < _mindActs.Count; i++)
                            if (_mindActs[i].Point == p)
                            {
                                ok = false;   // another is on the way to it
                                break;
                            }
                    Dictionary<string, int> tally = ok ? _s1TypeFound : _s1TypeNone;
                    int n;
                    tally.TryGetValue(t, out n);
                    tally[t] = n + 1;
                    if (n == 0)
                        (ok ? (Action<string>)Log.Write : Log.Detail)("MIND scenarios: " + t + (ok ? " found near " + (_placeTown ?? "here") + " (the first)" : " none free near " + (_placeTown ?? "here") + " (the first miss)"));   // which types this game has where, once each
                    if (!ok)
                    {
                        _s1Miss[cell] = now + S1MissMs;
                        continue;
                    }
                    pAt = c;
                    name = t;
                    return p;
                }
                _s1SearchNextAt = now + S1SearchGapMs;   // a sweep that found nothing: the next one waits a moment, whoever asks (the frame-cost audit)
                return 0;
            }
            finally
            {
                WatchLeave(wd);
            }
        }

        /// A rest, a meal, a drink or the work by the game's points of that type (standing, or just got up): sent to the point
        /// (the proven nearest-to-coords call at the point itself, 1.5 m, so the nearest is the one found), else to the bar,
        /// else something in place that fits, else nothing. Null when it began, else why not.
        private string S1Point(MindAct a, MindWho w, int h, string kind, Vector3 at, int now, out string did)
        {
            did = null;
            uint seed = (uint)Voice.Hash(w.Id + "|" + kind + "|s1");
            bool woman = w.P != null && w.P.Woman;
            MindScenarios.Plan pl = MindScenarios.PlanFor(kind, woman, w.TradeGroup == "gentry", _placeKind == "saloon", seed);
            if (pl == null)
                return LostNoAct;
            int hold = MindScenarios.HoldMs(seed);
            Vector3 pAt;
            string name;
            int p = a.Point != 0 && TASK.DOES_SCENARIO_POINT_EXIST(a.Point) && TASK._GET_PED_USING_SCENARIO_POINT(a.Point) == 0 ? a.Point : 0;
            if (p != 0)
            {
                pAt = a.PointAt;
                name = a.PointType;
            }
            else if (!S1MaySearch(now))
                return LostGap;   // a sweep found nothing a moment ago: tried again later (the frame-cost audit)
            else
                p = S1FindPoint(at, pl, out pAt, out name);
            a.Stage1 = true;
            a.StartAt = at;
            if (p != 0)
            {
                float d = pAt.DistanceTo(at);
                TASK._TASK_USE_NEAREST_SCENARIO_TO_COORD(a.Handle, pAt, 1.5f, hold, false, true, false, false);
                a.Point = p;
                a.PointAt = pAt;
                a.PointType = name;
                a.PointHash = Natives.Hash(name);
                a.ReachBy = now + MindScenarios.ReachMs(d);
                a.Until = a.ReachBy + hold;
                a.NextPoll = now + 2000;
                _s1Walked++;
                did = MindScenarios.Words(name) + " (" + (int)d + " m off)";
                return null;
            }
            if (pl.Place != null && MindToPlace(a, w, pl.Place, at, now, out did))
            {
                a.Stage1 = false;   // to the bar: the place's own arrival (MindActsStep), as before
                return null;
            }
            if (pl.InPlace != null)
            {
                a.PointHash = Natives.Hash(pl.InPlace);
                a.PointType = pl.InPlace;
                a.InPlace = true;
                int brief = MindScenarios.InPlaceHoldMs(seed);   // 12-25 s in place, not a point's minute or more (NIGHT4 item 6)
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(a.Handle, a.PointHash, brief, true, 0u, -1f, false);   // the Director's proven call
                a.ReachBy = now + 5000;
                a.Until = a.ReachBy + brief;
                a.NextPoll = now + 2000;
                did = MindScenarios.Words(pl.InPlace) + " (no fitting spot near)";
                return null;
            }
            a.Stage1 = false;
            return S1NoPoint;
        }

        /// Someone sat or leaning gets up for their choice (from MindActTry, after its checks, the gap and the cap): the
        /// destination first (guard 3: a point of the right type, a place, someone to talk to, or something in place), then the
        /// waking's get-up. Null when it began, else why not (the caller may fall back to a look from the seat).
        private string MindGetUp(MindWho w, int h, string kind, int now, Ped me, bool test)
        {
            if (_bridge != null && _bridge.RecordAt(h) != null)
                return MindRetry.Busy;   // one of enRAGE's own: never up from their business (guard 6, to be safe; MayDirect has it too)
            Vector3 at = ENTITY.GET_ENTITY_COORDS(h, true, true);
            if (!test)
            {
                string gate = S1Refuse(w, at, now);
                if (gate != null)
                    return gate;
            }
            var a = new MindAct { Handle = h, Kind = kind, Name = w.Name, Started = now, Stage1 = true, S1Begun = true, GettingUp = true, StartAt = at, UpAskedAt = now, NextPoll = now + MindScenarios.PollMs };
            uint type = (uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h);
            a.FromType = MindScenarios.NameOf(type) ?? "scenario 0x" + type.ToString("X8");
            // somewhere to go to, before anyone is asked up
            if (MindScenarios.ByPoint(kind))
            {
                if (!S1MaySearch(now))
                    return LostGap;   // a sweep found nothing a moment ago: tried again later (the frame-cost audit)
                MindScenarios.Plan pl = MindScenarios.PlanFor(kind, w.P != null && w.P.Woman, w.TradeGroup == "gentry", _placeKind == "saloon", (uint)Voice.Hash(w.Id + "|" + kind + "|s1"));
                Vector3 pAt = default(Vector3);
                string name = null;
                int p = pl != null ? S1FindPoint(at, pl, out pAt, out name) : 0;
                if (p != 0)
                {
                    a.Point = p;
                    a.PointAt = pAt;
                    a.PointType = name;
                }
                else if (pl == null || (pl.InPlace == null && !(pl.Place != null && MindPlaceNear(pl.Place, at))))
                {
                    w.Stage1RestUntil = now + MindScenarios.FailRestMs;
                    S1Lose(S1NoPoint);
                    return S1NoPoint;
                }
            }
            else if (kind == "talk")
            {
                int partner;
                int to = MindPairToJoin(h, at, out partner);
                if (to == 0)
                    to = MindSomeoneToTalkTo(h, at);
                if (to == 0)
                    return LostNoTalk;
                a.Toward = to;   // kept for S1Send (rechecked there, not searched again: the review)
                a.Partner = partner;
            }
            else if (kind == "steerclear" && MindFoeNear(w, h) == 0)
                return LostNoFoe;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);   // the waking's Take: nothing pulls them back to the point or into a reaction halfway out
            a.Blocked = true;
            // up Rockstar's way: the exit flag (toward the point, when there is one) and the stand-still the same tick, no
            // clear (a bare clear froze 20 of 23 in Valentine, 2026-11-01; ScenarioExit.cs)
            a.Exit = ScenarioExit.Ask(h, w.Name, "MIND", a.Point != 0 ? (Vector3?)a.PointAt : null, now);
            Spawner.StandUp(h);
            a.Until = now + MindScenarios.UpMostMs + 60000;
            _mindActs.Add(a);
            _mindActNextAt = now + MindActGapMs;
            if (!test)
                S1Started(w, now);
            NoteMindAct(kind + ", gets up");
            _mindActLast = w.Name + " gets up to " + Choosing.WordsOf(kind);
            Log.Write("MIND gets up: " + w.Name + " (" + a.FromType + ") -> " + Choosing.WordsOf(kind) + (a.PointType != null ? ", to " + a.PointType : "") + " (" + (test ? "the Testing row" : w.ChoiceWhy) + ", " + (int)at.DistanceTo(me.Position) + " m from you; exit " + a.Exit.Path + ")");
            return null;
        }

        /// Whether a place of this kind (the bar) is known within 150 m.
        private bool MindPlaceNear(string kind, Vector3 at)
        {
            PlaceSpot s = PlaceSpots.Find(_store.Spots, _placeTown, PlaceSpots.KindFor(kind), at.X, at.Y);
            return s != null && new Vector3(s.X, s.Y, s.Z).DistanceTo(at) <= 150f;
        }

        /// Two already talking, 4-12 m off, both free and standing within 2.5 m of each other: the nearer of them (a third joins
        /// in), and the other in `partner`; 0 for none. Over the mind's near ring only.
        // MindPairToJoin's one pass (the review: it was O(n^2) in natives): who, where, and whether standing and free, reused
        private readonly int[] _pairWho = new int[Levels.NearAllMax];
        private readonly Vector3[] _pairAt = new Vector3[Levels.NearAllMax];

        private int MindPairToJoin(int h, Vector3 at, out int partner)
        {
            partner = 0;
            if (!_cfg.MindActsScenarios)
                return 0;
            int n = 0;
            int wd = WatchEnter("mind acts: a pair to join");
            try
            {
                // the natives once each: only people who could be one of the pair (within 12 + 2.5 m, standing, free, not acting)
                for (int i = 0; i < _mindNear.Count && n < _pairWho.Length; i++)
                {
                    int x = _mindNear[i];
                    if (x == h || !ENTITY.DOES_ENTITY_EXIST(x) || ENTITY.IS_ENTITY_DEAD(x))
                        continue;
                    Vector3 xa = ENTITY.GET_ENTITY_COORDS(x, true, true);
                    if (xa.DistanceTo(at) > 14.5f || PED.IS_PED_USING_ANY_SCENARIO(x) || NotFree(x) || IsMindActing(x) || !MindTalkable(x))
                        continue;
                    _pairWho[n] = x;
                    _pairAt[n] = xa;
                    n++;
                }
            }
            finally
            {
                WatchLeave(wd);
            }
            // then the pairs, in plain arithmetic: the nearer of two standing within 2.5 m of each other, 4-12 m off
            int best = 0;
            float bestD = 12f;
            for (int i = 0; i < n; i++)
            {
                float d = _pairAt[i].DistanceTo(at);
                if (d < 4f || d >= bestD)
                    continue;
                for (int j = 0; j < n; j++)
                {
                    if (j == i || _pairAt[j].DistanceTo(_pairAt[i]) > 2.5f)
                        continue;
                    best = _pairWho[i];
                    bestD = d;
                    partner = _pairWho[j];
                    break;
                }
            }
            if (best == 0)
                partner = 0;
            return best;
        }

        /// One stage-1 act's step from MindActsStep: 0 not stage 1's now (the act's own handling goes on), 1 keep it, 2 it's
        /// over (removed by the caller, with no orders left behind). The natives are asked every 250 ms at most.
        private int S1Step(MindAct a, int h, int now)
        {
            if (now < a.NextPoll && a.NextPoll - now <= 5000)
                return 1;
            int wd = WatchEnter("mind acts: the getting up");
            try
            {
                a.NextPoll = now + MindScenarios.PollMs;
                if (S1GameHasThem(h, a, now))
                {
                    // the game took them (a fight, a flee, a fall) or enRAGE has them now: let go with no orders, as MindActWhyNot
                    // refuses at the start (the review: a stand-still, a walk or a wander in the gunfire)
                    S1Lose(S1Taken);
                    Log.Detail("MIND acts: " + a.Name + " let go: " + S1Taken + (a.GettingUp ? " (getting up)" : ""));
                    return 2;
                }
                if (a.GettingUp)
                {
                    string step = ScenarioExit.Poll(a.Exit, now);   // the flags logged on change; the one second ask made there
                    if (step == "up")
                    {
                        a.GettingUp = false;
                        _s1GotUp++;
                        _s1UpMsSum += now - a.UpAskedAt;
                        TASK.TASK_STAND_STILL(h, MindScenarios.StandMs);   // on their feet: 3 s stood, the next order 2.5 s on (the waking's Phase 1, exactly)
                        a.SendAt = now + MindScenarios.SendAfterUpMs;
                        a.NextPoll = a.SendAt;
                        Log.Write("MIND up: " + a.Name + " from " + a.FromType + " (" + ScenarioExit.UpWords(a.Exit, now) + ")");
                        return 1;
                    }
                    if (step == "restore")
                    {
                        // not out in time: put back in their scenario (their point, their type in place, or a wander), never left
                        // as is, looked at again 3 s on (ScenarioExit.Step); not asked again for a while
                        MindWho kw;
                        if (_mindWho.TryGetValue(h, out kw))
                        {
                            kw.Stage1RestUntil = now + MindScenarios.FailRestMs;
                            kw.ActWhyNot = S1Kept;
                        }
                        S1Lose(S1Kept);
                        ScenarioExit.Restore(a.Exit, now);
                        return 2;
                    }
                    return 1;
                }
                if (a.SendAt > 0)
                {
                    if (now < a.SendAt)
                        return 1;
                    a.SendAt = 0;
                    return S1Send(a, h, now);
                }
                if (a.PointHash != 0 && !a.TookUp)
                {
                    bool there = (a.Point != 0 && PED.IS_PED_USING_THIS_SCENARIO(h, a.Point)) || PED.IS_PED_USING_SCENARIO_HASH(h, a.PointHash) ||
                                 (a.InPlace && PED.IS_PED_USING_ANY_SCENARIO(h));
                    if (there)
                    {
                        a.TookUp = true;
                        a.NextPoll = now + 2000;
                        _s1TookUp++;
                        MindWho tw;
                        if (!a.InPlace && _mindWho.TryGetValue(h, out tw))
                            tw.Stage1RestUntil = now + MindScenarios.FailRestMs;   // sat or leaning where they chose: not got up again for 4 min (NIGHT4 item 6: benches sat on and left in turn)
                        string k = MindScenarios.CountKind(a.PointType, a.InPlace);
                        int n;
                        _s1TookKind.TryGetValue(k, out n);
                        _s1TookKind[k] = n + 1;
                        _s1Last = a.Name + " " + MindScenarios.Words(a.PointType);
                        Log.Write("MIND took up: " + a.Name + " " + a.PointType + (a.InPlace ? " in place" : " at " + (int)ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(a.StartAt) + " m") + " (" + Choosing.WordsOf(a.Kind) + ")");
                        return 2;   // there: let go at once, no orders left (the scenario runs its own time as the game's ambient life; the cap of 10 stays free)
                    }
                    if (now >= a.ReachBy)
                    {
                        S1Lose(S1Never);
                        S1RememberBad(a.Point, now);   // the next searches skip it a while (the review)
                        MindWho nw;
                        if (_mindWho.TryGetValue(h, out nw))
                            nw.Stage1RestUntil = now + MindScenarios.FailRestMs;
                        if (Log.Debug) Log.Detail("MIND acts: " + a.Name + " never reached " + (a.PointType ?? "the point") + (PED.IS_PED_USING_ANY_SCENARIO(h) ? " (took another: 0x" + ((uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h)).ToString("X8") + ")" : ""));
                        if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                            TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // on their way, not stood there
                        return 2;
                    }
                    return 1;
                }
                return 0;
            }
            finally
            {
                WatchLeave(wd);
            }
        }

        /// Up and stood a moment: the act itself (the same as a standing person's, MindActBegin). 1 when it began, 2 when there
        /// was nothing after all (on their way, a wander).
        private int S1Send(MindAct a, int h, int now)
        {
            MindWho w;
            Ped me = Game.Player.Character;
            if (!_mindWho.TryGetValue(h, out w) || w.Id == null)
            {
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                return 2;
            }
            string did;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(h, true, true);
            S1Unblock(a, false);   // the get-up is over: the walk itself goes as a standing person's, the game's reactions free
            string lost = MindActBegin(a, w, h, a.Kind, now, me, at, out did);
            if (lost != null)
            {
                S1Lose(S1Nothing);
                w.Stage1RestUntil = now + MindScenarios.FailRestMs;   // not got up again for nothing straight after (the review)
                Log.Detail("MIND acts: " + a.Name + " got up, but " + lost + ": on their way");
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                return 2;
            }
            if (a.PointHash == 0)
                a.Stage1 = false;   // a walk to someone, a place or home: the act's own handling from here (MindActsStep)
            _mindActLast = w.Name + " gets up and " + did;
            Log.Write("MIND acts: " + w.Name + " gets up and " + did + " (" + w.ChoiceWhy + ", " + (int)at.DistanceTo(me.Position) + " m from you)");
            return 1;
        }

        /// F7: stage 1's own counts.
        private string S1Status()
        {
            if (!_cfg.MindActsScenarios)
                return "getting up and the game's own spots off ([Mind] MindActsScenarios); " + ScenarioExit.Status();
            float nearest;
            int active = S1Active(Game.Player.Character.Position, out nearest);
            return "stage 1: " + _s1Starts + " started, got up " + _s1GotUp + (_s1GotUp > 0 ? " (" + (_s1UpMsSum / _s1GotUp) / 100 / 10.0 + " s on average)" : "") + ", walked to a point " + _s1Walked +
                   ", took up " + _s1TookUp + " (" + (_s1TookKind.Count == 0 ? "none yet" : string.Join(", ", _s1TookKind.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) + ")" +
                   ", joined two talking " + _s1Joined + ", stopped to watch you " + _s1Watched +
                   "; failed: " + (_s1Lost.Count == 0 ? "none" : string.Join(", ", _s1Lost.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   "; getting up or on the way now (to a point, the bar, home or a talk) " + active + " of " + MindScenarios.AtOnce + " (one start every " + MindScenarios.GapMs / 1000 + "-" + (MindScenarios.GapMs + MindScenarios.GapJitterMs) / 1000 + " s, never two within " + (int)MindScenarios.ApartMetres + " m)" +
                   "; types found " + _s1TypeFound.Count + ", never found here " + _s1TypeNone.Keys.Count(k => !_s1TypeFound.ContainsKey(k)) + (_s1Last != null ? "; last: " + _s1Last : "") +
                   "; " + ScenarioExit.Status();
        }

        /// The Testing row: the nearest person the mind knows who is sat or leaning, free, within 15 m, gets up for their own
        /// choice if it gets them up, else for a rest elsewhere (a bench or a wall), past the rate limit once.
        private string GetUpTest(int now)
        {
            if (!_cfg.MindLive || !_cfg.MindActs || !_cfg.MindActsScenarios)
                return "[Mind] MindLive, MindActs and MindActsScenarios must be on";
            Ped me = Game.Player.Character;
            int best = 0;
            float bestD = 15f;
            string lastWhy = null;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || !PED.IS_PED_USING_ANY_SCENARIO(h) || IsMindActing(h))
                    continue;
                string why = MindActWhyNot(new Ped(h), h, true);
                if (why != null)
                {
                    lastWhy = w.Name + ": " + why;
                    continue;
                }
                float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position);
                if (d < bestD)
                {
                    bestD = d;
                    best = h;
                }
            }
            if (best == 0)
                return "nobody the mind knows sat or leaning and free within 15 m" + (lastWhy != null ? " (the last passed over: " + lastWhy + ")" : "");
            MindWho bw = _mindWho[best];
            string kind = bw.Choice != null && MindScenarios.GetsUpFor(bw.Choice) && MindAlreadyAt(bw.Choice, best) == null ? bw.Choice : "rest";
            string lost = MindGetUp(bw, best, kind, now, me, true);
            if (lost != null && kind != "rest")
                lost = MindGetUp(bw, best, "rest", now, me, true);
            return lost == null ? null : bw.Name + ": " + lost;
        }
    }
}
