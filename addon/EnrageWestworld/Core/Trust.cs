// Trust lost fast, won slow (FIVEDAY item 20; NIGHT3's psychologist list). Until now a greeting raised enRAGE's liking bar
// by the same 34 for anyone not afraid or angry at that moment, so three hellos the day after you knocked a man down
// filled his bar as they would a stranger's. Now a wrong of yours done to someone (a shove, a beating, a robbery, a theft,
// a horse taken, a kidnapping, in the last 60 game days) makes your kindness count for less with them until time and
// repeated kindness bring them back. It starts at a fifth, and the rest returns over their forgiving time (three weeks as
// built: sooner for the compassionate and trusting, longer for the angry, 5 to 42 days) and with each kindness since.
// And the wrong itself costs three hellos' worth at once: one threat undoes three kindnesses. With nothing between you,
// "three hellos fill the bar" (Lex's GreetLiking) is untouched. Behind [Mind] TrustSlow.
//
// Why it reads true: negativity bias (Baumeister: bad is stronger than good; one harm weighs about what several kindnesses
// do), and forgiveness as a slow process that runs at the pace of the forgiver's temperament (agreeableness, trust) and is
// helped by repeated amends. Plain C#, no game. mindtest TrustTest.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Trust
    {
        public const int WindowDays = 60;      // a wrong older than this is past
        public const int Undoes = 3;           // one wrong undoes three kindnesses (the bar, at once)
        public const float Floor = 0.2f;       // a kindness the day after counts for a fifth

        private static readonly HashSet<string> Wrongs = new HashSet<string> { "shove", "assault", "robbery", "theft", "horsetheft", "kidnap" };

        public static bool IsWrong(string deedKind) { return deedKind != null && Wrongs.Contains(deedKind); }

        /// The latest wrong of his done to this person within the window, or null.
        public static Deed LastWrong(List<Deed> deeds, string id, long now)
        {
            if (deeds == null || id == null)
                return null;
            for (int i = deeds.Count - 1; i >= 0; i--)
            {
                Deed d = deeds[i];
                if (d != null && d.VictimId == id && IsWrong(d.Kind) && now - d.At >= 0 && now - d.At <= WindowDays * 1440L)
                    return d;
            }
            return null;
        }

        /// How long they take to forgive, in game days: 21, less for the compassionate and the trusting, more for the angry.
        public static int ForgiveDays(Traits t)
        {
            float d = 21f;
            if (t != null)
                d += -6f * t.Z[Traits.Compassion] - 4f * t.Z[Traits.Trust] + 6f * t.Z[Traits.Anger];
            return (int)Math.Round(Math.Max(5f, Math.Min(42f, d)));
        }

        /// What a kindness counts for now (0.2 to 1): the floor, the rest coming back with the days since the wrong over their
        /// forgiving time, and with each kindness since (six hundredths, scaled by how forgiving they are).
        public static float Factor(Traits t, long minutesSince, int kindSince)
        {
            float days = Math.Max(0f, minutesSince / 1440f);
            int fd = ForgiveDays(t);
            float f = Floor + (1f - Floor) * Math.Min(1f, days / fd) + 0.06f * Math.Max(0, kindSince) * 21f / fd;   // each kindness since helps, the more for the forgiving
            return Math.Min(1f, f);
        }

        /// The key the kindnesses since a wrong are counted under.
        public static string MendKey(string id, Deed wrong) { return id + "|" + (wrong != null ? wrong.Id ?? wrong.At.ToString() : ""); }
    }
}
