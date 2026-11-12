// Gossip that travels along ties, the engine's part (FIVEDAY item 23; Core/TieGossip.cs). Each world tick (half a game
// hour), for each deed of the player's known in its town this week: who knows it (those who saw it, those the talk or a
// rumour reached, those already told along a tie), and one step of the spread along the town's ties (TieGossip.Spread).
// The newly told know the deed (KnownDeeds: what they say to his face, their diaries) and think of who told them
// (TieGossipCandidate, in the resident's and the record's thoughts). Behind [Mind] TieGossip.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _tieGossipTold;

        private void TieGossipTick(long now)
        {
            if (Cfg == null || !Cfg.TieGossip || !Cfg.Minds)
                return;
            var recent = Store.Deeds.Where(d => d != null && d.Known && !d.Unseen && d.Town != null && d.Id != null && now - d.At >= 0 && now - d.At <= TieGossip.WindowDays * 1440L && d.Weight >= 12 &&
                                                (Minds.DeedGroup(d.Kind) == "deed:violence" || Minds.DeedGroup(d.Kind) == "deed:theft")).ToList();
            if (recent.Count == 0)
                return;
            int budget = TieGossip.MostPerTick;
            // who knows each deed, in one pass over the kept people for all of them, and whether they're still about asked only of
            // those who know: Present walks the residents and the deaths, and asked of all ~3000 kept people for every deed it took
            // 140 ms a world tick in the soak of a copy of Lex's save a few deeds in (HARDEN item 2). The same people come out, in
            // the same order.
            var knownOf = new Dictionary<string, Dictionary<string, TieHeard>>();
            var deedAt = new Dictionary<string, long>();
            foreach (Deed d in recent)
                if (!knownOf.ContainsKey(d.Id))
                {
                    knownOf[d.Id] = new Dictionary<string, TieHeard>();
                    deedAt[d.Id] = d.At;
                }
            var presentAsked = new Dictionary<string, bool>();
            foreach (KeyValuePair<string, PersonState> kv in Store.People)
            {
                PersonState st = kv.Value;
                if (st == null || (st.SawDeeds.Count == 0 && st.KnownDeeds.Count == 0))
                    continue;
                for (int pass = 0; pass < 2; pass++)
                    foreach (string id in pass == 0 ? st.SawDeeds : st.KnownDeeds)
                    {
                        Dictionary<string, TieHeard> into;
                        if (id == null || !knownOf.TryGetValue(id, out into) || into.ContainsKey(kv.Key))
                            continue;
                        bool here;
                        if (!presentAsked.TryGetValue(kv.Key, out here))
                            presentAsked[kv.Key] = here = Present(kv.Key);   // (the dead and the gone pass nothing on)
                        if (here)
                            into[kv.Key] = new TieHeard { Id = kv.Key, DeedId = id, At = deedAt[id] + 60, Hops = pass == 0 ? 0 : 1 };   // (the talk's own start: an hour on)
                    }
            }
            foreach (Deed d in recent)
            {
                var known = knownOf[d.Id];
                foreach (TieHeard th in Store.TieHeard)
                    if (th.DeedId == d.Id)
                        known[th.Id] = th;
                List<TieHeard> told = TieGossip.Spread(d.Id, d.Town, known, Store.Ties, now, id => id != d.VictimId && Store.Find(id) != null && !id.StartsWith("player") && Present(id), budget);
                foreach (TieHeard h in told)
                {
                    PersonState st = Store.Find(h.Id);
                    if (!st.KnownDeeds.Contains(d.Id))
                        st.KnownDeeds.Add(d.Id);
                    Store.TieHeard.Add(h);
                    _tieGossipTold++;
                    Detail("GOSSIP " + h.Id + " heard of your " + d.Kind + " from " + (h.FromName ?? h.FromId) + " (hop " + h.Hops + ", fidelity " + TieGossip.Fidelity(h.Hops) + ")");
                }
                budget -= told.Count;
                if (budget <= 0)
                    break;
            }
            if (Store.TieHeard.Count > TieGossip.Kept)
                Store.TieHeard.RemoveRange(0, Store.TieHeard.Count - TieGossip.Kept);
        }

        /// Alive and still in town: not dead, not gone west or fled (the Part 4 review: the dead were told, and told others).
        private bool Present(string id)
        {
            if (HasDied(id))
                return false;
            Resident r = Store.FindResident(id);
            return r == null || (!r.Left && !r.Fled && !r.Dead);
        }

        /// The thought of one told along a tie in the last two days, once a deed: who told them, and the version they got.
        private Minds.Candidate TieGossipCandidate(Person p, PersonState st, string seed, long now)
        {
            if (Cfg == null || !Cfg.TieGossip || p == null || st == null)
                return null;
            TieHeard th = null;
            for (int i = Store.TieHeard.Count - 1; i >= 0; i--)
                if (Store.TieHeard[i].Id == p.Id)
                {
                    th = Store.TieHeard[i];
                    break;
                }
            if (th == null || now - th.At > 2 * 1440 || st.Has("tg:" + th.DeedId))
                return null;
            Deed d = Store.Deeds.FirstOrDefault(x => x.Id == th.DeedId);
            if (d == null)
                return null;
            return new Minds.Candidate
            {
                Source = "heard:tie", Pull = 36 + Minds.Jitter(seed, "tiegossip", 6),
                Make = () =>
                {
                    var r = new TownRumour { Id = th.Id + "|" + th.DeedId, DeedId = d.Id, Origin = d.Town, Town = d.Town, Fidelity = TieGossip.Fidelity(th.Hops), Hops = th.Hops };
                    string line = TieGossip.Thought(Person.FirstOf(th.FromName), Rumours.Version(r, d, Player, PlayerNameKnownIn(d.Town)), seed);
                    if (line != null)
                        st.Mark("tg:" + th.DeedId);
                    return line;
                }
            };
        }

        public string TieGossipStatus()
        {
            return Cfg == null || !Cfg.TieGossip ? "off ([Mind] TieGossip)" : _tieGossipTold + " told along a tie this session; " + Store.TieHeard.Count + " kept";
        }
    }
}
