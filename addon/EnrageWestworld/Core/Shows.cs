// The Wild West show (SIXDAY item 10d; roadmap 10; roadmap.md P24), an event on the town-events framework (Core/TownEvents.cs).
// Once the player's legend has travelled (his most-told deed known in four towns, one of them three hops from where it
// happened), a touring show comes to a large town and acts out the most bent version of it: posters a week ahead, a barker, a
// crowd, riders in a ring with blanks, the act billed under a name the rumour gave him. Away, the paper reviews it and the
// show's own telling bends the legend further, from that town outward. There, he can watch, heckle (the crowd turns on him),
// stand up and say who he is (a deed: the crowd stares, some leave, the paper has it), or rob the takings. Why it reads true:
// Buffalo Bill's Wild West was acting out the previous year's news by 1899, and the people in the ring were sometimes the
// people in the news. Plain C#, no game; all words original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Shows
    {
        public const int GapDays = 30, PostDaysAhead = 7, ShowFrom = 14, ShowTo = 16;
        public const int LeastTowns = 4, LeastHops = 3;       // fame: the deed told in four towns, one of them three hops from the first
        public const int LeastWeight = 50;
        public const int BendLoss = 15;                       // the show's telling: the town's version bent further
        public const int HeckleSoured = -5, HeckleRegulars = 6;
        public const int TakingsLeast = 4000, TakingsMost = 9000;   // cents
        public const int RevealWeight = 45;
        public static readonly string[] Towns1899 = { "Saint Denis", "Valentine", "Rhodes" };
        public static readonly string[] Towns1907 = { "Saint Denis", "Valentine", "Rhodes", "Blackwater" };
        public static readonly string[] Companies = { "Colonel Hatch's Wild West", "Captain Dunmore's Congress of Rough Riders", "Major Pell's Great Western Exhibition", "Doc Varley's Frontier Pageant" };
        public static readonly string[] RobberyKinds = { "robbery", "bankrobbery", "trainrobbery", "stagecoach" };
        public const string TestVersion = "a ghost in a gray hat held up the Lemoyne flyer single-handed and rode off laughing";

        public static string Company(string seed)
        {
            return Companies[Voice.Hash(seed + "|co") % Companies.Length];
        }

        public static string[] TownsFor(int year)
        {
            return year >= 1907 ? Towns1907 : Towns1899;
        }

        /// Whether a deed can carry a legend: known, heavy, violent or a robbery.
        public static bool Legendary(Deed d)
        {
            return d != null && d.Known && d.Weight >= LeastWeight && (Deeds.IsViolent(d.Kind) || RobberyKinds.Contains(d.Kind));
        }

        /// The most-told legendary deed: the one with rumours in the most towns, the heavier first on a tie.
        public static Deed MostTold(List<Deed> deeds, List<TownRumour> rumours)
        {
            if (deeds == null)
                return null;
            Deed best = null;
            int bestTowns = -1;
            foreach (Deed d in deeds)
            {
                if (!Legendary(d))
                    continue;
                int towns = rumours == null ? 0 : rumours.Count(r => r != null && r.DeedId == d.Id);
                if (towns > bestTowns || (towns == bestTowns && best != null && d.Weight > best.Weight))
                {
                    best = d;
                    bestTowns = towns;
                }
            }
            return best;
        }

        /// The furthest version of a deed's talk: the most hops, then the least faithful.
        public static TownRumour Furthest(List<TownRumour> rumours, string deedId)
        {
            TownRumour best = null;
            if (rumours == null || deedId == null)
                return null;
            foreach (TownRumour r in rumours)
                if (r != null && r.DeedId == deedId && (best == null || r.Hops > best.Hops || (r.Hops == best.Hops && r.Fidelity < best.Fidelity)))
                    best = r;
            return best;
        }

        public static bool FameEnough(int towns, int maxHops)
        {
            return towns >= LeastTowns && maxHops >= LeastHops;
        }

        /// The large town the show comes to: one he has been to, the nearest to where the furthest version is told.
        public static string PickTown(string[] large, Dictionary<string, long> visits, string furthestTown)
        {
            if (large == null || visits == null)
                return null;
            string best = null;
            float bestD = float.MaxValue;
            foreach (string t in large)
            {
                if (!visits.ContainsKey(t))
                    continue;
                float d = furthestTown != null ? Rumours.Distance(t, furthestTown) : 0f;
                if (d < bestD)
                {
                    best = t;
                    bestD = d;
                }
            }
            return best;
        }

        public static int Bend(int fidelity)
        {
            return Math.Max(5, fidelity - BendLoss);
        }

        /// The region's name on the bill, from where the deed was done.
        public static string RegionName(string originTown)
        {
            switch (Calendar.Region(originTown))
            {
                case "south": return "LEMOYNE";
                case "high": return "THE GRIZZLIES";
                default: return "THE HEARTLANDS";
            }
        }

        /// The act's name on the poster: what the furthest rumour made of him.
        public static string Bill(string kind, string originTown, string seed)
        {
            string region = RegionName(originTown);
            switch (kind)
            {
                case "trainrobbery": return Voice.Pick(seed, "THE TERROR OF THE RAILS", "THE MAN WHO STOPPED THE FLYER");
                case "bankrobbery": return Voice.Pick(seed, "THE BANK-BREAKER OF " + region, "THE GENTLEMAN ROBBER");
                case "robbery": case "stagecoach": return Voice.Pick(seed, "THE GENTLEMAN ROBBER OF " + region, "THE ROAD AGENT NOBODY CAUGHT");
                case "lawkill": return Voice.Pick(seed, "THE SCOURGE OF THE LAW", "THE DEVIL OF " + region);
                default: return Voice.Pick(seed, "THE DEVIL OF " + region, "THE MAN THEY CAN'T KILL", "THE GHOST IN THE GRAY HAT");
            }
        }

        /// The show, posted a week ahead: two till four.
        public static TownEvent Make(string town, long day, Deed d, TownRumour furthest, string seed)
        {
            string company = Company(seed);
            return new TownEvent
            {
                Id = Ids.New(), Kind = "show", Town = town, Title = "The Wild West show", At = day * 1440 + ShowFrom * 60, Until = day * 1440 + ShowTo * 60,
                AId = d != null ? d.Id : null, AName = company, BId = furthest != null ? furthest.Id : null, Extra = Bill(d != null ? d.Kind : null, d != null ? d.Town : town, seed),
                Count = furthest != null ? furthest.Hops : 0
            };
        }

        public static int Takings(string seed)
        {
            return TakingsLeast + Voice.Hash(seed + "|take") % (TakingsMost - TakingsLeast + 1);
        }

        // ---- the words ----

        public static string PostedText(TownEvent e)
        {
            return e.AName + " comes to " + e.Town + " a week today, two o'clock, on the ground outside town. On the bill: " + e.Extra + ".";
        }

        public static string PaperText(TownEvent e)
        {
            return e.AName + " will exhibit at " + e.Town + " on the afternoon of the " + Voice.Pick(e.Id, "coming", "next") + " week's day, with riding, roping and shooting, and an act from the late news of this territory entitled " + e.Extra + ". Admission fifty cents, children half.";
        }

        public static string PosterBody(TownEvent e)
        {
            return e.AName.ToUpperInvariant() + "\n\nONE AFTERNOON ONLY at " + e.Town.ToUpperInvariant() + "\n\nRough riders of two continents! Trick shooting! The lasso!\n\nAND, acted in the ring exactly as it happened:\n\n   " + e.Extra + "\n\nA true scene of this territory, from the late news.\n\nTwo o'clock sharp. Fifty cents. Children half.";
        }

        /// The paper's review, after.
        public static string Review(TownEvent e, string version, bool revealed, bool robbed, bool heckled)
        {
            string v = Grammar.SoftLower((version ?? TestVersion).TrimEnd('.'));
            string s = e.AName + " played " + e.Town + " to a good crowd. The riding was well liked and the act billed as " + e.Extra + " better still, in which it was shown how " + v + ".";
            if (revealed)
                s += " A man in the crowd stood up at the end and announced himself the original of the piece, to some sensation; the management did not dispute it.";
            if (robbed)
                s += " The takings were carried off before the ground was cleared, by a person the crowd had taken for part of the act.";
            else if (heckled)
                s += " There was some heckling, which the barker answered to the crowd's satisfaction.";
            if (!revealed && !robbed)
                s += " Several in the crowd were heard to say the real one is in the territory yet.";
            return s;
        }

        public static string OverToast(TownEvent e, bool attended)
        {
            return attended ? "The show in " + e.Town + " is over. The crowd went home talking about the real one." : e.AName + " played " + e.Town + " this afternoon, they say. The act was " + e.Extra + ".";
        }

        // ---- the barker, in captions ----

        public static string[] Opening(TownEvent e)
        {
            return new[]
            {
                "Ladies and gentlemen! " + e.AName + "! Riders of two continents, and the finest shots west of the Mississippi!",
                "And now, friends, the act you came for. Taken from the news of this very territory, and acted as it happened.",
                "I give you... " + e.Extra + "!",
            };
        }

        /// The act, told from the bent version: four lines over the riders.
        public static string[] Act(string version, string seed)
        {
            string v = Grammar.SoftLower((version ?? TestVersion).TrimEnd('.'));
            return new[]
            {
                "The riders come in at a gallop. The barker's voice over them: \"Here, friends, is the day " + v + "!\"",
                Voice.Pick(seed + "|2", "Blanks, loud as the real thing. The crowd jumps and laughs at itself for jumping.", "A pistol goes off, then three. The horses dance. A woman in the front row has her hands over her ears and is grinning."),
                Voice.Pick(seed + "|3", "The man in the gray hat stands over the fallen. Nobody in the ring is hurt. Nobody in the crowd is sure.", "The one playing him rides the ring slow with his hat off, bowing. He's younger than the real one. Handsomer, too."),
                Voice.Pick(seed + "|4", "The crowd roars. Somebody fires into the air for real, and the barker shouts him down.", "Applause like rain on a roof. Two boys at the rope are already playing it over."),
            };
        }

        public static string Closing(string seed)
        {
            return Voice.Pick(seed, "And the real one, friends? They say he's in this very territory tonight. Sleep well!", "Remember what you saw, friends. The real one's out there yet, and some of you may meet him.");
        }

        public static string HeckleRetort(string seed)
        {
            return Voice.Pick(seed, "A critic! Friends, we have a critic! Sir, the ticket office will refund your fifty cents, and your opinion with it!", "The gentleman says it never happened that way. Were you there, sir? No? Then sit down and let the people who paid enjoy it!");
        }

        public static string RevealLine(string seed)
        {
            return Voice.Pick(seed, "Friends... friends. The gentleman says he's the real one. Well, look at him. I believe it. Ladies and gentlemen, give him a hand, and give him room!", "The original! Here in the crowd! Sir, I'd offer you a place in the company, but I don't believe you'd take direction.");
        }

        public static string RobbedShout(string seed)
        {
            return Voice.Pick(seed, "The box! He's got the box! Stop him, somebody, that's the day's takings!", "Thief! That's the takings! Will nobody... it's not part of the act, damn you, stop him!");
        }

        /// The crowd's words, for the memories of the town's people after.
        public static string CrowdAfter(bool revealed, string seed)
        {
            return revealed
                ? Voice.Pick(seed, "He stood up. The real one. I was six feet from him.", "I thought the man in the ring was something. Then the real one stood up and the ring looked small.")
                : Voice.Pick(seed, "A good show. But the real one's out there, and that's not a show.", "The boys will be playing it in the street for a month.");
        }

        /// The player's rows at the show.
        public static List<InteractOption> Rows(TownEvent e)
        {
            string part = e != null ? e.Part ?? "" : "";
            return new List<InteractOption>
            {
                new InteractOption { Id = "show:heckle", Label = "Heckle the show.", Why = part.Contains("heckled") ? "once is enough" : null },
                new InteractOption { Id = "show:reveal", Label = "Stand up and say who you are.", Why = part.Contains("revealed") ? "they know" : null },
                new InteractOption { Id = "show:rob", Label = "Rob the takings." },
                new InteractOption { Id = "show:watch", Label = "Just watch." },
            };
        }

        public static IEnumerable<string> AllLines()
        {
            var d = new Deed { Id = "d", Kind = "trainrobbery", Town = "Rhodes", Weight = 60 };
            var r = new TownRumour { Id = "r", DeedId = "d", Hops = 3, Fidelity = 20, Town = "Valentine" };
            TownEvent e = Make("Valentine", 10, d, r, "s");
            yield return PostedText(e); yield return PaperText(e); yield return PosterBody(e);
            yield return Review(e, TestVersion, false, false, false); yield return Review(e, TestVersion, true, false, true); yield return Review(e, TestVersion, false, true, false);
            yield return OverToast(e, true); yield return OverToast(e, false);
            foreach (string s in Opening(e)) yield return s;
            for (int i = 0; i < 3; i++)
            {
                foreach (string s in Act(TestVersion, "a" + i)) yield return s;
                yield return Closing("c" + i); yield return HeckleRetort("h" + i); yield return RevealLine("v" + i); yield return RobbedShout("b" + i);
                yield return CrowdAfter(true, "w" + i); yield return CrowdAfter(false, "x" + i);
            }
            foreach (string kind in new[] { "trainrobbery", "bankrobbery", "robbery", "stagecoach", "lawkill", "murder", "kill", null })
                foreach (string town in new[] { "Valentine", "Rhodes", "Strawberry" })
                    yield return Bill(kind, town, "b");
            foreach (InteractOption o in Rows(e)) yield return o.Label;
            foreach (string c in Companies) yield return c;
        }
    }
}
