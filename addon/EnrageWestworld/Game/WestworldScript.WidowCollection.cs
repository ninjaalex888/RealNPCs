// A collection for the widow, the game side (FOURTEENHOUR reserve R20; Core/WidowCollection.cs; [Scenes] WidowCollection). A look every
// 45 s in a town by day (nine till six), out of doors: a death of the town in the last three days not yet collected for, and a woman of
// the mind's within 25 m, in view and free, walks up to you (the friend calls' walk: timed by the distance, the word at 2.6 m). Her ask
// and J: a dollar, five, or not today. Given: the money, her thanks, the world log's charity (she remembers: the late thanks may come
// of it), the town's word, and the Sunday after the preacher's thanks from the pulpit (the news, due that morning). Once for each death.
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
        private sealed class CollectionWalk
        {
            public Ped Ped;
            public string Id, Name, DeadId, Dead, Widow, Seed;
            public int Until, SaidAt;
            public bool Chose, Test;
            public bool Plate;   // the Sunday plate, not a widow's collection (DEPTH item 11)
        }

        private int _plates, _plateGiven, _plateCents, _platePassed;

        private CollectionWalk _collection;
        private int _collectionLookAt, _collections, _collectionGiven, _collectionCents;
        private string _collectionLast;

        private void CollectionStep(int now)
        {
            CollectionWalk w = _collection;
            if (w != null)
            {
                CollectionWalkStep(w, now);
                return;
            }
            if (!WidowCollection.On || now < _collectionLookAt || _store == null || _placeTown == null || _interiorType != 0)
                return;
            _collectionLookAt = now + WidowCollection.LookMs;
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (CollectionPlate.On && CollectionPlate.Hour(InnerLife.Weekday, clock) && !MISC.GET_MISSION_FLAG() && !WalkUpGoing() && !_director.Busy && PlateBegin(now, false) == null)
                return;   // the Sunday plate after the service (DEPTH item 11)
            if (clock < WidowCollection.FromHour || clock >= WidowCollection.ToHour || MISC.GET_MISSION_FLAG() || WalkUpGoing() || _director.Busy || _store.Deaths == null)
                return;
            long g = _store.LastGameMin;
            DeathEntry dead = _store.Deaths.LastOrDefault(x => x != null && x.Town == _placeTown && x.Id != null && x.Name != null && g - x.At >= 0 && g - x.At < WidowCollection.DaysAfter * 1440L &&
                                                              !_store.Counts.ContainsKey("collection|" + x.Id));
            if (dead == null)
                return;
            CollectionBegin(dead.Id, dead.Name, now, false);
        }

        private bool CollectionBegin(string deadId, string deadName, int now, bool test)
        {
            Ped me = Game.Player.Character;
            Candidate pick = ReasonCandidates(me, now, test).Where(c => c.Who.Woman && c.Ped.Position.DistanceTo(me.Position) < 25f && !PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) && !NotFree(c.Ped.Handle) && NodInView(me, c.Ped))
                                                         .OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (pick == null)
                return false;   // nobody to take it round just now: again later
            string widow = null;
            Person dp = _engine.KinPerson(deadId);
            if (dp != null)
            {
                PersonState ds = _store.Get(dp);
                List<Relative> fam = ds != null && ds.Family != null ? ds.Family : Family.For(dp);
                Relative wife = fam != null ? fam.FirstOrDefault(r => r != null && r.Relation == "wife") : null;
                widow = wife != null ? wife.Name : null;
            }
            if (!test)
                _store.Counts["collection|" + deadId] = (int)(_store.LastGameMin / 1440);   // once for each death
            int h = pick.Ped.Handle;
            float metres = pick.Ped.Position.DistanceTo(me.Position);
            int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));   // the friend calls' walk
            PED.SET_PED_KEEP_TASK(h, true);
            TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs, 0, 51, 0);
            _collection = new CollectionWalk { Ped = pick.Ped, Id = pick.Who.Id, Name = pick.Who.Name, DeadId = deadId, Dead = deadName, Widow = widow, Seed = deadId + "|coll", Until = now + walkMs, Test = test };
            _collections++;
            Log.Write("COLLECTION " + pick.Who.Name + " takes up a collection for " + (widow ?? deadName + "'s family") + " and comes over (" + (int)metres + " m)" + (test ? " (a test)" : ""));
            return true;
        }

        /// The Sunday plate (DEPTH item 11): by the town's church within 60 m, once a town a Sunday, a churchman within 25 m
        /// of you, in view and free (the devout and the churchgoers first), walks up with it. Null when he comes, else why not.
        private string PlateBegin(int now, bool test)
        {
            Ped me = Game.Player.Character;
            Vector3 church;
            if (_placeTown == null || !Churches.TryGetValue(_placeTown, out church))
                return "a town whose church is known";
            if (me.Position.DistanceTo(church) > CollectionPlate.ChurchMetres)
                return "within " + (int)CollectionPlate.ChurchMetres + " m of the church";
            string key = "plate|" + _placeTown + "|" + _store.LastGameMin / 1440;
            if (!test && _store.Counts.ContainsKey(key))
                return "the plate has come round already today";
            Candidate pick = ReasonCandidates(me, now, test).Where(c => !c.Who.Woman && c.Ped.Position.DistanceTo(me.Position) < 25f && !PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) && !NotFree(c.Ped.Handle) && NodInView(me, c.Ped))
                                                         .OrderBy(c => c.Who.P != null && (c.Who.P.Devotion == "Devout" || c.Who.P.Devotion == "Churchgoer") ? 0 : 1).ThenBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (pick == null)
                return "a man the mind knows within 25 m, in view and free";
            if (!test)
                _store.Counts[key] = (int)(_store.LastGameMin / 1440);   // once a town a Sunday
            int h = pick.Ped.Handle;
            float metres = pick.Ped.Position.DistanceTo(me.Position);
            int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));   // the friend calls' walk, as the widow's collector
            PED.SET_PED_KEEP_TASK(h, true);
            TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs, 0, 51, 0);
            _collection = new CollectionWalk { Ped = pick.Ped, Id = pick.Who.Id, Name = pick.Who.Name, Seed = key, Until = now + walkMs, Test = test, Plate = true };
            _plates++;
            Log.Write("PLATE " + pick.Who.Name + " brings the collection plate round after the service (" + (int)metres + " m)" + (test ? " (a test)" : ""));
            return null;
        }

        private void CollectionWalkStep(CollectionWalk w, int now)
        {
            Ped me = Game.Player.Character;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0) || MISC.GET_MISSION_FLAG())
            {
                CollectionEnd("something else took her");
                return;
            }
            int h = w.Ped.Handle;
            if (w.SaidAt == 0)
            {
                if (w.Ped.Position.DistanceTo(me.Position) < 2.6f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 6000, 0f, 0f, 0f);
                    string line = w.Plate ? CollectionPlate.Asks(w.Seed) : WidowCollection.Asks(w.Dead, w.Widow, w.Seed);
                    Feed.Say(Person.FirstOf(w.Name), line, Speech.When(h, "chat", me.Handle), h, toYou: true);
                    w.SaidAt = now;
                    string kind = w.Plate ? CollectionPlate.Kind : WidowCollection.Kind;
                    if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                        PromptSet(kind, new TownEvent { Kind = kind, Town = _placeTown, AName = w.Name }, WidowCollection.PromptMs);
                }
                else if (now > w.Until)
                    CollectionEnd("she never reached you");
                return;
            }
            if (w.Chose || (now - w.SaidAt > WidowCollection.PromptMs + 2000 && !(_eventMenu != null && _eventMenu.Mode == (w.Plate ? CollectionPlate.Kind : WidowCollection.Kind))))
            {
                if (!w.Chose)
                    CollectionChoose(w.Plate ? "plate:0" : "collection:0");
                CollectionEnd(_collectionLast ?? "done");
            }
        }

        /// J's row.
        private void CollectionChoose(string id)
        {
            CollectionWalk w = _collection;
            if (w == null || w.Chose || w.SaidAt == 0)
                return;
            w.Chose = true;
            if (_eventPrompt == WidowCollection.Kind || _eventPrompt == CollectionPlate.Kind)
                PromptClear();
            if (w.Plate)
            {
                PlateChoose(w, id);   // the Sunday plate (DEPTH item 11)
                return;
            }
            int cents = id == "collection:5" ? 500 : id == "collection:1" ? 100 : 0;
            if (cents > 0 && MONEY._MONEY_GET_CASH_BALANCE() < cents)
                cents = 0;
            Ped me = Game.Player.Character;
            int h = w.Ped.Handle;
            Feed.Say(Person.FirstOf(w.Name), WidowCollection.Thanks(cents, w.Seed), Speech.When(h, cents > 0 ? "thanks" : "chat", me.Handle), h, toYou: true);
            if (cents > 0)
            {
                MONEY._MONEY_DECREMENT_CASH_BALANCE(cents);
                _collectionGiven++;
                _collectionCents += cents;
                if (!w.Test)
                {
                    RecordEvent("charity", "player", "you", w.Id, w.Name, w.Ped.Position, me.Handle, me.Handle, h, "gave for " + (w.Widow ?? w.Dead + "'s family"));   // she remembers who gave
                    long g = _store.LastGameMin;
                    string town = _placeTown;
                    int days = WidowCollection.DaysToSunday(InnerLife.Weekday);
                    Safe("collection", () => _engine.NewsAt(WidowCollection.Pulpit(w.Dead, w.Widow, town, cents, w.Seed), town, (g / 1440 + days) * 1440 + 11 * 60, g));   // the Sunday after, from the pulpit
                }
                _collectionLast = "you gave $" + (cents / 100) + " for " + (w.Widow ?? w.Dead + "'s family");
            }
            else
                _collectionLast = "not today";
            Log.Write("COLLECTION " + _collectionLast);
        }

        /// J at the plate: a dime, a dollar, or it passes. Given: his thanks, the world log's charity (seen; the late thanks reads it),
        /// and a stranger's dollar the next Sunday's sermon. Passed: one of the church near you thinks it.
        private void PlateChoose(CollectionWalk w, string id)
        {
            int cents = id == "plate:100" ? CollectionPlate.DollarCents : id == "plate:10" ? CollectionPlate.DimeCents : 0;
            if (cents > 0 && MONEY._MONEY_GET_CASH_BALANCE() < cents)
                cents = 0;
            Ped me = Game.Player.Character;
            int h = w.Ped.Handle;
            Feed.Say(Person.FirstOf(w.Name), CollectionPlate.Thanks(cents, w.Seed), Speech.When(h, cents > 0 ? "thanks" : "chat", me.Handle), h, toYou: true);
            if (cents > 0)
            {
                MONEY._MONEY_DECREMENT_CASH_BALANCE(cents);
                _plateGiven++;
                _plateCents += cents;
                if (!w.Test)
                {
                    RecordEvent("charity", "player", "you", w.Id, w.Name, w.Ped.Position, me.Handle, me.Handle, h, "put " + (cents >= 100 ? "a dollar" : "a dime") + " in the plate");   // seen: who gave
                    if (cents >= CollectionPlate.DollarCents)
                    {
                        long g = _store.LastGameMin;
                        string town = _placeTown;
                        Safe("collection", () => _engine.NewsAt(CollectionPlate.Sermon(town, w.Seed), town, (g / 1440 + 7) * 1440 + 11 * 60, g));   // the next Sunday's sermon
                    }
                }
                _collectionLast = "you put " + (cents >= 100 ? "a dollar" : "a dime") + " in the plate";
            }
            else
            {
                _platePassed++;
                _collectionLast = "you let the plate pass";
                foreach (int q in _mindNear)
                {
                    MindWho mw;
                    if (q == h || !_mindWho.TryGetValue(q, out mw) || mw.P == null || (mw.P.Devotion != "Devout" && mw.P.Devotion != "Churchgoer") || !ENTITY.DOES_ENTITY_EXIST(q) || !NodInView(me, new Ped(q)))
                        continue;
                    _ticker.Push(Person.FirstOf(mw.Name) + ", thinking", CollectionPlate.Passed(w.Seed + q), "near", q);   // seen: who didn't
                    break;
                }
            }
            Log.Write("PLATE " + _collectionLast);
        }

        private string PlateStatus()
        {
            if (!CollectionPlate.On)
                return "off ([Scenes] CollectionPlate)";
            return _plates + " plates came round, you gave to " + _plateGiven + " ($" + (_plateCents / 100.0).ToString("0.00") + "), let it pass " + _platePassed;
        }

        private void CollectionEnd(string why)
        {
            CollectionWalk w = _collection;
            _collection = null;
            if (_eventPrompt == WidowCollection.Kind || _eventPrompt == CollectionPlate.Kind)
                PromptClear();
            if (w != null && w.Ped != null && w.Ped.Exists() && !w.Ped.IsDead)
            {
                PED.SET_PED_KEEP_TASK(w.Ped.Handle, false);
                TASK.TASK_CLEAR_LOOK_AT(w.Ped.Handle);
                if (!_director.Involves(w.Ped.Handle) && !PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0))
                    WanderLater(w.Ped.Handle, Game.GameTime + 3000);
            }
            Log.Detail("COLLECTION over: " + why);
        }

        private bool CollectorWalking(int h)
        {
            CollectionWalk w = _collection;
            return w != null && w.Ped != null && w.Ped.Handle == h;
        }

        private string CollectionFire(int now, out string said)
        {
            said = null;
            if (!WidowCollection.On)
                return "off ([Scenes] WidowCollection)";
            if (_collection != null)
                return "a collection is coming round already";
            if (_placeTown == null)
                return "it wants a town";
            if (!CollectionBegin("test-dead", "Amos Teale", now, true))
                return "no woman of the town within 25 m, in view and free";
            said = _collection.Name + " comes round with a collection (a test: nothing kept)";
            return null;
        }

        private string CollectionStatus()
        {
            if (!WidowCollection.On)
                return "off ([Scenes] WidowCollection)";
            return _collections + " collections came round, you gave to " + _collectionGiven + " ($" + (_collectionCents / 100) + ")" + (_collectionLast != null ? "; last: " + _collectionLast : "");
        }
    }
}
