// The town's regard of you in the "Around town" box ([Display] TownRegard; Core/TownRegard.cs; Lex, 2026-11-04). Every twenty
// seconds in a town: the standing the town gives you this week with its counts (Engine.StandingOf: the known killings of
// yours against your kindnesses), the town's mood (the ledger), how many you've met whose town this is, how the ones about
// town now (within 120 m) regard you (the sights' regard: afraid, fond, holding something against you), and the latest talk
// that has reached the town about you.
// A line under the box's title whenever the box is up while you're in a town; an item in the box when you come into a
// town and whenever the standing, the counts or the mood change (no sooner than TownRegard.PushGapMinutes after the last,
// unless the town changed). F7 "TEST regard:".
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private const int RegardEveryMs = 20000;
        private const float RegardMetres = 120f;   // about town (Lex, 2026-11-04: the count within 40 m read too small)
        private const int RegardLookMost = 80;
        private string _regardLine, _regardKey, _regardTown, _regardLast;   // _regardLast: the whole of the latest item (the standing box shows it)
        private int _nextRegard, _regardPushed;
        private long _regardPushedAt;
        private Action _aRegard;

        private void RegardStep(int now)
        {
            if (_cfg == null || !_cfg.TownRegard || _store == null || _engine == null || now < _nextRegard)
            {
                if (_cfg != null && !_cfg.TownRegard)
                    _regardLine = null;
                return;
            }
            _nextRegard = now + RegardEveryMs;
            if (_aRegard == null)
                _aRegard = () => RegardLook(_now);
            Safe("regard", _aRegard);
        }

        private void RegardLook(int now)
        {
            string town = _placeTown;
            if (town == null)
            {
                _regardLine = null;
                _regardTown = null;
                return;
            }
            long g = Bridge.Now();
            int kills, kindnesses;
            string standing = _engine.StandingOf(town, g, out kills, out kindnesses);
            TownLedger ledger = _engine.LedgerOf(town);
            string mood = ledger != null ? ledger.Mood : null;
            _regardLine = TownRegard.Line(town, standing, kills, kindnesses);
            string key = TownRegard.Key(town, standing, kills, kindnesses, mood);
            bool newTown = town != _regardTown;
            if (key == _regardKey || (!newTown && g - _regardPushedAt < TownRegard.PushGapMinutes))
                return;
            // everyone you've met whose town this is (the store), and the ones about town now with how they regard you (the sights'
            // regard: once each per push, not per frame)
            int met = 0;
            foreach (PersonState ps in _store.People.Values)
                if (ps != null && ps.FirstSeenTown == town && Knowing.Met(ps))
                    met++;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            int known = 0, afraid = 0, fond = 0, against = 0, looked = 0;
            foreach (Ped p in Peds.All())
                {
                    if (looked >= RegardLookMost)
                        break;
                    int h = p.Handle;
                    if (h == me.Handle || !p.Exists() || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || p.Position.DistanceTo(me.Position) > RegardMetres)
                        continue;
                    MindWho w = MindKnown(h, now);
                    if (w == null || w.Id == null)
                        continue;
                    looked++;
                    PersonState st = _store.Find(w.Id);
                    if (st == null || !Knowing.Met(st))
                        continue;
                    known++;
                    string regard = SightRegard(p, w.Id);
                    if (regard == "afraid") afraid++;
                    else if (regard == "hostile") against++;
                    else if (regard == "fond") fond++;
                }
            // the latest talk that has reached this town about you
            string talk = null;
            long talkAt = -1;
            foreach (TownRumour r in _store.TownRumours)
                if (r != null && r.Town == town && r.Text != null && r.At > talkAt)
                {
                    talkAt = r.At;
                    talk = r.Text;
                }
            string item = TownRegard.Item(town, standing, kills, kindnesses, mood, met, known, afraid, fond, against, talk);
            _regardKey = key;
            _regardTown = town;
            _regardPushedAt = g;
            _regardPushed++;
            _regardLast = item;
            NewsPush("What " + town + " makes of you", item, "regard");
            Log.Write("REGARD " + town + ": " + item);
        }

        private string RegardStatus()
        {
            if (_cfg == null || !_cfg.TownRegard)
                return "off ([Display] TownRegard)";
            return (_regardLine ?? "no town about you") + "; " + _regardPushed + " items this session" + (_regardLast != null ? "; last: " + _regardLast : "");
        }
    }
}
