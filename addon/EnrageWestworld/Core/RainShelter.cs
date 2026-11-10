// Rain empties the street and fills the saloon (FOURTEENHOUR item 26; [Scenes] RainShelter; the roadmap's design 5, "Weather-aware
// worries", and "Rain ends the drought", neither built till now: nothing kept a record of the rain). The town now keeps one: each game
// minute you're in a town, a minute of its sky, and a minute of rain when it's raining (Store.Counts "sky|town|day", "rain|town|day").
// Out of it:
// - in the rain the street's errands wait: the horse trade, the tooth, the way asked, the snub, the temperance door, a stroll (no one
//   is walked anywhere: the rain only takes the street's reasons away, so nobody is gathered at a door, Lex's rule);
// - under one roof while it rains (and an hour after), two strangers talk about it and leave acquainted, a tie made, so they nod
//   in passing after (the acknowledgement in passing, seen);
// - two days of rain or more in a farmer's town while his drought worry runs end it well, and its "rain" lines come only when the rain
//   was seen; with the sky watched and no rain at all, its good end is the well dug deeper, never "Rain!" (InnerLife.RainDaysIn).
// Why it reads true: a shared small hardship in a small space is the quickest way strangers become acquaintances (propinquity:
// Festinger, Schachter and Back, 1950), and rain is the one hardship everyone in a town shares the same afternoon. Plain C#, no game;
// all words this add-on's.
namespace EnrageWestworld.Core
{
    public static class RainShelter
    {
        public static bool On = true;   // [Scenes] RainShelter, from the settings
        public const int Percent = 45, Strength = 42, RainDayMinutes = 20, AfterRainMinutes = 60;

        public static string SkyKey(string town, long day) { return "sky|" + town + "|" + day; }
        public static string RainKey(string town, long day) { return "rain|" + town + "|" + day; }

        /// Two strangers under one roof while it rains (or an hour after): a reason, or null.
        public static Reason For(Someone x, Someone y, long gameDay, bool rain, bool indoors)
        {
            if (!On || !rain || !indoors || x == null || y == null || x.Id == y.Id)
                return null;
            string key = string.CompareOrdinal(x.Id, y.Id) < 0 ? x.Id + "|" + y.Id : y.Id + "|" + x.Id;
            if (!Voice.Chance(key + gameDay + "|shelter", Percent))
                return null;
            return new Reason { Kind = "shelter", Strength = Strength, Why = "the rain's kept " + x.Name + " and " + y.Name + " under one roof" };
        }

        /// The talk: the weather, then names. Kind "introduced": a tie made, the two acquainted from now.
        public static Scene Play(SceneCast c, string seed)
        {
            // their own names (a stranger's N is "mister"): each tells the other his
            string a = c.Names != null && c.Names[0] != null ? Person.FirstOf(c.Names[0]) : Scenes.N(c, 0, seed);
            string b = c.Names != null && c.Names[1] != null ? Person.FirstOf(c.Names[1]) : Scenes.N(c, 1, seed);
            Scene s = new Scene { Kind = "introduced", Summary = "two strangers sheltering from the rain got acquainted" };
            s.Add(0, Voice.Pick(seed + "|w0", "Some weather.", "Coming down like the Flood out there.", "Wet as a well, and no end to it."), "mild", "face")
             .Add(1, Voice.Pick(seed + "|w1", "Won't let up till dark, I'd say.", "Good for the crops, they'll tell you. Not for my boots.", "My horse is standing in it, poor brute."), "mild")
             .Add(0, Voice.Pick(seed + "|n0", "I'm " + a + ", by the way. Don't believe we've met.", "Name's " + a + ". Might as well, since we're stuck.", a + ". Pleased to share a roof with you."), "mild")
             .Add(1, Voice.Pick(seed + "|n1", b + ". Likewise.", "Call me " + b + ".", b + ". Here's hoping it clears."), "mild");
            return s;
        }
    }
}
