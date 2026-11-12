// What they saw themselves blurs too (LIVES item 1c, 2026-11-12; [Mind] MemoryBlur, settings version 463). Memory v1
// (Minds.Memory.cs Drifted) already bends what was told or heard (the unknown doer becomes their foe, a slight grows, two
// become three) and lets strangers' names go to "somebody" past twice a memory's half-life; but a thing seen with one's own
// eyes kept every detail until then. Real memory is reconstructed each time it's recalled, and the details go first: the
// day of the week, the weather, who stood where, how many there were (Loftus; Bartlett's "War of the Ghosts", 1932). The
// painful kind does the opposite: the flashbulb memory of a death or a terror feels as sharp as the day, even when it isn't
// (Brown & Kulik, 1977; Talarico & Rubin, 2003: vivid and confident, not more accurate).
//
// So once a first-hand memory is older than its half-life, some of the times it comes to mind it comes as the person
// doubting its details ("That quarrel. Was it a Tuesday? I'd have said a Tuesday."), unless it was strong (70 or more) or a
// death, which come as too sharp ("I could tell you what everyone had on. Down to the buttons."). The player's own deeds keep
// their own rules (they never drift here: the rumours bend them, Core/Rumours.cs), and so do things told or heard.
//
// No new state: it reads the memory's kind, how it was had, its strength and age. Off in Core so the goldens stand.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MemoryBlur
    {
        /// [Mind] MemoryBlur: set by the game from the settings.
        public static bool On;

        public const int Chance = 40;          // of the times an old first-hand memory comes to mind
        public const int SharpStrength = 70;   // this strong, or a death: too sharp, not blurred
        public const long LeastAge = 2 * 1440;  // and two game days at least, whatever its half-life (the review: a quarrel's is 18 hours)

        /// "blur", "sharp" or null: how this memory comes to mind now, for this seed.
        public static string How(MindMemory m, long now, string seed)
        {
            if (!On || m == null || m.From != null || m.How == "heard" || Minds.AboutYou(m) || m.Kind == null || m.Kind == "rescued")
                return null;
            if (now - m.At < Minds.HalfLife(m) || now - m.At < LeastAge)
                return null;   // still fresh: as it was
            if (!Voice.Chance(seed + "|blur", Chance))
                return null;
            return m.Strength >= SharpStrength || m.Kind == "death" || m.Kind == "deed:violence" ? "sharp" : "blur";
        }

        /// The group of lines a memory's kind reads from.
        public static string GroupOf(string kind)
        {
            switch (kind)
            {
                case "quarrel": case "slight": case "blows": return "fight";
                case "theft": case "deed:theft": return "theft";
                case "warmth": case "rescued": case "deed:kindness": return "kind";
                case "death": case "deed:violence": return "death";
                default: return "other";
            }
        }

        /// The thought, or null.
        public static string Line(MindMemory m, string how, string seed, List<string> told)
        {
            string[] pool;
            if (m == null || how == null || !Lines.TryGetValue(how + ":" + GroupOf(m.Kind), out pool))
                return null;
            return pool[(Voice.Hash(seed + "|blurl") & 0x7fffffff) % pool.Length];   // (a line said lately is passed over by the candidate's own check, SaidToday)
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in Lines)
                foreach (string l in kv.Value)
                    yield return l;
        }

        private static readonly Dictionary<string, string[]> Lines = new Dictionary<string, string[]>
        {
            { "blur:fight", new[] {
                "That set-to by the store. Was it a Tuesday? I'd have said a Tuesday.",
                "Who threw the first punch that day? I knew once. I'd swear to either one now.",
                "That quarrel. Could've sworn it was raining. Maybe that was another time.",
                "The fight I saw. Two of them, or three? It grows when I'm not looking.",
                "Remember the words, near enough. Can't for the life of me call up the faces." } },
            { "blur:theft", new[] {
                "The robbery I saw. Morning, I think. Or it felt like morning.",
                "Thief had a hat. Brown. Or black. A hat, anyhow.",
                "Tried to tell it straight the other day and caught myself adding to it.",
                "Saw it plain as day, I told the deputy. Plain as day is getting dim." } },
            { "blur:kind", new[] {
                "Somebody was good to me. The kindness I keep. The rest is going.",
                "Can't recall what was said. Only that I walked home lighter.",
                "Was it morning or evening? It was warm, whatever it was. That's what stayed.",
                "Lost the day of it and kept the feeling. Good trade." } },
            { "blur:other", new[] {
                "Something was said that day I meant to remember. Gone now.",
                "Had it all clear once. Now it's a picture somebody left in the rain.",
                "Funny what stays. The smell of the coffee. Not a word of what was said.",
                "Told it to myself so often I don't know which parts were there." } },
            { "sharp:fight", new[] {
                "Could tell you every word of that fight. Wish I couldn't.",
                "I could still name everyone who stood there. Every last one.",
                "Close my eyes and I'm back there. Same light. Same noise." } },
            { "sharp:theft", new[] {
                "I'd know that face in a crowd of a thousand. I see it in my sleep.",
                "Could draw you the coat he wore, button by button." } },
            { "sharp:kind", new[] {
                "Every word of it. I keep it like a coin in my pocket.",
                "Clear as the day it happened. Clearer, maybe." } },
            { "sharp:death", new[] {
                "Time is supposed to soften it. Nobody told time.",
                "Every face that was there, I could draw you. I'd sooner not.",
                "The sound of it. I'll have the sound of it when I'm old.",
                "Remember the light on the street. The very light. Isn't that a strange thing to keep." } },
            { "sharp:other", new[] {
                "Odd what stays sharp. That I'll remember on my deathbed, and I don't know why.",
                "Plain as if it were this morning." } },
        };
    }
}
