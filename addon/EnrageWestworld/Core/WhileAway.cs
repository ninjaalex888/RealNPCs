// The town moves on while you're away (FIVEDAY item 19). The daily tick already lived a town's days without the player:
// its events resolved, weddings held, people dead, gone west on the train or come home, a new face behind a counter. Now
// when he rides back into a town after two game days or more, what he missed comes to him: a "While you were away" item in
// the Around town box, and someone near who has met him means to tell him (a thought over their head).
// Gathered from the store since his last time there (Store.TownLastAbout, kept every few seconds while he is about town). Behind [World] WhileAway.
//
// Why it reads true: where everyone knew everyone, a neighbour back from a trip was brought up to date at the first
// meeting, the deaths and weddings first. Plain C#, no game; all words original. mindtest WhileAwayTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class AwayNews
    {
        public long At;
        public int Weight;       // the deaths first, then the weddings, the leavings and the rest
        public string Text;      // a sentence for the box ("Elihu Starling died on Tuesday.")
        public string Thought;   // what a neighbour thinks of telling him, or null
    }

    public static class WhileAway
    {
        public const long AwayMinutes = 2 * 1440;   // two game days away before it's news
        public const int MostItems = 5;

        /// What happened in the town since `since`, the weightiest first (at most MostItems).
        public static List<AwayNews> Gather(Store s, string town, long since, long now)
        {
            var list = new List<AwayNews>();
            if (s == null || town == null)
                return list;
            foreach (Resident r in s.Residents)
            {
                if (r == null || r.Town != town || r.Name == null)
                    continue;
                if (r.Dead && r.DiedAt > since && r.DiedAt <= now)
                    list.Add(new AwayNews { At = r.DiedAt, Weight = 50, Text = r.Name + " died while you were gone. There's a fresh grave.", Thought = "Does he know about " + Person.FirstOf(r.Name) + "? Somebody ought to tell him." });
                else if (r.Left && r.LeftAt > since && r.LeftAt <= now)
                    list.Add(new AwayNews { At = r.LeftAt, Weight = 30, Text = r.Name + " took the train out and isn't coming back, they say.", Thought = Person.FirstOf(r.Name) + " left on the train. He won't have heard." });
                else if (r.PostId != null && r.TookOverAt > since && r.TookOverAt <= now)
                    list.Add(new AwayNews { At = r.TookOverAt, Weight = 25, Text = "There's a new face at the " + (r.Trade ?? "counter") + ": " + r.Name + (r.KinOf != null ? ", " + r.KinOf + "'s " + (r.Kin ?? "kin") : "") + ".", Thought = null });
            }
            foreach (Wedding w in s.Weddings)
                if (w != null && w.Done && w.Town == town && w.At > since && w.At <= now)
                    list.Add(new AwayNews { At = w.At, Weight = 40, Text = w.AName + " and " + w.BName + " were married.", Thought = "He missed the wedding. " + Person.FirstOf(w.AName) + " and " + Person.FirstOf(w.BName) + ", at last." });
            foreach (TownEvent e in s.Events)
            {
                if (e == null || !e.Done || e.Town != town || e.At <= since || e.At > now || e.Attended)
                    continue;
                string t = EventMissed(e);
                if (t != null)
                    list.Add(new AwayNews { At = e.At, Weight = e.Kind == "gallows" ? 45 : e.Kind == "trial" || e.Kind == "election" ? 35 : 20, Text = t, Thought = null });
            }
            foreach (Breakdown b in s.Breakdowns)
            {
                Resident r = s.FindResident(b.Id);
                if (r == null || r.Town != town || r.Dead || b.Day * 1440 <= since || b.Day * 1440 > now)
                    continue;
                string t = b.Way == StressLoad.Drink ? r.Name + " has taken to drink, this last week." : b.Way == StressLoad.Pray ? r.Name + " has been at the church every day." :
                           b.Way == StressLoad.Grave ? r.Name + " can't keep away from the graveyard." : b.Way == StressLoad.Temper ? "There's been shouting at " + Person.FirstOf(r.Name) + "'s house." : null;
                if (t != null)
                    list.Add(new AwayNews { At = b.Day * 1440, Weight = 15, Text = t, Thought = null });
            }
            return list.OrderByDescending(n => n.Weight).ThenByDescending(n => n.At).Take(MostItems).ToList();
        }

        /// A town's event he missed, in a sentence, or null.
        public static string EventMissed(TownEvent e)
        {
            switch (e.Kind)
            {
                case "dance": return "There was a dance. You missed it.";
                case "election":
                    if (e.Outcome == null || e.Outcome == "called off" || e.Outcome.IndexOf(" (") < 0)
                        return null;
                    return "The election's been and gone. " + e.Outcome.Substring(0, e.Outcome.IndexOf(" (")) + " won it.";   // Outcome: "Name (va-vb)" (Engine.Events.cs)
                case "trial":
                    if (e.Outcome != "guilty" && e.Outcome != "not guilty")
                        return null;
                    return "The circuit judge sat" + (e.AName != null ? ". " + e.AName + " was found " + e.Outcome + "." : ".");
                case "gallows":
                    if (e.Outcome == null || !e.Outcome.StartsWith("hanged"))
                        return null;
                    return e.AName != null ? e.AName + " was hanged." : "There was a hanging.";
                case "show": return "The Wild West show came through. The whole town went.";
                case PrizeFight.Kind: return e.Count2 > 0 && e.AName != null && e.BName != null ? "There was a fight out back of the saloon. " + (e.Count2 == 1 ? e.AName : e.BName) + " won it." : null;
                default: return null;
            }
        }

        /// The box's item: the days he was gone, then the news.
        public static string Box(string town, long since, long now, List<AwayNews> news)
        {
            if (news == null || news.Count == 0)
                return null;
            long days = (now - since) / 1440;
            return "Back in " + town + " after " + (days >= 14 ? "weeks" : days + " days") + ". " + string.Join(" ", news.Select(n => n.Text));
        }
    }
}
