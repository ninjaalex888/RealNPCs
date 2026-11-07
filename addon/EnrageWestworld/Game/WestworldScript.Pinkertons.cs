// Pinkertons asking, staged (FIVEDAY item 15; Core/Pinkertons.cs). By day in a town where a violent deed of yours is known
// from the past week, once in a while (a town's game day's roll, five days a town between), a man in a city suit walks in
// from out of view and works the street: up to three of the town's people the mind knows, near you, each walked up to and
// asked after "a man of your description"; each answers by what they feel for you (a friend lies, the afraid talk, the rest
// shrug; Pinkertons.Answer). Then he thanks the last of them, walks away and is let go once out of sight. The town keeps
// what was said: a friend coming over later (FriendCalls' warning) tells you he was asking and who talked.
// As with the other encounters, he never appears where you can see, nor in a mission, a wanted spell or a fight. A gun
// on him, shots near or his being hurt ends it, and he goes. Behind [Encounters] Pinkertons.
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
        private sealed class PinkRun
        {
            public string Town, DeedKind, Seed, Talked;
            public uint Model;
            public Vector3 At;
            public Ped Man;
            public List<int> Asked = new List<int>();
            public int Step, Since, NextAt, Health, Target, Lied, Told, Shrugged, LeaveAt;
            public bool Test;
        }

        private sealed class PinkVisit
        {
            public long At;
            public string Talked;
            public bool Warned;
        }

        private PinkRun _pink;
        private int _pinkNextLook, _pinkVisits, _pinkLies, _pinkTold;
        private string _pinkWhyNot;
        private Action _aPink;
        private readonly Dictionary<string, PinkVisit> _pinkByTown = new Dictionary<string, PinkVisit>();
        private const int PinkModelPick = 3;   // the Saint Denis men's pool's gentleman (Spawner.ModelFor): the city suit

        private void PinkertonsStep(int now)
        {
            if (!_cfg.Pinkertons)
            {
                if (_pink != null)
                    EndPinkerton("switched off");
                return;
            }
            if (_store == null || _engine == null)
                return;
            if (_pink == null && now < _pinkNextLook)
                return;
            if (_aPink == null)
                _aPink = () =>
                {
                    Ped me = Game.Player.Character;
                    if (me == null || !me.Exists() || me.IsDead)
                        return;
                    if (_pink != null)
                    {
                        if (_now >= _pink.NextAt)
                            PinkertonStep(me, _now);
                    }
                    else
                    {
                        _pinkNextLook = _now + 60000;   // a look a real minute
                        PinkertonLook(me, _now, false);
                    }
                };
            Safe("pinkertons", _aPink);
        }

        /// A violent deed of yours known in this town within the week: its kind, else null.
        private string PinkDeed(string town, long g)
        {
            for (int i = _store.Deeds.Count - 1; i >= 0; i--)
            {
                Deed d = _store.Deeds[i];
                if (d == null || g - d.At > Pinkertons.KnownDays * 1440L)
                    continue;
                if (d.Known && d.Town == town && Minds.DeedGroup(d.Kind) == "deed:violence")
                    return d.Kind;
            }
            return null;
        }

        /// The people he'll ask: the town's people the mind knows, alive, free, within 50 m of you, nearest first.
        private List<int> PinkAskable(Ped me, int now)
        {
            var list = new List<int>();
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > 50f || MayDirectWhyNot(p) != null || NotFree(h) || _director.Involves(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false))
                    continue;
                list.Add(h);
            }
            return list.OrderBy(h => ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me.Position)).ToList();
        }

        /// Sets one coming, or says why not. test: the Testing row's, with no deed, day's roll or rest needed.
        private string PinkertonLook(Ped me, int now, bool test)
        {
            if (_pink != null)
                return _pinkWhyNot = "one is already in town";
            if (_placeTown == null)
                return _pinkWhyNot = "out of the towns";
            if (MISC.GET_MISSION_FLAG() || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0))
                return _pinkWhyNot = "a mission, or you're wanted or fighting";
            long g = Bridge.Now();
            int hour = (int)(g / 60 % 24);
            string kind = PinkDeed(_placeTown, g);
            if (!test)
            {
                if (hour < 8 || hour >= 19)
                    return _pinkWhyNot = "he asks by day (8 to 7)";
                if (kind == null)
                    return _pinkWhyNot = "no violence of yours known in " + _placeTown + " this week";
                if (!Voice.Chance(_placeTown + "|" + g / 1440 + "|pinkerton", Pinkertons.Chance))
                    return _pinkWhyNot = "not today in " + _placeTown + " (the day's roll)";
                string skipped;
                string pre = Pacing.WhyNot(_store.Pace, "pinkerton", _placeTown, null, g, 0, false, out skipped);
                if (pre != null)
                    return _pinkWhyNot = "rests: " + pre;
            }
            if (PinkAskable(me, now).Count < (test ? 1 : 2))
                return _pinkWhyNot = "too few of the town's people near to ask";
            Vector3 at;
            if (!EncounterSpot(me, 25f, 45f, out at))
                return _pinkWhyNot = "nowhere out of view for him to come from";
            if (!test)
                Pacing.Note(_store.Pace, "pinkerton", _placeTown, null, g);
            _pink = new PinkRun { Town = _placeTown, DeedKind = kind ?? "a test", Seed = _placeTown + "|" + g, Model = Spawner.ModelFor("Saint Denis", false, PinkModelPick), At = at, Since = now, NextAt = now, Test = test };
            Log.Write("PINKERTON coming to " + _placeTown + " to ask after you (your " + (kind ?? "test") + " known here)" + (test ? " (a test)" : ""));
            return _pinkWhyNot = null;
        }

        private void PinkertonStep(Ped me, int now)
        {
            PinkRun r = _pink;
            r.NextAt = now + 250;
            if (MISC.GET_MISSION_FLAG())
            {
                EndPinkerton("a mission");
                return;
            }
            if (r.Man == null)
            {
                if (now - r.Since > 15000)
                {
                    EndPinkerton("the game never streamed his body in (" + Spawner.LastSpawnWhy + ")");
                    return;
                }
                if (!Spawner.Ready(r.Model) || !Spawner.SafeToSpawn())
                    return;
                Ped p = Spawner.Spawn(r.Model, r.At, 0f);
                if (p == null)
                    return;
                r.Man = p;
                r.Health = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                r.Since = now;
                Log.Write("PINKERTON in " + r.Town + ": a man in a city suit walks into the street");
                return;
            }
            if (!r.Man.Exists() || r.Man.IsDead || ENTITY.GET_ENTITY_HEALTH(r.Man.Handle) < r.Health ||
                PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, r.Man.Handle) || PED.IS_PED_SHOOTING(me.Handle))
            {
                EndPinkerton("broken off: he was hurt, or a gun came out");
                return;
            }
            if (r.LeaveAt > 0)
            {
                if (now >= r.LeaveAt || r.Man.Position.DistanceTo(me.Position) > 80f)
                    EndPinkerton(null);
                return;
            }
            if (r.Man.Position.DistanceTo(me.Position) > 120f || now - r.Since > 240000)
            {
                PinkLeave(r, now, "you went, or he took too long");
                return;
            }
            if (r.Target == 0)
            {
                if (r.Asked.Count >= Pinkertons.MostAsked)
                {
                    PinkLeave(r, now, null);
                    return;
                }
                int next = PinkAskable(me, now).FirstOrDefault(h => !r.Asked.Contains(h));
                if (next == 0)
                {
                    PinkLeave(r, now, r.Asked.Count == 0 ? "nobody to ask" : null);
                    return;
                }
                r.Target = next;
                r.Step = 0;
                TASK.TASK_GO_TO_ENTITY(r.Man.Handle, next, 30000, 1.6f, 1.0f, 0f, 0);
                return;
            }
            int t = r.Target;
            if (!ENTITY.DOES_ENTITY_EXIST(t) || ENTITY.IS_ENTITY_DEAD(t))
            {
                r.Asked.Add(t);
                r.Target = 0;
                return;
            }
            MindWho w;
            _mindWho.TryGetValue(t, out w);
            string name = w != null && w.Name != null ? w.Name : TalkName(new Ped(t));
            string seed = r.Seed + "|" + (w != null ? w.Id : t.ToString());
            switch (r.Step)
            {
                case 0:   // walking up
                    if (r.Man.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(t, true, true)) > 2.4f)
                    {
                        if (now - r.Since > 200000)
                            PinkLeave(r, now, "he never reached them");
                        return;
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Man.Handle, t, 9000, 0f, 0f, 0f);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(t, r.Man.Handle, 9000, 0f, 0f, 0f);
                    SpeechCaption("A man in a city suit", Pinkertons.AskLine(seed), r.Man.Handle, 4000);
                    r.Step = 1;
                    r.NextAt = now + 4000;
                    return;
                case 1:   // the answer, by what they feel for you
                    {
                        long g = Bridge.Now();
                        bool friend = w != null && AllyWarmth(new Ped(t), w) >= 2;
                        bool afraid = w != null && w.Id != null && FeltAboutYou(w.Id, "fear", g) >= 0.3f;
                        Traits tr = w != null && w.Src != null ? (w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src))) : null;
                        string ans = Pinkertons.Answer(friend, afraid, tr, seed);
                        SpeechCaption(name, Pinkertons.AnswerLine(ans, seed), t, 4000);
                        if (ans == "lie") { r.Lied++; _pinkLies++; }
                        else if (ans == "talk") { r.Told++; _pinkTold++; if (r.Talked == null) r.Talked = name; }
                        else r.Shrugged++;
                        Log.Write("PINKERTON asked " + name + " after you: " + ans + (friend ? " (a friend)" : "") + (afraid ? " (afraid of you)" : ""));
                        r.Asked.Add(t);
                        r.Step = 2;
                        r.NextAt = now + 3500;
                        return;
                    }
                default:
                    if (r.Asked.Count >= Pinkertons.MostAsked || PinkAskable(me, now).All(h => r.Asked.Contains(h)))
                        SpeechCaption("A man in a city suit", Pinkertons.LeaveLine(seed), r.Man.Handle, 3000);
                    r.Target = 0;
                    r.NextAt = now + 1500;
                    return;
            }
        }

        /// He's done: walks off, the visit kept for a friend to warn you of, and he's handed back once away or after a while.
        private void PinkLeave(PinkRun r, int now, string why)
        {
            if (r.Asked.Count > 0)
            {
                _pinkByTown[r.Town] = new PinkVisit { At = Bridge.Now(), Talked = r.Talked };   // a test's too, so the warning can be tried
                _pinkVisits++;
            }
            Log.Write("PINKERTON leaves " + r.Town + ": asked " + r.Asked.Count + ", " + r.Lied + " lied for you, " + r.Told + " told him, " + r.Shrugged + " shrugged" + (why != null ? " (" + why + ")" : ""));
            if (r.Man != null && r.Man.Exists())
                TASK.TASK_WANDER_STANDARD(r.Man.Handle, 10f, 10);
            r.LeaveAt = now + 30000;
            r.Target = 0;
        }

        private void EndPinkerton(string why)
        {
            PinkRun r = _pink;
            _pink = null;
            if (r == null)
                return;
            if (r.Man != null && r.Man.Exists())
                Spawner.Release(r.Man, !ENTITY.IS_ENTITY_ON_SCREEN(r.Man.Handle));
            if (why != null)
                Log.Write("PINKERTON gone from " + r.Town + " (" + why + ")");
        }

        /// For a friend's warning (FriendDanger): a Pinkerton asked here in two days and no friend has warned you yet.
        private PinkVisit PinkToWarn(string town, long g)
        {
            PinkVisit v;
            return town != null && _pinkByTown.TryGetValue(town, out v) && !v.Warned && g - v.At <= 2880 ? v : null;
        }

        private string PinkertonsStatus()
        {
            if (!_cfg.Pinkertons)
                return "off ([Encounters] Pinkertons)";
            return _pinkVisits + " Pinkerton visits this session, " + _pinkLies + " lied for you, " + _pinkTold + " told him" +
                   (_pink != null ? "; one in " + _pink.Town + " now, asked " + _pink.Asked.Count : "") + (_pinkWhyNot != null ? "; none now: " + _pinkWhyNot : "");
        }
    }
}
