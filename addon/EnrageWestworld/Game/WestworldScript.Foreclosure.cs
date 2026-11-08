// A foreclosure sale in game (FIVEDAY reserve R3; Core/Foreclosure.cs, Core/Engine.Foreclosure.cs). Every ten seconds in a
// town from ten till two: if the bank sells someone up here today (asked once a town a day) and you're within 80 m of the
// town's "bankdoor" spot (else "mainstreet"), up to seven of the named people within 60 m of it, free and on foot, are held
// and gather there; the first man is the sheriff's man with the hammer. The family, if their body is near, watches (turned
// to face it, not held). Three lots: each called, then bid low by a neighbour, or met with silence when the neighbours won't
// bid against the family (two sales in five; the bank's man takes it), then sold. While a lot is called, J lets you buy it
// back for the family, once a sale: the cash, the auctioneer's word, the family's thanks, a "charity" happening. The
// family's thought, a neighbour's, and the box's word at the end; all let go. Once a town a day. Behind [World] Foreclosures.
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
        private sealed class ForeRun
        {
            public readonly List<Ped> Men = new List<Ped>(7);   // the sheriff's man first, then the neighbours
            public List<MindWho> Who = new List<MindWho>();
            public Ped Family;
            public string FamilyId, FamilyName, Seed, Thing;
            public int Step, NextAt, Until, Price;
            public bool Refuse, Bought, Test;
        }

        private ForeRun _fore;
        private TownEvent _foreAsk;
        private int _foreNextLook, _foreRuns, _foreBought;
        private string _foreWhyNot;
        private readonly Dictionary<string, long> _foreDone = new Dictionary<string, long>();      // town -> the game day of its sale
        private readonly Dictionary<string, string> _foreToday = new Dictionary<string, string>(); // "town|day" -> the family's id ("" none)

        private void ForeclosureStep(int now)
        {
            if (_fore != null)
            {
                if (_cfg == null || !_cfg.Foreclosures)
                    EndForeclosure("[World] Foreclosures switched off");
                else if (now >= _fore.NextAt)
                    Safe("foreclosure", () => ForeclosureBeat(now));
                return;
            }
            if (_cfg == null || !_cfg.Foreclosures || _store == null || _placeTown == null || now < _foreNextLook)
                return;
            _foreNextLook = now + 10000;
            Safe("foreclosure", () => ForeclosureLook(now, false));
        }

        private string ForeclosureLook(int now, bool test)
        {
            if (_placeTown == null)
                return _foreWhyNot = "only in a town";
            long g = Bridge.Now(), day = g / 1440;
            long done;
            if (!test && (!Foreclosure.Hour((int)(g % 1440)) || _foreDone.TryGetValue(_placeTown, out done) && done == day))
                return _foreWhyNot = "ten till two, once a town a day";
            Ped me = Game.Player.Character;
            Resident fam = null;
            if (!test)
            {
                string key = _placeTown + "|" + day, id;
                if (!_foreToday.TryGetValue(key, out id))
                {
                    Resident r = _engine.ForeclosureToday(_placeTown, day);
                    _foreToday[key] = id = r != null ? r.Id : "";
                    if (_foreToday.Count > 60)
                        _foreToday.Clear();
                }
                fam = id != "" ? _store.FindResident(id) : null;
                if (fam == null)
                    return _foreWhyNot = "nobody sold up here today";
            }
            Spot spot = Spots.Nearest(_spots, _placeTown, "bankdoor", me.Position.X, me.Position.Y) ?? Spots.Nearest(_spots, _placeTown, "mainstreet", me.Position.X, me.Position.Y);
            if (spot == null)
                return _foreWhyNot = "no bankdoor or mainstreet spot recorded in " + _placeTown + " (the spots tool)";
            var at = new Vector3(spot.X, spot.Y, spot.Z);
            if (at.DistanceTo(me.Position) > 80f)
                return _foreWhyNot = "you're not near the bank's door or the main street";
            var run = new ForeRun { Seed = "fore|" + _placeTown + "|" + day, NextAt = now + 1000, Until = now + 150000, Test = test };
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (run.Men.Count >= Foreclosure.MostMen || !_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (test && fam == null && Resident.IsResident(w.Id))
                {
                    fam = _store.FindResident(w.Id);   // the Testing row: the first resident near stands for the family
                    if (fam != null)
                    {
                        run.Family = ForeFamilyBody(h);
                        continue;
                    }
                }
                if (fam != null && w.Id == fam.Id)
                {
                    run.Family = ForeFamilyBody(h);
                    continue;
                }
                Ped p = new Ped(h);
                if (run.Men.Count == 0 && !PED.IS_PED_MALE(h))
                    continue;   // the sheriff's man is a man; the neighbours anyone
                if (p.Position.DistanceTo(at) > 60f || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || _staged.Contains(h) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                run.Men.Add(p);
                run.Who.Add(w);
            }
            if (fam == null)
                return _foreWhyNot = "no resident near to stand for the family";
            if (run.Men.Count < Foreclosure.LeastMen)
                return _foreWhyNot = "too few people about (" + run.Men.Count + ")";
            run.FamilyId = fam.Id;
            run.FamilyName = fam.Name;
            run.Refuse = Foreclosure.NeighboursRefuse(run.Seed);
            if (!test)
                _foreDone[_placeTown] = day;
            for (int i = 0; i < run.Men.Count; i++)
            {
                StagedHold(run.Men[i]);
                Vector3 to = i == 0 ? at : at + new Vector3((float)Math.Cos(i * 0.8) * 4f, (float)Math.Sin(i * 0.8) * 4f, 0f);
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(run.Men[i].Handle, to.X, to.Y, to.Z, 1.2f, 30000, 1f, 0, 0f);
            }
            _fore = run;
            _foreRuns++;
            Log.Write("FORECLOSURE " + fam.Name + "'s goods sold in " + _placeTown + " (" + run.Men.Count + " about" + (run.Family != null ? ", the family watching" : "") + (run.Refuse ? ", the neighbours won't bid" : "") + ")" + (test ? " (a test)" : ""));
            return _foreWhyNot = null;
        }

        /// The family's body, only when it's ours to direct (the R1-R6 review: an enRAGE-driven record or a ped in a scene of
        /// ours had its tasks wiped); else none, and the sale runs without them watching.
        private Ped ForeFamilyBody(int h)
        {
            Ped p = new Ped(h);
            return MayDirectWhyNot(p) == null && !NotFree(h) && !_director.Involves(h) && !_staged.Contains(h) ? p : null;
        }

        private void ForeclosureBeat(int now)
        {
            ForeRun r = _fore;
            if (now > r.Until || MISC.GET_MISSION_FLAG() || r.Men.Any(p => !p.Exists() || p.IsDead || PED.IS_PED_IN_COMBAT(p.Handle, 0)))
            {
                EndForeclosure("broken off");
                return;
            }
            Ped hammer = r.Men[0];
            string label = r.Who[0].Name ?? "The sheriff's man";
            if (r.Step == 0)
            {
                foreach (Ped p in r.Men.Skip(1))
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, hammer.Handle, 120000, 0f, 0f, 0f);
                if (r.Family != null && r.Family.Exists())
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Family.Handle, hammer.Handle, 60000, 0f, 0f, 0f);
                r.NextAt = now + 15000;
                r.Step++;
                return;
            }
            if (r.Step == 1)
            {
                SpeechCaption(label, Foreclosure.Opening(r.FamilyName, r.Seed), hammer.Handle, 5000);
                r.NextAt = now + 6000;
                r.Step++;
                return;
            }
            int beat = r.Step - 2, lot = beat / 3, part = beat % 3;
            if (lot >= Foreclosure.Lots)
            {
                ForeclosureFinish(r);
                EndForeclosure(null);
                return;
            }
            switch (part)
            {
                case 0:   // the lot called; J may buy it back
                    r.Thing = Foreclosure.Thing(r.Seed, lot);
                    r.Price = Foreclosure.PriceCents(r.Seed, lot);
                    SpeechCaption(label, Foreclosure.Call(r.Thing, r.Seed + lot), hammer.Handle, 4000);
                    if (!r.Bought)
                    {
                        _foreAsk = new TownEvent { Kind = Foreclosure.Kind, Town = _placeTown, AId = r.FamilyId, AName = r.FamilyName, Extra = r.Thing, Count = r.Price };
                        PromptSet(Foreclosure.Kind, _foreAsk, Foreclosure.PromptMs);
                    }
                    r.NextAt = now + 6000;
                    break;
                case 1:   // a low bid, or the street's silence
                    if (r.Refuse)
                        SpeechCaption(label, Foreclosure.Silence(r.Seed + lot), hammer.Handle, 4500);
                    else if (r.Men.Count > 1)
                    {
                        int i = 1 + (int)((Voice.Hash(r.Seed + "|bidder|" + lot) & 0x7fffffff) % (r.Men.Count - 1));
                        SpeechCaption(r.Who[i].Name ?? "A neighbour", Foreclosure.Bid(r.Seed + lot), r.Men[i].Handle, 3000);
                    }
                    r.NextAt = now + 5000;
                    break;
                default:  // sold
                    if (_eventPrompt == Foreclosure.Kind)
                        PromptClear();   // sold: no buying it back now (the R1-R6 review)
                    if (!r.Refuse)
                        SpeechCaption(label, Foreclosure.SoldLine(r.Seed + lot), hammer.Handle, 3000);
                    if (lot == 0 && r.Family != null && r.Family.Exists())
                        _ticker.Push((r.FamilyName ?? "Someone") + ", thinking", Foreclosure.FamilyThought(r.Seed), "near", r.Family.Handle);
                    r.NextAt = now + 4000;
                    break;
            }
            r.Step++;
        }

        private void ForeclosureFinish(ForeRun r)
        {
            if (r.Men.Count > 1)
                _ticker.Push((r.Who[1].Name ?? "A neighbour") + ", thinking", Foreclosure.NeighbourThought(r.Seed), "near", r.Men[1].Handle);
            if (!r.Test)
                NewsPush(_placeTown, Foreclosure.Summary(r.FamilyName, _placeTown, r.Refuse), "news");
            Log.Write("FORECLOSURE over: " + Foreclosure.Lots + " lots" + (r.Refuse ? ", not a bid among the neighbours" : "") + (r.Bought ? "; you bought one back" : ""));
        }

        /// J while a lot is called: buy it back for the family (once a sale).
        private void ForeclosureBuy(TownEvent e)
        {
            ForeRun r = _fore;
            if (e == null || e.Kind != Foreclosure.Kind || r == null || r.Bought || !ReferenceEquals(e, _foreAsk))
                return;
            if (MONEY._MONEY_GET_CASH_BALANCE() < e.Count)
            {
                Hint("You haven't the money.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(e.Count);
            r.Bought = true;
            _foreBought++;
            Ped me = Game.Player.Character;
            SpeechCaption(r.Who[0].Name ?? "The sheriff's man", Foreclosure.BoughtBackLine(r.Seed), r.Men[0].Handle, 4000);
            int fh = r.Family != null && r.Family.Exists() ? r.Family.Handle : 0;
            if (fh != 0)
                SpeechCaption(Person.FirstOf(r.FamilyName) ?? "The family", Foreclosure.Thanks(r.Seed), fh, 4500, true);
            RecordEvent("charity", "player", "you", r.FamilyId, r.FamilyName, me.Position, me.Handle, me.Handle, fh, "bought back " + e.Extra + " at the sheriff's sale");
            string acq = FolkIdOf(r.FamilyId);
            if (acq != null && _bridge != null)
                _bridge.LikeMore(acq, 25);
            Log.Write("FORECLOSURE you bought back " + e.Extra + " for " + r.FamilyName + " (" + Slate.Dollars(e.Count) + ")");
        }

        private void EndForeclosure(string why)
        {
            ForeRun r = _fore;
            _fore = null;
            if (r == null)
                return;
            foreach (Ped p in r.Men)
                StagedLetGo(p, true);
            if (r.Family != null && r.Family.Exists() && !r.Family.IsDead)
                TASK.CLEAR_PED_TASKS(r.Family.Handle, true, false);
            if (why != null)
                Log.Write("FORECLOSURE over (" + why + ")");
        }

        private string ForeclosureStatus()
        {
            if (_cfg == null || !_cfg.Foreclosures)
                return "off ([World] Foreclosures)";
            return _foreRuns + " sales this session, " + _foreBought + " lots you bought back" + (_foreWhyNot != null ? "; none now: " + _foreWhyNot : "");
        }
    }
}
