// A crowd makes a ring round a street fight (the NPC-life run, step 5, the fight ring; Lex, 2026-10-30: crowds joining in,
// one thing drawing the next). While people trade blows, those within [Mind] BrawlCrowdMetres (40; 20 fixed until
// 2026-10-31, Lex: "a crowd that gathers") who aren't of a fearful nature (the mind's anxiety), free and on their feet,
// walk over and stand round it, facing in, and cheer it on: up to 6 round two men, about 4.5 m out; up to 16 round a
// brawl (three or more fighting), a second ring 7 m out, and in a brawl the sat and the leaning get up to come and see. A
// busy street fills it to the cap, a few a look. Close enough that a friend of one may yet join in (WestworldScript.JoinIn.cs).
// The ring is the fight's (its own number), so it stays when a downed leader is replaced. When the fight is over they go
// back to their own ways. Behind [Scenes] FightCrowds and MindLive.
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
        private const int RingMostPair = 6, RingMostBrawl = 16, RingInner = 6, RingPerLook = 4;
        private const float RingRadius = 4.5f, RingOuterRadius = 7f;
        private float RingFrom { get { return _cfg.BrawlCrowdMetres; } }
        private string _ringKey;
        private readonly List<int> _ring = new List<int>();
        private readonly List<int[]> _ringRising = new List<int[]>();   // { handle, when to walk over, spot index, since }: getting up off a bench or the bar first
        private int _ringNextCheer, _ringNextLook, _rings, _ringBiggest;
        private string _crowdMood, _crowdKey, _crowdFor;   // how this fight's crowd takes it (FIVEDAY2 R10)
        private int _crowdDeadline, _crowdByModel, _crowdByRules;

        private void RingStep(int now)
        {
            List<int> a = null, b = null;
            bool on = _cfg.FightCrowds && _cfg.MindLive && _director.FightSides(out a, out b);
            if (!on)
            {
                if (_ring.Count > 0)
                    RingBreak();
                return;
            }
            string key = _director.FightId != 0 ? "f" + _director.FightId : a[0] + "|" + b[0];   // the fight's, not its leaders': a downed leader replaced keeps the ring
            if (key != _ringKey)
            {
                RingBreak();
                _ringKey = key;
                _ringNextLook = now;
            }
            if (!ENTITY.DOES_ENTITY_EXIST(a[0]) || !ENTITY.DOES_ENTITY_EXIST(b[0]))
                return;
            Vector3 mid = (ENTITY.GET_ENTITY_COORDS(a[0], true, true) + ENTITY.GET_ENTITY_COORDS(b[0], true, true)) * 0.5f;
            int fighting = a.Count + b.Count;
            int most = fighting >= 3 ? RingMostBrawl : RingMostPair;
            if (now >= _ringNextLook && _ring.Count < most)
            {
                _ringNextLook = now + 2000;
                RingGather(mid, now, most, fighting >= 3);
            }
            if (_ringRising.Count > 0)
                RingRiseStep(mid, now);
            if (_cfg.CrowdPicks)
                CrowdStep(key, fighting, now);
            if (now >= _ringNextCheer && _ring.Count > 0)
            {
                // a bigger ring cheers more often: one voice every 2.5-5 s round two men, about three times as often round sixteen
                _ringNextCheer = now + Math.Max(800, (2500 + _rng.Next(2500)) * RingMostPair / Math.Max(RingMostPair, _ring.Count));
                int h = _ring[_rng.Next(_ring.Count)];
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !_director.Involves(h) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(mid) < RingOuterRadius + 3f)
                {
                    string tag = _cfg.CrowdPicks && _crowdMood != null ? GroupActs.CrowdVoice(_crowdMood, h + "|" + now / 2000) : _rng.Next(3) == 0 ? "mock" : "cheer";
                    if (tag != null)
                        Speech.SayTag(h, tag, a[0]);
                }
            }
        }

        private void RingGather(Vector3 mid, int now, int most, bool brawl)
        {
            Ped me = Game.Player.Character;
            var taken = new List<Ped>();
            float from = RingFrom;
            foreach (Ped p in Peds.All())
            {
                if (_ring.Count + taken.Count >= most || taken.Count >= RingPerLook)
                    break;
                if (p == null || p.Position.DistanceTo(mid) > from)
                    continue;   // the distance first
                int h = p.Handle;
                if (h == me.Handle || _ring.Contains(h) || p.IsDead || !PED.IS_PED_HUMAN(h) || Natives.IsLaw(p) || PED.IS_PED_IN_COMBAT(h, 0) ||
                    PED.IS_PED_FLEEING(h) || _director.Involves(h) || JoinIsPending(h))
                    continue;
                bool sat = PED.IS_PED_USING_ANY_SCENARIO(h);
                if (sat && !brawl)
                    continue;   // round two men the sat and the leaning stay put; a brawl gets them up
                // the mind's own acts' rules, less "sat" for those a brawl gets up
                string why = sat ? (NotFree(h) || Spawner.IsOurs(h) || Natives.StoryModel(p) || !IsGrownUp(p) || AimedLately(h, now) ? "busy" : MayDirectWhyNot(p)) : MindActWhyNot(p, h);
                if (why != null)
                    continue;
                Traits t = TraitsAt(p, now);
                if (t == null || t.Z[Traits.Anxiety] > 0.4f)
                    continue;   // the mind's: nobody it doesn't know, and the fearful keep away
                taken.Add(p);
            }
            int rose = 0;
            foreach (Ped p in taken)
            {
                int i = _ring.Count;
                _ring.Add(p.Handle);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);   // the fight doesn't send them off: they came to see it
                if (PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                {
                    ScenarioExit.SoftUp(p.Handle, x => TASK.TASK_STAND_STILL(x, 3000));   // up Rockstar's way (the exit flag and the next task, no bare clear: that froze people in the pose; Lex, 2026-11-01, Valentine: Cyrus and Zenas); over once on their feet
                    _ringRising.Add(new[] { p.Handle, now + 1500, i, now });
                    rose++;
                    continue;
                }
                RingWalk(p.Handle, mid, i);
            }
            if (taken.Count > 0)
            {
                if (_ring.Count == taken.Count)
                    _rings++;
                if (_ring.Count > _ringBiggest)
                    _ringBiggest = _ring.Count;
                Log.Write("RING " + taken.Count + " gather round the fight (" + _ring.Count + " in the ring, up to " + most + ", from within " + from.ToString("0") + " m" + (rose > 0 ? "; " + rose + " get up to come" : "") + ")");
            }
        }

        /// Their spot: the first six on the inner ring (60 degrees apart), the rest on the outer (30 apart, offset), facing in.
        private void RingWalk(int h, Vector3 mid, int i)
        {
            int off = Voice.Hash(_ringKey ?? "") % 60;
            double ang = (i < RingInner ? i * 60 + off : (i - RingInner) * 30 + off + 15) * Math.PI / 180.0;
            float r = i < RingInner ? RingRadius : RingOuterRadius;
            Vector3 spot = mid + new Vector3((float)Math.Cos(ang), (float)Math.Sin(ang), 0f) * r;
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, spot.X, spot.Y, spot.Z, 1.3f, 40000, 0.8f, 0, 0f);   // 40 s: from 40 m off they get there (10 s until 2026-10-31)
            TASK.TASK_LOOK_AT_ENTITY(h, _director.CastAt(0) != null ? _director.CastAt(0).Handle : 0, 40000, 0, 51, 0);
        }

        /// Those getting up to come and see: walked over once off the bench or the bar; still on it 6 s on, left where they are.
        private void RingRiseStep(Vector3 mid, int now)
        {
            for (int k = _ringRising.Count - 1; k >= 0; k--)
            {
                int[] r = _ringRising[k];
                if (now < r[1])
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(r[0]) || ENTITY.IS_ENTITY_DEAD(r[0]) || !_ring.Contains(r[0]))
                {
                    _ringRising.RemoveAt(k);
                    continue;
                }
                if (PED.IS_PED_USING_ANY_SCENARIO(r[0]))
                {
                    if (now - r[3] > 4000 && now - r[3] <= 5500)
                        ScenarioExit.Reask(r[0], x => TASK.TASK_STAND_STILL(x, 3000));   // asked again, Rockstar's guarded way (never mid-exit)
                    if (now - r[3] > 6000)
                    {
                        _ringRising.RemoveAt(k);
                        _ring.Remove(r[0]);
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(r[0], false);   // still sat: watches from there, as the game has him
                    }
                    continue;
                }
                _ringRising.RemoveAt(k);
                RingWalk(r[0], mid, r[2]);
            }
        }

        /// Once a fight's crowd is three strong: one decision for all of them, the model's if it's on; the rules' otherwise, or if it's late.
        private void CrowdStep(string fight, int fighting, int now)
        {
            if (fight != _crowdFor && _ring.Count >= 3)
            {
                _crowdFor = fight;
                _crowdMood = null;
                _crowdKey = null;
                _crowdDeadline = now + PickWaitMs;
                if (PicksOn)
                {
                    int clock = CLOCK.GET_CLOCK_HOURS();
                    LanguageRequest r = GroupActs.CrowdRequest(Voice.NowYear, clock, _ring.Count, fighting, _placeTown, _engine.NowIndoors ? _engine.NowPlace : null, clock < 5 || clock >= 20);
                    string key = "crowd|" + fight + "|" + now;
                    if (_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = "the crowd", PersonId = "crowd", Request = r, AskedAtMs = Environment.TickCount }))
                        _crowdKey = key;
                }
            }
            if (fight != _crowdFor || _crowdMood != null)
                return;
            string answer = null;
            long ms = 0;
            bool came = _crowdKey != null && _engine.LanguageAnswers.TakePick(_crowdKey, out answer, out ms);
            if (!came && _crowdKey != null && now < _crowdDeadline && now >= _crowdDeadline - PickWaitMs)
                return;
            int idx = came ? Language.ParsePick(answer, GroupActs.CrowdMoods.Length) : -1;
            int h = CLOCK.GET_CLOCK_HOURS();
            _crowdMood = idx >= 0 ? GroupActs.CrowdMoods[idx] : GroupActs.CrowdRules(fighting, h < 5 || h >= 20, fight);
            if (idx >= 0)
                _crowdByModel++;
            else
                _crowdByRules++;
            Log.Write("RING the crowd " + GroupActs.CrowdWords(_crowdMood) + (idx >= 0 ? " (the model)" : " (the rules)"));
        }

        private void RingBreak()
        {
            foreach (int h in _ring)
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !_director.Involves(h))
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                }
            if (_ring.Count > 0)
                Log.Detail("RING breaks up (" + _ring.Count + ")");
            _ring.Clear();
            _ringRising.Clear();
            _ringKey = null;
            _crowdMood = null;
            _crowdFor = null;
            _crowdKey = null;
        }

        private string RingStatus()
        {
            return !_cfg.FightCrowds ? "off ([Scenes] FightCrowds)" : _crowdByModel + " crowds decided by the model, " + _crowdByRules + " by the rules; " + _rings + " fights drew a ring this session (the biggest " + _ringBiggest + "; from within " + RingFrom.ToString("0") + " m, up to " + RingMostPair + " round two, " + RingMostBrawl + " round a brawl)" + (_ring.Count > 0 ? "; one now (" + _ring.Count + ")" : "");
        }
    }
}
