// The wake in game (FIVEDAY item 27; Core/WakeNight.cs). Every fifteen seconds at night (eight to two) in a town: who is
// being sat up with tonight, the town's dead (WakeNight.Due) and enRAGE's funerals due tomorrow whose plot lies in it. Once
// a night a town, the box tells you; in West World, once a night, one of the waking near you swears the dead one moved. The
// street's quiet talks take it up in Life.cs (WakeTonightFirst). F7 "TEST wakes:". Behind [World] Wakes.
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
        private int _wakeNextLook, _wakeTalks, _wakeOdd, _wakeBoxed;
        private readonly HashSet<string> _wakeDone = new HashSet<string>();
        private string _wakeTonightTown;
        private List<WakeFor> _wakeTonight = new List<WakeFor>();

        /// Who's sat up with tonight in this town (found every fifteen seconds; the game side adds enRAGE's funerals).
        private List<WakeFor> WakesHere(string town, long g)
        {
            List<WakeFor> list = WakeNight.Due(_store, town, g);
            if (_bridge != null)
            {
                try
                {
                    long tomorrow = WakeNight.Tonight(g) + 1;
                    foreach (PlanInfo p in _bridge.Plans())
                        if (p != null && p.Kind == "funeral" && !p.Done && p.DueAt / 1440 == tomorrow && !string.IsNullOrEmpty(p.Dead) && EnrageWestworld.InGame.Places.TownAt(p.Pos) == town)
                            list.Add(new WakeFor { Id = p.Id, Name = p.Dead, Town = town });
                }
                catch (Exception ex)
                {
                    Log.Detail("WAKE enRAGE's funerals unread: " + ex.Message);
                }
            }
            return list;
        }

        /// For the side talk: the first name of one sat up with tonight here, or null.
        private string WakeTonightFirst()
        {
            return _cfg != null && _cfg.Wakes && _wakeTonightTown == _placeTown && _wakeTonight.Count > 0 ? Person.FirstOf(_wakeTonight[0].Name) : null;
        }

        private void WakeNightStep(int now)
        {
            if (_cfg == null || !_cfg.Wakes || _store == null || now < _wakeNextLook)
                return;
            _wakeNextLook = now + 15000;
            Safe("wakes", () =>
            {
                long g = Bridge.Now();
                int hour = (int)(g / 60 % 24);
                if (_placeTown == null || !WakeNight.Hour(hour))
                {
                    _wakeTonight.Clear();
                    _wakeTonightTown = null;
                    return;
                }
                _wakeTonight = WakesHere(_placeTown, g);
                _wakeTonightTown = _placeTown;
                if (_wakeTonight.Count == 0)
                    return;
                long night = WakeNight.Tonight(g);
                WakeFor w = _wakeTonight[0];
                if (_wakeDone.Count > 200)
                    _wakeDone.Clear();
                if (_wakeDone.Add("box|" + _placeTown + "|" + night))
                {
                    NewsPush("A wake", WakeNight.Box(_placeTown, w.Name, w.Id + "|" + night), "news");
                    _wakeBoxed++;
                    Log.Write("WAKE " + _placeTown + ": they sit up with " + w.Name + " tonight");
                    return;
                }
                if (_cfg.WestWorld && _wakeDone.Add("odd|" + _placeTown + "|" + night))
                {
                    Ped me = Game.Player.Character;
                    foreach (int h in _mindNear)
                    {
                        MindWho m;
                        if (!_mindWho.TryGetValue(h, out m) || m.Id == null || _engine.StageOf(m.Id) < 1 || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position) > 30f)
                            continue;
                        _ticker.Push((m.Name ?? "Someone") + ", thinking", WakeNight.OddThought(Person.FirstOf(w.Name), m.Id + "|" + night), "near", h);
                        _wakeOdd++;
                        Log.Write("WAKE " + m.Name + " swears " + w.Name + " moved (West World)");
                        return;
                    }
                    _wakeDone.Remove("odd|" + _placeTown + "|" + night);   // nobody waking near yet: try again
                }
            });
        }

        private string WakesStatus()
        {
            if (_cfg == null || !_cfg.Wakes)
                return "off ([World] Wakes)";
            return _wakeBoxed + " wakes told, " + _wakeTalks + " talks of the dead, " + _wakeOdd + " who swore the dead moved, this session" +
                   (_wakeTonight.Count > 0 ? "; tonight in " + _wakeTonightTown + ": " + string.Join(", ", _wakeTonight.Select(x => x.Name)) : "");
        }
    }
}
