// What follows a street fight near the player (the NPC-life run, step 1; Lex, 2026-10-30: "consequences that chain"). The
// Director says when a fight is over and who was left standing (FightOver); a few seconds on, a friend of the one who
// went down walks over and sees to him (Scenes.Tend), or, with no friend there, a kind soul among those who saw it; then a
// lawman within 50 m has a word with the one left standing (Scenes.LawWord). Each is a scene of its own, played when the
// street is free; the whole of it gives up after a minute or once the player is far off. The grudge it leaves comes back
// on its own: the fight's hostile tie brings the two to words again when they next meet (Reasons, after a day's rest).
// Behind [Mind] MindLive and Aftermath.
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
        private sealed class AfterFight
        {
            public Ped Winner, Loser;
            public int At, Until, Step;   // step 0: the tending; 1: the law; 2: done
            public string Seed;
            public Candidate Go, Second;  // the friend (or the kind soul) who goes, and a second friend for closing round (Core/GroupActs.cs; item 21)
            public string LoserId, LoserName, Pick;   // Pick: null not yet, "asking" (the mind's and the model's pick on its way), tend, close, leave
            public StagedAsk Ask;
        }

        private AfterFight _after;
        private int _afterTends, _afterLaw;
        private string _afterLast;

        /// From the Director: a fight between two of the street's people is over.
        private void OnFightOver(int winner, int loser)
        {
            if (!_cfg.MindLive || !_cfg.Aftermath || winner == 0 || loser == 0)
                return;
            int now = Game.GameTime;
            _after = new AfterFight { Winner = new Ped(winner), Loser = new Ped(loser), At = now + 3000, Until = now + 60000, Step = 0, Seed = winner + "|" + loser + "|" + now / 60000 };
            Log.Detail("AFTER a fight: " + (MindNameOf(winner) ?? "the winner") + " left standing, " + (MindNameOf(loser) ?? "the other") + " down");
        }

        private void AftermathStep(Ped me, int now)
        {
            AfterFight a = _after;
            if (a == null || now < a.At)
                return;
            if (now > a.Until || a.Step >= 2 || a.Loser == null || !a.Loser.Exists() || a.Loser.Position.DistanceTo(me.Position) > 45f)
            {
                _after = null;
                return;
            }
            if (_director.Busy || Feed.Busy)
            {
                a.At = now + 1000;   // the next of it waits for the street to be free
                return;
            }
            a.At = now + 1500;
            try
            {
                if (a.Step == 0)
                {
                    if (!a.Loser.IsDead && AfterTend(a, me, now))
                    {
                        if (a.Pick == "asking")
                        {
                            a.At = now + 500;   // the friend's mind (and the model) is choosing: close round, tend alone, or leave it (item 21)
                            return;
                        }
                        a.Step = 1;
                        a.At = now + 4000;
                        return;
                    }
                    a.Step = 1;
                }
                if (a.Step == 1)
                {
                    a.Step = 2;
                    if (now >= _brawlLawAfterUntil)
                        AfterLaw(a, me, now);   // unless a lawman broke it up himself and has said his word (WestworldScript.Brawl.cs)
                }
            }
            catch (Exception ex)
            {
                _after = null;
                Log.Detail("AFTER failed: " + ex.Message);
            }
        }

        /// A friend of the one who went down goes to him; with none near, maybe a kind soul who saw it. With another friend near
        /// and [Mind] GroupActs on, the first friend's mind (and the model, for a known mind) picks: tend him alone, fetch the
        /// other and close round him (Core/GroupActs.cs; item 21), or leave it. True if a scene began, or the pick is on its way
        /// (a.Pick "asking").
        private bool AfterTend(AfterFight a, Ped me, int now)
        {
            if (a.Pick == "asking")
            {
                if (!StagedSettle(a.Ask, "closeround", a.Go.Who.Name, now))
                    return true;
                a.Pick = a.Ask != null && a.Ask.Picked != null ? a.Ask.Picked : "tend";
                if (a.Ask != null && a.Ask.Asked)
                    _afterModelPicks++;
                return AfterGo(a, me, now);
            }
            string loserId, loserName;
            if (!LastingId(a.Loser, out loserId, out loserName))
                return false;
            Vector3 at = a.Loser.Position;
            Candidate friend = null, second = null, kind = null;
            int best = 0, next = 0;
            foreach (Candidate c in ReasonCandidates(me, now))
            {
                int h = c.Ped.Handle;
                if (h == a.Loser.Handle || h == a.Winner.Handle || c.Who.Id == loserId || _director.Involves(h) || PED.IS_PED_IN_COMBAT(h, 0))
                    continue;
                float d = c.Ped.Position.DistanceTo(at);
                if (d > 25f)
                    continue;
                int w = LadderWarmth(c.Who.Id, loserId);
                if (w > best)
                {
                    if (friend != null && best > next)
                    {
                        next = best;
                        second = friend;
                    }
                    best = w;
                    friend = c;
                    continue;
                }
                if (w > 0 && w > next)
                {
                    next = w;
                    second = c;
                    continue;
                }
                if (kind == null && d < 12f)
                {
                    MindWho mw = MindKnown(h, now);
                    Traits t = mw != null && mw.Id != null && mw.Src != null ? (mw.Traits ?? (mw.Traits = _engine.Shadow.TraitsOf(mw.Src))) : null;
                    if (t != null && t.Z[Traits.Compassion] > 0.9f && Voice.Chance(a.Seed + "|kind|" + c.Who.Id, 50))
                        kind = c;
                }
            }
            Candidate go = friend ?? kind;
            if (go == null)
                return false;
            a.Go = go;
            a.Second = friend != null ? second : null;
            a.LoserId = loserId;
            a.LoserName = loserName;
            if (_cfg.GroupActs && friend != null && second != null && go.Who != null)
            {
                // the first friend's mind, and the model's pick when it's on and the mind knows them (as the favours ask)
                StagedMind sm = StagedMindOf(go.Who, null);
                sm.Warmth = best;
                Staged.Weights wt = GroupActs.CloseRoundWeights(sm, 2);
                string other = Person.FirstOf(second.Who.Name), loserFirst = Person.FirstOf(loserName);
                a.Ask = StagedChoose(go.Ped.Handle, "closeround", wt, a.Seed + "|close", id => GroupActs.Words(id, other), GroupActs.Now(loserFirst), loserFirst, now);
                a.Pick = "asking";
                return true;
            }
            a.Pick = "tend";
            return AfterGo(a, me, now);
        }

        private int _afterCloseRounds, _afterModelPicks;

        /// The pick taken: the tending as before, the two friends closing round him (a scene of three), or nothing.
        private bool AfterGo(AfterFight a, Ped me, int now)
        {
            Candidate go = a.Go;
            if (go == null || go.Ped == null || !go.Ped.Exists() || go.Ped.IsDead || a.Loser == null || !a.Loser.Exists())
                return false;
            if (a.Pick == "leave")
            {
                _afterLast = go.Who.Name + " left " + a.LoserName + " to it";
                Log.Write("AFTER the fight: " + go.Who.Name + ", a friend, leaves " + a.LoserName + " to it (the mind's pick)");
                return false;
            }
            Vector3 at = a.Loser.Position;
            bool saloon = _placeKind == "saloon";
            if (a.Pick == "close" && a.Second != null && a.Second.Ped != null && a.Second.Ped.Exists() && !a.Second.Ped.IsDead && !_director.Involves(a.Second.Ped.Handle))
            {
                string seed = a.Seed + "|close";
                SceneCast cast = BuildCast(go.Ped, a.Loser, a.Second.Ped, saloon, me, now, seed);
                Scene s = GroupActs.CloseRound(cast, seed);
                AddMeet(s, "comfort", go.Ped, a.Loser, go.Ped.Position.DistanceTo(at));
                if (StartScene(s, new List<Ped> { go.Ped, a.Loser, a.Second.Ped }, cast, seed, now))
                {
                    _afterCloseRounds++;
                    _afterLast = go.Who.Name + " and " + a.Second.Who.Name + " closed round " + a.LoserName;
                    Log.Write("AFTER the fight: " + go.Who.Name + " fetches " + a.Second.Who.Name + " and the two close round " + a.LoserName + (a.Ask != null && a.Ask.Asked ? " (the model's pick)" : ""));
                    return true;
                }
            }
            string tseed = a.Seed + "|tend";
            SceneCast tcast = BuildCast(go.Ped, a.Loser, null, saloon, me, now, tseed);
            Scene ts = Scenes.Tend(tcast, tseed, "rough");
            AddMeet(ts, "tend", go.Ped, a.Loser, go.Ped.Position.DistanceTo(at));
            if (!StartScene(ts, new List<Ped> { go.Ped, a.Loser }, tcast, tseed, now))
                return false;
            _afterTends++;
            _afterLast = go.Who.Name + " tended " + a.LoserName;
            Log.Write("AFTER the fight: " + go.Who.Name + (a.Second != null || LadderWarmth(go.Who.Id, a.LoserId) > 0 ? ", a friend," : ", who has a kind heart,") + " goes to see to " + a.LoserName);
            return true;
        }

        /// A lawman near has a word with the one left standing.
        private void AfterLaw(AfterFight a, Ped me, int now)
        {
            if (a.Winner == null || !a.Winner.Exists() || a.Winner.IsDead || _director.Involves(a.Winner.Handle))
                return;
            Vector3 at = a.Winner.Position;
            Ped law = Peds.All().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && p.Position.DistanceTo(at) < 50f && Natives.IsLaw(p) &&
                                                    !PED.IS_PED_ON_MOUNT(p.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) && !PED.IS_PED_IN_COMBAT(p.Handle, 0) &&
                                                    !NotFree(p.Handle))   // (NotFree: a scene, the theft's or the body's lawman, an encounter's fetched deputy: NIGHT4 item 6)
                                        .OrderBy(p => p.Position.DistanceTo(at)).FirstOrDefault();
            if (law == null)
                return;
            string id, name, temper;
            LastingId(a.Winner, out id, out name, out temper);
            bool saloon = _placeKind == "saloon";
            string seed = a.Seed + "|law";
            SceneCast cast = BuildCast(law, a.Winner, null, saloon, me, now, seed);
            if (cast.Names != null && cast.Names.Length > 0 && cast.Names[0] == null)
                cast.Names[0] = "The deputy";
            Scene s = Scenes.LawWord(cast, seed, temper);
            if (!StartScene(s, new List<Ped> { law, a.Winner }, cast, seed, now))
                return;
            _afterLaw++;
            _afterLast = "the law had a word with " + (name ?? "the winner");
            Log.Write("AFTER the fight: a lawman " + (int)law.Position.DistanceTo(at) + " m off walks over to " + (name ?? "the one left standing") + " (" + (temper ?? "plain") + ")");
        }

        /// F7: what followed fights this session.
        private string AftermathStatus()
        {
            if (_cfg.GroupActs)
                return AftermathStatusPlain() + "; closing round " + _afterCloseRounds + " (" + _afterModelPicks + " the model's pick)";
            return AftermathStatusPlain();
        }

        private string AftermathStatusPlain()
        {
            if (!_cfg.MindLive || !_cfg.Aftermath)
                return "off ([Scenes] Aftermath, with MindLive)";
            return _afterTends + " tended after a fight, " + _afterLaw + " words from the law" + (_afterLast != null ? "; last: " + _afterLast : "") + (_after != null ? "; one under way" : "");
        }
    }
}
