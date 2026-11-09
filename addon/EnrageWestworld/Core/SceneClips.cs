// The scenes' own clips (THREEHOUR item 2). The clip walker played these in Lex's game on 2026-11-09 (Core/Catalogue.cs,
// "played in game: the clip walker"), each from the set of the very scene it belongs to: the execution's guard and victim, the
// chain gang's guard and prisoners, the injured rider on the ground, the burial's preacher and digger. Until then no scene of
// ours played them: an unproven clip can leave someone frozen or T-posed, so they waited for the walker.
// Each is played through Perform (Game/WestworldScript.Perform.cs) only on someone the scene already stands there, between its
// beats, and the scene puts them back as they were after it (kneeling, aiming, their scenario). Only proven rows are handed out:
// a row that isn't proven is never played, whatever this table says. [Encounters] SceneClips switches it all off.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class SceneClips
    {
        public const string ExecWarn = "script_re@execution / warn_forward_guard";   // a captor waves you back as you come in
        public const string ExecShake = "script_re@execution / action_vic01";        // the man on his knees, afraid
        public const string ExecFreed = "script_re@execution / exit_quick_vic01";    // saved, he scrambles up and away
        public const int ExecShakeEveryMs = 11000;                                   // the least gap between two shakes

        // road scenes: kind -> the clips for each place in its cast (empty: none for that one)
        private static readonly Dictionary<string, string[][]> Road = new Dictionary<string, string[][]>
        {
            { "chaingang", new[] {
                new[] { "script_re@chain_gang / action_guard01" },                                                // the guard
                new[] { "script_re@chain_gang / action_pris01", "script_re@chain_gang / action_pick01" },          // a prisoner
                new[] { "script_re@chain_gang / action_pick01", "script_re@chain_gang / action_pris01" } } },      // a prisoner
            { "burial", new[] {
                new[] { "script_re@town_burial@preacher@pose_a@base / base" },                                    // the preacher
                new string[0],                                                                                     // the widow kneels
                new[] { "script_re@town_burial@grave_digger / ilo_med_n" },                                       // the man with a spade
                new string[0] } },
            { "injured", new[] {
                new[] { "script_re@injured_rider / idle_01" } } },                                                 // the man on the ground
        };

        /// The clip for someone in a road scene this turn, or null. Only a proven row (Catalogue) is ever given.
        public static Expression ForRoad(string kind, int role, int turn)
        {
            string[][] roles;
            if (kind == null || !Road.TryGetValue(kind, out roles) || role < 0 || role >= roles.Length || roles[role].Length == 0)
                return null;
            string[] mine = roles[role];
            return Row(mine[(turn < 0 ? 0 : turn) % mine.Length]);
        }

        public static bool HasRoad(string kind)
        {
            return kind != null && Road.ContainsKey(kind);
        }

        /// A proven clip row by name, or null.
        public static Expression Row(string name)
        {
            return name == null ? null : Catalogue.All.FirstOrDefault(e => e.Name == name && e.HasClip && e.Proven);
        }

        /// Every name the table hands out, for the offline test.
        public static IEnumerable<string> All()
        {
            foreach (string[][] roles in Road.Values)
                foreach (string[] r in roles)
                    foreach (string n in r)
                        yield return n;
            yield return ExecWarn;
            yield return ExecShake;
            yield return ExecFreed;
        }

        /// The kinds with clips of their own, and how many places in each cast.
        public static IEnumerable<KeyValuePair<string, int>> Kinds()
        {
            return Road.Select(kv => new KeyValuePair<string, int>(kv.Key, kv.Value.Length));
        }
    }
}
