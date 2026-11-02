// What the day itself brings to mind: the meal that's late or the one just eaten, the small hours, payday and
// bath night, Sunday collars, wash day; the year it is (1899, with the century about to turn, or the epilogue's
// 1907, motorcars and telephones); and what the player did in this town lately, as the town tells it (nobody
// names him). All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        // ---- the clock ----------------------------------------------------------------------------------

        private static readonly string[] Breakfast =
        {
            "Coffee's bitter and the bacon's burnt. Best part of the day, still.", "Belly growling before the rooster. Biscuit in my coat for later.",
            "Cold hands round a hot cup. Steam on my face. A minute of peace.",
            "Fire won't catch. Damp kindling. Damp everything.", "Ate standing up at the stove. Sitting down is for people with time.", "Grits again. Salt's the only thing that changes.", "First light through the boards. Wish it'd stay this quiet all day.", "Egg cracked in my pocket on the way out. Smell of it all morning.", "Coffee's yesterday's grounds boiled twice. Tastes like a promise broken."
        };

        private static readonly string[] Midday =
        {
            "Noon. Belly's been complaining since ten.", "Dinner pail's got bread and a pickle. Could eat the pail.", "Smell of somebody's stew through a window. Torture, that.",
            "Sun straight overhead and no shade worth the name.", "Half a day gone. The half that's left is the long half.", "Ate my dinner on an upturned bucket. Finest chair in the county.", "Too hot to eat. Ate anyway. Might not get the chance later.", "Bell for dinner down the street. Not my bell. Listened anyway.", "Dozed a minute after eating. Woke with my hat in my lap and drool on my chin."
        };

        private static readonly string[] Supper =
        {
            "Beans tonight. Beans tomorrow. Beans for ever and ever, amen.", "Cornbread somewhere down the row. Walking slower to keep the smell.",
            "Feet up after supper. A minute. Then the chores.",
            "Smell of fried onions. Somebody in this town is eating well tonight.", "Supper's late. Supper's always late when you're hungry.", "Washed my hands for supper. Water went brown. Washed them again.", "Soup thin enough to read through. Read it anyway.", "Lamp lit, plate full, door shut. That's a good night, near enough.", "Ate fast. Nobody to talk to. Talking slows a meal down."
        };

        private static readonly string[] LateNight =
        {
            "Eyes heavy as horseshoes. Bed's a mile off, and a narrow one.", "Lamp oil's low. Read the same line four times. Bed, then.",
            "Somebody's still singing in the saloon. Badly, and with feeling.", "Banked the fire. Sat with it a while longer than sense allows.", "Dog barking at nothing out past the edge of things. Dogs know.", "Owl on the roof. Calling for somebody. Not me, I hope.", "Late. The kind of late where every thought comes in its nightshirt.", "Blew out the lamp and lay listening to the house settle."
        };

        private static readonly string[] SmallHours =
        {
            "Two in the morning and wide awake. Know every crack in that ceiling.", "Can't sleep. Counting what I owe instead of sheep. Sheep are cheaper.",
            "Whole town's asleep but me and the dogs.",
            "Coyotes singing up on the ridge. Sounds like they're laughing.", "Something moved in the yard. Lay still and listened. Wind, I'll call it.", "Got up for water. Stood at the window an hour. Don't know why.", "Rooster's confused. Crowed at the moon. There'll be other fools up tonight.", "Mind won't stop at this hour. Walks every room of my life, one after another.", "Clock ticks louder after midnight. Same clock. Louder."
        };

        // ---- the week -----------------------------------------------------------------------------------

        private static readonly string[] Sunday =
        {
            "Sunday collar's choking me. Preacher'll go long. He always goes long.", "Sunday. No work, they say. Nobody told the cow.",
            "Church bell. Half the town walking one way, the other half pretending not to hear.",
            "Hymn I don't know the words to. Moved my lips like a fish.", "Sunday dinner. Chicken, if the chicken didn't run fast enough.", "Preacher looked at me during the part about sin. Coincidence, I hope.", "Day of rest. Rested my back, worked my conscience.", "Fresh shirt for church. Tight under the arms. Shrunk, or I grew."
        };

        private static readonly string[] Monday = { "Monday. Back aches just knowing it.", "Wash day. Lye on the hands, steam in the face.",
            "Monday. Week's longer from this end.", "Washtub, wringer, line. Wind took a shirt over the fence again.", "Boss is sour on a Monday. Keep my head down till Wednesday.", "Monday morning and the whole town walks like it's carrying something.", "Wash water's cold before I'm done. Hands are red as a radish." };

        private static readonly string[] Tuesday = { "Tuesday. Nothing to love about it, nothing to hate. A day like a plain bowl.", "Tuesday, and Saturday's still a mountain off.", "Mended the harness. Tuesday work. No glory in it.", "Mail comes Tuesday. Stood by the window all the same." };

        private static readonly string[] Wednesday = { "Middle of the week. Over the hump, they say. Feels like the hump.", "Wednesday. Market day in town. Everybody's selling, nobody's buying.", "Halfway to pay day. Halfway's a long way.", "Prayer meeting tonight. Haven't been in a month. Might go. Might not." };

        private static readonly string[] Thursday = { "Thursday. Tomorrow's Friday. Tomorrow's always a better day to be.", "Thursday and the larder's showing its shelves.", "Paper comes in Thursday from the city. Week-old news, fresh to me.", "Thursday. Body's tired, but it's the tired before rest, the good kind." };

        // midweek: the three days' lines in one pool, so a whole town doesn't share four lines on a Tuesday
        private static readonly string[] Midweek = Tuesday.Concat(Wednesday).Concat(Thursday).ToArray();

        private static readonly string[] Friday = { "Rent's due tomorrow. Counting it again doesn't make it more.", "Friday. Wages tonight, if the foreman's sober. Rent tomorrow, whether or not.", "End of the week and the jar's lighter than the week was long.",
            "Friday, and every man in town walking an inch taller.", "Fish on Friday for the Catholics. Beans for the rest of us.", "Foreman's late with the pay. Every man on the job watching the road.", "Last day of the week. My back feels every one of them." };

        private static readonly string[] Saturday =
        {
            "Saturday. Pay in my pocket, for an hour or two.", "Bath night. Water's grey by the third one in. I'm the third one in.",
            "Saturday crowd in town. Everybody pretending they came for flour.",
            "Barber's full on a Saturday. Everybody wants a shave for Sunday.", "Dance at the hall tonight. Got a clean shirt and two left feet.", "Farm wagons in from all over. Town smells of horses and money.", "Saturday night the saloon fills by six. Sunday morning the church don't.", "Half a day's work and a whole day's tiredness."
        };

        /// What the hour or the day of the week brings to mind, or null when nothing in particular does.
        private static string[] ForDay(int hour, int weekday, string seed)
        {
            string[] need = hour >= 5 && hour < 8 ? Breakfast : hour >= 11 && hour < 14 ? Midday : hour >= 17 && hour < 20 ? Supper
                          : hour >= 21 ? LateNight : hour < 4 ? SmallHours : null;
            string[] day = hour < 7 || hour >= 21 ? null : weekday == 0 ? Sunday : weekday == 1 ? Monday : weekday >= 2 && weekday <= 4 ? Midweek : weekday == 5 ? Friday : weekday == 6 ? Saturday : null;
            if (need != null && day != null)
                return Voice.Chance(seed + "wd", 50) ? need : day;
            return need ?? day;
        }

        // ---- the year -----------------------------------------------------------------------------------

        private static readonly string[] Era1899 =
        {
            "Next year the century turns. Preacher says it means something. Means new calendars.",
            "Nephew came back from Cuba with the shakes and a medal. Keeps the medal in a sock.",
            "Men selling their farms to chase gold in the Klondike. Gold or frostbite. Mostly frostbite.",
            "Fella rode a bicycle down the street in {town}. Horses lost their minds. So did I.",
            "War in the Philippines now. Never heard of the place a year ago. Now it's in the paper every day.",
            "Boers fighting the British down in Africa now. Paper calls it a war of farmers. Good luck to the farmers.", "Everybody's buying a bicycle back East. Can't sit a horse, so they sit a wheel.", "McKinley's face in the paper again. Looks like a man who's never been hungry.", "Doctor in Saint Denis has a machine that sees your bones. Wouldn't let him look at mine.", "Old soldier at the store still fighting the war between the states. Over the cracker box, mostly.", 
            "Railroad's coming everywhere. Soon a man won't be able to get lost if he tries.", "Heard a phonograph at the hall. A dead man singing out of a horn. Gave me a turn."
        };

        private static readonly string[] Era1907 =
        {
            "Motorcar came through {town} this morning. The stink of it hung on after it was gone.",
            "Telephone wire strung to the store. Ring it and somebody in Saint Denis answers. Unnatural.",
            "Paper says they're still rebuilding San Francisco. Whole city shook to the ground.",
            "Roosevelt in the paper again, grinning like he owns the country. Maybe he does.",
            "Moving pictures in Saint Denis. Five cents to watch a train come right at you.",
            "The old gangs are gone. Hanged or old. Roads are safer. Roads are duller.",
            "Two brothers from Ohio built a machine that flies. Paper says so. Paper says lots.",
            "Banks back East closing their doors. People in lines round the block for their own money.", "Indian Territory's to be a state, they say. Oklahoma. Somebody's land again, either way.", "Navy's sending its ships round the whole world, painted white. Showing off, if you ask me.", "Every week another family from the old country gets off the train. Can't follow half what they say.", "Fella at the store says in twenty years nobody'll own a horse. Fella at the store is a fool.", "Saw a lady in {town} wearing a skirt that showed her boots. Times have changed."
        };

        // ---- the town's talk ----------------------------------------------------------------------------

        private static readonly string[] TalkMurder =
        {
            "Man shot dead in {town}. Blood's scrubbed off the boards. Still walk round the spot.",
            "Somebody killed a man here and walked off calm. That's what folks keep saying. Calm.",
            "Locking the door at night since the killing. Never locked it before. Key's stiff."
        };

        private static readonly string[] TalkKill =
        {
            "Gunfight in {town}. Dead man's still the talk of the store.", "Shooting in the street lately. Kept the children in. Kept myself in, too."
        };

        private static readonly string[] TalkLaw =
        {
            "Lawman shot in {town}. Sheriff's jumpy. Looks at every stranger like a debt.", "Deputy's widow at the store today. Bought nothing. Just stood there."
        };

        private static readonly string[] TalkRobbery =
        {
            "Store got robbed. Prices went up the next day. Somebody always pays twice.", "Robbery in broad day. A gun in a man's face over a till. Over tin money."
        };

        private static readonly string[] TalkTheft =
        {
            "Things going missing round {town}. Checking my pockets more than my watch.",
            "Somebody went through a dead man's pockets right there in the street. Picked him like a chicken."
        };

        private static readonly string[] TalkHorse =
        {
            "Horse stolen off the rail. Man walked home crying. Grown man. Horse was all he had.",
            "Horse thief in the county, they say. Checked my mare twice before breakfast.",
            "A man came through asking after a bay with a white sock. Lost or stolen, he wouldn't say."
        };

        private static readonly string[] TalkAssault =
        {
            "Fella got beat in the street. Teeth in the dirt. Nobody stepped in. I didn't either.",
            "Bruises on the man who got knocked down, purple as plums. A stranger did it, they say."
        };

        /// The town's talk about the newest of the player's recent deeds here (he's never named: a stranger did it).
        private static string TownTalk(Person p, List<Deed> deeds, string seed, List<string> told)
        {
            string[] pool;
            switch (deeds[0].Kind)
            {
                case "murder": pool = TalkMurder; break;
                case "lawkill": pool = TalkLaw; break;
                case "robbery": pool = TalkRobbery; break;
                case "theft":
                case "looting": pool = TalkTheft; break;
                case "horsetheft": pool = TalkHorse; break;
                case "assault": pool = TalkAssault; break;
                default: pool = TalkKill; break;
            }
            return Pick(pool, p, seed, told);
        }

        /// The kinds of the player's deeds a town talks about for a few days.
        public static readonly HashSet<string> TalkedAbout = new HashSet<string> { "kill", "murder", "lawkill", "robbery", "theft", "looting", "horsetheft", "assault" };

        /// Every line of the day, the year and the town's talk, for the tests.
        public static IEnumerable<string> AllDayLines()
        {
            foreach (string[] pool in new[] { Breakfast, Midday, Supper, LateNight, SmallHours, Sunday, Monday, Friday, Saturday, Era1899, Era1907,
                                              TalkMurder, TalkKill, TalkLaw, TalkRobbery, TalkTheft, TalkHorse, TalkAssault, Again })
                foreach (string line in pool)
                    yield return line;
        }
    }
}
