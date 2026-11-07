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
            "Fire won't catch. Damp kindling. Damp everything.", "Ate standing up at the stove. Sitting down is for people with time.", "Grits again. Salt's the only thing that changes.", "First light through the boards. Wish it'd stay this quiet all day.", "Egg cracked in my pocket on the way out. Smell of it all morning.", "Coffee's yesterday's grounds boiled twice. Tastes like a promise broken.",
            "Fried mush with a scrape of molasses. Fills the hole, doesn't decorate it.",
            "Salt pork sizzling. Spat at me twice. I've earned worse.",
            "Oatmeal stuck to the pot. Soaking it is a job for whoever's last.",
            "Cream's turned. Coffee black, then, and a face to match.",
            "Hotcakes this morning. Somebody in this house loves me, or the flour's going stale.",
            "Buttered a heel of bread with the back of a spoon. Knife's gone missing again.",
            "Ham from the smokehouse, cut thin. Thin enough to see tomorrow through.",
            "Johnnycake and buttermilk. Same as Grandmother's table, minus the scolding.",
            "Coffee mill sticks on the third turn. Every morning, third turn.",
            "Ate breakfast by lamplight. Feels like supper's ghost.",
            "Hen laid two this morning. Felt rich as a railroad baron.",
            "Burnt my tongue on the first sip. Tasted nothing else till dinner.",
            "Biscuits like stones. Dunked them till they gave up.",
            "No sugar left. Stirred the coffee anyway, out of habit.",
            "Children at the table too sleepy to fight. Best meal of the week, that.",
            "Leftover beans warmed in the skillet. Breakfast doesn't ask questions.",
        };

        private static readonly string[] Midday =
        {
            "Noon. Belly's been complaining since ten.", "Dinner pail's got bread and a pickle. Could eat the pail.", "Smell of somebody's stew through a window. Torture, that.",
            "Sun straight overhead and no shade worth the name.", "Half a day gone. The half that's left is the long half.", "Ate my dinner on an upturned bucket. Finest chair in the county.", "Too hot to eat. Ate anyway. Might not get the chance later.", "Bell for dinner down the street. Not my bell. Listened anyway.", "Dozed a minute after eating. Woke with my hat in my lap and drool on my chin.",
            "Cold potato and a wedge of cheese. Ate it in the shade of the wagon.",
            "Hard-boiled egg peeled with dirty thumbs. Tastes of the job.",
            "Whistle blew for dinner. Every hand dropped at once, like a spell broken.",
            "Pie at the eating house, ten cents a slice. Thought about it longer than it deserved.",
            "Washed the dust down with buttermilk from the springhouse. Cold as a creek stone.",
            "Noon meal's the big one. Pork and greens, then an hour I can barely stand up after.",
            "Sat on the hitching rail with a sandwich. Horse watched every bite.",
            "Fly landed in my coffee. Fished it out. Didn't waste the coffee.",
            "Sun right overhead. My shadow's hiding under my boots.",
            "Shared my bread with a dog. He's the better conversationalist.",
            "Too much dinner. The afternoon will be spent regretting the second helping.",
            "Midday and the bank's shut for an hour. Even money takes its dinner.",
            "Jar of cold tea wrapped in a wet sack. Better than gold at noon.",
            "The schoolhouse bell for recess. Little ones tearing out like calves let loose.",
            "Corn pone and a raw onion. Eyes watering, belly grateful.",
            "Fellow at the next table eats like he's mad at his plate.",
        };

        private static readonly string[] Supper =
        {
            "Beans tonight. Beans tomorrow. Beans for ever and ever, amen.", "Cornbread somewhere down the row. Walking slower to keep the smell.",
            "Feet up after supper. A minute. Then the chores.",
            "Smell of fried onions. Somebody in this town is eating well tonight.", "Supper's late. Supper's always late when you're hungry.", "Washed my hands for supper. Water went brown. Washed them again.", "Soup thin enough to read through. Read it anyway.", "Lamp lit, plate full, door shut. That's a good night, near enough.", "Ate fast. Nobody to talk to. Talking slows a meal down.",
            "Chicken and dumplings. Dumplings sank. Ate them anyway, like stones in gravy.",
            "Fried catfish from the creek. Bones in every bite, but it was free.",
            "Table set with the chipped plates. Good ones are for the preacher.",
            "Said grace too fast. Lord knows I was hungry.",
            "Potatoes again, three ways. Boiled, mashed, and wishing.",
            "Venison stew from the neighbor. Paid for it by hearing the hunting story twice.",
            "Lamp in the middle of the table, moths circling it like guests.",
            "Cleaned my plate with the last of the bread. Plate barely needs washing now.",
            "Mutton. Tastes of wool and mutton.",
            "Pot of greens simmering since three. House smells green clear to the rafters.",
            "Children kicking each other under the table. Pretending not to notice.",
            "Buttermilk pie for after. Worth every bean before it.",
            "Ate on the porch to keep the heat out of the kitchen. Mosquitoes ate on me.",
            "Corned beef and cabbage. The house will remember it for days.",
            "Somebody left the gate open while we ate. Spent dessert chasing a hog.",
            "Supper's cleared. Sat at the bare table a while, not ready to stand.",
        };

        private static readonly string[] LateNight =
        {
            "Eyes heavy as horseshoes. Bed's a mile off, and a narrow one.", "Lamp oil's low. Read the same line four times. Bed, then.",
            "Somebody's still singing in the saloon. Badly, and with feeling.", "Banked the fire. Sat with it a while longer than sense allows.", "Dog barking at nothing out past the edge of things. Dogs know.", "Owl on the roof. Calling for somebody. Not me, I hope.", "Late. The kind of late where every thought comes in its nightshirt.", "Blew out the lamp and lay listening to the house settle.",
            "Wound the clock and set it back on the mantel. Last chore, always.",
            "Boots by the door, coat on the nail. The day's hung up.",
            "Saloon's emptying out. Somebody singing their way home along a crooked line.",
            "Checked the latch twice. Then once more, for luck.",
            "Mending by the lamp. Needle keeps finding my thumb instead of the cloth.",
            "Said my prayers short. Too tired to list everybody.",
            "Pulled the quilt up and the cold found my feet anyway.",
            "Mouse in the wall again. Scratching at the same spot every night past nine.",
            "Turned the wick down to a bead. Not ready for the dark, not able to stay up.",
            "Heard the last train go through. Bed's the only place I'm going.",
            "Ten o'clock and still a lamp on in the doctor's window. Somebody's bad off.",
            "Put the cat out. Cat came back in by the window. We'll argue it in the morning.",
            "Bare feet on cold floorboards on the way to bed. Hopping like a fool.",
            "Wrote two lines of a letter and quit. Tomorrow, when I'm clever.",
            "Bed ropes need tightening. Sags in the middle like an old horse.",
            "Stars out through the window. Too tired to look properly. Looked anyway.",
        };

        private static readonly string[] SmallHours =
        {
            "Two in the morning and wide awake. Know every crack in that ceiling.", "Can't sleep. Counting what I owe instead of sheep. Sheep are cheaper.",
            "Whole town's asleep but me and the dogs.",
            "Coyotes singing up on the ridge. Sounds like they're laughing.", "Something moved in the yard. Lay still and listened. Wind, I'll call it.", "Got up for water. Stood at the window an hour. Don't know why.", "Rooster's confused. Crowed at the moon. There'll be other fools up tonight.", "Mind won't stop at this hour. Walks every room of my life, one after another.", "Clock ticks louder after midnight. Same clock. Louder.",
            "Three in the morning. The hour when every small worry puts its boots on.",
            "Woke with a cramp in my calf. Stood on the cold floor till it let go.",
            "Heard the stage rattle past in the dark. Who travels at this hour, and why?",
            "Rain on the roof now. Bucket under the leak's filling faster than I'd like.",
            "Lay awake doing sums. They come out the same at night, only worse.",
            "Somebody's horse got loose. Hoofbeats up the street and back, no rider.",
            "Moonlight on the quilt. Bright enough to read by, if I had the heart.",
            "Clock struck three and I counted along like a child.",
            "Thought about my mother's kitchen. Haven't stood in it in twenty years.",
            "Baby next door crying. Then a lamp, then a lullaby, then nothing.",
            "Wind rattling the stovepipe. Sounds like somebody wanting in.",
            "Got up and ate a cold biscuit in the dark. Felt like a thief in my own pantry.",
            "Fire's dead and the room's gone cold as a cellar.",
            "Dreamt I missed the train. Woke up glad I've nowhere to go.",
            "Lying still so the bed won't creak. Nobody to wake. Habit.",
            "The night watchman's lantern swinging along the street. Him and me, keeping the town.",
        };

        // ---- the week -----------------------------------------------------------------------------------

        private static readonly string[] Sunday =
        {
            "Sunday collar's choking me. Preacher'll go long. He always goes long.", "Sunday. No work, they say. Nobody told the cow.",
            "Church bell. Half the town walking one way, the other half pretending not to hear.",
            "Hymn I don't know the words to. Moved my lips like a fish.", "Sunday dinner. Chicken, if the chicken didn't run fast enough.", "Preacher looked at me during the part about sin. Coincidence, I hope.", "Day of rest. Rested my back, worked my conscience.", "Fresh shirt for church. Tight under the arms. Shrunk, or I grew.",
            "Sat through the sermon on hard oak. The Lord's day, my backside's penance.",
            "Children scrubbed pink and warned to sit still. Lasted till the second hymn.",
            "Collection plate came by. Put in a dime. Held it a second too long.",
            "Visiting in the churchyard after. Every family's business traded like coin.",
            "Afternoon nap with a newspaper over my face. Sabbath at its finest.",
            "Store's shut. Saloon's shut, officially. Back door's another matter.",
            "Walked out to the cemetery after dinner. Pulled weeds off the family stones.",
            "Lemonade on the porch, if there's ice. There's never ice.",
            "Sunday school lesson on the loaves and fishes. Wish I knew the trick of it.",
            "No hammering on the Sabbath. Fence will just have to lean another day.",
            "Courting couples walking the creek road at a proper distance. Mostly proper.",
            "Read the Bible an hour. Fell asleep in Leviticus. Most do.",
        };

        private static readonly string[] Monday = { "Monday. Back aches just knowing it.", "Wash day. Lye on the hands, steam in the face.",
            "Monday. Week's longer from this end.", "Washtub, wringer, line. Wind took a shirt over the fence again.", "Boss is sour on a Monday. Keep my head down till Wednesday.", "Monday morning and the whole town walks like it's carrying something.", "Wash water's cold before I'm done. Hands are red as a radish.",
            "Up at four to heat the copper. Wash day waits for nobody's back.",
            "Shaved soap into the boiler till my wrist ached. Clean clothes cost skin.",
            "Sunday's roast cold for Monday dinner. No time to cook with the wash on.",
            "Bluing in the rinse to make the whites whiter. Makes my hands blue, mostly.",
            "Washboard's worn a groove in my knuckles. Every Monday deepens it.",
            "Sheets on the line, white as new snow. Then the wind turned and brought the smokestack.",
            "Back to the shop after a day off. Tools feel heavier on a Monday.",
            "Monday ledger's full of Saturday's debts. Folks forgot what they owed by Sunday.",
            "Neighbor's wash was on the line before mine. I'll hear about it at church.",
            "Steam from the tubs fogged every window in the house.",
            "Wrung out the overalls till my arms shook. Wringer's broke, so it's me.",
            "Clothespins in my mouth, basket on my hip. Not a word out of me till noon.", };

        private static readonly string[] Tuesday = { "Tuesday. Nothing to love about it, nothing to hate. A day like a plain bowl.", "Tuesday, and Saturday's still a mountain off.", "Mended the harness. Tuesday work. No glory in it.", "Mail comes Tuesday. Stood by the window all the same.",
                "Ironing day. Sad irons on the stove, one heating, one going cold, one in my hand.",
                "Sprinkled Monday's wash and rolled it tight. Ironing's what Tuesday's for.",
                "Scorched a collar. Turned it so the scorch is on the inside. Nobody'll know.",
                "Tuesday's a working day with no excuse in it.",
                "Blacksmith shoes on Tuesdays. Took the mare in. Waited half the morning.",
                "Nothing happened today worth telling. Told it anyway, at length, at supper.",
                "Pressed my good shirt for Sunday already. Five days early. Ambition, that.",
                "Kitchen hot as a smithy from the irons. Opened the door. Flies came in.",
                "Week's barely started and I'm already behind it.",
                "Starch on the shirts so stiff they'd stand up by themselves.",
                "Quiet Tuesday. Mended a sock, fed the stock, quarreled with nobody.",
                "Grange meeting tonight. Same faces, same complaints, better pie.", };

        private static readonly string[] Wednesday = { "Middle of the week. Over the hump, they say. Feels like the hump.", "Wednesday. Market day in town. Everybody's selling, nobody's buying.", "Halfway to pay day. Halfway's a long way.", "Prayer meeting tonight. Haven't been in a month. Might go. Might not.",
                "Mending day. Basket of holes and one needle to answer them all.",
                "Patched the knees of the same trousers a third time. More patch than trousers now.",
                "Wednesday. The week's belt buckle. Sits right in the middle and pinches.",
                "Egg money counted out. Enough for thread. Not enough for the hat.",
                "Butter churned and pressed in the mold. Taking it to the store for credit.",
                "Wednesday's for getting caught up. Haven't been caught up since Easter.",
                "Got the garden weeded. By Saturday it'll look like I never touched it.",
                "Peddler comes through midweek with tin pans and lies. Bought a pan.",
                "Washed the windows. Can see the dirt on the street better now. No improvement.",
                "Schoolteacher held a spelling bee. Children can spell better than I ever could.",
                "Supper's whatever Monday and Tuesday left behind.",
                "Sharpened every blade in the house. Feels like getting ready for something.", };

        private static readonly string[] Thursday = { "Thursday. Tomorrow's Friday. Tomorrow's always a better day to be.", "Thursday and the larder's showing its shelves.", "Paper comes in Thursday from the city. Week-old news, fresh to me.", "Thursday. Body's tired, but it's the tired before rest, the good kind.",
                "Scrubbed the floors on my knees. Cleaner than Sunday, if anybody looked.",
                "Thursday. Close enough to Saturday to smell it.",
                "Store's restocking off the Thursday freight. Fresh coffee beans, if I'm quick.",
                "Beat the rugs on the line till the dust made a cloud the shape of a hen.",
                "Took the mare to the farrier. Slow day for him, so he talked the whole time.",
                "Ladies' sewing circle this afternoon. They stitch a quilt and unpick the whole town.",
                "Two days' labour left. Today's first, then the other.",
                "Wages are close enough now that I've started spending them in my head.",
                "Cleaned the lamp chimneys and trimmed the wicks. Regular as the moon, that chore.",
                "Traveling dentist set up at the hotel. Tooth's been aching since Monday.",
                "Hauled water for the garden all afternoon. Sky's clear as glass and twice as useless.",
                "Thursday night and the saloon's half full. Folks pacing themselves for Saturday.", };

        // midweek: each day its own pool again since FIVEDAY item 9 (sixteen lines each, several naming their day; the review)

        private static readonly string[] Friday = { "Rent's due tomorrow. Counting it again doesn't make it more.", "Friday. Wages tonight, if the foreman's sober. Rent tomorrow, whether or not.", "End of the week and the jar's lighter than the week was long.",
            "Friday, and every man in town walking an inch taller.", "Fish on Friday for the Catholics. Beans for the rest of us.", "Foreman's late with the pay. Every man on the job watching the road.", "Last day of the week. My back feels every one of them.",
            "Paid off at the mill. Folded the bills small and put them in my boot.",
            "Friday baking. Six loaves, a pie, and a burn on the wrist to prove it.",
            "Grocer's book open on the counter. Settled up. Owe nothing till Monday.",
            "Week's work done but the last hour. Last hour's always the longest.",
            "Pay envelope's lighter than I counted on. Docked for the day I was sick.",
            "Bought a sack of peppermints for the children. Spent a nickel being loved.",
            "Cleaning for Sunday starts now. Stove blacked, brass shined, tempers short.",
            "Saloon will take half the town's wages by midnight. Not mine. Not most of mine.",
            "Cowhands in from the ranches, paid and loud. Kept to the far side of the street.",
            "Put a dollar in the sugar tin before anything else could get it.",
            "Friday evening. Sat down without a chore waiting. Didn't trust it.",
            "Lodge meeting tonight. Funny hats and a secret handshake. Wouldn't miss it.", };

        private static readonly string[] Saturday =
        {
            "Saturday. Pay in my pocket, for an hour or two.", "Bath night. Water's grey by the third one in. I'm the third one in.",
            "Saturday crowd in town. Everybody pretending they came for flour.",
            "Barber's full on a Saturday. Everybody wants a shave for Sunday.", "Dance at the hall tonight. Got a clean shirt and two left feet.", "Farm wagons in from all over. Town smells of horses and money.", "Saturday night the saloon fills by six. Sunday morning the church don't.", "Half a day's work and a whole day's tiredness.",
            "Hauled the eggs and butter into town. Traded them for coffee, thread and a newspaper.",
            "Market's thick with wagons. Melons, hens in crates, a goat that won't stop shouting.",
            "Hair cut and shaved. Smell like bay rum. Feel like a banker.",
            "Beans in the pot by noon so Sunday needs no cooking.",
            "Stood at the dry goods window looking at a hat. Hat looked back. Neither of us moved.",
            "Farm boys racing horses down the main street. Sheriff pretends not to see it.",
            "Paid the doctor a dollar on account. He'll be dead before I'm square.",
            "Saturday's the only day town feels full. The rest of the week it's just us.",
            "Store stays open till nine on a Saturday. Clerk looks like he aged a year.",
            "Spent my pay in the order I'd planned. Then a little more, unplanned.",
            "Medicine show set up on the square. Tonic for everything. Cures a purse of its money.",
            "Blacked my boots and laid the Sunday clothes out on the chair.",
        };

        /// What the hour or the day of the week brings to mind, or null when nothing in particular does.
        private static string[] ForDay(int hour, int weekday, string seed)
        {
            string[] need = hour >= 5 && hour < 8 ? Breakfast : hour >= 11 && hour < 14 ? Midday : hour >= 17 && hour < 20 ? Supper
                          : hour >= 21 ? LateNight : hour < 4 ? SmallHours : null;
            string[] day = hour < 7 || hour >= 21 ? null : weekday == 0 ? Sunday : weekday == 1 ? Monday : weekday == 2 ? Tuesday : weekday == 3 ? Wednesday : weekday == 4 ? Thursday : weekday == 5 ? Friday : weekday == 6 ? Saturday : null;
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
            "Talk of the Boers and the British coming to blows down in Africa. Good luck to the farmers.", "Everybody's buying a bicycle back East. Can't sit a horse, so they sit a wheel.", "McKinley's face in the paper again. Looks like a man who's never been hungry.", "Doctor in Saint Denis has a machine that sees your bones. Wouldn't let him look at mine.", "Old soldier at the store still fighting the war between the states. Over the cracker box, mostly.", 
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
