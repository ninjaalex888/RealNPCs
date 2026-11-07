// The barn prize fight, staged (FIVEDAY item 10; Core/PrizeFight.cs, Core/Engine.PrizeFight.cs). From nine at night in a
// town where the word went round, out back of the saloon he last came out of (else a spot unseen 20-40 m off): the two
// fighters stripped of anything but their fists, a ring of six men facing in, a lookout a few steps off watching the
// street. The lookout's word, the rules, then the bout: the two set on each other with the game's own unarmed combat
// (TASK_COMBAT_PED), the ring shouting; it ends when one is down to half his health or after forty seconds (the worse off
// loses), both told to stop, the purse called, the loser's word. The staged result is the result (the engine's pick only
// for a fight nobody saw). J while it runs: a dollar on either man (paid two to one), tip off the sheriff (twenty-five
// seconds later the lookout cries the law and they scatter; nobody there knows who told), or watch. The dance's rails:
// nothing spawned in view, never in missions, shots or a gun drawn break it up and the town remembers who fired; all of
// of them released to the game afterwards. Switches: [World] TownEvents, EventPrizeFights.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int FightMostMs = 40000;
        private const int FightTipMs = 25000;
        private int _fightsStaged, _fightsBet, _fightsTipped, _fightsWonBet;
        private string _fightLast;

        private void BeginPrizeFight(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at;
            Vector3 door;
            if (_saloonDoor.TryGetValue(e.Town, out door) && door.DistanceTo(me.Position) < 120f)
            {
                Vector3 spot;
                // behind the saloon: 14 m past the door on the far side from the player, out of his sight
                Vector3 away = door - me.Position;
                float len = Math.Max(1f, (float)Math.Sqrt(away.X * away.X + away.Y * away.Y));
                Vector3 back = door + new Vector3(away.X / len * 14f, away.Y / len * 14f, 0f);
                if (!Natives.StandSpot(back, door.Z, out spot) || Natives.Looking(spot + new Vector3(0f, 0f, 1f), 50f))
                {
                    if (!EncounterSpot(me, 20f, 40f, out at))
                        return;
                }
                else
                    at = spot;
            }
            else if (!EncounterSpot(me, 20f, 40f, out at))
                return;   // nowhere unseen just now: again in two seconds
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 3);   // the lookout (the run's first place)
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 4);
            run.WomanModel = Spawner.ModelFor(e.Town, false, seed + 5);    // a second man's model for the ring, so it isn't one face
            _eventRun = run;
            _eventsStaged++;
            _fightsStaged++;
            Log.Write("EVENT prizefight " + e.Town + ": staging " + e.AName + " against " + e.BName + " " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void PrizeFightStep(EventRun r, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            TownEvent e = r.E;
            if (!r.Ready)
            {
                if (!EventCastReady(r))
                {
                    if (now - r.Since > 15000)
                        EventRunLost(r, now, "the game never streamed the bodies in");
                    return;
                }
                if (!Spawner.SafeToSpawn())
                    return;
                Vector3 c = r.At;
                r.Fiddler = Spawner.Spawn(r.FiddlerModel, c + new Vector3(0f, 9f, 0f), 0f);   // the lookout, facing the street
                if (r.Fiddler == null)
                {
                    EventRunLost(r, now, "the lookout never appeared");
                    return;
                }
                r.Cast.Add(r.Fiddler);
                Ped a = Spawner.Spawn(r.ManModel, c + new Vector3(-1.6f, 0f, 0f), 90f), b = Spawner.Spawn(r.WomanModel, c + new Vector3(1.6f, 0f, 0f), 270f);
                if (a == null || b == null)
                {
                    foreach (Ped p in new[] { a, b })
                        if (p != null) Spawner.Release(p, true);
                    EventRunLost(r, now, "the fighters never appeared");
                    return;
                }
                r.Performers.Add(a);
                r.Performers.Add(b);
                r.Cast.Add(a);
                r.Cast.Add(b);
                foreach (Ped f in r.Performers)
                {
                    _fighterStart[f.Handle] = ENTITY.GET_ENTITY_HEALTH(f.Handle);   // their own health is the bout's measure
                    WEAPON.REMOVE_ALL_PED_WEAPONS(f.Handle, true, true);   // fists only
                    WEAPON.SET_CURRENT_PED_WEAPON(f.Handle, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                }
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(a.Handle, b.Handle, 60000, 0f, 0f, 0f);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(b.Handle, a.Handle, 60000, 0f, 0f, 0f);
                for (int i = 0; i < 6; i++)
                {
                    double ang = i * Math.PI / 3 + 0.3;
                    Ped p = Spawner.Spawn(i % 2 == 0 ? r.ManModel : r.WomanModel, c + new Vector3((float)Math.Cos(ang) * 4.6f, (float)Math.Sin(ang) * 4.6f, 0f), (float)(ang * 180 / Math.PI + 180));
                    if (p == null)
                        continue;
                    r.Cast.Add(p);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, a.Handle, 120000, 0f, 0f, 0f);
                }
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    if (!r.Performers.Contains(p))
                        r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);   // the ring and the lookout: harm to them breaks it up
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT prizefight " + e.Town + ": the ring set; " + (r.Cast.Count - 3) + " in the ring, a lookout");
                return;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
                    _eventRun = null;
                    _eventsNext = now + 60000;
                }
                return;
            }
            if (_eventPrompt == PrizeFight.Kind && now > _eventPromptUntil)
                PromptClear();
            // the fight itself takes health: only shots, a gun drawn or harm to the ring break it up (the fighters' own blows don't)
            bool byYou = false;
            string broke = now - r.SpawnedAt < 1500 ? null : PrizeFightBroken(r, out byYou);
            if (broke != null)
            {
                PromptClear();
                BreakEventRun(r, byYou, broke, now, g);
                return;
            }
            float d = me.Position.DistanceTo(r.At);
            if (d > 120f || (g >= e.Until && r.Step < 9))
            {
                PromptClear();
                PrizeFightEnd(r, now, g, now - r.Since >= 90000, d > 120f ? "you rode off" : "the hour passed");
                return;
            }
            if (r.Step >= 1 && r.Step <= 7 && d < 25f && _eventPrompt == null)
                PromptSet(PrizeFight.Kind, e, 12000);
            // the law, after a tip-off
            if (r.Grumbled && now - r.Looked >= FightTipMs && r.Step < 8)   // (a bout already over isn't raided: the Part 3 review)
            {
                SpeechCaption("The lookout", PrizeFight.RaidLine(e.Id), r.Fiddler.Handle, 4000, false, false);
                Speech.SayAny(r.Fiddler.Handle, Speech.Scared, 0);
                foreach (Ped p in r.Cast)
                    if (ENTITY.DOES_ENTITY_EXIST(p.Handle) && !ENTITY.IS_ENTITY_DEAD(p.Handle))
                    {
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                        TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                        TASK.TASK_SMART_FLEE_PED(p.Handle, r.Fiddler.Handle, 60f, 20000, 0, 3f, 0);
                    }
                PromptClear();
                Log.Write("EVENT prizefight " + e.Town + ": the law came (your tip); they scattered");
                PrizeFightEnd(r, now, g, true, "raided");
                return;
            }
            // the bout: over at half health or forty seconds
            if (r.Step == 3 && r.Performers.Count == 2)
            {
                Ped a = r.Performers[0], b = r.Performers[1];
                float ha = HealthShare(r, a), hb = HealthShare(r, b);
                if (ha <= 0.5f || hb <= 0.5f || now - r.FightAt > FightMostMs || IsDeadOrGone(a) || IsDeadOrGone(b))
                {
                    int w = IsDeadOrGone(a) ? 1 : IsDeadOrGone(b) ? 0 : ha >= hb ? 0 : 1;
                    e.Count2 = w + 1;   // the fight as it went (the engine's pick only for one nobody saw)
                    foreach (Ped f in r.Performers)
                        if (!IsDeadOrGone(f))
                            TASK.CLEAR_PED_TASKS(f.Handle, true, false);
                    Ped won = w == 0 ? a : b, lost = w == 0 ? b : a;
                    if (!IsDeadOrGone(won))
                        Speech.SayAny(won.Handle, Speech.Cheer, 0);
                    SpeechCaption("The lookout", PrizeFight.WonLine(e, w, e.Id), r.Fiddler.Handle, 4500, false, false);
                    if (!IsDeadOrGone(lost))
                        SpeechCaption(Person.FirstOf(w == 0 ? e.BName : e.AName) ?? "The loser", PrizeFight.LostLine(e, w, e.Id), lost.Handle, 4000, false, false);
                    r.Step = 8;
                    r.NextCall = now + 7000;
                    PromptClear();   // the hat's closed: no bet on a known winner (the Part 3 review)
                    Log.Write("EVENT prizefight " + e.Town + ": " + (w == 0 ? e.AName : e.BName) + " won (" + (int)(ha * 100) + "% / " + (int)(hb * 100) + "% left)");
                }
                else if (now >= r.NextCall && d < 50f)
                {
                    r.NextCall = now + 6000 + _rng.Next(1500);   // a shout every six or seven seconds while they fight
                    Ped crowd = r.Cast.Where(p => !r.Performers.Contains(p) && p != r.Fiddler && !IsDeadOrGone(p)).OrderBy(p => _rng.Next()).FirstOrDefault() ?? r.Fiddler;   // the ring only (the Part 3 review: the fighters move in the list)
                    Speech.SayAny(crowd.Handle, Speech.Cheer, a.Handle);
                    SpeechCaption("Someone in the ring", PrizeFight.RingLine(e, e.Id + "|" + now / 6000), crowd.Handle, 3500, false, false);
                }
                return;
            }
            if (now < r.NextCall)
                return;
            r.NextCall = now + 7000;
            switch (r.Step)
            {
                case 0:
                case 1:
                    SpeechCaption("The lookout", PrizeFight.LookoutLine(e.Id + "|" + r.Step), r.Fiddler.Handle, 5500, false, false);
                    break;
                case 2:
                    {
                        Ped a = r.Performers[0], b = r.Performers[1];
                        TASK.TASK_COMBAT_PED(a.Handle, b.Handle, 0, 16);
                        TASK.TASK_COMBAT_PED(b.Handle, a.Handle, 0, 16);
                        Speech.SayAny(a.Handle, Speech.Threat, b.Handle);
                        r.Step = 3;
                        r.FightAt = now;   // the bout's clock (FightMostMs from here)
                        r.NextCall = now + 4000;
                        Log.Write("EVENT prizefight " + e.Town + ": the bout begins");
                        return;
                    }
                default:
                    PromptClear();
                    PrizeFightEnd(r, now, g, true, "over");
                    return;
            }
            r.Step++;
        }

        private float HealthShare(EventRun r, Ped p)
        {
            int start;
            if (p == null || !ENTITY.DOES_ENTITY_EXIST(p.Handle) || !_fighterStart.TryGetValue(p.Handle, out start) || start <= 0)
                return 0f;
            return ENTITY.GET_ENTITY_HEALTH(p.Handle) / (float)start;
        }

        private static bool IsDeadOrGone(Ped p)
        {
            return p == null || !ENTITY.DOES_ENTITY_EXIST(p.Handle) || ENTITY.IS_ENTITY_DEAD(p.Handle);
        }

        /// Shots, a gun drawn, or harm to anyone but the two fighters: EventBroken's own reading with the fighters left out
        /// (they're in combat and losing health by design).
        private string PrizeFightBroken(EventRun r, out bool byYou)
        {
            var fighters = new List<Ped>(r.Performers);
            r.Cast.RemoveAll(fighters.Contains);
            try
            {
                return EventBroken(r, out byYou);
            }
            finally
            {
                r.Cast.AddRange(fighters);
            }
        }

        private readonly Dictionary<int, int> _fighterStart = new Dictionary<int, int>();   // a fighter's health as the ring was set

        /// It's over: the engine's end (the tie, the memories, the morning's word), his bet paid, the ring sent off.
        private void PrizeFightEnd(EventRun r, int now, long g, bool stayed, string why)
        {
            TownEvent e = r.E;
            _engine.FinishEvent(e, stayed, g);
            if (stayed)
                _eventsAttended++;
            string part = e.Part ?? "";
            if (!part.Contains("tipped") && e.Count2 > 0 && (part.Contains("bet:A") && e.Count2 == 1 || part.Contains("bet:B") && e.Count2 == 2))
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(PrizeFight.StakeCents * 2, 0);
                _fightsWonBet++;
                Hint("Your man won. Two dollars back in your hand.", 3500);
            }
            else if (part.Contains("bet:") && !part.Contains("tipped"))
                Hint("Your dollar stays in the hat.", 3000);
            else if (part.Contains("bet:"))
                Hint("Your dollar went with the hat when the law came.", 3000);
            foreach (Ped f in r.Performers)
                if (f != null)
                    _fighterStart.Remove(f.Handle);
            foreach (Ped p in r.Cast)
                WanderLater(p.Handle, now + 1500 + _rng.Next(6000));
            _fightLast = e.Town + ": " + e.Outcome;
            Log.Write("EVENT prizefight " + e.Town + ": " + why + "; " + e.Outcome);
            r.ReleaseAt = now + 12000;
        }

        /// What he chose at the fight (the J menu, WestworldScript.Trials.cs).
        private void PrizeFightChoice(EventRun r, TownEvent e, string what, int now)
        {
            if (r == null || r.E != e)
                return;
            if (what == "betA" || what == "betB")
            {
                if (r.Step >= 3)
                {
                    Hint("The hat's closed once they're at it.", 2500);   // no bets once the bout is on (the Part 3 review)
                    return;
                }
                int cash = MONEY._MONEY_GET_CASH_BALANCE();
                if (cash < PrizeFight.StakeCents)
                {
                    Hint("You haven't a dollar on you.", 2500);
                    return;
                }
                MONEY._MONEY_DECREMENT_CASH_BALANCE(PrizeFight.StakeCents);
                _engine.PrizeFightPart(e, what == "betA" ? "bet:A" : "bet:B");
                _fightsBet++;
                string man = Person.FirstOf(what == "betA" ? e.AName : e.BName) ?? "him";
                SpeechCaption("The lookout", "A dollar on " + man + ". It's in the hat, mister.", r.Fiddler.Handle, 3500, true, false);
            }
            else if (what == "tip")
            {
                _engine.PrizeFightPart(e, "tipped");
                _fightsTipped++;
                r.Grumbled = true;   // the law comes FightTipMs from now
                r.Looked = now;
                Hint("You slip a word to the sheriff's man on the corner.", 3500);
            }
        }

        private string PrizeFightStatus()
        {
            if (!_cfg.EventPrizeFights)
                return "off ([World] EventPrizeFights)";
            return _fightsStaged + " staged this session, " + _fightsBet + " bets (" + _fightsWonBet + " won), " + _fightsTipped + " tip-offs" + (_fightLast != null ? "; the last: " + _fightLast : "");
        }
    }
}
