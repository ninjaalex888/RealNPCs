// Arthur in a brawl, on J (FOURTEENHOUR item 11; Core/BrawlVerbs.cs; [Scenes] BrawlVerbs). Once a fight, when its fists are
// flying within 20 m of him and he's on foot and not fighting himself: J offers to break it up, to stand with the man losing,
// or (with an onlooker of the wagers' waiting for a taker) to take his bet. Anyone who turns on Arthur is the game's and
// enRAGE's at once: let go by the Director the same tick (TWELVEHOUR.md's rule). Seen runners: the J menu (the slate), the
// Director's brawls, the wagers. A look a second, never a frame.
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
        private int _bvNextLook, _bvAgainAt, _bvOfferedFor = -1, _bvBreakups, _bvStands, _bvBets, _bvPaid, _bvWelshed, _bvWon;
        private int _bvLoser, _bvBeater, _bvOwedCents, _bvOwedUntil;
        private WagerMan _bvBacker, _bvOwedTo;
        private string _bvLast;

        private void BrawlVerbsStep(int now)
        {
            if (_cfg == null || !_cfg.BrawlVerbs || !BrawlsOn || now < _bvNextLook)
                return;
            _bvNextLook = now + 1000;
            Safe("brawl verbs", () => BrawlVerbsLook(now));
        }

        private void BrawlVerbsLook(int now)
        {
            if (_bvOwedTo != null)
            {
                // a lost bet: its prompt only when J is free (it took another's place before), the 30 s from when it shows, held while
                // a menu is open, his own pay menu most likely (the review of items 1-13)
                if (_eventMenu != null)
                    _bvOwedUntil = Math.Max(_bvOwedUntil, now + 1500);
                else if (!_bvPayShown)
                    BrawlVerbsPayPrompt(now);
                else if (now > _bvOwedUntil)
                    BrawlVerbsWelsh("the prompt lapsed");   // a lost bet owed and never paid: a welsh
            }
            if (_eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil) || now < _bvAgainAt)
                return;   // something else is on J just now
            List<int> a, b;
            if (!_director.FightSides(out a, out b) || _director.FightOnFor(now) <= 0 || a.Count == 0 || b.Count == 0 || _bvOfferedFor == _director.FightId)
                return;
            Ped me = Game.Player.Character;
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || PED.IS_PED_IN_MELEE_COMBAT(me.Handle))
                return;
            if (!ENTITY.DOES_ENTITY_EXIST(a[0]) || !ENTITY.DOES_ENTITY_EXIST(b[0]) || me.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(a[0], true, true)) > BrawlVerbs.NearMetres)
                return;
            _bvLoser = BrawlHealth(a[0]) <= BrawlHealth(b[0]) ? a[0] : b[0];
            _bvBeater = _bvLoser == a[0] ? b[0] : a[0];
            _bvOfferedFor = _director.FightId;
            _bvAgainAt = now + BrawlVerbs.AgainMs;
            _bvBacker = WagersOn ? _wagerOpen.FirstOrDefault(o => !o.Matched && ENTITY.DOES_ENTITY_EXIST(o.H) && !ENTITY.IS_ENTITY_DEAD(o.H) && o.H != me.Handle) : null;
            if (_bvBacker != null && _store != null && BrawlVerbs.WelshedHere(_store.WorldLog, _placeTown, _store.LastGameMin))
            {
                // a welsher's name (SEAMS item 4): the backer who'd have held out his dollar says why he won't, and there's no bet row
                WagerSay(me, _bvBacker, BrawlVerbs.WontBetWithYou(_bvBacker.Id + "|" + _director.FightId), me.Handle);
                _bvShunned++;
                Log.Write("BRAWL bet: " + _bvBacker.Name + " won't take your money (you welshed in " + _placeTown + " this week)");
                _bvBacker = null;
            }
            PromptSet(BrawlVerbs.Kind, new TownEvent { Kind = BrawlVerbs.Kind, Town = _placeTown, AName = NameOfFighter(_bvLoser) }, BrawlVerbs.PromptMs);
        }

        private bool _bvPayShown;
        private int _wrNextLook, _wrAgainAt, _wrRepaid;
        private Ped _wrTo;
        private readonly HashSet<string> _wrOffered = new HashSet<string>();   // "id|minute": offered at most once a minute

        /// J near the man you welshed on, within the month (SEAMS item 24): pay what you owe. Once a second, J free, on foot.
        private void WelshRepayStep(int now)
        {
            if (!BrawlVerbs.RepayOn || _cfg == null || _store == null || _placeTown == null || now < _wrNextLook)
                return;
            _wrNextLook = now + 1000;
            if (_eventMenu != null || now < _wrAgainAt || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil) || _director.FightOnFor(now) > 0)
                return;
            Safe("welsh repay", () =>
            {
                Ped me = Game.Player.Character;
                if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || PED.IS_PED_IN_COMBAT(me.Handle, 0))
                    return;
                foreach (Ped p in Peds.All())
                {
                    if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead || p.Position.DistanceTo(me.Position) > 4f)
                        continue;
                    string id, name;
                    if (!LastingId(p, out id, out name) || id == null || _wrOffered.Contains(id + "|" + now / 60000))
                        continue;
                    Happening owed = BrawlVerbs.UnpaidWelsh(_store.WorldLog, id, _store.LastGameMin);
                    if (owed == null || _store.LastGameMin - owed.At < BrawlVerbs.RepayAfterMinutes)
                        continue;   // not straight after: the welsh stands a while first (else it would mean nothing)
                    _wrOffered.Add(id + "|" + now / 60000);
                    _wrTo = p;
                    _wrAgainAt = now + 30000;
                    PromptSet(BrawlVerbs.RepayKind, new TownEvent { Kind = BrawlVerbs.RepayKind, Town = _placeTown, AId = id, AName = name }, 8000);
                    return;
                }
            });
        }

        /// J, "Pay him the dollar": the money, his word, his liking back, the wagers' paid-bet memory, the ledger's "welshpaid".
        private void WelshRepayPay(TownEvent e)
        {
            Ped to = _wrTo, me = Game.Player.Character;
            if (e == null || e.AId == null || to == null || !to.Exists() || to.IsDead || BrawlVerbs.UnpaidWelsh(_store.WorldLog, e.AId, _store.LastGameMin) == null)
                return;
            if (MONEY._MONEY_GET_CASH_BALANCE() < BrawlVerbs.StakeCents)
            {
                Hint("You haven't a dollar on you.", 2500);
                return;
            }
            MONEY._MONEY_DECREMENT_CASH_BALANCE(BrawlVerbs.StakeCents);
            SpeechCaption(Person.FirstOf(e.AName ?? "He"), BrawlVerbs.RepaidLine(e.AId), to.Handle, 4000, true, false);
            FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(to.Handle) : null;
            if (ac != null)
                _bridge.LikeMore(ac.Id, 12);   // back most of the way (the welsh took 15: he remembers he had to wait)
            RecordEvent(BrawlVerbs.Repaid, "player", "you", e.AId, e.AName, to.Position, me.Handle, me.Handle, to.Handle, "paid the bet he welshed on");
            if (_engine != null && !FromTestRow("player", me.Handle, me.Handle, to.Handle))
            {
                Minds.Party you = new Minds.Party { Id = "player", Name = _engine.KnowsYourName(e.AId) && YourFullName() != null ? YourFullName() : "the stranger", Label = "the stranger" };
                Minds.Party his = PartyOf(to, false);
                if (his != null && his.Id != null)
                    _engine.RememberScene("wagerpaid", you, his, null, _placeTown, null);   // the bet paid, in the wagers' own words
            }
            _wrRepaid++;
            Log.Write("BRAWL bet: you paid " + e.AName + " the dollar you welshed on; the betting men will take your money again");
        }
        private int _bvShunned;   // backers who wouldn't take your money, a welsher's (SEAMS item 4)

        /// The lost bet's prompt on J, once J is free; its 30 s count from then.
        private void BrawlVerbsPayPrompt(int now)
        {
            if (_bvOwedTo == null || _bvPayShown || _eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;
            _bvPayShown = true;
            _bvOwedUntil = now + BrawlVerbs.PayPromptMs;
            PromptSet(BrawlVerbs.PayKind, new TownEvent { Kind = BrawlVerbs.PayKind, Town = _placeTown, AName = _bvOwedTo.Name, Count = _bvOwedCents }, BrawlVerbs.PayPromptMs);
        }

        /// The menu's rows: break it up, stand with the man losing, the bet (with a backer waiting), leave them to it.
        private List<InteractOption> BrawlVerbsRows()
        {
            List<int> a, b;
            string backed = null, other = null;
            if (_bvBacker != null && _director.FightSides(out a, out b) && a.Count > 0 && b.Count > 0)
            {
                backed = NameOfFighter(_bvBacker.Side == 0 ? a[0] : b[0]);
                other = NameOfFighter(_bvBacker.Side == 0 ? b[0] : a[0]);
            }
            string between = null;
            if (BrawlVerbs.Reminds && _store != null && ENTITY.DOES_ENTITY_EXIST(_bvLoser))
            {
                MindWho lw = MindKnown(_bvLoser, Game.GameTime);
                if (lw != null && lw.Id != null)
                {
                    var stood = new HashSet<string>();
                    Interact.StoodByYou(_store.WorldLog, _store.LastGameMin, stood);   // once, at the menu: never per frame
                    between = BrawlVerbs.Between(stood.Contains(lw.Id), Interact.SavedLately(_store.WorldLog, lw.Id, _store.LastGameMin));
                }
            }
            return BrawlVerbs.Rows(NameOfFighter(_bvLoser), _bvBacker != null ? Person.FirstOf(_bvBacker.Name) : null, backed, other, MONEY._MONEY_GET_CASH_BALANCE(), between);
        }

        private void BrawlVerbChoose(string id)
        {
            int now = Game.GameTime;
            Ped me = Game.Player.Character;
            string seed = "bv|" + _director.FightId + "|" + now / 1000;
            List<int> a, b;
            bool fight = _director.FightSides(out a, out b);
            switch (id)
            {
                case "brawl:breakup":
                    if (fight)
                        BrawlVerbsBreakUp(me, a, b, seed, now);
                    break;
                case "brawl:stand":
                    if (fight)
                        BrawlVerbsStand(me, seed, now);
                    break;
                case "brawl:bet":
                    if (fight && _bvBacker != null)
                        BrawlVerbsBet(me, a, b, seed);
                    break;
                case "brawlpay:pay":
                    if (_bvOwedTo != null && MONEY._MONEY_GET_CASH_BALANCE() >= _bvOwedCents)
                    {
                        MONEY._MONEY_DECREMENT_CASH_BALANCE(_bvOwedCents);
                        _bvPaid++;
                        _bvLast = "you paid " + _bvOwedTo.Name + " the " + BrawlVerbs.Money(_bvOwedCents) + " you lost";
                        Log.Write("BRAWL bet: " + _bvLast);
                        _bvOwedTo = null;
                    }
                    break;
                case "brawlpay:no":
                    BrawlVerbsWelsh("you wouldn't pay");
                    break;
            }
        }

        /// "Break it up!": each fighter heeds it by his nature and by what he thinks of Arthur (the fond and the afraid more, the
        /// hostile less); those who heed stop and go their ways; of those who don't, one who'd go for a man near him turns on Arthur
        /// (Brawl.AtYou), his fight the game's from then on. With one side emptied, it's over.
        private void BrawlVerbsBreakUp(Ped me, List<int> a, List<int> b, string seed, int now)
        {
            SpeechCaption("You", BrawlVerbs.Shout(seed), me.Handle, 2500, false, false);
            int stopped = 0, at = 0, all = a.Count + b.Count;
            bool testFight = false;   // a Testing row's brawl: no deed of it (the review)
            bool saloon = _placeKind == "saloon";
            var said = new List<string>();
            foreach (int h in a.Concat(b).ToList())
            {
                testFight |= TestPerson(h);
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || AtArthur(h))
                    continue;   // (one already at Arthur is the game's: the review of items 1-13)
                MindWho w = MindKnown(h, now);
                Traits t = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
                bool drunk = w != null && InHisCups(new Ped(h), w.P);
                string regard = SightRegard(new Ped(h), w != null ? w.Id : null);
                int chance;
                bool stops = Brawl.Heeds(t, drunk, BrawlHealth(h) >= 0.75f, seed + "|" + h, out chance, 1, BrawlVerbs.Standing(regard));
                string name = w != null && w.Name != null ? Person.FirstOf(w.Name) : "a stranger";
                if (stops)
                {
                    if (DropFighter(h))   // (dropped even as the last of his side: the review of items 1-13)
                    {
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2500, 0f, 0f, 0f);
                        WanderLater(h, now + 6000);
                    }
                    stopped++;
                    said.Add(name + " stops (" + chance + "%" + (regard != null ? ", " + regard : "") + ")");
                    continue;
                }
                string why;
                if (Brawl.AtYou(t, drunk, regard == "hostile", regard == "afraid", false, Math.Max(4, all), saloon, seed + "|atyou|" + h, out why) && DropFighter(h))
                {
                    TASK.TASK_COMBAT_PED(h, me.Handle, 0, 16);   // his fight is the game's now (and enRAGE's): we let go of him
                    at++;
                    said.Add(name + " turns on you (" + why + ")");
                    continue;
                }
                said.Add(name + " keeps swinging (" + chance + "%)");
            }
            _bvBreakups++;
            if (_director.FightSides(out a, out b) && (a.Count == 0 || b.Count == 0 || stopped >= all - 1))
            {
                _director.Abort("Arthur broke it up");
                if (at == 0 && stopped > 0 && !testFight)
                    BreakUpKnown(me);   // a peace only when nobody turned on you for it, and never a test's (the review)
            }
            _bvLast = "you broke it up: " + string.Join("; ", said);
            Log.Write("BRAWL you shout to break it up: " + string.Join("; ", said));
        }

        /// "Stand with him": the one beating the man losing turns on Arthur (the game's from there); the man losing and his side
        /// think the better of him for it, the one beating him the worse.
        private void BrawlVerbsStand(Ped me, string seed, int now)
        {
            // whoever is beating him now (the review of items 1-13: the one at the offer may since be down, out or replaced)
            int loser = _bvLoser, beater = _director.TargetOf(_bvLoser);
            if (beater == 0)
                beater = _bvBeater;
            if (!ENTITY.DOES_ENTITY_EXIST(loser) || ENTITY.IS_ENTITY_DEAD(loser) || _director.IsOut(loser) || !ENTITY.DOES_ENTITY_EXIST(beater) || ENTITY.IS_ENTITY_DEAD(beater) ||
                _director.IsOut(beater) || _brawlOut.ContainsKey(beater) || AtArthur(beater) || !DropFighter(beater))
            {
                _bvLast = "too late to stand with " + NameOfFighter(loser) + ": the man beating him is down or out of it";
                Log.Write("BRAWL " + _bvLast);
                return;
            }
            MindWho lw = MindKnown(loser, now), bw = MindKnown(beater, now);
            TASK.TASK_COMBAT_PED(beater, me.Handle, 0, 16);   // at you now: the game's and enRAGE's (we let go of him)
            FolkRef la = _bridge != null ? _bridge.AcquaintanceAt(loser) : null, ba = _bridge != null ? _bridge.AcquaintanceAt(beater) : null;
            if (la != null)
                _bridge.LikeMore(la.Id, 10);   // as the allies' standing with someone in a fight (enRAGE's own method for its townsfolk)
            if (ba != null)
                _bridge.LikeMore(ba.Id, -10);
            if (lw != null && lw.Id != null)
            {
                RecordEvent("stoodby", "player", "you", lw.Id, lw.Name, ENTITY.GET_ENTITY_COORDS(loser, true, true), me.Handle, me.Handle, loser, "stood with him in a fight");
                // he keeps it (SEAMS item 2; [Encounters] StoodRemembered): the rescue's memory ("{who} stepped in for me"), so his
                // thoughts come back to it and some days on he comes over: "That fight. I was going down and you waded in beside me."
                if (_cfg.StoodRemembered && !FromTestRow("player", me.Handle, me.Handle, loser) && OwedMemory(lw.Id, _placeTown, "defend", "stood", 65))
                    Log.Write("BRAWL " + lw.Name + " keeps it, your standing with him: his thoughts, and his thanks to come");
            }
            _bvStands++;
            _bvLast = "you stood with " + (lw != null ? lw.Name : "a stranger") + " against " + (bw != null ? bw.Name : "a stranger");
            Log.Write("BRAWL you stand with " + (lw != null ? lw.Name : "a stranger") + "; " + (bw != null ? bw.Name : "the man beating him") + " turns on you");
        }

        /// The bet: Arthur takes the waiting backer's, a dollar on the other man; nothing changes hands until it's settled.
        private void BrawlVerbsBet(Ped me, List<int> a, List<int> b, string seed)
        {
            WagerMan backer = _bvBacker;
            if (backer == null || backer.Matched || a.Count == 0 || b.Count == 0)
                return;
            backer.Matched = true;
            var you = new WagerMan { H = me.Handle, Side = 1 - backer.Side, Cents = BrawlVerbs.StakeCents, Id = "player", Name = "you" };
            var bet = new WagerBet { A = backer, B = you, OnA = backer.Side == 0 ? a[0] : b[0], OnB = backer.Side == 0 ? b[0] : a[0], Cents = BrawlVerbs.StakeCents, Seed = seed + "|bet" };
            _wagerBets.Add(bet);
            _bvBets++;
            WagerSay(me, backer, BrawlVerbs.TakesYourBet(seed), me.Handle);
            Log.Write("BRAWL bet: you take " + backer.Name + "'s bet, " + BrawlVerbs.Money(bet.Cents) + " on " + NameOfFighter(bet.OnB) + " against " + NameOfFighter(bet.OnA));
        }

        /// From the wagers' settling (WagersFightOver): a bet of Arthur's. Won, the backer pays by his nature (or owes it, or
        /// won't); lost, Arthur owes it, and J offers to pay for 30 s.
        private void BrawlVerbsSettle(WagerBet bet, bool youWon, Ped me)
        {
            WagerMan other = bet.A.Id == "player" ? bet.B : bet.A;
            if (!youWon)
            {
                _bvOwedTo = other;
                _bvOwedCents = bet.Cents;
                _bvOwedUntil = Game.GameTime + BrawlVerbs.PayPromptMs;
                _bvPayShown = false;
                WagerSay(me, other, BrawlVerbs.WantsHisDollar(bet.Seed), me.Handle);
                BrawlVerbsPayPrompt(Game.GameTime);   // now if J is free, else at the first look that finds it free
                Log.Write("BRAWL bet: your man lost; you owe " + other.Name + " " + BrawlVerbs.Money(bet.Cents));
                return;
            }
            int pct;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(other.H, true, true);
            string how = Wagers.Settle(other.T, other.Drunk, 0, BrawlFindLaw(at, 30f, false, 0) != null, bet.Cents, bet.Seed + "|settle", out pct);
            if (how == Wagers.Welshes || how == Wagers.Owes)
            {
                WagerSay(me, other, Wagers.Fill(Wagers.LoserLine(how, bet.Seed), bet.Cents, null, null, "you"), me.Handle);
                Log.Write("BRAWL bet: your man won; " + other.Name + " " + (how == Wagers.Welshes ? "won't pay you" : "says he'll owe it you"));
                return;
            }
            MONEY._MONEY_INCREMENT_CASH_BALANCE(bet.Cents, 0);
            _bvWon++;
            WagerSay(me, other, BrawlVerbs.PaysYourBet(bet.Seed), me.Handle);
            Log.Write("BRAWL bet: your man won; " + other.Name + " pays you " + BrawlVerbs.Money(bet.Cents));
        }

        /// A fight you broke up on J is a "peace" deed, as a word stepping into a scene is (Frontier.cs SceneReaction; SEAMS item
        /// 6; [Scenes] BreakUpKnown): the witnesses keep it, it goes round as gossip ("broke up a fight"), a newspaper might carry
        /// it and the town's ledger counts it among your good deeds. Before, the J menu's shout ended the fight and nobody but
        /// the fighters knew. Only when the fight is over for your shout (the director's Abort), never for a shout that failed.
        private void BreakUpKnown(Ped me)
        {
            if (!_cfg.BreakUpKnown || !_cfg.Deeds || _engine == null)
                return;
            try
            {
                Vector3 pos = me.Position;
                long g = Bridge.Now();
                NoteDeedMinds(_engine.OnDeed(Deeds.Make("peace", "a fight", _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 25f)), WitnessesNear(pos, 0, 25f), g), pos, 0);
                Log.Write("BRAWL you broke it up: a peace deed, the town will hear of it");
            }
            catch (Exception ex)
            {
                Log.Detail("BRAWL the break-up's deed couldn't be kept: " + ex.Message);
            }
        }

        /// The man you welshed on keeps it, and so do those who saw (SEAMS item 3; [Scenes] WelshRemembered): the wagers' own
        /// "welsh" memory, as when one of the street welshes on another (Wagers.cs), with you its doer. Before, a welsh of yours
        /// cost only enRAGE's -15 and a spoken line: no memory was written, and nobody watching kept a thing. Now he
        /// thinks it in the bet's own words ("Won fair and square and he won't pay"), and the watchers' "Saw him welsh on..." can
        /// be told on. Memories only: no tie, no scene, nobody sent after you (the callouts read enRAGE's liking, as before).
        private void WelshRemembered(Ped me, WagerMan m)
        {
            if (!_cfg.WelshRemembered || _engine == null || m == null || m.Id == null || !ENTITY.DOES_ENTITY_EXIST(m.H) || FromTestRow("player", me.Handle, me.Handle, m.H))
                return;
            try
            {
                Ped him = new Ped(m.H);
                Minds.Party you = new Minds.Party { Id = "player", Name = _engine.KnowsYourName(m.Id) && YourFullName() != null ? YourFullName() : "the stranger", Label = "the stranger" };
                Minds.Party his = PartyOf(him, false);
                if (his == null || his.Id == null)
                    return;
                List<Minds.Party> watchers = MindWatchers(new List<Ped> { me, him }, SceneWatchMetres, false);
                _engine.RememberScene("welsh", you, his, watchers, _placeTown, _engine.NowIndoors ? _engine.NowPlace : null);
                Log.Write("BRAWL bet: " + m.Name + " won't forget you welshing" + (watchers != null && watchers.Count > 0 ? ", nor will " + watchers.Count + " who saw" : ""));
            }
            catch (Exception ex)
            {
                Log.Detail("BRAWL bet: the welsh couldn't be remembered: " + ex.Message);
            }
        }

        /// Arthur didn't pay a lost bet: the one he owed thinks the worse of him, says so, and keeps it.
        private void BrawlVerbsWelsh(string why)
        {
            WagerMan m = _bvOwedTo;
            _bvOwedTo = null;
            if (m == null)
                return;
            _bvWelshed++;
            Ped me = Game.Player.Character;
            FolkRef ac = _bridge != null && ENTITY.DOES_ENTITY_EXIST(m.H) ? _bridge.AcquaintanceAt(m.H) : null;
            if (ac != null)
                _bridge.LikeMore(ac.Id, -15);
            if (m.Id != null && ENTITY.DOES_ENTITY_EXIST(m.H))
            {
                RecordEvent("welshed", "player", "you", m.Id, m.Name, ENTITY.GET_ENTITY_COORDS(m.H, true, true), me.Handle, me.Handle, m.H, "welshed on a bet");
                WagerSay(me, m, BrawlVerbs.WelshedOn("w|" + m.Id), me.Handle);
                WelshRemembered(me, m);
            }
            _bvLast = "you welshed on " + m.Name + " (" + why + ")";
            Log.Write("BRAWL bet: " + _bvLast);
        }

        /// F7: what Arthur has done in brawls this session.
        private string BrawlVerbsStatus()
        {
            if (!_cfg.BrawlVerbs)
                return "off ([Scenes] BrawlVerbs)";
            return "broke up " + _bvBreakups + ", stood with " + _bvStands + ", bets " + _bvBets + " (won " + _bvWon + ", paid " + _bvPaid + ", welshed " + _bvWelshed + (_bvShunned > 0 ? ", refused you " + _bvShunned : "") + (_wrRepaid > 0 ? ", paid late " + _wrRepaid : "") + ")" +
                   (_bvOwedTo != null ? "; you owe " + _bvOwedTo.Name + " " + BrawlVerbs.Money(_bvOwedCents) : "") + (_bvLast != null ? "; last: " + _bvLast : "");
        }
    }
}
