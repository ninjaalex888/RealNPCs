// The written thought with an act a person's own mind began, and with an encounter's turn their own mind settled (the
// no-model run, item 1; the lines in Core/ActThoughts.cs and beside each encounter's options). Through the rules every
// written thought keeps here: never a line they thought today (SaidToday, shared with the Minds), and with ThoughtVariety
// the frame guard across people (a frame anyone thought within the hour is passed over while another line is left, and
// taken rather than nothing, as ThinkImpl does), the line noted as shown. The game side asks only for people in view, with
// its caps passed (Game/WestworldScript.ActThoughts.cs). Reads and notes in memory only; nothing saved.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        public int ActThoughtsGiven, ActThoughtsNone, TurnThoughtsGiven, TurnThoughtsNone;   // for F7: lines given, and asks with nothing left
        private readonly List<string[]> _actPools = new List<string[]>(2);
        private readonly List<string[]> _turnPools = new List<string[]>(1);
        private bool _actThoughtFailed;

        /// The thought with an act `p`'s own mind just began: what moves them to it, by the act, the need it serves (`why`,
        /// the choice's why) and their temper; `who` the one it is about (a friend, a talker, a foe: a first name they know,
        /// else null), and the player as they know him for keeping clear of him. Null when the act has no lines or none is
        /// left for them today.
        public string ActThought(Person p, string act, string why, string who, string seed)
        {
            if (p == null || p.Id == null || act == null)
                return null;
            try
            {
                ActThoughts.PoolsFor(act, ActThoughts.NeedOf(why), Minds.TemperGroup(p.Personality), _actPools);
                if (_actPools.Count == 0)
                    return null;
                string you = null;
                if (act == "keepclear")
                {
                    PersonState st = Store.Find(p.Id);
                    you = Voice.YouPrivate(p, st != null ? Talk.RefFor(p, st, Player) : Player, true);
                }
                string line = WrittenThought(p.Id, "act:" + act, _actPools, seed, who, you);
                if (line != null)
                    ActThoughtsGiven++;
                else
                    ActThoughtsNone++;
                return line;
            }
            catch (Exception ex)
            {
                if (!_actThoughtFailed)
                    Log("MIND act thought failed for " + p.Name + ", so none for now: " + ex);
                _actThoughtFailed = true;
                return null;
            }
        }

        /// The thought with an encounter's turn (role|option, beside the encounter's options); `id` null for someone the
        /// add-on keeps no state for (a stranger), who then has the frame guard only. `who`: the other in it, if the lines name them.
        public string TurnThought(string id, string role, string option, string who, string seed)
        {
            try
            {
                string[] pool = ActThoughts.TurnPool(role, option);
                if (pool == null)
                    return null;
                _turnPools.Clear();
                _turnPools.Add(pool);
                string line = WrittenThought(id, "turn:" + role, _turnPools, seed, who, null);
                if (line != null)
                    TurnThoughtsGiven++;
                else
                    TurnThoughtsNone++;
                return line;
            }
            catch (Exception ex)
            {
                if (!_actThoughtFailed)
                    Log("ENCOUNTER turn thought failed (" + role + ", " + option + "), so none for now: " + ex);
                _actThoughtFailed = true;
                return null;
            }
        }

        /// The pools in order, each from a seeded place: the first line not thought by them today, passing over a frame anyone
        /// thought within the hour (ThoughtVariety) on the first pass; the second takes one rather than nothing.
        private string WrittenThought(string id, string source, List<string[]> pools, string seed, string who, string you)
        {
            long now = Store.LastGameMin;
            if (now < _thinkClock - 5)
                ForgetThinking();   // an older save was loaded: what was thought after it hasn't happened either (ThinkImpl's rule)
            _thinkClock = now;
            HashSet<string> said = id != null ? SaidToday(id, now) : null;
            bool variety = Cfg != null && Cfg.ThoughtVariety;
            int start = Voice.Hash(seed ?? string.Empty);
            for (int pass = 0; pass < 2; pass++)
                for (int k = 0; k < pools.Count; k++)
                {
                    string[] pool = pools[k];
                    if (pool == null || pool.Length == 0)
                        continue;
                    for (int j = 0; j < pool.Length; j++)
                    {
                        string line = ActThoughts.Fill(pool[(start % pool.Length + j) % pool.Length], who, you);
                        if (line == null || (said != null && said.Contains(line)))
                            continue;
                        if (pass == 0 && variety && FrameHeard(line, now))
                        {
                            FramesHeld++;
                            continue;
                        }
                        if (said != null)
                            said.Add(line);
                        if (variety)
                            NoteShown(source, line, now);
                        Detail("THOUGHT (" + source + ") " + (id ?? "someone") + ": \"" + line + "\"");
                        return line;
                    }
                }
            return null;
        }
    }
}
