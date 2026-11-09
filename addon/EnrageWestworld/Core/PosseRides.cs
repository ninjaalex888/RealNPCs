// The posse rides (FIVEDAY reserve R32). The posse talk (Scenes.Posse: men talking of getting one up) turns real: the day
// after a bad crime in a town (Engine.RecentCrime: a violent or thieving deed of weight 45 or more in the last day), the
// sheriff swears in three to five of the town's own men and they ride out after whoever did it. Off screen, for at least
// eighteen hours (the day's tick after): they come back with nothing (the trail went cold), or with a man tied on a mule
// (a drifter nobody knew: whether he did it, nobody asks), or one short (a posse man shot from the rocks, brought home
// across his saddle: a death like any other, mourned, waked and buried). While they're out, the town thinks of the road.
// Once a deed, once a town at a time. Kept in Store.Counts (when it rode, who rode, the deed), no new save field. Behind
// [World] PosseRides.
//
// The ride on screen (men mounting at the jail and riding out together) isn't built: a group riding in step is not proven
// in this game (only one rider's go-to-entity is, Core/StalkerRide.cs). The Testing row "posse:probe" puts a named man near
// on a riderless horse and sends him 80 m on, logging how far he got, so Lex can see whether it holds first.
//
// Why it reads true: a posse is a town's anger made into a few men on horses, and what it brings back is never quite
// justice: an empty trail, a stranger who'll do, or a neighbour's body. When the crime was yours, every outcome is yours too.
// Plain C#, no game; all words original. mindtest PosseRidesTest.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class PosseRides
    {
        public const int Min = 3, Max = 5, OutMinutes = 18 * 60, LostPct = 45, BroughtPct = 30;   // the rest (25%): one comes back dead
        public const string Prefix = "posse:", ManPrefix = "posseman:";

        public static string TownKey(string town) { return Prefix + town; }        // the game minute it rode out
        public static string DeedKey(string id) { return "possedeed:" + id; }      // once a deed
        public static string ManKey(string id) { return ManPrefix + id; }          // a man riding with it

        public static int Size(string seed) { return Min + (int)(Voice.Hash(seed + "|ps") % (Max - Min + 1)); }

        /// How it ends: "lost", "brought" or "dead".
        public static string Outcome(string seed)
        {
            int r = (int)(Voice.Hash(seed + "|po") % 100);
            return r < LostPct ? "lost" : r < LostPct + BroughtPct ? "brought" : "dead";
        }

        /// Whether a resident rides with it: a townsman, free, keeping no counter, not a coward.
        public static bool Rides(Resident r, string town)
        {
            return r != null && !r.Woman && r.Town == town && !r.Dead && !r.Left && !r.Fled && r.Held == null && r.PostId == null && r.Cast == null && r.Name != null && r.Personality != "Coward";   // (Cast: not one walking in the world now)
        }

        /// The men who ride: up to Size of the town's residents who fit, in an order fixed by the deed.
        public static List<Resident> Pick(IEnumerable<Resident> all, string town, string seed)
        {
            if (all == null || town == null)
                return new List<Resident>();
            return all.Where(r => Rides(r, town)).OrderBy(r => Voice.Hash(seed + "|" + r.Id)).Take(Size(seed)).ToList();
        }

        public static string What(string kind) { return Deeds.IsViolent(kind) ? "the killing" : "the robbery"; }

        // ---- a killing on the road (TENHOUR item 3). A deed out of town has no town, so no posse ever rode for it: you could kill a man on the Valentine road in front of three people and nothing
        // came of it but talk. Now the nearest town rides, if it's near enough for word to get there and someone is left to carry it. Why it reads true: the thing about a killing in empty country
        // is that it feels as though it has happened nowhere; it hasn't, and finding that out as men you know on horses is what consequence feels like.
        public const float RoadMetres = 1000f;

        /// Which town rides after a killing out of town, or null: it's too far for word to reach anyone that day, nobody lived to tell it, or it isn't that kind of thing. For the posse only: the deed's own Town stays empty, because the town's regard, the rumours and the witnesses all read it.
        public static string RoadTown(Deed d, long now)
        {
            if (d == null || d.Town != null || d.Witnesses <= 0 || d.Unseen || d.TalkAt != 0 || !Deeds.IsViolent(d.Kind) || d.Weight < 45 || now < d.At || now - d.At >= 1440)
                return null;
            float metres;
            string town = Rumours.NearestTown(d.X, d.Y, out metres);
            return town != null && metres <= RoadMetres ? town : null;
        }

        private static readonly string[] Formed = {
            "The sheriff swore in {k} men at {t}, and they rode out after {w}: {names}.",
            "A posse left {t} at first light after {w}. {names}, with rifles across their saddles." };
        // ---- a vigilance committee (TENHOUR item 4). Nothing tells the code a town has lost its law (Posts leaves sheriffs out), so it comes from the posse's own failure: after a killing, a posse that comes
        // home with nothing can leave the angrier men to settle on someone, a few days on. Why it reads true: when the institution fails, the grievance looks for another way out, and a crowd's certainty needs only a name.
        public const int CommitteeChance = 40, CommitteeFromDays = 2, CommitteeSpanDays = 3, CommitteeKeepDays = 3, CommitteeHangsPct = 50;

        public static string CommitteeKey(string town) { return "committee:" + town; }   // the game minute it's due

        /// Whether this posse's coming home empty leaves men wanting to settle it themselves.
        public static bool Committee(string seed) { return Voice.Chance(seed + "|comm", CommitteeChance); }

        /// Game minutes from now until they act.
        public static int CommitteeDelay(string seed) { return (CommitteeFromDays + (int)(Voice.Hash(seed + "|commd") % CommitteeSpanDays)) * 1440; }

        /// Left to themselves with nobody there to see it, whether they go through with it.
        public static bool CommitteeHangs(string seed) { return Voice.Chance(seed + "|commh", CommitteeHangsPct); }

        private static readonly string[] Hanged = {
            "Some of the men of {t} took {n} out to the cottonwoods last night. The sheriff says he saw nothing.",
            "{n} of {t} was found hanged by the creek. Nobody in town will say who held the rope." };
        private static readonly string[] Cooled = {
            "The talk at {t} of taking the law in hand came to nothing. Men went home and looked at their boots.",
            "A dozen men met behind the livery at {t}, and a dozen men went home again." };

        public static string CommitteeLine(bool hanged, string town, string name, string seed)
        {
            return Fill(Voice.Pick(seed + "|comml", hanged ? Hanged : Cooled), town, null, null, 0, name);
        }

        private static readonly string[] RoadFormed = {
            "Word of a killing out on the road reached {t}, and {k} men rode out before dark: {names}.",
            "A man came into {t} white as paper with news from the road. {names} had saddled up inside the hour." };
        private static readonly string[] Lost = {
            "The posse came back to {t} with tired horses and nothing else. The trail went cold in the hills.",
            "{t}'s posse is home, empty-handed. Rain took the tracks, they say." };
        private static readonly string[] Brought = {
            "The posse rode back into {t} with a man tied on a mule, a drifter nobody knew. Whether he did it, nobody asks.",
            "{t}'s posse brought a stranger in for {w}. He swears he was never near. They've got him in the cell." };
        private static readonly string[] Dead = {
            "The posse came back to {t} one short. {n} was shot from the rocks; they brought him home across his saddle.",
            "{n} rode out with the posse from {t} and came home dead. The rest won't say much." };
        private static readonly string[] Waiting = {
            "My husband rode out with the posse at dawn. I keep looking down the road.",
            "Half the men are gone after whoever did it. The town's too quiet.",
            "If they catch him they'll hang him from the first good tree. If they don't, he'll be back." };

        private static string Fill(string l, string town, string what, string names, int k, string name)
        {
            return l.Replace("{t}", town ?? "town").Replace("{w}", what ?? "the killing").Replace("{names}", names ?? "").Replace("{k}", k.ToString()).Replace("{n}", Person.FirstOf(name) ?? "One");
        }

        public static string FormedLine(string town, string what, List<string> names, string seed)
        {
            string list = names == null || names.Count == 0 ? "" : names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
            return Fill(Voice.Pick(seed + "|pf", Formed), town, what, list, names != null ? names.Count : 0, null);
        }

        /// The same, after a killing out on the road.
        public static string RoadFormedLine(string town, List<string> names, string seed)
        {
            string list = names == null || names.Count == 0 ? "" : names.Count == 1 ? names[0] : string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
            return Fill(Voice.Pick(seed + "|prf", RoadFormed), town, "the killing", list, names != null ? names.Count : 0, null);
        }

        public static string BackLine(string outcome, string town, string what, string deadName, string seed)
        {
            string[] pool = outcome == "dead" ? Dead : outcome == "brought" ? Brought : Lost;
            return Fill(Voice.Pick(seed + "|pb", pool), town, what, null, 0, deadName);
        }

        /// The town's thought while they're out; "my husband rode out" only from a woman (the review).
        public static string WaitingThought(string seed, bool woman) { return woman ? Voice.Pick(seed + "|pw", Waiting) : Voice.Pick(seed + "|pw", Waiting[1], Waiting[2]); }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Formed) yield return Fill(l, "Valentine", "the killing", "Jim, Abe and Walt", 3, null);
            foreach (string l in RoadFormed) yield return Fill(l, "Valentine", "the killing", "Jim, Abe and Walt", 3, null);
            foreach (string[] p in new[] { Lost, Brought, Dead })
                foreach (string l in p)
                    yield return Fill(l, "Valentine", "the killing", null, 0, "Abe Cole");
            foreach (string l in Waiting) yield return l;
            foreach (string l in Hanged) yield return Fill(l, "Valentine", null, null, 0, "Abe Cole");
            foreach (string l in Cooled) yield return Fill(l, "Valentine", null, null, 0, "Abe Cole");
        }
    }
}
