// The old soldiers, the game side (FOURTEENHOUR item 17; Core/OldSoldiers.cs; [Scenes] OldSoldiers, only with [World]
// HistoricalAccuracy). Who served is read once a person, from the build (an old-timer's model or trade; a man of middle years by his
// age, the age the links and the worries give him), his trade (a soldier) and his home town, and kept by id: the reasons read it on
// the Someone, the scene's cast on SceneCast.Wars, a fight's sides on SidePerson.War. The scenes go through the reasons and the
// Director like any street scene (seen runners: the reasons, the Director's talk and its fistfights, the flask); the Testing rows
// fire either at once. A dictionary look a person, never a frame.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, string> _warOf = new Dictionary<string, string>();   // id -> his war ("" for none)
        private int _oldSoldierTests;
        private string _oldSoldierLast;

        /// From SomeoneOf: his war on the Someone, worked out the first time he's looked at and kept by id.
        private Someone Veteran(Ped p, Someone s)
        {
            if (s == null || s.Woman || s.Id == null || !OldSoldiers.On)
                return s;
            string war;
            if (!_warOf.TryGetValue(s.Id, out war))
            {
                string m = p != null ? Natives.ModelName(p) : string.Empty;
                bool old = m.Contains("_m_o_") || (s.P != null && s.P.Trade == "an old-timer"), middle = m.Contains("_m_m_"), young = m.Contains("_m_y_");
                int age = middle && s.P != null ? Romance.AgeOrGuess(s.P) : -1;   // a man of middle years by the age the rest of the add-on gives him
                bool soldier = s.P != null && InnerLife.TradeGroup(s.P.Trade) == "soldier";
                war = OldSoldiers.WarOf(s.Id, s.P != null && s.P.Home != null ? s.P.Home : _placeTown, false, old, age, young, soldier, InnerLife.ClockYear, CLOCK.GET_CLOCK_MONTH() + 1) ?? "";
                if (_warOf.Count > 600)
                    _warOf.Clear();   // (a long session's people: worked out again when next seen, the same by the seed)
                _warOf[s.Id] = war;
            }
            s.War = war.Length > 0 ? war : null;
            return s;
        }

        /// His war for a fight's sides: as SomeoneOf found it, else from his trade (an old-timer), else none.
        private string WarOfKnown(MindWho w)
        {
            if (!OldSoldiers.On || w == null || w.Id == null)
                return null;
            string war;
            if (_warOf.TryGetValue(w.Id, out war))
                return war.Length > 0 ? war : null;
            Person p = w.P;
            if (p == null || p.Woman)
                return null;
            return OldSoldiers.WarOf(w.Id, p.Home ?? _placeTown, false, TradeGroupOf(w) == "old", -1, false, false, InnerLife.ClockYear, CLOCK.GET_CLOCK_MONTH() + 1);
        }

        /// The Testing page: the nearest two named men within 10 m. Old men argue the war (set on opposite sides if they aren't);
        /// for the story, the nearer is home from the islands and tells the other.
        private string OldSoldiersFire(bool story, int now)
        {
            if (!OldSoldiers.On)
                return "off ([Scenes] OldSoldiers, or [World] HistoricalAccuracy)";
            Ped me = Game.Player.Character;
            List<Candidate> men = ReasonCandidates(me, now, true).Where(c => !c.Who.Woman && (story || Director.OldModel(c.Ped))).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).ToList();
            for (int i = 0; i + 1 < men.Count; i++)   // (each pair once: the second of it from i + 1)
                for (int j = i + 1; j < men.Count; j++)
                {
                    if (men[i].Ped.Position.DistanceTo(men[j].Ped.Position) > 10f)
                        continue;
                    string seed = men[i].Who.Id + "|" + men[j].Who.Id + "|" + now + "|soldiers";
                    SceneCast cast = BuildCast(men[i].Ped, men[j].Ped, null, _placeKind == "saloon", me, now, seed);
                    string heard = cast.Wars != null ? cast.Wars[1] : null;
                    if (story)
                        cast.Wars = new[] { "islands", heard };   // (the test's: he's home from the islands)
                    else if (cast.Wars == null || !OldSoldiers.Old(cast.Wars[0]) || !OldSoldiers.Old(cast.Wars[1]) || cast.Wars[0] == cast.Wars[1])
                        cast.Wars = new[] { "blue", "grey" };   // (the test's: opposite sides)
                    Scene s = story ? OldSoldiers.Story(cast, seed) : OldSoldiers.War(cast, seed);
                    StartScene(s, new List<Ped> { men[i].Ped, men[j].Ped }, cast, seed, now, true);
                    if (!_director.Busy)
                        return "the Director wouldn't take the scene";
                    _oldSoldierTests++;
                    _oldSoldierLast = men[i].Who.Name + " (" + cast.Wars[0] + ") and " + men[j].Who.Name + " (" + (cast.Wars[1] ?? "no war") + "): " + s.Kind;
                    Log.Write("OLD SOLDIERS " + _oldSoldierLast + ", " + s.Beats[0].Line + " (a test)");
                    return null;
                }
            return (men.Count < 2 ? (story ? "fewer than two named men near you" : "fewer than two named old men near you") : "the men near you are too far apart") +
                   (story ? "" : TestCastHint("two old men"));
        }

        /// F7: who near has a war, and the tests.
        private string OldSoldiersStatus()
        {
            if (!OldSoldiers.On)
                return "off ([Scenes] OldSoldiers" + (_cfg != null && !_cfg.HistoricalAccuracy ? ", and [World] HistoricalAccuracy is off" : "") + ")";
            int blue = _warOf.Values.Count(v => v == "blue"), grey = _warOf.Values.Count(v => v == "grey"), young = _warOf.Values.Count(v => v == "cuba" || v == "islands");
            return "of " + _warOf.Count + " men looked at this session: " + blue + " wore the blue, " + grey + " the grey, " + young + " home from Cuba or the islands; tests " + _oldSoldierTests +
                   (_oldSoldierLast != null ? ", last: " + _oldSoldierLast : "");
        }
    }
}
