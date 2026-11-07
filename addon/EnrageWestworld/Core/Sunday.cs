// Sunday, properly (FIVEDAY item 25). The Sabbath had the devout's kneel in the churchyard (Sabbath.cs), the dry Sunday,
// the street preacher (Scenes.Preacher) and a church choice from nine to noon (Choosing). Now the whole morning:
// - from eight to ten, the town's believers make for the church, the devout most (a lean on the choice);
// - from half past nine to half past eleven, near the church, you hear the hymn from the street (an old hymn by name);
// - the sermon's subject comes from the town's week: a killing known here (the Sixth Commandment), a theft (the Eighth),
//   a death among them (the resurrection), someone gone to drink (temperance), else the season's;
// - after the service the churchgoers think it over, and from noon to two there's dinner on the grounds, with neighbours
//   lingering to talk and eat.
// Sunday clothes are left out (outfits are held back). Behind [World] SundayMorning.
//
// Why it reads true: in an 1899 town the Sunday service was the week's one gathering of everybody. The preacher took his
// text from what the town had lived through, the talk after was of the sermon and its targets, and a dinner on the grounds
// followed the service in the summer months. The hymns named are all older than 1899 and in the public domain; only
// their titles are used. Plain C#, no game; all other words original. mindtest SundayTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Sunday
    {
        public const int ToChurchFrom = 8, ToChurchTo = 10;      // the walk to the nine o'clock service, and the late ones
        public const int HymnFromMin = 9 * 60 + 30, HymnToMin = 11 * 60 + 30;
        public const int DinnerFrom = 12, DinnerTo = 14;

        /// How Sunday leans a choice (Choosing's act ids): to church before the service, by devotion; to company and food after.
        public static float Lean(int weekday, int hour, string devotion, string act)
        {
            if (weekday != 0)
                return 0f;
            if (hour >= ToChurchFrom && hour < ToChurchTo && act == "church")
                return devotion == "Devout" ? 0.35f : devotion == "Churchgoer" ? 0.22f : devotion == "Lapsed" ? 0.04f : devotion == "Godless" ? 0f : 0.1f;
            if (hour >= DinnerFrom && hour < DinnerTo && devotion != "Godless")
                return act == "eat" ? 0.12f : act == "greet" ? 0.1f : act == "talk" ? 0.06f : act == "work" ? -0.1f : 0f;
            return 0f;
        }

        private static readonly string[] Hymns =
        {
            "Shall We Gather at the River", "Rock of Ages", "Amazing Grace", "What a Friend We Have in Jesus", "Nearer, My God, to Thee",
            "Abide with Me", "Blessed Assurance", "Leaning on the Everlasting Arms", "Holy, Holy, Holy", "How Firm a Foundation",
        };

        /// A hymn from the church as you pass in the street.
        public static string HymnLine(string seed)
        {
            string h = Hymns[(Voice.Hash(seed + "|hymn") & 0x7fffffff) % Hymns.Length];
            return Voice.Pick(seed + "|hl", "From the church, the whole town singing \"" + h + "\".", "\"" + h + "\" through the church doors, the organ a beat behind.",
                              "The congregation is on \"" + h + "\". A dog on the steps howls along.", "Voices from the church: \"" + h + "\", slow and loud.");
        }

        /// The sermon's subject from the town's week: "kill", "steal", "death", "drink", or "season".
        public static string Subject(bool killing, bool theft, bool death, bool drink)
        {
            return killing ? "kill" : theft ? "steal" : death ? "death" : drink ? "drink" : "season";
        }

        private static readonly Dictionary<string, string[]> After = new Dictionary<string, string[]>
        {
            { "kill", new[] { "The reverend preached on the Sixth Commandment. Everybody knew who he meant.", "Thou shalt not kill, he said, and looked at the back pews a long while.", "A sermon on Cain today. The whole church thought of the same man." } },
            { "steal", new[] { "Thou shalt not steal. Reverend near shouted it. Somebody in town has a guilty conscience.", "The sermon was on thieves. I kept my hands in my lap.", "He preached on the Eighth Commandment. Folks looked at their neighbours." } },
            { "death", new[] { "He preached on the resurrection. For the family, I think. They wept.", "In my Father's house are many mansions. He read it for the one we buried.", "A sermon on death and what's after. Hard to sit through, this week." } },
            { "drink", new[] { "Wine is a mocker, the reverend said. Half the pews went red.", "A sermon on the bottle. Nobody looked at anybody.", "He preached against the drink. The saloon will be full by supper anyway." } },
            { "season", new[] { "A good sermon on the harvest. Short, too. Bless him.", "The reverend went long. My stomach spoke up twice.", "He preached on the lilies of the field. I thought of my garden.", "Sermon on loving thy neighbour. Some folks need it twice." } },
        };

        public static string AfterThought(string subject, string seed)
        {
            string[] pool;
            return After.TryGetValue(subject ?? "season", out pool) ? Voice.Pick(seed + "|after", pool) : null;
        }

        /// The box's item at the dinner on the grounds.
        public static string DinnerLine(string town, string seed)
        {
            return Voice.Pick(seed + "|dinner", "Dinner on the grounds behind the church in " + town + ": trestle tables, fried chicken and pies, the children running between.",
                              "After the service in " + town + ", dinner on the grounds: ham, biscuits, lemonade in a tub, and everybody's business talked over.",
                              "The ladies of " + town + " have the tables out behind the church: chicken, beans, three kinds of pie. Anyone may sit.");
        }

        public static IEnumerable<string> AllLines()
        {
            for (int i = 0; i < 12; i++) yield return HymnLine("h" + i);
            foreach (string[] p in After.Values) foreach (string l in p) yield return l;
            for (int i = 0; i < 6; i++) yield return DinnerLine("Valentine", "d" + i);
        }
    }
}
