// One way to play a row of the catalogue (FIVEDAY2 item 7; Core/Catalogue.cs). Until now every feature played its own clips its own way, and
// nothing played from the catalogue. Perform takes a row and a person and does what the row says through the same calls the proven
// features use: a gesture on the upper body so their feet stay where they are, a clip over the whole body, a scenario in place, a
// line in their own voice, a look or a turn. The animation set is held while it plays and let go after (Game/Loads.cs), and a
// set that won't load or a clip the game doesn't have fails soft: the log says which, once, and whatever called it carries on without it.
// The first user is the clip walker (WestworldScript.AnimTest.cs), then the composed acts and the things to come across.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class Performing
        {
            public int Handle, AskedAt, Until, Flags;
            public string Name, Dict, Clip;
            public bool Held, Started;
        }

        private const int PerformLoadMs = 3000, PerformScenarioMs = 8000, PerformVoiceMs = 2500, PerformMoveMs = 2000;
        private const int PerformUpper = 8 | 16;   // upper body, secondary: as the reactions and the nods play
        private readonly List<Performing> _performing = new List<Performing>();
        private readonly HashSet<string> _performFailNoted = new HashSet<string>();
        private int _performStarted, _performFailed;
        private string _performLast;
        private Action _aPerform;

        /// Someone playing a row now, or still waiting for its set.
        private bool IsPerforming(int h)
        {
            for (int i = 0; i < _performing.Count; i++)
                if (_performing[i].Handle == h)
                    return true;
            return false;
        }

        /// A row on this person: null when it has begun or is waiting for its set, else why not. `target`: whom a look, a turn or a
        /// line is aimed at. `ms`: about how long it takes, for whoever is timing what comes next.
        private string Perform(int h, Expression row, int target, out int ms)
        {
            ms = 0;
            if (row == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                return "gone";
            if (IsPerforming(h))
                return "already at something";
            int now = Game.GameTime;
            if (row.HasClip)
            {
                string dict = row.Dict(!PED.IS_PED_MALE(h));
                if (!STREAMING.DOES_ANIM_DICT_EXIST(dict))
                    return PerformFailed(row.Name, "not a set this game has");
                _performing.Add(new Performing { Handle = h, AskedAt = now, Name = row.Name, Dict = dict, Clip = row.Clip, Flags = row.Channel == "gesture" ? PerformUpper : 0 });
                ms = 3000;   // until the set is in and its length known
                return null;
            }
            switch (row.Channel)
            {
                case "scenario":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(row.Name), PerformScenarioMs, true, 0u, -1f, false);
                    ms = PerformScenarioMs;
                    break;
                case "voice":
                    if (!Speech.Say(h, row.Name, target))
                        return PerformFailed(row.Name, "no such line in this voice");
                    ms = PerformVoiceMs;
                    break;
                case "move":
                    if (!PerformMove(h, row.Name, target))
                        return "not a move to make here";
                    ms = PerformMoveMs;
                    break;
                default:
                    return "not something to play (" + row.Channel + ")";
            }
            _performStarted++;
            _performLast = row.Name;
            return null;
        }

        private static bool PerformMove(int h, string name, int target)
        {
            switch (name)
            {
                case Catalogue.TurnMove:
                    if (target == 0) return false;
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, target, PerformMoveMs, 0f, 0f, 0f);
                    return true;
                case Catalogue.LookMove:
                    if (target == 0) return false;
                    TASK.TASK_LOOK_AT_ENTITY(h, target, PerformMoveMs, 0, 51, 0);
                    return true;
                case Catalogue.StillMove:
                    TASK.TASK_STAND_STILL(h, PerformMoveMs);
                    return true;
                case Catalogue.WanderMove:
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    return true;
                default:
                    return false;
            }
        }

        private string PerformFailed(string name, string why)
        {
            _performFailed++;
            if (_performFailNoted.Add(name) && _performFailNoted.Count < 200)
                Log.Write("PERFORM " + name + ": " + why + " (played without it)");
            return why;
        }

        /// Each frame while anyone is at a row: their set loaded and the clip begun, and the set let go when it's over.
        private void PerformStep(int now)
        {
            if (_performing.Count == 0)
                return;
            if (_aPerform == null)
                _aPerform = () => PerformTick(_now);
            Safe("perform", _aPerform);
        }

        private void PerformTick(int now)
        {
            for (int i = _performing.Count - 1; i >= 0; i--)
            {
                Performing p = _performing[i];
                bool gone = !ENTITY.DOES_ENTITY_EXIST(p.Handle) || ENTITY.IS_ENTITY_DEAD(p.Handle);
                if (gone || (p.Started && now >= p.Until))
                {
                    PerformEnd(i);
                    continue;
                }
                if (p.Started)
                    continue;
                if (Loads.AnimFor(p.Dict, ref p.Held))
                {
                    TASK.TASK_PLAY_ANIM(p.Handle, p.Dict, p.Clip, 4f, -4f, -1, p.Flags, 0f, false, 0, false, "", false);
                    float secs = ENTITY.GET_ANIM_DURATION(p.Dict, p.Clip);
                    p.Started = true;
                    p.Until = now + (secs > 0f ? Math.Min((int)(secs * 1000f), 15000) : 4000) + 200;
                    _performStarted++;
                    _performLast = p.Name;
                    continue;
                }
                if (now - p.AskedAt > PerformLoadMs || now < p.AskedAt)
                {
                    PerformFailed(p.Name, "its set didn't load in time");
                    PerformEnd(i);
                }
            }
        }

        /// Anything still waiting for its set on this person is dropped: whatever comes next is not ours to overlay.
        private void PerformCancel(int h)
        {
            for (int i = _performing.Count - 1; i >= 0; i--)
                if (_performing[i].Handle == h && !_performing[i].Started)
                    PerformEnd(i);
        }

        private void PerformEnd(int i)
        {
            Performing p = _performing[i];
            _performing.RemoveAt(i);
            Loads.LetGoAnim(p.Dict, ref p.Held);
        }

        /// F7.
        private string PerformStatus()
        {
            return _performStarted + " played this session, " + _performFailed + " failed soft (" + _performFailNoted.Count + " different), " + _performing.Count + " going now" + (_performLast != null ? "; last: " + _performLast : "");
        }
    }
}
