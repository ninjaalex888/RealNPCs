// What's really out where you are, in one place, for everything that thinks or talks about it (Lex, 2026-11-09, from a player; Core/WeatherTruth.cs says what it's for). [Ticker] WeatherChecked, WeatherTalk.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly SkyWatch _skyWatch = new SkyWatch();
        private static uint _skyType;   // the weather type showing; 0 with [Ticker] WeatherChecked off (the game's "previous", as it was)
        private uint _skyLogged;
        private int _skyTurns, _skyTestStep;

        /// The weather type showing now, for what asks for the game's own type.
        private static uint SkySeen()
        {
            return _skyType != 0 ? _skyType : MISC._GET_PREV_WEATHER_TYPE_HASH_NAME();
        }

        /// Every second and a half (Places). One read of the game's weather and one of the temperature.
        private void WeatherStep(int now, Vector3 pos)
        {
            uint prev = MISC._GET_PREV_WEATHER_TYPE_HASH_NAME();
            if (_cfg == null || !_cfg.WeatherChecked)
            {
                _skyType = 0;
                WeatherTruth.Now = null;
                _engine.NowSky = null;
                _engine.NowWeather = InnerLife.WeatherOf(prev);
                return;
            }
            uint next = MISC._GET_NEXT_WEATHER_TYPE_HASH_NAME();
            float mix = World.WeatherTransition;
            uint seen = WeatherTruth.Seen(prev, next, mix);
            SkyNow s = new SkyNow { Type = seen, Kind = InnerLife.WeatherOf(seen), Temp = WeatherTruth.Sane(MISC._GET_TEMPERATURE_AT_COORDS(pos)), Hour = CLOCK.GET_CLOCK_HOURS() };
            if (_cfg.WeatherTalk)
            {
                string fresh = _skyWatch.Step(s.Kind, now);
                s.Turned = _skyWatch.Turned;
                if (fresh != null)
                {
                    _skyTurns++;
                    Log.Write("WEATHER turned: " + fresh + " (" + WeatherTruth.Describe(s) + ")");
                }
            }
            if (RainShelter.On && _placeTown != null && _store != null)
                _engine.NoteSky(_placeTown, s.Wet, _store.LastGameMin);   // the town's record of the rain (FOURTEENHOUR item 26)
            _skyType = seen;
            WeatherTruth.Now = s;
            _engine.NowSky = s;
            _engine.NowWeather = s.Kind;
            if (seen != _skyLogged)
            {
                _skyLogged = seen;
                Log.Write("WEATHER now: " + WeatherTruth.Describe(s) + " (showing " + seen + "; game's previous " + prev + ", next " + next + ", blend " + mix.ToString("0.00") + ")");
            }
        }

        /// The Testing row: what's read here; and with WeatherTalk, each press has people take it as the next kind of change.
        private string WeatherTest(int now, out string said)
        {
            said = null;
            if (!_cfg.WeatherChecked)
                return "[Ticker] WeatherChecked is off";
            string read = WeatherTruth.Describe(WeatherTruth.Now);
            if (!_cfg.WeatherTalk)
            {
                said = "the weather here: " + read;
                return null;
            }
            string to = SkyWatch.TurnNames[_skyTestStep++ % SkyWatch.TurnNames.Length];
            _skyWatch.Force(to, now);
            if (_cfg.ThoughtsAtOnce > 1 && _cfg.ThoughtsOverHeads)
                _ticker.SidesNow();
            _nextTickerUpdate = 0;
            said = "the weather here: " + read + "; people near you now take it as: " + to + " (" + _skyTurns + " real turns this session)";
            Log.Write("WEATHER test: " + said);
            return null;
        }
    }
}
