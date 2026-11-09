// Someone you know, out of place (FIVEDAY2 item 21; built on the regulars and their worries, seen in game). A person from a town, met far from it, for a reason their
// own trouble gives. Why it reads true: recognition out of context is the strongest sign a world goes on without you. In town you expect them; on an empty
// road, it means they had somewhere to be, and a life you'd only seen the edge of. All words original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Wayside
    {
        public const string KnownId = "known";

        /// What they're doing out here, by the trouble they're living through.
        private static string[] KnownWhy(string worry)
        {
            switch (worry)
            {
                case "debt": return new[] { "I owe more than I can stand. Don't tell them you saw me.", "Figured a man can't be dunned if he can't be found." };
                case "courting": return new[] { "There's someone waiting for me at the crossing. Her father doesn't know.", "I'm going to ask. I've been walking two hours working out how." };
                case "feud": return new[] { "Going to see a man about a fence. Don't ask how it ends.", "I told my wife I was going for feed." };
                case "fever": return new[] { "Going for a doctor who'll come. Ours won't.", "I'm walking to the next town for quinine. It's all gone at ours." };
                case "letter": return new[] { "I walked out to the station myself. There's got to be a letter.", "Three weeks. Somebody's holding my mail." };
                case "son": return new[] { "Looking for my boy. He wrote he was working a camp out this way.", "Have you seen a young fellow, tall, talks too much? He's mine." };
                case "savings": return new[] { "Going to see if my money's still where I buried it.", "Don't laugh. A bank's just a man with a key." };
                case "cards": return new[] { "Lost the lot at cards. Walking home's the punishment.", "I'd have had him on the last hand. I'd have had him." };
                default: return new[] { "I needed to walk, that's all. It gets close in town.", "Don't mind me. I just needed to see something else for a day." };
            }
        }

        private static readonly string[] KnownThoughts =
        {
            "Of all the people to meet out here.",
            "Say something ordinary. Say something about the weather.",
            "If he tells them in town I'll be hearing of it till I'm buried."
        };

        /// A kind made for this one person, to stage like any other. `who`: their name; `town`: their home; `yourName`: null if they don't know it.
        public static WaysideKind Known(string who, bool woman, string town, string worry, string yourName, string seed)
        {
            string first = Person.FirstOf(who) ?? who;
            string greet = Voice.Pick(seed + "|greet", yourName != null ? yourName + "? Well, I'll be. What are you doing out here?" : "It's you. From " + town + ". What are you doing out here?", "I know you. You're a long way from " + town + ".", "Lord, you gave me a start. I thought I was alone out here.");
            return new WaysideKind
            {
                Id = KnownId, Mark = first, Test = "Someone you know, far from home", From = 0, To = 24, Weight = 0,
                Cast = new[] { R(who, "town", null, "stand", 0f, 0f, -1, woman) },
                Beats = new[]
                {
                    Say(0, 0, "thanks", greet),
                    Say(6000, 0, "thanks", KnownWhy(worry))
                },
                Choices = new[]
                {
                    C("give", "Give " + first + " a few dollars", 300, "help", 0, "I'll remember this. I'll get it back to you, every cent.", "You didn't have to do that. You know that, don't you."),
                    C("home", "Tell " + first + " to go home", 0, "leave", 0, "Home. Yes. You make it sound easy.", "Maybe. Maybe tomorrow."),
                    C("leave", "Say you never saw them", 0, "help", 0, "Thank you. That's worth more than money, today.")
                },
                Thoughts = KnownThoughts
            };
        }

        /// Every line of it, for the tests.
        public static IEnumerable<string> KnownLines()
        {
            foreach (string w in new[] { "debt", "courting", "feud", "fever", "letter", "son", "savings", "cards", "other" })
                foreach (string l in KnownWhy(w))
                    yield return l;
            foreach (string l in KnownThoughts)
                yield return l;
        }
    }
}
