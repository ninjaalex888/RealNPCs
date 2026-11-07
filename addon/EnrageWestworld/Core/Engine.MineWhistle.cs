// Three whistles, the engine's part (FIVEDAY item 28; Core/MineWhistle.cs). The daily tick rolls the day (MineWhistle.Today:
// the 4% and the three weeks' gap) and books the minute; the world tick, at that minute, takes the one or two and records
// their deaths through NoteDied (the friends hear of it, the mourning and the wake follow), and the ledger counts them.
// The game side reads LastMine to stage it when the player is in Annesburg; away, a toast. Behind [World] MineWhistle.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class MineAccident
    {
        public long At;
        public List<string> Names = new List<string>();
    }

    public partial class Engine
    {
        public MineAccident LastMine { get; private set; }
        private int _minesSession;

        private void MineDaily(long now)
        {
            if (Cfg == null || !Cfg.MineWhistle)
                return;
            long day = now / 1440;
            int minute;
            if (Store.Count("mine:due") > 0 && Store.Count("mine:due") > now)
                return;   // one booked already
            if (MineWhistle.Today(day, Store.Count("mine:lastday"), out minute))
            {
                Store.Counts["mine:due"] = (int)(day * 1440 + minute);
                Log("MINE an accident is coming at Annesburg today at " + minute / 60 + ":" + (minute % 60).ToString("00"));
            }
        }

        /// From the world tick: the booked minute come.
        private void MineTick(long now)
        {
            int due = Store.Count("mine:due");
            if (Cfg == null || !Cfg.MineWhistle || due <= 0 || now < due)
                return;
            Store.Counts["mine:due"] = 0;
            MineAccidentNow(now, false);
        }

        /// The accident itself (the Testing row's too): the names, the deaths, the ledger, the word away.
        public string MineAccidentNow(long now, bool test)
        {
            List<Resident> dead = MineWhistle.Victims(Store.Residents.Where(r => r.Town == MineWhistle.Town), "mine|" + now);
            if (dead.Count == 0)
                return "no men of Annesburg known to RealNPCs to be below";
            Store.Counts["mine:lastday"] = (int)(now / 1440);
            var acc = new MineAccident { At = now };
            foreach (Resident r in dead)
            {
                NoteDied(new DeathEntry { Id = r.Id, Name = r.Name, Kind = "resident", At = now, Town = MineWhistle.Town, How = "the mine", By = "unknown" });
                acc.Names.Add(r.Name);
            }
            TownLedger l = LedgerOf(MineWhistle.Town);
            if (l != null)
                l.DeathsOther += dead.Count;
            LastMine = acc;
            _minesSession++;
            Push(new Toast { Kind = "life", Title = "Annesburg", Weight = 60, Text = MineWhistle.Away(acc.Names) });
            Log("MINE three whistles at Annesburg: " + string.Join(", ", acc.Names) + " killed below" + (test ? " (a test)" : ""));
            return null;
        }

        public string MineStatus()
        {
            if (Cfg == null || !Cfg.MineWhistle)
                return "off ([World] MineWhistle)";
            int due = Store.Count("mine:due");
            return _minesSession + " accidents this session; the last day " + Store.Count("mine:lastday") + (due > 0 ? "; one booked at minute " + due : "") + (LastMine != null ? "; the last: " + string.Join(", ", LastMine.Names) : "");
        }
    }
}
