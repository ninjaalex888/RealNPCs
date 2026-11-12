// A gift that fits, game side (LIVES item 4c; [Scenes] FittingGifts; Core/FittingGifts.cs): in the town's general store, J
// once a visit lists up to three of the town fond of you with the gift that fits each; bought, it's in your pocket for two
// game days, and a few steps from them J gives it: their answer by nature, a kindness (charity) they and those near see.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class GiftInHand
        {
            public string ToId, ToName, Item, Temper, Town;
            public long Until;
        }

        private GiftInHand _gift;
        private bool _giftOffered;   // offered while they're near: not again till you step away
        private List<Resident> _giftFor = new List<Resident>();
        private int _giftNext, _giftsGiven;
        private string _giftStoreVisit;   // the town of this store visit: the offer once a visit
        private readonly Dictionary<string, long> _giftLastTo = new Dictionary<string, long>();   // their last gift, by id (game minutes)

        private void FittingGiftsStep(int now)
        {
            if (!FittingGifts.On || !_cfg.FittingGifts || _store == null || _engine == null || now < _giftNext || !PromptFree())
                return;
            _giftNext = now + 3000;
            long g = _store.LastGameMin;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            if (_gift != null && g > _gift.Until)
            {
                Log.Write("GIFT: " + _gift.Item + " for " + _gift.ToName + " never given");
                _gift = null;
            }
            if (_gift != null)
            {
                bool near = false;
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || (w.Id != _gift.ToId && _engine.WorryIdOf(w.Id) != _gift.ToId) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                        continue;
                    if (new Ped(h).Position.DistanceTo(me.Position) > FittingGifts.GiveMetres)
                        continue;
                    near = true;
                    if (_giftOffered)
                        return;   // once an approach (the review)
                    _giftOffered = true;
                    PromptSet(FittingGifts.GiveKind, new TownEvent { Kind = FittingGifts.GiveKind, Town = _placeTown, AId = _gift.ToId, AName = _gift.ToName, Extra = _gift.Item }, FittingGifts.PromptMs);
                    return;
                }
                if (!near)
                    _giftOffered = false;   // stepped away: offered again next time
                return;
            }
            if (_placeKind != "genstore" || _placeTown == null)
            {
                if (_placeKind != "genstore")
                    _giftStoreVisit = null;   // out of the store: the next visit offers again
                return;
            }
            if (_giftStoreVisit == _placeTown)
                return;
            _giftStoreVisit = _placeTown;
            _giftFor = _store.Residents.Where(r => r != null && r.Town == _placeTown && !_engine.HasDied(r.Id))
                                       .Select(r => new { r, st = StateOrNull(r.Id) })
                                       .Where(x => x.st != null && x.st.Affection >= FittingGifts.LeastAffection)
                                       .OrderByDescending(x => x.st.Affection).Take(FittingGifts.Most).Select(x => x.r).ToList();
            if (_giftFor.Count == 0)
                return;
            PromptSet(FittingGifts.BuyKind, new TownEvent { Kind = FittingGifts.BuyKind, Town = _placeTown, AName = _giftFor[0].Name }, FittingGifts.PromptMs);
        }

        private PersonState StateOrNull(string id)
        {
            PersonState st;
            return id != null && _store.People.TryGetValue(id, out st) ? st : null;   // a plain read: never Find (it remakes the trimmed)
        }

        /// The store's menu: a row a person, the gift that fits and its price.
        private List<InteractOption> GiftRows()
        {
            int cash = MONEY._MONEY_GET_CASH_BALANCE();
            var rows = new List<InteractOption>();
            for (int i = 0; i < _giftFor.Count; i++)
            {
                Resident r = _giftFor[i];
                GiftFit f = FittingGifts.For(Routines.ProfileOf(r, Voice.NowYear), r.Woman);
                if (f == null)
                    continue;
                rows.Add(new InteractOption { Id = "gift:" + i, Label = Grammar.UpperFirst(f.Item) + " for " + Person.FirstOf(r.Name) + " (" + (f.Cents < 100 ? f.Cents + " cents" : "$" + f.Cents / 100) + ").", Why = cash < f.Cents ? "you haven't the money" : null });
            }
            rows.Add(new InteractOption { Id = "gift:no", Label = "Nothing today." });
            return rows;
        }

        private void GiftBuy(string rowId)
        {
            int i;
            if (rowId == null || !rowId.StartsWith("gift:") || !int.TryParse(rowId.Substring(5), out i) || i < 0 || i >= _giftFor.Count)
                return;
            Resident r = _giftFor[i];
            GiftFit f = FittingGifts.For(Routines.ProfileOf(r, Voice.NowYear), r.Woman);
            if (f == null)
                return;
            if (MONEY._MONEY_GET_CASH_BALANCE() < f.Cents)
            {
                Hint("You haven't the money.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(f.Cents);
            _gift = new GiftInHand { ToId = r.Id, ToName = r.Name, Item = f.Item, Temper = r.Personality, Town = r.Town, Until = _store.LastGameMin + FittingGifts.KeepDays * 1440L };
            Hint(Grammar.UpperFirst(f.Item) + " for " + Person.FirstOf(r.Name) + " is in your pocket (two days).", 4500);
            Log.Write("GIFT bought: " + f.Item + " for " + r.Name);
        }

        private void GiftGive(TownEvent e)
        {
            GiftInHand gi = _gift;
            if (e == null || gi == null || e.AId != gi.ToId)
                return;
            _gift = null;
            long g = _store.LastGameMin;
            long last;
            bool again = _giftLastTo.TryGetValue(gi.ToId, out last) && g - last < 7 * 1440L;
            _giftLastTo[gi.ToId] = g;
            int h = 0;
            foreach (int x in _mindNear)
            {
                MindWho w;
                if (_mindWho.TryGetValue(x, out w) && (w.Id == gi.ToId || _engine.WorryIdOf(w.Id) == gi.ToId) && ENTITY.DOES_ENTITY_EXIST(x))
                {
                    h = x;
                    break;
                }
            }
            string takes = FittingGifts.Takes(gi.Temper);
            if (h != 0)
                SpeechCaption(Person.FirstOf(gi.ToName) ?? "They", FittingGifts.Answer(takes, again, gi.Item, gi.ToId + "|" + g), h, 5000, true);
            PersonState st = _store.Find(gi.ToId);
            if (st != null)
                st.Affection = Romance.Clamp(st.Affection + (again ? FittingGifts.LikingAgain : takes == "cold" ? FittingGifts.LikingAgain : FittingGifts.Liking), -100, 100);
            Ped me = Game.Player.Character;
            if (h != 0)
                RecordEvent("charity", "player", "you", gi.ToId, gi.ToName, ENTITY.GET_ENTITY_COORDS(h, true, true), me.Handle, me.Handle, h, "gave them " + gi.Item);   // a kindness they and those near see
            string gacq = FolkIdOf(gi.ToId);
            if (gacq != null && _bridge != null)
                _bridge.LikeMore(gacq, again ? 5 : 15);   // enRAGE's own fondness for one of its townsfolk
            _giftsGiven++;
            Log.Write("GIFT given: " + gi.Item + " to " + gi.ToName + " (" + (again ? "again this week" : takes) + ")");
        }
    }
}
