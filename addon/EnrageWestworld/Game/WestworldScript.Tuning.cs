// NIGHT3's numbers as settings (For Lex item 8; Lex, 2026-10-31; settings version 163). The rules live in Core as static
// fields with the built numbers as their defaults (so the offline tests run on them unchanged); the settings are handed
// to them here whenever the settings are applied. The game side reads its own (the lonely look, the Talk model wait,
// the onlookers' run) from _cfg where it uses them.
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private void ApplyTunedNumbers()
        {
            Observed.LawMetres = _cfg.WatchedLawMetres;
            Observed.RecklessChance = _cfg.WatchedRecklessChance;
            Observed.ChurchMetres = _cfg.WatchedChurchMetres;
            Lonely.CompanyBar = _cfg.LonelyNeed;
            Reasons.CourtGuessAges = _cfg.CourtFolkAges;   // townsfolk court with an age guessed (NIGHT4 item 5)
            Lonely.WaitMs = _cfg.LonelyWaitSeconds * 1000;
            Lonely.StungRest = _cfg.LonelyRestDays * 1440L;
            Thanks.After = _cfg.ThanksAfterHours * 60L;
            Thanks.Within = _cfg.ThanksWithinDays * 1440L;
            Thanks.RepayAfter = _cfg.ThanksLoanDays * 1440L;
            Thanks.DrinkChance = _cfg.ThanksDrinkChance;
            Thanks.DollarChance = _cfg.ThanksDollarChance;
            Thanks.FiveChance = _cfg.ThanksFiveChance;
            Thanks.MendedChance = _cfg.ThanksMendedChance;
            Space.Metres = _cfg.SpaceMetres;
            Space.SaloonMetres = _cfg.SpaceSaloonMetres;
            Space.LingerMs = (int)(_cfg.SpaceSeconds * 1000f);
            Scenes.TurnsHothead = _cfg.BullyTurnsHothead;
            Scenes.TurnsProud = _cfg.BullyTurnsProud;
            Scenes.TurnsColdGreedy = _cfg.BullyTurnsColdGreedy;
            Scenes.TurnsCoward = _cfg.BullyTurnsCoward;
            Scenes.TurnsOthers = _cfg.BullyTurnsOthers;
            Scenes.TurnsDrunk = _cfg.BullyTurnsDrunk;
            Scenes.TurnsMeant = _cfg.BullyTurnsMeant;
            Onlooking.Running = _cfg.OnlookersRunning;
            TownMood.HushHours = _cfg.HushHours;
            TownMood.CityHushHours = _cfg.HushHoursSaintDenis;
            Reasons.StrainChance = _cfg.StrainChance;
        }
    }
}
