// The reload noticed, in the engine (SIXDAY item 12b; Core/Ghosts.cs): the ghosts logged when the clock turns back, both sides
// of a rewound tie given a memory of the thing that didn't happen, the waking told to talk of the morning that happened twice,
// the deja vu marked felt, the ghosts trimmed daily. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _dejaVus;

        /// After Store.Rewind: the ghosts it kept, logged; the rewound ties thought about by both sides; the waking's talk set.
        private void GhostsNoted(long now)
        {
            List<Ghost> fresh = Store.LastRewound ?? new List<Ghost>();
            Log("GHOST rewound: " + Ghosts.Summary(fresh) + "; " + Ghosts.WentBack(Store.Rewinds));
            if (Cfg.WestWorld && Cfg.Ghosts)
                foreach (Ghost g in Ghosts.TiesUnfelt(Store.Ghosts))
                {
                    g.Felt = true;
                    bool warm = g.Kind == "tie:flirt" || g.Kind == "tie:sweet" || g.Kind == "tie:propose" || g.Kind == "tie:comfort" || g.Kind == "tie:cheer" || g.Kind == "tie:makeup";
                    string sub = warm ? "ghostkiss" : "ghostquarrel";
                    Remember(g.Who, "talk", sub, "heard", g.Town, now, warm ? 1 : -1, 45, "ghost:" + g.Id + ":a", g.Other, g.OtherName);
                    Remember(g.Other, "talk", sub, "heard", g.Town, now, warm ? 1 : -1, 45, "ghost:" + g.Id + ":b", g.Who, g.WhoName);
                }
            Scenes.WentBackLately = Cfg.WestWorld && Cfg.Ghosts && Ghosts.WentBackLately(Store.Ghosts, now);
        }

        /// From DailyTick: the ghosts of a week ago gone; the waking's talk of it ends two days on.
        private void GhostsDaily(long now)
        {
            if (Store.Ghosts == null)
                return;
            Ghosts.Trim(Store.Ghosts, now);
            Scenes.WentBackLately = Cfg.WestWorld && Cfg.Ghosts && Ghosts.WentBackLately(Store.Ghosts, now);
        }

        /// The deja vu waiting for this person (alive again after a rewound death), if any.
        public Ghost DejaVuFor(string id)
        {
            if (!Cfg.WestWorld || !Cfg.Ghosts || Store.Ghosts == null || HasDied(id))
                return null;
            return Ghosts.DejaVuFor(Store.Ghosts, id);
        }

        /// The deja vu had: once, awareness up.
        public void DejaVuFelt(Ghost g, string name, long now)
        {
            if (g == null || g.Felt)
                return;
            g.Felt = true;
            _dejaVus++;
            PersonState st = MindOf(g.Who, name);
            if (st != null)
                st.Awareness = Math.Min(100, st.Awareness + Ghosts.DejaVuAwareness);
            Log("GHOST felt: " + (name ?? g.WhoName) + " (" + g.Kind + (g.ByPlayer ? ", by you" : "") + ")");
        }

        public string GhostsStatus(long now)
        {
            if (Store.Ghosts == null || Store.Ghosts.Count == 0)
                return Ghosts.WentBack(Store.Rewinds) + "; no ghosts live";
            var parts = Store.Ghosts.Where(g => g != null).OrderByDescending(g => g.RewoundAt).Take(8)
                .Select(g => g.Kind + " " + (g.WhoName ?? g.Who ?? "?") + (g.OtherName != null ? " / " + g.OtherName : "") + (g.Town != null ? " in " + g.Town : "") + (g.Felt ? " (felt)" : ""));
            return Ghosts.WentBack(Store.Rewinds) + "; " + Store.Ghosts.Count + " ghosts live (a week kept): " + string.Join("; ", parts) + "; " + _dejaVus + " deja vus this session" + (Scenes.WentBackLately ? "; the waking talk of the morning that happened twice" : "");
        }
    }
}
