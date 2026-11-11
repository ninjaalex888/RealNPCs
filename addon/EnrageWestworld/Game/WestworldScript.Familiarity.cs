// Familiarity, the game side (DEPTH item 17; Core/Familiarity.cs; [Mind] Familiarity). A look every 20 s (the frame pass's scheduler;
// Tick04's own call with LookScheduler off), in its own guard: every two people of this town near you (the mind's own near scan, nobody
// made), within 6 m of each other and with no tie at all, are a sighting; the third sighting in a game day, an hour or more after the
// one before, makes them acquainted (kept in memory by Core.Familiarity, never as a tie: review 2), and one of them thinks it. In a brawl
// SideTieOf gives one a small lean to the other. Runner: the thoughts (seen), the brawl sides (built). Out of a town, nobody.
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
        private readonly Core.Familiarity _familiar = new Core.Familiarity();
        private readonly List<int> _famHandles = new List<int>();
        private readonly List<Vector3> _famAt = new List<Vector3>();
        private readonly List<MindWho> _famWho = new List<MindWho>();
        private readonly HashSet<string> _famTied = new HashSet<string>();
        private Action _aFamiliar;
        private int _famNextLook, _famSightings, _famMade;
        private string _famLast;

        private static readonly string[] FamThinks = { "Him again. Every day about now, at the same spot.", "I know that face by now. Couldn't tell you his name.", "There's that fellow. We keep the same hours, he and I." };

        private void FamiliarityStep(int now)
        {
            if (!Core.Familiarity.On || now < _famNextLook || _store == null || _placeTown == null)
                return;
            _famNextLook = now + 20000;
            Safe("familiarity", _aFamiliar ?? (_aFamiliar = () => FamiliarityLook(_now)));
        }

        private void FamiliarityLook(int now)
        {
            long g = Bridge.Now();
            // the town's people near, each read once (review 2: the positions were read again for every pair)
            _famHandles.Clear(); _famAt.Clear(); _famWho.Clear();
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || w.P.Home != _placeTown || !ENTITY.DOES_ENTITY_EXIST(h))
                    continue;
                _famHandles.Add(h);
                _famAt.Add(ENTITY.GET_ENTITY_COORDS(h, true, true));
                _famWho.Add(w);
            }
            if (_famWho.Count < 2)
                return;
            _famTied.Clear();
            foreach (SceneTie t in _store.Ties)
                if (t != null && t.A != null && t.B != null)
                    _famTied.Add(string.CompareOrdinal(t.A, t.B) < 0 ? t.A + "|" + t.B : t.B + "|" + t.A);   // a tie speaks for them already
            for (int i = 0; i < _famWho.Count; i++)
                for (int j = i + 1; j < _famWho.Count; j++)
                {
                    MindWho a = _famWho[i], b = _famWho[j];
                    if (a.Id == b.Id || _famAt[i].DistanceTo(_famAt[j]) > Core.Familiarity.NearMetres)
                        continue;
                    if (_famTied.Contains(string.CompareOrdinal(a.Id, b.Id) < 0 ? a.Id + "|" + b.Id : b.Id + "|" + a.Id))
                        continue;
                    _famSightings++;
                    if (!_familiar.Note(a.Id, b.Id, g))
                        continue;
                    _famMade++;
                    _famLast = a.Name + " and " + b.Name;
                    _ticker.Push(Person.FirstOf(a.Name) + ", thinking", FamThinks[(Voice.Hash(a.Id + b.Id) & 0x7fffffff) % FamThinks.Length], "near", _famHandles[i]);
                    Log.Write("FAMILIAR " + a.Name + " and " + b.Name + " have been about the same place three times today: they know each other by sight now, in " + _placeTown);
                }
        }

        private string FamiliarityStatus()
        {
            if (!Core.Familiarity.On)
                return "off ([Mind] Familiarity)";
            return _famSightings + " sightings, " + _familiar.Pairs + " pairs seen today, " + _familiar.Known + " acquainted (in memory), " + _famMade + " this session" + (_famLast != null ? " (the last: " + _famLast + ")" : "");
        }
    }
}
