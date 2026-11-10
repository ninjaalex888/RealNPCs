// Sides by who they are (FOURTEENHOUR item 6; [Mind] BrawlSides). Before this, a man joined a fight for a friend, against a
// foe, or out of heat with nothing at stake; with no stake, a stranger had to clear the crowd's bar and a quarter more, so in
// Valentine's street (Lex's log, 2026-11-09) nobody did. Now an onlooker also leans to the fighter who is his own kind: his kin,
// a man of his own trade against a man of another (the farmhands for the farmhand, the working men for the miner, the rough
// for the rough), the town's own against an outsider (a newcomer off the train, a drifter, a man from another town). About half
// a friend's pull, and its bar halfway between a friend's and a stranger's. Why it reads true: Tajfel's minimal groups (1971):
// people favour their own on the slightest label, and in a brawl "whose side are you on?" is answered by who you are before
// anyone asks what happened. The well-off never join; they send for the law (FOURTEENHOUR item 21). Plain C#, no game.
namespace EnrageWestworld.Core
{
    /// Who someone is, as far as a fight's sides go (the game side fills it from the mind's person).
    public sealed class SidePerson
    {
        public string Id, KinId, Group, Home, Surname;
        public bool Outsider;   // a newcomer, a drifter, a stranger to the town
        public bool Pious;      // the church-minded (the dry vote's side; item 9)
        public string War;      // the war he was in (Core/OldSoldiers.cs; item 17): "blue", "grey", or a young man's, else null
    }

    /// What pulls an onlooker to a fighter: how much, and why (for the log).
    public sealed class SideTie
    {
        public float Pull;
        public string Why;
    }

    public static class BrawlSides
    {
        public const float KinPull = 0.22f, TradePull = 0.12f, TownPull = 0.10f, NamePull = 0.10f, QuarrelPull = 0.08f, ClassPull = 0.14f;

        static bool WorkingClass(string g) { return g == "working" || g == "farm" || g == "rough" || g == "trapper"; }

        /// A fight over one of 1899's quarrels (item 9): an onlooker on the fighter's side of it leans to him, when the one he's
        /// fighting is on the other; a little (QuarrelPull), and only with the topic's words.
        public static SideTie QuarrelTie(string over, SidePerson on, SidePerson fighter, SidePerson other)
        {
            // the old soldiers' war (FOURTEENHOUR item 17): a man who wore the same coat leans to his old side, as a trade does; in
            // Lemoyne the town's young men to the grey (the honour country's own, Core/Honour.cs), a little
            if (over == "war")
            {
                if (!OldSoldiers.On || on == null || fighter == null || !OldSoldiers.Old(fighter.War) || (other != null && other.War == fighter.War))
                    return null;
                if (on.War == fighter.War)
                    return new SideTie { Pull = TradePull, Why = "for his old side, the " + fighter.War };
                if (on.War == null && fighter.War == "grey" && !on.Outsider && Honour.Holds(on.Home))
                    return new SideTie { Pull = QuarrelPull, Why = "a son of the South, for the grey" };
                return null;
            }
            // a working man against a gentleman (FOURTEENHOUR item 18): the working men near lean to their own, a little more than to a
            // trade's (it's two of them against his kind)
            if (over == "class")
            {
                if (!Snub.On || on == null || fighter == null || other == null || other.Group != "gentry" || !WorkingClass(on.Group) || !WorkingClass(fighter.Group))
                    return null;
                return new SideTie { Pull = ClassPull, Why = "for a working man against a swell" };
            }
            // the temperance door (FOURTEENHOUR item 25): the pious lean to the man who stood up for the churchwoman, the drinking men
            // (the rough, the card men, the working men) to the drinker
            if (over != null && over.StartsWith("temperance:"))
            {
                if (!Temperance.DoorOn || on == null || fighter == null)
                    return null;
                string defender = over.Substring(11);
                if (on.Pious && fighter.Id == defender)
                    return new SideTie { Pull = QuarrelPull, Why = "for the man who stood up for the temperance ladies" };
                if (!on.Pious && (on.Group == "rough" || on.Group == "card" || on.Group == "working") && fighter.Id != defender && other != null && other.Id == defender)
                    return new SideTie { Pull = QuarrelPull, Why = "for a drinking man against the church" };
                return null;
            }
            if (!QuarrelPolitics.On)
                return null;   // (the game side no longer asks only with it on: the war above has its own switch)
            if (over == null || !over.StartsWith("politics:") || on == null || fighter == null)
                return null;
            string key = over.Substring(9);
            int mine = QuarrelPolitics.Stance(key, on.Group, on.Pious), his = QuarrelPolitics.Stance(key, fighter.Group, fighter.Pious);
            int theirs = other != null ? QuarrelPolitics.Stance(key, other.Group, other.Pious) : 0;
            if (mine == 0 || mine != his || theirs == his)
                return null;
            return new SideTie { Pull = QuarrelPull, Why = "on his side of " + QuarrelPolitics.Words(key) };
        }

        /// The trades that stand together in a fight: the rest (plain, town, gentry, old, girl, drummer, law) don't make a side.
        public static bool SideGroup(string g)
        {
            return g == "farm" || g == "working" || g == "rough" || g == "card" || g == "soldier" || g == "trapper";
        }

        /// The words for a trade group's side.
        public static string GroupWords(string g)
        {
            switch (g)
            {
                case "farm": return "farm hands";
                case "working": return "working men";
                case "rough": return "the rough sort";
                case "card": return "the card men";
                case "soldier": return "soldiers";
                case "trapper": return "trappers";
                default: return g;
            }
        }

        /// What pulls `on` to `fighter`, whose foe is `other` (who `fighter` is fighting), in `town`; null for nothing. Kin first,
        /// then the same family name in the same town, the same trade against another, the town's own against an outsider.
        public static SideTie Tie(SidePerson on, SidePerson fighter, SidePerson other, string town)
        {
            if (on == null || fighter == null || on.Id == null || fighter.Id == null || on.Id == fighter.Id)
                return null;
            if ((on.KinId != null && on.KinId == fighter.Id) || (fighter.KinId != null && fighter.KinId == on.Id))
                return new SideTie { Pull = KinPull, Why = "for his kin" };
            if (!string.IsNullOrEmpty(on.Surname) && on.Surname == fighter.Surname && (other == null || other.Surname != fighter.Surname) &&
                town != null && on.Home == town && fighter.Home == town)
                return new SideTie { Pull = NamePull, Why = "for one of the " + on.Surname + "s" };
            if (SideGroup(on.Group) && on.Group == fighter.Group && (other == null || other.Group != fighter.Group))
                return new SideTie { Pull = TradePull, Why = "for his own (" + GroupWords(on.Group) + ")" };
            if (town != null && on.Home == town && fighter.Home == town && !fighter.Outsider && !on.Outsider && other != null && (other.Outsider || (other.Home != null && other.Home != town)))
                return new SideTie { Pull = TownPull, Why = "for the town's own, against an outsider" };
            return null;
        }

        /// The well-off don't join a brawl: they send for the law (item 21 has them fetch him).
        public static bool SendsForTheLaw(string group)
        {
            return group == "gentry";
        }
    }
}
