// A night together, off screen (0.6.3, [Romance] Trysts; Lex: the saloon's women always willing, single women likely,
// married ones less so, Interest.ForPlayer). When a woman says yes to the player's flirting, flirting with her again
// within two minutes is asking her somewhere quieter: the two lines (Core/Trysts.cs), then the screen fades out, an
// hour passes on the clock, it fades back in, and her thought after shows over her head. Nothing is shown. The player's
// controls are off while it's dark; a watchdog outside the feature's guard brings the screen back whatever happens.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class TrystOffer
        {
            public int Handle, Until;
            public string Name, Kind;
            public Person Who;
            public bool Hookups;   // the night is CasualHookups' (WestworldScript.Hookups.cs): no fade of ours
            public bool Night;     // her yes to the flirting is a yes to the night too; else asking gets her no (Trysts.NightChance)
            public int Greeted;    // he greeted her first (1) or flirted cold (-1)
            public bool Asked;     // Hook up has been asked (CasualHookups' night): H is Flirt again, and its own Flirt takes it on
            public string Where;   // upstairs, home, hotel, quiet (Trysts.Where; item 15)
            public int Cents;      // what the saloon took
        }

        private sealed class TrystRun
        {
            public TrystOffer Offer;
            public int Phase, At;
            public string After;
            public bool Recorded;   // written down (the engine's record, the husband's chance): the price stays with the saloon from then
        }

        private TrystOffer _trystOffer;
        private TrystRun _tryst;
        private int _trystDarkSince, _trystCount, _trystHints, _nextTrystLook, _trystMornings, _trystExpects, _trystCooled;
        private string _trystLast;
        private Action _aTryst;

        /// A woman said yes (FlirtResult.Tryst): a second flirt soon is the ask.
        private void TrystOffered(FlirtResult r, string name, int ped)
        {
            if (r == null || !r.Tryst || r.Who == null || !_cfg.Trysts)
                return;
            // a kind CasualHookups turns down, or no to the night: the night is ours, or nobody's
            bool hookups = r.Night && HookupsLinked && !HookupsWont(ped);
            if (r.Night && HookupsLinked && !hookups)
                Log.Write("HOOKUPS: CasualHookups won't take " + name + "'s kind (" + (ModelOf(ped) ?? "?") + "), so her Hook up is RealNPCs' own night (the fade)");
            _trystOffer = new TrystOffer { Handle = ped, Name = name, Kind = r.TrystKind, Who = r.Who, Hookups = hookups, Night = r.Night, Greeted = r.Greeted,
                                           Until = Game.GameTime + (hookups ? HookupWillingMs : Trysts.OfferMs) };
            Log.Write("TRYST offered: " + name + " (" + r.TrystKind + ", chance " + r.Chance + "%, the night " + r.NightChance + "% -> " + (r.Night ? "yes" : "no") +
                      (r.Greeted > 0 ? ", greeted first" : r.Greeted < 0 ? ", flirted cold" : "") + "): " +
                      (!r.Night ? "no Hook up today (the H prompt stays Flirt)" : hookups ? "willing for CasualHookups' Flirt (" + HookupsKeyText + ") for " + HookupWillingMs / 1000 + " s" : "Hook up (H, once her answer is said) within " + Trysts.OfferMs / 1000 + " s"));
            if (r.Night && _trystHints < 3)
            {
                _trystHints++;
                Feed.Notice("RealNPCs", hookups ? name + " is willing: Hook up (H), then CasualHookups' Flirt (" + HookupsKeyText + "), in the next five minutes."
                                                : name + " is willing: Hook up (H) within two minutes to go somewhere quieter.");
            }
        }

        /// The second flirt, on the woman who said yes: true if it was taken as the ask.
        private bool TrystAsked(Ped her)
        {
            TrystOffer o = _trystOffer;
            if (o == null || her == null || her.Handle != o.Handle || Game.GameTime > o.Until || _tryst != null || !_cfg.Trysts)
                return false;
            Ped me = Game.Player.Character;
            string cant = MISC.GET_MISSION_FLAG() ? "a story mission" : PED.IS_PED_IN_COMBAT(me.Handle, 0) ? "there's fighting going on" : PED.IS_PED_ON_MOUNT(me.Handle) ? "you're on horseback" :
                          PED.IS_PED_IN_COMBAT(her.Handle, 0) || PED.IS_PED_FLEEING(her.Handle) ? "she's frightened off" : her.Position.DistanceTo(me.Position) > 6f ? "she's too far off" : null;
            if (cant != null)
            {
                Log.Write("TRYST not now: " + cant);
                return true;   // still taken as the ask, so the flirt doesn't count against her
            }
            string seed = o.Who.Id + "|" + Bridge.Now() / 60;
            o.Where = Trysts.Where(o.Kind, _placeKind == "saloon", o.Who.Home != null && o.Who.Home == _placeTown, Trysts.HasHotel(_placeTown), seed);   // where they go (item 15)
            string ask = Trysts.Ask(seed), yes = Trysts.Yes(o.Kind, o.Where, seed);
            Feed.DropAhead();
            Feed.Say(_engine.Player.FirstName, ask, FlirtVoice(_engine.Player.FirstName, Speech.WhenFirst(me.Handle, FlirtVoices.You(true, seed), her.Handle)), ahead: true);
            if (!o.Night)
            {
                // she liked the flirting, not that much: her no, in words and a voice that fit (a cold approach hears of it)
                string no = Trysts.NotTonight(o.Kind, o.Greeted, seed);
                Feed.Say(o.Name, no, FlirtVoice(o.Name, Speech.WhenFirst(her.Handle, FlirtVoices.Reply(o.Who, "no", o.Greeted < 0 ? "cold" : "busy", 0, seed), me.Handle)), toYou: true, ahead: true);
                _flirtSaidUntil = Game.GameTime + 7000;
                _trystOffer = null;
                Log.Write("TRYST asked: " + o.Name + " (" + o.Kind + "): " + ask + " -> not tonight" + (o.Greeted < 0 ? " (flirted cold)" : "") + ": " + no);
                return true;
            }
            // the saloon's price (item 15): said before her yes, taken when he has it; without it, no night. Taken once: the same
            // offer asked again (CasualHookups started nothing, "ask her again") was charged twice (the review of items 15-19)
            int price = o.Kind == "saloon" && _cfg.TrystsAfter && _cfg.TrystPrice > 0 && o.Cents == 0 ? _cfg.TrystPrice * 100 : 0;
            if (price > 0 && MONEY._MONEY_GET_CASH_BALANCE() < price)
            {
                string broke = Trysts.CantPay(_cfg.TrystPrice, seed);
                Feed.Say(o.Name, broke, FlirtVoice(o.Name, Speech.WhenFirst(her.Handle, FlirtVoices.Reply(o.Who, "no", "busy", 0, seed), me.Handle)), toYou: true, ahead: true);
                _flirtSaidUntil = Game.GameTime + 7000;
                _trystOffer = null;
                Log.Write("TRYST asked: " + o.Name + " (saloon): " + ask + " -> you can't pay the $" + _cfg.TrystPrice + ": " + broke);
                return true;
            }
            if (price > 0)
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(-price, 0);
                o.Cents = price;
                yes = Trysts.PriceLine(_cfg.TrystPrice, seed) + " " + yes;
            }
            Feed.Say(o.Name, yes, FlirtVoice(o.Name, Speech.WhenFirst(her.Handle, FlirtVoices.Night(o.Kind, seed), me.Handle)), toYou: true, ahead: true);
            _flirtSaidUntil = Game.GameTime + 7000;
            if (o.Hookups && (!HookupsLinked || HookupsWont(her.Handle)))
            {
                // learned since her yes that CasualHookups won't take her kind (or RealNPCs no longer decides): our own night
                o.Hookups = false;
                Log.Write("HOOKUPS: " + o.Name + "'s night is RealNPCs' own after all (" + (HookupsLinked ? "CasualHookups won't take her kind, " + (ModelOf(her.Handle) ?? "?") : "RealNPCs no longer decides for CasualHookups") + ")");
            }
            if (o.Hookups)
            {
                // the night is CasualHookups': the words only, and she stays willing a while longer
                o.Until = Math.Max(o.Until, Game.GameTime + HookupWillingMs);
                o.Asked = true;   // asked: the prompt goes back to Flirt, and CasualHookups' own Flirt takes it on
                Log.Write("TRYST asked: " + o.Name + " (" + o.Kind + "): " + ask + " -> " + yes + "; CasualHookups' Flirt (" + HookupsKeyText + ") next");
                HookupsAsked(o, Game.GameTime);   // press B for a kind it takes; else a watch, and RealNPCs' own night if nothing begins
                return true;
            }
            _tryst = new TrystRun { Offer = o, Phase = 0, At = Game.GameTime + 4500 };
            _trystOffer = null;
            Log.Write("TRYST asked: " + o.Name + " (" + o.Kind + ", " + o.Where + (o.Cents > 0 ? ", $" + o.Cents / 100 + " paid" : "") + "): " + ask + " -> " + yes);
            return true;
        }

        /// She said yes and CasualHookups started nothing: she says yes again, in her voice, and the night is RealNPCs' own,
        /// so her words and what happens agree (2026-10-26, Lex). If now won't do, a second flirt still asks her. `sayYes`
        /// false: she said yes to Hook up a moment ago, so straight to the dark without saying it twice.
        private void TrystFallback(TrystOffer o, string why, bool sayYes = true, bool tell = false)
        {
            if (o == null || _tryst != null)
                return;
            Ped me = Game.Player.Character;
            bool there = ENTITY.DOES_ENTITY_EXIST(o.Handle) && !ENTITY.IS_ENTITY_DEAD(o.Handle) && new Ped(o.Handle).Position.DistanceTo(me.Position) <= 8f;
            string cant = !there ? "she's gone or too far off" : MISC.GET_MISSION_FLAG() ? "a story mission" : PED.IS_PED_IN_COMBAT(me.Handle, 0) ? "there's fighting going on" :
                          PED.IS_PED_ON_MOUNT(me.Handle) ? "you're on horseback" : PED.IS_PED_IN_COMBAT(o.Handle, 0) || PED.IS_PED_FLEEING(o.Handle) ? "she's frightened off" : null;
            o.Hookups = false;
            o.Asked = false;   // RealNPCs' own night may be asked now: H is Hook up again
            if (cant != null)
            {
                o.Until = Game.GameTime + Trysts.OfferMs;
                if (!there)
                    TrystRefund(o, "she's gone or too far off, and the offer with her");   // (the review)
                _trystOffer = there ? o : null;
                Log.Write("TRYST " + o.Name + ": " + why + ", and RealNPCs' own night can't be now (" + cant + ")" + (there ? "; Hook up (H) to ask her again" : ""));
                if (tell && there)
                    Feed.Notice("RealNPCs", "CasualHookups didn't take " + o.Name + ". Hook up (H) to ask her again.");
                return;
            }
            if (tell)
                Feed.Notice("RealNPCs", "CasualHookups didn't take " + o.Name + ": RealNPCs' own night instead.");
            string seed = o.Who.Id + "|" + Bridge.Now() / 60;
            string yes = sayYes ? Trysts.Yes(o.Kind, o.Where, seed) : null;
            if (sayYes)
            {
                Feed.Say(o.Name, yes, FlirtVoice(o.Name, Speech.WhenFirst(o.Handle, FlirtVoices.Night(o.Kind, seed), me.Handle)), toYou: true, ahead: true);
                _flirtSaidUntil = Game.GameTime + 7000;
            }
            _trystOffer = null;
            _tryst = new TrystRun { Offer = o, Phase = 0, At = Game.GameTime + (sayYes ? 4500 : 1500) };
            Log.Write("TRYST " + o.Name + " (" + o.Kind + "): " + why + ", so RealNPCs' own night" + (sayYes ? ": " + yes : " (her yes to Hook up already said)"));
        }

        private void TrystStep(int now)
        {
            if (_aTryst == null)
                _aTryst = () => TrystTick(_now);
            Safe("trysts", _aTryst);
        }

        private void TrystTick(int now)
        {
            TrystRun t = _tryst;
            if (t == null)
            {
                TrystOffer o = _trystOffer;
                if (o != null && o.Cents > 0 && _hookRun == null && now > o.Until)
                {
                    TrystRefund(o, "her offer lapsed with the price paid");   // paid and never asked again in time, no watch going: the saloon's money back (the review)
                    _trystOffer = null;
                }
                TrystAfterLook(now);   // the days after (item 15)
                return;
            }
            if (now < t.At)
                return;
            int h = t.Offer.Handle, player = Game.Player.Handle;
            switch (t.Phase)
            {
                case 0:   // the words said: dark, if nothing has come between
                    if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(Game.Player.Character.Handle, 0) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    {
                        Log.Write("TRYST called off before the dark: something came between");
                        TrystRefund(t.Offer, "the night was called off before the dark");
                        _tryst = null;
                        return;
                    }
                    if (MayDirect(new Ped(h)))
                        TASK.TASK_STAND_STILL(h, 8000);
                    PLAYER.SET_PLAYER_CONTROL(player, false, 0, false);
                    CAM.DO_SCREEN_FADE_OUT(1500);
                    _trystDarkSince = now;
                    t.Phase = 1;
                    t.At = now + 1700;
                    break;
                case 1:   // dark: an hour passes
                    if (!CAM.IS_SCREEN_FADED_OUT() && now - _trystDarkSince < 4000)
                    {
                        t.At = now + 200;
                        return;
                    }
                    CLOCK.ADD_TO_CLOCK_TIME(1, 0, 0);
                    t.After = _engine.Tryst(t.Offer.Who, t.Offer.Kind, Bridge.Now(), _placeTown, _placeKind == "saloon", t.Offer.Cents);   // written down; a husband may hear (item 15)
                    t.Recorded = true;
                    t.Phase = 2;
                    t.At = now + 2500;
                    break;
                case 2:   // back
                    CAM.DO_SCREEN_FADE_IN(1500);
                    PLAYER.SET_PLAYER_CONTROL(player, true, 0, false);
                    _trystDarkSince = 0;
                    t.Phase = 3;
                    t.At = now + 2200;
                    break;
                default:   // her thought, after
                    if (t.After != null && ENTITY.DOES_ENTITY_EXIST(h))
                        _ticker.Push(t.Offer.Name + ", thinking", t.After, "near", h);
                    _trystCount++;
                    _trystLast = t.Offer.Name + " (" + t.Offer.Kind + ")";
                    Log.Write("TRYST over: " + t.Offer.Name + " (" + t.Offer.Kind + "); she thinks: " + t.After);
                    _tryst = null;
                    break;
            }
        }

        /// The days after (item 15; [Romance] TrystsAfter): every twenty seconds with the screen lit and women of past nights
        /// remembered, the known people near you in view: her thought the morning after (6-36 h on), a sweetheart's word to
        /// your face when you've been gone three days after two nights (within 8 m), or the thought of one who gave up at a
        /// fortnight; each once (Core/Engine.Trysts.cs keeps the once). The husband comes by the kin's walk-up.
        private void TrystAfterLook(int now)
        {
            // _mindNear is the mind shadow's list of the known people near (WestworldScript.Mind.cs): with [Mind] MindShadow off nobody is
            // ever near here and the days after are never seen (said in F7; the review of items 15-19)
            if (_cfg == null || !_cfg.TrystsAfter || now < _nextTrystLook || _store == null || _store.Trysts == null || _store.Trysts.Count == 0 || _placeTown == null || _mindNear.Count == 0)
                return;
            _nextTrystLook = now + 20000;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead || MISC.GET_MISSION_FLAG())
                return;
            long g = Bridge.Now();
            for (int i = 0; i < _mindNear.Count; i++)
            {
                int h = _mindNear[i];
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                bool remembered = false;
                for (int j = 0; j < _store.Trysts.Count; j++)
                    if (_store.Trysts[j] != null && _store.Trysts[j].WomanId == w.Id)
                    {
                        remembered = true;
                        break;
                    }
                if (!remembered)
                    continue;
                Ped her = new Ped(h);
                float d = her.Position.DistanceTo(me.Position);
                if (d > 12f || !NodInView(me, her))
                    continue;
                string line = _engine.TrystMorning(w.Id, g);
                if (line != null)
                {
                    _ticker.Push(w.Name + ", thinking", line, "near", h);
                    _trystMornings++;
                    Log.Write("TRYST the morning after: " + w.Name + " thinks: " + line);
                    continue;
                }
                string word = d <= 8f ? _engine.TrystExpects(w.Id, g) : null;   // asked only within 8 m: the engine's once is spent by the asking (the review)
                if (word != null)
                {
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4000, 0, 51, 0);
                    Feed.Say(w.Name, word, Speech.When(h, "angry", me.Handle), h, toYou: true);
                    _trystExpects++;
                    Log.Write("TRYST a sweetheart: " + w.Name + " to your face: " + word);
                    continue;
                }
                string cooled = _engine.TrystCooled(w.Id, g);
                if (cooled != null)
                {
                    _ticker.Push(w.Name + ", thinking", cooled, "near", h);
                    _trystCooled++;
                    Log.Write("TRYST cooled: " + w.Name + " thinks: " + cooled);
                }
            }
        }

        /// InWorld() is false while the screen is faded out, so the tryst's dark is stepped from here, or it never ends
        /// (2026-10-26: Lex was left on a black screen with the controls off; the step and its watchdog both sat behind
        /// that gate, in Tick04).
        private void TrystInTheDark()
        {
            if (_engine == null || (_tryst == null && _trystDarkSince == 0) || Game.IsLoading || Game.IsPaused)
                return;
            int now = Game.GameTime;
            _now = now;
            TrystWatchdog(now);
            TrystStep(now);
        }

        /// Outside the feature's guard, every tick: if the screen has been dark for a tryst longer than it ever should,
        /// bring it back and give the player his controls, whatever went wrong.
        private void TrystWatchdog(int now)
        {
            if (_trystDarkSince == 0 || now - _trystDarkSince < 12000)
                return;
            try
            {
                CAM.DO_SCREEN_FADE_IN(500);
                PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, true, 0, false);
                Log.Write("TRYST watchdog: the screen was dark too long; brought back");
                if (_tryst != null && !_tryst.Recorded)
                    TrystRefund(_tryst.Offer, "the watchdog brought the screen back before the night was written down");
            }
            catch
            {
            }
            _trystDarkSince = 0;
            _tryst = null;
        }

        /// A mission started, or the add-on is stopping: the light back and the controls with it, if it was dark.
        private void TrystAbort()
        {
            TrystRefund(_trystOffer, "the offer was dropped (a mission, or the add-on stopping)");   // the saloon's price back when no night comes of it (the review)
            if (_tryst != null && !_tryst.Recorded)
                TrystRefund(_tryst.Offer, "the night was cut short before it was written down");
            _trystOffer = null;
            _hookRun = null;
            if (_tryst == null && _trystDarkSince == 0)
                return;
            try
            {
                if (_trystDarkSince != 0 || (_tryst != null && _tryst.Phase >= 1))
                {
                    CAM.DO_SCREEN_FADE_IN(500);
                    PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, true, 0, false);
                }
            }
            catch
            {
            }
            _trystDarkSince = 0;
            _tryst = null;
        }

        /// The saloon's price given back when no night came of it (the review of items 15-19): once per offer, said in the log.
        private void TrystRefund(TrystOffer o, string why)
        {
            if (o == null || o.Cents <= 0)
                return;
            try
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(o.Cents, 0);
                Log.Write("TRYST the saloon's $" + o.Cents / 100 + " given back: " + why);
            }
            catch (Exception ex)
            {
                Log.Write("TRYST the saloon's $" + o.Cents / 100 + " couldn't be given back (" + ex.Message + ")");
            }
            o.Cents = 0;
        }

        /// F7: the setting, the chances, a waiting offer, this session's.
        private string TrystStatus()
        {
            if (!_cfg.Trysts)
                return "off ([Romance] Trysts); women answer by chance: single " + _cfg.WillingSingle + "%, married or spoken for " + _cfg.WillingTaken + "%, the saloon's always";
            string offer = _trystOffer != null && Game.GameTime < _trystOffer.Until ? _trystOffer.Name + " (" + _trystOffer.Kind + ", " + (_trystOffer.Until - Game.GameTime) / 1000 + " s left)" : "none";
            return "on; women answer by chance: single " + _cfg.WillingSingle + "%, married or spoken for " + _cfg.WillingTaken + "%, the saloon's always, what they think of you tipping it; " +
                   "offer waiting: " + offer + (_tryst != null ? "; one going on (phase " + _tryst.Phase + ")" : "") + "; this session " + _trystCount + (_trystLast != null ? ", last " + _trystLast : "") +
                   "; the saloon's price $" + _cfg.TrystPrice + (_cfg.MindShadow ? "" : "; the days after's looks need [Mind] MindShadow, which is off") + "; " + (_engine != null ? _engine.TrystsStatus(Bridge.Now()) : "") + " (this session " + _trystMornings + " mornings, " + _trystExpects + " words, " + _trystCooled + " cooled)";
        }
    }
}
