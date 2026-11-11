// The frame pass, the game's half (DEPTH item 4, 2026-11-10; [Mind] LookScheduler, on). Core/LookScheduler.cs runs the
// background looks below at most two a frame, the most overdue first; Tick04 calls each of them itself when the setting is off
// (exactly as before). Only looks that do nothing but notice or think on a timer are here: each one's whole body sits behind
// its own `now < _next` check, so the scheduler can space them but never run one more often. Left out on purpose: anything a
// player waits on (the J prompts, the walks, the jail door, the vouch, the storyteller), the ledger and the hand-back.
// Registration happens on the first step, never in the constructor (the constructor rule).
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly LookScheduler _looks = new LookScheduler();
        private bool _looksRegistered;
        private int _looksMinuteMost;   // the most looks one frame ran this minute (the PERF minute line)

        private void LooksStep(int now)
        {
            if (!_looksRegistered)
            {
                _looksRegistered = true;
            _looks.Add("territory", 2000, TerritoryStep);
            _looks.Add("tales", 5000, TaleStep);
            _looks.Add("criminals", CriminalsLookMs, CriminalsStep);
            _looks.Add("epilogue", EpilogueLookMs, EpilogueStep);
            _looks.Add("places remember", PlaceMemoryLookMs, PlaceMemoryStep);
            _looks.Add("notice board", 5000, NoticeBoardStep);
            _looks.Add("regard", RegardEveryMs, RegardStep);
            _looks.Add("fence cutters", 10000, FenceStep);
            _looks.Add("posse", 30000, PosseStep);
            _looks.Add("hobo signs", 10000, HoboStep);
            _looks.Add("relics", 10000, RelicStep);
            _looks.Add("editor", 40000, EditorStep);
            _looks.Add("posters", 5000, PosterStep);
            _looks.Add("dime novel", 30000, NovelStep);
            _looks.Add("going dry", 30000, DryStep);
            _looks.Add("letters home", 20000, LettersHomeStep);
            _looks.Add("roundup", 30000, RoundupStep);
            _looks.Add("town rivals", 60000, TownRivalStep);
            _looks.Add("cold snaps", 30000, ColdSnapStep);
            _looks.Add("grinding", 30000, GrindStep);
            _looks.Add("fever", 20000, FeverStep);
            _looks.Add("keepers back", KeeperBack.LookMs, KeeperBackStep);
            _looks.Add("places mourned", PlacesMourned.LookMs, PlacesMournedStep);
            _looks.Add("payday night", 10000, PaydayNightStep);
            _looks.Add("wake night", 15000, WakeNightStep);
            _looks.Add("sunday", 10000, SundayStep);
            _looks.Add("lasting marks", 3000, MarksStep);
            _looks.Add("survivors", 20000, SurvivorsStep);
            _looks.Add("calendar", 30000, CalendarStep);
            _looks.Add("step between", 500, StepBetweenStep);
            _looks.Add("brands", 500, BrandsStep);   // DEPTH item 8
            _looks.Add("send word", 1000, SendWordStep);   // DEPTH item 10
            _looks.Add("familiarity", 20000, FamiliarityStep);   // DEPTH item 17
            _looks.Add("note meet", 2000, NoteMeetStep);   // DEPTH reserve R2
            _looks.Add("spread a rumour", 2000, SlanderStep);   // DEPTH reserve R3   // J to step between two about to quarrel (WestworldScript.StepBetween.cs; DEPTH item 7: a new look registers here)
            }
            int ran = _looks.Step(now);
            if (ran > _looksMinuteMost)
                _looksMinuteMost = ran;
        }

        /// After a load: the looks' first turns spread again, so they don't all come due on the frame after.
        private void LooksRestart()
        {
            _looks.Restart();
        }

        private string LooksStatus()
        {
            if (_cfg == null || !_cfg.LookScheduler)
                return "off ([Mind] LookScheduler): each look on its own timer, every frame";
            return _looksRegistered ? _looks.Status() : "not started yet";
        }
    }
}
