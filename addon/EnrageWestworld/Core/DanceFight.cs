// The fight at the dance (LIVES item 2d, 2026-11-12; [Scenes] DanceFight, settings version 470; the roadmap's "The fight at
// the dance" row). The Saturday dance (Core/TownEvents.cs, [World] EventDances) already left warm ties and, one dance in
// three, a pair of sweethearts at words over it, but only as the day's tally; nothing happened in the room. Now while a dance
// is on in the town's saloon, a man who is spoken for (Someone.Taken: a sweetheart, someone he courts) and another man there
// who's no friend of his may have it out over her ("That's twice you've had her round the floor."): a brawl over a girl at
// the dance, which the brawl's own machinery carries on (friends of each join their man, the barkeep shouts, the law comes),
// or hard words if it can't be a fight here.
//
// Why it reads true: frontier dances were where courting was public and men outnumbered women, so the dance floor was a
// contest and jealousy a matter of honour in front of the town (the "honour culture" of the South and West: Nisbett and
// Cohen, 1996); "taking it outside" was the dance's own rule, and the fiddler played on. Cast from two already in the room.
// Off in Core.
namespace EnrageWestworld.Core
{
    public static class DanceFight
    {
        public const string Over = "dance", Kind = "dancefight";
        public static bool On;   // [Scenes] DanceFight, set by the game
        public const int Strength = 55, Percent = 25;

        /// x spoken for, y another man at the dance and no friend of his: a brawl over her, or null.
        public static Reason For(Someone x, Someone y, SceneTie tie, long gameDay, Reasons.Place where)
        {
            if (!On || where == null || !where.DanceOn || !where.Saloon || x == null || y == null || x.Id == y.Id || x.Woman || y.Woman)
                return null;
            if (!x.Taken || tie != null && tie.Warmth > 0)
                return null;   // a friend may dance with your girl
            if (!Voice.Chance(Lives.Key(x, y) + "|" + gameDay + "|dance", Reasons.Weighted(Percent, where.Trouble)))
                return null;
            return new Reason { Kind = Kind, Fallback = "argue", Strength = Strength, Matter = "woman", Over = Over, Why = x.Name + " won't have " + y.Name + " dancing with his girl" };
        }

        /// A, spoken for, opens; blows, or words when either is a coward (the scene's own: Over "dance" always).
        public static Scene Make(SceneCast c, string seed)
        {
            return CauseFight.Make(c, Openers, Over, "two men came to blows over a girl at the dance", "two men had words over a girl at the dance", seed);
        }

        /// The openers (A spoken for, B the other man): A opens, B answers, A presses; the fourth is A's voice.
        public static readonly string[][] Openers =
        {
            new[] { "That's twice you've had her round the floor.", "She asked me. Ask her yourself.", "I'm asking you. Outside.", "threat" },
            new[] { "Mind how close you hold her on the turns, friend.", "It's a reel, not a wedding.", "Then you won't mind it ending.", "angry" },
            new[] { "Find your own girl.", "Seems I have. She's a fine dancer.", "Say that again and the fiddler stops.", "threat" },
            new[] { "You stepped in between us on that last one.", "You were slow off the wall.", "I'm not slow now.", "angry" },
        };
    }
}
