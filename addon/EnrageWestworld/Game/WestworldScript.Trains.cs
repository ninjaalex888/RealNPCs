// Train time, in the game (SIXDAY item 11b; Core/Trains.cs, Core/Engine.Trains.cs). Every second while the player is within
// 120 m of a platform spot (Core/Spots.cs), the tracks are asked for a train near it; standing still for five seconds means it
// has arrived, moving again means it has left. On arrival, for two minutes: up to four people on the platform turn to look,
// the station's keeper calls it in his own voice, one person steps down (a newcomer or kin, made a resident on a free body
// by the platform, unseen), letters waiting for the town come off it, and anyone leaving today is cast on the platform, says
// goodbye to a friend if one is near, walks to the train and is let go. Never the train, its crew or its passengers; nothing
// in missions; nothing spawned at all. [World] TrainTime. Log TRAIN; F7 "TEST trains:".
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
        private int _trainNext;
        private int _trainHandle;            // the train near the platform, 0 for none
        private int _trainStillSince;        // game ms it stopped moving, 0 while moving
        private bool _trainArrived;          // standing long enough: arrived (once per standing)
        private int _trainArrivedAt;
        private Spot _trainPlatform;
        private string _trainLastSeen = "none yet";
        private bool _trainProbed;
        private int _trainsArrived, _trainsDeparted, _trainNewcomers, _trainLookers;
        private int _leaverHandle;           // the leaver's body walking to the train
        private int _leaverUntil;
        private Resident _leaver;
        private string _leaverWhy;
        private Action _aTrains;

        private void TrainsStep(int now)
        {
            if (_aTrains == null)
                _aTrains = () => TrainsTick(_now);
            Safe("trains", _aTrains);
        }

        private void TrainsTick(int now)
        {
            if (_cfg == null || _engine == null || _store == null)
                return;
            if (!_cfg.TrainTime)
            {
                if (_leaver != null)
                    LeaverAbort();   // switched off mid-walk: the body is the game's again, the resident stays (the review of items 11-12)
                return;
            }
            if (now < _trainNext)
                return;
            _trainNext = now + 1000;
            LeaverStep(now);   // once a second, like the rest of this
            if (MISC.GET_MISSION_FLAG())
                return;
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            Spot platform = Spots.Nearest(_spots, null, "platform", pos.X, pos.Y);
            if (platform == null || Spots.Distance(platform, pos.X, pos.Y) > Trains.PlatformWatchMetres)
            {
                TrainReset();
                return;
            }
            _trainPlatform = platform;
            int train = TrainNear(platform);
            if (train == 0)
            {
                if (_trainHandle != 0)
                {
                    Log.Write("TRAIN " + platform.Town + ": gone from the platform" + (_trainArrived ? " (departed)" : ""));
                    if (_trainArrived)
                        _trainLastSeen = "departed " + platform.Town;
                }
                TrainReset();
                return;
            }
            float speed = ENTITY.GET_ENTITY_SPEED(train);
            if (!_trainProbed)
            {
                _trainProbed = true;
                bool waiting = false;
                try { waiting = VEHICLE.IS_TRAIN_WAITING_AT_STATION(train); } catch { }
                Log.Write("TRAIN probe at " + platform.Town + ": IS_TRAIN_WAITING_AT_STATION " + waiting + ", speed " + speed.ToString("0.0") + " m/s");
            }
            if (train != _trainHandle)
            {
                _trainHandle = train;
                _trainStillSince = 0;
                _trainArrived = false;
                _trainLastSeen = "near the platform at " + platform.Town;
            }
            if (speed < Trains.StandingSpeed)
            {
                if (_trainStillSince == 0)
                    _trainStillSince = now;
                else if (!_trainArrived && now - _trainStillSince >= Trains.StandingMs)
                {
                    _trainArrived = true;
                    _trainArrivedAt = now;
                    TrainArrived(platform, train, now);
                }
            }
            else if (speed > Trains.MovingSpeed)
            {
                if (_trainArrived)
                {
                    _trainsDeparted++;
                    _trainLastSeen = "departed " + platform.Town;
                    Log.Write("TRAIN " + platform.Town + ": departed");
                    _trainArrived = false;
                }
                _trainStillSince = 0;
            }
        }

        private void TrainReset()
        {
            _trainHandle = 0;
            _trainStillSince = 0;
            _trainArrived = false;
        }

        /// A train near the platform, by the tracks (no vehicle scan): the handle, or 0.
        private int TrainNear(Spot platform)
        {
            for (int i = 0; i < Trains.MostTracks; i++)
            {
                if (!VEHICLE._DOES_TRAIN_EXIST_ON_TRACK(i))
                    continue;
                int v = VEHICLE._GET_TRAIN_VEHICLE_FROM_TRACK_INDEX(i);
                if (v == 0 || !ENTITY.DOES_ENTITY_EXIST(v))
                    continue;
                Vector3 tp = ENTITY.GET_ENTITY_COORDS(v, true, true);
                if (Spots.Distance(platform, tp.X, tp.Y) <= Trains.TrainNearMetres)
                    return v;
            }
            return 0;
        }

        /// The train stands: the platform turns to look, the keeper calls it, someone steps down, the post comes off, the leavers go.
        private void TrainArrived(Spot platform, int train, int now)
        {
            Ped me = Game.Player.Character;
            long gameNow = Bridge.Now();
            string town = platform.Town;
            Vector3 at = new Vector3(platform.X, platform.Y, platform.Z);
            _trainsArrived++;
            int looking = 0;
            Ped keeper = null;
            PostKind keeperKind = null;
            Ped stepDown = null;
            Ped leaverBody = null;
            Resident back = _engine.ReturnerDue(town, gameNow);   // a neighbour who left, back on this train (item 22): their body picked by sex, their own model first (the review of items 20-27)
            Ped backBody = null;
            bool backSameModel = false;
            List<Resident> due = _engine.DeparturesDue(town, gameNow);
            Resident leaver = due.Count > 0 && _leaver == null ? due[0] : null;
            // the leaver already walking the town in a body of their own: that body if it's near the platform, else the name card
            // comes off it before another is cast (one body with their name; the review of items 11-12)
            Ped ownBody = null;
            if (leaver != null)
            {
                foreach (KeyValuePair<int, ResBind> kv in _resBind)
                    if (kv.Value.R != null && kv.Value.R.Id == leaver.Id && ENTITY.DOES_ENTITY_EXIST(kv.Key) && !ENTITY.IS_ENTITY_DEAD(kv.Key))
                    {
                        Ped own = new Ped(kv.Key);
                        if (own.Position.DistanceTo(at) <= 60f && !_director.Involves(kv.Key) && MayDirectWhyNot(own) == null)
                            ownBody = own;
                        break;
                    }
                if (ownBody == null)
                    UnbindOthers(leaver.Id);
            }
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || p.IsDead || ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) || PED.IS_PED_IN_ANY_TRAIN(h))
                    continue;   // never the train's crew or its passengers
                float d = p.Position.DistanceTo(at);
                if (d > 40f)
                    continue;
                PostKind pk = PostKindOf(p);
                if (pk != null)
                {
                    if (keeper == null && pk.Kind == "station")
                    {
                        keeper = p;
                        keeperKind = pk;
                    }
                    continue;
                }
                if (!Natives.Bystander(p, me.Handle, IsRecord(h)))
                    continue;
                if (d <= Trains.LookMetres && looking < Trains.MostLooking)
                {
                    TASK.TASK_LOOK_AT_ENTITY(h, train, 8000, 0, 51, 0);   // a look, never a task cleared
                    looking++;
                }
                bool free = !_resBind.ContainsKey(h) && !ENTITY.IS_ENTITY_ON_SCREEN(h) && !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17) && ResidentWhy(p) == null && !_bridge.Looked(h) && d <= Trains.StepDownMetres;
                if (!free)
                    continue;
                bool woman = !PED.IS_PED_MALE(h);
                if (leaver != null && ownBody == null && leaverBody == null && woman == leaver.Woman)
                    leaverBody = p;   // (their own body near stands for them: the free ones are the arrival's)
                else if (back != null && woman == back.Woman && (backBody == null || (!backSameModel && ENTITY.GET_ENTITY_MODEL(h) == back.Model)))
                {
                    // the returner's body: the same sex as theirs, and their own model whenever a free one is about (the review of items 20-27)
                    if (backBody != null && stepDown == null)
                        stepDown = backBody;
                    backBody = p;
                    backSameModel = ENTITY.GET_ENTITY_MODEL(h) == back.Model;
                }
                else if (stepDown == null)
                    stepDown = p;
            }
            _trainLookers += looking;
            string arrival = null;
            if (keeper != null && keeperKind != null)
            {
                Resident holder = HolderAt(keeper, keeperKind);
                SpeechCaption(holder != null ? holder.Name : keeperKind.Role, Trains.ArrivalLine(keeperKind.Role, town + "|" + now / 1000), keeper.Handle, 4500, false, true);
            }
            if (back != null && backBody != null)
                arrival = StepBack(backBody, back, town, gameNow);
            else if (stepDown != null)
                arrival = StepDown(stepDown, town, gameNow);   // (a returner with no free body of their sex waits for another train)
            DeliverLetters(town, gameNow, "train");
            if (leaver != null && (ownBody ?? leaverBody) != null)
                LeaverCast(leaver, ownBody ?? leaverBody, train, at, now, gameNow, ownBody != null);
            Log.Write("TRAIN " + town + ": arrived (" + looking + " looking" + (keeper != null ? ", the keeper called it" : "") + (arrival != null ? ", arrival: " + arrival : ", nobody got off") + (leaver != null ? ((ownBody ?? leaverBody) != null ? ", leaving: " + leaver.Name : ", " + leaver.Name + " leaving but no free body for them") : "") + ")");
        }

        private int _trainsBack, _trainDoubleTakes;

        /// A free body by the platform becomes a neighbour who left and is back (Core/Engine.Trains.cs ReturnerDue): bound to their own
        /// record (their outfit only on their own model), back among the town's people; a friend near and in view does a double take.
        private string StepBack(Ped p, Resident r, string town, long gameNow)
        {
            int h = p.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            bool changed = r.Model != 0 && r.Model != model;
            if (changed)
            {
                r.Model = model;   // no body of their own model was free: the years away changed their look, and this is it from now on (the review of items 20-27)
                r.Outfit = 0;      // the outfit noted before was the old model's
            }
            UnbindOthers(r.Id);
            _resBind[h] = new ResBind { R = r, Model = model };
            if (r.Regular && r.Outfit > 0 && r.Model == model && _cfg.RegularsDress && _bridge.CanTellLooked)
            {
                PED._EQUIP_META_PED_OUTFIT_PRESET(h, r.Outfit, false);
                PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                Dressed.Note(h);   // not read for a while (Game/Dressed.cs)
            }
            _engine.ReturnedByTrain(r, town, gameNow);
            _engine.KeepResident(r, gameNow);
            _trainsBack++;
            // the double take: a bound friend near, in view, thinks of it
            Ped me = Game.Player.Character;
            foreach (string f in Trials.FriendsOf(r.Id, _store.Ties))
            {
                foreach (KeyValuePair<int, ResBind> kv in _resBind)
                    if (kv.Value.R != null && kv.Value.R.Id == f && ENTITY.DOES_ENTITY_EXIST(kv.Key) && !ENTITY.IS_ENTITY_DEAD(kv.Key))
                    {
                        Ped friend = new Ped(kv.Key);
                        if (friend.Position.DistanceTo(p.Position) > 25f || !NodInView(me, friend))
                            continue;
                        TASK.TASK_LOOK_AT_ENTITY(kv.Key, h, 5000, 0, 51, 0);
                        string thought = Trains.BackThought(Person.FirstOf(r.Name), r.Id + "|" + gameNow);
                        _ticker.Push(kv.Value.R.Name + ", thinking", thought, "near", kv.Key);
                        _trainDoubleTakes++;
                        Log.Write("TRAIN " + town + ": " + kv.Value.R.Name + " does a double take at " + r.Name + "; thinks: " + thought);
                        return r.Name + " (back from the west" + (changed ? ", a changed look" : "") + "; " + kv.Value.R.Name + " saw)";
                    }
            }
            return r.Name + " (back from the west" + (changed ? ", a changed look" : "") + ")";
        }

        /// A free body by the platform becomes someone new to the town: kin of a resident here (one time in two, when there is one), else a newcomer.
        private string StepDown(Ped p, string town, long gameNow)
        {
            int h = p.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            bool woman = !PED.IS_PED_MALE(h);
            string seed = h + "|train|" + gameNow;
            Resident r = _engine.MeetResident(model, woman, town, ResidentTrade(p, woman), seed, gameNow);
            if (r == null)
                return null;
            if (_cfg.Drummers && !woman && Drummers.Comes(seed))
            {
                // a drummer (FIVEDAY R1): his goods, his firm, two days at the hotel; never a regular
                _engine.DrummerArrived(r, town, gameNow);
                _engine.KeepResident(r, gameNow);
                _resBind[h] = new ResBind { R = r, Model = model };
                _trainNewcomers++;
                return r.Name + " (a drummer in " + Drummers.WareOf(r.Trade) + ")";
            }
            Resident kinOf = null;
            string relation = null;
            List<Resident> here = _store.Residents.Where(x => x.Regular && !x.Dead && !x.Fled && x.Town == town && x.Id != r.Id && x.Name != null).ToList();
            if (here.Count > 0 && Voice.Hash(seed + "|kin?") % 2 == 0)
            {
                kinOf = here[Voice.Hash(seed + "|who") % here.Count];
                relation = Trains.KinRelation(woman, kinOf.Woman, seed);
                string surname = Residents.SurnameOf(kinOf.Name);
                if (!string.IsNullOrEmpty(surname))
                {
                    r.Name = Person.FirstOf(r.Name) + " " + surname;
                    _engine.ForgetPerson(r.Id);   // MeetResident cached their Person under the old name
                }
            }
            int outfit = Regulars.NewOutfit(_store.Residents, town, model, _cfg.RegularsDress && _bridge.CanTellLooked ? OutfitsOf(h, model) : Regulars.PerModel, RegularsCap(town), seed);
            if (outfit >= 0)
            {
                r.Regular = true;   // the town has room: one of its regulars from today
                r.Outfit = outfit;
            }
            _engine.ArrivedByTrain(r, town, kinOf, relation, gameNow);
            _engine.KeepResident(r, gameNow);
            _resBind[h] = new ResBind { R = r, Model = model };
            _trainNewcomers++;
            return r.Name + (relation != null ? " (" + Person.FirstOf(kinOf.Name) + "'s " + relation + ")" : " (from " + r.From + ")");
        }

        /// The leaver, on a free body by the platform: a goodbye to a friend if one is bound near, then the walk to the train.
        private void LeaverCast(Resident r, Ped body, int train, Vector3 platform, int now, long gameNow, bool own)
        {
            int h = body.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            if (!own)
                _resBind[h] = new ResBind { R = r, Model = model };
            if (!own && r.Regular && r.Outfit > 0 && r.Model == model && _cfg.RegularsDress && _bridge.CanTellLooked)
            {   // a preset of their own build only, as the regulars' casting dresses them (the review of items 11-12)
                PED._EQUIP_META_PED_OUTFIT_PRESET(h, r.Outfit, false);
                PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                Dressed.Note(h);   // not read for a while (Game/Dressed.cs)
            }
            _leaver = r;
            _leaverHandle = h;
            _leaverWhy = _engine.LeavingWhy(r.Id);
            _leaverUntil = now + 25000;
            // a friend near: the warm end of a tie whose body is bound within 15 m
            Ped friend = null;
            foreach (string f in Trials.FriendsOf(r.Id, _store.Ties))
            {
                foreach (KeyValuePair<int, ResBind> kv in _resBind)
                    if (kv.Value.R != null && kv.Value.R.Id == f && ENTITY.DOES_ENTITY_EXIST(kv.Key) && new Ped(kv.Key).Position.DistanceTo(body.Position) < 15f)
                    {
                        friend = new Ped(kv.Key);
                        break;
                    }
                if (friend != null)
                    break;
            }
            Resident who = _store.FindResident(r.Id);
            if (friend != null)
            {
                ResBind fb = _resBind[friend.Handle];
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, friend.Handle, 5000, 0f, 0f, 0f);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(friend.Handle, h, 5000, 0f, 0f, 0f);
                SpeechCaption(Person.FirstOf(r.Name), Trains.Goodbye(who != null ? who.Personality : null, Person.FirstOf(fb.R.Name), r.Id + "|bye"), h, 5000, false, true);
                SpeechCaption(Person.FirstOf(fb.R.Name), Trains.Farewell(r.Id + "|fare"), friend.Handle, 4500, false, true);
                _leaverUntil = now + 32000;
            }
            Vector3 tp = ENTITY.GET_ENTITY_COORDS(train, true, true);
            // a point beside the rails, 3.5 m from the engine's own coordinates toward the platform: never onto the track itself
            float ox = platform.X - tp.X, oy = platform.Y - tp.Y, ol = (float)Math.Sqrt(ox * ox + oy * oy);
            if (ol > 0.5f)
            {
                tp.X += ox / ol * 3.5f;
                tp.Y += oy / ol * 3.5f;
            }
            if (own)
                TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, tp.X, tp.Y, tp.Z, 1f, -1, 2f, 0, 0f);
            Log.Write("TRAIN " + r.Town + ": " + r.Name + " on the platform to leave (" + _leaverWhy + ")" + (friend != null ? ", a goodbye to " + _resBind[friend.Handle].R.Name : ", nobody to say goodbye to"));
        }

        /// The walk given up (TrainTime switched off mid-walk): the body is the game's again and the resident stays in town.
        private void LeaverAbort()
        {
            int h = _leaverHandle;
            _resBind.Remove(h);
            if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
            {
                TASK.CLEAR_PED_TASKS(h, true, false);
                WanderLater(h, Game.GameTime + 500);
            }
            Log.Write("TRAIN " + _leaver.Town + ": " + _leaver.Name + "'s walk to the train given up (TrainTime off); they stay");
            _leaver = null;
            _leaverHandle = 0;
        }

        /// The leaver walks to the train; at it, or when the time's up, they're gone for good and the body is the game's again.
        private void LeaverStep(int now)
        {
            if (_leaver == null)
                return;
            int h = _leaverHandle;
            bool there = ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h);
            bool atTrain = there && _trainHandle != 0 && ENTITY.DOES_ENTITY_EXIST(_trainHandle) && new Ped(h).Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(_trainHandle, true, true)) < 6f;
            if (there && now < _leaverUntil && !atTrain)
                return;
            long gameNow = Bridge.Now();
            _engine.Departed(_leaver, gameNow, true, _leaverWhy);
            _trainsDeparted++;
            _resBind.Remove(h);
            if (there)
            {
                TASK.CLEAR_PED_TASKS(h, true, false);
                WanderLater(h, now + 500);   // the body is the game's again, going its own way out of the town's story
            }
            _leaver = null;
            _leaverHandle = 0;
        }

        /// Letters waiting for the town, given over: at the post office on entering, or off the train.
        private void DeliverLetters(string town, long gameNow, string via)
        {
            if (!_cfg.Letters || town == null || !PostTowns.Contains(town))
                return;
            List<Paper> waiting = _store.Papers.Where(p => !p.Delivered && (p.Town == null || p.Town == town)).ToList();
            if (waiting.Count == 0)
                return;
            foreach (Paper p in waiting)
            {
                p.Delivered = true;
                p.Town = town;
                p.At = gameNow;
            }
            string what = waiting.Count == 1 ? "a letter" : Voice.Number(waiting.Count) + " letters";
            string from = waiting.Count == 1 && waiting[0].From != null ? ", from " + waiting[0].From : "";
            if (via == "train")
                News("Off the train", "The mail sack had " + what + " for you" + from + ". [" + _readerKey + "] to read.");
            else
                News("Post office", "The clerk in " + town + " had " + what + " for you" + from + ". [" + _readerKey + "] to read.");
            Log.Write("LETTERS collected in " + town + " (" + via + "): " + string.Join(", ", waiting.Select(p => p.Title)));
        }

        private string TrainsStatus()
        {
            Vector3 pos = Game.Player.Character.Position;
            Spot platform = Spots.Nearest(_spots, null, "platform", pos.X, pos.Y);
            return "neighbours back off the train " + _trainsBack + " (" + _trainDoubleTakes + " double takes); " + (platform != null ? "nearest platform " + platform.Town + " " + (int)Spots.Distance(platform, pos.X, pos.Y) + " m" : "no platform spot known (the spot key, or come out of a station)") +
                   "; last train: " + _trainLastSeen + (_trainHandle != 0 ? " (one at the platform now" + (_trainArrived ? ", arrived" : "") + ")" : "") +
                   "; this session: " + _trainsArrived + " arrivals, " + _trainsDeparted + " departures, " + _trainNewcomers + " stepped down, " + _trainLookers + " looked" +
                   (_leaver != null ? "; " + _leaver.Name + " on the platform to leave" : "") + "; engine: " + _engine.TrainsEngineStatus();
        }
    }
}
