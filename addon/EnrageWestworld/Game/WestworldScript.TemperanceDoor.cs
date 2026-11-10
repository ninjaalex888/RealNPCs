// The temperance ladies at the saloon door, the game side (FOURTEENHOUR item 25; Core/Temperance.cs; [Scenes] TemperanceDoor with
// [Scenes] Temperance). The reason is the street's own (Core/Reasons.cs: a churchwoman and a man in drink by the door at dusk); here,
// a second woman of the church beside her (within 6 m: a cast at the door, never a crowd walked over to watch, Lex's rule) is cast as
// the scene's third; or, with a hothead or a proud man in drink, her kin within 25 m (her Person's KinId) is cast as A and the drinker
// as B, she the third, and he walks over and answers the insult with his fists (the fight between the first two, as the Director has
// fights). A pledge signed (the scene's kind "pledge") goes into the town's tales (Core/Tales.cs) and the week's count the preacher
// reads on Sunday. Looks only when the scene is played; never a frame.
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _doorKin, _doorSecond, _doorPledges;

        /// Her kin within 25 m: the man her Person names as kin, on his feet, free, nobody's scene; null for none.
        private Ped DoorKinNear(Someone her, Ped herPed, Ped hisPed, int now, out string kinId)
        {
            kinId = her != null && her.P != null ? her.P.KinId : null;
            if (kinId == null || herPed == null || !herPed.Exists())
                return null;
            Ped me = Game.Player.Character;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || p.Handle == herPed.Handle || (hisPed != null && p.Handle == hisPed.Handle) || !PED.IS_PED_MALE(p.Handle) ||
                    p.Position.DistanceTo(herPed.Position) > 25f)
                    continue;
                MindWho w;
                if (!_mindWho.TryGetValue(p.Handle, out w) || w.Id != kinId || NotFree(p.Handle) || _director.Involves(p.Handle) || MayDirectWhyNot(p) != null)
                    continue;
                _doorKin++;
                return p;
            }
            return null;
        }

        /// A second woman of the church beside her (within 6 m), on her feet, free; null for none.
        private Ped ChurchwomanBeside(Ped herPed, Ped hisPed, int now)
        {
            if (herPed == null || !herPed.Exists())
                return null;
            Ped me = Game.Player.Character;
            Ped best = null;
            float bestD = 6f;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || p.Handle == herPed.Handle || (hisPed != null && p.Handle == hisPed.Handle) || PED.IS_PED_MALE(p.Handle))
                    continue;
                float d = p.Position.DistanceTo(herPed.Position);
                if (d >= bestD || PED.IS_PED_USING_ANY_SCENARIO(p.Handle) || NotFree(p.Handle) || _director.Involves(p.Handle) || MayDirectWhyNot(p) != null)
                    continue;
                MindWho w = MindKnown(p.Handle, now);
                if (w == null || w.P == null || (w.P.Devotion != "Devout" && w.P.Devotion != "Churchgoer") || InnerLife.TradeGroup(w.P.Trade) == "girl")
                    continue;
                best = p;
                bestD = d;
            }
            if (best != null)
                _doorSecond++;
            return best;
        }

        /// From OnPlayedOut: a man signed the pledge at the door: the town's tale of it (and back at the bar by Friday, as often as not).
        private void PledgeNote(Ped a, Ped b)
        {
            if (!Temperance.DoorOn || !Tales.On || _store == null || _placeTown == null)
                return;
            Ped man = a != null && a.Exists() && PED.IS_PED_MALE(a.Handle) ? a : b;
            MindWho w = man != null && man.Exists() ? MindKnown(man.Handle, Game.GameTime) : null;
            if (w == null || w.Name == null)
                return;
            _store.Tales.Add(Tales.Pledge(_placeTown, Person.FirstOf(w.Name), _store.LastGameMin));
            while (_store.Tales.Count > Tales.Kept)
                _store.Tales.RemoveAt(0);
            _doorPledges++;
            Log.Write("TALE noted: " + w.Name + " signed the pledge at the saloon door in " + _placeTown);
        }

        /// Pledges signed at this town's door this week (the preacher's word on Sunday).
        private int PledgesThisWeek()
        {
            if (_store == null || _placeTown == null || _store.Tales.Count == 0)
                return 0;
            long g = _store.LastGameMin;
            return _store.Tales.Count(t => t.Kind == "pledge" && t.Town == _placeTown && g >= t.At && g - t.At < 7 * 1440);
        }

        /// F7: the door this session.
        private string TemperanceDoorStatus()
        {
            if (!Temperance.DoorOn)
                return "off ([Scenes] TemperanceDoor, or [Scenes] Temperance)";
            return "a second woman beside her " + _doorSecond + ", her kin stood up " + _doorKin + ", pledges signed " + _doorPledges + "; this week here " + PledgesThisWeek();
        }
    }
}
