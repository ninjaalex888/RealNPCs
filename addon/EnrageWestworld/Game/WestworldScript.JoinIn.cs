// Onlookers who join a fistfight going on near the player, on a side, from their minds (Core/JoinIn.cs): a friend of
// someone getting beaten, someone with bad blood against a fighter, a drunk hothead in a saloon. A single joiner each look, every few
// seconds, so a fight spreads the way it would: the friend of the man just set upon may be next (the NPC-life run, Lex
// 2026-10-30: "it can break out between any 2+ NPCs if their inner thoughts lead to it"). Behind [Mind] MindLive and
// MindJoinFights.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int JoinEveryMs = 2500;
        // [Mind] BrawlJoinMetres (30): 12 until 2026-10-31, then 18 fixed (Lex, in busy Saint Denis: more of the street should join; later
        // that day "brawls reach further": a lawman 41 m off arrived to find it over)
        private float JoinMetres { get { return _cfg.BrawlJoinMetres; } }
        private const float JoinYouMetres = 18f;   // the brawl that comes at you (WestworldScript.JoinYou.cs) keeps its 18 m: those beside you, not the street
        private const float JoinWalkFrom = 10f, JoinArriveMetres = 6f;   // a joiner farther than this runs over first, and is in it once this close
        private const int JoinMeetMost = 6;     // strangers given a resident's identity on the spot in one look, the nearest first (the cost)
        private int _joinNextAt, _joinTotal, _joinMet;
        private bool _joinFailed;
        private string _joinLast;

        // [Mind] MindBrawls (Core/Brawl.cs): the cap, the waves, and the sat and the leaning who get up first
        private bool BrawlsOn { get { return _cfg.MindLive && _cfg.MindJoinFights && _cfg.MindBrawls; } }
        private int MostJoinersNow { get { return BrawlsOn ? Brawl.MostJoiners(_cfg.BrawlMost) : JoinIn.MostJoiners; } }
        private sealed class JoinPending { public int Handle, Side, Target, At, Until, Since; public bool Walk, Pulled; public string Name, Why, With; }   // Walk: running over from afar (events held till he is in it)
        private readonly List<JoinPending> _joinPending = new List<JoinPending>();
        private readonly Dictionary<int, int> _joinSideOf = new Dictionary<int, int>();   // this fight's picks: whose side each took, kept for the fight
        private int _brawlBiggest, _joinPendingAt, _joinSidesFor, _joinCameOver, _joinGaveUp;

        private int _joinFightSince, _joinLawSaidFor, _joinWhoSaidFor;

        // ---- the big brawl's shape (FOURTEENHOUR item 10; [Mind] BrawlShape) ----
        private int _shapeFor, _shapeBigAt, _shapeSlipped, _shapeQuit, _shapeScattered, _shapeDrewAt;
        private bool _shapeKeeperSaid, _shapeScatterDone;

        /// At a look: once a fight is big (six or more), the barkeep's shout (words and a look: he keeps his post) and the timid
        /// and the women near slip out (three at most); a gun drawn in it (a holdout of BrawlDraw's) scatters the room; and from
        /// ten seconds before the brawl's least time, the beaten-looking quit at each look, so it thins and the last finish it.
        private void ShapeLook(List<int> a, List<int> b, List<Ped> looking, Vector3 mid, int now)
        {
            if (_shapeFor != _director.FightId)
            {
                _shapeFor = _director.FightId;
                _shapeBigAt = 0;
                _shapeKeeperSaid = _shapeScatterDone = false;
                _shapeDrewAt = _brawlDrew;
            }
            int fighting = a.Count + b.Count;
            string seed = "f" + _director.FightId + "|shape";
            if (fighting >= Brawl.BigAt && _shapeBigAt == 0)
            {
                _shapeBigAt = now;
                Log.Write("BRAWL big: " + fighting + " at it" + (_placeKind == "saloon" ? " in the saloon" : " in the street"));
                if (_placeKind == "saloon" && !_shapeKeeperSaid)
                {
                    _shapeKeeperSaid = true;
                    Ped keep = Peds.All().FirstOrDefault(p => p != null && p.Exists() && !ENTITY.IS_ENTITY_DEAD(p.Handle) && PostKindOf(p) != null && PostKindOf(p).Role == "the barkeep" &&
                                                               p.Position.DistanceTo(mid) < 25f && !_director.Involves(p.Handle));
                    if (keep != null && !Natives.StoryModel(keep))
                    {
                        TASK.TASK_LOOK_AT_ENTITY(keep.Handle, a[0], 8000, 0, 51, 0);   // a look, no order: he keeps his post
                        if (_cfg.ScenesOnTicker && _cfg.Ticker && keep.Position.DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                            SpeechCaption("The barkeep", Brawl.KeeperShout(seed), keep.Handle, 2500);
                        Log.Write("BRAWL big: the barkeep shouts from behind his bar");
                    }
                }
                int slipped = 0;
                for (int i = looking.Count - 1; i >= 0 && slipped < 3; i--)
                {
                    Ped p = looking[i];
                    if (p.Position.DistanceTo(mid) > 20f)
                        continue;
                    MindWho w = MindKnown(p.Handle, now);
                    Traits t = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
                    if (!Brawl.SlipsOut(t, !PED.IS_PED_MALE(p.Handle), seed + "|" + p.Handle))
                        continue;
                    looking.RemoveAt(i);
                    TASK.TASK_SMART_FLEE_PED(p.Handle, a[0], 25f, 9000, 0, 1f, 0);   // out of it, at a walk (the brave's back-off, farther)
                    slipped++;
                    _shapeSlipped++;
                }
                if (slipped > 0)
                    Log.Write("BRAWL big: " + slipped + " slip out of it");
            }
            // a gun drawn in a big brawl: the room scatters (the onlookers near; the fighters are BrawlDraw's own)
            if (!_shapeScatterDone && fighting >= Brawl.BigAt - 2 && _brawlDrew > _shapeDrewAt)   // a gun drawn in this fight (BrawlDraw counts them)
            {
                _shapeScatterDone = true;
                int n = 0;
                for (int i = looking.Count - 1; i >= 0 && n < 6; i--)
                {
                    Ped p = looking[i];
                    if (p.Position.DistanceTo(mid) > 25f)
                        continue;
                    looking.RemoveAt(i);
                    TASK.TASK_SMART_FLEE_PED(p.Handle, a[0], 40f, -1, 0, 3f, 0);   // at a run
                    n++;
                }
                _shapeScattered += n;
                Log.Write("BRAWL big: a gun out in it, and " + n + " near scatter");
            }
            // burning out: from ten seconds before the least time, the beaten-looking quit (35% a look), never a side's last man
            if (_shapeBigAt != 0 && _director.FightOnFor(now) >= _cfg.BrawlLeastSeconds * 1000 - Brawl.BurnOutLeadMs)
            {
                foreach (List<int> side in new[] { a, b })
                {
                    int left = side.Count;
                    foreach (int h in side.ToList())
                    {
                        if (left <= 1)
                            break;
                        if (h == a[0] || (b.Count > 0 && h == b[0]) || !ENTITY.DOES_ENTITY_EXIST(h) || BrawlHealth(h) >= Brawl.BurnOutHealth)
                            continue;
                        if (!Voice.Chance(seed + "|quit|" + h + "|" + now / 2500, Brawl.BurnOutPercent) || !_director.LeaveFight(h))
                            continue;
                        left--;
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        WanderLater(h, now + 2000);
                        _shapeQuit++;
                        Log.Detail("BRAWL burning out: " + NameOfFighter(h) + " has had enough and drops out of it");
                    }
                }
            }
        }

        // ---- the stray blow and the friend who pulls him out (FOURTEENHOUR item 8; [Mind] BrawlStrays) ----
        private readonly Dictionary<int, int> _strayNear = new Dictionary<int, int>();   // within arm's length of the fighting since (this fight)
        private readonly HashSet<int> _strayRolled = new HashSet<int>(), _pulledOut = new HashSet<int>();
        private int _strayFor, _strays, _strayJoins, _pullOuts;

        /// At a look: someone free within 1.5 m of one of the fighters at two looks running takes a stray blow, once a fight
        /// (Brawl.StrayPercent); the hot swing back and are in it against the man who hit them, the timid back off, a woman
        /// cries out and leaves.
        private void StraysLook(List<int> a, List<int> b, List<Ped> looking, int now)
        {
            if (_strayFor != _director.FightId)
            {
                _strayFor = _director.FightId;
                _strayNear.Clear();
                _strayRolled.Clear();
                _pulledOut.Clear();
            }
            for (int i = looking.Count - 1; i >= 0; i--)
            {
                Ped p = looking[i];
                int h = p.Handle;
                if (_strayRolled.Contains(h))
                    continue;
                Vector3 at = p.Position;
                int hitter = 0;
                foreach (int f in a.Concat(b))
                    if (ENTITY.DOES_ENTITY_EXIST(f) && ENTITY.GET_ENTITY_COORDS(f, true, true).DistanceTo(at) < 1.5f)
                    {
                        hitter = f;
                        break;
                    }
                if (hitter == 0)
                {
                    _strayNear.Remove(h);
                    continue;
                }
                int since;
                if (!_strayNear.TryGetValue(h, out since))
                {
                    _strayNear[h] = now;
                    continue;
                }
                _strayRolled.Add(h);
                string seed = "f" + _director.FightId + "|stray|" + h;
                if (!Voice.Chance(seed, Brawl.StrayPercent))
                    continue;
                _strays++;
                MindWho w = MindKnown(h, now);
                Traits t = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
                bool woman = !PED.IS_PED_MALE(h);
                string answer = Brawl.StrayAnswer(t, w != null && InHisCups(p, w.P), woman, _cfg.WomenInFights, seed);
                string name = w != null && w.Name != null ? w.Name : "a stranger", by = NameOfFighter(hitter);
                int room = MostJoinersNow - _director.JoinerCount - _joinPending.Count;
                if (answer == "swing" && room > 0 && w != null && w.Id != null)
                {
                    int side = a.Contains(hitter) ? 1 : 0;   // against the man who hit him
                    looking.RemoveAt(i);
                    _strayJoins++;
                    JoinAdd(h, side, hitter, name, "a stray blow from " + by + ", and swings back", NameOfFighter(side == 0 ? a[0] : b[0]), by);
                    continue;
                }
                looking.RemoveAt(i);
                TASK.TASK_SMART_FLEE_PED(h, hitter, answer == "cry" ? 20f : 8f, answer == "cry" ? 8000 : 4000, 0, 1f, 0);   // off out of reach (as the Director's brave back off)
                Log.Write("BRAWL " + name + " takes a stray blow from " + by + (answer == "cry" ? " and cries out and goes" : " and backs off out of it"));
            }
        }

        /// At a look: a fighter losing badly (his health under a third) with a friend near and free (warmth 2 or more) may be
        /// pulled out of it; the two leave together, side by side, away from the fight, and the one beating him lets them go
        /// (or a hothead follows). Once a man a fight.
        private void PullOutLook(List<int> a, List<int> b, List<Ped> looking, int now)
        {
            foreach (int f in a.Concat(b).ToList())
            {
                if (_pulledOut.Contains(f) || !ENTITY.DOES_ENTITY_EXIST(f) || ENTITY.IS_ENTITY_DEAD(f) || BrawlHealth(f) >= Brawl.PullOutHealth)
                    continue;
                MindWho fw = MindKnown(f, now);
                if (fw == null || fw.Id == null)
                    continue;
                Vector3 fat = ENTITY.GET_ENTITY_COORDS(f, true, true);
                foreach (Ped p in looking)
                {
                    MindWho w = MindKnown(p.Handle, now);
                    if (w == null || w.Id == null || p.Position.DistanceTo(fat) > 15f)
                        continue;
                    int warmth = LadderWarmth(w.Id, fw.Id);
                    if (warmth < 2)
                        continue;
                    _pulledOut.Add(f);
                    Traits t = w.Traits ?? (w.Traits = w.Src != null ? _engine.Shadow.TraitsOf(w.Src) : null);
                    if (!Brawl.PullsOut(t, warmth, "f" + _director.FightId + "|" + w.Id + "|" + fw.Id) || !_director.LeaveFight(f))
                        break;
                    int beater = _director.TargetOf(f);
                    MindWho bw = beater != 0 && ENTITY.DOES_ENTITY_EXIST(beater) ? MindKnown(beater, now) : null;
                    Traits bt = bw != null && bw.Src != null ? (bw.Traits ?? (bw.Traits = _engine.Shadow.TraitsOf(bw.Src))) : null;
                    bool follows = bw != null && Brawl.FollowsThem(bt, InHisCups(new Ped(beater), bw.P));
                    // away from the fight, 20 m, the two a metre apart (the walk the Director's leavewith uses, seen working)
                    Vector3 away = fat - ENTITY.GET_ENTITY_COORDS(beater != 0 && ENTITY.DOES_ENTITY_EXIST(beater) ? beater : a[0], true, true);
                    away.Z = 0f;
                    away = Math.Abs(away.X) + Math.Abs(away.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : away.Normalized;
                    Vector3 spot = fat + away * 20f, side = new Vector3(-away.Y, away.X, 0f) * 1.1f;
                    TASK.CLEAR_PED_TASKS(f, true, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(f, false);
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(f, spot.X + side.X, spot.Y + side.Y, spot.Z, 1f, -1, 1f, 0, 0f);
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, spot.X - side.X, spot.Y - side.Y, spot.Z, 1f, -1, 1f, 0, 0f);
                    WanderLater(f, now + 15000);
                    WanderLater(p.Handle, now + 15000);
                    if (!follows && beater != 0 && ENTITY.DOES_ENTITY_EXIST(beater))
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(beater, f, 2000, 0f, 0f, 0f);   // he lets them go, and watches them off
                    else if (follows)
                        TASK.TASK_COMBAT_PED(beater, f, 0, 16);
                    _pullOuts++;
                    Log.Write("BRAWL " + w.Name + " pulls " + fw.Name + " out of it, beaten; they leave together" + (beater != 0 ? ", and " + NameOfFighter(beater) + (follows ? " goes after them" : " lets them go") : ""));
                    break;
                }
            }
        }
        private readonly Dictionary<string, SidePerson> _brawlSides = new Dictionary<string, SidePerson>();   // the people of this fight, by id (item 6)
        private int _brawlSidesFor;
        private Func<string, string, string, SideTie> _sideTieOf;

        /// Why someone near a fight isn't weighed at all (null: they are). The sat and the leaning stay put until three are at
        /// it (pulled off a bar they went into a T pose), unless they have a stake in it ([Mind] BrawlSides: a friend, a foe or
        /// their own kind fighting; FOURTEENHOUR item 6): then they get up for a fight of two.
        private string JoinPassOver(Ped p, int h, List<int> a, List<int> b, int now)
        {
            if (Spawner.IsOurs(h))
                return "ours";
            if (Natives.StoryModel(p) || !IsGrownUp(p))
                return "the story's or a child";
            if (PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false))
                return "riding";
            if (_wakeActs.Any(w => w.Handle == h))
                return "a wake act";
            if (PED.IS_PED_USING_ANY_SCENARIO(h) && !(BrawlsOn && (a.Count + b.Count >= 3 || (_cfg.BrawlSides && SeatedStake(h, a, b, now)))))
                return "sat or leaning";
            if (PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h))
                return "in a fight or fleeing";
            string why = MayDirectWhyNot(p);
            if (why != null)
                return "can't be directed (" + why + ")";
            if (JoinIsPending(h) || AimedLately(h, now) || IsStaged(h) || MindGettingUp(h) || WalkingUp(h) || InTalk(h))   // (an encounter's, a get-up, a walk-up, a talk: NIGHT4 item 6; not all of NotFree, whose onlookers are the joiners)
                return "busy with something of ours";
            return null;
        }

        /// Someone sat or leaning with a stake in this fight: a tie to one of the fighters, or their own kind in it.
        private bool SeatedStake(int h, List<int> a, List<int> b, int now)
        {
            MindWho w = MindKnown(h, now);
            if (w == null || w.Id == null)
                return false;
            SidePerson me = SideNote(w);
            int lead0 = a.Count > 0 ? a[0] : 0, lead1 = b.Count > 0 ? b[0] : 0;
            foreach (int fh in a.Concat(b))
            {
                MindWho fw = ENTITY.DOES_ENTITY_EXIST(fh) ? MindKnown(fh, now) : null;
                if (fw == null || fw.Id == null)
                    continue;
                if (LadderWarmth(w.Id, fw.Id) != 0)
                    return true;
                int other = a.Contains(fh) ? lead1 : lead0;
                MindWho ow = other != 0 && ENTITY.DOES_ENTITY_EXIST(other) ? MindKnown(other, now) : null;
                if (BrawlSides.Tie(me, SideNote(fw), ow != null && ow.Id != null ? SideNote(ow) : null, _placeTown) != null)
                    return true;
            }
            return false;
        }

        /// The person as the fight's sides see them, noted for this fight (kept by id until the next fight).
        private SidePerson SideNote(MindWho w)
        {
            if (_brawlSidesFor != _director.FightId)
            {
                _brawlSidesFor = _director.FightId;
                _brawlSides.Clear();
            }
            SidePerson s;
            if (_brawlSides.TryGetValue(w.Id, out s))
                return s;
            Person p = w.P;
            s = new SidePerson
            {
                Id = w.Id, KinId = p != null ? p.KinId : null, Group = TradeGroupOf(w), Home = p != null ? p.Home : null,
                Surname = w.Name != null ? Residents.SurnameOf(w.Name) : null,
                Outsider = p == null || p.Home == null || (_placeTown != null && p.Home != _placeTown),
                Pious = w.Src != null && (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))).Z[Traits.Piety] > 1f
            };
            _brawlSides[w.Id] = s;
            return s;
        }

        private static string TradeGroupOf(MindWho w)
        {
            return w.TradeGroup ?? (w.TradeGroup = InnerLife.TradeGroup(w.P != null ? w.P.Trade : null));
        }

        private SideTie SideTieOf(string on, string fighter, string other)
        {
            SidePerson o, f, x = null;
            if (on == null || fighter == null || !_brawlSides.TryGetValue(on, out o) || !_brawlSides.TryGetValue(fighter, out f))
                return null;
            if (other != null)
                _brawlSides.TryGetValue(other, out x);
            SideTie own = BrawlSides.Tie(o, f, x, _placeTown);
            Scene sc = _cfg.QuarrelPolitics ? _director.CurrentScene : null;
            SideTie quarrel = sc != null ? BrawlSides.QuarrelTie(sc.Over, o, f, x) : null;   // a fight over silver: his side of it (item 9)
            return own == null ? quarrel : quarrel == null || own.Pull >= quarrel.Pull ? own : quarrel;
        }
        private const int BrawlOpeningMs = 10000;

        private void JoinStep(int now)
        {
            int least = _cfg.BrawlLeastSeconds * 1000;
            _director.BrawlLeastMs = least;   // [Mind] BrawlLeastSeconds: a brawl lasts at least this long from its first blows
            _director.FightMostMs = BrawlsOn ? Math.Max(45000, least + 15000) : 20000;
            _director.BraveryNow = _cfg.Bravery;   // the scene's own frights hold their ground by [World] Bravery
            _director.DownsOn = BrawlsOn;   // a man beaten down in a brawl: killed or knocked out, and it goes on (WestworldScript.Brawl.cs)
            _director.HoldFightStart = _cfg.FightHoldStart;   // [Mind] FightHoldStart: the two keep the order until the first blow (FOURTEENHOUR item 1a)
            _director.MeetAgain = _cfg.MeetWalkAgain;         // [Scenes] MeetWalkAgain: a walker who stops short is told again (item 1b)
            if (_director.FighterDown == null)
                _director.FighterDown = OnFighterDown;
            if (_director.GoneWhose == null)
                _director.GoneWhose = GoneWhose;   // a cast member's body gone: whose it was, for the line (item 1d)
            List<int> a, b;
            if (_joinPending.Count > 0 && now - _joinPendingAt >= 0)
            {
                // those on their way, every half second: in once they're close (or up off the bar), and counted for their
                // side meanwhile, so the fight doesn't end before they get there
                _joinPendingAt = now + 500;
                if (!_director.FightSides(out a, out b))
                    JoinPendingDrop("the fight is over");
                else
                    JoinPendingStep(now);
            }
            if (!_cfg.MindLive || !_cfg.MindJoinFights || _joinFailed || now < _joinNextAt)
                return;
            _joinNextAt = now + JoinEveryMs;
            if (!_director.FightSides(out a, out b))
            {
                JoinPendingDrop("the fight is over");
                return;
            }
            if (_joinSidesFor != _director.FightId)
            {
                _joinSidesFor = _director.FightId;
                _joinSideOf.Clear();   // a new fight: nobody's side is taken yet
                _joinFightSince = now;
            }
            // the first seconds of a fight draw people in more readily, so two at it can grow before one goes down (Lex,
            // 2026-11-01: someone may go down early, but bystanders more likely to join) ([Mind] BrawlOpeningBoost)
            JoinIn.OpeningBoost = now - _joinFightSince < BrawlOpeningMs && now >= _joinFightSince ? _cfg.BrawlOpeningBoost / 100f : 0f;
            if (a.Count + b.Count > _brawlBiggest)
                _brawlBiggest = a.Count + b.Count;
            if (!_director.FightSides(out a, out b))
                return;   // the sides again: those who got up to join are on them now, so the balance sees them (the review, 2026-10-31)
            if (_director.JoinerCount + _joinPending.Count >= MostJoinersNow)
                return;
            try
            {
                JoinLook(a, b, now);
            }
            catch (Exception ex)
            {
                _joinFailed = true;
                Log.Write("BRAWL joining failed once, off for this session: " + ex.Message);
            }
        }

        private void JoinLook(List<int> a, List<int> b, int now)
        {
            Ped me = Game.Player.Character;
            if (!ENTITY.DOES_ENTITY_EXIST(a[0]))
                return;
            Vector3 mid = ENTITY.GET_ENTITY_COORDS(a[0], true, true);
            var sides = new[] { a.Select(FighterOf).ToList(), b.Select(FighterOf).ToList() };
            var looking = new List<Ped>();
            int witnesses = 0;
            // why those near were passed over, once a fight (FOURTEENHOUR item 6: of 21 near Lex's one long fight, three were weighed)
            Dictionary<string, int> passedOver = _joinWhoSaidFor != _director.FightId ? new Dictionary<string, int>() : null;
            bool law = false;
            float lawD = 99f;
            int lawH = 0;   // the nearest lawman within 30 m, for the hold by sight and distance (FOURTEENHOUR item 5)
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Position.DistanceTo(mid) > JoinMetres + 12f)
                    continue;   // the distance first
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(h))
                    continue;
                if (Natives.IsLaw(p))
                {
                    float ld = p.Position.DistanceTo(mid);
                    if (ld < 30f)
                    {
                        law = true;
                        if (ld < lawD)
                        {
                            lawD = ld;
                            lawH = h;
                        }
                    }
                    continue;
                }
                if (p.Position.DistanceTo(mid) > JoinMetres || _director.Involves(h))
                    continue;
                witnesses++;
                if (_director.IsOut(h) || _brawlOut.ContainsKey(h) || _brawlFatal.Contains(h))
                    continue;   // knocked out in it, or killed: not getting up to join
                string passed = JoinPassOver(p, h, a, b, now);
                if (passed != null)
                {
                    if (passedOver != null)
                    {
                        int pn;
                        passedOver.TryGetValue(passed, out pn);
                        passedOver[passed] = pn + 1;
                    }
                    continue;
                }
                looking.Add(p);
            }
            if (passedOver != null)
            {
                _joinWhoSaidFor = _director.FightId;
                Log.Write("BRAWL who could join: " + witnesses + " near, " + looking.Count + " to weigh" + (passedOver.Count > 0 ? "; passed over: " + string.Join(", ", passedOver.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value)) : ""));
            }
            if (BrawlAfterOn)
                AfterNote(a, b);   // who was on which side, and the most it grew to (FOURTEENHOUR item 12)
            if (BrawlsOn && _cfg.BrawlShape)
                ShapeLook(a, b, looking, mid, now);   // the big brawl's beats (FOURTEENHOUR item 10)
            if (BrawlsOn && _cfg.BrawlStrays)
            {
                StraysLook(a, b, looking, now);   // the stray blow (FOURTEENHOUR item 8)
                PullOutLook(a, b, looking, now);  // a friend pulls a beaten man out of it
            }
            if (looking.Count == 0)
                return;
            string fight = (_director.FightId != 0 ? "f" + _director.FightId : a[0] + "|" + b[0]) + "|" + (a.Count + b.Count);
            bool saloon = _placeKind == "saloon", sure = BrawlsOn && now < _brawlTestUntil;   // the Testing row: the waves without the bar or the roll
            int most = MostJoinersNow, room = most - _director.JoinerCount - _joinPending.Count;
            // [Mind] BrawlLawSight (FOURTEENHOUR item 5): the badge holds by sight and distance, not by being within 30 m (one
            // ray from him to the fight, a look every 2.5 s)
            float lawHold = -1f;
            if (law && BrawlsOn && _cfg.BrawlLawSight && lawH != 0)
            {
                bool sees = ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(lawH, a[0], 17);
                lawHold = Brawl.LawHoldsSeen(saloon, a.Count + b.Count, lawD, sees);
                if (_joinLawSaidFor != _director.FightId)
                {
                    _joinLawSaidFor = _director.FightId;
                    Log.Detail("BRAWL the law: a lawman " + (int)lawD + " m off, " + (sees ? "in sight" : "out of sight") + ": joiners held to " + (int)Math.Round(lawHold * 100) + "% of their chance");
                }
            }
            int wave = BrawlsOn ? Brawl.Wave(a.Count + b.Count, saloon, room, witnesses) : Math.Min(1, room);
            // [Mind] BrawlSides (FOURTEENHOUR item 6): who each is to the fighters, by kin, trade and town (the fighters noted first)
            Func<string, string, string, SideTie> sideTie = null;
            if (BrawlsOn && _cfg.BrawlSides)
            {
                foreach (int fh in a.Concat(b))
                {
                    MindWho fw = ENTITY.DOES_ENTITY_EXIST(fh) ? MindKnown(fh, now) : null;
                    if (fw != null && fw.Id != null)
                        SideNote(fw);
                }
                sideTie = _sideTieOf ?? (_sideTieOf = SideTieOf);
            }
            var takes = new List<KeyValuePair<JoinTake, KeyValuePair<Ped, MindWho>>>();
            int met = 0;
            if (BrawlsOn && !OwnLooksOn)
                looking = looking.OrderBy(p => p.Position.DistanceTo(mid)).ToList();   // the nearest strangers are the ones given a name
            foreach (Ped p in looking)
            {
                MindWho w = MindKnown(p.Handle, now);
                // with brawls on, anyone near may join (Lex, 2026-10-31: in Saint Denis only people the mind had met could):
                // a stranger is given a resident's identity on the spot, as the street scenes do, a few a look; never with
                // OwnLooks on (its reading of clothes is the kind that froze the game)
                if (BrawlsOn && !OwnLooksOn && (w == null || w.Id == null) && met < JoinMeetMost && ResidentEligible(p))
                {
                    met++;
                    if (ResidentOf(p) != null)
                    {
                        _mindWho.Remove(p.Handle);   // the cache had them as nobody a moment ago
                        w = MindKnown(p.Handle, now);
                        _joinMet++;
                    }
                }
                if (w == null || w.Id == null || w.Src == null)
                    continue;   // the mind decides who joins: nobody it doesn't know
                if (sideTie != null && BrawlSides.SendsForTheLaw(TradeGroupOf(w)))
                {
                    Log.Detail("BRAWL " + w.Name + " stays out: the well-off send for the law");
                    continue;
                }
                if (sideTie != null)
                    SideNote(w);
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                JoinTake take = JoinIn.Weigh(w.Id, t, !PED.IS_PED_MALE(p.Handle), InHisCups(p, w.P), sides, LadderWarmth, witnesses, law, saloon,
                                             _cfg.WomenInFights, _director.JoinerCount + _joinPending.Count, fight + "|" + w.Id, most, BrawlsOn, sure, lawHold, sideTie, BrawlsOn && _cfg.BrawlCascade);
                if (take.Side >= 0)
                    takes.Add(new KeyValuePair<JoinTake, KeyValuePair<Ped, MindWho>>(take, new KeyValuePair<Ped, MindWho>(p, w)));
                else if (take.Why != null && take.Score > 0.2f)
                    Log.Detail("BRAWL " + w.Name + " stays out: " + take.Why);
            }
            int placed = 0, joinedNow = _director.JoinerCount + _joinPending.Count;
            foreach (var kv in takes.OrderByDescending(x => x.Key.Score))
            {
                if (placed >= wave)
                    break;
                JoinTake best = kv.Key;
                Ped bestPed = kv.Value.Key;
                MindWho bw = kv.Value.Value;
                string bestName = bw.Name;
                if (BrawlsOn && placed > 0)
                {
                    // weighed again against the sides as this wave has left them, so a wave doesn't all pile onto one man
                    best = JoinIn.Weigh(bw.Id, bw.Traits, !PED.IS_PED_MALE(bestPed.Handle), InHisCups(bestPed, bw.P), sides, LadderWarmth, witnesses, law, saloon,
                                        _cfg.WomenInFights, joinedNow, fight + "|" + bw.Id, most, BrawlsOn, sure, lawHold, sideTie, BrawlsOn && _cfg.BrawlCascade);
                    if (best.Side < 0)
                        continue;
                }
                int took;
                if (_joinSideOf.TryGetValue(bestPed.Handle, out took) && took != best.Side)
                {
                    Log.Detail("BRAWL " + bestName + " stays with the side he first took");   // picked before for the other side: nobody switches sides mid-fight
                    continue;
                }
                placed++;
                joinedNow++;
                _joinSideOf[bestPed.Handle] = best.Side;
                List<int> theirs = best.Side == 0 ? b : a;
                if (BrawlsOn)
                {
                    JoinIn.Join(sides, best, new Fighter { Id = bw.Id, Woman = !PED.IS_PED_MALE(bestPed.Handle) });
                    (best.Side == 0 ? a : b).Add(bestPed.Handle);   // the handles kept beside the sides, index for index
                }
                int target = theirs[Math.Max(0, Math.Min(theirs.Count - 1, best.Target))];
                string with = NameOfFighter(best.Side == 0 ? a[0] : b[0]), at = NameOfFighter(target);
                string why = best.Why + "; " + witnesses + " watching" + (saloon ? ", a saloon" : "") + (sure ? ", a test" : "") + "; " + (a.Count + b.Count) + " fighting";
                bool sat = PED.IS_PED_USING_ANY_SCENARIO(bestPed.Handle);
                float off = bestPed.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(target, true, true));
                if (sat || off > JoinWalkFrom)
                {
                    // up off the bar the game's way first, or a run over from afar: counted for his side on the way (the
                    // Director's Coming), in once he's close (Lex, 2026-10-31: nine got up to join, and it was over first)
                    var j = new JoinPending { Handle = bestPed.Handle, Side = best.Side, Target = target, Since = now, At = now + (sat ? 1500 : 0), Until = now + JoinWaitMs(off, sat),
                                              Name = bestName, With = with, Why = with + "'s side and goes for " + at + " (" + why + ")" };
                    _joinPending.Add(j);
                    if (sat)
                        ScenarioExit.SoftUp(bestPed.Handle, x => TASK.TASK_STAND_STILL(x, 6000));   // the exit flag and the next task the same tick, no bare clear (Game/ScenarioExit.cs)
                    else
                        JoinRunOver(j);
                    Log.Write("BRAWL " + bestName + (sat ? " gets up to join in" : " runs over to join in, " + off.ToString("0") + " m off") + ", for " + with);
                    continue;
                }
                JoinAdd(bestPed.Handle, best.Side, target, bestName, with + "'s side and goes for " + at + " (" + why + ")", with, at);
            }
            JoinComingCount();
        }

        /// How long one on his way may take: about 1.6 s a metre and 4 s more, 30 s at most (10 s at least for one getting up).
        private static int JoinWaitMs(float metres, bool sat)
        {
            return Math.Max(sat ? 10000 : 6000, Math.Min(30000, 4000 + (int)(metres * 1600f)));
        }

        /// He runs to the man he's going for; the fight's own events don't send him off on the way.
        private static void JoinRunOver(JoinPending j)
        {
            j.Walk = true;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(j.Handle, true);
            TASK.TASK_GO_TO_ENTITY(j.Handle, j.Target, 30000, 3f, JoinArriveMetres - 2f, 0f, 0);
        }

        /// The fight's sides learn who is on the way to each.
        private void JoinComingCount()
        {
            int c0 = 0, c1 = 0;
            foreach (JoinPending j in _joinPending)
                if (j.Side == 0)
                    c0++;
                else
                    c1++;
            _director.SetComing(c0, c1);
        }

        /// Everyone on the way let go to the game (the fight is over, or the add-on stopping).
        private void JoinPendingDrop(string why)
        {
            if (_joinPending.Count > 0)
            {
                foreach (JoinPending j in _joinPending)
                    JoinLetWalkerGo(j);
                Log.Detail("BRAWL " + _joinPending.Count + " on their way to join let go: " + why);
                _joinGaveUp += _joinPending.Count;
            }
            _joinPending.Clear();
            _director.SetComing(0, 0);
        }

        private static void JoinLetWalkerGo(JoinPending j)
        {
            if (!j.Walk || !ENTITY.DOES_ENTITY_EXIST(j.Handle) || ENTITY.IS_ENTITY_DEAD(j.Handle))
                return;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(j.Handle, false);
            TASK.CLEAR_PED_TASKS(j.Handle, true, false);
        }

        private bool JoinIsPending(int h)
        {
            foreach (JoinPending j in _joinPending)
                if (j.Handle == h)
                    return true;
            return false;
        }

        private void JoinAdd(int h, int side, int target, string name, string how, string with, string at)
        {
            if (!_director.AddFighter(h, side, target))
                return;
            _joinTotal++;
            _joinLast = name + " for " + with + ", at " + at;
            Log.Write("BRAWL joins: " + name + " takes " + how);
            Safe("join tie", () => NoteSceneTie("brawl", new List<Ped> { new Ped(h), new Ped(target) }));
        }

        /// Those on their way to join, every half second. One getting up: still on the bar or the chair 6 s on, taken off it
        /// at once (as the Director does for the two who began it); once up, in if he's close, else he runs over. One running
        /// over: in once he's within 6 m of the man he's going for (another of that side if that one is down). Each is
        /// dropped when his time is up (about 1.6 s a metre).
        private void JoinPendingStep(int now)
        {
            List<int> a, b;
            _director.FightSides(out a, out b);
            for (int i = _joinPending.Count - 1; i >= 0; i--)
            {
                JoinPending j = _joinPending[i];
                bool gone = !ENTITY.DOES_ENTITY_EXIST(j.Handle) || ENTITY.IS_ENTITY_DEAD(j.Handle);
                if (gone || now > j.Until || PED.IS_PED_IN_COMBAT(j.Handle, Game.Player.Character.Handle) || PED.IS_PED_FLEEING(j.Handle) || _director.Involves(j.Handle))
                {
                    if (!gone && !_director.Involves(j.Handle))
                        JoinLetWalkerGo(j);
                    _joinPending.RemoveAt(i);
                    if (!gone && now > j.Until)
                    {
                        _joinGaveUp++;
                        Log.Detail("BRAWL " + j.Name + " never got there (" + (now - j.Since) / 1000 + " s on the way)");
                    }
                    continue;
                }
                if (_director.IsOut(j.Target) || !ENTITY.DOES_ENTITY_EXIST(j.Target) || ENTITY.IS_ENTITY_DEAD(j.Target))
                {
                    List<int> theirs = j.Side == 0 ? b : a;
                    int t = theirs != null ? theirs.FirstOrDefault(x => x != j.Target && !_director.IsOut(x) && ENTITY.DOES_ENTITY_EXIST(x) && !ENTITY.IS_ENTITY_DEAD(x)) : 0;
                    if (t == 0)
                        continue;   // nobody over there to go for just now: he keeps coming, and AddFighter finds one
                    j.Target = t;
                    if (j.Walk)
                        JoinRunOver(j);
                }
                if (now < j.At)
                    continue;
                if (!j.Walk && PED.IS_PED_USING_ANY_SCENARIO(j.Handle))
                {
                    if (!j.Pulled && now - j.Since > 6000)
                    {
                        j.Pulled = true;
                        string how = ScenarioExit.Pull(j.Handle, x => JoinRunOver(j));   // the run the same tick: no idle pose between; the immediate clear only off camera (no T pose)
                        j.Until = Math.Max(j.Until, now + JoinWaitMs(ENTITY.GET_ENTITY_COORDS(j.Handle, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(j.Target, true, true)), false));   // the run's own time, as the path after a get-up gives it
                        Log.Write("BRAWL " + j.Name + " was still sat or leaning " + (now - j.Since) / 1000 + " s on: pulled out of it to join in (" + how + ")");
                    }
                    continue;
                }
                float off = ENTITY.GET_ENTITY_COORDS(j.Handle, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(j.Target, true, true));
                if (off > JoinArriveMetres)
                {
                    if (!j.Walk)
                    {
                        JoinRunOver(j);   // up off the bar, and the fight is across the street: he runs to it
                        j.Until = Math.Max(j.Until, now + JoinWaitMs(off, false));
                    }
                    continue;
                }
                _joinPending.RemoveAt(i);
                if (j.Walk)
                    _joinCameOver++;
                JoinAdd(j.Handle, j.Side, j.Target, j.Name, j.Why, j.With ?? NameOfFighter(j.Target), NameOfFighter(j.Target));
                if (!_director.Involves(j.Handle))
                    JoinLetWalkerGo(j);   // the fight wouldn't take him after all
            }
            JoinComingCount();
        }

        private Fighter FighterOf(int h)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, Game.GameTime) : null;
            return new Fighter { Id = w != null ? w.Id : null, Woman = ENTITY.DOES_ENTITY_EXIST(h) && !PED.IS_PED_MALE(h), On = _director.AttackersOn(h) };
        }

        private string NameOfFighter(int h)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, Game.GameTime) : null;
            return w != null && w.Name != null ? w.Name : "a stranger";
        }

        /// F7: who joined fights this session.
        private string JoinStatus()
        {
            if (!_cfg.MindLive || !_cfg.MindJoinFights)
                return "off ([Mind] MindJoinFights, with MindLive)";
            return _joinFailed ? "FAILED once this session (see the log)" : _joinTotal + " joined a fight this session" + (BrawlsOn ? ", the biggest " + _brawlBiggest + " fighting (cap " + _cfg.BrawlMost + "), " + _joinMet + " onlookers given a name to weigh, a brawl lasts " + _cfg.BrawlLeastSeconds + " s at least" : "") + "; joining from within " + JoinMetres.ToString("0") + " m, " + _joinCameOver + " ran over from afar, " + _joinGaveUp + " never got there, " + _joinPending.Count + " on the way now; the ring " + _ring.Count + " now" + (_joinLast != null ? " (last: " + _joinLast + ")" : "");
        }
    }
}
