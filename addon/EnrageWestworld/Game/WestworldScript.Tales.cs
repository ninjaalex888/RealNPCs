// The story grows in the telling, the game side (FOURTEENHOUR item 24; Core/Tales.cs; [Mind] MemoriesBlur). A big brawl's aftermath
// (WestworldScript.BrawlAfter.cs) leaves its tale in the save: how many, how many laid out, where and when, who started it (the fight's
// first man), and who was in it and who was hurt (ids, the eyewitnesses). Told on in the town's talk (the engine's rumour pool, for the
// gossip scenes) and here, in the thoughts of those near you: a look every 5 s, one thought every 90 s at most, each person once a
// tale; one who was in it tells it nearer the truth, one who holds someone in contempt may put the starting of it on him (item 23).
// A Testing row leaves a test tale here (a brawl two days back) to see it told. Never a frame.
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private string _afterStarter;
        private readonly HashSet<string> _taleTold = new HashSet<string>();
        private int _taleLookAt, _taleShowAt, _talesNoted, _talesThought;
        private string _taleLast;

        /// From BrawlWeekAfter, for a big one: its tale, into the save.
        private void TaleNote(int fid, int loser)
        {
            if (!Tales.On || _store == null || _placeTown == null)
                return;
            int now = Game.GameTime;
            var were = new List<string>();
            var hurt = new List<string>();
            foreach (int h in _afterSide.Keys)
            {
                MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, now) : null;
                if (w == null || w.Id == null)
                    continue;
                were.Add(w.Id);
                if (_brawlOut.ContainsKey(h) || _brawlFatal.Contains(h) || h == loser)
                    hurt.Add(w.Id);
            }
            TownTale t = Tales.Brawl(_placeTown, _afterSaloon, _afterPeak, _afterDowns, _afterStarter != null ? Person.FirstOf(_afterStarter) : null, _store.LastGameMin, CLOCK.GET_CLOCK_HOURS(), were, hurt);
            _store.Tales.Add(t);
            while (_store.Tales.Count > Tales.Kept)
                _store.Tales.RemoveAt(0);
            _talesNoted++;
            Log.Write("TALE noted: a brawl of " + t.Count + " at " + (t.Saloon ? "the saloon" : "the street") + " in " + t.Town + ", " + t.Down + " laid out" + (t.Starter != null ? ", " + t.Starter + " began it" : "") +
                      "; " + were.Count + " who were in it will tell it truest");
        }

        private System.Action _aTale;

        /// A look every 5 s: one near you who knows of a live tale in this town thinks of it, every 90 s at most.
        private void TaleStep(int now)
        {
            if (!Tales.On || _store == null || _placeTown == null || now < _taleLookAt || _store.Tales.Count == 0)
                return;
            _taleLookAt = now + 5000;
            if (_aTale == null)
                _aTale = () => TaleLook(_now);
            Safe("tales", _aTale);   // (a cached action: nothing made a frame)
        }

        private void TaleLook(int now)
        {
            if (now < _taleShowAt)
                return;
            long g = _store.LastGameMin;
            TownTale t = _store.Tales.Where(x => x.Town == _placeTown && Tales.Live(x, g)).OrderByDescending(x => x.At).FirstOrDefault();
            if (t == null)
                return;
            Ped me = Game.Player.Character;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || p.Position.DistanceTo(me.Position) > 20f || !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) || _director.Involves(p.Handle))
                    continue;
                MindWho w;
                if (!_mindWho.TryGetValue(p.Handle, out w) || w.Id == null || w.Name == null || !_taleTold.Add(w.Id + "|" + t.Id))
                    continue;
                bool was = t.Were != null && t.Were.Contains(w.Id), hurt = t.Hurt != null && t.Hurt.Contains(w.Id);
                List<Opinions.Held> views = Opinions.Act ? ViewsHeldBy(w.Id) : null;
                Opinions.Held scorned = views != null ? views.FirstOrDefault(v => v.View == "contempt") : null;
                string text = Tales.Telling(t, g, was, hurt, scorned != null ? Person.FirstOf(scorned.Name) : null, t.Id + "|" + w.Id);
                if (text == null || !_ticker.Aside(Person.FirstOf(w.Name) + ", thinking", text, "tale|" + w.Id, p.Handle, 8000, now))
                    continue;
                if (!was)
                    t.Told++;   // passed on once more: it grows (the eyewitness's own never does)
                _taleShowAt = now + Tales.ThoughtGapMs;
                _talesThought++;
                _taleLast = w.Name + (was ? " (who was in it)" : "") + ": " + text;
                Log.Write("TALE told: " + _taleLast);
                return;
            }
        }

        /// The Testing row: a test tale here, a brawl of six two days back, to see it told.
        private string TaleTestFire(int now, out string said)
        {
            said = null;
            if (!Tales.On)
                return "off ([Mind] MemoriesBlur)";
            if (_store == null || _placeTown == null)
                return "it wants a town (there's none here)";
            _store.Tales.Add(Tales.Brawl(_placeTown, _placeKind == "saloon", 6, 2, "Abe", _store.LastGameMin - 2 * 1440, 21, new List<string>(), new List<string>()));
            _taleShowAt = 0;
            _taleTold.Clear();
            said = "a test tale in " + _placeTown + ": six men two days back; those near you will tell it, grown";
            return null;
        }

        /// F7: the tales this session.
        private string TalesStatus()
        {
            if (!Tales.On)
                return "off ([Mind] MemoriesBlur)";
            long g = _store != null ? _store.LastGameMin : 0;
            int live = _store != null ? _store.Tales.Count(x => Tales.Live(x, g)) : 0;
            return live + " tales live in the save, " + _talesNoted + " noted this session, " + _talesThought + " thought of near you" + (_taleLast != null ? "; last: " + _taleLast : "");
        }
    }
}
