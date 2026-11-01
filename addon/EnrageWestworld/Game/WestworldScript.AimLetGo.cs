// Aimed at, let go to the game (Lex, 2026-10-31, Saint Denis: "the first few didn't react ... stuck from previous scenes
// possibly"; later a woman put her hands up and begged, the game's own). Every frame, cheap: whoever Arthur free-aims at,
// or locks on to with a gun in his hand (never the unarmed lock-on that Talk and Flirt use), is looked at once per person
// (by handle and model, again after a minute, or after 5 s when something of ours held them): whatever of RealNPCs' still holds them is ended at once (a street scene or
// its fight, the fight's ring, someone on the way to join, a waking act or hold, a mind's act, an onlooker's act, the Talk
// menu, a conversation, bravery's PCF 44), and an ambient person's events are let go as a safety net, so the game's own
// answer to a gun (hands up, begging, running) takes over. Never during enRAGE's questioning: a man it can question is
// left to it at gunpoint (Bridge.Gunpoint.cs: EnrageCanQuestion, that man alone). A walk-up to Arthur, a rescue's thanks,
// an onlooker's acts to come and a wander due are ended too; a lawman on a job of ours is handed straight back
// (Spawner.GiveToGame). Our own encounter cast (spawned, the test cast, or still borrowed after that) are only logged.
// For 10 s after, nothing of ours starts on them (the mind's acts, the ring, a conversation, a brawl's join, a street
// scene, an onlooker's act, a wander: AimedLately). Log
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

        /// One a re-staged encounter holds is aimed at (NIGHT4 item 6, the review across features: "nothing of ours held them"
        /// before, and the encounter's next turn played over the hands-up). The callout's two answer it themselves (its step-in:
        /// the caller gives way; the end gives no wander to one aimed at); anyone else of a callout, or the domestic's people
        /// (but a man who has struck her: that fight is the encounter's own), ends it, all let go. The hogtied's own answer the
        /// gun (the men's draw, the captive tied): noted. What was let go, or null.
        private string StagedAimLetGo(int h, int now, List<string> noted)
        {
            StreetCallout c = _streetCallout;
            if (c != null && (AimIs(c.A, h) || AimIs(c.B, h) || c.On.Exists(o => AimIs(o, h))))
            {
                if ((AimIs(c.A, h) || AimIs(c.B, h)) && !c.YouIn && c.Phase != "call" && c.Phase != "end")
                {
                    ScYouStepIn(c, true, now);
                    noted.Add("a callout (it answers the gun itself)");
                    return null;
                }
                EndStreetCallout((MindNameOf(h) ?? "one of them") + " under your gun", false);
                return "a callout";
            }
            DomesticRow d = _domestic;
            if (d != null && (AimIs(d.Man, h) || AimIs(d.Wife, h) || AimIs(d.Neighbour, h) || AimIs(d.Law, h)))
            {
                if (d.Struck && AimIs(d.Man, h))
                {
                    noted.Add("a domestic (he struck her: the fight is its own)");
                    return null;
                }
                EndDomestic((AimIs(d.Man, h) ? "he" : MindNameOf(h) ?? "one of them") + " under your gun");
                return "a domestic";
            }
            if (_hog != null)
                noted.Add("the hogtied (they answer the gun themselves)");
            return null;
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
            bool record = IsRecord(h);
            // only the man enRAGE can question is left to it (the review, 2026-10-31: the global "questioning anyone" left
            // every bystander held, and for a minute after); one of its records while it questions is left too, looked at
            // again in 3 s, so once the questioning is over he is let go
            if (_bridge != null && _bridge.EnrageCanQuestion(p, me, 30f))
            {
                _aimEnrage++;
                Log.Write("AIM: " + name + " left to enRAGE's questioning");   // a man it can question is left to it at gunpoint
                return;
            }
            if (_bridge != null && record && _bridge.Interrogating)
            {
                _aimSeen[h] = new KeyValuePair<uint, int>(ENTITY.GET_ENTITY_MODEL(h), now - AimAgainMs + 3000);
                Log.Detail("AIM: " + name + " (one of enRAGE's people) left while enRAGE questions someone; looked at again in 3 s");
                return;
            }
            // our encounters' own: spawned by us or the test cast (Spawner's Ours holds the borrowed too: those are decided after
            // the releases, since a lawman's job or a wake act may hand them back below)
            bool borrowed = Spawner.IsBorrowed(h), spawned = Spawner.IsOurs(h) && !borrowed, cast = Spawner.IsCast(h);
            if (Natives.OtherScripts(h) && !(spawned || cast || borrowed))
                return;   // another script's (a mission, another mod): not ours to touch
            _aimLooked++;
            var what = new List<string>();
            var noted = new List<string>();   // ours, but kept on purpose: logged, never counted as a hold let go
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
                MindExitLetGo(_mindActs[i], "aimed at", false);   // still getting up: looked at once the gun's answer is over, put back if stuck
                _mindActs.RemoveAt(i);
                tasked = true;
            }
            if (_onlookBusy.Remove(h))
            {
                what.Add("onlooking");
                tasked = true;   // their stand-still or anim cleared too (not in a scenario: see below)
            }
            // their acts still to come (the hurry queued after a freeze) go with it (the review, 2026-10-31)
            if (_onlookQueue.RemoveAll(a => a.Handle == h) + _onlookPending.RemoveAll(a => a.Handle == h) > 0 && !what.Contains("onlooking"))
                what.Add("an onlooker's act to come");
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
            if (IsStaged(h))
            {
                string st = StagedAimLetGo(h, now, noted);
                if (st != null)
                {
                    what.Add(st);
                    tasked = true;   // the encounter's stand, turn or walk cleared
                }
            }
            string walk = WalkUpLetGo(h, noted);
            if (walk != null)
            {
                what.Add(walk);
                tasked = true;   // the walk over cleared
            }
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
            {
                what.Add("a rescue's thanks");
                _rescuedPed = 0;   // ended: unblocked below, the thanks not said
            }
            if (_wedding != null && (AimIs(_wedding.A, h) || AimIs(_wedding.B, h) || AimIs(_wedding.Preacher, h) || _wedding.Guests.Exists(g => AimIs(g, h))))
                noted.Add("a wedding");   // the wedding goes on its own way: noted, not ended
            // a lawman on a job of ours (a body to clear, a fight to break up): the job ended and he is handed straight back to
            // the game, unblocked and KEEP_TASK off, no wander given (the review, 2026-10-31: he carried on deaf to the gun)
            bool lawJob = false;
            if (_bodyClear != null && _bodyClear.Law == h)
            {
                BodyClearEnd(_bodyClear, "the lawman was aimed at", false);
                what.Add("a lawman's job (a body to clear)");
                lawJob = true;
            }
            for (int i = _brawlLaw.Count - 1; i >= 0; i--)
                if (_brawlLaw[i].Law == h)
                {
                    _brawlLaw.RemoveAt(i);
                    what.Add("a lawman's job (breaking up a fight)");
                    lawJob = true;
                }
            if (!lawJob && _lawGiveBack.Exists(kv => kv.Key == h))
            {
                what.Add("a lawman's job, done and waiting to be handed back");   // (the domestic's fetched lawman walking over, or a brawl's after the rope)
                lawJob = true;
            }
            if (lawJob)
            {
                _lawGiveBack.RemoveAll(kv => kv.Key == h);
                if (Spawner.IsBorrowed(h))
                    Spawner.GiveToGame(h);
                else if (ENTITY.DOES_ENTITY_EXIST(h))
                    PED.SET_PED_KEEP_TASK(h, false);
            }
            _wanderLater.RemoveAll(kv => kv.Key == h);   // a wander of ours due later would undo the game's answer
            if (_brawlOut.ContainsKey(h))
            {
                Log.Write("AIM: " + name + " is knocked out in a brawl; left down");
                return;
            }
            borrowed = Spawner.IsBorrowed(h);   // after the releases: a wake act or a lawman's job handed back is the game's now
            if (spawned || cast || borrowed)
            {
                Log.Write("AIM: " + name + " is held by an encounter of RealNPCs' (" + (cast ? "the test cast" : borrowed ? "borrowed" : "made by it") + ")" +
                          (what.Count > 0 ? ", and was held by " + string.Join(", ", what) : "") + (noted.Count > 0 ? "; also " + string.Join(", ", noted) : "") + "; left to it");
                return;
            }
            // the safety net: an ambient person's events let go whatever held them (no game read says whether they were blocked);
            // one of enRAGE's people only when a hold of ours had them, as enRAGE may block its own
            bool unblock = !record || what.Count > 0;
            if (unblock)
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            if (tasked && !PED.IS_PED_USING_ANY_SCENARIO(h))
                TASK.CLEAR_PED_TASKS(h, true, false);
            string kept = noted.Count > 0 ? "; kept: " + string.Join(", ", noted) : "";
            if (what.Count == 0)
            {
                _aimLastWhat = name + ": nothing";
                Log.Write("AIM: " + name + ": nothing of ours held them" + (unblock ? " (events unblocked)" : "") + kept);
                return;
            }
            _aimLetGo++;
            _aimSeen[h] = new KeyValuePair<uint, int>(ENTITY.GET_ENTITY_MODEL(h), now - AimAgainMs + 5000);   // something held them: looked at again in 5 s, so one taken up again meanwhile is let go too
            _aimLastWhat = name + ": " + string.Join(", ", what);
            Log.Write("AIM: " + name + " was held by " + string.Join(", ", what) + "; let go to the game" + (unblock ? " (events unblocked)" : "") + kept);
        }

        /// A walk-up of ours to Arthur ended for one aimed at (the review, 2026-10-31: none ends on a gun by itself; the grudge
        /// walker said his line over the hands-up and walked off mid-robbery). No new task: the game's answer takes over. The
        /// pickpocket about to run and a theft's thief are kept (catching him still gets the money back), in `noted`.
        private string WalkUpLetGo(int h, List<string> noted)
        {
            string what = null;
            if (_grudgeWalk != null && _grudgeWalk.Ped != null && _grudgeWalk.Ped.Handle == h)
            {
                _grudgeWalk = null;
                what = "a grudge's walk-up to you";
            }
            if (_thanksWalk != null && _thanksWalk.Ped != null && _thanksWalk.Ped.Handle == h)
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                _thanksWalk = null;
                what = "a walk-up to thank you";
            }
            if (LonelyWaiting(h))
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                _lonelyWalk = null;
                what = "a lonely one's walk-up to you";
            }
            if (FavourWaiting(h))
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                _favourWalk = null;
                what = "a walk-up to ask you a favour";
            }
            if (WarnWaiting(h))
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                _warnWalk = null;
                what = "a walk-up to have words with you or warn you";
            }
            if (_kinWalk != null && _kinWalk.Ped != null && _kinWalk.Ped.Handle == h && !_kinWalk.Spawned)
            {
                _casts.Remove(h);
                _kinWalk = null;   // one made for the part is our encounter's own, left to it
                what = "a relative's walk-up to you";
            }
            if (_lifter != null && _lifter.Handle == h)
            {
                _lifter = null;   // never reached you: nothing taken
                what = "a pickpocket's walk into you";
            }
            if (_liftRun != null && _liftRun.Handle == h)
                noted.Add("a pickpocket about to run with your money");
            if (_theft != null && _theft.Thief != null && _theft.Thief.Handle == h)
                noted.Add("a theft (he is the thief: catching him gets it back)");
            return what;
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
