// Stage 1 of 0.7.12 (NIGHT4 queue item 3; Lex, 2026-10-31: people near Arthur visibly act on their own minds): the mind's
// everyday choices acted through the game's own scenarios, found by type. Until now a rest, a meal, a drink or the work took
// whatever scenario point was closest, of any type (work became a smoke, a rest a seat at a table,
// 5 of 11 found nothing), and someone sat or leaning was never moved. Here, pure (no natives): which kinds of point fit each
// choice (benches, chairs, steps, walls, posts, railings, the bar, a table, a broom), what to play in place when none is
// near, what a sat person's own scenario already is (so "at it already" counts only where the scenario truly is the choice),
// which choices get a sat person up, and the rate limit (one start every few seconds near Arthur, three at once, never two
// within 8 m). The game side is Game/WestworldScript.MindScenarios.cs. Type names from femga/rdr3_discoveries'
// animations/scenarios list (as Roughs.cs has them); the hashes are the game's own joaat of the name, pinned in mindtest
// against the log's values.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MindScenarios
    {
        // ---- the rate limit (constants, shown on F7) ----
        public const int GapMs = 4000, GapJitterMs = 2000;   // one stage-1 start near Arthur every 4-6 s
        public const int AtOnce = 3;                         // getting up or walking to a point at once
        public const float ApartMetres = 8f;                 // never two starting within this of each other (never a street at once)
        public const int FailRestMs = 240000;                // a person the seat kept, or with no point near, is left 4 min
        // ---- the get-up (the waking's exactly, WakeHold.cs "getup": events blocked, one non-immediate clear, the flag polled, 9 s
        // at most; once up, stood still 3 s and the next order 2.5 s on. The review: no re-ask, it starts the getting up over) ----
        public const int UpMostMs = 9000, PollMs = 250, StandMs = 3000, SendAfterUpMs = 2500;
        // ---- the walk and the hold ----
        public const float PointMetres = 15f, PointFromYou = 1.5f;
        public const int ReachMinMs = 12000, ReachPerMetreMs = 1000, HoldMinMs = 60000, HoldSpanMs = 90000;

        // the kinds of point (Kind)
        public const string Seat = "seat", Lean = "lean", Bar = "bar", Table = "table", Work = "work", Standing = "standing", Unknown = "unknown";

        // two in-place names the lists below share (static readonly, before the arrays that read them: initialised in order)
        public static readonly string LookOverInPlace = Wh("CROUCH_INSPECT"), CigarInPlace = Wh("SMOKE_CIGAR");
        // rest: a seat, else a lean (a wall, a post, a railing; a hitching rail is a railing point: the list has no HITCH type)
        public static readonly string[] RestSeats = { "PROP_HUMAN_SEAT_BENCH", "PROP_HUMAN_SEAT_BENCH_PORCH", "GENERIC_SEAT_BENCH_SCENARIO", "PROP_HUMAN_SEAT_CHAIR",
                                                      "PROP_HUMAN_SEAT_CHAIR_PORCH", "GENERIC_SEAT_CHAIR_SCENARIO", "WORLD_HUMAN_SEAT_STEPS" };
        public static readonly string[] RestLeans = { Wh("LEAN_BACK_WALL"), Wh("LEAN_WALL_RIGHT"), Wh("LEAN_WALL_LEFT"), Wh("LEAN_POST_LEFT"),
                                                      Wh("LEAN_POST_RIGHT"), Wh("LEAN_RAILING"), Wh("LEAN_BACK_RAILING") };
        // the rest's two orders, made once (the frame-cost audit, NIGHT4 item 6: a list and an array on every plan before)
        private static readonly string[] RestSeatsFirst = Both(RestSeats, RestLeans), RestLeansFirst = Both(RestLeans, RestSeats);

        private static string[] Both(string[] a, string[] b)
        {
            var t = new string[a.Length + b.Length];
            a.CopyTo(t, 0);
            b.CopyTo(t, a.Length);
            return t;
        }
        // other seats and leans seen in the game's log (counted as a rest where someone already is)
        public static readonly string[] OtherRests = { "WORLD_HUMAN_LEAN_BACK_WALL_NO_PROPS", "WORLD_HUMAN_LEAN_BACK_WALL_SMOKING", "WORLD_HUMAN_LEAN_RAILING_SMOKING",
                                                       "PROP_HUMAN_SEAT_BENCH_TIRED", "PROP_HUMAN_SEAT_CHAIR_READ_NEWSPAPER", "PROP_HUMAN_SEAT_CHAIR_WHITTLE",
                                                       "PROP_HUMAN_SEAT_CHAIR_SMOKING", "WORLD_HUMAN_SMOKE", CigarInPlace, "WORLD_HUMAN_SIT_SMOKE" };
        // drink: the bar rail, or a chair at a table with a glass
        public static readonly string[] DrinkPoints = { "WORLD_HUMAN_BARCUSTOMER", "WORLD_HUMAN_BARCUSTOMER_BEER", "WORLD_HUMAN_BARCUSTOMER_WHISKEY", "SC_WORLD_HUMAN_STAND_BAR",
                                                        "PROP_HUMAN_SEAT_CHAIR_TABLE_DRINKING" };
        public static readonly string[] OtherDrinks = { "WORLD_HUMAN_LEAN_BAR_READ_NEWSPAPER", "WORLD_HUMAN_DRUNK_BAR_SLUMPED", "PROP_HUMAN_SEAT_CHAIR_DRINKING", "WORLD_HUMAN_DRINKING" };
        // eat: a table with a plate
        public static readonly string[] EatPoints = { "PROP_HUMAN_SEAT_CHAIR_TABLE_EATING_KNIFE_FORK", "PROP_CAMP_SEAT_CHAIR_TABLE_STEW" };
        // work in town: a broom, a cloth, a hammer, the tally
        public static readonly string[] WorkPoints = { "WORLD_HUMAN_BROOM_WORKING", "WORLD_HUMAN_STRAW_BROOM_WORKING", "WORLD_HUMAN_PUSH_BROOM_WORKING", "WORLD_HUMAN_CLEAN_TABLE",
                                                       "WORLD_HUMAN_CLEAN_WINDOW", "WORLD_HUMAN_HAMMER_TABLE", "WORLD_HUMAN_HAMMER_WALL", "WORLD_HUMAN_CLIPBOARD",
                                                       "WORLD_HUMAN_BUCKET_POUR_LOW", "PROP_HUMAN_WOOD_CHOP" };
        public static readonly string[] OtherWork = { "WORLD_HUMAN_BARTENDER", "WORLD_HUMAN_BARTENDER_CLEAN_GLASS", "WORLD_HUMAN_SHOPKEEPER", "WORLD_HUMAN_LEAN_TABLE_SHARPEN_KNIFE",
                                                      "SC_WORLD_HUMAN_PUSH_BROOM_WORKING", "WORLD_HUMAN_HAMMER_GROUND", "WORLD_HUMAN_PITCH_HAY_SPREAD" };
        // in place, when no point fits (each seen in the game's log or already played by the Director)
        public const string SmokeInPlace = "WORLD_HUMAN_SMOKE", FanInPlace = "WORLD_HUMAN_FAN", StareInPlace = "WORLD_HUMAN_STARE_STOIC", DrinkInPlace = "WORLD_HUMAN_DRINKING",
                            TallyInPlace = "WORLD_HUMAN_CLIPBOARD";
        // standing scenarios: no seat to get up from (they clear in a moment)
        public static readonly string[] StandingKinds = { "WORLD_HUMAN_SMOKE", CigarInPlace, "WORLD_HUMAN_STARE_STOIC", "WORLD_HUMAN_DRINKING", "WORLD_HUMAN_FAN",
                                                          "WORLD_HUMAN_INSPECT", LookOverInPlace, "WORLD_HUMAN_STAND_WAITING", "WORLD_HUMAN_WAITING_IMPATIENT",
                                                          "WORLD_HUMAN_COFFEE_DRINK", "WORLD_HUMAN_SMOKE_NERVOUS_STRESSED" };

        private static Dictionary<uint, string> _names;

        /// A standing-about type's full name from the part after the game's prefix (a method, so the arrays above may use it).
        private static string Wh(string rest)
        {
            return "WORLD_HUMAN_" + rest;
        }

        /// The game's hash of a name: GET_HASH_KEY over the lower-cased name, the same sum Natives.Hash makes.
        public static uint Joaat(string s)
        {
            unchecked
            {
                uint h = 0;
                foreach (char ch in s.ToLowerInvariant())
                {
                    h += ch;
                    h += h << 10;
                    h ^= h >> 6;
                }
                h += h << 3;
                h ^= h >> 11;
                h += h << 15;
                return h;
            }
        }

        /// Every type name these lists know, by hash; null for one they don't (the log names it by number).
        public static string NameOf(uint hash)
        {
            if (_names == null)
            {
                var d = new Dictionary<uint, string>();
                foreach (string[] list in new[] { RestSeats, RestLeans, OtherRests, DrinkPoints, OtherDrinks, EatPoints, WorkPoints, OtherWork, StandingKinds,
                                                  new[] { "GENERIC_SEAT_CHAIR_TABLE_SCENARIO", "PROP_HUMAN_SEAT_CHAIR_TABLE_FAN_WHORE", "PROP_HUMAN_PIANO" } })
                    foreach (string n in list)
                        d[Joaat(n)] = n;
                _names = d;
            }
            string s;
            return _names.TryGetValue(hash, out s) ? s : null;
        }

        private static bool In(string[] list, string name)
        {
            if (name == null)
                return false;
            for (int i = 0; i < list.Length; i++)
                if (list[i] == name)
                    return true;
            return false;
        }

        /// What kind of scenario a type is: a seat, a lean, the bar, a table, the work, standing (no seat to get up from), unknown.
        public static string Kind(string name)
        {
            if (name == null)
                return Unknown;
            if (In(DrinkPoints, name) || In(OtherDrinks, name))
                return name.IndexOf("_SEAT_", StringComparison.Ordinal) >= 0 ? Table : name == DrinkInPlace ? Standing : Bar;
            if (In(EatPoints, name) || name == "GENERIC_SEAT_CHAIR_TABLE_SCENARIO" || name == "PROP_HUMAN_SEAT_CHAIR_TABLE_FAN_WHORE")
                return Table;
            if (In(WorkPoints, name) || In(OtherWork, name))
                return name == "WORLD_HUMAN_LEAN_TABLE_SHARPEN_KNIFE" ? Lean : Work;
            if (In(StandingKinds, name))
                return Standing;
            if (name.IndexOf("_LEAN_", StringComparison.Ordinal) >= 0)
                return Lean;
            if (name.IndexOf("_SEAT_", StringComparison.Ordinal) >= 0 || name.IndexOf("_SIT_", StringComparison.Ordinal) >= 0)
                return Seat;
            return Unknown;
        }

        /// The choices that get someone sat or leaning up to act on (stage 1). A watch, a wary look, a word with a friend stay
        /// looks from the seat (NIGHT4 fix 2: a head turn plays on seated people); a card game or a prayer has no point to go
        /// to; a feeling's urge has no act. A rest gets them up only from a scenario that isn't one (AtItAlready).
        public static bool GetsUpFor(string choice)
        {
            switch (choice)
            {
                case "rest": case "eat": case "drink": case "work": case "saloon": case "store": case "church": case "home": case "shelter":
                case "talk": case "stroll": case "showoff": case "steerclear":
                    return true;
                default:
                    return false;
            }
        }

        /// The look-only choices a seated person acts on from where they sit (fix 2), when they don't get up.
        public static bool LookFromSeat(string choice)
        {
            return choice == "watch" || choice == "keepclear" || choice == "greet" || choice == "talk";
        }

        /// The choices whose act is a point of the game's found by type (standing people too, with stage 1 on).
        public static bool ByPoint(string choice)
        {
            return choice == "rest" || choice == "eat" || choice == "drink" || choice == "work";
        }

        /// Whether a sat or leaning person's own scenario already is the choice (guard 4: decided once, mindtest pins it): a rest
        /// on any seat, lean or chair at a table, or over a smoke where they stand; a drink at the bar, at a table with a glass, or with one in
        /// hand; a meal at a table with a plate; the work at a broom, a cloth, a hammer, the tally, the bar's own side. A stare,
        /// a fan, a look over something is no rest; an unknown type is nothing (it may get up and go to one that is).
        public static bool AtItAlready(string choice, string name)
        {
            if (name == null)
                return false;
            switch (choice)
            {
                case "rest":
                    {
                        string k = Kind(name);
                        return k == Seat || k == Lean || k == Table || In(OtherRests, name);   // a chair at a table is a rest too
                    }
                case "drink":
                    return In(DrinkPoints, name) || In(OtherDrinks, name);
                case "eat":
                    return In(EatPoints, name);
                case "work":
                    return In(WorkPoints, name) || In(OtherWork, name);
                default:
                    return false;
            }
        }

        /// Guard 4 with the types the lists don't know (null: an unknown hash, or 0 from an in-place sit): a rest anywhere they
        /// sit or lean that the lists don't know is a rest (the review: unknown seats are the commonest, and getting them up to
        /// sit down again elsewhere looks wrong); true or false for a known type; null for an unknown type and any other choice
        /// (the game's older lists decide: the bar, a meal, the barkeep's work).
        public static bool? AtItOrUnknown(string choice, string name)
        {
            if (name != null)
                return AtItAlready(choice, name);
            return choice == "rest" ? true : (bool?)null;
        }

        /// How to act on one choice by the game's points: the types to look for (in order), how far, what to play in place when
        /// none is near (null: nothing, the place or the walk instead), and whether it ends at a place (the saloon's bar).
        public sealed class Plan
        {
            public string Choice;
            public string[] Types;
            public float Radius;
            public string InPlace;     // a scenario in place when no point fits
            public string Place;       // "saloon": to the bar when no point fits (MindToPlace)
        }

        /// The plan for a choice that acts by a point (ByPoint), else null. woman, gentry, saloon: who they are and where.
        public static Plan PlanFor(string choice, bool woman, bool gentry, bool saloon, uint seed)
        {
            switch (choice)
            {
                case "rest":
                    {
                        // a seat or a lean, which first by who they are (half and half), both looked for
                        bool seatFirst = (seed & 1) == 0 || woman;
                        return new Plan { Choice = choice, Types = seatFirst ? RestSeatsFirst : RestLeansFirst, Radius = PointMetres, InPlace = woman ? FanInPlace : (seed & 2) == 0 ? SmokeInPlace : StareInPlace };
                    }
                case "drink":
                    return new Plan { Choice = choice, Types = DrinkPoints, Radius = PointMetres, Place = "saloon", InPlace = saloon ? DrinkInPlace : null };
                case "eat":
                    return new Plan { Choice = choice, Types = EatPoints, Radius = PointMetres, Place = "saloon" };
                case "work":
                    return new Plan { Choice = choice, Types = WorkPoints, Radius = PointMetres, InPlace = gentry ? TallyInPlace : LookOverInPlace };
                default:
                    return null;
            }
        }

        /// What they're seen to do at a point of this type, in the log's and F7's words.
        public static string Words(string name)
        {
            if (name == null)
                return "takes a spot";
            if (name.Contains("BENCH")) return "sits down on the bench";
            if (name.Contains("STEPS")) return "sits down on the steps";
            if (name.Contains("TABLE_EATING") || name.Contains("TABLE_STEW")) return "sits down to eat";
            if (name.Contains("TABLE_DRINKING") || name.Contains("CHAIR_DRINKING")) return "sits down with a drink";
            if (name.Contains("BARCUSTOMER") || name.Contains("STAND_BAR")) return "goes up to the bar";
            if (name.Contains("CHAIR")) return "takes a chair";
            if (name.Contains("LEAN_POST")) return "leans on a post";
            if (name.Contains("RAILING")) return "leans on the rail";
            if (name.Contains("LEAN")) return "leans on the wall";
            if (name.Contains("BROOM")) return "gets on with the sweeping";
            if (name.Contains("CLEAN_TABLE")) return "wipes down a table";
            if (name.Contains("CLEAN_WINDOW")) return "cleans a window";
            if (name.Contains("HAMMER")) return "takes a hammer to something that needs it";
            if (name.Contains("CLIPBOARD")) return "goes over the tally";
            if (name.Contains("BUCKET")) return "sees to a bucket";
            if (name.Contains("WOOD_CHOP")) return "chops some wood";
            if (name.Contains("CROUCH_INSPECT")) return "crouches to look something over";
            if (name.Contains("SMOKE")) return "has a smoke where they stand";
            if (name.Contains("FAN")) return "stands and fans herself";
            if (name.Contains("STARE")) return "stands and takes a breath";
            if (name == DrinkInPlace) return "has a drink where they stand";
            return "takes a spot";
        }

        /// The kind of point a type is, for F7's counts: seat, lean, bar (the bar and a table's drink), work, eat (a table), in place.
        public static string CountKind(string name, bool inPlace)
        {
            if (inPlace)
                return "in place";
            string k = Kind(name);
            if (k == Seat || k == Lean)
                return k;
            if (k == Bar || In(DrinkPoints, name))
                return "bar";
            if (k == Table)
                return "table";
            return k == Work ? "work" : "other";
        }

        // ---- the rate limit (guard 2) ----
        public const string RefuseGap = "the stage-1 gap", RefuseAtOnce = "3 getting up at once", RefuseNear = "another getting up within 8 m", RefuseRested = "rested after a get-up failed";

        /// Why a stage-1 start mayn't begin now, else null: the gap since the last start, three already getting up or walking,
        /// one of them starting within 8 m (never a street at once), this person rested after the seat kept them.
        public static string Refuse(int now, int nextAt, int active, float nearestActiveMetres, int restedUntil)
        {
            if (restedUntil != 0 && now < restedUntil && restedUntil - now <= FailRestMs)
                return RefuseRested;
            if (active >= AtOnce)
                return RefuseAtOnce;
            if (nearestActiveMetres < ApartMetres)
                return RefuseNear;
            if (now < nextAt && nextAt - now <= GapMs + GapJitterMs)
                return RefuseGap;
            return null;
        }

        /// When the next start may come after one now: 4 s and a jitter of up to 2 s by the seed.
        public static int NextAt(int now, uint seed)
        {
            return now + GapMs + (int)(seed % (uint)(GapJitterMs + 1));
        }

        /// How long a walk to a point this far off is given before "never reached it".
        public static int ReachMs(float metres)
        {
            return ReachMinMs + (int)(Math.Max(0f, Math.Min(40f, metres)) * ReachPerMetreMs);
        }

        /// How long they keep a point once there before the act lets go (they keep it after, as the game's own ambient life).
        public static int HoldMs(uint seed)
        {
            return HoldMinMs + (int)(seed % (uint)(HoldSpanMs + 1));
        }

        /// The getting-up step's decision, from the scenario flag and the time since the one ask: "up" (stood still, then sent
        /// on), "kept" (9 s: the seat kept them, give up, never pulled, never asked again), or null (wait). As the waking's.
        public static string UpStep(bool inScenario, int sinceAskMs)
        {
            if (!inScenario)
                return "up";
            if (sinceAskMs >= UpMostMs)
                return "kept";
            return null;
        }
    }
}
