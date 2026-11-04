// The town keeps time (SIXDAY item 5a; roadmap 17's last slice: "the town keeps time, its regulars at work by day, at the
// saloon in the evening, in church on Sunday morning and home at night"). Nobody walks across town: the game already puts
// people in the street, the saloon and the store, and a town's regulars are cast onto them out of sight (Regulars.cs). So
// the routine decides WHO a passer-by becomes, from where they stand and the hour: of the free regulars of that build, the
// one whose habits put them at that kind of spot now; and a new regular made for a spot is made to fit it (no devout
// churchgoer is born at the bar at midnight). The habits are derived from who they are (their trade, temper, devotion and
// age) and never saved, so every old save has them too. Why it reads true: a town's familiarity is the same faces in the
// same places at the same hours (place attachment: a regular's stool is theirs because they're on it at seven every
// evening), and a face out of its hour is noticed, by them most of all. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Routines
    {
        /// Under this fit a new regular is made for the spot instead, when the town has room.
        public const int Least = 30;
        /// Under this fit a regular is somewhere their routine wouldn't put them (a thought of it follows).
        public const int Odd = 20;

        /// A regular's habits, read off who they are. Never saved.
        public sealed class Profile
        {
            public string Group;         // InnerLife.TradeGroup
            public bool Woman, Old;      // past sixty
            public bool Churchgoer;      // Devout or Churchgoer
            public bool Devout;
            public bool Drinker;         // the saloon is theirs of an evening
            public bool NightOwl;        // out after eleven
            public bool EarlyRiser;      // up at dawn
            public bool Indoors;         // works under a roof (the store, the counter, the parlour)
            public bool Shopper;         // the store by day
        }

        public static Profile ProfileOf(Resident r, int nowYear)
        {
            string g = InnerLife.TradeGroup(r.Trade);
            string t = r.Personality ?? "";
            var p = new Profile { Group = g, Woman = r.Woman };
            p.Old = g == "old" || (r.BornYear > 0 && nowYear - r.BornYear >= 60);
            p.Devout = r.Devotion == "Devout";
            p.Churchgoer = p.Devout || r.Devotion == "Churchgoer";
            bool godless = r.Devotion == "Godless" || r.Devotion == "Lapsed";
            p.Drinker = !p.Devout && (g == "rough" || g == "card" || g == "working" || g == "girl" || g == "trapper" || t == "Drifter" || t == "Hothead" || (godless && (t == "Greedy" || r.Sociability == "Outgoing")));
            p.NightOwl = !p.Devout && (g == "card" || g == "girl" || g == "rough" || t == "Drifter" || (p.Drinker && r.Sociability == "Outgoing" && !p.Old));   // the devout keep decent hours
            p.EarlyRiser = g == "farm" || g == "working" || g == "old" || g == "soldier" || g == "law" || g == "trapper" || p.Old;
            p.Indoors = g == "gentry" || g == "town" || g == "card" || g == "girl";
            p.Shopper = r.Woman || g == "gentry" || g == "town";
            return p;
        }

        /// The kind of spot a passer-by stands at, from the interior kind the game side reads (null: out of doors): saloon,
        /// church, hotel (the bath house too), station, store (every other counter), street.
        public static string SpotOf(string interiorKind)
        {
            if (interiorKind == null)
                return "street";
            switch (interiorKind)
            {
                case "saloon": case "church": case "hotel": case "station": return interiorKind;
                case "bath": return "hotel";
                default: return "store";
            }
        }

        public static readonly string[] Spots = { "saloon", "church", "store", "hotel", "station", "street" };

        /// dawn 4-7, morning 7-11, midday 11-14, afternoon 14-17, evening 17-22, night 22-4.
        public static string Daypart(int hour)
        {
            hour = ((hour % 24) + 24) % 24;
            return hour < 4 ? "night" : hour < 7 ? "dawn" : hour < 11 ? "morning" : hour < 14 ? "midday" : hour < 17 ? "afternoon" : hour < 22 ? "evening" : "night";
        }

        /// How well this regular belongs at this spot at this hour and weekday (0 Sunday), 0-100. The shape of a day: the
        /// early up at dawn, work and errands by day, the saloon of an evening (the drinkers' most, nearly anyone's a
        /// little), church on Sunday morning for those who go, home at night but for the owls.
        public static int Fit(Profile p, string spot, int hour, int weekday)
        {
            string d = Daypart(hour);
            bool sunday = weekday == 0;
            bool sundayMorning = sunday && hour >= 8 && hour < 12;
            int f;
            switch (spot)
            {
                case "saloon":
                    if (d == "evening") f = p.Drinker ? 90 : p.NightOwl ? 80 : p.Devout ? 8 : p.Churchgoer && sunday ? 25 : 45;
                    else if (d == "night") f = p.NightOwl ? 85 : p.Drinker ? 70 : p.Devout ? 3 : 10;
                    else if (d == "midday" || d == "afternoon") f = p.Group == "card" ? 70 : p.Drinker ? 55 : p.Devout ? 5 : 22;
                    else f = p.Drinker && !p.Churchgoer ? 28 : 5;   // the morning: the early drinkers
                    if (sundayMorning) f = Math.Min(f, p.Churchgoer ? 3 : 25);
                    break;
                case "church":
                    if (sundayMorning) f = p.Churchgoer ? 95 : p.Old ? 65 : 15;
                    else if (p.Devout && (d == "morning" || d == "evening")) f = 50;
                    else f = p.Churchgoer ? 12 : 4;
                    break;
                case "store":
                    if (sunday) f = p.Indoors ? 12 : 8;   // shut
                    else if (d == "morning" || d == "midday" || d == "afternoon") f = p.Shopper ? 72 : p.Indoors ? 65 : 40;   // under the street's 80 for those who work out of doors
                    else if (d == "evening") f = 22;
                    else f = 4;
                    break;
                case "hotel":
                    f = p.Group == "gentry" ? 70 : p.Group == "town" ? 50 : 30;
                    if (d == "night") f = Math.Max(f, 38);
                    break;
                case "station":
                    f = (d == "night") ? 15 : p.Group == "gentry" || p.Group == "town" ? 55 : 40;
                    break;
                default:   // the street
                    if (d == "dawn") f = p.EarlyRiser ? 75 : p.NightOwl ? 10 : 25;
                    else if (d == "morning" || d == "midday" || d == "afternoon") f = sundayMorning && p.Churchgoer ? 15 : !p.Indoors ? 80 : p.Shopper ? 60 : 50;
                    else if (d == "evening") f = 50;
                    else f = p.NightOwl ? 60 : p.Drinker ? 45 : 8;
                    break;
            }
            if (p.EarlyRiser && d == "night" && spot != "street") f -= 25;
            if (p.NightOwl && d == "dawn") f -= 20;
            if (p.Old && d == "night") f -= 15;
            return Math.Max(0, Math.Min(100, f));
        }

        /// Where their routine would have them now, in words, for the F7 line and the log: "the saloon", "church",
        /// "the store", "the street", or "home abed" when nothing fits (night, no owl).
        public static string Expected(Profile p, int hour, int weekday)
        {
            string best = null;
            int bestFit = 0;
            foreach (string s in Spots)
            {
                if (s == "hotel" || s == "station")
                    continue;   // nobody's routine is a hotel
                int f = Fit(p, s, hour, weekday);
                if (f > bestFit)
                {
                    bestFit = f;
                    best = s;
                }
            }
            if (best == null || bestFit < Odd)
                return "home abed";
            return best == "church" ? "church" : "the " + best;
        }

        /// A new regular made for a spot is made to fit it: their devotion and sociability nudged (never the temper or the
        /// trade, which the body gave them) until the fit clears Least, where it can.
        public static void FitNew(Resident r, string spot, int hour, int weekday, string seed, int nowYear)
        {
            if (Fit(ProfileOf(r, nowYear), spot, hour, weekday) >= Least)
                return;
            string d = Daypart(hour);
            if (spot == "saloon")
            {
                if (r.Devotion == "Devout" || r.Devotion == "Churchgoer")
                    r.Devotion = Voice.Hash(seed + "dv") % 3 == 0 ? "Godless" : "Lapsed";
                r.Sociability = "Outgoing";
            }
            else if (spot == "church")
            {
                if (r.Devotion != "Devout")
                    r.Devotion = Voice.Hash(seed + "dv") % 2 == 0 ? "Devout" : "Churchgoer";
            }
            else if (spot == "street" && d == "night")
            {
                if (r.Devotion == "Devout")
                    r.Devotion = "Lapsed";
                r.Sociability = "Outgoing";
            }
        }

        /// The pool a regular's thought comes from here, now: the spot and the part of the day it is for them.
        public static string Key(string spot, int hour, int weekday)
        {
            string d = Daypart(hour);
            switch (spot)
            {
                case "saloon": return d == "evening" ? "saloon|evening" : d == "night" ? "saloon|night" : "saloon|day";
                case "church": return weekday == 0 ? "church|sunday" : "church|weekday";
                case "store": return d == "morning" || d == "dawn" ? "store|morning" : "store|afternoon";
                case "hotel": return "hotel";
                case "station": return "station";
                default: return d == "dawn" ? "street|dawn" : d == "morning" ? "street|morning" : d == "midday" || d == "afternoon" ? "street|day" : d == "evening" ? "street|evening" : "street|night";
            }
        }

        /// The lines for a key (Pools), or the odd ones when the regular is somewhere their routine wouldn't put them.
        public static string[] Pool(string key, bool odd)
        {
            string[] lines;
            if (odd)
                return OddLines;
            return key != null && Pools.TryGetValue(key, out lines) ? lines : null;
        }

        public static readonly string[] OddLines =
        {
            "Never come in here on a Tuesday. Today I did.", "This isn't my hour for this place. Don't know what brought me.",
            "Out of my habit. Feels like a borrowed coat.", "Wrong place, wrong hour. The day's turned strange on me.",
            "I know this room by evening. By day it's a stranger.", "Broke my own routine. Something's at me and I can't name it.",
            "My feet brought me here. They didn't ask.", "Everyone looks at me like I've come in the wrong door. Maybe I have.",
        };

        public static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "saloon|evening", new[] {
                "Seven o'clock. Saloon. Same stool.", "Day's done. The bar knows my elbow by now.",
                "One for the dust, one for the road, one because it's evening.", "Same stool, same crack in the glass. Home enough.",
                "Evening's the one hour a man owns outright.", "The piano's started. That's my bell.",
                "Supper can wait. The first glass can't.", "The same faces along the bar, in the same order. Good." } },
            { "saloon|night", new[] {
                "Past midnight and nothing to go home to but a cold stove.", "Last one standing at the bar again. Pour it.",
                "Night's the only hour the talk here is honest.", "The lamp's low and so am I. One more.",
                "Owls and drinkers keep the same hours.", "Nobody waiting up. Nobody to lie to about the time.",
                "The barkeep's yawning. I'll outlast him.", "Late. The good drinkers have gone and left me the bad ones." } },
            { "saloon|day", new[] {
                "Noon and I'm at the bar. Don't tell my mother.", "Drinking in daylight. The sun comes in and judges.",
                "Early for whiskey. Late for everything else.", "The bar's quiet by day. You can hear yourself think, which is the trouble.",
                "A midday glass to get through the afternoon.", "Nobody in here but the drinkers who mean it.",
                "Daylight through the window shows the dust on the bottles.", "Morning, and the bar's open. Last night never quite ended for me." } },
            { "church|sunday", new[] {
                "Sunday. The one bench I'm sure of.", "Third pew, left side, same as my father.",
                "Sunday best is Tuesday worst, but it's clean.", "The bell said nine and here I am. Habit or faith, it's the same walk.",
                "The whole town in one room, pretending for an hour.", "Collection plate's coming. Here's my dime and my doubts.",
                "A week of sin and an hour of sorry. Fair trade, the preacher says.", "The hymn's the same as last Sunday. So are my sins." } },
            { "church|weekday", new[] {
                "Nobody here but me and the dust in the light. That's when He listens.", "A weekday prayer. Quieter than a Sunday one.",
                "Came in off the street for the cool and stayed for the quiet.", "The church on a weekday. Must be something on my mind.",
                "Empty pews and I can hear my own breathing.", "Lit nothing, said nothing. Sat.",
                "The door was open. Took it as an invitation.", "Midweek, and the Lord's keeping shop for one." } },
            { "store|morning", new[] {
                "Flour, nails, a letter to send. The morning list.", "The store's open and so's my purse, a little.",
                "First through the door. The clerk's still sweeping.", "Morning errands. Then the real day.",
                "Counting coins at the counter. Every week the same sum.", "Shelves full, pocket not. The usual.",
                "A pound of coffee and the week's talk, both over the counter.", "The bell over the door rang for me first today." } },
            { "store|afternoon", new[] {
                "The afternoon run for what I forgot this morning.", "The store's busiest when I've least time.",
                "Afternoon. The clerk knows what I want before I say it.", "Came in for one thing. Leaving with three.",
                "A look at the catalogue. A dream for a dollar.", "Flour's gone up again. The shelf doesn't care.",
                "Credit till Friday. He wrote it down without a word.", "Late in the day and the good cuts are gone." } },
            { "hotel", new[] {
                "A roof that isn't mine. Costs a dollar a night.", "The hotel's clean and nobody knows my name. Both suit me.",
                "Travelling's half waiting in lobbies.", "A bed off the ground. Worth the dollar.",
                "Strangers in every chair. I'm one of them here.", "The clerk looked at my boots before my face.",
                "Clean sheets. Can't remember the last time.", "A hotel's a town of its own, and everyone's leaving." } },
            { "station", new[] {
                "The train knows the hour better than I do.", "Waiting on a train is the honest kind of waiting.",
                "Tickets, trunks and goodbyes. The station's trade.", "Every train takes somebody's son.",
                "The timetable's the only promise this town keeps.", "Smoke on the line. Somebody's coming or going.",
                "The bench is hard and the train is late. Both on purpose, I think.", "Everyone here is halfway somewhere else." } },
            { "street|dawn", new[] {
                "Up before the rooster. He's getting lazy.", "First light. The street's mine for an hour.",
                "Dawn, and the dust not yet kicked up.", "The early man gets the quiet, not the worm.",
                "Boots on before the birds.", "Cold at this hour. Worth it for the quiet.",
                "The sun's late. I'm not.", "Dawn's the one hour nobody wants anything of me." } },
            { "street|morning", new[] {
                "Morning. The street's filling. Everybody with somewhere to be.", "Work's waiting. It always is.",
                "The morning walk, the same hundred steps.", "A nod here, a nod there. Morning's made of nods.",
                "Shops opening one by one, like eyes.", "Monday's walk is longer than Saturday's. Same street.",
                "Morning. Same mud, different boots.", "Early enough that the street still smells of bread." } },
            { "street|day", new[] {
                "Noon sun. Even the dogs are under the porches.", "Afternoon. The long part of the day.",
                "The street's slow at this hour. So am I.", "Half the day done and half the work.",
                "Dust and heat and the hours dragging their feet.", "Afternoon errands. Then the evening, which is mine.",
                "Midday. The shadows have gone under things to hide.", "The afternoon's all walking and no arriving." } },
            { "street|evening", new[] {
                "Evening. Lamps going up. The day lets go.", "Homeward, or saloonward. The road forks here.",
                "The street's prettier at dusk. Hides the mud.", "Evening air. The one thing in this town that's free.",
                "Day's work done. Walking it off.", "Dusk, and every window a different supper.",
                "Evening. The street empties one way and the saloon fills the other.", "The light goes gold and the town forgives itself." } },
            { "street|night", new[] {
                "Out past a decent hour. The night's for the likes of me.", "Night street. Only owls and fools about.",
                "The lamps and me. Everyone sensible's abed.", "Midnight and my shadow's the only company.",
                "The town sleeps. I never learned how.", "Nobody about but the kind who shouldn't be.",
                "A night walk clears nothing. I take it anyway.", "The dark's quieter than my own room." } },
        };

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (string s in OddLines)
                yield return s;
            foreach (KeyValuePair<string, string[]> kv in Pools)
                foreach (string s in kv.Value)
                    yield return s;
        }
    }
}
