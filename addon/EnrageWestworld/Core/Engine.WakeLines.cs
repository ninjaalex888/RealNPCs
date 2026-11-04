// A line that wakes, and the revived dead seen, in the engine (SIXDAY item 12c; Core/WakeLines.cs): a stage-four person's line
// said to someone (the game's scenes), the hearing kept on the listener, the second mouth that wakes them, one a town a day; and
// a revived man seen by someone tied to him, once each, the town chewing on it for a day. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private readonly Dictionary<string, long> _lineWakes = new Dictionary<string, long>();   // town -> the day a line woke someone there
        private int _linesSaid, _lineWoke, _revivedSeen;

        /// Whether two people have any tie between them in the last sixty days.
        public bool TiedTo(string a, string b)
        {
            if (a == null || b == null || Store.Ties == null)
                return false;
            long now = Store.LastGameMin;
            foreach (SceneTie t in Store.Ties)
                if (t != null && ((t.A == a && t.B == b) || (t.A == b && t.B == a)) && now - t.At < 60 * 1440L)
                    return true;
            return false;
        }

        /// The line a stage-four person would say in a scene with this other, or null: at stage four, the other tied to them or waking too.
        public string WakeLineFor(string fromId, string toId)
        {
            if (!Cfg.WestWorld || !Cfg.WakeLines || fromId == null || toId == null || StageOf(fromId) < Awakening.Awake)
                return null;
            if (!TiedTo(fromId, toId) && StageOf(toId) <= 0)
                return null;
            return WakeLines.Said(WakeLines.IndexOf(fromId), fromId + "|" + toId + "|" + Store.LastGameMin / 1440);
        }

        /// The line said: the hearing kept on the listener; heard from two different mouths within three days, the listener wakes
        /// a stage (awareness 40 and over, not awake already), at most one a town a day.
        public void LineSaid(string fromId, string fromName, string toId, string toName, string town, long now)
        {
            if (!Cfg.WestWorld || !Cfg.WakeLines || fromId == null || toId == null || toId == fromId)
                return;
            PersonState st = MindOf(toId, toName);
            if (st == null)
                return;
            _linesSaid++;
            int idx = WakeLines.IndexOf(fromId);
            bool twice = WakeLines.Record(st.Told, idx, fromId, now / 1440);
            Log("WAKE line said: " + (fromName ?? fromId) + " -> " + (toName ?? toId) + " (\"" + WakeLines.Lines[idx] + "\"" + (twice ? ", heard from two now" : "") + ")");
            if (!twice || !WakeLines.CanWake(st.Awareness, st.WakeStage))
                return;
            long day = now / 1440, last;
            if (town != null && _lineWakes.TryGetValue(town, out last) && last == day)
                return;   // one such waking a town a day
            Person p = Find(toId);
            if (p == null && Resident.IsResident(toId))
            {
                Resident r = Store.FindResident(toId);
                if (r != null)
                    p = PersonOfResident(r);
            }
            if (p == null)
                return;
            if (town != null)
                _lineWakes[town] = day;
            int stage = Advance(p, st, now);
            _lineWoke++;
            Log("WAKE line: " + (fromName ?? fromId) + " -> " + (toName ?? toId) + " (stage " + stage + ")");
        }

        /// A revived person's ghost that someone tied to them hasn't reacted to yet: the ghost, for the watch.
        public Ghost RevivedGhost(string id)
        {
            if (!Cfg.WestWorld || !Cfg.WakeLines || Store.Ghosts == null || id == null || HasDied(id))
                return null;
            return Store.Ghosts.FirstOrDefault(g => g != null && (g.Kind == "killing" || g.Kind == "death") && g.Who == id);
        }

        /// Whether this one would know the revived man for dead: tied to him in the last sixty days, and not reacted yet.
        public bool WouldKnowDead(Ghost g, string seerId)
        {
            return g != null && seerId != null && seerId != g.Who && (g.SeenBy == null || !g.SeenBy.Contains(seerId)) && TiedTo(seerId, g.Who);
        }

        /// The revived man seen by one who knew him for dead: once each; awareness up, a stage at sixty; the town talks for a day.
        public void SeenDead(Ghost g, string seerId, string seerName, long now)
        {
            if (g == null || seerId == null)
                return;
            if (g.SeenBy == null)
                g.SeenBy = new List<string>();
            if (g.SeenBy.Contains(seerId))
                return;
            g.SeenBy.Add(seerId);
            _revivedSeen++;
            PersonState st = MindOf(seerId, seerName);
            int stage = -1;
            if (st != null)
            {
                st.Awareness = Math.Min(100, st.Awareness + WakeLines.SeenDeadAwareness);
                if (st.Awareness >= WakeLines.SeenDeadStageAt && st.WakeStage < Awakening.Awake)
                {
                    Person p = Find(seerId);
                    if (p == null && Resident.IsResident(seerId))
                    {
                        Resident r = Store.FindResident(seerId);
                        if (r != null)
                            p = PersonOfResident(r);
                    }
                    if (p != null)
                        stage = Advance(p, st, now);
                }
            }
            string deadFirst = Person.FirstOf(g.WhoName ?? "him");
            int told = 0;
            foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && !x.Fled && x.Town == g.Town && x.Id != seerId && x.Id != g.Who).Take(6))
            {
                Remember(r.Id, "talk", "seendead", "heard", g.Town, now, 0, 40, "seendead:" + g.Id + ":" + r.Id, g.Who, g.WhoName);
                told++;
            }
            Log("WAKE seen-dead: " + (seerName ?? seerId) + " saw " + (g.WhoName ?? g.Who) + (stage >= 0 ? " (stage " + stage + ")" : "") + "; " + told + " of the town talk of it");
        }

        public string WakeLinesStatus()
        {
            return _linesSaid + " lines said, " + _lineWoke + " woken by one, " + _revivedSeen + " revived seen" + (_lineWakes.Count > 0 ? "; today's line-wakes: " + string.Join(", ", _lineWakes.Where(kv => kv.Value == Store.LastGameMin / 1440).Select(kv => kv.Key)) : "");
        }
    }
}
