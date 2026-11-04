RealNPCs 0.7.20 - lives of their own for the people of Red Dead Redemption 2 (formerly enRAGE Westworld)
========================================================================================================

The people of the world start acting like they have their own lives. They tell you about
themselves, remember what you did, think about you, keep diaries, gossip, fall in love, marry,
mourn, owe each other money, and come after you. The whole world feels it: word of what you do
travels from town to town and grows in the telling, kin you never met write to you or ride in for
revenge, strangers quarrel, sing, haggle, court and square off around you (and you can step in),
and some people begin to wonder whether any of this is real. A few of the dead don't stay dead.

It's free. It no longer needs any other mod. It began as an add-on for enRAGE, the nemesis mod,
and stays compatible with it: with enRAGE installed, RealNPCs reads its nemeses and townsfolk
and gives them lives too. It's not made by the enRAGE author and never modifies enRAGE's program
(it edits enRAGE's settings file only when you change an enRAGE setting in its panel, after a
backup). enRAGE is on Nexus Mods: https://www.nexusmods.com/reddeadredemption2/mods/10967
Source code and bug reports: https://github.com/ninjaalex888/RealNPCs

Story Mode only. Don't run script mods in Red Dead Online.


NEW: THE PEOPLE SPEAK IN THEIR OWN VOICES (OPTIONAL, RECOMMENDED)
------------------------------------------------------------------
The headline of 0.7.19. With the voice server running, the people near you say RealNPCs' words
out loud in their own voices: the thought over someone's head is murmured in that person's
voice a moment after it shows, and what people say near you (the street's scenes, their talks
with each other, Talk's answers, the encounters) is said in the speaker's own voice, the
subtitle coming with it. Played for the first time on 2026-11-03, and the town talks.

How it works: each voice is cloned on your own PC, by the open-source Chatterbox model, from
the game's own recording of that person's voice, and those recordings come from your own game:
someone whose voice has no clip yet says one of the game's own lines once, in a quiet moment as
you meet them, and the server records it off your PC's sound and keeps it ([Mind] VoiceRecord,
on). A clearer recording later replaces a weaker one; another voice too near spoils a clip and
it's recorded again another time; a person whose model has several voice banks is given one
for good, by who they are. Nothing of the game's audio ships with RealNPCs, and nothing leaves
your PC. One voice plays at a time, louder near and from the side the speaker stands on, within
14 m ([Mind] VoiceMetres); VoiceVolume (100). The game's own recorded remark, where one plays,
still plays.

What you need: an NVIDIA graphics card with about 3 GB of memory free beside the game while
someone speaks (with the AI model running too, about 8 GB free: a 16 GB card does both, a 12 GB
card one or the other), about 8 GB of disk, and a one-time setup of about ten minutes. Without the voice server nothing
changes: the switches are on as they come, and nothing is heard until it answers (the log says
so once).

Set it up once:
1. Keep the zip's "voice" folder somewhere permanent, for instance Documents\RealNPCs\voice. It
   holds install-voice.cmd, start-voice-server.cmd, voice_server.py and voices.json.
2. Double-click install-voice.cmd once. It fetches uv, a Python of its own, PyTorch for NVIDIA
   cards, Chatterbox and its model files, and the microphone's transcriber (faster-whisper,
   about 150 MB) into that folder (about 8 GB, once), and ends by saying
   "The voices are ready." through your speakers. Nothing else on your PC changes; delete the
   folder to remove it all. If it fails part way (a download dropped), run it again: it carries
   on where it stopped.
3. Before you play, double-click start-voice-server.cmd and leave its window open. It takes
   about 15 seconds to load the model, then says it's listening.
4. Play. People are recorded as you meet them, so the first voices come after a few minutes in
   a town; F7's "TEST voice:" line and the server's window (and voice-server.log beside it) say
   what it's doing.

What to expect, honestly: a line takes a second or two to make, and the words wait for it; the
clips are recorded off the street, so a voice can carry a little of the sound around the person
it was recorded from (the server cleans it, and re-records a better one when it can); the voice
server holds about 2.5 GB of your card while people are speaking and gives it back to the game
after a minute with nobody speaking (the next line brings it back in under a second;
"--idle-seconds 0" in start-voice-server.cmd keeps it on the card, "--full" runs the voice
model in full precision, 3.5 GB, if the voices sound off), and 2.5 GB of system memory. To
turn it off: [Mind] VoicedThoughts and VoicedDialogue on the settings panel's Mind page, or
close the server's window.


OPTIONAL, RECOMMENDED: THE FREE LOCAL AI MODEL
-----------------------------------------------------
YOU DON'T NEED IT. RealNPCs runs fully without the AI model and without Ollama: no extra
graphics memory, nothing else to install, and every feature works on its own written lines.
If your graphics card has no room to spare, simply skip this section.

If your card does have room, install the free local AI model to get the most out of
RealNPCs. With it on, the people near you:
- think in their own words, from who they are, how they feel and what's on their mind, so no
  two think alike;
- choose what they do next from what their own mind allows: head home, pick a quarrel, call in
  a debt, court someone;
- answer you in their own words when you Talk to them (Q, locked on);
- say some of their thoughts aloud in their own recorded voice, the remark that fits;
- and with West World on, those waking up choose their own strange acts.

Without the model they still think, choose and talk on RealNPCs' own rules and written lines:
a thought with what they choose to do, scenes they start for their own reasons, what they
feel most said aloud in their own voice, Talk answers that vary and remember, and thousands of
written thoughts that hardly repeat in an hour. The model words it more freely; it isn't needed.

It runs on your own graphics card through Ollama (https://ollama.com): nothing is sent
anywhere, and there's no account and no cost. Everything the model writes is checked first
(length, names it wasn't given, modern words). When it's slow or a line fails the checks, the
written line is used instead, so the game never waits on it. It's still in testing, so expect
the odd strange line.

What you need, only if you add the model: a graphics card with about 5 GB of memory free
beside RDR2 (a 12 GB card or more; RealNPCs is developed on a 16 GB card), and about 5 GB of
disk space. Graphics card memory, with RDR2 on medium settings:
- 12 GB or more (e.g. RTX 3060 12 GB, 4070, RX 6700 XT): recommended; the game and the model
  fit with room to spare.
- 10 GB (e.g. RTX 3080 10 GB): workable at medium settings at 1080p; tight at higher settings.
- 8 GB or less: too tight for the model beside the game: play without it. Everything still
  works.
- No room to spare: RealNPCs runs fully on its own written lines.
A slower card can still run the model: answers just come a little later (under half a second
on a fast card, about a second or more on a mid-range one), and any answer that comes too
late (after two to three seconds) gives way to RealNPCs' own line or choice, so the game
never waits for it.

Set it up once:
1. Install Ollama from https://ollama.com/download and leave it running (it sits in the
   system tray).
2. Open a Command Prompt and run: ollama pull llama3.1:8b   (a 4.9 GB download)
3. Start the game. RealNPCs finds Ollama and the model by itself and turns the AI model on,
   with a one-time notice saying so. Within a few seconds, people near you start thinking in
   the model's words.
4. To check it's working, open the settings panel (Alt+F): AI MODEL (OLLAMA) at the top of the
   Essentials page shows whether it's on and answering. Turn it off there whenever you like;
   once you've turned it off, RealNPCs leaves it off.

Optional: to have the model answer two people at once, set the Windows environment variable
OLLAMA_NUM_PARALLEL to 2, then quit and reopen Ollama.


NEW IN 0.7.20
-------------
- The green dot on friends and allies is smaller ([Encounters] AlliesDotScale, 0.6; 1 is the
  game's own size, which drew large). A change shows on the next dot made; Insert remakes
  them all.

NEW IN 0.7.19
-------------
- The voices (see the section above): with the voice server, the people near you speak RealNPCs'
  words in their own voices, cloned on your PC from your own game as you meet them ([Mind]
  VoicedThoughts, VoicedDialogue, VoiceRecord, on; VoiceMetres 14, VoiceVolume 100). Seen in
  game. The zip's "voice" folder holds the one-time installer and the server.
- People talk to each other from what's on their minds ([Mind] MindTalks, on; no AI model
  needed): among people who know each other, one brings up the worry they're living through,
  the times they live in, the stranger as they remember him, a plan they carry, the town's
  talk, what they're short of, or the day; the other answers from their own nature, kindly,
  coldly or sharply, and on the times from their own side of the question; with the AI model,
  in its own words. Said in their own voices with the voice server.
- The times they live in: about 440 lines of what people in 1899 were really talking of (the
  election of 1900, Bryan and McKinley, the war in the Philippines, Dewey's homecoming, Cuba and
  the fever, the Klondike and Nome, the Boers, the reservations and the allotments, Wounded Knee,
  Geronimo, the trusts and the railroads, the horseless carriage, the telephone, the bicycle,
  the yellow papers, Jeffries, the Maple Leaf Rag, the blizzard, the century turning, The Hague,
  Dreyfus, Georgia, the frontier closed, votes for women, wages) and in the epilogue's 1907
  (Roosevelt and the canal, the Panic, Oklahoma, the white fleet, motorcars and flying machines,
  San Francisco, the Thaw trial, Monongah, The Jungle, Taft and Bryan, the Cubs, Johnson and
  Cobb, Ellis Island, the old West gone, the suffrage women, Geronimo's book), each from the
  month it was in the papers. In the talks above, and when you ask someone the news.
- Allies who come to your aid ([Encounters] AlliesHelp, on; AlliesMetres 40, AlliesMost 3,
  AlliesLiking 90, AlliesRoadChance 30): attacked by anyone, the law too, within sight of people who
  think well of you (enRAGE's fondness, a kindness of yours they remember, a trouble you
  mended, talks and your name known), some of them come: the brave and the hot first, the loyal
  for a friend, cowards never, a woman only against fists; against guns they're given a
  revolver. They shout as they come, go for your attackers, and have a word with you after;
  they remember standing with you, and enRAGE's townsfolk think better of you for it. Out of
  the towns a friend from a town's ledger may happen by, on horseback (AlliesRide, on), and come
  in off the horse. An ally who dies standing with you is grieved by those who knew them, who
  lose AlliesGrievedLiking (15) of their fondness for you (a third for someone who merely knew
  them) and remember whose quarrel it was; the town has it by evening (AlliesGrieved, on). Not
  yet seen in game.
- Thoughts follow the moment they're in: someone fighting you (fists or guns), fighting
  someone else, running for their life, knocked down, hurt, or looking down your barrel
  thinks of that before anything, awake or not, with or without the AI model (the line is
  written and turns over every few seconds); so does anyone with shots, a dead body or a
  fight close by. The quieter moments join their thoughts too: the saddle, a wagon, a drink,
  a meal, a smoke, their work, a seat, walking in the dark, drunk, a gun in hand, how you look
  beside them. About 430 new lines by temper. The AI model is told what's happening to them.
  Only the waking doubt the world: someone still asleep never wonders whether any of it is
  real. Not yet seen in game.
- A green dot on each friend near you on the map (the bar at AlliesLiking or more, someone you
  saved), and on each ally while they're with you ([Encounters] AlliesDot, on;
  AlliesDotColour: a blip modifier's name, BLIP_MODIFIER_DEBUG_GREEN as built). Greeting
  someone makes them fonder of you ([Encounters] GreetLiking, 34): enRAGE's fondness rises
  by a third of the bar on every greeting of someone, so three hellos fill the bar and make
  a friend who'll stand with you (from 90 on the bar); nothing from the afraid or the
  angry. Someone you save is yours for good: the man you pull a bully off, the
  one whose beater you put down in a brawl, the captive you save from the rope, the hogtied
  you cut loose who thank you: fondness to the top, a friend who owes you for a month of game
  days (they come to your aid first), and they remember it. Not yet seen in game.
- The year as it was ([World] HistoricalAccuracy, on): the real calendar, keyed to the game's
  own date (Easter 2 April 1899, the Fourth, Election Day, Thanksgiving, Christmas; Mardi Gras
  and All Saints' in Lemoyne), each with what the day, its eve and its morning after bring to
  mind and a word of the day on the ticker; the year's work by region (calving and the roundup,
  cane and cotton, the trapping lines); in 1907 the Food and Drugs Act and the Panic. The talk
  of the times and the lines about the year now stand behind the same switch: false, and none
  of it is thought or said. Not yet seen in game.
- A kindness seen ([Encounters] KindnessSeen, on; KindnessSeenLiking, 5): the named people
  who see you do a kindness to someone else from within 12 m (a drink stood, money or a gift
  given, a debt paid, a hand up, a bully faced, a mercy shown) think better of you too, and
  remember seeing you do right. Not yet seen in game.
- Fixed: the allies Testing row said "the model isn't loaded" (the man's body, not the AI
  model) and made nobody; it waits for the body now. Allies keep fists against fists; nobody
  new comes while you're wanted. The VRAM gauge keeps trying after failed readings instead of
  giving up, and takes the card's size from the registry when DXGI reads it smaller.
- The VRAM gauge ([Display] VramGauge, model): "VRAM 11.3 / 15.6 GB" at the top left while the
  AI model is on; amber over 85%, red over 95%.
- The voices are heard within 14 m (VoiceMetres, from 15).
- Fixed: Talk (Q) on someone walking by left them walking and the list closed; they stop and
  turn to you now. The subtitle for a voiced line waits for the voice. A street scene could call
  someone by one name while the card over their head used another; one name now. The glitch
  that repeated a greeting three times is gone, and the questioning stage of the Awakening says
  nothing twice.
- Called out yourself ([Encounters] CalloutsArthur, on): by day in a town, someone with something
  against you (you struck or robbed them, they saw you kill, you soured them) walks up and says it
  to your face. Stay and most put up their fists (fighting them is self-defence), walk off past
  12 m and they call you a coward, hold a gun on them and they back off. Seen in game.
- The hogtied: hold a gun on the men and bounty hunters fight back; one in four you cut loose
  pulls a knife on you instead of thanking you ([Encounters] HogtiedTurnsOnYou, 25).
- Names that fit the person: Chinatown's people get Chinese names, the Wapiti theirs, Hispanic and
  Black townsfolk names of their own, from the game's own voice recordings; French, Italian,
  Irish, German and Scandinavian names in the towns they settled. People you've met keep theirs.
- The Awakening rarer and eerier: gatherings mostly after dark or in fog and rain, some gone
  when you look back, the waking's odd lines far fewer. GatheringChance 10, GatheringGapHours 24,
  AwakeningDespair 50 as they come (an ini on the old values is moved; your own are kept).
- The held ring ([Mind] HeldRing, off while it's tried): the people within 12 m of you go from
  one act straight into the next instead of the game taking them back between acts.
- Fixed: the law now comes to a fight its scene let go; the crying woman holds only her knife.
- Without the AI model, people with cause now and then wonder at the world in a written thought
  ([Mind] WrittenWakes, on).


NEW IN 0.7.18
-------------
- Half the gatherings of the awake turn on you within 10 m, the whole ring together, each a red
  dot ([Encounters] GatheringHostile, 50). A ring does one strange thing together.
- The game's own event dot on the map where one of RealNPCs' happenings is going on near you,
  until you reach it ([Encounters] EventDots, on). The dot is seen in game; the hostile turn not yet.


NEW IN 0.7.17
-------------
- More happening, less idle talk: street scenes twice as often ([Scenes] DramaGapSeconds 30),
  and scenes of nothing but talk passed over ([Scenes] EventfulOnly, on).
- A lawman near may shoot the one who picked your pocket ([Scenes] LawShootsThieves, 50).
- Gatherings of the awake: six to ten in the ring, each kneeling, dancing, staring, lying on
  the ground, holding a lantern or making a low sound.
- Talk varies (three or four answers where there was one) and remembers what it said; feelings
  know their cause; the waking mutter far more; more of the game's recorded lines between people.
  All without the AI model. Not yet seen in game.


NEW IN 0.7.16
-------------
- Far fewer repeated thoughts, with no AI model needed: a thought is remembered for about an
  hour of play, not a few minutes, and about 650 new lines arrive (thoughts about you, every
  worry's stages, the meals, the days and the year, the weather, sore and glad days, watching
  the street, and the street's hellos and news). In a three-hour ride, thoughts already seen fell
  from 22/43/50% by the hour to 0/14/31%; the street's talk repeats 9% of the time in an
  hour, from 15% ([Mind] SaidLately). Measured offline; not yet seen in game.


NEW IN 0.7.15
-------------
- Brawls at the bar: two people sat or leaning may come to blows, getting up first the game's
  own way ([Mind] SeatedBrawls, on); onlookers join more readily in a fight's first 10 s
  ([Mind] BrawlOpeningBoost, 20). The crowd that ringed a fight is off ([Scenes] FightCrowds):
  some of them froze. Seen in game in the Valentine saloon.
- Without the AI model: thoughts said aloud in their own recorded voice, from what they feel
  most ([Mind] ThoughtsAloud), and scenes people start for their own reasons, on top of the
  street's ([Mind] MindStartsScenes). Not yet seen in game.
- A table of the game's 592 voices ships inside RealNPCs (line names and take counts only).
- The settings panel opens on Alt+F or Alt+R; Alt+F11 is left to enRAGE.
- Fixed: one name per person everywhere; people frozen after a reload or by the fight crowd.


NEW IN 0.7.14
-------------
- Fights grow into brawls: onlookers are readier to join ([Mind] BrawlJoinBoost, 20; 0 as
  before), so two men fighting can draw in friends, foes and hotheads.
- Whoever threw the first punch wears a small red dot on the map while the fight lasts, and
  stepping in against him is self-defence ([Mind] BrawlStarterDot, on).
- A fight is heard: if no lawman sees it, after a few seconds any free lawman within 60 m comes.
  Seen in game in Saint Denis.


NEW IN 0.7.13
-------------
- A thought with what people choose to do, no AI model needed ([Mind] ActThoughts, on). Someone
  near you who gets up to go home, heads for a drink, sits down to rest, keeps clear of you or goes
  over to a friend shows what moves them to it; each turn of an encounter shows the reason for the
  answer chosen. One person at most once every 90 s (ActThoughtEachSeconds), never within 15 s of
  another thought of theirs (ActThoughtGapSeconds). With the model on, its own thought comes first.
  Loaded and running in game; not yet seen on screen.
- The settings page "The world and its people" is "World and people" now.


NEW IN 0.7.12
-------------
- The AI model is optional. RealNPCs runs without it and without Ollama, with no extra graphics
  memory (see OPTIONAL, RECOMMENDED above for which cards have room for it). Everything
  new below works without it: people's own minds choose, and with the model on it only picks
  among the same few things. Making RealNPCs as rich without the model is the next big piece of
  work (roadmap 41 on GitHub).
- People get up and go about what their minds choose ([Mind] MindActsScenarios, with MindActs).
  Someone sat on a bench, leaning on a wall, at the bar or having a smoke whose mind wants a
  drink, a meal, a rest elsewhere, their work, home or someone to talk to gets up the way the
  game's own scripts get people up, and goes to it: a bench, a chair or the steps to rest; a
  wall, a post or a rail to lean on; the bar rail or a table for a drink or a meal; a broom, a
  cloth or a hammer for the work; two people already talking, to join them. Anyone not up in 9
  seconds is put back where they were, so nobody is left frozen in a pose. One gets up every 4 to
  6 seconds near you, three at most at once. Testing row "A seated person gets up and acts on
  their mind". Seen in game: people getting up from benches and walls, and nobody left frozen.
  Not yet seen: the walk to a spot of the kind they want.
- Two street scenes at once ([Scenes] MainScenesAtOnce 2, on the Essentials page; 1 is one at a
  time, as before). While one full scene plays, a second can start between two other people 20 m
  or more away, with its own walk-over and its words as subtitles when it's near you. Fights,
  rough handling and thefts stay one at a time. In the log so far, not yet watched on screen.
- The storyteller ([World] Storyteller, lively; calm, wild, or off as before; on the Essentials
  page). When nothing worth stopping for has happened near you for a while (2 minutes in lively,
  4 in calm, 1 in wild), the next street scene, chance meeting, walk-up and encounter roll come
  sooner; after something big, a little later for half a minute or so. It only changes the waits,
  and holds everything as built in a mission, a fight, while wanted or indoors. Seen running in
  game.
- Called out before the street ([Encounters] PublicCallouts, PublicCalloutChance 35). In a town
  by day, someone with a real grievance against another (a debt, a theft, a beating, hard words)
  calls him out in front of whoever is about; each answers by their own nature, or the AI model
  picks among the same few things, and up to two people near watch, jeer, step in or walk off.
  Walk up and it ends. Testing row "Someone calls out another before the street". Not yet seen in
  game.
- A husband's temper ([Encounters] DomesticDisputes, DomesticChance 30). In a town by day, a man
  starts in on his wife in the street; he rants, storms off or lets it drop, and now and then
  strikes her (rarely, once); she gives it back, pleads, sobs or walks off; a neighbour may step
  in or fetch the law. Walk up before he's struck her and it's over. Once he has struck her,
  dealing with him is no crime. Testing row "A husband loses his temper outside his home". Not
  yet seen in game.
- Hogtied by the road ([Encounters] Hogtied, HogtiedChance 20). Away from the towns by day, once
  a day, one of a nearby town's people lies tied up off the road (a mark on the map), two men by
  them: bounty hunters for a known thief (a lawful claim), a creditor's men, or outlaws. Cut them
  loose with the game's own prompt, pay the price the men name (greet their leader) or fight;
  ride on and they're carried off. HogtiedMenDraw (on): their leader may draw on you unprovoked
  as you come near (bounty hunters never do); set it false and the men fight only once you step
  in. Testing row "Stage a hogtied captive nearby". Played once through that row: the game's
  own hogtie took, the leader warned you off, the two men came at you and were killed, and the
  captive, cut loose, thanked you with the game's own clip. Not yet seen: one met by chance, the
  price paid, the leader drawing unprovoked, a captive carried off.
- What they feel about you, as you pass ([Mind] MindPassingLasts). Someone you roughed up gives
  you a hard look when you pass them hours later; someone you helped tips their hat. Testing row
  "Someone near shows what they feel about you as you pass". In the log so far (a spit and hard
  looks among them), not yet watched on screen.
- A favour asked ([Scenes] FavoursAsked). Someone at the worst of a debt, a bad tooth or a fare
  they can't make may come up and ask you, or hint, or lose their nerve; then Talk lists the fix.
  Testing row "Someone in trouble comes to ask a favour". Not yet seen in game.
- Courting townsfolk ([Scenes] CourtFolkAges). enRAGE's townsfolk, whose ages it doesn't keep,
  now court each other on the street too. Not yet seen in game.
- Witnessed, or warned ([Scenes] WarnedYou). Someone who saw what you did may tell you so to your
  face; someone who thinks well of you may warn you of a man with a grudge near, or the law
  asking. Words only. Testing row "Someone warns you, or tells you to your face what they saw".
  Not yet seen in game.
- Chance meetings from the two minds ([Mind] MindChanceMeetings). What passes between two people
  who meet by chance near you comes from who they are: friends talk and sing, foes quarrel or
  turn their backs, the sore snap, someone with money worries trades or asks the other for a loan
  (given or refused by the other's nature), drink tells tall tales. With the model's picks on,
  the two may stand still for up to 2.5 s while it chooses. False: rolled by the place alone, as
  before. In the log so far, not yet watched on screen; the cold shoulder and loan not yet seen.
- Violence against Arthur is on as it comes: the supper invitation ([Encounters]
  SupperInvitation: at dusk on the roads, follow a too-friendly man to supper and you wake in a
  ditch at first light with most of your cash gone) and the hogtied's men drawing on you
  (HogtiedMenDraw). Set either false to turn it off. The supper invitation was off as it came, so
  an older ini has it turned on as it upgrades: set it false again to keep it off. Neither has
  been seen in game yet (the supper invitation not in any version).
- More of what people's minds choose reaches the screen. Someone keeping watch looks at you or
  the street, people sat or leaning look up from their seat, and someone who wants to be seen
  stands where the street can see them ([Mind] MindActsSeatedLooks); a choice they couldn't act
  on is tried again a minute later, up to three times (MindRetries); the street's scenes take
  turns, so fewer sharp words ([Scenes] ReasonVariety); chance meetings get their turn, nearer
  you (ChanceFirst); the same faces less often; fewer of the AI model's thoughts thrown away. Two
  with a reason to come to blows while one of them is sat or leaning have it out in words
  instead; getting them up to fight is next.
- Show minds and "Wake stages over heads (with West World)" are on the settings panel's UI page
  now, not the Testing page (they stay in [Testing] in the ini). The UI page (names over heads,
  bars, Show minds, the ticker, subtitles) now shows in the panel's short view too, beside the
  Essentials, the Hotkeys and the Mod list. The panel has 12 pages, not 16: "West World and
  stalkers", "The world and its people" and "Pacing, files and the log" each hold two or three
  sections under their own headings (the ini keeps them all). The Essentials page leaves its
  screen settings to the UI page beside it, and its pickpocket, self-defence and memory-reset
  rows are one group, "You".
- F7 adds TEST story:, TEST callouts:, TEST domestic:, TEST hogtied:, TEST nods you:, TEST
  favours:, TEST warned: and TEST chance meetings:, and TEST reasons:, TEST minds acting:, TEST
  model: and TEST picks: count more. The new Testing rows have no key as they come: set one on
  the Testing page.
- Fixes: a pick the AI model made stands until the person's own mind changes its choice; a scene
  that fails to start no longer makes its people wait; someone going over for a word goes only to
  a grown person on foot, never a lawman; a line between other people isn't cut off by another's
  within 2.5 s unless that one is nearer you; a friend's grudge over a refused request falls on
  the one who refused; the stalker never takes anyone RealNPCs is using; lawmen and stalkers
  handed back are properly released to the game; less work a frame (not measured in game).
- Settings version 172. An older ini is rewritten with the new lines and your values kept, except
  the supper invitation (above); the log lists what's new.


NEW IN 0.7.11
-------------
- Talk. Locked on to a grown-up RealNPCs knows, the game's menu offers Talk on Q (a pad's RB).
  Q is the game's own lock-on "target info" control: rebind it in the game's Settings >
  Controls and Talk and its icon follow. A short list opens above the ticker: "What's the
  news?", a drink (in a saloon), "Here, take this" ($1, or $5 once they've told you of money
  trouble), "Watch yourself.", "Goodbye.", and when they apply: "The name's Arthur Morgan.",
  asking after a worry they told you of, settling a debt ($11), "Here, for the doctor ($2)",
  "Here, see a proper dentist ($3)", "Here, toward that ticket ($5)" and "I'm sorry about
  before." And ([Encounters] FondnessActs, on): "Whiskey for the house, on me ($2)" in a
  saloon with two or more others at the bar (the one you say it to answers for the room by
  their nature, the room cheers, and everyone treated thinks better of you, the drunk most,
  each with a drink of yours to remember); a gift (forty cents: a pouch of tobacco for a
  man, a tin of peaches for a woman), which the proud take where they'd refuse money and
  the poor are fondest of; and condolences to someone grieving a friend ("I'm sorry about
  Abner."), thrown back by anyone who knows you killed them. And when a creditor is pressing
  someone in the street before you, lock on to the creditor: Talk offers "I'll cover what he
  owes you ($N)." alone; he walks off paid, the one pressed thanks you and is yours after.
  The men at a campfire you sit at are the nearest town's people from then on, and remember
  the night in its street. After dark in a town, Talk on a woman, someone in drink or a timid
  man offers "Let me see you home.": say yes and walk them to their door, 60-100 m off (fall
  behind and they wait, then go on alone); at the door they thank you, and are fonder of you by
  a lot. Testing row "Walk someone home". Someone with a quarrel still sore may give you a
  word to carry ("Any word you'd have carried to Hiram?"): for two days the other wears a small
  dot on the map while near, and Talk on them opens with the word; taken, the quarrel is
  mended, and either way both think better of you. Lock on to the piano player, the guitar or
  the banjo and Talk offers a coin for the music ($0.25): they thank you, and remember the face.
  The last row, "Say something else..." ([Mind] TalkTyped, on; keyboard only), opens a line to
  type your own words on: Enter says it, Backspace deletes, Esc leaves. They answer from who they
  are and how they stand with you: in the AI model's own words with it on, else a written answer
  by what your words seem to be about (the news, their name, how they are, their trouble, the
  weather, the way, a drink, money, God, guns, the law, thanks, sorry, goodbye). Nothing is paid
  or promised by typing.
  With the voice server running and a microphone, the row "Say it out loud..." ([Mind] TalkVoice,
  off as it comes; keyboard or pad) listens to you instead: say it and fall quiet (or Space, Enter
  or a pad's A: that's all; Backspace, Esc or B: never mind; TalkVoiceSeconds, 6, at most), the
  server writes it down (faster-whisper on the CPU, none of your card), and your words are said
  to them as if typed, the same answers either way. Nothing you say is kept or leaves the PC. The
  row is greyed, with the reason, when the server isn't running, has no transcriber (run
  install-voice.cmd again if the voices were set up before this version) or finds no microphone.
  Three more rows ([Encounters] TalkVerbs, on): "What do you know of <X>?" for someone they've
  had dealings with lately (praise, a grievance or a plain fact by how they stand with X, and a
  warning when the town knows X for a thief, a brawler or a killer; the afraid and the hostile
  carry no tales); "Could you use a loan? ($10)" (taken by most, owed three game days, then
  "About that ten dollars I lent you...": paid back by those who can, put off by those who
  can't, denied now and then by the greedy, who walk off); and "Have you met <X>?" when someone
  RealNPCs knows stands near whom they don't (a word each, a hat tipped, acquainted from then
  on: they nod to each other in passing).
- Seeing what happened. The papers (N) end with Overheard ([Display] Overheard): what went on near
  you in the last game day, one sentence each, newest first ("Simon Walsh and Ezra Whitlow came to
  blows in the saloon, two hours ago."), then Who's who. And a cue on the ticker when a scene starts
  out of your view within 22 m ([Ticker] SceneCues: "Raised voices behind you.", "A scuffle behind
  you.", "Laughter behind you."), at most once a minute.
  Up and down with the D-pad or the mouse wheel, Space or Enter (A) to say it,
  Backspace (B) to leave. They answer from who they are and how they stand with you, in their
  own recorded voice; with the AI model on, in its words, the meaning kept (the written line if
  it takes longer than 2.8 s). A settled debt or a mended trouble changes their story and is
  kept in the save; tell someone your name and they say it from then on; someone you helped may
  come and thank you hours or days later (the proud pay the $11 back); someone lonely who knows
  you may come over hoping for a word. While you talk, nothing else of RealNPCs' takes them.
  [World] InteractionMenu, TalkKey.
  Talk is a list first, with your own words, typed or spoken, at its foot. The aim is real-time
  talk in your own voice with the model throughout, where your hardware allows.
- Brawls that grow. A fistfight draws people in a wave at a time, up to [Mind] BrawlMost (12)
  fighting: friends and grudges first, from up to BrawlJoinMetres (30) away, most of all in a
  saloon with drink in it. Those sitting or leaning get up to join. The sides balance, a man
  going down no longer ends it (another of his side leads), and it lasts at least
  BrawlLeastSeconds (45) unless a side is all down. A crowd of up to 16 rings it and cheers,
  from BrawlCrowdMetres (40). The law breaks it up (BrawlLaw): those who stop are let off with a
  word, those who keep swinging get the lawman's fists and are hogtied, and a second lawman
  comes when three or more hold out. In a brawl of four or more near you, someone may turn on
  you (BrawlPullsYouIn); rarely, a losing armed man draws (BrawlDraws, BrawlDrawShare 40).
  Every man beaten down is knocked out (down 20-40 s, then up and on his way) or killed
  (BrawlFistsFatal, 50, moved by who is in the fight; booked as "a beating"). One of enRAGE's
  people is only ever knocked out. Not yet seen in game: fists that kill or knock out.
- A braver town ([World] Bravery, 85, on the Essentials page). Brave townsfolk stand their
  ground where the game would send them running (the odd coward still runs), fistfighters
  don't scatter the street, and the mod's own frights send fewer running. Whoever you aim at or
  lock on to gets the game's own reaction back for 20 s, so hold-ups and robberies work as
  before. Never children, story characters, lawmen, enRAGE's people or other scripts' people;
  everyone is put back when a story mission starts. 0 is everything as before. Not yet seen in
  game: whether witnesses still run for the law, and how a brave robbery victim answers.
- The stalker in black, and more often ([Stalkers], on the Essentials page). StalkerBlack (on):
  the stalker is a stranger who turns up out of sight behind you, in StalkerBlackOutfit of
  StalkerBlackModel. StalkerEveryMinutes 15 (was 34) and StalkerChance 35 (was 20): about once
  in 45 minutes of play, where it was about once in three hours. An ini on the old stock values
  moves to the new ones; values of your own are kept. He keeps low, crouched until he runs,
  shows as a red dot on the map, and dealing with him is no crime. The Testing row "Stalker outfit: show the next
  candidate" (TestStalkerOutfitKey) shows other outfits and says the two lines to copy.
- The free local AI model turns itself on. AI MODEL (OLLAMA) ([Mind] Language) is the large
  gold switch at the top of the settings panel, with its status beside it ("on, answering",
  "on, NOT ANSWERING", "off"...). At each start RealNPCs asks Ollama; if it runs and holds
  llama3.1:8b, and you have never chosen, the model is turned on and a notice says so once.
  Turn it off and it stays off ([Mind] LanguageChosen). See OPTIONAL, RECOMMENDED above.
- Hook up is on H now, the same key as Flirt: after her yes, once she has said it, the H prompt
  reads Hook up. Nothing of RealNPCs' is on G (enRAGE's Give Money). With CasualHookups, press B
  for its scene when it takes her kind; for a kind it won't take, RealNPCs' own night follows
  (at once, or after 15 s with no scene), so her yes is never left hanging.
- Thoughts that vary ([Mind] LanguageVariety, with the model): each thought is about one thing,
  in a shape of its own, and steers clear of their last few (the weather came up in 23% of
  sampled thoughts before, 10% now). Nobody wonders whether the world is real without cause. A
  doubt the model voices can start the waking (ModelWakes). The model picks for people farther
  off and more at once (PickMetres 25, PicksAtOnce 8), and can choose a person's own reason with
  someone near, which starts that scene (MindStartsScenes). [WestWorld] AwakeningShare 2.25
  (was 3).
- Word of a killing travels slowly, and only from witnesses. Seen, it reaches the dead man's
  kin in 2-5 game days ([Features] KinWordDaysMin, KinWordDaysMax; it was 12-60 game hours).
  With nobody alive within 35 m, or in self-defence, word goes only once the body is found, or
  after 5-10 days (UnseenKinWordDaysMin, UnseenKinWordDaysMax), and doesn't name you: no
  letter, nobody rides after you. The telegram says how, where and when. Not yet seen in game.
- Gatherings of the awake at any hour ([Encounters] GatheringHours any, or night): by day too,
  out in the wilds or just outside a town's edge, never in the street or indoors.
  GatheringGapHours (24) between them. By clear day they come a quarter as often, and only with nobody else in sight; fog and rain count as the dark. Some are gone when you look back.
- Lawmen carry bodies off the street ([World] LawClearsBodies): a body left 20 s or more in a
  town street is lifted by the nearest lawman on foot within 50 m, the game undertaker's way,
  and carried to a spot he can reach. Testing row "A lawman clears a body". Seen working in
  game (carried 12 m and set down); jogging with it is new and not yet seen.
- Name plates stay up in crowds and after a saloon: the nearest eight are always drawn. After
  going in and out of a saloon every name could go for minutes, because the list of people in
  the world came back empty; RealNPCs now keeps the last good list while there's trouble.
- Bold thieves ([Scenes] BoldThieves, 25): that percent of would-be thieves try it anyway with
  the law looking on (a hothead, a greedy one or a drunk 38%). By the church on a Sunday every
  thief still waits.
- Whoever you aim at, or lock on to with a gun out, is let go at once from anything of
  RealNPCs' that held them (a street scene, a fight, the ring, a waking act, Talk, a walk-up to
  you, a lawman's job, Bravery), so the game's own reaction takes over: hands up, begging.
- Also new, mostly not yet seen in game: the street's trouble waits while a lawman within 25 m
  is looking, and on a Sunday morning by the church ([Scenes] BeingWatched); the street no longer
  hushes after a killing unless you ask for it ([Mind] HushHours, HushHoursSaintDenis, 0),
  never after a brawl, and the town is on edge for some hours ([Mind] MindTownMood); onlookers run
  from real danger, and fright spreads; stress that builds, and honour rules in Lemoyne ([Mind]
  MindTempers); personal space ([World] PersonalSpace); why a scene starts, as a thought
  ([Scenes] ReasonThoughts); the bully turns on whoever stepped in; Arthur's own recorded
  fighting lines ([World] ArthurSpeaks); a lawman steps in on a waking lash-out ([Scenes]
  LawChases); a worry, and what they knew of you, carry over when enRAGE takes someone up.
- Fixes: the people list after a saloon; the lawman's carry (the lift cancelled, standing still
  and dropping the body, taken back by the game, dropped at a catalogue or the barber); the
  game's own knockouts after a fight went unrolled; one man picked for both sides; every
  joiner going for one man; one thing at a time for each person (chance meetings no longer take
  the person you're talking to; one walk-up at a time; no greeting straight after goodbye; no
  pickpocket during a scene); one name on the lock-on prompt and over the head; carrying a body
  you didn't kill is no crime, for good; subtitles between others only for speakers on screen;
  an ini missing only a Testing row no longer undoes your own values; the reload fix; smoother
  frames.
- Settings version 164.

NEW IN 0.7.10
-------------
- A fix for the game freezing in busy towns (Saint Denis above all). RealNPCs read what Arthur was
  wearing every few seconds, so that people could react to him naked or half-dressed; that
  reading could stop the game dead. It no longer reads clothes at all, so for now nobody remarks
  on Arthur being undressed. Everything else is as in 0.7.9.

NEW IN 0.7.9
------------
- Stripping is held back: the first time one of the awake stripped in testing, the game froze.
  Nothing strips now, whatever WakeStripChance says, until the cause is found. Everything else
  of 0.7.8 is unchanged.

NEW IN 0.7.8
------------
- The waking stay strange. From the second stage, a waking person near you no longer slips
  back into their day between acts: they stand dazed, drift, pace or freeze, for up to three
  minutes, and are let go at once for a fight, gunfire, the law or a street scene.
- They use their hands and bodies as they act (rubbing their eyes, pointing at the sky, a hand
  to the head, pleading), and what they say shows as subtitles.
- From the third stage they bother the people around them: right up close and staring, a hand
  waved before a face, following someone step for step, "you're not real", preaching. The
  other walks off, curses, laughs or takes fright.
- A quarter of holds at the third and fourth stages end with them turning on you or on someone
  near: fists, or their own gun once fully awake. Stopping them is self-defence.
- A few of the fully awake (5%), once each, strip bare where they stand. Never enRAGE's people.
- A waking person you greet keeps their stage when enRAGE takes them up.
- All under [WestWorld]: WakeHold, WakeHoldMinutes, WakeGestures, WakeStrange, WakeStripChance,
  WakeLashOut. Testing page: "Strange waking acts, the next in turn".

NEW IN 0.7.7
------------
- The settings panel (Alt+R) and people turning to look work on other builds of Script Hook
  RDR2 .NET V2. On some builds the panel failed as it opened and switched itself off; the
  log said "Could not load type 'RDR2.Native._NAMESPACE30'". RealNPCs now calls those two
  game functions directly, whatever the build names them.

NEW IN 0.7.6
------------
- Killing a pickpocket who robbed you is self-defence, never murder. The game's pursuit could
  outlast the clear of the bounty and wanted level; now the game's record of the crime is
  wiped too (what you owed before is kept), and if the law is still after you, it's switched
  off for a moment to end the chase. Under [World] SelfDefenceNoBounty.

NEW IN 0.7.5
------------
- Alt+R keeps working. Each part of RealNPCs switches itself off if it keeps failing, and the
  settings panel's count of errors ran all session, so a rare error every so often could leave
  it off until the game restarted. Now a part switches off only for errors close together, and
  the settings key always brings the panel back. The settings keys also work with Shift or Ctrl
  held (sprinting, crouching, or a key Windows thinks is still down after Alt+Tab).
- If Alt+R still does nothing, try Alt+F (the second settings key), and send RealNPCs.log
  (Documents\Rockstar Games\Red Dead Redemption 2\RealNPCs) with a bug report.

NEW IN 0.7.4
------------
- After a fight: a friend of the one who lost goes to them and helps them up, and a lawman
  nearby has a word with the winner. [Scenes] Aftermath.
- A crowd gathers in a ring around a fistfight and cheers it on. [Scenes] FightCrowds. When you
  start a fistfight near others, a friend of the man you hit, or someone against you, may come
  at you, and someone fond of you may take your side.
- A quarrel may end in shots, when the one who has had enough is armed: deadly, true to 1899.
  [Mind] LadderShots.
- Rival gangs who meet fight it out. [Scenes] GangShootouts.
- The night: a mugging in a dark street (the mugger runs, and a lawman who catches a thief
  hogties him), and a drunk carried or walked home by a friend. [Scenes] NightScenes. A horse
  thief rides off on a hitched horse, and the owner shouts for help. [Scenes] HorseThieves.
- Plans over days: someone wronged means to get even, someone helped means to thank them,
  someone in debt means to pay it, a sweetheart means to court. Near you, they go and find that
  person, and the plan shapes their day (the store for a gift, the work over the saloon, a
  brooding drink). [Mind] MindPlans.
- People go to the real saloon, store and church of their town, more street scenes play at once
  ([Scenes] ScenesAtOnce, 4 now, up to 6), and people who meet by chance stop for a word.
- What you're wearing, or not: someone naked or half dressed is noticed (shocked, amused or
  disgusted). Mud is remarked on less.
- The law: self-defence holds for ten minutes after the fight (a witness who goes to the law
  later can't make it murder); a trampling by someone else's horse or wagon isn't charged to
  you; nor is carrying a body you didn't kill.
- Names: lawmen, sheriffs and counter keepers say so on their cards; names show inside big
  buildings and in Saint Denis; a stranger you greet keeps the name you saw. Pickpockets keep to
  their trade, and say so on their card once they've robbed you.
- Shouts carry: anger, fright, "Thief!" and fight talk are shouted the game's way and heard from
  down the street. [Scenes] ShoutsCarry.
- Two subtitle boxes: what's said to Arthur in the game's place, what people say to each other
  just under it. No thought is ever a subtitle.
- The bar lists every scene nearby, in turn when they don't fit; T steps through them.
- A working girl's own come-on, now and then, wherever she flirts.
- Fewer people wake, and slowly: [WestWorld] AwakeningShare (3, can be a fraction),
  AwakeningDays (5).
- With a local language model ([Mind] Language, off unless you set it up; see the settings):
  thoughts worded by it, the model picking what people near you do from what their minds allow
  ([Mind] LanguagePicks), and thoughts said aloud in their own recorded voice when one of their
  remarks fits ([Mind] ThoughtsAloud). In testing.

NEW IN 0.7.3
------------
- Several street scenes at once: while one plays near you, and between scenes, other people
  elsewhere on the street have quiet talks of their own (gossip, small talk, the old days),
  words only, in their own voices, with a subtitle when they're near. [Scenes] ScenesAtOnce (2;
  1-3; 1 is one scene at a time, as before).
- With [Mind] MindLive on, every argument or fistfight near you comes from the two people's
  minds: whether it stays words, comes to a shove or to blows is theirs. A man's hand against a
  woman at a tenth of his rate, as in 1899.
- Fights spread: while two people trade blows, a friend of one, someone with bad blood against
  the other, or a drunk hothead in a saloon may join in on a side; most just watch, fewer the
  bigger the crowd, nobody with the law at hand; at most three join. [Mind] MindJoinFights.
- People act on their own choices: someone near you takes a stroll, sits down on the nearest
  bench or chair, goes to the bar or the card table, gets on with the work, heads off home, keeps
  clear of you or of someone they can't abide, or goes over to a friend for a word. A few at a
  time, never someone already sat or busy. [Mind] MindActs.
- Names over heads always on: a name over everyone within 25 m, hidden only by walls (not a
  table or someone in the way). [Display] NameCardsAlways, NameCardsMetres (25).
- The bar names the street scene near you and where it is: "... - 10 m south".
- The fistfight test takes any two people, women too. Nobody brawls from a chair, a man at the
  bar gets up before a fight instead of going into a T pose, a pickpocket who stands still is
  sent running again, and a wake stage tag sits on top of a thought over the head.

NEW IN 0.7.2
------------
- The minds act: [Mind] MindLive is on for everyone now (an ini where it was off is switched on
  once; turn it off there if you'd rather not). Each part below has its own switch.
- The settings panel pauses the world while it's open (it used to slow it), and the mouse works
  on it: point at a row, click to change it, scroll with the wheel, drag the scrollbar.
- Someone who steps into a quarrel near you (to stand up to a bully, to talk a friend down, a
  jealous husband or wife) walks all the way over and says it to their faces, instead of calling
  it out from where they stood.
- With [Mind] MindLive on: who steps in comes from their own minds. Each onlooker
  takes it their own way: a friend of the one being picked on, a kind soul, a hothead will often
  step in, a coward or a cold man seldom; the bigger the crowd, the less each one feels it falls
  to them; a lawman nearby or a church hushes it. Women step in too, half as often as men for
  now. Whether the bully backs down comes from his own nature: a hothead or a proud man faced by
  another man holds his ground, a proud man shamed by a woman before the street mostly backs down.
  When he holds his ground, the one who stepped in may lead the other away instead, and it
  never comes to blows. [Mind] MindStandUp turns this part off.
- With [Mind] MindLive on: someone who only watched a quarrel, a fight or something you did
  does something about it by what they feel: the frightened walk off (a man who keeps his fear
  off his face slips away quietly), the pious shake their heads and leave, a gun or something
  uncanny roots them to the spot, contempt shakes its head, a keen eye turns to watch. Two
  people at most each time, never the law or a shopkeeper. [Mind] MindOnlookers.
- With [Mind] MindLive on (and West World): the Awakening follows what people live through.
  Something uncanny, a killing seen, a man they saw die walking past them again shake their sense
  that things are as they seem, so they wake sooner; a kindness steadies them. The moment that
  sense gives way they begin to stir. [Mind] MindWakes.
- With [Mind] MindLive on: people who feel something about you show it as you pass: a hat tip
  and a greeting from someone you helped, a hard look from someone you wronged (a sullen look
  away if they fear you), a glance from the frightened.
- With [Mind] MindLive on: what passes between people who meet near you leans by the day
  they're having: sore people come to words sooner, glad and lonely ones stop to talk.
  [Mind] MindMeetings.
- With [Mind] MindLive on (West World): despair isn't only an ending. When someone awake begins
  their last words, a friend or a kind stranger near them may walk over and try to talk them
  down; a friend is heeded more often. [Mind] MindRescue, RescueChance.
- With [Mind] MindLive on: when someone falls near you, whoever is most moved by it comes to
  help, and how they help is theirs: a hand up, a gruff one, or walking on. [Mind] MindHelps.
- With [Mind] MindLive on: people who know each other greet each other by how they feel
  lately and the day they're having: sore at a friend, a nod and no word; afraid, eyes away;
  grateful, a warm word; a glad day, a good day even to a near stranger. Someone who feels
  something about you from a while back shows it as you pass, the same way. [Mind] MindPassing.
- With [Mind] MindLive on: your papers end with Who's who, the people you have met, newest
  first: who they are, how they take you, and who they are friends or at odds with. [Display] WhosWho.
- (In testing, off as it comes) [World] OwnLooks: without enRAGE, someone you've met is known
  again by their look (the same clothes on the same build), so yesterday's townsman is the same
  man today. With enRAGE installed, enRAGE's own knowing by look is used instead.
- Women come to blows too, with a man or another woman, half as often as two men do for each
  woman in it ([Scenes] WomenInFights; off: only two men ever fight, as before).
- A hermit at their camp has a name over their head like anyone else.
- Flirting: a willing woman's voice no longer turns you down (a late "slow down" from an
  earlier flirt could play over her yes).

NEW IN 0.7.1
------------
- Names over heads are easier to hide. The settings panel page is now "UI (Nameplates, bars,
  etc)", with every on-screen setting on it,
  and turning Names over heads off there also hides enRAGE's own cards when enRAGE is installed
  (its [Menu] NameCards set to 0, after a copy of NemesisMod.ini). New: Personality line under
  the names ([Display] NameCardsSecondLine): off shows the name alone.
- What people say near you (a street scene, a hello, the doctor, the law chase) shows as a
  subtitle the game's way, never on the bar at the bottom, which keeps the news. Thoughts stay
  over heads. [Output] Subtitles = off hides it.
- Self-defence the law honours: the pickpocket who robbed you, anyone who comes at you, and a
  bully laying into someone (a red dot on him while he's fair game) can be dealt with and it's no
  crime, the way the game marks its own thieves; the man you saved thanks you. Bumping into
  someone on the way no longer spoils it.
- Someone you haven't met is "Stranger" over their head, with no name, until you greet them.
- The settings panel opens on the essentials (a page of key settings, the hotkeys, the mod
  list); F switches to every setting and the Testing page.
- A Mod list page on the settings panel turns your other mods off and on (scripts with the next
  Insert reload, ASI plugins when the game next starts); nothing is deleted.
- Reset RealNPCs Memory: at a station clerk, hold Q (pad RB) and everyone forgets what they know
  and feel about you (enRAGE's townsfolk too, when it's installed); the people and their lives
  stay, and copies of the saves are kept. [Features] ResetAtClerk.
- The middle bar under people reads "Fondness" (was "Liking"), and RealNPCs' subtitles sit just
  under the game's own, so the two never overlap.
- Pickpockets walk straight up to you, on foot (never a man with his horse at hand), close
  enough to take it, and their dot turns red when they run.

NEW IN 0.7.0
------------
- RealNPCs no longer needs enRAGE. It runs on its own (the townsfolk, their ties and memories,
  the street scenes, the minds, the names over heads are all its own) and stays compatible: with
  enRAGE installed it reads its nemeses and townsfolk and gives them lives too.
- Names over heads, the way enRAGE drew them: the people RealNPCs knows, the game's own named
  people (the sheriffs, the story cast) and the gangs' men ("Lemoyne Raider - Stranger", "An
  O'Driscoll"). "Stranger" until you've met them. [Display] NameCards.
- Pickpockets: a man may bump into you anywhere and lift some of your cash, then run, a red dot
  on the map and fair game; catch him to get it back. Street pickpockets come three times as
  often, and a lawman near runs a thief down and tackles him. [Scenes] Lifts, LawChases.
- The doctor sees people for their own complaints (a tooth, a back, a cough, ribs after a fight,
  a child's fever): they tell him what's wrong and he answers, aloud.
- People who know each other say hello and goodbye in their own voices as they pass, and what
  people say in a scene near you shows on the ticker as they say it.
- A livelier street (a scene a minute at most), people walking briskly on their errands, the
  world feeling unreal only to those who have begun to wake (thirty thoughts a stage now), a
  quicker despair, and the settings panel on Alt+R (and Alt+F).
- From the night run and the day after: a mind under every person (traits, needs, feelings,
  memories that fade and get retold, beliefs), quarrels that climb or cool by who the two are,
  thoughts that vary, manners of 1899, and the rest below.
- Fixes: a controller binding of "+" alone no longer stops RealNPCs starting; the test cast fit
  their parts for the mind too; the supper invitation's host wanders off when nobody comes, and a
  fault in the dark no longer says your pockets were turned out; Show minds' cards cost less in a
  crowd; a talk cut short isn't remembered as small talk.
- From two players' reports: the dead tell nothing (a man killed after an interrogation no longer
  goes on telling the town how he was questioned when enRAGE missed his death); RealNPCs' own
  avengers come one at a time ([World] AvengersAtOnce, AvengerGapHours; the "X heard you killed
  his friend" notices are enRAGE's, off with [Menu] Messages = 2 in NemesisMod.ini); a townsperson enRAGE
  takes for someone you greeted, by his look, now says only that your face seems known. The
  "X is walking toward you" notice is gone, and the relative who walks up is picked and steered so they
  reach you (one a town a day); they're named when they speak. Testing page: "Kin of someone you know walks up".
- Show minds (the cards over people near you) is off as it comes; Ctrl+F2 or the Testing page's
  Switches turn it on, and an ini that already has it on keeps it.
- Strangers at a campfire: at night out of the towns, men round a fire call you over for
  coffee and a tale; now and then a traveller walks in from the dark. [Encounters] Campfires.
- The voice below the floor (West World): late at night in a saloon, a muffled plea from under
  the boards behind the bar; the barkeep says pipes, and thinks otherwise. [Encounters] VoiceBelow.
- The doctor's patient: by day, someone whose child has the fever walks to the doctor's and
  back, thinking it over. Now anyone may go for what ails them (a tooth, a back, a cough, ribs after a
  fight, a child's fever), say it to the doctor and hear his word aloud. [Scenes] DoctorsPatient.
- A lawman near runs down a pickpocket and brings him down for arrest: [Scenes] LawChases.
- Anywhere, a man may bump into you and lift some of your cash, then run: catch him to get it back.
  [Scenes] Lifts.
- People walk briskly on their errands (to a scene, to the doctor, to a talk, home again):
  [Scenes] ErrandPace (2; 1 an ordinary walk, 3 a run).
- People think of what happened to them in more ways: every memory has four or more lines
  (over a thousand in all, from five hundred), so a brawl or a kindness isn't recalled in the
  same two sentences all week; the weather, the shop they're in and the people in sight too,
  what they tell you they do for a living, the small thoughts of a quiet day, and every worry's
  crisis and its ending (about 1,200 new lines in all).
- Manners of 1899: a friend is someone you've had two scenes with (one kind word no longer
  makes a mourner); a respectable woman doesn't drink herself drunk, stand rounds or spit,
  and shames a bully rather than squaring up; strangers are "mister" and "ma'am"; he and she,
  never they; the old men and the sweethearts have twice the talks.
- The mind in shadow goes on: the man you assault judges it himself (his MIND line in the log),
  an execution on the road counts as a killing seen, the night visitor as the uncanny. With
  [Mind] MindLive on, people's thoughts from their feelings have twelve lines each instead of
  three, say the mask when the face hides the feeling, and a hungry farmhand doesn't sound like a
  hungry clerk. Memory behaves like memory: after a talk one tells the other the freshest thing
  worth telling, the hearer keeps it a little less surely, and what was heard drifts in the retelling
  (the unnamed doer becomes someone the hearer dislikes, a slight grows, two men become three); what
  someone saw never changes who did it, and what you did never drifts. Standing where something
  happened brings it back. The one spoken to in a street scene answers with their body from
  their own mind and the manners of 1899. Off (as it comes), thoughts, memories and reactions
  are exactly as in 0.6.3. With it on, a quarrel between two people the mind knows climbs or
  cools by their natures and the room (words, a shove, fists, a gun drawn; an apology, a joke,
  a friend stepping in, a drink, the law looking on); the shot is never fired. [Mind]
  QuarrelLadder (on, under MindLive). No two people near you think the same shape of thought within an hour,
  and a kind of thought that has had the last few turns gives way to the others ([Mind]
  ThoughtVariety, on).
  With Ollama on your PC and [Mind] Language on (off as it comes), the mind's own thoughts are
  worded by a local model and checked line by line; the written line shows whenever the model's
  won't do. Nothing leaves the machine.
  And the mind now says what each person near you would do (the log's MIND would: lines, the
  card's "would:"): a meal, the work, the saloon of an evening, church on Sunday, a word with a
  friend, stepping clear of a foe. Nobody is moved by it yet. And what each believes: the log
  says when a belief flips (MIND believes: "now that stranger is a dangerous man"). Nothing acts
  on it yet. [Mind] MindLevels (off, in testing) spreads the mind's work by distance and a
  budget a frame: the people in view every look, the town around now and then, everyone saved
  elsewhere once a game day.

NEW IN 0.6.3
------------
- A livelier street. People deal with each other around you far more often: brawls between
  hot-blooded strangers by day or night, saloon fights at any hour, someone standing up to a
  bully (and the fight that follows, with real punches), falls and a hand up, rounds bought,
  grudges taken up, a mother asking after her lost child, a street preacher any day, two old
  men arguing over the old days, a man calling another out at noon, condolences in the street,
  sweethearts falling out and making up, a debt paid in public, a card cheat shown the door,
  the Sabbath, and a bell at three only the awake hear. [Scenes] TroubleWeight (1 as built,
  2 twice as often, 0 never by chance) sets how often people come to blows or quarrel.
- Night encounters: a figure crouched over a body who stands and stares, then turns up behind
  you; a stranded traveller asking the way (lost, snakebitten, or being robbed on the road);
  and the supper invitation, on as it comes (CONTENT NOTE: accepting it costs you
  most of your cash and the night, and it can't be undone; [Encounters] SupperInvitation).
- People notice how you look (bloodied, drunk, filthy, soaked) and take it by their nature, and
  people who know each other nod, tip a hat or wave as they pass (enemies give a hard look).
- The dead are kept in a ledger shared with enRAGE, so a death it missed is still a death.
  enRAGE's own records can stalk you and can despair (CONTENT NOTE; booked in enRAGE's history).
- Despair has a voice: the farewell in the person's own recorded voice, chosen by temperament.
- A mind under every person, run beside the game and only reported while it's tested: traits,
  needs, worries, what they feel about what happens near you, their mood. Show minds (Ctrl+F2)
  puts a card over the people near you; in a crowd the nearest has the full card and the rest a
  line, never overlapping. [Mind] MindShadow, MindLive (on).
- Controllers: hold RB and press D-pad up for the settings panel, left for F7, right for your
  letters; set your own on the Hotkeys page from the controller ([Controller]).
- Subtitles, one set at a time: the game draws its own white subtitle for any recorded line
  while its Subtitles option is on, so RealNPCs' boxed ones show only while that option is off
  ([Output] Subtitles = game); "ours" shows RealNPCs' always.
- The settings panel's Testing page fires everything RealNPCs can do, by key or by Enter, and
  "Bring the test cast here" brings eight people the tests need, kept in your save.
- The world slows while the settings panel is open. Saves happen in quiet moments (loading,
  paused, faded out) and never put an empty save over a real one.
- Seen working in game: the street scenes above (the brawl, the stand-up, the fall, the lost
  child, the old days, the preacher, the round's words), the test cast, the mind cards, despair's
  farewell, the stalker, the execution. Not yet seen: the controller buttons, the supper
  invitation, and several of the rarer scenes (the callout, the Sabbath, the bell).

NEW IN 0.6.2
------------
- Thoughts above their heads. The thought of the person whose turn it is shows over them in small
  type, one person at a time, for the people within 20 m that you can see (it was 30). News and
  the world summary stay on the bar at the bottom. [Ticker] ThoughtsOverHeads,
  ThoughtsOverHeadScale (0.24); with it off, thoughts go on the bar as before.
- News has a box of its own, "Around town", in the top right under the "Sore at you" panel.
- No more white text. Subtitles look like the game's own now (white serif text in a dark box),
  only for what people say to you, and only while their voice is playing. Notices are the game's
  own updates at the top left, and so are enRAGE's territory changes ([Output] TerritoryNews).
- Conversations in their real voices. About every two minutes, two people standing near you have
  a word in their own recorded voices: the weather, how they're getting on, a piece of news, a
  hello. They remark on riders going by, tell a story about their town, sweethearts pay each other
  compliments, and in a saloon of an evening there are crude remarks (CrudeTalk = false leaves
  those out). Overheard talk has no subtitle. [Scenes] RealVoiceTalk, RealVoiceTalkSeconds (120).
- Trouble between people, each time for a reason: a man with a grudge and a temper smashes the
  other's face into the bar, a table or a wall (RoughHandling); pickpockets among the townsfolk
  (NpcThieves); drunks in a saloon at night who call a man out and fall on their face before a
  punch is thrown (DrunkChallenges); someone short of money asking a stranger for a coin; the
  timid crossing the street to avoid whoever hurt them; a friend seeing to someone who was roughed
  up. The town remembers who did what.
- People react to what's said and done to them, each by their own nature. What's around them is
  on their minds too: a scene they just saw, the people in sight, the weather, the place.
- People flirt with each other and court. You see it in their thoughts: the one asking working up
  to it, the other as they answer. A yes or a maybe becomes a romance that goes on each time they
  meet. [Romance] NpcFlirting.
- Flirting: lock on to her and press H (Flirt in the game's lock-on menu, FlirtPrompt). When she
  says yes and has said it, the same H becomes Hook up (never before a yes, never on a no;
  nothing of RealNPCs' is on G, which enRAGE uses). There is no keyboard flirt key now
  (FlirtKey, None; it was F2). Whether they take to it comes from what they think of you and who they
  are: their nature, faith, troubles and mood, against your money and your name in town.
- enRAGE's own people (its nemeses, gang men and named folk) join in street scenes, reactions,
  talk and waking acts, unless enRAGE has them busy. [Scenes] EnragePeopleJoinIn.
- CONTENT NOTE: despair no longer needs a gun on them. Someone fully awake who can't bear it does it
  the way that fits them: a gun that suits them (drawn, and held a moment, before the shot), a
  knife, or running at a wall. It can be one of
  RealNPCs' own people or one of enRAGE's townsfolk, never enRAGE's nemeses or gang men, and never
  with you within 3 m. AwakeningDespair (50; 0 turns it off). Everyone gets one fresh chance.
- F7 marks the fully awake around you on the map for a minute, with what they'd do, and names the
  nearest who could.
- Self-defence lasts as long as the fight: whoever came at you stays fair game while they fight on,
  draw on you or stand at your gunpoint.
- The settings panel opens with Alt+F too, scrolls with the mouse wheel, and a notice after loading
  says which key opens it.
- Seen working in game: the thoughts above heads, the map marks, the real-voice conversations,
  the bar slam and the reactions to it, Flirt in the lock-on menu, the new subtitles, and the knife
  and gun endings.
- Known issues: about one walk-over in ten still gives up before the two meet. The drunk's
  challenge, begging and the wall ending haven't been seen in a game yet. The stalker's digging,
  hammering and scrubbing don't start yet.


NEW IN 0.6.1
------------
- Built for enRAGE 0.9.5.3.
- Each town keeps its own people. When the game puts an ordinary person in the street out of your
  sight, they become one of that town's regulars: someone met there before, back in their own
  clothes, or, while the town has room, someone new with an outfit of their own. Their names,
  worries and lives go on between visits, and the ones you've met know you again. Nobody changes
  clothes in front of you, and nobody enRAGE has already named is touched. The town keeps time
  ([World] RegularsRoutines): each regular has a routine read off who they are (the early up at
  dawn, work and errands by day, the saloon of an evening, church on Sunday morning for those
  who go, home at night but for the owls), and who a passer-by becomes follows the hour and the
  spot: the drinkers are the ones at the bar of an evening, the churchgoers the ones in church on
  Sunday. Nobody walks across town for it, and a regular caught out of their hour thinks so.
  [World] Regulars, RegularsPerTown (30), RegularsDress, RegularsRoutines.
- The street reacts to other people's fights ([Scenes] StreetReacts). The moments of a scene
  between other people (the fists flying, two men squaring up, a gun out, a man killed or
  knocked out) are raised as the game's own shocking events, so its crowd does what it does for
  your fights: looking, backing off, running, calling the law, by its own rules. Held in a town
  for thirty game minutes when one brings the law into combat, a gun out of someone else's
  holster or people fleeing beside you; never on a mission, with the law after you, or beside
  enRAGE's own fighters.
- Reputation in the street ([Scenes] StreetReputation). An outlaw walking in a town (enRAGE's
  gunmen, or a resident the law wants) is looked after by the people near, by their natures: a
  look that follows, a step off his line, the old and the timid turning away, the proud staring
  him down; the doctor, a preacher, the sheriff and a keeper out of doors get a hat tipped, a nod
  and a word ("Morning, Doc."). One witness mutters it on the ticker. A known killer or
  peacemaker in a town gets a thought from a witness, never a task.
- Ordinary people have lives of their own. Each one is going through something that fits their
  trade and age: a debt, a sick child, a letter that doesn't come, a feud over a fence, a
  courtship, the drink, a secret. It runs about eight game days in their thoughts (it begins,
  gets worse, comes to a head, and ends well or badly) with the same named people all through.
  When the other person in a debt, a feud or a courtship can be met, it's a real regular in town,
  who thinks about it from their side. Thoughts also follow the day (meals, Saturday night,
  Sunday church), the year (1899, or 1907 in the epilogue) and what you did in town lately.
  enRAGE's townsfolk think about their own lives too when they don't feel strongly about you.
- CONTENT NOTE: a sick child doesn't always get better. About a third of those worries end with a
  burial.
- The ticker gives up to three people near you a turn, and the person right beside you every
  other turn, with a new thought about every 30 seconds.
- Street scenes happen for a reason, and where you can see them. A quarrel goes on, and between
  two hot-tempered men it comes to blows. Sweethearts flirt, and in time one asks. A debt is
  called in. Someone at the worst of a bad week snaps at whoever speaks to them; someone whose
  trouble just ended tells it. If the two are apart, one walks over first. A pair's story moves
  on each time they meet, and what happened is remembered: for days they think about each other
  by name, and the town gossips about it. If one of them has died since, the other thinks of that
  instead: guilt after a quarrel, grief for a friend.
- People notice. Bystanders turn to look, and one may call out: taking a side, trying to calm it,
  or egging it on. People make the gestures that go with what they say.
- Bars under the people near you show their fear of you, how much they like you and their anger
  at you, as enRAGE keeps them. A "Sore at you" panel in the top right lists the strangers whose
  grudge against you is building, with a mark where they'd become a nemesis, and the nemesis
  after you now. [Display] FeelingBars, FeelingBarsMetres (15), FeelingBarsShowEmpty, GrudgeMeter;
  in the panel (Alt+R or Alt+F), "Bars and meter".
- Names over heads, the way enRAGE drew them: people RealNPCs knows (the town regulars, anyone
  who has thought, spoken with you or been in a street scene, the game's own named people such as
  the sheriffs and the story cast; enRAGE's people when it runs) carry
  their name in the game's title type, with a temper, a trade or "Stranger" under it, steadied as
  they walk, hidden behind walls, fading with distance. [Display] NameCards (known; off, or
  everyone for a grey "A townsman" over the rest), NameCardsMetres (25), NameCardsKnownMetres (75);
  while enRAGE draws its own cards ours stay off unless NameCardsBesideEnrage.
- Standing still no longer holds up enRAGE's funerals or someone's evening at the saloon.
- Aiming at one of enRAGE's outlaws leaves him to enRAGE (he stands his ground and his crew draws
  on you). Ordinary townsfolk still plead.
- Stalkers make gestures Story Mode has (a hat tip, a hand up for quiet, a slow head-shake...),
  crouch as they creep up, and sprint in to 2-30 m before doing odd things.
- F7 shows the town regulars around you on the map for a minute.
- Fixes: the names over street-scene lines match the names inside them; the paper reader no
  longer answers peaceful news as if it were a killing; "Thought he should know" instead of
  "Thought him should know"; rumours keep a name's capital.
- Known issues: about one walk-over in ten gives up before the two meet, and the scene is
  dropped. The stalker's digging, hammering and scrubbing don't start yet.


NEW IN 0.6.0
------------
- New name: enRAGE Westworld is now RealNPCs. The file is RealNPCs.dll and its folder in Documents is
  RealNPCs. If you had the old version: DELETE EnrageWestworld.dll from the scripts folder (RealNPCs
  won't run next to it, and tells you so). Your saves, diaries and settings are copied over from
  the old enRAGE-Westworld folder the first time RealNPCs starts; the old folder is left as it was.
- Two master switches. WestWorld turns the whole West World side on or off: the Awakening, people
  wondering if the world is real, glitches, and the dead who wake. Off, nothing of it happens and
  people already waking go quiet (they pick up where they left off if you turn it back on).
  Stalkers is its own switch now, with its own settings: how often and how likely, how
  far back they follow, how close they come, how far they run off, whether they mutter, and what
  they do when you stand still (StalkerWhenStill: circle you close, watch you from where they are,
  run straight at you and stop a few steps short, do odd things, or a mix - they never attack).
- A new settings panel on Alt+F11, with larger text: RealNPCs, West World, Stalkers and every enRAGE
  setting in one place. Every key, RealNPCs' and enRAGE's, is on one Hotkeys page, and all of
  enRAGE's settings are on one enRAGE page, section by section. Up/Down choose (Shift: the next
  section), Left/Right change (Shift: ten at a time), Q/E turn the page, Tab jumps between
  RealNPCs and enRAGE, Enter sets a key, Backspace closes. Greyed-out
  settings need their master switch on first. Changes apply at once. NemesisMod.ini is backed up
  to Documents\Rockstar Games\Red Dead Redemption 2\RealNPCs\backup before the first change.
  ReplaceEnrageSettings = false gives Alt+F11 back to enRAGE's own panel; Alt+R (SettingsKey)
  opens this one too. A pad works too: D-pad, LB/RB, A, and B to close.
- AwakeningBreakdown (90): how often an unravelling or awake person falls apart (kneels, shouts
  at the sky, runs) rather than holding it together.
- The ini is reorganised into [WestWorld], [Stalkers] and [Voices]; your values are kept.
- The ticker is always one line high, so it never overlaps the game's subtitles.
- T (TickerNextKey) shows the next line on the ticker now; on a long line shown in parts, the
  next part first. T is also enRAGE's telegraph key: near a telegraph clerk, both happen.
- New defaults: AwakeningDays 1, AwakeningBreakdown 90, AwakeningDespair 75, ReturnChance 35,
  StrangerReturnChance 15. If your ini still has the old defaults (6, 35, 2, 15, 5), they are
  raised to these, and the log says which; values you chose yourself are kept.
- CONTENT NOTE: AwakeningDespair is now 75 percent by default, so most fully awake, armed
  ordinary people end their own life. Set it to 0 if you would rather not see it.
- Stalkers do odd things now. Stand still and, more often than not, the stalker sprints in to
  somewhere between 2 and 30 m (StalkerActMin / StalkerActMax), faces you and works through the
  game's own gestures and tasks, one after another with a stare between: digging a grave,
  scrubbing blood from the ground, kneeling as at a graveside, lying down like the dead, writing in
  a notebook, tipping a hat, a hand up for quiet, a slow head-shake, spitting, laughing at
  nothing, a bow, a jig. Some
  gestures are done twice exactly alike, stop dead partway, or go slow. Two play on not being seen:
  it creeps closer while it is off your screen and stops dead the moment you look, or each time
  you look away and back it is standing somewhere else round you. Move and it stops at once and
  runs off. StalkerWhenStill is now mix by default (odd things most times, else circling,
  watching or rushing); eerie is odd things only. An ini still on the old default (circle) is
  moved to mix.
- Who stalks you has changed. Every StalkerEveryMinutes (34) real minutes of play there is a roll
  at StalkerChance (20 percent). On a hit, someone already near you (within StalkerNearbyMetres,
  60) stops what they're doing, stares at you, and starts following you: whoever has the most
  history with you, such as an enRAGE townsperson with a grudge or who saw what you did, or someone
  who saw your deeds or talked with you; a passer-by only if there's nobody like that. enRAGE's own
  people are never taken over (enRAGE runs them). If nobody near can follow you for a few minutes, a
  stranger turns up behind you instead (StalkerStrangers = false turns that off). Afterwards they
  go back to their day. The daily roll and its day and night hours (StalkerDayOnly, StalkerNightOnly,
  StalkerBoth, StalkerDayFrom/To, StalkerNightFrom/To) are gone, and StalkerChance starts again at
  20 because it now means each roll.
- The ] key (StalkerKey) makes a stalker now, for testing or whenever you like: someone near you,
  or a stranger if nobody near can be. It replaces Shift+F7.
- The wake key is now Y (WakeKey, was G) and the stalker key ] (was 0). An ini still on G or 0 is
  moved over and the log says so; a key you chose yourself is kept.
- No more Windows voice. Every line is now spoken, when it is spoken, in the person's own recorded
  game voice: a line that fits the feeling of what the subtitle says. The uncanny lines (people
  wondering if their world is real, glitches, stalkers muttering behind you) get the game's
  unsettled or taunting reactions instead. The game can't say new words, so the subtitle carries
  the meaning; where the words of a recording are known (voices\words.txt), the subtitle shows
  what they really say and the line written for them goes to the ticker as their thought.
  The [Voices] settings (SpokenVoice, VoiceVolume, VoiceMan, VoiceWoman, VoiceVariety) are gone.
- The reader for letters, diaries and clippings is now the N key (ReaderKey, was F5). An ini
  still on F5 moves over; a key you chose yourself is kept.
- More of the game's own recordings, chosen to fit what the subtitle says: "get away from me",
  "what are you looking at", "are you following me", "who's there", a sentence that trails off.
  The hermit's warnings get sharper each time, and he uses the game's own words for someone
  coming up to a camp. What people mutter under their breath, and the gathering's murmurs, get a
  wordless sound instead of words that wouldn't match. When the ticker shows the thought of
  someone within a few steps, they make a small wordless sound too, at most every 3 minutes each
  (ThoughtSounds in [Ticker]; false turns it off). The words are still the game's own: the
  subtitle carries the meaning.
- Quiet while you stand still. Stand still for 3 seconds (QuietStillSeconds), or turn on the
  cinematic camera, and enRAGE's tension stops building and no nemesis, gang or faction is sent
  after you, through enRAGE's own calm period (the one it uses after a load or a fast travel).
  It carries on within about 10 seconds of you moving again. A fight already going on carries
  on, and enRAGE's town regulars wait too. QuietWhenStill and QuietInCinematic in [Quiet] turn
  each off. It needs enRAGE's own [Calm] Enabled (on by default).
- Self-defence isn't a crime. Kill or hurt someone who came at you, or who was hostile to you
  (red), and RealNPCs notes no crime: no witnesses remember it, no rumour, no posse talk, no
  newspaper. Someone you shot or drew on first still counts, even if they fire back, and so do
  lawmen and anyone who begged. enRAGE's fighters (red on your map) count as having come at
  you even if you fire first. While you fight them off, the game isn't allowed to call it a crime
  at all, so no witness runs to report it. enRAGE judges its own witnesses the same way, and resets the
  bounty after fights with its own outlaws. For everyone else who comes at you, soldiers too,
  RealNPCs puts back any bounty or wanted level the fight gave you (SelfDefenceNoBounty), unless
  you also hurt someone who hadn't come at you or did another crime in the last few minutes.
  Story missions keep their own law, and a fight that moves into another state keeps the
  bounty you owe there.
- The same people at the counters. Each general store keeper, gunsmith, butcher, doctor, barber,
  barkeep, hotel keeper, undertaker, photographer, station agent, trader and stable hand in
  every town is one person for good, with a name, a life of their own and a memory of you: a new
  face at first, a regular later. Get near and the ticker shows what they're thinking, about the
  work, the town, and you. Shoot one down and the game brings them back bandaged: they survived.
  The same person, who talks about the wound, and who is frightened of you (or bitter, or coldly
  polite, by temperament) if they know it was you. Anyone who saw them fall finds it uncanny that
  they're back. TownPostsAfterDeath = successor makes a death final instead: the counter passes to
  a relative or a newcomer who bought the business. Sheriffs, preachers and anyone a story mission
  names are left alone. TownPosts = false turns it off.
- People think about what is on their minds ([Mind] Minds). Each keeps memories of what they lived
  through: a fight they were in or saw, a slight, a kindness, a theft, the drunk who called them
  out. What they think is whatever pulls hardest right now: that, their worry, the weather,
  someone in sight, you. Memories fade, come back when someone in them walks by, and weigh
  differently on each temperament. Nobody thinks the same line twice in a day. So far from scenes
  between people, from what they saw you do, from deaths (a friend grieves for weeks, once word
  reaches them), from your flirting, and from being saved. Minds = false: the old mix.
- An execution on the road ([Encounters] Executions, ExecutionChance 20). Away from the towns, once a
  day at most, one of the nearest town's regulars kneels at gunpoint, a mark on your map, for a
  reason from his own life: his debt (the creditor and a hired man), his feud (the other family),
  or talking to the law (the region's outlaws). Shoot or rush them and they fight you, with no
  crime noted. Save him and the town hears of it; wait or ride on and they shoot him, and his kin
  and his town hear. To see it now: press [, or the settings panel's Testing page.
  CONTENT NOTE: a man can be shot dead in front of you. Executions = false turns it off.
- Night encounters ([Encounters]). Out in the wilds between ten at night and four in the morning,
  you may hear a woman crying somewhere off the road (CryingWomanChance, 15 percent on each roll
  every EncounterEveryMinutes, 12, at most once a night). She kneels in the dark and calls for
  help in her own voice. Go close, or point a gun at her, and she may lunge at you with a knife
  and run (and later cry again somewhere else), or two or three armed men may step out of the
  brush, or she may say something that makes no sense and walk off into the night. Anyone who
  attacks is hostile from the start, so fighting them off (her too) is self-defence. Never in towns,
  indoors, in missions, while you're wanted or fighting, or while a stalker is out.
  CONTENT NOTE: the knife and the ambush are violent. CryingWoman = false, or Encounters =
  false for the whole section, turns it off.
- The hermits who woke (West World). Someone fully awake who runs from their town in the end
  of a wake act never comes back to it: they live rough in the wilds now. Out there, on a roll
  (HermitChance, 25 percent, at most one new camp a day, up to 8), one of them makes camp out of
  sight 60 to 140 m away and shouts once at the voice they hear (if nobody has fled yet,
  someone who walked out before you came by is made instead). The camp is saved: come within
  about 150 m and they are there, by a fire, talking back to the voice, or asleep beside it
  from midnight to five. They stand and watch as you come, warn you off if you come closer, and
  tell you how it was if you keep your distance (each thing once). Crowd them, aim at them or
  hurt them and they run, and are back another time. They are never armed. Killing one counts
  like any killing.
- The gathering (West World). From midnight to half past three in the wilds, on a roll
  (GatheringChance, 10 percent, at most once a night), six to ten of the awake stand in a
  silent ring somewhere off in the dark, facing in: people who have begun to come apart or are
  fully awake, as they looked when you met them, and strangers. Come near and every head turns to
  you. Look away and they are closer; look back and they stop dead, never nearer than a few
  steps. Stand in the middle and they speak at once. Aim at one, shoot, or hit one and they
  scatter. They never attack.


NEW IN 0.5.5
------------
- Stalkers can appear again. In 0.5.4 the game turned down every spot a stalker tried to appear
  on, so none ever showed up. If the game won't give a spot, the stalker now uses plain ground
  behind you (never inside a building). Staged weddings use the same fix.
- Stand still for 20 seconds and the stalker runs in close and circles you. Take a step and
  he runs back out to 45 to 70 m, behind a tree or a building corner if there is one, and
  watches you from there. Turning the camera doesn't scare him off. Walking up to him no
  longer ends it; drawing on him still does.
- More of what people say comes with their real voice. Conversations with enRAGE's people,
  remarks as you pass, replies to insults, flirting, couples and strangers chatting, and
  mourners at funerals now play a recorded game line that fits the feeling of the subtitle:
  angry, scared, grieving, grateful, gossiping, flirting, brushing you off. Arthur or John
  answers in his own voice when you flirt or talk. The words differ from the subtitle (the game
  can't say new words); the feeling matches. Uncanny lines keep the Windows voice.
- A stalker now keeps it up for StalkerMinutes in full, and the default is 120 (two hours, up to
  240). If yours was the old default (8) or the old ceiling (60), it's raised to 120 for you.
- The Awakening, first part. About 5% of the people enRAGE tracks are quietly chosen to wake up, leaning toward those who already suspect the most. Over days of game time each one moves through four stages: stirring (deja vu), questioning (they count things, test the world), unravelling (fragments, repeated words), awake (lucid, looking for you). What they think, say and write changes with the stage, and from stage two they keep a secret diary. The ticker summary counts them. Settings: AwakeningShare (5), AwakeningDays (6 days per stage). To see it at once, face one of enRAGE's people and press G (WakeKey in the ini): they wake one stage and say something.
- Everyone is someone. Ordinary people who are not enRAGE's (most of the world) get a name, a trade, a town and a temperament of their own when you press the wake key near them or the ticker shows their thoughts. They can wake like anyone else: press G (WakeKey) near any adult and they wake one stage. They exist only in this add-on and are saved once something happens to them.
- The Awakening on screen. Woken people now act oddly when you are near. Stirring: a glance a moment too long, or a stop and a stare. Questioning: they stand perfectly still watching you, or pace the same few steps over and over. Unravelling: they freeze mid-step and pace faster, with pauses, as if stuck in a loop. Awake: they walk straight up to you, face you and say something lucid. Only ordinary people and enRAGE's named townsfolk are directed (never enRAGE's full people, never during story missions or in danger), and they are handed back as they were. Use G near someone to wake them and watch.
- Ordinary people's thoughts are now written from real psychology: the body, status anxiety, loss and threat bias, the worries of 1899, the mask over what they feel, and what they notice about you when you are close.
- Every adult can now be a person with a mind, soldiers and lawmen too. Ordinary people get the full acts; enRAGE's own people and soldiers in its camps only turn their heads, think and speak (their movement is enRAGE's).
- Fuller acts. Stirring: double-takes. Questioning: standing perfectly still, pacing, walking out to see where the town ends, asking a neighbour if they feel it too. Unravelling: freezing mid-step, shouting at the sky, kneeling, running with no goal. Awake is a lasting state: they come to you, say a few things (who is writing this, is there an outside) and end by pleading, trying to leave town, trying to wake someone or refusing to get up.
- CONTENT NOTE: a chance (AwakeningDespair, default 50 percent since 0.7.19, 75 from 0.6.0; 0 turns it off) that a fully awake ordinary person cannot bear it and ends their own life: with a gun that fits them, with poison, with a knife, or by running at a wall. It never happens to anyone keeping a counter, or to anyone enRAGE has busy at that moment (a fight, a job, a ride), and it is not counted as anything you did. Set it to 0 if you would rather not see it.
- Acts and words belong together now: people who pace count their steps in their heads, people who freeze lose their sentence halfway, and the one walking up to you thinks about why. The thoughts appear on the ticker as it happens.
- Ordinary people have inner lives of their own: what a passer-by thinks now comes from their trade, town, temperament, faith or the hour of the day.
- Gang and faction news (wars, feuds, clashes, rivalries) now appears as the game's pop-up on the right of the screen, away from the bottom ticker, which keeps what people think and do. WarNewsTo in the ini (right or ticker) switches it back.
- The wake key now makes the person act at once so you can see what each stage does.
- The log shows the game's first few answers ("SAFECOORD probe"), to help pin down the cause.


NEW IN 0.5.4
------------
- Daytime stalkers. Each in-game day now rolls one of four outcomes: no stalker, one by day
  (06:00 to 17:00), one at night (18:00 to 04:00), or both. StalkerChance is the chance that
  someone follows you that day; StalkerDayOnly, StalkerNightOnly and StalkerBoth (default
  25 / 50 / 25) set how a stalker day splits. A daytime stalker who never gets a clear moment
  before dusk is dropped; a night stalker still waits until the next night.
- The log shows each day's roll, e.g. "Stalker roll (20%): one by day and one at night".


NEW IN 0.5.3
------------
- Stalkers fixed. A stalker due after midnight was wiped out when the day changed, so many nights
  never got one, and one who couldn't find a hidden spot was given up for the day. Now a stalker
  who is due waits for the first clear moment at night (outdoors, no story mission, no lawmen
  after you, not fighting), and if the night ends first he comes the next night.
- The log says when a stalker is due and what he's waiting for.
- Shift+F7 calls a stalker right away, for testing (see TESTING IT). F7's log dump now includes
  the stalker's state.
- Old saved talk that read "Pvt. saw ..." or "Pvt.'s brother" (from before 0.5.1) now reads
  "a private saw ..." and "a private's brother".


NEW IN 0.5.2
------------
- About twenty lines of dialogue reworded, so every line is the add-on's own.
- A diary line about a person's goal no longer garbles words like "your".
- The source code is now on GitHub (link above).


NEW IN 0.5.1
------------
- Stalkers are more common: a 20% chance per in-game day (was 8%). If you never changed the old
  setting, it's raised for you; the log notes each night a stalker is due.
- Ranks read properly: "Pvt. Maddox says..." instead of "Pvt. says he Pvt. ...".
- Holding F5, F6 or F7 no longer repeats it over and over.
- Killing armed men in a fight, rival gangs out for your blood, or anyone during a story mission
  counts as a killing, not a murder.
- Regions such as the Heartlands no longer count as towns you "ride into".
- Small fixes to life updates ("hit the The Klan").


NEW IN 0.5
----------
- More life in the street. Strangers now also square off in standoffs, sing old songs drunk, tell
  tall tales (some about you), read the paper aloud, haggle over horses, fall for a snake-oil
  pitch, make small talk, and pick pockets. After a bad crime, men talk about getting up a posse.
  The widow of a man you shot may be grieving in town, and may recognize you.
- Step into a scene: greet or antagonize someone in it. Take a side in a quarrel, break up a fight
  or a standoff (the town hears you're a peacemaker), or egg it on.
- Pickpockets: when one strikes near you, run him down (lasso, hogtie, tackle) and the town hears
  of it. Shoot him and it's a killing.
- Hold a gun on a stranger and they beg, with a name, a family, a trade. Lower the gun and word
  gets around that you showed mercy. Shoot anyway and that's what they'll say about you.
- Riding into town: locals react to what they've heard of you, your bounty, or your good name.
- The game's own voices are used far more: most of the add-on's lines now come with a fitting
  recorded line in that person's real voice (cheering, mocking, pleading, gossip, thanks...).
- The Windows voice can be any voice you have installed, and each speaker keeps their own voice,
  pitch and pace.
- Westworld: more host lines and conversations, people returning to one day of their past, strange
  dreams in their diaries, and a few lines they find in their heads (Shakespeare, Poe, Dickinson).
- Everyday thoughts from passers-by on the ticker.


NEW IN 0.4
----------
- The ticker: a line at the bottom of the screen that's always there. It shows what the people
  near you are thinking, and when nobody is near, a summary of the world and the latest news.
- Your deeds matter: towns and shops you visit, church, the saloon, hunting, killing, robbing,
  stealing horses, cheating at cards, sweet-talking, letting a beaten man go. People who saw it
  remember; people who didn't hear about it.
- Rumours travel town to town, about a hop a day, and get bigger and stranger the further they go.
- Your deeds turn up in enRAGE's newspapers.
- Letters wait for you at post offices: from the kin of people you killed, from people you were
  good to, from sweethearts, from people who suspect the truth, and wedding invitations.
- Diaries: loot a body (or search a gang camp) and you may find the diary they kept. Read it, and
  your letters and clippings, with the N key. People write about their work, their town, their family,
  their faith and habits, what's on their mind, and you.
- Kin far away hear when you kill someone. Some write. Some come west for you, as one of enRAGE's
  own people with a grudge.
- In someone's home town, a passer-by may turn out to be their mother or brother.
- Strangers around you flirt, quarrel, call in debts, catch cheats, propose and come to blows.
  In saloons they trade rumours, and loose talk can give away a gang job.
- Weddings happen at the church. Be there and the couple will remember it. Shoot it up and they'll
  remember that instead.
- Mourners at enRAGE's funerals speak of the dead.
- People go after what they want, owe each other money and collect it, and get moved out of jail.
- Westworld: some people start to question their world, out loud. Some of it is read aloud by the
  Windows voice. Now and then someone glitches.
- Reveries: some of the people you kill wake up again days later and remember how they died and
  who did it. Strangers you killed can come back too.
- Stalkers: rarely, someone follows you from a distance and hides when you look.
- Personalities colour what people say: a hothead, a coward, a proud man, a cold one.
- Changes to the ini take effect in game within a few seconds.
- Story missions are left alone: no scenes, stalkers or staged weddings while one is running, and
  nobody from a mission comes back as a revenant.


THE TICKER
----------
A thin bar at the very bottom of the screen.
- When someone enRAGE knows (or a townsperson who knows you) is within about 30 metres, it shows
  what they're privately thinking, closest person first. A stranger's passing thought can show too.
- When nobody is near, it shows a summary ("140 lives - 12 know your name - 3 hold a grudge ...")
  and then rotates through the latest news: thoughts from far away, diary entries, life updates,
  letters, rumours.
- New news gets a turn straight away and flashes brighter.
- T (TickerNextKey) moves on to the next line now. A long line shown in parts goes to its next
  part first; from someone's thought it moves on to the news.
- It hides in menus, cutscenes, the pause menu, when the HUD is hidden, and while enRAGE's own
  menus are open.
Subtitles (0.6.2): only what people say to you, and only while their voice plays; talk you only
overhear has none. Ticker = false turns the ticker off.


YOUR DEEDS
----------
The add-on notices what you do and where:
- Towns you ride into, and shops: the gunsmith, general store, saloon, church, bank, doctor,
  barber, bath, hotel, sheriff, station, tailor, fence, theatre, photographer, stable.
- Who you sweet-talk (Flirt, H), and the people enRAGE knows that you beat and then let go.
- Hunting (what you bring down), killing (strangers, lawmen, people enRAGE knows), and the crimes
  the game itself sees: robbery, bank, coach and train robbery, theft, horse theft, assault,
  looting, grave robbing, kidnapping, arson, cheating.
People enRAGE knows who saw it remember it (it goes into their enRAGE memories). Locals hear about
it. Later they say so to your face ("Saw you at church. Didn't figure you for the praying kind."),
think about it, write it in their diaries, and gossip.

Word travels. A deed people talk about spreads to the nearest towns, then on from there. Each town
keeps its own version, and it bends with distance: in Valentine "a man called Morgan shot a ranch
hand", in Saint Denis "some outlaw gunned down three men and rode off laughing", and further out
"a devil on a black horse - they say bullets go right through him". You hear the local version
when you ride into town.

The newspapers enRAGE sells pick up the talk too, in the small items about town.


LETTERS, DIARIES AND THE READER (N)
-----------------------------------
Letters are held at the post office. Ride into a town with one (most towns) and the clerk hands
them over. Press N to read them.
Diaries: loot the body of someone enRAGE knows who kept a diary, and you'll be offered it ("You
find a diary on ... [N] take it"). Searching an empty gang camp can turn up a diary too.
N opens your papers: Left/Right turn pages, Up/Down go to the next paper, Backspace closes.
Diaries are also saved as text files (see FILES).


FAMILY FAR AWAY
---------------
The people enRAGE knows have family: a mother in Saint Denis, a brother in Blackwater, a sister
back East. Kill someone and, a day or two later, their family gets a telegram. Some write to you.
Some decide to come west and settle it. A relative who comes for you becomes one of enRAGE's own
people, hates you, and hunts you the way enRAGE's people do.
In a relative's home town, if they've heard of you, a passer-by may walk up and turn out to be them.


LIVES IN MOTION
---------------
- Scenes: strangers near you flirt, argue, call in debts, catch a cheat at cards, propose, catch
  their spouse being sweet-talked, and sometimes come to blows (fists only: if one reaches for a
  gun, it goes back in the holster): most often two hot-tempered men with bad blood, or a hothead
  with drink in him in a saloon at night. In saloons they trade
  the rumours going round, and loose talk can give away a gang job, which then shows up in
  enRAGE like any other tip.
- The storyteller ([World] Storyteller, on the Essentials page: calm, lively as it comes, wild,
  or off as before). When nothing worth stopping for has happened near you for a while, the next
  street scene, chance meeting, walk-up and encounter roll come sooner; after something big, a
  little later for half a minute or so. It only changes the waits: it never starts or stops
  anything itself, and holds everything as built in a mission, a fight, while wanted or indoors.
- Weddings: when two of enRAGE's people get engaged, a wedding is set at the church in their town.
  The banns are posted (and printed in the paper); friends of yours get an invitation. Be there
  at the time and you'll see it, and they'll remember you came. Draw a gun or start shooting and
  the wedding breaks up; they marry quietly later and remember who spoiled it.
- Funerals: stand near the grave at one of enRAGE's funerals and the mourners speak of the dead.
  If you killed him, they'll know you.
- People pursue their goals, owe each other money and collect it (one way or another), and the
  ones held in jail get moved (and often don't arrive).


FRONTIER SCENES AND YOU (0.5)
-----------------------------
Scenes happen near you between strangers, at most one a minute or so (DramaGapSeconds, 60):
- Standoff: two men square off, a crowd gathers. Guns come out only when both are armed and no
  lawman is close; it ends with someone backing down, or with fists. Nobody fires.
- Drunk singing (saloons, nights), tall tales from old-timers, the paper read aloud (your deeds
  included, once they're in print), horse trading, a snake-oil man, small talk between neighbours.
- A pickpocket: he bumps someone, runs, and the victim begs you to stop him.
- A posse: within a day of a bad crime in town, men talk about riding after whoever did it. If
  someone saw your face, they may notice you standing there.
- A widow (or brother) grieving a stranger you killed in that town in the last week.
Greet or antagonize anyone in a scene and it turns on you: take a side in a quarrel or a haggle,
break up a fight or a standoff ("peace": it travels like your other deeds), or stir it up.

Pleading: hold your gun on a stranger for a moment and they'll beg. Lower it and they thank you
("mercy"). Shoot and it's an execution, and that's how the rumour goes. In towns where the dead
have been waking, some say stranger things.

Riding in: the first time, or after a while away, someone may say what they've heard of you:
fear after killings, scorn after thefts, warmth after good deeds, a remark if you have a bounty.

The lines are original. The songs (cowboy ballads and hymns) and the few quotations are public
domain, and the movie flavour comes from the genre's stock moments and period slang.


WESTWORLD
---------
Hosts. Some people start to suspect their world is made. At first it's deja vu and odd dreams.
Later they notice the loops: the same words, the same faces, the music that comes before trouble.
The ones who've died and come back, or who watched you die and get up again, say stranger things
still. Strangers in the street wonder out loud too, in their own recorded voices, unsettled.
Rarely, someone glitches: freezes and stares at you, then says what that was like.

Reveries. When you kill one of enRAGE's people (not story characters), there's a chance
(ReturnChance, 35%) they wake up again a few days later. They remember where they died, what
killed them and who did it, and they come back the way enRAGE's people come back from the dead.
Strangers you kill are remembered too, and a few (StrangerReturnChance, 15%) come back.

Stalkers. Now and then someone near you, often someone who knows you, starts following you. They keep their distance, stand and stare when they think
you can't see, freeze when you look at them and slip out of sight, and somehow turn up behind you
again after you've ridden off. Stand still for 20 seconds and they come closer and do something:
odd gestures and tasks (kneeling as at a graveside, a tipped hat, a hand up for quiet), circling you, or
just watching; move and they run back out, behind a tree or a building if there is one. They
never attack. Draw on them and they run for good. One can follow you for up to two hours.
Sometimes it's someone you killed.


REQUIREMENTS
------------
- Red Dead Redemption 2 (PC), Story Mode
- Script Hook RDR2 and Script Hook RDR2 .NET (V2), the same ones enRAGE needs
- enRAGE 0.9.5.3 installed and working. If a later enRAGE changes something, the parts that need it
  turn off and the log says which.
- Optional: CasualHookups, for an on-screen night together (see INSTALL, step 4).
- Optional: the voices need an NVIDIA graphics card with about 4 GB of memory free beside the
  game and about 8 GB of disk (see the voices section at the top, and INSTALL, step 5).


INSTALL
-------
1. Copy RealNPCs.dll into your game's "scripts" folder. enRAGE is not needed; if you use it,
   RealNPCs goes beside its NemesisMod.dll and works with it.
   Coming from enRAGE Westworld (the old name): delete EnrageWestworld.dll from that folder.
   Updating from an earlier RealNPCs: just replace the file. What people remember about you
   carries over, and your ini is updated with the new options (your values are kept).
2. Start the game and load your Story Mode save.
3. About 10 seconds after you're in the world, the ticker appears at the bottom of the screen.
4. Optional: CasualHookups, a separate mod by its own author
   (https://www.nexusmods.com/reddeadredemption2/mods/7608), plays an on-screen night together
   instead of RealNPCs' fade to black. Install it as its page says (CasualHookups.asi and
   CasualHookups.ini in the game folder). Its Flirt then works only on a woman who has said yes
   to your flirting here ([Romance] CasualHookupsDecides). The first time RealNPCs starts with it,
   it changes CasualHookups.ini once: its Flirt key moves from G (enRAGE's own lock-on prompts
   use G) to B, her clothes come back afterwards (enRAGE knows its people by their clothes), and
   it always gives in, since she has already said yes. The original file is kept as
   RealNPCs\backup\CasualHookups.ini.before-realnpcs. Restart the game once after that: it may
   only read its settings when the game starts. Its settings are on the panel's Romance page.
   After Hook up (H), press B for its scene. It shows no prompt at all for some kinds of women
   (Saint Denis' working girls, for one): if none of its scenes begins within 15 seconds, the
   night is RealNPCs' own fade instead, and once B came to nothing on her (or twice with no
   B), RealNPCs remembers that kind. With CasualHookupsDecides (or Romance, or Trysts) off,
   RealNPCs gives CasualHookups its own chances back from that copy (from the next start; until
   then it holds CasualHookups' Flirt key on every woman), and sets them again when it's back
   on; chances you set on the panel yourself are left alone.
5. Optional, recommended: the voices. Keep the zip's "voice" folder somewhere permanent, run
   install-voice.cmd once (about 8 GB, ten minutes; an NVIDIA card), and run
   start-voice-server.cmd before you play. The section at the top says the rest.

To uninstall, delete RealNPCs.dll. You can also delete the RealNPCs folder in
Documents if you don't want to keep what people remembered.


TESTING IT
----------
This version was checked outside the game (it builds, every enRAGE function it uses was found,
the people, rumours, letters and diaries were run through simulated weeks, and every scene, plea
and remark was checked line by line). 0.4 and 0.5.1 have since been played in game without
trouble; of the new 0.5 scenes, the posse has been seen so far. If something misbehaves, the log
(see FILES) says what happened. A feature that keeps failing switches itself off and says so in
the log; the rest carries on. Bug reports (with your log) are welcome on GitHub (link above).
- Press F7 near someone enRAGE knows: it shows what they're thinking now. Away from anyone, it
  shows the summary, puts a test line on the ticker, and writes the state of every feature to
  the log.
- Walk into a saloon or a church, buy cartridges, ride into another town a day later: listen to
  what people say, and watch the ticker.
- Kill one of enRAGE's people, loot the body for a diary, then visit a post office a few days later.
- Stand around a busy street or saloon for a few minutes. Greet someone in the middle of a quarrel.
- Stay a while in one town: watch who meets whom, and why, and the bars under the people near you.
- Hold your gun on a passer-by in town (then lower it). Ride into a town where you've done things.
- Press ] to make a stalker right away: someone near you, or a stranger if nobody near can be. It
  happens once you're outdoors, off any story mission, with no lawmen after you and not fighting;
  until then the log says what it's waiting for. Go about
  your business and look behind you now and then (he keeps 30 to 70 metres back).


FILES
-----
Documents\Rockstar Games\Red Dead Redemption 2\RealNPCs\
    RealNPCs.ini            settings (created on first run, updated by new versions)
    RealNPCs.log            what happened
    arthur.json, john.json  what people know and feel about you, and the world's state,
                            kept separately for each character
    diaries\                one text file per diary, e.g. "arthur - Elihu Starling.txt"
    backup\                 NemesisMod.ini and CasualHookups.ini as they were before RealNPCs
                            changed them (and CasualHookups.chances-at-start: the chances
                            it started the current game with)

If you load an older save, the add-on notices that game time went backwards and forgets anything
that hadn't happened yet.


SETTINGS (RealNPCs.ini)
-----------------------
Saved changes are picked up in game within a few seconds (ReloadIni). Every option has a comment
in the file. The new ones:

[Ticker]    Ticker (true), TickerNearbyMetres (20), TickerSeconds (12), TickerY (0.962, height on
            screen), TickerScale (0.30, text size), TickerWidth (0.60),
            TickerNextKey (T): the next line now, ThoughtSounds (true): a small sound from someone
            near as their thought shows, ThoughtsOverHeads (true): the thoughts of people near you
            above their head in small type instead of on the bar, ThoughtsOverHeadScale (0.24)
            ScenesOnTicker (true): what people say in a street scene within ScenesOnTickerMetres (30)
            shows on the ticker as they say it
[Display]   FeelingBars (true): bars under people near you for their fear, fondness and anger,
            FeelingBarsMetres (15), FeelingBarsShowEmpty (false): the empty part of each bar too,
            GrudgeMeter (true): the "Sore at you" panel, top right,
            VramGauge (model; on, off): a small gauge at the top left, how much of your graphics
            card's memory is in use (VRAM 11.3 / 15.6 GB, as Task Manager counts it), amber over
            85%, red over 95%; model shows it only while the AI model is on,
            NameCards (known; off, everyone): names over the heads of people RealNPCs knows, in
            the game's title type, NameCardsMetres (25), NameCardsKnownMetres (75),
            NameCardsAlways (true): a name over everyone in range, hidden only by walls,
            NameCardsBesideEnrage (false): ours even while enRAGE draws its own
[WestWorld] WestWorld (true): the master switch. false turns off everything below it.
            AwakeningShare (2.25), AwakeningDays (1), AwakeningBreakdown (90, how often an unravelling
            or awake person falls apart), AwakeningDespair (50, 0-100, see the content note above),
            WakeVoices (true): the waking say the game's own lines in the wrong places, more each stage,
            WakeHold (true): from the second stage they stay strange between acts instead of going
            back to their day, for WakeHoldMinutes (3); WakeGestures (true): their hands and bodies
            as they act; WakeStrange (true): from the third stage they bother the people around them
            (up close and staring, a hand before the face, following, "you're not real", preaching);
            WakeStripChance (5): held back since 0.7.9 (it froze the game); nobody strips,
            WakeLashOut (25): percent of holds at stages 3-4 that end with them turning on you or on
            someone near (fists, or their own gun when awake),
            HostsQuestionReality (true), RealityChance (15), Glitches (true),
            Reveries (true), ReturnChance (35), StrangerReturnChance (15)
[Stalkers]  Stalkers (true): the master switch. StalkerRecords (true): one of enRAGE's people may be
            the stalker while enRAGE has no use for them. StalkerKey (]): make one now,
            StalkerEveryMinutes (15): real minutes between rolls, StalkerChance (35, percent per roll),
            StalkerBlack (true): the stalker is a stranger in black who turns up out of sight behind
            you, StalkerBlackModel (a_m_o_sdupperclass_01), StalkerBlackOutfit (0); with StalkerBlack off:
            StalkerNearbyMetres (60): someone this close becomes the stalker, StalkerStrangers (true):
            a stranger if nobody near can, StalkerMinutes (120), StalkerAppearMin (45) /
            StalkerAppearMax (70): how far behind you a stranger first turns up, StalkerFollowMetres (26), StalkerStareMetres (34),
            StalkerStillSeconds (20), StalkerWhenStill (mix | eerie | circle | watch | rush): what he
            does when you stand still, StalkerCircleMetres (8), StalkerActMin (2) / StalkerActMax
            (30): how far off he does odd things, StalkerHideMin (45) / StalkerHideMax (70):
            how far he runs off when you move, StalkerMutters (true). He never attacks.
[Quiet]     QuietWhenStill (true): while you stand still, enRAGE sends nothing after you and its
            tension stops building, QuietStillSeconds (3), QuietInCinematic (true): the same in the
            cinematic camera
[Encounters] Encounters (true): the master switch. EncounterEveryMinutes (12): real minutes
            between rolls, EncountersAtOnce (5, 1-8): how many may be going on at once, a stalker
            among them (one of each kind; a roll starts one at most), Executions (true), ExecutionChance (20, percent per roll, away from the
            towns, once a day), CryingWoman (true), CryingWomanChance (15, percent per roll, 10 pm to
            4 am in the wilds), Hermits (true, needs WestWorld), HermitChance (25, a new
            camp's chance per roll in the wilds), Gatherings (true, needs WestWorld),
            GatheringChance (10, percent per roll), GatheringHours (any | night): with any, a ring
            may stand by day too, in the wilds or just outside a town, GatheringGapHours (24), ExecutionKey ([),
            CryingWomanKey, HermitKey, GatheringKey (None): bring one out near you now, for testing
            NightVisitor (true), NightVisitorChance (18): after midnight in a town, a pale figure over a
            body, gone when you look away; the town talks of it next morning
            Travellers (true), TravellerChance (20): by day on the roads, a man on foot asks you the way to
            the nearest town; greet him or stop a moment and he finds his way (one in three is
            snakebit and asks for help instead; one in five is being robbed, and you can step in)
            SupperInvitation (true; set false to turn it off), SupperChance (12): at dusk on the roads, a too-friendly man asks
            you to supper at his place; follow him there on foot and you wake in a ditch at first
            light with most of your cash gone ($250 at most, and it can't be undone); ride on and
            he lets you go
            PublicCallouts (true), PublicCalloutChance (35): by day in a town, someone calls another
            out before the street over something real between them; each answers by their own mind
            DomesticDisputes (true), DomesticChance (30): in a town by day, a man starts in on his
            wife in the street; she, a neighbour and you decide how it ends
            Hogtied (true), HogtiedChance (20): away from the towns by day, one of a town's people
            tied up off the road, two men by them; HogtiedMenDraw (true; set false to turn it off):
            their leader may draw on you unprovoked as you come near (never bounty hunters)
[World]    Deeds, SelfDefenceNoBounty (true), TownPosts (true), TownPostsAfterDeath (survive | successor),
            Regulars (true), RegularsPerTown (30), RegularsDress (true), RegularsRoutines (true), RumoursTravel, Newspaper, Letters, FindDiaries, Avengers (all true),
            AvengerChance (35), AvengersAtOnce (2), AvengerGapHours (48), CastRelatives, Drama (true), DramaGapSeconds (60), SaloonGossip,
            Weddings, Funerals, JobRumours, PrisonerTransfers, Debts (all true)
            Bravery (85, 0-100): the brave stand their ground where the game would send them running;
            0 is the game's own way. InteractionMenu (true): Talk in the lock-on menu, TalkKey (Q:
            the game's lock-on target-info control; another key, e.g. T, is read directly while the
            prompt is up; None: a pad's RB only). LawClearsBodies (true), PersonalSpace (true) with
            SpaceMetres (0.95), SpaceSaloonMetres (0.75), SpaceSeconds (1.8), ArthurSpeaks (true),
            Storyteller (lively | calm | wild | off): quiet streets and roads get the next scene,
            walk-up and encounter roll sooner, something big a short breather; off is as before
            HistoricalAccuracy (true): the real history: the talk of the times from the papers of
            1899 and 1907, the lines about the year, the real calendar's holidays on their true dates
            (each with its eve and morning after, and a word of the day on the ticker), the year's
            work by region, the snake-oil man's Food and Drugs Act and the Panic in 1907; false:
            none of it, and the timeless texture of the days stays
[Features]  KinWordDaysMin (2), KinWordDaysMax (5): game days for word of a killing someone saw to
            reach kin; UnseenKinWordDaysMin (5), UnseenKinWordDaysMax (10): for one nobody saw
[Scenes]    SceneReactions, Standoffs, Songs, TallTales, NewspaperTalk, Haggling, SnakeOil,
            SmallTalk, Pickpockets, PosseTalk, Mourners, Pleading, ArrivalTalk (all true),
            ScenesAtOnce (4): the scene and quiet talks beside it elsewhere on the street (1-6),
            MainScenesAtOnce (2): a second full scene 20 m or more from the first, words and walk-overs
            only (fights, rough handling and thefts stay one at a time); 1: one at a time,
            Aftermath (true): after a fight a friend tends the loser and the law has a word with the winner,
            FightCrowds (true): a ring of onlookers around a fistfight, NightScenes (true): a mugging, a
            drunk taken home, HorseThieves (true), GangShootouts (true): rival gangs who meet fight it
            out, ShoutsCarry (true): shouts shouted the game's way, heard from farther off,
            Reactions (true): in a street scene the one spoken to reacts to the words by their
            nature, and a man whose face goes into the bar reels
            Acknowledge (true): people who know each other nod, tip a hat, wave or say hello as they
            pass; enemies give a hard look, sweethearts a long one; each thinks something of it
            SightReactions (true): people near you notice when you look bloodied, drunk, filthy or
            soaked, and react by their nature
            Kindnesses (true): someone who trips in the street is helped up, or left lying, by the one
            who saw it; walk up while they're down and you help them yourself; someone may step in
            before a bully's blow lands; and of an evening someone glad stands the saloon a round
            (the evening after a wedding, a toast to the newlyweds)
            Vices (true): someone with bad news, or word of a friend's death, drinks the night away in
            the saloon (and a friend may sit with them)
            Grudges (true): a friend has it out with whoever wronged their friend; someone whose friend
            you killed may tell you so to your face (never violent)
            (Standoffs also brings the noon callout: two men with bad blood face off in the street about
            noon until one backs down; nobody fires)
            (Drama also brings two old men arguing about the old days, on a Sunday afternoon a
            street preacher, answered by each listener's own faith, and by day a mother asking after
            her lost child)
            RoughHandling (true): a man with a grudge (or a hothead with drink in him) smashes another's
            face into the nearest table, bar or wall; NpcThieves (true): pickpockets among the
            townsfolk, and begging; DrunkChallenges (true): a drunk calls a man out at night
            EnragePeopleJoinIn (true): enRAGE's own people take part in all of these too, unless
            enRAGE has them busy (a fight, a job, a ride)
            RealVoiceTalk (true), RealVoiceTalkSeconds (120): people near you talk to each other in
            their real recorded voices; CrudeTalk (true): the game's crude remarks among them
            BeingWatched (true): the street's trouble waits while a lawman is looking (WatchedLawMetres
            25, WatchedRecklessChance 40, WatchedChurchMetres 60); BoldThieves (25): percent of thieves
            who try it anyway; LonelyRegulars (true), LateThanks (true), ReasonThoughts (true)
            ReasonVariety (true): the street's scenes take turns, so fewer sharp words; ChanceFirst
            (true): chance meetings get their turn when nobody near has a strong reason, nearer you
            FavoursAsked (true, with MindLive): someone hard pressed for money may come and ask you,
            and Talk lists the fix; WarnedYou (true, with MindLive): a witness may have words with
            you, and a friend may warn you of a grudge or the law; CourtFolkAges (true): enRAGE's
            townsfolk court each other too (an age guessed)
[Romance]   NpcFlirting (true): people near you make eyes at each other and court, each answering by
            what they make of the other; their thoughts on the ticker show it as it happens;
            sweethearts fall out now and then, and one comes back with a small gift to make it up
            FlirtPrompt (true): Flirt in the game's lock-on menu, with Greet and Antagonize, on H (only on women while SameSexRomance is off)
            (D-pad down on a pad)
            WillingSingle (70), WillingTaken (20): percent chance a single, or a married or
            spoken-for, woman takes to your flirting before what she thinks of you tips it (the
            saloon's women always do); greeting her first (R) helps by 15, flirting cold costs 15
            Trysts (true): when she says yes, the same H is Hook up (once she's said it) for two minutes; her answer
            to that is its own (better after a greeting), then the screen goes dark, an hour passes
            CasualHookupsDecides (true): with CasualHookups installed, its Flirt works only on a
            woman who has said yes to you, and RealNPCs doesn't fade to black itself (unless
            CasualHookups won't take her kind: then the fade, at once or after 15 s); off: its own
            chances back
[Output]    PerfMeter (true): times every part of RealNPCs each frame; F7 writes the costliest,
            and the log says when it goes over budget (PERF)
[Mind]      Minds (true): people think about what is most on their minds, from what they lived through;
            false: the old mix of thoughts. RecordMinds (true): enRAGE's own people too, weighed with
            enRAGE's record of them. RecordFates (true): a death RealNPCs caused goes into enRAGE's record.
            ShareWithEnrage (true): RealNPCs keeps its own record of
            who has died; when one of enRAGE's townsfolk dies and enRAGE didn't see it, RealNPCs
            marks them dead there too
            MindShadow (true, while it is tested): the mind underneath works out the traits, needs,
            feelings and mood of the people near you and writes them to the log (MIND lines), F7
            (TEST mind shadow) and the Show minds cards; it changes nothing anyone does or thinks.
            MindLive (true): what that mind decides reaches the screen: moods, and thoughts of what people really feel (about you, a friend, a stranger) and of a pressing want
            MindLevels (false, in testing): the mind's work spread by distance and a budget a frame.
            MindActs (true, under MindLive): people near you do what their minds choose (a stroll, the
            nearest bench, chair, bar or work spot, off home, clear of you or a foe, over to a friend).
            MindActsScenarios (true, under MindActs): people sat or leaning get up for it, the game's own
            way (never pulled out of a chair in view), and go to a spot of the kind they want: a bench or
            the steps, a wall or a post, the bar rail, a broom; one every few seconds near you, three at
            most at once.
            MindActsSeatedLooks (true, under MindActs): people watch you or the street, look up from
            their seat, and make sure to be seen. MindRetries (true, under MindActs): a choice they
            couldn't act on is tried again a minute later, up to three times.
            MindPassingLasts (true, with MindPassing): what they feel about you shows for hours as
            you pass, not minutes. MindChanceMeetings (true, under MindLive): a chance meeting near
            you comes from the two people's minds; false: rolled by the place alone.
            MindJoinFights (true, under MindLive): onlookers may join a fistfight on a side. MindBrawls
            (true): a fight grows into a brawl, up to BrawlMost (12); BrawlLaw, BrawlPullsYouIn,
            BrawlDraws (all true), BrawlDrawShare (40), BrawlFistsFatal (50: percent of men beaten down
            who die; the rest are knocked out).
            BrawlJoinMetres (30): they join from within this many metres; those over 10 m off run over,
            counted for their side on the way. BrawlCrowdMetres (40): people this close walk over to watch
            and cheer (FightCrowds; up to 6 round two men, 16 round a brawl). BrawlLeastSeconds (45): a
            brawl goes on at least this long from its first blows, unless one side is all down.
            MindPlans (true, under MindLive): plans over days (get even, thank, pay a debt, court) that send
            people to find each other and shape their day. LadderShots (true, under MindLive): a quarrel
            may end in shots when one of them is armed.
            QuarrelLadder (true, under MindLive): an argument or a fight between two people the mind knows
            climbs or cools by their natures and the room; the shot is never fired.
            ThoughtVariety (true): no two people near you think the same shape of thought within an hour,
            and a kind of thought that has had the last few turns gives way to the others.
            Language (false, in testing): the mind's own thoughts worded by a local model (Ollama at
            LanguageUrl, running LanguageModel); every answer checked, the written line the fallback.
            The AI MODEL (OLLAMA) switch at the top of the settings panel. Off as it comes, but at a
            start, if Ollama answers holding LanguageModel, RealNPCs turns it on by itself, once, and
            says so, unless you have chosen. LanguageChosen (false): set true by itself whenever you
            change Language (in the panel or in this file), or when Language is on at a start, so a
            model you turned off stays off.
            LanguagePicks (true, with Language): the model picks what people near you do, from the few
            things their minds allow; ThoughtsAloud (true, with Language): a thought is said aloud in
            their own recorded voice when one of their remarks fits it.
            LanguageVariety, LanguageTalk (true, in testing, with Language); ModelWakes (true, in
            testing, with Language, MindWakes and LanguageVariety); MindStartsScenes (true, in
            testing, with LanguagePicks); PickMetres (25), PicksAtOnce (8); MindTownMood,
            MindTempers (true, in testing).
            HushHours (0), HushHoursSaintDenis (0): with MindTownMood, for this many game hours after
            the latest killing in a town its street holds back its own fights, thefts and callouts
            (0-12; 0 never, as it comes, for a livelier world; 2 and 1 for a short quiet). Someone beaten to death in a brawl, a Testing row's own trouble (your
            own killings still hush, but not of the test's own people), and brawls, roughs and
            standoffs never hush it; a brawler shot or cut in the fight is a killing and does.
[Output]    NewsTo = corner | ticker | gamefeed
                corner: the "Around town" box (default); ticker: the ticker;
                gamefeed: the game's own updates, top left
            WarNewsTo (feed | corner | ticker): where gang and faction news goes (feed, the default:
                the game's own updates, top left)
            TerritoryNews (true): enRAGE's territory changes go there too. enRAGE also prints
                them as white text while its own [Menu] Messages is 3 (2 or 4 stops that)
            Subtitles (on | off): what people say to you, on the game's subtitle line while their voice
                plays (the game's own Subtitles option has to be on too)
            TestKey (F7), FlirtKey (None), WakeKey (Y), ReaderKey (N), ReloadIni (true),
            ReplaceEnrageSettings (false): true lets Alt+F11 open the RealNPCs panel (enRAGE 0.9.5.3 only),
            SettingsKey (Alt+F) and SettingsKey2 (Alt+R) open and close it with any enRAGE version,
            PanelSlowsTime (true), PanelTimeScale (0.25): the world slows to a quarter speed while the
            panel is open, as it does for the weapon wheel (never in a story mission)
All the older options are still there and work the same.


KEYS
----
Alt+F     the RealNPCs settings panel (all RealNPCs and enRAGE settings; SettingsKey). Alt+R
          (SettingsKey2) opens it too, and either closes it again. Its Testing page lists everything
          RealNPCs can make happen near you now, by group: Enter on a row sets its key, Space fires it
          (the panel closes first; on a gamepad, A fires it), Delete leaves it without a key. A key
          another test has already moves to the new one and leaves that test without a key; one of
          enRAGE's keys, or one RealNPCs needs (F7, the flirt key...), is refused, naming whose it is. The keys are in [Testing] in the ini (TestKeys switches
          them all off). Every street scene has a row (the nearest named pair that fits plays it),
          and so do pleading, arrivals, letters and the strange remarks. Show minds (off as it
          comes; its switch is on the UI page) puts a card over the people near you: their
          temper, mood, worry, ties and what is on their mind; Replay fires the last test again.
          "Bring the test cast here" brings eight people the tests need (a mother,
          two old men, a preacher and a listener, three men for a bully) to stroll about a spot 10 m in
          front of you; they're kept in the save, so the same people come each time, and
          "Let the test cast go" takes them away. There is also a row that wipes every NPC's memory (Enter twice). With ReplaceEnrageSettings on, Alt+F11 opens it too, but
          only on enRAGE 0.9.5.3; on other versions Alt+F11 opens enRAGE's own.
          About 15 seconds after loading, a notice says which key to use.
RB+D-pad  on a controller: hold RB and press D-pad up for the settings panel, left for F7, right
          for your letters (the letters turn with the D-pad and close with B). Set them from the
          controller on the panel's Hotkeys page (A, then hold one button and press another), or in
          [Controller] in the ini (PlayStation names work: R1+Up). Never in the game's own menus.
          Subtitles: the game draws its own white one for any recorded line while its Subtitles option
          is on, so RealNPCs' boxed ones show only while that option is off ([Output] Subtitles = game);
          set it to "ours" and switch the game's off to see only RealNPCs'. [Scenes] TroubleWeight sets
          how often people come to blows or quarrel (1 as built, 2 twice as often, 0 never by chance).
F7  what the nearest person enRAGE knows thinks of you (and a status line in the log); the town
          regulars and the fully awake around you show on the map for a minute
H         locked on to someone, Flirt (FlirtPrompt); after her yes, the same H is Hook up. FlirtKey can set
          a keyboard key as well (None; it was F2).
N         read your letters, diaries and clippings (or take a diary you've just found; ReaderKey)
]         make a stalker now: with StalkerBlack on, a stranger in black behind you; off, someone
          near you, or a stranger (StalkerKey)
[         stage an execution on the road near you, out of sight, to see one now (ExecutionKey)
Y         wake the person you are facing one stage (the Awakening, for testing; WakeKey)
Shift+F7  two people near you talk in their real voices (TestTalkKey)
Shift+F9  one man near you puts another's face into the bar (TestRoughKey)
Shift+F2  the subtitle line in each font, to find the game's (TestFontKey)
Ctrl+F2   Show minds on or off for this session: the card over people near you (TestShowMindsKey;
          the settings panel's UI page has the switch that's saved)
          These seven (], [, Y, Ctrl+F2 and the three Shift keys) are the Testing page's; the rest of its
          rows have no key as they come.
Q         locked on to someone RealNPCs knows: Talk (TalkKey; a pad's RB). It is the game's own
          lock-on "target info" control, with its own icon: rebind that control in the game's
          Settings > Controls to use another key (e.g. T), and the icon follows (rebound to T, the
          same press also turns the ticker, and next to a telegraph clerk opens enRAGE's telegraph).
          In the list: D-pad or mouse wheel to move, Space or Enter (A) to say it, Backspace (B) to
          leave.
T         the next line on the ticker now (on a long line, its next part first;
          TickerNextKey). T is enRAGE's telegraph key too: near a telegraph clerk, both happen.
None of the others are used by enRAGE (F11, Alt+F11, Ctrl+F10, I, T, Insert), Script Hook .NET's
console (F8) or Faster Looting (F9, F10). If the game or another mod already uses one, change it
in the ini or the panel (e.g. ReaderKey = Ctrl+N).


CREDITS
-------
enRAGE is by its author on Nexus Mods (link above). This add-on reads what enRAGE already tracks
and adds to it, through enRAGE's own functions wherever enRAGE has them. It contains no enRAGE
code or content.
CasualHookups is by its author on Nexus Mods (link above). RealNPCs works with it when it's
installed and contains none of its code or files.


LICENSE
-------
Copyright 2026 lexthestoic. All rights reserved. You're welcome to download and play RealNPCs;
please ask before reusing its code.
