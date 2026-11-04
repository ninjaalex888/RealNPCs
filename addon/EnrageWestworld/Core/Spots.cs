// Named outdoor places in each town (SIXDAY item 11a; roadmap 11; roadmap.md P9): the platform, the wharf, the pithead, the
// stockyards, the cemetery, the bandstand, the church steps, the corral, the main street, the saloon porch, the bank's door, the
// jail yard, the show ground. The table is built-in (empty for now) and filled from spots.txt beside RealNPCs.ini, one spot a
// line: town|kind|x|y|z|heading|room. A key in the game records where the player stands under a kind (WestworldScript.Spots.cs);
// trains, hangings and shows ask here for a place to stand. Bad lines are skipped and logged, never fatal. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Spot
    {
        public string Town, Kind;
        public float X, Y, Z, Heading;
        public float Room = Spots.DefaultRoom;   // metres of open ground around it
        public bool Transient;                   // learned from a door this session, never written to the file
    }

    public static class Spots
    {
        public const float DefaultRoom = 6f, NearMetres = 200f;
        public static readonly string[] Kinds = { "platform", "wharf", "pithead", "stockyards", "cemetery", "bandstand", "churchsteps", "corral", "mainstreet", "saloonporch", "bankdoor", "jailyard", "showground" };
        private static readonly Spot[] BuiltIn = new Spot[0];   // none yet: the key fills the file

        public static bool IsKind(string kind)
        {
            return kind != null && Kinds.Any(k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
        }

        public static string NextKind(string kind)
        {
            int i = Array.FindIndex(Kinds, k => string.Equals(k, kind, StringComparison.OrdinalIgnoreCase));
            return Kinds[(i + 1 + Kinds.Length) % Kinds.Length];
        }

        /// One spot a line: town|kind|x|y|z|heading|room (room may be left off). Null with why when the line is bad.
        public static Spot Parse(string line, out string why)
        {
            why = null;
            if (line == null)
            {
                why = "empty";
                return null;
            }
            string t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#", StringComparison.Ordinal) || t.StartsWith("//", StringComparison.Ordinal))
            {
                why = "comment";
                return null;
            }
            string[] f = t.Split('|');
            if (f.Length < 6)
            {
                why = "fewer than six fields";
                return null;
            }
            string town = f[0].Trim(), kind = f[1].Trim().ToLowerInvariant();
            if (town.Length == 0)
            {
                why = "no town";
                return null;
            }
            if (!IsKind(kind))
            {
                why = "unknown kind '" + kind + "'";
                return null;
            }
            float x, y, z, h, room = DefaultRoom;
            if (!Num(f[2], out x) || !Num(f[3], out y) || !Num(f[4], out z) || !Num(f[5], out h))
            {
                why = "a number that isn't one";
                return null;
            }
            if (f.Length > 6 && f[6].Trim().Length > 0 && (!Num(f[6], out room) || room <= 0f))
            {
                why = "a room that isn't one";
                return null;
            }
            return new Spot { Town = town, Kind = kind, X = x, Y = y, Z = z, Heading = h, Room = room };
        }

        private static bool Num(string s, out float v)
        {
            return float.TryParse(s.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        public static string Format(Spot s)
        {
            return s.Town + "|" + s.Kind + "|" + Num2(s.X) + "|" + Num2(s.Y) + "|" + Num2(s.Z) + "|" + Num2(s.Heading) + "|" + Num2(s.Room);
        }

        private static string Num2(float v)   // two decimals, a dot for the point whatever the machine's language
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:0.00}", v);
        }

        /// The built-in table and the file's lines, the bad ones logged and skipped. Never throws.
        public static List<Spot> Load(string path, Action<string> log)
        {
            var all = new List<Spot>(BuiltIn);
            if (path == null || !File.Exists(path))
                return all;
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception ex)
            {
                log?.Invoke("SPOTS " + path + " couldn't be read: " + ex.Message);
                return all;
            }
            int bad = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string why;
                Spot s = Parse(lines[i], out why);
                if (s != null)
                    all.Add(s);
                else if (why != "comment" && why != "empty")
                {
                    bad++;
                    log?.Invoke("SPOTS line " + (i + 1) + " skipped (" + why + "): " + lines[i]);
                }
            }
            log?.Invoke("SPOTS " + (all.Count - BuiltIn.Length) + " read from " + Path.GetFileName(path) + (bad > 0 ? ", " + bad + " skipped" : ""));
            return all;
        }

        /// A spot appended to the file. False (logged) when it couldn't be written; the spot is kept in memory either way.
        public static bool Append(string path, Spot s, Action<string> log)
        {
            try
            {
                File.AppendAllText(path, Format(s) + Environment.NewLine);
                return true;
            }
            catch (Exception ex)
            {
                log?.Invoke("SPOTS couldn't write " + path + ": " + ex.Message);
                return false;
            }
        }

        public static List<Spot> All(List<Spot> all, string town, string kind)
        {
            var list = new List<Spot>();
            if (all == null)
                return list;
            foreach (Spot s in all)
                if (s != null && (town == null || string.Equals(s.Town, town, StringComparison.OrdinalIgnoreCase)) && (kind == null || string.Equals(s.Kind, kind, StringComparison.OrdinalIgnoreCase)))
                    list.Add(s);
            return list;
        }

        /// The nearest spot of a kind in a town (any town when null) to a point; null when there is none.
        public static Spot Nearest(List<Spot> all, string town, string kind, float x, float y)
        {
            Spot best = null;
            float bestD = float.MaxValue;
            if (all == null)
                return null;
            foreach (Spot s in all)
            {
                if (s == null || (town != null && !string.Equals(s.Town, town, StringComparison.OrdinalIgnoreCase)) || (kind != null && !string.Equals(s.Kind, kind, StringComparison.OrdinalIgnoreCase)))
                    continue;
                float dx = s.X - x, dy = s.Y - y, d = dx * dx + dy * dy;
                if (d < bestD)
                {
                    best = s;
                    bestD = d;
                }
            }
            return best;
        }

        public static float Distance(Spot s, float x, float y)
        {
            float dx = s.X - x, dy = s.Y - y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// For F7: the spots per town and kind, and the nearest of each kind within 200 m of a point.
        public static string Status(List<Spot> all, float x, float y)
        {
            if (all == null || all.Count == 0)
                return "none recorded (the spot key: stand on the place and press it; Shift and the key change the kind)";
            var byTown = all.Where(s => s != null).GroupBy(s => s.Town).OrderBy(g => g.Key)
                            .Select(g => g.Key + " " + string.Join(" ", g.GroupBy(s => s.Kind).OrderBy(k => k.Key).Select(k => k.Key + (k.Count() > 1 ? " x" + k.Count() : ""))));
            var near = new List<string>();
            foreach (string kind in Kinds)
            {
                Spot s = Nearest(all, null, kind, x, y);
                if (s != null && Distance(s, x, y) <= NearMetres)
                    near.Add(kind + " " + (int)Distance(s, x, y) + " m (" + s.Town + ")");
            }
            return all.Count + " spots: " + string.Join("; ", byTown) + (near.Count > 0 ? "; near: " + string.Join(", ", near) : "; none within 200 m");
        }
    }
}
