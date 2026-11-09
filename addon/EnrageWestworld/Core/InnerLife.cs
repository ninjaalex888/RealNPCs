// What an ordinary person is thinking, drawn from who they are rather than from one shared list: their trade,
// their town, their temperament, their faith and the hour; the worry their life is going through
// (InnerLife.Worries.cs); the day of the week, the meal they're missing, the year, and what the player did in
// their town lately (InnerLife.Days.cs). Each thought comes from one of those and is remembered so the same
// person doesn't think it again soon. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        /// What the game day brings, for a thought: set up by the engine (it knows the save) before asking.
        public sealed class Context
        {
            public long GameDay = -1;              // whole game days, for the worry a life is going through; -1 unknown
            public List<Deed> TownDeeds;           // the player's recent bad deeds in this person's town, newest first
            public List<SceneTie> Ties;                 // street scenes this person had lately, with whom (Ties.cs), newest first
            public List<SceneTie> TownTies;             // the town's talk: scenes between two others here lately, newest first
            public List<WorryLink> Linked;              // someone else's worry this person is the other end of (Links.cs)
            public Func<string, bool> Dead;             // whether someone in those has died since, by id (may be null)
            public string Weather;                      // how it feels out (WeatherOf): rain, storm, snow, dust, fog, sun, grey; null unknown
            public SkyNow Sky;                          // what's really out, with the temperature (Core/WeatherTruth.cs); null when it isn't read
            public bool Indoors;                        // under a roof (the weather doesn't touch them; the place does)
            public string Place;                        // the kind of place they're in: saloon, church, store, hotel... (null: the street)
            public Witnessed JustSaw;                        // a scene that just played out in front of them, if any (InnerLife.Now.cs)
            public List<Nearby> Nearby;                 // the other people in sight, as this one sees them
            public long Now = -1;                       // the game minute (for how long ago today's ties were); -1 unknown
            public string Routine;                      // a regular's routine here, now (Routines.Key: "saloon|evening"...), for a thought of it; null for anyone else
            public bool RoutineOdd;                     // ...and they're somewhere their routine wouldn't put them (Routines.Odd)
            public string TownMood;                     // the town's month (TownLedgers): grieving, fearful, prospering; null steady or unknown

            public bool IsDead(string id)
            {
                return Dead != null && id != null && Dead(id);
            }
        }

        /// Set by the game side from the clock: 0 Sunday .. 6 Saturday (-1 unknown), and the year (1907 in the epilogue).
        public static int Weekday = -1;
        public static int ClockYear = 1899;
        public static int ClockMonth;   // 1 to 12 (0 unknown): what was in the papers by now (Times.cs)
        public static int ClockDay;     // 1 to 31 (0 unknown): the real calendar (Core/Calendar.cs)
        public static int CalendarYear; // the clock's own year (John's may still read 1899: dates and weekdays follow it; the era wording ClockYear)
        public static bool Historical = true;   // [World] HistoricalAccuracy: the times' talk, the year's lines and the real calendar are thought and said; off, none of it (Lex's brief, 2026-11-03: a strict toggle)

        // ---- trade ----------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByTrade = new Dictionary<string, string[]>
        {
            { "farm", new[] {
                "North fence is down again. Cattle know it before I do.", "If the well goes dry before August, I'm done.", "Hog got out. I'll find him by the smell.",
                "Hands are cracked to the bone. Salve costs more than the corn it saves.", "Rain by Thursday, the almanac says. The almanac lies.", "Hay's cut. Hay's turned. Hay's rotting if this weather holds.",
                "Mule kicked the milk pail over. Laughed so I wouldn't cry.", "Planting by the moon, like my father did. The moon never paid a bank.", "Smell of turned earth. Only honest smell there is.",
                "Mouse sign all through the seed corn. Count what's left and pray over it.",
                "Cow's due and the calf won't come. Something's turned wrong in there.",
                "Plough point's worn to a stub. Smith wants a dollar to draw it out. I want the dollar more.",
                "Hired man quit at noon. Took his wages and my good hat." } },
            { "town", new[] {
                "Ought to settle that account at the general store before he asks.", "Same faces on the same corners. I could name every one in my sleep.",
                "Stage is late. Stage is always late, and always my fault.", "Church supper Sunday. Somebody'll bring that awful pie again.", "Rent's due Friday. Wages come Saturday. Math was never my strength.", "Another quiet day in {town}. I ought to be grateful.",
                "Barber holds a razor to your throat and tells you his politics. Nobody argues.", "New sign over the hotel. Same bedbugs under it.", "In {town}, everybody's business is everybody's business.",
                "Somebody painted the hotel front white. Now the rest of {town} looks dirty.",
                "Neighbour bought a parlour organ. Now I hear the same hymn through the wall every night.",
                "Ladies' Aid meets Thursday. Somebody will be asked to carry chairs. It'll be me.",
                "Paper's meant to come out Fridays. It comes out when the editor's sober." } },
            { "old", new[] {
                "Knees say rain. Knees were right before the telegraph was.", "Used to be a man could ride from here to the river and not see a fence.", "Everyone's in such a hurry now. Where do they think they're going?",
                "I buried two wives and a brother. Still can't abide cold coffee.", "That boy's got my father's walk. Don't know whose boy he is.", "Bench is warm. Sun is out. That's a good day, at my age.",
                "Hold the paper at arm's length to read it. Arms are getting shorter.", "Outlived the doctor who said I'd never see fifty. Went to his funeral. Ate his widow's ham.", "Young folks walk so fast. I walked fast once. Got nowhere sooner.",
                "Can't hear the preacher any more. Don't need to. Heard him the first forty years.",
                "Wrote down where I put my spectacles. Then lost the paper.",
                "Every funeral now is somebody I sat beside at school. The school's gone as well.",
                "Grandchildren talk so fast. I nod and smile. It's usually the right answer." } },
            { "gentry", new[] {
                "Third letter to my lawyer. He answers like a man paid by the hour. He is.", "The dust in this town settles on everything. Even my opinion of it.", "Dinner at seven. I'll wear the grey.",
                "I did not come west for this. I came for the air. The air is mostly horses.", "One must be seen at church. One need not listen.", "If the bank holds, we hold. If it doesn't, I'll be selling china.",
                "Cook gave notice. Says the house is haunted. It's only my mother-in-law.", "Kid gloves in this dust. Absurd. Wouldn't be seen without them.", "Brought calling cards west. Nobody here to call on.",
                "Had the piano shipped from Boston. It arrived in tune. The town did not.",
                "One sends east for decent gloves and waits a month for the wrong size.",
                "The hired girl burned the toast again. She's a dear. The toast is not.",
                "The bank's people to dinner Friday. Laugh at the right stories. None of the wrong ones." } },
            { "working", new[] {
                "Back aches. Shift's got four hours yet.", "Foreman looked at me like I owed him something. I don't. Not till payday.", "A man could make a living at this if the wage was honest.",
                "Dinner pail's light. Pay is lighter.", "Soon as I've got fifty dollars saved, I'm out of here. Been saying fifty for a year.", "Boots wore through at the toe. Stuffing them with newsprint again.",
                "Blisters on the blisters. Palms like an old saddle.", "Foreman's whistle. Hear it in my sleep. Wake up already tired.", "Ate standing up again. Chairs are for men who own them.",
                "Paid in scrip only the company store will take. The store knows it, too.",
                "Bread and a cold potato. Ate it at ten. Hungry till six.",
                "Lost a fingernail to a crate this morning. Foreman said mind the crates.",
                "Union man talked to some of us behind the shed. Didn't sign. Thought about it all shift." } },
            { "drummer", Drummers.Thoughts },   // (FIVEDAY R1)
            { "card", new[] {
                "Luck runs like a river. Mine's dry upstream.", "Man across the table blinks when he bluffs. Tonight I will own his watch.", "One more hand. One more hand and I'll walk away. Same as last night.",
                "Never play a man who's drinking water.", "Dealt a pair of sixes. Sixes are a poor man's promise.", "Owe the house more than I'll admit. Ace in the hole will fix that.",
                "Shuffle, cut, deal. Hands know it better than they know a prayer.", "Lost my watch to a man with a glass eye. He winked at me. I think.", "Marked deck in my coat. Never used it. A comfort, like a derringer.",
                "Three kings, and I yawned. The yawn won me forty dollars.",
                "Faro box in this house is crooked. Play anyway. I like to know how I'm losing.",
                "Lucky silver dollar in my vest. Rubbed it so long the eagle's gone bald.",
                "Dealer's thumb rests on the top card too long. Not sure he knows I've seen it." } },
            { "rough", new[] {
                "Nobody in this town looks at me twice. That's how I like it.", "Hungry. Always hungry. Hunger makes a man think things.", "Sheriff's eyes follow me. Let them. I've done nothing today.",
                "Owed twenty dollars. Collecting soon, one way or another.", "Pretty horse at the rail. Don't look at it. Don't look.", "Rain's coming. Better find a roof that isn't mine.",
                "Knife's gone dull. Whet it tonight. Not for anything. A dull knife's an insult.", "Caught my face in a shop window. Looked like trouble. Didn't mind.", "Slept in a hayloft. Itching in places I can't scratch in company.",
                "Boots belonged to a man who won't be needing them. Fit near enough.",
                "Two dollars in my boot and a grudge worth more.",
                "Woman pulled her child in off the walk when I passed. Can't say she's wrong.",
                "Sleep with my back to the wall and one boot on. Old habit, older reasons." } },
            { "trapper", new[] {
                "Pelts are thin this year. Fox know something I don't.", "Haven't spoken to anyone in nine days. Town's loud.", "Trader will cheat me by a dime a pelt. Or a quarter. Depends on his mood.",
                "Smoke from the east means weather. Smell it? Weather.", "Traps want checking before dusk. Wolves were close.", "Boots stink of the creek. People step back. That's fine.",
                "Beaver's near gone from the creeks. Hats back east. Hats.", "Slept under a ledge with a fox two yards off. Neither of us minded.", "Town food sits like a stone. Miss my own fire.",
                "Fresh marten tracks along the ridge. My hands are itching for the line.",
                "Bought coffee, salt, lead and a looking glass. The looking glass was a mistake.",
                "Mended the snowshoe webbing with gut by the fire. Fingers stink of it for a week.",
                "Talked to the mule all winter. He's heard every opinion I own." } },
            { "girl", new[] {
                "Rent's due. Rent's always due.", "Nobody in this town ever says my name in daylight.", "Feet are killing me. Shoes are killing me. The whole evening is killing me.",
                "Saving up. Another year, maybe two. I'll be somewhere that doesn't know me.", "Nice man came in last night. Left a good tip and a worse story.", "If the preacher looks at me one more time with that face...",
                "Lace torn again. Mend it by lamplight. Smile by lamplight too.", "Same tune on the piano every night. Could sing it backwards. Some nights I do.", "Boy asked me to marry him last night. Boys will promise anything on whiskey.",
                "Rouge from a tin that says Paris. Ohio, more like.",
                "Laugh at his joke. Laugh at the next one. My face aches by midnight.",
                "Letters home say I'm a seamstress. Mother's proud. I keep a needle in my bag to make it true.",
                "Cold tea in my glass at whiskey prices. The house keeps the difference. I keep a nickel." } },
            { "soldier", new[] {
                "Orders are orders. Can't remember who gave this one.", "Drilled the same maneuver till it's the only thing my legs know.", "Fort food again. If this is duty, give me a different duty.",
                "Keep the line. Keep the line. Keep the line.", "Sergeant says don't think. Easy for a man who gets paid to.", "Boots wet, rifle heavy, and somebody back home still thinks I'm a hero.",
                "Rifle's cleaner than my bunk. Sergeant's priorities.", "Letter from home asks when I'll be done. Nobody tells a private anything.", "Saluted a horse in the dark. Thought it was the captain. Hope nobody saw.",
                "Polished every button twice. Sergeant found a thumbprint on the third one down.",
                "Hardtack you could shingle a roof with. Soaked it in coffee. Still cracked a tooth.",
                "Thirteen dollars a month, and the army takes half of it back for boots.",
                "Bugle at five. Even the horses groan." } },
            { "law", new[] {
                "Badge gets heavy on a hot day.", "Everyone in town knows my name and none of them wave.", "Quiet shift. Quiet shifts frighten me more than shooting.",
                "Wonder if I'd still be a good man without this star on my chest.", "Every drunk swears he's innocent. Some are.", "Keys on my belt, and not one opens the door I want.",
                "Posters curl in the sun faster than I catch the men on them.", "Coffee at the jail is worse than the cells. Prisoners say so. They're right.", "Half the faces in town I've arrested. Other half I will. Hard to make friends.",
                "Writing up one drunk takes longer than catching him. Rather wrestle him twice.",
                "Council wants a quiet town and won't pay for a second deputy.",
                "Every man I lock up has a mother who comes to stand at the window.",
                "Took a knife off a boy no older than fourteen. He thanked me. Didn't know what to say to that." } },
            { "plain", new[] {
                "Need to be somewhere. Can't remember where.", "Wonder if it'll rain.", "Hungry. Supper's two hours off, and I'm not paying for a second meal.",
                "Should've written Mother last month. Now it's a bigger letter.", "Heat makes everyone short-tempered. Me too.", "Who decided it should be so far to anywhere?",
                "Button's off my coat. Can't find it. Can't buy one. A pin, then.", "Wonder what folks in the city are doing right now. Something better.", "Long road, short money. Same as ever.",
                "Letter came with a black border. Haven't opened it yet.",
                "Lost a dime through a hole in my pocket. Sewed the pocket. The dime stays lost.",
                "Mended the same chair leg three times. Next time it's kindling.",
                "Somebody's frying onions. Can't see where. Can't think of anything else." } }
        };

        // ---- the hour -------------------------------------------------------------------------------

        private static readonly string[] Dawn =
        {
            "Early light on the rooftops. Quiet enough to hear my own coffee.", "Rooster's shouting at nobody. Rooster's been shouting since four.", "Cold this early. Joints complaining before I've stood up.",
            "Mist on the road. Never trust the first hour. It's too pretty.", "Smell of woodsmoke and horse. Mornings are honest at least.",
            "Frost on the trough. Broke it with my elbow. Cold goes up the arm and stays.", "First wagon of the day. Wheels sound louder before the town's awake.", "Light comes up grey, then gold. Same as yesterday. Still worth a look.",
            "Milk pail rings empty, then fuller. Best sound I know before six.",
            "Hens off the roost and already quarreling over the same worm.",
            "Lit the lamp to dress, blew it out by the time I'd found my boots.",
            "Dew soaked through my socks crossing the yard. Wet till noon now.",
            "Pumped the handle twenty times before the water came. Pump's older than me.",
            "The baker's chimney's going. Bread by seven if he's sober.",
            "Sky over the ridge looks like a peach somebody bruised.",
            "Splashed my face at the basin. Woke up all at once, against my will.",
            "Mule won't look at me till he's had his oats. Can't say I blame him.",
            "Mourning doves on the fence wire, three in a row, gossiping.",
            "Stove's still warm from last night's coals. Small mercy, gladly taken.",
            "Up before the sun. Never once beat it to anything worth having.",
            "Cows lowing at the gate like I owe them money.",
            "Chamber pot to empty, then the day can start properly.",
            "Fog's sitting in the low field. Fence posts poking out like stubble.",
            "Whistle from the mill at half past five. Hate it. Set my day by it.",
            "Bread rising under a cloth on the sill. Smells like somebody's patience.",
            "Washed in the creek. Cold took my breath and kept it a while.",
        };

        private static readonly string[] Day =
        {
            "Sun's high. Shade's a luxury.", "Dust everywhere. In my teeth, in my tea.", "Noon bell. Everyone pretending they're not hungry.",
            "Heat sits on the street like a hat. Nobody wants to move.", "Long day and not half over.",
            "Flies at the horse's eyes. Flies at mine. We're both too hot to fight them.", "Shadows gone small under the boardwalk. Nowhere to stand that isn't baking.", "Middle of the day and the street's gone still. Even the dogs have given up.",
            "Sweat running down my back like it's late for something.",
            "Blacksmith's hammer going all afternoon. Ringing in my teeth by three.",
            "Water barrel's warm as soup. Drank it anyway.",
            "Store's doing slow trade. Clerk's asleep on the flour sacks.",
            "Hat brim's soaked through. Wrung it out and put it back on.",
            "Wagon stuck axle-deep at the crossing. Six men watching, one pushing.",
            "Afternoon drags. Clock in the bank window hasn't moved, I'd swear.",
            "Stopped under the elm for a breath. Took three. Back to it.",
            "Grasshoppers jumping out of the ditch with every step, like corn in a hot pan.",
            "Collar's rubbed my neck raw. Should've starched it less or not at all.",
            "Somebody's laundry snapping on a line. Only breeze in town is over there.",
            "Errands half done. The half that's left all need money.",
            "Road's so bright it hurts to look at. Squinting my way to the post office.",
            "The well rope's hot to the touch. Even the bucket's sweating.",
            "Children playing marbles in the dirt by the feed store. Wish I had their worries.",
            "Mule stopped dead in the street and would not be argued with.",
            "Ice wagon came by. Followed it a block just to stand in the drip.",
            "Two o'clock. The hour when everything takes twice as long.",
        };

        private static readonly string[] Evening =
        {
            "Light's going gold. Nice for a minute, then it's work again.", "Supper smells drift up the street. Somebody has bacon.", "Lamps being lit down the row. First one's always the saloon.",
            "Cooler now. Now a man can think.", "Dogs bark, children called indoors. Evening, same as every one.",
            "Bats out over the livery. They keep better hours than I do.", "Shutters going closed down the row. The store first, the saloon never.", "Smoke from every chimney at once. Town smells like supper and wet wood.",
            "Cows coming in on their own. They know the hour better than the clock.",
            "Sun's sinking behind the water tower. Paints it like a church window.",
            "Chores done but one. There's always one.",
            "Crickets starting up in the grass. Loud as a congregation.",
            "Sat on the step with my boots off. Feet thanked me.",
            "Folks out on porches now, rocking and saying nothing much.",
            "Swallows diving over the creek, taking the gnats before I can complain of them.",
            "The day's dust settling. Air smells like warm boards and sage.",
            "Shopkeeper sweeping his stretch of boardwalk. Sweeps it toward the next fellow's.",
            "Lamp chimney's black with soot. Cleaned it with an old stocking.",
            "Kettle on for the dishes. Half the evening goes to washing what the other half dirtied.",
            "Children chasing fireflies in the lane. Caught one myself. Let it go.",
            "Wind's turned and gone chill. Brings the smell of the stockyard with it, sadly.",
            "Last train whistle off to the east. Somebody leaving. Somebody always is.",
            "Red sky. Fair tomorrow, if the old saying's worth a cent.",
            "Closed the henhouse door before the fox could think of it.",
            "Long shadows across the street, all pointing the same way. Home, I'd guess.",
            "Neighbor's fiddle through the open window. Scratchy, but it's company.",
        };

        private static readonly string[] Night =
        {
            "Too dark to see my hand. Too quiet to hear my own thoughts.", "Piano's still going at the saloon. Tune never changes.", "Moon's up. Wolves are, too.",
            "Should be home by now. Should be a lot of things.", "Something about night makes the town look like a stage set after the play.",
            "Stars thick as spilled salt. Too many to be looking down at me.", "One lamp lit at the far end of the street. Somebody can't sleep either.", "Owl in the cottonwoods. Says the same thing every night and I still listen.",
            "Wind working at a loose shingle. Tap, tap, all night, like a debt collector.",
            "Street's empty but for a drunk talking kindly to a post.",
            "Lamp's out in the jailhouse window. Sheriff's either asleep or gone.",
            "Cold coming up off the ground now. Should've brought the heavier coat.",
            "Horse snorting in the livery. Dreaming of oats, I hope, not wolves.",
            "Couldn't find the outhouse in the dark. Found the woodpile instead, with my shin.",
            "Train went through without stopping. Lit windows sliding by full of strangers.",
            "Frogs down at the creek, going like a sawmill.",
            "Saloon door swings. Laughter spills out, then gets shut back in.",
            "Nights like this I count the stars I can name. Comes to four.",
            "Somewhere a baby crying and a voice hushing it. Same tune in every house.",
            "Dark so thick out past the last fence it feels like a wall.",
            "Mosquitoes found me. They work nights, same as the lawless.",
            "A late rider passed at a walk. Didn't call out. Neither did I.",
            "Coal oil smell in the room long after the lamp's out.",
            "Cat on the fence, eyes like two lit matches.",
            "Church steeple black against the sky. Looks taller at night. Looks stern.",
            "Wind's dropped. Quiet enough to hear the windmill thinking about turning.",
        };

        // ---- temperament, faith ---------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByTemper = new Dictionary<string, string[]>
        {
            { "Hothead", new[] { "One more look from that fellow and I'll answer it with my fist.", "Wasn't raised to take an insult. Wasn't raised to forgive one either.", "Counting to ten. Never gets past six.",
                "Broke a chair over nothing last month. Paid for the chair. It wasn't nothing.",
                "Somebody whistled behind me. If it was for me, he'll hear about it.",
                "Knuckles still split from Saturday. They itch like they want another go.",
                "Mother said my temper would hang me. Not yet, Mother.",
                "That fellow at the rail keeps smiling to himself. If it's at me, he'll smile different soon." } },
            { "Coward", new[] { "Is that man armed? He looks armed. They all look armed.", "Best not to be noticed. Best to be a fence post.", "Heard a shot last night. Haven't left the house the same since.",
                "Went the long way round to miss the saloon door. A mile out of my way. My legs can spare it.",
                "Said yes when I meant no. Again. Easier than watching his face go hard.",
                "Back to the wall and I still jumped when the stove ticked.",
                "Somebody shouted my name across the street. Heart near quit. Only the grocer, with my change.",
                "Practised what I'd say if he came back. Said it to the mirror. The mirror wasn't scared either." } },
            { "Proud", new[] { "They don't know what I've done. They don't need to.", "I'd sooner starve than ask Hollis for a loan.", "A man's name is what he's got. Mine's still clean.",
                "Frayed coat to church and my chin up. Let them look at the chin.",
                "He short-changed me two cents. Said nothing. Let him carry the two cents home.",
                "My father put up half the houses on this street. Nobody remembers. I do.",
                "Slipped in the mud outside the hotel. Got up slow, like I'd meant to sit.",
                "Never once asked anybody for anything. It's lonely. It's mine." } },
            { "Cold", new[] { "People talk too much. I listen. I remember.", "Don't need friends. Need a quiet room and a fair price.", "Smiled at him. It cost me nothing. It bought me plenty.",
                "Neighbour's barn burned. Took a pie over, as you do. Paced off his acres while I was there.",
                "Kept my eyes down at the funeral. The will's read Tuesday.",
                "He told me his troubles for an hour. I learned where he keeps his money.",
                "Don't hate anybody. Hating's work. I keep a list instead.",
                "Everyone's somebody's fool. Worth knowing whose." } },
            { "Greedy", new[] { "If he'll pay four, he'll pay five. Ask for five.", "Dollar here, dollar there. A man gets rich by never being full.", "Count it again. Count it twice. It still isn't enough.",
                "Found a nickel in the street. Walked the rest of the way with my eyes on the ground.",
                "Charged the widow full price. She'd think less of the goods otherwise.",
                "Lent him ten dollars at a dollar a week. He'll be paying it off grey.",
                "Sold the old horse as a six-year-old. He was nine. He walked like six on the day.",
                "Ate free at the wake and took two biscuits home in my hat." } },
            { "Loyal", new[] { "Promised I'd look in on her. I will. I keep my promises.", "Blood or not, they're mine. Anyone says different answers to me.", "Never left a friend behind. Not going to start.",
                "Sat up all night with a neighbour's colicky horse. He'd have done the same for mine.",
                "Somebody ran my brother down at the store. I let it pass. Once.",
                "Still write to my old schoolteacher at Christmas. She's past ninety. Writes back in pencil now.",
                "Owe a man a favour from ten years back. He hasn't called it in. I'm ready when he does.",
                "Got him home drunk and into bed with his boots off. He won't remember. Doesn't matter." } },
            { "Drifter", new[] { "Been here three weeks. Longer than most places. Feet are twitching.", "Every town's the same town in a different hat.", "Might head north. Might not. Depends on the wind.",
                "Know every water hole from here to the border better than any man's name.",
                "Somebody asked where I'm from. Gave them a different town than last time.",
                "New boots. My feet are already making plans.",
                "Man offered steady work through spring. Thanked him kindly. Gone by morning.",
                "Never unpack the saddlebags all the way. Saves time later." } }
        };

        private static readonly Dictionary<string, string[]> ByFaith = new Dictionary<string, string[]>
        {
            { "Devout", new[] { "Said grace on the porch this morning. Said it again for the horses.", "The Lord provides. He's slow about it, but He provides.", "Prayed for rain. Prayed for forgiveness. Mostly for the second.",
                "A chapter every night by the lamp. Read the same one twice some nights. It says something new.",
                "Kept the Sabbath and let the hay lie in the rain. Lord, that was a hard one.",
                "Put the man who cheated me in my prayers. Through my teeth, but he's in them.",
                "Knelt so long on the plank floor my knees have their own opinion of the Almighty.",
                "Sermon was poor today. Forgave the preacher. Then asked pardon for judging him." } },
            { "Godless", new[] { "Hear them singing from the church. Sounds nice. Wouldn't stay.", "If there's a Heaven, I hope it has better coffee.", "Preacher says everything's for a reason. I wish I could see it.",
                "Sunday morning, every soul in church and the whole street mine. That's my service.",
                "Mother prayed every night of her life. Died the same as anybody.",
                "Preacher tipped his hat to me. I tipped mine back. Neither of us meant a thing by it.",
                "Watched a good man drown and a thief live to eighty. Do the sums yourself.",
                "Wouldn't mind being wrong about it. Nothing's turned up yet to make me." } },
            { "Other", new[] { "Church on Sunday. Rest of the week I'm on my own.", "Said a prayer, mostly for habit. Nobody answered, which was fair.", "God and I are on speaking terms. Mostly I do the speaking.",
                "Go to church for the singing. The sermon's extra.",
                "Crossed myself when the hearse went by. Not even Catholic. Seemed only polite.",
                "Say grace when there's company. Just eat when there isn't.",
                "A dime in the plate at Easter and another at Christmas. Keeps the account open.",
                "Not sure about Heaven. Sure about the preacher's wife's pie. That's what I go for." } }
        };

        // ---- towns ----------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByTown = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "Smell of cattle on the wind. You get used to it. You never get fond of it.", "Stockyards bawling again. Never sleeps, this town.", "Another train, another load of beef. Nobody asks where it goes.",
                "Mud in the main street to the boot tops. Lost a shoe in it last spring. Probably still down there.",
                "Cowhands paid off on Saturday, broke by Sunday, sorry by Monday.",
                "Auction man talks so fast the cattle are sold before they know it.",
                "Snow on the peaks to the north. Winter comes down that pass like it's owed money." } },
            { "Rhodes", new[] { "Gray or Braithwaite, doesn't matter. Everyone here picks a side and pays for it.", "Cotton and sugar and old grudges. That's Rhodes.", "Heard shooting out past the plantation. Heard it, didn't hear it, you understand.",
                "Red clay on everything. Wash a shirt white and it comes out pink.",
                "Old men on the porch still fighting the war, and losing it slower every year.",
                "Heat comes up off the cane fields wet as a dishrag.",
                "Cicadas so loud at noon I can't hear the church bell." } },
            { "Saint Denis", new[] { "Trolley bell, horse cart, brass band. A man can't think in this town.", "Ships in the harbor. Somebody's getting rich off the cargo, and it isn't me.", "Streets so crowded a man could be robbed and never know whose hand.",
                "Factory smoke over the river so thick the sun comes up brown.",
                "Mosquitoes off the bayou big enough to carry off a man's purse.",
                "Nickel coffee at the market, half chicory. Still the best thing I've had all week.",
                "They bury the dead above ground here. The water won't keep them down." } },
            { "Blackwater", new[] { "Everything here costs twice. Even the quiet.", "Ferry horn means someone important arrived. Always someone important.", "New buildings every month. Same old people inside them.",
                "Streets swept every morning here. Feels like being watched by a broom.",
                "Wind off the water of an evening. Only thing in Blackwater nobody charges for.",
                "Folks here talk about the future like it's a train they've bought tickets for.",
                "Brick fronts, painted signs, a schoolhouse with glass windows. Somebody means for this place to last." } },
            { "Strawberry", new[] { "Smell of cut pine. Sawdust in the soup.", "Mill whistle at six. Mill whistle at six. Mill whistle at six.", "Hills are pretty. Town's the kind of pretty where you keep your hand on your wallet.",
                "Mountain air's meant to be good for the chest. Mine aches from it.",
                "Logs going down to the mill all day, rumbling like slow thunder.",
                "Sawyer lost a finger at the mill Tuesday. Back at work Wednesday. Says nine's plenty.",
                "Night comes early behind the mountains. Lamps lit by five." } },
            { "Annesburg", new[] { "Coal dust in the lungs, coal dust in the stew.", "Mine whistle. Count the shifts, count the dead, count again.", "Company owns the houses, the store, and half my opinions.",
                "Breaker boys come off the shift black to the eyes. Some of them can't be ten.",
                "Scrub the step every morning. Coal's back on it by noon.",
                "Company doctor says it's only a cough. Company doctor says that about everything.",
                "Hills all round, green as anything, and the tipple standing black between them." } },
            { "Van Horn", new[] { "Dock air, bad whiskey, men who don't ask names. Suits some.", "Wouldn't walk the back streets after dark. Wouldn't walk the front ones either.", "Everyone in Van Horn is passing through. Only some are honest about it.",
                "Boardwalk's half rotten. Locals know which planks to step on. Strangers learn.",
                "Smell of fish guts and tar on everybody. On me too, now.",
                "Gulls fight over scraps on the dock. So do the men, come evening.",
                "Ship came in last night, gone before dawn. Nobody saw it. Everybody's drinking better today." } },
            { "Armadillo", new[] { "Dry as a bone. Even the gossip is dusty.", "Fever took half the street last year. We say little about it.", "Sun on the adobe. Shade is the only civilised thing out here.",
                "Water tower's the tallest thing for twenty miles. Everybody looks at it twice a day.",
                "Shook a scorpion out of my boot this morning. He looked put out.",
                "Rattler under the boardwalk again. Nobody'll go under after him.",
                "Train stops ten minutes for water. Passengers look out at us like we're the zoo." } },
            { "Tumbleweed", new[] { "Used to be a town. Now it's a place where things happened.", "Wind through the empty windows. Sounds like someone arguing in another room.", "Nobody stays. Those that do know why they can't leave.",
                "Counted the chimneys with smoke this morning. Four. Last winter it was seven.",
                "Mine's been played out for years. Still hear the stamp mill in my sleep.",
                "Saloon's open two nights a week now. The other nights it's just the dog.",
                "Sand drifts against the doors of the empty houses. Nobody digs them out." } }
        };

        // ---- the body, the mirror, the biases, the age, the mask (from the writing brief) ---------------

        /// What the body is saying right now: tiredness, hunger, a craving, weather on the skin.
        private static readonly string[] Body =
        {
            "Blister on the left heel, hot as a coin. Boots weren't made for this road.", "Stomach folded in on itself. Last real meal was Tuesday. Or Monday.",
            "Craving tobacco. Fingers keep patting the empty pocket.", "Wind straight through the coat. Bones aching like a bad tooth.", "Sweat down the spine. Collar like a wet rope.",
            "Eyes gritty from the dust. Can't think when they didn't sting.", "Back's one long knot. Slept on a floor again.", "Tongue stuck to the roof of my mouth. Water. Water.",
            "Tooth at the back throbbing with my heartbeat. Dentist's a day's ride. Whiskey's nearer.", "Hands cracked at the knuckles. Cold and lye soap. They bleed when I make a fist.", "Head pounding behind the eyes. Too much sun or too little sleep. Both.", "Knee locks on the stairs. Thirty-odd years of stairs, and it's keeping count.",
            "Ears ringing since the thunder yesterday. Hear it in bed. Hear it in church.", "Wool itching at the shoulders. Can't reach it, can't leave it alone.", "Hungry enough to eat the horse's oats. Looked at them twice.", "Chilblains on every finger. Can't do up a button before noon."
        };

        /// How they think they look to everyone else.
        private static readonly string[] Mirror =
        {
            "Saw how the grocer looked at my sleeve. Frayed. He noticed. They always notice.", "Straighten up. Hat square. Let them think I own something.",
            "Laughed at the banker's joke a beat too late. Did they see?", "Wearing the good coat in this heat. The good coat says somebody.",
            "They go quiet when I pass. Respect or pity. Can't tell which is worse.", "Walk like there's somewhere to be. Nobody asks a busy man for a favour.",
            "That man's boots cost more than my horse. Doesn't mean he can ride.",
            "Took my hat off to the lady and my hair stood up. Could feel her not laughing.", "Patched the elbow myself. From across the street it passes. Up close it's a patch.", "Speak slower in the store. Slow sounds like a man with money.", "Caught my face in the window. Older than I carry it.", "Boots blacked this morning. Let them look at the boots and not the rest."
        };

        /// Fear of losing, the eye that finds the threat first, the comfort of thinking the poor earned it.
        private static readonly string[] Bias =
        {
            "Count the coins twice. Once more. Still short.", "One sneer on Tuesday. Still tasting it. The compliment? Gone by supper.",
            "That beggar's young enough to work. Nobody handed me anything. Why him?", "Lose the horse, lose the job. Lose the job, lose the room. Lose the",
            "Every stranger's a tax collector until proven otherwise.", "Prices up a penny. Nobody says a word. I'll say it later, to the dog.",
            "Folks are poor for a reason. Say it often enough, it holds.",
            "A hundred good days and the one bad one is the one I'd swear by.", "Stranger smiled at me. Wanted something. They always want something.", "Kept the dollar back from the collection. The Lord has plenty. I've a dollar."
        };

        /// The age pressing in: smoke, disease, the law, the bank.
        private static readonly string[] Age =
        {
            "Smoke from the new railhead. Takes a little more sky every year.", "Heard the Pinkertons are asking questions again.",
            "Cough in the next room all night. Consumption, they say. Nobody says it loud.", "Cholera down the river. Boil the water. Boil everything.",
            "Telegraph poles marching in from the east. Soon there won't be a place left to hide.", "Mills, mines, machines. Hands like mine aren't worth what they were.",
            "Bank wants the note by spring. Spring is a hungry word.", "The law's got faster horses and longer arms now. Don't like where the wire leads.",
            "New bridge going up downriver. Iron, not timber. Nothing's built to be fixed any more.", "Paper says motor carriages in Saint Denis. Horses laughed at them, I expect. So did I."
        };

        /// What they show and what they feel.
        private static readonly string[] Mask =
        {
            "Smile. Nod. Wish he'd choke on his own coin.", "Polite as pie to the preacher. Thinking about his sister-in-law, God forgive me.", "Voice steady. Hands shaking in my pockets.",
            "Telling a joke to the table. Thinking about the letter in my coat.", "Everyone says I'm calm. I'm counting the exits.", "Tough face for the street. Terrified of the doctor on Thursday.",
            "Shook his hand and counted the fingers after. Pleasant as you please.", "Laughed at the story. Second time I've heard it. He tells it worse each time.", "Asked after her health like I cared. Wrote it down in my head: ask again Tuesday.", "Steady voice at the counter. Walked out and leaned on the wall till it passed."
        };

        // ---- the stranger on the road ------------------------------------------------------------------

        /// What a person can see of the player right now.
        public sealed class PlayerLook
        {
            public bool Near;       // close enough to study
            public bool Armed;      // a gun in hand
            public bool Mounted;    // on a horse
            public int Seen;        // times this person has met him (a town regular), counting now
            public Moment Now;      // the moment this person is in (Core/Moments.cs), for its quiet side: a drink, the saddle, their work; null unknown
        }

        /// A town regular who has met him before, seeing him again.
        private static readonly string[] Again =
        {
            "Him again. Same hat. Same way of standing, like the street owes him money.",
            "Seen this one before. I don't forget a man who looks at me that long.",
            "Back in {town}, is he. I'd started to think I dreamed him.",
            "That stranger again. Nodded at me last time. Will he this time?",
            "I know that face. Know it from here. Every time, from right here.",
            "Third time, or fourth. I've stopped pretending I don't notice him.",
            "Back again, that one. The town's got used to him. I haven't.", "That stranger, and still a stranger. Some men stay one on purpose.",
            "Him again. The dog knows him now. The dog's a better judge than me.", "Back once more. Every time he comes the paper has something new in it.", "That one again. I'd give a dollar to know what he's after in this town.", "Seen him so often now I'd miss him if he stopped coming. Odd thing to think.",
            // the no-model run, item 6 (2026-11-01): more of them, so a town regular's second look isn't the first one again
            "Him again. I'd know that walk in a crowd of a hundred.", "There he is. Every time I see him I tell myself I won't look.", "Back, and nobody sent for him. That's how it always is with him.",
            "Same stranger. Same way of reading the street before he steps into it.", "Him. The last time he was here, something happened after. Can't say what. Something.", "I've seen him enough to know his horse before I know him.",
            "He comes, he goes, the town talks a week, then he comes again.", "That one. I described him to my wife once. She knew him at once.", "Back. Looks older than last time, or I do.",
            "Him again. I keep meaning to ask where he's from, and I keep not asking.", "There's that man. Children stop playing when he passes. I notice that.", "He's back. I'd hoped the road had swallowed him. I'd hoped it hadn't, too.",
            "That stranger. Walks in like he's come to collect, every time.", "Knew he'd be back. Men like that circle the same towns like hawks.", "Him again. Bought a drink last time and never touched it. I remember things like that.",
            "Back in {town}. The sheriff will find a reason to stand on his porch now.", "I'd swear he's looking for someone. The same faces he studies every time.", "That one again. Gives the same short nod. I've started giving it back.",
            "Saw him out past the last houses at dawn once. Didn't think he slept.", "Him. Familiar the way a scar's familiar.", "Every time he's here the night's louder. Could be chance. Could be him.",
            "He's back, and the street goes careful. I go careful with it.", "Knew the hat before I knew the man. Now I know both.", "That fellow again. Mud of three counties on him this time.",
            "Never stays long enough to be a neighbour, never gone long enough to be forgotten.", "That one's back. Wonder if he ever thinks of this town when he's gone. Doubt it.", "Seen him come in quiet and leave loud. Today it's quiet, so far.",
            "Him again, and me pretending to be busy again.",
            "Him again. Last time he tipped his hat to me. I've wondered since whether he meant anything by it.",
            "There he is again. Still knows nobody's name here. Half of {town} knows his.",
            "Back again. Stops at the store window same as last time. Never goes in.",
            "Him. Last time he was here I told him the way to the river road. Wonder did he find it.",
            "Back, and bolder this time. Or I'm less afraid of him. One or the other.",
            "Somebody will tell me what he's done this visit by morning. Somebody always does.",
            "There he is. Never looks my way till he's past me, and then he looks back once.",
            "Back in {town}. Last time he paid for a stranger's supper and left before the thanks came.",
            "There's the man I've a name for in my head. Not a kind name. Not a cruel one either.",
            "He's back, and I'm glad of it, and I couldn't tell you why I'm glad.",
            "He nodded to me first, this time. I'll be turning that over for a week.",
            "The same stranger. Folk here have quit asking who he is. Now they ask what he wants.",
            "That one's back. Thinner, I'd say. Wherever he goes between times, it doesn't feed him.",
            "Him again. I've told people about him so often he grew in the telling. Here he is, man-sized.",
            "I mark it in the almanac every time he rides in. Don't ask me why I keep count.",
            "Last time he was in {town}, the bank had a new lock by the week's end.",
            "Know that coat. Torn at the pocket then. Somebody's mended it since.",
            "There's the stranger who asked me the time and never looked at his own watch.",
            "Rode a dappled mare last visit. Rides her still. Loyal, at least.",
            "His horse lifted its head at me. Remembers the apple I gave it, maybe.",
            "More beard on him than I remember. Hides more of him.",
            "Heard he was killed up in the hills. Seems the hills heard wrong.",
            "Said good morning to me once, months back. I've been owed another since.",
            "Gave him wrong directions last time. Hope he's forgotten.",
            "Word was he'd gone west for good. West gave him back.",
            "Last I saw him he was bleeding at the knuckles. Healed now.",
            "Scar on his cheek that wasn't there before. Somebody got close.",
            "Lord, him. And me in my shirtsleeves.",
            "He'll remember me. Or he won't, and I'll feel a fool for waiting on it.",
            "Last visit he stood my round at the saloon. I never thanked him.",
            "Folk at the feed store say he shot a man at Valentine. Folk say a great deal.",
            "He looks rested. Never seen him rested. Makes him look younger, and worse.",
            "A month gone, and he walks in as if he'd stepped out for tobacco.",
            "Will he nod? Last time he did. I keep a tally.",
            "Odd relief, seeing him. A known danger sits easier than a new one.",
            "Dread first, then I place him. Then dread again, but smaller.",
            "Moves a touch stiffer than last time. Something caught up with him.",
            "His horse is new. I liked the old one. Wonder what became of it.",
            "Told my sister about him at Christmas. Now here he is, in the flesh.",
            "Bought cartridges by the box, last time. Wonder if they're spent.",
            "He's in {town} again and the saloon keeper's already counting the glasses.",
            "I know that whistle. Same three notes he calls the horse with.",
            "Paper had a fellow like him in it last month. A fellow like him, it said.",
            "Been near a year. Didn't think I'd place him so quick.",
            "Same chipped tooth when he grins. Not that he grins much.",
            "Practised what to say if he spoke to me. Now I've forgotten every word.",
            "Nobody in {town} sent word he was coming. Men like him don't send word.",
            "He stood in the rain last time, under no roof, like it suited him.",
            "Back with more dust on him and less money, by the look.",
            "His hat's newer. Lost the old one somewhere. Wonder how.",
            "My daughter asked after him once. I told her to hush.",
            "He looked straight at me. He knows me. Heaven help me, he knows me.",
            "Doesn't know me from Adam. Good. Let it stay that way.",
            "Sold him coffee last time and he paid double. Didn't wait for change.",
            "The tale since is he broke a man's arm in Rhodes. Looks capable.",
            "Hard to look at him and not think of the night the stable burned.",
            "Every time he's back, I lock the cellar. Habit now.",
            "His mare's gone grey at the muzzle. Years of road on both of them.",
            "Quieter this time. Less swagger. Something's taken it out of him.",
            "More swagger this time, if anything. Done well since, then.",
            "Remember his voice better than his face. Low. Doesn't waste words.",
            "Thinner in the face and heavier in the hand.",
            "I'd started telling him like a ghost story. Here's the ghost, buying flour.",
            "Knew him by the way he spits. Not a fine thing to know a man by.",
            "Hope he's not come about last time. Last time ought to stay buried.",
            "Seasons turn, and he comes back with them. Like geese, but worse company.",
            "Owe him a kindness from last winter. Don't know how to pay it.",
            "Last time he put his shoulder to a stuck wagon and walked off without thanks.",
            "Folks swear he's a Pinkerton. Others swear he's what Pinkertons chase.",
            "Grey in his stubble now. Wasn't any, the first time.",
            "Can't decide if I'm glad. Being glad of him feels like a sin.",
            "The horse found the trough without being led. They've stopped here enough.",
            "Another season, another look to him. Hard to say which is the true one.",
            "Didn't know him at first. Clean-shaven, collar on. Nearly respectable.",
            "Same nod, same half second, same nothing after. He's a clock.",
            "I swear he looks at the church each time. Never goes in.",
            "Since last time there's been talk of him. Not all of it bad.",
            "Rumour has it there's paper on him in two states. He doesn't look worried.",
            "I'll wager he doesn't recall my name. I never gave it.",
            "His saddlebags were near empty last time. Bulging now. Odd.",
            "Heard he'd married. No ring. Rumour, then.",
            "The last fight in {town} happened the evening he left. Coincidence, the sheriff said.",
            "Back, and I'm stood on the same plank I stood on last time. Fancy that.",
            "Long enough away that I'd started doubting my own memory of him.",
            "First thing I see is his hands. Last time they shook. Steady now.",
            "My cousin swore she saw him in Strawberry. Must have been after he left here.",
            "Mud on his boots last time. Today they're blacked. Somebody's paying him.",
            "Each time he comes the children make up a new name for him.",
            "Used to stop and watch the blacksmith. Still does. Some men like fire.",
            "That's the man who asked me if the river ever floods. Strange question. Still think on it.",
            "The stranger with the chestnut gelding. I'd know that blaze anywhere.",
            "Thank God, it's only him. For a moment I thought it was someone new.",
            "My stomach turns before my head does. It always knows him first.",
            "I said I'd speak to him next time. This is next time. My tongue's stuck.",
            "Back from wherever men like him go. Somewhere with no soap, by the smell.",
            "Gentler with the horse than last time. Maybe gentler all round.",
            "Took the seat by the window last time. Bet he takes it again.",
            "Saw his likeness on a poster for a week. Then the rain took it. Looked nothing like him.",
            "Tipped his hat to the doctor's wife last time. She's talked of it since.",
            "Three months, and the town had quit locking up for him. Back he comes.",
            "Wonder if he remembers what I said to him. I wish I didn't.",
            "That's twice now he's caught me staring. Third time he may say something.",
            "Brought a storm with him last time. Clear sky today. We'll see.",
            "Not changed. Some men don't. Granite doesn't either.",
            "Older by a winter, and the winter shows.",
            "I keep his face in mind when the dogs bark at night.",
            "Old Hollis says he's seen him rob nobody and help nobody. Fair summary.",
            "Last time a glass of beer. This time it'll be whiskey by the bottle, from the look.",
            "He's caught sight of me. Hand to the brim. He remembers, then.",
            "Walked right past without a sign. Either he forgot me or he's careful.",
            "Still rides that piebald. Ugly horse. Faithful, though.",
            "Sat at the barber's last time and slept through the shave. Didn't seem the type.",
            "Seeing him again is like finding a letter you meant to burn.",
            "Never seen him smile in {town}. Wonder if he smiles somewhere.",
            "Grocer swears he's honest. Grocer would swear it of anyone who pays.",
            "Back once again, and the street pulls in its elbows.",
            "Children used to run from him. Now they only walk fast. Progress.",
            "Since he was here last the Hendersons moved away. I wonder if he knows why.",
            "Here he comes, and my heart goes up like a startled quail.",
            "It's the same stranger, but the eyes are tireder.",
            "New rip in his duster, and new stories to go with it, no doubt.",
            "Had a dream about him once. Won't say what. Then here he is.",
            "Last spring he asked after a man named Doyle. Doyle left town the next day.",
            "If he nods, I'll nod. If he doesn't, I'll find the floor interesting.",
            "Suppose he's as tired of seeing me as I'm uneasy seeing him.",
            "Every visit, the bank manager draws the blinds. Today's no different.",
            "I remember the horse more fondly than the man. Kind eyes, that horse.",
            "Came in by the north road this time. Always the south before.",
            "Spoke to me about the rain once, civil as a schoolmaster.",
            "The last stranger I trusted here was him. Haven't decided if I was right.",
        };

        private static readonly string[] SeenArmed =
        {
            "Gun hand never far from the gun. Not for show. For use.", "Holster's worn smooth at the edge. That's years of drawing.", "Doesn't lift the gun. Doesn't need to. The weight shows in how he stands.",
            "Two guns. One's for show, or he's twice as dangerous as he looks.", "Rifle on the saddle, pistol on the hip. Prepared for weather I can't see.", "Coat cut away from the holster. Somebody tailored that on purpose.",
            "Gun's clean and the rest of him isn't. Says what he looks after.", "Thumb sits on the hammer while he stands and looks about. Doesn't seem to know it's there.", "Wears it high on the belt, butt forward, cavalry fashion. Not a farmer's way of carrying it.",
            "Wears the gun like other men wear a watch. Without a thought for it.", "Hand drifts to the holster when a door bangs. Then away again, slow.", "Belt's full of cartridges. Expecting a long day or a short war.",
            "Never seen him draw. Don't care to.", "The gun's old. Older than him, maybe. Somebody's before it was his.", "Keeps his gun side to the wall when he can. I've watched him do it.",
            "That rifle's had its stock mended. Broke it on something. Or someone.", "He has the look of a man who's been shot at and didn't care for it.", "Carries more iron than the blacksmith's apron.",
            "Even with his hands empty he looks armed.", "Knife on the belt as well. A man who doesn't like to run short.", "Gun's oiled. You can smell it from here. He looks after what keeps him breathing.",
            "Holster tied down at the thigh. Not a townsman's habit.", "He shifts the gun belt when he sits, like it's part of sitting.", "There's a nick in the grip. I'd like the story. I'd hate to hear it.",
            "Doesn't wave the gun about. The ones who wave it are the ones you laugh at later.",
            "Gun's out and pointed at the dirt. Not at anybody. Not yet.",
            "Holds the pistol low along his leg, easy as a man carrying a lantern.",
            "Somebody ought to fetch the sheriff. Somebody else. Not me, not with that gun out.",
            "Gun in his hand and nothing in his face changes. It's the face that frightens me.",
            "Thumbs the cylinder round, counting what's in it. He knew already. Counts anyway.",
            "Why's his gun out? Nothing's happened. Unless he knows what's about to.",
            "His gun's out, and the dog that barks at everybody has gone under the porch.",
            "Hasn't put it away. Walks the whole street with it out. Daring somebody, or waiting on somebody.",
            "Barrel's pointed at the ground. His eyes aren't. His eyes are on every window.",
            "Storekeeper's putting up the shutter. Can't blame him, with that iron out in the open.",
            "Hammer's down on that pistol, I think. I won't be getting close enough to be sure.",
            "Rifle in the crook of his arm, the way you'd carry a sleeping child. Careful. That's worse.",
            "Pistol in one hand, and he touches his hat with the other. Polite as a deacon, if deacons went armed.",
            "Holds it loose. The ones who know hold it loose. It's the scared ones who grip.",
            "Looks down at the gun like he'd forgotten it was in his hand. Then he remembers, and so do I.",
            "Revolver out, and walking toward the church. Lord, not that way.",
            "Shotgun broke open over his forearm. Polite of him. Still loaded, I'd bet.",
            "A rifle carried in the hand like a broom. Nobody carries a broom like that.",
            "My boy's out by the trough. I want him inside before that man turns round.",
            "Where's the marshal when there's iron in the street? Asleep, or paid.",
            "I carried a Springfield once. I know how a man holds one when he means it.",
            "Hand resting on the holster flap. Resting. That's the word he'd use, I expect.",
            "Count to ten, keep my eyes on my boots, and let him pass.",
            "No call to have a pistol drawn on a quiet afternoon.",
            "Sawed-off, that scattergun. That's not for birds. That's for rooms.",
            "Ten paces off, a pistol in his fist. I'll stand right where I am.",
            "Sergeant I served under held a rifle just that way. He died old. His men didn't.",
            "If he raises that, I go flat behind the rain barrel. Decided, then.",
            "Revolver with the bluing worn off. Pulled a great deal, that one.",
            "Mothers are calling children in all along the row. Quiet about it, too.",
            "There's no law closer than the county seat. He knows that.",
            "Disgraceful, a drawn gun where folk do their marketing.",
            "Lever rifle, and he works the lever without looking. Practiced.",
            "His finger isn't inside the trigger guard. Small mercy. I'll take it.",
            "Keeps the shotgun barrels down and his chin up. A man who's done this before.",
            "Nobody's said a word since he came round the corner with that thing.",
            "Doc's office is three doors down. Good to know, with him about.",
            "Would I run, if he turned it on me? I'd like to think I'd run.",
            "Hand hovering by the holster, fingers loose. Like a man about to deal cards.",
            "A pistol drawn and he's still chewing. Chewing! Like it's nothing.",
            "Saw what a forty-five does up close, back in Kansas. Never again, I said.",
            "Pray he's only cleaning it. Men don't clean them walking, though.",
            "The deputy saw him too. Deputy's found something to do at the livery.",
            "Rifle slung on his shoulder, muzzle up past his hat. Easy in it.",
            "Little ones are pressing their faces to the glass. Get back from there.",
            "One pull of that shotgun would empty this boardwalk.",
            "Gun in hand at the post office. What's he expecting from the mail?",
            "Steady hands. Steadier than mine, and I'm only holding a cup.",
            "Long-barreled revolver. Fine for a fence post at forty yards. Or a man.",
            "If he means trouble, it'll be done before anybody finds the sheriff.",
            "Pistol out and he's looking at the bank. I don't like sums like that.",
            "My brother would have words with him. My brother's in Ohio.",
            "Shoulders square, pistol low. Not in a hurry to use it. Not shy of it either.",
            "The barber's stopped mid-shave. Nobody wants a razor and a gun in the same minute.",
            "One shell in the shotgun, one more between his fingers. Ready for two.",
            "Thumb's on the rifle hammer. Half-cocked, I think. Safe as a coiled snake.",
            "Hand jumped toward the holster when the wagon wheel cracked. Quick as a cat.",
            "Gun's out, and he hasn't looked at it once. It does the looking for him.",
            "Ought to be a law. There is a law. It's just not here today.",
            "Kept my sons from men like him all their lives. Here's one at the gate.",
            "A drawn gun and a clean collar. That's a new sort of trouble.",
            "Fought at Shiloh. Knew boys who carried a rifle that easy. Few came home.",
            "Sighting down the rifle barrel at nothing. Checking it's straight. It is.",
            "That revolver's nickel-plated. Catches the sun like a signal mirror.",
            "Every window on this street has a face in it now, and nobody's opened a door.",
            "If I'd any sense I'd be indoors. I've no sense. I'm watching.",
            "Gun out, but his other hand's in his pocket. Wonder what's in that one.",
            "Hold still, Sadie. Let the man and his pistol go by.",
            "No badge on him. A badge would make that gun easier to look at.",
            "Shotgun in one fist and a sack in the other. Coming from the bank side.",
            "Grip's wrapped with rawhide. Slipped on him once, I'd guess. Never again.",
            "A Winchester, by the look. Half the county carries one. Not like that.",
            "The preacher crossed the road to avoid him. The preacher, mind.",
            "That pistol's pointed nowhere in particular. Nowhere in particular has me in it.",
            "Seen drunks with guns. He's not drunk. I'd almost rather he were.",
            "Still as a heron, gun hanging at his side. Waiting for the fish.",
            "Somebody's going to have to tell him to put it away. Not today, it seems.",
            "A man with a gun out on the main street is either the law or the reason for it.",
            "I'll walk round by the alley. Longer, but there's no barrel in it.",
            "Cylinder's full. I can see the brass from here. Six chances.",
            "Rests the rifle butt on his boot like a walking stick. Comfortable.",
            "My hand's shaking on the broom. His isn't, on the gun.",
            "Grandfather said never watch a man's gun, watch his eyes. His eyes are worse.",
            "His hand's riding on the holster now, not the gun. Small improvement.",
            "Don't stare. Staring's how you get noticed by a man with a shotgun.",
            "They'll say in church Sunday that somebody should have done something. Not me.",
            "The schoolmarm's ringing the bell early. Good woman. Quick thinker.",
            "Oiled leather, oiled steel, and a face nothing kind has touched in years.",
            "Holds that scattergun at port arms. Militia drill. He's drilled somewhere.",
            "Wish the town had kept a constable after the last one quit.",
            "Old army pistol, cavalry issue. He didn't buy that from a catalogue.",
            "Whatever he's waiting for with that rifle, I hope it's not in this shop.",
            "Lowered the gun when the old woman passed. Raised it a little after.",
            "Gun out in the open at noon. Either he's a fool or nobody here scares him.",
            "Hate to think what the rifle's for. Hate more to find out.",
            "Fingers drumming on the holster like he's thinking a song through.",
            "Baby's crying somewhere behind me. Hush now. Don't give him a reason.",
            "Ought to lock the door. Can't get my legs to walk to it.",
            "The bore of that shotgun looks wide as a well from here.",
            "Pistol held down by his thigh, half hid by the coat. Doesn't want a fuss. Wants it handy.",
        };

        private static readonly string[] SeenMounted =
        {
            "Sits that horse like a chair. Never been afraid of it, or never shows.", "Horse is better fed than half this street.", "Horse is watching me. Rider isn't. Somehow that's worse.",
            "Reins loose in one hand. The horse does the thinking and he lets it.", "Rides a horse that's been somewhere. Scars on the chest, calm in the eye.", "Looks down from the saddle like the street's a map he's already read.",
            "Horse stands square while he looks about. Well broke, or well frightened.", "Dismounts on the far side from me. Keeps the horse between us. Careful man.", "Saddle's worth more than the horse. Or he stole one of them.",
            "Rides in slow. A man in a hurry gets noticed. He wants to be noticed less.", "Horse is lathered at the neck. Came a long way, or came fast.", "Good animal. Good animals cost money, and money comes from somewhere.",
            "Sits easy, but his eyes don't. Up and down the street twice before he lets the horse walk on.", "That horse has carried trouble before. You can see it in how it stands for nothing.", "Saddlebags heavy. Wouldn't care to guess with what.",
            "He talks to the horse. Low, so nobody else hears. Kinder to it than to people, I'd wager.", "Horse throws its head by the gunsmith's. Smells powder, or remembers it.", "Rides like the cavalry taught him, or like he taught himself and nobody told him different.",
            "Mud to the horse's belly. Came by the river road, then.", "Horse is better shod than I am.", "He ties the horse loose at the rail. Wants to be able to leave in a hurry.",
            "Leans on the horn to look about. Takes his time. Has plenty, it seems.", "The horse is fond of him. You can tell by how it leans.", "Bedroll tied on behind. Doesn't plan to sleep under anybody's roof.",
            "He rides past and the dust takes a long time settling.", "Horse has a brand I don't know. Bought far off, or taken far off.", "Steps down light for a man that size.",
            "Watches the street from up there like a man on a hill watches weather.", "Horse stamps and he doesn't even look. They know each other.", "Spurs never touch the horse. Just there to say he could.",
            "Rope on the saddle, coiled neat. A man who ropes things for a living, or used to.", "Rides one-handed. The other hand stays free. For what, I wonder.", "Horse and man both look like they'd rather be out of town.",
            "Rides right up to the boardwalk like the boards are his to stop at.", "That's a horse that's outrun something. Lean in the right places.", "Swings down and the horse just waits. Doesn't need tying, that one.",
            "Dusty from hat to stirrup. Must have ridden since before dawn.", "His saddle's cracked and mended twice. Won't buy new. Fond of it, or broke.", "He looks my horse over like a buyer. Knows horses, then.",
            "Rides past slow enough to be seen. Fast enough not to be stopped.",
            "Horse shies at a scrap of blown paper and he never so much as shifts his seat.",
            "He'd see the horse watered before himself. You can tell that about a man.",
            "Pulls up for a wagon and touches his hat to the driver. Patient, from up there.",
            "Up in the saddle the way a preacher's up in the pulpit. Like the height was given him.",
            "Turns the horse with his knees and keeps both hands quiet. Somebody taught him that, or a war did.",
            "From up there he can see over every fence on the street, and he's looking over every one.",
            "Rides with the brim low and the chin up. Can't tell where he's looking. That's the point of it.",
            "Saddle horn's worn bright where his hand rests. Years of that one hand, right there.",
            "Never looks down at the horse. Looks where he's going, and the horse goes there.",
            "Walks the horse up the street, and every horse at the rails turns its head to watch him pass.",
            "Rides like a man who'd sooner sleep in the saddle than in a bed, and has.",
            "Weight back in the saddle, easy, like he's ridden more miles than he's walked.",
            "The horse sets its feet down neat as a dancer. Better manners than most of this street.",
            "Keeps the horse sideways to the street while he looks about. Can go either way from there.",
            "Comes in at a walk, but the horse is blowing. They were running not long ago.",
            "Stirrups let out long. Rides with his legs near straight, like the old Texas men.",
            "Somebody braided that horse's mane on one side. Took time over it, too.",
            "Got down and stood a moment holding the horn. Long ride, sore legs.",
            "Loosens the cinch before he goes inside. Thinks of the animal first.",
            "Rifle scabbard under the stirrup leather, butt forward. Reached for it plenty, I'd say.",
            "Swung his leg over the cantle neat as closing a gate.",
            "Pats the horse's neck twice and says something. Horse flicks an ear back to hear it.",
            "That's a mare, and a fine one. Too fine for the rest of his outfit.",
            "Reins are knotted and mended with rawhide. Been broke more than once.",
            "Sat that horse at the end of the street a good while first. Sizing up the town.",
            "Ribs showing on that horse. He's been short of oats, or short of time.",
            "Never a jingle out of his rig when he moves. Everything tied down tight.",
            "Lord, look at the size of that animal. Wouldn't want it stepping on my foot.",
            "Rides in with the sun at his back. Can't make out his face till he's right on you.",
            "Bit of a limp once his boots hit the ground. Saddle's kinder to him than the street.",
            "Leads the horse the last hundred yards on foot. Saving its legs, or showing respect.",
            "Saddle blanket's Navajo weave, faded near to grey. Traded for, or won.",
            "His horse won't let the livery boy near. Only answers to the one man.",
            "Kept his seat when the dog ran under the hooves. Didn't even grab the horn.",
            "Horse is a paint, splashed white like spilled milk. Easy to remember. Bad for a man hiding.",
            "Walks the last bit leading the horse by the cheek strap, talking to it.",
            "Saddlebag flap's tied down with string. Something in there he can't lose.",
            "Carries his rifle across the pommel like a soldier on picket.",
            "The horse's shoes ring sharp on the stones. Fresh shod. Somebody paid a smith lately.",
            "Stays mounted while he talks to the storekeeper. Doesn't mean to stay long.",
            "Up there he's taller than the awning. Has to duck under the sign.",
            "Gets down slow, like the ground might be hot.",
            "Envy him that horse. Wish I had anywhere worth riding to.",
            "Bridle's got silver conchos, tarnished black. Was somebody's pride once.",
            "Halts by the well and drinks from his hat, then holds the hat down for the horse.",
            "Lashed to the saddle, a shovel. What does a man need with a shovel out there?",
            "Coming off that horse he looks twice as tired as he did on it.",
            "Horse turns its rump to the crowd. Learned that from him, maybe.",
            "Wonder how many miles are on that saddle. More than on my whole life.",
            "Spurs are filed blunt at the rowels. Doesn't care to cut the animal.",
            "Stands in the stirrups to see over the stage. Then sits, satisfied or not.",
            "A pelt's tied behind the cantle. Fox, fresh. Hunts on the way, then.",
            "Chestnut gelding with one white sock. I'd know it again anywhere.",
            "Horse froze at the firecracker. Rider froze quicker, and his hand went somewhere.",
            "Mounts from the wrong side, like the Indian riders do. Wonder where he learned that.",
            "Ties his reins with a knot I've never seen. One pull and it's free.",
            "Slid off the horse like water off a roof.",
            "Doesn't trot. Walks or gallops. Nothing in between, that one.",
            "Mane full of burrs. Came through scrub, not by any road.",
            "Kneels to look at the horse's hoof right there in the street. Picks a stone out with his knife.",
            "How a man rides into a strange town tells you plenty. He rides in like he was invited.",
            "Rode right past the livery. Doesn't trust it, or can't pay it.",
            "No daylight between his seat and the saddle at a lope. Rode before he walked, I'd say.",
            "Frost still on the saddle blanket. Came down from the high country overnight.",
            "Pulls the horse up short when the child runs out. Hard hands, quick hands.",
            "Gun on his hip and another on the saddle. Expecting a long argument somewhere.",
            "Dust off that horse could fill a flour barrel.",
            "Whistles once, two notes, and the horse comes to him across the lot.",
            "Sweat-stained saddle, salt rings dried white on the leather. Hard riding.",
            "Glad he's getting down. Up there he looks like judgment come to town.",
            "Hat brim and horse's ears line up, both pointed at the bank.",
            "Looks small, somehow, standing next to that horse. Not a small man, either.",
            "Unbuckles the saddlebags and slings them over his shoulder. Won't leave them out of sight.",
            "Horse is grey in the muzzle. Old friend, then. Won't trade it for a younger.",
            "Kicked loose of the stirrups before the horse even stopped. Wanted his feet ready.",
            "Reining in, he looked at me. Just a glance, but I felt it in my stomach.",
            "My pa would have bought that horse off him, and asked no questions about it.",
            "Leans down out of the saddle to hear the boy. Patient with children, then.",
            "Gallops in, then slows to a walk at the first house. Knows how folk talk.",
            "Every rail in town's full, and he finds a place. Somebody always moves for him.",
            "Strange horse. Stranger man. Both looking at the same window.",
            "Hard mouth on that horse. Pulls to the left. He corrects it without thinking.",
            "Lariat hangs off the horn, frayed at the honda. Used, not for show.",
            "Shotgun in a boot on the far side. Didn't see it till he turned.",
            "Poor beast. Carrying that much man and that much iron all day.",
            "Stepped down into a puddle and didn't even look. Boots past caring.",
            "Pays the stable boy before the boy can ask. Knows what it is to be owed.",
            "Funny, how a horse makes a man look honest. Probably not true of this one.",
            "Rubs the horse down with a twist of straw. Takes longer over it than I take over supper.",
            "Chaps torn to ribbons at the shins. Thorn country, the whole way here.",
            "The horse snorts at me. Agrees with the rider about me, I suppose.",
            "Gets the horse to back up three steps with a word. Never seen that done.",
            "Hooves clatter on the bridge and half the street turns. He doesn't hurry for it.",
            "Holds the reins short, like he expects to wheel about any moment.",
            "Sits crooked in the saddle. Old wound on the right side, I'd guess.",
            "Wouldn't sell that horse for any money, by the look he gives the dealer.",
            "Dried blood on the saddle skirt. Game, I hope.",
            "Gentle with the bit. Hard with everything else, I'd wager.",
            "Horse sidesteps the drunk on the boardwalk. Smarter than the drunk.",
            "Who rides a horse like that into a town like this, at noon, alone?",
            "Rides double-cinched. Ropes something big now and then.",
            "Grey dust caked on the horse's flanks. Came across the flats.",
            "Tugs his hat down as he rides under the bank's clock. Shy of clocks, or of windows.",
            "His boots don't touch the ground for a long while after the horse stops. Just sits and looks.",
            "Canteen on the horn, dented like it's been dropped off a cliff.",
            "Half the horses at this rail are plugs next to his.",
            "Some men are nothing off a horse. He's still something.",
            "Clicks his tongue and the horse moves off. No heel, no rein.",
            "Steam rising off that horse in the cold morning. Off him, too.",
            "Bet he named that horse. Men like him always do, and never tell anybody.",
            "Steers around every puddle. Fussy about his horse's feet, or about his own.",
            "One stirrup's a different colour leather than the other. Mended out on the trail.",
            "Sat down on the trough edge right after dismounting. Ridden all night, that face says.",
            "Takes the bridle off and lets the horse graze the church lawn. Bold.",
            "You can tell the horse is afraid of nothing but him.",
            "Unhurried getting down. Made the whole street wait without asking it to.",
            "Saddle creaks like an old door when he shifts. Tired leather, tired man.",
            "Fancy tooling on that saddle, worn half away. Bought in better days.",
            "Pleased to see him tie up across the street and not right outside my door.",
            "Lowers its head for him, that horse, so he can slip the bridle. Trusts him.",
            "Mustang, by the look. Wild-caught and gentled. Gentled by him, I'd wager.",
            "Drops down from the saddle and his spurs sing on the boards.",
            "Should I take his horse? He looks like he'd tip a boy. Or skin one.",
            "Cattle brand on the hip's been burned over. Somebody changed that horse's history.",
            "Pistol hangs where he can reach it mounted. Wears it higher than walking men do.",
            "Wipes the horse's eyes clear of dust with his own kerchief. Tender, for a hard case.",
            "Comes up the street slow behind the funeral wagon. Takes his hat off and holds it.",
            "Second time he's ridden past the bank today. I'm counting.",
            "Feeds his horse a lump of sugar from his pocket. Keeps sugar on him. Who'd have thought.",
            "Quirt hanging from his wrist, but the horse has never felt it. You can see that much.",
        };

        private static readonly string[] Seen =
        {
            "Doesn't look away first. Never does, I'd wager.", "Carries his weight low, like a man ready to move.", "Smells of woodsmoke and horse.", "Eyes don't rest on one thing. Counts the exits like I do.",
            "Dust on him from three days' road, not three miles.", "Quiet. The kind of quiet that's decided something.",
            "Boots he's walked the soles off. Not a man who rides everywhere.", "Keeps his hat low and his eyes up. Opposite of most.", "Stands where he can see the door. Habit, that. A bad one or a good one.", "Hands still when he talks. Still hands are the ones to watch.",
            "Spurs that have seen use. Most spurs in town are for the look of them.", "Looks at a man's hands before his face. Learned that somewhere hard.", "Walks the middle of the street, not the boards. Wants room.", "Clean-shaven under all that dust. Takes trouble over something, then.",
            "Coat's seen weather. Patched at the elbow with a different cloth.", "Big man. Moves smaller than he is.", "He looks at the street like he's read how it ends already.",
            "Nobody's told him to take his hat off indoors in a long while, I'd guess.", "Beard's a week old. Not a choice. A circumstance.", "There's tiredness in him under the hardness. You only see it if you look.",
            "Whistles something under his breath. Can't place the tune.", "Strikes a match on his thumbnail. Showing off, or just practised.", "Stands like a man who's been told to wait and doesn't care for it.",
            "Hat brim's bent where he grabs it. Grabs it a lot, then.", "Speaks soft when he speaks at all. Makes you lean in. Maybe that's the trick of it.", "Knuckles scarred white across the back. Not from farm work.",
            "Smells like a camp. Coffee and smoke and something that died nearby.", "Walks past the church without a look. Past the saloon without one either. Odd.", "There's a letter half out of his pocket. Read and folded a hundred times.",
            "Turns his whole body to look, not just his head. Like a man with an old hurt.", "Pays in coin, not paper. Counts it out slow.", "Nods to the old men on the bench. They nod back. Even they don't know him.",
            "His eyes go up to the roofs. Who looks at roofs?", "Smiles at something nobody said. Something he remembered, maybe.", "Takes up more of the doorway than a door was made for.",
            "Scrapes his boots before he goes in. A gentleman somewhere in there, buried deep.", "Spits, and doesn't look where it lands. Never been fined for it, I'd guess.", "There's a ring on a string round his neck. Not his, I'd guess.",
            "Looks at me like he's deciding whether I'm worth the trouble. Decides I'm not.", "Bit of blood on the cuff. Old, or he doesn't care to wash it.",
            "Reads every poster outside the sheriff's office. Slowly. Like a man checking a list for his own name.",
            "Holds the door for an old woman and walks on before she can thank him.",
            "Eats standing at the counter, like a man who's had a plate taken off him before.",
            "Laughs once, short, at something the barber says. Looks surprised at himself after.",
            "Rolls a smoke one-handed and doesn't spill a flake. Practice, or a long time alone.",
            "Pats his coat pockets the way a man does when everything he owns is on him.",
            "Lets a stray sniff his knuckles and scratches its ear. The dog follows him two streets.",
            "Shirt was white once. Washed in a creek since, by the look, and dried on a bush.",
            "Asks the price and pays it, no haggling. Either flush or too tired to argue.",
            "Sawdust from the saloon floor on his boots, and red mud from somewhere a long way off.",
            "Talks low to a horse at the rail like an old friend. It isn't even his horse.",
            "Watches the children at their game a good while. Something in his face goes soft, then shuts.",
            "Sleeves rolled to the elbow. Arms of a man who's swung an axe more than he's held a pen.",
            "Looks at the bank the way a farmer looks at a field he means to work.",
            "Fresh bandage on one hand. Whatever bit him, it bit him since yesterday.",
            "Gun belt sits low on the hip, buckled a notch past new. Lost weight, or gained a habit.",
            "Collar's turned up though there's no wind. Hiding a scar, maybe.",
            "Takes the steps two at a time and doesn't puff at the top. Sound in the chest, that one.",
            "Looked at my shoes. Mine. Nobody looks at a clerk's shoes.",
            "Holster leather's dark where his thumb rests. Rests there often.",
            "Squints even in the shade. Too many years staring at far country.",
            "One ear's notched at the top. Knife or barbed wire, and I'd bet on the knife.",
            "Wipes his mouth with the back of his hand, then on his trousers. Raised rough.",
            "Stops dead when a door slams, and only his eyes move.",
            "Puts his back to the wall without seeming to choose it.",
            "Tips his hat to the schoolmistress. She'll talk of nothing else till Sunday.",
            "Wouldn't want to owe that man money. Wouldn't want him owing me any, either.",
            "A watch chain across the vest. A watch on it? Doubtful.",
            "Trousers patched at the knee with flour sacking. Somebody sewed that for him once.",
            "Must be forty if he's a day, and every one of those years shows on him.",
            "Walks like his left boot pinches. Too proud to buy another pair, I'd say.",
            "Neckerchief faded from red to the colour of brick dust.",
            "Hums a hymn, of all things. Sounds like he learned it young and kept only the tune.",
            "Big hands. Could wrap one round my whole neck and have thumb to spare.",
            "Fingernails black to the quick. Been digging, or burying.",
            "Lord, but he needs a bath. Downwind of him is no place for a Christian.",
            "Steps aside for the doctor's buggy without being asked. Somebody raised him partway.",
            "Good teeth for a drifter. Most of a set, at least.",
            "Leans on the post like it owes him something.",
            "A scar runs into his hairline. Somebody tried to open his head and didn't finish.",
            "Moves his jaw like a man who's been hit there and still feels it.",
            "Not a drinker, I think. Eyes too clear for this hour.",
            "That coat's too heavy for the season. Unless there's something under it worth hiding.",
            "Reads the chalkboard menu twice and orders the cheapest thing on it.",
            "Keeps one thumb hooked in his belt, right beside the buckle. Close to the iron.",
            "My mother would've set a plate for him and locked up the silver.",
            "Glanced at the bank twice. Once could be nothing.",
            "Bad knee, I'd say. Favours it on the steps and hides it on the flat.",
            "Never smiles with the top half of his face.",
            "Every dog in town seems to know him. Every child steers wide.",
            "Boots are filthy but the pistol's clean as a church plate. Tells you what he cares for.",
            "Hasn't said a word to anybody, and still everybody's heard he's here.",
            "Wonder what his mother called him. Something softer than what men call him now.",
            "Sun's burned the back of his neck to leather. Works outdoors, or hides outdoors.",
            "Carries a book in his coat. A man like that, reading. Wouldn't have thought it.",
            "Stood at the milliner's window a long while looking at a lady's hat. For who, I wonder.",
            "His shadow's longer than the hitching post. Big all over.",
            "Pretends not to hear the sheriff's name. His shoulders hear it fine.",
            "Makes way for a woman with a basket and touches the brim. Manners from somewhere.",
            "Quick eyes, slow feet. Takes his time getting anywhere and sees everything doing it.",
            "Cheek's swollen on one side. Toothache, or a fist. Either way he's not saying.",
            "Doesn't sit. Been here an hour and hasn't once sat down.",
            "Mud on his knees. Been kneeling somewhere. Praying, or hunting.",
            "If he's a lawman, I'm the Pope in Rome.",
            "Strange how still he goes when the church bell rings.",
            "There's grey in his beard he's not old enough for.",
            "Smells of gun oil. Cleans that iron more than he cleans himself.",
            "Wears his hat square on his head. No tilt. No vanity in it at all.",
            "Talks to the barkeep like they've met. They haven't. He's just like that.",
            "Coins clink in his pocket. More than a drifter ought to have.",
            "Notched his belt with a knife to make another hole. Lean times, somewhere.",
            "Stares into the trough a good while. Looking at the water or at his own face.",
            "Staring like a child at a circus. My mother would swat me for it.",
            "Watched him pick up a dropped glove and hand it back. Gentle with it, for such hands.",
            "Whoever cut his hair last did it with a knife and no mirror.",
            "Pity, really. Fine-looking man under all that wear.",
            "Plenty of men carry a pistol here. Not many carry it like it's part of the arm.",
            "Right sleeve's more worn than the left. Draws with that one, I'd guess.",
            "Lets the drunk shove past him. Could break him in half and lets him go.",
            "Keeps looking back the way he came, like something's following.",
            "So that's what a hard man looks like up close. Softer round the eyes than I'd thought.",
            "Knows the street, or acts like it. Doesn't once stop to get his bearings.",
            "Wish I walked into a room like that. Like the room was waiting on me.",
            "Hat's got a hole through the crown, front and back. He was lucky, or the shot was poor.",
            "Counted his change, then counted it again. Been cheated in some town before this one.",
            "Got the look of a man who's slept on the ground so long a bed would vex him.",
            "Gives the preacher a long look. Not friendly, not unfriendly. Just long.",
            "Swears soft at a splinter in his palm, then picks it out with his teeth.",
            "Mended his own shirt by the look of the stitching. Big stitches, all crooked.",
            "Can't tell if he's twenty years older than me or ten. Hard living does that.",
            "Eyes the colour of creek water. Cold right through.",
            "What a man like that wants in a town like this, I'd give a dollar to know.",
            "Nothing in his hands, and still I feel it'd be foolish to cross him.",
            "Breathes slow. Slower than me, and I'm the one sitting still.",
            "Somebody's going to get hurt today. Hope it's not on my porch.",
            "Arms folded high on his chest. Gun hand tucked where it's quickest.",
            "Laces on his boots are twine. Broke the leather ones and made do.",
            "Wrinkled his nose at the tannery smell like a city man. Not from round here.",
            "Looks tired enough to fall asleep standing. Wouldn't care to be the one who wakes him.",
            "Folks part for him on the boards like water round a rock.",
            "Asked the time of me. Had to clear my throat twice before I could tell him.",
            "Burn mark across the back of one hand. Old campfire, or an old cruelty.",
            "Remember to breathe. He's only buying tobacco.",
            "Cartridges in his belt, every loop full. A man who plans to need them.",
            "Shook the blacksmith's hand, and the blacksmith shook out his fingers after.",
            "Lips cracked from wind. Hasn't been near a town in weeks, I'd say.",
            "Too calm. Nobody's that calm unless they've stopped being scared of dying.",
            "Gloves off, tucked in his belt. Wants his fingers bare for something.",
            "Chews a stem of grass like a farm boy. Doesn't suit the rest of him.",
            "Tracked mud right across the clean floor and never looked back.",
            "Sniffs the air outside the bakery like a hungry dog. Poor devil.",
            "Ain't seen him before, and I see everybody.",
            "Bowed to the judge's wife. Bowed! Like a man who's been to a cotillion.",
            "Stands with his weight on his heels. Hard to knock down, a man who stands like that.",
            "Rubs his eyes with the heels of his hands like a boy. Long night behind him.",
            "Hard to say what he is. Not a cowhand. Not a gambler. Something with more edges.",
            "Spoke Spanish to the Mexican fellow at the rail. Just a word or two, but easy.",
            "Jaw like an axe head, and about as friendly.",
            "Must be a dozen notches on that holster. No, it's just wear. I hope it's wear.",
            "Ears red from the cold, nose red from something stronger.",
            "Put his last coins down for coffee, by the look of the purse. Then tipped the girl anyway.",
            "Yawns like a bear coming out of a cave. Big as one, too.",
            "Brushes the dust off his hat against his leg before he goes in. Bit of pride left.",
            "Eyebrow's split through the middle. Hair never grew back in the scar.",
            "Steady on his feet, though he's had a drink or three. Holds it like he holds a grudge.",
            "Sad, somehow. Not the face. The shoulders.",
            "Can't decide if I'd run to him or from him, if there was shooting.",
            "Looks a fellow up and down once, and that's him weighed and measured.",
            "Turned down a free drink. Turned it down polite. Wants nobody's favour.",
            "Thin wrists for such shoulders. Fed poorly as a boy, maybe, and grew anyway.",
        };

        // ---- putting it together ------------------------------------------------------------------

        /// The trade group for a description such as "a farmhand" or "an old woman".
        public static string TradeGroup(string trade)
        {
            string t = (trade ?? string.Empty).ToLowerInvariant();
            if (t.Contains("soldier")) return "soldier";
            if (t.Contains("drummer")) return "drummer";   // a travelling salesman off the train (FIVEDAY R1)
            if (t.Contains("lawman")) return "law";
            if (t.Contains("farm")) return "farm";
            if (t.Contains("old")) return "old";
            if (t.Contains("lady") || t.Contains("gentleman")) return "gentry";
            if (t.Contains("working girl")) return "girl";
            if (t.Contains("labour") || t.Contains("working") || t.Contains("miner") || t.Contains("dock")) return "working";
            if (t.Contains("card")) return "card";
            if (t.Contains("rough") || t.Contains("hard-looking")) return "rough";
            if (t.Contains("trapper")) return "trapper";
            if (t.Contains("town")) return "town";
            return "plain";
        }

        private static string[] ForHour(int hour)
        {
            return hour >= 4 && hour < 8 ? Dawn : hour >= 8 && hour < 17 ? Day : hour >= 17 && hour < 21 ? Evening : Night;
        }

        /// A thought for this person at this hour. `told` remembers what they've thought lately (may be null); `day`
        /// brings the worry they're living through and the town's news (may be null: then only the timeless pools).
        public static string Thought(Person p, int hour, string seed, List<string> told, PlayerLook look = null, Context day = null)
        {
            string source;
            return Thought(p, hour, seed, told, out source, look, day);
        }

        /// The mind's own say, first ([Mind] MindLive; set by the Engine): a feeling still strongly felt, then a pressing
        /// need. Null (the default, and with MindLive off) leaves every thought as it was. Returns the line and its source.
        public static Func<Person, int, string, List<string>, Context, KeyValuePair<string, string>> MindFirst;

        /// The same, with where it came from (for the log and the tests): again, look, tie, linked, worry, towntie,
        /// towntalk, day, era, now:<what> (InnerLife.Now.cs) or filler:<pool>.
        public static string Thought(Person p, int hour, string seed, List<string> told, out string source, PlayerLook look = null, Context day = null)
        {
            if (MindFirst != null && p != null)
            {
                KeyValuePair<string, string> first = MindFirst(p, hour, seed, told, day);
                if (first.Key != null)
                {
                    source = first.Value;
                    return first.Key;
                }
            }
            // a regular who has met him before knows him again
            if (look != null && look.Near && look.Seen >= 2 && Voice.Hash(seed + "again") % 100 < 22)
            {
                source = "again";
                return Pick(Again, p, seed, told);
            }
            // the stranger close by is worth a thought of its own
            if (look != null && look.Near && Voice.Hash(seed + "look") % 100 < 28)
            {
                source = "look";
                return Pick(look.Armed && Voice.Chance(seed + "la", 55) ? SeenArmed : look.Mounted && Voice.Chance(seed + "lm", 55) ? SeenMounted : Seen, p, seed, told);
            }
            int roll = Voice.Hash(seed + "ch") % 100;
            // their own life first: the worry they're going through, what happened in town, the day and the year
            string own = null;
            source = null;
            if (roll < 24)
            {
                // someone they had words with (or a sweetheart) lately shares this slot with the worry
                if (day != null && day.Ties != null && day.Ties.Count > 0 && Voice.Chance(seed + "tb", 45))
                {
                    own = Ties.Thought(day.Ties[0], p.Id, seed, day.IsDead(day.Ties[0].OtherId(p.Id)));
                    source = "tie";
                }
                else if (day != null && day.Linked != null && day.Linked.Count > 0 && Voice.Chance(seed + "lb", 40))
                {
                    own = Links.OtherThought(day.Linked[Voice.Hash(seed + "lw") % day.Linked.Count], day.GameDay, seed);   // the other end of someone's worry
                    source = "linked";
                }
                else
                {
                    own = day != null && day.GameDay >= 0 ? WorryThought(p, day.GameDay, seed, told) : null;
                    source = "worry";
                }
            }
            else if (roll < 34)
            {
                bool deeds = day != null && day.TownDeeds != null && day.TownDeeds.Count > 0, ties = day != null && day.TownTies != null && day.TownTies.Count > 0;
                if (ties && (!deeds || Voice.Chance(seed + "tw", 50)))
                {
                    SceneTie tt = day.TownTies[Voice.Hash(seed + "twp") % Math.Min(3, day.TownTies.Count)];
                    own = Ties.TownThought(tt, p.Home, seed, day.IsDead(tt.A), day.IsDead(tt.B));
                    source = "towntie";
                }
                else
                {
                    own = deeds ? TownTalk(p, day.TownDeeds, seed, told) : null;
                    source = "towntalk";
                }
            }
            else if (roll < 44)
            {
                own = DayLine(p, hour, seed, told);
                source = "day";
            }
            else if (roll < 49)
            {
                string cal = null;
                own = Voice.Chance(seed + "|alm", 55) ? Almanac.Thought(p, ClockYear, ClockMonth, ClockDay, seed, told, out cal) : null;   // the year's dated news first, half the time (Core/Almanac.cs)
                if (own == null)
                    own = Calendar.Line(p, CalendarYear, ClockMonth, ClockDay, hour, seed, told, out cal);   // then the date: a holiday, its eve or morning after, the season's work (Core/Calendar.cs)
                source = own != null ? cal : "era";
                if (own == null)
                    own = EraLine(p, seed, told);
            }
            else if (roll < 58)
            {
                string what;
                // the quiet side of the moment they're in (Core/Moments.cs: a drink, the saddle, their work) half the time, else
                // what's going on around them (InnerLife.Now.cs)
                own = look != null && look.Now != null && Voice.Chance(seed + "mo", 50) ? Moments.Ambient(p, look.Now, seed, told, out source) : null;
                if (own == null)
                {
                    own = NowThought(p, seed, told, day, out what);
                    source = "now:" + what;
                }
            }
            if (own != null)
                return own;
            if (roll < 58)
                roll = 58 + roll * 42 / 58;   // nothing of their own for this roll: one of the timeless frames instead
            string pool;
            string line = Filler(p, hour, seed, told, roll, out pool);
            source = "filler:" + pool;
            return line;
        }

        /// The day of the week, the meal they're missing or the small hours, or null.
        public static string DayLine(Person p, int hour, string seed, List<string> told)
        {
            string[] when = ForDay(hour, Weekday, seed);
            return when != null ? Pick(when, p, seed, told) : null;
        }

        /// The year they're living in (1899, or 1907 in the epilogue). The times' own lines (Times.cs) are said, not thought:
        /// in the talks between people and when the news is asked (they run long for the ticker, and the life harness's
        /// period rule reads Roosevelt and the telephone as 1907's).
        public static string EraLine(Person p, string seed, List<string> told)
        {
            if (!Historical)
                return null;   // [World] HistoricalAccuracy off: the year is nobody's business
            return Pick(ClockYear >= 1900 ? Era1907 : Era1899, p, seed, told);
        }

        /// One of a pool, for this person, not said lately (Pick, for the calendar's pools: Core/Calendar.cs).
        public static string PickLine(string[] pool, Person p, string seed, List<string> told)
        {
            return Pick(pool, p, seed, told ?? new List<string>());
        }

        /// Whether this person has thought every line of the pool lately (the told list's keys, Pick's own); false with no list.
        public static bool AllTold(string[] pool, List<string> told)
        {
            if (told == null || pool == null || pool.Length == 0)
                return false;
            foreach (string raw in pool)
                if (!told.Contains("il" + Voice.Hash(raw)))
                    return false;
            return true;
        }

        /// One of the timeless frames, by a roll from 58 to 99: trade, hour, body, mirror, bias, age, mask, pleasure,
        /// temper, town or faith (`pool` says which).
        public static string Filler(Person p, int hour, string seed, List<string> told, int roll, out string pool)
        {
            return Pick(FillerLines(p, hour, roll, out pool), p, seed, told);
        }

        /// Which timeless frame a roll falls in for this person (the name Filler reports), without thinking it.
        public static string FillerPool(Person p, int roll)
        {
            string pool;
            FillerLines(p, 0, roll, out pool);
            return pool;
        }

        private static string[] FillerLines(Person p, int hour, int roll, out string pool)
        {
            string[] lines;
            if (roll < 68)
            {
                lines = ByTrade[TradeGroup(p.Trade)];
                pool = "trade";
            }
            else if (roll < 73)
            {
                lines = ForHour(hour);
                pool = "hour";
            }
            else if (roll < 79)
            {
                lines = Body;
                pool = "body";
            }
            else if (roll < 83)
            {
                lines = Mirror;
                pool = "mirror";
            }
            else if (roll < 86)
            {
                lines = Bias;
                pool = "bias";
            }
            else if (roll < 88)
            {
                lines = Age;
                pool = "age";
            }
            else if (roll < 91)
            {
                lines = Mask;
                pool = "mask";
            }
            else if (roll < 94)
            {
                lines = Pleasure;
                pool = "pleasure";
            }
            else if (roll < 97 && p.Personality != null && ByTemper.ContainsKey(p.Personality))
            {
                lines = ByTemper[p.Personality];
                pool = "temper";
            }
            else if (roll < 99 && p.Home != null && ByTown.ContainsKey(p.Home))
            {
                lines = ByTown[p.Home];
                pool = "town";
            }
            else
            {
                lines = ByFaith.ContainsKey(p.Devotion ?? "") ? ByFaith[p.Devotion] : ByFaith["Other"];
                pool = "faith";
            }
            return lines;
        }

        /// A thought about the player close by: knowing him again (a regular who has met him twice), else the look of him.
        public static string AboutPlayer(Person p, string seed, List<string> told, PlayerLook look, bool again)
        {
            if (again)
                return Pick(Again, p, seed, told);
            return Pick(look != null && look.Armed && Voice.Chance(seed + "la", 55) ? SeenArmed : look != null && look.Mounted && Voice.Chance(seed + "lm", 55) ? SeenMounted : Seen, p, seed, told);
        }

        /// What the town is saying about the player's deeds here (InnerLife.Days.cs).
        public static string TownTalkLine(Person p, List<Deed> deeds, string seed, List<string> told)
        {
            return deeds != null && deeds.Count > 0 ? TownTalk(p, deeds, seed, told) : null;
        }

        private static string Pick(string[] pool, Person p, string seed, List<string> told)
        {
            int start = Voice.Hash(seed) % pool.Length;
            for (int i = 0; i < pool.Length; i++)
            {
                string raw = pool[(start + i) % pool.Length];
                string key = "il" + Voice.Hash(raw);
                if (told != null && told.Contains(key))
                    continue;
                if (!WeatherTruth.Fits(raw))
                    continue;   // says it's hot in a cold drizzle, or the like: the next one (Core/WeatherTruth.cs)
                if (told != null)
                {
                    told.Add(key);
                    if (told.Count > 40)
                        told.RemoveAt(0);
                }
                return raw.Replace("{town}", string.IsNullOrEmpty(p.Home) ? "this town" : p.Home);
            }
            // every line of this pool thought lately: only this pool starts over (the list also holds other kinds of
            // lines, such as the reality musings already said, which must stay remembered)
            if (told != null)
                foreach (string raw in pool)
                    told.Remove("il" + Voice.Hash(raw));
            return pool[start].Replace("{town}", string.IsNullOrEmpty(p.Home) ? "this town" : p.Home);
        }
    }
}
