// The game's own pulsing event dot on the map where one of our happenings is going on near you (Lex, 2026-11-01: "the same
// sort of white blinking dot that the game uses for on the minimap indicating where a nearby NPC event is happening"):
// Rockstar's BLIP_STYLE_RANDOM_EVENT, at the spot, for as long as it lasts. [Encounters] EventDots. Ours alone are made and
// taken away; one a happening, by key.
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
        private readonly Dictionary<string, Blip> _eventDots = new Dictionary<string, Blip>();
        private bool _eventDotFailed;
        private const float EventDotNearMetres = 8f;

        /// The dot for this happening at this spot: made if it isn't there, moved if it is.
        private void EventDot(string key, Vector3 at)
        {
            if (_cfg == null || !_cfg.EventDots || _eventDotFailed)
            {
                EventDotClear(key);
                return;
            }
            if (Game.Player.Character.Position.DistanceTo(at) < EventDotNearMetres)
            {
                EventDotClear(key);   // you're there: the dot has done its work (Lex, 2026-11-01: "go away when the character gets within 5-10m")
                return;
            }
            try
            {
                Blip b;
                if (_eventDots.TryGetValue(key, out b) && b != null && b.Exists())
                {
                    MAP.SET_BLIP_COORDS(b.Handle, at.X, at.Y, at.Z);
                    return;
                }
                b = new Blip(MAP.BLIP_ADD_FOR_COORDS(Natives.Hash("BLIP_STYLE_RANDOM_EVENT"), at.X, at.Y, at.Z));
                _eventDots[key] = b;
            }
            catch (Exception ex)
            {
                _eventDotFailed = true;
                Log.Write("EVENT DOT failed, none for this session: " + ex.Message);
            }
        }

        private void EventDotClear(string key)
        {
            Blip b;
            if (!_eventDots.TryGetValue(key, out b))
                return;
            _eventDots.Remove(key);
            try
            {
                if (b != null && b.Exists())
                    b.Delete();
            }
            catch { }
        }

        private void EventDotsClearAll()
        {
            foreach (string k in new List<string>(_eventDots.Keys))
                EventDotClear(k);
        }
    }
}
