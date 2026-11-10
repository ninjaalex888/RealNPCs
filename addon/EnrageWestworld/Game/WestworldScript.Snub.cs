// The well-off and the working man, the game side (FOURTEENHOUR item 18; Core/Snub.cs; [Scenes] Snubs). The reason is the street's
// own (Core/Reasons.cs: a working man and a gentleman, by day out of doors); here, a lady beside the gentleman (within 3 m, a woman
// dressed like money, on her feet and nobody's scene) is cast as the scene's third, so her word is in it and the two of them walk
// on together (the Director's leavewith, seen). A cast of three takes no stand-up and no bystander (both need two). The Testing row
// fires it at once with the nearest working man and gentleman. Runs only when the scene is played; never a frame.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private static readonly string[] WorkingGroups = { "working", "farm", "rough", "trapper" };   // Core/Snub.cs's working men
        private int _snubLady, _snubAlone, _snubTests;
        private string _snubLast;

        /// A lady beside the gentleman: within 3 m of him, a woman dressed like money, on her feet, nobody's scene; null when he's alone.
        private Ped LadyBeside(Ped gent, Ped other)
        {
            Ped best = null;
            float bestD = 3f;
            if (gent == null || !gent.Exists())
                return null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == gent.Handle || (other != null && p.Handle == other.Handle) || p.Handle == Game.Player.Character.Handle)
                    continue;
                float d = p.Position.DistanceTo(gent.Position);
                if (d >= bestD || PED.IS_PED_MALE(p.Handle) || !PED.IS_PED_HUMAN(p.Handle) || !WellOff(p) || NotFree(p.Handle) || _director.Involves(p.Handle) ||
                    PED.IS_PED_USING_ANY_SCENARIO(p.Handle) || Spawner.IsOurs(p.Handle) || MayDirectWhyNot(p) != null)   // (MayDirect: the story's, enRAGE's in a role, mounted...: the review of items 14-21)
                    continue;
                bestD = d;
                best = p;
            }
            return best;
        }

        /// The Testing page: the nearest named working man and the nearest gentleman within 10 m of each other; the lady beside him
        /// joins in.
        private string SnubFire(int now)
        {
            if (!Snub.On)
                return "off ([Scenes] Snubs)";
            Ped me = Game.Player.Character;
            List<Candidate> near = ReasonCandidates(me, now, true).Where(c => !c.Who.Woman).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).ToList();
            Candidate gent = near.FirstOrDefault(c => c.Who.WellOff);
            Candidate hand = gent == null ? null : near.FirstOrDefault(c => !c.Who.WellOff && c.Who.P != null && c.Ped.Position.DistanceTo(gent.Ped.Position) <= 10f &&
                                                                         WorkingGroups.Contains(InnerLife.TradeGroup(c.Who.P.Trade)));
            if (gent == null || hand == null)
                return gent == null ? "no named gentleman near you (a town's well-off part: Saint Denis, Rhodes)" : "no named working man within 10 m of " + gent.Who.Name;
            string seed = hand.Who.Id + "|" + gent.Who.Id + "|" + now + "|snub";
            Ped lady = LadyBeside(gent.Ped, hand.Ped);
            SnubCount(lady);
            SceneCast cast = BuildCast(hand.Ped, gent.Ped, lady, _placeKind == "saloon", me, now, seed);
            Scene s = Snub.Play(cast, seed);
            var people = new List<Ped> { hand.Ped, gent.Ped };
            if (lady != null && s.Cast == 3)
                people.Add(lady);
            StartScene(s, people, cast, seed, now, true);
            if (!_director.Busy)
                return "the Director wouldn't take the scene";
            _snubTests++;
            _snubLast = hand.Who.Name + " and " + gent.Who.Name + (lady != null ? " (and his lady)" : "") + ": " + s.Kind;
            Log.Write("SNUB " + _snubLast + ", " + s.Beats[0].Line + " (a test)");
            return null;
        }

        /// Counted once a snub is cast (not every look for a lady).
        private void SnubCount(Ped lady)
        {
            if (lady != null)
                _snubLady++;
            else
                _snubAlone++;
        }

        /// F7: how often a lady was by the gentleman, and the tests.
        private string SnubStatus()
        {
            if (!Snub.On)
                return "off ([Scenes] Snubs)";
            return "cast with a lady beside him " + _snubLady + ", alone " + _snubAlone + "; tests " + _snubTests + (_snubLast != null ? ", last: " + _snubLast : "");
        }
    }
}
