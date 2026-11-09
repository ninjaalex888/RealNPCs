// Camps and cranks (FIVEDAY2 item 19; built on the hermits' camps and the campfire strangers). Why it reads true: the people who live furthest out are the ones who
// went looking for something, and the first thing any of them wants is someone new to tell about it. Some are glad of you. Some have reason not to be.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Camps =
        {
            new WaysideKind
            {
                Id = "moonshine", Mark = "A camp", Test = "In the wilds: a moonshiner's camp", From = 17, To = 4, Weight = 10, Threat = "fight",
                Cast = new[] { R("A man at a camp", "rough", "rifle", W("SIT_GROUND"), 1.4f, 0f), R("A man at a camp", "rough", null, W("SIT_GROUND"), -1.4f, 0.6f) },
                Beats = new[]
                {
                    Say(0, 0, "threat", "That's far enough. Who sent you?", "You lost, mister, or looking?"),
                    Say(5000, 1, "thanks", "Easy. He's only a traveller. Aren't you? Sit, have some.", "It's corn, and it's clean. Two dollars the jar."),
                    Say(14000, 0, "threat", "You've seen a camp. You saw nothing else.")
                },
                Choices = new[]
                {
                    C("buy", "Buy a jar", 200, "help", 1, "Sip it. I mean that. Sip it."),
                    C("hostile", "Tell them you'll tell the sheriff", 0, "hostile", 0, "Now why would you go and say a thing like that."),
                    C("leave", "Walk on", 0, "leave", -1)
                },
                Thoughts = new[] { "A lawman would have come in slower.", "Forty gallons by Sunday. Then I'm done. I said that last year.", "Keep your hand near it. Just near it." },
                News = "Someone's cooking corn liquor up past {town}, and {you} has been up there."
            },
            new WaysideKind
            {
                Id = "panner", Mark = "A man working", Test = "In the wilds: a gold panner, talking", From = 7, To = 19, Weight = 8,
                Cast = new[] { R("An old man crouching", "rough", null, W("CROUCH_INSPECT"), 0f, 0f) },
                Beats = new[]
                {
                    Say(0, 0, "thanks", "Don't stand in my light, friend.", "You're the first fellow I've seen since Easter."),
                    Say(6000, 0, "thanks", "There's color here. Not much. But there's color.", "Nineteen years. I've been about to strike it for nineteen years."),
                    Say(14000, 0, "thanks", "You won't tell anybody, now. Not that there's much to tell.")
                },
                Choices = new[]
                {
                    C("give", "Leave him a dollar", 100, "help", 0, "Well now. That's more than this creek's paid me since spring."),
                    C("leave", "Wish him luck", 0, "leave", 0, "Luck's got nothing to do with it. It's all gravel.")
                },
                Thoughts = new[] { "One more pan. Then coffee. One more.", "Martha said come home by Christmas. Which Christmas, I forget.", "It's in there. It's got to be in there." },
                News = "The old man panning the creek past {town} told {you} he's about to strike it. He's told everyone that."
            },
            new WaysideKind
            {
                Id = "sharpshooter", Mark = "Gunshots", Test = "A sharpshooter, showing off", From = 8, To = 19, Weight = 9, Threat = "fight",
                Cast = new[] { R("A man with a rifle", "rough", "rifle", "stand", 0f, 0f, 1), R("A man watching", "rough", null, W("STARE_STOIC"), 3f, 2f, 0) },
                Beats = new[]
                {
                    Say(0, 0, "thanks", "Stand clear, friend. I'm about to make this man poor.", "See that post? See the knot on it?"),
                    Say(5000, 1, "thanks", "He does this. He does this every time.", "Don't bet with him. I'm telling you as a friend."),
                    Say(9000, 0, "thanks", "A dollar says I hit it. You game?", "Dollar says I don't miss.")
                },
                Choices = new[]
                {
                    C("bet", "Bet him a dollar", 100, "bet", 0, "Done. Watch the knot, now."),
                    C("leave", "Leave him to it", 0, "leave", -1)
                },
                Thoughts = new[] { "Breathe out. Breathe out and it's already done.", "Used to be I could do this drunk.", "If he takes the bet I eat tonight." },
                News = "A sharpshooter's been taking bets out past {town}, and {you} had a look."
            },
            new WaysideKind
            {
                Id = "duel", Mark = "Two men facing", Test = "Two men settle it with pistols", From = 5, To = 10, Weight = 8, Threat = "flee",
                Cast = new[]
                {
                    R("A gentleman", "rough", "revolver", "stand", 0f, 6f, 1), R("A gentleman", "rough", "revolver", "stand", 0f, -6f, 0),
                    R("His second", "rough", null, W("STARE_STOIC"), 4f, 3f), R("His second", "rough", null, W("STARE_STOIC"), -4f, -3f)
                },
                SideA = new[] { 0 }, SideB = new[] { 1 },
                Beats = new[]
                {
                    Say(0, 2, "scared", "It isn't too late. A word from either of you and we can all go to breakfast.", "Gentlemen, for the last time."),
                    Say(5000, 0, "threat", "He knows what he said.", "Ask him what he said about my sister."),
                    Say(8500, 1, "threat", "I said it, and I'll say it over you.", "Be about it, then."),
                    Say(11500, 3, "final", "On the word.", "Stand ready."),
                    Do(13500, "shoot")
                },
                Choices = new[] { C("part", "Step between them", 0, "part", 2, "Lord be thanked. Somebody finally did.", "Sir, whoever you are, I could kiss you."), C("leave", "Keep clear of it", 0, "leave", -1) },
                Thoughts = new[] { "My hand's steady. Why is my hand steady?", "Three weeks of letters, and it comes to this field.", "Nobody ever asks the second what he thinks." },
                News = "Two men fought with pistols at sunup out past {town}, and only one walked home."
            },
        };
    }
}
