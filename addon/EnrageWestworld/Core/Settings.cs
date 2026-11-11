// RealNPCs.ini. Read by field name (key = value ; comment), so a new option only needs a field
// here and a line in ToIni(). Older files are upgraded in place: values kept, new options added.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace EnrageWestworld.Core
{
    public sealed class Settings
    {
        public const int CurrentVersion = 451;

        public int SettingsVersion = CurrentVersion;

        // [Features]
        public bool Conversations = true;
        public bool PanelShowsAll;   // the settings panel shows every setting and the Testing page; off: the essentials (Lex, 2026-10-30; settings version 123)
        public bool ResetAtClerk = true;   // "Reset RealNPCs Memory" in a station clerk's list (Lex, 2026-10-30; settings version 121)
        public bool Remarks = true;
        public bool Thoughts = true;
        public bool DiaryDreams = true;   // [Mind] with Diaries: a strong hard memory written down comes back a night or two later as a dream in the diary, half the time (the roadmap's idea bank; settings version 364)
        public bool Diaries = true;
        public bool WordOfMouth = true;
        // word of a killing to the dead man's kin (Lex, 2026-10-31; settings version 162): game days to the telegram when
        // someone saw it (was 12-60 game hours), and when nobody did or it was self-defence (sooner once the body is found)
        public int KinWordDaysMin = 2;
        public int KinWordDaysMax = 5;
        public int UnseenKinWordDaysMin = 5;
        public int UnseenKinWordDaysMax = 10;
        public bool LifeUpdates = true;
        public bool RevealToEnrage = true;

        // [Romance]
        public bool Romance = true;
        public bool FlirtPrompt = true;
        public string TalkKey = "Q";   // Talk while locked on to someone you know: Q is the game's own lock-on target-info control (rebindable in its Controls menu, its icon follows); any other key is read directly (settings version 164)
        public bool InteractionMenu = true;   // Talk (Q; a pad's RB) in the lock-on menu opens a short list: the news, your name, their worry asked after, a drink, money, a debt to settle, an apology, a warning (settings version 148; NIGHT3 item 4)
        public bool LawClearsBodies = true;   // a lawman carries a body off the town street (roadmap 33; settings version 150; NIGHT3 item 6)
        public bool BodyToUndertaker = true;  // ...and to the undertaker's door or the jail door when one is within 80 m, with someone who knew the dead walking behind and standing over them after (Core/Undertaker.cs; SIXDAY item 18; settings version 234)
        public bool PersonalSpace = true;   // someone he stands right up against for a couple of seconds minds it: steps back, asks for room, or stands their ground (NIGHT3; settings version 154; plays through SightReactions)
        public bool ArthurSpeaks = true;   // Arthur says a recorded line of his own when someone comes at him or he catches the pickpocket (roadmap 34; settings version 151; NIGHT3 item 7)
        public bool YieldToDying = true;   // nothing of RealNPCs' on anyone dying: no line, no fall, no move or clip of ours on someone the game has dying, so a death-animations mod's deaths play as they are (Game/Natives.cs Dying/Ragdoll, Speech.Gone, the Director; roadmap 29; SIXDAY item 27; settings version 242)
        public bool NpcFlirting = true;
        public bool Trysts = true;
        public bool CasualHookupsDecides = true;
        public int WillingSingle = 70;
        public int WillingTaken = 20;
        public int TrystPrice = 2;           // [Romance] what one of the saloon's women asks for the night, in dollars (0: nothing asked; Core/Trysts.cs; SIXDAY item 15; settings version 231)
        public bool TrystsAfter = true;      // [Romance] the days after a night: her thought the morning after, a sweetheart's word when you've been gone (and her cooling at a fortnight), a husband who hears of it and comes as the kin do (Core/Engine.Trysts.cs; roadmap 26; SIXDAY item 15)
        public bool NpcRomance = true;
        public bool AmbientFlirting = true;
        public bool SameSexRomance;
        public bool RomanceAffectsEnrage = true;

        // [Ticker]
        public bool Ticker = true;
        public float TickerNearbyMetres = 20f;
        public int TickerSeconds = 12;
        public float TickerY = 0.962f;
        public float TickerScale = 0.30f;
        public float TickerWidth = 0.60f;
        public string TickerNextKey = "T";
        public string SpotKey = "K";          // [Keys] records where you stand as a named place of the town (platform, wharf, cemetery...) into spots.txt; Shift and the key change the kind (Core/Spots.cs; SIXDAY item 11a; settings version 221)
        public string EventKey = "J";         // [Keys] the trial's, the gallows' and the show's menus while their prompt shows at the foot of the screen (Core/Trials.cs, Core/Shows.cs; settings version 224)
        public bool ThoughtSounds = true;
        public bool SceneCues = true;             // [Ticker] a line on the ticker when something starts up at your back within 22 m ("Raised voices behind you."), at most once a minute (Core/Overheard.cs; SIXDAY item 7; settings version 214)
        public int TickerGapSeconds = 3;
        public bool ThoughtMarker = true;
        public bool ThoughtsOverHeads = true;
        public float ThoughtsOverHeadScale = 0.24f;
        public bool HistoryNotes = true;   // [Ticker] when someone near thinks of 1899's (or 1907's) real news, the paper's own sentence of what happened in the news box; once a story, every 90 s at most (Lex, 2026-11-09: a historical education toggle; settings version 367)
        public bool ThoughtsFollowOn = true;      // [Ticker] someone's next thoughts pick up from what just happened to them, and with the AI model on from their last thought (Core/ThoughtThreads.cs; FIVEDAY2 item 27; settings version 330)
        public bool RoadThoughts = true;          // [Ticker] thoughts from those you pass on the road, by how they're travelling and what they make of you, and by their part at a scene (Core/RoadThoughts.cs; FIVEDAY2 item 26; settings version 329)
        public float RoadThoughtsMetres = 35f;    // ... riders and drivers this far off, out of town
        public bool Asides = true;                // [Ticker] while someone near you talks, whoever's listening may think something about it, over their head (Core/Asides.cs; FIVEDAY2 item 25; settings version 328)
        public bool WeatherChecked = true;   // [Ticker] what people think and say about the weather is held to what's really out, the temperature too (Core/WeatherTruth.cs; a player's report, 2026-11-09; settings version 342)
        public bool WeatherTalk = true;      // [Ticker] with WeatherChecked: people remark on the weather more the worse it is, and for a couple of minutes after it turns
        public bool Opinions = true;   // [Ticker] with PairThoughts: a view of each other read off the tie (respect, contempt, envy, fear), one-sided, in their thoughts when they meet (the roadmap's idea bank; settings version 365)
        public bool PairThoughts = true;   // [Ticker] two in view with a history between them each think about the other, at the same moment, in the boxes over their heads (TENHOUR item 14; settings version 345)
        public int ThoughtsAtOnce = 3;            // [Ticker] how many people's thoughts may be up over their heads at once (FIVEDAY2 item 24; settings version 327); 1: singly, as it was
        public bool TickerShowsScene = true;   // the bar names the street scene playing out near you, while it plays (Lex, 2026-10-30; settings version 122)
        public bool ScenesOnTicker = true;        // a street scene's lines on the ticker while it plays within ScenesOnTickerMetres (2026-10-29, Lex)
        public float ScenesOnTickerMetres = 12f;   // was 30 before settings version 120 (Lex, 2026-10-30: a mourner's words subtitled from 26 m away)
        public bool ShareWithEnrage = true;
        public bool Minds = true;
        public bool RecordMinds = true;
        public bool RecordFates = true;
        public bool MindShadow = true;    // the NPC mind (daytime 2026-10-27): works out traits, needs, feelings and mood and only reports them
        public bool KeepersBackSeen = true;   // [Mind] a counter's keeper shot down and back bandaged: those who saw the fall stare at them, with a thought (THREEHOUR R3; settings version 363)
        public bool ThreadsNextDay = true;   // [Ticker] with ThoughtsFollowOn: a resident's loud moment kept in the save; their first quiet thought the next game day may pick it up (THREEHOUR R2; settings version 362)
        public bool OnlookerPicks = true;   // [Mind] with the model's picks: at a fight or a quarrel, the strongest onlooker's own act is the model's pick from what the game plays; late, the rules' (THREEHOUR R1; settings version 361)
        public bool PlacesRemember = true;   // [Mind] memory that behaves like memory (Core/PlaceMemory.cs; roadmap 4; SIXDAY item 24): someone who knew one you killed thinks of it near the spot (a friend's for good, an acquaintance's for a month); people who've seen you on your horse know the horse before the face as you ride up; the same place at the same hour on three different days is a habit the town notices (settings version 240)
        public bool HeldStayInMind = true;   // [Mind] someone the ring is walking stays in mind to the ring's own leash, not dropped at the mind's 25 m edge (Lex, 2026-11-09; settings version 369)
        public bool MindLive = true;      // what the mind decides reaches the screen (Lex, 2026-10-30: on for everyone from 0.7.2; off until settings version 126)
        public bool LookScheduler = true;   // [Mind] the background looks (fever, posters, the calendar and two dozen more) run at most two a frame, the most overdue first, so they never pile onto one frame (Core/LookScheduler.cs; DEPTH item 4; settings version 412); off: each on its own timer, as before
        public bool MindLevels = false;   // the shadow's scan run from the levels scheduler (night 2026-10-28, item 7; settings version 89): a near ring in view, the town around every 10-30 s, the people away one time a game day, a budget of work a frame; off, the scan as it was
        public bool QuarrelLadder = true;  // with MindLive: an argue or a brawl between two people the mind knows is played from the quarrel ladder (Core/Escalation.cs, Scenes.Ladder.cs; night 2026-10-28, item 16; settings version 92): how far it goes and how it ends from their traits and the surroundings; the shot is never played
        public bool WalkTogether = true;     // [Mind] two friends (or a courting couple) whose talk plays to its end may walk on together, side by side: to the saloon after work, homeward at dusk, a Sunday stroll; two pairs a town at once (FOURTEENHOUR item 15; settings version 383)
        public bool QuarrelPolitics = true;  // [Mind] with QuarrelSlights: two in drink on opposite sides of 1899's quarrels (free silver, the trusts, the Philippines, the dry vote) may come to blows over it, onlookers on each side lean to their man, and the town retells it by what it was over (FOURTEENHOUR item 9; settings version 377)
        public bool QuarrelSlights = true;   // [Mind] with QuarrelLadder: a quarrel "over nothing" says what it is over (a shoulder in passing, a spilled drink, a bad week, the old business between foes); words and the log only, never the ladder's odds (FOURTEENHOUR item 1; settings version 371)
        public bool BrawlShape = true;       // [Mind] with MindBrawls: a big brawl (six or more) has its beats: the barkeep's shout from his post, the timid and the women slipping out, the room scattering at a gun, and from near its end the beaten-looking quitting so it thins (FOURTEENHOUR item 10; settings version 378)
        public bool BrawlStrays = true;      // [Mind] with MindBrawls: someone at arm's length of the fighting may take a stray blow (the hot swing back and are in it, the timid back off, a woman cries out and leaves), and a friend of a man losing badly may pull him out of it (FOURTEENHOUR item 8; settings version 376)
        public bool BrawlCascade = true;     // [Mind] with MindBrawls: an onlooker with no stake joins only once enough are at it for a man of his nature (low for the hot, the reckless, the drunk), so most fights stay small and a few tip into a brawl (Granovetter's thresholds; FOURTEENHOUR item 7; settings version 375)
        public bool BrawlSides = true;       // [Mind] with MindBrawls: onlookers lean to their own in a fight (kin, a man of their trade, the town's own against an outsider), those sat or leaning get up for a fight of two when they have a stake in it, and the well-off stay out (FOURTEENHOUR item 6; settings version 374)
        public bool WitnessesFetchLaw = true;   // [Mind] with no lawman come to a brawl, an onlooker (the well-off first) runs for one within 150 m; after a beating, a witness fetches one who has a word (FOURTEENHOUR item 21; settings version 389)
        public bool BrawlLawSight = true;    // [Mind] with MindBrawls: the law holds joiners back by sight and distance (in sight within 12 m as before, farther half as much, out of sight a little), a lawman may let a fair fight run, one alone against six or more sends for help, and "Law!" is cried as he comes (FOURTEENHOUR item 5; settings version 373)
        public bool FightHoldStart = true;   // [Mind] two told to fight keep the order until the first blow (the game's own alarms held off them, the order kept), then the game's fists as before; off: the alarms come back the moment they're told (FOURTEENHOUR item 1; settings version 371)
        public bool MemoriesBlur = true;     // [Mind] a big brawl leaves a tale in the town that grows in the telling (more men, a knife, the night), truest from those who were in it (FOURTEENHOUR item 24; settings version 392)
        public bool OpinionsAct = true;      // [Mind] what people think of each other decides things: fear keeps clear and out of a brawl, contempt picks a side, respect walks together, and what's said behind backs is the teller's view (FOURTEENHOUR item 23; settings version 391)
        public bool ControlCensus = true;    // [Mind] every 3 s who holds each person within 25 m, named or not (the story, the game's encounters, ours, enRAGE's, another script's, the game's own business, free); a CONTROL line in the log a minute (FOURTEENHOUR item 22; settings version 390)
        public bool HandBackLent = true;     // [Mind] what we set on people not ours to make them fight (fight even unarmed, always fight, the hostile group) is put back once they've been out of any fight 20 s and nothing of ours has them (FOURTEENHOUR item 3; settings version 372)
        public bool PaydayNight = true;      // [Scenes] with Payday: Saturday night after eight, the paid hands stay in town and in the saloon, its fights come easier and grow faster, and the town says which night it is (FOURTEENHOUR item 13; settings version 381)
        public bool BrawlAfter = true;       // [Scenes] with MindLive: the week after a brawl: the winner of a saloon's fight stands a round; a big one has the town's news with its numbers, the barkeep's broken chairs (J at the bar), the men of opposite sides sore at each other; the law's prisoner is locked up overnight and his wife or mother waits at the jail door at dawn (J pays his fine) (FOURTEENHOUR item 12; settings version 380)
        public bool BrawlVerbs = true;       // [Scenes] with MindBrawls: J in a fight near you: break it up (heeded by their natures and what they think of you), stand with the man losing (the one beating him turns on you), or take a waiting onlooker's bet (FOURTEENHOUR item 11; settings version 379)
        public bool PurseSnatch = true;      // [Scenes] at a thief's snatch a man near may give chase and bring him down; your own catch asks on J what's done with him (FOURTEENHOUR item 20; settings version 388)
        public bool Rivals = true;           // [Scenes] two men courting the same woman have it out (words, blows, or one walks away), and then she chooses (FOURTEENHOUR item 19; settings version 387)
        public bool StoreHoldups = true;     // [Scenes] by day a man holds up a customer outside the store near you; J: talk him down (FOURTEENHOUR reserve R1; settings version 395)
        public int HoldupChance = 15;        // [Scenes] with StoreHoldups: percent, the day's roll once a robber and a customer fit
        public bool HoldupShots = false;     // [Scenes] with StoreHoldups: now and then he fires at one who bolts (someone can die on screen; off as shipped)
        public bool RainShelter = true;      // [Scenes] the town keeps a record of the rain: in it the street's errands wait, strangers under one roof get acquainted, and rain days end a farmer's drought (FOURTEENHOUR item 26; settings version 394)
        public bool CenturyTurns = true;   // [World] New Year's Eve of 1899 and 1900: two who stop together argue whether the century turns at midnight; the turn of the year on the ticker (Core/CenturyTurns.cs; DEPTH reserve R6; settings version 426)
        public bool MinersMeet = true;   // [Scenes] at Annesburg after the shift, the working men already standing together talk of the scrip and the weights and take a vote, two of the company's men watching; captions only (Core/MinersMeet.cs; DEPTH reserve R7; settings version 427)
        public bool Grumble = true;   // [Scenes] an old hand and a friend of his talk over the greenhorn off the train where he can hear; he answers dry, bristles, or goes (Core/Grumble.cs; DEPTH reserve R5; settings version 425)
        public bool PostReward = true;   // [Scenes] at the jail door, J to put five dollars with the sheriff for whoever robbed someone you know; a regular brings the thief in, or claims it falsely, or nobody does (Core/YourReward.cs; DEPTH reserve R4; settings version 424)
        public bool SpreadRumour = true;   // [Scenes] in a saloon, J at a man who likes to talk: a word in his ear about a townsman you know; the town's tales carry it and it grows, and he hears of it (Core/Slander.cs; DEPTH reserve R3; settings version 423)
        public bool NoteMeet = true;   // [Scenes] after a word you carried between two who fell out, the next time they're near each other (two game days) they meet: a hand if it was taken, a cold word if not (Core/NoteMeet.cs; DEPTH reserve R2; settings version 422)
        public bool GraveNight = true;   // [World] with Mourning: the night after a burial one of the dead one's own sits up with the grave, and speaks of them if you come near (Core/Mourning.cs; DEPTH reserve R1; settings version 421)
        public bool SleepOnIt = true;   // [Mind] overnight, a quarrel nothing fed the day before eases a step: sore to cool, cool to nothing; one kept alive by a rematch or a word doesn't (Core/SleepOnIt.cs; DEPTH item 19; settings version 420)
        public bool FeudArcs = true;   // [Mind] where a feud would come to blows, the one who began it may offer his hand before the street: taken, they make it up; refused, blows (Core/FeudArcs.cs; DEPTH item 18; settings version 419)
        public bool Familiarity = true;   // [Mind] two of the town seen about the same place three times a game day, an hour apart, are acquainted: they nod in passing, and in a brawl one leans to the other (Core/Familiarity.cs; DEPTH item 17; settings version 418)
        public bool SessionClock = true;   // [World] the arcs you're meant to watch end within a session: two game days at most (a vouched man's week, the town's tales, a kin's visit, the judge, a loan, a sweetheart's patience...); off: every old length (Core/SessionClock.cs; DEPTH item 16; settings version 417)
        public bool RevivedSeen = true;   // [World] with West World and MindWakes: the revived dead are known by anyone who knew them, not only those who saw them die; each thinks it, and the brave ask them to their face (Core/RevivedSeen.cs; DEPTH item 15; settings version 416)
        public bool CollectionPlate = true;   // [Scenes] on a Sunday after the service, by a town's church, one of its churchmen brings the plate round to you: J a dime, a dollar, or let it pass (Core/CollectionPlate.cs; DEPTH item 11; settings version 415)
        public bool Brands = true;   // [Scenes] a ranch man of a ranch town, stock gone with the brands run over, blames his neighbour in the street; if you came on the rustlers' fire lately, J to tell him who it was (Core/Brands.cs; DEPTH item 8; settings version 414)
        public bool StepBetween = true;   // [Scenes] J when two men with a quarrel between them are about to have it out near you: step between them; heeded by both, it's put off six game hours, and the second time they make it up (Core/StepBetween.cs; DEPTH item 7; settings version 413)
        public bool TemperanceDoor = true;   // [Scenes] at dusk a churchwoman puts the pledge to a drinker at the saloon door, a second woman beside her; a hothead's insult brings her kin; a pledge is the town's talk and the preacher's (FOURTEENHOUR item 25; settings version 393)
        public bool Snubs = true;            // [Scenes] a gentleman snubs a working man in the street; his lady's word ends it or makes it worse; alone, it can come to blows (FOURTEENHOUR item 18; settings version 386)
        public bool OldSoldiers = true;      // [Scenes] old soldiers of either side argue the war or remember it, and a young man home from Cuba or the islands tells his; with [World] HistoricalAccuracy (FOURTEENHOUR item 17; settings version 385)
        public bool Greenhorn = true;        // [Scenes] a regular ribs a greenhorn off the train (his first two weeks), who laughs it off and is welcomed, squares up to him, or takes it and goes (FOURTEENHOUR item 16; settings version 384)
        public bool StreetMore = true;       // [Scenes] the street's own business beside its fights: a horse sold at the hitching post (the buyer may come off the worse, a grudge with a cause), a bad tooth pulled in the street, a stranger asking the way (FOURTEENHOUR item 14; settings version 382)
        public bool MeetWalkAgain = true;    // [Scenes] a street scene's walker who gets no nearer in 3 s is told again, and the one waiting in a seat or at work gets up to meet them (FOURTEENHOUR item 1; settings version 371)
        public bool SaidLately = true;   // the street's talk avoids lines said in the last ~300 (the no-model run, item 5; settings version 183)
        public bool WrittenWakes = true;   // [Mind] with MindWakes: a rare written wondering in someone with cause below the second stage chips "things are as they seem" as the model's does (the no-model run, item 10); settings version 188
        public bool EventfulOnly = true;   // [Scenes] street scenes of nothing but talk (small talk, gossip, a song, a haggle...) and the quiet talks beside them are passed over (Lex, 2026-11-01); settings version 185
        public bool ThoughtVariety = true;   // no two people near you think the same frame within an hour, and a source that gave the last few thoughts gives way to the others (Lex, 2026-10-29; settings version 93; Engine.Minds.cs)
        public bool MindStandUp = true;      // with MindLive: who steps into a rough or a brawl between two others comes from the onlookers' own minds, and whether the bully backs down from his (Core/StepIn.cs; roadmap 39; night 2026-10-29, item 2; settings version 107); off, the old roll
        public bool MindPlans = true;   // with MindLive: plans over days, from a wrong done them, a kindness, a debt, a sweetheart; the holder seeks the other out and the street leans to the scene (the NPC-life run, step 6; settings version 139)
        public bool MindActs = true;   // with MindLive: someone near and in view does what their mind just chose: a stroll, the nearest bench, chair, bar or work spot, a walk off home, clear of you or of a foe, over to a friend (the NPC-life run, Lex 2026-10-30; settings version 130)
        public bool MindActsSeatedLooks = true;   // with MindActs: watch acts for everyone (a look at you or the street), people sat or leaning act on look-only choices from their seat, showoff plays in place, and a choice someone is already at counts as acted (NIGHT4 fix 2; settings version 166)
        public bool MindRetries = true;   // with MindActs: a choice that never acted for a reason that passes (sat, riding, busy, the gap) is tried again at the minute's recheck, the model asked first; seated people may be asked by the model, offered only what they can do from a seat (NIGHT4 fix 3; settings version 166)
        public bool MindActsScenarios = true;   // with MindActs: people sat or leaning get up (the waking's way, never pulled) for a choice that needs them on their feet, and rests, meals, drinks and work go to the game's own spots of that kind (a bench, a wall, the bar, a broom), a few at a time near you (0.7.12 stage 1; settings version 168)
        public bool ActThoughts = true;   // a written thought over someone near and in view as an act their own mind chose begins (what moves them to it: tired, thirsty, sore at someone, wanting company; needs MindLive and MindActs), and as an encounter's turn is settled by their own mind (callouts, a domestic, the hogtied, a favour, a warning); works without the AI model: with it on and answering, its own thought comes first (the no-model run, item 1; settings version 173)
        public int ActThoughtEachSeconds = 90;   // a person's act thought at most once in this many seconds (Lex, 2026-11-01: livelier; 180 as built); settings version 174
        public int ActThoughtGapSeconds = 15;    // never within this many seconds of another, different thought of theirs (45 as built; a repeat of the same line doesn't count)
        public bool HeldRing = false;            // with MindActs: the named near you are held, one act straight into the next (Lex, 2026-11-02: a tighter ring, held longer; Game/WestworldScript.HeldRing.cs); off while it's tried; settings version 192
        public bool ControlRing = true;          // [Mind] the control ring (FIVEDAY2 item 1; Lex, 2026-11-08: build it, test the frame cost after): the held ring wider, by how you're moving, with a guard on what it costs (Core/Ring.cs; Game/WestworldScript.HeldRing.cs; settings version 323)
        public bool CrowdPicks = true;           // [Mind] with [Scenes] FightCrowds on: how a crowd takes a fight, one decision for all of them, the AI model's with it on (FIVEDAY2 R10; settings version 339)
        public bool ComposedModel = true;        // [Mind] with the AI model on, it chooses the order of a composed act, from a short numbered menu in plain words (FIVEDAY2 item 9; settings version 336)
        public bool ComposedActs = true;         // [Mind] someone the ring holds, with a feeling strong enough, shows it in two or three of the game's own moves in an order that can follow (Core/Composed.cs; FIVEDAY2 item 8; settings version 335)
        public bool ControlRingPicks = true;     // [Mind] with the AI model on, it may change someone's next step as they come into the ring (FIVEDAY2 item 5; settings version 334)
        public bool ControlRingPlans = true;     // [Mind] someone the ring takes in has a short errand of two to four things, kept by who they are for five minutes (FIVEDAY2 item 4; settings version 333)
        public bool ControlRingSeated = true;    // [Mind] the ring takes in those sat at the game's own benches and rails: after a decent dwell, someone whose mind wants something else gets up for it (FIVEDAY2 item 3; settings version 332; needs MindActsScenarios)
        public bool ControlRingAhead = true;     // [Mind] the ring takes in people along the way you're going a little sooner than those behind you (FIVEDAY2 R1; settings version 338)
        public bool ControlRingKeepAt = true;    // [Mind] someone the ring holds who stops getting where they were sent is given the order again (FIVEDAY2 item 2; settings version 331)
        public float ControlRingMetres = Ring.DefaultMetres;   // ... how near you on foot, in metres (6-25); less in the saddle, none at a gallop
        public int ControlRingMost = Ring.DefaultMost;         // ... how many at once on foot (2-20)
        public float HeldRingMetres = 12f;       // the held ring's reach (4-20 m); let go 13 m beyond it
        public int HeldRingMinutes = 10;         // how long one person is held at most (1-30), then two minutes their own
        public bool LadderShots = true;   // with MindLive and QuarrelLadder: a quarrel the ladder takes to the shot is fired, deadly as in 1899 (Lex, 2026-10-30; settings version 132); off: the standoff breaks off, as before
        public bool MindJoinFights = true;   // with MindLive: an onlooker may join a fistfight on a side, by their nature, drink, and ties to those fighting (Core/JoinIn.cs; the NPC-life run, Lex 2026-10-30; settings version 128)
        public bool MindBrawls = true;       // with MindLive and MindJoinFights: a fight may grow into a brawl, a wave at a time, up to BrawlMost; the law at hand holds most back, not all (Core/Brawl.cs; Lex, 2026-10-31; settings version 160)
        public int BrawlMost = 12;           // the most fighting at once in one brawl, the two who began it among them (3 to 20)
        public bool BrawlLaw = true;         // with MindLive: a lawman who sees a brawl shouts to break it up, lets off those who stop and fights and hogties those who don't
        public bool BrawlPullsYouIn = true;  // with MindBrawls: in a brawl of four or more near you, someone you hit, someone against you or a drunk hothead may come at you (fair game)
        public bool BrawlDraws = true;       // with MindBrawls and LadderShots: rarely, an armed man losing a brawl draws, by the quarrel ladder's own heat for the gun
        public int BrawlDrawShare = Brawl.DrawShare;   // with BrawlDraws: the share of the quarrel ladder's draw odds a losing man has, in percent (0-100; 25 until settings version 161, 2026-10-31)
        public int BrawlFistsFatal = Brawl.FistsFatal; // with MindBrawls: a man beaten down in a brawl is killed this percent of the plain time, else knocked out and up again (0-100; Lex, 2026-10-31; settings version 161)
        public float BrawlJoinMetres = 30f;   // with MindJoinFights: onlookers within this many metres of a fight may join it (10-60; 18 fixed until settings version 164, Lex 2026-10-31: "brawls reach further")
        public float BrawlCrowdMetres = 40f;  // with FightCrowds: people within this many metres of a fight walk over to watch and cheer (10-60; 20 fixed until settings version 164)
        public int BrawlLeastSeconds = 45;    // with MindBrawls: a brawl (four or more, or two joined) lasts at least this long from its first blows (15-120; settings version 164)
        public int BrawlJoinBoost = 20;   // with MindBrawls: added to every onlooker's pull to join a fight (percent points; 0 as before 2026-11-01; Lex: two fighting should grow into big brawls); settings version 175
        public bool BrawlStarterDot = true;   // with MindBrawls: the one who started a fight wears the game's red enemy dot while it lasts, and is fair game (Lex, 2026-11-01)
        public bool SeatedBrawls = true;   // with MindBrawls' fights: two who are sat or leaning may come to blows, got up first Rockstar's way (Lex, 2026-11-01, Valentine saloon: the brawl test found no pair); off: words only, as before
        public int BrawlOpeningBoost = 20;   // with MindBrawls: added to the pull to join in a fight's first 10 s (percent points; Lex, 2026-11-01: more bystanders join early)
        public bool MindOnlookers = true;    // with MindLive: someone who only watched a quarrel, a fight or something you did acts on what they feel about it (walks off from it, stands rooted at a gun, shakes their head, bristles, turns to watch), two a happening at most (Core/Onlooking.cs; roadmap 39; night 2026-10-29, item 3; settings version 108); off, they only turn their heads
        public bool GroupActs = true;        // with MindLive: acts for several at once: after a street fight the first friend may fetch another and the two close round the man who went down (the mind's pick, the model's for a known mind); an onlooker who leaves takes their spouse or sweetheart along (Core/GroupActs.cs; roadmap 39; SIXDAY item 21; settings version 237)
        public bool MindWakes = true;        // with MindLive and West World: what someone has lived through (the shadow's doubts about "things are as they seem") leans the Awakening, a buried man seen walking breaks that belief, and the moment it gives way they stir (Core/Engine.Doubt.cs; the mind doc's step 10; night 2026-10-29, item 4; settings version 109); off, the Awakening by its own roll alone
        public bool MindPassing = true;      // with MindLive: a greeting in passing follows what the first one feels about the other lately and their mood (Core/Passing.Mind.cs; roadmap 39; night 2026-10-29, item 6; settings version 111); and how someone shows you what they feel about you from a while back as you pass (Core/Passing.ToYou.cs, item 6f); off, the tie's greeting
        public bool MindPassingLasts = true; // with MindPassing: how someone shows you what they feel about you as you pass reads the feeling by the mood's slower fading (hours for a slight), not its own quick one (minutes), and from 12 m, not 9 (NIGHT4 item 5; settings version 170)
        public bool MindHelps = true;        // with MindLive: who helps someone up after a fall, and how, comes from what those who saw it feel (Scenes.FallAnswerOf; roadmap 39; night 2026-10-29, item 6b; settings version 112); off, the tie and a roll
        public bool MindRescue = true;       // with MindLive: when someone awake begins their last words, someone near whom it moves (a friend, a kind stranger) may go to them and talk them down (Core/Rescue.cs; roadmap 3's other side; night 2026-10-29, item 6d; settings version 113); off, nobody steps in
        public int RescueChance = 60;        // with MindRescue: of the despairs where someone near is moved to go to them, this share in percent where they do (the rest hesitate too long); seeded (settings version 114)
        public bool MindMeetings = true;     // with MindLive: the street's chance meetings lean by the two's moods today and how much they want company (Core/Reasons.Mood.cs; roadmaps 18 and 30; night 2026-10-29, item 6e; settings version 115); off, by the reasons alone
        public bool MoodsCarry = true;       // with MindLive: the weather wears on everyone's mood out in it (a quarter under a roof), and a feeling shown in a crowd is caught by those near who felt less of it (Core/MoodsCarry.cs; roadmap 18; SIXDAY item 14; settings version 229)
        public bool MindTalks = true;        // people talk to each other from what's on their minds: the worry they're living through, the times (the election, the war, the reservations, the new machines), the stranger, a plan, the town's talk, a need, the day; the other answers from their own nature, and with the AI model in its own words (Core/MindTalk.cs, Core/Times.cs; Lex, 2026-11-03; settings version 196)
        public bool TimesTalk = true;        // with MindTalks and [World] HistoricalAccuracy: any two people near you may talk over the times (1899's news, who holds office, the crops, the railroad...; Core/Reasons.cs TimesReason; Run 3, item 2, settings version 250)
        public int TimesTalkShare = 100;     // with TimesTalk: how often, percent of as built (0-300)
        public bool MindChanceMeetings = true; // with MindLive: what a chance meeting near you is (a song, an argument, a trade, a tall tale ...) is chosen by the two people's minds: their tie, their day, their worry, drink and nature lean what the place allows, and with LanguagePicks the model may pick among the three likeliest (Core/Scenes.Minds.cs; NIGHT4 item 5's plan E; settings version 171); off, the roll by place, sex and age alone
        public bool ThoughtsAloud = true;   // with Language: a thought over someone near is said aloud in their own recorded voice when one of their remarks fits it (settings version 143)
        public bool LanguageVariety = true;   // with Language: each thought the model words gets one thing their mind goes to and a shape, their thoughts lately to steer clear of, and no doubting the world without cause (Core/Language.Variety.cs; settings version 159)
        public bool ModelWakes = true;        // with Language, MindWakes and LanguageVariety: someone with cause who wonders at the world in a thought of their own chips "things are as they seem", so the model's wondering can start them stirring (settings version 159)
        public bool LanguageTalk = true;   // with Language: what people answer in Talk is said in the model's words (the meaning stays the menu's; the written line if it's slow or fails the checks) (settings version 156; NIGHT3)
        public bool TalkTyped = true;      // [Mind] Talk's last row, "Say something else...", opens a line to type your own words on (a keyboard in use); they answer from who they are: in the model's own words with LanguageTalk, else a written answer by what the words seem to be about (Core/FreeTalk.cs; roadmap 37; SIXDAY item 4; settings version 209)
        public bool TalkVoice = false;     // [Mind] Talk's row "Say it out loud...": the voice server listens to your microphone and writes down what you say, and it's said to them as if typed (the same answers). Off as it comes: it needs the voice server with its transcriber (faster-whisper, install-voice.cmd), and a microphone. Nothing is kept, nothing leaves the PC (SIXDAY item 4b; settings version 210)
        public int TalkVoiceSeconds = 6;   // [Mind] with TalkVoice: it listens for at most this many seconds (3-12); a moment's quiet after your words ends it sooner, Space or Enter too
        public bool LanguagePicks = true;   // with Language: the model picks what people near you do, from the few things the mind allows (settings version 142)
        public bool MindStartsScenes = true;   // with LanguagePicks: a person's own reasons with someone near (a quarrel, a friend to comfort, a debt) join the model's list, and a pick starts the scene (settings version 146; NIGHT3 item 1)
        public int PickMetres = 25;    // how far from the player the model picks for people (15 until settings version 147; NIGHT3 item 2); past 15 m it's asked half as often
        public int PicksAtOnce = 8;    // how many picks may wait on the model at once (4 until settings version 147)
        public bool MindTownMood = true;   // with MindLive: a town on edge for some hours after a killing (halving every three game hours; the safety need for all, the model told), and the same trouble seen again moving onlookers less (settings version 149; NIGHT3 item 5)
        public bool MindTempers = true;   // with MindLive: displaced aggression (shamed earlier, they take it out on someone who won't hit back) and the culture of honour in Lemoyne (quarrels climb faster, backing down costs more) (settings version 152; NIGHT3)
        public bool Language = false;        // the language layer (2026-10-29; roadmap 15): with MindLive, the mind's own thoughts worded by a local model (Ollama at LanguageUrl, LanguageModel) through Language.Check, the written line always the fallback; off until the generated thoughts pass the checks at scale (settings version 94)
        public bool LanguageChosen;          // Language was set by the player (the panel or this file); while false and Language off, each start asks Ollama off the game thread and turns Language on, once, if it answers with the model (settings version 163)
        public bool VoicedThoughts = true;    // [Mind] on as it comes from settings version 198 (nothing is heard until the voice server runs): a thought over someone near murmured in their own voice, cloned by the local voice server (voiceoice_server.py) from the game's recordings for their model; settings version 193
        public bool VoicedDialogue = true;    // [Mind] on as it comes from 198: our words said near you (scenes, the talks between people, Talk, encounters) said in the speaker's own voice by the voice server
        public string VoiceUrl = "http://127.0.0.1:5005";   // [Mind] the local voice server; nothing leaves the machine
        public float VoiceMetres = 14f;       // [Mind] voiced within this many metres of you (5-30; 15 until 2026-11-03, Lex: 6% shorter)
        public int VoiceVolume = 100;         // [Mind] the voices' loudness, percent (10-200)
        public bool VoiceRecord = true;       // [Mind] with the voice server: the voices recorded from your own game as you meet people (someone whose voice has no clip yet says one of the game's own lines once, in a quiet moment; the server keeps it; nothing of Rockstar's ships)
        public string LanguageUrl = "http://127.0.0.1:11434";   // the local server; nothing leaves the machine
        public string LanguageModel = EnrageWestworld.Core.Language.DefaultModel;   // the model Ollama holds (ollama pull <name>): llama3.2:3b as it comes since settings version 249 (llama3.1:8b before; Lex, 2026-11-04)

        // [Display] - drawn on screen besides the ticker
        public bool KnownOnMap;   // [Display] a grey mark with the name on the regulars you've met, near you in town. Off as it comes (TENHOUR R6; settings version 353)
        public bool CleanScreen;             // [Display] one switch for a clear screen: names, bars, the meter, mind cards, thoughts, the bar, the Around town box and the VRAM gauge all go, and come back as they were (players on Nexus, 2026-11: at least eight couldn't find how; settings version 322)
        public bool FeelingBars = true;
        public float FeelingBarsMetres = 15f;
        public bool FeelingBarsShowEmpty;
        public bool NameCardsMourns = true;          // [Display] "Mourns one you killed" on the card of someone grieving a friend you killed this week (SEAMS item 12; settings version 445)
        public bool NameCardsRobbed = true;          // [Display] "Robbed you" on the card of the pickpocket who got away with your money, till you have it back from him (SEAMS item 9; settings version 441)
        public bool NameCardsStood = true;           // [Display] "Stood by you" on the card of someone who fought for you this week (an ally in a fight, a friend who ran down your thief) (SEAMS item 5; settings version 439)
        public bool NameCardsOwes = true;            // [Display] "Owes you" on the card of someone you saved or stood with in a fight, for the month they owe you (DEEPEN item 7; settings version 434)
        public string NameCards = "known";        // off | known | everyone: names over heads, enRAGE's way (2026-10-29)
        public float NameCardsMetres = 15f;       // strangers' cards within (30 until settings version 129; Lex, 2026-10-30: 25; Lex, 2026-11-09: 15, for the frame rate)
        public bool NameCardsAlways = true;       // a card over everyone in range, always: hidden only by walls, not tables or people, sixteen at most, drawn before the Testing overlays (Lex, 2026-10-30; settings version 129)
        public float NameCardsKnownMetres = 30f;  // people you know within (75 until settings version 356; Lex, 2026-11-09: 30, for the frame rate)
        public bool NameCardsBesideEnrage;        // ours beside enRAGE's own cards too (else left to enRAGE while its are on)
        public bool NameCardsStrangerNames;      // a stranger's own name over "Stranger"; off: "Stranger" alone (Lex, 2026-10-30; settings version 124)
        public bool NameCardsSecondLine = true;   // the line under the name (temper, trade, Stranger, gang); off: the name alone (players, 2026-10-30; settings version 117)
        public bool WhosWho = true;               // for everyone (Lex, 2026-10-30; MindLive only for its Lately): the reader (the papers key) ends with a page of the people you've met, read from what is kept (Core/WhosWho.cs; roadmap 24; night 2026-10-29; settings version 116)
        public bool Overheard = true;             // [Display] the papers end with Overheard: what went on near you in the last game day, one sentence each, newest first (read from what's kept; nothing saved) (Core/Overheard.cs; roadmap 24; SIXDAY item 7; settings version 214)
        public bool GrudgeMeter = true;
        public string VramGauge = "model";        // off | on | model: "VRAM 11.3 / 15.6 GB" at the top left, the whole card's memory in use as Windows counts it; model: only while Language is on (Lex, 2026-11-01; Core/Vram.cs; set aside for 0.7.12, back 2026-11-03; settings version 195)

        // [WestWorld] - the master switch and everything under it
        public bool WestWorld = true;
        public float AwakeningShare = 3f;   // a share of a percent too (0.6); 5 until settings version 138 (Lex, 2026-10-30: fewer waking), 3 until 159 (Lex, 2026-10-31: fewer stirring, a little), 2.25 until 252 (Run 3, item 4; Lex, 2026-11-04: "we can tune up the amount of awake people, and frequency")
        public int AwakeningDays = 3;   // 1 until settings version 138 (Lex, 2026-10-30: "very slow how fast they progress the stages"), 5 until 252 (Run 3, item 4: the first awake after about 7 hours of play, not 14)
        public int AwakeningBreakdown = 90;
        public int AwakeningDespair = 65;   // 75 until settings version 191 (Lex, 2026-11-02: the waking rarer, so a despair death lands harder), 50 until 252 (Run 3, item 4: Lex hadn't seen many)
        public bool WakeVoices = true;   // the waking speak the game's own lines, oddly, more each stage (Core/WakeVoice.cs; settings version 78)
        public bool LoopVoice = true;    // with West World: from the second stage a waking regular hears their habit as a voice giving orders at the hour (on the ticker, once a day), from the third answers it, at the fourth breaks the loop now and then (a broom on an empty street at night, the church wall at noon, the river's edge: one a town a day) and, once, comes to the centre (three thoughts that end with the voice known as their own); after it an ending may be a reset to stage 0 (Core/LoopVoice.cs; SIXDAY item 12a; settings version 225)
        public bool HostNotes = true;   // with West World: two waking people test the world together (counting faces, walking to the edge, comparing dreams) half the times they talk strangely, and each wakes faster for three days after (Core/HostNotes.cs; FIVEDAY R28, settings version 317)
        public bool Ghosts = true;       // with West World: the reload noticed. What a save loaded from before drops is kept a week as ghosts (always); with this on, a victim of a rewound killing who walks again gets one deja vu when you come within 12 m, both sides of a rewound tie think of the quarrel or the kiss that didn't happen, the waking talk of the morning that happened twice for two days, and the ticker says how many times the clock turned back (Core/Ghosts.cs; SIXDAY item 12b; settings version 226)
        public bool WakeLines = true;    // with West World: each person at the fourth stage holds one line out of the public domain and says it when they talk with someone tied to them or waking; a listener who suspects enough (awareness 40+) and hears the same line from two different people within three days wakes a stage, one a town a day; and someone tied to a dead man who sees him alive again (a save loaded from before) reacts once (Core/WakeLines.cs; SIXDAY item 12c; settings version 227)
        public bool DespairFall = true;   // with West World: despair may end at the edge of a sheer drop of 7 m or more near (Core/Despair.cs; Run 3, item 4; settings version 252)
        public bool DespairWater = true;  // with West World: a fully awake person who can't bear it, with the river near and no gun or poison in them, may walk into the water and keep walking (Core/Despair.cs; SIXDAY item 12d; settings version 228)
        // the waking held and stranger (settings version 144; Lex, 2026-10-31: they swapped back to the game between acts; Game/WestworldScript.WakeHold.cs)
        public bool WakeHold = true;
        public int WakeHoldMinutes = 3;
        public bool WakeGestures = true;
        public bool WakeStrange = true;
        public bool WakeByNature = true;   // with WakeStrange: which strange thing they do follows their temper and what they feel most, and the one they bother is whoever they feel most about, else the strongest tie, else the nearest (Core/WakeOdd.cs StrangeByNature; roadmap 41; SIXDAY item 25; settings version 241)
        public int WakeStripChance = 5;
        public int WakeLashOut = 25;   // settings version 145 (Lex, 2026-10-31: the awake should turn on Arthur and on others)
        public bool HostsQuestionReality = true;
        public int RealityChance = 15;
        public bool Glitches = true;
        public bool Reveries = true;
        public int ReturnChance = 35;
        public int StrangerReturnChance = 15;

        // [Stalkers]
        public bool Stalkers = true;
        public string StalkerKey = "]";
        public int StalkerEveryMinutes = 15;   // 34 until settings version 161 (Lex, 2026-10-31: "have the stalker appear more often")
        public int StalkerChance = 35;         // 20 until settings version 161: about once in 45 minutes of play, was about once in three hours
        public int StalkerNearbyMetres = 60;
        public bool StalkerStrangers = true;
        public bool WagonOthersStop = true;   // [Encounters] with WagonWheel: someone else on the road may pull up for the wagon when you haven't helped, and set the wheel with the driver (FIVEDAY item 32d, settings version 287)
        public bool StalkerRides = true;      // [Stalkers] a mounted stalker stays in the saddle to follow, come in close and ride off; checked, and down on foot if the horse doesn't go (Core/StalkerRide.cs; FIVEDAY item 32c, settings version 286)
        public int StalkerHorse = 35;          // percent of stalkers who appear on horseback and sit the horse watching; they get down to move (SIXDAY item 19; settings version 235)
        public bool RevenantsAnger = true;     // [Stalkers] someone you killed comes back by their anger: the angry walk again more often and come at you readier, the mild mostly stay down (Lex, 2026-11-09; settings version 368)
        public bool RevenantsHunt = true;      // [Stalkers] someone you killed may come to kill you back, by their grudge (Lex, 2026-10-31; violence against Arthur on by default, 2026-11-01; settings version 326)
        public int RevenantHuntChance = 30;    // ... percent at each chance once close, before what they hold against you adds to it
        public bool RevenantsMinds = true;     // [Stalkers] with the AI model on, it chooses what someone you killed does when they've found you; the rules choose without it (Core/RevenantMind.cs; settings version 325)
        public bool RevenantsAtCamp = true;    // with West World: someone you killed, due back (or due within two days), comes to the edge of your firelight when you sit at a fire of a night out of the towns (SIXDAY item 19)
        public bool StalkerRecords = true;
        public bool StalkerBlack = true;       // the stalker is always a stranger dressed in black, spawned out of sight (Lex, 2026-10-31); never someone near turned or one of enRAGE's people, whose clothes enRAGE knows them by; false: as before (settings version 161)
        public string StalkerBlackModel = "a_m_o_sdupperclass_01";   // his model, from the Testing row "Stalker outfit: show the next candidate"
        public int StalkerBlackOutfit = 0;     // his outfit preset in that model (-1: as the game made him)
        public int StalkerMinutes = 120;
        public int StalkerAppearMin = 45;
        public int StalkerAppearMax = 70;
        public int StalkerFollowMetres = 26;
        public int StalkerStareMetres = 34;
        public int StalkerStillSeconds = 20;
        public string StalkerWhenStill = "mix";
        public int StalkerCircleMetres = 8;
        public int StalkerActMin = 2;
        public int StalkerActMax = 30;
        public int StalkerHideMin = 45;
        public int StalkerHideMax = 70;
        public bool StalkerMutters = true;

        // [Quiet]
        public bool QuietWhenStill = true;
        public int QuietStillSeconds = 3;
        public bool QuietInCinematic = true;

        // [Encounters]
        public bool Encounters = true;
        public int EncounterEveryMinutes = 12;
        public int EncountersAtOnce = 5;   // the encounters and a stalker going on at once (Lex, 2026-10-27; it was one; settings version 79)
        public bool CryingWoman = true;
        public int CryingWomanChance = 15;
        public bool NightVisitor = true;
        public int NightVisitorChance = 18;
        public bool Travellers = true;
        public int TravellerChance = 20;
        public bool SecondActs = true;    // [Encounters] the people of the encounters come back: the saved to thank you, the kin of a man you watched die, the men you let get away (FOURTEENHOUR reserve R3; settings version 396)
        public bool Elopers = true;       // [Encounters] a couple running off from her family comes to you; hide them, send them on, or give them up (FOURTEENHOUR reserve R18; settings version 407)
        public bool AlleyVoice = true;    // [Encounters] Saint Denis after dark: a voice calls for help from the dark; real, or bait (FOURTEENHOUR reserve R15; settings version 406)
        public bool NoMapRider = true;    // [Encounters] at night, rarely, a rider asks the way to a town on no map, and goes on into the dark (FOURTEENHOUR reserve R11; settings version 404)
        public bool Riders = true;        // [Encounters] on the traveller's turn, a rider: someone of the next town rides along a while, or a deputy asks after the posse's man (FOURTEENHOUR reserve R9; settings version 402)
        public bool WagonWheel = true;    // [Encounters] a wagon ahead of you on the road loses a wheel (roadmap 32; Core/WagonWheel.cs; settings version 245)
        public int WagonWheelChance = 20;
        public bool Pinkertons = true;    // [Encounters] by day in a town where a violent deed of yours is known this week, now and then a Pinkerton man asks its people after you; friends lie, the afraid talk, and a friend warns you after (Core/Pinkertons.cs; FIVEDAY item 15, settings version 267)
        public bool Campfires = true;     // [Encounters] strangers at a campfire at night in the wilds (night 2026-10-28, item 14; settings version 90)
        public int CampfireChance = 15;
        public bool VoiceBelow = true;    // [Encounters] West World: late at night in a saloon, a muffled plea from under the boards and the barkeep's thoughts (night 2026-10-28, item 14; settings version 90)
        public bool SupperInvitation = true;   // the supper invitation, the player version (Lex, 2026-10-27 18:33; settings version 81); off until tested from version 82; on as it comes from version 172 (Lex, 2026-11-01: violence against Arthur on by default)
        public int SupperChance = 12;
        public string CryingWomanKey = "None";
        public bool Executions = true;
        public int ExecutionChance = 20;
        public string ExecutionKey = "[";
        public bool Hermits = true;
        public int HermitChance = 25;
        public string HermitKey = "None";
        public bool CircleAnswers = true;        // [Encounters] walk into the middle of a ring of the awake and the ring decides what to do about it, as one: the AI model's choice with it on, the rules' without (FIVEDAY2 item 10; settings version 337)
        public bool Gatherings = true;
        public int GatheringHostile = 50;   // [Encounters] percent of gatherings that turn on you once you come within 10 m (Lex, 2026-11-01; violence against Arthur, on as it comes); 0 never; settings version 186
        public bool EventDots = true;   // [Encounters] the game's pulsing event dot on the map where one of ours is going on (Lex, 2026-11-01); settings version 187
        public int GatheringChance = 10;   // 15 until settings version 191 (Lex, 2026-11-02: rarer and eerier)
        public string GatheringHours = "any";   // any: the night's (midnight to 03:30, the wilds) and by day, in the open or at a town's edge; night: the night's only, as before settings version 163 (Lex, 2026-10-31: "make them happen more often and at any time of day")
        public int GatheringGapHours = 24;      // game hours at least between gatherings, so they don't stack (settings version 163; 6 until 191, Lex 2026-11-02: rarer)
        public string GatheringKey = "None";
        // re-staged from the game's own encounters, with the town's people choosing by their minds (NIGHT4 item 4; settings version 169)
        public bool PublicCallouts = true;      // by day in a town, one of the people about calls another out before the street over something real between them
        public int PublicCalloutChance = 35;
        public bool DomesticDisputes = true;    // in a town, a man shouting at his wife in the street; she and a neighbour answer by their own minds
        public int DomesticChance = 30;
        public bool Hogtied = true;             // out of the towns by day, one of a nearby town's people tied up by the road with two men by them, who they are from the captive's own life
        public int HogtiedChance = 20;
        public bool HogtiedMenDraw = true;   // on as it comes from version 172 (Lex, 2026-11-01: violence against Arthur on by default): the hogtied's men may draw on you unprovoked as you come near; off: they fight only once you step in, as the execution's men
        public bool CalloutsArthur = true;   // [Encounters] by day in a town, one of the people near who holds something against you comes over to say so; stay and they fight (violence against Arthur, on as it comes; Lex, 2026-11-01); settings version 190
        public int CalloutsArthurChance = 30;
        public int CalloutsArthurDraw = 10;   // percent: an armed hothead calling you out over blood draws instead of putting up fists (0 never)
        public int HogtiedTurnsOnYou = 25;   // [Encounters] percent: one you cut loose pulls a knife on you instead of thanking you (violence against Arthur, on as it comes; Lex, 2026-11-01); settings version 189
        public bool Wayside = true;          // [Encounters] things to come across on the road: fights, prisoners, trouble, camps, what's left after (Core/Wayside*.cs; settings version 324)
        public int WaysideChance = 25;       // ... percent a roll, among the other encounters
        public bool AlliesHelp = true;       // [Encounters] attacked within sight of people who think well of you, some of them come to your aid (Core/Allies.cs; a player's request, 2026-11-03; settings version 197)
        public bool KinThanks = true;        // [Scenes] with KinGrudges: one time in two, a kinsman of someone you saved comes to find you a few days on and thank you for him (SEAMS item 7; settings version 444)
        public bool GratitudeFelt = true;    // [Encounters] with MindShadow: the man you stood with in a fight, paid the fine of or stood surety for feels it as a kindness in his mood, and so do those who saw (SEAMS item 11; settings version 443)
        public bool SuretyKeptOwes = true;   // [Encounters] ... a man you stood surety for who keeps the peace his week out owes you then, as one you saved; he remembers it and comes to thank you (SEAMS item 10; settings version 442)
        public bool FineOwes = true;         // [Encounters] ... a man whose fine you paid at the jail door owes you for a month, as one you saved; he remembers it and comes to thank you; standing surety is a kindness, no debt (SEAMS item 1; settings version 435)
        public bool StoodRemembered = true;  // [Encounters] ... the man you stood with in a fight keeps it in his own mind: his thoughts come back to it, and days on he comes over to thank you (SEAMS item 2; settings version 436)
        public bool StoodByOwes = true;      // [Encounters] ... a man you stood with in a fight (J, "Stand with him") owes you for a month, as one you saved: readier to come to your aid, to call on you, to run down your thief (DEEPEN item 4; settings version 433)
        public bool AlliesSpareLaw = true;   // [Encounters] ... in a fight that has had anyone but the law against you, friends leave the law alone; the law alone at you, they still stand with you (a player on Nexus, 2026-11-10: friends killed the sheriffs while fighting bandits; Core/Allies.cs LawIsFoe; settings version 429)
        public int AlliesMetres = 40;        // ... from within this many metres
        public int AlliesMost = 3;           // ... at most this many at once
        public int AlliesLiking = 90;        // ... enRAGE's fondness for you (0-100) from which someone counts as a friend (Lex, 2026-11-03: "a friend when the fondness bar is 90% or higher"; 40 until settings version 202); a kindness of yours they remember, a trouble you mended, talks and your name known count on their own
        public int AlliesRoadChance = 30;    // ... percent: out of the towns, with nobody you know in sight and two or more armed against you, a friend from a town's ledger happens by
        public bool AlliesDot = true;        // ... a small green dot on the map on each friend near you and on each ally while they're with you (Lex, 2026-11-03; settings version 199, friends from 204)
        public string AlliesDotColour = "BLIP_MODIFIER_DEBUG_GREEN";   // ... the dot's colour: one of the game's blip modifiers by name; empty keeps the companion dot's own colour
        public float AlliesDotScale = 0.6f;  // ... the dot's size (0.2-2; Lex, 2026-11-03: the green dot "quite large"; settings version 205)
        public int GreetLiking = 34;         // [Encounters] enRAGE's fondness for you (0-100) grows by this on every greeting of someone (three hellos fill the bar and pass AlliesLiking 90: Lex, 2026-11-03, "each greet gives 25% fondness boost for now until we add in more NPC interactions", then "3 greets will get to 100%"); settings version 199 (12), 200 (20), 201 (25), 34 from 203
        public bool FondnessActs = true;     // [Encounters] more ways to win people over through Talk: whiskey for the house in a saloon, a gift, condolences to someone grieving (Core/Fondness.cs; SIXDAY item 1, 2026-11-04; settings version 206)
        public bool TalkVerbs = true;        // [Encounters] three more verbs in Talk: what they know of someone they've had dealings with, a loan of ten dollars (and its collecting three days on), an introduction to someone standing near (Core/Verbs.cs; roadmap 6; SIXDAY item 6; settings version 213)
        public bool TalkWorries = true;      // [Encounters] every worry mendable through Talk once they've told you of it (the money ones keep their row), and every worry with a dark mirror: a word that turns it the wrong way (Core/Helps.cs; roadmap 9; SIXDAY item 9; settings version 216)
        public bool KindnessSeen = true;     // [Encounters] the named people who see you do a kindness to someone else (a drink stood, money given, a hand up, a bully faced, a mercy) think better of you too (settings version 206)
        public int KindnessSeenLiking = 5;   // ... enRAGE's fondness each of them gains (0-30); our residents' affection moves a little on its own
        public bool AlliesRide = true;       // [Encounters] the friend from the road comes on horseback (45-70 m off, down off the horse near you); off: on foot, 30-45 m off, as before (SIXDAY item 2; settings version 207)
        public bool AlliesGrieved = true;    // [Encounters] an ally who dies standing with you is grieved by those who knew them, who think the less of you for it, and the town has it by evening
        public int AlliesGrievedLiking = 15; // ... enRAGE's fondness a friend of the fallen loses (0-50; a third of it for someone who merely knew them)

        // [World]
        public bool Deeds = true;
        public bool SelfDefenceNoBounty = true;
        public bool TownPosts = true;
        public string TownPostsAfterDeath = "survive";
        public bool Regulars = true;
        public int RegularsPerTown = 30;
        public bool RegularsDress = true;
        public bool Returners = false;      // [World] a resident who matters (woken, a worry, met) comes back unseen in a body of their build, dressed in an outfit of their own (Core/Returners.cs; the recognition plan's Stage 1; Run 3, item 5; settings version 253); off until Lex has seen it: enRAGE may take a dressed body for one of its own
        public int ReturnersChance = Core.Returners.ChanceAsBuilt;    // [World] percent of the unseen bodies the regulars' scan looks at that a returner is tried on first
        public int ReturnersPerTown = Core.Returners.PerTownAsBuilt;  // [World] at most this many returners with an outfit in one town
        public int LooksReadDepth = 1;      // [World] the Testing row "Read the looks of the people near you" only: 1 counts the pieces, 2 reads them, 3 their tints, 4 their kinds (the recognition plan's Stage 2)
        public bool RegularsRoutines = true;   // [World] the town keeps time: who a passer-by becomes follows the hour and the spot (the drinkers at the saloon of an evening, the churchgoers in church on Sunday morning, the early up at dawn, only the owls out at night); a new regular is made to fit the spot, and a regular out of their hour thinks so (Core/Routines.cs; roadmap 17; SIXDAY item 5a; settings version 211)
        public bool RumoursTravel = true;
        public bool Newspaper = true;
        public bool Letters = true;
        public bool LetterReplies = true;     // [World] answer a letter from the reader (R): kindly, coldly, with a threat, with money, or asking to meet; it lands one to three game days on and changes what the writer does next, and most write back (Core/Replies.cs; roadmap 8; SIXDAY item 8; settings version 215)
        public bool WriteLetters = true;      // [World] write first, from the reader in a post town (L), to anyone you've met: a warning, an apology, a love note, a threat or money (settings version 215)
        public bool TownEvents = true;        // [World] town events on one framework: things a town does on a day, posted ahead, that happen whether you're there or not (Core/TownEvents.cs; roadmap 10; SIXDAY item 10; settings version 217)
        public bool EventDances = true;       // [World] the Saturday dance: every second Saturday in a town with a saloon, seven till eleven, posted on Thursday; staged outside the saloon when you're there
        public bool EventElections = true;    // [World] election day: the first Tuesday after the first Monday of November, in every town with five living regulars; two of its men stand for mayor or town marshal; posted a week ahead; the voters lean by temper and ties, and by your word (Core/Elections.cs; SIXDAY item 10b; settings version 218)
        public bool EventTrials = true;       // [World] the circuit judge and gallows day: a resident who kills a man in a fight is held at the jail, tried three to seven days on (the verdict from witnesses, friends, temper, your word and the town's fear), and hanged two days after a guilty verdict; you can testify, be sworn in, break him out or stop it (Core/Trials.cs; SIXDAY item 10c; settings version 219)
        public bool EventShows = true;        // [World] the Wild West show: once your most-told deed is known in four towns (one three hops off), a touring show comes to a large town you've been to, a week after its posters, and acts out the most bent version; the paper reviews it and the legend bends further; heckle, stand up as the real one, or rob the takings (Core/Shows.cs; SIXDAY item 10d; settings version 220)
        public bool NewFaces = true;   // [World] with TrainTime: a stranger off the train may come as a settler, a schoolteacher, a new preacher or a mail-order bride come to wed a bachelor there; a newcomer's thoughts for two weeks (the roadmap's idea bank; settings version 366)
        public bool TrainTime = true;         // [World] a train standing at a town's platform (a platform spot: the spot key, or a station you came out of): the platform turns to look, the keeper calls it, a newcomer or kin steps down, letters come off it, and a resident whose worry ended in leaving says goodbye and goes for good (Core/Trains.cs; SIXDAY item 11b; settings version 222)
        public bool ReturningNeighbours = true;   // [World] with TrainTime: someone who left the town on the train a week ago or more may step off a later one, back among the town's people, and a friend near does a double take (Core/Trains.cs; roadmap 36; SIXDAY item 22; settings version 238)
        public bool EpilogueCarries = true;   // [World] John's first save (john.json made with arthur.json beside it) takes Arthur's residents over eight years on (stayed, married, widowed, moved on, died), the ties between them, and the memory of a man called Morgan in those who met him (Core/Epilogue.cs; roadmap 25; SIXDAY item 23; settings version 239)
        public bool TownLedger = true;        // [World] each town's ledger, read off the records: your killings, other deaths and hangings take its index down, arrivals, good turns, the show and the dance take it up; the last month sets its mood (grieving, fearful, prospering) for its people's thoughts; the regulars a town keeps scale with the index (Core/TownLedgers.cs; SIXDAY item 11c; settings version 223)
        public bool ThinStreets = false;      // [World] off as it comes: a town whose index is under 30 has its ambient people thinned each frame (never below 0.6, that town only, never in missions); the log says what the game does with it
        public bool FindDiaries = true;
        public bool Avengers = true;
        public int AvengerChance = 35;
        public int AvengersAtOnce = 2;      // riding, here or still alive on enRAGE's roster (Lex's players, 2026-10-28)
        public int AvengerGapHours = 48;    // game hours between one avenger's arrival and the next's
        public bool CastRelatives = true;
        public bool CastRelativesSpawn = true;   // when nobody free is on the street (enRAGE has named most walkers in a well-played town), the relative is made out of sight behind you and let go to the game after (Lex, 2026-10-29; settings version 95)
        public int Bravery = EnrageWestworld.Core.Bravery.AsBuilt;   // [World] how brave the townsfolk are, 0-100: how readily an onlooker joins a fistfight (85 a tenth more pull than the 2026-11-01 tuning, which is 50 now; higher more, lower fewer; Core/Bravery.JoinPull; settings version 248), and how often the mod's own frights send someone running (the brave stand back and watch). Never the game's own flight: the cower flag (PCF 44) it used to set froze people where they stood, and BraveStand went with it (Lex, 2026-11-04; settings version 247)
        public string Storyteller = "lively";   // [World] off | calm | lively | wild: the storyteller shortens the waits on a quiet street or road and lengthens them a little after something big; it never starts anything itself (NIGHT4 item 2; settings version 167)
        public bool HistoricalAccuracy = true;   // [World] the real history: what people talk of from the papers of 1899 and 1907 (Times.cs), the lines about the year they live in, the real calendar's holidays and the year's work by region (Core/Calendar.cs), the snake-oil man's Food and Drugs Act in 1907, the Panic in the paper; off, none of it, and the timeless texture of the days stays (Lex's brief, 2026-11-03: "bEnableHistoricalAccuracy"; SIXDAY item 3; settings version 208)
        public bool AlmanacSides = true;   // [World] with Almanac: on the year's quarrels (the Philippines, the trusts, gold and silver, the dry towns, the vote, the Boers) a farmer, a working man, the well-off, a soldier and a woman think their own side (Lex, 2026-11-09; settings version 370)
        public bool Almanac = true;   // [World] with HistoricalAccuracy: the almanac, a few hundred dated things of 1899 (and of 1907) known from the day they were in the papers, and the year's everyday talk, in the thoughts, Talk's news, the talks between two people, the paper read aloud on the street, the week's paper in the reader and a word on the ticker (Core/Almanac.cs; the second six-day run, item 1; settings version 246)
        public int AlmanacShare = 100;   // [World] percent: how much of it gets through (the pull of the thoughts, the share of the news answers and the talks); 0 none, 200 twice
        public bool OwnLooks = false;   // [World] standing alone: an ordinary person is known again by the look of the body they were met in (the model and every piece they wear), so the same townsman met again is the same man (roadmap 34, phase A; night 2026-10-29, item 5; settings version 110); off as it comes until seen in game; with enRAGE running, its own knowing by look is used and this never
        public bool Drama = true;
        public int DramaGapSeconds = 30;   // 30 from settings version 185 (Lex, 2026-11-01: "up the frequency"); 150 until 87, 90 until 101, 60 until 185 (Lex, 2026-10-28 and 10-29: the world lively)
        public bool SaloonGossip = true;
        public bool Weddings = true;
        public bool Funerals = true;
        public bool JobRumours = true;
        public bool PrisonerTransfers = true;
        public bool Debts = true;

        // [Scenes]
        public bool SceneReactions = true;
        public bool ShoutsCarry = true;   // shouts (anger, threats, fright, "Thief!", fight talk, cheering) are shouted, and carry farther (settings version 141)
        public bool RealVoiceTalk = true;
        public int RealVoiceTalkSeconds = 120;
        public bool CrudeTalk = true;
        public bool RoughHandling = true;
        public float TroubleWeight = 1f;   // [Scenes] every chance of a fight or a quarrel between people scaled by it (Lex, 2026-10-28; settings version 88)
        public bool ReasonVariety = true;   // [Scenes] the street's reasons take turns: a worry said once a visit, the last four scenes' kinds weigh against their own, snapping starts lower (NIGHT4 fix 5; settings version 166)
        public bool ChanceFirst = true;   // [Scenes] the chance meeting (songs, tales, haggling, standoffs) goes first when no reason is strong, the argument's share stops growing past weight 1.5, main scenes start nearer you, and two held in chairs far apart aren't cast (NIGHT4 fix 7; settings version 166)
        public bool HorseThieves = true;   // [Scenes] with MindLive: a man whose mind leans to it takes a horse with nobody on it and rides off; its owner shouts after him (the NPC-life run, step 5; settings version 137)
        public bool FightCrowds = false;   // off as it comes from settings version 182 (Lex, 2026-11-01: onlookers froze; "remove the feature that tells the NPCs to gather as a crowd") [Scenes] with MindLive: a crowd rings a street fight and cheers it on (the fearful keep away) (the NPC-life run, step 5; settings version 136)
        public bool FindTheLetter = true;     // [Scenes] the letter a resident waits on sits at another town's depot: a station agent there hands it to you (J), and you carry it to him (J) (SEAMS item 18; settings version 451)
        public bool SavedKnown = true;        // [Scenes] with Deeds: someone you save in the street or on the road (a bully pulled off, a beater put down, the hogtied cut loose) is a good deed the town hears of (SEAMS item 17; settings version 450)
        public bool StepBetweenKnown = true;   // [Scenes] with StepBetween and Deeds: two you stepped between who both let it be: a peace deed the town hears of, as a fight broken up is (SEAMS item 16; settings version 449)
        public bool WalkHomeMends = true;     // [Scenes] with WalkHome: seeing a man in drink home while the drink is his worry mends it, as coffee through Talk does (SEAMS item 15; settings version 448)
        public bool FriendsKnowYourThief = true;   // [Scenes] with FriendsWarnThief and NameCardsRobbed: a friend's warning of a pickpocket near says when he's the one who got away with yours (SEAMS item 14; settings version 447)
        public bool StandReminds = true;      // [Scenes] with BrawlVerbs: the J row "Stand with ..." says when the man losing stood by you this week, or owes you (SEAMS item 13; settings version 446)
        public bool BreakUpKnown = true;      // [Scenes] with Deeds: a fight you break up on J is a peace deed, as a word in a scene is: the witnesses keep it, it goes round as gossip, a newspaper might carry it (SEAMS item 6; settings version 440)
        public bool WelshersShunned = true;   // [Scenes] with BrawlVerbs: welsh on a bet in a town and its betting men won't take your money for a week; the one who'd have offered says why (SEAMS item 4; settings version 438)
        public bool WelshRemembered = true;   // [Scenes] with Minds: the man you welshed on, and those who saw it, keep it in mind in the bet's own words (SEAMS item 3; settings version 437)
        public bool BrawlWagers = true;   // [Scenes] with MindLive: people watching a fistfight bet on it among themselves, and settle after: paid, grumbled, owed or welshed on (Core/Wagers.cs; FIVEDAY item 1, settings version 256)
        public bool Rematches = true;   // [Scenes] with MindLive and MindPlans: the one beaten in a fistfight takes it his own way (a rematch, keeping clear, a handshake) days later (Core/Rematch.cs; FIVEDAY item 2, settings version 257)
        public bool AlliesRemember = true;   // [Scenes] with FriendCalls: an ally who stood with you in a fight comes over within the week to talk it over, and of a comrade who fell in it (FIVEDAY item 4, settings version 258)
        public float SideTalkMetres = SideTalk.Metres;   // [Scenes] with ScenesAtOnce 2 or more: two people this far apart at most may stop and talk beside the scene (2.5-8; 3.4 before settings version 259, when the quiet talks almost never started; FIVEDAY item 5)
        public bool LastingMarks = true;   // [Mind] one you beat carries it for days: their thoughts of it, a friend naming you (Core/LastingMarks.cs; FIVEDAY item 24, settings version 276)
        public bool LastingMarksLook = false;   // [Mind] ... and shows it: the gait and the bruising below (off until the probe row has been seen in game)
        public string LastingMarksGait = "injured_general";   // [Mind] the game's gait for the marked (the probe row's candidates; empty: none)
        public string LastingMarksPack = "SCR_Torture";   // [Mind] the game's damage pack for the marked (the probe row's candidates; empty: none)
        public bool MoreMoments = true;   // [Mind] more of the moment in thoughts: a horse bolting, a purse lifted, rain in the saddle, a storm, the brawl's ring and an execution's onlookers; women's own fist lines; how you look, pools of its own (Core/Moments.cs; FIVEDAY item 32e, settings version 288)
        public bool TelegraphKnows = true;   // [Mind] the station agent knows first what came over the wire: deaths elsewhere, and your deeds in other towns before the word gets here (Core/Telegraph.cs; FIVEDAY R15, settings version 303)
        public bool PlacesMourned = true;   // [Mind] where someone died near you by another hand (yours are PlaceMemory's, PlacesRemember), those close to them stop there for three weeks after, turn to it, think of them; the dearest kneel (THREEHOUR item 5; settings version 360)
        public bool MemorialPhotos = true;   // [Mind] Saint Denis and Blackwater: the photographer thinks of the memorial portraits of the town's dead, and mourners of the photograph that came (Core/MemorialPhoto.cs; FIVEDAY R11, settings version 299)
        public bool UndertakerKnows = true;   // [Mind] the undertaker thinks of the week's dead of his town by name and cause, a busy week, and you as trade (Core/Undertaker.cs; FIVEDAY R10, settings version 298)
        public bool GamblersBorrow = true;   // [Mind] a gambler at the worst of his cards borrows from a man of his town (a friend first), who lends, refuses or snaps; paid back or never (Links, Exchanges.MakeLinked; FIVEDAY item 32a, settings version 284)
        public bool TieGossip = true;   // [Mind] word of your deeds moves along the town's ties: friends tomorrow, others the day after, bent at each telling (Core/TieGossip.cs; FIVEDAY item 23, settings version 275)
        public bool SharedFright = true;   // [Mind] two who both saw you kill are tied by it: their tie warms, they keep clear of you together, and their quiet talks are of it (Core/SharedFright.cs; FIVEDAY item 22, settings version 274)
        public bool SurvivorsChanged = true;   // [Mind] one you beat or robbed comes back changed: with friends to get even, with standing for having faced you, or broken and keeping clear (Core/Survivors.cs; FIVEDAY item 21, settings version 273)
        public bool TrustSlow = true;   // [Mind] a wrong of yours undoes three greetings at once, and kindness to someone you wronged counts for less until time and kindness bring them back (Core/Trust.cs; FIVEDAY item 20, settings version 272)
        public bool PeakEnd = true;   // [Mind] a meeting with you is remembered by its strongest moment and how it ended, not its length (Core/PeakEnd.cs; FIVEDAY item 18, settings version 270)
        public bool StressBuilds = true;   // [Mind] a week's load adds up (deaths, beatings, quarrels, thefts, a worry); past their breaking point a person breaks in their own way and the habit stays a week (Core/StressLoad.cs; FIVEDAY item 17, settings version 269)
        public bool WorriesShown = true;   // [Mind] with MindLive and MindActs: the worry a person carries leans what they choose to do, and the thought over their head says so when they act on it (Core/WorryActs.cs; FIVEDAY item 7, settings version 260)
        public bool VoicedEncounters = true;   // [Mind] with VoicedDialogue and the voice server: the encounters' and walk-ups' lines (a friend's visit, a warning, a campfire, a traveller, a callout, a bet...) said in the speaker's own cloned voice instead of a recorded remark picked by tone, one every 5 s at most (FIVEDAY item 8, settings version 261)
        public bool EventPrizeFights = true;   // [World] with TownEvents: now and then two of a town's men fight for a purse out back of the saloon at night, the word going round that afternoon (Core/PrizeFight.cs; FIVEDAY item 10, settings version 262)
        public bool Payday = true;   // [Scenes] Saturday noon to eight in town: the working people's day bends to the week's pay, and a woman may meet a man at the door for his wages (Core/Payday.cs; FIVEDAY item 11, settings version 263)
        public bool MurderBallads = true;   // [Scenes] at a campfire at night, once a night, a murder ballad from the next fire: your most-told killing, else a death known (Core/MurderBallad.cs; FIVEDAY R20, settings version 309)
        public bool QuiltingBee = true;   // [Scenes] Tuesday and Thursday afternoons, still by the church steps: the quilting bee through the window, the town's goings-on and a word of you, on the ticker (Core/QuiltingBee.cs; FIVEDAY R8, settings version 296)
        public bool WalkHome = true;   // [Scenes] after dark in a town, J by a woman, someone in drink or a timid man: offer to see them home (Talk's own walk and answers; THREEHOUR item 4; settings version 359)
        public bool BarRound = true;   // [Scenes] at a saloon's bar, J: buy the house a round, a dime a head; the room cheers and is fonder of you, one a day in each saloon (THREEHOUR item 3; settings version 358)
        public bool StoreSlate = true;   // [Scenes] at the store: someone asks to put it on the slate; the keeper chalks it, asks for something first, or says it's full, and J lets you pay what they owe; on payday they pay it down (Core/Slate.cs; FIVEDAY R2, settings version 290)
        public bool MoneyTalk = true;   // [Scenes] two men meeting in a town argue gold against silver, the banks and the trusts; who they are decides the side, and those who disagree are the colder for it (TENHOUR item 15; settings version 346)
        public bool Suffrage = true;   // [Scenes] by day in Saint Denis: a woman says what she thinks about the vote to a man she meets; he forbids it, laughs, is shaken or agrees (Core/Suffrage.cs; TENHOUR item 16, settings version 347)
        public bool ScripTalk = true;   // [Scenes] by day at Annesburg: a man says what he thinks of being paid in the company's scrip, and the other agrees, hushes him or warns him (Core/ScripTalk.cs; TENHOUR R2, settings version 349)
        public bool Temperance = true;   // [Scenes] by day, a temperance woman asks a drinker in the street to sign the pledge (Core/Temperance.cs; FIVEDAY item 12, settings version 264)
        public bool GrindingSeason = true;   // [World] 15 October to 20 December: the cane grinding in Rhodes, Saint Denis and Lagras, its men working into the night, and the grinding ball at Rhodes to end it (Core/Grinding.cs; FIVEDAY R23, settings version 312)
        public bool EventFaisDoDo = true;   // [World] with TownEvents: Saturday nights in Lagras, the bayou's dance, and a young couple who come away courting (Core/Festivals.cs; FIVEDAY R22, settings version 311)
        public bool EventBrassFuneral = true;   // [World] with TownEvents: the day after a named death in Saint Denis, the society's brass band funeral, the dirge out and the second line back (Core/Festivals.cs; FIVEDAY R21, settings version 310)
        public bool EventMardiGras = true;   // [World] with TownEvents: Mardi Gras night in Saint Denis, the krewe, the king, the king cake (Core/Festivals.cs; FIVEDAY R19, settings version 308)
        public bool EventFourth = true;   // [World] with TownEvents: the Fourth of July in each town of size, the band, the reading, the shooting match, the fireworks (Core/Festivals.cs; FIVEDAY R18, settings version 307)
        public bool EventFair = true;   // [World] with TownEvents: the county fair at Valentine, the third Saturday of September: the judging, the pies, the greasy pig (Core/Festivals.cs; FIVEDAY R17, settings version 305)
        public bool EventBaseBall = true;   // [World] with TownEvents: every other Saturday, April to September, Valentine against Strawberry at base ball, home by turns (Core/BaseBall.cs; FIVEDAY R16, settings version 304)
        public bool EventBarnRaisings = true;   // [World] with TownEvents: March to October, now and then a farm family's barn raised by the neighbours on a Saturday morning; two men at odds put on one beam (Core/BarnRaising.cs; FIVEDAY R7, settings version 295)
        public bool EventBoxSocials = true;   // [World] with TownEvents: the first Saturday of the month, a box social at the church: two suitors bid for a young woman's box, and J lets you bid against them (Core/BoxSocial.cs; FIVEDAY R6, settings version 294)
        public bool EventBaptisms = true;   // [World] with TownEvents and EventRevivals: the Sunday after a camp meeting saves a man, his baptism at the river (the riverbank spot); baptised, he's saved three weeks without backsliding (Core/Baptism.cs; FIVEDAY R5, settings version 293)
        public bool EventRevivals = true;   // [World] with TownEvents: in summer, three nights of a camp meeting outside a country town; the third night's altar call saves a drinker or a gambler for two weeks (Core/Revival.cs; FIVEDAY R4, settings version 292)
        public bool EventShivarees = true;   // [World] with TownEvents: the night after a wedding in town, the couple's friends raise a din until the groom stands them a round (Core/Shivaree.cs; FIVEDAY item 13, settings version 265)
        public bool WhileAway = true;   // [World] back in a town after two game days, what you missed: the box's "What you missed" and a neighbour's thought (Core/WhileAway.cs; FIVEDAY item 19, settings version 271)
        public bool KinTownWrongs = true;   // [Scenes] with KinGrudges: a family takes up a beating or a theft one townsman did another; a man of theirs comes to town a week on and has it out with him (Engine.KinTownDaily; FIVEDAY item 32b, settings version 285)
        public bool FenceCutters = true;   // [World] now and then a homesteader of the open range finds his wire cut and blames the cattleman, and their feud climbs; near that night, you saw who it was (Core/FenceCutters.cs; FIVEDAY item 31, settings version 283)
        public bool SaleDay = true;   // [World] Tuesdays at the Valentine stockyards: the auction, the bids, sometimes a rancher ruined by the price, and the drovers drinking their pay (Core/SaleDay.cs; FIVEDAY item 30, settings version 282)
        public bool Foreclosures = true;   // [World] the day a resident's worry over the bank ends badly, the sheriff's sale of their goods at the bank's door; the neighbours bid low or won't bid at all, and J buys a lot back (Core/Foreclosure.cs; FIVEDAY R3, settings version 291)
        public bool Drummers = true;   // [World] with TrainTime: one man in three off a train is a drummer, a travelling salesman who calls on the store's keeper with his pitch and leaves in two days (Core/Drummers.cs; FIVEDAY R1, settings version 289)
        public bool ShapeUp = true;   // [World] at dawn on the Saint Denis waterfront the foreman picks his gang from the crowd; the passed-over drift to the saloon, and one may take it out on the last man picked (Core/ShapeUp.cs; FIVEDAY item 29, settings version 281)
        public bool EditorSides = true;   // [World] each town's paper's editor adds his word to your deeds in print, in his slant (law and order, the people's, business), and readers near you agree or argue (Core/Editor.cs; FIVEDAY R14, settings version 302)
        public bool DriftingPosters = true;   // [World] the bill outside the sheriff's describes you as the town's rumour has it, true or bent out of shape, and people near hold you up against it (Core/DriftPoster.cs; FIVEDAY R13, settings version 301)
        public bool DimeNovels = true;   // [World] your most-told deed written up as a dime novel (a lurid title in the box), and a month of people near you thinking of having read it (Core/DimeNovel.cs; FIVEDAY R12, settings version 300)
        public bool GoingDry = true;   // [World] 1907: Rhodes and Strawberry dry by local option, the saloon a blind tiger, the law-abiding keeping out (Core/GoingDry.cs; FIVEDAY R27, settings version 316)
        public bool SceneClips = true;   // [Encounters] the scenes' own clips, proven by the clip walker in game (2026-11-09): the execution's guard and victim, the chain gang, the injured rider, the burial (THREEHOUR item 2; settings version 357)
        public bool WaysideComposed = true;   // [Encounters] with [Mind] ComposedActs: someone in a road scene, between what's said and done, shows what they feel in two or three of the game's own moves (TENHOUR item 13; settings version 344)
        public bool WaysideAnswers = true;   // [Encounters] when you walk up on a road scene, one of them decides what to do about you: carry on, warn you off, call you over, ask for help, or turn on you. The AI model's word if it's on (TENHOUR item 11; settings version 343)
        public bool VigilanceCommittees = true;   // [World] with PosseRides: after a killing, a posse that comes home with nothing can leave the town's angrier men to settle on someone, on the road if you're near, off screen if not (TENHOUR item 4; settings version 341)
        public bool PosseFromTheRoad = true;   // [World] with PosseRides: a killing out of town, near enough to a town for word to get there, with someone left to tell it, brings that town's posse (TENHOUR item 3; settings version 340)
        public bool ColdSnaps = true;   // [World] twice a winter, five days of bitter cold in the high country; an old man may not wake, off screen (Core/ColdSnap.cs; TENHOUR R3, settings version 350)
        public bool TownRivals = true;   // [World] a tale crossing to a rival town arrives more bent, and people say what they think of the place (Core/TownRivals.cs; TENHOUR R4, settings version 351)
        public bool WeekColumn = true;   // [World] on a Sunday the paper carries a column of what really happened in the town you're nearest that week (Core/WeekColumn.cs; TENHOUR R5, settings version 352)
        public bool LettersHome = true;   // [World] those who left on the train write to a friend still in the town, and the friend has it on their mind for a few days (Core/LettersHome.cs; TENHOUR R7, settings version 354)
        public bool RoundupSeason = true;   // [World] the fortnight after each roundup, the cattle towns fill with paid-off hands, and one comes back hurt (Core/Roundup.cs; TENHOUR R1, settings version 348)
        public bool PosseRides = true;   // [World] after a bad crime in a town, three to five of its men ride out as a posse, off screen, and come back a day on with nothing, a stranger, or one of them dead (Core/PosseRides.cs; FIVEDAY R32, settings version 321)
        public bool WantedWhich = true;   // [World] a resident whose worry is the wanted poster is recognised within 4 m (once, in the box), then J near them: turn in for the bounty, warn them, buy them a ticket, or leave it (Core/WantedWhich.cs; FIVEDAY R31, settings version 320)
        public bool NeighbourReward = true;   // [World] a resident who beats a man to death in a brawl has a reward posted on him for a week: the town splits, he frets, and near him J offers turn him in, warn him, or leave it (Core/NeighbourReward.cs; FIVEDAY R30, settings version 319)
        public bool HoboSigns = true;   // [World] the tramps' chalk on the water tower by the station, drawn from the tempers of the town's residents, once a town a day (Core/HoboSigns.cs; FIVEDAY R29, settings version 318)
        public bool Relics = true;   // [World] 1907: the photographers sell the old outlaws as cabinet cards, his thoughts and his window (Core/Relics.cs; FIVEDAY R26, settings version 315)
        public bool BoyRemembers = true;   // [World] in 1907, one who was a boy in '99 and met Arthur remembers him as a boy would, and may ask John about him (Core/Epilogue.cs; FIVEDAY R25, settings version 314)
        public bool ClaimAgents = true;   // [World] three days after a mine death or a killing in Saint Denis, the claim agent calls on the widow; she signs, refuses, or the town takes up a collection (Core/ClaimAgent.cs; FIVEDAY R24, settings version 313)
        public bool Fever = true;   // [World] rarely, a fever goes through a town along its warm ties: the sick keep to home a week, a quarantine is posted at three, and with FeverDeaths one in twelve dies (Core/Fever.cs; FIVEDAY R9, settings version 297)
        public bool FeverDeaths = true;   // [World] with Fever: one in twelve of the sick dies of it, mourned like anyone
        public bool MineWhistle = true;   // [World] rarely, three whistles at the Annesburg mine: one or two of its men killed below, off screen; the town runs to the pithead, the names, the grief, the collection (Core/MineWhistle.cs; FIVEDAY item 28, settings version 280)
        public bool Vouch = true;             // [Scenes] at the jail door the morning after a brawl, vouch for the man in the cells (FOURTEENHOUR reserve R22; settings version 410)
        public bool HorseWatch = true;        // [Scenes] down off your horse in town, a man down on his luck offers to watch her for a dime (FOURTEENHOUR reserve R21; settings version 409)
        public bool WidowCollection = true;   // [Scenes] after a death in town a woman takes up a collection for the widow; J to give (FOURTEENHOUR reserve R20; settings version 408)
        public bool StandTheWake = true;   // [Scenes] with BarRound and Wakes: on a wake night, J at the bar can stand a drink to the dead's memory (FOURTEENHOUR reserve R19; settings version 405)
        public bool Wakes = true;   // [World] the night before a burial the neighbours sit up with the dead: the box tells you, the street talks of them, and in West World one swears the dead moved (Core/WakeNight.cs; FIVEDAY item 27, settings version 279)
        public bool AllSaintsGraves = true;   // [World] with Mourning: on All Saints' Day in the south, those with someone dead go to the tombs, the ones you put there among them (TENHOUR R8; settings version 355)
        public bool Mourning = true;   // [World] three weeks of mourning a friend: quieter days and thoughts, hushed neighbours, the grave on a Sunday afternoon (Core/Mourning.cs; FIVEDAY item 26, settings version 278)
        public bool SundayMorning = true;   // [World] Sunday properly: the believers walk to the service, the hymn heard from the street, a sermon on the town's week talked over after, dinner on the grounds (Core/Sunday.cs; FIVEDAY item 25, settings version 277)
        public bool NoticeBoard = true;   // [World] the week ahead on one board: a "Notice board" page in the reader, and what a town has posted in the Around town box as you come in (Core/NoticeBoard.cs; FIVEDAY item 16, settings version 268)
        public bool WitnessesChoose = true;   // [World] each witness to your violence the mind knows tells it or keeps quiet (fear of you, loyalty to you); a gun on one within the hour leans on them (Core/WitnessChoice.cs; FIVEDAY item 14, settings version 266)
        public bool NightScenes = true;   // [Scenes] with MindLive: at night, a mugging by someone whose mind leans to it, and a drunk taken home by a friend (the NPC-life run, step 5; settings version 135)
        public bool Aftermath = true;   // [Scenes] with MindLive: after a street fight near you, a friend tends the one who went down and a lawman near has a word with the one left standing (the NPC-life run, Lex 2026-10-30; settings version 131)
        public float ChanceMeetMetres = 6f;   // [Scenes] two people this far apart may meet by chance, one walking over to the other (3.4, standing together, until settings version 134; Lex, 2026-10-30)
        public int ScenesAtOnce = 4;   // 2 (at most 3) until settings version 140 (Lex, 2026-10-30: more at once);   // [Scenes] street scenes playing at once: the one scene, and quiet talks beside it elsewhere on the street (the NPC-life run, Lex 2026-10-30: "several at once"; settings version 127); 1: just the scene, as before
        public int MainScenesAtOnce = 2;   // [Scenes] full street scenes at once (1-3; at most two play): a second full scene at least 20 m from the first, never the same people, words and walk-overs only (the fights, the rough move and thefts stay the first's); 1: a single scene, as before; apart from ScenesAtOnce's quiet talks (NIGHT4 item 3; settings version 168)
        public bool GangShootouts = true;   // [World] men of two rival gangs who meet near you fight it out: words, a standoff, a shootout (Lex, 2026-10-30; settings version 133)
        public bool WomenInFights = true;   // [Scenes] women may come to blows too, at half a man's chance for each woman in it (Lex, 2026-10-29: "we can allow men to trade blows with women in this mod ... 50% less likely than a man getting in a fight until we have the innermind logic take over"; night 2026-10-29, item 2b; settings version 107); false: two men only, as before
        public bool NpcThieves = true;
        public bool CriminalClass = true;    // people who take to crime keep to it: a theft to their name this month makes them likelier to go for a pocket again, known as a thief the whole month, a drunk rolled with its own words, and gone straight after thirty clean days (Core/Criminals.cs; roadmap 28; SIXDAY item 16; settings version 232)
        public bool DrunkChallenges = true;
        public bool BarkeepSteps = true;     // the barkeep steps in on a saloon brawl or rough as they square up: heeded, they take it outside and the blows never come; else he's shoved off and swears to fetch the sheriff (Core/Scenes.Trouble.cs BarkeepSteps; roadmap 7; SIXDAY item 17; settings version 233)
        public bool DrunkOutside = true;     // a drunk's challenge taken up is really taken outside: the two walk to the saloon's door you last came out of and the fight is in the street (Scenes.DrunkOutside, the Director's "outside"; SIXDAY item 17)
        public bool Reactions = true;
        public bool Acknowledge = true;
        public bool SightReactions = true;
        public bool StreetReacts = true;      // [Scenes] the street reacts to other people's fights: the moments of a scene between others (blows, squaring up, a gun out, a man killed or knocked out) are raised as the game's own shocking events, so its crowd does what it does for yours; held in a town for 30 game minutes when one brings the law into combat, a gun out of someone else's holster or people fleeing beside you (Core/Street.cs; roadmap 17; SIXDAY item 5b; settings version 212)
        public bool StreetReputation = true;  // [Scenes] reputation in the street: an outlaw walking among people (enRAGE's gunmen, a resident wanted by the law) is looked after, stepped around, turned from or stared down by their natures; the doctor, a preacher, the law and a keeper out of doors are greeted; one witness mutters it; a known killer or peacemaker gets a thought from a witness, never a task (Core/Reputation.cs; SIXDAY item 5c; settings version 212)
        public bool Kindnesses = true;
        public bool Vices = true;
        public bool Grudges = true;
        public bool KinGrudges = true;       // a wrong of yours on a known person brings a man of their family, whom you never met, to town five to nine days on to say it to your face (Core/KinGrudges.cs; roadmap 20; SIXDAY item 14; settings version 230)
        public bool ReasonThoughts = true;   // [Scenes] when a street scene starts for a reason between two people, the one whose reason it is thinks why as they go over (settings version 155; NIGHT3)
        public bool CourtFolkAges = true;    // [Scenes] enRAGE's townsfolk, whose ages it doesn't keep, may court each other on the street (an age guessed from who they are; Reasons.CourtGuessAges; settings version 170)
        public bool WarnedYou = true;        // [Scenes] someone who saw Arthur's violence may tell him to his face what they think of it; someone who thinks well of him may warn him of a man with a grudge, or the law asking (words only; with MindLive; settings version 170)
        public bool FavoursAsked = true;     // [Scenes] someone at the worst of a money trouble (a debt, a tooth, a fare) may come and ask Arthur; Talk then lists the fix (with MindLive; settings version 170; NIGHT4 item 5)
        public bool LonelyRegulars = true;   // [Scenes] someone lonely who knows Arthur comes over hoping for a word; glad if he stops to talk, stung if he walks on (with MindLive; settings version 157; NIGHT3)
        public bool BeingWatched = true;   // [Scenes] the street's own trouble (thefts, shoves, fights, standoffs, the noon callout) waits while a lawman has the one who'd start it in sight, or on a Sunday morning by the church; they think why (settings version 158; NIGHT3)
        // NIGHT3's numbers as settings (For Lex item 8; Lex, 2026-10-31; settings version 163): each default is the number built
        public float WatchedLawMetres = 25f;       // [Scenes] a lawman this near, facing them with a clear sight, is watching
        public int WatchedRecklessChance = 40;     // [Scenes] a hothead or a drunk goes ahead anyway (percent, one roll a day)
        public float WatchedChurchMetres = 60f;    // [Scenes] "by the church" on a Sunday morning
        public float LonelyNeed = 0.65f;           // [Scenes] the company need (0-1) at least this for someone to come over hoping for a word
        public int LonelyLookSeconds = 45;         // [Scenes] how often a lonely one is looked for in town
        public float LonelyMetres = 18f;           // [Scenes] how near they must be
        public int LonelyWaitSeconds = 15;         // [Scenes] how long they stand hoping
        public int LonelyRestDays = 3;             // [Scenes] walked past: game days before they try again
        public int ThanksAfterHours = 6;           // [Scenes] late thanks: game hours at least after the kindness
        public int ThanksWithinDays = 7;           // [Scenes] ... and game days at most
        public int ThanksDrinkChance = 15, ThanksDollarChance = 35, ThanksFiveChance = 60, ThanksMendedChance = 90;   // [Scenes] percent thanked later: a drink, a dollar, five dollars, a worry mended
        public int ThanksLoanDays = 3;             // [Scenes] the proud pay a loan back after this many game days
        public float SpaceMetres = 0.95f, SpaceSaloonMetres = 0.75f;   // [World] personal space: nearer than this (at a saloon's bar, this) ...
        public float SpaceSeconds = 1.8f;          // [World] ... both still this long, and it's minded
        public int BullyTurnsHothead = 55, BullyTurnsProud = 40, BullyTurnsColdGreedy = 25, BullyTurnsCoward = 3, BullyTurnsOthers = 15;   // [Mind] percent a bully holding his ground turns on whoever stepped in, by his temper
        public int BullyTurnsDrunk = 15, BullyTurnsMeant = 10;   // [Mind] more when he's drunk; less when the one who stepped in looks like he means it (Proud, Hothead)
        public float OnlookersRunMetres = 20f;     // [Mind] how far the badly frightened run
        public int OnlookersRunning = 4;           // [Mind] how many may run from one happening (fewer with Bravery)
        public float TalkModelWaitSeconds = 2.8f;  // [Mind] Talk: how long the model may take before the written line
        public float HushHours = 0f, HushHoursSaintDenis = 0f;   // off as it comes (Lex, 2026-10-31: "remove that hush feature altogether" for a livelier world); 2 and 1 are the realistic values   // [Mind] game hours the street hushes after the latest killing in its town (0-12; 0 never); a beating in a brawl, a Testing row's own trouble (the player's killings only of the test's own people), brawls, roughs and standoffs never hush (settings version 165; HushFear and HushFearSaintDenis before, Lex in Saint Denis 2026-10-31)
        public int StrainChance = 40;              // [Mind] stress that builds: percent they snap at someone who won't hit back (a coward always)
        public int BoldThieves = 25;   // with BeingWatched: percent of would-be thieves who try it with the law looking on, once a day each; the hothead, the greedy and the drunk half as likely again (Lex, 2026-10-31: "less realistic, more exciting"; settings version 163)
        public bool LateThanks = true;   // [Scenes] someone Arthur stood a drink, gave money or whose debt he settled comes over hours or days later and thanks him for it; the proud repay a debt (settings version 153; NIGHT3)
        public bool FriendCalls = true;   // [Scenes] a friend in town comes over unasked now and then: a warning, the news, a small gift or a favour of their trade (Core/FriendCalls.cs; Run 3, item 7; settings version 254)
        public bool FriendGifts = true;   // [Scenes] with FriendCalls: gifts and favours hand something over (a few cents, cartridges, a meal, a drink, the doctor's patching); off, friends bring words only
        public bool Stories = true;          // [World] a story over days: a father whose son is at the Annesburg mine writes to you (FOURTEENHOUR reserve R10; settings version 403)
        public bool GameEncounters = true;   // [World] the game's own encounters near you are seen by the people of the mind and told in the town's talk (FOURTEENHOUR reserve R8; settings version 401)
        public bool BestFist = true;          // [Scenes] three fights won in a town make a man its best fist; men from other towns come to try him at the prize fight (FOURTEENHOUR reserve R7; settings version 400)
        public bool CardsharpRunOut = true;   // [Scenes] a card cheat shown the door is walked out past the last houses by the table's men, and comes back another day, sore (FOURTEENHOUR reserve R6; settings version 399)
        public bool RowdyDrunks = true;     // [Scenes] at night two friends in drink come down the street singing; one picks on a man going by; the friend hauls him home, or it comes to blows (FOURTEENHOUR reserve R5; settings version 398)
        public bool DoctorsDoor = true;     // [Scenes] after dark a man runs for the doctor for his child's fever; J to walk him home (FOURTEENHOUR reserve R4; settings version 397)
        public bool DoctorsPatient = true;   // [Scenes] someone goes to the doctor for what ails them, tells him, hears his word, and walks home (night 2026-10-28, item 14; widened 2026-10-29; settings version 90)
        public float ErrandPace = 2.0f;   // [Scenes] how fast people walk on errands for RealNPCs: 1 a walk, 2 brisk (was about 1-1.4), 3 a run (Lex, 2026-10-29)
        public bool LawChases = true;   // [Scenes] a lawman near runs down a pickpocket of ours and brings him down (Lex, 2026-10-29)
        public int LawShootsThieves = 50;   // [Scenes] percent: with LawChases, the lawman shoots the one who picked your pocket instead of running him down (Lex, 2026-11-01); settings version 184
        public bool Lifts = true;   // [Scenes] a man bumps into you in town and lifts some of your cash; catch him to get it back (Lex, 2026-10-29); needs Pickpockets on too (settings version 428)
        public bool FriendsWarnThief = true;   // [Scenes] ... with FriendCalls: a friend who comes over may warn you of a known pickpocket near, and you're on your guard 20 minutes (no lift tried) (DEEPEN item 3; settings version 432)
        public bool FriendsChase = true;   // [Scenes] ... a friend of yours near (as the allies count one) may run down the man who robbed you and bring your money back (Core/FriendChase.cs; DEEPEN item 2; settings version 431)
        public bool LiftsWary = true;   // [Scenes] ... 24 game hours between at least, 48 in one town (not every four game hours), and fewer try it where your killings are known (Core/LiftNerve.cs; a player on Nexus, 2026-11-10: "pickpocketed pretty much every time I visited"; settings version 430)
        public bool EnragePeopleJoinIn = true;
        public bool Standoffs = true;
        public bool Songs = true;
        public bool TallTales = true;
        public bool NightTales = true;   // [Scenes] at night the old tales are told: in the saloon, the street after dark and at a campfire (Core/Folklore.cs; Run 3, item 3; settings version 251)
        public bool NewspaperTalk = true;
        public bool Haggling = true;
        public bool SnakeOil = true;
        public bool SmallTalk = true;
        public bool Pickpockets = true;   // [Scenes] a pickpocket strikes near you; off also stops the lift on you, Lifts (a player on Nexus, 2026-11-10; settings version 428)
        public bool PosseTalk = true;
        public bool Mourners = true;
        public bool Pleading = true;
        public bool GunpointStages = true;   // with Pleading: the gun still on them, the second step six seconds on by who they are (beg, defy, back away, run, freeze; the model's pick when it's on) and the third at fourteen (Core/Gunpoint.cs; roadmap 38; SIXDAY item 20; settings version 236)
        public bool ArrivalTalk = true;

        // [Pacing]
        public int ThoughtEverySeconds = 300;
        public int ToastGapSeconds = 45;
        public int LifeUpdateGapSeconds = 180;
        public int RemarkGapSeconds = 40;
        public int RemarkSamePersonSeconds = 600;
        public float RemarkDistance = 7f;
        public int SubtitleSeconds = 5;
        public int AmbientFlirtSeconds = 240;

        // [Output]
        public string NewsTo = "corner";
        public int AroundTownStaysSeconds = 30;   // [Display] the regard line keeps the box up this long after arriving or a change (0: always while in a town); players found the box up too much (2026-11-07; settings version 306)
        public bool TownRegard = true;   // [Display] the town's regard of you in the "Around town" box: a line under the title while you're in a town (the standing, the killings known, the kindnesses) and an item on coming in and when it changes (the mood, who you know here is afraid or fond, the talk) (Core/TownRegard.cs; Lex, 2026-11-04; settings version 244)
        public string Subtitles = "game";   // game | ours | both | off (2026-10-28)
        public bool Translate = true;   // [Output] translation.txt in the RealNPCs folder, when there is one, shows RealNPCs' lines in your language (Core/Translations.cs; Run 3, item 8; settings version 255)
        public bool UseGameFeed;
        public string TestKey = "F7";
        public string WarNewsTo = "feed";
        public bool TerritoryNews = true;
        public string FlirtKey = "None";   // F2 until 2026-10-29 (Lex: H flirts in the lock-on menu, and after her yes the same H hooks up; no keyboard key)
        public string WakeKey = "Y";
        public string ReaderKey = "N";
        public bool ReloadIni = true;
        public bool ReplaceEnrageSettings = false;   // off as it comes from settings version 177 (Lex, 2026-11-01: Alt+F11 is enRAGE's own settings menu)
        public string SettingsKey = "Alt+F";   // the one key from settings version 177 (Lex, 2026-11-01: "only Alt-F"); Alt+R before, Ctrl+F7 until 2026-10-29
        public string SettingsKey2 = "Alt+R";   // the second key (Lex, 2026-11-01: "add back in Alt-R"; None in 177 alone, Alt+F before, Alt+R from 98 to 101)
        public bool PanelSlowsTime = true;    // the world slows while the settings panel is open, as it does for the game's wheels (Lex, 2026-10-27; settings version 84)
        public float PanelTimeScale = 0f;   // 0: paused (Lex, 2026-10-30; 0.25 until settings version 125)
        // [Controller] (Lex, 2026-10-28; settings version 86): a controller's buttons for RealNPCs' own keys, a button
        // held and another pressed (Core/PadChord.cs); read only while a controller is the last thing touched
        public bool PadKeys = true;
        public string PadSettings = "RB+DPadUp";
        public string PadStatus = "RB+DPadLeft";
        public string PadLetters = "RB+DPadRight";
        public string PadTickerNext = "None";
        public bool DebugLog;
        public bool PerfMeter = true;

        // [Testing] (overnight 2026-10-27): the Testing page's keys (Core/TestActions.cs). The ones that were settings
        // already (StalkerKey, WakeKey, ExecutionKey...) stay fields; the rest are kept here by their ini name.
        public bool TestKeys = true;
        public bool ProofTour = true;          // [Testing] the proof tour row walks every Testing row that can run where you stand, each in its turn, each logged as PROOF ran, skipped or failed (Core/ProofTour.cs; DEPTH item 2; settings version 411)
        public bool ProofTourRisky = false;    // [Testing] ... and the risky rows too: real money, danger to you, a death, a probe (off: passed over, and the log says why)
        public int ProofTourGapSeconds = 20;   // [Testing] ... a row about every this many seconds (8-120), once the last one is no longer under way
        public bool ShowMinds = false;   // off as it comes (Lex, 2026-10-28; on from settings version 77 to the night of 10-28)
        public bool ShowWakeStages;   // [Testing] a tag over each person who has begun to wake (2026-10-27)
        public bool TestCastReturns = false;  // [Testing] the test cast kept in the save come back by themselves when you come to their town; off as it comes since 2026-10-29 (Lex: they should come only on the row's key; an ini that says true is set false once, version 96) (night 2026-10-28; settings version 90)
        public Dictionary<string, string> TestKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// The key text a Testing action is set to: its field, its line in [Testing], or its default.
        public string TestKeyOf(TestActions.Entry e)
        {
            if (e == null)
                return "None";
            if (e.Field)
            {
                FieldInfo f = typeof(Settings).GetField(e.KeySetting, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                string v = f != null ? f.GetValue(this) as string : null;
                return string.IsNullOrEmpty(v) ? e.DefaultKey : v;
            }
            string t;
            return TestKeyMap.TryGetValue(e.KeySetting, out t) && !string.IsNullOrEmpty(t) ? t : e.DefaultKey;
        }

        /// Where notices go: "corner" (the "Around town" box), "ticker" or "gamefeed" (the game's own top-left updates).
        /// The old routes that were white text (subtitles, enRAGE's notice line) go to the box, as does the ticker when
        /// it's off.
        public string NewsRoute
        {
            get
            {
                string n = (NewsTo ?? "corner").Trim().ToLowerInvariant();
                if (n == "off")
                    return "off";   // no Around town box at all (players, 2026-11-07: "How do I disable the window in the top right corner?")
                if (n != "ticker" && n != "gamefeed")
                    n = "corner";
                if (UseGameFeed && n == "ticker")
                    n = "gamefeed";
                if (n == "ticker" && !Ticker)
                    n = "corner";
                return n;
            }
        }

        /// Where gang and faction news goes: "feed" (the game's own top-left updates, the default; "right" was the old
        /// name for them), "corner" (the "Around town" box) or "ticker" (the box when the ticker is off).
        public string WarRoute
        {
            get
            {
                string n = (WarNewsTo ?? "feed").Trim().ToLowerInvariant();
                if (n == "ticker")
                    return Ticker ? "ticker" : "corner";
                return n == "corner" ? "corner" : "feed";
            }
        }

        /// Takes every value from another Settings (an ini reload keeps this same object, which the engine also holds).
        /// What the game actually runs with: the values from the ini, with the master switches applied (West World
        /// off turns off everything under it). Never written back to the ini, so the player's own values are kept.
        public Settings Gated()
        {
            Settings g = new Settings();
            g.CopyFrom(this);
            if (!g.WestWorld)
            {
                g.AwakeningShare = 0;
                g.AwakeningDespair = 0;
                g.HostsQuestionReality = false;
                g.Glitches = false;
                g.Reveries = false;
                g.ReturnChance = 0;
                g.StrangerReturnChance = 0;
                g.Hermits = false;      // the hermits and the gatherings are the awake: West World things
                g.Gatherings = false;
            }
            if (!g.Encounters)
            {
                g.CryingWoman = false;
                g.NightVisitor = false;
                g.Travellers = false;
                g.SupperInvitation = false;
                g.Executions = false;
                g.PublicCallouts = false;
                g.DomesticDisputes = false;
                g.Hogtied = false;
                g.CalloutsArthur = false;
                g.Hermits = false;
                g.Gatherings = false;
            }
            return g;
        }

        public void CopyFrom(Settings other)
        {
            if (other == null)
                return;
            foreach (FieldInfo f in typeof(Settings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                f.SetValue(this, f.GetValue(other));
        }

        public static DateTime Stamp(string path)
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// An ini still holding an old default gets the new one; a value of the player's own is kept (but a setting whose
        /// meaning changed starts again from its default).
        private static void RaiseOldDefaults(Settings s, Action<string> log)
        {
            List<string> raised = new List<string>();
            if (s.SettingsVersion < 356)
            {
                // 2026-11-09 (Lex): names drawn nearer, for the frame rate: the name cards were the costliest thing in the first session
                // with this build (0.17-0.36 ms a frame), and every person within the larger distance plus 20 m is looked at each pass.
                // A distance of your own is kept.
                if (Math.Abs(s.NameCardsMetres - 25f) < 0.01f) { s.NameCardsMetres = 15f; raised.Add("NameCardsMetres 25 -> 15"); }
                if (Math.Abs(s.NameCardsKnownMetres - 75f) < 0.01f) { s.NameCardsKnownMetres = 30f; raised.Add("NameCardsKnownMetres 75 -> 30"); }
            }
            if (s.SettingsVersion < 17)
            {
                if (s.AwakeningDays == 6) { s.AwakeningDays = 1; raised.Add("AwakeningDays 6 -> 1"); }
                if (s.AwakeningBreakdown == 35) { s.AwakeningBreakdown = 90; raised.Add("AwakeningBreakdown 35 -> 90"); }
                if (s.AwakeningDespair == 2 || s.AwakeningDespair == 10) { raised.Add("AwakeningDespair " + s.AwakeningDespair + " -> 75"); s.AwakeningDespair = 75; }
                if (s.ReturnChance == 15) { s.ReturnChance = 35; raised.Add("ReturnChance 15 -> 35"); }
                if (s.StrangerReturnChance == 5) { s.StrangerReturnChance = 15; raised.Add("StrangerReturnChance 5 -> 15"); }
            }
            if (s.SettingsVersion < 18 && s.StalkerWhenStill == "circle") { s.StalkerWhenStill = "mix"; raised.Add("StalkerWhenStill circle -> mix (the new odd acts, among the rest)"); }
            if (s.SettingsVersion < 19 && s.StalkerChance != 20)
            {
                // it was a chance per in-game day; now it is per roll, every StalkerEveryMinutes: back to the default
                raised.Add("StalkerChance " + s.StalkerChance + " -> 20 (now a chance per roll, every " + s.StalkerEveryMinutes + " minutes)");
                s.StalkerChance = 20;
            }
            if (s.SettingsVersion < 20)
            {
                // the keys moved: the wake key from G to Y, the stalker key from 0 to ] (a key of your own is kept)
                if (KeyIs(s.WakeKey, "G")) { s.WakeKey = "Y"; raised.Add("WakeKey G -> Y"); }
                if (KeyIs(s.StalkerKey, "0") || KeyIs(s.StalkerKey, "D0")) { s.StalkerKey = "]"; raised.Add("StalkerKey 0 -> ]"); }
            }
            if (s.SettingsVersion < 25 && KeyIs(s.ReaderKey, "F5"))
            {
                s.ReaderKey = "-";   // the reader moved from F5 to the minus key (a key of your own is kept)
                raised.Add("ReaderKey F5 -> -");
            }
            if (s.SettingsVersion < 26 && (KeyIs(s.ReaderKey, "-") || KeyIs(s.ReaderKey, "OemMinus")))
            {
                s.ReaderKey = "N";   // ...and on to N (a key of your own is kept)
                raised.Add("ReaderKey - -> N");
            }
            if (s.SettingsVersion < 28 && s.StalkerEveryMinutes == 10)
            {
                s.StalkerEveryMinutes = 34;   // the roll for a stalker comes less often now (a value of your own is kept)
                raised.Add("StalkerEveryMinutes 10 -> 34");
            }
            if (s.SettingsVersion < 38)
            {
                // 0.6.2: the ticker keeps only the thoughts of people you can see; news has its own box
                if (KeyIs(s.NewsTo, "ticker")) { s.NewsTo = "corner"; raised.Add("NewsTo ticker -> corner"); }
                if (KeyIs(s.WarNewsTo, "right")) { s.WarNewsTo = "corner"; raised.Add("WarNewsTo right -> corner"); }
            }
            if ((s.SettingsVersion < 48 && KeyIs(s.FlirtKey, "F6")) || (s.SettingsVersion == 48 && KeyIs(s.FlirtKey, "5")))
            {
                // F6 is the game's camera, and 5 (the default for one version) takes out the melee slot of the item
                // wheel; a key of your own is kept
                raised.Add("FlirtKey " + s.FlirtKey + " -> None");
                s.FlirtKey = "None";   // F2 until 2026-10-29: flirting is H in the lock-on menu now
            }
            if (s.SettingsVersion < 46)
            {
                // 0.6.2: no white text any more. Gang and faction news goes to the game's own top-left updates, notices
                // that were white text (subtitles, enRAGE's notice line) to the box, and subtitles are the game's own
                if (KeyIs(s.WarNewsTo, "corner") || KeyIs(s.WarNewsTo, "right")) { raised.Add("WarNewsTo " + s.WarNewsTo + " -> feed"); s.WarNewsTo = "feed"; }
                if (KeyIs(s.NewsTo, "subtitles") || KeyIs(s.NewsTo, "enrage")) { raised.Add("NewsTo " + s.NewsTo + " -> corner"); s.NewsTo = "corner"; }
            }
            if (s.SettingsVersion < 57 && Math.Abs(s.TickerNearbyMetres - 30f) < 0.01f)
            {
                // 0.6.2: thoughts show above heads now, and a nearer circle keeps them to people you can read (a value of
                // your own is kept)
                s.TickerNearbyMetres = 20f;
                raised.Add("TickerNearbyMetres 30 -> 20");
            }
            if (s.SettingsVersion < 83)
            {
                // Show minds has a key of its own since version 83 (Lex, 2026-10-27: on as it comes, with a key to turn
                // it off and on); an ini's "None" was the old default, a key of your own is kept
                string k;
                if (!s.TestKeyMap.TryGetValue("TestShowMindsKey", out k) || string.IsNullOrEmpty(k) || KeyIs(k, "None"))
                {
                    s.TestKeyMap["TestShowMindsKey"] = "Ctrl+F2";
                    raised.Add("TestShowMindsKey None -> Ctrl+F2");
                }
            }
            if (s.SettingsVersion < 87 && s.DramaGapSeconds == 150)
            {
                // the old default (Lex, 2026-10-28: scenes between people fairly often); a gap of your own is kept
                s.DramaGapSeconds = 90;
                raised.Add("DramaGapSeconds 150 -> 90");
            }
            if (s.SettingsVersion < 161)
            {
                // 2026-10-31 (Lex): "have the stalker appear more often"; the defaults as built move, values of your own are kept
                if (s.StalkerEveryMinutes == 34) { s.StalkerEveryMinutes = 15; raised.Add("StalkerEveryMinutes 34 -> 15"); }
                if (s.StalkerChance == 20) { s.StalkerChance = 35; raised.Add("StalkerChance 20 -> 35"); }
            }
            if (s.SettingsVersion < 159)
            {
                // 2026-10-31 (Lex): fewer stirring, a little; the waking more from what they live through and the model's
                // own wondering ([Mind] ModelWakes). The two shares as built go down a quarter; a value of your own is kept
                if (Math.Abs(s.AwakeningShare - 3f) < 0.01f) { s.AwakeningShare = 2.25f; raised.Add("AwakeningShare 3 -> 2.25"); }
                else if (Math.Abs(s.AwakeningShare - 0.6f) < 0.01f) { s.AwakeningShare = 0.45f; raised.Add("AwakeningShare 0.6 -> 0.45"); }
            }
            if (s.SettingsVersion < 140 && s.ScenesAtOnce == 2)
            {
                s.ScenesAtOnce = 4;   // 2026-10-30 (Lex): more of the street at once; a number of your own is kept
                raised.Add("ScenesAtOnce 2 -> 4");
            }
            if (s.SettingsVersion < 138)
            {
                // 2026-10-30 (Lex): fewer waking, and slowly; a value of your own is kept
                if (Math.Abs(s.AwakeningShare - 5f) < 0.01f) { s.AwakeningShare = 3f; raised.Add("AwakeningShare 5 -> 3"); }
                if (s.AwakeningDays == 1) { s.AwakeningDays = 5; raised.Add("AwakeningDays 1 -> 5"); }
            }
            if (s.SettingsVersion < 129 && Math.Abs(s.NameCardsMetres - 30f) < 0.01f)
            {
                s.NameCardsMetres = 25f;   // 2026-10-30 (Lex): names within 25 m; a distance of your own is kept
                raised.Add("NameCardsMetres 30 -> 25");
            }
            if (s.SettingsVersion < 126 && !s.MindLive)
            {
                s.MindLive = true;   // 0.7.2 (Lex, 2026-10-30): the mind acts for everyone; it was off while it was tested
                raised.Add("MindLive false -> true (on for everyone from 0.7.2)");
            }
            if (s.SettingsVersion < 125 && Math.Abs(s.PanelTimeScale - 0.25f) < 0.001f)
            {
                s.PanelTimeScale = 0f;   // 2026-10-30 (Lex): the panel pauses the world; a speed of your own is kept
                raised.Add("PanelTimeScale 0.25 -> 0 (paused)");
            }
            if (s.SettingsVersion < 120 && Math.Abs(s.ScenesOnTickerMetres - 30f) < 0.01f)
            {
                s.ScenesOnTickerMetres = 12f;   // 2026-10-30 (Lex): the subtitles only for people near you; a distance of your own is kept
                raised.Add("ScenesOnTickerMetres 30 -> 12");
            }
            if (s.SettingsVersion < 101 && s.DramaGapSeconds == 90)
            {
                s.DramaGapSeconds = 60;   // 2026-10-29 (Lex): "these NPC interactions happening pretty frequently to make the world feel lively"
                raised.Add("DramaGapSeconds 90 -> 60");
            }
            if (s.SettingsVersion == 81 && s.SupperInvitation)
            {
                // version 81 had the supper invitation on as it came, for an hour; off until it's been tested (Lex,
                // 2026-10-27), so an 81 ini's "on" was that default, not a choice
                s.SupperInvitation = false;
                raised.Add("SupperInvitation on -> off (until it's been tested)");
            }
            if (s.SettingsVersion < 172)
            {
                // off was the stock value until version 172 (Lex, 2026-11-01: violence against Arthur on by default)
                if (!s.SupperInvitation) { s.SupperInvitation = true; raised.Add("SupperInvitation off -> on (violence against Arthur on by default)"); }
                if (!s.HogtiedMenDraw) { s.HogtiedMenDraw = true; raised.Add("HogtiedMenDraw off -> on (violence against Arthur on by default)"); }
            }
            if (s.SettingsVersion < 249 && string.Equals((s.LanguageModel ?? "").Trim(), EnrageWestworld.Core.Language.OldDefaultModel, StringComparison.OrdinalIgnoreCase))
            {
                // the stock model until version 249 (Lex, 2026-11-04, "Yes for now": a smaller model, about 2 GB of the card in
                // place of 5, so the game keeps its room); a model of your own is kept, and until the 3B is pulled the 8B goes
                // on being used (Language.ModelToUse), so nobody's AI goes quiet for it
                s.LanguageModel = EnrageWestworld.Core.Language.DefaultModel;
                raised.Add("LanguageModel " + EnrageWestworld.Core.Language.OldDefaultModel + " -> " + EnrageWestworld.Core.Language.DefaultModel + " (a smaller model; until you run ollama pull " + EnrageWestworld.Core.Language.DefaultModel + " the 8B you have goes on being used)");
            }
            if (s.SettingsVersion < 252)
            {
                // the waking tuned up (Run 3, item 4; Lex, 2026-11-04: "I haven't seen many despair ending recently, we can tune up
                // the amount of awake people, and frequency"); values of your own are kept
                if (Math.Abs(s.AwakeningShare - 2.25f) < 0.01f) { s.AwakeningShare = 3f; raised.Add("AwakeningShare 2.25 -> 3"); }
                if (s.AwakeningDays == 5) { s.AwakeningDays = 3; raised.Add("AwakeningDays 5 -> 3"); }
                if (s.AwakeningDespair == 50) { s.AwakeningDespair = 65; raised.Add("AwakeningDespair 50 -> 65"); }
            }
            if (raised.Count > 0)
                log?.Invoke("Settings: brought up to date: " + string.Join(", ", raised) + ".");
        }

        private static bool KeyIs(string value, string key)
        {
            return value != null && value.Replace(" ", "").Equals(key, StringComparison.OrdinalIgnoreCase);
        }

        public static Settings Load(string path, Action<string> log)
        {
            Settings s = new Settings();
            if (!File.Exists(path))
            {
                Write(path, s);
                return s;
            }
            s.SettingsVersion = 1;
            var testSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    int comment = value.IndexOf(" ;", StringComparison.Ordinal);
                    if (comment >= 0)
                        value = value.Substring(0, comment).Trim();
                    if (value.StartsWith(";"))
                        value = string.Empty;
                    if (TestActions.ByKey(key) != null)
                        testSeen.Add(key);
                    FieldInfo f = typeof(Settings).GetField(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                    if (f == null)
                    {
                        if (TestActions.ByKey(key) != null)
                            s.TestKeyMap[key] = value;   // one of the Testing page's keys (Core/TestActions.cs)
                        continue;
                    }
                    try
                    {
                        if (f.FieldType == typeof(bool))
                            f.SetValue(s, value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" ||
                                          value.Equals("on", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
                        else if (f.FieldType == typeof(int))
                            f.SetValue(s, int.Parse(value, CultureInfo.InvariantCulture));
                        else if (f.FieldType == typeof(float))
                            f.SetValue(s, float.Parse(value, CultureInfo.InvariantCulture));
                        else
                            f.SetValue(s, value);
                    }
                    catch
                    {
                        log?.Invoke("Settings: couldn't read " + key + " = " + value + " (kept " + f.GetValue(s) + ")");
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Invoke("Settings: couldn't read " + path + ": " + ex.Message);
                return s;
            }
            try
            {
                s.Clamp();   // a value Clamp can't take (night 2026-10-28: the controller's "+") must not escape the script's constructor
            }
            catch (Exception ex)
            {
                log?.Invoke("Settings: couldn't clamp " + path + ": " + ex.Message);
            }
            bool testMissing = false;
            foreach (TestActions.Entry te in TestActions.All)
                testMissing |= !testSeen.Contains(te.KeySetting);
            if (testMissing && s.SettingsVersion >= CurrentVersion)
            {
                // a Testing key missing from the file: written again with all of them, values kept and nothing moved (lowering the
                // version for it ran the last version's default moves again over values of the player's own; the review, 2026-10-31)
                s.SettingsVersion = CurrentVersion;
                Write(path, s);
                log?.Invoke("Settings: " + Path.GetFileName(path) + " written again with every Testing row (your values kept).");
            }
            else if (s.SettingsVersion < CurrentVersion)
            {
                if (s.SettingsVersion < 3 && s.TestKey != null && s.TestKey.Replace(" ", "").Equals("Ctrl+F7", StringComparison.OrdinalIgnoreCase))
                    s.TestKey = "F7";
                if (s.SettingsVersion < 6 && s.StalkerChance == 8)
                    s.StalkerChance = 20;   // the old default: stalkers were too rare to ever meet
                if (s.SettingsVersion < 8 && (s.StalkerMinutes == 8 || s.StalkerMinutes == 60))
                    s.StalkerMinutes = 120; // the old default and the old ceiling: a stalker now stays up to two hours
                if (s.SettingsVersion < 96 && s.TestCastReturns)
                {
                    s.TestCastReturns = false;   // 2026-10-29 (Lex): the test cast come only on the row's key, not by themselves at every visit
                    log?.Invoke("Settings: TestCastReturns set off (the test cast come only on the Testing row's key now); set it on again if you want them back by themselves).");
                }
                if (s.SettingsVersion < 102)
                {
                    // 2026-10-29 (Lex): Alt+R opens the settings and Alt+F is the second key; Ctrl+F7 goes. An ini on any of the old
                    // defaults (Ctrl+F7 and Alt+F, or the afternoon's Ctrl+F7 and Alt+R) is moved; a key of the player's own is kept.
                    string k1 = s.SettingsKey, k2 = s.SettingsKey2;
                    bool old1 = KeyIs(k1, "Ctrl+F7") || KeyIs(k1, "Alt+F") || KeyIs(k1, "Alt+R");
                    bool old2 = KeyIs(k2, "Ctrl+F7") || KeyIs(k2, "Alt+F") || KeyIs(k2, "Alt+R");   // an empty or None second key is the player's choice: kept
                    if (old1)
                        s.SettingsKey = "Alt+R";
                    if (old2 || KeyIs(k2, s.SettingsKey))
                        s.SettingsKey2 = KeyIs(s.SettingsKey, "Alt+F") ? "Alt+R" : "Alt+F";
                    if (old1 || old2)
                        log?.Invoke("Settings: the settings panel's keys are " + s.SettingsKey + " and " + s.SettingsKey2 + " (were " + k1 + " and " + k2 + "; Ctrl+F7 opens nothing now).");
                }
                if (s.SettingsVersion < 177)
                {
                    // 2026-11-01 (Lex): one key, Alt+F, and Alt+F11 back to enRAGE's own menu. An ini on the old pair (Alt+R and Alt+F)
                    // moves; a key of the player's own is kept.
                    if (KeyIs(s.SettingsKey, "Alt+R") && (KeyIs(s.SettingsKey2, "Alt+F") || KeyIs(s.SettingsKey2, "None") || string.IsNullOrEmpty(s.SettingsKey2)))
                    {
                        s.SettingsKey = "Alt+F";
                        s.SettingsKey2 = "Alt+R";
                        log?.Invoke("Settings: the settings panel opens on Alt+F, and Alt+R as well; Alt+F11 is enRAGE's own.");
                    }
                    if (s.ReplaceEnrageSettings)
                    {
                        s.ReplaceEnrageSettings = false;
                        log?.Invoke("Settings: Alt+F11 opens enRAGE's own settings menu again (ReplaceEnrageSettings off); Alt+F opens RealNPCs', enRAGE's settings on its enRAGE page.");
                    }
                }
                if (s.SettingsVersion < 179)
                    log?.Invoke("Settings: [Mind] MindStartsScenes works without the AI model now: people near you start scenes from their own reasons (a debt to call in, a friend to comfort, a quarrel), on top of the street's own, with the written why.");
                if (s.SettingsVersion < 180)
                    log?.Invoke("Settings: new in this version: [Mind] SeatedBrawls (on): two people sat or leaning at a bar or a table may come to blows, getting up first the game's own way (before, their quarrel stayed words).");
                if (s.SettingsVersion < 181)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlOpeningBoost (20): in a fight's first 10 seconds onlookers are readier still to join in, so two at it grow into a brawl more often.");
                if (s.SettingsVersion < 183)
                    log?.Invoke("Settings: thoughts vary over an hour of play now, not a few minutes; new in this version: [Mind] SaidLately (on): the street's talk passes over lines said lately.");
                if (s.SettingsVersion < 184)
                    log?.Invoke("Settings: new in this version: [Scenes] LawShootsThieves (50): a lawman near may shoot the one who picked your pocket instead of running him down.");
                if (s.SettingsVersion < 451)
                    log?.Invoke("Settings: new in this version: [Scenes] FindTheLetter (on): carry a waiting man's letter from another town's depot.");
                if (s.SettingsVersion < 450)
                    log?.Invoke("Settings: new in this version: [Scenes] SavedKnown (on): someone you save in the street or on the road is a good deed the town hears of.");
                if (s.SettingsVersion < 449)
                    log?.Invoke("Settings: new in this version: [Scenes] StepBetweenKnown (on): a quarrel you stepped into and stopped is a good deed the town hears of.");
                if (s.SettingsVersion < 448)
                    log?.Invoke("Settings: new in this version: [Scenes] WalkHomeMends (on): seeing a man in drink home mends his drinking worry.");
                if (s.SettingsVersion < 447)
                    log?.Invoke("Settings: new in this version: [Scenes] FriendsKnowYourThief (on): a friend's warning says when the pickpocket near is the one who robbed you.");
                if (s.SettingsVersion < 446)
                    log?.Invoke("Settings: new in this version: [Scenes] StandReminds (on): the J row says when the man losing a fight stood by you, or owes you.");
                if (s.SettingsVersion < 445)
                    log?.Invoke("Settings: new in this version: [Display] NameCardsMourns (on): \"Mourns one you killed\" on the card of someone grieving a friend you killed.");
                if (s.SettingsVersion < 444)
                    log?.Invoke("Settings: new in this version: [Scenes] KinThanks (on): a kinsman of someone you saved may come to thank you.");
                if (s.SettingsVersion < 443)
                    log?.Invoke("Settings: new in this version: [Encounters] GratitudeFelt (on): a stand, a fine paid, a word at the jail are felt as kindnesses in the mind.");
                if (s.SettingsVersion < 442)
                    log?.Invoke("Settings: new in this version: [Encounters] SuretyKeptOwes (on): a man you stood surety for who keeps his week owes you, and comes to thank you.");
                if (s.SettingsVersion < 441)
                    log?.Invoke("Settings: new in this version: [Display] NameCardsRobbed (on): \"Robbed you\" on the card of the pickpocket who got away with your money.");
                if (s.SettingsVersion < 440)
                    log?.Invoke("Settings: new in this version: [Scenes] BreakUpKnown (on): a fight you break up on J is a good deed the town hears of.");
                if (s.SettingsVersion < 439)
                    log?.Invoke("Settings: new in this version: [Display] NameCardsStood (on): \"Stood by you\" on the card of someone who fought for you this week.");
                if (s.SettingsVersion < 438)
                    log?.Invoke("Settings: new in this version: [Scenes] WelshersShunned (on): welsh on a bet and that town's betting men won't take your money for a week.");
                if (s.SettingsVersion < 437)
                    log?.Invoke("Settings: new in this version: [Scenes] WelshRemembered (on): the man you welshed on, and those who saw it, keep it in mind.");
                if (s.SettingsVersion < 436)
                    log?.Invoke("Settings: new in this version: [Encounters] StoodRemembered (on): the man you stood with in a fight remembers it, and comes to thank you.");
                if (s.SettingsVersion < 435)
                    log?.Invoke("Settings: new in this version: [Encounters] FineOwes (on): a man whose fine you paid owes you, remembers it, and comes to thank you.");
                if (s.SettingsVersion < 434)
                    log?.Invoke("Settings: new in this version: [Display] NameCardsOwes (on): \"Owes you\" on the card of someone you saved or stood with in a fight.");
                if (s.SettingsVersion < 433)
                    log?.Invoke("Settings: new in this version: [Encounters] StoodByOwes (on): a man you stood with in a fight owes you, as one you saved.");
                if (s.SettingsVersion < 432)
                    log?.Invoke("Settings: new in this version: [Scenes] FriendsWarnThief (on): a friend may warn you of a known pickpocket near, and you're on your guard a while.");
                if (s.SettingsVersion < 431)
                    log?.Invoke("Settings: new in this version: [Scenes] FriendsChase (on): when your pocket is picked, a friend of yours near may run the thief down and bring your money back.");
                if (s.SettingsVersion < 430)
                    log?.Invoke("Settings: new in this version: [Scenes] LiftsWary (on): the thief who bumps into you comes about once an hour of play, and fewer try it where your killings are known.");
                if (s.SettingsVersion < 429)
                    log?.Invoke("Settings: new in this version: [Encounters] AlliesSpareLaw (on): with bandits or anyone else at you too, friends who come to your aid leave the law alone; the law alone at you, they still stand with you.");
                if (s.SettingsVersion < 428)
                    log?.Invoke("Settings: [Scenes] Pickpockets off now also stops the thief who bumps into you and lifts your money ([Scenes] Lifts): either one off stops it.");
                if (s.SettingsVersion < 427)
                    log?.Invoke("Settings: new in this version: [Scenes] MinersMeet (on): the miners' meeting at Annesburg after the shift.");
                if (s.SettingsVersion < 426)
                    log?.Invoke("Settings: new in this version: [World] CenturyTurns (on): New Year's Eve argues whether the century turns.");
                if (s.SettingsVersion < 425)
                    log?.Invoke("Settings: new in this version: [Scenes] Grumble (on): old hands talk over a newcomer where he can hear.");
                if (s.SettingsVersion < 424)
                    log?.Invoke("Settings: new in this version: [Scenes] PostReward (on): J at the jail door to put up a reward for a thief.");
                if (s.SettingsVersion < 423)
                    log?.Invoke("Settings: new in this version: [Scenes] SpreadRumour (on): J in a saloon to put a word about a man in a talker's ear.");
                if (s.SettingsVersion < 422)
                    log?.Invoke("Settings: new in this version: [Scenes] NoteMeet (on): two a word went between meet afterwards.");
                if (s.SettingsVersion < 421)
                    log?.Invoke("Settings: new in this version: [World] GraveNight (on): the night after a burial, one of theirs sits up with the grave.");
                if (s.SettingsVersion < 420)
                    log?.Invoke("Settings: new in this version: [Mind] SleepOnIt (on): a night's sleep cools a quarrel nothing fed.");
                if (s.SettingsVersion < 419)
                    log?.Invoke("Settings: new in this version: [Mind] FeudArcs (on): a feud may be made up before the street, where it would have come to blows.");
                if (s.SettingsVersion < 418)
                    log?.Invoke("Settings: new in this version: [Mind] Familiarity (on): those seen about the same place day after day get acquainted.");
                if (s.SettingsVersion < 417)
                    log?.Invoke("Settings: new in this version: [World] SessionClock (on): the arcs you're meant to see end within a session.");
                if (s.SettingsVersion < 416)
                    log?.Invoke("Settings: new in this version: [World] RevivedSeen (on): the revived dead known by anyone who knew them, and asked to their face.");
                if (s.SettingsVersion < 415)
                    log?.Invoke("Settings: new in this version: [Scenes] CollectionPlate (on): the plate comes round after the Sunday service.");
                if (s.SettingsVersion < 414)
                    log?.Invoke("Settings: new in this version: [Scenes] Brands (on): a ranch man blames his neighbour for doctored brands.");
                if (s.SettingsVersion < 413)
                    log?.Invoke("Settings: new in this version: [Scenes] StepBetween (on): J to step between two men about to quarrel.");
                if (s.SettingsVersion < 412)
                    log?.Invoke("Settings: new in this version: [Mind] LookScheduler (on): the background looks run at most two a frame, so fewer hitches.");
                if (s.SettingsVersion < 411)
                    log?.Invoke("Settings: new in this version: [Testing] ProofTour (on): the proof tour row walks every Testing row that can run where you stand; ProofTourRisky (off) adds the risky ones.");
                if (s.SettingsVersion < 410)
                    log?.Invoke("Settings: new in this version: [Scenes] Vouch (on): vouch for a man in the cells.");
                if (s.SettingsVersion < 409)
                    log?.Invoke("Settings: new in this version: [Scenes] HorseWatch (on): a man to watch your horse.");
                if (s.SettingsVersion < 408)
                    log?.Invoke("Settings: new in this version: [Scenes] WidowCollection (on): a collection for the widow.");
                if (s.SettingsVersion < 407)
                    log?.Invoke("Settings: new in this version: [Encounters] Elopers (on): a couple running off.");
                if (s.SettingsVersion < 406)
                    log?.Invoke("Settings: new in this version: [Encounters] AlleyVoice (on): a voice from the dark in Saint Denis.");
                if (s.SettingsVersion < 405)
                    log?.Invoke("Settings: new in this version: [Scenes] StandTheWake (on): a drink to the dead's memory on a wake night.");
                if (s.SettingsVersion < 404)
                    log?.Invoke("Settings: new in this version: [Encounters] NoMapRider (on): the rider who asks the way to a town on no map.");
                if (s.SettingsVersion < 403)
                    log?.Invoke("Settings: new in this version: [World] Stories (on): a story over days, the son at the mine.");
                if (s.SettingsVersion < 402)
                    log?.Invoke("Settings: new in this version: [Encounters] Riders (on): riders on the road.");
                if (s.SettingsVersion < 401)
                    log?.Invoke("Settings: new in this version: [World] GameEncounters (on): the game's own encounters are seen and talked of.");
                if (s.SettingsVersion < 400)
                    log?.Invoke("Settings: new in this version: [Scenes] BestFist (on): a town's best fist, and the men who come to try him.");
                if (s.SettingsVersion < 399)
                    log?.Invoke("Settings: new in this version: [Scenes] CardsharpRunOut (on): a card cheat walked out of town by the table.");
                if (s.SettingsVersion < 398)
                    log?.Invoke("Settings: new in this version: [Scenes] RowdyDrunks (on): rowdy drunks in the street at night.");
                if (s.SettingsVersion < 397)
                    log?.Invoke("Settings: new in this version: [Scenes] DoctorsDoor (on): after dark a man runs for the doctor, and J walks him home.");
                if (s.SettingsVersion < 396)
                    log?.Invoke("Settings: new in this version: [Encounters] SecondActs (on): the people of the encounters come back.");
                if (s.SettingsVersion < 395)
                    log?.Invoke("Settings: new in this version: [Scenes] StoreHoldups (on): by day a man holds up a customer outside the store near you, and J is your word; HoldupShots (off): he may fire at one who bolts.");
                if (s.SettingsVersion < 394)
                    log?.Invoke("Settings: new in this version: [Scenes] RainShelter (on): rain empties the street and fills the saloon.");
                if (s.SettingsVersion < 393)
                    log?.Invoke("Settings: new in this version: [Scenes] TemperanceDoor (on): the temperance ladies at the saloon door at dusk.");
                if (s.SettingsVersion < 392)
                    log?.Invoke("Settings: new in this version: [Mind] MemoriesBlur (on): the town's tales grow in the telling.");
                if (s.SettingsVersion < 391)
                    log?.Invoke("Settings: new in this version: [Mind] OpinionsAct (on): what people think of each other decides things.");
                if (s.SettingsVersion < 390)
                    log?.Invoke("Settings: new in this version: [Mind] ControlCensus (on): a CONTROL line a minute, who holds the people near you.");
                if (s.SettingsVersion < 389)
                    log?.Invoke("Settings: new in this version: [Mind] WitnessesFetchLaw (on): someone runs for the law when nobody's seen the fight.");
                if (s.SettingsVersion < 388)
                    log?.Invoke("Settings: new in this version: [Scenes] PurseSnatch (on): a man near gives chase to a thief, and J asks what's done with the one you catch.");
                if (s.SettingsVersion < 387)
                    log?.Invoke("Settings: new in this version: [Scenes] Rivals (on): two men courting the same woman have it out, and she chooses.");
                if (s.SettingsVersion < 386)
                    log?.Invoke("Settings: new in this version: [Scenes] Snubs (on): the well-off and the working man in the street.");
                if (s.SettingsVersion < 385)
                    log?.Invoke("Settings: new in this version: [Scenes] OldSoldiers (on): old soldiers of either side argue the war, and young men home from Cuba tell theirs.");
                if (s.SettingsVersion < 384)
                    log?.Invoke("Settings: new in this version: [Scenes] Greenhorn (on): the town tests a newcomer off the train.");
                if (s.SettingsVersion < 383)
                    log?.Invoke("Settings: new in this version: [Mind] WalkTogether (on): friends whose talk is done walk on together.");
                if (s.SettingsVersion < 382)
                    log?.Invoke("Settings: new in this version: [Scenes] StreetMore (on): the street's own business: a horse trade, a tooth pulled, the way asked.");
                if (s.SettingsVersion < 381)
                    log?.Invoke("Settings: new in this version: [Scenes] PaydayNight (on): Saturday night the paid hands fill the saloon, and its fights grow faster.");
                if (s.SettingsVersion < 380)
                    log?.Invoke("Settings: new in this version: [Scenes] BrawlAfter (on): the week after a brawl: the winner's round, the chairs, sore sides, and the jail door at dawn.");
                if (s.SettingsVersion < 379)
                    log?.Invoke("Settings: new in this version: [Scenes] BrawlVerbs (on): J in a fight near you: break it up, stand with the man losing, or take a bet.");
                if (s.SettingsVersion < 378)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlShape (on): a big brawl has its beats, and burns out instead of stopping dead.");
                if (s.SettingsVersion < 377)
                    log?.Invoke("Settings: new in this version: [Mind] QuarrelPolitics (on): men in drink come to blows over silver, the trusts, the Philippines and the dry vote.");
                if (s.SettingsVersion < 376)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlStrays (on): a stray blow pulls a bystander in; a friend pulls a beaten man out.");
                if (s.SettingsVersion < 375)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlCascade (on): a crowd joins a fight by thresholds: most fights stay small, a few tip into a brawl.");
                if (s.SettingsVersion < 374)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlSides (on): onlookers take a fighter's side by who they are: kin, their trade, the town's own.");
                if (s.SettingsVersion < 373)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlLawSight (on): the badge holds a brawl back by sight and distance, not by being within 30 m; one deputy against a room sends for help.");
                if (s.SettingsVersion < 372)
                    log?.Invoke("Settings: new in this version: [Mind] HandBackLent (on): what we set on people to make them fight is put back once they're done.");
                if (s.SettingsVersion < 371)
                    log?.Invoke("Settings: new in this version: [Mind] QuarrelSlights (on): a quarrel over nothing says what it's over; [Mind] FightHoldStart (on): two told to fight keep at it until the first blow; [Scenes] MeetWalkAgain (on): a walker who stops short is told again, and the one waiting gets up.");
                if (s.SettingsVersion < 370)
                    log?.Invoke("Settings: new in this version: [World] AlmanacSides (on): people take their own side on 1899's quarrels.");
                if (s.SettingsVersion < 369)
                    log?.Invoke("Settings: new in this version: [Mind] HeldStayInMind (on): the ring keeps hold of those it's walking, out to its own leash.");
                if (s.SettingsVersion < 368)
                    log?.Invoke("Settings: new in this version: [Stalkers] RevenantsAnger (on): the dead come back for you by their anger.");
                if (s.SettingsVersion < 367)
                    log?.Invoke("Settings: new in this version: [Ticker] HistoryNotes (on): the history behind 1899's thoughts, in the news box.");
                if (s.SettingsVersion < 366)
                    log?.Invoke("Settings: new in this version: [World] NewFaces (on): settlers, a schoolteacher, a preacher, a mail-order bride off the train.");
                if (s.SettingsVersion < 365)
                    log?.Invoke("Settings: new in this version: [Ticker] Opinions (on): townsfolk hold views of each other.");
                if (s.SettingsVersion < 364)
                    log?.Invoke("Settings: new in this version: [Mind] DiaryDreams (on): hard days come back as dreams in the diaries.");
                if (s.SettingsVersion < 363)
                    log?.Invoke("Settings: new in this version: [Mind] KeepersBackSeen (on): those who saw a keeper fall stare when they're back.");
                if (s.SettingsVersion < 362)
                    log?.Invoke("Settings: new in this version: [Ticker] ThreadsNextDay (on): yesterday's fright comes back the morning after.");
                if (s.SettingsVersion < 361)
                    log?.Invoke("Settings: new in this version: [Mind] OnlookerPicks (on): the model picks what a fight's strongest onlooker does.");
                if (s.SettingsVersion < 360)
                    log?.Invoke("Settings: new in this version: [Mind] PlacesMourned (on): those close to the dead stop where they died, and the dearest kneel.");
                if (s.SettingsVersion < 359)
                    log?.Invoke("Settings: new in this version: [Scenes] WalkHome (on): J offers to see someone home after dark.");
                if (s.SettingsVersion < 358)
                    log?.Invoke("Settings: new in this version: [Scenes] BarRound (on): J at a saloon's bar buys the house a round.");
                if (s.SettingsVersion < 357)
                    log?.Invoke("Settings: new in this version: [Encounters] SceneClips (on): the scenes' own clips, seen to play in game, in the executions and the road scenes.");
                if (s.SettingsVersion < 355)
                    log?.Invoke("Settings: new in this version: [World] AllSaintsGraves (on): on All Saints' Day in the south, people go to the tombs.");
                if (s.SettingsVersion < 354)
                    log?.Invoke("Settings: new in this version: [World] LettersHome (on): those who left write to a friend still in the town.");
                if (s.SettingsVersion < 353)
                    log?.Invoke("Settings: new in this version: [Display] KnownOnMap (off): the regulars you've met as named marks on the map.");
                if (s.SettingsVersion < 352)
                    log?.Invoke("Settings: new in this version: [World] WeekColumn (on): a Sunday column in the paper of what really happened that week.");
                if (s.SettingsVersion < 351)
                    log?.Invoke("Settings: new in this version: [World] TownRivals (on): towns think little of their neighbours, and tell their news meaner.");
                if (s.SettingsVersion < 350)
                    log?.Invoke("Settings: new in this version: [World] ColdSnaps (on): spells of bitter cold in the high country; an old man may not wake.");
                if (s.SettingsVersion < 349)
                    log?.Invoke("Settings: new in this version: [Scenes] ScripTalk (on): the men at Annesburg talk about being paid in scrip.");
                if (s.SettingsVersion < 348)
                    log?.Invoke("Settings: new in this version: [World] RoundupSeason (on): the cattle towns fill with paid-off hands after the roundup.");
                if (s.SettingsVersion < 347)
                    log?.Invoke("Settings: new in this version: [Scenes] Suffrage (on): women in Saint Denis put the case for the vote to the men they meet.");
                if (s.SettingsVersion < 346)
                    log?.Invoke("Settings: new in this version: [Scenes] MoneyTalk (on): the money question argued in the street and the saloon.");
                if (s.SettingsVersion < 345)
                    log?.Invoke("Settings: new in this version: [Ticker] PairThoughts (on): two in view with a history each think about the other.");
                if (s.SettingsVersion < 344)
                    log?.Invoke("Settings: new in this version: [Encounters] WaysideComposed (on): people in road scenes show what they feel in the game's own moves.");
                if (s.SettingsVersion < 343)
                    log?.Invoke("Settings: new in this version: [Encounters] WaysideAnswers (on): walk up on a road scene and one of them decides what to do about you.");
                if (s.SettingsVersion < 342)
                    log?.Invoke("Settings: new in this version: [Ticker] WeatherChecked (on): talk about the weather is held to what's really out. WeatherTalk (on): more of it the worse it is, and when it turns.");
                if (s.SettingsVersion < 341)
                    log?.Invoke("Settings: new in this version: [World] VigilanceCommittees (on): when a posse finds nothing, the town's angrier men may settle on someone.");
                if (s.SettingsVersion < 340)
                    log?.Invoke("Settings: new in this version: [World] PosseFromTheRoad (on): a killing out on the road can bring the nearest town's posse.");
                if (s.SettingsVersion < 339)
                    log?.Invoke("Settings: new in this version: [Mind] CrowdPicks (on): how a crowd takes a fight is one decision for all of them (needs [Scenes] FightCrowds).");
                if (s.SettingsVersion < 338)
                    log?.Invoke("Settings: new in this version: [Mind] ControlRingAhead (on): the ring takes in people along the way you're going a little sooner.");
                if (s.SettingsVersion < 337)
                    log?.Invoke("Settings: new in this version: [Encounters] CircleAnswers (on): walk into the middle of a ring of the awake and it answers you as one.");
                if (s.SettingsVersion < 336)
                    log?.Invoke("Settings: new in this version: [Mind] ComposedModel (on): with the AI model on, it chooses the order of a composed act.");
                if (s.SettingsVersion < 335)
                    log?.Invoke("Settings: new in this version: [Mind] ComposedActs (on): someone the ring holds shows a strong feeling in two or three of the game's own moves, in order.");
                if (s.SettingsVersion < 334)
                    log?.Invoke("Settings: new in this version: [Mind] ControlRingPicks (on): with the AI model on, it may change someone's next step as they come into the ring.");
                if (s.SettingsVersion < 333)
                    log?.Invoke("Settings: new in this version: [Mind] ControlRingPlans (on): someone the ring holds has a short errand of their own, kept for five minutes.");
                if (s.SettingsVersion < 332)
                    log?.Invoke("Settings: new in this version: [Mind] ControlRingSeated (on): those sat near you are held too, and get up when their mind wants something else.");
                if (s.SettingsVersion < 331)
                    log?.Invoke("Settings: new in this version: [Mind] ControlRingKeepAt (on): someone the ring holds who stops short is given the order again.");
                if (s.SettingsVersion < 330)
                    log?.Invoke("Settings: new in this version: [Ticker] ThoughtsFollowOn (on): someone's next thoughts pick up from what just happened to them.");
                if (s.SettingsVersion < 329)
                    log?.Invoke("Settings: new in this version: [Ticker] RoadThoughts (on), RoadThoughtsMetres (35): thoughts from those you pass on the road, and by their part at a scene.");
                if (s.SettingsVersion < 328)
                    log?.Invoke("Settings: new in this version: [Ticker] Asides (on): while someone talks near you, the person listening may think something about it.");
                if (s.SettingsVersion < 327)
                    log?.Invoke("Settings: new in this version: [Ticker] ThoughtsAtOnce (3): up to three people's thoughts over their heads at once.");
                if (s.SettingsVersion < 326)
                    log?.Invoke("Settings: new in this version: [Stalkers] RevenantsHunt (on), RevenantHuntChance (30): someone you killed who comes back may try to kill you.");
                if (s.SettingsVersion < 325)
                    log?.Invoke("Settings: new in this version: [Stalkers] RevenantsMinds (on): someone you killed who comes back chooses what to do next, by the model if it is on.");
                if (s.SettingsVersion < 324)
                    log?.Invoke("Settings: new in this version: [Encounters] Wayside (on), WaysideChance (25): things to come across out on the road.");
                if (s.SettingsVersion < 323)
                    log?.Invoke("Settings: new in this version: [Mind] ControlRing (on), ControlRingMetres (22), ControlRingMost (12): the people near you kept at what their minds choose, less as you ride faster, drawn in if it costs too much.");
                if (s.SettingsVersion < 322)
                    log?.Invoke("Settings: new in this version: [Display] CleanScreen (off): one switch to clear RealNPCs off the screen.");
                if (s.SettingsVersion < 321)
                    log?.Invoke("Settings: new in this version: [World] PosseRides (on): the posse rides, off screen.");
                if (s.SettingsVersion < 320)
                    log?.Invoke("Settings: new in this version: [World] WantedWhich (on): you know a face from a wanted poster.");
                if (s.SettingsVersion < 319)
                    log?.Invoke("Settings: new in this version: [World] NeighbourReward (on): a reward posted for a neighbour who killed in a brawl.");
                if (s.SettingsVersion < 318)
                    log?.Invoke("Settings: new in this version: [World] HoboSigns (on): the tramps' chalk by the tracks.");
                if (s.SettingsVersion < 317)
                    log?.Invoke("Settings: new in this version: [WestWorld] HostNotes (on): the waking compare notes.");
                if (s.SettingsVersion < 316)
                    log?.Invoke("Settings: new in this version: [World] GoingDry (on): 1907's dry towns.");
                if (s.SettingsVersion < 315)
                    log?.Invoke("Settings: new in this version: [World] Relics (on): 1907's photographers sell the old outlaws.");
                if (s.SettingsVersion < 314)
                    log?.Invoke("Settings: new in this version: [World] BoyRemembers (on): the boy who remembers Arthur, in 1907.");
                if (s.SettingsVersion < 313)
                    log?.Invoke("Settings: new in this version: [World] ClaimAgents (on): the claim agent and the widow.");
                if (s.SettingsVersion < 312)
                    log?.Invoke("Settings: new in this version: [World] GrindingSeason (on): grinding season in the cane, and its ball.");
                if (s.SettingsVersion < 311)
                    log?.Invoke("Settings: new in this version: [World] EventFaisDoDo (on): Saturday night dances in Lagras.");
                if (s.SettingsVersion < 310)
                    log?.Invoke("Settings: new in this version: [World] EventBrassFuneral (on): brass band funerals in Saint Denis.");
                if (s.SettingsVersion < 309)
                    log?.Invoke("Settings: new in this version: [Scenes] MurderBallads (on): murder ballads at a fire.");
                if (s.SettingsVersion < 308)
                    log?.Invoke("Settings: new in this version: [World] EventMardiGras (on): Mardi Gras night in Saint Denis.");
                if (s.SettingsVersion < 307)
                    log?.Invoke("Settings: new in this version: [World] EventFourth (on): the Fourth of July as an event.");
                if (s.SettingsVersion < 306)
                    log?.Invoke("Settings: new in this version: the Around town box stays up 30 seconds after you come into a town, not the whole time ([Output] AroundTownStaysSeconds; 0 as before), shows each item a little shorter, and [Output] NewsTo = off turns it off.");
                if (s.SettingsVersion < 305)
                    log?.Invoke("Settings: new in this version: [World] EventFair (on): the county fair at Valentine in September.");
                if (s.SettingsVersion < 304)
                    log?.Invoke("Settings: new in this version: [World] EventBaseBall (on): Valentine against Strawberry at base ball.");
                if (s.SettingsVersion < 303)
                    log?.Invoke("Settings: new in this version: [Mind] TelegraphKnows (on): the station agent knows first.");
                if (s.SettingsVersion < 302)
                    log?.Invoke("Settings: new in this version: [World] EditorSides (on): the town papers' editors take a side.");
                if (s.SettingsVersion < 301)
                    log?.Invoke("Settings: new in this version: [World] DriftingPosters (on): the bills describe you as the rumours do.");
                if (s.SettingsVersion < 300)
                    log?.Invoke("Settings: new in this version: [World] DimeNovels (on): dime novels about you.");
                if (s.SettingsVersion < 299)
                    log?.Invoke("Settings: new in this version: [Mind] MemorialPhotos (on): memorial photographs of the dead.");
                if (s.SettingsVersion < 298)
                    log?.Invoke("Settings: new in this version: [Mind] UndertakerKnows (on): the undertaker thinks of the week's dead.");
                if (s.SettingsVersion < 297)
                    log?.Invoke("Settings: new in this version: [World] Fever (on), FeverDeaths (on): now and then a fever goes through a town.");
                if (s.SettingsVersion < 296)
                    log?.Invoke("Settings: new in this version: [Scenes] QuiltingBee (on): the quilting bee overheard by the church.");
                if (s.SettingsVersion < 295)
                    log?.Invoke("Settings: new in this version: [World] EventBarnRaisings (on): barn raisings, and two at odds on one beam.");
                if (s.SettingsVersion < 294)
                    log?.Invoke("Settings: new in this version: [World] EventBoxSocials (on): the church's box social on the first Saturday.");
                if (s.SettingsVersion < 293)
                    log?.Invoke("Settings: new in this version: [World] EventBaptisms (on): a river baptism the Sunday after a camp meeting; the spots tool has a riverbank kind.");
                if (s.SettingsVersion < 292)
                    log?.Invoke("Settings: new in this version: [World] EventRevivals (on): summer camp meetings, three nights of revival.");
                if (s.SettingsVersion < 291)
                    log?.Invoke("Settings: new in this version: [World] Foreclosures (on): the sheriff's sale in the street when the bank takes a family's place.");
                if (s.SettingsVersion < 290)
                    log?.Invoke("Settings: new in this version: [Scenes] StoreSlate (on): credit at the store's counter, and J to pay someone's slate.");
                if (s.SettingsVersion < 289)
                    log?.Invoke("Settings: new in this version: [World] Drummers (on): travelling salesmen off the train.");
                if (s.SettingsVersion < 288)
                    log?.Invoke("Settings: new in this version: [Mind] MoreMoments (on): a horse bolting, a purse lifted, rain, a storm, a fight's ring.");
                if (s.SettingsVersion < 287)
                    log?.Invoke("Settings: new in this version: [Encounters] WagonOthersStop (on): someone else may stop for the broken wagon.");
                if (s.SettingsVersion < 286)
                    log?.Invoke("Settings: new in this version: [Stalkers] StalkerRides (on): a stalker on horseback rides, follows, comes in close and rides off.");
                if (s.SettingsVersion < 285)
                    log?.Invoke("Settings: new in this version: [Scenes] KinTownWrongs (on): kin take up a wrong one townsman did another.");
                if (s.SettingsVersion < 284)
                    log?.Invoke("Settings: new in this version: [Mind] GamblersBorrow (on): a gambler borrows from a man of his town.");
                if (s.SettingsVersion < 283)
                    log?.Invoke("Settings: new in this version: [World] FenceCutters (on): wire cut in the night, and a feud that climbs.");
                if (s.SettingsVersion < 282)
                    log?.Invoke("Settings: new in this version: [World] SaleDay (on): Tuesday's auction at the Valentine stockyards.");
                if (s.SettingsVersion < 281)
                    log?.Invoke("Settings: new in this version: [World] ShapeUp (on): the dawn shape-up on the Saint Denis waterfront.");
                if (s.SettingsVersion < 280)
                    log?.Invoke("Settings: new in this version: [World] MineWhistle (on): rarely, an accident at the Annesburg mine.");
                if (s.SettingsVersion < 279)
                    log?.Invoke("Settings: new in this version: [World] Wakes (on): the night before a burial, the wake.");
                if (s.SettingsVersion < 278)
                    log?.Invoke("Settings: new in this version: [World] Mourning (on): mourning and graves visited.");
                if (s.SettingsVersion < 277)
                    log?.Invoke("Settings: new in this version: [World] SundayMorning (on): Sunday properly, the service and the dinner after.");
                if (s.SettingsVersion < 276)
                    log?.Invoke("Settings: new in this version: [Mind] LastingMarks (on): a beating's marks worn for days; LastingMarksLook (off) until the probe is seen.");
                if (s.SettingsVersion < 275)
                    log?.Invoke("Settings: new in this version: [Mind] TieGossip (on): word of your deeds travels along the town's ties.");
                if (s.SettingsVersion < 274)
                    log?.Invoke("Settings: new in this version: [Mind] SharedFright (on): two who saw you kill are tied by it.");
                if (s.SettingsVersion < 273)
                    log?.Invoke("Settings: new in this version: [Mind] SurvivorsChanged (on): those you beat or robbed come back changed.");
                if (s.SettingsVersion < 272)
                    log?.Invoke("Settings: new in this version: [Mind] TrustSlow (on): trust lost fast, won slow, with those you wronged.");
                if (s.SettingsVersion < 271)
                    log?.Invoke("Settings: new in this version: [World] WhileAway (on): back in a town after days away, what you missed.");
                if (s.SettingsVersion < 270)
                    log?.Invoke("Settings: new in this version: [Mind] PeakEnd (on): a meeting with you is remembered by its peak and its end.");
                if (s.SettingsVersion < 269)
                    log?.Invoke("Settings: new in this version: [Mind] StressBuilds (on): a week's load adds up to a breaking point, and each breaks in their own way.");
                if (s.SettingsVersion < 268)
                    log?.Invoke("Settings: new in this version: [World] NoticeBoard (on): the week ahead on one board, in the reader and the Around town box.");
                if (s.SettingsVersion < 267)
                    log?.Invoke("Settings: new in this version: [Encounters] Pinkertons (on): a Pinkerton man asks the town after you where your violence is known.");
                if (s.SettingsVersion < 266)
                    log?.Invoke("Settings: new in this version: [World] WitnessesChoose (on): witnesses to your violence tell it or keep quiet, and can be leaned on with a gun.");
                if (s.SettingsVersion < 265)
                    log?.Invoke("Settings: new in this version: [World] EventShivarees (on): the night after a wedding, a shivaree.");
                if (s.SettingsVersion < 264)
                    log?.Invoke("Settings: new in this version: [Scenes] Temperance (on): the temperance ladies put the pledge to the town's drinkers in the street.");
                if (s.SettingsVersion < 263)
                    log?.Invoke("Settings: new in this version: [Scenes] Payday (on): Saturday afternoons in town the wages are out, and it shows.");
                if (s.SettingsVersion < 262)
                    log?.Invoke("Settings: new in this version: [World] EventPrizeFights (on): now and then two of a town's men fight for a purse behind the saloon at night; the word goes round that afternoon.");
                if (s.SettingsVersion < 261)
                    log?.Invoke("Settings: new in this version: [Mind] VoicedEncounters (on): with the voice server running, the encounters' and walk-ups' lines are said in the speaker's own voice, matching the words.");
                if (s.SettingsVersion < 260)
                    log?.Invoke("Settings: new in this version: [Mind] WorriesShown (on): the worry people carry now shows in what they do, and their thoughts say why.");
                if (s.SettingsVersion < 259)
                    log?.Invoke("Settings: the quiet talks beside the street's scene start now: two people within 4.5 m, standing or strolling, may stop and talk ([Scenes] SideTalkMetres, new; before, they had to stand still within 3.4 m and hardly ever did).");
                if (s.SettingsVersion < 258)
                    log?.Invoke("Settings: new in this version: [Scenes] AlliesRemember (on): someone who stood with you in a fight comes over within the week to talk it over.");
                if (s.SettingsVersion < 257)
                    log?.Invoke("Settings: new in this version: [Scenes] Rematches (on): the one beaten in a fistfight may, days later, want it again, keep clear of the winner, or offer his hand.");
                if (s.SettingsVersion < 256)
                    log?.Invoke("Settings: new in this version: [Scenes] BrawlWagers (on): people watching a fistfight bet on it among themselves and settle after it; a bet welshed on leaves bad blood between the two who made it.");
                if (s.SettingsVersion < 255)
                    log?.Invoke("Settings: new in this version: [Output] Translate (on): translation.txt in the RealNPCs folder, when you make one, shows RealNPCs' lines in your language; the Testing row Write the translation template gives you every line to fill in.");
                if (s.SettingsVersion < 254)
                    log?.Invoke("Settings: new in this version: [Scenes] FriendCalls (on): a friend of yours in town comes over unasked with a warning, the news, a small gift or a favour of their trade; FriendGifts (on): what they hand over.");
                if (s.SettingsVersion < 253)
                    log?.Invoke("Settings: new in this version: [World] Returners (off, in testing), ReturnersChance (35), ReturnersPerTown (12): someone who matters comes back unseen in a body of their own build, dressed as themselves each time; LooksReadDepth (1): the Testing row that reads the looks of the people near you.");
                if (s.SettingsVersion < 252)
                    log?.Invoke("Settings: West World's waking is tuned up where it was as it came (more people wake, a stage every 3 days, despair 65%), and new in this version: [WestWorld] DespairFall (on): despair may end at the edge of a drop.");
                if (s.SettingsVersion < 251)
                    log?.Invoke("Settings: new in this version: [Scenes] NightTales (on): at night people tell the old tales, in the saloon, in the street after dark and at a campfire: the bell on the grave, the tommyknockers, the feu follet, the Bell Witch, Tailypo, the white stallion.");
                if (s.SettingsVersion < 250)
                    log?.Invoke("Settings: new in this version: [Mind] TimesTalk (on), TimesTalkShare (100): people near you talk over the times far more often: 1899's news, who holds office, the wheat and the bank, the railroad, the catalogue, the dry vote, the old soldiers; by day in the street, any hour in the saloon.");
                if (s.SettingsVersion < 249)
                    log?.Invoke("Settings: the AI model now leaves the graphics card after 5 minutes with nothing asked (30 before), and llama3.2:3b (about 2 GB of the card) is the model as it comes; llama3.1:8b (about 5 GB) is still yours to name in [Mind] LanguageModel. The voice pack keeps its model on a card of 12 GB or more.");
                if (s.SettingsVersion < 248)
                    log?.Invoke("Settings: [World] Bravery at 85 now brings more onlookers into a fistfight than before (50 is how it was), and the fighters no longer stop the crowd from fleeing: some may run, as the game has them.");
                if (s.SettingsVersion < 247)
                    log?.Invoke("Settings: [World] Bravery now changes only how readily onlookers join a fight (85 as before; higher, more join; lower, fewer) and the mod's own frights; it no longer sets the game's cower-instead-of-flee flag, so nobody freezes on its account, and BraveStand is gone with it.");
                if (s.SettingsVersion < 246)
                    log?.Invoke("Settings: new in this version: [World] Almanac (on), AlmanacShare (100): a few hundred dated things of 1899 and 1907, known from the day they were in the papers, in people's thoughts, Talk's news, the talks between two people, the paper read aloud, the week's paper in the reader and a word on the ticker.");
                if (s.SettingsVersion < 245)
                    log?.Invoke("Settings: new in this version: [Encounters] WagonWheel (on): by day on the roads, a wagon ahead of you loses a wheel; stop and set it, or ride on and send word from the next town.");
                if (s.SettingsVersion < 244)
                    log?.Invoke("Settings: new in this version: [Display] TownRegard (on): the town's regard of you in the Around town box: its known killings of yours against your kindnesses, its mood, who you know here is afraid or fond, and the talk.");
                if (s.SettingsVersion < 243)
                    log?.Invoke("Settings: new in this version: [World] BraveStand (on): a brave townsperson the game has cowering is stood up, holds their ground on their feet, and walks off.");
                if (s.SettingsVersion < 242)
                    log?.Invoke("Settings: new in this version: [World] YieldToDying (on): nothing of RealNPCs' (no line, no fall, no move) on anyone dying, so a death-animations mod's deaths play as they are.");
                if (s.SettingsVersion < 241)
                    log?.Invoke("Settings: new in this version: [WestWorld] WakeByNature (on): the waking's strange acts follow their nature and whoever they feel most about, not a roll.");
                if (s.SettingsVersion < 240)
                    log?.Invoke("Settings: new in this version: [Mind] PlacesRemember (on): places hold memories (someone who knew one you killed thinks of it at the spot), people know you by your horse, and the town notices your habits.");
                if (s.SettingsVersion < 239)
                    log?.Invoke("Settings: new in this version: [World] EpilogueCarries (on): John's first save takes Arthur's world over, eight years on.");
                if (s.SettingsVersion < 238)
                    log?.Invoke("Settings: new in this version: [World] ReturningNeighbours (on): someone who left on the train may come back on a later one.");
                if (s.SettingsVersion < 237)
                    log?.Invoke("Settings: new in this version: [Mind] GroupActs (on): friends close round a beaten friend after a fight, and an onlooker who leaves takes their spouse or sweetheart along.");
                if (s.SettingsVersion < 236)
                    log?.Invoke("Settings: new in this version: [World] GunpointStages (on): the gun kept on someone brings a second and a third step by who they are.");
                if (s.SettingsVersion < 235)
                    log?.Invoke("Settings: new in this version: [Stalkers] StalkerHorse (35): some stalkers appear on horseback; RevenantsAtCamp (on): someone you killed comes to the edge of your firelight.");
                if (s.SettingsVersion < 234)
                    log?.Invoke("Settings: new in this version: [World] BodyToUndertaker (on): the lawman carries the dead to the undertaker's or the jail when one is near, and someone who knew them follows.");
                if (s.SettingsVersion < 233)
                    log?.Invoke("Settings: new in this version: [Scenes] BarkeepSteps (on): the barkeep steps in on a saloon fight as they square up; DrunkOutside (on): a drunk's challenge taken up is taken outside.");
                if (s.SettingsVersion < 232)
                    log?.Invoke("Settings: new in this version: [Scenes] CriminalClass (on): thieves keep to it for a month, are known for it that long, roll drunks, and go straight after thirty clean days.");
                if (s.SettingsVersion < 231)
                    log?.Invoke("Settings: new in this version: [Romance] TrystPrice (2): the saloon's price for the night; TrystsAfter (on): the morning after, a sweetheart who expects you back, a husband who finds out.");
                if (s.SettingsVersion < 230)
                    log?.Invoke("Settings: new in this version: [Scenes] KinGrudges (on): a wrong of yours on a known person brings a man of their family to town a week on to say it to your face.");
                if (s.SettingsVersion < 229)
                    log?.Invoke("Settings: new in this version: [Mind] MoodsCarry (on): the weather wears on moods, and a feeling shown in a crowd is caught by those near.");
                if (s.SettingsVersion < 228)
                    log?.Invoke("Settings: new in this version: [WestWorld] DespairWater (on): despair may end in the river.");
                if (s.SettingsVersion < 227)
                    log?.Invoke("Settings: new in this version: [WestWorld] WakeLines (on): the fully awake pass on a line that wakes a listener who hears it twice; the revived dead are seen for what they are.");
                if (s.SettingsVersion < 226)
                    log?.Invoke("Settings: new in this version: [WestWorld] Ghosts (on): the people who lived through hours a loaded save undid feel it: a deja vu, a quarrel or a kiss that didn't happen, the morning that happened twice.");
                if (s.SettingsVersion < 225)
                    log?.Invoke("Settings: new in this version: [WestWorld] LoopVoice (on): the waking hear their habit as a voice at the hour, answer it, break the loop now and then, and may be reset to the start.");
                if (s.SettingsVersion < 224)
                    log?.Invoke("Settings: new in this version: [Keys] EventKey (J): the trial's, the gallows' and the show's menus while their prompt shows; change it if the game's journal key gets in the way.");
                if (s.SettingsVersion < 223)
                    log?.Invoke("Settings: new in this version: [World] TownLedger (on): each town's ledger from your killings, its deaths, arrivals and good turns; its mood in its people's thoughts; the regulars it keeps scale with it. ThinStreets (off): thinner streets in a town under 30.");
                if (s.SettingsVersion < 222)
                    log?.Invoke("Settings: new in this version: [World] TrainTime (on): a train at a town's platform brings a newcomer or kin and the post, and takes away a resident whose trouble ended in leaving.");
                if (s.SettingsVersion < 221)
                    log?.Invoke("Settings: new in this version: [Keys] SpotKey (K): records where you stand as a named place of the town into spots.txt (Shift and the key change the kind); the trains, the hangings and the shows use the table. Safe to ignore.");
                if (s.SettingsVersion < 220)
                    log?.Invoke("Settings: new in this version: [World] EventShows (on): a Wild West show acts out the most bent version of your legend once it has travelled; heckle, stand up as the real one, or rob the takings.");
                if (s.SettingsVersion < 219)
                    log?.Invoke("Settings: new in this version: [World] EventTrials (on): a resident who kills a man in a fight is held for the circuit judge; a verdict in days, a hanging two days after a guilty one; testify, be sworn in, break him out or stop it.");
                if (s.SettingsVersion < 218)
                    log?.Invoke("Settings: new in this version: [World] EventElections (on): election day in November in every town with five regulars, two of its men standing; posted a week ahead, your word counts.");
                if (s.SettingsVersion < 217)
                    log?.Invoke("Settings: new in this version: [World] TownEvents (on) and EventDances (on): the Saturday dance in a town with a saloon, every second week, posted on Thursday; staged outside the saloon when you're there, resolved among the town's regulars when you're not.");
                if (s.SettingsVersion < 216)
                    log?.Invoke("Settings: new in this version: [Encounters] TalkWorries (on): every worry they've told you of can be mended through Talk, and every one turned the wrong way with a word (its dark mirror).");
                if (s.SettingsVersion < 215)
                    log?.Invoke("Settings: new in this version: [World] LetterReplies (on): answer a letter from the reader (R) and change what its writer does next; WriteLetters (on): write first to anyone you've met (L in the reader, in a post town).");
                if (s.SettingsVersion < 214)
                    log?.Invoke("Settings: new in this version: [Display] Overheard (on): the papers end with what went on near you this last day; [Ticker] SceneCues (on): a cue on the ticker when voices are raised behind you.");
                if (s.SettingsVersion < 213)
                    log?.Invoke("Settings: new in this version: [Encounters] TalkVerbs (on): Talk can ask what someone knows of a person they've had dealings with, lend ten dollars (and ask after it three days on), and introduce two people.");
                if (s.SettingsVersion < 212)
                    log?.Invoke("Settings: new in this version: [Scenes] StreetReacts (on): the street reacts to other people's fights through the game's own crowd; StreetReputation (on): outlaws stared at and stepped around, the doctor, the preacher, the law and the keepers greeted, and a witness's thought of your own standing in a town.");
                if (s.SettingsVersion < 211)
                    log?.Invoke("Settings: new in this version: [World] RegularsRoutines (on): the town keeps time; who a passer-by becomes follows the hour and the spot (the saloon of an evening, church on Sunday morning, the early up at dawn, the owls at night).");
                if (s.SettingsVersion < 210)
                    log?.Invoke("Settings: new in this version: [Mind] TalkVoice (off): with the voice server and a microphone, Talk's row \"Say it out loud...\" listens to you and says your words to them; TalkVoiceSeconds (6) is the most it listens for.");
                if (s.SettingsVersion < 209)
                    log?.Invoke("Settings: new in this version: [Mind] TalkTyped (on): Talk's last row opens a line to type your own words on; they answer from who they are, in the AI model's words when it's on, else a written answer.");
                if (s.SettingsVersion < 208)
                    log?.Invoke("Settings: new in this version: [World] HistoricalAccuracy (on): the talk of the times, the lines about the year, the real calendar's holidays and the year's work by region; set it false for a world with no dated history in it.");
                if (s.SettingsVersion < 207)
                    log?.Invoke("Settings: new in this version: [Encounters] AlliesRide (on): the friend from the road rides in on horseback; AlliesGrieved (on), AlliesGrievedLiking (15): an ally who dies standing with you is grieved by those who knew them, who think the less of you for it.");
                if (s.SettingsVersion < 206)
                    log?.Invoke("Settings: new in this version: [Encounters] FondnessActs (on): whiskey for the house, a gift and condolences in Talk; KindnessSeen (on) and KindnessSeenLiking (5): people who see you do a kindness to someone else think better of you too.");
                if (s.SettingsVersion < 205)
                    log?.Invoke("Settings: new in this version: [Encounters] AlliesDotScale (0.6): the size of the green dot on friends and allies on the map (it drew large; 1 is the game's own size).");
                if (s.SettingsVersion < 204)
                    log?.Invoke("Settings: friends near you wear the green dot on the map whenever they're about ([Encounters] AlliesDot), and come to your aid against anyone, the law too; someone who owes you (the bar at AlliesLiking or more, someone you saved) comes whatever the odds.");
                if (s.SettingsVersion < 203)
                {
                    // 2026-11-03, Lex: "3 greets will get to 100% fondness, instead of 4": GreetLiking 34; an ini on any built value so far (12, 20, 25)
                    // moves, a value of your own is kept (the 201 block below then finds nothing to move)
                    if (s.GreetLiking == 12 || s.GreetLiking == 20 || s.GreetLiking == 25)
                        s.GreetLiking = 34;
                    log?.Invoke("Settings: [Encounters] GreetLiking is 34 now: three greetings of someone fill their fondness for you and make a friend who'll stand with you (AlliesLiking 90).");
                }
                if (s.SettingsVersion < 202)
                {
                    // 2026-11-03, Lex: "they become a friend when the fondness bar is 90% or higher": AlliesLiking 90 (40 since 197); an ini on
                    // 40 moves, a value of your own is kept
                    if (s.AlliesLiking == 40)
                        s.AlliesLiking = 90;
                    log?.Invoke("Settings: [Encounters] AlliesLiking is 90 now (from 40): someone counts as a friend who'll stand with you once their fondness for you is 90 or more (three greetings, or saving them); a kindness they remember, a trouble you mended, talks and your name known count on their own as before.");
                }
                if (s.SettingsVersion < 201)
                {
                    // 2026-11-03, Lex: "the friend/ally status from 4 days to upon 2nd greet, even if it's the same day", then "each greet gives
                    // 25% fondness boost for now": 25 on every greeting; an ini on the 12 of version 199 or the 20 of version 200 moves, a value
                    // of your own is kept
                    if ((s.SettingsVersion == 199 && s.GreetLiking == 12) || (s.SettingsVersion == 200 && s.GreetLiking == 20))
                        s.GreetLiking = 25;
                    log?.Invoke("Settings: [Encounters] GreetLiking is 25 now: every greeting of someone adds 25 to their fondness for you, so two hellos make a friend who'll stand with you (AlliesLiking 40) and four fill the bar.");
                }
                if (s.SettingsVersion < 199)
                    log?.Invoke("Settings: new in this version: [Encounters] AlliesDot (on) and AlliesDotColour: a small green dot on the map on each ally with you; GreetLiking: greeting someone makes them fonder of you (enRAGE's fondness, the bar under them).");
                if (s.SettingsVersion < 198)
                {
                    // 2026-11-03: the voices on as they come (nothing is heard until the voice server runs); an ini from 193-197 with both off
                    // (never shipped: only the test PC had them) comes on too
                    if (!s.VoicedThoughts && !s.VoicedDialogue)
                        s.VoicedThoughts = s.VoicedDialogue = true;
                    log?.Invoke("Settings: the voices are on as they come now ([Mind] VoicedThoughts, VoicedDialogue, VoiceRecord): nothing is heard until the voice server runs (the zip's voice folder: install-voice.cmd once, then start-voice-server.cmd before you play).");
                }
                if (s.SettingsVersion < 197)
                    log?.Invoke("Settings: new in this version: [Encounters] AlliesHelp (on), AlliesMetres (40), AlliesMost (3), AlliesLiking (40), AlliesRoadChance (30): attacked within sight of people who think well of you, some of them come to your aid, with a gun if the attackers have guns; out of the towns a friend from the ledger may happen by.");
                if (s.SettingsVersion < 196)
                    log?.Invoke("Settings: new in this version: [Mind] MindTalks (on): people near you talk to each other from what's on their minds, the worry they're living through, the times they live in (the election, the war, the reservations, the new machines), the stranger, a plan, the town's talk; the other answers from their own nature, and with the AI model in its own words.");
                if (s.SettingsVersion < 195)
                {
                    // 2026-11-03 (Lex): the voices heard a little less far (6% shorter); the value as it came moves, a value of your own is kept
                    string moved = "";
                    if (s.VoiceMetres == 15f) { s.VoiceMetres = 14f; moved = "; VoiceMetres 15 -> 14"; }
                    log?.Invoke("Settings: new in this version: [Display] VramGauge (model): a small gauge at the top left shows how much of your graphics card's memory is in use while the AI model is on; on shows it always, off never. The voices are heard within 14 m now (VoiceMetres)" + moved + ".");
                }
                if (s.SettingsVersion < 194)
                    log?.Invoke("Settings: new in this version: [Mind] VoiceRecord (on): with the voice server, the voices are recorded from your own game as you meet people; someone whose voice has no clip yet says one of the game's own lines once, in a quiet moment, and the server keeps it.");
                if (s.SettingsVersion < 193)
                    log?.Invoke("Settings: new in this version: [Mind] VoicedThoughts and VoicedDialogue (both off, in testing), VoiceUrl, VoiceMetres (15), VoiceVolume (100): thoughts and our lines said in each person's own voice, cloned on this PC by the local voice server (voice\\start-voice-server.cmd; needs your own export of the game's voice banks).");
                if (s.SettingsVersion < 192)
                    log?.Invoke("Settings: new in this version: [Mind] HeldRing (off, in testing), HeldRingMetres (12), HeldRingMinutes (10): the people near you held, one act straight into the next, instead of the game taking them back between acts.");
                if (s.SettingsVersion < 191)
                {
                    // 2026-11-02 (Lex): the Awakening's happenings rarer and eerier. The values as they came move; a value of your own is kept
                    var moved = new List<string>();
                    if (s.GatheringChance == 15) { s.GatheringChance = 10; moved.Add("GatheringChance 15 -> 10"); }
                    if (s.GatheringGapHours == 6) { s.GatheringGapHours = 24; moved.Add("GatheringGapHours 6 -> 24"); }
                    if (s.AwakeningDespair == 75) { s.AwakeningDespair = 50; moved.Add("AwakeningDespair 75 -> 50"); }
                    log?.Invoke("Settings: the Awakening is rarer and eerier: gatherings mostly after dark or in fog and rain (by day a quarter as often, and only with nobody else in sight), some gone when you look back, the waking's odd lines far fewer" +
                                (moved.Count > 0 ? "; " + string.Join(", ", moved) : "") + ".");
                }
                if (s.SettingsVersion < 190)
                    log?.Invoke("Settings: new in this version: [Encounters] CalloutsArthur (on): by day in a town, someone with something against you comes up and says it to your face; walk off and they call you a coward, stay and they put up their fists.");
                if (s.SettingsVersion < 189)
                    log?.Invoke("Settings: new in this version: [Encounters] HogtiedTurnsOnYou (25): now and then one you cut loose pulls a knife on you instead of thanking you; and the hogtied's bounty hunters fight back when you hold a gun on them.");
                if (s.SettingsVersion < 188)
                    log?.Invoke("Settings: new in this version: [Mind] WrittenWakes (on): without the AI model too, people with cause wonder at the world now and then, and it may wake them as the model's wondering does.");
                if (s.SettingsVersion < 187)
                    log?.Invoke("Settings: new in this version: [Encounters] EventDots (on): the game's pulsing event dot on the map where a brawl, a gathering or one of the encounters is going on near you.");
                if (s.SettingsVersion < 186)
                    log?.Invoke("Settings: new in this version: [Encounters] GatheringHostile (50): half the gatherings of the awake turn on you when you come within 10 m of them.");
                if (s.SettingsVersion < 185)
                {
                    if (s.DramaGapSeconds == 60)
                        s.DramaGapSeconds = 30;
                    log?.Invoke("Settings: new in this version: [Scenes] EventfulOnly (on): street scenes of nothing but talk are passed over, and the quiet talks beside them; scenes come more often (DramaGapSeconds 30 as it comes, from 60).");
                }
                if (s.SettingsVersion < 182 && s.FightCrowds)
                {
                    s.FightCrowds = false;
                    log?.Invoke("Settings: [Scenes] FightCrowds is off now: people no longer walk over to stand in a ring round a fight (some froze where they stopped). Brawls still draw people in to fight.");
                }
                if (s.SettingsVersion == 177 && KeyIs(s.SettingsKey, "Alt+F") && (KeyIs(s.SettingsKey2, "None") || string.IsNullOrEmpty(s.SettingsKey2)))
                {
                    s.SettingsKey2 = "Alt+R";   // version 177 left Alt+F alone for a day: Alt+R back as the second key (Lex, 2026-11-01)
                    log?.Invoke("Settings: Alt+R opens the settings panel again, beside Alt+F.");
                }
                if (s.SettingsVersion < 106 && KeyIs(s.FlirtKey, "F2"))
                {
                    s.FlirtKey = "None";   // 2026-10-29 (Lex): flirting is H in the lock-on menu, the hook up G; F2 goes
                    log?.Invoke("Settings: FlirtKey F2 -> None (flirt with H in the lock-on menu, then Hook up with the same H once she says yes).");
                }
                if (s.SettingsVersion < 163 && s.Language)
                    s.LanguageChosen = true;   // on before RealNPCs could find the model by itself: the player's own choice, kept (2026-10-31)
                if (s.SettingsVersion < 165)
                    log?.Invoke("Settings: the street's hush now follows only the latest killing in a town, for HushHours (" + F(s.HushHours) + ") game hours, Saint Denis HushHoursSaintDenis (" + F(s.HushHoursSaintDenis) + "); HushFear and HushFearSaintDenis are gone (Lex in Saint Denis, 2026-10-31: brawls kept the street hushed).");   // the old keys are skipped on reading and not written again
                if (s.SettingsVersion < 166)
                    log?.Invoke("Settings: new in this version: [Mind] MindActsSeatedLooks, MindRetries, [Scenes] ReasonVariety, ChanceFirst (all on; the minds act on more of their choices, the street's reasons and chance meetings take turns).");
                if (s.SettingsVersion < 167)
                    log?.Invoke("Settings: new in this version: [World] Storyteller (lively; calm, wild or off as before): quiet streets and roads get the next scene, walk-up or encounter sooner, and something big a short breather.");
                if (s.SettingsVersion < 168)
                    log?.Invoke("Settings: new in this version: [Mind] MindActsScenarios (on): people sat or leaning get up for what their minds choose, and rests, meals, drinks and work go to a spot of the game's of that kind; [Scenes] MainScenesAtOnce (2): a second full street scene at least 20 m from the first.");
                if (s.SettingsVersion < 169)
                    log?.Invoke("Settings: new in this version: [Encounters] PublicCallouts (on): in a town by day, one of the people about calls another out before the street, and each answers by their own mind; DomesticDisputes (on): a man starts in on his wife in the street, and she and a neighbour answer by their own minds; Hogtied (on): away from the towns, someone tied up by the road with two men by them; HogtiedMenDraw (on as it comes since 172): those men may draw on you unprovoked.");
                if (s.SettingsVersion < 170)
                    log?.Invoke("Settings: new in this version: [Mind] MindPassingLasts (on): people near you show what they feel about what you did for hours after, not minutes; [Scenes] FavoursAsked (on): someone in money trouble may come and ask you, and Talk lists the fix; [Scenes] CourtFolkAges (on): enRAGE's townsfolk may court each other on the street; [Scenes] WarnedYou (on): someone who saw what you did may have words with you, and a friend may warn you of a grudge or the law.");
                if (s.SettingsVersion < 172)
                    log?.Invoke("Settings: violence against Arthur is on as it comes (Lex): [Encounters] SupperInvitation and HogtiedMenDraw are on; set either false to turn it off");
                if (s.SettingsVersion < 171)
                    log?.Invoke("Settings: new in this version: [Mind] MindChanceMeetings (on): what passes between two people who meet by chance near you comes from their minds: friends talk and sing, foes quarrel or turn their backs, a money worry asks the other for a loan.");
                if (s.SettingsVersion < 173)
                    log?.Invoke("Settings: new in this version: [Mind] ActThoughts (on): as someone near you starts what their own mind chose, and at each turn of an encounter, a thought over their head says what moved them to it; written lines, no AI model needed.");
                if (s.SettingsVersion < 174)
                    log?.Invoke("Settings: new in this version: [Mind] ActThoughtEachSeconds (90) and ActThoughtGapSeconds (15): thoughts with what people choose to do come more often, and to people right beside you too; and an encounter shows both the reason for a quarrel and the first answer to it.");
                if (s.SettingsVersion < 175)
                    log?.Invoke("Settings: new in this version: [Mind] BrawlJoinBoost (20): onlookers are readier to join a fight, so two men fighting grow into a big brawl more often (0 as before); BrawlStarterDot (on): the one who started a fight wears a red dot on the map, and stepping in against him is self-defence; and a fight is heard: after a few seconds any free lawman within 60 m comes, whether he saw it or not.");
                if (s.SettingsVersion < 176)
                    log?.Invoke("Settings: [Mind] ThoughtsAloud works without the AI model now: as a thought shows over someone near, what they feel most may come out aloud in their own recorded voice (a curse, a frightened word, thanks, relief).");
                RaiseOldDefaults(s, log);   // 0.6.0: what play-testing settled on, and the stalker's new acts
                s.SettingsVersion = CurrentVersion;
                Write(path, s);
                log?.Invoke("Settings: updated " + Path.GetFileName(path) + " to this version (your values kept, new options added).");
            }
            return s;
        }

        /// Keeps typed-in numbers inside what the add-on can use.
        private void Clamp()
        {
            ReturnChance = Math.Max(0, Math.Min(100, ReturnChance));
            ProofTourGapSeconds = Math.Max(8, Math.Min(120, ProofTourGapSeconds));
            StrangerReturnChance = Math.Max(0, Math.Min(100, StrangerReturnChance));
            RealityChance = Math.Max(0, Math.Min(100, RealityChance));
            AwakeningShare = Math.Max(0f, Math.Min(100f, AwakeningShare));
            AwakeningDays = Math.Max(1, Math.Min(60, AwakeningDays));
            AwakeningDespair = Math.Max(0, Math.Min(100, AwakeningDespair));
            AwakeningBreakdown = Math.Max(0, Math.Min(100, AwakeningBreakdown));
            WakeHoldMinutes = Math.Max(1, Math.Min(15, WakeHoldMinutes));
            WakeStripChance = Math.Max(0, Math.Min(100, WakeStripChance));
            WakeLashOut = Math.Max(0, Math.Min(100, WakeLashOut));
            PickMetres = Math.Max(5, Math.Min(60, PickMetres));
            HogtiedTurnsOnYou = Math.Max(0, Math.Min(100, HogtiedTurnsOnYou));
            CalloutsArthurChance = Math.Max(0, Math.Min(100, CalloutsArthurChance));
            CalloutsArthurDraw = Math.Max(0, Math.Min(100, CalloutsArthurDraw));
            AlliesMetres = Math.Max(10, Math.Min(80, AlliesMetres));
            AlliesMost = Math.Max(1, Math.Min(6, AlliesMost));
            AlliesLiking = Math.Max(0, Math.Min(100, AlliesLiking));
            AlliesRoadChance = Math.Max(0, Math.Min(100, AlliesRoadChance));
            AlliesDotColour = (AlliesDotColour ?? string.Empty).Trim();
            AlliesDotScale = Math.Max(0.2f, Math.Min(2f, AlliesDotScale));
            GreetLiking = Math.Max(0, Math.Min(50, GreetLiking));
            KindnessSeenLiking = Math.Max(0, Math.Min(30, KindnessSeenLiking));
            AlliesGrievedLiking = Math.Max(0, Math.Min(50, AlliesGrievedLiking));
            GatheringHostile = Math.Max(0, Math.Min(100, GatheringHostile));
            LawShootsThieves = Math.Max(0, Math.Min(100, LawShootsThieves));
            ActThoughtEachSeconds = Math.Max(20, Math.Min(600, ActThoughtEachSeconds));
            ActThoughtGapSeconds = Math.Max(0, Math.Min(120, ActThoughtGapSeconds));
            WaysideChance = Math.Max(0, Math.Min(100, WaysideChance));
            RevenantHuntChance = Math.Max(0, Math.Min(100, RevenantHuntChance));
            ControlRingMetres = Math.Max(Ring.LeastMetres, Math.Min(Ring.MostMetres, ControlRingMetres));
            ControlRingMost = Math.Max(Ring.LeastHeld, Math.Min(Ring.MostHeld, ControlRingMost));
            HeldRingMetres = Math.Max(4f, Math.Min(20f, HeldRingMetres));
            HeldRingMinutes = Math.Max(1, Math.Min(30, HeldRingMinutes));
            PicksAtOnce = Math.Max(1, Math.Min(16, PicksAtOnce));
            BrawlMost = Math.Max(Brawl.LeastAtAll, Math.Min(Brawl.MostAtAll, BrawlMost));
            BrawlDrawShare = Math.Max(0, Math.Min(100, BrawlDrawShare));
            BrawlFistsFatal = Math.Max(0, Math.Min(100, BrawlFistsFatal));
            BrawlJoinMetres = Math.Max(10f, Math.Min(60f, BrawlJoinMetres));
            BrawlCrowdMetres = Math.Max(10f, Math.Min(60f, BrawlCrowdMetres));
            SideTalkMetres = SideTalk.Clamp(SideTalkMetres);
            BrawlLeastSeconds = Math.Max(15, Math.Min(120, BrawlLeastSeconds));
            BrawlJoinBoost = Math.Max(0, Math.Min(60, BrawlJoinBoost));
            BrawlOpeningBoost = Math.Max(0, Math.Min(60, BrawlOpeningBoost));
            Bravery = Math.Max(0, Math.Min(100, Bravery));
            // stalker hours: the stalker day still runs 05:00 to 05:00; day hours stay in daylight, night hours cross midnight
            StalkerEveryMinutes = Math.Max(1, Math.Min(120, StalkerEveryMinutes));
            StalkerBlackOutfit = Math.Max(-1, Math.Min(200, StalkerBlackOutfit));
            LastingMarksGait = (LastingMarksGait ?? "").Trim();   // (FIVEDAY item 24)
            LastingMarksPack = (LastingMarksPack ?? "").Trim();
            StalkerBlackModel = string.IsNullOrWhiteSpace(StalkerBlackModel) ? "a_m_o_sdupperclass_01" : StalkerBlackModel.Trim().ToLowerInvariant();
            StalkerNearbyMetres = Math.Max(20, Math.Min(150, StalkerNearbyMetres));
            // stalker distances, kept in a sensible order so one odd value can't break him
            StalkerAppearMin = Math.Max(20, Math.Min(150, StalkerAppearMin));
            StalkerAppearMax = Math.Max(StalkerAppearMin, Math.Min(200, StalkerAppearMax));
            StalkerFollowMetres = Math.Max(10, Math.Min(80, StalkerFollowMetres));
            StalkerStareMetres = Math.Max(StalkerFollowMetres + 4, Math.Min(100, StalkerStareMetres));
            StalkerStillSeconds = Math.Max(5, Math.Min(300, StalkerStillSeconds));
            StalkerCircleMetres = Math.Max(3, Math.Min(15, StalkerCircleMetres));
            StalkerHideMin = Math.Max(20, Math.Min(150, StalkerHideMin));
            StalkerHideMax = Math.Max(StalkerHideMin, Math.Min(200, StalkerHideMax));
            string still = (StalkerWhenStill ?? "").Trim().ToLowerInvariant();
            StalkerWhenStill = still == "circle" || still == "watch" || still == "rush" || still == "eerie" ? still : "mix";
            StalkerActMin = Math.Max(2, Math.Min(40, StalkerActMin));
            StalkerActMax = Math.Max(StalkerActMin, Math.Min(40, StalkerActMax));
            QuietStillSeconds = Math.Max(1, Math.Min(60, QuietStillSeconds));
            EncounterEveryMinutes = Math.Max(2, Math.Min(120, EncounterEveryMinutes));
            EncountersAtOnce = Math.Max(1, Math.Min(8, EncountersAtOnce));
            CryingWomanChance = Math.Max(0, Math.Min(100, CryingWomanChance));
            NightVisitorChance = Math.Max(0, Math.Min(100, NightVisitorChance));
            TravellerChance = Math.Max(0, Math.Min(100, TravellerChance));
            WagonWheelChance = Math.Max(0, Math.Min(100, WagonWheelChance));
            HoldupChance = Math.Max(0, Math.Min(100, HoldupChance));
            AlmanacShare = Math.Max(0, Math.Min(200, AlmanacShare));
            TimesTalkShare = Math.Max(0, Math.Min(300, TimesTalkShare));
            ReturnersChance = Math.Max(0, Math.Min(100, ReturnersChance));
            ReturnersPerTown = Math.Max(1, Math.Min(40, ReturnersPerTown));
            LooksReadDepth = Math.Max(1, Math.Min(4, LooksReadDepth));
            CampfireChance = Math.Max(0, Math.Min(100, CampfireChance));
            SupperChance = Math.Max(0, Math.Min(100, SupperChance));
            ExecutionChance = Math.Max(0, Math.Min(100, ExecutionChance));
            PublicCalloutChance = Math.Max(0, Math.Min(100, PublicCalloutChance));
            DomesticChance = Math.Max(0, Math.Min(100, DomesticChance));
            HogtiedChance = Math.Max(0, Math.Min(100, HogtiedChance));
            HermitChance = Math.Max(0, Math.Min(100, HermitChance));
            GatheringChance = Math.Max(0, Math.Min(100, GatheringChance));
            GatheringHours = (GatheringHours ?? "").Trim().ToLowerInvariant() == "night" ? "night" : "any";
            GatheringGapHours = Math.Max(1, Math.Min(72, GatheringGapHours));
            BoldThieves = Math.Max(0, Math.Min(100, BoldThieves));
            WatchedLawMetres = Math.Max(5f, Math.Min(60f, WatchedLawMetres));
            WatchedRecklessChance = Math.Max(0, Math.Min(100, WatchedRecklessChance));
            WatchedChurchMetres = Math.Max(10f, Math.Min(200f, WatchedChurchMetres));
            LonelyNeed = Math.Max(0.3f, Math.Min(1f, LonelyNeed));
            LonelyLookSeconds = Math.Max(10, Math.Min(600, LonelyLookSeconds));
            LonelyMetres = Math.Max(5f, Math.Min(40f, LonelyMetres));
            LonelyWaitSeconds = Math.Max(5, Math.Min(60, LonelyWaitSeconds));
            LonelyRestDays = Math.Max(0, Math.Min(14, LonelyRestDays));
            ThanksAfterHours = Math.Max(0, Math.Min(72, ThanksAfterHours));
            ThanksWithinDays = Math.Max(1, Math.Min(14, ThanksWithinDays));
            if (ThanksWithinDays * 24 <= ThanksAfterHours)
                ThanksWithinDays = ThanksAfterHours / 24 + 1;   // the window must close after it opens
            ThanksDrinkChance = Math.Max(0, Math.Min(100, ThanksDrinkChance));
            ThanksDollarChance = Math.Max(0, Math.Min(100, ThanksDollarChance));
            ThanksFiveChance = Math.Max(0, Math.Min(100, ThanksFiveChance));
            ThanksMendedChance = Math.Max(0, Math.Min(100, ThanksMendedChance));
            ThanksLoanDays = Math.Max(0, Math.Min(14, ThanksLoanDays));
            SpaceMetres = Math.Max(0.5f, Math.Min(2f, SpaceMetres));
            SpaceSaloonMetres = Math.Max(0.4f, Math.Min(2f, SpaceSaloonMetres));
            SpaceSeconds = Math.Max(0.5f, Math.Min(10f, SpaceSeconds));
            BullyTurnsHothead = Math.Max(0, Math.Min(100, BullyTurnsHothead));
            BullyTurnsProud = Math.Max(0, Math.Min(100, BullyTurnsProud));
            BullyTurnsColdGreedy = Math.Max(0, Math.Min(100, BullyTurnsColdGreedy));
            BullyTurnsCoward = Math.Max(0, Math.Min(100, BullyTurnsCoward));
            BullyTurnsOthers = Math.Max(0, Math.Min(100, BullyTurnsOthers));
            BullyTurnsDrunk = Math.Max(0, Math.Min(100, BullyTurnsDrunk));
            BullyTurnsMeant = Math.Max(0, Math.Min(100, BullyTurnsMeant));
            OnlookersRunMetres = Math.Max(5f, Math.Min(60f, OnlookersRunMetres));
            OnlookersRunning = Math.Max(1, Math.Min(8, OnlookersRunning));   // the onlookers' queue holds 8 (WestworldScript.Onlookers.cs, OnlookQueueMost)
            TalkModelWaitSeconds = Math.Max(1f, Math.Min(7f, TalkModelWaitSeconds));   // a model request gives up at 8 s (WestworldScript.Language.cs, LanguageTimeoutMs)
            TalkVoiceSeconds = Math.Max(3, Math.Min(12, TalkVoiceSeconds));   // the voice server's --listen-most is 12
            HushHours = Math.Max(0f, Math.Min(12f, HushHours));
            HushHoursSaintDenis = Math.Max(0f, Math.Min(12f, HushHoursSaintDenis));
            StrainChance = Math.Max(0, Math.Min(100, StrainChance));
            TownPostsAfterDeath = (TownPostsAfterDeath ?? "").Trim().ToLowerInvariant() == "successor" ? "successor" : "survive";
            RegularsPerTown = Math.Max(5, Math.Min(80, RegularsPerTown));            StalkerChance = Math.Max(0, Math.Min(100, StalkerChance));
            StalkerHorse = Math.Max(0, Math.Min(100, StalkerHorse));
            StalkerMinutes = Math.Max(1, Math.Min(240, StalkerMinutes));
            AvengerChance = Math.Max(0, Math.Min(100, AvengerChance));
            AvengersAtOnce = Math.Max(1, Math.Min(10, AvengersAtOnce));
            AvengerGapHours = Math.Max(0, Math.Min(720, AvengerGapHours));
            KinWordDaysMin = Math.Max(0, Math.Min(30, KinWordDaysMin));
            KinWordDaysMax = Math.Max(KinWordDaysMin, Math.Min(30, KinWordDaysMax));
            UnseenKinWordDaysMin = Math.Max(0, Math.Min(60, UnseenKinWordDaysMin));
            UnseenKinWordDaysMax = Math.Max(UnseenKinWordDaysMin, Math.Min(60, UnseenKinWordDaysMax));
            DramaGapSeconds = Math.Max(20, DramaGapSeconds);
            ScenesAtOnce = Math.Max(1, Math.Min(6, ScenesAtOnce));
            MainScenesAtOnce = Math.Max(1, Math.Min(3, MainScenesAtOnce));
            ChanceMeetMetres = Math.Max(2f, Math.Min(12f, ChanceMeetMetres));
            TroubleWeight = Math.Max(0f, Math.Min(5f, TroubleWeight));
            RescueChance = Math.Max(0, Math.Min(100, RescueChance));
            TickerSeconds = Math.Max(4, Math.Min(120, TickerSeconds));
            TickerY = Math.Max(0.05f, Math.Min(0.99f, TickerY));
            TickerScale = Math.Max(0.15f, Math.Min(0.8f, TickerScale));
            TickerWidth = Math.Max(0.2f, Math.Min(1f, TickerWidth));
            TickerNearbyMetres = Math.Max(3f, Math.Min(150f, TickerNearbyMetres));
            ThoughtsOverHeadScale = Math.Max(0.12f, Math.Min(0.6f, ThoughtsOverHeadScale));
            ThoughtsAtOnce = Math.Max(1, Math.Min(4, ThoughtsAtOnce));
            RoadThoughtsMetres = Math.Max(10f, Math.Min(80f, RoadThoughtsMetres));
            ScenesOnTickerMetres = Math.Max(5f, Math.Min(80f, ScenesOnTickerMetres));
            PanelTimeScale = Math.Max(0f, Math.Min(1f, PanelTimeScale));
            PadSettings = PadChord.Normal(PadSettings) ?? "RB+DPadUp";   // one that can't be read: the default (the panel is how a controller reaches everything)
            PadStatus = PadChord.Normal(PadStatus) ?? "None";
            PadLetters = PadChord.Normal(PadLetters) ?? "None";
            PadTickerNext = PadChord.Normal(PadTickerNext) ?? "None";
            if (string.IsNullOrWhiteSpace(LanguageUrl)) LanguageUrl = "http://127.0.0.1:11434";
            if (string.IsNullOrWhiteSpace(VoiceUrl)) VoiceUrl = "http://127.0.0.1:5005";
            VoiceUrl = VoiceUrl.Trim().TrimEnd('/');
            VoiceMetres = Math.Max(5f, Math.Min(30f, VoiceMetres));
            VoiceVolume = Math.Max(10, Math.Min(200, VoiceVolume));
            if (string.IsNullOrWhiteSpace(LanguageModel)) LanguageModel = EnrageWestworld.Core.Language.DefaultModel;
            LanguageUrl = LanguageUrl.Trim().TrimEnd('/');
            LanguageModel = LanguageModel.Trim();
            WillingSingle = Math.Max(0, Math.Min(100, WillingSingle));
            WillingTaken = Math.Max(0, Math.Min(100, WillingTaken));
            TrystPrice = Math.Max(0, Math.Min(20, TrystPrice));
            TickerGapSeconds = Math.Max(0, Math.Min(20, TickerGapSeconds));
            RealVoiceTalkSeconds = Math.Max(30, Math.Min(1800, RealVoiceTalkSeconds));
            // game | ours | both | off (Lex, 2026-10-28: the game's own white subtitle for a recorded line can't be stopped, so
            // one set at a time); the old on (and matched, game) read as game
            string sm = (Subtitles ?? "").Trim().ToLowerInvariant();
            Subtitles = sm == "off" || sm == "ours" || sm == "both" ? sm : "game";
            ErrandPace = Math.Max(1f, Math.Min(3f, ErrandPace));
            FeelingBarsMetres = Math.Max(3f, Math.Min(40f, FeelingBarsMetres));
            string nc = (NameCards ?? "known").Trim().ToLowerInvariant();
            NameCards = nc == "off" || nc == "everyone" || nc == "targeted" ? nc : "known";
            string sto = (Storyteller ?? "lively").Trim().ToLowerInvariant();
            Storyteller = sto == "off" || sto == "calm" || sto == "wild" ? sto : "lively";
            VramGauge = Vram.Normal(VramGauge);
            NameCardsMetres = Math.Max(5f, Math.Min(150f, NameCardsMetres));
            NameCardsKnownMetres = Math.Max(5f, Math.Min(150f, NameCardsKnownMetres));
        }

        private static void Write(string path, Settings s)
        {
            try
            {
                File.WriteAllText(path, s.ToIni(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        public string ToIni()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("; RealNPCs settings (an unofficial add-on for enRAGE). true/false, numbers in seconds unless noted. Alt+F or Alt+R in game edits them too (the settings panel).");
            b.AppendLine("; Saved changes are picked up in game within a few seconds (ReloadIni).");
            b.AppendLine("SettingsVersion = " + SettingsVersion + "   ; leave this alone");
            b.AppendLine();
            b.AppendLine("[Features]");
            b.AppendLine("ResetAtClerk = " + B(ResetAtClerk) + "   ; a station clerk's list offers Reset RealNPCs Memory (hold Q): everyone forgets what they know and feel about you, enRAGE's townsfolk too; the people and their lives stay; copies of the saves are kept");
            b.AppendLine("Conversations = " + B(Conversations) + "   ; greet someone enRAGE knows and they talk about themselves");
            b.AppendLine("Remarks = " + B(Remarks) + "   ; people who remember you speak up when you pass");
            b.AppendLine("Thoughts = " + B(Thoughts) + "   ; now and then, what someone privately thinks of you");
            b.AppendLine("DiaryDreams = " + B(DiaryDreams) + "   ; with Diaries: a fright, a grief, a shame or an anger they wrote down comes back a night or two later as a dream in their diary, bent out of shape");
            b.AppendLine("Diaries = " + B(Diaries) + "   ; people write down their days (also saved as text files)");
            b.AppendLine("WordOfMouth = " + B(WordOfMouth) + "   ; what you did reaches their kin, friends and family far away");
            b.AppendLine("KinWordDaysMin = " + KinWordDaysMin + "   ; a killing someone saw: the dead man's kin get the telegram at least this many game days on (0-30; a game day is about 48 real minutes)");
            b.AppendLine("KinWordDaysMax = " + KinWordDaysMax + "   ; the latest it may come, in game days");
            b.AppendLine("UnseenKinWordDaysMin = " + UnseenKinWordDaysMin + "   ; a killing nobody saw, or self-defence: the kin hear at least this many game days on, unless the body is found sooner (buried, or a lawman carries it off; then KinWordDays from the find), and the word doesn't know it was you: no letter to you, nobody rides after you (0-60)");
            b.AppendLine("UnseenKinWordDaysMax = " + UnseenKinWordDaysMax + "   ; the latest it may come, in game days");
            b.AppendLine("LifeUpdates = " + B(LifeUpdates) + "   ; news from the lives of people you know");
            b.AppendLine("RevealToEnrage = " + B(RevealToEnrage) + "   ; what they tell you is marked as known in enRAGE's records");
            b.AppendLine();
            b.AppendLine("[Romance]");
            b.AppendLine("Romance = " + B(Romance) + "   ; people flirt with you, and you can flirt back (FlirtKey)");
            b.AppendLine("NpcFlirting = " + B(NpcFlirting) + "   ; people near you make eyes at each other in street scenes and court, each answering by what they make of the other, as with you");
            b.AppendLine("Trysts = " + B(Trysts) + "   ; when a woman says yes to your flirting and is willing for more, once her answer is said the same H (Flirt) becomes Hook up for two minutes (never before a yes, never on a no); press it to go somewhere quieter, and the screen goes dark, an hour passes, nothing is shown");
            b.AppendLine("CasualHookupsDecides = " + B(CasualHookupsDecides) + "   ; with the CasualHookups mod installed: its Flirt works only on a woman who has said yes to your flirting here, and RealNPCs leaves the night to it instead of fading to black (false: the two mods stay apart)");
            b.AppendLine("WillingSingle = " + WillingSingle + "   ; percent chance a single woman takes to your flirting, before what she thinks of you tips it (the saloon's women always do)");
            b.AppendLine("WillingTaken = " + WillingTaken + "   ; the same for a woman who's married or spoken for");
            b.AppendLine("TrystPrice = " + TrystPrice + "   ; what one of the saloon's women asks for the night, in dollars, said before her yes and taken when you have it (0: nothing asked)");
            b.AppendLine("TrystsAfter = " + B(TrystsAfter) + "   ; the days after a night: her thought the morning after if you're still about; a single woman after two nights expects you back (a word to your face when you've been gone three days, and she cools if it's a fortnight); a married woman's husband hears of it one time in three (two in three when the saloon saw) and comes to town two to five days on to say it to your face, as the kin do (never violent); false: the night alone");
            b.AppendLine("FlirtPrompt = " + B(FlirtPrompt) + "   ; Flirt in the game's lock-on menu, with Greet and Antagonize, on its H key (D-pad down on a pad), as well as FlirtKey; once she has said yes to the night and her answer is said, the same H is Hook up (nothing of RealNPCs' is on G, which enRAGE uses)");
            b.AppendLine("NpcRomance = " + B(NpcRomance) + "   ; enRAGE's people court, marry, quarrel and fight over each other");
            b.AppendLine("AmbientFlirting = " + B(AmbientFlirting) + "   ; strangers in town flirt with each other now and then");
            b.AppendLine("SameSexRomance = " + B(SameSexRomance) + "   ; true: some people are drawn to their own sex, NPCs and the player alike");
            b.AppendLine("RomanceAffectsEnrage = " + B(RomanceAffectsEnrage) + "   ; moods, memories, grudges, marriages and gang tips go into enRAGE's records");
            b.AppendLine();
            b.AppendLine("[Mind]");
            b.AppendLine("Minds = " + B(Minds) + "   ; people think about what is most on their minds: what they lived through (a fight they were in or saw, a kind word, a slight), their worry, what's around them; false: the old mix of thoughts");
            b.AppendLine("RecordMinds = " + B(RecordMinds) + "   ; enRAGE's own people think the same way: what RealNPCs saw them live through, weighed with enRAGE's own record of them (what they feel about you, its memories of you, what they've heard, their goals, their mood)");
            b.AppendLine("RecordFates = " + B(RecordFates) + "   ; when one of enRAGE's people dies of something RealNPCs did (despair, a street fight gone too far), it is written into enRAGE's record of them (their history, and their death if enRAGE missed it); it also lets despair reach them");
            b.AppendLine("ShareWithEnrage = " + B(ShareWithEnrage) + "   ; RealNPCs keeps its own record of who has died; when one of enRAGE's townsfolk dies and enRAGE didn't see it (the area unloaded before it looked), RealNPCs marks them dead in enRAGE's records too, so it doesn't bring them back");
            b.AppendLine("MindShadow = " + B(MindShadow) + "   ; the mind underneath (in testing): works out each nearby person's traits, needs, feelings and mood and writes them to the log, the F7 dump and the Show minds cards; it changes nothing they do or think");
            b.AppendLine("KeepersBackSeen = " + B(KeepersBackSeen) + "   ; a keeper shot down at the counter is back, bandaged, as the game brings them: those who saw the fall, near you, turn and stare, with a thought");
            b.AppendLine("ThreadsNextDay = " + B(ThreadsNextDay) + "   ; with ThoughtsFollowOn: a fight, a fright or a body stays with a resident overnight; the next day, their first quiet thought may come back to it");
            b.AppendLine("OnlookerPicks = " + B(OnlookerPicks) + "   ; with the model's picks on: at a fight or a quarrel, what the strongest onlooker does (watch, walk off, shake their head, square up) is the model's pick, with a thought; late or unreadable, the rules' as before");
            b.AppendLine("PlacesRemember = " + B(PlacesRemember) + "   ; memory that behaves like memory: someone who knew one you killed thinks of it near the spot (once each; a friend's ground for good, an acquaintance's for a month), people who have seen you on your horse know the horse before the face as you ride up (once a day each), and a place you keep at the same hour on three different days is a habit the town notices (once each); false: none of the three");
            b.AppendLine("HeldStayInMind = " + B(HeldStayInMind) + "   ; someone the ring is walking on an errand stays in mind out to the ring's own reach; false: dropped at the mind's 25 m edge, as before");
            b.AppendLine("WalkTogether = " + B(WalkTogether) + "   ; with MindLive on: two friends (or a courting couple) whose talk in the street plays to its end may walk on together, side by side, with a thought on the way and a word as they part: two men to the saloon after work, homeward at dusk, a couple's stroll on a Sunday afternoon; two pairs a town at once, a pair once a day; false: they go their own ways");
            b.AppendLine("QuarrelPolitics = " + B(QuarrelPolitics) + "   ; with QuarrelSlights on: two men in drink who hold opposite sides of one of 1899's quarrels (free silver against gold, the trusts, the Philippines, the dry vote, by their trade or their piety) may come to blows over it instead of a look or a word; onlookers on each side lean to their man, and the town retells it by what it was over; false: never");
            b.AppendLine("QuarrelSlights = " + B(QuarrelSlights) + "   ; with QuarrelLadder on: a quarrel between strangers that was \"over nothing\" says what it's over, by the place (a shoulder in passing, the way blocked, a look held, a word about a man's horse or hat; in a saloon a spilled drink, a seat taken, a laugh), a man at the worst of his trouble snaps because of the week he's had, and foes have it out over their old business; the words and the log only, never how far it goes; false: the two old openings");
            b.AppendLine("MemoriesBlur = " + B(MemoriesBlur) + "   ; a big brawl leaves a tale in the town for a week, told in the gossip and in the thoughts of those near you, and it grows in the telling: more men each day, the hour sliding into the night, a knife, the starting of it put on someone the teller despises; those who were in it tell it truest; false: the brawl's news once, as before");
            b.AppendLine("OpinionsAct = " + B(OpinionsAct) + "   ; with MindLive: the views people hold of each other (respect, contempt, envy, fear) decide things: the fearful keep clear of the one they fear and out of any brawl he's in, contempt for a fighter pulls an onlooker against him, respect draws two to walk on together, and a friend tells a friend what they think of someone not there; false: views are thoughts only, as before");
            b.AppendLine("ControlCensus = " + B(ControlCensus) + "   ; a CONTROL line in the log once a minute: of the people within 25 m, named or not, how many are the story's, the game's own encounters', ours (and which feature), enRAGE's in a role, another script's, the game's (fighting, fleeing, riding, in a scenario...) and free for us; only reads, a third of them a frame; false: no line");
            b.AppendLine("HandBackLent = " + B(HandBackLent) + "   ; what RealNPCs sets on people who aren't its own to make them fight (an ally, a brawl's holdout, a caller, a gang's man, one who joins your fight, the waking who lash out, the road's fights: 'fight even unarmed', 'always fight', and the hostile group on callers and thieves) is put back once they've been out of any fight 20 s and nothing of ours has them; false: it stays until the body despawns, as before");
            b.AppendLine("BrawlShape = " + B(BrawlShape) + "   ; with MindBrawls on: once six or more are at it, the barkeep shouts from behind his bar (he keeps his post), the timid and the women near slip out, a gun drawn in it scatters the room, and from ten seconds before its least time is up the beaten-looking quit, so it thins out instead of stopping dead; false: none of these");
            b.AppendLine("BrawlStrays = " + B(BrawlStrays) + "   ; with MindBrawls on: someone standing at arm's length of the fighting may take a stray blow (the hot swing back and are in it against the man who hit them, the timid back off, a woman cries out and leaves), and a friend of a man losing badly may pull him out of it, the two leaving together (the one beating him lets them go, or a hothead follows); false: neither");
            b.AppendLine("BrawlCascade = " + B(BrawlCascade) + "   ; with MindBrawls on: an onlooker with no stake in a fight joins only once enough are already at it for a man of his nature (the hot, the reckless and the drunk first, the self-possessed and the law-abiding last; more with the law near), so most fights stay two or three and a few tip into a brawl; friends, foes and their own kind join by their own reasons; false: everyone weighed alike, so nearly every fight with no law near grows to a brawl");
            b.AppendLine("BrawlSides = " + B(BrawlSides) + "   ; with MindBrawls on: an onlooker leans to the fighter who is his own kind (his kin, a man of his trade against another: the farm hands for the farm hand, the working men for the miner; the town's own against an outsider), about half a friend's pull; those sat or leaning get up for a fight of two when they have a stake in it; the well-off stay out and leave it to the law; false: friends, foes and heat alone, as before");
            b.AppendLine("WitnessesFetchLaw = " + B(WitnessesFetchLaw) + "   ; with MindLive: in a brawl of three or more that no lawman has seen or heard, one onlooker (a gentleman or a lady first, else the law-abiding by nature) runs to a lawman within 150 m and brings him at a run; after a face put into a bar or a window, a witness may fetch one who has a word with the man who did it; false: the law comes only to what it sees or hears");
            b.AppendLine("BrawlLawSight = " + B(BrawlLawSight) + "   ; with MindBrawls on: a lawman holds onlookers back from joining a fight by what they can see (in sight within 12 m as before; in sight farther, half as much; out of sight, a little), some lawmen let a fair fight between two run a while and step in when it turns two on one, one deputy alone against six or more holds off and sends for help, and someone cries 'Law!' as he comes, so the law-abiding slip away; false: any lawman within 30 m holds every joiner, as before");
            b.AppendLine("FightHoldStart = " + B(FightHoldStart) + "   ; two people told to fight in a street scene keep the order until the first blow lands (the game's own alarms held off them, as enRAGE does), then the game's fists as before; false: the alarms come back the moment they're told, and a man may run off instead (Valentine, 2026-11-09)");
            b.AppendLine("MindLive = " + B(MindLive) + "   ; what that mind decides reaches the screen (their moods, and thoughts of what they really feel and need, and what they do about it); each part has its own switch below");
            b.AppendLine("LookScheduler = " + B(LookScheduler) + "   ; the background looks (the fever's thoughts, the posters, the calendar, the town's rivals and two dozen more) run at most two a frame, the most overdue first, so they never pile onto one frame; false: each on its own timer, as before");
            b.AppendLine("MindLevels = " + B(MindLevels) + "   ; (in testing) the mind's work spread by distance and a budget a frame: the people in view every look, the town around every 10-30 s, everyone saved elsewhere one time a game day (counted only); off, exactly as before");
            b.AppendLine("QuarrelLadder = " + B(QuarrelLadder) + "   ; with MindLive on: an argue or a brawl between two people the mind knows climbs or cools by the quarrel ladder (words, a shove, blows, a gun drawn; an apology, a joke, backing down, a friend stepping in, a drink, the law in sight) from their natures and the surroundings; the shot is never fired. Off: the fixed scenes");
            b.AppendLine("ThoughtVariety = " + B(ThoughtVariety) + "   ; no two people near you think the same thought-shape within an hour of play (the names aside; within a game hour until 0.7.16), and a kind of thought that has had the last few turns gives way to the others (the weather, the place, the day), so a town doesn't read as one worried chorus; false: the old choice");
            b.AppendLine("SaidLately = " + B(SaidLately) + "   ; the street's scenes and chance meetings pass over a line said in the last few hundred and take the next in its pool, so their talk repeats far less in an hour of play (no AI model needed); false: the old picks");
            b.AppendLine("MindStandUp = " + B(MindStandUp) + "   ; with MindLive on: who steps into a rough or a brawl between two others comes from the onlookers' own minds (what it means to them, how many are watching, what a man or a woman of 1899 may let show), and the bully backs down or goes on from his; false: the old roll");
            b.AppendLine("BullyTurnsHothead = " + BullyTurnsHothead + "   ; with MindStandUp on: percent a hot-tempered bully who holds his ground turns on the man who stepped in (a shove, a word, and off) instead (0-100)");
            b.AppendLine("BullyTurnsProud = " + BullyTurnsProud + "   ; ... a proud bully (0-100)");
            b.AppendLine("BullyTurnsColdGreedy = " + BullyTurnsColdGreedy + "   ; ... a cold or a greedy one (0-100)");
            b.AppendLine("BullyTurnsCoward = " + BullyTurnsCoward + "   ; ... a coward (0-100)");
            b.AppendLine("BullyTurnsOthers = " + BullyTurnsOthers + "   ; ... any other bully (0-100)");
            b.AppendLine("BullyTurnsDrunk = " + BullyTurnsDrunk + "   ; added when the bully is drunk (0-100)");
            b.AppendLine("BullyTurnsMeant = " + BullyTurnsMeant + "   ; taken off when the one who stepped in looks like he means it (a proud or hot-tempered man) (0-100). Never on a woman who steps in");
            b.AppendLine("MindPlans = " + B(MindPlans) + "   ; with MindLive on: people carry plans over the next few days (to get even with someone who wronged them, to thank someone who helped, to pay a debt back, to court a sweetheart); near you, they go to find that person, and the scene it wants plays between them");
            b.AppendLine("MindActs = " + B(MindActs) + "   ; with MindLive on: people near you do what their minds choose: take a stroll, sit down on the nearest bench or chair, go to the bar or the card table, get on with the work, head off home, keep clear of you or of someone they can't abide, go over to a friend for a word; never someone busy, a few at a time (people sat or leaning look up from their seat: MindActsSeatedLooks; they get up for the rest: MindActsScenarios)");
            b.AppendLine("MindActsSeatedLooks = " + B(MindActsSeatedLooks) + "   ; with MindActs: people watch you or the street when their minds say so, people sat or leaning look up at you or a friend from their seat (the seat never touched), someone who wants to be seen stands where the street can see them, and someone already at what they chose (a drink at the bar, a rest on a bench) counts as doing it");
            b.AppendLine("MindRetries = " + B(MindRetries) + "   ; with MindActs: someone whose mind chose something they could not do just then (sat, on a horse, busy) tries again a minute later, up to three times; the AI model may be asked for people sat or leaning too, offered only what they can do from where they sit");
            b.AppendLine("MindActsScenarios = " + B(MindActsScenarios) + "   ; with MindActs: people sat or leaning get up when their minds want something else (a bench, the bar, a wall to lean on, their work, a walk home, someone to talk to), the game's own way and never pulled out of a chair; rests, meals, drinks and work go to a spot of that kind nearby (a bench or steps, a wall or a post, the bar rail, a broom), else something fitting where they stand; one starts every few seconds near you, three at most at once, never two side by side");
            b.AppendLine("ActThoughts = " + B(ActThoughts) + "   ; a thought over the head of someone near you as they start what their own mind chose (heading home tired, a drink for the thirst, keeping clear of you; with MindActs) and at each turn of an encounter they settle themselves (a callout, a domestic, the hogtied, a favour, a warning); written lines, works without the AI model (with the model on and answering, its own thought comes first and these fill in where it gave none); one person at most once in 3 minutes, never on top of another thought of theirs");
            b.AppendLine("ActThoughtEachSeconds = " + ActThoughtEachSeconds + "   ; with ActThoughts: one person's thought with what they chose at most once in this many seconds (20 to 600)");
            b.AppendLine("ActThoughtGapSeconds = " + ActThoughtGapSeconds + "   ; with ActThoughts: none within this many seconds of another, different thought of theirs (0 to 120)");
            b.AppendLine("ControlRing = " + B(ControlRing) + "   ; with MindActs: the people the mind knows near you are kept at what their minds choose, one act straight into the next, instead of the game taking them back between acts; less in the saddle, nobody at a gallop or on a train; draws itself in if it costs too much (F7: TEST mind acts). false: the older HeldRing below, if that is on");
            b.AppendLine("CrowdPicks = " + B(CrowdPicks) + "   ; with [Scenes] FightCrowds: how a crowd takes a fight (eggs them on, calls for it to stop, shouts for the law, goes quiet), one decision for all of them: the AI model's if it is on, the rules' if not");
            b.AppendLine("ComposedModel = " + B(ComposedModel) + "   ; with ComposedActs and the AI model: the model chooses which moves and in what order, from a short numbered list in plain words; a bad or late answer falls back to the rules");
            b.AppendLine("ComposedActs = " + B(ComposedActs) + "   ; with ControlRing: someone held, with a strong enough feeling, shows it in two or three of the game's own moves in an order that can follow (a look, a shrug, walking off); only moves the game has been seen to play");
            b.AppendLine("ControlRingPicks = " + B(ControlRingPicks) + "   ; with ControlRing and the AI model: as someone comes into the ring, the model may change the next step of their errand; late or unreadable, the errand stands (within PicksAtOnce)");
            b.AppendLine("ControlRingPlans = " + B(ControlRingPlans) + "   ; with ControlRing: someone held has a short errand of two to four things (the store, a talk, home) that they carry on with if they leave your side and come back; a strong feeling still comes first");
            b.AppendLine("ControlRingSeated = " + B(ControlRingSeated) + "   ; with ControlRing and MindActsScenarios: those sat or leaning near you are held too, and after a minute or so someone whose mind wants something else gets up for it, a few at most at a time");
            b.AppendLine("ControlRingAhead = " + B(ControlRingAhead) + "   ; with ControlRing: people along the way you are going are taken in a little sooner than those behind you, so they are already about something when you reach them");
            b.AppendLine("ControlRingKeepAt = " + B(ControlRingKeepAt) + "   ; with ControlRing: someone held who stops getting where they were sent is given the order again, a few times at most; never over a fight, a flee or a fall");
            b.AppendLine("ControlRingMetres = " + F(ControlRingMetres) + "   ; with ControlRing: how near you, in metres, on foot (6 to 25)");
            b.AppendLine("ControlRingMost = " + ControlRingMost + "   ; with ControlRing: how many people at once, on foot (2 to 20)");
            b.AppendLine("HeldRing = " + B(HeldRing) + "   ; (in testing) with MindActs: the people the mind knows within HeldRingMetres of you are held: when one of their acts ends the next begins at once (what their mind chose, else what it ranked next, else the nearest seat or spot, else a short stroll) instead of the game taking them back. Let go 13 m farther out, after HeldRingMinutes, or the moment a gun, a fight, a scene, Talk, the law or enRAGE has a claim on them");
            b.AppendLine("HeldRingMetres = " + F(HeldRingMetres) + "   ; with HeldRing: how near you, in metres, people are held (4 to 20)");
            b.AppendLine("HeldRingMinutes = " + HeldRingMinutes + "   ; with HeldRing: how long one person is held at most (1 to 30 real minutes), then two minutes to their own ways");
            b.AppendLine("LadderShots = " + B(LadderShots) + "   ; with MindLive on: a quarrel that climbs all the way to the shot is fired: the one who pushed it there fires, the other fires back if he carries a gun or runs, and whoever falls stays dead; never in a saloon, with the law near, or unless both carry guns. false: the standoff breaks off");
            b.AppendLine("MindJoinFights = " + B(MindJoinFights) + "   ; with MindLive on: while two people fight, an onlooker may join in on a side: a friend of one, someone with bad blood against the other, a drunk hothead in a saloon; most watch, fewer the bigger the crowd, nobody with the law at hand; a man against a woman seldom (1899); at most three join");
            b.AppendLine("MindBrawls = " + B(MindBrawls) + "   ; with MindLive and MindJoinFights on: a fight can grow into a brawl, a wave at a time: friends and grudges pull people in, and the bigger it gets the less it takes, likelier in a saloon or with drink; the law at hand holds most back on the street, fewer in a saloon riot; false: at most three join, never with the law near");
            b.AppendLine("BrawlMost = " + BrawlMost + "   ; the most fighting at once in one brawl, the two who began it among them (3 to 20)");
            b.AppendLine("BrawlLaw = " + B(BrawlLaw) + "   ; with MindLive on: a lawman who sees a fistfight or a brawl comes over and shouts to break it up; those who stop are let off with a word, whoever keeps swinging gets his fists and is hogtied; a gun drawn in it gets his gun");
            b.AppendLine("BrawlPullsYouIn = " + B(BrawlPullsYouIn) + "   ; with MindBrawls on: in a brawl of four or more within a few steps of you, someone you've hit, someone against you or a drunk hothead may come at you; whoever does is fair game");
            b.AppendLine("BrawlDraws = " + B(BrawlDraws) + "   ; with MindBrawls and LadderShots on: rarely, an armed man losing a brawl who is drunk, hot-tempered, or proud with bad blood, or set upon by two or more, draws, by the quarrel ladder's own odds for the gun; he may fire, and the law treats it as a shooting");
            b.AppendLine("BrawlDrawShare = " + BrawlDrawShare + "   ; with BrawlDraws on: how much of the quarrel ladder's own odds for the gun a man losing a brawl has, in percent (0-100; 40 as built, a quarter before)");
            b.AppendLine("BrawlFistsFatal = " + BrawlFistsFatal + "   ; with MindBrawls on: a man beaten down in a brawl is killed this percent of the time in a plain beating, else knocked out for 20-40 s and up again; more often with several on him, a drunk or a hothead at him, bad blood or an old man down, less with the law close, a friend of his in it or a fair fight; one of enRAGE's people is only ever knocked out (0-100; 0: nobody dies of fists)");
            b.AppendLine("BrawlJoinMetres = " + F(BrawlJoinMetres) + "   ; with MindJoinFights on: onlookers within this many metres of a fistfight may join it, and those over 10 m off run over to it, counted for their side on the way so it doesn't end before they get there (10-60)");
            b.AppendLine("BrawlCrowdMetres = " + F(BrawlCrowdMetres) + "   ; with [Scenes] FightCrowds on: people within this many metres of a fight notice it and walk over to watch and cheer, up to 6 round two men and up to 16 round a brawl (a busy street fills it); the law comes from this far too, 35 m at least (10-60)");
            b.AppendLine("BrawlLeastSeconds = " + BrawlLeastSeconds + "   ; with MindBrawls on: a brawl (four or more fighting, or two who joined) goes on at least this many seconds from its first blows, unless one side is all down (15-120)");
            b.AppendLine("BrawlJoinBoost = " + BrawlJoinBoost + "   ; with MindBrawls on: how much readier onlookers are to join a fight (0 to 60; 20 as it comes, 0 as before): higher, two men fighting grow into a big brawl more often");
            b.AppendLine("BrawlStarterDot = " + B(BrawlStarterDot) + "   ; with MindBrawls on: the one who threw the first punch wears a small red dot on the map while the fight lasts, and stepping in against him is self-defence (on as it comes)");
            b.AppendLine("SeatedBrawls = " + B(SeatedBrawls) + "   ; two people who are sat or leaning (at a bar, at a table) may still come to blows: they get up first, the game's own way; false: a quarrel between them stays words (on as it comes)");
            b.AppendLine("BrawlOpeningBoost = " + BrawlOpeningBoost + "   ; with MindBrawls on: how much readier onlookers are to join in a fight's first 10 seconds, on top of BrawlJoinBoost (0 to 60; 20 as it comes): someone may still go down early, but more join before they do");
            b.AppendLine("MindOnlookers = " + B(MindOnlookers) + "   ; with MindLive on: someone who only watched a quarrel, a fight or something you did acts on what they feel about it, by their nature and what 1899 lets them show (the frightened walk off, a man who keeps his fear off his face slips away, a gun or something uncanny roots them to the spot, contempt shakes its head, the pious walk off disapproving, a keen eye turns to watch), at most two each time; false: they only turn their heads");
            b.AppendLine("GroupActs = " + B(GroupActs) + "   ; with MindLive on: acts for several people at once: after a street fight, when two friends of the man who went down are near, the first friend's mind (the model's, for someone it knows) picks between tending him alone, fetching the other and closing round him (a scene of three: the two see him up and walk him off) and leaving it; and an onlooker whose mind says leave takes their spouse or sweetheart within 6 m along, side by side, with a word; false: one friend tends, onlookers leave alone");
            b.AppendLine("OnlookersRunMetres = " + F(OnlookersRunMetres) + "   ; with MindOnlookers on: how far, in metres, an onlooker badly frightened by real danger runs from it (5-60)");
            b.AppendLine("OnlookersRunning = " + OnlookersRunning + "   ; how many onlookers may run from one happening once one does, the fright spreading (1-8; fewer the braver the town, [World] Bravery)");
            b.AppendLine("MindWakes = " + B(MindWakes) + "   ; with MindLive and West World on: the Awakening follows what people have lived through: the uncanny, a killing or a buried man seen walking shake their sense that things are as they seem, so they wake sooner, a good turn steadies them, and the moment that sense gives way they begin to stir; someone who saw a man die and sees him walk again is frightened by it and begins to wake (with MindOnlookers on, backs away from him, or, struck hard, stands rooted); false: the Awakening by its own roll alone");
            b.AppendLine("MindPassing = " + B(MindPassing) + "   ; with MindLive on: when two who know each other pass, the greeting follows how the first feels about the other lately and the day they are having: sore at a friend, a nod and no word; afraid, eyes away; ashamed before them, eyes down; grateful, a warm word; a sore day, a nod; a glad one, a word for anyone they know; and someone who feels something about you from a while back shows it as you pass (grateful, a hat tip and a word; sore, a hard look; afraid or shamed, eyes away); false: the greeting the tie gives");
            b.AppendLine("MindPassingLasts = " + B(MindPassingLasts) + "   ; greetings as you pass remember what you did for hours, not minutes (with MindPassing)");
            b.AppendLine("MindHelps = " + B(MindHelps) + "   ; with MindLive on: when someone takes a fall, the one most moved by it of those who saw it is the one who comes, and how they help comes from what they feel (a hand up, a gruff one, or walking past); a foe still sneers; false: the nearest, by the tie and a roll");
            b.AppendLine("MindRescue = " + B(MindRescue) + "   ; with MindLive on (West World): when someone fully awake begins to say their last words, someone near who is moved by it (a friend, or a kind stranger) may walk over and try to talk them down; a friend is heeded more often; false: nobody steps in");
            b.AppendLine("RescueChance = " + RescueChance + "   ; with MindRescue on: of the despairs where someone near is moved to step in, how often in percent they really do before it is too late (0-100); lower it to see more despair, raise it to see more people saved");
            b.AppendLine("MindMeetings = " + B(MindMeetings) + "   ; with MindLive on: what passes between two people who meet near you leans by the day they are having: two sore people come to words sooner than small talk, two glad ones the other way, and someone lonely stops for a word; false: by their reasons alone");
            b.AppendLine("MoodsCarry = " + B(MoodsCarry) + "   ; with MindLive on: rain, snow, a storm, fog and dust wear on the mood of everyone out in them (a quarter of it under a roof; the unsteady feel it more), and a feeling shown on one face in a crowd is caught by those near who felt less of it themselves (the agreeable and the open catch more); false: moods from their own feelings alone");
            b.AppendLine("MindTalks = " + B(MindTalks) + "   ; people near you talk to each other from what's on their minds: the worry they're living through, the times they live in (the election, the war, the reservations, the new machines...), the stranger, a plan they carry, the town's talk, what they're short of, the day; the other answers from their own nature (kindly, coldly, sharply, and on the times from their own side of it), and with the AI model on, in its own words. No AI model needed");
            b.AppendLine("TimesTalk = " + B(TimesTalk) + "   ; with MindTalks and [World] HistoricalAccuracy on: any two people who stop near you may talk over the times: 1899's news, who holds office (McKinley, Hobart, Root, Henderson; Roosevelt's men in 1907) and who is talked of (Bryan, Roosevelt, Dewey), the wheat and the bank, the railroad, the catalogue, the dry vote, the old soldiers; by day in the street, any hour in the saloon; voiced with VoicedDialogue. False: only now and then, among the other things on their minds");
            b.AppendLine("TimesTalkShare = " + TimesTalkShare + "   ; with TimesTalk: how often, percent (100 as it comes: about one street scene in five is talk of the times; 0 to 300)");
            b.AppendLine("MindChanceMeetings = " + B(MindChanceMeetings) + "   ; with MindLive on: a chance meeting near you comes from the two people's minds (friends talk and sing, foes quarrel or turn their backs, the sore snap, money worries trade or ask a loan, drink tells tall tales); false: rolled by the place alone");
            b.AppendLine("Language = " + B(Language) + "   ; the AI MODEL (OLLAMA) switch at the top of the settings panel's Essentials page. With MindLive on, the mind's own thoughts, choices and answers are worded by a local language model running on your own PC (Ollama; nothing leaves the machine); every answer is checked for the period, the names and the length, and the written line shows whenever the model's won't do. RealNPCs turns it on by itself when it finds Ollama with the model at a start, unless you have chosen (LanguageChosen)");
            b.AppendLine("LanguageChosen = " + B(LanguageChosen) + "   ; set true by itself whenever you change Language (in the settings panel or in this file), or when Language is on at a start, so a Language you turned off stays off. While false and Language is off, each start asks Ollama at LanguageUrl in the background (a few seconds at most) whether it runs and holds LanguageModel, and if it does, turns Language on, once, with a notice");
            b.AppendLine("LanguageVariety = " + B(LanguageVariety) + "   ; (in testing) with Language on: the model's thoughts vary: each one goes to one thing (their work, someone in sight, a memory, the body, the worry, money, kin, faith, the town's talk, a small thing seen; the weather only now and then) in a shape of its own (fragments, a plain sentence, a question, a reckoning, an opinion, a saying half-remembered), never opening the way their last few did; and nobody doubts the world is real without cause (from the second stage of waking, or after something strange: a buried man seen walking, the night visitor, a death they woke from)");
            b.AppendLine("ModelWakes = " + B(ModelWakes) + "   ; (in testing) with Language, MindWakes and LanguageVariety on: someone with cause who wonders at the world in a thought of their own is shaken a little more by it, so the model's own wondering can start them stirring; off, only what they live through moves them");
            b.AppendLine("WrittenWakes = " + B(WrittenWakes) + "   ; with MindWakes on: someone below the second stage of waking who has cause (something seen that shouldn't have been, a death they woke from, a moment lived twice) now and then wonders at the world in a written thought, at most once a day, and it shakes them a little as the AI model's wondering does; no AI model needed");
            b.AppendLine("LanguageTalk = " + B(LanguageTalk) + "   ; (in testing) with Language on: when you Talk to someone, the model words their answer by their nature, mood and what's on their mind (what it means stays the same: the drink taken, the money refused); if it's slow or says something it shouldn't, the written line");
            b.AppendLine("TalkModelWaitSeconds = " + F(TalkModelWaitSeconds) + "   ; with LanguageTalk on: how long, in seconds, the model may take to word a Talk answer before the written line is said instead (1-7; a request to the model gives up at 8)");
            b.AppendLine("TalkTyped = " + B(TalkTyped) + "   ; Talk's last row, \"Say something else...\", opens a line to type your own words on (keyboard only): Enter says it, Backspace deletes, Esc leaves. They answer from who they are and how they stand with you: with the AI model and LanguageTalk on, in their own words (what they'd not tell a stranger they keep to themselves); without it, a written answer by what your words seem to be about (the news, their name, how they are, their trouble, the weather, the way, a drink, money, God, guns, the law, thanks, sorry, goodbye), and anything else met with a plain non-answer. Nothing is paid or promised by typing");
            b.AppendLine("TalkVoice = " + B(TalkVoice) + "   ; (off as it comes) Talk's row \"Say it out loud...\" (keyboard or pad): the voice server listens to your microphone until you've said something and fallen quiet (or Space, Enter or a pad's A; Backspace, Esc or B never mind), writes it down, and it's said to them as if typed, with the same answers. Needs the voice server running with its transcriber (voice\\install-voice.cmd puts it in; about 150 MB more, once; it runs on the CPU and takes none of the card) and a microphone. The sound is never kept and never leaves this PC");
            b.AppendLine("TalkVoiceSeconds = " + TalkVoiceSeconds + "   ; with TalkVoice: it listens for at most this many seconds (3 to 12)");
            b.AppendLine("LanguagePicks = " + B(LanguagePicks) + "   ; (in testing) with Language on: for people near you, the model picks what they do next from the few things their mind allows (the best three, and a plan's step), by their nature and how they feel; if it's slow or says nothing sensible, their mind's own choice stands");
            b.AppendLine("MindStartsScenes = " + B(MindStartsScenes) + "   ; people near you start scenes from their own reasons with someone else near (a quarrel to pick up, a friend to comfort, a debt to call in), on top of the street's own scenes (at most one a minute, never past MainScenesAtOnce), with the written why. No AI model needed; with the model's picks on, the model chooses among those reasons as well; false: scenes start only from the street's own timing");
            b.AppendLine("PickMetres = " + PickMetres + "   ; how far from you (metres, 5 to 60) the model picks what people do; past 15 m each person is asked half as often; the mind itself looks 25 m out, so more than 25 changes nothing yet");
            b.AppendLine("PicksAtOnce = " + PicksAtOnce + "   ; how many people's picks may wait on the model at once (1 to 16); F7's TEST model: line shows how long answers take, so you can raise it while they stay quick");
            b.AppendLine("MindTownMood = " + B(MindTownMood) + "   ; (in testing) with MindLive on: a killing, a robbery or a mugging leaves the whole town on edge for some hours (the fear halves every three game hours), worst in the first hours and at night (people out go home, keep clear, keep watch; their thoughts say why), and the same kind of trouble seen again in a town that week moves onlookers less each time");
            b.AppendLine("HushHours = " + F(HushHours) + "   ; with MindTownMood on: for this many game hours after a killing in a town (the latest one only), the street hushes: no street fights, thefts or noon callouts from its own timing (0-12; 0 never, as it comes: a livelier world; 2 for the old quiet after a killing). Someone beaten to death in a brawl, a Testing row's own trouble (your own killings still hush, but not of the test's own people), and brawls, roughs and standoffs never hush a street");
            b.AppendLine("HushHoursSaintDenis = " + F(HushHoursSaintDenis) + "   ; the same for Saint Denis, a city used to trouble (0-12 game hours; 0 never)");
            b.AppendLine("MindTempers = " + B(MindTempers) + "   ; (in testing) with MindLive on: someone slighted, quarrelled with or beaten in the last few hours may take it out on someone who won't hit back; and in Lemoyne's towns, where honour rules, quarrels climb a little faster and backing down costs more");
            b.AppendLine("StrainChance = " + StrainChance + "   ; with MindTempers on: someone with two or more troubles at once snaps at someone who won't hit back this percent of the time when they meet (0-100; a coward is always picked on; the timid and the loyal never snap)");
            b.AppendLine("ThoughtsAloud = " + B(ThoughtsAloud) + "   ; as a thought shows over someone within 12 m, they may say something aloud in their own recorded voice (most thoughts stay unspoken). No AI model needed: what they feel most decides it (a curse when angry, a frightened word, thanks, relief; grief and shame stay inside). With the AI model on and a voice list for that voice, the model picks the remark that fits instead");
            b.AppendLine("VoicedThoughts = " + B(VoicedThoughts) + "   ; a thought over someone within VoiceMetres is murmured in their own voice a moment after it shows (one at a time, each person once in 90 s). Needs the local voice server running (voice\\start-voice-server.cmd after voice\\install-voice.cmd once; an NVIDIA card with about 4 GB free); without it nothing changes and nothing is heard");
            b.AppendLine("VoicedDialogue = " + B(VoicedDialogue) + "   ; what people say near you in RealNPCs' words (the street's scenes, their talks with each other, Talk's answers, the encounters) is said in their own voice by the voice server, the subtitle waiting for the voice");
            b.AppendLine("VoiceUrl = " + VoiceUrl + "   ; where the voice server listens");
            b.AppendLine("VoiceMetres = " + F(VoiceMetres) + "   ; voiced within this many metres of you (5 to 30)");
            b.AppendLine("VoiceVolume = " + VoiceVolume + "   ; the voices' loudness, percent (10 to 200)");
            b.AppendLine("VoiceRecord = " + B(VoiceRecord) + "   ; the voices recorded from your own game as you meet people: someone whose voice has no clip yet says one of the game's own lines once, in a quiet moment, and the voice server keeps it");
            b.AppendLine("LanguageUrl = " + LanguageUrl + "   ; where Ollama listens");
            b.AppendLine("LanguageModel = " + LanguageModel + "   ; the model it holds (ollama pull <name>). " + EnrageWestworld.Core.Language.DefaultModel + " as it comes: about 2 GB of the graphics card; " + EnrageWestworld.Core.Language.OldDefaultModel + " words a little better and takes about 5 GB. Either leaves the card after 5 minutes with nothing asked. While " + EnrageWestworld.Core.Language.DefaultModel + " isn't pulled but " + EnrageWestworld.Core.Language.OldDefaultModel + " is, the 8B is used");
            b.AppendLine();
            b.AppendLine("[Ticker]");
            b.AppendLine("Ticker = " + B(Ticker) + "   ; a line at the bottom of the screen: what the people near you are thinking, else news");
            b.AppendLine("TickerNearbyMetres = " + F(TickerNearbyMetres) + "   ; people this close count as near you");
            b.AppendLine("TickerSeconds = " + TickerSeconds + "   ; how long each line stays before the next");
            b.AppendLine("TickerY = " + F(TickerY) + "   ; height on screen, 0 = top, 1 = bottom");
            b.AppendLine("TickerScale = " + F(TickerScale) + "   ; text size");
            b.AppendLine("TickerWidth = " + F(TickerWidth) + "   ; share of the screen width it may use");
            b.AppendLine("TickerNextKey = " + TickerNextKey + "   ; shows the next line on the ticker now (on a long line, its next part first); while the bar shows two or more scenes nearby, the next of them");
            b.AppendLine("SpotKey = " + SpotKey + "   ; records where you stand as a named place of the town (platform, wharf, pithead, stockyards, cemetery, bandstand, churchsteps, corral, mainstreet, saloonporch, bankdoor, jailyard, showground, riverbank) into spots.txt beside this file; Shift and the key change the kind (shown on the news line); refused indoors. The trains, the hangings and the shows stand at these; safe to ignore");
            b.AppendLine("EventKey = " + EventKey + "   ; opens the small menu while a prompt shows at the foot of the screen: speak to the court at a trial, be sworn in before a hanging, the cell at night inside the jail, the Wild West show's rows; the game's own J is the journal, so change this if the two get in each other's way");
            b.AppendLine("ThoughtSounds = " + B(ThoughtSounds) + "   ; when a thought of someone within a few steps comes up on the ticker, they make a small wordless sound (at most every 3 minutes each)");
            b.AppendLine("SceneCues = " + B(SceneCues) + "   ; a line on the ticker when something starts up at your back within 22 m (\"Raised voices behind you.\", \"A scuffle behind you.\", \"Laughter behind you.\"), at most once a minute");
            b.AppendLine("TickerGapSeconds = " + TickerGapSeconds + "   ; a short pause between one thought and the next, so they don't run together (0: none)");
            b.AppendLine("ThoughtMarker = " + B(ThoughtMarker) + "   ; a small gold mark over the person whose thought is on the ticker");
            b.AppendLine("ThoughtsOverHeads = " + B(ThoughtsOverHeads) + "   ; the thoughts of people near you show above their head in small type, not on the bar (the news and the rest stay on the bar; a thought whose thinker is out of view is passed over, never put on the bar)");
            b.AppendLine("ThoughtsOverHeadScale = " + F(ThoughtsOverHeadScale) + "   ; text size of thoughts above heads (TickerScale is the bar's)");
            b.AppendLine("HistoryNotes = " + B(HistoryNotes) + "   ; the history behind the thought: when someone near you thinks of the real news of the day, the news box says what happened, as the papers had it (once a story, one every 90 seconds at most); false: the thought alone");
            b.AppendLine("ThoughtsFollowOn = " + B(ThoughtsFollowOn) + "   ; for a few minutes after a fight, a run for it or a gun in their face, someone's next thoughts pick up from it; with the AI model on, some carry on from the last thing they thought");
            b.AppendLine("RoadThoughts = " + B(RoadThoughts) + "   ; out of town, the riders, drivers and walkers you pass think something about the road and about you, and people at a scene think by their part in it");
            b.AppendLine("RoadThoughtsMetres = " + F(RoadThoughtsMetres) + "   ; riders and drivers this far off, out of town (at least TickerNearbyMetres)");
            b.AppendLine("Asides = " + B(Asides) + "   ; while someone near you talks, the person listening may have a thought about it over their head (needs ThoughtsAtOnce above 1)");
            b.AppendLine("WeatherChecked = " + B(WeatherChecked) + "   ; what people think and say about the weather is held to what's really out, the temperature too: no heat talk in a cold drizzle");
            b.AppendLine("WeatherTalk = " + B(WeatherTalk) + "   ; with WeatherChecked: people remark on the weather more the worse it is, and for a couple of minutes after it turns (the first drops, thunder, snow, the rain stopping)");
            b.AppendLine("Opinions = " + B(Opinions) + "   ; with PairThoughts: what one thinks of the other, from what's passed between them (fear of who hurt them, contempt, envy of the better off, respect), in their thoughts when two who know each other are in view");
            b.AppendLine("PairThoughts = " + B(PairThoughts) + "   ; two people in view with a history between them each think about the other, at the same moment, in the boxes over their heads. Needs ThoughtsAtOnce 3 or more");
            b.AppendLine("ThoughtsAtOnce = " + ThoughtsAtOnce + "   ; how many people's thoughts may be up over their heads at once, arriving one after another (1 to 4; 1: singly, as it was; needs ThoughtsOverHeads)");
            b.AppendLine("TickerShowsScene = " + B(TickerShowsScene) + "   ; while a street scene plays out within 40 m, the bar at the bottom says what is going on and who is in it (\"Nearby: ...\")");
            b.AppendLine("ScenesOnTicker = " + B(ScenesOnTicker) + "   ; what people say near you (a street scene, a hello, the doctor, the law) shows as they say it in the subtitle box, the game's way (their name, the line); never on the bar; [Output] Subtitles = off hides it");
            b.AppendLine("ScenesOnTickerMetres = " + F(ScenesOnTickerMetres) + "   ; what people say within this many metres shows as a subtitle (5-80)");
            b.AppendLine();
            b.AppendLine("[Display]");
            b.AppendLine("KnownOnMap = " + B(KnownOnMap) + "   ; a grey mark with their name on the regulars you've met, near you in town, kept up to date every half a minute");
            b.AppendLine("CleanScreen = " + B(CleanScreen) + "   ; one switch for a clear screen: hides the names over heads, the feeling bars and the meter, the mind cards and wake tags, thoughts over heads, the ticker, the Around town box and the VRAM gauge (what people say to you, the Talk menu and this panel stay); false brings it all back, each as you had it");
            b.AppendLine("FeelingBars = " + B(FeelingBars) + "   ; bars under people near you who feel something about you: their fear, fondness and anger, as enRAGE keeps them");
            b.AppendLine("FeelingBarsMetres = " + F(FeelingBarsMetres) + "   ; how close they must be (the nearest three in plain view get bars)");
            b.AppendLine("FeelingBarsShowEmpty = " + B(FeelingBarsShowEmpty) + "   ; also show the empty part of each bar, so a low value reads as a little out of a whole bar");
            b.AppendLine("NameCardsMourns = " + B(NameCardsMourns) + "   ; \"Mourns one you killed\" on the name card of someone grieving a friend you killed, for a week from when word reached them (\"Owes you\" and \"Stood by you\" show first)");
            b.AppendLine("NameCardsRobbed = " + B(NameCardsRobbed) + "   ; \"Robbed you\" on the name card of the pickpocket who got clean away with your money, until you catch him at it and have yours back");
            b.AppendLine("NameCardsStood = " + B(NameCardsStood) + "   ; \"Stood by you\" on the name card of someone who fought for you this week: an ally who came to your aid, a friend who ran down your thief");
            b.AppendLine("NameCardsOwes = " + B(NameCardsOwes) + "   ; \"Owes you\" on the name card of someone who owes you (you saved them, or stood with them in a fight), for the month they owe you: they'll come to your aid whatever the odds");
            b.AppendLine("NameCards = " + NameCards + "   ; names over heads in the game's title type, the way enRAGE drew them: off, targeted (only the person you're locked on to or aiming at), known (people RealNPCs knows: enRAGE's records and townsfolk, regulars, anyone who thought or spoke or took part in a scene), everyone (the rest get what they look like, \"A townsman\", in grey). While enRAGE runs with its own cards on, ours stay off unless NameCardsBesideEnrage; off on the settings panel turns enRAGE's own cards off too");
            b.AppendLine("NameCardsMetres = " + F(NameCardsMetres) + "   ; strangers' names within (metres); they fade out near the edge");
            b.AppendLine("NameCardsKnownMetres = " + F(NameCardsKnownMetres) + "   ; the names of people you know within (metres)");
            b.AppendLine("NameCardsBesideEnrage = " + B(NameCardsBesideEnrage) + "   ; draw ours even while enRAGE draws its own (two cards over each head enRAGE knows)");
            b.AppendLine("NameCardsAlways = " + B(NameCardsAlways) + "   ; a name over everyone within NameCardsMetres, always: hidden only by walls (not by a table or someone in the way), up to sixteen people, drawn before the Testing overlays; false: only where the view is clear, the ten nearest");
            b.AppendLine("NameCardsStrangerNames = " + B(NameCardsStrangerNames) + "   ; someone you haven't met shows their name with Stranger under it; false: Stranger alone, until you've greeted them or dealt with them");
            b.AppendLine("NameCardsSecondLine = " + B(NameCardsSecondLine) + "   ; the smaller line under each name: their temper and trade, \"Stranger\", their gang, \"Wanted\", a nemesis's mood. Off shows the name alone; NameCards = off hides the cards altogether");
            b.AppendLine("WhosWho = " + B(WhosWho) + "   ; the papers you carry end with Who's who, the people you have met, the most recent first: who they are, how they take you, who they are friends or at odds with (and, with [Mind] MindLive, how they feel about you lately); read only");
            b.AppendLine("Overheard = " + B(Overheard) + "   ; the papers end with Overheard: what went on near you in the last game day (a fight, a quarrel, a courting, a killing between others, within 22 m), one sentence each, newest first; read from what the add-on already keeps, nothing saved");
            b.AppendLine("GrudgeMeter = " + B(GrudgeMeter) + "   ; top right: strangers whose grudge against you is building, with the mark where they'd become a nemesis, and the nemesis after you");
            b.AppendLine("VramGauge = " + VramGauge + "   ; a small gauge at the top left, under the game's own updates: how much of your graphics card's memory is in use (the game, the AI model and everything else on it), as Task Manager counts it, e.g. VRAM 11.3 / 15.6 GB; amber over 85%, red over 95%. model: only while the AI model ([Mind] Language) is on; on: always; off: never");
            b.AppendLine();
            b.AppendLine("[WestWorld]");
            b.AppendLine("WestWorld = " + B(WestWorld) + "   ; master switch: the people who start to see their world is made. false turns off everything in this section");
            b.AppendLine("AwakeningShare = " + F(AwakeningShare) + "   ; percent of people who slowly wake up and see the world for what it is, a part of one too (0.6); 0 turns the Awakening off");
            b.AppendLine("AwakeningDays = " + AwakeningDays + "   ; in-game days a woken person spends at least on each of the first three stages (each day after, a better than even chance of the next)");
            b.AppendLine("AwakeningBreakdown = " + AwakeningBreakdown + "   ; percent of the time an unravelling or awake person breaks down (kneels, shouts at the sky, runs) rather than keeping it together");
            b.AppendLine("AwakeningDespair = " + AwakeningDespair + "   ; percent chance that a fully awake ordinary person, alone, can't bear it and ends their own life, with a gun that fits them, poison, a knife, by running at a wall, by walking into the river (DespairWater) or by stepping off a drop (DespairFall) (0-100, 0 turns this off; a content note is in the readme)");
            b.AppendLine("WakeVoices = " + B(WakeVoices) + "   ; the waking say the game's own recorded lines in the wrong places: to nobody, the same words again, to the sky, at you; a little at the first stage, a great deal by the fourth, with a thought over the head for what they make of it");
            b.AppendLine("LoopVoice = " + B(LoopVoice) + "   ; the waking regulars of a town hear their habit as a voice giving orders at the hour (the ticker, once a day each, from the second stage: Seven o'clock. The saloon. You'll be at the saloon.), answer it back from the third, and at the fourth break the loop now and then where the habit forbids (a broom on an empty street before dawn, the church wall at noon, the river's edge to the knee; one a town a day; the church steps and the wharf from the spot table); once, three thoughts end with the voice known as their own (the centre), after which an ending may be a reset: back to the start, cheerful and blank, their friends noticing for two days. F7 TEST loop");
            b.AppendLine("HostNotes = " + B(HostNotes) + "   ; with WestWorld: half the time two waking people talk strangely, they test the world together instead (count the faces on the street, walk to the town's edge, tell each other their dreams), and each wakes faster for three days after");
            b.AppendLine("Ghosts = " + B(Ghosts) + "   ; the reload noticed: a save loaded from before keeps what it undid as ghosts for a week (a killing, a death, a deed, a tie, a link; always kept, never rewound themselves); with this on, a victim of a rewound killing who walks the town again gets one sharp deja vu the first time you come within 12 m (a thought and an uneasy line; awareness +10), both sides of a rewound tie think of the quarrel or the kiss that didn't happen, the waking open their talk with the morning that happened twice for two days, and the ticker says how many times the clock turned back. F7 TEST ghosts");
            b.AppendLine("WakeLines = " + B(WakeLines) + "   ; two ways the Awakening spreads: each person at the fourth stage holds one line (The Tempest, Poe, Dickinson, Ecclesiastes) and says it as a talk closes with someone tied to them or waking too; a listener who already suspects (awareness 40 and over) and hears the same line from two different people within three days wakes a stage, at most one a town a day; and someone tied to a man they knew for dead who sees him alive again (a save loaded from before) looks, steps back, thinks of it once, awareness +10 and a stage at 60, the town chewing on it for a day. F7 TEST wakelines");
            b.AppendLine("DespairFall = " + B(DespairFall) + "   ; one more way despair ends: with a sheer drop of 7 m or more near (a bluff, a ledge, a high bank) and no gun or poison in them, they walk to the edge, stand and look out, and step off");
            b.AppendLine("DespairWater = " + B(DespairWater) + "   ; one more way despair ends: with the river's edge within 14 m (the game's own water height, about at their feet) and neither a gun nor poison chosen, 45% walk into the water and keep walking; six seconds in, they go under");
            b.AppendLine("WakeHold = " + B(WakeHold) + "   ; from the second stage, a waking person near you goes from one strange act into the next instead of going back to their day: stood dazed, drifting a few steps, pacing, freezing; let go for a fight, gunfire, the law after you, or a scene");
            b.AppendLine("WakeHoldMinutes = " + WakeHoldMinutes + "   ; how long one hold lasts before they are let go for a minute (1 to 15)");
            b.AppendLine("WakeGestures = " + B(WakeGestures) + "   ; the waking use their hands and bodies as they act: rubbing their eyes, pointing at the sky, a hand to the head, pleading; a clip that won't play or carries them off is dropped for the session (the log says which)");
            b.AppendLine("WakeStrange = " + B(WakeStrange) + "   ; from the third stage, the waking do strange things to the people around them: right up close and staring, a hand waved before a face, following someone step for step, telling them they aren't real, preaching to whoever is about; the other walks off, curses, laughs or takes fright");
            b.AppendLine("WakeByNature = " + B(WakeByNature) + "   ; with WakeStrange on: the strange thing each of them does comes from their temper (hotheads get in a face, the proud preach, cowards keep behind or test from arm's length) and from what they feel most (fear keeps its distance, anger confronts, grief turns outward), and they bother whoever they feel most about when near, else the person they have the strongest tie with, else whoever is nearest; their friends laugh it off and their enemies curse them; false: the roll and the nearest, as before");
            b.AppendLine("WakeStripChance = " + WakeStripChance + "   ; percent of the fully awake who, once, take every stitch off where they stand (never enRAGE's people: it knows them by their clothes); 0 for never; held back since 0.7.9, whatever this says: the first strip froze the game");
            b.AppendLine("WakeLashOut = " + WakeLashOut + "   ; percent of holds at the third and fourth stages that end with them turning on you or on someone near: fists while unravelling, their own gun when awake and carrying one; hotheads and the proud more often, cowards less. Coming at you or at someone else is fair game to stop; 0 for never");
            b.AppendLine("HostsQuestionReality = " + B(HostsQuestionReality) + "   ; people sometimes wonder whether their world is real");
            b.AppendLine("RealityChance = " + RealityChance + "   ; percent of remarks and thoughts that drift that way");
            b.AppendLine("Glitches = " + B(Glitches) + "   ; rarely, someone freezes and stares");
            b.AppendLine("Reveries = " + B(Reveries) + "   ; the dead may wake up again, remembering how they died and who did it");
            b.AppendLine("ReturnChance = " + ReturnChance + "   ; percent of the people enRAGE knows (not story characters) who wake up after you kill them");
            b.AppendLine("StrangerReturnChance = " + StrangerReturnChance + "   ; percent of strangers you kill who come back");
            b.AppendLine();
            b.AppendLine("[Stalkers]");
            b.AppendLine("Stalkers = " + B(Stalkers) + "   ; master switch: rarely, someone follows you from a distance and hides when you look. false turns off everything in this section");
            b.AppendLine("StalkerEveryMinutes = " + StalkerEveryMinutes + "   ; every this many real minutes of play, a roll for a stalker (1-120; 15 as built, 34 before)");
            b.AppendLine("StalkerChance = " + StalkerChance + "   ; percent chance on each roll (35 as built, 20 before: with 15 minutes, about once in 45 minutes of play)");
            b.AppendLine("StalkerBlack = " + B(StalkerBlack) + "   ; the stalker is a stranger dressed in black, who turns up out of sight behind you; never someone near you turned, nor one of enRAGE's people (enRAGE knows its people by their clothes, so they're never re-dressed); false: someone near you or one of enRAGE's people, as below");
            b.AppendLine("StalkerBlackModel = " + StalkerBlackModel + "   ; his model: the Testing row 'Stalker outfit: show the next candidate' shows each in turn and says the two lines to put here");
            b.AppendLine("StalkerBlackOutfit = " + StalkerBlackOutfit + "   ; his outfit preset in that model, from the same row (-1: as the game makes him)");
            b.AppendLine("StalkerRecords = " + B(StalkerRecords) + "   ; with StalkerBlack off: one of enRAGE's people may be the stalker (one who hates you most likely), while enRAGE has no use for them; handed back the moment it has, and after 20 minutes at most");
            b.AppendLine("StalkerNearbyMetres = " + StalkerNearbyMetres + "   ; with StalkerBlack off: on a hit, someone within this distance starts following you, someone with a history with you first (20-150)");
            b.AppendLine("StalkerStrangers = " + B(StalkerStrangers) + "   ; with StalkerBlack off: if nobody near can be turned for a few minutes, a stranger turns up behind you instead");
            b.AppendLine("StalkerRides = " + B(StalkerRides) + "   ; a stalker on horseback stays in the saddle: he follows at a distance, rides in close and back out again round you, pulls up short of you, and rides off when he flees or falls back (if the horse won't go within five seconds, he gets down and does it on foot as before). Hiding and his eerie acts stay on foot");
            b.AppendLine("StalkerHorse = " + StalkerHorse + "   ; percent of stalkers who appear on horseback: he sits the horse watching from where he appeared and gets down the first time he moves (the horse is given back out of view); 0: always on foot");
            b.AppendLine("RevenantsAnger = " + B(RevenantsAnger) + "   ; someone you killed comes back by their temper: the angrier they were, the likelier they walk again and come at you; the mild mostly stay in the ground");
            b.AppendLine("RevenantsHunt = " + B(RevenantsHunt) + "   ; someone you killed who has come back may try to kill you, by what they hold against you; false: they follow and stare, and never lay a hand on you");
            b.AppendLine("RevenantHuntChance = " + RevenantHuntChance + "   ; percent at each chance once they are within 25 m, more for a grievance from before, for having begged, for having come back before (0 to 100)");
            b.AppendLine("RevenantsMinds = " + B(RevenantsMinds) + "   ; someone you killed who has come back chooses what to do next: the AI model chooses from how they died and what they held against you, if it is on; the rules choose if not; false: they go about it the old way");
            b.AppendLine("RevenantsAtCamp = " + B(RevenantsAtCamp) + "   ; with WestWorld and Stalkers: sit at a fire of a night out of the towns and someone you killed who is due back (or due within two days) comes to the edge of the firelight, 13-18 m off, and stares; a word from the dark; the stalker's ways after; false: they come behind you on the road as before");
            b.AppendLine("StalkerMinutes = " + StalkerMinutes + "   ; how long a stalker keeps it up (real minutes, up to 240)");
            b.AppendLine("StalkerAppearMin = " + StalkerAppearMin + "   ; he first turns up this many metres behind you, at least...");
            b.AppendLine("StalkerAppearMax = " + StalkerAppearMax + "   ; ... and at most");
            b.AppendLine("StalkerFollowMetres = " + StalkerFollowMetres + "   ; how close he follows");
            b.AppendLine("StalkerStareMetres = " + StalkerStareMetres + "   ; within this distance he stops and stares (more than StalkerFollowMetres)");
            b.AppendLine("StalkerStillSeconds = " + StalkerStillSeconds + "   ; stand still this long and he does what StalkerWhenStill says");
            b.AppendLine("StalkerWhenStill = " + StalkerWhenStill + "   ; circle: runs in close and circles you; watch: keeps his distance and stares; rush: runs straight at you and stops a few steps short; eerie: sprints in to between StalkerActMin and StalkerActMax and does odd things (digs a grave, kneels, waves, beckons, freezes mid-gesture); mix: a different one each time (he never attacks)");
            b.AppendLine("StalkerCircleMetres = " + StalkerCircleMetres + "   ; how close he circles (3-15)");
            b.AppendLine("StalkerActMin = " + StalkerActMin + "   ; he sprints in and does his odd things at least this many metres from you...");
            b.AppendLine("StalkerActMax = " + StalkerActMax + "   ; ... and at most (a new distance each time he comes in; 2-40)");
            b.AppendLine("StalkerHideMin = " + StalkerHideMin + "   ; when you move, he runs back out at least this far...");
            b.AppendLine("StalkerHideMax = " + StalkerHideMax + "   ; ... and at most, behind a tree or a building if there is one");
            b.AppendLine("StalkerMutters = " + B(StalkerMutters) + "   ; he mutters now and then");
            b.AppendLine();
            b.AppendLine("[Quiet]");
            b.AppendLine("QuietWhenStill = " + B(QuietWhenStill) + "   ; while you stand still, enRAGE's tension stops building and no nemesis, gang or faction comes for you (through enRAGE's own calm period)");
            b.AppendLine("QuietStillSeconds = " + QuietStillSeconds + "   ; how long you must stand still first (1-60)");
            b.AppendLine("QuietInCinematic = " + B(QuietInCinematic) + "   ; the same while the cinematic camera is on, moving or not");
            b.AppendLine();
            b.AppendLine("[Encounters]");
            b.AppendLine("Encounters = " + B(Encounters) + "   ; master switch, on as it comes: the encounters on the roads, in the wilds and in town (the crying woman, the night visitor, travellers, campfires, the supper invitation, executions, callouts, a husband's temper, the hogtied, hermits, gatherings). false turns off everything in this section");
            b.AppendLine("EncounterEveryMinutes = " + EncounterEveryMinutes + "   ; real minutes of play between rolls for an encounter, when the time and place are right (2-120)");
            b.AppendLine("EncountersAtOnce = " + EncountersAtOnce + "   ; how many encounters (and a stalker) may be going on at once, one of each kind (1-8); each roll starts one at most, so they come one by one");
            b.AppendLine("CryingWoman = " + B(CryingWoman) + "   ; at night in the wilds, a woman sobbing in the dark. Go close and she may draw a knife, or not be alone");
            b.AppendLine("CryingWomanChance = " + CryingWomanChance + "   ; percent on each roll, between ten at night and four in the morning");
            b.AppendLine("NightVisitor = " + B(NightVisitor) + "   ; after midnight in a town, a pale figure crouched over a body: it rises, stares, and is gone when you look away; next morning the town speaks of it");
            b.AppendLine("NightVisitorChance = " + NightVisitorChance + "   ; percent on each roll, midnight to four, once a night at most");
            b.AppendLine("Travellers = " + B(Travellers) + "   ; by day on the roads, a man on foot waves you down and asks the way to the nearest town; greet him, or stop a moment, and he finds his way");
            b.AppendLine("TravellerChance = " + TravellerChance + "   ; percent on each roll, seven in the morning to seven at night, once a day at most");
            b.AppendLine("WagonWheel = " + B(WagonWheel) + "   ; by day on the roads (06:00-20:00), once a day: a wagon ahead of you, out of view, loses a wheel and stops, the driver down beside it; within 25 m he calls to you. On foot within 3 m of the wagon for three seconds you set the wheel with him (his thanks, his liking for you up, and he drives on); ride past and once you're 40 m gone he calls after you to send word: reach any town within thirty game minutes and it's sent. Nothing is spawned: the wagon is the game's own, handed back");
            b.AppendLine("WagonOthersStop = " + B(WagonOthersStop) + "   ; with WagonWheel on: twenty-five seconds after the driver is down beside his wagon with no help from you, or once you ride on, six wagons in ten get a passer-by who turns aside, offers a hand and sets the wheel with him (he says so if you rode past)");
            b.AppendLine("SecondActs = " + B(SecondActs) + "   ; the people of the encounters come back: someone you saved (at an execution, cut loose, from a beating, warned of a poster, on the road) comes over in their town half a day to three weeks on to thank you for that very thing, with a token of their trade (with [Scenes] FriendCalls); an execution you found and never stepped into: one of the dead man's menfolk comes to say it to your face five to nine days on (with [Scenes] KinGrudges); the men who got away after you saved their man are heard of again, and the region's next outlaws are the same men, who know you; false: as before");
            b.AppendLine("Elopers = " + B(Elopers) + "   ; in a town by day (nine till nine), on foot and out of doors, once a day at most: a young woman and a young man come running to you, her family a minute behind; J: hide them, send them on, or give them up; then her father and brother ask which way (J: the wrong way, or the right one); nobody is hurt, and the town has it next day; false: never");
            b.AppendLine("AlleyVoice = " + B(AlleyVoice) + "   ; in Saint Denis after dark (nine till four), on foot and out of doors, once a night at most: a man you can't see calls for help; go to him and he's real (beaten and robbed: J, help him up or leave him) or bait (two men step out behind you with their revolvers: J, pay five to twenty dollars or refuse, and it's their fight with you); ignore him and the voice stops; false: never");
            b.AppendLine("NoMapRider = " + B(NoMapRider) + "   ; with Riders: on a road at night (nine till four), on the traveller's turn (he walks by day), one time in ten and never within two weeks of the last, a stranger rides up and asks the way to a town nobody has heard of, thanks you whatever you say, and goes on into the dark; a day or two later the nearest town hears of a man found dead on that road, and those near you think of it; false: never");
            b.AppendLine("Riders = " + B(Riders) + "   ; on the road, half the traveller's turns are a rider instead (the road's pace is the same): someone of the nearest town comes up beside you (on a horse when you're in the saddle), rides along a while talking by nature and worry, and turns off (J: ride on together, and he knows you in town; or good day); while a posse is out of that town, a deputy may ride up asking after the man (J: the right way, the wrong way, or nothing, and the posse's end follows your word); false: travellers only");
            b.AppendLine("WagonWheelChance = " + WagonWheelChance + "   ; percent on each encounter roll (0 to 100)");
            b.AppendLine("Pinkertons = " + B(Pinkertons) + "   ; by day in a town where a violent deed of yours is known from the past week, now and then (at most once a town in five days) a man from the Pinkerton agency walks in and asks two or three of its people after you: your friends lie for you, those afraid of you tell him, the rest shrug; a friend comes over later to warn you and says who talked");
            b.AppendLine("Campfires = " + B(Campfires) + "   ; at night in the wilds, two or three men round a fire off the road call you over for coffee and a tale; now and then a traveller walks in from the dark");
            b.AppendLine("CampfireChance = " + CampfireChance + "   ; percent on each roll, eight in the evening to four, with ten game hours between fires");
            b.AppendLine("VoiceBelow = " + B(VoiceBelow) + "   ; West World: late at night in a saloon, a muffled plea from under the boards behind the bar; the barkeep says it's the pipes, and his thoughts say otherwise; once a night a town");
            b.AppendLine("SupperInvitation = " + B(SupperInvitation) + "   ; at dusk on the roads, a too-friendly man asks you to supper at his place. Follow him there on foot and you wake in a ditch at first light with most of your cash gone ($250 at most). Ride on and he lets you go. On as it comes (violence against Arthur is on by default); false to turn it off");
            b.AppendLine("SupperChance = " + SupperChance + "   ; percent on each roll, half past five to nine in the evening, once a day at most");
            b.AppendLine("Executions = " + B(Executions) + "   ; out on the roads, two men holding one of a nearby town's people on his knees at gunpoint, for a reason from his own life. Ride on and they go through with it");
            b.AppendLine("ExecutionChance = " + ExecutionChance + "   ; percent on each roll, once a day at most, outside the towns");
            b.AppendLine("PublicCallouts = " + B(PublicCallouts) + "   ; by day in a town (not the noon hour), one of the people about calls another out in front of the street over something real between them (a debt, a theft, a beating, hard words). Each answers by their own mind (or the model, with LanguagePicks): let it go, own up, give it back, square up; those near watch, jeer, step in or walk off. Walk up and it ends. No guns; two who square up go to the street's quarrel");
            b.AppendLine("PublicCalloutChance = " + PublicCalloutChance + "   ; percent on each encounter roll while one may happen (six real minutes at least between them)");
            b.AppendLine("DomesticDisputes = " + B(DomesticDisputes) + "   ; out in a town's street, 08:00 to 21:00, a husband turns on his wife over what's eating him (money, drink, another man). He rants, storms off, lets it drop when he sees he's watched, or now and then strikes her (rarely, once, never worse); she gives it back, pleads, sobs, shrinks back or walks off; a neighbour may step in or fetch the law. Walk up and it ends; once he has struck her, dealing with him is no crime");
            b.AppendLine("DomesticChance = " + DomesticChance + "   ; percent on each encounter roll while one may happen (eight real minutes at least between them)");
            b.AppendLine("Hogtied = " + B(Hogtied) + "   ; away from the towns by day, one of a nearby town's people lies tied up off the road, two men by them: bounty hunters if they're a known thief, a debt's collectors, or outlaws. The captive calls, bargains or curses by their nature; the men warn you off, name a price (greet the leader to pay), pack up or (with HogtiedMenDraw) draw. Cut them loose with the game's own prompt, pay, fight or ride on. Bounty hunters are a lawful claim: shooting them first is a crime");
            b.AppendLine("HogtiedChance = " + HogtiedChance + "   ; percent on each roll, six in the morning to eight at night, once a day at most");
            b.AppendLine("HogtiedMenDraw = " + B(HogtiedMenDraw) + "   ; on as it comes: those men may draw on you unprovoked as you come near; off: those men fight only once you step in (fire on them, rush them, hold a gun on them, or cut the captive loose unpaid); on: their leader may also draw on you unprovoked as you come near (it turns on you)");
            b.AppendLine("HogtiedTurnsOnYou = " + HogtiedTurnsOnYou + "   ; percent of the hogtied you cut loose who pull a knife on you and want your money instead of thanking you (0 to 100; 25 as it comes; 0 never); fighting them off is self-defence");
            b.AppendLine("CalloutsArthur = " + B(CalloutsArthur) + "   ; by day in a town, out of doors, one of the people near with something against you (you struck or robbed them, they saw you kill, word of your thieving reached them, or you soured them) walks up and says it to your face. Walk off past 12 m and they call you a coward; stay and most put up their fists (fighting them is self-defence); hold a gun on them and they back off. Once a day each");
            b.AppendLine("CalloutsArthurChance = " + CalloutsArthurChance + "   ; percent on each encounter roll while one may happen (five real minutes at least between them)");
            b.AppendLine("CalloutsArthurDraw = " + CalloutsArthurDraw + "   ; percent: an armed hothead calling you out over blood (a beating, a killing, an old grudge) draws on you instead (0 never)");
            b.AppendLine("SceneClips = " + B(SceneClips) + "   ; the scenes' own clips, seen to play in game: a captor waves you back, the man on his knees shakes, the saved one scrambles off; the chain gang, the injured rider, the burial's preacher and digger");
            b.AppendLine("WaysideComposed = " + B(WaysideComposed) + "   ; with ComposedActs: someone standing in a road scene shows what they feel, between what's said and done, in moves the game has been seen to play");
            b.AppendLine("WaysideAnswers = " + B(WaysideAnswers) + "   ; when you walk up on a road scene, one of them decides what to do about you, from a short list that fits the scene. The AI model's word if it's on, the rules' if not. The armed may turn on you");
            b.AppendLine("Wayside = " + B(Wayside) + "   ; things to come across out of the towns: men fighting by the road, and more: their own people, staged out of your sight, and you can step in or ride on");
            b.AppendLine("WaysideChance = " + WaysideChance + "   ; percent a roll, among the other encounters (0 to 100)");
            b.AppendLine("AlliesHelp = " + B(AlliesHelp) + "   ; attacked by anyone, the law too, within sight of people who think well of you, some of them come to your aid: someone who owes you (the bar at AlliesLiking or more, someone you saved) whatever the odds, and of the rest the brave and the hot first, cowards never, a woman only against fists, and against a gun only those who carry one (they're given a revolver for it and keep it after). They shout as they come, and have a word with you after; they remember it, and think better of you for it");
            b.AppendLine("KinThanks = " + B(KinThanks) + "   ; with KinGrudges on: one time in two, a kinsman of someone you saved (a bully pulled off them, their beater put down, the rope, the hogtied cut loose) comes to find you five to nine days on, as kin come over a wrong, and says the family is grateful; false: only wrongs bring kin");
            b.AppendLine("GratitudeFelt = " + B(GratitudeFelt) + "   ; with MindShadow on: standing with a man in a fight, paying his fine or standing surety for him is felt in his mood as the kindness it is (and by those who saw it), as a drink stood or a coin given already is; false: only the ledger has it");
            b.AppendLine("SuretyKeptOwes = " + B(SuretyKeptOwes) + "   ; a man you stood surety for at the jail who keeps the peace his week out owes you from then on, for a month of game days, as someone you saved does, and comes over later to say so; false: his kept week only warms him to you");
            b.AppendLine("FineOwes = " + B(FineOwes) + "   ; a man whose fine you paid at the jail door owes you for a month of game days, as someone you saved does: he comes to your aid, remembers it in his thoughts, and comes over later to thank you; standing surety for him is a kindness the town sees, but no debt (he may yet break his word); false: paying a fine changes nothing between you");
            b.AppendLine("StoodRemembered = " + B(StoodRemembered) + "   ; the man you stood with when he was losing a fight keeps it in his own mind, as someone you saved does: his thoughts come back to it (\"stepped in for me\"), and some days on he comes over to say how much it meant; false: only the ledger has it");
            b.AppendLine("StoodByOwes = " + B(StoodByOwes) + "   ; a man you stood with when he was losing a fight (J, \"Stand with him\") owes you for a month of game days, as someone you saved does: he comes to your aid whatever the odds, and calls on you as a friend; and those who saw it count it a kindness, as the town's regard does; false: only a rescue counts");
            b.AppendLine("AlliesSpareLaw = " + B(AlliesSpareLaw) + "   ; in a fight that has had anyone but the law against you (bandits, say), friends leave the law alone, until nobody has fought you for six seconds; when the law alone comes for you they still stand with you; false: the law is a foe whenever it fights you");
            b.AppendLine("AlliesMetres = " + AlliesMetres + "   ; from within this many metres of you (10 to 80)");
            b.AppendLine("AlliesMost = " + AlliesMost + "   ; at most this many with you at once (1 to 6)");
            b.AppendLine("AlliesLiking = " + AlliesLiking + "   ; enRAGE's fondness for you (0 to 100: the bar under people with FeelingBars) from which someone counts as a friend of yours (90: three greetings, or saving them); a kindness of yours they remember, a trouble of theirs you mended, talks and your name known count on their own; 0: enRAGE's fondness doesn't count");
            b.AppendLine("AlliesRoadChance = " + AlliesRoadChance + "   ; percent: out of the towns, with nobody you know in sight and two or more armed against you, a friend from a town's ledger happens by and runs in (once in five minutes at most; 0 never)");
            b.AppendLine("AlliesDot = " + B(AlliesDot) + "   ; a small green dot on the map on each friend near you (the bar at AlliesLiking or more, someone you saved, a kindness remembered, a trouble mended; within 80 m) and on each ally while they're with you (the game's own small companion dot, recoloured); someone who already has a dot keeps their own");
            b.AppendLine("AlliesDotColour = " + AlliesDotColour + "   ; the dot's colour: one of the game's blip modifiers by name (BLIP_MODIFIER_DEBUG_GREEN as built; BLIP_MODIFIER_FRIENDLY the companions' blue); empty: the companion dot's own colour");
            b.AppendLine("AlliesDotScale = " + AlliesDotScale.ToString(CultureInfo.InvariantCulture) + "   ; the dot's size (0.2 to 2; 1 is the game's own size, which drew large); a change shows on the next dot made (Insert reloads them all)");
            b.AppendLine("GreetLiking = " + GreetLiking + "   ; enRAGE's fondness for you (0 to 100: the bar under people with FeelingBars, what AlliesLiking reads) grows by this much on every greeting of someone (34: three hellos fill the bar and make a friend who'll stand with you, AlliesLiking 90); nothing from someone afraid of you or angry with you; 0 off (0 to 50)");
            b.AppendLine("FondnessActs = " + B(FondnessActs) + "   ; more ways to win people over through Talk (Q, locked on): whiskey for the house ($2, in a saloon with two or more others at the bar: the room cheers and everyone treated is fonder of you, the drunk most), a gift (forty cents: a pouch of tobacco for a man, a tin of peaches for a woman; the proud take a gift where they'd refuse money, the poor are fondest of one), and condolences to someone grieving a friend (thrown back by anyone who knows you killed them)");
            b.AppendLine("TalkVerbs = " + B(TalkVerbs) + "   ; three more verbs in Talk: \"What do you know of <X>?\" for someone they've had dealings with (answered by how they stand with X, with a warning when X is known for a thief, a brawler or a killer; the afraid and the hostile carry no tales), \"Could you use a loan? ($10)\" (taken by most, owed three game days, then \"About that ten dollars...\": paid back by those who can, put off by those who can't, denied now and then by the greedy), and \"Have you met <X>?\" for someone standing near they don't know (a word each, a hat tipped, acquainted from then on)");
            b.AppendLine("TalkWorries = " + B(TalkWorries) + "   ; once they've told you their trouble, Talk offers its mend (a dollar for the telegraph, the water cart, the fare to the mine, a word put in, a word with the neighbour, the bank note, coffee instead of whiskey, the marker, a secret kept, the stage out of town; the debt, the doctor, the dentist and the fare as before) and its dark mirror, a word that turns it the wrong way (the creditor told where to find them, the fever called a chill, the letter given up on...); the frightened believe the worst at once, the proud and the cold half the time, and nobody forgets who made it worse");
            b.AppendLine("KindnessSeen = " + B(KindnessSeen) + "   ; the named people who see you do a kindness to someone else from within 12 m (a drink stood, money or a gift given, a debt paid, a hand up, a bully faced, a mercy shown) think better of you too, and remember seeing you do right");
            b.AppendLine("KindnessSeenLiking = " + KindnessSeenLiking + "   ; enRAGE's fondness for you (the bar under people) that each of them gains for it (0 to 30; 0: only our own people's affection moves)");
            b.AppendLine("AlliesRide = " + B(AlliesRide) + "   ; the friend from a town's ledger who happens by on the road comes on horseback, 45-70 m off, and is down off the horse and in on foot near you (if the horse won't take the order within 8 s they run in as before); off: on foot, 30-45 m off");
            b.AppendLine("AlliesGrieved = " + B(AlliesGrieved) + "   ; an ally who dies standing with you is grieved by those who knew them: their friends lose AlliesGrievedLiking of their fondness for you (a third of it for someone who merely knew them), remember whose quarrel it was, and the town has it by evening");
            b.AppendLine("AlliesGrievedLiking = " + AlliesGrievedLiking + "   ; enRAGE's fondness a friend of the fallen loses (0 to 50)");
            b.AppendLine("Hermits = " + B(Hermits) + "   ; West World: people who woke and walked out of their towns live rough in the wilds, and keep their camps");
            b.AppendLine("HermitChance = " + HermitChance + "   ; percent on each roll that a new hermit's camp turns up near you in the wilds");
            b.AppendLine("CircleAnswers = " + B(CircleAnswers) + "   ; walk into the middle of a ring of the awake and it answers you as one: they close in, one steps out and speaks, they all kneel, or they wake the one lying among them; the AI model chooses if it is on, the rules if not");
            b.AppendLine("Gatherings = " + B(Gatherings) + "   ; West World: the awake stand in a silent circle, facing in: from midnight to half past three out in the wilds (once a night), and with GatheringHours any by day too, in an open field or just outside a town's edge");
            b.AppendLine("GatheringChance = " + GatheringChance + "   ; percent on each encounter roll while one may happen");
            b.AppendLine("GatheringHours = " + GatheringHours + "   ; any: at any hour (the night ones as they were, and by day in the open or at a town's edge); night: only from midnight to half past three in the wilds, as before");
            b.AppendLine("GatheringGapHours = " + GatheringGapHours + "   ; game hours at least between one gathering and the next, so they don't come one on another (1-72; a game hour is about two real minutes)");
            b.AppendLine("GatheringHostile = " + GatheringHostile + "   ; percent of the gatherings of the awake that turn on you, bare-handed, all at once, when you come within 10 m of them; fighting them off is self-defence (0 to 100; 50 as it comes; 0: they never attack)");
            b.AppendLine("EventDots = " + B(EventDots) + "   ; the game's own pulsing event dot on the map where one of RealNPCs' happenings is going on near you (a brawl, a gathering, a callout, a husband's temper, the hogtied, an execution), as the game marks its own; false: none");
            b.AppendLine();
            b.AppendLine("[World]");
            b.AppendLine("Bravery = " + Bravery + "   ; how brave the townsfolk are, 0-100: how readily onlookers join a fistfight (85 as it comes: brawls grow more readily than before; 50 is how they grew before; higher, more step in; lower, fewer; 0, only a fighter's friend or foe), and the mod's own frights (an onlooker hurrying off, a fright that spreads, a man a gun is drawn on) mostly stand back and watch instead of running. It never changes how the game itself sends people running or cowering");
            b.AppendLine("Storyteller = " + Storyteller + "   ; how lively the world is around you: the storyteller notices how long it has been since something worth stopping for happened near you (a fight, a theft, a standoff, a song, someone coming up to you, an encounter on the road) and, when it has been quiet too long, shortens the waits before the next scene, chance meeting, walk-up and encounter roll; after something big it waits a little longer for half a minute or so. It never starts anything itself and never stops anything. calm (a quieter world than before), lively, wild; off: as before. In a mission, wanted, in a fight or indoors it holds everything as built");
            b.AppendLine("HistoricalAccuracy = " + B(HistoricalAccuracy) + "   ; the real history in people's heads and mouths: what was in the papers of 1899 and 1907 (the election, the Philippines, the reservations, the trusts, the new machines; in 1907 Roosevelt, the Panic, Oklahoma), the lines about the year they live in, the real calendar (Easter on 2 April 1899, the Fourth, Election Day, Thanksgiving, Christmas, and in Lemoyne Mardi Gras and All Saints', each with its eve and its morning after, and a word of the day on the ticker), the year's work by region (calving and roundup, cane and cotton, the trapping lines), the snake-oil man's Food and Drugs Act in 1907 and the Panic in the paper. false: none of it, and the timeless texture of the days (bath night, Sunday collars, wash day) stays as it was");
            b.AppendLine("AlmanacSides = " + B(AlmanacSides) + "   ; with Almanac: on the stories the country argued over in 1899, people think their own side of it, by their trade (and for the vote and the drink, being a woman); false: the same lines for everyone");
            b.AppendLine("CenturyTurns = " + B(CenturyTurns) + "   ; with HistoricalAccuracy: on New Year's Eve of 1899 and of 1900, from five in the afternoon, two who stop together may argue whether the twentieth century begins at midnight or a year on (the almanacs said 1901; the crowds held it on 1 January 1900), and the turn of the year is on the ticker at midnight. No gunfire is played");
            b.AppendLine("Almanac = " + B(Almanac) + "   ; with HistoricalAccuracy: the almanac, a few hundred dated things of 1899 (and of 1907 in the epilogue) known from the day they were in the papers (the war at Manila, the great cold of February, the train robbed at Wilcox, Jeffries, Dewey's homecoming, the Boers, Hobart's death, Lawton, Moody; in 1907 the Thaw trial, the midnight forests, Haywood, the Panic, Oklahoma, Monongah, the fleet) and the year's everyday talk (the catalogue, the wheel, the phonograph, the new rag, the medicines, the trusts, where the old names of the West got to): in people's thoughts, in Talk's What's the news and the typed question, in the talks between two people, read aloud from the paper on the street, the week's paper in the reader, and a word on the ticker the day a big thing breaks");
            b.AppendLine("AlmanacShare = " + AlmanacShare + "   ; percent: how much of the almanac gets through: the pull of its thoughts and its share of the news answers and the talks (0 none, 100 as written, 200 twice)");
            b.AppendLine("Deeds = " + B(Deeds) + "   ; people react to where you go and what you do: shops, church, hunting, killing, stealing");
            b.AppendLine("SelfDefenceNoBounty = " + B(SelfDefenceNoBounty) + "   ; killing or hurting someone who came at you (soldiers too) doesn't raise your bounty or make you wanted: RealNPCs puts it back, unless you also hurt someone who didn't, or did another crime");
            b.AppendLine("TownPosts = " + B(TownPosts) + "   ; the shopkeepers, barkeeps, gunsmiths, doctors and station agents are each one person for good, who knows you; when one dies, a relative or a newcomer takes the counter");
            b.AppendLine("TownPostsAfterDeath = " + TownPostsAfterDeath + "   ; survive: the game brings a shot-down counter keeper back bandaged, so they lived and remember it; successor: they died, and a relative or a newcomer takes the counter");
            b.AppendLine("Regulars = " + B(Regulars) + "   ; each town keeps its own ordinary people: the same names, faces and lives every time you're there, each going through something of their own");
            b.AppendLine("RegularsPerTown = " + RegularsPerTown + "   ; how many regulars a town keeps (5-80)");
            b.AppendLine("RegularsDress = " + B(RegularsDress) + "   ; regulars wear their own clothes each time you meet them (put on while nobody can see); off: they keep their names and lives, not their looks");
b.AppendLine("Returners = " + B(Returners) + "   ; (off as it comes, in testing) someone who matters to RealNPCs (woken, a worry running, someone you've met) comes back unseen, 20 to 150 metres from you, in a body of their own build, dressed in an outfit of their own, the same each time; their first body keeps the game's clothes. With it on, the woken no longer turn up in a stranger's body in plain sight. enRAGE may take a dressed body for one of its own people: watch for a name over a returner that isn't theirs");
            b.AppendLine("ReturnersChance = " + ReturnersChance + "   ; with Returners: percent of the unseen bodies the town regulars' scan looks at that a returner is tried on first (0-100)");
            b.AppendLine("ReturnersPerTown = " + ReturnersPerTown + "   ; with Returners: at most this many returners with an outfit of their own in one town (1-40)");
            b.AppendLine("LooksReadDepth = " + LooksReadDepth + "   ; the Testing row \"Read the looks of the people near you\" only (nothing else reads a body's clothes): 1 counts the pieces, 2 reads each piece, 3 adds its tints, 4 adds its kind");
                        b.AppendLine("RegularsRoutines = " + B(RegularsRoutines) + "   ; the town keeps time: who a passer-by becomes follows the hour and the spot (the drinkers at the saloon of an evening, the churchgoers in church on Sunday morning, the early risers at dawn, only the night owls out late); nobody walks across town for it, and a regular caught out of their hour thinks so");
            b.AppendLine("RumoursTravel = " + B(RumoursTravel) + "   ; word of your deeds travels town to town and grows in the telling");
            b.AppendLine("Newspaper = " + B(Newspaper) + "   ; your deeds and the town talk turn up in enRAGE's newspapers");
            b.AppendLine("Letters = " + B(Letters) + "   ; letters wait for you at post offices (read them with ReaderKey)");
            b.AppendLine("LetterReplies = " + B(LetterReplies) + "   ; answer a letter from the reader (R on it): kindly, coldly, with a threat, with money ($5 or $20), or asking to meet; the answer lands one to three game days on and changes what the writer does next (a grieving relative riding west may stand down or ride sooner, a sweetheart warms or cools, a frightened host feels believed or mocked), most write back, and some wait at their town's station on a set day from ten till four");
            b.AppendLine("WriteLetters = " + B(WriteLetters) + "   ; write first, from the reader in a post town (L): to anyone you've met, a warning, an apology, a love note, a threat or money ($5); carried and taken the same way (a love note or a threat may be shown around the town)");
            b.AppendLine("TownEvents = " + B(TownEvents) + "   ; town events on one framework: things a town does on a day, posted ahead (a notice, the paper), that happen whether you're there or not; away, they resolve among the town's regulars; there, they're staged, and shots break them up");
            b.AppendLine("EventDances = " + B(EventDances) + "   ; the Saturday dance: every second Saturday in a town with a saloon you've been to, seven till eleven, posted on Thursday; outside the saloon when you're there (a fiddler, the dancing, the caller's shouts), two to four regulars warmer toward each other after, the odd quarrel between sweethearts; an hour there and the regulars think the better of you");
            b.AppendLine("EventElections = " + B(EventElections) + "   ; election day (the first Tuesday after the first Monday of November) in every town with five living regulars: two of its men stand for mayor or town marshal, posted a week ahead (a notice, the paper, a poster among your papers); the voters lean by temper and ties and by your word (Talk on a candidate: your word for him, whiskey for his voters, or a word to the other to step aside); the result in the paper, and the town takes sides after");
            b.AppendLine("EventTrials = " + B(EventTrials) + "   ; the circuit judge and gallows day: a resident of ours who kills a man in a fight is held at the jail; the judge sits three to seven days on, ten till two, outside the saloon (the verdict weighed from who saw it, the dead man's friends against his, his temper in the dock, your word if you give it, and a frightened town); guilty means a hanging two days on at the jail, eleven o'clock, the drop never shown; not guilty splits the town. J (keyboard) to testify when the judge asks, to be sworn in as a special deputy before the hour, or to break him out of the jail at night with no lawman near; a hogtied man of ours carried into a sheriff's office is brought in");
            b.AppendLine("EventShows = " + B(EventShows) + "   ; the Wild West show: once your most-told deed (a killing or a robbery) is talked of in four towns, one of them three hops from where it happened, a touring show books a large town you have been to (Saint Denis, Valentine, Rhodes; Blackwater in 1907), one a month at most: posters a week ahead (a notice, the paper, a poster among your papers), the act billed under the name the rumour gave you; away, the paper reviews it and the town's version of the legend bends further and spreads; there (two till four, within 150 m of the town's heart), the ground outside town, a barker, riders in the ring, a crowd; J (keyboard) to heckle, stand up as the real one (a deed), or rob the takings (a robbery)");
            b.AppendLine("NewFaces = " + B(NewFaces) + "   ; with TrainTime: a stranger off the train may come as a settler, a schoolteacher, a new preacher or a mail-order bride come to wed a bachelor here; they think as newcomers for two weeks");
            b.AppendLine("TrainTime = " + B(TrainTime) + "   ; a train standing at a town's platform (a platform spot from the spot key, or the station you came out of): up to four on the platform turn to look, the station's keeper calls it in his own voice, one person steps down (kin of someone in the town one time in two, else a newcomer from a far city) and becomes a resident, letters waiting for the town come off it, and a resident whose worry just ended in leaving (the ticket bought; the house gone to the bank, half; a son gone, a third) says goodbye to a friend, walks to the train and is gone for good, seen or not. Never the train or its people; nothing spawned");
            b.AppendLine("ReturningNeighbours = " + B(ReturningNeighbours) + "   ; with TrainTime: someone who left the town on the train a week ago or more may step off a later train (one in three), back among the town's people, with a notice, and a friend near and in view does a double take; false: those who leave are gone for good");
            b.AppendLine("EpilogueCarries = " + B(EpilogueCarries) + "   ; the epilogue remembers: the first time John's save is made with Arthur's beside it, Arthur's people come over eight years older (some married, widowed, moved on or dead), the ties between them with them, and those who met Arthur think of a man called Morgan when they see John; false: John starts with a world of his own");
            b.AppendLine("TownLedger = " + B(TownLedger) + "   ; each town's ledger, read off the records every day: 50 to start; a killing of yours -3, another death -1, a hanging -4, someone leaving on the train -2, someone arriving +2, a good turn of yours (peace, mercy, a rescue, someone spared, a thief caught) +1, the Wild West show +2, a dance you went to +1; the last month sets the mood (fearful: two or more killings of yours; grieving: three or more deaths; prospering: arrivals and good turns three ahead) for its people's thoughts; the regulars a town keeps are RegularsPerTown scaled by the index, never below 10 (or the setting); a monthly mention in the paper for towns you have been to; F7 TEST towns");
            b.AppendLine("ThinStreets = " + B(ThinStreets) + "   ; off as it comes: in a town whose index is under 30 the game's ambient people are thinned each frame (never below 0.6, that town only, never in missions); the log counts the people within 60 m once a minute so you can see what the game makes of it");
            b.AppendLine("FindDiaries = " + B(FindDiaries) + "   ; loot a body and you may find the diary they kept");
            b.AppendLine("Avengers = " + B(Avengers) + "   ; kin you never met may ride in to settle accounts");
            b.AppendLine("AvengerChance = " + AvengerChance + "   ; percent chance a grieving relative comes for you");
            b.AppendLine("AvengersAtOnce = " + AvengersAtOnce + "   ; at most this many avengers riding, arrived or still alive at a time (1-10); the rest grieve at home");
            b.AppendLine("AvengerGapHours = " + AvengerGapHours + "   ; game hours between one avenger's arrival and the next's (0-720)");
            b.AppendLine("CastRelatives = " + B(CastRelatives) + "   ; in their town, folk on the street turn out to be someone's relative");
            b.AppendLine("CastRelativesSpawn = " + B(CastRelativesSpawn) + "   ; when nobody free is on the street (a well-played town: enRAGE has named most walkers by their looks, and the test cast carry names), the relative is made out of sight behind you and let go to the game once they have spoken; false: the walk-up waits for a free passer-by");
            b.AppendLine("OwnLooks = " + B(OwnLooks) + "   ; (in testing) without enRAGE: someone you met is known again by how they look (the same clothes on the same build), so the townsman you spoke to yesterday is the same man today, not a stranger with his face; with enRAGE running, its own knowing by look is used instead; false: a new stranger each time a body is met again");
            b.AppendLine("InteractionMenu = " + B(InteractionMenu) + "   ; Talk (TalkKey, Q; a pad's RB) in the lock-on menu of someone RealNPCs knows: a short list to ask the news, tell them your name, ask after a worry they've told you of, stand a drink in a saloon, give money, pay toward a trouble they've told you of (their debt, the doctor for a fever at home, a proper dentist, the fare they're saving), say you're sorry for a wrong of yours lately, or warn them; they answer in their own voice and by how they feel about you, and the mind remembers it (gratitude, fear, how they greet you after)");
            b.AppendLine("TalkKey = " + TalkKey + "   ; with InteractionMenu: locked on to someone you know, this key opens Talk. Q (the default) is the game's own lock-on \"target info\" control, with its own icon (a pad's RB): to use another key (e.g. T), rebind that control in the game's Settings > Controls and the icon follows. Any other key here (e.g. T) is read by RealNPCs itself while the Talk prompt is up: the prompt then reads \"Talk (T)\" beside the control's own icon, which does nothing for it, and on the keyboard Talk stands aside next to a station or telegraph clerk (T is enRAGE's telegraph key there). None: no keyboard key, a pad's RB only");
            b.AppendLine("LawClearsBodies = " + B(LawClearsBodies) + "   ; a body left lying in a town street for a while: the nearest lawman on foot carries it off the street, sets it down out of the way and goes back to his rounds (never while there's a fight or a pursuit, never the story's own)");
            b.AppendLine("BodyToUndertaker = " + B(BodyToUndertaker) + "   ; with LawClearsBodies: the body goes to the undertaker's door (his counter within 80 m) or the sheriff's door you last came out of in that town, the short spots off the street as the fallback; someone who knew the dead and is within 25 m as it's lifted walks behind the lawman and stands over them where they're set down; false: off the street as before, nobody following");
            b.AppendLine("PersonalSpace = " + B(PersonalSpace) + "   ; stand right up against someone (under a metre, closer in a saloon) for a couple of seconds and they mind it as their nature says: a step back and a word, a look, or, from the hot-tempered, a word to back off; never the law, anyone sitting or at the bar, or anyone busy (needs [Scenes] SightReactions)");
            b.AppendLine("SpaceMetres = " + F(SpaceMetres) + "   ; with PersonalSpace on: standing nearer than this to someone, in metres, is too close (0.5-2)");
            b.AppendLine("SpaceSaloonMetres = " + F(SpaceSaloonMetres) + "   ; the same at a saloon's bar, shoulder to shoulder (0.4-2)");
            b.AppendLine("SpaceSeconds = " + F(SpaceSeconds) + "   ; how long, in seconds, you both stand still that close before they mind it (0.5-10)");
            b.AppendLine("ArthurSpeaks = " + B(ArthurSpeaks) + "   ; Arthur speaks a recorded fighting line of his own when someone comes at him or when he catches the pickpocket himself; never over a line already playing on him, 20 s between");
            b.AppendLine("YieldToDying = " + B(YieldToDying) + "   ; nothing of RealNPCs' on anyone dying: no line, no fall (a shove, a knock-down), no move or clip of ours on someone the game has dying, so a death animations and sounds mod's deaths play as they are; RealNPCs still reads every death (the ledger, the grudges, the kin); false: as before");
            b.AppendLine("Drama = " + B(Drama) + "   ; strangers argue, flirt, brawl, propose and gossip around you");
            b.AppendLine("DramaGapSeconds = " + DramaGapSeconds + "   ; at most one street scene this often (seconds); a chance meeting between two people with no reason of their own waits one and a half times this. Lower for a livelier street");
            b.AppendLine("SaloonGossip = " + B(SaloonGossip) + "   ; in saloons, strangers trade rumours, some about you");
            b.AppendLine("Weddings = " + B(Weddings) + "   ; couples marry at the church - be there and they'll remember");
            b.AppendLine("Funerals = " + B(Funerals) + "   ; mourners at enRAGE's funerals speak of the dead");
            b.AppendLine("JobRumours = " + B(JobRumours) + "   ; loose talk gives away gang jobs you can act on");
            b.AppendLine("PrisonerTransfers = " + B(PrisonerTransfers) + "   ; word of prisoners being moved, and how it went");
            b.AppendLine("Debts = " + B(Debts) + "   ; people collect what they're owed");
            b.AppendLine();
            b.AppendLine("[Scenes]");
            b.AppendLine("PaydayNight = " + B(PaydayNight) + "   ; with Payday on: Saturday from eight into the small hours, the paid hands stay in town and lean to the saloon, the bottle and the cards; a saloon's fights grow faster that night (each man's threshold to join comes nearer); the town says which night it is; false: payday ends at eight, as before");
            b.AppendLine("BrawlAfter = " + B(BrawlAfter) + "   ; the week after a brawl: the winner of a saloon's fight stands the house a round; a big one (six or more) is in the town's news with its numbers, leaves the barkeep's chairs broken (J at the bar pays for them) and the men of opposite sides sore at each other; a man the law tied up is locked up overnight, and from seven to noon the next morning his wife or mother waits at the jail door (J pays his $2 fine); false: none of it");
            b.AppendLine("BrawlVerbs = " + B(BrawlVerbs) + "   ; with MindBrawls on: once a fight near you (20 m), J offers to break it up (each heeds by their nature and what they think of you; one who won't may turn on you), to stand with the man losing (the one beating him turns on you; that side thinks the better of you, he the worse), or to take the bet of an onlooker waiting for a taker (a dollar on the other man; a lost bet unpaid is a welsh he holds against you); false: none of it");
            b.AppendLine("PurseSnatch = " + B(PurseSnatch) + "   ; when a thief snatches and runs, one man near may give chase, by his nature (the bold, the kind, a friend of hers), bring him down and hand back what he took; and when you catch a thief yourself, J asks: make him give it back, hand him to the law, or let him run; false: as before");
            b.AppendLine("Rivals = " + B(Rivals) + "   ; two men courting the same woman (a courting worry, a sweetheart, an affair) meet and have it out: words, blows their friends may join, or one walks away; then she chooses by her nature and how it went (the winner, the one who walked away, or neither), her ties change, and the town hears of it; false: never");
            b.AppendLine("StoreHoldups = " + B(StoreHoldups) + "   ; by day (9 till 6) in a town, once a town a day at most: a man who leans to it (known for theft, honest with nobody and short of money, or at the worst of a money worry) shows his gun to a customer on their own outside the store near you; they plead, hand it over, or bolt; he runs with it (the law's chase and a man near giving chase, as a pickpocket), or empty-handed. J (or a greeting) is your word: talked down by his nature, the desperate least. Never with a lawman within 50 m; false: never");
            b.AppendLine("HoldupChance = " + HoldupChance + "   ; with StoreHoldups: percent, the day's roll once a man who leans to it and a customer on their own are by the store near you (0 to 100; offline, 15 gave a holdup on about 3 town-days in 100 by a store)");
            b.AppendLine("HoldupShots = " + B(HoldupShots) + "   ; with StoreHoldups: one in four who bolt are fired after (someone can die on screen); false: he curses and runs");
            b.AppendLine("RainShelter = " + B(RainShelter) + "   ; the town keeps a record of its rain while you're in it: in the rain the street's errands wait (nobody is walked anywhere), strangers under one roof talk and leave acquainted (they nod in passing after), and two days of rain in a farmer's town end his drought well; false: as before");
            b.AppendLine("Grumble = " + B(Grumble) + "   ; an old hand of the town and a friend of his (within 8 m) talk over a greenhorn off the train where he can hear (his boots, his hands, the winter coming); he answers them dry and wins them a little, bristles and they shrug it off, or goes without a word. With no friend by, the ribbing (Greenhorn) instead");
            b.AppendLine("MinersMeet = " + B(MinersMeet) + "   ; at Annesburg after the shift on a weekday (six to nine), once a game week, when four or more of the town's working men are already standing together near you and two other men stand off watching: the boldest speaks up about the scrip and the weights, one backs him, one doubts, he asks for hands, and it's carried or it isn't; the company's men think it; the Around town box has it. Nobody is walked anywhere: they say it where they stand");
            b.AppendLine("PostReward = " + B(PostReward) + "   ; at a town's jail door (known once you've walked out of the sheriff's office), when someone whose name you know was robbed there in the last three game days: J to put five dollars with the sheriff; within the day a regular brings the thief in (the money is his), or a man claims it falsely (your money back), or nobody does (back after two game days); false: no such J");
            b.AppendLine("SpreadRumour = " + B(SpreadRumour) + "   ; with MemoriesBlur's tales: in a saloon, J beside a man who likes to talk, to put a word in his ear about a townsman whose name you know (he cheats at cards, drinks his wages...); it goes round as the town's tales do and grows, and the man it's said of hears it; false: no such J");
            b.AppendLine("NoteMeet = " + B(NoteMeet) + "   ; after you carried a word between two who fell out (Talk's errand), the next time they're near each other within two game days they meet in the street: taken, a word and a hand; sent back, a cold word and they part; false: the word is all there is");
            b.AppendLine("GraveNight = " + B(GraveNight) + "   ; with Mourning on: the night after a burial, eight till midnight, one of those mourning the dead walks out to the grave (the town's cemetery spot, else by the church) and kneels there; come within a few steps and they speak of the dead; false: only the Sunday visits");
            b.AppendLine("SleepOnIt = " + B(SleepOnIt) + "   ; at each day's turn, a quarrel between two that nothing fed the day before (no scene between them) eases a step, sore to cool and cool to nothing, so a feud left alone fades in a day or two; one fed that day doesn't; false: quarrels keep their heat until a scene changes it");
            b.AppendLine("FeudArcs = " + B(FeudArcs) + "   ; a feud picks up again within hours; where it would come to blows, the one who began it, if his nature allows (the kind and the loyal, never the proud or a hothead before others), offers his hand where the street can see; the other takes it by his own nature (a making-up) or refuses it (blows); false: the blows as before");
            b.AppendLine("Familiarity = " + B(Familiarity) + "   ; two of the town who end up near each other at the same place three times in a game day, an hour or more apart, become acquainted (a tie of no warmth): a nod or a word in passing, and in a brawl one leans to the other a little; false: only scenes make ties");
            b.AppendLine("SessionClock = " + B(SessionClock) + "   ; the arcs you're meant to watch start and end within two game days (about an hour and a half of play): a man you vouched for, the town's tales, the cardsharp's return, the beaten man's plan, kin come about a wrong, the circuit judge, a loan, the claim agent, a sweetheart's patience; what keeps things rare, the calendar and how long people remember keep their lengths; false: every old length");
            b.AppendLine("RevivedSeen = " + B(RevivedSeen) + "   ; with West World (and MindLive, MindWakes): someone dead on RealNPCs' ledger who walks again is known by anyone with a tie to them, not only those who saw them die; each thinks it over their head, and the bold walk up and ask, and get a denial; false: only those who saw the death, and only their act");
            b.AppendLine("CollectionPlate = " + B(CollectionPlate) + "   ; on a Sunday from eleven till one, within 60 m of a town's church, one churchman (never a crowd) brings the collection plate round to you: J a dime, a dollar, or let it pass; who gave is seen, and a stranger's dollar is the next Sunday's sermon; false: no plate");
            b.AppendLine("Brands = " + B(Brands) + "   ; in a ranch town by day a ranch man, stock gone with the brands run over, blames his neighbour to his face in the street (a hostile tie between them); if you came on the rustlers at their branding fire in the last two game days, J while it plays to tell him who it was, and they make it right; false: no such scene");
            b.AppendLine("StepBetween = " + B(StepBetween) + "   ; two men with a quarrel between them about to have it out near you (words, before any blow): J to step between them; each heeds you by his nature and what he thinks of you; both heed and it's put off six game hours; one won't and he shoves past; the second time both heed, they make it up; false: no such J");
            b.AppendLine("TemperanceDoor = " + B(TemperanceDoor) + "   ; with Temperance on: at dusk a churchwoman by the saloon door puts the pledge to a man in drink going in, with a second woman of the church beside her if one is near (never a crowd); a hothead's insult brings her kin if he's near, and blows with sides; a man who signs is the town's talk for a week (and back at the bar by Friday, as often as not) and the preacher's word on Sunday; false: the pledge only in chance meetings, as before");
            b.AppendLine("Snubs = " + B(Snubs) + "   ; a working man asks a gentleman something in the street by day and is brushed off; he takes it, sneers or answers back; the gentleman's pride; with a lady beside him, her word ends it or makes it worse and they walk on together; alone, a hot man and a proud one can come to blows, and the working men near lean to their own; false: never");
            b.AppendLine("OldSoldiers = " + B(OldSoldiers) + "   ; with [World] HistoricalAccuracy: two old men who fought on opposite sides in the war find each other out, mostly in the saloon, and argue it (it ends in a toast to the dead, a cold parting or blows, by their natures; in a brawl over it the old soldiers near lean to their own side); two of the same side remember it together; and a young man home from Cuba or the islands tells his war: an old soldier hears him out, a man against the islands won't; false: never");
            b.AppendLine("Greenhorn = " + B(Greenhorn) + "   ; a regular ribs a greenhorn off the train in his first two weeks (his boots, his hat, his city ways), and he answers by his nature: laughs it off and gives as good (welcomed, a tie), squares up (a fistfight, and someone may step in), or takes it and goes (scorned); men only; false: never");
            b.AppendLine("StreetMore = " + B(StreetMore) + "   ; the street's own business beside its fights, by day: a man with horses to deal sells one at the hitching post (the buyer may come off the worse and know it: a grudge with a cause), a man whose bad tooth is at its worst has it pulled in the street by a steady hand, a stranger asks a resident the way (shown it, walked there, sent off short, or sent wrong for a joke); false: none of them");
            b.AppendLine("MeetWalkAgain = " + B(MeetWalkAgain) + "   ; a street scene's walker who gets no nearer in 3 s is told again, and the one waiting for them in a seat or at their work gets up to meet them, the game's own way; false: the walker is told once, and the scene stops if they never get there");
            b.AppendLine("ShoutsCarry = " + B(ShoutsCarry) + "   ; what's shouted is shouted (anger, threats, fright, a cry for the law, fight talk, cheering, calling out): the game lets a shout carry much farther than talk, so you hear it from down the street; ordinary talk is as before");
            b.AppendLine("RealVoiceTalk = " + B(RealVoiceTalk) + "   ; now and then two people standing together near you have a word in their real recorded voices: the weather, how they're getting on, a piece of news, a hello");
            b.AppendLine("RealVoiceTalkSeconds = " + RealVoiceTalkSeconds + "   ; about how often (seconds, 30 to 1800); each person then rests five minutes, and the pair fifteen");
            b.AppendLine("EnragePeopleJoinIn = " + B(EnragePeopleJoinIn) + "   ; enRAGE's own people (its nemeses, gang men and named folk) take part in street scenes, reactions, talk and waking acts like anyone else, unless enRAGE has them busy (a fight, a job, a ride)");
            b.AppendLine("Reactions = " + B(Reactions) + "   ; street scenes: the one spoken to reacts to the words by their nature (bristles, flinches, shakes their head, nods), and a man whose face goes into the bar reels");
            b.AppendLine("Grudges = " + B(Grudges) + "   ; grudges that travel: a friend of someone wronged has it out with the one who did it; and someone whose friend you killed may come up and say so to your face (never violent)");
            b.AppendLine("KinGrudges = " + B(KinGrudges) + "   ; a wrong of yours on a known person (a threat, a beating, a theft, a trouble turned the wrong way on purpose) brings a man of their family, a brother or a father you never met, to town five to nine days on: he walks up and says it to your face by his nature, and stays a resident of the town; once for each person wronged; never violent; false: never");
            b.AppendLine("ReasonThoughts = " + B(ReasonThoughts) + "   ; when a street scene starts for a reason between two people (a quarrel over money, a friend to comfort, good news to tell), the one whose reason it is thinks why over their head as they go over, so you can follow what led to it");
            b.AppendLine("CourtFolkAges = " + B(CourtFolkAges) + "   ; enRAGE's townsfolk, whose ages it doesn't keep, may court each other (an age guessed from who they are)");
            b.AppendLine("WarnedYou = " + B(WarnedYou) + "   ; (with MindLive) someone who saw what you did may have it out with you in words, and someone who thinks well of you may warn you of a man with a grudge, or the law asking. Words only");
            b.AppendLine("FavoursAsked = " + B(FavoursAsked) + "   ; (with MindLive) someone hard pressed for money (a debt, a tooth, a fare) may come and ask you; stop and Talk to help");
            b.AppendLine("LonelyRegulars = " + B(LonelyRegulars) + "   ; (with MindLive) someone lonely who knows you (your name, or a kindness of yours lately) may come over when they see you in town, on foot and stopped, hoping for a word; stop and Talk, or greet them, and they're glad of it; walk on and it stings them, and they leave you be a few days. Never someone afraid of you, sore at you, wronged by you lately or mourning one you killed");
            b.AppendLine("LonelyNeed = " + F(LonelyNeed) + "   ; with LonelyRegulars on: how lonely (their need for company, 0.3-1) someone who knows you must be to come over hoping for a word");
            b.AppendLine("LonelyLookSeconds = " + LonelyLookSeconds + "   ; how often, in seconds, someone lonely is looked for in town (10-600)");
            b.AppendLine("LonelyMetres = " + F(LonelyMetres) + "   ; how near you they must be, in metres (5-40)");
            b.AppendLine("LonelyWaitSeconds = " + LonelyWaitSeconds + "   ; how long, in seconds, they stand facing you hoping you'll Talk (5-60)");
            b.AppendLine("LonelyRestDays = " + LonelyRestDays + "   ; walked past once, game days before they try again (0-14)");
            b.AppendLine("BeingWatched = " + B(BeingWatched) + "   ; people behave better when they're watched: a theft, a shove, a fight, a standoff or the noon callout from the street's own timing waits while a lawman within WatchedLawMetres (25 m) has the one who'd start it in sight (a hothead or a drunk goes ahead now and then, WatchedRecklessChance; a thief only if bold, BoldThieves), and on a Sunday morning by the church (but for the godless; a thief always waits there); they glance at the law and think why. A chance meeting with the law looking keeps to words");
            b.AppendLine("BoldThieves = " + BoldThieves + "   ; with BeingWatched on: percent of would-be thieves (a pickpocket in the street, a lift on you) who try it anyway with a lawman looking on, one roll a day each; a hothead, a greedy one or a drunk half as likely again (0 never, as before; 100 the law stops no thief)");
            b.AppendLine("WatchedLawMetres = " + F(WatchedLawMetres) + "   ; with BeingWatched on: a lawman this near, in metres, facing them with a clear sight, is watching (5-60)");
            b.AppendLine("WatchedRecklessChance = " + WatchedRecklessChance + "   ; percent of hot-tempered or drunk would-be troublemakers (a shove, a fight, a standoff, a callout) who go ahead anyway, one roll a day each (0-100)");
            b.AppendLine("WatchedChurchMetres = " + F(WatchedChurchMetres) + "   ; how near the church, in metres, counts as by the church on a Sunday morning (10-200)");
            b.AppendLine("AlliesRemember = " + B(AlliesRemember) + "   ; with [Scenes] FriendCalls on: someone who stood with you in a fight comes over within the week, the first time they see you after, to talk it over (and of anyone they knew who fell in it); once a fight");
b.AppendLine("FriendCalls = " + B(FriendCalls) + "   ; a friend of yours in town (the friend dot's rule: a full bar, a kindness remembered, a trouble mended, someone you saved) comes over unasked now and then with a warning (someone here holds a grudge, or the law has been asking), the news, a small gift or a favour of their trade (the doctor looks you over, the gunsmith tops up your rounds, the barkeep stands a drink); about once in six game hours at most (some twelve real minutes), a friend once in two days");
            b.AppendLine("FriendGifts = " + B(FriendGifts) + "   ; with FriendCalls: the gifts and favours hand something over (a few cents, a dozen cartridges for the gun you carry, a meal or a drink into your cores, the doctor's patching); false: friends bring a warning or the news only");
                        b.AppendLine("LateThanks = " + B(LateThanks) + "   ; people you were good to through the Talk menu (a drink, money, a debt paid, the doctor, a dentist or the fare paid for) may seek you out when they next see you, some hours or a few days on, and say what it meant to them (few for a drink, most for money, nearly all for a trouble mended); the proud pay back a debt they called a loan, days later; the cold, the seated and women after dark just nod");
            b.AppendLine("ThanksAfterHours = " + ThanksAfterHours + "   ; with LateThanks on: game hours at least between a kindness and their thanks (0-72)");
            b.AppendLine("ThanksWithinDays = " + ThanksWithinDays + "   ; game days at most (1-14; the world remembers its last 400 happenings, so a drink may be forgotten sooner in a busy town)");
            b.AppendLine("ThanksDrinkChance = " + ThanksDrinkChance + "   ; percent who come to thank you for a drink you stood them (0-100)");
            b.AppendLine("ThanksDollarChance = " + ThanksDollarChance + "   ; ... for a dollar (0-100)");
            b.AppendLine("ThanksFiveChance = " + ThanksFiveChance + "   ; ... for five dollars (0-100)");
            b.AppendLine("ThanksMendedChance = " + ThanksMendedChance + "   ; ... for a trouble you mended with money: a debt, the doctor, the dentist, the fare (0-100)");
            b.AppendLine("ThanksLoanDays = " + ThanksLoanDays + "   ; a proud one who took your money for their debt as a loan pays it back after this many game days (0-14)");
            b.AppendLine("Stories = " + B(Stories) + "   ; a story over days (with Letters): a father in a town you've been to, whose son went up to the Annesburg mine, writes to you (the letter waits at his post office); ride to Annesburg and the son finds you: J, carry his letter home or send him home on the train; either way the father's worry ends well, he thanks you, and the town has it. Ten days and it lapses, and he remembers who never came back; and the mine whistle doesn't wait. One open at a time; false: none");
            b.AppendLine("GameEncounters = " + B(GameEncounters) + "   ; the game's own random encounters near you (a snakebite, a coach robbery, a prison wagon; read by their models' names, never touched): someone of the mind who saw it keeps it in mind and thinks of it, and the nearest town talks of it the next day, with whether you stopped or rode on. A few kinds are left out on purpose (your camp's visitors, the lewd, the game's story people); false: as before");
            b.AppendLine("BestFist = " + B(BestFist) + "   ; three fights won in a town (the street's, the saloon's, the prize fight out back) make a man its best fist: the town's talk says so, and he thinks of it near you once a day; then six times in ten the town's prize fight is his, against a man of a fighting temper from another town, for twice the purse; lose it and the name goes (with [World] EventPrizeFights for the challenges); false: no names");
            b.AppendLine("CardsharpRunOut = " + B(CardsharpRunOut) + "   ; a card cheat shown the door, with men at the table to see to it: six times in ten up to two of them (the man he cheated first) walk him out and down the street out past the last houses, a word at the door and one at the edge, and turn back; bad blood with each, and two days to two weeks on he's back in that town, sore, and thinks of it near you; false: he's shown the door, as before");
            b.AppendLine("RowdyDrunks = " + B(RowdyDrunks) + "   ; out of doors in a town at night: a man in drink (never a coward) with a friend in drink beside him picks on a man going by who isn't drunk or his friend; they sing a line apiece as they come; the friend hauls him home, or now and then it comes to blows (the brawl's own rules from there). A drunk alone calls the man out and falls on his face, as in the saloon (with DrunkChallenges); false: never");
            b.AppendLine("DoctorsDoor = " + B(DoctorsDoor) + "   ; after dark (nine till five) in a town whose doctor is at his post near you, once a night: a man whose child's fever is at its worst (or whose wife's time has come) runs to the doctor and calls out; the doctor answers from his post; as the man turns for home, J lets you walk with him to his door (a voice calls from inside), or let him go. Next morning the town hears how the night went, and whether a stranger walked with him; false: never");
            b.AppendLine("DoctorsPatient = " + B(DoctorsPatient) + "   ; by day in a town with its doctor (Valentine, Rhodes, Saint Denis), someone near you walks to him for what ails them (a child's fever, a tooth, ribs after a fight, a back, a cough...), tells him, hears his word, and walks home thinking it over");
            b.AppendLine("ErrandPace = " + F(ErrandPace) + "   ; how fast people walk on their errands here (to a scene, to the doctor, to a talk, to a camp seat, home again): 1 an ordinary walk, 2 brisk, 3 a run");
            b.AppendLine("LawChases = " + B(LawChases) + "   ; when a pickpocket runs and a lawman is near, the lawman gives chase, brings him down and holds him for arrest");
            b.AppendLine("LawShootsThieves = " + LawShootsThieves + "   ; with LawChases: percent of the times a lawman near draws and shoots the one who picked your pocket instead of running him down (0 to 100; 50 as it comes); your money comes back off him either way");
            b.AppendLine("FriendsWarnThief = " + B(FriendsWarnThief) + "   ; with FriendCalls: a friend who comes over may warn you of a known pickpocket near (one who has robbed you or been caught at it), by name if they know him; forewarned, you're on your guard 20 real minutes and no thief tries you");
            b.AppendLine("FriendsChase = " + B(FriendsChase) + "   ; when your pocket is picked, a friend of yours within 30 m (someone who'd come to your aid in a fight) may run the thief down, the bold and the kind more readily, and bring your money back; he thinks the better of you for the good turn, and the town hears of it");
            b.AppendLine("LiftsWary = " + B(LiftsWary) + "   ; the thief who bumps into you comes about once an hour of play (24 game hours between, which the storyteller may halve; 48 in the same town), and thieves weigh the mark: where the town knows you've killed, fewer try it (one killing known: 60% of them; two or three: 35%; more: 15%), and fewer again where you've run a thief down this week (one: 60% of that; two or more: 35%); false: as before, a town every four game hours");
            b.AppendLine("Lifts = " + B(Lifts) + "   ; \"Pickpocketed\": a man may bump into you anywhere and lift a little of your cash (a fifth, $1 to $25), then run: catch him, or let a lawman, to get it back; Pickpockets = false stops it too");
            b.AppendLine("Vices = " + B(Vices) + "   ; habits and vices: someone in a saloon whose trouble ended badly today, or who has just heard a friend has died, drinks the night away (and a friend may sit with them); drunk for the rest of the night");
            b.AppendLine("Kindnesses = " + B(Kindnesses) + "   ; small kindnesses between people: someone who trips in the street is helped up by a friend or a kind passer-by, or left lying by the cold and their enemies (and you can give them a hand up yourself); someone may step in on a bully, who backs off, goes on, or shoves the one who stepped in and storms off; remembered either way");
            b.AppendLine("SightReactions = " + B(SightReactions) + "   ; people near you notice how you look (bloodied, drunk, filthy, soaked) and take it by their nature: the timid afraid, the hard sizing you up, the kind concerned; and a man walking off bloodied from a street fight gets looked at");
            b.AppendLine("Acknowledge = " + B(Acknowledge) + "   ; two people who know each other nod, tip a hat or wave as they pass (enemies give a hard look, sweethearts a long one), and each thinks something of it; a pair once in 45 game minutes");
            b.AppendLine("StreetReacts = " + B(StreetReacts) + "   ; the street reacts to other people's fights the way it does to yours: blows, two men squaring up, a gun out, a man killed or knocked out are raised as the game's own shocking events for its crowd (looking, backing off, running, calling the law by its own rules); held in a town for 30 game minutes when one brings the law into combat, a gun out of someone else's holster or people fleeing beside you; never on a mission, with the law after you or beside enRAGE's own fighters");
            b.AppendLine("StreetReputation = " + B(StreetReputation) + "   ; people react to who walks among them: an outlaw (enRAGE's gunmen, a resident the law wants) is looked after, stepped around, turned from by the old and the timid, stared down by the proud; the doctor, a preacher, the sheriff and a keeper out of doors get a hat tipped, a nod and a word; one witness mutters it on the ticker; a known killer or peacemaker in a town gets a thought from a witness (never a task)");
            b.AppendLine("TroubleWeight = " + F(TroubleWeight) + "   ; how often people come to blows or quarrel: 1 as built, 2 twice as often, 0.5 half, 0 never by chance (0-5); it scales the brawl's and the argument's share of a chance meeting, hot blood between strangers, drink and temper, and a snap that comes to blows; over 1 it also shortens everyone's rest between scenes (2 min, 15 after blows), by half at most");
            b.AppendLine("ReasonVariety = " + B(ReasonVariety) + "   ; the street's scenes take turns: someone at the worst of a worry says so once while you're about, not to everyone they meet, the same kind of scene three times running is less likely, and gossip, theft, drunks and courting get their turns over a sharp word");
            b.AppendLine("ChanceFirst = " + B(ChanceFirst) + "   ; chance meetings (a song, a tall tale, haggling, a standoff, a card cheat) get their turn when nobody has a strong reason, nearer you; a high TroubleWeight brings more fights, not more arguments; two people sat far apart are not picked for a scene they would never walk to");
            b.AppendLine("HorseThieves = " + B(HorseThieves) + "   ; with MindLive on, in town: a man honest with nobody and short of money (or a known thief) gets up on a horse that isn't his and rides off, a red dot and fair game; its owner shouts after him. At most once a town in two game hours");
            b.AppendLine("FightCrowds = " + B(FightCrowds) + "   ; with MindLive on: while people trade blows, those within [Mind] BrawlCrowdMetres who aren't of a fearful nature walk over, stand round in a ring and cheer it on (up to 6 round two men, 16 round a brawl, in two rings); they go when it's over");
            b.AppendLine("FindTheLetter = " + B(FindTheLetter) + "   ; one you've met who waits on a letter that never comes: it sits at another town's depot. At that town's station agent J offers it to you; carry it to him and J gives it over: he reads it, and his worry ends well; false: the letter never comes");
            b.AppendLine("SavedKnown = " + B(SavedKnown) + "   ; with Deeds on: pulling a bully off a man, putting down the man beating him, cutting the hogtied loose and the like are good deeds the town knows of (those who saw remember it, it's told about, it may be in the paper), as the rescue at an execution already was; false: only the one saved knows");
            b.AppendLine("StepBetweenKnown = " + B(StepBetweenKnown) + "   ; with StepBetween and Deeds on: when you step between two men about to quarrel (J) and both let it be, it's a good deed the town knows of, as a fight you break up is; false: only the two of them know");
            b.AppendLine("WalkHomeMends = " + B(WalkHomeMends) + "   ; with WalkHome on: walking a man in drink home (J at night) while his own trouble is the drink, gone worse or at its worst, turns it: the rest of his worry ends well, as coffee through Talk does; false: the walk is only a kindness");
            b.AppendLine("FriendsKnowYourThief = " + B(FriendsKnowYourThief) + "   ; with FriendsWarnThief and NameCardsRobbed on: when a friend walks over to warn you of a pickpocket near and he's the one who got clean away with your money, the friend says so");
            b.AppendLine("StandReminds = " + B(StandReminds) + "   ; in a fistfight, the J row \"Stand with ...\" says \"(he stood by you)\" when the man losing fought for you this week, or \"(he owes you)\" when he's one you saved; false: just his name");
            b.AppendLine("BreakUpKnown = " + B(BreakUpKnown) + "   ; with Deeds on: a fistfight you end with your shout (J, \"Break it up\") is a good deed the town knows of, as stepping into any scene is: those who saw it remember, it's talked of, it may be in the paper; false: only the fighters know");
            b.AppendLine("WelshersShunned = " + B(WelshersShunned) + "   ; welsh on a bet in a town (J at a fistfight, lost and not paid) and for a week its betting men won't take your money: the one who'd have offered you a bet tells you why, and there's no bet on J; false: they offer as before");
            b.AppendLine("WelshRemembered = " + B(WelshRemembered) + "   ; with Minds on: when you don't pay a bet you lost on a fistfight, the man you owed and those who saw it keep it in mind, as they do when one of the street welshes on another: his thoughts come back to it, and the onlookers may pass it on; false: only his liking for you drops");
            b.AppendLine("BrawlWagers = " + B(BrawlWagers) + "   ; with MindLive on: people watching a fistfight between two of the street's people bet on it among themselves (words only, nobody walks over), up to three bets a fight; after it the one who backed the loser pays, pays with a sour word, says he'll owe it, or won't pay, and a welshed bet leaves bad blood between the two backers");
            b.AppendLine("Rematches = " + B(Rematches) + "   ; with MindLive and [Mind] MindPlans on: the one left on the ground after a fistfight takes it his own way, by his nature: half a game day or more later, when the two meet near you, a rematch (the hot and the proud), crossing the street to keep clear (the fearful), or his hand offered (the kind), taken or refused; most let it lie");
            b.AppendLine("SideTalkMetres = " + F(SideTalkMetres) + "   ; with ScenesAtOnce 2 or more: the quiet talks beside the street's scene pair two people at most this far apart (2.5-8 m), both standing or strolling; 3.4 m and standing still before, and they almost never started");
            b.AppendLine("LastingMarks = " + B(LastingMarks) + "   ; someone you beat carries it for days (a beating four, a knocking down one): when you're about they think of it, and those fond of them name you for it");
            b.AppendLine("LastingMarksLook = " + B(LastingMarksLook) + "   ; ... and their body shows it: the gait and the bruising below (off as it comes: the names aren't proven; the Testing row \"A beating's marks\" tries each)");
            b.AppendLine("LastingMarksGait = " + LastingMarksGait + "   ; the game's gait for the marked, from the probe: injured_general, injured_left_leg, injured_right_leg, injured_torso, injured_left_arm, injured_right_arm (empty: none)");
            b.AppendLine("LastingMarksPack = " + LastingMarksPack + "   ; the game's damage pack for the marked, from the probe (empty: none)");
            b.AppendLine("MoreMoments = " + B(MoreMoments) + "   ; more of the moment in what people think: their horse bolting under them, their purse lifted or taken in the last hour and a half, rain on them in the saddle, a thunderstorm, standing in a fistfight's ring or near an execution by the road; women in a fistfight think their own lines; and how you look beside them has pools of its own");
            b.AppendLine("TelegraphKnows = " + B(TelegraphKnows) + "   ; the station agent at his counter thinks of what came over the wire these two days: deaths in other towns by name, and, with you at his window, your deed in another town before the word reaches his");
            b.AppendLine("PlacesMourned = " + B(PlacesMourned) + "   ; where someone died near you by another hand, those who were close to them, passing the spot in the three weeks after, stop and think of them; the dearest kneel there");
            b.AppendLine("MemorialPhotos = " + B(MemorialPhotos) + "   ; in Saint Denis and Blackwater: the photographer at his counter thinks of the memorial portraits he made of the town's dead these ten days, by name, and a mourner there now and then thinks of the photograph that came");
            b.AppendLine("UndertakerKnows = " + B(UndertakerKnows) + "   ; the undertaker (Rhodes, Armadillo) thinks of his town's dead of the last week by name and by how they died: the fever, the mine, the rope, a gun, by their own hand, or by you; a busy week; and, when you've killed twice or more, you as trade");
            b.AppendLine("GamblersBorrow = " + B(GamblersBorrow) + "   ; a townsman whose worry is the cards borrows from another man of his town, a friend first: they meet in the street over it as it goes on (he lends, refuses, or snaps at him), and at the end it's paid back where the street can see, or called in and never paid. The lender thinks of it too");
            b.AppendLine("TieGossip = " + B(TieGossip) + "   ; inside a town, word of your violence and your thieving moves person to person along the ties the street made: the friends of someone who knows hear it the next day, anyone else they're tied to the day after, a little more bent at each telling; who heard it thinks of who told them. A week on, the talk is done");
            b.AppendLine("SharedFright = " + B(SharedFright) + "   ; two people who both saw you kill are tied by it: the tie between them warms, for two weeks they keep clear of you together when both are about, and when the street's quiet talk falls to the two of them it is mostly about that, low-voiced");
            b.AppendLine("SurvivorsChanged = " + B(SurvivorsChanged) + "   ; someone you beat or robbed and left alive comes back changed, a day on, by who they are: angry and with friends, they bring up to two of them when they call you out (and they fight with them); the bold have more standing for having faced you; the timid are broken by it, never come at you, keep clear and stay home; their thoughts and their friends' say which");
            b.AppendLine("TrustSlow = " + B(TrustSlow) + "   ; trust lost fast, won slow: a shove, a beating, a robbery or a theft of yours takes three greetings' worth off their liking at once, and for 60 days your greetings count for less with them (a fifth the day after, the rest coming back over three weeks, sooner for the forgiving, and with each kindness since). Someone you never wronged: three hellos fill the bar as before");
            b.AppendLine("PeakEnd = " + B(PeakEnd) + "   ; a meeting with you (what they feel about you, no more than a quarter hour between) is remembered by its strongest moment and how it ended, not by how long it went on: a long friendly talk ended by a shove is remembered as the shove; the moments between fade to a quarter");
            b.AppendLine("StressBuilds = " + B(StressBuilds) + "   ; a week's troubles add up, person by person: a friend's death, a death seen, a beating, a theft, quarrels, a worry at its worst. Past their breaking point (lower for the anxious, higher for the steady) someone breaks, once in three weeks, in a way that fits them, and it stays a week: drinks the nights away, prays at the church, can't keep from the grave, takes it out on those at home, or takes the train");
            b.AppendLine("WorriesShown = " + B(WorriesShown) + "   ; with MindLive and MindActs on: the worry each person carries this week leans what they do (a debtor keeps to his work and away from the cards, a parent with a feverish child goes home and prays, a toothache rests and reaches for the whiskey, a dry spell keeps an eye on the sky), most at its worst, and the thought over their head says why");
            b.AppendLine("VoicedEncounters = " + B(VoicedEncounters) + "   ; with [Mind] VoicedDialogue on and the voice server running: what people say to you or near you in the encounters and walk-ups (a friend's visit, a warning, a campfire, a traveller, a callout at you, a bet on a fight) is said in their own cloned voice, matching the words, instead of a recorded remark picked by its tone; at most one such line every 5 seconds, so the graphics card isn't crowded. Off: the recorded remarks, as before");
            b.AppendLine("EventPrizeFights = " + B(EventPrizeFights) + "   ; with TownEvents on: now and then (at most once in six days a town) two of a town's men of a fighting temper settle it with their fists for a purse, out back of the saloon from nine at night; the word goes round that afternoon, never in the paper. There, J opens: a dollar on either man, tip off the sheriff, or watch");
            b.AppendLine("Payday = " + B(Payday) + "   ; Saturday from noon till eight in a town: wages paid, the working people lean to the saloon, the cards, the store and a drink and away from work, and a woman may stop a man on his way to the saloon for the week's money");
            b.AppendLine("MurderBallads = " + B(MurderBallads) + "   ; sitting by a campfire out of the towns at night, once a night: a murder ballad from the next fire on the ticker, four verses, of your most-told killing if one has been told, else of a death RealNPCs knows, by name and place");
            b.AppendLine("QuiltingBee = " + B(QuiltingBee) + "   ; Tuesday and Thursday from one till half past four, on foot and still for a few seconds within 25 m of a town's church steps spot: half a minute of the quilting bee through the open window on the ticker, the town's last three days by name as the women tell them, and what they say of you. Once a town a day");
            b.AppendLine("WalkHome = " + B(WalkHome) + "   ; with FondnessActs: after dark in a town, beside a woman, someone in drink or a timid man, J offers to see them home; they answer by their temper and, if it's yes, walk with you to their door");
            b.AppendLine("BarRound = " + B(BarRound) + "   ; at a saloon's bar by the barkeep, with a few others about: J to buy the house a round (a dime a head, $1 to $4); the room cheers and each one treated thinks better of you, the drunk most; one a day in each saloon");
            b.AppendLine("StoreSlate = " + B(StoreSlate) + "   ; at the store, now and then: a townsman or townswoman asks the keeper to put it on the slate, and he chalks it up, asks for something on account first, or says the slate's full (by what they owe this week and his manner). Refused, J for ten seconds lets you pay what they owe, a kindness remembered. On Saturday afternoon they pay it down instead");
            b.AppendLine("MoneyTalk = " + B(MoneyTalk) + "   ; two men meeting in a town argue gold against silver, the banks and the trusts (the bank panic in 1907); how they're fixed decides the side, and those who fall out are the colder for it");
            b.AppendLine("Suffrage = " + B(Suffrage) + "   ; by day in Saint Denis: a woman says what she thinks about the vote to a man she meets; he forbids it, laughs, is shaken or agrees");
            b.AppendLine("ScripTalk = " + B(ScripTalk) + "   ; by day at Annesburg: a man says what he thinks of being paid in the company's scrip; the other agrees, hushes him or warns him");
            b.AppendLine("Temperance = " + B(Temperance) + "   ; by day in the street: a temperance woman asks a drinker before the saloon doors to sign the pledge; he jeers, slinks off, swears off for the day, or is taken home on her arm. The pious start it oftener, and a man whose worry is the drink is the one she stops");
            b.AppendLine("GrindingSeason = " + B(GrindingSeason) + "   ; from 15 October to 20 December, the cane grinding: the working men of Rhodes, Saint Denis and Lagras work into the night (less saloon, less home) and think of the mill; with TownEvents, the grinding ball at Rhodes on the twentieth, and a couple who come away courting");
            b.AppendLine("EventFaisDoDo = " + B(EventFaisDoDo) + "   ; with TownEvents on: every Saturday night in Lagras, eight till half past eleven (its main street spot, else near you): the fiddle and the accordion, waltzes and two-steps, the babies sung to sleep, and a young couple of the village dancing every dance, who come away courting");
            b.AppendLine("EventBrassFuneral = " + B(EventBrassFuneral) + "   ; with TownEvents on: the day after a named death in Saint Denis, three till five from the cemetery spot (else near you): the society's brass band, the dirge going out, the hymn at the grave, and the second line dancing back");
            b.AppendLine("EventMardiGras = " + B(EventMardiGras) + "   ; with TownEvents on: on Fat Tuesday, seven till eleven on the Saint Denis main street spot (else near you): masks and torches, the krewe's float, a townsman crowned King of the Carnival, the king cake and the baby found by a townswoman, dancing till the bells; Ash Wednesday's quiet in the morning's word");
            b.AppendLine("EventFourth = " + B(EventFourth) + "   ; with TownEvents on: on the Fourth of July, six till half past nine at the bandstand spot (else near you) in Valentine, Saint Denis, Rhodes, Blackwater, Strawberry and Armadillo: the band, the Declaration read out, the speech, firecrackers, the shooting match (a townsman wins), the fireworks at dark");
            b.AppendLine("EventFair = " + B(EventFair) + "   ; with TownEvents on: the county fair at Valentine on the third Saturday of September, eleven till two at the stockyards spot (else near you): the livestock judged and a townsman's prize, the blue ribbon for pies and a rival's sour face, the greasy pig");
            b.AppendLine("EventBaseBall = " + B(EventBaseBall) + "   ; with TownEvents on: every other Saturday from April to September at two, Valentine against Strawberry at base ball, home by turns (posted Thursday; the home town's showground spot, else near you): the pitches, the hits, the crowd, the score; the losers sore, the season's tally kept");
            b.AppendLine("EventBarnRaisings = " + B(EventBarnRaisings) + "   ; with TownEvents on: March to October, now and then a farm family's barn is raised on a Saturday from nine (posted ahead; at the town's corral spot, else near you): the lifts, the dinner. Two men at odds are put on the same beam, and six times in ten the work mends it. The neighbours who came are warmer to the family, and the regulars to you if you were there");
            b.AppendLine("EventBoxSocials = " + B(EventBoxSocials) + "   ; with TownEvents on: posted on the Thursday, the first Saturday of the month from three, a box social at the church (its church steps spot, else near you): a young woman's supper box auctioned, two young men bidding each other up for it; J while they bid lets you go fifty cents over, and she eats with you. The winner and she come away courting, the loser envying him");
            b.AppendLine("EventBaptisms = " + B(EventBaptisms) + "   ; with TownEvents on: the Sunday after a camp meeting's altar call, from two, the man saved is baptised in the river at the town's riverbank spot (record one with the spots tool; without it, it happens away): the hymn, under and up, the amens. Baptised, he keeps from the saloon three weeks and doesn't backslide");
            b.AppendLine("EventRevivals = " + B(EventRevivals) + "   ; with TownEvents on: now and then from June to August, a preacher holds three nights of a camp meeting outside a country town (posted ahead), half past seven till ten, at its showground or church steps spot; on the third night a townsman whose worry is the drink or the cards goes up to the bench, and for two weeks keeps from the saloon (a third backslide at the week). A night there warms the regulars to you a little");
            b.AppendLine("EventShivarees = " + B(EventShivarees) + "   ; with TownEvents on: the night after a wedding in a town, from nine, the couple's friends gather with pans and a cowbell and shout for the groom until he comes out and stands them a round; there a minute, the regulars think better of you for it");
            b.AppendLine("WhileAway = " + B(WhileAway) + "   ; back in a town after two game days or more: what happened while you were gone (deaths, weddings, who left on the train, a new face at a counter, the dance, the trial, the hanging, the election you missed), in the Around town box, and someone near who knows you thinks of telling you");
            b.AppendLine("KinTownWrongs = " + B(KinTownWrongs) + "   ; with KinGrudges on: when one townsman beats or robs another, two times in five the victim's brother, father, son, cousin or uncle comes to town five to nine days on, and if the one who did it is near you, walks up to him and has it out (words, a walk-off, an apology, or blows between two hot-tempered men)");
            b.AppendLine("FenceCutters = " + B(FenceCutters) + "   ; about once in a fortnight, a homesteader in an open-range town (Valentine, Strawberry, Rhodes, Emerald Ranch, Blackwater, Armadillo, Tumbleweed) finds his wire cut at first light and blames the cattleman, his feud neighbour first: their tie goes two steps colder, toward the quarrels and fights. A third of the time it was drifters. If you were near the town that night, the Around town box says what you saw");
            b.AppendLine("SaleDay = " + B(SaleDay) + "   ; Tuesdays from ten to half past one in Valentine, with the stockyards spot recorded and you near it, the named men about the pens gather to the auctioneer: three lots called and bid on (four in the autumn sale), one in five sold so low its rancher is ruined (the debt worry for a week, with WorriesShown), and those who were there drink their pay from one");
            b.AppendLine("Foreclosures = " + B(Foreclosures) + "   ; the day a resident's worry over the bank ends badly, from ten till two, with the bankdoor (or mainstreet) spot recorded and you near: the sheriff's man sells their things in the street, three lots; the neighbours bid low, or two times in five won't bid at all; while a lot is called, J buys it back for the family, once a sale");
            b.AppendLine("Drummers = " + B(Drummers) + "   ; with TrainTime on: one man in three who steps down off a train is a drummer for a far city's firm, with a line of goods; near the store with him in town, he walks over to its keeper with his pitch, and the keeper orders, haggles or sends him off; in two days he's gone on the train");
            b.AppendLine("ShapeUp = " + B(ShapeUp) + "   ; from five to half past six in Saint Denis, with the wharf spot recorded and you near it, the named men about it crowd the wharf and the foreman picks his gang; the picked go to work, the passed-over to the saloon for the day, and a bitter one may have it out with the last man picked (a rematch plan, with MindPlans)");
            b.AppendLine("EditorSides = " + B(EditorSides) + "   ; when one of your deeds is printed, the town's paper's editor adds his word in his own slant (Saint Denis and Rhodes law and order, Valentine and Strawberry the people's, Blackwater and Armadillo business); for two days someone near you there who read it agrees or argues by temper");
            b.AppendLine("DriftingPosters = " + B(DriftingPosters) + "   ; in a town your deed's rumour has reached, the first time each day: the bill outside the sheriff's as it reads there (true where the telling's faithful, a thing wrong further out, wild when it's been passed along), in the box; and someone near now and then holds you up against it");
            b.AppendLine("DimeNovels = " + B(DimeNovels) + "   ; when your most-told deed has been talked of in two towns or more, on a Monday some New York hack writes it up: a dime novel in the store windows (the box has its lurid title), once a deed, sixty days apart; for thirty days people near you think of having read it, by their temper");
            b.AppendLine("GoingDry = " + B(GoingDry) + "   ; in John's 1907, Rhodes and Strawberry are dry by local option: the saloon pours under the counter, the law-abiding and the devout keep out, the rest talk of nothing else; the box shows the notice on the saloon door once a day");
            b.AppendLine("VigilanceCommittees = " + B(VigilanceCommittees) + "   ; with PosseRides: a posse that comes home with nothing after a killing can leave the town's angrier men to settle on someone a few days on: an execution on the road if you're near, and off screen if you never come. Someone can die of it");
            b.AppendLine("PosseFromTheRoad = " + B(PosseFromTheRoad) + "   ; with PosseRides: a killing out of town, near enough to a town for word to get there, with someone left alive to tell it, brings that town's posse too");
            b.AppendLine("ColdSnaps = " + B(ColdSnaps) + "   ; twice a winter, five days of bitter cold at Colter, Wapiti and Strawberry: the store runs out of coal oil, and an old man may not wake. Someone can die of it, off screen");
            b.AppendLine("TownRivals = " + B(TownRivals) + "   ; a tale crossing to a rival town (Valentine and Strawberry, say) arrives more bent than the miles would make it, and people near you say what they think of the place");
            b.AppendLine("WeekColumn = " + B(WeekColumn) + "   ; on a Sunday the paper carries a column of what really happened in the town you're nearest that week: who was buried, who married, how many fights");
            b.AppendLine("LettersHome = " + B(LettersHome) + "   ; those who left on the train write to a friend still in the town, and the friend has it on their mind for a few days");
            b.AppendLine("RoundupSeason = " + B(RoundupSeason) + "   ; in the fortnight after each roundup (July and November) the cattle towns fill with paid-off hands, and one comes back hurt");
            b.AppendLine("PosseRides = " + B(PosseRides) + "   ; the day after a bad crime in a town (a killing or a robbery with talk of it), three to five of its own men ride out as a posse; off screen, they come back a day on with nothing, with a stranger they say did it, or one short (a posse man killed, mourned like anyone); while they're out the town thinks of the road");
            b.AppendLine("WantedWhich = " + B(WantedWhich) + "   ; a resident living quietly with the wanted-poster worry: within 4 m you know the face (once each, in the box), and near them J: turn them in for the bounty ($50 to $150; gone in irons), warn them (they owe you, as one you saved), buy them a ticket on the next train ($15; gone by train), or leave it");
            b.AppendLine("NeighbourReward = " + B(NeighbourReward) + "   ; when one of a town's residents beats a man to death in a brawl, the sheriff posts $25 to $75 on him for a week: the notice in the box, the town split on it (the greedy think of the money, the loyal of warning him), and near him J: turn him in (paid; he goes to the county jail), warn him (he runs), or leave it");
            b.AppendLine("HoboSigns = " + B(HoboSigns) + "   ; near the station agent, once a town a day, the box shows the tramps' chalk on the water tower: two of the town's residents, each with the mark their temper earns (a cat for a kind woman, teeth for a temper, a cross for the devout)");
            b.AppendLine("Relics = " + B(Relics) + "   ; in John's 1907: the photographers of Saint Denis and Blackwater think of the trade in the old outlaws (cabinet cards of the dead, an old man paid to pose, the hanging postcards), and near the studio once a day the box shows the window, where one face is one you'd know");
            b.AppendLine("BoyRemembers = " + B(BoyRemembers) + "   ; with EpilogueCarries, in John's 1907: one who was a boy of eight to fifteen in '99 and met Arthur thinks of him as a boy remembers a man (the dollar, the reins, the way he stood), and if John comes within six metres, asks him aloud if he knew a man called Morgan");
            b.AppendLine("ClaimAgents = " + B(ClaimAgents) + "   ; three days after a man dies at the Annesburg mine, the company's claim agent calls on his widow with fifty dollars and a paper; after a killing in Saint Denis, the insurance man. The box tells it, and the next day what she did: signed, sent him away for a lawyer, or the town took up a collection");
            b.AppendLine("Fever = " + B(Fever) + "   ; rarely (a town in about a hundred and fifty a day, ninety days apart), a fever breaks out in a town and passes along its warm ties, one in seven a tie a day, among its residents: the sick keep to home about a week and think of it; at three sick the board of health posts a quarantine; it's over when nobody's sick, and the box says what it cost");
            b.AppendLine("FeverDeaths = " + B(FeverDeaths) + "   ; with Fever on: one in twelve of the sick dies of it, named and mourned like anyone (the wake, the grave). Off: they all get up again");
            b.AppendLine("MineWhistle = " + B(MineWhistle) + "   ; rarely (a day in twenty-five, never within three weeks of the last), the mine whistle at Annesburg blows three times: one or two of the town's men are killed below, named, never shown, and mourned like anyone. In Annesburg you hear it, people run for the pithead, the names come up, and the hat goes round; away, the word reaches you");
            b.AppendLine("Vouch = " + B(Vouch) + "   ; at the jail door the morning after a brawl ([Scenes] BrawlAfter), a third row: vouch for the man in the cells. He's out, bound over to keep the peace a week; break it and your $5 goes to the county and that sheriff won't take your word for a month; keep it and he's fonder of you; false: never");
            b.AppendLine("HorseWatch = " + B(HorseWatch) + "   ; down off your horse in a town by day, once a town a day: a man who's down on his luck offers to watch her for a dime (J); hired, he stands by her, shouts off a horse thief who goes for a horse near him, and says so when you're back in the saddle; false: never");
            b.AppendLine("WidowCollection = " + B(WidowCollection) + "   ; in the three days after a death in town, by day, a townswoman comes round with a collection for his widow or family: J, give a dollar, five, or not today; she remembers who gave, the town hears of it, and the Sunday after the preacher thanks a generous stranger; once for each death; false: never");
            b.AppendLine("StandTheWake = " + B(StandTheWake) + "   ; with BarRound and Wakes: on a wake night in town (8 till 2), J at the bar can stand the house a drink to his memory (a round's price, once a day); the barkeep says it for him, and those near you who knew him warmly think better of you; false: the round only");
            b.AppendLine("Wakes = " + B(Wakes) + "   ; the night after a death in town (or before one of enRAGE's funerals), from eight to two, the neighbours sit up with the dead: the Around town box tells you once, the street's quiet talks are of the dead half the time, and with West World one of the waking swears the dead one moved");
            b.AppendLine("AllSaintsGraves = " + B(AllSaintsGraves) + "   ; with Mourning: on the first of November in the south, those with someone dead go to the tombs, the ones you put there among them");
            b.AppendLine("Mourning = " + B(Mourning) + "   ; for three weeks after a friend's death they mourn: quieter days (home, prayer, not the cards or the saloon), their thoughts of the dead, neighbours hushing near them, and on a Sunday afternoon a walk to the grave to kneel (the town's cemetery spot, else by the church). At the grave of one you killed, they know you");
            b.AppendLine("SundayMorning = " + B(SundayMorning) + "   ; Sunday properly: from eight the town's believers make for the church (the devout most); from half past nine near the church you hear the hymn; the sermon is on the town's week (a killing, a theft, a death, the drink) and the churchgoers think it over after; from noon, dinner on the grounds");
            b.AppendLine("NoticeBoard = " + B(NoticeBoard) + "   ; what is posted for the coming seven days, gathered on one board: a Notice board page in the reader (each town by day: dances, elections, the circuit judge, hangings, the show, prize fights, announced weddings, funerals, Saturday wages and Sunday service), and as you come into a town, what it has posted, in the Around town box (once a town a day)");
            b.AppendLine("WitnessesChoose = " + B(WitnessesChoose) + "   ; each witness to your violence whom RealNPCs knows chooses: tell it, or keep quiet out of fear of you or loyalty to you (the law-abiding, the honest and a friend of the victim tell); if nobody who saw it will tell, the town never learns it was you. Within the hour, a gun on a witness leans on them: the frightened swear they saw nothing, the proud tell it sooner");
            b.AppendLine("NightScenes = " + B(NightScenes) + "   ; with MindLive on, at night in town: someone short of money and honest with nobody (or a known thief) robs someone on their own, who hands it over or won't by their nature; and late, a drunk who goes down in the street is got up and taken home by a friend. Each at most once a town a night");
            b.AppendLine("Aftermath = " + B(Aftermath) + "   ; with MindLive on: after a street fight near you, a friend of the one who went down (or a kind soul who saw it) goes to see to him, and a lawman near has a word with the one left standing");
            b.AppendLine("ChanceMeetMetres = " + F(ChanceMeetMetres) + "   ; two people up to this far apart may meet by chance near you, one walking over to the other (2-12; 3.4 was standing together only)");
            b.AppendLine("ScenesAtOnce = " + ScenesAtOnce + "   ; street scenes playing at once (1-6): one scene, and the rest quiet talks between other people elsewhere on the street (words only, never blows); 1: just the scene");
            b.AppendLine("EventfulOnly = " + B(EventfulOnly) + "   ; street scenes and chance meetings of nothing but talk (small talk, gossip, a song, a haggle, a tall tale, the paper read out) are passed over, and so are the quiet talks beside a scene: the street keeps its quarrels, fights, thefts, flirts, debts and troubles; false: the talk too, as before");
            b.AppendLine("MainScenesAtOnce = " + MainScenesAtOnce + "   ; full street scenes at once (1-3; two at most play for now): a second scene with its own words and walk-overs at least 20 m from the first, never the same people (fights, rough handling and thefts only ever in the first); 1: a single scene; the quiet talks of ScenesAtOnce come on top");
            b.AppendLine("GangShootouts = " + B(GangShootouts) + "   ; men of two rival gangs who meet within 90 m of you may fight it out (more often where enRAGE reports a war between them): words, a standoff, then a shootout until a side is beaten or gone; at most one in five minutes");
            b.AppendLine("WomenInFights = " + B(WomenInFights) + "   ; women may come to blows in a quarrel too (with a man or another woman), half as often as two men for each woman in it; false: only two men ever fight");
            b.AppendLine("RoughHandling = " + B(RoughHandling) + "   ; street scenes: a man with a grudge (or a hothead with drink in him) grabs another and smashes his face into the nearest table, bar or wall, with the game's own move; with nothing to hand it stays a fistfight");
            b.AppendLine("NpcThieves = " + B(NpcThieves) + "   ; street scenes: someone short of money or light-fingered picks a well-off or drunk mark's pocket; mostly the mark never knows who (townsfolk's pockets only: yours is Lifts)");
            b.AppendLine("CriminalClass = " + B(CriminalClass) + "   ; people who take to crime keep to it: anyone with a pocket picked, a lift or a mugging to their name in the last thirty days is likelier to go for a pocket again whatever their nature or their money, wears the thief's name over their head the whole month, rolls a drunk with words of its own, and after thirty clean days has gone straight and thinks of it once; false: a thief is forgotten in a week, as before");
            b.AppendLine("DrunkChallenges = " + B(DrunkChallenges) + "   ; street scenes: a drunk in a saloon at night calls a man out and falls on his face before a punch is thrown");
            b.AppendLine("BarkeepSteps = " + B(BarkeepSteps) + "   ; the barkeep steps in on a saloon brawl or rough as they square up: the hot and the drunk heed him three times in ten, the proud half the time, the rest three in four, and take it outside with no blows; the rest shove him off, he swears to fetch the sheriff, and the fight goes on; false: he only has his say after");
            b.AppendLine("DrunkOutside = " + B(DrunkOutside) + "   ; a drunk's challenge taken up (the other squares up) is really taken outside: the two walk to the saloon's door you last came out of (within 40 m) and the fight is in the street; false: the drunk falls on his face inside, as before");
            b.AppendLine("CrudeTalk = " + B(CrudeTalk) + "   ; in those conversations, the game's own crude remarks: a man to a woman in a saloon of an evening (she tells him off), or between sweethearts at night");
            b.AppendLine("SceneReactions = " + B(SceneReactions) + "   ; greet or antagonize someone in a scene: take a side, break up a fight, or stir it up");
            b.AppendLine("Standoffs = " + B(Standoffs) + "   ; two men square off in the street (guns come out only away from the law)");
            b.AppendLine("Songs = " + B(Songs) + "   ; drunks sing old songs in saloons and at night");
            b.AppendLine("TallTales = " + B(TallTales) + "   ; old-timers tell tall tales, sometimes about you");
            b.AppendLine("NightTales = " + B(NightTales) + "   ; at night the old tales are told, in the saloon, in the street after dark and at a campfire in the wilds: the bell on the grave, the tommyknockers, the feu follet, the Bell Witch, Tailypo, the fiddler at the crossroads, the white stallion; the listener shivers, scoffs or leans in by their nature");
            b.AppendLine("NewspaperTalk = " + B(NewspaperTalk) + "   ; somebody reads the paper aloud, your deeds included");
            b.AppendLine("Haggling = " + B(Haggling) + "   ; horse trading and haggling in the street");
            b.AppendLine("SnakeOil = " + B(SnakeOil) + "   ; a snake-oil man works the crowd");
            b.AppendLine("SmallTalk = " + B(SmallTalk) + "   ; neighbours pass the time of day");
            b.AppendLine("Pickpockets = " + B(Pickpockets) + "   ; a pickpocket strikes near you: run him down and the town hears of it; false also stops the one who bumps into you and lifts your money (Lifts)");
            b.AppendLine("PosseTalk = " + B(PosseTalk) + "   ; after a bad crime in town, men talk about getting up a posse");
            b.AppendLine("Mourners = " + B(Mourners) + "   ; the widow (or brother) of a stranger you killed grieves in town");
            b.AppendLine("Pleading = " + B(Pleading) + "   ; hold a gun on a stranger and they beg; lower it or don't");
            b.AppendLine("GunpointStages = " + B(GunpointStages) + "   ; with Pleading: keep the gun on someone six seconds and they take a second step by who they are (beg, defy you, back away a step at a time, break and run, or freeze with their hands up; the model picks when it's on and the mind knows them), and a third at fourteen (most who begged or froze break and run; the steady hold, the defiant stand); false: the plea and the game's own answer alone");
            b.AppendLine("ArrivalTalk = " + B(ArrivalTalk) + "   ; when you ride into town, the locals say what they've heard of you");
            b.AppendLine();
            b.AppendLine("[Pacing]");
            b.AppendLine("ThoughtEverySeconds = " + ThoughtEverySeconds);
            b.AppendLine("ToastGapSeconds = " + ToastGapSeconds);
            b.AppendLine("LifeUpdateGapSeconds = " + LifeUpdateGapSeconds);
            b.AppendLine("RemarkGapSeconds = " + RemarkGapSeconds);
            b.AppendLine("RemarkSamePersonSeconds = " + RemarkSamePersonSeconds);
            b.AppendLine("RemarkDistance = " + F(RemarkDistance) + "   ; metres");
            b.AppendLine("SubtitleSeconds = " + SubtitleSeconds + "   ; how long each spoken line stays up");
            b.AppendLine("AmbientFlirtSeconds = " + AmbientFlirtSeconds + "   ; at most one flirt between strangers this often");
            b.AppendLine();
            b.AppendLine("[Output]");
            b.AppendLine("NewsTo = " + NewsTo + "   ; where news, rumours and what people elsewhere think of you go: corner (the \"Around town\" box, top right), ticker, gamefeed (the game's own updates, top left), or off (no Around town box at all)");
            b.AppendLine("AroundTownStaysSeconds = " + AroundTownStaysSeconds + "   ; how long the Around town box keeps the town's regard of you up after you come into a town or it changes (seconds; 0: the whole time you're in a town, as before 2026-11-07)");
            b.AppendLine("TownRegard = " + B(TownRegard) + "   ; the town's regard of you in the Around town box: a line under its title while you're in a town (a killer here, a peacemaker, or nothing held against you; the killings of yours known this week and your kindnesses) and an item when you come into a town and whenever it changes (the town's mood, how many of the people you know here are afraid or fond of you, the talk that has reached the town); false: neither");
b.AppendLine("Translate = " + B(Translate) + "   ; a translation of your own: translation.txt in the RealNPCs folder (beside the saves), one line each: the English, a TAB, your translation (UTF-8; # starts a note; the English may keep the {slots}, and your translation puts them where your language wants). Read when the game starts and again within ten seconds of saving it. The bar, the thoughts over heads, the subtitles and the hints show it; the voices still say the English. The Testing row Write the translation template writes every line, ready to fill in, to translation-template.txt");
                        b.AppendLine("Subtitles = " + Subtitles + "   ; what people say to you, while their voice plays (their recorded words when known). The game draws its own white subtitle for any recorded line while its Subtitles option is on, and a mod can't stop that, so: game: RealNPCs' boxed subtitles only while the game's option is off (one set at a time); ours: RealNPCs' always (switch the game's off to see only these); both; off: none of RealNPCs'. Talk you only overhear (ScenesOnTicker) shows in the same box unless this is off");
            b.AppendLine("UseGameFeed = " + B(UseGameFeed) + "   ; EXPERIMENTAL: the game's top-left toasts (same as NewsTo = gamefeed)");
            b.AppendLine("TestKey = " + TestKey + "   ; shows what the nearest person enRAGE knows thinks of you");
            b.AppendLine("WarNewsTo = " + WarNewsTo + "   ; where gang and faction news (wars, feuds, clashes, rivalries) is shown: feed (the game's own updates, top left), corner (the \"Around town\" box) or ticker");
            b.AppendLine("TerritoryNews = " + B(TerritoryNews) + "   ; enRAGE's territory changes (a gang gaining or losing its hold on a district) shown where WarNewsTo says. enRAGE also prints them itself, as white text, while its own [Menu] Messages is 3");
            b.AppendLine("FlirtKey = " + FlirtKey + "   ; look at someone (as if to greet them) and press it to flirt");
            b.AppendLine("ReaderKey = " + ReaderKey + "   ; read the diaries, letters and clippings you carry");
            b.AppendLine("ReloadIni = " + B(ReloadIni) + "   ; pick up changes to this file while the game runs");
            b.AppendLine("ReplaceEnrageSettings = " + B(ReplaceEnrageSettings) + "   ; Alt+F11 opens the larger RealNPCs settings panel (RealNPCs, West World, Stalkers and every enRAGE section) instead of enRAGE's own (enRAGE 0.9.5.3 only)");
            b.AppendLine("SettingsKey = " + SettingsKey + "   ; opens the RealNPCs settings panel (with any enRAGE version), and closes it again");
            b.AppendLine("SettingsKey2 = " + SettingsKey2 + "   ; a second key that does the same (None: no second key)");
            b.AppendLine("PanelSlowsTime = " + B(PanelSlowsTime) + "   ; the world slows while the settings panel is open, as it does while the game's weapon wheel or satchel is open (never in a story mission)");
            b.AppendLine("PanelShowsAll = " + B(PanelShowsAll) + "   ; the settings panel shows every setting and the Testing page; false: only the essentials, the UI page, the hotkeys and the mod list (F on the panel switches)");
            b.AppendLine("PanelTimeScale = " + F(PanelTimeScale) + "   ; 0 pauses the world and its clock while the panel is open; 1 is normal speed, 0.25 a quarter (0-1)");
            b.AppendLine("DebugLog = " + B(DebugLog));
            b.AppendLine("PerfMeter = " + B(PerfMeter) + "   ; times every part of RealNPCs each frame: F7 writes the costliest (TEST perf), and the log says when the add-on goes over 1 ms a frame for 10 s or one frame over 4 ms (PERF)");
            b.AppendLine();
            b.AppendLine("[Controller]");
            b.AppendLine("PadKeys = " + B(PadKeys) + "   ; a controller's buttons for RealNPCs' keys below, used while a controller is the last thing you touched");
            b.AppendLine("; each is a button held and another pressed (RB+DPadUp), or one button alone. Buttons: A B X Y LB RB LT RT LS RS Select DPadUp DPadDown");
            b.AppendLine("; DPadLeft DPadRight (PlayStation: Cross Circle Square Triangle L1 R1 L2 R2 L3 R3 Touchpad), or a game control by name (INPUT_...); None: no button.");
            b.AppendLine("; The settings panel's Hotkeys page sets them from the controller. Everything on its Testing page fires from there with A.");
            b.AppendLine("PadSettings = " + PadSettings + "   ; opens the settings panel (as " + SettingsKey + " does)");
            b.AppendLine("PadStatus = " + PadStatus + "   ; what the nearest person thinks of you, and the status lines in the log (as " + TestKey + " does)");
            b.AppendLine("PadLetters = " + PadLetters + "   ; your letters, diaries and clippings (as " + ReaderKey + " does)");
            b.AppendLine("PadTickerNext = " + PadTickerNext + "   ; the next line on the ticker now (as " + TickerNextKey + " does)");
            b.AppendLine();
            b.AppendLine("[Testing]");
            b.AppendLine("TestKeys = " + B(TestKeys) + "   ; master switch for the keys below (the settings panel's Testing page fires them with it off too)");
            b.AppendLine("ProofTour = " + B(ProofTour) + "   ; the Testing row \"The proof tour\" walks every row that can run where you stand, each in its turn, and the log says of each PROOF ran, skipped (its own why) or failed, then a PROOF summary; it pauses for a fight, a mission, a menu or the saddle (the row again resumes it); false: the row does nothing");
            b.AppendLine("ProofTourRisky = " + B(ProofTourRisky) + "   ; the proof tour runs the risky rows too (real money, danger to you, someone's death, a probe of something unproven); false: they're passed over and the log says why");
            b.AppendLine("ProofTourGapSeconds = " + ProofTourGapSeconds + "   ; the proof tour fires a row about every this many seconds (8-120), once the last one is no longer under way");
            b.AppendLine("ShowMinds = " + B(ShowMinds) + "   ; a small card over named people within 11 m: temper, mood, worry, ties and what is on their mind");
            b.AppendLine("ShowWakeStages = " + B(ShowWakeStages) + "   ; a tag over each person within 25 m who has begun to wake: stirring, questioning, unravelling or awake, and what holds them back (riding, a counter, had their chance)");
            b.AppendLine("TestCastReturns = " + B(TestCastReturns) + "   ; the Testing page's test cast, once made and kept in the save, come back by themselves (out of sight) when you come to their town; off as it comes: the cast come only on the Testing row's key");
            foreach (TestActions.Entry e in TestActions.All)
                b.AppendLine(e.KeySetting + " = " + TestKeyOf(e) + "   ; " + e.Label + (e.Needs != null ? " (" + e.Needs + ")" : string.Empty) + "; None: no key");
            return b.ToString();
        }

        /// A setting's value written the way the ini writes it ("true", "0.962", "rush"), or null for an unknown key.
        public string ValueText(string key)
        {
            TestActions.Entry te = TestActions.ByKey(key);
            if (te != null && !te.Field)
                return TestKeyOf(te);
            FieldInfo f = typeof(Settings).GetField(key ?? string.Empty, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            if (f == null)
                return null;
            object v = f.GetValue(this);
            if (v is bool)
                return B((bool)v);
            if (v is float)
                return F((float)v);
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string B(bool b)
        {
            return b ? "true" : "false";
        }

        private static string F(float f)
        {
            return f.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
