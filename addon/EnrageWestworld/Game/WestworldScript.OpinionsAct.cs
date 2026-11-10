// Opinions that act, the game side (FOURTEENHOUR item 23; Core/Opinions.cs; [Mind] OpinionsAct). The views are read off the ties as
// the pair thoughts read them (Opinions.View: respect, contempt, envy, fear), kept an hour of game time by pair; a person's own views
// of others (the strongest three) a half hour by id, on the Someone for the reasons. Used: the fearful stay out of a brawl a man they
// fear is in (the join's look), contempt for a fighter pulls an onlooker against him (the sides' pull), respect draws two to walk
// together and contempt or fear never (the walk together), and the talk of someone not there carries the teller's view and the
// friend's own (the reason's scene). Dictionary looks; never a frame.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly Dictionary<string, KeyValuePair<long, string>> _viewOf = new Dictionary<string, KeyValuePair<long, string>>();
        private readonly Dictionary<string, KeyValuePair<long, List<Opinions.Held>>> _viewsHeld = new Dictionary<string, KeyValuePair<long, List<Opinions.Held>>>();
        private int _opFearHeld, _opScorn, _opRespectWalks, _opNoWalk, _opTalks;

        /// This one's view of the other, off the tie between them (null for none, or no tie).
        private string ViewOfIds(string id, string otherId)
        {
            if (id == null || otherId == null || _store == null)
                return null;
            string key = id + ">" + otherId;
            long g = _store.LastGameMin;
            KeyValuePair<long, string> kv;
            if (_viewOf.TryGetValue(key, out kv) && g >= kv.Key && g - kv.Key < 60)
                return kv.Value;
            SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(id) && x.Has(otherId));
            string v = t != null ? ViewOfTie(t, id) : null;
            if (_viewOf.Count > 2000)
                _viewOf.Clear();
            _viewOf[key] = new KeyValuePair<long, string>(g, v);
            return v;
        }

        private string ViewOfTie(SceneTie t, string id)
        {
            string other = t.OtherId(id);
            Resident mine = _store.FindResident(id), theirs = _store.FindResident(other);
            Person pm = mine == null && _engine != null ? _engine.Find(id) : null;
            string temper = mine != null ? mine.Personality : pm != null ? pm.Personality : null;
            bool iGentry = mine != null && InnerLife.TradeGroup(mine.Trade) == "gentry", otherGentry = theirs != null && InnerLife.TradeGroup(theirs.Trade) == "gentry";
            return Opinions.View(t, id, temper, iGentry, otherGentry, false);
        }

        /// Their views of others they know, the strongest three to pass on, kept a half hour of game time.
        private List<Opinions.Held> ViewsHeldBy(string id)
        {
            if (id == null || _store == null)
                return null;
            long g = _store.LastGameMin;
            KeyValuePair<long, List<Opinions.Held>> kv;
            if (_viewsHeld.TryGetValue(id, out kv) && g >= kv.Key && g - kv.Key < 30)
                return kv.Value;
            List<Opinions.Held> list = null;
            foreach (SceneTie t in _store.Ties)
            {
                if (!t.Has(id))
                    continue;
                string other = t.OtherId(id);
                if (other == null || other == "player")
                    continue;
                string v = ViewOfTie(t, id);
                if (v == null)
                    continue;
                (list ?? (list = new List<Opinions.Held>())).Add(new Opinions.Held { Id = other, Name = t.Other(id), View = v, Woman = IsWomanId(other) });
            }
            if (list != null)
                list = list.OrderByDescending(h => Opinions.Weight(h.View)).Take(3).ToList();
            if (_viewsHeld.Count > 800)
                _viewsHeld.Clear();
            _viewsHeld[id] = new KeyValuePair<long, List<Opinions.Held>>(g, list);
            return list;
        }

        /// The talk of someone not there, for the reason's scene: his name, the teller's view, the friend's own, "w" for a woman.
        private string[] TalkOfCast(Someone teller, Someone friend)
        {
            Opinions.Held z = Reasons.TalkOfWhom(teller, friend);
            if (z == null)
                return null;
            _opTalks++;
            return new[] { Person.FirstOf(z.Name), z.View, ViewOfIds(friend.Id, z.Id), z.Woman ? "w" : "" };
        }

        /// Whether this onlooker fears one of those fighting: then he stays out of it.
        private bool FearsOneOf(string id, List<int> a, List<int> b, int now)
        {
            if (id == null || _store == null)
                return false;
            var ids = new HashSet<string>();
            foreach (int h in a.Concat(b))
            {
                MindWho w = _mindWho.TryGetValue(h, out w) ? w : null;
                if (w != null && w.Id != null)
                    ids.Add(w.Id);
            }
            foreach (SceneTie t in _store.Ties)
                if (t.Has(id) && ids.Contains(t.OtherId(id)) && ViewOfIds(id, t.OtherId(id)) == "fear")
                    return true;
            return false;
        }

        /// F7: what the views did this session.
        private string OpinionsActStatus()
        {
            if (!Opinions.Act)
                return "off ([Mind] OpinionsAct)";
            return "kept out of a brawl by fear " + _opFearHeld + ", sided against a man despised " + _opScorn + ", walked together for respect " + _opRespectWalks +
                   ", never walked beside one despised or feared " + _opNoWalk + ", talks of someone not there " + _opTalks;
        }
    }
}
