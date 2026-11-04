// The Wild West show in the engine (SIXDAY item 10d; Core/Shows.cs): the daily tick books a show once the legend has travelled
// far enough (at most one a month), the posters and the paper, the review and the legend bent further when it's over, and the
// player's part when he was there: heckled, revealed, robbed. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// From EventsDaily: a show booked a week ahead when his legend has travelled, one a month at most.
        private void ShowsDaily(long now, long today)
        {
            if (Store.Events.Any(e => e != null && e.Kind == "show" && Math.Abs(e.At - now) < Shows.GapDays * 1440L))
                return;
            Deed d = Shows.MostTold(Store.Deeds, Store.TownRumours);
            if (d == null)
                return;
            List<TownRumour> talk = Store.TownRumours.Where(r => r != null && r.DeedId == d.Id).ToList();
            int maxHops = talk.Count == 0 ? 0 : talk.Max(r => r.Hops);
            if (!Shows.FameEnough(talk.Count, maxHops))
                return;
            TownRumour furthest = Shows.Furthest(talk, d.Id);
            string town = Shows.PickTown(Shows.TownsFor(Voice.NowYear), Store.TownVisits, furthest != null ? furthest.Town : null);
            if (town == null)
                return;   // no large town he has been to: he'd never hear of it
            BookShow(town, today + Shows.PostDaysAhead, d, furthest, now);
        }

        private TownEvent BookShow(string town, long day, Deed d, TownRumour furthest, long now)
        {
            TownEvent e = Shows.Make(town, day, d, furthest, (d != null ? d.Id : "test") + "|" + day);
            e.Posted = true;
            e.PostedAt = now;
            Store.Events.Add(e);
            Push(new Toast { Kind = "life", Title = "Posters in " + ShortTown(town), Weight = 40, Text = Shows.PostedText(e) });
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = Shows.PaperText(e) });
            Store.Papers.Add(new Paper { Id = Ids.New(), Kind = "clipping", Title = e.AName + ", " + town, From = "a poster", Body = Shows.PosterBody(e), At = now, Delivered = true, Town = town });
            Log("EVENT show " + town + ": " + e.AName + " posted for " + DateText(e.At) + "; the bill: " + e.Extra + (furthest != null ? " (the version told in " + furthest.Town + ", " + furthest.Hops + " hops, fidelity " + furthest.Fidelity + ")" : " (a test legend)"));
            return e;
        }

        /// The version the show acts out: the furthest rumour's, as that town tells it.
        public string ShowVersion(TownEvent e)
        {
            Deed d = e.AId != null ? Store.Deeds.FirstOrDefault(x => x.Id == e.AId) : null;
            TownRumour r = e.BId != null ? Store.TownRumours.FirstOrDefault(x => x.Id == e.BId) : null;
            if (d == null || r == null)
                return Shows.TestVersion;
            return Rumours.Version(r, d, Player, PlayerNameKnownIn(r.Town)) ?? Shows.TestVersion;
        }

        /// The show over (there or away): the review, the legend bent further from this town, the town's memories.
        private void ShowOver(TownEvent e, long now, bool attended)
        {
            string part = e.Part ?? "";
            bool revealed = part.Contains("revealed"), robbed = part.Contains("robbed"), heckled = part.Contains("heckled");
            string version = ShowVersion(e);
            e.Outcome = (attended ? "attended" : "away") + (revealed ? ", revealed" : "") + (robbed ? ", robbed" : "") + (heckled ? ", heckled" : "");
            if (Cfg.Newspaper)
                Requests.Add(new WorldRequest { Kind = "paper", Town = e.Town, Text = Shows.Review(e, version, revealed, robbed, heckled) });
            Push(new Toast { Kind = "life", Title = e.Title, Weight = 30, Text = Shows.OverToast(e, attended) });
            // the legend bends again, from here: this town's version worse than it was, and sent on to towns without one
            Deed d = e.AId != null ? Store.Deeds.FirstOrDefault(x => x.Id == e.AId) : null;
            TownRumour far = e.BId != null ? Store.TownRumours.FirstOrDefault(x => x.Id == e.BId) : null;
            if (d != null)
            {
                TownRumour here = Store.TownRumours.FirstOrDefault(x => x.DeedId == d.Id && x.Town == e.Town);
                if (here == null)
                {
                    here = new TownRumour { Id = Ids.New(), DeedId = d.Id, Origin = d.Town, Town = e.Town, Hops = (far != null ? far.Hops : 0) + 1, Fidelity = Shows.Bend(far != null ? far.Fidelity : 50), At = now };
                    Store.TownRumours.Add(here);
                }
                else
                    here.Fidelity = Shows.Bend(here.Fidelity);
                here.NextAt = now + 900;
                here.Heard = false;
                Log("EVENT show " + e.Town + ": the legend bent again here (fidelity " + here.Fidelity + "); it spreads from " + e.Town);
            }
            foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && !x.Fled && x.Held == null && x.Town == e.Town))
            {
                bool there = Voice.Hash(e.Id + "|crowd|" + r.Id) % 100 < 60;
                Remember(r.Id, "talk", revealed && there ? "revealed" : "wildwest", there ? "saw" : "heard", e.Town, now, revealed && there ? 1 : 0, revealed && there ? 55 : 35, "show:" + e.Id + ":" + r.Id);
            }
            Log("EVENT show " + e.Town + ": over, " + e.Outcome + "; the act: " + version);
        }

        /// The player heckled: the town's regulars who were there think the less of him.
        public void ShowHeckled(TownEvent e, long now)
        {
            if (e == null || e.Done)
                return;
            e.Part = (e.Part ?? "") + "heckled;";
            foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && x.Held == null && x.Town == e.Town).Take(Shows.HeckleRegulars))
            {
                PersonState st = MindOf(r.Id, r.Name);
                if (st != null)
                    st.Affection = Romance.Clamp(st.Affection + Shows.HeckleSoured, -100, 100);
            }
            Log("EVENT show " + e.Town + ": you heckled; the crowd turned on you");
        }

        /// He stood up and said he was the man in the act: a deed everyone there saw.
        public void ShowRevealed(TownEvent e, long now)
        {
            if (e == null || e.Done)
                return;
            e.Part = (e.Part ?? "") + "revealed;";
            if (Cfg.Deeds)
                OnDeed(new Deed { Id = Ids.New(), Kind = "reveal", Detail = e.Extra, Town = e.Town, At = now, Weight = Shows.RevealWeight, Witnesses = 8 }, new List<string>(), now);
            Log("EVENT show " + e.Town + ": you stood up as the real one");
        }

        /// He robbed the takings: the money (the game adds it), a robbery deed; the show breaks up after.
        public int ShowRobbed(TownEvent e, long now)
        {
            if (e == null || e.Done)
                return 0;
            e.Part = (e.Part ?? "") + "robbed;";
            int cents = Shows.Takings(e.Id);
            if (Cfg.Deeds)
                OnDeed(new Deed { Id = Ids.New(), Kind = "robbery", Detail = "the Wild West show's takings", Town = e.Town, At = now, Weight = 55, Witnesses = 8 }, new List<string>(), now);
            Log("EVENT show " + e.Town + ": you robbed the takings ($" + cents / 100 + ")");
            return cents;
        }

        /// A Testing row: a show posted here for tomorrow at two from his most-told deed (a test legend without one); posted
        /// already, it begins in two minutes. What was done.
        public string ShowTest(string town, long now)
        {
            if (!Cfg.TownEvents || !Cfg.EventShows)
                return "off ([World] TownEvents, EventShows)";
            if (town == null)
                return "no town here";
            TownEvent open = Store.Events.FirstOrDefault(x => x != null && !x.Done && x.Kind == "show" && x.Town == town);
            if (open != null)
            {
                if (now >= open.At - 30 && now < open.Until)
                    return "the show is on now";
                open.At = now + 2;
                open.Until = open.At + (Shows.ShowTo - Shows.ShowFrom) * 60;
                Log("EVENT show " + town + ": moved up by the Testing row, begins in two minutes");
                return open.AName + " begins in two minutes";
            }
            Deed d = Shows.MostTold(Store.Deeds, Store.TownRumours);
            TownRumour far = d != null ? Shows.Furthest(Store.TownRumours, d.Id) : null;
            TownEvent e = BookShow(town, now / 1440 + 1, d, far, now);
            return e.AName + " posted for tomorrow at two; the bill: " + e.Extra + (d == null ? " (a test legend: no deed of yours has travelled yet)" : "") + " (the row again: it begins in two minutes)";
        }

        private string ShowsStatus(TownEvent e)
        {
            return e.AName + ": " + e.Extra + (e.Part != null ? " (" + e.Part.TrimEnd(';').Replace(";", ", ") + ")" : "");
        }
    }
}
