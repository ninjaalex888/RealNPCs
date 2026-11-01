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
            new Entry("campfire", "Encounters", "Strangers at a campfire", "TestCampfireKey", needs: "out of the towns; a test takes turns with and without the traveller who walks in"),
            new Entry("callout2", "Encounters", "Someone calls out another before the street", "TestCallout2Key", needs: "a town's street, two named people on their feet 2-18 m apart with something between them (a tie gone sour: a quarrel, a beating, a theft; or a debt), one in your view"),
            new Entry("domestic", "Encounters", "A husband loses his temper outside his home", "TestDomesticKey", needs: "a town's street, a named man on his feet 6-35 m from you (his sweetheart near plays his wife, else his wife walks up from out of sight)"),
            new Entry("hogtied", "Encounters", "Stage a hogtied captive nearby", "TestHogtiedKey", needs: "out of the towns, a town with regulars; a mark on the map; greet the men's leader to pay when he names a price (a test takes no money)"),
            new Entry("lift", "Encounters", "A man bumps into you and lifts your money", "TestLiftYouKey", needs: "a man near you (4-22 m) free to send, $2 or more in your pocket; he walks into you, takes a fifth of it ($1-25) and runs: catch him to get it back"),
            new Entry("visitor", "Encounters", "The night visitor", "TestVisitorKey", needs: "in a town, out of doors, no lawman within 60 m"),
            new Entry("gathering", "Encounters", "A gathering of the awake", "GatheringKey", field: true),
            new Entry("stalker", "Encounters", "A stalker now", "StalkerKey", "]", needs: "outdoors", field: true),
            new Entry("stalkeroutfit", "Encounters", "Stalker outfit: show the next candidate", "TestStalkerOutfitKey", needs: "out of the saddle; a man stands 4 m in front of you in the next model and preset (every preset of one model, then the next), the one before goes; the log and the hint give the two ini lines for [Stalkers] StalkerBlackModel and StalkerBlackOutfit"),
            // ---- people --------------------------------------------------------------------------------------------
            new Entry("wake", "People", "Wake the person you face one stage", "WakeKey", "Y", needs: "face someone a few steps off", field: true),
            new Entry("realtalk", "People", "Two people talk in their real voices", "TestTalkKey", "Shift+F7", needs: "two people within 20 m"),
            new Entry("rough", "People", "A face into the bar (rough handling)", "TestRoughKey", "Shift+F9", needs: "two men within 10 m, best in a saloon"),
            new Entry("font", "People", "The subtitle line in each font", "TestFontKey", "Shift+F2"),
            new Entry("plea", "People", "A plea at gunpoint (then aim at someone)", "TestPleaKey", needs: "aim at someone within 30 m in the next minute; lowering your gun counts as real mercy"),
            new Entry("arrival", "People", "A word as you ride into town", "TestArrivalKey", needs: "in a town"),
            new Entry("letter", "People", "A letter (the waiting ones, else a test one)", "TestLetterKey"),
            new Entry("musing", "People", "A stranger says something strange", "TestMusingKey", needs: "someone 3-14 m off"),
            new Entry("glitch", "People", "A stranger glitches", "TestGlitchKey", needs: "someone 3-16 m off"),
            new Entry("disbelief", "People", "Disbelief, voiced, over someone's head", "TestDisbeliefKey", needs: "a named person within 15 m"),
            new Entry("wakevoices", "People", "Wake voices: every line, one by one", "TestWakeVoicesKey", needs: "someone within 12 m"),
            new Entry("despairnow", "People", "Despair now, the next way in turn", "TestDespairNowKey", needs: "someone RealNPCs knows, 3-12 m away"),
            new Entry("despairanims", "People", "Despair animations, one by one", "TestDespairAnimsKey", needs: "someone free within 12 m"),
            new Entry("stageanims", "People", "Wake stage gestures, one by one", "TestStageAnimsKey", needs: "someone free within 12 m"),
            new Entry("wakestrange", "People", "Strange waking acts, the next in turn", "TestWakeStrangeKey", needs: "West World on, someone unravelling or awake (stage 3+) within 15 m, someone else near them (stripping: someone awake). Order: close up, wave, follow, not real, preach, lash out at you, lash out at another, strip, dazed"),
            new Entry("mindscene", "People", "A mind starts a scene (their own reason)", "TestMindSceneKey", needs: "named people near with a reason between them (a quarrel, a friend grieving, a debt); [Mind] Language and LanguagePicks for the model to pick, else their strongest reason plays"),
            new Entry("talk", "People", "Talk to the nearest person you know", "TestMenuKey", needs: "an adult RealNPCs knows within 6 m, not in a scene or a fight"),
            new Entry("space", "People", "Someone minds you standing so close", "TestSpaceKey", needs: "someone within 3 m, standing, not the law, not sitting or at the bar, not busy"),
            new Entry("lonely", "People", "A lonely regular comes over", "TestLonelyKey", needs: "MindLive, someone the mind knows within 18 m and in view, standing, who neither fears you nor holds anything against you"),
            new Entry("favour", "People", "Someone in trouble comes to ask a favour", "TestFavourKey", needs: "MindLive; someone the mind knows within 18 m, standing, in view"),
            new Entry("warnyou", "People", "Someone warns you, or tells you to your face what they saw", "TestWarnYouKey", needs: "MindLive; someone the mind knows within 18 m, in view"),
            new Entry("thanks", "People", "Someone you helped thanks you", "TestThanksKey", needs: "someone within 20 m who can see you, whom you stood a drink, gave money or settled a debt for this week (the Talk menu; Greedy people don't come, Cold ones nod)"),
            new Entry("lawbody", "People", "A lawman clears a body", "TestLawBodyKey", needs: "a body within 40 m and a lawman on foot within 80 m of it, no fight near"),
            new Entry("arthur", "People", "Arthur's own line", "TestArthurKey", needs: "nothing: each press tries the next moment's lines (someone came at him, the pickpocket caught)"),
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
            new Entry("patient", "People", "Someone walks to the doctor and tells him what ails them", "TestPatientKey", needs: "a town with its doctor within 90 m (Valentine, Rhodes or Saint Denis); a named person within 16 m in view, free; they say what's wrong, he answers, they walk home"),
            new Entry("lostchild", "People", "A mother asks around for her lost child", "TestLostChildKey", needs: "a town street, a named woman within 25 m in view (within 7 m she asks you)"),
            new Entry("callout", "People", "A man calls another out at noon", "TestCalloutKey", needs: "a town street, two named men with bad blood 2-14 m apart"),
            new Entry("grudge", "People", "A friend takes up a wrong", "TestGrudgeKey", needs: "a friend of someone wronged in the last 3 days, near the one who did it"),
            new Entry("grudge:you", "People", "Someone grieving one you killed", "TestGrudgeYouKey", needs: "in town, someone within 20 m whose friend you killed this week"),
            new Entry("kin", "People", "Kin of someone you know walks up", "TestKinKey", needs: "in a town where someone you have dealt with has kin, and a passer-by in view within 18 m, unnamed, whom RealNPCs may direct"),
            new Entry("drink", "People", "Someone drinks away bad news", "TestDrinkKey", needs: "a saloon, a named person within 15 m"),
            new Entry("round", "People", "Someone stands the house a drink", "TestRoundKey", needs: "a saloon, two named people within 6 m of each other"),
            new Entry("sight", "People", "Someone reacts to how you look", "TestSightKey", needs: "someone within 10 m who can see you; looking ordinary, bloodied, drunk, filthy and soaked in turn"),
            new Entry("nod", "People", "Two who know each other acknowledge it", "TestNodKey", needs: "two people with a tie within 10 m of each other"),
            new Entry("younod", "People", "Someone near shows what they feel about you as you pass", "TestYouNodKey", needs: "MindLive and MindPassing; someone the mind knows within 12 m, in view, who feels something about you"),
            new Entry("seatedlook", "People", "Someone sat or leaning looks up (the mind's seated look)", "TestSeatedLookKey", needs: "[Mind] MindLive, MindActs and MindActsSeatedLooks; someone the mind knows sat or leaning within 15 m, free (they look at you if you are in view, else at the nearest person; their seat is never touched)"),
            new Entry("getup", "People", "A seated person gets up and acts on their mind", "TestGetUpKey", needs: "[Mind] MindLive, MindActs and MindActsScenarios; someone the mind knows sat or leaning within 15 m, free (they get up the game's own way for their choice, or for a rest on a bench or a wall elsewhere; a chair that never lets go is left be)"),
            // ---- street scenes: the nearest named pair that fits plays it now (no tie or memory is noted for a test) --
            new Entry("scene:argue", "Street scenes", "An argument", "TestArgueKey", needs: "two named people within 16 m"),
            new Entry("scene:brawl", "Street scenes", "A fistfight", "TestBrawlKey", needs: "two named people within 16 m; women too with WomenInFights"),
            new Entry("scene:smalltalk", "Street scenes", "Small talk", "TestSmallTalkKey", needs: "two named people within 16 m"),
            new Entry("scene:gossip", "Street scenes", "Gossip", "TestGossipKey", needs: "two named people within 16 m"),
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
            new Entry("scene:x:courting", "Street scenes", "A courting worry, face to face", "TestCourtingKey", needs: "a named man and woman within 16 m"),
            // ---- tools ---------------------------------------------------------------------------------------------
            new Entry("showminds", "Tools", "Show minds (a card over people near you)", "TestShowMindsKey", "Ctrl+F2"),   // on and off for the session (Lex, 2026-10-27)
            new Entry("wakestages", "Tools", "Show wake stages (a tag over the waking)", "TestWakeStagesKey"),
            new Entry("replay", "Tools", "Fire the last test again", "TestReplayKey"),
            new Entry("cast", "Tools", "Bring the test cast here (kept in the save)", "TestCastKey", needs: "a town, out of doors; eight people the tests need (a mother, two old men, a preacher and a listener, three men for a bully) come and stroll about a spot 10 m in front of you"),
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
