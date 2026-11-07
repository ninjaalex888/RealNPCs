// Sale day in game (FIVEDAY item 30; Core/SaleDay.cs). Tuesdays from ten to half past one in Valentine, once a day, when
// the town's "stockyards" spot is recorded and you're within 80 m of it: up to eight of the named men the mind knows within
// 70 m of it, free and on foot, are held and walk to the pens. The first is the auctioneer, the second the rancher whose
// herd is sold, the rest bid. Each lot: the auctioneer opens it, two bidders call (the winner last, at the lot's price),
// and it's knocked down; the buyer thinks of it. A ruinous last lot gets one low bid and the auctioneer's regret, and the
// rancher's thought. At the end the box has the sale's word, the drovers and hands think of their pay, and everyone there
// carries the day's mood (ChoiceContext.SaleMood: paid, or ruined) into the afternoon: the saloon from one. The ruined man
// carries the debt worry a week (with WorriesShown). All are let go after. F7 "TEST sale day:". Behind [World] SaleDay.
// Kept for the session only, like the shape-up's passed-over: nothing new in the save.
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
        private sealed class SaleRun
        {
            public readonly List<Ped> Men = new List<Ped>(8);   // the auctioneer, the rancher, the bidders
            public List<MindWho> Who = new List<MindWho>();
            public Vector3 At;
            public int Step, NextAt, Until, Lots, Head, Best;
            public bool Ruinous;
            public int RuinPrice;
            public string Seed;
        }

        private SaleRun _sale;
        private int _saleNextLook, _saleRuns, _saleRuined;
        private long _saleDayDone = -1;
        private string _saleWhyNot;
        private readonly Dictionary<string, long> _salePaid = new Dictionary<string, long>();    // id -> the game day they were at the sale
        private readonly Dictionary<string, long> _saleRuinedOn = new Dictionary<string, long>(); // id -> the game day their herd went for nothing

        /// The day's mood of one who was at the sale (Choosing's SaleMood): "ruined", "paid", or null.
        private string SaleMoodToday(string id)
        {
            if (_cfg == null || !_cfg.SaleDay || id == null)
                return null;
            long day = Bridge.Now() / 1440, d;
            if (_saleRuinedOn.TryGetValue(id, out d) && d == day)
                return "ruined";
            return _salePaid.TryGetValue(id, out d) && d == day ? "paid" : null;
        }

        /// Whether their herd went for nothing within the week (the debt worry, with WorriesShown).
        private bool RuinedThisWeek(string id)
        {
            long d;
            return _cfg != null && _cfg.SaleDay && id != null && _saleRuinedOn.TryGetValue(id, out d) && Bridge.Now() / 1440 - d < SaleDay.RuinDays;
        }

        private void SaleDayStep(int now)
        {
            if (_cfg == null || !_cfg.SaleDay || _store == null)
                return;
            if (_sale != null)
            {
                if (now >= _sale.NextAt)
                    Safe("sale day", () => SaleDayBeat(now));
                return;
            }
            if (now < _saleNextLook || _placeTown != SaleDay.Town)
                return;
            _saleNextLook = now + 5000;
            Safe("sale day", () => SaleDayLook(now, false));
        }

        private string SaleDayLook(int now, bool test)
        {
            long g = Bridge.Now();
            if (!test && (!SaleDay.Now(InnerLife.Weekday, (int)(g % 1440)) || _saleDayDone == g / 1440))
                return _saleWhyNot = "Tuesdays only (ten to half past one), once a day";
            Ped me = Game.Player.Character;
            Spot pens = Spots.Nearest(_spots, SaleDay.Town, "stockyards", me.Position.X, me.Position.Y);
            if (pens == null)
                return _saleWhyNot = "no stockyards spot recorded in Valentine (the spots tool)";
            var at = new Vector3(pens.X, pens.Y, pens.Z);
            if (at.DistanceTo(me.Position) > 80f)
                return _saleWhyNot = "you're not near the stockyards";
            int month = InnerLife.ClockMonth;
            var run = new SaleRun { At = at, Seed = "sale|" + g / 1440, NextAt = now + 1000, Until = now + 180000, Lots = SaleDay.Lots(month) };
            run.Ruinous = SaleDay.Ruinous(run.Seed, month);
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (run.Men.Count >= SaleDay.MostMen || !_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_MALE(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(at) > 70f || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || _staged.Contains(h) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                run.Men.Add(p);
                run.Who.Add(w);
            }
            if (run.Men.Count < SaleDay.LeastMen)
                return _saleWhyNot = "too few men about the pens (" + run.Men.Count + ")";
            _saleDayDone = g / 1440;
            for (int i = 0; i < run.Men.Count; i++)
            {
                StagedHold(run.Men[i]);
                // the auctioneer on the spot (the rail), the rest in an arc before him
                Vector3 to = i == 0 ? at : at + new Vector3((float)Math.Cos(i * 0.7) * 3.5f, (float)Math.Sin(i * 0.7) * 3.5f, 0f);
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(run.Men[i].Handle, to.X, to.Y, to.Z, 1.3f, 30000, 1f, 0, 0f);
            }
            _sale = run;
            _saleRuns++;
            Log.Write("SALEDAY " + run.Men.Count + " named men come to the pens for the Valentine sale; " + (run.Who[0].Name ?? "the auctioneer") + " calls, " + (run.Who[1].Name ?? "a rancher") + "'s herd, " + run.Lots + " lots" + (run.Ruinous ? ", the last ruinous" : "") + (test ? " (a test)" : ""));
            return _saleWhyNot = null;
        }

        private Traits SaleTraits(MindWho w)
        {
            return w.Traits ?? (w.Src != null ? (w.Traits = _engine.Shadow.TraitsOf(w.Src)) : null);
        }

        private void SaleDayBeat(int now)
        {
            SaleRun r = _sale;
            if (now > r.Until || MISC.GET_MISSION_FLAG() || r.Men.Any(p => !p.Exists() || p.IsDead || PED.IS_PED_IN_COMBAT(p.Handle, 0)))
            {
                EndSaleDay("broken off");
                return;
            }
            Ped auctioneer = r.Men[0];
            int bidders = r.Men.Count - 2;
            if (r.Step == 0)   // gathering: all turn to the auctioneer
            {
                foreach (Ped p in r.Men.Skip(1))
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, auctioneer.Handle, 90000, 0f, 0f, 0f);
                r.NextAt = now + 15000;
                r.Step++;
                return;
            }
            int beat = r.Step - 1, lot = beat / 4, part = beat % 4;
            if (lot >= r.Lots)
            {
                FinishSale(r);
                EndSaleDay(null);
                return;
            }
            bool ruinous = r.Ruinous && lot == r.Lots - 1;
            int price = SaleDay.Price(r.Seed, lot, ruinous), head = SaleDay.Head(r.Seed, lot);
            var bt = r.Who.Skip(2).Select(SaleTraits).ToList();
            int win = SaleDay.Winner(bt, r.Seed, lot);
            int other = bidders > 1 ? (win + 1 + (int)((Voice.Hash(r.Seed + "|o|" + lot) & 0x7fffffff) % (bidders - 1))) % bidders : -1;
            switch (part)
            {
                case 0:   // the lot opened
                    SpeechCaption(r.Who[0].Name ?? "The auctioneer", SaleDay.Opening(r.Seed, lot, head), auctioneer.Handle, 5000);
                    r.NextAt = now + 6000;
                    break;
                case 1:   // the under-bidder (none on a ruinous lot: nobody wants them)
                    if (!ruinous && other >= 0)
                        SpeechCaption(r.Who[other + 2].Name ?? "A buyer", SaleDay.Bid(r.Seed, lot, 0, price - 2), r.Men[other + 2].Handle, 3000);
                    r.NextAt = now + (ruinous ? 1500 : 4000);
                    break;
                case 2:   // the winning bid
                    SpeechCaption(r.Who[win + 2].Name ?? "A buyer", SaleDay.Bid(r.Seed, lot, 1, price), r.Men[win + 2].Handle, 3000);
                    r.NextAt = now + 4000;
                    break;
                default:  // knocked down
                    SpeechCaption(r.Who[0].Name ?? "The auctioneer", SaleDay.Sold(r.Seed, lot, ruinous), auctioneer.Handle, 4500);
                    r.Head += head;
                    if (ruinous)
                    {
                        MindWho rancher = r.Who[1];
                        r.RuinPrice = price;
                        _saleRuinedOn[rancher.Id] = Bridge.Now() / 1440;
                        _saleRuined++;
                        _ticker.Push((rancher.Name ?? "The rancher") + ", thinking", SaleDay.Ruined(rancher.Id + "|" + r.Seed), "near", r.Men[1].Handle);
                        Log.Write("SALEDAY " + (rancher.Name ?? "the rancher") + "'s last lot goes for $" + price + " a head: ruined");
                    }
                    else
                    {
                        r.Best = Math.Max(r.Best, price);
                        if (lot == 0)
                            _ticker.Push((r.Who[win + 2].Name ?? "A buyer") + ", thinking", SaleDay.Bought(r.Who[win + 2].Id + "|" + r.Seed), "near", r.Men[win + 2].Handle);
                    }
                    r.NextAt = now + 6000;
                    break;
            }
            r.Step++;
        }

        /// The sale's end: the box's word, the day's mood for all who were there, a drover's thought of his pay.
        private void FinishSale(SaleRun r)
        {
            long day = Bridge.Now() / 1440;
            MindWho rancher = r.Who[1];
            for (int i = 1; i < r.Who.Count; i++)
                if (r.Who[i].Id != null)
                    _salePaid[r.Who[i].Id] = day;
            NewsPush(SaleDay.Town, SaleDay.Summary(r.Lots, r.Head, r.Best, r.Ruinous ? Person.FirstOf(rancher.Name) ?? "A rancher" : null, r.RuinPrice), "news");
            int drover = r.Who.Count > 3 ? 3 : 2;
            _ticker.Push((r.Who[drover].Name ?? "A drover") + ", thinking", SaleDay.Paid(r.Who[drover].Id + "|" + r.Seed), "near", r.Men[drover].Handle);
            if (r.Ruinous && r.Who.Count > 4)   // and one who saw it says so (the street knows by the evening)
                _ticker.Push((r.Who[4].Name ?? "Someone") + ", thinking", SaleDay.Talk(Person.FirstOf(rancher.Name), r.Seed), "near", r.Men[4].Handle);
            Log.Write("SALEDAY over: " + r.Lots + " lots, " + r.Head + " head; " + (r.Who.Count - 1) + " carry the day's pay (or loss) into the afternoon");
        }

        private void EndSaleDay(string why)
        {
            SaleRun r = _sale;
            _sale = null;
            if (r == null)
                return;
            foreach (Ped p in r.Men)
                if (p.Exists())
                    StagedLetGo(p, true);
            if (why != null)
                Log.Write("SALEDAY over (" + why + ")");
        }

        private string SaleDayStatus()
        {
            if (_cfg == null || !_cfg.SaleDay)
                return "off ([World] SaleDay)";
            return _saleRuns + " sales this session, " + _saleRuined + " ruined, " + _salePaid.Count + " at a sale" + (_saleWhyNot != null ? "; none now: " + _saleWhyNot : "");
        }
    }
}
