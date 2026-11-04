// What the three more verbs of Talk leave in the mind (SIXDAY item 6; Core/Verbs.cs): a loan taken is a bond remembered
// (and asked after, a small sting; denied, a wrong they know they did); an introduction is a gift of standing both remember,
// with the other's name in it. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// Ten dollars lent to them: fonder, and a memory of the lending that sits on the chest until it's paid.
        public void Lent(string id, string name, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Talks++;
            st.Affection = Romance.Clamp(st.Affection + Verbs.LentAffection, -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "loan", How = "did", Who = new List<string> { "player" },
                    Town = CurrentTown, At = now, Feel = 2, Strength = 45, Key = "loan:" + now / 60
                }, now);
        }

        /// Asked after the loan: paid, a weight off; put off, a little sore; refused, they know what they did (the fondness
        /// goes, so the next word with him is a cold one); early, a sting.
        public void Collected(string id, string name, long now, string outcome)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            int delta = outcome == "paid" ? Verbs.RepaidAffection : outcome == "putoff" ? Verbs.PutOffAffection : outcome == "refused" ? Verbs.RefusedAffection : -1;
            st.Affection = Romance.Clamp(st.Affection + delta, -100, 100);
            if (outcome == "paid")
                st.Talks++;
        }

        /// Two people made known to each other by him: both a little fonder of him, and each remembers the other's name came
        /// through him.
        public void Introduced(string aId, string aName, string bId, string bName, long now)
        {
            Note(aId, aName, bId, bName, now);
            Note(bId, bName, aId, aName, now);
        }

        private void Note(string id, string name, string otherId, string otherName, long now)
        {
            PersonState st = MindOf(id, name);
            if (st == null)
                return;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + Verbs.IntroducedAffection, -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "introduced", How = "did", Who = new List<string> { "player", otherId }, Names = new List<string> { otherName != null ? Person.FirstOf(otherName) : "somebody" },
                    Town = CurrentTown, At = now, Feel = 2, Strength = 40, Key = "introduced:" + otherId + ":" + now / 60
                }, now);
        }
    }
}
