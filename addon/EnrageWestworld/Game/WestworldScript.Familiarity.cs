// Familiarity, the game side (DEPTH item 17; Core/Familiarity.cs; [Mind] Familiarity). A look every 20 s (the frame pass's scheduler;
// Tick04's own call with LookScheduler off), in its own guard: every two people of this town near you (the mind's own near scan, nobody
// made), within 6 m of each other and with no tie at all, are a sighting; the third sighting in a game day, an hour or more after the
// one before, is a tie at warmth 0, kind "acquainted", with nothing else new saved. From then on Ties does what it does with any tie
// (the nods in passing), and in a brawl SideTieOf gives one a small lean to the other. Runner: the ties and the nods (seen), the brawl
// sides (built). Out of a town, nobody.
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Core.Familiarity _familiar = new Core.Familiarity();
        private Action _aFamiliar;
        private int _famNextLook, _famSightings, _famMade;
        private string _famLast;

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
            int n = _mindNear.Count;
            for (int i = 0; i < n; i++)
            {
                MindWho a;
                int ha = _mindNear[i];
                if (!_mindWho.TryGetValue(ha, out a) || a.Id == null || a.P == null || a.P.Home != _placeTown || !ENTITY.DOES_ENTITY_EXIST(ha))
                    continue;
                Vector3 pa = ENTITY.GET_ENTITY_COORDS(ha, true, true);
                for (int j = i + 1; j < n; j++)
                {
                    MindWho b;
                    int hb = _mindNear[j];
                    if (!_mindWho.TryGetValue(hb, out b) || b.Id == null || b.P == null || b.P.Home != _placeTown || b.Id == a.Id || !ENTITY.DOES_ENTITY_EXIST(hb))
                        continue;
                    if (ENTITY.GET_ENTITY_COORDS(hb, true, true).DistanceTo(pa) > Core.Familiarity.NearMetres)
                        continue;
                    if (_store.Ties.Any(t => t != null && t.Has(a.Id) && t.Has(b.Id)))
                        continue;   // they have a tie already: whatever it is, it speaks for them
                    _famSightings++;
                    if (!_familiar.Note(a.Id, b.Id, g))
                        continue;
                    Ties.Note(_store.Ties, a.Id, a.Name, b.Id, b.Name, _placeTown, Core.Familiarity.Kind, g);
                    _famMade++;
                    _famLast = a.Name + " and " + b.Name;
                    Log.Write("FAMILIAR " + a.Name + " and " + b.Name + " have been about the same place three times today: acquainted now, in " + _placeTown);
                }
            }
        }

        private string FamiliarityStatus()
        {
            if (!Core.Familiarity.On)
                return "off ([Mind] Familiarity)";
            return _famSightings + " sightings, " + _familiar.Pairs + " pairs seen today, " + _famMade + " acquainted this session" + (_famLast != null ? " (the last: " + _famLast + ")" : "");
        }
    }
}
