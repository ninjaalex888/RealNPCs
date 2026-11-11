// The proof tour, the game's half (DEPTH item 2, 2026-11-10; [Testing] ProofTour, ProofTourRisky, ProofTourGapSeconds).
// Core/ProofTour.cs decides the order, the verdicts and the walk; this file asks it once a tick (ProofTourStep from Tick04,
// after TestingStep and before the mission's return, so a mission pauses it), fires the row it hands over through the row's
// own start path (TestFire, as a key or the panel would), and writes its PROOF lines to the log. Runner: the Testing rows
// themselves (the J menus and the Director's scenes among them, seen in game). Nothing per frame beyond a few cheap checks
// while a tour walks; nothing at all when none does.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private ProofTour.Walk _tour;
        private Action _aProofTour;

        /// The row "prooftour": a new tour, or the paused one resumed, or (pressed while it walks) the walk stopped.
        private string ProofTourRow(int now, out string said)
        {
            said = null;
            if (!_cfg.ProofTour)
                return "the proof tour is off ([Testing] ProofTour)";
            if (_tour != null && !_tour.Over)
            {
                if (_tour.PausedFor != null)
                {
                    _tour.Resume(now);
                    said = "The proof tour goes on (row " + (_tour.At + 1) + " of " + _tour.Rows.Count + ").";
                }
                else
                {
                    _tour.Finish(true);
                    said = "The proof tour stopped. The row again starts a new one.";
                }
                ProofTourFlush();
                return null;
            }
            _tour = new ProofTour.Walk(TestActions.All, _cfg.ProofTourRisky, _cfg.ProofTourGapSeconds * 1000);
            Log.Write("PROOF tour: " + _tour.Rows.Count + " rows, one about every " + _cfg.ProofTourGapSeconds + " s" + (_cfg.ProofTourRisky ? ", the risky ones too" : ", risky ones passed over ([Testing] ProofTourRisky)"));
            said = "The proof tour: " + _tour.Rows.Count + " rows, one about every " + _cfg.ProofTourGapSeconds + " s. Stay put; the log says what ran. The row again stops it.";
            _tour.LastAt = now;   // the first row a gap after this one, so the panel has closed and the hint been read
            return null;
        }

        private void ProofTourStep(int now)
        {
            if (_tour == null || _tour.Over)
                return;
            if (_aProofTour == null)
                _aProofTour = () => ProofTourTick(_now);
            Safe("proof tour", _aProofTour);
        }

        private void ProofTourTick(int now)
        {
            TestActions.Entry e = _tour.Next(now, ProofTourStop(), ProofTourBusy());
            if (e != null)
            {
                string result;
                try
                {
                    result = TestFire(e.Id, "the proof tour", now);
                }
                catch (Exception ex)
                {
                    result = "failed: " + ex.GetType().Name + ": " + ex.Message;   // one row's fault is that row's, never the tour's
                    Log.Write("TEST fire " + e.Id + ": failed under the proof tour: " + ex);
                }
                _tour.Done(e.Id, result);
            }
            ProofTourFlush();
        }

        /// Why the tour can't go on now, else null: a fight, a mission, a menu, the saddle (DEPTH item 2).
        private string ProofTourStop()
        {
            if (MISC.GET_MISSION_FLAG())
                return "a story mission";
            Ped me = Game.Player.Character;
            if (me.IsDead)
                return "you're dead";
            if (PED.IS_PED_IN_COMBAT(me.Handle, 0))
                return "you're fighting";
            if (PanelOpen || _reader.Open || HUD.IS_PAUSE_MENU_ACTIVE())
                return "a menu is open";
            if (PED.IS_PED_ON_MOUNT(me.Handle))
                return "you're in the saddle";
            bool inHand = PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
            if (!inHand)
                return "you have no control just now";   // a cutscene: paused, not a row lost to it (review 1)
            return null;
        }

        /// Whether anything we fired is still under way: a scene of the Director's, an encounter or one asked for.
        private bool ProofTourBusy()
        {
            return _director.AnyPlaying || _encounterRequested != null || _execRequested || EncountersLive > 0 ||
                   WalkUpGoing() || _collection != null || _watch != null || _word != null ||   // a walk-up still on its way (review 1)
                   _eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil);   // a J prompt or menu still up
        }

        /// F7's line: off, none yet, or where the walk is and what came of it so far.
        private string ProofTourStatus()
        {
            if (!_cfg.ProofTour)
                return "off ([Testing] ProofTour)";
            if (_tour == null)
                return "none walked this session (the Testing row \"The proof tour\" starts one; risky rows " + (_cfg.ProofTourRisky ? "too" : "passed over") + ", one about every " + _cfg.ProofTourGapSeconds + " s)";
            return (_tour.Over ? (_tour.Stopped ? "stopped" : "walked") : _tour.PausedFor != null ? "paused (" + _tour.PausedFor + ")" : _tour.Waiting != null ? "firing " + _tour.Waiting : "walking") +
                   " at row " + Math.Min(_tour.At + 1, _tour.Rows.Count) + " of " + _tour.Rows.Count + ": ran " + _tour.Ran + ", skipped " + _tour.Skipped + ", failed " + _tour.Failed + ", risky passed over " + _tour.Passed;
        }

        private void ProofTourFlush()
        {
            if (!_tour.HasLines)
                return;   // nothing new: no list made this frame (review 1)
            foreach (string line in _tour.Take())
                Log.Write(line);
        }
    }
}
