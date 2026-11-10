// Called out yourself ([Encounters] CalloutsArthur; Core/CalloutsYou.cs). By day in a town, out of doors: one of the named
// people within 40 m, on their feet and free, with something against you, walks over and says it to your face. Walk off past// 12 m and they shout "coward" after you; stay and most put up their fists (MakeHostile: fighting them is self-defence), an
// armed hothead may draw instead ([Encounters] CalloutsArthurDraw); hold a gun on them and they back off with their hands up.
// Once a day each person, five real minutes at least between them; it never runs past two minutes.
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
        private sealed class CallYou
        {
            public string Phase = "walk";   // walk, face, wait, fight, gone
            public Ped P;
            public Someone W;
            public List<Ped> Friends = new List<Ped>();   // a survivor's friends who came with them (Core/Survivors.cs; FIVEDAY item 21)
            public string Matter, Seed, Ending;
            public int StartedAt, NextAt, FacedAt;
            public bool Test;
            public string Outcome;
        }

        private CallYou _callYou;
        private int _cyStarted, _cyFights, _cyDraws, _cyCoward, _cyBacked, _cySpat, _cyNextOk;
        private readonly HashSet<string> _cyDoneToday = new HashSet<string>();   // id|game day
        private string _cyLast, _cyWhyNot;
        private const float CallYouFrom = 40f, CallYouWalkOff = 12f;

        /// Why there can't be one here and now; null when there can.
        private string CallYouNotNow(bool test = false)
        {
            if (_placeTown == null || _interiorType != 0)
                return "not out in a town's street";
            if (test)
                return null;
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (clock < 8 || clock >= 20)
                return "it's " + clock.ToString("00") + ":00 (from 08:00 to 20:00)";
            if (_now < _cyNextOk)
                return "you were called out a little while ago";
            return null;
        }

        private bool StartCallYou(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string not = CallYouNotNow(test);
            if (not != null)
            {
                _cyWhyNot = not;
                if (test)
                    Hint("Not now: " + not + ".", 5000);
                return false;
            }
            long g = _store.LastGameMin;
            Vector3 at = me.Position;
            var byId = new Dictionary<string, Deed>();
            Candidate best = null;
            string matter = null;
            float bestD = float.MaxValue;
            int looked = 0;
            // the Testing row with nobody sore at you near (Lex, 2026-11-02: pressed [ in town, "not seeing it work"): the nearest
            // one named and free is given a test grievance, hard words between you, so the row always shows something. A hothead,
            // the proud or a drunk first, whoever's nearer (Lex, 2026-11-02, after a coward said his piece and went: the fists
            // and the draw come from the hot)
            Candidate stand = null;
            float standD = float.MaxValue, standScore = float.MaxValue;
            foreach (Candidate c in ReasonCandidates(me, now, test))
            {
                int h = c.Ped.Handle;
                if (PED.IS_PED_USING_ANY_SCENARIO(h) || IsMindActing(h) || MindActWhyNot(c.Ped, h) != null)
                    continue;
                float d = c.Ped.Position.DistanceTo(at);
                if (test && d <= CallYouFrom && d >= 4f && !(c.Who.P != null && (c.Who.P.Vengeful || c.Who.P.Murderous)))
                {
                    bool hot = c.Who.Temper == "Hothead" || c.Who.Temper == "Proud" || c.Who.Drunk;
                    float score = d - (hot ? 1000f : 0f) + (c.Who.Temper == "Coward" && !c.Who.Drunk ? 1000f : 0f);
                    if (score < standScore)
                    {
                        stand = c;
                        standD = d;
                        standScore = score;
                    }
                }
                if (d > CallYouFrom || d < 4f || d >= bestD)
                    continue;
                if (!test && _cyDoneToday.Contains(c.Who.Id + "|" + g / 1440))
                    continue;
                looked++;
                if (!test && _director.FullSceneMetres(c.Ped.Position) < EncounterSceneApart)
                    continue;
                PersonState st = _store.Find(c.Who.Id);
                string m = CalloutsYou.MatterOf(c.Who.Id, c.Who.Name, c.Who.Temper, c.Who.Drunk, c.Who.P != null ? c.Who.P.Home : null, c.Who.P, st, _store.Deeds, g);
                if (m == null)
                    continue;
                if ((m == "hurt" || m == "robbed") && SurvivorWayOf(c.Ped, c.Who) == Survivors.Broken)
                    continue;   // broken by it: they never come at you (FIVEDAY item 21)
                best = c;
                matter = m;
                bestD = d;
            }
            if (best == null && stand != null)
            {
                best = stand;
                // a hothead stands in with an old grudge (fists most of the time, and a draw now and then if armed); the rest over hard words
                bool hotOne = stand.Who.Temper == "Hothead" || stand.Who.Temper == "Proud" || stand.Who.Drunk;
                matter = hotOne ? "grudge" : "words";
                bestD = standD;
                Log.Write("ENCOUNTER test, callyou: nobody near with something against you; " + stand.Who.Name + " (" + (stand.Who.Temper ?? "no temper") + (stand.Who.Drunk ? ", drunk" : "") + ") stands in, sore over " + (hotOne ? "an old grudge" : "hard words") + " (a test)");
            }
            if (best == null)
            {
                _cyWhyNot = "nobody near with something against you (" + looked + " named and free within " + (int)CallYouFrom + " m)";
                if (test)
                    Hint("Nobody here to do it: someone named near you (4-40 m) with something against you (you struck or robbed them, they saw you kill, word of your thieving, or you soured them). " + looked + " named and free near you.", 7000);
                return false;
            }
            var cy = new CallYou { P = best.Ped, W = best.Who, Matter = matter, Seed = best.Who.Id + "|you|" + g / 60, StartedAt = now, NextAt = now, Test = test };
            StagedHold(cy.P);
            if ((matter == "hurt" || matter == "robbed") && SurvivorWayOf(best.Ped, best.Who) == Survivors.GetEven)
                foreach (Ped f in SurvivorFriends(best.Ped, best.Who, me))
                {
                    StagedHold(f);
                    TASK.TASK_GO_TO_ENTITY(f.Handle, me.Handle, 30000, 3.0f, 1.8f, 0f, 0);   // the friends come along, a step behind
                    cy.Friends.Add(f);
                    _survivorFriendsBrought++;
                }
            if (cy.Friends.Count > 0)
                Log.Write("ENCOUNTER callyou: " + cy.Friends.Count + " of " + best.Who.Name + "'s friends come with them (getting even)");
            _callYou = cy;
            StoryEncounterStarted("callyou", bestD, test);
            _cyStarted++;
            if (!test)
            {
                if (_cyDoneToday.Count > 200)
                    _cyDoneToday.Clear();
                _cyDoneToday.Add(best.Who.Id + "|" + g / 1440);
                _cyNextOk = now + 5 * 60000;
            }
            Log.Write("ENCOUNTER callyou: " + best.Who.Name + " (" + (best.Who.Temper ?? "no temper") + ") heads your way, sore over " + CalloutsYou.Over(matter) + " in " + _placeTown + ", " + (int)bestD + " m off" + (test ? ", a test" : ""));
            return true;
        }

        private void CallYouStep(int now)
        {
            CallYou c = _callYou;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndCallYou(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died", false);
                return;
            }
            if (c.P == null || !c.P.Exists() || c.P.IsDead)
            {
                EndCallYou(c.P != null && c.P.Exists() ? "they're dead" : "they're gone", c.Phase == "fight");
                return;
            }
            int h = c.P.Handle;
            if (c.Phase == "fight")
            {
                // theirs and yours now: over when they're down, running, or well away (the fight's own)
                if (!PED.IS_PED_IN_COMBAT(h, 0) && now - c.FacedAt > 8000 || c.P.Position.DistanceTo(me.Position) > 60f || now - c.StartedAt > 120000)
                    EndCallYou(c.Outcome ?? "the fight is done", true);
                return;
            }
            if (c.Phase != "walk" && c.Phase != "gone" && (PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || _director.Involves(h)))
            {
                EndCallYou("they were taken up by something else", true);
                return;
            }
            if (now - c.StartedAt > 90000 && c.Phase != "gone")
            {
                EndCallYou("it ran on too long", c.Phase != "walk");
                return;
            }
            // a gun on them: hands up, back off, and it's done
            if ((c.Phase == "walk" || c.Phase == "face" || c.Phase == "wait") && PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h))
            {
                TASK.TASK_HANDS_UP(h, 4000, me.Handle, -1, false);
                Feed.Say(c.W.Name, CalloutsYou.Backs(c.Seed), Speech.When(h, "scared", me.Handle), h, toYou: true);
                _cyBacked++;
                c.Outcome = "you drew on them; they backed off";
                Log.Write("ENCOUNTER callyou: you drew on " + c.W.Name + "; they back off");
                c.Phase = "gone";
                c.NextAt = now + 4500;
                return;
            }
            if (now < c.NextAt)
                return;
            float apart = c.P.Position.DistanceTo(me.Position);
            switch (c.Phase)
            {
                case "walk":
                    if (c.FacedAt == 0)
                    {
                        // the hail from where they are, then over to you
                        c.FacedAt = -1;
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 60000, 0, 51, 0);
                        Feed.Say(c.W.Name, CalloutsYou.Hail(c.Matter, c.Seed), Speech.When(h, "angry", me.Handle), h, toYou: true);
                    }
                    if (c.P.Position.DistanceTo(me.Position) > 3.2f)
                    {
                        EventDot("callyou", c.P.Position);
                        if (now - c.StartedAt > 40000)
                        {
                            EndCallYou("they never reached you", false);
                            return;
                        }
                        TASK.TASK_GO_TO_ENTITY(h, me.Handle, 4000, Natives.Brisk, 2.2f, 0f, 0);
                        c.NextAt = now + 1500;
                        return;
                    }
                    EventDotClear("callyou");
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2000, 0f, 0f, 0f);
                    c.Phase = "face";
                    c.NextAt = now + 700;
                    break;
                case "face":
                    StagedGesture(h, "angry", c.W.Woman, c.Seed);
                    Feed.Say(c.W.Name, CalloutsYou.Face(c.Matter, c.Seed), Speech.When(h, "angry", me.Handle), h, toYou: true);
                    _ticker.Push(c.W.Name + ", thinking", CalloutsYou.Thought(c.Seed), "near", h);
                    NoteTheirThought(h, now);
                    foreach (Ped f in c.Friends.Where(x => x.Exists() && !x.IsDead).Take(1))
                    {
                        MindWho fw = MindKnown(f.Handle, now);
                        Feed.Say(fw != null && fw.Name != null ? fw.Name : TalkName(f), Survivors.BackingLine(Person.FirstOf(c.W.Name), c.Seed), Speech.When(f.Handle, "threat", me.Handle), f.Handle, toYou: true);
                    }
                    uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                    bool armed = gun != 0 && gun != Natives.Hash("WEAPON_UNARMED");
                    c.Ending = CalloutsYou.Ending(c.W.Temper, c.W.Drunk, c.Matter, armed, _cfg.CalloutsArthurDraw, c.Seed);
                    c.FacedAt = now;
                    c.Phase = "wait";
                    c.NextAt = now + 1000;   // then the 12 m walk-off check every 300 ms for the 7 s they wait on you
                    break;
                case "wait":
                    if (apart > CallYouWalkOff)
                    {
                        // you walked off on them
                        Feed.Say(c.W.Name, CalloutsYou.Coward(c.Seed), Speech.When(h, "mock", me.Handle), h, toYou: true);
                        _cyCoward++;
                        c.Outcome = "you walked off; they called you a coward";
                        Log.Write("ENCOUNTER callyou: you walked off; " + c.W.Name + " calls you a coward");
                        c.Phase = "gone";
                        c.NextAt = now + 3000;
                        return;
                    }
                    if (now - c.FacedAt < 7000)
                    {
                        c.NextAt = now + 300;
                        return;
                    }
                    CallYouEnds(c, now);
                    break;
                case "gone":
                    EndCallYou(c.Outcome ?? "over", true);
                    break;
            }
        }

        /// You stood there and took it: fists, a gun, or a last word and away.
        private void CallYouEnds(CallYou c, int now)
        {
            Ped me = Game.Player.Character;
            int h = c.P.Handle;
            Feed.Say(c.W.Name, CalloutsYou.Ends(c.Ending, c.Seed), Speech.When(h, c.Ending == "spit" ? "warned" : "threat", me.Handle), h, toYou: true);
            Log.Write("ENCOUNTER callyou: " + c.W.Name + " " + (c.Ending == "fists" ? "puts up their fists" : c.Ending == "draw" ? "draws on you" : "says their piece and goes"));
            if (c.Ending == "spit")
            {
                _cySpat++;
                c.Outcome = "had their say and left";
                c.Phase = "gone";
                c.NextAt = now + 2500;
                return;
            }
            _staged.Remove(h);
            PED.SET_PED_KEEP_TASK(h, false);
            MakeHostile(c.P, now);   // they came at you: fighting them is self-defence
            if (c.Ending == "draw")
            {
                _cyDraws++;
                uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                WEAPON.SET_CURRENT_PED_WEAPON(h, gun, true, 0, false, false);
                c.Outcome = "out came their gun";
            }
            else
            {
                _cyFights++;
                BrawlFists(h, "a caller");
                c.Outcome = "they put up their fists";
            }
            TASK.TASK_COMBAT_PED(h, me.Handle, 0, 16);
            foreach (Ped f in c.Friends)
                if (f.Exists() && !f.IsDead)
                {
                    _staged.Remove(f.Handle);
                    PED.SET_PED_KEEP_TASK(f.Handle, false);
                    MakeHostile(f, now);   // they came at you with them: self-defence too
                    BrawlFists(f.Handle, "a caller's friend");
                    TASK.TASK_COMBAT_PED(f.Handle, me.Handle, 0, 16);
                }
            c.FacedAt = now;
            c.Phase = "fight";
            c.NextAt = now + 1000;
        }

        private void EndCallYou(string why, bool played)
        {
            EventDotClear("callyou");
            CallYou c = _callYou;
            _callYou = null;
            if (c == null)
                return;
            bool reached = played && c.Phase != "walk";
            StoryEncounterEnded("callyou", c.StartedAt, reached, c.Test, reached && c.P != null && c.P.Exists()
                ? () => Safe("world events", () => RecordEvent("encounter:callyou", c.W.Id, c.W.Name, "player", "you", c.P.Position, c.P.Handle, c.P.Handle, 0, why))
                : (Action)null);
            if (c.P != null && c.Phase != "fight")
                StagedLetGo(c.P, true);
            if (c.Phase != "fight")
                foreach (Ped f in c.Friends)
                    if (f.Exists())
                        StagedLetGo(f, true);
            _cyLast = (c.W != null ? c.W.Name : "?") + " called you out over " + CalloutsYou.Over(c.Matter) + ": " + why + (c.Test ? " (a test)" : "");
            Log.Write("ENCOUNTER callyou: over (" + why + ")");
        }

        private string CallYouStatus()
        {
            if (!_cfg.CalloutsArthur)
                return "off ([Encounters] CalloutsArthur)";
            return _cfg.CalloutsArthurChance + "% a roll (08:00-20:00 in a town), draw " + _cfg.CalloutsArthurDraw + "%; " + _cyStarted + " started: fists " + _cyFights + ", drew " + _cyDraws + ", their piece said " + _cySpat +
                   ", you walked off " + _cyCoward + ", a gun held on them " + _cyBacked + (_callYou != null ? "; going on: " + _callYou.W.Name + " (" + _callYou.Phase + ")" : "") +
                   (_cyLast != null ? "; last: " + _cyLast : "") + (_cyWhyNot != null ? "; the last look: " + _cyWhyNot : "");
        }
    }
}
