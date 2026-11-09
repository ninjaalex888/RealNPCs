// Labour trouble at a pithead or a rail camp (FIVEDAY2 R9; built on the road's table and the fists and guns of the road fights). Why it reads true: the frontier ran on wages somebody else set, and most of the violence in it was over who got paid what.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Labour =
        {
            new WaysideKind
            {
                Id = "strike", Mark = "Men shouting", Test = "A camp: the men have come out on strike", From = 7, To = 19, Weight = 8, Threat = "fight",
                Cast = new[]
                {
                    R("A foreman", "town", "revolver", "stand", 0f, 3f), R("A company man", "rough", "rifle", "stand", 2.2f, 3.4f), R("A company man", "rough", "rifle", "stand", -2.2f, 3.4f),
                    R("A striker", "rough", null, "stand", -1.5f, -1f, 0), R("A striker", "rough", null, "stand", 1.5f, -1.2f, 0), R("A striker", "rough", null, W("STARE_STOIC"), 0f, -3f, 0)
                },
                SideA = new[] { 0, 1, 2 }, SideB = new[] { 3, 4, 5 },
                Beats = new[]
                {
                    Say(0, 3, "angry", "Eleven hours and they dock us for the lamp oil!", "Tell the company it can dig its own damn coal."),
                    Say(5000, 0, "threat", "Back on shift by noon or there's a train full of men who will be.", "Every man who walks off this line walks off for good."),
                    Say(10000, 4, "angry", "And who'll bury the next one that's crushed? You?", "Two men dead since spring and not a nickel to their widows."),
                    Say(15000, 1, "threat", "Move along now. I've said it once.", "That's far enough."),
                    Do(19000, "fight")
                },
                Choices = new[]
                {
                    C("part", "Stand between them", 0, "part", 0, "All right. All right. Noon, I said.", "Who asked you? ...Noon."),
                    C("give", "Put something in the strike fund", 300, "give", 3, "That's a week's bread for somebody's children.", "Somebody out there's on our side, then."),
                    C("leave", "Keep riding", 0, "leave", -1)
                },
                Thoughts = new[]
                {
                    "Eleven hours. My father did twelve and thanked them.", "If I go back in, they'll know my name for it.", "I don't care who wins as long as I'm paid Friday.", "One shot and this is in every paper in the state.",
                    "They'd bring in men from Saint Denis for half. They'd do it, too.", "Hold the line. Hold the line. Is anyone else holding the line?",
                },
                News = "There was trouble at the workings near {town}: the men came out, and the company sent rifles."
            },
        };
    }
}
