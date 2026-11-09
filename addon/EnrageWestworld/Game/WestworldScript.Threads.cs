// Thoughts that follow on, in the game (FIVEDAY2 item 27; Core/ThoughtThreads.cs; [Ticker] ThoughtsFollowOn). ThoughtNow notes each loud moment as it's thought; once the moment is over, the
// person's next thought or two come from it. The model is told too, through Engine.StillOnMind.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, ThreadNote> _threads = new Dictionary<string, ThreadNote>();
        private readonly Dictionary<string, int> _lastThoughtAt = new Dictionary<string, int>();
        private int _threadFollows;
        private string _threadLast;

        /// A loud moment was just thought: it's what's on their mind for a while.
        private void ThreadLoud(string key, string source, int now)
        {
            if (!_cfg.ThoughtsFollowOn || key == null)
                return;
            string family = ThoughtThreads.FamilyOf(source);
            if (family == null)
                return;
            ThreadNote n;
            if (!_threads.TryGetValue(key, out n))
            {
                if (_threads.Count > 200)
                    foreach (string old in _threads.Where(kv => now - kv.Value.At > ThoughtThreads.KeepMs || now < kv.Value.At).Select(kv => kv.Key).ToList())
                        _threads.Remove(old);
                _threads[key] = n = new ThreadNote();
            }
            n.Family = family;
            n.At = now;
            n.Follows = 0;
        }

        /// The quiet after it: their next thought, if it follows on.
        private string ThreadFollow(string key, Person p, int now)
        {
            ThreadNote n;
            if (!_cfg.ThoughtsFollowOn || key == null || !_threads.TryGetValue(key, out n) || !ThoughtThreads.Open(n, now))
                return null;
            string seed = key + "|" + n.At + "|" + n.Follows;
            if (!Voice.Chance(seed + "|follow", ThoughtThreads.Chance))
                return null;
            string line = ThoughtThreads.After(n.Family, p != null ? p.Personality : null, n.Follows, seed);
            if (line == null)
                return null;
            n.Follows++;
            _threadFollows++;
            _threadLast = (p != null ? p.Name : key) + " (" + n.Family + "): " + line;
            if (Log.Debug)
                Log.Detail("FOLLOWS ON " + _threadLast);
            return line;
        }

        /// For the model: what they've just been through, in a few words, or null.
        private string ThreadStill(string key)
        {
            ThreadNote n;
            return _cfg.ThoughtsFollowOn && key != null && _threads.TryGetValue(key, out n) && Game.GameTime >= n.At && Game.GameTime - n.At < ThoughtThreads.KeepMs ? ThoughtThreads.Words(n.Family) : null;
        }

        /// How long ago this person last thought, in ms; -1 if not lately.
        private int ThreadLastThought(string key)
        {
            int at;
            return key != null && _lastThoughtAt.TryGetValue(key, out at) && Game.GameTime >= at ? Game.GameTime - at : -1;
        }

        private void ThreadThoughtShown(string key, int now)
        {
            if (key == null)
                return;
            if (_lastThoughtAt.Count > 300)
                PruneTimes(_lastThoughtAt, now, ThoughtThreads.CarryOnMs);
            _lastThoughtAt[key] = now;
        }

        private string ThreadStatus()
        {
            return _cfg.ThoughtsFollowOn ? _threadFollows + " thoughts followed on from what just happened" + (_threadLast != null ? ", last " + _threadLast : "") : "follow-on thoughts off ([Ticker] ThoughtsFollowOn)";
        }
    }
}
