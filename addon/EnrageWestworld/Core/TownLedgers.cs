// The town ledger (SIXDAY item 11c; roadmap 11; roadmap.md P25): towns that grow or shrink. Each town's ledger is read off the
// records the add-on already keeps (its deaths, the player's deeds there, who came by train and who left, what its events came
// to), so it can never drift and a rewind needs nothing special: the index starts at 50 and moves with what happened, the mood
// comes from the last month. A grieving town thinks in mourning, a fearful one locks up early, a prospering one complains of
// prices and new faces; the regulars a town keeps scale with the index, never below ten. A town the player never goes to has no
// records and never changes. Why it reads true: a town's population in 1899 was a running sum of trains, funerals and the
// paper's Personal Mention column, and everyone could tell you which way it was running. Plain C#, no game; all words original.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class TownLedger
    {
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public int Index = 50;          // 0-100
        [DataMember(EmitDefaultValue = false)] public string Mood;             // prospering, steady, fearful, grieving
        [DataMember(EmitDefaultValue = false)] public int Arrivals, Departures, DeathsByPlayer, DeathsOther, GoodDeeds, Hangings, Shows, Dances;
        [DataMember(EmitDefaultValue = false)] public long LastChangeAt;       // game minute the index last moved
        [DataMember(EmitDefaultValue = false)] public long LastPaperAt;        // the monthly mention
        [DataMember(EmitDefaultValue = false)] public string Why;              // the last change, in a word or two
    }

    public static class TownLedgers
    {
        public const int Start = 50, Least = 10;
        public const int DeathByPlayer = -3, DeathOther = -1, Departure = -2, Arrival = 2, GoodDeed = 1, Hanging = -4, Show = 2, Dance = 1;
        public const int MonthMinutes = 30 * 1440;
        public const int FearfulKills = 2, GrievingDeaths = 3, ProsperingGain = 3;   // the last month's counts that set a mood
        public const float ThinnestStreets = 0.6f;                                // the density multiplier's floor
        public const int ThinBelow = 30;                                          // the index under which streets thin
        public static readonly string[] GoodKinds = { "peace", "mercy", "rescue", "spared", "charity" };

        /// The regulars a town keeps at this index: the setting's number scaled, never below min(10, the setting).
        public static int Cap(int index, int perTown)
        {
            int least = Math.Min(Least, perTown);
            int scaled = (int)Math.Round(perTown * (0.5 + Math.Max(0, Math.Min(100, index)) / 100.0));   // 50 keeps the setting; 0 half; 100 half again
            return Math.Max(least, Math.Min(perTown * 3 / 2, scaled));
        }

        /// The ledger of a town, read off the records: the whole of them for the index, the last month for the mood.
        public static TownLedger Read(string town, long now, IEnumerable<DeathEntry> deaths, IEnumerable<Deed> deeds, IEnumerable<Resident> residents, IEnumerable<TownEvent> events, TownLedger keep = null)
        {
            var l = keep ?? new TownLedger { Town = town };
            l.Town = town;
            int index = Start, recentKills = 0, recentDeaths = 0, recentGain = 0, recentLoss = 0;
            long month = now - MonthMinutes;
            l.Arrivals = l.Departures = l.DeathsByPlayer = l.DeathsOther = l.GoodDeeds = l.Hangings = l.Shows = l.Dances = 0;
            long lastAt = 0;
            string why = null;
            if (deaths != null)
                foreach (DeathEntry d in deaths)
                {
                    if (d == null || d.Town != town)
                        continue;
                    bool byPlayer = d.By == "player";
                    if (byPlayer) l.DeathsByPlayer++; else l.DeathsOther++;
                    index += byPlayer ? DeathByPlayer : DeathOther;
                    if (d.At >= month) { recentDeaths++; if (byPlayer) recentKills++; }
                    if (d.At > lastAt) { lastAt = d.At; why = byPlayer ? "a killing of yours" : "a death"; }
                }
            if (deeds != null)
                foreach (Deed d in deeds)
                {
                    if (d == null || d.Town != town || !GoodKinds.Contains(d.Kind))
                        continue;
                    l.GoodDeeds++;
                    index += GoodDeed;
                    if (d.At >= month) recentGain++;
                    if (d.At > lastAt) { lastAt = d.At; why = "a good turn of yours"; }
                }
            if (residents != null)
                foreach (Resident r in residents)
                {
                    if (r == null || r.Town != town)
                        continue;
                    if (r.Left)
                    {
                        l.Departures++;
                        index += Departure;
                        if (r.LeftAt >= month) recentLoss++;
                        if (r.LeftAt > lastAt) { lastAt = r.LeftAt; why = "someone took the train"; }
                    }
                    if (r.Kin != null || r.From != null)
                    {
                        l.Arrivals++;
                        index += Arrival;
                        if (r.MetAt >= month) recentGain++;
                        if (r.MetAt > lastAt) { lastAt = r.MetAt; why = "someone came on the train"; }
                    }
                }
            if (events != null)
                foreach (TownEvent e in events)
                {
                    if (e == null || e.Town != town || !e.Done)
                        continue;
                    int move = 0;
                    switch (e.Kind)
                    {
                        case "gallows": if (e.Outcome != null && e.Outcome.StartsWith("hanged", StringComparison.Ordinal)) { l.Hangings++; move = Hanging; if (e.Until >= month) recentDeaths++; } break;
                        case "show": if (!e.Broken) { l.Shows++; move = Show; if (e.Until >= month) recentGain++; } break;
                        case "dance": if (e.Attended) { l.Dances++; move = Dance; if (e.Until >= month) recentGain++; } break;
                    }
                    index += move;
                    if (move != 0 && e.Until > lastAt) { lastAt = e.Until; why = e.Kind == "gallows" ? "a hanging" : e.Kind == "show" ? "the show" : "the dance"; }
                }
            l.Index = Math.Max(0, Math.Min(100, index));
            l.Mood = MoodOf(recentKills, recentDeaths, recentGain, recentLoss);
            l.LastChangeAt = lastAt;
            l.Why = why;
            return l;
        }

        /// The last month's balance: grieving when the dead outnumber everything, fearful when the player's killings do,
        /// prospering when arrivals and good turns outrun the losses, steady otherwise.
        public static string MoodOf(int recentKills, int recentDeaths, int recentGain, int recentLoss)
        {
            if (recentKills >= FearfulKills)
                return "fearful";
            if (recentDeaths >= GrievingDeaths && recentDeaths > recentGain)
                return "grieving";
            if (recentGain - recentLoss - recentDeaths >= ProsperingGain)
                return "prospering";
            return "steady";
        }

        /// The ambient density multiplier for a thinned town (ThinStreets): 1 at the threshold, the floor at zero.
        public static float Density(int index)
        {
            if (index >= ThinBelow)
                return 1f;
            return Math.Max(ThinnestStreets, ThinnestStreets + (1f - ThinnestStreets) * index / ThinBelow);
        }

        public static readonly string[] Months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        /// The monthly Personal Mention: what the town has gained and lost since the month before.
        public static string Paper(TownLedger l, int month)
        {
            string since = month >= 1 && month <= 12 ? "since " + Months[(month + 10) % 12] : "this month past";
            int lost = l.DeathsByPlayer + l.DeathsOther + l.Hangings, gone = l.Departures, came = l.Arrivals;
            string s = l.Town + (lost > 0 ? " has lost " + Voice.Number(lost) + (lost == 1 ? " soul " : " souls ") + since : " has buried nobody " + since) + ".";
            if (gone > 0) s += " " + (gone == 1 ? "One took the train." : Capital(Voice.Number(gone)) + " took the train.");
            if (came > 0) s += " " + (came == 1 ? "One new face came by it." : Capital(Voice.Number(came)) + " new faces came by it.");
            switch (l.Mood)
            {
                case "grieving": s += " The town is in mourning and shows it."; break;
                case "fearful": s += " Doors are barred early and the street is quiet by dusk."; break;
                case "prospering": s += " Rents are up and the hotel is full."; break;
                default: s += " The town goes on much as it did."; break;
            }
            return s;
        }

        private static string Capital(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        // ---- what a town's people think of it ----

        private static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "grieving", new[] {
                "Another funeral this week. The church bell knows the way by itself now.",
                "Black on half the doors in town. I've stopped counting.",
                "The undertaker's the only trade doing well. I'd not want his sleep.",
                "A town can only bury so many before it stops talking about anything else.",
                "Saw the widow at the store. Nobody knew what to say. Nobody said it.",
                "The cemetery's grown faster than the town this year." } },
            { "fearful", new[] {
                "Bar the door at dusk now. Everybody does. Nobody says why.",
                "A gun under the counter and the shutters up by six. That's the town these days.",
                "Nobody walks the main street after dark unless they've a reason and a pistol.",
                "I keep looking at the road. Not for anybody in particular. For anybody.",
                "The saloon shuts early. The barkeep says it's the lamp oil. It isn't the lamp oil.",
                "A stranger rides in and the whole street holds its breath till he's gone." } },
            { "prospering", new[] {
                "Rent's up again. Landlord says it's the new people. The new people say it's the landlord.",
                "Not a bed to be had in town for love or money. Strangers everywhere, and all of them spending.",
                "Three new faces at the store this week. Town's filling up like a bucket under a drip.",
                "Flour's a cent dearer. Everything is. They call that prospering.",
                "Fresh paint on the bank. There's a sign a town's doing well, or wants you to think it.",
                "Somebody's building again on the east road. Hammers from first light." } },
            { "steady", new[] {
                "Same faces, same mud, same price for a drink. A man could do worse than steady.",
                "Nobody's come and nobody's gone this month. The town just sits here, like me." } },
        };

        public static string[] Pool(string mood)
        {
            string[] pool;
            return mood != null && Pools.TryGetValue(mood, out pool) ? pool : null;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] pool in Pools.Values)
                foreach (string s in pool)
                    yield return s;
            var l = new TownLedger { Town = "Valentine", Mood = "grieving", DeathsByPlayer = 4, DeathsOther = 8, Departures = 2, Arrivals = 1 };
            yield return Paper(l, 4);
            l.Mood = "fearful"; yield return Paper(l, 1);
            l.Mood = "prospering"; l.DeathsByPlayer = l.DeathsOther = 0; yield return Paper(l, 12);
            l.Mood = "steady"; l.Departures = 0; l.Arrivals = 0; yield return Paper(l, 0);
        }
    }
}
