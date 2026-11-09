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
            new WaysideKind
            {
                Id = "sheepherder", Mark = "Men shouting", Test = "In the wilds: cowboys run off a sheepherder", From = 7, To = 19, Weight = 9,
                Cast = new[]
                {
                    R("A cowboy", "rough", null, "stand", 1.3f, 0f, 1), R("A sheepherder", "rough", null, "stand", -1.3f, 0f, 0), R("A cowboy", "rough", "revolver", W("STARE_STOIC"), 3f, 3f)
                },
                SideA = new[] { 0 }, SideB = new[] { 1 },
                Beats = new[]
                {
                    Say(0, 0, "threat", "This is cattle range. It was cattle range before you were born.", "Get those woollies off this grass by Sunday."),
                    Say(3500, 1, "scared", "I have a paper from the county, mister. I have a paper.", "Grass doesn't know whose it is."),
                    Say(7000, 2, "threat", "A paper. Show him what we think of paper.", "Go on. Knock some sense into him."),
                    Do(9000, "fight"),
                    Say(14000, 1, "fight", "Thirty years I've kept sheep! Thirty years!")
                },
                Choices = new[]
                {
                    C("part", "Stand between them", 0, "part", 1, "God keep you. They'll be back, but God keep you."),
                    C("side", "Tell the herder to clear off", 0, "leave", 0, "There. Even a stranger can see it."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "A sheep bites the grass off at the root. Ask anyone.", "Thirty years and I never hurt a soul with a lamb.", "Sunday. He'll be gone by Sunday, one way or the other." },
                News = "Cowboys ran a sheepherder off the range near {town}, and the talk is he'll be back with the county's paper."
            },
            new WaysideKind
            {
                Id = "surveyors", Mark = "Men arguing", Test = "By a farm: surveyors and a farmer's gun", From = 8, To = 18, Weight = 9, Threat = "fight",
                Cast = new[]
                {
                    R("A surveyor", "rough", null, W("CROUCH_INSPECT"), 0f, 0f), R("A chainman", "rough", null, "stand", 3f, 1f, 2), R("A farmer", "rough", "rifle", "stand", -3.5f, 0f, 0)
                },
                Beats = new[]
                {
                    Say(0, 2, "threat", "Pull that stake or I'll pull it for you.", "My father cleared this ground. Get off it."),
                    Say(4000, 0, "scared", "The company has the right of way, sir. It's all been filed.", "I only measure. Talk to the gentlemen in Saint Denis."),
                    Say(8000, 2, "threat", "Filed. Who filed it? Nobody asked me.", "You tell your company I've got two barrels and a bad temper."),
                    Say(12000, 1, "scared", "Easy now. We'll come back with the sheriff.", "Put it down, mister. Nobody's digging anything today.")
                },
                Choices = new[]
                {
                    C("farmer", "Stand with the farmer", 0, "part", 2, "Obliged to you. You heard the man, the pair of you. Off."),
                    C("company", "Tell him it's filed and done", 0, "leave", 0, "Thank you, sir. Reasonable men are hard to find out here."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "Forty acres, and a line drawn across it by a man in Saint Denis.", "Three dollars a day to be shot at.", "They'll pay him next to nothing and he'll take it. They always do." },
                News = "A farmer near {town} ran the railroad's surveyors off his land with a gun. They've said they'll be back."
            },
            new WaysideKind
            {
                Id = "organiser", Mark = "Lantern light", Test = "By the mines, after dark: a union man", From = 20, To = 4, Weight = 22, Place = PlaceMines, WalkMetres = 110f, Threat = "flee",
                Cast = new[]
                {
                    R("A man with a lantern", "rough", null, W("GUARD_LANTERN_NERVOUS"), 0f, 0f), R("A miner", "rough", null, "stand", 2f, 1f, 0), R("A miner", "rough", null, "stand", -2f, 1.5f, 0),
                    R("A company man", "rough", "revolver", "stand", 2f, -26f), R("A company man", "rough", "revolver", "stand", -2f, -30f)
                },
                Beats = new[]
                {
                    Say(0, 0, "plead", "They own the scales and the store and the roof. Think on it.", "One man asks, they let him go. Five hundred ask, they listen."),
                    Say(4500, 1, "scared", "If the foreman hears of this I'm out of the house as well.", "My wife says I'm a fool to come."),
                    Do(8000, "chase", 3),
                    Do(8300, "chase", 4),
                    Say(10000, 3, "threat", "You, with the lantern! Stay where you are!", "There he is. The one from Pennsylvania."),
                    Do(10500, "run", 0),
                    Do(11000, "flee", 1),
                    Do(11300, "flee", 2)
                },
                Choices = new[]
                {
                    C("wrong", "Point the company men the wrong way", 0, "misdirect", 3, "Obliged. He won't have got far."),
                    C("sell", "Tell them which way he went, for a price", 0, "paid", 3, "That's the first useful thing anyone's told me in this county."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "Scrip at the company store. Scrip at the company store.", "They've got a list. I know they've got a list.", "Say the word union out loud and see who looks at his boots." },
                News = "A union man was run off in the dark near {town}, and two miners are out of their houses for it."
            },
        };
    }
}
