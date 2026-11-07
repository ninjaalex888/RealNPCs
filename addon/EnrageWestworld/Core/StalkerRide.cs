// A stalker on horseback who rides (FIVEDAY item 32c; README 13; For Lex 42). Since SIXDAY item 19 a mounted stalker sat his
// horse watching and got down to do anything else. With [Stalkers] StalkerRides he stays in the saddle for what a rider
// does: he follows you at a distance and comes in close to sit his horse near you (the allies' go-to-entity, which is
// proven to drive the mount), and he rides off when he flees or falls back (the game's smart flee, given from the saddle).
// The flee from the saddle isn't proven to take the horse, so every ride is checked: if he hasn't moved three metres in
// five seconds and hasn't arrived, he gets down and does it on foot as before. Hiding, an eerie act and standing still
// stay on foot. This is the rule; the orders are Game/Stalker.cs's. mindtest StalkerRideTest.
//
// Why it reads true (the two lenses): a man who stays mounted can leave at any moment, and a watcher who can leave at any
// moment is more frightening than one who has to walk back to his horse. The sound of a horse circling in the dark is
// pure 1899 dread.
namespace EnrageWestworld.Core
{
    public static class StalkerRide
    {
        public const int CheckMs = 5000;          // how often a ride is checked for moving
        public const float MovedMetres = 3f;      // less than this in CheckMs, not arrived: he gets down
        public const float CloseMetres = 9f, CloseFar = 13f;   // the circle in the saddle: in to 9 m, out to 13, by turns
        public const float RushMetres = 6f, RushStop = 8f;     // the rush in the saddle: stops short at 8 m (a horse's length more than on foot)
        public const float RetreatSafe = 60f, RetreatDone = 45f;   // falling back: rides to 60 m, sits again from 45

        /// What a mounted stalker does in this mode (the Stalker's Mode names): "sit" (stays mounted, watching), "ride" (the
        /// go-to-entity), "rideoff" (the smart flee from the saddle), or "dismount" (on foot, as before). With the switch off
        /// everything but sitting is done on foot.
        public static string Way(string mode, bool ridesOn)
        {
            if (mode == "Stare" || mode == "Lurk" || mode == "Watched")
                return "sit";
            if (!ridesOn)
                return "dismount";
            switch (mode)
            {
                case "Follow": case "Circle": case "Rush": return "ride";
                case "Flee": case "Retreat": return "rideoff";
                default: return "dismount";   // Hide, Act, Still: on foot
            }
        }

        /// The check: stuck in the saddle (moved too little, and a ride that hasn't arrived).
        public static bool Stuck(float moved, bool arrived) { return !arrived && moved < MovedMetres; }
    }
}
