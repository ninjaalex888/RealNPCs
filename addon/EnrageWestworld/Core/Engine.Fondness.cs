// The engine's side of the fondness interactions (SIXDAY item 1; Core/Fondness.cs): what a kindness seen, a round for
// the house and a friend's grief leave with our own people (affection, a memory), beside enRAGE's fondness, which the
// game side writes through enRAGE's own method.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// Someone saw the player do a kindness to another (a drink stood, money given, a hand up, a bully faced): a
        /// little fonder of him, and a memory of having seen him do right ("deed:kindness|saw": the pools that already
        /// word what they saw him do for a stranger). Indirect reciprocity: what he did for someone else is the surest
        /// sign of what he'd do for them. True if they could remember it.
        public bool KindnessSeen(string id, string name, string kind, string targetName, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return false;
            st.Affection = Romance.Clamp(st.Affection + Fondness.SeenAffection, -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "seen:" + kind, How = "saw", Who = new List<string> { "player" },
                    Names = new List<string> { targetName ?? "somebody" }, Town = CurrentTown, At = now,
                    Feel = 1, Strength = 35, Key = "seenkind:" + kind + ":" + now / 60   // the same kindness seen twice in the hour is one memory
                }, now);
            return true;
        }

        /// The player stood the house a drink and they were in it: fonder of him (the drunk most), and they remember it
        /// as a round he bought them (the "house|did" pools, worded with {you} as his deeds are: "{you} stood the whole
        /// house a drink. Mine too.").
        public void StoodTheHouse(string id, string name, bool drunk, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + (drunk ? Fondness.HouseDrunkAffection : Fondness.HouseAffection), -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "house", How = "did", Who = new List<string> { "player" },
                    Town = CurrentTown, At = now, Feel = drunk ? 3 : 2, Strength = drunk ? 55 : 45, Key = "house:" + now / 60
                }, now);
        }

        /// The player covered their debt while the creditor stood over them: fonder of him by a lot, and a memory of it
        /// (the "debtpaid|did" pools). The worry itself is settled by SettleWorry where it's their debt worry.
        public void DebtCovered(string id, string name, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Talks++;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + Fondness.CoverDebtorAffection, -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "debtpaid", How = "did", Who = new List<string> { "player" },
                    Town = CurrentTown, At = now, Feel = 3, Strength = 80, Key = "debtpaid:" + now / 60
                }, now);
        }

        /// The creditor the player paid: a little fonder (he got his money).
        public void PaidByYou(string id, string name)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + Fondness.CoverPaidAffection, -100, 100);
        }

        /// One of the men at a fire the player sat at: fonder of him, and a memory of the night (the "campfire|did" pools),
        /// so a regular of the nearest town who was at that fire remembers him in the street.
        public void CampfireShared(string id, string name, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Talks++;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + Fondness.CampfireAffection, -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "campfire", How = "did", Who = new List<string> { "player" },
                    Town = CurrentTown, At = now, Feel = 2, Strength = 55, Key = "campfire:" + now / 180
                }, now);
        }

        /// The player walked them home through the dark (home: to the door; else he gave up part way: a share of the
        /// affection for the thought, no memory). The "escort|did" pools.
        public void Escorted(string id, string name, long now, bool home)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + (home ? Fondness.EscortAffection : Fondness.EscortGaveUpAffection), -100, 100);
            if (!home)
                return;
            st.Talks++;
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "escort", How = "did", Who = new List<string> { "player" },
                    Town = CurrentTown, At = now, Feel = 3, Strength = 60, Key = "escort:" + now / 60
                }, now);
        }

        /// The player carried one person's word to another: both fonder of him (the sender less when the word was sent
        /// back), each with a memory of it (the "errand|did" and "errandto|did" pools).
        public void ErrandCarried(string fromId, string fromName, string toId, string toName, long now, bool madeUp)
        {
            PersonState from = MindOf(fromId, fromName), to = MindOf(toId, toName);
            if (from != null)
            {
                from.Talks++;
                from.Affection = Romance.Clamp(from.Affection + (madeUp ? Fondness.ErrandAffection : Fondness.ErrandSentBackAffection), -100, 100);
                if (Cfg.Minds)
                    Minds.Note(from.Mind, new MindMemory
                    {
                        Kind = "deed:kindness", Sub = madeUp ? "errand" : "errandback", How = "did", Who = new List<string> { "player", toId }, Names = new List<string> { toName != null ? Person.FirstOf(toName) : "somebody" },
                        Town = CurrentTown, At = now, Feel = madeUp ? 3 : 1, Strength = madeUp ? 60 : 35, Key = "errand:" + fromId + ":" + toId + ":" + now / 60
                    }, now);
            }
            if (to != null)
            {
                to.Affection = Romance.Clamp(to.Affection + Fondness.ErrandAffection, -100, 100);
                if (Cfg.Minds)
                    Minds.Note(to.Mind, new MindMemory
                    {
                        Kind = "deed:kindness", Sub = "errandto", How = "did", Who = new List<string> { "player", fromId }, Names = new List<string> { fromName != null ? Person.FirstOf(fromName) : "somebody" },
                        Town = CurrentTown, At = now, Feel = madeUp ? 2 : 1, Strength = madeUp ? 50 : 30, Key = "errandto:" + fromId + ":" + toId + ":" + now / 60
                    }, now);
            }
        }

        /// A coin for the music: a little fonder, and they remember the face.
        public void Tipped(string id, string name, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + 6, -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "tip", How = "did", Who = new List<string> { "player" },
                    Town = CurrentTown, At = now, Feel = 1, Strength = 30, Key = "tip:" + now / 60
                }, now);
        }

        /// The first name of a friend this person grieves (word of the death reached them within `days`), with the dead
        /// one's id; null when nobody. For condolences (Fondness.Condole).
        public string GrievesFor(string id, int days, out string deadId)
        {
            deadId = null;
            PersonState st = id != null ? Store.Find(id) : null;
            if (st == null || st.Mind == null)
                return null;
            long now = Store.LastGameMin;
            MindMemory m = st.Mind.Where(x => x.Kind == "death" && x.Sub == "friend" && x.Who != null && x.Who.Count > 0 && Math.Max(x.At, x.DueAt) <= now && now - Math.Max(x.At, x.DueAt) < days * 1440L)
                                  .OrderByDescending(x => Math.Max(x.At, x.DueAt)).FirstOrDefault();
            if (m == null)
                return null;
            deadId = m.Who[0];
            return m.NameAt(0);
        }
    }
}
