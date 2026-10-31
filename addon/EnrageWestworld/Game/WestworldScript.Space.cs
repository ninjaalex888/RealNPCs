// Personal space, the game side (Core/Space.cs has the reactions; NIGHT3). Every 500 ms standing still on foot: the one person standing
// right up against the player (under a metre, less in a saloon), both of them still; held for 1.8 s, they mind it through
// the street reactions' own player (SightPlay: the look, a gesture, a word where the feeling has one, the step back, the
// thought). Once a person in five minutes, 20 s between any two. Never the law, anyone busy (a scene, a talk, our errands,
// the interaction menu), anyone at a counter or in a scenario (a stool, the bar), or while he's riding, aiming, fighting
// or in a mission. [World] PersonalSpace, with [Scenes] SightReactions. Testing row "Someone minds you standing so close".
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
        private const int SpaceSampleMs = 500, SpaceGapMs = 20000;
        private int _spaceNext, _spaceWho, _spaceSince, _spaceGapUntil, _spaceCount;
        private readonly Dictionary<int, int> _spaceAgain = new Dictionary<int, int>();   // a handle -> when they may mind it again
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

        /// The one standing right up against him, held long enough, minds it. Null when it started (or is still being
        /// held), else why not.
        private string SpaceTick(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string why = SpaceCant(me, now, test);
            if (why != null)
            {
                _spaceWho = 0;
                return _spaceWhyNot = why;
            }
            float reach = test ? 3f : _placeKind == "saloon" ? Space.SaloonMetres : Space.Metres, best = reach;
            Ped near = null;
            int w = WatchEnter("personal space");   // the hang watch: a scan of the street
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead)
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d < best)
                {
                    near = p;
                    best = d;
                }
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
                    return null;
                }
                if (now - _spaceSince < Space.LingerMs || ENTITY.GET_ENTITY_SPEED(h) > 0.4f)
                    return null;
                int again;
                if (_spaceAgain.TryGetValue(h, out again) && now < again)
                    return null;
            }
            string cant = SightCant(near, me);
            if (cant == null && (PED.IS_PED_USING_ANY_SCENARIO(h) || NotFree(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_RAGDOLL(h)))
                cant = "busy, sitting or at the bar";
            if (cant != null)
            {
                _spaceAgain[h] = now + 30000;   // not them, not for a while: no asking every frame
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
                Look = Space.Look(temper, regard, woman, seed), Start = now, Step = 0
            };
            _sights.Add(run);
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

        /// Why nobody minds it now: riding, aiming, fighting, a mission, the menu open, a reaction playing, the gap.
        private string SpaceCant(Ped me, int now, bool test)
        {
            if (me == null || !me.Exists() || me.IsDead || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                return "you're riding";
            if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return "a mission, a fight or a weapon drawn";
            if (_menu != null || _sights.Count > 0)
                return "the Talk menu or another reaction";
            if (!test && now < _spaceGapUntil)
                return "20 s between";
            if (!test && ENTITY.GET_ENTITY_SPEED(me.Handle) > 0.4f)
                return "you're on the move";   // passing close is the game's own business (its bumps); only standing there is minded
            return null;
        }

        /// The Testing row: the nearest within 3 m minds it now (the second and more, the gap and the five minutes skipped).
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
