// A box social, staged (FIVEDAY reserve R6; Core/BoxSocial.cs, Core/Engine.BoxSocial.cs): at the town's "churchsteps" spot
// (else a spot unseen 20-35 m off), the auctioneer, the woman whose box it is, her two suitors (bodies with their names) and
// three of the congregation, spawned out of view; beats every six to eight seconds while you're within 50 m: the box opened
// to bids, the two suitors bidding each other up (J while the bidding goes lets you bid fifty cents over), and sold, to the
// winner or to you; her words to whoever bought it, the loser's thought. Reaching the end is attending. The shivaree's rails.
// Behind [World] TownEvents and EventBoxSocials.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _boxStaged, _boxBought;
        private Ped _boxGirl, _boxWinner, _boxLoser;
        private int _boxHigh;
        private bool _boxYou;

        private void BeginBoxSocial(TownEvent e, int now)
        {
            Ped me = Game.Player.Character;
            Spot spot = Spots.Nearest(_spots, e.Town, "churchsteps", me.Position.X, me.Position.Y);
            Vector3 at;
            if (spot != null && new Vector3(spot.X, spot.Y, spot.Z).DistanceTo(me.Position) < 150f && !Natives.Looking(new Vector3(spot.X, spot.Y, spot.Z + 1f), 50f))
                at = new Vector3(spot.X, spot.Y, spot.Z);
            else if (!EncounterSpot(me, 20f, 35f, out at))
                return;
            var run = new EventRun { E = e, At = at, Since = now, NextCall = now + 5000 };
            int seed = (e.Id ?? "").GetHashCode();
            run.ManModel = Spawner.ModelFor(e.Town, false, seed + 31);
            run.WomanModel = Spawner.ModelFor(e.Town, true, seed + 32);
            run.FiddlerModel = Spawner.ModelFor(e.Town, false, seed + 33);   // the auctioneer
            _eventRun = run;
            _eventsStaged++;
            _boxStaged++;
            _boxYou = false;
            Log.Write("EVENT boxsocial " + e.Town + ": staging " + (int)at.DistanceTo(me.Position) + " m off");
        }

        private void BoxSocialStep(EventRun r, int now)
        {
            long g = Bridge.Now();
            Ped me = Game.Player.Character;
            TownEvent e = r.E;
            if (!r.Ready)
            {
                if (!EventCastReady(r))
                {
                    if (now - r.Since > 15000)
                        EventRunLost(r, now, "the game never streamed the bodies in");
                    return;
                }
                if (!Spawner.SafeToSpawn())
                    return;
                Ped auctioneer = Spawner.Spawn(r.FiddlerModel, r.At, 180f);
                _boxGirl = Spawner.Spawn(r.WomanModel, r.At + new Vector3(1.5f, 1f, 0f), 200f);
                _boxWinner = Spawner.Spawn(r.ManModel, r.At + new Vector3(-2f, -3f, 0f), 0f);
                _boxLoser = Spawner.Spawn(r.ManModel, r.At + new Vector3(2f, -3.5f, 0f), 0f);
                foreach (Ped p in new[] { auctioneer, _boxGirl, _boxWinner, _boxLoser })
                    if (p != null)
                        r.Cast.Add(p);
                for (int i = 0; i < 3; i++)
                {
                    Ped p = Spawner.Spawn(i == 1 ? r.WomanModel : r.ManModel, r.At + new Vector3(-3f + i * 3f, -5f, 0f), 0f);
                    if (p != null)
                        r.Cast.Add(p);
                }
                if (auctioneer == null || _boxGirl == null || _boxWinner == null || _boxLoser == null)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, true);
                    EventRunLost(r, now, "too few came");
                    return;
                }
                r.Fiddler = auctioneer;
                foreach (Ped p in r.Cast)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                    if (p != auctioneer)
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, auctioneer.Handle, 120000, 0f, 0f, 0f);
                }
                r.Ready = true;
                r.SpawnedAt = now;
                Log.Write("EVENT boxsocial " + e.Town + ": the boxes out, " + e.BName + " and the other after " + e.AName + "'s");
                return;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    foreach (Ped p in r.Cast)
                        Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
                    _eventRun = null;
                    _eventsNext = now + 60000;
                }
                return;
            }
            bool byYou = false;
            string broke = now - r.SpawnedAt < 1500 ? null : EventBroken(r, out byYou);
            if (broke != null)
            {
                BreakEventRun(r, byYou, broke, now, g);
                return;
            }
            float d = me.Position.DistanceTo(r.At);
            if (d > 120f || g >= e.Until)
            {
                bool stayed = now - r.Since >= 60000;
                _engine.FinishEvent(e, stayed, g);
                if (stayed) _eventsAttended++;
                foreach (Ped p in r.Cast)
                    WanderLater(p.Handle, now + 1000 + _rng.Next(5000));
                r.ReleaseAt = now + 10000;
                return;
            }
            if (now < r.NextCall || d > 50f)
                return;
            r.NextCall = now + 6000 + _rng.Next(2000);
            string seed = e.Id + "|" + r.Step;
            string[] lo = (e.Extra ?? "|").Split('|');
            string loserName = lo.Length > 1 ? lo[1] : null;
            int[] bids = BoxSocial.Bids(e.Id);
            switch (r.Step)
            {
                case 0:
                    SpeechCaption("The auctioneer", BoxSocial.Open(seed), r.Fiddler.Handle, 5000);
                    break;
                case 1: case 2: case 3: case 4:
                    {
                        if (_boxYou)
                        {
                            r.Step = 5;   // you've bid: it's yours
                            goto case 5;
                        }
                        int i = r.Step - 1;
                        bool loserTurn = i % 2 == 0;
                        Ped who = loserTurn ? _boxLoser : _boxWinner;
                        SpeechCaption(Person.FirstOf(loserTurn ? loserName : e.BName) ?? "A young man", BoxSocial.RivalBid(seed, bids[i]), who.Handle, 3000);
                        _boxHigh = bids[i];
                        PromptSet(BoxSocial.Kind, e, BoxSocial.PromptMs);   // J: bid fifty cents over
                        break;
                    }
                case 5:
                    SpeechCaption("The auctioneer", BoxSocial.Sold(_boxYou, seed), r.Fiddler.Handle, 4000);
                    if (_boxYou)
                    {
                        e.Part = "you";
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(_boxGirl.Handle, me.Handle, 4000, 0f, 0f, 0f);
                    }
                    break;
                case 6:
                    SpeechCaption(Person.FirstOf(e.AName) ?? "She", BoxSocial.Her(_boxYou, seed), _boxGirl.Handle, 4500, _boxYou);
                    _ticker.Push((loserName ?? "A young man") + ", thinking", BoxSocial.LoserThought(seed), "near", _boxLoser.Handle);
                    break;
                default:
                    PromptClear();
                    _engine.FinishEvent(e, true, g);
                    _eventsAttended++;
                    foreach (Ped p in r.Cast)
                        WanderLater(p.Handle, now + 1500 + _rng.Next(5000));
                    Log.Write("EVENT boxsocial " + e.Town + ": over, " + (_boxYou ? "the box yours" : e.BName + " has it"));
                    r.ReleaseAt = now + 14000;
                    return;
            }
            r.Step++;
        }

        /// J while they bid: fifty cents over the high bid, from your pocket; the next beat sells it to you.
        private void BoxSocialBid(TownEvent e)
        {
            if (e == null || e.Kind != BoxSocial.Kind || _boxYou || _eventRun == null || _eventRun.E != e)
                return;
            int bid = _boxHigh + BoxSocial.YouOverCents;
            if (MONEY._MONEY_GET_CASH_BALANCE() < bid)
            {
                Hint("You haven't the money.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(bid);
            _boxYou = true;
            _boxBought++;
            SpeechCaption("You", Slate.Dollars(bid) + ".", Game.Player.Character.Handle, 2000, false, false);
            Log.Write("EVENT boxsocial " + e.Town + ": you bid " + Slate.Dollars(bid) + " for " + e.AName + "'s box");
        }

        private string BoxSocialStatus()
        {
            return !_cfg.EventBoxSocials ? "off ([World] EventBoxSocials)" : _boxStaged + " staged this session, " + _boxBought + " boxes you bought";
        }
    }
}
