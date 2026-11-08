// A box social in the engine (FIVEDAY reserve R6; Core/BoxSocial.cs): posted on the Thursday before the first Saturday of
// the month in the settled towns that have the three to stand for it (her, and two who want her box), and settled when it's
// over: the winner and she a "flirt" tie, the loser an "envy" tie with the winner; or, if you bought it (Part "you"), her
// fondness for you and the outbid suitor's soreness at you. The morning's word either way.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _boxSocials;

        private void BoxSocialDaily(long now)
        {
            if (Cfg == null || !Cfg.TownEvents || !Cfg.EventBoxSocials || Store.Events == null || !BoxSocial.PostsToday(InnerLife.Weekday, InnerLife.ClockDay, InnerLife.ClockMonth >= 1 && InnerLife.ClockMonth <= 12 ? System.DateTime.DaysInMonth(InnerLife.ClockYear, InnerLife.ClockMonth) : 31))
                return;
            long day = now / 1440;
            foreach (string town in BoxSocial.Towns)
                BookBoxSocial(town, day + 2, now);
        }

        private TownEvent BookBoxSocial(string town, long day, long now)
        {
            if (Store.Events.Any(x => x != null && x.Kind == BoxSocial.Kind && x.Town == town && !x.Done))
                return null;
            Resident girl, winner, loser;
            if (!BoxSocial.Cast(Store.Residents.Where(r => r.Regular && r.Town == town && !r.Left && !r.Fled && r.Held == null).ToList(), Store.Ties, Voice.NowYear, town + "|" + day, out girl, out winner, out loser))
                return null;
            var e = new TownEvent
            {
                Id = Ids.New(), Kind = BoxSocial.Kind, Town = town, Title = "A box social", At = day * 1440 + BoxSocial.FromMin, Until = day * 1440 + BoxSocial.ToMin,
                AId = girl.Id, AName = girl.Name, BId = winner.Id, BName = winner.Name, Extra = loser.Id + "|" + loser.Name, Posted = true, PostedAt = now
            };
            Store.Events.Add(e);
            _boxSocials++;
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 20, Text = BoxSocial.Posted(town) });
            Log("EVENT boxsocial " + town + ": " + girl.Name + "'s box, " + winner.Name + " and " + loser.Name + " after it");
            return e;
        }

        public string BoxSocialTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventBoxSocials)
                return "off ([World] TownEvents, EventBoxSocials)";
            if (town == null)
                return "no town here";
            TownEvent e = BookBoxSocial(town, now / 1440, now);
            if (e == null)
                return "no young woman and two young men among " + town + "'s regulars (or one is booked already)";
            e.At = now + 2;
            e.Until = now + 50;
            return "A box social in two minutes here: " + e.AName + "'s box, " + e.BName + " after it.";
        }

        private void BoxSocialOver(TownEvent e, long now, bool attended)
        {
            e.Outcome = attended ? "attended" : "away";
            bool you = e.Part == "you";
            string[] lo = (e.Extra ?? "|").Split('|');
            string loserId = lo[0], loserName = lo.Length > 1 ? lo[1] : null;
            if (you)
            {
                PersonState st = MindOf(e.AId, e.AName);
                if (st != null)
                    st.Affection = Romance.Clamp(st.Affection + BoxSocial.Affection, -100, 100);
                PersonState sore = MindOf(e.BId, e.BName);   // the suitor you outbid is sore at you
                if (sore != null)
                    sore.Affection = Romance.Clamp(sore.Affection - BoxSocial.Sore, -100, 100);
            }
            else
            {
                Ties.Note(Store.Ties, e.BId, e.BName, e.AId, e.AName, e.Town, "flirt", now);
                if (loserId != "")
                    Ties.Note(Store.Ties, loserId, loserName, e.BId, e.BName, e.Town, "envy", now);
            }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = BoxSocial.Morning(e, you), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT boxsocial " + e.Town + ": " + (you ? "you bought " + e.AName + "'s box" : e.BName + " bought " + e.AName + "'s box over " + loserName) + " (" + e.Outcome + ")");
        }
    }
}
