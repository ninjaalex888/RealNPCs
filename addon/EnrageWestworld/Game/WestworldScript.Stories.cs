// A story over days, the game side (FOURTEENHOUR reserve R10; Core/Stories.cs, Core/Engine.Stories.cs; [World] Stories). A look every
// 2 s: in Annesburg with a father's son to find, a townsman is cast for the son (the kin grudges' way: a grown man 30-120 m off,
// unseen, nobody's, named and kept), walks up to you and says who he is; J then: carry his letter home, or the next train for him
// (the engine's StoryFound). In the father's town with his letter carried, it's done (the engine's StoryAtTown). Never a frame.
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class StoryWalk
        {
            public Ped Ped;
            public StoryRun S;
            public Resident R;
            public int Until, SaidAt;
            public bool Chose;
        }

        private StoryWalk _storyWalk;
        private int _storyLookAt, _storyCastAt, _storiesMet;
        private string _storyLast;

        /// From the scenes' helpers.
        private void StoriesStep(int now)
        {
            if (!Stories.On || _store == null || _engine == null || _placeTown == null || now < _storyLookAt)
                return;
            _storyLookAt = now + 2000;
            Ped me = Game.Player.Character;
            long g = _store.LastGameMin;
            if (_engine.StoryAtTown(_placeTown, g))
                _storyLast = "a letter carried home to " + _placeTown;
            StoryWalk w = _storyWalk;
            if (w != null)
            {
                StoryWalkStep(w, me, now, g);
                return;
            }
            StoryRun s = _engine.SonToFind(_placeTown);
            if (s == null || now < _storyCastAt || MISC.GET_MISSION_FLAG() || WalkUpGoing() || _director.Busy)
                return;
            _storyCastAt = now + 30000;   // a cast tried at most every 30 s
            Ped body = null;
            float bestD = 120f;
            foreach (Ped p in Peds.All())
            {
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || !IsGrownUp(p) || !PED.IS_PED_MALE(h))
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d < 30f || d >= bestD || ENTITY.IS_ENTITY_ON_SCREEN(h) || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17))
                    continue;
                if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_ON_MOUNT(h) || ResidentWhy(p) != null || PostKindOf(p) != null || Natives.StoryModel(p) ||
                    _resBind.ContainsKey(h) || _director.Involves(h) || !MayDirect(p) || NotFree(h) || (_bridge != null && _bridge.Looked(h)))
                    continue;
                body = p;
                bestD = d;
            }
            if (body == null)
                return;   // nobody free and unseen just now: again in thirty seconds
            int bh = body.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(bh);
            Resident res = _engine.MeetResident(model, false, Stories.SonTown, "a miner", bh + "|son|" + now, g, false);
            res.Name = s.SonName;
            _engine.KeepResident(res, g);
            _resBind[bh] = new ResBind { R = res, Model = model };
            PED.SET_PED_KEEP_TASK(bh, true);
            TASK.CLEAR_PED_TASKS(bh, true, false);
            TASK.TASK_GO_TO_ENTITY(bh, me.Handle, 45000, 2.0f, 1.6f, 0f, 0);
            TASK.TASK_LOOK_AT_ENTITY(bh, me.Handle, 45000, 0, 51, 0);
            _storyWalk = new StoryWalk { Ped = body, S = s, R = res, Until = now + 45000 };
            Log.Write("STORY " + s.SonName + " heard a stranger was asking after him: he comes over (" + (int)bestD + " m)");
        }

        private void StoryWalkStep(StoryWalk w, Ped me, int now, long g)
        {
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0) || _director.Involves(w.Ped.Handle) || MayDirectWhyNot(w.Ped) != null)
            {
                StoryWalkEnd(w, "something else took him");
                return;
            }
            int h = w.Ped.Handle;
            if (w.SaidAt == 0)
            {
                if (w.Ped.Position.DistanceTo(me.Position) < 3f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 6000, 0f, 0f, 0f);
                    string line = Stories.SonMeets(w.S.AskerName, w.S.AskerTown, w.S.SonName, w.S.Id);
                    Feed.Say(Person.FirstOf(w.S.SonName), line, Speech.When(h, "greet", me.Handle), h, toYou: true);
                    w.SaidAt = now;
                    _storiesMet++;
                    if (_eventMenu == null && (_eventPrompt == null || Game.GameTime > _eventPromptUntil))
                        PromptSet(Stories.Kind, new TownEvent { Kind = Stories.Kind, Town = _placeTown, AName = w.S.SonName }, Stories.PromptMs);
                    Log.Write("STORY " + w.S.SonName + " to your face: " + line);
                }
                else if (now > w.Until)
                    StoryWalkEnd(w, "he never reached you");
                return;
            }
            bool menuOpen = _eventMenu != null && _eventMenu.Mode == Stories.Kind;
            if (w.Chose || (!menuOpen && now - w.SaidAt > Stories.PromptMs + 3000))
                StoryWalkEnd(w, w.Chose ? "he has your word" : "no word from you: he'll wait to hear");
        }

        /// J's row: his letter carried home, or the next train for him.
        private void StoryChoose(string id)
        {
            StoryWalk w = _storyWalk;
            if (w == null || w.SaidAt == 0 || w.Chose)
                return;
            string choice = id == "story:train" ? "train" : "carry";
            w.Chose = true;
            Feed.Say(Person.FirstOf(w.S.SonName), Stories.SonAnswer(choice, w.S.Id), Speech.When(w.Ped.Handle, "thanks", Game.Player.Character.Handle), w.Ped.Handle, toYou: true);
            _engine.StoryFound(w.S, w.R != null ? w.R.Id : null, choice, _store.LastGameMin);
            if (choice == "carry")
                Hint("You carry " + Person.FirstOf(w.S.SonName) + "'s letter: take it to " + w.S.AskerTown + ".", 4000);
            _storyLast = w.S.SonName + ": " + (choice == "carry" ? "his letter to carry home" : "the train home");
        }

        private void StoryWalkEnd(StoryWalk w, string why)
        {
            _storyWalk = null;
            if (_eventPrompt == Stories.Kind)
                PromptClear();
            if (w.Ped != null && w.Ped.Exists() && !w.Ped.IsDead)
            {
                PED.SET_PED_KEEP_TASK(w.Ped.Handle, false);
                TASK.TASK_CLEAR_LOOK_AT(w.Ped.Handle);
                if (!PED.IS_PED_IN_COMBAT(w.Ped.Handle, 0) && !_director.Involves(w.Ped.Handle))
                    WanderLater(w.Ped.Handle, Game.GameTime + 2500);
            }
            Log.Write("STORY " + (w.S != null ? w.S.SonName : "the son") + ": " + why);
        }

        /// He's walking up to you, or waiting on your word: busy for the other features.
        private bool StoryWalking(int h)
        {
            StoryWalk w = _storyWalk;
            return w != null && w.Ped != null && w.Ped.Handle == h;
        }

        /// F7: the stories.
        private string StoriesStatus()
        {
            if (!Stories.On)
                return "off ([World] Stories)";
            return _storiesMet + " sons found this session; " + _engine.StoriesStatus() + (_storyLast != null ? "; last: " + _storyLast : "");
        }
    }
}
