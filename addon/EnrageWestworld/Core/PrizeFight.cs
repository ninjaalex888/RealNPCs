// A barn prize fight (FIVEDAY item 10; roadmap.md's idea bank: "an illegal bare-knuckle fight in a barn, bets, a ring of
// regulars. Bet, fight, or tip off the sheriff"). Now and then a town with a saloon has two of its own men agree to settle who
// is the better with their fists, for a purse, out back after dark. It is never in the paper: the word goes round the saloon
// that afternoon (a toast). Away, the daily tick settles it (who won by their natures, the town's talk next morning); there,
// it is staged behind the saloon where nobody sees it set up: the two fighters, a ring of men and a lookout, the lookout's
// call, the bout, the end. On J while it runs (the event menu): put a dollar on either man (paid two to one if he wins), tip
// off the sheriff (the law breaks it up a little later, and nobody there knows who told), or just watch. The town events'
// rails as the dance's: never in missions, nothing spawned in view, shots or a gun drawn break it up.
//
// Why it reads true: bare-knuckle prize fighting was against the law in most states by 1899 and drew crowds all the more for
// it (Sullivan and Kilrain fought in Mississippi in 1889 against the governor's ban, and the nation read every round); in a small town it was two
// men with a grievance or a reputation, a hat passed round for the purse, and a boy at the corner watching for the marshal.
// The psychologist's lens: a contest of status made public, a crowd's partisanship, and the thrill of a forbidden thing done
// together, which binds those who were there. Plain C#, no game; all words original. mindtest PrizeFightTest.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class PrizeFight
    {
        public const string Kind = "prizefight";
        public const int FromHour = 21, ToHour = 23;          // nine to eleven at night
        public const int GapDays = 6;                         // at most one a town in six days
        public const int Chance = 20;                         // a town that can have one books it on a given day this often (percent)
        public const int WordHour = 15;                       // the word goes round from three in the afternoon
        public const int StakeCents = 100;                    // the player's dollar; two to one if his man wins
        public static readonly string[] Towns = { "Valentine", "Rhodes", "Blackwater", "Strawberry", "Saint Denis", "Van Horn", "Annesburg", "Armadillo", "Tumbleweed" };

        /// Whether a regular would put up his fists for a purse: a man of fighting age, of a temper that would.
        public static bool Fighter(Resident r, int year)
        {
            if (r == null || r.Woman || r.Dead || r.PostId != null)
                return false;
            int age = r.BornYear > 0 ? year - r.BornYear : 30;
            if (age < 18 || age > 48)
                return false;
            return r.Personality == "Hothead" || r.Personality == "Proud" || r.Personality == "Drifter" || r.Personality == "Greedy" || r.Disposition == "Surly";
        }

        /// The two to fight, from the town's regulars who would: seeded, never the same man twice.
        public static Resident[] Pick(List<Resident> regulars, int year, string seed)
        {
            var can = regulars.Where(r => Fighter(r, year)).OrderBy(r => Voice.Hash(seed + "|" + r.Id)).ToList();
            return can.Count >= 2 ? new[] { can[0], can[1] } : null;
        }

        /// The purse in dollars: what the hat collects (3 to 12).
        public static int Purse(string seed)
        {
            return 3 + (Voice.Hash(seed + "|purse") & 0x7fffffff) % 10;
        }

        public static TownEvent Make(string town, long day, Resident a, Resident b, string seed)
        {
            return new TownEvent
            {
                Id = Ids.New(), Kind = Kind, Town = town, Title = "A fight out back", At = day * 1440 + FromHour * 60, Until = day * 1440 + ToHour * 60,
                AId = a.Id, AName = a.Name, BId = b.Id, BName = b.Name, Count = Purse(seed)
            };
        }

        /// Who wins, 0 (A) or 1 (B): the readier fighter by nature (assertiveness, risk, anger, less anxiety), with a
        /// good share of luck, as a fistfight has. pct: A's chance.
        public static int Winner(Traits ta, Traits tb, string seed, out int pct)
        {
            Func<Traits, float> grit = t => t == null ? 0f : 0.5f * t.Z[Traits.Assertiveness] + 0.3f * t.Z[Traits.Risk] + 0.3f * t.Z[Traits.Anger] - 0.4f * t.Z[Traits.Anxiety] + 0.2f * t.Z[Traits.SelfControl];
            float d = grit(ta) - grit(tb);
            float p = 0.5f + 0.18f * Math.Max(-1.5f, Math.Min(1.5f, d));
            pct = (int)Math.Round(Math.Max(0.2f, Math.Min(0.8f, p)) * 100);
            return Voice.Chance(seed + "|win", pct) ? 0 : 1;
        }

        // ---- the words: {a} {b} the fighters' first names, {w} the winner's, {l} the loser's, {town}, {purse} -------------

        private static readonly string[] Word =
        {
            "Word in {town}: {a} and {b} are to settle it with their fists out back of the saloon tonight, after nine. A ${purse} purse. Keep it from the marshal.",
            "They're saying in {town} that {a} means to fight {b} behind the saloon tonight, bare knuckles, ${purse} in the hat. Not a word to the law.",
            "A fight tonight in {town}, out back after dark: {a} against {b}, ${purse} to the winner. The men are already laying money.",
        };

        private static readonly string[] Lookout =
        {
            "Keep it down! The marshal's two streets over.",
            "Quiet, all of you. I'll whistle if anybody comes.",
            "Gentlemen, the hat's going round. Pay up, or go home.",
            "Ring up, boys. Nobody steps inside it but the two of them.",
            "No guns, no knives, no kicking a man down. That's the rule here.",
            "Purse is in the hat. Winner takes the hat and all.",
        };

        private static readonly string[] Ring =
        {
            "Come on, {a}! Show him the ground!",
            "Get your hands up, {b}!",
            "That's it! Under the ribs, under the ribs!",
            "Keep moving, {b}, he can't hit what he can't catch!",
            "Hit him back, {a}, don't just stand there!",
            "Two bits says {b} drops in the next minute!",
            "Ooh! That one rang his bell!",
            "Mind the lantern, you fools!",
            "He's bleeding! Keep at it, {a}!",
            "Not so loud! The law, the law!",
        };

        private static readonly string[] Won =
        {
            "That's {w}'s! Hand him the hat!",
            "{w}! {w} takes it!",
            "Down and out. The purse to {w}.",
            "Somebody pick {l} up. {w} wins it fair.",
        };

        private static readonly string[] Lost =
        {
            "Lord, my head...",
            "I'll have you next time, {w}. Next time.",
            "Fair. It was fair. Help me up.",
            "Don't nobody tell my mother.",
        };

        private static readonly string[] Raid =
        {
            "Law! The law's coming! Scatter!",
            "Lanterns out! Somebody talked!",
            "It's the marshal! Run, boys!",
        };

        /// The word that goes round (a toast) the afternoon of the fight.
        public static string WordText(TownEvent e, string seed)
        {
            return Fill(Pick(Word, seed + "|word"), e);
        }

        public static string LookoutLine(string seed) { return Pick(Lookout, seed + "|look"); }
        public static string RingLine(TownEvent e, string seed) { return Fill(Pick(Ring, seed + "|ring"), e); }
        public static string RaidLine(string seed) { return Pick(Raid, seed + "|raid"); }
        public static string WonLine(TownEvent e, int winner, string seed) { return Fill(Pick(Won, seed + "|won"), e, winner); }
        public static string LostLine(TownEvent e, int winner, string seed) { return Fill(Pick(Lost, seed + "|lost"), e, winner); }

        /// The town's talk next morning: who won, or that the law broke it up.
        public static string Morning(TownEvent e, int winner, bool raided)
        {
            if (raided)
                return Fill("The law broke up a fist fight behind the saloon in {town} last night, {a} against {b}. Nobody will say who told.", e);
            return Fill("Out back of the {town} saloon last night {w} put {l} down for a ${purse} purse. {l} moves careful today.", e, winner);
        }

        /// The event menu's rows (J while the fight runs).
        public static List<InteractOption> Rows(TownEvent e)
        {
            string part = e != null ? e.Part ?? "" : "";
            bool bet = part.Contains("bet:"), told = part.Contains("tipped");
            string a = Person.FirstOf(e != null ? e.AName : null) ?? "the first man", b = Person.FirstOf(e != null ? e.BName : null) ?? "the other";
            return new List<InteractOption>
            {
                new InteractOption { Id = "fight:betA", Label = "Put a dollar on " + a + ".", Why = bet ? "your money's down" : null },
                new InteractOption { Id = "fight:betB", Label = "Put a dollar on " + b + ".", Why = bet ? "your money's down" : null },
                new InteractOption { Id = "fight:tip", Label = "Slip away and tip off the sheriff.", Why = told ? "you've told" : null },
                new InteractOption { Id = "fight:watch", Label = "Just watch." },
            };
        }

        private static string Fill(string line, TownEvent e, int winner = 0)
        {
            string a = Person.FirstOf(e.AName) ?? "the one", b = Person.FirstOf(e.BName) ?? "the other";
            return line.Replace("{a}", a).Replace("{b}", b).Replace("{w}", winner == 0 ? a : b).Replace("{l}", winner == 0 ? b : a)
                       .Replace("{town}", e.Town ?? "town").Replace("{purse}", e.Count.ToString());
        }

        private static string Pick(string[] pool, string seed)
        {
            return pool[(Voice.Hash(seed) & 0x7fffffff) % pool.Length];
        }

        public static IEnumerable<string> AllLines()
        {
            var e = new TownEvent { AName = "Amos Pike", BName = "Hiram Cole", Town = "Valentine", Count = 7 };
            foreach (string l in Word.Concat(Ring).Concat(Won).Concat(Lost))
            {
                yield return Fill(l, e, 0);
                yield return Fill(l, e, 1);
            }
            foreach (string l in Lookout.Concat(Raid))
                yield return l;
            yield return Morning(e, 0, false);
            yield return Morning(e, 1, true);
        }
    }
}
