// The VRAM gauge's words and colours (Lex, 2026-11-01: "a small VRAM gauge at the top left of the screen informing users
// the current VRAM load on their GPU such as '15GB/16GB VRAM usage'"): "VRAM 12.4 / 16.0 GB", white, amber over 85% of
// the card, red over 95%. GB is Task Manager's (1024^3 bytes), so the two read the same. [Display] VramGauge: off, on,
// or model (the default: only while [Mind] Language, the AI model, is on). The numbers are read off the game thread
// (Game/VramReader.cs) and drawn by Game/WestworldScript.Vram.cs; everything here is plain C#, checked by mindtest.
using System.Collections.Generic;
using System.Globalization;

namespace EnrageWestworld.Core
{
    public static class Vram
    {
        public const long GiB = 1024L * 1024L * 1024L;
        public const int Off = 0, On = 1, WithModel = 2;

        /// The setting as the ini keeps it: off, on, or model (the default, and what anything else reads as).
        public static string Normal(string mode)
        {
            string m = (mode ?? string.Empty).Trim().ToLowerInvariant();
            if (m == "off" || m == "false" || m == "no" || m == "0")
                return "off";
            if (m == "on" || m == "true" || m == "yes" || m == "1" || m == "always")
                return "on";
            return "model";
        }

        public static int ModeOf(string mode)
        {
            string m = Normal(mode);
            return m == "off" ? Off : m == "on" ? On : WithModel;
        }

        /// Whether the gauge shows: on always, model only while the AI model is on.
        public static bool Shown(int mode, bool language)
        {
            return mode == On || (mode == WithModel && language);
        }

        /// "VRAM 12.4 / 16.0 GB"; with the card's size unknown (0), "VRAM 12.4 GB".
        public static string Text(long used, long total)
        {
            string u = Gb(used);
            return total > 0 ? "VRAM " + u + " / " + Gb(total) + " GB" : "VRAM " + u + " GB";
        }

        /// Bytes as GB with one decimal, the invariant way ("12.4" on every Windows).
        public static string Gb(long bytes)
        {
            return (bytes < 0 ? 0.0 : bytes / (double)GiB).ToString("0.0", CultureInfo.InvariantCulture);
        }

        /// 0 white, 1 amber over 85% of the card, 2 red over 95%; 0 while the card's size is unknown.
        public static int Tier(long used, long total)
        {
            if (total <= 0 || used <= 0)
                return 0;
            if (used * 100L > total * 95L)
                return 2;
            if (used * 100L > total * 85L)
                return 1;
            return 0;
        }

        public static string TierWord(int tier)
        {
            return tier == 2 ? "red" : tier == 1 ? "amber" : "white";
        }

        public static void Colour(int tier, out int r, out int g, out int b)
        {
            if (tier == 2)
            {
                r = 238; g = 64; b = 52;
            }
            else if (tier == 1)
            {
                r = 255; g = 180; b = 40;
            }
            else
            {
                r = 255; g = 255; b = 255;
            }
        }

        /// The used share of the card, in percent with one decimal ("70.6%"), or "" with its size unknown.
        public static string Share(long used, long total)
        {
            return total > 0 ? (used * 100.0 / total).ToString("0.0", CultureInfo.InvariantCulture) + "%" : "";
        }

        // ---- adapters ----------------------------------------------------------------------------------------------

        /// An adapter's LUID as one number: the high part above the low (DXGI's AdapterLuid, a counter's luid_0xHIGH_0xLOW).
        public static ulong Luid(uint low, int high)
        {
            return ((ulong)(uint)high << 32) | low;
        }

        /// As Windows' counters write it: "0x00000000_0x000121a8".
        public static string LuidText(ulong luid)
        {
            return "0x" + (luid >> 32).ToString("x8") + "_0x" + (luid & 0xffffffffUL).ToString("x8");
        }

        /// The adapter's LUID in a counter instance's name ("luid_0x00000000_0x000121A8_phys_0", or the per-process
        /// "pid_18860_luid_0x00000000_0x000121A8_phys_0"); false when the name holds none.
        public static bool LuidOf(string instance, out ulong luid)
        {
            luid = 0;
            if (string.IsNullOrEmpty(instance))
                return false;
            int at = instance.IndexOf("luid_", System.StringComparison.OrdinalIgnoreCase);
            if (at < 0)
                return false;
            int i = at + 5;
            uint high, low;
            if (!Hex(instance, ref i, out high) || i >= instance.Length || instance[i] != '_')
                return false;
            i++;
            if (!Hex(instance, ref i, out low))
                return false;
            luid = ((ulong)high << 32) | low;
            return true;
        }

        /// The process id at the head of a per-process instance ("pid_18860_luid_..."), else -1.
        public static int PidOf(string instance)
        {
            if (string.IsNullOrEmpty(instance) || !instance.StartsWith("pid_", System.StringComparison.OrdinalIgnoreCase))
                return -1;
            int i = 4, digits = 0;
            long pid = 0;
            while (i < instance.Length && instance[i] >= '0' && instance[i] <= '9' && digits < 10)
            {
                pid = pid * 10 + (instance[i] - '0');
                i++;
                digits++;
            }
            return digits > 0 && pid <= int.MaxValue && i < instance.Length && instance[i] == '_' ? (int)pid : -1;
        }

        // "0x" and one to eight hex digits, from i; i left after them
        private static bool Hex(string s, ref int i, out uint value)
        {
            value = 0;
            if (i + 2 > s.Length || s[i] != '0' || (s[i + 1] != 'x' && s[i + 1] != 'X'))
                return false;
            i += 2;
            int digits = 0;
            while (i < s.Length && digits < 8)
            {
                char c = s[i];
                int d = c >= '0' && c <= '9' ? c - '0' : c >= 'a' && c <= 'f' ? c - 'a' + 10 : c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
                if (d < 0)
                    break;
                value = (value << 4) | (uint)d;
                i++;
                digits++;
            }
            return digits > 0;
        }

        /// The adapter to show: the game's own (the one this process holds the most memory on) when it is among them,
        /// else the one with the most in use; false when there is none. game 0: not known.
        public static bool Pick(IDictionary<ulong, long> used, ulong game, out ulong luid, out bool isGame)
        {
            luid = 0;
            isGame = false;
            if (used == null || used.Count == 0)
                return false;
            if (game != 0 && used.ContainsKey(game))
            {
                luid = game;
                isGame = true;
                return true;
            }
            long most = -1;
            foreach (KeyValuePair<ulong, long> kv in used)
                if (kv.Value > most || (kv.Value == most && kv.Key < luid))
                {
                    most = kv.Value;
                    luid = kv.Key;
                }
            return true;
        }
    }
}
