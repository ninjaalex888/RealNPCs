// Worries you can change, all of them, with their dark mirrors (SIXDAY item 9; roadmap 9; roadmap.md P1 and P13, on the
// settled mark). Every ordinary person's trouble can be mended through Talk once they've told you of it, and every one can be
// made worse. The money worries (the debt, the doctor, the dentist, the fare) keep their row; the rest get theirs here: a
// dollar for the telegraph, the water cart, the fare to the mine, a word put in, a word with the neighbour, the bank note, coffee
// instead of whiskey, the marker, a secret kept, the stage out of town. The mirror of each is a word that turns the trouble the
// wrong way: the creditor told where to find them, the fever called a chill, the letter given up on, the farm sold up, the mine's
// bad news, a word in the wrong ear, the neighbour's insult carried, the bank's doom, another whiskey, a game pointed out, a sure
// thing for the savings, the tooth pulled by hand, the secret already known, the sheriff asking. Why it reads true: a trouble
// told to a stranger is a door held open, and what the stranger says through it lands with the weight of a verdict; the
// frightened believe the worst at once, the proud and the cold want proof, and nobody forgets who made it worse. Plain C#, no
// game; all words original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One thing he can do about a worry: a mend or its mirror.
    public sealed class Help
    {
        public string Worry, Kind, Id, Label, Outcome;   // Kind: mend | mirror; Id: the row ("mend:letter")
        public int Cents;
        public bool Saloon;                              // only in a saloon (coffee, a whiskey)
    }

    public static class Helps
    {
        public static readonly string[] Worries = { "debt", "fever", "letter", "drought", "son", "courting", "feud", "bank", "drink", "cards", "savings", "tooth", "secret", "law" };
        public const string HarmOutcome = "made their trouble worse";
        public const int MendLiking = 25, HarmLiking = -15, DoubtLiking = -5;
        public const int MendAffection = 15, HarmAffection = -15;

        /// The mend on offer for a worry that money doesn't already mend through the settle row; null for those four.
        public static Help Mend(string worry)
        {
            switch (worry)
            {
                case "letter": return new Help { Worry = worry, Kind = "mend", Id = "mend:letter", Label = "Here, a dollar for the telegraph ($1)", Cents = 100, Outcome = "paid for a telegraph" };
                case "drought": return new Help { Worry = worry, Kind = "mend", Id = "mend:drought", Label = "Here, toward the water cart ($5)", Cents = 500, Outcome = "paid toward the water cart" };
                case "son": return new Help { Worry = worry, Kind = "mend", Id = "mend:son", Label = "Here, the fare to go and see him ($5)", Cents = 500, Outcome = "paid their fare to the mine" };
                case "courting": return new Help { Worry = worry, Kind = "mend", Id = "mend:courting", Label = "I'll put in a word for you.", Cents = 0, Outcome = "put in a word for them" };
                case "feud": return new Help { Worry = worry, Kind = "mend", Id = "mend:feud", Label = "Let me have a word with your neighbour.", Cents = 0, Outcome = "had a word with their neighbour" };
                case "bank": return new Help { Worry = worry, Kind = "mend", Id = "mend:bank", Label = "Here, toward the note ($10)", Cents = 1000, Outcome = "paid toward their note" };
                case "drink": return new Help { Worry = worry, Kind = "mend", Id = "mend:drink", Label = "Coffee, not whiskey. On me.", Cents = 25, Saloon = true, Outcome = "stood them coffee" };
                case "cards": return new Help { Worry = worry, Kind = "mend", Id = "mend:cards", Label = "Let me pay your marker ($5)", Cents = 500, Outcome = "paid their marker" };
                case "secret": return new Help { Worry = worry, Kind = "mend", Id = "mend:secret", Label = "Your secret's safe with me.", Cents = 0, Outcome = "kept their secret" };
                case "law": return new Help { Worry = worry, Kind = "mend", Id = "mend:law", Label = "Here, for the stage out of town ($5)", Cents = 500, Outcome = "paid their stage out of town" };
                default: return null;
            }
        }

        /// The dark mirror: a word that turns the trouble the wrong way. One for every worry.
        public static Help Mirror(string worry)
        {
            switch (worry)
            {
                case "debt": return M(worry, "Your creditor asked after you. I said where you'd be.");
                case "fever": return M(worry, "It's a chill, is all. Save your money.");
                case "letter": return M(worry, "I'd not wait on that letter. They'd have written by now.");
                case "drought": return M(worry, "Sell up now, before the bank does it for you.");
                case "son": return M(worry, "I heard about that mine. You should know.");
                case "courting": return M(worry, "I told them what you said about them.");
                case "feud": return M(worry, "Your neighbour called you a coward. I heard it myself.");
                case "bank": return M(worry, "The bank's calling in every note in the county.");
                case "drink": return M(worry, "Another whiskey for my friend here.", 50, true);
                case "cards": return M(worry, "There's a game tonight at the back. Deep pockets.");
                case "savings": return M(worry, "I know a sure thing for that money of yours.");
                case "tooth": return M(worry, "Hold still. I'll have it out myself.");
                case "secret": return M(worry, "The whole town knows already, you know.");
                case "law": return M(worry, "The sheriff was asking your name this morning.");
                default: return null;
            }
        }

        private static Help M(string worry, string label, int cents = 0, bool saloon = false)
        {
            return new Help { Worry = worry, Kind = "mirror", Id = "mirror:" + worry, Label = label, Cents = cents, Saloon = saloon, Outcome = HarmOutcome };
        }

        /// Whether a help happening's outcome is one of these mends (Thanks reads it as a mended worry).
        public static bool IsMendOutcome(string outcome)
        {
            if (outcome == null)
                return false;
            foreach (string w in Worries)
            {
                Help h = Mend(w);
                if (h != null && h.Outcome == outcome)
                    return true;
            }
            return false;
        }

        private static string Pick(string seed, params string[] lines)
        {
            return lines[Voice.Hash(seed) % lines.Length];
        }

        /// Their answer: a mend taken or refused (the hostile take nothing of his; the proud won't be paid for, but take a word),
        /// a mirror believed or doubted (the frightened believe the worst at once, the proud and the cold half the time want
        /// proof, the rest three times in four). A believed mirror sets `Worsens`; a taken mend `Settles`.
        public static InteractAnswer Answer(Help h, string temper, bool woman, string regard, string seed)
        {
            var a = new InteractAnswer { Then = "stay" };
            if (h == null)
            {
                a.Line = "...";
                return a;
            }
            bool hostile = regard == "hostile", afraid = regard == "afraid";
            if (h.Kind == "mend")
            {
                if (hostile)
                {
                    a.Line = h.Cents > 0 ? "I'll not take a cent from you." : "I want no help of yours.";
                    a.VoiceHate = 40;
                    return a;
                }
                if (temper == "Proud" && h.Cents > 0 && !afraid)
                {
                    a.Line = Pick(seed + "|hp", "I'll not be paid for, mister. Thank you, no.", "Keep your money. I'll manage, as I always have.");
                    a.Thought = "Charity. From a stranger. As if I'd come to that.";
                    return a;
                }
                a.Event = "help";
                a.Cents = h.Cents;
                a.Settles = true;
                a.LikeDelta = MendLiking;
                a.Friendly = !afraid;
                if (afraid)
                {
                    a.Line = "You... you'd do that? I won't forget it, mister.";
                    a.Thought = "Help, from him. What does he want for it?";
                    a.VoiceFear = 20;
                    return a;
                }
                switch (h.Worry)
                {
                    case "letter": a.Line = Pick(seed + "|ml", "A dollar for the wire. I'll know by tonight, one way or the other.", "The telegraph. Why didn't I think of it. Thank you."); a.Thought = "Wired them. Answer by dark. I can wait till dark."; break;
                    case "drought": a.Line = Pick(seed + "|md", "Water. Lord. That's a week bought.", "The cart'll come tomorrow, with this. A week's life for the corn."); a.Thought = "A week of water. The sky's got a week to change its mind."; break;
                    case "son": a.Line = Pick(seed + "|ms", "The fare. I'll go Saturday. I'll see him with my own eyes.", "I can go to him. I can go and see for myself."); a.Thought = "Saturday's stage. I'll see my boy. I'll see him."; break;
                    case "courting": a.Line = Pick(seed + "|mc", "You'd speak for me? ...Don't make me sound a fool.", "A word from a stranger might do what mine can't. Go on, then."); a.Thought = "He'll speak to them. What if they say no? What if they say yes?"; break;
                    case "feud": a.Line = Pick(seed + "|mf", "Talk to him? He'll hear a stranger sooner than me. Go on, then.", "A word with him. All right. Before it comes to the gun."); a.Thought = "A stranger between us. Maybe that's what it needed."; break;
                    case "bank": a.Line = Pick(seed + "|mb", "Toward the note. It buys a month. A month's a lot.", "Ten dollars. The bank can wait a month now. So can I."); a.Thought = "A month bought. Pray the crop comes in."; break;
                    case "drink": a.Line = Pick(seed + "|mk", "Coffee. ...All right. Coffee.", "Coffee, he says. Fine. One night sober won't kill me."); a.Thought = "Coffee. Hands shaking. One night. Start with one night."; break;
                    case "cards": a.Line = Pick(seed + "|mg", "My marker? You'd pay that? I'll not sit down again. I swear it.", "Paid. Lord. I'll not touch a card. Not this month."); a.Thought = "Marker's paid. The itch is still there. Sit on it."; break;
                    case "secret": a.Line = Pick(seed + "|mx", "You'd keep it? ...I believe you. God help me, I believe you.", "Safe with you. All right. I'll hold you to it."); a.Thought = "Told him. He'll keep it. Lighter already, somehow."; break;
                    case "law": a.Line = Pick(seed + "|mw", "The stage. Tonight. I'll be gone before the poster's dry.", "Out of town. Yes. A new name somewhere nobody reads papers."); a.Thought = "Tonight's stage. New town, new name. Don't look back."; break;
                    default: a.Line = "You'd do that? I'll not forget it."; break;
                }
                if (temper == "Greedy")
                    a.Thought = "Wonder what else he'd do for a man, a man like that.";
                else if (temper == "Cold")
                    a.Thought = "Nobody helps for nothing. Still. It's done.";
                return a;
            }
            // the mirror
            if (hostile)
            {
                a.Line = "What would you know of it? Get away from me.";
                a.VoiceHate = 40;
                a.LikeDelta = DoubtLiking;
                return a;
            }
            int roll = Voice.Hash(seed + "|believe") % 100;
            bool believes = afraid || temper == "Coward" || ((temper == "Proud" || temper == "Cold") ? roll < 50 : roll < 75);
            if (!believes)
            {
                a.Line = Pick(seed + "|db", "I'll believe that when I see it.", "What would you know of it, mister?", "That's talk. I've heard talk.");
                a.Thought = "Stranger talking out of his hat. Pay it no mind.";
                a.LikeDelta = DoubtLiking;
                return a;
            }
            a.Event = "harm";
            a.Worsens = true;
            a.Cents = h.Cents;   // the drink bought is paid for (the review: it was free)
            a.LikeDelta = HarmLiking;
            a.VoiceFear = afraid || temper == "Coward" ? 40 : 10;
            a.VoiceHate = temper == "Hothead" ? 30 : 0;
            switch (h.Worry)
            {
                case "debt": a.Line = Pick(seed + "|xd", "You... you told him where I'd be?", "You've done for me, then. He'll have the mule by Saturday."); a.Thought = "He'll come for the mule now. Nowhere to hide it."; break;
                case "fever": a.Line = Pick(seed + "|xf", "A chill. You think so? ...Maybe you're right. Maybe it's only a chill.", "Save the money. Yes. Two dollars is two dollars."); a.Thought = "A chill, he says. God forgive me, I'll keep the two dollars."; break;
                case "letter": a.Line = Pick(seed + "|xl", "They'd have written. Yes. They'd have written by now.", "You're right. I'll stop walking past the post office."); a.Thought = "No letter coming. He said it plain. No letter coming."; break;
                case "drought": a.Line = Pick(seed + "|xr", "Sell up. ...Maybe. Before the bank. Lord.", "You think it's that bad? ...You're right. It's that bad."); a.Thought = "Sell up, he said. Three generations. Sell up."; break;
                case "son": a.Line = Pick(seed + "|xs", "The mine? What about the mine? What did you hear?", "...Don't. Don't tell me. ...Tell me."); a.Thought = "Something at the mine. He heard something. My boy."; break;
                case "courting": a.Line = Pick(seed + "|xc", "You told them? Why? Why would you do that?", "...Then it's over before it began."); a.Thought = "They know what I said. I can never look at them now."; break;
                case "feud": a.Line = Pick(seed + "|xe", "A coward? He called me that? We'll see who's the coward.", "He said that. Out loud. ...Then it's past talking."); a.Thought = "Coward. He'll eat that word. Tonight."; break;
                case "bank": a.Line = Pick(seed + "|xb", "Every note? Then it's over. Then we're done.", "Calling them in. All of them. ...There's no getting ahead of that."); a.Thought = "Every note. The farm's gone. It's only the date left to settle."; break;
                case "drink": a.Line = Pick(seed + "|xk", "Another? Well. If you're buying.", "One more. One more never hurt."); a.Thought = "One more. Always one more. Tomorrow I'll stop. Tomorrow."; break;
                case "cards": a.Line = Pick(seed + "|xg", "A game? Tonight? ...Deep pockets, you say.", "I shouldn't. I said I wouldn't. ...What time?"); a.Thought = "One game. Win it back. Win it all back."; break;
                case "savings": a.Line = Pick(seed + "|xv", "A sure thing? For the whole of it?", "You'd put me on to it? ...Tell me more."); a.Thought = "A sure thing. The can's full enough. One throw and I'm gone west first class."; break;
                case "tooth": a.Line = Pick(seed + "|xt", "Yourself? You're no dentist. ...Go on, then. Quick.", "Pull it? Here? ...Lord. Do it."); a.Thought = "He pulled it. Half of it. The rest's still in there, throbbing."; break;
                case "secret": a.Line = Pick(seed + "|xx", "They know? All of them? ...Then I'm finished here.", "The whole town. ...I thought I'd been careful."); a.Thought = "Everyone knows. Every nod in the street means something now."; break;
                case "law": a.Line = Pick(seed + "|xw", "The sheriff? My name? ...I have to go. Now.", "Asking my name. Then he knows. Then it's done."); a.Thought = "The sheriff knows. Run tonight, or hang by Friday."; break;
                default: a.Line = "...You're sure of that?"; break;
            }
            return a;
        }

        /// Every line, for the offline checks.
        public static IEnumerable<string> AllLines()
        {
            foreach (string w in Worries)
                foreach (string temper in new[] { null, "Hothead", "Coward", "Proud", "Cold", "Greedy" })
                    foreach (string regard in new[] { null, "afraid", "hostile", "fond" })
                    {
                        foreach (Help h in new[] { Mend(w), Mirror(w) })
                        {
                            if (h == null)
                                continue;
                            yield return h.Label;
                            for (int s = 0; s < 3; s++)
                            {
                                InteractAnswer a = Answer(h, temper, false, regard, "s" + s + temper + regard);
                                yield return a.Line;
                                if (a.Thought != null) yield return a.Thought;
                            }
                        }
                    }
        }
    }
}
