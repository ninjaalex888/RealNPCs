// Two who think about each other, in the game (TENHOUR item 14; Core/PairThoughts.cs; [Ticker] PairThoughts). Every few seconds, the ties between people in view are looked over; two with history who are near each other get a box each, about the other, beside whatever else is up.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _pairNextAt, _pairsShown;
        private bool _pairForce;   // the Testing row: no roll and no gap, this once
        private string _pairLast;
        private readonly Dictionary<string, NearThought> _pairNear = new Dictionary<string, NearThought>(StringComparer.Ordinal);

        /// From the thought board's update, a few times a second; finds a pair at most every few seconds.
        private void PairThoughtsStep(int now)
        {
            if (!_cfg.PairThoughts || _ticker.Several < 3 || (now < _pairNextAt && _pairNextAt - now <= PairThoughts.GapMs && !_pairForce))
                return;
            _pairNextAt = now + PairThoughts.LookMs;
            _pairNear.Clear();
            for (int i = 0; i < _near.Count; i++)
            {
                NearThought n = _near[i];
                if (n.Key != null && n.Handle != 0 && !n.Key.StartsWith("road:", StringComparison.Ordinal) && !n.Key.StartsWith("stranger:", StringComparison.Ordinal) && !_ticker.Waiting(n.Handle))
                    _pairNear[n.Key] = n;
            }
            if (_pairNear.Count < 2)
                return;
            foreach (SceneTie t in _store.Ties)
            {
                NearThought a, b;
                if (t.A == null || t.B == null || !_pairNear.TryGetValue(t.A, out a) || !_pairNear.TryGetValue(t.B, out b) || !ENTITY.DOES_ENTITY_EXIST(a.Handle) || !ENTITY.DOES_ENTITY_EXIST(b.Handle))
                    continue;
                if (ENTITY.GET_ENTITY_COORDS(a.Handle, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(b.Handle, true, true)) > PairThoughts.Metres)
                    continue;
                string seed = t.A + "|" + t.B + "|" + now / 60000;
                if (!_pairForce && !Voice.Chance(seed + "|chance", PairThoughts.Chance))
                    continue;
                PairShow(t, a, b, seed, now);
                return;
            }
        }

        private void PairShow(SceneTie t, NearThought a, NearThought b, string seed, int now)
        {
            Someone wa = SomeoneOf(new Ped(a.Handle)), wb = SomeoneOf(new Ped(b.Handle));
            string va = OpinionOf(t, t.A, wa, b.Handle), vb = OpinionOf(t, t.B, wb, a.Handle);   // their views of each other (Core/Opinions.cs)
            string la = (va != null ? Opinions.Line(va, Person.FirstOf(t.BName), seed + "a") : null) ?? PairThoughts.Line(t, t.A, wa != null ? wa.Temper : null, seed + "a");
            string lb = (vb != null ? Opinions.Line(vb, Person.FirstOf(t.AName), seed + "b") : null) ?? PairThoughts.Line(t, t.B, wb != null ? wb.Temper : null, seed + "b");
            _pairForce = false;
            if (la == null || lb == null || !_ticker.Aside(a.Name + ", thinking", la, a.Key, a.Handle, PairThoughts.ForMs, now) || !_ticker.Aside(b.Name + ", thinking", lb, b.Key, b.Handle, PairThoughts.ForMs, now))
                return;
            _pairNextAt = now + PairThoughts.GapMs;
            _pairsShown++;
            NoteTheirThought(a.Handle, now, la);
            NoteTheirThought(b.Handle, now, lb);
            _pairLast = a.Name + (va != null ? " (" + va + ")" : "") + ": " + la + " / " + b.Name + (vb != null ? " (" + vb + ")" : "") + ": " + lb;
            Log.Write("PAIR " + _pairLast);
        }

        /// This one's view of the other, seven times in ten when they have one; null for the tie's plain thought.
        private string OpinionOf(SceneTie t, string id, Someone me, int otherHandle)
        {
            if (!_cfg.Opinions || t == null || id == null)
                return null;
            Resident mine = _store.FindResident(id), theirs = _store.FindResident(t.OtherId(id));
            bool iGentry = mine != null && InnerLife.TradeGroup(mine.Trade) == "gentry", otherGentry = theirs != null && InnerLife.TradeGroup(theirs.Trade) == "gentry";
            bool otherDrunk = ENTITY.DOES_ENTITY_EXIST(otherHandle) && InHisCups(new Ped(otherHandle), null);
            string v = Opinions.View(t, id, me != null ? me.Temper : null, iGentry, otherGentry, otherDrunk);
            return v != null && Voice.Chance(t.A + "|" + t.B + "|" + id + "|view|" + _store.LastGameMin / 60, Opinions.ShownChance) ? v : null;
        }

        /// The Testing row.
        private string PairTest(int now, out string said)
        {
            said = null;
            if (!_cfg.PairThoughts || _ticker.Several < 3)
                return "[Ticker] PairThoughts is off, or ThoughtsAtOnce is under 3";
            _pairForce = true;
            _pairNextAt = 0;
            said = "the next two in view with a tie between them, within " + (int)PairThoughts.Metres + " m of each other";
            return null;
        }

        private string PairStatus()
        {
            return (_cfg.PairThoughts ? "on" : "off ([Ticker] PairThoughts)") + ", " + _pairsShown + " pairs this session" + (_pairLast != null ? "; last: " + _pairLast : "");
        }
    }
}
