// Prisoners and pursuit (FIVEDAY2 item 17; built on the road execution's captors and the law chase Lex has seen). Why it reads true: a man in irons or a man
// running asks one question of whoever sees him, and it isn't about the law: it's whether you think he deserves it, knowing nothing at all.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Pursuit =
        {
            new WaysideKind
            {
                Id = "chaingang", Mark = "A chain gang", Test = "On a road: a chain gang under guard", From = 7, To = 19, OnRoad = true, Weight = 10, WalkMetres = 90f, Threat = "fight",
                Cast = new[]
                {
                    R("A guard", "rough", "rifle", "stand", 0f, 4f), R("A prisoner", "rough", null, "stand", 0f, 1.5f), R("A prisoner", "rough", null, "stand", 0f, -0.8f),
                    R("A prisoner", "rough", null, "stand", 0f, -3.1f), R("A guard", "rough", "rifle", "stand", 0f, -5.5f)
                },
                Beats = new[]
                {
                    Do(0, "walk"),
                    Say(2000, 0, "threat", "Keep to the side, mister. Nothing to see.", "Give them room. They bite."),
                    Say(7000, 2, "plead", "Water. Mister, a little water.", "Tell my wife in Rhodes you saw me."),
                    Say(11000, 4, "threat", "Eyes front! You'll have your water at the camp.", "Shut your mouth and walk.")
                },
                Choices = new[]
                {
                    C("give", "Give the last man your water", 0, "help", 2, "Lord bless you. Lord bless you, mister."),
                    C("free", "Tell the guard to turn them loose", 0, "hostile", 0, "Turn them loose? Get back, or get in line."),
                    C("leave", "Ride on", 0, "leave", -1)
                },
                Thoughts = new[] { "Left foot. Right foot. Don't think past the next tree.", "Eleven months for a horse I never saw.", "Two more hours and I can sit. Two more hours.", "Hold the rifle high. Let him see it." },
                News = "A chain gang went through by the {town} road, and {you} stopped to look them over."
            },
            new WaysideKind
            {
                Id = "fugitive", Mark = "Someone running", Test = "A man runs past with men behind him", From = 6, To = 22, Weight = 12, WalkMetres = 110f, Threat = "flee",
                Cast = new[]
                {
                    R("A man running", "rough", null, "stand", 0f, 0f), R("A man with a gun", "rough", "revolver", "stand", 2f, -24f), R("A man with a gun", "rough", "revolver", "stand", -2f, -28f)
                },
                Beats = new[]
                {
                    Do(0, "run", 0),
                    Say(500, 0, "scared", "Don't tell them! You never saw me!", "Which way's the river? Which way?"),
                    Do(1500, "chase", 1),
                    Do(1800, "chase", 2),
                    Say(4000, 1, "threat", "Stop him! Stop that man!", "Hold him! There's money in it!"),
                    Say(9000, 2, "threat", "Which way'd he go?", "You! You see a man come through?")
                },
                Choices = new[]
                {
                    C("wrong", "Point them the wrong way", 0, "misdirect", 1, "Obliged. Come on, he can't have got far."),
                    C("right", "Point them the right way", 0, "leave", 1, "That's the first honest face I've seen today."),
                    C("leave", "Say nothing", 0, "leave", -1)
                },
                Thoughts = new[] { "Don't look back. It costs a step every time.", "Six dollars. All of this over six dollars.", "He's blown already. Give it another half mile." },
                News = "A man was chased through the country near {town}. Nobody will say what he did or if they caught him."
            },
            new WaysideKind
            {
                Id = "firing", Mark = "Soldiers", Test = "In the wilds: a firing party and a deserter", From = 6, To = 19, Weight = 6, Threat = "fight",
                Cast = new[]
                {
                    R("An officer", "soldier", "revolver", "stand", 2.5f, 3f, 4), R("A soldier", "soldier", "rifle", "stand", 1.6f, 7f, 4), R("A soldier", "soldier", "rifle", "stand", 0f, 7f, 4), R("A soldier", "soldier", "rifle", "stand", -1.6f, 7f, 4),
                    R("A deserter", "soldier", null, W("GRAVE_MOURNING_KNEEL"), 0f, 0f, 2)
                },
                SideA = new[] { 1, 2, 3 }, SideB = new[] { 4 },
                Beats = new[]
                {
                    Say(0, 0, "threat", "You left your post in the face of the enemy. Have you anything to say?", "The findings of this court are that you deserted the regiment."),
                    Say(4500, 4, "plead", "I walked away. That's all I did. I walked away.", "Write to my mother. Say it was fever."),
                    Say(9000, 0, "final", "Make ready."),
                    Say(11500, 0, "final", "Aim."),
                    Say(14000, 0, "final", "Fire."),
                    Do(14000, "shoot")
                },
                Choices = new[]
                {
                    C("ask", "Ask what he did", 0, "leave", 0, "Keep back, sir. This is army business and it's nearly done."),
                    C("leave", "Keep back", 0, "leave", -1)
                },
                Thoughts = new[] { "Aim for the coat. Don't look at his face.", "Sky's very blue. Never looked at it much.", "One of us has the blank. They say one of us has the blank." },
                News = "Soldiers shot a deserter out past {town}. They buried him where he fell."
            },
        };
    }
}
