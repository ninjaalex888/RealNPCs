// The kin who take up a wrong ([Scenes] KinGrudges; the words and the bookkeeping are Core/KinGrudges.cs and
// Core/Engine.KinGrudges.cs; SIXDAY item 14; roadmap 20). Every fifteen seconds in a town, with no one else walking up to
// you: the engine's grudge due here (the one wronged's town, or the kin's own) gets a body: a free unseen man 30-120 m off
// becomes the kin, a resident of his town from then on (one body with the name: his own if he already has one near), and
// walks up to you by the grudges' own walk (GrudgeWalk in WestworldScript.Grudges.cs): at your side he says it to your
// face in the game's angry or threatening voice, with the line subtitled, and turns away; the town saw someone face you
// (a happening, "scene:kin"); a notice names him. Nothing violent from our side, as the grudge rule has it. F7 "TEST kin:".
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private const int KinGrudgeLookMs = 15000, KinGrudgeWalkMs = 45000;
        private int _nextKinGrudge, _kinGrudgeCasts, _kinGrudgeSaid;
        private KinGrudge _kinGrudgeWalk;      // the one walking up to you now (the walk itself is the grudges' GrudgeWalk)
        private string _kinGrudgeLast;
        private Action _aKinGrudge;

        /// Tick04: a look every fifteen seconds for the kin due here; the grudges' walk stepped here too when [Scenes] Grudges is off.
        private void KinGrudgesStep(int now)
        {
            if (_cfg == null || !_cfg.KinGrudges || _store == null || _engine == null)
                return;
            if (!_cfg.Grudges && _grudgeWalk != null && _kinGrudgeWalk != null)
            {
                Ped me0 = Game.Player.Character;
                if (me0 != null && me0.Exists() && !me0.IsDead)
                    GrudgeWalkStep(me0, now);
            }
            if (now < _nextKinGrudge)
                return;
            _nextKinGrudge = now + KinGrudgeLookMs;
            if (_aKinGrudge == null)
                _aKinGrudge = () => KinGrudgeLook(_now);
            Safe("kin", _aKinGrudge);
        }

        private void KinGrudgeLook(int now)
        {
            if (_placeTown == null || _grudgeWalk != null || WalkUpGoing() || _director.Busy || MISC.GET_MISSION_FLAG())
                return;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return;
            long g = Bridge.Now();
            KinGrudge k = _engine.KinDue(_placeTown, g);
            if (k == null)
                return;
            KinGrudgeCast(k, me, now, g);
        }

        /// The kin's body: his own if he already walks this town (cast before and still about), else a free unseen man 30-120 m off,
        /// made a resident of his town and bound to the name; then the walk up.
        private void KinGrudgeCast(KinGrudge k, Ped me, int now, long g)
        {
            Ped body = null;
            Resident res = k.CastId != null ? _store.FindResident(k.CastId) : null;
            if (res != null)
            {
                foreach (KeyValuePair<int, ResBind> kv in _resBind)
                    if (kv.Value.R == res && ENTITY.DOES_ENTITY_EXIST(kv.Key) && !ENTITY.IS_ENTITY_DEAD(kv.Key))
                    {
                        Ped own = new Ped(kv.Key);
                        if (own.Position.DistanceTo(me.Position) < 150f && !_director.Involves(kv.Key) && MayDirectWhyNot(own) == null)
                            body = own;
                        break;
                    }
            }
            bool fresh = false;
            if (body == null)
            {
                float bestD = 120f;
                foreach (Ped p in Peds.All())
                {
                    int h = p.Handle;
                    if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || !IsGrownUp(p) || !PED.IS_PED_MALE(h))
                        continue;
                    float d = p.Position.DistanceTo(me.Position);
                    if (d < 30f || d >= bestD || ENTITY.IS_ENTITY_ON_SCREEN(h) || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17))
                        continue;
                    if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || ResidentWhy(p) != null || PostKindOf(p) != null || Natives.StoryModel(p) ||
                        _resBind.ContainsKey(h) || _director.Involves(h) || !MayDirect(p) || (_bridge != null && _bridge.Looked(h)))
                        continue;
                    body = p;
                    bestD = d;
                }
                if (body == null)
                    return;   // nobody free and unseen just now: again in fifteen seconds
                int bh = body.Handle;
                uint model = ENTITY.GET_ENTITY_MODEL(bh);
                if (res == null)
                {
                    res = _engine.MeetResident(model, false, k.Town ?? _placeTown, "come about " + Person.FirstOf(k.VictimName ?? "kin"), bh + "|kin|" + Game.GameTime, g, false);
                    res.Name = k.Name;
                    _engine.ForgetPerson(res.Id);
                    k.CastId = res.Id;   // one body with the name, this time and the next
                }
                _engine.KeepResident(res, g);
                _resBind[bh] = new ResBind { R = res, Model = model };
                fresh = true;
            }
            int handle = body.Handle;
            // the kin had the surname from the one wronged, if they knew it
            Person victim = _engine.KinPerson(k.VictimId);
            PersonState vst = victim != null ? _store.Get(victim) : null;
            string you = vst != null && vst.KnowsName ? _engine.Player.Surname : null;
            string line = KinGrudges.Opening(k, you, k.Id);
            PED.SET_PED_KEEP_TASK(handle, true);
            TASK.CLEAR_PED_TASKS(handle, true, false);
            TASK.TASK_GO_TO_ENTITY(handle, me.Handle, KinGrudgeWalkMs, 2.0f, 1.6f, 0f, 0);
            TASK.TASK_LOOK_AT_ENTITY(handle, me.Handle, KinGrudgeWalkMs, 0, 51, 0);
            _grudgeWalk = new GrudgeWalk { Ped = body, Name = k.Name, Line = line, Voice = KinGrudges.VoiceFor(k.Temper), Until = now + KinGrudgeWalkMs, Id = null, DeadId = null };
            _kinGrudgeWalk = k;
            _kinGrudgeCasts++;
            _kinGrudgeLast = k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + ") in " + _placeTown;
            Log.Write("KIN GRUDGE: " + k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + ", " + (k.Temper ?? "plain") + ") comes up to you in " + _placeTown + " on " + Person.FirstOf(k.VictimName ?? "?") + "'s account (you " + k.WrongWord + "), " +
                      (int)body.Position.DistanceTo(me.Position) + " m off (" + (fresh ? "a body cast" : "his own body") + ")");
        }

        /// The grudges' walk said it to your face: the kin's business is done; the street saw someone face you; a notice names him.
        private void KinGrudgeSaid(GrudgeWalk w, Ped me, int now)
        {
            KinGrudge k = _kinGrudgeWalk;
            if (k == null || w == null)
                return;
            _kinGrudgeWalk = null;
            _engine.KinDone(k, "said to your face", Bridge.Now());
            _kinGrudgeSaid++;
            ResBind rb;
            string kinId = w.Ped != null && _resBind.TryGetValue(w.Ped.Handle, out rb) && rb.R != null ? rb.R.Id : null;
            int h = w.Ped != null ? w.Ped.Handle : 0;
            RecordEvent("scene:kin", kinId, k.Name, "player", "you", me.Position, h, h, me.Handle, k.WrongWord);
            Feed.Notice("Kin", KinGrudges.Notice(k, _placeTown));
            if (h != 0)
                _ticker.Push(k.Name + ", thinking", KinGrudges.Thought(k, k.Id + "|after"), "near", h);
        }

        /// The walk never reached you (the time ran out, the body was lost, the switch went off): done all the same, and said so.
        private void KinGrudgeGaveUp()
        {
            if (_kinGrudgeWalk == null)
                return;
            _engine.KinDone(_kinGrudgeWalk, "never reached you", Bridge.Now());
            _kinGrudgeWalk = null;
        }

        private string KinGrudgesStatus()
        {
            if (_engine == null || _store == null)
                return "not loaded";
            return _engine.KinStatus(Bridge.Now()) + "; " + _kinGrudgeCasts + " cast, " + _kinGrudgeSaid + " said this session" + (_kinGrudgeLast != null ? "; last: " + _kinGrudgeLast : "");
        }
    }
}
