// Someone you save is yours for good (Lex, 2026-11-03: "if Arthur saves a person in a brawl or execution, the person who was
// attacked will become max Fondness with the player"): the man you pull a bully off (Watch.cs Rescued), the one whose beater
// you put down in a brawl (BrawlDown.cs OnFighterDown), the captive you save from the rope (Executions.cs ExecutionSaved) and
// the hogtied you cut loose who thank you (Hogtied.cs HogFreedStep). enRAGE's fondness goes to the top (the bar under them;
// through its own LikeMore, an audited write; someone enRAGE hasn't taken up yet is tried again twice, seconds on), the
// ledger gets a "defend" happening with you the doer (Interact.SavedLately: a friend who owes you for 30 game days to
// Allies.Warmth, whatever else they feel, so they come to your aid; KindLately counts it as a kindness, so they trust you with
// a worry), and a memory of it (the mind's "defend|did:done" lines: "{who} stepped in for me"). Log "SAVED"; F7 "TEST saved:".
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
        private readonly List<int[]> _savedLikeLater = new List<int[]>();   // { ped, due at, tries }
        private int _savedCount;
        private string _savedLast;

        /// Someone you saved, known by their handle: named by the add-on if it can (the mind's who, else the ledger's resident).
        private void SavedByYou(int handle, string how)
        {
            string id = null, name = null;
            try
            {
                MindWho w = MindKnown(handle, Game.GameTime);
                if (w != null && w.Id != null)
                {
                    id = w.Id;
                    name = w.Name;
                }
                else
                {
                    Resident r = ResidentOf(new Ped(handle));
                    if (r != null)
                    {
                        id = r.Id;
                        name = r.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Detail("SAVED " + handle + " couldn't be named: " + ex.Message);
            }
            SavedByYou(handle, id, name ?? NameToShow(handle), how);
        }

        /// Someone you saved: enRAGE's fondness to the top, the ledger's "defend" with you the doer, and a memory of it.
        private void SavedByYou(int handle, string id, string name, string how)
        {
            if (handle == 0 || !ENTITY.DOES_ENTITY_EXIST(handle) || ENTITY.IS_ENTITY_DEAD(handle))
                return;
            try
            {
                Ped me = Game.Player.Character;
                if (!SavedLike(handle))
                    _savedLikeLater.Add(new[] { handle, Game.GameTime + 4000, 0 });   // enRAGE may take them up a moment on
                if (id != null)
                {
                    RecordEvent("defend", "player", "you", id, name ?? "someone", ENTITY.GET_ENTITY_COORDS(handle, true, true), me.Handle, me.Handle, handle, how);
                    PersonState st = _store.Find(id);
                    if (st != null)
                    {
                        if (st.Mind == null)
                            st.Mind = new List<MindMemory>();
                        string you = _engine.KnowsYourName(id) && YourFullName() != null ? Person.FirstOf(YourFullName()) : "the stranger";
                        Minds.Note(st.Mind, new MindMemory
                        {
                            Kind = "warmth", Sub = "defend", Who = new List<string> { "player" }, Names = new List<string> { you }, Town = _placeTown, At = _store.LastGameMin,
                            How = "did", Role = "done", Feel = 3, Strength = 80, Key = "saved:" + id + ":" + _store.LastGameMin / 1440 + ":" + SecondActs.HowKind(how)   // the how, for the thanks later (FOURTEENHOUR reserve R3)
                        }, _store.LastGameMin);
                    }
                }
                SavedKnown(handle, name, how);
                _savedCount++;
                _savedLast = (name ?? "someone") + " (" + how + ")";
                Log.Write("SAVED " + (name ?? "someone") + ": " + how + "; theirs for good (fondness to the top" + (id != null ? "; the ledger and their memory have it" : "; nobody the add-on knows by name") + ")");
            }
            catch (Exception ex)
            {
                Log.Write("SAVED " + (name ?? handle.ToString()) + " couldn't be kept: " + ex.Message);
            }
        }

        /// A good turn of yours kept in his own mind as one he owes you for (SEAMS items 1 and 2): warmth with you in it, the
        /// saved key ("saved:<id>:<day>:<how>") that SecondActs' late thanks looks for, by the how's own thanks words. Only
        /// someone who already has a saved state (as SavedByYou's); false when there's none, and nobody is made for it.
        private bool OwedMemory(string id, string town, string sub, string how, int strength)
        {
            PersonState st = id != null && _store != null ? _store.Find(id) : null;
            if (st == null)
                return false;
            if (st.Mind == null)
                st.Mind = new List<MindMemory>();
            long g = _store.LastGameMin;
            string you = _engine.KnowsYourName(id) && YourFullName() != null ? Person.FirstOf(YourFullName()) : "the stranger";
            Minds.Note(st.Mind, new MindMemory
            {
                Kind = "warmth", Sub = sub, Who = new List<string> { "player" }, Names = new List<string> { you }, Town = town ?? _placeTown, At = g,
                How = "did", Role = "done", Feel = 3, Strength = strength, Key = "saved:" + id + ":" + g / 1440 + ":" + how
            }, g);
            return true;
        }

        /// The town hears of the saving (SEAMS item 17; [Scenes] SavedKnown): a "saved" deed, as a fight broken up or a thief run down
        /// is one, so those who saw it remember it, it's told about ("stood up for Abe"), the paper may carry it and the
        /// town's ledger counts it. Before, only the rope's rescue (Executions) was a deed: the bully pulled off a man, his beater
        /// put down, the hogtied cut loose were known only to the one saved. Never the rope (its own "rescue"), never a test's.
        private void SavedKnown(int handle, string name, string how)
        {
            if (!_cfg.SavedKnown || !_cfg.Deeds || _engine == null || SecondActs.HowKind(how) == "rope" || TestPerson(handle) || !ENTITY.DOES_ENTITY_EXIST(handle))
                return;
            Vector3 pos = ENTITY.GET_ENTITY_COORDS(handle, true, true);
            long g = Bridge.Now();
            string who = name != null ? Person.FirstOf(name) : "a stranger";
            NoteDeedMinds(_engine.OnDeed(Deeds.Make("saved", who, _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, handle, 25f)), WitnessesNear(pos, handle, 25f), g), pos, handle);
            Log.Write("SAVED " + who + ": a good deed the town will hear of (" + how + ")");
        }

        /// enRAGE's fondness to the top, when it has the person; false when it hasn't yet.
        private bool SavedLike(int handle)
        {
            FolkRef ac = _bridge.AcquaintanceAt(handle);
            if (ac == null || ac.Id == null)
                return false;
            _bridge.LikeMore(ac.Id, 100);   // LikeMore keeps it to 100
            return true;
        }

        /// Someone enRAGE hadn't taken up when you saved them: tried again, twice.
        private void SavedLater(int now)
        {
            for (int i = _savedLikeLater.Count - 1; i >= 0; i--)
            {
                int[] s = _savedLikeLater[i];
                if (now < s[1])
                    continue;
                _savedLikeLater.RemoveAt(i);
                try
                {
                    if (ENTITY.DOES_ENTITY_EXIST(s[0]) && !ENTITY.IS_ENTITY_DEAD(s[0]) && !SavedLike(s[0]) && s[2] < 1)
                        _savedLikeLater.Add(new[] { s[0], now + 6000, s[2] + 1 });
                }
                catch (Exception ex)
                {
                    Log.Detail("SAVED the late fondness failed: " + ex.Message);
                }
            }
        }

        private string SavedStatus()
        {
            return _savedCount + " saved by you this session (a bully pulled off them, their beater put down, the rope, the hogtied cut loose): fondness to the top, a friend who owes you for 30 game days" +
                   (_savedLast != null ? "; last: " + _savedLast : "");
        }
    }
}
