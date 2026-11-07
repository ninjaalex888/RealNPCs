// Gossip that travels along ties (FIVEDAY item 23; NIGHT3's psychologist list). Inside a town, word of the player's deeds
// reached the people who saw it, and a day's four locals picked at random (Engine.DeedTalk); between towns it travelled as
// a rumour, bent with the miles (Rumours.cs). Now, within the town, it also moves person to person along the ties the
// street made: a friend of someone who knows hears it the next day, anyone else they're tied to (an acquaintance, an
// enemy) the day after, a little more bent at each telling (Rumours.Version's fidelity, 100 less 18 a hop, four hops at
// most). The one who heard it thinks of who told them and what was said. A week after the deed, the talk is done.
// Behind [Mind] TieGossip.
//
// Why it reads true: gossip runs along the social network, near ties first, and each retelling sharpens and bends it
// (Allport and Postman's levelling and sharpening); sharing it is how alliances are kept (Dunbar). Plain C#, no game;
// all words original. mindtest TieGossipTest.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class TieHeard
    {
        [DataMember(EmitDefaultValue = false)] public string Id, DeedId, FromId, FromName;
        [DataMember(EmitDefaultValue = false)] public long At;
        [DataMember(EmitDefaultValue = false)] public int Hops;
    }

    public static class TieGossip
    {
        public const long FriendMinutes = 1440, OtherMinutes = 2880;
        public const int MostHops = 4, WindowDays = 7, MostPerTick = 40, Kept = 1000;

        /// How true the telling still is after this many hops (Rumours.Version's fidelity).
        public static int Fidelity(int hops) { return Math.Max(20, 100 - 18 * hops); }

        /// One step of the spread: everyone who knows (id -> how they came by it) tells those they're tied to in the town, when
        /// it's due (a friend a day after they heard it, anyone else two). canHear: whether this one can be told (a mind the
        /// store keeps, alive). At most `most` new hearers.
        public static List<TieHeard> Spread(string deedId, string town, Dictionary<string, TieHeard> known, List<SceneTie> ties, long now, Func<string, bool> canHear, int most)
        {
            var told = new List<TieHeard>();
            if (known == null || known.Count == 0 || ties == null)
                return told;
            foreach (SceneTie t in ties)
            {
                if (t == null || t.A == null || t.B == null || (town != null && t.Town != null && t.Town != town))
                    continue;
                TieHeard a, b;
                bool ka = known.TryGetValue(t.A, out a), kb = known.TryGetValue(t.B, out b);
                if (ka == kb)
                    continue;   // both know, or neither
                TieHeard from = ka ? a : b;
                string to = ka ? t.B : t.A, fromName = ka ? t.AName : t.BName;
                if (from.Hops >= MostHops || now < from.At + (t.Warmth >= 1 ? FriendMinutes : OtherMinutes) || (canHear != null && !canHear(to)))
                    continue;
                var h = new TieHeard { Id = to, DeedId = deedId, FromId = from.Id, FromName = fromName, At = now, Hops = from.Hops + 1 };
                known[to] = h;
                told.Add(h);
                if (told.Count >= most)
                    break;
            }
            return told;
        }

        /// What the one told thinks: who told them, and the version they got.
        public static string Thought(string fromFirst, string version, string seed)
        {
            if (string.IsNullOrEmpty(version))
                return null;
            string v = Grammar.SoftLower(version.TrimEnd('.'));
            string who = string.IsNullOrEmpty(fromFirst) ? "Somebody" : fromFirst;
            return Grammar.Sentence(Voice.Pick(seed + "|tg", who + " told me " + v, who + " swears " + v, "Heard it from " + who + ": " + v, who + " was full of it. " + Grammar.Sentence(v).TrimEnd('.'))) +
                   " " + Voice.Pick(seed + "|tg2", "I don't know what to believe.", "It's all over town by now.", "I'll be keeping my door shut.", "Can't say I'm surprised.");
        }
    }
}
