// The control census, the game side (FOURTEENHOUR item 22; Core/Census.cs; [Mind] ControlCensus). Every 3 s one scan for the humans
// within 25 m (the minute's line averages that), then a third of them a frame, each boxed by the natives the add-on already calls (the
// model's name, mission entity, combat, flight, ragdoll, mount, vehicle, water, scenario, speed) and by our own holds (the Director's
// scene, the ring, an encounter's cast, a wake act, Talk, a nod or a walk-up, a walk together, spawned or borrowed). Whether the mind
// has named them is read off its cache, never asked (so the census names nobody). Once a minute a CONTROL line; F7 TEST control census.
// Its own frame-meter slot ("control census"). It only reads: it gives nobody an order.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly List<int> _censusPass = new List<int>();
        private readonly Census.Tally _censusMinute = new Census.Tally();
        private int _censusNext, _censusPer = 1, _censusPassAt, _censusMinuteAt, _censusMinutes;
        private string _censusLastLine;
        private Action _aCensus;

        private void CensusStep(int now)
        {
            if (_cfg == null || !Census.On || _bridge == null)
                return;
            if (_aCensus == null)
                _aCensus = () => CensusLook(_now);
            Safe("control census", _aCensus);
        }

        private void CensusLook(int now)
        {
            Ped me = Game.Player.Character;
            if (_censusNext >= _censusPass.Count)
            {
                if (now < _censusPassAt)
                    return;
                _censusPassAt = now + Census.PassMs;
                _censusPass.Clear();
                Vector3 at = me.Position;
                foreach (Ped p in Peds.All())
                    if (p != null && p.Exists() && p.Handle != me.Handle && !p.IsDead && PED.IS_PED_HUMAN(p.Handle) && p.Position.DistanceTo(at) <= Census.LineMetres)
                        _censusPass.Add(p.Handle);
                _censusNext = 0;
                _censusPer = Math.Max(1, (_censusPass.Count + 2) / 3);   // a third a frame
                _censusMinute.Looks++;
            }
            for (int n = 0; n < _censusPer && _censusNext < _censusPass.Count; n++)
            {
                int h = _censusPass[_censusNext++];
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
                    _censusMinute.Add(CensusFacts(new Ped(h)));
            }
            if (now >= _censusMinuteAt)
            {
                if (_censusMinuteAt > 0 && _censusMinute.Looks > 0)
                {
                    _censusLastLine = Census.Line(_censusMinute);
                    _censusMinutes++;
                    Log.Write("CONTROL minute: " + _censusLastLine);
                }
                _censusMinute.Clear();
                _censusMinuteAt = now + Census.MinuteMs;
            }
        }

        /// What decides who has this person now, read and never changed.
        private Census.Facts CensusFacts(Ped p)
        {
            int h = p.Handle;
            string model = Natives.ModelName(p);
            bool record = IsRecord(h);
            MindWho w;
            var f = new Census.Facts
            {
                Story = Natives.StoryModel(p) || (MISC.GET_MISSION_FLAG() && Natives.OtherScripts(h) && !record),
                GameEncounter = model.StartsWith("re_", StringComparison.Ordinal),
                OtherScript = Natives.OtherScripts(h),
                Record = record,
                RecordInRole = record && _bridge.EnrageFighter(h),
                Fighting = PED.IS_PED_IN_COMBAT(h, 0),
                Fleeing = PED.IS_PED_FLEEING(h),
                Down = PED.IS_PED_RAGDOLL(h) || PED.IS_PED_INCAPACITATED(h) || PED.IS_PED_HOGTIED(h),
                Riding = PED.IS_PED_ON_MOUNT(h),
                Vehicle = PED.IS_PED_IN_ANY_VEHICLE(h, false),
                Water = PED.IS_PED_SWIMMING(h) || ENTITY.IS_ENTITY_IN_WATER(h),
                Scenario = PED.IS_PED_USING_ANY_SCENARIO(h),
                Moving = ENTITY.GET_ENTITY_SPEED(h) > 0.6f,
                Named = _mindWho.TryGetValue(h, out w) && w.Id != null,
                Ours = OursHolding(h)
            };
            if (f.Scenario)
                f.Scenery = SceneryOf(ScenarioName(p));
            return f;
        }

        /// Which feature of ours holds them, or null.
        private string OursHolding(int h)
        {
            if (Spawner.IsOurs(h)) return "spawned or borrowed";
            if (_director.Involves(h)) return "a scene";
            if (_ring.Contains(h)) return "the ring";
            if (IsStaged(h)) return "an encounter's cast";
            if (InWakeAct(h)) return "a wake act";
            if (InTalk(h)) return "talking with you";
            if (IsAcknowledging(h)) return "a nod or a walk-up";
            if (WalkingTogether(h)) return "walking together";
            if (NotFree(h)) return "another job of ours";
            return null;
        }

        /// A scenario's name in a word: sitting, leaning, at the bar, drinking, smoking, at work, or another.
        private static string SceneryOf(string name)
        {
            string n = name ?? string.Empty;
            if (n.Contains("BAR")) return "at the bar";
            if (n.Contains("SEAT") || n.Contains("SIT") || n.Contains("CHAIR") || n.Contains("BENCH")) return "sitting";
            if (n.Contains("LEAN")) return "leaning";
            if (n.Contains("DRINK")) return "drinking";
            if (n.Contains("SMOK")) return "smoking";
            if (n.Contains("WORK") || n.Contains("BROOM") || n.Contains("HAMMER") || n.Contains("SAW") || n.Contains("SHOVEL") || n.Contains("WASH")) return "at work";
            return "another";
        }

        /// Why the game has them, in a word (the ring's "the game has them" and the directing's "can't be directed now", split).
        private string CantDirectWhy(Ped ped, bool fleeingOk = false)
        {
            int h = ped.Handle;
            if (ENTITY.IS_ENTITY_DEAD(h)) return "dead";
            if (PED.IS_PED_IN_COMBAT(h, 0)) return "fighting";
            if (!fleeingOk && PED.IS_PED_FLEEING(h)) return "fleeing";
            if (PED.IS_PED_RAGDOLL(h)) return "down";
            if (PED.IS_PED_ON_MOUNT(h)) return "riding";
            if (PED.IS_PED_IN_ANY_VEHICLE(h, false)) return "in a vehicle";
            if (PED.IS_PED_SWIMMING(h) || ENTITY.IS_ENTITY_IN_WATER(h)) return "in the water";
            if (Natives.OtherScripts(h)) return "another script's";
            if (Spawner.IsOurs(h)) return "ours";
            return "unknown";
        }

        /// F7: the minute so far, and the last minute's line.
        private string CensusStatus()
        {
            if (!Census.On)
                return "off ([Mind] ControlCensus)";
            return (_censusMinute.Looks > 0 ? "this minute so far: " + Census.Line(_censusMinute) : "this minute: no looks yet") + (_censusLastLine != null ? "; the last minute: " + _censusLastLine : "") +
                   " (" + _censusMinutes + " minutes logged)";
        }
    }
}
