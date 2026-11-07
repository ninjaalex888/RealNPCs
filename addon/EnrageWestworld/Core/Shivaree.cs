// The shivaree (FIVEDAY item 13; roadmap.md's idea bank: "the night after a wedding, friends bang pots and pans under the
// couple's window until the groom stands drinks"). A town event booked when a wedding in town is over (and wasn't broken
// up): the next night from nine, the couple's friends gather with pans, a cowbell and a tin horn, raise a din and shout for
// the groom; he comes out, groans, laughs, and stands the lot of them a round. Away, it leaves the friends warmer to the
// couple and the couple a memory; there (staged by the saloon he last came out of, where the revellers start from), being
// a minute or more of it counts as attending, as at the dance, and the regulars warm a little to the stranger.
//
// Why it reads true: the charivari came west from French and English villages, and on the frontier it was the community's
// claim on a new couple: noise as a welcome and a test of good humour, paid off in drink, that took the couple into the
// town's life. Plain C#, no game; all words original. mindtest ShivareeTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Shivaree
    {
        public const string Kind = "shivaree";
        public const int FromHour = 21, ToMinutes = 22 * 60 + 30;   // nine till half past ten, the night after
        public const int Affection = 3;                             // what a regular there feels for the stranger who stood with them

        /// The shivaree for a wedding just over: the next night, the couple's own names (the groom the man of them).
        public static TownEvent For(Wedding w, long now)
        {
            if (w == null || w.Ruined || w.Town == null)
                return null;
            long day = now / 1440 + 1;
            bool aGroom = !w.AWoman;
            return new TownEvent
            {
                Id = Ids.New(), Kind = Kind, Town = w.Town, Title = "A shivaree", At = day * 1440 + FromHour * 60, Until = day * 1440 + ToMinutes,
                AId = aGroom ? w.AId : w.BId, AName = aGroom ? w.AName : w.BName, BId = aGroom ? w.BId : w.AId, BName = aGroom ? w.BName : w.AName
            };
        }

        private static readonly string[] Din = { "[pans clanging, a cowbell, a tin horn]", "[a washtub beaten like a drum]", "[pots and lids, and somebody's fiddle out of tune]" };
        private static readonly string[] Calls =
        {
            "Come on out, {a}! We know you're in there!",
            "{a}! Your friends are thirsty!",
            "No sleep for you two till the drinks are stood!",
            "Louder, boys! He's not stirring!",
            "{b}, send him out to us, or we'll sing all night!",
            "We've a whole hymnal of bad songs, {a}!",
            "Show your face, {a}, you lucky dog!",
        };
        private static readonly string[] Groom = { "All right! All right! Drinks are on me, you heathens!", "For the love of God. The saloon, the lot of you. I'm buying.", "I knew you'd come. I've the money ready. Go on!" };
        private static readonly string[] After = { "Three cheers for the bride!", "Long life and a quiet house!", "To the saloon, boys, before he changes his mind!" };

        public static string DinLine(string seed) { return Pick(Din, seed + "|din"); }
        public static string CallLine(TownEvent e, string seed) { return Fill(Pick(Calls, seed + "|call"), e); }
        public static string GroomLine(string seed) { return Pick(Groom, seed + "|groom"); }
        public static string AfterLine(string seed) { return Pick(After, seed + "|after"); }

        /// The morning's word about town.
        public static string Morning(TownEvent e, bool attended)
        {
            return Fill(attended ? "They banged pans for {a} and {b} till half past ten, the stranger among them. {a} paid for a round at the saloon."
                                 : "They gave {a} and {b} a proper shivaree last night. Pans, a cowbell, the lot. {a} stood the crowd a round.", e);
        }

        private static string Fill(string l, TownEvent e)
        {
            return l.Replace("{a}", Person.FirstOf(e.AName) ?? "the groom").Replace("{b}", Person.FirstOf(e.BName) ?? "the bride");
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            var e = new TownEvent { AName = "Amos Pike", BName = "Ruth Cole" };
            foreach (string l in Calls) yield return Fill(l, e);
            foreach (string l in Groom) yield return l;
            foreach (string l in After) yield return l;
            yield return Morning(e, true);
            yield return Morning(e, false);
        }
    }
}
