// Wanted, but which one? in game (FIVEDAY reserve R31; Core/WantedWhich.cs). Every two seconds in a town: the first named
// resident within 4 m whose worry is the poster (InnerLife.WorryNow "law", stage 2 or before) and not yet settled. The first
// time, the box says you know the face and they think it; then, near them, the J prompt: turn them in (the bounty; gone in
// irons), warn them (yours for good, as one you saved: WestworldScript.Saved.cs), buy them a ticket (gone by the train:
// Engine.Departed), or leave it. Behind [World] WantedWhich.
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _wantedNextLook, _wantedKnown, _wantedTaken, _wantedWarned, _wantedTrained, _wantedAskWho;
        private TownEvent _wantedAsk;
        private string _wantedTestFor;   // the Testing row's stand-in: treated as on the poster whatever their worry

        private void WantedStep(int now)
        {
            if (_cfg == null || !_cfg.WantedWhich || _placeTown == null || _engine == null || _store == null || now < _wantedNextLook)
                return;
            _wantedNextLook = now + 2000;
            Ped me = Game.Player.Character;
            long day = _store.LastGameMin / 1440;
            foreach (int h in _mindNear)
            {
                MindWho mw;
                if (!_mindWho.TryGetValue(h, out mw) || mw.Id == null || mw.P == null || !Resident.IsResident(mw.Id) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (new Ped(h).Position.DistanceTo(me.Position) > WantedWhich.NearMetres)
                    continue;
                int seen;
                _store.Counts.TryGetValue(WantedWhich.Key(mw.Id), out seen);
                if (seen >= WantedWhich.Settled)
                    continue;
                string worry;
                int stage;
                bool good;
                if (mw.Id != _wantedTestFor && (!InnerLife.WorryNow(mw.P, day, out worry, out stage, out good) || !WantedWhich.OnThePoster(worry, stage)))
                    continue;
                Resident r = _store.FindResident(mw.Id);
                if (r == null || r.Dead || r.Left || r.Fled || r.Held != null || r.PostId != null)   // (a keeper's counter would keep them; the review)
                    continue;
                if (seen == 0)
                {
                    _store.Counts[WantedWhich.Key(r.Id)] = WantedWhich.Seen;
                    _wantedKnown++;
                    Notice(_placeTown, WantedWhich.KnowsLine(r.Name, r.Id), "news");
                    _ticker.Push((mw.Name ?? r.Name) + ", thinking", WantedWhich.ThoughtLine(r.Id), "near", h);
                    Log.Write("WANTED you know " + r.Name + " from the poster (" + WantedWhich.Crime(r.Id) + ")");
                }
                if (_eventMenu == null && (_eventPrompt == null || _eventPrompt == WantedWhich.Kind || now > _eventPromptUntil))
                {
                    if (_wantedAsk == null || _wantedAsk.AId != r.Id)
                        _wantedAsk = new TownEvent { Kind = WantedWhich.Kind, Town = _placeTown, AId = r.Id, AName = r.Name, Count = WantedWhich.Cents(r.Id) };
                    _wantedAskWho = h;
                    PromptSet(WantedWhich.Kind, _wantedAsk, WantedWhich.PromptMs);
                }
                return;
            }
        }

        /// The J menu's choice: turn them in, warn them, buy the ticket, or leave it.
        private void WantedChoice(TownEvent e, string what)
        {
            if (e == null || e.Kind != WantedWhich.Kind)
                return;
            Resident r = _store.FindResident(e.AId);
            if (r == null || r.Left || r.Fled || r.Dead)
                return;
            if (what == "train" && MONEY._MONEY_GET_CASH_BALANCE() < WantedWhich.TicketCents)
                return;   // not settled: the choice comes back (the review)
            if (what == "leave")
            {
                _store.Counts[WantedWhich.Key(r.Id)] = WantedWhich.Settled;   // left be: never asked again (the review: J came back every two seconds)
                _wantedAsk = null;
                return;
            }
            int now = Game.GameTime, h = _wantedAskWho;
            bool there = h != 0 && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h);
            _store.Counts[WantedWhich.Key(r.Id)] = WantedWhich.Settled;
            string first = Person.FirstOf(r.Name);
            if (what == "turnin")
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(e.Count, 0);
                _engine.GoneFromTown(r, Bridge.Now(), "turned in by you on the poster: up the line in irons");
                _wantedTaken++;
                Notice(e.Town, WantedWhich.TakenLine(r.Name, r.Id), "news");
            }
            else if (what == "warn")
            {
                if (there)
                {
                    SpeechCaption(first, WantedWhich.WarnedLine(r.Id), h, 6000, true, false);
                    SavedByYou(h, "you warned them the poster was up");   // they owe you: as one you saved
                }
                _wantedWarned++;
                Log.Write("WANTED you warned " + r.Name);
            }
            else if (what == "train")
            {
                MONEY._MONEY_DECREMENT_CASH_BALANCE(WantedWhich.TicketCents);
                if (there)
                    SpeechCaption(first, WantedWhich.TrainLine(r.Id), h, 6000, true, false);
                _engine.Departed(r, Bridge.Now(), true, "a ticket you bought them, and a new name");
                _wantedTrained++;
            }
            if (there && what != "warn")
                WanderLater(h, now + 4000);   // they go their way; never cast again (Left, Fled)
            _wantedAsk = null;
        }

        private string WantedStatus()
        {
            return "wanted: " + (_cfg != null && _cfg.WantedWhich ? "on" : "off") + ", faces known " + _wantedKnown + " this session, turned in " + _wantedTaken +
                   ", warned " + _wantedWarned + ", put on the train " + _wantedTrained;
        }
    }
}
