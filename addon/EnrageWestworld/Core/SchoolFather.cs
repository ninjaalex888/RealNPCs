// The schoolteacher and the father (LIVES item 2a, 2026-11-12; [Scenes] SchoolFather, settings version 467; the roadmap's
// "town between people" row). A town's schoolteacher meets a farming or working father of her town in the street on a school
// day and asks after his boy, who hasn't been at his desk; the father wants him in the fields. How it ends is the father's
// nature: the proud and the hotheaded won't be told ("He'll learn what he needs from me."), the rest give a little
// ("Mornings, then. Afternoons he's mine."). It comes most at planting and harvest, when the boys were kept home.
//
// Why it reads true: in 1899 most rural children went to school only part of the year, and attendance fell away exactly
// when the farm needed hands (compulsory attendance laws existed in most states and were barely enforced in the country).
// The quarrel is between two goods, the family's bread now and the boy's chances later, so neither is the villain, and the
// player who watches can't tell who's right. The teacher is often new and young, and the father reads it as being told his
// business by a girl; the softer fathers are the ones who never had the schooling themselves.
//
// It leaves a tie: cold if he won't hear it ("schoolrow"), warm if he gives ground ("schoolkept"). Cast from two already in
// the street near each other (the Director's pair reasons): nobody is walked anywhere. Off in Core so the goldens stand.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class SchoolFather
    {
        public const string Kind = "school", RowKind = "schoolrow", KeptKind = "schoolkept";
        public static bool On;   // [Scenes] SchoolFather, set by the game
        public const int Strength = 41, SeasonPercent = 14, OtherPercent = 5;

        public static bool IsTeacher(Someone s)
        {
            return s != null && s.P != null && s.P.Trade != null && s.P.Trade.IndexOf("teacher", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// A father who might keep a boy home: farming or working folk, old enough for a boy at school.
        public static bool IsFather(Someone s, int year)
        {
            if (s == null || s.Woman || s.P == null || IsTeacher(s))
                return false;
            string g = InnerLife.TradeGroup(s.P.Trade);
            int age = s.P.AgeIn(year);
            return (g == "farm" || g == "working" || g == "plain") && age >= 28 && age <= 58;
        }

        /// Planting and harvest, when boys were kept home.
        public static bool FieldSeason(int month)
        {
            return month >= 3 && month <= 5 || month >= 8 && month <= 10;
        }

        /// x the teacher, y a father of her town, school hours on a weekday, in the street: a reason, or null.
        public static Reason For(Someone x, Someone y, SceneTie tie, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || where.Saloon || where.Night || x == null || y == null || x.Id == y.Id)
                return null;
            if (!IsTeacher(x) || !IsFather(y, InnerLife.ClockYear))
                return null;
            if (x.P.Home == null || x.P.Home != y.P.Home)
                return null;
            int wd = InnerLife.Weekday;
            if (wd < 1 || wd > 5 || where.Hour < 8 || where.Hour >= 17)
                return null;   // a school day, school hours
            if (tie != null && tie.Kind == KeptKind && gameDay * 1440 - tie.At < 7 * 1440)
                return null;   // he gave ground this week: she lets it be a while
            int pct = FieldSeason(InnerLife.ClockMonth) ? SeasonPercent : OtherPercent;
            if (!Voice.Chance(x.Id + "|" + y.Id + "|" + gameDay + "|school", pct))
                return null;
            bool again = tie != null && tie.Kind == RowKind;
            return new Reason { Kind = Kind, Strength = Strength + (again ? 4 : 0), Why = x.Name + " asks " + y.Name + " after his boy, who hasn't been at school" + (again ? " (again)" : "") };
        }

        /// Whether he gives ground, by his nature (and a second time asked, a little likelier).
        public static bool GivesGround(string temper, bool again, string seed)
        {
            int pct = temper == "Proud" || temper == "Hothead" ? 15 : temper == "Cold" || temper == "Greedy" ? 30 : temper == "Loyal" ? 65 : 50;
            return Voice.Chance(seed + "|ground", pct + (again ? 15 : 0));
        }

        /// A the teacher, B the father.
        public static Scene Make(SceneCast c, string seed)
        {
            string b = c != null && c.Names != null && c.Names[1] != null ? Surname(c.Names[1]) : null;
            string mister = b != null ? "Mr. " + b : "mister";
            bool ground = GivesGround(c != null && c.Tempers != null ? c.Tempers[1] : null, false, seed);
            var s = new Scene { Kind = ground ? KeptKind : RowKind, Doer = 0, Summary = "the schoolteacher asked a father after his boy, kept home from school" };
            s.Add(0, Pick(Asks, seed + "|a").Replace("{m}", mister), "mild", "face")
             .Add(1, Pick(Fields, seed + "|f"), ground ? "mild" : "angry")
             .Add(0, Pick(Presses, seed + "|p"), "mild");
            if (ground)
            {
                s.Add(1, Pick(Gives, seed + "|g"), "mild");
                s.Add(0, Pick(Thanks, seed + "|t"), "thanks");
                s.Summary += "; he gave a little ground";
            }
            else
            {
                s.Add(1, Pick(Wont, seed + "|w"), "angry", "face");
                s.Summary += "; he wouldn't hear it";
            }
            return s;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[] a in new[] { Fields, Presses, Gives, Thanks, Wont })
                foreach (string l in a)
                    yield return l;
            foreach (string l in Asks)
                yield return l.Replace("{m}", "Mr. Tully");
        }

        private static readonly string[] Asks = {
            "Good day, {m}. Your boy hasn't been at his desk since Monday.",
            "{m}, a word? I've had an empty seat in the second row all week.",
            "{m}, is your boy unwell? He's missed four days running." };
        private static readonly string[] Fields = {
            "Corn won't pick itself, miss.",
            "He's needed at home. There's a field to get in.",
            "I can't spare him. Not with the season on us.",
            "His hands earn more in my rows than his head does at your slate." };
        private static readonly string[] Presses = {
            "Nor will he learn to read by picking it.",
            "He's the quickest in the room at his sums. It would be a shame to waste it.",
            "A boy who can read a contract won't be cheated out of his land.",
            "Give him the mornings, at least. That's all I ask." };
        private static readonly string[] Gives = {
            "Mornings, then. Afternoons he's mine.",
            "When the field's in, he'll be back. You have my word.",
            "His mother says the same as you. I'll not argue with the both of you." };
        private static readonly string[] Thanks = {
            "Thank you. I'll keep his seat.",
            "That's all I wanted. He'll be glad of it when he's grown.",
            "I'll send his lessons home with the Hale girl." };
        private static readonly string[] Wont = {
            "He'll learn what he needs from me, miss. That's an end of it.",
            "I never had a day of school and I've done well enough.",
            "You'll not tell me how to raise my own son, miss." };

        private static string Surname(string name)
        {
            string[] w = name.Trim().Split(' ');
            return w.Length >= 2 ? w[w.Length - 1] : null;
        }

        private static string Pick(string[] a, string seed) { return a[(Voice.Hash(seed) & 0x7fffffff) % a.Length]; }
    }
}
