// The shivaree in the engine (FIVEDAY item 13; Core/Shivaree.cs): booked as a wedding in town finishes, settled when its hour
// is over: the couple's friends a step warmer to them, the couple a memory of it, the stranger who was there thought the
// better of by the town's regulars, and the morning's word. Plain C#, no game.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// From FinishWedding: the night after, a shivaree for the couple.
        private void BookShivaree(Wedding w, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventShivarees || Store.Events == null)
                return;
            TownEvent e = Shivaree.For(w, now);
            if (e == null || Store.Events.Any(x => x != null && x.Kind == Shivaree.Kind && x.AId == e.AId && !x.Done))
                return;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            Log("EVENT shivaree " + e.Town + ": for " + e.AName + " and " + e.BName + ", tomorrow night from " + Shivaree.FromHour + ":00");
        }

        /// The Testing row: a shivaree here two game minutes from now, for two of the town's regulars as the couple.
        public string ShivareeTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventShivarees)
                return "off ([World] TownEvents, EventShivarees)";
            if (town == null)
                return "no town here";
            Resident man = Store.Residents.FirstOrDefault(r => r.Regular && !r.Dead && !r.Woman && r.Town == town);
            Resident woman = Store.Residents.FirstOrDefault(r => r.Regular && !r.Dead && r.Woman && r.Town == town);
            if (man == null || woman == null)
                return "no man and woman among " + town + "'s regulars to stand for the couple";
            TownEvent e = Shivaree.For(new Wedding { AId = man.Id, AName = man.Name, BId = woman.Id, BName = woman.Name, AWoman = false, BWoman = true, Town = town }, now);
            e.At = now + 2;
            e.Until = now + 50;
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            return "A shivaree in two minutes here, for " + man.Name + " and " + woman.Name + ".";
        }

        private void ShivareeOver(TownEvent e, long now, bool attended)
        {
            e.Outcome = attended ? "attended" : "away";
            var couple = new[] { e.AId, e.BId }.Where(x => x != null).ToList();
            int warmed = 0;
            foreach (SceneTie t in Store.Ties.Where(t => t != null && t.Warmth > 0 && couple.Any(t.Has)).ToList())
            {
                string friend = couple.Contains(t.A) ? t.B : t.A, friendName = couple.Contains(t.A) ? t.BName : t.AName;
                string one = couple.Contains(t.A) ? t.A : t.B, oneName = couple.Contains(t.A) ? t.AName : t.BName;
                if (friend == null || couple.Contains(friend))
                    continue;
                Ties.Note(Store.Ties, friend, friendName, one, oneName, e.Town, "cheer", now);
                warmed++;
            }
            if (Cfg.Minds)
                foreach (string id in couple)
                {
                    PersonState st = MindOf(id, id == e.AId ? e.AName : e.BName);
                    if (st == null)
                        continue;
                    Minds.Note(st.Mind, new MindMemory
                    {
                        Kind = "warmth", Sub = "shivaree", How = "did", Who = new List<string> { id == e.AId ? e.BId : e.AId }, Names = new List<string> { Person.FirstOf(id == e.AId ? e.BName : e.AName) },
                        Town = e.Town, At = now, Feel = 2, Strength = 40, Key = "shivaree:" + e.Id + ":" + id
                    }, now);
                }
            if (attended)
                foreach (Resident r in Store.Residents.Where(r => r.Regular && !r.Dead && r.Town == e.Town).OrderBy(r => Voice.Hash(r.Id + "|" + e.Id)).Take(8))   // eight of the town's, by the night (the Part 3 review: always the same first eight)
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection + Shivaree.Affection, -100, 100);
                }
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(e.Town), At = now, Text = Shivaree.Morning(e, attended), DueAt = (now / 1440 + 1) * 1440 + 8 * 60 });
            Log("EVENT shivaree " + e.Town + ": " + e.Outcome + "; " + warmed + " friends of the couple warmer to them");
        }
    }
}
