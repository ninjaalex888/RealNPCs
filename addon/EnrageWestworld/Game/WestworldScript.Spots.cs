// The spot table in the game (SIXDAY item 11a; Core/Spots.cs): spots.txt beside RealNPCs.ini is read at the start; the spot key
// ([Keys] SpotKey, K as it comes) records where the player stands under the current kind (Shift and the key cycles the kind,
// shown on the news line) and appends it to the file; indoors it refuses and says why. A station he comes out of gives its town
// a platform spot for the session when it has none. F7 "TEST spots:". The key builds the table and is safe to ignore.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private List<Spot> _spots = new List<Spot>();
        private string _spotKind = Spots.Kinds[0];
        private int _spotRequested;   // 1: record here, 2: the next kind (KeyDown04)
        private int _spotsRecorded;

        private string SpotsPath
        {
            get { return Path.Combine(_dir, "spots.txt"); }
        }

        private void SpotsLoad()
        {
            _spots = Spots.Load(SpotsPath, Log.Write);
        }

        /// The key pressed (KeyDown04): with Shift, the next kind; alone, this place under the kind.
        private void SpotsStep(int now)
        {
            int ask = _spotRequested;
            if (ask == 0)
                return;
            _spotRequested = 0;
            if (ask == 2)
            {
                _spotKind = Spots.NextKind(_spotKind);
                News("Spots", "Kind: " + _spotKind + " (the key records here under it).");
                return;
            }
            Ped me = Game.Player.Character;
            if (Natives.InteriorType(me.Handle) != 0)
            {
                News("Spots", "Indoors (an interior): no spot recorded. Step outside.");
                return;
            }
            Vector3 pos = me.Position;
            string town = _bridge.TownAt(pos) ?? Rumours.NearestTown(pos.X, pos.Y);
            if (town == null)
            {
                News("Spots", "Too far from any town to put a name to it: nothing recorded.");
                return;
            }
            var s = new Spot { Town = town, Kind = _spotKind, X = pos.X, Y = pos.Y, Z = pos.Z, Heading = ENTITY.GET_ENTITY_HEADING(me.Handle), Room = Spots.DefaultRoom };
            _spots.Add(s);
            _spotsRecorded++;
            bool written = Spots.Append(SpotsPath, s, Log.Write);
            int n = Spots.All(_spots, town, _spotKind).Count;
            News("Spots", town + ": " + _spotKind + " recorded" + (n > 1 ? " (" + n + " of that kind here)" : "") + (written ? "." : "; the file couldn't be written (the log says why)."));
            Log.Write("SPOTS recorded " + Spots.Format(s) + (written ? "" : " (not written)"));
        }

        /// A station he came out of: its door stands for the platform while the town has no spot of its own (never written).
        private void NoteStationDoor(string wasKind, Vector3 at)
        {
            if (wasKind != "station" || _placeTown == null || Spots.Nearest(_spots, _placeTown, "platform", at.X, at.Y) != null)
                return;
            _spots.Add(new Spot { Town = _placeTown, Kind = "platform", X = at.X, Y = at.Y, Z = at.Z, Heading = 0f, Room = 10f, Transient = true });
            Log.Write("SPOTS " + _placeTown + ": the station door stands for the platform this session (no platform spot recorded)");
        }

        private string SpotsStatus()
        {
            Vector3 pos = Game.Player.Character.Position;
            return Spots.Status(_spots, pos.X, pos.Y) + "; kind under the key: " + _spotKind + "; " + _spotsRecorded + " recorded this session" + (_spots.Any(s => s.Transient) ? "; " + _spots.Count(s => s.Transient) + " from station doors (this session only)" : "");
        }
    }
}
