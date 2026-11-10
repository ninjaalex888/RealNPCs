// Seeing what happened (SIXDAY item 7; roadmap 24; roadmap.md P4): an "Overheard" page in the reader of what went on near
// you in the last game day, one plain sentence each, newest first ("Simon Walsh and Ezra Whitlow came to blows in the
// saloon, two hours ago."), read from the world log alone (the happenings the player stood witness to, within Metres; nothing
// new is saved); and a line on the ticker when something starts up at your back within Metres ("Raised voices behind you."),
// at most once a minute. Why: a town's life goes on at your back, and the reader is the player's memory of what he half
// saw; the cue is the sound a street makes before you turn round. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Text;

namespace EnrageWestworld.Core
{
    public static class Overheard
    {
        public const float Metres = 22f;        // what counts as near enough to have overheard
        public const int Most = 40;             // lines on the page
        public const long DayMinutes = 1440;    // the last game day
        public const int CueGapMs = 60000;      // a cue at most once a minute

        /// Whether the player stood witness to this, near enough.
        public static bool Overhead(Happening e)
        {
            if (e == null || e.Witnesses == null)
                return false;
            foreach (EventWitness w in e.Witnesses)
                if (w != null && w.Id == "player" && w.Metres <= Metres)
                    return true;
            return false;
        }

        /// The happenings worth a line: scenes between others, and deaths; never the player's own doing.
        public static bool Counts(Happening e)
        {
            return e != null && e.Kind != null && e.DoerId != "player" && e.TargetId != "player" && (e.Kind.StartsWith("scene:", StringComparison.Ordinal) || e.Kind == "death");
        }

        /// The lines of the last game day near the player, newest first, at most Most.
        public static List<string> Lines(List<Happening> log, long now)
        {
            var lines = new List<string>();
            if (log == null)
                return lines;
            for (int i = log.Count - 1; i >= 0 && lines.Count < Most; i--)
            {
                Happening e = log[i];
                if (e == null || e.At > now || now - e.At > DayMinutes || !Counts(e) || !Overhead(e))
                    continue;
                string line = Line(e, now);
                if (line != null)
                    lines.Add(line);
            }
            return lines;
        }

        /// The page's text, or null when nothing was overheard.
        public static string Body(List<Happening> log, long now)
        {
            List<string> lines = Lines(log, now);
            if (lines.Count == 0)
                return null;
            var b = new StringBuilder();
            b.Append("What went on near you, this last day:\n");
            foreach (string l in lines)
                b.Append('\n').Append(l);
            return b.ToString();
        }

        /// One plain sentence for a happening: who, what, where and how long ago.
        public static string Line(Happening e, long now)
        {
            if (e == null)
                return null;
            string a = e.DoerName ?? "Someone", t = e.TargetName ?? "someone";
            string what = What(e.Kind, a, t);
            if (what == null)
                return null;
            return what + Where(e) + ", " + Ago(now - e.At) + ".";
        }

        /// The deed in words, by the scene's kind.
        public static string What(string kind, string a, string t)
        {
            if (kind == null)
                return null;
            if (kind == "death")
                return t + " was killed" + (a != "Someone" ? ", by " + a : "");
            string k = kind.StartsWith("scene:", StringComparison.Ordinal) ? kind.Substring(6) : kind;
            switch (k)
            {
                case "brawl": return a + " and " + t + " came to blows";
                case "argue": case "quarrel": return a + " and " + t + " had words";
                case "snap": return a + " snapped at " + t;
                case "standoff": return a + " and " + t + " pulled guns on each other";
                case "callout": return a + " called " + t + " out";
                case "debt": return a + " and " + t + " quarrelled over money";
                case "settled": return a + " paid " + t + " what was owed";
                case "fright": return a + " and " + t + " stood together after the killing, white as sheets";
                case "bout": return a + " and " + t + " fought out back for money, and shook on it";
                case "cheat": return a + " and " + t + " fell out over cards";
                case "welsh": return a + " wouldn't hand " + t + " the winnings from the fistfight";
                case "wagerowed": return a + " still owes " + t + " the winnings from the fistfight";
                case "wagerpaid": return a + " handed " + t + " the winnings from the fistfight";
                case "feud": return a + " and " + t + " had their feud out";
                case "flirt": case "sweet": case "courting": return a + " and " + t + " were sweet on each other";
                case "propose": return a + " asked " + t + " to marry";
                case "brushoff": return t + " brushed " + a + " off";
                case "smalltalk": case "mindtalk": case "haggle": case "news": case "paper": case "posse": return a + " and " + t + " passed the time of day";
                case "gossip": return a + " and " + t + " traded the town's talk";
                case "song": return a + " sang for " + t;
                case "talltale": return a + " told " + t + " a tall one";
                case "nighttale": return a + " told " + t + " one of the old tales";
                case "pitch": return a + " tried to sell " + t + " something";
                case "hosts": return a + " and " + t + " spoke strangely together";
                case "pickpocket": case "lift": return a + " picked " + t + "'s pocket";
                case "mugging": return a + " robbed " + t;
                case "holdup": return a + " held " + t + " up at gunpoint";
                case "rough": return a + " put " + t + "'s face into the bar";
                case "drunk": return a + ", in drink, called " + t + " out";
                case "help": case "carried": return a + " helped " + t + " up";
                case "passby": return a + " walked past " + t + " lying in the street";
                case "comfort": case "condole": return a + " comforted " + t;
                case "cheer": case "glad": return a + " cheered " + t + " up";
                case "envy": return a + " envied " + t + " out loud";
                case "makeup": return a + " and " + t + " made up";
                case "round": return a + " stood " + t + " a drink";
                case "charity": return a + " gave " + t + " a coin";
                case "scorn": return a + " sent " + t + " off with a flea in the ear";
                case "defend": return a + " stood up for " + t;
                case "faced": return a + " faced " + t + " down";
                case "apology": return a + " said sorry to " + t;
                case "thanks": return a + " thanked " + t;
                case "mourn": case "widow": return a + " mourned with " + t;
                case "lawword": return "the law had a word with " + a;
                case "avoid": return a + " kept clear of " + t;
                case "introduced": return a + " and " + t + " were made acquainted";
                default: return a + " and " + t + " had some business (" + k + ")";
            }
        }

        private static string Where(Happening e)
        {
            if (e.Place != null)
                return " in the " + e.Place;
            if (e.Town != null)
                return " in " + e.Town;
            return " on the road";
        }

        /// "just now", "an hour ago", "five hours ago", "a day ago".
        public static string Ago(long minutes)
        {
            if (minutes < 30)
                return "just now";
            if (minutes < 90)
                return "an hour ago";
            long hours = (minutes + 30) / 60;
            if (hours < 24)
                return Word(hours) + " hours ago";
            return "a day ago";
        }

        private static string Word(long n)
        {
            switch (n)
            {
                case 2: return "two"; case 3: return "three"; case 4: return "four"; case 5: return "five"; case 6: return "six"; case 7: return "seven";
                case 8: return "eight"; case 9: return "nine"; case 10: return "ten"; case 11: return "eleven"; case 12: return "twelve";
                default: return n.ToString();
            }
        }

        /// The cue on the ticker when a scene starts behind the player, by its kind; null for the silent kinds (a pocket
        /// picked makes no sound).
        public static string Cue(string kind)
        {
            switch (kind)
            {
                case "argue": case "quarrel": case "snap": case "debt": case "feud": case "cheat": case "standoff": case "callout": case "drunk": case "brushoff": case "scorn": case "envy":
                    return "Raised voices behind you.";
                case "brawl": case "rough": case "mugging":
                    return "A scuffle behind you.";
                case "holdup":
                    return "A cry of 'Thief!' behind you.";
                case "song": case "talltale": case "cheer": case "glad": case "gossip": case "hosts":
                    return "Laughter behind you.";
                case "nighttale":
                    return "A low voice telling something, behind you.";
                case "flirt": case "sweet": case "propose": case "courting":
                    return "Soft words behind you.";
                case "mourn": case "widow": case "comfort": case "condole":
                    return "Someone weeping behind you.";
                case "smalltalk": case "haggle": case "paper": case "pitch": case "news": case "posse": case "round": case "lawword": case "settled": case "help": case "carried": case "makeup": case "apology": case "thanks": case "charity": case "defend": case "faced": case "mindtalk":
                    return "Talk behind you.";
                default:
                    return null;
            }
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            string[] kinds = { "brawl", "argue", "snap", "standoff", "callout", "debt", "settled", "cheat", "feud", "flirt", "propose", "brushoff", "smalltalk", "gossip", "song", "talltale", "pitch", "hosts", "pickpocket", "mugging", "holdup", "rough", "drunk", "help", "passby", "comfort", "cheer", "envy", "makeup", "round", "charity", "scorn", "defend", "faced", "apology", "thanks", "mourn", "lawword", "avoid", "introduced", "death" };
            foreach (string k in kinds)
            {
                yield return What(k == "death" ? k : "scene:" + k, "Simon Walsh", "Ezra Whitlow");
                string c = Cue(k);
                if (c != null) yield return c;
            }
        }
    }
}
