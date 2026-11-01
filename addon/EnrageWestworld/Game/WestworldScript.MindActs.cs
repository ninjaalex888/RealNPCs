// The mind acts on its everyday choices (the NPC-life run, step 2; Lex, 2026-10-30: "start with the mind acting on
// choices"). Until now Choosing.Choose's answer was only written to the log ("MIND would: ... heads home"); here a changed
// choice of someone near and in view becomes what they do: a stroll, the nearest of the game's own spots for a rest, a
// meal, a drink, the card table or the work (a bench, a chair, the bar, a work point: the world's scenario points, which
// already fit where they stand), a walk off home or out of the weather, a walk clear of you or of someone they can't
// abide, a walk over to a friend or someone to talk to (a quiet talk may follow: two standing together), a wary eye on you.
// Gently: never someone sat or leaning, busy in a scene, held by enRAGE or the story; one new act every 3 s at most, six
// acting at once, and each let go to their own ways when it's done. Behind [Mind] MindLive and MindActs.
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
        private sealed class MindAct
        {
            public int Handle;
            public string Kind, Name;
            public int Until, Started, LookAt;
            public Vector3 To;
            public int Toward;          // the one they walk to (talk, greet), 0 none
            public bool Spot;           // sent to the nearest scenario point
            public string Arrive;       // at the place: "spot" (take the nearest seat or the bar), "browse" (a while at the counter), null
            public bool Arrived;
            public uint Then;           // a scenario in place to start at ThenAt (showoff: after the turn to face the street)
            public int ThenAt;
            public bool Seated;         // a look from where they sit (NIGHT4 fix 2): their scenario is never touched
        }

        private readonly List<MindAct> _mindActs = new List<MindAct>();
        private const int MindActsAtOnce = 10, MindActGapMs = 1500;   // 6 and 3 s until 2026-10-30 (Lex: more at once)
        private int _mindActNextAt, _mindActsDone, _mindActsSkipped;
        private string _mindActLast;
        private readonly Dictionary<string, int> _mindActCount = new Dictionary<string, int>();

        private bool IsMindActing(int h)
        {
            for (int i = 0; i < _mindActs.Count; i++)
                if (_mindActs[i].Handle == h)
                    return true;
            return false;
        }

        // what became of every choice tried (NIGHT4 fix 4): acted, or the reason it wasn't, so F7 can say where they go
        private readonly Dictionary<string, int> _mindActLost = new Dictionary<string, int>();
        private int _mindActsTried, _mindActsTakenUp, _mindChoseQuietly;
        private const string LostOff = "off", LostGap = "the 1.5 s gap", LostAtOnce = "10 acting at once", LostActing = "acting already", LostStand = "stand or showoff: no act",
                             LostUrge = "a feeling's urge: no act", LostNoSpot = "no free spot within 15 m", LostNoFoe = "nobody to step clear of", LostNoFriend = "no friend near",
                             LostNoTalk = "nobody to talk to", LostWatch = "watch: never saw you violent", LostNoAct = "no act for this choice",
                             LostBusyNod = "nodding to someone or listening to you", LostSatNoLook = "sat, leaning or at something already";
        private int _mindAlreadyAt, _mindLookAtYouAt, _mindSeatedLooks;

        /// A choice for someone near and in view (MindChooseFor, a pick's answer): what they do about it, if anything. Null when
        /// an act began; else why not (counted for F7, and kept on the person: MindWho.ActWhyNot).
        private string MindActOn(MindWho w, int h, int now)
        {
            string lost = MindActTry(w, h, now);
            if (lost == LostOff)
                return lost;
            _mindActsTried++;
            w.ActWhyNot = lost;
            if (lost == null)
            {
                w.ChoiceActed = true;
                w.ActedAt = now;
                return null;
            }
            int n;
            _mindActLost.TryGetValue(lost, out n);
            _mindActLost[lost] = n + 1;
            return lost;
        }

        private string MindActTry(MindWho w, int h, int now)
        {
            if (!_cfg.MindLive || !_cfg.MindActs || w.Choice == null)
                return LostOff;
            if (IsMindActing(h))
                return LostActing;
            string kind = w.Choice;
            bool looks = _cfg.MindActsSeatedLooks;   // NIGHT4 fix 2: watch, seated looks, showoff and "already at it"
            if (kind == "stand" || Choosing.CarriesOn(kind) || (kind == "showoff" && !looks))
                return LostStand;
            Ped p = new Ped(h);
            Ped me = Game.Player.Character;
            bool sat = looks && PED.IS_PED_USING_ANY_SCENARIO(h);
            string why = MindActWhyNot(p, h, sat);
            if (why != null)
            {
                _mindActsSkipped++;
                Log.Detail("MIND acts not: " + w.Name + " -> " + Choosing.WordsOf(kind) + " (" + why + ")");
                return why;
            }
            if (sat)
            {
                string already = MindAlreadyAt(kind, h);
                if (already != null)
                {
                    // sat on a bench and choosing a rest, at the bar and choosing a drink: they're doing it (counted, never a refusal)
                    _mindAlreadyAt++;
                    NoteMindAct(kind + ", at it already");
                    Log.Detail("MIND acts: " + w.Name + " " + Choosing.WordsOf(kind) + ", and is at it already (" + already + ")");
                    return null;
                }
            }
            if (now < _mindActNextAt)
                return LostGap;
            if (_mindActs.Count >= MindActsAtOnce)
                return LostAtOnce;
            if (sat)
                return MindSeatedLook(w, h, kind, now, me, false);
            Vector3 at = p.Position;
            var a = new MindAct { Handle = h, Kind = kind, Name = w.Name, Started = now, Until = now + 60000 };
            string did;
            switch (kind)
            {
                case "stroll":
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    a.Until = now + 30000;
                    did = "takes a stroll";
                    break;
                case "rest": case "eat": case "drink": case "cards": case "work": case "pray":
                    // the nearest of the world's own spots: a bench or a porch outside, a chair or the bar within, a work point by
                    // the store; it may be none, and then nothing happens (Rockstar's own call: radius, then how long)
                    if (!TASK.DOES_SCENARIO_EXIST_IN_AREA(at, 15f, true, 0, false))
                    {
                        _mindActsSkipped++;
                        Log.Detail("MIND acts not: " + w.Name + " -> " + Choosing.WordsOf(kind) + " (no free spot within 15 m)");
                        return LostNoSpot;
                    }
                    TASK._TASK_USE_NEAREST_SCENARIO_TO_COORD(h, at, 15f, 90000, false, true, false, false);
                    a.Spot = true;
                    a.LookAt = now + 6000;   // which spot they took, for the log
                    a.Until = now + 90000;
                    did = Choosing.WordsOf(kind) + " (the nearest spot)";
                    break;
                case "saloon": case "store": case "church":
                    if (MindToPlace(a, w, kind, at, now, out did))
                        break;
                    goto case "home";   // no place known in this town yet: off out of the scene, as before
                case "home": case "shelter":
                    {
                        // off out of the scene, away from you, along their own way (seeded by who they are)
                        Vector3 away = at - me.Position;
                        away.Z = 0f;
                        double turn = (Voice.Hash(w.Id + "|home") % 120 - 60) * Math.PI / 180.0;
                        Vector3 dir = away.Length() < 0.5f ? new Vector3(1f, 0f, 0f) : away.Normalized;
                        dir = new Vector3((float)(dir.X * Math.Cos(turn) - dir.Y * Math.Sin(turn)), (float)(dir.X * Math.Sin(turn) + dir.Y * Math.Cos(turn)), 0f);
                        a.To = at + dir * 60f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, a.To.X, a.To.Y, a.To.Z, 1f, -1, 1f, 0, 0f);
                        a.Until = now + 60000;
                        did = Choosing.WordsOf(kind) + " (walks off)";
                        break;
                    }
                case "keepclear":
                    {
                        Vector3 away = at - me.Position;
                        away.Z = 0f;
                        Vector3 dir = away.Length() < 0.5f ? new Vector3(0f, 1f, 0f) : away.Normalized;
                        a.To = at + dir * 20f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, a.To.X, a.To.Y, a.To.Z, 1f, -1, 1f, 0, 0f);
                        a.Until = now + 25000;
                        did = "keeps well clear of you";
                        break;
                    }
                case "steerclear":
                    {
                        int foe = MindFoeNear(w, h);
                        if (foe == 0)
                            return LostNoFoe;
                        Vector3 away = at - ENTITY.GET_ENTITY_COORDS(foe, true, true);
                        away.Z = 0f;
                        Vector3 dir = away.Length() < 0.5f ? new Vector3(0f, 1f, 0f) : away.Normalized;
                        a.To = at + dir * 15f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, a.To.X, a.To.Y, a.To.Z, 1f, -1, 1f, 0, 0f);
                        a.Until = now + 25000;
                        did = "steps clear of " + (MindNameOf(foe) ?? "someone they can't abide");
                        break;
                    }
                case "greet": case "talk":
                    {
                        int to = kind == "greet" ? MindFriendNear(w, h) : MindSomeoneToTalkTo(h, at);
                        if (to == 0)
                            return kind == "greet" ? LostNoFriend : LostNoTalk;
                        a.Toward = to;
                        TASK.TASK_GO_TO_ENTITY(h, to, 20000, 1f, 1.4f, 0f, 0);
                        a.Until = now + 20000;
                        did = (kind == "greet" ? "goes over to " : "goes to talk to ") + (MindNameOf(to) ?? "someone");
                        break;
                    }
                case "watch":
                    {
                        bool feared = _engine.SawYouViolent(w.Id, 3);
                        if (!looks)
                        {
                            if (!feared)
                                return LostWatch;
                            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 8000, 0, 51, 0);
                            a.Until = now + 8000;
                            did = "keeps a wary eye on you";
                            break;
                        }
                        if (IsAcknowledging(h))
                            return LostBusyNod;
                        // a look at you within 15 m in view (one person every 10 s), else at the nearest stranger; nobody to look
                        // at: a stoic stare in place (NIGHT4 fix 2: watch was the commonest unacted choice and never reached the screen)
                        bool atYou;
                        int target = MindLookTarget(h, at, me, now, true, out atYou);
                        if (target != 0)
                        {
                            int ms = 6000 + (int)(Voice.Hash(w.Id + "|watch") % 2000);
                            TASK.TASK_LOOK_AT_ENTITY(h, target, ms, 0, 51, 0);
                            a.Until = now + ms;
                            did = atYou ? (feared ? "keeps a wary eye on you" : "keeps an eye on you") : "keeps an eye on the street (" + (MindNameOf(target) ?? "a passer-by") + ")";
                        }
                        else
                        {
                            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_STARE_STOIC"), 10000, true, 0u, -1f, false);
                            a.Until = now + 10000;
                            did = "keeps an eye on the street (stands and watches)";
                        }
                        break;
                    }
                case "showoff":
                    {
                        if (IsAcknowledging(h))
                            return LostBusyNod;
                        // turned to face whoever is about, then one of the in-place scenarios the scenes already use (Director.cs)
                        bool atYou;
                        int target = MindLookTarget(h, at, me, now, true, out atYou);
                        if (target != 0)
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, target, 1500, 0f, 0f, 0f);
                        a.Then = Natives.Hash(w.P != null && w.P.Woman ? "WORLD_HUMAN_STARE_STOIC" : (Voice.Hash(w.Id + "|showoff") & 1) == 0 ? "WORLD_HUMAN_SMOKE" : "WORLD_HUMAN_STARE_STOIC");
                        a.ThenAt = now + (target != 0 ? 1500 : 0);
                        a.Until = now + 15000;
                        did = "makes sure to be seen" + (target != 0 ? " (facing " + (atYou ? "you" : MindNameOf(target) ?? "the street") + ")" : "");
                        break;
                    }
                default:
                    return kind.StartsWith("urge:", StringComparison.Ordinal) ? LostUrge : LostNoAct;
            }
            _mindActs.Add(a);
            _mindActNextAt = now + MindActGapMs;
            NoteMindAct(kind);
            _mindActLast = w.Name + " " + did;
            Log.Write("MIND acts: " + w.Name + " " + did + " (" + w.ChoiceWhy + ", " + (int)at.DistanceTo(me.Position) + " m from you)");
            return null;
        }

        private void NoteMindAct(string key)
        {
            _mindActsDone++;
            int n;
            _mindActCount.TryGetValue(key, out n);
            _mindActCount[key] = n + 1;
        }

        // the game's scenario types, by name (rdr3_discoveries' list, as Roughs.cs has them), for "at it already" (NIGHT4 fix 2)
        private static readonly string[] EatScenarios = { "PROP_HUMAN_SEAT_CHAIR_TABLE_EATING_KNIFE_FORK", "PROP_CAMP_SEAT_CHAIR_TABLE_STEW" };
        private static readonly string[] WorkScenarios = { "WORLD_HUMAN_BARTENDER_CLEAN_GLASS", "WORLD_HUMAN_BARTENDER" };
        private static uint[] _eatHashes, _workHashes, _barHashes;

        private static string ScenarioOf(int h, uint type, string[] names, ref uint[] hashes)
        {
            if (hashes == null)
            {
                var hs = new uint[names.Length];
                for (int i = 0; i < names.Length; i++)
                    hs[i] = Natives.Hash(names[i]);
                hashes = hs;
            }
            for (int i = 0; i < hashes.Length; i++)
                if (type == hashes[i] || PED.IS_PED_USING_SCENARIO_HASH(h, hashes[i]))
                    return names[i];
            return null;
        }

        /// Sat or leaning in a scenario that already is what their mind chose (a rest anywhere they sit or lean but at work, a drink
        /// at the bar, a meal at a table, the barkeep's work): the scenario's name, else null (an unknown type is logged by number).
        private string MindAlreadyAt(string kind, int h)
        {
            if (kind != "rest" && kind != "drink" && kind != "eat" && kind != "work")
                return null;
            uint type = (uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h);
            string work = ScenarioOf(h, type, WorkScenarios, ref _workHashes);
            if (kind == "work")
                return work;
            if (kind == "rest")
                return work == null ? "sat or leaning, scenario 0x" + type.ToString("X8") : null;
            if (kind == "drink")
                return ScenarioOf(h, type, BarScenarios, ref _barHashes);
            return ScenarioOf(h, type, EatScenarios, ref _eatHashes);
        }

        /// Whom someone at `at` looks at: you, within 15 m and in plain view (one person every 10 s at most), else the nearest
        /// other person in the mind's near ring within 15 m; 0 for nobody.
        private int MindLookTarget(int h, Vector3 at, Ped me, int now, bool allowYou, out bool atYou)
        {
            atYou = false;
            if (allowYou && at.DistanceTo(me.Position) <= 15f && (now - _mindLookAtYouAt >= 10000 || now < _mindLookAtYouAt) && NodInView(me, new Ped(h)))
            {
                _mindLookAtYouAt = now;
                atYou = true;
                return me.Handle;
            }
            int best = 0;
            float bestD = 15f;
            for (int i = 0; i < _mindNear.Count; i++)
            {
                int oh = _mindNear[i];
                if (oh == h || !ENTITY.DOES_ENTITY_EXIST(oh) || ENTITY.IS_ENTITY_DEAD(oh))
                    continue;
                float d = ENTITY.GET_ENTITY_COORDS(oh, true, true).DistanceTo(at);
                if (d < bestD)
                {
                    bestD = d;
                    best = oh;
                }
            }
            return best;
        }

        /// Someone sat or leaning acts on a look-only choice from where they are: a watch (you or the street), a wary look at you
        /// (keep clear), a look at a friend (greet) or at someone near to talk to. TASK_LOOK_AT_ENTITY only, as the waking's glance,
        /// which plays on seated people; the scenario is never touched. Null when the look began, else why not.
        private string MindSeatedLook(MindWho w, int h, string kind, int now, Ped me, bool test)
        {
            if (kind != "watch" && kind != "keepclear" && kind != "greet" && kind != "talk")
                return LostSatNoLook;   // getting up for it is stage 1 of 0.7.12, not built
            if (IsAcknowledging(h))
                return LostBusyNod;
            Vector3 at = ENTITY.GET_ENTITY_COORDS(h, true, true);
            bool atYou = false;
            int target;
            if (kind == "greet")
                target = MindFriendNear(w, h);
            else if (kind == "keepclear")
            {
                target = MindLookTarget(h, at, me, now, true, out atYou);
                if (!atYou)
                    target = 0;   // a wary look is at you or nobody
            }
            else
                target = MindLookTarget(h, at, me, now, kind == "watch", out atYou);
            if (target == 0)
                return kind == "greet" ? LostNoFriend : kind == "talk" ? LostNoTalk : LostSatNoLook;
            int ms = 6000 + (int)(Voice.Hash(w.Id + "|" + kind + "|look") % 2000);
            TASK.TASK_LOOK_AT_ENTITY(h, target, ms, 0, 51, 0);
            _mindActs.Add(new MindAct { Handle = h, Kind = kind, Name = w.Name, Started = now, Until = now + ms, Seated = true });
            _mindActNextAt = now + MindActGapMs;
            _mindSeatedLooks++;
            NoteMindAct(kind + ", a look from a seat");
            string whom = atYou ? "you" : MindNameOf(target) ?? "someone near";
            string did = kind == "watch" ? (atYou ? "keeps an eye on you" : "keeps an eye on the street (" + whom + ")") : kind == "keepclear" ? "eyes you warily" : "looks over at " + whom;
            _mindActLast = w.Name + " " + did + " from where they sit";
            Log.Write("MIND acts: " + w.Name + " " + did + " from where they sit (" + (test ? "the Testing row" : w.ChoiceWhy) + ", " + (int)at.DistanceTo(me.Position) + " m from you)");
            return null;
        }

        /// Why this body mayn't be moved by its own choice now, or null.
        private string MindActWhyNot(Ped p, int h, bool sitOk = false)
        {
            if (!p.Exists() || p.IsDead)
                return "gone";
            if (AimedLately(h, Game.GameTime))
                return "aimed at just now";   // left to the game's own answer to a gun (WestworldScript.AimLetGo.cs)
            if (Natives.StoryModel(p) || !IsGrownUp(p) || Natives.IsLaw(p))
                return "the story's, a child or the law";
            if (!sitOk && PED.IS_PED_USING_ANY_SCENARIO(h))   // (sitOk: a look from where they sit, which never touches the scenario; NIGHT4 fix 2)
                return "sat, leaning or at something already";
            if (PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h))
                return "riding, fighting or running";
            if (NotFree(h) || Spawner.IsOurs(h))   // (NotFree: a scene, Talk, a walk-up, a nod, a wake act and its other, the law at work)
                return "busy with something of RealNPCs'";
            return MayDirectWhyNot(p);
        }

        /// Each frame's look at those acting: done, gone or arrived, they're let go to their own ways.
        private void MindActsStep(int now)
        {
            for (int i = _mindActs.Count - 1; i >= 0; i--)
            {
                MindAct a = _mindActs[i];
                int h = a.Handle;
                bool gone = !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h);
                if (!gone && TakenUp(h))
                {
                    _mindActsTakenUp++;
                    _mindActs.RemoveAt(i);   // another feature has them now (Talk, a walk-up, a scene): the act ends, with no new orders (the cross-cutting review; a nod is not enough)
                    continue;
                }
                if (!gone && a.Then != 0 && now >= a.ThenAt)
                {
                    if (!PED.IS_PED_USING_ANY_SCENARIO(h) && !_director.Involves(h))
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, a.Then, 12000, true, 0u, -1f, false);   // showoff: in place, facing the street (NIGHT4 fix 2)
                    a.Then = 0;
                }
                if (!gone && a.LookAt > 0 && now >= a.LookAt)
                {
                    a.LookAt = 0;
                    if (a.Spot)
                        Log.Detail("MIND acts: " + a.Name + (PED.IS_PED_USING_ANY_SCENARIO(h) ? " took a spot (scenario type " + TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h) + ")" : " found no spot to take"));
                }
                bool arrived = !gone && a.Toward != 0 && ENTITY.DOES_ENTITY_EXIST(a.Toward) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(a.Toward, true, true)) < 2f;
                if (arrived)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, a.Toward, 2000, 0f, 0f, 0f);   // face to face: a talk may start between them
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(a.Toward, h, 2000, 0f, 0f, 0f);
                }
                bool there = !gone && a.To != default(Vector3) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(a.To) < 3f;
                if (there && a.Arrive != null && !a.Arrived && !_director.Involves(h))
                {
                    // at the place: a seat, the bar rail or a pew nearby, or a while at the counter, then their own way
                    a.Arrived = true;
                    Vector3 p = ENTITY.GET_ENTITY_COORDS(h, true, true);
                    if (a.Arrive == "spot" && TASK.DOES_SCENARIO_EXIST_IN_AREA(p, 8f, true, 0, false))
                        TASK._TASK_USE_NEAREST_SCENARIO_TO_COORD(h, p, 8f, 150000, false, true, false, false);
                    else
                        TASK.TASK_STAND_STILL(h, 30000);
                    a.Until = now + (a.Arrive == "spot" ? 150000 : 30000);
                    Log.Detail("MIND acts: " + a.Name + " is at the " + a.Kind + (PED.IS_PED_USING_ANY_SCENARIO(h) ? ", and took a spot" : ""));
                    if (a.Kind == "store")
                        Safe("plans", () => PlanAtStore(h, now));   // something to give, for a plan to court or to thank (WestworldScript.Plans.cs)
                    continue;
                }
                if (a.Arrived && !gone && now < a.Until && !_director.Involves(h))
                    continue;   // a while there
                if (gone || arrived || there || now >= a.Until || _director.Involves(h))
                {
                    _mindActs.RemoveAt(i);
                    if (!gone && there && (a.Kind == "home" || a.Kind == "shelter" || a.Kind == "saloon" || a.Kind == "store" || a.Kind == "church"))
                        TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // on their way, beyond the scene
                }
            }
        }

        /// To the real place in this town: the bar (the barkeep's spot), the store's counter (the keeper's), the church's door
        /// (Churches); false when none is known here, or it's far.
        private bool MindToPlace(MindAct a, MindWho w, string kind, Vector3 at, int now, out string did)
        {
            did = null;
            Vector3 to;
            string where;
            if (kind == "church")
            {
                if (_placeTown == null || !Churches.TryGetValue(_placeTown, out to))
                    return false;
                where = "the church";
            }
            else
            {
                PlaceSpot s = PlaceSpots.Find(_store.Spots, _placeTown, PlaceSpots.KindFor(kind), at.X, at.Y);
                if (s == null)
                    return false;
                to = new Vector3(s.X, s.Y, s.Z);
                where = kind == "saloon" ? "the bar" : "the store's counter";
            }
            float d = to.DistanceTo(at);
            if (d > 150f || d < 3f)
                return false;
            a.To = to;
            a.Arrive = kind == "store" ? "browse" : "spot";
            a.Until = now + 90000;
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(a.Handle, to.X, to.Y, to.Z, 1f, -1, 2.5f, 0, 0f);
            did = Choosing.WordsOf(kind) + " (" + where + ", " + (int)d + " m)";
            return true;
        }

        private int MindFoeNear(MindWho w, int h)
        {
            foreach (int oh in _mindNear)
            {
                MindWho o;
                if (oh == h || !_mindWho.TryGetValue(oh, out o) || o.Id == null)
                    continue;
                SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(w.Id) && x.Has(o.Id));
                if (t != null && t.Warmth < 0 && ENTITY.DOES_ENTITY_EXIST(oh))
                    return oh;
            }
            return 0;
        }

        private int MindFriendNear(MindWho w, int h)
        {
            int best = 0, warm = 0;
            foreach (int oh in _mindNear)
            {
                MindWho o;
                if (oh == h || !_mindWho.TryGetValue(oh, out o) || o.Id == null || !ENTITY.DOES_ENTITY_EXIST(oh) || NotFree(oh))
                    continue;
                SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(w.Id) && x.Has(o.Id));
                if (t != null && t.Warmth > warm)
                {
                    warm = t.Warmth;
                    best = oh;
                }
            }
            return best;
        }

        private int MindSomeoneToTalkTo(int h, Vector3 at)
        {
            int best = 0;
            float bestD = 12f;
            foreach (int oh in _mindNear)
            {
                if (oh == h || !ENTITY.DOES_ENTITY_EXIST(oh) || ENTITY.IS_ENTITY_DEAD(oh) || NotFree(oh) || IsMindActing(oh) || PED.IS_PED_IN_COMBAT(oh, 0))
                    continue;
                float d = ENTITY.GET_ENTITY_COORDS(oh, true, true).DistanceTo(at);
                if (d > 2.5f && d < bestD)
                {
                    bestD = d;
                    best = oh;
                }
            }
            return best;
        }

        private string MindNameOf(int h)
        {
            MindWho o;
            return _mindWho.TryGetValue(h, out o) && o.Name != null ? o.Name : null;
        }

        /// The Testing row (NIGHT4 fix 2): the nearest person the mind knows who is sat or leaning, free, within 15 m, looks up
        /// from where they sit as a watch would have them. Null when it played, else why not.
        private string SeatedLookTest(int now)
        {
            if (!_cfg.MindLive || !_cfg.MindActs || !_cfg.MindActsSeatedLooks)
                return "[Mind] MindLive, MindActs and MindActsSeatedLooks must be on";
            Ped me = Game.Player.Character;
            int best = 0;
            float bestD = 15f;
            string lastWhy = null;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || !PED.IS_PED_USING_ANY_SCENARIO(h) || IsMindActing(h))
                    continue;
                string why = MindActWhyNot(new Ped(h), h, true);
                if (why != null)
                {
                    lastWhy = w.Name + ": " + why;
                    continue;
                }
                float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position);
                if (d < bestD)
                {
                    bestD = d;
                    best = h;
                }
            }
            if (best == 0)
                return "nobody the mind knows sat or leaning and free within 15 m" + (lastWhy != null ? " (the last passed over: " + lastWhy + ")" : "");
            _mindLookAtYouAt = 0;   // the row always tries you first
            return MindSeatedLook(_mindWho[best], best, "watch", now, me, true);
        }

        /// F7: what the minds did this session.
        private string MindActsStatus()
        {
            if (!_cfg.MindLive || !_cfg.MindActs)
                return "off ([Mind] MindActs, with MindLive)";
            int acting, scenes, named;
            MindActingShare(Game.GameTime, out acting, out scenes, out named);
            return MindShareWords(acting, scenes, named) + "; " + (_cfg.MindActsSeatedLooks ? "watch, looks and showoff on: " + _mindSeatedLooks + " looks from a seat, " + _mindAlreadyAt + " already at what they chose; " : "watch, looks and showoff off ([Mind] MindActsSeatedLooks); ") +
                   _mindActsDone + " acted on their choices this session (" + (_mindActCount.Count == 0 ? "none yet" : string.Join(", ", _mindActCount.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   "), " + _mindActs.Count + " acting now, " + _mindActsSkipped + " passed over" + (_mindActLast != null ? "; last: " + _mindActLast : "") +
                   "; of " + _mindActsTried + " tried, not acted: " + (_mindActLost.Count == 0 ? "none" : string.Join(", ", _mindActLost.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   "; taken up by something else mid-act " + _mindActsTakenUp + "; chosen quietly (the law, children, the story's) " + _mindChoseQuietly;
        }

        /// The law, a child, one of the story's own: the mind chooses for them, but no act is ever tried (NIGHT4 fix 4).
        private bool MindChosenQuietly(int h)
        {
            Ped p = new Ped(h);
            return p.Exists() && (Natives.StoryModel(p) || !IsGrownUp(p) || Natives.IsLaw(p));
        }

        /// The goal's measure in game (NIGHT4 fix 4): of the named people in the mind's near ring, how many are acting on their
        /// minds now (an act under way, or their present choice acted in the last two minutes) and how many are in a scene.
        private void MindActingShare(int now, out int acting, out int scenes, out int named)
        {
            acting = scenes = named = 0;
            for (int i = 0; i < _mindNear.Count; i++)
            {
                int h = _mindNear[i];
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                named++;
                if (IsMindActing(h) || (w.ChoiceActed && now - w.ActedAt >= 0 && now - w.ActedAt < 120000))
                    acting++;
                else if (_director != null && _director.Involves(h))
                    scenes++;
            }
        }

        private static string MindShareWords(int acting, int scenes, int named)
        {
            return named == 0 ? "nobody named near you" : acting + " of " + named + " named near you acting on their minds (" + (100 * acting / named) + "%), " + scenes + " more in a scene (" + (100 * (acting + scenes) / named) + "% in all)";
        }

        private int _mindShareMinuteAt, _mindShareLastTried;

        /// Once a minute while the mind is trying acts: the share near you acting on their minds, for the log (NIGHT4 fix 4).
        private void MindShareMinute(int now)
        {
            if (now < _mindShareMinuteAt && now >= _mindShareMinuteAt - 120000)
                return;
            _mindShareMinuteAt = now + 60000;
            if (_mindActsTried == _mindShareLastTried || !_cfg.MindLive || !_cfg.MindActs)
                return;
            int tried = _mindActsTried - _mindShareLastTried;
            _mindShareLastTried = _mindActsTried;
            int acting, scenes, named;
            MindActingShare(now, out acting, out scenes, out named);
            Log.Write("MIND acting minute: " + MindShareWords(acting, scenes, named) + "; " + tried + " choices tried this minute, " + _mindActsDone + " acts this session");
        }
    }
}
