// Found after the fact (FIVEDAY2 item 20; built on what the world already leaves behind and on the night visitor's body). Why it reads true: most of what happens
// to people happens when nobody is watching, and coming on the end of something makes the mind write the rest. Being too late is its own kind of story.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Found =
        {
            new WaysideKind
            {
                Id = "bodies", Mark = "A thing beside the road", Test = "By the road: men dead, a while now", From = 0, To = 24, OnRoad = true, Weight = 8,
                Cast = new[] { R("A dead man", "rough", null, "dead", 0f, 0f), R("A dead man", "rough", null, "dead", 2.5f, 1.2f) },
                Choices = new[]
                {
                    C("search", "Go through their pockets", 0, "search", -1),
                    C("bless", "Say a word over them", 0, "bless", -1),
                    C("leave", "Leave them", 0, "leave", -1)
                },

                News = "Two men were found dead by the {town} road. Their pockets had been gone through, or else {you} left them be."
            },
            new WaysideKind
            {
                Id = "alone", Mark = "A thing beside the road", Test = "In the wilds: someone dead, alone", From = 0, To = 24, Weight = 7,
                Cast = new[] { R("A dead woman", "rough", null, "dead", 0f, 0f, -1, true) },
                Choices = new[]
                {
                    C("search", "Look for a name on her", 0, "search", -1),
                    C("bless", "Say a word over her", 0, "bless", -1),
                    C("leave", "Leave her", 0, "leave", -1)
                },

                News = "A woman nobody knows was found dead out past {town}."
            },
            new WaysideKind
            {
                Id = "graverobbers", Mark = "Lantern light", Test = "After dark: grave robbers surprised", From = 21, To = 4, Weight = 8, Threat = "flee",
                Cast = new[] { R("A man digging", "rough", null, W("CROUCH_INSPECT"), 0f, 0f), R("A man keeping watch", "rough", "revolver", W("GUARD_LANTERN_NERVOUS"), 3f, 2f) },
                Beats = new[]
                {
                    Say(0, 1, "scared", "Somebody's there. Somebody's coming. Leave it!", "Listen. Did you hear a horse?"),
                    Say(5000, 0, "scared", "Two more minutes. It's only a ring. He don't need it.", "Hold the light still, damn you."),
                    Do(9000, "flee", 1),
                    Do(9500, "flee", 0)
                },
                Thoughts = new[] { "He don't need it. He don't need it where he is.", "If the sheriff's boy sees this lantern we're both done.", "It's only digging. I've dug before." },
                News = "Someone's been digging at a grave out past {town}, and {you} scared them off."
            },
            new WaysideKind
            {
                Id = "frozen", Mark = "A thing in the snow", Test = "High country: a man frozen where he sat", From = 0, To = 24, Weight = 24, Place = PlaceSnow,
                Cast = new[] { R("A frozen man", "rough", null, "dead", 0f, 0f) },
                Choices = new[]
                {
                    C("search", "Go through his coat", 0, "search", -1),
                    C("bless", "Say a word over him", 0, "bless", -1),
                    C("leave", "Leave him", 0, "leave", -1)
                },

                News = "A trapper came down to {town} saying there's a man sat frozen against a rock in the high country, with his boots off."
            },
            new WaysideKind
            {
                // Weight 30: just over a crossing he's the likeliest single scene, about three picks in ten, twice the next. At 200 he was seven in
                // ten, and in the swamps, where you're nearly always just over a crossing, he'd have been most of the road (TENHOUR item 10's follow-up).
                Id = "ashore", Mark = "A thing on the bank", Test = "Just over a crossing: someone washed ashore", From = 0, To = 24, Weight = 30, Place = PlaceWater,
                Cast = new[] { R("A drowned man", "rough", null, "dead", 0f, 0f) },
                Choices = new[]
                {
                    C("search", "Look for papers on him", 0, "search", -1),
                    C("bless", "Say a word over him", 0, "bless", -1),
                    C("leave", "Leave him for the river", 0, "leave", -1)
                },

                News = "A drowned man was found on the bank near {town}. Nobody knows how far he'd come."
            },
        };
    }
}
