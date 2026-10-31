// Personal space, the game side (Core/Space.cs has the reactions; NIGHT3, reworked after its review). Every 500 ms
// standing still on foot: the one person he walked up to and now stands right up against (under a metre, less in a
// saloon, where only someone facing him minds it: shoulder to shoulder at the bar is nothing), both of them still; held
// for 1.8 s, they mind it through the street reactions' own player (SightPlay: the look, a gesture, a word where the
// feeling has one, a step back and, 2.6 s on, a turn to face him before going on their way; the thought). Someone who
// walked up to him isn't minded; nor anyone while he's locked on to someone (the Talk prompt, the game's Greet), sitting,
// riding, aiming, fighting or in a mission. Once a person in five minutes, 20 s between any two. Never the law, anyone
// busy (a scene, a talk, our errands, the interaction menu), anyone at a counter or in a scenario, a working girl (she
// comes close on purpose), or a horse. [World] PersonalSpace, with [Scenes] SightReactions. Testing row "Someone minds
// you standing so close".
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
        private const int SpaceSampleMs = 500, SpaceGapMs = 20000, SpaceApproachMs = 3000, SpaceTurnMs = 2600;
        private int _spaceNext, _spaceWho, _spaceSince, _spaceGapUntil, _spaceCount, _spaceLastSample, _spaceMovedAt = -100000;
        private bool _spaceTheyCame;   // the one at his elbow walked up to him: theirs to mind, not his
        private readonly Dictionary<int, int> _spaceAgain = new Dictionary<int, int>();   // a handle -> when they may mind it again
        private readonly List<KeyValuePair<int, int>> _spaceTurns = new List<KeyValuePair<int, int>>();   // stepped back: a handle -> when they turn to face him
        private string _spaceLast, _spaceWhyNot;
        private Action _aSpace;

        private void SpaceStep(int now)
        {
            if (!_cfg.PersonalSpace || !_cfg.SightReactions || _store == null || now < _spaceNext)
                return;
            _spaceNext = now + SpaceSampleMs;
            if (_aSpace == null)
                _aSpace = () => SpaceTick(_now, false);
            Safe("personal space", _aSpace);
        }

        /// From SightPlay, once a step back is ordered: they'll turn to face him a moment later, then go on their way.
        private void SpaceTurnLater(int h, int at)
        {
            if (_spaceTurns.Count < 16)
                _spaceTurns.Add(new KeyValuePair<int, int>(h, at));
        }

        private void SpaceTurns(Ped me, int now)
        {
            for (int i = _spaceTurns.Count - 1; i >= 0; i--)
            {
                if (now < _spaceTurns[i].Value)
                    continue;
                int h = _spaceTurns[i].Key;
                _spaceTurns.RemoveAt(i);
                if (me == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _director.Involves(h) || InTalk(h) || NotFree(h) || MayDirectWhyNot(new Ped(h)) != null)
                    continue;
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2000, 0f, 0f, 0f);   // a look back at him, not their back
                WanderLater(h, now + 2500);
            }
        }

        /// The one standing right up against him, held long enough, minds it. Null when it started (or is still being
        /// held), else why not.
        private string SpaceTick(int now, bool test)
        {
            Ped me = Game.Player.Character;
            if (!test && now - _spaceLastSample > 1500)
                _spaceWho = 0;   // a gap (a mission, control off, the panel): the clock starts again
            _spaceLastSample = now;
            SpaceTurns(me, now);
            string why = SpaceCant(me, now, test);
            if (why != null)
            {
                _spaceWho = 0;
                return _spaceWhyNot = why;
            }
            float reach = test ? 3f : _placeKind == "saloon" ? Space.SaloonMetres : Space.Metres, best = reach;
            Vector3 mePos = me.Position;
            Ped near = null;
            int w = WatchEnter("the street scan");   // the hang watch
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Handle == me.Handle)
                    continue;
                float d = p.Position.DistanceTo(mePos);   // the distance first: the rest only for the one or two inside reach
                if (d >= best)
                    continue;
                int ph = p.Handle, again;
                if (!p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(ph) || (!test && _spaceAgain.TryGetValue(ph, out again) && now < again))
                    continue;   // a horse, the dead, someone resting from it
                near = p;
                best = d;
            }
            WatchLeave(w);
            if (near == null)
            {
                _spaceWho = 0;
                return test ? _spaceWhyNot = "nobody within 3 m" : null;
            }
            int h = near.Handle;
            if (!test)
            {
                if (h != _spaceWho)
                {
                    _spaceWho = h;   // someone new at his elbow: the clock starts
                    _spaceSince = now;
                    _spaceTheyCame = now - _spaceMovedAt > SpaceApproachMs;   // he was standing there and they came to him
                    return null;
                }
                if (_spaceTheyCame || now - _spaceSince < Space.LingerMs || ENTITY.GET_ENTITY_SPEED(h) > 0.4f)
                    return null;
                if (_placeKind == "saloon" && !Facing(near, mePos, 70f))
                    return null;   // shoulder to shoulder at the bar: nothing to mind
            }
            string cant = SightCant(near, me);
            if (cant == null && (PED.IS_PED_USING_ANY_SCENARIO(h) || NotFree(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_RAGDOLL(h)))
                cant = "busy, sitting or at the bar";
            if (cant == null && Speech.WorkingGirl(h))
                cant = "a working girl (she comes close on purpose)";
            if (cant != null)
            {
                _spaceAgain[h] = now + 30000;   // not them, not for a while: no asking every tick
                _spaceWho = 0;
                return _spaceWhyNot = (test ? "the nearest can't: " : "") + cant;
            }
            string id, name, temper;
            bool known = LastingId(near, out id, out name, out temper);
            bool woman = !PED.IS_PED_MALE(h);
            string seed = (known ? id : "h:" + h + ":" + ENTITY.GET_ENTITY_MODEL(h)) + "|space|" + now / 60000;
            if (!known)
                temper = Sights.StrangerTemper(seed);
            string regard = SightRegard(near, known ? id : null);
            var run = new SightRun
            {
                By = near, At = me, ToYou = true, Name = SightName(near, known ? name : null, woman), State = "close", Key = "space", Woman = woman, Seed = seed,
                Look = Space.Look(temper, regard, woman, seed, _engine.NowIndoors), Start = now, Step = 0
            };
            _sights.Add(run);
            _sightNext = now + SightGapMs;   // no reaction to his mud a second after this one
            SightSetsLoad();
            if (_spaceAgain.Count > 200)
                foreach (int k in _spaceAgain.Where(kv => kv.Value < now).Select(kv => kv.Key).ToList())
                    _spaceAgain.Remove(k);
            _spaceAgain[h] = now + Space.AgainMs;
            _spaceGapUntil = now + SpaceGapMs;
            _spaceWho = 0;
            _spaceCount++;
            _spaceLast = run.Name + " " + run.Look.Feeling + (run.Look.StepsBack ? ", stepped back" : "") + (test ? " (a test)" : "");
            Log.Write("SPACE " + run.Name + ": you stood " + best.ToString("0.0") + " m off -> " + run.Look.Feeling + " (" + (temper ?? "plain") + (regard != null ? ", " + regard + " of you" : "") +
                      (run.Look.StepsBack ? "; steps back" : "; stands") + (run.Look.Spoken != null ? "; \"" + run.Look.Spoken + "\"" : "") + ")" + (test ? ", a test" : ""));
            return null;
        }

        /// Why nobody minds it now: riding, sitting, aiming, fighting, a mission, locked on to someone, the menu open, a
        /// reaction playing, the gap, or on the move (which is also when he's walking up to someone).
        private string SpaceCant(Ped me, int now, bool test)
        {
            if (me == null || !me.Exists() || me.IsDead || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                return "you're riding";
            if (ENTITY.GET_ENTITY_SPEED(me.Handle) > 0.4f)
            {
                _spaceMovedAt = now;   // walking: whoever he ends up beside, he came to them
                if (!test)
                    return "you're on the move";   // passing close is the game's own business (its bumps); only standing there is minded
            }
            if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return "a mission, a fight or a weapon drawn";
            if (PED.IS_PED_USING_ANY_SCENARIO(me.Handle))
                return "you're sitting or leaning";
            if (_menu != null || _sights.Count > 0)
                return "the Talk menu or another reaction";
            if (!test && (_talkPromptFor != 0 || LockedOnTo() != 0))
                return "you're locked on to someone (Talk, or the game's Greet)";
            if (!test && now < _spaceGapUntil)
                return "20 s between";
            return null;
        }

        /// The ped he's locked on to (the game's interaction target), or 0.
        private static unsafe int LockedOnTo()
        {
            int target = 0;
            PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            return target;
        }

        /// The Testing row: the nearest within 3 m minds it now (who came to whom, the second and more, the gap and the five
        /// minutes skipped).
        private string SpaceTest(int now)
        {
            if (!_cfg.SightReactions)
                return "[Scenes] SightReactions is off (personal space plays through it)";
            return SpaceTick(now, true);
        }

        /// F7: on or off, how many minded it this session, the last, and why not lately.
        private string SpaceStatus()
        {
            if (!_cfg.PersonalSpace)
                return "off ([World] PersonalSpace)";
            return (_cfg.SightReactions ? "on" : "on, but [Scenes] SightReactions is off, so nothing plays") + "; " + _spaceCount + " minded you standing so close this session" +
                   (_spaceLast != null ? "; last: " + _spaceLast : "") + (_spaceWhyNot != null ? "; the last look: " + _spaceWhyNot : "");
        }
    }
}
