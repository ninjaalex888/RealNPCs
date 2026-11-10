// Rowdy drunks at night, the game side (FOURTEENHOUR reserve R5; Core/Rowdy.cs; [Scenes] RowdyDrunks). The reason is the street's own
// (Reasons: a man in drink picks on a man going by, at night out of doors); here, as the pair's reason is played (PlayPairReason), his
// friend in drink beside him is found for the cast's third (the song, the walk-over, the walk home: the Director's meet, approach and
// homewith, the drunk carried home's walk); with nobody beside him it's the saloon drunk's scene (he falls on his face). A fight is
// the Director's between the drunk and the man he picked on, so the brawl's gates, joiners and cascade are the brawl's. The Testing
// row and F7. Never a frame of its own.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _rowdyPicked, _rowdyBlows, _rowdyTests;

        /// The drunk's friend in drink beside him: a man the mind knows within 8 m of him, in his cups, warm to him, free; or null.
        private Ped DrinkingFriendBeside(Ped drunk, Ped other, int now)
        {
            MindWho dw = drunk != null && drunk.Exists() ? MindKnown(drunk.Handle, now) : null;
            if (dw == null || dw.Id == null)
                return null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == drunk.Handle || (other != null && p.Handle == other.Handle) || !PED.IS_PED_MALE(p.Handle) || !PED.IS_PED_HUMAN(p.Handle) ||
                    p.Position.DistanceTo(drunk.Position) > Rowdy.FriendMetres)
                    continue;
                if (NotFree(p.Handle) || _director.Involves(p.Handle) || MayDirectWhyNot(p) != null || PED.IS_PED_USING_ANY_SCENARIO(p.Handle) || PED.IS_PED_IN_COMBAT(p.Handle, 0) || Spawner.IsOurs(p.Handle))
                    continue;
                MindWho w = MindKnown(p.Handle, now);
                if (w == null || w.Id == null || !InHisCups(p, w.P) || LadderWarmth(dw.Id, w.Id) < 1)
                    continue;
                return p;
            }
            return null;
        }

        /// The Testing row: the nearest named man as the drunk, the nearest other within 8 m of him as his friend, another within 12 m
        /// as the man going by; by turns the friend hauls him home, then it comes to blows.
        private string RowdyFire(int now, out string said)
        {
            said = null;
            if (!Rowdy.On)
                return "off ([Scenes] RowdyDrunks)";
            Ped me = Game.Player.Character;
            List<Candidate> men = ReasonCandidates(me, now, true).Where(c => !c.Who.Woman).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).ToList();
            foreach (Candidate drunk in men)
            {
                Candidate pal = men.Where(c => c != drunk && c.Ped.Position.DistanceTo(drunk.Ped.Position) <= Rowdy.FriendMetres).OrderBy(c => c.Ped.Position.DistanceTo(drunk.Ped.Position)).FirstOrDefault();
                if (pal == null)
                    continue;
                Candidate by = men.Where(c => c != drunk && c != pal && c.Ped.Position.DistanceTo(drunk.Ped.Position) <= 12f).OrderBy(c => c.Ped.Position.DistanceTo(drunk.Ped.Position)).FirstOrDefault();
                if (by == null)
                    continue;
                string seed = drunk.Who.Id + "|" + by.Who.Id + "|" + now + "|rowdy";
                SceneCast cast = BuildCast(drunk.Ped, by.Ped, pal.Ped, false, me, now, seed);
                bool fights = _rowdyTests++ % 2 == 1;
                Scene s = Rowdy.Play(cast, seed, Rowdy.Answer(by.Who.Temper), fights);
                if (!StartScene(s, new List<Ped> { drunk.Ped, by.Ped, pal.Ped }, cast, seed, now, true))
                    return "the Director wouldn't take the scene";
                said = drunk.Who.Name + " and " + pal.Who.Name + " sing, and " + drunk.Who.Name + " picks on " + by.Who.Name + (fights ? " (blows, this time)" : " (hauled home, this time)");
                return null;
            }
            return men.Count < 3 ? "fewer than three named men near you" : "no man near you with another within 8 m and a third within 12 m";
        }

        /// F7: the rowdy drunks this session.
        private string RowdyStatus()
        {
            if (!Rowdy.On)
                return "off ([Scenes] RowdyDrunks)";
            return _rowdyPicked + " picked on a man going by (" + _rowdyBlows + " came to blows, the rest hauled home); tests " + _rowdyTests;
        }
    }
}
