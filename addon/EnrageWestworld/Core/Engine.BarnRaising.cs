// A barn raising in the engine (FIVEDAY reserve R7; Core/BarnRaising.cs): the day's roll books a Saturday in a country town
// (a farm family, and the two men at odds for one beam if the town has them: Extra "aId|aName|bId|bName"); over, the feud
// mended or not (decided once, by the event's seed, so the staging and the end agree), the helpers' ties to the family, the
// regulars' thought of the stranger, the morning's word. Store.Counts "barn:<town>" the last booked day. Nothing new saved.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _barnsBooked;

        private void BarnRaisingDaily(long now)
        {
            if (Cfg == null || !Cfg.TownEvents || !Cfg.EventBarnRaisings || Store.Events == null)
                return;
            long day = now / 1440;
            foreach (string town in BarnRaising.Towns)
                if (BarnRaising.Books(town, day, InnerLife.ClockMonth, Store.Count("barn:" + town) == 0 ? -999 : Store.Count("barn:" + town)))
                {
                    if (BookBarnRaising(town, day + BarnRaising.DaysToSaturday(InnerLife.Weekday), now) != null)
                        break;   // one town a day
                }
        }

        private TownEvent BookBarnRaising(string town, long day, long now)
        {
            if (Store.Events.Any(x => x != null && x.Kind == BarnRaising.Kind && x.Town == town && !x.Done))
                return null;
            var people = Store.Residents.Where(r => r.Town == town && !r.Dead && !r.Left && !r.Fled).ToList();
            Resident fam = BarnRaising.Family(people, town + "|" + day);
            if (fam == null)
                return null;
            var men = new System.Collections.Generic.HashSet<string>(people.Where(r => !r.Woman && r.Id != fam.Id).Select(r => r.Id));
            SceneTie feud;
            string extra = BarnRaising.Feud(Store.Ties, men, out feud) ? feud.A + "|" + feud.AName + "|" + feud.B + "|" + feud.BName : null;
            var e = new TownEvent
            {
                Id = Ids.New(), Kind = BarnRaising.Kind, Town = town, Title = "A barn raising", At = day * 1440 + BarnRaising.FromMin, Until = day * 1440 + BarnRaising.ToMin,
                AId = fam.Id, AName = fam.Name, Extra = extra, Posted = true, PostedAt = now
            };
            Store.Events.Add(e);
            Store.Counts["barn:" + town] = (int)(now / 1440);
            _barnsBooked++;
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 20, Text = BarnRaising.Posted(town, fam.Name) });
            Log("EVENT barnraising " + town + ": for " + fam.Name + " on day " + day + (extra != null ? "; " + feud.AName + " and " + feud.BName + " on one beam" : ""));
            return e;
        }

        public string BarnRaisingTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventBarnRaisings)
                return "off ([World] TownEvents, EventBarnRaisings)";
            if (town == null)
                return "no town here";
            TownEvent e = BookBarnRaising(town, now / 1440, now);
            if (e == null)
                return "no farm family of " + town + " known to RealNPCs (or one is booked already)";
            e.At = now + 2;
            e.Until = now + 60;
            return "A barn raising in two minutes here, for " + e.AName + (e.Extra != null ? "; two at odds on one beam" : "") + ".";
        }

        /// The two at odds, from the event (null when none), and whether the beam mends it.
        public static bool BarnPair(TownEvent e, out string aId, out string aName, out string bId, out string bName, out bool mends)
        {
            aId = aName = bId = bName = null;
            mends = false;
            string[] p = e != null && e.Extra != null ? e.Extra.Split('|') : null;
            if (p == null || p.Length < 4)
                return false;
            aId = p[0]; aName = p[1]; bId = p[2]; bName = p[3];
            mends = BarnRaising.Mends(e.Id ?? "");
            return true;
        }

        private void BarnRaisingOver(TownEvent e, long now, bool attended)
        {
            e.Outcome = attended ? "attended" : "away";
            string aId, aName, bId, bName, pair = null;
            bool mends;
            if (BarnPair(e, out aId, out aName, out bId, out bName, out mends))
            {
                Ties.Note(Store.Ties, aId, aName, bId, bName, e.Town, mends ? "makeup" : "argue", now);
                pair = Person.FirstOf(aName) + " and " + Person.FirstOf(bName);
            }
            foreach (Resident r in Store.Residents.Where(r => r.Town == e.Town && r.Regular && !r.Dead && !r.Woman && r.Id != e.AId).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).Take(BarnRaising.Helpers))
                Ties.Note(Store.Ties, r.Id, r.Name, e.AId, e.AName, e.Town, "help", now, r.Id);   // the neighbours who came
            if (attended)
                foreach (Resident r in Store.Residents.Where(r => r.Regular && !r.Dead && r.Town == e.Town).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).Take(6))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + BarnRaising.Affection, -100, 100);
                }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = BarnRaising.Morning(e, pair, mends, attended), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT barnraising " + e.Town + ": " + e.AName + "'s barn up (" + e.Outcome + ")" + (pair != null ? "; " + pair + (mends ? " mended it" : " fell out again") : ""));
        }
    }
}
