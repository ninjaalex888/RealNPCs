// The slate at the store in game (FIVEDAY reserve R2; Core/Slate.cs). Every twenty seconds in a town, when the store's
// keeper (PostKind "store") is within 25 m of you and the last scene here was ninety seconds ago or more, a third of the
// time: one of the named people near (within 18 m of the keeper, free, on foot, once a day each) walks up to the counter.
// On Saturday afternoon (payday) he pays his slate down. Otherwise he asks to put it on the slate, and the keeper answers by
// what's owed (Slate.OwedCents: their own this week) and his manner (Exchanges.Manner of his holder's temper). Refused, he
// thinks of the shame, and for ten seconds J at the counter lets you pay what he owes: your cash down, his thanks and the
// keeper's, a "charity" happening (the kindness seen by those near; with enRAGE, its own fondness for one of its townsfolk),
// and his thought after. Who's been paid off is kept for the session. Behind [Scenes] StoreSlate.
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
        private sealed class SlateRun
        {
            public Ped Who, Keeper;
            public string Id, Name, KeeperName, Manner;
            public int Owed, Until;
            public bool Payday, Test;
        }

        private SlateRun _slate;
        private TownEvent _slateAsk;                    // the one refused, for the J menu (a TownEvent of Kind "slate", not kept)
        private int _slateNextLook, _slateLastAt = -100000, _slateScenes, _slatePaidOff;
        private string _slateWhyNot;
        private readonly Dictionary<string, long> _slateDay = new Dictionary<string, long>();   // id -> the game day they came to the counter
        private readonly HashSet<string> _slateSquared = new HashSet<string>();                 // ids you paid off this session (by "id|week")

        private int _slateWalkAt;

        private void SlateStep(int now)
        {
            if (_slate != null)
            {
                if (_cfg == null || !_cfg.StoreSlate)
                    EndSlate("[Scenes] StoreSlate switched off");
                else if (now >= _slateWalkAt || _slateWalkAt - now > 250)
                {
                    _slateWalkAt = now + 250;   // (TENHOUR item 1)
                    Safe("slate", () => SlateWalkStep(now));
                }
                return;
            }
            if (_cfg == null || !_cfg.StoreSlate || _store == null || _placeTown == null || now < _slateNextLook)
                return;
            _slateNextLook = now + Slate.LookMs;
            if (now - _slateLastAt < Slate.RestMs || _rng.Next(100) >= Slate.Chance)
                return;
            Safe("slate", () => SlateLook(now, false));
        }

        private string SlateLook(int now, bool test)
        {
            if (_placeTown == null)
                return _slateWhyNot = "stand in a town with a store";
            Ped me = Game.Player.Character;
            Ped keeper = null;
            PostKind kk = null;
            foreach (Ped p in Peds.All())
            {
                if (p.IsDead || p.Position.DistanceTo(me.Position) > Slate.KeeperNearYou)
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
                return _slateWhyNot = "no store keeper within 25 m of you";
            long g = Bridge.Now(), day = g / 1440;
            Ped who = null;
            MindWho ww = null;
            foreach (int h in _mindNear)
            {
                MindWho w;
                long d;
                if (h == keeper.Handle || !_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (!test && _slateDay.TryGetValue(w.Id, out d) && d == day)
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(keeper.Position) > Slate.CustomerFromKeeper || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || _staged.Contains(h) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                who = p;
                ww = w;
                break;
            }
            if (who == null)
                return _slateWhyNot = "no named customer by the counter";
            string wk;
            int ws;
            bool wg;
            string worry = InnerLife.WorryNow(ww.P, day, out wk, out ws, out wg) ? wk : null;
            bool wellOff = InnerLife.TradeGroup(ww.P.Trade) == "gentry";
            Resident holder = HolderAt(keeper, kk);
            MindWho kw;
            string keeperTemper = holder != null ? holder.Personality : _mindWho.TryGetValue(keeper.Handle, out kw) && kw.P != null ? kw.P.Personality : null;
            int owed = _slateSquared.Contains(ww.Id + "|" + day / 7) ? 0 : Slate.OwedCents(ww.Id, day, worry, wellOff);
            if (test && owed < Slate.ChalkUnderCents)
                owed = Slate.FullOverCents;   // the Testing row shows the refusal and the J menu
            _slate = new SlateRun
            {
                Who = who, Keeper = keeper, Id = ww.Id, Name = ww.Name, KeeperName = holder != null ? holder.Name : kk.Role, Owed = owed, Until = now + 30000, Test = test,
                Payday = !test && Payday.Now(InnerLife.Weekday, CLOCK.GET_CLOCK_HOURS()) && owed > 0,
                Manner = Exchanges.Manner(new Someone { Id = holder != null ? holder.Id : "store|" + _placeTown, Temper = keeperTemper }, false)
            };
            _slateDay[ww.Id] = day;
            _slateLastAt = now;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(who.Handle, true);
            TASK.CLEAR_PED_TASKS(who.Handle, true, false);
            TASK.TASK_GO_TO_ENTITY(who.Handle, keeper.Handle, 30000, Slate.CounterMetres - 0.6f, 1.2f, 0f, 0);
            Log.Write("SLATE " + ww.Name + " goes to the counter in " + _placeTown + " (owes " + Slate.Dollars(owed) + (worry != null ? ", worried over " + worry : "") + ")" + (test ? " (a test)" : ""));
            return _slateWhyNot = null;
        }

        private void SlateWalkStep(int now)
        {
            SlateRun r = _slate;
            if (!r.Who.Exists() || r.Who.IsDead || !r.Keeper.Exists() || r.Keeper.IsDead || now > r.Until || MISC.GET_MISSION_FLAG())
            {
                EndSlate("he never reached the counter");
                return;
            }
            if (r.Who.Position.DistanceTo(r.Keeper.Position) > Slate.CounterMetres || _director.Busy)
                return;
            _slate = null;
            string seed = r.Id + "|" + Bridge.Now() / 1440, label = Person.FirstOf(r.Name) ?? "A customer", keeperLabel = r.KeeperName != null ? Person.FirstOf(r.KeeperName) : "The storekeeper";
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Who.Handle, r.Keeper.Handle, 3000, 0f, 0f, 0f);
            _slateScenes++;
            if (r.Payday)
            {
                SpeechCaption(label, Slate.PayDown(seed), r.Who.Handle, 4500);
                SpeechCaption(keeperLabel, Slate.AnswerLine("chalk", r.Owed, seed + "|pd"), r.Keeper.Handle, 3500);
                Log.Write("SLATE " + r.Name + " pays down " + Slate.Dollars(r.Owed) + " on payday");
                SlateLetGo(r);
                return;
            }
            string answer = Slate.Answer(r.Owed, r.Manner);
            SpeechCaption(label, Slate.Ask(seed), r.Who.Handle, 4000);
            SpeechCaption(keeperLabel, Slate.AnswerLine(answer, r.Owed, seed), r.Keeper.Handle, 4500);
            Log.Write("SLATE " + r.Name + " asks for credit: " + answer + " (" + Slate.Dollars(r.Owed) + ", the keeper " + r.Manner + ")");
            if (answer != "chalk")
            {
                _ticker.Push((r.Name ?? "Someone") + ", thinking", Slate.RefusedThought(seed), "near", r.Who.Handle);
                _slateAsk = new TownEvent { Kind = Slate.Kind, Town = _placeTown, AId = r.Id, AName = r.Name, Count = r.Owed, BName = r.KeeperName };
                _slateAskWho = r.Who;
                _slateAskKeeper = r.Keeper;
                PromptSet(Slate.Kind, _slateAsk, Slate.PromptMs);
            }
            SlateLetGo(r);
        }

        private Ped _slateAskWho, _slateAskKeeper;

        /// J: "Pay what they owe": the cash, the thanks, the kindness (FIVEDAY R2).
        private void SlatePay(TownEvent e)
        {
            if (e == null || e.Kind != Slate.Kind)
                return;
            int cash = MONEY._MONEY_GET_CASH_BALANCE();
            if (cash < e.Count)
            {
                Hint("You haven't the money.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(e.Count);
            long day = Bridge.Now() / 1440;
            _slateSquared.Add(e.AId + "|" + day / 7);
            string seed = e.AId + "|paid|" + day;
            Ped me = Game.Player.Character, who = _slateAskWho;
            int h = who != null && who.Exists() ? who.Handle : 0;
            if (h != 0)
            {
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                SpeechCaption(Person.FirstOf(e.AName) ?? "He", Slate.Thanks(seed), h, 4500, true);
                _ticker.Push((e.AName ?? "Someone") + ", thinking", Slate.PaidThought(seed), "near", h);
            }
            if (_slateAskKeeper != null && _slateAskKeeper.Exists())
                SpeechCaption(e.BName != null ? Person.FirstOf(e.BName) : "The storekeeper", Slate.KeeperPaid(seed), _slateAskKeeper.Handle, 3500);
            RecordEvent("charity", "player", "you", e.AId, e.AName, me.Position, me.Handle, me.Handle, h, "paid their slate at the store");   // a kindness, seen by those near
            string acq = FolkIdOf(e.AId);
            if (acq != null && _bridge != null)
                _bridge.LikeMore(acq, 20);   // enRAGE's own fondness for one of its townsfolk
            _slatePaidOff++;
            Log.Write("SLATE you paid " + e.AName + "'s slate: " + Slate.Dollars(e.Count));
            _slateAsk = null;
        }

        private void SlateLetGo(SlateRun r)
        {
            if (r == null || r.Who == null || !r.Who.Exists() || r.Who.IsDead)
                return;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r.Who.Handle, false);
        }

        private void EndSlate(string why)
        {
            SlateRun r = _slate;
            _slate = null;
            SlateLetGo(r);
            if (r != null && r.Who.Exists() && !r.Who.IsDead)
                TASK.CLEAR_PED_TASKS(r.Who.Handle, true, false);
            if (r != null && why != null)
                Log.Write("SLATE " + r.Name + ": " + why);
        }

        private string SlateStatus()
        {
            if (_cfg == null || !_cfg.StoreSlate)
                return "off ([Scenes] StoreSlate)";
            return _slateScenes + " at the counter this session, " + _slatePaidOff + " slates you paid" + (_slateWhyNot != null ? "; none now: " + _slateWhyNot : "");
        }
    }
}
