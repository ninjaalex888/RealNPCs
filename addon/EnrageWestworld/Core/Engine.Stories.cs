// A story over days, the engine's part (FOURTEENHOUR reserve R10; Core/Stories.cs; [World] Stories): the father's letter, the lapse,
// the mine whistle, the son found (from the game side), the train home, the letter carried to his town, and the end: his worry
// settled (SettleWorry: it ends well in his own lines), his thanks by letter, the town's word; or, lapsed, a memory of who never came.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private int _storiesOffered, _storiesDone, _storiesLapsed;

        /// From the daily tick: the open stories move on (the train home, the mine whistle, the lapse); a father may write.
        private void StoriesDaily(long now)
        {
            if (!Stories.On || Store == null)
                return;
            if (Store.Stories == null)
                Store.Stories = new List<StoryRun>();
            long today = now / 1440;
            foreach (StoryRun s in Store.Stories.Where(Stories.Open).ToList())
            {
                if (s.Choice == "train" && s.HomeAt > 0 && now >= s.HomeAt)
                {
                    StoryDone(s, "home", now);
                    continue;
                }
                if (s.Stage == 0 && LastMine != null && LastMine.At >= s.At && now - LastMine.At < 1440 && Voice.Chance(s.Id + "|whistle|" + LastMine.At, Stories.WhistlePercent))
                {
                    StoryEnd(s, "whistle", now);   // the mine whistle doesn't wait
                    continue;
                }
                if (now >= s.DueAt)
                    StoryEnd(s, "lapsed", now);   // nor does the world
            }
            if (!Cfg.Letters || Store.Stories.Count(Stories.Open) >= Stories.MostOpen || Store.Stories.Any(x => Stories.Open(x) && x.Kind == "son"))
                return;
            foreach (Resident r in Store.Residents.Where(x => x != null && x.Regular && !x.Dead && !x.Fled && !x.Left && x.Held == null && !x.Woman && x.Town != null && x.Town != Stories.SonTown &&
                                                             Store.TownVisits.ContainsKey(x.Town))
                                                  .OrderBy(x => Voice.Hash(x.Id + "|story|" + today)))
            {
                Person p = PersonOfResident(r);
                string kind;
                int stage;
                bool good;
                if (!InnerLife.WorryNow(p, today, out kind, out stage, out good) || kind != "son" || stage < 1 || stage > 2 || InnerLife.WorryFill(p, today, "{place}") != Stories.SonPlace)
                    continue;
                if (Store.Stories.Any(x => x.AskerId == r.Id))
                    continue;   // a father asks once
                if (!Voice.Chance("story|" + r.Id + "|" + today, Stories.OfferPercent))
                    return;   // one father looked at a day
                StoryOffer(r, p, today, now);
                return;
            }
        }

        private StoryRun StoryOffer(Resident r, Person p, long today, long now, bool test = false)
        {
            string pre = test ? "test-" : "";   // a test's story and letter go on load (Store.Fix)
            string surname = r.Name != null && r.Name.Contains(" ") ? r.Name.Substring(r.Name.LastIndexOf(' ') + 1) : null;
            string first = InnerLife.WorryFill(p, today, "{who}");
            var s = new StoryRun { Id = pre + Ids.New(), Kind = "son", AskerId = r.Id, AskerName = r.Name, AskerTown = r.Town, SonName = first + (surname != null ? " " + surname : ""), At = now, DueAt = now + Stories.DueDays * 1440L };
            Store.Stories.Add(s);
            KeepResident(r, now);
            QueueLetter(new Paper { Id = pre + Ids.New(), Kind = "letter", Letter = "story", Title = "A letter from " + r.Name, From = r.Name, FromId = r.Id, Town = r.Town, At = now,
                                    Body = Stories.SonLetter(KnowsYourName(r.Id) && Player != null ? Player.Surname : null, r.Name, first, r.Town, s.Id) });
            _storiesOffered++;
            Log("STORY " + r.Name + " of " + r.Town + " writes to you: his son " + s.SonName + " is at the Annesburg mine (ten days)");
            return s;
        }

        /// The open son story the player is to find in this town, or null.
        public StoryRun SonToFind(string town)
        {
            return Stories.On && town == Stories.SonTown && Store.Stories != null ? Store.Stories.FirstOrDefault(x => Stories.Open(x) && x.Kind == "son" && x.Stage == 0) : null;
        }

        /// The son found (the game side's walk-up and J): his letter carried, or the train home.
        public void StoryFound(StoryRun s, string sonId, string choice, long now)
        {
            if (s == null || !Stories.Open(s) || s.Stage != 0)
                return;
            s.Stage = 1;
            s.SonId = sonId;
            s.Choice = choice;
            if (choice == "train")
                s.HomeAt = now + (1 + Voice.Hash(s.Id + "|train") % 2) * 1440L;   // the next train: a day or two
            else
                Store.Papers.Add(new Paper { Id = Ids.New(), Kind = "note", Title = Person.FirstOf(s.SonName) + "'s letter home", From = s.SonName, At = now, Delivered = true,
                                             Body = "Sealed, for " + s.AskerName + " of " + s.AskerTown + "." });
            Log("STORY found " + s.SonName + " at Annesburg: " + (choice == "train" ? "he takes the next train home" : "you carry his letter to " + s.AskerTown));
        }

        /// In a town: a carried letter has reached the father's. True when one was.
        public bool StoryAtTown(string town, long now)
        {
            if (!Stories.On || town == null || Store.Stories == null)
                return false;
            StoryRun s = Store.Stories.FirstOrDefault(x => Stories.Open(x) && x.Stage == 1 && x.Choice == "carry" && x.AskerTown == town);
            if (s == null)
                return false;
            StoryDone(s, "carried", now);
            return true;
        }

        private void StoryDone(StoryRun s, string how, long now)
        {
            s.Stage = 2;
            s.Outcome = how;
            SettleWorry(s.AskerId);   // his worry ends well, in his own lines
            PersonState st = MindOf(s.AskerId, s.AskerName);
            if (st != null && Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory { Kind = "warmth", Sub = "storyhelp", How = "did", Role = "done", Who = new List<string> { "player" }, Names = new List<string> { "the stranger" }, Town = s.AskerTown,
                                                     At = now, Feel = 3, Strength = 70, Key = "story:" + s.Id }, now);   // the one who asked remembers
            QueueLetter(new Paper { Id = Ids.New(), Kind = "letter", Letter = "thanks", Title = "A letter from " + s.AskerName, From = s.AskerName, FromId = s.AskerId, Town = s.AskerTown, At = now,
                                    Body = Stories.Thanks(KnowsYourName(s.AskerId) && Player != null ? Player.Surname : null, s.SonName, how, s.Id) });
            string news = Stories.News(s.AskerName, s.SonName, s.AskerTown, how);
            if (news != null)
                WaysideHeard(news, s.AskerTown, s.Id + "|news", now);
            _storiesDone++;
            Log("STORY done: " + s.AskerName + "'s son " + s.SonName + " (" + how + ")");
        }

        private void StoryEnd(StoryRun s, string how, long now)
        {
            s.Stage = 2;
            s.Outcome = how;
            PersonState st = MindOf(s.AskerId, s.AskerName);
            if (st != null && Cfg.Minds && how == "lapsed")
                Minds.Note(st.Mind, new MindMemory { Kind = "slight", Sub = "storylapse", How = "did", Role = "done", Who = new List<string> { "player" }, Names = new List<string> { "the stranger" }, Town = s.AskerTown,
                                                     At = now, Feel = -1, Strength = 45, Key = "story:" + s.Id }, now);   // asked, and nobody came back to say
            if (how == "whistle")
                Push(new Toast { Kind = "life", Title = Stories.SonTown, Weight = 55, Text = Stories.News(s.AskerName, s.SonName, s.AskerTown, "whistle") });
            _storiesLapsed++;
            Log("STORY over: " + s.AskerName + "'s son " + s.SonName + " (" + how + ")");
        }

        /// The Testing row: a story from a man of this town (no worry needed), his letter at this post office now; in Annesburg, one
        /// from Valentine, to be found here at once.
        public string StoryTest(string town, long now)
        {
            if (!Stories.On)
                return "off ([World] Stories)";
            if (town == null)
                return "it wants a town";
            if (Store.Stories == null)
                Store.Stories = new List<StoryRun>();
            if (Store.Stories.Any(x => Stories.Open(x) && x.Kind == "son"))
                return "a son story is open already";
            string from = town == Stories.SonTown ? "Valentine" : town;
            Resident r = Store.Residents.FirstOrDefault(x => x != null && x.Regular && !x.Dead && !x.Woman && x.Town == from);
            if (r == null)
                return "no regular of " + from + " to write";
            StoryOffer(r, PersonOfResident(r), now / 1440, now, true);
            return null;
        }

        public string StoriesStatus()
        {
            int open = Store.Stories != null ? Store.Stories.Count(Stories.Open) : 0;
            StoryRun o = Store.Stories != null ? Store.Stories.FirstOrDefault(Stories.Open) : null;
            return _storiesOffered + " offered, " + _storiesDone + " done, " + _storiesLapsed + " lapsed this session; " + open + " open" +
                   (o != null ? " (" + o.AskerName + "'s son " + o.SonName + ": " + (o.Stage == 0 ? "to find at Annesburg" : o.Choice == "carry" ? "his letter to carry to " + o.AskerTown : "on the train home") +
                                ", " + Math.Max(0, (o.DueAt - Store.LastGameMin) / 1440) + " days left)" : "");
        }
    }
}
