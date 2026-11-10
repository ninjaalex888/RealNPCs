// The house a round from J at the bar (THREEHOUR item 3; Core/BarRound.cs). Once a second in a saloon: you on foot within 4 m of
// the barkeep (PostKind "barkeep"), two or more others at the bar, no other J prompt up, and no round from you here today: J
// offers it for a dime a head. Bought, the cash goes, the barkeep says his word, and it's the Talk round's own (HouseRoundAt in
// WestworldScript.Fondness.cs): the room cheers, each named person treated is fonder, the drunk most, and keeps a "round".
// Not offered again for 20 s if you let it pass. Behind [Scenes] BarRound. F7 "TEST fondness:" counts it; log "FOND bar round".
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _barNextLook, _barAgainAt, _barRounds;
        private Ped _barKeeper;
        private string _barKeeperName;
        private readonly HashSet<string> _barRoundDays = new HashSet<string>();   // "town|day": a round bought here today

        private void BarRoundStep(int now)
        {
            if (_cfg == null || !_cfg.BarRound || _placeKind != "saloon" || _placeTown == null || _store == null || _engine == null || now < _barNextLook)
                return;
            _barNextLook = now + BarRound.LookMs;
            if (_eventMenu != null || now < _barAgainAt || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;   // something else is offered on J just now, or this was, a moment ago
            Safe("bar round", () => BarRoundLook(now));
        }

        private void BarRoundLook(int now)
        {
            Ped me = Game.Player.Character;
            if (PED.IS_PED_ON_MOUNT(me.Handle) || _barRoundDays.Contains(BarRound.DayKey(_placeTown, _store.LastGameMin)))
                return;
            Ped keeper = null;
            PostKind kind = null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Handle == me.Handle || p.Position.DistanceTo(me.Position) > BarRound.BarMetres || !p.Exists() || p.IsDead)
                    continue;
                PostKind pk = PostKindOf(p);
                if (pk != null && pk.Kind == "barkeep")
                {
                    keeper = p;
                    kind = pk;
                    break;
                }
            }
            if (keeper == null)
                return;
            int company = HouseCompany(keeper.Handle);
            if (company < BarRound.LeastCompany)
                return;
            Resident holder = HolderAt(keeper, kind);
            _barKeeper = keeper;
            _barKeeperName = holder != null ? Person.FirstOf(holder.Name) : "The barkeep";
            _barAgainAt = now + BarRound.AgainMs;
            PromptSet(BarRound.Kind, new TownEvent { Kind = BarRound.Kind, Town = _placeTown, Count = BarRound.Cents(company), Extra = company.ToString() }, BarRound.PromptMs);
        }

        /// J, "Buy the house a round": the cash, the barkeep's word, and the round.
        private void BarRoundBuy(TownEvent e)
        {
            if (e == null || e.Kind != BarRound.Kind)
                return;
            int cash = MONEY._MONEY_GET_CASH_BALANCE();
            if (cash < e.Count)
            {
                Hint("You haven't the money.", 2500);
                return;
            }
            Ped k = _barKeeper;
            if (k == null || !k.Exists() || k.IsDead)
            {
                Hint("The barkeep's not there.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(e.Count);
            _barRoundDays.Add(BarRound.DayKey(e.Town, _store.LastGameMin));
            int now = Game.GameTime;
            SpeechCaption(_barKeeperName ?? "The barkeep", BarRound.KeeperLine(e.Town + "|" + _store.LastGameMin), k.Handle, 4000);
            HouseRoundAt(k.Handle, "barround|" + e.Town, now);
            _barRounds++;
            Log.Write("FOND bar round: you bought the house a round in " + e.Town + " for " + BarRound.Dollars(e.Count) + " (" + e.Extra + " at the bar)");
        }
    }
}
