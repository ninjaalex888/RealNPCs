// Spread a rumour, the game side (DEPTH reserve R3; Core/Slander.cs; [Scenes] SpreadRumour, with [Mind] MemoriesBlur's tales). A look every
// 2 s (the frame pass's scheduler; Tick04's own call with it off): in a saloon, J free, a talker within 4 m of you (one the mind knows,
// sociable by his traits, a man, on his feet or at the bar: nobody is given an order) gets the J prompt once a talker a game day. The
// rows: up to three men of this town whose names you know (not the talker). Chosen: his answer, and the town's tales gain a slander
// (Core/Tales.cs), told and bent as they are, and thought over by the man it's said of when he's near and has heard. Runner: the J menu
// (seen), the tales (built, not yet seen).
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
        private readonly HashSet<string> _slanderAsked = new HashSet<string>();
        private Action _aSlander;
        private int _slanderNextLook, _slanderOffered, _slanderSpread;
        private string _slanderTalker, _slanderTalkerName, _slanderLast;
        private int _slanderTalkerHandle;

        private void SlanderStep(int now)
        {
            if (!Slander.On || !Tales.On || now < _slanderNextLook)
                return;
            _slanderNextLook = now + 2000;
            Safe("spread a rumour", _aSlander ?? (_aSlander = () => SlanderLook(_now)));
        }

        private void SlanderLook(int now)
        {
            if (_placeKind != "saloon" || _store == null || _placeTown == null || _eventMenu != null || (_eventPrompt != null && Game.GameTime <= _eventPromptUntil))
                return;
            Ped me = Game.Player.Character;
            long day = _store.LastGameMin / 1440;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.Name == null || w.Traits == null || !ENTITY.DOES_ENTITY_EXIST(h) || !PED.IS_PED_MALE(h))
                    continue;
                if (w.Traits.Z[Traits.Sociability] < 0.5f || _slanderAsked.Contains(w.Id + "|" + day) || new Ped(h).Position.DistanceTo(me.Position) > Slander.TalkerMetres)
                    continue;
                if (SlanderTargets(w.Id).Count == 0)
                    return;
                if (_slanderAsked.Count > 300)
                    _slanderAsked.Clear();
                _slanderAsked.Add(w.Id + "|" + day);
                _slanderTalker = w.Id;
                _slanderTalkerName = w.Name;
                _slanderTalkerHandle = h;
                _slanderOffered++;
                PromptSet(Slander.PromptKind, new TownEvent { Kind = Slander.PromptKind, Town = _placeTown, AName = w.Name, AId = w.Id }, 8000);
                return;
            }
        }

        /// Up to three men of this town whose names you know: id and name.
        private List<KeyValuePair<string, string>> SlanderTargets(string notId)
        {
            var list = new List<KeyValuePair<string, string>>();
            foreach (Resident r in _store.Residents)
            {
                if (r == null || r.Id == null || r.Name == null || r.Woman || r.Dead || r.Left || r.Fled || r.Town != _placeTown || r.Id == notId)
                    continue;
                PersonState st = _store.Find(r.Id);
                if (st == null || !st.KnowsName)
                    continue;
                list.Add(new KeyValuePair<string, string>(r.Id, r.Name));
                if (list.Count >= 3)
                    break;
            }
            return list;
        }

        private List<InteractOption> SlanderRows()
        {
            return Slander.Rows(SlanderTargets(_slanderTalker), _store.LastGameMin / 1440);
        }

        private void SlanderChoose(string id)
        {
            if (id == null || !id.StartsWith("slander:", StringComparison.Ordinal) || id == "slander:no" || _store == null || _store.Tales == null)
                return;
            string targetId = id.Substring("slander:".Length);
            Resident r = _store.FindResident(targetId);
            if (r == null || r.Name == null)
                return;
            long g = _store.LastGameMin;
            string first = Person.FirstOf(r.Name), charge = Slander.ChargeFor(r.Id, g / 1440);
            if (_slanderTalkerHandle != 0 && ENTITY.DOES_ENTITY_EXIST(_slanderTalkerHandle))
                SpeechCaption(Person.FirstOf(_slanderTalkerName) ?? "He", Slander.Takes(first, targetId + "|" + g), _slanderTalkerHandle, 3500, true, true);
            _store.Tales.Add(new TownTale { Id = Ids.New(), Kind = Slander.Kind, Town = _placeTown, Starter = first, How = charge, About = r.Id, At = g, Hour = CLOCK.GET_CLOCK_HOURS() });
            while (_store.Tales.Count > Tales.Kept)
                _store.Tales.RemoveAt(0);
            _slanderSpread++;
            _slanderLast = "about " + r.Name + " (" + charge + "), told to " + _slanderTalkerName;
            Log.Write("TALE slander: you put it about " + r.Name + " (" + charge + "), in " + _slanderTalkerName + "'s ear, in " + _placeTown);
        }

        private string SlanderStatus()
        {
            return !Slander.On ? "off ([Scenes] SpreadRumour)" : "offered " + _slanderOffered + ", spread " + _slanderSpread + (_slanderLast != null ? "; the last: " + _slanderLast : "");
        }
    }
}
