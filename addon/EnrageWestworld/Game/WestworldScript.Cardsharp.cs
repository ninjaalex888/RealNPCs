// The cardsharp run out of town, the game side (FOURTEENHOUR reserve R6; Core/Cardsharp.cs; [Scenes] CardsharpRunOut). On the card
// cheat's "shown the door" cue (the scene's cb:thrownout, WestworldScript.Life.cs): two seconds on, once the scene has let them go, up
// to two men of the table (the man he cheated first, then any man of the mind's within 5 m of him, sat ones got up Rockstar's way,
// ScenarioExit.SoftUp) walk him out: he walks to a spot past the town's core in the saloon's direction (Places.TownCore;
// 140 m at most), they follow at his heels (the go-to-entity walk, given again while he moves). One word at the door, one at the last houses;
// then they turn back and he walks on. Bad blood with each of them (a tie, "runout": -2), and the save keeps it: back in that town two
// days to two weeks on, near you, he thinks of it (once). Anyone fighting, fleeing, aimed at by you, a mission or the add-on stopping
// lets them all go. Rides on the thrown-out step: never a frame of its own.
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
        private sealed class RunOut
        {
            public Ped Cheat, Caller;
            public readonly List<Ped> Escorts = new List<Ped>();   // the table's men walking him
            public string CheatId, CheatName, Town, Seed;
            public Vector3 Edge;
            public int StartAt, Until, FollowAt;
            public bool Walking;
        }

        private RunOut _runOut;
        private int _runOuts, _runOutsDone, _runOutsBack, _soreLookAt;
        private string _runOutLast;

        /// The cheat being walked out, or one of the men walking him: busy for the other features (IsAcknowledging).
        private bool InRunOut(int h)
        {
            RunOut r = _runOut;
            if (r == null || !r.Walking)
                return false;
            if (r.Cheat != null && r.Cheat.Handle == h)
                return true;
            for (int i = 0; i < r.Escorts.Count; i++)   // a loop, not Any: asked every frame
                if (r.Escorts[i] != null && r.Escorts[i].Handle == h)
                    return true;
            return false;
        }

        /// From the thrown-out cue: the table may see him off; the walk starts once the scene has let them go. True if it will.
        private bool RunOutBegin(Ped cheat, Ped caller, bool test = false)
        {
            if (!Cardsharp.On || _runOut != null || _placeTown == null || cheat == null || caller == null || !cheat.Exists() || !caller.Exists())
                return false;
            Vector3 centre;
            float radius;
            if (!EnrageWestworld.InGame.Places.TownCore(_placeTown, out centre, out radius))
                return false;
            Vector3 dir = cheat.Position - centre;
            dir.Z = 0f;
            dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
            Vector3 edge = centre + dir * (radius + Cardsharp.EdgePast);
            if (edge.DistanceTo(cheat.Position) > Cardsharp.MostWalk)
                edge = cheat.Position + dir * Cardsharp.MostWalk;   // a long way out: 140 m of it is enough to make the point
            int men = 1 + Peds.All().Count(p => p != null && p.Exists() && !p.IsDead && p.Handle != caller.Handle && p.Handle != cheat.Handle && PED.IS_PED_MALE(p.Handle) &&
                                                p.Position.DistanceTo(caller.Position) <= Cardsharp.TableMetres);
            MindWho cw = MindKnown(cheat.Handle, Game.GameTime);
            string seed = (cw != null && cw.Id != null ? cw.Id : cheat.Handle.ToString()) + "|" + _store.LastGameMin / 1440 + "|runout";
            if (!test && !Cardsharp.RunsOut(men, seed))
                return false;
            _runOut = new RunOut
            {
                Cheat = cheat, Caller = caller, CheatId = cw != null ? cw.Id : null, CheatName = cw != null ? cw.Name : FullNameOf(cheat), Town = _placeTown, Seed = seed, Edge = edge,
                StartAt = Game.GameTime + 2000, Until = Game.GameTime + 2000 + Cardsharp.WalkMs
            };
            _runOuts++;
            Log.Write("RUNOUT the table will see " + (_runOut.CheatName ?? "the card cheat") + " out past the last houses of " + _placeTown + " (" + (int)edge.DistanceTo(cheat.Position) + " m)");
            return true;
        }

        /// From the thrown-out step: the walk out, a look every half second; and the sore return, a look every 5 s.
        private void RunOutStep(int now)
        {
            SoreLook(now);
            RunOut r = _runOut;
            if (r == null || now < r.StartAt)
                return;
            Ped me = Game.Player.Character;
            if (!r.Walking)
            {
                if (_director.Involves(r.Cheat.Handle) || _director.Involves(r.Caller.Handle))
                {
                    if (now - r.StartAt > 6000)
                        RunOutEnd("the scene held them too long", false);
                    else
                        r.StartAt = now + 500;   // the scene's still letting them go
                    return;
                }
                // the table's men: the man he cheated first, then those at his elbow (sat ones up Rockstar's way)
                foreach (Ped p in new[] { r.Caller }.Concat(Peds.All().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != r.Caller.Handle && x.Handle != r.Cheat.Handle && PED.IS_PED_MALE(x.Handle) &&
                                                                                      PED.IS_PED_HUMAN(x.Handle) && x.Position.DistanceTo(r.Caller.Position) <= Cardsharp.TableMetres)
                                                                          .OrderBy(x => x.Position.DistanceTo(r.Caller.Position))))
                {
                    if (r.Escorts.Count >= Cardsharp.MostMen)
                        break;
                    if (!p.Exists() || p.IsDead || NotFree(p.Handle) || _director.Involves(p.Handle) || MayDirectWhyNot(p) != null || PED.IS_PED_IN_COMBAT(p.Handle, 0) ||
                        PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, p.Handle) || Spawner.IsOurs(p.Handle))
                        continue;
                    r.Escorts.Add(p);
                }
                if (r.Escorts.Count == 0 || !r.Cheat.Exists() || r.Cheat.IsDead)
                {
                    RunOutEnd("nobody at the table was free to see him off", false);
                    return;
                }
                int ch = r.Cheat.Handle;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(ch, true);
                TASK.CLEAR_PED_TASKS(ch, true, false);
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(ch, r.Edge.X, r.Edge.Y, r.Edge.Z, 1f, -1, 2f, 0, 0f);   // a walk: they don't run a man out, they walk him
                foreach (Ped m in r.Escorts)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(m.Handle, true);
                    ScenarioExit.SoftUp(m.Handle, x => TASK.TASK_GO_TO_ENTITY(x, ch, -1, 2.2f, 1.2f, 0f, 0));   // up from the table, Rockstar's way, and after him
                }
                if (r.Escorts[0].Position.DistanceTo(me.Position) < 30f)
                {
                    SpeechCaption(Person.FirstOf(NameToShow(r.Escorts[0].Handle)), Cardsharp.AtDoor(r.Seed), r.Escorts[0].Handle, 3500, false, false);
                    SpeechCaption(Person.FirstOf(r.CheatName ?? "The card cheat"), Cardsharp.CheatGoes(r.Seed), ch, 3500, false, false);
                }
                r.Walking = true;
                r.FollowAt = now + 1500;
                return;
            }
            bool cheatOk = r.Cheat.Exists() && !r.Cheat.IsDead && !PED.IS_PED_IN_COMBAT(r.Cheat.Handle, 0) && !PED.IS_PED_FLEEING(r.Cheat.Handle) && !_director.Involves(r.Cheat.Handle);
            if (!cheatOk || MISC.GET_MISSION_FLAG() || PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, r.Cheat.Handle) || r.Escorts.Any(m => !m.Exists() || m.IsDead || PED.IS_PED_IN_COMBAT(m.Handle, 0) || _director.Involves(m.Handle)))
            {
                RunOutEnd("something else came between them", false);
                return;
            }
            bool there = r.Cheat.Position.DistanceTo(r.Edge) < 6f;
            if (!there && now < r.Until)
            {
                if (now >= r.FollowAt)
                {
                    r.FollowAt = now + 1500;
                    foreach (Ped m in r.Escorts)
                        if (m.Position.DistanceTo(r.Cheat.Position) > 3.5f && !PED.IS_PED_USING_ANY_SCENARIO(m.Handle))
                            TASK.TASK_GO_TO_ENTITY(m.Handle, r.Cheat.Handle, -1, 2.2f, 1.2f, 0f, 0);   // at his heels, given again while he walks
                }
                return;
            }
            // out past the last houses: a word each, they turn back, he walks on
            if (r.Escorts[0].Position.DistanceTo(me.Position) < 35f || r.Cheat.Position.DistanceTo(me.Position) < 35f)
            {
                SpeechCaption(Person.FirstOf(NameToShow(r.Escorts[0].Handle)), Cardsharp.AtEdge(r.Seed), r.Escorts[0].Handle, 3500, false, false);
                SpeechCaption(Person.FirstOf(r.CheatName ?? "The card cheat"), Cardsharp.Retort(r.Seed), r.Cheat.Handle, 3500, false, false);
            }
            RunOutEnd(there ? "walked out past the last houses" : "walked most of the way out", true);
        }

        private void RunOutEnd(string why, bool walked)
        {
            RunOut r = _runOut;
            _runOut = null;
            if (r == null)
                return;
            int now = Game.GameTime;
            foreach (Ped m in r.Escorts)
                if (m.Exists() && !m.IsDead)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(m.Handle, false);
                    if (!PED.IS_PED_IN_COMBAT(m.Handle, 0) && !_director.Involves(m.Handle))
                        WanderLater(m.Handle, now + 1500);   // back to town
                }
            if (r.Cheat != null && r.Cheat.Exists() && !r.Cheat.IsDead)
            {
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Cheat.Handle, false);
                if (!PED.IS_PED_IN_COMBAT(r.Cheat.Handle, 0) && !_director.Involves(r.Cheat.Handle))
                    WanderLater(r.Cheat.Handle, now + 2500);   // on he goes
            }
            if (walked && r.CheatId != null && _store != null)
            {
                long g = _store.LastGameMin;
                foreach (Ped m in r.Escorts)
                {
                    MindWho mw = m.Exists() ? MindKnown(m.Handle, now) : null;
                    if (mw != null && mw.Id != null)
                        Ties.Note(_store.Ties, mw.Id, mw.Name, r.CheatId, r.CheatName, r.Town, Cardsharp.Kind, g, mw.Id);   // bad blood: they did it to him
                }
                _store.Counts["runout|" + r.CheatId + "|" + r.Town] = (int)(g / 1440);   // he'll be back, sore (SoreLook)
                _runOutsDone++;
            }
            _runOutLast = (r.CheatName ?? "the card cheat") + ": " + why;
            Log.Write("RUNOUT " + _runOutLast + " (" + r.Escorts.Count + " of the table's men)");
        }

        /// Back in the town he was walked out of, two days to two weeks on, near you: he thinks of it, once.
        private void SoreLook(int now)
        {
            if (!Cardsharp.On || now < _soreLookAt || _store == null || _placeTown == null)
                return;
            _soreLookAt = now + 5000;
            long day = _store.LastGameMin / 1440;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null)
                    continue;
                string key = "runout|" + w.Id + "|" + _placeTown;
                int was;
                if (!_store.Counts.TryGetValue(key, out was))
                    continue;
                if (day - was > Cardsharp.BackMostDays)
                {
                    _store.Counts.Remove(key);   // too long: let it lie
                    continue;
                }
                if (day - was < Cardsharp.BackLeastDays || !ENTITY.DOES_ENTITY_EXIST(h) || !ENTITY.IS_ENTITY_ON_SCREEN(h))
                    continue;
                if (!_ticker.Aside(Person.FirstOf(w.Name ?? "The card cheat") + ", thinking", Cardsharp.Sore(_placeTown, w.Id + "|" + day), "sore|" + w.Id, h, 8000, now))
                    continue;
                _store.Counts.Remove(key);
                _runOutsBack++;
                Log.Write("RUNOUT " + w.Name + " is back in " + _placeTown + ", " + (day - was) + " days on, sore");
                return;
            }
        }

        /// The Testing row: the nearer named man as the cheat, the other as the man he cheated (and the table round him).
        private string RunOutFire(int now, out string said)
        {
            said = null;
            if (!Cardsharp.On)
                return "off ([Scenes] CardsharpRunOut)";
            if (_runOut != null)
                return "a cheat is being walked out already";
            if (_placeTown == null)
                return "it wants a town";
            Ped me = Game.Player.Character;
            List<Candidate> men = ReasonCandidates(me, now, true).Where(c => !c.Who.Woman).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).Take(2).ToList();
            if (men.Count < 2)
                return "fewer than two named men near you";
            if (!RunOutBegin(men[0].Ped, men[1].Ped, true))
                return "this town's bounds aren't known (one of the nine towns only)";
            _runOut.StartAt = now + 500;
            said = men[1].Who.Name + " and the table walk " + men[0].Who.Name + " out past the last houses";
            return null;
        }

        /// F7: the card table's run-outs this session.
        private string RunOutStatus()
        {
            if (!Cardsharp.On)
                return "off ([Scenes] CardsharpRunOut)";
            return _runOuts + " times the table rose to see a cheat off (" + _runOutsDone + " walked out of town), " + _runOutsBack + " came back sore" + (_runOutLast != null ? "; last: " + _runOutLast : "");
        }
    }
}
