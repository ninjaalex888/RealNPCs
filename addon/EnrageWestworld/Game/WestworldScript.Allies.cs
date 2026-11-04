// Allies who come to your aid (a player's request, 2026-11-03, the roadmap's front item; Lex: "more interactions and NPC
// events that can add to the fondness"; [Encounters] AlliesHelp; Core/Allies.cs weighs who comes and has their words).
// Every 1.5 s: anyone in combat with you within AlliesMetres (never the law, never during a mission, never while you're
// wanted or the law has an incident on you: the townsfolk answering your crime are "in combat with you" too) is a foe.
// While foes stand, the named people near who think well of you (enRAGE's fondness, a kindness of yours they remember, a
// trouble you mended, talks and your name known; never the afraid or the aggrieved) are weighed by their own nature
// (Allies.Weigh), and the readiest comes: up out of their seat the game's way, a revolver given against guns (fists kept
// to fists), a shout as they come, straight at the nearest foe, retasked as foes fall, a small green dot on the map while
// they're with you ([Encounters] AlliesDot, AlliesDotColour; Lex, 2026-11-03). Out of the towns, with nobody you know in
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
            public Blip Dot;   // the small green dot on the map while they're with you (ours to take away)
        }

        private const int AlliesTickMs = 1500, AlliesOverMs = 6000, AlliesRoadGapMs = 300000, AlliesAskGapMs = 20000, AlliesBodyWaitMs = 15000;
        private readonly List<Ally> _allies = new List<Ally>();
        private readonly List<int> _alliesFoes = new List<int>();
        private readonly Dictionary<int, int> _alliesAskedAt = new Dictionary<int, int>();
        private int _alliesNextAt, _alliesFightSince, _alliesFoeAt, _alliesRoadAt, _alliesRoadTriedFor, _alliesCame, _alliesRoadCame, _alliesFights, _alliesFell, _alliesTestFriend, _alliesTestMan;
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
            foreach (Ally gone in _allies)
                if (!ENTITY.DOES_ENTITY_EXIST(gone.Handle))
                    AllyDotClear(gone);
            _allies.RemoveAll(a => !ENTITY.DOES_ENTITY_EXIST(a.Handle));
            // the foes: whoever is in combat with you within reach, never the law; none during a mission. While you're wanted,
            // nobody new counts (the townsfolk answering your crime are "in combat with you" too, and nobody should come to
            // shoot them: the review); the foes already engaged stay foes, so a fight the law walks into isn't called over
            // with the gunman still up (the advisor)
            _alliesFoes.Clear();
            bool armedFoe = false;
            bool wanted = PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0;
            if (!MISC.GET_MISSION_FLAG())
                foreach (Ped p in Peds.All())
                {
                    if (p == null)
                        continue;
                    int h = p.Handle;
                    if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(me.Position) > _cfg.AlliesMetres)
                        continue;
                    if (!PED.IS_PED_IN_COMBAT(h, me.Handle) || Natives.IsLaw(p) || _allies.Any(a => a.Handle == h))
                        continue;
                    if (wanted && !_alliesSeenFoes.Contains(h))
                        continue;
                    _alliesFoes.Add(h);
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
                _alliesSeenFoes.Add(f);
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
                if (a.Target == 0 || !ENTITY.DOES_ENTITY_EXIST(a.Target) || ENTITY.IS_ENTITY_DEAD(a.Target) || !_alliesFoes.Contains(a.Target))
                {
                    Vector3 at = ENTITY.GET_ENTITY_COORDS(a.Handle, true, true);
                    a.Target = _alliesFoes.OrderBy(f => ENTITY.GET_ENTITY_COORDS(f, true, true).DistanceTo(at)).First();
                    TASK.TASK_COMBAT_PED(a.Handle, a.Target, 0, 16);
                }
            }
            if (wanted || _allies.Count(a => !ENTITY.IS_ENTITY_DEAD(a.Handle)) >= _cfg.AlliesMost)
                return;   // wanted: those with you fight on, nobody new comes
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
            string lastWhy = null;
            int helping = _allies.Count(a => !ENTITY.IS_ENTITY_DEAD(a.Handle));
            foreach (Ped p in Peds.All())
            {
                if (p == null)
                    continue;
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(at) > _cfg.AlliesMetres)
                    continue;
                if (_alliesFoes.Contains(h) || _allies.Any(a => a.Handle == h) || Natives.IsLaw(p) || Natives.IsChild(p) || Natives.StoryModel(p) || !IsGrownUp(p))
                    continue;
                if (Spawner.IsOurs(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) ||
                    _director.Involves(h) || IsStaged(h) || MindGettingUp(h) || InTalk(h) || MayDirectWhyNot(p) != null)
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
                AllyTake take = Allies.Weigh(t, w.P != null ? w.P.Personality : null, !PED.IS_PED_MALE(h), InHisCups(p, w.P), warmth, armedFoe, WEAPON.IS_PED_ARMED(h, 4), _alliesFoes.Count, helping,
                                             w.Id + "|" + _alliesFightSince);
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
                }
            }
            if (bestTake == null)
            {
                if (lastWhy != null)
                    _alliesWhyNot = lastWhy;
                return null;
            }
            best = AllyStart(bestPed, bestWho.Id, bestWho.Name, armedFoe, false, now, bestTake.Why);
            return best;
        }

        /// How warm this person is to you, from everything kept: enRAGE's fondness, a kindness of yours lately, a trouble you
        /// mended, talks and your name known; nothing for the afraid or the aggrieved. The Testing row's friend counts as one.
        private int AllyWarmth(Ped p, MindWho w)
        {
            float fear, like, anger;
            if (!_bridge.FeelingsOf(p.Handle, out fear, out like, out anger))
                fear = like = anger = 0f;
            string regard = SightRegard(p, w.Id);
            bool against = regard == "afraid" || regard == "hostile" || fear >= 40f || anger >= 40f;
            long g = _store.LastGameMin;
            bool kind = _store.WorldLog != null && Interact.KindLately(_store.WorldLog, w.Id, g);
            bool mended = _store.WorrySettled != null && _store.WorrySettled.ContainsKey(w.Id);
            PersonState st = _store.Find(w.Id);
            int warmth = Allies.Warmth(like, _cfg.AlliesLiking, kind, mended, st != null ? st.Talks : 0, st != null && st.KnowsName, against);
            if (p.Handle == _alliesTestFriend && !against)
                warmth = Math.Max(warmth, 2);
            return warmth;
        }

        /// They come: up out of whatever they're at the game's way, a revolver against guns (fists against fists), a shout,
        /// a green dot on the map, and at the nearest foe.
        private Ally AllyStart(Ped p, string id, string name, bool armedFoe, bool road, int now, string why)
        {
            int h = p.Handle, me = Game.Player.Character.Handle;
            Vector3 at = p.Position;
            int target = _alliesFoes.OrderBy(f => ENTITY.GET_ENTITY_COORDS(f, true, true).DistanceTo(at)).First();
            var a = new Ally { Handle = h, Id = id, Name = name, Target = target, Since = now, Road = road };
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);    // fights even against the armed
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);   // always fights
            if (armedFoe && !WEAPON.IS_PED_ARMED(h, 4))
            {
                p.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                a.GivenGun = true;
            }
            else if (!armedFoe)
                WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);   // fists against fists, as those who join your fistfight (JoinYou.cs): a gun of their own stays put (the review)
            ScenarioExit.SoftUp(h, x => TASK.TASK_COMBAT_PED(x, target, 0, 16));   // up the game's way and at them the same tick
            Speech.SayTag(h, "fight", target);
            SpeechCaption(Person.FirstOf(name), Allies.Shout(armedFoe, road, id + "|" + now), h, 2500, true);
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

        /// A small green dot on the map on each ally while they're with you (Lex, 2026-11-03: "light green mini dots on the
        /// minimap, similar to the mini white dots"): the game's own companion dot (the small dot the enemy's is, in the
        /// companions' colour), recoloured by [Encounters] AlliesDotColour (a blip modifier's name; empty keeps the
        /// companions' own colour). Someone who already has a dot (enRAGE's, a regular's) keeps their own.
        private void AllyDot(Ally a)
        {
            if (!_cfg.AlliesDot)
                return;
            try
            {
                if (MAP.GET_BLIP_FROM_ENTITY(a.Handle) != 0)
                    return;
                a.Dot = new Blip(MAP.BLIP_ADD_FOR_ENTITY(Natives.Hash("BLIP_STYLE_COMPANION"), a.Handle));
                if (!string.IsNullOrEmpty(_cfg.AlliesDotColour))
                    MAP.BLIP_ADD_MODIFIER(a.Dot.Handle, Natives.Hash(_cfg.AlliesDotColour.Trim()));
                a.Dot.Label = Person.FirstOf(a.Name) + ", with you";
            }
            catch (Exception ex)
            {
                a.Dot = null;
                Log.Write("ALLIES the dot on " + a.Name + " failed: " + ex.Message);
            }
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

        /// The script going away (OnAborted): the dots go with it.
        private void AlliesAbort()
        {
            foreach (Ally a in _allies)
                AllyDotClear(a);
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
                if (Allies.Warmth(0f, 0, kind, mended, st != null ? st.Talks : 0, st != null && st.KnowsName, _engine.SawYouViolent(r.Id, 3)) >= 2 && !r.Woman)
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
            _alliesPendMin = 30f;
            _alliesPendMax = 45f;
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
            }
            _resBind[h] = new ResBind { R = friend, Model = friend.Model };
            _encounterPeds.Add(h);
            _alliesRoadAt = now;
            AllyStart(p, friend.Id, friend.Name, true, true, now, "a friend from " + (friend.Town ?? "the ledger") + ", happening by");
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

        /// No foe left: those with you leave off, turn to you with a word, remember it, and think better of you for it.
        /// A friend from the road is handed back to the game; the Testing row's man too.
        private void AlliesEnd(Ped me, int now)
        {
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
                    if (a.Road)
                        AlliesLetGo(h);
                    continue;
                }
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);   // the street's own events reach them again (true left them deaf to gunfire for good: the review)
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 5000, 0f, 0f, 0f);
                bool hurt = ENTITY.GET_ENTITY_HEALTH(h) < ENTITY.GET_ENTITY_MAX_HEALTH(h, false) * 0.7f;
                Speech.SayTag(h, "relieved", me.Handle);
                SpeechCaption(Person.FirstOf(a.Name), Allies.After(ran, hurt, a.Id + "|" + now), h, 4000, true);
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
            return (_cfg.AlliesHelp ? "on" : "off ([Encounters] AlliesHelp)") + "; " + _alliesFights + " fights of yours seen, " + _alliesCame + " came to your aid (" + _alliesRoadCame + " from the road), " + _alliesFell + " fell" +
                   (_allies.Count > 0 ? "; " + _allies.Count + " with you now" : "") + (_alliesPendKind != null ? "; " + (_alliesPendKind == "test" ? "the test's man" : "a friend from the road") + " on the way" : "") +
                   (_alliesLast != null ? "; last: " + _alliesLast : "") + (_alliesWhyNot != null ? "; held back lately: " + _alliesWhyNot : "");
        }
    }
}
