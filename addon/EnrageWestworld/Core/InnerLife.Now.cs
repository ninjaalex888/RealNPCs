// What is going on around a person right now, for a thought: something that just happened in front of them (a face
// put into the bar, a fistfight, a drunk on the floor, a thief), the other people in sight and what this person makes
// of them by their own temperament (envy of the well-dressed, worry for the drunk, fear of the hothead, contempt,
// curiosity), the weather on their skin when they're out in it, and the place they're in. Attention goes to what is
// near, new and threatening first (an event they just saw outranks the rain), and people judge others through their
// own nature, as people do. Small pleasures and memories (`Pleasure`) are a timeless frame of their own, so a life
// isn't all worry. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        /// Someone else in sight, as another person sees them (the game side fills it from the ticker's list).
        public sealed class Nearby
        {
            public string Id, Name, Temper;   // Name: the first name they go by
            public bool Woman, WellOff, Drunk;
            public string Known;              // what they're known for lately: "bully" (put a face in the bar), "thief" (caught at it), or null
        }

        /// A scene that just played out near the player (and so near whoever is thinking): the kind, who was in it.
        public sealed class Witnessed
        {
            public string Kind, AName, BName, AId, BId, Town;
            public long At;                   // game minute
        }

        /// The game's weather type, as it feels to someone out in it: rain, storm, snow, dust, fog, sun, grey, or null.
        public static string WeatherOf(uint weather)
        {
            switch (weather)
            {
                case 1420204096u: case 2572975940u: case 3878058453u: case 212278652u: case 1974067816u: return "rain";   // rain, drizzle, shower, sleet, hail
                case 3061285535u: case 2082228755u: case 839715153u: return "storm";                                     // thunder, thunderstorm, hurricane
                case 4021743606u: case 603685163u: case 669657108u: case 725623432u: case 2137137442u: case 1679686673u: return "snow";
                case 2977915153u: return "dust";                                                                          // sandstorm
                case 3592150529u: case 1500834021u: return "fog";                                                          // fog, misty
                case 1632247697u: case 4121459557u: return "sun";                                                          // sunny, high pressure
                case 3146353965u: case 433385945u: return "grey";                                                          // overcast, dark overcast
                default: return null;
            }
        }

        private static readonly Dictionary<string, string[]> ByWeather = new Dictionary<string, string[]>
        {
            { "rain", new[] {
                "Rain finds the one gap in a collar. Every time, the one gap.", "Boots full of water. Squelch, squelch, all the way home.",
                "Smell of wet dog and wet wool. Every soul in town smells the same today.", "Should've brought the oilskin. Said that this morning, too.",
                "Rain on the tin roof like a drummer who's lost the tune.", "Puddles the width of the street. Everybody's a tightrope walker today.",
                "Rain on the window and a dry roof over me. Small mercies.", "Mud to the ankle. Everybody walks like an old man in this.", "Rain's the only thing in this town that never takes a day off.", "Water running off every hat brim in the street. A town of little waterfalls.", "Wet saddle, wet coat, wet everything. Even my thoughts are damp.", "Drumming on the boardwalk roof all afternoon. Puts a man to sleep standing.",
                "Hat brim's a gutter. Every time I nod, it empties down my front.",
                "Rain's washing the horse. First bath she's had since spring.",
                "Every man on the street walks hunched, like he owes the sky money.",
                "Rain got into the tobacco pouch. There goes the day.",
                "Wagon's sunk to the axle at the crossing. Three men pushing, one swearing.",
                "Slicker keeps the rain out and the sweat in. Wet either way.",
                "Water off the store awning straight down my neck. Aimed, it felt like.",
                "Chickens huddled under the wagon like a church meeting.",
                "Rain's laid the dust at least. Small favor for a soaked collar.",
                "Gutters running like little creeks. Children floating sticks in them.",
                "Smell of wet pine and woodsmoke. Not the worst smell this town's had.",
                "Coat weighs double soaked. Walking like I've a sack of meal on my back.",
                "Dog shook himself beside me. Got me with the half the rain missed.", } },
            { "storm", new[] {
                "Thunder like that, my mother used to cover the mirrors.", "Counted between the flash and the boom. Getting closer.",
                "Horses don't like it. Can't say I do.",
                "Lightning showed the whole street white. Then black. Then every dog in town.",
                "Hail now. Hail on the hat like somebody throwing gravel.", "Shutters banging down the row. Somebody's barn door is going to go.",
                "Wind took somebody's laundry clean over the church.", "Every flash I count the windows. Still all there.", "Storm like this, my father used to say the Lord's moving furniture.", "Thunder rolls down the valley like a wagon on a bad road.", "Smell of struck earth after the flash. Like a match the size of a barn.", "Water coming down so hard it bounces back up off the street.",
                "Hair on my arms stands up just before the flash. The body hears it coming.",
                "Dog under the boardwalk, shaking. Wish I fit under there with him.",
                "Thunder so close it rattled the coins in my pocket.",
                "Wind turned the umbrella inside out. Let the wind have it.",
                "Tree down across the east road. Nobody's leaving town tonight.",
                "Rain coming sideways. Left side soaked, right side dry. Only half drowned.",
                "Lamp flickers every time it thunders, like it's scared too.",
                "Church bell rang once by itself in the wind. Gave me a turn.",
                "Water pouring off the roof faster than the barrel can take it.",
                "Crack so loud I felt it in my teeth. Tree on the hill split clean.",
                "Lightning lit the hills like a photographer's flash. Then gone.",
                "Cattle bunched in the corner of the pen, heads low, waiting it out.",
                "Sky's gone green at the edges. Never seen it that colour. Don't like it.", } },
            { "snow", new[] {
                "Can't feel my toes. Haven't for an hour. That's bad, isn't it.", "Snow muffles everything. Town sounds like it's holding its breath.",
                "Breath freezing on the scarf. Stiff as a board by the second street.",
                "Snow on the hat brim, snow down the neck. Same snow, different complaints.",
                "Snow squeaks under the boot. Means it's colder than it looks.", "Drifts up to the sill. The door opens inward, thank God.",
                "Snow on the roofs makes even this town look decent.", "Footprints in the snow tell you who went where. Glad nobody's reading mine.", "Hands so cold the buttons won't do up.", "Children throwing snow at the schoolhouse. Teacher throwing it back.", "White as far as I can see, and the cold comes up through the boots.", "Stove's eating wood like it's paid by the stick.",
                "Horse in snow to the stirrups, lifting every foot like a question.",
                "Eyelashes freezing together. Blink slow or they stick.",
                "Shovelled the walk this morning. It's filled in behind me like I was never there.",
                "Sleigh bells down the street. Somebody's glad of all this.",
                "Coming down slow and quiet now. Near enough makes a body forgive it.",
                "Rabbit tracks cross the street and just end. Hawk, I'd say.",
                "Fingers so stiff I can't count my change. Storekeeper counted it for me.",
                "Icicles off the eaves long as a man's arm.",
                "Snow's got a blue look under the lamp. Pretty, and it'd kill you.",
                "Horse's breath smoking like a kettle. Mine too.",
                "Fell on the ice outside the post office. Nobody laughed. Good town.",
                "Coal smoke hangs low in the cold. Every chimney going.",
                "Snow's hid all the mud. For once the street looks clean.", } },
            { "dust", new[] {
                "Grit in my teeth, grit in my eyes. I'll be tasting dirt till Tuesday.", "Can't see the end of the street. Nor the man standing in it.",
                "Wind's carrying half of somebody's field. Hope it wasn't mine.",
                "Tied a kerchief over my mouth. Still taste the road.",
                "Shut my eyes and the dust finds a way in anyhow.", "Every hat in the street held on with one hand.",
                "Dust in the coffee, dust in the bread. Eating the road.", "Sky's gone the colour of a brick.", "Every window in town shut tight and the dust still gets in.", "Horse turns his tail to the wind and waits. Wise animal.", "Blew my nose and it came out brown.", "Wind's been at it since dawn. Feels like it's after something.",
                "Dust devil spun down the street and took a hat with it. Not mine, this time.",
                "Rubbed my eyes. Made it worse. Every time, and I still rub them.",
                "Sun's a pale coin up there behind the dust. Can look straight at it.",
                "Washing on the line's gone brown. Somebody's wife will be crying in a yard tonight.",
                "Man two feet away and we're both shouting to be heard.",
                "Grit under the collar, rubbing like sandpaper. The shirt's a punishment.",
                "Teeth gritty as a millstone. Swallowing half a road a day.",
                "Horse sneezed three times running. Bless him.",
                "Dust settled in the creases of my hands like ink. Won't wash out.",
                "Shopkeeper sweeping the step again. Fifth time, I'd guess. Wind always wins.",
                "Wind keeps rattling the sign over the store like it wants the name off.",
                "Can taste the next county on my lip.",
                "Window sills furred brown by noon. Wrote my name in it with a finger.", } },
            { "fog", new[] {
                "Fog this thick, a man could walk right past his own brother.", "Everything wet and grey and close. Like being inside a cloud's pocket.",
                "Hoofbeats and no horse. Fog does that. I don't care for it.",
                "Lamp lit at noon. Can't see the end of my own arm.",
                "Church bell somewhere in the white. Can't say which way.", "Fog stays in the coat an hour after it's lifted.",
                "Fog off the water. Smells of mud and fish and something older.", "Somebody coughed in the fog and I nearly drew. Nerves.", "The street's gone. Just the boards under me and the white.", "Hear a wagon coming and can't see it. Stood still till it passed.", "Fog beads on my sleeve like sweat.", "Morning fog. It'll burn off by ten, they say. They always say.",
                "Street lamps hang in the air with nothing holding them up.",
                "Heard my name called somewhere in it. Nobody there. Walked faster.",
                "A dog barking close by and nowhere at all.",
                "Wet on the face like a handkerchief nobody will take away.",
                "Can't see the church steeple. Town's lost its faith overnight.",
                "Passed a man close enough to touch. Neither of us spoke. Never saw his face.",
                "Bumped a hitching post. Said sorry to it before I knew.",
                "Lantern across the street's a smudge, like a thumb on a window.",
                "Fog swallowed the train whistle halfway through.",
                "Voices carry strange. Heard a whole quarrel and never saw who.",
                "Damp gathers on the hat and drips off the brim like rain that forgot itself.",
                "Trees on the ridge gone. Like somebody rubbed them out.",
                "Horse walking slow, ears forward. Trusting her more than my own eyes.", } },
            { "sun", new[] {
                "Sun on the back of my neck like a warm hand.", "Squinting so long my face'll stay this way.",
                "Good drying weather. Washing on every line in town, flapping like flags.",
                "Hot enough to fry an egg on the hitching rail. Somebody ought to try.",
                "Glare off the river like a struck match. Hat brim's no use.", "Horses standing in whatever shade a post makes.",
                "Shade's worth a dollar a foot today.", "Sweat in the hatband by nine. Long day of it.", "Sun's so bright the white houses hurt to look at.", "Dog lying flat under the porch. Smartest one on the street.", "Sun on the water trough, flies on the sun. Summer.", "Hot wind like an oven door left open.",
                "Backs of my hands going brown as a saddle.",
                "Canteen water's warm as tea and tastes of the tin.",
                "Shadows sharp as cut paper. Every post has a black twin lying beside it.",
                "Clear blue since dawn, and every farmer in town scowling up at it.",
                "Boardwalk's hot through the boot soles. Walking like a cat on a stove lid.",
                "Eyes shut, sun on the lids, all red inside. Could stand here till supper.",
                "Sun's got the tar weeping between the planks.",
                "Water in the horse trough warm as a bath by noon.",
                "Far end of the street wobbles in the heat like air over a stove.",
                "Neck's burned red where the collar stops.",
                "Butter sliding off the bread before I can get it to my mouth.",
                "Shade of the bank building's the busiest spot in town.",
                "Good honest sun. Corn will like it, if nobody else does.", } },
            { "grey", new[] {
                "Sky the colour of dishwater. Can't make up its mind.", "Grey day. Grey mood. One follows the other like a dog.",
                "Can't make up its mind to rain. Can't make up mine to work.",
                "Low cloud sitting on the roofs like a hen on eggs.",
                "Grey since dawn. Grey till dark, I'd wager. A day for mending harness.", "Not a shadow in the street. Everything looks the same age.",
                "Sky pressing down like a lid on a pot.", "Grey and still. Even the birds have nothing to say.", "Couldn't tell you what hour it is by the light. Could be any.", "Weather that can't decide. I know the feeling.", "Damp in the air that's not quite rain. Gets in the joints.", "Dull light, dull work, dull thoughts. Good day to get nothing done.",
                "Washing's been on the line since yesterday and it's no drier.",
                "Crows on the fence, sky like slate behind. Only things out here that look sure of themselves.",
                "Not cold, not warm. A day with no opinions at all.",
                "Chimney smoke goes straight up and stops flat under the cloud.",
                "Every coat on the street the colour of the sky. Town's dressed to match.",
                "Red barn looks redder against all this grey. Small thing. I'll take it.",
                "Light so flat, can't tell a puddle from the road till I'm in it.",
                "Everybody talking low today, like the clouds might hear.",
                "Sun showed once at noon, the size of a dime. Then thought better of it.",
                "Day feels like waiting on a letter.",
                "Dishwater, stone, ash. Only colours left in the world today.",
                "Damp in the woodpile. Fire won't catch. Grey inside and out.",
                "Pigeons on the roof ridge, the same colour as the sky.", } }
        };

        private static readonly Dictionary<string, string[]> ByPlace = new Dictionary<string, string[]>
        {
            { "saloon", new[] {
                "One drink. Then home. Said that two drinks ago.", "Piano's half a note out. Nobody minds after the third glass.",
                "Sawdust on the floor to soak up what nobody wants to look at.", "Every man in here is telling a story bigger than his life.",
                "Spittoon's been missed more than hit tonight.", "Same four men at the same table. They were there when I came in last year.",
                "Beer's warm, flat and a nickel. Paid more for worse.",
                "Brass rail under my boot. Only thing in here that gets polished.",
                "Big painting over the bar of a lady in not much at all. She's seen more of this town than the preacher.",
                "Card game in the corner's gone quiet. Quiet at a card table is worse than shouting.",
                "Bought the house a round to be liked. Liked for exactly as long as the round lasted.",
                "Face in the mirror behind the bottles looks done in. I'd stand it a drink if it weren't mine.",
                "Somebody's singing about his mother again. Every saloon, every night, somebody's mother.",
                "Pickled eggs and crackers. Third supper running off this bar.",
                "Elbows stick to the bar. Best not to ask what with.",
                "Smoke so thick the lamps wear haloes. Only saints in here tonight.",
                "Lost two bits at faro, won a dime back. Fifteen cents of education.",
                "Every time the doors swing, every head turns. Then back to the glass." } },
            { "church", new[] {
                "Lord, I don't ask much. Just a little less than I've been getting.", "The pew creaks every time I shift. Like it's keeping count.",
                "Preacher's looking right at me. Or I feel like he is. Same thing.",
                "Hymn's one I know. Mouth the words. Nobody's listening to me, least of all Him.",
                "Collection plate coming. Hand's in the pocket already, weighing which coin.", "A hat in the pew ahead the width of a wagon wheel. The sermon comes through it, worse luck.",
                "Sermon on the lost sheep again. Preacher looks at the back pew every time he gets to the sheep.",
                "Organ wheezes on the low notes like an old man on stairs.",
                "Child behind me kicking the pew. I'd kick it too, six years old and shut in here all morning.",
                "Every lady fanning herself with the undertaker's paper fan. His name's on the back. Clever man.",
                "Light through the coloured glass lies red on my hands. Looks like I've done something.",
                "Head bowed for the prayer. Counted the knots in the floorboards instead. Fourteen.",
                "Widow in the front pew, still in black after three years. Nobody's going to be the one to say it.",
                "Benediction. Best word in the whole service. It means dinner." } },
            { "store", new[] {
                "Prices up again. Molasses costs like medicine now.", "Write it in the book, I'll say. The book is getting long.",
                "Shopkeeper weighs with his thumb. Everybody knows. Nobody says.",
                "Shopkeeper's got the good flour behind the counter. Always has. Never for me.",
                "Jar of peppermints by the till. For children, he says. Looks at me when he says it.", "Cloth by the yard, and the yard's shorter than it was.",
                "Stood at the counter so long I've read every label on the shelf. Twice.",
                "Cracker barrel's open. Took one. Nobody saw. Feel like a thief over a cracker.",
                "List in my fist, and I can't make out the third thing. Bought what I'd guess was meant.",
                "Shopkeeper says it's the last tin. Said that last week about the same tin.",
                "Nickel-trimmed stove in the window. Stopped to look at it again. It doesn't know me.",
                "Old men round the stove at the back, settling the country's business. Never buy a thing.",
                "Bolt of red calico on the counter. Thought of my daughter. Bought the brown.",
                "Penny candy in glass jars. Haven't had a lemon drop since I was nine. Still won't buy one." } },
            { "genstore", new[] {
                "Prices up again. Molasses costs like medicine now.", "Coffee, flour, lamp oil. Coffee, flour, lamp oil. Don't forget the lamp oil.",
                "Nails by the pound, and the pound gets lighter every year.",
                "Lamp oil's gone up again. Dark is free, at least.",
                "Smell of coffee beans and rope. Could stand here all day. Can't afford to.", "He's out of the one thing I came for. He's always out of the one thing.",
                "Asked at the mail window in the back. Nothing for me. Asked like I knew there'd be nothing.",
                "Seed packets on the wall with pictures of cabbages bigger than any that grew.",
                "Traded a dozen eggs for a spool of thread and a nickel's credit.",
                "Somebody's dog asleep across the doorway. Everyone steps over him. Nobody moves him.",
                "Clerk goes up the ladder for the top shelf like a sailor up a mast.",
                "Flour on my shoulder and a dime short for the coffee. Coffee waits.",
                "Same big knife cuts the plug tobacco and the cheese. Never wiped between.",
                "Wanted a new axe handle. Held six of them. Bought the one I'd held first." } },
            { "hotel", new[] {
                "Paid for a bed and got a lumpy argument with a mattress.", "Walls thin enough to hear a man think next door.",
                "Clerk looks at your boots before your face. Decides the price by the boots.",
                "Bed's been slept in by every drummer in the state. You can tell.",
                "Stairs creak a different note on every step. Somebody could play a tune.", "Spittoon in the lobby, polished, and nobody uses it. That's how you know it's a good hotel.",
                "Water in the basin was left by the last guest. Washed my face in it anyway.",
                "Roses climbing up the wallpaper. Counted two hundred and I'm still awake.",
                "Chair wedged under the doorknob. Not that I'm worried. Habit of the road.",
                "Clerk asked how long I'm staying. I'd like to know that myself.",
                "Dining room serves one stew under three names.",
                "Card on the door says no spurs on the bedding. Somebody's spurred the bedding.",
                "Man in the lobby's been on the same page of the paper since noon. Watching the stairs, I'd say.",
                "Ironed sheets. Lay a minute on top of them just for that." } },
            { "barber", new[] {
                "Sit still. Man with a razor at your neck, you sit still and agree with him.", "Smells of bay rum and other men's gossip in here.",
                "Barber knows everything that happens in town a day before it happens.",
                "Hair on the floor. Some of it mine, some grey. More grey than I thought.",
                "Razor strop going like a whip. Soothing, somehow.", "Hair from three heads on the floor and nobody has swept since noon.",
                "Towel over my face, steaming. Can't see the room. Best two minutes of the week.",
                "Barber asks after my plans. Plan is to leave this chair with both ears.",
                "Mirror shows a man with half a beard. Halfway to respectable.",
                "Waiting my turn on the bench. Paper's a week old and folded soft as cloth.",
                "Lathered to the eyebrows. He says I've a strong chin under there. Says it to everybody.",
                "Bell on the door rings and the man in the chair flinches. The razor doesn't.",
                "Shave's a quarter. The talk comes free, and there's no refusing it.",
                "He trimmed the hair in my nose without asking. Didn't know I'd let it go so far." } },
            { "bath", new[] {
                "Hot water. Real hot water. Could weep.", "Third in the same bathwater. Don't think about it. Don't think about it.",
                "Water was hot once today. For somebody.",
                "Two bits for a tub and a towel that's seen a war.",
                "Steam on the window and the street going by outside, not knowing.", "Soap the size of a biscuit and worn to a sliver before it's mine.",
                "Boy brings another bucket of hot. Pour it slow, I tell him. Make it last.",
                "Knees out of the water or shoulders out. Tub was built for somebody smaller.",
                "A week of road came off me and turned the water brown. Didn't know I was carrying so much.",
                "Fellow behind the curtain is singing. Badly. Happily.",
                "Water's going cold. Not getting out. Not yet.",
                "Scrubbed till I'm pink. Found a scar I'd clean forgotten.",
                "Lye soap finds every cut I own. Didn't know I owned so many.",
                "Steam in the lungs. First breath all week that didn't taste of dust." } },
            { "photo", new[] {
                "Hold still for the camera, he says. I've held still my whole life.", "Folks pay to have their faces kept. Wonder who'd want to keep mine.",
                "Face on a card for ever. Mine's no face for ever.",
                "The fellow under the cloth says don't breathe. Hard not to.",
                "Stiff collar, stiffer smile. The card will show a stranger.", "He says the picture lasts a hundred years. Which of us will know.",
                "Borrowed his coat for the sitting. Fits better than mine. That's the coat they'll remember me in.",
                "Iron brace behind my neck to keep the head still. Like the dentist's chair.",
                "Fixed my collar four times in his little mirror. The camera won't notice. I'd notice.",
                "One card for Mother, one for my sister, one to keep in case I forget what I looked like.",
                "Don't blink, he says, and counts. I blinked at nine.",
                "Flash powder went off like a pistol. Still seeing a white square everywhere I look.",
                "Painted columns behind me. Never stood near a column in my life.",
                "He's got me holding a book I've never read. Look learned, he says." } },
            { "fence", new[] {
                "Don't ask where any of it came from. That's the rule in here.", "Every watch in that case stopped at the minute somebody lost it.",
                "Half the town's lost property on one shelf. Priced by the ounce.",
                "Man behind the counter never asks a name. That's the trade.",
                "Ring on the tray I'd swear I saw on a hand at church last spring.", "Nobody in here asks where a thing came from. That's the trade.",
                "He holds the watch to his ear like a doctor. Says it has a weak heart. Offers two dollars.",
                "Back room, no window, one lamp. Everybody in here talks to the floor.",
                "Brought him a silver spoon. He knew whose house it left before I said a word.",
                "Price goes down the longer I stand here. He knows I won't carry it back out.",
                "Christening cup on the shelf, the name scratched off. Somebody's baby, once.",
                "Paid in coin, no questions. Still feel I've been robbed somewhere in it.",
                "Three bolts on his door. He draws each one slow and looks at me before the next.",
                "Cigar box full of wedding rings, tangled like worms after rain." } },
            { "bank", new[] {
                "Banker smiles like he's counting your teeth for gold.", "Every coin in that vault was somebody's sweat. Never the banker's.",
                "Vault door thick as a church wall. Mine's the only money in there that worries.",
                "Clerk counts slow so you can watch your money leave.",
                "Marble floor. Marble, and the town's mud on it.", "Teller's cage. Which side of the bars is the prisoner on.",
                "Pen on a chain at the counter. They'll keep my money but they won't trust me with a pen.",
                "Clock on the wall ticks louder in here. Every tick is somebody's interest.",
                "Took my hat off at the door without thinking. Never did that for the judge.",
                "Signed the slip in my best hand. Clerk looked at it like it was mud on his floor.",
                "Three dollars to deposit. He writes it in a ledger the size of a tombstone.",
                "Asked about a loan. He asked about my land, then my people, then said no.",
                "Man ahead of me drew out the lot, counted it twice and left without his hat.",
                "Everybody whispers in a bank, like the money's asleep." } },
            { "doctor", new[] {
                "Smells of carbolic and bad news in here.", "Doctor says rest. Rest doesn't plough a field.",
                "Everybody in this room is pretending it's nothing. Including the doctor.",
                "Jar of something on the shelf. Don't want to know what.",
                "The chair where everybody waits. The dent in it's from worse news than mine.", "Doctor's hands smell of soap. Mine smell of what I do.",
                "Say ah, he says. I said ah. He wrote something down and didn't tell me what.",
                "Diploma on the wall from a college back east. The frame's worth more than my horse.",
                "Skeleton in the corner, wired at the joints. Somebody's grandfather, I'd wager.",
                "He listens to my chest a long while. It's the long that worries me.",
                "Bottle of tonic, a dollar. Smells like whiskey, tastes like whiskey, says tonic on the label.",
                "Child crying in the back room, then not. The not is worse.",
                "Didn't tell him about the drinking. Didn't need to. He looked at my hands.",
                "Four chairs and a druggist's calendar. Read the whole year twice waiting." } },
            { "sheriff", new[] {
                "Never stood in a lawman's office without feeling guilty of something.", "Wanted posters. Looking at the faces, half of them could be me.",
                "Cell door's open. Still don't like standing this near it.",
                "Sheriff's boots on the desk. The law at its ease.",
                "Keys on a ring the size of a dinner plate.", "Rifle rack behind the desk. Every one of them clean. Every one loaded, I expect.",
                "Prisoner asleep in the cell, boots and all. Snores like he's paying for the room.",
                "Came in to report a stolen saddle. Deputy wrote it down on the back of an envelope.",
                "Old coffee, lamp smoke, and a bucket in the cell that wants emptying.",
                "Stove's red, door's shut, and I'm sweating for no reason I could swear to.",
                "Deputy whittling at the desk. Shavings all over a stack of warrants.",
                "He writes slow with his lips moving. Takes my name down like he's carving it.",
                "Calendar on the wall's a month behind. Only the prisoners count days in here.",
                "Five hundred dollars on that handbill. Looked at the face longer than I meant to." } },
            { "station", new[] {
                "Train's late. Train's always late, and always worth the wait, they say. They lie.", "Every train out of here is somebody's second chance.",
                "Smell of coal smoke and leaving.",
                "Watched the train pull out. Wasn't on it. Never am.",
                "Clock on the wall runs the railroad's time, not the town's. Nobody's told the clock.", "Trunks stacked for somewhere. A whole life in each, strapped shut.",
                "Ticket in my glove. I keep pressing it to be sure it's there.",
                "Bench worn smooth by a thousand waiting backsides. Mine fits right in.",
                "Woman beside me with a crate of hens and a Bible, going to her son's.",
                "Rails start to sing before the train shows. Everybody stands up at once.",
                "Agent behind the grille tapping at the key, never looking up. Wonder what the wire says about us.",
                "Carpetbag on my knees. All I'm taking fits on my knees.",
                "Platform swept and flowers in a barrel. They want you to feel good about leaving.",
                "Blackboard says on time. Chalk will say anything." } },
            { "stable", new[] {
                "Horse smell and hay. Better company than most people.", "That grey mare watches me like she knows what I owe.",
                "Mucking out is honest work. Smells honest, too.",
                "Horses know when you're in a hurry. They slow down for it.",
                "The grey in the end stall kicks the boards when a stranger comes. Kicked for me.", "Bran mash cooking. Horses eat better than the boarding house.",
                "Stable boy brushes my horse better than anyone's ever brushed me.",
                "Swallows in the rafters coming and going free while the horses stand and wait.",
                "Cat on the grain bin, watching a hole in the boards. Only one in here at work.",
                "Hay in my hair, my collar, my boots. There'll be hay in my supper.",
                "Horse at the end nickers when I come in. Not mine. Wish he were.",
                "Neatsfoot oil and old leather in the tack room. Best room in town and nobody sleeps in it.",
                "Stall boards chewed half through. A bored horse is a carpenter backwards.",
                "Lantern on a nail sways when the wind gets in. Every horse watching the shadows go." } },
            { "tailor", new[] {
                "A good coat makes a man taller. I'll need a very good coat.", "Measured for clothes I can't afford. Tailor's tape around my middle like a judgement.",
                "Pins in his mouth and opinions behind them.",
                "Fine cloth. Finer than anything that'll ever touch my back.",
                "Chalk marks on the cloth like a map of a man who's not finished.", "A coat's a year's wages here. A year, hung on a nail.",
                "Arms out like a scarecrow while he measures. Feel a fool. Stand straighter anyhow.",
                "He says I carry my left shoulder high. All these years and nobody told me.",
                "Grey wool or brown. Picked the brown. Picked the brown last time too.",
                "Mirror in three panels. Three of me from three sides, and not one I'd choose.",
                "Pinned into the trousers. One of the pins is pinned into me.",
                "My measure's in his book from three years back. The waist's been crossed out twice.",
                "Second fitting. It's a coat now, not a hope.",
                "Cloth's from England, he says. So was my grandfather, and he came cheap." } },
            { "gunsmith", new[] {
                "Rows of guns, all oiled and waiting. Waiting for what, I wonder.", "Man ahead of me bought shells for a rifle he doesn't own. Odd.",
                "Smell of oil and the quiet of men being careful.",
                "He hands over a pistol the way a preacher hands over a Bible.",
                "Click of a hammer being tried. Everybody in the room stops to hear whether it fires.", "Boxes of shells in rows like a church choir.",
                "Picked a pistol up to feel the weight. Put it down quick, like it was warm.",
                "He knew what ailed my rifle from the sound of it set on the counter.",
                "Engraved revolver in the case, silver and pearl. Costs more than my house. Shoots the same.",
                "Paper targets on the wall, every one shot through the middle. Sales talk, that.",
                "Brought my father's shotgun in for cleaning. Like bringing the old man to the doctor.",
                "Men in here talk low and stand sideways.",
                "Door bell jangles, and every hand in the place drops an inch toward the hip.",
                "He's casting bullets in the back. Air tastes like a penny." } },
            { "theatre", new[] {
                "Paid a dime to watch other folks pretend. Do it for free all week.", "The villain had the best lines. The villain always has the best lines.",
                "Painted sea on the backcloth. Nearest I'll get to the real one.",
                "Woman next to me cried at the ending. I'd have cried at the price.",
                "Tobacco ground into the plush. Somebody sat out a long second act here.", "The girl in the front row knows the words. Mouths them. Every one.",
                "Seats as hard as pews. Better sermon, though.",
                "Footlights smoking and the leading lady's eyes streaming. That, or she's very good.",
                "Somebody behind me cracking peanuts all through the death scene.",
                "Orchestra's three men and a drum, playing like they're twelve.",
                "Hero's moustache came unstuck in the second act. Held it on with a thumb and kept going.",
                "Snow in the play is torn paper from the rafters. Prettier than any I've stood in.",
                "Gallery's hissing the villain. Hissed with them. Did me good.",
                "Kept the ticket stub. Can't say why. It's in my hatband." } }
        };

        // a scene that just played out in front of them, by kind ({a} started it, {b} had it done to him)
        private static readonly Dictionary<string, string[]> BySeen = new Dictionary<string, string[]>
        {
            { "rough", new[] {
                "Heard {b}'s face hit the wood from across the room. Sound like a dropped melon.",
                "{a} moved so fast. One moment talking, next {b}'s face is in the bar.",
                "Nobody stopped {a}. Nobody ever stops a man like {a}. I didn't either.",
                "{a} had {b} by the collar before anybody blinked.",
                "Never heard a man make the sound {b} made. Hope I never do again.",
                "{a} wiped his hands after. Like he'd been gutting a fish.",
                "Whole room went quiet when {a} grabbed {b}. Then louder than before.",
                "Could've been me in {a}'s hands. Could've been anybody.",
                "{b} sat there after, holding his face. Nobody went to him at first.",
                "Barkeep just looked at {a}. Didn't say a word. Neither did I.",
                "{a} has a temper on him like a struck match.",
                "Blood on the wood where {b}'s face went. I keep looking at it.",
                "Should somebody fetch the sheriff for {a}? Not me. Not tonight.",
                "Glad I was sat at the far end, away from {a}.",
                "{b} won't be eating supper easy tonight.",
                "{b}'s chair went over and {b} with it. {a} didn't even breathe hard.",
                "Heard the crack before I saw anything. Then {a} let go.",
                "Smiling the whole time, {a} was. That's what I'll remember.",
                "Somebody's drink went in my lap when {a} shoved past. Didn't complain.",
                "Picked up {b}'s hat after. Didn't know what else to do with my hands.",
                "Bottles rattled on the shelf when {b} hit the bar. Then nobody moved.", } },
            { "brawl", new[] {
                "{a} and {b} going at it like dogs. Heart's still pounding and it wasn't even my fight.",
                "Blood on the boards. Somebody'll want to sweep that.",
                "{a} and {b} rolling in the dirt like schoolboys.",
                "Somebody's hat went under a horse. Think it was {b}'s.",
                "{a} threw the first one. I'd swear to it.",
                "Never knew {b} had a punch like that in him.",
                "Folks cheered {a} and {b} on. Folks are no better than dogs, some days.",
                "Heard a tooth go. {a}'s or {b}'s, couldn't say.",
                "Took two men to pull {a} off {b}.",
                "My hands were fists the whole time and it wasn't even my quarrel.",
                "{b} got up slow. {a} got up grinning.",
                "Dust everywhere. {a} and {b} both brown with it.",
                "Ma would've thrown a bucket of water on {a} and {b} both.",
                "Whole street'll have it by supper. {a} and {b}.",
                "A chair leg went past my ear. {a} or {b} threw it, no telling which.",
                "{b} came up spitting dirt and swinging at the air.",
                "Dog got in on it too, nipping at {a}'s boots.",
                "Lost a button squeezing back from the crowd round {a} and {b}.",
                "Sheriff came strolling up after it ended. Timing like that is a gift.",
                "Bets went down on {a} before the second punch. I didn't. Nearly.", } },
            { "drunk", new[] {
                "{a}, out cold on the floor, and him fixing to fight the world.",
                "Laughed at {a}. Shouldn't have. Could be me one day, a bottle further on.",
                "{a} went down like a tree in a storm. Floor won that one.",
                "{a} wanted to fight the whole room. The whole room laughed.",
                "Smelled the whiskey on {a} from the door.",
                "{a} tried to stand, twice. Sat back down, twice.",
                "{b} just looked at {a} like a man looks at a dog that's been sick.",
                "Somebody ought to see {a} home before he hurts himself.",
                "Drink makes a fool of the best of them. {a}'s no best of them.",
                "{a} called {b} a coward and then fell on his face.",
                "Felt sorry for {a}, after the laughing stopped.",
                "{a}'ll wake on that floor and remember none of it. Lucky.",
                "Pa drank like {a} does. Never ended well.",
                "{b} walked off and left {a} to it. Can't blame {b}.",
                "{a} sang a hymn to the spittoon. Knew all the verses, too.",
                "Lost a boot somewhere between the bar and the door, {a} did.",
                "Had to step over {a} to get to my seat. Nobody else bothered to move.",
                "{b} put a coat under {a}'s head. Kinder than I'd have been.",
                "A horse will carry {a} home tonight. Horse knows the way better.",
                "Watched {a} pay for a round with a button. Barkeep took it, too.", } },
            { "pickpocket", new[] {
                "Thief! Right in front of me. Hand in {b}'s coat, quick as a snake.",
                "Checking my pockets now. Twice. Everybody is.",
                "Saw the hand go in {b}'s coat and come out quick.",
                "{a}'s fingers moved like a card sharp's.",
                "{b} never felt it. That's the frightening part.",
                "Thief, in daylight, bold as you like. {a}, of all people.",
                "My purse is in my hand now and staying there.",
                "{a} looked right at me after. Knew I'd seen.",
                "Should I tell {b}? Should I? Lord, what if {a} sees me telling.",
                "Moved to the other side of the street from {a}.",
                "{a} dressed so respectable, too.",
                "Can't trust a crowd now. Every elbow's a thief.",
                "{b} shouted after {a}. Too late. Always too late.",
                "Patted every pocket I've got. All there. Still feel robbed.",
                "Felt my own coat after, sure somebody'd been at it too.",
                "{a} bumped {b}, said beg pardon, and was gone. Neat as a tailor.",
                "{b}'s watch chain's hanging empty. {b} hasn't looked down yet.",
                "Wonder how many times I've walked past {a} with my purse loose.",
                "Kept my eye on {a} the length of the street. Lost them at the corner.",
                "Pinned my pocketbook inside my vest. My mother's trick, and a good one.", } },
            { "argue", new[] {
                "{a} and {b} going at each other. Pretended not to listen. Heard every word.",
                "Raised voices. The whole street leaned in.",
                "{a} and {b} at it hammer and tongs.",
                "Heard {b}'s name and some words my mother wouldn't care for.",
                "{a}'s face was red as a brick.",
                "Folks pretend not to listen to {a} and {b}. Everybody listens.",
                "Whatever's between {a} and {b}, it's been brewing a while.",
                "Dog started barking when {a} raised their voice.",
                "{b} gave as good as got. Better, maybe.",
                "Hope {a} and {b} settle it with words and leave it there.",
                "Couldn't follow the half of it. Something about a fence, I think.",
                "{a} walked off muttering. {b} stood there shaking.",
                "Bad blood between {a} and {b}. Everybody knows it now.",
                "Glad it's not me {a}'s shouting at.",
                "Window up above flew open to hear {a} and {b} better.",
                "{b} counted off grievances on their fingers. Ran out of fingers.",
                "Every word {a} said came out louder than the one before.",
                "A wagon stopped in the road so the driver could hear {a} and {b}.",
                "Pretended to read a handbill the whole time. Couldn't say what it said.",
                "Started with a borrowed plow. Ended with everything from the last ten years.", } },
            { "standoff", new[] {
                "Guns out, right there in the street. My knees haven't stopped.",
                "Held my breath the whole time. Still holding it, nearly.",
                "Hands on their guns, {a} and {b}. My hands were in my pockets.",
                "Thought for sure somebody was going to die in that street.",
                "Children pulled indoors. Shutters banged. Then nothing.",
                "{a} looked like he meant it. {b} too.",
                "Sweat running down my back and it isn't even hot.",
                "Somebody said fetch the sheriff. Nobody went.",
                "When it ended, half the street let out a breath at once.",
                "{a} and {b}, staring like a pair of tomcats.",
                "Never seen a man's hand shake like {b}'s. Mine did too.",
                "Pride. All that for pride.",
                "Lord, I thought I'd be seeing a funeral this week.",
                "Wind kicked dust between {a} and {b}. Nobody blinked.",
                "Heard a baby crying behind a shutter. Only sound in the whole street.",
                "{b}'s fingers kept opening and closing. Like counting to something.",
                "Crouched behind a water trough. Wouldn't stop a bullet, a trough.",
                "A horse stamped and three men nearly drew on it.",
                "{a} walked away first. Took more nerve than drawing, I'd say.", } },
            { "flirt", new[] {
                "{a} sweet-talking {b} in broad daylight. Some people have no shame. Good for them.",
                "{a} has a way with words. {b} seemed to think so.",
                "Saw {b} blush clear to the ears.",
                "Young folk. Were we ever that foolish? I was.",
                "{a} leaning on the post like a dandy for {b}'s sake.",
                "Smiled to myself, watching {a} and {b}. First smile today.",
                "Wonder if {b}'s people know about {a}.",
                "Spring in the air, and it's not even spring.",
                "{a} said something and {b} laughed like a bell.",
                "Reminded me of courting days. Long gone, those.",
                "{b} looked back over their shoulder at {a}. That's the sign.",
                "Gossip'll have {a} and {b} married by Sunday.",
                "Envy {a} a little. Just a little.",
                "{b} pretended not to hear {a}. Heard every word, I'd bet.",
                "{a} picked a flower from somebody's window box for {b}. Bold.",
                "Old widow beside me tutted at {a} and {b}. Then smiled when she thought nobody saw.",
                "{a} tripped on the boardwalk showing off. {b} laughed, and that was the point.",
                "Watched {b} fix their hair twice before {a} reached the step.",
                "Nobody's studied me so close since the doctor in ninety-one.", } },
            { "propose", new[] {
                "{a} went down on one knee to {b}, right there. Never seen the like.",
                "{a} asked {b} in the open street. Half the town held its breath.",
                "{a} on one knee in the dirt, hat in hand. Never seen {a} so pale.",
                "Saw {a} ask {b}. Whatever the answer, that took nerve.",
                "Ring in a twist of paper, it looked like. {a}'s hands shook opening it.",
                "Whole boardwalk went quiet waiting on {b} to answer.",
                "{b} put both hands to their mouth. Couldn't tell yes from no.",
                "Somebody's hat went up in the air at the end. Somebody cheered.",
                "{a} knelt in a puddle and never noticed. That's how you know.",
                "Whatever {b} says, {a} will remember this street for life.", } },
            { "snap", new[] {
                "{a} bit {b}'s head clean off. Something's eating at {a}.",
                "{a} bit {b}'s head off for no cause I could see.",
                "Something's eating {a} up from the inside.",
                "{b} only said good day. {a} near took it as a slap.",
                "Never seen {a} so short with anybody.",
                "Trouble at home, I'd guess. {a}'s got that look.",
                "{b} looked hurt. Walked off with their hat in their hands.",
                "Whatever's weighing on {a}, it's heavy.",
                "Gave {a} a wide berth after that.",
                "Poor {b}. Caught the end of somebody else's storm.",
                "{a}'s eyes were red. From drink or from worry.",
                "Should somebody ask after {a}? Somebody braver than me.",
                "Bad day for {a}. Worse for anyone near.",
                "{b} only asked to borrow a match. {a} acted like it was a kidney.",
                "{a} slammed the door so hard the shop bell fell off.",
                "Saw {a}'s jaw working after, like chewing the words over again.",
                "{b} stood there with their mouth open. Hadn't a thing to answer.",
                "Something sharp's lodged in {a} lately. Snags on anybody passing.",
                "Looked sorry half a minute later, {a} did. Too proud to say so.", } },
            { "cheat", new[] {
                "{b} called {a} a cheat over the cards. Never seen chairs go back so fast.",
                "{b} saw the card up {a}'s sleeve. Whole table saw it then.",
                "Chairs scraped back so fast I thought the floor would go.",
                "{a}'s been winning all week. Now we know why.",
                "Never sit at a table with {a}. Learned that free tonight.",
                "Money on the floor and nobody reaching for it.",
                "{b} said cheat out loud. Brave or foolish.",
                "{a} swore on his mother. His mother'd be ashamed.",
                "Barkeep had his hand under the bar the whole time.",
                "Thought guns would come out over {a}'s cards.",
                "Saw {a}'s hand shake picking up his money.",
                "Cards are the devil's prayer book, Ma said. {a} proved her right.",
                "Glad I lost early and went to the bar.",
                "Ace slid out of {a}'s cuff onto the felt. Nobody spoke for a long breath.",
                "{a} kept smiling like the cards would back them up.",
                "Seen men shot for less than what {a} did tonight.",
                "Somebody counted the deck. Fifty-four cards. Room got very small.",
                "{b} had the pot in both arms before {a} could reach it.",
                "Knew {a}'s luck was too good. Never said so. Wish I had.", } },
            { "jealous", new[] {
                "Jealous men and loaded tempers. Saw the whole thing, and wished I hadn't.",
                "Green-eyed and loud about it. Jealousy makes a fool fast.",
                "Two men and one sweetheart and a street full of ears. Never ends well.",
                "Two men at each other over a girl. Old as Adam, and no wiser for it.",
                "Hat knocked off in the street, all over a smile at the dance.",
                "The sweetheart stood between them looking more cross than flattered.",
                "One shoved, the other shoved back, and the cause of it walked off with a third.",
                "Heard one of them say mine. As if folks were horses at a sale.",
                "Somebody's ring went in the horse trough. Whole crowd stared at the water.",
                "Saw a letter torn up and thrown in the street. Pieces blowing all down the boardwalk.", } },
            { "debt", new[] {
                "{a} calling in money from {b}, out in the open. That's a shame on both of them.",
                "{a} calling in money from {b} in front of half the street.",
                "Money owed and tempers short. {a} and {b} both.",
                "{b} looked about ready to sink through the boards.",
                "Never borrow from {a}. Seen what it costs.",
                "{a} had a ledger out. A ledger. In the street.",
                "Whatever {b} owes {a}, it's more than money now.",
                "Glad my debts are only to the store.",
                "{b} promised Friday. {a} didn't look like they believed it.",
                "Hate to see neighbours fall out over dollars.",
                "{a}'s within their rights. Still ugly, doing it out here.",
                "Felt for {b}. Everybody's short this season.",
                "There'll be more words between {a} and {b} before this is done.",
                "{b} turned out empty pockets in front of everybody. Lint and a button.",
                "{a} counted the coins twice, out loud, so the whole street could hear.",
                "{b} offered a pocket watch instead. {a} weighed it in one palm like a melon.",
                "Folks found sudden business at the shop windows while {a} and {b} went at it.",
                "Was it {a}'s money or {a}'s pride that wanted paying?",
                "Owe a little myself. Walked faster past the store.", } },
            { "charity", new[] {
                "{b} gave {a} money, just like that. Wish somebody'd give me money, just like that.",
                "{b} pressed money into {a}'s hand. Didn't make a show of it.",
                "Good to see. Don't see enough of it.",
                "{a} near cried, thanking {b}.",
                "Wish I'd been the one to help {a}. Wish I'd had it to give.",
                "{b}'s a better soul than most in this town.",
                "Felt warm watching {b} give. Felt cheap, too.",
                "{a} looked at the coin like it was gold. Maybe it was, to {a}.",
                "Kindness in the street. Rare as rain in August.",
                "Will remember {b} for that.",
                "{a}'s hard up. Didn't know how hard.",
                "Ought to do the same, next time I'm asked.",
                "{b} walked off before {a} could even finish thanking.",
                "{b} gave {a} the coat right off their own back. Walked off in shirtsleeves.",
                "{a} held it out again. {b} folded {a}'s fingers over it.",
                "Coin went from {b}'s hand to {a}'s so quick I nearly missed it.",
                "Made me look at the dime in my own pocket a while.",
                "Preacher talks of kindness every Sunday. {b} just went and did it.",
                "Stood straighter after, {a} did. Funny what a dollar does to a spine.", } },
            { "temperance", new[] {
                "Temperance woman had a man by the sleeve outside the saloon. Brave, or foolish.", "Pledge cards on the boardwalk again. Half the men cross the street.",
                "Pledge put to a man right there in the street. I'd have run.", "Somebody ought to sign. Not me. Somebody.",
                "Those ladies will pray the paint off the saloon door one day.", "Saw a man slink off from the temperance woman like a scolded dog.",
                "Can't fault the ladies' aim. That one needed it.", "Bread money, she said. She's not wrong.", } },
            { "scorn", new[] {
                "{b} turned {a} away cold. Could've spared a nickel. Could I have? Maybe.",
                "{b} sent {a} off like a stray dog. Ugly to watch.",
                "{a} only asked for a nickel. {b} acted like it was a horse.",
                "Shame burned on {a}'s face. Could see it from here.",
                "Hard times make hard people. {b}'s plenty hard.",
                "Should've given {a} something myself. Didn't.",
                "{b} has the money. Everyone knows {b} has the money.",
                "{a} walked off with their head down. Felt that in my chest.",
                "There but for the grace of God. {a} could be any of us.",
                "{b} laughed. Didn't need to laugh.",
                "Wonder what {a} will eat tonight.",
                "Turned away myself before {a} could ask me. Not proud of it.",
                "{b}'ll want kindness one day. Hope it's measured out the same.",
                "{b} didn't even stop walking. Just waved a hand, like at a fly.",
                "{a} held their hat out, and {b} looked straight past it.",
                "Folks on the boardwalk looked away from {a} too. Me among them.",
                "{b}'s boots cost more than {a} eats in a month.",
                "Saw {a} pick up a dropped crust after. Ate it right there.",
                "Shameful, what {b} said. Worse, the laughing from the porch.", } }
        };

        // what one makes of someone else in sight, by their own nature ({name}: the other's first name)
        private static readonly string[] Envy =
        {
            "{name}'s coat cost more than my year. Wears it like it's nothing.", "Look at {name}. Never done an honest day's work and fed better than me.",
            "{name} has new boots. Where does {name} find the money? Not from working.",
            "{name} eats meat on a Tuesday. Meat. On a Tuesday.",
            "{name}'s hands are soft as a baby's. Mine could sand a floor."
        };
        private static readonly string[] Pity =
        {
            "{name} is in the bottle again. Somebody ought to walk {name} home.", "Poor {name}. Drink gets its hooks in, and it never lets go.",
            "{name} was a fine hand once. The bottle took the hands first.",
            "Somebody should feed {name} before the whiskey does."
        };
        private static readonly string[] Wary =
        {
            "Keep clear of {name}. Got that look again.", "{name} is spoiling for something. Not me. Not today.",
            "{name}'s been drinking since noon and looking for a reason. Don't be the reason.",
            "Give {name} the width of the street today."
        };
        // what the town knows of them lately: the one who put a man's face in the bar, the one caught with a hand in a coat
        private static readonly string[] KnownBully =
        {
            "That's {name}. Put a man's face through the bar top. Keep walking.", "Don't catch {name}'s eye. Folks who catch {name}'s eye end up picking teeth off the floor."
        };
        private static readonly string[] KnownBullyHot =
        {
            "So that's {name}. Let {name} try that with me. Just let {name} try.",
            "{name}, the one who put a man's face in the bar. {name} wants a bar of {name}'s own some day.",
            "There's {name}, walking like the street's {name}'s. Somebody ought to teach {name} different. Somebody might."
        };
        private static readonly string[] KnownThief =
        {
            "{name}'s the one they caught with a hand in a coat. Hand on my purse.", "Count your change twice when {name}'s about, they say."
        };
        private static readonly Dictionary<string, string[]> Watching = new Dictionary<string, string[]>
        {
            { "Proud", new[] { "Look at {name}, preening. Some folks were born to be looked at, and know it.", "{name} talks like a man who's never been told no.",
                               "{name} nodded at me like a king to a stable boy. I'll remember the nod.",
                "{name} wears that coat like it was cut for somebody grander. It wasn't.",
                "{name} spends more on hats than I do on bread.", "{name} walks the boardwalk like the boards were laid for {name}.", "{name} expects a door held. Folks do it, too.", "{name} laughs a little too loud so the room hears. I hear.", "{name} would rather be wrong than be told." } },
            { "Loyal", new[] { "{name} looks done in. Ought to ask after {name}.", "Haven't seen {name} smile in a week. Something's wrong at home.",
                               "If {name} needs a hand with that roof, I'll go Saturday. Nobody asked. Doesn't matter.",
                "{name}'s boots want mending. Ought to say. Ought to just do it.",
                "{name} looks thin. Somebody ought to bring a pie round. Somebody's me.", "{name} stood by me when it counted. I don't forget that.", "{name} won't ask for help. Never has. That's why you offer.", "{name}'s boy is running wild. Might have a quiet word with {name}.", "{name} works too hard with that cough." } },
            { "Drifter", new[] { "{name} will be standing right there when I'm long gone. Rooted like a fence post.", "Wonder if {name} ever wanted to just walk away.",
                                 "{name} knows every name in this town. I know the names of three roads out.",
                "{name} has never slept under the open sky. Can see it from here.",
                "{name} has a house and a family and a pew. Sounds heavy to carry.", "{name} gazes down the east road sometimes. I've seen it.", "{name} thinks this town is the world. It's a dot on a map.", "{name} will die within a mile of the cradle. Can't decide if that's sad.", "{name} asked where I'm headed. Said wherever. Got a jealous look for it." } },
            { "Cold", new[] { "{name} talks too much. Always has.", "{name} wants something. Folks only smile like that when they want something.",
                              "{name} cried at a funeral last month. Didn't know the man. Some folks just like to be seen.",
                "{name} says good morning to everybody. Costs nothing, means nothing.",
                "{name} remembers everybody's birthday. Must have nothing else to think about.", "{name} wants to be liked. It shows all over.", "{name} is crying again. There's always something with {name}.", "{name} gives to the collection plate where everybody can see.", "{name} tells the same story every Saturday. Same laugh at the same place." } },
            { "Hothead", new[] { "{name} keeps looking over here. Let {name} look. See what it gets.", "{name} laughed just now. At me? Better not have been at me.", "{name} stood in my way just now. Didn't mean to, likely. Likely.", "If {name} says my name in that voice once more there'll be words.",
                "{name} bumped my shoulder last week. Said sorry. Too late, I'd say.", "{name} talks big at the bar. Let {name} try it on me.", "{name} gave me a look. Saw it. Don't care to see it twice.", "{name} has a mouth. Somebody'll close it one day.", "{name} thinks I'm slow. Let {name} find out." } },
            { "Greedy", new[] { "{name} owes somebody money. You can tell by the shoes.", "{name} would sell a sick mule as a racehorse. I'd know.",
                                "Wonder what {name} paid for that hat. Too much. Folks like {name} always pay too much.",
                "{name}'s wearing new boots. Who paid for those, I wonder.",
                "{name} has a new saddle. Money from somewhere.", "{name} counts change twice. Wonder what {name} is short of.", "{name} owns half that row now. Bought it cheap from people who needed the money.", "{name} would charge a drowning man for the rope.", "{name} tips the boy at the livery. Showing off, or buying something." } },
            { "Coward", new[] { "{name} keeps looking at me. Did I do something? I didn't do anything.", "Stand behind {name} if trouble starts. {name} is wider than me.", "{name} looks like trouble waiting on a reason. Keep walking.", "Smiled at {name}. Can't hurt to be on {name}'s good side.",
                "{name} has a temper. Heard it through the wall once.", "{name} is looking for a fight today. Not with me. Please not with me.", "{name} carries a gun. Why does {name} carry a gun?", "If {name} asks, I'll say yes. Easier.", "{name} stared at me at the store. I bought the wrong thing to get out of there." } },
            { "", new[] { "Wonder what {name} does all day. Never once seen {name} in a hurry.", "{name} laughs at everything. Wish I had the knack.",
                         "{name} looks like {name}'s mother. Same chin. Same scowl.", "{name} has a new way of walking. Somebody's in love, or in debt.",
                         "{name} said good morning yesterday. Didn't today. What changed?",
                "{name} is talking to the horse again. Sounds like a good conversation.", "{name} has had a haircut. Looks younger. Looks surprised about it.", "{name} hasn't been to church in a month. Folks notice.", "{name} bought a whole sack of sugar. Somebody's baking, or courting.", "{name} sat on the porch all morning and didn't move. Thinking, or not thinking." } }
        };

        // a mood that carries: what happened to them today spills over into everything else (mood-congruent thinking)
        private static readonly string[] Sore =
        {
            "Everything's grating today. Even the church bell sounds sour.", "Snapped at the dog. Dog didn't deserve it. Nobody does, today.",
            "Feel like a fist looking for a wall.", "Whole day's gone crooked. Like a picture nobody straightens.",
            "Everybody's in my way today. Every single body.", "Spilled the coffee. Of course I did. Today.", "Somebody said good morning and I wanted to argue with it.", "Kicked the stove. Hurt my foot. The stove won.", 
            "Can't shake the taste of this morning. Sour as a green apple.", "Every little noise lands like a stone.", "If one more thing goes wrong I'll sit down in the road and stay there.", "Short with everybody. Long with nobody."
        };
        private static readonly string[] Glad =
        {
            "Good day. Can't say why. Won't look at it too hard in case it runs off.", "Caught myself whistling. Haven't whistled since spring.",
            "Even the flies seem friendly today.", "Folks are kinder than I give them credit for. Some folks.",
            "Sun on my face and nobody wanting anything from me. That'll do.", "Laughed out loud at nothing. Folks looked. Let them.", "Paid a man a compliment and meant it. Felt good on the tongue.", "Feels like the day's on my side for once.", 
            "Sang a hymn under my breath. Don't even know all the words.", "Gave a child a penny for no reason. Best penny I ever spent.", "Light step all day. Don't know where it came from. Not asking.", "The world's not so bad from where I'm standing."
        };

        /// How today has left them: "sore" (roughed up, robbed, sent off empty-handed, made a fool of in drink, in the
        /// last six hours), "glad" (a coin given or taken gratefully, a kindness, a sweetheart, a quarrel mended), else
        /// null; with nothing of the kind, a foul day out of doors makes them sore one time in three.
        public static string MoodOf(Person p, Context day, string seed)
        {
            if (day == null)
                return null;
            SceneTie t = day.Ties != null && day.Now >= 0 ? day.Ties.FirstOrDefault(x => day.Now - x.At >= 0 && day.Now - x.At < 360) : null;
            if (t != null)
            {
                bool did = t.Did(p.Id);
                if ((t.Kind == "rough" || t.Kind == "pickpocket") && !did) return "sore";
                if (t.Kind == "scorn" && did) return "sore";      // asked, and was sent off
                if (t.Kind == "drunk" && did) return "sore";      // the morning after, and everybody grinning
                if (t.Kind == "charity" || t.Kind == "comfort" || t.Kind == "cheer" || t.Kind == "settled" || t.Kind == "makeup" || t.Kind == "sweet" || t.Kind == "propose" || t.Kind == "flirt") return "glad";
            }
            if (!day.Indoors && (day.Weather == "rain" || day.Weather == "storm" || day.Weather == "snow" || day.Weather == "dust") && Voice.Chance(seed + "wmood", 33))
                return "sore";
            return null;
        }

        // small joys and old memories: the moments a life is mostly made of, between the worries
        private static readonly string[] Pleasure =
        {
            "Smell of bread from somewhere. Whoever's baking, God keep them.", "First coffee of the day. Burnt my tongue. Worth it.",
            "Got a tune stuck in my head and I don't even know the words.", "Found a dime in the dirt. Small luck is still luck.",
            "Pipe smoke, and for a moment I'm nine years old in my father's barn.", "My mother sang while she scrubbed. Haven't thought of that in years.",
            "A dog followed me half the length of the street. Nobody's dog. Everybody's friend.", "Clean shirt today. Feel like a different man. A slightly better one."
        };

        /// A thought about what is happening around them now, or null: something they just saw (by far the loudest),
        /// someone else in sight, the weather if they're out in it, the place they're in.
        public static string NowThought(Person p, string seed, List<string> told, Context day)
        {
            string what;
            return NowThought(p, seed, told, day, out what);
        }

        /// The same, with what it was about: seen, nearby, mood, weather or place (null with no thought).
        public static string NowThought(Person p, string seed, List<string> told, Context day, out string what)
        {
            what = null;
            List<KeyValuePair<string, Func<string>>> parts = NowParts(p, seed, told, day);
            if (parts.Count == 0)
                return null;
            // something seen just now comes first two times in three; otherwise any of what's there
            int i = parts[0].Key == "seen" && Voice.Chance(seed + "loud", 66) ? 0 : Voice.Hash(seed + "now") % parts.Count;
            what = parts[i].Key;
            return parts[i].Value();
        }

        /// Each thing around them now that could be thought about, by name (seen, nearby, mood, weather, place), with
        /// the line made only when asked for. Minds (Minds.cs) weighs them one by one.
        public static List<KeyValuePair<string, Func<string>>> NowParts(Person p, string seed, List<string> told, Context day)
        {
            var parts = new List<KeyValuePair<string, Func<string>>>();
            if (day == null)
                return parts;
            var options = new List<Func<string>>();
            var names = new List<string>();
            Witnessed s = day.JustSaw;
            bool saw = s != null && s.Kind != null && BySeen.ContainsKey(s.Kind) && s.AId != p.Id && s.BId != p.Id && s.AName != null && s.BName != null;
            if (saw)
            {
                options.Add(() => Pick(BySeen[s.Kind], p, seed, told).Replace("{a}", Person.FirstOf(s.AName)).Replace("{b}", Person.FirstOf(s.BName)));
                names.Add("seen");
            }
            List<Nearby> others = day.Nearby != null ? day.Nearby.Where(n => n.Id != p.Id && !string.IsNullOrEmpty(n.Name)).ToList() : null;
            if (others != null && others.Count > 0)
            {
                Nearby n = others[Voice.Hash(seed + "nb") % others.Count];
                options.Add(() => Ties.Pronouns(Pick(Regard(p, n), p, seed + "nb2", told), n.Woman).Replace("{name}", n.Name));   // {he}/{him}/{his} of the one in sight (the review)
                names.Add("nearby");
            }
            string mood = MoodOf(p, day, seed);
            if (mood != null)
            {
                options.Add(() => Pick(mood == "sore" ? Sore : Glad, p, seed + "mood", told));
                names.Add("mood");
            }
            if (!day.Indoors && (day.Weather != null || day.Sky != null && day.Sky.Turned != null))   // rain clearing to plain cloud has no kind, and it's still news
            {
                bool turned;
                string[] sky = WeatherTruth.PoolFor(day.Sky, seed, out turned);   // it's just turned, a cold rain, a clear night...: the weather as it really is (Core/WeatherTruth.cs)
                if (sky == null && day.Weather != null && ByWeather.ContainsKey(day.Weather))
                    sky = ByWeather[day.Weather];
                if (sky != null)
                {
                    options.Add(() => Pick(sky, p, seed, told));
                    names.Add(turned ? "weather-turn" : "weather");
                }
            }
            if (day.Indoors && day.Place != null && ByPlace.ContainsKey(day.Place))
            {
                options.Add(() => Pick(ByPlace[day.Place], p, seed, told));
                names.Add("place");
            }
            if (day.Routine != null)
            {
                string[] routine = Routines.Pool(day.Routine, day.RoutineOdd);   // a regular's routine here, now (SIXDAY 5a): the same stool at seven, or a face out of its hour
                if (routine != null)
                {
                    options.Add(() => Pick(routine, p, seed + "rt", told));
                    names.Add(day.RoutineOdd ? "routine-odd" : "routine");
                }
            }
            if (day.TownMood != null)
            {
                string[] townMood = TownLedgers.Pool(day.TownMood);   // the town's month (Core/TownLedgers.cs; SIXDAY 11c): mourning, barred doors, or prices and new faces
                if (townMood != null)
                {
                    options.Add(() => Pick(townMood, p, seed + "tm", told));
                    names.Add("townmood");
                }
            }
            for (int i = 0; i < options.Count; i++)
                parts.Add(new KeyValuePair<string, Func<string>>(names[i], options[i]));
            return parts;
        }

        /// How this person sees that one: envy of money, pity for a drunk, fear of a hothead (for the timid), else
        /// through their own temperament.
        private static string[] Regard(Person p, Nearby n)
        {
            string t = p.Personality ?? "";
            if (n.Known == "bully")
                return t == "Hothead" || t == "Proud" ? KnownBullyHot : KnownBully;
            if (n.Known == "thief")
                return KnownThief;
            if (n.Drunk && t != "Cold" && t != "Greedy")
                return Pity;
            if (n.Temper == "Hothead" && (t == "Coward" || t == "Loyal"))
                return Wary;
            if (n.WellOff && (t == "Greedy" || t == "Drifter" || TradeGroup(p.Trade) == "working" || TradeGroup(p.Trade) == "farm"))
                return Envy;
            string[] pool;
            return Watching.TryGetValue(t, out pool) ? pool : Watching[""];
        }

        /// Every line of the "now" band and the small pleasures, for the tests.
        public static IEnumerable<string> AllNowLines()
        {
            return ByWeather.Values.SelectMany(x => x).Concat(ByPlace.Values.SelectMany(x => x)).Concat(BySeen.Values.SelectMany(x => x))
                .Concat(Envy).Concat(Pity).Concat(Wary).Concat(Watching.Values.SelectMany(x => x)).Concat(Pleasure)
                .Concat(KnownBully).Concat(KnownBullyHot).Concat(KnownThief).Concat(Sore).Concat(Glad);
        }
    }
}
