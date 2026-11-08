// The Testing page's catalogue (overnight 2026-10-27, Tier 1.2): every interaction and encounter RealNPCs can fire
// on demand, with its label, its group on the page, the ini key that fires it ([Testing], None by default) and what
// the player has to do or where he has to be first. The game side (WestworldScript.Testing.cs) dispatches each id to
// the feature's own start path; a test may skip the time, place and chance rolls, never the safety rules.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class TestActions
    {
        public sealed class Entry
        {
            public string Id, Label, Group, KeySetting, DefaultKey, Needs;
            public bool Field;   // the key is one of Settings' own fields (StalkerKey, WakeKey...), else Settings.TestKeyMap

            public Entry(string id, string group, string label, string keySetting, string defaultKey = "None", string needs = null, bool field = false)
            {
                Id = id;
                Group = group;
                Label = label;
                KeySetting = keySetting;
                DefaultKey = defaultKey;
                Needs = needs;
                Field = field;
            }
        }

        public static readonly string[] Groups = { "Encounters", "People", "Street scenes", "Tools" };

        public static readonly List<Entry> All = new List<Entry>
        {
            // ---- encounters (the encounters' tick starts them once nothing is in the way) ------------------------
            new Entry("crying", "Encounters", "The crying woman", "CryingWomanKey", field: true),
            new Entry("execution", "Encounters", "An execution on the road", "ExecutionKey", "[", needs: "a town with regulars", field: true),
            new Entry("hermit", "Encounters", "A hermit's camp", "HermitKey", needs: "outside a town; the camp stays", field: true),
            new Entry("traveller", "Encounters", "A traveller asking the way", "TestTravellerKey", needs: "out of the towns; best on a road"),
            new Entry("supper", "Encounters", "The supper invitation (a test takes no money)", "TestSupperKey", needs: "out of the towns, the ticker on; best on a road; takes no money and leaves the clock alone"),
            new Entry("wagonscene", "Encounters", "A wagon ahead loses a wheel (the scene)", "TestWagonSceneKey", needs: "out of the towns on a road, an ambient wagon with a driver 40-150 m off and out of view, moving (the row takes any hour and any wagon not in view); stop on foot within 3 m of it to set the wheel, or ride on"),
            new Entry("campfire", "Encounters", "Strangers at a campfire", "TestCampfireKey", needs: "out of the towns; a test takes turns with and without the traveller who walks in"),
            new Entry("callout2", "Encounters", "Someone calls out another before the street", "TestCallout2Key", needs: "a town's street, two named people on their feet 2-18 m apart with something between them (a tie gone sour: a quarrel, a beating, a theft; or a debt), one in your view"),
            new Entry("domestic", "Encounters", "A husband loses his temper outside his home", "TestDomesticKey", needs: "a town's street, a named man on his feet 6-35 m from you (his sweetheart near plays his wife, else his wife walks up from out of sight)"),
            new Entry("hogtied", "Encounters", "Stage a hogtied captive nearby", "TestHogtiedKey", needs: "out of the towns, a town with regulars; a mark on the map; greet the men's leader to pay when he names a price (a test takes no money)"),
            new Entry("callyou", "Encounters", "Someone with a grudge calls you out", "TestCallYouKey", needs: "a town's street, someone named on their feet 4-40 m from you with something against you (you struck or robbed them, they saw you kill, word of your thieving, or you soured them)"),
            new Entry("lift", "Encounters", "A man bumps into you and lifts your money", "TestLiftYouKey", needs: "a man near you (4-22 m) free to send, $2 or more in your pocket; he walks into you, takes a fifth of it ($1-25) and runs: catch him to get it back"),
            new Entry("visitor", "Encounters", "The night visitor", "TestVisitorKey", needs: "in a town, out of doors, no lawman within 60 m"),
            new Entry("gathering", "Encounters", "A gathering of the awake", "GatheringKey", field: true),
            new Entry("stalker", "Encounters", "A stalker now", "StalkerKey", "]", needs: "outdoors", field: true),
            new Entry("stalkerhorse", "Encounters", "A stalker on horseback now", "TestStalkerHorseKey", needs: "outdoors; [Stalkers] StalkerRides for him to ride (stand still to bring him in, move to send him off)"),
            new Entry("stalkeroutfit", "Encounters", "Stalker outfit: show the next candidate", "TestStalkerOutfitKey", needs: "out of the saddle; a man stands 4 m in front of you in the next model and preset (every preset of one model, then the next), the one before goes; the log and the hint give the two ini lines for [Stalkers] StalkerBlackModel and StalkerBlackOutfit"),
            // ---- people --------------------------------------------------------------------------------------------
            new Entry("wake", "People", "Wake the person you face one stage", "WakeKey", "Y", needs: "face someone a few steps off", field: true),
            new Entry("realtalk", "People", "Two people talk in their real voices", "TestTalkKey", "Shift+F7", needs: "two people within 20 m"),
            new Entry("rough", "People", "A face into the bar (rough handling)", "TestRoughKey", "Shift+F9", needs: "two men within 10 m, best in a saloon"),
            new Entry("font", "People", "The subtitle line in each font", "TestFontKey", "Shift+F2"),
            new Entry("plea", "People", "A plea at gunpoint (then aim at someone)", "TestPleaKey", needs: "aim at someone within 30 m in the next minute; lowering your gun counts as real mercy"),
            new Entry("arrival", "People", "A word as you ride into town", "TestArrivalKey", needs: "in a town"),
            new Entry("letter", "People", "A letter (the waiting ones, else a test one)", "TestLetterKey"),
            new Entry("trial", "People", "The circuit judge (a regular held; again: starts in 2 min)", "TestTrialKey", needs: "[World] TownEvents, EventTrials; a town with a living man among its regulars (the dead man is made up); the row again moves the trial, then the hanging, to two minutes from now"),
            new Entry("prizefight", "People", "A prize fight out back (in 2 min here)", "TestPrizeFightKey", needs: "[World] TownEvents, EventPrizeFights; a town with two of its regulars of a fighting temper (men, 18-48); you've been out of its saloon for the spot behind it"),
            new Entry("revival", "People", "A camp meeting, night one (in 2 min here)", "TestRevivalKey", needs: "[World] TownEvents, EventRevivals; a town (its showground or church steps spot, else a spot near you)"),
            new Entry("revival3", "People", "A camp meeting, the altar call (in 2 min here)", "TestRevival3Key", needs: "[World] TownEvents, EventRevivals; a town with a man whose worry is the drink or the cards, for the bench"),
            new Entry("baptism", "People", "A baptism at the river (in 2 min here)", "TestBaptismKey", needs: "[World] TownEvents, EventBaptisms; a town with a riverbank spot recorded within 150 m, and a man of it known to RealNPCs"),
            new Entry("boxsocial", "People", "A box social (in 2 min here)", "TestBoxSocialKey", needs: "[World] TownEvents, EventBoxSocials; a town with a young woman and two young men among its regulars"),
            new Entry("barnraising", "People", "A barn raising (in 2 min here)", "TestBarnRaisingKey", needs: "[World] TownEvents, EventBarnRaisings; a town with a farm family known to RealNPCs (two men at odds for the beam, if any)"),
            new Entry("baseball", "People", "Base ball (in 2 min, Valentine or Strawberry)", "TestBaseBallKey", needs: "[World] TownEvents, EventBaseBall; in Valentine or Strawberry to see it staged"),
            new Entry("shivaree", "People", "A shivaree (in 2 min here)", "TestShivareeKey", needs: "[World] TownEvents, EventShivarees; a town with a man and a woman among its regulars to stand for the couple"),
            new Entry("marks", "Tools", "A beating's marks: try the next gait and bruising", "TestMarksKey", needs: "a named person within 15 m (each press the next pair; the log says which)"),
            new Entry("fence", "People", "Fence cutters: a wire found cut here (now)", "TestFenceKey", needs: "[World] FenceCutters; a town with two men known to RealNPCs (the row counts you as near last night)"),
            new Entry("saleday", "People", "Sale day at the stockyards (now)", "TestSaleDayKey", needs: "[World] SaleDay; Valentine, the stockyards spot recorded and within 80 m, four named men about it"),
            new Entry("novel", "People", "Publish a dime novel of your deeds (now)", "TestNovelKey", needs: "[World] DimeNovels; your most-told deed if one has travelled, else a test deed (not kept as yours)"),
            new Entry("fever", "People", "A fever starts in this town (the nearest resident first)", "TestFeverKey", needs: "[World] Fever; a town, a resident within 20 m; it spreads at each day's tick"),
            new Entry("quilt", "People", "Overhear the quilting bee (now)", "TestQuiltKey", needs: "[Scenes] QuiltingBee; on foot within 25 m of a town's church steps spot (recorded with the spots tool)"),
            new Entry("foreclosure", "People", "A sheriff's sale in the street (now)", "TestForeclosureKey", needs: "[World] Foreclosures; a town with its bankdoor or mainstreet spot recorded and within 80 m, a named resident near for the family and three others about"),
            new Entry("slate", "People", "Someone asks for credit at the store (now)", "TestSlateKey", needs: "[Scenes] StoreSlate; the store's keeper within 25 m of you, and a named person within 18 m of him (refused, so J shows)"),
            new Entry("drummer", "People", "A drummer calls at the store (now)", "TestDrummerKey", needs: "[World] Drummers; a town's store keeper within 60 m of you, and a named man near to stand for the drummer"),
            new Entry("shapeup", "People", "The shape-up at the wharf (now)", "TestShapeUpKey", needs: "[World] ShapeUp; Saint Denis, the wharf spot recorded and within 80 m, three named men about it"),
            new Entry("mine", "People", "Three whistles at the mine (now)", "TestMineKey", needs: "[World] MineWhistle; men of Annesburg known to RealNPCs (two die); in Annesburg to see it staged"),
            new Entry("pinkerton", "People", "A Pinkerton asks the town after you (now)", "TestPinkertonKey", needs: "[Encounters] Pinkertons; a town, with one of its people the mind knows within 50 m"),
            new Entry("show", "People", "The Wild West show (posted here; again: starts in 2 min)", "TestShowKey", needs: "[World] TownEvents, EventShows; a town; your most-told deed if one has travelled, else a test legend"),
            new Entry("musing", "People", "A stranger says something strange", "TestMusingKey", needs: "someone 3-14 m off"),
            new Entry("glitch", "People", "A stranger glitches", "TestGlitchKey", needs: "someone 3-16 m off"),
            new Entry("disbelief", "People", "Disbelief, voiced, over someone's head", "TestDisbeliefKey", needs: "a named person within 15 m"),
            new Entry("wakevoices", "People", "Wake voices: every line, one by one", "TestWakeVoicesKey", needs: "someone within 12 m"),
            new Entry("despairnow", "People", "Despair now, the next way in turn", "TestDespairNowKey", needs: "someone RealNPCs knows, 3-12 m away"),
            new Entry("despairanims", "People", "Despair animations, one by one", "TestDespairAnimsKey", needs: "someone free within 12 m"),
            new Entry("stageanims", "People", "Wake stage gestures, one by one", "TestStageAnimsKey", needs: "someone free within 12 m"),
            new Entry("wakestrange", "People", "Strange waking acts, the next in turn", "TestWakeStrangeKey", needs: "West World on, someone unravelling or awake (stage 3+) within 15 m, someone else near them (stripping: someone awake). Order: close up, wave, follow, not real, preach, lash out at you, lash out at another, strip, dazed"),
            new Entry("mindscene", "People", "A mind starts a scene (their own reason)", "TestMindSceneKey", needs: "named people near with a reason between them (a quarrel, a friend grieving, a debt); [Mind] Language and LanguagePicks for the model to pick, else their strongest reason plays"),
            new Entry("mindtalk", "People", "Two people talk of what's on their minds", "TestMindTalkKey", needs: "[Mind] MindTalks; two named people within 24 m, free, a few steps apart (the nearest two); one brings up a worry, the times, the stranger, a plan, the town's talk, a need or the day, and the other answers by their nature; with [Mind] Language and LanguageTalk the answer in the model's words"),
            new Entry("timestalk", "People", "Two people talk over the times", "TestTimesTalkKey", needs: "[Mind] MindTalks and TimesTalk, [World] HistoricalAccuracy; two named people within 24 m, free, a few steps apart (the nearest two); one brings up the news of the day or a topic of the year, the other answers by their nature, often a second round"),
            new Entry("talk", "People", "Talk to the nearest person you know", "TestMenuKey", needs: "an adult RealNPCs knows within 6 m, not in a scene or a fight"),
            new Entry("allies", "People", "An armed man comes at you; a friend comes to your aid", "TestAlliesKey", needs: "[Encounters] AlliesHelp; a named adult within AlliesMetres, free (the nearest counts as your friend for the test); out of doors, no mission. A revolver man is made 14-20 m off as soon as the game has his body streamed in (a moment) and comes at you; fighting him is self-defence"),
            new Entry("escort", "People", "Walk someone home (Talk on the nearest person you know)", "TestEscortKey", needs: "[Encounters] FondnessActs; an adult RealNPCs knows within 6 m, free, on their feet; the hour and the town are skipped, the walk is real: keep up with them to their door 60-100 m off"),
            new Entry("space", "People", "Someone minds you standing so close", "TestSpaceKey", needs: "someone within 3 m, standing, not the law, not sitting or at the bar, not busy"),
            new Entry("lonely", "People", "A lonely regular comes over", "TestLonelyKey", needs: "MindLive, someone the mind knows within 18 m and in view, standing, who neither fears you nor holds anything against you"),
            new Entry("favour", "People", "Someone in trouble comes to ask a favour", "TestFavourKey", needs: "MindLive; someone the mind knows within 18 m, standing, in view"),
            new Entry("warnyou", "People", "Someone warns you, or tells you to your face what they saw", "TestWarnYouKey", needs: "MindLive; someone the mind knows within 18 m, in view"),
            new Entry("thanks", "People", "Someone you helped thanks you", "TestThanksKey", needs: "someone within 20 m who can see you, whom you stood a drink, gave money or settled a debt for this week (the Talk menu; Greedy people don't come, Cold ones nod)"),
            new Entry("friendcall", "People", "A friend comes over (a warning, news, a gift, a favour)", "TestFriendCallKey", needs: "[Scenes] FriendCalls; a friend of yours within 20 m who can see you, free and standing (the friend dot's rule: a full bar, a kindness remembered, a trouble mended, someone you saved); they come at once with what they'd bring (a warning, the news, a gift or their trade's favour); a test hands nothing over (Run 3, item 7)"),
            new Entry("lawbody", "People", "A lawman clears a body", "TestLawBodyKey", needs: "a body within 40 m and a lawman on foot within 80 m of it, no fight near"),
            new Entry("arthur", "People", "Arthur's own line", "TestArthurKey", needs: "nothing: each press tries the next moment's lines (someone came at him, the pickpocket caught, a gunfight)"),
            new Entry("almanac", "People", "The almanac: the day's news and a paper to read", "TestAlmanacKey", needs: "[World] HistoricalAccuracy and Almanac; the clock's date (F7 \"TEST almanac:\" shows it and what is known today). Pushes the freshest dated thing onto the ticker as \"In the paper\" and puts the last fortnight's paper in the reader; the log line names the freshest thing and how many are known"),
            new Entry("wagon", "People", "A wagon's wheel off (the probe: one way a press)", "TestWagonKey", needs: "an ambient wagon with a driver within 150 m and out of view; best on a road out of town, standing still yourself. Each press tries the next way of breaking the wheel (five in all); a way that does nothing ends in 12 s, one that works is watched a minute; the answers in the log, a notice and F7"),
            new Entry("gunshot", "People", "Gunshots at the ground, six ways (which can you hear?)", "TestGunshotKey", needs: "someone free 2-12 m off; best out of town (people near may run, and the law may come)"),
            new Entry("fall", "People", "Someone takes a fall (and is helped up, or not)", "TestFallKey", needs: "two named people outdoors, one of them within 14 m and in view"),
            new Entry("preacher", "People", "A street preacher (Sunday afternoons)", "TestPreacherKey", needs: "a town street, a devout named person and another within 8 m"),
            new Entry("olddays", "People", "Two old men argue about the old days", "TestOldDaysKey", needs: "two named old men within 10 m"),
            new Entry("below", "People", "The voice below the saloon floor (West World)", "TestBelowKey", needs: "West World on, in a saloon with the town's barkeep within 14 m; then stand by the bar"),
            new Entry("bell", "People", "The bell at three (only the awake hear it)", "TestBellKey", needs: "West World on, out of doors in a town with a church, someone awake (stage 2+) within 30 m in view"),
            new Entry("sabbath", "People", "Someone kneels by the church (Sunday mornings)", "TestSabbathKey", needs: "within 60 m of a town's church, a named person within 25 m in view"),
            new Entry("mind:request", "People", "The language request for the nearest known person (log)", "TestMindRequestKey", needs: "[Mind] MindShadow on, someone RealNPCs knows within 20 m; nothing is asked of any model"),
            new Entry("mind:ask", "People", "Ask the local model for the nearest known person's thought", "TestMindAskKey", needs: "[Mind] MindShadow on, Ollama running with the model named in [Mind] LanguageModel, someone RealNPCs knows within 20 m; the answer comes to the log (LANGUAGE lines) a moment later; works with [Mind] Language off"),
            new Entry("events:probe", "Tools", "Log every game event for 30 s (to find the shove's)", "TestEventProbeKey", needs: "nothing; each event id the game raises, with its count and its first words, goes to the log as EVENT probe lines (2026-10-29: a shove of yours raised no damage event, so the mind never saw it)"),
            new Entry("looks:probe", "Tools", "Read the looks of the people near you", "TestLooksProbeKey", needs: "the recognition plan's Stage 2 (Run 3, item 5), the only read of anyone's clothes there is: up to six people within 40 m, nearest first, each once a session at a depth; [World] LooksReadDepth sets how deep (1 counts the pieces, 2 reads them, 3 their tints, 4 their kinds); refused while the world is quiet (a load, a fade, a death, a mission, the controls taken) and 5-10 s after, with why; LOOKS probe lines in the log, the body being read in lastread.txt"),
            new Entry("translation:template", "Tools", "Write the translation template", "TestTranslationTemplateKey", needs: "nothing; every line RealNPCs has goes to translation-template.txt in the RealNPCs folder, one a line with a TAB after it: fill in the translations after the TABs and save it as translation.txt there ([Output] Translate) (Run 3, item 8)"),
            new Entry("patient", "People", "Someone walks to the doctor and tells him what ails them", "TestPatientKey", needs: "a town with its doctor within 90 m (Valentine, Rhodes or Saint Denis); a named person within 16 m in view, free; they say what's wrong, he answers, they walk home"),
            new Entry("lostchild", "People", "A mother asks around for her lost child", "TestLostChildKey", needs: "a town street, a named woman within 25 m in view (within 7 m she asks you)"),
            new Entry("callout", "People", "A man calls another out at noon", "TestCalloutKey", needs: "a town street, two named men with bad blood 2-14 m apart"),
            new Entry("grudge", "People", "A friend takes up a wrong", "TestGrudgeKey", needs: "a friend of someone wronged in the last 3 days, near the one who did it"),
            new Entry("kingrudge", "People", "Kin come about a wrong (the nearest known resident's)", "TestKinGrudgeKey", needs: "[Scenes] KinGrudges; a town; a known resident within 20 m with a man among their kin"),
            new Entry("grudge:you", "People", "Someone grieving one you killed", "TestGrudgeYouKey", needs: "in town, someone within 20 m whose friend you killed this week"),
            new Entry("kintown", "People", "Kin come for a townsman who beat theirs (now)", "TestKinTownKey", needs: "[Scenes] KinGrudges, KinTownWrongs; a known resident within 30 m with a man among their kin, and another named man near"),
            new Entry("kin", "People", "Kin of someone you know walks up", "TestKinKey", needs: "in a town where someone you have dealt with has kin, and a passer-by in view within 18 m, unnamed, whom RealNPCs may direct"),
            new Entry("drink", "People", "Someone drinks away bad news", "TestDrinkKey", needs: "a saloon, a named person within 15 m"),
            new Entry("round", "People", "Someone stands the house a drink", "TestRoundKey", needs: "a saloon, two named people within 6 m of each other"),
            new Entry("sight", "People", "Someone reacts to how you look", "TestSightKey", needs: "someone within 10 m who can see you; looking ordinary, bloodied, drunk, filthy and soaked in turn"),
            new Entry("nod", "People", "Two who know each other acknowledge it", "TestNodKey", needs: "two people with a tie within 10 m of each other"),
            new Entry("younod", "People", "Someone near shows what they feel about you as you pass", "TestYouNodKey", needs: "MindLive and MindPassing; someone the mind knows within 12 m, in view, who feels something about you"),
            new Entry("seatedlook", "People", "Someone sat or leaning looks up (the mind's seated look)", "TestSeatedLookKey", needs: "[Mind] MindLive, MindActs and MindActsSeatedLooks; someone the mind knows sat or leaning within 15 m, free (they look at you if you are in view, else at the nearest person; their seat is never touched)"),
            new Entry("getup", "People", "A seated person gets up and acts on their mind", "TestGetUpKey", needs: "[Mind] MindLive, MindActs and MindActsScenarios; someone the mind knows sat or leaning within 15 m, free (they get up the game's own way for their choice, or for a rest on a bench or a wall elsewhere; anyone not out of their scenario in time is put back in it)"),
            // ---- street scenes: the nearest named pair that fits plays it now (no tie or memory is noted for a test) --
            new Entry("scene:argue", "Street scenes", "An argument", "TestArgueKey", needs: "two named people within 16 m"),
            new Entry("scene:brawl", "Street scenes", "A fistfight", "TestBrawlKey", needs: "two named people within 16 m; women too with WomenInFights"),
            new Entry("rematch", "Street scenes", "After a fight: rematch, clear off, handshake", "TestRematchKey", needs: "[Scenes] Rematches; two named people within 16 m (the nearest stand for the two who fought)"),
            new Entry("wager", "Street scenes", "A fistfight with bets on it", "TestWagerKey", needs: "[Scenes] BrawlWagers; two named people within 16 m to fight, and others the mind knows within 25 m of them (bets need one for each side within 9 m of each other)"),
            new Entry("scene:smalltalk", "Street scenes", "Small talk", "TestSmallTalkKey", needs: "two named people within 16 m"),
            new Entry("scene:gossip", "Street scenes", "Gossip", "TestGossipKey", needs: "two named people within 16 m"),
            new Entry("scene:nighttale", "Street scenes", "An old tale told after dark", "TestNightTaleKey", needs: "two named people within 16 m; [Scenes] NightTales (any hour from the row)"),
            new Entry("scene:flirt", "Street scenes", "A flirt", "TestFlirtSceneKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:court", "Street scenes", "A courtship (the answer turns: yes, maybe, no)", "TestCourtKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:propose", "Street scenes", "A proposal", "TestProposeKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:tiff", "Street scenes", "Sweethearts have words", "TestTiffKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:condole", "Street scenes", "Condolences to someone grieving", "TestCondoleKey", needs: "two named people within 16 m"),
            new Entry("scene:mend", "Street scenes", "Making it up with a gift (taken, then not)", "TestMendKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:drunk", "Street scenes", "A drunk calls a man out", "TestDrunkKey", needs: "two named men within 16 m"),
            new Entry("scene:lift", "Street scenes", "A purse lifted", "TestLiftKey", needs: "two named people within 16 m"),
            new Entry("scene:pickpocket", "Street scenes", "A pickpocket caught at it", "TestPickpocketKey", needs: "a named man and another within 16 m"),
            new Entry("scene:beg", "Street scenes", "Someone begs a coin", "TestBegKey", needs: "two named people within 16 m"),
            new Entry("scene:tend", "Street scenes", "A friend tends the one who was hurt", "TestTendKey", needs: "two named people within 16 m"),
            new Entry("scene:avoid", "Street scenes", "Someone keeps clear of the one who wronged them", "TestAvoidKey", needs: "two named people within 16 m"),
            new Entry("brawltown", "Street scenes", "A brawl grows and the law steps in", "TestBrawlTownKey", needs: "MindLive, MindJoinFights and MindBrawls; two named people within 16 m to start the fight and others the mind knows within 12 m to join it (a saloon is best); a lawman within 70 m for the second half"),
            new Entry("scene:standup", "Street scenes", "Someone stands up to a bully", "TestStandUpKey", needs: "two named men and a third named person within 16 m; a fistfight they step into; it takes turns: the fight goes on, the bully backs off, with MindLive they take the other away, then he turns on the one who stepped in"),
            new Entry("scene:ladder", "Street scenes", "A quarrel from the ladder (turns: words, blows)", "TestLadderKey", needs: "two people within 16 m whom the mind knows ([Mind] MindShadow on); it takes turns, an argument from words and then a brawl from the blows, and the ladder decides the rest; the test needs no MindLive"),
            new Entry("scene:standoff", "Street scenes", "A standoff", "TestStandoffKey", needs: "two named men within 16 m"),
            new Entry("scene:song", "Street scenes", "A song", "TestSongKey", needs: "two named people within 16 m"),
            new Entry("scene:talltale", "Street scenes", "A tall tale", "TestTallTaleKey", needs: "two named people within 16 m"),
            new Entry("scene:haggle", "Street scenes", "Haggling", "TestHaggleKey", needs: "two named people within 16 m"),
            new Entry("scene:cheat", "Street scenes", "A card cheat caught", "TestCheatKey", needs: "two named men within 16 m"),
            new Entry("scene:hosts", "Street scenes", "Two who are waking talk of it", "TestHostsKey", needs: "two named people; HostsQuestionReality on"),
            new Entry("scene:x:snap", "Street scenes", "A snap at the worst of a worry", "TestSnapKey", needs: "someone whose worry is at its worst"),
            new Entry("scene:x:goodnews", "Street scenes", "Good news told", "TestGoodNewsKey", needs: "someone whose worry ended well"),
            new Entry("scene:x:badnews", "Street scenes", "Bad news told", "TestBadNewsKey", needs: "someone whose worry ended badly"),
            new Entry("scene:x:quarrel", "Street scenes", "An old quarrel picked up", "TestQuarrelKey", needs: "two named people within 16 m"),
            new Entry("scene:x:sweet", "Street scenes", "Sweethearts", "TestSweetKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:x:debt", "Street scenes", "A debt called in", "TestDebtKey", needs: "two named people within 16 m"),
            new Entry("scene:x:feud", "Street scenes", "A feud flares", "TestFeudKey", needs: "two named people within 16 m"),
            new Entry("scene:x:cards", "Street scenes", "A gambler asks a man for a loan", "TestGamblerLoanKey", needs: "[Mind] GamblersBorrow; a named man whose worry is the cards (or who has a lender), and another within 16 m"),
            new Entry("scene:x:courting", "Street scenes", "A courting worry, face to face", "TestCourtingKey", needs: "a named man and woman within 16 m"),
            // ---- tools ---------------------------------------------------------------------------------------------
            new Entry("showminds", "Tools", "Show minds (a card over people near you)", "TestShowMindsKey", "Ctrl+F2"),   // on and off for the session (Lex, 2026-10-27)
            new Entry("wakestages", "Tools", "Show wake stages (a tag over the waking)", "TestWakeStagesKey"),
            new Entry("replay", "Tools", "Fire the last test again", "TestReplayKey"),
            new Entry("cast", "Tools", "Bring the test cast here (kept in the save)", "TestCastKey", needs: "a town, out of doors; eight people the tests need (a mother, two old men, a preacher and a listener, three men for a bully) come and stroll about a spot 10 m in front of you"),
            new Entry("voice", "Tools", "Voice the nearest person now (the voice server)", "TestVoiceKey", needs: "[Mind] VoicedThoughts or VoicedDialogue on, the voice server running (voice\\start-voice-server.cmd), someone within VoiceMetres"),
            new Entry("voicerec", "Tools", "Record the nearest person's voice clip (the voice server)", "TestVoiceRecKey", needs: "[Mind] VoicedThoughts or VoicedDialogue on, the voice server running, someone within 8 m whose voice bank is certain and who isn't speaking; press by press: their own voice, then the bank by its full name, then by its short name"),
            new Entry("voicerec:all", "Tools", "Record every voice still wanted on the nearest person", "TestVoiceRecAllKey", needs: "the voice server running; whoever is nearest within 10 m says each bank's line in turn, about ten seconds each (all 592 take about 90 minutes); it waits through rain, fights and other voices; pressed again, it stops"),
            new Entry("listen", "Tools", "Hear you through the microphone (the voice server)", "TestListenKey", needs: "[Mind] TalkVoice on, the voice server running with its transcriber (install-voice.cmd puts it in) and a microphone; say something within TalkVoiceSeconds: what it heard shows above the ticker and in the log as HEARD"),
            new Entry("subtitle", "Tools", "Show a test subtitle (RealNPCs' box)", "TestSubtitleKey"),
            new Entry("cast:go", "Tools", "Let the test cast go (they stay in the save)", "TestCastGoKey"),
        };

        public static Entry Find(string id)
        {
            return id == null ? null : All.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        /// The entry an ini key belongs to, or null.
        public static Entry ByKey(string key)
        {
            return key == null ? null : All.FirstOrDefault(e => e.KeySetting.Equals(key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
