// Places hold memories, the game side (THREEHOUR item 5; Core/PlacesMourned.cs). Every 2 s in a town: the town's remembered death
// places within 45 m of you (the list made afresh each day and on coming into a town), and the named people the mind has near you
// (_mindNear). One of them within 15 m of a spot, close to the one who died there, free and ours to direct, stops: turns to the
// spot, or kneels there if they were dearest, with a thought over their head. One a day each for each dead; one in the
// town every 30 s at most. Behind [Mind] PlacesMourned. Log "PLACES".
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _placesNextLook, _placesNextAny, _placesStops;
        private List<DeathEntry> _placesHere = new List<DeathEntry>();
        private string _placesTown;
        private long _placesDay = -1;
        private readonly HashSet<string> _placesDone = new HashSet<string>();   // "living|dead|day"

        private void PlacesMournedStep(int now)
        {
            if (_cfg == null || !_cfg.PlacesMourned || _placeTown == null || _store == null || _engine == null || now < _placesNextLook)
                return;
            _placesNextLook = now + PlacesMourned.LookMs;
            if (now < _placesNextAny)
                return;
            Safe("places remember", () => PlacesMournedLook(now));
        }

        private void PlacesMournedLook(int now)
        {
            long g = _store.LastGameMin;
            if (_placesTown != _placeTown || _placesDay != g / 1440)
            {
                _placesHere = PlacesMourned.Places(_store.Deaths, _placeTown, g);
                _placesTown = _placeTown;
                _placesDay = g / 1440;
            }
            if (_placesHere.Count == 0 || _mindNear.Count == 0)
                return;
            Vector3 mp = Game.Player.Character.Position;
            foreach (DeathEntry d in _placesHere)
            {
                Vector3 spot = new Vector3(d.X, d.Y, d.Z);
                if (spot.DistanceTo(mp) > PlacesMourned.LookOutMetres)
                    continue;
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.Id == d.Id || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    Ped p = new Ped(h);
                    if (p.Position.DistanceTo(spot) > PlacesMourned.NearMetres)
                        continue;
                    SceneTie t = PlacesMourned.TieWith(_store.Ties, w.Id, d.Id);
                    if (t == null)
                        continue;
                    string key = PlacesMourned.DayKey(w.Id, d.Id, g);
                    if (_placesDone.Contains(key) || NotFree(h) || MayDirectWhyNot(p) != null || _director.Involves(h) || _staged.Contains(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_COMBAT(h, 0))
                        continue;
                    _placesDone.Add(key);
                    _placesNextAny = now + PlacesMourned.GapMs;
                    _placesStops++;
                    bool kneel = PlacesMourned.Kneels(t);
                    TASK.TASK_TURN_PED_TO_FACE_COORD(h, spot.X, spot.Y, spot.Z, 2500);
                    if (kneel)
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(Kneel), PlacesMourned.KneelMs, true, 0u, -1f, false);
                    _ticker.Push(Person.FirstOf(w.Name) + ", thinking", PlacesMourned.Thought(Person.FirstOf(d.Name), kneel, key), "near", h);
                    Log.Write("PLACES " + w.Name + " stops where " + d.Name + " died, " + (int)p.Position.DistanceTo(spot) + " m off it (" + (kneel ? "kneels" : "looks") + ")");
                    return;
                }
            }
        }

        private string PlacesMournedStatus()
        {
            return _placesHere.Count + " places remembered in " + (_placesTown ?? "no town") + ", " + _placesStops + " stops this session";
        }
    }
}
