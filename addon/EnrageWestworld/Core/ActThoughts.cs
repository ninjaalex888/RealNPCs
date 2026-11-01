// A thought for every act a person's own mind chooses and every turn of an encounter (the no-model run, item 1; roadmap
// 41; Lex, 2026-10-31: what they think and what they do agree). Until now an act begun from Choosing.Choose showed
// nothing (its "MIND would" went to the log only), and an encounter's turn settled by the person's own draw showed
// nothing either; only the AI model's pick came with a thought. Here every act of Choosing.All that does something
// ("stand" carries on) has its own written lines: what moves them to it (tired, thirsty, sore at someone, wanting company),
// never a telling of the act, by the need it serves where it serves more than one (Score's why: home for rest or for
// safety, the saloon for company or for the pull, the card table, a word, a stroll; "feeling" where a feeling's urge
// chose it), then by their temper's group (Minds.TemperGroup), everyone's lines after their own. {who} is the one it is
// about (a friend, a talker, a foe: a name they know) and {you} the player as they know him (for keeping their distance from him), only
// where the act has someone. Each encounter keeps its turns' lines beside its options' words (StreetCallouts, Domestic,
// Hogtied, Favours, Warnings: Turns). The caps, the frame guard and SaidToday are applied by the Engine and the game side
// (Engine.ActThoughts.cs, WestworldScript.ActThoughts.cs); [Mind] ActThoughts. Works without the model; with it on and
// answering, the model's own thought comes first. Plain C#, no game; all words original.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class ActThoughts
    {
        /// The acts of Choosing.All that do something, in its order ("stand" carries on; an urge with no act keeps its feeling's line).
        public static readonly string[] Acts = { "stroll", "eat", "rest", "home", "shelter", "work", "drink", "cards", "pray", "talk", "watch", "keepclear", "showoff", "saloon", "store", "church", "greet", "steerclear" };
        /// Minds.TemperGroup's four: plain the most, since most people are.
        public static readonly string[] Groups = { "plain", "bold", "fearful", "cold" };

        // the caps (game ms): a person's act thought at most once in 3 minutes, never within 45 s of another thought of theirs;
        // one act thought every 8 s near you and one encounter turn's every 3 s, and none while the ticker has more than two waiting
        public const int PersonGapMs = 180000, TheirsGapMs = 45000, ActGapMs = 8000, TurnGapMs = 3000, FreshMost = 2;
        public const int Never = int.MinValue;

        // the need keys, in NeedState's order (Choosing.Score's why is NeedState.Names[i])
        private static readonly string[] NeedKeys = { "hunger", "rest", "warmth", "money", "safety", "company", "standing", "meaning", "vice" };

        // "<act>|<need>|<group>": the need "" is the act's own (the need it mostly serves); a need of its own only where the act
        // serves more than one and the words differ (those cells are everyone's: plain alone)
        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            // a stroll: restless, the head wanting clearing (meaning), or people to be among (company)
            { "stroll||plain", new[] { "Legs want moving and the head wants clearing.", "Sat too long with the same worry. Walk it off.", "Horse dung and woodsmoke. Still better than four walls.",
                                       "An hour with nowhere to be. Spend it slow.", "Thoughts settle when the feet move. Up the street and back." } },
            { "stroll||bold", new[] { "Let the street get a good look at me.", "Walk like the place belongs to me. Half of it ought to.", "Never could abide sitting idle.", "Nobody's getting in my way today. Let them try." } },
            { "stroll||fearful", new[] { "Keep to where folks are. Safer with people about.", "Stretch my legs while there's light. Not after.", "Walk a little. Maybe the nerves walk off with it.", "Mind the alleys. Stay on the boardwalk." } },
            { "stroll||cold", new[] { "My own company beats anybody's chatter.", "Walk and think the sums through. Easier on the move.", "Watch the town at its business. Learn what it hides.", "No errand and no company. Suits me fine." } },
            { "stroll|company|plain", new[] { "Bound to be a face out there to pass the time of day with.", "Folks about. Maybe a face I know.", "Walk where the people are. Any voice but my own.",
                                              "A nod from a stranger beats the clock ticking.", "See who's out. Maybe hear something worth the hearing." } },
            // a meal
            { "eat||plain", new[] { "Belly's been at me since morning. Something hot.", "Can't think straight on an empty stomach. Feed it first.", "Smell of fried onions from somewhere. That settles it.",
                                    "Bread, a bit of meat, a cup of coffee. A body can bear anything after.", "Stomach first. The rest can wait its turn." } },
            { "eat||bold", new[] { "Earned a plate today. Mean to have it.", "Hungry enough to fight a man for his supper. Cheaper to buy my own.", "Not waiting on my belly's account a minute longer.", "Biggest plate in the house. Let them watch me clear it." } },
            { "eat||fearful", new[] { "Eat while I can. Who knows when the next chance comes.", "Quick bite and gone. Don't linger where folks stare.", "Shaky from want of food. That's all it is. Food.", "Eat while there's coin for it. Might not be, next week." } },
            { "eat||cold", new[] { "Food's fuel. Take it on and get back to business.", "Paid for, so it gets eaten. Waste nothing.", "Cheapest plate that fills. No call for more.", "Eat alone. Nobody reaching across for my bread." } },
            // a rest; a feeling's (relief, a joy too big to stand under)
            { "rest||plain", new[] { "Feet are done in. Sit before they decide for me.", "Ten minutes off my feet. All I ask of the day.", "Back's ached since noon. A bench will do.",
                                     "Breath to catch. Sit a spell.", "Bones say enough. Listen to them for once." } },
            { "rest||bold", new[] { "Sit where I please. Nobody's moving me.", "Strong back still wants a rest. Nobody need know it aches.", "Done more today than the lot of them. Take my ease.", "Rest the legs. Never the eyes." } },
            { "rest||fearful", new[] { "Sit down before the shaking shows.", "A seat with a wall behind it. Then rest.", "Weak at the knees. Sit, and don't let on why.", "Rest where I can see the street coming." } },
            { "rest||cold", new[] { "Rest is cheaper than a doctor.", "Sit and say nothing. Let the world go by.", "Tired. Not that a soul is asking.", "Rest now, work later. Simple arithmetic." } },
            { "rest|feeling|plain", new[] { "Knees gone to water. Sit, before they fold.", "Heart still going like a drum. Sit till it slows.", "Sit a minute. Let it sink in.",
                                            "Too much for one day. A seat, and some quiet.", "Hands won't stop. Sit and wait them out." } },
            // home: supper and bed (rest), or a door to bar (safety)
            { "home||plain", new[] { "Day's done. Fire, a plate, and bed.", "Boots off, feet up. Thinking of nothing else.", "Lamp'll be lit at home by now. Get there.",
                                     "Long day. A body's own roof is the only cure for it.", "Supper's waiting, if it isn't burnt." } },
            { "home||bold", new[] { "Done with the lot of them for today.", "Go when I please. And I please now.", "Day's work done, and done right. My own chair now.", "Nobody to answer to at home. Best part of the day." } },
            { "home||fearful", new[] { "Home before the dark gets thick. Bar the door.", "Wrong sort fill the streets after sundown. Go now.", "Home. Lock it. Sit where I can see the window.", "The walk home gets longer every evening." } },
            { "home||cold", new[] { "Nothing more to be had out here today.", "Count the day's takings at my own table. In peace.", "Nobody waiting up. Quiet's what I want.", "Home's cheaper than the saloon. Quieter too." } },
            { "home|safety|plain", new[] { "Something's off in town today. Indoors.", "Too many strange faces about. Home, and stay put.", "Eyes on my back. Home, quick.",
                                           "Not safe out here tonight. Not for me.", "Behind my own door till morning." } },
            // a roof in the wet
            { "shelter||plain", new[] { "Soaked through. Any roof will do.", "Wet to the bone. Under cover, quick.", "Rain's found every hole in this coat.",
                                        "Water in my boots. Somewhere dry.", "Weather's turned mean. Duck in till it passes." } },
            { "shelter||bold", new[] { "Not standing out in this like a fool.", "Rain doesn't trouble me. It ruins a good hat, though.", "Let the weather do its worst. I'll watch it from a porch.", "Not catching my death on anybody's account." } },
            { "shelter||fearful", new[] { "Wet and cold brings fever. Get under cover.", "Sky's black as a coal hole. Don't like it.", "Chill on the chest. That's how it starts.", "Thunder. Inside, quick." } },
            { "shelter||cold", new[] { "A cold costs a week's wages. Out of the wet.", "No sense ruining good cloth. Under the eaves.", "Rain's no friend to the ledger. Wait it out.", "Shelter's free. Doctoring isn't." } },
            // the day's work
            { "work||plain", new[] { "Work won't do itself. Rent won't wait.", "Hands busy, head quiet. Back to it.", "Every idle hour is a coin I don't see.",
                                     "Get it done. Pay comes Saturday, not before.", "Foreman's eye goes round. Be working when it lands." } },
            { "work||bold", new[] { "Nobody in this town does it better. Let them see it.", "Work hard enough and they'll stop looking down on me.", "Show them how it's done. Again.", "Pride in the work. The one thing that's all mine." } },
            { "work||fearful", new[] { "Lose this place and there's nothing. Keep at it.", "Can't afford a black mark. Head down.", "Stop for a minute and they'll call me idle. They'd like that.", "Plenty waiting for my place. Plenty." } },
            { "work||cold", new[] { "Time is money. Mine, anyhow.", "Work for wages. Never expect thanks.", "Every hour is pennies in the jar.", "Do the work, take the pay, leave the talk to others." } },
            // a drink: the thirst (the vice), or a place among people (company)
            { "drink||plain", new[] { "Throat's dry as a creek bed in August. One glass.", "A glass to take the edge off. Just the edge.", "Long day. A drink's the least I'm owed.",
                                      "Smell of whiskey from the door. Can't walk past it.", "One to wet the throat. Then home. Then home." } },
            { "drink||bold", new[] { "Pour it and leave the bottle. I'll say when.", "Drink with the best of them and walk straight after.", "I'm entitled. Let anybody say different.", "Good whiskey, not the barrel slop." } },
            { "drink||fearful", new[] { "Nerves rattling like a loose wheel. One glass settles them.", "Drink till the worry goes quiet. Just quiet.", "Nobody looks at somebody drinking in a corner.", "A little courage out of a glass. Only a little." } },
            { "drink||cold", new[] { "Cheapest bottle on the shelf. Drunk is drunk.", "One drink, slow. Make it last the hour.", "Drink and listen. Drunk men talk about money.", "Not for the company. For the quiet it buys." } },
            { "drink|company|plain", new[] { "A glass is the price of a place among people.", "There's always one at the bar who'll talk.", "Raise a glass with whoever's there. Beats drinking alone.",
                                             "Buy one, lean on the rail, belong somewhere an hour.", "Laughing at the bar. I want some of that." } },
            // the card table: the itch (the vice), the company, or the money short
            { "cards||plain", new[] { "Itch in the palms again. Only the cards scratch it.", "A few hands to pass the evening.", "Felt luck in my bones this morning. Test it.",
                                      "Chips clicking on the felt. Sweetest sound there is.", "Lose a little, win a little. It's something to do." } },
            { "cards||bold", new[] { "Nobody at that table can read me. Not one of them.", "Take their money and they'll thank me for the lesson.", "Big pot or none. Small stakes are for timid men.", "Show them who has the nerve at this table." } },
            { "cards||fearful", new[] { "Shouldn't. Know I shouldn't. Sit down anyway.", "Win back what I lost before anybody finds out.", "Small stakes. Small. I promise.", "Last hand still stings. This one'll mend it." } },
            { "cards||cold", new[] { "Count the cards, read the faces. Fools pay.", "Sit only with the ones who drink while they play.", "A game's just arithmetic with whiskey poured on it.", "Patience wins at cards. Patience and a cold nerve." } },
            { "cards|company|plain", new[] { "Cards are only an excuse to sit with folks.", "A laugh and a hand. Company at the table.", "Good to jaw a while between deals.",
                                             "Better a game with friends than a night alone.", "Sit in. More for the company than the pot." } },
            { "cards|money|plain", new[] { "Rent's short. A lucky night would square it.", "Two dollars into ten. It's been done.", "Need the money too badly not to try.",
                                           "If luck owes me anything, tonight it pays up.", "Win enough to quiet the landlady. Then stop." } },
            // a prayer
            { "pray||plain", new[] { "Need a word with the Lord. Long overdue.", "Something's heavy on me. Lay it down.", "Ask forgiveness. Ask for strength. Both, if He's listening.",
                                     "Quiet with God a while. The world's too loud.", "Give thanks. Even a hard week has something in it." } },
            { "pray||bold", new[] { "The Lord and I have an understanding. Remind Him of it.", "Not begging. Asking. There's a difference.", "Thank God for what I've built. Nobody else will.", "Pray standing tall. He made me this way." } },
            { "pray||fearful", new[] { "Please, Lord. Keep trouble off me a while longer.", "Forgive me. Whatever it was. Forgive it.", "Pray it away. All of it.", "Lord, watch over the house tonight." } },
            { "pray||cold", new[] { "Mother's habit. Might as well keep it.", "Pray in case. No harm in hedging a bet.", "Prayer is free. Not much else is.", "Pray for the price of corn. Somebody ought to." } },
            // a word with someone: company, or a feeling that has to be told (share)
            { "talk||plain", new[] { "Need a voice besides the one in my head.", "Talk to somebody. About anything. Even the weather.", "Not a word spoken all day. Feel it in my chest.",
                                     "{who} looks like company worth having.", "Half the day gone without a decent word. Mend that." } },
            { "talk||bold", new[] { "Got opinions. Somebody's going to hear them.", "Plenty to say and nobody saying it.", "{who} could stand to hear what I think.", "Tell somebody how this town ought to be run." } },
            { "talk||fearful", new[] { "A friendly voice. That's all. Just a friendly voice.", "Maybe somebody's heard the news. Ask quiet.", "Feel less alone with somebody near.", "Ask {who} if it's only me who feels it." } },
            { "talk||cold", new[] { "Talk long enough and people say what they're after.", "Find out what {who} knows. Talk is cheap.", "Listen more than I speak. Always pays.", "Pass the time. Keep an ear out for a bargain." } },
            { "talk|feeling|plain", new[] { "Bursting with it. It has to come out.", "Can't keep this to myself. Not this.", "Say it out loud and it'll be real.",
                                            "{who} will want to know. Tell it.", "Tell it while it's fresh, before the nerve goes." } },
            // a wary eye (safety); a feeling's (something going on, watched hard)
            { "watch||plain", new[] { "Something about today. Keep the eyes open.", "Nobody minding the street but me, it seems.", "Watch first. Trust later.",
                                      "A feeling in the gut. Pay it mind.", "Eyes open, mouth shut. Learn what's going on." } },
            { "watch||bold", new[] { "Let anybody try anything. I'm watching.", "Nothing gets past me. Not today.", "Trouble shows its face to whoever looks for it.", "Stand my ground and watch. Whoever wants it can come." } },
            { "watch||fearful", new[] { "Something's wrong. Can't say what. Something.", "Too many faces I don't know. Watch them all.", "Don't move. Don't draw the eye. Just watch.", "Heard something. Didn't I? Watch that corner." } },
            { "watch||cold", new[] { "Watch who goes where. Knowing is worth money.", "Folks show what they are when they think nobody's looking.", "Mark who's armed. Mark who's drunk.", "Patience. Whatever's coming will show itself." } },
            { "watch|feeling|plain", new[] { "Something going on over there. See it through.", "Can't look away. Not yet.", "See what comes of it. Might be needed.",
                                             "Somebody had better be watching this.", "A stir in my gut. Watch how it turns." } },
            // clear of the player, whom they saw do violence ({you}: as they know him)
            { "keepclear||plain", new[] { "Give {you} a wide berth.", "Saw what that one can do. Not near me.", "Don't catch his eye. Don't.",
                                          "Some men bring trouble like weather. Stay out of it.", "Easy, now. Don't draw his notice." } },
            { "keepclear||bold", new[] { "No fear of {you}. Plain sense, that's all.", "Pick my fights. That one isn't worth it today.", "Not scared. Choosing my ground.", "Let {you} pass. Another day, maybe." } },
            { "keepclear||fearful", new[] { "That's the one. Lord, that's the one.", "Hands won't stop shaking. Eyes on the ground.", "If he looks this way I'll be sick.", "Saw what he did. Pretend I didn't." } },
            { "keepclear||cold", new[] { "Nothing to gain near {you}. Plenty to lose.", "A man like that costs more than he's worth.", "Distance is cheap. Bullets aren't.", "Stay clear and stay breathing. Simple sums." } },
            // being seen (standing); a feeling's (pride that wants an audience)
            { "showoff||plain", new[] { "Let them see me today. For once.", "Stand tall. Somebody might be watching.", "Nobody notices a body in a corner. Out in the light.",
                                        "Be seen. Be remembered. Even a little.", "Clean collar today. Might as well be seen in it." } },
            { "showoff||bold", new[] { "Let them look. They don't see the like every day.", "Out where the whole street can admire it.", "Town ought to know who walks it.", "Chin up, shoulders back. Let them talk." } },
            { "showoff||fearful", new[] { "Stand where they can see. Prove I'm not hiding.", "Act like I belong. Maybe they'll believe it.", "Show my face. Hiding only makes the talk worse.", "Smile. Look easy. Nobody needs to know." } },
            { "showoff||cold", new[] { "Being seen is half of business.", "Let them see somebody doing well. It opens doors.", "Appearances cost little and pay a lot.", "A good coat and a straight back. Folks trust that." } },
            { "showoff|feeling|plain", new[] { "Want everybody to see. Let it show on me.", "Not ashamed of it. Let them look.", "Today I walk tall. Earned it.",
                                               "Proud enough to burst. Let the street see.", "Today nobody looks past me." } },
            // the saloon: company, or the pull of the bottle or the cards (the vice)
            { "saloon||plain", new[] { "Lamps lit at the saloon. Voices. Go.", "A bit of noise and a few faces. The saloon's cure.", "Evening's long alone. The saloon's never empty.",
                                       "Piano, smoke, somebody's tall tale. Just what the day needs.", "Who's at the saloon tonight? Find out." } },
            { "saloon||bold", new[] { "Saloon's where the town's talk is. I'll be in it.", "Somebody there owes me a drink. Collect it.", "Best story in the room will be mine.", "Walk in like I own the place. Some nights I nearly do." } },
            { "saloon||fearful", new[] { "Among people. Safer in a crowd than out here.", "Loud enough in there to drown the thinking out.", "Corner table, back to the wall, a little company.", "Need to hear people laugh. Even at nothing." } },
            { "saloon||cold", new[] { "Saloon's where money changes hands. Be there.", "Drink loosens tongues. Somebody always says too much.", "The news is at the saloon before it's anywhere else.", "Sit in the saloon, buy little, hear everything." } },
            { "saloon|vice|plain", new[] { "The pull's on me tonight. Can't fight it every night.", "Saloon doors swinging. Hear them from here.", "Told myself not tonight. Told myself last night too.",
                                           "One hour at the saloon. Just the one.", "My feet find the saloon whether I steer them or not." } },
            // the store
            { "store||plain", new[] { "Out of flour, out of coffee, out of excuses.", "Lamp oil, salt, thread. Keep the list in my head.", "Cupboard's bare. Store before it shuts.",
                                      "A few things to get. And see who's in.", "Coffee's down to the grounds. Can't have that." } },
            { "store||bold", new[] { "Store man had better have the best in.", "Buy what I want and let them watch me pay.", "Pay his price and see his face. No haggling.", "Fine hat in the window. Mine, if I want it." } },
            { "store||fearful", new[] { "Out of flour. Can't put it off, much as I'd like to.", "Hope the credit's still good. Don't ask. Just hope.", "Get the things before prices climb again.", "Count the coins twice before going in." } },
            { "store||cold", new[] { "Buy cheap, buy little, buy once.", "Check his prices. He's been creeping them up.", "Store's warmer than the street, and standing in it is free.", "Tobacco. That's all. No chatter." } },
            // church
            { "church||plain", new[] { "Bells ringing. Feels right to go.", "Sit in a pew and feel small. Good to feel small.", "Hymn singing does something for a body.",
                                       "Been away too long. The Lord keeps a tally.", "Something to hold on to, on a day like this." } },
            { "church||bold", new[] { "A person of standing goes to church. Be seen there.", "Front pew. Where else?", "The preacher had better be worth the walk.", "Show the Lord I'm still in the fight." } },
            { "church||fearful", new[] { "Need the walls of a church around me.", "Sit near the back where nobody turns round.", "God sees. Better He sees me trying.", "Nothing bad happens in a church. Nothing." } },
            { "church||cold", new[] { "Church is where the town decides who's respectable.", "Shake the right hands at the door.", "Long sermon. Good business after.", "Folks notice who's missing from church. Not me." } },
            // a word with a friend ({who}: the friend)
            { "greet||plain", new[] { "There's {who}. The day just got better.", "Ought to say good day to {who}. It's been a while.", "A face I know. Thank goodness.",
                                      "{who} always has a word for me.", "A word with an old friend. Feel human again." } },
            { "greet||bold", new[] { "{who}! Not letting that pass.", "Go shake {who}'s hand. Folks should see who my friends are.", "A friend over there. Can't walk by that.", "Let them see I'm well thought of." } },
            { "greet||fearful", new[] { "{who}. Thank God. Somebody I know.", "Stand near a friend. Feel safer.", "A friend's face. Breathe easier.", "Trouble thinks twice with two of us." } },
            { "greet||cold", new[] { "{who} has friends worth knowing. Stay on the right side.", "Old acquaintance. Cheap to keep up.", "Keep friends close. Never know when.", "Nod to {who}. Keep the door open." } },
            // clear of someone they can't abide ({who}: the foe)
            { "steerclear||plain", new[] { "{who}. Not today. Can't face it today.", "Rather not cross paths with {who}. Not here.", "Bad blood there. Keep my distance.",
                                           "The long way round saves a quarrel.", "Not worth the trouble. Not worth the words." } },
            { "steerclear||bold", new[] { "Wouldn't give {who} the satisfaction of a word.", "Not running from {who}. Just not bothering.", "Go near that one and there'll be blows. Not here.", "{who} can keep to that side of the street." } },
            { "steerclear||fearful", new[] { "{who}'s there. Oh no. Go round.", "Don't let {who} see me. Please.", "Every time we meet, something bad comes of it.", "Knees gone weak. I'll go the long way." } },
            { "steerclear||cold", new[] { "{who} isn't worth the breath.", "Some people are best kept at arm's length. Or farther.", "A quarrel earns nothing. Walk on.", "Ignore {who}. Stings worse than a word." } },
        };

        /// The need a choice's why names (Choosing.Score's why, NeedState.Names[i], or "a feeling (...)"), as the pools key
        /// it; "" for anything else (their nature, the model's pick), which is the act's own.
        public static string NeedOf(string why)
        {
            if (string.IsNullOrEmpty(why))
                return string.Empty;
            if (why.StartsWith("a feeling", StringComparison.Ordinal))
                return "feeling";
            for (int i = 0; i < NeedKeys.Length && i < NeedState.Names.Length; i++)
                if (why.StartsWith(NeedState.Names[i], StringComparison.Ordinal))
                    return NeedKeys[i];
            return string.Empty;
        }

        /// The cell's need for this act: the need itself where the act has lines of its own for it, else the act's own ("").
        public static string CellNeed(string act, string need)
        {
            return !string.IsNullOrEmpty(need) && Pools.ContainsKey(act + "|" + need + "|plain") ? need : string.Empty;
        }

        /// The pools to draw from, in order (into `into`, cleared first; no allocation): their temper group's own lines for
        /// the act and need, then everyone's.
        public static void PoolsFor(string act, string need, string group, List<string[]> into)
        {
            into.Clear();
            if (act == null)
                return;
            string n = CellNeed(act, need);
            string[] p;
            if (group != null && group != "plain" && Pools.TryGetValue(act + "|" + n + "|" + group, out p))
                into.Add(p);
            if (Pools.TryGetValue(act + "|" + n + "|plain", out p))
                into.Add(p);
        }

        /// The slot an act's lines may use, when the act is about someone: {who} for a friend, a talker or a foe, {you} for
        /// the player whose violence they saw; null for the rest.
        public static string SlotOf(string act)
        {
            return act == "greet" || act == "talk" || act == "steerclear" ? "{who}" : act == "keepclear" ? "{you}" : null;
        }

        /// A line with its slots filled; null when it names someone and nobody is given (the next line is tried).
        public static string Fill(string line, string who, string you)
        {
            if (line == null)
                return null;
            if (line.IndexOf("{who}", StringComparison.Ordinal) >= 0)
            {
                if (string.IsNullOrEmpty(who))
                    return null;
                line = line.Replace("{who}", who);
            }
            if (line.IndexOf("{you}", StringComparison.Ordinal) >= 0)
            {
                if (string.IsNullOrEmpty(you))
                    return null;
                line = line.Replace("{you}", you);
            }
            return Minds.Sentences(line);
        }

        private static bool Within(int now, int at, int ms)
        {
            return at != Never && now - at >= 0 && now - at < ms;
        }

        /// Why an act's thought isn't shown now, or null (the game side's caps, here so they can be tested): a retry (the first
        /// try's moment has passed), the model's own thought came with this choice, out of view or past PickMetres, their own
        /// act thought within 3 minutes, a thought of theirs within 45 s (or one waiting on the ticker), another act thought a
        /// moment ago, or the ticker backed up.
        public static string WhyNot(bool retry, bool modelThought, bool seen, float metres, float most, int now, int lastActAt, int lastTheirsAt, bool waiting, int nextAt, int fresh)
        {
            if (retry)
                return "a retry";
            if (modelThought)
                return "the model's own thought";
            return Shared(seen, metres, most, now, lastTheirsAt, waiting, nextAt, fresh) ?? (Within(now, lastActAt, PersonGapMs) ? "theirs within 3 minutes" : null);
        }

        /// The same for an encounter's turn: the model's thought, out of view, a thought of theirs within 45 s or waiting, another
        /// turn's a moment ago, or the ticker backed up.
        public static string TurnWhyNot(bool modelThought, bool seen, float metres, float most, int now, int lastTheirsAt, bool waiting, int nextAt, int fresh)
        {
            return modelThought ? "the model's own thought" : Shared(seen, metres, most, now, lastTheirsAt, waiting, nextAt, fresh);
        }

        private static string Shared(bool seen, float metres, float most, int now, int lastTheirsAt, bool waiting, int nextAt, int fresh)
        {
            if (!seen || metres > most)
                return "out of view";
            if (waiting || Within(now, lastTheirsAt, TheirsGapMs))
                return "a thought of theirs within 45 s";
            if (nextAt != Never && now < nextAt && nextAt - now <= Math.Max(ActGapMs, TurnGapMs))
                return "another a moment ago";
            if (fresh > FreshMost)
                return "the ticker is full";
            return null;
        }

        /// The lines for an encounter's turn, kept beside each encounter's options (role|option), or null.
        public static string[] TurnPool(string role, string option)
        {
            if (role == null || option == null)
                return null;
            string k = role + "|" + option;
            string[] p;
            if (StreetCallouts.Turns.TryGetValue(k, out p) || Domestic.Turns.TryGetValue(k, out p) || Hogtied.Turns.TryGetValue(k, out p) ||
                Favours.Turns.TryGetValue(k, out p) || Warnings.Turns.TryGetValue(k, out p))
                return p;
            return null;
        }

        /// Every encounter's turns, role|option, for the tests.
        public static IEnumerable<KeyValuePair<string, string[]>> AllTurns()
        {
            foreach (var t in new[] { StreetCallouts.Turns, Domestic.Turns, Hogtied.Turns, Favours.Turns, Warnings.Turns })
                foreach (var kv in t)
                    yield return kv;
        }

        /// Every line, acts' and turns', for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in Pools)
                foreach (string l in kv.Value)
                    yield return l;
            foreach (var kv in AllTurns())
                foreach (string l in kv.Value)
                    yield return l;
        }
    }
}
