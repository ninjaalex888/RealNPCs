// Known by the horse (LIVES item 6a, 2026-11-12; [World] HorseTalk, settings version 482; the roadmap's "Known by horse and
// hat" row, the horse's half). People who have met you already know your horse before your face (Core/PlaceMemory.cs). But a
// town that only heard of what you did never learned how you looked doing it: its tale said "a stranger", "a big fella in a
// worn hat". Now a deed of yours remembers the horse you rode for it (or rode in on, within the hour), and the town's tale,
// while it still keeps fairly to the truth, says it ("a stranger on a grey Arabian shot two men outside the saloon"). Where
// that tale is told, someone near who sees you ride up on that same horse puts the two together ("Grey Arabian. That's the
// horse they talk of, from Valentine."); ride in on another horse and the tale doesn't find you: the trail goes cold.
//
// Why it reads true: before photographs were common, a horse was a man's most identifiable possession (horse descriptions
// filled the wanted bills and the stock thieves' trials), and eyewitnesses remember the vivid, unusual detail far better than a
// face (weapon focus, and its kin, the distinctive-feature effect). So changing horses was, then as now, the outlaw's cheapest
// disguise. The hat is left out: the game's outfit can't be read reliably here, and a guessed hat would lie.
//
// What it keeps: one short phrase on a deed (Deed.Horse, saved like the rest of the deed, gone when the deed is trimmed).
// The horse's words come from its model's name (breed and coat). Off in Core.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class HorseTalk
    {
        public static bool On;   // [World] HorseTalk, set by the game
        public const int TellsAtFidelity = 50, Pull = 32;
        public const long RideMinutes = 60;   // a deed done on foot remembers the horse you rode in on this long ago

        private static readonly Dictionary<string, string> Breeds = new Dictionary<string, string>
        {
            { "arabian", "Arabian" }, { "americanstandardbred", "Standardbred" }, { "morgan", "Morgan" }, { "shire", "Shire" },
            { "tennesseewalker", "Tennessee Walker" }, { "thoroughbred", "Thoroughbred" }, { "mustang", "mustang" }, { "appaloosa", "Appaloosa" },
            { "dutchwarmblood", "Dutch Warmblood" }, { "hungarianhalfbred", "Hungarian Half-bred" }, { "kentuckysaddle", "Kentucky Saddler" },
            { "nokota", "Nokota" }, { "turkoman", "Turkoman" }, { "missourifoxtrotter", "Missouri Fox Trotter" }, { "americanpaint", "paint" },
            { "andalusian", "Andalusian" }, { "belgian", "Belgian" }, { "ardennes", "Ardennes" }, { "suffolkpunch", "Suffolk Punch" },
            { "criollo", "Criollo" }, { "kladruber", "Kladruber" }, { "breton", "Breton" }, { "gypsycob", "cob" }, { "norfolkroadster", "roadster" },
            { "mp_mangy_backup", "nag" }, { "murfreebrood", "nag" }, { "buell", "horse" }, { "john_endlesssummer", "horse" },
        };

        private static readonly string[][] Coats =
        {
            new[] { "white", "white" }, new[] { "black", "black" }, new[] { "chestnut", "chestnut" }, new[] { "bay", "bay" },
            new[] { "dapplegrey", "dapple grey" }, new[] { "grey", "grey" }, new[] { "gray", "grey" }, new[] { "palomino", "palomino" },
            new[] { "buckskin", "buckskin" }, new[] { "dun", "dun" }, new[] { "roan", "roan" }, new[] { "sorrel", "sorrel" },
            new[] { "pinto", "pinto" }, new[] { "brown", "brown" }, new[] { "silver", "silver" }, new[] { "cream", "cream" },
            new[] { "champagne", "pale gold" }, new[] { "leopard", "spotted" }, new[] { "blanket", "spotted" }, new[] { "overo", "spotted" },
            new[] { "tobiano", "spotted" }, new[] { "splashed", "splashed" }, new[] { "sooty", "dark" }, new[] { "liver", "liver chestnut" },
        };

        /// "a grey Arabian" from "a_c_horse_arabian_grey", or null for a model that isn't a horse or can't be read.
        public static string Words(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            string m = model.ToLowerInvariant();
            if (!m.StartsWith("a_c_horse_"))
                return null;
            string rest = m.Substring(10);
            string breed = null, key = null;
            foreach (var kv in Breeds)
                if (rest.StartsWith(kv.Key) && (key == null || kv.Key.Length > key.Length))
                {
                    key = kv.Key;
                    breed = kv.Value;
                }
            string coat = null;
            string tail = key != null ? rest.Substring(key.Length) : rest;
            foreach (string[] c in Coats)
                if (tail.Contains(c[0]))
                {
                    coat = c[1];
                    break;
                }
            if (breed == null && coat == null)
                return null;
            string words = (coat != null ? coat + " " : "") + (breed ?? "horse");
            if (breed != null && breed != "horse" && breed != "nag" && breed != "cob" && breed != "roadster" && breed != "paint" && breed != "mustang" && coat == null)
                words = breed + " horse";
            return (StartsVowel(words) ? "an " : "a ") + words;
        }

        private static bool StartsVowel(string s) { return s.Length > 0 && "aeiouAEIOU".IndexOf(s[0]) >= 0; }

        /// The tale's who, with the horse while the tale keeps fairly to the truth ("a stranger on a grey Arabian").
        public static string With(string who, string horse, int fidelity)
        {
            if (!On || string.IsNullOrEmpty(horse) || string.IsNullOrEmpty(who) || fidelity < TellsAtFidelity || who.Contains(" horse"))
                return who;
            return who + " on " + horse;
        }

        /// Someone who sees you ride up on the horse their town's tale tells of. town: where it happened.
        public static string Seen(string horse, string town, string seed)
        {
            string h = horse != null && horse.StartsWith("a ") ? horse.Substring(2) : horse != null && horse.StartsWith("an ") ? horse.Substring(3) : horse ?? "horse";
            return Voice.Pick(seed + "|hs", Lines).Replace("{h}", Grammar.UpperFirst(h)).Replace("{hl}", h).Replace("{horse}", horse ?? "a horse").Replace("{t}", town ?? "up the road");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Lines) yield return l.Replace("{h}", "Grey Arabian").Replace("{hl}", "grey Arabian").Replace("{horse}", "a grey Arabian").Replace("{t}", "Valentine");
        }

        private static readonly string[] Lines = {
            "{h}. That's the horse they talk of, from {t}.",
            "That {hl}. Just as they told it, the business in {t}.",
            "They said the man rode {horse}. Well, here's {horse}, and a man on it.",
            "Don't look at him. That's the {hl} from what happened in {t}." };
    }
}
