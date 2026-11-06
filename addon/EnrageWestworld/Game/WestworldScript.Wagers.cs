// Wagers on a fight, the game side (Core/Wagers.cs has the rules and the words; FIVEDAY item 1). While two of the street's
// people trade blows (the Director's fight), every 2.5 s the people within 25 m of it whom the mind knows and who are free
// (not in it, not the law, not fleeing or fighting, not one of ours) are each weighed once a fight: Wagers.Backs says
// whether they bet and on whom. A backer of one side within 9 m of a backer of the other strikes a bet: the offer and the
// taking, said in their own voices with the words on the subtitle, up to three bets a fight. Words only: nobody is given a
// task, walks, turns or stops (the ring that gathered people round a fight froze some, and is off since settings 182).
// When the Director says the fight is over and who was left standing (FightOver), each bet settles: the winner's backer
// crows, the loser's backer pays, pays with a sour word, says he'll owe it, or won't pay (Wagers.Settle), and the tie and
// both memories keep it; a welsh leaves a hostile tie the street's reasons pick up when the two next meet. A fight with
// nobody left standing, or one the scene drops, calls the bets off. Arthur is never part of it. Behind [Scenes]
// BrawlWagers and MindLive. The Testing row "A fistfight with bets on it" (TestWagerKey) starts a fistfight in which
// everyone the mind knows nearby bets (the bar and the roll skipped).
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
        private const int WagerLookMs = 2500, WagerOffAfterMs = 4000;
        private const float WagerMetres = 25f, WagerSayMetres = 30f;

        private sealed class WagerMan
        {
            public int H, Side, Cents;
            public string Id, Name, Devotion;
            public Traits T;
            public bool Drunk, Matched;
        }

        private sealed class WagerBet
        {
            public WagerMan A, B;      // A backs OnA, B backs OnB (the two who fought, as they led their sides then)
            public int OnA, OnB, Cents;
            public string Seed;
        }

        private readonly HashSet<int> _wagerLooked = new HashSet<int>();
        private readonly List<WagerMan> _wagerOpen = new List<WagerMan>();
        private readonly List<WagerBet> _wagerBets = new List<WagerBet>();
        private int _wagerFight = -1, _wagerNextAt, _wagerEndedAt, _wagerSureUntil, _wagerOffFight = -1;   // _wagerOffFight: bets called off on it, none struck again (the review)
        private int _wagerMade, _wagerPaid, _wagerSour, _wagerOwed, _wagerWelshed, _wagerOff, _wagerFights;
        private bool _wagerFailed;
        private string _wagerLast;

        private bool WagersOn { get { return _cfg.MindLive && _cfg.BrawlWagers && !_wagerFailed; } }

        /// Each tick beside the brawl's: weigh the people near a fight, strike bets, call them off when it ends unsettled.
        private void WagersStep(Ped me, int now)
        {
            if (!WagersOn || now < _wagerNextAt)
                return;
            _wagerNextAt = now + WagerLookMs;
            try
            {
                List<int> a, b;
                if (MISC.GET_MISSION_FLAG() || !_director.FightSides(out a, out b) || a.Count == 0 || b.Count == 0 || !ENTITY.DOES_ENTITY_EXIST(a[0]) || !ENTITY.DOES_ENTITY_EXIST(b[0]))
                {
                    if (_wagerBets.Count > 0 || _wagerOpen.Count > 0)
                    {
                        if (_wagerEndedAt == 0)
                            _wagerEndedAt = now;   // FightOver comes as the scene closes; give it a moment before calling the bets off
                        else if (now - _wagerEndedAt >= WagerOffAfterMs)
                            WagersCallOff(me, "the fight ended with no one standing or was let go");
                    }
                    return;
                }
                _wagerEndedAt = 0;
                if (_director.FightId == _wagerOffFight)
                    return;   // its bets were called off: the same people don't bet on it twice
                if (_director.FightId != _wagerFight)
                {
                    WagersReset();
                    _wagerFight = _director.FightId;
                    _wagerFights++;
                }
                if (_wagerBets.Count >= Wagers.MostBets)
                    return;
                Vector3 mid = ENTITY.GET_ENTITY_COORDS(a[0], true, true);
                if (mid.DistanceTo(me.Position) > 60f)
                    return;   // nobody near the player to hear it: the street's money is its own business
                MindWho fa = MindKnown(a[0], now), fb = MindKnown(b[0], now);
                string faName = fa != null && fa.Name != null ? Person.FirstOf(fa.Name) : null, fbName = fb != null && fb.Name != null ? Person.FirstOf(fb.Name) : null;
                bool sure = now < _wagerSureUntil;
                foreach (Ped p in Peds.All())
                {
                    if (p == null || p.Position.DistanceTo(mid) > WagerMetres)
                        continue;   // the distance first
                    int h = p.Handle;
                    if (_wagerLooked.Contains(h) || h == me.Handle || a.Contains(h) || b.Contains(h))
                        continue;
                    if (p.IsDead || !PED.IS_PED_HUMAN(h) || Natives.IsLaw(p) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || _director.Involves(h) ||
                        JoinIsPending(h) || Spawner.IsOurs(h) || Natives.StoryModel(p) || !IsGrownUp(p))
                        continue;
                    MindWho w = MindKnown(h, now);
                    if (w == null || w.Id == null || w.Src == null)
                        continue;   // the mind's: nobody it doesn't know (looked at again next look, once known)
                    _wagerLooked.Add(h);
                    Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                    bool drunk = InHisCups(p, w.P);
                    string seed = "wager|" + _wagerFight + "|" + w.Id;
                    int wa = fa != null && fa.Id != null ? LadderWarmth(w.Id, fa.Id) : 0, wb = fb != null && fb.Id != null ? LadderWarmth(w.Id, fb.Id) : 0;
                    string why;
                    int side = Wagers.Backs(t, wa, wb, drunk, w.P != null ? w.P.Devotion : null, seed, out why);
                    if (side < 0 && sure && (w.P == null || w.P.Devotion != "Devout"))
                    {
                        side = wa > wb ? 0 : wb > wa ? 1 : (Voice.Hash(seed + "|side") & 1);   // the Testing row: everyone known bets
                        why = "the test";
                    }
                    if (side < 0)
                    {
                        Log.Detail("WAGER " + w.Name + " watches the fight and keeps the money (" + why + ")");
                        continue;
                    }
                    var man = new WagerMan { H = h, Side = side, Cents = Wagers.Stake(t, drunk, seed), Id = w.Id, Name = w.Name, Devotion = w.P != null ? w.P.Devotion : null, T = t, Drunk = drunk };
                    WagerMan other = _wagerOpen.FirstOrDefault(o => !o.Matched && o.Side != side && ENTITY.DOES_ENTITY_EXIST(o.H) && !ENTITY.IS_ENTITY_DEAD(o.H) &&
                                                                     ENTITY.GET_ENTITY_COORDS(o.H, true, true).DistanceTo(p.Position) <= Wagers.PairMetres);
                    if (other == null)
                    {
                        _wagerOpen.Add(man);
                        Log.Detail("WAGER " + w.Name + " would back " + (side == 0 ? faName : fbName) + " (" + why + "), waiting for a taker");
                        continue;
                    }
                    other.Matched = man.Matched = true;
                    // the one who spoke up first offers; the newcomer takes it, at the smaller of their stakes
                    WagerMan offer = other, take = man;
                    int onOffer = offer.Side == 0 ? a[0] : b[0], onTake = take.Side == 0 ? a[0] : b[0];
                    var bet = new WagerBet { A = offer, B = take, OnA = onOffer, OnB = onTake, Cents = Math.Min(offer.Cents, take.Cents), Seed = seed };
                    _wagerBets.Add(bet);
                    _wagerMade++;
                    string himA = offer.Side == 0 ? faName : fbName, himB = take.Side == 0 ? faName : fbName;
                    string lineA = Wagers.Fill(Wagers.OfferLine(bet.Seed), bet.Cents, himA, himB, Person.FirstOf(take.Name));
                    string lineB = Wagers.Fill(Wagers.TakeLine(bet.Seed), bet.Cents, himB, himA, Person.FirstOf(offer.Name));
                    WagerSay(me, offer, lineA, take.H);
                    WagerSay(me, take, lineB, offer.H);
                    _wagerLast = offer.Name + " and " + take.Name + ", " + Wagers.Money(bet.Cents);
                    Log.Write("WAGER " + offer.Name + " bets " + take.Name + " " + Wagers.Money(bet.Cents) + ": " + (himA ?? "one") + " against " + (himB ?? "the other") + " (" + _wagerBets.Count + " on this fight)");
                    if (_wagerBets.Count >= Wagers.MostBets)
                        break;
                }
            }
            catch (Exception ex)
            {
                _wagerFailed = true;
                Log.Write("WAGER failed once, off for this session: " + ex.Message);
            }
        }

        /// From the Director (beside the aftermath): the fight is over, `winner` left standing. Each bet on it settles.
        private void WagersFightOver(int winner, int loser)
        {
            if (!WagersOn || _wagerBets.Count == 0)
                return;
            try
            {
                Ped me = Game.Player.Character;
                int now = Game.GameTime;
                foreach (WagerBet bet in _wagerBets)
                {
                    // A won if the man A backed is the winner, or the man B backed is the loser (a leader replaced counts as his side)
                    bool aWon = bet.OnA == winner || bet.OnB == loser, aLost = bet.OnA == loser || bet.OnB == winner;
                    if (aWon == aLost)
                    {
                        WagerSay(me, bet.A, Wagers.Fill(Wagers.OffLine(bet.Seed), bet.Cents, null, null, Person.FirstOf(bet.B.Name)), bet.B.H);
                        _wagerOff++;
                        continue;
                    }
                    WagerMan won = aWon ? bet.A : bet.B, lost = aWon ? bet.B : bet.A;
                    if (!ENTITY.DOES_ENTITY_EXIST(lost.H) || ENTITY.IS_ENTITY_DEAD(lost.H) || !ENTITY.DOES_ENTITY_EXIST(won.H) || ENTITY.IS_ENTITY_DEAD(won.H))
                    {
                        _wagerOff++;
                        continue;   // one of them is gone: nobody to collect from, or nobody to collect
                    }
                    Vector3 at = ENTITY.GET_ENTITY_COORDS(lost.H, true, true);
                    bool lawNear = BrawlFindLaw(at, 30f, false, 0) != null;
                    int pct;
                    string how = Wagers.Settle(lost.T, lost.Drunk, LadderWarmth(lost.Id, won.Id), lawNear, bet.Cents, bet.Seed + "|settle", out pct);
                    WagerSay(me, won, Wagers.Fill(Wagers.CrowLine(bet.Seed), bet.Cents, null, null, Person.FirstOf(lost.Name)), lost.H);
                    WagerSay(me, lost, Wagers.Fill(Wagers.LoserLine(how, bet.Seed), bet.Cents, null, null, Person.FirstOf(won.Name)), won.H);
                    if (how == Wagers.Welshes)
                        WagerSay(me, won, Wagers.Fill(Wagers.CheatedLine(bet.Seed), bet.Cents, null, null, Person.FirstOf(lost.Name)), lost.H);
                    // the tie and the memories: the one who lost the bet first (one-sided: Ties.Trouble, Minds.OneSided)
                    string kind = Wagers.TieKind(how);
                    Ties.Note(_store.Ties, lost.Id, lost.Name, won.Id, won.Name, _placeTown, kind, Bridge.Now(), lost.Id);
                    var watchers = how == Wagers.Welshes ? WagerWatchers(lost, won) : null;
                    _engine.RememberScene(kind, new Minds.Party { Id = lost.Id, Name = lost.Name, Home = _placeTown }, new Minds.Party { Id = won.Id, Name = won.Name, Home = _placeTown }, watchers, _placeTown, null);
                    if (how == Wagers.Welshes) _wagerWelshed++; else if (how == Wagers.Owes) _wagerOwed++; else if (how == Wagers.Grumbles) _wagerSour++; else _wagerPaid++;
                    _wagerLast = lost.Name + " " + how + " (" + Wagers.Money(bet.Cents) + " to " + won.Name + (how == Wagers.Welshes ? ", " + pct + "% to welsh" : "") + ")";
                    Log.Write("WAGER settled: " + _wagerLast);
                }
            }
            catch (Exception ex)
            {
                _wagerFailed = true;
                Log.Write("WAGER settling failed once, off for this session: " + ex.Message);
            }
            WagersReset();
        }

        /// The other backers near a welsh see it: it goes round.
        private List<Minds.Party> WagerWatchers(WagerMan lost, WagerMan won)
        {
            var list = new List<Minds.Party>();
            foreach (WagerBet b in _wagerBets)
                foreach (WagerMan m in new[] { b.A, b.B })
                    if (m != lost && m != won && m.Id != null && list.All(x => x.Id != m.Id))
                        list.Add(new Minds.Party { Id = m.Id, Name = m.Name, Home = _placeTown });
            foreach (WagerMan m in _wagerOpen)
                if (m != lost && m != won && m.Id != null && list.All(x => x.Id != m.Id))
                    list.Add(new Minds.Party { Id = m.Id, Name = m.Name, Home = _placeTown });
            return list;
        }

        private void WagersCallOff(Ped me, string why)
        {
            WagerBet first = _wagerBets.FirstOrDefault();
            if (first != null && ENTITY.DOES_ENTITY_EXIST(first.A.H) && !ENTITY.IS_ENTITY_DEAD(first.A.H))
                WagerSay(me, first.A, Wagers.Fill(Wagers.OffLine(first.Seed), first.Cents, null, null, Person.FirstOf(first.B.Name)), first.B.H);
            if (_wagerBets.Count > 0)
            {
                _wagerOff += _wagerBets.Count;
                Log.Write("WAGER " + _wagerBets.Count + " bet(s) off: " + why);
            }
            int off = _wagerFight;
            WagersReset();
            _wagerOffFight = off;
        }

        private void WagersReset()
        {
            _wagerLooked.Clear();
            _wagerOpen.Clear();
            _wagerBets.Clear();
            _wagerEndedAt = 0;
            if (_wagerFight >= 0 && _wagerSureUntil != 0)
                _wagerSureUntil = 0;   // the Testing row's window ends with its fight
            _wagerFight = -1;
        }

        /// A line in their own voice (the game's nearest line for it, Speech.For), with our words on the subtitle; said
        /// only within earshot of Arthur.
        private void WagerSay(Ped me, WagerMan m, string line, int to)
        {
            if (m == null || line == null || !ENTITY.DOES_ENTITY_EXIST(m.H) || ENTITY.IS_ENTITY_DEAD(m.H))
                return;
            if (ENTITY.GET_ENTITY_COORDS(m.H, true, true).DistanceTo(me.Position) > WagerSayMetres)
                return;
            Feed.Say(m.Name, line, Speech.For(m.H, line, to, 0, 0, true, "chat"), m.H);
        }

        /// The Testing row: a fistfight now, and everyone the mind knows near it bets (two minutes).
        private string WagerTest(int now, string seed, out string said)
        {
            said = null;
            if (!_cfg.MindLive || !_cfg.BrawlWagers)
            {
                return "[Mind] MindLive and [Scenes] BrawlWagers must be on";
            }
            _wagerSureUntil = now + 120000;
            string not = FireScene("brawl", now, seed, out said);
            if (not != null)
                _wagerSureUntil = 0;
            else
                said = (said != null ? said + " " : "") + "Everyone near who the mind knows will bet on it.";
            return not;
        }

        private string WagersStatus()
        {
            if (!_cfg.BrawlWagers)
                return "off ([Scenes] BrawlWagers)";
            if (_wagerFailed)
                return "FAILED once this session (see the log)";
            return _wagerMade + " bets on " + _wagerFights + " fights this session: " + _wagerPaid + " paid, " + _wagerSour + " paid sour, " + _wagerOwed + " owed, " + _wagerWelshed + " welshed, " + _wagerOff + " off" +
                   (_wagerLast != null ? "; the last: " + _wagerLast : "") + (_wagerBets.Count > 0 ? "; " + _wagerBets.Count + " on the fight now" : "");
        }
    }
}
