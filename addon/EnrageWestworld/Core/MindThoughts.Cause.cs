// Feelings that know their cause (the no-model run, item 6g; Lex, 2026-11-01): the anger of a shove isn't the anger of a
// theft, and fear after a killing isn't fear of a fistfight. When what they feel came from something they saw or had
// done to them (Appraisal's event kind, kept on the feeling in memory only: FeltAt.Why), most of the time the thought
// says it in these words; else, or with the face masking it, the feeling's own pools as before. Slot-free on purpose:
// the one who did it may be a man or a woman, known or not. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class MindThoughts
    {
        internal static readonly Dictionary<string, string[]> CausePools = new Dictionary<string, string[]>
        {
            { "anger|shove", new[] { "Shoved me like I was a door in the way. A door.", "Shoulder still knows where the hand landed.", "Pushed past me on my own street. On my own street.", "Nearly ended on my backside in the road. With everybody watching.", "A shove is a question. I'll answer it yet." } },
            { "anger|theft", new[] { "Pocket's lighter and my blood's hotter. Somebody's hand was in my coat.", "Worked a week for that money. A week, gone in a breath.", "Thief. In broad day, bold as a bishop.", "Keep checking the pocket like it'll fill back up.", "I'll know those fingers if I see them again." } },
            { "anger|slight", new[] { "Said it in front of people. That's what I can't let lie.", "Words don't bruise, they say. They say wrong.", "Turned my back on the joke and still heard the laughing.", "Called me that with half the street listening, and I just stood there.", "Turning the words over all afternoon. They get sharper." } },
            { "anger|brawl", new[] { "Knuckles split and still not finished with it.", "Fists aren't done even if the fight is.", "Somebody landed one on me and walked off. Walked off.", "The brawl's over. My temper didn't get the news.", "Blood in my mouth and fire in my belly." } },
            { "anger|debtcall", new[] { "Called in the money in front of the whole store. Could've waited.", "Owing is one thing. Being shamed for it in a crowd is another.", "Made me count it out on the boardwalk like a beggar.", "Owe it, yes. Didn't owe the shame of being asked like that." } },
            { "anger|lawgrab", new[] { "Lawman's hand on my collar like I was a stray dog.", "Grabbed by the law for nothing. For standing there.", "Badge thinks it makes him right. Makes him armed.", "Marched me off in front of everybody. They'll talk for a week." } },
            { "anger|challenge", new[] { "Called me out. Me. In the street, like a dime novel.", "Wants to see if I'll back down. Let's see.", "A man calls me out, I don't forget the voice.", "Squared up to me like I was nothing. Nothing." } },
            { "fear|death", new[] { "Saw a man killed. Just now. Can't stop seeing it.", "One shot and he was down, and the street went on like nothing.", "Hands won't stop. Saw the life go right out of him.", "Could have been me standing there. Could have been anybody.", "Blood in the dust. I keep looking at my own boots." } },
            { "fear|brawl", new[] { "Fists everywhere. Got out of it by luck, not wit.", "Somebody nearly fell on me. Grown men, like dogs.", "Kept to the wall till it was over. No shame in a wall.", "My heart's still going like the fight never stopped." } },
            { "fear|challenge", new[] { "A man looking for a fight looked at me. Looked right at me.", "Somebody's going to draw over this. Don't let it be near me.", "Voices raised and a hand on a gun. I'm going home.", "Felt the whole street hold its breath." } },
            { "fear|theft", new[] { "If they can get into my pocket, they can get into my house.", "Keep my money in my boot from now on. My boot.", "Watched every face on the way home. Any of them.", "Somebody's watching who has what. I don't care to be watched." } },
            { "fear|lawgrab", new[] { "Law took a man off the street just now. Could take anybody.", "Saw the irons come out. My wrists ache from watching.", "Keep my head down. The law's in a grabbing mood.", "They'll take who's nearest when they want someone. Stay far." } },
            { "gratitude|kindness", new[] { "Somebody did me a kindness and asked nothing for it. Nothing.", "Didn't have to help. Helped anyway. Won't forget it.", "There's still decent folk. Saw one today.", "Owe a good turn now. Glad to owe it.", "A kindness like that keeps a body going a week." } },
            { "relief|brawl", new[] { "Fight's done and I'm still in one piece.", "Thought it'd come my way. It didn't. Thank God.", "Walked away whole. Plenty didn't." , "Breathing slower now the fists have stopped." } },
            { "relief|lawgrab", new[] { "The law walked right past me. Right past.", "Thought the badge was coming for me. It wasn't.", "Somebody else's collar this time. Shameful to be glad. I'm glad.", "Let out a breath I didn't know I'd been keeping." } },
            { "joy|goodnews", new[] { "Good news. Real good news, for once. Could sing.", "Something went right today. Holding it carefully.", "Want to tell the whole street. Telling the horse instead.", "Smiling at nothing. That's what good news does." } },
            { "joy|kindness", new[] { "The day's turned kind. Somebody turned it.", "Light in the chest. A small thing done for me, and look.", "World's not all sharp edges. Found a soft one today.", "Whistling. Somebody was good to me, and I'm whistling." } },
            { "shame|pratfall", new[] { "Went down in the mud with the whole street looking.", "Laughed at. My ears are still hot with it.", "Made a fool of myself. They'll tell it at supper.", "Can't show my face at the store for a week." } },
            { "shame|brawl", new[] { "Beaten in front of everybody. Can't look anybody in the eye.", "Went down and stayed down. They all saw.", "Lip's split and my pride's worse off than my lip.", "Everyone in town knows I lost. Everyone." } },
            { "shame|shove", new[] { "Got pushed aside and did nothing. Nothing at all.", "Stepped back when I should have stood. They saw it.", "A grown man shoved like a child, and I let it happen.", "Should've pushed back. Will think of that all night." } },
            { "contempt|pratfall", new[] { "Couldn't keep their feet on a dry street. Sober, too.", "Watched that fall and didn't feel sorry. Should I?", "Some folk are made for the mud.", "Laughed at it. Didn't mean to. Not much." } },
            { "contempt|shove", new[] { "Only a coward shoves a man from behind.", "Pushing folk about makes nobody bigger.", "That kind always needs somebody smaller to push.", "Shoving people in the street. Never learned manners, that one." } },
            { "grief|death", new[] { "Gone. Just like that, in the street. Gone.", "Was talking to them this morning. This morning.", "Can't get warm. Somebody I knew is dead.", "Keep turning to say something to them.", "They'll bury them on Thursday. Thursday. As if it's a day like others." } },
            { "pride|brawl", new[] { "Stood my ground in the brawl. Still standing.", "Gave as good as I got. Better.", "Knuckles sore, head high. Fair trade.", "They'll talk about that fight. They'll talk about me in it." } },
            { "pride|challenge", new[] { "Called out, and I didn't back down. Not an inch.", "Let them see me not flinch. They saw.", "Stood square when it counted. That's a thing to know about yourself.", "Faced it. Hands steady. Who knew." } },
        };

        /// Most of the time a feeling with a known cause is thought in its cause's words (null: none fits, or the dice).
        public static string CauseLine(string feeling, string why, string seed, Func<string, bool> used)
        {
            if (string.IsNullOrEmpty(why))
                return null;
            string[] pool;
            if (!CausePools.TryGetValue(feeling + "|" + why, out pool) || !Voice.Chance(seed + "|cause", 65))
                return null;
            return Pick(pool, seed + "|c", used, null, null);
        }
    }
}
