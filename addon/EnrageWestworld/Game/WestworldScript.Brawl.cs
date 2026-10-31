// A brawl near the player, from its side (Core/Brawl.cs has the choosing; WestworldScript.JoinIn.cs grows it a wave at a
// time). Here: the law that stops it ([Mind] BrawlLaw: a lawman who sees it comes over, shouts to break it up; those who
// stop are let off with a word, whoever keeps swinging gets his fists and the rope, each in turn, a second lawman for a
// riot), the brawl coming at Arthur ([Mind] BrawlPullsYouIn: someone he hit, someone against him, a drunk hothead), and
// the rare gun drawn by a man losing it ([Mind] BrawlDraws with LadderShots: the law answers it with his gun). Those still
// swinging after the law's word, and whoever draws, are fair game (Watch.cs). Every 1.5 s, never per frame. Lex,
// 2026-10-31: "the law will get involved with stopping the brawls, as well as having them cascade into whole town
// brawls if it leads that way".
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
        private const int BrawlEveryMs = 1500;

        private sealed class LawHand
        {
            public int Law, Target, Since, At, Phase;   // phase 0 coming, 1 shouted (the word to those who stopped next), 2 fighting Target, 3 tying Target
            public bool Gun;
            public string Seed;
            public List<int> Stopped = new List<int>(), Queue = new List<int>();
        }

        private readonly List<LawHand> _brawlLaw = new List<LawHand>();
        private readonly Dictionary<int, int> _brawlHoldouts = new Dictionary<int, int>();   // still swinging after the word, or drew: whom they're at (fair game)
        private readonly HashSet<int> _brawlDrawRolled = new HashSet<int>();
        private int _brawlNextAt, _brawlTestUntil, _brawlLawFor, _brawlFightFor, _brawlFightSince, _brawlAtYouN, _brawlLawAfterUntil;
        private int _brawlBreakups, _brawlLetOff, _brawlTied, _brawlAtYou, _brawlDrew, _brawlShots, _brawlNoLaw;
        private bool _brawlFailed;
        private string _brawlLast;

        private void BrawlStep(Ped me, int now)
        {
            if (!_cfg.MindLive || _brawlFailed || now < _brawlNextAt)
                return;
            _brawlNextAt = now + BrawlEveryMs;
            try
            {
                BrawlLawStep(now);
                BrawlHoldoutsPrune();
                List<int> a, b;
                if (MISC.GET_MISSION_FLAG() || !_director.FightSides(out a, out b) || !ENTITY.DOES_ENTITY_EXIST(a[0]))
                    return;
                if (_brawlFightFor != a[0])
                {
                    _brawlFightFor = a[0];
                    _brawlFightSince = now;
                    _brawlAtYouN = 0;
                    _brawlDrawRolled.Clear();
                }
                Vector3 mid = ENTITY.GET_ENTITY_COORDS(a[0], true, true);
                if (BrawlsOn && _cfg.LadderShots && _cfg.BrawlDraws && BrawlDraw(a, b, mid, now))
                    return;
                if (_cfg.BrawlLaw)
                    BrawlLawComes(a, b, mid, now);
                if (BrawlsOn && _cfg.BrawlPullsYouIn)
                    BrawlAtYou(me, a, b, mid, now);
            }
            catch (Exception ex)
            {
                _brawlFailed = true;
                Log.Write("BRAWL the law and the brawl failed once, off for this session: " + ex.Message);
            }
        }

        private bool BrawlTesting(int now) { return now < _brawlTestUntil; }

        // ---- the law comes -----------------------------------------------------------------------------------------

        /// A lawman free, on foot, near enough and able to see it (the Testing row: anyone within 70 m).
        private Ped BrawlFindLaw(Vector3 mid, float metres, bool sight, int seen)
        {
            Ped me = Game.Player.Character;
            Ped best = null;
            float bestD = metres;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null)
                    continue;
                float d = p.Position.DistanceTo(mid);
                if (d >= bestD)
                    continue;   // the distance first (the cost audit)
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || p.IsDead || !Natives.IsLaw(p) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) ||
                    PED.IS_PED_IN_COMBAT(h, me.Handle) || NotFree(h) || _director.Involves(h) || _brawlLaw.Any(x => x.Law == h) ||
                    (_theft != null && _theft.Lawman != null && _theft.Lawman.Handle == h))
                    continue;
                if (sight && seen != 0 && !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(h, seen, 17))
                    continue;
                best = p;
                bestD = d;
            }
            return best;
        }

        private void BrawlLawComes(List<int> a, List<int> b, Vector3 mid, int now)
        {
            if (_brawlLawFor == a[0] || _brawlLaw.Any(x => x.Phase == 0))
                return;   // this fight has had its lawman (or one is on his way)
            bool test = BrawlTesting(now);
            if (test && a.Count + b.Count < 5 && now - _brawlFightSince < 15000)
                return;   // the Testing row lets it grow first, so both halves are seen
            Ped law = BrawlFindLaw(mid, test ? 70f : 35f, !test, a[0]);
            if (law == null)
            {
                if (test && now - _brawlFightSince > 20000 && _brawlLawFor != -a[0])
                {
                    _brawlLawFor = -a[0];   // said once
                    _brawlNoLaw++;
                    Log.Write("LAW breaks up: no lawman free within 70 m; the brawl runs its course (a test)");
                }
                return;
            }
            _brawlLawFor = a[0];
            var lh = new LawHand { Law = law.Handle, Since = now, Phase = 0, Seed = a[0] + "|" + now / 1000 };
            _brawlLaw.Add(lh);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(lh.Law, true);
            TASK.CLEAR_PED_TASKS(lh.Law, true, false);
            TASK.TASK_GO_TO_ENTITY(lh.Law, a[0], -1, 4f, 2f, 0f, 0);   // at a run
            Log.Write("LAW breaks up: a lawman " + (int)law.Position.DistanceTo(mid) + " m off sees the fight (" + (a.Count + b.Count) + " fighting) and comes over" + (test ? ", a test" : ""));
        }

        /// The word: who stops and who keeps swinging, from their minds; the fight is ended, and those who won't stop go on.
        private void BrawlBreakUp(LawHand lh, int now)
        {
            List<int> a, b;
            Ped law = new Ped(lh.Law);
            if (!_director.FightSides(out a, out b))
            {
                lh.Phase = 9;   // over before he got there: the aftermath's word does
                return;
            }
            var all = a.Concat(b).Where(h => ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h)).ToList();
            var targets = all.ToDictionary(h => h, h => _director.TargetOf(h));
            var holdouts = new List<int>();
            var said = new List<string>();
            foreach (int h in all)
            {
                MindWho w = MindKnown(h, now);
                Traits t = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
                bool drunk = w != null && InHisCups(new Ped(h), w.P);
                int chance;
                bool stops = Brawl.Heeds(t, drunk, BrawlHealth(h) >= 0.75f, lh.Seed + "|" + (w != null ? w.Id : h.ToString()), out chance);
                string name = w != null && w.Name != null ? w.Name : "a stranger";
                said.Add(name + (stops ? " stops" : " keeps swinging") + " (" + chance + "%" + (drunk ? ", drunk" : "") + ")");
                if (stops)
                    lh.Stopped.Add(h);
                else
                    holdouts.Add(h);
            }
            _brawlBreakups++;
            _brawlLawAfterUntil = now + 60000;   // the aftermath's own "a word with the winner" stands aside: this was the word
            LawSay(law, "The lawman", Brawl.LawBreaksUp(lh.Seed), "SHOUT_STOP");
            _director.Abort("the law broke it up");
            foreach (int h in lh.Stopped)
            {
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, lh.Law, 2500, 0f, 0f, 0f);
                WanderLater(h, now + 7000);
            }
            if (lh.Stopped.Count > 0)
            {
                Ped first = new Ped(lh.Stopped[0]);
                string line, sw;
                int take;
                Speech.PlayFirst(first.Handle, new[] { "GENERIC_SORRY", "GENERIC_YES", "GENERIC_SHOCKED_MED" }, lh.Law, out line, out take, out sw);
                if (_cfg.ScenesOnTicker && _cfg.Ticker && first.Position.DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                    SpeechCaption(MindNameOf(first.Handle) ?? "A man", Brawl.Stopping(lh.Seed), first.Handle, 2500);
            }
            // those who won't stop go back at whoever they were at (or at the lawman, with nobody left to hit)
            foreach (int h in holdouts)
            {
                int t;
                targets.TryGetValue(h, out t);
                if (t == 0 || !ENTITY.DOES_ENTITY_EXIST(t) || ENTITY.IS_ENTITY_DEAD(t) || lh.Stopped.Contains(t))
                    t = lh.Law;
                BrawlFists(h);
                TASK.TASK_COMBAT_PED(h, t, 0, 16);
                _brawlHoldouts[h] = t;
            }
            lh.Queue.AddRange(holdouts);
            // a riot: a second lawman takes half of them, if one is free near
            if (holdouts.Count >= 3)
            {
                Ped second = BrawlFindLaw(law.Position, 45f, false, 0);
                if (second != null)
                {
                    var lh2 = new LawHand { Law = second.Handle, Since = now, Phase = 1, At = now + 1500, Seed = lh.Seed + "|2" };
                    for (int i = lh.Queue.Count - 1; i >= 0; i -= 2)
                    {
                        lh2.Queue.Add(lh.Queue[i]);
                        lh.Queue.RemoveAt(i);
                    }
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(lh2.Law, true);
                    _brawlLaw.Add(lh2);
                    Log.Write("LAW breaks up: a second lawman comes to help with " + lh2.Queue.Count);
                }
            }
            lh.Phase = 1;
            lh.At = now + 2500;
            _brawlLast = said.Count + " fighting: " + string.Join("; ", said);
            Log.Write("LAW breaks up: the lawman shouts; " + _brawlLast);
        }

        private void BrawlFists(int h)
        {
            WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);   // fists for fists
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
        }

        /// Each lawman's work, a step at a time: on his way, the word, each holdout fought and tied, then back to his rounds.
        private void BrawlLawStep(int now)
        {
            for (int i = _brawlLaw.Count - 1; i >= 0; i--)
            {
                LawHand lh = _brawlLaw[i];
                if (!ENTITY.DOES_ENTITY_EXIST(lh.Law) || ENTITY.IS_ENTITY_DEAD(lh.Law) || now - lh.Since > 120000)
                {
                    if (ENTITY.DOES_ENTITY_EXIST(lh.Law) && !ENTITY.IS_ENTITY_DEAD(lh.Law))
                    {
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(lh.Law, false);   // two minutes at it: back to his rounds
                        WanderLater(lh.Law, now + 2000);
                    }
                    _brawlLaw.RemoveAt(i);
                    continue;
                }
                Ped law = new Ped(lh.Law);
                switch (lh.Phase)
                {
                    case 0:
                        {
                            List<int> a, b;
                            if (!_director.FightSides(out a, out b))
                            {
                                lh.Phase = 9;   // it ended before he got there
                                break;
                            }
                            if (ENTITY.DOES_ENTITY_EXIST(a[0]) && law.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(a[0], true, true)) < 9f || now - lh.Since > 9000)
                                BrawlBreakUp(lh, now);
                            break;
                        }
                    case 1:
                        if (now < lh.At)
                            break;
                        if (lh.Stopped.Count > 0)
                        {
                            LawSay(law, "The lawman", Brawl.LawLetsOff(lh.Seed), "GENERIC_CURSE_MED");
                            _brawlLetOff += lh.Stopped.Count;
                        }
                        lh.Phase = 3;   // straight on to the first holdout (phase 3 with nobody tied takes the next)
                        lh.Target = 0;
                        lh.At = now;
                        break;
                    case 2:
                        {
                            int t = lh.Target;
                            if (t == 0 || !ENTITY.DOES_ENTITY_EXIST(t) || ENTITY.IS_ENTITY_DEAD(t))
                            {
                                lh.Phase = 3;
                                lh.Target = 0;
                                lh.At = now;
                                break;
                            }
                            bool down = BrawlHealth(t) < 0.5f || PED.IS_PED_RAGDOLL(t) || now - lh.Since > 15000;
                            if (!down || law.Position.DistanceTo(new Ped(t).Position) > 4f)
                            {
                                if (now - lh.Since > 25000)
                                {
                                    lh.Phase = 3;   // he couldn't get hold of him: on to the next
                                    lh.Target = 0;
                                    lh.At = now;
                                }
                                break;
                            }
                            PED.SET_PED_TO_RAGDOLL(t, 1500, 2500, 0, false, false, "");
                            TASK.CLEAR_PED_TASKS(lh.Law, true, false);
                            TASK.TASK_HOGTIE_TARGET_PED(lh.Law, t);
                            _brawlHoldouts.Remove(t);
                            _brawlTied++;
                            LawSay(law, "The lawman", Brawl.LawTakes(lh.Seed + "|" + t), "WON_FIGHT");
                            Log.Write("LAW breaks up: the lawman hogties " + (MindNameOf(t) ?? "a man who wouldn't stop"));
                            lh.Phase = 3;
                            lh.At = now + 6000;   // the rope takes a while
                            break;
                        }
                    case 3:
                        {
                            if (now < lh.At)
                                break;
                            int next = 0;
                            while (lh.Queue.Count > 0 && next == 0)
                            {
                                int h = lh.Queue[0];
                                lh.Queue.RemoveAt(0);
                                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && (PED.IS_PED_IN_COMBAT(h, 0) || _brawlHoldouts.ContainsKey(h)))
                                    next = h;
                            }
                            if (next == 0)
                            {
                                lh.Phase = 9;
                                break;
                            }
                            lh.Target = next;
                            lh.Since = now;
                            lh.Phase = 2;
                            TASK.CLEAR_PED_TASKS(lh.Law, true, false);
                            if (lh.Gun)
                            {
                                uint gun = WEAPON.GET_BEST_PED_WEAPON(lh.Law, false, false);
                                if (gun != 0)
                                    WEAPON.SET_CURRENT_PED_WEAPON(lh.Law, gun, true, 0, false, false);
                                PED.SET_PED_COMBAT_ATTRIBUTES(lh.Law, 5, false);
                            }
                            else
                                BrawlFists(lh.Law);
                            TASK.TASK_COMBAT_PED(lh.Law, next, 0, 16);
                            Log.Write("LAW breaks up: the lawman goes for " + (MindNameOf(next) ?? "a man") + (lh.Gun ? ", his gun out (a gun was drawn)" : ", with his fists"));
                            break;
                        }
                }
                if (lh.Phase == 9)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(lh.Law, false);
                    if (!PED.IS_PED_IN_COMBAT(lh.Law, Game.Player.Character.Handle))
                        WanderLater(lh.Law, now + 15000);   // back to his rounds once the rope is done
                    _brawlLaw.RemoveAt(i);
                }
            }
        }

        /// No longer fair game once they've stopped fighting and nobody is after them, or are gone.
        private void BrawlHoldoutsPrune()
        {
            if (_brawlHoldouts.Count == 0)
                return;
            foreach (int h in _brawlHoldouts.Keys.ToList())
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || (!PED.IS_PED_IN_COMBAT(h, 0) && !_brawlLaw.Any(x => x.Target == h || x.Queue.Contains(h))))
                    _brawlHoldouts.Remove(h);
        }

        private static float BrawlHealth(int h)
        {
            int max = ENTITY.GET_ENTITY_MAX_HEALTH(h, false);
            return max > 0 ? ENTITY.GET_ENTITY_HEALTH(h) / (float)max : 1f;
        }

        // ---- the brawl comes at Arthur -----------------------------------------------------------------------------

        private void BrawlAtYou(Ped me, List<int> a, List<int> b, Vector3 mid, int now)
        {
            int fighting = a.Count + b.Count;
            if (fighting < 4 || _brawlAtYouN >= 3 || me.IsDead || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) ||
                PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle) || me.Position.DistanceTo(mid) > 10f)
                return;
            bool saloon = _placeKind == "saloon";
            foreach (int h in a.Concat(b))
            {
                if (h == a[0] || (b.Count > 0 && h == b[0]) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _director.TargetOf(h) == me.Handle)
                    continue;   // the two who began it stay at each other (going for him would end the scene); a joiner may turn
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > 6f)
                    continue;
                MindWho w = MindKnown(h, now);
                if (w == null || w.Id == null || w.Src == null)
                    continue;
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                string regard = SightRegard(p, w.Id), why;
                bool hit = ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(h, me.Handle, true, true);
                if (!Brawl.AtYou(t, InHisCups(p, w.P), regard == "hostile", regard == "afraid", hit, fighting, saloon, _brawlFightFor + "|" + w.Id + "|" + _brawlAtYouN + "|" + now / 6000, out why))
                    continue;
                if (!_director.Retarget(h, me.Handle))
                    continue;
                _brawlAtYouN++;
                _brawlAtYou++;
                string line, sw;
                int take;
                Speech.PlayFirst(h, new[] { "GENERIC_ANGRY_SHOUT", "GENERIC_CURSE_HIGH", "GENERIC_CURSE_MED" }, me.Handle, out line, out take, out sw);
                SpeechCaption(w.Name ?? "A man", Brawl.ComesAtYou(w.Id + "|" + now / 1000), h, 2500, true);
                Log.Write("BRAWL comes at you: " + (w.Name ?? "a man") + " (" + why + "; " + fighting + " fighting)");
                return;   // one a look
            }
        }

        // ---- a gun drawn ----------------------------------------------------------------------------------------------

        private bool BrawlDraw(List<int> a, List<int> b, Vector3 mid, int now)
        {
            int witnesses = -1;
            bool lawNear = false;
            foreach (int h in a.Concat(b))
            {
                if (_brawlDrawRolled.Contains(h) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || BrawlHealth(h) >= 0.55f)
                    continue;
                _brawlDrawRolled.Add(h);   // once a brawl, when he first finds himself losing
                int target = _director.TargetOf(h);
                MindWho w = MindKnown(h, now);
                if (target == 0 || w == null || w.Id == null || w.Src == null || target == Game.Player.Character.Handle)
                    continue;
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                if (witnesses < 0)
                {
                    witnesses = 0;   // looked at only once someone is losing (the cost)
                    lawNear = BrawlFindLaw(mid, 30f, false, 0) != null;
                    foreach (Ped p in World.GetAllPeds())
                        if (p != null && p.Position.DistanceTo(mid) < 15f)
                            witnesses++;
                }
                uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false), theirs = WEAPON.GET_BEST_PED_WEAPON(target, false, false);
                uint fists = Natives.Hash("WEAPON_UNARMED");
                MindWho tw = MindKnown(target, now);
                int warmth = tw != null && tw.Id != null ? LadderWarmth(w.Id, tw.Id) : 0;
                var c = new QuarrelContext
                {
                    Place = _placeKind == "saloon" ? "saloon" : "street", Hour = (int)(_store.LastGameMin % 1440 / 60), DrunkA = InHisCups(new Ped(h), w.P),
                    ArmedA = gun != 0 && gun != fists, ArmedB = theirs != 0 && theirs != fists, WomanA = !PED.IS_PED_MALE(h), WomanB = !PED.IS_PED_MALE(target),
                    WomenFight = _cfg.WomenInFights, LawNear = lawNear, Witnesses = witnesses, Warmth = warmth
                };
                int pct;
                string draws = Brawl.Draws(t, c, true, warmth, _brawlFightFor + "|" + w.Id, out pct);
                Log.Detail("BRAWL " + w.Name + " losing: " + (draws == "none" ? "keeps to his fists" : draws) + (pct > 0 ? " (" + pct + "% to draw)" : ""));
                if (draws == "none")
                    continue;
                _director.Abort("a gun drawn in the brawl");
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                WEAPON.SET_CURRENT_PED_WEAPON(h, gun, true, 0, false, false);
                PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, false);
                string name = w.Name ?? "a man", at = MindNameOf(target) ?? "the man beating him";
                if (draws == "shoot")
                {
                    TASK.TASK_COMBAT_PED(h, target, 0, 16);
                    if (c.ArmedB)
                    {
                        WEAPON.SET_CURRENT_PED_WEAPON(target, theirs, true, 0, false, false);
                        TASK.TASK_COMBAT_PED(target, h, 0, 16);
                    }
                    else
                        TASK.TASK_SMART_FLEE_PED(target, h, 80f, -1, 0, 3f, 0);
                    _brawlShots++;
                    WanderLater(h, now + 15000);
                }
                else
                {
                    TASK.TASK_AIM_GUN_AT_ENTITY(h, target, 6000, false, 0);
                    TASK.TASK_SMART_FLEE_PED(target, h, 40f, 8000, 0, 3f, 0);
                    WanderLater(h, now + 9000);
                }
                _brawlDrew++;
                _brawlHoldouts[h] = target;   // a gun drawn on a man: stopping him is no crime
                _brawlLast = name + " drew on " + at + (draws == "shoot" ? " and fired" : "");
                Log.Write("BRAWL draws: " + name + ", losing, pulls his gun on " + at + (draws == "shoot" ? " and fires" : " and holds it on him") + " (" + pct + "% to draw" + (c.DrunkA ? ", drunk" : "") + ")");
                if (_cfg.BrawlLaw)
                {
                    Ped law = BrawlFindLaw(mid, 50f, false, 0);   // a shot is heard, not seen
                    if (law != null)
                    {
                        var lh = new LawHand { Law = law.Handle, Since = now, Phase = 3, At = now, Gun = true, Seed = h + "|gun" };
                        lh.Queue.Add(h);
                        _brawlLawFor = a[0];
                        _brawlLaw.Add(lh);
                        LawSay(law, "The lawman", Brawl.LawBreaksUp(lh.Seed), "SHOUT_STOP");
                    }
                }
                return true;
            }
            return false;
        }

        // ---- the Testing row and F7 ----------------------------------------------------------------------------------

        private string BrawlTest(int now, string seed, out string said)
        {
            said = null;
            if (!_cfg.MindLive)
                return "[Mind] MindLive is off";
            if (!_cfg.MindJoinFights || !_cfg.MindBrawls)
                return "[Mind] MindJoinFights or MindBrawls is off";
            string why = FireScene("brawl", now, seed, out said);
            if (why == null)
            {
                _brawlTestUntil = now + 90000;   // for a minute and a half, joining skips the bar and the roll; the law comes once five fight
                said = "A fistfight; the crowd joins in waves, then the law steps in";
            }
            return why;
        }

        private string BrawlStatus()
        {
            if (!_cfg.MindLive)
                return "off (MindLive)";
            if (_brawlFailed)
                return "FAILED once this session (see the log)";
            return (BrawlsOn ? "growing on (cap " + _cfg.BrawlMost + ", the biggest " + _brawlBiggest + ")" : "growing off ([Mind] MindBrawls)") +
                   "; the law " + (_cfg.BrawlLaw ? "on: " + _brawlBreakups + " broken up, " + _brawlLetOff + " let off, " + _brawlTied + " hogtied" + (_brawlNoLaw > 0 ? ", " + _brawlNoLaw + " with no lawman" : "") : "off") +
                   "; at you " + (_cfg.BrawlPullsYouIn ? _brawlAtYou.ToString() : "off") + "; drawn " + (_cfg.BrawlDraws && _cfg.LadderShots ? _brawlDrew + " (" + _brawlShots + " fired)" : "off") +
                   (_brawlLaw.Count > 0 ? "; " + _brawlLaw.Count + " lawmen at work now" : "") + (_brawlLast != null ? "; last: " + _brawlLast : "");
        }
    }
}
