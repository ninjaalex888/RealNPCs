// Thoughts from the saddle (FIVEDAY2 item 26; [Ticker] RoadThoughts): riders, drivers and walkers you pass out on the road, who are nobody you'll ever see again, and people at a scene by
// their part in it (whoever's held, whoever's holding them, whoever came to watch). The moment's own pools (Core/Moments.cs) think about the saddle and the weather; these think about
// you, or the road, or what they're in the middle of. Why it reads true: on an empty road every stranger is a small decision, and the watchers at something terrible are thinking about
// their supper and their own skin. All lines original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class RoadThoughts
    {
        public const string Captive = "captive", Captor = "captor", Crowd = "crowd";

        // by how they're travelling, then bold, fearful, cold, plain
        private static readonly Dictionary<string, string[][]> Passing_ = new Dictionary<string, string[][]>
        {
            { "riding", new[] {
                new[] { "Nobody's stopping me on my own road.", "Road's wide enough. For now.", "Let that one try it, out here." },
                new[] { "Don't stop. Don't slow. Just nod.", "Is that one slowing down? Lord, slowing down.", "Nobody'd hear a thing out here." },
                new[] { "Good horse. Worth more than the rider.", "Count the guns, then count the miles.", "Saddlebags ride heavy on that one." },
                new[] { "First face I've seen since morning.", "Wonder where that one's headed.", "It's a good way yet to anywhere." } } },
            { "driving", new[] {
                new[] { "They'll go round me or they'll go under.", "Wagon's slow. The rifle isn't.", "Keep off my ruts and we'll get on." },
                new[] { "A loaded wagon's an easy thing to stop.", "Don't let them look at the load.", "I wish I'd come with somebody." },
                new[] { "Twelve sacks, and not one I'm telling anybody about.", "Everybody looks at the team. Fine horses, those.", "Whatever they want, it's not on this wagon. Officially." },
                new[] { "Room for one more, if anybody asked.", "A long way round for a sack of seed.", "The children will be asleep before I'm back." } } },
            { "walking", new[] {
                new[] { "Go on, ride by. I'll get there.", "Nobody offers. Nobody ever offers.", "Some have all the horses and none of the legs." },
                new[] { "Off the road. Let them go past.", "A man on foot out here is anybody's.", "Don't look up. They go by if you don't look up." },
                new[] { "Nobody'd miss me for a week. That cuts both ways.", "Let them think I've got nothing. I've got nothing.", "Boots wear out slower than luck." },
                new[] { "Nine miles, or is it ten.", "Feet'll get there when they get there.", "There was a creek along here. I'd have sworn." } } },
        };

        private static readonly string[] Night_ =
        {
            "Who rides at this hour? Nobody good.", "Me, that's who. And look how that's going.", "Dark's no time to meet anybody.", "Can't see the hands on that one.", "Lamp's low. Keep it low.",
        };

        private static readonly Dictionary<string, string[]> Roles = new Dictionary<string, string[]>
        {
            { Captive, new[] { "If I could get one hand loose.", "Somebody's looking. Somebody's got to be.", "Not like this. Not out here, with nobody.", "Don't beg. Whatever else, don't beg.", "Mother would know what to say.", "I should have gone the long way." } },
            { Captor, new[] { "Nothing personal in it. There never is.", "A day's pay for this. A day's pay.", "Don't look at the face. That's the way of it.", "He'd do the same to me. He'd do worse.", "Who's that coming? Hand on the rifle.", "The quicker this is done the better I'll sleep." } },
            { Crowd, new[] { "Better them than me.", "Somebody ought to stop this. Somebody else.", "I can see fine from here.", "I'll tell this better than it happened.", "Don't stand too near. It spreads.", "Nobody leaves. Nobody ever leaves." } },
        };

        /// Someone you're passing out on the road, or null if they're doing something else. `group`: bold 0, fearful 1, cold 2, plain 3.
        public static string Passing(Moment m, int group, string seed)
        {
            if (m == null || m.Doing == null)
                return null;
            if (m.Night && !m.Indoors && Voice.Chance(seed + "|night", 40))
                return Voice.Pick(seed + "|nt", Night_);
            string[][] byGroup;
            string doing = m.Doing == "running" || m.Doing == "standing" ? "walking" : m.Doing;
            if (!Passing_.TryGetValue(doing, out byGroup))
                return null;
            return Voice.Pick(seed + "|road", byGroup[Math.Max(0, Math.Min(3, group))]);
        }

        /// Someone at a scene, by their part in it.
        public static string ForRole(string role, string seed)
        {
            string[] pool;
            return role != null && Roles.TryGetValue(role, out pool) ? Voice.Pick(seed + "|role", pool) : null;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string[][] t in Passing_.Values)
                foreach (string[] g in t)
                    foreach (string l in g)
                        yield return l;
            foreach (string l in Night_)
                yield return l;
            foreach (string[] p in Roles.Values)
                foreach (string l in p)
                    yield return l;
        }
    }
}
