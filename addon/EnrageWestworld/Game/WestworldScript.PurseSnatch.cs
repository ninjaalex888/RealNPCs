// A purse snatched, the game side (FOURTEENHOUR item 20; Core/PurseSnatch.cs; [Scenes] PurseSnatch). On the pickpocket's own theft
// (WestworldScript.Frontier.cs: the thief runs, fair game, the lawman's chase): at her cry, one look at the men near her (within 20
// m, free, the mind's people), the first whose nature makes it his gives chase at a sprint (the lawman's chase order, seen) and,
// within arm's reach, brings him down (the lawman's tackle, seen); 2.5 s on he hands her purse back, she thanks him, their tie warms,
// the town's news has it, and the thief, on his feet again, is off empty-handed. Too quick for him (20 s, or 40 m ahead): he gives it up.
// Your own catch asks first, on J: make him give it back (a hot one may fight you: his fight is the game's), hand him to the law (a
// lawman within 60 m walks over and ties him, the hogtie seen), or let him run; unasked in 15 s, he gives it back, as a catch always
// did. Looks ride on TheftTick; never a frame of their own.
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private Theft _theftAsk;
        private int _snatchChases, _snatchCatches, _snatchLost, _snatchGaveBack, _snatchFought, _snatchToLaw, _snatchLawTook, _snatchLetGo, _snatchSureUntil;
        private string _snatchLast;

        /// The bystander's chase: one look at her cry; then the run, the tackle, or giving it up. True while it has the theft in hand.
        private bool ChaseStep(Theft t, Ped me, int now)
        {
            int h = t.Thief.Handle;
            if (t.Chaser == null)
            {
                if (t.ChaseLooked || now - t.Started < 1500)
                    return false;
                t.ChaseLooked = true;   // one look, at her cry
                Ped mark = t.Mark;
                RDR2.Math.Vector3 at = mark != null && mark.Exists() ? mark.Position : t.Thief.Position;
                bool sure = now < _snatchSureUntil;   // the Testing row's: the first man near goes
                MindWho mw = mark != null && mark.Exists() ? MindKnown(mark.Handle, now) : null;
                foreach (Ped p in Peds.All().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && p.Handle != h && (mark == null || p.Handle != mark.Handle) &&
                                                         PED.IS_PED_MALE(p.Handle) && PED.IS_PED_HUMAN(p.Handle) && !Natives.IsLaw(p) && !Natives.IsChild(p) && p.Position.DistanceTo(at) < PurseSnatch.ChaseFromMetres &&
                                                         !PED.IS_PED_ON_MOUNT(p.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) && !PED.IS_PED_IN_COMBAT(p.Handle, 0) && !NotFree(p.Handle) &&
                                                         !Spawner.IsOurs(p.Handle) && !PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                                              .OrderBy(p => p.Position.DistanceTo(at)).Take(4).ToList())
                {
                    MindWho w = MindKnown(p.Handle, now);
                    if (w == null || w.Id == null || w.Src == null)
                        continue;
                    Traits tr = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                    bool friend = mw != null && mw.Id != null && LadderWarmth(w.Id, mw.Id) >= 2;
                    if (!sure && !PurseSnatch.Chases(tr, friend, t.MarkWoman, t.Started + "|chase|" + w.Id))
                        continue;
                    t.Chaser = p;
                    t.ChaserName = w.Name;
                    t.ChaserId = w.Id;
                    t.ChaseSince = now;
                    t.ChaseLookAt = now + 2000;
                    TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                    TASK.TASK_GO_TO_ENTITY(p.Handle, h, -1, 1.5f, 3f, 0f, 0);   // at a sprint (the lawman's chase order)
                    LawSay(p, Person.FirstOf(w.Name), PurseSnatch.Shout(t.Started + "|shout"), "SHOUT_STOP");
                    _snatchChases++;
                    Log.Write("SNATCH " + w.Name + " gives chase to the thief" + (friend ? " (a friend of " + (t.MarkName ?? "hers") + ")" : "") + (sure ? " (a test)" : ""));
                    return false;
                }
                return false;
            }
            Ped c = t.Chaser;
            if (!c.Exists() || c.IsDead || PED.IS_PED_IN_COMBAT(c.Handle, me.Handle))
            {
                t.Chaser = null;   // gone, or at you now (the game's): no second man
                return false;
            }
            float d = c.Position.DistanceTo(t.Thief.Position);
            if (d < 1.8f && !PED.IS_PED_RAGDOLL(h))
            {
                // the tackle: down he goes, and the man stands over him (the lawman's, seen)
                Natives.Ragdoll(h, 4000, 6000);
                TASK.CLEAR_PED_TASKS(c.Handle, true, false);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(c.Handle, h, 6000, 0f, 0f, 0f);
                t.ChaseHeld = true;
                t.ChaseHeldAt = now;
                LawSay(c, Person.FirstOf(t.ChaserName), PurseSnatch.Got(t.Started + "|got"), "WON_FIGHT");
                _snatchCatches++;
                Log.Write("SNATCH " + t.ChaserName + " brings the thief down after " + (now - t.ChaseSince) / 1000 + " s");
                return true;
            }
            if (now - t.ChaseSince > PurseSnatch.ChaseMostMs || d > 40f)
            {
                TASK.CLEAR_PED_TASKS(c.Handle, true, false);
                WanderLater(c.Handle, now + 3000);
                LawSay(c, Person.FirstOf(t.ChaserName), PurseSnatch.Away(t.Started + "|away"), "GENERIC_CURSE_MED");
                t.Chaser = null;
                _snatchLost++;
                Log.Write("SNATCH " + t.ChaserName + " lost him (" + (int)d + " m behind, " + (now - t.ChaseSince) / 1000 + " s)");
                return false;
            }
            if (now >= t.ChaseLookAt)
            {
                t.ChaseLookAt = now + 2000;
                TASK.TASK_GO_TO_ENTITY(c.Handle, h, -1, 1.5f, 3f, 0f, 0);   // the thief changes course: the order again
            }
            return false;
        }

        /// The man who ran him down hands her purse back; the thief, on his feet again, is off with nothing.
        private void ChaseHeldStep(Theft t, int now)
        {
            if (now - t.ChaseHeldAt < 2500)
                return;
            _theft = null;
            int h = t.Thief.Handle;
            _thieves.Remove(h);
            if (t.Blip != null && t.Blip.Exists())
                t.Blip.Delete();
            Ped c = t.Chaser, mark = t.Mark, me = Game.Player.Character;
            string seed = t.Started + "|held";
            string chaser = Person.FirstOf(t.ChaserName ?? "A man");
            bool markHere = mark != null && mark.Exists() && !mark.IsDead;
            if (c != null && c.Exists())
            {
                if (markHere && mark.Position.DistanceTo(c.Position) < 40f)
                    TASK.TASK_GO_TO_ENTITY(c.Handle, mark.Handle, 15000, 1.5f, 1.2f, 0f, 0);   // back to her with it
                SayLater(600, chaser, PurseSnatch.Handed(t.MarkWoman, seed), c.Handle, "mild");
                WanderLater(c.Handle, now + 16000);
            }
            if (markHere && mark.Position.DistanceTo(me.Position) < 90f)
                SayLater(3500, t.MarkName ?? (t.MarkWoman ? "The woman" : "The man"), PurseSnatch.Thanks(t.MarkWoman, seed), mark.Handle, "thanks");
            MindWho mw = markHere ? MindKnown(mark.Handle, now) : null;
            if (_store != null && t.ChaserId != null && mw != null && mw.Id != null)
                Ties.Note(_store.Ties, t.ChaserId, t.ChaserName, mw.Id, mw.Name, t.Town, "help", Bridge.Now());   // he did her a good turn (a warmer tie)
            if (t.Lawman != null && t.Lawman.Exists())
            {
                TASK.CLEAR_PED_TASKS(t.Lawman.Handle, true, false);   // the lawman who was after him too: back to his rounds
                WanderLater(t.Lawman.Handle, now + 3000);
            }
            if (t.Thief.Exists() && !t.Thief.IsDead)
            {
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_SMART_FLEE_PED(h, c != null && c.Exists() ? c.Handle : me.Handle, 120f, -1, 0, 3f, 0);   // up and off, empty-handed
            }
            NewsPush(t.Town ?? "Around here", PurseSnatch.News(chaser, Person.FirstOf(t.MarkName ?? (t.MarkWoman ? "a lady" : "a man")), t.MarkWoman, seed), "news");
            _snatchLast = chaser + " ran the thief down and gave " + (t.MarkName ?? "her") + " back what he took";
            Log.Write("SNATCH " + _snatchLast);
        }

        /// A man near who was after the thief stops: you have him now.
        private void ChaseLetGo(Theft t, int now)
        {
            if (t.Chaser == null || !t.Chaser.Exists())
                return;
            TASK.CLEAR_PED_TASKS(t.Chaser.Handle, true, false);
            WanderLater(t.Chaser.Handle, now + 4000);
            t.Chaser = null;
        }

        /// Your catch, alive: J asks what's done with him first (15 s; unasked, he gives it back). False when J is taken just now.
        private bool ThiefAskFirst(Theft t, int now)
        {
            if (!PurseSnatch.On || t.PlayerMark || _eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return false;
            t.Asking = true;
            t.AskUntil = now + PurseSnatch.AskMs;
            ChaseLetGo(t, now);
            _theftAsk = t;
            PromptSet(PurseSnatch.Kind, new TownEvent { Kind = PurseSnatch.Kind, Town = t.Town, AName = MindNameOf(t.Thief.Handle) ?? "the thief" }, PurseSnatch.AskMs);
            Log.Write("SNATCH you have the thief: J says what's done with him (15 s; unasked, he gives it back)");
            return true;
        }

        private void ThiefAskStep(Theft t, int now)
        {
            int h = t.Thief.Handle;
            if (t.ToLaw != null)
            {
                ToLawStep(t, now);
                return;
            }
            if (!t.HandsUp && !PED.IS_PED_RAGDOLL(h) && !PED.IS_PED_HOGTIED(h) && !PED.IS_PED_LASSOED(h))
            {
                t.HandsUp = true;
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_HANDS_UP(h, PurseSnatch.AskMs + 5000, Game.Player.Character.Handle, -1, false);   // hands up while you decide (the hogtie check's, seen)
            }
            if (_eventMenu != null && _eventMenu.Mode == PurseSnatch.Kind)
            {
                t.AskUntil = Math.Max(t.AskUntil, now + 1500);   // the menu open: the clock waits
                return;
            }
            if (now > t.AskUntil)
                ThiefChoose("thief:back");   // unasked: he gives it back, as a catch always did
        }

        /// A lawman free within reach of the thief, nearest first; null for none.
        private Ped NearLawFor(Ped thief, float metres)
        {
            if (thief == null || !thief.Exists())
                return null;
            Ped me = Game.Player.Character;
            return Peds.All().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && Natives.IsLaw(p) && !PED.IS_PED_ON_MOUNT(p.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) &&
                                         !PED.IS_PED_IN_COMBAT(p.Handle, me.Handle) && p.Position.DistanceTo(thief.Position) < metres && !NotFree(p.Handle))
                             .OrderBy(p => p.Position.DistanceTo(thief.Position)).FirstOrDefault();
        }

        /// J: what's done with the thief you caught.
        private void ThiefChoose(string id)
        {
            Theft t = _theftAsk;
            _theftAsk = null;
            if (t == null || !t.Asking || t.Thief == null || !t.Thief.Exists() || !ReferenceEquals(t, _theft))
                return;
            int now = Game.GameTime;
            Ped me = Game.Player.Character;
            int h = t.Thief.Handle;
            string seed = t.Started + "|ask";
            if (id == "thief:law")
            {
                Ped law = NearLawFor(t.Thief, PurseSnatch.LawNearMetres);
                if (law != null)
                {
                    _theftAsk = t;   // still his until the lawman has him (ToLawStep)
                    t.ToLaw = law;
                    t.ToLawSince = now;
                    t.Lawman = law;   // (NotFree: the theft's lawman)
                    t.ChaseLookAt = now + 2000;
                    TASK.CLEAR_PED_TASKS(law.Handle, true, false);
                    TASK.TASK_GO_TO_ENTITY(law.Handle, h, -1, 1.8f, 2f, 0f, 0);
                    SpeechCaption("You", PurseSnatch.ToLawLine(seed), me.Handle, 2500, false, false);
                    _snatchToLaw++;
                    Log.Write("SNATCH you hand the thief to the law: a lawman " + (int)law.Position.DistanceTo(t.Thief.Position) + " m off comes for him");
                    return;
                }
            }
            if (id == "thief:go")
            {
                t.Asking = false;
                _theft = null;
                _thieves.Remove(h);
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_SMART_FLEE_PED(h, me.Handle, 100f, -1, 0, 3f, 0);
                if (t.Mark != null && t.Mark.Exists() && !t.Mark.IsDead && t.Mark.Position.DistanceTo(me.Position) < 60f)
                    SayLater(800, t.MarkName ?? (t.MarkWoman ? "The woman" : "The man"), PurseSnatch.LetGoLine(t.MarkWoman, seed), t.Mark.Handle, "angry");
                _snatchLetGo++;
                _snatchLast = "you let the thief run";
                Log.Write("SNATCH you let the thief run, with what he took");
                return;
            }
            // give it back (or no lawman after all): by his nature, a hot one may fight you for it
            MindWho w = MindKnown(h, now);
            bool gives = PurseSnatch.GivesBack(w != null && w.P != null ? w.P.Personality : null, seed);
            SpeechCaption(w != null && w.Name != null ? Person.FirstOf(w.Name) : "The thief", PurseSnatch.GiveLine(gives, seed), h, 2500, false, false);
            t.Asking = false;
            if (!gives)
            {
                _theft = null;   // (still a thief: fair game)
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_COMBAT_PED(h, me.Handle, 0, 16);   // his fight is the game's from here (and enRAGE's): we let go of him
                _snatchFought++;
                _snatchLast = "the thief fought you for it";
                Log.Write("SNATCH the thief won't give it back: he fights you for it");
                return;
            }
            _snatchGaveBack++;
            _snatchLast = "the thief gave it back";
            ResolveTheft(t, "caught", now);   // her thanks and a good name (as a catch always had)
        }

        /// The lawman walks over to the thief you hold and ties him (the hogtie, seen), 30 s at most.
        private void ToLawStep(Theft t, int now)
        {
            Ped l = t.ToLaw;
            int h = t.Thief.Handle;
            if (l == null || !l.Exists() || l.IsDead || PED.IS_PED_IN_COMBAT(l.Handle, Game.Player.Character.Handle) || now - t.ToLawSince > 30000)
            {
                t.ToLaw = null;
                t.Lawman = null;
                ThiefChoose("thief:back");   // he never came: the thief gives it back to you
                return;
            }
            if (l.Position.DistanceTo(t.Thief.Position) < 2.2f)
            {
                _theftAsk = null;
                t.Asking = false;
                Natives.Ragdoll(h, 1500, 2500);
                TASK.TASK_HOGTIE_TARGET_PED(l.Handle, h);
                _hogtieBy = l;
                _hogtieWho = t.Thief;
                _hogtieCheckAt = now + 6000;
                LawSay(l, "The deputy", LawChase.Got(t.Started + "|tolaw"), "WON_FIGHT");
                WanderLater(l.Handle, now + 20000);
                _snatchLawTook++;
                _snatchLast = "the law took the thief you caught";
                ResolveTheft(t, "caught", now);   // her thanks and a good name: you caught him
                return;
            }
            if (now >= t.ChaseLookAt)
            {
                t.ChaseLookAt = now + 2000;
                TASK.TASK_GO_TO_ENTITY(l.Handle, h, -1, 1.8f, 2f, 0f, 0);
            }
        }

        /// The Testing row: the pickpocket's scene now, and the first man near her gives chase.
        private string SnatchFire(int now, string seed, out string said)
        {
            said = null;
            if (!PurseSnatch.On)
                return "off ([Scenes] PurseSnatch)";
            _snatchSureUntil = now + 30000;
            return FireScene("pickpocket", now, seed, out said);
        }

        /// F7: the chases, the catches, and what you did with yours.
        private string SnatchStatus()
        {
            if (!PurseSnatch.On)
                return "off ([Scenes] PurseSnatch)";
            return "chases " + _snatchChases + " (brought down " + _snatchCatches + ", lost " + _snatchLost + "); yours: gave it back " + _snatchGaveBack + ", fought you " + _snatchFought +
                   ", to the law " + _snatchToLaw + " (taken " + _snatchLawTook + "), let run " + _snatchLetGo + (_snatchLast != null ? "; last: " + _snatchLast : "");
        }
    }
}
