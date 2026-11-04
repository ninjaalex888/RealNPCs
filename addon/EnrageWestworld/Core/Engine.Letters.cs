// Letters answered and written (SIXDAY item 8; Core/Replies.cs): the answer goes in the post from a town, lands one to three
// game days on (ReplyTick, from WorldTick), and at the writer's end changes what they do next: a grieving relative riding west
// stands down or rides sooner, a sweetheart warms or cools, a frightened host feels believed or mocked, the thankful stay
// grateful; most write back (QueueLetter, like any letter), and some will wait at their town's station on a set day. A letter
// of his own lands the same way. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// An answer to a letter, from a post town: in the post for one to three game days by the towns' distance.
        public Reply SendReply(Paper p, string tone, int amount, string fromTown, float metresToWriter, long now)
        {
            if (p == null || !Replies.Answerable(p) || Replies.Answered(Store.Replies, p.Id))
                return null;
            string letter = Replies.KindOf(p);
            Person who = p.FromId != null ? Find(p.FromId) : null;
            string town = who != null ? who.Home : Replies.TownOf(p);
            if (letter == "grief")
            {
                Avenger a = Store.Avengers.FirstOrDefault(x => x.Name == p.From && x.Stage <= 1);
                if (a != null && a.Town != null)
                    town = a.Town;
            }
            int days = Replies.Days(metresToWriter);
            var r = new Reply
            {
                Id = Ids.New(), PaperId = p.Id, ToId = p.FromId, ToName = p.From, ToWoman = who != null && who.Woman, Letter = letter, Tone = tone, Amount = amount,
                From = fromTown, Town = town, SentAt = now, ArrivesAt = now + days * 1440L
            };
            Store.Replies.Add(r);
            Log("REPLY to " + r.ToName + ": " + tone + (amount > 0 ? " ($" + amount / 100 + ")" : "") + ", in the post " + days + (days == 1 ? " day" : " days") + (town != null ? " to " + town : ""));
            return r;
        }

        /// A letter of his own to someone he has met, from a post town.
        public Reply WriteLetter(string toId, string toName, bool toWoman, string toTown, string kind, int amount, string fromTown, float metres, long now)
        {
            if (toId == null || kind == null)
                return null;
            int days = Replies.Days(metres);
            var r = new Reply
            {
                Id = Ids.New(), ToId = toId, ToName = toName, ToWoman = toWoman, Letter = "own", Tone = kind, Amount = amount, From = fromTown, Town = toTown,
                SentAt = now, ArrivesAt = now + days * 1440L, Own = true
            };
            Store.Replies.Add(r);
            Log("WRITE to " + toName + ": " + kind + (amount > 0 ? " ($" + amount / 100 + ")" : "") + ", in the post " + days + (days == 1 ? " day" : " days"));
            return r;
        }

        /// Letters landing (from WorldTick).
        private void ReplyTick(long now)
        {
            if (Store.Replies == null)
                return;
            foreach (Reply r in Store.Replies)
                if (r != null && !r.Done && r.ArrivesAt <= now)
                    Land(r, now);
        }

        /// What a letter does at the other end, and the letter back.
        private void Land(Reply r, long now)
        {
            r.Done = true;
            PersonState st = r.ToId != null ? MindOf(r.ToId, r.ToName) : null;
            bool courted = st != null && (st.Heart >= 2 || st.Flirts > 0);
            bool grudge = r.ToId != null && Interact.LastWrong(Store.WorldLog, r.ToId, now) != null;
            Replies.Effect e = r.Own ? Replies.OwnOutcome(r.Tone, courted, grudge, r.Id) : Replies.Outcome(r.Letter, r.Tone, r.Amount, r.Id);
            if (st != null)
            {
                st.Affection = Romance.Clamp(st.Affection + e.Affection, -100, 100);
                if (e.Heart != 0)
                    st.Heart = Romance.Clamp(st.Heart + e.Heart, 0, 5);
                if (e.Awareness != 0)
                    st.Awareness = Romance.Clamp(st.Awareness + e.Awareness, 0, 100);
            }
            if (e.Liking != 0 && r.ToId != null && r.ToId.StartsWith("folk:"))
                Effect(new EnrageEffect { PersonId = r.ToId, Kind = "like", Amount = e.Liking });   // enRAGE's own townsfolk, through its own method (the game side)
            string kin = null;
            if (r.Letter == "grief")
            {
                Avenger a = Store.Avengers.FirstOrDefault(x => x.Name == r.ToName && x.Stage <= 1);
                if (a != null)
                {
                    kin = "my " + KinWord(a.Relation, a.DeadWoman);
                    if (e.StandDown)
                    {
                        a.Stage = 3;
                        Push(new Toast { Kind = "life", Title = "Word from " + ShortTown(a.Town), Weight = 50, Text = a.Name + " has stayed home after all, they say. Your letter did that." });
                    }
                    else if (e.RideSooner)
                    {
                        a.DueAt = Math.Max(now + 120, a.DueAt - 1440);
                        Push(new Toast { Kind = "life", Title = "Word from " + ShortTown(a.Town), Weight = 55, Text = a.Name + " took the first train after your letter, they say." });
                    }
                }
                else
                {
                    // the kin grieve at home, nobody riding: the words alone
                    kin = "my own";
                    e.StandDown = false;
                    e.RideSooner = false;
                    if (e.Note == "stays home" || e.Note == "frightened off" || e.Note == "rides sooner" || e.Note == "burns the money")
                        e.Note = "taken";
                }
            }
            r.Outcome = e.Note;
            if (e.Meet)
                r.MeetAt = Replies.MeetAt(now);
            string day = Replies.DayWord(InnerLife.Weekday, Replies.MeetDaysOn);
            Paper back = Replies.Answer(r, e, Player, st != null && st.KnowsName, day, kin, now);
            if (back != null)
                QueueLetter(back);
            if (r.Own && (r.Tone == "love" || r.Tone == "threat") && r.Town != null && Voice.Hash(r.Id + "|shown") % 100 < (r.Tone == "threat" ? 40 : 30))
            {
                // shown around: the town hears (a love note or a threat makes talk)
                string word = Replies.ShownAround(r.Tone, r.ToName, Player != null ? Player.FullName : "the stranger");
                if (word != null)
                    Push(new Toast { Kind = "news", Title = "Word around " + ShortTown(r.Town), Weight = r.Tone == "threat" ? 15 : 10, Text = word });
            }
            Log((r.Own ? "WRITE landed: " : "REPLY landed: ") + r.ToName + ", " + r.Tone + " -> " + e.Note + (back != null ? "; they write back" : "") + (e.Meet ? "; they'll wait at " + (r.Town ?? "their town") + " on " + day : ""));
        }

        /// The writer waiting now in this town, if any (the game side casts them).
        public Reply MeetDue(string town, long now)
        {
            if (Store.Replies == null || town == null)
                return null;
            foreach (Reply r in Store.Replies)
                if (r != null && r.Town == town && Replies.MeetOpen(r, now))
                    return r;
            return null;
        }

        /// The meeting happened (met), or its day passed.
        public void MeetOver(Reply r, bool met, long now)
        {
            if (r == null)
                return;
            r.Met = true;
            if (met && r.ToId != null)
            {
                PersonState st = MindOf(r.ToId, r.ToName);
                if (st != null)
                {
                    st.Affection = Romance.Clamp(st.Affection + 5, -100, 100);
                    st.Talks++;
                }
            }
            Log("MEET " + r.ToName + (met ? " met you at " + (r.Town ?? "the station") : "'s day passed without you at " + (r.Town ?? "the station")));
        }

        /// The people he could write to: met (Who's who's rule), alive, with a name, the most recently seen first.
        public List<PersonState> WriteTo(long now)
        {
            var list = new List<PersonState>();
            if (Store.People == null)
                return list;
            foreach (PersonState st in Store.People.Values)
            {
                if (st == null || st.Id == null || st.Name == null || !WhosWho.Met(st) || HasDied(st.Id))
                    continue;
                list.Add(st);
            }
            list.Sort((a, b) => b.LastSeenAt.CompareTo(a.LastSeenAt));
            if (list.Count > Replies.MostWriteTo)
                list.RemoveRange(Replies.MostWriteTo, list.Count - Replies.MostWriteTo);
            return list;
        }

        /// For F7: the letters in the post and the meets waiting.
        public string RepliesStatus(long now)
        {
            if (Store.Replies == null || Store.Replies.Count == 0)
                return "none sent";
            int onWay = 0, landed = 0, meets = 0;
            string next = null;
            foreach (Reply r in Store.Replies)
            {
                if (r == null) continue;
                if (!r.Done) { onWay++; if (next == null) next = r.ToName + " (" + r.Tone + ", " + Math.Max(0, r.ArrivesAt - now) / 60 + " h)"; }
                else landed++;
                if (r.MeetAt > 0 && !r.Met) meets++;
            }
            return onWay + " in the post" + (next != null ? " (next: " + next + ")" : "") + ", " + landed + " landed, " + meets + " meets to come";
        }
    }
}
