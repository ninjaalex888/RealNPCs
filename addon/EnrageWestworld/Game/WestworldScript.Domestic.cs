// A husband's temper ([Encounters] DomesticDisputes; Core/Domestic.cs, re-staged from the game's beat_domestic_dispute).
// Out of doors in a town, 08:00 to 21:00: a named man of the town, standing
// near you, and his wife. A woman he is courting or sweet on, standing near, plays her; else his wife is brought from
// out of sight and walks up to him (a townswoman of the game's, his wife by name). Then turn by turn, each by their own
// mind or the model among the same few things: he rants at her (the game's own clips for that encounter), raises his
// hand (rare, once at most, a blow that knocks her down), storms off, or lets it drop when he sees he's watched; she
// gives it back, pleads, sobs, cowers or walks off; a named neighbour within 15 m watches, steps in, fetches a lawman
// within 50 m or walks on. Walk right up before he's struck her and he backs off or has words for you, and it's over.
// Once he has struck her he's fair game: no crime in dealing with him (as with the stalker, put back after); hit him
// and the fight is the game's, and she thanks you. Everyone's thought shows over their heads. The town's log has it.
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
        private sealed class DomesticRow
        {
            public string Phase = "spawn";   // spawn, gather, his, hers, end
            public Ped Man, Wife, Neighbour, Law;
            public Someone WM, WW, WN;
            public StagedMind MM, MW;
            public string Matter, Seed, Town, HisName, HerName, HerFull;
            public StagedAsk HisAsk, HerAsk;
            public string His, Hers;
            public int StartedAt, NextAt, Turn, Rant, StruckAt;
            public bool Spawned, Struck, YouIn, YouHit, Test, FairGame;
            public uint WifeModel;
            public string Outcome;
        }

        private DomesticRow _domestic;
        private int _domStarted, _domStruck, _domYouIn, _domYouHit, _domStepIn, _domLaw, _domModel, _domMind, _domNextOk;
        private readonly Dictionary<string, int> _domChoices = new Dictionary<string, int>();
        private string _domLast, _domWhyNot;

        private string DomesticNotNow(bool test = false)
        {
            if (_placeTown == null || _interiorType != 0)
                return "not out in a town's street";
            if (test)
                return null;
            int hr = CLOCK.GET_CLOCK_HOURS();
            if (hr < 8 || hr >= 21)
                return "it's " + hr.ToString("00") + ":00 (from 08:00 to 21:00)";
            if (_now < _domNextOk)
                return "there was one a little while ago";
            return null;
        }

        private bool StartDomestic(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string not = DomesticNotNow(test);
            if (not != null)
            {
                _domWhyNot = not;
                if (test)
                    Hint("Not now: " + not + ".", 5000);
                return false;
            }
            List<Candidate> pool = ReasonCandidates(me, now, test).Where(x => !PED.IS_PED_USING_ANY_SCENARIO(x.Ped.Handle) && !IsMindActing(x.Ped.Handle) && MindActWhyNot(x.Ped, x.Ped.Handle) == null).ToList();
            Candidate man = null, wife = null;
            float best = float.MaxValue;
            int wd = WatchEnter("domestic: the man search");
            try
            {
                // each position read once, the ties from one pass, the sight test only when it could change the pick (the frame-cost audit)
                int pn = pool.Count;
                var pos = new Vector3[pn];
                var ids = new HashSet<string>();
                for (int i = 0; i < pn; i++)
                {
                    pos[i] = pool[i].Ped.Position;
                    ids.Add(pool[i].Who.Id);
                }
                Dictionary<string, SceneTie> ties = PoolTies(ids);
                Vector3 mePos = me.Position;
                for (int i = 0; i < pn; i++)
                {
                    Candidate m = pool[i];
                    if (m.Who.Woman)
                        continue;
                    float d = pos[i].DistanceTo(mePos);
                    if (d < 6f || d > 35f)
                        continue;
                    // his sweetheart near him plays his wife: a courted tie, standing within 15 m
                    Candidate w = null;
                    for (int j = 0; j < pn && w == null; j++)
                    {
                        SceneTie t;
                        if (pool[j].Who.Woman && pos[j].DistanceTo(pos[i]) < 15f && ties.TryGetValue(ScPairKey(pool[j].Who.Id, m.Who.Id), out t) && t.Courted)
                            w = pool[j];
                    }
                    float score = d - (w != null ? 20f : 0f);
                    if (score - 5f >= best)
                        continue;
                    if (NodInView(me, m.Ped))
                        score -= 5f;
                    if (score >= best)
                        continue;
                    best = score;
                    man = m;
                    wife = w;
                }
            }
            finally
            {
                WatchLeave(wd);
            }
            if (man == null)
            {
                _domWhyNot = "no named man on his feet 6-35 m from you (" + pool.Count + " named and free)";
                if (test)
                    Hint("Nobody to do it: a named man on his feet 6-35 m from you. " + pool.Count + " named and free near you.", 6000);
                return false;
            }
            long day = _store.LastGameMin / 1440;
            var c = new DomesticRow { Man = man.Ped, WM = man.Who, Town = _placeTown, StartedAt = now, NextAt = now + 300, Test = test, Seed = man.Who.Id + "|dom|" + _store.LastGameMin / 60 };
            c.MM = StagedMindOf(man.Who, wife != null ? wife.Who.Id : null);
            string kind;
            int stage;
            bool good;
            if (man.Who.P != null && InnerLife.WorryNow(man.Who.P, day, out kind, out stage, out good) && !good)
                c.MM.Worry = kind;
            c.Matter = Domestic.MatterOf(c.MM.Worry, c.MM.Drunk);
            c.HisName = Person.FirstOf(man.Who.Name);
            if (wife != null)
            {
                c.Wife = wife.Ped;
                c.WW = wife.Who;
                c.MW = StagedMindOf(wife.Who, man.Who.Id);
                c.HerFull = wife.Who.Name;
                c.HerName = Person.FirstOf(wife.Who.Name);
                c.Phase = "gather";
                StagedHold(c.Wife);
            }
            else
            {
                // no sweetheart near: an ordinary townswoman walks in from out of view to play his wife (her name is made for him)
                c.WifeModel = Spawner.ModelFor(_placeTown, true, Voice.Hash(c.Seed));
                c.HerName = Residents.FirstName(true, man.Who.Id + "|wife");
                c.HerFull = c.HerName + " " + SurnameOf(man.Who.Name);
                c.MW = new StagedMind { Woman = true, Temper = Voice.Pick(man.Who.Id + "|wifet", "Loyal", "Proud", "Coward", "Cold", "Hothead", null) };
                c.Spawned = true;
            }
            // a named neighbour to see it: the nearest other within 15 m of him
            Candidate n = pool.Where(x => x != man && x != wife && x.Ped.Position.DistanceTo(man.Ped.Position) < 15f).OrderBy(x => x.Ped.Position.DistanceTo(man.Ped.Position)).FirstOrDefault();
            if (n != null)
            {
                c.Neighbour = n.Ped;
                c.WN = n.Who;
                StagedHold(c.Neighbour);
            }
            StagedLoad(Domestic.Dicts.Concat(new[] { SobDict }), now);
            StagedHold(c.Man);
            TASK.TASK_STAND_STILL(c.Man.Handle, 20000);
            _domestic = c;
            _domStarted++;
            if (!test)
                _domNextOk = now + 8 * 60000;
            Log.Write("ENCOUNTER domestic: " + man.Who.Name + " (" + (man.Who.Temper ?? "plain") + (c.MM.Drunk ? ", in his cups" : "") + ", " + c.Matter + ") and " + c.HerFull +
                      (c.Spawned ? " (his wife, brought from out of sight)" : " (his sweetheart, here already)") + " in " + _placeTown + ", " + (int)man.Ped.Position.DistanceTo(me.Position) + " m from you" +
                      (c.WN != null ? "; " + c.WN.Name + " near to see it" : "") + (test ? ", a test" : ""));
            return true;
        }

        private void DomesticStep(int now)
        {
            DomesticRow c = _domestic;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndDomestic(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died");
                return;
            }
            if (c.Phase == "spawn")
            {
                DomesticSpawn(c, now);
                return;
            }
            if (c.FairGame && c.Man.Exists() && !c.Man.IsDead)
                _cameAtYouSeen[c.Man.Handle] = now;   // he struck her: dealing with him is self-defence while it lasts
            if (c.Struck && !c.YouHit && c.Man.Exists() && c.Wife != null && c.Wife.Exists() && ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(c.Man.Handle, me.Handle, true, false))
            {
                DomYouHit(c, now);
                return;
            }
            string broke = ScBroken(c.Man) ?? (c.Wife == null ? "her gone" : !c.Wife.Exists() || c.Wife.IsDead ? "she's gone" : null);
            if (broke != null && !(c.Struck && c.Man.Exists() && c.Man.IsDead))
            {
                EndDomestic(broke == "is fighting or running" && c.Struck ? "he's fighting now" : "one of them " + broke);
                return;
            }
            if (c.Man.IsDead)
            {
                EndDomestic("he's dead");
                return;
            }
            if (now - c.StartedAt > 120000)
            {
                EndDomestic("it ran on too long");
                return;
            }
            int hm = c.Man.Handle, hw = c.Wife.Handle;
            if (!c.YouIn && !c.Struck && c.Phase != "gather" && c.Phase != "end" && me.Position.DistanceTo(c.Man.Position) < 3f)
            {
                DomYouStepIn(c, now);
                return;
            }
            if (now < c.NextAt)
                return;
            string sa = c.Seed + "|" + c.Turn;
            switch (c.Phase)
            {
                case "gather":
                    if (c.Wife.Position.DistanceTo(c.Man.Position) > 2.6f && now - c.StartedAt < 30000)
                    {
                        if (now - c.StartedAt > 600 && !PED.IS_PED_IN_COMBAT(hw, 0))
                            TASK.TASK_GO_TO_ENTITY(hw, hm, 20000, Natives.Brisk, 1.6f, 0f, 0);
                        c.NextAt = now + 1500;
                        return;
                    }
                    if (c.Wife.Position.DistanceTo(c.Man.Position) > 4f)
                    {
                        EndDomestic("she didn't get to him in time");
                        return;
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(hm, hw, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(hm, hw, 60000, 0, 51, 0);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(hw, hm, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(hw, hm, 60000, 0, 51, 0);
                    if (c.Neighbour != null && c.Neighbour.Exists())
                        TASK.TASK_LOOK_AT_ENTITY(c.Neighbour.Handle, hm, 60000, 0, 51, 0);
                    c.Phase = "his";
                    c.NextAt = now + 1200;
                    break;
                case "his":
                    if (c.HisAsk == null)
                    {
                        bool watched = (me.Position.DistanceTo(c.Man.Position) < 20f && NodInView(me, c.Man)) || LawWatching(c.Man, c.Wife) != null;
                        c.HisAsk = StagedChoose(hm, "husband", Domestic.Husband(c.MM, c.Matter, c.Turn, watched, c.Struck, sa), sa + "h",
                                                id => Domestic.Words("husband", id, c.HisName, c.HerName), Domestic.Now("husband", c.HisName, c.HerName, c.Matter), c.HerName, now);
                        if (c.Turn == 0)
                            _ticker.Push(c.WM.Name + ", thinking", Domestic.Thought("husband", c.Matter, c.Seed), "near", hm);
                    }
                    if (!StagedSettle(c.HisAsk, "domestic", c.WM.Name, now))
                    {
                        c.NextAt = now + 250;
                        return;
                    }
                    DomHis(c, now);
                    break;
                case "hers":
                    if (c.HerAsk == null)
                        c.HerAsk = StagedChoose(hw, "wife", Domestic.Wife(c.MW, c.His, c.Struck), sa + "w",
                                                id => Domestic.Words("wife", id, c.HisName, c.HerName), Domestic.Now("wife", c.HisName, c.HerName, c.Matter), c.HisName, now);
                    if (!StagedSettle(c.HerAsk, "domestic", c.HerFull, now))
                    {
                        c.NextAt = now + 250;
                        return;
                    }
                    DomHers(c, now);
                    break;
                case "end":
                    EndDomestic(c.Outcome ?? (c.His + ", " + c.Hers));
                    break;
            }
        }

        private void DomesticSpawn(DomesticRow c, int now)
        {
            if (now - c.StartedAt > 15000)
            {
                EndDomestic("nowhere out of sight for his wife");
                return;
            }
            if (!Spawner.Ready(c.WifeModel))
                return;
            Vector3 at;
            if (!EncounterSpot(Game.Player.Character, 18f, 32f, out at))
                return;
            Ped w = Spawner.Spawn(c.WifeModel, at, 0f);
            if (w == null)
                return;
            c.Wife = w;
            _encounterPeds.Add(w.Handle);
            StagedHold(w);
            TASK.TASK_GO_TO_ENTITY(w.Handle, c.Man.Handle, 25000, Natives.Brisk, 1.6f, 0f, 0);
            c.Phase = "gather";
            c.NextAt = now + 1500;
            Log.Write("ENCOUNTER domestic: his wife " + c.HerFull + " comes up the street, " + (int)at.DistanceTo(c.Man.Position) + " m from him");
        }

        private static string SurnameOf(string fullName)
        {
            string f = fullName ?? "";
            int sp = f.LastIndexOf(' ');
            return sp >= 0 ? f.Substring(sp + 1) : f;
        }

        private void DomCount(string what, StagedAsk a)
        {
            int n;
            _domChoices[what] = _domChoices.TryGetValue(what, out n) ? n + 1 : 1;
            if (a != null && a.ByModel)
                _domModel++;
            else
                _domMind++;
        }

        private void DomHis(DomesticRow c, int now)
        {
            Ped me = Game.Player.Character;
            int hm = c.Man.Handle, hw = c.Wife.Handle;
            string his = c.His = c.HisAsk.Picked;
            DomCount("he " + his, c.HisAsk);
            Log.Write("ENCOUNTER domestic: " + c.WM.Name + " chose " + his + " (" + By(c.HisAsk) + ")");
            c.HisAsk = null;
            string sa = c.Seed + "|" + c.Turn;
            switch (his)
            {
                case "rant":
                    string[] dc = Domestic.RantClips[c.Rant++ % Domestic.RantClips.Length].Split('|');
                    StagedClip(hm, dc[0], dc[1], "angry", false, sa, now);
                    Feed.Say(c.WM.Name, Domestic.Rant(c.Matter, c.HerName, sa), Speech.When(hm, "angry", hw), hm);
                    break;
                case "raise":
                    // a blow that knocks her down, once: no worse (the quarrel ladder's rule for a man against a woman)
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(hm, hw, 800, 0f, 0f, 0f);
                    Feed.Say(c.WM.Name, Domestic.Raise(sa), Speech.When(hm, "angry", hw), hm);
                    PED.SET_PED_TO_RAGDOLL(hw, 1200, 2200, 0, false, false, "");
                    Speech.SayAny(hw, Speech.Scared, hm);
                    c.Struck = true;
                    c.StruckAt = now;
                    _domStruck++;
                    if (!c.FairGame)
                    {
                        c.FairGame = true;
                        _cameAtYouSeen[hm] = now;
                        PED.SET_PED_CONFIG_FLAG(hm, 6, true);   // PCF_DontInfluenceWantedLevel, as the stalker: dealing with him is no crime (put back after)
                    }
                    Log.Write("ENCOUNTER domestic: " + c.WM.Name + " struck " + c.HerFull + " (fair game to you now)");
                    break;
                case "storm":
                    Feed.Say(c.WM.Name, Domestic.Storm(sa), Speech.When(hm, "storm", hw), hm);
                    StagedLetGo(c.Man, true);
                    c.Outcome = (c.Struck ? "he struck her, then " : "") + "he stormed off";
                    break;
                default:
                    Feed.Say(c.WM.Name, Domestic.Ease(sa), Speech.When(hm, "defuse", hw), hm);
                    c.Outcome = (c.Struck ? "he struck her, then " : "") + "he let it drop";
                    break;
            }
            c.Phase = "hers";
            c.NextAt = now + (his == "raise" ? 2400 : 3600);
        }

        private void DomHers(DomesticRow c, int now)
        {
            Ped me = Game.Player.Character;
            int hm = c.Man.Handle, hw = c.Wife.Handle;
            string hers = c.Hers = c.HerAsk.Picked;
            DomCount("she " + hers, c.HerAsk);
            Log.Write("ENCOUNTER domestic: " + c.HerFull + " chose " + hers + " (" + By(c.HerAsk) + ")");
            c.HerAsk = null;
            string sa = c.Seed + "|" + c.Turn;
            Feed.Say(c.HerFull, Domestic.Her(hers, c.HisName, sa), Speech.When(hw, hers == "argue" ? "angry" : hers == "leave" ? "storm" : "plead", hm), hw);
            switch (hers)
            {
                case "argue":
                    StagedGesture(hw, "angry", true, sa);
                    break;
                case "plead":
                    StagedGesture(hw, "plead", true, sa);
                    break;
                case "sob":
                    if (STREAMING.HAS_ANIM_DICT_LOADED(SobDict) && !PED.IS_PED_RAGDOLL(hw))
                        TASK.TASK_PLAY_ANIM(hw, SobDict, SobClip, 4f, -4f, -1, 0, 0f, false, 0, false, "", false);   // the proven sob (despair's)
                    break;
                case "cower":
                    TASK.TASK_HANDS_UP(hw, 4000, hm, -1, false);   // TASK_COWER is in the API but unproven: the hands up stands in
                    break;
                default:
                    StagedLetGo(c.Wife, false);
                    if (c.Struck)
                        TASK.TASK_SMART_FLEE_PED(hw, hm, 40f, -1, 0, 3f, 0);
                    else
                        TASK.TASK_WANDER_STANDARD(hw, 10f, 10);
                    c.Outcome = (c.Outcome != null ? c.Outcome + "; " : "") + "she walked off";
                    break;
            }
            if (c.Turn == 0 && c.Neighbour != null && c.Neighbour.Exists() && !c.Neighbour.IsDead)
                DomNeighbour(c, now);
            c.Turn++;
            bool over = hers == "leave" || c.His == "storm" || c.His == "ease" || c.Turn >= 3 || c.Outcome != null && c.Outcome.Contains("the law");
            if (over)
            {
                if (c.Outcome == null)
                    c.Outcome = (c.Struck ? "he struck her; " : "") + "they had it out";
                c.Phase = "end";
                c.NextAt = now + 4000;
            }
            else
            {
                c.Phase = "his";
                c.NextAt = now + 3600;
            }
        }

        private void DomNeighbour(DomesticRow c, int now)
        {
            int hn = c.Neighbour.Handle, hm = c.Man.Handle;
            SceneTie tw = c.WW != null ? _store.Ties.FirstOrDefault(x => x.Has(c.WN.Id) && x.Has(c.WW.Id)) : null;
            Ped law = null;
            Vector3 manAt = c.Man.Position;
            int wd = WatchEnter("domestic: a lawman near");
            try
            {
                foreach (Ped p in Peds.All())   // the distance first, the lawman test (a model name and a group) last (the frame-cost audit)
                    if (p != null && p.Position.DistanceTo(manAt) < 50f && p.Exists() && !p.IsDead && !PED.IS_PED_IN_COMBAT(p.Handle, 0) && !Spawner.IsOurs(p.Handle) && !NotFree(p.Handle) && Natives.IsLaw(p))
                    {
                        law = p;
                        break;
                    }
            }
            finally
            {
                WatchLeave(wd);
            }
            // the neighbour's own mind, at once (the two in it are the ones the model is asked for: one ask at a time each turn)
            string pick = Staged.Choose(Domestic.Neighbour(StagedMindOf(c.WN, null), tw != null ? tw.Warmth : 0, c.Struck, law != null), c.Seed + "|n");
            DomCount("neighbour " + pick, null);
            Log.Write("ENCOUNTER domestic: neighbour " + c.WN.Name + " " + (pick == "stepin" ? "steps in" : pick == "law" ? "goes for the law" : pick == "watch" ? "watches" : "walks on") + " (mind)");
            string line = Domestic.FromNeighbour(pick, c.HisName, c.Seed);
            switch (pick)
            {
                case "stepin":
                    _domStepIn++;
                    TASK.TASK_GO_TO_ENTITY(hn, hm, 8000, Natives.Brisk, 1.6f, 0f, 0);
                    Feed.Say(c.WN.Name, line, Speech.When(hn, "intervene", hm), hn);
                    break;
                case "law":
                    _domLaw++;
                    Feed.Say(c.WN.Name, line, Speech.When(hn, "thief", hm), hn);
                    if (law != null)
                    {
                        c.Law = law;
                        StagedHold(law);
                        TASK.TASK_GO_TO_ENTITY(law.Handle, hm, 20000, Natives.Brisk, 2f, 0f, 0);
                        Feed.Say("A lawman", Domestic.LawWord(c.HisName, c.Seed), Speech.When(law.Handle, "warned", hm), law.Handle);
                        c.Outcome = (c.Struck ? "he struck her; " : "") + "the law had a word";
                    }
                    break;
                case "watch":
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(hn, hm, 2000, 0f, 0f, 0f);
                    break;
                default:
                    StagedLetGo(c.Neighbour, true);
                    break;
            }
        }

        /// Arthur's blow lands on him once he has hit her: the game runs that fight, and she is grateful.
        private void DomYouHit(DomesticRow c, int now)
        {
            Ped me = Game.Player.Character;
            int hm = c.Man.Handle, hw = c.Wife.Handle;
            c.YouHit = true;
            _domYouHit++;
            Log.Write("ENCOUNTER domestic: you went for " + c.WM.Name + " after he struck her (fair game)");
            _staged.Remove(hm);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(hm, false);
            PED.SET_PED_KEEP_TASK(hm, false);
            Feed.Say(c.HerFull, Domestic.Thanks(true, c.Seed), Speech.When(hw, "thanks", me.Handle), hw, toYou: true);
            c.Outcome = "he struck her; you fought him";
            c.Phase = "end";
            c.NextAt = now + 4000;
        }

        /// You walked right up before he'd struck her: he backs off, or has words for you (never a gun), and it's over.
        private void DomYouStepIn(DomesticRow c, int now)
        {
            Ped me = Game.Player.Character;
            c.YouIn = true;
            _domYouIn++;
            int hm = c.Man.Handle;
            bool backs = c.MM.ToYou >= 0 && c.WM.Temper != "Hothead" && !c.MM.Drunk || c.WM.Temper == "Coward";
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(hm, me.Handle, 2000, 0f, 0f, 0f);
            StagedGesture(hm, backs ? "mild" : "angry", false, c.Seed + "you");
            Feed.Say(c.WM.Name, Domestic.ToYou(backs, c.Seed), Speech.When(hm, backs ? "defuse" : "warned", me.Handle), hm, toYou: true);
            if (c.Wife != null && c.Wife.Exists())
                Feed.Say(c.HerFull, Domestic.Thanks(false, c.Seed), Speech.When(c.Wife.Handle, "thanks", me.Handle), c.Wife.Handle, toYou: true);
            Log.Write("ENCOUNTER domestic: you stepped in; " + c.WM.Name + (backs ? " backs off" : " has words for you, and goes"));
            c.Outcome = backs ? "you stepped in; he backed off" : "you stepped in; he had words for you and went";
            c.Phase = "end";
            c.NextAt = now + 3500;
        }

        private void EndDomestic(string why)
        {
            DomesticRow c = _domestic;
            _domestic = null;
            if (c == null)
                return;
            Ped me = Game.Player.Character;
            if (c.Phase != "spawn" && c.Man != null && c.Man.Exists())
            {
                if (c.WW != null)
                    Ties.Note(_store.Ties, c.WM.Id, c.WM.Name, c.WW.Id, c.WW.Name, c.Town, c.Struck ? "rough" : "argue", Bridge.Now(), c.Struck ? c.WM.Id : null);
                Safe("world events", () => RecordEvent("encounter:domestic", c.WM.Id, c.WM.Name, c.WW != null ? c.WW.Id : null, c.HerFull, c.Man.Position, c.Man.Handle, c.Man.Handle, c.Wife != null && c.Wife.Exists() ? c.Wife.Handle : 0,
                                                       (c.Struck ? "struck; " : "words; ") + why));
            }
            if (c.FairGame && !c.YouHit && c.Man != null && c.Man.Exists() && !c.Man.IsDead)
                NoWanted(c.Man.Handle, false);   // an ordinary man again once it's over (dead, he stays as he fell)
            else if (c.FairGame && c.YouHit && c.Man != null && c.Man.Exists() && !c.Man.IsDead)
                NoWanted(c.Man.Handle, true);    // you are fighting him: PCF 6 stays on, and Watch lifts it with his self-defence mark, five minutes on, as for any attacker
            if (c.FairGame && c.Man != null && !c.YouHit)
                _cameAtYouSeen.Remove(c.Man.Handle);   // he walks the street after: no longer fair game (a fight you had with him stays judged as one he started, as the execution's men)
            foreach (Ped p in new[] { c.Man, c.Neighbour, c.Law })
                if (p != null && !(p == c.Man && c.YouHit))
                    StagedLetGo(p, true);
            if (c.Wife != null)
            {
                bool walking = c.Hers == "leave";
                StagedLetGo(c.Wife, !walking);
                if (c.Spawned && c.Wife.Exists())
                    Spawner.Release(c.Wife, false);   // the game's to keep or clear, as any passer-by
            }
            _domLast = (c.WM != null ? c.WM.Name : "?") + " and " + (c.HerFull ?? "his wife") + ": " + why + (c.Test ? " (a test)" : "");
            Log.Write("ENCOUNTER domestic: over (" + why + ")");
        }

        private string DomesticStatus()
        {
            if (!_cfg.DomesticDisputes)
                return "off ([Encounters] DomesticDisputes)";
            return _cfg.DomesticChance + "% a roll (08:00-21:00 in a town); " + _domStarted + " started, struck " + _domStruck + ", a neighbour stepped in " + _domStepIn + ", the law fetched " + _domLaw +
                   ", you stepped in " + _domYouIn + ", you fought him once he'd struck her " + _domYouHit + "; choices " + (_domChoices.Count == 0 ? "none yet" : string.Join(", ", _domChoices.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   " (the model's " + _domModel + ", the mind's " + _domMind + ")" + (_domestic != null ? "; going on: " + _domestic.WM.Name + " (" + _domestic.Phase + ")" : "") +
                   (_domLast != null ? "; last: " + _domLast : "") + (_domWhyNot != null ? "; the last look: " + _domWhyNot : "");
        }
    }
}
