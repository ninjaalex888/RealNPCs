// Find the letter, the game side (SEAMS item 18; Core/FindLetter.cs; [Scenes] FindTheLetter). Once a second in a town on foot with
// J free: within 4 m of one you carry a letter for (his "letterheld" mark), J offers to give it over; else within 4 m of a station
// agent (Posts' kind "station"), when a resident you've met in another town is at his letter worry gone worse or at its crisis and
// his letter is held at this depot (FindLetter.HeldAt), J offers to take it. Taken: the agent's word, a notice, the mark. Given: he
// reads it aloud (the "relieved" voice), his worry is settled for the rest of its run (Engine.SettleWorry, as a mend's), a "help"
// of yours in the ledger (the kindness the street saw, the late thanks' reading), enRAGE's liking, his thought after. Runners: the
// J menu and the posts' keepers (both seen); nobody walks anywhere but you. F7 "TEST letters:"; log "LETTER".
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _flNextLook, _flAgainAt, _flScanAt = -100000, _flTaken, _flGiven;
        private Resident _flHeldHere;          // this depot's letter for one you know, from the last scan (null: none)
        private string _flHeldTown, _flLast;
        private Ped _flAgent, _flTo;
        private readonly HashSet<string> _flOffered = new HashSet<string>();   // "id|kind": offered once a session each

        private void FindLetterStep(int now)
        {
            if (!FindLetter.On || _cfg == null || _placeTown == null || _store == null || _engine == null || now < _flNextLook)
                return;
            _flNextLook = now + FindLetter.LookMs;
            if (_eventMenu != null || now < _flAgainAt || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;
            Safe("find letter", () => FindLetterLook(now));
        }

        private void FindLetterLook(int now)
        {
            Ped me = Game.Player.Character;
            if (PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || PED.IS_PED_IN_COMBAT(me.Handle, 0))
                return;
            Ped agent = null;
            foreach (Ped p in Peds.All())
            {
                if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead || p.Position.DistanceTo(me.Position) > FindLetter.NearMetres)
                    continue;
                string id, name;
                if (LastingId(p, out id, out name) && id != null)
                {
                    PersonState st = _store.Find(id);
                    if ((st == null || !st.Has(FindLetter.HeldMark)) && _engine.WorryIdOf(id) != id)
                    {
                        id = _engine.WorryIdOf(id);   // enRAGE took him up after you took his letter: its mark is on the resident (review 5)
                        st = _store.Find(id);
                    }
                    if (st != null && st.Has(FindLetter.HeldMark) && !st.Has(FindLetter.GivenMark) && !_flOffered.Contains(id + "|give|" + now / 60000))
                    {
                        // the one you carry a letter for: J to give it over
                        _flOffered.Add(id + "|give|" + now / 60000);
                        _flTo = p;
                        _flAgainAt = now + FindLetter.AgainMs;
                        PromptSet(FindLetter.GiveKind, new TownEvent { Kind = FindLetter.GiveKind, Town = _placeTown, AId = id, AName = name, Extra = InnerLife.LetterKin(_engine.KinPerson(id)) }, FindLetter.PromptMs);
                        return;
                    }
                }
                PostKind pk = agent == null ? PostKindOf(p) : null;
                if (pk != null && pk.Kind == "station")
                    agent = p;
            }
            if (agent == null)
                return;
            Resident r = LetterHeldHere(now);
            if (r == null || _flOffered.Contains(r.Id + "|take"))
                return;
            _flOffered.Add(r.Id + "|take");
            _flAgent = agent;
            _flAgainAt = now + FindLetter.AgainMs;
            PromptSet(FindLetter.Kind, new TownEvent { Kind = FindLetter.Kind, Town = _placeTown, AId = r.Id, AName = r.Name, Extra = r.Town }, FindLetter.PromptMs);
        }

        /// A letter held at this depot for one you've met in another town, still waiting on it (a scan of the residents every 10 s,
        /// only while you stand at a station agent).
        private Resident LetterHeldHere(int now)
        {
            if (now - _flScanAt < FindLetter.ScanMs && now >= _flScanAt && _flHeldTown == _placeTown)
                return _flHeldHere;
            _flScanAt = now;
            _flHeldTown = _placeTown;
            _flHeldHere = null;
            if (Array.IndexOf(FindLetter.Towns, _placeTown) < 0)
                return null;
            long day = _store.LastGameMin / 1440;
            foreach (Resident r in _store.Residents)
            {
                if (r == null || r.Dead || r.Id == null || r.Town == null || r.Town == _placeTown || r.Name == null || r.Cast != null || _flOffered.Contains(r.Id + "|take"))
                    continue;   // (never a test's cast; one declined this session doesn't hide the next: review 4)
                if (_store.WorrySeedOf != null && _store.WorrySeedOf.ContainsValue(r.Id))
                    continue;   // taken up by enRAGE: his marks live under another id (review 4)
                PersonState st = _store.Find(r.Id);
                if (st == null || st.Has(FindLetter.HeldMark) || st.Has(FindLetter.GivenMark) || !(st.Talks > 0 || st.KnowsName || st.ContactAt > 0))
                    continue;   // only one you've met, and one letter a person
                Person p = _engine.PersonOfResident(r);
                string kind;
                int stage;
                bool good;
                if (p == null || !InnerLife.WorryNow(p, day, out kind, out stage, out good) || !FindLetter.Waiting(kind, stage))
                    continue;
                if (FindLetter.HeldAt(r.Id, r.Town, InnerLife.ArcFor(r.Id, day)) != _placeTown)
                    continue;
                _flHeldHere = r;
                break;
            }
            return _flHeldHere;
        }

        private void FindLetterChoose(string id, TownEvent e)
        {
            if (e == null || e.AId == null)
                return;
            if (id == "findletter:take")
                Safe("find letter", () => FindLetterTake(e));
            else if (id == "giveletter:give")
                Safe("find letter", () => FindLetterGive(e));
        }

        private void FindLetterTake(TownEvent e)
        {
            PersonState st = _store.Find(e.AId);
            if (st == null || st.Has(FindLetter.HeldMark))
                return;
            st.Mark(FindLetter.HeldMark);
            st.Mark(FindLetter.ArcMark + InnerLife.ArcFor(e.AId, _store.LastGameMin / 1440));
            Resident kept = _store.FindResident(e.AId);
            if (kept != null)
                _engine.KeepResident(kept, _store.LastGameMin);   // kept until it's delivered (review 4: a resident pruned took the mark with him)
            _flHeldHere = null;
            _flTaken++;
            if (_flAgent != null && _flAgent.Exists() && !_flAgent.IsDead)
                SpeechCaption("The station agent", FindLetter.AgentLine(e.AName, e.Extra, e.AId), _flAgent.Handle, 5000, true, false);
            Feed.Notice("A letter", FindLetter.Notice(e.AName, e.Extra));
            _flLast = "took " + e.AName + "'s letter at " + _placeTown;
            Log.Write("LETTER you carry " + e.AName + "'s letter (of " + (e.Extra ?? "?") + "), held at " + _placeTown);
        }

        private void FindLetterGive(TownEvent e)
        {
            PersonState st = _store.Find(e.AId);
            Ped to = _flTo, me = Game.Player.Character;
            if (st == null || !st.Has(FindLetter.HeldMark) || st.Has(FindLetter.GivenMark) || to == null || !to.Exists() || to.IsDead || TestPerson(to.Handle))
                return;
            long arcTaken = FindLetter.ArcOf(st.Told);
            st.Told.Remove(FindLetter.HeldMark);
            st.Told.RemoveAll(t => t != null && t.StartsWith(FindLetter.ArcMark, StringComparison.Ordinal));
            st.Mark(FindLetter.GivenMark);
            string seed = e.AId + "|letter";
            string first = Person.FirstOf(e.AName ?? "He");
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(to.Handle, me.Handle, 4000, 0f, 0f, 0f);
            string line = FindLetter.Reads(e.Extra, seed);
            Feed.Say(first, line, Speech.For(to.Handle, line, me.Handle, 0, 0, true, "relieved"), to.Handle, toYou: true);
            long day = _store.LastGameMin / 1440;
            Person wp = _engine.KinPerson(e.AId);
            string wk;
            int wstage;
            bool wgood;
            bool same = wp != null && InnerLife.WorryNow(wp, day, out wk, out wstage, out wgood) && wk == "letter" && InnerLife.ArcFor(e.AId, day) == arcTaken;
            if (same)
                _engine.SettleWorry(e.AId);   // the rest of his worry's run reads as ended well; a letter from a run that's over settles nothing new (review 4)
            RecordEvent("help", "player", "you", e.AId, e.AName, to.Position, me.Handle, me.Handle, to.Handle, FindLetter.Outcome);
            FolkRef ac = _bridge != null ? _bridge.AcquaintanceAt(to.Handle) : null;
            if (ac != null)
                _bridge.LikeMore(ac.Id, 20);
            _ticker.Push(first + ", thinking", FindLetter.After(seed), "near", to.Handle);
            _flGiven++;
            _flLast = "gave " + e.AName + " his letter";
            Log.Write("LETTER you gave " + e.AName + " the letter from " + (e.Extra ?? "his kin") + (same ? ": his worry ends well" : ": late, his worry had run its course"));
        }

        private string FindLetterStatus()
        {
            return "letters taken " + _flTaken + ", given " + _flGiven + (_flLast != null ? "; last: " + _flLast : "");
        }
    }
}
