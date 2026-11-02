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
        public const int CurrentVersion = 186;

        public int SettingsVersion = CurrentVersion;

        // [Features]
        public bool Conversations = true;
        public bool PanelShowsAll;   // the settings panel shows every setting and the Testing page; off: the essentials (Lex, 2026-10-30; settings version 123)
        public bool ResetAtClerk = true;   // "Reset RealNPCs Memory" in a station clerk's list (Lex, 2026-10-30; settings version 121)
        public bool Remarks = true;
        public bool Thoughts = true;
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
        public bool PersonalSpace = true;   // someone he stands right up against for a couple of seconds minds it: steps back, asks for room, or stands their ground (NIGHT3; settings version 154; plays through SightReactions)
        public bool ArthurSpeaks = true;   // Arthur says a recorded line of his own when someone comes at him or he catches the pickpocket (roadmap 34; settings version 151; NIGHT3 item 7)
        public bool NpcFlirting = true;
        public bool Trysts = true;
        public bool CasualHookupsDecides = true;
        public int WillingSingle = 70;
        public int WillingTaken = 20;
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
        public bool ThoughtSounds = true;
        public int TickerGapSeconds = 3;
        public bool ThoughtMarker = true;
        public bool ThoughtsOverHeads = true;
        public float ThoughtsOverHeadScale = 0.24f;
        public bool TickerShowsScene = true;   // the bar names the street scene playing out near you, while it plays (Lex, 2026-10-30; settings version 122)
        public bool ScenesOnTicker = true;        // a street scene's lines on the ticker while it plays within ScenesOnTickerMetres (2026-10-29, Lex)
        public float ScenesOnTickerMetres = 12f;   // was 30 before settings version 120 (Lex, 2026-10-30: a mourner's words subtitled from 26 m away)
        public bool ShareWithEnrage = true;
        public bool Minds = true;
        public bool RecordMinds = true;
        public bool RecordFates = true;
        public bool MindShadow = true;    // the NPC mind (daytime 2026-10-27): works out traits, needs, feelings and mood and only reports them
        public bool MindLive = true;      // what the mind decides reaches the screen (Lex, 2026-10-30: on for everyone from 0.7.2; off until settings version 126)
        public bool MindLevels = false;   // the shadow's scan run from the levels scheduler (night 2026-10-28, item 7; settings version 89): a near ring in view, the town around every 10-30 s, the people away one time a game day, a budget of work a frame; off, the scan as it was
        public bool QuarrelLadder = true;  // with MindLive: an argue or a brawl between two people the mind knows is played from the quarrel ladder (Core/Escalation.cs, Scenes.Ladder.cs; night 2026-10-28, item 16; settings version 92): how far it goes and how it ends from their traits and the surroundings; the shot is never played
        public bool SaidLately = true;   // the street's talk avoids lines said in the last ~300 (the no-model run, item 5; settings version 183)
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
        public bool MindWakes = true;        // with MindLive and West World: what someone has lived through (the shadow's doubts about "things are as they seem") leans the Awakening, a buried man seen walking breaks that belief, and the moment it gives way they stir (Core/Engine.Doubt.cs; the mind doc's step 10; night 2026-10-29, item 4; settings version 109); off, the Awakening by its own roll alone
        public bool MindPassing = true;      // with MindLive: a greeting in passing follows what the first one feels about the other lately and their mood (Core/Passing.Mind.cs; roadmap 39; night 2026-10-29, item 6; settings version 111); and how someone shows you what they feel about you from a while back as you pass (Core/Passing.ToYou.cs, item 6f); off, the tie's greeting
        public bool MindPassingLasts = true; // with MindPassing: how someone shows you what they feel about you as you pass reads the feeling by the mood's slower fading (hours for a slight), not its own quick one (minutes), and from 12 m, not 9 (NIGHT4 item 5; settings version 170)
        public bool MindHelps = true;        // with MindLive: who helps someone up after a fall, and how, comes from what those who saw it feel (Scenes.FallAnswerOf; roadmap 39; night 2026-10-29, item 6b; settings version 112); off, the tie and a roll
        public bool MindRescue = true;       // with MindLive: when someone awake begins their last words, someone near whom it moves (a friend, a kind stranger) may go to them and talk them down (Core/Rescue.cs; roadmap 3's other side; night 2026-10-29, item 6d; settings version 113); off, nobody steps in
        public int RescueChance = 60;        // with MindRescue: of the despairs where someone near is moved to go to them, this share in percent where they do (the rest hesitate too long); seeded (settings version 114)
        public bool MindMeetings = true;     // with MindLive: the street's chance meetings lean by the two's moods today and how much they want company (Core/Reasons.Mood.cs; roadmaps 18 and 30; night 2026-10-29, item 6e; settings version 115); off, by the reasons alone
        public bool MindChanceMeetings = true; // with MindLive: what a chance meeting near you is (a song, an argument, a trade, a tall tale ...) is chosen by the two people's minds: their tie, their day, their worry, drink and nature lean what the place allows, and with LanguagePicks the model may pick among the three likeliest (Core/Scenes.Minds.cs; NIGHT4 item 5's plan E; settings version 171); off, the roll by place, sex and age alone
        public bool ThoughtsAloud = true;   // with Language: a thought over someone near is said aloud in their own recorded voice when one of their remarks fits it (settings version 143)
        public bool LanguageVariety = true;   // with Language: each thought the model words gets one thing their mind goes to and a shape, their thoughts lately to steer clear of, and no doubting the world without cause (Core/Language.Variety.cs; settings version 159)
        public bool ModelWakes = true;        // with Language, MindWakes and LanguageVariety: someone with cause who wonders at the world in a thought of their own chips "things are as they seem", so the model's wondering can start them stirring (settings version 159)
        public bool LanguageTalk = true;   // with Language: what people answer in Talk is said in the model's words (the meaning stays the menu's; the written line if it's slow or fails the checks) (settings version 156; NIGHT3)
        public bool LanguagePicks = true;   // with Language: the model picks what people near you do, from the few things the mind allows (settings version 142)
        public bool MindStartsScenes = true;   // with LanguagePicks: a person's own reasons with someone near (a quarrel, a friend to comfort, a debt) join the model's list, and a pick starts the scene (settings version 146; NIGHT3 item 1)
        public int PickMetres = 25;    // how far from the player the model picks for people (15 until settings version 147; NIGHT3 item 2); past 15 m it's asked half as often
        public int PicksAtOnce = 8;    // how many picks may wait on the model at once (4 until settings version 147)
        public bool MindTownMood = true;   // with MindLive: a town on edge for some hours after a killing (halving every three game hours; the safety need for all, the model told), and the same trouble seen again moving onlookers less (settings version 149; NIGHT3 item 5)
        public bool MindTempers = true;   // with MindLive: displaced aggression (shamed earlier, they take it out on someone who won't hit back) and the culture of honour in Lemoyne (quarrels climb faster, backing down costs more) (settings version 152; NIGHT3)
        public bool Language = false;        // the language layer (2026-10-29; roadmap 15): with MindLive, the mind's own thoughts worded by a local model (Ollama at LanguageUrl, LanguageModel) through Language.Check, the written line always the fallback; off until the generated thoughts pass the checks at scale (settings version 94)
        public bool LanguageChosen;          // Language was set by the player (the panel or this file); while false and Language off, each start asks Ollama off the game thread and turns Language on, once, if it answers with the model (settings version 163)
        public string LanguageUrl = "http://127.0.0.1:11434";   // the local server; nothing leaves the machine
        public string LanguageModel = "llama3.1:8b";           // the model Ollama holds (ollama pull <name>)

        // [Display] - drawn on screen besides the ticker
        public bool FeelingBars = true;
        public float FeelingBarsMetres = 15f;
        public bool FeelingBarsShowEmpty;
        public string NameCards = "known";        // off | known | everyone: names over heads, enRAGE's way (2026-10-29)
        public float NameCardsMetres = 25f;       // strangers' cards within (30 until settings version 129; Lex, 2026-10-30: 25)
        public bool NameCardsAlways = true;       // a card over everyone in range, always: hidden only by walls, not tables or people, sixteen at most, drawn before the Testing overlays (Lex, 2026-10-30; settings version 129)
        public float NameCardsKnownMetres = 75f;  // people you know within
        public bool NameCardsBesideEnrage;        // ours beside enRAGE's own cards too (else left to enRAGE while its are on)
        public bool NameCardsStrangerNames;      // a stranger's own name over "Stranger"; off: "Stranger" alone (Lex, 2026-10-30; settings version 124)
        public bool NameCardsSecondLine = true;   // the line under the name (temper, trade, Stranger, gang); off: the name alone (players, 2026-10-30; settings version 117)
        public bool WhosWho = true;               // for everyone (Lex, 2026-10-30; MindLive only for its Lately): the reader (the papers key) ends with a page of the people you've met, read from what is kept (Core/WhosWho.cs; roadmap 24; night 2026-10-29; settings version 116)
        public bool GrudgeMeter = true;

        // [WestWorld] - the master switch and everything under it
        public bool WestWorld = true;
        public float AwakeningShare = 2.25f;   // a share of a percent too (0.6); 5 until settings version 138 (Lex, 2026-10-30: fewer waking), 3 until 159 (Lex, 2026-10-31: fewer stirring, a little)
        public int AwakeningDays = 5;   // 1 until settings version 138 (Lex, 2026-10-30: "very slow how fast they progress the stages")
        public int AwakeningBreakdown = 90;
        public int AwakeningDespair = 75;
        public bool WakeVoices = true;   // the waking speak the game's own lines, oddly, more each stage (Core/WakeVoice.cs; settings version 78)
        // the waking held and stranger (settings version 144; Lex, 2026-10-31: they swapped back to the game between acts; Game/WestworldScript.WakeHold.cs)
        public bool WakeHold = true;
        public int WakeHoldMinutes = 3;
        public bool WakeGestures = true;
        public bool WakeStrange = true;
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
        public bool Gatherings = true;
        public int GatheringHostile = 50;   // [Encounters] percent of gatherings that turn on you once you come within 10 m (Lex, 2026-11-01; violence against Arthur, on as it comes); 0 never; settings version 186
        public int GatheringChance = 15;
        public string GatheringHours = "any";   // any: the night's (midnight to 03:30, the wilds) and by day, in the open or at a town's edge; night: the night's only, as before settings version 163 (Lex, 2026-10-31: "make them happen more often and at any time of day")
        public int GatheringGapHours = 6;       // game hours at least between gatherings, so they don't stack (settings version 163)
        public string GatheringKey = "None";
        // re-staged from the game's own encounters, with the town's people choosing by their minds (NIGHT4 item 4; settings version 169)
        public bool PublicCallouts = true;      // by day in a town, one of the people about calls another out before the street over something real between them
        public int PublicCalloutChance = 35;
        public bool DomesticDisputes = true;    // in a town, a man shouting at his wife in the street; she and a neighbour answer by their own minds
        public int DomesticChance = 30;
        public bool Hogtied = true;             // out of the towns by day, one of a nearby town's people tied up by the road with two men by them, who they are from the captive's own life
        public int HogtiedChance = 20;
        public bool HogtiedMenDraw = true;   // on as it comes from version 172 (Lex, 2026-11-01: violence against Arthur on by default): the hogtied's men may draw on you unprovoked as you come near; off: they fight only once you step in, as the execution's men

        // [World]
        public bool Deeds = true;
        public bool SelfDefenceNoBounty = true;
        public bool TownPosts = true;
        public string TownPostsAfterDeath = "survive";
        public bool Regulars = true;
        public int RegularsPerTown = 30;
        public bool RegularsDress = true;
        public bool RumoursTravel = true;
        public bool Newspaper = true;
        public bool Letters = true;
        public bool FindDiaries = true;
        public bool Avengers = true;
        public int AvengerChance = 35;
        public int AvengersAtOnce = 2;      // riding, here or still alive on enRAGE's roster (Lex's players, 2026-10-28)
        public int AvengerGapHours = 48;    // game hours between one avenger's arrival and the next's
        public bool CastRelatives = true;
        public bool CastRelativesSpawn = true;   // when nobody free is on the street (enRAGE has named most walkers in a well-played town), the relative is made out of sight behind you and let go to the game after (Lex, 2026-10-29; settings version 95)
        public int Bravery = EnrageWestworld.Core.Bravery.AsBuilt;   // [World] how brave the townsfolk are, 0-100: the brave hold their ground where the game would send them running (PCF 44, cower instead of flee), and stand back from the mod's own frights; 0 as the game has it (Lex, 2026-10-31; settings version 161)
        public string Storyteller = "lively";   // [World] off | calm | lively | wild: the storyteller shortens the waits on a quiet street or road and lengthens them a little after something big; it never starts anything itself (NIGHT4 item 2; settings version 167)
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
        public bool NightScenes = true;   // [Scenes] with MindLive: at night, a mugging by someone whose mind leans to it, and a drunk taken home by a friend (the NPC-life run, step 5; settings version 135)
        public bool Aftermath = true;   // [Scenes] with MindLive: after a street fight near you, a friend tends the one who went down and a lawman near has a word with the one left standing (the NPC-life run, Lex 2026-10-30; settings version 131)
        public float ChanceMeetMetres = 6f;   // [Scenes] two people this far apart may meet by chance, one walking over to the other (3.4, standing together, until settings version 134; Lex, 2026-10-30)
        public int ScenesAtOnce = 4;   // 2 (at most 3) until settings version 140 (Lex, 2026-10-30: more at once);   // [Scenes] street scenes playing at once: the one scene, and quiet talks beside it elsewhere on the street (the NPC-life run, Lex 2026-10-30: "several at once"; settings version 127); 1: just the scene, as before
        public int MainScenesAtOnce = 2;   // [Scenes] full street scenes at once (1-3; at most two play): a second full scene at least 20 m from the first, never the same people, words and walk-overs only (the fights, the rough move and thefts stay the first's); 1: a single scene, as before; apart from ScenesAtOnce's quiet talks (NIGHT4 item 3; settings version 168)
        public bool GangShootouts = true;   // [World] men of two rival gangs who meet near you fight it out: words, a standoff, a shootout (Lex, 2026-10-30; settings version 133)
        public bool WomenInFights = true;   // [Scenes] women may come to blows too, at half a man's chance for each woman in it (Lex, 2026-10-29: "we can allow men to trade blows with women in this mod ... 50% less likely than a man getting in a fight until we have the innermind logic take over"; night 2026-10-29, item 2b; settings version 107); false: two men only, as before
        public bool NpcThieves = true;
        public bool DrunkChallenges = true;
        public bool Reactions = true;
        public bool Acknowledge = true;
        public bool SightReactions = true;
        public bool Kindnesses = true;
        public bool Vices = true;
        public bool Grudges = true;
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
        public bool DoctorsPatient = true;   // [Scenes] someone goes to the doctor for what ails them, tells him, hears his word, and walks home (night 2026-10-28, item 14; widened 2026-10-29; settings version 90)
        public float ErrandPace = 2.0f;   // [Scenes] how fast people walk on errands for RealNPCs: 1 a walk, 2 brisk (was about 1-1.4), 3 a run (Lex, 2026-10-29)
        public bool LawChases = true;   // [Scenes] a lawman near runs down a pickpocket of ours and brings him down (Lex, 2026-10-29)
        public int LawShootsThieves = 50;   // [Scenes] percent: with LawChases, the lawman shoots the one who picked your pocket instead of running him down (Lex, 2026-11-01); settings version 184
        public bool Lifts = true;   // [Scenes] a man bumps into you in town and lifts some of your cash; catch him to get it back (Lex, 2026-10-29)
        public bool EnragePeopleJoinIn = true;
        public bool Standoffs = true;
        public bool Songs = true;
        public bool TallTales = true;
        public bool NewspaperTalk = true;
        public bool Haggling = true;
        public bool SnakeOil = true;
        public bool SmallTalk = true;
        public bool Pickpockets = true;
        public bool PosseTalk = true;
        public bool Mourners = true;
        public bool Pleading = true;
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
        public string Subtitles = "game";   // game | ours | both | off (2026-10-28)
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
            GatheringHostile = Math.Max(0, Math.Min(100, GatheringHostile));
            LawShootsThieves = Math.Max(0, Math.Min(100, LawShootsThieves));
            ActThoughtEachSeconds = Math.Max(20, Math.Min(600, ActThoughtEachSeconds));
            ActThoughtGapSeconds = Math.Max(0, Math.Min(120, ActThoughtGapSeconds));
            PicksAtOnce = Math.Max(1, Math.Min(16, PicksAtOnce));
            BrawlMost = Math.Max(Brawl.LeastAtAll, Math.Min(Brawl.MostAtAll, BrawlMost));
            BrawlDrawShare = Math.Max(0, Math.Min(100, BrawlDrawShare));
            BrawlFistsFatal = Math.Max(0, Math.Min(100, BrawlFistsFatal));
            BrawlJoinMetres = Math.Max(10f, Math.Min(60f, BrawlJoinMetres));
            BrawlCrowdMetres = Math.Max(10f, Math.Min(60f, BrawlCrowdMetres));
            BrawlLeastSeconds = Math.Max(15, Math.Min(120, BrawlLeastSeconds));
            BrawlJoinBoost = Math.Max(0, Math.Min(60, BrawlJoinBoost));
            BrawlOpeningBoost = Math.Max(0, Math.Min(60, BrawlOpeningBoost));
            Bravery = Math.Max(0, Math.Min(100, Bravery));
            // stalker hours: the stalker day still runs 05:00 to 05:00; day hours stay in daylight, night hours cross midnight
            StalkerEveryMinutes = Math.Max(1, Math.Min(120, StalkerEveryMinutes));
            StalkerBlackOutfit = Math.Max(-1, Math.Min(200, StalkerBlackOutfit));
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
            HushHours = Math.Max(0f, Math.Min(12f, HushHours));
            HushHoursSaintDenis = Math.Max(0f, Math.Min(12f, HushHoursSaintDenis));
            StrainChance = Math.Max(0, Math.Min(100, StrainChance));
            TownPostsAfterDeath = (TownPostsAfterDeath ?? "").Trim().ToLowerInvariant() == "successor" ? "successor" : "survive";
            RegularsPerTown = Math.Max(5, Math.Min(80, RegularsPerTown));            StalkerChance = Math.Max(0, Math.Min(100, StalkerChance));
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
            ScenesOnTickerMetres = Math.Max(5f, Math.Min(80f, ScenesOnTickerMetres));
            PanelTimeScale = Math.Max(0f, Math.Min(1f, PanelTimeScale));
            PadSettings = PadChord.Normal(PadSettings) ?? "RB+DPadUp";   // one that can't be read: the default (the panel is how a controller reaches everything)
            PadStatus = PadChord.Normal(PadStatus) ?? "None";
            PadLetters = PadChord.Normal(PadLetters) ?? "None";
            PadTickerNext = PadChord.Normal(PadTickerNext) ?? "None";
            if (string.IsNullOrWhiteSpace(LanguageUrl)) LanguageUrl = "http://127.0.0.1:11434";
            if (string.IsNullOrWhiteSpace(LanguageModel)) LanguageModel = "llama3.1:8b";
            LanguageUrl = LanguageUrl.Trim().TrimEnd('/');
            LanguageModel = LanguageModel.Trim();
            WillingSingle = Math.Max(0, Math.Min(100, WillingSingle));
            WillingTaken = Math.Max(0, Math.Min(100, WillingTaken));
            TickerGapSeconds = Math.Max(0, Math.Min(20, TickerGapSeconds));
            RealVoiceTalkSeconds = Math.Max(30, Math.Min(1800, RealVoiceTalkSeconds));
            // game | ours | both | off (Lex, 2026-10-28: the game's own white subtitle for a recorded line can't be stopped, so
            // one set at a time); the old on (and matched, game) read as game
            string sm = (Subtitles ?? "").Trim().ToLowerInvariant();
            Subtitles = sm == "off" || sm == "ours" || sm == "both" ? sm : "game";
            ErrandPace = Math.Max(1f, Math.Min(3f, ErrandPace));
            FeelingBarsMetres = Math.Max(3f, Math.Min(40f, FeelingBarsMetres));
            string nc = (NameCards ?? "known").Trim().ToLowerInvariant();
            NameCards = nc == "off" || nc == "everyone" ? nc : "known";
            string sto = (Storyteller ?? "lively").Trim().ToLowerInvariant();
            Storyteller = sto == "off" || sto == "calm" || sto == "wild" ? sto : "lively";
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
            b.AppendLine("MindLive = " + B(MindLive) + "   ; what that mind decides reaches the screen (their moods, and thoughts of what they really feel and need, and what they do about it); each part has its own switch below");
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
            b.AppendLine("OnlookersRunMetres = " + F(OnlookersRunMetres) + "   ; with MindOnlookers on: how far, in metres, an onlooker badly frightened by real danger runs from it (5-60)");
            b.AppendLine("OnlookersRunning = " + OnlookersRunning + "   ; how many onlookers may run from one happening once one does, the fright spreading (1-8; fewer the braver the town, [World] Bravery)");
            b.AppendLine("MindWakes = " + B(MindWakes) + "   ; with MindLive and West World on: the Awakening follows what people have lived through: the uncanny, a killing or a buried man seen walking shake their sense that things are as they seem, so they wake sooner, a good turn steadies them, and the moment that sense gives way they begin to stir; someone who saw a man die and sees him walk again is frightened by it and begins to wake (with MindOnlookers on, backs away from him, or, struck hard, stands rooted); false: the Awakening by its own roll alone");
            b.AppendLine("MindPassing = " + B(MindPassing) + "   ; with MindLive on: when two who know each other pass, the greeting follows how the first feels about the other lately and the day they are having: sore at a friend, a nod and no word; afraid, eyes away; ashamed before them, eyes down; grateful, a warm word; a sore day, a nod; a glad one, a word for anyone they know; and someone who feels something about you from a while back shows it as you pass (grateful, a hat tip and a word; sore, a hard look; afraid or shamed, eyes away); false: the greeting the tie gives");
            b.AppendLine("MindPassingLasts = " + B(MindPassingLasts) + "   ; greetings as you pass remember what you did for hours, not minutes (with MindPassing)");
            b.AppendLine("MindHelps = " + B(MindHelps) + "   ; with MindLive on: when someone takes a fall, the one most moved by it of those who saw it is the one who comes, and how they help comes from what they feel (a hand up, a gruff one, or walking past); a foe still sneers; false: the nearest, by the tie and a roll");
            b.AppendLine("MindRescue = " + B(MindRescue) + "   ; with MindLive on (West World): when someone fully awake begins to say their last words, someone near who is moved by it (a friend, or a kind stranger) may walk over and try to talk them down; a friend is heeded more often; false: nobody steps in");
            b.AppendLine("RescueChance = " + RescueChance + "   ; with MindRescue on: of the despairs where someone near is moved to step in, how often in percent they really do before it is too late (0-100); lower it to see more despair, raise it to see more people saved");
            b.AppendLine("MindMeetings = " + B(MindMeetings) + "   ; with MindLive on: what passes between two people who meet near you leans by the day they are having: two sore people come to words sooner than small talk, two glad ones the other way, and someone lonely stops for a word; false: by their reasons alone");
            b.AppendLine("MindChanceMeetings = " + B(MindChanceMeetings) + "   ; with MindLive on: a chance meeting near you comes from the two people's minds (friends talk and sing, foes quarrel or turn their backs, the sore snap, money worries trade or ask a loan, drink tells tall tales); false: rolled by the place alone");
            b.AppendLine("Language = " + B(Language) + "   ; the AI MODEL (OLLAMA) switch at the top of the settings panel's Essentials page. With MindLive on, the mind's own thoughts, choices and answers are worded by a local language model running on your own PC (Ollama; nothing leaves the machine); every answer is checked for the period, the names and the length, and the written line shows whenever the model's won't do. RealNPCs turns it on by itself when it finds Ollama with the model at a start, unless you have chosen (LanguageChosen)");
            b.AppendLine("LanguageChosen = " + B(LanguageChosen) + "   ; set true by itself whenever you change Language (in the settings panel or in this file), or when Language is on at a start, so a Language you turned off stays off. While false and Language is off, each start asks Ollama at LanguageUrl in the background (a few seconds at most) whether it runs and holds LanguageModel, and if it does, turns Language on, once, with a notice");
            b.AppendLine("LanguageVariety = " + B(LanguageVariety) + "   ; (in testing) with Language on: the model's thoughts vary: each one goes to one thing (their work, someone in sight, a memory, the body, the worry, money, kin, faith, the town's talk, a small thing seen; the weather only now and then) in a shape of its own (fragments, a plain sentence, a question, a reckoning, an opinion, a saying half-remembered), never opening the way their last few did; and nobody doubts the world is real without cause (from the second stage of waking, or after something strange: a buried man seen walking, the night visitor, a death they woke from)");
            b.AppendLine("ModelWakes = " + B(ModelWakes) + "   ; (in testing) with Language, MindWakes and LanguageVariety on: someone with cause who wonders at the world in a thought of their own is shaken a little more by it, so the model's own wondering can start them stirring; off, only what they live through moves them");
            b.AppendLine("LanguageTalk = " + B(LanguageTalk) + "   ; (in testing) with Language on: when you Talk to someone, the model words their answer by their nature, mood and what's on their mind (what it means stays the same: the drink taken, the money refused); if it's slow or says something it shouldn't, the written line");
            b.AppendLine("TalkModelWaitSeconds = " + F(TalkModelWaitSeconds) + "   ; with LanguageTalk on: how long, in seconds, the model may take to word a Talk answer before the written line is said instead (1-7; a request to the model gives up at 8)");
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
            b.AppendLine("LanguageUrl = " + LanguageUrl + "   ; where Ollama listens");
            b.AppendLine("LanguageModel = " + LanguageModel + "   ; the model it holds (ollama pull <name>)");
            b.AppendLine();
            b.AppendLine("[Ticker]");
            b.AppendLine("Ticker = " + B(Ticker) + "   ; a line at the bottom of the screen: what the people near you are thinking, else news");
            b.AppendLine("TickerNearbyMetres = " + F(TickerNearbyMetres) + "   ; people this close count as near you");
            b.AppendLine("TickerSeconds = " + TickerSeconds + "   ; how long each line stays before the next");
            b.AppendLine("TickerY = " + F(TickerY) + "   ; height on screen, 0 = top, 1 = bottom");
            b.AppendLine("TickerScale = " + F(TickerScale) + "   ; text size");
            b.AppendLine("TickerWidth = " + F(TickerWidth) + "   ; share of the screen width it may use");
            b.AppendLine("TickerNextKey = " + TickerNextKey + "   ; shows the next line on the ticker now (on a long line, its next part first); while the bar shows two or more scenes nearby, the next of them");
            b.AppendLine("ThoughtSounds = " + B(ThoughtSounds) + "   ; when a thought of someone within a few steps comes up on the ticker, they make a small wordless sound (at most every 3 minutes each)");
            b.AppendLine("TickerGapSeconds = " + TickerGapSeconds + "   ; a short pause between one thought and the next, so they don't run together (0: none)");
            b.AppendLine("ThoughtMarker = " + B(ThoughtMarker) + "   ; a small gold mark over the person whose thought is on the ticker");
            b.AppendLine("ThoughtsOverHeads = " + B(ThoughtsOverHeads) + "   ; the thoughts of people near you show above their head in small type, not on the bar (the news and the rest stay on the bar; a thought whose thinker is out of view is passed over, never put on the bar)");
            b.AppendLine("ThoughtsOverHeadScale = " + F(ThoughtsOverHeadScale) + "   ; text size of thoughts above heads (TickerScale is the bar's)");
            b.AppendLine("TickerShowsScene = " + B(TickerShowsScene) + "   ; while a street scene plays out within 40 m, the bar at the bottom says what is going on and who is in it (\"Nearby: ...\")");
            b.AppendLine("ScenesOnTicker = " + B(ScenesOnTicker) + "   ; what people say near you (a street scene, a hello, the doctor, the law) shows as they say it in the subtitle box, the game's way (their name, the line); never on the bar; [Output] Subtitles = off hides it");
            b.AppendLine("ScenesOnTickerMetres = " + F(ScenesOnTickerMetres) + "   ; what people say within this many metres shows as a subtitle (5-80)");
            b.AppendLine();
            b.AppendLine("[Display]");
            b.AppendLine("FeelingBars = " + B(FeelingBars) + "   ; bars under people near you who feel something about you: their fear, fondness and anger, as enRAGE keeps them");
            b.AppendLine("FeelingBarsMetres = " + F(FeelingBarsMetres) + "   ; how close they must be (the nearest three in plain view get bars)");
            b.AppendLine("FeelingBarsShowEmpty = " + B(FeelingBarsShowEmpty) + "   ; also show the empty part of each bar, so a low value reads as a little out of a whole bar");
            b.AppendLine("NameCards = " + NameCards + "   ; names over heads in the game's title type, the way enRAGE drew them: off, known (people RealNPCs knows: enRAGE's records and townsfolk, regulars, anyone who thought or spoke or took part in a scene), everyone (the rest get what they look like, \"A townsman\", in grey). While enRAGE runs with its own cards on, ours stay off unless NameCardsBesideEnrage; off on the settings panel turns enRAGE's own cards off too");
            b.AppendLine("NameCardsMetres = " + F(NameCardsMetres) + "   ; strangers' names within (metres); they fade out near the edge");
            b.AppendLine("NameCardsKnownMetres = " + F(NameCardsKnownMetres) + "   ; the names of people you know within (metres)");
            b.AppendLine("NameCardsBesideEnrage = " + B(NameCardsBesideEnrage) + "   ; draw ours even while enRAGE draws its own (two cards over each head enRAGE knows)");
            b.AppendLine("NameCardsAlways = " + B(NameCardsAlways) + "   ; a name over everyone within NameCardsMetres, always: hidden only by walls (not by a table or someone in the way), up to sixteen people, drawn before the Testing overlays; false: only where the view is clear, the ten nearest");
            b.AppendLine("NameCardsStrangerNames = " + B(NameCardsStrangerNames) + "   ; someone you haven't met shows their name with Stranger under it; false: Stranger alone, until you've greeted them or dealt with them");
            b.AppendLine("NameCardsSecondLine = " + B(NameCardsSecondLine) + "   ; the smaller line under each name: their temper and trade, \"Stranger\", their gang, \"Wanted\", a nemesis's mood. Off shows the name alone; NameCards = off hides the cards altogether");
            b.AppendLine("WhosWho = " + B(WhosWho) + "   ; the papers you carry end with Who's who, the people you have met, the most recent first: who they are, how they take you, who they are friends or at odds with (and, with [Mind] MindLive, how they feel about you lately); read only");
            b.AppendLine("GrudgeMeter = " + B(GrudgeMeter) + "   ; top right: strangers whose grudge against you is building, with the mark where they'd become a nemesis, and the nemesis after you");
            b.AppendLine();
            b.AppendLine("[WestWorld]");
            b.AppendLine("WestWorld = " + B(WestWorld) + "   ; master switch: the people who start to see their world is made. false turns off everything in this section");
            b.AppendLine("AwakeningShare = " + F(AwakeningShare) + "   ; percent of people who slowly wake up and see the world for what it is, a part of one too (0.6); 0 turns the Awakening off");
            b.AppendLine("AwakeningDays = " + AwakeningDays + "   ; in-game days a woken person spends at least on each of the first three stages (each day after, a better than even chance of the next)");
            b.AppendLine("AwakeningBreakdown = " + AwakeningBreakdown + "   ; percent of the time an unravelling or awake person breaks down (kneels, shouts at the sky, runs) rather than keeping it together");
            b.AppendLine("AwakeningDespair = " + AwakeningDespair + "   ; percent chance that a fully awake ordinary person, alone, can't bear it and ends their own life, with a gun that fits them, poison, a knife, or by running at a wall (0-100, 0 turns this off; a content note is in the readme)");
            b.AppendLine("WakeVoices = " + B(WakeVoices) + "   ; the waking say the game's own recorded lines in the wrong places: to nobody, the same words again, to the sky, at you; a little at the first stage, a great deal by the fourth, with a thought over the head for what they make of it");
            b.AppendLine("WakeHold = " + B(WakeHold) + "   ; from the second stage, a waking person near you goes from one strange act into the next instead of going back to their day: stood dazed, drifting a few steps, pacing, freezing; let go for a fight, gunfire, the law after you, or a scene");
            b.AppendLine("WakeHoldMinutes = " + WakeHoldMinutes + "   ; how long one hold lasts before they are let go for a minute (1 to 15)");
            b.AppendLine("WakeGestures = " + B(WakeGestures) + "   ; the waking use their hands and bodies as they act: rubbing their eyes, pointing at the sky, a hand to the head, pleading; a clip that won't play or carries them off is dropped for the session (the log says which)");
            b.AppendLine("WakeStrange = " + B(WakeStrange) + "   ; from the third stage, the waking do strange things to the people around them: right up close and staring, a hand waved before a face, following someone step for step, telling them they aren't real, preaching to whoever is about; the other walks off, curses, laughs or takes fright");
            b.AppendLine("WakeStripChance = " + WakeStripChance + "   ; percent of the fully awake who, once, take every stitch off where they stand (never enRAGE's people: it knows them by their clothes); 0 for never; held back since 0.7.9, whatever this says: the first strip froze the game");
            b.AppendLine("WakeLashOut = " + WakeLashOut + "   ; percent of holds at the third and fourth stages that end with them turning on you or on someone near: fists while unravelling, their own gun when awake and carrying one; hotheads and the proud more often, cowards less. Coming at you or at someone else is fair game to stop; 0 for never");
            b.AppendLine("HostsQuestionReality = " + B(HostsQuestionReality) + "   ; people sometimes wonder whether their world is real");
            b.AppendLine("RealityChance = " + RealityChance + "   ; percent of remarks and thoughts that drift that way");
            b.AppendLine("Glitches = " + B(Glitches) + "   ; rarely, someone freezes or repeats themselves");
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
            b.AppendLine("Hermits = " + B(Hermits) + "   ; West World: people who woke and walked out of their towns live rough in the wilds, and keep their camps");
            b.AppendLine("HermitChance = " + HermitChance + "   ; percent on each roll that a new hermit's camp turns up near you in the wilds");
            b.AppendLine("Gatherings = " + B(Gatherings) + "   ; West World: the awake stand in a silent circle, facing in: from midnight to half past three out in the wilds (once a night), and with GatheringHours any by day too, in an open field or just outside a town's edge");
            b.AppendLine("GatheringChance = " + GatheringChance + "   ; percent on each encounter roll while one may happen");
            b.AppendLine("GatheringHours = " + GatheringHours + "   ; any: at any hour (the night ones as they were, and by day in the open or at a town's edge); night: only from midnight to half past three in the wilds, as before");
            b.AppendLine("GatheringGapHours = " + GatheringGapHours + "   ; game hours at least between one gathering and the next, so they don't come one on another (1-72; a game hour is about two real minutes)");
            b.AppendLine("GatheringHostile = " + GatheringHostile + "   ; percent of the gatherings of the awake that turn on you, bare-handed, all at once, when you come within 10 m of them; fighting them off is self-defence (0 to 100; 50 as it comes; 0: they never attack)");
            b.AppendLine();
            b.AppendLine("[World]");
            b.AppendLine("Bravery = " + Bravery + "   ; how brave the townsfolk are, 0-100: in a fistfight nobody runs, they gather and watch; with guns most hold their ground and the odd coward still runs; the mod's own frights (an onlooker hurrying off, a fright that spreads, a man a gun is drawn on) mostly stand back and watch instead. Someone you aim at or lock on to is left as the game has them, so hold-ups and robbery work as before. Never lawmen, story characters, mission people or enRAGE's. 0: as the game has it");
            b.AppendLine("Storyteller = " + Storyteller + "   ; how lively the world is around you: the storyteller notices how long it has been since something worth stopping for happened near you (a fight, a theft, a standoff, a song, someone coming up to you, an encounter on the road) and, when it has been quiet too long, shortens the waits before the next scene, chance meeting, walk-up and encounter roll; after something big it waits a little longer for half a minute or so. It never starts anything itself and never stops anything. calm (a quieter world than before), lively, wild; off: as before. In a mission, wanted, in a fight or indoors it holds everything as built");
            b.AppendLine("Deeds = " + B(Deeds) + "   ; people react to where you go and what you do: shops, church, hunting, killing, stealing");
            b.AppendLine("SelfDefenceNoBounty = " + B(SelfDefenceNoBounty) + "   ; killing or hurting someone who came at you (soldiers too) doesn't raise your bounty or make you wanted: RealNPCs puts it back, unless you also hurt someone who didn't, or did another crime");
            b.AppendLine("TownPosts = " + B(TownPosts) + "   ; the shopkeepers, barkeeps, gunsmiths, doctors and station agents are each one person for good, who knows you; when one dies, a relative or a newcomer takes the counter");
            b.AppendLine("TownPostsAfterDeath = " + TownPostsAfterDeath + "   ; survive: the game brings a shot-down counter keeper back bandaged, so they lived and remember it; successor: they died, and a relative or a newcomer takes the counter");
            b.AppendLine("Regulars = " + B(Regulars) + "   ; each town keeps its own ordinary people: the same names, faces and lives every time you're there, each going through something of their own");
            b.AppendLine("RegularsPerTown = " + RegularsPerTown + "   ; how many regulars a town keeps (5-80)");
            b.AppendLine("RegularsDress = " + B(RegularsDress) + "   ; regulars wear their own clothes each time you meet them (put on while nobody can see); off: they keep their names and lives, not their looks");
            b.AppendLine("RumoursTravel = " + B(RumoursTravel) + "   ; word of your deeds travels town to town and grows in the telling");
            b.AppendLine("Newspaper = " + B(Newspaper) + "   ; your deeds and the town talk turn up in enRAGE's newspapers");
            b.AppendLine("Letters = " + B(Letters) + "   ; letters wait for you at post offices (read them with ReaderKey)");
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
            b.AppendLine("PersonalSpace = " + B(PersonalSpace) + "   ; stand right up against someone (under a metre, closer in a saloon) for a couple of seconds and they mind it as their nature says: a step back and a word, a look, or, from the hot-tempered, a word to back off; never the law, anyone sitting or at the bar, or anyone busy (needs [Scenes] SightReactions)");
            b.AppendLine("SpaceMetres = " + F(SpaceMetres) + "   ; with PersonalSpace on: standing nearer than this to someone, in metres, is too close (0.5-2)");
            b.AppendLine("SpaceSaloonMetres = " + F(SpaceSaloonMetres) + "   ; the same at a saloon's bar, shoulder to shoulder (0.4-2)");
            b.AppendLine("SpaceSeconds = " + F(SpaceSeconds) + "   ; how long, in seconds, you both stand still that close before they mind it (0.5-10)");
            b.AppendLine("ArthurSpeaks = " + B(ArthurSpeaks) + "   ; Arthur says one of his own recorded fighting lines when someone comes at him or when he catches the pickpocket himself; never over a line already playing on him, 20 s between");
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
            b.AppendLine("ShoutsCarry = " + B(ShoutsCarry) + "   ; what's shouted is shouted (anger, threats, fright, a cry for the law, fight talk, cheering, calling out): the game lets a shout carry much farther than talk, so you hear it from down the street; ordinary talk is as before");
            b.AppendLine("RealVoiceTalk = " + B(RealVoiceTalk) + "   ; now and then two people standing together near you have a word in their real recorded voices: the weather, how they're getting on, a piece of news, a hello");
            b.AppendLine("RealVoiceTalkSeconds = " + RealVoiceTalkSeconds + "   ; about how often (seconds, 30 to 1800); each person then rests five minutes, and the pair fifteen");
            b.AppendLine("EnragePeopleJoinIn = " + B(EnragePeopleJoinIn) + "   ; enRAGE's own people (its nemeses, gang men and named folk) take part in street scenes, reactions, talk and waking acts like anyone else, unless enRAGE has them busy (a fight, a job, a ride)");
            b.AppendLine("Reactions = " + B(Reactions) + "   ; street scenes: the one spoken to reacts to the words by their nature (bristles, flinches, shakes their head, nods), and a man whose face goes into the bar reels");
            b.AppendLine("Grudges = " + B(Grudges) + "   ; grudges that travel: a friend of someone wronged has it out with the one who did it; and someone whose friend you killed may come up and say so to your face (never violent)");
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
            b.AppendLine("LateThanks = " + B(LateThanks) + "   ; people you were good to through the Talk menu (a drink, money, a debt paid, the doctor, a dentist or the fare paid for) may seek you out when they next see you, some hours or a few days on, and say what it meant to them (few for a drink, most for money, nearly all for a trouble mended); the proud pay back a debt they called a loan, days later; the cold, the seated and women after dark just nod");
            b.AppendLine("ThanksAfterHours = " + ThanksAfterHours + "   ; with LateThanks on: game hours at least between a kindness and their thanks (0-72)");
            b.AppendLine("ThanksWithinDays = " + ThanksWithinDays + "   ; game days at most (1-14; the world remembers its last 400 happenings, so a drink may be forgotten sooner in a busy town)");
            b.AppendLine("ThanksDrinkChance = " + ThanksDrinkChance + "   ; percent who come to thank you for a drink you stood them (0-100)");
            b.AppendLine("ThanksDollarChance = " + ThanksDollarChance + "   ; ... for a dollar (0-100)");
            b.AppendLine("ThanksFiveChance = " + ThanksFiveChance + "   ; ... for five dollars (0-100)");
            b.AppendLine("ThanksMendedChance = " + ThanksMendedChance + "   ; ... for a trouble you mended with money: a debt, the doctor, the dentist, the fare (0-100)");
            b.AppendLine("ThanksLoanDays = " + ThanksLoanDays + "   ; a proud one who took your money for their debt as a loan pays it back after this many game days (0-14)");
            b.AppendLine("DoctorsPatient = " + B(DoctorsPatient) + "   ; by day in a town with its doctor (Valentine, Rhodes, Saint Denis), someone near you walks to him for what ails them (a child's fever, a tooth, ribs after a fight, a back, a cough...), tells him, hears his word, and walks home thinking it over");
            b.AppendLine("ErrandPace = " + F(ErrandPace) + "   ; how fast people walk on their errands here (to a scene, to the doctor, to a talk, to a camp seat, home again): 1 an ordinary walk, 2 brisk, 3 a run");
            b.AppendLine("LawChases = " + B(LawChases) + "   ; when a pickpocket runs and a lawman is near, the lawman gives chase, brings him down and holds him for arrest");
            b.AppendLine("LawShootsThieves = " + LawShootsThieves + "   ; with LawChases: percent of the times a lawman near draws and shoots the one who picked your pocket instead of running him down (0 to 100; 50 as it comes); your money comes back off him either way");
            b.AppendLine("Lifts = " + B(Lifts) + "   ; a man may bump into you anywhere and lift a little of your cash (a fifth, $1 to $25), then run: catch him, or let a lawman, to get it back");
            b.AppendLine("Vices = " + B(Vices) + "   ; habits and vices: someone in a saloon whose trouble ended badly today, or who has just heard a friend has died, drinks the night away (and a friend may sit with them); drunk for the rest of the night");
            b.AppendLine("Kindnesses = " + B(Kindnesses) + "   ; small kindnesses between people: someone who trips in the street is helped up by a friend or a kind passer-by, or left lying by the cold and their enemies (and you can give them a hand up yourself); someone may step in on a bully, who backs off, goes on, or shoves the one who stepped in and storms off; remembered either way");
            b.AppendLine("SightReactions = " + B(SightReactions) + "   ; people near you notice how you look (bloodied, drunk, filthy, soaked) and take it by their nature: the timid afraid, the hard sizing you up, the kind concerned; and a man walking off bloodied from a street fight gets looked at");
            b.AppendLine("Acknowledge = " + B(Acknowledge) + "   ; two people who know each other nod, tip a hat or wave as they pass (enemies give a hard look, sweethearts a long one), and each thinks something of it; a pair once in 45 game minutes");
            b.AppendLine("TroubleWeight = " + F(TroubleWeight) + "   ; how often people come to blows or quarrel: 1 as built, 2 twice as often, 0.5 half, 0 never by chance (0-5); it scales the brawl's and the argument's share of a chance meeting, hot blood between strangers, drink and temper, and a snap that comes to blows; over 1 it also shortens everyone's rest between scenes (2 min, 15 after blows), by half at most");
            b.AppendLine("ReasonVariety = " + B(ReasonVariety) + "   ; the street's scenes take turns: someone at the worst of a worry says so once while you're about, not to everyone they meet, the same kind of scene three times running is less likely, and gossip, theft, drunks and courting get their turns over a sharp word");
            b.AppendLine("ChanceFirst = " + B(ChanceFirst) + "   ; chance meetings (a song, a tall tale, haggling, a standoff, a card cheat) get their turn when nobody has a strong reason, nearer you; a high TroubleWeight brings more fights, not more arguments; two people sat far apart are not picked for a scene they would never walk to");
            b.AppendLine("HorseThieves = " + B(HorseThieves) + "   ; with MindLive on, in town: a man honest with nobody and short of money (or a known thief) gets up on a horse that isn't his and rides off, a red dot and fair game; its owner shouts after him. At most once a town in two game hours");
            b.AppendLine("FightCrowds = " + B(FightCrowds) + "   ; with MindLive on: while people trade blows, those within [Mind] BrawlCrowdMetres who aren't of a fearful nature walk over, stand round in a ring and cheer it on (up to 6 round two men, 16 round a brawl, in two rings); they go when it's over");
            b.AppendLine("NightScenes = " + B(NightScenes) + "   ; with MindLive on, at night in town: someone short of money and honest with nobody (or a known thief) robs someone on their own, who hands it over or won't by their nature; and late, a drunk who goes down in the street is got up and taken home by a friend. Each at most once a town a night");
            b.AppendLine("Aftermath = " + B(Aftermath) + "   ; with MindLive on: after a street fight near you, a friend of the one who went down (or a kind soul who saw it) goes to see to him, and a lawman near has a word with the one left standing");
            b.AppendLine("ChanceMeetMetres = " + F(ChanceMeetMetres) + "   ; two people up to this far apart may meet by chance near you, one walking over to the other (2-12; 3.4 was standing together only)");
            b.AppendLine("ScenesAtOnce = " + ScenesAtOnce + "   ; street scenes playing at once (1-6): one scene, and the rest quiet talks between other people elsewhere on the street (words only, never blows); 1: just the scene");
            b.AppendLine("EventfulOnly = " + B(EventfulOnly) + "   ; street scenes and chance meetings of nothing but talk (small talk, gossip, a song, a haggle, a tall tale, the paper read out) are passed over, and so are the quiet talks beside a scene: the street keeps its quarrels, fights, thefts, flirts, debts and troubles; false: the talk too, as before");
            b.AppendLine("MainScenesAtOnce = " + MainScenesAtOnce + "   ; full street scenes at once (1-3; two at most play for now): a second scene with its own words and walk-overs at least 20 m from the first, never the same people (fights, rough handling and thefts only ever in the first); 1: a single scene; the quiet talks of ScenesAtOnce come on top");
            b.AppendLine("GangShootouts = " + B(GangShootouts) + "   ; men of two rival gangs who meet within 90 m of you may fight it out (more often where enRAGE reports a war between them): words, a standoff, then a shootout until a side is beaten or gone; at most one in five minutes");
            b.AppendLine("WomenInFights = " + B(WomenInFights) + "   ; women may come to blows in a quarrel too (with a man or another woman), half as often as two men for each woman in it; false: only two men ever fight");
            b.AppendLine("RoughHandling = " + B(RoughHandling) + "   ; street scenes: a man with a grudge (or a hothead with drink in him) grabs another and smashes his face into the nearest table, bar or wall, with the game's own move; with nothing to hand it stays a fistfight");
            b.AppendLine("NpcThieves = " + B(NpcThieves) + "   ; street scenes: someone short of money or light-fingered picks a well-off or drunk mark's pocket; mostly the mark never knows who");
            b.AppendLine("DrunkChallenges = " + B(DrunkChallenges) + "   ; street scenes: a drunk in a saloon at night calls a man out and falls on his face before a punch is thrown");
            b.AppendLine("CrudeTalk = " + B(CrudeTalk) + "   ; in those conversations, the game's own crude remarks: a man to a woman in a saloon of an evening (she tells him off), or between sweethearts at night");
            b.AppendLine("SceneReactions = " + B(SceneReactions) + "   ; greet or antagonize someone in a scene: take a side, break up a fight, or stir it up");
            b.AppendLine("Standoffs = " + B(Standoffs) + "   ; two men square off in the street (guns come out only away from the law)");
            b.AppendLine("Songs = " + B(Songs) + "   ; drunks sing old songs in saloons and at night");
            b.AppendLine("TallTales = " + B(TallTales) + "   ; old-timers tell tall tales, sometimes about you");
            b.AppendLine("NewspaperTalk = " + B(NewspaperTalk) + "   ; somebody reads the paper aloud, your deeds included");
            b.AppendLine("Haggling = " + B(Haggling) + "   ; horse trading and haggling in the street");
            b.AppendLine("SnakeOil = " + B(SnakeOil) + "   ; a snake-oil man works the crowd");
            b.AppendLine("SmallTalk = " + B(SmallTalk) + "   ; neighbours pass the time of day");
            b.AppendLine("Pickpockets = " + B(Pickpockets) + "   ; a pickpocket strikes near you: run him down and the town hears of it");
            b.AppendLine("PosseTalk = " + B(PosseTalk) + "   ; after a bad crime in town, men talk about getting up a posse");
            b.AppendLine("Mourners = " + B(Mourners) + "   ; the widow (or brother) of a stranger you killed grieves in town");
            b.AppendLine("Pleading = " + B(Pleading) + "   ; hold a gun on a stranger and they beg; lower it or don't");
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
            b.AppendLine("NewsTo = " + NewsTo + "   ; where news, rumours and what people elsewhere think of you go: corner (the \"Around town\" box, top right), ticker, or gamefeed (the game's own updates, top left)");
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
