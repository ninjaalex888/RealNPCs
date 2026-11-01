// A favour asked, the game side (Core/Favours.cs has the choosing and the words; NIGHT4 item 5). Every FavourLookSeconds
// (45, x the storyteller's) in a town, with [Mind] MindLive and [Scenes] FavoursAsked: someone near whom the mind knows, at
// the worst of a worry money can mend (a debt, a tooth, the fare), not sore at him, not wronged by him lately, not mourning
// one he killed, not afraid of him or against him, who hasn't told him of it this run, free, standing, in view within
// 18 m, comes over to a stopped Arthur (the lonely walk's way exactly: a walk timed by the distance, a look, the line at
// 2.6 m, facing him for the wait). On the way their mind chooses: ask outright, hint, a dollar, or lose their nerve
// (with [Mind] LanguagePicks, the model among the same, 2.5 s at most). Once they've said it, Talk on them lists the fix
// ("Let me settle that debt ($11)"); Talk or a greeting within 15 s is being heard out; walking on stings, and they
// leave him be for the rest of that worry. A single walker, never alongside another walk-up; Pacing "favour".
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int FavourLookMs = 45000;
        private const float FavourMetres = 18f;

        private sealed class FavourWalk
        {
            public Ped Ped;
            public string Id, Name, Worry, Who, You, Line, Key;
            public int Until, SaidAt;
            public float Nearest = float.MaxValue;
            public bool Test, Woman;
            public StagedAsk Ask;
        }

        private FavourWalk _favourWalk;
        private int _nextFavour, _favourStepAt, _favourStillSince, _favourLookedAt;
        private int _favourAsked, _favourAsk, _favourHint, _favourHeld, _favourCoin, _favourModel, _favourModelOf, _favourHeard, _favourSettled, _favourWalked;
        private string _favourLast, _favourWhyNot;
        private readonly HashSet<string> _favourRested = new HashSet<string>();   // id|arc: asked (or lost their nerve) this worry; never again this run
        private readonly HashSet<string> _favourAskedIds = new HashSet<string>(); // ids who asked: a settle on Talk is counted for this feature
        private Action _aFavour;

        /// Tick04: the one coming over or waiting on an answer, and now and then a look for someone in trouble.
        private void FavourStep(int now)
        {
            if (!_cfg.FavoursAsked || !_cfg.MindLive)
            {
                if (_favourWalk != null)
                    FavourLetGo(_favourWalk.Ped != null && _favourWalk.Ped.Exists() ? _favourWalk.Ped.Handle : 0);
                return;
            }
            if (_store == null || (_favourWalk == null && now < _nextFavour))
                return;
            if (_aFavour == null)
                _aFavour = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    if (_favourWalk != null)
                    {
                        if (_now < _favourStepAt)
                            return;
                        _favourStepAt = _now + 250;
                        FavourWalkStep(me, _now);
                    }
                    else if (_now >= _nextFavour)
                        FavourLook(me, _now, false);
                };
            Safe("favours asked", _aFavour);
        }

        /// Whether this is the one asking a favour, coming over or waiting (Talk may open on them).
        private bool FavourWaiting(int h)
        {
            return _favourWalk != null && _favourWalk.Ped != null && _favourWalk.Ped.Handle == h;
        }

        /// Someone at the worst of a worry money can mend comes over. Null if it started, else why not.
        private string FavourLook(Ped me, int now, bool test)
        {
            _nextFavour = now + (int)(FavourLookMs * Story.WalkLook);   // (x the storyteller's)
            if (WalkUpGoing())
                return _favourWhyNot = "someone is already coming up to you, or you're in the Talk menu";
            if (!test && (_placeTown == null || _director.Busy))
                return _favourWhyNot = _placeTown == null ? "out of the towns" : "a scene is going on";
            if (MISC.GET_MISSION_FLAG() || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return _favourWhyNot = "a mission, or you're wanted, fighting or aiming";
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                return _favourWhyNot = "you're riding";
            bool still = ENTITY.GET_ENTITY_SPEED(me.Handle) <= 0.4f;
            if (!still)
                _favourStillSince = 0;
            else if (_favourStillSince == 0 || now - _favourLookedAt > 1500)
                _favourStillSince = now;
            _favourLookedAt = now;
            if (!test && (_favourStillSince == 0 || now - _favourStillSince < 2000))
            {
                _nextFavour = now + 1000;
                return _favourWhyNot = "you're on the move (they come to a man who has stopped)";
            }
            long g = _store.LastGameMin, day = g / 1440;
            string skipped;
            string pre = Pacing.WhyNot(_store.Pace, "favour", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pre != null)
                return _favourWhyNot = "rests: " + pre;
            Vector3 mePos = me.Position;
            int worriers = 0;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null)
                    continue;
                // the pure tests first: the worry at its worst and mendable, before any native
                string worry;
                int stage;
                bool good;
                bool has = InnerLife.WorryNow(w.P, day, out worry, out stage, out good);
                if (!test && (!has || !Favours.Asks(worry, stage)))
                    continue;
                if (test && (!has || !Favours.Asks(worry, stage)))
                    worry = "debt";   // a test: a debt stands in (Talk lists the fix only for a real one)
                long arc = InnerLife.ArcFor(w.Id, day);
                string key = w.Id + "|" + arc + "|" + worry;   // TalkToldKey's shape: Talk lists the fix from this
                worriers++;
                if (!test && (_talkConfided.Contains(key) || _favourRested.Contains(w.Id + "|" + arc)))
                    continue;   // told him already, or asked (or lost their nerve) this worry
                if (!test && (Lonely.Sore(_engine.Shadow.Felt(w.Id), g) || Interact.LastWrong(_store.WorldLog, w.Id, g) != null || _engine.MournsYourKill(w.Id, 7)))
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                bool woman = !PED.IS_PED_MALE(h);
                if (woman && !test && (Natives.Night() || _engine.NowIndoors))
                    continue;   // a woman doesn't walk up to a man after dark or in a saloon
                Ped p = new Ped(h);
                float metres = p.Position.DistanceTo(mePos);
                if (metres > FavourMetres || !NodInView(me, p) || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || PED.IS_PED_USING_ANY_SCENARIO(h))
                    continue;
                string temper = w.Src != null ? w.Src.Temper : null, regard = SightRegard(p, w.Id);
                bool knows = _engine.KnowsYourName(w.Id), kind = Interact.KindLately(_store.WorldLog, w.Id, g);
                float need = w.Needs != null ? w.Needs[NeedState.Money] : 0f;
                if (test ? regard == "afraid" || regard == "hostile" : !Favours.Comes(temper, regard, knows, kind, Favours.NeedOf(need, worry, stage)))
                    continue;
                if (Pacing.WhyNot(_store.Pace, "favour", _placeTown, w.Id, g, 0, test, out skipped) != null)
                    continue;
                if (!test)
                {
                    Pacing.Note(_store.Pace, "favour", _placeTown, w.Id, g);
                    StoryNoticed("you:favour", 2, metres);   // the storyteller (WestworldScript.Story.cs)
                }
                // the mind's choice (and the model's, settled on the walk over)
                Someone who = SomeoneOf(p);
                StagedMind sm = who != null ? StagedMindOf(who, null) : new StagedMind { Temper = temper, Woman = woman };
                bool poor = !WellOff(p) && w.TradeGroup != "gentry";
                Staged.Weights wt = Favours.Weights(sm, regard, need, poor);
                string you = knows ? YourSurname() : null;
                StagedAsk ask = StagedChoose(h, "favour", wt, w.Id + "|" + arc + "|favour", id => Favours.Words(id, worry), Favours.Now(you, _placeTown, worry), null, now);
                if (ask.Asked)
                    _favourModelOf++;
                string owed = worry == "debt" && InnerLife.Counterpart != null ? InnerLife.Counterpart(w.P, "debt", arc) : null;
                int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));
                TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs + Favours.WaitMs, 0, 51, 0);
                _favourWalk = new FavourWalk { Ped = p, Id = w.Id, Name = w.Name, Worry = worry, Who = owed, You = you, Key = key, Until = now + walkMs, Test = test, Woman = woman, Ask = ask };
                if (!test)
                {
                    if (_favourRested.Count > 300)
                        _favourRested.Clear();
                    _favourRested.Add(w.Id + "|" + arc);
                }
                _favourAsked++;
                Log.Write("FAVOUR " + w.Name + " (" + (temper ?? "plain") + ", " + worry + " stage " + stage + ", need " + need.ToString("0.00") + (knows ? ", knows your name" : "") + (kind ? ", a kindness from you lately" : "") +
                          ") comes over (" + (int)metres + " m)" + (test ? ", a test" + (has && Favours.Asks(worry, stage) ? "" : " (no worry at its worst: a debt stands in)") + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
                return null;
            }
            return _favourWhyNot = worriers == 0 ? "nobody near at the worst of a debt, a tooth or a fare" : "nobody in trouble near who'd come to you, free and in view (" + worriers + " in trouble)";
        }

        /// The walk, the choice settled, the words, and the wait.
        private void FavourWalkStep(Ped me, int now)
        {
            FavourWalk w = _favourWalk;
            if (w == null)
                return;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0))
            {
                _favourWalk = null;
                return;
            }
            int h = w.Ped.Handle;
            StagedSettle(w.Ask, "favour", now);   // the model's answer, if it came (else the mind's own stands)
            if (_menu != null && _menu.Handle == h)
            {
                if (w.SaidAt == 0 && w.Ask.Picked != "holdback")
                {
                    FavourTold(w);   // Talk before they'd said it: they tell it now (the menu lists the fix)
                    TalkRefreshOptions();
                }
                FavourHeard(w, h, now, "you stopped to talk");
                return;
            }
            if (w.SaidAt == 0)
            {
                float d = w.Ped.Position.DistanceTo(me.Position);
                if (d < w.Nearest)
                    w.Nearest = d;
                if (d > w.Nearest + 3f || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                {
                    FavourLetGo(h);
                    Log.Write("FAVOUR " + w.Name + ": you went on before they reached you; they let it be");
                    return;
                }
                if (_menu != null || _grudgeWalk != null || _thanksWalk != null || _kinWalk != null || _lifter != null || _lonelyWalk != null || _warnWalk != null)
                {
                    FavourLetGo(h);
                    Log.Write("FAVOUR " + w.Name + " saw you busy and went on their way");
                    return;
                }
                if (d < 2.6f)
                {
                    if (!w.Ask.Done)
                        return;   // the model's answer still due (StagedSettle gives it up at 2.5 s from the ask)
                    string pick = w.Ask.Picked ?? "holdback";
                    Log.Write("FAVOUR " + w.Name + " chose " + pick + " (" + By(w.Ask) + ")");
                    if (w.Ask.ByModel)
                        _favourModel++;
                    w.Line = Favours.Line(pick, w.Worry, w.Who, w.You, w.Woman, w.Id + "|" + now / 60000);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, Favours.WaitMs, 0f, 0f, 0f);
                    Feed.Say(w.Name, w.Line, Speech.For(h, w.Line, me.Handle, 0, 0, true, "chat"), h, toYou: true);
                    w.SaidAt = now;
                    Log.Write("FAVOUR " + w.Name + " to your face: " + w.Line);
                    if (pick == "ask") _favourAsk++;
                    else if (pick == "hint") _favourHint++;
                    else if (pick == "coin") _favourCoin++;
                    else _favourHeld++;
                    if (pick == "holdback")
                    {
                        string t = Favours.After("holdback", w.Id + "|" + now / 60000);
                        _ticker.Push(w.Name + ", thinking", t, "near", h);
                        _favourLast = w.Name + ": lost their nerve" + (w.Test ? " (a test)" : "");
                        Log.Write("FAVOUR " + w.Name + ": held back; thinks: " + t);
                        FavourLetGo(h);
                        return;
                    }
                    FavourTold(w);
                }
                else if (now > w.Until)
                {
                    FavourLetGo(h);
                    Log.Write("FAVOUR " + w.Name + " never reached you: went on their way");
                }
                return;
            }
            if (me.Position.DistanceTo(w.Ped.Position) > 6f || now - w.SaidAt > Favours.WaitMs)
            {
                _favourWalked++;
                string hurt = Favours.After("walked", w.Id + "|" + now / 60000);
                if (NodInView(me, w.Ped) || me.Position.DistanceTo(w.Ped.Position) < 15f)
                    _ticker.Push(w.Name + ", thinking", hurt, "near", h);
                _favourLast = w.Name + ": you walked on" + (w.Test ? " (a test)" : "");
                Log.Write("FAVOUR " + w.Name + ": you walked on; thinks: " + hurt);
                FavourLetGo(h);
            }
        }

        /// They've told him: from now Talk on them lists the fix (the same key as TalkToldKey).
        private void FavourTold(FavourWalk w)
        {
            if (w.Key == null)
                return;
            if (_talkConfided.Count > 500)
                _talkConfided.Clear();
            _talkConfided.Add(w.Key);
            if (_favourAskedIds.Count > 300)
                _favourAskedIds.Clear();
            _favourAskedIds.Add(w.Id);
        }

        private void FavourHeard(FavourWalk w, int h, int now, string how)
        {
            _favourHeard++;
            string glad = Favours.After("heard", w.Id + "|" + now / 60000);
            _ticker.Push(w.Name + ", thinking", glad, "near", h);
            _favourLast = w.Name + ": heard out, " + how + (w.Test ? " (a test)" : "");
            Log.Write("FAVOUR " + w.Name + ": " + how + "; thinks: " + glad);
            _favourWalk = null;   // the menu or the greeting has them now
        }

        /// The game's Greet to the one coming over or waiting: being heard out (from GreetedSpecial).
        private void FavourGreeted(int handle)
        {
            FavourWalk w = _favourWalk;
            if (w != null && w.Ped != null && w.Ped.Handle == handle && w.SaidAt != 0)
                FavourHeard(w, handle, _now, "you greeted them");
        }

        /// From Talk's settle: counted when the one settled for had asked him (the late thanks pick it up by themselves).
        private void FavourSettledFor(string id, string worry, int cents)
        {
            if (id == null || !_favourAskedIds.Contains(id))
                return;
            _favourSettled++;
            _favourAskedIds.Remove(id);
            Log.Write("FAVOUR " + id + ": you settled it ($" + cents / 100 + ", " + (worry ?? "their trouble") + ")");
        }

        private void FavourLetGo(int h)
        {
            if (h != 0 && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            }
            _favourWalk = null;
        }

        /// The Testing row: anyone the mind knows within 18 m who isn't afraid of you comes now (no roll, pace or rest; a
        /// debt stands in when they have no worry at its worst).
        private string FavourTest(int now)
        {
            if (!_cfg.MindLive)
                return "[Mind] MindLive is off (the worries and needs come from the mind)";
            if (_favourWalk != null)
                return "someone is already coming over";
            return FavourLook(Game.Player.Character, now, true);
        }

        /// F7: asked, by choice, the model's share, heard out, settled, walked on, the last, and why not lately.
        private string FavourStatus()
        {
            if (!_cfg.FavoursAsked)
                return "off ([Scenes] FavoursAsked)";
            return (_cfg.MindLive ? "on" : "on, but [Mind] MindLive is off") + "; asked " + _favourAsked + " (ask " + _favourAsk + ", hint " + _favourHint + ", held back " + _favourHeld + ", a coin " + _favourCoin +
                   "; the model's " + _favourModel + " of " + _favourModelOf + "), heard out " + _favourHeard + ", settled " + _favourSettled + ", walked on " + _favourWalked +
                   (_favourWalk != null ? "; " + _favourWalk.Name + " is " + (_favourWalk.SaidAt == 0 ? "coming over" : "waiting on an answer") : "") +
                   "; last: " + (_favourLast ?? "none") + (_favourWhyNot != null ? "; the last look: " + _favourWhyNot : "");
        }
    }
}
