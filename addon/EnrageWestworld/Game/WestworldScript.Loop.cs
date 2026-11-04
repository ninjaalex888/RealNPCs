// The voice and the loop, in the game (SIXDAY item 12a; Core/LoopVoice.cs, Core/Engine.Loop.cs). Every two seconds, the waking
// residents bound within 30 m hear their habit's order for the hour on the ticker, once a day each ("Seven o'clock. The saloon.
// You'll be at the saloon."); from the third stage an answer follows a breath later; at the fourth, one loop a town a day is
// broken by a wake act where the habit forbids (a broom on an empty street at night, the church wall at noon, the river's
// edge: WestworldScript.WakeActs.cs). F7 "TEST loop:" lists each near waking resident's routine and whether they're keeping it.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class VoiceAnswer
        {
            public int Handle, Due;
            public string Name, Text;
        }

        private int _loopNext, _loopsBroken, _resetsSeen;
        private Spot _loopGoal;   // the spot a loop-breaking act walks to (ChooseWakeAct -> BeginAct)
        private readonly List<VoiceAnswer> _voiceAnswers = new List<VoiceAnswer>();
        private Action _aLoop;

        private void LoopStep(int now)
        {
            if (_aLoop == null)
                _aLoop = () => LoopTick(_now);
            Safe("loop voice", _aLoop);
        }

        private void LoopTick(int now)
        {
            if (_cfg == null || !_cfg.WestWorld || !_cfg.LoopVoice || _engine == null || _store == null)
                return;
            for (int i = _voiceAnswers.Count - 1; i >= 0; i--)
            {
                VoiceAnswer a = _voiceAnswers[i];
                if (now < a.Due)
                    continue;
                _voiceAnswers.RemoveAt(i);
                if (ENTITY.DOES_ENTITY_EXIST(a.Handle) && !ENTITY.IS_ENTITY_DEAD(a.Handle))
                    _ticker.Push(a.Name + ", thinking", a.Text, "near", a.Handle);
            }
            if (now < _loopNext)
                return;
            _loopNext = now + 2000;
            if (MISC.GET_MISSION_FLAG() || _resBind.Count == 0)
                return;
            Ped me = Game.Player.Character;
            long g = Bridge.Now(), day = g / 1440;
            int hour = CLOCK.GET_CLOCK_HOURS(), weekday = InnerLife.Weekday;
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                Resident r = kv.Value.R;
                if (r == null || r.Dead || !r.Regular || _engine.HeardToday(r.Id, day))
                    continue;
                PersonState st = _store.Find(r.Id);
                int stage = st != null ? st.WakeStage : 0;
                if (!LoopVoice.Hears(stage))
                    continue;
                int h = kv.Key;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || new Ped(h).Position.DistanceTo(me.Position) > LoopVoice.HearMetres)
                    continue;
                string order = _engine.Hear(r, stage, hour, weekday, day);
                if (order == null)
                    continue;
                string first = Person.FirstOf(r.Name);
                _ticker.Push(first + ", hearing", order, "near", h);
                if (LoopVoice.Answers(stage))
                    _voiceAnswers.Add(new VoiceAnswer { Handle = h, Name = first, Text = LoopVoice.Answer(stage, r.Id + "|" + day), Due = now + LoopVoice.AnswerDelayMs });
                break;   // one voice a tick: the ticker is a narrow thing
            }
        }

        /// The loop-breaking act for a stage-four resident, when the hour and the ground allow and the town's one a day isn't spent:
        /// the kind, with the spot to walk to for the wall and the water.
        private bool LoopBreakKind(Resident res, Ped ped, out string kind, out Spot goal)
        {
            kind = null;
            goal = null;
            if (_cfg == null || !_cfg.LoopVoice || res == null || !res.Regular || res.Town == null)
                return false;
            long day = Bridge.Now() / 1440;
            if (!_engine.LoopBreakDue(res.Town, day))
                return false;
            int hour = CLOCK.GET_CLOCK_HOURS(), weekday = InnerLife.Weekday;
            uint it = Natives.InteriorType(ped.Handle);
            string here = Routines.SpotOf(it != 0 ? InteriorKind(it) : null);
            if (Routines.Fit(Routines.ProfileOf(res, Voice.NowYear), here, hour, weekday) >= Routines.Odd)
                return false;   // the habit allows this spot at this hour: nothing to break
            string k = LoopVoice.BreakFor(hour);
            Vector3 pos = ped.Position;
            switch (k)
            {
                case "broom":
                    if (it != 0)
                        return false;   // the empty street, not a room
                    break;
                case "wall":
                    goal = Spots.Nearest(_spots, res.Town, "churchsteps", pos.X, pos.Y);
                    if (goal == null || Spots.Distance(goal, pos.X, pos.Y) > 60f)
                        return false;
                    break;
                default:
                    goal = Spots.Nearest(_spots, res.Town, "wharf", pos.X, pos.Y);
                    if (goal == null || Spots.Distance(goal, pos.X, pos.Y) > 60f)
                        return false;
                    break;
            }
            kind = k;
            return true;
        }

        /// A loop broken: the town's one a day spent, the log.
        private void LoopBroke(WakeAct act)
        {
            _loopsBroken++;
            _engine.LoopBroken(act.Res != null ? act.Res.Town : _placeTown, Bridge.Now() / 1440, act.Name, act.Kind, CLOCK.GET_CLOCK_HOURS());
        }

        private string LoopStatus()
        {
            if (_cfg == null || !_cfg.WestWorld)
                return "West World is off";
            if (!_cfg.LoopVoice)
                return "off ([WestWorld] LoopVoice)";
            Ped me = Game.Player.Character;
            long day = Bridge.Now() / 1440;
            int hour = CLOCK.GET_CLOCK_HOURS(), weekday = InnerLife.Weekday;
            var parts = new List<string>();
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                Resident r = kv.Value.R;
                if (r == null || r.Dead || !r.Regular || !ENTITY.DOES_ENTITY_EXIST(kv.Key) || new Ped(kv.Key).Position.DistanceTo(me.Position) > 40f)
                    continue;
                PersonState st = _store.Find(r.Id);
                int stage = st != null ? st.WakeStage : 0;
                if (stage <= 0)
                    continue;
                uint it = Natives.InteriorType(kv.Key);
                string here = Routines.SpotOf(it != 0 ? InteriorKind(it) : null);
                Routines.Profile prof = Routines.ProfileOf(r, Voice.NowYear);
                string expected = Routines.Expected(prof, hour, weekday) ?? "nowhere in particular";
                bool keeping = Routines.Fit(prof, here, hour, weekday) >= Routines.Odd;
                parts.Add(r.Name + " (stage " + stage + "): the habit says " + expected + ", at " + here + (keeping ? ", keeping it" : ", astray") + (_engine.HeardToday(r.Id, day) ? ", heard the voice today" : "") + (st != null && st.Has("centre") ? ", past the centre" : st != null && st.Has("centre1") ? ", nearing the centre" : "") + (st != null && st.Has("reset") ? ", reset once" : ""));
                if (parts.Count >= 6)
                    break;
            }
            return (parts.Count == 0 ? "no waking regular within 40 m" : string.Join("; ", parts)) + "; " + _engine.LoopEngineStatus() + "; this session: " + _loopsBroken + " loops broken, " + _resetsSeen + " resets seen";
        }
    }
}
