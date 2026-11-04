// Reactions at gunpoint, in stages (SIXDAY item 20; roadmap 38). The game's own first answer to a gun stays (hands up,
// the begging: the plea in WestworldScript.Frontier.cs); here is what comes after, when the gun stays on them. Six seconds
// on, a second step chosen by who they are (the mind's table, and the model's pick when it's on and the mind knows them):
// beg for their life, defy him to his face, back away a step at a time, break and run, or freeze with their hands up
// and say nothing. Fourteen seconds on, a third: those who begged or froze mostly break and run, the steady hold, the
// defiant stand, the ones backing off keep going. Why it reads true: fear under a gun moves in steps (freeze, then
// appease, then flight), and who takes which step is temperament and what they think of the man holding it. Plain C#,
// no game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Gunpoint
    {
        public const int SecondStageMs = 6000, ThirdStageMs = 14000;   // real time with the gun still on them
        public static readonly string[] Stances = { "plead", "defy", "back", "run", "freeze" };
        private static readonly string[] StrangerTempers = { "Coward", "Hothead", "Proud", "Cold", "Loyal", "Drifter", null, null };

        /// A stranger's temper for the table, when the mind doesn't know them: seeded.
        public static string StrangerTemper(string seed)
        {
            return StrangerTempers[Voice.Hash(seed + "|gtemper") % StrangerTempers.Length];
        }

        /// The second step's table: what they do with the gun still on them six seconds on. defiantFirst: the plea itself was
        /// defiant (Plea.Defiant); bravery: [World] Bravery, 0-100 (the brave run less and stand more).
        public static Staged.Weights Weights(StagedMind m, bool defiantFirst, int bravery)
        {
            string t = m != null ? m.Temper : null;
            int plead = 3, defy = 1, back = 2, run = 2, freeze = 2;
            switch (t)
            {
                case "Coward": plead += 3; run += 3; defy = -9; break;
                case "Hothead": defy += 4; run -= 1; plead -= 1; break;
                case "Proud": defy += 3; plead -= 2; freeze += 1; break;
                case "Cold": back += 3; freeze += 2; plead -= 2; break;
                case "Loyal": freeze += 2; break;
                case "Greedy": plead += 1; break;
                case "Drifter": run += 2; back += 1; break;
            }
            if (m != null && m.Woman) { plead += 1; defy -= 1; }
            if (m != null && m.Drunk) { defy += 1; run -= 1; back -= 1; }
            if (m != null && m.ToYou < 0) defy += 2;
            if (m != null && m.ToYou > 0) plead += 1;
            if (defiantFirst) { defy += 3; plead -= 1; }
            if (bravery >= 70) { run -= 1; freeze += 1; defy += 1; }
            else if (bravery <= 30) run += 1;
            var w = new Staged.Weights();
            w.Add("plead", Math.Max(0, plead)).Add("defy", Math.Max(0, defy)).Add("back", Math.Max(0, back)).Add("run", Math.Max(0, run)).Add("freeze", Math.Max(1, freeze));
            return w.Clamp();
        }

        /// The third step, fourteen seconds on: the defiant stand, the ones backing off keep going, a runner is gone; those who
        /// begged or froze mostly break and run (three in four), the steady (Loyal, Cold) mostly hold.
        public static string Third(string second, string temper, string seed)
        {
            if (second == "defy" || second == "back" || second == "run")
                return second;
            bool steady = temper == "Loyal" || temper == "Cold";
            return Voice.Chance(seed + "|break", steady ? 40 : 75) ? "run" : "freeze";
        }

        /// The option as the model is offered it (the person's own point of view).
        public static string Words(string stance)
        {
            switch (stance)
            {
                case "plead": return "beg him for your life";
                case "defy": return "defy him to his face";
                case "back": return "back away from him a step at a time";
                case "run": return "break and run for it";
                default: return "stand with your hands up and say nothing";
            }
        }

        /// The model's "Right now:" line.
        public static string Now(string label)
        {
            return "A stranger's gun has been on you six seconds now with no shot. You are " + (label ?? "a bystander") + ".";
        }

        /// What they say taking the step (freeze: nothing).
        public static string Line(string stance, bool woman, string seed)
        {
            switch (stance)
            {
                case "plead":
                    return woman ? Voice.Pick(seed + "|gp", "Please. I've children. Please, mister.", "Whatever it is you want, take it. Only put that down.", "I'm nobody. I'm nobody at all. Please.", "Don't. Oh God, don't. I'll not say a word to anyone.")
                                 : Voice.Pick(seed + "|gp", "Please. I've a family. Take what you want.", "I'll not tell a soul. On my mother's grave, mister.", "There's four dollars in my coat. It's yours. Just lower it.", "I never crossed you in my life. Put it away.");
                case "defy":
                    return woman ? Voice.Pick(seed + "|gd", "Shoot, then, if you're going to. I'll not beg.", "You'd point that at a woman. Your mother must be proud.", "Pull it, then. Half the town can see you from here.", "Men twice your size have tried that on me. Shoot or clear off.")
                                 : Voice.Pick(seed + "|gd", "Shoot or don't. I'll not beg a man like you.", "You've got the gun. That's all you've got.", "Go on, then. They hang men for it, and they'll hang you.", "Bigger men than you have tried that look on me, boy.");
                case "back":
                    return Voice.Pick(seed + "|gb", "Easy. Easy, now. I'm going. I'm just going.", "No trouble from me. See? I'm backing off.", "I'm walking off, nice and slow. Watch my hands.");
                case "run":
                    return Voice.Pick(seed + "|gr", "Help! He's got a gun on me!", "Lord save me!", "Somebody! Somebody!");
                default:
                    return null;
            }
        }

        /// The game's voiced line set that goes with it.
        public static string VoiceFor(string stance)
        {
            return stance == "plead" ? "plead" : stance == "defy" ? "threat" : stance == "freeze" ? null : "scared";
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string s in Stances)
            {
                yield return Words(s);
                for (int i = 0; i < 6; i++)
                {
                    string m = Line(s, false, "m" + i), w = Line(s, true, "w" + i);
                    if (m != null) yield return m;
                    if (w != null) yield return w;
                }
            }
            yield return Now("a farmhand");
        }
    }
}
