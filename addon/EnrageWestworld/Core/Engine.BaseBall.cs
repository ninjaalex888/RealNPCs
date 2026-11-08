// Town base ball in the engine (FIVEDAY reserve R16; Core/BaseBall.cs): Thursday posts the Saturday's game in season; over,
// the result in the morning, the season's tally (Store.Counts "baseball:<town>" wins), an "envy" tie from a loser's man to a
// winner's, and, attended, the home regulars a little warmer to the stranger in the crowd.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private void BaseBallDaily(long now)
        {
            if (Cfg == null || !Cfg.TownEvents || !Cfg.EventBaseBall || Store.Events == null)
                return;
            string home;
            if (!BaseBall.PostsToday(InnerLife.Weekday, InnerLife.ClockMonth, now / 1440, out home))
                return;
            BookBaseBall(home, now / 1440 + 2, now);
        }

        private TownEvent BookBaseBall(string home, long day, long now)
        {
            if (Store.Events.Any(x => x != null && x.Kind == BaseBall.Kind && !x.Done))
                return null;
            var e = new TownEvent
            {
                Id = Ids.New(), Kind = BaseBall.Kind, Town = home, Title = "Base ball: Valentine against Strawberry", At = day * 1440 + BaseBall.FromMin, Until = day * 1440 + BaseBall.ToMin,
                Extra = home == BaseBall.A ? BaseBall.B : BaseBall.A, Posted = true, PostedAt = now
            };
            Store.Events.Add(e);
            Push(new Toast { Kind = "life", Title = ShortTown(home), Weight = 20, Text = BaseBall.Posted(home) });
            Log("EVENT baseball at " + home + " against " + e.Extra + ", day " + day);
            return e;
        }

        public string BaseBallTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventBaseBall)
                return "off ([World] TownEvents, EventBaseBall)";
            string home = town == BaseBall.B ? BaseBall.B : BaseBall.A;
            TownEvent e = BookBaseBall(home, now / 1440, now);
            if (e == null)
                return "no: a game is booked already";
            e.At = now + 2;
            e.Until = now + 60;
            return "Base ball in two minutes at " + home + " (staged where you are if you're there).";
        }

        private void BaseBallOver(TownEvent e, long now, bool attended)
        {
            e.Outcome = attended ? "attended" : "away";
            int h, a;
            BaseBall.Score(e.Id ?? "", out h, out a);
            string winner = h > a ? e.Town : e.Extra, loser = h > a ? e.Extra : e.Town;
            Store.Counts["baseball:" + winner] = Store.Count("baseball:" + winner) + 1;
            Resident wm = Store.Residents.Where(r => r.Town == winner && r.Regular && !r.Woman && !r.Dead).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).FirstOrDefault();
            Resident lm = Store.Residents.Where(r => r.Town == loser && r.Regular && !r.Woman && !r.Dead).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).FirstOrDefault();
            if (wm != null && lm != null)
                Ties.Note(Store.Ties, lm.Id, lm.Name, wm.Id, wm.Name, null, "envy", now);   // sore losers
            if (attended)
                foreach (Resident r in Store.Residents.Where(r => r.Regular && !r.Dead && r.Town == e.Town).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).Take(5))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + BaseBall.Affection, -100, 100);
                }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = BaseBall.Result(e.Town, e.Extra, h, a), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT baseball: " + BaseBall.Result(e.Town, e.Extra, h, a) + " (" + e.Outcome + ")");
        }
    }
}
