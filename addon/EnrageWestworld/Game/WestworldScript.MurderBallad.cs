// Murder ballads at a fire, in game (FIVEDAY reserve R20; Core/MurderBallad.cs). Every fifteen seconds at night out of the
// towns: sitting still by a campfire (AtFireNow, the camp revenant's test), once a game night, a ballad from the next fire
// comes on the ticker a verse every eight seconds: your most-told killing (Shows.MostTold, a kill or a murder) if one has
// been told of, else the newest death the add-on knows of a named person, with its place and its hand. Nothing is spawned
// or directed: it's heard from the dark. Behind [Scenes] MurderBallads.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private List<string> _balladLines;
        private int _balladAt, _balladNextLook, _ballads;
        private long _balladNight = -1;

        private void BalladStep(int now)
        {
            if (_balladLines != null)
            {
                if (now >= _balladAt)
                {
                    _ticker.Push("From the next fire", _balladLines[0], "near", 0);
                    _balladLines.RemoveAt(0);
                    _balladAt = now + MurderBallad.VerseMs;
                    if (_balladLines.Count == 0)
                        _balladLines = null;
                }
                return;
            }
            if (_cfg == null || !_cfg.MurderBallads || _store == null || now < _balladNextLook)
                return;
            _balladNextLook = now + MurderBallad.CheckMs;
            long night = (Bridge.Now() + 6 * 60) / 1440;   // the night counts from 6 pm (a fire after midnight is the same night)
            if (night == _balladNight)
                return;
            Safe("ballad", () =>
            {
                if (!AtFireNow())
                    return;
                StartBallad(now, night);
            });
        }

        private string StartBallad(int now, long night)
        {
            List<string> lines = null;
            Deed d = Shows.MostTold(_store.Deeds, _store.TownRumours);
            if (d != null && (d.Kind == "kill" || d.Kind == "murder" || d.Kind == "lawkill"))
                lines = MurderBallad.OfYours(d.Detail, d.Town ?? d.Zone, d.Id);
            if (lines == null)
            {
                DeathEntry dead = _store.Deaths.Where(x => x != null && x.Name != null && x.Town != null).OrderByDescending(x => x.At).FirstOrDefault();
                if (dead != null)
                {
                    string by = dead.By == "player" ? "the stranger" : dead.ById != null ? (_store.FindResident(dead.ById) != null ? _store.FindResident(dead.ById).Name : null) : null;
                    lines = MurderBallad.OfDeath(Person.FirstOf(dead.Name), dead.Town, by, dead.Id ?? "x");
                }
            }
            if (lines == null)
                return "nothing to sing of: no killing told of, no death known";
            _balladLines = lines;
            _balladAt = now;
            _balladNight = night;
            _ballads++;
            Log.Write("BALLAD from the next fire: " + lines[0]);
            return null;
        }

        private string BalladStatus()
        {
            return _cfg == null || !_cfg.MurderBallads ? "off ([Scenes] MurderBallads)" : _ballads + " ballads heard this session";
        }
    }
}
