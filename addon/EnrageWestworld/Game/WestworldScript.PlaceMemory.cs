// Memory that behaves like memory ([Mind] PlacesRemember; Core/PlaceMemory.cs; SIXDAY item 24; roadmap 4). Every twenty
// seconds the people near you with a record of their own are looked at for three things, one of each a look: someone
// who knew one you killed, near the spot it happened, thinks of it (the ground); someone who has seen you on your horse
// knows the horse before the face as you ride up (the horse: learned within 15 m while you're mounted, known from 18 m
// out to 45); someone in a place you keep an hour at thinks of your habit (the habits: a place at about the same hour on
// three different days, counted as you come in). F7 "TEST places remember:".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private const int PlaceMemoryLookMs = 20000;
        private int _nextPlaceMemory, _groundThoughts, _horseLearned, _horseThoughts, _habitsFormed, _habitThoughts;
        private Habit _habitHere;   // the habit of this place about this hour, if one has formed (set as you come in)
        private string _placeMemoryLast;
        private Action _aPlaceMemory;

        /// Tick04: a look every twenty seconds, under its own Safe.
        private void PlaceMemoryStep(int now)
        {
            if (_cfg == null || !_cfg.PlacesRemember || _store == null || _engine == null || now < _nextPlaceMemory)
                return;
            _nextPlaceMemory = now + PlaceMemoryLookMs;
            if (_aPlaceMemory == null)
                _aPlaceMemory = () => PlaceMemoryLook(_now);
            Safe("places remember", _aPlaceMemory);
        }

        /// You came into a place (WestworldScript.Watch.cs, as the place kind changes): the habit counts the day, or begins; the
        /// habit of this place about this hour, if formed, is kept for the look.
        private void PlaceMemoryEntered(string place, string town)
        {
            _habitHere = null;
            if (_cfg == null || !_cfg.PlacesRemember || _store == null || place == null || town == null)
                return;
            try
            {
                int clockHour = CLOCK.GET_CLOCK_HOURS();   // (the local's name is this add-on's own: the overlap scan)
                long day = Bridge.Now() / 1440;
                bool formed;
                Habit h = PlaceMemory.Note(_store.Habits, town, place, clockHour, day, out formed);
                if (formed && h != null)
                {
                    _habitsFormed++;
                    Log.Write("HABIT " + town + ": " + PlaceMemory.PlaceWords(place) + " about " + h.Hour + "h on " + h.Days + " different days: the town has your pattern now");
                }
                _habitHere = PlaceMemory.Formed(_store.Habits, town, place, clockHour);
            }
            catch (Exception ex)
            {
                Log.Write("HABIT: the visit couldn't be counted (" + ex.Message + ")");
            }
        }

        private void PlaceMemoryLook(int now)
        {
            if (MISC.GET_MISSION_FLAG())
                return;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            Vector3 at = me.Position;
            long g = Bridge.Now(), day = g / 1440;
            // the ground near you (the thinker within GroundMetres of the spot and 20 m of you: the spot may be up to 45 m off)
            DeathEntry ground = PlaceMemory.NearestGround(_store.Deaths, at.X, at.Y, at.Z, PlaceMemory.GroundMetres + 20f);
            Vector3 groundAt = ground != null ? new Vector3(ground.X, ground.Y, ground.Z) : at;
            // your horse, by its model: the same breed and coat reads the same, as it would to them
            uint horse = 0;
            if (PED.IS_PED_ON_MOUNT(me.Handle))
            {
                int mount = PED.GET_MOUNT(me.Handle);
                if (mount != 0 && ENTITY.DOES_ENTITY_EXIST(mount))
                    horse = ENTITY.GET_ENTITY_MODEL(mount);
            }
            bool groundDone = false, horseDone = false, habitDone = false;
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                float d = p.Position.DistanceTo(at);
                if (d > PlaceMemory.HorseFarMost)
                    continue;
                string id, name;
                if (!LastingId(p, out id, out name) || id == null)
                    continue;
                PersonState st = _store.Find(id);
                if (st == null)
                    continue;   // only people with a record of their own: nobody unknown reacts
                bool known = Knowing.Met(st);
                string surname = st.KnowsName ? _engine.Player.Surname : null;
                string first = name != null ? Person.FirstOf(name) : "someone";
                // the ground: one who knew the dead, near the spot, once
                if (!groundDone && ground != null && d <= 20f && p.Position.DistanceTo(groundAt) <= PlaceMemory.GroundMetres && !st.Has(PlaceMemory.GroundMark(ground)))
                {
                    bool friend = PlaceMemory.Friend(id, ground.Id, _store.Ties);   // a warm tie: the ground pulls for good
                    if ((friend || PlaceMemory.Knew(id, ground.Id, _store.Ties) || _engine.HeardOfDeath(id, ground.Id)) && PlaceMemory.Pulls(ground, friend, g) && NodInView(me, p))
                    {
                        st.Mark(PlaceMemory.GroundMark(ground));
                        bool knowsYou = ground.Seen;   // the killing was seen: the town can say who (the ledger's rule)
                        if (knowsYou)
                            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4000, 0, 51, 0);
                        else
                            TASK.TASK_TURN_PED_TO_FACE_COORD(h, ground.X, ground.Y, ground.Z, 2500);
                        string line = PlaceMemory.GroundLine(ground.Name != null ? Person.FirstOf(ground.Name) : null, knowsYou, friend, id + "|" + ground.At);
                        _ticker.Push(first + ", thinking", line, "near", h);
                        _groundThoughts++;
                        _placeMemoryLast = first + " at " + (ground.Name ?? "someone") + "'s ground";
                        Log.Write("GROUND " + first + (friend ? " (a friend of " : " (who heard of ") + (ground.Name ?? "the dead") + (friend ? ")" : "'s death)") + " at the spot, " +
                                  (int)p.Position.DistanceTo(groundAt) + " m off it" + (knowsYou ? ", and can say whose doing it was" : ", and can't say whose doing it was") + "; thinks: " + line);
                        groundDone = true;
                        continue;
                    }
                }
                // the horse: learned up close while you're mounted; known from farther off
                if (horse != 0 && known)
                {
                    if (d <= PlaceMemory.HorseLearnMetres)
                    {
                        if (st.Horse != horse && NodInView(me, p))
                        {
                            st.Horse = horse;
                            _horseLearned++;
                            Log.Detail("HORSE " + first + " has seen you on this horse");
                        }
                    }
                    else if (!horseDone && d >= PlaceMemory.HorseFar && st.Horse == horse && !st.Has(PlaceMemory.HorseMark(day)) && NodInView(me, p))
                    {
                        st.Mark(PlaceMemory.HorseMark(day));
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 3500, 0, 51, 0);
                        string line = PlaceMemory.HorseLine(surname, id + "|" + day);
                        _ticker.Push(first + ", thinking", line, "near", h);
                        _horseThoughts++;
                        _placeMemoryLast = first + " knew your horse";
                        Log.Write("HORSE " + first + " knows you by your horse from " + (int)d + " m; thinks: " + line);
                        horseDone = true;
                        continue;
                    }
                }
                // the habit: a known person in the place you keep an hour at, once
                if (!habitDone && _habitHere != null && _placeKind != null && _placeKind == _habitHere.Place && known && d <= 12f && !st.Has(PlaceMemory.HabitMark(_habitHere)) && NodInView(me, p))
                {
                    st.Mark(PlaceMemory.HabitMark(_habitHere));
                    string line = PlaceMemory.HabitLine(surname, _habitHere.Place, _habitHere.Hour, id + "|" + _habitHere.Hour);
                    _ticker.Push(first + ", thinking", line, "near", h);
                    _habitThoughts++;
                    _placeMemoryLast = first + " noticed your habit";
                    Log.Write("HABIT " + first + " has your pattern (" + PlaceMemory.PlaceWords(_habitHere.Place) + " about " + _habitHere.Hour + "h, " + _habitHere.Days + " days); thinks: " + line);
                    habitDone = true;
                }
            }
        }

        private string PlaceMemoryStatus()
        {
            if (_cfg == null || !_cfg.PlacesRemember)
                return "off ([Mind] PlacesRemember)";
            if (_store == null)
                return "not loaded";
            int grounds = 0, horses = 0;
            foreach (DeathEntry e in _store.Deaths)
                if (e != null && e.By == "player" && e.Id != null && !(e.X == 0f && e.Y == 0f && e.Z == 0f))
                    grounds++;
            foreach (PersonState st in _store.People.Values)
                if (st != null && st.Horse != 0)
                    horses++;
            var formed = new List<string>();
            foreach (Habit h in _store.Habits)
                if (h != null && h.Days >= PlaceMemory.HabitDays)
                    formed.Add(h.Town + " " + PlaceMemory.PlaceWords(h.Place) + " about " + h.Hour + "h (" + h.Days + " days)");
            return "on; grounds: " + grounds + " spots of yours, " + _groundThoughts + " thought of this session; the horse: " + horses + " people know it, " + _horseLearned + " learned and " + _horseThoughts +
                   " knew you by it this session; habits: " + formed.Count + " formed" + (formed.Count > 0 ? " (" + string.Join("; ", formed.ToArray()) + ")" : "") + ", " + _store.Habits.Count + " kept, " +
                   _habitsFormed + " formed and " + _habitThoughts + " thought of this session" + (_placeMemoryLast != null ? "; last: " + _placeMemoryLast : "");
        }
    }
}
