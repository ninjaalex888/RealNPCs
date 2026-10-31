// Aimed at, let go to the game (Lex, 2026-10-31, Saint Denis: "the first few didn't react ... stuck from previous scenes
// possibly"; later a woman put her hands up and begged, the game's own). Every frame, cheap: whoever Arthur free-aims at,
// or locks on to with a gun in his hand (never the unarmed lock-on that Talk and Flirt use), is looked at once per person
// (by handle and model, again after a minute): whatever of RealNPCs' still holds them is ended at once (a street scene or
// its fight, the fight's ring, someone on the way to join, a waking act or hold, a mind's act, an onlooker's act, the Talk
// menu, a conversation, bravery's PCF 44), and an ambient person's events are let go as a safety net, so the game's own
// answer to a gun (hands up, begging, running) takes over. Never during enRAGE's questioning: a man it can question is
// left to it at gunpoint (Bridge.Gunpoint.cs). Our own encounter cast (spawned or borrowed, KEEP_TASK) are only logged.
// For 10 s after, nothing of ours starts on them (the mind's acts, the ring, a conversation: AimedLately). Log
// "AIM: <name> was held by <what>; let go to the game", or "nothing of ours held them".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int AimAgainMs = 60000, AimLatelyMs = 10000;
        private static readonly uint AimFists = Natives.Hash("WEAPON_UNARMED");
        private readonly Dictionary<int, KeyValuePair<uint, int>> _aimSeen = new Dictionary<int, KeyValuePair<uint, int>>();   // handle -> (model, when looked at)
        private readonly Dictionary<int, int> _aimLatelyAt = new Dictionary<int, int>();   // handle -> the last frame Arthur aimed at them
        private int _aimLooked, _aimLetGo, _aimEnrage;
        private string _aimLastWhat;
        private Action _aAimLetGo;

        private void AimLetGoStep()
        {
            Safe("aim lets go", _aAimLetGo ?? (_aAimLetGo = () => AimLetGoTick(_now)));
        }

        /// Aimed at in the last 10 s: nothing of ours starts on them, so the game's own answer isn't undone.
        private bool AimedLately(int h, int now)
        {
            int at;
            return _aimLatelyAt.Count > 0 && _aimLatelyAt.TryGetValue(h, out at) && now - at >= 0 && now - at < AimLatelyMs;
        }

        private unsafe void AimLetGoTick(int now)
        {
            int player = Game.Player.Handle;
            int t = 0;
            ulong slot = 0;
            if (PLAYER.IS_PLAYER_FREE_AIMING(player))
            {
                if (PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(player, (int*)&slot))
                    t = (int)(uint)slot;
                if (t == 0)
                {
                    slot = 0;
                    if (PLAYER.GET_PLAYER_TARGET_ENTITY(player, (int*)&slot))
                        t = (int)(uint)slot;
                }
            }
            if (t == 0)
            {
                // locked on with a gun in his hand: the interaction lock-on, never the unarmed one Talk and Flirt use
                ulong w = 0;
                Ped me0 = Game.Player.Character;
                if (WEAPON.GET_CURRENT_PED_WEAPON(me0.Handle, (uint*)&w, true, 0, false) && (uint)w != AimFists && (uint)w != 0)
                {
                    int lock0 = 0;
                    PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(player, &lock0, false, false);
                    t = lock0;
                }
            }
            if (t == 0 || !ENTITY.DOES_ENTITY_EXIST(t) || !ENTITY.IS_ENTITY_A_PED(t))
                return;
            _aimLatelyAt[t] = now;
            if (_aimLatelyAt.Count > 64)
                AimPrune(now);
            uint model = ENTITY.GET_ENTITY_MODEL(t);
            KeyValuePair<uint, int> seen;
            if (_aimSeen.TryGetValue(t, out seen) && seen.Key == model && now - seen.Value >= 0 && now - seen.Value < AimAgainMs)
                return;   // looked at already: once per person a minute
            _aimSeen[t] = new KeyValuePair<uint, int>(model, now);
            AimLook(t, now);
        }

        private void AimPrune(int now)
        {
            var old = new List<int>();
            foreach (KeyValuePair<int, int> kv in _aimLatelyAt)
                if (now - kv.Value > AimLatelyMs || now < kv.Value)
                    old.Add(kv.Key);
            foreach (int h in old)
                _aimLatelyAt.Remove(h);
            old.Clear();
            foreach (KeyValuePair<int, KeyValuePair<uint, int>> kv in _aimSeen)
                if (now - kv.Value.Value > AimAgainMs || now < kv.Value.Value)
                    old.Add(kv.Key);
            foreach (int h in old)
                _aimSeen.Remove(h);
        }

        /// The first look at someone aimed at: what of ours holds them, let go.
        private void AimLook(int h, int now)
        {
            if (!PED.IS_PED_HUMAN(h) || PED.IS_PED_A_PLAYER(h) || ENTITY.IS_ENTITY_DEAD(h) || MISC.GET_MISSION_FLAG())
                return;
            Ped p = new Ped(h);
            if (Natives.StoryModel(p))
                return;
            Ped me = Game.Player.Character;
            string name = MindNameOf(h) ?? TalkName(p);
            if (_bridge != null && (_bridge.Interrogating || _bridge.EnrageHandlesAtGunpoint(p, me, 30f)))
            {
                _aimEnrage++;
                Log.Write("AIM: " + name + " left to enRAGE's questioning");   // a man it can question is left to it at gunpoint
                return;
            }
            bool ours = Spawner.IsOurs(h) || Spawner.IsCast(h) || Spawner.IsBorrowed(h);
            bool record = IsRecord(h);
            if (Natives.OtherScripts(h) && !ours)
                return;   // another script's (a mission, another mod): not ours to touch
            _aimLooked++;
            var what = new List<string>();
            bool tasked = false;   // a hold of ours gave them orders: cleared, so the game's answer isn't fighting them
            string d = _director.LetGo(h);
            if (d != null)
                what.Add(d);
            if (_ring.Remove(h))
            {
                _ringRising.RemoveAll(r => r[0] == h);
                what.Add("the fight ring");
                tasked = true;
            }
            for (int i = _joinPending.Count - 1; i >= 0; i--)
                if (_joinPending[i].Handle == h)
                {
                    JoinLetWalkerGo(_joinPending[i]);
                    _joinPending.RemoveAt(i);
                    what.Add("on the way to join a fight");
                    JoinComingCount();
                }
            for (int i = _wakeActs.Count - 1; i >= 0; i--)
            {
                WakeAct act = _wakeActs[i];
                if (act.Handle != h)
                    continue;
                what.Add("a wake " + (act.Links > 0 ? "hold" : "act") + " (" + act.Kind + ")");
                _wakeActs.RemoveAt(i);
                EndWakeAct(act, "aimed at");
            }
            for (int i = _mindActs.Count - 1; i >= 0; i--)
            {
                if (_mindActs[i].Handle != h)
                    continue;
                what.Add("a mind act (" + _mindActs[i].Kind + ")");
                _mindActs.RemoveAt(i);
                tasked = true;
            }
            if (_onlookBusy.Remove(h))
                what.Add("onlooking");
            if (_menu != null && _menu.Handle == h)
            {
                what.Add("the Talk menu");
                TalkClose("aimed at");
            }
            if (_talk != null && _talk.People != null)
                foreach (Ped tp in _talk.People)
                    if (tp != null && tp.Handle == h)
                    {
                        what.Add("a conversation");
                        TalkEnd(_talk, "cut short: one of them was aimed at", now);
                        break;
                    }
            if (WalkingUp(h))
                what.Add("a walk-up to you (it ends on a gun out by itself)");
            BraveRec br;
            if (_brave.TryGetValue(h, out br))
            {
                if (br.Set)
                {
                    what.Add("bravery (PCF 44)");
                    BraveRestore(h, br);
                    _braveAimed++;
                }
                br.HeldUntil = now + 20000;
            }
            if (_rescuedPed == h && now < _rescuedUntil)
                what.Add("a rescue's thanks");
            if (_wedding != null && (AimIs(_wedding.A, h) || AimIs(_wedding.B, h) || AimIs(_wedding.Preacher, h) || _wedding.Guests.Exists(g => AimIs(g, h))))
                what.Add("a wedding");
            if (_brawlOut.ContainsKey(h))
            {
                what.Add("knocked out in a brawl (left down)");
                Log.Write("AIM: " + name + " is knocked out in a brawl; left down");
                return;
            }
            if (ours)
            {
                Log.Write("AIM: " + name + " is held by an encounter of RealNPCs' (" + (Spawner.IsCast(h) ? "the test cast" : Spawner.IsBorrowed(h) ? "borrowed" : "made by it") + ")" +
                          (what.Count > 0 ? ", and was held by " + string.Join(", ", what) : "") + "; left to it");
                return;
            }
            // the safety net: an ambient person's events let go whatever held them (no game read says whether they were blocked);
            // one of enRAGE's people only when a hold of ours had them, as enRAGE may block its own
            bool unblock = !record || what.Count > 0;
            if (unblock)
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            if (tasked && !PED.IS_PED_USING_ANY_SCENARIO(h))
                TASK.CLEAR_PED_TASKS(h, true, false);
            if (what.Count == 0)
            {
                _aimLastWhat = name + ": nothing";
                Log.Write("AIM: " + name + ": nothing of ours held them" + (unblock ? " (events unblocked)" : ""));
                return;
            }
            _aimLetGo++;
            _aimLastWhat = name + ": " + string.Join(", ", what);
            Log.Write("AIM: " + name + " was held by " + string.Join(", ", what) + "; let go to the game" + (unblock ? " (events unblocked)" : ""));
        }

        private static bool AimIs(Ped p, int h)
        {
            return p != null && p.Handle == h;
        }

        private string AimStatus()
        {
            return "looked at " + _aimLooked + " aimed at, " + _aimLetGo + " let go from a hold of ours, " + _aimEnrage + " left to enRAGE's questioning" + (_aimLastWhat != null ? "; last: " + _aimLastWhat : "");
        }
    }
}
