// Second acts (FOURTEENHOUR reserve R3; TWELVEHOUR item 14; [Encounters] SecondActs). The people of the encounters come back.
// Already there: someone you save is yours for good (WestworldScript.Saved.cs: the captive saved at an execution, the hogtied cut
// loose, the man you pulled a bully off, the one whose beater you put down: fondness to the top, a friend who owes you, a memory
// of it), and a friend near comes over now and then with a warning, the news, a gift or a favour of their trade (Core/FriendCalls.cs,
// the friend calls' walk, seen). What was missing is the thanks itself: the friend call came with the almanac's news, never with
// "you're the one who cut me loose". Now:
//   the saved come back: the first time a man you saved comes over after it (half a day to three weeks on, in his own town), it's
//     thanks for that very deed, in the words of how you saved him, and a token of his trade (the friend calls' hand-over);
//   the kin of a man you watched them shoot: an execution you found and never stepped into, and they shot him: a man of his family
//     comes to the town five to nine days on and says it to your face (the kin grudges' walk-up, seen: a wrong "watched");
//   captors you let get away strike again: you saved their man and some of them rode off alive: the region hears of them two to
//     four days on, and the next outlaws' execution in that region within two weeks is the same men, who know you ("You again").
//   The road table's town people (the injured, the snakebitten, the stranded: a resident you helped) count as saved, so their second
//     act is the same (the road table is an unproven runner: this only waits for it).
// Why it reads true: gratitude and grudges both need a face to land on (the reciprocity norm is the oldest rule a town keeps); a
// thanks that names the very thing is what makes it memory and not a menu's answer; and the bystander is blamed by the family, not
// the law (Darley and Latane's onlookers, as the bereaved see them). Plain C#, no game; all words this add-on's.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class SecondActs
    {
        public static bool On = true;   // [Encounters] SecondActs, from the settings
        public const string Saved = "saved";       // the friend calls' kind
        public const string Watched = "watched";   // the kin grudges' wrong
        public const int SavedWeight = 400;        // beside the friend call's others (the fight 180, a warning 45...): the saving first, nearly always
        public const long ThankAfter = 720;        // half a day on at the soonest: the shock first, then the thanks
        public const int ThankWithinDays = 21;
        public const string FineSub = "fine";      // the memory of a fine you paid (warmth, Sub "fine": its own words, never "stepped in for me"; SEAMS item 1)
        public const int AgainDays = 14, AtLargeNewsLeast = 2, AtLargeNewsMost = 4;
        public const string WatchedMark = "kin:watched";

        /// The how of a saving, from SavedByYou's words (WestworldScript.Saved.cs), for the saved memory's key: rope, cut, beaten,
        /// pulled, poster, road, or "any".
        public static string HowKind(string how)
        {
            string h = (how ?? "").ToLowerInvariant();
            if (h.Contains("cut loose")) return "cut";   // (before the rope: "cut loose from the rope")
            if (h.Contains("rope")) return "rope";
            if (h.Contains("beating")) return "beaten";
            if (h.Contains("pulled")) return "pulled";
            if (h.Contains("poster")) return "poster";
            if (h.Contains("road")) return "road";
            if (h.Contains("his fine")) return "fine";      // "paid his fine" (SEAMS item 1; never "stood surety", which leaves no memory)
            if (h.Contains("stood with")) return "stood";   // "stood with him in a fight" (SEAMS item 2)
            return "any";
        }

        /// The how kind kept on the end of a saved memory's key ("saved:<id>:<day>:<how>"), or "any" for an older one.
        public static string HowOf(string key)
        {
            if (key == null)
                return "any";
            string tail = key.Substring(key.LastIndexOf(':') + 1);   // the last part: ids have colons of their own ("res:...")
            return Thanks.ContainsKey(tail) ? tail : "any";
        }

        public static string ThankedMark(MindMemory m)
        {
            return "savedthanked:" + (m != null ? m.Key : "");
        }

        /// The saving they haven't thanked you for yet: their latest saved memory (SavedByYou's: Kind warmth, Sub defend, Key
        /// "saved:...") from half a day to three weeks back, not marked thanked in `told`. Null for none.
        public static MindMemory SavedMemory(List<MindMemory> mind, long now, List<string> told)
        {
            if (mind == null)
                return null;
            MindMemory best = null;
            foreach (MindMemory m in mind)
                if (m != null && m.Key != null && m.Key.StartsWith("saved:", StringComparison.Ordinal) && (m.Sub == "defend" || (Interact.FineOwes && m.Sub == FineSub)) && now - m.At >= ThankAfter && now - m.At < ThankWithinDays * 1440L &&
                    (told == null || !told.Contains(ThankedMark(m))) && (best == null || m.At > best.At))
                    best = m;
            return best;
        }

        static readonly Dictionary<string, string[]> Thanks = new Dictionary<string, string[]>
        {
            { "rope", new[] { "You're the one who rode up while they had me on my knees. I've thought of little else since, {you}.",
                              "I'm breathing because you didn't ride on. I wanted to look at you when I said so.",
                              "Out there by the road, with the gun at my head. That was you. I owe you every day since." } },
            { "cut",  new[] { "You cut me loose out there. Trussed on the ground like a calf, and you stopped. {you}, thank you.",
                              "I'd still be lying in that ditch if you hadn't come by. I keep the rope marks to remind me.",
                              "The ropes you cut off me, I burned them. Couldn't stand the sight. Couldn't thank you then, either." } },
            { "beaten", new[] { "That brawl. You put him down before he'd finished me. I've still got my teeth for it.",
                                "He'd have kept on kicking. You stopped him. I don't forget a thing like that, {you}." } },
            { "pulled", new[] { "You pulled him off me in the street, and nobody else stirred. I've been wanting to thank you.",
                                "The whole street watched him at me. You were the one who moved. Thank you, {you}." } },
            { "poster", new[] { "You told me my face was on that paper. I kept out of sight, and they rode on through. Thank you.",
                                "If you'd not warned me about that poster, I'd be in a cell. I owe you that." } },
            { "fine", new[] { "You paid my fine at the jail, and you didn't know me from Adam. I've come to square it, {you}.",
                              "A night in that cell was enough. You saw I didn't get a second. I don't forget a thing like that." } },
            { "stood", new[] { "That fight. I was going down and you waded in beside me. I've wanted to say so since, {you}.",
                               "You took blows that were meant for me. Not many would. I owe you for it." } },
            { "road", new[] { "You stopped for me on the road when nobody else would. I'm home and whole for it.",
                              "Out there on the road, you were the only one who pulled up. I've told everybody since." } },
            { "any",  new[] { "I never thanked you right for what you did for me. I'm doing it now.",
                              "You did me a turn when I needed it most, {you}. I've not forgotten." } },
        };

        static readonly Dictionary<string, string[]> Token = new Dictionary<string, string[]>
        {
            { "patch", new[] { "Sit a moment. Let me look you over. No charge, not to you.", "Hold still. I'll see to those hurts of yours, free." } },
            { "rounds", new[] { "Here, a box of rounds. You'll put them to better use than I would.", "Take these cartridges. It's what I have." } },
            { "drink", new[] { "Your drink's on the house. Today and any day.", "There's a glass poured for you. Don't argue." } },
            { "meal", new[] { "I put something by for you to eat. Go on, take it.", "Here's bread and a bit of meat for the road." } },
            { "coin", new[] { "It's not much. Take it anyway.", "Don't insult me by saying no. Take it." } },
        };

        /// What they say to your face: thanks for the very thing, by how you saved them, and the token (the friend calls' detail:
        /// patch, rounds, drink, meal, coin).
        public static string Line(string how, string detail, string you, string seed)
        {
            string[] pool;
            if (!Thanks.TryGetValue(how ?? "any", out pool))
                pool = Thanks["any"];
            string line = Voice.Pick(seed + "|sv|" + how, pool).Replace("{you}", you ?? "mister");
            string[] tok;
            if (detail != null && Token.TryGetValue(detail, out tok))
                line += " " + Voice.Pick(seed + "|tk|" + detail, tok);
            return Grammar.UpperFirst(line.Trim());
        }

        /// Their own thought as they go.
        public static string After(string seed)
        {
            return Voice.Pick(seed + "|sva", "Said it at last. Took me long enough.", "He just nodded. Men like that always just nod.", "Some debts you can't pay. You can say so, at least.");
        }

        /// The kin's words for a man shot while you watched (KinGrudges.Opening's close, by the kin's temper).
        public static string WatchedClose(string temper, string seed)
        {
            switch (temper)
            {
                case "Hothead": return Voice.Pick(seed + "|wc", "He was worth ten of the men who did it, and you let them.", "Next time you sit and watch a man die, think of my face.");
                case "Proud": return Voice.Pick(seed + "|wc", "I wanted to see the face of a man who'd sit his horse and watch. I've seen it.", "My family doesn't forget who stood by.");
                case "Cold": return Voice.Pick(seed + "|wc", "I don't ask what you'd have done. I know what you did. Nothing.", "You'll carry it or you won't. I'm done with you.");
                default: return Voice.Pick(seed + "|wc", "Somebody had to say it to you. Now it's said.", "You could have stopped it. Maybe you couldn't. Either way, you were there.");
            }
        }

        /// The kin's thought after, for a man you watched die.
        public static string WatchedThought(string first, string seed)
        {
            return Voice.Pick(seed + "|wt", "Said it to his face. " + first + " can rest a little easier.", "He didn't say a word. What could he say?", "I thought I'd feel better. I don't. But it's said.");
        }

        /// The captors' first word to you when they're the men you let get away last time (Executions.WarnYou with Plan.Again).
        public static string Again(string seed)
        {
            return Voice.Pick(seed + "|ag", "You again. Not this time, mister.", "We know you. Ride on, or kneel down next to him.", "Last time you got lucky. Keep riding.");
        }

        /// What the region hears of men you let get away, a few days on.
        public static string AtLargeNews(string gang, string region, string seed)
        {
            string who = gang ?? "The men who held that fellow on the road";
            string where = region ?? "the roads";
            return Voice.Pick(seed + "|al", who + " have been seen around " + where + " again. Folks are keeping their doors barred.",
                                            "Word is " + who + " haven't gone far. A rider saw them near " + where + ".");
        }

        /// The day the news comes, two to four days on.
        public static long AtLargeNewsDue(long at, string seed)
        {
            return at + (AtLargeNewsLeast + Voice.Hash(seed + "|ald") % (AtLargeNewsMost - AtLargeNewsLeast + 1)) * 1440L;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in Thanks)
                foreach (string s in kv.Value)
                    yield return s.Replace("{you}", "Morgan");
            foreach (var kv in Token)
                foreach (string s in kv.Value)
                    yield return s;
        }
    }
}
