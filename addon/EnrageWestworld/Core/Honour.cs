// Where the culture of honour holds (NIGHT3; the psychologist's lens: Nisbett and Cohen's "culture of honor" of the
// American South, where an insult had to be answered and a man who backed down lost his standing). Lemoyne's towns and
// country: the quarrel ladder climbs a little faster there and backing down costs more (Escalation, QuarrelContext.Honour).
namespace EnrageWestworld.Core
{
    public static class Honour
    {
        private static readonly string[] South = { "Saint Denis", "Rhodes", "Lagras", "Braithwaite", "Bayou Nwa", "Scarlett Meadows", "Bluewater Marsh", "Caliga", "Lemoyne" };

        public static bool Holds(string town)
        {
            if (string.IsNullOrEmpty(town))
                return false;
            foreach (string s in South)
                if (town.IndexOf(s, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }
    }
}
