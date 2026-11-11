// Hire a man to watch your horse, the game side (FOURTEENHOUR reserve R21; Core/HorseWatch.cs; [Scenes] HorseWatch). A look every 4 s:
// down off your horse in a town by day, within 6 m of her, once a town a day: one of the mind's men within 15 m, down on his luck and free,
// walks over (the friend calls' walk) and offers; J: a dime, or no. Hired, he goes to your horse and stands by her (a stand, not a
// gathering: one man); a horse thief who goes for a horse within 20 m of him meets his shout and runs (the thief's scene ends);
// back in the saddle (or half an hour, or you 150 m off) he says so, a little fonder of you, and goes on his way.
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class WatchRun
        {
            public Ped Man, Horse;
            public string Id, Name, Seed, Phase = "offer";   // offer, ask, watch
            public int Until, SaidAt, StartedAt;
            public bool Chose, Test;
        }

        private WatchRun _watch;
        private int _watchLookAt, _watchOffers, _watchHired, _watchShouts;
        private string _watchLast;

        private void HorseWatchStep(int now)
        {
            if (!HorseWatch.On || now < _watchLookAt || _store == null)
                return;
            _watchLookAt = now + (_watch != null ? 500 : HorseWatch.LookMs);
            Ped me = Game.Player.Character;
            WatchRun w = _watch;
            if (w != null)
            {
                WatchRunStep(w, me, now);
                return;
            }
            if (_placeTown == null || _interiorType != 0 || PED.IS_PED_ON_MOUNT(me.Handle) || !HorseWatch.Hour(CLOCK.GET_CLOCK_HOURS()) || MISC.GET_MISSION_FLAG() || WalkUpGoing() || _director.Busy)
                return;
            int mount = PED._GET_LAST_MOUNT(me.Handle);
            if (mount == 0 || !ENTITY.DOES_ENTITY_EXIST(mount) || ENTITY.IS_ENTITY_DEAD(mount) || new Ped(mount).Position.DistanceTo(me.Position) > 6f)
                return;
            string key = "horsewatch|" + _placeTown + "|" + _store.LastGameMin / 1440;
            if (_nightDone.Contains(key))
                return;
            WatchOffer(me, new Ped(mount), now, false, key);
        }

        private bool WatchOffer(Ped me, Ped horse, int now, bool test, string key)
        {
            long day = _store.LastGameMin / 1440;
            foreach (Candidate c in ReasonCandidates(me, now, test).Where(x => !x.Who.Woman && x.Ped.Position.DistanceTo(me.Position) < HorseWatch.OfferMetres).OrderBy(x => x.Ped.Position.DistanceTo(me.Position)))
            {
                if (PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || NotFree(c.Ped.Handle) || !NodInView(me, c.Ped))
                    continue;
                MindWho mw = MindKnown(c.Ped.Handle, now);
                string wk = null; int st; bool good;
                if (c.Who.P != null && !InnerLife.WorryNow(c.Who.P, day, out wk, out st, out good))
                    wk = null;
                if (!test && !HorseWatch.DownOnLuck(mw != null && mw.Needs != null ? mw.Needs[NeedState.Money] : 0f, wk))
                    continue;
                if (key != null)
                    _nightDone.Add(key);   // once a town a day, whatever you say
                int h = c.Ped.Handle;
                float metres = c.Ped.Position.DistanceTo(me.Position);
                int walkMs = Math.Min(20000, 4000 + (int)(900 * metres));
                PED.SET_PED_KEEP_TASK(h, true);
                TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
                _watch = new WatchRun { Man = c.Ped, Horse = horse, Id = c.Who.Id, Name = c.Who.Name, Seed = c.Who.Id + "|" + day + "|watch", Until = now + walkMs, StartedAt = now, Test = test };
                _watchOffers++;
                Log.Write("HORSEWATCH " + c.Who.Name + " comes over to offer to watch your horse (" + (int)metres + " m)" + (test ? " (a test)" : ""));
                return true;
            }
            return false;
        }

        private void WatchRunStep(WatchRun w, Ped me, int now)
        {
            if (w.Man == null || !w.Man.Exists() || w.Man.IsDead || PED.IS_PED_IN_COMBAT(w.Man.Handle, 0) || _director.Involves(w.Man.Handle) || MISC.GET_MISSION_FLAG())
            {
                WatchEnd("something else took him", false);
                return;
            }
            int h = w.Man.Handle;
            switch (w.Phase)
            {
                case "offer":
                    if (w.Man.Position.DistanceTo(me.Position) < 2.6f)
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 6000, 0f, 0f, 0f);
                        Feed.Say(Person.FirstOf(w.Name), HorseWatch.Offer(w.Seed), Speech.When(h, "chat", me.Handle), h, toYou: true);
                        w.Phase = "ask";
                        w.SaidAt = now;
                        if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                            PromptSet(HorseWatch.Kind, new TownEvent { Kind = HorseWatch.Kind, Town = _placeTown, AName = w.Name }, HorseWatch.PromptMs);
                    }
                    else if (now > w.Until || PED.IS_PED_ON_MOUNT(me.Handle))
                        WatchEnd("he never reached you", false);
                    return;
                case "ask":
                    if (!w.Chose && now - w.SaidAt > HorseWatch.PromptMs + 2000 && !(_eventMenu != null && _eventMenu.Mode == HorseWatch.Kind))
                        WatchChoose("horsewatch:no");
                    return;
                case "watch":
                    // a horse thief going for a horse near him: his shout, and the thief runs
                    HorseTheft t = _horseTheft;
                    if (t != null && t.Phase == 0 && t.Thief != null && t.Thief.Exists() && t.Thief.Position.DistanceTo(w.Man.Position) < HorseWatch.ThiefMetres)
                    {
                        SpeechCaption(Person.FirstOf(w.Name), HorseWatch.Shout(w.Seed), h, 3000, false, false);
                        Speech.SayTag(h, "thief", t.Thief.Handle);
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t.Thief.Handle, false);
                        TASK.CLEAR_PED_TASKS(t.Thief.Handle, true, false);
                        TASK.TASK_SMART_FLEE_PED(t.Thief.Handle, h, 80f, -1, 0, 3f, 0);   // seen: he drops it and runs
                        _horseTheft = null;
                        _watchShouts++;
                        Log.Write("HORSEWATCH " + w.Name + " shouts at a horse thief; he runs");
                    }
                    bool back = PED.IS_PED_ON_MOUNT(me.Handle) && PED.GET_MOUNT(me.Handle) == (w.Horse != null ? w.Horse.Handle : 0);
                    if (back || now - w.StartedAt > HorseWatch.WatchMs || w.Man.Position.DistanceTo(me.Position) > 150f || w.Horse == null || !w.Horse.Exists())
                        WatchEnd(back ? "you rode her off again" : "the watch is over", back);
                    return;
            }
        }

        /// J's row.
        private void WatchChoose(string id)
        {
            WatchRun w = _watch;
            if (w == null || w.Phase != "ask" || w.Chose)
                return;
            w.Chose = true;
            if (_eventPrompt == HorseWatch.Kind)
                PromptClear();
            if (id == SendWord.RowId)
            {
                SendWordGo(w);   // a word carried instead of the watch (WestworldScript.SendWord.cs; DEPTH item 10)
                return;
            }
            Ped me = Game.Player.Character;
            int h = w.Man.Handle;
            if (id == "horsewatch:yes" && MONEY._MONEY_GET_CASH_BALANCE() >= HorseWatch.Cents)
            {
                MONEY._MONEY_DECREMENT_CASH_BALANCE(HorseWatch.Cents);
                Feed.Say(Person.FirstOf(w.Name), HorseWatch.Hired(w.Seed), Speech.When(h, "thanks", me.Handle), h, toYou: true);
                if (w.Horse != null && w.Horse.Exists())
                    TASK.TASK_GO_TO_ENTITY(h, w.Horse.Handle, 120000, 1.8f, 1.0f, 0f, 0);   // to her side, and he stays there
                w.Phase = "watch";
                w.StartedAt = Game.GameTime;
                _watchHired++;
                _watchLast = w.Name + " watches your horse for a dime";
                Log.Write("HORSEWATCH " + _watchLast);
                return;
            }
            Feed.Say(Person.FirstOf(w.Name), HorseWatch.Declined(w.Seed), Speech.When(h, "chat", me.Handle), h, toYou: true);
            WatchEnd("no, thank you", false);
        }

        private void WatchEnd(string why, bool back)
        {
            WatchRun w = _watch;
            _watch = null;
            if (_eventPrompt == HorseWatch.Kind)
                PromptClear();
            if (w == null)
                return;
            if (w.Man != null && w.Man.Exists() && !w.Man.IsDead)
            {
                if (back)
                {
                    SpeechCaption(Person.FirstOf(w.Name), HorseWatch.Back(w.Seed), w.Man.Handle, 3500, true, true);
                    PersonState st = !w.Test ? _store.Find(w.Id) : null;
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + 5, -100, 100);   // a stranger who gave him work
                }
                PED.SET_PED_KEEP_TASK(w.Man.Handle, false);
                if (!_director.Involves(w.Man.Handle) && !PED.IS_PED_IN_COMBAT(w.Man.Handle, 0))
                    WanderLater(w.Man.Handle, Game.GameTime + 3000);
            }
            _watchLast = (w.Name ?? "the watcher") + ": " + why;
            Log.Write("HORSEWATCH " + _watchLast);
        }

        private bool WatchWalking(int h)
        {
            WatchRun w = _watch;
            return w != null && w.Man != null && w.Man.Handle == h;
        }

        private string HorseWatchFire(int now, out string said)
        {
            said = null;
            if (!HorseWatch.On)
                return "off ([Scenes] HorseWatch)";
            if (_watch != null)
                return "someone is watching your horse already";
            Ped me = Game.Player.Character;
            int mount = PED._GET_LAST_MOUNT(me.Handle);
            if (mount == 0 || !ENTITY.DOES_ENTITY_EXIST(mount) || PED.IS_PED_ON_MOUNT(me.Handle))
                return "get down off your horse first";
            if (!WatchOffer(me, new Ped(mount), now, true, null))
                return "no man of the mind's within 15 m, free and in view";
            said = _watch.Name + " comes over to offer to watch your horse";
            return null;
        }

        private string HorseWatchStatus()
        {
            if (!HorseWatch.On)
                return "off ([Scenes] HorseWatch)";
            return _watchOffers + " offered to watch your horse, " + _watchHired + " hired, " + _watchShouts + " horse thieves shouted off" + (_watchLast != null ? "; last: " + _watchLast : "");
        }
    }
}
