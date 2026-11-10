// A holdup at a store's door (FOURTEENHOUR reserve R1 ★; TWELVEHOUR item 8; [Scenes] StoreHoldups). The night's mugging already had
// a man short of money and honest with nobody stop someone alone in the dark (Scenes.Mugging); the pickpocket's theft already had the
// run after it (a red dot and fair game, the law's chase, a man near giving chase, your catch and what's done with him on J). This is
// the day's worse hour: by a store in town, from nine till six, a man who leans to it (known for theft, the mugging's own lean, or at
// the worst of his money trouble and not an honest man) walks up to a customer outside the store and shows his revolver. The keeper
// keeps his post: a shout from behind his counter, words only. The customer answers by nature: the anxious plead and hand it over,
// the meek hand it over, the bold bolt for help. A man who got the money runs with it (the theft's run from there); one who didn't
// curses and runs empty-handed, or, with [Scenes] HoldupShots on, now and then fires at the one who bolted (★: someone can die on
// screen; off as shipped). You: J (or a greeting) "Easy, friend": talked down by his nature (the desperate least), he lowers it and
// goes; not, he runs with it. The town tells it after (Core/Tales.cs: one man grows into a gang in the telling). Why it reads true: a
// holdup is a desperate man's worst hour, and what stops it is a word he can take, not a gun he has to face. Plain C#, no game; all
// words this add-on's.
namespace EnrageWestworld.Core
{
    public static class Holdup
    {
        public static bool On = true;      // [Scenes] StoreHoldups, from the settings
        public static bool Shots = false;  // [Scenes] HoldupShots: he may fire at one who bolts (★)
        public const string Kind = "holdup";
        public static int Percent = 15;    // [Scenes] HoldupChance: the day's roll, once a robber and a customer fit
        public const int From = 9, To = 18, ShotPercent = 25, AskMs = 9000;
        public const float StoreMetres = 18f, AloneMetres = 5f;   // the customer within this of the keeper, nobody else this near them

        /// Whether a man would hold someone up: known for theft (the pickpocket's or the mugger's mark); the mugging's own lean (honest
        /// with nobody, an appetite for risk, money short); or at the worst of his money trouble and not an honest man (desperate).
        public static bool Leans(Traits t, float moneyNeed, bool known, bool crisis, out bool desperate)
        {
            desperate = false;
            if (known)
                return true;
            if (t == null)
                return false;
            if (t.Z[Traits.Honesty] < -0.6f && t.Z[Traits.Risk] > 0.3f && moneyNeed >= 0.5f)
                return true;
            desperate = crisis && t.Z[Traits.Honesty] < -0.2f && t.Z[Traits.Risk] > 0f;
            return desperate;
        }

        /// The customer's answer by nature: "plead" (the anxious: then hands it over), "give" (the meek, at once), "bolt" (the bold run
        /// for help; most of the rest too). Unknown: a roll.
        public static string Answer(Traits t, string seed)
        {
            if (t == null)
                return Voice.Chance(seed + "|ans", 45) ? "give" : Voice.Chance(seed + "|ans2", 50) ? "plead" : "bolt";
            if (t.Z[Traits.Anxiety] > 0.2f)
                return "plead";
            if (t.Z[Traits.Assertiveness] < 0f)
                return "give";
            return t.Z[Traits.Risk] > 0.2f || Voice.Chance(seed + "|bolt", 50) ? "bolt" : "plead";
        }

        /// The holdup: A the robber, B the customer. answer: "give", "plead" or "bolt" (Answer). shoots: A fires at a bolter (only with
        /// Shots; the Director's own shot ends the scene and runs him off). A man who got it runs with it (cb:thief, the theft's run).
        public static Scene Play(SceneCast c, string seed, string answer, bool shoots)
        {
            string b = Scenes.N(c, 1, seed);
            Scene s = new Scene { Kind = Kind, Summary = "someone was held up outside the store in broad day" };
            s.Add(0, null, null, "meet");
            s.Add(0, Voice.Pick(seed + "|hq", "The purse, and nothing foolish.", "Hand it here, " + b + ". Slow.", "Not a sound. Your money, now.", "Easy, now. Just the money."), "threat", "aim");
            if (answer == "bolt")
            {
                s.Add(1, Voice.Pick(seed + "|bolt", "Help! Somebody!", "No! No, you don't!", "Thief! Thief!"), "scared", "flee");
                s.Add(0, Voice.Pick(seed + "|cur", "Damn you!", "Fool!", "Suit yourself!"), "angry");
                if (shoots && Shots)
                {
                    s.Add(0, null, "angry", "shoot");   // (★ HoldupShots: the Director's own shot, at the one who ran; the scene ends on it)
                    return s;
                }
                s.Add(0, null, null, "run");   // empty-handed: off before the town comes
                return s;
            }
            if (answer == "plead")
                s.Add(1, Voice.Pick(seed + "|pl", "Please, mister. It's all I have for the week.", "I've children at home. Please.", "Not here. Not in the street, with folks watching."), "plead");
            s.Add(1, Voice.Pick(seed + "|give", "Here, it's yours. Go on, go.", "There. That's every cent.", "Don't shoot. Here."), "scared");
            s.Add(0, Voice.Pick(seed + "|got", "Obliged. Count to fifty.", "Smart. You never saw me.", "Wasn't so hard, was it?"), "mock", "holster");
            s.Add(0, null, null, "cb:thief");   // a thief on the run from here: the red dot, the law's chase, a man near giving chase, your catch (the theft's)
            s.Add(0, null, null, "run");
            s.Add(1, Voice.Pick(seed + "|after", "Stop him! He's got my money!", "Sheriff! Somebody fetch the sheriff!", "Robbed! At the store, in broad day!"), "scared");
            return s;
        }

        /// The keeper's shout from behind his counter (words only: he keeps his post).
        public static string KeeperShout(string seed)
        {
            return Voice.Pick(seed + "|keep", "Hey! Not on my steps!", "I've a shotgun under this counter, you hear?", "Somebody get the sheriff!", "Leave my customers be!");
        }

        /// The chance a man holding someone up is talked down by a stranger's word: the coward and the plain most, the hot and the proud
        /// least, the desperate 20 less.
        public static int DownPercent(string temper, bool desperate)
        {
            int pct = temper == "Coward" ? 75 : temper == "Plain" || temper == "Loyal" ? 60 : temper == "Hothead" || temper == "Proud" ? 30 : 45;
            return desperate ? pct - 20 : pct;
        }

        public static bool TalkedDown(string temper, bool desperate, string seed)
        {
            return Voice.Chance(seed + "|down", DownPercent(temper, desperate));
        }

        /// Your word in it (Scenes.Intrude, on J or a greeting): talked down, he lowers it and walks off (the customer thanks you); not,
        /// he runs with it. antagonized: never talked down.
        public static Scene YourWord(SceneCast c, bool friendly, string seed, out string outcome)
        {
            string temper = c.Tempers != null && c.Tempers.Length > 0 ? c.Tempers[0] : null;
            bool down = friendly && TalkedDown(temper, c.Desperate, seed);
            outcome = down ? "peace" : "ran";
            Scene s = new Scene { Kind = Kind, Cast = 2 };
            if (down)
            {
                s.Add(0, Voice.Pick(seed + "|dn", "...Fine. Fine, mister. I'm going.", "You're right. God help me, you're right.", "Keep it. Keep it, then."), "scared", "holster")
                 .Add(0, null, null, "walkaway")
                 .Add(1, Voice.Pick(seed + "|th", "Thank you. Lord, thank you.", "I owe you, mister.", "Is he gone? Is he really gone?"), "thanks", "faceplayer");
                return s;
            }
            s.Add(0, Voice.Pick(seed + "|no", friendly ? "Stay out of this, mister." : "Back off, or you're next!", "Not another step!", "This ain't your business!"), "threat")
             .Add(1, Voice.Pick(seed + "|give2", "Take it! Just go!", "Here! Here, take it!"), "scared")
             .Add(0, null, null, "cb:thief")
             .Add(0, null, null, "run")
             .Add(1, Voice.Pick(seed + "|gone", "He's got it! He's got all of it!", "Stop him! Somebody!"), "scared");
            return s;
        }

        /// The J menu's rows while he holds them up.
        public static System.Collections.Generic.List<InteractOption> Rows(string robber)
        {
            return new System.Collections.Generic.List<InteractOption>
            {
                new InteractOption { Id = "holdup:talk", Label = "\"Easy, now. Holster that.\" (talk " + (robber ?? "him") + " down)" },
                new InteractOption { Id = "holdup:leave", Label = "Say nothing" },
            };
        }
    }
}
