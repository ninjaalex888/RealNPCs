// What we change on someone, put back after (FOURTEENHOUR item 3; TWELVEHOUR item 5). To make a man fight, eight paths of
// ours set the game's combat attributes 5 ("fight even unarmed", against the armed) and 46 ("always fight") on people who
// aren't ours: allies who come to your aid, a brawl's holdouts after the law's word (and the lawman who fights them), callers
// who come at you, rival gangs who meet, people who join a fight of yours, the waking who lash out (and the lawman who steps
// in), the road's fistfights; and two put people in the REALNPCS_HOSTILE group (callers and thieves, so you may deal with them
// without it being a crime). Only the Director saved what was there and put it back (its casts and joiners, FightAttrIds).
// Everyone else kept "always fight" until their body despawned: a townsman who threw one punch in a brawl stayed a fighter
// for good (it may be behind the Nexus report of townsfolk joining a robber, the game's own robbery, as fighters would).
// Now every one of those paths asks KeepOwn first (LendFists, LendGroup): the first change keeps what was there; and once
// they're out of any fight for 20 s, and nothing of ours still has them (a scene, an ally, a holdout, a theft under way,
// ours), they get their own back. As enRAGE's companion code saves a companion's group, flags and attributes and puts them
// back when he's done (its author permits the reuse; the idea, not its code). Behind [Mind] HandBackLent (on).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class LentState
        {
            public bool[] Attrs;      // their own 5 and 46, as they were before the first of ours touched them
            public bool GroupKept;
            public uint Group;        // their own relationship group, when one of ours changed it
            public int Since, QuietSince;
            public string By;         // the path that first lent them (for the log)
            public uint Model;        // whose handle it was: the game reuses them (the review of items 1-13)
        }

        private static readonly int[] LentAttrIds = { 5, 46 };   // what the paths set: fight even unarmed, always fight
        private const int LentQuietMs = 20000, LentLookMs = 1000, LentLookMost = 8;
        private readonly Dictionary<int, LentState> _lent = new Dictionary<int, LentState>();
        private readonly List<int> _lentKeys = new List<int>();   // the sweep's order, reused (no list a look)
        private int _lentLookAt, _lentNext, _lentBack, _lentGone;
        private readonly Dictionary<string, int> _lentBackBy = new Dictionary<string, int>();
        private Action _aHandBack;

        /// What was there before one of ours changes it: kept the first time only, so the true own is what comes back.
        private void KeepOwn(int h, string by, bool group = false)
        {
            if (_cfg == null || !_cfg.HandBackLent || h == 0 || h == Game.Player.Character.Handle || !ENTITY.DOES_ENTITY_EXIST(h))
                return;
            LentState s;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            if (!_lent.TryGetValue(h, out s) || s.Model != model)
            {
                s = new LentState { Attrs = new bool[LentAttrIds.Length], Since = _now, QuietSince = -1, By = by, Model = model };
                for (int i = 0; i < LentAttrIds.Length; i++)
                    s.Attrs[i] = PED._GET_PED_COMBAT_ATTRIBUTE(h, LentAttrIds[i]);
                _lent[h] = s;
            }
            if (group && !s.GroupKept)
            {
                s.Group = PED.GET_PED_RELATIONSHIP_GROUP_HASH(h);
                s.GroupKept = true;
            }
        }

        /// Fists for a fight of ours (every path's two lines before): what was there kept, then 5 and 46 on.
        private void LendFists(int h, string by, bool unarmedToo = true)
        {
            KeepOwn(h, by);
            if (unarmedToo)
                PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);    // fights even against the armed
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);       // always fights
        }

        /// A relationship group of ours (the hostile one), what was there kept.
        private void LendGroup(int h, uint group, string by)
        {
            KeepOwn(h, by, group: true);
            PED.SET_PED_RELATIONSHIP_GROUP_HASH(h, group);
        }

        /// Their own again: 5 and 46 as they were, and their group.
        private void GiveOwnBack(int h, LentState s, string why)
        {
            _lent.Remove(h);
            bool still = ENTITY.DOES_ENTITY_EXIST(h);
            if (!still)
                return;
            for (int i = 0; i < LentAttrIds.Length; i++)
                PED.SET_PED_COMBAT_ATTRIBUTES(h, LentAttrIds[i], s.Attrs[i]);
            if (s.GroupKept)
                PED.SET_PED_RELATIONSHIP_GROUP_HASH(h, s.Group);
            _lentBack++;
            int n;
            _lentBackBy.TryGetValue(s.By ?? "?", out n);
            _lentBackBy[s.By ?? "?"] = n + 1;
            Log.Detail("LENT back: " + NameToShow(h) + " has their own again (" + s.By + (s.GroupKept ? ", the group too" : "") + "; " + why + ", " + (_now - s.Since) / 1000 + " s after)");
        }

        /// Still in use by something of ours: a scene, a walk-up, a wake act, ours (spawned or borrowed), a holdout, an ally,
        /// someone who joined your fight, a gang's man, the thief while his theft runs.
        private bool LentInUse(int h)
        {
            if (NotFree(h) || Spawner.IsOurs(h) || _ring.Contains(h) || JoinIsPending(h) || _brawlHoldouts.ContainsKey(h) || _youJoiners.Contains(h))
                return true;
            if (_theft != null && _theft.Thief != null && _theft.Thief.Handle == h)
                return true;
            if (_thieves.Contains(h) || (_horseTheft != null && _horseTheft.Thief != null && _horseTheft.Thief.Handle == h))
                return true;   // a thief still fair game, a horse thief's chase (the review of items 1-13: his group came back mid-chase)
            for (int i = 0; i < _allies.Count; i++)
                if (_allies[i].Handle == h)
                    return true;
            return _gang != null && (_gang.SideA.Contains(h) || _gang.SideB.Contains(h));   // a gang shootout under way
        }

        /// A look every second at up to eight of them, in turn (sliced, no list a look): the gone dropped, the dead dropped, those
        /// fighting or still ours left as they are, and the rest given their own back once they've been quiet 20 s.
        private void HandBackStep(int now)
        {
            if (_lent.Count == 0 || now < _lentLookAt)
                return;
            if (_aHandBack == null)
                _aHandBack = () => HandBackLook(_now);
            Safe("hand back", _aHandBack);
        }

        private void HandBackLook(int now)
        {
            _lentLookAt = now + LentLookMs;
            if (!_cfg.HandBackLent)
            {
                _lent.Clear();
                return;
            }
            if (_lentNext >= _lentKeys.Count)
            {
                _lentKeys.Clear();
                foreach (int k in _lent.Keys)
                    _lentKeys.Add(k);
                _lentNext = 0;
            }
            for (int n = 0; n < LentLookMost && _lentNext < _lentKeys.Count; n++)
            {
                int h = _lentKeys[_lentNext++];
                LentState s;
                if (!_lent.TryGetValue(h, out s))
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || ENTITY.GET_ENTITY_MODEL(h) != s.Model)   // (another's now: the handle reused)
                {
                    _lent.Remove(h);
                    _lentGone++;
                    continue;
                }
                if (PED.IS_PED_IN_COMBAT(h, 0) || LentInUse(h))
                {
                    s.QuietSince = -1;
                    continue;
                }
                if (s.QuietSince < 0)
                    s.QuietSince = now;
                else if (now - s.QuietSince >= LentQuietMs)
                    GiveOwnBack(h, s, "out of the fight 20 s");
            }
        }

        /// When the add-on stops: everyone still lent gets their own back (the bodies stay in the world).
        private void HandBackAll(string why)
        {
            foreach (KeyValuePair<int, LentState> kv in _lent.ToList())
                GiveOwnBack(kv.Key, kv.Value, why);
        }

        /// F7: how many are lent now, and how many have had their own back, by the path that lent them.
        private string HandBackStatus()
        {
            if (!_cfg.HandBackLent)
                return "off ([Mind] HandBackLent): what we set stays until the body despawns, as before";
            return _lent.Count + " lent now" + (_lent.Count > 0 ? " (" + string.Join(", ", _lent.Values.GroupBy(v => v.By).Select(g => g.Key + " " + g.Count())) + ")" : "") +
                   "; " + _lentBack + " given their own back this session" + (_lentBackBy.Count > 0 ? " (" + string.Join(", ", _lentBackBy.Select(kv => kv.Key + " " + kv.Value)) + ")" : "") +
                   ", " + _lentGone + " gone first";
        }
    }
}
