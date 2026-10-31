// Late, specific thanks, the game side (Core/Thanks.cs has the choosing and the words; NIGHT3). Every 30 s in a town,
// only when someone may be owed thanks (a kindness through the interaction menu in the world log, six game hours to a
// few days ago, or a debt settled in the save's record): anyone of them within 20 m who can see him, free and not in a
// scene, walks over (the grudge walk's way: no clearing first, a walk timed by the distance) and thanks him for it to his
// face (a subtitle, the game's own thanks voice), then goes on their way; the proud who took his eleven dollars as a loan
// pay it back. The cold, anyone sitting, and a woman after dark or indoors give a nod and think it. Once for each
// kindness, the biggest thanked for and the rest covered (marks on them); Pacing "thanks" keeps it rare. Never while
// he's wanted, fighting, aiming, in the Talk menu, or someone else is walking up to him. [Scenes] LateThanks. Testing row
// "Someone you helped thanks you" (no waits, no roll, no pace, no mark, and no money paid).
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
        private const int ThanksLookMs = 30000;

        private sealed class ThanksWalk
        {
            public Ped Ped;
            public string Id, Name, Line;
            public List<string> Keys;
            public int Cents, Until, SaidAt;
            public bool Test;
        }

        private ThanksWalk _thanksWalk;
        private int _nextThanks, _thanksSaid, _thanksNods, _thanksRepaid;
        private string _thanksLast, _thanksWhyNot;
        private Action _aThanks;

        /// Tick04: the one walking over to thank him, and now and then a look for someone who owes him thanks.
        private void ThanksStep(int now)
        {
            if (!_cfg.LateThanks)
            {
                if (_thanksWalk != null)
                    ThanksAbort();   // switched off mid-walk: let them go (else they'd stay busy for everything)
                return;
            }
            if (_store == null || (_thanksWalk == null && now < _nextThanks))
                return;
            if (_aThanks == null)
                _aThanks = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    ThanksWalkStep(me, _now);
                    if (_thanksWalk == null && _now >= _nextThanks)
                        ThanksLook(me, _now, false);
                };
            Safe("late thanks", _aThanks);
        }

        /// Someone near who owes him thanks comes over (or nods). Null if it started, else why not.
        private string ThanksLook(Ped me, int now, bool test)
        {
            _nextThanks = now + ThanksLookMs;
            if (_thanksWalk != null || _grudgeWalk != null || _kinWalk != null || _lifter != null || _liftRun != null || _menu != null)
                return _thanksWhyNot = "someone is already coming up to you, or you're in the Talk menu";
            if (!test && (_placeTown == null || _director.Busy))
                return _thanksWhyNot = _placeTown == null ? "out of the towns" : "a scene is going on";
            if (MISC.GET_MISSION_FLAG() || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return _thanksWhyNot = "a mission, or you're wanted, fighting or aiming";
            long g = _store.LastGameMin;
            if (!Thanks.AnyDue(_store.WorldLog, g, test, _store.WorrySettled != null ? _store.WorrySettled.Values : null))
                return _thanksWhyNot = "nobody you've done a kindness to (a drink, money, a debt) " + (test ? "this week" : "six game hours to a week ago");
            string skipped;
            string pre = Pacing.WhyNot(_store.Pace, "thanks", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pre != null)
                return _thanksWhyNot = "rests: " + pre;
            bool late = Natives.Night() || _engine.NowIndoors;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 20f)
                                                .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                string id, name, temper;
                if (!LastingId(p, out id, out name, out temper) || id == null || !_engine.CanRemember(id))
                    continue;
                long settled;
                if (_store.WorrySettled == null || !_store.WorrySettled.TryGetValue(id, out settled))
                    settled = -1;
                ThanksOwed owed = Thanks.Due(_store.WorldLog, id, g, k => !test && _engine.Thanked(id, k), test, settled, at => _engine.Thanked(id, Thanks.LoanKey(at)));
                if (owed == null)
                    continue;
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || !NodInView(me, p))
                    continue;
                bool woman = !PED.IS_PED_MALE(h);
                string way = Thanks.Way(temper, SightRegard(p, id), woman, late, PED.IS_PED_USING_ANY_SCENARIO(h));
                if (way == null)
                    continue;
                string pace = Pacing.WhyNot(_store.Pace, "thanks", _placeTown, id, g, 0, test, out skipped);
                if (pace != null)
                    continue;
                if (!test)
                    Pacing.Note(_store.Pace, "thanks", _placeTown, id, g);   // a test leaves the real one its turn
                string who = name ?? TalkName(p), seed = id + "|" + owed.Key;
                float metres = p.Position.DistanceTo(me.Position);
                if (way == "nod")
                {
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 3500, 0, 51, 0);
                    string thought = Thanks.Nod(owed.Kind, seed);
                    _ticker.Push(who + ", thinking", thought, "near", h);
                    if (!test && !owed.Loan)   // a loan's nod is no repayment: it stays owed
                        foreach (string k in owed.Keys)
                            _engine.MarkThanked(id, k, who);
                    _thanksNods++;
                    _thanksLast = who + " nodded to you over the " + owed.Kind + (test ? " (a test)" : "");
                    Log.Write("THANKS " + who + " (" + (temper ?? "plain") + ") nods to you over the " + owed.Kind + " " + (g - owed.At) / 60 + " game h ago; thinks: " + thought + (test ? ", a test" : ""));
                    return null;
                }
                int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));   // a walk timed by the distance, not 15 s for 20 m
                int repay = test ? (owed.Loan ? Interact.SettleCents : 0) : Thanks.Repays(owed, g);
                TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);   // no clearing first (the grudge walk's way): a cleared ped can stand a moment before it walks
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs, 0, 51, 0);
                _thanksWalk = new ThanksWalk
                {
                    Ped = p, Id = id, Name = who, Line = Thanks.Line(owed.Kind, temper, woman, seed, g / 1440 == owed.At / 1440, repay), Keys = owed.Keys,
                    Cents = test ? 0 : repay, Until = now + walkMs, Test = test
                };
                Log.Write("THANKS " + who + (temper != null ? " (" + temper + ")" : "") + " comes over with thanks for the " + owed.Kind + " " + (g - owed.At) / 60 + " game h ago (" + (int)metres + " m" +
                          (repay > 0 ? (test ? "; would pay back $" : "; pays back $") + (repay / 100.0).ToString("0.00") : "") + ")" + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
                return null;
            }
            return _thanksWhyNot = "nobody who owes you thanks is near, free and can see you";
        }

        /// The one walking over: says it to his face once there (and pays back what's owed), then goes on their way.
        private void ThanksWalkStep(Ped me, int now)
        {
            ThanksWalk w = _thanksWalk;
            if (w == null)
                return;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0))
            {
                _thanksWalk = null;   // someone else has them now (enRAGE, a fright, a scene): our orders stop here
                return;
            }
            int h = w.Ped.Handle;
            if (w.SaidAt == 0)
            {
                if (_menu != null || _grudgeWalk != null)
                {
                    TASK.TASK_CLEAR_LOOK_AT(h);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // he's taken up with someone else: another time (nothing marked)
                    _thanksWalk = null;
                    Log.Write("THANKS " + w.Name + " saw you busy and went on their way");
                    return;
                }
                if (w.Ped.Position.DistanceTo(me.Position) < 2.6f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 4500, 0f, 0f, 0f);
                    Feed.Say(w.Name, w.Line, Speech.For(h, w.Line, me.Handle, 0, 0, true, "thanks"), h, toYou: true);
                    if (w.Cents > 0)
                    {
                        MONEY._MONEY_INCREMENT_CASH_BALANCE(w.Cents, 0);
                        _thanksRepaid += w.Cents;
                        Hint(w.Name + " paid you back $" + (w.Cents / 100.0).ToString("0.00") + ".", 3000);
                    }
                    if (!w.Test)
                        foreach (string k in w.Keys)
                            _engine.MarkThanked(w.Id, k, w.Name);   // thanked for this once, and the rest it covers
                    w.SaidAt = now;
                    _thanksSaid++;
                    _thanksLast = w.Name + ": " + w.Line + (w.Test ? " (a test)" : "");
                    Log.Write("THANKS " + w.Name + " to your face: " + w.Line + (w.Cents > 0 ? " (paid you back $" + (w.Cents / 100.0).ToString("0.00") + ")" : ""));
                }
                else if (now > w.Until)
                {
                    TASK.TASK_CLEAR_LOOK_AT(h);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    _thanksWalk = null;
                    Log.Write("THANKS " + w.Name + " never reached you: went on their way");
                }
                return;
            }
            if (now - w.SaidAt > 5000)
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // said it; goes on their way
                _thanksWalk = null;
            }
        }

        /// The Testing row: anyone within 20 m he's done a kindness to this week, at once (the waits, the roll, the pace and
        /// the mark skipped; a loan's repayment is said, not paid).
        private string ThanksTest(int now)
        {
            if (_thanksWalk != null)
                return "someone is already walking over to thank you";
            return ThanksLook(Game.Player.Character, now, true);
        }

        private void ThanksAbort()
        {
            if (_thanksWalk != null && _thanksWalk.Ped != null && _thanksWalk.Ped.Exists())
                TASK.CLEAR_PED_TASKS(_thanksWalk.Ped.Handle, true, false);
            _thanksWalk = null;
        }

        /// F7: on or off, this session's thanks, nods and money paid back, the last, and why not lately.
        private string ThanksStatus()
        {
            if (!_cfg.LateThanks)
                return "off ([Scenes] LateThanks)";
            return _thanksSaid + " thanked you, " + _thanksNods + " nodded, $" + (_thanksRepaid / 100.0).ToString("0.00") + " paid back this session" + (_thanksWalk != null ? "; " + _thanksWalk.Name + " is coming over" : "") +
                   (_thanksLast != null ? "; last: " + _thanksLast : "") + (_thanksWhyNot != null ? "; the last look: " + _thanksWhyNot : "");
        }
    }
}
