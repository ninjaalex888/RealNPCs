// Rivals for one woman, the game side (FOURTEENHOUR item 19; Core/Rivals.cs; [Scenes] Rivals). Whom each man is courting is read off
// the save (his courting worry's link, a warm sweetheart, an affair of enRAGE's), kept a half hour of game time by id, on the Someone
// for the reasons. When the two have had it out (the scene played to its end, or the fight over: the Director's PlayedOut and
// FightOver, both seen), she chooses: the tie between her and the man she chose goes sweet, her tie with the other a brush-off (which
// ends a courting, Core/Ties.cs), the one she didn't choose says his piece over his head, and the town's news has it. A Testing row
// fires it at once between the nearest two men, rivals for a made-up woman (a test: no ties written). A look a scene, never a frame.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class RivalsRun
        {
            public string HerId, HerName, HerFull, AId, AName, BId, BName, ATemper, BTemper, Town;
            public int AH, BH, WalkedAway = -1, Until;
            public bool Fight, Test;
        }

        private RivalsRun _rivals;
        private Scene _rivalsScene;   // the rivals' scene just built, for NotePlayedReason
        private readonly Dictionary<string, KeyValuePair<long, string[]>> _courtingOf = new Dictionary<string, KeyValuePair<long, string[]>>();
        private readonly Dictionary<string, string> _rivalLost = new Dictionary<string, string>();   // id -> "her id|game minute" she chose the other man (a week)
        private int _rivalsMet, _rivalsChosen, _rivalsNeither, _rivalsTests;
        private string _rivalsLast;

        /// Whom he's courting and her name, from the save, kept a half hour of game time by id.
        private string CourtingFor(string id, out string name)
        {
            name = null;
            if (id == null || _store == null)
                return null;
            long g = _store.LastGameMin;
            KeyValuePair<long, string[]> c;
            if (!_courtingOf.TryGetValue(id, out c) || g - c.Key > 30 || g < c.Key)
            {
                string n;
                string other = Rivals.CourtingOf(_store, id, IsWomanId, out n);
                string lost;
                long lostAt;
                if (other != null && _rivalLost.TryGetValue(id, out lost) && lost.StartsWith(other + "|") && long.TryParse(lost.Substring(other.Length + 1), out lostAt) && g - lostAt < 7 * 1440)
                    other = n = null;   // she turned him down for the other man: not this week
                c = new KeyValuePair<long, string[]>(g, new[] { other, n });
                if (_courtingOf.Count > 800)
                    _courtingOf.Clear();
                _courtingOf[id] = c;
            }
            name = c.Value[1];
            return c.Value[0];
        }

        /// Whether an id is a woman's, by the engine's people or the residents; unknown counts as a woman (a man's sweetheart, mostly).
        private bool IsWomanId(string id)
        {
            Person p = _engine != null ? _engine.Find(id) : null;
            if (p != null)
                return p.Woman;
            Resident r = _store != null ? _store.Residents.FirstOrDefault(x => x.Id == id) : null;
            return r == null || r.Woman;
        }

        /// From NotePlayedReason: the rivals' scene has begun; her choice waits for its end (3 minutes at most).
        private void RivalsNote(PairReason pr)
        {
            Scene s = _rivalsScene;
            _rivalsScene = null;
            if (s == null)
                return;
            Someone a = pr.Why.BFirst ? pr.B.Who : pr.A.Who, b = pr.Why.BFirst ? pr.A.Who : pr.B.Who;
            Ped pa = pr.Why.BFirst ? pr.B.Ped : pr.A.Ped, pb = pr.Why.BFirst ? pr.A.Ped : pr.B.Ped;
            RivalsBegin(s, a.Courting ?? b.Courting, a.CourtingName ?? b.CourtingName, a.Id, a.Name, a.Temper, pa.Handle, b.Id, b.Name, b.Temper, pb.Handle, false);
        }

        private void RivalsBegin(Scene s, string herId, string herName, string aId, string aName, string aTemper, int ah, string bId, string bName, string bTemper, int bh, bool test)
        {
            int walked = -1;
            foreach (Beat bt in s.Beats)
                if (bt.Act == "walkaway")
                    walked = bt.Who;
            _rivals = new RivalsRun
            {
                HerId = herId, HerName = herName != null ? Person.FirstOf(herName) : "her", HerFull = herName ?? "her", AId = aId, AName = aName, ATemper = aTemper, AH = ah, BId = bId, BName = bName, BTemper = bTemper, BH = bh,
                Town = _placeTown, Fight = s.Fight, WalkedAway = s.Fight || s.Summary.Contains("left it to her") ? -1 : walked, Until = Game.GameTime + 180000, Test = test
            };
            _rivalsMet++;
            Log.Write("RIVALS " + aName + " & " + bName + ", both courting " + _rivals.HerName + ": " + (s.Fight ? "blows" : walked >= 0 && _rivals.WalkedAway >= 0 ? (walked == 0 ? aName : bName) + " walks away from it" : "words") + (test ? " (a test)" : ""));
        }

        /// From OnFightOver (winner's handle) and OnPlayedOut (0): her choice, once the two have had it out.
        private void RivalsOver(int winner, int a, int b)
        {
            RivalsRun r = _rivals;
            if (r == null)
                return;
            if (Game.GameTime > r.Until)
            {
                _rivals = null;   // never played out (cut short before blows): another day
                return;
            }
            if (!((a == r.AH && b == r.BH) || (a == r.BH && b == r.AH)))
                return;   // the two of them, not another scene of one of them (the review of items 14-21)
            _rivals = null;
            bool fought = r.Fight && winner != 0;   // (a fight cut off before any blows is words: the review of items 14-21)
            r.Fight = fought;
            int won = fought ? (winner == r.AH ? 0 : winner == r.BH ? 1 : -1) : -1;
            Person her = r.HerId != null && _engine != null ? _engine.Find(r.HerId) : null;
            string herTemper = her != null && her.Personality != null ? her.Personality : Voice.Pick((r.HerId ?? r.AId) + "|temper", "Plain", "Loyal", "Proud", "Cold", "Greedy");
            string seed = (r.HerId ?? "her") + "|" + r.AId + "|" + r.BId + "|" + (_store != null ? _store.LastGameMin / 1440 : 0);
            int chose = Rivals.Chooses(herTemper, r.ATemper, r.BTemper, won, r.WalkedAway, seed);
            string chosenName = chose == 0 ? r.AName : chose == 1 ? r.BName : null, otherName = chose == 0 ? r.BName : chose == 1 ? r.AName : null;
            string news = Rivals.News(r.HerName, chosenName != null ? Person.FirstOf(chosenName) : null, otherName != null ? Person.FirstOf(otherName) : null, r.Fight, chose >= 0 && won == chose,
                                      chose >= 0 && r.WalkedAway == chose, chose >= 0 && r.WalkedAway == 1 - chose, seed);
            if (!r.Test && _store != null && r.HerId != null)
            {
                long g = Bridge.Now();
                string chosenId = chose == 0 ? r.AId : chose == 1 ? r.BId : null;
                foreach (string[] man in new[] { new[] { r.AId, r.AName }, new[] { r.BId, r.BName } })
                {
                    Ties.Note(_store.Ties, r.HerId, r.HerFull, man[0], man[1], r.Town, man[0] == chosenId ? "sweet" : "brushoff", g);   // her yes, her no (a brush-off ends a courting)
                    if (man[0] != chosenId)
                        _rivalLost[man[0]] = r.HerId + "|" + g;   // he courts her no more this week, whatever courted her (the review of items 14-21)
                }
                _courtingOf.Remove(r.AId);
                _courtingOf.Remove(r.BId);
            }
            if (chose >= 0)
                _rivalsChosen++;
            else
                _rivalsNeither++;
            // the one she didn't choose (both, for neither) says his piece, where you can see it
            foreach (int k in chose >= 0 ? new[] { 1 - chose } : new[] { 0, 1 })
            {
                int h = k == 0 ? r.AH : r.BH;
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && _cfg.ScenesOnTicker && _cfg.Ticker && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                    SpeechCaption(Person.FirstOf(k == 0 ? r.AName : r.BName), Rivals.LoserLine(k == 0 ? r.ATemper : r.BTemper, seed + "|" + k), h, 3000);
            }
            NewsPush(r.Town ?? "Around here", news, "news");
            _rivalsLast = r.HerName + " chose " + (chosenName ?? "neither of them") + (r.Fight ? (won >= 0 ? "; " + (won == 0 ? r.AName : r.BName) + " won the fight" : "") : "") + (r.Test ? " (a test)" : "");
            Log.Write("RIVALS over: " + _rivalsLast + " (her nature " + herTemper + ")");
        }

        /// The Testing page: the nearest two named men within 10 m, rivals for a made-up woman; no ties written.
        private string RivalsFire(int now)
        {
            if (!Rivals.On)
                return "off ([Scenes] Rivals)";
            Ped me = Game.Player.Character;
            List<Candidate> men = ReasonCandidates(me, now, true).Where(c => !c.Who.Woman).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).ToList();
            for (int i = 0; i + 1 < men.Count; i++)   // (each pair once: the second from i + 1)
                for (int j = i + 1; j < men.Count; j++)
                {
                    if (men[i].Ped.Position.DistanceTo(men[j].Ped.Position) > 10f)
                        continue;
                    string seed = men[i].Who.Id + "|" + men[j].Who.Id + "|" + now + "|rivals";
                    string her = men[i].Who.CourtingName ?? Voice.Pick(seed + "|her", "Nell", "Clara", "Mary Beth", "Ada", "Josie", "Hattie");
                    SceneCast cast = BuildCast(men[i].Ped, men[j].Ped, null, _placeKind == "saloon", me, now, seed);
                    cast.Courted = Person.FirstOf(her);
                    Scene s = Rivals.Play(cast, seed);
                    StartScene(s, new List<Ped> { men[i].Ped, men[j].Ped }, cast, seed, now, true);
                    if (!_director.Busy)
                        return "the Director wouldn't take the scene";
                    _rivalsTests++;
                    RivalsBegin(s, null, her, men[i].Who.Id, men[i].Who.Name, men[i].Who.Temper, men[i].Ped.Handle, men[j].Who.Id, men[j].Who.Name, men[j].Who.Temper, men[j].Ped.Handle, true);
                    return null;
                }
            return men.Count < 2 ? "fewer than two named men near you" : "the men near you are too far apart";
        }

        /// F7: the rivals met this session, and what came of it.
        private string RivalsStatus()
        {
            if (!Rivals.On)
                return "off ([Scenes] Rivals)";
            return _rivalsMet + " met, she chose " + _rivalsChosen + ", neither " + _rivalsNeither + "; tests " + _rivalsTests + (_rivals != null ? "; waiting on " + _rivals.AName + " and " + _rivals.BName : "") +
                   (_rivalsLast != null ? "; last: " + _rivalsLast : "");
        }
    }
}
