// Riders, the game side (FOURTEENHOUR reserve R9; Core/Riders.cs; [Encounters] Riders). On the traveller's turn of the road's roll
// (half the time on a road), or from the Testing rows: a resident of the nearest town (riding along), or a lawman while a posse is out
// of it (the deputy), made unseen 60-90 m off (EncounterSpot), dressed as himself (the resident's outfit), and, when you're in the
// saddle, on a horse made under him (the allies' SetPedOntoMount: the one proven ride). He comes up to you by the game's go-to-entity
// (from the saddle it drives the horse), given again every second while he keeps near; the watchdog (StalkerRide's: 3 m in 5 s, or he
// gets down and walks) and a minute's limit to reach you. Beside you: his greeting and J; riding along, a word every 16 s (his worry,
// else his nature) for about a hundred seconds, then he turns off; "Ride on together": he rides on ahead to his town and knows you
// there. The deputy's question and J: your word is the posse's end (Engine.PosseBack). Then off, and given back to the game.
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
        private sealed class RiderRun
        {
            public Ped Man, Horse;
            public Resident R;
            public bool Deputy, Mounted, Test, Together, Greeted, Chose;
            public string Town, Name, Seed, Temper, Phase = "spawn";   // spawn, come, along, leave
            public uint Model, HorseModel;
            public int Started, PhaseAt, NextTalkAt, Talks, CheckAt, OrderAt, Tries;
            public Vector3 CheckFrom, To;
        }

        private RiderRun _rider;
        private int _riders, _ridersAlong, _ridersTogether, _riderDeputies, _riderPointed, _riderWalked;
        private string _riderLast;

        /// On the traveller's turn, or a test: a rider sets off toward you. False when none could be readied.
        private bool StartRider(int now, bool test, bool deputy)
        {
            if (!Riders.On || _rider != null || _store == null)
                return false;
            Ped me = Game.Player.Character;
            Vector3 p = me.Position;
            float metres, tx, ty;
            string town = Travellers.Nearest(p.X, p.Y, out metres, out tx, out ty);
            bool asDeputy = test ? deputy : town != null && _engine.PosseOut(town) && Voice.Chance("deputy|" + town + "|" + _store.LastGameMin / 60, Riders.DeputyPercent);
            var r = new RiderRun { Deputy = asDeputy, Test = test, Town = town, Seed = "rider|" + now, Started = now, PhaseAt = now, To = new Vector3(tx, ty, p.Z) };
            if (asDeputy)
            {
                r.Model = Natives.Hash("s_m_m_ambientlawrural_01");
                r.Name = "The deputy";
            }
            else
            {
                Resident res = town == null ? null : _store.Residents.Where(x => x != null && x.Regular && !x.Dead && !x.Fled && !x.Left && x.Held == null && x.Cast == null && !x.Woman && x.Town == town && x.Model != 0 &&
                                                                                 !_resBind.Values.Any(b => b.R == x))
                                                              .OrderBy(x => Voice.Hash(r.Seed + "|" + x.Id)).FirstOrDefault();
                if (res == null)
                    return false;   // nobody of that town free to ride out: the traveller's turn as before
                r.R = res;
                r.Model = res.Model;
                r.Name = res.Name;
                r.Temper = res.Personality;
            }
            r.Mounted = PED.IS_PED_ON_MOUNT(me.Handle);
            if (r.Mounted)
                r.HorseModel = Natives.Hash(Allies.HorseModel(r.Seed));
            _rider = r;
            Spawner.Ready(r.Model);   // asked for now; made once it's in
            if (r.Mounted)
                Spawner.Ready(r.HorseModel);
            Log.Write("RIDER begins: " + (asDeputy ? "a deputy, the posse out of " + town : r.Name + " of " + town) + (r.Mounted ? ", on horseback" : ", on foot (you're on foot)") + (test ? " (a test)" : ""));
            return true;
        }

        private void RiderStep(int now)
        {
            RiderRun r = _rider;
            if (r == null)
                return;
            Ped me = Game.Player.Character;
            if (r.Phase == "spawn")
            {
                if (now - r.Started > 20000 || r.Tries > 10)
                {
                    EndRider("nowhere to make him unseen");
                    return;
                }
                if (!Spawner.Ready(r.Model) || (r.Mounted && !Spawner.Ready(r.HorseModel)))
                    return;
                Vector3 at;
                if (!EncounterSpot(me, 60f, 90f, out at))
                {
                    r.Tries++;
                    return;
                }
                Ped m = Spawner.Spawn(r.Model, at, ENTITY.GET_ENTITY_HEADING(me.Handle));
                if (m == null)
                {
                    r.Tries++;
                    return;
                }
                int h = m.Handle;
                if (r.R != null)
                {
                    PED._EQUIP_META_PED_OUTFIT_PRESET(h, r.R.Outfit, false);   // himself, as his town knows him
                    PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                    Dressed.Note(h);
                    _resBind[h] = new ResBind { R = r.R, Model = r.R.Model };
                }
                _encounterPeds.Add(h);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                r.Man = m;
                if (r.Mounted)
                {
                    try
                    {
                        Ped horse = Spawner.Spawn(r.HorseModel, at, ENTITY.GET_ENTITY_HEADING(me.Handle));
                        if (horse != null)
                        {
                            Function.Call(SetPedOntoMount, h, horse.Handle, -1, true);   // the allies' ride (proven)
                            r.Horse = horse;
                        }
                        else
                            r.Mounted = false;
                    }
                    catch (Exception ex)
                    {
                        r.Mounted = false;
                        Log.Write("RIDER no horse (" + ex.Message + "): on foot");
                    }
                }
                RiderOrder(r, me, now);
                r.Phase = "come";
                r.PhaseAt = now;
                r.CheckAt = now + 5000;
                r.CheckFrom = m.Position;
                _riders++;
                return;
            }
            if (r.Man == null || !r.Man.Exists() || r.Man.IsDead || PED.IS_PED_IN_COMBAT(r.Man.Handle, 0) || PED.IS_PED_FLEEING(r.Man.Handle) || MISC.GET_MISSION_FLAG())
            {
                EndRider(r.Man != null && r.Man.Exists() && r.Man.IsDead ? "he died" : "something else took him");
                return;
            }
            int mh = r.Man.Handle;
            float d = r.Man.Position.DistanceTo(me.Position);
            switch (r.Phase)
            {
                case "come":
                    if (d <= Riders.NearMetres)
                    {
                        RiderGreet(r, me, now);
                        return;
                    }
                    if (now - r.PhaseAt > 60000 || d > 160f)
                    {
                        EndRider("never caught up with you");
                        return;
                    }
                    RiderWatch(r, now);
                    if (now >= r.OrderAt)
                        RiderOrder(r, me, now);
                    return;
                case "along":
                    if (d > 40f)
                    {
                        RiderLeave(r, me, now, "you rode on from him");
                        return;
                    }
                    if (now >= r.OrderAt)
                        RiderOrder(r, me, now);   // keeps near: the order again about once a second
                    RiderWatch(r, now);
                    if (r.Deputy)
                    {
                        if (r.Chose || (now - r.PhaseAt > Riders.PromptMs + 3000 && !(_eventMenu != null && _eventMenu.Mode == Riders.Kind)))
                            RiderLeave(r, me, now, r.Chose ? "had his answer" : "had no answer");
                        return;
                    }
                    if (now >= r.NextTalkAt && r.Talks < 3 && d < 15f)
                    {
                        r.NextTalkAt = now + Riders.TalkGapMs;
                        r.Talks++;
                        string line = RiderTalk(r);
                        if (line != null)
                            SpeechCaption(Person.FirstOf(r.Name), line, mh, Math.Min(7000, 1800 + line.Length * 55), true, true);
                    }
                    if (now - r.PhaseAt > Riders.AlongMs)
                        RiderLeave(r, me, now, "rode along a while, and turned off");
                    return;
                case "leave":
                    if (now - r.PhaseAt > 20000 || d > 120f)
                        EndRider(r.Together ? "rode on ahead to " + (r.Town ?? "town") : "turned off");
                    return;
            }
        }

        /// The go-to-entity at you (from the saddle it drives the horse), given again about once a second.
        private void RiderOrder(RiderRun r, Ped me, int now)
        {
            r.OrderAt = now + 1000;
            TASK.TASK_GO_TO_ENTITY(r.Man.Handle, me.Handle, -1, Riders.StopMetres, r.Mounted ? 2.5f : 1.3f, 0f, 0);
        }

        /// StalkerRide's watchdog: no 3 m in 5 s while still coming, and he's down off the horse and walking (the twin).
        private void RiderWatch(RiderRun r, int now)
        {
            if (now < r.CheckAt)
                return;
            float moved = r.Man.Position.DistanceTo(r.CheckFrom);
            r.CheckAt = now + StalkerRide.CheckMs;
            r.CheckFrom = r.Man.Position;
            if (r.Phase != "come" || !r.Mounted || moved >= StalkerRide.MovedMetres)
                return;
            r.Mounted = false;
            _riderWalked++;
            if (PED.IS_PED_ON_MOUNT(r.Man.Handle))
                Function.Call(TaskDismountAnimal, r.Man.Handle, 0, 0, 0, 0, 0);
            Log.Write("RIDER " + r.Name + "'s horse wouldn't take the order (" + moved.ToString("0.0") + " m in 5 s): he walks the rest");
        }

        private void RiderGreet(RiderRun r, Ped me, int now)
        {
            int h = r.Man.Handle;
            r.Phase = "along";
            r.PhaseAt = now;
            r.Greeted = true;
            r.NextTalkAt = now + 6000;
            string line = r.Deputy ? Riders.DeputyAsks(r.Town, r.Seed) : Riders.Greeting(r.Temper, r.Town, r.Seed);
            SpeechCaption(Person.FirstOf(r.Name), line, h, Math.Min(7000, 1800 + line.Length * 55), true, true);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 8000, 0, 51, 0);
            if (r.Deputy)
                _riderDeputies++;
            else
                _ridersAlong++;
            if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                PromptSet(Riders.Kind, new TownEvent { Kind = Riders.Kind, Town = r.Town, AName = r.Name, Extra = r.Deputy ? "deputy" : "along" }, Riders.PromptMs);
            Log.Write("RIDER " + r.Name + " beside you: " + line);
        }

        /// What he says riding along: his worry (the mind's own words for it), else a word by his nature.
        private string RiderTalk(RiderRun r)
        {
            if (r.R != null)
            {
                Person pp = _engine.PersonOfResident(r.R);
                string said = pp != null ? InnerLife.WorrySaid(pp, _store.LastGameMin / 1440, r.Seed + "|" + r.Talks) : null;
                if (said != null && r.Talks == 1)
                    return said;
            }
            return Riders.Talk(r.Temper, r.Seed + "|" + r.Talks);
        }

        /// J's row.
        private void RiderChoose(string id)
        {
            RiderRun r = _rider;
            if (r == null || r.Man == null || !r.Man.Exists() || r.Phase != "along")
                return;
            Ped me = Game.Player.Character;
            int h = r.Man.Handle;
            if (r.Deputy)
            {
                string how = id == "rider:right" ? "right" : id == "rider:wrong" ? "wrong" : "none";
                r.Chose = true;
                SpeechCaption("The deputy", Riders.DeputyAnswer(how, r.Seed), h, 3500, true, true);
                if (!r.Test && r.Town != null && how != "none")
                    _store.Counts["possepoint:" + r.Town] = how == "right" ? 1 : 2;   // the posse's end follows your word (Engine.PosseBack)
                if (how != "none")
                    _riderPointed++;
                Log.Write("RIDER the deputy asked; you " + (how == "right" ? "pointed him the right way" : how == "wrong" ? "pointed him the wrong way" : "said nothing"));
                return;
            }
            r.Together = id == "rider:together";
            if (r.Together)
            {
                _ridersTogether++;
                if (!r.Test && r.R != null)
                {
                    Person pp = _engine.PersonOfResident(r.R);
                    PersonState st = pp != null ? _store.Get(pp) : null;
                    if (st != null)
                    {
                        st.Sightings = Math.Max(st.Sightings, 2);   // met: he knows you in town after
                        st.Affection = Romance.Clamp(st.Affection + 8, -100, 100);
                    }
                }
            }
            RiderLeave(r, me, Game.GameTime, r.Together ? "you said ride on together" : "you said good day");
        }

        private void RiderLeave(RiderRun r, Ped me, int now, string why)
        {
            if (r.Phase == "leave")
                return;
            r.Phase = "leave";
            r.PhaseAt = now;
            if (_eventPrompt == Riders.Kind)
                PromptClear();
            int h = r.Man.Handle;
            if (!r.Deputy && r.Greeted)
                SpeechCaption(Person.FirstOf(r.Name), Riders.Parting(r.Together, r.Town, r.Seed), h, 3500, true, true);
            // off toward his town (or on ahead of you to it), else away from you along the road
            Vector3 to = r.To;
            if (to.DistanceTo(r.Man.Position) < 30f || r.Deputy)
            {
                Vector3 dir = r.Man.Position - me.Position;
                dir.Z = 0f;
                dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                to = r.Man.Position + dir * 150f;
            }
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, to.X, to.Y, to.Z, r.Mounted ? 2.5f : 1.3f, -1, 2f, 0, 0f);   // (from the saddle not proven: if the horse stands, the end gives him back to the game)
            _riderLast = r.Name + ": " + why;
            Log.Write("RIDER " + _riderLast);
        }

        private void EndRider(string why)
        {
            RiderRun r = _rider;
            _rider = null;
            if (r == null)
                return;
            if (_eventPrompt == Riders.Kind)
                PromptClear();
            if (r.Man != null && r.Man.Exists())
            {
                _resBind.Remove(r.Man.Handle);
                _encounterPeds.Remove(r.Man.Handle);
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(r.Man.Handle) && r.Man.Position.DistanceTo(Game.Player.Character.Position) < 120f;
                Spawner.Release(r.Man, !seen);   // gone unseen, or let go to the game where you can see him
            }
            if (r.Horse != null && r.Horse.Exists())
                Spawner.Release(r.Horse, !(ENTITY.IS_ENTITY_ON_SCREEN(r.Horse.Handle) && r.Horse.Position.DistanceTo(Game.Player.Character.Position) < 120f));
            _riderLast = (r.Name ?? "a rider") + ": " + why;
            Log.Write("RIDER over: " + _riderLast);
        }

        /// F7: the riders this session.
        private string RidersStatus()
        {
            if (!Riders.On)
                return "off ([Encounters] Riders)";
            return _riders + " riders came (" + _ridersAlong + " rode along, " + _ridersTogether + " on together; " + _riderDeputies + " deputies asked, pointed " + _riderPointed + "; " + _riderWalked +
                   " horses wouldn't take it and he walked)" + (_rider != null ? "; one out now (" + _rider.Phase + ")" : "") + (_riderLast != null ? "; last: " + _riderLast : "");
        }
    }
}
