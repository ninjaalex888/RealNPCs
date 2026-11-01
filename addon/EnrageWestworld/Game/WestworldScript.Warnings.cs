// Witnessed, or warned, the game side (Core/Warnings.cs has the tables and the words; NIGHT4 item 5). Every 40 s (x the
// storyteller's) in a town, with [Mind] MindLive and [Scenes] WarnedYou, never while another walk-up is going and only to
// a stopped Arthur: someone near whom the mind knows, free, standing, in view within 18 m, with a real fact about him (w1:
// they saw his violence today and aren't afraid of him; w2: someone else within 30 m mourns one he killed or is still
// strongly angry at him, and they think well of him (by name only if tied to him); w3: a known deed of his in this town
// in the last two days and a lawman within 60 m, and they think well of him; never a lawman, one warning of a danger a
// day, Pacing "warn") chooses by their mind (with [Mind] LanguagePicks the model among the same, 2.5 s at
// most): have it out in words (the hot and the proud), a hard stare, a quiet warning, a look toward the danger with a
// word, or let it be. The walk is the lonely walk's way; the words are said at 2.6 m over the game's own voice, then
// they turn away. Each fact once a person a day. Words, looks and turning away only.
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
        private const int WarnLookMs = 40000, WarnAfterMs = 4000;
        private const float WarnMetres = 18f, WarnGrudgeMetres = 30f, WarnLawMetres = 60f;
        private static readonly string[] WarnHarmed = { "WAS_THIS_YOU", "TROUBLE_RECENTLY" };

        private sealed class WarnWalk
        {
            public Ped Ped;
            public string Id, Name, Fact, Danger, DangerId, You, Line, Pick;
            public int DangerHandle, Until, SaidAt, StartedAt;
            public float Nearest = float.MaxValue;
            public bool Test, Walking;
            public StagedAsk Ask;
        }

        private WarnWalk _warnWalk;
        private int _nextWarn, _warnStepAt, _warnStillSince, _warnLookedAt;
        private int _warnOut, _warnWarned, _warnGrudge, _warnLaw, _warnHinted, _warnStared, _warnLetBe, _warnModel, _warnModelOf;
        private string _warnLast, _warnWhyNot;
        private readonly HashSet<string> _warnDone = new HashSet<string>();   // id|fact|day: said (or let be) once
        private Action _aWarn;

        private void WarnStep(int now)
        {
            if (!_cfg.WarnedYou || !_cfg.MindLive)
            {
                if (_warnWalk != null)
                    WarnLetGo(_warnWalk.Ped != null && _warnWalk.Ped.Exists() ? _warnWalk.Ped.Handle : 0);
                return;
            }
            if (_store == null || (_warnWalk == null && now < _nextWarn))
                return;
            if (_aWarn == null)
                _aWarn = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    if (_warnWalk != null)
                    {
                        if (_now < _warnStepAt)
                            return;
                        _warnStepAt = _now + 250;
                        WarnWalkStep(me, _now);
                    }
                    else if (_now >= _nextWarn)
                        WarnLook(me, _now, false);
                };
            Safe("warned you", _aWarn);
        }

        private bool WarnWaiting(int h)
        {
            return _warnWalk != null && _warnWalk.Ped != null && _warnWalk.Ped.Handle == h;
        }

        /// Their regard of him from the record or the folk alone (SightRegard's "saw you violent means afraid" left out:
        /// a witness who isn't afraid is the one who has it out with him).
        private string WarnRegard(Ped p)
        {
            RecordRef rec = _bridge.RecordAt(p.Handle);
            if (rec != null)
            {
                Person rp = PersonOf(rec);
                return rp.Fear >= 40 ? "afraid" : rp.Hate >= 50 ? "hostile" : null;
            }
            FolkRef ac = _bridge.AcquaintanceAt(p.Handle);
            if (ac != null)
                return ac.Fear >= 40 ? "afraid" : ac.Grudge >= 40 ? "hostile" : ac.Like >= 40 ? "fond" : null;
            return null;
        }

        /// The strongest of one feeling about him still moving their mood (the mood's slower fading).
        private float FeltAboutYou(string id, string kind, long g)
        {
            List<FeltAt> felt = _engine.Shadow.Felt(id);
            if (felt == null)
                return 0f;
            float best = 0f;
            foreach (FeltAt f in felt)
                if (f.Kind >= 0 && f.Kind < Feeling.Kinds.Length && Feeling.Kinds[f.Kind] == kind && Passing.AboutB(f, "player"))
                {
                    float x = Mood.Weight(f, g);
                    if (x > best)
                        best = x;
                }
            return best;
        }

        /// Whether the warner knows the grudge-holder: a street scene between them, a worry linking them, or one family
        /// (the same surname). Pure lookups over the store; called only for the one candidate who'd warn.
        private bool WarnKnowsOf(MindWho w, MindWho o)
        {
            if (w == null || o == null || w.Id == null || o.Id == null)
                return false;
            foreach (SceneTie t in _store.Ties)
                if (t != null && ((t.A == w.Id && t.B == o.Id) || (t.A == o.Id && t.B == w.Id)))
                    return true;
            foreach (WorryLink l in _store.Links)
                if ((Links.IsOf(l, w.Id) && Links.IsOtherOf(l, o.Id)) || (Links.IsOf(l, o.Id) && Links.IsOtherOf(l, w.Id)))
                    return true;
            string a = w.P != null ? w.P.Surname : null, b = o.P != null ? o.P.Surname : null;
            return !string.IsNullOrEmpty(a) && a == b;
        }

        private string WarnLook(Ped me, int now, bool test)
        {
            _nextWarn = now + (int)(WarnLookMs * Story.WalkLook);
            if (WalkUpGoing())
                return _warnWhyNot = "someone is already coming up to you, or you're in the Talk menu";
            if (!test && (_placeTown == null || _director.Busy))
                return _warnWhyNot = _placeTown == null ? "out of the towns" : "a scene is going on";
            if (MISC.GET_MISSION_FLAG() || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                return _warnWhyNot = "a mission, or you're wanted, fighting or aiming";
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                return _warnWhyNot = "you're riding";
            bool still = ENTITY.GET_ENTITY_SPEED(me.Handle) <= 0.4f;
            if (!still)
                _warnStillSince = 0;
            else if (_warnStillSince == 0 || now - _warnLookedAt > 1500)
                _warnStillSince = now;
            _warnLookedAt = now;
            if (!test && (_warnStillSince == 0 || now - _warnStillSince < 2000))
            {
                _nextWarn = now + 1000;
                return _warnWhyNot = "you're on the move";
            }
            long g = _store.LastGameMin, day = g / 1440;
            string skipped;
            string pre = Pacing.WhyNot(_store.Pace, "warn", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pre != null)
                return _warnWhyNot = "rests: " + pre;   // one walk-up of this kind every while, never a street's worth back to back (the review)
            Vector3 mePos = me.Position;
            // w2's grudge-holder: someone near mourning one he killed or still strongly angry at him (pure tests first), not
            // one already warned of today
            int grudgeH = 0;
            string grudgeName = null, grudgeId = null;
            MindWho grudgeWho = null;
            foreach (int h in _mindNear)
            {
                MindWho o;
                if (!_mindWho.TryGetValue(h, out o) || o.Id == null)
                    continue;
                if (!test && _warnDone.Contains("about|" + o.Id + "|" + day))
                    continue;
                if (!_engine.MournsYourKill(o.Id, 7) && FeltAboutYou(o.Id, "anger", g) < Warnings.GrudgeAnger)
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(mePos) > WarnGrudgeMetres)
                    continue;
                grudgeH = h;
                grudgeName = o.Name;
                grudgeId = o.Id;
                grudgeWho = o;
                break;
            }
            // w3: a known deed of his here lately (the law not yet warned of here today), and only then a lawman near
            bool deedHere = false;
            if (_placeTown != null && (test || !_warnDone.Contains("law|" + _placeTown + "|" + day)))
                for (int i = _store.Deeds.Count - 1; i >= 0; i--)
                {
                    Deed d = _store.Deeds[i];
                    if (d == null || g - d.At > 2880)
                        continue;
                    string grp = Minds.DeedGroup(d.Kind);
                    if (d.Known && d.Town == _placeTown && (grp == "deed:violence" || grp == "deed:theft"))
                    {
                        deedHere = true;
                        break;
                    }
                }
            int lawH = 0;
            if (deedHere)
            {
                int wd = WatchEnter("warnings: a lawman near");
                try
                {
                    foreach (Ped q in Peds.All())   // the distance first, the lawman test last (the frame-cost audit)
                        if (q != null && q.Position.DistanceTo(mePos) <= WarnLawMetres && q.Exists() && !q.IsDead && Natives.IsLaw(q))
                        {
                            lawH = q.Handle;
                            break;
                        }
                }
                finally
                {
                    WatchLeave(wd);
                }
            }
            int known = 0;
            Ped nearestNamed = null;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || h == grudgeH || h == lawH || w.TradeGroup == "law")
                    continue;   // never the danger, nor a lawman (the law's dealings are the game's)
                known++;
                bool saw = _engine.SawYouViolent(w.Id, 1);
                string fact = saw ? Warnings.Saw : grudgeH != 0 ? Warnings.Grudge : lawH != 0 ? Warnings.Law : null;
                if (fact == null && !test)
                    continue;
                if (!test && _warnDone.Contains(w.Id + "|" + fact + "|" + day))
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                bool woman = !PED.IS_PED_MALE(h);
                if (woman && !test && (Natives.Night() || _engine.NowIndoors))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(mePos) > WarnMetres || !NodInView(me, p) || MayDirectWhyNot(p) != null || NotFree(h) || AimedLately(h, now) || _director.Involves(h) || PED.IS_PED_USING_ANY_SCENARIO(h) || Natives.IsLaw(p))
                    continue;
                if (nearestNamed == null)
                    nearestNamed = p;
                string regard = WarnRegard(p), temper = w.Src != null ? w.Src.Temper : null;
                if (regard == "afraid" || regard == "hostile")
                    continue;
                bool grateful = FeltAboutYou(w.Id, "gratitude", g) >= Passing.MindLeft || Interact.KindLately(_store.WorldLog, w.Id, g);
                if (fact == Warnings.Saw && !test && temper == "Coward")
                    fact = grudgeH != 0 ? Warnings.Grudge : lawH != 0 ? Warnings.Law : null;   // a coward never has it out; perhaps the other facts
                if (fact != Warnings.Saw && fact != null && !test && !Warnings.MayWarn(regard, grateful, _engine.KnowsYourName(w.Id), temper))
                    continue;
                if (fact == null)
                {
                    if (!test)
                        continue;
                    fact = Warnings.Grudge;   // a test with no fact: the nearest other named person stands in as the grudge-holder
                }
                if (Pacing.WhyNot(_store.Pace, "warn", _placeTown, w.Id, g, 0, test, out skipped) != null)
                    continue;
                // w2 by name only from someone tied to the grudge-holder (a scene between them, a worry's link, the same
                // surname); anyone else has only seen the looks, and may only hint
                bool knowsDanger = fact != Warnings.Grudge || grudgeWho == null || WarnKnowsOf(w, grudgeWho);
                string danger = fact == Warnings.Grudge && knowsDanger ? grudgeName : null;
                int dangerH = fact == Warnings.Grudge ? grudgeH : fact == Warnings.Law ? lawH : 0;
                bool standIn = false;
                if (fact == Warnings.Grudge && dangerH == 0)
                {
                    foreach (int o in _mindNear)
                    {
                        MindWho ow;
                        if (o != h && _mindWho.TryGetValue(o, out ow) && ow.Id != null && ENTITY.DOES_ENTITY_EXIST(o))
                        {
                            dangerH = o;
                            danger = ow.Name;
                            standIn = true;
                            break;
                        }
                    }
                }
                bool cowed = FeltAboutYou(w.Id, "fear", g) >= Warnings.Cowed;
                Someone who = SomeoneOf(p);
                StagedMind sm = who != null ? StagedMindOf(who, null) : new StagedMind { Temper = temper, Woman = woman };
                Staged.Weights wt = Warnings.Weights(fact, sm, cowed, knowsDanger);
                string you = _engine.KnowsYourName(w.Id) ? YourSurname() : null;
                string f0 = fact, d0 = danger;
                StagedAsk ask = StagedChoose(h, "warn", wt, w.Id + "|" + fact + "|" + day + "|warn", id => Warnings.Words(id, f0, d0), Warnings.Now(fact, you, danger, _placeTown), danger != null ? Person.FirstOf(danger) : null, now);
                if (ask.Asked)
                    _warnModelOf++;
                if (!test)
                {
                    if (_warnDone.Count > 300)
                        _warnDone.Clear();
                    _warnDone.Add(w.Id + "|" + fact + "|" + day);
                    Pacing.Note(_store.Pace, "warn", _placeTown, w.Id, g);
                }
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4000, 0, 51, 0);   // they've noticed him; the choice follows
                _warnWalk = new WarnWalk { Ped = p, Id = w.Id, Name = w.Name, Fact = fact, Danger = danger, DangerId = fact == Warnings.Grudge && !standIn ? grudgeId : null, DangerHandle = dangerH, You = you, Ask = ask, Test = test, StartedAt = now };
                Log.Write("WARN " + w.Name + " (" + fact + (danger != null ? ": " + danger + (standIn ? ", a test's stand-in" : "") : fact == Warnings.Grudge && grudgeName != null ? ": " + grudgeName + ", no tie to them (a hint at most)" : "") + ", " + (temper ?? "plain") + (cowed ? ", afraid of you" : "") + ") has noticed you (" +
                          (int)p.Position.DistanceTo(mePos) + " m)" + (test ? ", a test" : ""));
                return null;
            }
            return _warnWhyNot = known == 0 ? "nobody the mind knows is near" : grudgeH == 0 && lawH == 0 ? "nobody near saw you do violence today, holds a grudge, or the law isn't asking" : "nobody near who'd say it, free and in view";
        }

        private void WarnWalkStep(Ped me, int now)
        {
            WarnWalk w = _warnWalk;
            if (w == null)
                return;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null || MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0))
            {
                _warnWalk = null;
                return;
            }
            int h = w.Ped.Handle;
            if (w.Pick == null)
            {
                if (!StagedSettle(w.Ask, "warn", now))
                    return;   // the model's answer still due (2.5 s at most)
                w.Pick = w.Ask.Picked ?? "letbe";
                if (w.Ask.ByModel)
                    _warnModel++;
                Log.Write("WARN " + w.Name + " (" + w.Fact + ") chose " + w.Pick + " (" + By(w.Ask) + ")");
                if (w.Pick == "stare" || w.Pick == "letbe")
                {
                    string t = Warnings.Thought(w.Pick, w.Fact, w.Id + "|" + now / 60000);
                    if (w.Pick == "stare")
                    {
                        _warnStared++;
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 5000, 0, 51, 0);
                    }
                    else
                        _warnLetBe++;
                    _ticker.Push(w.Name + ", thinking", t, "near", h);
                    _warnLast = w.Name + ": " + (w.Pick == "stare" ? "stared you down" : "let it be") + " (" + w.Fact + ")" + (w.Test ? " (a test)" : "");
                    Log.Write("WARN " + w.Name + ": " + w.Pick + "; thinks: " + t);
                    _warnWalk = null;   // the look plays out by itself; nothing held
                    return;
                }
                float metres = w.Ped.Position.DistanceTo(me.Position);
                int walkMs = Math.Min(25000, 4000 + (int)(900 * metres));
                TASK.TASK_GO_TO_ENTITY(h, me.Handle, walkMs, 2.0f, 1.0f, 0f, 0);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, walkMs + WarnAfterMs, 0, 51, 0);
                w.Until = now + walkMs;
                w.Walking = true;
                StoryNoticed("you:warn", 2, metres);   // the storyteller (WestworldScript.Story.cs)
                return;
            }
            if (w.SaidAt == 0)
            {
                float d = w.Ped.Position.DistanceTo(me.Position);
                if (d < w.Nearest)
                    w.Nearest = d;
                if (d > w.Nearest + 3f || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || _menu != null || _lonelyWalk != null || _favourWalk != null ||
                    _grudgeWalk != null || _thanksWalk != null || _kinWalk != null || _lifter != null)
                {
                    WarnLetGo(h);
                    Log.Write("WARN " + w.Name + ": you went on, or were taken up; they let it be");
                    return;
                }
                if (d < 2.6f)
                {
                    w.Line = Warnings.Line(w.Pick, w.Fact, w.Danger, null, w.You, w.Id + "|" + now / 60000);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, WarnAfterMs, 0f, 0f, 0f);
                    if (w.Line != null)
                    {
                        if (w.Pick == "haveitout")
                        {
                            Speech.SayAny(h, WarnHarmed, me.Handle);   // the game's own voice for it
                            Feed.Say(w.Name, w.Line, null, h, toYou: true);
                        }
                        else
                            Feed.Say(w.Name, w.Line, Speech.For(h, w.Line, me.Handle, 0, 0, true, "chat"), h, toYou: true);
                    }
                    if (w.Pick == "hint" && w.DangerHandle != 0 && ENTITY.DOES_ENTITY_EXIST(w.DangerHandle))
                        TASK.TASK_LOOK_AT_ENTITY(h, w.DangerHandle, 3000, 0, 51, 0);   // a look toward the danger
                    w.SaidAt = now;
                    if (!w.Test && w.Pick != "haveitout")
                    {
                        long day = _store.LastGameMin / 1440;   // the danger warned of: nobody else warns of it today
                        if (w.Fact == Warnings.Grudge && w.DangerId != null)
                            _warnDone.Add("about|" + w.DangerId + "|" + day);
                        else if (w.Fact == Warnings.Law && _placeTown != null)
                            _warnDone.Add("law|" + _placeTown + "|" + day);
                    }
                    if (w.Pick == "haveitout") _warnOut++;
                    else if (w.Pick == "hint") _warnHinted++;
                    else
                    {
                        _warnWarned++;
                        if (w.Fact == Warnings.Law) _warnLaw++;
                        else _warnGrudge++;
                    }
                    Log.Write("WARN " + w.Name + " to your face: " + (w.Line ?? "(nothing)"));
                }
                else if (now > w.Until)
                {
                    WarnLetGo(h);
                    Log.Write("WARN " + w.Name + " never reached you: went on their way");
                }
                return;
            }
            if (now - w.SaidAt > WarnAfterMs || me.Position.DistanceTo(w.Ped.Position) > 6f)
            {
                string t = Warnings.Thought(w.Pick, w.Fact, w.Id + "|" + now / 60000);
                _ticker.Push(w.Name + ", thinking", t, "near", h);
                _warnLast = w.Name + ": " + w.Pick + " (" + w.Fact + ")" + (w.Test ? " (a test)" : "");
                Log.Write("WARN " + w.Name + ": turns away; thinks: " + t);
                WarnLetGo(h);
            }
        }

        /// A mission, the add-on stopping or a wipe: the one coming over stops, their look and walk cleared, no new order (as
        /// LonelyAbort).
        private void WarnAbort()
        {
            if (_warnWalk != null && _warnWalk.Ped != null && _warnWalk.Ped.Exists())
            {
                TASK.TASK_CLEAR_LOOK_AT(_warnWalk.Ped.Handle);
                TASK.CLEAR_PED_TASKS(_warnWalk.Ped.Handle, true, false);
            }
            _warnWalk = null;
        }

        private void WarnLetGo(int h)
        {
            if (h != 0 && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
            {
                TASK.TASK_CLEAR_LOOK_AT(h);
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            }
            _warnWalk = null;
        }

        private string WarnTest(int now)
        {
            if (!_cfg.MindLive)
                return "[Mind] MindLive is off (what they know of you comes from the mind)";
            if (_warnWalk != null)
                return "someone is already coming over";
            return WarnLook(Game.Player.Character, now, true);
        }

        private string WarnStatus()
        {
            if (!_cfg.WarnedYou)
                return "off ([Scenes] WarnedYou)";
            return (_cfg.MindLive ? "on" : "on, but [Mind] MindLive is off") + "; had it out " + _warnOut + ", warned " + _warnWarned + " (a grudge " + _warnGrudge + ", the law " + _warnLaw + "), hinted " + _warnHinted +
                   ", stared " + _warnStared + ", let be " + _warnLetBe + "; the model's " + _warnModel + " of " + _warnModelOf +
                   (_warnWalk != null ? "; " + _warnWalk.Name + " is " + (_warnWalk.Pick == null ? "choosing" : _warnWalk.SaidAt == 0 ? "coming over" : "saying it") : "") +
                   "; last: " + (_warnLast ?? "none") + (_warnWhyNot != null ? "; the last look: " + _warnWhyNot : "");
        }
    }
}
