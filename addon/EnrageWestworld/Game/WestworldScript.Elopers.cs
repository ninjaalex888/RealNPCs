// A couple running off, the game side (FOURTEENHOUR reserve R18; Core/Elopers.cs; [Encounters] Elopers). In a town by day, out of doors
// and on foot, once a day at most (a look a minute, one in twelve or so): a young woman and a young man made unseen 30-50 m off come
// to you at a run. Her plea and J: hide them (they slip 10 m behind you and keep down), send them on (they run on the way they were
// going), or give them up (they wait). Fifty seconds on her father and her brother come the same way; J then: the wrong way (they run
// on past; the pair get away) or the right one (they run the pair down: she's walked home between them, he runs). Nobody is hurt.
// The town's word next day. Everyone made is given back once it's over. A look a quarter second while it runs.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class ElopeRun
        {
            public Ped Her, Him;
            public readonly List<Ped> Pursuers = new List<Ped>();   // her father and her brother
            public string Seed, Town, Choice, Word, Phase = "spawn";   // spawn, come, plea, wait, men, ask, after
            public uint HerModel, HimModel, MenModel;
            public Vector3 From, Away, Hide;
            public bool Down;   // hidden and keeping down
            public int Started, PhaseAt, MenAt, StepAt, Tries;
            public bool Test, Caught;
        }

        private ElopeRun _elope;
        private int _elopeLookAt, _elopes, _elopeAway, _elopeCaught;
        private string _elopeLast;

        private void ElopeStep(int now)
        {
            if (_elope != null)
            {
                if (now >= _elope.StepAt)
                {
                    _elope.StepAt = now + 250;
                    ElopeRunStep(_elope, now);
                }
                return;
            }
            if (!Elopers.On || now < _elopeLookAt || _store == null || _placeTown == null || _interiorType != 0)
                return;
            _elopeLookAt = now + 60000;
            if (MISC.GET_MISSION_FLAG() || !Elopers.Hour(CLOCK.GET_CLOCK_HOURS()) || PED.IS_PED_ON_MOUNT(Game.Player.Character.Handle) || _director.Busy)
                return;
            string key = "elope|" + _placeTown + "|" + _store.LastGameMin / 1440;
            if (_nightDone.Contains(key))
                return;
            string seed = "elope|" + _placeTown + "|" + _store.LastGameMin / 60;
            if (!Voice.Chance(seed, Elopers.Percent))
                return;
            _nightDone.Add(key);
            ElopeStart(now, false, seed);
        }

        private void ElopeStart(int now, bool test, string seed)
        {
            _elope = new ElopeRun { Seed = seed, Town = _placeTown, Started = now, PhaseAt = now, Test = test, HerModel = Spawner.ModelFor(_placeTown, true, Voice.Hash(seed + "|her")),
                                    HimModel = Spawner.ModelFor(_placeTown, false, Voice.Hash(seed + "|him")), MenModel = Spawner.ModelFor(_placeTown, false, Voice.Hash(seed + "|men")) };
            Spawner.Ready(_elope.HerModel);
            Spawner.Ready(_elope.HimModel);
            Spawner.Ready(_elope.MenModel);
            _elopes++;
            Log.Write("ELOPE a couple running off, in " + _placeTown + (test ? " (a test)" : ""));
        }

        private void ElopeRunStep(ElopeRun r, int now)
        {
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                ElopeEnd("a mission, or you");
                return;
            }
            if (ElopeFight(r, me))
            {
                ElopeEnd("a fight, or one of them down: they're the game's now");   // let go at once (the rule): no order after it
                return;
            }
            if (r.Phase == "spawn")
            {
                if (now - r.Started > 20000 || r.Tries > 10)
                {
                    ElopeEnd("nowhere to make them");
                    return;
                }
                if (!Spawner.Ready(r.HerModel) || !Spawner.Ready(r.HimModel))
                    return;
                Vector3 at;
                if (!EncounterSpot(me, 30f, 50f, out at))
                {
                    r.Tries++;
                    return;
                }
                Ped her = Spawner.Spawn(r.HerModel, at, 0f), him = Spawner.Spawn(r.HimModel, at + new Vector3(1.2f, 0f, 0f), 0f);
                if (her == null || him == null)
                {
                    if (her != null) Spawner.Release(her, true);
                    if (him != null) Spawner.Release(him, true);
                    r.Tries++;
                    return;
                }
                r.Her = her;
                r.Him = him;
                r.From = at;
                Vector3 dir = me.Position - at;
                dir.Z = 0f;
                r.Away = me.Position + (Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized) * 70f;   // on past you, the way they were going
                foreach (Ped p in new[] { her, him })
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    PED.SET_PED_KEEP_TASK(p.Handle, true);
                    WEAPON.REMOVE_ALL_PED_WEAPONS(p.Handle, true, true);
                    TASK.TASK_GO_TO_ENTITY(p.Handle, me.Handle, -1, 2.5f, 2.6f, 0f, 0);   // at a run
                    _encounterPeds.Add(p.Handle);
                }
                r.Phase = "come";
                r.PhaseAt = now;
                return;
            }
            if (r.Her == null || !r.Her.Exists() || r.Her.IsDead || r.Him == null || !r.Him.Exists() || r.Him.IsDead)
            {
                ElopeEnd("one of them was gone");
                return;
            }
            float d = r.Her.Position.DistanceTo(me.Position);
            switch (r.Phase)
            {
                case "come":
                    if (d <= 4f)
                    {
                        r.Phase = "plea";
                        r.PhaseAt = now;
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Her.Handle, me.Handle, 9000, 0f, 0f, 0f);
                        SpeechCaption("The young woman", Elopers.Plea(r.Seed), r.Her.Handle, 5000, true, true);
                        if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                            PromptSet(Elopers.Kind, new TownEvent { Kind = Elopers.Kind, Town = r.Town, Extra = "first" }, Elopers.PromptMs);
                    }
                    else if (now - r.PhaseAt > 40000)
                        ElopeEnd("they never reached you");
                    return;
                case "plea":
                    if (r.Choice == null && now - r.PhaseAt > Elopers.PromptMs + 2000 && !(_eventMenu != null && _eventMenu.Mode == Elopers.Kind))
                        ElopeChoose("elope:send");   // no word: they run for it
                    return;
                case "wait":
                    if (r.Choice == "hide" && !r.Down && r.Her.Position.DistanceTo(r.Hide) < 2.5f)
                    {
                        r.Down = true;
                        foreach (Ped p in new[] { r.Her, r.Him })
                            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(p.Handle, Director.Crouch, 120000, true, 0u, -1f, false);   // down out of sight (the night visitor's crouch)
                    }
                    if (now < r.MenAt)
                        return;
                    if (!Spawner.Ready(r.MenModel))
                        return;
                    Vector3 at;
                    if (!EncounterSpot(me, 30f, 50f, out at))
                        at = r.From;
                    for (int i = 0; i < 2; i++)
                    {
                        Ped m = Spawner.Spawn(r.MenModel, at + new Vector3(i * 1.4f, 0f, 0f), 0f);
                        if (m == null)
                            continue;
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(m.Handle, true);
                        PED.SET_PED_KEEP_TASK(m.Handle, true);
                        WEAPON.REMOVE_ALL_PED_WEAPONS(m.Handle, true, true);   // a father's anger, not a gunfight
                        TASK.TASK_GO_TO_ENTITY(m.Handle, me.Handle, -1, 3f, 2.6f, 0f, 0);
                        _encounterPeds.Add(m.Handle);
                        r.Pursuers.Add(m);
                    }
                    if (r.Pursuers.Count == 0)
                    {
                        ElopeOutcome(r, true, "nobody came after them");
                        return;
                    }
                    r.Phase = "men";
                    r.PhaseAt = now;
                    return;
                case "men":
                    if (r.Pursuers[0].Exists() && r.Pursuers[0].Position.DistanceTo(me.Position) <= 5f)
                    {
                        r.Phase = "ask";
                        r.PhaseAt = now;
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Pursuers[0].Handle, me.Handle, 9000, 0f, 0f, 0f);
                        SpeechCaption("Her father", Elopers.PursuerAsks(r.Seed), r.Pursuers[0].Handle, 5000, true, true);
                        if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                            PromptSet(Elopers.Kind, new TownEvent { Kind = Elopers.Kind, Town = r.Town, Extra = r.Choice == "give" ? "gaveup" : "men" }, Elopers.PromptMs);
                    }
                    else if (now - r.PhaseAt > 45000)
                        ElopeOutcome(r, true, "they never caught up");
                    return;
                case "ask":
                    if (r.Word == null && now - r.PhaseAt > Elopers.PromptMs + 2000 && !(_eventMenu != null && _eventMenu.Mode == Elopers.Kind))
                        ElopeChoose(r.Choice == "give" ? "elope:there" : "elope:wrong");   // no word: given up stays given up; else they go on past
                    return;
                case "after":
                    if (r.Caught && r.Pursuers.Count > 0 && r.Pursuers[0].Exists() && r.Her.Position.DistanceTo(r.Pursuers[0].Position) < 3f && !r.Test)
                    {
                        // run down: she's walked home between them; he runs
                        Vector3 home = r.From;
                        ScenarioExit.SoftUp(r.Her.Handle, x => TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(x, home.X, home.Y, home.Z, 1f, -1, 1.5f, 0, 0f));   // up from where she hid, Rockstar's way, and home
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(r.Pursuers[0].Handle, home.X, home.Y, home.Z, 1f, -1, 1.5f, 0, 0f);
                        r.Caught = false;
                    }
                    if (now - r.PhaseAt > 25000)
                        ElopeEnd(_elopeLast ?? "over");
                    return;
            }
        }

        /// J's rows: the first (hide, send, give), then the men's (wrong, right; or there, given up).
        private void ElopeChoose(string id)
        {
            ElopeRun r = _elope;
            if (r == null)
                return;
            if (_eventPrompt == Elopers.Kind)
                PromptClear();
            Ped me = Game.Player.Character;
            if (r.Phase == "plea" && r.Choice == null)
            {
                r.Choice = id == "elope:hide" ? "hide" : id == "elope:give" ? "give" : "send";
                r.Phase = "wait";
                r.MenAt = Game.GameTime + (r.Test ? 15000 : Elopers.BehindMs);
                if (r.Choice == "hide")
                {
                    Vector3 back = me.Position - ENTITY.GET_ENTITY_FORWARD_VECTOR(me.Handle) * 10f;
                    r.Hide = back;
                    SpeechCaption("The young woman", Elopers.Hidden(r.Seed), r.Her.Handle, 3000, true, true);
                    foreach (Ped p in new[] { r.Her, r.Him })
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, back.X, back.Y, back.Z, 2.5f, 20000, 1f, 0, 0f);   // behind you, and down (the crouch once there)
                }
                else if (r.Choice == "send")
                {
                    SpeechCaption("The young woman", Elopers.SentOn(r.Seed), r.Her.Handle, 3000, true, true);
                    foreach (Ped p in new[] { r.Her, r.Him })
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, r.Away.X, r.Away.Y, r.Away.Z, 2.6f, -1, 2f, 0, 0f);   // on, at a run
                }
                else
                {
                    SpeechCaption("The young woman", Elopers.GivenUp(r.Seed), r.Her.Handle, 3500, true, true);
                    foreach (Ped p in new[] { r.Her, r.Him })
                        TASK.TASK_STAND_STILL(p.Handle, 120000);
                }
                Log.Write("ELOPE you " + (r.Choice == "hide" ? "hid them" : r.Choice == "send" ? "sent them on" : "made them wait"));
                return;
            }
            if (r.Phase == "ask" && r.Word == null)
            {
                r.Word = id == "elope:right" || id == "elope:there" ? "right" : "wrong";
                bool caught = r.Word == "right";
                SpeechCaption("Her father", caught ? Elopers.PursuerRight(r.Seed) : Elopers.PursuerWrong(r.Seed), r.Pursuers[0].Handle, 3500, true, true);
                if (caught)
                {
                    foreach (Ped m in r.Pursuers)
                        if (m.Exists())
                            TASK.TASK_GO_TO_ENTITY(m.Handle, r.Her.Handle, -1, 1.5f, 2.6f, 0f, 0);
                    int father = r.Pursuers[0].Handle;
                    ScenarioExit.SoftUp(r.Him.Handle, x => TASK.TASK_SMART_FLEE_PED(x, father, 120f, -1, 0, 3f, 0));   // he runs (up first, if he was down)
                    SpeechCaption("The young woman", Elopers.Caught(r.Seed), r.Her.Handle, 3500, true, true);
                    r.Caught = true;
                }
                else
                {
                    Vector3 off = r.Pursuers[0].Position + (r.Pursuers[0].Position - r.Away) * 0.6f;   // away from where they really went
                    foreach (Ped m in r.Pursuers)
                        if (m.Exists())
                            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Handle, off.X, off.Y, off.Z, 2.6f, -1, 2f, 0, 0f);
                }
                ElopeOutcome(r, !caught, r.Choice);
            }
        }

        private void ElopeOutcome(ElopeRun r, bool away, string how)
        {
            r.Phase = "after";
            r.PhaseAt = Game.GameTime;
            if (away)
                _elopeAway++;
            else
                _elopeCaught++;
            if (!r.Test && r.Town != null)
                Safe("elopers", () => _engine.WaysideHeard(Elopers.News(r.Town, away, how, r.Seed), r.Town, r.Seed + "|news", _store.LastGameMin));
            _elopeLast = away ? "they got away (" + how + ")" : "her father's men had her home; he ran";
            Log.Write("ELOPE " + _elopeLast);
        }

        private void ElopeEnd(string why)
        {
            ElopeRun r = _elope;
            _elope = null;
            if (r == null)
                return;
            if (_eventPrompt == Elopers.Kind)
                PromptClear();
            Ped me = Game.Player.Character;
            var all = new List<Ped>(r.Pursuers) { r.Her, r.Him };
            foreach (Ped p in all)
            {
                if (p == null || !p.Exists())
                    continue;
                _encounterPeds.Remove(p.Handle);
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && p.Position.DistanceTo(me.Position) < 120f;
                Spawner.Release(p, !p.IsDead && !seen && !PED.IS_PED_IN_COMBAT(p.Handle, me.Handle));   // the unseen vanish; one fighting you stays the game's
            }
            Log.Write("ELOPE over: " + why);
        }

        /// Any of the four brawling, struck by Arthur, or dead: the scene is over and the game has them back
        /// (the reserve's review: the run gave orders to whoever it held, fighting or not).
        private static bool ElopeFight(ElopeRun r, Ped me)
        {
            if (ElopeFighting(r.Her, me) || ElopeFighting(r.Him, me))
                return true;
            foreach (Ped p in r.Pursuers)
                if (ElopeFighting(p, me))
                    return true;
            return false;
        }

        private static bool ElopeFighting(Ped p, Ped me)
        {
            return p != null && p.Exists() && (p.IsDead || PED.IS_PED_IN_COMBAT(p.Handle, 0) || ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(p.Handle, me.Handle, true, true));
        }

        /// Busy for the other features (IsAcknowledging; a loop, asked every frame).
        private bool InElope(int h)
        {
            ElopeRun r = _elope;
            if (r == null)
                return false;
            if ((r.Her != null && r.Her.Handle == h) || (r.Him != null && r.Him.Handle == h))
                return true;
            for (int i = 0; i < r.Pursuers.Count; i++)
                if (r.Pursuers[i] != null && r.Pursuers[i].Handle == h)
                    return true;
            return false;
        }

        private string ElopeFire(int now, out string said)
        {
            said = null;
            if (!Elopers.On)
                return "off ([Encounters] Elopers)";
            if (_elope != null)
                return "a couple is running already";
            if (_placeTown == null)
                return "it wants a town";
            ElopeStart(now, true, "elope|test|" + now);
            said = "a couple comes running (her father's men 15 s behind them, for the test)";
            return null;
        }

        private string ElopersStatus()
        {
            if (!Elopers.On)
                return "off ([Encounters] Elopers)";
            return _elopes + " couples ran off near you (" + _elopeAway + " got away, " + _elopeCaught + " run down)" + (_elopeLast != null ? "; last: " + _elopeLast : "");
        }
    }
}
