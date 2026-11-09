// Things to come across out on the road, in the game (FIVEDAY2 items 16-20; the table is Core/Wayside*.cs). Rolled like the other encounters, at most one
// out at a time, none in the towns. The cast is put out of your sight 45-70 m off, as the encounters always have been. Come near and the beats play on
// their clock; within 10 m the J menu has whatever can be done; draw on them and they run or come at you, as the kind says; ride on and it ends
// by itself, and they go back to the game. A fight is fists only and nobody dies of it. Testing rows "wayside:<id>"; F7 "TEST wayside:"; log "WAYSIDE".
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
        private sealed class WaysideRun
        {
            public bool AnswerDone;                    // what they do about you (TENHOUR item 11): settled
            public List<string> AnswerMenu;
            public int AnswerSpeaker, AnswerDeadline;
            public string AnswerKey;
            public LanguageRequest AnswerRequest;
            public WaysideKind K;
            public string Phase = "spawn";   // spawn, stand, play, fight, after, walk
            public Vector3 At;
            public float Facing;
            public Ped[] Cast;
            public uint[] Models;
            public int StartedAt, NoticedAt, PhaseUntil, Beat, Tries;
            public bool Noticed, Test, Chose, Fought;
            public string Outcome = "you rode on";
            public string BetSeed;   // a bet waiting on the fight
            public Blip Mark;
            public TownEvent Ask;
            public string Town;
            public Resident R;   // someone you know, when it's them (Core/Wayside.Known.cs)
        }

        private const int WaysideFightMs = 22000, WaysideAfterMs = 12000, WaysideRepeatDays = 2, WaysideFightOnMs = 120000, WaysideLingerMs = 180000;
        private WaysideRun _wayside;
        private string _waysideForce;
        private int _waysideCount;
        private string _waysideLast;

        private bool IsWaysidePed(int h)
        {
            WaysideRun w = _wayside;
            if (w == null || w.Cast == null)
                return false;
            for (int i = 0; i < w.Cast.Length; i++)
                if (w.Cast[i] != null && w.Cast[i].Handle == h)
                    return true;
            return false;
        }

        private string WaysideNotNow()
        {
            Ped me = Game.Player.Character;
            if (_bridge.TownOnly(me.Position) != null)
                return "you are in a town";
            if (_interiorType != 0)
                return "you're indoors";
            return null;
        }

        private bool WaysideLately(string id)
        {
            int day;
            return _store.Counts.TryGetValue("wayside:" + id, out day) && (int)(_store.LastGameMin / 1440) - day < WaysideRepeatDays && day <= (int)(_store.LastGameMin / 1440);
        }

        /// Someone you've met, a saved regular of a town other than the one you're nearest, nobody's just now: who it might be out here.
        private Resident WaysideKnownWho(Vector3 at)
        {
            string near = Rumours.NearestTown(at.X, at.Y);
            HashSet<Resident> bound = new HashSet<Resident>(_resBind.Values.Select(b => b.R));
            List<Resident> pool = _store.Residents.Where(r => r.Regular && !r.Dead && !r.Left && !r.Fled && r.Held == null && r.PostId == null && r.Cast == null && r.Model != 0 && r.Town != near && !bound.Contains(r) && GatherMet(r)).ToList();
            return pool.Count == 0 ? null : pool[_rng.Next(pool.Count)];
        }

        private WaysideKind WaysideKnownKind(Resident r)
        {
            Person who = _engine.PersonOfResident(r);
            long day = _store.LastGameMin / 1440;
            string worry;
            int stage;
            bool good;
            if (!InnerLife.WorryNow(who, day, out worry, out stage, out good))
                worry = null;
            return Wayside.Known(r.Name, r.Woman, r.Town, worry, _engine.KnowsYourName(r.Id) ? _engine.Player.FirstName : null, r.Id + "|" + day);
        }

        private readonly List<string> _waysideHere = new List<string>(3);
        private int _crossedAt, _crossingCheckAt;
        private Vector3 _crossedPos;

        /// Once a second: are you or your horse in the water, and where. We can't tell where the shores are, but a place you've just forded is one (TENHOUR item 10).
        private void WaysideNoteWater(int now)
        {
            if (now < _crossingCheckAt && _crossingCheckAt - now <= 1000)
                return;
            _crossingCheckAt = now + 1000;
            Ped me = Game.Player.Character;
            if (ENTITY.IS_ENTITY_IN_WATER(PED.IS_PED_ON_MOUNT(me.Handle) ? PED.GET_MOUNT(me.Handle) : me.Handle))
            {
                _crossedAt = now;
                _crossedPos = me.Position;
            }
        }

        /// What's true of where you are, for the scenes that can only happen in some places (TENHOUR items 8 to 10).
        private List<string> WaysideHere(Vector3 p)
        {
            _waysideHere.Clear();
            string near = Rumours.NearestTown(p.X, p.Y);
            if (near == "Annesburg" || near == "Butcher Creek")
                _waysideHere.Add(Wayside.PlaceMines);
            if (_bridge.ZoneName(p) == "the Grizzlies" || _engine.NowWeather == "snow")
                _waysideHere.Add(Wayside.PlaceSnow);
            if (_crossedAt != 0 && _now - _crossedAt < Wayside.CrossingMs && _now >= _crossedAt && _crossedPos.DistanceTo(p) < Wayside.CrossingNearMetres)
                _waysideHere.Add(Wayside.PlaceWater);
            return _waysideHere;
        }

        private bool StartWayside(int now, bool test)
        {
            Ped me = Game.Player.Character;
            Vector3 p = me.Position;
            bool onRoad = PATHFIND.IS_POINT_ON_ROAD(p.X, p.Y, p.Z, 0);
            Resident known = _waysideForce == Wayside.KnownId || (_waysideForce == null && !WaysideLately(Wayside.KnownId) && _rng.Next(100) < 25) ? WaysideKnownWho(p) : null;
            if (_waysideForce == Wayside.KnownId && known == null)
            {
                _waysideForce = null;
                Hint("Nobody you've met is free to be out here just now.", 5000);
                return false;
            }
            WaysideKind k = known != null ? WaysideKnownKind(known) : _waysideForce != null ? Wayside.Of(_waysideForce) : Wayside.Pick(CLOCK.GET_CLOCK_HOURS(), onRoad, WaysideLately, _store.LastGameMin + "|" + now, WaysideHere(p));
            _waysideForce = null;
            if (k == null)
                return false;
            string town = Rumours.NearestTown(p.X, p.Y) ?? _bridge.TownAt(p);
            var w = new WaysideRun { K = k, StartedAt = now, Test = test, Town = town, Cast = new Ped[k.Cast.Length], Models = new uint[k.Cast.Length], Facing = (float)_rng.NextDouble() * 360f };
            for (int i = 0; i < k.Cast.Length; i++)
                w.Models[i] = WaysideModel(k.Cast[i], town);
            if (known != null)
            {
                w.R = known;
                w.Models[0] = known.Model;   // themselves, as the town knows them
            }
            _wayside = w;
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);   // a big moment: the quiet starts (Pacing.cs)
            Log.Write("WAYSIDE " + k.Id + " begins" + (test ? " (a test)" : "") + " near " + (town ?? "the road"));
            return true;
        }

        private static readonly string[] SoldierModels = { "s_m_m_army_01", "s_m_y_army_01" };

        private uint WaysideModel(WaysideRole role, string town)
        {
            if (role.Pool == "soldier")
                foreach (string m in SoldierModels)
                {
                    uint hash = Natives.Hash(m);
                    if (STREAMING.IS_MODEL_VALID(hash))
                        return hash;
                }
            return Spawner.ModelFor(role.Pool == "town" ? town : null, role.Woman, _rng.Next());   // no soldier's body in this game: someone rough will do
        }

        /// The way the scene faces, as a direction on the ground.
        private static Vector3 WaysideAlong(WaysideRun w)
        {
            double rad = w.Facing * Math.PI / 180.0;
            return new Vector3((float)Math.Sin(rad), (float)-Math.Cos(rad), 0f);
        }

        private static bool WaysideOfTheDead(WaysideRun w)
        {
            WaysideRole[] cast = w.K.Cast;
            for (int i = 0; i < cast.Length; i++)
                if (cast[i].Act != "dead")
                    return false;
            return true;
        }

        private readonly List<Ped> _waysideLive = new List<Ped>(8);

        private void WaysideStep(int now)
        {
            WaysideRun w = _wayside;
            if (MISC.GET_MISSION_FLAG())
            {
                WaysideEnd("a mission started");
                return;
            }
            if (w.Phase == "spawn")
            {
                WaysideSpawn(w, now);
                return;
            }
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            EventDot("wayside", w.At);
            bool theDead = WaysideOfTheDead(w);
            List<Ped> live = _waysideLive;   // kept, so a step allocates nothing (TENHOUR item 1)
            live.Clear();
            float d = float.MaxValue;
            foreach (Ped c in w.Cast)
                if (c != null && c.Exists() && (theDead || !c.IsDead))
                {
                    live.Add(c);
                    d = Math.Min(d, c.Position.DistanceTo(pos));
                }
            if (live.Count == 0)
            {
                WaysideEnd("none of them is left");
                return;
            }
            if (d > 150f)
            {
                WaysideEnd(w.Chose ? w.Outcome : "you rode on");
                return;
            }
            if (w.Phase == "after")
            {
                bool stillAtIt = w.Fought && now < w.PhaseUntil + WaysideFightOnMs && live.Any(c => PED.IS_PED_IN_COMBAT(c.Handle, me.Handle));   // the scene waits for the shooting, up to a point
                if (now >= w.PhaseUntil && !stillAtIt)
                    WaysideEnd(w.Outcome);
                return;
            }
            if (WaysideThreatened(w, live, me, d, now))
                return;
            if (!w.Noticed)
            {
                bool seen = live.Any(c => ENTITY.IS_ENTITY_ON_SCREEN(c.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, c.Handle, 17));
                if (d < Wayside.NearMetres || (d < 70f && seen))
                {
                    w.Noticed = true;
                    w.NoticedAt = now;
                    w.Phase = "play";
                    Log.Write("WAYSIDE " + w.K.Id + ": you've come upon it, " + (int)d + " m off");
                }
                else if (now - w.StartedAt > (w.Test ? 2 * Wayside.UnseenMs : Wayside.UnseenMs))
                {
                    WaysideEnd("you never came near; nothing came of it");
                    return;
                }
                return;
            }
            if ((w.Phase == "play" || w.Phase == "walk") && w.Beat >= (w.K.Beats != null ? w.K.Beats.Length : 0) && now - w.NoticedAt > WaysideLingerMs)
            {
                WaysideEnd(w.Chose ? w.Outcome : "you stayed by it and nothing more came of it");
                return;
            }
            WaysidePlay(w, live, now, d);
            if (_cfg.WaysideAnswers)
                WaysideAnswerStep(w, live, me, d, now);
            if (w.Phase == "fight" && now >= w.PhaseUntil)
                WaysideFightOver(w, live, now);
            if (w.Phase == "shooting" && now >= w.PhaseUntil)
                WaysideShotsDone(w, now);
            if (!w.Chose && d <= Wayside.ChoiceMetres && w.K.Choices != null && _eventMenu == null && (_eventPrompt == null || _eventPrompt == Wayside.Kind || now > _eventPromptUntil))
            {
                if (w.Ask == null)
                    w.Ask = new TownEvent { Kind = Wayside.Kind, Town = w.Town, Extra = w.K.Id };
                PromptSet(Wayside.Kind, w.Ask, 1500);
            }
        }

        private void WaysideSpawn(WaysideRun w, int now)
        {
            if (now - w.StartedAt > 20000 || w.Tries > 8)
            {
                WaysideEnd("nowhere hidden to put them");
                return;
            }
            if (w.Models.Any(m => !Spawner.Ready(m)))
                return;
            Ped me = Game.Player.Character;
            Vector3 at;
            if (!EncounterSpot(me, 45f, 70f, out at) || CampInView(at) || (w.K.Place == Wayside.PlaceWater && !w.Test && at.DistanceTo(_crossedPos) > Wayside.CrossingMetres))
            {
                w.Tries++;
                return;
            }
            w.At = at;
            Vector3 along = WaysideAlong(w), across = new Vector3(along.Y, -along.X, 0f);
            var spots = new Vector3[w.K.Cast.Length];
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 s = at + across * w.K.Cast[i].X + along * w.K.Cast[i].Y, ground;
                spots[i] = Natives.StandSpot(s, at.Z, out ground) ? ground : s;
            }
            for (int i = 0; i < w.Cast.Length; i++)
            {
                WaysideRole role = w.K.Cast[i];
                Vector3 toward = role.Face >= 0 ? spots[role.Face] : at;
                Ped p = Spawner.Spawn(w.Models[i], spots[i], Encounters.Heading(toward.X - spots[i].X, toward.Y - spots[i].Y));
                if (p == null || ENTITY.IS_ENTITY_IN_WATER(p.Handle))
                {
                    if (p != null)
                        Spawner.Release(p, true);
                    continue;
                }
                int h = p.Handle;
                w.Cast[i] = p;
                _encounterPeds.Add(h);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                PED.SET_PED_KEEP_TASK(h, true);
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                if (role.Arm == "revolver")
                    p.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                else if (role.Arm == "rifle")
                    WEAPON.GIVE_WEAPON_TO_PED(h, Natives.Hash("WEAPON_RIFLE_SPRINGFIELD"), 30, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);
                if (w.R != null && i == 0)
                {
                    PED._EQUIP_META_PED_OUTFIT_PRESET(h, w.R.Outfit, false);
                    PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                    Dressed.Note(h);   // not read for a while (Game/Dressed.cs)
                    _resBind[h] = new ResBind { R = w.R, Model = w.R.Model };
                }
                if (role.Act == "dead")
                {
                    ENTITY.SET_ENTITY_HEALTH(h, 0, 0);
                    continue;
                }
                if (role.Act != null && role.Act != "stand")
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(role.Act), -1, true, 0u, -1f, false);
                else
                    TASK.TASK_STAND_STILL(h, -1);
            }
            if (w.Cast.All(c => c == null))
            {
                WaysideEnd("they couldn't be put there");
                return;
            }
            try
            {
                w.Mark = w.Cast.First(c => c != null).AddBlip(BlipType.WhiteDot);
                w.Mark.Label = w.K.Mark;
            }
            catch
            {
            }
            w.Phase = "stand";
            Log.Write("WAYSIDE " + w.K.Id + ": " + w.Cast.Count(c => c != null) + " of them, " + (int)at.DistanceTo(me.Position) + " m away");
        }

        // ---- what they do about you (TENHOUR item 11; Core/Wayside.Answer.cs)
        private int _waysideByModel, _waysideByRules;
        private string _waysideAnswerLast;

        /// You've come close: one of them is asked, and acts on it when the answer's in or the wait is up. Once a scene.
        private void WaysideAnswerStep(WaysideRun w, List<Ped> live, Ped me, float d, int now)
        {
            if (w.AnswerDone)
                return;
            if (w.AnswerMenu == null)
            {
                if (d > Wayside.AnswerMetres || now - w.NoticedAt < Wayside.AnswerAfterMs || (w.Phase != "play" && w.Phase != "walk"))
                    return;
                w.AnswerMenu = Wayside.AnswerMenu(w.K, w.K.Id + "|" + w.StartedAt);
                w.AnswerSpeaker = Wayside.AnswerSpeaker(w.K, i => w.Cast[i] != null && w.Cast[i].Exists() && !w.Cast[i].IsDead);
                if (w.AnswerMenu.Count < 2 || w.AnswerSpeaker < 0)
                {
                    w.AnswerDone = true;
                    return;
                }
                w.AnswerDeadline = now + Wayside.AnswerWaitMs;
                if (PicksOn)
                {
                    LanguageRequest r = Wayside.AnswerRequest(w.K, w.AnswerSpeaker, Voice.NowYear, CLOCK.GET_CLOCK_HOURS(), w.AnswerMenu);
                    string key = "wayside|" + w.K.Id + "|" + w.StartedAt;
                    if (_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = w.K.Cast[w.AnswerSpeaker].Name, PersonId = "wayside", Request = r, AskedAtMs = Environment.TickCount }))
                    {
                        w.AnswerKey = key;
                        w.AnswerRequest = r;
                    }
                }
                return;
            }
            string answer = null;
            long ms = 0;
            bool came = w.AnswerKey != null && _engine.LanguageAnswers.TakePick(w.AnswerKey, out answer, out ms);
            if (!came && now < w.AnswerDeadline && now >= w.AnswerDeadline - Wayside.AnswerWaitMs)
                return;
            w.AnswerDone = true;
            if (w.Chose || w.Fought || (w.Phase != "play" && w.Phase != "walk"))
                return;   // you've done something about it first, or it has come to blows: that's the answer
            string thought = null, seed = "wayside|" + w.K.Id + "|" + w.StartedAt;
            int idx = came ? Language.ParseThinkPick(answer, w.AnswerMenu.Count, out thought) : -1;
            string what = idx >= 0 ? w.AnswerMenu[idx] : Wayside.AnswerRules(w.AnswerMenu, seed);
            if (idx >= 0)
                _waysideByModel++;
            else
                _waysideByRules++;
            _waysideAnswerLast = w.K.Id + ": " + Wayside.AnswerWords(what) + (idx >= 0 ? " (the model)" : " (the rules)");
            Log.Write("WAYSIDE " + _waysideAnswerLast);
            WaysideAnswerDo(w, me, what, thought, seed, now);
        }

        private void WaysideAnswerDo(WaysideRun w, Ped me, string what, string thought, string seed, int now)
        {
            Ped who = w.Cast[w.AnswerSpeaker];
            if (who == null || !who.Exists() || who.IsDead)
                return;
            int h = who.Handle, you = me.Handle;
            string line = Wayside.AnswerLine(what, seed);
            if (line != null)
            {
                TASK.TASK_LOOK_AT_ENTITY(h, you, 4000, 0, 51, 0);
                string tag = what == Wayside.AnswerAsk ? "plead" : what == Wayside.AnswerCall ? "hum" : "threat";
                Feed.Say(w.K.Cast[w.AnswerSpeaker].Name, line, Speech.When(h, tag, you), h, toYou: true);
            }
            if (!string.IsNullOrEmpty(thought) && w.AnswerRequest != null && Language.Check(w.AnswerRequest, thought) == null)
                _ticker.Push(w.K.Cast[w.AnswerSpeaker].Name + ", thinking", thought, "near", h);
            if (what != Wayside.AnswerTurn)
                return;
            foreach (Ped p in w.Cast)
                if (p != null && p.Exists() && !p.IsDead && WEAPON.IS_PED_ARMED(p.Handle, 4))
                {
                    PED.SET_PED_KEEP_TASK(p.Handle, false);
                    MakeHostile(p, now);
                    _cameAtYouSeen[p.Handle] = now;   // they came at you: answering them is self-defence
                    ScenarioExit.SoftUp(p.Handle, x => TASK.TASK_COMBAT_PED(x, you, 0, 16));
                }
            w.Fought = true;
            w.Chose = true;
            w.Outcome = "they turned on you";
            w.Phase = "after";
            w.PhaseUntil = now + 90000;
        }

        /// Drawn on, shot at or hit: they run, or the armed come at you.
        private bool WaysideThreatened(WaysideRun w, List<Ped> live, Ped me, float d, int now)
        {
            if (w.Fought || WaysideOfTheDead(w))
                return false;
            bool threat = PED.IS_PED_SHOOTING(me.Handle) && d < 40f;
            for (int i = 0; i < live.Count && !threat; i++)
                threat = ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(live[i].Handle, me.Handle, true, false) || PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, live[i].Handle);
            if (!threat)
                return false;
            w.Fought = true;
            int you = me.Handle;
            foreach (Ped c in live)
            {
                int h = c.Handle;
                PED.SET_PED_KEEP_TASK(h, false);
                if (w.K.Threat == "fight" && WEAPON.IS_PED_ARMED(h, 4))
                {
                    MakeHostile(c, now);
                    _cameAtYouSeen.Remove(h);
                    _youStarted[h] = now;   // you drew first: answering them isn't self-defence (WestworldScript.Watch.cs)
                    ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, you, 0, 16));
                }
                else
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    ScenarioExit.SoftUp(h, x => TASK.TASK_SMART_FLEE_PED(x, you, 150f, -1, 0, 3f, 0));
                }
            }
            w.Outcome = "you pulled a gun on them";
            w.Phase = "after";
            w.PhaseUntil = now + 45000;
            Log.Write("WAYSIDE " + w.K.Id + ": you pulled a gun on them; they " + (w.K.Threat == "fight" ? "come at you" : "run"));
            return true;
        }

        /// The next of what's said and done, on the clock from when you came upon it.
        private void WaysidePlay(WaysideRun w, List<Ped> live, int now, float d)
        {
            WaysideBeat[] beats = w.K.Beats ?? new WaysideBeat[0];
            while (w.Beat < beats.Length && now - w.NoticedAt >= beats[w.Beat].Ms)
            {
                WaysideBeat b = beats[w.Beat++];
                if (b.Do != null)
                {
                    WaysideDo(w, b, live, now);
                    continue;
                }
                Ped who = b.Who >= 0 && b.Who < w.Cast.Length ? w.Cast[b.Who] : null;
                if (who == null || !who.Exists() || who.IsDead || b.Lines == null || d > 60f)
                    continue;
                int to = b.To == -2 ? Game.Player.Character.Handle : b.To >= 0 && b.To < w.Cast.Length && w.Cast[b.To] != null ? w.Cast[b.To].Handle : 0;
                Feed.Say(w.K.Cast[b.Who].Name, Wayside.Word(b.Lines, w.K.Id + "|" + w.StartedAt + "|" + w.Beat), Speech.When(who.Handle, b.Tag, to), who.Handle, toYou: b.To == -2);
            }
        }

        private void WaysideDo(WaysideRun w, WaysideBeat b, List<Ped> live, int now)
        {
            if (b.Do == "walk")
            {
                // in file along the way they face, each keeping their place in the line
                Vector3 along = WaysideAlong(w);
                for (int i = 0; i < w.Cast.Length; i++)
                    if (w.Cast[i] != null && w.Cast[i].Exists() && !w.Cast[i].IsDead)
                    {
                        Vector3 to = w.At + along * (w.K.WalkMetres + w.K.Cast[i].Y);
                        PED.SET_PED_KEEP_TASK(w.Cast[i].Handle, false);
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(w.Cast[i].Handle, to.X, to.Y, to.Z, 1f, -1, 1f, 0, 0f);
                    }
                return;
            }
            Ped doer = b.Who >= 0 && b.Who < w.Cast.Length ? w.Cast[b.Who] : null;
            if (b.Do == "run" && doer != null && doer.Exists() && !doer.IsDead)
            {
                Vector3 to = w.At + WaysideAlong(w) * w.K.WalkMetres;
                PED.SET_PED_KEEP_TASK(doer.Handle, false);
                ScenarioExit.SoftUp(doer.Handle, x => TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(x, to.X, to.Y, to.Z, 3f, -1, 1f, 0, 0f));   // up first: someone at a scenario doesn't take a walk order
                return;
            }
            if (b.Do == "chase" && doer != null && doer.Exists() && !doer.IsDead && w.Cast[0] != null && w.Cast[0].Exists())
            {
                PED.SET_PED_KEEP_TASK(doer.Handle, false);
                TASK.TASK_GO_TO_ENTITY(doer.Handle, w.Cast[0].Handle, 60000, 1.5f, 3f, 0f, 0);
                return;
            }
            if (b.Do == "flee")
            {
                int you = Game.Player.Character.Handle;
                foreach (Ped p in doer != null ? new List<Ped> { doer } : live)
                    if (p != null && p.Exists() && !p.IsDead)
                    {
                        PED.SET_PED_KEEP_TASK(p.Handle, false);
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                        ScenarioExit.SoftUp(p.Handle, x => TASK.TASK_SMART_FLEE_PED(x, you, 150f, -1, 0, 3f, 0));
                    }
                return;
            }
            if (b.Do == "shoot" && w.K.SideA != null && w.K.SideB != null)
            {
                WaysideFire(w, w.K.SideA, w.K.SideB);
                if (w.K.SideA.Length == 1 && w.K.SideB.Length == 1)
                    WaysideFire(w, w.K.SideB, w.K.SideA);   // a duel: both at once
                w.Phase = "shooting";
                w.PhaseUntil = now + 3000;
                Log.Write("WAYSIDE " + w.K.Id + ": they fire");
                return;
            }
            if (b.Do == "fight" && w.K.SideA != null && w.K.SideB != null)
            {
                foreach (int i in w.K.SideA)
                    WaysideSwing(w, i, w.K.SideB);
                foreach (int i in w.K.SideB)
                    WaysideSwing(w, i, w.K.SideA);
                w.Phase = "fight";
                w.PhaseUntil = now + WaysideFightMs;
                Log.Write("WAYSIDE " + w.K.Id + ": it comes to blows");
            }
        }

        /// Fists: their guns away, up on their feet, and at the nearest of the other side.
        private void WaysideSwing(WaysideRun w, int i, int[] others)
        {
            Ped p = w.Cast[i];
            Ped foe = others.Select(o => w.Cast[o]).FirstOrDefault(c => c != null && c.Exists() && !c.IsDead);
            if (p == null || !p.Exists() || p.IsDead || foe == null)
                return;
            int h = p.Handle, f = foe.Handle;
            WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);
            ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, f, 0, 16));
        }

        /// The game plays the shots, as it does for the road execution's gunman.
        private void WaysideFire(WaysideRun w, int[] shooters, int[] targets)
        {
            Ped t = targets.Select(o => w.Cast[o]).FirstOrDefault(c => c != null && c.Exists() && !c.IsDead);
            if (t == null)
                return;
            int target = t.Handle;
            foreach (int i in shooters)
            {
                Ped p = w.Cast[i];
                if (p == null || p.IsDead || !p.Exists())
                    continue;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                PED.SET_PED_KEEP_TASK(p.Handle, false);
                TASK.CLEAR_PED_TASKS(p.Handle, false, false);
                TASK.TASK_COMBAT_PED(p.Handle, target, 0, 16);
            }
        }

        /// However the shots went, it ends with one down: the deserter, or in a duel whichever the morning chooses. The rest stop and go.
        private void WaysideShotsDone(WaysideRun w, int now)
        {
            bool duel = w.K.SideA.Length == 1 && w.K.SideB.Length == 1;
            int[] losers = duel && Voice.Chance(w.K.Id + "|" + w.StartedAt + "|duel", 50) ? w.K.SideA : w.K.SideB;
            foreach (int i in losers)
            {
                Ped p = w.Cast[i];
                if (p != null && p.Exists() && !p.IsDead)
                    ENTITY.SET_ENTITY_HEALTH(p.Handle, 0, 0);
            }
            foreach (Ped p in w.Cast)
                if (p != null && p.Exists() && !p.IsDead)
                {
                    TASK.CLEAR_PED_TASKS(p.Handle, false, false);
                    TASK.TASK_STAND_STILL(p.Handle, 6000);
                }
            w.Outcome = duel ? "one of them fell" : "they shot him";
            w.Phase = "after";
            w.PhaseUntil = now + WaysideAfterMs;
        }

        /// The fight is over: one side down on the ground, nobody dead of it.
        /// A dollar down, won or lost. `noFight`: a bet on someone's shooting, not on a man in a fight.
        private string WaysideSettleBet(string seed, bool noFight)
        {
            if (Wayside.BetWon(seed))
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(200, 0);
                Hint(noFight ? "He missed. Two dollars to you." : "Your man won. Two dollars.", 4000);
                return "you bet and won";
            }
            Hint(noFight ? "Clean through the knot. The dollar's his." : "Your man went down. The dollar's gone.", 4000);
            return "you bet and lost";
        }

        private void WaysideFightOver(WaysideRun w, List<Ped> live, int now)
        {
            bool aDown = Voice.Chance(w.K.Id + "|" + w.StartedAt + "|down", 50);
            foreach (int i in aDown ? w.K.SideA : w.K.SideB)
            {
                Ped p = w.Cast[i];
                if (p != null && p.Exists() && !p.IsDead)
                {
                    TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                    PED.SET_PED_TO_RAGDOLL(p.Handle, 7000, 9000, 0, false, false, "");
                }
            }
            foreach (int i in aDown ? w.K.SideB : w.K.SideA)
            {
                Ped p = w.Cast[i];
                if (p != null && p.Exists() && !p.IsDead)
                {
                    TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                    TASK.TASK_STAND_STILL(p.Handle, 5000);
                }
            }
            w.Outcome = w.BetSeed != null ? WaysideSettleBet(w.BetSeed, false) : "the fight ran its course";
            w.Phase = "after";
            w.PhaseUntil = now + WaysideAfterMs;
            Log.Write("WAYSIDE " + w.K.Id + ": the fight is over, side " + (aDown ? "A" : "B") + " down");
        }

        /// Chosen on the J menu (Trials.cs's EventMenuChoose).
        private void WaysideChoose(TownEvent e, string id)
        {
            WaysideRun w = _wayside;
            if (w == null || e == null || w.K.Id != e.Extra)
                return;
            WaysideChoice c = Wayside.Choice(w.K, id);
            if (c == null)
                return;
            w.Chose = true;
            int now = Game.GameTime;
            if (c.Cents > 0)
                MONEY._MONEY_DECREMENT_CASH_BALANCE(c.Cents);
            Ped by = c.By >= 0 && c.By < w.Cast.Length ? w.Cast[c.By] : null;
            string seed = w.K.Id + "|" + w.StartedAt + "|" + id;
            if (c.Said != null && c.Said.Length > 0 && by != null && by.Exists() && !by.IsDead)
                Feed.Say(w.K.Cast[c.By].Name, Wayside.Word(c.Said, seed), Speech.When(by.Handle, "thanks", Game.Player.Character.Handle), by.Handle, toYou: true);
            w.Outcome = WaysideEffect(w, c, seed, now);
            Log.Write("WAYSIDE " + w.K.Id + ": you chose " + id + ": " + w.Outcome);
        }

        /// What a choice does. Null keeps it going as it was.
        private string WaysideEffect(WaysideRun w, WaysideChoice c, string seed, int now)
        {
            switch (c.Effect)
            {
                case "part":
                    foreach (Ped p in w.Cast)
                        if (p != null && p.Exists() && !p.IsDead)
                        {
                            TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                            TASK.TASK_STAND_STILL(p.Handle, 5000);
                        }
                    w.Phase = "after";
                    w.PhaseUntil = now + WaysideAfterMs;
                    return "you parted them";
                case "bet":
                    if (w.K.SideA != null && (w.Phase == "play" || w.Phase == "fight"))
                    {
                        w.BetSeed = seed;   // settled when the fight is (WaysideFightOver)
                        Hint("Your dollar's down. Now watch.", 3000);
                        return "you put a dollar on it";
                    }
                    return WaysideSettleBet(seed, w.K.SideA == null);
                case "buy":
                    Hint("It tastes like a fence post. You'll live.", 4000);
                    return "you bought a jar";
                case "misdirect":
                    {
                        Vector3 me = Game.Player.Character.Position;
                        for (int i = 0; i < w.Cast.Length; i++)
                        {
                            Ped p = w.Cast[i];
                            if (p == null || !p.Exists() || p.IsDead || w.K.Cast[i].Arm == null)
                                continue;
                            Vector3 away = p.Position + new Vector3(p.Position.Y - me.Y, me.X - p.Position.X, 0f).Normalized * 120f;   // off at right angles to where he went
                            PED.SET_PED_KEEP_TASK(p.Handle, false);
                            TASK.CLEAR_PED_TASKS(p.Handle, false, false);
                            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, away.X, away.Y, away.Z, Natives.Brisk, -1, 1f, 0, 0f);
                        }
                        w.Phase = "after";
                        w.PhaseUntil = now + WaysideAfterMs;
                        return "you sent them the wrong way";
                    }
                case "leave":
                    return "you left them to it";
                case "help":
                    {
                        if (w.R != null)
                            _engine.HelpedUp(_engine.PersonOfResident(w.R), true, Bridge.Now());   // what you did for them on an empty road: fonder of you, and they remember it
                        Ped by = c.By >= 0 && c.By < w.Cast.Length ? w.Cast[c.By] : null;
                        if (by != null && by.Exists() && !by.IsDead && w.K.WalkMetres <= 0f)
                        {
                            PED.SET_PED_KEEP_TASK(by.Handle, false);
                            ScenarioExit.SoftUp(by.Handle, x => TASK.TASK_WANDER_STANDARD(x, 10f, 10));
                        }
                        if (w.K.WalkMetres <= 0f)
                        {
                            w.Phase = "after";
                            w.PhaseUntil = now + WaysideAfterMs;
                        }
                        return "you helped";
                    }
                case "rob":
                    {
                        Ped by = c.By >= 0 && c.By < w.Cast.Length ? w.Cast[c.By] : null;
                        int cents = 100 + Voice.Hash(seed + "|rob") % 500;
                        MONEY._MONEY_INCREMENT_CASH_BALANCE(cents, 0);
                        Hint("$" + (cents / 100) + "." + (cents % 100).ToString("00") + ". It's all he had.", 4000);
                        if (by != null && by.Exists() && !by.IsDead)
                        {
                            int you = Game.Player.Character.Handle;
                            PED.SET_PED_KEEP_TASK(by.Handle, false);
                            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(by.Handle, false);
                            ScenarioExit.SoftUp(by.Handle, x => TASK.TASK_SMART_FLEE_PED(x, you, 150f, -1, 0, 2f, 0));
                        }
                        w.Phase = "after";
                        w.PhaseUntil = now + WaysideAfterMs;
                        return "you robbed him";
                    }
                case "hostile":
                    {
                        int you = Game.Player.Character.Handle;
                        foreach (Ped p in w.Cast)
                            if (p != null && p.Exists() && !p.IsDead && WEAPON.IS_PED_ARMED(p.Handle, 4))
                            {
                                PED.SET_PED_KEEP_TASK(p.Handle, false);
                                MakeHostile(p, now);
                                ScenarioExit.SoftUp(p.Handle, x => TASK.TASK_COMBAT_PED(x, you, 0, 16));
                            }
                        w.Fought = true;
                        w.Phase = "after";
                        w.PhaseUntil = now + 90000;
                        return "you crossed them";
                    }
                case "search":
                    {
                        int cents = 40 + Voice.Hash(seed + "|search") % 360;
                        MONEY._MONEY_INCREMENT_CASH_BALANCE(cents, 0);
                        Hint("$" + (cents / 100) + "." + (cents % 100).ToString("00") + ", and nothing with a name on it.", 4000);
                        w.Phase = "after";
                        w.PhaseUntil = now + 5000;
                        return "you went through their pockets";
                    }
                case "paid":
                    {
                        int cents = 200 + Voice.Hash(seed + "|paid") % 600;
                        MONEY._MONEY_INCREMENT_CASH_BALANCE(cents, 0);
                        Hint("$" + (cents / 100) + "." + (cents % 100).ToString("00") + " in your hand, and nobody the wiser.", 4000);
                        w.Phase = "after";
                        w.PhaseUntil = now + WaysideAfterMs;
                        return "you took their money";
                    }
                case "bless":
                    Hint("You take your hat off a moment. There's nobody to see it.", 4000);
                    w.Phase = "after";
                    w.PhaseUntil = now + 5000;
                    return "you said a word over them";
                default:
                    return "you " + c.Effect;
            }
        }

        /// What comes of it: the nearest town hears, a while on, and someone you stopped for may write, to the post office there.
        private void WaysideAfterward(WaysideRun w)
        {
            long g = Bridge.Now();
            string seed = w.K.Id + "|" + w.StartedAt;
            if (!w.K.NewsIfHelped || w.Outcome == "you helped")
                _engine.WaysideHeard(Wayside.Heard(w.K, w.Town, null), w.Town, seed, g);
            if (w.K.NewsIfHelped && w.Outcome == "you helped" && w.R == null && w.Town != null && Wayside.Writes(seed) && _cfg.Letters)
                _engine.QueueLetter(new Paper { Id = Ids.New(), Kind = "note", Title = "A note left for you at " + w.Town, From = "Someone from the road", Body = Wayside.ThanksBody(seed), At = g, Town = w.Town, Delivered = false });
        }

        private void WaysideEnd(string why)
        {
            EventDotClear("wayside");
            WaysideRun w = _wayside;
            _wayside = null;
            if (w == null)
                return;
            try
            {
                if (w.Mark != null && w.Mark.Exists())
                    w.Mark.Delete();
            }
            catch
            {
            }
            if (_eventPrompt == Wayside.Kind)
                PromptClear();
            if (w.Phase != "spawn")
            {
                if (!w.Test)
                    _store.Counts["wayside:" + w.K.Id] = (int)(_store.LastGameMin / 1440);
                if (w.Noticed)
                    Safe("world events", () => RecordEvent("encounter:wayside", null, w.K.Id, null, w.Town, w.At, 0, 0, 0, why));
                if (w.Noticed && !w.Test && _engine != null)
                    WaysideAfterward(w);   // what comes of it
            }
            Ped me = Game.Player.Character;
            if (w.Cast != null)
                foreach (Ped p in w.Cast)
                {
                    if (p == null)
                        continue;
                    bool alive = p.Exists() && !p.IsDead;
                    bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle);
                    Spawner.Release(p, alive && !PED.IS_PED_IN_COMBAT(p.Handle, 0) && (!seen || p.Position.DistanceTo(me.Position) > 120f));
                }
            _waysideCount++;
            _waysideLast = w.K.Id + ": " + why;
            Log.Write("WAYSIDE " + w.K.Id + ": over (" + why + ")");
        }

        /// One of the cast, on the ticker.
        private void WaysideNear(Ped ped, float d)
        {
            WaysideRun w = _wayside;
            if (w == null || w.K.Thoughts == null)
                return;
            int i = Array.FindIndex(w.Cast, c => c != null && c.Handle == ped.Handle);
            if (i < 0 || w.Phase == "spawn")
                return;
            long now = Bridge.Now();
            _near.Add(new NearThought
            {
                Handle = ped.Handle, Key = "way:" + ped.Handle, Name = w.K.Cast[i].Name, Distance = d + 2f,
                Text = () => _cfg.RoadThoughts ? Wayside.ThoughtOf(w.K, i, ped.Handle + "|" + now / 180) : Wayside.Word(w.K.Thoughts, ped.Handle + "|" + now / 180)
            });
        }

        private string WaysideStatus()
        {
            return (_cfg.Wayside ? _cfg.WaysideChance + "% a roll" : "off ([Encounters] Wayside)") + "; " + _waysideCount + " this session" + (_wayside != null ? "; out: " + _wayside.K.Id + " (" + _wayside.Phase + ")" : "") + (_waysideLast != null ? "; last: " + _waysideLast : "");
        }
    }
}
