// Worries that turn day by day (LIVES item 1a, 2026-11-12; [Mind] WorryTurns, settings version 461). A resident's worry
// runs eight game days in four stages (InnerLife.Worries.cs: begins, worse, the crisis, how it ended), and every day of a
// stage drew from the same pool, so day three sounded like day four. Three days of the run now have a turn of their own:
//
//   day 3 (the first day it's worse): looking back. The mind measures how far it has come ("A week ago it was only...").
//   day 4: a hope. Something small to hold to: a word, a sign, a sum that nearly adds up.
//   day 6 (the last day of the crisis): the eve. Tomorrow it's settled, one way or the other.
//
// The psychology: people live a trouble as a story with turns, not a flat level of dread. Hope in the middle is what makes
// the crisis land (Snyder's hope theory: hope is the sense of a path, and the crisis is the path closing), and the eve is
// the anticipatory peak (dread climbs fastest just before an outcome is known, Loewenstein's "anticipation" studies), so
// the player who hears a man three days running hears a week, not a loop. The hope line is the same whether the worry ends
// well or badly: the player only learns which on day 7, the way the man does.
//
// Game design: nothing new to do and no new state. It reads the run's day (InnerLife.DayIn), so it's free and saves nothing;
// a worry the player settled keeps its own lines (they come first in WorryThought). Off in Core so the harness goldens stand;
// ApplySettings turns it on. Plain C#, no game.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class WorryTurns
    {
        /// [Mind] WorryTurns: set by the game from the settings; off here so the goldens of the old pools stand.
        public static bool On;

        /// Percent of thoughts on a turning day that take the turn (the rest keep the stage's pool, so it isn't every time).
        public const int Chance = 60;

        public const int Back = 2, Hope = 3, Eve = 5;   // the day in the run (0-based) each turn falls on

        /// Which turn this day of the run is, or null.
        public static string TurnOn(int dayIn)
        {
            return dayIn == Back ? "back" : dayIn == Hope ? "hope" : dayIn == Eve ? "eve" : null;
        }

        /// The lines for this worry's turn, or null (the placeholders are the worry's own: {who}, {kin}, {place}).
        public static string[] For(string worry, string turn)
        {
            string[][] t;
            if (worry == null || turn == null || !Lines.TryGetValue(worry, out t))
                return null;
            return turn == "back" ? t[0] : turn == "hope" ? t[1] : turn == "eve" ? t[2] : null;
        }

        /// Every line, for the line tools (they read IEnumerable<string> All*Lines methods).
        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in AllPools())
                foreach (string l in kv.Value)
                    yield return l;
        }

        /// Every pool, for the line tools and the overlap checks.
        public static IEnumerable<KeyValuePair<string, string[]>> AllPools()
        {
            foreach (var kv in Lines)
            {
                yield return new KeyValuePair<string, string[]>("worryturn:" + kv.Key + ":back", kv.Value[0]);
                yield return new KeyValuePair<string, string[]>("worryturn:" + kv.Key + ":hope", kv.Value[1]);
                yield return new KeyValuePair<string, string[]>("worryturn:" + kv.Key + ":eve", kv.Value[2]);
            }
        }

        // back, hope, eve: three lines each, for every worry kind in InnerLife.Worries.cs
        private static readonly Dictionary<string, string[][]> Lines = new Dictionary<string, string[][]>
        {
            { "debt", new[] {
                new[] { "Two days back it was a sum I owed. Now it's a man I hide from.",
                        "Remember when eleven dollars sounded small. Last week, that was.",
                        "Started with a handshake and a smile from {who}. Neither of us smiling now." },
                new[] { "Foreman says there may be Saturday work. May be. I'll take may be.",
                        "Seven dollars counted, if the hens keep laying. Four to find. Four is a number a man can find.",
                        "{who} didn't mention it at church. Maybe he's forgot. Men forget." },
                new[] { "Tomorrow I face {who} with what I've got. Laid it out on the table twice tonight.",
                        "Last night before I know where I stand with {who}. Won't sleep. Won't try.",
                        "Shined my boots for tomorrow. Whatever {who} says, I'll hear it standing up." } } },
            { "fever", new[] {
                new[] { "Two days ago it was a cough. Just a cough. Said so myself.",
                        "Remember telling {who} to hush and go to sleep. Wish I'd sat up.",
                        "Monday {who} was out by the pump. Seems a long time since Monday." },
                new[] { "{who} took a little broth. Three spoons. Counted every one.",
                        "Cooler this morning, the brow. Cooler. Won't say it out loud yet.",
                        "{who} asked for the dog. If they want the dog they're still in there, fighting." },
                new[] { "Doctor said tonight will tell. Lamp's trimmed. Water's by the bed.",
                        "By morning we'll know. Kept my hand on {who}'s chest to feel it rise.",
                        "Last night of not knowing. Can't tell if I want it over or don't." } } },
            { "letter", new[] {
                new[] { "Used to count weeks since the last letter. Counting months now.",
                        "First week I wasn't worried. That's the week I'd like back.",
                        "Told myself {kin} was busy. Told myself that a good while." },
                new[] { "Clerk says a mail sack went astray at the junction. Could be. Could be in it.",
                        "Dreamed of {who}'s handwriting. Woke up hopeful. Hope's cheap and I'll take it.",
                        "A man from that county says the roads were washed out. That would explain it. Would." },
                new[] { "Mail comes in tomorrow. Going to stand at the window till it's sorted.",
                        "Tomorrow I'll know, or I'll stop asking. One or the other.",
                        "Wrote out what I'll say if there's nothing again. Tore it up. Tomorrow." } } },
            { "drought", new[] {
                new[] { "Spring I worried about too much rain. Seems like a joke a man would tell.",
                        "Remember the creek up to the second rock. Can walk across it dry now.",
                        "Two weeks ago I'd have sold the cow for a good price. Every day it's worth less." },
                new[] { "Clouds over the hills at sundown. Thin ones. Watched them till dark.",
                        "Smelled rain in the wind this noon. Might've been the river. Might not.",
                        "Almanac says the weather turns at the full moon. Moon's near full." },
                new[] { "Tomorrow I decide about the cow. Walked out to her and stood a while.",
                        "Last day I can wait. After this it's the well or the cow.",
                        "Sat on the porch watching the sky till the stars came out. Nothing. Tomorrow, then." } } },
            { "son", new[] {
                new[] { "When {who} left I said he'd write in a week. Said it to his mother, mostly.",
                        "Used to listen for the stage. Now I listen for the stage and dread it too.",
                        "His boots are still by the door, the old pair. Haven't moved them. Won't." },
                new[] { "A drummer swore he saw a lad like {who} in {place}, working and well fed.",
                        "Found a note in {who}'s coat: an address in {place}. Wrote to it straight off.",
                        "{who} always was slow to write. Slow at his letters as a boy. Just slow." },
                new[] { "Stage comes in tomorrow from the {place} line. Going to meet it.",
                        "Tomorrow I'll know if that letter found him. Laid his place at supper anyhow.",
                        "Last night of hoping blind. Tomorrow there's an answer or there isn't." } } },
            { "courting", new[] {
                new[] { "Week ago I didn't know which house was {who}'s. Know it now. Know every window.",
                        "First time I saw {who} I thought nothing of it. Thought nothing of it for a whole day.",
                        "Used to sleep fine. That was before {who} said good morning." },
                new[] { "{who} looked back. Twice. Twice is not by accident.",
                        "{who}'s brother said she asked my name. My name. Asked it.",
                        "Got the nerve up to say hello, and {who} said hello back like she meant it." },
                new[] { "Sunday's tomorrow. I'll ask {who} to walk out, or I'll know I'm a coward.",
                        "Pressed my good shirt for tomorrow. Practised the words on the mirror. Mirror laughed.",
                        "Tomorrow I find out if it's me or the fellow behind me. Can't eat." } } },
            { "feud", new[] {
                new[] { "Started over a fence post. A foot of dirt. Look where a foot of dirt has got us.",
                        "{who} and I used to share a jug at harvest. Can't call to mind the taste of it now.",
                        "My father and his father were friends. Don't know what they'd make of it. Don't want to." },
                new[] { "{who}'s wife nodded to mine at the store. Women settle things men won't.",
                        "Saw {who} mending his side of the fence. His side. Maybe that means something.",
                        "Preacher says he'll have a word with {who}. Preacher's words have moved harder men." },
                new[] { "Tomorrow I go to {who}'s door, one way or the other. Haven't decided which way.",
                        "Cleaned the shotgun tonight. Put it back up on the pegs. Tomorrow I talk first.",
                        "Last night of this, I've decided. Tomorrow it ends. Don't know how." } } },
            { "bank", new[] {
                new[] { "When I signed that note it was a good year. Banker shook my hand like a friend.",
                        "Used to drive past the bank without a thought. Now I take the back road.",
                        "Spring seemed a long way off when I signed. Spring's always a long way off when you sign." },
                new[] { "Banker's clerk said they've given others more time. Others. Might be me.",
                        "Brother-in-law says he might go halves on the note. Might. He said might.",
                        "Good price for hogs this week. If it holds to Saturday, I've a chance." },
                new[] { "Tomorrow I sit across from the banker. Wrote my figures out clean.",
                        "Last night under this roof that's still mine for certain. Walked every room.",
                        "Tomorrow they'll tell me. Wife's asleep. One of us should be." } } },
            { "drink", new[] {
                new[] { "First day dry I thought it was easy. Easy, I said. Fool.",
                        "Remember the morning I swore off. Sun was up. Felt like a new man for an hour.",
                        "Used to count drinks. Now I count hours since the last one." },
                new[] { "Hands were steadier this morning. Shaved without cutting myself.",
                        "Slept the night through. First time. Woke and didn't know where the bottle was. Didn't care.",
                        "Wife smiled at breakfast. Hadn't seen that smile in a year." },
                new[] { "Tomorrow's a week dry, or it's nothing. Tonight's the long night.",
                        "Saloon's lit up. I can hear the piano from here. One more night. Just this one.",
                        "Gave the bottle to the preacher to pour out. Tomorrow I'll know if I meant it." } } },
            { "cards", new[] {
                new[] { "Week ago I was up twenty dollars. Twenty. Thought I'd found the trick of it.",
                        "Lost the first hand on purpose, to draw them in. Lost the rest without trying.",
                        "Mrs. {who} used to call me a nice young man. Doesn't call me anything now." },
                new[] { "Won six last night. Six. Paid Mrs. {who} two of it before I could think.",
                        "Fellow at the livery says there's day work hauling. Honest money. Slow, but money.",
                        "Cards ran good at the end. Felt it turning. A man can feel it turn." },
                new[] { "Tomorrow's the end of the week. Rent or the street. Got one hand left to play.",
                        "Packed my bag tonight, in case. Didn't unpack it. Tomorrow.",
                        "Mrs. {who}'s nephew was on the porch at supper. Tomorrow, then." } } },
            { "savings", new[] {
                new[] { "Started saving in the spring. A penny, a nickel. {place} seemed close then.",
                        "Remember the first dollar in the can. Felt like a deed to a house.",
                        "When I started I knew what the money was for. Lately it's for everything." },
                new[] { "Counted the can. More than I thought. A whole half dollar more.",
                        "Ticket agent says the fare to {place} drops come autumn. Autumn's soon.",
                        "Picked up extra washing this week. Hands are raw. Can's heavier." },
                new[] { "Tomorrow I buy the ticket, or I give the money to the rent and stop dreaming.",
                        "Took the can out tonight and held it. Tomorrow I choose.",
                        "Last night of not knowing which. {place} or the rent. Tomorrow." } } },
            { "tooth", new[] {
                new[] { "Started as an ache when I chewed. Now it aches when I think.",
                        "Remember eating an apple. A whole apple, on the left side. Last month, that was.",
                        "First day I thought it'd pass. Teeth don't pass. They stay and they argue." },
                new[] { "Clove oil helps. An hour. I'll take an hour.",
                        "Swelling's down a little. Maybe it's going on its own. Maybe.",
                        "Slept on the other side and it let me be till dawn." },
                new[] { "Barber opens at seven. I'll be on his step at six, or I won't go at all.",
                        "Last night with this tooth. Can't say I'll miss it. Can't say I'm ready.",
                        "Told the barber first thing tomorrow before I lose my nerve." } } },
            { "secret", new[] {
                new[] { "Used to forget it for days at a time. Now it's the first thing in the morning.",
                        "When I first came here nobody asked anything. That was the beauty of it.",
                        "Thought I'd buried it deep. Turns out you never bury it deep enough." },
                new[] { "Man who asked about Missouri left on the morning train. Gone. Maybe he never cared at all.",
                        "Nobody's looked at me strange today. Not once. Counted.",
                        "Paid back a dollar of it. Slid it in the till when nobody looked. Felt lighter." },
                new[] { "Tomorrow I tell it, or I carry it to my grave. Lay awake choosing.",
                        "If anyone's coming to ask, it'll be soon. Kept the lamp on.",
                        "Last night as the man they think I am. Maybe. Maybe not." } } },
            { "law", new[] {
                new[] { "Week ago I walked down Main Street bold as anybody. Look over my shoulder now.",
                        "When I first rode in, nobody knew my face. That's what I liked about the place.",
                        "First poster, I laughed. Bad likeness. The second one's not so bad." },
                new[] { "Riders went on south this morning. Didn't stop. Didn't look this way.",
                        "Sheriff tipped his hat to me. Friendly. Either he doesn't know, or he wants me easy.",
                        "Rain washed the new poster half off the wall. Providence, maybe." },
                new[] { "Tomorrow I ride or I stay for good. Horse knows something's up. Won't settle.",
                        "Last night here, maybe. Sat in the saloon and looked at every face. Liked them, mostly.",
                        "Packed the saddlebags. Hung them back on the peg. Tomorrow I'll know which." } } },
        };
    }
}
