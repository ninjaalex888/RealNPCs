// A burial party by the road (FIVEDAY2 R7; built on the road's table and the kneel Lex has seen). Why it reads true: out here the dead were put in the ground where the wagon gave up, and whoever rode by was the only congregation they got.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Burial =
        {
            new WaysideKind
            {
                Id = "burial", Mark = "Some people by a grave", Test = "By the road: a burial with no one to see it", From = 7, To = 19, OnRoad = true, Weight = 9,
                Cast = new[]
                {
                    R("A preacher", "town", null, W("STARE_STOIC"), 0f, 2f), R("A widow", "town", null, W("GRAVE_MOURNING_KNEEL"), -1.2f, 0f, 0, true),
                    R("A man with a spade", "rough", null, W("SMOKE"), 3f, -1f), R("A neighbour", "town", null, W("STARE_STOIC"), 1.6f, -2.4f)
                },
                Beats = new[]
                {
                    Say(0, 0, "mild", "We brought him as far as the horses would go.", "The ground's hard here, Lord, and we've done what we could with it."),
                    Say(5500, 1, "mild", "He said to put him where he could see the road.", "Nineteen years, and I can't call to mind the last thing I said to him."),
                    Say(11000, 0, "mild", "He was no worse than most of us, and better than some here.", "Whatever he owed, let it be forgiven him, and whatever he was owed."),
                    Say(16500, 2, "mild", "Somebody want to hold these boards?", "I'll fill it when you've gone. It goes quicker with nobody looking.")
                },
                Choices = new[]
                {
                    C("bless", "Take off your hat a moment", 0, "bless", 1, "Thank you, sir. There wasn't going to be anybody.", "He'd have liked that, a stranger stopping."),
                    C("give", "Give something toward the box", 200, "give", 1, "That's the boards paid for. God keep you.", "You didn't know him. You didn't have to do that."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[]
                {
                    "He owed me six dollars. I'll not bring it up.", "The preacher has the wrong name. Nobody's going to say.", "It could be any of us in that hole.", "Who'll work his ground now? That's what I'd like to know.",
                    "Don't cry yet. There's the walk back to be done first.", "A stranger on the road. Say something, somebody.",
                },
                News = "A burial was held by the road out past {town}, with the grave dug where the wagon stopped."
            },
        };
    }
}
