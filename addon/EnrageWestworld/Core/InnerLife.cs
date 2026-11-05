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
            "Frost on the trough. Broke it with my elbow. Cold goes up the arm and stays.", "First wagon of the day. Wheels sound louder before the town's awake.", "Light comes up grey, then gold. Same as yesterday. Still worth a look."
        };

        private static readonly string[] Day =
        {
            "Sun's high. Shade's a luxury.", "Dust everywhere. In my teeth, in my tea.", "Noon bell. Everyone pretending they're not hungry.",
            "Heat sits on the street like a hat. Nobody wants to move.", "Long day and not half over.",
            "Flies at the horse's eyes. Flies at mine. We're both too hot to fight them.", "Shadows gone small under the boardwalk. Nowhere to stand that isn't baking.", "Middle of the day and the street's gone still. Even the dogs have given up."
        };

        private static readonly string[] Evening =
        {
            "Light's going gold. Nice for a minute, then it's work again.", "Supper smells drift up the street. Somebody has bacon.", "Lamps being lit down the row. First one's always the saloon.",
            "Cooler now. Now a man can think.", "Dogs bark, children called indoors. Evening, same as every one.",
            "Bats out over the livery. They keep better hours than I do.", "Shutters going closed down the row. The store first, the saloon never.", "Smoke from every chimney at once. Town smells like supper and wet wood."
        };

        private static readonly string[] Night =
        {
            "Too dark to see my hand. Too quiet to hear my own thoughts.", "Piano's still going at the saloon. Tune never changes.", "Moon's up. Wolves are, too.",
            "Should be home by now. Should be a lot of things.", "Something about night makes the town look like a stage set after the play.",
            "Stars thick as spilled salt. Too many to be looking down at me.", "One lamp lit at the far end of the street. Somebody can't sleep either.", "Owl in the cottonwoods. Says the same thing every night and I still listen."
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
            "I mark it in the almanac every time he rides in. Don't ask me why I keep count."
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
            "Looks down at the gun like he'd forgotten it was in his hand. Then he remembers, and so do I."
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
            "Comes in at a walk, but the horse is blowing. They were running not long ago."
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
            "Fresh bandage on one hand. Whatever bit him, it bit him since yesterday."
        };

        // ---- putting it together ------------------------------------------------------------------

        /// The trade group for a description such as "a farmhand" or "an old woman".
        public static string TradeGroup(string trade)
        {
            string t = (trade ?? string.Empty).ToLowerInvariant();
            if (t.Contains("soldier")) return "soldier";
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
