// Friends walk together, the game side (FOURTEENHOUR item 15; Core/WalkTogether.cs; [Mind] WalkTogether). When a quiet talk between
// two the mind knows plays to its end (the Director's PlayedOut), and they're friends (warmth 2 or more) or a courting couple, they
// may go on together: to the saloon's door after work, homeward at dusk, or for a stroll; side by side (the walk the Director's
// leavewith and the campfire's walkers use, seen working), a thought on the way, and a word and a turn to each other as they part.
// Two pairs a town at once at most, a pair once a day. While they walk, nothing else of ours takes them (NotFree). A look a second.
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
        private sealed class WalkPair
        {
            public Ped A, B;
            public string AName, BName, Where, Seed;
            public Vector3 To;
            public int Until;
        }

        private readonly List<WalkPair> _walkPairs = new List<WalkPair>();
        private readonly HashSet<string> _walkedToday = new HashSet<string>();
        private int _walkNextAt, _walksBegun, _walksArrived;
        private string _walkLast;

        /// From the Director: a scene played to its end, with its first two.
        private void OnPlayedOut(string kind, Ped a, Ped b)
        {
            if (_rivals != null && a != null && b != null)
                Safe("rivals", () => RivalsOver(0, a.Handle, b.Handle));   // two rivals had it out in words: her choice (FOURTEENHOUR item 19)
            if (_cfg == null || !_cfg.WalkTogether || !_cfg.MindLive || !WalkTogether.TalkKind(kind) || a == null || b == null || _store == null || _placeTown == null)
                return;
            Safe("walk together", () => WalkTogetherBegin(kind, a, b));
        }

        private void WalkTogetherBegin(string kind, Ped a, Ped b)
        {
            if (_walkPairs.Count >= WalkTogether.MostPairs || !a.Exists() || !b.Exists() || a.IsDead || b.IsDead)
                return;
            int now = Game.GameTime;
            MindWho wa = MindKnown(a.Handle, now), wb = MindKnown(b.Handle, now);
            if (wa == null || wb == null || wa.Id == null || wb.Id == null)
                return;
            long day = _store.LastGameMin / 1440;
            string pairKey = string.CompareOrdinal(wa.Id, wb.Id) < 0 ? wa.Id + "|" + wb.Id : wb.Id + "|" + wa.Id;
            if (_walkedToday.Contains(pairKey + "|" + day))
                return;
            if (PED.IS_PED_USING_ANY_SCENARIO(a.Handle) || PED.IS_PED_USING_ANY_SCENARIO(b.Handle) || MayDirectWhyNot(a) != null || MayDirectWhyNot(b) != null || NotFree(a.Handle) || NotFree(b.Handle))
                return;
            bool couple = _store.Links.Any(l => l.Kind == "courting" && ((Links.IsOf(l, wa.Id) && Links.IsOtherOf(l, wb.Id)) || (Links.IsOf(l, wb.Id) && Links.IsOtherOf(l, wa.Id))));
            int warmth = LadderWarmth(wa.Id, wb.Id);
            string seed = pairKey + "|" + day;
            if (!WalkTogether.Goes(warmth, couple, seed))
                return;
            bool bothMen = PED.IS_PED_MALE(a.Handle) && PED.IS_PED_MALE(b.Handle);
            Vector3 door;
            bool saloon = _saloonDoor.TryGetValue(_placeTown, out door);
            string where = WalkTogether.Where(CLOCK.GET_CLOCK_HOURS(), InnerLife.Weekday, bothMen, couple, saloon, seed);
            if (where == null)
                return;
            Vector3 from = a.Position, me = Game.Player.Character.Position;
            Vector3 to;
            if (where == "saloon")
                to = door;
            else
            {
                // a stroll down the street, or homeward: away from you, the way they're facing as near as can be told
                Vector3 dir = from - me;
                dir.Z = 0f;
                dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                to = from + dir * (where == "stroll" ? WalkTogether.StrollMetres : WalkTogether.HomeMetres);
            }
            if (to.DistanceTo(from) < 6f || to.DistanceTo(from) > 120f)
                return;   // already there, or too far for a walk worth watching
            Vector3 heading = to - from;
            heading.Z = 0f;
            heading = heading.Normalized;
            Vector3 side = new Vector3(-heading.Y, heading.X, 0f) * 0.55f;   // a metre apart, side by side
            _walkedToday.Add(pairKey + "|" + day);
            if (_walkedToday.Count > 200)
                _walkedToday.Clear();
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(a.Handle, to.X + side.X, to.Y + side.Y, to.Z, 1f, -1, 1.5f, 0, 0f);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(b.Handle, to.X - side.X, to.Y - side.Y, to.Z, 1f, -1, 1.5f, 0, 0f);
            var pair = new WalkPair { A = a, B = b, AName = wa.Name, BName = wb.Name, Where = where, Seed = seed, To = to, Until = now + WalkTogether.WalkMs };
            _walkPairs.Add(pair);
            _walksBegun++;
            if (_cfg.Ticker)
                _ticker.Push(Person.FirstOf(wa.Name) + ", thinking", WalkTogether.Thought(where, Person.FirstOf(wb.Name), seed), "near", a.Handle);
            _walkLast = wa.Name + " and " + wb.Name + " walk " + (where == "saloon" ? "to the saloon" : where == "stroll" ? "out together" : "homeward") + " together";
            Log.Write("WALK together: " + _walkLast + " (" + (couple ? "courting" : "friends, warmth " + warmth) + ", after a " + kind + ")");
        }

        /// While they walk: there, they turn to each other with a word and go their ways; anyone gone or taken by something else ends it.
        private void WalkTogetherStep(int now)
        {
            if (_walkPairs.Count == 0 || now < _walkNextAt)
                return;
            _walkNextAt = now + 1000;
            for (int i = _walkPairs.Count - 1; i >= 0; i--)
            {
                WalkPair p = _walkPairs[i];
                bool gone = !p.A.Exists() || !p.B.Exists() || p.A.IsDead || p.B.IsDead;
                if (gone || _director.Involves(p.A.Handle) || _director.Involves(p.B.Handle) || PED.IS_PED_IN_COMBAT(p.A.Handle, 0) || PED.IS_PED_IN_COMBAT(p.B.Handle, 0) || PED.IS_PED_FLEEING(p.A.Handle))
                {
                    _walkPairs.RemoveAt(i);   // left as they are: something else has them
                    continue;
                }
                bool there = p.A.Position.DistanceTo(p.To) < WalkTogether.ArriveMetres && p.B.Position.DistanceTo(p.To) < WalkTogether.ArriveMetres;
                if (!there && now < p.Until)
                    continue;
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.A.Handle, p.B.Handle, 1500, 0f, 0f, 0f);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.B.Handle, p.A.Handle, 1500, 0f, 0f, 0f);
                if (there && p.A.Position.DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres && _cfg.ScenesOnTicker && _cfg.Ticker)
                    SpeechCaption(Person.FirstOf(p.AName), WalkTogether.Parting(p.Where, p.Seed), p.A.Handle, 2000);
                WanderLater(p.A.Handle, now + 2000);
                WanderLater(p.B.Handle, now + 2600);
                if (there)
                    _walksArrived++;
                Log.Detail("WALK together: " + p.AName + " and " + p.BName + (there ? " got there, and part" : " never quite got there; they part"));
                _walkPairs.RemoveAt(i);
            }
        }

        /// Walking together just now (NotFree: nothing else of ours takes them meanwhile).
        private bool WalkingTogether(int h)
        {
            for (int i = 0; i < _walkPairs.Count; i++)
                if (_walkPairs[i].A.Handle == h || _walkPairs[i].B.Handle == h)
                    return true;
            return false;
        }

        private string WalkTogetherStatus()
        {
            if (!_cfg.WalkTogether)
                return "off ([Mind] WalkTogether)";
            return _walksBegun + " pairs walked together this session, " + _walksArrived + " got where they were going; " + _walkPairs.Count + " walking now" + (_walkLast != null ? "; last: " + _walkLast : "");
        }
    }
}
