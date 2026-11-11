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
        private int _doorKin, _doorSecond, _doorPledges, _doorWives, _doorBarkeeps;
        private string _doorBarkeepLast;

        /// His own wife near the door (DEPTH item 5): within 25 m of him, already in the street (never fetched), on her feet, free,
        /// grown and nobody's scene; his wife by the name his story gives her, or a warm sweetheart tie between them. Null for none.
        private Ped DoorWifeNear(Someone him, Ped hisPed, Ped herPed, int now)
        {
            if (him == null || hisPed == null || !hisPed.Exists() || _store == null)
                return null;
            Ped me = Game.Player.Character;
            string spouse = him.P != null && him.P.SpouseName != null && !him.P.SpouseName.StartsWith("a ", System.StringComparison.Ordinal) ? him.P.SpouseName : null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || p.Handle == hisPed.Handle || (herPed != null && p.Handle == herPed.Handle) || PED.IS_PED_MALE(p.Handle) ||
                    p.Position.DistanceTo(hisPed.Position) > 25f)
                    continue;
                MindWho w;
                if (!_mindWho.TryGetValue(p.Handle, out w) || w.Id == null || w.Name == null)
                    continue;
                bool his = spouse != null && string.Equals(spouse, w.Name, System.StringComparison.OrdinalIgnoreCase);
                for (int j = 0; j < _store.Ties.Count && !his; j++)
                {
                    SceneTie t = _store.Ties[j];
                    his = t != null && t.Warmth > 0 && Ties.Sweethearts(t) && t.Has(him.Id) && t.Has(w.Id);
                }
                if (!his || Natives.StoryModel(p) || !IsGrownUp(p) || PED.IS_PED_USING_ANY_SCENARIO(p.Handle) || NotFree(p.Handle) || _director.Involves(p.Handle) || MayDirectWhyNot(p) != null)
                    continue;
                _doorWives++;
                return p;
            }
            return null;
        }

        /// A friend of the marked man's within 8 m of him (DEPTH item 13): a warm friend's tie (Ties.Friends), on his feet, free, grown,
        /// no story model, nobody's scene; null for none. He's the limp's tale's third: he sets it straight, or backs him.
        private Ped LimpFriendNear(Someone him, Ped hisPed, Ped other, int now)
        {
            if (him == null || hisPed == null || !hisPed.Exists() || _store == null)
                return null;
            Ped me = Game.Player.Character;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || p.Handle == hisPed.Handle || (other != null && p.Handle == other.Handle) || p.Position.DistanceTo(hisPed.Position) > 8f)
                    continue;
                MindWho w;
                if (!_mindWho.TryGetValue(p.Handle, out w) || w.Id == null)
                    continue;
                SceneTie t = _store.Ties.FirstOrDefault(x => x != null && x.Has(him.Id) && x.Has(w.Id));
                if (t == null || !Ties.Friends(t) || Natives.StoryModel(p) || !IsGrownUp(p) || PED.IS_PED_USING_ANY_SCENARIO(p.Handle) || NotFree(p.Handle) || _director.Involves(p.Handle) || MayDirectWhyNot(p) != null)
                    continue;
                return p;
            }
            return null;
        }

        /// From OnPlayedOut, a "marchedhome" (DEPTH item 5): the town's barkeep, within 30 m of the door, thinks less of the church
        /// woman who emptied his bar (a soured tie, which Opinions reads as contempt), says so to the next man at his bar (a caption
        /// from him: a counter keeper is never given an order), and thinks it. a, b: the scene's first two (the church woman and him).
        private void BarkeepSour(Ped a, Ped b)
        {
            if (!Temperance.DoorOn || _store == null || a == null || !a.Exists())
                return;
            Ped churchwoman = PED.IS_PED_MALE(a.Handle) ? b : a;
            if (churchwoman == null || !churchwoman.Exists())
                return;
            Ped me = Game.Player.Character;
            Ped keep = null;
            foreach (Ped p in Peds.All())
                if (p != null && p.Exists() && !p.IsDead && p.Position.DistanceTo(churchwoman.Position) < 30f && PostKindOf(p) != null && PostKindOf(p).Role == "the barkeep")
                {
                    keep = p;
                    break;
                }
            if (keep == null)
            {
                Log.Detail("TEMPERANCE no barkeep within 30 m of the door to see it");
                return;
            }
            Resident h = HolderAt(keep, PostKindOf(keep));
            MindWho cw = MindKnown(churchwoman.Handle, Game.GameTime);
            if (h == null || cw == null || cw.Id == null)
                return;
            long g = Bridge.Now();
            Ties.Note(_store.Ties, h.Id, h.Name, cw.Id, cw.Name, _placeTown, "soured", g, cw.Id);   // her doing, his view (Opinions: contempt)
            string seed = h.Id + "|" + cw.Id + "|" + g / 60;
            Ped next = null;
            float best = 4f;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == keep.Handle || p.Handle == me.Handle || p.Handle == churchwoman.Handle || (b != null && p.Handle == b.Handle) || !PED.IS_PED_MALE(p.Handle))
                    continue;
                float d = p.Position.DistanceTo(keep.Position);
                if (d < best)
                {
                    best = d;
                    next = p;
                }
            }
            string first = Person.FirstOf(h.Name), said = null;
            if (next != null)
            {
                said = Temperance.BarkeepLine(seed);
                SpeechCaption(first ?? "The barkeep", said, keep.Handle, 4000);   // to the next man at his bar
            }
            string thought = Temperance.BarkeepThought(Person.FirstOf(cw.Name), seed);
            _ticker.Push((first ?? "The barkeep") + ", thinking", thought, "near", keep.Handle);   // over his head (as the keepers' own thoughts)
            _doorBarkeeps++;
            _doorBarkeepLast = (first ?? "the barkeep") + " soured on " + cw.Name + (said != null ? " and said so" : "");
            Log.Write("TEMPERANCE barkeep " + (h.Name ?? "?") + " thinks less of " + cw.Name + " for walking his custom home" + (said != null ? "; to the next man at the bar: \"" + said + "\"" : "; nobody at the bar to tell"));
        }

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
            if (!Temperance.DoorOn || !Tales.On || _store == null || _store.Tales == null || _placeTown == null)
                return;
            Ped man = a != null && a.Exists() && PED.IS_PED_MALE(a.Handle) ? a : b;
            MindWho w = man != null && man.Exists() ? MindKnown(man.Handle, Game.GameTime) : null;
            if (w == null || w.Name == null)
                return;
            _store.Tales.Add(Tales.Pledge(_placeTown, Person.FirstOf(w.Name), _store.LastGameMin, CLOCK.GET_CLOCK_HOURS()));
            while (_store.Tales.Count > Tales.Kept)
                _store.Tales.RemoveAt(0);
            _doorPledges++;
            Log.Write("TALE noted: " + w.Name + " signed the pledge at the saloon door in " + _placeTown);
        }

        /// Pledges signed at this town's door this week (the preacher's word on Sunday).
        private int PledgesThisWeek()
        {
            if (_store == null || _placeTown == null || _store.Tales == null || _store.Tales.Count == 0)
                return 0;
            long g = _store.LastGameMin;
            return _store.Tales.Count(t => t.Kind == "pledge" && t.Town == _placeTown && g >= t.At && g - t.At < 7 * 1440);
        }

        /// F7: the door this session.
        private string TemperanceDoorStatus()
        {
            if (!Temperance.DoorOn)
                return "off ([Scenes] TemperanceDoor, or [Scenes] Temperance)";
            return "a second woman beside her " + _doorSecond + ", her kin stood up " + _doorKin + ", pledges signed " + _doorPledges + "; this week here " + PledgesThisWeek() +
                   "; his wife near to walk him home " + _doorWives + ", the barkeep soured " + _doorBarkeeps + (_doorBarkeepLast != null ? " (the last: " + _doorBarkeepLast + ")" : "");
        }
    }
}
