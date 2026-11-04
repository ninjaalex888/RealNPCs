// Town events in the engine (SIXDAY item 10; Core/TownEvents.cs): the daily tick posts the coming dances (Thursday for
// Saturday, every second week per town), the world tick resolves one that passed with nobody there (the regulars' ties, their
// memories, the notice), and the game side asks for the one open in the town it's in, then reports it attended or broken.
// Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// From DailyTick: the dances posted ahead.
        private void EventsDaily(long now)
        {
            if (!Cfg.TownEvents || Store.Events == null)
                return;
            int weekday = InnerLife.Weekday;
            if (weekday < 0)
                return;   // the clock's weekday unknown: no scheduling
            long today = now / 1440;
            int toSat = TownEvents.DaysToSaturday(weekday);
            if (!Cfg.EventDances || weekday != TownEvents.DancePostDay)
                return;
            long saturday = today + toSat;
            foreach (string town in TownEvents.DanceTowns)
            {
                if (!TownEvents.DanceWeek(town, saturday))
                    continue;
                if (Store.Events.Any(e => e.Kind == "dance" && e.Town == town && e.At / 1440 == saturday))
                    continue;
                if (!Store.TownVisits.ContainsKey(town))
                    continue;   // a town he has never been to posts nothing he'd hear of
                TownEvent d = TownEvents.Dance(town, saturday);
                d.Posted = true;
                Store.Events.Add(d);
                string when = TownEvents.DaysToSaturday(weekday) == 0 ? "tonight" : "on Saturday";
                Push(new Toast { Kind = "life", Title = "Dance in " + ShortTown(town), Weight = 30, Text = TownEvents.PostedText(d, when) });
                if (Cfg.Newspaper)
                    Requests.Add(new WorldRequest { Kind = "paper", Town = town, Text = TownEvents.PaperText(d, when) });
                Log("EVENT dance " + town + ": posted for " + DateText(d.At));
            }
        }

        /// From WorldTick: an event whose hour has passed with nobody there resolves itself.
        private void EventsTick(long now)
        {
            if (Store.Events == null)
                return;
            foreach (TownEvent e in Store.Events)
                if (e != null && !e.Done && now >= e.Until)
                    ResolveAway(e, now);
        }

        private void ResolveAway(TownEvent e, long now)
        {
            e.Done = true;
            e.Outcome = "away";
            if (e.Kind == "dance")
                DanceLeaves(e, now, false);
            Push(new Toast { Kind = "life", Title = "Word from " + ShortTown(e.Town), Weight = 20, Text = TownEvents.OverText(e, 0, false) });
            Log("EVENT " + e.Kind + " " + e.Town + ": away (" + (e.Count) + " pairs)");
        }

        /// What a dance leaves: the regulars' ties and memories (and, attended, their thought of the player who came).
        private void DanceLeaves(TownEvent e, long now, bool attended)
        {
            List<Resident> regulars = Store.Residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.Town == e.Town).ToList();
            List<string[]> pairs = TownEvents.DanceAway(regulars, Store.Ties, e.Id);
            e.Count = pairs.Count;
            foreach (string[] p in pairs)
            {
                Ties.Note(Store.Ties, p[0], p[1], p[2], p[3], e.Town, p[4], now);
                if (Cfg.Minds)
                    foreach (int i in new[] { 0, 2 })
                    {
                        PersonState st = MindOf(p[i], p[i + 1]);
                        if (st == null)
                            continue;
                        Minds.Note(st.Mind, new MindMemory
                        {
                            Kind = p[4] == "argue" ? "quarrel" : "warmth", Sub = p[4] == "argue" ? "dancequarrel" : "dance", How = "did", Who = new List<string> { p[i == 0 ? 2 : 0] },
                            Names = new List<string> { Person.FirstOf(p[i == 0 ? 3 : 1]) }, Town = e.Town, At = now, Feel = p[4] == "argue" ? -2 : 2, Strength = 40, Key = "dance:" + e.Id + ":" + p[i]
                        }, now);
                        if (attended)
                            st.Affection = Romance.Clamp(st.Affection + TownEvents.DanceAffection, -100, 100);
                    }
            }
        }

        /// The event open now in this town, for the game to stage (null: none).
        public TownEvent EventNear(string town, long now)
        {
            if (!Cfg.TownEvents || Store.Events == null || town == null)
                return null;
            foreach (TownEvent e in Store.Events)
                if (e != null && e.Town == town && TownEvents.Open(e, now))
                    return e;
            return null;
        }

        /// The event's hour is over with the player there (attended), or he left before the end.
        public void FinishEvent(TownEvent e, bool attended, long now)
        {
            if (e == null || e.Done)
                return;
            e.Done = true;
            e.Attended = attended;
            e.Outcome = attended ? "attended" : "left";
            if (e.Kind == "dance")
                DanceLeaves(e, now, attended);
            Push(new Toast { Kind = "life", Title = e.Title, Weight = 25, Text = TownEvents.OverText(e, e.Count, attended) });
            Log("EVENT " + e.Kind + " " + e.Town + ": " + e.Outcome + " (" + e.Count + " pairs)");
        }

        /// Shots or a gun pointed broke it up: nothing comes of it, and the town remembers who fired.
        public void BreakEvent(TownEvent e, bool byYou, string why, long now)
        {
            if (e == null || e.Done)
                return;
            e.Done = true;
            e.Broken = true;
            e.Outcome = "broken: " + why;
            if (byYou)
            {
                foreach (Resident r in Store.Residents.Where(x => x.Regular && !x.Dead && x.Town == e.Town).Take(6))
                {
                    PersonState st = MindOf(r.Id, r.Name);
                    if (st != null)
                        st.Affection = Romance.Clamp(st.Affection - 10, -100, 100);
                }
                Push(new Toast { Kind = "life", Title = e.Title, Weight = 40, Text = "The " + (e.Kind == "dance" ? "dance" : e.Kind) + " in " + e.Town + " broke up in panic. They know who to blame." });
            }
            Log("EVENT " + e.Kind + " " + e.Town + ": broken (" + why + (byYou ? ", by you" : "") + ")");
        }

        /// For F7: what's coming and what passed.
        public string EventsStatus(long now)
        {
            if (!Cfg.TownEvents)
                return "off ([World] TownEvents)";
            if (Store.Events == null || Store.Events.Count == 0)
                return "nothing posted yet (dances are posted on Thursdays for towns you've been to)";
            var parts = new List<string>();
            foreach (TownEvent e in Store.Events.OrderBy(x => x.At))
            {
                if (e == null) continue;
                if (!e.Done)
                    parts.Add(e.Kind + " in " + e.Town + " " + (now >= e.At ? "on now" : "in " + Math.Max(0, e.At - now) / 60 + " h"));
                else if (now - e.Until < 3 * 1440)
                    parts.Add(e.Kind + " in " + e.Town + ": " + e.Outcome);
            }
            return parts.Count == 0 ? "nothing coming" : string.Join("; ", parts);
        }
    }
}
