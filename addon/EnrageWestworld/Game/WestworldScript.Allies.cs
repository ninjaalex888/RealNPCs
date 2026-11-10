// Allies who come to your aid (a player's request, 2026-11-03, the roadmap's front item; Lex: "more interactions and NPC
// events that can add to the fondness"; [Encounters] AlliesHelp; Core/Allies.cs weighs who comes and has their words).
// Every 1.5 s: anyone in combat with you within AlliesMetres (the law too, and the townsfolk answering your crime: Lex,
// 2026-11-03, a friend "would now protect the main character if fought by the law or anyone else"; never during a mission)
// is a foe. While foes stand, the named people near who think well of you (enRAGE's fondness, a kindness of yours they
// remember, a trouble you mended, someone you saved, talks and your name known; never the afraid or the aggrieved) are
// weighed by their own nature (Allies.Weigh: someone who owes you, the bar at AlliesLiking or more, comes whatever the
// odds; a friend who has already bolted from the shooting may turn back, since nearly everyone bolts at the first shot and
// Lex's log showed seven coming against fists and one against guns), and the readiest comes: up out of their seat the game's way, a revolver given against guns (fists kept to
// fists), a shout as they come, straight at the nearest foe, retasked as foes fall, a small green dot on the map
// (the one friends near you wear anyway: WestworldScript.FriendDots.cs; [Encounters] AlliesDot, AlliesDotColour). Out of the towns, with nobody you know in
// sight and two or more armed against you, a friend from a town's ledger may happen by (AlliesRoadChance): made 30-45 m
// off in their own clothes as soon as the game has their body streamed in, and run in. When no foe is left for six
// seconds it's over: they turn to you with a word (your words, in their voice with the voice server), remember it as
// standing up for you (the mind's "defend" memory: their thoughts say so for days), enRAGE's townsfolk think better of
// you for it, and they go on their way (a friend from the road handed back to the game). An ally who falls is logged; the
// death ledger has the rest. Testing row "allies" (a revolver man made 14-20 m off comes at you once his body is in; the
// nearest named person counts as your friend); F7 "TEST allies:"; log "ALLIES".
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
        private sealed class Ally
        {
            public int Handle, Target, Since;
            public string Id, Name;
            public bool Road, GivenGun;
            public bool Turned;   // had bolted from the shooting and turned back: kept deaf to the gunfire until it's over, and their word after says so
            public Blip Dot;   // the small green dot on the map while they're with you (ours to take away)
        }

        private const int AlliesTickMs = 1500, AlliesOverMs = 6000, AlliesRoadGapMs = 300000, AlliesAskGapMs = 20000, AlliesBodyWaitMs = 15000, AlliesHorseWaitMs = 6000;   // the horse's body waited for this long past the rider's, then on foot (the review)
        private readonly List<Ally> _allies = new List<Ally>();
        private readonly List<int> _alliesFoes = new List<int>();
        private bool _alliesFoesLaw;   // one of the foes is the law: no lawman comes against his own (Lex, 2026-11-04: lawmen may be friends)
        private readonly Dictionary<int, int> _alliesAskedAt = new Dictionary<int, int>();
        private int _alliesNextAt, _alliesFightSince, _alliesFoeAt, _alliesRoadAt, _alliesRoadTriedFor, _alliesCame, _alliesRoadCame, _alliesFights, _alliesFell, _alliesTestFriend, _alliesTestMan, _alliesRode, _alliesGrieved;
        private const ulong SetPedOntoMount = 0x028F76B6E78246EB, TaskDismountAnimal = 0x48E92D3DDE23C23A;   // by hash: the API's wrappers differ between builds (the 0.7.7 lesson)

        /// The friend from the road on their way in on horseback (SIXDAY item 2; [Encounters] AlliesRide).
        private sealed class AllyRide
        {
            public Ped Rider, Horse;
            public Resident Friend;
            public int Since, CheckedAt, DismountAt;
            public float LastD;
            public bool Dismounting;
        }

        private AllyRide _alliesRide;
        private uint _alliesPendHorse;
        private string _alliesPendHorseName;   // the horse's model by name, for the log (the review: the arrival line named another seed's)
        private string _alliesLast, _alliesWhyNot;
        // a body on its way (the Testing row's revolver man, a friend from the road): made when the game has their model
        // streamed in. Spawner.Spawn only asks for a model it hasn't got and makes nobody that call (Lex, 2026-11-03: the
        // row said "the model isn't loaded", which read as the AI model)
        private string _alliesPendKind;   // "test" or "road"; null: nobody on the way
        private uint _alliesPendModel;
        private float _alliesPendMin, _alliesPendMax;
        private int _alliesPendSince, _alliesPendNextAt;
        private Resident _alliesPendFriend;

        private void AlliesStep(int now)
        {
            if (_cfg == null || !_cfg.AlliesHelp)
                return;
            if (_alliesPendKind != null && now >= _alliesPendNextAt)
            {
                _alliesPendNextAt = now + 300;
                AlliesPendingStep(now);
            }
            if (now < _alliesNextAt)
                return;
            _alliesNextAt = now + AlliesTickMs;
            try
            {
                AlliesTick(now);
            }
            catch (Exception ex)
            {
                _alliesNextAt = now + 60000;
                Log.Write("ALLIES failed once (" + ex.Message + "); nothing for a minute");
            }
        }

        private void AlliesTick(int now)
        {
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            if (_alliesRide != null)
                AlliesRideStep(me, now);   // the friend from the road on horseback
            foreach (Ally gone in _allies)
                if (!ENTITY.DOES_ENTITY_EXIST(gone.Handle))
                    AllyDotClear(gone);
            _allies.RemoveAll(a => !ENTITY.DOES_ENTITY_EXIST(a.Handle));
            // the foes: whoever is in combat with you within reach, the law and the townsfolk answering your crime among them
            // (Lex, 2026-11-03: friends protect you "if fought by the law or anyone else"); none during a mission
            _alliesFoes.Clear();
            _alliesFoesLaw = false;
            bool armedFoe = false;
            if (!MISC.GET_MISSION_FLAG())
                foreach (Ped p in Peds.All())
                {
                    if (p == null)
                        continue;
                    int h = p.Handle;
                    if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(me.Position) > Math.Max(_cfg.AlliesMetres, Allies.FoeMetres))
                        continue;   // (farther than a friend will come from: gunmen open up at range, and a fight not seen has nobody to help in it)
                    if (!PED.IS_PED_IN_COMBAT(h, me.Handle) || _allies.Any(a => a.Handle == h))
                        continue;
                    _alliesFoes.Add(h);
                    if (Natives.IsLaw(p))
                        _alliesFoesLaw = true;
                    if (WEAPON.IS_PED_ARMED(h, 4))
                        armedFoe = true;
                }
            foreach (int f in _alliesSeenFoes)
                if (ENTITY.DOES_ENTITY_EXIST(f) && ENTITY.IS_ENTITY_DEAD(f))
                    _alliesFoesFell = true;   // one of them fell this fight: the word after says won, not ran
            if (_alliesFoes.Count == 0)
            {
                if (_alliesFightSince != 0 && now - _alliesFoeAt > AlliesOverMs)
                    AlliesEnd(me, now);
                return;
            }
            foreach (int f in _alliesFoes)
                if (_alliesSeenFoes.Add(f))
                    AlliesNoteFoe(f);
            _alliesFoeAt = now;
            if (_alliesFightSince == 0)
            {
                _alliesFightSince = now;
                _alliesFights++;
            }
            // those with you keep at it: a fallen foe's hunter goes for the next
            foreach (Ally a in _allies)
            {
                if (ENTITY.IS_ENTITY_DEAD(a.Handle))
                    continue;
                bool bolted = PED.IS_PED_FLEEING(a.Handle);   // the gunfire got to them after all: put back in the fight, and deaf to it now
                if (bolted)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(a.Handle, true);
                    a.Turned = true;
                    _alliesKeptIn++;
                }
                if (bolted || a.Target == 0 || !ENTITY.DOES_ENTITY_EXIST(a.Target) || ENTITY.IS_ENTITY_DEAD(a.Target) || !_alliesFoes.Contains(a.Target))
                {
                    Vector3 at = ENTITY.GET_ENTITY_COORDS(a.Handle, true, true);
                    a.Target = _alliesFoes.OrderBy(f => ENTITY.GET_ENTITY_COORDS(f, true, true).DistanceTo(at)).First();
                    if (bolted)
                        TASK.CLEAR_PED_TASKS(a.Handle, true, false);
                    TASK.TASK_COMBAT_PED(a.Handle, a.Target, 0, 16);
                }
            }
            if (_allies.Count(a => !ENTITY.IS_ENTITY_DEAD(a.Handle)) >= _cfg.AlliesMost)
                return;
            // who comes: the readiest of those near who think well of you
            Ally came = AlliesLook(me, armedFoe, now);
            if (came != null)
                return;
            // out of the towns, nobody you know in sight: a friend from the ledger may happen by, once a fight and not within five minutes
            if (_placeTown == null && _allies.Count == 0 && _alliesPendKind == null && _alliesFoes.Count >= 2 && armedFoe && _cfg.AlliesRoadChance > 0 && _alliesRoadTriedFor != _alliesFightSince)
            {
                _alliesRoadTriedFor = _alliesFightSince;
                if (now - _alliesRoadAt >= AlliesRoadGapMs && _rng.Next(100) < _cfg.AlliesRoadChance)
                    AlliesRoadFriend(me, now);
            }
        }

        /// The people near who could come: named, free, weighed by how they stand with you and their own nerve; the best comes.
        private Ally AlliesLook(Ped me, bool armedFoe, int now)
        {
            Vector3 at = me.Position;
            Ally best = null;
            AllyTake bestTake = null;
            Ped bestPed = null;
            MindWho bestWho = null;
            bool bestFleeing = false;
            string lastWhy = null;
            int helping = _allies.Count(a => !ENTITY.IS_ENTITY_DEAD(a.Handle));
            foreach (Ped p in Peds.All())
            {
                if (p == null)
                    continue;
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(at) > _cfg.AlliesMetres)
                    continue;
                if (_alliesFoes.Contains(h) || _allies.Any(a => a.Handle == h) || (Natives.IsLaw(p) && _alliesFoesLaw) || Natives.IsChild(p) || Natives.StoryModel(p) || !IsGrownUp(p))
                    continue;   // (a lawman who thinks well of you comes against anyone but the law; Lex, 2026-11-04)
                // Fleeing doesn't rule them out here (a player, 2026-11-04: friends never came). Nearly everyone in the street bolts or
                // cowers at the first shot and the game reports both as fleeing, so this used to leave nobody to come in a gunfight
                // (Lex's own log: seven came against fists, one against guns). Weigh decides who turns back.
                if (Spawner.IsOurs(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_RAGDOLL(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) ||
                    _director.Involves(h) || IsStaged(h) || MindGettingUp(h) || InTalk(h) || MayDirectWhyNot(p, true) != null)
                    continue;
                int asked;
                if (_alliesAskedAt.TryGetValue(h, out asked) && now - asked < AlliesAskGapMs && now >= asked)
                    continue;   // weighed a moment ago: the answer stands a while
                MindWho w = MindKnown(h, now);
                if (w == null || w.Id == null || w.Src == null)
                    continue;
                if (_alliesAskedAt.Count > 300)
                    _alliesAskedAt.Clear();
                _alliesAskedAt[h] = now;
                int warmth = AllyWarmth(p, w);
                if (warmth <= 0)
                    continue;
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                bool fleeing = PED.IS_PED_FLEEING(h);
                AllyTake take = Allies.Weigh(t, w.P != null ? w.P.Personality : null, !PED.IS_PED_MALE(h), InHisCups(p, w.P), warmth, armedFoe, WEAPON.IS_PED_ARMED(h, 4), _alliesFoes.Count, helping,
                                             w.Id + "|" + _alliesFightSince, fleeing);
                if (!take.Comes)
                {
                    lastWhy = w.Name + ": " + take.Why;
                    continue;
                }
                if (bestTake == null || take.Score > bestTake.Score)
                {
                    bestTake = take;
                    bestPed = p;
                    bestWho = w;
                    bestFleeing = fleeing;
                }
            }
            if (bestTake == null)
            {
                if (lastWhy != null)
                    _alliesWhyNot = lastWhy;
                return null;
            }
            best = AllyStart(bestPed, bestWho.Id, bestWho.Name, armedFoe, false, now, bestTake.Why, bestFleeing);
            return best;
        }

        /// How warm this person is to you, from everything kept: enRAGE's fondness, a kindness of yours lately, a trouble you
        /// mended, talks and your name known; nothing for the afraid or the aggrieved. The Testing row's friend counts as one.
        private readonly HashSet<int> _friendHeldLogged = new HashSet<int>();   // a full bar held back from friendship, said once a body

        private int AllyWarmth(Ped p, MindWho w)
        {
            float fear, like, anger;
            if (!_bridge.FeelingsOf(p.Handle, out fear, out like, out anger))
                fear = like = anger = 0f;
            string regard = SightRegard(p, w.Id);
            bool against = Allies.Against(regard, fear, anger, like, _cfg.AlliesLiking);   // fear doesn't outrank a full bar; a grudge does (Lex, 2026-11-04: Margaret)
            long g = _store.LastGameMin;
            bool kind = _store.WorldLog != null && Interact.KindLately(_store.WorldLog, w.Id, g);
            bool mended = _store.WorrySettled != null && _store.WorrySettled.ContainsKey(w.Id);
            bool saved = _store.WorldLog != null && Interact.SavedLately(_store.WorldLog, w.Id, g);   // someone you saved owes you, whatever else they feel (WestworldScript.Saved.cs)
            PersonState st = _store.Find(w.Id);
            int warmth = Allies.Warmth(like, _cfg.AlliesLiking, kind, mended, st != null ? st.Talks : 0, st != null && st.KnowsName, against, saved);
            if (warmth < 2 && _cfg.AlliesLiking > 0 && like >= _cfg.AlliesLiking && _friendHeldLogged.Add(p.Handle))
            {
                if (_friendHeldLogged.Count > 300)
                    _friendHeldLogged.Clear();
                Log.Write("FRIENDS " + (w.Name ?? "someone") + ": the bar at " + (int)like + " and no friend: " + (regard ?? "no regard") + ", enRAGE's fear " + (int)fear + ", anger " + (int)anger + " (once a body)");
            }
            if (p.Handle == _alliesTestFriend && !against)
                warmth = Math.Max(warmth, 2);
            return warmth;
        }

        /// They come: up out of whatever they're at the game's way, a revolver against guns (fists against fists), a shout,
        /// a green dot on the map, and at the nearest foe.
        private Ally AllyStart(Ped p, string id, string name, bool armedFoe, bool road, int now, string why, bool turned = false)
        {
            int h = p.Handle, me = Game.Player.Character.Handle;
            Vector3 at = p.Position;
            int target = _alliesFoes.OrderBy(f => ENTITY.GET_ENTITY_COORDS(f, true, true).DistanceTo(at)).First();
            var a = new Ally { Handle = h, Id = id, Name = name, Target = target, Since = now, Road = road, Turned = turned };
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, turned);   // someone who bolted once would bolt again at the next shot: deaf to it until it's over (AlliesEnd lets it go)
            if (turned)
                _alliesTurned++;
            LendFists(h, "an ally");   // fights even against the armed, always fights; their own back once it's over (FOURTEENHOUR item 3)
            if (armedFoe && !WEAPON.IS_PED_ARMED(h, 4))
            {
                p.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                a.GivenGun = true;
            }
            else if (!armedFoe)
                WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);   // fists against fists, as those who join your fistfight (JoinYou.cs): a gun of their own stays put (the review)
            ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, target, 0, 16));   // up the game's way and at them the same tick
            Speech.SayTag(h, "fight", target);
            SpeechCaption(Person.FirstOf(name), Allies.Shout(armedFoe, road, id + "|" + now, turned), h, 2500, true);
            AllyDot(a);
            _allies.Add(a);
            _alliesCame++;
            if (road)
                _alliesRoadCame++;
            _alliesLast = name + (road ? " (from the road)" : "") + " against " + _alliesFoes.Count + " (" + why + ")";
            Log.Write("ALLIES " + name + (road ? " happens by and" : "") + " comes to your aid against " + _alliesFoes.Count + (armedFoe ? " (guns out" + (a.GivenGun ? ", given a revolver" : "") + ")" : " (fists)") + ": " + why);
            StoryNoticed("you:allies", 3, 0f);
            return a;
        }

        /// An ally who died standing with you: those who knew them hear of it and think the less of you (our affection and
        /// their memory through the engine; enRAGE's fondness through its own method), the town has it by evening,
        /// and you're told now (SIXDAY item 2; Core/Engine.Allies.cs).
        private void AllyFell(Ally a, int now)
        {
            if (!_cfg.AlliesGrieved || a == null || a.Id == null || _engine == null)
                return;
            try
            {
                Feed.Notice("RealNPCs", Allies.FellCaption(a.Name));
                Resident home = a.Road && a.Id != null && Resident.IsResident(a.Id) ? _store.FindResident(a.Id) : null;   // the friend from the road fell out of any town: their own town hears (the review)
                List<KeyValuePair<string, bool>> mourners = _engine.AllyFell(a.Id, a.Name, _placeTown ?? (home != null ? home.Town : null), _store.LastGameMin, YourFullName());
                int folk = 0;
                foreach (KeyValuePair<string, bool> m in mourners)
                {
                    string acq = FolkIdOf(m.Key);
                    if (acq == null)
                        continue;
                    _bridge.LikeMore(acq, -Allies.FellLiking(m.Value, _cfg.AlliesGrievedLiking));   // enRAGE's own method for its own townsfolk, by id (their bodies may be anywhere)
                    folk++;
                }
                _alliesGrieved++;
                Log.Write("ALLIES " + a.Name + "'s death costs you: " + mourners.Count + " who knew them think the less of you (" + folk + " of enRAGE's townsfolk, -" + _cfg.AlliesGrievedLiking + " a friend); the town will talk of it this evening");
            }
            catch (Exception ex)
            {
                Log.Write("ALLIES the death's cost couldn't be counted for " + a.Name + ": " + ex.Message);
            }
        }

        /// enRAGE's own id for one of its townsfolk from the add-on's lasting id ("folk:<id>"), null for anyone else.
        private static string FolkIdOf(string lastingId)
        {
            return lastingId != null && lastingId.StartsWith("folk:", StringComparison.Ordinal) && lastingId.Length > 5 ? lastingId.Substring(5) : null;
        }

        /// A small green dot on the map on each ally while they're with you (Lex, 2026-11-03: "light green mini dots on the
        /// minimap, similar to the mini white dots"): the game's own companion dot (the small dot the enemy's is, in the
        /// companions' colour), recoloured by [Encounters] AlliesDotColour (a blip modifier's name; empty keeps the
        /// companions' own colour). Someone who already has a dot (enRAGE's, a regular's) keeps their own.
        private void AllyDot(Ally a)
        {
            if (!_cfg.AlliesDot)
                return;
            a.Dot = GreenDot(a.Handle, Person.FirstOf(a.Name) + ", with you");   // the friends' dot (WestworldScript.FriendDots.cs); null when they wear one already
        }

        private static void AllyDotClear(Ally a)
        {
            if (a == null || a.Dot == null)
                return;
            try
            {
                if (a.Dot.Exists())
                    a.Dot.Delete();
            }
            catch { }
            a.Dot = null;
        }

        /// The script going away (OnAborted): the dots go with it, the friends' too.
        private void AlliesAbort()
        {
            foreach (Ally a in _allies)
            {
                AllyDotClear(a);
                if (a.Turned && ENTITY.DOES_ENTITY_EXIST(a.Handle))
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(a.Handle, false);   // never left deaf to the street
            }
            FriendDotsClear();
        }

        /// A friend from a town's ledger happens by: someone who thinks well of you (a kindness remembered, a trouble mended,
        /// talks), not dead, not fled, not cast now; their body asked for now and made 30-45 m off in their own clothes as soon
        /// as the game has it (AlliesPendingStep), and sent in.
        private void AlliesRoadFriend(Ped me, int now)
        {
            long g = _store.LastGameMin;
            var fond = new List<Resident>();
            foreach (Resident r in _store.Residents)
            {
                if (r == null || r.Dead || r.Fled || r.Model == 0 || r.Id == null || _resBind.Values.Any(b => b != null && b.R == r))
                    continue;
                PersonState st = _store.Find(r.Id);
                bool kind = _store.WorldLog != null && Interact.KindLately(_store.WorldLog, r.Id, g);
                bool mended = _store.WorrySettled != null && _store.WorrySettled.ContainsKey(r.Id);
                bool saved = _store.WorldLog != null && Interact.SavedLately(_store.WorldLog, r.Id, g);
                if (Allies.Warmth(0f, 0, kind, mended, st != null ? st.Talks : 0, st != null && st.KnowsName, _engine.SawYouViolent(r.Id, 3), saved) >= 2 && !r.Woman)
                    fond.Add(r);
            }
            if (fond.Count == 0)
            {
                _alliesWhyNot = "nobody in the ledger who'd come (no friend of yours there)";
                return;
            }
            Resident friend = fond[_rng.Next(fond.Count)];
            bool ready = Spawner.Ready(friend.Model);   // streaming starts now
            _alliesPendKind = "road";
            _alliesPendModel = friend.Model;
            _alliesPendHorse = 0;
            if (_cfg.AlliesRide)
            {
                _alliesPendHorseName = Allies.HorseModel(friend.Id + "|" + now);
                _alliesPendHorse = Natives.Hash(_alliesPendHorseName);
                if (!Spawner.Ready(_alliesPendHorse))   // the horse's model asked for beside the rider's; made only if it's in by then
                    Log.Detail("ALLIES the horse's body is streaming in");
            }
            _alliesPendMin = _cfg.AlliesRide ? Allies.RideMinMetres : 30f;
            _alliesPendMax = _cfg.AlliesRide ? Allies.RideMaxMetres : 45f;
            _alliesPendSince = now;
            _alliesPendFriend = friend;
            Log.Write("ALLIES " + friend.Name + " of " + (friend.Town ?? "the ledger") + " is on the way from the road (" + (ready ? "body ready" : "body streaming in") + ")");
        }

        /// A body on its way: made as soon as the game has the model streamed in and a spot off the road to put them,
        /// given up after fifteen seconds (or, for the friend from the road, when the fight is over before they get there).
        private void AlliesPendingStep(int now)
        {
            string kind = _alliesPendKind;
            try
            {
                if (now - _alliesPendSince > AlliesBodyWaitMs || now < _alliesPendSince)
                {
                    _alliesPendKind = null;
                    _alliesWhyNot = "the game didn't stream " + (kind == "test" ? "the man's" : "the friend's") + " body in within 15 s";
                    Log.Write("ALLIES " + (kind == "test" ? "the test's man" : "the friend from the road") + " never came: " + _alliesWhyNot);
                    if (kind == "test")
                        Feed.Notice("RealNPCs", "the allies test: the game never streamed the man's body in. Try the row again.");
                    return;
                }
                if (kind == "road" && _alliesFightSince == 0)
                {
                    _alliesPendKind = null;   // the fight ended before they got here
                    return;
                }
                if (!Spawner.Ready(_alliesPendModel) || !Spawner.SafeToSpawn())
                    return;
                if (_alliesPendHorse != 0 && !Spawner.Ready(_alliesPendHorse))
                {
                    // the rider's body is in, the horse's not yet: both are waited for (the review: the friend was made on foot 45-70 m off the
                    // moment the rider's model came), the horse given up after AlliesHorseWaitMs and the walk shortened to a foot arrival's
                    if (now - _alliesPendSince < AlliesHorseWaitMs)
                        return;
                    STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(_alliesPendHorse);
                    _alliesPendHorse = 0;
                    _alliesPendMin = 30f;
                    _alliesPendMax = 45f;
                    Log.Write("ALLIES " + (_alliesPendFriend != null ? _alliesPendFriend.Name + "'s" : "the friend's") + " horse (" + _alliesPendHorseName + ") never streamed in within " + AlliesHorseWaitMs / 1000 + " s: on foot");
                }
                Ped me = Game.Player.Character;
                if (me == null || !me.Exists())
                    return;
                Vector3 at;
                if (!EncounterSpot(me, _alliesPendMin, _alliesPendMax, out at))
                    return;   // nowhere unseen to put them just now: again next tick
                Ped p = Spawner.Spawn(_alliesPendModel, at, 0f);
                if (p == null)
                {
                    _alliesPendKind = null;
                    _alliesWhyNot = "the game made nobody (" + (Spawner.LastSpawnWhy ?? "no reason given") + ")";
                    Log.Write("ALLIES " + (kind == "test" ? "the test's man" : "the friend from the road") + ": " + _alliesWhyNot);
                    if (kind == "test")
                        Feed.Notice("RealNPCs", "the allies test: " + _alliesWhyNot + ".");
                    return;
                }
                _alliesPendKind = null;
                if (kind == "test")
                    AlliesTestMan(p, me, now);
                else
                    AlliesRoadArrive(p, _alliesPendFriend, now);
            }
            catch (Exception ex)
            {
                _alliesPendKind = null;
                Log.Write("ALLIES the body on its way failed (" + ex.Message + ")");
            }
        }

        /// The friend from the road is here: themselves, as the town knows them, and in.
        private void AlliesRoadArrive(Ped p, Resident friend, int now)
        {
            int h = p.Handle;
            if (friend == null || _alliesFoes.Count == 0)
            {
                Spawner.Release(p, true);   // nothing left to come to
                return;
            }
            if (friend.Regular || friend.Outfit > 0)
            {
                PED._EQUIP_META_PED_OUTFIT_PRESET(h, friend.Outfit, false);   // themselves, as the town knows them
                PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                Dressed.Note(h);   // not read for a while (Game/Dressed.cs)
            }
            _resBind[h] = new ResBind { R = friend, Model = friend.Model };
            _encounterPeds.Add(h);
            _alliesRoadAt = now;
            if (_cfg.AlliesRide && _alliesPendHorse != 0 && AlliesRideIn(p, friend, now))
                return;   // on horseback: in once they're down off it (AlliesRideStep)
            AllyStart(p, friend.Id, friend.Name, true, true, now, "a friend from " + (friend.Town ?? "the ledger") + ", happening by");
        }

        /// The friend on horseback: a horse made under them where they stand (unseen), the two sent your way at a canter;
        /// off the horse near you and in on foot (AlliesRideStep). False when no horse could be made: in on foot as before.
        private bool AlliesRideIn(Ped p, Resident friend, int now)
        {
            Ped horse = null;
            try
            {
                if (!Spawner.Ready(_alliesPendHorse))
                {
                    Log.Write("ALLIES " + friend.Name + "'s horse never streamed in: on foot");
                    return false;
                }
                Vector3 at = p.Position;
                float heading = ENTITY.GET_ENTITY_HEADING(p.Handle);
                horse = Spawner.Spawn(_alliesPendHorse, at, heading);
                if (horse == null)
                {
                    Log.Write("ALLIES no horse for " + friend.Name + " (" + (Spawner.LastSpawnWhy ?? "the game made none") + "): on foot");
                    return false;
                }
                Function.Call(SetPedOntoMount, p.Handle, horse.Handle, -1, true);
                Ped me = Game.Player.Character;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                TASK.TASK_GO_TO_ENTITY(p.Handle, me.Handle, 30000, 6f, 2.5f, 0f, 0);   // the rider's order drives the mount (checked: no nearer in 8 s and they're down and running)
                _alliesRide = new AllyRide { Rider = p, Horse = horse, Friend = friend, Since = now, CheckedAt = now, LastD = at.DistanceTo(me.Position) };
                Speech.SayTag(p.Handle, "fight", me.Handle);
                SpeechCaption(Person.FirstOf(friend.Name), Allies.Shout(true, true, friend.Id + "|ride|" + now), p.Handle, 2500, true);
                Log.Write("ALLIES " + friend.Name + " rides in from " + (int)_alliesRide.LastD + " m off (" + (_alliesPendHorseName ?? "a horse") + ")");
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("ALLIES the ride failed (" + ex.Message + "): on foot");
                _alliesRide = null;
                if (horse != null)
                {
                    try
                    {
                        if (PED.IS_PED_ON_MOUNT(p.Handle))
                            Function.Call(TaskDismountAnimal, p.Handle, 0, 0, 0, 0, 0);
                        Spawner.Release(horse, true);   // a horse already made is given back, not left a mission entity till shutdown (the review)
                    }
                    catch { }
                }
                return false;
            }
        }

        /// Every allies tick while a friend rides in: down off the horse near you (or when the horse won't come, or the
        /// fight's over), then in on foot as any ally; the horse given back to the game.
        private void AlliesRideStep(Ped me, int now)
        {
            AllyRide r = _alliesRide;
            if (r == null)
                return;
            try
            {
                if (r.Rider == null || !r.Rider.Exists() || r.Rider.IsDead)
                {
                    AlliesRideEnd("the rider's gone", false);
                    return;
                }
                int h = r.Rider.Handle;
                if (r.Dismounting)
                {
                    if (!PED.IS_PED_ON_MOUNT(h) || now - r.DismountAt > 4000)
                    {
                        AlliesRideEnd(null, true);   // down (or stuck on the horse: let go of it anyway): in on foot
                        if (_alliesFoes.Count > 0)
                            AllyStart(r.Rider, r.Friend.Id, r.Friend.Name, true, true, now, "a friend from " + (r.Friend.Town ?? "the ledger") + ", riding in");
                        else
                            AlliesLetGo(h);   // the fight ended on the way: the game's again
                    }
                    return;
                }
                float d = r.Rider.Position.DistanceTo(me.Position);
                bool mounted = PED.IS_PED_ON_MOUNT(h);
                bool near = d <= Allies.DismountMetres;
                bool stuck = now - r.CheckedAt >= Allies.RideNoProgressMs && d > r.LastD - 5f;
                bool over = _alliesFoes.Count == 0 && _alliesFightSince == 0;
                if (near || stuck || over || !mounted || now - r.Since > Allies.RideMostMs)
                {
                    if (mounted)
                        Function.Call(TaskDismountAnimal, h, 0, 0, 0, 0, 0);
                    r.Dismounting = true;
                    r.DismountAt = now;
                    Log.Write("ALLIES " + r.Friend.Name + (near ? " is here" : stuck ? "'s horse won't come" : over ? ": the fight's over" : !mounted ? " never got on the horse" : " rode long enough") + ": down and " + (over ? "away" : "in on foot") + " (" + (int)d + " m)");
                    return;
                }
                if (now - r.CheckedAt >= Allies.RideNoProgressMs)
                {
                    r.CheckedAt = now;
                    r.LastD = d;
                    TASK.TASK_GO_TO_ENTITY(h, me.Handle, 30000, 6f, 2.5f, 0f, 0);   // the order again, in case the game took it
                }
            }
            catch (Exception ex)
            {
                Log.Write("ALLIES the ride step failed (" + ex.Message + ")");
                AlliesRideEnd("it failed", false);
            }
        }

        /// The horse given back to the game (it wanders off; never deleted in sight), the ride forgotten.
        private void AlliesRideEnd(string why, bool rodeIn)
        {
            AllyRide r = _alliesRide;
            _alliesRide = null;
            if (r == null)
                return;
            if (rodeIn)
                _alliesRode++;
            try
            {
                if (r.Horse != null && r.Horse.Exists())
                    Spawner.Release(r.Horse, false);
            }
            catch
            {
            }
            if (why != null)
            {
                Log.Write("ALLIES the ride ends: " + why);
                if (r.Rider != null && r.Rider.Exists() && !_allies.Any(a => a.Handle == r.Rider.Handle))
                    AlliesLetGo(r.Rider.Handle);
            }
        }

        /// The Testing row's man is here: a revolver, and at you.
        private void AlliesTestMan(Ped m, Ped me, int now)
        {
            m.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
            MakeHostile(m, now);
            TASK.TASK_COMBAT_PED(m.Handle, me.Handle, 0, 16);
            _encounterPeds.Add(m.Handle);
            _alliesTestMan = m.Handle;
            _alliesNextAt = 0;
            Log.Write("ALLIES test: a revolver man made " + (int)m.Position.DistanceTo(me.Position) + " m off comes at you");
        }

        // Whose people fought you, for the log once a fight is over (a player, 2026-11-06: a robber opens up in town and the townsfolk join
        // him). RealNPCs sends nobody at you in a gunfight, and enRAGE and the game each have their own rules for townsfolk; this
        // says whose people they were, so the next log can tell.
        private int _alliesFoeLaw, _alliesFoeRecords, _alliesFoeFolk;
        private string _alliesFoeFolkNames;

        private void AlliesNoteFoe(int h)
        {
            Ped p = new Ped(h);
            if (Natives.IsLaw(p))
                _alliesFoeLaw++;
            else if (IsRecord(h))
                _alliesFoeRecords++;
            else
            {
                _alliesFoeFolk++;
                if (_alliesFoeFolk <= 6)
                {
                    MindWho w = MindKnown(h, Game.GameTime);
                    _alliesFoeFolkNames = (_alliesFoeFolkNames == null ? "" : _alliesFoeFolkNames + ", ") + (w != null && w.Name != null ? w.Name : "someone unnamed");
                }
            }
        }

        /// No foe left: those with you leave off, turn to you with a word, remember it, and think better of you for it.
        /// A friend from the road is handed back to the game; the Testing row's man too.
        private void AlliesEnd(Ped me, int now)
        {
            Log.Write("ALLIES the fight is over: at you were " + _alliesFoeLaw + " of the law, " + _alliesFoeRecords + " of enRAGE's people, " + _alliesFoeFolk + " others" + (_alliesFoeFolkNames != null ? " (" + _alliesFoeFolkNames + ")" : "") + "; " + _allies.Count + " stood with you");
            _alliesFoeLaw = _alliesFoeRecords = _alliesFoeFolk = 0;
            _alliesFoeFolkNames = null;
            int fell = 0;
            bool ran = !_alliesFoesFell;
            foreach (Ally a in _allies)
            {
                int h = a.Handle;
                AllyDotClear(a);
                if (!ENTITY.DOES_ENTITY_EXIST(h))
                    continue;
                if (ENTITY.IS_ENTITY_DEAD(h))
                {
                    fell++;
                    _alliesFell++;
                    Log.Write("ALLIES " + a.Name + " fell standing with you");
                    AllyFell(a, now);   // grieved by those who knew them, at a cost to you ([Encounters] AlliesGrieved)
                    if (a.Road)
                        AlliesLetGo(h);
                    continue;
                }
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);   // the street's own events reach them again (true left them deaf to gunfire for good: the review)
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 5000, 0f, 0f, 0f);
                bool hurt = ENTITY.GET_ENTITY_HEALTH(h) < ENTITY.GET_ENTITY_MAX_HEALTH(h, false) * 0.7f;
                Speech.SayTag(h, "relieved", me.Handle);
                SpeechCaption(Person.FirstOf(a.Name), Allies.After(ran, hurt, a.Id + "|" + now, a.Turned), h, 4000, true);
                // what they keep of it: stood up for you (the mind's defend memory: their thoughts say so for days)
                try
                {
                    PersonState st = _store.Find(a.Id);
                    if (st != null)
                    {
                        if (st.Mind == null)
                            st.Mind = new List<MindMemory>();   // a state from an older save
                        string you = _engine.KnowsYourName(a.Id) && YourFullName() != null ? Person.FirstOf(YourFullName()) : "the stranger";
                        Minds.Note(st.Mind, new MindMemory
                        {
                            Kind = "warmth", Sub = "defend", Who = new List<string> { "player" }, Names = new List<string> { you }, Town = _placeTown, At = _store.LastGameMin,
                            How = "did", Role = "doer", Feel = 2, Strength = 60, Key = "ally:" + a.Id + ":" + _alliesFightSince
                        }, _store.LastGameMin);
                    }
                    FolkRef ac = _bridge.AcquaintanceAt(h);
                    if (ac != null && ac.Id != null)
                        _bridge.LikeMore(ac.Id, 10);   // enRAGE's own townsfolk think better of you for standing together
                    RecordEvent("defend", a.Id, a.Name, "player", "you", ENTITY.GET_ENTITY_COORDS(h, true, true), h, h, me.Handle, Allies.MemoryWords(YourFullName()));
                }
                catch (Exception ex)
                {
                    Log.Detail("ALLIES the memory couldn't be kept for " + a.Name + ": " + ex.Message);
                }
                WanderLater(h, now + 9000);
                if (a.Road)
                    AlliesLetGo(h);
                Log.Write("ALLIES " + a.Name + " stood with you to the end" + (hurt ? ", hurt" : "") + (a.GivenGun ? ", and keeps the revolver" : "") + "; they remember it");
            }
            _allies.Clear();
            _alliesSeenFoes.Clear();
            _alliesFightSince = 0;
            _alliesFoesFell = false;
            _alliesTestFriend = 0;
            if (_alliesTestMan != 0)
            {
                AlliesLetGo(_alliesTestMan);   // the Testing row's man: the game's again, dead or alive (a mission entity held for good leaked: the review)
                _alliesTestMan = 0;
            }
        }

        /// One we made (a friend from the road, the Testing row's man) handed back to the game: it clears them when
        /// they're far, as it does its own people; a bound resident's body keeps its name while it's about (ResidentOf
        /// drops the bind when the handle is someone else's).
        private static void AlliesLetGo(int h)
        {
            try
            {
                if (ENTITY.DOES_ENTITY_EXIST(h))
                    Spawner.Release(new Ped(h), false);
            }
            catch (Exception ex)
            {
                Log.Detail("ALLIES letting " + h + " go failed: " + ex.Message);
            }
        }

        private int _alliesTurned, _alliesKeptIn;   // friends who turned back from running; those put back in the fight when the gunfire got to them
        private bool _alliesFoesFell;   // a foe died this fight (the word after says "won" rather than "ran")
        private readonly HashSet<int> _alliesSeenFoes = new HashSet<int>();

        /// The Testing row: a revolver man's body is asked for and he is made 14-20 m off as soon as the game has it
        /// (AlliesPendingStep), and comes at you; the nearest named person counts as your friend.
        private string AlliesTest(int now, out string said)
        {
            said = null;
            if (!_cfg.AlliesHelp)
                return "off ([Encounters] AlliesHelp)";
            if (MISC.GET_MISSION_FLAG())
                return "a mission is running";
            if (_alliesPendKind != null)
                return "someone is already on their way";
            Ped me = Game.Player.Character;
            Ped friend = null;
            float bestD = _cfg.AlliesMetres;
            foreach (Ped p in Peds.All())
            {
                if (p == null)
                    continue;
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || Natives.IsLaw(p) || Natives.IsChild(p) || Natives.StoryModel(p) || !IsGrownUp(p))
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d >= bestD || Spawner.IsOurs(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || _director.Involves(h) || InTalk(h) || MayDirectWhyNot(p) != null)
                    continue;
                MindWho w = MindKnown(h, now);
                if (w == null || w.Id == null)
                    continue;
                bestD = d;
                friend = p;
            }
            if (friend == null)
                return "nobody named and free within " + _cfg.AlliesMetres + " m to stand with you";
            Vector3 at;
            if (!EncounterSpot(me, 14f, 20f, out at))
                return "no spot 14-20 m off to make the man at";
            uint model = Spawner.ModelFor(null, false, _rng.Next());
            bool ready = Spawner.Ready(model);   // streaming starts now; he is made when it's in
            _alliesPendKind = "test";
            _alliesPendModel = model;
            _alliesPendMin = 14f;
            _alliesPendMax = 20f;
            _alliesPendSince = now;
            _alliesPendFriend = null;
            _alliesTestFriend = friend.Handle;
            MindWho fw = MindKnown(friend.Handle, now);
            string who = fw != null && fw.Name != null ? fw.Name : "the nearest person";
            said = "a revolver man comes at you" + (ready ? "" : " in a moment (the game is streaming his body in)") + "; " + who + " (" + (int)bestD + " m) counts as your friend and should come";
            Log.Write("ALLIES test: a revolver man is on his way (" + (ready ? "body ready" : "body streaming in") + "); " + who + " counts as your friend");
            return null;
        }

        private string AlliesStatus()
        {
            return (_cfg.AlliesHelp ? "on" : "off ([Encounters] AlliesHelp)") + "; " + _alliesFights + " fights of yours seen, " + _alliesCame + " came to your aid (" + _alliesRoadCame + " from the road, " + _alliesRode + " on horseback, " + _alliesTurned + " turned back from running; " + _alliesKeptIn + " times one was put back in the fight), " + _alliesFell + " fell (" + _alliesGrieved + " grieved at a cost to you)" + (_alliesRide != null ? ", one riding in now" : "") +
                   (_allies.Count > 0 ? "; " + _allies.Count + " with you now" : "") + (_alliesPendKind != null ? "; " + (_alliesPendKind == "test" ? "the test's man" : "a friend from the road") + " on the way" : "") +
                   (_alliesLast != null ? "; last: " + _alliesLast : "") + (_alliesWhyNot != null ? "; held back lately: " + _alliesWhyNot : "");
        }
    }
}
