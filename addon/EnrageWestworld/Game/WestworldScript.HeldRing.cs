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
            public string Last;
        }

        private readonly Dictionary<int, Held> _held = new Dictionary<int, Held>();
        private readonly Dictionary<int, int> _heldRest = new Dictionary<int, int>();   // let go: not held again before this
        private const int HeldRestMs = 120000, HeldMost = 8, HeldStepMs = 500;
        private int _heldStepAt, _heldStarts, _heldChained, _heldFills, _heldEnds;
        private readonly Dictionary<string, int> _heldEndWhy = new Dictionary<string, int>();
        private string _heldLast;

        private bool HeldOn { get { return _cfg != null && _cfg.HeldRing && _cfg.MindLive && _cfg.MindActs; } }
        private float HeldInMetres { get { return Math.Max(4f, Math.Min(20f, _cfg.HeldRingMetres)); } }
        private float HeldOutMetres { get { return HeldInMetres + 13f; } }

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
            Ped me = Game.Player.Character;
            Vector3 at = me.Position;
            string all = HeldAllWhy(me);
            foreach (int h in _held.Keys.ToList())
            {
                string why = all ?? HeldWhyNot(h, at, now);
                if (why != null)
                    HeldLetGo(h, why, now, all == null);   // a let-go of everyone (you mounted, fired) leaves no rest: back in once it's over
            }
            if (all != null)
                return;
            // who comes in: the named people the mind has near, nearest first
            if (_held.Count < HeldMost)
                foreach (int h in _mindNear.OrderBy(x => ENTITY.GET_ENTITY_COORDS(x, true, true).DistanceTo(at)))
                {
                    if (_held.Count >= HeldMost)
                        break;
                    if (_held.ContainsKey(h))
                        continue;
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    if (ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(at) > HeldInMetres)
                        break;   // ordered by distance: the rest are farther
                    int rest;
                    if (_heldRest.TryGetValue(h, out rest) && now < rest && now >= rest - HeldRestMs - 1000)
                        continue;
                    Ped p = new Ped(h);
                    if (PED.IS_PED_USING_ANY_SCENARIO(h))
                        continue;   // sat or at a spot: theirs; the minute's choice gets them up, and on their feet they're taken in (a bench full of the held was a ring of nobody chained)
                    if (MindChosenQuietly(h) || MindActWhyNot(p, h, false) != null || !CanDirect(p))
                        continue;
                    _held[h] = new Held { Name = w.Name, Since = now, NextAt = now };
                    _heldStarts++;
                    Log.Write("HELD: " + w.Name + " held, " + (int)p.Position.DistanceTo(at) + " m from you (" + (w.Choice != null ? Choosing.WordsOf(w.Choice) : "no choice yet") + ")");
                }
            // keep each going: the next act the moment one ends
            foreach (KeyValuePair<int, Held> kv in _held.ToList())
            {
                int h = kv.Key;
                Held hd = kv.Value;
                if (now < hd.NextAt || IsMindActing(h) || InWakeAct(h))
                    continue;
                if (PED.IS_PED_USING_ANY_SCENARIO(h))
                {
                    hd.NextAt = now + 3000;   // at a seat or a spot: theirs, untouched (the minute's choice may get them up)
                    continue;
                }
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.P == null)
                {
                    HeldLetGo(h, "the mind lost them", now);
                    continue;
                }
                HeldNext(h, w, hd, me, now);
            }
        }

        /// What lets go everyone at once, or null.
        private string HeldAllWhy(Ped me)
        {
            if (MISC.GET_MISSION_FLAG() || _missionPaused) return "a mission";
            if (me.IsDead) return "you died";
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0) return "you're wanted";
            if (PED.IS_PED_SHOOTING(me.Handle)) return "you fired";
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false)) return "you're riding";
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
                if (!CanDirect(p)) return "the game has them";
                return null;
            }
            if (PED.IS_ANY_PED_SHOOTING_IN_AREA(pos.X - 40f, pos.Y - 40f, pos.Z - 15f, pos.X + 40f, pos.Y + 40f, pos.Z + 15f, false, false)) return "gunfire near";
            if (!CanDirect(p)) return "the game has them";
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

        /// The next act for one held between acts: the mind's choice, the one it ranked next, a spot, a stroll. Never nothing.
        private void HeldNext(int h, MindWho w, Held hd, Ped me, int now)
        {
            hd.NextAt = now + 1500;
            string lost = HeldTry(w, h, now, w.MindChoice ?? w.Choice);
            if (lost != null && w.ChoiceNext != null && w.ChoiceNext != (w.MindChoice ?? w.Choice))
                lost = HeldTry(w, h, now, w.ChoiceNext);
            if (lost == null)
            {
                hd.Acts++;
                _heldChained++;
                hd.Last = _mindActLast;
                _heldLast = _mindActLast;
                Log.Detail("HELD: " + hd.Name + " goes on: " + _mindActLast);
                return;
            }
            if (lost == MindRetry.Aimed || lost == MindRetry.Story || lost == MindRetry.Riding || lost == MindRetry.Busy)
                return;   // HeldWhyNot lets them go on the next step
            HeldFill(h, w, hd, now, lost);
        }

        /// One choice tried as an act now, the mind's own way (MindActOn), past the street's 1.5 s gap: a held one going on
        /// isn't a new start. Null when it began.
        private string HeldTry(MindWho w, int h, int now, string kind)
        {
            if (kind == null || kind == "stand" || Choosing.CarriesOn(kind))
                return LostStand;
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
            return lost;
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
            hd.Last = did;
            _heldLast = w.Name + " " + did;
            Log.Detail("HELD: " + w.Name + " " + did + " (their choice: " + lost + ")");
        }

        /// The minute's line and F7's: how many in the ring are held, and how it's gone.
        private string HeldWords(Vector3 at)
        {
            if (!HeldOn)
                return "held ring off ([Mind] HeldRing)";
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
            return "held ring: " + held + " of " + inRing + " named within " + (int)HeldInMetres + " m held" + (inRing > 0 ? " (" + (100 * held / inRing) + "%)" : "") + ", " + acting + " of them at an act or a spot; " +
                   _heldStarts + " held this session, " + _heldChained + " acts chained, " + _heldFills + " spots or strolls, " + _heldEnds + " let go" +
                   (_heldEndWhy.Count > 0 ? " (" + string.Join(", ", _heldEndWhy.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)) + ")" : "") + (_heldLast != null ? "; last: " + _heldLast : "");
        }
    }
}
