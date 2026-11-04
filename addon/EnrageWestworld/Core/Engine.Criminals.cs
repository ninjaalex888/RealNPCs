// The criminal class (Core/Criminals.cs; SIXDAY item 16): the engine's side. A thief gone straight is noticed once (the
// mark on their state keyed by the last theft, so a man who takes it up again and goes straight again is noticed again),
// and the F7 dump lists the bent of the month. Behind [Scenes] CriminalClass. Nothing here touches the game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private int _reformedNoticed;

        /// A thief gone straight this month, once: the mark set, the thought (Criminals.ReformThought); else null.
        public string CriminalReformed(string id, long now)
        {
            if (Cfg == null || !Cfg.CriminalClass)
                return null;
            long lastAt;
            if (!Criminals.Reformed(Store.Ties, id, now, out lastAt))
                return null;
            Person p = KinPerson(id);
            if (p == null)
                return null;
            PersonState st = Store.Get(p);
            string mark = "reformed:" + lastAt;
            if (st.Has(mark))
                return null;
            st.Mark(mark);
            _reformedNoticed++;
            Log("CRIMINAL " + p.Name + " has gone straight: no theft in " + Criminals.ReformDays + " days");
            return Criminals.ReformThought(id + "|" + lastAt);
        }

        /// For the F7 dump: the bent of the month by name, and those gone straight.
        public string CriminalsStatus(long now)
        {
            if (Cfg == null || !Cfg.CriminalClass)
                return "off";
            var bent = new List<string>();
            int reformed = 0;
            foreach (string id in Store.Ties.Where(t => t != null && Criminals.IsCrime(t.Kind) && t.Doer != null).Select(t => t.Doer).Distinct().ToList())
            {
                long last;
                int n = Criminals.Crimes(Store.Ties, id, now, out last);
                if (n > 0)
                {
                    Person p = KinPerson(id);
                    bent.Add((p != null ? p.Name : id) + " (" + n + ")");
                }
                else if (Criminals.Reformed(Store.Ties, id, now, out last))
                    reformed++;
            }
            return bent.Count + " of the bent this month" + (bent.Count > 0 ? ": " + string.Join(", ", bent.Take(6)) : "") + "; " + reformed + " gone straight (" + _reformedNoticed + " noticed this session)";
        }
    }
}
