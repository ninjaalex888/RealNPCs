// Grudges that travel ([Scenes] Grudges; the words are Core/Grudges.cs). Between people: a friend of someone wronged in
// the last three days (a tie with a Doer: rough, pickpocket, brawl, scorn, passby, faced) comes upon the one who did it,
// near you, and has it out with them through the Director (the friend walks over). With the player: someone whose
// friend he killed (a friend's death they've had word of, in our own record "by the player", in the last week) and who
// is near him in town: the hot-tempered and the proud walk up and say it to his face (subtitled, the threat or angry
// voice), then turn away; the rest stare and think it. Once for each death and person (a mark, saved). Nothing
// violent from our side toward the player. Pacing "grudge" (30%, three game hours between, the pair or person once a
// day).
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
        private const int GrudgeLookMs = 30000, GrudgeWalkMs = 15000;

        private sealed class GrudgeWalk
        {
            public Ped Ped;
            public string Name, Line, Voice, Id, DeadId;
            public int Until, SaidAt;
            public KinGrudge Kin;   // a kin's walk (WestworldScript.KinGrudges.cs), so the word or the giving up is never put to another's walk; null for the grudges' own
        }

        private GrudgeWalk _grudgeWalk;
        private int _nextGrudge, _nextGrudgeYou, _grudges, _grudgesYou;
        private string _grudgeLast, _grudgeWhyNot;

        /// A friend of someone wronged has it out with the one who did it. True if a scene started.
        private bool GrudgeScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Grudges)
            {
                why = "[Scenes] Grudges is off";
                return false;
            }
            if (!test && now < _nextGrudge)
                return false;
            _nextGrudge = now + (int)(GrudgeLookMs * Story.WalkLook);   // (x the storyteller's)
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "grudge", _placeTown, null, g, _rng.Next(100), test, out skipped0);
            if (pre != null)
            {
                why = _grudgeWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test);
            var byId = new Dictionary<string, Candidate>();
            foreach (Candidate c in people)
                if (c.Who.Id != null && !byId.ContainsKey(c.Who.Id))
                    byId[c.Who.Id] = c;
            Candidate friend = null, doer = null;
            SceneTie wrong = null;
            float best = float.MaxValue;
            foreach (SceneTie t in _store.Ties)
            {
                Candidate a;
                string wrongId = Grudges.Wrongdoer(t);   // (a scorn's Doer asked: the wrong is the refusal)
                if (!Grudges.Wrong(t.Kind) || wrongId == null || g - t.At < 0 || g - t.At > 3 * 1440 || !byId.TryGetValue(wrongId, out a))
                    continue;
                string victim = t.OtherId(wrongId);
                foreach (SceneTie w in _store.Ties)
                {
                    if (!Ties.Friends(w) || !w.Has(victim) || w.Has(wrongId))
                        continue;
                    Candidate c;
                    string cid = w.OtherId(victim);
                    if (!byId.TryGetValue(cid, out c) || c == a || TieWarmth(cid, a.Who) > 0 || (!test && _engine.Grudged(cid, "tie|" + wrongId + "|" + t.At)))
                        continue;   // not a friend of the one who did it too, and not over this same wrong again
                    if (!test && !Grudges.KnowsOf(g - t.At, WorldEvents.Witnessed(_store.WorldLog, "scene:" + t.Kind, t.At, cid)))
                        continue;   // word hasn't reached them yet, and they didn't see it
                    float apart = c.Ped.Position.DistanceTo(a.Ped.Position), fromYou = Math.Min(c.Ped.Position.DistanceTo(me.Position), a.Ped.Position.DistanceTo(me.Position));
                    if (apart > (test ? 12f : 8f) || fromYou >= best || (!NodInView(me, c.Ped) && !NodInView(me, a.Ped)))
                        continue;
                    friend = c;
                    doer = a;
                    wrong = t;
                    best = fromYou;
                }
            }
            if (friend == null)
            {
                why = _grudgeWhyNot = "nobody here has a friend's wrong to settle with someone close by";
                return false;
            }
            string pair = NodPair(friend.Who.Id, doer.Who.Id), skipped;
            string pace = Pacing.WhyNot(_store.Pace, "grudge", _placeTown, pair, g, 0, test, out skipped);
            if (pace != null)
            {
                why = _grudgeWhyNot = "rests: " + pace;
                return false;
            }
            string seed = pair + "|grudge|" + g / 60;
            string victimName = Person.FirstOf(wrong.Other(doer.Who.Id));
            string manner = Exchanges.Manner(doer.Who, doer.Who.Hurt != null);
            bool hot = friend.Who.Temper == "Hothead" || friend.Who.Temper == "Proud" || doer.Who.Temper == "Hothead" || doer.Who.Temper == "Proud";
            SceneCast cast = BuildCast(friend.Ped, doer.Ped, null, _placeKind == "saloon", me, now, seed);
            bool blows = manner == "sharp" && hot && !cast.AWoman && !cast.BWoman && !cast.Off.Contains("brawl") &&
                         !(_hush && !test) && !(!test && _cfg.BeingWatched && LawWatching(friend.Ped, doer.Ped) != null);   // hushed or watched: words (the cross-cutting review)
            Scene s = Grudges.Confront(cast, seed, wrong.Kind, victimName, manner, blows);
            float apart2 = friend.Ped.Position.DistanceTo(doer.Ped.Position);
            if (apart2 > 2.6f)
                s.Beats.Insert(0, new Beat(0, null, null, "meet"));   // the friend goes over to them first
            StartScene(s, new List<Ped> { friend.Ped, doer.Ped }, cast, seed, now, test);
            if (!_director.Busy)
            {
                why = _grudgeWhyNot = "the Director wouldn't take the scene";
                return false;
            }
            Pacing.Note(_store.Pace, "grudge", _placeTown, pair, g);
            if (!test)
                _engine.MarkGrudged(friend.Who.Id, "tie|" + wrong.Doer + "|" + wrong.At);   // had it out over this once
            _grudges++;
            string outcome = s.Kind == "brawl" ? "comes to blows" : s.Kind == "brushoff" ? "walks off" : s.Kind == "apology" ? "says sorry" : "words";
            _grudgeLast = friend.Who.Name + " at " + doer.Who.Name + " for " + victimName + ": " + outcome + (test ? " (a test)" : "");
            string sk = skipped ?? skipped0;
            Log.Write("GRUDGE " + friend.Who.Name + " at " + doer.Who.Name + " for " + wrong.Other(doer.Who.Id) + " (" + wrong.Kind + ", " + (g - wrong.At) / 60 + " game h ago): " + outcome +
                      " (" + manner + ")" + (test ? ", a test" + (sk != null ? " (skipped: " + sk + ")" : "") : ""));
            return true;
        }

        /// Someone whose friend you killed, near you in town: says so to your face, or stares. True if it started.
        private bool GrudgeAtYou(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Grudges)
            {
                why = "[Scenes] Grudges is off";
                return false;
            }
            if (WalkUpGoing())
            {
                why = "one is coming up to you already, or you're in the Talk menu";
                return false;
            }
            if (!test && (now < _nextGrudgeYou || _placeTown == null))
                return false;
            _nextGrudgeYou = now + (int)(GrudgeLookMs * Story.WalkLook);   // (x the storyteller's)
            if (!test && (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle)))
            {
                why = "you're wanted, fighting or aiming";
                return false;   // (after its timer: the natives once a look, not every tick)
            }
            long g = _store.LastGameMin;
            foreach (Candidate c in ReasonCandidates(me, now, test).Where(x => x.Ped.Position.DistanceTo(me.Position) < 20f && !IsRecord(x.Ped.Handle))
                                                                     .OrderBy(x => x.Ped.Position.DistanceTo(me.Position)))
            {
                string deadId;
                string dead = _engine.YourKillGrieved(c.Who.Id, 7, out deadId);
                if (dead == null || !NodInView(me, c.Ped))
                    continue;
                string skipped;
                string pace = Pacing.WhyNot(_store.Pace, "grudge", _placeTown, c.Who.Id, g, _rng.Next(100), test, out skipped);
                if (pace != null)
                {
                    why = _grudgeWhyNot = "rests: " + pace;
                    return false;
                }
                Pacing.Note(_store.Pace, "grudge", _placeTown, c.Who.Id, g);
                if (!test)
                    StoryNoticed("you:grudge", 2, c.Ped.Position.DistanceTo(me.Position));   // the storyteller (WestworldScript.Story.cs)
                string seed = c.Who.Id + "|" + deadId;
                int h = c.Ped.Handle;
                _grudgesYou++;
                if (c.Who.Temper == "Hothead" || c.Who.Temper == "Proud")
                {
                    TASK.TASK_GO_TO_ENTITY(h, me.Handle, GrudgeWalkMs, 2.0f, 1.4f, 0f, 0);
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, GrudgeWalkMs, 0, 51, 0);
                    _grudgeWalk = new GrudgeWalk { Ped = c.Ped, Name = c.Who.Name, Line = Grudges.ToYourFace(c.Who.Temper, dead, seed), Voice = c.Who.Temper == "Hothead" ? "threat" : "angry", Until = now + GrudgeWalkMs,
                                                   Id = test ? null : c.Who.Id, DeadId = deadId };
                    _grudgeLast = c.Who.Name + " came up to you about " + dead + (test ? " (a test)" : "");
                    Log.Write("GRUDGE " + c.Who.Name + " at you for " + dead + ": walks up to you (" + c.Who.Temper + ", " + (int)c.Ped.Position.DistanceTo(me.Position) + " m)" + (test ? ", a test" : ""));
                }
                else
                {
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 3500, 0, 51, 0);
                    if (!test)
                        _engine.MarkGrudged(c.Who.Id, deadId);   // once for this death
                    string thought = Grudges.Stare(c.Who.Temper, dead, seed);
                    _ticker.Push(c.Who.Name + ", thinking", thought, "near", h);
                    _grudgeLast = c.Who.Name + " stared at you over " + dead + (test ? " (a test)" : "");
                    Log.Write("GRUDGE " + c.Who.Name + " at you for " + dead + ": stares (" + (c.Who.Temper ?? "plain") + "); thinks: " + thought + (test ? ", a test" : ""));
                }
                return true;
            }
            why = _grudgeWhyNot = "nobody near grieving someone you killed";
            return false;
        }

        /// The one walking up to you: says it to your face once there, then turns and goes; gives up if they can't reach you.
        private void GrudgeWalkStep(Ped me, int now)
        {
            GrudgeWalk w = _grudgeWalk;
            if (w == null)
                return;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null)
            {
                _grudgeWalk = null;
                KinGrudgeGaveUp(w, "the body was lost");   // a kin's walk lost its body (WestworldScript.KinGrudges.cs)
                return;
            }
            int h = w.Ped.Handle;
            if (w.SaidAt == 0)
            {
                if (w.Ped.Position.DistanceTo(me.Position) < 3f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 4000, 0f, 0f, 0f);
                    Feed.Say(w.Name, w.Line, Speech.When(h, w.Voice, me.Handle), h, toYou: true);
                    w.SaidAt = now;
                    if (w.Id != null)
                        _youNodAt[w.Id] = now;   // said it to his face: no passing look straight after (ToYou's rest)
                    if (w.Id != null)
                        _engine.MarkGrudged(w.Id, w.DeadId);   // said to your face: once for this death
                    Log.Write("GRUDGE " + w.Name + " to your face: " + w.Line);
                    KinGrudgeSaid(w, me, now);   // a kin's word: done, and the street saw it (WestworldScript.KinGrudges.cs)
                }
                else if (now > w.Until)
                {
                    PED.SET_PED_KEEP_TASK(h, false);   // handed back as taken (the kin's cast keeps the task while he walks)
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    _grudgeWalk = null;
                    Log.Write("GRUDGE " + w.Name + " never reached you: gave it up");
                    KinGrudgeGaveUp(w, "never reached you");
                }
                return;
            }
            if (now - w.SaidAt > 5000)
            {
                PED.SET_PED_KEEP_TASK(h, false);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // said it; turns and goes
                _grudgeWalk = null;
            }
        }

        private Action _aGrudges;

        /// Tick04: the one walking up to you, and now and then a look for someone grieving one you killed.
        private void GrudgesStep(int now)
        {
            if (!_cfg.Grudges && _grudgeWalk != null && _grudgeWalk.Kin == null)
                GrudgesAbort();   // switched off mid-walk: let them go (else they'd stay busy, and the thanks would wait for ever); a kin's walk is stepped by KinGrudgesStep then
            if (!_cfg.Grudges || _store == null || (_grudgeWalk == null && now < _nextGrudgeYou))
                return;
            if (_aGrudges == null)
                _aGrudges = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    GrudgeWalkStep(me, _now);
                    string why;
                    if (_grudgeWalk == null && !_director.Busy && _now >= _nextGrudgeYou)
                        GrudgeAtYou(me, _now, false, out why);
                };
            Safe("grudges", _aGrudges);
        }

        private void GrudgesAbort()
        {
            GrudgeWalk w = _grudgeWalk;
            _grudgeWalk = null;
            if (w == null)
                return;
            KinGrudgeGaveUp(w, "the walk was called off");   // a kin's: done, never re-cast for the same word (the review of item 14)
            if (w.Ped != null && w.Ped.Exists() && !w.Ped.IsDead)
            {
                int h = w.Ped.Handle;
                PED.SET_PED_KEEP_TASK(h, false);
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // the body is the game's again, going its own way (not left standing)
            }
        }

        /// F7: on or off, the confrontations between people and with you this session, the last, and why not lately.
        private string GrudgesStatus()
        {
            if (!_cfg.Grudges)
                return "off ([Scenes] Grudges)";
            return _grudges + " between people, " + _grudgesYou + " with you this session" + (_grudgeWalk != null ? "; " + _grudgeWalk.Name + " is coming up to you" : "") +
                   (_grudgeLast != null ? "; last: " + _grudgeLast : "") + (_grudgeWhyNot != null ? "; the last look: " + _grudgeWhyNot : "");
        }
    }
}
