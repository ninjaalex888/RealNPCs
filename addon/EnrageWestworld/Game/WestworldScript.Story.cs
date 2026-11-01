// The storyteller in the game (NIGHT4 item 2; Core/Storyteller.cs, [World] Storyteller): reads where the player is and
// what he's doing every two seconds, hears every notable happening near him (RecordEvent, and the walk-ups that never
// reach the WorldLog), and hands its multipliers to the timers that already decide when the street, the walk-ups and the
// encounters look again. It starts nothing and stops nothing; it never writes to _cfg (the ini and the panel would save
// the scaled value), never touches the hush, a scene's cast.Off, the specials' rest, the fighters' rest or the stalker.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private StoryMeter _story;
        private int _storyNextAt, _storyLoggedAt = int.MinValue, _storyPulled;
        private bool _storyNotingSide;   // a quiet talk's words are noted: never notable
        private Action _aStory;

        /// What the timers multiply by: as built while the storyteller is off or not made yet. A struct: no allocation.
        private StoryMults Story { get { return _story != null ? _story.Mults : StoryMults.Neutral; } }

        /// From Tick04 once the player has control: an int compare a frame, the work every two seconds.
        private void StoryStep(int now)
        {
            if (now < _storyNextAt)
                return;
            _storyNextAt = now + StoryMeter.UpdateMs;
            Safe("storyteller", _aStory ?? (_aStory = () => StoryUpdate(_now)));
        }

        /// From the mission pause: everything as built while a story mission runs, and the meter still.
        private void StoryHold(int now)
        {
            if (_story == null || now < _storyNextAt)
                return;
            _storyNextAt = now + StoryMeter.UpdateMs;
            StoryMults before = _story.Mults;
            if (_story.Update(now, new StoryState { Mission = true }))
                StoryBandChanged(now);
            if (!before.Same(_story.Mults))
                StoryApply();
        }

        private void StoryUpdate(int now)
        {
            StoryMode mode = StoryMeter.ParseMode(_cfg.Storyteller);
            if (_story == null)
            {
                _story = new StoryMeter(mode, now);
                Log.Write("STORY the storyteller is " + StoryMeter.ModeName(mode) + (mode == StoryMode.Off ? ": every wait as built" : ": quiet after " + StoryMeter.QuietAfterSeconds(mode) + " s with nothing worth stopping for near you"));
                StoryApply();
            }
            else if (_story.SetMode(mode, now))
            {
                Log.Write("STORY the storyteller is " + StoryMeter.ModeName(mode) + " now ([World] Storyteller): " + _story.Mults.Sped);
                _storyLoggedAt = now;
                StoryApply();
            }
            Ped me = Game.Player.Character;
            int h = me.Handle;
            var s = new StoryState
            {
                Town = _placeTown != null, Interior = _interiorType != 0, Saloon = _placeKind == "saloon", Panel = PanelOpen,
                Wanted = PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0, Combat = PED.IS_PED_IN_COMBAT(h, 0),
                RidingFast = PED.IS_PED_ON_MOUNT(h) && ENTITY.GET_ENTITY_SPEED(h) > 6f
            };
            if (!s.Town && _story.On)
            {
                Vector3 p = me.Position;
                s.Road = PATHFIND.IS_POINT_ON_ROAD(p.X, p.Y, p.Z, 0);
            }
            StoryMults before = _story.Mults;
            if (_story.Update(now, s))
                StoryBandChanged(now);
            if (!before.Same(_story.Mults))
                StoryApply();
        }

        /// The Pacing scales follow the multipliers (Pacing reads them on every look; set here, never per call).
        private void StoryApply()
        {
            StoryMults m = Story;
            Pacing.StoryChance = m.PaceChance;
            Pacing.StoryRest = m.PaceRest;
        }

        /// A band change: a STORY line (one a minute in the log; more often at Detail), and when the street went quiet, its
        /// deadlines pulled in once (they're absolute, written at a start, so a shorter gap would only land at the next one).
        private void StoryBandChanged(int now)
        {
            StoryBand band = _story.Band;
            if (band == StoryBand.Quiet && _cfg.Drama)
            {
                int soon = now + 8000;   // the street's own retry while due
                bool pulled = false;
                if (_nextDrama > soon) { _nextDrama = soon; pulled = true; }
                if (_nextChance > soon) { _nextChance = soon; pulled = true; }
                if (_nextSide > soon) { _nextSide = soon; pulled = true; }
                if (pulled)
                    _storyPulled++;
            }
            string line = "STORY " + StoryMeter.BandName(band) + " (" + StoryMeter.ModeName(_story.Mode) + ", was " + StoryMeter.BandName(_story.Was) + "): " +
                          (band == StoryBand.Held ? _story.HeldWhy + ", every wait as built"
                           : band == StoryBand.Quiet ? "nothing worth stopping for near you in " + _story.QuietSeconds(now) + " s; sooner: " + _story.Mults.Sped
                           : band == StoryBand.Busy ? "tension " + _story.Tension.ToString("0.00") + (_story.BreatherSeconds(now) > 0 ? ", a breather of " + _story.BreatherSeconds(now) + " s after " + _story.Last(now) : "") + "; a little later: " + _story.Mults.Sped
                           : "as the mode has it: " + _story.Mults.Sped);
            if (now - _storyLoggedAt >= 60000 || _storyLoggedAt == int.MinValue)
            {
                Log.Write(line);
                _storyLoggedAt = now;
            }
            else
                Log.Detail(line);
        }

        /// Something happened `metres` from the player (a Happening, or a walk-up): the meter weighs it.
        private void StoryNoticed(string kind, int weight, float metres)
        {
            if (_story != null && weight > 0)
                _story.Noticed(kind, weight, _now, metres);
        }

        /// For F7: TEST story.
        private string StoryStatus()
        {
            if (_story == null)
                return "not started yet ([World] Storyteller " + (_cfg != null ? _cfg.Storyteller : "?") + ")";
            int now = _now;
            StoryMults m = _story.Mults;
            string last = _story.Last(now);
            return StoryMeter.ModeName(_story.Mode) + ", " + StoryMeter.BandName(_story.Band) + (_story.HeldWhy != null ? " (" + _story.HeldWhy + ")" : "") + " in " + _story.Where +
                   "; tension " + _story.TensionAt(now).ToString("0.00") + "; nothing notable near you for " + _story.QuietSeconds(now) + " s (quiet after " + StoryMeter.QuietAfterSeconds(_story.Mode) + ")" +
                   "; last: " + (last ?? "none yet") + (_story.BreatherSeconds(now) > 0 ? "; breather " + _story.BreatherSeconds(now) + " s more" : "") +
                   "; noted " + _story.Noted + " (" + _story.Ignored + " too far off), bands changed " + _story.BandChanges + ", the street pulled in " + _storyPulled +
                   "; now: " + m.Sped + (m.EncounterEvery != 1f ? " (encounter rolls every " + (_cfg.EncounterEveryMinutes * m.EncounterEvery).ToString("0.0") + " min)" : "") +
                   (m.Drama != 1f ? " (scene gaps about " + (int)(_cfg.DramaGapSeconds * m.Drama) + " s)" : "");
        }
    }
}
