// A friend runs down the man who robbed you (DEEPEN item 2, 2026-11-11; [Scenes] FriendsChase). Two proven runners joined: the
// lift (WestworldScript.Lift.cs: a man bumps into you and runs with some of your cash, seen in game) and the bystander's chase
// (WestworldScript.PurseSnatch.cs: the lawman's run and tackle orders, both seen). Until now nobody but the law went after the
// man who robbed you. Now one of your friends near you may: the same people who come to your aid in a fight (Allies: enRAGE's
// fondness at the bar, a kindness of yours remembered, a trouble you mended, someone you saved), weighed by their nature.
// The psychology:
//   - Who runs: the bold and the reckless first, the kind, the hot-tempered; the anxious seldom, as with any chase. A friend
//     has a reason a stranger hasn't, and one who owes you (warmth 3) has more.
//   - What it does to them (the Ben Franklin effect): people come to like those they have done a good turn for, not only the
//     other way round. So the friend who ran your thief down thinks the better of you for it (enRAGE's fondness up a little),
//     and keeps it as a memory of standing up for you, which their thoughts speak of for days.
//   - What the town makes of it: word goes round that a townsman ran down the man who robbed the stranger.
// Plain C#, no game. All lines are original to this add-on. Off in Core so the harness goldens stand; ApplySettings sets it.
using System;

namespace EnrageWestworld.Core
{
    public static class FriendChase
    {
        public static bool On;

        public const float FromMetres = 30f;   // a friend within this of you when he runs
        public const int LikeMore = 6;         // enRAGE's fondness a friend gains for you by doing you the good turn

        /// The share of friends who give chase (percent): warmth 2 a friend, 3 one who owes you; then their nature.
        public static int Percent(Traits t, int warmth)
        {
            float c = warmth >= 3 ? 0.50f : warmth >= 2 ? 0.30f : 0f;
            if (c <= 0f)
                return 0;
            if (t != null)
                c += 0.15f * t.Z[Traits.Assertiveness] + 0.12f * t.Z[Traits.Risk] + 0.08f * t.Z[Traits.Compassion] + 0.08f * t.Z[Traits.Anger] - 0.15f * t.Z[Traits.Anxiety];
            return (int)Math.Round(Math.Max(0.05f, Math.Min(0.85f, c)) * 100);
        }

        public static bool Chases(Traits t, int warmth, string seed)
        {
            return warmth >= 2 && Voice.Chance(seed, Percent(t, warmth));
        }

        public static string Shout(string you, string seed)
        {
            return Voice.Pick(seed, "Hey! That's " + you + "'s money you got!", "I saw that! Come back here!", "Thief! I've got him, " + you + "!",
                              "Drop it and run, or I'll sit on you!");
        }

        public static string Handed(string seed)
        {
            return Voice.Pick(seed, "Here. Count it. He didn't get far.", "Yours, I believe. Keep a hand on it next time.",
                              "Took it off him. He won't try you again in a hurry.", "There. The fella had legs, I'll give him that.");
        }

        public static string News(string chaser, string seed)
        {
            return Voice.Pick(seed, chaser + " ran down the man who robbed the stranger and gave him back every cent.",
                              "A thief lifted the stranger's money, and " + chaser + " caught him before the corner.");
        }

        public static string NoticeWords(string chaser, string amount)
        {
            return chaser + " ran him down and hands you back your " + amount + ".";
        }
    }
}
