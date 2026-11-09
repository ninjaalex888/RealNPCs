// On the open range (TENHOUR items 5 to 8; built on the camps and the road's fights Lex's log has the like of). The quarrels here are the ones the land was being fought over in 1899: whose cattle, whose grass, whose
// right of way, whose wages. Why it reads true: nobody out here thinks of themselves as the villain, and stumbling on it you get both sides at once, from the people who'd each tell you the other started it.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Range =
        {
            new WaysideKind
            {
                Id = "rustlers", Mark = "A fire", Test = "In the wilds: rustlers at a branding fire", From = 17, To = 5, Weight = 9, Threat = "fight",
                Cast = new[]
                {
                    R("A man at the fire", "rough", null, W("CROUCH_INSPECT"), 0f, 0f), R("A man with a rope", "rough", "revolver", W("STARE_STOIC"), 3f, 1f), R("A man keeping watch", "rough", "rifle", W("SMOKE"), -4f, 5f)
                },
                Beats = new[]
                {
                    Say(0, 0, "threat", "Hold her still. A bar over the B and she's ours.", "Iron's hot. Bring the next one."),
                    Say(4000, 2, "scared", "Somebody's on the ridge. Cover that fire.", "Hush. I heard a horse."),
                    Say(8000, 1, "threat", "Evening, friend. We're working stock. You can ride on.", "These are ours, and you've seen nothing worth telling.")
                },
                Choices = new[]
                {
                    C("paper", "Ask to see their bill of sale", 0, "hostile", 1, "A bill of sale. You hear that? He wants to see a bill of sale."),
                    C("quiet", "Take a few dollars to forget it", 0, "paid", 1, "Smart man. You were never here."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "Two more and we're over the ridge before light.", "Nobody reads a brand that close.", "If he's a stock detective I'll know by his hands." },
                News = "Somebody's been at the cattle out past {town} with a running iron, by night."
            },
        };
    }
}
