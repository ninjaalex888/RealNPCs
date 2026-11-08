// Drummers in game (FIVEDAY reserve R1; Core/Drummers.cs, Core/Engine.Drummers.cs). Every ten seconds in a town: if a store's
// keeper (the counter post, PostKind "store") is within 60 m of you and a drummer of this town is bound to a body within
// 80 m of the keeper, free to be sent, he walks over (go-to-entity, the keeper as the target; once a day each). At the counter
// (2.6 m): his pitch (his firm, his goods, the town), the keeper's answer by his manner (orders, haggles, or sends him off),
// the drummer's thought after, and the Around town box when there's an order. Then he's let go. Session only: who called
// today. The Testing row casts a named man near as the drummer for the call, without changing who he is. Behind
// [World] Drummers.
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
        private sealed class DrumWalk
        {
            public Ped Drummer, Keeper;
            public string Name, KeeperName, Ware, Firm, Id, Manner;
            public int Until;
            public bool Test;
        }

        private DrumWalk _drum;
        private int _drumNextLook, _drumCalls, _drumOrders;
        private string _drumWhyNot;
        private readonly Dictionary<string, long> _drumCalledDay = new Dictionary<string, long>();   // drummer id -> the game day he called

        private void DrummerStep(int now)
        {
            if (_drum != null)
            {
                if (_cfg == null || !_cfg.Drummers)
                    EndDrummer("[World] Drummers switched off");
                else
                    Safe("drummer", () => DrummerWalkStep(now));
                return;
            }
            if (_cfg == null || !_cfg.Drummers || _store == null || _placeTown == null || now < _drumNextLook)
                return;
            _drumNextLook = now + 10000;
            Safe("drummer", () => DrummerLook(now, false));
        }

        /// The store's keeper near you, and a drummer of the town near him (or, for the test, any named man near): sent over.
        private string DrummerLook(int now, bool test)
        {
            if (_placeTown == null)
                return _drumWhyNot = "stand in a town with a store";
            Ped me = Game.Player.Character;
            Ped keeper = null;
            PostKind kk = null;
            foreach (Ped p in Peds.All())
            {
                if (p.IsDead || p.Position.DistanceTo(me.Position) > 60f)
                    continue;
                PostKind pk = PostKindOf(p);
                if (pk != null && pk.Kind == "store")
                {
                    keeper = p;
                    kk = pk;
                    break;
                }
            }
            if (keeper == null)
                return _drumWhyNot = "no store keeper within 60 m of you";
            long day = Bridge.Now() / 1440;
            Ped body = null;
            Resident who = null;
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                Resident r = kv.Value.R;
                long d;
                if (r == null || r.Dead || r.Left || (!test && (!Drummers.Is(r.Trade) || r.Town != _placeTown || _drumCalledDay.TryGetValue(r.Id, out d) && d == day)))
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(kv.Key) || ENTITY.IS_ENTITY_DEAD(kv.Key) || !PED.IS_PED_MALE(kv.Key))
                    continue;
                Ped p = new Ped(kv.Key);
                if (p.Handle == keeper.Handle || p.Position.DistanceTo(keeper.Position) > Drummers.CallFromMetres || MayDirectWhyNot(p) != null || NotFree(kv.Key) || _director.Involves(kv.Key) || PED.IS_PED_ON_MOUNT(kv.Key))
                    continue;
                body = p;
                who = r;
                break;
            }
            if (body == null)
                return _drumWhyNot = test ? "no named man near the store to stand for the drummer" : "no drummer in town near the store";
            Resident holder = HolderAt(keeper, kk);
            MindWho kw;
            string keeperTemper = holder != null ? holder.Personality : _mindWho.TryGetValue(keeper.Handle, out kw) && kw.P != null ? kw.P.Personality : null;
            string keeperId = holder != null ? holder.Id : "store|" + _placeTown;
            _drum = new DrumWalk
            {
                Drummer = body, Keeper = keeper, Name = who.Name, KeeperName = holder != null ? holder.Name : kk.Role, Id = who.Id, Test = test,
                Ware = Drummers.WareOf(who.Trade) ?? Drummers.Ware(who.Id), Firm = Drummers.Firm(who.Id), Until = now + 45000,
                Manner = Exchanges.Manner(new Someone { Id = keeperId, Temper = keeperTemper }, false)
            };
            if (!test)
                _drumCalledDay[who.Id] = day;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(body.Handle, true);
            TASK.CLEAR_PED_TASKS(body.Handle, true, false);
            TASK.TASK_GO_TO_ENTITY(body.Handle, keeper.Handle, 45000, Drummers.AtCounter - 0.6f, 1.3f, 0f, 0);
            Log.Write("DRUMMER " + who.Name + " (" + _drum.Ware + ", " + _drum.Firm + ") makes for the store in " + _placeTown + (test ? " (a test)" : ""));
            return _drumWhyNot = null;
        }

        private void DrummerWalkStep(int now)
        {
            DrumWalk w = _drum;
            if (!w.Drummer.Exists() || w.Drummer.IsDead || !w.Keeper.Exists() || w.Keeper.IsDead || now > w.Until || MISC.GET_MISSION_FLAG())
            {
                EndDrummer("he never reached the counter");
                return;
            }
            if (w.Drummer.Position.DistanceTo(w.Keeper.Position) > Drummers.AtCounter || _director.Busy)
                return;
            _drum = null;
            string outcome = Drummers.Outcome(w.Manner), seed = w.Id + "|" + Bridge.Now() / 1440;
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(w.Drummer.Handle, w.Keeper.Handle, 3000, 0f, 0f, 0f);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(w.Keeper.Handle, w.Drummer.Handle, 3000, 0f, 0f, 0f);
            SpeechCaption(Person.FirstOf(w.Name) ?? "The drummer", Drummers.Pitch(w.Firm, w.Ware, _placeTown, seed), w.Drummer.Handle, 5500);
            SpeechCaption(w.KeeperName != null ? Person.FirstOf(w.KeeperName) : "The storekeeper", Drummers.Answer(outcome, seed), w.Keeper.Handle, 4500);
            _ticker.Push((w.Name ?? "The drummer") + ", thinking", Drummers.AfterThought(outcome, seed), "near", w.Drummer.Handle);
            if (outcome == "order" && !w.Test)
                NewsPush(_placeTown, "The store in " + _placeTown + " has put in an order for " + w.Ware + " with a drummer for " + w.Firm + ".", "news");
            _drumCalls++;
            if (outcome == "order")
                _drumOrders++;
            Log.Write("DRUMMER " + w.Name + " pitches " + w.Ware + " to " + (w.KeeperName ?? "the storekeeper") + ": " + outcome + " (" + w.Manner + ")");
            DrummerLetGo(w);
        }

        private void DrummerLetGo(DrumWalk w)
        {
            if (w == null || w.Drummer == null || !w.Drummer.Exists() || w.Drummer.IsDead)
                return;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(w.Drummer.Handle, false);
            TASK.TASK_WANDER_STANDARD(w.Drummer.Handle, 10f, 10);
        }

        private void EndDrummer(string why)
        {
            DrumWalk w = _drum;
            _drum = null;
            DrummerLetGo(w);
            if (w != null && why != null)
                Log.Write("DRUMMER " + w.Name + ": " + why);
        }

        private string DrummerStatus()
        {
            if (_cfg == null || !_cfg.Drummers)
                return "off ([World] Drummers)";
            return _engine.DrummersStatus() + "; " + _drumCalls + " calls at a store this session, " + _drumOrders + " orders" + (_drumWhyNot != null ? "; none now: " + _drumWhyNot : "");
        }
    }
}
