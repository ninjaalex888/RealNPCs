// Seeing what happened (SIXDAY item 7; roadmap 24; roadmap.md P4; Core/Overheard.cs): the "Overheard" page among the
// papers (what went on near you in the last game day, read from the world log when the reader opens, never kept), and the
// line on the ticker when something starts up at your back within 22 m ("Raised voices behind you."), at most once a minute.
// [Display] Overheard, [Ticker] SceneCues. Log "CUE" and "READER pages:"; F7 "TEST overheard:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _cueNextAt, _cues, _cuesSkipped, _overheardLines;
        private string _cueLast;

        /// A scene just started between others: a cue when it's behind the player, near, and a minute has passed since the last.
        private void SceneCue(Scene s, List<Ped> people, int now, bool test)
        {
            if (_cfg == null || !_cfg.SceneCues || s == null || test || people == null || people.Count == 0 || people[0] == null || !people[0].Exists())
                return;
            string cue = Overheard.Cue(s.Kind);
            if (cue == null)
                return;
            if (now < _cueNextAt)
            {
                _cuesSkipped++;
                return;
            }
            Ped me = Game.Player.Character;
            int h = people[0].Handle;
            float d = people[0].Position.DistanceTo(me.Position);
            if (d > Overheard.Metres || ENTITY.IS_ENTITY_ON_SCREEN(h))
                return;   // in view: he can see for himself
            _cueNextAt = now + Overheard.CueGapMs;
            _cues++;
            _cueLast = cue + " (" + s.Kind + ", " + (int)d + " m)";
            _ticker.Push("Behind you", cue, "news");
            Log.Detail("CUE " + _cueLast);
        }

        /// The Overheard page for the reader, or null when nothing was overheard this last day.
        private Paper OverheardPaper()
        {
            if (_cfg == null || !_cfg.Overheard || _store == null)
                return null;
            string body;
            try
            {
                body = Overheard.Body(_store.WorldLog, _store.LastGameMin);
                _overheardLines = body != null ? Overheard.Lines(_store.WorldLog, _store.LastGameMin).Count : 0;
            }
            catch (Exception ex)
            {
                Log.Write("OVERHEARD failed: " + ex.Message);
                return null;
            }
            if (body == null)
                return null;
            return new Paper { Kind = "note", Title = "Overheard", From = "this last day", Body = body, Delivered = true, Read = true };
        }

        private string OverheardStatus()
        {
            return (_cfg.Overheard ? "the page on (" + Overheard.Lines(_store.WorldLog, _store.LastGameMin).Count + " lines this last day)" : "the page off") + "; cues " +
                   (_cfg.SceneCues ? "on: " + _cues + " given, " + _cuesSkipped + " within the minute" + (_cueLast != null ? "; last: " + _cueLast : "") : "off");
        }
    }
}
