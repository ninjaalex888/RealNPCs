// The festivals in the engine (FIVEDAY reserve R17-R19; Core/Festivals.cs): the daily tick books tomorrow's (its town, its
// winners named from the town's regulars: AId the man, BId the woman), and the end settles it: a neighbour's "cheer" to each
// winner, a rival woman's "envy" to the woman's, the regulars warmer to the stranger who came, the morning's word.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private bool FestivalOn(string kind)
        {
            if (Cfg == null || !Cfg.TownEvents)
                return false;
            switch (kind)
            {
                case "fair": return Cfg.EventFair;
                default: return false;
            }
        }

        private void FestivalsDaily(long now)
        {
            if (Cfg == null || !Cfg.TownEvents || Store.Events == null || InnerLife.ClockMonth < 1)
                return;
            int y, m, d;
            Festivals.Tomorrow(InnerLife.ClockYear, InnerLife.ClockMonth, InnerLife.ClockDay < 1 ? 1 : InnerLife.ClockDay, out y, out m, out d);
            foreach (FestivalKind f in Festivals.All)
                if (FestivalOn(f.Kind) && Festivals.On(f, y, m, d))
                    foreach (string town in f.Towns)
                        BookFestival(f, town, now / 1440 + 1, now);
        }

        private TownEvent BookFestival(FestivalKind f, string town, long day, long now)
        {
            if (Store.Events.Any(x => x != null && x.Kind == f.Kind && x.Town == town && !x.Done))
                return null;
            var regs = Store.Residents.Where(r => r.Town == town && r.Regular && !r.Dead && !r.Left && !r.Fled && r.Held == null).OrderBy(r => Voice.Hash(r.Id + "|" + f.Kind + "|" + day)).ToList();
            Resident man = f.ManPrize != null ? regs.FirstOrDefault(r => !r.Woman) : null, woman = f.WomanPrize != null ? regs.FirstOrDefault(r => r.Woman) : null;
            var e = new TownEvent
            {
                Id = Ids.New(), Kind = f.Kind, Town = town, Title = f.Title, At = day * 1440 + f.FromMin, Until = day * 1440 + f.ToMin,
                AId = man != null ? man.Id : null, AName = man != null ? man.Name : null, BId = woman != null ? woman.Id : null, BName = woman != null ? woman.Name : null,
                Posted = true, PostedAt = now
            };
            Store.Events.Add(e);
            Push(new Toast { Kind = "life", Title = ShortTown(town), Weight = 25, Text = Festivals.Fill(f.Posted, town, e.AName, e.BName) });
            Log("EVENT " + f.Kind + " " + town + ": tomorrow" + (man != null ? ", " + man.Name + "'s prize" : "") + (woman != null ? ", " + woman.Name + "'s" : ""));
            return e;
        }

        public string FestivalTest(string kind, string town, long now)
        {
            FestivalKind f = Festivals.Of(kind);
            if (f == null || !FestivalOn(kind))
                return "off ([World] TownEvents and the festival's own switch)";
            string at = town != null && f.Towns.Contains(town) ? town : f.Towns[0];
            TownEvent e = BookFestival(f, at, now / 1440, now);
            if (e == null)
                return "no: one is booked at " + at + " already";
            e.At = now + 2;
            e.Until = now + 60;
            return f.Title + " in two minutes at " + at + " (staged if you're there).";
        }

        private bool IsFestival(string kind) { return Festivals.Of(kind) != null; }

        private void FestivalOver(TownEvent e, long now, bool attended)
        {
            FestivalKind f = Festivals.Of(e.Kind);
            e.Outcome = attended ? "attended" : "away";
            var regs = Store.Residents.Where(r => r.Town == e.Town && r.Regular && !r.Dead && r.Id != e.AId && r.Id != e.BId).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).ToList();
            Resident friend = regs.FirstOrDefault(r => !r.Woman), rival = regs.FirstOrDefault(r => r.Woman);
            if (e.AId != null && friend != null)
                Ties.Note(Store.Ties, friend.Id, friend.Name, e.AId, e.AName, e.Town, "cheer", now);
            if (e.BId != null && rival != null)
                Ties.Note(Store.Ties, rival.Id, rival.Name, e.BId, e.BName, e.Town, "envy", now);   // a rival's sour face at the ribbon
            if (attended)
                foreach (Resident r in regs.Take(6))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + (f != null ? f.Affection : 2), -100, 100);
                }
            if (f != null)
                Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = Festivals.Fill(f.Morning, e.Town, Person.FirstOf(e.AName), Person.FirstOf(e.BName)), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT " + e.Kind + " " + e.Town + ": over (" + e.Outcome + ")");
        }
    }
}
