// [World] Bravery, the game side. Until 2026-11-04 this file gave the brave among the ambient townsfolk the game's PCF 44
// (cower instead of flee) in a 2 s sweep over the street, and BraveStand stood the cowering back up. In game the cower read
// as people frozen where they stood (Lex twice: the Valentine saloon after a killing, then Strawberry after a death), and
// the stand-up never once fired: it asked CanDirect, which refuses the fleeing, and a person cowering is fleeing to the
// game. Lex, 2026-11-04: "Can we have it so bravery only effects their chance to join a brawl - and not the flee or cower
// flag?" So the sweep, the flag, its put-backs (aimed at, a story mission, the script stopping) and the stand-up are gone.
// Bravery now pulls on an onlooker's choice to join a fistfight (Core/Bravery.JoinPull into JoinIn.BraveryPull, set in
// ApplySettings; 85 as tuned), and the mod's own frights still answer to it (Bravery.Holds and the onlookers' bars: the
// brave stand back and watch rather than being sent running). The game's own flight and cower are the game's.
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        /// F7: what Bravery does now.
        private string BraveryStatus()
        {
            float pull = JoinIn.BraveryPull;
            return "Bravery " + _cfg.Bravery + ": an onlooker's pull to join a fight " + (pull >= 0f ? "+" : "") + pull.ToString("0.00") +
                   " (0 at 85, as tuned); the mod's own frights stand back by it; the game's cower flag is never set";
        }
    }
}
