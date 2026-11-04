// The voice and the loop in the engine (SIXDAY item 12a; Core/LoopVoice.cs): what a waking resident hears at the hour, once a
// day; one loop broken per town per day; the reset that closes the loop over someone and what their friends make of it.
// Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private readonly Dictionary<string, long> _voiceHeard = new Dictionary<string, long>();    // resident id -> the game day the voice spoke (not saved: a reload may repeat once)
        private readonly Dictionary<string, long> _loopBroken = new Dictionary<string, long>();   // town -> the game day a loop was broken there
        private int _voices, _loopBreaks, _resets;

        /// The order a waking resident hears at this hour, or null: not waking enough, no habit for the hour, or heard today already.
        public string Hear(Resident r, int stage, int hour, int weekday, long day)
        {
            if (!Cfg.WestWorld || !Cfg.LoopVoice || r == null || r.Id == null || !LoopVoice.Hears(stage))
                return null;
            long heard;
            if (_voiceHeard.TryGetValue(r.Id, out heard) && heard == day)
                return null;
            string spot = Routines.Expected(Routines.ProfileOf(r, Voice.NowYear), hour, weekday);
            string order = LoopVoice.Order(spot, hour, r.Id + "|" + day + "|" + hour);
            if (order == null)
                return null;
            _voiceHeard[r.Id] = day;
            if (_voiceHeard.Count > 400)
                _voiceHeard.Clear();
            _voices++;
            Log("VOICE " + r.Name + ": " + hour + ":00 -> " + spot + " (" + LoopVoice.Heard(stage) + ")");
            return order;
        }

        public bool HeardToday(string id, long day)
        {
            long heard;
            return id != null && _voiceHeard.TryGetValue(id, out heard) && heard == day;
        }

        /// Whether a loop may be broken in this town today (one a day), and the breaking taken.
        public bool LoopBreakDue(string town, long day)
        {
            long broke;
            return Cfg.WestWorld && Cfg.LoopVoice && town != null && !(_loopBroken.TryGetValue(town, out broke) && broke == day);
        }

        public void LoopBroken(string town, long day, string name, string kind, int hour)
        {
            if (town == null)
                return;
            _loopBroken[town] = day;
            _loopBreaks++;
            Log("LOOP broken: " + name + " at " + hour + ":00 in " + town + " (" + kind + ")");
        }

        /// The reset: the loop closes over them, cheerful and blank; their friends notice for two days.
        public void ResetWake(Person p, PersonState st, long now)
        {
            if (p == null || st == null)
                return;
            st.WakeStage = 0;
            st.WakeStageAt = now;
            st.Awareness = Math.Min(st.Awareness, Awakening.Floor(0));
            st.Mark("reset");
            _resets++;
            foreach (string f in Trials.FriendsOf(p.Id, Store.Ties))
                Remember(f, "talk", "reset", "heard", p.Home, now, 0, 45, "reset:" + p.Id + ":" + now / 1440 + ":" + f, p.Id, p.Name);
            Log("WAKE reset: " + p.Name + " (stage 0, awareness " + st.Awareness + ")");
        }

        public string LoopEngineStatus()
        {
            return _voices + " voices, " + _loopBreaks + " loops broken, " + _resets + " resets";
        }
    }
}
