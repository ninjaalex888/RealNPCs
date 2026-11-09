// Fights away from town (FIVEDAY2 item 16; built on the fists of the street fights and brawls Lex has seen: "seem good"). Two sides and a couple of
// people watching, somewhere nobody's going to send for the law. Why it reads true: out of town nobody has to back down for the sake of who's
// looking, so what's said stops being a show and turns into fists very fast; and a stranger stopping to watch changes who is winning.
namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        private static readonly WaysideKind[] Fights =
        {
            new WaysideKind
            {
                Id = "teamsters", Mark = "Men fighting", Test = "By the road: two teamsters come to blows", From = 7, To = 20, OnRoad = true, Weight = 14,
                Cast = new[]
                {
                    R("A teamster", "rough", null, "stand", 1.3f, 0f, 1), R("A teamster", "rough", null, "stand", -1.3f, 0f, 0),
                    R("A man watching", "rough", null, W("STARE_STOIC"), 0f, 5f), R("A man watching", "rough", null, W("SMOKE"), 4f, -4f)
                },
                SideA = new[] { 0 }, SideB = new[] { 1 },
                Beats = new[]
                {
                    Say(0, 0, "threat", "You took the middle of the road on purpose.", "Forty miles behind you, eating your dust."),
                    Say(3500, 1, "threat", "I took what was there. Move your mules.", "There's a ditch if you want your own road."),
                    Say(7000, 0, "threat", "Say that with your hands.", "Get down here and say it."),
                    Do(9000, "fight"),
                    Say(12000, 2, "fight", "Go on! Mind his left!", "That's it! Again!"),
                    Say(19000, 3, "fight", "Two bits says the short one.", "He's got no wind left.")
                },
                Choices = new[]
                {
                    C("part", "Part them", 0, "part", 0, "All right. All right! I'm done.", "He started it. Tell him he started it."),
                    C("bet", "Put a dollar on one of them", 100, "bet", 3, "Done. Your dollar and mine."),
                    C("leave", "Leave them to it", 0, "leave", -1)
                },
                Thoughts = new[] { "One clean one on the jaw and I can go home.", "Forty miles of his dust. Forty.", "Should've let him pass. Too late now." },
                News = "Two teamsters laid each other out on the {town} road, and nobody can say what over."
            },
            new WaysideKind
            {
                Id = "hunters", Mark = "Men arguing", Test = "In the wilds: two hunters over a kill", From = 6, To = 19, Weight = 10,
                Cast = new[]
                {
                    R("A hunter", "rough", "rifle", "stand", 1.5f, 0f, 1), R("A hunter", "rough", "rifle", "stand", -1.5f, 0f, 0), R("A boy's father", "rough", null, W("STARE_STOIC"), 3f, 4f)
                },
                SideA = new[] { 0 }, SideB = new[] { 1 },
                Beats = new[]
                {
                    Say(0, 0, "threat", "My shot dropped it. Yours went in the trees.", "Two days I tracked that animal."),
                    Say(3500, 1, "threat", "Look where the hole is, then tell me that.", "Tracked it right past my bullet, did you?"),
                    Say(7500, 2, "scared", "Put the guns down, the pair of you. It's meat."),
                    Do(9500, "fight"),
                    Say(16000, 2, "fight", "Not the rifles! Hands, damn you, hands!")
                },
                Choices = new[]
                {
                    C("part", "Tell them to split it", 0, "part", 2, "Half each. Lord, was that so hard.", "Now that's sense. Hear him?"),
                    C("leave", "Leave them to it", 0, "leave", -1)
                },
                Threat = "fight",
                Thoughts = new[] { "He'll say it was his in every saloon from here to the river.", "Two days. Two days and a wet bedroll.", "If he reaches for that rifle I won't wait." },
                News = "Two hunters near {town} fell out over a kill, and it took a third man to stop it going further."
            },
            new WaysideKind
            {
                Id = "cards", Mark = "Men quarrelling", Test = "A camp: the card game goes wrong", From = 18, To = 5, Weight = 10,
                Cast = new[]
                {
                    R("A card player", "rough", null, W("SIT_GROUND"), 1.4f, 0f, 1), R("A card player", "rough", null, W("SIT_GROUND"), -1.4f, 0f, 0),
                    R("A card player", "rough", null, W("SIT_GROUND"), 0f, 1.6f), R("A man at the edge of it", "rough", null, W("SMOKE"), 4f, 3f)
                },
                SideA = new[] { 0 }, SideB = new[] { 1, 2 },
                Beats = new[]
                {
                    Say(0, 0, "threat", "That's the third ace I've seen tonight.", "Show me your sleeve."),
                    Say(3500, 1, "threat", "Show you what? I'll show you the road.", "You lost. Men lose."),
                    Say(7000, 2, "scared", "Sit down, the both of you. It's four dollars."),
                    Do(9000, "fight"),
                    Say(14000, 3, "fight", "Mind the coffee! Lord, mind the coffee!")
                },
                Choices = new[]
                {
                    C("part", "Tell them to deal again", 0, "part", 2, "He's right. Deal them again. And deal them straight."),
                    C("leave", "Leave them to it", 0, "leave", -1)
                },
                Thoughts = new[] { "Three aces. I can count, you know.", "Four dollars. A man's nose for four dollars.", "Finish it quick and take the pot while they're at it." },
                News = "A card game at a camp out past {town} ended in a fight, and a pot that nobody can account for."
            },
        };
    }
}
