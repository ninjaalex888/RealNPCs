// Vouch for a prisoner (FOURTEENHOUR reserve R22; TWELVEHOUR reserve R11; [Scenes] Vouch). The jail door the morning after a
// brawl (FOURTEENHOUR item 12, Core/BrawlAfter.cs: his wife or his mother waits there with his breakfast, and J pays his fine)
// has a third row: vouch for him. The deputy writes your name beside his in the book: he's bound over to keep the peace a week,
// you his surety for five dollars, and he's out now with no fine. Then the week. If he breaks the peace (seen in a fight near
// you on the side that began it, tied up by the law again, or the week's own word, a fight somewhere you weren't), your name
// pays: the five dollars go to the county the next time you're in his town, the town hears whose word it was, and that sheriff
// won't take your word again for a month. If he keeps it, he's fonder of the stranger who stood for him, and the town hears
// that too. Why it reads true: a justice of the 1890s could bind a brawler over "to keep the peace" with a surety who forfeited
// a sum if he broke it, so one man's name stood for another's conduct; and a man watched by the one who vouched for him (and
// by his own wife) has two reasons to keep his fists in his pockets, which is why the loyal, the honest and the family man
// keep it likelier and the hothead and the drinker less. Plain C#, no game; the game side is Game/WestworldScript.Vouch.cs.
// All the words are this add-on's.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    /// One man you stood surety for, kept in the save (Store.Vouches) so his week runs on across a reload.
    [DataContract]
    public sealed class Vouched
    {
        [DataMember(EmitDefaultValue = false)] public string Id, Name, Town;   // the man, and the town whose sheriff took your word
        [DataMember(EmitDefaultValue = false)] public long At;                  // the game minute you stood for him
        [DataMember(EmitDefaultValue = false)] public int BreaksOn;             // the week's own word, fixed then: the day (1-6) he fights where you aren't; 0, he keeps it
        [DataMember(EmitDefaultValue = false)] public string Ended;             // null while his week runs; then "broke", "kept" or "dead"
        [DataMember(EmitDefaultValue = false)] public string How;               // how he broke it: "seen" (near you), "law" (tied up again), "word" (somewhere you weren't)
        [DataMember(EmitDefaultValue = false)] public long EndedAt;             // the game minute it ended
        [DataMember(EmitDefaultValue = false)] public bool Settled;             // its end told to you in his town (the forfeit taken, or the thanks)
        [DataMember(EmitDefaultValue = false)] public bool Paid;                // broke: the five dollars were on you when it was told
        [DataMember(EmitDefaultValue = false)] public bool Test;                // the Testing row's: gone on load, and no fondness from it
    }

    public static class Vouch
    {
        public static bool On = true;   // [Scenes] Vouch, from the settings
        public const string RowId = "jaildoor:vouch";
        public const int SuretyCents = 500, KeptFondness = 10, LookMs = 5000, MostKept = 12;
        public static int KeepDays { get { return SessionClock.Days(7, 2); } }
        static string Span { get { return SessionClock.On ? "two days" : "a week"; } }   // what the lines call his time (review 2: they said a week for two days)   // the session clock (DEPTH item 16): SessionClock.Days(as built, a session's)
        public static int NoWordDays { get { return SessionClock.Days(30, 2); } }

        /// The chance he breaks the peace in his week somewhere you aren't: one in five for most men; a hothead or a drinker
        /// likelier, a proud man or a braggart a little; a loyal man, an honest one, a family man or a married man less (two
        /// people watching him), a coward a little less. Never under one in twenty, never over seven in ten.
        public static float BreakChance(string personality, IList<string> quirks, bool married)
        {
            float p = 0.2f;
            if (personality == "Hothead") p += 0.25f;
            else if (personality == "Proud") p += 0.1f;
            else if (personality == "Loyal") p -= 0.1f;
            else if (personality == "Coward") p -= 0.08f;
            if (quirks != null)
            {
                if (quirks.Contains("drunk")) p += 0.15f;
                if (quirks.Contains("braggart")) p += 0.05f;
                if (quirks.Contains("honest")) p -= 0.08f;
                if (quirks.Contains("family man")) p -= 0.06f;
            }
            if (married) p -= 0.04f;
            return Math.Max(0.05f, Math.Min(0.7f, p));
        }

        /// The week's own word, fixed the moment you stand for him (seeded by him and the day): the day of his week (1-6) he
        /// gets into a fight somewhere you aren't, or 0 if he keeps the peace. A fight you see, or the law taking him, comes first.
        public static int BreaksOn(string id, long day, float chance)
        {
            int h = Voice.Hash((id ?? "") + "|" + day + "|vouch") & 0x7fffffff;
            if ((h % 1000) / 1000f >= chance)
                return 0;
            return 1 + (h / 1000) % 6;
        }

        /// What his week comes to by now, when nothing you saw ended it: "broke" once the week's word comes, "kept" when the
        /// week is out, or null while it runs.
        public static string WeekEnd(Vouched v, long now)
        {
            if (v == null || v.Ended != null)
                return null;
            long days = (now - v.At) / 1440;
            if (v.BreaksOn > 0 && days >= v.BreaksOn)
                return "broke";
            return now - v.At >= KeepDays * 1440L ? "kept" : null;
        }

        /// A man you vouched for broke the peace in this town within the month: that sheriff won't take your word again yet.
        public static bool NoWord(List<Vouched> vs, string town, long now)
        {
            if (vs == null || town == null)
                return false;
            foreach (Vouched v in vs)
                if (v != null && !v.Test && v.Town == town && v.Ended == "broke" && now - v.EndedAt < NoWordDays * 1440L)   // (the Testing row's never greys it)
                    return true;
            return false;
        }

        /// A week running (any man), for the live checks: nothing to look up when there's none.
        public static bool AnyOpen(List<Vouched> vs)
        {
            if (vs == null)
                return false;
            foreach (Vouched v in vs)
                if (v != null && v.Ended == null)
                    return true;
            return false;
        }

        /// His open week, if you stood for him and it runs still.
        public static Vouched OpenFor(List<Vouched> vs, string id)
        {
            if (vs == null || id == null)
                return null;
            foreach (Vouched v in vs)
                if (v != null && v.Ended == null && v.Id == id)
                    return v;
            return null;
        }

        /// The list kept to MostKept: the oldest settled go first, as ever; and only past twice that, the oldest whose end you were
        /// never told (you never went back to his town): before, those stayed for good, past the cap too (SWEEP item 4's soak). A
        /// week still running is never dropped.
        public static void Trim(List<Vouched> vs)
        {
            if (vs == null)
                return;
            while (vs.Count > MostKept)
            {
                int oldest = -1;
                for (int i = 0; i < vs.Count; i++)
                    if (vs[i] != null && vs[i].Settled && (oldest < 0 || vs[i].EndedAt < vs[oldest].EndedAt))
                        oldest = i;
                if (oldest < 0)
                    break;   // none settled: the rest stay, unless...
                vs.RemoveAt(oldest);
            }
            while (vs.Count > MostKept * 2)
            {
                int oldest = -1;
                for (int i = 0; i < vs.Count; i++)
                    if (vs[i] != null && vs[i].Ended != null && (oldest < 0 || vs[i].EndedAt < vs[oldest].EndedAt))
                        oldest = i;
                if (oldest < 0)
                    break;   // all still running: they stay
                vs.RemoveAt(oldest);
            }
        }

        /// Done with: told, and a broken word's month is out (a kept one or a dead man's a day after it's told).
        public static bool Gone(Vouched v, long now)
        {
            if (v == null)
                return true;
            if (!v.Settled)
                return false;
            return now - v.EndedAt > (v.Ended == "broke" ? NoWordDays : 1) * 1440L;
        }

        /// Why the sheriff won't take your word here: a killer's word is no word, nor the word of a man whose last one broke.
        public static string Why(string standing, bool noWord)
        {
            if (standing == "killer")
                return "the sheriff won't take a killer's word";
            if (noWord)
                return "the sheriff here won't take your word again";
            return null;
        }

        public static InteractOption Row(string first, string why)
        {
            return new InteractOption { Id = RowId, Label = "Vouch for " + first + " (" + BrawlVerbs.Money(SuretyCents) + " if he fights " + (SessionClock.On ? "these two days" : "this week") + ").", Why = why };
        }

        // ---- the words ----

        private static readonly string[] Thanks = { "You'd put your name to him? He'll not shame you, mister. I'll see he doesn't.",
                                                    SessionClock.On ? "Two days. He can keep his hands to himself two days, God help him." : "A week. He can keep his hands to himself a week, God help him.",
                                                    "Bless you. Nobody ever stood up for him before." };

        public static string ThanksLine(string seed) { return Voice.Pick(seed + "|vthanks", Thanks); }

        public static string BoundHint(string first)
        {
            return "The deputy writes your name beside " + first + "'s in the book: bound over to keep the peace " + Span + ", and you his surety.";
        }

        public static string StoodNews(string name, string seed)
        {
            return Voice.Pick(seed + "|vstood", "A stranger stood surety for " + name + " at the jail this morning. He's bound over to keep the peace " + Span + ".",
                                                name + " is out of the cells on a stranger's word, bound over " + Span + ".");
        }

        /// How he broke it, for the hint and the news.
        public static string HowWords(string how, string seed)
        {
            switch (how)
            {
                case "seen": return "with you there to see it";
                case "law": return "and the deputy had to lock him up again";
                default: return Voice.Pick(seed + "|vhow", "brawling at the saloon's bar", "trading blows behind the livery", "out in the street after dark");
            }
        }

        public static string BrokeHint(string first, string how, bool paid, string seed)
        {
            return "The sheriff's word: " + first + " broke the peace " + HowWords(how, seed) + ". " +
                   (paid ? "Your " + BrawlVerbs.Money(SuretyCents) + " surety goes to the county, and the sheriff won't take your word again this month."
                         : "You haven't the " + BrawlVerbs.Money(SuretyCents) + " surety on you: the sheriff marks it against your name and won't take your word this month.");
        }

        public static string BrokeNews(string name, string seed)
        {
            return Voice.Pick(seed + "|vbroke", name + " broke the peace before his week was out. The stranger who stood for him is five dollars poorer.",
                                                 "So much for a stranger's word: " + name + " was fighting again inside his week.");
        }

        public static string KeptHint(string first, string seed)
        {
            return Voice.Pick(seed + "|vkepth", first + " kept the peace his week out. The sheriff draws a line through your name in the book.",
                                                  first + "'s week is out and he never raised a fist. Word is he tells folks who stood for him.");
        }

        public static string KeptNews(string name, string seed)
        {
            return Voice.Pick(seed + "|vkept", name + " kept the peace his week out, on a stranger's word.",
                                                Grammar.UpperFirst(Span) + " bound over and not a fist raised: " + name + ". The stranger who stood for him was right.");
        }

        /// Every line through the period words (mindtest vouch).
        public static IEnumerable<string> AllLines(string seed)
        {
            foreach (string s in Thanks) yield return s;
            yield return BoundHint("Amos");
            yield return StoodNews("Amos Pike", seed);
            foreach (string how in new[] { "seen", "law", "word" })
            {
                yield return BrokeHint("Amos", how, true, seed);
                yield return BrokeHint("Amos", how, false, seed);
            }
            yield return BrokeNews("Amos Pike", seed);
            yield return KeptHint("Amos", seed);
            yield return KeptNews("Amos Pike", seed);
            yield return Row("Amos", null).Label;
        }
    }
}
