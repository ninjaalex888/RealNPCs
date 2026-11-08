// The claim agent and the widow (FIVEDAY reserve R24). Three days after a man dies at the Annesburg mine (How "the mine"),
// the company's claim agent calls on his widow with a paper and fifty dollars "in full settlement"; after a man is killed in
// Saint Denis by another's hand, the insurance company's man calls instead, to see what can be got out of paying. What she
// does is her own (by the seed): she signs it (four in ten), she refuses and finds a lawyer (three in ten), or the town takes
// up a collection so she needn't sign (three in ten). The box tells it in the town, a day later its end. Behind [World]
// ClaimAgents.
//
// Why it reads true: 1899's companies settled deaths for what a grieving family would take, and timed the visit for when the
// grief was rawest (the agent's craft is choice under duress); the collection is the town's answer, the neighbours closing
// ranks. Plain C#, no game; all words original. mindtest ClaimAgentTest.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ClaimAgent
    {
        public const int AfterDays = 3;
        public const string Prefix = "claim:";

        /// Whether a death brings a claim agent: the mine's (the company), or a killing in Saint Denis (the insurance man).
        public static string Kind(DeathEntry d)
        {
            if (d == null || d.Name == null || (d.Kind != "resident" && d.Kind != "folk"))
                return null;
            if (d.How == "the mine")
                return "company";
            if (d.Town == "Saint Denis" && (d.By == "another" || d.By == "player"))
                return "insurance";
            return null;
        }

        /// What the widow does: "signs", "lawyer" or "collection".
        public static string Widow(string seed)
        {
            int r = (Voice.Hash(seed + "|widow") & 0x7fffffff) % 10;
            return r < 4 ? "signs" : r < 7 ? "lawyer" : "collection";
        }

        public static string Visit(string kind, string dead, string town)
        {
            string n = Person.FirstOf(dead) ?? "the dead man";
            return kind == "company"
                ? "The company's claim agent called on " + n + "'s widow with fifty dollars and a paper to sign, \"in full settlement\"."
                : "The insurance man came to " + n + "'s widow in " + (town ?? "town") + ", asking questions about the policy, not about her.";
        }

        public static string End(string choice, string dead)
        {
            string n = Person.FirstOf(dead) ?? "the dead man";
            switch (choice)
            {
                case "signs": return n + "'s widow signed the agent's paper. Fifty dollars for a man. The children will eat this winter, at least.";
                case "lawyer": return n + "'s widow sent the agent away and wrote to a lawyer in Saint Denis. The company won't like it.";
                default: return "The town took up a collection for " + n + "'s widow, so she wouldn't have to sign. Two hundred dollars in the hat.";
            }
        }

        public static IEnumerable<string> AllLines()
        {
            yield return Visit("company", "Abel Grange", "Annesburg");
            yield return Visit("insurance", "Abel Grange", "Saint Denis");
            foreach (string c in new[] { "signs", "lawyer", "collection" }) yield return End(c, "Abel Grange");
        }
    }
}
