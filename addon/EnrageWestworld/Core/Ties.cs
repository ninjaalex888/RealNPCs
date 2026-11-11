// What a street scene leaves between two people: a tie, kept once per pair, with the latest kind of scene, how warm
// or hostile it has been, how many times, and when. Kept only between people who can be met again (town regulars,
// residents already saved for something else, enRAGE's townsfolk), so a tie never saves anyone by itself. The two
// think about each other by name for a few days (InnerLife), and the rest of their town talks about it.
// All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class SceneTie
    {
        [DataMember(EmitDefaultValue = false)] public string A, B;           // ids: res:N for a resident, folk:... for one of enRAGE's townsfolk
        [DataMember(EmitDefaultValue = false)] public string AName, BName;   // their full names, so a thought never has to look them up
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Kind;           // the latest scene between them: argue, brawl, flirt, smalltalk...
        [DataMember(EmitDefaultValue = false)] public int Warmth;            // the scenes added up: warm above 0, hostile below (-3..3)
        [DataMember(EmitDefaultValue = false)] public int Count;             // scenes between them
        [DataMember(EmitDefaultValue = false)] public long At;               // game minute of the latest
        [DataMember(EmitDefaultValue = false)] public int Stage;             // how far a story between them has gone (follow-up scenes)
        [DataMember(EmitDefaultValue = false)] public string Doer;           // for trouble one did to the other (rough, pickpocket, lift, drunk): who did it
        [DataMember(EmitDefaultValue = false)] public bool Courted;          // they were sweethearts (a flirt, sweet words, a proposal): a quarrel since leaves them at odds (Scenes.Mend)
        [DataMember(EmitDefaultValue = false)] public long FrightAt;         // both saw the player kill, then (Core/SharedFright.cs; FIVEDAY item 22)
        [DataMember(EmitDefaultValue = false)] public string Crime;          // the latest theft between them (pickpocket, lift, mugging), kept when a later scene moves Kind on (the criminal class reads it: Core/Criminals.cs; the review of items 15-19)
        [DataMember(EmitDefaultValue = false)] public long CrimeAt;          // when
        [DataMember(EmitDefaultValue = false)] public string CrimeDoer;      // who did it
        [DataMember(EmitDefaultValue = false)] public long PutOffAt;         // when a stranger last stepped between them and both let it be (DEPTH item 7): their quarrel waits StepBetween.PutOffMinutes
        [DataMember(EmitDefaultValue = false)] public int PutOffs;           // how often that has happened since they last made up (the second time, they make it up)
        [DataMember(EmitDefaultValue = false)] public bool Sore;             // only sore sides of a big brawl so far (FOURTEENHOUR item 12): a stake in the next fight, no quarrel of its own

        /// This one did it to the other (the attacker, the thief, the drunk who called him out).
        public bool Did(string id)
        {
            return Doer != null && Doer == id;
        }

        public bool Has(string id)
        {
            return A == id || B == id;
        }

        public string Other(string id)
        {
            return A == id ? BName : AName;
        }

        public string OtherId(string id)
        {
            return A == id ? B : A;
        }
    }

    public static class Ties
    {
        public const int Max = 300;

        /// What a scene does to the two in it: warmer above 0, colder below, 0 they know each other a little better;
        /// null for a scene that leaves nothing between them (a song, a tale, a pitch to a crowd, the player's
        /// business). The exchanges (Exchanges.cs) end as comfort, brushoff, snap, cheer, envy, makeup or sweet.
        public static int? Effect(string kind)
        {
            switch (kind)
            {
                case "argue": case "brawl": case "cheat": case "debt": case "standoff": case "snap": case "envy": case "drunk": case "rowdy":
                    return -1;
                case "rough": case "pickpocket": case "mugging": case "holdup": case "runout":   // (runout: the card table walked him out of town; reserve R6)
                    return -2;   // a face into the bar, or a hand caught in a coat, isn't soon forgotten
                case "horsedeal":
                    return -1;   // sold a lame horse at the hitching post, and he knows it (FOURTEENHOUR item 14): Doer is the dealer
                case "toothpull": case "wayshown": case "welcomed": case "oldtoast": case "comrades": case "warheard": case "pledge":
                    return 1;    // his tooth out in the street; shown the way, or walked there (item 14): Doer is the one who did it
                case "welsh":
                    return -2;
                case "fencecut":
                    return -2;
                case "blamed":
                    return -2;   // doctored brands laid at his door in front of the street (Core/Brands.cs; DEPTH item 8): Doer is the one who blames
                case "cleared":
                    return 2;    // ...and taken back when the stranger told him who it was: they stand where they stood before   // his wire cut in the night, and his neighbour's name on it (Core/FenceCutters.cs; FIVEDAY item 31): Doer is the one who blames   // a bet welshed on in front of the street (Core/Wagers.cs): a matter of honour
                case "wagerowed":
                    return -1;   // a lost bet owed till payday: a sore point until it's paid
                case "wagerpaid":
                    return 0;    // paid up, with or without a sour word: square
                case "lift":
                    return 0;    // the mark never saw who: nothing between them that he knows of (the thief knows)
                case "flirt": case "propose": case "sweet": case "comfort": case "cheer": case "charity": case "help": case "defend": case "round": case "apology": case "settled":
                    return 1;
                case "scorn": case "passby": case "faced": case "snub": case "soured":   // (soured: the barkeep who watched a church woman empty his bar; DEPTH item 5: Doer is the woman)
                    return -1;   // asked for a coin and sent off with a flea in the ear; left lying in the street
                case "carried": case "thanks":
                    return 1;    // got home on a friend's arm
                case "makeup":
                    return 2;
                case "bout":
                    return 1;
                case "fright":
                    return 1;   // both saw him kill: fear seeks company (Core/SharedFright.cs)   // a fair fight out back, done: respect a step (Core/PrizeFight.cs)   // a quarrel mended counts for more than one more kind word
                case "smalltalk": case "gossip": case "haggle": case "hosts": case "brushoff": case "avoid": case "mindtalk": case "introduced":   // (introduced: made known to each other by the player, Core/Verbs.cs)
                    return 0;
                default:
                    return null;
            }
        }

        /// Friends, as opposed to two people one kind word apart: warmth 2 or more, or warm with two scenes between them
        /// (night 2026-10-28, item 10: one hand up or one round bought had everyone grieving, drinking over, condoling
        /// and taking up grudges for a man they'd met once).
        public static bool Friends(SceneTie t)
        {
            return t != null && (t.Warmth >= 2 || t.Warmth > 0 && t.Count >= 2);
        }

        /// Courting, as opposed to a friendly word.
        public static bool Romantic(string kind)
        {
            return kind == "flirt" || kind == "propose" || kind == "sweet";
        }

        /// Sweethearts now: their last scene a romantic one, or the making up after a quarrel (Scenes.Mend).
        public static bool Sweethearts(SceneTie t)
        {
            return t != null && (Romantic(t.Kind) || (t.Courted && t.Kind == "makeup"));
        }

        /// Sweethearts at odds: courting once, and their last meeting cross words, not yet past mending. (A gift turned
        /// down, or a courtship refused, is a brushoff: that ends it.)
        public static bool Cooled(SceneTie t)
        {
            return t != null && t.Courted && t.Warmth >= -1 && (t.Kind == "argue" || t.Kind == "snap" || t.Kind == "envy");
        }

        /// Notes a scene between two people: the tie between them, made or carried on. Null if the scene leaves none.
        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now)
        {
            return Note(all, aId, aName, bId, bName, town, kind, now, Trouble(kind) ? aId : null);
        }

        /// A scene where one did something to the other: A (the first in the scene) is the one who did it.
        public static bool Trouble(string kind)
        {
            return kind == "rough" || kind == "pickpocket" || kind == "lift" || kind == "drunk" || kind == "charity" || kind == "scorn" ||   // (the asking is A's doing too)
                   kind == "horsedeal" || kind == "toothpull" || kind == "wayshown" ||   // the dealer, the man with the pliers, the one who showed the way (FOURTEENHOUR item 14)
                   kind == "help" || kind == "passby" ||   // A helped B up, or walked past B
                   kind == "defend" || kind == "faced" ||  // A stood up for B; A faced B down
                   kind == "round" ||                      // A stood B a drink
                   kind == "settled" ||                    // A paid B what was owed, in the street
                   kind == "blamed" ||                     // A blamed B for the doctored brands (DEPTH item 8)
                   kind == "welsh" || kind == "wagerowed" || kind == "wagerpaid"; // A lost a bet to B: wouldn't pay, owes, or paid (Core/Wagers.cs)
        }

        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now, string doer)
        {
            int? effect = Effect(kind);
            if (effect == null || aId == null || bId == null || aId == bId)
                return null;
            SceneTie t = all.FirstOrDefault(x => x.Has(aId) && x.Has(bId));
            if (t == null)
            {
                t = new SceneTie { A = aId, B = bId };
                all.Add(t);
            }
            bool keepRomance = Sweethearts(t) && !Romantic(kind) && effect.Value > 0;   // a kindness between sweethearts: still sweethearts
            // small talk, the paper, a nod between sweethearts (or sweethearts at odds) leaves what's between them standing
            bool keepKind = keepRomance || ((Sweethearts(t) || Cooled(t)) && effect.Value == 0 && kind != "brushoff");
            if (Romantic(kind) || Romantic(t.Kind))
                t.Courted = true;
            if (t.Count > 0 && !keepKind)
                t.Stage++;   // they meet again: their story moves on (Reasons)
            t.AName = t.A == aId ? aName : bName;
            t.BName = t.A == aId ? bName : aName;
            t.Town = town ?? t.Town;
            if (!keepKind)
                t.Kind = kind;
            if (doer != null || kind != "avoid")
                t.Doer = doer;   // keeping clear of him doesn't change who did what to whom
            if (doer != null && Criminals.IsCrime(kind))
            {
                t.Crime = kind;   // kept when a later scene between them moves Kind on: small talk since doesn't wipe a theft (the review of items 15-19)
                t.CrimeAt = now;
                t.CrimeDoer = doer;
            }
            t.Warmth = Math.Max(-3, Math.Min(3, t.Warmth + effect.Value));
            if (kind == "settled" && t.Warmth < 1)
                t.Warmth = 1;   // squared up: whatever the calling in left between them, it's mended
            t.Count++;
            t.At = now;
            while (all.Count > Max)
                all.Remove(all.OrderBy(x => x.At).First());
            return t;
        }

        /// This person's ties from the last few days, newest first.
        public static List<SceneTie> Of(IEnumerable<SceneTie> all, string id, long now, int days)
        {
            return all.Where(t => t.Has(id) && now - t.At >= 0 && now - t.At < days * 1440L).OrderByDescending(t => t.At).ToList();
        }

        /// The town's talk: ties in this town from the last two days worth talking about (warm or hostile), not
        /// the thinker's own, newest first.
        public static List<SceneTie> TalkOf(IEnumerable<SceneTie> all, string town, string notId, long now)
        {
            return all.Where(t => t.Town == town && t.Kind != "lift" && (t.Warmth < 0 || Romantic(t.Kind) || t.Kind == "makeup" || t.Kind == "charity" || t.Kind == "help" || t.Kind == "round" || t.Kind == "settled") && !t.Has(notId) && now - t.At >= 0 && now - t.At < 2 * 1440L)
                      .OrderByDescending(t => t.At).ToList();
        }

        // ---- what they think of each other --------------------------------------------------------------

        private static readonly string[] Quarrel =
        {
            "Shouldn't have said that to {other}. Or should have said worse. Can't decide which.",
            "Words with {other}. Keep having the argument again in my head, and winning it this time.",
            "{other} looked at me like I was dirt on a boot. I'll remember that face.",
            "{other} got the final say. I've had a hundred since, all in my head.",
            "Still hot in the face over {other}. Supper went cold while I stewed.",
            "Should've walked away from {other}. Didn't. Never do.",
            "{other}'s voice in my ears all afternoon. Like a fly at a window.",
            "Half the street heard me and {other}. Let them.",
            "I was right. {other} was louder. That's not the same thing.",
            "Tossed and turned over {other}. The pillow's no help.",
            "Next time {other} starts, I'll keep my mouth shut. I won't, but I'll mean to.",
            "{other} said I'd always been a fool. Always. Like {he}'d kept count.",
            "Words with {other} again. Same words. Same mud.",
            "Kicked the stove over {other}. The stove didn't deserve it.",
            "Thought of the right answer an hour later. {other} would've been struck dumb by it.",
            "{other} pointed at me. Pointed. Like a schoolmarm.",
            "Ma used to say hold your temper. {other} makes it slippery.",
            "Can't see {other}'s hat in a crowd without my jaw going tight.",
            "Whole quarrel over nothing, with {other}. Nothing's the worst kind.",
            "{other} won't let it lie. Neither will I, it seems.",
            "Gave {other} a piece of my mind. Haven't much to spare.",
            "Told the dog about {other}. The dog took my side.",
            "Bit my tongue with {other}. Then let go of it.",
            "Should I go to {other} and settle it? Pride says no. Pride's loud today.",
            "{other}'s face when I said it. Almost worth the rest.",
            "Ears still ringing from {other}'s shouting. Or mine.",
            "Saw {other} whispering after. About me, I'd bet a dollar.",
            "There's a stone in my boot, and its name is {other}.",
            "Cleaned the lamp chimney twice, thinking of {other}. It was clean the first time.",
            "{other} thinks {he} won. Let {him} think it."
        };

        private static readonly string[] Blows =
        {
            "Jaw still aches. {other} hits like a mule kicks.",
            "Knuckles split from {other}'s teeth. Worth it. Mostly.",
            "Came to blows with {other}. In the street, like boys. Can't look anybody in the eye.",
            "Lip's split. {other}'s eye is worse. That's something.",
            "Can't close my right hand. {other}'s skull is hard as a stump.",
            "Washed {other}'s blood off my knuckles. Some of it was mine.",
            "Rolled in the dirt with {other}. Grown men. Lord.",
            "{other} fights dirty. Thumb in the eye. I'll know next time.",
            "Ribs ache when I breathe. Every breath, {other}.",
            "Somebody pulled us apart, me and {other}. Good thing, maybe.",
            "Hat's ruined. Stepped on in the fight with {other}.",
            "{other} got the first one in. I got the last. I'll call that even.",
            "Tooth's loose. Keep touching it with my tongue. Keep thinking of {other}.",
            "Felt good, hitting {other}. Doesn't feel good now.",
            "Shirt torn at the shoulder. {other} owes me a shirt.",
            "Everyone saw me and {other} go at it. Everyone will tell it wrong.",
            "My hands still shake from it. Not fear of {other}. Not quite.",
            "{other} bled on my boots. Can't get the stain out.",
            "Should've kept my fists in my pockets. {other} wouldn't have.",
            "Raw meat on my eye, like Pa used to. Thinking of {other}.",
            "{other} dropped like a felled post. Got up like a mule.",
            "Ear's thick as a biscuit. {other}'s doing.",
            "Last time I fought, I was fourteen. {other} made me fourteen again.",
            "Saw {other} limping after. Don't feel as sorry as I should.",
            "Won't drink where {other} drinks. Not for a while.",
            "Swollen knuckles on the coffee cup. Every sip, {other}.",
            "Fighting {other} settled nothing. Never does. Felt like it would.",
            "Wonder if {other}'s lying awake too.",
            "Taste of blood all morning. Iron and dust and {other}.",
            "Next time {other} raises a hand, I'll be ready. Or gone."
        };

        private static readonly string[] Money =
        {
            "{other} and the money. Always the money between us now.",
            "Saw {other} buy a round like a rich man. With whose money, I wonder.",
            "{other} owes me, and walks around like the sun owes {him} a living.",
            "Counted what {other} owes. Counted it again. Same number.",
            "Lent {other} money. Should've lent {him} a rope.",
            "Every time {other} buys tobacco I add it up. That's my tobacco.",
            "{other} says next week. Said next week last month.",
            "Money sours everything. Sours me and {other}, anyhow.",
            "Would've given {other} the shirt off my back. Now I want the shirt back.",
            "{other} won't meet my eye since the money.",
            "Could use what {other} owes me. Rent's due Friday.",
            "{other}'s good for it. I keep telling myself. {other}'s good for it.",
            "Saw {other} at the card table. With my money, likely.",
            "Never lend to friends, Pa said. Should've listened. {other}.",
            "{other} talked fine words when {he} borrowed. Quiet now.",
            "Ink on the IOU's faded. {other}'s memory's faded faster.",
            "Five dollars is five dollars, {other}. Even to you.",
            "Lie awake doing sums. Every sum ends in {other}.",
            "{other} bought a new hat. A new hat.",
            "Might have to ask {other} plain. Hate asking.",
            "{other} paid half. Half's a start. Half's an insult.",
            "Money between neighbours is a fence nobody built on purpose. Me and {other}.",
            "{other} crossed the street when I came. Money does that.",
            "Should I let {other} keep it? Buy myself some peace?",
            "If {other} pays, I'll stand {him} a whiskey. If.",
            "Told the storekeeper about {other}'s debt. Shouldn't have. Did.",
            "Wonder if {other} even remembers. They remember everything else.",
            "Every dollar {other} spends is a dollar of mine walking by.",
            "Money's money, and {other}'s {other}. Hard to keep the two apart.",
            "{other} owes me money and an honest word. Neither's come."
        };

        private static readonly string[] Sweet =
        {
            "{other} laughed at what I said. Twice. I've been counting.",
            "Thinking about {other} again. Burned the coffee doing it.",
            "Wonder if {other} thinks of me at all. Probably not. Maybe.",
            "Wore my good collar on the chance of seeing {other}.",
            "{other} said my name. Just my name. Heard it all afternoon.",
            "Walked past {other}'s door twice. On purpose. Don't tell anyone.",
            "Wonder what {other} looks like first thing in the morning.",
            "Every bell on the street, I look up. In case it's {other}.",
            "{other} smells of lavender and something else. Can't place it.",
            "Practised what I'd say to {other}. Said none of it.",
            "Ma would say I'm mooning. Ma would be right. {other}.",
            "{other} touched my sleeve. I keep looking at the sleeve.",
            "Bought a ribbon. Haven't the nerve to give it to {other}.",
            "Can't eat. Can't sleep. Can think of {other}, though.",
            "{other} laughed at my bad joke. My worst one.",
            "Would {other} wait for the likes of me? Doubt it. Hope it.",
            "Heart going like a hammer when {other} passed.",
            "Wrote {other}'s name in the dust on the window. Rubbed it out fast.",
            "{other} has a freckle by the ear. Left ear. I counted.",
            "Sunday I'll sit where I can see {other}. The preacher can manage.",
            "{other} said 'good day' different to me. I'm sure of it.",
            "Picked wildflowers. Stood there holding them like a fool. No {other}.",
            "When {other} smiles the whole street goes quiet. In my head, anyhow.",
            "Thought about {other} so long the coffee went cold. Again.",
            "Could I make {other} happy? I'd try. Lord, I'd try.",
            "Saw {other} with somebody else. Only talking. Only talking.",
            "{other}'s handwriting on a note. Kept the note.",
            "Asked after {other} at the store. Too casual. Fooled nobody.",
            "{other}. Just the name's enough to set me grinning.",
            "{other} waved. At me. Looked behind me to be sure."
        };

        private static readonly string[] Known =
        {
            "Talked with {other} a good while. Easy company. Rare, that.",
            "{other} said a thing I keep turning over. Clever, for a talker.",
            "{other} and I had a good long jaw. Hours went by like minutes.",
            "{other} knows a thing or two about horses. Didn't expect that.",
            "Laughed with {other}. Haven't laughed like that since spring.",
            "{other} told me about {his} people back east. Felt like I'd met them.",
            "Good to have someone to pass the time with. {other}, of all people.",
            "{other} remembered my name. Most don't bother.",
            "Talked weather with {other}. Then talked everything else.",
            "{other} has a dry way of saying things. Took me a while to catch on.",
            "Easy, talking to {other}. Like an old boot.",
            "{other} said {he}'d buy the next coffee. I'll hold {him} to it.",
            "Never knew {other} had a brother in the army.",
            "{other} talks with {his} hands. Nearly lost my hat.",
            "Nice, being asked my opinion. {other} asked twice.",
            "{other} and me see the town the same way. Rare.",
            "Bumped into {other} again. Twice in a week. Getting to be a habit.",
            "{other} said the same thing Ma used to say. Gave me a turn.",
            "Wonder what {other} makes of me. Hope it's decent.",
            "{other} walked me halfway home, talking. Didn't notice the cold.",
            "Told {other} something I hadn't told anybody. Strange.",
            "{other} doesn't gossip. I noticed that. I like that.",
            "Talked about nothing with {other}. Best kind of nothing.",
            "{other} reads. Quoted a book at me. Couldn't tell you which.",
            "Might ask {other} to supper. Might not. Might.",
            "{other} has a laugh like a door with a bad hinge. I like it.",
            "{other} knows everybody. Told me who's who in this town.",
            "Good to know a face like {other}'s on this street.",
            "{other} asked how I was. Waited for the answer, too.",
            "{other}'s easy company, and that's worth something out here."
        };

        private static readonly string[] Kindness =
        {
            "{other} asked after my trouble. Meant it, too. Didn't expect that.",
            "{other} was glad for me. Truly glad. There's few would be.",
            "{other} brought me soup. Soup. I near wept into it.",
            "{other} said it'd come right. Said it like {he} believed it.",
            "Didn't ask for help. {other} gave it anyway.",
            "{other} sat with me a while. Didn't say much. Didn't need to.",
            "I won't forget {other} stopping. Not for a long time.",
            "{other}'s kindness has a weight to it. Good weight.",
            "Tried to pay {other} back. {other} waved it off.",
            "Funny how one kind word from {other} turns a whole day.",
            "{other} remembered my trouble. Asked after it a week on.",
            "Plenty walked by. {other} didn't.",
            "{other} put a hand on my shoulder. Still feel it there.",
            "Owe {other} more than coffee. Coffee's a start.",
            "{other} has troubles of {his} own. Still made room for mine.",
            "There's good people yet. {other}'s one.",
            "Ma would've liked {other}. That's the highest I've got.",
            "{other} said I'd done right. Hadn't heard that in a while.",
            "Will look out for {other} from now on. That's how it goes.",
            "{other} made me laugh when I'd no business laughing.",
            "Thought I was alone in it. {other} showed me otherwise.",
            "Kindness from {other}. Didn't know where to put my hands.",
            "{other} gave me {his} time. Rich folk don't give that.",
            "Wrote {other}'s name in my book. So I won't forget.",
            "{other} didn't make a show of it. Just did it.",
            "Felt foolish taking {other}'s help. Felt worse turning it down.",
            "{other} has kind eyes. Never noticed before today.",
            "Pray {other}'s day goes easy. They made mine easier.",
            "Small thing {other} did. Biggest thing all week.",
            "Next time someone's down, I'll do as {other} did."
        };

        private static readonly string[] Fright = { "{other} saw it too. We don't speak of it, except to each other.", "Only {other} knows what that was like. Only {other}.", "{other} and I stood there while it happened. That's a thing between us now.", "I look for {other} in the street these days. I don't know why.", "{other} was there. I'm glad of it, God forgive me." };

        private static readonly string[] Bout = { "{other} hits harder than they say. Fair fight, though.", "Stood up to {other} out back. Neither of us shamed.", "{other} shook my hand after, for all the blood.", "Fought {other} for money. Got a bit of respect for my trouble.", "I'll buy {other} a drink one day. Not today. Jaw's sore." };

        private static readonly string[] MadeUp =
        {
            "Made it up with {other}. Like setting down a sack of feed I'd carried a month.",
            "{other} said sorry first. I'd have said it second. Probably.",
            "Shook hands with {other}. Firm grip. Meant it.",
            "{other} and I are square now. Feels like a window opened.",
            "Laughed with {other} today. After all that. Laughed.",
            "Took both of us being stubborn, and both of us giving in. {other} first.",
            "Should've made it up with {other} months back. Wasted months.",
            "{other} said {he} was sorry. I said I was sorrier. We'll argue about that instead.",
            "Bought {other} a drink. Drank it together. Like before.",
            "Lighter on my feet since {other} and I settled it.",
            "Ma always said never let the sun go down on it. Took a few sundowns with {other}.",
            "{other}'s not so bad. Never was, really.",
            "Mended fences with {other}. Real fence still needs mending, too.",
            "Feels strange, being easy with {other} again. Good strange.",
            "Grudges are heavy. Set mine down with {other}.",
            "{other} remembered my birthday after all that. Didn't expect it.",
            "Pride cost me {other} a while. Not anymore.",
            "Saw {other} on the street and smiled. Didn't have to think about it.",
            "{other} and me will have words again, someday. Fine. We'll mend them again.",
            "Sat with {other} at supper. Talked like the quarrel never was.",
            "Glad I swallowed it with {other}. Tasted bad. Went down fine.",
            "The town'll have to find something else to talk about. Me and {other} are done fighting.",
            "Hand on {other}'s shoulder. Theirs on mine. That was all it took.",
            "Shouldn't have taken so long. {other} knows it. I know it.",
            "Back to borrowing {other}'s tools. Missed that.",
            "Wrote {other} a note. Tore it up. Said it to {his} face instead.",
            "Slept the whole night through. First since the quarrel with {other}.",
            "Some quarrels end with a shot. Mine with {other} ended with coffee. Thank the Lord.",
            "{other} grinned at me like old times. Old times are back.",
            "Peace with {other}. Small word. Big thing."
        };

        private static readonly string[] Shrugged =
        {
            "Told {other} my news. Got a shrug for it. I'll remember the shrug.",
            "{other} couldn't get away from me fast enough. Fine. Fine.",
            "Thought {other} would be glad for me. {other} looked at the clock.",
            "{other} yawned while I talked. Yawned.",
            "Tried to tell {other} my troubles. Might as well've told the hitching post.",
            "{other} had somewhere to be. Always somewhere to be.",
            "Small thing, {other} not caring. Sits heavy all the same.",
            "Won't share my news with {other} again.",
            "{other} said 'that's nice' and turned away. That's nice.",
            "Funny who listens and who doesn't. {other} doesn't.",
            "{other} talked over me. Twice. Didn't notice, I think.",
            "Should've known better than to tell {other} anything.",
            "{other} has ears for gossip and none for me.",
            "Felt a fool, going on to {other} like that.",
            "{other} looked past me at somebody better.",
            "Cold, {other}. Cold as a creek in March.",
            "Kept my news to myself after that. {other} taught me.",
            "Thought we were friendlier than that, {other} and me.",
            "{other} nodded the way you nod at a dog.",
            "One word from {other}. One. Then gone.",
            "Wanted {other} to ask more. {other} never asks.",
            "{other}'s mind was miles off. Not on me.",
            "Told {other} straight out. Got a shrug. A shrug.",
            "Won't hold it against {other}. Will remember it, though.",
            "Some folks are kind and some are busy. {other}'s busy.",
            "Felt smaller after talking to {other}.",
            "{other} meant no harm. That almost makes it worse.",
            "Walked off from {other} quicker than I meant to.",
            "Felt it in my throat, how little {other} cared.",
            "Should've told the barber instead of {other}. He at least listens."
        };

        // trouble one did to the other: the one who did it, and the one it was done to
        private static readonly string[] RoughDid =
        {
            "Put {other}'s face into the bar. Felt good for about a minute.",
            "Hand still smells of {other}'s hair oil. Should've washed it.",
            "Folks give me room since that business with {other}. I like the room.",
            "{other} had it coming. Said so to anyone who'd listen.",
            "Barkeep gave me a look after what I did to {other}. Let him look.",
            "Knuckles still white from gripping {other}'s collar.",
            "Didn't think. Just grabbed {other}. Thinking came after.",
            "{other}'s face hit the wood loud as a gunshot. Can still hear it.",
            "Might've gone too far with {other}. Might not've gone far enough.",
            "{other} won't run {his} mouth for a while. Can't, with that lip.",
            "Woke up thinking of {other}'s face going down. Not proud. Not sorry.",
            "If the law asks, {other} slipped.",
            "My temper got the better of me with {other}. Got the better of {other} too.",
            "Felt the bar shake when {other} hit it. Felt it in my arm all night.",
            "Pa would've taken the strap to me for what I did to {other}.",
            "Can't take it back. Wouldn't, with {other}.",
            "{other} looked so surprised. That's the part I keep seeing.",
            "Drinks taste different since {other}. Don't know why.",
            "Should I square it with {other}? Not likely.",
            "{other} had friends in that room. Nobody moved. Remember that.",
            "Seen {other} since. They looked at the floor. Good.",
            "Don't feel like a big man. Felt like one for a minute, with {other}.",
            "Sheriff's been asking about me and {other}. Keeping my head down.",
            "{other} had a mouth. I had a quicker hand.",
            "Thought about it all night. {other} started it. I'm near sure.",
            "If {other}'s brother comes looking, I'll be ready.",
            "Can't settle. Keep seeing the blood on the wood from {other}'s nose.",
            "Washed my hands three times. Still feel {other}'s collar in them.",
            "Hope {other} thinks twice now. Before opening that mouth.",
            "{other}'s hat rolled under a table. Nobody picked it up."
        };
        private static readonly string[] RoughDone =
        {
            "Can't chew on the left side since {other} put my face in the bar. Every bite, I think of {other}.",
            "Everybody saw. Everybody saw {other} do it, and nobody said a word.",
            "I'll be standing with my back to the wall from now on. {other} taught me that much.",
            "Nose won't stop throbbing. {other} did that.",
            "Mirror's no friend this week. {other}'s handiwork all over my face.",
            "Flinch when anybody comes up fast now. {other}'s gift.",
            "{other} grabbed me like a sack of meal. Like I was nothing.",
            "Eye's swollen shut. The other one sees {other} everywhere.",
            "Didn't even see it coming. {other} was just there, and then the wood was.",
            "Folks look at my face and then look away. {other} put that look there.",
            "Can't drink in that saloon now. Not with {other} in it.",
            "Lay awake planning what I'd do to {other}. Won't do any of it.",
            "Lip split in two places. Every word stings. Every word, {other}.",
            "Ma would cry to see me. Good thing she can't. {other}.",
            "The worst of it isn't the hurt. It's how quick {other} was.",
            "Nobody stopped {other}. Not one of them.",
            "Tasted varnish and blood for a day after. {other}.",
            "Want to be angry at {other}. Mostly I'm afraid.",
            "Doctor says nothing's broke. {other} meant it to be.",
            "Walked the long way round to miss {other}. Felt like a dog doing it.",
            "Somebody should do to {other} what {other} did to me.",
            "Shame's worse than the bruise. {other} gave me both.",
            "Heard {other} laughing about it. Laughing.",
            "Couldn't sleep for the ache. Or for thinking of {other}.",
            "Don't know what I said to set {other} off. Doesn't matter now.",
            "Every time a door bangs I'm back there with {other}'s hand on my neck.",
            "There'll be a day for {other}. There will.",
            "Can still feel {other}'s grip on the back of my collar.",
            "Hat's still on that saloon floor. Not going back for it. {other}.",
            "Spat pink all morning. {other}."
        };
        private static readonly string[] LiftDid =
        {
            "{other} never felt a thing. Some folks walk around asking to be robbed.",
            "Three dollars and a lucky rabbit's foot off {other}. Wasn't lucky for {other}.",
            "Saw {other} patting {his} coat. Didn't look back. You never look back.",
            "{other} kept the purse in the left pocket, loose. Two fingers and it was mine.",
            "Passed {other} on the boardwalk after and tipped my hat. Never been so polite in my life.",
            "{other}'s money bought my supper. Ate it slow, with one eye on the door.",
            "Heard {other} asking at the store if anybody'd found a purse. Kept my hands flat on the counter.",
            "Easy pickings, {other}. Then I saw {his} shoes, mended twice. Should have picked a fatter coat."
        };
        private static readonly string[] LiftDone =
        {
            "Had money this morning. Somewhere between here and the store it walked off.",
            "Somebody had a hand in my coat. I keep going over every face I passed.",
            "Lost my money. Or it was took. Either way I'm a fool, and I'll eat beans till Saturday.",
            "Turned my coat out three times on the bed. Lint and a button. Nothing else.",
            "My mother's ring was in that purse, besides the money. The money I could stand to lose.",
            "Keep a hand on my pocket in a crowd now. A fool's habit. A needful one.",
            "Everybody I passed today looked like a thief. The preacher too, for a moment.",
            "Told the deputy. He wrote nothing down. Asked if I'd looked in my other boot."
        };
        private static readonly string[] CaughtDid =
        {
            "{other} saw my face. Has to have. Best stay off that street a while.",
            "Should've let {other} be. Should've picked an easier mark.",
            "{other}'s grip on my wrist, like a trap on a rabbit. Wrist's still red.",
            "{other} hollered thief so loud the horses shied. My ears are ringing with it yet.",
            "Ran three streets from {other} till my lungs burned. Didn't even get the purse.",
            "If {other} gives the sheriff my face, I'm finished in this town. Packed my bag, in case.",
            "Thought {other} was dozing on that bench. Dozing with one hand ready, it turns out.",
            "Can't stop looking at my own hand, the one {other} caught. Clumsy thing."
        };
        private static readonly string[] CaughtDone =
        {
            "I'd know {other}'s face anywhere now. Had a hand right in my pocket.",
            "{other}. Bold as brass, in broad day. I'll count my change twice around {other}.",
            "Felt the tug, turned round, and there was {other}. Hand in my coat.",
            "{other} had the nerve to smile when I caught {him}.",
            "Keep my money in my boot now. Thanks to {other}.",
            "Thought {other} was decent. So much for thinking.",
            "Told the whole street about {other}. Every word true.",
            "{other}'s fingers were quick. Not quick enough.",
            "Pat my pocket every few steps now. Can't help it. {other}.",
            "Ought to have called the sheriff on {other}. Still might.",
            "{other} looked me in the eye with {his} hand in my pocket. Bold as a crow.",
            "Lost nothing but my trust. {other} took that clean.",
            "Next time I see {other}, I'll cross the street and count to ten.",
            "{other} said it was a mistake. Hand slipped. Into my coat.",
            "Can't stand in a crowd now without thinking of {other}.",
            "Wonder how many {other} got before me.",
            "Watch {other}'s hands now, every time. Every time.",
            "Felt sick after catching {other}. Like I'd been the one at fault.",
            "Pockets sewn shut. Never thought I'd need to. {other}.",
            "{other} goes to church. Sits three pews ahead. Thief.",
            "Caught {other} fair. Folks believed me, too.",
            "Will tell my children about {other}. Watch your pockets, I'll say.",
            "{other}'s face when I grabbed that wrist. Almost funny.",
            "Ain't the money. It's that {other} thought I wouldn't notice.",
            "Saw {other} at the store after. Counted my change twice.",
            "Somebody ought to warn the next fool about {other}. I'm doing it.",
            "Had my whole week's pay in that coat. {other} knew it, too.",
            "Can't decide if I'm angrier at {other} or at me for being easy.",
            "{other} ran. That says all there is to say.",
            "{other}'s hand was cold. That's what I remember. Cold."
        };
        private static readonly string[] AvoidDid =
        {
            "{other} saw me coming and found somewhere else to be. Good.",
            "{other} won't look me in the eye now. Suits me.",
            "{other} crossed to the far boardwalk the moment I stepped out. Left a sentence hanging, too.",
            "Funny, how {other} finds a sudden need for the hardware store whenever I'm about.",
            "{other} used to sit by the stove at the saloon. Sits by the door now. I took the stove.",
            "Gave {other} a nod today, to see. {other} flinched like I'd thrown something.",
            "{other} keeps clear of me. Part of me's proud of it. A smaller part isn't.",
            "Took a drink where {other} drinks, just to see. {other} left the glass half full."
        };
        private static readonly string[] AvoidDone =
        {
            "Went the long way round so as not to pass {other}. Coward's walk. Still walking it.",
            "Saw {other} and my hands started shaking. Hate that they do that.",
            "Took the alley so I'd not pass {other}. Felt like a cat dodging a dog.",
            "Saw {other}'s hat and turned round. Bought nothing I came for.",
            "Not going near {other}. Not after last time.",
            "My legs go wrong when {other}'s close. Can't help it.",
            "Sat with my back to the wall in case {other} came in.",
            "The store's out of my way now. {other} drinks across from it.",
            "Waited till {other} rode off before I came out.",
            "Hate that {other} decides where I walk.",
            "Heard {other}'s laugh down the street. Went home.",
            "Some day I'll walk right past {other}. Not today.",
            "Kept my eyes on my boots when {other} went by.",
            "{other} doesn't even know I'm hiding. That's the worst part.",
            "If {other} says a word to me I'll freeze. I know I will.",
            "Ma says hold your head up. Ma never met {other}.",
            "Heart thumps if I catch sight of {other}. Like a rabbit.",
            "Missed the post office again. {other} was outside it.",
            "Hate being afraid. Hate {other} more for it.",
            "Took my coffee and went. {other} came in.",
            "Wish I had the nerve to look {other} in the eye.",
            "A grown soul hiding from {other}. Fine sight I make.",
            "Practise what I'd say to {other}. Never say it.",
            "Saw {other} with friends. Walked faster.",
            "Dreamed of {other} last night. Woke up sweating.",
            "Avoiding {other} has me late for everything.",
            "One day I'll stop crossing the street for {other}. One day.",
            "{other} owns this end of town now, as far as my feet are concerned.",
            "Let {other} have the boardwalk. I'll take the mud.",
            "Counted the steps home. Two hundred, the long way. {other}."
        };
        // {other}: the one who fell (for the one who helped or walked past), or the one who helped or walked past
        private static readonly string[] HelpDid = { "{other} took a fall and I helped {him} up. Glad I was there.", "Hope {other}'s mending. Went down hard.",
            "Still got road dust on my sleeve from lifting {other}. Leaving it there a while.",
            "{other} weighed next to nothing coming up off the ground. Worries me, that.",
            "{other} kept saying sorry for falling. As if anybody falls on purpose.",
            "Saw {other} limping past the church today. Wanted to ask after the knee. Didn't.",
            "Felt ten feet tall after helping {other} up. Then felt foolish for feeling it.",
            "{other} grabbed my coat coming up and tore a button off. Worth a button." };
        private static readonly string[] HelpDone = {
            "{other} picked me up when I fell. I owe {other} for that.",
            "{other} stopped. Plenty didn't.",
            "{other} brushed the dirt off my coat. Like Ma would.",
            "Knee's sore, pride's sorer. {other} was kind about both.",
            "Could've lain there an hour. {other} came in a minute.",
            "{other}'s hand was steady. Mine wasn't.",
            "Thanked {other} twice. Should've thanked {him} three times.",
            "Wonder what I'd have done if {other} hadn't come.",
            "{other} didn't laugh. Others did. {other} didn't.",
            "Feel I owe {other} a pie. Maybe two.",
            "Tumbled in the street like a fool. {other} made it not matter.",
            "Will look out for {other}, should {he} ever fall.",
            "{other} didn't ask anything for it. Just went on.",
            "Ankle's swelling. {other} said to keep it up. I will.",
            "Kind of {other}. The town's full of folk who wouldn't.",
            "Told Ma about {other} in a letter. She'll like that.",
            "{other}'s face when {he} reached me. Worried. For me.",
            "Didn't know {other} could lift like that.",
            "Will remember {other}'s name now. Didn't before.",
            "Ashamed of falling. Grateful to {other}. Both at once.",
            "{other} stayed till I could stand. That's a friend.",
            "Hope I'd do as {other} did, if it was {him} down there.",
            "{other} picked up my hat too. Dusted it. Small thing. Big thing.",
            "Every time I see {other} now, I'll think of the street, and the hand.",
            "Somebody laughed while I lay there. Then {other} came.",
            "Feel steadier for knowing {other}'s about.",
            "{other} made sure I got home. Wouldn't take a coin.",
            "There's good in this town. Its name's {other}, today.",
            "{other}'s grip was warm. I was cold right through.",
            "Might bake for {other}. Might just say it again. Thank you."
        };
        private static readonly string[] PassDid = { "{other} took a tumble. Not my affair.", "{other} looked at me like I'd done something. I hadn't.",
            "Had a full sack on my shoulder when {other} went down. Couldn't set it in the mud. Could I?",
            "{other} had people round in a minute. Didn't need me as well.",
            "Keep telling myself somebody else stopped for {other}. Somebody did. I think.",
            "{other} turned a shoulder to me at the post office. All this over one fall in the road.",
            "Was late for the train. That's all it was. {other} can think what {other} likes.",
            "If it'd been my own mother I'd have stopped. {other}'s not my mother." };
        private static readonly string[] PassDone = {
            "{other} stepped right past me. Me lying in the street.",
            "I won't forget {other} walking on by.",
            "{other} looked right at me lying there. Then looked at the sky.",
            "Didn't ask {other} for much. A hand. That's all.",
            "{other} stepped round me like a puddle.",
            "Won't forget {other}'s boots going by my face.",
            "Thought better of {other}. Thought wrong.",
            "If {other} ever falls, I'll stop. Just to show {him}.",
            "Lay in the dirt and watched {other} walk off.",
            "Funny how quick {other} remembered somewhere to be.",
            "{other} saw me. Don't tell me {he} didn't.",
            "Would it have cost {other} anything? A minute. A minute.",
            "Still feel the street's chill. And {other}'s back.",
            "Told everyone how {other} walked on by. Told it plain.",
            "{other} tips {his} hat to me now like nothing happened.",
            "My knee healed. What {other} did hasn't.",
            "Some folks are all Sunday and no Monday. {other}.",
            "Picked myself up. No thanks to {other}.",
            "Ma always said you learn who folks are when you fall. {other}.",
            "Can't look at {other} the same way. Won't try.",
            "Watched {other}'s shadow go by. Didn't even slow.",
            "Hope {other} slept well. I didn't.",
            "There's a list I keep: who stopped, who didn't. {other} didn't.",
            "A stranger would've stopped. {other} knows me. Didn't.",
            "Smile at {other}? I'll smile. I'll remember too.",
            "{other} could've said a word. Even a word.",
            "Still got the scrape on my palm. Still got {other}'s back in my eye.",
            "{other} walked past me like I was part of the road.",
            "Dusted myself off while {other} turned the corner.",
            "There's a kind of quiet that's a lie. {other}'s, walking by."
        };
        // standing up to a bully: {other} the one picked on (for the one who stepped in), or the one who stepped in; faced: the bully, or the one who faced him
        private static readonly string[] DefendDid = { "Stood up for {other}. Hands shook after. Worth it.", "Couldn't stand by and watch it done to {other}.",
            "My voice cracked telling the bully to leave {other} be. Cracked like a boy's. Held my ground, though.",
            "{other} thanked me with tears standing in both eyes. Didn't know where to look.",
            "The bully's glare follows me down the street now. {other} sleeps easier. Fair trade.",
            "Didn't think, just stepped between them. {other} would have done the same. I hope.",
            "Still feel where my heart was banging when I stood up for {other}. Right under the collarbone.",
            "Pa never stood up for anybody in his life. I stood up for {other}. That's one up on Pa." };
        private static readonly string[] DefendDone = {
            "{other} stepped in for me. Didn't have to.",
            "Owe {other}. Plain as that.",
            "{other} stood between me and it. Just stood there. Didn't have to.",
            "Thought I was done for. Then {other}'s voice.",
            "Don't know if I'd have done the same for {other}. Should've. Will.",
            "{other}'s a braver soul than me. Found that out the hard way.",
            "Owe {other} my teeth, likely.",
            "Hands still shaking. Not from fear now. From {other} stepping in.",
            "{other} said three words and the bully went. Three words.",
            "Will stand by {other}, come what may. That's settled.",
            "Never had anyone stand up for me before {other}.",
            "{other} didn't make a show of it after. Just went on.",
            "Can walk this street again. Thanks to {other}.",
            "Told {other} I owe {him}. {other} said nonsense. I owe {him}.",
            "Saw the look {other} gave him. Didn't know {other} had that in {him}.",
            "Ma always said find your people. Found one. {other}.",
            "Won't forget the sound of {other} saying enough.",
            "Shame I needed it. Glad {other} gave it.",
            "{other}'s name is good in my house from now on.",
            "If anybody comes for {other}, they'll find me there.",
            "{other} took a risk for me. Folks don't, much.",
            "Lay awake thinking of {other} stepping in. Good thoughts, for once.",
            "Bought {other} a drink. Not near enough.",
            "Don't feel as small since {other} stood up.",
            "{other} looked at me after like it was nothing. It wasn't nothing.",
            "Wish I'd thanked {other} better. Choked on it.",
            "Never liked {other} much before. Changed my mind.",
            "{other} didn't even know me well. Stood up anyway.",
            "Keep thinking what would've happened without {other}. Don't like the picture.",
            "{other}'s voice was steady. Mine still isn't."
        };
        private static readonly string[] FacedDid = { "Told {other} to his face. About time somebody did.", "{other} looked at me like murder. Let him.",
            "{other} went red to the ears when I said it. Never seen that before. Liked it.",
            "Hands steady till {other} walked off. Then they shook so I spilled my coffee.",
            "{other} will want to get even. I'll not walk home the dark way for a while.",
            "Somebody had to tell {other}. Wish it hadn't been me. Glad it was.",
            "The whole street heard me tell {other} to quit. Two men have stood me a drink for it since.",
            "{other} muttered something about my mother as he went. I'll let that go. This once." };
        private static readonly string[] RoundDid = { "{other} drank my health tonight. Worth every cent.", "Stood {other} a drink. Felt like a rich man, for once.", "Stood the saloon a round. {other} thanked me twice.",
            "{other} raised a glass to me across the room. Warmer than the whiskey, that.",
            "My purse is lighter, and {other} grinned at me like I'd paid the rent.",
            "Wonder if {other} will remember who stood the round, come morning. I will.",
            "Coins I'd saved for boots, spent on {other} and the rest. The feet will wait.",
            "{other} slapped my back so hard I spilled half my own glass." };
        private static readonly string[] RoundDone = { "{other} stood me a drink. I'll remember that.", "Drank to {other}'s health. Meant it, too.", "{other} bought me a drink. And everyone else.",
            "Whiskey on {other}'s coin. Went down smoother for being free.",
            "Must stand {other} one back when my pay comes. Won't forget.",
            "{other} bought for the room like a cattle baron. Wonder where the money came from.",
            "First drink I didn't pay for in a month. {other}'s round. Made it last.",
            "{other} looked so pleased to be thanked. Like nobody had thanked {other} in years." };
        private static readonly string[] SettledDid = { "Paid {other} every cent, out in the street where folks could see.", "{other} said it out loud: square. I'll sleep tonight.", "Owe {other} nothing now. Strange, how light a body feels.", "Square with {other}. Folks saw it done.", "{other} counted it twice. Let them. It's all there." };
        private static readonly string[] SettledDone = { "{other} paid me back. Every cent. I'd have bet the other way.", "Counted {other}'s money twice. All there.", "Didn't think I'd see that money again. {other} surprised me.", "{other} owes me nothing now. Strange, after all that.", "Took {other}'s coins in the street. Felt near sorry." };
        private static readonly string[] TownSettled = { "{a} paid {b} back in the street, every cent, with folks watching.", "Heard {a} squared up with {b} in front of half the town.", "{a} paid what was owed. {b} said so out loud. Good for {a}.", "{a} squared up with {b} where the whole street could watch.", "Money changed hands between {a} and {b} today. Square, they say." };
        private static readonly string[] TownRound = { "{a} stood the whole saloon a round. Folks'll be friends with {a} till Sunday.", "{a} bought for the house, {b} says. Wish I'd been there.", "{a} bought the house a drink last night. {b} hasn't stopped saying so.",
            "{b} was singing in the street at midnight. {a} stood the whole room a round, seems.",
            "{a} bought drinks for the house, and {b} put away three on the strength of it.",
            "Heard {a} stood a round. {b} says it was the good bottle, too, not the rotgut.",
            "{a} must have come into money. Stood the saloon a round, {b} says, and paid in silver.",
            "{b} raised a glass to {a} in the street this morning, an empty one. Must have been some round." };
        private static readonly string[] FacedDone = { "{other} put {his} nose in my business. I won't forget it.", "{other}, playing the hero. In front of half the street.",
            "{other} told me off like I was a dog at the table. In front of everybody.",
            "Laughed it off when {other} spoke up. Laughed. Jaw's been tight ever since.",
            "{other} will find out what it costs to shame a man in the street. Not today.",
            "Saw {other} at the store, chin up like a rooster. Let {other} crow. Roosters end in the pot.",
            "Who made {other} the sheriff? Nobody I know of.",
            "Lay awake going over what {other} said. Every word sharper the second time." };

        private static readonly string[] CharityDid =
        {
            "{other} gave me money. Didn't even make me ask twice. There's good in folks yet.",
            "Bread tonight, thanks to {other}. Swore I'd never ask. Swear a lot of things.",
            "{other}'s dollar is folded small in my boot. Keep touching it to be sure.",
            "{other} looked me in the eye, giving it. Didn't toss it. That's the part I keep.",
            "Hot soup tonight for the first time this week. {other}'s coin. Belly's warm.",
            "Stood a long while working up to ask {other}. Ten words. Took me an hour.",
            "Wrote down what {other} gave me on a scrap of paper. So I can pay it back to the cent.",
            "{other} gave and walked on without a sermon. I could have wept for that alone."
        };
        private static readonly string[] CharityDone =
        {
            "Gave {other} a dollar. Felt good, then foolish, then good again.",
            "{other} looked so ashamed to ask. I know that look. Wore it myself, once.",
            "{other}'s hand was cold as a pump handle when I put the coin in it.",
            "Hope {other} spends it on food and not the bottle. Gave it anyhow. Can't follow a coin.",
            "Saw {other} at the bakery after, buying a loaf. Glad. Relieved, too, to tell the truth.",
            "Gave {other} more than I meant. Hand went in the pocket and came out generous.",
            "They'd say at home I'm soft. Gave {other} the coin before I could talk myself out of it.",
            "{other} said God bless. Haven't heard that said to me and meant in a long while."
        };
        private static readonly string[] ScornDid =
        {
            "Asked {other} for a nickel. Got told to work. I'd work, if there was work.",
            "Won't ask {other} for anything again. Won't ask anybody. Rather go hungry.",
            "{other} looked at my hand like it was something stuck to a boot.",
            "Lay hungry all night. Kept hearing {other} say get work, in that voice.",
            "{other} had a gold chain across the vest and couldn't spare a penny. I counted the links.",
            "Asking {other} took everything I had left. Got nothing back but a look.",
            "Should have known better than to ask {other}. That sort never looks down.",
            "{other} said try the church. I'd tried the church. The church said try {other}'s sort."
        };
        private static readonly string[] ScornDone =
        {
            "{other} asked me for money, in the street. The nerve. ...Still thinking about it, though.",
            "Should've given {other} something. A nickel's nothing. Too late now.",
            "Told {other} to find work. Said it the way Pa used to. Didn't like hearing him in my mouth.",
            "If I give to {other}, I'll have every idler in the county at my door.",
            "{other} didn't argue. Just nodded and went. That's what keeps nagging me.",
            "Worked hard for every coin in my pocket. {other} could do the same.",
            "Saw {other} still sitting by the depot at dusk. Went the other way round.",
            "Spent the nickel {other} wanted on tobacco. It didn't taste right."
        };
        private static readonly string[] DrunkDid =
        {
            "Don't remember much past the third glass. Woke up with sawdust in my mouth. Did I call {other} out?",
            "Folks keep grinning at me. Something about {other}, and the floor. I'll not ask.",
            "Head like a split log. Somebody says I called {other} a cheat and fell over my own boots.",
            "{other} has every right to be sore. Don't know what I said, so I can't even be sorry right.",
            "Owe {other} an apology and the saloon a chair, I'm told. Can't afford either.",
            "Saw {other} at the store and couldn't think whether to nod or run. Did neither. Stood there.",
            "Swear off whiskey every time. This time {other} saw me on the floor. This time I mean it.",
            "Bruise on my cheek the shape of a floorboard. {other} never laid a hand on me, they say."
        };
        private static readonly string[] DrunkDone =
        {
            "{other} wanted to fight me. Couldn't find his own feet. Almost felt sorry for him.",
            "Poor {other}. Drink'll do that. Somebody ought to take that bottle away.",
            "{other} squared up, swung at the air and sat down hard. Didn't have to lift a finger.",
            "{other} called me a cheat in front of the room. Drunk talk. Still stung a little.",
            "Helped {other} up after and got a whiskey-breath thank you. Strange night.",
            "{other} came by sober and couldn't look at me. Told him to forget it. He won't.",
            "Everybody laughed at {other} on the floor. I didn't. Saw my father that way too often.",
            "{other} will be back on the stool by Saturday. Some men can't leave it alone."
        };

        // the other one has died since
        private static readonly string[] DeadFoe =
        {
            "Last thing I ever gave {other} was trouble. Now {other}'s dead, and I can't take it back.",
            "{other}'s in the ground, and I'm still angry. Angry at the dead. What does that make me?",
            "Called {other} a cheat in front of the store, the last time. Said it loud, and I can't take it back.",
            "Angry at the dead, that's what I am. {other} took the quarrel into the ground.",
            "Wished {other} gone, more than once. Now the wish has come true, and I can't take it back.",
            "Angry at the dead. Felt it again today, passing the porch where {other} used to sit.",
            "Spat in the road as {other} went by, that last week. The spit's long dry, and I can't take it back.",
            "Angry at the dead, and ashamed of it. {other} never let me win an argument, and now never will."
        };
        private static readonly string[] DeadFriend =
        {
            "{other}'s dead. I keep turning round to tell {other} things.",
            "Can't walk past where {other} used to stand. Go the long way now.",
            "Saved the newspaper for {other}, like always. Then remembered. It's still folded on the chair.",
            "I'd give a month's wages to hear {other} laugh once more. Even at me.",
            "Wore the scarf {other} gave me. Didn't think it through. Had to sit down on the church step.",
            "Nobody else knows how I take my coffee. {other} knew.",
            "Heard a step on the porch and thought it was {other}. It was the wind and a loose board.",
            "Said goodnight to {other} out loud tonight, to the dark. Felt foolish. Did it anyway."
        };
        private static readonly string[] DeadKnown =
        {
            "Talked with {other} only the other day. Now {other}'s gone. That's how quick it is.",
            "{other} nodded to me every morning for years. The street's a nod short now.",
            "Heard {other} was buried Tuesday. Didn't know {other} well. Still took my hat off.",
            "{other} owed me nothing and I owed {other} nothing. Still, the corner's wrong without that face.",
            "Never asked {other} a single real question. Now there's nobody to ask.",
            "{other}'s window is dark tonight. I'd never noticed it lit till now.",
            "{other} wasn't a friend, quite. Wasn't a stranger either. Don't know what to call the hollow.",
            "Signed the book at {other}'s burying. Wrote my name very neat, as if it mattered."
        };

        /// A thought about someone they had a scene with lately, by first name; about their death if they've died since.
        /// He or she for the other person in a line ({he} {him} {his} {himself}; the slots are capitalised when the
        /// line starts with them). The Ties pools said "they" of a named person until the night of 2026-10-28, which read
        /// modern; a person of 1899 is he or she.
        public static string Pronouns(string line, bool woman)
        {
            if (line == null || line.IndexOf('{') < 0)
                return line;
            line = line.Replace("{he}", woman ? "she" : "he").Replace("{him}", woman ? "her" : "him").Replace("{his}", woman ? "her" : "his").Replace("{himself}", woman ? "herself" : "himself");
            return line.Replace("{He}", woman ? "She" : "He").Replace("{His}", woman ? "Her" : "His");
        }

        /// Whether the other end of a tie is a woman, by their first name when nothing better is known (the tie carries
        /// names, not sex; the add-on's and the scenes' women's names are known lists).
        public static bool OtherIsWoman(SceneTie t, string id)
        {
            string name = t != null ? t.Other(id) : null;
            if (name == null)
                return false;
            string first = Person.FirstOf(name);
            return Residents.IsWomansName(first) || Scenes.IsWomansName(first);
        }

        public static string Thought(SceneTie t, string id, string seed, bool otherDead = false, bool? otherWoman = null)
        {
            if (otherDead)
                return Pronouns(Voice.Pick(seed + "tdead", t.Warmth < 0 ? DeadFoe : t.Warmth > 0 ? DeadFriend : DeadKnown).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
            if (t.Kind == "fencecut")   // the one who blames, and the one blamed (Core/FenceCutters.cs; FIVEDAY item 31)
                return t.Did(id) ? FenceCutters.AccuserThought(Person.FirstOf(t.Other(id)), seed) : FenceCutters.AccusedThought(Person.FirstOf(t.Other(id)), seed);
            if (t.Kind == "avoid" && t.Doer != null)
                return Pronouns(Voice.Pick(seed + "avoid", t.Did(id) ? AvoidDid : AvoidDone).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
            if (Trouble(t.Kind) && t.Doer != null)
            {
                bool did = t.Did(id);
                string[] trouble = t.Kind == "rough" ? (did ? RoughDid : RoughDone) : t.Kind == "lift" ? (did ? LiftDid : LiftDone)
                                 : t.Kind == "pickpocket" ? (did ? CaughtDid : CaughtDone) : t.Kind == "charity" ? (did ? CharityDid : CharityDone)
                                 : t.Kind == "scorn" ? (did ? ScornDid : ScornDone) : t.Kind == "help" ? (did ? HelpDid : HelpDone)
                                 : t.Kind == "passby" ? (did ? PassDid : PassDone) : t.Kind == "defend" ? (did ? DefendDid : DefendDone)
                                 : t.Kind == "faced" ? (did ? FacedDid : FacedDone) : t.Kind == "round" ? (did ? RoundDid : RoundDone) : t.Kind == "settled" ? (did ? SettledDid : SettledDone) : (did ? DrunkDid : DrunkDone);
                return Pronouns(Voice.Pick(seed + "trouble", trouble).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
            }
            string[] pool = t.Kind == "fright" ? Fright : t.Kind == "bout" ? Bout : t.Kind == "brawl" ? Blows : t.Kind == "debt" ? Money : t.Kind == "makeup" ? MadeUp : t.Kind == "brushoff" ? Shrugged
                          : t.Kind == "comfort" || t.Kind == "cheer" || t.Kind == "apology" ? Kindness : t.Warmth < 0 ? Quarrel : Romantic(t.Kind) ? Sweet : Known;
            return Pronouns(Voice.Pick(seed + "tie", pool).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
        }

        // ---- what the town says ---------------------------------------------------------------------------

        private static readonly string[] TownHostile =
        {
            "{a} and {b} were shouting at each other in the street. Half of {town} heard it.",
            "{a} won't stand in the same room as {b} now. Ask either one why, you get a different story.",
            "Loud words between {a} and {b} outside the post office. The postmaster shut his window on them.",
            "Heard {a} call {b} a liar across the street. Loud. Wagons stopped.",
            "{b} came into the store white with temper. {a} again, I'd wager.",
            "{a} and {b} aren't speaking. Their families aren't either. That's how you know it's serious.",
            "Whatever's between {a} and {b} has half of {town} taking sides already.",
            "{a} spat when {b}'s name came up at the barber's. Just spat, and said nothing."
        };

        private static readonly string[] TownBlows =
        {
            "{a} and {b} came to blows. Over nothing, they say. It's never nothing.",
            "Somebody pulled {a} off {b} before it got worse. Blood on the boardwalk.",
            "{a} and {b} rolled right under a wagon, fighting. The horses never even twitched.",
            "{b}'s got a lip like a plum, and {a} can't close one hand. Some fight, that was.",
            "Saw {a} and {b} slugging it out by the trough. Neither one's any good at it.",
            "{a} and {b} went at it in the street. The barber came out with lather on his razor to watch.",
            "{a} knocked {b} into the hitching rail, they say. Then {b} knocked {a} into it back.",
            "The barbershop has money on whether {a} and {b} go another round by Sunday."
        };

        // {a} did it, {b} had it done to him
        private static readonly string[] TownRough =
        {
            "{a} grabbed {b} by the collar and put {b}'s face through the bar top. Barkeep's still picking up teeth.",
            "Did you see what {a} did to {b}? Never seen a man move that fast with his hands full.",
            "{a} put {b}'s head down on the bar like a man ringing a bell. Whole saloon heard it.",
            "{a} slammed {b} into the bar and went back to drinking. Didn't even spill.",
            "They say {a} bloodied {b}'s nose on the bar for one wrong word. One.",
            "{a} had {b} by the scruff before anybody could blink. The bar top's got a dent in it now.",
            "Since {a} did that to {b} at the bar, nobody leans on that end.",
            "Heard {a} roughed up {b} at the saloon. {b} hasn't shown that face in there since."
        };
        private static readonly string[] TownCaught =
        {
            "{b} caught {a} with a hand in a coat pocket. In the open street. Some nerve.",
            "Watch your pockets round {a}. {b} found out the hard way.",
            "{b} turned round and found {a}'s fingers in a coat pocket. Grabbed the wrist and hollered.",
            "Heard {b} chased {a} two streets over a purse. Didn't catch {a}, but everybody saw the face.",
            "{a}'s a thief, {b} says. Caught at it on the boardwalk in the noon crowd.",
            "The storekeeper keeps one eye on {a} since {b} caught those fingers.",
            "{b} says {a} smiled while being caught. Smiled. I'll button my coat.",
            "Everybody's patting their pockets round {a} since {b} raised the cry."
        };
        private static readonly string[] TownDrunk =
        {
            "{a} called {b} out, drunk as a lord, and went down like a felled tree before a fist was raised.",
            "{a} wanted to fight {b}. Fought the floor instead. The floor won.",
            "{a} came at {b} on whiskey legs and swung at a post. Missed the post.",
            "Heard {a} called {b} every name there is, then fell asleep sitting up.",
            "{b} only stepped aside, and {a} went straight past into the spittoon.",
            "{a} was so far gone, {b} got called out by the wrong name. Twice.",
            "{b} sat {a} down on a bench and fetched water. Better than {a} deserved, maybe.",
            "{a} wanted to fight {b} last night. This morning {a} can't recall {b}'s face."
        };

        private static readonly string[] TownCharity =
        {
            "{b} gave {a} money in the street, plain as day. Folks will be lining up now.",
            "Saw {b} press a coin on {a}. Didn't think {b} had it in them.",
            "{a} asked {b} for help and got a dollar. A whole dollar, they say.",
            "{b} gave {a} a coin and didn't make a speech about it. Rare.",
            "Saw {a} come out of the bakery with a loaf. {b}'s money, I'd guess.",
            "{b} put money in {a}'s hand on the corner. Half the street watched, and none of us did the same.",
            "Heard {b} gave {a} the price of a meal. Some will call it soft. Wish I'd done it.",
            "{a} thanked {b} three times in the street. Three. Must have been the first kindness in a while."
        };
        private static readonly string[] TownScorn =
        {
            "{a} asked {b} for a nickel and got sent off like a stray dog.",
            "{b} wouldn't spare {a} a penny. And {b} with a gold watch chain.",
            "{a} held a hand out to {b} on the corner. {b} looked straight over it.",
            "Heard {b} told {a} to find work. Where, {b} didn't say.",
            "{b} turned {a} away at the door. Then sat down to a big supper, I'd bet.",
            "{a} asked {b} for the price of bread and got a lecture on idleness.",
            "Saw {a} walk off from {b} with eyes on the ground. Cost {b} nothing to be civil.",
            "{b} says {a} would only drink it. Maybe. {a} looked hungry, not thirsty."
        };

        private static readonly string[] TownHelp =
        {
            "{b} took a fall in the street and {a} was there before anybody. Helped {b} right up.",
            "Saw {a} pick {b} up out of the dirt. Dusted {b} off and all.",
            "{b} went down hard by the trough, and {a} had a hand out before the water stopped slopping.",
            "{a} walked {b} home after the fall, slow, arm in arm. Good to see.",
            "Heard {a} carried {b}'s parcels home after that tumble. Didn't have to.",
            "{a} stopped a wagon to help {b} up out of the road. Driver cursed. {a} paid it no mind.",
            "{b} fell, and it was {a}, of all people, who stopped.",
            "{a} got down in the dust for {b} without a thought for the good trousers."
        };
        private static readonly string[] TownPassby =
        {
            "{b} fell flat in the street and {a} walked right past. Folks noticed.",
            "{a} stepped round {b} like a dropped hat. With half the street watching.",
            "{b} lay in the road a good minute while {a} walked by looking at the sky.",
            "{b} fell right in front of {a}. {a} stepped over and kept on to the bank.",
            "{b} was down in the mud, and {a} never broke stride. Plenty saw it.",
            "{b} says {a} looked straight down and kept walking. {b} won't forget it, nor will I.",
            "{b} took a tumble outside the store, and {a} just tipped a hat and walked on.",
            "{b} got up alone, with {a} not ten feet off and walking away. Folk were sour about it all day."
        };

        private static readonly string[] TownWarm =
        {
            "{a} has been seen walking with {b}. Folks talk. Folks always talk.",
            "{a} and {b}, stood close as two spoons. Give it a month.",
            "{a} bought ribbon on Friday. Sunday, {b} wore it. The whole pew noticed.",
            "Saw {a} and {b} by the creek at dusk, talking low. Neither one saw me.",
            "{b} blushed to the ears when {a}'s name came up at the well.",
            "{a} has taken to shaving every morning. {b}'s the reason, the barber says.",
            "{a} and {b} sat through the whole sermon holding hands under a hymnbook.",
            "Wagers at the saloon say {a} and {b} will be married by harvest."
        };

        private static readonly string[] TownMadeUp =
        {
            "{a} and {b} made it up, would you credit it. Shook on it outside the store.",
            "{a} and {b} are on speaking terms again. Town's lost a good quarrel.",
            "Saw {a} and {b} share a bench outside the store. A month ago they'd have shared a cell.",
            "{a} bought {b} a drink at the saloon. After all that. The barkeep nearly dropped the bottle.",
            "{a} and {b} walked to church together Sunday. Folk nudged each other the whole service.",
            "Heard {a} said sorry to {b} first. Never thought {a} would bend.",
            "{a} and {b} laughing together at the feed store. Laughing! I had to look twice.",
            "The quarrel between {a} and {b} is done, they say. The barber will miss it most."
        };

        // one of the two has died since: {a} is the one left, {b} the dead ({af}, {bf}: first names, said again)
        private static readonly string[] TownDeadFoe =
        {
            "Everybody knew {a} couldn't stand {b}. Now {bf}'s dead, and folks are careful what they say about it.",
            "They say {b} never got over that business with {a}. Now {bf}'s buried, and {af} won't speak of it.",
            "{a} and {b} hadn't a kind word between them in years, and now {bf}'s dead. {af} went to the burying.",
            "Years of feuding with {b}, and now {bf}'s dead. Folk watched {a} at the graveside.",
            "{a} quarrelled with {b} every market day. Now {bf}'s buried, and {af} stands alone at the stall.",
            "{a} and {b} fought over a fence line for years, and now {bf}'s dead. The fence is still standing.",
            "{a} took a hat off as the coffin passed, they say. Feuded with {b} for years. Now {bf}'s buried.",
            "Wonder if {a} sleeps easier now {bf}'s in the ground. {b} gave {af} no peace for years."
        };
        private static readonly string[] TownDeadWarm =
        {
            "{a} was sweet on {b}, and now {bf}'s gone. Somebody ought to look in on {af}.",
            "{a} and {b} were walking out, and now {bf}'s in the churchyard. {af} hasn't been seen since the burying.",
            "Folk were picking a wedding hat for {a} and {b}. Now it's a black one, for {bf}.",
            "{a} put wildflowers on {b}'s grave again this morning. Every morning, they say.",
            "{a} and {b} were all but promised, and now {bf}'s gone. {af} sits on the porch till dark.",
            "Saw {a} at the store fingering ribbon in {b}'s colour. Didn't buy it. Just held it.",
            "{a} was courting {b} all spring, and now {bf}'s gone. Who's to tell {af} to eat?",
            "{a} still walks the creek path where {b} and {af} used to go. Alone, now."
        };
        private static readonly string[] TownDeadMended =
        {
            "{a} and {b} only just made it up, and now {bf}'s gone. Glad they got to, I suppose.",
            "{a} and {b} shook hands at last, and a week later {bf} was gone. The handshake's what {af} has left.",
            "{a} buried {b} a friend. Six months back they weren't speaking. Strange, the timing.",
            "Good thing {a} and {b} made peace in time. {af} helped carry the coffin and never once looked away.",
            "{a} and {b} were friends again a bare month before {bf} died. A month of it, at least.",
            "Heard {a} wept at {b}'s burying. A year ago {af} wouldn't cross the road for {bf}.",
            "{a} keeps saying it was lucky, making up with {b} while there was still time. Keeps saying it.",
            "Since {b} died, {a} talks of nothing but the day they shook hands. Glad of it, and grieving both."
        };

        /// The town's talk about a tie between two others, with both their full names; deadA/deadB: that one has died since.
        public static string TownThought(SceneTie t, string town, string seed, bool deadA = false, bool deadB = false)
        {
            if (deadA || deadB)
            {
                string left = deadB ? t.AName : t.BName, gone = deadB ? t.BName : t.AName;
                string[] dead = t.Kind == "makeup" ? TownDeadMended : Romantic(t.Kind) ? TownDeadWarm : TownDeadFoe;
                return Voice.Pick(seed + "ttd", dead).Replace("{af}", Person.FirstOf(left)).Replace("{bf}", Person.FirstOf(gone)).Replace("{a}", left).Replace("{b}", gone);
            }
            if (t.Kind == "fencecut")   // (FIVEDAY item 31) {a} the one who blames
                return FenceCutters.TownLine(t.Doer == t.B ? t.BName : t.AName, t.Doer == t.B ? t.AName : t.BName, seed);
            string[] pool = t.Kind == "brawl" ? TownBlows : t.Kind == "makeup" ? TownMadeUp : Romantic(t.Kind) ? TownWarm
                          : t.Kind == "rough" ? TownRough : t.Kind == "pickpocket" ? TownCaught : t.Kind == "drunk" ? TownDrunk
                          : t.Kind == "charity" ? TownCharity : t.Kind == "scorn" ? TownScorn : t.Kind == "help" ? TownHelp : t.Kind == "passby" ? TownPassby : t.Kind == "round" ? TownRound : t.Kind == "settled" ? TownSettled : TownHostile;
            // in the trouble lines {a} did it: the one who did, whichever way round the tie was first made
            bool swap = Trouble(t.Kind) && t.Doer != null && t.Doer == t.B;
            string an = swap ? t.BName : t.AName, bn = swap ? t.AName : t.BName;
            return Voice.Pick(seed + "tt", pool).Replace("{a}", an).Replace("{b}", bn).Replace("{town}", town ?? "town");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return Quarrel.Concat(Blows).Concat(Money).Concat(Sweet).Concat(Known).Concat(Kindness).Concat(MadeUp).Concat(Shrugged)
                .Concat(RoughDid).Concat(RoughDone).Concat(LiftDid).Concat(LiftDone).Concat(CaughtDid).Concat(CaughtDone).Concat(DrunkDid).Concat(DrunkDone).Concat(AvoidDid).Concat(AvoidDone)
                .Concat(CharityDid).Concat(CharityDone).Concat(ScornDid).Concat(ScornDone).Concat(TownCharity).Concat(TownScorn)
                .Concat(TownRough).Concat(TownCaught).Concat(TownDrunk).Concat(HelpDid).Concat(HelpDone).Concat(PassDid).Concat(PassDone).Concat(TownHelp).Concat(TownPassby).Concat(DefendDid).Concat(DefendDone).Concat(FacedDid).Concat(FacedDone).Concat(RoundDid).Concat(RoundDone).Concat(TownRound).Concat(SettledDid).Concat(SettledDone).Concat(TownSettled)
                .Concat(DeadFoe).Concat(DeadFriend).Concat(DeadKnown)
                .Concat(TownHostile).Concat(TownBlows).Concat(TownWarm).Concat(TownMadeUp)
                .Concat(TownDeadFoe).Concat(TownDeadWarm).Concat(TownDeadMended);
        }
    }
}
