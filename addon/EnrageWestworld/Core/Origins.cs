// Where a person's people come from, so their name fits them (a player, 2026-11-02: the names "sometimes aren't accurate to
// the ethnic background of the NPC"). The game's own voice banks say it: each is named after its ped model and the people it
// was recorded for (0797_A_M_M_SDCHINATOWN_01_ASIAN_01, 0055_A_F_M_BLWTOWNFOLK_02_HISPANIC_01). Of the 358 models in the voice
// table, 164 carry one such word and are certain from the model alone; 20 carry two or more (Blackwater's townsfolk, the
// poor, the working girls of Blackwater, the Dusters) and are told apart by the seed in the share the game's own banks give
// each; the rest (mostly the uniques, named by StoryCast) carry none. Without a word, a white townsman takes a share of his
// town's own people: French and Creole in Saint Denis and the bayou, Italian in Saint Denis, German and Scandinavian on the
// farms of the north and the plains. Reading the face instead (the metaped natives) is left alone: they hung the game in
// Saint Denis (2026-10-31). Plain C#, no game: the voice table is read by VoiceBook, which hands each bank's name here.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Origins
    {
        // the people the game's banks are recorded for, as the bank names spell them
        public const string White = "White", Black = "Black", Hispanic = "Hispanic", Chinese = "Chinese", Native = "Native", Irish = "Irish";
        // a white townsman's own people, from his town (never from a bank)
        public const string French = "French", Italian = "Italian", German = "German", Scandinavian = "Scandinavian";

        private static readonly Regex BankTag = new Regex(@"^\d+_(.+?)_(WHITE|BLACK|HISPANIC|ASIAN|NATIVE|IRISH)(?:_\d+)?$", RegexOptions.IgnoreCase);

        // model hash -> the banks' words for it, one entry a bank (a model with two white banks and one black is two to one)
        private static readonly Dictionary<uint, List<string>> _byModel = new Dictionary<uint, List<string>>();
        private static readonly HashSet<string> _seenBanks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static int ModelCount { get { lock (_byModel) return _byModel.Count; } }

        /// One bank of the voice table (VoiceBook.ReadBanks hands each over). A bank without a people's word teaches nothing.
        public static void Learn(string bank)
        {
            if (string.IsNullOrEmpty(bank))
                return;
            Match m = BankTag.Match(bank.Trim());
            if (!m.Success)
                return;
            string tag = Word(m.Groups[2].Value);
            uint h = MindScenarios.Joaat(m.Groups[1].Value);
            lock (_byModel)
            {
                if (!_seenBanks.Add(bank.Trim()))
                    return;
                List<string> l;
                if (!_byModel.TryGetValue(h, out l))
                    _byModel[h] = l = new List<string>(2);
                l.Add(tag);
            }
        }

        /// Forgets the table (the harness, between runs).
        public static void Clear()
        {
            lock (_byModel)
            {
                _byModel.Clear();
                _seenBanks.Clear();
            }
        }

        private static string Word(string tag)
        {
            switch (tag.ToUpperInvariant())
            {
                case "BLACK": return Black;
                case "HISPANIC": return Hispanic;
                case "ASIAN": return Chinese;   // every Asian bank in the game is Saint Denis's Chinatown, the railworkers, the dockworkers
                case "NATIVE": return Native;
                case "IRISH": return Irish;
                default: return White;
            }
        }

        /// The people the voice banks give this model: certain when they give one, else by the seed in their own shares;
        /// null when the banks say nothing (model 0, a unique, a model the table lacks).
        public static string FromModel(uint model, string seed)
        {
            if (model == 0)
                return null;
            lock (_byModel)
            {
                List<string> l;
                if (!_byModel.TryGetValue(model, out l) || l.Count == 0)
                    return null;
                return l.Count == 1 ? l[0] : l[Voice.Hash((seed ?? "") + "origin") % l.Count];
            }
        }

        /// Whether the banks give this model more than one people (the guess is then the seed's).
        public static bool Mixed(uint model)
        {
            lock (_byModel)
            {
                List<string> l;
                if (!_byModel.TryGetValue(model, out l))
                    return false;
                for (int i = 1; i < l.Count; i++)
                    if (l[i] != l[0])
                        return true;
                return false;
            }
        }

        /// A person's people: the banks' word for their model, and for a white townsman (or one the banks say nothing of) a
        /// share of his town's own. Null means the plain American lists (Residents' first ones), as everyone had before.
        public static string Of(uint model, string town, string seed)
        {
            string o = FromModel(model, seed);
            if (o != null && o != White)
                return o;
            int roll = Voice.Hash((seed ?? "") + "townfolk") % 100;
            switch (town ?? "")
            {
                case "Saint Denis":
                    return roll < 22 ? French : roll < 34 ? Italian : roll < 44 ? Irish : null;
                case "Lagras":
                case "Bayou Nwa":
                    return roll < 55 ? French : null;
                case "Rhodes":
                case "Braithwaite Manor":
                    return roll < 6 ? French : null;
                case "Strawberry":
                case "Valentine":
                case "Emerald Ranch":
                case "Wallace Station":
                case "Riggs Station":
                    return roll < 14 ? German : roll < 26 ? Scandinavian : null;
                case "Colter":
                case "Annesburg":
                case "Van Horn":
                    return roll < 10 ? Scandinavian : roll < 18 ? Irish : null;
                case "Blackwater":
                    return roll < 10 ? German : roll < 18 ? Irish : null;
                default:
                    return null;
            }
        }
    }
}
