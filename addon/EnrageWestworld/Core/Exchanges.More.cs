// More openers for the street's exchanges (the no-model run, item 6e; Lex, 2026-11-01): the hello that starts them, and
// for each worry three more ways to snap at its worst and to tell how it ended, good or bad, beside the one each had, so
// half the street's scenes don't open on the same words. Picked with the scene's seed (and, in game, passed over when
// said lately: Voice.FreshScope). All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Exchanges
    {
        private static readonly string[] MoreHello =
        {
            "Afternoon, {a}.", "{a}. How's the family?", "There you are, {a}.", "{a}! Just the one I wanted to see.",
            "Evening, {a}. Long day?", "{a}. You're up and about early.", "Well, if it isn't {a}.", "{a}. Fine weather, ain't it?",
            "You keeping well, {a}?", "{a}, you look like you swallowed a frog.", "Hold up, {a}. A minute.", "{a}. Been meaning to catch you.",
            "{a}. A fair day for it.", "{a}. Didn't see you at church.", "{a}! Where've you been hiding?", "Morning, {a}. Coffee any good where you've been?"
        };

        private static readonly Dictionary<string, string[]> MoreSnap = new Dictionary<string, string[]>
        {
            { "debt", new[] { "Don't ask me how I'm keeping. Ask {who}. He keeps the tally.", "Unless you've eleven dollars in your pocket, keep walking.", "Can't stop. If {who} sees me standing about he'll think I'm rich." } },
            { "fever", new[] { "Can't talk. {who}'s worse since last night.", "Not now. I've been up three nights with {who}. Three.", "If you've a dollar for the doctor, talk. Else let me pass." } },
            { "letter", new[] { "No. Nothing came. Thank you for asking. Stop asking.", "Everybody asks about the post. Nothing from {kin}. There.", "I've been to that post office four times. Don't you start." } },
            { "drought", new[] { "Fine weather? Fine for who? Not for corn.", "Don't you dare look at that sky and smile.", "Well's dry, cow's thin, and you want small talk." } },
            { "son", new[] { "Don't ask after {who}. I've nothing to tell you.", "Every time somebody says hello I think it's news of {who}. It never is.", "Leave it. {place} is a long way and it doesn't write." } },
            { "courting", new[] { "Not now. My head's somewhere else entirely.", "Never mind the collar. I know it's my good one.", "If you've come to tease, I've no humour for it today." } },
            { "feud", new[] { "Is that {who}'s horse at the rail? Then I'm not stopping.", "Ask {who} how I am. He'll tell you lies about it.", "Can't talk. Got a fence to stand guard over." } },
            { "bank", new[] { "Morning? The bank's morning, maybe. Not mine.", "Got a letter from the bank that wants an answer I haven't got.", "Leave me. I've sums to do that won't come right." } },
            { "drink", new[] { "Don't stand so close. You smell of the saloon.", "Keep walking. I'm holding on by my fingernails.", "Coffee. Only coffee. Don't offer me anything else." } },
            { "cards", new[] { "Mrs. {who}'s nephew is looking for me. Have you seen him?", "If you came to collect, you're third in line.", "Not now. Lady luck and I aren't speaking." } },
            { "savings", new[] { "Somebody's light-fingered round here. Mind your pockets.", "Money talk? No. Not today. Not ever.", "Leave me be. I'm counting what I haven't got." } },
            { "tooth", new[] { "Mmph. Can't. Tooth.", "Talk to my good side. The other side's on fire.", "Don't make me laugh. Please. It hurts to laugh." } },
            { "secret", new[] { "Who sent you? Nobody sent you? Then why stop me?", "I'm busy. Busy all day. Don't ask with what.", "You didn't see me here. Understand?" } },
            { "law", new[] { "Keep your voice down. Who's that by the sheriff's?", "Can't stop. Got somewhere to be. Somewhere far.", "Did anybody ask after me? Strangers, I mean." } }
        };

        private static readonly Dictionary<string, string[]> MoreGood = new Dictionary<string, string[]>
        {
            { "debt", new[] { "Owe nobody now. Not {who}, not a soul. Buy you a drink on it.", "{who} and I are square. Shook hands like gentlemen.", "Settled with {who}. I walked home on air." } },
            { "fever", new[] { "{who}'s mended! Running about the yard like nothing happened.", "Doctor says {who} will be fine. Fine! I made him say it twice.", "{who} ate a whole bowl of stew this morning. A whole bowl." } },
            { "letter", new[] { "{who} wrote! Pages of it. Mostly about the weather, God bless them.", "Word from {who} at last. All's well. I could kiss the postmaster.", "Letter came. {who}'s moved, that's all. Just moved." } },
            { "drought", new[] { "Rain last night! Did you hear it? I stood out in it like a fool.", "Creek's up. Corn's standing. I could sing.", "Well's full again. Drink as much as you like at my place." } },
            { "son", new[] { "{who} wrote from {place}. Doing well. Got himself a raise.", "{who} came home for a visit. Brought me tobacco.", "Heard from {who}. Healthy and saving money. Saving!" } },
            { "courting", new[] { "We're walking out. {who} and me. Can you believe it?", "{who} took my arm after church. In front of everybody.", "Asked {who} at last. Got a yes before I finished asking." } },
            { "feud", new[] { "{who} and me had a drink. Like the old days.", "Fence is settled. {who} and me drove the post in together.", "Made peace with {who}. Took us a year to remember why we were fighting." } },
            { "bank", new[] { "Note's paid! The farm's mine, free and clear.", "Banker says the house is mine another year. I could hug the man. Nearly did.", "Paid the bank and had change left over. Change!" } },
            { "drink", new[] { "Two months dry. Look, my hands are steady.", "Haven't touched a drop since spring. My wife smiles at me now.", "Off the drink. Sleep through the night. Remember my mornings." } },
            { "cards", new[] { "Won it all back and walked away. Walked away!", "Rent's paid a month ahead. Mrs. {who} near fainted.", "Gave up the table. Best bet I ever made." } },
            { "savings", new[] { "I'm off to {place} Monday. Ticket's in my boot.", "Saved enough for {place} and a bit over.", "{place}. I'm really going. I keep saying it to believe it." } },
            { "tooth", new[] { "Tooth's out and the pain's gone with it. Feel twenty years younger.", "Can chew on both sides again. You don't know what that's worth.", "Swelling's down. I can eat apples. Apples!" } },
            { "secret", new[] { "Worry's over. That's all I'll say. It's over.", "Put right what I'd done wrong. Nobody the wiser.", "Slept well last night. First time in months." } },
            { "law", new[] { "That fellow who was asking after me? Gone on the stage.", "Sheriff's got other fish to fry. Not me.", "Nobody's looking for me. Nobody at all. Hah." } }
        };

        private static readonly Dictionary<string, string[]> MoreBad = new Dictionary<string, string[]>
        {
            { "debt", new[] { "Couldn't pay {who}. He took the saddle. My father's saddle.", "{who} called it in. I've nothing left to call.", "Lost the mule to {who}. Children cried all night." } },
            { "fever", new[] { "Lost {who}. Last Tuesday. Don't say anything.", "Little grave on the rise. Go and look if you like. I can't.", "{who} is gone. The house is so quiet." } },
            { "letter", new[] { "Letter came. Not from {who}. About {who}.", "{who} won't be writing. Ever.", "Read the letter twice. It said the same thing both times." } },
            { "drought", new[] { "Corn's dead. Every row of it.", "Well's gone dry. Hauling water from the creek for the house now.", "Sold the team. Can't farm without them. Can't feed them either." } },
            { "son", new[] { "{who} isn't coming home. Not walking, anyhow.", "Man came from {place} with {who}'s things in a sack.", "{who}'s hurt. Bad. Out at {place}." } },
            { "courting", new[] { "{who} said no. Kindly. That's the worst of it.", "{who} is marrying the other one. Spring wedding.", "Didn't get to ask. Somebody asked first." } },
            { "feud", new[] { "{who} has the land now. The court gave it him.", "{who} and me will never speak again. Never.", "{who} shot my dog. Said it was in his hens." } },
            { "bank", new[] { "We're out by the end of the month. The bank has it.", "Strangers moving into my house Saturday.", "The bank won. Banks always win." } },
            { "drink", new[] { "Took a drink. Then all the drinks.", "I'm drinking again. Don't lecture me.", "Lost my job over it. The drink, I mean." } },
            { "cards", new[] { "Lost my room. Sleeping in the livery.", "Mrs. {who} has my things in the street.", "Bet the rent and lost. What else is there to say?" } },
            { "savings", new[] { "Somebody took my savings. All of it.", "{place} will have to wait. Maybe for ever.", "Had to spend the ticket money. Doctor's bill." } },
            { "tooth", new[] { "Barber broke it off. Root's still in there.", "Tooth's out but my jaw's worse than ever.", "Swelling won't go down. Can't eat a thing." } },
            { "secret", new[] { "Everybody knows now. Everybody.", "Lost my place over it. Packing tonight.", "They found out. Doesn't matter how." } },
            { "law", new[] { "They've got my name. And my face on paper.", "Deputy follows me everywhere now.", "Sheriff kept me overnight. Let go, for now. For now." } }
        };

        /// The worry's line for this exchange: its own one or one of the three more, by the seed.
        private static string WorryLine(Dictionary<string, string> one, Dictionary<string, string[]> more, string worry, string seed)
        {
            string[] extra;
            if (!more.TryGetValue(worry, out extra))
                return one[worry];
            return Voice.Pick(seed, new[] { one[worry] }.Concat(extra).ToArray());
        }

        private static IEnumerable<string> MoreLines()
        {
            return MoreHello.Concat(MoreSnap.Values.SelectMany(x => x)).Concat(MoreGood.Values.SelectMany(x => x)).Concat(MoreBad.Values.SelectMany(x => x));
        }
    }
}
