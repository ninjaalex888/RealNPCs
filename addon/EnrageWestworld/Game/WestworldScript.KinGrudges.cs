// The kin who take up a wrong ([Scenes] KinGrudges; the words and the bookkeeping are Core/KinGrudges.cs and
// Core/Engine.KinGrudges.cs; SIXDAY item 14; roadmap 20). Every fifteen seconds in a town, with no one else walking up to
// you: the engine's grudge due here (the one wronged's town, or the kin's own) gets a body: a free unseen man 30-120 m off
// becomes the kin, a resident of his town from then on (one body with the name: his own if he already has one near), and
// walks up to you by the grudges' own walk (GrudgeWalk in WestworldScript.Grudges.cs, which carries the grudge as its Kin
// so a word or a walk given up is never put to the wrong man): at your side he says it to your face in the game's angry
// or threatening voice, with the line subtitled, and turns away; the town saw someone face you (a happening, "scene:kin");
// a notice names him. Nothing violent from our side, as the grudge rule has it. F7 "TEST kingrudges:".
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
        private string _kinGrudgeLast;
        private Action _aKinGrudge, _aKinGrudgeWalk;

        /// Tick04: a look every fifteen seconds for the kin due here; the grudges' walk stepped here too (under its own Safe)
        /// when [Scenes] Grudges is off, since GrudgesStep steps nothing then.
        private void KinGrudgesStep(int now)
        {
            if (_cfg == null || !_cfg.KinGrudges || _store == null || _engine == null)
                return;
            if (!_cfg.Grudges && _grudgeWalk != null && _grudgeWalk.Kin != null)
            {
                if (_aKinGrudgeWalk == null)
                    _aKinGrudgeWalk = () =>
                    {
                        Ped me0 = Game.Player.Character;
                        if (me0 != null && me0.Exists() && !me0.IsDead)
                            GrudgeWalkStep(me0, _now);
                    };
                Safe("kingrudge", _aKinGrudgeWalk);
            }
            if (_kinDoer != null)
            {
                if (_aKinDoer == null)
                    _aKinDoer = () => KinDoerStep(Game.Player.Character, _now);   // made once (no allocation a tick: the Part 5 review)
                Safe("kingrudge", _aKinDoer);   // a townsman's grudge on its way (FIVEDAY item 32b)
            }
            if (now < _nextKinGrudge)
                return;
            _nextKinGrudge = now + KinGrudgeLookMs;
            if (_aKinGrudge == null)
                _aKinGrudge = () => KinGrudgeLook(_now);
            Safe("kingrudge", _aKinGrudge);
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
            if (k == null && _cfg.KinTownWrongs && _kinDoer == null)
            {
                // a townsman's grudge, when the man who did it is near you (FIVEDAY item 32b)
                k = _engine.KinDue(_placeTown, g, id => KinDoerNear(id, me) != null);
                if (k != null)
                {
                    KinGrudgeCast(k, me, now, g, KinDoerNear(k.DoerId, me));
                    return;
                }
            }
            if (k == null)
                return;
            KinGrudgeCast(k, me, now, g);
        }

        /// The kin's body: his own if he already walks this town (cast before and still about, free to be sent), else a free
        /// unseen man 30-120 m off, made a resident of his town and bound to the name (any other body with his name let go
        /// first: one body with the name); then the walk up. A kin who died since (the resident's record) comes no more.
        private void KinGrudgeCast(KinGrudge k, Ped me, int now, long g, Ped doer = null)
        {
            Ped body = null;
            Resident res = k.CastId != null ? _store.FindResident(k.CastId) : null;
            if (res != null && res.Dead)
            {
                _engine.KinDone(k, "the man died before he could say it", g);
                return;
            }
            if (res != null)
            {
                foreach (KeyValuePair<int, ResBind> kv in _resBind)
                    if (kv.Value.R == res && ENTITY.DOES_ENTITY_EXIST(kv.Key) && !ENTITY.IS_ENTITY_DEAD(kv.Key))
                    {
                        Ped own = new Ped(kv.Key);
                        if (own.Position.DistanceTo(me.Position) < 150f && !_director.Involves(kv.Key) && MayDirectWhyNot(own) == null)
                        {
                            body = own;
                            break;
                        }
                    }
                if (body == null)
                    UnbindOthers(res.Id);   // his old body is gone, busy or far: the name comes off it before another is cast
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
                    if (k.Temper != null)
                        res.Personality = k.Temper;   // the card's temper and the line's agree (the review of item 14)
                    _engine.ForgetPerson(res.Id);
                    k.CastId = res.Id;   // one body with the name, this time and the next
                }
                _engine.KeepResident(res, g);
                _resBind[bh] = new ResBind { R = res, Model = model };
                fresh = true;
            }
            int handle = body.Handle;
            if (doer != null)
            {
                // a townsman's grudge: he goes to the man who did it, not to you (KinDoerStep has it out when he's there)
                PED.SET_PED_KEEP_TASK(handle, true);
                TASK.CLEAR_PED_TASKS(handle, true, false);
                TASK.TASK_GO_TO_ENTITY(handle, doer.Handle, KinGrudgeWalkMs, 2.0f, 1.6f, 0f, 0);
                _kinDoer = new KinDoerWalk { Kin = body, Doer = doer, K = k, Until = now + KinGrudgeWalkMs };
                _kinGrudgeCasts++;
                _kinGrudgeLast = k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + ") for " + k.DoerName + " in " + _placeTown;
                Log.Write("KIN GRUDGE: " + k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + ", " + (k.Temper ?? "plain") + ") walks up to " + k.DoerName + " in " + _placeTown +
                          ", " + (int)body.Position.DistanceTo(doer.Position) + " m off (" + (fresh ? "a body cast" : "his own body") + ")");
                return;
            }
            // the kin had the surname from the one wronged, if they knew it
            Person victim = _engine.KinPerson(k.VictimId);
            PersonState vst = victim != null ? _store.Get(victim) : null;
            string you = vst != null && vst.KnowsName ? _engine.Player.Surname : null;
            string line = KinGrudges.Opening(k, you, k.Id);
            PED.SET_PED_KEEP_TASK(handle, true);
            TASK.CLEAR_PED_TASKS(handle, true, false);
            TASK.TASK_GO_TO_ENTITY(handle, me.Handle, KinGrudgeWalkMs, 2.0f, 1.6f, 0f, 0);
            TASK.TASK_LOOK_AT_ENTITY(handle, me.Handle, KinGrudgeWalkMs, 0, 51, 0);
            _grudgeWalk = new GrudgeWalk { Ped = body, Name = k.Name, Line = line, Voice = KinGrudges.VoiceFor(k.Temper), Until = now + KinGrudgeWalkMs, Id = null, DeadId = null, Kin = k };
            _kinGrudgeCasts++;
            _kinGrudgeLast = k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + ") in " + _placeTown;
            Log.Write("KIN GRUDGE: " + k.Name + " (" + Person.FirstOf(k.VictimName ?? "?") + "'s " + k.Relation + ", " + (k.Temper ?? "plain") + ") comes up to you in " + _placeTown + " on " + Person.FirstOf(k.VictimName ?? "?") + "'s account (you " + k.WrongWord + "), " +
                      (int)body.Position.DistanceTo(me.Position) + " m off (" + (fresh ? "a body cast" : "his own body") + ")");
        }

        /// The grudges' walk said it to your face: the kin's business is done; the street saw someone face you; a notice names
        /// him; no passing nod from him straight after (ToYou's rest). Only for a walk that is a kin's.
        private void KinGrudgeSaid(GrudgeWalk w, Ped me, int now)
        {
            KinGrudge k = w != null ? w.Kin : null;
            if (k == null)
                return;
            w.Kin = null;
            _engine.KinDone(k, "said to your face", Bridge.Now());
            _kinGrudgeSaid++;
            ResBind rb;
            string kinId = w.Ped != null && _resBind.TryGetValue(w.Ped.Handle, out rb) && rb.R != null ? rb.R.Id : null;
            int h = w.Ped != null ? w.Ped.Handle : 0;
            if (kinId != null)
                _youNodAt[kinId] = now;
            RecordEvent("scene:kin", kinId, k.Name, "player", "you", me.Position, h, h, me.Handle, k.WrongWord);
            Feed.Notice("Kin", KinGrudges.Notice(k, _placeTown));
            if (h != 0)
                _ticker.Push(k.Name + ", thinking", KinGrudges.Thought(k, k.Id + "|after"), "near", h);
        }

        /// A kin's walk that ended without the word (the time ran out, the body was lost, you aimed at him): done all the same,
        /// and said so; `keep` (the walk called off by a mission or the add-on stopping, nothing of his doing): the grudge stands and
        /// he comes again tomorrow (Engine.KinLater; the review of items 15-19). Nothing for a walk that isn't a kin's.
        private void KinGrudgeGaveUp(GrudgeWalk w, string how, bool keep = false)
        {
            KinGrudge k = w != null ? w.Kin : null;
            if (k == null)
                return;
            w.Kin = null;
            if (keep)
                _engine.KinLater(k, how ?? "the walk was called off", Bridge.Now());
            else
                _engine.KinDone(k, how ?? "never reached you", Bridge.Now());
        }

        // ---- a townsman's grudge (FIVEDAY item 32b; [Scenes] KinTownWrongs) ----------------------------------------------

        private sealed class KinDoerWalk
        {
            public Ped Kin, Doer;
            public KinGrudge K;
            public int Until;
        }

        private KinDoerWalk _kinDoer;
        private Action _aKinDoer;
        private int _kinDoerHad;

        /// The man who did it, by id, among the named people near you (within 60 m of you, free to be directed), else null.
        private Ped KinDoerNear(string id, Ped me)
        {
            if (id == null)
                return null;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id != id || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > 60f || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || PED.IS_PED_ON_MOUNT(h))
                    return null;
                return p;
            }
            return null;
        }

        /// Every tick while the kin walks: at the man's side (within 3 m), the friends' confrontation with the kin's own
        /// first words (who he is, from where); the man answers by his manner. Done once said; lost or out of time: done
        /// too (the man was found and slipped him); called off by a mission: again tomorrow.
        private void KinDoerStep(Ped me, int now)
        {
            KinDoerWalk w = _kinDoer;
            if (w == null)
                return;
            if (MISC.GET_MISSION_FLAG())
            {
                EndKinDoer("a mission began");
                return;
            }
            if (!w.Kin.Exists() || w.Kin.IsDead || !w.Doer.Exists() || w.Doer.IsDead || now > w.Until)
            {
                _kinDoer = null;
                if (w.Kin.Exists() && !w.Kin.IsDead)
                    PED.SET_PED_KEEP_TASK(w.Kin.Handle, false);
                _engine.KinDone(w.K, "never reached " + (w.K.DoerName ?? "him"), Bridge.Now());
                return;
            }
            if (w.Kin.Position.DistanceTo(w.Doer.Position) > 3f || _director.Busy)
                return;
            _kinDoer = null;
            MindWho dw;
            Someone doerWho = _mindWho.TryGetValue(w.Doer.Handle, out dw) ? new Someone { Id = dw.Id, Name = dw.Name, Temper = dw.P != null ? dw.P.Personality : null } : new Someone { Id = w.K.DoerId, Name = w.K.DoerName };
            string seed = "kintown|" + w.K.Id;
            string manner = Exchanges.Manner(doerWho, false);
            bool hot = w.K.Temper == "Hothead" || w.K.Temper == "Proud" || doerWho.Temper == "Hothead" || doerWho.Temper == "Proud";
            SceneCast cast = BuildCast(w.Kin, w.Doer, null, _placeKind == "saloon", me, now, seed);
            bool blows = manner == "sharp" && hot && !cast.BWoman && !cast.Off.Contains("brawl") && !_hush && !(_cfg.BeingWatched && LawWatching(w.Kin, w.Doer) != null);
            string wrong = w.K.Wrong != null && w.K.Wrong.StartsWith("town:", StringComparison.Ordinal) ? w.K.Wrong.Substring(5) : "rough";
            Scene s = Grudges.Confront(cast, seed, wrong, Person.FirstOf(w.K.VictimName ?? "my kin"), manner, blows);
            s.Beats.Insert(1, new Beat(0, KinGrudges.TownIntro(w.K, seed), "angry", null));   // who he is, before the wrong
            s.Summary = "a man's kin came to town and had it out with the one who wronged him";
            StartScene(s, new List<Ped> { w.Kin, w.Doer }, cast, seed, now, false);
            _engine.KinDone(w.K, "had it out with " + (w.K.DoerName ?? "him"), Bridge.Now());
            _kinDoerHad++;
            Feed.Notice("Kin", KinGrudges.TownNotice(w.K, _placeTown));
            Log.Write("KIN GRUDGE: " + w.K.Name + " confronts " + w.K.DoerName + " for " + Person.FirstOf(w.K.VictimName ?? "?") + " (" + manner + (blows ? ", blows" : "") + ")");
        }

        /// The walk called off by something outside the story: the grudge stands, again tomorrow (Engine.KinLater).
        private void EndKinDoer(string why)
        {
            KinDoerWalk w = _kinDoer;
            _kinDoer = null;
            if (w == null)
                return;
            if (w.Kin != null && w.Kin.Exists())
            {
                PED.SET_PED_KEEP_TASK(w.Kin.Handle, false);
                TASK.CLEAR_PED_TASKS(w.Kin.Handle, true, false);
            }
            _engine.KinLater(w.K, why, Bridge.Now());
        }

        private string KinGrudgesStatus()
        {
            if (_engine == null || _store == null)
                return "not loaded";
            return _engine.KinStatus(Bridge.Now()) + "; " + _kinGrudgeCasts + " cast, " + _kinGrudgeSaid + " said this session, " + _kinDoerHad + " had it out with a townsman" + (_kinGrudgeLast != null ? "; last: " + _kinGrudgeLast : "") +
                   (_grudgeWalk != null && _grudgeWalk.Kin != null ? "; " + _grudgeWalk.Name + " is coming up to you now" : "");
        }
    }
}
