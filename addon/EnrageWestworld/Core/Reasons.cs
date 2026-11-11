// Why two people near the player would have something to do with each other right now, from who they are: what is
// between them already (a SceneTie, and how far its story has gone), the worry each is living through (InnerLife),
// their temperament, whether they're waking, and what the player did in their town. The strongest reason picks the
// scene and who starts it; with no reason there is no scene (the game side keeps a rare chance meeting for a town
// where nobody has a history yet). A pair's story moves on each time they meet instead of repeating: a quarrel goes
// on, then comes to blows between two hot-tempered men; courting goes on until one of them asks.
// The trouble the game shows between people comes from here too (Scenes.Trouble.cs): a grudge between two men, or a
// hothead with drink in him, can end in one smashing the other's face into the nearest table or bar ("rough"; the game
// side checks there's a surface to hand, else it stays a fistfight or words); someone short of money or light of
// finger robs a well-off or drunk mark ("lift" in passing, "pickpocket" when the mark catches it at once); a drunk in a
// saloon at night calls a man out ("drunk"). Each from who they are, never just from who is standing closest.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One person near the player, as the reasons see them.
    public sealed class Someone
    {
        public string Id, Name, Temper;
        public bool Woman;
        public Person P;          // for the worry they're living through
        public int WakeStage;
        public bool WellOff;      // dressed like money: a gentleman, a lady (a mark worth robbing)
        public bool Drunk;        // in his cups: slumped at the bar, drinking himself silly (a mark, or a man who calls people out)
        public string Hurt;       // just had it done to them (the last hour and a half): "rough" (a face in the bar) or "robbed", else null
        public bool Taken;        // spoken for: a sweetheart, someone they're courting, an affair of enRAGE's
        public string Mood;       // their day: "sore", "glad" or null (InnerLife.MoodOf)
        public string Grief;      // the first name of a friend whose death they've had word of in the last three days, else null
        public long GriefAt;      // the game minute that word reached them
        public string GriefId;    // the dead friend's id (null when unknown)
        public Func<string, bool> HeardOf;   // whether word of this death (by the dead one's id) has reached them; null: not asked
        public bool Known;        // known lately for a face in the bar or a hand in a coat
        public bool Bent;         // of the criminal class: a theft to their name this month (Core/Criminals.cs; SIXDAY item 16), set by the game side
        public string Broke;      // a breakdown's habit this week (Core/StressLoad.cs; FIVEDAY item 17): "temper" snaps as if strained twice over
        public bool Greenhorn;    // off the train in the last two weeks (a resident's NewAt; FOURTEENHOUR item 16), set by the game side
        public List<Opinions.Held> Views;   // what they think of others they know (Core/Opinions.cs; FOURTEENHOUR item 23), set by the game side; null: none
        public string Courting, CourtingName;   // the woman he's courting, and her name (Core/Rivals.cs; FOURTEENHOUR item 19), set by the game side
        public string War;        // the war he was in: "blue", "grey", "cuba", "islands" (Core/OldSoldiers.cs; FOURTEENHOUR item 17), set by the game side
        public string Shamed;     // slighted, had words or beaten in the last four hours and not answered it ("slighted", "had words", "beaten"), else null (NIGHT3: displaced aggression)
    }

    public sealed class Reason
    {
        public string Kind;       // the scene to play: argue, brawl, flirt, propose, debt, smalltalk, gossip, hosts
        public int Strength;      // the strongest reason near the player wins
        public string Why;        // for the log and the F7 dump
        public bool BFirst;       // the second person starts it (A in the scene)
        public string Fallback;   // the scene instead, if the game side can't play this one (no surface by him for "rough")
        public string Leaned;     // the mind's lean on it, for the log (Reasons.MoodLean; with MindMeetings), else null
        public string Matter;     // what the quarrel is over, when the reason knows (drink, money, honour, woman, land); null: the game side works it out (Scenes.Ladder.cs)
        public string Over;       // what it's over in words, when the matter's "nothing" but the reason knows better ("snap:<worry>": the doer's own bad week; FOURTEENHOUR item 1); never the ladder's odds
    }

    public static partial class Reasons
    {
        /// Game minutes before the same two people have another scene (about four real minutes).
        public const long PairRest = 120;
        public const long HostileRest = 45;   // a quarrel picks up again sooner than a friendship does (Lex: fights were too rare to see)

        /// Everything the place adds: is it a saloon, is it night, are there rumours to pass on, did the player do
        /// something in this town lately that people would talk about.
        public sealed class Place
        {
            public bool Saloon, Night, Rumours, DeedsHere;
            public bool Rain, Indoors;   // raining (or within the hour after), and under a roof (FOURTEENHOUR item 26)
            public int Hour = 12;   // the clock's hour (the temperance door's dusk: FOURTEENHOUR item 25); noon in the old harnesses
            public bool Courting;   // [Romance] NpcFlirting: people make eyes at each other
            public bool SameSex;    // [Romance] SameSexRomance
            public float Trouble = 1f;   // [Scenes] TroubleWeight: every chance of blows or a quarrel scaled by it (Lex, 2026-10-28)
            public bool WomenFight;      // [Scenes] WomenInFights: a woman may come to blows too, half as often a woman (Lex, 2026-10-29); off (and in the old harnesses), two men only
            public bool MindDecides;   // the quarrel ladder plays the quarrel (MindLive, QuarrelLadder): MayFight leaves blows to it (Lex, 2026-10-30)
            public bool Strain;        // [Mind] MindTempers: what weighs on someone at once adds up (Strains; NIGHT3)
            public bool Variety = true;   // [Scenes] ReasonVariety: x:snap starts at 42, not 45, so gossip, a lift, a drunk and courting win turns (NIGHT4 fix 5; the old harnesses: on, as the game comes)
            public bool TimesTalk;       // [Mind] TimesTalk (with MindTalks and HistoricalAccuracy): any two may talk over the times (Run 3, item 2)
            public int TimesShare = 100; // [Mind] TimesTalkShare: its chance, percent of as built
            public bool MindTalks;       // [Mind] MindTalks: people talk to each other from what's on their minds (MindTalk.cs); the reason of last resort among people who know each other, ahead of small talk
        }

        /// For each woman in a fight, this percent of a man's chance (Lex, 2026-10-29: half as likely, until the mind decides it).
        public const int WomanFightPercent = 50;
        public static int StrainChance = 40;   // [Mind] StrainChance: stress that builds snaps at someone who won't hit back this percent of the time (but a coward, always)

        /// Whether these two may come to blows at all: two men always, as before; with a woman in it (WomenFight on) on a
        /// seeded roll of WomanFightPercent for each woman, by the pair and what it's over, so the same quarrel gives the same
        /// answer. The rough move and the drunk's challenge stay men's (their clips were only ever seen on men).
        public static bool MayFight(Someone a, Someone b, Place where, string key)
        {
            if (!a.Woman && !b.Woman)
                return true;
            if (where == null || !where.WomenFight)
                return false;
            if (where.MindDecides)
                return true;   // the quarrel ladder decides whether it comes to blows, by the two of them (a man's hand against a woman at a tenth: Escalation)
            int pct = (a.Woman ? WomanFightPercent : 100) * (b.Woman ? WomanFightPercent : 100) / 100;
            return Voice.Chance(PairKey(a, b) + key + "|womanfight", pct);
        }

        /// A percent chance scaled by the trouble weight, 0-100.
        public static int Weighted(int pct, float weight)
        {
            return Math.Max(0, Math.Min(100, (int)Math.Round(pct * weight)));
        }

        /// The strongest reason between these two, either way round; null if they have none (or had their scene
        /// together too lately).
        public static Reason Between(Someone a, Someone b, SceneTie tie, long gameDay, long now, Place where, WorryLink link = null)
        {
            if (a == null || b == null || a.Id == b.Id)
                return null;
            bool mixed = a.Woman != b.Woman, twoMen = !a.Woman && !b.Woman;
            if (tie != null)
            {
                long rest = tie.Kind == "brawl" ? 1440 : tie.Warmth < 0 ? HostileRest : PairRest;   // after a fight, a day before the next thing between them
                if (now - tie.At >= 0 && now - tie.At < rest)
                    return null;   // they've just had words, or a walk together
                if (StepBetween.PutOff(tie, now))
                    return null;   // a stranger stepped between them and both let it be: it waits (DEPTH item 7)
                // sore sides of a big brawl, and nothing between them since: strangers still as far as what starts between them goes,
                // and the join reads the warmth (a stake in the next fight among them). As a quarrel of its own it made a fight begin a
                // tenth more often in the week after, against the plan's rule on how often fights start (the run's-end measure;
                // FOURTEENHOUR item 12)
                if (tie.Sore && tie.Count <= 1)
                    tie = null;
            }
            // one is the real person at the other end of the other's worry: the strongest reason there is (Links.cs)
            if (link != null && Links.Linkable(link.Kind))
            {
                string[] what = { "it begins", "it's worse", "it's at the worst", "it's ended" };
                int stage = InnerLife.StageFor(link.Id, gameDay);
                if (!(stage == 3 && link.Ended))   // its ending has been had out between them already
                    return new Reason { Kind = "x:" + link.Kind, Strength = 85, BFirst = Links.IsOf(link, b.Id),
                                        Why = link.Name + "'s " + link.Kind + " with " + link.OtherName + " (" + what[stage] + ")" };
            }
            // two men courting the same woman (FOURTEENHOUR item 19): whatever they are to each other, that's what's between them now
            Reason rivals = Rivals.For(a, b, gameDay, where.Saloon);
            if (rivals != null)
                return rivals;
            if (tie != null)
            {
                // sweethearts at odds since they last met: the cooler head comes to make it up, with a small gift, where
                // the street can see it (Scenes.Mend)
                if (Ties.Cooled(tie) && (mixed || where.SameSex) && where.Courting)
                {
                    if (Voice.Hash(PairKey(a, b) + tie.Count + "mend") % 100 >= Scenes.MendChance)
                        return null;   // at odds, and neither ready to say sorry yet: they keep apart for now
                    bool bBrings = Hot(a) && !Hot(b);
                    return new Reason { Kind = "mend", Strength = 64, BFirst = bBrings,
                                        Why = (bBrings ? b.Name : a.Name) + " comes to make it up with " + (bBrings ? a.Name : b.Name) + ", with a small gift" };
                }
                // the one it was done to (a face in the bar, a hand in his coat) sees the one who did it: the timid
                // keep clear of him from then on; the rest have it out, as with any other quarrel (below)
                if (tie.Warmth < 0 && (tie.Kind == "rough" || tie.Kind == "pickpocket" || tie.Kind == "avoid") && tie.Doer != null)
                {
                    Someone victim = tie.Doer == a.Id ? b : tie.Doer == b.Id ? a : null;
                    Someone doer = victim == a ? b : a;
                    // fear keeps him clear too (item 23), but never a proud man or a hothead: he has it out, whatever he feels
                    bool afraid = victim != null && Opinions.Act && victim.Temper != "Proud" && victim.Temper != "Hothead" &&
                                  Opinions.View(tie, victim.Id, victim.Temper, victim.WellOff, doer.WellOff, doer.Drunk) == "fear";
                    if (victim != null && (Exchanges.Manner(victim, false) == "kind" || afraid))
                        return new Reason { Kind = "avoid", Strength = 50, BFirst = tie.Doer == b.Id,
                                            Why = victim.Name + " keeps clear of " + (victim == a ? b.Name : a.Name) + " since that business" };
                }
                if (tie.Warmth < 0)
                {
                    bool hot = Hot(a) || Hot(b);
                    // the first quarrel can come to blows too, between two hot men or with drink in one of them
                    bool blows = MayFight(a, b, where, "feud" + tie.Count) && ((tie.Stage >= 1 && (hot || tie.Warmth <= -3 || tie.Stage >= 2)) || (Hot(a) && Hot(b)) || (hot && (a.Drunk || b.Drunk)));
                    // blows between a hothead (or a proud or a cold man) and the man he hates: sometimes he grabs him and
                    // smashes his face into whatever is at hand instead of trading punches
                    if (blows && twoMen && (Rough(a.Temper) || Rough(b.Temper)) && Voice.Hash(a.Id + b.Id + tie.Count + "rough") % 100 < RoughChance)
                        return new Reason { Kind = "rough", Fallback = "brawl", Strength = 72 - 10 * tie.Warmth, BFirst = !Rough(a.Temper),
                                            Why = "their quarrel comes to blows, and " + (Rough(a.Temper) ? a.Name : b.Name) + " has the temper to do it rough" };
                    // the first time words; then it picks up again (and may be made up: Exchanges); two hot men, blows
                    return new Reason { Kind = blows ? "brawl" : tie.Stage >= 1 || tie.Kind != "argue" ? "x:quarrel" : "argue", Fallback = blows ? "argue" : null, Strength = 70 - 10 * tie.Warmth,
                                        Why = blows ? "their quarrel comes to blows" : "their quarrel picks up again" };
                }
                // a friend of someone grieving (word of a friend's death in the last three days) offers sympathy, the first
                // time they meet after the news reached them (Scenes.Condole; the grieving one is A)
                if (Ties.Friends(tie) && (a.Grief != null || b.Grief != null))
                {
                    Someone grieving = a.Grief != null ? a : b, friend = grieving == a ? b : a;
                    // the friend must have had the word too: nobody offers sympathy for a death they haven't heard of
                    bool friendKnows = grieving.GriefId == null || friend.HeardOf == null || friend.HeardOf(grieving.GriefId);
                    if (tie.At < grieving.GriefAt && friendKnows)
                        return new Reason { Kind = "condole", Strength = 62, BFirst = grieving == b, Why = friend.Name + " offers " + grieving.Name + " sympathy for " + grieving.Grief };
                }
                // a friend (or sweetheart) of someone who has just been roughed up or robbed goes to them: a small kindness
                if (Ties.Friends(tie) && (a.Hurt != null || b.Hurt != null))
                {
                    Someone hurt = a.Hurt != null ? a : b, friend = hurt == a ? b : a;
                    return new Reason { Kind = "tend", Strength = 58, BFirst = friend == b, Why = friend.Name + " goes to " + hurt.Name + ", who was just " + (hurt.Hurt == "rough" ? "roughed up" : "robbed") };
                }
                if (tie.Warmth > 0 && (mixed || where.SameSex) && Ties.Sweethearts(tie))
                {
                    bool ask = tie.Warmth >= 2 && tie.Stage >= 2 && !where.Night;
                    // now and then sweethearts have words: a hot head, or a sore day, in one of them (Scenes.Tiff)
                    bool aCross = Hot(a) || a.Mood == "sore", bCross = Hot(b) || b.Mood == "sore";
                    if (!ask && where.Courting && Scenes.TiffNow(aCross || bCross, tie.Stage, PairKey(a, b) + tie.Count))
                        return new Reason { Kind = "tiff", Strength = 60, BFirst = !aCross,
                                            Why = "sweethearts, and " + (aCross ? a.Name : b.Name) + " has a grievance" };
                    return new Reason { Kind = ask ? "propose" : tie.Stage == 0 ? "flirt" : "x:sweet", Strength = 60 + 10 * tie.Warmth, BFirst = ask && a.Woman && !b.Woman,   // the man asks (Scenes.Propose kneels the man)
                                        Why = ask ? "they've been courting long enough for one to ask" : "they're sweet on each other" };
                }
                Reason timesTie = tie.Warmth >= 0 ? TimesReason(a, b, gameDay, where) : null;
                if (timesTie != null)
                    return timesTie;   // people who know each other talk over the times too, before the plain catching up (Run 3, item 2)
                // a friend tells a friend what he thinks of someone not there (FOURTEENHOUR item 23): the town's talk of its own, bent
                // by the teller's view
                Reason behind = tie.Warmth > 0 && Opinions.Act ? (TalkOf(a, b, gameDay, false) ?? TalkOf(b, a, gameDay, true)) : null;
                if (behind != null)
                    return behind;
                if (tie.Warmth > 0)
                    return new Reason { Kind = where.MindTalks ? "mindtalk" : "smalltalk", Strength = 35, Why = "they're on good terms" + (where.MindTalks ? ", and " + a.Name + " has something on their mind" : "") };
                return new Reason { Kind = where.MindTalks ? "mindtalk" : where.Rumours ? "gossip" : "smalltalk", Strength = 30, Why = "they know each other" };
            }
            Reason ab = Own(a, b, gameDay, where, mixed, twoMen), ba = Own(b, a, gameDay, where, mixed, twoMen);
            if (ba != null && (ab == null || ba.Strength > ab.Strength))
            {
                ba.BFirst = !ba.BFirst;   // b's own reason: b's side of it comes first
                return ba;
            }
            return ab;
        }

        /// x tells y what he thinks of someone else he knows (his strongest view to pass on), on some pair-days; null for none.
        public static Opinions.Held TalkOfWhom(Someone x, Someone y)
        {
            if (x == null || y == null || x.Views == null)
                return null;
            Opinions.Held best = null;
            foreach (Opinions.Held h in x.Views)
                if (h.Id != y.Id && h.Id != x.Id && (best == null || Opinions.Weight(h.View) > Opinions.Weight(best.View)))
                    best = h;
            return best;
        }

        private static Reason TalkOf(Someone x, Someone y, long gameDay, bool bFirst)
        {
            Opinions.Held z = TalkOfWhom(x, y);
            if (z == null || !Voice.Chance(PairKey(x, y) + gameDay + "talkof", Opinions.TalkOfPercent))
                return null;
            string wk; int st; bool good;
            if (x.P != null && InnerLife.WorryNow(x.P, gameDay, out wk, out st, out good) && st >= 1)
                return null;   // a worry on him: his friend hears that, not his view of a third (talkof outranked the worry talk) (the review of items 22-26)
            return new Reason { Kind = "talkof", Strength = Opinions.TalkOfStrength, BFirst = bFirst, Why = x.Name + " tells " + y.Name + " what they think of " + z.Name + " (" + z.View + ")" };
        }

        /// What weighs on someone at once (Own's strain): a worry at its worst or gone wrong today, a slight or a beating
        /// not answered, a friend's death, a face in the bar or a purse gone lately, a sore day. In words, for the log.
        public static List<string> Strains(Someone x, long gameDay)
        {
            var s = new List<string>();
            if (x == null)
                return s;
            string kind;
            int stage;
            bool good;
            if (x.P != null && InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) && (stage == 2 || (stage == 3 && !good)))
                s.Add(stage == 2 ? kind + " at its worst" : kind + " gone wrong");
            if (x.Shamed != null)
                s.Add(x.Shamed + " earlier");
            if (x.Grief != null)
                s.Add("grieving " + x.Grief);
            if (x.Hurt != null && x.Shamed == null)
                s.Add(x.Hurt == "rough" ? "roughed up" : "robbed");   // the same rough as the shame: counted once (the review)
            return s;   // (a sore day is these added up already: not counted again)
        }

        /// What is played of a reason the game side may refuse (`cant`: why a kind can't be played with these two now, or null):
        /// the reason's own kind, else its fallback, else words. Blows always fall back to words (a brawl between two where
        /// one is sat or leaning: they have it out in words, the matter kept), and so does a rough move that falls back to a
        /// brawl that can't be played either. Null when nothing can be played; `note` says what was done, for the log.
        public static string Settle(string kind, string fallback, Func<string, string> cant, out string note)
        {
            note = null;
            string why = cant != null ? cant(kind) : null;
            if (why == null)
                return kind;
            string next = fallback ?? (kind == "brawl" ? "argue" : null);
            note = kind + " can't be played (" + why + ")";
            for (int i = 0; i < 2 && next != null; i++)
            {
                string why2 = cant(next);
                if (why2 == null)
                {
                    note += next == "argue" && (kind == "brawl" || kind == "rough") ? ", so played as words" : ", so " + next;
                    return next;
                }
                note += "; nor " + next + " (" + why2 + ")";
                next = next == "brawl" ? "argue" : null;
            }
            return null;
        }

        /// The two ids in a fixed order, so a roll between them is the same whichever comes first in a scan.
        private static string PairKey(Someone a, Someone b)
        {
            return string.CompareOrdinal(a.Id, b.Id) < 0 ? a.Id + "|" + b.Id : b.Id + "|" + a.Id;
        }

        /// A reason that comes from one person's own life (x), with the other (y) as whoever is there.
        private static Reason Own(Someone x, Someone y, long gameDay, Place where, bool mixed, bool twoMen)
        {
            string kind;
            int stage;
            bool good;
            bool worry = InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good);
            // every reason that fits, and the strongest wins (the first of equals, so the old order still decides ties)
            var all = new List<Reason>();
            // a debt is called in, and courting done, only with the real person at the other end (a link, above): never
            // with whoever happens to be standing there, which would contradict their own thoughts. At the worst of it
            // (or any other worry): spoken to, they snap, and say what's wrong (Exchanges)
            if (worry && stage == 2)
            {
                bool hot = Hot(x);
                // a hot man at the worst of it snaps at a man who won't take it: it comes to blows
                if (hot && Hot(y) && MayFight(x, y, where, "snap" + gameDay) && Voice.Chance(x.Id + gameDay + "snapblows", Weighted(35, where.Trouble)))   // was 20 (Lex, 2026-10-28: brawls often)
                    all.Add(new Reason { Kind = "brawl", Fallback = "argue", Strength = 56, Matter = "nothing", Over = "snap:" + kind,   // the matter stays "nothing" for the ladder's odds; the words say it's his own bad week (FOURTEENHOUR item 1)
                                         Why = x.Name + " is at the worst of it (" + kind + ") and snaps at " + y.Name + ", who won't take it" });
                else
                    all.Add(new Reason { Kind = "x:snap", Strength = hot ? 55 : where.Variety ? 42 : 45, Why = x.Name + " is at the worst of it (" + kind + ")" });
            }
            // displaced aggression (NIGHT3, the psychologist's lens): shamed earlier and unable to answer it there, a man takes it
            // out on someone who won't hit back; the one who shamed him isn't the one who pays
            if (x.Shamed != null && x.Id != y.Id && !Hot(y) && (y.Temper == "Coward" || Voice.Chance(x.Id + y.Id + gameDay + "kick", 40)))
                all.Add(new Reason { Kind = "x:snap", Strength = Hot(x) ? 52 : 46, Why = x.Name + " was " + x.Shamed + " earlier, and takes it out on " + y.Name });
            // stress that builds (NIGHT3, the psychologist's lens): one trouble is borne; two or more at once and they snap at
            // whoever is there and won't hit back, the more the harder (a snap they'd have anyway comes harder)
            // (the timid and the loyal withdraw rather than snap; the anger picks a safe target: not someone well-off, and a
            // man doesn't take it out on a strange woman; and not at everyone: the shamed rule's roll)
            if (where.Strain && x.Id != y.Id && !Hot(y) && x.Temper != "Coward" && x.Temper != "Loyal" && !y.WellOff && (x.Woman || !y.Woman) &&
                (y.Temper == "Coward" || Voice.Chance(x.Id + y.Id + gameDay + "strain", StrainChance)))
            {
                List<string> strains = Strains(x, gameDay);
                if (x.Broke == StressLoad.Temper)
                {
                    strains.Add("a week too heavy to carry");   // broken by the week's load, the temper way: as strained as two troubles at once (FIVEDAY item 17)
                    if (strains.Count < 2)
                        strains.Add("no patience left");
                }
                if (strains.Count >= 2)
                {
                    int bump = 4 * (strains.Count - 1);
                    Reason snap = null;
                    foreach (Reason r in all)
                        if (r.Kind == "x:snap")
                            snap = r;
                    if (snap != null)
                    {
                        snap.Strength = Math.Min(64, Math.Max(snap.Strength, 45) + bump);   // (the lower start of ReasonVariety is for a worry alone: too much at once comes as hard as before)
                        snap.Why += "; and too much at once (" + string.Join(", ", strains) + ")";
                    }
                    else
                        all.Add(new Reason { Kind = "x:snap", Strength = Math.Min(60, 46 + bump), Why = x.Name + " has too much at once (" + string.Join(", ", strains) + ") and snaps at " + y.Name });
                }
            }
            if (x.WakeStage >= 2 && y.WakeStage >= 1)
                all.Add(new Reason { Kind = "hosts", Strength = 45, Why = "both are waking" });
            if (worry && stage == 3 && !(good && kind != "debt" && InnerLife.PaidOn(x.Id, gameDay)))   // mended today: the doctor, the dentist, the ticket are still to come
                all.Add(new Reason { Kind = good ? "x:goodnews" : "x:badnews", Strength = 42, Why = x.Name + "'s " + kind + " ended " + (good ? "well" : "badly") + ", and they say so" });
            if (where.DeedsHere && where.Rumours)
                all.Add(new Reason { Kind = "gossip", Strength = 40, Why = "the town is talking about what the stranger did" });
            // a thief and a mark: someone short of money, or light of finger by nature, near someone worth robbing;
            // driven by money trouble at its worst it outweighs even a temper
            string need = Thief(x, gameDay);
            if (need != null && (y.WellOff || y.Drunk) && x.Id != y.Id)
            {
                bool caught = !x.Woman && !y.Drunk && Voice.Hash(x.Id + y.Id + gameDay + "caught") % 100 < (Alert(y.Temper) ? 55 : 30) - (x.Temper == "Drifter" ? 20 : 0);
                all.Add(new Reason { Kind = caught ? "pickpocket" : "lift", Fallback = caught ? "lift" : null, Strength = need == Desperate && worry && stage == 2 ? 47 : need == Criminals.KeepsToIt ? 46 : 43,
                                     Why = x.Name + " " + need + ", and " + y.Name + (y.Drunk ? " is in his cups" : " looks like money") + (caught ? "; the mark feels it" : "") });
            }
            // the same want in an honest person: they ask instead of taking (the well-off, never the drunk)
            string asks = Beggar(x, gameDay);
            if (asks != null && y.WellOff && !y.Drunk && x.Id != y.Id && need == null)
                all.Add(new Reason { Kind = "beg", Strength = 41, Why = x.Name + " " + asks + ", and asks " + y.Name + " for a coin" });
            // a drunk in a saloon at night calls a man out, and falls down before a blow is struck
            if (x.Drunk && twoMen && where.Saloon && where.Night && x.Temper != "Coward")
                all.Add(new Reason { Kind = "drunk", Strength = 44, Why = x.Name + " is drunk and spoiling for a fight" });
            // out in the street at night, a man in drink picks on a man going by (FOURTEENHOUR reserve R5): the game side finds his friend in
            // drink beside him for the song and the walk home; alone, it's the saloon's drunk who falls on his face
            if (twoMen && Rowdy.Picks(x, y, where.Night, !where.Saloon && !where.Indoors, false, gameDay))
                all.Add(new Reason { Kind = Rowdy.Kind, Strength = Rowdy.Strength, Why = x.Name + " is drunk and loud in the street, and picks on " + y.Name });
            // drink and temper in a saloon, any hour (at night only until Lex asked for saloon fights by day too, 2026-10-28):
            // two men, one of them hot, one of them drunk, come to blows (a coward only gets words); now and then the hot
            // one does it rough
            if (where.Saloon && Hot(x) && (x.Drunk || y.Drunk) && MayFight(x, y, where, "drink" + gameDay) && Voice.Chance(x.Id + gameDay + "drinktemper", Weighted(DrinkTemperChance, where.Trouble)))
            {
                if (y.Temper == "Coward")
                    all.Add(new Reason { Kind = "argue", Strength = 46, Matter = "drink", Why = x.Name + " has drink and temper in him, and " + y.Name + " won't stand up to it" });
                else if (twoMen && Voice.Hash(x.Id + y.Id + gameDay + "drough") % 100 < RoughChance)
                    all.Add(new Reason { Kind = "rough", Fallback = "brawl", Strength = 57, Matter = "drink", Why = x.Name + " has drink and temper in him, and does it rough" });
                else
                    all.Add(new Reason { Kind = "brawl", Fallback = "argue", Strength = 56, Matter = "drink", Why = x.Name + " has drink and temper in him, and " + y.Name + " answers it" });
            }
            else if (x.Temper == "Hothead" && twoMen && where.Saloon && where.Trouble > 0f)
            {
                // now and then a hothead with drink in him doesn't stop at words with whoever is in his way; at night he
                // wins the turn over most things (Lex: fights were too rare to see)
                int night = where.Night ? 14 : 0;
                if (Voice.Hash(x.Id + gameDay + "srough") % 100 < Weighted(StrangerRoughChance, where.Trouble))
                    all.Add(new Reason { Kind = "rough", Fallback = "argue", Strength = 32 + night, Matter = "drink", Why = x.Name + " is a hothead with drink in him, and " + y.Name + " is in his way" });
                else
                    all.Add(new Reason { Kind = "argue", Strength = 30 + night, Matter = "drink", Why = x.Name + " is a hothead with drink in him" });
            }
            // two hot-blooded men in each other's way come to blows now and then, by day or night (Lex, 2026-10-28: brawls
            // weren't happening; a reason always came first, so the chance meeting's brawl seldom got a turn). A roll per
            // pair and day, until the minds choose it themselves; strong enough to come before a snap or gossip
            // Two hot men (or a hot and a cold one) at the full chance; one hot man at half; two ordinary men at a quarter
            // (Lex, 2026-10-28, at the highest weight in Valentine: no fights, since two hot men together are rare in a
            // thin town and the weight had no pair to work on); never a coward
            if (x.Temper != "Coward" && y.Temper != "Coward" && MayFight(x, y, where, "hotblood" + gameDay))
            {
                bool hx = Hot(x), hy = Hot(y);
                int full = where.Saloon ? HotBloodSaloonChance : HotBloodStreetChance;
                int pct = hx && (hy || y.Temper == "Cold") || hy && x.Temper == "Cold" ? full : hx || hy ? full / 2 : full / 4;
                if (Voice.Chance(PairKey(x, y) + gameDay + "hotblood", Weighted(pct, where.Trouble)))
                    all.Add(new Reason { Kind = "brawl", Fallback = "argue", Strength = where.Saloon ? 50 : 47,
                                         Why = hx && hy ? x.Name + " and " + y.Name + " are both hot-blooded, and neither will give way"
                                             : hx || hy ? (hx ? x.Name : y.Name) + " has a temper, and " + (hx ? y.Name : x.Name) + " won't be pushed"
                                             : x.Name + " and " + y.Name + " rub each other the wrong way, and neither will give way" });
            }
            // the street's own business (FOURTEENHOUR item 14; Core/StreetMore.cs): a horse to sell, a tooth at its worst, the way
            // asked; under the fights' strengths (ties go to them), over a mild snap and the talk
            // in the rain out of doors the street's errands wait (FOURTEENHOUR item 26): its own business, the snub, the temperance door
            bool wetOut = where.Rain && !where.Indoors && RainShelter.On;
            Reason more = wetOut ? null : StreetMore.For(x, y, gameDay, where.Saloon, where.Night, twoMen);
            if (more != null)
                all.Add(more);
            Reason green = Greenhorn.For(x, y, gameDay, where.Saloon, where.Night);   // a regular ribs the greenhorn (FOURTEENHOUR item 16)
            if (green != null)
                all.Add(green);
            Reason soldiers = OldSoldiers.For(x, y, gameDay, where.Saloon, where.Night);   // old soldiers of either side, or a young man home from the war (item 17)
            if (soldiers != null)
                all.Add(soldiers);
            Reason snub = wetOut ? null : Snub.For(x, y, gameDay, where.Saloon, where.Night);   // a gentleman snubs a working man in the street (item 18)
            string yKind;
            int yStage;
            bool yGood;
            bool yDrink = y.P != null && InnerLife.WorryNow(y.P, gameDay, out yKind, out yStage, out yGood) && yKind == "drink";
            Reason door = wetOut ? null : Temperance.For(x, y, gameDay, where.Saloon, where.Hour, yDrink);   // the pledge at the saloon door at dusk (item 25)
            Reason shelter = RainShelter.For(x, y, gameDay, where.Rain, where.Indoors);   // strangers under one roof in the rain (item 26)
            if (shelter != null)
                all.Add(shelter);
            if (door != null)
                all.Add(door);
            if (snub != null)
                all.Add(snub);
            // someone free and taken with the one beside them makes eyes at them; the answer is the other's own
            Reason court = Courtship(x, y, gameDay, where);
            if (court != null)
                all.Add(court);
            // what's on their mind, said to whoever is there (MindTalk.cs): a worry as it begins or grows, the times, the stranger, a
            // plan, the town's talk, a need, the day. Below the trouble (a snap, a lift, a beggar) and gossip's 40, above small talk;
            // the solitary bring it up half as often; a roll a pair and a day keeps the same two from talking it over every hour
            if (where.MindTalks && x.Id != y.Id && Voice.Chance(PairKey(x, y) + gameDay + "mindtalk", x.Temper == "Cold" || x.Temper == "Greedy" ? 35 : 70))
                all.Add(new Reason { Kind = "mindtalk", Strength = worry && stage <= 1 ? 39 : 37, Why = x.Name + " has something on their mind, and " + y.Name + " is there" });
            // the times stand aside where a theft, a beggar or someone's own news is to be had (the review: at 44 they beat a lift's
            // 43, the news's 42 and a beggar's 41 on every pair-day they came up)
            Reason times = all.Exists(r => r.Kind == "lift" || r.Kind == "pickpocket" || r.Kind == "beg" || r.Kind == "x:goodnews" || r.Kind == "x:badnews") ? null : TimesReason(x, y, gameDay, where);
            if (times != null)
                all.Add(times);
            Reason best = null;
            foreach (Reason r in all)
                if (best == null || r.Strength > best.Strength)
                    best = r;
            return best;
        }

        // ---- the times talked over (Run 3, item 2; Lex, 2026-11-04: "Ideally the player will just hear lots of talk relating to
        // the time of living in America midwest 1899", who holds office among it, through the talks between people and the
        // street's scenes, not Talk). Any two who stop together may talk over the news of the day: an almanac story of the
        // fortnight, or a topic of the year (Core/Times.cs: the election, McKinley and Hobart, the war, the wheat and the bank,
        // the railroad, the catalogue, the dry vote, the old soldiers...). Before it, talk of the times was one scene in fifty
        // (mindtest TimesTalkRateTest: 2%, about half a talk an hour; Lex's logs: 1.3 an hour). Its strength sits above gossip
        // (40), the worry said aloud (42) and a lift (43), below a fight (47 and up): people talk the news over more than they
        // quarrel, and the street's variety penalty (6 a time among its last four) keeps it from every scene in a row. Why it
        // fits the period: the paper came weekly, and the store, the saloon and the depot were where it was read aloud and argued
        // over; strangers talked news as readily as friends, and it was the safest talk there was between people who didn't
        // know each other's business.
        public const int TimesStrength = 41, TimesChance = 80, TimesNightChance = 30;

        /// Two who may talk over the times today (a roll a pair and a day; by night in the street, less), or null.
        private static Reason TimesReason(Someone x, Someone y, long gameDay, Place where)
        {
            if (!where.TimesTalk || where.TimesShare <= 0 || x == null || y == null || x.Id == y.Id)
                return null;
            int pct = (where.Night && !where.Saloon ? TimesNightChance : TimesChance) * where.TimesShare / 100;
            if (!Voice.Chance(PairKey(x, y) + gameDay + "times", Math.Min(100, pct)))
                return null;
            return new Reason { Kind = MindTalk.TimesKind, Strength = TimesStrength, Why = x.Name + " and " + y.Name + " talk over the times" };
        }

        /// Percent of days a free person, taken with someone beside them, tries their luck (from both ids and the day).
        public const int CourtChance = 12;

        /// [Scenes] CourtFolkAges (NIGHT4 item 5): enRAGE's townsfolk keep no birth year, so Romance.AgeOf is -1 for them and
        /// no courtship with one of them was ever made; with this on, Courtship alone reads an age guessed from who they
        /// are (Romance.AgeOrGuess, the guess the links and the worries already use). Children and the player's romance
        /// never read a guess.
        public static bool CourtGuessAges = true;

        /// Why Courtship gave nothing, counted (F7 TEST reasons:): not a courting place, an age unknown, under age, too many
        /// years apart, not drawn, spoken for, not today, not taken with them; and how many it made.
        public static readonly int[] CourtWhyNot = new int[9];
        public const int CourtNotPlace = 0, CourtAgeUnknown = 1, CourtUnder = 2, CourtApart = 3, CourtNotDrawn = 4, CourtSpokenFor = 5, CourtNotToday = 6, CourtNotTaken = 7, CourtMade = 8;

        public static string CourtStatus()
        {
            int[] c = CourtWhyNot;
            return "courtships " + c[CourtMade] + "; passed over: not a courting place " + c[CourtNotPlace] + ", age unknown " + c[CourtAgeUnknown] + ", under age " + c[CourtUnder] +
                   ", apart " + c[CourtApart] + ", not drawn " + c[CourtNotDrawn] + ", spoken for " + c[CourtSpokenFor] + ", not today " + c[CourtNotToday] + ", not taken " + c[CourtNotTaken] +
                   (CourtGuessAges ? " (townsfolk ages guessed)" : " (townsfolk ages unknown: CourtFolkAges off)");
        }

        /// x, free, drawn to y and not against them (x's own answer, judged as the player is: Interest, a yes or a maybe:
        /// it takes some nerve, but a maybe is enough to try), tries their luck; y answers by what y makes of x. Kind "court:answer:why". Never anyone spoken for or under age, nor more
        /// than 15 years apart; only between people with no tie yet (Between calls Own only then).
        public static Reason Courtship(Someone x, Someone y, long gameDay, Place where)
        {
            if (where == null || !where.Courting)
            {
                CourtWhyNot[CourtNotPlace]++;
                return null;
            }
            if (x == null || y == null || x.Id == y.Id || x.P == null || y.P == null)
                return null;
            if (x.Taken)
            {
                CourtWhyNot[CourtSpokenFor]++;
                return null;
            }
            int ax = CourtGuessAges ? Romance.AgeOrGuess(x.P) : Romance.AgeOf(x.P), ay = CourtGuessAges ? Romance.AgeOrGuess(y.P) : Romance.AgeOf(y.P);
            if (ax < 0 || ay < 0)
            {
                CourtWhyNot[CourtAgeUnknown]++;
                return null;
            }
            if (ax < 18 || ay < 18)
            {
                CourtWhyNot[CourtUnder]++;
                return null;
            }
            if (System.Math.Abs(ax - ay) > 15)
            {
                CourtWhyNot[CourtApart]++;
                return null;
            }
            if (!Romance.DrawnTo(x.Id, x.Woman, y.Woman, where.SameSex))
            {
                CourtWhyNot[CourtNotDrawn]++;
                return null;
            }
            if (Voice.Hash(x.Id + "|" + y.Id + "|" + gameDay + "|court") % 100 >= CourtChance)
            {
                CourtWhyNot[CourtNotToday]++;
                return null;   // not today
            }
            string mine, why;
            if (Interest.Answer(ViewOf(x, y, gameDay, where), out mine) == "no")
            {
                CourtWhyNot[CourtNotTaken]++;
                return null;   // x isn't taken with y
            }
            CourtWhyNot[CourtMade]++;
            string answer = Interest.Answer(ViewOf(y, x, gameDay, where), out why);
            // a romance beginning outweighs an everyday snap (45); a rebuff is a smaller thing to see
            return new Reason { Kind = "court:" + answer + ":" + why, Strength = answer == "yes" ? 52 : answer == "maybe" ? 48 : 42,
                                Why = x.Name + " is taken with " + y.Name + ", and " + y.Name + (answer == "yes" ? " is with " + x.Name + " too" : answer == "maybe" ? " isn't sure" : " isn't (" + why + ")") };
        }

        /// How "me" sees "them" as a sweetheart (Interest.Ask), as the player is seen: no tie yet, so a stranger; their
        /// worry and mood, their nature; them dressed like money or not, known lately for trouble or not.
        public static Interest.Ask ViewOf(Someone me, Someone them, long gameDay, Place where)
        {
            string kind = null;
            int stage = -1;
            bool good;
            bool worry = me.P != null && InnerLife.WorryNow(me.P, gameDay, out kind, out stage, out good);
            return new Interest.Ask
            {
                Regard = "stranger", Drawn = Romance.DrawnTo(me.Id, me.Woman, them.Woman, where != null && where.SameSex), Taken = me.Taken, AtWorst = worry && stage == 2,
                Temper = me.Temper, Mood = me.Mood, Who = me.P, Worry = worry ? kind : null, Cash = them.WellOff ? 600 : 150, BadHere = them.Known ? 1 : 0,
                Saloon = where != null && where.Saloon
            };
        }

        private const string Desperate = "is desperate for money";

        /// Percent of fistfights between enemies that a rough man turns into a face into the table instead.
        public const int RoughChance = 45;
        public const int DrinkTemperChance = 50;   // a hothead, a night: drink and temper in the saloon come to blows (rolled per man, not per pair); 35 until Lex asked for brawls often (2026-10-28)

        /// Quick to anger: a hothead or a proud man.
        public static bool Hot(Someone s)
        {
            return s != null && (s.Temper == "Hothead" || s.Temper == "Proud");
        }
        /// Percent of a saloon hothead's run-ins with a stranger that go that far.
        public const int StrangerRoughChance = 15;
        /// Percent of a day's meetings between two hot-blooded men that come to blows (2026-10-28).
        public const int HotBloodSaloonChance = 35, HotBloodStreetChance = 15;

        /// A temper that grabs a man and uses the furniture on him.
        public static bool Rough(string temper)
        {
            return temper == "Hothead" || temper == "Proud" || temper == "Cold";
        }

        /// Quick to feel a hand in his coat: the proud, the cold, the hot-tempered.
        public static bool Alert(string temper)
        {
            return temper == "Proud" || temper == "Cold" || temper == "Hothead";
        }

        /// Why this person would ask a stranger for money today, or null: money trouble, and too honest (or too loyal,
        /// or too timid) to steal. Not every day: a day is an asking day for some of them, from their id and the day.
        public static string Beggar(Someone x, long gameDay)
        {
            if (x == null || x.P == null || x.Temper == "Greedy" || x.Temper == "Proud")
                return null;   // the greedy take, and the proud would sooner starve than ask
            string kind;
            int stage;
            bool good;
            if (!InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) || stage < 1 || stage > 2 ||
                (kind != "debt" && kind != "savings" && kind != "bank" && kind != "drink" && kind != "cards"))
                return null;
            bool honest = x.Temper == "Loyal" || x.Temper == "Coward" || x.P.Has("honest") || x.P.Facet("Honesty") >= 60;
            if (!honest || Voice.Hash(x.Id + gameDay + "beg") % 100 >= 35)
                return null;
            return kind == "drink" ? "has drunk the rent" : kind == "cards" ? "has gambled the rent away" : "has come to the end of the money";
        }

        /// Why this person would pick a pocket today, or null: by nature (greedy, a drifter, dishonest), or driven
        /// to it by money trouble at its worst. Never the loyal or the honest; most days not even the light-fingered
        /// (a day is a thieving day for a few of them, from their id and the day, so it isn't every time they pass).
        public static string Thief(Someone x, long gameDay)
        {
            if (x == null || x.Temper == "Loyal" || (x.P != null && x.P.Has("honest")))
                return null;
            if (x.Bent)   // the criminal class (Core/Criminals.cs; item 16): a theft this month, and they keep to it, whatever their nature or their money
                return Voice.Hash(x.Id + gameDay + "thief") % 100 < Criminals.KeepsChance ? Criminals.KeepsToIt : null;
            string kind;
            int stage;
            bool good;
            bool money = x.P != null && InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) && stage >= 1 && stage <= 2 &&
                         (kind == "debt" || kind == "savings" || kind == "bank" || kind == "drink" || kind == "cards");
            bool nature = x.Temper == "Greedy" || x.Temper == "Drifter" || (x.P != null && x.P.Facet("Honesty") < 30);
            if (!money && !nature)
                return null;
            int chance = (money ? 30 : 0) + (nature ? 25 : 0);
            if (Voice.Hash(x.Id + gameDay + "thief") % 100 >= chance)
                return null;
            return money ? Desperate : "has light fingers";
        }
    }
}
