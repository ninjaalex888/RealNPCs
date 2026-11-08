// The epilogue remembers ([World] EpilogueCarries; Core/Epilogue.cs; SIXDAY item 23; roadmap 25). When John's save is made
// for the first time with Arthur's beside it, Arthur's residents come over eight years on; and every thirty seconds the
// known people near John in view are looked at for one who remembers a man called Morgan, who thinks of it once over
// their head. F7 "TEST epilogue:".
using System;
using System.IO;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public sealed partial class WestworldScript
    {
        private const int EpilogueLookMs = 30000;
        private int _nextEpilogue, _morganThoughts;
        private string _epilogueCarried;
        private Action _aEpilogue;

        /// At John's load: a fresh store with arthur.json beside it takes Arthur's world over, once (Core/Epilogue.cs).
        private void EpilogueCarry(bool john)
        {
            if (!john || _cfg == null || !_cfg.EpilogueCarries || _store == null || _store.CarriedOver || _store.Residents.Count > 0)
                return;
            try
            {
                string arthurPath = Path.Combine(_dir, "arthur.json");
                if (!File.Exists(arthurPath))
                {
                    Log.Write("EPILOGUE: John's save is new and there is no arthur.json beside it; nothing to carry over");
                    return;
                }
                Store arthur = Store.Load(arthurPath, Log.Write);
                int year = 1907;
                try { year = Math.Max(1907, CLOCK.GET_CLOCK_YEAR()); } catch { }
                _epilogueCarried = Epilogue.CarryOver(arthur, _store, Bridge.Now(), year);   // on John's clock (0 put the carried people first in line to be dropped: the review of items 20-27)
                Log.Write("EPILOGUE: " + _epilogueCarried);
            }
            catch (Exception ex)
            {
                Log.Write("EPILOGUE: the carry-over failed (" + ex.Message + "); John starts with a fresh world");
            }
        }

        /// Tick04: a look every thirty seconds, John only, under its own Safe.
        private void EpilogueStep(int now)
        {
            if (_cfg == null || !_cfg.EpilogueCarries || _store == null || _engine == null || now < _nextEpilogue || !_engine.Player.Scarred)
                return;
            _nextEpilogue = now + EpilogueLookMs;
            if (_aEpilogue == null)
                _aEpilogue = () => EpilogueLook(_now);
            Safe("epilogue", _aEpilogue);
        }

        private void EpilogueLook(int now)
        {
            // _mindNear is the mind shadow's list of the known people near (WestworldScript.Mind.cs): with [Mind] MindShadow off nobody is
            // ever near here (said in F7; the review of items 15-19)
            if (_mindNear.Count == 0 || MISC.GET_MISSION_FLAG())
                return;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            for (int i = 0; i < _mindNear.Count; i++)
            {
                int h = _mindNear[i];
                MindWho w;
                if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                PersonState st = _store.Find(w.Id);   // never made here: everyone who remembers has a state from the carry (the review of items 20-27)
                if (st == null || !st.Has(Epilogue.Morgan) || st.Has(Epilogue.MorganSaid))
                    continue;
                Ped p = new Ped(h);
                if (p.Position.DistanceTo(me.Position) > 15f || !NodInView(me, p))
                    continue;
                st.Mark(Epilogue.MorganSaid);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4000, 0, 51, 0);
                Resident boy = _cfg.BoyRemembers ? _store.FindResident(w.Id) : null;
                bool wasBoy = Epilogue.WasBoy(boy);
                string thought = wasBoy ? Epilogue.BoyThought(w.Id + "|" + now / 60000) : Epilogue.MorganThought(w.Id + "|" + now / 60000);   // a boy's memory is a boy's (FIVEDAY R25)
                if (wasBoy && p.Position.DistanceTo(me.Position) <= 6f)
                {
                    SpeechCaption(Person.FirstOf(w.Name) ?? "A young man", Epilogue.BoyAsks(w.Id), h, 6000, true);   // he says it aloud, once
                    _boysAsked++;
                }
                _ticker.Push(w.Name + ", thinking", thought, "near", h);
                _morganThoughts++;
                Log.Write("EPILOGUE " + w.Name + " remembers a man called Morgan; thinks: " + thought);
                return;   // one a look
            }
        }

        private int _boysAsked;

        private string EpilogueStatus()
        {
            if (_cfg == null || !_cfg.EpilogueCarries)
                return "off";
            if (_store == null)
                return "not loaded";
            int remember = 0, since = 0;
            foreach (Resident r in _store.Residents)
                if (r != null && r.Since1899 != null)
                    since++;
            foreach (PersonState st in _store.People.Values)
                if (st != null && st.Has(Epilogue.Morgan))
                    remember++;
            return (_store.CarriedOver ? "carried over" + (_epilogueCarried != null ? " this session (" + _epilogueCarried + ")" : "") : "not carried over" + (_engine != null && _engine.Player.Scarred ? " (John's save had people already, or no arthur.json)" : " (Arthur's own save)")) +
                   "; " + since + " residents with eight years on them, " + remember + " remember a man called Morgan, " + _morganThoughts + " thought of it this session" + (!_cfg.MindShadow ? "; the look needs [Mind] MindShadow, which is off" : "");
        }
    }
}
