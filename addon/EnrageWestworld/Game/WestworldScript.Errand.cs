// Carrying a word (SIXDAY item 1; Core/Fondness.cs has the choosing and the words): the first errand an NPC gives you.
// Talk on someone with a quarrel still sore (a tie gone cold with a named person of the same town, or sweethearts fallen
// out) offers "Any word you'd have carried to <name>?"; by their nature they give you one ("Tell him I was in the wrong. I'll
// not say it to his face."), and the errand is yours for two game days: the other wears a dot on the map while they're
// near you, Talk on them offers the word, and by their nature they take it (the quarrel mended: a makeup tie and the
// memory of it for both) or send it back ("Let him come and say it himself"). Either way both warm to you for carrying
// it. Why it works: pride stops a man apologising to a face; a go-between lets him do it without losing it, and the one
// who carries a peace is owed by both sides. One errand at a time, kept in memory only (a reload or a wipe forgets it:
// nothing new in the save). F7 in "TEST fondness:"; log "FOND errand".
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
        private sealed class Errand
        {
            public string FromId, FromName, ToId, ToName, Town, Message;
            public bool Sweethearts;
            public long StartedMin;
            public int ScanAt, ToHandle, DotTriedFor;   // DotTriedFor: the body a dot was tried on (one wearing another's dot gets none, once)
            public Blip Dot;
        }

        private Errand _errand;

        // they meet (DEPTH reserve R2): the two a word went between, for two game days after it was taken or sent back; in memory
        private sealed class NoteMeetFor
        {
            public string A, AName, B, BName;
            public bool Mended;
            public long Until;
        }
        private readonly List<NoteMeetFor> _noteMeets = new List<NoteMeetFor>();
        private Action _aNoteMeet;
        private int _noteMeetNextLook, _noteMet;
        private string _noteMetLast;

        private void NoteMeetStep(int now)
        {
            if (!NoteMeet.On || _noteMeets.Count == 0 || now < _noteMeetNextLook)
                return;
            _noteMeetNextLook = now + 2000;
            Safe("note meet", _aNoteMeet ?? (_aNoteMeet = () => NoteMeetLook(_now)));
        }

        /// The two near each other (within 12 m, both in the near scan, free, nobody's scene) and the Director free: they meet.
        private void NoteMeetLook(int now)
        {
            long g = _store.LastGameMin;
            _noteMeets.RemoveAll(m => g > m.Until || g < m.Until - NoteMeet.KeepMinutes);   // done with, or a clock gone back
            if (_noteMeets.Count == 0 || _director.Busy || _placeTown == null)
                return;
            foreach (NoteMeetFor m in _noteMeets)
            {
                int ha = 0, hb = 0;
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null)
                        continue;
                    if (w.Id == m.A) ha = h;
                    else if (w.Id == m.B) hb = h;
                }
                if (ha == 0 || hb == 0 || !ENTITY.DOES_ENTITY_EXIST(ha) || !ENTITY.DOES_ENTITY_EXIST(hb))
                    continue;
                Ped pa = new Ped(ha), pb = new Ped(hb), me = Game.Player.Character;
                if (pa.Position.DistanceTo(pb.Position) > NoteMeet.NearMetres || PED.IS_PED_USING_ANY_SCENARIO(ha) || PED.IS_PED_USING_ANY_SCENARIO(hb) || NotFree(ha) || NotFree(hb) ||
                    MayDirectWhyNot(pa) != null || MayDirectWhyNot(pb) != null || PED.IS_PED_IN_COMBAT(ha, 0) || PED.IS_PED_IN_COMBAT(hb, 0))
                    continue;
                string seed = m.A + "|" + m.B + "|meet";
                SceneCast cast = BuildCast(pa, pb, null, _placeKind == "saloon", me, now, seed);
                Scene s = NoteMeet.Make(cast, m.Mended, seed);
                if (!_director.Start(s, new List<Ped> { pa, pb }, cast.Names, seed, () => { }, cast))
                    return;
                _noteMeets.Remove(m);
                _noteMet++;
                _noteMetLast = m.AName + " and " + m.BName + (m.Mended ? " shook hands" : " parted cold");
                Log.Write("NOTE meet: " + m.AName + " and " + m.BName + " meet after the word you carried: " + (m.Mended ? "a hand" : "a cold word"));
                return;
            }
        }

        private string NoteMeetStatus()
        {
            return !NoteMeet.On ? "off ([Scenes] NoteMeet)" : _noteMeets.Count + " pairs waiting to meet, " + _noteMet + " met" + (_noteMetLast != null ? " (the last: " + _noteMetLast + ")" : "");
        }
        private int _errands, _errandsDelivered, _errandsLapsed;
        private string _errandLast;

        /// They gave you a word to carry (TalkAnswer, Then "errand").
        private void ErrandStart(TalkMenu t, InteractAnswer a, int now)
        {
            if (_errand != null || t == null || t.ErrandTie == null || string.IsNullOrEmpty(a.Message))
                return;
            SceneTie tie = t.ErrandTie;
            string toId = tie.A == t.Id ? tie.B : tie.A, toName = tie.Other(t.Id);
            _errand = new Errand { FromId = t.Id, FromName = t.Name, ToId = toId, ToName = toName, Town = tie.Town ?? _placeTown, Message = a.Message, Sweethearts = tie.Courted, StartedMin = _store.LastGameMin };
            _errands++;
            _errandLast = "a word from " + Person.FirstOf(t.Name) + " to " + Person.FirstOf(toName);
            Hint("A word to carry to " + Person.FirstOf(toName) + ": Talk to them when you find them (two days).", 5000);
            Log.Write("FOND errand: " + t.Name + " gives you a word for " + toName + " (\"" + a.Message + "\"; " + (tie.Courted ? "sweethearts fallen out" : "a quarrel, warmth " + tie.Warmth) + ")");
        }

        /// Tick04, every two seconds while a word is being carried: the lapse, and the one it's for found and marked while near.
        private void ErrandStep(int now)
        {
            Errand e = _errand;
            if (e == null)
                return;
            if (_featureOff.Contains("errand"))
            {
                ErrandAbort();   // Safe switched the errand off after errors: the deliver row doesn't stay up for good (the review)
                return;
            }
            if (now < e.ScanAt)
                return;
            e.ScanAt = now + 2000;
            Safe("errand", () => ErrandStepImpl(e, _now));
        }

        private void ErrandStepImpl(Errand e, int now)
        {
            if (!ReferenceEquals(e, _errand))
                return;
            long g = _store.LastGameMin;
            if (g - e.StartedMin > Fondness.ErrandDays * 1440L || _store.DeathOf(e.ToId) != null)
            {
                ErrandLapse(e, _store.DeathOf(e.ToId) != null ? e.ToName + " is dead" : "two days went by");
                return;
            }
            Ped me = Game.Player.Character;
            int found = 0;
            if (e.ToHandle != 0 && ENTITY.DOES_ENTITY_EXIST(e.ToHandle) && !ENTITY.IS_ENTITY_DEAD(e.ToHandle))
            {
                string id, name;
                if (LastingId(new Ped(e.ToHandle), out id, out name) && id == e.ToId && new Ped(e.ToHandle).Position.DistanceTo(me.Position) < 120f)
                    found = e.ToHandle;
            }
            if (found == 0)
            {
                Vector3 at = me.Position;
                foreach (Ped p in Peds.All())
                {
                    if (p == null || !p.Exists() || p.Handle == me.Handle || p.IsDead || p.Position.DistanceTo(at) > 80f || !PED.IS_PED_HUMAN(p.Handle))
                        continue;
                    string id, name;
                    if (LastingId(p, out id, out name) && id == e.ToId)
                    {
                        found = p.Handle;
                        break;
                    }
                }
            }
            if (found != e.ToHandle || (found != 0 && e.Dot == null && e.DotTriedFor != found))
            {
                ErrandDotRemove(e);
                e.ToHandle = found;
                if (found != 0)
                {
                    e.DotTriedFor = found;
                    e.Dot = GreenDot(found, Person.FirstOf(e.ToName) + ": a word to carry");   // the friends' small dot (WestworldScript.FriendDots.cs); null when they wear one already (tried once: the review)
                    Log.Detail("FOND errand: " + e.ToName + " is near (" + (int)new Ped(found).Position.DistanceTo(me.Position) + " m)");
                }
            }
        }

        private void ErrandDotRemove(Errand e)
        {
            if (e.Dot != null)
            {
                try
                {
                    if (e.Dot.Exists())
                        e.Dot.Delete();
                }
                catch
                {
                }
                e.Dot = null;
            }
        }

        /// The one the word is for, talked to: Talk's list carries it (TalkOpen).
        private bool ErrandFor(string id, out Errand e)
        {
            e = _errand;
            return e != null && id != null && e.ToId == id;
        }

        /// The word delivered (TalkAnswer): by their answer the quarrel is mended (a makeup tie and the memory of it for
        /// both, the town's talk) or not; either way both think better of you for carrying it.
        private void ErrandDelivered(TalkMenu t, InteractAnswer a, int now)
        {
            Errand e = _errand;
            if (e == null || t == null || t.Id != e.ToId)
                return;
            _errand = null;
            ErrandDotRemove(e);
            long g = _store.LastGameMin;
            Ped me = Game.Player.Character;
            bool madeUp = a.Then == "makeup";
            string town = e.Town ?? _placeTown;
            if (madeUp)
            {
                SceneTie tie = Ties.Note(_store.Ties, e.FromId, e.FromName, e.ToId, e.ToName, town, "makeup", g);
                if (tie != null)
                    Log.Detail("TIE " + tie.AName + " & " + tie.BName + ": makeup (a word carried)");
                _engine.RememberScene("makeup", new Minds.Party { Id = e.FromId, Name = e.FromName, Home = town }, new Minds.Party { Id = e.ToId, Name = e.ToName, Home = town }, null, town, null);
            }
            // the one who sent it: fonder of you, and the memory; their handle may be anywhere (the log gets the position of the one you told)
            RecordEvent("help", "player", "you", e.FromId, e.FromName, ENTITY.GET_ENTITY_COORDS(t.Handle, true, true), me.Handle, me.Handle, t.Handle, Fondness.ErrandOutcome);
            _engine.ErrandCarried(e.FromId, e.FromName, e.ToId, e.ToName, g, madeUp);
            // the one you told: their answer's own LikeDelta (Fondness.Deliver) went through TalkAnswer; not twice (the review)
            string fromAcq = FolkIdOf(e.FromId);
            if (fromAcq != null && _bridge != null)
                _bridge.LikeMore(fromAcq, madeUp ? Fondness.ErrandLiking : Fondness.ErrandLiking / 2);   // the sender, one of enRAGE's townsfolk, by id (their body may be anywhere)
            if (NoteMeet.On)
            {
                if (_noteMeets.Count > 20)
                    _noteMeets.RemoveAt(0);
                _noteMeets.Add(new NoteMeetFor { A = e.FromId, AName = e.FromName, B = e.ToId, BName = e.ToName, Mended = madeUp, Until = g + NoteMeet.KeepMinutes });   // they meet (reserve R2)
            }
            _errandsDelivered++;
            _errandLast = e.ToName + (madeUp ? " took " : " sent back ") + Person.FirstOf(e.FromName) + "'s word";
            _fondLast = _errandLast;
            Log.Write("FOND errand: you gave " + e.ToName + " " + e.FromName + "'s word; " + (madeUp ? "the quarrel's mended" : "they'll not take it from a go-between") + "; both warm to you for it");
        }

        private void ErrandLapse(Errand e, string why)
        {
            if (!ReferenceEquals(e, _errand))
                return;
            _errand = null;
            ErrandDotRemove(e);
            _errandsLapsed++;
            _errandLast = "lapsed: " + why;
            Log.Write("FOND errand: " + e.FromName + "'s word for " + e.ToName + " was never carried (" + why + ")");
            // the sender, when near: the sting
            Ped me = Game.Player.Character;
            foreach (Ped p in Peds.All())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Position.DistanceTo(me.Position) > 25f)
                    continue;
                string id, name;
                if (LastingId(p, out id, out name) && id == e.FromId)
                {
                    _ticker.Push(Person.FirstOf(name) + ", thinking", Fondness.ErrandLapsedThought(e.FromId + "|" + e.StartedMin), "near", p.Handle);
                    break;
                }
            }
        }

        private void ErrandAbort()
        {
            if (_errand != null)
            {
                ErrandDotRemove(_errand);
                _errand = null;
            }
        }

        private string ErrandStatus()
        {
            return _errands + " words given you, " + _errandsDelivered + " delivered, " + _errandsLapsed + " lapsed" +
                   (_errand != null ? "; now: " + Person.FirstOf(_errand.FromName) + "'s word for " + _errand.ToName + (_errand.ToHandle != 0 ? " (near)" : " (not met yet)") : "") + (_errandLast != null ? "; last: " + _errandLast : "");
        }
    }
}
