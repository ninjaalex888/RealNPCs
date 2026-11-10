// A holdup at a store's door, the game side (FOURTEENHOUR reserve R1 ★; Core/Holdup.cs; [Scenes] StoreHoldups). From DramaTick when
// the street is free, by day (9 till 6) in a town, out of doors, with no lawman within 50 m: the store's keeper (the counter's post,
// PostKind "store") within 60 m of you, a man among the mind's people near you who leans to it (Holdup.Leans: known for theft, the
// mugging's own lean, or at the worst of his money trouble) and carries a gun, and a customer on their own within 18 m of the keeper.
// Once a town a day, at 15% when one fits. The scene is the Director's (the mugging's beats: meet, aim, holster, run, flee, the
// theft's cb:thief, all seen); the keeper's shout is a caption only (he keeps his post); J asks for your word while he holds them up
// (Scenes.Intrude's "holdup": talked down by his nature, or he runs with it), and a greeting does the same. Drawn on (the Director
// stops a scene you aim into), he runs empty-handed. The town's tale after (Core/Tales.cs). Looks ride on TheftTick; never a frame
// of their own.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class HoldupNote
        {
            public Ped Robber, Mark, Keeper;
            public string RobberName, MarkName, KeeperName, Seed, Town, Answer;
            public bool Desperate, Ran, Worded, KeeperSaid, Shot;   // Shot: the scene had his shot at a bolter (HoldupShots)
            public int Started, KeeperAt, LookAt;
        }

        private HoldupNote _holdup;
        private int _holdupNextAt, _holdups, _holdupsDown, _holdupsRan, _holdupsDrawn, _holdupsBolted;
        private string _holdupLast;

        /// From DramaTick when the street is free: today's holdup at the store near you, if one fits. why: what stood in the way (the test).
        private bool HoldupScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!Holdup.On || !_cfg.MindLive)
            {
                why = "off ([Scenes] StoreHoldups, or [Mind] MindLive)";
                return false;
            }
            if (_placeTown == null || _interiorType != 0)
            {
                why = "it wants a town street, out of doors";
                return false;
            }
            if (!test && now < _holdupNextAt)
                return false;
            _holdupNextAt = now + 20000;
            bool sure = test;   // the Testing row: no store, hour, lean or roll needed
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!sure && (clock < Holdup.From || clock >= Holdup.To))
                return false;
            long day = _store.LastGameMin / 1440;
            string key = "holdup|" + _placeTown + "|" + day;
            if (!sure && _nightDone.Contains(key))
                return false;
            if (_holdup != null || _theft != null)
            {
                why = "a holdup or a thief's run is going on";
                return false;
            }
            if (LawmanNear(me, 50f))
            {
                why = "a lawman within 50 m";
                return false;
            }
            Ped keeper = null;
            PostKind kk = null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Position.DistanceTo(me.Position) > 60f)
                    continue;
                PostKind pk = PostKindOf(p);
                if (pk != null && pk.Kind == "store")
                {
                    keeper = p;
                    kk = pk;
                    break;
                }
            }
            if (keeper == null && !test)
                return false;
            List<Candidate> people = ReasonCandidates(me, now, test).Where(c => !c.Ped.IsDead && c.Ped.Position.DistanceTo(me.Position) < NightMetres && Natives.InteriorType(c.Ped.Handle) == 0).ToList();
            Candidate robber = null;
            bool desperate = false;
            string lean = null;
            foreach (Candidate c in people.OrderBy(c => c.Ped.Position.DistanceTo(me.Position)))
            {
                if (c.Who.Woman || PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || !WEAPON.IS_PED_ARMED(c.Ped.Handle, 4))
                    continue;   // a man carrying a sidearm, on his feet
                PersonState st = _store.Find(c.Who.Id);
                bool known = st != null && (st.Has(PickpocketMark) || st.Has(MuggerMark));
                MindWho w = MindKnown(c.Ped.Handle, now);
                Traits t = TraitsAt(c.Ped, now);
                bool crisis = false;
                string wk; int stage; bool good;
                if (c.Who.P != null && InnerLife.WorryNow(c.Who.P, day, out wk, out stage, out good) && stage == 2 && (wk == "debt" || wk == "bank" || wk == "cards"))
                    crisis = true;   // at the worst of his money trouble
                bool d;
                bool leans = Holdup.Leans(t, w != null && w.Needs != null ? w.Needs[NeedState.Money] : 0f, known, crisis, out d);
                if (!sure && !leans)
                    continue;
                robber = c;
                desperate = crisis;   // hardest to talk down (Holdup.DownPercent)
                lean = sure ? "a test" + (crisis ? ", at the worst of his money trouble" : "") : known ? "known for it" : d ? "at the worst of his money trouble" : "short of money, and honest with nobody";
                break;
            }
            if (robber == null)
            {
                why = "no armed man near you (" + people.Count + " free within " + (int)NightMetres + " m)";
                return false;
            }
            // a customer on their own: within 18 m of the keeper (any 3-16 m of him for the test), 3 to 20 m from the robber, nobody else within 5 m
            Candidate mark = people.Where(c => c != robber && c.Ped.Position.DistanceTo(robber.Ped.Position) > 3f && c.Ped.Position.DistanceTo(robber.Ped.Position) < (sure ? 16f : 20f) &&
                                               (keeper == null || sure || c.Ped.Position.DistanceTo(keeper.Position) < Holdup.StoreMetres) &&
                                               (sure || !people.Any(o => o != c && o != robber && o.Ped.Position.DistanceTo(c.Ped.Position) < Holdup.AloneMetres)))
                                   .OrderBy(c => c.Ped.Position.DistanceTo(robber.Ped.Position)).FirstOrDefault();
            if (mark == null)
            {
                why = "nobody on their own near " + robber.Who.Name + (keeper != null ? " by the store" : "");
                return false;
            }
            string seed = robber.Ped.Handle + "|" + mark.Ped.Handle + "|" + day + "|hold";
            if (!sure)
            {
                _nightDone.Add(key);   // the roll is the day's, whatever it says
                if (!Voice.Chance(seed, Holdup.Percent))
                {
                    Log.Detail("HOLDUP " + robber.Who.Name + " eyes " + mark.Who.Name + " by the store, and lets it go today");
                    return false;
                }
            }
            SceneCast cast = BuildCast(robber.Ped, mark.Ped, null, false, me, now, seed);
            cast.Armed0 = true;    // the holdup's own rule: only he needs the gun (no lawman within 50 m, looked at above)
            cast.Armed1 = false;   // the customer doesn't draw
            cast.Desperate = desperate;
            string answer = Holdup.Answer(TraitsAt(mark.Ped, now), seed);
            bool shoots = answer == "bolt" && Holdup.Shots && Voice.Chance(seed + "|shot", Holdup.ShotPercent);
            Scene s = Holdup.Play(cast, seed, answer, shoots);
            if (!StartScene(s, new List<Ped> { robber.Ped, mark.Ped }, cast, seed, now, test))
            {
                why = "the scene wouldn't start (the Director refused it: see the SCENE line)";
                return false;
            }
            Resident holder = keeper != null ? HolderAt(keeper, kk) : null;
            _holdup = new HoldupNote
            {
                Robber = robber.Ped, Mark = mark.Ped, Keeper = keeper, RobberName = robber.Who.Name, MarkName = mark.Who.Name, KeeperName = holder != null ? holder.Name : kk != null ? kk.Role : null,
                Seed = seed, Town = _placeTown, Answer = answer, Desperate = desperate, Shot = shoots, Started = now, KeeperAt = now + 2600, LookAt = now + 500
            };
            _holdups++;
            PersonState rs = _store.Find(robber.Who.Id);
            if (rs != null)
                rs.Mark(MuggerMark);   // known for it after
            if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                PromptSet(Holdup.Kind, new TownEvent { Kind = Holdup.Kind, Town = _placeTown, AName = robber.Who.Name }, Holdup.AskMs);
            Log.Write("HOLDUP " + robber.Who.Name + " (" + lean + ") holds up " + mark.Who.Name + " by the store in " + _placeTown + ", who " +
                      (answer == "bolt" ? "bolts" + (shoots ? ", and he fires (HoldupShots)" : "") : answer == "plead" ? "pleads, and hands it over" : "hands it over") +
                      " (" + (int)robber.Ped.Position.DistanceTo(me.Position) + " m from you; J: your word)");
            return true;
        }

        /// From TheftTick, every half second while a holdup is on: the keeper's shout; the thief's run once he has it; drawn on, he runs.
        private void HoldupStep(int now)
        {
            HoldupNote h = _holdup;
            if (h == null || now < h.LookAt)
                return;
            h.LookAt = now + 500;
            Ped me = Game.Player.Character;
            if (!h.KeeperSaid && now >= h.KeeperAt)
            {
                h.KeeperSaid = true;
                if (h.Keeper != null && h.Keeper.Exists() && !h.Keeper.IsDead && h.Keeper.Position.DistanceTo(me.Position) < 40f)
                    SpeechCaption(h.KeeperName ?? "The storekeeper", Holdup.KeeperShout(h.Seed), h.Keeper.Handle, 4500, false, false);   // words only: he keeps his post
            }
            bool robberOk = h.Robber != null && h.Robber.Exists() && !h.Robber.IsDead;
            bool playing = robberOk && _director.Involves(h.Robber.Handle);
            if (playing && now - h.Started < 60000)
                return;   // still playing
            // the scene is over, or cut short. How it ended: he ran with it (the theft's from here); your word turned him; they bolted
            // (and he fired, with HoldupShots); or cut short before he had it (the player aimed at the cast, and the Director stops a
            // scene so; or someone came in)
            string how = h.Ran ? "ran" : h.Worded ? "down" : h.Answer == "bolt" ? (h.Shot ? "shot" : "bolted") : "drawn";
            if (!h.Ran && !h.Worded && robberOk && !playing && !PED.IS_PED_IN_COMBAT(h.Robber.Handle, me.Handle))
            {
                // empty-handed, whatever ended it: off before the town comes (the flee, seen). In combat with you, he's the game's
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h.Robber.Handle, false);
                TASK.CLEAR_PED_TASKS(h.Robber.Handle, true, false);
                TASK.TASK_SMART_FLEE_PED(h.Robber.Handle, me.Handle, 150f, -1, 0, 3f, 0);
            }
            if (how == "drawn")
                _holdupsDrawn++;
            else if (how == "bolted" || how == "shot")
                _holdupsBolted++;
            HoldupTale(h, how);
            _holdupLast = h.RobberName + " held up " + h.MarkName + (how == "down" ? "; your word turned him" : how == "ran" ? "; he ran with it" : how == "drawn" ? "; cut short, he ran empty-handed" :
                                                                     how == "shot" ? "; they bolted, and he fired after them" : "; they bolted, and he ran");
            Log.Write("HOLDUP over: " + _holdupLast);
            if (_eventPrompt == Holdup.Kind)
                PromptClear();
            _holdup = null;
        }

        /// The scene's cb:thief on a holdup: he has it, and runs (the theft's from here). From SceneAction.
        private void HoldupRan()
        {
            HoldupNote h = _holdup;
            if (h == null)
                return;
            h.Ran = true;
            _holdupsRan++;
            if (_eventPrompt == Holdup.Kind)
                PromptClear();   // too late for a word: catch him
        }

        /// J's row: your word while he holds them up (Scenes.Intrude's "holdup", as a greeting would).
        private void HoldupChoose(string id)
        {
            HoldupNote h = _holdup;
            if (h == null || id != "holdup:talk" || h.Ran || h.Worded || h.Robber == null || !h.Robber.Exists() || h.Robber.IsDead)
                return;
            if (h.Robber.Position.DistanceTo(Game.Player.Character.Position) > 25f)
            {
                Hint("Too far off for him to hear you.", 2500);
                return;
            }
            SceneIntrusion(h.Robber.Handle, true);
        }

        /// Your word reached the scene (SceneIntrusion, by J or a greeting): noted for the tale and F7.
        private void HoldupWorded(string outcome)
        {
            HoldupNote h = _holdup;
            if (h == null)
                return;
            h.Worded = true;
            if (outcome == "peace")
                _holdupsDown++;
            if (_eventPrompt == Holdup.Kind)
                PromptClear();
            Log.Write("HOLDUP your word: " + (outcome == "peace" ? h.RobberName + " lowers it and goes" : h.RobberName + " won't hear it, and runs with it"));
        }

        /// The town's tale of it (Core/Tales.cs): told on, the one man grows into a gang.
        private void HoldupTale(HoldupNote h, string how)
        {
            if (!Tales.On || _store == null || h.Town == null)
                return;
            var were = new List<string>();
            MindWho mw = h.Mark != null && h.Mark.Exists() ? MindKnown(h.Mark.Handle, Game.GameTime) : null;
            if (mw != null && mw.Id != null)
                were.Add(mw.Id);
            _store.Tales.Add(Tales.Holdup(h.Town, Person.FirstOf(h.RobberName), Person.FirstOf(h.MarkName), how, _store.LastGameMin, CLOCK.GET_CLOCK_HOURS(), were));
            while (_store.Tales.Count > Tales.Kept)
                _store.Tales.RemoveAt(0);
            _talesNoted++;
            Log.Write("TALE noted: " + h.RobberName + " held up " + h.MarkName + " in " + h.Town + " (" + how + ")");
        }

        /// The Testing row: the nearest armed man near you holds up the nearest other within 16 m (no store, hour or lean needed).
        private string HoldupFire(int now, out string said)
        {
            said = null;
            string why;
            Ped me = Game.Player.Character;
            if (!HoldupScene(me, now, true, out why))
                return why ?? "nothing fit";
            said = _holdup != null ? _holdup.RobberName + " holds up " + _holdup.MarkName + " (" + _holdup.Answer + "); J for your word" : null;
            return null;
        }

        /// F7: the holdups this session.
        private string HoldupStatus()
        {
            if (!Holdup.On)
                return "off ([Scenes] StoreHoldups)";
            return _holdups + " holdups (talked down " + _holdupsDown + ", ran with it " + _holdupsRan + ", they bolted " + _holdupsBolted + ", cut short " + _holdupsDrawn + "); shots " +
                   (Holdup.Shots ? "on" : "off") + (_holdupLast != null ? "; last: " + _holdupLast : "");
        }
    }
}
