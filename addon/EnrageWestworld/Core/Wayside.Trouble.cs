// Trouble on the road (FIVEDAY2 item 18; built on the hogtied captive and the wagon wheel, both of which ask something of whoever comes by). Why it reads true:
// it's easy to help when others are watching. Help given on an empty road, where nobody would ever know you'd ridden past, is the kind that gets remembered.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Trouble =
        {
            new WaysideKind
            {
                Id = "injured", Mark = "A man down", Test = "By the road: a rider hurt, his horse gone", From = 7, To = 20, OnRoad = true, Weight = 12,
                Cast = new[] { R("A man on the ground", "rough", null, W("SIT_GROUND"), 0f, 0f) },
                Beats = new[]
                {
                    Say(0, 0, "plead", "Hey! Over here! Please!", "Mister! You there! Hold up!"),
                    Say(6000, 0, "plead", "Horse put me off and kept going. It's my leg.", "Can't get up. I've been here since morning."),
                    Say(16000, 0, "plead", "Don't ride on. Please don't ride on.")
                },
                Choices = new[]
                {
                    C("help", "Get him on his feet", 0, "help", 0, "God, that hurts. Thank you. I can make it from here.", "I'll remember this. What's your name? No. I'll remember it anyway."),
                    C("give", "Leave him a dollar for the doctor", 100, "help", 0, "A dollar. Well. That's more than the last two gave me."),
                    C("rob", "Go through his pockets", 0, "rob", 0, "No! No, what are you, that's all I've got!"),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "Somebody'll come. Somebody has to come.", "That horse was never any good.", "If this leg's broke I'm finished." },
                News = "A man hurt on the {town} road says {you} stopped for him."
            },
            new WaysideKind
            {
                Id = "snakebite", Mark = "A man down", Test = "In the wilds: a man bitten by a snake", From = 8, To = 19, Weight = 8,
                Cast = new[] { R("A man on the ground", "rough", null, W("SIT_GROUND"), 0f, 0f) },
                Beats = new[]
                {
                    Say(0, 0, "plead", "Snake! It got me! Help me!", "It got my hand. Oh Lord, it got my hand."),
                    Say(7000, 0, "plead", "I can't feel my fingers. Is that bad? Is that the poison?", "Have you got whiskey? They say whiskey."),
                    Say(18000, 0, "plead", "It's getting cold. Why's it so cold?")
                },
                Choices = new[]
                {
                    C("help", "Cut it and bleed it", 0, "help", 0, "Oh God, do it, do it. ...Bless you. I suppose. Bless you.", "It's easing. Lord, it's easing."),
                    C("leave", "Leave him", 0, "leave", -1)
                },
                Thoughts = new[] { "It's in my arm now. I can feel it going up.", "Stay awake. Stay awake. Stay awake.", "Forty years old and killed by a thing with no legs." },
                News = "A man bit by a snake near {town} lived. He says a stranger cut it for him."
            },
            new WaysideKind
            {
                Id = "stranded", Mark = "Someone waiting", Test = "On a road: someone stranded", From = 7, To = 21, OnRoad = true, Weight = 10,
                Cast = new[] { R("A woman by the road", "rough", null, W("STARE_STOIC"), 0f, 0f, -1, true) },
                Beats = new[]
                {
                    Say(0, 0, "plead", "Excuse me! Sir! Which way is it to town?", "Sir! Is there anyone behind you on the road?"),
                    Say(7000, 0, "plead", "The stage left without me at the last stop. My case is on it.", "I've been walking since it was light.")
                },
                Choices = new[]
                {
                    C("help", "Point her to the nearest town", 0, "help", 0, "Thank you. I'd started to think it was all just road."),
                    C("give", "Give her a dollar for the fare", 100, "help", 0, "I'll send it back. Tell me where to send it. No? Well. Thank you."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "Don't let him see you crying.", "Everything I own is on that coach.", "Keep walking. It has to be somewhere." },
                News = "A woman put off the stage walked into {town} and says {you} showed her the way."
            },
        };
    }
}
