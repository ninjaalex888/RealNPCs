// Called out before the street ([Encounters] PublicCallouts; Core/StreetCallouts.cs, re-staged from the game's
// beat_town_confrontation). By day in a town, out of doors and away from the noon callout's hour: two of the people
// already about, named and on their feet, 4-14 m apart, one of them in your view, with something real between them
// (a tie gone sour, or the one's debt to the other): the one with the grievance calls the other over and says it out
// loud. Then each in turn chooses by their own mind, or the model among the same few options: the one called out lets
// it go by, owns up, gives it back or squares up; the first takes it, plays it to the onlookers, mocks him, warns him
// or walks off; up to two named people near watch, jeer, step in or walk off. Two who square up are handed to the
// street's quarrel (its ladder keeps the fists as they are). Walk up within 3 m and both turn to you: the caller drops
// it, warmly or with a word for you to mind your own, and it's done; draw on him and he raises his hands. The
// tie between them moves (an apology taken warms it, the rest cools it), and the town's log has it.
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
        private sealed class StreetCallout
        {
            public string Phase = "call";   // call, close, confront, answer, reply, end
            public Ped A, B;
            public Someone WA, WB;
            public StagedMind MA, MB;
            public string Matter, Seed, Town, AName, BName, DictA, DictB;
            public readonly List<Ped> On = new List<Ped>();
            public readonly List<Someone> OnWho = new List<Someone>();
            public readonly List<StagedAsk> OnAsk = new List<StagedAsk>();
            public StagedAsk TAsk, AAsk;
            public int StartedAt, NextAt;
            public bool Test, YouIn;
            public string Outcome;
        }

        private StreetCallout _streetCallout;
        private int _scStarted, _scHandedOn, _scYouIn, _scModel, _scMind, _scNextOk;
        private readonly Dictionary<string, int> _scChoices = new Dictionary<string, int>();
        private string _scLast, _scWhyNot;

        /// Why there can't be one here and now; null when there can.
        private string StreetCalloutNotNow(bool test = false)
        {
            if (_placeTown == null || _interiorType != 0)
                return "not out in a town's street";
            if (test)
                return null;
            int clock = CLOCK.GET_CLOCK_HOURS() * 60 + CLOCK.GET_CLOCK_MINUTES();
            if (clock < 8 * 60 || clock >= 20 * 60)
                return "it's " + (clock / 60).ToString("00") + ":00 (from 08:00 to 20:00)";
            if (clock >= 11 * 60 + 20 && clock <= 12 * 60 + 40)
                return "the noon callout's hour (11:20-12:40)";
            if (_now < _scNextOk)
                return "there was one a little while ago";
            return null;
        }

        private bool StartStreetCallout(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string not = StreetCalloutNotNow(test);
            if (not != null)
            {
                _scWhyNot = not;
                if (test)
                    Hint("Not now: " + not + ".", 5000);
                return false;
            }
            List<Candidate> pool = ReasonCandidates(me, now, test).Where(x => !PED.IS_PED_USING_ANY_SCENARIO(x.Ped.Handle) && !IsMindActing(x.Ped.Handle) && MindActWhyNot(x.Ped, x.Ped.Handle) == null).ToList();
            long g = _store.LastGameMin;
            Candidate ca = null, cb = null;
            SceneTie tie = null;
            bool debt = false, wronged = false;
            float best = float.MaxValue;
            for (int i = 0; i < pool.Count; i++)
                for (int j = 0; j < pool.Count; j++)
                {
                    if (i == j || pool[i].Who.Id == pool[j].Who.Id)
                        continue;
                    Candidate a = pool[i], b = pool[j];
                    float apart = a.Ped.Position.DistanceTo(b.Ped.Position);
                    if (apart < (test ? 2f : 4f) || apart > (test ? 18f : 14f) || (!NodInView(me, a.Ped) && !NodInView(me, b.Ped)))
                        continue;
                    SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(a.Who.Id) && x.Has(b.Who.Id));
                    WorryLink link = _store.Links.LastOrDefault(l => l.Id == b.Who.Id && l.Kind == "debt" && l.OtherId == a.Who.Id);   // b worries over what he owes a (the link is the worrier's; the one at the other end is owed): a is the one wronged, and calls it in
                    bool sour = t != null && t.Warmth < 0;
                    if (!sour && link == null)
                        continue;
                    bool aWronged = link != null || (t != null && t.Did(b.Who.Id));
                    string matter = StreetCallouts.MatterOf(t != null ? t.Kind : null, t != null && t.Courted, link != null);
                    if (!StreetCallouts.Calls(StagedMindOf(a.Who, b.Who.Id), StagedMindOf(b.Who, a.Who.Id), aWronged, matter))
                        continue;   // (the pair is looked at both ways round)
                    float fromYou = Math.Min(a.Ped.Position.DistanceTo(me.Position), b.Ped.Position.DistanceTo(me.Position));
                    if (fromYou >= best)
                        continue;
                    ca = a;
                    cb = b;
                    tie = t;
                    debt = link != null;
                    wronged = aWronged;
                    best = fromYou;
                }
            if (ca == null)
            {
                _scWhyNot = "nobody about with something between them, the right distance apart (" + pool.Count + " named and free near you)";
                if (test)
                    Hint("Nobody here to do it: two named people with something between them (a quarrel, a beating, a debt), 4-14 m apart. " + pool.Count + " named and free near you.", 7000);
                return false;
            }
            var c = new StreetCallout
            {
                A = ca.Ped, B = cb.Ped, WA = ca.Who, WB = cb.Who, Town = _placeTown, StartedAt = now, NextAt = now + 300, Test = test,
                Matter = StreetCallouts.MatterOf(tie != null ? tie.Kind : null, tie != null && tie.Courted, debt),
                Seed = ca.Who.Id + "|" + cb.Who.Id + "|" + g / 60
            };
            c.MA = StagedMindOf(ca.Who, cb.Who.Id);
            c.MB = StagedMindOf(cb.Who, ca.Who.Id);
            c.AName = Person.FirstOf(ca.Who.Name);
            c.BName = Person.FirstOf(cb.Who.Name);
            c.DictA = StreetCallouts.DictFor(ca.Who.Woman, ca.Who.Temper);
            c.DictB = StreetCallouts.DictFor(cb.Who.Woman, cb.Who.Temper);
            Vector3 mid = (ca.Ped.Position + cb.Ped.Position) * 0.5f;
            foreach (Candidate o in pool.Where(x => x != ca && x != cb && x.Who.Id != ca.Who.Id && x.Who.Id != cb.Who.Id && x.Ped.Position.DistanceTo(mid) < 12f).OrderBy(x => x.Ped.Position.DistanceTo(mid)).Take(2))
            {
                c.On.Add(o.Ped);
                c.OnWho.Add(o.Who);
            }
            StagedLoad(StreetCallouts.Dicts, now);
            StagedHold(c.A);
            StagedHold(c.B);
            foreach (Ped o in c.On)
                StagedHold(o);
            _streetCallout = c;
            _scStarted++;
            if (!test)
                _scNextOk = now + 6 * 60000;
            Log.Write("ENCOUNTER callout: " + ca.Who.Name + " calls out " + cb.Who.Name + " over " + c.Matter + " in " + _placeTown + " (" + (wronged ? "the one wronged" : "the hotter head") + ", " +
                      (tie != null ? "their tie " + tie.Kind + " " + tie.Warmth : "no tie") + (debt ? ", a debt" : "") + "; " + (int)ca.Ped.Position.DistanceTo(cb.Ped.Position) + " m apart, " + (int)best + " m from you; " +
                      c.On.Count + " named near to see it)" + (test ? ", a test" : ""));
            return true;
        }

        private void StreetCalloutStep(int now)
        {
            StreetCallout c = _streetCallout;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndStreetCallout(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died", false);
                return;
            }
            string broke = ScBroken(c.A) ?? ScBroken(c.B);
            if (broke != null)
            {
                EndStreetCallout("one of them " + broke, false);
                return;
            }
            if (now - c.StartedAt > 90000)
            {
                EndStreetCallout("it ran on too long", true);
                return;
            }
            int ha = c.A.Handle, hb = c.B.Handle;
            // you step in: right up to them, or a gun on the one who called it
            if (!c.YouIn && c.Phase != "call" && c.Phase != "end")
            {
                bool aimed = PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, ha) || PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, hb);
                if (aimed || me.Position.DistanceTo(c.A.Position) < 3f || me.Position.DistanceTo(c.B.Position) < 3f)
                {
                    ScYouStepIn(c, aimed, now);
                    return;
                }
            }
            if (now < c.NextAt)
                return;
            string sa = c.Seed + "|" + c.Phase;
            switch (c.Phase)
            {
                case "call":
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(ha, hb, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(ha, hb, 30000, 0, 51, 0);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(hb, ha, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(hb, ha, 30000, 0, 51, 0);
                    c.NextAt = now + 900;
                    c.Phase = "callover";
                    break;
                case "callover":
                    StagedClip(ha, c.DictA, StreetCallouts.CallClip, "angry", c.WA.Woman, sa, now);
                    Feed.Say(c.WA.Name, StreetCallouts.CallOver(c.BName, c.Matter, c.Seed), Speech.When(ha, "angry", hb), ha);
                    foreach (Ped o in c.On)
                        if (o.Exists())
                            TASK.TASK_LOOK_AT_ENTITY(o.Handle, ha, 30000, 0, 51, 0);
                    if (c.A.Position.DistanceTo(c.B.Position) > 3.2f)
                        TASK.TASK_GO_TO_ENTITY(ha, hb, 9000, Natives.Brisk, 2.2f, 0f, 0);   // he crosses to him to say it to his face
                    c.Phase = "close";
                    c.NextAt = now + 2500;
                    break;
                case "close":
                    if (c.A.Position.DistanceTo(c.B.Position) > 3.4f && now - c.StartedAt < 14000)
                    {
                        c.NextAt = now + 250;
                        return;
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(ha, hb, 2000, 0f, 0f, 0f);
                    StagedClip(ha, c.DictA, StreetCallouts.ConfrontClip, "angry", c.WA.Woman, sa, now);
                    Feed.Say(c.WA.Name, StreetCallouts.Confront(c.BName, c.Matter, c.Seed), Speech.When(ha, "angry", hb), ha);
                    _ticker.Push(c.WB.Name + ", thinking", StreetCallouts.Thought("target", c.AName, c.Matter, c.Seed), "near", hb);
                    c.TAsk = StagedChoose(hb, "target", StreetCallouts.Target(c.MB, c.Matter), sa + "t",
                                          id => StreetCallouts.Words("target", id, c.AName, c.BName, c.Matter), StreetCallouts.Now("target", c.AName, c.BName, c.Matter), c.AName, now);
                    c.Phase = "answer";
                    c.NextAt = now + 3200;
                    break;
                case "answer":
                    if (!StagedSettle(c.TAsk, "callout, " + c.WB.Name, now))
                    {
                        c.NextAt = now + 250;
                        return;
                    }
                    string ans = c.TAsk.Picked;
                    ScCount("answer " + ans, c.TAsk);
                    Log.Write("ENCOUNTER callout: " + c.WB.Name + " chose " + ans + " (" + By(c.TAsk) + ")");
                    StagedClip(hb, c.DictB, StreetCallouts.ClipOf(ans), StreetCallouts.ToneOf(ans), c.WB.Woman, sa, now);
                    Feed.Say(c.WB.Name, StreetCallouts.Answer(ans, c.AName, c.Matter, c.Seed), Speech.When(hb, StreetCallouts.ToneOf(ans), ha), hb);
                    c.AAsk = StagedChoose(ha, "accuser", StreetCallouts.Accuser(c.MA, ans), sa + "a",
                                          id => StreetCallouts.Words("accuser", id, c.AName, c.BName, c.Matter), StreetCallouts.Now("accuser", c.AName, c.BName, c.Matter) + ", and " + c.BName + " " + StreetCallouts.Words("target", ans, c.AName, c.BName, c.Matter), c.BName, now);
                    for (int i = 0; i < c.On.Count; i++)
                    {
                        Someone ow = c.OnWho[i];
                        SceneTie ta = _store.Ties.FirstOrDefault(x => x.Has(ow.Id) && x.Has(c.WA.Id)), tb = _store.Ties.FirstOrDefault(x => x.Has(ow.Id) && x.Has(c.WB.Id));
                        c.OnAsk.Add(StagedChoose(c.On[i].Handle, "onlooker", StreetCallouts.Onlooker(StagedMindOf(ow, null), ta != null ? ta.Warmth : 0, tb != null ? tb.Warmth : 0), sa + "o" + i,
                                                 id => StreetCallouts.Words("onlooker", id, c.AName, c.BName, c.Matter), StreetCallouts.Now("onlooker", c.AName, c.BName, c.Matter), c.AName, now));
                    }
                    c.Phase = "reply";
                    c.NextAt = now + 3200;
                    break;
                case "reply":
                    if (!StagedSettle(c.AAsk, "callout, " + c.WA.Name, now) || c.OnAsk.Any(x => !StagedSettle(x, "callout, onlooker", now)))
                    {
                        c.NextAt = now + 250;
                        return;
                    }
                    string rep = c.AAsk.Picked;
                    ScCount("reply " + rep, c.AAsk);
                    Log.Write("ENCOUNTER callout: " + c.WA.Name + " chose " + rep + " (" + By(c.AAsk) + ")");
                    if (rep == "to_crowd" && c.On.Count > 0 && c.On[0].Exists())
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(ha, c.On[0].Handle, 1500, 0f, 0f, 0f);
                    StagedClip(ha, c.DictA, StreetCallouts.ClipOf(rep), StreetCallouts.ToneOf(rep), c.WA.Woman, sa, now);
                    Feed.Say(c.WA.Name, StreetCallouts.Reply(rep, c.BName, c.Seed), Speech.When(ha, StreetCallouts.ToneOf(rep), hb), ha);
                    for (int i = 0; i < c.On.Count; i++)
                        ScOnlooker(c, i, now);
                    c.Phase = "end";
                    c.NextAt = now + 3500;
                    break;
                case "end":
                    ScFinish(c, now);
                    break;
            }
        }

        /// One of the two can't go on: gone, dead, or taken by the game or another of ours.
        private string ScBroken(Ped p)
        {
            if (p == null || !p.Exists())
                return "is gone";
            if (p.IsDead)
                return "is dead";
            int h = p.Handle;
            if (PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h))
                return "is fighting or running";
            if (_director.Involves(h))
                return "was taken by a scene";
            return null;
        }

        private void ScOnlooker(StreetCallout c, int i, int now)
        {
            Ped o = c.On[i];
            StagedAsk a = c.OnAsk[i];
            if (!o.Exists() || o.IsDead)
                return;
            int h = o.Handle;
            string pick = a.Picked;
            ScCount("onlooker " + pick, a);
            Log.Write("ENCOUNTER callout: onlooker " + c.OnWho[i].Name + " " + (pick == "stepin" ? "steps in" : pick == "jeer" ? "jeers" : pick == "watch" ? "watches" : "walks off") + " (" + By(a) + ")");
            string line = StreetCallouts.FromOnlooker(pick, c.Seed + i);
            switch (pick)
            {
                case "stepin":
                    TASK.TASK_GO_TO_ENTITY(h, c.B.Handle, 6000, Natives.Brisk, 1.6f, 0f, 0);
                    Feed.Say(c.OnWho[i].Name, line, Speech.When(h, "intervene", c.A.Handle), h);
                    break;
                case "jeer":
                    StagedGesture(h, "mock", c.OnWho[i].Woman, c.Seed + i);
                    Feed.Say(c.OnWho[i].Name, line, Speech.When(h, "cheer", c.A.Handle), h);
                    break;
                case "watch":
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, c.A.Handle, 2000, 0f, 0f, 0f);
                    if (i == 0)
                        _ticker.Push(c.OnWho[i].Name + ", thinking", StreetCallouts.Thought("onlooker", c.AName, c.Matter, c.Seed + i), "near", h);
                    break;
                default:
                    StagedLetGo(o, true);
                    break;
            }
        }

        /// You walked right up, or drew on one of them: both turn to you, and the caller drops it, kindly if he thinks well
        /// of you, sharply if not; drawn on, he raises his hands. It's done either way.
        private void ScYouStepIn(StreetCallout c, bool aimed, int now)
        {
            Ped me = Game.Player.Character;
            c.YouIn = true;
            _scYouIn++;
            int ha = c.A.Handle, hb = c.B.Handle;
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(hb, me.Handle, 2000, 0f, 0f, 0f);
            if (aimed)
            {
                TASK.TASK_HANDS_UP(ha, 5000, me.Handle, -1, false);
                Feed.Say(c.WA.Name, StreetCallouts.ToYou(true, c.Seed + "aim"), Speech.When(ha, "scared", me.Handle), ha, toYou: true);
                Log.Write("ENCOUNTER callout: you drew on them; " + c.WA.Name + " gives way");
                c.Outcome = "you drew; he gave way";
            }
            else
            {
                bool good = c.MA.ToYou >= 0 && c.WA.Temper != "Hothead" || c.MA.ToYou > 0;
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(ha, me.Handle, 2000, 0f, 0f, 0f);
                StagedClip(ha, c.DictA, good ? StreetCallouts.ToYouGood : StreetCallouts.ToYouBad, good ? "yes" : "no", c.WA.Woman, c.Seed + "you", now);
                Feed.Say(c.WA.Name, StreetCallouts.ToYou(good, c.Seed + "you"), Speech.When(ha, good ? "defuse" : "warned", me.Handle), ha, toYou: true);
                Log.Write("ENCOUNTER callout: you stepped in; " + c.WA.Name + (good ? " lets it lie" : " tells you it's none of yours, and lets it lie anyway"));
                c.Outcome = good ? "you stepped in; he let it lie" : "you stepped in; he told you off and let it lie";
            }
            c.Phase = "end";
            c.NextAt = now + 3000;
        }

        private void ScFinish(StreetCallout c, int now)
        {
            string ans = c.TAsk != null ? c.TAsk.Picked : null, rep = c.AAsk != null ? c.AAsk.Picked : null;
            bool squared = !c.YouIn && ans == "threat" && (rep == "threat" || rep == "taunt");
            if (squared && !_director.Busy && c.A.Exists() && c.B.Exists())
            {
                // two who square up: the street's quarrel takes them on (its ladder keeps the fists as they are)
                foreach (Ped p in new[] { c.A, c.B })
                {
                    _staged.Remove(p.Handle);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                    PED.SET_PED_KEEP_TASK(p.Handle, false);
                }
                string seed = c.Seed + "|ladder";
                SceneCast cast = BuildCast(c.A, c.B, null, false, Game.Player.Character, now, seed);
                Scene s = Scenes.Make("argue", cast, seed);
                if (StartScene(s, new List<Ped> { c.A, c.B }, cast, seed, now, c.Test) && _director.Busy)
                {
                    _scHandedOn++;
                    c.Outcome = "squared up; handed to the street's quarrel";
                    Log.Write("ENCOUNTER callout: " + c.WA.Name + " and " + c.WB.Name + " squared up: the street's quarrel takes them on");
                    EndStreetCallout(c.Outcome, true, true);
                    return;
                }
                StagedHold(c.A);
                StagedHold(c.B);
            }
            if (c.Outcome == null)
                c.Outcome = ans + ", then " + rep;
            EndStreetCallout(c.Outcome, true);
        }

        private void ScCount(string what, StagedAsk a)
        {
            int n;
            _scChoices[what] = _scChoices.TryGetValue(what, out n) ? n + 1 : 1;
            if (a != null && a.ByModel)
                _scModel++;
            else
                _scMind++;
        }

        private void EndStreetCallout(string why, bool played, bool handedOn = false)
        {
            StreetCallout c = _streetCallout;
            _streetCallout = null;
            if (c == null)
                return;
            if (played && c.WA != null && c.WB != null && c.Phase != "call")
            {
                bool mended = c.TAsk != null && c.TAsk.Picked == "positive" && c.AAsk != null && c.AAsk.Picked == "accept";
                if (!handedOn)
                    Ties.Note(_store.Ties, c.WA.Id, c.WA.Name, c.WB.Id, c.WB.Name, c.Town, mended ? "apology" : "argue", Bridge.Now());
                if (c.A.Exists())
                    Safe("world events", () => RecordEvent("encounter:callout", c.WA.Id, c.WA.Name, c.WB.Id, c.WB.Name, c.A.Position, c.A.Handle, c.A.Handle, c.B.Exists() ? c.B.Handle : 0, why));
            }
            foreach (Ped p in new[] { c.A, c.B }.Concat(c.On))
                if (p != null && !(handedOn && (p == c.A || p == c.B)))
                    StagedLetGo(p, true);
            _scLast = (c.WA != null ? c.WA.Name : "?") + " called out " + (c.WB != null ? c.WB.Name : "?") + " over " + c.Matter + ": " + why + (c.Test ? " (a test)" : "");
            Log.Write("ENCOUNTER callout: over (" + why + ")");
        }

        private string StreetCalloutStatus()
        {
            if (!_cfg.PublicCallouts)
                return "off ([Encounters] PublicCallouts)";
            return _cfg.PublicCalloutChance + "% a roll (08:00-20:00 in a town, not the noon hour); " + _scStarted + " started, " + _scHandedOn + " handed to the street's quarrel, you stepped in " + _scYouIn +
                   "; choices " + (_scChoices.Count == 0 ? "none yet" : string.Join(", ", _scChoices.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   " (the model's " + _scModel + ", the mind's " + _scMind + ")" + (_streetCallout != null ? "; going on: " + _streetCallout.WA.Name + " and " + _streetCallout.WB.Name + " (" + _streetCallout.Phase + ")" : "") +
                   (_scLast != null ? "; last: " + _scLast : "") + (_scWhyNot != null ? "; the last look: " + _scWhyNot : "") + "; " + StagedStatus();
        }
    }
}
