// The lives round's two log rows (the advisor's check: switches without a Testing row): "horsetale" says the horse you ride as
// the tales would, and whether this town tells a tale of yours with it; "shrinking" says this town's ledger index and, at 35 or
// under, one of its people's thoughts of it. Nothing is kept.
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string HorseTaleTest(out string said)
        {
            said = null;
            if (!_cfg.HorseTalk || !HorseTalk.On)
                return "[World] HorseTalk is off";
            if (_engine == null || _store == null)
                return "not loaded";
            string horse = _engine.PlayerMounted ? _engine.PlayerHorse : null;
            Deed tale = _placeTown != null ? _engine.HorseTaleIn(_placeTown, _store.LastGameMin) : null;
            said = (horse != null ? "you ride " + horse : _engine.PlayerMounted ? "your horse can't be read into words" : "you're on foot")
                   + "; " + (tale != null ? _placeTown + " tells of " + tale.Kind + " in " + (tale.Where ?? "?") + " with that horse" : "no tale here with this horse" + (_placeTown == null ? " (outside the towns)" : ""));
            Log.Write("HORSE talk (test): " + said);
            return null;
        }

        private string ShrinkingTest(out string said)
        {
            said = null;
            if (!_cfg.TownShrinks || !TownShrinks.On || !_cfg.TownLedger)
                return "[World] TownShrinks and TownLedger must be on";
            if (_engine == null || _placeTown == null)
                return "this works only within a town's bounds";
            TownLedger l = _engine.LedgerOf(_placeTown);
            if (l == null)
                return _placeTown + " has no ledger yet (nothing has happened there)";
            said = _placeTown + "'s ledger index " + l.Index + " (" + (l.Mood ?? "steady") + ")" + (TownShrinks.Shrinking(l.Index) ? ": shrinking; e.g. \"" + TownShrinks.Line(_placeTown + "|test|" + Game.GameTime, null) + "\"" : ": not shrinking (35 or under)");
            Log.Write("TOWN shrinking (test): " + said);
            return null;
        }
    }
}
