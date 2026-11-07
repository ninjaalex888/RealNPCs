// More lines for each stage of every worry (the no-model run, item 6b; Lex, 2026-11-01): three a stage became six, so
// the same worry seen in a dozen faces in an afternoon doesn't read as one line passed round. Joined onto the worries'
// own lines the first time a worry is thought (Grow), in their order: begin, worse, crisis, good, bad. The slots are each
// worry's own ({who} the creditor, the child, the neighbour; {kin}; {place}; {town}). All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        private static readonly Dictionary<string, string[][]> MoreWorry = new Dictionary<string, string[][]>
        {
            { "debt", new[] {
                new[] { "Eleven dollars. Say it fast and it sounds like nothing. It's a month.", "{who} never mentions it. He doesn't have to. I mention it to myself.", "Bought flour on credit with {who}'s money still owing. A debt on a debt." },
                new[] { "{who} sent his boy round with a jar of honey. A kindness. A reminder.", "Dreamed I paid {who} in buttons and he counted every one.", "Wife asked why I won't go to the store. Told her the prices. Didn't tell her {who}." },
                new[] { "Saturday. {who} said Saturday. It's Thursday and the jar's no fuller.", "Thought of asking {who} for more time. Thought of his face when I do.", "Sold my good knife this morning. One dollar. Ten to go." },
                new[] { "Paid {who} and he poured me a drink on it. Best whiskey I never paid for.", "Owe nobody now. Walked down the middle of the street for the pleasure of it.", "{who} shook my hand like a man, not a lender. Took the weight off both of us." },
                new[] { "Lost the mule to {who}. The children named that mule. They haven't asked yet.", "{who} has what was mine and I have the debt off my back. Doesn't feel like a trade.", "Say good morning to {who} still. Can't afford not to. Can't afford much else." } } },
            { "fever", new[] {
                new[] { "{who} wants to be carried everywhere today. Hasn't done that since small.", "Kept {who} home from the schoolhouse. Watched the window all morning instead of working.", "{who}'s eyes are too bright. Bright eyes in a child aren't always a good thing." },
                new[] { "Boiled willow bark for {who}. Tastes bitter. Tasted it first so I'd know.", "{who} talks in sleep now. Names I don't know. Fever names.", "Asked the preacher's wife what she did for hers. She told me. Then she looked away." },
                new[] { "Sat by {who} and made promises. To God. To the doctor. To {who}.", "Can't feel the cold cloth getting warm any more. {who} is burning it through.", "Every breath {who} takes, I take one with it. Can't help it." },
                new[] { "{who} wanted a story. Told the one about the bear. Badly. Laughter, anyway.", "{who}'s fever's gone and mine's come. The kind you get from not sleeping.", "Sat on the porch with {who} in a blanket. Sun on us both. Didn't say a word for an hour." },
                new[] { "Can't go in {who}'s room. Neighbour's wife went in and did what wanted doing.", "Neighbours brought food enough for a wedding. Couldn't eat any of it.", "Kept {who}'s little shoes. Don't know what for. Couldn't burn them." } } },
            { "letter", new[] {
                new[] { "{kin} always writes by the first of the month. It's the ninth.", "Asked at the counter in a voice like I didn't much care. The clerk knows better.", "Wrote my name big on the last letter so they'd know whose it was coming back to." },
                new[] { "Read the old letters from {who} again. Same words, but they don't comfort the same.", "Stage was late. Late stages carry bad news. People say that. Stupid people.", "Bought another stamp. Haven't written. Holding it like a promise." },
                new[] { "Thought I saw {who}'s hand on an envelope in the rack. Wasn't. Had to sit down.", "Asked the preacher to pray for {who}. He asked what for. Couldn't say.", "Every rider coming in from the east, I look for {who} on the horse." },
                new[] { "Letter from {who}, and a pressed flower in it. Showed it to the cat.", "{who} wrote that they're well and asked after me. Asked after me. Imagine.", "Carried {who}'s letter in my shirt all day. Took it out twice to make sure." },
                new[] { "Put {who}'s last letter in the Bible. It'll keep there.", "Wrote back to the stranger who wrote. Thanked them. Couldn't think what else.", "Clerk was gentle with me today. Means he read the envelope before I did." } } },
            { "drought", new[] {
                new[] { "Dust on the windowsill every morning. Wiped it. It came back by noon.", "Watched clouds pile up over the hills and go round us. Always round us.", "Leaves on the cottonwood hanging limp. Even the trees have given up asking." },
                new[] { "Rationing water to the hens now. They eye me like I owe them.", "Sold the second cow before she thinned. Got half what she'd fetch in a wet year.", "Prayed for rain at church. The whole congregation did. Preacher's well is dry too." },
                new[] { "If it doesn't rain by Sunday, there's nothing to bring in. Nothing at all.", "Tasted the bottom of the well bucket. Mud and iron. All that's left.", "Looked at the sky so long my neck hurts. It looked back. Nothing in it." },
                new[] { "Rain on the roof all night. Lay awake on purpose to hear it.", "Corn's standing up again. Took one rain. One good rain. Lord.", "The children ran in the rain in their clothes and I didn't stop them." },
                new[] { "Fields are dust. Hitched the team and drove to {town} to ask about work.", "The land's still mine. Nothing grows on it. Mine and good for nothing, like me.", "Burned the dead stalks. Smoke went straight up. Not even wind." } } },
            { "son", new[] {
                new[] { "Found {who}'s pocket knife in the barn. He'll want it. Keeping it for him.", "His mother sets the table for one less and won't look at me while she does it.", "{who} is grown. Grown men go. I said so myself. Didn't mean it." },
                new[] { "Read in the paper about an accident at {place}. No names. Read it three times for names.", "{who} was always the one who'd write. Not writing is a kind of letter.", "Can't sleep for listening to the road. Every horse could be him." },
                new[] { "Went to the telegraph office. Stood there. Couldn't think what to send.", "His mother asked if we should go to {place}. With what money? With what horse?", "Dreamed {who} called from the yard. Went out in my nightshirt. Just the wind." },
                new[] { "{who} brought his mother a shawl from {place}. She hasn't taken it off since.", "Sat up with {who} till the oil gave out, hearing about {place}. Didn't want him to stop.", "{who} slept twelve hours in his own bed. I checked on him twice like he was small." },
                new[] { "{who} sits on the porch and watches the road. Doesn't say what for.", "His mother keeps the letters from {place} in a ribbon. The last one's still sealed.", "Neighbours ask after {who}. I tell them he's home. That's true, as far as it goes." } } },
            { "courting", new[] {
                new[] { "Combed my hair twice before going to the store. For flour.", "{who} has a way of saying good morning that lasts all day.", "Wondered if {who} knows my name. Then wondered why I'd want them to." },
                new[] { "Saw {who} at the window. Waved. It was a curtain. Waved at a curtain.", "My sister says {who} asked about me. My sister lies. Asked her to say it again anyway.", "Bought a ribbon I'll never give. Keep it in my coat." },
                new[] { "Sunday. Tomorrow's Sunday. Practised what to say until the words stopped meaning anything.", "Asked my father how he asked my mother. He laughed till he coughed. No help at all.", "If {who} says no I'll leave for {town}. I won't. But I'll think about it." },
                new[] { "Danced with {who}. Trod on a foot. {who} laughed and kept hold.", "{who} saved me a seat at church. A seat. The whole sermon was about something.", "Walked {who} home and the walk was too short. Next time I'll find a longer road." },
                new[] { "Saw {who} with the other one on the boardwalk. Crossed the street. Walked into a post.", "Gave the ribbon to my sister. She knew what it was for. She didn't say.", "Work, then. A man can work. Work doesn't look past you at a dance." } } },
            { "feud", new[] {
                new[] { "{who}'s cattle drank at my trough. My trough, my water, his cattle.", "Wife says let it go. Wife doesn't have to look at that fence every morning.", "{who} waved at me. Wave of a man who thinks he's won something." },
                new[] { "{who}'s boy threw a rock at my dog. Boys are boys. Fathers teach them.", "Told the preacher my side of it. He listened like a judge. A tired judge.", "Locked the gate tonight. Never locked it in eleven years." },
                new[] { "Saw {who} at the store with a new rifle. Bought a box of shells myself, after.", "Both of us at the same feed store counter. Clerk served us at once to have us gone.", "My wife won't sit near {who}'s wife at church now. It's gone past us." },
                new[] { "{who} helped me pull a wheel out of the mud. Neither of us said a thing. Didn't need to.", "Sent {who} a ham at Christmas. He sent back a jar of preserves. That's a treaty.", "Had a drink with {who} at the saloon. Talked about the weather. First good talk in a year." },
                new[] { "Moved my hives away from {who}'s line. Let him find his own honey.", "Our children used to play together. They don't now. That's what we've done.", "{who} won. Or I did. Nobody's happy, so call it a draw." } } },
            { "bank", new[] {
                new[] { "Wife asked about the note. Told her not to fret. Then fretted all night.", "Every letter's from the bank in my head, before I open it.", "Banker lifted his hat in church when he saw me. I'd rather he hadn't seen me." },
                new[] { "Sold the good saddle to pay the interest. Riding on a blanket like a boy.", "Did the sums in the almanac's margin. They come out the same every time.", "Banker's clerk came round to look at the barn. Looked at it like he owned it already." },
                new[] { "Went to ask the bank for time. Wore my best collar. Collar didn't help.", "Wife's packing the good dishes in straw. Hasn't said why. Doesn't have to.", "If the bank takes it, my father's grave is on their land. Thought of that at three in the morning." },
                new[] { "Note's paid. Framed the receipt. Wife says it's vulgar. Left it on the wall.", "Banker said I'm a good risk. Never been called a good anything by a banker.", "Walked the land at sunset. Every fence post mine. Every one." },
                new[] { "Rented a room over the store. The bed's narrow. So's everything now.", "Banker sold the place to a man from back east. He's painting it blue.", "Drove past the old house. Somebody's washing on my line." } } },
            { "drink", new[] {
                new[] { "Coffee instead. Coffee all day. Heart's going like a hammer.", "Barkeep said I look well. Said it like a man who'd lost a customer.", "Promised the children. That's the promise that holds. So far.",
                "Hid my last dollar in my boot so it can't find the bar on its own.",
                "Ate three suppers' worth of bread. Belly wants something. Bread will do.",
                "Woke at the hour the saloon opens. Lay there and let it open without me.",
                "Put a stone in my pocket. Every time I want one, I squeeze the stone.",
                "Sat on the porch with lemonade like a deacon. Felt a fool. Stayed dry.",
                "Gave my whiskey glass to the cat for her water. She likes it fine.",
                "Told the store clerk not to sell me any. Clerk laughed. Then saw I meant it.",
                "Sky looks bigger at night without a glass in my hand. Never looked up before.", },
                new[] { "Shaking stopped this morning. Then started again at noon, like it forgot.", "Smelled it on a man's breath at the store and had to step outside.", "Took the long road home to miss the saloon. Long road's longer every day.",
                "Hands won't stay still. Sat on them through the whole sermon.",
                "Every bottle in the store window has my name on it, seems like.",
                "Sweats at night, chills at noon. Body doesn't know what season it is.",
                "Can't stand the smell of my own coat. Years of it in the wool.",
                "Heard a cork pop at the next table in the hotel. Jumped like at a gunshot.",
                "Sleep comes in pieces now. An hour here, an hour there.",
                "Snapped a pencil in half doing sums. Didn't know I'd been gripping it.",
                "Dog follows me to the saloon door out of habit. We both turn back.", },
                new[] { "Somebody left a bottle on the bench outside the livery. Half full. It's still there. So am I.", "Bad day. Bad days are what it's for. Didn't. Wanted to.", "Wrote down every reason not to. Read it twice. Folded it small.",
                "Ten cents on the counter. Barkeep's hand on the bottle. My hand on the dime.",
                "Pay envelope's in my coat, heavy as a brick. Saloon's on the way home.",
                "Wedding party spilled into the street with jugs. Ducked into the livery.",
                "Old friend says one won't hurt. He's been saying it nine years.",
                "Thirsty in a way water can't reach. Drank water anyhow, a full bucket.",
                "Smelled the empty barrel behind the saloon. Just the empty barrel. Low.",
                "Bought a pint and walked it home. It's on the table. I'm by the door.",
                "Prayed for the craving to go. It stayed for the whole prayer and after.", },
                new[] { "Children ran to meet me at the door today. Not away from it. To it.", "Hands are steady. Held a cup of coffee and didn't spill a drop. Small thing. Big thing.", "Paid off my slate at the saloon and walked out without a glass. Barkeep shook my hand.",
                "Bought the children shoes. Store-bought, with the drink money.",
                "Caught myself laughing at nothing on the porch. Clear-headed laughing.",
                "Sent money home this month. First time. Brother wrote back two pages.",
                "Pledge card's framed by the door now. Clean. Read it on the way out.",
                "Neighbor asked me to stand surety on a loan. Trusts my word now. My word.",
                "Played checkers at the store all evening. Won twice. Remember both.",
                "Slept from dark to dawn without a dream in it. Woke hungry.",
                "Barkeep's dog still wags at me. Barkeep doesn't. Fair trade.", },
                new[] { "Wife packed my things. Didn't shout. I'd rather she'd shouted.", "Drank the money for the doctor. The children were well, so it didn't matter. It mattered.", "Back on the stool by the window. Same stool. Like I never left it.",
                "Woke in the alley behind the bar with a dog licking my face.",
                "Used the pledge card as a spill to light a cigar. Watched myself do it.",
                "Pawned my coat in May. Swore I'd fetch it back before fall. Fall's here.",
                "Nobody bothers to ask how I'm keeping now. They can see.",
                "Owe the barkeep, the grocer, the barber. Owe myself most of all.",
                "Sixty days dry, gone in a night. Like a dam with one crack in it.",
                "Bottle under the floorboard now. Hiding it from a house that already knows.",
                "Children set my place at supper and then clear it, untouched. They've learned.", } } },
            { "cards", new[] {
                new[] { "Mrs. {who} smiled when I paid last month. Hasn't had cause since.", "Know the odds. Know them better than the man who beat me. Didn't help.", "Lucky coat's at the tailor's. That's what went wrong. Has to be." },
                new[] { "Borrowed two dollars from the barber for a stake. Lost it in four hands.", "Mrs. {who} has started saying good morning like it's a question.", "Swore off cards for a day. Dreamed about them all night." },
                new[] { "Going back tonight. Once more. Then never. Said that last night.", "Mrs. {who}'s nephew carried my trunk to the door. Set it down gentle. A warning.", "Laid out the rent on the bed and looked at it till the light went." },
                new[] { "Won back the rent and four dollars more. Quit the table while I could still stand up from it.", "Mrs. {who} gave me a cup of tea with the receipt. Tea. I nearly wept.", "Paid what I owe and the barber too. Lighter in the pocket and the chest." },
                new[] { "Sleeping in the hayloft. The horses don't mind my losing.", "Mrs. {who}'s new boarder has my old room and my old chair by the window.", "Sold my watch to a drummer. Same drummer that took my room." } } },
            { "savings", new[] {
                new[] { "Wrote {place} on a scrap and put it in the can. A reminder what the money's for.", "Folk in {place} have work and a future. Read it in a paper. Papers say a lot of things.", "Mended my boots myself instead of paying. Another dime toward {place}." },
                new[] { "Moved the can to under a floorboard. Lie awake listening for the board to creak.", "Ticket man said the fare's going up again in the fall. Fall's coming.", "Gave a dime to a beggar and hated myself. Then hated myself for hating it." },
                new[] { "Counted it to the cent. Enough if nothing goes wrong. Something always goes wrong.", "Boss offered extra work on Sunday. Took it. God will understand. {place} won't wait.", "Dreamed I got on the train and it went nowhere. Just sat there, steaming." },
                new[] { "Packed what I own in one bag. It closes easy. Not much to show. Enough to start.", "Told the boss I'm leaving for {place}. He said good luck like he meant it.", "Ticket's in my boot. Walking careful so I don't wear it out." },
                new[] { "Somebody's got my money and {place} has got along without me.", "Started over. New can. A penny in it. Laughed at myself for that.", "Watched the train leave for {place} from the platform. Waved at nobody." } } },
            { "tooth", new[] {
                new[] { "Can't take coffee hot now. Let it go cold and hated every sip.", "Tongue keeps finding the tooth. Can't leave it alone. Like a bad thought.", "Laughed at a joke and the tooth reminded me not to.",
                "Ache's back, bottom right. Comes and goes like a debt collector.",
                "Chewing slow, chewing careful, chewing on the left. Supper takes an hour.",
                "Salt water rinse morning and night. Tooth just takes it.",
                "Sweet tea's a sin now. The tooth preaches at every sip.",
                "Throbs worse lying down. Sleeping propped on two pillows and a coat.",
                "Rubbed whiskey on the gum. Gum went numb. Tooth didn't notice.",
                "Every bite of bread's a gamble on which side.",
                "Bit my tongue trying to spare the tooth. Now both hurt.", },
                new[] { "Half my face is swollen. Children stare. Can't blame them.", "Held a cold stone to my jaw all afternoon. Stone got warm. Jaw didn't.", "Can't eat anything but soup. Soup and spite.",
                "Jaw's too stiff to yawn. Never missed a yawn before.",
                "Cheek's hot to the touch. Folks ask have I been out in the sun.",
                "Pain wakes me before the rooster does. Rooster seems lazy now.",
                "Porridge three meals running. Hate porridge. Hate the tooth more.",
                "Ear on that side rings all day. Tooth's moved in next door.",
                "Talking out of one side of my mouth like a confidence man.",
                "Tried a hot onion poultice. Smell of onion, ache of tooth. Both stayed.",
                "Couldn't finish my work. Sat in the shade holding my face.", },
                new[] { "Barber's shop is open till six. It's five. I'm standing across the street.", "Asked the blacksmith to do it. He laughed. Then he looked at my jaw and stopped laughing.", "Took a drink for courage and another for the pain. Neither came.",
                "Walked to the barber's door three times today. Touched the handle once.",
                "Fever's up. Sweating in the chair by the stove, and shivering too.",
                "Swore I'd go first thing. First thing's come and gone twice.",
                "Begged the druggist for something stronger. He gave it. Haven't dared it yet.",
                "Whiskey in one hand, a dollar in the other. Barber's door ahead.",
                "Tied a string round it and to the doorknob. Lost my nerve before the slam.",
                "Can't think of anything but the tooth. Tried counting fence posts. Counted throbs.",
                "Barber's boy says it's quick. He's twelve. Twelve-year-olds say anything.", },
                new[] { "Ate an apple. A whole apple. Sweetest thing in a month.", "Slept on my left side. First time in two weeks.", "Barber gave me the tooth in a twist of paper. Keeping it. A trophy.",
                "Chewed jerky like a dog with a bone. Jaw's tired. Happy tired.",
                "Swelling went down overnight. Face came back like a lost hat.",
                "Hot coffee, black, near boiling. Drank it to spite the memory.",
                "Paid the barber, thanked him, and shook his hand twice.",
                "Woke and didn't touch my jaw first thing. Touched it second, to check.",
                "Sang at the supper table. Got told to hush. Didn't care.",
                "Back to a full plate. Cleaned it and asked for more.",
                "Tongue keeps visiting the gap. Nothing there to fear now.", },
                new[] { "Jaw aches when it rains. I'm a weather vane now.", "Smile crooked now, for the gap. People think I'm sly. Let them.", "Paid a dollar for that. A dollar and a week of my life.",
                "Gap where it was catches every seed. Spend supper picking.",
                "Bills from the barber and the druggist both. Tooth's still costing.",
                "Jaw went stiff and stayed so. Chew like an old horse.",
                "Whistle's gone with the tooth. Air goes out the gap instead.",
                "Laudanum on the shelf where the tea used to be.",
                "Swelling left a lump under the jaw. Doctor says leave it. Easy for him.",
                "Bad pull. Broke at the root, and the barber left the root.",
                "Can't bite an apple now. Cut it in slices like a lady at tea.", } } },
            { "secret", new[] {
                new[] { "The old name nearly came out of my mouth at the store today.", "Every new face in town, I look twice. Then look away quick.", "Wrote the old name in the dirt with a stick and rubbed it out.",
                "Keep the old name folded small in my head, like a letter I can't post.",
                "Answered a stranger's call today and caught myself just in time.",
                "Flinched at a postmark. Only a catalog from Chicago.",
                "Watch my own hands when I talk. Old habits from the old place.",
                "Bought a new hat with a wider brim. Shade's useful. So's the rest of it.",
                "Careful with stories now. Never tell the same one to two people.",
                "Lit the lamp late tonight. Thinking about the river back home.",
                "Learned the hymns they sing here. Different from ours. Ours. Theirs.", },
                new[] { "Somebody asked where I'm from. Gave the answer I always give. Felt it wobble.", "Dreamed my mother called me by the old name across the whole town.", "Burned the last thing I had with the old name on it. Felt lighter. Felt sick.",
                "Stranger at the depot asked where my train came from. Named the wrong town.",
                "Heard someone say a name like my sister's. Spilled my coffee.",
                "Old turn of speech slipped out at the store. Clerk cocked an ear.",
                "Drummer's selling goods from my old county. His accent made my skin creep.",
                "Can't keep my stories straight in my sleep. Woke saying them.",
                "Tore the family page out of the old Bible. Can't burn it. Can't keep it.",
                "Saw a face in the crowd I'd swear I knew. Gone before I could be sure.",
                "Every knock at the door, my hand goes to the valise.", },
                new[] { "A man in the saloon kept looking at me. When I looked back he smiled like he knew.", "Got my story straight in my head. Got it straight three ways. Which one did I tell before?", "If it comes out, I'll leave. I've left before. Got good at it.",
                "Somebody's been asking the postmaster about me. Postmaster told me, with a wink.",
                "Man in a city coat has taken the room above mine. He walks soft.",
                "Heard my old name said in the saloon, plain. Didn't look up. Didn't breathe.",
                "Packed the valise with the window open. Train's at midnight.",
                "Rehearsed what I'll say if asked. Sounds like a lie even to me.",
                "Friend here asked me straight am I hiding something. Changed the subject to hay.",
                "Can't eat. Can't sit. Walking circles in a room seven feet across.",
                "Letter came with no words inside, just my old town's name. Nothing more.", },
                new[] { "Somebody called me by the new name across the street, and I turned. Mine now.", "Preacher asked me to read the lesson Sunday. Me. They trust me here.", "Nobody's asking any more. The town's decided who I am. I'll take it.",
                "The man in the city coat left on the noon stage. Never once looked at me.",
                "Baked pies for the church supper this year. Ate my own and everybody's praise.",
                "Signed a deed in the new name. The law says it's mine. So it is.",
                "Somebody asked how long I've lived here. Said always. They nodded.",
                "Unpacked the valise for good. Put my shirts in the drawer like I'm staying.",
                "Laughed out loud in the street today. Didn't care who looked.",
                "Haven't thought of the old name in a week. Thought of it now. Let it go.",
                "Neighbors asked me to sit up with their sick mother. Trusted me with the night.", },
                new[] { "Half the town knows and the other half's guessing. Walk with my eyes down.", "Packed a bag and put it under the bed. Haven't decided. The bag has.", "People are kind about it where I can hear. That's the worst of it.",
                "Hear the old name whispered in the store every time a door closes.",
                "Rent's doubled, sudden. Landlord won't say why. He doesn't need to.",
                "Friends say good day now and keep walking. Short days.",
                "Thinking of the old place again. If they know here, maybe home is safer.",
                "Valise is packed. Has been for a week. Just waiting on the nerve.",
                "Got a cold nod from the preacher. Even he's picked a side.",
                "Wrote a letter explaining all of it. Nobody to send it to.",
                "Fellow I trusted told the town. Didn't even get a dollar for it.", } } },
            { "law", new[] {
                new[] { "Strange horse at the rail outside the sheriff's. Good horse. County horse.", "Shave every morning now. A clean face is a different face.", "Paid in coin at the store. Paper has a way of being remembered." },
                new[] { "Sheriff bought me a drink. Friendly as a dog. Dogs bite.", "Saw a man writing in a little book outside the saloon. Writing what?", "Stopped going to the same place two days running. A pattern's a trail." },
                new[] { "Somebody's been in my room. Nothing gone. Things moved an inch.", "Deputy asked my name and I gave it and he wrote it down slow, letter by letter.", "Slept in the hills last night. Cold. Better than the cell." },
                new[] { "Sheriff's got bigger trouble down the valley. Thank God for other men's sins.", "The man with the little book got on the stage. Waved to him. He didn't wave back.", "Bought a new shirt and walked down the street in it like a citizen." },
                new[] { "They've got my name and my face and the date I was born. All that's left is me.", "Rode out at night with what I could carry. Again.", "Sheriff says if I'm still here by Friday. Didn't finish. Didn't need to." } } },
        };

        private static readonly object WorryGrowLock = new object();
        private static bool _worriesGrown;

        /// Joins MoreWorry onto each worry's own lines, once (the first worry thought, or the tests' walk).
        private static void GrowWorries()
        {
            if (_worriesGrown)
                return;
            lock (WorryGrowLock)
            {
                if (_worriesGrown)
                    return;
                foreach (WorryKind k in Worries)
                {
                    string[][] more;
                    if (!MoreWorry.TryGetValue(k.Key, out more) || more.Length < 5)
                        continue;
                    k.Begin = k.Begin.Concat(more[0]).ToArray();
                    k.Worse = k.Worse.Concat(more[1]).ToArray();
                    k.Crisis = k.Crisis.Concat(more[2]).ToArray();
                    k.Good = k.Good.Concat(more[3]).ToArray();
                    k.Bad = k.Bad.Concat(more[4]).ToArray();
                    string[][] deep;
                    if (DeepWorry.TryGetValue(k.Key, out deep) && deep.Length >= 5)
                    {
                        // ten more a stage (Run 3, item 6: "thousands more thoughts"; InnerLife.WorriesDeep.cs)
                        k.Begin = k.Begin.Concat(deep[0]).ToArray();
                        k.Worse = k.Worse.Concat(deep[1]).ToArray();
                        k.Crisis = k.Crisis.Concat(deep[2]).ToArray();
                        k.Good = k.Good.Concat(deep[3]).ToArray();
                        k.Bad = k.Bad.Concat(deep[4]).ToArray();
                    }
                }
                _worriesGrown = true;
            }
        }
    }
}
