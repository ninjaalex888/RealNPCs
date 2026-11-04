// The engine's side of an ally's death (SIXDAY item 2; Core/Allies.cs): someone who stood with the player and fell is
// grieved by those who knew them, and the town lays it at his door a little. Their friends hear of it within an hour or
// three (as word of any death reaches them: RememberDeathHeard), with his part in it remembered ("{who} died standing
// with {you}. {you}'s quarrel, {who}'s blood."), and think the less of him for it; the town has it by evening. Why: a death in
// someone else's quarrel is the oldest grievance there is, and it's what makes an ally's coming worth something.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public partial class Engine
    {
        /// An ally fell standing with the player: each person tied to them (a friend grieves, someone known regrets)
        /// loses affection for him and keeps the memory; the town's talk has it by the evening. Returns the mourners
        /// (id, friend?) so the game side can move enRAGE's fondness for its own townsfolk through enRAGE's method.
        public List<KeyValuePair<string, bool>> AllyFell(string deadId, string deadName, string town, long now, string yourName)
        {
            var mourners = new List<KeyValuePair<string, bool>>();
            if (deadId == null)
                return mourners;
            string deadFirst = Person.FirstOf(deadName) ?? "somebody";
            var seen = new HashSet<string>();
            foreach (SceneTie t in Store.Ties)
            {
                if (t == null || !t.Has(deadId) || t.Warmth <= 0)
                    continue;
                string other = t.A == deadId ? t.B : t.A;
                if (other == null || other == "player" || !seen.Add(other) || HasDied(other))
                    continue;
                bool friend = Ties.Friends(t);
                PersonState st = MindOf(other, t.Other(deadId));
                if (st == null)
                    continue;
                st.Affection = Romance.Clamp(st.Affection - Allies.FellAffection(friend), -100, 100);
                if (Cfg.Minds)
                    Minds.Note(st.Mind, new MindMemory
                    {
                        Kind = "death", Sub = "ally", How = "heard", Who = new List<string> { deadId, "player" }, Names = new List<string> { deadFirst },
                        Town = town, At = now, DueAt = now + 60 + Voice.Hash(other + "|" + deadId + "|fell") % 120,
                        Feel = friend ? -3 : -1, Strength = friend ? 70 : 45, Key = "allyfell:" + deadId
                    }, now);
                mourners.Add(new KeyValuePair<string, bool>(other, friend));
            }
            if (town != null)
            {
                long evening = now + 90 + Voice.Hash(deadId + now) % 120;
                Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = Allies.FellNews(deadFirst, yourName), DueAt = evening });
            }
            Log("ALLIES " + deadName + " fell standing with you: " + mourners.Count + " who knew them will hold it against you a little (" + mourners.Count(m => m.Value) + " friends)");
            return mourners;
        }
    }
}
