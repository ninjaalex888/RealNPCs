// The town made braver ([World] Bravery, 0-100, 85 as built; Lex, 2026-10-31: "we should make all NPCs much, much more
// brave ... it's also a game so it will be more exciting for the player"). The fleeing seen in Saint Denis was the game's
// own panic, not ours (the mod's own "hurries off" fired zero times), so the game's own answer is changed, on the brave
// ones only: every 2 s, in the people ledger's own pass over the street (no array of our own), each ambient grown-up
// within 60 m is asked once (Core/Bravery: by handle and model, so the odd coward still runs and a handle the game gives
// someone else is asked again), and the brave get PCF 44, PCF_CowerInsteadOfFlee (femga's CPED_CONFIG_FLAGS and Halen84's
// ePedScriptConfigFlags agree on it): where the game would send them running they stay where they are. Their own value is
// read first and put back when they're no longer ours to change (taken by a mission or enRAGE, Bravery lowered, far off).
// Never a child, a story model, a lawman, another script's person, one of enRAGE's people or one of the mod's own; and
// whoever Arthur aims at or locks on to is put back that frame, so a hold-up or a robbery gets the game's own answer.
// Nothing that blocks events (they still gather to watch a fight, report a killing, put their hands up). The fighters
// themselves get CA 77 and 78 in the Director. Not yet seen in game.
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
        private const int BraveEveryMs = 2000;
        private const float BraveMetres = 60f;
        private const int PcfCowerInsteadOfFlee = 44;

        private sealed class BraveRec
        {
            public uint Model;
            public bool Orig;      // PCF 44 as the game had it
            public bool Set;       // ours set now
            public int Seen;       // the last sweep that saw them
            public int HeldUntil;  // aimed at: left as the game has them until then
        }

        private readonly Dictionary<int, BraveRec> _brave = new Dictionary<int, BraveRec>();
        private int _braveNextAt, _braveSweepAt, _braveRestored, _braveAimed, _braveReused, _braveLooked;
        private bool _braveDue, _braveFailed;
        private int _braveAimLast;
        private Action _aBraveAim;   // made once: the frame step allocates nothing

        /// Called at the top of the ledger's pass: whether this pass is a bravery sweep.
        private void BraveSweepBegin(int now)
        {
            _braveDue = !_braveFailed && now >= _braveNextAt;
            if (!_braveDue)
                return;
            _braveNextAt = now + BraveEveryMs;
            _braveSweepAt = now;
            if (_cfg.Bravery <= 0 && _brave.Count > 0)
            {
                BraveRestoreAll("Bravery is 0");
                _braveDue = false;
            }
            else if (_cfg.Bravery <= 0)
                _braveDue = false;
        }

        /// One ped of the ledger's pass (within its range, human, not the player).
        private void BraveOne(Ped p, int h, float dist)
        {
            if (!_braveDue)
                return;
            try
            {
                BraveRec r;
                bool known = _brave.TryGetValue(h, out r);
                uint model = ENTITY.GET_ENTITY_MODEL(h);
                if (known && r.Model != model)
                {
                    _brave.Remove(h);   // the game gave the handle to someone else: theirs is untouched, asked afresh
                    _braveReused++;
                    known = false;
                }
                if (!known && dist > BraveMetres)
                    return;
                if (known)
                    r.Seen = _braveSweepAt;
                if (known && r.HeldUntil - _braveSweepAt > 0)
                    return;
                bool aimed = h == _braveAimLast, ours = Spawner.IsOurs(h) || Spawner.IsCast(h) || Spawner.IsBorrowed(h);
                // the cheap questions first; enRAGE's (a reflection call) only for one who'd be brave
                string d = Bravery.Decide(_cfg.Bravery, h, model, Natives.IsChild(p), IsGrownUp(p), Natives.StoryModel(p), Natives.IsLaw(p), false, false, ours, ENTITY.IS_ENTITY_DEAD(h), aimed);
                if (d == "brave")
                {
                    bool record = IsRecord(h);
                    d = Bravery.Decide(_cfg.Bravery, h, model, false, true, false, false, Natives.OtherScripts(h) && !record, record, false, false, false);
                }
                if (d == "brave")
                {
                    if (!known)
                    {
                        _braveLooked++;
                        r = new BraveRec { Model = model, Orig = PED.GET_PED_CONFIG_FLAG(h, PcfCowerInsteadOfFlee, false), Seen = _braveSweepAt };
                        _brave[h] = r;
                    }
                    if (!r.Set)
                    {
                        PED.SET_PED_CONFIG_FLAG(h, PcfCowerInsteadOfFlee, true);
                        r.Set = true;
                    }
                    return;
                }
                if (known)
                {
                    BraveRestore(h, r);   // no longer ours to change (a mission, enRAGE, dead, Bravery lowered)
                    _brave.Remove(h);
                }
            }
            catch (Exception ex)
            {
                _braveFailed = true;
                _braveDue = false;   // the rest of this pass too: nobody made braver after the failure (the review, 2026-10-31)
                Log.Write("BRAVERY failed once, off for this session (people are left as the game has them): " + ex.Message);
                BraveRestoreAll("it failed");
            }
        }

        /// At the end of the ledger's pass: those not seen for 30 s (far off, gone) are put back and forgotten.
        private void BraveSweepEnd(int now)
        {
            if (!_braveDue || _brave.Count == 0)
                return;
            List<int> stale = null;
            foreach (KeyValuePair<int, BraveRec> kv in _brave)
                if (now - kv.Value.Seen > 30000)
                    (stale ?? (stale = new List<int>())).Add(kv.Key);
            if (stale == null)
                return;
            foreach (int h in stale)
            {
                BraveRec r = _brave[h];
                if (ENTITY.DOES_ENTITY_EXIST(h) && ENTITY.GET_ENTITY_MODEL(h) == r.Model)
                    BraveRestore(h, r);
                _brave.Remove(h);
            }
        }

        /// Every frame, cheap (two natives, no allocation): whoever Arthur aims at or locks on to gets the game's own answer
        /// back at once (hands up, the robbery prompts), and is left so for 20 s.
        private unsafe void BraveAimStep(int now)
        {
            if (_brave.Count == 0)
                return;
            int t = 0;
            ulong slot = 0;
            if (PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
            {
                if (PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot))
                    t = (int)(uint)slot;
                if (t == 0)
                {
                    slot = 0;
                    if (PLAYER.GET_PLAYER_TARGET_ENTITY(Game.Player.Handle, (int*)&slot))
                        t = (int)(uint)slot;   // aim assist's lock-on, as the gunpoint code reads it (Frontier.cs; the review, 2026-10-31)
                }
            }
            if (t == 0)
            {
                int lock0 = 0;
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &lock0, false, false);
                t = lock0;
            }
            _braveAimLast = t;
            BraveRec r;
            if (t == 0 || !_brave.TryGetValue(t, out r))
                return;
            if (r.Set)
            {
                BraveRestore(t, r);
                _braveAimed++;
            }
            r.HeldUntil = now + 20000;
        }

        /// Someone the mod is about to send running (a pickpocket, a shooter, a thief, a scene's fright): PCF 44 put back as
        /// the game had it first and left so for two minutes, so the run isn't turned into cowering where he stands (the
        /// review, 2026-10-31; whether the game's smart flee heeds PCF 44 at all is not yet seen in game). A lookup, nothing
        /// for one never made braver.
        private void BraveRelease(int h)
        {
            BraveRec r;
            if (h == 0 || !_brave.TryGetValue(h, out r))
                return;
            BraveRestore(h, r);
            r.HeldUntil = Game.GameTime + 120000;
        }

        private void BraveRestore(int h, BraveRec r)
        {
            if (!r.Set)
                return;
            if (ENTITY.DOES_ENTITY_EXIST(h))
                PED.SET_PED_CONFIG_FLAG(h, PcfCowerInsteadOfFlee, r.Orig);
            r.Set = false;
            _braveRestored++;
        }

        /// Everyone put back as the game had them (Bravery 0, a failure, the script stopping).
        private void BraveRestoreAll(string why)
        {
            int n = 0;
            foreach (KeyValuePair<int, BraveRec> kv in _brave)
                if (kv.Value.Set && ENTITY.DOES_ENTITY_EXIST(kv.Key) && ENTITY.GET_ENTITY_MODEL(kv.Key) == kv.Value.Model)
                {
                    BraveRestore(kv.Key, kv.Value);
                    n++;
                }
            _brave.Clear();
            if (n > 0)
                Log.Write("BRAVERY: " + n + " put back as the game had them (" + why + ")");
        }

        /// F7: how many are braver now.
        private string BraveryStatus()
        {
            if (_braveFailed)
                return "FAILED once this session (see the log)";
            if (_cfg.Bravery <= 0)
                return "off ([World] Bravery 0: as the game has it)";
            int set = _brave.Values.Count(x => x.Set);
            return set + " braver near you now (Bravery " + _cfg.Bravery + "; " + _braveLooked + " made braver this session, " + _braveRestored + " put back, " + _braveAimed + " of them for your aim, " + _braveReused + " handles the game gave someone else)";
        }
    }
}
