// The dead come back by their temper (Lex, 2026-11-09: "a NPC the player has killed comes out for a vengeance... Their Anger trait
// should represent this"; [Stalkers] RevenantsAnger). Someone you killed may already walk again and come for you (the reveries and the
// revenant's hunt: Game/WestworldScript.Watch.cs rolls StrangerReturnChance, Core/RevenantMind.cs AttackChance by their grievance).
// Who they were didn't count. Now the mind's own measure of their anger (Traits.Anger, a z-score: 0 the town's middle, +2 the hottest
// in a hundred) weighs both: an angry man is likelier to come back, and likelier to come at you when he finds you; a mild one mostly
// stays in the ground, or comes only to look. Kept on the record of the dead (Fallen.Anger), so it holds for every return.
// Why it reads true: revenge is anger with a long memory; the people who carry a grudge past the grave are the ones who carried it in life.
namespace EnrageWestworld.Core
{
    public static class Vengeance
    {
        public const float Pull = 0.5f;        // each step of anger above the middle adds half the chance again
        public const int AttackPerStep = 15;   // and fifteen points to the odds he goes for you
        public const float Most = 2.5f;        // anger past this counts as this

        static float Clamp(float z)
        {
            return z > Most ? Most : z < -Most ? -Most : z;
        }

        /// The chance (0-100) someone you killed walks again, from the setting's and their anger.
        public static int ReturnChance(int setting, float anger)
        {
            if (setting <= 0)
                return 0;
            float c = setting * (1f + Pull * Clamp(anger));
            return c < 1f ? 1 : c > 90f ? 90 : (int)System.Math.Round(c);
        }

        /// What their anger adds to (or takes from) the odds he comes at you when he's found you.
        public static int AttackShift(float anger)
        {
            return (int)System.Math.Round(AttackPerStep * Clamp(anger));
        }
    }
}
