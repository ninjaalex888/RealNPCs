// The circuit judge and gallows day (SIXDAY item 10c; roadmap 10; roadmap.md P23), two events on the town-events framework
// (Core/TownEvents.cs). A resident who kills a man in a fight is taken by the law (the death ledger's ById names him: Engine.NoteDied)
// and held at the jail; the circuit judge comes three to seven days on and tries him in the saloon, ten till two. The verdict is
// weighed from what there is: who saw it, the dead man's friends in the town against the accused's, the accused's own temper in the
// dock (a hothead convicts himself, a coward is pitied), the player's word if he gave it (worth what the town makes of him: a known
// killer's word counts half), and a frightened town, which hangs more readily. Guilty means a hanging two days on at the jail yard:
// a stockade, the sheriff's special deputies inside, a crowd outside, and the drop never shown. Not guilty splits the town for a
// week. The player can testify (true or false), break the prisoner out of the jail at night, stop the hanging (shots), or be sworn
// in as a special deputy and stand inside. Why it reads true: a frontier trial was a contest of standing and nerve more than of
// law, and a town remembered for years who had stood where. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Trials
    {
        public const int JudgeDaysLeast = 3, JudgeDaysMost = 7;    // the judge's coming, after the arrest
        public const int TrialFrom = 10, TrialTo = 14;              // the trial's hours
        public const int GallowsDaysAhead = 2, GallowsFrom = 11, GallowsTo = 12;
        public const int BreakoutFrom = 22, BreakoutTo = 5;         // the jail's quiet hours
        public const int ScoreLeast = 5, ScoreMost = 95;
        public const int WitnessWeight = 12, FriendWeight = 5, TestimonyWeight = 25, FearWeight = 10;
        public const int JailbreakWeight = 55;                      // the deed, like a robbery
        public const int DeadFriendsSoured = -15, AccusedFriendsWarmed = 10;   // what a breakout or a rescue does to their regard for the player
        public const int SwornSoured = -8;                          // the condemned man's friends, for the player who stood inside

        /// The trial, posted at the arrest: the judge comes three to seven days on (the seed decides), ten till two.
        public static TownEvent Trial(string town, Resident accused, string deadId, string deadName, long now, string seed)
        {
            long today = now / 1440;
            int days = JudgeDaysLeast + Voice.Hash(seed + "|judge") % (JudgeDaysMost - JudgeDaysLeast + 1);
            long day = today + days;
            return new TownEvent
            {
                Id = Ids.New(), Kind = "trial", Town = town, Title = "The circuit judge", At = day * 1440 + TrialFrom * 60, Until = day * 1440 + TrialTo * 60,
                AId = accused.Id, AName = accused.Name, BId = deadId, BName = deadName, Extra = "the death of " + deadName
            };
        }

        /// The hanging, posted at a guilty verdict: two days on, eleven o'clock.
        public static TownEvent Gallows(TownEvent trial, long now)
        {
            long day = Math.Max(now / 1440, trial.At / 1440) + GallowsDaysAhead;
            return new TownEvent
            {
                Id = Ids.New(), Kind = "gallows", Town = trial.Town, Title = "A hanging", At = day * 1440 + GallowsFrom * 60, Until = day * 1440 + GallowsTo * 60,
                AId = trial.AId, AName = trial.AName, BId = trial.BId, BName = trial.BName, Extra = trial.Extra
            };
        }

        /// Whether this man can be tried: a resident, alive, in the town, not held already, never tried for this death before.
        public static string CantTry(Resident accused, string deadId, List<TownEvent> events)
        {
            if (accused == null || accused.Id == null || accused.Name == null)
                return "nobody we know";
            if (accused.Dead) return "dead";
            if (accused.Fled) return "gone from the town";
            if (accused.Held != null) return "held already";
            if (events != null)
                foreach (TownEvent e in events)
                    if (e != null && (e.Kind == "trial" || e.Kind == "gallows") && e.AId == accused.Id && e.BId == deadId)
                        return "tried for it already";
            return null;
        }

        /// The verdict's weight, 5-95: the chance in a hundred the judge finds him guilty.
        public static int Score(int witnesses, int deadFriends, int accusedFriends, string temper, string testimony, string standing, int townKills)
        {
            int s = 30 + Math.Min(3, witnesses) * WitnessWeight + Math.Min(3, deadFriends) * FriendWeight - Math.Min(3, accusedFriends) * FriendWeight;
            switch (temper)
            {
                case "Hothead": s += 10; break;    // "he had it coming": the dock is no place for it
                case "Coward": s -= 5; break;      // pitied
                case "Cold": case "Proud": s += 5; break;   // no remorse the jury could see
            }
            if (testimony == "guilty" || testimony == "innocent")
            {
                int w = standing == "killer" ? TestimonyWeight / 2 : TestimonyWeight;   // a killer's word is worth half, either way
                s += testimony == "guilty" ? w : -w;
            }
            if (townKills >= 2)
                s += FearWeight;   // a frightened town hangs more readily
            return Math.Max(ScoreLeast, Math.Min(ScoreMost, s));
        }

        public static string Verdict(int score, string seed)
        {
            return Voice.Hash(seed + "|verdict") % 100 < score ? "guilty" : "not guilty";
        }

        /// Friends of someone in a town: the other ends of warm ties.
        public static List<string> FriendsOf(string id, List<SceneTie> ties)
        {
            var list = new List<string>();
            if (id == null || ties == null)
                return list;
            foreach (SceneTie t in ties)
            {
                if (t == null || t.Warmth <= 0)
                    continue;
                if (t.A == id && t.B != null && !list.Contains(t.B)) list.Add(t.B);
                else if (t.B == id && t.A != null && !list.Contains(t.A)) list.Add(t.A);
            }
            return list;
        }

        /// The jail's quiet hours, for a breakout.
        public static bool QuietHour(int hour)
        {
            return hour >= BreakoutFrom || hour < BreakoutTo;
        }

        // ---- the player's rows ----

        public static List<InteractOption> TestifyRows()
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "testify:guilty", Label = "He did it. I saw it myself." },
                new InteractOption { Id = "testify:innocent", Label = "It wasn't him. I was there." },
                new InteractOption { Id = "testify:none", Label = "Nothing for the court from me." },
            };
        }

        public static List<InteractOption> SwearRows()
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "swear:yes", Label = "Swear me in. I'll stand inside." },
                new InteractOption { Id = "swear:no", Label = "I'll watch with the rest." },
            };
        }

        public static List<InteractOption> BreakoutRows(string name)
        {
            return new List<InteractOption>
            {
                new InteractOption { Id = "breakout:yes", Label = "Get " + Person.FirstOf(name) + " out of here." },
                new InteractOption { Id = "breakout:no", Label = "Leave him to the judge." },
            };
        }

        // ---- the words ----

        public static string HeldToast(TownEvent e)
        {
            return e.AName + " is held at the jail in " + e.Town + " for " + e.Extra + ". The circuit judge is expected within the week.";
        }

        public static string HeldPaper(TownEvent e)
        {
            return e.AName + " is in the custody of the sheriff at " + e.Town + ", charged in " + e.Extra + ". The circuit judge will sit on the matter when he comes through.";
        }

        public static string JudgeToast(TownEvent e)
        {
            return "The circuit judge sits in " + e.Town + " today from ten. " + e.AName + " is to be tried for " + e.Extra + ".";
        }

        public static string VerdictToast(TownEvent e, string verdict)
        {
            return verdict == "guilty" ? e.AName + " is found guilty of " + e.Extra + ". He is to hang." : e.AName + " walks free of " + e.Extra + ". The town is not of one mind about it.";
        }

        public static string VerdictPaper(TownEvent e, string verdict, int witnesses)
        {
            return verdict == "guilty"
                ? "At " + e.Town + " the circuit judge found " + e.AName + " guilty in " + e.Extra + (witnesses > 0 ? ", on the word of " + Voice.Number(witnesses) + (witnesses == 1 ? " witness" : " witnesses") : ", there being no witness to say otherwise") + ". Sentence of death was passed, to be carried out at the jail yard on the second day following."
                : "At " + e.Town + " the circuit judge discharged " + e.AName + " in " + e.Extra + (witnesses > 0 ? ", the witnesses not agreeing" : ", no witness having come forward") + ". There was some feeling in the town afterward.";
        }

        public static string GallowsToast(TownEvent e)
        {
            return e.AName + " hangs at the jail yard in " + e.Town + " at eleven, two days from now. The sheriff is swearing in special deputies.";
        }

        public static string GallowsPaper(TownEvent e)
        {
            return "The sentence upon " + e.AName + " is to be carried out at " + e.Town + " at eleven o'clock on the second day following, within an enclosure at the jail. The sheriff has sworn in special deputies to keep the ground.";
        }

        public static string HangedToast(TownEvent e)
        {
            return e.AName + " was hanged at " + e.Town + " this morning.";
        }

        public static string HangedPaper(TownEvent e, bool crowd)
        {
            return e.AName + " was hanged at " + e.Town + " at eleven o'clock, within the enclosure at the jail. " + (crowd ? "A considerable crowd stood outside, on the wagons and the roofs, and was quiet at the end." : "Few came to stand outside.") + " He is said to have died without a word of complaint.";
        }

        public static string CalledOff(TownEvent e, string why)
        {
            return "The matter of " + e.AName + " at " + e.Town + " is at an end: " + why + ".";
        }

        public static string BreakoutPaper(TownEvent e)
        {
            return e.AName + " was taken out of the jail at " + e.Town + " in the night by a person unknown. The sheriff is asking questions, and the circuit judge will not be pleased.";
        }

        public static string StoppedPaper(TownEvent e)
        {
            return "The hanging of " + e.AName + " at " + e.Town + " was broken up by gunfire from the crowd, and in the uproar the condemned man was got away. The sheriff has a name and is not saying it.";
        }

        public static string FinishedInsidePaper(TownEvent e)
        {
            return "There was shooting outside the jail at " + e.Town + " at the hour of " + e.AName + "'s hanging, and the crowd scattered. The sentence was carried out inside by the deputies.";
        }

        public static string FinedPaper(string name, string town)
        {
            return name + " was brought before the justice at " + town + " on a charge of theft and fined ten dollars, or thirty days. He paid.";
        }

        // ---- the trial, in captions ----

        public static string[] JudgeOpens(TownEvent e)
        {
            return new[]
            {
                "This court is in session. Take off your hats.",
                e.AName + ", you stand charged in " + e.Extra + ". How do you answer?",
            };
        }

        /// The accused's answer, by temper: what the town hears in the dock.
        public static string Plea(string temper, string deadFirst, string seed)
        {
            switch (temper)
            {
                case "Hothead": return Voice.Pick(seed, "He had it coming, Judge. He'd have had it from somebody.", "I hit him. He went down. That's all there is, and I'd do it again.");
                case "Coward": return Voice.Pick(seed, "I never meant it. I never meant any of it. It was the drink, Judge, and him pushing.", "I'd give anything for it not to have happened. Anything. Ask anyone.");
                case "Proud": return Voice.Pick(seed, "Not guilty. I'll not beg this court for anything.", "I fought a man who fought me. If that's a crime, hang the half of this town.");
                case "Cold": return Voice.Pick(seed, "Not guilty.", "I've nothing to add to what's been said.");
                case "Loyal": return Voice.Pick(seed, "I've lived here my whole life, Judge. I'd not harm a neighbour meaning it.", "Whatever's decided, I'll take it. I'm sorry for " + deadFirst + "'s people.");
                case "Greedy": return Voice.Pick(seed, "I've money to make it right with the family, Judge. Say the sum.", "It was a fair fight and a bad fall. I'll pay for the burying.");
                default: return Voice.Pick(seed, "Not guilty, Judge. It was a fight, and he fell wrong.", "I'm sorry for it. I never meant him dead.");
            }
        }

        public static string WitnessLine(string deadFirst, string seed)
        {
            return Voice.Pick(seed, "I saw it, Judge. He kept hitting " + deadFirst + " after he was down.", "They were both at it. Then " + deadFirst + " didn't get up. That's what I saw.", "I was close enough to hear it. I'll not forget the sound.");
        }

        public static string JudgeAsksYou(string seed)
        {
            return Voice.Pick(seed, "You there, by the door. You were in the town. Have you anything to tell this court?", "The stranger. Step up if you saw anything. The court will hear you.");
        }

        public static string JudgeOnTestimony(string what, string seed)
        {
            switch (what)
            {
                case "guilty": return Voice.Pick(seed, "The court notes it. Sit down.", "Sworn and noted. The court thanks you.");
                case "innocent": return Voice.Pick(seed, "You'd swear to that. Very well. Noted.", "The court has heard you. Sit down.");
                default: return Voice.Pick(seed, "Then sit down and be quiet.", "As you like.");
            }
        }

        public static string[] JudgeRules(TownEvent e, string verdict)
        {
            return verdict == "guilty"
                ? new[] { "The court has heard enough. " + e.AName + ", stand up.", "On the evidence, the court finds you guilty in " + e.Extra + ".", "The sentence of this court is that you be taken from here to the jail, and on the second day hanged by the neck until dead. May God have mercy on you." }
                : new[] { "The court has heard enough. " + e.AName + ", stand up.", "The evidence does not carry it. The court finds you not guilty, and you are discharged.", "This court is adjourned. Clear the room." };
        }

        /// The crowd's word at the verdict.
        public static string CrowdAtVerdict(string verdict, string accusedFirst, string seed)
        {
            return verdict == "guilty"
                ? Voice.Pick(seed, "Hang him and be done!", "That's the law, then. God help him.", "Two days. " + accusedFirst + " has two days.")
                : Voice.Pick(seed, "Free? Free? And a man in the ground!", "Praise be. Praise be for that.", "The judge is bought. Somebody bought the judge.");
        }

        // ---- gallows day, in captions ----

        public static string[] SheriffReads(TownEvent e)
        {
            return new[]
            {
                e.AName + ". By the sentence of the circuit court, for " + e.Extra + ".",
                "Have you anything to say?",
            };
        }

        /// The condemned man's last words, by temper; to the sworn-in player when he stands inside.
        public static string LastWords(string temper, bool toYou, string seed)
        {
            if (toYou)
                return Voice.Pick(seed, "You. You stood by for this. Remember my face.", "Tell them I walked up myself. Tell them that.", "Mister, if you've a drink on you, now's the time.", "Don't look away. Somebody ought to see it done.");
            switch (temper)
            {
                case "Hothead": return Voice.Pick(seed, "Hurry it along, damn the lot of you.", "I'll see the half of you in hell.");
                case "Coward": return Voice.Pick(seed, "Please. Please, I'll go to Mexico, I'll never...", "Mother. Oh, Mother.");
                case "Proud": return Voice.Pick(seed, "Not a word for any of you.", "Do it clean. That's all I ask.");
                case "Loyal": return Voice.Pick(seed, "Tell my people I'm sorry. Tell them that.", "I was born in this town. I'd not have had it end otherwise.");
                case "Greedy": return Voice.Pick(seed, "There's money buried behind my place. Find it, somebody. Don't let the sheriff.", "Forty dollars. It all came down to forty dollars.");
                default: return Voice.Pick(seed, "I'm sorry for it. That's the truth. I'm sorry.", "Nothing. Get it done.");
            }
        }

        public static string PreacherLine(string seed)
        {
            return Voice.Pick(seed, "Be still, now. All of you. Be still.", "Whatever he did, he is in God's hands in a moment. Hold your tongues.");
        }

        public static readonly string[] Hymn = { "Someone in the crowd starts a hymn. The rest take it up, ragged, then together.", "A woman's voice begins a hymn, thin, alone. By the second line the whole crowd is singing it." };
        public static readonly string[] Silence = { "The singing stops. Nobody told it to.", "A silence. The kind you can hear a horse shift its feet in." };
        public static readonly string[] Drop = { "A sound from inside the stockade, flat and final. The crowd's breath goes out of it all at once.", "The trap. Nobody outside saw it. Everybody outside heard it." };
        public static readonly string[] After = { "A man puts his hat back on. Then everyone does.", "The crowd comes apart slowly, nobody looking at anybody.", "Somebody is being sick behind the wagon." };

        public static string DeputyToYou(bool sworn, string seed)
        {
            return sworn
                ? Voice.Pick(seed, "Stand there. Don't speak to him unless he speaks to you.", "You're sworn. If anyone comes over that fence, you stop them. Any way you can.")
                : Voice.Pick(seed, "Back behind the line, mister. Nobody inside but the sworn men.", "That's far enough. The sheriff's men only, past here.");
        }

        public static string DeputySwears(string seed)
        {
            return Voice.Pick(seed, "Raise your right hand. Say you'll keep the peace of this ground till it's done. Good. You're sworn.", "Hand up. You'll keep the ground and hold your fire unless I say. Sworn. Get inside.");
        }

        public static string Line(string[] pool, string seed)
        {
            return pool[Voice.Hash(seed) % pool.Length];
        }

        /// The town's thoughts after an acquittal, for the dead man's side and the accused's.
        public static string AfterAcquittal(bool deadSide, string accusedFirst, string seed)
        {
            return deadSide
                ? Voice.Pick(seed, accusedFirst + " walks the street and " + "a man is in the ground. That's the law for you.", "I'll not drink where " + accusedFirst + " drinks. Not now.", "Not guilty. Say it to the widow.")
                : Voice.Pick(seed, accusedFirst + "'s free. It was a fight, that's all it ever was.", "The judge saw it right. It could have been any of us.", "Half the town's not speaking to the other half. Over " + accusedFirst + ".");
        }

        public static IEnumerable<string> AllLines()
        {
            var e = new TownEvent { Kind = "trial", Town = "Valentine", AName = "Hiram Teale", BName = "Amos Cobb", Extra = "the death of Amos Cobb" };
            yield return HeldToast(e); yield return HeldPaper(e); yield return JudgeToast(e);
            yield return VerdictToast(e, "guilty"); yield return VerdictToast(e, "not guilty");
            yield return VerdictPaper(e, "guilty", 2); yield return VerdictPaper(e, "guilty", 0); yield return VerdictPaper(e, "not guilty", 1); yield return VerdictPaper(e, "not guilty", 0);
            yield return GallowsToast(e); yield return GallowsPaper(e); yield return HangedToast(e); yield return HangedPaper(e, true); yield return HangedPaper(e, false);
            yield return CalledOff(e, "the prisoner died in his cell"); yield return BreakoutPaper(e); yield return StoppedPaper(e); yield return FinishedInsidePaper(e); yield return FinedPaper("Hiram Teale", "Valentine");
            foreach (string s in JudgeOpens(e)) yield return s;
            foreach (string t in new[] { "Hothead", "Coward", "Proud", "Cold", "Loyal", "Greedy", null })
                for (int i = 0; i < 2; i++)
                    yield return Plea(t, "Amos", "p" + i);
            for (int i = 0; i < 3; i++) { yield return WitnessLine("Amos", "w" + i); yield return JudgeAsksYou("a" + i); yield return CrowdAtVerdict("guilty", "Hiram", "c" + i); yield return CrowdAtVerdict("not guilty", "Hiram", "d" + i); yield return AfterAcquittal(true, "Hiram", "e" + i); yield return AfterAcquittal(false, "Hiram", "f" + i); }
            foreach (string w in new[] { "guilty", "innocent", "none" }) for (int i = 0; i < 2; i++) yield return JudgeOnTestimony(w, "j" + i);
            foreach (string s in JudgeRules(e, "guilty")) yield return s;
            foreach (string s in JudgeRules(e, "not guilty")) yield return s;
            foreach (string s in SheriffReads(e)) yield return s;
            for (int i = 0; i < 4; i++) yield return LastWords(null, true, "y" + i);
            foreach (string t in new[] { "Hothead", "Coward", "Proud", "Loyal", "Greedy", null })
                for (int i = 0; i < 2; i++)
                    yield return LastWords(t, false, "l" + i);
            for (int i = 0; i < 2; i++) { yield return PreacherLine("p" + i); yield return DeputyToYou(true, "s" + i); yield return DeputyToYou(false, "n" + i); yield return DeputySwears("o" + i); }
            foreach (string s in Hymn) yield return s;
            foreach (string s in Silence) yield return s;
            foreach (string s in Drop) yield return s;
            foreach (string s in After) yield return s;
            foreach (InteractOption o in TestifyRows()) yield return o.Label;
            foreach (InteractOption o in SwearRows()) yield return o.Label;
            foreach (InteractOption o in BreakoutRows("Hiram Teale")) yield return o.Label;
        }
    }
}
