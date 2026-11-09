// What RealNPCs asks the game to keep in memory (FIVEDAY2 0a; players on Nexus, 2026-11: VRAM climbing and stutters). The game
// streams a body or an animation set in when a script asks and keeps it for as long as that script's ask stands, so an ask that's
// never let go is memory the game can't take back.
//
// Bodies are the ones that sit on the graphics card (their textures). Spawner.Spawn has always let go of a model once it made
// someone from it, but a model asked for and then never used (the encounter that ended early, the ring that couldn't stand there,
// the friend who never rode in) stayed asked for. Spawner.Ready now notes each ask here; a model nobody has asked after for
// ModelIdleMs is let go.
//
// Animation sets are counted, and those asked for through AnimFor are let go when their user has finished. The ones the older
// features ask for stay as they were on purpose: they check for "loaded" before they ask again, so an ask can't tell a set in
// constant use from one that's idle, a set let go would cost that feature its next gesture, and animation data is ordinary memory
// rather than the card's, so there's little to win. Those sets are a fixed list, so they can't grow without end.
using System;
using System.Collections.Generic;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Loads
    {
        public const int ModelIdleMs = 45000, SweepMs = 10000;

        private static readonly Dictionary<uint, int> _modelsAsked = new Dictionary<uint, int>();   // model -> when last asked after
        private static readonly List<uint> _drop = new List<uint>();
        private static readonly HashSet<string> _setsAsked = new HashSet<string>();
        private static readonly Dictionary<string, int> _setsLeased = new Dictionary<string, int>();   // set -> how many users hold it now
        private static int _sweepAt, _modelsLetGo, _modelsMade, _setsLetGo;
        private const int LogEveryMs = 300000;
        private static int _loggedAt;
        private static string _logged;

        /// Spawner.Ready: this model is wanted now (asked for, or found loaded and about to be used).
        public static void ModelAsked(uint model, int now)
        {
            _modelsAsked[model] = now;
        }

        /// Spawner.Spawn: someone has been made from it and the ask let go.
        public static void ModelUsed(uint model)
        {
            _modelsAsked.Remove(model);
            _modelsMade++;
        }

        /// Asks for an animation set the way every feature always has; only counted (see the top of this file).
        public static void Anim(string dict)
        {
            if (dict == null)
                return;
            _setsAsked.Add(dict);
            STREAMING.REQUEST_ANIM_DICT(dict);
        }

        /// Takes a hold on an animation set for someone about to play from it: true once it's loaded. Pair with LetGoAnim. A set
        /// the game doesn't have answers false for good, so a clip that isn't there fails soft.
        public static bool AnimFor(string dict, ref bool held)
        {
            if (dict == null || !STREAMING.DOES_ANIM_DICT_EXIST(dict))
                return false;
            if (!held)
            {
                held = true;
                int n;
                _setsLeased.TryGetValue(dict, out n);
                _setsLeased[dict] = n + 1;
                _setsAsked.Add(dict);
            }
            if (STREAMING.HAS_ANIM_DICT_LOADED(dict))
                return true;
            STREAMING.REQUEST_ANIM_DICT(dict);
            return false;
        }

        /// The hold taken by AnimFor ends; with nobody else holding it, the game may have the memory back.
        public static void LetGoAnim(string dict, ref bool held)
        {
            if (!held || dict == null)
                return;
            held = false;
            int n;
            if (!_setsLeased.TryGetValue(dict, out n))
                return;
            if (n > 1)
            {
                _setsLeased[dict] = n - 1;
                return;
            }
            _setsLeased.Remove(dict);
            STREAMING.REMOVE_ANIM_DICT(dict);
            _setsLetGo++;
        }

        /// A few times a minute: a model nobody has asked after lately is let go.
        public static void Sweep(int now)
        {
            if (now < _sweepAt && now >= _sweepAt - SweepMs * 2)
                return;
            _sweepAt = now + SweepMs;
            if (now >= _loggedAt)
            {
                _loggedAt = now + LogEveryMs;
                string s = Status();
                if (s != _logged)
                {
                    _logged = s;
                    Log.Write("LOADS " + s);
                }
            }
            if (_modelsAsked.Count == 0)
                return;
            _drop.Clear();
            foreach (KeyValuePair<uint, int> kv in _modelsAsked)
                if (now - kv.Value >= ModelIdleMs || now < kv.Value)
                    _drop.Add(kv.Key);
            for (int i = 0; i < _drop.Count; i++)
            {
                STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(_drop[i]);
                _modelsAsked.Remove(_drop[i]);
                _modelsLetGo++;
            }
        }

        /// F7, and the log every few minutes.
        public static string Status()
        {
            return _modelsAsked.Count + " body models asked for and not yet used, " + _modelsMade + " used and let go, " + _modelsLetGo + " never used and let go; " +
                   _setsAsked.Count + " animation sets asked for this session, " + _setsLeased.Count + " held now, " + _setsLetGo + " let go";
        }
    }
}
