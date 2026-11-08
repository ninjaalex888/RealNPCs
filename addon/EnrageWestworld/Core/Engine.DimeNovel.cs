// Dime novels in the engine (FIVEDAY reserve R12; Core/DimeNovel.cs): on a Monday, the daily tick may publish one for your
// most-told deed (once a deed, sixty days apart), with its title in the box; NovelNow tells the game side which (for the
// readers' thoughts these thirty days). Store.Counts "novel:<deedId>" the day, "novel:last" the last day: nothing new saved.
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        private int _novels;
        private static readonly Deed TestDeed = new Deed { Id = "test-novel", Kind = "kill", Town = "Valentine" };

        private void DimeNovelDaily(long now)
        {
            if (Cfg == null || !Cfg.DimeNovels)
                return;
            Deed d = Shows.MostTold(Store.Deeds, Store.TownRumours);
            if (d == null)
                return;
            int towns = Store.TownRumours == null ? 0 : Store.TownRumours.Count(r => r != null && r.DeedId == d.Id);
            long day = now / 1440;
            if (!DimeNovel.Publishes(InnerLife.Weekday, towns, Store.Count(DimeNovel.Prefix + d.Id) > 0, day, Store.Count(DimeNovel.LastKey) == 0 ? -999 : Store.Count(DimeNovel.LastKey)))
                return;
            Publish(d, day, towns);
        }

        private string Publish(Deed d, long day, int towns)
        {
            Store.Counts[DimeNovel.Prefix + d.Id] = (int)day;
            Store.Counts[DimeNovel.LastKey] = (int)day;
            _novels++;
            string title = DimeNovel.Title(Player.FullName, d);
            Push(new Toast { Kind = "life", Title = "A dime novel", Weight = 40, Text = DimeNovel.Notice(title, d.Town) });
            Log("NOVEL published, of " + d.Kind + " at " + (d.Town ?? d.Zone) + " (told in " + towns + " towns): " + title);
            return title;
        }

        /// The novel being read now (published these thirty days), its title, or null.
        public string NovelNow(long now)
        {
            if (Cfg == null || !Cfg.DimeNovels)
                return null;
            int last = Store.Count(DimeNovel.LastKey);
            if (!DimeNovel.Reading(now / 1440, last))
                return null;
            Deed d = Store.Deeds.FirstOrDefault(x => x != null && Store.Count(DimeNovel.Prefix + x.Id) == last) ?? (Store.Count(DimeNovel.Prefix + TestDeed.Id) == last ? TestDeed : null);
            return d != null ? DimeNovel.Title(Player.FullName, d) : null;
        }

        /// The Testing row: your most-told deed written up now, else a test deed's.
        public string DimeNovelTest(long now)
        {
            if (Cfg == null || !Cfg.DimeNovels)
                return "off ([World] DimeNovels)";
            Deed d = Shows.MostTold(Store.Deeds, Store.TownRumours) ?? TestDeed;   // (a test deed is never added to your deeds)
            return "Published: " + Publish(d, now / 1440, 0);
        }
    }
}
