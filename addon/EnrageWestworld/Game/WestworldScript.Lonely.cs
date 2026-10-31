// Lonely regulars, the game side (Core/Lonely.cs has the choosing and the words; NIGHT3). Every 45 s in a town, with
// [Mind] MindLive (the needs come from the mind): someone near whom the mind knows, lonely and knowing Arthur (his name,
// or a kindness from him lately), free, not sitting, in view, within 18 m, walks over (the thanks walk's way: no
// clearing first, a walk timed by the distance), says they hoped to see him, and waits up to 15 s facing him. If he opens
// Talk with them (the prompt stays theirs while they wait) they're glad; if he walks off or the time runs out, it
// stings, and the thought says so; they go on their way. A single walker, never alongside another walk-up; Pacing "lonely".
// [Scenes] LonelyRegulars. Testing row "A lonely regular comes over" (anyone the mind knows near, lonely or not).
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
        private const int LonelyLookMs = 45000;

        private sealed class LonelyWalk
        {
            public Ped Ped;
            public string Id, Name, Line;
            public int Until, SaidAt;
            public bool Test;
        }

        private LonelyWalk _lonelyWalk;
        private int _nextLonely, _lonelyCame, _lonelyGlad, _lonelyStung;
        private string _lonelyLast, _lonelyWhyNot;
        private Action _aLonely;

        /// Tick04: the one walking over or waiting for a word, and now and then a look for someone lonely who knows him.
        private void LonelyStep(int now)
        {
            if (!_cfg.LonelyRegulars || !_cfg.MindLive)
            {
                if (_lonelyWalk != null)
                    LonelyAbort();   // switched off mid-walk: let them go
                return;
            }
            if (_store == null || (_lonelyWalk == null && now < _nextLonely))
                return;
            if (_aLonely == null)
                _aLonely = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    LonelyWalkStep(me, _now);
                    if (_lonelyWalk == null && _now >= _nextLonely)
                        LonelyLook(me, _now, false);
                };
            Safe("lonely regulars", _aLonely);
        }

        /// Whether this is the lonely one waiting on a word from him (Talk may still open on them).
        private bool LonelyWaiting(int h)
        {
            return _lonelyWalk != null && _lonelyWalk.Ped != null && _lonelyWalk.Ped.Handle == h;
        }

        /// Someone lonely near who knows him comes over. Null if it started, else why not.
        private string LonelyLook(Ped me, int now, bool test)
        {
            _nextLonely = now + LonelyLookMs;
            if (_lonelyWalk != null || _thanksWalk != null || _grudgeWalk != null || _kinWalk != null || _lifter != null || _liftRun != null || _menu != null)
                return _lonelyWhyNot = "someone is already coming up to you, or you're in the Talk menu";
            if (!test && (_placeTown == null || _director.Busy))
                return _lonelyWhyNot = _placeTown == null ? "out of the towns" : "a scene is going on";
            if (MISC.GET_MISSION_FLAG() || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return _lonelyWhyNot = "a mission, or you're wanted, fighting or aiming";
            long g = _store.LastGameMin;
            string skipped;
            string pre = Pacing.WhyNot(_store.Pace, "lonely", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pre != null)
                return _lonelyWhyNot = "rests: " + pre;
            Vector3 mePos = me.Position;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || (!test && w.Needs == null) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(mePos) > 18f || !NodInView(me, p) || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || PED.IS_PED_USING_ANY_SCENARIO(h))
                    continue;
                bool knows = _engine.KnowsYourName(w.Id), kind = Interact.KindLately(_store.WorldLog, w.Id, g);
                string temper = w.Src != null ? w.Src.Temper : null, regard = SightRegard(p, w.Id);
                float company = w.Needs != null ? w.Needs[NeedState.Company] : 0f;
                if (test ? regard == "afraid" || regard == "hostile" : !Lonely.Comes(company, knows, kind, regard, temper))
                    continue;
                string pace = Pacing.WhyNot(_store.Pace, "lonely", _placeTown, w.Id, g, 0, test, out skipped);
                if (pace != null)
                    continue;
                if (!test)
                    Pacing.Note(_store.Pace, "lonely", _placeTown, w.Id, g);
                float metres = p.Position.DistanceTo(mePos);
                int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));
                TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs + Lonely.WaitMs, 0, 51, 0);
                _lonelyWalk = new LonelyWalk
                {
                    Ped = p, Id = w.Id, Name = w.Name, Line = Lonely.Hello(temper, !PED.IS_PED_MALE(h), knows ? YourSurname() : null, w.Id + "|" + g / 60), Until = now + walkMs, Test = test
                };
                _lonelyCame++;
                Log.Write("LONELY " + w.Name + " (" + (temper ?? "plain") + ", company " + company.ToString("0.00") + (knows ? ", knows your name" : "") + (kind ? ", a kindness from you lately" : "") +
                          ") comes over hoping for a word (" + (int)metres + " m)" + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
                return null;
            }
            return _lonelyWhyNot = "nobody lonely who knows you is near, free and can see you";
        }

        /// The walk, the word, and the wait: Talk opened on them is a gladness, his walking off a sting.
        private void LonelyWalkStep(Ped me, int now)
        {
            LonelyWalk w = _lonelyWalk;
            if (w == null)
                return;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0))
            {
                _lonelyWalk = null;
                return;
            }
            int h = w.Ped.Handle;
            if (w.SaidAt == 0)
            {
                if (_menu != null || _grudgeWalk != null || _thanksWalk != null)
                {
                    LonelyLetGo(h);   // he's taken up with someone else: another time
                    Log.Write("LONELY " + w.Name + " saw you busy and went on their way");
                    return;
                }
                if (w.Ped.Position.DistanceTo(me.Position) < 2.6f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, Lonely.WaitMs, 0f, 0f, 0f);   // facing him, hoping
                    Feed.Say(w.Name, w.Line, Speech.For(h, w.Line, me.Handle, 0, 0, true, "chat"), h, toYou: true);
                    w.SaidAt = now;
                    Log.Write("LONELY " + w.Name + " to your face: " + w.Line);
                }
                else if (now > w.Until)
                {
                    LonelyLetGo(h);
                    Log.Write("LONELY " + w.Name + " never reached you: went on their way");
                }
                return;
            }
            if (_menu != null && _menu.Handle == h)
            {
                _lonelyGlad++;
                string glad = Lonely.After(true, w.Id + "|" + now / 60000);
                _ticker.Push(w.Name + ", thinking", glad, "near", h);
                _lonelyLast = w.Name + ": glad you stopped" + (w.Test ? " (a test)" : "");
                Log.Write("LONELY " + w.Name + ": you stopped to talk; thinks: " + glad);
                _lonelyWalk = null;   // the menu has them now
                return;
            }
            if (me.Position.DistanceTo(w.Ped.Position) > 6f || now - w.SaidAt > Lonely.WaitMs)
            {
                _lonelyStung++;
                string hurt = Lonely.After(false, w.Id + "|" + now / 60000);
                if (NodInView(me, w.Ped) || me.Position.DistanceTo(w.Ped.Position) < 15f)
                    _ticker.Push(w.Name + ", thinking", hurt, "near", h);
                _lonelyLast = w.Name + ": you walked on" + (w.Test ? " (a test)" : "");
                Log.Write("LONELY " + w.Name + ": you didn't stop; thinks: " + hurt);
                LonelyLetGo(h);
            }
        }

        private void LonelyLetGo(int h)
        {
            if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            }
            _lonelyWalk = null;
        }

        /// The Testing row: anyone the mind knows within 18 m comes over now, lonely or not (the wait, the pace skipped).
        private string LonelyTest(int now)
        {
            if (!_cfg.MindLive)
                return "[Mind] MindLive is off (the needs come from the mind)";
            if (_lonelyWalk != null)
                return "someone is already coming over";
            return LonelyLook(Game.Player.Character, now, true);
        }

        private void LonelyAbort()
        {
            if (_lonelyWalk != null && _lonelyWalk.Ped != null && _lonelyWalk.Ped.Exists())
                TASK.CLEAR_PED_TASKS(_lonelyWalk.Ped.Handle, true, false);
            _lonelyWalk = null;
        }

        /// F7: on or off, this session's lonely ones, glad and stung, the last, and why not lately.
        private string LonelyStatus()
        {
            if (!_cfg.LonelyRegulars)
                return "off ([Scenes] LonelyRegulars)";
            return (_cfg.MindLive ? "on" : "on, but [Mind] MindLive is off") + "; " + _lonelyCame + " came over this session, " + _lonelyGlad + " glad you stopped, " + _lonelyStung + " stung you didn't" +
                   (_lonelyWalk != null ? "; " + _lonelyWalk.Name + " is " + (_lonelyWalk.SaidAt == 0 ? "coming over" : "waiting on a word") : "") +
                   (_lonelyLast != null ? "; last: " + _lonelyLast : "") + (_lonelyWhyNot != null ? "; the last look: " + _lonelyWhyNot : "");
        }
    }
}
