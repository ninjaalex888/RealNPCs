// The game's own encounters, noticed, the game side (FOURTEENHOUR reserve R8; Core/GameEncounters.cs; [World] GameEncounters). A look
// every 3 s at the people within 30 m of you for the game's own encounter models (re_..., read by name, never touched): the first one
// of a kind starts a minute's watch; at its end (or once its people are gone), if the kind has a phrase of ours, the nearest person
// the mind knows who isn't one of the encounter's own keeps it in mind and thinks of it, and the nearest town hears of it the next
// day, with whether you stopped by it (40 s of the minute within 30 m) or rode on. A kind once a day. The Testing row "owners" asks,
// for the mission-entity people near you, which script owns them, and for one of ours and one of enRAGE's what
// DOES_ENTITY_BELONG_TO_THIS_SCRIPT says (log only; nothing in a default path calls either native).
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
        private sealed class EncounterWatch
        {
            public string Kind, Phrase;
            public Vector3 At;
            public int Started, NearMs, LastLook;
            public readonly HashSet<int> Peds = new HashSet<int>();
        }

        private EncounterWatch _encWatch;
        private readonly HashSet<string> _encSeenToday = new HashSet<string>();   // "kind|day"
        private int _encLookAt, _encSeen, _encWitnessed, _encTold;
        private string _encLast;

        /// From the scenes' helpers: a look every 3 s, and the watch.
        private void EncounterLook(int now)
        {
            if (!GameEncounters.On || now < _encLookAt || _store == null)
                return;
            _encLookAt = now + 3000;
            Ped me = Game.Player.Character;
            long day = _store.LastGameMin / 1440;
            EncounterWatch w = _encWatch;
            if (w != null)
            {
                bool any = false;
                foreach (int h in w.Peds)
                    if (ENTITY.DOES_ENTITY_EXIST(h))
                        any = true;
                if (me.Position.DistanceTo(w.At) <= GameEncounters.SeenMetres)
                    w.NearMs += now - w.LastLook;
                w.LastLook = now;
                if (any && now - w.Started < GameEncounters.WatchMs)
                    return;
                _encWatch = null;
                EncounterOver(w, me, day);
                return;
            }
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || p.Position.DistanceTo(me.Position) > GameEncounters.SeenMetres)
                    continue;
                string kind = GameEncounters.KindOf(Natives.ModelName(p));
                if (kind == null || _encSeenToday.Contains(kind + "|" + day))
                    continue;
                _encSeenToday.Add(kind + "|" + day);
                if (_encSeenToday.Count > 200)
                    _encSeenToday.Clear();
                _encSeen++;
                string phrase = GameEncounters.PhraseOf(kind);
                if (phrase == null)
                {
                    Log.Detail("ENCOUNTER the game's own " + kind + " near you: left out on purpose");
                    return;
                }
                var ew = new EncounterWatch { Kind = kind, Phrase = phrase, At = p.Position, Started = now, LastLook = now };
                foreach (Ped q in Peds.All())
                    if (q != null && q.Exists() && q.Position.DistanceTo(p.Position) < 25f && GameEncounters.KindOf(Natives.ModelName(q)) == kind)
                        ew.Peds.Add(q.Handle);
                _encWatch = ew;
                Log.Write("ENCOUNTER the game's own " + kind + " near you (" + ew.Peds.Count + " of its people): watched a minute");
                return;
            }
        }

        /// The watch is over: a witness of the mind's keeps it, and the nearest town hears of it tomorrow.
        private void EncounterOver(EncounterWatch w, Ped me, long day, bool test = false)
        {
            bool stayed = w.NearMs >= GameEncounters.StayedMs;
            string town = _bridge != null ? _bridge.TownAt(w.At) : _placeTown;
            Ped witness = null;
            MindWho ww = null;
            float best = GameEncounters.SeenMetres + 1f;
            foreach (int h in _mindNear)
            {
                MindWho m;
                if (!_mindWho.TryGetValue(h, out m) || m.Id == null || w.Peds.Contains(h) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                Ped p = new Ped(h);
                float d = p.Position.DistanceTo(w.At);
                if (d < best)
                {
                    best = d;
                    witness = p;
                    ww = m;
                }
            }
            string seed = w.Kind + "|" + day;
            if (witness != null && ww != null)
            {
                PersonState st = test ? null : _store.Find(ww.Id);   // (a test keeps nothing)
                if (st != null)
                {
                    if (st.Mind == null)
                        st.Mind = new List<MindMemory>();
                    Minds.Note(st.Mind, new MindMemory { Kind = "talk", Sub = "encounter", How = "saw", Names = new List<string> { w.Phrase }, Town = town, At = _store.LastGameMin, Feel = -1, Strength = 40,
                                                         Key = "enc:" + seed }, _store.LastGameMin);
                }
                _ticker.Aside(Person.FirstOf(ww.Name ?? "Someone") + ", thinking", GameEncounters.Witness(w.Phrase, seed + "|" + ww.Id), "enc|" + ww.Id, witness.Handle, 8000, Game.GameTime);
                _encWitnessed++;
            }
            if (!test)
            {
                _engine.WaysideHeard(GameEncounters.News(w.Phrase, town, stayed, seed), town ?? _placeTown, seed + "|news", _store.LastGameMin);
                _encTold++;
            }
            _encLast = w.Kind + (stayed ? ", you stopped" : ", you rode on") + (ww != null ? "; " + ww.Name + " saw it" : "; nobody of ours saw it");
            Log.Write("ENCOUNTER over: " + _encLast + "; " + (town ?? "the nearest town") + " hears of it tomorrow");
        }

        /// The Testing row: which script owns the mission-entity people near you, and what DOES_ENTITY_BELONG_TO_THIS_SCRIPT says
        /// for one of ours and one of enRAGE's. The log only.
        private unsafe string OwnersProbe(out string said)
        {
            said = null;
            Ped me = Game.Player.Character;
            int asked = 0;
            int ours = 0, theirs = 0;
            foreach (Ped p in Peds.All().Where(x => x != null && x.Exists() && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 30f).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                int h = p.Handle;
                if (ours == 0 && Spawner.IsOurs(h))
                    ours = h;
                if (theirs == 0 && IsRecord(h))
                    theirs = h;
                if (asked >= 12 || !ENTITY.IS_ENTITY_A_MISSION_ENTITY(h))
                    continue;
                asked++;
                try
                {
                    ulong arg = 0;
                    uint script = ENTITY._GET_ENTITY_SCRIPT(h, &arg);
                    Log.Write("OWNERS " + (NameToShow(h) ?? Natives.ModelName(p)) + " (" + Natives.ModelName(p) + ", " + (int)p.Position.DistanceTo(me.Position) + " m): script 0x" + script.ToString("X8"));
                }
                catch (Exception ex)
                {
                    Log.Write("OWNERS " + Natives.ModelName(p) + ": couldn't be asked (" + ex.Message + ")");
                }
            }
            foreach (int h in new[] { ours, theirs })
            {
                if (h == 0)
                    continue;
                try
                {
                    Log.Write("OWNERS " + (h == ours ? "one of ours" : "one of enRAGE's") + " (" + Natives.ModelName(new Ped(h)) + "): DOES_ENTITY_BELONG_TO_THIS_SCRIPT " + ENTITY.DOES_ENTITY_BELONG_TO_THIS_SCRIPT(h, false));
                }
                catch (Exception ex)
                {
                    Log.Write("OWNERS belong-to-this-script couldn't be asked: " + ex.Message);
                }
            }
            said = asked + " mission-entity people asked (OWNERS lines in the log)" + (ours != 0 ? "; one of ours" : "") + (theirs != 0 ? "; one of enRAGE's" : "");
            return null;
        }

        /// The Testing row: as if a man bitten by a snake were by you now and you'd stopped: the witness's thought (nothing kept, no news).
        private string EncounterTest(out string said)
        {
            said = null;
            if (!GameEncounters.On)
                return "off ([World] GameEncounters)";
            Ped me = Game.Player.Character;
            var w = new EncounterWatch { Kind = "snakebite", Phrase = GameEncounters.PhraseOf("snakebite"), At = me.Position, Started = Game.GameTime, NearMs = GameEncounters.StayedMs };
            EncounterOver(w, me, _store.LastGameMin / 1440, true);
            said = "a snakebite by you, as if seen: " + (_encLast ?? "");
            return null;
        }

        /// F7: the game's encounters this session.
        private string GameEncountersStatus()
        {
            if (!GameEncounters.On)
                return "off ([World] GameEncounters)";
            return _encSeen + " of the game's own encounters seen near you, " + _encWitnessed + " with a witness of ours, " + _encTold + " told in the towns" + (_encWatch != null ? "; watching " + _encWatch.Kind : "") +
                   (_encLast != null ? "; last: " + _encLast : "");
        }
    }
}
