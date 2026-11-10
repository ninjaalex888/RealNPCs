// Walk someone home, from J (THREEHOUR item 4). Seeing someone home came first through Talk (SIXDAY item 1; WestworldScript.Escort.cs):
// after dark in a town, a woman, someone in drink or a timid man is offered "Let me see you home.", answers by their temper, and if
// it's yes they set off for their door with you beside them, wait if you fall behind, and thank you there. Talk is frozen, so this
// is the same walk from the event key: once a second after dark in a town, on foot, with nobody being seen home and nothing else on
// J, the nearest named grown-up within 4 m whom the Talk offer would go to (Fondness.EscortOffered: not seated, neither hating nor
// fearing you) gets J "Offer to walk them home", once a night each. Their answer is Talk's own (Fondness.Escort), said in their
// voice; yes starts Talk's own escort (EscortStartFor). Behind [Scenes] WalkHome. Log "FOND walk home".
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _walkHomeNextLook, _walkHomeAgainAt, _walkHomeOffered, _walkHomeYes;
        private Ped _walkHomeWho;
        private string _walkHomeId, _walkHomeName, _walkHomeTemper, _walkHomeRegard;
        private bool _walkHomeWoman, _walkHomeDrunk;
        private readonly HashSet<string> _walkHomeAsked = new HashSet<string>();   // "id|night": offered once a night each

        private void WalkHomeStep(int now)
        {
            if (_cfg == null || !_cfg.WalkHome || !_cfg.FondnessActs || _placeTown == null || _escort != null || _store == null || _engine == null || now < _walkHomeNextLook)
                return;
            _walkHomeNextLook = now + WalkHome.LookMs;
            if (_eventMenu != null || _menu != null || now < _walkHomeAgainAt || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil) || !Natives.Night())
                return;
            Safe("walk home", () => WalkHomeLook(now));
        }

        private void WalkHomeLook(int now)
        {
            Ped me = Game.Player.Character;
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || PED.IS_PED_IN_COMBAT(me.Handle, 0))
                return;
            string night = WalkHome.NightKey(_store.LastGameMin);
            Ped best = null;
            float bestD = WalkHome.NearMetres;
            string bid = null, bname = null, btemper = null, bregard = null;
            bool bwoman = false, bdrunk = false;
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Handle == me.Handle)
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d > bestD || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(p.Handle) || !IsGrownUp(p))
                    continue;
                int h = p.Handle;
                if (!Natives.Bystander(p, me.Handle, IsRecord(h)) || NotFree(h) || MayDirectWhyNot(p) != null || _director.Involves(h) || PED.IS_PED_USING_ANY_SCENARIO(h))
                    continue;
                string pid, pname, temper;
                if (!LastingId(p, out pid, out pname, out temper) || pid == null || _walkHomeAsked.Contains(pid + "|" + night))
                    continue;
                bool woman = !PED.IS_PED_MALE(h), drunk = InHisCups(p, null);
                string regard = SightRegard(p, pid);
                if (!Fondness.EscortOffered(true, true, woman, drunk, temper, regard, false))
                    continue;
                best = p; bestD = d; bid = pid; bname = pname; btemper = temper; bregard = regard; bwoman = woman; bdrunk = drunk;
            }
            if (best == null)
                return;
            _walkHomeWho = best;
            _walkHomeId = bid; _walkHomeName = bname; _walkHomeTemper = btemper; _walkHomeRegard = bregard; _walkHomeWoman = bwoman; _walkHomeDrunk = bdrunk;
            _walkHomeAsked.Add(bid + "|" + night);
            _walkHomeAgainAt = now + WalkHome.AgainMs;
            _walkHomeOffered++;
            PromptSet(WalkHome.Kind, new TownEvent { Kind = WalkHome.Kind, Town = _placeTown, AId = bid, AName = bname }, WalkHome.PromptMs);
        }

        /// J, "Offer to walk them home": their answer (Talk's own), and on a yes, Talk's own walk.
        private void WalkHomeOffer(TownEvent e)
        {
            Ped p = _walkHomeWho;
            if (e == null || p == null || !p.Exists() || p.IsDead || e.AId != _walkHomeId || _escort != null)
                return;
            Ped me = Game.Player.Character;
            int h = p.Handle, now = Game.GameTime;
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2500, 0f, 0f, 0f);
            Hint(Fondness.EscortLabel(), 2500);   // what you said: Talk's own words for it
            InteractAnswer a = Fondness.Escort(_walkHomeTemper, _walkHomeWoman, _walkHomeDrunk, _walkHomeRegard, false, false, _walkHomeId + "|walkhome|" + _store.LastGameMin / 1440);
            if (a.Line != null)
                Feed.Say(Person.FirstOf(_walkHomeName), a.Line, Speech.For(h, a.Line, me.Handle, 0, 0, a.Friendly, "chat"), h, toYou: true);
            if (a.Then == "escort" && EscortStartFor(h, _walkHomeId, _walkHomeName, _walkHomeTemper, _walkHomeWoman, _walkHomeDrunk, now))
            {
                _walkHomeYes++;
                Log.Write("FOND walk home: " + _walkHomeName + " says yes (J)");
            }
            else
                Log.Write("FOND walk home: " + _walkHomeName + " says no (" + (_walkHomeTemper ?? "?") + ")");
        }

        private string WalkHomeStatus()
        {
            return _walkHomeOffered + " offered on J, " + _walkHomeYes + " said yes";
        }
    }
}
