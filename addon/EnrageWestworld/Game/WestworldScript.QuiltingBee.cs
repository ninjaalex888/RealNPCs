// A quilting bee overheard (FIVEDAY reserve R8; Core/QuiltingBee.cs). Every second in a town on Tuesday and Thursday
// afternoons: on foot, still for six seconds, within 25 m of the town's "churchsteps" spot (the church hall), once a town a
// day, the bee's half minute plays on the ticker as "Through the window", a line every seven seconds: the opening, the
// town's ties of the last three days told as the women tell them (Ties.TownThought), their own talk between, and a word of
// you by the town's standing (Engine.StandingOf). Nothing is spawned or directed. Behind [Scenes] QuiltingBee.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private List<string> _quiltLines;
        private int _quiltAt, _quiltNextLook, _quiltStillSince, _quiltHeard;
        private readonly Dictionary<string, long> _quiltDay = new Dictionary<string, long>();   // town -> the game day it was heard
        private string _quiltWhyNot;

        private void QuiltStep(int now)
        {
            if (_quiltLines != null)
            {
                if (now >= _quiltAt)
                {
                    _ticker.Push("Through the window", _quiltLines[0], "near", 0);
                    _quiltLines.RemoveAt(0);
                    _quiltAt = now + QuiltingBee.LineMs;
                    if (_quiltLines.Count == 0)
                        _quiltLines = null;
                }
                return;
            }
            if (_cfg == null || !_cfg.QuiltingBee || _store == null || _placeTown == null || now < _quiltNextLook)
                return;
            _quiltNextLook = now + 1000;
            Safe("quilting bee", () => QuiltLook(now, false));
        }

        private string QuiltLook(int now, bool test)
        {
            long g = Bridge.Now(), day = g / 1440, done;
            if (!test && !QuiltingBee.Now(InnerLife.Weekday, (int)(g % 1440)))
                return _quiltWhyNot = "Tuesday or Thursday afternoons";
            if (!test && _quiltDay.TryGetValue(_placeTown, out done) && done == day)
                return _quiltWhyNot = "heard here today already";
            Ped me = Game.Player.Character;
            Spot steps = Spots.Nearest(_spots, _placeTown, "churchsteps", me.Position.X, me.Position.Y);
            if (steps == null)
                return _quiltWhyNot = "no churchsteps spot recorded in " + _placeTown + " (the spots tool)";
            if (new Vector3(steps.X, steps.Y, steps.Z).DistanceTo(me.Position) > QuiltingBee.NearMetres || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
            {
                _quiltStillSince = 0;
                return _quiltWhyNot = "on foot within 25 m of the church steps";
            }
            if (ENTITY.GET_ENTITY_SPEED(me.Handle) > 0.4f)
            {
                _quiltStillSince = 0;
                return _quiltWhyNot = "stand still a moment";
            }
            if (_quiltStillSince == 0)
                _quiltStillSince = now;
            if (!test && now - _quiltStillSince < QuiltingBee.StillMs)
                return _quiltWhyNot = "listening";
            var ties = new List<string>();
            foreach (SceneTie t in Ties.TalkOf(_store.Ties, _placeTown, null, g))
            {
                if (ties.Count >= QuiltingBee.MostTies)
                    break;
                if (g - t.At < 3 * 1440L)
                    ties.Add(Ties.TownThought(t, _placeTown, "quilt|" + day + "|" + ties.Count));
            }
            int kills, kind;
            _engine.StandingOf(_placeTown, g, out kills, out kind);
            _quiltLines = QuiltingBee.Lines(ties, kills, kind, "quilt|" + _placeTown + "|" + day);
            _quiltAt = now;
            _quiltDay[_placeTown] = day;
            _quiltHeard++;
            Log.Write("QUILT the bee overheard in " + _placeTown + ": " + ties.Count + " of the town's goings-on, and you (" + kills + " killings known, " + kind + " kindnesses)");
            return _quiltWhyNot = null;
        }

        private string QuiltStatus()
        {
            if (_cfg == null || !_cfg.QuiltingBee)
                return "off ([Scenes] QuiltingBee)";
            return _quiltHeard + " heard this session" + (_quiltWhyNot != null ? "; none now: " + _quiltWhyNot : "");
        }
    }
}
